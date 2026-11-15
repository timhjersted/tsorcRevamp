#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float Opacity;
float Time;
float3 BeamColor;
float4 PixelGrid;
float4 NoiseScale;

float4 MawPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelated giant maw beam with origin cone taper at the jaws and GenericLaser turbulent aliveness.
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float distance = abs(uv.y - 0.5);

    float originProgress = saturate((uv.x - 0.01) * NoiseScale.w);
    float coneProfile = sqrt(originProgress);
    float coneWidth = lerp(0.10, 1.0, coneProfile);
    float scaledDistance = distance / coneWidth;
    float originCap = saturate(originProgress * 3.5);
    float tip = saturate((0.985 - uv.x) * 14.0);

    float2 samplePoint = uv * NoiseScale.xy + float2(NoiseScale.z, Time * 0.06);
    float turb1 = tex2D(PrimarySampler, samplePoint * 0.85).r;
    float turb2 = tex2D(PrimarySampler, samplePoint * 0.32 + float2(NoiseScale.z * 0.45, -Time * 0.10)).g;
    float flow = saturate(turb1 * 0.55 + turb2 * 0.65 - 0.10);
    float surge = saturate(flow * 1.45);

    float edgeGuard = saturate((0.46 - scaledDistance) * 8.0);
    float fade = coneProfile * originCap * tip * edgeGuard * Opacity;

    float outerReach = 0.26 + 0.19 * flow;
    float sheath = saturate((outerReach - scaledDistance) * 6.5) * (0.25 + 1.20 * surge) * fade;
    float body = saturate((0.20 + 0.10 * flow - scaledDistance) * 10.5) * (0.45 + 0.90 * surge) * fade * coneProfile;
    float core = saturate((0.13 + 0.05 * flow - scaledDistance) * 16.0) * (0.85 + 0.35 * turb1) * fade * coneProfile * tip;

    float3 rgb = BeamColor * sheath
        + float3(1.0, 0.44, 0.06) * body
        + float3(1.0, 0.98, 0.86) * core;
    float alpha = saturate(sheath * 0.85 + body + core);
    return float4(saturate(rgb), alpha) * sampleColor.a;
}

float4 HullLaserPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelated copy of GenericLaser (both telegraph and attack) with a conical origin taper at the emitter.
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float distance = abs(uv.y - 0.5);

    float originProgress = saturate((uv.x - 0.01) * NoiseScale.w);
    float coneProfile = sqrt(originProgress);
    float coneWidth = lerp(0.08, 1.0, coneProfile);
    float scaledDistance = distance / coneWidth;

    float baseIntensity = saturate(1.0 - scaledDistance);
    float i2 = baseIntensity * baseIntensity;
    float edgeGuard = saturate((0.46 - scaledDistance) * 8.0);
    float endTaper = saturate((0.985 - uv.x) * 10.0);
    float intensity = i2 * i2 * i2 * coneProfile * endTaper * edgeGuard;

    float2 samplePoint = uv * NoiseScale.xy + float2(NoiseScale.z, 0.0);
    float sampleIntensity = tex2D(PrimarySampler, samplePoint).r;
    float detailIntensity = tex2D(PrimarySampler, samplePoint * 0.45 + float2(NoiseScale.z * 0.4, 0.17)).g;
    float liveNoise = saturate(sampleIntensity * 0.65 + detailIntensity * 0.55 - 0.08);

    float envelope = pow(intensity, 1.5) * 2.0 * Opacity;
    float i4 = intensity * intensity;
    float whiteCore = i4 * i4 * (0.30 + 0.95 * liveNoise) * Time;

    float3 rgb = BeamColor * liveNoise * envelope + float3(1.0, 0.98, 0.90) * whiteCore;
    float alpha = saturate(envelope + whiteCore);
    return float4(rgb, alpha) * sampleColor.a;
}

technique DestroyerMawBeam
{
    pass MawPass
    {
        PixelShader = compile ps_2_0 MawPixel();
    }
}

technique DestroyerHullLaser
{
    pass HullLaserPass
    {
        PixelShader = compile ps_2_0 HullLaserPixel();
    }
}
