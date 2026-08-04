// GreatRedKnightDominion.fx
// Side-view arena control: textured exterior containment, turbulent sweep/execution lanes,
// and a filled final nova. Compiles for tModLoader's Reach profile.

sampler2D TextureSampler : register(s0);
sampler2D MarbleNoise : register(s1);
sampler2D TurbulentNoise : register(s2);
sampler2D VeinNoise : register(s3);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Intensity;
float Active;
float RadiusRatio;
float Direction;
float2 DrawSize;

struct PixelInput
{
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float hashNoise(float2 uv)
{
    float marble = tex2D(MarbleNoise, uv).r;
    float turbulent = tex2D(TurbulentNoise, uv * 1.37 + float2(0.17, 0.31)).r;
    return saturate(marble * 0.58 + turbulent * 0.62 - 0.13);
}

float4 DominionFieldPS(PixelInput input) : COLOR0
{
    float2 p = input.TexCoord - 0.5;
    float radius = length(p);
    float2 slowUV = p * 2.35 + float2(Time * 0.017, -Time * 0.026);
    float cloud = hashNoise(slowUV);
    float boundaryNoise = (cloud - 0.5) * 0.006;
    float outside = smoothstep(RadiusRatio - 0.003 + boundaryNoise,
        RadiusRatio + 0.012 + boundaryNoise, radius);
    float depth = smoothstep(RadiusRatio, RadiusRatio + 0.16, radius);
    float3 color = lerp(DarkColor, MidColor, cloud * 0.82);
    color *= lerp(0.54, 0.92, depth) * Intensity;
    // Alpha is driven only by the boundary mask (outside) and the caller's Opacity, not by the cloud
    // noise — otherwise the wall always had faint see-through troughs even at full Opacity.
    float alpha = outside * Opacity;
    return float4(color, alpha) * input.Color;
}

float4 DominionEdgePS(PixelInput input) : COLOR0
{
    float2 p = input.TexCoord - 0.5;
    float radius = length(p);
    float angleWarp = atan2(p.y, p.x) / 6.2831853;
    float radialNoise = tex2D(TurbulentNoise,
        float2(angleWarp * 5.0 + Time * 0.045, radius * 13.0 - Time * 0.035)).r;
    float fineNoise = tex2D(VeinNoise,
        float2(angleWarp * 8.0 - Time * 0.032, radius * 21.0 + Time * 0.02)).r;
    float displacedRadius = RadiusRatio + (radialNoise - 0.5) * 0.006;
    float distanceToEdge = abs(radius - displacedRadius);
    float haze = 1.0 - smoothstep(0.003, 0.016, distanceToEdge);
    float core = 1.0 - smoothstep(0.0008, 0.0038, abs(radius - RadiusRatio));
    float broken = saturate(0.42 + radialNoise * 0.72 + fineNoise * 0.34);
    float3 color = MidColor * haze * broken + CoreColor * core;
    float alpha = saturate(haze * 0.64 + core) * Opacity;
    return float4(color * Intensity, alpha) * input.Color;
}

float4 DominionLancePS(PixelInput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float along = uv.x;
    float across = abs(uv.y - 0.5) * 2.0;
    float startFade = smoothstep(0.0, 0.07, along);
    float endFade = 1.0 - smoothstep(0.91, 1.0, along);
    float longitudinal = startFade * endFade;
    float2 flowUV = float2(along * 4.8 - Time * (0.72 + Active * 0.46) * Direction,
        uv.y * 2.6 + Time * 0.045);
    float flow = tex2D(VeinNoise, flowUV).r;
    float raggedEdge = 0.62 + (flow - 0.5) * 0.46;
    float body = 1.0 - smoothstep(raggedEdge, 1.0, across);
    float coreWidth = lerp(0.09, 0.24, Active);
    float core = 1.0 - smoothstep(coreWidth, coreWidth + 0.10, across);
    float reveal = smoothstep(along - 0.12, along + 0.08, Progress);
    float3 color = DarkColor * body + MidColor * body * (0.52 + flow * 0.78);
    color += CoreColor * core * lerp(0.14 + flow * 0.12, 1.35, Active);
    float alpha = longitudinal * reveal * body * Opacity * lerp(0.54, 1.0, Active);
    return float4(color * Intensity, alpha) * input.Color;
}

float4 DominionNovaPS(PixelInput input) : COLOR0
{
    float2 p = input.TexCoord - 0.5;
    float radius = length(p);
    float2 inwardUV = p * lerp(5.2, 3.1, Progress)
        + float2(-Time * 0.075, Time * 0.052);
    float cloud = hashNoise(inwardUV);
    float inside = 1.0 - smoothstep(RadiusRatio - 0.012, RadiusRatio, radius);
    float rim = 1.0 - smoothstep(0.002, 0.018, abs(radius - RadiusRatio));
    float inward = saturate(1.0 - radius / max(RadiusRatio, 0.001));
    float body = inside * saturate(0.38 + cloud * 0.86);
    float ignition = lerp(0.22 + 0.36 * Progress, 1.0, Active);
    float3 color = lerp(DarkColor, MidColor, cloud) * body;
    color += CoreColor * (rim * lerp(0.35, 1.35, Active) + inward * cloud * Active * 0.54);
    float alpha = inside * Opacity * ignition * saturate(0.68 + cloud * 0.48);
    return float4(color * Intensity, alpha) * input.Color;
}

technique DominionField
{
    pass Pass1 { PixelShader = compile ps_2_0 DominionFieldPS(); }
}

technique DominionEdge
{
    pass Pass1 { PixelShader = compile ps_2_0 DominionEdgePS(); }
}

technique DominionLance
{
    pass Pass1 { PixelShader = compile ps_2_0 DominionLancePS(); }
}

technique DominionNova
{
    pass Pass1 { PixelShader = compile ps_2_0 DominionNovaPS(); }
}
