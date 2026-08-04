sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Aperture(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float n = tex2D(DetailSampler, p * 2.6 + float2(Time * 0.05, -Time * 0.04)).r;
    float r = length(float2(p.x, p.y * 0.7));
    // Iris ring perturbed by noise so the narrowing eyelid reads as crackling energy
    // instead of a perfectly clean vector ring.
    float radius = lerp(0.44, 0.23, Progress) + (n - 0.5) * 0.018;
    float ring = saturate((0.045 - abs(r - radius)) * 24.0) * (0.72 + n * 0.5);
    float lids = saturate((0.06 - abs(abs(p.y) - (radius - abs(p.x) * 0.45))) * 20.0);
    float core = Active * saturate((0.2 - r) * 6.0) * (0.6 + n * 0.7);
    // Was a square SDF ring (max(abs(p.x), abs(p.y)) banded at 0.46) gated on Active - that drew a
    // literal solid white RECTANGLE around the eye for the whole dash. Replaced with a soft radial
    // bloom that stays inside the quad and fades smoothly, so there is no straight edge anywhere.
    float bloom = Active * saturate(1.0 - r * 1.6) * (0.25 + n * 0.45);
    float3 color = lerp(MidColor, CoreColor, ring * Progress + core);
    return float4(color * (ring + lids * 0.7 + core * 1.4 + bloom * 0.7), saturate(ring + lids + core + bloom * 0.55) * Opacity) * v;
}

float4 Lane(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Root-cause fix: the old version sampled PrimarySampler ("trail" = T_trail12, a small
    // centered 4-point star flare) as if it were a horizontal streak mask. Stretched across a
    // 700px lane, almost the entire length sampled the texture's black background - which is
    // exactly why the preview lane was never visible. Rebuilt fully procedurally instead.
    float2 uv = UV(c);
    float x = uv.x;
    float y = abs(uv.y - 0.5) * 2.0;
    float broad = tex2D(DetailSampler, float2(x * 3.0 - Time * 0.6, uv.y * 2.0)).r;
    float grit = tex2D(DetailSampler, float2(x * 7.0 - Time * 1.0, uv.y * 4.0 + Time * 0.08)).r;
    float taper = saturate(x * 7.0) * saturate((1.0 - x) * 2.0);
    // A little sine flicker along the lane so it reads as live charged energy rather than a
    // static decal.
    float flicker = 0.8 + 0.2 * sin(Time * 12.0 + x * 16.0);
    float halfWidth = (0.32 + broad * 0.22) * taper;
    float laneValue = saturate((halfWidth - y) * 6.0) * saturate(0.35 + broad * 0.5 + grit * 0.25) * flicker;
    float core = saturate((0.09 * taper - y) * 9.0) * taper * Active;
    float3 color = lerp(MidColor, CoreColor, Progress + core);
    return float4(color * (laneValue * 0.68 + core * 1.4), saturate(laneValue + core) * Opacity) * v;
}

float4 Wake(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Root-cause fix: the old version sampled PrimarySampler ("windstreak" = T_Windstreak3),
    // whose RGB is solid white across the entire canvas (its real shape lives only in the alpha
    // channel, never read here) - so it contributed zero shape, leaving only the flat taper curve.
    // Rebuilt fully procedurally instead, matching the flame trail's tapered-wake rework.
    float2 uv = UV(c);
    float x = uv.x;
    float y = abs(uv.y - 0.5) * 2.0;
    float broad = tex2D(DetailSampler, float2(x * 2.3 - Time * 1.1, uv.y * 1.7 + Time * 0.2)).r;
    float grit = tex2D(DetailSampler, float2(x * 5.4 - Time * 1.6, uv.y * 3.1 - Time * 0.12)).r;
    float taper = saturate(x * 4.0) * saturate((1.0 - x) * 2.2);
    float halfWidth = (0.36 + broad * 0.2) * taper;
    float body = saturate((halfWidth - y) * 5.2) * saturate(0.32 + broad * 0.5 + grit * 0.3);
    float core = saturate((0.1 * taper - y) * 8.0) * taper;
    float3 color = lerp(DarkColor, MidColor, saturate(body * 0.7 + broad * 0.2));
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.6 + core * 1.3), saturate(body + core) * Opacity) * v;
}

technique EvilEyeChargeAperture { pass P { PixelShader = compile ps_2_0 Aperture(); } }
technique EvilEyeChargeLane { pass P { PixelShader = compile ps_2_0 Lane(); } }
technique EvilEyeChargeWake { pass P { PixelShader = compile ps_2_0 Wake(); } }
