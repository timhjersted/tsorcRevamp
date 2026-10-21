// OolacileMadness.fx
// The Acolyte of Grand Occultist's Madness kit (Content/Projectiles/Enemy/Acolyte/AcolyteSpells.cs, AcolyteMadnessDraw).
// Previewed offline with .agents/tools/ShaderPreview (recipes "Madness*").
//
// Every technique is procedural: the quad texture (s0) is only a noise field, the detail noise sits in s1.
// Blend contract: BlendState.AlphaBlend, which in XNA is PREMULTIPLIED, so each return is float4(color, alpha) with
// color already scaled by coverage. rgb > alpha = light on top of its own occlusion, which keeps Madness yellow
// saturated over a bright daytime sky instead of clipping to white (vfx-shader-tips §43).
//
// Palette (set from C#): DarkColor = burnt amber (120,78,0), MidColor = Madness yellow (255,220,45), CoreColor = white.
// The Abyss Drop portal reuses MadnessRing with a dark-red palette.
//
// "Frenzied" is driven from C#, not here: the jitter (pixel-snapped quad offsets re-rolled every 6+ frames), the flare
// arm lengths (Extra) and the core flicker (Active) are all re-rolled at <= 10 Hz, local to each projectile, with no
// full-black <-> full-white swing (photosensitivity guard, plan §5).
//
// Uniforms: Active = core-flicker multiplier (~0.75..1.1); Direction = technique-specific switch; Extra = four
// technique-specific scalars, documented per technique. Pure-uniform maths is pre-divided in C# (no preshader at ps_2_0).

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

matrix WorldViewProjection;
float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float4 Extra;
float4 PixelGrid;

// ---------------------------------------------------------------------------------------------
// MadnessMote: a white-hot core in a yellow halo, crossed by a jittering 4-point flare.
// Square quad; p = uv*2-1. Extra = INVERSE arm lengths (+x, -x, +y, -y), each arm <= 0.95 long, so
// every arm provably ends inside the quad edge (r = 1). The halo reaches 0 at r = 0.62.
// ---------------------------------------------------------------------------------------------
float4 MotePixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float halo = saturate((0.62 - r) * 2.2);
    halo *= halo;
    float core = saturate((0.22 - r) * 7.0);

    float inverseX = lerp(Extra.y, Extra.x, step(0.0, p.x));
    float inverseY = lerp(Extra.w, Extra.z, step(0.0, p.y));
    float armX = saturate(1.0 - abs(p.x) * inverseX) * saturate(1.0 - abs(p.y) * 8.0);
    float armY = saturate(1.0 - abs(p.y) * inverseY) * saturate(1.0 - abs(p.x) * 8.0);
    float flare = max(armX, armY);

    float churn = tex2D(DetailSampler, p * 0.6 + Time * 0.31).r;

    float alpha = saturate(halo * 0.5 + core + flare * 0.9) * Opacity;
    float3 color = MidColor * (halo * (0.55 + churn * 0.5) + flare * 1.0)
        + CoreColor * ((core * 1.1 + flare * flare * 0.9) * Active);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// MadnessEye: white sclera, yellow iris, black pupil, red-veined cracks, inside an almond eyelid slit.
// Square quad; p = uv*2-1. Progress = how open the lid is (0 shut .. 1 open).
// Extra.xy = look direction (length <= 1; the iris slides 0.22 toward it), Extra.z = pupil radius (contracts on lock),
// Extra.w = vein strength. Active = lock flash (whitens the iris). The lid's half-height is
// Progress * 0.55 * (1 - x^2 / 0.81), zero at |x| = 0.9, so the almond provably sits inside every quad edge.
// ---------------------------------------------------------------------------------------------
float4 EyePixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;

    float lid = Progress * 0.55 * saturate(1.0 - p.x * p.x * 1.2346);
    float edge = lid - abs(p.y);
    float inside = saturate(edge * 18.0);
    float rim = saturate(1.0 - abs(edge) * 16.0) * saturate((0.9 - abs(p.x)) * 6.0);

    float irisDistance = length(p - Extra.xy * 0.22);
    float iris = saturate((0.32 - irisDistance) * 14.0);
    float pupil = saturate((Extra.z - irisDistance) * 18.0);

    // Veins: the iso-line of a noise field (vfx-shader-tips §51e), kept off the iris.
    float veinNoise = tex2D(DetailSampler, p * 0.8 + 0.37).r;
    float vein = saturate(1.0 - abs(veinNoise - 0.5) * 14.0) * saturate(irisDistance * 2.5 - 0.9) * Extra.w;

    float3 sclera = lerp(CoreColor, float3(0.75, 0.08, 0.05), vein);
    float3 irisColor = MidColor * (0.7 + Active * 0.5);
    float3 eye = lerp(sclera, irisColor, iris) * (1.0 - pupil);

    float alpha = saturate(inside + rim * 0.9) * Opacity;
    float3 color = eye * inside + DarkColor * (rim * 1.4);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// MadnessBeam: a thin white core in a yellow sheath whose width wobbles along its length, ragged at both ends.
