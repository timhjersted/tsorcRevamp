// ArtoriasAbyssHand.fx
// Pulsing black-violet replacement for Artorias's sword arm during Tendril Grab.

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

float4 HandShadowPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float radius = length(p * float2(1.05, 0.88));
    float aura = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler,
        uv * 3.6 + float2(Time * 0.08, -Time * 0.06)).r;
    float pulse = 0.035 * sin(Time * 7.0) + Progress * 0.035;
    float mass = saturate((0.48 + pulse - radius + (noise - 0.5) * 0.09) * 7.2);
    mass *= saturate(aura * 0.70 + noise * 0.52);
    float3 color = lerp(DarkColor, MidColor, noise * 0.28 + Active * 0.12);
    float alpha = vertexColor.a * mass * Opacity * (0.72 + Progress * 0.18);
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 HandCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float radius = length(p);
    float aura = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler,
        uv * 4.7 + float2(-Time * 0.13, Time * 0.10)).r;
    float shellRadius = 0.31 + sin(Time * 6.2) * 0.025;
    float shell = saturate((0.075 - abs(radius - shellRadius)) * 13.3);
    float veins = saturate((noise - 0.64) * 2.8) * saturate((0.48 - radius) * 5.0);
    float core = saturate((0.18 + Active * 0.035 - radius) * 5.6);
    float energy = saturate((shell * 0.62 + veins) * aura);
    float3 color = lerp(DarkColor, MidColor, energy);
    color = lerp(color, CoreColor, core * (0.48 + Progress * 0.38));
    return float4(vertexColor.rgb * color * (energy * 0.76 + core * 1.32),
        vertexColor.a * saturate(energy + core) * Opacity);
}

technique ArtoriasAbyssHandShadow
{
    pass ShadowPass { PixelShader = compile ps_2_0 HandShadowPixel(); }
}

technique ArtoriasAbyssHandCore
{
    pass CorePass { PixelShader = compile ps_2_0 HandCorePixel(); }
}
