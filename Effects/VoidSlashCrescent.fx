#include "PixelShaderCommon.fxh"

// Abyss material for VanillaSwordArc's overlay pass (CinderOverlayStyle = Void, Artorias's melee sweeps).
// Premultiplied AlphaBlend. It is NitoReaperSweep's look (NitoReaperTrail.fx, left untouched for Nito and
// the Echo Step phantom) re-expressed inside the crescent sprite: the sprite's alpha is the silhouette, so
// the material can never drift off the tracked blade the way the old free-floating quad did.
sampler PrimaryTexture : register(s0);   // VanillaSwordArc.png, 4 x 170px frames, hard 0/255 alpha
sampler FlowNoise : register(s1);        // Turbulence_06 - seamless, so it can scroll continuously

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;

// All pre-divided in VanillaSwordArc.DrawCinderOverlay (ps_2_0 has no preshader).
float2 FrameMin;          // atlas UV of the current frame's first texel centre
float2 FrameMax;          // atlas UV of its last texel centre
float2 FrameUVScale;      // atlas UV -> frame units (texture size / frame size)
float4 PixelGrid;         // 2x2 screen-pixel blocks in atlas UV: xy = count, zw = reciprocal

float4 VoidSlashCrescentPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);

    // Hard-edged mask. The clamp matters: pixel blocks are atlas-aligned, not frame-aligned, so a block
    // on the frame border would otherwise read a texel of the neighbouring frame.
    float mask = tex2D(PrimaryTexture, clamp(uv, FrameMin, FrameMax)).a;

    // Frame-local p in [-1,1]. Every frame's crescent hugs the +X side, and the projectile rotates +X onto
    // the blade, so r is ACROSS the arc (outer rim ~0.93 = the blade tip's path, inner edge ~0.3 = the
    // wake) and p.y runs ALONG it.
    float2 p = (uv - FrameMin) * FrameUVScale * 2.0 - 1.0;
    float r = length(p);

    // Low frequency along the arc, higher across it, so features stretch into streaks that follow the
    // sweep instead of reading as puffs (same flow construction as NitoReaperSweep).
    float2 flowUV = float2(r * 1.30 - Time * 0.55, p.y * 0.55 + Time * 0.10);
    float shape = tex2D(FlowNoise, flowUV).r;
    float detail = tex2D(FlowNoise, flowUV * 1.90 + float2(Time * 0.31, -Time * 0.12)).r;

    // Crisp leading rim, ragged trailing edge - that asymmetry is what makes it read as a cut. Noise only
    // ever eats the INNER (wake) side; the outer rim stays the sprite's own hard edge on the blade tip.
    // Where the wake is eaten away, the purple vanilla crescent drawn underneath still shows through.
    float tail = saturate((r - 0.34 - shape * 0.42) * 3.20);
    float body = mask * tail;
    float heat = body * (0.42 + detail * 0.85);
    float edge = mask * saturate((r - 0.76) * 6.0) * (0.55 + detail * 0.90);

    // Dark body occludes over bright sky; violet heat and the pale rim stay emissive on top.
    float alpha = saturate(body * 1.25 + edge * 0.35) * Opacity;
    float3 color = DarkColor * (body * 0.95)
        + MidColor * (heat * 0.85)
        + CoreColor * (edge * edge * 0.95);
    return float4(color * Opacity, alpha);
}

technique VoidSlashCrescent
{
    pass VoidSlashCrescentPass
    {
        PixelShader = compile ps_2_0 VoidSlashCrescentPixel();
    }
}
