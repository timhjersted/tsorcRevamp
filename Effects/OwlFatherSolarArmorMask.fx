// Sprite-attached solar material. This remains a duplicate of the solid armor/axe draw, but it now
// uses alpha as containment rather than painting one uniform gold silhouette. The separate puppet
// wisp ribbons are deliberately unmasked so flame can continue beyond these sprite boundaries.
sampler ArmorSampler : register(s0);
sampler FlowNoise : register(s1);

float3 uColor;            // deep solar orange
float3 uSecondaryColor;   // bright solar yellow
float uOpacity;
float uTime;
float4 uSourceRect;       // x/y/width/height in armor-sheet pixels
float2 uImageSize0;

float4 OwlFatherSolarArmorMaskPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 armor = tex2D(ArmorSampler, coords);
    float mask = armor.a * sampleColor.a;

    float2 sheetPixel = coords * uImageSize0;
    float2 frameUV = (sheetPixel - uSourceRect.xy) / max(uSourceRect.zw, float2(1.0, 1.0));

    // Two vertically biased, counter-drifting samples. SmoothNoise has real long flame filaments;
    // the anisotropic frequencies make them climb rather than read as round scrolling clouds.
    float n1 = tex2D(FlowNoise,
        frac(frameUV * float2(3.8, 1.12) + float2(uTime * 0.10, -uTime * 0.58))).r;
    float n2 = tex2D(FlowNoise,
        frac(frameUV * float2(6.4, 1.78) + float2(-uTime * 0.13, -uTime * 0.37))).r;

    // The broad layer owns the material body; the finer layer only carves moving hot channels.
    // Keeping a substantial threshold is what restores visible orange/yellow contrast instead of
    // the previous nearly constant amber wash.
    float body = saturate(n1 * 1.18 - 0.22);
    float detail = saturate(n2 * 1.35 - 0.40);
    float heat = saturate(body * 0.72 + detail * 0.52 - 0.12);
    float hot = heat * heat;
    float core = hot * hot;

    float3 solarColor = uColor * (0.11 + body * 0.24)
        + uSecondaryColor * (hot * 0.52)
        + float3(1.0, 0.88, 0.42) * (core * 0.38);

    // Player rendering uses premultiplied AlphaBlend. Low-heat areas retain enough alpha to shade
    // the flat gold sprite toward burnt orange; hot channels exceed alpha and read as emitted light.
    float alpha = mask * uOpacity * (0.34 + body * 0.24 + hot * 0.16);
    float3 rgb = solarColor * mask * uOpacity;
    return float4(rgb, alpha);
}

technique OwlFatherSolarArmorMask
{
    pass OwlFatherSolarArmorMaskPass
    {
        PixelShader = compile ps_2_0 OwlFatherSolarArmorMaskPixel();
    }
}
