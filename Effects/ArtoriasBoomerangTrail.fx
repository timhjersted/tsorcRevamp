// ArtoriasBoomerangTrail.fx
// Kept separate from ArtoriasBoomerang.fx so the legacy Reach compiler can
// remain below its combined effect-expression limits.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;

float4 BoomerangRibbonPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // T_Windstreak3's long axis is local Y. Draw code rotates that axis onto velocity.
    float streak = tex2D(PrimarySampler, coords).r;
    float noise = tex2D(DetailSampler,
        coords * float2(3.4, 2.1) + float2(Time * 0.15 * Direction, -Time * 0.31)).r;
    float center = saturate(1.0 - abs(coords.x - 0.5) * 2.25);
    float tailFade = saturate(coords.y * 2.8) * saturate((1.0 - coords.y) * 1.25);
    float body = saturate(streak * (0.60 + noise * 0.62) - 0.16) * center * tailFade;
    float thread = saturate((body - 0.52) * 2.1);

    float3 returnColor = lerp(MidColor, float3(0.86, 0.14, 0.72), Active * 0.70);
    float3 color = lerp(DarkColor, returnColor, body);
    color = lerp(color, CoreColor, thread * 0.45);
    return float4(vertexColor.rgb * color * (body * 0.78 + thread * 0.64),
        vertexColor.a * body * Opacity);
}

technique ArtoriasBoomerangRibbon
{
    pass RibbonPass { PixelShader = compile ps_2_0 BoomerangRibbonPixel(); }
}
