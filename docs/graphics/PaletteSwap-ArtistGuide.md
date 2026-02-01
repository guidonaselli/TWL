# Palette Swap - Guía para Artistas

## Introducción

Esta guía explica cómo crear sprites para el sistema de palette swap del juego. Si eres el artista del proyecto, este documento es tu referencia principal.

### ¿Qué es Palette Swapping?

En lugar de crear un sprite diferente para cada variante de color (goblin verde, goblin rojo, etc.), creas:

1. **Una textura BASE** en escala de grises (blanco/negro/grises)
2. **Un MAPA de colores** que indica qué región es qué (piel = rojo, ropa = verde, etc.)

El juego usa un shader que combina ambas texturas para generar el sprite final con colores personalizados.

### Ventajas para Ti

- ✅ Creas 1 sprite → obtienes infinitas variantes de color
- ✅ No necesitas redibujar para cada color
- ✅ Los colores se ajustan en el código, no en Photoshop
- ✅ Mantiene la consistencia de sombras automáticamente

---

## Pipeline de Trabajo

### Proceso Completo

```
1. Dibujar sprite en COLOR (como normalmente lo harías)
        ↓
2. Convertir a ESCALA DE GRISES → Guardar como "_base.png"
        ↓
3. Crear MAPA DE COLORES planos → Guardar como "_mapa.png"
        ↓
4. Importar ambos al juego
        ↓
5. El shader combina ambos en tiempo real
```

### Ejemplo Visual

**Sprite Original (Color):**
```
🧍 Personaje con:
- Piel beige
- Pelo naranja
- Camisa azul
- Ojos azules
```

↓

**Textura BASE (_base.png):**
```
🧍 Misma pose pero en:
- Escala de grises pura (R=G=B)
- Sombras preservadas
- Highlights preservados
- Fondo transparente
```

↓

**Mapa de Colores (_mapa.png):**
```
🧍 Silueta con colores planos (SIN sombras):
- Piel → Rojo puro (255, 0, 0)
- Pelo → Azul puro (0, 0, 255)
- Camisa → Verde puro (0, 255, 0)
- Ojos → Amarillo puro (255, 255, 0)
- Resto → Transparente
```

↓

**Resultado en Juego:**
```
El shader toma:
- Los grises de BASE (para sombras)
- Los colores del MAPA (para saber qué es qué)
- Los colores del código (ej: pelo naranja)

= Sprite con pelo naranja + sombras correctas
```

---

## Crear la Textura BASE

### Paso a Paso en Photoshop

#### 1. Dibuja tu sprite normalmente (en color)

- Usa tus colores habituales
- Añade sombras y highlights
- Trabaja con las capas que prefieras

#### 2. Convierte a escala de grises

**Método A - Desaturar (Recomendado):**
```
Image → Adjustments → Desaturate (Shift+Ctrl+U)
```

**Método B - Modo Grayscale:**
```
Image → Mode → Grayscale
Image → Mode → RGB Color (volver a RGB para exportar)
```

**IMPORTANTE:** Usa Desaturate, NO solo quitar saturación. Debe ser R=G=B en TODOS los píxeles.

#### 3. Ajusta las sombras si es necesario

- Asegúrate de que haya buen contraste
- Sombras oscuras = grises oscuros (ej: RGB 40, 40, 40)
- Highlights = grises claros (ej: RGB 220, 220, 220)
- Nunca negro puro ni blanco puro

**Regla de oro:**
```
Píxel muy oscuro → RGB(30, 30, 30) ✅
Píxel muy claro → RGB(240, 240, 240) ✅
Negro puro → RGB(0, 0, 0) ❌ (demasiado oscuro)
Blanco puro → RGB(255, 255, 255) ❌ (se ve plano)
```

#### 4. Guarda como PNG con transparencia

```
File → Export → Export As...
Formato: PNG
☑ Transparency
Nombre: [direccion]_cuerpo_base.png
```

### Ejemplo de Nombres

```
abajo_cuerpo_base.png    // Cuerpo mirando hacia abajo
abajo_pelo_base.png      // Pelo mirando hacia abajo
arriba_cuerpo_base.png   // Cuerpo mirando hacia arriba
arriba_pelo_base.png     // Pelo mirando hacia arriba
lateral_cuerpo_base.png  // Cuerpo de lado (voltear en código)
lateral_pelo_base.png    // Pelo de lado
```

### Verificación Rápida

**✅ Textura BASE correcta:**
- Todo en escala de grises (no hay color)
- Sombras y highlights visibles
- Fondo transparente
- Misma pose que el sprite original

