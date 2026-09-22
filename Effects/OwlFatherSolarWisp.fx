// Organic phase-two solar flame. The primary texture is one of three irregular flame masks;
// separate flowing and high-contrast noise fields animate the body and erode its boundary. This
// pass uses premultiplied AlphaBlend so orange remains saturated instead of clipping to white.
sampler PrimarySampler : register(s0);
sampler FlowSampler : register(s1);
sampler ErosionSampler : register(s2);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Seed;

float4 OwlFatherSolarWispPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 flowUV = float2(
        coords.x * 2.85 + Seed * 0.17 + Time * 0.07,
        coords.y * 1.10 + Seed * 0.09 - Time * 0.54);
    float2 erosionUV = float2(
        coords.x * 4.40 - Seed * 0.13 - Time * 0.10,
        coords.y * 2.05 + Seed * 0.21 - Time * 0.33);
    float flow = tex2D(FlowSampler, flowUV).r;
    float erosionNoise = tex2D(ErosionSampler, erosionUV).r;

    // Distort the recognizable flame mask rather than exposing it as a static stamped sprite.
    float2 warpedUV = coords + float2(
        (flow - 0.5) * 0.052 + (erosionNoise - 0.5) * 0.018,
        (erosionNoise - 0.5) * 0.022);
    float mask = tex2D(PrimarySampler, warpedUV).a;
    float expandedMask = tex2D(PrimarySampler, (warpedUV - 0.5) * 0.91 + 0.5).a;

    // The macro flow owns the silhouette; the finer erosion field only cuts and textures it.
    // Multiplication by mask guarantees transparent pixels stay transparent at every quad edge.
    float macro = saturate(flow * 1.20 - 0.16);
    float detail = saturate(erosionNoise * 1.18 - 0.30);
    float density = saturate((mask * (0.66 + macro * 0.48) + detail * 0.16 - 0.46) * 2.65);
    density *= mask;

    // An expanded copy provides a feathered orange fringe outside the body, but still resolves to
    // zero inside the flame texture's generous transparent padding—there can be no clipped quad.
    float halo = saturate(expandedMask - mask * 0.72) * (0.42 + macro * 0.38);
    float material = saturate(macro * 0.68 + detail * 0.46 - 0.08);
    float heat = density * material;
    float hot = heat * heat;
    float filament = saturate(1.0 - abs(flow - erosionNoise) * 7.5) * density;

    float lifeBody = 1.0 - Progress * 0.22;
    float coverage = saturate(density * lifeBody + halo * 0.48);
    float alpha = coverage * Opacity * (0.30 + material * 0.34);
    float3 color = DarkColor * (halo * 0.22 + density * 0.24)
        + MidColor * (heat * 0.58)
        + CoreColor * (hot * 0.48 + filament * 0.20);

    // Premultiplied color: low-density edges fade instead of becoming solid orange cutouts, while
    // the confined hot channels are allowed to emit a modest amount of light above their alpha.
    return float4(color * Opacity, alpha);
}

technique OwlFatherSolarWisp
{
    pass OwlFatherSolarWispPass
    {
        PixelShader = compile ps_2_0 OwlFatherSolarWispPixel();
    }
}
