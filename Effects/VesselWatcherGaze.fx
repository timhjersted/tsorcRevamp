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
float4 PixelGrid; // xy = 2px block count across the final quad, zw = reciprocal (divided in C#)
float3 Nebula;    // x = spiral-warp gain, y = shell radius (0..1 of the half quad), z = heat (palette shift toward CoreColor)
float4 Scroll;    // xy = layer-1 UV offset, zw = layer-2 UV offset (both include the +0.5 recentre)

// The sentinel eye's charge / detonation telegraph: a contracting shell of swirling nebula.
//   - the shell is torn, not stroked: its radius is displaced by the macro cloud noise and its density
//     comes from that cloud, so dark dust lanes cut through it
//   - two noise layers move independently: each scrolls on its own heading, and they sit behind
//     opposite-signed spiral warps so they shear against each other. The warp gain is animated in C#
//     (Nebula.x), so the arms wind and unwind instead of sitting frozen
//   - thin bright filaments (the inverted dark veins of the detail texture) ride the cloud
//   - pixelated to the same 2px blocks Nito's shockwave uses
// Premultiplied over AlphaBlend, NOT additive: additive cannot make a saturated purple over a daytime
// sky (it clips to white, which is what the old ring looked like). The body occludes and the filaments
// carry the light; the palette slides from MidColor to CoreColor as `heat` rises, so the colour still
// changes as the charge builds.
//
// Every per-frame quantity (gain, radius, heat, scroll offsets) is computed in C# and passed in: a raw
// ps_2_0 entry point has no preshader, so uniform-only maths would be re-evaluated for every pixel.
// This is the difference between 56 and 75+ arithmetic slots.
//
// Progress is unused here (its effect arrives through Nebula); Opacity carries the idle..charge..detonate
// intensity.
float4 IrisPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = (floor(c * PixelGrid.xy) + 0.5) * PixelGrid.zw;
    float2 p = c - 0.5;
    float r = length(p) * 2.0;           // 0 at the pupil, 1.0 at the quad edge

    // Radius-dependent perpendicular slide = cheap spiral arms with no sin/cos: stronger toward the
    // centre, opposite senses for the two layers.
    float arm = (1.15 - r) * Nebula.x;
    float2 tangent = float2(-p.y, p.x);
    float n1 = tex2D(PrimarySampler, (p + tangent * arm) * 1.10 + Scroll.xy).r;
    float n2 = tex2D(DetailSampler, (p - tangent * arm * 0.8) * 2.30 + Scroll.zw).r;

    float cloud = saturate(n1 * 1.20 - 0.30);              // broad marbled body
    float thread = saturate((1.0 - n2) * 4.2 - 0.30);      // dark veins of the detail texture -> bright filaments

    // Shell radius, torn by the cloud noise. `ring` is the dense shell, `wide` the haze the wisps trail into.
    float d = abs(r - Nebula.y + (n1 - 0.5) * 0.32);
    float ring = saturate(1.0 - d * 3.0);
    float wide = saturate(1.0 - d * 1.5);

    float body = ring * (0.35 + cloud * 1.10);
    float wisp = wide * wide * cloud * thread * 1.3;

    // Noise-independent cutoff: zero at the quad edge whatever the noise does.
    float edge = saturate((1.0 - r) * 4.5);
    float fade = edge * edge * Opacity;

    float alpha = saturate(body * 1.30 + wisp * 0.7) * fade;
    float3 rgb = lerp(MidColor, CoreColor, Nebula.z + wisp) * (body * 0.8 + wisp) * fade;
    return float4(rgb, alpha);
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
