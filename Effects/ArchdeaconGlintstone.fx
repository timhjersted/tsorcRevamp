// ArchdeaconGlintstone.fx
// Shader ALTERNATIVE for the Archdeacon's glintstone sorcery (the shipped look is sprites + dust).
// The DEFAULT look since 2026-10-18; /glintlegacy toggles back to the old sprite + dust draws.
// Previewed offline with .agents/tools/ShaderPreview (recipes "Glint*").
//
// Both techniques are fully procedural: the drawn quad's own texture is only a second noise field,
// so the shape never depends on a sprite. Blend contract: BlendState.AlphaBlend, which in XNA is
// PREMULTIPLIED, so each return is float4(color, alpha) with color already scaled by coverage.
// rgb > alpha = light on top of its own occlusion, which is what keeps the blue saturated over a
// bright daytime sky instead of clipping to white the way additive would (vfx-shader-tips §43).
//
// Palette (set from C#): DarkColor = deep azure body, MidColor = glint blue, CoreColor = blue-white.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float4 PixelGrid;

// ---------------------------------------------------------------------------------------------
// Glintstone comet: a blue-white star head with a 4-point flare and a streaming wispy tail.
// Progress = tail length, 0..1 (callers clamp it): 0 is a bare hanging glint, 1 is the full comet.
// Quad is drawn 3:1 (e.g. 96x32) and rotated to the velocity; the head sits at uv (0.80, 0.5) and
// the tail runs toward uv.x = 0. Units below are quad HEIGHTS, so x spans 0..3.
// ---------------------------------------------------------------------------------------------
float4 GlintCometPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 fromHead = float2((uv.x - 0.80) * 3.0, uv.y - 0.5);
    float headDistance = length(fromHead);

    // Head: a tight white-hot core inside a soft halo. The halo reaches 0 at 0.45 heights, inside
    // both the top/bottom edges (0.5) and the right edge (0.60 heights from the head).
    float halo = saturate((0.45 - headDistance) * 2.6);
    halo *= halo;
    float core = saturate((0.13 - headDistance) * 9.0);

    // 4-point glint: |x*y| is ~0 along both axes through the head, so one term draws the cross;
    // it fades out by 0.42 heights from the head (inside the halo, so inside every quad edge).
    float flare = saturate(1.0 - abs(fromHead.x * fromHead.y) * 160.0) * saturate(1.0 - headDistance * 2.4);

    // Tail: `along` is 0 at the head and 1 at the left edge; its width tapers to 0 there, so the tail
    // provably vanishes before the left quad edge. Noise is LOW frequency along the tail and HIGH
    // across it (§45), scrolling away from the head, so it reads as streaming wisps, not puffs.
    float along = saturate((0.80 - uv.x) * 1.25);
    float width = (1.0 - along) * 0.34 * Progress;
    float tailBody = saturate((width - abs(uv.y - 0.5)) * 14.0) * step(uv.x, 0.80);
    float wisp = saturate(tex2D(DetailSampler, float2(along * 1.1 + Time * 1.15, uv.y * 2.7)).r * 1.25 - 0.15);
    float tail = tailBody * wisp * (1.0 - along) * (1.0 - along);

    // Coverage: the halo and tail occlude a little (so the blue holds over sky); core and flare are light.
    float alpha = saturate(halo * 0.55 + tail * 0.95 + core) * Opacity;
    // No DarkColor term: under premultiplied alpha the darker body is carried by alpha, not painted.
    float3 color = MidColor * (tail * 1.10 + halo * 0.75)
        + CoreColor * (core * 1.10 + flare * 0.90);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Great glintstone orb: a churning azure sphere with a blue-white heart, crossed by two tilted orbit
// ribbons with light travelling along them. Square quad; p = uv*2-1 so r = 1 at the quad's edge midpoints.
// ---------------------------------------------------------------------------------------------
float4 GlintOrbPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    // Sphere body (radius 0.52), its interior churned in ORDINARY uv space (§49: volume, not ring space).
    // The 0.35 floor keeps every part of the ball lit, so a dip in the noise never reads as a dark bruise.
    // A soft 0.2-wide feather (full at r = 0.36, zero at r = 0.56) instead of a hard rim plus a separate
    // glow term, which did not fit the slot budget; the ribbons carry the light outside the sphere.
    float body = saturate((0.56 - r) * 5.0);
    float churn = tex2D(PrimarySampler, p * 0.70 + float2(-Time * 0.05, Time * 0.07)).r;
    float heart = saturate((0.30 - r) * 4.0);

    // Orbit ribbons: two circles of radius 0.78 in a rotated, 2.6x vertically squashed space, i.e. tilted
    // ellipses crossing the sphere. Analytic arcs, because ring-space noise can only make spokes (§51).
    // Quad cutoff: the ellipses reach at most 0.78 / 0.95 = 0.82 from the centre, and the 1/14 feather
    // keeps them under 0.9 - inside every quad edge (r = 1) whatever the noise does.
    float2 tiltA = float2(p.x * 0.95 + p.y * 0.30, (p.y * 0.95 - p.x * 0.30) * 2.6);
    float2 tiltB = float2(p.x * 0.95 - p.y * 0.30, (p.y * 0.95 + p.x * 0.30) * 2.6);
    float ribbonA = saturate(1.0 - abs(length(tiltA) - 0.78) * 14.0);
    float ribbonB = saturate(1.0 - abs(length(tiltB) - 0.70) * 14.0);
    // Light travelling along the ribbons: one fast-scrolling sample shared by both.
    float shimmer = tex2D(DetailSampler, p * 0.70 + float2(Time * 0.35, Time * 0.18)).r;
    float ribbons = (ribbonA + ribbonB) * (0.35 + shimmer * 0.90);

    float alpha = saturate(body * 0.85 + ribbons * 0.60) * Opacity;
    float3 color = MidColor * (body * (0.35 + churn * 0.70) + ribbons * 0.80)
        + CoreColor * (heart + ribbons * 0.30);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Glintstone Snare field: a glowing ellipse lying on the floor with a curtain of starlight rising from it.
