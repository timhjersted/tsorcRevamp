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

float4 GroundRiftPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float crack = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * 3.2 + float2(Time * 0.07, -Time * 0.03)).r;
    float radius = length(float2(centered.x, centered.y * 2.3));
    float footprint = saturate((0.51 - radius) * 10.0);
    float reveal = saturate((Progress + 0.12 - radius * 1.65) * 7.5);
    float fissure = saturate((crack - 0.22 + noise * 0.15) * 3.0) * footprint * reveal;
    float core = saturate((crack - 0.67) * 4.4) * footprint * reveal;
    float3 color = lerp(DarkColor, MidColor, fissure);
    color = lerp(color, CoreColor, core);
    return float4(color * (fissure * 0.8 + core * 1.5), saturate(fissure + core) * Opacity) * vertexColor;
}

float4 GravePlumePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float smoke = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.4, 3.8) + float2(Time * 0.06, -Time * 0.24)).r;
    float rise = saturate((Progress - (1.0 - uv.y) * 0.7) * 5.5);
    float center = saturate((0.43 - abs(uv.x - 0.5)) * 3.2);
    float plume = saturate(smoke * 0.74 + noise * 0.45 - 0.42) * center * rise;
    float channel = saturate((0.1 - abs(uv.x - 0.5)) * 10.0) * rise;
    float3 color = lerp(DarkColor, MidColor, plume);
    color = lerp(color, CoreColor, channel * 0.72);
    return float4(color * (plume * 0.66 + channel * 1.15), saturate(plume + channel) * Opacity) * vertexColor;
}

float4 GraveHandPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float4 hand = tex2D(PrimarySampler, uv);
    float noise = tex2D(DetailSampler, uv * 3.0 + float2(Time * 0.08, -Time * 0.19)).r;
    float rise = saturate((Progress - (1.0 - uv.y) * 0.52) * 6.0);
    float silhouette = hand.a * rise;
    float bone = hand.r * silhouette;
    float aura = saturate(hand.a * 0.72 + noise * hand.a * 0.3);
    float3 color = lerp(DarkColor, MidColor, aura);
    color = lerp(color, CoreColor, bone * 0.78);
    return float4(color * (aura * 0.55 + bone * 1.1), silhouette * Opacity) * vertexColor;
}

technique NitoGroundRift
{
    pass Pass1 { PixelShader = compile ps_2_0 GroundRiftPixel(); }
}

technique NitoGravePlume
{
    pass Pass1 { PixelShader = compile ps_2_0 GravePlumePixel(); }
}

technique NitoGraveHand
{
    pass Pass1 { PixelShader = compile ps_2_0 GraveHandPixel(); }
}
