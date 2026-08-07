// QuaraInkGeyser.fx
// Dark ink attack shaders for QuaraHydromancer in Reach ps_2_0

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float4 uSourceRect;

float4 InkGeyser(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float2 noiseUV = c * 2.8 + float2(Time * 0.08, -Time * 0.15);
    float n1 = tex2D(DetailSampler, noiseUV).r;
    float n2 = tex2D(DetailSampler, c * 4.2 + float2(-Time * 0.12, Time * 0.2)).r;

    float boundary = saturate(1.0 - abs(r - 0.72) * 5.0);
    float cloud = saturate(1.0 - r * 1.2) * saturate(n1 * 1.5 + n2 * 0.5 - 0.5);
    float activeCore = Active * saturate(1.0 - r * 1.6) * saturate(n2 * 1.8 - 0.3);

    float body = boundary * 0.8 + cloud * lerp(0.4, 1.0, Active) + activeCore;
    float3 color = lerp(DarkColor, MidColor, cloud);
    color = lerp(color, CoreColor, boundary * 0.7 + activeCore);

    float alpha = saturate(body) * edgeMask * Opacity;
    return float4(v.rgb * color * (1.2 + activeCore * 0.8), alpha) * v.a;
}

float4 InkJet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float edgeY = saturate(pow(1.0 - saturate(abs(p.y)), 1.8));
    float edgeX = saturate(pow(1.0 - saturate(abs(p.x)), 1.4));
    float edgeMask = edgeX * edgeY;

    float n = tex2D(DetailSampler, c * float2(2.8, 3.8) + float2(-Time * 0.4, Time * 0.08)).r;
    float core = saturate(1.0 - abs(p.y) * 4.0) * saturate(n * 2.0 - 0.5);
    float body = saturate(1.0 - abs(p.y) * 1.8) * (0.6 + n * 0.5);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);

    float alpha = saturate(body + core) * edgeMask * Opacity;
    return float4(v.rgb * color * (1.2 + core * 0.8), alpha) * v.a;
}

float4 InkBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));

    float2 noiseUV = c * 3.2 + float2(Time * 0.1, -Time * 0.1);
    float n = tex2D(DetailSampler, noiseUV).r;

    float waveRadius = lerp(0.1, 0.8, Progress);
    float ring = saturate(1.0 - abs(r - waveRadius) * 5.0);
    float blot = saturate(1.0 - r * 1.2) * saturate(n * 2.0 - 0.6) * (1.0 - Progress);

    float body = ring + blot;
    float3 color = lerp(DarkColor, MidColor, blot);
    color = lerp(color, CoreColor, ring);

    float alpha = saturate(body) * edgeMask * Opacity * (1.0 - Progress * 0.5);
    return float4(v.rgb * color * 1.3, alpha) * v.a;
}

technique QuaraInkGeyser { pass P { PixelShader = compile ps_2_0 InkGeyser(); } }
technique QuaraInkJet { pass P { PixelShader = compile ps_2_0 InkJet(); } }
technique QuaraInkBurst { pass P { PixelShader = compile ps_2_0 InkBurst(); } }
