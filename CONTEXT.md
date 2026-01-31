# CONTEXT.md

## Proyecto: The Wonderland Legacy (TWL)

**The Wonderland Legacy (TWL)** es un JRPG/MMORPG por turnos inspirado en *Wonderland Online*, con foco en:

* Mundo persistente compartido (en etapas: primero singleplayer / pseudo-online, luego MMO real).
* Combate táctico por turnos, basado en **party** (jugador + pets + aliados).
* Sistema profundo de **stats, equipamiento, rebirth, pets, quests y crafting ligero**.
* Personajes totalmente **personalizables visualmente** (palette swapping + equip visible).
* Arquitectura **clean**: dominio compartido, cliente gráfico y servidor autoritativo.

Este archivo describe el **modelo conceptual del juego**, la **arquitectura de la solución** y ciertas **reglas estructurales** que deben respetarse en todo el código nuevo.

---

## Visión de juego

TWL quiere capturar la sensación de *Wonderland Online*:

* Personaje principal creado por el jugador, con:

  * Elemento (Fuego, Agua, Tierra, Viento, etc.).
  * Distribución de stats (STR, CON, INT, WIS, SPD).
  * Apariencia base (sexo/modelo, colores de piel/pelo/ojos).
* Mundo explorado en mapas 2D basados en **Tiled** (TMX), con:

  * Zonas urbanas, mazmorras, campos abiertos, mapas de eventos.
  * Capa de colisión explícita.
* Sistema de combate por turnos:

  * Orden de turno basado en SPD + modificadores.
  * Skills elementales, físicos, de soporte y control.
  * Party del jugador (jugador + pets + aliados temporales).
* Progressión:

  * Level + EXP + ExpToNextLevel.
  * Rebirth / Renacer (reseteo estratégico con bonus).
  * Oro y moneda premium (**TwlPoints**).
  * Equipo y pets como motores centrales de poder.
* Experiencia estética:

  * HUD tipo *Wonderland Online* (panel de personaje, party, chat, mini-mapa, shortcuts, inventario, etc.).
  * Equip visible, superpuesto al sprite base del personaje.

---

## Arquitectura de la solución

La solución se organiza en tres capas principales:

### 1. TWL.Shared (Dominio y contratos)

Proyecto **puro de dominio** (sin referencias a MonoGame ni a nada gráfico).

Responsabilidades:

* Modelo de dominio:

  * `Character`, `PlayerCharacter`, `PetCharacter`, `NpcCharacter`.
  * `Inventory`, `InventoryItem`, `ItemDefinition`, `EquipmentSlot`, `EquipmentSet`.
  * `Stats`: STR, CON, INT, WIS, SPD, HP/MP máximos, cálculos de daño.
  * Sistema de rebirth, progresión y fórmulas básicas.
* Modelo de combate por turnos:

  * Representación abstracta de turnos, acciones, skills, efectos de estado.
* Modelo de quests y progresión narrativa:

  * Quests, estados de quest, objetivos, recompensas.
* Modelo de economía:

  * Oro, TwlPoints, drops, recompensas de combate.
* Modelo de red:

  * DTOs, eventos de gameplay (BattleStarted, BattleFinished, InventoryUpdate, etc.).
  * Interfaces de comunicación (`INetworkChannel`, `IGameManager`, etc.).

Reglas:

* TWL.Shared **no puede depender** de MonoGame, ContentManager, Texture2D, SpriteBatch, GraphicsDevice ni de ningún tipo gráfico.
* Shared modela **el “qué”** (datos y reglas), nunca el “cómo se dibuja”.

---

### 2. TWL.Client (Presentación y cliente de juego)

Proyecto de **cliente gráfico**, basado en MonoGame.

Responsabilidades:

* Motor de render:

  * Escenas (`SceneBase`, `SceneMainMenu`, `SceneGameplay`, `SceneBattle`, `SceneMarketplace`).
  * Carga de contenido (`ContentManager`, `IAssetLoader`).
  * Render de mapas Tiled (MonoGame.Extended).
  * Cámara 2D, input, UI/HUD.
* Representación visual del jugador:

  * `PlayerView`: responsable de:

    * Cargar sprites base (idle/walk para cada dirección).
    * Aplicar **palette swapping** según colores del jugador.
    * Combinar **sprite base + equip visible**:

      * Capas: cuerpo, pelo, ojos, ropa, arma, accesorios.
      * Orden de dibujo consistente con la dirección.
