#include "PixelShaderCommon.fxh"
sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float Time, Opacity;
float4 PixelGrid;
float4 Sliver(float4 v:COLOR0, float2 coords:TEXCOORD0):COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float u = saturate((uv.x - 0.125) * 1.333333);
    float lens = 4 * u * (1-u);
    float across = abs(uv.y - 0.5) * 32;
    float width = lens * lens * 7;
    float body = saturate((width - across) * 1.5);
    float blue = saturate((width * 0.34 - across) * 2);
    float edge = saturate((1.7 - abs(across - width * 0.75)) * 1.2) * body;
    float streak = tex2D(PrimarySampler, float2(uv.x * 3 - Time * 0.9, uv.y * 0.6)).r;
    float cut = saturate((uv.x - 0.125) * 64) * saturate((0.875 - uv.x) * 64);
    float alpha = body * cut * Opacity;
    float3 color = float3(0.46,0.65,0.82) * (0.7 + streak * 0.3) + float3(0.45,0.4,0.3) * edge;
    color = lerp(color, float3(0.16,0.68,1), blue);
    // Longitudinal cut reaches zero at x=.125/.875; width<=7 leaves 9px vertical padding.
    return float4(color * alpha, alpha);
}
technique DarkmoonSliver { pass P0 { PixelShader = compile ps_2_0 Sliver(); } }
