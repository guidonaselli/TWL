#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Base grayscale texture
sampler TextureSampler : register(s0);

// Map texture with color IDs
texture MapTexture;
sampler MapSampler = sampler_state
{
    Texture = <MapTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Debug mode: 0 = normal, 1 = show map only, 2 = show base only
float DebugMode = 0;

// Color replacement values
float4 ColorPiel = float4(0.992, 0.737, 0.706, 1.0);
float4 ColorRopa = float4(0.420, 0.447, 0.502, 1.0);
float4 ColorPelo = float4(1.0, 0.6, 0.2, 1.0);
float4 ColorExtra = float4(0.290, 0.565, 0.886, 1.0);
float HairMinLum = 0.35;

float4 MainPS(float4 position : SV_POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR
{
    float4 grayPixel = tex2D(TextureSampler, texCoord);
    float4 mapPixel = tex2D(MapSampler, texCoord);

    // Debug mode 1: Show map texture directly
    if (DebugMode > 0.5 && DebugMode < 1.5)
    {
        return mapPixel;
    }

    // Debug mode 2: Show base texture directly
    if (DebugMode > 1.5)
    {
        return grayPixel;
    }

    // Early exit if transparent
    if (grayPixel.a < 0.01)
        return grayPixel;

    // If map is transparent, return base as-is
    if (mapPixel.a < 0.01)
        return grayPixel;

    // Calculate luminance
    float lum = dot(grayPixel.rgb, float3(0.299, 0.587, 0.114));

    // Default to gray
    float3 targetColor = grayPixel.rgb;

    // YELLOW first (two channels high)
    if (mapPixel.r > 0.85 && mapPixel.g > 0.85 && mapPixel.b < 0.1)
    {
        targetColor = ColorExtra.rgb * lum;
    }
    // RED = Skin
    else if (mapPixel.r > 0.85 && mapPixel.g < 0.1 && mapPixel.b < 0.1)
    {
        targetColor = ColorPiel.rgb * lum;
    }
    // GREEN = Clothing
    else if (mapPixel.g > 0.85 && mapPixel.r < 0.1 && mapPixel.b < 0.1)
    {
        targetColor = ColorRopa.rgb * lum;
    }
    // BLUE = Hair
    else if (mapPixel.b > 0.85 && mapPixel.r < 0.1 && mapPixel.g < 0.1)
    {
        lum = max(lum, HairMinLum);
        targetColor = ColorPelo.rgb * lum;
    }

    return float4(targetColor, grayPixel.a * color.a);
}

technique PaletteSwap
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
