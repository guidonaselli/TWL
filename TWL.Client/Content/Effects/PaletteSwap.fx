// Simple palette swap shader for MonoGame
// MapTexture defines regions by solid colors (R,G,B,Y).

texture Texture;
texture MapTexture;

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler MapSampler = sampler_state
{
    Texture = <MapTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 ColorPiel = float4(1, 1, 1, 1);
float4 ColorRopa = float4(1, 1, 1, 1);
float4 ColorPelo = float4(1, 1, 1, 1);
float4 ColorExtra = float4(1, 1, 1, 1); // reserved (e.g. yellow)

float4 PaletteSwapPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 base = tex2D(TextureSampler, texCoord);
    if (base.a <= 0.001)
        return base;

    float4 map = tex2D(MapSampler, texCoord);
    if (map.a <= 0.001)
        return base;

    float3 target = base.rgb;

    // Determine region based on dominant map channel.
    if (map.r > 0.5 && map.g < 0.2 && map.b < 0.2)
        target = ColorPiel.rgb;
    else if (map.g > 0.5 && map.r < 0.2 && map.b < 0.2)
        target = ColorRopa.rgb;
    else if (map.b > 0.5 && map.r < 0.2 && map.g < 0.2)
        target = ColorPelo.rgb;
    else if (map.r > 0.5 && map.g > 0.5)
        target = ColorExtra.rgb;

    // Preserve shading from grayscale base.
    float lum = dot(base.rgb, float3(0.299, 0.587, 0.114));
    float3 rgb = target * lum;

    return float4(rgb, base.a);
}

technique PaletteSwap
{
    pass P0
    {
        PixelShader = compile ps_2_0 PaletteSwapPS();
    }
}
