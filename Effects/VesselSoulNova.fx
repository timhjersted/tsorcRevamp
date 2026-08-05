sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;

// Expanding soul shockwave. Active = the ring's normalised radius, Direction = its normalised half
// thickness — both derived from the projectile's REAL collision radius, so the bright front is an
// honest reading of where the hit is.
//
// The front's falloff DISTANCE is noise-driven rather than the shape being eroded after the fact,
// so the edge tears organically and never reads as a drawn circle. Behind it, a veil frays inward
// and dies out on its own.
//
// sampleColor is dropped throughout: VesselVFX.Draw always passes Color.White.
float4 SoulNovaPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    // Ring-space: continuous all the way around, no tiling grid and no atan2 seam.
    float n1 = tex2D(PrimarySampler, dir * (0.40 + r * 0.45) + (0.5 + Time * 0.05)).r;
    float n2 = tex2D(DetailSampler, dir * 0.26 + (0.5 - Time * 0.04)).r;
    float grain = saturate(n1 * 0.85 + n2 * 0.60 - 0.24);

    float half = max(Direction, 0.004);
    float t = (r - Active * 0.5) / half;   // 0 at the front, +outside, -inside

    // Leading front, thickness modulated by the noise.
    float front = saturate(1.0 - abs(t) / (0.9 + grain * 1.5));
    // Trailing veil: a bump that peaks a few thicknesses behind the front and is exactly zero both
    // at the front and further in, so it needs no separate clamp.
    float veil = saturate(-t * 0.1667) * saturate(1.0 + t * 0.1667) * (0.35 + grain * 0.8);

    // Noise-independent cutoff — the caller's padding only just clears the front's reach, so this
    // is what actually guarantees nothing survives to be clipped by the quad.
    float edge = saturate((0.5 - r) * 11.0);
    edge *= edge;
    front *= edge;
    veil *= edge;

    float3 color = lerp(DarkColor, MidColor, saturate(veil * 1.6 + front * 0.5));
    color = lerp(color, CoreColor, saturate(front * front * 1.3));

    float alpha = saturate(front * 0.95 + veil * 0.7) * Opacity;
    return float4(color * (front * 1.45 + veil * 0.55), alpha);
}

technique VesselSoulNova
{
    pass SoulNovaPass { PixelShader = compile ps_2_0 SoulNovaPixel(); }
}