**❌ Errores comunes:**
- Tiene color residual (R≠G≠B)
- Demasiado plano (sin suficiente contraste)
- Sombras en lugares incorrectos

---

## Crear el Mapa de Colores

### Colores Exactos a Usar

| Región | Color | Código RGB | Código Hex |
|--------|-------|------------|------------|
| **Piel** | 🔴 Rojo puro | (255, 0, 0) | #FF0000 |
| **Ropa** | 🟢 Verde puro | (0, 255, 0) | #00FF00 |
| **Pelo** | 🔵 Azul puro | (0, 0, 255) | #0000FF |
| **Ojos/Extra** | 🟡 Amarillo puro | (255, 255, 0) | #FFFF00 |
| **Sin colorear** | ⚪ Transparente | (0, 0, 0, 0) | Transparente |

**⚠️ CRÍTICO:** Deben ser colores EXACTOS. Nada de RGB(254, 1, 0) o RGB(255, 5, 0).

### Paso a Paso en Photoshop

#### 1. Duplica tu sprite original

```
Capa original → Duplicate Layer
Oculta la original (por si acaso)
```

#### 2. Crea una nueva capa para cada región

```
Layer → New → Layer (Ctrl+Shift+N)

Tendrás capas como:
- Capa Piel (rojo)
- Capa Ropa (verde)
- Capa Pelo (azul)
- Capa Ojos (amarillo)
```

#### 3. Pinta cada región con el color correspondiente

**Para PIEL:**
```
1. Selecciona la región de piel con Magic Wand/Lasso
2. Fill con RGB(255, 0, 0) EXACTO
3. Elimina anti-aliasing (ver más abajo)
```

**Para ROPA:**
```
1. Selecciona la ropa
2. Fill con RGB(0, 255, 0) EXACTO
```

**Para PELO:**
```
1. Selecciona el pelo
2. Fill con RGB(0, 0, 255) EXACTO
```

**Para OJOS:**
```
1. Selecciona los ojos
2. Fill con RGB(255, 255, 0) EXACTO
```

#### 4. IMPORTANTE: Elimina anti-aliasing

El mapa NO debe tener gradientes ni píxeles semi-transparentes.

```
Select → Modify → Contract (2px)
Select → Modify → Expand (2px)
Fill con color puro de nuevo
```

Alternativamente, usa el Pencil Tool (no Brush) que no tiene anti-aliasing.

#### 5. Elimina TODAS las sombras

El mapa debe ser **colores planos**:

```
❌ MAL:  Piel con sombras → rojo oscuro en sombras
✅ BIEN: Toda la piel → mismo rojo puro
```

**Tip:** Usa Fill Layers con colores sólidos:
```
Layer → New Fill Layer → Solid Color
Color: #FF0000
Mask: Shape de la región
```

#### 6. Verifica en el Color Picker

Haz clic en cada región y verifica en el Color Picker:

```
Piel → R:255 G:0 B:0 ✅
Ropa → R:0 G:255 B:0 ✅
Pelo → R:0 G:0 B:255 ✅

Piel → R:253 G:2 B:1 ❌ (No es puro, rehacer)
```

#### 7. Exporta como PNG

```
File → Export → Export As...
Formato: PNG
☑ Transparency
☐ Desactivar compresión (si está disponible)
Nombre: [direccion]_cuerpo_mapa.png
```

### Diagrama de Capas Recomendado

```
📁 Archivo Photoshop
  ├─ 📄 Capa Ojos (amarillo #FFFF00)
  ├─ 📄 Capa Pelo (azul #0000FF)
  ├─ 📄 Capa Piel (rojo #FF0000)
  ├─ 📄 Capa Ropa (verde #00FF00)
  └─ 🔒 Capa Guía (sprite original, oculta)
```

### Verificación del Mapa

**✅ Mapa correcto:**
- Colores EXACTAMENTE puros (255,0,0 / 0,255,0 / 0,0,255 / 255,255,0)
- Sin gradientes, sin sombras
- Sin anti-aliasing (bordes nítidos)
- Misma pose y tamaño que BASE
- Fondo transparente

**❌ Errores comunes:**
- Colores "casi puros" (254,1,0 en lugar de 255,0,0)
- Anti-aliasing en bordes (píxeles semi-transparentes)
- Sombras en el mapa (debe ser plano)
- Tamaño diferente a BASE

---

## Aseprite

Si usas Aseprite en lugar de Photoshop:

### Crear BASE

```
1. Dibuja normalmente
2. Sprite → Color Mode → Grayscale
3. Verifica que todo sea gris
4. File → Export → PNG
```

