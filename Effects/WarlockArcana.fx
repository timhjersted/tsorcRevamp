// WarlockArcana.fx
// The Warlock's prismatic arcana: blue-violet energy refracted through floating crystal prisms.
// Drawn by WarlockArcanaDraw (Content/Projectiles/Enemy/Warlock/WarlockSpells.cs). Previewed offline with
// .agents/tools/ShaderPreview (recipes "Warlock*").
//
// Every technique is fully procedural: the drawn quad's own texture (T_CloudNoise_Tiled) and the detail texture
// (T_VFX_Noise41) are only noise fields, so no shape depends on a sprite.
// Blend contract: BlendState.AlphaBlend, which in XNA is PREMULTIPLIED, so each return is float4(color, alpha) with
// colour already scaled by coverage. rgb > alpha = light on top of its own occlusion (vfx-shader-tips §43).
// Every shape provably fades out before its quad edge (each technique states where), so no quad edge is ever lit.
//
// Palette (set from C#): MidColor = energy blue-violet, AccentColor = prism lilac, CoreColor = blue-white heat.
// Extra (set from C#, per technique) carries pre-computed rotations and ratios: ps_2_0 has no preshader, so any
// uniform-only maths written here would be re-evaluated for every pixel.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 MidColor;
float3 AccentColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float4 Extra;
float4 PixelGrid;

// ---------------------------------------------------------------------------------------------
// Energy orb: a churning sphere with a white heart, a thin lilac prism rim, and swirl arms turning inside it.
// Square quad, p = uv*2-1. Progress 0..1 = charge (hotter heart, brighter rim). Extra.xy = cos/sin of the swirl
// angle. Quad cutoff: the rim band ends at r = 0.62 + 1/14 = 0.69, inside every edge (r = 1).
// ---------------------------------------------------------------------------------------------
float4 OrbPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float body = saturate((0.58 - r) * 4.0);
    float heart = saturate((0.24 + Progress * 0.10 - r) * 5.0);
    float rim = saturate(1.0 - abs(r - 0.62) * 14.0);

    // The swirl texture is sampled in a frame rotated by Extra, so the arms visibly turn without a per-pixel sincos.
    float2 turned = float2(p.x * Extra.x - p.y * Extra.y, p.x * Extra.y + p.y * Extra.x);
    float swirl = tex2D(DetailSampler, turned * 0.45 + 0.5).r;
    float churn = tex2D(PrimarySampler, p * 0.6 + float2(Time * 0.05, -Time * 0.07)).r;
    float rimLit = rim * (0.35 + churn * 0.8 + Progress * 0.6);

    float alpha = saturate(body * 0.8 + rimLit * 0.6) * Opacity;
    float3 color = MidColor * body * (0.3 + churn * 0.6 + swirl * 0.5)
        + AccentColor * rimLit
        + CoreColor * heart * 1.1;
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Beam: a hot core with two braided strands winding around it. uv.x runs along the beam (0 = origin, 1 = far end),
// uv.y across it. The quad is drawn ~2.4x the 24px collider wide, so the core sits inside the collider and the
// glow spills around it. Progress 1 = live (full heat), 0 = afterglow. Extra.x = quad length / quad height, so the
// noise and the braid keep the same world-space scale on any beam length.
// Quad cutoff: body and strands reach 0 before across = 1; `ends` fades both ends (origin over 4%, far end over 8%).
// ---------------------------------------------------------------------------------------------
float4 BeamPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float centred = (uv.y - 0.5) * 2.0;
    float across = abs(centred);
    float along = uv.x * Extra.x;

    float flow = tex2D(PrimarySampler, float2(along * 0.35 - Time * 1.6, uv.y * 0.8)).r;
    float body = saturate((0.55 - across * (1.0 - flow * 0.35)) * 2.6);
    float core = saturate((0.20 - across) * 6.0) * (0.35 + Progress * 0.65);

    // Two strands on opposite phases of one triangle wave: the braid (a triangle wave is ~5 slots cheaper than sin
    // and reads the same at beam scale). 0.30 amplitude + 1/9 feather stays inside across = 0.42.
    float wave = (abs(frac(along * 0.25 - Time * 2.2) * 2.0 - 1.0) * 2.0 - 1.0) * 0.30;
    float strands = saturate(1.0 - abs(centred - wave) * 9.0) + saturate(1.0 - abs(centred + wave) * 9.0);

    // Both ends fade (origin over 4%, far end over 8%), folded into one coverage factor with Opacity.
    float fade = saturate(uv.x * 25.0 - 0.25) * saturate((1.0 - uv.x) * 12.0 - 0.2) * Opacity;

    float alpha = saturate((body + strands * 0.7) * 0.7 + core) * fade;
    float3 color = MidColor * body * (0.5 + flow * 0.6)
        + AccentColor * strands * 0.9
        + CoreColor * core * 1.2;
    return float4(color * fade, alpha);
}

