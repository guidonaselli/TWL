using System.Collections.Generic;

namespace TWL.Shared.Domain.Graphics;

public static class PaletteRegistry
{
    private static readonly Dictionary<PaletteType, PaletteVariant> _variants = new();

    static PaletteRegistry()
    {
        // Default variants
        Register(new PaletteVariant(PaletteType.Normal, "Default"));

        var fire = new PaletteVariant(PaletteType.Fire, "Fire");
        fire.ColorReplacements["#0000FF"] = "#FF0000"; // Example: Blue to Red
        Register(fire);

        var water = new PaletteVariant(PaletteType.Water, "Water");
        water.ColorReplacements["#FF0000"] = "#0000FF"; // Example: Red to Blue
        Register(water);
    }

    public static void Register(PaletteVariant variant)
    {
        _variants[variant.Type] = variant;
    }

    public static PaletteVariant Get(PaletteType type)
    {
        return _variants.TryGetValue(type, out var v) ? v : _variants[PaletteType.Normal];
    }
}
