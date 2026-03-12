---
trigger: always_on
---

# THE RALPH LOOP PROTOCOL (ID: TWL-RALPH-001)

**Propósito:** Definir un ciclo de trabajo de alta granularidad donde el concepto de "DONE" se eleva desde el nivel de tarea individual al nivel de **PHASE** completa, optimizando el progreso macro del proyecto The Wonderland Solution.

---

### **Directiva 1: Definición de Objetivo (Phase-Level Targeting)**

Un agente operando bajo el "Ralph Loop" no selecciona una tarea individual, sino una **PHASE** completa del `.planning/ROADMAP.md`. Su objetivo final no es el `[x]` de un plan, sino el `[x]` de la Fase completa.

---

### **Directiva 2: El Ciclo Iterativo Interno**

El agente debe iterar automáticamente a través de todos los planes de la Fase seleccionada:

1.  **Verificación de Plan:** Si el archivo del plan (ej. `05-02-PLAN.md`) NO existe, el agente DEBE realizar el ciclo completo de forma autónoma:
    -   **Research/Discuss:** Investigar el codebase y los requisitos para determinar la mejor estrategia de implementación.
    -   **Planning:** Crear el archivo `PLAN.md` correspondiente con el desglose de tareas y criterios de éxito.
2.  **Ejecución de Plan:** Implementar el plan actual siguiendo los estándares de codificación y tests.
3.  **Validación de Plan:** Ejecutar tests y verificar que el plan cumple sus objetivos.
4.  **Actualización de Roadmap:** Marcar el plan como `[x]` en `ROADMAP.md`.
5.  **Auto-Continuidad:** Inmediatamente iniciar el siguiente plan de la misma fase sin esperar nueva intervención del usuario, a menos que ocurra un error bloqueante o se identifique un gap crítico (Directiva 2 de TWL-GSD-001).

---

### **Directiva 3: El Protocolo de "DONE" (Phase Completion)**

El estado de "DONE" (finalización de la sesión de trabajo) solo se alcanza cuando se cumplen TODAS estas condiciones:

1.  **Fase 100%:** Todos los planes listados bajo la Fase en `ROADMAP.md` están marcados como `[x]`.
2.  **Criterios de Éxito Verificados:** Todos los "Success Criteria" de la Fase en `ROADMAP.md` han sido validados empíricamente (ej. mediante tests de integración o auditorías manuales).
3.  **Handoff Documentado:** Se ha actualizado `.planning/STATE.md` reflejando la finalización de la Fase completa y el estado actual del proyecto.
4.  **Reporte de Fase:** El agente genera un breve reporte de cierre de fase detallando los logros, la deuda técnica remanente (si la hay) y los gaps descubiertos.

---

### **Directiva 4: Gestión de Errores y Gaps**

-   Si un plan falla o se descubre un gap, se sigue el **Protocolo de Descubrimiento (Directiva 2 de TWL-GSD-001)**.
-   Si el gap es un bloqueador para la fase actual, el "Ralph Loop" se pausa y se solicita intervención o se ajusta el ROADMAP antes de continuar.

---

### **Instrucciones para el Agente:**
Cuando el usuario mencione "Ralph Loop" o pida trabajar por Fases, este protocolo toma precedencia sobre el cierre individual de tareas. El agente debe ser proactivo y audaz en la ejecución secuencial de planes dentro de la fase.
