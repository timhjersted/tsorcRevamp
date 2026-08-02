// ArtoriasFloorFire.fx
// Sprite-masked purple flame used by the traveling FlipSlashLand fire front.
// The authored pixel flame supplies the silhouette; turbulent noise supplies motion,
// erosion, and a pale inner heat layer without ever exposing the source rectangle.

sampler SpriteSampler : register(s0);
sampler NoiseSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float Layer;
float2 FrameUVOrigin;
float2 FrameUVScale;

float MaskValue(float4 value)
{
    return max(value.a, max(value.r, max(value.g, value.b)));
}

float4 FloorFirePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - FrameUVOrigin) / FrameUVScale;
    float mask = MaskValue(tex2D(SpriteSampler, coords));
    float noise = tex2D(NoiseSampler,
        uv * float2(3.4, 5.2) + float2(Time * 0.10 * Direction + Layer, -Time * 0.24)).r;
    float sideFade = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
    float endFade = saturate(uv.y * 8.0) * saturate((1.0 - uv.y) * 8.0);
    float torn = mask * sideFade * endFade * saturate(0.72 + noise * 0.58);
    float hot = saturate(mask + noise * 0.24 - 0.66) * sideFade * endFade;

    float3 color = lerp(DarkColor, MidColor, torn);
    color = lerp(color, CoreColor, hot * (0.48 + Progress * 0.28));
    float alpha = torn * Opacity;
    float intensity = torn * 0.72 + hot * 1.16;
    return float4(vertexColor.rgb * color * intensity * Opacity,
        vertexColor.a * alpha);
}

technique ArtoriasFloorFire
{
    pass FloorFirePass
    {
        PixelShader = compile ps_2_0 FloorFirePixel();
    }
}
