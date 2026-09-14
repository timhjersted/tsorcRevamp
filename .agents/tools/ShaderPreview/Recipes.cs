using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShaderPreview
{
    /// <summary>
    /// One shader draw, mirroring the argument list of ArtoriasVFX's shared private Draw(). Field
    /// names match its parameters deliberately, so a recipe can be checked against a call site by
    /// reading the two side by side.
    /// </summary>
    internal sealed class Recipe
    {
        public string Name;
        public string Effect;          // .xnb file stem under Effects/
        public string Technique;
        public string Primary;         // sampled by PrimarySampler (s0) AND drawn as the quad
        public string Detail;          // bound to s1
        public Vector2 DrawSize;
        public float Rotation;
        public Color Dark;
        public Color Mid;
        public Color Accent;
        public Color Core;
        public float Opacity = 1f;
        public float Progress;
        public float Active = 1f;
        public float Direction = 1f;
        public BlendState Blend = BlendState.AlphaBlend;
        public bool FullTexture;
        public int SourceColumns = 1;
        public int SourceRows = 1;
        public int SourceFrameCount = 1;
        public Vector2[] SourceFrameCenterOffsets;
        public float MinimumProgressScale = 1f;
        public float PixelBlockSize = 5f;
        public float Time = 12.5f;     // stands in for Main.GlobalTimeWrappedHourly
    }

    /// <summary>
    /// Transcribed from the call sites in Projectiles/Enemy/ArtoriasVFX.cs.
    ///
    /// This is a MIRROR, not the source of truth - the drift risk the vfx-shader-tips skill warns
    /// about is real, but it is now confined to these constants rather than covering all the HLSL
    /// maths as the hand-ported harness does. `--verify` re-reads ArtoriasVFX.cs and reports whether
    /// the technique/texture/blend triples here still appear there.
    ///
    /// Colours named below are ArtoriasVFX's own palette constants.
    /// </summary>
    internal static class Recipes
    {
        private static readonly Color VoidBlack = new Color(6, 2, 14);
        private static readonly Color AbyssIndigo = new Color(38, 16, 78);
        private static readonly Color AbyssViolet = new Color(96, 40, 168);
        private static readonly Color KnightSilver = new Color(198, 200, 214);
        private static readonly Color DangerMagenta = new Color(214, 44, 148);
        private static readonly Vector2[] VoidExplosionFrameCenterOffsets =
        {
            new(0.5f, 33.5f), new(0f, 31f), new(0.5f, 30.5f), new(0.5f, 30f), new(0.5f, 28f),
            new(0.5f, 25.5f), new(1.5f, 23f), new(1.5f, 20.5f), new(2f, 18f), new(2.5f, 16f),
            new(3f, 15f), new(4f, 14f), new(3f, 12.5f), new(2f, 10f), new(1.5f, 8f),
            new(1.5f, 6f), new(1f, 4.5f), new(1f, 3f), new(1f, 1f), new(1f, -0.5f),
            new(1f, -1f), new(0f, -1f), new(0f, -2f), new(0f, -2.5f), new(0f, -2f),
        };

        internal static Recipe[] All()
        {
            return new[]
            {
                // DrawSwordSwipe(center, rotation, size, progress, opacity)
                // 94x110 is the real size AbyssSlash.cs passes, and the aspect matters a lot here:
                // the arc is an SDF over p = uv*2-1, so a landscape quad bends it into a different
                // shape entirely. It is also the size the approved preview was rendered at.
                new Recipe
                {
                    Name = "SwordSwipe",
                    Effect = "ArtoriasSwordSwipe",
                    Technique = "ArtoriasSwordSwipe",
                    Primary = "T_MarbleNoise_tiled",
                    Detail = "Vein_04-512x512",
                    DrawSize = new Vector2(94f, 110f),
                    Rotation = 0f,
                    Dark = new Color(8, 2, 20),
                    Mid = new Color(102, 32, 176),
                    Core = new Color(232, 68, 198),
                    Progress = 0.5f,
                    Active = 1f,
                    Direction = 1f,
                    Blend = BlendState.AlphaBlend,
                    FullTexture = true,
                    PixelBlockSize = 2f,   // DrawSwordSwipe passes this explicitly
                },

                // Largest live Artorias charge: the damage radius is exactly 90% of the final visual.
                new Recipe
                {
                    Name = "VoidExplosionCharge",
                    Effect = "VoidExplosion",
                    Technique = "VoidExplosionCharge",
                    Primary = "T_fire_flipbook4_sm",
                    Detail = "TurbulentNoise",
                    DrawSize = Vector2.One * (1400f / 0.9f),
                    Dark = new Color(5, 1, 14),
                    Mid = new Color(86, 20, 146),
                    Accent = new Color(226, 52, 166),
                    Core = new Color(246, 232, 255),
                    Opacity = 0.62f,
                    Direction = 1f,
                    Blend = BlendState.AlphaBlend,
                    SourceColumns = 5,
                    SourceRows = 5,
                    SourceFrameCount = 13,
                    SourceFrameCenterOffsets = VoidExplosionFrameCenterOffsets,
                    MinimumProgressScale = 0.1f,
                    PixelBlockSize = 2f,
                },

                // Largest live Artorias blast, preserving the 90%-hitbox/100%-visual contract.
                new Recipe
                {
                    Name = "VoidExplosionBlast",
                    Effect = "VoidExplosion",
                    Technique = "VoidExplosionBlast",
                    Primary = "T_fire_flipbook4_sm",
                    Detail = "TurbulentNoise",
                    DrawSize = Vector2.One * (1400f / 0.9f),
                    Dark = new Color(5, 1, 14),
                    Mid = new Color(86, 20, 146),
                    Accent = new Color(226, 52, 166),
                    Core = new Color(246, 232, 255),
                    Opacity = 0.92f,
                    Direction = 1f,
                    Blend = BlendState.AlphaBlend,
                    SourceColumns = 5,
                    SourceRows = 5,
                    SourceFrameCount = 25,
                    SourceFrameCenterOffsets = VoidExplosionFrameCenterOffsets,
                    PixelBlockSize = 2f,
                },

                // AbyssShard's 60-tick tile-surface warning. The quad is bottom-anchored by its
                // call site; this recipe previews its exact 80x52 world size and 2px filter.
                new Recipe
                {
                    Name = "AbyssShardPortal",
                    Effect = "ArtoriasAbyssEruption",
                    Technique = "ArtoriasAbyssShardPortal",
                    Primary = "T_VFX_NoiseF1",
                    Detail = "TurbulentNoise",
                    DrawSize = new Vector2(80f, 52f),
                    Dark = new Color(5, 1, 14),
                    Mid = new Color(92, 26, 154),
                    Core = new Color(242, 226, 255),
                    Opacity = 0.90f,
                    Direction = 0.37f,
                    Blend = BlendState.AlphaBlend,
                    FullTexture = true,
                    PixelBlockSize = 2f,
                },

                // DrawCrescent(center, rotation, size, returning: false, opacity)
                new Recipe
                {
                    Name = "Crescent",
                    Effect = "ArtoriasAbyssProjectile",
                    Technique = "ArtoriasCrescent",
                    Primary = "T_Gradient_circle22",
                    Detail = "T_VFX_Noise41",
                    DrawSize = new Vector2(150f, 110f),
                    Dark = VoidBlack,
                    Mid = AbyssViolet,
                    Core = KnightSilver,
                    Progress = 0f,
                    Active = 0f,
                    Direction = 1f,
                    Blend = BlendState.Additive,
                },

                // DrawTendril's core pass: Draw(tendrilEffect, "ArtoriasTendrilCore", windstreak, ...)
                new Recipe
                {
                    Name = "TendrilCore",
                    Effect = "ArtoriasAbyssTendril",
                    Technique = "ArtoriasTendrilCore",
                    Primary = "T_Windstreak3",
                    Detail = "T_VFX_Noise41",
                    DrawSize = new Vector2(220f, 46f),
                    Dark = VoidBlack,
                    Mid = AbyssViolet,
                    Core = KnightSilver,
                    Progress = 0.7f,   // tension
                    Active = 0.7f,     // tension again, per the call site
                    Blend = BlendState.Additive,
                },

                // DrawBoundary's mirrored edge, non-warning. Active/Direction carry radius and
                // halfWidth rather than 0..1 flags - this shader reuses those slots.
                new Recipe
                {
                    Name = "BoundaryEdge",
                    Effect = "ArtoriasAbyssBoundary",
                    Technique = "ArtoriasAbyssBoundaryEdge",
                    Primary = "TurbulentNoise",
                    Detail = "TurbulentNoise",
                    DrawSize = Vector2.One * 800f,
                    Dark = new Color(7, 1, 18),
                    Mid = new Color(108, 24, 174),
                    Core = new Color(210, 72, 214),
                    Opacity = 0.82f,
                    Progress = 0f,
                    Active = 320f,     // radius
                    Direction = 40f,   // halfWidth
                    Blend = BlendState.Additive,
                },

                // DrawHomingVolleyOrb's dedicated irregular abyss knot.
                new Recipe
                {
                    Name = "HomingVolleyOrb",
                    Effect = "ArtoriasHomingVolley",
                    Technique = "ArtoriasHomingVolleyOrb",
                    Primary = "T_MarbleNoise_tiled",
                    Detail = "TurbulentNoise",
                    DrawSize = new Vector2(48f, 48f),
                    Dark = new Color(5, 1, 14),
                    Mid = new Color(122, 28, 186),
                    Core = new Color(224, 176, 246),
                    Opacity = 0.94f,
                    Progress = 0.5f,
                    Active = 0.5f,
                    Blend = BlendState.AlphaBlend,
                    FullTexture = true,
                    PixelBlockSize = 2f,
                },

                // DrawHomingFlameWisp's largest live wake layer (32x104 at the projectile head).
                new Recipe
                {
                    Name = "HomingVolleyWake",
                    Effect = "ArtoriasHomingVolley",
                    Technique = "ArtoriasHomingVolleyWake",
                    Primary = "T_VFX_NoiseF1",
                    Detail = "TurbulentNoise",
                    DrawSize = new Vector2(32f, 104f),
                    Dark = new Color(4, 1, 12),
                    Mid = new Color(112, 24, 180),
                    Core = new Color(224, 164, 246),
                    Opacity = 0.88f,
                    Progress = 0.5f,
                    Active = 0.5f,
                    Blend = BlendState.AlphaBlend,
                    FullTexture = true,
                    PixelBlockSize = 2f,
                },
            };
        }
    }
}
