sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 MiasmaPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float radius = length(centered);
    float smoke = tex2D(PrimarySampler, uv + float2(Time * 0.015, -Time * 0.009)).r;
    float cells = tex2D(DetailSampler, uv * 2.8 + float2(-Time * 0.045, Time * 0.032)).r;
    float veins = tex2D(DetailSampler, uv.yx * 5.6 + float2(Time * 0.09, -Time * 0.055)).r;
    float volume = saturate(smoke * 0.62 + cells * 0.52 - 0.36) * saturate((0.53 - radius) * 7.5);
    float pocket = saturate((0.55 - cells) * 2.4) * volume;
    float vein = saturate((veins - 0.76) * 4.2) * volume;
    float ageFade = 1.0 - Progress * 0.35;
    float3 color = lerp(MidColor, DarkColor, pocket);
    color = lerp(color, CoreColor, vein * 0.42);
    float alpha = volume * ageFade * Opacity;
    return float4(color * (volume * 0.68 + vein * 0.42), alpha) * vertexColor;
}

technique NitoMiasmaVolume
{
    pass Pass1 { PixelShader = compile ps_2_0 MiasmaPixel(); }
}
