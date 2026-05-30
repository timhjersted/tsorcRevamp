using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Wings
{
    /// <summary>
    /// Draws a generic wings sprite (NPCs/Wings/Wings_7.png) behind an NPC.
    /// For NPCs that do NOT use the player draw pipeline (so vanilla wing layers
    /// can't be reused).  Frame is selected from an <see cref="EnemyFlightController"/>'s
    /// <see cref="EnemyFlightController.WingAnimPhase"/> so flap-and-glide cadence is consistent
    /// between this path and the puppet-puppet path.
    ///
    /// Call from the NPC's PreDraw BEFORE it draws its own sprite, e.g.:
    ///   EnemyWingsDrawLayer.Draw(NPC, _flight, spriteBatch, screenPos, lightColor);
    ///   // then draw the body sprite as usual
    /// </summary>
    public static class EnemyWingsDrawLayer
    {
        private const string DefaultTexturePath = "tsorcRevamp/NPCs/Wings/Wings_7";
        private const int FrameCount = 4;

        private static Asset<Texture2D> _defaultAsset;

        /// <summary>
        /// Draw wings behind the NPC at its current position.
        /// </summary>
        /// <param name="npc">The NPC to draw wings for.</param>
        /// <param name="flight">Flight controller (null = static spread pose with no animation).</param>
        /// <param name="spriteBatch">Active sprite batch.</param>
        /// <param name="screenPos">Main.screenPosition (passed in by PreDraw).</param>
        /// <param name="drawColor">Light color from PreDraw.</param>
        /// <param name="texturePath">Optional override (defaults to NPCs/Wings/Wings_7).</param>
        /// <param name="offset">Optional offset from NPC.Center (in pre-flip space).</param>
        /// <param name="scale">Scale applied to the wing sprite.</param>
        public static void Draw(NPC npc,
                                EnemyFlightController flight,
                                SpriteBatch spriteBatch,
                                Vector2 screenPos,
                                Color drawColor,
                                string texturePath = null,
                                Vector2 offset = default,
                                float scale = 1f)
        {
            // Don't draw wings while fully grounded (would visually clip into the body pose).
            if (flight != null && flight.Mode == FlightMode.Grounded)
                return;

            Asset<Texture2D> asset;
            if (string.IsNullOrEmpty(texturePath))
            {
                _defaultAsset ??= ModContent.Request<Texture2D>(DefaultTexturePath);
                asset = _defaultAsset;
            }
            else
            {
                asset = ModContent.Request<Texture2D>(texturePath);
            }

            if (asset == null || !asset.IsLoaded) return;
            Texture2D tex = asset.Value;

            int frameHeight = tex.Height / FrameCount;
            int frame = flight != null
                ? (int)(flight.WingAnimPhase * FrameCount) % FrameCount
                : 0;
            Rectangle src = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);

            Vector2 worldPos = npc.Center + new Vector2(offset.X * npc.direction, offset.Y);
            Vector2 drawPos  = worldPos - screenPos;
            Vector2 origin   = new Vector2(tex.Width / 2f, frameHeight / 2f);

            SpriteEffects fx = npc.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            spriteBatch.Draw(tex, drawPos, src, drawColor, 0f, origin, scale, fx, 0f);
        }
    }
}
