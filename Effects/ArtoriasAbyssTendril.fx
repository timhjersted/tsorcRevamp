// ArtoriasAbyssTendril.fx
// Dark corpse-smoke sheath, independently moving violet/white filaments, and
// a directional three-pronged grab tip. Designed for Reach / ps_2_0.

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

float4 TendrilShadowPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float noise = tex2D(DetailSampler,
        uv * float2(3.2, 4.8) + float2(-Time * 0.09, Time * 0.045)).r;
    float fog = tex2D(PrimarySampler,
        uv * float2(1.7, 2.3) + float2(Time * 0.025, -Time * 0.038)).r;
    float wave = sin(uv.x * 15.0 + Time * 4.8) * 0.095 * taper;
    float distanceFromBody = abs(uv.y - 0.5 - wave);
    float width = 0.105 + noise * 0.075 + Active * 0.025;
    float body = saturate((width - distanceFromBody) * 9.5) * taper;
    float torn = body * saturate(fog * 0.72 + noise * 0.48 - 0.22);
    float3 color = lerp(DarkColor, MidColor, torn * 0.38);
    float alpha = vertexColor.a * saturate(body * 0.62 + torn * 0.36) * Opacity;
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 TendrilCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 6.0);
    float noise = tex2D(DetailSampler,
        uv * float2(4.1, 5.3) + float2(Time * 0.12, -Time * 0.08)).r;
    float waveA = sin(uv.x * 20.0 + Time * 7.2) * 0.085 * taper;
    float waveB = (-waveA * 0.72 + (noise - 0.5) * 0.055) * taper;
    float strandA = saturate((0.040 - abs(uv.y - 0.5 - waveA)) * 25.0);
    float strandB = saturate((0.031 - abs(uv.y - 0.5 - waveB)) * 32.0);
    float strands = saturate(strandA + strandB * 0.78) * taper;
    float pulsePos = frac(Time * (0.58 + Active * 0.24));
    float pulse = saturate((0.11 - abs(uv.x - pulsePos)) * 9.1) * strands;
    float breakup = strands * saturate(0.48 + noise * 0.72);
    float3 color = lerp(DarkColor, MidColor, breakup);
    color = lerp(color, CoreColor, pulse * 0.88);
    return float4(vertexColor.rgb * color * (breakup * 0.82 + pulse * 1.38),
        vertexColor.a * saturate(breakup + pulse) * Opacity);
}

float4 TendrilTipPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float noise = tex2D(DetailSampler,
        uv * 4.2 + float2(-Time * 0.10, Time * 0.075)).r;
    float lengthMask = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float center = saturate((0.055 - abs(p.y)) * 18.2);
    float upper = saturate((0.045 - abs(p.y - 0.17)) * 22.2);
    float lower = saturate((0.045 - abs(p.y + 0.17)) * 22.2);
    float prongs = saturate(center + upper + lower) * lengthMask;
    float knot = saturate((0.15 - length(p * float2(1.35, 1.0))) * 6.7);
    float body = saturate(prongs * (0.62 + noise * 0.62) + knot);
    float core = saturate(center * lengthMask + knot * 0.55) * Active;
    float3 stateColor = lerp(MidColor, float3(0.86, 0.18, 0.68), Active * 0.58);
    float3 color = lerp(DarkColor, stateColor, body);
    color = lerp(color, CoreColor, core * 0.72);
    return float4(vertexColor.rgb * color * (body * 0.86 + core * 1.15),
        vertexColor.a * body * Opacity);
}

technique ArtoriasTendrilShadow
{
    pass ShadowPass { PixelShader = compile ps_2_0 TendrilShadowPixel(); }
}

technique ArtoriasTendrilCore
{
    pass CorePass { PixelShader = compile ps_2_0 TendrilCorePixel(); }
}

technique ArtoriasTendrilTip
{
    pass TipPass { PixelShader = compile ps_2_0 TendrilTipPixel(); }
}
