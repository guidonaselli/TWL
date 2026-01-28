# SKILLS ROADMAP
> Roadmap específico para el sistema de Habilidades y Packs de Skills.

## Definición de “listo” (DoD)
### Contenido (skills)
- Definición en JSON con schema y validación.
- Coverage mínima (si es skill pack: 3 ramas).
- Cero nombres/listas copiadas de WLO/NWLO/Wonderland M (Excepción: Nombres de Goddess Skills).

---

## Backlog de Skills (P1)

- [x] **SKL-001**: Schema JSON definitivo + validador (IDs únicos, referencias, constraints por tier).
- [x] **SKL-002**: Mastery por uso + thresholds + persistencia + stage upgrades (1/2/3 con {6,12}).
- [x] **SKL-003**: UnlockRules (level/stats/pre-skill mastery/quest flag).
- [ ] **SKL-004**: Tier budgets (SP range, coeficientes, límites de control).
- [ ] **SKL-005**: Pack T1 — Earth (3 ramas) + tests.
- [ ] **SKL-006**: Pack T1 — Water (3 ramas) + tests.
- [ ] **SKL-007**: Pack T1 — Fire (3 ramas) + tests.
- [ ] **SKL-008**: Pack T1 — Wind (3 ramas) + tests.
- [ ] **SKL-009**: Packs T2 (rotación por elemento) + evolución stage 2/3 donde aplique.
- [x] **SKL-010**: Counters avanzados (Stacking Policies, Priority, Resistance logic).
- [ ] **SKL-011**: Goddess Skills. Implementar habilidades de Diosa (Shrink, Blockage, Hotfire, Vanish) con IDs separados al resto y evitando que esten duplicados, asegurando también su asignación por elemento.
- [ ] **SKL-012**: Life Skills (Passives). Implementar skills pasivas de Alquimia, Minería, Pesca (si aplica) que modifican rates de economía.
