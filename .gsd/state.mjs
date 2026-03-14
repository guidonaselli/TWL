import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

function read(path) {
  return existsSync(path) ? readFileSync(path, 'utf8') : null;
}

function readStateHints(basePath) {
  const content = read(join(basePath, '.gsd', 'STATE.md'));
  if (!content) return null;
  const milestone = content.match(/^- Active Milestone:\s+(M\d+(?:-[a-z0-9]{6})?)\b/m)?.[1] || null;
  const slice = content.match(/^- Active Slice:\s+(S\d+)\b/m)?.[1] || null;
  const task = content.match(/^- Active Task:\s+(T\d+)\b/m)?.[1] || null;
  if (!milestone || !slice) return null;
  return { milestoneId: milestone, sliceId: slice, taskId: task };
}

function parseRequirements(content) {
  const counts = { active: 0, validated: 0, deferred: 0, outOfScope: 0, blocked: 0, total: 0 };
  if (!content) return counts;
  const sectionBody = (heading) => {
    const lines = content.split('\n');
    const start = lines.findIndex(l => l.trim() === `## ${heading}`);
    if (start === -1) return '';
    let end = lines.length;
    for (let i = start + 1; i < lines.length; i++) {
      if (/^##\s+/.test(lines[i])) { end = i; break; }
    }
    return lines.slice(start + 1, end).join('\n');
  };
  const countInSection = (heading) => ((sectionBody(heading).match(/^###\s+[A-Z0-9-]+\s+—/gm) || []).length);
  counts.validated = countInSection('Validated');
  counts.active = countInSection('Active');
  counts.deferred = countInSection('Deferred');
  counts.outOfScope = countInSection('Out of Scope');
  counts.blocked = (content.match(/- Status: blocked/gi) || []).length;
  counts.total = counts.active + counts.validated + counts.deferred + counts.outOfScope;
  return counts;
}

function parseRoadmap(content) {
  const title = (content.match(/^#\s+(.+)$/m)?.[1] || '').trim();
  const slices = [...content.matchAll(/^- \[([ x])\] \*\*(S\d+):\s+(.+?)\*\* `risk:([^`]+)` `depends:\[([^\]]*)\]`/gm)].map(m => ({
    done: m[1].toLowerCase() === 'x',
    id: m[2],
    title: m[3].trim(),
    risk: m[4].trim(),
    depends: m[5].trim() ? m[5].split(',').map(s => s.trim()).filter(Boolean) : [],
  }));
  return { title, slices };
}

function parsePlan(content) {
  const h1 = content.match(/^#\s+(S\d+):\s+(.+)$/m);
  const tasks = [...content.matchAll(/^- \[([ x])\] \*\*(T\d+):\s+(.+?)\*\*/gm)].map(m => ({
    done: m[1].toLowerCase() === 'x',
    id: m[2],
    title: m[3].trim(),
  }));
  return { id: h1?.[1] || '', title: h1?.[2] || '', tasks };
}

function findMilestoneIds(basePath) {
  const dir = join(basePath, '.gsd', 'milestones');
  if (!existsSync(dir)) return [];
  return readdirSync(dir, { withFileTypes: true })
    .filter(d => d.isDirectory() && /^M\d+/.test(d.name))
    .map(d => d.name.match(/^(M\d+(?:-[a-z0-9]{6})?)/)?.[1] || d.name)
    .sort();
}

export async function deriveState(basePath) {
  const milestoneIds = findMilestoneIds(basePath);
  const requirements = parseRequirements(read(join(basePath, '.gsd', 'REQUIREMENTS.md')));

  if (milestoneIds.length === 0) {
    return {
      activeMilestone: null, activeSlice: null, activeTask: null, phase: 'pre-planning', recentDecisions: [], blockers: [],
      nextAction: 'No milestones found. Run /gsd to create one.', registry: [], requirements, progress: { milestones: { done: 0, total: 0 } },
    };
  }

  const registry = [];
  let activeMilestone = null;
  let activeRoadmap = null;
  for (const mid of milestoneIds) {
    const roadmapContent = read(join(basePath, '.gsd', 'milestones', mid, `${mid}-ROADMAP.md`));
    if (!roadmapContent) {
      registry.push({ id: mid, title: mid, status: activeMilestone ? 'pending' : 'active' });
      if (!activeMilestone) activeMilestone = { id: mid, title: mid };
      continue;
    }
    const roadmap = parseRoadmap(roadmapContent);
    const title = roadmap.title.replace(/^M\d+(?:-[a-z0-9]{6})?:\s*/, '');
    const complete = roadmap.slices.length > 0 && roadmap.slices.every(s => s.done);
    if (complete) registry.push({ id: mid, title, status: 'complete' });
    else if (!activeMilestone) {
      activeMilestone = { id: mid, title };
      activeRoadmap = roadmap;
      registry.push({ id: mid, title, status: 'active' });
    } else registry.push({ id: mid, title, status: 'pending' });
  }

  const progress = { milestones: { done: registry.filter(r => r.status === 'complete').length, total: registry.length } };
  if (!activeMilestone || !activeRoadmap) {
    return { activeMilestone: null, activeSlice: null, activeTask: null, phase: 'complete', recentDecisions: [], blockers: [], nextAction: 'All milestones complete.', registry, requirements, progress };
  }

  progress.slices = { done: activeRoadmap.slices.filter(s => s.done).length, total: activeRoadmap.slices.length };

  const hints = readStateHints(basePath);
  if (hints && hints.milestoneId === activeMilestone.id) {
    const slice = activeRoadmap.slices.find(s => s.id === hints.sliceId) || null;
    if (slice) {
      const planContent = read(join(basePath, '.gsd', 'milestones', activeMilestone.id, 'slices', slice.id, `${slice.id}-PLAN.md`));
      const plan = planContent ? parsePlan(planContent) : { tasks: [] };
      const task = (hints.taskId && plan.tasks.find(t => t.id === hints.taskId)) || plan.tasks.find(t => !t.done) || null;
      progress.tasks = { done: plan.tasks.filter(t => t.done).length, total: plan.tasks.length };
      return {
        activeMilestone,
        activeSlice: { id: slice.id, title: slice.title },
        activeTask: task ? { id: task.id, title: task.title } : null,
        phase: task ? 'executing' : 'summarizing',
        recentDecisions: [], blockers: [],
        nextAction: task ? `Execute ${task.id}: ${task.title} in slice ${slice.id}.` : `Complete slice ${slice.id}.`,
        registry, requirements, progress,
      };
    }
  }

  const doneSliceIds = new Set(activeRoadmap.slices.filter(s => s.done).map(s => s.id));
  const slice = activeRoadmap.slices.find(s => !s.done && s.depends.every(d => doneSliceIds.has(d))) || activeRoadmap.slices.find(s => !s.done) || null;
  if (!slice) {
    return { activeMilestone, activeSlice: null, activeTask: null, phase: 'blocked', recentDecisions: [], blockers: ['No active slice could be resolved.'], nextAction: 'Resolve slice dependencies.', registry, requirements, progress };
  }

  const planContent = read(join(basePath, '.gsd', 'milestones', activeMilestone.id, 'slices', slice.id, `${slice.id}-PLAN.md`));
  if (!planContent) {
    return { activeMilestone, activeSlice: { id: slice.id, title: slice.title }, activeTask: null, phase: 'planning', recentDecisions: [], blockers: [], nextAction: `Plan slice ${slice.id}.`, registry, requirements, progress };
  }
  const plan = parsePlan(planContent);
  progress.tasks = { done: plan.tasks.filter(t => t.done).length, total: plan.tasks.length };
  const task = plan.tasks.find(t => !t.done) || null;
  return {
    activeMilestone,
    activeSlice: { id: slice.id, title: slice.title },
    activeTask: task ? { id: task.id, title: task.title } : null,
    phase: task ? 'executing' : 'summarizing',
    recentDecisions: [], blockers: [],
    nextAction: task ? `Execute ${task.id}: ${task.title} in slice ${slice.id}.` : `Complete slice ${slice.id}.`,
    registry, requirements, progress,
  };
}

export async function getActiveMilestoneId(basePath) {
  const state = await deriveState(basePath);
  return state.activeMilestone?.id ?? null;
}

if (import.meta.url === new URL(`file://${process.argv[1]}`).href) {
  const basePath = process.argv[2] || process.cwd();
  const state = await deriveState(basePath);
  console.log(JSON.stringify(state, null, 2));
}
