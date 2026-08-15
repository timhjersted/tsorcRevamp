// QuaraTideRush.fx
// Quara Hydromancer's water-form movement shift. Both variants are procedural so no sprite-alpha
// box can leak through. `QuaraTideRush` is the default flowing form; `QuaraTideRushUndertow`
// remains available for an A/B call-site swap.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize, PixelDrawSize;
float4 uSourceRect;

// `Progress` runs 0->1 while dissolving, stays at 1 during the surge, then runs 0->1 while
// reforming. Active selects the inverse curve for reformation without a branch or new uniform.
float ShiftAmount()
{
    return Progress * (1.0 - Active) + (1.0 - Progress) * Active;
}

// A — Flowing Water Form. A narrow upper column pours into a broad, low pool. Macro noise owns
// the silhouette; faster detail only varies the surface, so the result stays a connected watery
// form rather than dissolving into speckles. Its vertical fade reaches zero before y=0 and y=1,
// while its width is <= .47, so alpha is provably zero before every quad edge.
float4 TideRush(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(c, PixelDrawSize, 2.0);
    float shift = ShiftAmount();
    float edge = tex2D(DetailSampler, uv * float2(1.55, 1.08) + float2(-Time * 0.16 * Direction, Time * 0.07)).r;
    float detail = tex2D(DetailSampler, uv * float2(3.65, 2.40) + float2(-Time * 0.43 * Direction, Time * 0.19)).r;

    float topFade = saturate((uv.y - 0.07) * 7.5);
    float floorFade = saturate((0.96 - uv.y) * 9.0);
    float falling = topFade * floorFade;
    float pool = saturate((uv.y - 0.66) * 4.1) * floorFade;
    float columnWidth = (0.205 + edge * 0.105) * falling;
    float poolWidth = 0.355 + edge * 0.085;
    float width = lerp(columnWidth, poolWidth, pool);
    float body = saturate((width - abs(uv.x - 0.5)) * 10.0) * falling;

    // A noise-independent cap leaves a transparent gutter on all four sides even at the widest pool.
    float quadFade = saturate((0.48 - abs(uv.x - 0.5)) * 12.0) * topFade * floorFade;
    body *= quadFade;
    float churn = saturate(edge * 0.74 + detail * 0.52 - 0.16);
    float foam = pool * body * saturate(detail * 1.38 - 0.38);

    float alpha = saturate(body * (0.74 + churn * 0.17) + foam * 0.37) * Opacity * shift;
    float3 color = lerp(DarkColor, MidColor, churn);
    color = lerp(color, CoreColor, foam * 0.74);
    return float4(color * alpha, alpha) * v;
}

// B — Undertow. This version compresses the upper water into a curved, racing stream that throws
// out into a rippling base. It is intentionally more directional and aggressive than the default.
float4 TideRushUndertow(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(c, PixelDrawSize, 2.0);
    float shift = ShiftAmount();
    float macro = tex2D(DetailSampler, uv * float2(1.20, 1.46) + float2(-Time * 0.22 * Direction, Time * 0.10)).r;
    float ripple = tex2D(DetailSampler, uv * float2(4.10, 2.05) + float2(-Time * 0.56 * Direction, -Time * 0.14)).r;

    float rise = saturate((uv.y - 0.08) * 8.0) * saturate((0.95 - uv.y) * 10.0);
    float pool = saturate((uv.y - 0.70) * 4.5);
    float lean = Direction * (0.07 + (1.0 - uv.y) * 0.075);
    float columnWidth = (.185 + macro * .090) * rise;
    float poolWidth = .360 + macro * .075;
    float width = lerp(columnWidth, poolWidth, pool);
    float body = saturate((width - abs(uv.x - 0.5 - lean)) * 11.0) * rise;

    // Max width is .435 and rise vanishes inside the top/bottom edges: no clipped rectangle.
    float quadFade = saturate((0.47 - abs(uv.x - 0.5)) * 13.0) * rise;
    body *= quadFade;
    float flow = saturate(macro * 0.70 + ripple * 0.56 - 0.14);
    float crest = pool * body * saturate((Direction * (uv.x - 0.5) + 0.16) * 5.8) * saturate(ripple * 1.18 - 0.18);

    float alpha = saturate(body * (0.68 + flow * 0.21) + crest * 0.35) * Opacity * shift;
    float3 color = lerp(DarkColor, MidColor, flow);
    color = lerp(color, CoreColor, crest * 0.76);
    float3 emission = CoreColor * crest * 0.24;
    return float4(color * alpha + emission * (Opacity * shift), alpha) * v;
}

technique QuaraTideRush { pass P { PixelShader = compile ps_2_0 TideRush(); } }
technique QuaraTideRushUndertow { pass P { PixelShader = compile ps_2_0 TideRushUndertow(); } }