### Crear MAPA

```
1. Crea nueva capa
2. Usa Pencil tool (no Brush - evita anti-aliasing)
3. Selecciona el color exacto:
   - Edit → Keyboard Shortcuts → "Palette"
   - Añade colores personalizados:
     - #FF0000 (rojo)
     - #00FF00 (verde)
     - #0000FF (azul)
     - #FFFF00 (amarillo)
4. Pinta las regiones (sin sombras)
5. File → Export → PNG
```

**Tip de Aseprite:**
```
Preferences → Experimental → Pixel Perfect
(Ayuda a mantener colores puros)
```

---

## Convención de Nomenclatura

### Estructura Requerida

```
[direccion]_[parte]_[tipo].png

direccion: abajo | arriba | lateral
parte:     cuerpo | pelo
tipo:      base | mapa
```

### Ejemplos Correctos

```
✅ abajo_cuerpo_base.png
✅ abajo_cuerpo_mapa.png
✅ arriba_pelo_base.png
✅ lateral_pelo_mapa.png
```

### Ejemplos Incorrectos

```
❌ body_down_base.png      (inglés, usa español)
❌ abajo_base_cuerpo.png   (orden incorrecto)
❌ cuerpo_abajo.png        (falta tipo)
❌ AbajoCuerpoBase.png     (usa minúsculas y guiones)
```

---

## Tamaños y Resoluciones

### Recomendaciones

**Tamaño del sprite:**
- Mínimo: 32×32 px (demasiado pequeño)
- Recomendado: **64×64 px** o **128×128 px**
- Máximo práctico: 256×256 px

**Actualmente en el juego:**
- Tamaño usado: **500×500 px**
- Nota: Es grande, considera optimizar a 128×128 en el futuro

**Regla importante:**
```
BASE y MAPA deben tener EXACTAMENTE el mismo tamaño
```

### Canvas vs. Sprite

```
Canvas de 500×500:
  ┌─────────────────────────┐
  │                         │
  │        🧍‍♂️              │  ← Sprite centrado
  │      (100×200)          │
  │                         │
  └─────────────────────────┘

El canvas grande permite espacio para animaciones
```

---

## Checklist de Calidad

### Antes de Entregar Sprites

#### Textura BASE

- [ ] Está en escala de grises pura (R=G=B en todos los píxeles)
- [ ] Tiene buen contraste (sombras visibles)
- [ ] No tiene color residual
- [ ] Fondo es transparente
- [ ] Tamaño correcto (ej: 500×500)
- [ ] Nombre correcto ([dir]_[parte]_base.png)

#### Mapa de Colores

- [ ] Solo usa los 4 colores permitidos + transparente
- [ ] Colores son EXACTAMENTE puros (255,0,0 / 0,255,0 / etc.)
- [ ] Sin anti-aliasing en los bordes
- [ ] Sin sombras (colores planos)
- [ ] Mismo tamaño que BASE
- [ ] Nombre correcto ([dir]_[parte]_mapa.png)

#### Ambos

- [ ] BASE y MAPA coinciden pixel-por-pixel (misma pose)
- [ ] Probado en el juego (se ve correcto)
- [ ] Exportado como PNG con transparencia
- [ ] Guardado en la carpeta correcta

---

## Troubleshooting

### Problema: El sprite se ve gris en el juego

**Causa:** La textura BASE tiene color en lugar de ser escala de grises.

**Solución:**
```
1. Abre la textura BASE en Photoshop
2. Image → Adjustments → Desaturate
3. Verifica con el Eyedropper que R=G=B
4. Re-exporta
```

**Script automático disponible:**
```bash
python scratchpad/fix_base_textures.py
```

### Problema: Solo el pelo tiene color

**Causa:** El mapa del cuerpo no tiene colores puros.

**Solución:**
```
1. Abre el mapa del cuerpo
2. Verifica cada región con Color Picker
3. Si ves RGB(254,2,1) en lugar de RGB(255,0,0):
   - Select → Color Range → Selecciona ese "rojo"
   - Fill con RGB(255,0,0) EXACTO
4. Re-exporta
```

**Script de verificación:**
```bash
python scratchpad/check_map_colors.py
```

### Problema: Se ven bordes raros (artifacts)

**Causa:** Anti-aliasing en el mapa.

**Solución:**
```
1. Abre el mapa
2. Usa Pencil Tool en lugar de Brush
3. O deshabilita anti-aliasing:
   Brush Settings → Hardness: 100%
4. Repinta los bordes
5. Re-exporta
```

### Problema: Los colores no coinciden

