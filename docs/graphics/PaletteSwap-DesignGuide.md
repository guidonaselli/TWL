# Sistema de Palette Swap - Guía de Diseño y Roadmap

## Evaluación del Sistema Actual

### ✅ Fortalezas

1. **Técnicamente Sólido**
   - Implementación profesional comparable a juegos AA
   - Performance excelente (negligible overhead)
   - Arquitectura limpia y mantenible

2. **Artísticamente Viable**
   - Permite variedad visual sin costo de producción
   - Mantiene consistencia artística (sombras coherentes)
   - Escalable para equipo pequeño de arte

3. **Suficiente para MVP**
   - 4 regiones coloreables = miles de combinaciones
   - Permite personalización de jugador
   - Permite variantes de enemigos/NPCs

### ⚠️ Limitaciones

1. **Artísticas**
   - Todas las variantes comparten sombras
   - No permite patrones complejos (rayas, lunares, etc.)
   - Limitado a 4 regiones actualmente

2. **De Diseño**
   - No funciona para jefes/personajes únicos muy detallados
   - Equipamiento complejo necesita otra solución
   - Sin efectos especiales por región (brillos, partículas)

3. **De Producción**
   - Requiere crear 2 PNGs por sprite (base + mapa)
   - Los artistas necesitan entender el pipeline
   - Testing más complejo (verificar todas las combinaciones)

---

## Comparación con Juegos Similares

### Tu Sistema vs. Benchmarks de la Industria

| Feature | Tu Juego | Stardew Valley | Dead Cells | Terraria | Hades |
|---------|----------|----------------|------------|----------|-------|
| **Técnica Base** | Palette Swap | Palette Swap | Palette + Overlays | Sistema de Capas | Sprites Únicos |
| **Regiones Coloreables** | 4 | 4 | 6 | 8+ | N/A |
| **Equipamiento Visual** | Pendiente | Simple | Avanzado | Muy Avanzado | Único por personaje |
| **Complejidad Arte** | ⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Complejidad Código** | ⭐⭐ | ⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Adecuado para Indies** | ✅ | ✅ | ⚠️ | ❌ | ❌ |

**Conclusión:** Estás en el tier correcto para un indie RPG.

---

## Roadmap de Evolución

### FASE 1: MVP (ACTUAL) ✅

**Estado:** Implementado y funcional

**Características:**
- ✅ Palette swap básico (4 regiones)
- ✅ Cuerpo y pelo separados
- ✅ Soporte para 4 direcciones
- ✅ Colores configurables por jugador

**Capacidades:**
- Crear variantes de enemigos (goblins verde/rojo/azul)
- Personalización básica del jugador
- NPCs con apariencias distintas

**Inversión:**
- Programación: Completa
- Arte por personaje: ~4-8 horas

**Suficiente para:**
- ✅ Vertical slice / demo jugable
- ✅ Pitch a publishers
- ✅ Early Access temprano
- ✅ Primeros 2-3 meses de desarrollo

### FASE 2: BETA (Recomendado para 3-6 meses)

**Objetivo:** Añadir profundidad visual sin explotar el presupuesto

**Características a Implementar:**

#### 2A. Sistema de Overlays Simple
```
Estructura de capas:
1. Cuerpo base (palette swap) ← Ya existe
2. Pelo base (palette swap) ← Ya existe
3. Equipamiento overlay (sprite único) ← NUEVO
```

**Ejemplo práctico:**
```csharp
// Jugador con armadura
Capas renderizadas:
1. Cuerpo (palette swap, piel customizable)
2. Pantalones (palette swap, color customizable)
3. Armadura de cuero (sprite único, marrón fijo)
4. Pelo (palette swap, color customizable)
5. Casco opcional (sprite único)
```

**Inversión:**
- Programación: ~1 semana
- Arte: +2 horas por pieza de equipamiento
- Beneficio: Items visualmente únicos sin sistema complejo

#### 2B. Selector de Personalización

**UI simple de creación de personaje:**
```
┌─────────────────────────────────┐
│   CREACIÓN DE PERSONAJE         │
├─────────────────────────────────┤
│                                 │
│   [Sprite Preview]              │
│                                 │
│   Color de Pelo:                │
│   [●] Naranja  [ ] Rubio        │
│   [ ] Castaño  [ ] Negro        │
│                                 │
│   Color de Piel:                │
│   [●] Claro    [ ] Medio        │
│   [ ] Oscuro   [ ] Bronceado    │
│                                 │
│   Color de Ropa:                │
│   [●] Azul     [ ] Rojo         │
│   [ ] Verde    [ ] Negro        │
│                                 │
│   [Confirmar]                   │
└─────────────────────────────────┘
```

