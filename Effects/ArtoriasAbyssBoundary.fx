// ArtoriasAbyssBoundary.fx
// Circular adaptation of MarilithRoundedFirewall: the same turbulent-noise,
// world-pixel flame construction, recolored into Artorias's violet Abyss palette.

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
float2 WorldDrawSize;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 BoundaryShroudPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 localPosition = (uv - 0.5) * WorldDrawSize;
    float radialDistance = length(localPosition);
    float halfWidth = max(Direction, 1.0);
    float safeRadius = Active - halfWidth;

    // MarilithFireAura's strength is two independently moving samples of the same fine
    // turbulent field. Retain that construction here at a much tighter world scale so the
    // exterior reads as living abyss fog instead of large translated texture cut-outs.
    float2 warpUV = localPosition.yx / 116.0
        + float2(-Time * 0.075, Time * 0.046);
    float2 warp = tex2D(DetailSampler, warpUV).gb - 0.5;
    float fogA = tex2D(PrimarySampler,
        localPosition / 164.0 + warp * 0.18 + float2(Time * 0.043, -Time * 0.118)).r;
    float fogB = tex2D(PrimarySampler,
        localPosition.yx / 91.0 - warp * 0.11 + float2(-Time * 0.092, Time * 0.061)).g;
    float fine = tex2D(DetailSampler,
        localPosition / 58.0 + float2(Time * 0.13, Time * 0.086)).b;

    float outside = saturate((radialDistance - safeRadius) / 18.0);
    float billow = saturate(fogA * 0.68 + fogB * 0.44 + fine * 0.18 - 0.42);
    float veins = saturate((abs(fogA - fogB) - 0.16) * 3.4) * (0.38 + fine * 0.62);
    float pulse = 0.92 + 0.08 * sin(Time * 2.4 + radialDistance * 0.015 + fine * 3.0);

    // Full-opacity forbidden space: motion lives in its color, never in holes that reveal the
    // ordinary world behind it. The 18px transition begins exactly at the damage boundary.
    float alpha = vertexColor.a * outside * Opacity;
    float3 color = lerp(DarkColor, MidColor, billow * 0.34 + veins * 0.20);
    color = lerp(color, CoreColor, veins * 0.075 * pulse);
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 BoundaryEdgePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 localPosition = (uv - 0.5) * WorldDrawSize;
    float radialDistance = length(localPosition);
    float halfThickness = max(Direction, 1.0);
    float safeRadius = Active - halfThickness;
    float signedDistance = radialDistance - safeRadius;
    float boundaryDistance = abs(signedDistance);

    // A circular, outward-facing port of MarilithRoundedFirewall. Unlike the earlier symmetric
    // band, flame height grows only into the damaging exterior, leaving the safe side readable.
    float2 tiledUV = localPosition / 142.0;
    tiledUV.x += Time * 0.055;
    tiledUV.y -= Time * 0.16;
    float flameNoise = tex2D(PrimarySampler, tiledUV).r;
    float breakup = tex2D(DetailSampler,
        localPosition.yx / 74.0 + float2(-Time * 0.11, Time * 0.07)).g;

    // The lethal seam remains centered exactly on signedDistance == 0, but its brightness now
    // rolls off over a wider eased shoulder instead of dropping through a narrow linear ramp.
    float coreBand = 1.0 - smoothstep(1.5, 13.0, boundaryDistance);
    float outerDistance = max(signedDistance, 0.0);
    float flameReach = 28.0 + flameNoise * 82.0;
    float flameBody = saturate(1.0 - outerDistance / flameReach);
    flameBody *= saturate((flameNoise * 0.74 + breakup * 0.46 + flameBody - 0.46) * 1.8);
    flameBody *= saturate((signedDistance + 10.0) / 18.0);
    float lickingEdge = flameBody * saturate((breakup - 0.48) * 2.2);

    float warning = Progress;
    float3 color = DarkColor * flameBody * 0.78;
    color += MidColor * flameBody * flameBody * (1.05 + warning * 0.22);
    color += CoreColor * lickingEdge * 0.42;
    color += float3(0.94, 0.30, 0.82) * coreBand * 0.78;
    color += float3(0.96, 0.76, 1.0) * coreBand * coreBand * 0.32;

    float alpha = saturate(coreBand + flameBody * 0.86) * Opacity;
    return float4(vertexColor.rgb * color, vertexColor.a * alpha);
}

float4 BoundaryInnerPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 localPosition = (uv - 0.5) * WorldDrawSize;
    float radialDistance = length(localPosition);
    float safeRadius = Active - max(Direction, 1.0);
    float signedDistance = radialDistance - safeRadius;
    float innerDistance = max(-signedDistance, 0.0);

    float flow = tex2D(PrimarySampler,
        localPosition / 132.0 + float2(Time * 0.055, -Time * 0.105)).r;
    float breakup = tex2D(DetailSampler,
        localPosition.yx / 78.0 + float2(-Time * 0.09, Time * 0.06)).g;
    float innerReach = 34.0 + flow * 46.0;
    float body = saturate(1.0 - innerDistance / innerReach);
    body *= saturate((flow * 0.72 + breakup * 0.42 + body - 0.52) * 1.7);
    body *= saturate((-signedDistance + 4.0) / 12.0);
    body *= 0.48;

    float3 color = DarkColor * body * 0.58
        + MidColor * body * body * 0.72
        + CoreColor * body * saturate((breakup - 0.62) * 1.9) * 0.12;
    float alpha = saturate(body * 0.78) * Opacity;
    return float4(vertexColor.rgb * color, vertexColor.a * alpha);
}

technique ArtoriasAbyssBoundaryShroud
{
    pass ShroudPass { PixelShader = compile ps_2_0 BoundaryShroudPixel(); }
}

technique ArtoriasAbyssBoundaryEdge
{
    pass EdgePass { PixelShader = compile ps_2_0 BoundaryEdgePixel(); }
}

technique ArtoriasAbyssBoundaryInner
{
    pass InnerPass { PixelShader = compile ps_2_0 BoundaryInnerPixel(); }
}
