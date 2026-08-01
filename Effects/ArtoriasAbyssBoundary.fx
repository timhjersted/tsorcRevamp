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

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 BoundaryPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float radius = length(centered);
    float noise = tex2D(DetailSampler, centered * 5.2 + float2(Time * 0.09, -Time * 0.07)).r;
    float ringRadius = Active * 0.5;
    float halfWidth = max(Direction, 0.006);
    float distanceToRing = abs(radius - ringRadius);
    float lethalBand = saturate(1.0 - distanceToRing / halfWidth);
    float tornBand = lethalBand * saturate(noise * 1.55 - 0.28);
    float outerShroud = saturate(1.0 - distanceToRing / (halfWidth * 2.1)) * (0.24 + noise * 0.36);
    float innerEdgeDistance = abs(radius - (ringRadius - halfWidth * 0.82));
    float silverEdge = saturate(1.0 - innerEdgeDistance / (halfWidth * 0.13));
    float warning = Progress;
    float3 color = lerp(DarkColor, MidColor, tornBand);
    color = lerp(color, CoreColor, silverEdge);
    float intensity = outerShroud * 0.22 + tornBand * (0.62 - warning * 0.24) + silverEdge * (1.45 - warning * 0.62);
    float alpha = saturate(outerShroud * 0.42 + lethalBand * 0.92 + silverEdge) * Opacity;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

technique ArtoriasAbyssBoundary
{
    pass BoundaryPass { PixelShader = compile ps_2_0 BoundaryPixel(); }
}
