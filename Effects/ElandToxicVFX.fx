// ElandToxicVFX.fx
// Eland's poison kit: lingering gas fog, venom projectile, corrosive impact splat.
//
// All three techniques are driven procedurally + by two noise inputs; none of them derive their
// SHAPE from the primary sprite. (The previous VenomGlob sampled T_Windstreak3's red channel for
// its silhouette, but that texture's RGB is solid white across the whole canvas - its real shape
// lives only in alpha - so the sample was a constant 1.0 and the glob rendered as a flat white
// blob. See Documentation/VFX_ARSENAL.md "Know what's actually in your input texture".)
//
// Texture contract (set by EnemyVFX.DrawEland*):
//   PrimarySampler = Voronoi_10  - cellular; bubbling/corrosive cell structure
//   DetailSampler  = T_CloudNoise_Tiled - billowing tileable cloud; fog body + flow erosion
//
// `DrawSize` remains the source texture size for EnemyVFX's legacy UV contract. `PixelDrawSize` is
// the final on-screen quad size, supplied only for this family, so the 2px filter remains stable at
// gameplay scale without changing the sampling behaviour of older EnemyVFX techniques.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize, PixelDrawSize;

float2 PixelateToxicUV(float2 uv)
{
    float2 blockUV = 2.0 / max(PixelDrawSize, float2(1.0, 1.0));
    return (floor(uv / blockUV) + 0.5) * blockUV;
}

// Lingering poison gas. Deliberately has NO boundary ring: the old version drew a hard-edged
// square/circle outline (`boundary`) which read as a white box on screen. This is fog only - dense
// through the damage radius, then feathering out well past it so the cloud has no visible border.
//
// `Direction` is repurposed as the damage-radius ratio: the fraction of the quad's half-extent that
// is actually the damaging area. EnemyVFX.DrawElandToxicField oversizes the quad by FogVisualScale
// and passes 1/FogVisualScale here, so the feather lives in the extra margin. Keep those in sync.
float4 ToxicField(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateToxicUV(c);
    float r = length(uv - 0.5);

    // Marilith-style multi-layer noise: two samples of the same cloud drifting in opposing
    // directions, so the fog churns without any repeating scroll line.
    float n1 = tex2D(DetailSampler, uv * 1.9 + float2( Time * 0.031, -Time * 0.052)).r;
    float n2 = tex2D(DetailSampler, uv * 3.7 + float2(-Time * 0.043,  Time * 0.028)).r;
    float bubbles = tex2D(PrimarySampler, uv * 2.4 + float2(Time * 0.017, -Time * 0.075)).r;
    float billow = saturate(n1 * 0.72 + n2 * 0.54 - 0.16);

    // The smooth macro cloud decides the silhouette; the smaller, faster layer only fills it in.
    // That makes the edge a handful of large fog lobes instead of a perfect disc or confetti.
    float damageEdge = 0.5 * Direction;
    float feather = max(0.5 - damageEdge, 0.02);
    float cloudReach = damageEdge + feather * (0.38 + n1 * 0.70);
    float damageBody = saturate((damageEdge - r) * 8.0);
    float cloudBody = saturate((cloudReach - r) * 8.0);
    float radial = max(damageBody * 0.82, cloudBody * (0.46 + billow * 0.54));

    // GUARANTEED CIRCULAR CUTOFF. The billow offset above subtracts up to ~0.06 from the effective
    // radius, so near the quad boundary the gas was still partly opaque when the square quad clipped
    // it - that straight cut is what read as a hard rectangle edge on the big fog and as a "square
    // with a circle inside" on the trail puffs. This term is noise-INDEPENDENT and provably reaches
    // zero before r = 0.5 in every direction, so no straight edge can ever survive. Squaring it also
    // turns the interior from a flat plateau into a dome, which is what makes small puffs read as
    // rounded globs rather than discs.
    float quadFade = saturate((0.5 - r) * 9.0);
    radial *= quadFade * quadFade;

    // Sparse voronoi cells popping through = corrosive gas bubbling up through the cloud.
    float density = saturate(radial * (0.42 + billow * 0.95) + saturate(bubbles - 0.72) * radial * 0.55);
    float d3 = density * density * density;

    // Non-linear ramp across the authored palette: mid tone arrives early, the pale core only at
    // the very thickest part of the gas (and mostly once the cloud is live rather than telegraphed).
    float3 color = lerp(DarkColor, MidColor, density);
    color = lerp(color, CoreColor, d3 * (0.35 + Active * 0.5));

    // Hold curve, not a decay: snap in over ~0.1 of the life, sit at full strength, fade only in the
    // last ~28%. Poison is supposed to linger - the old saturate(1 - P*P) started fading immediately.
    float hold = min(Progress * 9.0, 1.0) - max(Progress - 0.72, 0.0) * 3.57;
    float alpha = density * Opacity * max(hold, 0.0) * lerp(0.5, 1.0, Active);
    // AlphaBlend is premultiplied: returning bare RGB here was painting a transparent green quad.
    return float4(color * alpha * v.rgb, alpha * v.a);
}

