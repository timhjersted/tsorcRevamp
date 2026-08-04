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
// NOTE: `coords` is used directly instead of the usual UV(coords) helper. That helper divides by
// DrawSize, but EnemyVFX.Draw sets the DrawSize uniform to the *texture* size (not the on-screen
// size) whenever no source rectangle is supplied, which is every Eland call - so UV(c) == c here
// and the divide is pure cost. ps_2_0 only allows 64 arithmetic slots and the fog needs them.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Lingering poison gas. Deliberately has NO boundary ring: the old version drew a hard-edged
// square/circle outline (`boundary`) which read as a white box on screen. This is fog only - dense
// through the damage radius, then feathering out well past it so the cloud has no visible border.
//
// `Direction` is repurposed as the damage-radius ratio: the fraction of the quad's half-extent that
// is actually the damaging area. EnemyVFX.DrawElandToxicField oversizes the quad by FogVisualScale
// and passes 1/FogVisualScale here, so the feather lives in the extra margin. Keep those in sync.
float4 ToxicField(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = c;
    float r = length(uv - 0.5);

    // Marilith-style multi-layer noise: two samples of the same cloud drifting in opposing
    // directions, so the fog churns without any repeating scroll line.
    float n1 = tex2D(DetailSampler, uv * 1.9 + float2( Time * 0.031, -Time * 0.052)).r;
    float n2 = tex2D(DetailSampler, uv * 3.7 + float2(-Time * 0.043,  Time * 0.028)).r;
    float bubbles = tex2D(PrimarySampler, uv * 2.4 + float2(Time * 0.017, -Time * 0.075)).r;
    float billow = saturate(n1 * 0.72 + n2 * 0.54 - 0.16);

    // Noise-modulated falloff (Marilith firewall trick): the cloud reaches further out in some
    // directions than others, so the outer edge is ragged and organic rather than a clean circle.
    float damageEdge = 0.5 * Direction;
    float feather = max(0.5 - damageEdge, 0.02);
    float radial = 1.0 - smoothstep(damageEdge - feather * 0.55, 0.5, r + (billow - 0.5) * feather * 1.1);

    // GUARANTEED CIRCULAR CUTOFF. The billow offset above subtracts up to ~0.06 from the effective
    // radius, so near the quad boundary the gas was still partly opaque when the square quad clipped
    // it - that straight cut is what read as a hard rectangle edge on the big fog and as a "square
    // with a circle inside" on the trail puffs. This term is noise-INDEPENDENT and provably reaches
    // zero before r = 0.5 in every direction, so no straight edge can ever survive. Squaring it also
    // turns the interior from a flat plateau into a dome, which is what makes small puffs read as
    // rounded globs rather than discs.
    float quadFade = saturate((0.5 - r) * 7.0);
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
    return float4(color, alpha) * v;
}

// Flying venom projectile: a rounded head with a tapering, churning tail. One shape field feeds
// body / rim / core, so the bright membrane edge comes free from the same maths (a resolution-
// independent stand-in for Gwyn's texel-neighbour rim, which needs real sprite alpha and would be
// sub-pixel at this ~54x18 on-screen size).
float4 VenomGlob(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = c;
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
    return float4(color * energy, saturate(body + core + rim * 0.7) * Opacity) * v;
}

// Corrosive splat. The expanding edge radius is perturbed by voronoi cells so it throws irregular
// lobes instead of the old perfectly circular thin ring, and a haze skirt reaches past the splat so
// it dissipates rather than snapping off.
float4 VenomImpact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = c;
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
    return float4(color * (dens * 0.8 + rim * 1.15), saturate(dens + rim * 0.7) * Opacity) * v;
}

technique ElandToxicField { pass P { PixelShader = compile ps_2_0 ToxicField(); } }
technique ElandVenomGlob { pass P { PixelShader = compile ps_2_0 VenomGlob(); } }
technique ElandVenomImpact { pass P { PixelShader = compile ps_2_0 VenomImpact(); } }