// Quad is drawn SQUARE (e.g. 120x120) with the floor line at uv.y = FloorV, so the strands have ~6 tiles
// to rise into. Opacity carries the field's fade in / out; Progress pulses it bright the instant it snares.
// ---------------------------------------------------------------------------------------------
float4 GlintSnarePixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    const float FloorV = 0.92;

    // Floor ellipse (radii 0.40 x 0.035 of the quad). Its outer feather ends at x = 0.5 +/- 0.43 and
    // y = 0.92 + 0.04, inside every quad edge.
    float2 fromCentre = (uv - float2(0.5, FloorV)) * float2(2.5, 28.6);
    float ellipse = length(fromCentre);
    float glyph = saturate(1.0 - abs(ellipse - 1.0) * 6.0);
    float floorGlow = saturate(1.0 - ellipse);

    // Curtain: HIGH frequency across, LOW up the field (§45), scrolling upward, so it reads as rising strands.
    // Each strand stops at its own height: `reach` comes from a noise sampled by x only, so the tops are
    // ragged and soft instead of one flat cut-off line. reach <= 0.95 of the 0.92 rise, so every strand
    // ends by uv.y = 0.05, inside the top edge.
    float height = saturate((FloorV - uv.y) * 1.087);
    float spread = saturate((0.40 - abs(uv.x - 0.5)) * 8.0);
    float strand = tex2D(DetailSampler, float2(uv.x * 6.0, uv.y * 0.45 + Time * 0.55)).r;
    float reach = 0.35 + tex2D(PrimarySampler, float2(uv.x * 2.5 + Time * 0.04, 0.3)).r * 0.60;
    float tipFade = saturate((reach - height) * 5.0);
    float curtain = saturate(strand * 2.4 - 1.25) * spread * tipFade * step(uv.y, FloorV);

    // The curtain is light, not a body: low coverage, strong colour (rgb > alpha), so it never reads as dark smudges.
    float alpha = saturate(glyph * 0.80 + floorGlow * 0.35 + curtain * 0.25) * Opacity;
    float3 color = MidColor * (glyph * 0.70 + floorGlow * 0.45 + curtain * 1.10)
        + CoreColor * (glyph * glyph * 0.70 + curtain * (1.0 - height) * 0.40 + Progress * floorGlow * 0.80);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Glintstone Eruption lance: a spike of light bursting up out of the floor, sharp tip at the top.
// Quad is drawn at the lance hitbox (32x264, a tall needle). Opacity carries the fade.
// ---------------------------------------------------------------------------------------------
float4 GlintLancePixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float across = abs(uv.x - 0.5);

    // Half-width grows from 0 at the tip (uv.y = 0.04) to 0.40 at the base, so the spike provably clears
    // the side edges; the base fades over its last 10% so the bottom edge never cuts it flat.
    float halfWidth = saturate(uv.y - 0.04) * 0.42;
    float baseFade = saturate((1.0 - uv.y) * 10.0);
    float spike = saturate((halfWidth - across) * 30.0) * baseFade;
    float core = saturate((halfWidth * 0.35 - across) * 40.0) * baseFade;

    // Upward-streaming streaks: HIGH frequency across, LOW along the lance (§45).
    // 2.0 along the 264px lance keeps the streaks about as long as they were on the old 88px one.
    float streak = tex2D(DetailSampler, float2(uv.x * 2.4, uv.y * 2.0 + Time * 0.9)).r;
    float body = spike * (0.55 + streak * 0.60);

    float alpha = saturate(body * 0.80 + core) * Opacity;
    float3 color = MidColor * (body * 0.95) + CoreColor * (core * 1.10 + spike * streak * 0.25);
    return float4(color * Opacity, alpha);
}

technique ArchdeaconGlintComet
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlintCometPixel();
    }
}

technique ArchdeaconGlintOrb
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlintOrbPixel();
    }
}

technique ArchdeaconGlintSnare
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlintSnarePixel();
    }
}

technique ArchdeaconGlintLance
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlintLancePixel();
    }
}
