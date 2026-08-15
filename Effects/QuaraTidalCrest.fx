// QuaraTidalCrest.fx
// The Hydromancer's ground-hugging breaking wave. The primary sprite owns the side-view silhouette;
// these passes only add flowing water material inside it. Both return premultiplied alpha, so a
// transparent texel can never paint the rectangular draw quad.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize, PixelDrawSize;
float4 PixelGrid, uSourceRect;

float2 PixelateFrameUV(float2 uv)
{
    return (floor(uv * PixelGrid.xy) + 0.5) * PixelGrid.zw;
}

// A — Flowing Crest. Keeps the sprite's chunky breaking lip, then gives its body independently
// drifting water strata. Frame UV is quantized before either shape or noise work, so the complete
// material—not merely its texture sampling—uses deliberate 2px gameplay pixels.
float4 TidalCrest(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 frameUV = (c - uSourceRect.xy) / max(uSourceRect.zw, float2(0.001, 0.001));
    frameUV = PixelateFrameUV(frameUV);
    float2 sampleUV = uSourceRect.xy + frameUV * uSourceRect.zw;
    float4 wave = tex2D(PrimarySampler, sampleUV);

    float n1 = tex2D(DetailSampler, frameUV * float2(1.55, 1.10) + float2(-Time * 0.13 * Direction, Time * 0.06)).r;
    float n2 = tex2D(DetailSampler, frameUV * float2(3.40, 2.35) + float2(-Time * 0.33 * Direction, Time * 0.16)).r;
    float flow = saturate(n1 * 0.72 + n2 * 0.54 - 0.16);
    float crest = saturate(1.0 - frameUV.y * 1.45);
    float foam = wave.a * crest * saturate(n2 * 1.26 + n1 * 0.22 - 0.32);

    float3 water = lerp(DarkColor, MidColor, flow);
    water = lerp(water, CoreColor, foam * 0.72);
    float3 color = lerp(wave.rgb, water, 0.45);
    float alpha = wave.a * Opacity * (0.78 + foam * 0.22);
    return float4(color * alpha, alpha) * v;
}

// B — Deep Curl. More stormy and weighty: a darker water body with bright foam confined to the
// forward crest. It uses the same sprite silhouette and 2px grid, so it can be selected by name
// with no collision or timing change.
float4 TidalCrestDeepCurl(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 frameUV = (c - uSourceRect.xy) / max(uSourceRect.zw, float2(0.001, 0.001));
    frameUV = PixelateFrameUV(frameUV);
    float2 sampleUV = uSourceRect.xy + frameUV * uSourceRect.zw;
    float4 wave = tex2D(PrimarySampler, sampleUV);

    float macro = tex2D(DetailSampler, frameUV * float2(1.18, 1.42) + float2(-Time * 0.17 * Direction, Time * 0.09)).r;
    float ripple = tex2D(DetailSampler, frameUV * float2(3.72, 1.86) + float2(-Time * 0.41 * Direction, -Time * 0.12)).r;
    float depth = saturate(macro * 0.76 + ripple * 0.42 - 0.12);
    float crest = saturate(1.0 - frameUV.y * 1.70);
    float front = saturate((Direction * (frameUV.x - 0.5) + 0.08) * 3.8);
    float foam = wave.a * crest * front * saturate(ripple * 1.22 - macro * 0.12 - 0.24);

    float3 water = lerp(DarkColor, MidColor, depth * 0.76);
    water = lerp(water, CoreColor, foam * 0.84);
    float3 color = lerp(wave.rgb * 0.72, water, 0.74);
    float alpha = wave.a * Opacity * (0.80 + foam * 0.20);
    return float4(color * alpha, alpha) * v;
}

technique QuaraTidalCrest { pass P { PixelShader = compile ps_2_0 TidalCrest(); } }
technique QuaraTidalCrestDeepCurl { pass P { PixelShader = compile ps_2_0 TidalCrestDeepCurl(); } }