// ---------------------------------------------------------------------------------------------
// Prism: a faceted lozenge crystal, long axis along +x (its firing end). Drawn 2:1 (e.g. 40x20) and rotated to the
// firing direction. Two facet tones split on a diagonal, a band of light sweeping along it, a hot tip on the firing
// end. Progress 0..1 = flare (lock / about to fire). Extra.x = sweep phase 0..1 (from C#, per prism).
// Quad cutoff: d = 1 is the crystal edge; the edge highlight fades by d = 1.11, i.e. |p.x| <= 0.97, |p.y| <= 0.69.
// The tip glow is centred at x = 0.70 with radius 0.25, so it ends at x = 0.95.
// ---------------------------------------------------------------------------------------------
float4 PrismPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;

    float d = abs(p.x) * 1.15 + abs(p.y) * 1.6;
    float inside = saturate((1.0 - d) * 6.0);
    float edge = saturate(1.0 - abs(1.0 - d) * 9.0);

    // Facets: the upper-forward half catches the light, the rest is in shadow.
    float facet = 0.55 + 0.45 * step(0.0, p.x * 0.35 - p.y);
    float sweep = saturate(1.0 - abs(p.x - (Extra.x * 2.6 - 1.3)) * 4.0);
    float glint = tex2D(DetailSampler, p * 0.5 + float2(Time * 0.08, 0.3)).r;
    float tip = saturate(1.0 - length(p - float2(0.70, 0.0)) * 4.0) * (0.35 + Progress);

    float lit = inside * (facet * (0.45 + glint * 0.5) + sweep * 0.6);
    float alpha = saturate(inside * 0.85 + edge * 0.5 + tip) * Opacity;
    float3 color = AccentColor * lit
        + MidColor * edge * 0.8
        + CoreColor * (tip + sweep * inside * 0.35 + edge * Progress * 0.5);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Vortex (Collapse Point): spiral arms winding inward around a black heart, with a bright rim. Square quad.
// Progress 0..1 = collapse: the disc and rim contract toward the centre. The arms are a noise field sampled in a
// frame twisted by radius (twist 5 rad across the disc) and spun by Time, so they read as arms falling inward.
// Quad cutoff: the disc ends at r = outer + 0.2 <= 0.9 (outer = 0.70 at Progress 0); the rim ends at 0.75.
// ---------------------------------------------------------------------------------------------
float4 VortexPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float outer = 0.70 - Progress * 0.40;
    float disc = saturate((outer - r) * 5.0 + 1.0);

    float twistSin;
    float twistCos;
    sincos(r * 5.0 + Time * 3.0, twistSin, twistCos);
    float2 twisted = float2(p.x * twistCos - p.y * twistSin, p.x * twistSin + p.y * twistCos);
    float rawArms = saturate(tex2D(PrimarySampler, twisted * 0.55 + 0.5).r * 2.4 - 0.9);

    float heart = saturate((0.14 + Progress * 0.08 - r) * 8.0);
    float rim = saturate(1.0 - abs(r - outer * 0.92) * 12.0);
    // Arms brighten toward the centre; the heart itself is coverage with no colour, which draws black.
    float arms = rawArms * disc * (1.0 - heart) * (1.1 - r);

    float alpha = saturate(arms * 0.8 + heart * 0.95 + rim * 0.7) * Opacity;
    float3 color = MidColor * arms * 1.6 + AccentColor * rim * (1.0 + Progress);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Ward ring (Covenant Ward / Bastion): a rune-segmented ellipse lying on the floor, plus strands of light rising
