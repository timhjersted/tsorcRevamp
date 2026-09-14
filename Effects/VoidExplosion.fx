// Reusable large void detonation. The caller supplies the palette, progress and pixel grid; no
// Artorias-specific state is baked into this effect.
//
// PrimarySampler is one selected frame of Gwyn's Descent of the Sun 5x5 explosion flipbook. It
// supplies the entire rolling-cloud silhouette rather than a procedural circle; DetailSampler only
// colours that silhouette. There is intentionally no radius, disc, ring or inside test anywhere in
// this file: the animation frame's alpha is the sole boundary and is sampled once, never tiled.
//
// Both techniques use premultiplied AlphaBlend. Returning bare colour with zero alpha would paint
// the exact rectangular failure this shader replaces.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0); // one Descent explosion frame
sampler DetailSampler : register(s1);  // fine turbulence and interference

float3 DarkColor;
float3 MidColor;
float3 AccentColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float4 PixelGrid;
float4 uSourceRect;

float4 VoidExplosionChargePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // The shared source is a fixed 5x5 flipbook. Multiplication avoids uniform division in ps_2_0.
    float2 uv = PixelateShaderUV((coords - uSourceRect.xy) * 5.0, PixelGrid);
    float2 atlasUV = uSourceRect.xy + uv * 0.2;
    float2 p = (uv - 0.5) * 2.0;
    // The animated Descent frame owns the silhouette; fine noise only textures its interior.
    float4 explosion = tex2D(PrimarySampler, atlasUV);
    float macro = explosion.a;
    float heat = explosion.g;
    float2 detailPoint = float2(p.y, -p.x) * 0.57 + (0.50 - Time * 0.023);
    float detail = tex2D(DetailSampler, detailPoint).r;

    // Multiplying every material layer by macro is important: turbulence may never invent a disc or
    // expose the square quad outside Gwyn's hand-authored explosion animation.
    float cloud = macro * saturate(macro * 1.30 + detail * 0.24 - 0.20);
    float filaments = saturate(1.0 - abs(heat - detail) * 8.0) * cloud;
    float hot = saturate(heat * 1.68 + detail * 0.34 - 1.0) * cloud;
    float fade = Opacity * (0.44 + Progress * 0.56);
    float alpha = saturate(cloud * 0.92 + hot * 0.28) * fade;
    float3 color = DarkColor * (cloud * 0.18)
        + MidColor * (cloud * 0.70)
        + AccentColor * (filaments * 0.54 + hot * 0.26)
        + CoreColor * (hot * hot * 0.68);
    return float4(color * fade, alpha);
}

float4 VoidExplosionBlastPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV((coords - uSourceRect.xy) * 5.0, PixelGrid);
    float2 atlasUV = uSourceRect.xy + uv * 0.2;
    float2 p = (uv - 0.5) * 2.0;
    // The flipbook supplies a unique billowing explosion frame. Only the detail field scrolls, at
    // about one period across the frame, so neither layer can form wallpaper repetition.
    float4 explosion = tex2D(PrimarySampler, atlasUV);
    float macro = explosion.a;
    float heat = explosion.g;
    float2 detailPoint = float2(-p.y, p.x) * 0.55 + (0.50 - Time * 0.029);
    float detail = tex2D(DetailSampler, detailPoint).r;

    float cloud = macro * saturate(macro * 1.28 + detail * 0.22 - 0.18);
    float filaments = saturate(1.0 - abs(heat - detail) * 9.0) * cloud;
    float hot = saturate(heat * 1.72 + detail * 0.38 - 1.04) * cloud;

    // Fade the animation material itself. There is no analytic edge and therefore no circular
    // imprint revealing the collision radius.
    float breakup = 1.0 - Progress * Progress * 0.62;
    float fade = Opacity * breakup;
    float alpha = saturate(cloud * 0.90 + hot * 0.30) * fade;
    float3 color = DarkColor * (cloud * 0.18)
        + MidColor * (cloud * (0.24 + heat * 0.68))
        + AccentColor * (filaments * filaments * 0.54 + hot * 0.28)
        + CoreColor * (hot * hot * 0.82);
    return float4(color * fade, alpha);
}

technique VoidExplosionCharge
{
    pass ChargePass { PixelShader = compile ps_2_0 VoidExplosionChargePixel(); }
}

technique VoidExplosionBlast
{
    pass BlastPass { PixelShader = compile ps_2_0 VoidExplosionBlastPixel(); }
}
