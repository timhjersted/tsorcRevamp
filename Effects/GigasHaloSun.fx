// Gigas's orbiting votive suns. A 20px bright core remains inside the projectile's real hostile
// body; the wider broken corona is a deliberately faint, decorative shell behind its dust.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Phase;
float Active;

float4 GigasHaloSunPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = (coords - 0.5) * 2.0;
    float r = length(p);
    float macro = tex2D(MacroNoise, p * 2.85 + float2(0.5 + Time * 0.08 + Phase, 0.5 - Time * 0.11)).r;
    float detail = tex2D(DetailNoise, p * 7.10 + float2(0.5 - Time * 0.21, 0.5 + Time * 0.17 + Phase)).r;
    float fire = saturate(macro * 0.70 + detail * 0.54 - 0.14);

    // The core's .18 radius is 10px in the 56px draw shell, matching the 20px damage body.
    // quadFade is zero at r=1, so the noise-expanded corona never cuts off on a square edge.
    float quadFade = saturate((1.0 - r) * 4.6);
    quadFade *= quadFade;
    float core = saturate((0.18 - r) * 11.0) * (0.64 + detail * 0.36);
    float bodyReach = 0.30 + (macro - 0.5) * 0.13;
    float body = saturate((bodyReach - r) * 9.0) * quadFade;
    float coronaReach = 0.52 + (macro - 0.5) * 0.24;
    float corona = saturate((coronaReach - r) * 6.0) * saturate((r - bodyReach * 0.42) * 8.0) * fire * quadFade;
    float launch = 0.58 + Active * 0.42;

    float alpha = saturate(body * (0.66 + fire * 0.18) + corona * 0.30 + core * 0.48) * launch * Opacity;
    float3 color = lerp(OuterColor, MiddleColor, saturate(body * 0.60 + fire * 0.40));
    color = lerp(color, CoreColor, core * 0.72);
    float3 emission = CoreColor * core * (0.16 + Active * 0.13) * launch * Opacity;
    return float4(sampleColor.rgb * (color * alpha + emission), sampleColor.a * alpha);
}

technique GigasHaloSun
{
    pass GigasHaloSunPass
    {
        PixelShader = compile ps_2_0 GigasHaloSunPixel();
    }
}