// PoisonSmog core aura. Unlike ToxicField, this effect intentionally keeps its silhouette centred
// on the spinning 16px sprite: the noise changes density inside a symmetric envelope instead of
// pushing the envelope itself to one side. That prevents the soft body from looking as though it
// is orbiting the visible poison core. It has its own longer tail so the harmless visual lingers
// and dissipates after the damaging cloud's peak rather than vanishing abruptly.
float4 PoisonBurstAura(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateToxicUV(c);
    float r = length(uv - 0.5);

    float n1 = tex2D(DetailSampler, uv * 2.2 + float2( Time * 0.045, -Time * 0.034)).r;
    float n2 = tex2D(DetailSampler, uv * 4.1 + float2(-Time * 0.071,  Time * 0.053)).r;
    float cells = tex2D(PrimarySampler, uv * 3.0 + float2(Time * 0.030, -Time * 0.060)).r;
    float churn = saturate(n1 * 0.70 + n2 * 0.46 - 0.14);

    // Symmetric radial envelope first; animated noise only fills and erodes it. The extra outer
    // term forces alpha to zero well before the square quad boundary in every direction.
    float body = saturate((0.38 - r) * 6.4);
    float outer = saturate((0.50 - r) * 8.0);
    outer *= outer;
    float density = body * (0.46 + churn * 0.54 + saturate(cells - 0.72) * 0.22) * outer;
    float core = saturate((0.19 - r) * 8.5) * (0.55 + cells * 0.45);

    float fadeIn = min(Progress * 12.0, 1.0);
    // Start fading at 55% of its 600-tick life and take the remaining 45% to dissolve.
    float fadeOut = saturate((1.0 - Progress) * 2.22 + r * 0.001);
    float life = fadeIn * fadeOut;

    float3 color = lerp(DarkColor, MidColor, density);
    color = lerp(color, CoreColor, saturate(core + density * density * 0.25));
    float alpha = saturate(density + core * 0.42) * Opacity * life;
    float energy = density * 0.72 + core * 1.05;
    return float4(color * energy * alpha * v.rgb, alpha * v.a);
}

