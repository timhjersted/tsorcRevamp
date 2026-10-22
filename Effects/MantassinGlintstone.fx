// MantassinGlintstone.fx
// The Mantassin's finger sorceries, themed on Elden Ring's Glintstone Nails and Fleeting Microcosm (user's
// reference art, 2026-10-21): silver-white light with a faint lavender cast, a gold haze on the Microcosm.
// Its own file so the Archdeacon's shipped ArchdeaconGlintstone.fx can never regress from edits here.
//
// Blend contract: BlendState.AlphaBlend, which in XNA is PREMULTIPLIED, so each return is float4(color, alpha)
// with color already scaled by coverage. rgb > alpha = light; rgb ~ 0 with alpha > 0 = a real dark occluder
// (the Microcosm's black interior).
// Palette (set from C#): MidColor = silver-lavender body, CoreColor = white, AccentColor = Microcosm gold.
//
// Techniques:
//   MantassinNailTrail   - VS + PS. A strip along the nail's recorded path (uv.x 0 = head, 1 = tail end).
//   MantassinNailGlint   - square quad. The 4-point glint over a nail head (and the shatter flash).
//   MantassinMicrocosm   - square quad. Black star-filled disc, white rim, gold haze, full-radius guide ring.
//   MantassinMicrocosmRay- long quad. One lens-flare spike; drawn twice, crossed, for the X.
//   MantassinCrescent    - square quad. One Cherishing Light arc, centred on +x of the quad.
//   MantassinRune        - flat quad (about 3.3:1). Floor sigil whose ring tightens as Progress runs out.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

matrix WorldViewProjection;
float3 MidColor;
float3 CoreColor;
float3 AccentColor;
float Opacity;
float Time;
float Progress;
float Active;
float4 PixelGrid;

// ---------------------------------------------------------------------------------------------
// Nail trail. Vertex colour alpha carries the per-point fade the C# side computes (head bright, tail gone),
// so the strip's own geometry decides where the trail ends; the shader only shapes the cross-section.
// The noise texture is bound to s0 by the caller (no SpriteBatch here).
// PixelGrid here = (blocks along the trail, blocks across it, reciprocals), from the trail's real length.
// ---------------------------------------------------------------------------------------------
struct TrailVertexInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct TrailVertexOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

TrailVertexOutput NailTrailVertex(in TrailVertexInput input)
{
    TrailVertexOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 NailTrailPixel(TrailVertexOutput input) : COLOR0
{
    float2 uv = PixelateShaderUV(input.TextureCoordinates, PixelGrid);
    float across = abs(uv.y - 0.5) * 2.0;

    // Thick near the head, thinning to a hair at the tail end.
    float taper = 1.0 - uv.x * 0.75;
    float glow = saturate(1.0 - across / taper);
    glow *= glow;
    float core = saturate((taper * 0.30 - across) * 9.0);

    // Wisps streaming away from the head: low frequency along, high across.
    float wisp = tex2D(PrimarySampler, float2(uv.x * 1.4 - Time * 1.8, uv.y * 0.9)).r;
    glow *= 0.50 + wisp * 0.85;

    float fade = input.Color.a * Opacity;
    float alpha = saturate(glow * 0.55 + core) * fade;
    float3 color = MidColor * (glow * 1.15) + CoreColor * (core * 1.20);
    return float4(color * fade, alpha);
}

// ---------------------------------------------------------------------------------------------
// Nail glint: a tight white core, a soft silver halo and a thin 4-point flare. Progress = extra brightness
// (the flare before a nail launches, or the shatter flash). Every term is zero before r = 0.9.
// ---------------------------------------------------------------------------------------------
float4 NailGlintPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float halo = saturate(1.0 - r * 1.25);
    halo *= halo;
    float core = saturate((0.18 - r) * 7.0);
    float flare = saturate(1.0 - abs(p.x * p.y) * 90.0) * saturate(1.0 - r * 1.15);

    float alpha = saturate(halo * 0.45 + core + flare * 0.70) * Opacity;
    float3 color = MidColor * (halo * 0.90) + CoreColor * ((core + flare) * (0.60 + Progress * 0.80));
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Fleeting Microcosm disc. The quad is drawn at the hit diameter / 0.70, so the full hit radius sits at
// r = 0.70 and the gold haze (0.22 wide) ends by r = 0.92, inside every quad edge.
//   Progress = how far the void has opened (0 = a point, 1 = the full hit radius). Callers clamp it.
//   Active   = rim flash (1 on the pulse tick, decaying).
// The guide ring at r = 0.70 is drawn from the first tick, so the danger zone is visible before the void fills it.
// ---------------------------------------------------------------------------------------------
float4 MicrocosmPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);
    float voidRadius = Progress * 0.70;

    // Black interior: high coverage, almost no colour, so it really darkens what is behind it.
    float inside = saturate((voidRadius - r) * 40.0);

    // Star specks: the product of two noises at unrelated scales, thresholded hard, so they are small, many and do NOT
    // repeat (one noise alone tiled into a visible grid in the GPU preview). The cloud sample doubles as the nebula.
    float nebula = tex2D(PrimarySampler, p * 1.9 + float2(Time * 0.03, -Time * 0.02)).r;
    float speckle = tex2D(DetailSampler, p * 5.3 + float2(Time * 0.04, Time * 0.02)).r;
    float stars = saturate(speckle * nebula * 16.0 - 7.2);

    // White rim on the void's edge (sharp line + wider glow), broken by noise so it never reads as a perfect circle.
    float rimDistance = abs(r - voidRadius);
    float rimBreak = 0.55 + tex2D(DetailSampler, p * 3.0 + float2(-Time * 0.20, Time * 0.15)).r * 0.70;
    float rim = saturate(1.0 - rimDistance * 32.0) * rimBreak;
    float rimGlow = saturate(1.0 - rimDistance * 9.0);

    // Gold haze outside the rim, and the full-radius guide ring.
    float haze = saturate(1.0 - (r - voidRadius) * 4.5) * (1.0 - inside);
    float guide = saturate(1.0 - abs(r - 0.70) * 55.0);

    // The rim is WHITE-hot (the reference); gold only in the haze and the guide ring outside it.
    float alpha = saturate(inside * 0.92 + rim + rimGlow * 0.30 + haze * 0.28 + guide * 0.55) * Opacity;
    float3 color = MidColor * (nebula * inside * 0.30 + rimGlow * 0.30)
        + CoreColor * (stars * inside + rim * (1.6 + Active) + rimGlow * 0.45)
        + AccentColor * (haze * 0.55 + guide * 0.60);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Microcosm lens-flare spike: a needle of light along the quad's long axis, widest at the centre.
