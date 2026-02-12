# CORE ROADMAP

> **JULES-CONTEXT**: This is the master technical roadmap. When picking tasks, follow the
> priority order (P0 before P1) and the daily rotation schedule in section 5. Each task
> must produce: working code, tests, and a brief doc update. Never skip server-side validation.
> Reference: `GAMEPLAY_CONTRACTS.md` for system rules, `CONTENT_RULES.md` for data rules.

> Roadmap principal para Core Server, Arquitectura, Red, y Sistemas Base.

## 0) Principios no negociables (Invariantes)
1. **Server-authoritative**: combate, RNG, drops, progreso de quests, economía, inventario, social y flags se resuelven en servidor.
2. **Cliente = UI**: el cliente solo envía intents (`CastSkill`, `Move`, `Interact`, `TradeRequest`, etc.) y renderiza resultados.
3. **Data-driven**: skills/quests/items/recipes/mobs/maps/eventos se definen en contenido (JSON/tiles/paquetes) con **validación estricta**.
4. **Idempotencia**: cualquier operación “valiosa” (rewards, compras, trade, compound, drop commit) debe ser idempotente.
5. **Anti-dupe / anti-replay**: reintentos, reconexiones y latencia no pueden duplicar resultados.
6. **PRs pequeños y verificables**: build + tests en verde; cada PR aporta tests o evidencia (logs/bench) según el dominio.

---

## 1) Definición de “listo” (DoD)
### Feature (mecánica)
- Implementación server-side completa (pipeline + persistencia si aplica).
- Validación de inputs (fail-closed) y controles anti-abuso.
- Logs estructurados (correlation id + evento de dominio).
- Tests mínimos: unitarios y/o integración según criticidad.
- Contenido data-driven + validador actualizado (si aplica).
- Doc breve en `docs/<dominio>/` (contratos + ejemplos).

### Infra / refactor
- No cambia comportamiento salvo lo especificado.
- Reduce acoplamiento o riesgo medible.
- Incluye tests de regresión o harness.

---

## 2) Fases del proyecto (macro-milestones)
### Fase A — Foundation (P0)
Objetivo: servidor estable, contratos claros, persistencia mínima segura, y loop jugable básico (login → moverse → pelear → loot → quest simple).

### Fase B — Vertical Slice (P0/P1)
Objetivo: 1 región, 1 dungeon/instancia, 1 loop económico básico (craft/compound simple), 1 loop social básico (party), 1 loop de progresión (skills mastery + questline).

### Fase C — Alpha (P1)
Objetivo: 2–3 regiones, pets integradas, guilds, marketplace (si aplica), eventos del sistema, observabilidad real, hardening anti-cheat.

### Fase D — Beta (P1/P2)
Objetivo: balance, carga, anti-abuso maduro, migraciones de datos, economía estable, experiencia social completa.

### Fase E — Live (P2/P3)
Objetivo: contenido continuo, tooling, operaciones, monetización segura (item mall) y anti-fraude.

---

## 3) Orden de prioridad por dominios
**P0**: Core server, netcode, persistencia, combate, inventario, validación, anti-dupe, logs, tests.
**P1**: Skills completo (packs), quests robusto (questlines), economía (compound/crafting), party, instancias, pets core.
**P2**: Guilds/marriage, marketplace completo, eventos, PvP (si aplica), item mall.
**P3**: QoL, cosmetics, herramientas avanzadas, expansiones de contenido.

---

## 4) Backlog de Core Systems

### 4.1 CORE SERVER / NETWORK / SECURITY (P0)
- [ ] **CORE-001**: Protocolo de mensajes versionado (opcode + schemaVersion) con validación estricta (fail-closed).
- [ ] **CORE-002**: Sesiones + `sequence/nonce` por cliente para protección anti-replay (mínimo viable).
- [ ] **CORE-003**: Rate limiting por opcode (move/cast/chat/trade) + métricas de rechazos.
- [ ] **CORE-004**: Correlation ID end-to-end (client intent → server resolve → persist).
- [x] **CORE-005**: Servicio RNG server-side seedable (`IRandomService`) + auditoría de outcomes.
- [ ] **CORE-006**: Pipeline de comandos: `Validate -> Authorize -> Resolve -> Persist -> EmitEvents`.
- [ ] **CORE-007**: Autorización por dominio (ownership checks) para inventario, pets, party, guild, mail, marketplace.
- [ ] **CORE-008**: Idempotency keys estandarizadas para operaciones valiosas (reward/trade/compound/purchase).
- [ ] **CORE-009**: Hardening de serialización/deserialización (límites de payload, enums whitelist, strings length).
- [ ] **CORE-010**: Tests de seguridad básicos (replay/retry, invalid ranges, spoofed ownership).

