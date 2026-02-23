const fs = require('fs');

const questsJson = fs.readFileSync('Content/Data/quests.json', 'utf8');
const quests = JSON.parse(questsJson);

// Arcs to check
// Phase 3 arcs: Hidden Ruins (1301-1307), Ruins Expansion (1401-1404), Hidden Cove (2401)
const targetQuestIds = [1301, 1302, 1303, 1304, 1305, 1306, 1307, 1401, 1402, 1403, 1404, 2401];

const requiredKeys = new Set();

quests.forEach(q => {
    if (targetQuestIds.includes(q.QuestId)) {
        if (q.TitleKey) requiredKeys.add(q.TitleKey);
        if (q.DescriptionKey) requiredKeys.add(q.DescriptionKey);
        
        if (q.Objectives) {
            q.Objectives.forEach(obj => {
                if (obj.DescriptionKey) requiredKeys.add(obj.DescriptionKey);
            });
        }
    }
});

const reportJson = fs.readFileSync('artifacts/localization-index.json', 'utf8');
const report = JSON.parse(reportJson);

const existingKeys = new Set(report.ResourceKeys.base);

const missingKeys = [];
requiredKeys.forEach(k => {
    if (!existingKeys.has(k)) {
        missingKeys.push(k);
    }
});

console.log("Required Keys:", requiredKeys.size);
console.log("Missing Keys for Phase 3:");
missingKeys.forEach(k => console.log(k));
