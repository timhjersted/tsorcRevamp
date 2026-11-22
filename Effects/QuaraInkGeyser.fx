// QuaraInkGeyser.fx
// Dark ink attack shaders for QuaraHydromancer (and WaterFiendKraken's InkGeyser) in Reach ps_2_0.
// All three techniques draw through BlendState.AlphaBlend, so outputs are explicitly premultiplied
// and provably reach float4(0, 0, 0, 0) before every quad edge regardless of noise.
// PrimarySampler (s0) = T_CloudNoise_Tiled (macro billowing ink), DetailSampler (s1) = T_Noise_Wf4 (fluid churn).

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize, PixelDrawSize;
float4 PixelGrid, uSourceRect;

float4 InkGeyser(float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(c, PixelGrid);
    float2 p = (uv - 0.5) * 2.0;
    float r = length(p);

    // Outermost 2px block centers on the 80px quad sit at r >= 0.975, so quadFade is
    // provably 0 on all four quad edges regardless of noise.
    float quadFade = saturate((0.94 - r) * 3.8);
    quadFade = quadFade * quadFade;

    // Opposing seamless samples: macro cloud defines the boiling silhouette, fine turbulence textures the interior.
    float macro = tex2D(PrimarySampler, uv * 1.65 + float2(Time * 0.05, -Time * 0.07)).r;
    float churn = tex2D(DetailSampler, uv * 2.40 + float2(-Time * 0.11, Time * 0.14)).r;

    // Opacity carries the telegraph ramp-in and active fade-out from EnemyVFX.DrawQuaraInkGeyser.
    float fade = quadFade * Opacity;
    float reach = 0.60 + macro * 0.16;
    float inkBody = saturate((reach - r) * 3.8);
    float swirl = macro * 0.65 + churn * 0.45;

    // Organic rim along the noisy ink boundary + inward telegraph ring when charging + active core veins.
    float edgeRim = inkBody * (1.0 - inkBody) * 4.0;
    float gatherRing = (1.0 - Active) * saturate(1.0 - abs(r - (0.78 - Progress * 0.56)) * 6.5);
    float innerVein = Active * inkBody * saturate(swirl * 1.4 - 0.56);

    float coreGlow = edgeRim * 0.72 + gatherRing * 0.70 + innerVein * 0.65;
    float alpha = saturate(inkBody * 0.94 + coreGlow * 0.45) * fade;
    // Near-black DarkColor is carried by alpha occlusion under premultiplied AlphaBlend; MidColor + CoreColor glow on top.
    float3 color = MidColor * ((swirl * inkBody + edgeRim * 0.45) * 1.35 * fade) + CoreColor * (coreGlow * 0.85 * fade);
    return float4(color, alpha);
}

float4 InkJet(float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(c, PixelGrid);
    float2 p = (uv - 0.5) * 2.0;

    // 82x26 quad at 2px blocks: outermost block centers sit at |p.y| = 0.923 and uv.x in {0.012, 0.988}.
    // This envelope hits 0 strictly inside every quad border regardless of noise.
    float edgeY = saturate((0.86 - abs(p.y)) * 2.8);
    float headFade = saturate((0.96 - uv.x) * 6.5);
    float tailFade = saturate((uv.x - 0.04) * 1.5);
    float quadFade = edgeY * edgeY * headFade * tailFade;

    // Low frequency along the jet, higher across it so flow reads as pressurized streaks rather than blocks.
    float macro = tex2D(PrimarySampler, uv * float2(1.10, 2.20) + float2(-Time * 0.65, Time * 0.06)).r;
    float detail = tex2D(DetailSampler, uv * float2(1.85, 3.10) + float2(-Time * 1.05, -Time * 0.09)).r;

    // Narrower toward the tail, slightly wider near the leading head (uv.x ~ 0.80).
    float halfWidth = 0.38 + uv.x * 0.24 + (macro - 0.5) * 0.20;
    float jetMask = saturate((halfWidth - abs(p.y)) * 3.8) * quadFade;
    float stream = saturate(macro * 0.70 + detail * 0.50);

    // Continuous luminous core filament so the projectile reads cleanly against dark caves as well as bright sky.
    float coreBand = saturate(1.0 - abs(p.y) * 2.6) * saturate((uv.x - 0.08) * 1.5);
    float coreStreak = coreBand * (0.72 + detail * 0.48) * quadFade;

    float fade = Opacity;
    float alpha = saturate(jetMask * 0.94 + coreStreak * 0.50) * fade;
    float3 color = MidColor * (jetMask * (0.75 + stream * 0.85) * fade) + CoreColor * (coreStreak * 1.25 * fade);
    return float4(color, alpha);
}

float4 InkBurst(float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(c, PixelGrid);
    float2 p = (uv - 0.5) * 2.0;
    float r = length(p);

    // 92px quad at 2px blocks: outermost block centers sit at r >= 0.978.
    float quadFade = saturate((0.94 - r) * 3.4);
    quadFade = quadFade * quadFade;

    float macro = tex2D(PrimarySampler, uv * 1.85 + float2(Time * 0.07, -Time * 0.05)).r;
    float detail = tex2D(DetailSampler, uv * 2.75 + float2(-Time * 0.09, Time * 0.08)).r;

    float fadeOut = (1.0 - Progress * Progress) * Opacity;
    float splashReach = 0.36 + Progress * 0.40 + macro * 0.16;
    float inkSplat = saturate((splashReach - r) * 3.8) * saturate(macro * 1.05 + detail * 0.35 - 0.08) * quadFade;
    float waveRadius = 0.16 + Progress * 0.60;
    float ring = saturate(1.0 - abs(r - waveRadius) * 5.6) * (0.65 + macro * 0.40) * quadFade;

    float alpha = saturate(inkSplat * 0.94 + ring * 0.60) * fadeOut;
    float3 color = MidColor * (inkSplat * (0.65 + detail * 0.65) * fadeOut) + CoreColor * (ring * 0.90 * fadeOut);
    return float4(color, alpha);
}

technique QuaraInkGeyser { pass P { PixelShader = compile ps_2_0 InkGeyser(); } }
technique QuaraInkJet { pass P { PixelShader = compile ps_2_0 InkJet(); } }
technique QuaraInkBurst { pass P { PixelShader = compile ps_2_0 InkBurst(); } }
