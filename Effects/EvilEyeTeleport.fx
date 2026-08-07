// EvilEyeTeleport.fx
// EvilEye's space-distorting blink. Deliberately its OWN technique rather than reusing
// DemonSpiritSoulBurst: that shared shader carries a square-SDF `square` term which drew a solid
// white rectangle around the portal, and editing it would also change DemonSpirit's own explosion.
//
// Texture contract (set by EnemyVFX.DrawBurst -> EvilEyeTeleportBurst):
//   PrimarySampler = T_PerlinNoise_Tiled - slow seamless underlayer
//   DetailSampler  = SwirlyNoise         - wispy swirling body (both previously unused in the mod)

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

float4 TeleportRift(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;                       // 0 at centre, 1 at the quad's inscribed circle
    float2 dir = p / max(len, 0.0005);

    // RING-SPACE SAMPLING. Walking around the portal traces a *circle* through texture space, so
    // the noise varies continuously with no wrap discontinuity and - critically - no rectangular
    // tiling grid, which is what produced the visible repeating blocks inside the old portal.
    // (True polar atan2 mapping also works but costs ~14 slots and reintroduces a seam at +-pi;
    // this is cheaper AND seamless.) Two layers drift in opposing directions per the multi-layer
    // noise rule so nothing ever reads as a single scrolling texture.
    float2 uv1 = dir * (0.42 + r * 0.20) + float2(0.5 + Time * 0.030, 0.5 - Time * 0.024);
    float2 uv2 = dir * (0.27 - r * 0.13) + float2(0.5 - Time * 0.021, 0.5 + Time * 0.017);
    float s1 = tex2D(DetailSampler, uv1).r;
    float s2 = tex2D(PrimarySampler, uv2).r;
    float swirl = saturate(s1 * 1.15 + s2 * 0.75 - 0.42);

    // Ease-out expansion, squared for a soft-shouldered band rather than a hard-edged hoop.
    float grow = Progress * (2.0 - Progress);
    float ringR = 0.12 + grow * 0.66;
    float ringW = 0.09 + Progress * 0.14;
    float ring = saturate((ringW - abs(r - ringR)) / ringW);
    ring = ring * ring;

    float interior = saturate((ringR - r) * 2.2) * (0.30 + swirl * 0.95);

    // Squared circular vignette: alpha is already zero before the quad boundary, so the effect can
    // never be clipped into a rectangle - it simply ends, in a circle, on its own terms.
    float vig = saturate((1.0 - r) * 2.3);
    vig = vig * vig;

    // Noise-biased dissolve so the portal breaks up unevenly as it dies instead of stepping off.
    float dissolve = saturate((1.0 - Progress) * 2.6 + (swirl - 0.5) * 0.5);

    float3 color = lerp(DarkColor, MidColor, saturate(interior + ring * 0.45));
    color = lerp(color, CoreColor, saturate(ring * (0.55 + swirl * 0.5)));
    float alpha = saturate(ring * 1.05 + interior * 0.7) * vig * dissolve * Opacity;
    return float4(color * (ring * 1.45 + interior * 0.65), alpha) * v;
}

technique EvilEyeTeleportRift { pass P { PixelShader = compile ps_2_0 TeleportRift(); } }
