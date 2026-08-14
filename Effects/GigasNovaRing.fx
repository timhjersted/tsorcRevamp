// Wrath of Gold: a filled circle collision field with a molten solar body. The telegraph is a
// separate light-weight technique so the live effect can afford a billowing corona under Reach.
#include "PixelShaderCommon.fxh"
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);
sampler FlameNoise : register(s2);
float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float RingRadius;
float Progress;
float PixelBlockSize;

float4 GigasNovaTelegraphPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, DrawSize, PixelBlockSize);
    float radius = length((coords - 0.5) * DrawSize);
    float edge = saturate((8.0 - abs(radius - RingRadius * Progress)) / 5.0);
    float fill = saturate((RingRadius * Progress - radius) / 16.0) * 0.09;
    float alpha = (fill + edge * 0.42) * Opacity;
    return float4(sampleColor.rgb * MiddleColor * alpha, sampleColor.a * alpha);
}

float4 GigasNovaSunPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, DrawSize, PixelBlockSize);
    float radius = length((coords - 0.5) * DrawSize);

    // Counter-flowing planar samples form the molten body. Macro alone also pushes/pulls the
    // corona, so the silhouette breaks into slow uneven tongues rather than a clean circle.
    float macro = tex2D(MacroNoise, coords * 1.75 + float2(-Time * 0.10, Time * 0.14)).r;
    float detail = tex2D(DetailNoise, coords * 5.20 + float2(Time * 0.23, -Time * 0.31)).r;
    float flame = saturate(macro * 0.68 + detail * 0.56 - 0.12);
    float safeField = saturate((RingRadius - radius) / 12.0) * (0.26 + flame * 0.58);
    float solarReach = RingRadius + (macro - 0.48) * 74.0;
    float solarField = saturate((solarReach - radius) / 13.0) * (0.18 + flame * 0.62);
    float corona = solarField * saturate((radius - RingRadius + 11.0) / 26.0);
    float edge = saturate((8.0 - abs(radius - RingRadius)) / 5.0) * (0.34 + flame * 0.36);
    float hotPockets = safeField * saturate(flame * 1.32 - 0.16);

    float3 color = lerp(OuterColor, MiddleColor, saturate(safeField * 0.70 + flame * 0.30));
    color = lerp(color, CoreColor, hotPockets * 0.48);
    float alpha = saturate(safeField * 0.68 + corona * 0.38 + edge * 0.28) * Opacity;
    float3 emission = CoreColor * (hotPockets * 0.17 + edge * 0.04) * Opacity;
    return float4(sampleColor.rgb * (color * alpha + emission), sampleColor.a * alpha);
}

// A separate, slower crown pass. Its high-contrast fire texture only displaces the outer
// envelope; the dense solar body stays in the first pass so this reads as flame around a sun.
float4 GigasNovaCoronaPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, DrawSize, PixelBlockSize);
    float radius = length((coords - 0.5) * DrawSize);
    float macro = tex2D(MacroNoise, coords * 1.18 + float2(Time * 0.045, -Time * 0.072)).r;
    float flame = tex2D(FlameNoise, coords * 0.94 + float2(-Time * 0.035, Time * 0.235)).r;
    float tongues = saturate(flame * 1.24 + macro * 0.46 - 0.23);
    // Scale the corona travel with the actual field. This also lets Solar Slabs reuse the same
    // molten-nova burst at a compact contact size instead of sampling a 270px-only envelope.
    float reach = RingRadius + 18.0 + tongues * RingRadius * 0.275;
    float crown = saturate((reach - radius) / 10.0)
        * saturate((radius - RingRadius + 18.0) / 14.0);
    float rim = saturate((10.0 - abs(radius - RingRadius)) / 6.0);
    float heat = saturate(tongues * 1.32 - 0.12);
    float3 color = lerp(OuterColor, MiddleColor, heat);
    color = lerp(color, CoreColor, heat * heat * 0.42);
    float alpha = saturate(crown * (0.22 + heat * 0.36) + rim * 0.10) * Opacity;
    float3 emission = CoreColor * crown * heat * 0.11 * Opacity;
    return float4(sampleColor.rgb * (color * alpha + emission), sampleColor.a * alpha);
}

technique GigasNovaTelegraph
{
    pass GigasNovaTelegraphPass { PixelShader = compile ps_2_0 GigasNovaTelegraphPixel(); }
}

technique GigasNovaSun
{
    pass GigasNovaSunPass { PixelShader = compile ps_2_0 GigasNovaSunPixel(); }
}

technique GigasNovaCorona
{
    pass GigasNovaCoronaPass { PixelShader = compile ps_2_0 GigasNovaCoronaPixel(); }
}
