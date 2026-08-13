// Gigas's slow lobbed miniature sun. The dark furnace body gives the projectile real mass, while
// two independently scrolling fields push holy flame through its cracks and irregular corona.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;

float4 GigasSolarBoulderPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = (coords - 0.5) * 2.0;
    float r = length(p);
    // Ordinary UV space is intentional: ring-space turns a voluminous sun into radial spokes.
    float macro = tex2D(MacroNoise, p * 2.15 + float2(0.5 - Time * 0.055, 0.5 + Time * 0.071)).r;
    float detail = tex2D(DetailNoise, p * 5.65 + float2(0.5 + Time * 0.19, 0.5 - Time * 0.24)).r;
    float flame = saturate(macro * 0.72 + detail * 0.56 - 0.12);

    // reach is at most 0.56 and quadFade is zero at r = 1, so every edge fades before the quad.
    float quadFade = saturate((1.0 - r) * 4.2);
    quadFade *= quadFade;
    float reach = 0.43 + (macro - 0.5) * 0.26;
    float body = saturate((reach - r) * 7.2) * quadFade;
    float coronaReach = 0.61 + (macro - 0.5) * 0.34;
    float coronaBand = saturate((coronaReach - r) * 5.2) * saturate((r - reach * 0.64) * 8.4) * quadFade;
    float corona = coronaBand * flame;
    float molten = body * flame;
    float core = saturate((0.18 - r) * 8.2);
    float pulse = 0.78 + Progress * 0.22;

    float alpha = saturate(body * (0.72 + molten * 0.18) + corona * 0.34 + core * 0.32) * pulse * Opacity;
    float3 stone = lerp(OuterColor, MiddleColor, molten * 0.56);
    float3 color = stone * (body * 0.82 * pulse)
        + MiddleColor * (molten * 0.72 + corona * 0.32) * pulse
        + CoreColor * core * 0.38 * pulse;

    // AlphaBlend is premultiplied. Dark crust remains visible in daylight; the restrained core is
    // emission layered over it, rather than a flat additive white disc.
    return float4(sampleColor.rgb * color * Opacity, sampleColor.a * alpha);
}

technique GigasSolarBoulder
{
    pass GigasSolarBoulderPass
    {
        PixelShader = compile ps_2_0 GigasSolarBoulderPixel();
    }
}
