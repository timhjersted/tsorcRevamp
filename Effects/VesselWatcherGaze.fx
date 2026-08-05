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

// The sentinel eye's iris ring. THE CONCEPT IS DELIBERATELY UNCHANGED — the owner likes it: a ring
// that contracts as the eye charges and turns pink at detonation (the pink comes from CoreColor on
// the C# side). What changed is quality:
//   - the band's thickness is modulated by ring-space noise, so it breathes instead of being a
//     constant-width stroke
//   - a caustic pattern rotates inside the band
//   - a soft outer halo spills past the ring rather than the ring stopping dead
//   - a noise-independent cutoff makes clipping at the quad edge impossible
// Progress = charge (contracts the radius), Active = charge/detonation intensity.
float4 IrisPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;                 // 0 at the pupil, 1.0 at the quad edge
    float2 dir = p / max(len, 0.0005);
    float2 perp = float2(-dir.y, dir.x);

    // Caustic rotating inside the band: sliding along the ring's tangent is a rotation in texture
    // space and costs a fraction of a real sin/cos rotation.
    float spin = Time * (0.10 + Active * 0.22);
    float n1 = tex2D(PrimarySampler, dir * 0.34 + perp * spin + 0.5).r;
    float n2 = tex2D(DetailSampler, dir * 0.21 - perp * spin * 1.7 + 0.5).r;
    float grain = saturate(n1 * 0.85 + n2 * 0.65 - 0.26);

    // Contracting band. Thickness is noise-driven, so the edge is organic on both sides.
    float radius = lerp(0.80, 0.46, Progress);
    float band = saturate(1.0 - abs(r - radius) / (0.10 + grain * 0.16));
    // Halo spilling outward past the band so the ring does not end abruptly.
    float halo = saturate(1.0 - abs(r - radius) * 1.6) * (0.12 + grain * 0.22);
    // Dark pupil the ring encircles.
    float pupil = saturate((radius * 0.55 - r) * 5.0);

    // Noise-independent cutoff.
    float edge = saturate((1.0 - r) * 2.4);
    edge *= edge;
    band *= edge;
    halo *= edge;

    float3 color = lerp(DarkColor, MidColor, saturate(halo * 2.2 + band * 0.6));
    color = lerp(color, CoreColor, saturate(band * band * 1.45 + Active * band * 0.6));
    color *= 1.0 - pupil;

    float alpha = saturate(band * 0.95 + halo * 0.85 + pupil * 0.5) * Opacity;
    return float4(color * (band * 1.35 + halo * 0.6), alpha);
}

// The gaze thread. The old version sampled T_trail12-style streak art down a 620px lane; this is
// procedural: a tight core with a soft halo, beaded along its length, reaching toward the target as
// the charge completes so the player can see the lock-on travelling.
float4 GazeLinePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float n1 = tex2D(PrimarySampler, float2(c.x * 6.0 - Time * 0.75, 0.35)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 11.0 - Time * 1.40, 0.65)).r;
    float bead = saturate(n1 * 0.80 + n2 * 0.60 - 0.22);

    float core = saturate((0.22 - across) * 5.5);
    float halo = saturate(1.0 - across);

    // Reaches outward from the eye as Progress climbs.
    float reach = saturate((Progress * 1.3 - along) * 4.5);
    float fade = saturate(along * 7.0) * saturate((1.0 - along) * 7.0);

    float thread = core * (0.5 + bead * 0.7) * reach * fade;
    float glow = halo * halo * (0.18 + bead * 0.26) * reach * fade;

    float3 color = lerp(DarkColor, MidColor, saturate(glow * 2.2));
    color = lerp(color, CoreColor, saturate(thread * 1.4));
    return float4(color * (glow * 0.5 + thread * 1.3), saturate(glow * 0.5 + thread) * Opacity);
}

technique VesselWatcherIris { pass IrisPass { PixelShader = compile ps_2_0 IrisPixel(); } }
technique VesselWatcherLine { pass LinePass { PixelShader = compile ps_2_0 GazeLinePixel(); } }
