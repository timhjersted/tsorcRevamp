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

// Inside the gullet — the phase-2 nebula. Fullscreen, drawn at the wall layer: after the interior
// background image and before every tile, NPC, player, projectile and dust, so it tints the world
// without touching the UI or hiding gameplay.
//
// Density profile is the whole point here, per the owner's brief:
//   - the CENTRE stays mostly transparent so the interior background reads through it
//   - the CORNERS and edges carry the weight, on a smooth gradient with no visible boundary
//   - the clouds' inner limit is noise-perturbed, so they reach inward as organic tendrils rather
//     than stopping on a clean circle
// DrawSize carries the screen dimensions purely so the vignette can be aspect-corrected: without
// that the "corners" of a 16:9 screen are nowhere near the corners.
float4 VoidSpacePixel(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float aspect = DrawSize.x / max(DrawSize.y, 1.0);
    float2 centered = uv - 0.5;
    centered.x *= aspect;
    // Normalised so 0 is the screen centre and ~1.0 is a corner, on any aspect ratio.
    float radial = length(centered) / (0.5 * sqrt(aspect * aspect + 1.0));

    // Two layers drifting against each other at different scales and directions: the pair never
    // shares a phase, so there are no repeating scroll lines.
    float flow = tex2D(PrimarySampler, uv * float2(1.35, 1.0) + float2(Time * 0.010, -Time * 0.016)).r;
    float swirl = tex2D(DetailSampler, uv * float2(2.30, 1.7) + float2(-Time * 0.021, Time * 0.009)).r;
    float cloud = saturate(flow * 0.75 + swirl * 0.55 - 0.30);

    // Smooth edge ramp, and the noise pushes its inner limit around so the clouds send tendrils
    // toward the middle instead of ending on a circle.
    float t = saturate((radial - 0.30 + (cloud - 0.5) * 0.34) * 1.55);
    t = t * t * (3.0 - 2.0 * t);          // smoothstep curve — no seam anywhere on the ramp

    // Sparse pink highlights only in the denser outer material.
    float highlight = saturate(cloud * 1.9 - 1.15) * t;

    float3 color = lerp(DarkColor, MidColor, saturate(cloud * 0.85 + t * 0.35));
    color = lerp(color, CoreColor, saturate(highlight * 0.85));

    // Near-transparent at the centre, heavy in the corners.
    float alpha = saturate((0.06 + t * 0.66) * (0.55 + cloud * 0.65) * Opacity);
    // PREMULTIPLIED. Terraria's BlendState.AlphaBlend is (One, InvSrcAlpha), so returning straight
    // colour adds it at full strength even where alpha is ~0 — which would have flooded the whole
    // screen with a flat wash instead of the intended transparent-centre gradient.
    return float4(color * alpha, alpha);
}

technique VesselVoidSpace
{
    pass VoidSpacePass { PixelShader = compile ps_2_0 VoidSpacePixel(); }
}
