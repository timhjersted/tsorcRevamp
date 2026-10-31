#include "PixelShaderCommon.fxh"
sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float Time, Progress, Opacity, Active, Direction;
float4 PixelGrid;

float4 Moon(float4 v:COLOR0, float2 coords:TEXCOORD0):COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = (uv - 0.5) * 2.0;
    float r = length(p);
    float radius = Progress * 0.7;
    float sphere = saturate((radius - r) * 35.0);
    float n = tex2D(PrimarySampler, uv * 1.7 + float2(Time * 0.035 + Direction * 0.17, -Time * 0.02)).r;
    float f = tex2D(DetailSampler, uv * 2.1 + float2(-Time * 0.02, Time * 0.03)).r;
    float volume = saturate(1.0 - r * r * 2.0);
    float light = saturate(p.x * 1.7 - p.y * 0.35 + volume * 0.7 - 0.18 + n * 0.3);
    float clouds = saturate(n * 0.8 + f * 0.35);
    float limb = saturate(1.0 - abs(r - radius + 0.015) * 35.0) * sphere;
    float halo = saturate((radius + 0.23 - r) * 6.0) * saturate((r - radius) * 18.0);
    float3 body = float3(0.012,0.026,0.055) + float3(0.12,0.3,0.5) * light * clouds;
    body += float3(0.66,0.8,0.94) * light * light * 0.55;
    body += float3(0.08,0.48,0.95) * Active * volume * clouds * 0.65;
    float alpha = (sphere * 0.95 + halo * 0.18) * Opacity;
    float3 color = body * sphere + float3(0.44,0.7,1) * limb * (0.25 + n * 0.4) + float3(0.05,0.24,0.6) * halo * 0.2;
    // radius<=.7, halo ends at.93: every term is zero before the padded quad edge.
    return float4(color * Opacity, alpha);
}
float4 Warning(float4 v:COLOR0, float2 coords:TEXCOORD0):COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = (uv - 0.5) * 2;
    float r = length(p);
    float noise = tex2D(PrimarySampler, uv * 3 + Time * 0.05).r;
    float band = saturate((0.035 - abs(r - 0.889)) * 28) * (0.3 + noise * 0.7);
    float a = band * Opacity * (0.45 + Progress * 0.55);
    return float4(float3(0.25,0.65,1) * a, a);
}
technique DarkmoonMoonlight { pass P0 { PixelShader = compile ps_2_0 Moon(); } }
technique DarkmoonWarning { pass P0 { PixelShader = compile ps_2_0 Warning(); } }
