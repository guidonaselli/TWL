# Maps Roadmap

## Conventions
- **Map IDs**: Integer, 4 digits.
    - 0xxx: Isla Brisa (Starter Island)
    - 1xxx: Puerto Roca & Surroundings
    - 2xxx: North Island
    - 3xxx: South Island
    - 9xxx: Test Maps

## Initial Release (Puerto Roca)

### Phase 1: Core City & Surroundings
- [x] **1000 - Puerto Roca City**
    - **Status:** Implemented (Skeleton)
    - **Type:** Safe Zone / City
    - **Connections:** 1001 (Port), 1002 (North Gate), 1003 (Mines Entrance)
    - **Entities:** Banker, Merchant, Quest Givers.

- [x] **1001 - Puerto Roca Port**
    - **Status:** Implemented
    - **Type:** Safe Zone / Transition
    - **Connections:** 1000
    - **Entities:** Ship Captain, Sailors.

- [x] **1002 - North Path (Sendero Norte)**
    - **Status:** Implemented (Skeleton)
    - **Type:** Adventure Field
    - **Connections:** 1000
    - **Mobs:** Bandido del Camino (9200), Lobo del Bosque (9202).

- [x] **1003 - Old Mines Entrance**
    - **Status:** Implemented (Skeleton)
    - **Type:** Dungeon Entrance
    - **Connections:** 1000
    - **Mobs:** Cave Bat (9101).

- [x] **1010 - Barrio del Mercado**
    - **Status:** Implemented (Skeleton)
    - **Type:** Safe Zone / City
    - **Connections:** 1000
    - **Entities:** Merchants, Market Guard.

## Region 0: Isla Brisa (Starter Island)

### Phase 1: Crash Site
- [x] **0001 - Playa del Naufragio**
    - **Status:** Implemented
    - **Type:** Safe Zone / Beach
    - **Connections:** 0002 (Costa de Mareas)
    - **Entities:** Capitana Maren, Dr. Calloway.

- [x] **0002 - Costa de Mareas**
    - **Status:** Implemented
    - **Type:** Adventure Field
    - **Connections:** 0001 (Playa del Naufragio), 0003 (Sendero de la Selva)
    - **Mobs:** Coast Crab (2001), Tide Crab (2002), Root Vine (2009).

- [x] **0003 - Sendero de la Selva**
    - **Status:** Implemented
    - **Type:** Adventure Field
    - **Connections:** 0002 (Costa de Mareas)
    - **Mobs:** Rock Monkey (2005), Root Vine (2009).

- [x] **0004 - Cueva del Eco**
    - **Status:** Implemented (Skeleton)
    - **Type:** Mini-Dungeon
    - **Connections:** 0003 (Sendero de la Selva)
    - **Entities:** El Viejo Coral (Planned).

- [x] **0005 - Mirador del Faro**
    - **Status:** Implemented
    - **Type:** Landmark
    - **Connections:** 0002 (Costa de Mareas)
    - **Entities:** Ruined Lighthouse (Placeholder).

- [x] **0006 - Cala Escondida**
    - **Status:** Implemented
    - **Type:** Hidden Cove
    - **Connections:** 0002 (Costa de Mareas)
    - **Entities:** Pearl Resource.

- [x] **0007 - Santuario Olvidado**
    - **Status:** Implemented
    - **Type:** Safe Zone / Landmark
    - **Connections:** 0003 (Sendero de la Selva)
    - **Entities:** Ancient Altar.

## Backlog
- [ ] Starter Island (Tutorial) - Remaining maps (0003-0099)
- [ ] South Island Jungle
