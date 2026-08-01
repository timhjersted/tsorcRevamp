sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Glyph(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float2 ap = abs(p);
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.4 + float2(Time * 0.08, -Time * 0.06)).r;
    float square = max(ap.x, ap.y);
    float boundary = saturate((0.035 - abs(square - 0.46)) * 34.0);
    float corners = boundary * saturate((max(ap.x, ap.y) + min(ap.x, ap.y) - 0.56) * 9.0);
    float irisRadius = lerp(0.34, 0.09, Progress);
    float iris = saturate((0.035 - abs(r - irisRadius)) * 34.0);
    float pupil = saturate((0.09 - r) * 10.0) * Progress;
    float activeFill = Active * saturate((0.45 - square) * 7.0) * (0.18 + n * 0.18);
    float body = saturate(corners + iris * (0.72 + n * 0.28) + activeFill);
    float core = saturate(boundary * Active + pupil + iris * Active);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.72 + core * 1.55), saturate(body + core) * Opacity) * v;
}

technique EvilEyeGroundGlyph { pass P { PixelShader = compile ps_2_0 Glyph(); } }
