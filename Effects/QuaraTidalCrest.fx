// QuaraTidalCrest.fx
// Animated liquid ocean wave body — sprite-alpha masked, with sub-1x noise
// to prevent the repeating-tile seam lines visible in the previous version

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float4 uSourceRect;

float4 TidalCrest(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float4 waveTex = tex2D(PrimarySampler, c);
    float mask = waveTex.a;
    if (mask < 0.01) return float4(0, 0, 0, 0);

    // Convert atlas UV to normalized frame UV (0..1)
    float2 frameUV = (c - uSourceRect.xy) / max(uSourceRect.zw, float2(0.001, 0.001));

    // Edge rim detection for bright outlines
    float2 texel = 1.0 / max(PrimaryTextureSize, float2(1.0, 1.0));
    float neighborAlpha = min(
        min(tex2D(PrimarySampler, c + float2(texel.x, 0.0)).a,
            tex2D(PrimarySampler, c - float2(texel.x, 0.0)).a),
        min(tex2D(PrimarySampler, c + float2(0.0, texel.y)).a,
            tex2D(PrimarySampler, c - float2(0.0, texel.y)).a));
    float edge = saturate((waveTex.a - neighborAlpha) * 4.0);

    // Sub-1x noise scale: stretches across the whole sprite, never tiles visibly
    // Layer 1: slow-moving water body
    float n1 = tex2D(DetailSampler, frameUV * float2(0.6, 0.45) + float2(-Time * 0.18 * Direction, Time * 0.03)).r;
    // Layer 2: surface detail at slightly larger (but still sub-1x) scale
    float n2 = tex2D(DetailSampler, frameUV * float2(0.85, 0.65) + float2(-Time * 0.28 * Direction, Time * 0.06)).r;

    float body = saturate(n1 * 1.1 + n2 * 0.6 - 0.25);

    // Vertical gradient: brighter foam at the wave crest (top), darker water at base
    float crestGlow = saturate(1.0 - frameUV.y * 1.8);
    float foam = saturate(n2 * 1.5 - 0.4) * crestGlow;

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, foam * 0.7 + edge * 0.8);

    float3 finalColor = lerp(waveTex.rgb, color, 0.75) + CoreColor * (foam * 0.5 + edge * 0.6);
    float finalAlpha = mask * Opacity;
    return float4(v.rgb * finalColor, finalAlpha) * v.a;
}

technique QuaraTidalCrest
{
    pass P
    {
        PixelShader = compile ps_2_0 TidalCrest();
    }
}
