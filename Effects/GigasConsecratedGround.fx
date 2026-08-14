// Gigas's lingering holy fire. Each one-tile module is drawn bottom-anchored to its own terrain
// tile, so a longer field follows hills instead of bridging a flat flame strip through open air.
#include "PixelShaderCommon.fxh"
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Remaining;
float Phase;
float2 DrawSize;
float PixelBlockSize;

float4 GigasConsecratedGroundPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // This effect opts into the shared 2x2 pixel treatment. Quantizing before any shape or noise
    // maths pixelates the whole shader, not merely its texture sample.
    coords = PixelateShaderUV(coords, DrawSize, PixelBlockSize);
    // Low-frequency macro noise alone controls the silhouette: this keeps each flame tongue broad
    // at gameplay scale. The higher detail sample is restricted to material within that silhouette.
    float macro = tex2D(MacroNoise, float2(coords.x * 1.10 - Time * 0.08 + Phase, coords.y * 0.72 + Time * 0.13)).r;
    float detail = tex2D(DetailNoise, float2(coords.x * 4.40 + Time * 0.25 - Phase, coords.y * 1.70 - Time * 0.52)).r;

    // Every module owns one complete, enclosed flame body. It starts as a point, swells through
    // the middle, then softly narrows at its buried feet; adjoining one-tile modules may overlap
    // but no individual module can leave thin strands outside this silhouette.
    float flameHalfWidth = (0.04 + 0.30 * saturate(coords.y * 1.45))
        * (0.86 + macro * 0.18) * saturate((1.0 - coords.y) * 5.0);
    float flameBody = saturate((flameHalfWidth - abs(coords.x - 0.5)) * 12.0);
    // Keep the main bed above the physical lower edge. Its final few pixels are deliberately
    // embedded in the tile, making the field meet uneven terrain without a visible flat cutoff.
    float emberBottom = 0.78 + macro * 0.08;
    float floorFade = saturate((emberBottom - coords.y) * 10.0);
    float tongueTop = 0.76 - macro * 0.70;
    float tongues = saturate((coords.y - tongueTop) * 6.8) * floorFade * flameBody;
    float emberBed = saturate((coords.y - 0.60) * 7.6) * floorFade * flameBody;

    float coals = tongues * (0.62 + macro * 0.38);
    float hotCracks = emberBed * saturate((detail - 0.20) * 1.25) * (0.45 + macro * 0.55);

    float age = Remaining;
    float alpha = saturate(coals * 0.92 + hotCracks * 0.54) * age * Opacity;
    float3 color = lerp(OuterColor, MiddleColor, saturate(coals * 0.56 + macro * 0.44));
    // hotCracks is bounded below 1.4; .70 therefore stays below one without a clamp.
    color = lerp(color, CoreColor, hotCracks * 0.82);
    // AlphaBlend is premultiplied: solid gold material keeps its identity over daylight.
    return float4(sampleColor.rgb * color * alpha, sampleColor.a * alpha);
}

technique GigasConsecratedGround
{
    pass GigasConsecratedGroundPass
    {
        PixelShader = compile ps_2_0 GigasConsecratedGroundPixel();
    }
}