### 4.2 PERSISTENCE / RELIABILITY / OBSERVABILITY (P0/P1)
- [ ] **PERS-001**: Modelo de estado persistente mínimo (character, inventory, quest flags, skill mastery, pets stub).
- [ ] **PERS-002**: Dirty flags + batch/interval flush (evitar write por microcambio).
- [ ] **PERS-003**: Operaciones económicas con estado `Pending -> Committed` (journal mínimo).
- [ ] **PERS-004**: Outbox/inbox (o equivalente) para entregas críticas (rewards, mall delivery).
- [ ] **PERS-005**: Recovery básico: snapshot + replay de journal (o compensaciones).
- [ ] **PERS-006**: Health checks + graceful shutdown (flush controlado).
- [ ] **PERS-007**: Métricas del pipeline (latencia por etapa, throughput, errores, colas).
- [ ] **PERS-008**: Harness mínimo de carga (N bots simulados) + reporte comparativo.

### 4.3 COMBAT / STATUS ENGINE (P0/P1)
- [ ] **COMB-001**: Turn system determinista (SPD ordering) + tie-break server-side.
- [ ] **COMB-002**: Targeting shapes (single/aoe/line/cross/self/ally/enemy) estandarizados.
- [ ] **COMB-003**: Status engine tag-based (`Buff`, `Debuff`, `Control`, `DoT`, `Seal`, `Barrier`).
- [ ] **COMB-004**: Cleanse/Dispel por tags + prioridades + límites por cast.
- [ ] **COMB-005**: Reglas de hit chance para control (INT u otra regla explícita) + tests seedables.
- [ ] **COMB-006**: Ciclo Elemental estricto (Water > Fire > Wind > Earth) + Interactions (Counter/Seal rate).

### 4.6 INVENTORY / ITEMS / DROPS (P0/P1)
- [ ] **INV-001**: Inventario transaccional (add/remove/stack) con ownership checks.
- [ ] **INV-002**: Drops server-side (RNG, loot tables data-driven, commit idempotente).
- [ ] **INV-003**: Equipamiento y stats derivados coherentes (STR/INT/WIS/AGI/CON → ATK/MAT/MDEF/DEF/SPD).
- [ ] **INV-004**: Validador de items/recipes/loot tables (IDs, referencias, constraints).

### 4.7 ECONOMY: ALCHEMY / CRAFT / MARKET (P1/P2)
- [ ] **ECO-001**: Alchemy (Compound) System: Tier + Material Logic (Resonance-based crafting).
- [ ] **ECO-002**: Manufacturing System: Blueprints + Workbench interactions.
- [ ] **ECO-003**: RNG del compound auditable + métricas de éxito/fallo.
- [ ] **ECO-004**: Anti-dupe: doble consumo/rollback parcial/imposible.
- [ ] **ECO-005**: Marketplace P2P (listing, buy, cancel) con locks/idempotencia.
- [ ] **ECO-006**: Sinks (fees/taxes) + límites anti-bot.
- [ ] **ECO-007**: Item Mall: flujo receipt-verification + ledger append-only + delivery garantizada.
- [ ] **ECO-008**: Refund/chargeback handling (mínimo: marcar y bloquear benefits si aplica).

### 4.8 SOCIAL: TEAM / GUILD / MARRIAGE (P1/P2)
- [ ] **SOC-001**: Team System (Formations + Leader logic + Join/Leave).
- [ ] **SOC-002**: Team en combate (turn ownership, disconnect handling, combo attack).
- [ ] **SOC-003**: Guild (create/roles/permissions) + storage mínimo + logs.
- [ ] **SOC-004**: Marriage/intimacy (estado persistente + anti-abuso) si se incluye.
- [ ] **SOC-005**: Moderación y anti-spam en chat (rate limits + filtros + sanciones básicas).

### 4.10 WORLD / MAPS / INSTANCES / EVENTS / PvP (P1/P2)
- [ ] **WRLD-001**: Map streaming + colisiones + triggers server-side.
- [ ] **WRLD-002**: Instancias: create/join/complete + lockouts + rewards atómicas.
- [ ] **WRLD-003**: Eventos del sistema: scheduler + flags + rewards idempotentes.
- [ ] **WRLD-004**: PvP (si aplica): reglas, matchmaking básico, rewards consistentes, anti-abuso.

---

## 5) Rotación diaria recomendada
> Objetivo: progreso constante sin dispersión ni churn.

- **Día 1 (Core/Arch)**: tomar próximo `CORE-*` o `PERS-*` que desbloquee features.
- **Día 2 (Security)**: tomar próximo `CORE-*` o `ECO-*` enfocado en anti-dupe/anti-replay.
- **Día 3 (Reliability/Persistence)**: tomar próximo `PERS-*` o `ECO-*` transaccional.
- **Día 4 (Skills)**: tomar próximo `SKL-*` (ver `docs/skills/ROADMAP.md`).
- **Día 5 (Quests/Pets)**: tomar próximo `QST-*` o `PET-*` (ver roadmaps específicos).

---

## 6) Regla anti-churn (muy importante)
Un PR se rechaza si:
- Cambia nombres/estructura sin valor funcional,
- Introduce refactor masivo sin tests,
- Agrega features sin validación server-side,
- No respeta idempotencia en operaciones valiosas,
- No se puede mapear a un ítem de este ROADMAP (o de los sub-roadmaps).

---

## 7) Cómo agregar nuevos ítems al ROADMAP
Formato:
- `DOM-###`: título corto, prioridad (P0..P3), criterio de aceptación (3–6 bullets), riesgos.
- No duplicar. Si ya existe algo cercano, extender ese ítem.
