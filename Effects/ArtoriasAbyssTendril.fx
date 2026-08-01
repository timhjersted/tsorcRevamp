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

float4 TendrilPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float noise = tex2D(DetailSampler, uv * float2(3.0, 5.0) + float2(-Time * 0.13, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float wave = sin(uv.x * 18.8496 + Time * 7.5) * 0.055 * taper;
    float strandDistance = abs(uv.y - 0.5 - wave);
    float centerStrand = saturate((0.026 - strandDistance) * 38.46);
    float sideStrands = saturate((0.022 - abs(strandDistance - 0.085)) * 45.45);
    float strands = saturate(centerStrand + sideStrands) * taper;
    float pulsePosition = frac(Time * (0.55 + Active * 0.4));
    float tensionPulse = saturate((0.12 - abs(uv.x - pulsePosition)) * 8.33) * strands;
    float fringe = strands * (0.62 + noise * 0.38);
    float3 color = lerp(DarkColor, MidColor, fringe);
    color = lerp(color, CoreColor, tensionPulse);
    return float4(sampleColor.rgb * color * (fringe * 0.62 + tensionPulse * 1.25),
        sampleColor.a * strands * Opacity);
}

float4 TendrilTipPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float radius = length(uv - 0.5) * 2.0;
    float noise = tex2D(DetailSampler, uv * 3.5 + float2(Time * 0.11, -Time * 0.08)).r;
    float halo = saturate((1.0 - radius) * 5.0) * (0.55 + noise * 0.45);
    float core = saturate((0.42 - radius) * 8.0);
    float3 color = lerp(MidColor, CoreColor, core);
    return float4(sampleColor.rgb * color * (halo * 0.62 + core * 1.35),
        sampleColor.a * saturate(halo + core) * Opacity);
}

technique ArtoriasAbyssTendril
{
    pass TendrilPass { PixelShader = compile ps_2_0 TendrilPixel(); }
}
technique ArtoriasTendrilTip
{
    pass TendrilTipPass { PixelShader = compile ps_2_0 TendrilTipPixel(); }
}
