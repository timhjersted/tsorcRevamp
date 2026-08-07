sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Halo(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, p * 3.0 + float2(Time * 0.08, -Time * 0.06)).r;
    float turb = tex2D(DetailSampler, p * 1.6 - float2(Time * 0.05, Time * 0.07)).r;

    // Ring radius perturbed by noise so the halo edge crackles instead of being a perfect circle.
    float radius = lerp(0.45, 0.28, Progress) + (n - 0.5) * 0.015;
    float ring = saturate((0.035 - abs(r - radius)) * 30.0) * (0.7 + turb * 0.5);

    float d0 = min(abs(p.x), abs(p.y));
    float d1 = min(abs(p.y - p.x * 0.577), abs(p.y + p.x * 0.577));
    float d2 = min(abs(p.y - p.x * 1.732) * 0.5, abs(p.y + p.x * 1.732) * 0.5);
    float notches = saturate((0.018 - min(d0, min(d1, d2))) * 55.0);
    float sparks = notches * saturate((0.08 - abs(r - radius)) * 14.0);

    // Turbulent outer bloom while active — sells "unstable energy building" instead of a static
    // decal, and gives the enrage warm tint somewhere to actually breathe.
    float bloom = Active * saturate((0.5 - r) * 3.0) * saturate(turb * 1.4 - 0.3) * 0.5;

    float body = saturate(ring * 0.65 + sparks + bloom) * (0.75 + n * 0.25);
    float flash = Progress * saturate((sparks - 0.6) * 2.6);
    float3 warm = lerp(MidColor, float3(1.0, 0.28, 0.12), Active);
    float3 color = lerp(DarkColor, warm, body);
    color = lerp(color, CoreColor, flash);
    return float4(color * (body * 0.82 + flash * 1.5), saturate(body + flash) * Opacity) * v;
}

technique EvilEyeNovaHalo { pass P { PixelShader = compile ps_2_0 Halo(); } }
