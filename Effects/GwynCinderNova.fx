sampler SmoothNoise : register(s0);
sampler BrokenNoise : register(s1);

float3 OuterColor;
float3 FlameColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float RingRadius;
float RingHalfThickness;
float TrailLength;

float4 GwynCinderNovaPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Recover stable 0..1 coordinates from SpriteBatch's source-texture coordinates, then measure
    // the ring in screen pixels. RingRadius and RingHalfThickness therefore use the same units as
    // the projectile's annulus collision instead of relying on a scaled texture approximation.
    float2 safeDrawSize = max(DrawSize, float2(1.0, 1.0));
    float2 uv = coords * PrimaryTextureSize / safeDrawSize;
    float2 pixelFromCenter = (uv - float2(0.5, 0.5)) * safeDrawSize;
    float radialDistance = length(pixelFromCenter);
    float frontDistance = radialDistance - RingRadius;

    // Counter-scrolling planar fields are bent by radial distance. This keeps the fire rolling
    // around the circumference without the expensive polar-angle math that exceeds Reach limits.
    float2 centeredUV = uv - float2(0.5, 0.5);
    float2 broadPoint = centeredUV * 2.35
        + float2(radialDistance * 0.008 - Time * 0.16, -radialDistance * 0.012 - Time * 0.42);
    float2 detailPoint = centeredUV.yx * 5.10
        + float2(-radialDistance * 0.018 + Time * 0.21, radialDistance * 0.026 + Time * 0.30);
    float broadNoise = tex2D(SmoothNoise, broadPoint).r;
    float detailNoise = tex2D(BrokenNoise, detailPoint).r;
    float breakup = saturate(broadNoise * 0.85 + detailNoise * 0.55 - 0.38);

    // The bright band never moves away from the real damage front. Turbulence is confined to a
    // trailing inner wake, so the decorative fire cannot imply damage ahead of the collision.
    float absoluteFrontDistance = abs(frontDistance);
    float band = saturate((RingHalfThickness - absoluteFrontDistance)
        / max(RingHalfThickness * 0.28, 1.0));
    float core = saturate((RingHalfThickness * 0.28 - absoluteFrontDistance)
        / max(RingHalfThickness * 0.28 - 3.0, 1.0));
    float wakeDistance = saturate(-frontDistance / max(TrailLength, 1.0));
    float wakeEnvelope = saturate((wakeDistance - 0.02) / 0.16)
        * saturate((1.0 - wakeDistance) / 0.45);
    float wake = wakeEnvelope * breakup;

    float body = band * (0.62 + breakup * 0.65);
    float heat = max(max(body, wake * 0.72), core * 1.35);
    float3 color = lerp(OuterColor, FlameColor, saturate(band * 0.82 + breakup * 0.22));
    color = lerp(color, CoreColor, core);

    // Additive SpriteBatch applies source alpha to RGB, so opacity belongs in alpha only.
    float alpha = saturate(band * 0.82 + core * 0.90 + wake * 0.48) * Opacity;
    return float4(sampleColor.rgb * color * heat, sampleColor.a * alpha);
}

technique GwynCinderNova
{
    pass GwynCinderNovaPass
    {
        PixelShader = compile ps_2_0 GwynCinderNovaPixel();
    }
}
