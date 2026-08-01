sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float2 DrawSize;
float2 PrimaryTextureSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
    float mask = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.0, 3.5) + float2(-Time * 0.17, Time * 0.04)).r;
    float taper = saturate(uv.x * 4.5) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(mask * 0.9 + noise * 0.32 - 0.2) * taper;
    float core = body * body;
    float fade = 1.0 - Progress * 0.4;
    float smoke = saturate(body - core) * (0.55 + noise * 0.45);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    float intensity = smoke * 0.35 + body * 0.68 + core * 1.35;
    return float4(sampleColor.rgb * color * intensity * fade,
        sampleColor.a * body * fade * Opacity);
}

technique NitoReaperTrail
{
    pass NitoReaperTrailPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