**Inversión:**
- Programación: ~3 días
- Arte: Solo UI elements
- Beneficio: Los jugadores sienten ownership del personaje

#### 2C. Catálogo de Peinados

**En lugar de solo recolorear, permitir cambiar el estilo:**

```csharp
// Assets requeridos:
pelo_corto_base.png + pelo_corto_mapa.png
pelo_largo_base.png + pelo_largo_mapa.png
pelo_spiky_base.png + pelo_spiky_mapa.png
pelo_calvo_base.png + pelo_calvo_mapa.png
// etc... 5-10 estilos

// Cada uno palette swappable
```

**Inversión:**
- Programación: ~2 días (selector de asset)
- Arte: ~2 horas por peinado × 5 estilos = 10 horas
- Beneficio: Variedad visual exponencial

**Combinaciones con 5 peinados:**
```
5 estilos × 5 colores × 4 tonos piel × 3 colores ropa
= 300 combinaciones únicas
```

**Timing sugerido:**
- Implementar después de tener 2 horas de gameplay jugable
- Cuando tengas feedback de playtesters
- Si necesitas más "juice" visual para marketing

### FASE 3: RELEASE 1.0 (Solo si tiene éxito)

**Cuándo implementar:**
- ❌ NO antes de tener 5+ horas de contenido jugable
- ❌ NO antes de tener feedback positivo constante
- ✅ Solo si consigues funding/publisher
- ✅ Solo si el juego está funcionando y vendiendo en EA

**Opciones avanzadas:**

#### 3A. Expansión a 8 Regiones

**Técnica:** Usar dos mapas de color

```hlsl
// Mapa 1 (RGBY): Piel, Pelo, Ropa1, Ojos
// Mapa 2 (RGBY): Ropa2, Accesorios, Detalles, Efectos
```

**Inversión:**
- Programación: ~1 semana
- Arte: +50% tiempo por sprite (crear segundo mapa)
- Beneficio: Armadura multicolor, detalles complejos

#### 3B. Sistema de Gradientes

**Permite pelo/ropa con múltiples tonos:**

```hlsl
// En lugar de ColorPelo = Orange
// Usar gradiente: DarkOrange -> Orange -> LightYellow

texture HairGradientRamp; // Textura 1D con gradiente
// Luminancia del pixel elige posición en el gradiente
```

**Ejemplos visuales:**
- Pelo con highlights naturales
- Metal con brillos/reflejos
- Efectos mágicos con gradientes

**Inversión:**
- Programación: ~2 semanas
- Arte: Crear gradientes por color (1 día)
- Beneficio: Calidad visual AAA

#### 3C. Sistema de Capas Completo (Terraria-like)

**Pipeline de equipping avanzado:**

```
Jugador Renderizado:
├─ Cuerpo Base (palette swap)
├─ Género/Body Type (intercambiable)
├─ Peinado (catálogo, palette swap)
├─ Rostro (ojos/boca/nariz intercambiables)
├─ Ropa Interior (palette swap)
├─ Pantalones (catálogo + palette swap)
├─ Camisa (catálogo + palette swap)
├─ Armadura Pecho (catálogo, sprite único)
├─ Armadura Piernas (catálogo, sprite único)
├─ Botas (catálogo, sprite único)
├─ Guantes (catálogo, sprite único)
├─ Casco (catálogo, sprite único, opcional)
├─ Capa (catálogo, sprite único, opcional)
└─ Accesorios (anillos visibles, etc.)
```

**Inversión:**
- Programación: ~2 meses
- Arte: Masivo (100+ piezas únicas)
- Beneficio: Personalización extrema

**Solo considerar si:**
- El loot visual es tu core gameplay loop
- Tienes presupuesto para artistas dedicados
- El juego ya es exitoso y necesita más contenido

---

## Estrategia de Implementación

### Regla de Oro: MVP First

```
┌────────────────────────────────────────┐
│  PRIORIDAD 1: Gameplay Loop           │
│  - Combate divertido                  │
│  - Progresión satisfactoria           │
│  - 2-3 horas de contenido             │
└────────────────────────────────────────┘
              ↓
┌────────────────────────────────────────┐
│  PRIORIDAD 2: Contenido                │
│  - Más enemigos (usando palette swap) │
│  - Más zonas                           │
│  - Más habilidades/items              │
└────────────────────────────────────────┘
              ↓
┌────────────────────────────────────────┐
│  PRIORIDAD 3: Polish Visual            │
│  - Selector de personalización        │
│  - Overlays de equipamiento           │
│  - Más variantes visuales             │
└────────────────────────────────────────┘
```

