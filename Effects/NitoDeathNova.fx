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

float4 DeathRingPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float radius = length(centered);
    float noise = tex2D(DetailSampler, centered * 3.8 + float2(-Time * 0.12, Time * 0.08)).r;
    float ringRadius = Active * 0.5 + (noise - 0.5) * 0.014;
    float halfWidth = max(Direction, 0.006);
    float ringDistance = abs(radius - ringRadius);
    float inverseWidth = 1.0 / halfWidth;
    float outer = saturate(1.0 - ringDistance * inverseWidth * 0.37);
    float body = saturate(1.0 - ringDistance * inverseWidth);
    float edge = saturate(1.0 - ringDistance * inverseWidth * 3.33);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, edge);
    float intensity = outer * 0.22 + body * 0.9 + edge * 1.65;
    return float4(color * intensity, saturate(outer * 0.45 + body + edge) * Opacity) * vertexColor;
}

technique NitoDeathRing
{
    pass Pass1 { PixelShader = compile ps_2_0 DeathRingPixel(); }
}
