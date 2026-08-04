sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Tail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Root-cause fix: the old version drove its shape from PrimarySampler's red channel, but
    // T_Windstreak3's RGB is solid white across the whole canvas (its real shape lives only in
    // alpha, never read here) - so `streak` was a constant 1.0 everywhere and contributed nothing.
    // The entire "shape" was just the taper curve, which is why it read as a flat hard-edged blob
    // with no motion. Rebuilt fully procedurally off two DetailSampler octaves instead, matching
    // the tapered-energy-wake pattern used for Red Knight's spear wake.
    float x = c.x;
    float broad = tex2D(DetailSampler, float2(x * 2.6 - Time * 0.9, c.y * 1.8 + Time * 0.15)).r;

    // SQUIGGLE: jitter the centreline with a fast 1D noise scroll so the trail snakes instead of
    // being a dead-straight line. Damped toward the head (x=1) so it stays anchored to the sprite
    // and only whips around further back down the tail.
    float wob = tex2D(DetailSampler, float2(x * 1.7 - Time * 2.3, 0.5)).r;
    float squiggle = (wob - 0.5) * 0.26 * saturate((1.0 - x) * 1.8);
    float y = abs(c.y - 0.5 - squiggle) * 2.0;

    // BELL WIDTH: fattest just behind the head, tapering to a point at the tail, so the trail has
    // some mass near the projectile rather than a uniform hairline.
    float taper = saturate(x * 5.0) * saturate((1.0 - x) * 2.3);
    float bell = taper * (0.45 + x * 0.85);
    float halfWidth = (0.26 + broad * 0.20) * bell;
    float body = saturate((halfWidth - y) * 6.5) * saturate(0.35 + broad * 0.60);
    float core = saturate((0.10 * bell - y) * 10.0) * taper;
    float vein = Active * saturate((abs(sin((x + broad * 0.2) * 18.0)) - 0.8) * 6.0) * body;

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core + vein);
    float energy = body * (0.55 + broad * 0.35) + core * 0.9 + vein;
    return float4(color * energy, saturate(body + core + vein) * Opacity) * v;
}

float4 Core(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // The old version had zero texture sampling here — a flat two-circle gradient blob with no
    // motion at all. Given a turbulent noise sample to churn the orb's edge and hot center instead.
    float r = length(c - 0.5) * 2.0;
    float n = tex2D(DetailSampler, c * 3.2 + float2(Time * 0.55, -Time * 0.48)).r;
    // Drawn larger than the 16px sprite now, so an outer halo is added that spills past the sprite's
    // silhouette - previously the whole orb sat behind the sprite and was almost entirely hidden.
    float halo = saturate((1.0 - r) * 1.9) * (0.20 + n * 0.30);
    float orb = saturate((0.62 - r + (n - 0.5) * 0.14) * 6.0);
    float core = saturate((0.30 - r + (n - 0.5) * 0.10) * 9.0);
    float3 color = lerp(MidColor, CoreColor, core);
    return float4(color * (halo * 0.6 + orb * (0.55 + n * 0.3) + core * 1.5),
        saturate(halo * 0.55 + orb + core) * Opacity) * v;
}

float4 Impact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Noise-jagged ring edge instead of a clean SDF circle — reads as a shatter/burst rather than
    // a UI ripple, matching the eye's shatter-on-death identity established elsewhere in its kit.
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float n = tex2D(DetailSampler, uv * 4.0 - float2(Time * 0.1, Time * 0.15)).r;
    float r = length(float2(p.x, p.y * 1.7));
    float radius = lerp(0.12, 0.43, Progress) + (n - 0.5) * 0.02;
    float ring = saturate((0.055 - abs(r - radius)) * 22.0) * (0.7 + n * 0.6);
    float pupil = saturate((0.17 - length(p)) * 6.0) * (1.0 - Progress);
    float3 color = lerp(MidColor, CoreColor, pupil + ring * 0.5);
    return float4(color * (ring + pupil * 1.5), saturate(ring + pupil) * Opacity) * v;
}

technique EvilEyeFlameTail { pass P { PixelShader = compile ps_2_0 Tail(); } }
technique EvilEyeFlameCore { pass P { PixelShader = compile ps_2_0 Core(); } }
technique EvilEyeFlameImpact { pass P { PixelShader = compile ps_2_0 Impact(); } }