### Cuándo NO Expandir el Sistema

❌ **Señales de alerta:**

1. "El combate es aburrido pero al menos se ve bonito"
   - **Problema:** Estás puliendo lo incorrecto
   - **Solución:** Pausa gráficos, arregla gameplay

2. "Solo tengo 1 zona completa pero 20 opciones de peinado"
   - **Problema:** Over-engineering cosmético
   - **Solución:** Más contenido > más opciones

3. "Los playtesters no mencionan la personalización"
   - **Problema:** Nadie lo pide
   - **Solución:** Invierte tiempo donde hay demand

4. "No tengo tiempo para implementar X feature pero voy a añadir más colores"
   - **Problema:** Prioridades desbalanceadas
   - **Solución:** Feature core > cosmético

### Cuándo SÍ Expandir el Sistema

✅ **Señales verdes:**

1. "Playtesters piden más opciones de personalización"
   - Hay demanda real

2. "El gameplay loop está sólido, necesito más juice visual"
   - Fundación lista, safe para polish

3. "Tengo 5+ horas de contenido jugable"
   - Suficiente contenido para justificar cosmético

4. "Conseguí funding/publisher que quiere más polish"
   - Recursos disponibles

---

## Casos de Uso Recomendados

### Con Tu Sistema Actual (Fase 1)

#### ✅ BUEN USO: Variantes de Enemigos

```csharp
// Mismo sprite, 3 dificultades
public class Goblin : Enemy
{
    public static Goblin CreateScout()  // Fácil
    {
        return new Goblin
        {
            SkinColor = "#90EE90",  // Verde claro
            ClothColor = "#8B4513", // Marrón
            Stats = new(HP: 20, ATK: 5)
        };
    }

    public static Goblin CreateWarrior()  // Medio
    {
        return new Goblin
        {
            SkinColor = "#DC143C",  // Rojo
            ClothColor = "#2F4F4F", // Gris oscuro
            Stats = new(HP: 40, ATK: 10)
        };
    }

    public static Goblin CreateShaman()  // Difícil
    {
        return new Goblin
        {
            SkinColor = "#4169E1",  // Azul
            ClothColor = "#9370DB", // Púrpura
            Stats = new(HP: 30, ATK: 15, Magic: true)
        };
    }
}

// Resultado: 3 enemigos distintos, 0 arte adicional
```

#### ✅ BUEN USO: NPCs del Pueblo

```csharp
// Generar villagers aleatorios
public class VillageGenerator
{
    private static readonly string[] SkinTones =
    {
        "#FDBCB4", "#C68642", "#8D5524", "#E8BEAC"
    };

    private static readonly string[] HairColors =
    {
        "#FF9933", "#FFD700", "#8B4513", "#000000"
    };

    private static readonly string[] ClothColors =
    {
        "#4169E1", "#DC143C", "#228B22", "#9370DB"
    };

    public NPC GenerateRandomVillager()
    {
        var random = new Random();
        return new NPC
        {
            Name = NameGenerator.GetRandom(),
            SkinColor = SkinTones[random.Next(SkinTones.Length)],
            HairColor = HairColors[random.Next(HairColors.Length)],
            ClothColor = ClothColors[random.Next(ClothColors.Length)]
        };
        // 4 × 4 × 4 = 64 combinaciones únicas
    }
}
```

#### ❌ MAL USO: Jefe Único con Diseño Complejo

```csharp
// NO HAGAS ESTO
public class DragonBoss : Boss
{
    // Problema: Un dragón necesita:
    // - Escamas (textura única)
    // - Alas (anatomía diferente)
    // - Fuego (efectos de partículas)
    // - Detalles únicos (cicatrices, joyas)

    // Palette swap NO es adecuado aquí
    // Mejor: Sprite único pre-renderizado
}
```

**Solución correcta para jefes:**
```csharp
// Usa sprites únicos para personajes importantes
public class DragonBoss : Boss
{
    // Carga sprite único sin palette swap
    private Texture2D _uniqueSprite;

    public override void Draw(SpriteBatch sb)
    {
        // Sin shader, sprite directo
        sb.Draw(_uniqueSprite, Position, Color.White);
    }
}
```

---

