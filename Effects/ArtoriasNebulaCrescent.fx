#include "PixelShaderCommon.fxh"

// Spiral Fan's spinning blade: a procedural crescent of churning abyss nebula (no sprite). Drawn by
// ArtoriasVFX.DrawNebulaCrescent over the full TurbulentNoise texture, so coords are already 0..1 across the
// quad. Two noise layers drift against each other (the "nebulous" motion), one of them also warps the
// crescent's outline so its edges boil, and a sparse threshold of the detail noise makes drifting star specks.
// Direction = a per-projectile phase (0..1) so a ring of crescents never shows the same stamp.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Direction;
float4 PixelGrid;

float4 NebulaCrescentPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);

    // Two layers drifting in opposing directions, never in phase, so the body churns instead of scrolling.
    float n1 = tex2D(PrimarySampler, uv * 1.3 + float2(Time * 0.11 + Direction, -Time * 0.07)).r;
    float n2 = tex2D(DetailSampler, uv * 2.1 + float2(-Time * 0.06, Time * 0.13 + Direction)).r;
    float density = saturate(n1 * 0.75 + n2 * 0.55 - 0.25);

    // Crescent: outer disc r 0.86 minus an inner disc r 0.66 pushed +0.44 along x (opens toward +x).
    // The warp shifts the sample point by up to ~0.11 quad-half-widths, so both edges boil with the noise.
    float2 p = uv * 2.0 - 1.0 + (float2(n1, n2) - 0.5) * 0.22;
    // Squared distances (0.74 = 0.86^2, 0.44 = 0.66^2) instead of length(): the two rsq/rcp pairs don't fit the
    // ps_2_0 budget. Near the edges d(r^2) ~ 1.5 dr, so the falloff/rim factors below are the radial ones / 1.5.
    float2 q = p - float2(0.44, 0.0);
    float outerEdge = 0.74 - dot(p, p);
    float innerEdge = dot(q, q) - 0.44;
    // Soft falloff whose reach is itself noise-driven, so the silhouette never reads as two circles.
    float body = saturate(outerEdge * (3.3 + n2 * 4.0)) * saturate(innerEdge * (3.5 + n1 * 4.2));

    // Bright cutting rim along the inner (concave) edge, where the blade "edge" sits.
    // It peaks a little inside the edge (0.08 in r^2), where body is already solid, and is gone by ~0.4 in.
    float rim = saturate(1.3 - innerEdge * 3.6) * body;

    // Star specks: only the top ~8% of a fine, slowly drifting detail sample.
    float star = saturate((tex2D(DetailSampler, uv * 5.3 + float2(Time * 0.03, Direction)).r - 0.84) * 9.0) * body;

    // Mid tone arrives early (density * (1.6 - 0.6 density) ~ pow 0.75), the hot core only where it's thick
    // (density squared). Polynomials instead of pow(): each pow is ~3 slots and this sits at the ps_2_0 budget.
    float midMix = density * (1.6 - 0.6 * density);
    float hotMix = density * density;
    // No DarkColor term: under premultiplied alpha the dark wisps are carried by alpha over the scene (rgb -> 0).
    float3 color = MidColor * (0.55 + midMix * 0.75);
    color = lerp(color, CoreColor, saturate(hotMix * 0.7 + rim * 0.6 + star));

    // Premultiplied (drawn with BlendState.AlphaBlend): thin dark wisps at low density, near-solid where it's thick.
    float alpha = body * saturate(0.45 + density * 0.95 + rim * 0.4) * Opacity;
    return float4(color * alpha, alpha);
}

technique ArtoriasNebulaCrescent
{
    pass P0
    {
        PixelShader = compile ps_2_0 NebulaCrescentPixel();
    }
}