// Flying venom projectile: a rounded head with a tapering, churning tail. One shape field feeds
// body / rim / core, so the bright membrane edge comes free from the same maths (a resolution-
// independent stand-in for Gwyn's texel-neighbour rim, which needs real sprite alpha and would be
// sub-pixel at this ~54x18 on-screen size).
float4 VenomGlob(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateToxicUV(c);
    float x = uv.x;
    float y = abs(uv.y - 0.5);

    float flow  = tex2D(DetailSampler, float2(x * 2.4 - Time * 1.15, uv.y * 2.0 + Time * 0.22)).r;
    float cells = tex2D(PrimarySampler, uv * float2(2.6, 3.4) + float2(-Time * 0.95, Time * 0.18)).r;

    // Short and FAT: a dominant near-round head of poison liquid with only a stubby trailing wisp,
    // instead of the previous long thin streak that read as a green line. The head is pulled back
    // toward the middle of the quad and its radius nearly doubled, and the wobble on that radius is
    // stronger so the surface bulges like a falling droplet rather than a smooth capsule.
    float2 hp = float2((x - 0.58) * 1.06, uv.y - 0.5);
    float headField = saturate((0.34 + flow * 0.10 - length(hp)) * 6.0);

    float smear = saturate((0.58 - x) * 3.0) * saturate(x * 5.0);
    float tailField = saturate(((0.17 + flow * 0.16) * smear - y) * 7.0);

    float shape = max(headField, tailField);
    float body = shape * saturate(0.40 + flow * 0.55 + cells * 0.30);
    float rim = saturate(shape * 3.2) * (1.0 - saturate((shape - 0.28) * 3.4));
    // Off-centre highlight so the glob reads as a wet, refractive liquid surface rather than a gas.
    float core = saturate((headField - 0.48) * 2.6) * (0.55 + cells * 0.6);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate(core + rim * 0.45));
    float energy = body * 0.75 + core * 1.35 + rim * 0.5;
    float alpha = saturate(body + core + rim * 0.7) * Opacity;
    return float4(color * energy * alpha * v.rgb, alpha * v.a);
}

// Corrosive splat. The expanding edge radius is perturbed by voronoi cells so it throws irregular
// lobes instead of the old perfectly circular thin ring, and a haze skirt reaches past the splat so
// it dissipates rather than snapping off.
float4 VenomImpact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateToxicUV(c);
    float r = length(uv - 0.5);

    float cells = tex2D(PrimarySampler, uv * 2.2 + float2( Time * 0.02, -Time * 0.05)).r;
    float haze  = tex2D(DetailSampler,  uv * 2.8 - float2( Time * 0.06,  Time * 0.04)).r;

    float grow = Progress * (2.0 - Progress);
    float splatR = 0.15 + grow * 0.28 + (cells - 0.5) * 0.13;

    // FEATHERED, not stepped. `blob` used a hard (splatR - r) * 6.0 shoulder and the rim was a
    // narrow 0.05-wide band, so there was a crisp seam where the squiggly outer ring met the flat
    // green interior. The body shoulder is now much softer and the rim band is ~3x wider and
    // weighted by its own distance falloff, so the two grade into each other continuously.
    float blob = saturate((splatR - r) * 2.6);
    float rimD = abs(r - splatR);
    float rim  = saturate((0.15 - rimD) * 5.0) * (0.45 + cells * 0.8);
    float mist = saturate((splatR + 0.22 - r) * 2.0) * saturate(haze * 1.5 - 0.45);

    // Outer feather: guarantees the splat dissolves into nothing instead of ending at a boundary.
    float outer = saturate((0.5 - r) * 3.6);
    outer *= outer;

    float dens = saturate(blob * (0.45 + haze * 0.6) + mist * 0.55) * (1.0 - Progress * 0.30);
    dens *= outer;
    rim *= outer;
    float3 color = lerp(DarkColor, MidColor, dens);
    color = lerp(color, CoreColor, saturate(rim * 0.55 + saturate(dens - 0.78)));
    float alpha = saturate(dens + rim * 0.7) * Opacity;
    float energy = dens * 0.8 + rim * 1.15;
    return float4(color * energy * alpha * v.rgb, alpha * v.a);
}

technique ElandToxicField { pass P { PixelShader = compile ps_2_0 ToxicField(); } }
technique ElandPoisonBurstAura { pass P { PixelShader = compile ps_2_0 PoisonBurstAura(); } }
technique ElandVenomGlob { pass P { PixelShader = compile ps_2_0 VenomGlob(); } }
technique ElandVenomImpact { pass P { PixelShader = compile ps_2_0 VenomImpact(); } }
