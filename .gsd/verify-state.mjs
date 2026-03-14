#!/usr/bin/env node
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { deriveState } from './state.mjs';

const basePath = process.argv[2] ? resolve(process.argv[2]) : process.cwd();
const state = await deriveState(basePath);
const stateMd = await readFile(resolve(basePath, '.gsd', 'STATE.md'), 'utf8');

const summary = {
  phase: state.phase,
  activeMilestone: state.activeMilestone?.id ?? null,
  activeSlice: state.activeSlice?.id ?? null,
  activeTask: state.activeTask?.id ?? null,
  requirements: state.requirements,
  progress: state.progress,
  stateMdPreview: stateMd.split('\n').slice(0, 20).join('\n'),
};

console.log(JSON.stringify(summary, null, 2));
