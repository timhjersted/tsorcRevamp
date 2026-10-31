#include "PixelShaderCommon.fxh"
sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float Time, Progress, Opacity, Active, Growth, Fade;
float4 PixelGrid;
float4 Explosion(float4 v:COLOR0, float2 coords:TEXCOORD0):COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = (uv - 0.5) * 2;
    float r = length(p);
    float n = tex2D(PrimarySampler, uv * (2.4 - Progress * 0.8) + float2(Time * 0.025, -Time * 0.04)).r;
    float f = tex2D(DetailSampler, uv * 3.1 + float2(-Time * 0.035, Time * 0.02)).r;
    float grow = Growth;
    float fade = Fade;
    float reach = (0.67 + n * 0.2) * grow;
    float body = saturate((reach-r) * 8) * (0.2 + n * 0.8);
    float core = saturate((0.625 * grow - r) * 18);
    float filament = saturate(1 - abs(n-f) * 12) * body;
    float breakup = saturate((f + fade - 0.55) * 2);
    float late = 1 - Active;
    core *= 1 - late * 0.82;
    float heat = saturate(n * 1.4 + f * 0.3 - 0.18);
    float alpha = saturate(body * 0.65 + core * 0.7) * fade * breakup * Opacity;
    float3 color = float3(0.08,0.32,0.75) * body;
    color += float3(0.72,0.84,0.98) * core * (0.35 + heat * 0.65);
    color += float3(0.25,0.7,1) * filament * 0.6;
    // reach<=.87; full lethal radius=.625 (160px). Noise cannot reach the 256px quad edge.
    return float4(color * alpha, alpha);
}
float4 Ribbons(float4 v:COLOR0, float2 coords:TEXCOORD0):COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = (uv-0.5)*2;
    float r = length(p);
    float2 dir = p / max(r,0.008);
    float2 tangent = float2(-dir.y,dir.x);
    float n = tex2D(PrimarySampler, dir * (0.55 + r * 0.35) + tangent * (0.4-r*0.3) + 0.5 + Time*0.025).r;
    float f = tex2D(DetailSampler, dir * 0.7 - tangent * (0.3-r*0.2) + 0.5 - Time*0.018).r;
    float rays = saturate(1-abs(n-f)*12) * saturate((0.94*Growth-r)*9) * saturate((r-0.2)*4);
    float alpha = rays * Fade * (1-Progress) * Opacity * 0.65;
    float3 color = float3(0.65,0.85,1) + float3(0.2,0.14,0.0)*rays;
    // Outer ribbons have independent erosion/flow and vanish by .94 of the padded quad radius.
    return float4(color*alpha,alpha);
}
technique DarkmoonExplosion
{
    pass P0 { PixelShader = compile ps_2_0 Explosion(); }
    pass P1 { PixelShader = compile ps_2_0 Ribbons(); }
}
