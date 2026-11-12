// Blaidd's moonflame: the Moonlight Crescent's flame wall and the Moonrise Slam's ground crest. Adapted from
// DarkmoonLight.fx (same 2px pixel grid, scrolled-noise flow, additive float4(rgb * a, a) output).
// Drawn on one quad standing on the floor (BlaiddMoonflameVisuals.Draw). uv.x runs across the quad, uv.y top (0) to
// floor (1). s0 = T_PerlinNoise_Tiled, sampled twice: a slow body flow and a fast flicker, both scrolling upward.
#include "PixelShaderCommon.fxh"

sampler NoiseSampler : register(s0);

float Time;
float Opacity;      // overall fade 0..1 (spawn / death fade folded in by the caller)
float Lean;         // tongues lean back against travel: uv.x shift per unit of height (signed by the caller)
float Seed;         // per-projectile noise offset so two walls never flicker in step
float3 CoreColor;   // white-hot base
float3 BodyColor;   // main flame colour
float3 RimColor;    // tongue tips
float4 PixelGrid;   // xy = 2px block count across the quad, zw = reciprocal (computed in C#)

// Flame wall: a mound of fire, tallest at the centre, whose outline is broken into tongues by the body noise. Inside
// it: the body (rim colour toward the tips), a brighter core in the lower half and a floor glow that never leaves
// the ground. The flicker noise modulates brightness so it shimmers rather than pulsing as one block.
float4 Flame(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float height = 1.0 - uv.y;
    float x = uv.x + height * Lean;
    float across = abs(x - 0.5) * 2.0;

    float flow = tex2D(NoiseSampler, float2(x * 1.3 + Seed, uv.y * 0.9 + Time * 1.4)).r;
    float flicker = tex2D(NoiseSampler, float2(x * 2.7 + Seed, uv.y * 1.7 + Time * 2.6)).r;

    float mound = 1.0 - across * across;
    float reach = mound * (0.35 + flow * 0.85);
    float body = saturate((reach - height) * 3.0);
    float core = saturate((reach * 0.5 - height) * 4.0);
    float floorGlow = saturate(1.0 - height * 7.0) * saturate(1.0 - across * 1.25);

    float light = (body * (0.45 + flicker * 0.6) + core * (0.5 + flicker * 0.5) + floorGlow * 0.4) * Opacity;
    float3 color = lerp(RimColor, BodyColor, body);
    color = lerp(color, CoreColor, saturate(core + floorGlow * 0.3));
    return float4(color * light, light);
}

technique BlaiddMoonflame
{
    pass P0 { PixelShader = compile ps_2_0 Flame(); }
}