* Sistema de UI:

  * `UiMainMenu`, `UiGameplay`, ventanas de inventario, equipo, stats, party, chat, etc.
* Servicios locales de datos mientras no exista backend:

  * Carga de JSON (`items.json`, `quests.json`, `playercolors.json`, etc.).
  * `JsonPlayerColorsService` para obtener colores de jugador.
* Lógica de escena:

  * `SceneGameplay`: navegación de mapa, pathfinding, encuentros, UI in-game.
  * `SceneBattle`: visualización del combate por turnos usando los resultados del dominio (cuando el server exista).
  * `EncounterManager`: disparo de batallas (pseudo-random, zonas, triggers).

Reglas:

* El cliente **puede referenciar** TWL.Shared, pero no al revés.
* Cualquier lógica relacionada con texturas, sprite sheets, palette swapping, animaciones o layout de UI debe vivir en TWL.Client (p. ej. `PaletteSwapper`, `PlayerView`).
* El cliente no es dueño del estado “real” del personaje; lo representa a partir de modelos de Shared y datos del server (o simulados por JSON en esta primera etapa).

---

### 3. TWL.Server (Servidor autoritativo – futuro/gradual)

Proyecto que será el **dueño del estado del mundo y de los personajes**.

Responsabilidades:

* Persistencia:

  * Personajes, equip, pets, quests, inventarios, estado de la cuenta, etc.
* Lógica autoritativa de combate:

  * Validar acciones, aplicar efectos, resolver turnos, enviar resultados.
* Lógica de mundo:

  * Instancias de mapas, spawns, triggers de eventos.
* Sincronización:

  * Estado del jugador, posiciones relevantes en el mapa, actualizaciones de inventario y equipo.

En la fase actual, muchas de estas responsabilidades se simulan localmente usando JSON y lógica en el cliente.

---

## Personaje jugador y equipamiento

### PlayerCharacter (Shared)

`PlayerCharacter` vive en TWL.Shared y modela:

* Identidad:

  * `Id`, `Name`, `Element`.
* Progressión:

  * `Level`, `Exp`, `ExpToNextLevel`.
  * Métodos: `GainExp`, `TryLevelUp`, `DoRebirth`.
* Stats:

  * Stats base y stats totales derivados de:

    * Atributos básicos (STR, CON, INT, WIS, SPD).
    * Equipamiento (`Equipment`).
    * Efectos temporales (buffs/debuffs).
* Inventario y equipo:

  * `Inventory`: items consumibles, materiales, etc.
  * `Equipment`:

    * Slots: `Head`, `Body`, `Weapon`, `Boots`, `Accessory1`, `Accessory2`, etc.
    * Cada item de equip aporta:

      * Stats (STR +, CON +, HP +, etc.).
      * Flags/efectos especiales.
* Economía:

  * `Gold`, `TwlPoints`.
* Mascotas:

  * `Pets` como lista de `PetCharacter` con comportamiento similar a personajes.

PlayerCharacter **no sabe nada** de cómo se ve el equip; sólo conoce qué equip está equipado y qué stats aporta.

---

### Apariencia y equip visible (Client)

La apariencia visible surge de:

* **Datos de dominio del personaje** (Shared):

  * Elemento, sexo/modelo base, equipo actualmente equipado.
* **Metadatos visuales de equip** (Client/recursos):

  * Cada item de equip tiene una definición visual (p. ej. `EquipmentVisualDefinition`) que indica:

    * Qué spritesheets o overlays usar para cada dirección.
    * Qué capa ocupa (ropa, casco, arma en mano, etc.).
* **PlayerView** en TWL.Client:

  * Combina:

    * Sprite base del cuerpo (con palette swap aplicado a piel, pelo, ojos).
    * Capas visuales de equip (ropa, casco, arma…).
  * Gestiona:

    * Animación idle/walk/run.
    * Elección de frame según `FacingDirection` y estado (moverse, parado).
    * Paleta modificada por equip si corresponde (por ejemplo, ropa que define sus propios colores).

---

## Datos y contenido

Mientras no haya DB ni servidor, el contenido se carga desde JSON:

