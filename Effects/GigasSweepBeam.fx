// Gigas's ground-hugging blade of judgment. The 48px bright body matches the hostile beam height;
// the wider 72px draw quad exists only to give the non-damaging halo a clean place to fade.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;

float4 GigasSweepBeamPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float along = coords.x;
    float across = abs(coords.y - 0.5);

    // Long-frequency flow establishes a coherent horizontal blade; the finer field moves against
    // it so the gold filaments travel through the beam instead of becoming a scrolling texture.
    float macro = tex2D(MacroNoise, float2(along * 1.15 - Time * 0.82 * Direction, coords.y * 2.55 + Time * 0.10)).r;
    float detail = tex2D(DetailNoise, float2(along * 5.30 + Time * 2.10 * Direction, coords.y * 6.40 - Time * 0.34)).r;
    float shimmer = saturate(macro * 0.66 + detail * 0.58 - 0.12);

    // 0.333 across is 48px in the 72px visual quad: the broad bright body tells the truth about
    // the hostile rectangle. The halo reaches only 0.46 and therefore vanishes before the quad edge.
    float body = saturate((0.333 - across) * 7.8) * (0.62 + shimmer * 0.38);
    // A macro-only offset is cheap enough for Reach, but still lets the white-hot judgment
    // filament snake through the gold body instead of remaining a perfectly straight stripe.
    float coreOffset = (macro - 0.5) * 0.050;
    float core = saturate((0.075 - abs(coords.y - 0.5 - coreOffset)) * 19.0);
    float halo = saturate((0.460 - across) * 5.0) * (0.34 + shimmer * 0.66);

    // The 752px visual quad has 16px of padding around the 720px hitbox. Taper into that padding,
    // curving the cap more at the top and bottom, so neither end is a hard rectangular cutoff.
    float endLimit = 0.4787 - across * across * 0.24;
    float endFade = saturate((endLimit - abs(along - 0.5)) * 72.0);
    body *= endFade;
    core *= endFade;
    halo *= endFade;

    float gather = Progress;
    float telegraphHeat = halo * 0.08 + core * 0.18;
    float strikeHeat = body * (0.56 + shimmer * 0.46) + core * 0.94 + halo * 0.15;
    float heat = lerp(telegraphHeat, strikeHeat, Active) * gather;

    float3 color = lerp(OuterColor, MiddleColor, saturate(body * 0.73 + shimmer * 0.27));
    color = lerp(color, CoreColor, core * Active);
    float alpha = saturate((body * (0.70 + Active * 0.22) + halo * 0.18 + core * 0.45) * gather) * Opacity;

    float3 emission = CoreColor * core * Active * Opacity * 0.32;
    return float4(sampleColor.rgb * (color * heat * Opacity + emission), sampleColor.a * alpha);
}

technique GigasSweepBeam
{
    pass GigasSweepBeamPass
    {
        PixelShader = compile ps_2_0 GigasSweepBeamPixel();
    }
}
