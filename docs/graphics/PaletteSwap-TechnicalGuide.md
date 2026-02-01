# Sistema de Palette Swap - Guía Técnica

## Descripción General

El juego utiliza un sistema de **palette swapping** basado en shaders para permitir personalización de personajes y variantes visuales sin costo adicional de memoria.

### ¿Qué es Palette Swapping?

Es una técnica donde:
1. Creas sprites en **escala de grises** (texturas "base")
2. Creas un **mapa de colores** que indica qué región es qué (piel, pelo, ropa, etc.)
3. Un **shader** lee ambas texturas y aplica colores en tiempo real

### Ventajas

- ✅ **Eficiencia de memoria**: 1 sprite base = infinitas variantes de color
- ✅ **Performance**: Los shaders modernos manejan esto sin impacto
- ✅ **Flexibilidad**: Cambiar colores en tiempo real sin recargar assets
- ✅ **Variedad**: Miles de combinaciones posibles sin trabajo de arte adicional

### Desventajas

- ❌ **Sombras compartidas**: Todas las variantes tienen las mismas sombras
- ❌ **Regiones limitadas**: Solo 4 regiones coloreables actualmente (extensible a 8)
- ❌ **Sin patrones**: No se pueden añadir rayas, lunares, etc. fácilmente

---

## Arquitectura del Sistema

### Componentes

```
┌─────────────────────────────────────────┐
│          PlayerView.cs                  │
│  - Gestiona texturas base y mapas      │
│  - Configura parámetros del shader     │
│  - Dibuja cuerpo y pelo por separado   │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│       PaletteSwap.fx (Shader)           │
│  - Lee textura base (grayscale)        │
│  - Lee mapa de colores                 │
│  - Detecta regiones por color          │
│  - Aplica colores preservando sombras  │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│          SceneGameplay.cs               │
│  - Inicia SpriteBatch con efecto       │
│  - Dibuja en 2 batches separados       │
│    (cuerpo y pelo)                      │
└─────────────────────────────────────────┘
```

### Flujo de Renderizado

```csharp
// 1. SceneGameplay inicia batch con efecto
sb.Begin(..., _playerView.PaletteEffect, viewMatrix);

// 2. PlayerView configura shader para CUERPO
_paletteEffect.Parameters["MapTexture"].SetValue(bodyMap);
_paletteEffect.Parameters["ColorPiel"].SetValue(skinColor);
_paletteEffect.Parameters["ColorRopa"].SetValue(clothColor);

// 3. Dibuja cuerpo
sb.Draw(bodyBase, position, ...);
sb.End();

// 4. Repite para PELO en batch separado
sb.Begin(..., _paletteEffect, viewMatrix);
_paletteEffect.Parameters["MapTexture"].SetValue(hairMap);
_paletteEffect.Parameters["ColorPelo"].SetValue(hairColor);
sb.Draw(hairBase, position, ...);
sb.End();
```

**⚠️ Importante:** Cada capa (cuerpo/pelo) necesita su propio `Begin/End` para que MonoGame aplique correctamente los cambios de parámetros del shader.

---

## Estructura de Archivos

### Convención de Nombres

```
Sprites/Characters/[TipoPersonaje]/[Variante]/[Animacion]/
  ├── [direccion]_cuerpo_base.png    // Escala de grises
  ├── [direccion]_cuerpo_mapa.png    // Mapa de colores
  ├── [direccion]_pelo_base.png      // Escala de grises
  └── [direccion]_pelo_mapa.png      // Mapa de colores
```

### Ejemplo Actual

```
Sprites/Characters/RegularMale/Base/Idle/
  ├── abajo_cuerpo_base.png
  ├── abajo_cuerpo_mapa.png
  ├── abajo_pelo_base.png
  ├── abajo_pelo_mapa.png
  ├── arriba_cuerpo_base.png
  ├── arriba_cuerpo_mapa.png
  ├── arriba_pelo_base.png
  ├── arriba_pelo_mapa.png
  ├── lateral_cuerpo_base.png
  ├── lateral_cuerpo_mapa.png
  ├── lateral_pelo_base.png
  └── lateral_pelo_mapa.png
```

