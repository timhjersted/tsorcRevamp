using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    internal static class BasiliskHeldProjectileDraw
    {
        private const float FrameW = 60f;
        private const float FrameH = 60f;
        private const float HeldProjectileScale = 0.85f;

        private static readonly Vector2[] HandPixel = new Vector2[12]
        {
            new Vector2(43f, 36f),
            new Vector2(43f, 34f),
            new Vector2(45f, 37f),
            new Vector2(46f, 37f),
            new Vector2(46f, 38f),
            new Vector2(45f, 38f),
            new Vector2(44f, 38f),
            new Vector2(43f, 38f),
            new Vector2(42f, 37f),
            new Vector2(42f, 37f),
            new Vector2(42f, 36f),
            new Vector2(43f, 36f)
        };

        public static void Draw(NPC npc, SpriteBatch spriteBatch, Color drawColor, int projectileType, Color glowColor)
        {
            if (Main.dedServ || projectileType <= 0)
            {
                return;
            }

            Texture2D texture = TextureAssets.Projectile[projectileType].Value;
            int frameCount = Main.projFrames[projectileType] > 0 ? Main.projFrames[projectileType] : 1;
            int frameHeight = texture.Height / frameCount;
            int frame = frameCount == 1 ? 0 : (int)((Main.GameUpdateCount / 3) % (ulong)frameCount);
            Rectangle sourceRectangle = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Vector2 handWorld = CurrentHandWorld(npc);

            Lighting.AddLight(handWorld, glowColor.ToVector3() * 0.35f);
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(handWorld + Main.rand.NextVector2Circular(6f, 6f), DustID.GemEmerald, Main.rand.NextVector2Circular(0.25f, 0.25f), 120, glowColor, 1f);
                dust.noGravity = true;
            }

            spriteBatch.Draw(texture, handWorld - Main.screenPosition, sourceRectangle, drawColor, 0f, origin, npc.scale * HeldProjectileScale, SpriteEffects.None, 0f);
        }

        private static Vector2 CurrentHandWorld(NPC npc)
        {
            int frame = npc.frame.Height > 0 ? npc.frame.Y / npc.frame.Height : 0;
            if (frame < 0 || frame >= HandPixel.Length)
            {
                frame = 0;
            }

            Vector2 fp = HandPixel[frame];
            float x = npc.Center.X + (fp.X - FrameW / 2f) * npc.scale * -npc.spriteDirection;
            float y = npc.Center.Y + 24f + npc.gfxOffY + (fp.Y - FrameH) * npc.scale;
            return new Vector2(x, y);
        }
    }
}
