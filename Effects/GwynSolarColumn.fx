sampler PrimaryNoise : register(s0);
sampler SecondaryNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float EdgeTurbulence;
float CoreStrength;

float4 GwynSolarColumnPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // SpriteBatch texture coordinates are expressed in source-texture space. Convert them back
    // into a stable 0..1 rectangle so changing the column's width does not change its animation.
    float2 safeDrawSize = max(DrawSize, float2(1.0, 1.0));
    float2 uv = coords * PrimaryTextureSize / safeDrawSize;

    // X runs from the ground toward the sky because the strip is rotated by -90 degrees in C#.
    float axial = saturate(uv.x);
    float across = abs(uv.y * 2.0 - 1.0);

    // Two differently scaled, counter-scrolling noise fields stop the column from reading as a
    // stretched laser texture. Both inputs tile, so the effect remains seamless at any height.
    float2 flowA = float2(uv.x * 1.35 - Time * 0.72, uv.y * 2.65 + Time * 0.10);
    float2 flowB = float2(uv.x * 2.10 + Time * 0.43, uv.y * 4.20 - Time * 0.16);
    float broadNoise = tex2D(PrimaryNoise, flowA).r;
    float fineNoise = tex2D(SecondaryNoise, flowB).r;
    float turbulence = saturate(broadNoise * 0.68 + fineNoise * 0.58 - 0.22);

    // Noise deforms the silhouette while a narrow independent core preserves the white-hot read.
    float edge = saturate(1.04 - across + (turbulence - 0.5) * EdgeTurbulence);
    float body = pow(edge, 1.65);
    float core = pow(saturate(1.0 - across * 4.25), 2.2) * (0.72 + fineNoise * 0.28);

    // Keep the impact rooted, then dissolve the high end into the sky instead of ending on a line.
    float groundFade = smoothstep(0.0, 0.025, axial);
    float skyFade = 1.0 - smoothstep(0.74, 1.0, axial);
    float axialFade = groundFade * skyFade;

    float bodyHeat = body * (0.38 + turbulence * 0.92);
    float coreHeat = core * CoreStrength;
    float intensity = (bodyHeat + coreHeat) * axialFade;

    float3 color = lerp(OuterColor, MiddleColor, saturate(body * 0.78 + turbulence * 0.30));
    color = lerp(color, CoreColor, saturate(core * 1.35));

    float alpha = saturate((body * 0.72 + core) * axialFade * Opacity);
    return float4(sampleColor.rgb * color * intensity * Opacity, sampleColor.a * alpha);
}

technique GwynSolarColumn
{
    pass GwynSolarColumnPass
    {
        PixelShader = compile ps_2_0 GwynSolarColumnPixel();
    }
}
