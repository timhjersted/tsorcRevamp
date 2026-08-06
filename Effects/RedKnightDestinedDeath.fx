// RedKnightDestinedDeath.fx
// The Red Knight family's "Destined Death" material: genuine BLACK flame mixed with crimson fire,
// plus the crimson lightning and storm-gather that share its colour language.
//
// EVERY technique here is authored for BlendState.AlphaBlend, which in XNA is PREMULTIPLIED
// (One / InverseSourceAlpha). That is the whole reason the black flame can read as black: the sooty
// body punches a dark hole in the background (rgb < alpha) and the embers add light back over it
// (rgb > alpha). See vfx-shader-tips SKILL.md §43 — an additive-only version of this effect cannot
// produce a saturated deep red over Terraria's daytime sky, it clips to white.
//
// Sampler contract (bound by RedKnightVFX.DrawDestinedDeathQuad):
//   s1 MacroSampler  = Textures/Noise/T_Noise_6Yu1        smooth blobby macro noise -> SILHOUETTE
//   s2 DetailSampler = Textures/Noise/Turbulence_07-512x512 billowing filaments      -> TEXTURE
// Both tile seamlessly (verified with preview/ContactSheet.ps1 -Tile2x2); every technique here
// scrolls its samples, so a seam would strobe.
//
// The silhouette is taken off the MACRO layer alone and the fine layer only textures what the macro
// layer already decided exists (§46) — sharing one combined field between the two chews the outline
// into confetti.

sampler BaseSampler : register(s0);
sampler MacroSampler : register(s1);
sampler DetailSampler : register(s2);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Intensity;
float Active;
float Direction;
float2 DrawSize;

// ---------------------------------------------------------------------------------------------
// 1. Destined Death flame — the general-purpose black+red flame body.
//    Bottom-anchored: uv.y == 1 is the ground line / the caster's feet, uv.y == 0 is the tip.
//    Used by the Royal / Crimson Standard ground shockwaves AND by Crimson Dominion's body engulf.
// ---------------------------------------------------------------------------------------------
float4 DestinedDeathFlamePixel(float2 uv : TEXCOORD0) : COLOR0
{
    float rise = 1.0 - uv.y;
    float x = uv.x + Direction * 0.137;   // Direction doubles as a per-instance phase offset

    // HIGH sampling frequency across the column, LOW up it, so noise features elongate VERTICALLY
    // and read as flame tongues instead of popcorn (§45). The two layers drift in opposing
    // directions at different speeds so they never share a phase and never show a scroll line.
    float macroN = tex2D(MacroSampler, float2(x * 2.90 - Time * 0.13, uv.y * 1.05 - Time * 0.62)).r;
    float fineN = tex2D(DetailSampler, float2(x * 5.40 + Time * 0.22, uv.y * 1.95 - Time * 1.08)).r;

    float shape = saturate(macroN * 1.30 - 0.18);              // macro only -> silhouette
    float fire = saturate(macroN * 0.70 + fineN * 0.52 - 0.10); // combined  -> colour detail

    // span is 0 at both side edges. reach maxes at 1 * (0.30 + 0.54) * 1.0 = 0.84 < 1.0, and the
    // baseFade term is 0 at uv.y == 1. So `body` provably reaches zero before ALL FOUR quad edges
    // no matter what the noise does (§31 / §47) — nothing here can paint the boundary.
    float span = saturate(uv.x * 7.0) * saturate((1.0 - uv.x) * 7.0);

    // Smooth in and out; never a hard pop at either end of the effect's life. NOTE the `* span`:
    // the D3DX EFFECT compiler cannot compile a saturate() whose argument is purely uniform, and
    // reports it as a bogus "error compiling expression" on the technique line (vfx-pipeline §2).
    // Folding the per-pixel `span` in both dodges that and is a slot cheaper than two saturates.
    float surge = 0.42 + saturate(min(Progress * 5.0, (1.06 - Progress) * 3.2) * span) * 0.58;
    float reach = span * (0.30 + shape * 0.54) * surge;
    float baseFade = saturate(rise * 40.0);
    float body = saturate((reach - rise) * 6.5) * span * baseFade;

    // Black flame vs red flame. Noise is the SMALLER half of the ember term (§46) so the fire
    // undulates instead of speckling; everything the ember term does not claim stays sooty and
    // OCCLUDES, which is what makes the black read as black rather than as a dark tint.
    float ember = saturate(fire * 1.45 - 0.34);
    float core = saturate((reach * 0.46 - rise) * 8.0) * span * baseFade
        * saturate(fire * 1.30 - 0.42);

    // NOTE: the emissive term MUST be gated by `body`. An ungated `MidColor * ember` is non-zero
    // everywhere in the quad, and under premultiplied alpha that paints a flat tinted rectangle
    // exactly the size of the draw quad wherever alpha is 0 (§43). Caught in the first preview.
    float alpha = saturate(body * (0.82 + ember * 0.22)) * Opacity;
    float3 rgb = lerp(DarkColor, MidColor, ember * ember) * alpha
        + (MidColor * body * ember * 0.46 + CoreColor * core * 0.85) * Opacity * Intensity;
    return float4(rgb, alpha);
}

