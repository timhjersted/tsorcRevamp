using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Shared client-only aura renderer for Death, its Reapers and the held sickle.
    /// Every pass is shader-driven, so it takes the exact sprite frame, rotation and flip it is
    /// handed instead of approximating the silhouette.
    /// </summary>
    internal static class DeathVFX
    {
        const string GlowEffectPath = "tsorcRevamp/Effects/UnblockableGlow";

        static Asset<Effect> glowEffect;
        static bool loggedMissingGlowAsset;

        /// <summary>
        /// Vanilla draws NPC sheets as authored and flips them only when spriteDirection == 1
        /// (Main.DrawNPCDirect_Inner). A glow drawn from the same sprite has to flip on that same
        /// condition, otherwise it reads mirrored against the body it is meant to sit behind.
        /// </summary>
        internal static SpriteEffects GetVanillaNpcFlip(int spriteDirection)
        {
            return spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }

        static void LoadGlowAssets()
        {
            if (glowEffect != null)
            {
                return;
            }

            if (!ModContent.HasAsset(GlowEffectPath))
            {
                if (!loggedMissingGlowAsset)
                {
                    loggedMissingGlowAsset = true;
                    ModContent.GetInstance<tsorcRevamp>().Logger.Warn($"[DeathVFX] Missing effect asset: {GlowEffectPath}");
                }

                return;
            }

            glowEffect = ModContent.Request<Effect>(GlowEffectPath, AssetRequestMode.ImmediateLoad);
        }

        /// <summary>
        /// Shader-driven aura: draws the supplied sprite frame as a wide bloom, a bright rim and a
        /// tinted core. coreWhiteMix lerps the core colour toward white (0 = pure glow colour).
        /// Additive only - the effect returns a bare colour, so it must never run under AlphaBlend
        /// (that paints a flat rectangle the size of the quad).
        /// </summary>
        internal static void DrawShaderGlow(
            Texture2D texture,
            Rectangle? sourceRectangle,
            Vector2 position,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            Color color,
            float opacity,
            float haloRadius = 3f,
            int haloDirections = 10,
            float bloomScale = 1.25f,
            float bloomOpacity = 0.30f,
            float rimOpacity = 0.50f,
            float coreOpacity = 0.45f,
            float coreWhiteMix = 0.7f,
            float phase = 0f)
        {
            if (Main.dedServ || opacity <= 0f || texture == null || texture.IsDisposed)
            {
                return;
            }

            LoadGlowAssets();
            if (glowEffect == null)
            {
                return;
            }

            Rectangle source = sourceRectangle ?? texture.Bounds;
            // Breathing: a slow swell used by every Death entity, offset per instance via phase.
            float pulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + phase);
            float drawOpacity = MathHelper.Clamp(opacity * pulse, 0f, 1f);
            Vector3 glowTint = color.ToVector3();
            Vector3 coreTint = Color.Lerp(color, Color.White, MathHelper.Clamp(coreWhiteMix, 0f, 1f)).ToVector3();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            try
            {
                Effect effect = glowEffect.Value;
                effect.CurrentTechnique = effect.Techniques["UnblockableGlow"];

                // Wide bloom: an over-scaled copy of the frame so the aura reads around the sprite
                // rather than only on top of it.
                effect.Parameters["glowColor"]?.SetValue(glowTint);
                effect.Parameters["coreColor"]?.SetValue(coreTint);
                effect.Parameters["coreAmount"]?.SetValue(0f);
                effect.Parameters["opacity"]?.SetValue(drawOpacity * bloomOpacity);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition,
                    source,
                    Color.White,
                    rotation,
                    origin,
                    scale * bloomScale,
                    effects,
                    0f);

                // Bright rim: the silhouette stamped around a ring, which is what makes the outline
                // readable against the dark arena.
                effect.Parameters["coreAmount"]?.SetValue(0f);
                effect.Parameters["opacity"]?.SetValue(drawOpacity * rimOpacity);
                effect.CurrentTechnique.Passes[0].Apply();
                for (int direction = 0; direction < haloDirections; direction++)
                {
                    float angle = MathHelper.TwoPi * direction / haloDirections;
                    Vector2 offset = angle.ToRotationVector2() * haloRadius * scale;
                    Main.EntitySpriteDraw(
                        texture,
                        position - Main.screenPosition + offset,
                        source,
                        Color.White,
                        rotation,
                        origin,
                        scale,
                        effects,
                        0f);
                }

                // Inner core: reads as the entity being lit rather than flatly recoloured.
                effect.Parameters["glowColor"]?.SetValue(glowTint);
                effect.Parameters["coreColor"]?.SetValue(coreTint);
                effect.Parameters["coreAmount"]?.SetValue(0.55f);
                effect.Parameters["opacity"]?.SetValue(drawOpacity * coreOpacity);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition,
                    source,
                    Color.White,
                    rotation,
                    origin,
                    scale,
                    effects,
                    0f);
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }
    }
}
