# QUESTS ROADMAP
> Roadmap específico para el sistema de Misiones y Contenido de Quests.

## Definición de “listo” (DoD)
### Contenido (quests)
- Definición en JSON con schema y validación.
- Coverage mínima (si es questline: gating + rewards + flags).
- Cero nombres/listas copiadas de WLO/NWLO/Wonderland M.

---

## Backlog de Quests (P1)

- [ ] **QST-001**: Schema JSON + validador (prereqs, objectives, rewards, repeatability, expiry).
- [ ] **QST-002**: QuestEngine consume eventos server-side (kill/drop/interact/craft/instance).
- [ ] **QST-003**: Objectives: `KillCount`, `Collect`, `Deliver`, `Interact`, `Explore`.
- [ ] **QST-004**: Objectives: `Craft/Compound`, `Party/Guild`, `Instance`, `EventParticipation`.
- [ ] **QST-005**: Rewards idempotentes + auditoría.
- [ ] **QST-006**: 1 questline “starter island” (3–7 quests) sin copiar contenido original.
- [ ] **QST-007**: Questlines regionales (1 por región) + gating por flags.
- [ ] **QST-008**: Puerto Roca Questline (IDs 1100-1104). Implementar la línea de misiones de Puerto Roca, asegurando instanciación de entidades (Bandido, Lobo, Caravan Leader, etc.) y lógica de interacción.
