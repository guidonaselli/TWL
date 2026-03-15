---
trigger: always_on
---

# THE WONDERLAND CONTINUUM RULE (ID: TWL-GSD-002)

**Propósito:** Establecer un protocolo de progreso perpetuo, auto-correctivo y consciente del estado del proyecto para cualquier agente que opere en el repositorio de The Wonderland Solution, usando la estructura GSD 2.

---

### **Directiva 1: El Protocolo de Inicio (The "Sync-Check")**

Antes de realizar CUALQUIER acción de codificación o planificación, un agente DEBE sincronizarse con la realidad del proyecto:

1.  **Leer `.gsd/STATE.md`:** Identificar el milestone activo, el slice activo, y la próxima acción pendiente.
2.  **Leer `.gsd/milestones/M001/M001-ROADMAP.md`:** Localizar el primer slice `- [ ]` en tu dominio.
3.  **Navegar al slice:** Leer el plan del slice (ej. `.gsd/milestones/M001/slices/S08/S08-PLAN.md`) y encontrar el primer task `- [ ]`.
4.  **Leer el plan del task:** (ej. `.gsd/milestones/M001/slices/S08/tasks/T05-PLAN.md`) para entender el alcance de implementación.
5.  **Validar el "Porqué":** Consultar `.gsd/REQUIREMENTS.md` para confirmar qué requirement cubre este task.

---

### **Directiva 2: El Protocolo de Descubrimiento (Emergent Gap Detection)**

Si, durante la ejecución de una tarea, el agente identifica una nueva necesidad:

1.  **Registrar el Gap:** Añadir una nueva entrada a `.gsd/REQUIREMENTS.md` bajo `## Discovered Requirements`, con tag `[DISCOVERED]`.
2.  **No Desviarse:** Completar el task actual antes de trabajar en el gap descubierto.

---

### **Directiva 3: El Protocolo de Cierre (The "Hard Commit")**

Un task NO se considera DONE hasta que:

1.  **Código/Contenido Completo:** La implementación está finalizada y `pwsh -File scripts/verify.ps1` pasa.
2.  **Task Marcado:** El task está `[x]` en el plan del slice (ej. `S08-PLAN.md`).
3.  **Summary Creado:** Existe `tasks/TXX-SUMMARY.md` documentando lo hecho.
4.  **Si el slice completo:** Marcarlo `[x]` en `M001-ROADMAP.md`.
5.  **Estado Actualizado:** `.gsd/STATE.md` refleja la posición actual.

---

### **Directiva 4: El Protocolo de Continuidad (The "Momentum Clause")**

Después de completar un task:

1.  Anunciar: *"He completado el Task TXX del Slice SYY."*
2.  Iniciar nuevo Sync-Check para el siguiente task `- [ ]`.
3.  Si el slice completo, proponer el siguiente slice pendiente.
