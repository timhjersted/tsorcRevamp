// QuaraTideRush.fx
// Procedural elliptical water-puddle surge with organic noise-eroded edges
// Replaces the old NPC-sprite-alpha masking that exposed a hard rectangle

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float4 uSourceRect;

float4 TideRush(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Normalize to 0..1 frame UV
    float2 frameUV = (c - uSourceRect.xy) / max(uSourceRect.zw, float2(0.001, 0.001));

    // Procedural puddle mask: low, wide ellipse with noise-eroded edges
    // Creates a convincing water-puddle shape instead of a rectangle
    float2 puddle = frameUV - 0.5;
    puddle.x *= 0.45;   // wider than tall — a low racing puddle
    puddle.y *= 2.2;    // squash vertically — flat water on the ground
    float puddleR = length(puddle);

    // Low-frequency noise modulates the puddle boundary for organic feel
    float edgeNoise = tex2D(DetailSampler, frameUV * 0.6 + float2(-Time * 0.15 * Direction, Time * 0.03)).r;
    float puddleEdge = 0.42 + edgeNoise * 0.12;  // wavy boundary between 0.42..0.54
    float puddleMask = saturate((puddleEdge - puddleR) * 7.0);

    if (puddleMask < 0.01) return float4(0, 0, 0, 0);

    // Dual noise layers at sub-1x scale to avoid visible tiling
    // First layer: slow flowing water body
    float n1 = tex2D(DetailSampler, frameUV * 0.7 + float2(-Time * 0.35 * Direction, Time * 0.04)).r;
    // Second layer: faster surface ripples
    float n2 = tex2D(DetailSampler, frameUV * float2(0.9, 0.5) + float2(-Time * 0.55 * Direction, Time * 0.08)).r;

    float body = saturate(n1 * 1.2 + n2 * 0.5 - 0.35);
    float foam = saturate(n2 * 1.6 - 0.5) * saturate(1.0 - puddleR * 2.0);

    // Bright leading edge: the front of the rushing water
    float leadingEdge = saturate(1.0 - abs(puddle.x * 2.2 - Direction * 0.3) * 3.0) * puddleMask;

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, foam * 0.8 + leadingEdge * 0.4);

    float3 finalColor = color * (1.0 + foam * 0.5 + leadingEdge * 0.6);
    float finalAlpha = puddleMask * Opacity * saturate(body * 0.8 + foam + 0.3);
    return float4(v.rgb * finalColor, finalAlpha) * v.a;
}

technique QuaraTideRush
{
    pass P
    {
        PixelShader = compile ps_2_0 TideRush();
    }
}
