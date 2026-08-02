// ArtoriasPurpleFire.fx
// Layered mask-driven Abyss flame for traveling floor fire and homing wisps.

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
float2 WorldDrawSize;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float MaskValue(float4 sampleValue)
{
    return max(sampleValue.a, max(sampleValue.r, max(sampleValue.g, sampleValue.b)));
}

float4 PurpleFireBodyPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 animatedUV = uv;
    animatedUV.x += sin(uv.y * 11.0 + Time * 6.4 + Direction) * 0.036
        * (0.35 + abs(uv.y - 0.5));
    float mask = MaskValue(tex2D(PrimarySampler, animatedUV));
    float noise = tex2D(DetailSampler,
        uv * float2(3.1, 4.7) + float2(Time * 0.08 * Direction, -Time * 0.18)).r;
    float sideFade = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
    float endFade = saturate(uv.y * 5.0) * saturate((1.0 - uv.y) * 5.0);
    float edgeFade = sideFade * endFade;
    float torn = saturate(mask * 1.12 + noise * 0.46 - 0.38) * edgeFade;
    float ember = torn * saturate((noise - 0.62) * 2.7);
    float3 color = lerp(DarkColor, MidColor, torn * (0.58 + Progress * 0.20));
    color = lerp(color, CoreColor, ember * 0.32);
    float alpha = vertexColor.a * torn * Opacity;
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 PurpleFireCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 animatedUV = uv;
    animatedUV.x += sin(uv.y * 14.0 - Time * 8.2 + Direction) * 0.026
        * (0.30 + abs(uv.y - 0.5));
    float mask = MaskValue(tex2D(PrimarySampler, animatedUV));
    float noise = tex2D(DetailSampler,
        uv * float2(4.3, 5.6) + float2(-Time * 0.13, Time * 0.06 * Direction)).r;
    float sideFade = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
    sideFade *= sideFade;
    float endFade = saturate(uv.y * 5.0) * saturate((1.0 - uv.y) * 5.0);
    float edgeFade = sideFade * endFade;
    float core = saturate(mask * 1.24 + noise * 0.30 - 0.54) * edgeFade;
    float hot = saturate((core - 0.48) * 1.92) * (0.72 + Active * 0.28);
    float3 color = lerp(MidColor, CoreColor, hot);
    float intensity = core * 0.82 + hot * 1.18;
    return float4(vertexColor.rgb * color * intensity,
        vertexColor.a * core * Opacity);
}

technique ArtoriasPurpleFireBody
{
    pass BodyPass { PixelShader = compile ps_2_0 PurpleFireBodyPixel(); }
}

technique ArtoriasPurpleFireCore
{
    pass CorePass { PixelShader = compile ps_2_0 PurpleFireCorePixel(); }
}
