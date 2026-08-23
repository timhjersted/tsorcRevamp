#include "PixelShaderCommon.fxh"

// s0/s1 mean different things per technique, because the callers differ. GwynCinderArc and
// GwynCinderBlade take a real SPRITE at s0 (the boomerang's greatsword) and a flow field at s1.
// GwynCinderSlash is fully procedural and takes macro turbulence at s0, fine turbulence at s1.
sampler PrimaryTexture : register(s0);
sampler FlowNoise : register(s1);

float3 CinderColor;
float3 FlameColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float2 CoordScale;        // GwynCinderSlash: PrimaryTextureSize / DrawSize, pre-divided in C#
float4 PixelGrid;         // GwynCinderSlash: xy = 2px block count across the quad, zw = reciprocal
float Progress;

float2 NormalizedCoordinates(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 GwynCinderArcPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = NormalizedCoordinates(coords);
    float2 p = (uv - float2(0.5, 0.5)) * 2.0;
    float noise = tex2D(PrimaryTexture, uv * 1.85 + float2(-Time * 0.52, Time * 0.16)).r;

    float2 crescentPoint = float2(p.x * 0.92 + 0.14, p.y);
    float shellDistance = abs(dot(crescentPoint, crescentPoint) - 0.397);
    float shell = saturate((0.214 - shellDistance) * 4.67);
    float forwardMask = saturate((p.x + 0.48) * 1.5625);
    float body = shell * forwardMask;
    float core = body * body;
    float intensity = body * (0.42 + noise * 0.88) + core * 0.85;

    float3 color = lerp(CinderColor, FlameColor, saturate(noise * 1.15));
    color += CoreColor * core * 0.55;
    float ageFade = 1.0 - Progress * 0.40;
    float alpha = saturate(body + core * 0.60) * Opacity * ageFade;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

float4 GwynCinderBladePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 sprite = tex2D(PrimaryTexture, coords);
    float2 texel = 1.0 / max(PrimaryTextureSize, float2(1.0, 1.0));
    float neighborAlpha = min(
        min(tex2D(PrimaryTexture, coords + float2(texel.x, 0.0)).a,
            tex2D(PrimaryTexture, coords - float2(texel.x, 0.0)).a),
        min(tex2D(PrimaryTexture, coords + float2(0.0, texel.y)).a,
            tex2D(PrimaryTexture, coords - float2(0.0, texel.y)).a));
    float edge = saturate((sprite.a - neighborAlpha) * 4.0);
    float flow = tex2D(FlowNoise, coords * 2.2 + float2(-Time * 0.48, Time * 0.31)).r;
    float luminance = dot(sprite.rgb, float3(0.299, 0.587, 0.114));
    float heat = sprite.a * (0.42 + flow * 0.62 + luminance * 0.22) + edge * 1.15;

    float3 color = lerp(CinderColor, FlameColor, saturate(flow * 0.92 + luminance * 0.28));
    color = lerp(color, CoreColor, edge);
    float alpha = sprite.a * saturate(0.52 + flow * 0.62 + edge) * Opacity;
    return float4(sampleColor.rgb * color * heat, sampleColor.a * alpha);
}

