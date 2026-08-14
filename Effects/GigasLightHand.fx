// Gigas grasping light: a soft field of moving holy light, shaped by the pixel-art monolith mask.
// The crisp translucent monolith itself is drawn separately with point sampling above this aura.
#include "PixelShaderCommon.fxh"
sampler SlabTexture : register(s0);
sampler FlowNoise : register(s1);
sampler CrackNoise : register(s2);

float3 GoldColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float PixelBlockSize;

float4 GigasLightHandAuraPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, DrawSize, PixelBlockSize);
    float4 slab = tex2D(SlabTexture, coords);

    // The aura draw is deliberately much larger than the foreground slab. This source silhouette
    // and its inexpensive analytical fade give every flame enough transparent border to die out.
    float softMask = slab.a;
    float quadFade = saturate(coords.x * 3.2) * saturate((1.0 - coords.x) * 3.2)
        * saturate(coords.y * 3.2) * saturate((1.0 - coords.y) * 3.2);
    float macro = tex2D(FlowNoise, coords * float2(1.22, 1.78) + float2(-Time * 0.075, Time * 0.24)).r;
    float detail = tex2D(CrackNoise, coords * float2(4.80, 3.20) + float2(Time * 0.26, -Time * 0.42)).r;
    float flame = saturate(macro * 0.65 + detail * 0.54 - 0.17);
    float tongues = saturate(detail * 1.34 + macro * 0.42 - 0.30);
    float interior = softMask * (0.62 + flame * 0.31);
    float edge = softMask * (1.0 - slab.a) * tongues;
    float heat = slab.a * saturate(flame * 1.24 - 0.13);
    float alpha = saturate(interior + edge * 0.52) * quadFade * Opacity;
    float3 color = lerp(GoldColor * 0.76, CoreColor, flame * 0.68);
    float3 emission = CoreColor * (interior * 0.28 + heat * 0.54 + edge * 0.42) * quadFade * Opacity;
    return float4(color * alpha + emission, alpha);
}

technique GigasLightHandAura
{
    pass GigasLightHandAuraPass { PixelShader = compile ps_2_0 GigasLightHandAuraPixel(); }
}
