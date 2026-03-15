---
trigger: always_on
---

# THE SLICE LOOP PROTOCOL (ID: TWL-RALPH-002)

**Propósito:** Definir un ciclo de trabajo donde "DONE" se eleva desde el nivel de task individual al nivel de **SLICE** completo, optimizando el progreso macro del proyecto.

---

### **Directiva 1: Definición de Objetivo (Slice-Level Targeting)**

Un agente operando bajo el Slice Loop selecciona un **SLICE** completo del `.gsd/milestones/M001/M001-ROADMAP.md`. Su objetivo no es marcar un task `[x]`, sino completar el slice entero.

---

### **Directiva 2: El Ciclo Iterativo Interno**

El agente itera automáticamente a través de todos los tasks del slice:

1.  **Leer Plan del Task:** Si el plan del task (ej. `T05-PLAN.md`) no tiene suficiente detalle, enrichir con Purpose, Output, y criterios de aceptación.
2.  **Ejecutar:** Implementar siguiendo las convenciones de código y tests.
3.  **Verificar:** Ejecutar `pwsh -File scripts/verify.ps1`. Todos los tests deben pasar.
4.  **Cerrar Task:** Marcar `[x]` en el plan del slice. Crear `TXX-SUMMARY.md`.
5.  **Auto-Continuidad:** Iniciar el siguiente task `- [ ]` del mismo slice sin esperar intervención.

---

### **Directiva 3: Protocolo de "DONE" (Slice Completion)**

"DONE" solo cuando:

1.  **Slice 100%:** Todos los tasks en el plan del slice están `[x]`.
2.  **Verificación:** `pwsh -File scripts/verify.ps1` pasa limpio.
3.  **Roadmap:** Slice marcado `[x]` en `M001-ROADMAP.md`.
4.  **State:** `.gsd/STATE.md` actualizado con el nuevo slice activo.
5.  **Summary:** Crear `SXX-SUMMARY.md` (opcional pero recomendado para slices grandes).

---

### **Directiva 4: Gestión de Errores**

-   Si un task falla, seguir el Protocolo de Descubrimiento (Directiva 2 de TWL-GSD-002).
-   Si el gap es bloqueante, pausar y reportar en el SUMMARY del task con `status: blocked`.
