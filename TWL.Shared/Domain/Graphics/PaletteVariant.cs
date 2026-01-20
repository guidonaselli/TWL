namespace TWL.Shared.Domain.Graphics;

public enum PaletteType
{
    Normal,
    Fire,
    Water,
    Earth,
    Wind,
    Dark,
    Light
}

public class PaletteVariant
{
    public PaletteType Type { get; set; }
    public string Name { get; set; }

    // Mapping from source color index (or hex) to target color
    // For now, we'll keep it simple: just a dictionary of replacement colors or similar.
    // In a real scenario, this might refer to a lookup texture or shader parameter.
    public System.Collections.Generic.Dictionary<string, string> ColorReplacements { get; set; } = new();

    public PaletteVariant(PaletteType type, string name)
    {
        Type = type;
        Name = name;
    }
}