// Width reaches 0 at both ends and stays under 0.9 of the half-height, so no quad edge ever shows.
// ---------------------------------------------------------------------------------------------
float4 MicrocosmRayPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float along = abs(uv.x - 0.5) * 2.0;
    float across = abs(uv.y - 0.5) * 2.0;

    float width = (1.0 - along) * (1.0 - along);
    float ray = saturate((width * 0.85 - across) * 6.0);
    float core = saturate((width * 0.25 - across) * 14.0);
    float shimmer = tex2D(DetailSampler, float2(uv.x * 2.0 - Time * 0.6, uv.y * 0.5)).r;

    float alpha = saturate(ray * 0.45 * (0.60 + shimmer * 0.60) + core) * Opacity;
    float3 color = AccentColor * (ray * 0.55) + CoreColor * (core + ray * shimmer * 0.40);
    return float4(color * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// Cherishing Light arc: 70 degrees of a circle of radius 0.75 (quad units), centred on the quad's +x axis.
// The caller rotates the quad to place it. Angles via the unit vector (p / r), no atan2:
//   cos > 0.819 is inside the 35-degree half-angle; sin runs -0.574 .. +0.574 from one tip to the other.
//   Progress = trace-in (0 = nothing, 1 = the whole arc drawn from the -sin tip). Active = block/hit flash.
// The band is at most 0.11 thick, so it ends by r = 0.86, inside every quad edge.
// ---------------------------------------------------------------------------------------------
float4 CrescentPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);
    float2 unit = p / (r + 0.001);

    // A thin curved BEAM, not a crescent moon (the first GPU preview read as one): an even-width white core with a soft
    // silver glow, tapering only over the last few degrees so the tips come to points. `tips` is 0 at the tips, 1 a
    // few degrees in. Glow reaches 0 at |dr| = 0.125, so the band ends by r = 0.875.
    float tips = saturate((unit.x - 0.819) * 14.0);
    float radial = abs(r - 0.75);
    float core = saturate((0.035 * tips - radial) * 45.0);
    float glow = saturate(1.0 - radial * 8.0) * tips;
    glow *= glow;
    float trace = saturate((Progress * 1.25 - 0.574 - unit.y) * 12.0);

    // Light flowing along the arc (scrolled in angle), not across it.
    float flow = tex2D(DetailSampler, float2(unit.y * 2.0 - Time * 1.2, r * 3.0)).r;

    float shape = trace * Opacity;
    float alpha = saturate(glow * (0.35 + flow * 0.50) + core) * shape;
    float3 color = MidColor * (glow * (0.70 + flow * 0.80))
        + CoreColor * (core * (1.20 + Active) + glow * Active * 0.60);
    return float4(color * shape, alpha);
}

// ---------------------------------------------------------------------------------------------
// Floor sigil (Encircling Nails target, Glintstone Blink destination). Drawn as a flat quad, so the circle
// becomes a floor ellipse. Progress 0..1 = how much of the countdown has run: the outer ring tightens from
// 0.85 to 0.45 and the centre brightens. Every term is zero before r = 0.93.
// ---------------------------------------------------------------------------------------------
float4 RunePixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);

    float ringRadius = 0.85 - Progress * 0.40;
    float ring = saturate(1.0 - abs(r - ringRadius) * 14.0);
    float inner = saturate(1.0 - abs(r - 0.30) * 20.0) * 0.60;
    float glints = saturate(tex2D(DetailSampler, p * 2.5 + float2(Time * 0.20, -Time * 0.15)).r * 4.0 - 2.6)
        * saturate(0.90 - r);
    float centre = saturate((0.14 - r) * 8.0) * (0.40 + Progress * 0.60);

    float alpha = saturate(ring * 0.70 + inner + glints + centre) * Opacity;
    float3 color = MidColor * ((ring + inner) * 0.80) + CoreColor * (ring * ring * 0.60 + glints + centre);
    return float4(color * Opacity, alpha);
}

technique MantassinNailTrail
{
    pass P0
    {
        VertexShader = compile vs_2_0 NailTrailVertex();
        PixelShader = compile ps_2_0 NailTrailPixel();
    }
}

technique MantassinNailGlint
{
    pass P0
    {
        PixelShader = compile ps_2_0 NailGlintPixel();
    }
}

technique MantassinMicrocosm
{
    pass P0
    {
        PixelShader = compile ps_2_0 MicrocosmPixel();
    }
}

technique MantassinMicrocosmRay
{
    pass P0
    {
        PixelShader = compile ps_2_0 MicrocosmRayPixel();
    }
}

technique MantassinCrescent
{
    pass P0
    {
        PixelShader = compile ps_2_0 CrescentPixel();
    }
}

technique MantassinRune
{
    pass P0
    {
        PixelShader = compile ps_2_0 RunePixel();
    }
}
