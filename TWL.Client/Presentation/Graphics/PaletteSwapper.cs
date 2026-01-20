using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TWL.Client.Presentation.Models;

namespace TWL.Client.Presentation.Graphics
{
    public static class PaletteSwapper
    {
        // Reference colors from source sprites
        static readonly Color SkinSrc = new(140,141,87);
        static readonly Color HairSrc = new(74,40,55);
        static readonly Color EyeSrc  = new(155,61,31);
        static readonly Color ClothSrc = new(100,100,100); // Placeholder grey for base clothes

        public static Texture2D Swap(
            Texture2D original,
            PlayerColors colors,
            GraphicsDevice gd)
        {
            var data = new Color[original.Width * original.Height];
            original.GetData(data);
            for(int i=0;i<data.Length;i++)
            {
                var c = data[i];
                if (c.A==0) continue;

                if (AreSimilar(c, SkinSrc)) data[i] = ApplyTint(c, SkinSrc, colors.Skin);
                else if (AreSimilar(c, HairSrc)) data[i] = ApplyTint(c, HairSrc, colors.Hair);
                else if (AreSimilar(c, EyeSrc))  data[i] = ApplyTint(c, EyeSrc, colors.Eye);
                else if (AreSimilar(c, ClothSrc)) data[i] = ApplyTint(c, ClothSrc, colors.Cloth);
            }
            var tex = new Texture2D(gd, original.Width, original.Height);
            tex.SetData(data);
            return tex;
        }

        static bool AreSimilar(Color a, Color b, int tol=10)
            => Math.Abs(a.R-b.R)<=tol
               && Math.Abs(a.G-b.G)<=tol
               && Math.Abs(a.B-b.B)<=tol;

        static Color ApplyTint(Color original, Color sourceRef, Color target)
        {
             // Calculate relative intensity of the original pixel vs the reference source color.
             // This preserves shading (e.g. shadows stay dark).

             // Intensity formula: 0.299R + 0.587G + 0.114B (Standard Rec. 601)
             // Simplified for performance: Average (R+G+B)/3 or just Green channel.
             // Using average for safety.

             float srcLum = (sourceRef.R + sourceRef.G + sourceRef.B) / 3f;
             if (srcLum < 1f) srcLum = 1f; // avoid div by zero

             float originalLum = (original.R + original.G + original.B) / 3f;
             float factor = originalLum / srcLum;

             return new Color(
                 (int)Math.Clamp(target.R * factor, 0, 255),
                 (int)Math.Clamp(target.G * factor, 0, 255),
                 (int)Math.Clamp(target.B * factor, 0, 255),
                 original.A
             );
        }
    }
}