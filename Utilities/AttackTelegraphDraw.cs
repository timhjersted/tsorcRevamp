using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    /// <summary>Shared, dust-free weapon telegraphs for defensive-rule attacks.</summary>
    public static class AttackTelegraphDraw
    {
        private static readonly Vector2[] GlowDirections =
        {
            Vector2.UnitX,
            -Vector2.UnitX,
            Vector2.UnitY,
            -Vector2.UnitY,
            Vector2.Normalize(Vector2.One),
            Vector2.Normalize(new Vector2(1f, -1f)),
            Vector2.Normalize(new Vector2(-1f, 1f)),
            Vector2.Normalize(-Vector2.One),
        };

        private static Effect UnblockableGlowEffect;

        /// <summary>
        /// Draws a clean red/white energy silhouette from the supplied weapon or arm sprite. Because the source
        /// artwork is the mask, the tell follows the held equipment instead of forming an unrelated dust oval.
        /// Call this immediately before drawing the ordinary sprite so the real artwork remains crisp on top.
        /// </summary>
        public static void DrawUnblockableWeaponAura(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector2 screenPosition,
            Rectangle? sourceRectangle,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (Main.dedServ || texture == null || texture.IsDisposed)
            {
                return;
            }

            if (UnblockableGlowEffect == null || UnblockableGlowEffect.IsDisposed)
            {
                UnblockableGlowEffect = ModContent.Request<Effect>(
                    "tsorcRevamp/Effects/UnblockableGlow",
                    AssetRequestMode.ImmediateLoad).Value;
            }

            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            float outlineRadius = MathHelper.Lerp(1.6f, 2.8f, pulse) * Math.Max(1f, scale);

            UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);

            UnblockableGlowEffect.Parameters["glowColor"].SetValue(new Vector3(1f, 0.015f, 0.01f));
            UnblockableGlowEffect.Parameters["coreColor"].SetValue(new Vector3(1f, 0.9f, 0.72f));
            UnblockableGlowEffect.Parameters["coreAmount"].SetValue(0f);
            UnblockableGlowEffect.Parameters["opacity"].SetValue(MathHelper.Lerp(0.76f, 1f, pulse));
            UnblockableGlowEffect.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < GlowDirections.Length; i++)
            {
                spriteBatch.Draw(texture, screenPosition + GlowDirections[i] * outlineRadius, sourceRectangle,
                    Color.White, rotation, origin, scale, effects, 0f);
            }

            UnblockableGlowEffect.Parameters["coreAmount"].SetValue(MathHelper.Lerp(0.42f, 0.72f, pulse));
            UnblockableGlowEffect.Parameters["opacity"].SetValue(MathHelper.Lerp(0.82f, 1f, pulse));
            UnblockableGlowEffect.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(texture, screenPosition, sourceRectangle, Color.White, rotation, origin, scale, effects, 0f);

            UsefulFunctions.RestartSpritebatch(ref spriteBatch);
        }
    }
}