## Guía de Decisión: ¿Qué Sistema Usar?

### Diagrama de Flujo

```
¿Es un personaje genérico? (enemigo común, NPC, jugador)
  ├─ SÍ → Palette Swap ✅
  └─ NO → ¿Es visualmente único/especial?
           ├─ SÍ → Sprite único ✅
           └─ NO → ¿Necesita equipamiento visible?
                    ├─ SÍ → Palette Swap + Overlays (Fase 2) ✅
                    └─ NO → Palette Swap ✅
```

### Matriz de Decisión

| Tipo de Asset | Sistema Recomendado | Razón |
|---------------|---------------------|-------|
| **Jugador** | Palette Swap + Overlays | Personalización importante |
| **Enemigos comunes** | Palette Swap | Variantes baratas |
| **Jefes** | Sprite único | Necesitan destacar |
| **NPCs pueblo** | Palette Swap | Variedad barata |
| **NPCs importantes** | Sprite único | Memorable |
| **Equipamiento básico** | Palette Swap | (Fase 2) |
| **Equipamiento legendario** | Sprite único | Debe verse especial |
| **Efectos mágicos** | Partículas/Sprites | No es palette swappable |

---

## Métricas de Éxito

### KPIs para Validar el Sistema

#### Desarrollo
- ✅ Tiempo de crear variante: < 30 min (solo cambiar colores)
- ✅ Bugs por variante: 0 (sistema centralizado)
- ✅ Memoria por variante: ~0 bytes (compartida)

#### Jugadores (Futuro, Fase 2+)
- 🎯 % que customiza personaje: >70%
- 🎯 Tiempo en pantalla customización: >2 min
- 🎯 Variaciones creadas: >5 combinaciones probadas
- 🎯 Comentarios positivos sobre personalización: >50%

#### Producción
- ✅ Arte reutilizable: 1 base = 10+ variantes
- ✅ Iteración rápida: Cambiar colores sin recompilar assets
- ✅ QA simplificado: Testear 1 base = testear todas las variantes

### Red Flags

❌ **Señales de que el sistema NO está funcionando:**

1. Playtesters confunden variantes (no son visualmente distintas)
   - **Solución:** Colores más contrastantes

2. Artistas se quejan del pipeline
   - **Solución:** Mejorar tooling, scripts de conversión

3. Performance issues con 10+ personajes
   - **Solución:** Batching, optimización de shader

4. Jugadores ignoran la personalización
   - **Solución:** Sistema no agrega valor, simplificar

---

## Checklist de Calidad

### Antes de Añadir Más Features

- [ ] El gameplay loop está completo y divertido
- [ ] Tienes 2+ horas de contenido jugable
- [ ] Has hecho al menos 1 playtest con jugadores reales
- [ ] Los playtesters piden más opciones visuales
- [ ] Tienes tiempo/presupuesto para el arte adicional
- [ ] El sistema actual está libre de bugs críticos

### Antes de Lanzar (Fase 1)

- [x] Palette swap funciona en las 4 direcciones
- [x] Colores por defecto se ven bien
- [x] No hay artifacts visuales (magenta, negro, etc.)
- [x] Performance es aceptable (60 FPS)
- [ ] Al menos 3 combinaciones de color distintas testeadas
- [ ] Documentación para artistas completada

### Antes de Expandir a Fase 2

- [ ] Fase 1 completamente estable
- [ ] Tienes 5+ variantes de enemigos usando el sistema
- [ ] Feedback positivo de playtesters
- [ ] Roadmap de features core está completo al 80%
- [ ] Presupuesto/tiempo disponible para expansión

---

## Conclusión

### Sistema Actual: Perfecto para Indies

Tu implementación actual de palette swap es:
- ✅ Técnicamente sólida
- ✅ Artísticamente viable
- ✅ Económicamente eficiente
- ✅ Suficiente para MVP/Early Access

### No Necesitas Más (Ahora)

Enfócate en:
1. **Gameplay first:** Combate divertido > Gráficos perfectos
2. **Contenido over features:** 10 enemigos simples > 1 enemigo con 100 opciones
3. **Feedback-driven:** Espera a que jugadores pidan más

### Expande Solo Cuando

- ✅ El gameplay está sólido
- ✅ Tienes contenido suficiente
- ✅ Los jugadores lo piden
- ✅ Tienes recursos disponibles

**"Termina el juego, no el engine"**

---

**Última actualización:** 2026-02-01
**Próxima revisión:** Después del primer playtest público
**Owner:** Team Lead / Game Designer