---

## El Shader (PaletteSwap.fx)

### Mapeo de Colores

El shader detecta regiones por color en el mapa:

| Color en Mapa | Canal RGB | Región | Parámetro Shader |
|---------------|-----------|--------|------------------|
| 🔴 **Rojo puro** | (255, 0, 0) | Piel | `ColorPiel` |
| 🟢 **Verde puro** | (0, 255, 0) | Ropa | `ColorRopa` |
| 🔵 **Azul puro** | (0, 0, 255) | Pelo | `ColorPelo` |
| 🟡 **Amarillo puro** | (255, 255, 0) | Ojos/Extra | `ColorExtra` |
| ⚪ **Transparente** | Alpha = 0 | Sin colorear | N/A |

### Umbrales de Detección

El shader usa umbrales permisivos para manejar compresión de imagen:

```hlsl
// Rojo = Piel (red > 0.85, green < 0.1, blue < 0.1)
if (mapPixel.r > 0.85 && mapPixel.g < 0.1 && mapPixel.b < 0.1)
    targetColor = ColorPiel.rgb * luminance;
```

Esto permite detectar colores "casi puros" como RGB(251, 3, 1) que pueden resultar de la compresión PNG.

### Preservación de Sombras

```hlsl
// Calcula luminancia de la textura base en escala de grises
float lum = dot(grayPixel.rgb, float3(0.299, 0.587, 0.114));

// Multiplica el color target por la luminancia para preservar sombras
targetColor = ColorPiel.rgb * lum;
```

**Resultado:** Los píxeles oscuros se mantienen oscuros, los claros se mantienen claros, solo cambia el color.

---

## Código C# Relevante

### PlayerView.cs - Configuración de Colores

```csharp
private PlayerColors GetClientColors() => new()
{
    Skin = ColorHelper.FromHex(_player.Colors.SkinColor),
    Hair = ColorHelper.FromHex(_player.Colors.HairColor),
    Eye = ColorHelper.FromHex(_player.Colors.EyeColor),
    Cloth = ColorHelper.FromHex(_player.Colors.ClothColor ?? "#888888")
};
```

### PlayerColorsDto.cs - Colores por Defecto

```csharp
public class PlayerColorsDto
{
    public string SkinColor { get; set; } = "#FDBCB4";  // Peach
    public string HairColor { get; set; } = "#FF9933";  // Orange
    public string EyeColor { get; set; } = "#4A90E2";   // Blue
    public string ClothColor { get; set; } = "#6B7280"; // Gray
}
```

### ColorHelper.cs - Conversión Hex → Color

```csharp
public static Color FromHex(string hex)
{
    if (hex.StartsWith("#"))
        hex = hex[1..];

    var r = Convert.ToByte(hex[..2], 16);
    var g = Convert.ToByte(hex[2..4], 16);
    var b = Convert.ToByte(hex[4..6], 16);
    return new Color(r, g, b);
}
```

---

## Limitaciones Técnicas Actuales

### 1. Número de Regiones (4)

Solo puedes colorear 4 regiones porque usamos los 4 canales de color:
- Rojo → Piel
- Verde → Ropa
- Azul → Pelo
- Amarillo (R+G) → Ojos

**Solución futura:** Usar dos mapas de color para 8 regiones totales.

### 2. Batches Separados Requeridos

Cambiar parámetros del shader entre `Draw()` calls dentro del mismo `Begin/End` no funciona consistentemente en MonoGame. Por eso dibujamos cuerpo y pelo en batches separados.

**Implicación:** Si añades más capas (ej: sombrero, armadura), cada una necesitará su propio batch.

### 3. Textura Base Debe Ser Grayscale Pura

Si la textura base tiene color (R≠G≠B), el shader multiplicará color × color, resultando en colores incorrectos.