// Quad drawn along the beam: uv.x 0 at the origin, 1 at the end. Extra.x = length / height (noise scale),
// Extra.y = end feather (aspect * ~3, so each end feathers over about a third of the beam height).
// The sheath's half-width maxes at 0.85 of the quad half-height, inside the top and bottom edges.
// Opacity carries the dim, harmless clock-hand variant.
// ---------------------------------------------------------------------------------------------
float4 BeamPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float across = abs(uv.y - 0.5) * 2.0;

    float wobble = tex2D(DetailSampler, float2(uv.x * Extra.x * 0.15 - Time * 2.0, uv.y * 0.3 + 0.2)).r;
    float width = 0.45 + wobble * 0.40;
    float ends = saturate(uv.x * Extra.y) * saturate((1.0 - uv.x) * Extra.y * (0.6 + wobble));
    float sheath = saturate((width - across) * 5.0) * ends;
    float core = saturate((0.22 - across) * 10.0) * ends;

    float alpha = saturate(sheath * 0.55 + core) * Opacity;
    float3 color = MidColor * (sheath * 0.9) + CoreColor * (core * 1.2 * Active);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// MadnessRing: an annulus with a jagged, crackling edge; white on its leading (outer) side, yellow trailing.
// Square quad; p = uv*2-1. Progress = ring radius in p-space (callers keep it <= 0.80), Extra.x = 1 / band
// half-thickness (callers keep the band <= 0.10), Extra.y = dark interior strength (0 for Shriek, ~0.7 for the
// Abyss portal). Ring-space sampling (§32) so the jag is continuous around the circle. Outer reach is at most
// 0.80 + 0.05 jag + 0.10 band = 0.95, inside every quad edge.
// ---------------------------------------------------------------------------------------------
float4 RingPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float jagNoise = tex2D(DetailSampler, dir * 0.45 + (0.5 + Time * 0.05)).r;
    float crackle = tex2D(PrimarySampler, dir * 0.30 + (0.5 - Time * 0.09)).r;

    float signedDistance = (r - Progress - (jagNoise - 0.5) * 0.10) * Extra.x;
    float band = saturate(1.0 - abs(signedDistance)) * (0.55 + crackle * 0.75);
    float leading = saturate(signedDistance * 0.5 + 0.5);
    float interior = saturate((Progress - r) * 4.0) * Extra.y;

    float alpha = saturate(band * 1.1 + interior * 0.9) * Opacity;
    float3 color = MidColor * (band * 1.05) + CoreColor * (band * leading * leading * 0.9) + DarkColor * (interior * crackle * 0.6);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// MadnessGlyph: a flickering, twitching sigil. Square quad; p = uv*2-1.
