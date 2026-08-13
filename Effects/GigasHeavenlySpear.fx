// Gigas's heavenly spear. The needle (right side of its local quad) is anchored at the projectile's
// real 18px damage point; the long broken wake trails behind and grows only as decorative spectacle.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Phase;

float4 GigasHeavenlySpearPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float along = coords.x;
    float across = abs(coords.y - 0.5);
    float macro = tex2D(MacroNoise, float2(along * 2.20 - Time * 0.20 + Phase, coords.y * 1.45 + Time * 0.09)).r;
    float detail = tex2D(DetailNoise, float2(along * 6.65 + Time * 0.31, coords.y * 3.10 - Time * 0.48 + Phase)).r;

    // Both ends taper to zero before the quad's boundary. The broad head ends at .93, then a
    // separate needle continues to the real impact point at x=1: it cannot read as a blunt beam.
    float tail = saturate(along * 27.0);
    float tipFade = saturate((1.0 - along) * 27.0);
    float tailBlend = saturate(along * 5.5);
    float head = saturate((along - 0.66) * 5.5) * saturate((0.93 - along) * 7.2);
    float needleBlend = saturate((along - 0.84) * 6.25);
    float shaftWidth = (0.040 + macro * 0.055) * tailBlend;
    float headWidth = shaftWidth + head * (0.145 + macro * 0.020);
    float needleWidth = (1.0 - along) * 0.50;
    float width = lerp(headWidth, needleWidth, needleBlend);
    float body = saturate((width - across) * 22.0) * tail * tipFade;
    float wakeNoise = saturate(macro * 0.66 + detail * 0.54 - 0.14);
    float wake = saturate((shaftWidth + 0.040 + macro * 0.050 - across) * 8.0) * tail * tipFade * (1.0 - head) * wakeNoise;

    // The harmless hover grows from the future tip backward; the committed dive is fully revealed.
    float forming = saturate((Progress - (1.0 - along) * 0.74) * 5.2);
    float reveal = lerp(forming, 1.0, Active);
    float heat = saturate(macro * 0.64 + detail * 0.52 - 0.13);

    // The narrow core/fissures deliberately live in the second pass, preserving this pass's
    // portable body silhouette and leaving it well below Reach's arithmetic limit.
    float alpha = saturate(body * (0.76 + heat * 0.16) + wake * 0.30) * reveal * Opacity;
    float3 color = lerp(OuterColor, MiddleColor, saturate(body * 0.48 + heat * 0.52));
    return float4(sampleColor.rgb * color * alpha, sampleColor.a * alpha);
}

// The preferred preview's fine material layer, isolated from the broad body so it gets its own
// ps_2_0 budget. It repeats the same silhouette only to guarantee its cracks cannot escape it.
float4 GigasHeavenlySpearDetailsPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float along = coords.x;
    float across = abs(coords.y - 0.5);
    float macro = tex2D(MacroNoise, float2(along * 2.20 - Time * 0.20 + Phase, coords.y * 1.45 + Time * 0.09)).r;
    float detail = tex2D(DetailNoise, float2(along * 6.65 + Time * 0.31, coords.y * 3.10 - Time * 0.48 + Phase)).r;
    float tail = saturate(along * 27.0);
    float tipFade = saturate((1.0 - along) * 27.0);
    float tailBlend = saturate(along * 5.5);
    float head = saturate((along - 0.66) * 5.5) * saturate((0.93 - along) * 7.2);
    float needleBlend = saturate((along - 0.84) * 6.25);
    float shaftWidth = (0.040 + macro * 0.055) * tailBlend;
    float headWidth = shaftWidth + head * (0.145 + macro * 0.020);
    float needleWidth = (1.0 - along) * 0.50;
    float width = lerp(headWidth, needleWidth, needleBlend);
    float body = saturate((width - across) * 22.0) * tail * tipFade;

    float forming = saturate((Progress - (1.0 - along) * 0.74) * 5.2);
    float reveal = lerp(forming, 1.0, Active);
    float core = saturate((0.105 - across) * 16.0) * saturate((along - 0.68) * 4.1) * tail * tipFade;
    float fissures = body * saturate(detail * 1.28 - macro * 0.26 - 0.10) * (1.0 - needleBlend * 0.55);

    float alpha = saturate(core * 0.36 + fissures * 0.25) * reveal * Opacity;
    float3 color = lerp(MiddleColor, CoreColor, core * 0.72 + fissures * 0.25);
    float3 emission = CoreColor * core * (0.08 + Active * 0.12) * reveal * Opacity;
    return float4(sampleColor.rgb * (color * alpha + emission), sampleColor.a * alpha);
}

technique GigasHeavenlySpear
{
    pass GigasHeavenlySpearPass
    {
        PixelShader = compile ps_2_0 GigasHeavenlySpearPixel();
    }
}

technique GigasHeavenlySpearDetails
{
    pass GigasHeavenlySpearDetailsPass
    {
        PixelShader = compile ps_2_0 GigasHeavenlySpearDetailsPixel();
    }
}