// from inside it while Bastion is on. Quad width = ring diameter x 1.3, floor line at uv.y = FloorV.
// Extra.xy = (2.6, 8.33) / max(formation, 0.05), from C# (the ellipse's uv radii 0.3846 x 0.12, inverted), so the ring
// grows out from its centre as it forms.
// Extra.zw = the curtain's and the rune row's scroll offsets (Time x 0.6 and Time x 0.03, wrapped), computed in C#.
// Active 0..1 = Bastion strength (the curtain). The payout flash is dust, not shader (slot budget).
// Quad cutoff: the ellipse band ends at e = 1.2 -> |x - 0.5| <= 0.3846 * 1.2 = 0.46 and y <= 0.78 + 0.12 * 1.2 = 0.92;
// every strand stops at <= 0.92 of its rise, so it ends by uv.y = 0.07.
// ---------------------------------------------------------------------------------------------
float4 WardPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    const float FloorV = 0.78;

    // Extra.xy = (2.2, 20) / formation from C#: the ellipse's uv radii (0.4545 x 0.05) inverted and grown.
    float2 ellipse = (uv - float2(0.5, FloorV)) * Extra.xy;
    float e = length(ellipse);
    // One slow noise row drives both the strand heights and the rune marks: frac() of it, offset along x, chops the
    // ring into irregular glyph segments without a second texture fetch.
    float rowNoise = tex2D(PrimarySampler, float2(uv.x * 3.0 + Extra.w, 0.2)).r;
    float runes = step(0.35, frac(rowNoise * 5.0 + uv.x * 6.0));
    float glyph = saturate(1.0 - abs(e - 1.0) * 5.0) * (0.35 + runes * 0.65);
    float floorGlow = saturate(1.0 - e);

    // Rise above the floor line, 0 at the floor and 1 at the top; negative below the floor, where the curtain is cut.
    float height = (FloorV - uv.y) * 1.28;
    float insideX = saturate((1.0 - abs(ellipse.x)) * 4.0);
    float strand = tex2D(DetailSampler, float2(uv.x * 14.0, uv.y * 0.5 + Extra.z)).r;
    float reach = 0.30 + rowNoise * 0.62;
    float tipFade = saturate(min((reach - height) * 4.0, height * 12.0));
    float curtain = saturate(strand * 2.4 - 1.3) * insideX * tipFade * Active;

    float alpha = saturate(glyph * 0.8 + floorGlow * 0.45 + curtain * 0.25) * Opacity;
    float3 color = AccentColor * (glyph * (0.7 + Active * 0.4) + curtain)
        + MidColor * floorGlow * 0.7
        + CoreColor * glyph * 0.3;
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Burst: an expanding shock ring breaking into fragments around a fading flash. Square quad, Progress 0..1 = age.
// Quad cutoff: the ring radius tops out at 0.80 with a 0.1 feather (0.90); the flash ends at r = 0.4.
// ---------------------------------------------------------------------------------------------
float4 BurstPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float ringRadius = 0.15 + Progress * 0.65;
    float fragments = saturate(tex2D(PrimarySampler, p * 0.9 + float2(Time * 0.1, 0.0)).r * 2.2 - 0.4);
    float ring = saturate(1.0 - abs(r - ringRadius) * 10.0) * (1.0 - Progress) * fragments;
    float fade = 1.0 - Progress;
    float flash = saturate((0.4 - r) * 3.0) * fade * fade;

    // The flash is light, not a body: low coverage, strong colour, so it never reads as a grey disc over a dark cave.
    float alpha = saturate(ring * 0.9 + flash * 0.35) * Opacity;
    float3 color = AccentColor * ring + MidColor * (ring * 0.5 + flash * 0.9) + CoreColor * flash;
    return float4(color * Opacity, alpha);
}

technique WarlockOrb
{
    pass P0
    {
        PixelShader = compile ps_2_0 OrbPixel();
    }
}

technique WarlockBeam
{
    pass P0
    {
        PixelShader = compile ps_2_0 BeamPixel();
    }
}

technique WarlockPrism
{
    pass P0
    {
        PixelShader = compile ps_2_0 PrismPixel();
    }
}

technique WarlockVortex
{
    pass P0
    {
        PixelShader = compile ps_2_0 VortexPixel();
    }
}

technique WarlockWard
{
    pass P0
    {
        PixelShader = compile ps_2_0 WardPixel();
    }
}

technique WarlockBurst
{
    pass P0
    {
        PixelShader = compile ps_2_0 BurstPixel();
    }
}
