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

float4 IrisPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;
    float noise = tex2D(DetailSampler, uv * 3.4 + float2(Time * 0.08, -Time * 0.06)).r;
    float irisRadius = lerp(0.82, 0.48, Progress);
    float iris = saturate(1.0 - abs(r - irisRadius) * 12.0);
    float pupil = saturate((0.34 - r) * 8.0);
    float charge = Active * saturate((0.72 - r) * 3.2);
    float3 color = lerp(MidColor, CoreColor, iris * iris + charge * 0.45);
    color = lerp(color, DarkColor, pupil);
    float alpha = saturate(iris * (0.62 + noise * 0.38) + pupil * 0.48 + charge * 0.34) * Opacity;
    return float4(sampleColor.rgb * color * (iris * 1.15 + charge * 0.64), sampleColor.a * alpha);
}

float4 GazeLinePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.2, 3.0) + float2(-Time * 0.16, 0.0)).r;
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 2.0);
    float beam = saturate(streak * 0.72 + noise * 0.3 - 0.34) * taper;
    float3 color = lerp(MidColor, CoreColor, Progress * 0.62);
    return float4(sampleColor.rgb * color * beam * (0.45 + Progress * 0.55), sampleColor.a * beam * Opacity);
}

technique VesselWatcherIris { pass IrisPass { PixelShader = compile ps_2_0 IrisPixel(); } }
technique VesselWatcherLine { pass LinePass { PixelShader = compile ps_2_0 GazeLinePixel(); } }
