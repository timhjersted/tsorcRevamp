sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 GhostBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float cloud = tex2D(DetailSampler, p * 2.4 + float2(-Time * 0.1, Time * 0.08)).r;

    float ringRadius = lerp(0.05, 0.5, Progress);
    float ringWidth = lerp(0.06, 0.3, Progress);
    float ring = saturate((ringWidth - abs(r - ringRadius)) * 8.0) * (0.6 + cloud * 0.6);
    float wisps = saturate(cloud * 1.6 - 0.55) * saturate((0.55 - r) * 3.0) * (1.0 - Progress * 0.7);
    // NOTE: this old content-pipeline's effect compiler has a bug where saturate() on an argument
    // with zero per-pixel (texcoord-derived) dependency fails to compile at all (a preshader-folding
    // issue - confirmed by bisection, e.g. a bare `saturate(1.0 - Progress * 3.0)` alone will not
    // compile). The fix is to always keep a real per-pixel term (here, r) inside any saturate() call
    // instead of gating purely on uniforms like Progress.
    float flash = saturate((0.16 - r) * 6.0 - max(0.0, Progress * 3.0 - 1.0) * 5.0);

    float body = saturate(ring + wisps * 0.6 + flash);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, flash + ring * 0.3);
    float fade = 1.0 - Progress;
    return float4(color * (body * 0.85 + flash * 1.6), saturate(body + flash) * Opacity * fade) * v;
}

technique EvilEyeGhostBurst { pass P { PixelShader = compile ps_2_0 GhostBurst(); } }
