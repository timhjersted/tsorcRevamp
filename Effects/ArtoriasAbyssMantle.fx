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

float MantleMask(float2 uv, out float edge)
{
    float2 p = uv - 0.5;
    float fog = tex2D(PrimarySampler, uv + float2(p.y * 0.12, -Time * 0.035 * Direction)).r;
    float noise = tex2D(DetailSampler, uv * 3.4 + float2(Time * 0.07, -Time * 0.1)).r;
    float radius = length(float2(p.x * 1.18, p.y * 0.82));
    float shell = saturate((0.51 - radius) * 5.0) * saturate((radius - 0.08) * 7.0);
    float body = saturate(fog * 0.64 + noise * 0.48 - 0.38) * shell;
    edge = saturate((body - 0.38) * 2.4) * (0.6 + noise * 0.4);
    return body;
}

float4 MantleBodyPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float edge;
    float body = MantleMask(uv, edge);
    float center = saturate((0.34 - length(uv - 0.5)) * 3.0) * Active;
    float3 color = lerp(DarkColor, MidColor, edge * 0.36);
    float alpha = (body * 0.66 + center * 0.16) * Opacity;
    alpha *= sampleColor.a;
    return float4(sampleColor.rgb * color * alpha, alpha);
}

float4 MantleEdgePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float edge;
    float body = MantleMask(uv, edge);
    float silver = edge * edge;
    float3 color = lerp(MidColor, CoreColor, silver);
    float alpha = edge * Opacity * 0.58;
    return float4(sampleColor.rgb * color * (edge * 0.62 + silver), sampleColor.a * alpha);
}

technique ArtoriasMantleBody
{
    pass MantleBodyPass { PixelShader = compile ps_2_0 MantleBodyPixel(); }
}
technique ArtoriasMantleEdge
{
    pass MantleEdgePass { PixelShader = compile ps_2_0 MantleEdgePixel(); }
}
