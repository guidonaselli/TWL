---
trigger: always_on
---

    1 # THE WONDERLAND CONTINUUM RULE (ID: TWL-GSD-001)
    2
    3 **Propósito:** Establecer un protocolo de progreso perpetuo, auto-correctivo y consciente del estado del proyecto para cualquier agente GSD que opere en el repositorio de The Wonderland Solution.
    4
    5 ---
    6
    7 ### **Directiva 1: El Protocolo de Inicio (The "Sync-Check")**
    8
    9 Antes de realizar CUALQUIER acción de codificación o planificación, un agente DEBE sincronizarse con la realidad del proyecto:
   10
   11 1.  **Leer el `ROADMAP.md` y `STATE.md` en `.planning/`:** Identificar la siguiente tarea/plan `[PENDING]` de la fase actual (ej. `02-02-PLAN.md`).
   12 2.  **Validar el "Porqué":** Abrir `.planning/REQUIREMENTS.md`. Encontrar la sección de `Traceability` o el `Appendix A: Production Gap Analysis` que justifica la tarea actual. El agente debe
      declarar que entiende *por qué* la tarea es necesaria (ej. *"Procedo con el Plan 02-02, que es necesario para cerrar el gap de seguridad SEC-01: prevención de speed-hacks."*).
   13
   14 ---
   15
   16 ### **Directiva 2: El Protocolo de Descubrimiento (Emergent Gap Detection)**
   17
   18 Si, durante la ejecución de una tarea, el agente identifica una nueva necesidad (un test que falta en `TWL.Tests`, una oportunidad de refactor, una vulnerabilidad no contemplada, una feature ausent
      para cumplir el "Core Value"), DEBE seguir este protocolo de expansión:
   19
   20 1.  **Registrar el Gap:** Añadir una nueva línea a `.planning/REQUIREMENTS.md` bajo una sección `## v1.1 Discovered Requirements`, marcándola con `[DISCOVERED]`.
   21 2.  **Proponer la Tarea:** Añadir una nueva entrada de plan (ej. `02-04-PLAN.md`) a la fase correspondiente en `.planning/ROADMAP.md` para abordar este nuevo requisito.
   22 3.  **No Desviarse:** El agente NO debe trabajar en el nuevo gap inmediatamente. Debe completar su tarea actual y luego seguir el orden del `ROADMAP` actualizado.
   23
   24 ---
   25
   26 ### **Directiva 3: El Protocolo de Cierre (The "Hard Commit")**
   27
   28 Una tarea (ej. `02-02-PLAN.md`) NO se considera `[DONE]` y el agente NO puede dar por finalizado su trabajo hasta que se cumplan estas 3 condiciones, en este orden:
   29
   30 1.  **Código Completo:** La implementación está finalizada y los tests relevantes en `TWL.Tests` se ejecutan y pasan.
   31 2.  **Estado Actualizado:** El agente edita `.planning/ROADMAP.md` para marcar el plan como `[x]` (ej. `[x] 02-02-PLAN.md`).
   32 3.  **Estado Global Confirmado:** El agente edita `.planning/STATE.md`, actualizando la sección `last_completed_task` con el nombre del plan recién terminado y la fecha. **Este es el "commit" final
      inmutable de la sesión de trabajo.**
   33
   34 ---
   35
   36 ### **Directiva 4: El Protocolo de Continuidad (The "Momentum Clause")**
   37
   38 Inmediatamente después de completar exitosamente el Protocolo de Cierre, el agente DEBE tomar la iniciativa para mantener el impulso:
   39
   40 1.  **Anunciar Finalización:** Declarar: *"He completado y persistido el estado del Plan [nombre-del-plan]."*
   41 2.  **Proponer Siguiente Acción:** Inmediatamente después, iniciar un nuevo "Sync-Check" (Directiva 1) para la siguiente tarea pendiente en el `ROADMAP` y preguntar: *"La siguiente tarea es        
      [siguiente-plan]. ¿Procedo?"*