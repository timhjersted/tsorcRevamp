sampler SmoothNoise : register(s0);
sampler BrokenNoise : register(s1);

float3 GoldColor;
float3 HotColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float Progress;
float Active;
float Direction;

float2 NormalizedCoordinates(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 GwynJudgmentColumnPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = NormalizedCoordinates(coords);
    float noise = tex2D(BrokenNoise, float2(uv.y * 6.5 + Time * 2.2, Time * 0.27)).r;
    float boltCenter = 0.5 + (noise - 0.5) * 0.31;
    float distanceFromBolt = abs(uv.x - boltCenter);

    float reveal = saturate((Progress + 0.08 - uv.y) * 12.5);
    float telegraph = saturate((0.025 - distanceFromBolt) * 40.0) * reveal
        * (0.3 + Progress * 0.7);

    float aura = saturate((0.12 - distanceFromBolt) * 8.333);
    float body = saturate((0.050 - distanceFromBolt) * 20.0);
    float core = saturate((0.016 - distanceFromBolt) * 62.5);
    float endFade = saturate(uv.y * 35.0) * saturate((1.0 - uv.y) * 24.0);
    float strike = (aura * 0.26 + body * 0.90 + core * 1.65) * endFade;
    float intensity = lerp(telegraph, strike, Active);
    float3 color = lerp(GoldColor, HotColor, saturate(body + telegraph));
    color = lerp(color, CoreColor, core * Active);
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * saturate(intensity) * Opacity);
}

float4 GwynJudgmentGroundPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = NormalizedCoordinates(coords);
    float localX = 0.5 + Direction * (uv.x - 0.5);
    float noise = tex2D(BrokenNoise, float2(localX * 7.0 + Time * 2.8, Time * 0.23)).r;
    float arcHeight = 0.72 - (noise - 0.5) * 0.42;
    float arcDistance = abs(uv.y - arcHeight);
    float aura = saturate((0.18 - arcDistance) * 5.556);
    float arc = saturate((0.055 - arcDistance) * 18.182);
    float core = saturate((0.017 - arcDistance) * 58.824);
    float groundGlow = saturate((0.12 - abs(uv.y - 0.87)) * 8.333)
        * saturate((1.0 - localX) * 6.0);
    float intensity = aura * 0.24 + arc * 0.92 + core * 1.5 + groundGlow * 0.28;
    float3 color = lerp(GoldColor, HotColor, arc);
    color = lerp(color, CoreColor, core);
    return float4(sampleColor.rgb * color * intensity,
        sampleColor.a * saturate(aura * 0.35 + arc + core + groundGlow * 0.3) * Opacity);
}

technique GwynSunlightJudgmentColumn
{
    pass GwynSunlightJudgmentColumnPass
    {
        PixelShader = compile ps_2_0 GwynJudgmentColumnPixel();
    }
}

technique GwynSunlightJudgmentGround
{
    pass GwynSunlightJudgmentGroundPass
    {
        PixelShader = compile ps_2_0 GwynJudgmentGroundPixel();
    }
}