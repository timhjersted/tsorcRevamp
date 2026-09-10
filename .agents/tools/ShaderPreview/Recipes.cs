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
        public Color Core;
        public float Opacity = 1f;
        public float Progress;
        public float Active = 1f;
        public float Direction = 1f;
        public BlendState Blend = BlendState.AlphaBlend;
        public bool FullTexture;
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

                // DrawDetonation(center, radius, progress, opacity, active) with radius 110.
                new Recipe
                {
                    Name = "Detonation",
                    Effect = "ArtoriasAbyssDetonation",
                    Technique = "ArtoriasAbyssDetonation",
                    Primary = "T_Gradient_circle22",
                    Detail = "T_VFX_Noise41",
                    DrawSize = Vector2.One * 220f,
                    Dark = VoidBlack,
                    Mid = AbyssViolet,
                    Core = KnightSilver,
                    Progress = 0.55f,
                    Active = 1f,
                    Blend = BlendState.Additive,
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

                // DrawBoundary's inner seam, non-warning. Active/Direction carry radius and
                // halfWidth rather than 0..1 flags - this shader reuses those slots.
                new Recipe
                {
                    Name = "BoundaryInner",
                    Effect = "ArtoriasAbyssBoundary",
                    Technique = "ArtoriasAbyssBoundaryInner",
                    Primary = "TurbulentNoise",
                    Detail = "TurbulentNoise",
                    DrawSize = Vector2.One * 800f,
                    Dark = new Color(7, 1, 18),
                    Mid = new Color(108, 24, 174),
                    Core = new Color(210, 72, 214),
                    Opacity = 0.64f,
                    Progress = 0f,
                    Active = 320f,     // radius
                    Direction = 40f,   // halfWidth
                    Blend = BlendState.Additive,
                },

                // DrawHomingFlameWisp's shader - the one the naive spike rendered empty, because it
                // needs both a real DetailSampler texture and a non-zero PixelGrid.
                new Recipe
                {
                    Name = "PurpleFire",
                    Effect = "ArtoriasPurpleFire",
                    Technique = "ArtoriasPurpleFireBody",
                    Primary = "fire_01_a",
                    Detail = "TurbulentNoise",
                    DrawSize = new Vector2(96f, 96f),
                    Dark = VoidBlack,
                    Mid = AbyssViolet,
                    Core = KnightSilver,
                    Progress = 0.5f,
                    Active = 1f,
                    Blend = BlendState.Additive,
                    PixelBlockSize = 6f,
                },
            };
        }
    }
}
