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

// Packed souls rupturing outward — or, when the caller inverts Progress, imploding inward.
//
// The old version was "a circle with a texture stamped in it" for two concrete reasons: it sampled
// T_VFX_Ex1d (a single fixed shard-burst image) as its interior, and it ringed the result with
// `boundary = 1 - abs(radius - 0.965) * 34`, a hard circular edge right at the quad rim. Both are
// gone. The shell's radius is now noise-driven so its outline is ragged, shards are carved out of
// ring-space noise so no two detonations match, and the whole thing feathers to nothing.
//
// Active = exploding (vs. imploding). Progress = the animation, already inverted by the caller for
// the implode case, so this shader only ever sees "0 = start, 1 = finished".
float4 SoulRupturePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;                 // 0 at the centre, 1.0 at the quad edge
    float2 dir = p / max(len, 0.0005);

    // Ring-space sampling: continuous around the whole shell, no tiling grid, no atan2 seam.
    float n1 = tex2D(PrimarySampler, dir * (0.36 + r * 0.30) + (0.5 + Time * 0.07)).r;
    float n2 = tex2D(DetailSampler, dir * 0.24 + (0.5 - Time * 0.05)).r;
    float grain = saturate(n1 * 0.88 + n2 * 0.62 - 0.24);

    // The shell's radius is itself perturbed, so its outline is torn rather than circular.
    float shellRadius = Progress * 0.92 + (grain - 0.5) * 0.20;
    float shell = saturate(1.0 - abs(r - shellRadius) * (4.2 - Progress * 1.4));
    // Filaments dragged behind the shell, and shards flung along it.
    float veins = saturate(r - shellRadius + 0.55) * saturate(shellRadius - r + 0.05) * grain * 2.2;
    float shards = shell * saturate(grain * 2.2 - 1.05) * (0.5 + Active * 0.7);
    // Core flash, decaying as the rupture completes. The uniform is folded in as a per-pixel bias:
    // a pure-uniform saturate() argument does not compile.
    float flash = saturate((0.30 - r) * 3.6 - Progress * 2.2);

    // Noise-independent cutoff.
    float edge = saturate((1.0 - r) * 2.6);
    edge *= edge;
    shell *= edge;
    veins *= edge;

    float3 color = lerp(DarkColor, MidColor, saturate(veins * 1.3 + shell * 0.7));
    color = lerp(color, CoreColor, saturate(shards * 1.4 + flash * 1.2));

    float alpha = saturate(shell * 0.9 + veins * 0.6 + flash * 0.85) * Opacity;
    return float4(color * (shell * 1.15 + veins * 0.45 + flash * 1.3), alpha);
}

technique VesselSoulRupture
{
    pass SoulRupturePass { PixelShader = compile ps_2_0 SoulRupturePixel(); }
}
