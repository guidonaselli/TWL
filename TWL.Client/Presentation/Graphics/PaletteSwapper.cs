using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TWL.Client.Presentation.Models;

namespace TWL.Client.Presentation.Graphics
{
    public static class PaletteSwapper
    {
        static readonly Color SkinSrc = new(140,141,87);
        static readonly Color HairSrc = new(74,40,55);
        static readonly Color EyeSrc  = new(155,61,31);

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

                if (AreSimilar(c, SkinSrc)) data[i] = colors.Skin;
                else if (AreSimilar(c, HairSrc)) data[i] = colors.Hair;
                else if (AreSimilar(c, EyeSrc))  data[i] = colors.Eye;
            }
            var tex = new Texture2D(gd, original.Width, original.Height);
            tex.SetData(data);
            return tex;
        }

        static bool AreSimilar(Color a, Color b, int tol=10)
            => Math.Abs(a.R-b.R)<=tol
               && Math.Abs(a.G-b.G)<=tol
               && Math.Abs(a.B-b.B)<=tol;
    }
}