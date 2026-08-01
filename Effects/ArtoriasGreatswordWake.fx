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

float4 GreatswordWakePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.2, 4.2) + float2(-Time * 0.2, Time * 0.05)).r;
    float centerDistance = abs(uv.y - 0.5);
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * (1.65 + Direction * 1.1));
    float torn = saturate(streak * 0.72 + noise * 0.42 - 0.28) * taper;
    float bladeEdge = saturate((0.075 - centerDistance) * 13.33) * taper;
    float fragments = saturate((noise - 0.68) * 3.1) * torn * saturate(centerDistance * 4.0);
    float fade = 1.0 - Progress * 0.5;
    float3 color = lerp(DarkColor, MidColor, torn);
    color = lerp(color, CoreColor, bladeEdge);
    float intensity = fragments * 0.3 + torn * 0.62 + bladeEdge * 1.4;
    return float4(sampleColor.rgb * color * intensity * fade,
        sampleColor.a * saturate(torn + bladeEdge) * Opacity * fade);
}

technique ArtoriasGreatswordWake
{
    pass GreatswordWakePass { PixelShader = compile ps_2_0 GreatswordWakePixel(); }
}