// ---------------------------------------------------------------------------------------------
// 2. Destined Death seal — Crimson Dominion's finishing circle as it FILLS.
//    Quad is exactly the arena diameter, so r == 1.0 is the arena boundary.
// ---------------------------------------------------------------------------------------------
float4 DestinedDeathSealPixel(float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;

    // Ordinary UV space, NOT ring space: this is a volume, and ring-space sampling smears every
    // feature radially into a starburst of spokes (§49). Contracting the sampling scale as the
    // seal fills makes the cells appear to rush outward.
    float expand = 1.0 - Progress * 0.42;
    float macroN = tex2D(MacroSampler, p * (expand * 3.10) + (0.5 + Time * 0.05)).r;
    float fineN = tex2D(DetailSampler, p * (expand * 6.20) + (0.5 - Time * 0.09)).r;
    float churn = saturate(macroN * 0.78 + fineN * 0.46 - 0.14);

    float grow = Progress * (2.0 - Progress);   // fast-then-stalling; cheaper than sqrt (§50)
    float edge = 0.10 + grow * 0.80;            // <= 0.90
    float front = edge + (churn - 0.5) * 0.10;  // <= 0.95

    // Noise-independent cutoff, provably zero at r == 1.0 which is inside the quad's corners (§31).
    // Squared so the darkness domes toward the middle instead of ending on a flat lip.
    float quadFade = saturate((1.0 - r) * 4.0);
    quadFade *= quadFade;

    float filled = saturate((front - r) * 7.0) * quadFade;
    float rim = saturate(1.0 - abs(r - front) * 9.0) * quadFade;

    // Red flame energy INTENSIFIES toward the circle's edge; the interior stays black darkness.
    float heat = saturate(r * 1.20);
    float ember = filled * saturate(churn * 1.25 - 0.30) * (0.20 + heat * heat * 1.30);

    float alpha = saturate(filled * (0.88 + churn * 0.22) + rim * 0.55) * Opacity;
    float3 rgb = DarkColor * alpha
        + (MidColor * ember * 0.90
            + CoreColor * rim * (0.30 + grow * 0.95) * saturate(churn * 1.30 + 0.10))
        * Opacity * Intensity;
    return float4(rgb, alpha);
}

// ---------------------------------------------------------------------------------------------
// 3. Destined Death blast — the detonation the seal culminates in. Black smoke shell with red
//    flame boiling through it and a white-hot heart.
// ---------------------------------------------------------------------------------------------
float4 DestinedDeathBlastPixel(float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;

    float expand = 1.0 - Progress * 0.50;
    float macroN = tex2D(MacroSampler, p * (expand * 2.60) + (0.5 - Time * 0.12)).r;
    float fineN = tex2D(DetailSampler, p * (expand * 5.50) + (0.5 + Time * 0.18)).r;
    float churn = saturate(macroN * 0.82 + fineN * 0.48 - 0.12);

    float grow = Progress * (2.0 - Progress);
    float front = 0.26 + grow * 0.66 + (churn - 0.5) * 0.12;   // <= 0.98

    float quadFade = saturate((1.0 - r) * 3.4);
    quadFade *= quadFade;

    float ball = saturate((front - r) * 4.4) * quadFade;
    float shell = saturate(1.0 - abs(r - front) * 5.2) * quadFade;
    float ember = saturate(churn * 1.35 - 0.26);
    float heart = saturate((front * 0.42 - r) * 3.6) * quadFade;

    float alpha = saturate(ball * (0.84 + ember * 0.26) + shell * 0.52) * Opacity;
    float3 rgb = lerp(DarkColor, MidColor, ember * ember) * alpha
        + (MidColor * (shell * 0.72 + ball * ember * 0.45)
            + CoreColor * heart * 1.10) * Opacity * Intensity;
    return float4(rgb, alpha);
}

