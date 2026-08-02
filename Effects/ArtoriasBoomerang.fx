// ArtoriasBoomerang.fx
// Dedicated Reach-safe magic treatment for the hostile crescent boomerang.
// The recognizable sprite remains the crisp core; the wider moving layers live
// in separate effects so each legacy Reach program stays comfortably small.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;
float Layer;
float2 FrameUVOrigin;
float2 FrameUVScale;

float4 BoomerangCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - FrameUVOrigin) / FrameUVScale;
    float4 shape = tex2D(PrimarySampler, coords);
    float noise = tex2D(DetailSampler,
        uv * float2(2.8, 3.7) + float2(Time * 0.23 * Direction, -Time * 0.17)).r;
    float energy = saturate(noise * 0.82 + Active * 0.18);
    float3 color = lerp(DarkColor, MidColor, 0.38 + energy * 0.62);
    color = lerp(color, CoreColor, energy * Layer * 0.72);
    float brightness = 0.70 + energy * 0.72 + Layer * 0.22;
    float alpha = shape.a * lerp(0.38, 0.96, Layer) * Opacity;
    return float4(vertexColor.rgb * color * brightness, vertexColor.a * alpha);
}

technique ArtoriasBoomerangCore
{
    pass CorePass { PixelShader = compile ps_2_0 BoomerangCorePixel(); }
}