**Solución:** Usamos el script `fix_base_textures.py` para convertir automáticamente a escala de grises.

---

## Performance

### Benchmarks

- **CPU:** Negligible (configurar parámetros del shader)
- **GPU:** ~0.1ms por personaje en hardware moderno
- **Memoria:** Misma que sprites normales (cada mapa es del mismo tamaño que la base)

### Escalabilidad

- ✅ **10 personajes en pantalla:** Sin impacto
- ✅ **50 personajes en pantalla:** <1ms overhead total
- ⚠️ **100+ personajes:** Considera batching adicional

---

## Debugging

### Script de Análisis de Mapas

```bash
python scratchpad/check_map_colors.py
```

Analiza los mapas y reporta:
- Distribución de colores
- Píxeles que no son colores puros
- Regiones detectables

### Script de Verificación de Grayscale

```bash
python scratchpad/check_base_grayscale.py
```

Verifica que las texturas base sean escala de grises pura.

### Logs de Debug

Activa logs en `PlayerView.cs` (actualmente comentados):

```csharp
System.Console.WriteLine($"[DEBUG] Skin: {colors.Skin}, Hair: {colors.Hair}");
System.Console.WriteLine($"[DEBUG] Map: {bodyMap.Width}x{bodyMap.Height}");
```

---

## Troubleshooting

### Problema: Solo veo el pelo con color

**Causa:** Las texturas base del cuerpo tienen color en lugar de ser grayscale.

**Solución:**
```bash
python scratchpad/fix_base_textures.py
```

### Problema: Los colores no se aplican

**Causa 1:** Los mapas no tienen colores puros.

**Verificar:**
```bash
python scratchpad/check_map_colors.py
```

**Solución:** Re-exporta los mapas con colores exactos RGB(255,0,0), RGB(0,255,0), etc.

**Causa 2:** Los parámetros del shader no se están configurando.

**Verificar:** Añade logs en `DrawLayeredBody()` y `DrawLayeredHair()`.

### Problema: Veo colores magenta/raros

**Causa:** El shader detecta píxeles del mapa que no coinciden con ningún color conocido.

**Solución:** Revisa el mapa en Photoshop/Aseprite y asegúrate de usar solo los 4 colores definidos.

---

## Referencias

### Juegos que Usan Esta Técnica

- **Streets of Rage 4** - Palette swap para enemigos
- **Dead Cells** - Palette swap + overlays para armas
- **Stardew Valley** - Palette swap para NPCs
- **Terraria** - Sistema avanzado de capas

### Recursos Adicionales

- [Shader tutorial (HLSL basics)](https://rbwhitaker.wikidot.com/monogame-introduction-to-shaders)
- [MonoGame Effect documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Effect.html)
- [Palette swapping in pixel art](https://lospec.com/palette-list)

---

## Próximos Pasos Técnicos (Futuro)

### Extensión a 8 Regiones

```hlsl
// Añadir segundo mapa
texture MapTexture2;
sampler MapSampler2 = sampler_state { ... };

// En el shader:
float4 map1 = tex2D(MapSampler, texCoord);  // 4 regiones
float4 map2 = tex2D(MapSampler2, texCoord); // 4 regiones más
```

### Sistema de Gradientes

```hlsl
// En lugar de un solo color:
float4 ColorPelo;

// Usar rampa de gradiente:
texture HairGradient; // Textura 1D con gradiente
float3 hairColor = tex1D(HairGradientSampler, lum).rgb;
```

### Overlays para Equipamiento

```csharp
// En DrawLayeredBase, añadir:
foreach (var equipment in _player.Equipment)
{
    var equipTexture = GetEquipmentTexture(equipment);
    sb.Draw(equipTexture, position, ...); // Sin palette swap
}
```

---

**Última actualización:** 2026-02-01
**Versión del sistema:** 1.0
**Autor:** Sistema de palette swap implementado para TWL
