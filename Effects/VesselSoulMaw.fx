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

// THE INHALE. Per the design doc this is the fight's read-at-a-glance tell: "its mouth-hole is the
// threat... when the mouth opens, gravity turns on". So it is not a disc, it is a THROAT — soul
// matter spirals inward and drains into a dark aperture at the centre.
//
// Two things make the drain read: the sample angle twists further the closer to the middle, and the
// sampled radius scrolls inward over time. Together the whole field visibly falls toward the mouth
// rather than sitting still.
//
// Direction = the normalised inner deadzone (the real no-pull radius from the gameplay code).
// Progress = spin-up, 0 is a slow drift and 1 is a hard suck. Active = committed, adds hot streaks.
//
// The old version's `boundary = 1 - abs(r - 0.965) * 32` drew a hard ring at the quad edge. Deleted:
// the field now feathers out to nothing and is additionally forced to zero before the quad clips.
float4 SoulMawPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;                       // 0 at the mouth, 1.0 at the quad edge
    float2 dir = p / max(len, 0.0005);

    // Swirl by sliding the sample along the ring's TANGENT by an amount that grows toward the
    // centre, so streamlines curve into the mouth instead of pointing straight at it. An explicit
    // sin/cos rotation reads identically and cost 90/64 arithmetic slots; this is a few.
    float2 perp = float2(-dir.y, dir.x);
    float twist = (1.35 - r) * (0.30 + Progress * 0.45) + Time * 0.22;
    // Radius scrolls INWARD over time: the texture appears to fall down the throat.
    float intake = r - Time * (0.25 + Progress * 0.50);

    float spiral = tex2D(PrimarySampler, dir * (0.30 + r * 0.26) + perp * twist + 0.5).r;
    // Second layer carries only the inward scroll, not the swirl — the two layers still drift
    // against each other, and the extra tangent term was the last 3 slots over budget.
    float streaks = tex2D(DetailSampler, dir * 0.42 + (0.5 - intake * 0.22)).r;
    float matter = saturate(spiral * 0.85 + streaks * 0.65 - 0.28);

    // Streamlines, tightening as they approach the aperture and stopping at the real deadzone.
    // `pull` doubles as the noise-independent cutoff: it is squared and reaches exactly zero at
    // r = 1.0, which is the quad edge, so nothing can survive to be clipped there regardless of what
    // the noise does. Progress spin-up is folded in so it scales the threads too.
    float pull = saturate((1.0 - r) * 2.0);
    pull *= pull * (0.45 + Progress * 0.55);
    float stream = matter * pull * saturate((r - Direction) * 5.0);
    // Committed brightens the threads rather than adding a separate hot term — that term plus a
    // split intensity/alpha accumulation was what kept this at 77/64 arithmetic slots.
    float threads = saturate(matter * 2.1 - 1.05) * pull * (0.5 + Active * 0.9);

    // The mouth itself: a dark aperture that swallows the streams, sized by the real deadzone.
    float aperture = saturate((Direction - r) * 7.0);

    float3 color = lerp(DarkColor, MidColor, saturate(stream * 1.35));
    color = lerp(color, CoreColor, saturate(threads * 1.30));
    // Multiply toward black rather than lerp toward DarkColor: DarkColor is (6,2,10), so the result
    // is the same aperture and it costs less.
    color *= 1.0 - aperture;

    // sampleColor is dropped deliberately: VesselVFX.Draw always passes Color.White, so it is a
    // constant 1 and multiplying by it costs slots for nothing. Same in every Vessel technique.
    float intensity = stream * 0.85 + threads * 1.35;
    float alpha = saturate(stream * 0.95 + threads * 0.75 + aperture * 0.85) * Opacity;
    return float4(color * intensity, alpha);
}

technique VesselSoulMaw
{
    pass SoulMawPass { PixelShader = compile ps_2_0 SoulMawPixel(); }
}
