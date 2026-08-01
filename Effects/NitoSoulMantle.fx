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

float4 SoulMantlePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float radius = length(centered);
    float flow = Direction;
    float2 flowUV = uv + float2(centered.y * 0.16, -Time * 0.055 * flow);
    float shroud = tex2D(PrimarySampler, uv + float2(0.0, -Time * 0.018 * flow)).r;
    float noise = tex2D(DetailSampler, flowUV * 3.3).r;
    float shell = saturate((0.51 - radius) * 4.3) * saturate((radius - 0.11) * 6.0);
    float wisps = saturate(shroud * 0.66 + noise * 0.48 - 0.43) * shell;
    float inner = saturate((0.35 - radius) * 2.9) * (0.22 + noise * 0.34) * Active;
    float3 color = lerp(DarkColor, MidColor, wisps);
    color = lerp(color, CoreColor, inner * 0.3);
    float alpha = (wisps * 0.78 + inner * 0.3) * Opacity;
    return float4(color * alpha, alpha) * vertexColor;
}

technique NitoSoulMantle
{
    pass Pass1 { PixelShader = compile ps_2_0 SoulMantlePixel(); }
}