// Gwyn's greatsword slash, drawn under BlendState.AlphaBlend (premultiplied — the return is already
// density-weighted, so a bare float4(color, alpha) here would paint a rectangle the size of the quad).
//
// This replaces a version that tinted the shared 3-frame `Slash` sprite and edge-detected its
// outline. Two things doomed that: the sprite is a soft pale crescent whose art fights any palette
// put on it, and a 1-texel rim on a 512px source drawn at ~100px on screen is sub-pixel, so the one
// feature meant to give it definition simply vanished. Additive on top of that guaranteed a pale
// smear over any bright background (§43). Shape comes from maths here — resolution-independent, and
// it cannot silently degrade at a size nobody previewed.
//
// Local space is p in [-1,1]^2 with +X along the aim direction; the caller rotates the quad.
float4 GwynCinderSlashPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords * CoordScale, PixelGrid);
    float2 p = uv * 2.0 - 1.0;

    // The blade path: a circle centred behind the quad, so the arc bulges forward. `d` is the signed
    // distance to that centreline — positive ahead of it, negative back toward the hilt.
    float d = length(p - float2(-0.62, 0.0)) - 1.16;

    // The sweep, resolved BEFORE the thickness, because it has to TAPER the blade rather than just
    // gate it. Cutting a constant-width arc with a straight line leaves a flat slab at the leading
    // end — the single most sprite-like artifact this shape can produce, and the reason the first
    // draft had a bright bar across the quad. `behind` is how far the travelling tip has already
    // passed this point; the caller's vertical flip mirrors p.y, which mirrors the sweep with Gwyn's
    // facing for free. `age` only DIMS: letting it reach zero deleted the oldest third of the arc,
    // which read as a broken sprite rather than as a fading trail.
    float sweepY = Progress * 2.35 - 1.15;
    float behind = sweepY - p.y;
    float lead01 = saturate(behind * 3.40);
    float age = saturate(1.0 - behind * 0.42);

    // Thickness tapers to a point at both ends of the arc AND at the travelling tip. (1 - y*y) is
    // zero at p.y = +-1, which is the quad's own Y edge, so every term built on halfWidth provably
    // dies before that boundary. On X the outer flame reaches p.x = -0.62 + 1.16 + 0.34 = 0.88,
    // leaving 0.12 of clear quad — more than the 1/13 = 0.077 the leading edge needs to feather.
    float halfWidth = 0.34 * (1.0 - p.y * p.y) * lead01;

    // Sample ACROSS the arc at high frequency and ALONG it at low, so features elongate along the
    // sweep (§45). Sampling isotropically is what turns a slash into a row of puffs. Both frequencies
    // stay modest: the first draft ran ~7 texture units across the blade's thickness, and a field
    // that fine stops being texture and becomes confetti sitting on top of the shape (§46).
    float2 flowUV = float2(d * 1.30 - Time * 0.55, p.y * 0.55 + Time * 0.10);
    float shape = tex2D(PrimaryTexture, flowUV).r;
    float detail = tex2D(FlowNoise, flowUV * 1.90 + float2(Time * 0.31, -Time * 0.12)).r;

    // Crisp leading edge, ragged trailing edge — that asymmetry IS the slash. Noise only ever
    // extends the tail backward; it cannot push the cutting edge forward.
    float lead = saturate((halfWidth - d) * 13.0);
    float tail = saturate((d + halfWidth * (0.85 + shape * 2.30)) * 3.20);
    float blade = lead * tail;

    // The hot spot sits just BEHIND the travelling tip, not on it: the tip is exactly where the
    // taper drives the blade to zero width, so a glow centred there has nothing to light. Reusing
    // lead01 gets that offset for free — x * (1 - x) peaks at x = 0.5, which is the halfway point of
    // the sweep taper. Written as its own abs()-of-a-biased-distance it cost 7 slots of 64.
    float tipHeat = blade * lead01 * (1.0 - lead01) * 4.0;

    float body = blade * (0.30 + age * 0.70);
    float heat = body * (0.42 + detail * 0.85) + tipHeat * 0.55;
    float edge = body * saturate((halfWidth * 0.55 - abs(d)) * 6.0) + tipHeat * 0.70;

    // Accumulated density tiers, as in GwynCinderNova. CinderColor is a dark ember red so the aged
    // tail OCCLUDES rather than glowing — that dark half is what stops the slash washing out.
    float alpha = saturate(body * 1.25 + edge * 0.35) * Opacity;
    float3 color = CinderColor * (body * 0.95)
        + FlameColor * (heat * 0.85)
        + CoreColor * (edge * edge * 0.95);
    return float4(color * Opacity, alpha);
}

technique GwynCinderSlash
{
    pass GwynCinderSlashPass
    {
        PixelShader = compile ps_2_0 GwynCinderSlashPixel();
    }
}

technique GwynCinderArc
{
    pass GwynCinderArcPass
    {
        PixelShader = compile ps_2_0 GwynCinderArcPixel();
    }
}

technique GwynCinderBlade
{
    pass GwynCinderBladePass
    {
        PixelShader = compile ps_2_0 GwynCinderBladePixel();
    }
}