// ---------------------------------------------------------------------------------------------
// 4. Crimson lightning bolt — the ONE standard lightning look for the whole Red Knight family.
//    uv.x runs along the bolt, uv.y across it. Active == 1 is the strike, 0 is the telegraph.
//    (Replaces the reused Gwyn SunlightJudgment column, which was tinted storm-BLUE.)
// ---------------------------------------------------------------------------------------------
float4 LightningBoltPixel(float2 uv : TEXCOORD0) : COLOR0
{
    float along = uv.x;
    float across = uv.y - 0.5;

    // LOW sampling frequency along the bolt, so the kinks are long and blade-like rather than fuzz
    // (§45). Direction supplies a per-instance row offset so twelve simultaneous bolts do not all
    // trace the identical path (§33) — no extra uniform needed.
    float coarse = tex2D(MacroSampler, float2(along * 2.60 - Time * 1.35, Direction * 0.37)).r;
    float fine = tex2D(DetailSampler, float2(along * 7.40 + Time * 2.10, Direction * 0.61)).r;

    // taper is 0 at both ends of the bolt. Centre-line wander maxes at (0.60+0.30)*0.5*0.44 = 0.198
    // and the glow band reaches zero 0.26 away from it, so 0.198 + 0.26 = 0.458 < 0.5: alpha
    // provably hits zero before all four quad edges regardless of the noise (§31 / §47).
    float taper = saturate(along * 9.0) * saturate((1.0 - along) * 3.0);
    float jitter = ((coarse - 0.5) * 0.60 + (fine - 0.5) * 0.30) * (0.30 + Active * 0.14);
    float d = abs(across - jitter * taper);

    float glow = saturate((0.26 - d) * 4.6) * taper;
    float body = glow * glow * (0.45 + coarse * 0.55);
    float core = saturate((0.055 + Active * 0.045 - d) * 22.0) * taper;
    float reveal = 0.35 + Progress * 0.65;
    float flicker = 0.72 + 0.28 * fine;

    float alpha = saturate(body * (0.30 + Active * 0.55) + core * (0.55 + Active * 0.45))
        * Opacity * reveal;
    float3 rgb = DarkColor * body * 0.9
        + MidColor * body * (0.85 + Active * 0.95) * flicker
        + CoreColor * core * (0.60 + Active * 1.25);
    return float4(rgb * Opacity * reveal, alpha);
}

// ---------------------------------------------------------------------------------------------
// 5. Storm Herald gather — crimson, not blue. A dark occluding storm body with branching crimson
//    discharge crackling through it, blooming out of the knight over the herald's 150 ticks.
// ---------------------------------------------------------------------------------------------
float4 StormHeraldPixel(float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;

    // BOTH layers are sampled in ordinary UV space. This is a BODY, and ring-space sampling smears
    // every feature radially — the first two previews of this technique came out as a pinwheel of
    // spokes converging on the centre, which is §49's failure mode exactly. The detail layer's
    // scale CONTRACTS with radius so the cells appear to boil outward instead of sitting still.
    float storm = tex2D(MacroSampler, p * 2.40 + (0.5 - Time * 0.06)).r;
    float arcs = tex2D(DetailSampler, p * (2.90 - r * 0.90) + (0.5 + Time * 0.115)).r;

    float bloom = Progress * (2.0 - Progress);
    float radius = 0.30 + bloom * 0.56;                        // <= 0.86
    float quadFade = saturate((1.0 - r) * 3.2);
    quadFade *= quadFade;
    float cloud = saturate((radius + (storm - 0.5) * 0.14 - r) * 3.2) * quadFade;

    // Crackle is the thin RIDGE of the detail layer (its mid-level contour), not its bulk, so it
    // reads as branching discharge threading the storm rather than as a texture laid over it.
    float crack = saturate(1.0 - abs(arcs - 0.52) * 12.0) * saturate(storm * 1.55 - 0.28);
    float filament = cloud * crack * (0.30 + bloom * 1.00);
    float rim = saturate(1.0 - abs(r - radius) * 6.5) * quadFade * storm;
    float eye = saturate((0.20 - r) * 5.0) * bloom;

    float alpha = saturate(cloud * (0.78 + storm * 0.28) + rim * 0.35) * Opacity;
    float3 rgb = DarkColor * alpha
        + (MidColor * (filament * 1.05 + rim * 0.60 + eye * 0.50)
            + CoreColor * (filament * filament * 1.25 + eye * eye * 0.85)) * Opacity;
    return float4(rgb, alpha);
}

technique DestinedDeathFlame
{
    pass FlamePass { PixelShader = compile ps_2_0 DestinedDeathFlamePixel(); }
}

technique DestinedDeathSeal
{
    pass SealPass { PixelShader = compile ps_2_0 DestinedDeathSealPixel(); }
}

technique DestinedDeathBlast
{
    pass BlastPass { PixelShader = compile ps_2_0 DestinedDeathBlastPixel(); }
}

technique RedKnightLightningBolt
{
    pass BoltPass { PixelShader = compile ps_2_0 LightningBoltPixel(); }
}

technique RedKnightStormHerald
{
    pass HeraldPass { PixelShader = compile ps_2_0 StormHeraldPixel(); }
}
