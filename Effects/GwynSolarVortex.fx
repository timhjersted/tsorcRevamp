sampler SmoothNoise : register(s0);
sampler BrokenNoise : register(s1);

float3 BoundaryColor;
float3 StreamColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float PullRadius;
float InnerRadius;

float4 GwynSolarVortexPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 safeDrawSize = max(DrawSize, float2(1.0, 1.0));
    float2 uv = coords * PrimaryTextureSize / safeDrawSize;
    float2 pixelFromCenter = (uv - float2(0.5, 0.5)) * safeDrawSize;
    float radialDistance = length(pixelFromCenter);
    float safePullRadius = max(PullRadius, 1.0);
    float2 fieldPoint = pixelFromCenter / safePullRadius;
    float radial01 = saturate(radialDistance / safePullRadius);

    // The outer rim is a faint but exact marker of the live pull radius. Streams begin outside the
    // inner deadzone and never suggest that the center itself is another damaging projectile.
    float outerBoundary = saturate((9.0 - abs(radialDistance - PullRadius)) / 6.5);
    float innerBoundary = saturate((8.0 - abs(radialDistance - InnerRadius)) / 6.0);
    float insideField = saturate((PullRadius - radialDistance) / 12.0);
    float outsideCore = saturate((radialDistance - InnerRadius) / 34.0);
    float fieldMask = insideField * outsideCore;

    // Rotate the sampling basis toward the tangent as it approaches the center. Two differently
    // scaled fields then form broken, inward-bending streams without costly polar trigonometry.
    float2 tangent = float2(-fieldPoint.y, fieldPoint.x);
    float2 spiralPoint = fieldPoint * (3.4 + radial01 * 1.8) + tangent * (1.25 - radial01 * 0.65);
    float broadNoise = tex2D(SmoothNoise, spiralPoint + float2(-Time * 0.18, Time * 0.62)).r;
    float brokenNoise = tex2D(BrokenNoise, spiralPoint.yx * 1.85 + float2(Time * 0.27, Time * 0.88)).r;
    float streams = saturate(abs(broadNoise - brokenNoise) * 2.6 - 0.34) * fieldMask;

    float radialFalloff = 1.0 - radial01;
    float coreHalo = saturate((InnerRadius - radialDistance) / max(InnerRadius * 0.65, 1.0));
    float intensity = streams * (0.18 + radialFalloff * 0.68)
        + outerBoundary * 0.38 + innerBoundary * 0.16 + coreHalo * 0.22;
    float3 color = lerp(BoundaryColor, StreamColor, saturate(streams * 1.7 + radialFalloff * 0.25));
    color = lerp(color, CoreColor, coreHalo * 0.72);

    float alpha = saturate(streams * 0.68 + outerBoundary * 0.42
        + innerBoundary * 0.16 + coreHalo * 0.20) * Opacity;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

technique GwynSolarVortex
{
    pass GwynSolarVortexPass
    {
        PixelShader = compile ps_2_0 GwynSolarVortexPixel();
    }
}
