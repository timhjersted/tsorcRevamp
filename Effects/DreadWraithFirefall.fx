// Dread Wraith's phase-two ground fire. The warning and strike share one side-view flame
// silhouette: the caller draws the warning at half height and lower opacity, then the full pillar
// inside the existing 36x160 damage lane. AlphaBlend is premultiplied so the orange body stays
// saturated over bright terrain instead of washing into a white rectangle.
#include "PixelShaderCommon.fxh"

sampler ShapeNoise : register(s0);
sampler DetailNoise : register(s1);

float3 DarkColor;
float3 FlameColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float4 PixelGrid;

float4 DreadWraithFirefallPixel(float2 coords : TEXCOORD0) : COLOR0
{
    // Quantize BEFORE noise and silhouette maths so the flame itself, not merely its texture,
    // resolves on the requested 2x2 gameplay-pixel grid.
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float x = uv.x - 0.5;
    float across = abs(x);
    float y = uv.y; // 0 at the flame tip, 1 at the ground

    // High frequency across and low frequency upward stretches both fields into rising tongues.
    // SmoothNoise owns the silhouette; the finer marble field only breaks up heat inside it.
    float shapeNoise = tex2D(ShapeNoise,
        float2(uv.x * 2.35 + Time * 0.045, y * 0.66 - Time * 0.48)).r;
    float detailNoise = tex2D(DetailNoise,
        float2(uv.x * 4.70 - Time * 0.075, y * 1.08 - Time * 0.82)).r;
    float shape = saturate(shapeNoise * 1.20 - 0.14);
    float heatNoise = saturate(shapeNoise * 0.70 + detailNoise * 0.48 - 0.10);

    // The taper preserves the old pillar's narrow tip and broad base. At the base, reach maxes at
    // (0.07 + 0.20) * (0.82 + 0.16) = 0.2646: 33.9px across a 64px quad, safely inside the
    // 36px damage lane. It therefore reaches zero well before either side of the draw quad.
    float halfWidth = 0.07 + y * 0.20;
    float reach = halfWidth * (0.82 + shape * 0.16);
    float body = saturate((reach - across) * 12.0) * saturate(y * 8.0);

    // The warning grows from the floor through the same flame silhouette. The strike is fully
    // revealed immediately; Progress remains the real warning fraction rather than a brightness hack.
    float warningReveal = saturate((Progress * 1.18 - (1.0 - y)) * 4.2);
    float reveal = lerp(warningReveal, 1.0, Active);
    body *= reveal;

    float hot = body * saturate(heatNoise * 1.46 - 0.34);
    float core = body * saturate((reach * 0.42 - across) * 22.0)
        * saturate(detailNoise * 1.34 - 0.18);

    float3 rgb = DarkColor * body * 0.62
        + FlameColor * hot * 0.88
        + CoreColor * core * (0.18 + Active * 0.58);
    float alpha = saturate(body * 0.82 + hot * 0.28 + core * Active * 0.18) * Opacity;

    // Density-weighted color is already premultiplied; the small core surplus is intentional glow.
    return float4(rgb * Opacity, alpha);
}

technique DreadWraithFirefall
{
    pass DreadWraithFirefallPass
    {
        PixelShader = compile ps_2_0 DreadWraithFirefallPixel();
    }
}
