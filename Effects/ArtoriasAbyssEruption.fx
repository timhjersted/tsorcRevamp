#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;
float4 PixelGrid;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 RiftPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float crack = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * 3.6 + float2(Time * 0.08, -Time * 0.035)).r;
    float footprint = saturate((0.5 - length(float2(centered.x, centered.y * 2.8))) * 10.0);
    float reveal = saturate((Progress + 0.12 - abs(centered.x) * 1.75) * 7.0);
    float fissure = saturate((crack - 0.24 + noise * 0.15) * 3.0) * footprint * reveal;
    float core = saturate((crack - 0.68) * 4.5) * footprint * reveal;
    float3 color = lerp(DarkColor, MidColor, fissure);
    color = lerp(color, CoreColor, core);
    return float4(sampleColor.rgb * color * (fissure * 0.74 + core * 1.45),
        sampleColor.a * saturate(fissure + core) * Opacity);
}

float4 EruptionPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float smoke = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.5, 4.2) + float2(Time * 0.055, -Time * 0.24)).r;
    float x = abs(uv.x - 0.5);
    float rise = saturate((Progress - (1.0 - uv.y) * 0.72) * 6.0);
    float plume = saturate(smoke * 0.68 + noise * 0.46 - 0.39) * rise;
    float exactCore = saturate((0.39 - x) * 6.0) * rise;
    float silverChannel = saturate((0.12 - x) * 8.33) * rise;
    float3 color = lerp(DarkColor, MidColor, plume + exactCore);
    color = lerp(color, CoreColor, silverChannel);
    float intensity = plume * 0.35 + exactCore * 0.68 + silverChannel * 1.35;
    return float4(sampleColor.rgb * color * intensity,
        sampleColor.a * saturate(plume * 0.52 + exactCore + silverChannel) * Opacity);
}

// AbyssShard's warning portal is a side-view breach rising out of the tile surface. It deliberately
// has no ellipse, ring, rectangle fill, or static spike mask: two independently moving fields drive
// its height, side reach, filaments, and protrusions. Direction is a deterministic per-projectile
// phase so simultaneous shard clusters do not display the same stamp.
float4 AbyssShardPortalPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Quantise before every sample and every silhouette calculation. PixelGrid is built from the
    // final 80x52 world draw, so this is a real 2x2 gameplay-pixel filter rather than texture-scale
    // pixelation.
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float x = uv.x - 0.5;
    float y = 1.0 - uv.y; // height above the grounded bottom edge
    // Both fields travel upward, but at different speeds and spatial frequencies. Their horizontal
    // drift opposes so clusters never settle into one repeating vertical curtain.
    float macro = tex2D(PrimarySampler,
        float2(uv.x * 1.72 + Time * 0.055 + Direction, uv.y * 0.58 + Time * 0.31 + Direction * 0.37)).r;
    float detail = tex2D(DetailSampler,
        float2(uv.x * 2.86 - Time * 0.093 - Direction * 0.41, uv.y * 0.82 + Time * 0.57 + Direction)).g;

    float reveal = Progress * (2.0 - Progress);
    // A separate height at every x makes the top resolve into uneven rising spikes. Its proven
    // maximum is 0.94 of the quad, leaving clear space before the top edge. Detail has enough
    // authority here to produce several narrow 1-3-tile peaks instead of one low rounded mound.
    float top = reveal * (0.42 + macro * 0.27 + detail * 0.24);
    float heightMask = saturate((top - y) * 9.0);

    // Narrow toward the top. Fine turbulence briefly pushes the reach outward on both sides, while
    // the low-frequency layer keeps those protrusions connected to one readable portal body.
    float width = 0.10 + (1.0 - y) * 0.25;
    float material = macro * 0.66 + detail * 0.44;
    float reach = width * (0.42 + material * 0.69);
    float sideTeeth = saturate(detail * 2.18 - 1.24);
    reach += sideTeeth * (0.035 + (1.0 - y) * 0.018);
    float body = saturate((reach - abs(x)) * 13.5) * heightMask;

    float filament = saturate(detail * 2.65 + macro * 0.50 - 1.42) * body;

    float alpha = body * Opacity;
    float3 color = DarkColor * (body * 0.95)
        + MidColor * (body * (0.18 + material * 0.40))
        + CoreColor * (filament * 0.92);
    return float4(color * Opacity, alpha);
}

technique ArtoriasGroundRift
{
    pass RiftPass { PixelShader = compile ps_2_0 RiftPixel(); }
}

technique ArtoriasAbyssEruption
{
    pass EruptionPass { PixelShader = compile ps_2_0 EruptionPixel(); }
}

technique ArtoriasAbyssShardPortal
{
    pass PortalPass { PixelShader = compile ps_2_0 AbyssShardPortalPixel(); }
}