* `items.json`: definiciones de items (consumibles, equip, materiales).
* `equipment.json`: metadatos de equip (stats + información visual).
* `quests.json`: definiciones de quests, objetivos, recompensas.
* `playercolors.json`: colores de personalización para jugadores de prueba.
* `monsters.json`: definiciones básicas de enemigos.

Estos JSON se interpretan en el cliente y se mapean a modelos de dominio (Shared) y a modelos visuales (Client) cuando haga falta.

---

## Internacionalización (i18n)

El proyecto está pensado para ser **multi-idioma** (por ejemplo, español/inglés).

Principios:

* Ningún texto de UI debe quedar hardcodeado directamente en el código de presentación.
* Los textos se referencian por **clave simbólica**, estilo i18n en frontend:

  * Ejemplo de clave: `ui.mainmenu.new_game`, `ui.inventory.title`, `msg.battle.victory`.
* La resolución de estas claves se hace mediante un servicio de localización:

  * Ejemplo conceptual:

    ```csharp
    interface ILocalizationService
    {
        string Get(string key, params object[] args);
    }
    ```
  * Implementaciones pueden usar `.resx`, JSON o cualquier otra estrategia.
* El dominio (Shared) **no necesita** texto localizado; como mucho, usar claves que luego el cliente traduce.

Limitaciones:

* El código C# no se “traduce” como tal: lo que se internacionaliza son **strings visibles para el usuario**.
* Las claves de i18n funcionan como **constantes/identificadores**; la traducción se resuelve en tiempo de ejecución según idioma activo.

---

## Escena de gameplay (visión conceptual)

`SceneGameplay` en TWL.Client representa:

* Exploración del mapa:

  * Uso de `TiledMap` y `TiledMapRenderer`.
  * Capa de colisión leída desde el TMX.
* Movimiento:

  * Click en el mapa → conversión a coordenadas de mundo → pathfinding sobre tiles.
  * PlayerCharacter mantiene una cola de tiles (`SetPath`); PlayerView anima el movimiento.
* Encuentros:

  * `EncounterManager` puede disparar batallas según:

    * Zonas peligrosas.
    * Tasa de encuentros.
    * Misiones activas.
* HUD:

  * Barra de vida/MP.
  * Botón/tecla para inventario, equipo, estado, quest log.
  * (A futuro) minimapa, chat, party UI.

El flujo típico:

1. `SceneGameplay.Initialize`:

   * Crea/recibe `PlayerCharacter` del GameClientManager.
   * Crea `PlayerView` y UI.
2. `SceneGameplay.LoadContent`:

   * Carga mapa, assets de player, UI, etc.
3. `SceneGameplay.Update`:

   * Procesa input, pathfinding, encuentros, UI, actualiza PlayerCharacter y PlayerView.
4. `SceneGameplay.Draw`:

   * Dibuja mapa, luego player + equip, luego HUD/UI.

---

## Objetivo global

Este contexto apunta a que todo el código nuevo:

* Refuerce la separación **Shared (dominio) / Client (presentación) / Server (autoridad)**.
* Trate al jugador como una **entidad de dominio rica** (stats, equip, pets, quests, economía) y al cliente sólo como la capa que:

  * Lo dibuja (incluyendo equip visible y palette swapping).
  * Lo controla a través de input y UI.
  * Lo sincroniza con el servidor cuando este esté operativo.
* Prepare desde ahora el terreno para:

  * MMO real.
  * Sistema de equip completo y visible.
  * Internacionalización real con claves i18n.
  * Extensión futura (skills, elementos, instancias de mazmorras, PvP, etc.).

Este archivo debe leerse como la **fuente de verdad conceptual** del proyecto.

Evolución del diseño y refactorizaciones permitidas

El diseño de TWL es iterativo y evolutivo.
Los nombres actuales de clases, métodos y módulos son una foto del estado actual del proyecto, no un contrato inmutable.
Se permiten —y se esperan— refactorizaciones cuando mejoren:

La claridad conceptual del modelo.

La separación de responsabilidades (Client / Shared / Server).

La cohesión y expresividad de las entidades del dominio.

La extensibilidad para nuevas mecánicas (skills, equip avanzado, crafting, pet AI, etc.).

La performance o la mantenibilidad.