**Causa:** BASE y MAPA tienen tamaños diferentes o poses diferentes.

**Solución:**
```
1. Verifica: Image → Image Size
   - BASE: 500×500
   - MAPA: 500×500 ✅
   - Si son diferentes → redimensionar

2. Verifica alineación:
   - Superpón BASE y MAPA en Photoshop
   - Deben coincidir pixel por pixel
```

### Problema: Se ve magenta en el juego

**Causa:** El shader detecta píxeles en el mapa que no son ningún color conocido.

**Solución:**
```
1. Abre el mapa
2. Filter → Pixelate → Mosaic (1px) - para ver píxeles individuales
3. Busca píxeles que no sean exactamente R/G/B/Y
4. Reemplaza con colores puros
5. Re-exporta
```

---

## Workflow Avanzado

### Template de Photoshop

Puedes crear un template para trabajar más rápido:

```
📁 character_template.psd
  ├─ 🔒 GUÍA (no exportar)
  │   └─ Sprite original en color
  │
  ├─ 📁 GRUPO: BASE
  │   ├─ Ajuste: Desaturate
  │   └─ Sprite en escala de grises
  │
  ├─ 📁 GRUPO: MAPA
  │   ├─ Fill Layer: Ojos (#FFFF00)
  │   ├─ Fill Layer: Pelo (#0000FF)
  │   ├─ Fill Layer: Piel (#FF0000)
  │   └─ Fill Layer: Ropa (#00FF00)
  │
  └─ 📄 Fondo transparente
```

**Uso:**
```
1. Pega tu sprite en "GUÍA"
2. Ajusta las máscaras de cada Fill Layer
3. Export → BASE (solo grupo BASE visible)
4. Export → MAPA (solo grupo MAPA visible)
```

### Batch Processing

Si necesitas convertir muchos sprites:

**Photoshop Actions:**
```
1. Window → Actions
2. Create New Action: "Export Palette Swap"
3. Graba:
   - Desaturate
   - Export as [nombre]_base.png
4. Aplica a carpeta completa:
   File → Automate → Batch
```

**Script Python (incluido):**
```bash
python scratchpad/fix_base_textures.py
# Convierte todos los _base.png a grayscale automáticamente
```

---

## Recursos

### Herramientas Recomendadas

- **Photoshop CC:** Lo más completo
- **Aseprite:** Ideal para pixel art
- **GIMP:** Alternativa gratuita a Photoshop
- **Krita:** Buena para ilustración digital

### Scripts Incluidos

```bash
# Verifica que mapas tengan colores puros
python scratchpad/check_map_colors.py

# Verifica que bases sean grayscale
python scratchpad/check_base_grayscale.py

# Convierte bases a grayscale automáticamente
python scratchpad/fix_base_textures.py
```

### Paleta de Colores

Guarda esta paleta en tu software:

```
Palette Swap Colors:
#FF0000 - Rojo (Piel)
#00FF00 - Verde (Ropa)
#0000FF - Azul (Pelo)
#FFFF00 - Amarillo (Ojos)
```

**Archivo .aco (Photoshop):** [Crear si es necesario]

---

## FAQ

### ¿Puedo usar más de 4 colores?

No actualmente. El sistema soporta 4 regiones (R, G, B, Y). Si necesitas más, habla con el programador para implementar un segundo mapa.

### ¿Qué pasa si mi sprite tiene más regiones?

Combina regiones similares. Por ejemplo:
- Piel = cara + brazos + piernas (todo rojo)
- Ropa = camisa + pantalones (todo verde)

### ¿Puedo dejar regiones sin colorear?

Sí. Usa transparente en el mapa. Esas áreas mantendrán su gris de la BASE.

### ¿Las sombras van en BASE o MAPA?

**Solo en BASE.** El mapa debe ser colores planos sin sombras.

### ¿Qué resolución recomiendan?

Para pixel art: 64×64 o 128×128
Para sprites HD: 256×256 o 512×512

Actualmente usamos 500×500 pero podría optimizarse.

### ¿Cómo pruebo mis sprites?

1. Coloca los archivos en la carpeta correcta
2. Compila el juego: `dotnet build`
3. Ejecuta: `dotnet run`
4. Si algo falla, revisa la consola para errores

---

## Contacto

Si tienes dudas sobre el pipeline o encuentras problemas:

- Revisa `PaletteSwap-TechnicalGuide.md` para detalles técnicos
- Usa los scripts de verificación incluidos
- Contacta al programador si necesitas cambios al sistema

**Happy sprite making! 🎨**

---

**Última actualización:** 2026-02-01
**Versión del sistema:** 1.0
