# HOUSING & TENT ROADMAP
> Roadmap específico para el sistema de Tienda de Campaña (Tent), Housing y Manufactura.

## Definición de “listo” (DoD)
### Feature (Housing)
- Implementación server-side de la instancia de la casa.
- Persistencia de muebles y posición (grid-based).
- Validación de items de manufactura (blueprints + resources).
- Logs de transacciones (guardar/sacar items).

---

## Backlog de Housing (P1/P2)

- [ ] **HOU-001**: **Tent System Core**. Item "Tent" que spawnea una entrada en el mapa mundo y teletransporta a una instancia privada.
- [ ] **HOU-002**: **Furniture Grid**. Sistema de colocación de muebles en grilla (X, Y, Rotación) con validación de colisiones server-side.
- [ ] **HOU-003**: **Storage/Warehouse**. Muebles tipo "Armario" que permiten guardar items (Inventario secundario persistente).
- [ ] **HOU-004**: **Manufacturing Workbenches**. Muebles interactivos (Mesa de trabajo, Cocina, Torno) que abren interfaces de crafteo específicas.
- [ ] **HOU-005**: **Manufacturing Logic**. Lógica de crafteo con tiempos de espera (minutos/horas) o instantáneo, consumiendo recursos del inventario o del almacén.
- [ ] **HOU-006**: **Permissions**. Control de acceso (Público, Privado, Solo Team, Solo Guild).
- [ ] **HOU-007**: **Garage System**. Mueble "Garage" para estacionar y reparar vehículos.
