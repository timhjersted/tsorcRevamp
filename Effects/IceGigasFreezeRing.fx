// Absolute Zero's release: an expanding annular freeze wave. A true ring, matching
// GigasFreezeRing.Colliding — the interior is SAFE and the counterplay is rolling through the
// band, so the crisp leading lip is the gameplay-relevant edge and the trailing sheath is wake.
//
// Sampled in ring space (dir = p / length(p)) rather than planar UV: this quad reaches 680px at
// full expansion, where a planar sample would tile a 512px noise visibly. A ring-space sample
// never leaves a disc of radius k around the texture centre, so it cannot repeat by construction.
#include "PixelShaderCommon.fxh"
sampler CellNoise : register(s0);
sampler CrackNoise : register(s1);
float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float RingRadius;
float RingHalfThickness;
float TrailLength;
// xy = block count across the quad, zw = its reciprocal, both divided in C#. The (uv, drawSize,
// blockSize) overload re-evaluates its max() and reciprocal per pixel (~12 slots at ps_2_0,
// which has no preshader); this form is ~2 and is what brought the shader under budget.
float4 PixelGrid;

float4 IceGigasFreezeRingPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 fromCenter = (coords - 0.5) * DrawSize;
    float radius = length(fromCenter);
    float2 dir = fromCenter / max(radius, 1.0);
    float2 swirl = float2(-dir.y, dir.x);

    // Counter-twisting the two ring-space samples keeps the crystal pattern drifting instead of
    // sitting as one stamped ring. Sliding along the tangent is a few ops; a real rotation matrix
    // costs a sin+cos pair and is not affordable here.
    float front = radius - RingRadius;
    float depth = front * 0.0040;
    float facet = tex2D(CellNoise, (dir + swirl * (Time * 0.070)) * (0.42 + depth) + 0.5).r;
    float crack = tex2D(CrackNoise, (dir - swirl * (Time * 0.130)) * (0.95 + depth * 3.0) + 0.5).r;
    float shimmer = saturate(facet * 0.60 + crack * 0.48 - 0.10);

    // `lip` is the crisp leading wavefront at RingRadius. `reach` lets the wake extend inward by
    // up to TrailLength, shimmer-feathered so the trail is ragged rather than a flat band. sheath
    // is exactly 0 once front < -reach, so the body provably dies before the quad's inner edge no
    // matter what the noise does — no term here can drag alpha back above zero at the boundary.
    float lip = saturate((RingHalfThickness - front) * 0.34);
    float reach = RingHalfThickness * 0.85 + TrailLength * (0.30 + shimmer * 0.95);
    float sheath = lip * saturate((front + reach) * 0.026);

    float towardLip = saturate((front + RingHalfThickness * 1.10) * 0.030);
    float chill = sheath * towardLip * (0.55 + crack * 0.55);
    float core = sheath * saturate((front + RingHalfThickness * 0.35) * 0.045);
    float glint = saturate(facet * 1.9 - 1.05) * chill;

    // Accumulated tiers rather than chained lerps: under premultiplied alpha the sum IS the
    // premultiplied colour, so the return needs no extra multiply by alpha.
    // sampleColor is deliberately not taken: the draw always passes Color.White, so multiplying
    // by a constant 1 would cost ~4 slots for nothing.
    float alpha = saturate(sheath * 0.90 + core * 0.30) * Opacity;
    float3 color = OuterColor * (sheath * 0.90)
        + MiddleColor * (chill * 0.85)
        + CoreColor * (core * core * 1.05 + glint * 0.80);
    return float4(color * Opacity, alpha);
}

technique IceGigasFreezeRing
{
    pass IceGigasFreezeRingPass { PixelShader = compile ps_2_0 IceGigasFreezeRingPixel(); }
}
