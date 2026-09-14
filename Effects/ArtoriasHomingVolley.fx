// ArtoriasHomingVolley.fx
// Dedicated Homing Volley material: an irregular abyss knot and a torn directional wake.
// Both techniques return premultiplied colour for AlphaBlend and quantize their UVs before
// shape/noise work so the silhouette itself, not only the sampled texture, uses the pixel filter.
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

float4 ArtoriasHomingVolleyOrbPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float macro = tex2D(PrimarySampler,
        uv * 1.18 + float2(Time * 0.045, -Time * 0.032)).r;
    float grain = tex2D(DetailSampler,
        uv * 2.82 + float2(-Time * 0.078, Time * 0.049)).r;

    // Two skewed, noise-chipped slashes cross into a rotating abyss knot. There is no radial disc
    // in this construction. edgeFade is independently zero before all four quad edges, so noise
    // cannot resurrect a rectangular border either.
    float reach = 0.67 + (macro - 0.5) * 0.25;
    float armA = abs(p.y + p.x * 0.34) + abs(p.x) * 0.58;
    float armB = abs(p.x - p.y * 0.46) + abs(p.y) * 0.64;
    float knotA = saturate((reach - armA) * 7.4);
    float knotB = saturate((reach * 0.88 - armB) * 8.2);
    float edgeFade = saturate((0.96 - abs(p.x)) * 12.0)
        * saturate((0.96 - abs(p.y)) * 12.0);
    float sheath = max(knotA, knotB) * edgeFade;

    // Equal-value contours make sparse internal filaments instead of filling the knot with glow.
    float filament = sheath * saturate(1.0 - abs(macro - grain) * 7.2);
    float inward = saturate((0.78 - abs(p.x) - abs(p.y)) * 2.1);
    float hot = filament * inward * (0.72 + Progress * 0.28);
    float alpha = saturate(sheath * (0.86 + macro * 0.30)) * Opacity;

    float3 material = DarkColor * (sheath * 0.78)
        + MidColor * (sheath * (0.12 + macro * 0.18) + filament * 0.82)
        + CoreColor * (hot * 0.92);
    return float4(vertexColor.rgb * material * Opacity, vertexColor.a * alpha);
}

float4 ArtoriasHomingVolleyWakePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float along = uv.y;

    // High frequency across / low frequency along stretches the material into travelling fibers.
    float macro = tex2D(PrimarySampler,
        float2(uv.x * 2.35 + Time * 0.048, along * 0.82 - Time * 0.31)).r;
    float grain = tex2D(DetailSampler,
        float2(uv.x * 3.70 - Time * 0.072, along * 1.34 - Time * 0.49)).r;

    // Bottom is the old/tail end and top is the projectile end after the caller rotates the quad.
    // Both fades reach zero before the texture edges, and width is multiplied by them, proving
    // that neither the long sides nor either cap can expose a rectangular quad edge.
    float tailFade = saturate((along - 0.045) * 5.8);
    float headFade = saturate((0.965 - along) * 9.0);
    float lens = tailFade * headFade;
    float centerLine = 0.5 + (macro - 0.5) * lens * 0.16 + (grain - 0.5) * 0.035;
    float across = abs(uv.x - centerLine);
    float halfWidth = lens * (0.11 + along * 0.38) * (0.82 + macro * 0.28);
    float sheath = saturate((halfWidth - across) * 15.0);

    float filament = sheath * saturate(1.0 - abs(macro - grain) * 7.8);
    float leading = saturate((along - 0.48) * 1.9);
    float hot = filament * leading;
    float alpha = saturate(sheath * (0.72 + macro * 0.42)) * Opacity;

    float3 material = DarkColor * (sheath * 0.70)
        + MidColor * (sheath * (0.22 + macro * 0.20) + filament * 0.86)
        + CoreColor * (hot * hot * 0.96);
    return float4(vertexColor.rgb * material * Opacity, vertexColor.a * alpha);
}

technique ArtoriasHomingVolleyOrb
{
    pass OrbPass { PixelShader = compile ps_2_0 ArtoriasHomingVolleyOrbPixel(); }
}

technique ArtoriasHomingVolleyWake
{
    pass WakePass { PixelShader = compile ps_2_0 ArtoriasHomingVolleyWakePixel(); }
}