// `kind` is a compile-time switch, so each technique constant-folds to one sigil (both together were 80/64 slots):
// MadnessGlyph = spiral sigil (Frenzied Step flash, Hallucination shimmer), MadnessGlyphEye = eye sigil (Mark of Madness).
// Progress = reveal (0..1), Active = stroke flicker. Both kinds are masked to r < 0.85.
// ---------------------------------------------------------------------------------------------
float4 GlyphPixel(float2 coords : TEXCOORD0, uniform float kind) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float twitch = (tex2D(DetailSampler, p * 0.5 + Time * 0.2).r - 0.5) * 0.08;
    float r = length(p) + twitch;
    float mask = saturate((0.85 - r) * 6.0);

    // Spiral: two Archimedean arms turning with time.
    float angle = atan2(p.y, p.x) * 0.159155;
    float spiral = saturate(1.0 - abs(frac(angle * 2.0 + r * 2.6 - Time * 0.6) - 0.5) * 9.0);

    // Eye sigil: an almond outline (half-height 0.42 (1 - x^2/0.7)) around a circular iris outline.
    float lid = 0.42 * saturate(1.0 - p.x * p.x * 1.4286) - abs(p.y);
    float almond = saturate(1.0 - abs(lid) * 14.0) * saturate((0.84 - abs(p.x)) * 8.0);
    float irisRing = saturate(1.0 - abs(r - 0.22) * 16.0);
    float eye = max(almond, irisRing);

    float stroke = lerp(spiral, eye, kind) * mask * Progress;

    float alpha = saturate(stroke * 0.8) * Opacity;
    float3 color = MidColor * stroke + CoreColor * (stroke * stroke * Active);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Strip techniques (VertexStrip): coords.x runs 0..1 along the strip, coords.y 0..1 across it. The geometry (the
// scrawl's zig-zag, a thread's gravity sag) is built on the C# side in world space; these only shade the band.
// ---------------------------------------------------------------------------------------------
struct StripVertexInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct StripVertexOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

StripVertexOutput StripVS(in StripVertexInput input)
{
    StripVertexOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

// MadnessScrawl: a jagged, hand-drawn ink line. Progress = ripple front (0..1 along the strip), Extra.x = crumble
// front (everything before it has broken away), Extra.y = armed brightness (0 yellow .. 1 white; flickered from C#),
// Extra.z = strip length / width (noise scale), Extra.w = 1 / ripple half-width. Ink half-width <= 0.85.
float4 ScrawlPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float across = abs(uv.y - 0.5) * 2.0;

    float inkNoise = tex2D(DetailSampler, float2(uv.x * Extra.z * 0.25, uv.y * 0.5 + Time * 0.1)).r;
    float ink = saturate((0.55 + inkNoise * 0.30 - across) * 6.0);
    float core = saturate((0.20 - across) * 8.0);
    float alive = saturate((uv.x - Extra.x) * 40.0) * saturate((1.0 - uv.x) * 60.0);
    float ripple = saturate(1.0 - abs(uv.x - Progress) * Extra.w) * ink;

    float alpha = saturate(ink * 0.7 + core * 0.5 + ripple) * alive * Opacity;
    float3 color = (MidColor * ink + CoreColor * (core * Extra.y + ripple * 1.2)) * alive;
    return float4(color * Opacity, alpha);
}

// MadnessThread: a thin trembling strand. Progress = tautness (0 slack .. 1 taut; whitens the strand),
// Extra.x / Extra.y = 1 if the start / end hangs frayed in the air (no tile to anchor it), Extra.z = length / width
// (noise scale). Active = taut flicker. Strand offset <= 0.09 and half-width 0.40, so it stays inside the strip.
float4 ThreadPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float strandNoise = tex2D(DetailSampler, float2(uv.x * Extra.z * 0.2 - Time * 0.8, 0.5)).r;
    float tremble = (strandNoise - 0.5) * 0.18 * (1.0 - Progress * 0.5);
    float strand = saturate((0.40 - abs(uv.y - 0.5 - tremble) * 2.0) * 6.0);

    float frayStart = lerp(1.0, saturate(uv.x * 6.0) * (0.5 + strandNoise), Extra.x);
    float frayEnd = lerp(1.0, saturate((1.0 - uv.x) * 6.0) * (0.5 + strandNoise), Extra.y);
    float thread = strand * frayStart * frayEnd;

    float alpha = saturate(thread * 1.1) * Opacity;
    float3 color = lerp(MidColor, CoreColor, Progress * Active) * (thread * 1.15) + CoreColor * (thread * thread * 0.3);
    return float4(color * Opacity, alpha);
}

technique MadnessMote
{
    pass P0
    {
        PixelShader = compile ps_2_0 MotePixel();
    }
}

technique MadnessEye
{
    pass P0
    {
        PixelShader = compile ps_2_0 EyePixel();
    }
}

technique MadnessBeam
{
    pass P0
    {
        PixelShader = compile ps_2_0 BeamPixel();
    }
}

technique MadnessRing
{
    pass P0
    {
        PixelShader = compile ps_2_0 RingPixel();
    }
}

technique MadnessGlyph
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlyphPixel(0.0);
    }
}

technique MadnessGlyphEye
{
    pass P0
    {
        PixelShader = compile ps_2_0 GlyphPixel(1.0);
    }
}

// Slot-count entry points for fxc /E (it cannot pass the uniform argument itself).
float4 GlyphSpiralCount(float2 coords : TEXCOORD0) : COLOR0
{
    return GlyphPixel(coords, 0.0);
}

float4 GlyphEyeCount(float2 coords : TEXCOORD0) : COLOR0
{
    return GlyphPixel(coords, 1.0);
}

technique MadnessScrawl
{
    pass P0
    {
        VertexShader = compile vs_2_0 StripVS();
        PixelShader = compile ps_2_0 ScrawlPixel();
    }
}

technique MadnessThread
{
    pass P0
    {
        VertexShader = compile vs_2_0 StripVS();
        PixelShader = compile ps_2_0 ThreadPixel();
    }
}

// Pixel-shader-only twins of the strip techniques, for ShaderPreview's quad harness (a sprite quad's texcoords run
// 0..1 along and across exactly like a strip's). Unused in game; they cost nothing at runtime.
technique MadnessScrawlPreview
{
    pass P0
    {
        PixelShader = compile ps_2_0 ScrawlPixel();
    }
}

technique MadnessThreadPreview
{
    pass P0
    {
        PixelShader = compile ps_2_0 ThreadPixel();
    }
}
