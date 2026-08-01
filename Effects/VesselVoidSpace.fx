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

float4 VoidSpacePixel(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float aspect = DrawSize.x / max(DrawSize.y, 1.0);
    float2 centered = uv - 0.5;
    centered.x *= aspect;
    float flow = tex2D(PrimarySampler, uv * float2(1.45, 1.1) + float2(Time * 0.012, -Time * 0.018)).r;
    float organic = tex2D(DetailSampler, uv * float2(2.8, 2.1) + float2(-Time * 0.025, Time * 0.011)).r;
    float current = saturate(flow * 0.62 + organic * 0.48 - 0.38);
    float throat = saturate((0.72 - length(centered)) * 1.15);
    float pale = saturate((organic - 0.74) * 3.85) * current;
    float3 color = lerp(DarkColor, MidColor, current * 0.54);
    color = lerp(color, CoreColor, pale * 0.26);
    float alpha = saturate(0.38 + current * 0.42 + throat * 0.18) * Opacity;
    return float4(sampleColor.rgb * color * (0.58 + current * 0.34), sampleColor.a * alpha);
}

technique VesselVoidSpace
{
    pass VoidSpacePass { PixelShader = compile ps_2_0 VoidSpacePixel(); }
}
