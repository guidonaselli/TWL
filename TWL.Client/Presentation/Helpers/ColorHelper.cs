// Proyecto Cliente

using System;
using Microsoft.Xna.Framework;

namespace TWL.Client.Presentation.Helpers;

public static class ColorHelper
{
    public static Color FromHex(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        var r = Convert.ToByte(hex[0..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);
        return new Color(r, g, b);
    }
}