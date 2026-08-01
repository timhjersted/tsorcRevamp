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

float4 GravefallTrailPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.6, 3.4) + float2(-Time * 0.2, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.2);
    float fringe = saturate(streak * 0.72 + noise * 0.38 - 0.3) * taper;
    float core = saturate((streak - 0.64) * 3.0) * taper;
    float3 color = lerp(DarkColor, MidColor, fringe);
    color = lerp(color, CoreColor, core);
    return float4(color * (fringe * 0.7 + core * 1.35), saturate(fringe + core) * Opacity) * vertexColor;
}

float4 GraveSkyPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float ellipse = length(float2(centered.x, centered.y * 2.8));
    float noise = tex2D(DetailSampler, uv * float2(2.2, 4.0) + float2(Time * 0.12, -Time * 0.04)).r;
    float ringDistance = abs(ellipse - 0.31 - (noise - 0.5) * 0.025);
    float veil = saturate((0.26 - ellipse) * 4.0) * (0.25 + noise * 0.25);
    float fringe = saturate((0.1 - ringDistance) * 10.0);
    float ring = saturate((0.034 - ringDistance) * 29.4);
    float core = saturate((0.011 - ringDistance) * 90.9);
    float charge = 0.68 + Progress * 0.32;
    float3 color = lerp(DarkColor, MidColor, ring + veil);
    color = lerp(color, CoreColor, core);
    float alpha = saturate(fringe * 0.38 + ring + core + veil) * Opacity * charge;
    return float4(color * (fringe * 0.22 + ring + core * 1.45 + veil) * charge, alpha) * vertexColor;
}

technique NitoGravefallTrail
{
    pass Pass1 { PixelShader = compile ps_2_0 GravefallTrailPixel(); }
}

technique NitoGraveSky
{
    pass Pass1 { PixelShader = compile ps_2_0 GraveSkyPixel(); }
}
