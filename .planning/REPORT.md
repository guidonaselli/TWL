1) TITLE: Implement Server-Side Proximity Validation for Interactions (SEC-001)
2) TYPE: REPORT
3) SCOPE (IN):
- TWL.Server/Features/Interactions/InteractCommand.cs
- TWL.Server/Features/Interactions/InteractHandler.cs
- TWL.Server/Simulation/Networking/ClientSession.cs
- TWL.Server/Services/World/IWorldTriggerService.cs (o similar, para resolver coordenadas por nombre)
- TWL.Tests/Security/SecurityTests.cs (o nuevos tests de interacción)
4) OUT-OF-SCOPE:
- Modificaciones a otros opcodes que no sean InteractRequest.
- Implementación de ReplayGuard Strict Mode (eso pertenece a SEC-002).
- Cambios a la lógica interna de recompensas de quest/interacciones.
5) ACCEPTANCE CRITERIA (DoD):
- El comando `InteractCommand` (o `InteractHandler`) debe resolver las coordenadas (X,Y) de la entidad objetivo (`TargetName`) usando el mapa actual del jugador.
- Se verifica la distancia euclidiana entre el jugador y el objetivo.
- Si la distancia es mayor a `MaxInteractDistance` (ej. 5.0 unidades), se rechaza la interacción y no se procesan reglas ni recompensas.
- Se emite un log de seguridad mediante `SecurityLogger` cuando se detecta un intento de interacción fuera de rango.
6) REQUIRED TESTS / VALIDATIONS:
- Implementar y asegurar que pase el test `SecurityTests.InteractRequest_OutOfRange_ShouldReject`.
- Validar que interacciones dentro de rango sigan funcionando correctamente.
7) RISKS:
- Riesgo: Dificultad para resolver coordenadas de todas las entidades posibles por `TargetName` (ej. triggers vs NPCs móviles). Mitigación: Exponer un método centralizado en `IWorldTriggerService` o `SpawnManager` que busque entidades en el mapa activo.
- Riesgo: Romper interacciones legítimas si `MaxInteractDistance` es muy estricto. Mitigación: Considerar un margen de gracia (ej. 5.0 a 10.0 unidades lógicas).
8) NEXT: Implementar Protocol Schema Validation (CORE-001).
