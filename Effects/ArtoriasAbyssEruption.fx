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

float4 RiftPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float crack = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * 3.6 + float2(Time * 0.08, -Time * 0.035)).r;
    float footprint = saturate((0.5 - length(float2(centered.x, centered.y * 2.8))) * 10.0);
    float reveal = saturate((Progress + 0.12 - abs(centered.x) * 1.75) * 7.0);
    float fissure = saturate((crack - 0.24 + noise * 0.15) * 3.0) * footprint * reveal;
    float core = saturate((crack - 0.68) * 4.5) * footprint * reveal;
    float3 color = lerp(DarkColor, MidColor, fissure);
    color = lerp(color, CoreColor, core);
    return float4(sampleColor.rgb * color * (fissure * 0.74 + core * 1.45),
        sampleColor.a * saturate(fissure + core) * Opacity);
}

float4 EruptionPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float smoke = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.5, 4.2) + float2(Time * 0.055, -Time * 0.24)).r;
    float x = abs(uv.x - 0.5);
    float rise = saturate((Progress - (1.0 - uv.y) * 0.72) * 6.0);
    float plume = saturate(smoke * 0.68 + noise * 0.46 - 0.39) * rise;
    float exactCore = saturate((0.39 - x) * 6.0) * rise;
    float silverChannel = saturate((0.12 - x) * 8.33) * rise;
    float3 color = lerp(DarkColor, MidColor, plume + exactCore);
    color = lerp(color, CoreColor, silverChannel);
    float intensity = plume * 0.35 + exactCore * 0.68 + silverChannel * 1.35;
    return float4(sampleColor.rgb * color * intensity,
        sampleColor.a * saturate(plume * 0.52 + exactCore + silverChannel) * Opacity);
}

technique ArtoriasGroundRift
{
    pass RiftPass { PixelShader = compile ps_2_0 RiftPixel(); }
}

technique ArtoriasAbyssEruption
{
    pass EruptionPass { PixelShader = compile ps_2_0 EruptionPixel(); }
}
