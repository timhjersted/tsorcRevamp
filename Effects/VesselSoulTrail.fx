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

float2 LocalUV(float2 coords) { return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0)); }

float4 SoulTrailPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.7, 4.3) + float2(-Time * 0.19, Time * 0.04)).r;
    float center = abs(uv.y - 0.5);
    float taper = saturate(uv.x * 4.0) * saturate((1.0 - uv.x) * 1.8);
    float ribbon = saturate(streak * 0.72 + noise * 0.42 - 0.31) * taper;
    float paleSoul = saturate((noise - 0.69) * 3.2) * ribbon * saturate((0.30 - center) * 4.0);
    float core = saturate((0.095 - center) * 10.5) * ribbon * (0.42 + Active * 0.58);
    float3 color = lerp(DarkColor, MidColor, ribbon);
    color = lerp(color, CoreColor, paleSoul + core * 0.58);
    return float4(sampleColor.rgb * color * (ribbon * 0.64 + paleSoul * 0.88 + core),
        sampleColor.a * saturate(ribbon + core) * Opacity * (1.0 - Progress * 0.42));
}

technique VesselSoulTrail
{
    pass SoulTrailPass { PixelShader = compile ps_2_0 SoulTrailPixel(); }
}
