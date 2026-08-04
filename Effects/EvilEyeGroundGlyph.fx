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
    float n = tex2D(DetailSampler, uv * 3.6 + float2(Time * 0.08, -Time * 0.06)).r;

    // Rune seal: square boundary roughened by noise instead of a clean SDF edge, so it reads
    // as a hand-scrawled sigil rather than a geometric UI shape.
    float square = max(ap.x, ap.y) + (n - 0.5) * 0.05;
    float seal = saturate((0.04 - abs(square - 0.46)) * 26.0);
    float corners = seal * saturate((max(ap.x, ap.y) + min(ap.x, ap.y) - 0.56) * 9.0);

    // Iris closing in toward the detonation point.
    float irisRadius = lerp(0.34, 0.09, Progress);
    float iris = saturate((0.035 - abs(r - irisRadius)) * 30.0) * (0.7 + n * 0.5);

    // Cat-eye slit pupil (an ellipse, not a plain dot) — reads distinctly as "eye," not just "circle."
    float slit = saturate((irisRadius * 0.55 - length(float2(p.x * 3.2, p.y))) * 9.0) * Progress;

    float activeFill = Active * saturate((0.45 - square) * 6.0) * (0.2 + n * 0.3);

    float body = saturate(corners + iris * 0.85 + activeFill);
    float core = saturate(seal * Active * 0.6 + slit + iris * Active * 0.6);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.72 + core * 1.55), saturate(body + core) * Opacity) * v;
}

technique EvilEyeGroundGlyph { pass P { PixelShader = compile ps_2_0 Glyph(); } }
