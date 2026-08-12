// Gigas's column of judgment. The bright core remains inside the 44px damage lane; the broader
// gold body is decorative light behind the existing GoldFlame / GoldCoin dust.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;

float4 GigasSunPillarPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float across = abs(uv.x - 0.5);

    // Low-frequency noise owns the column silhouette; high-frequency noise only textures it.
    // The broad gold body descends slowly while finer sacred filaments climb through it. Opposing
    // flow keeps the column alive instead of reading as one texture sliding behind the dust.
    float macro = tex2D(MacroNoise, float2(uv.x * 2.7 - Time * 0.06, uv.y * 1.15 - Time * 0.46)).r;
    float detail = tex2D(DetailNoise, float2(uv.x * 7.1 + Time * 0.19, uv.y * 2.25 + Time * 0.92)).r;
    float shape = saturate(macro * 1.22 - 0.16);
    float shimmer = saturate(macro * 0.68 + detail * 0.52 - 0.12);

    // The body reaches at most 0.275 from centre (44px in the 80px draw), matching the damaging
    // PillarWidth. It still leaves 0.225 UV margin on either side, so it fades before the quad edge.
    float reach = 0.240 + shape * 0.035;
    float body = saturate((reach - across) * 8.5);
    float core = saturate((0.052 - across) * 24.0) * (0.70 + detail * 0.30);
    float halo = saturate((reach + 0.055 - across) * 4.6) * (0.45 + shimmer * 0.55);

    // Telegraph rises from the ground (uv.y = 1); the strike owns the entire column. The high end
    // dissolves rather than ending in a hard line, while the ground end remains visibly rooted.
    float telegraphReveal = saturate((Progress - (1.0 - uv.y)) * 9.0);
    float activeReveal = lerp(telegraphReveal, 1.0, Active);
    float axialFade = saturate(uv.y * 18.0) * saturate((1.0 - uv.y) * 7.0);
    float telegraphHeat = body * (0.13 + shimmer * 0.13) + halo * 0.07;
    float strikeHeat = body * (0.50 + shimmer * 0.42) + core * 0.92 + halo * 0.17;
    float heat = lerp(telegraphHeat, strikeHeat, Active) * activeReveal * axialFade;

    float3 color = lerp(OuterColor, MiddleColor, saturate(body * 0.72 + shimmer * 0.28));
    color = lerp(color, CoreColor, core * Active);
    float alpha = saturate((body * 0.72 + halo * 0.23 + core * 0.48) * activeReveal * axialFade) * Opacity;

    // AlphaBlend is premultiplied: the body retains its gold over a bright sky, with a restrained
    // emissive core added back after occlusion.
    float3 emission = CoreColor * core * Active * Opacity * 0.34;
    return float4(sampleColor.rgb * (color * alpha + emission), sampleColor.a * alpha);
}

technique GigasSunPillar
{
    pass GigasSunPillarPass
    {
        PixelShader = compile ps_2_0 GigasSunPillarPixel();
    }
}
