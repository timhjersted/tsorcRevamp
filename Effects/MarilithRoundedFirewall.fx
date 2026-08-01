sampler uImage0 : register(s0);

float uTime;
float2 uArenaSize;
float uCornerRadius;
float uWallThickness;
float uVisualPadding;
float uProgress;
float uCloudProgress;

float RoundedRectangleDistance(float2 localPoint, float2 halfSize, float radius)
{
    float2 innerHalfSize = max(halfSize - radius, 0);
    float2 q = abs(localPoint) - innerHalfSize;
    return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 canvasSize = uArenaSize + uVisualPadding * 2;
    float2 localPosition = (coords - 0.5) * canvasSize;
    float boundaryDistance = abs(RoundedRectangleDistance(localPosition, uArenaSize * 0.5, uCornerRadius));

    float2 tiledUV = localPosition / 220.0;
    tiledUV.x += uTime * 0.055;
    tiledUV.y -= uTime * 0.16;
    float flameNoise = tex2D(uImage0, tiledUV).r;

    float halfThickness = uWallThickness * 0.5;
    float coreBand = saturate((halfThickness - boundaryDistance) * 0.032);
    float flameBody = saturate(1 - (boundaryDistance - halfThickness) / (48 + flameNoise * 105));
    flameBody *= saturate((flameNoise + flameBody - 0.36) * 1.7);

    float3 color = float3(0.55, 0.018, 0.002) * flameBody;
    color += float3(1.0, 0.17, 0.008) * flameBody * flameBody;
    color += float3(1.0, 0.72, 0.16) * coreBand;
    color += float3(1.0, 0.96, 0.70) * coreBand * coreBand;

    // Preserve the old pale upper-wall phase cue without bleaching the lower boundary.
    float cloudBlend = saturate(uCloudProgress * -localPosition.y / uArenaSize.y * 4.8);
    color = lerp(color, float3(0.82, 0.76, 0.72) * (coreBand + flameBody), cloudBlend * 0.65);

    float fadeIn = uProgress * uProgress * (3 - 2 * uProgress);
    return float4(color * fadeIn, saturate(coreBand + flameBody) * fadeIn);
}

technique MarilithRoundedFirewall
{
    pass MarilithRoundedFirewallPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
