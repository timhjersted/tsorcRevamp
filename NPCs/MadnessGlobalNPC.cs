using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Client-only hallucination trail seen by the local player during Madness. It owns a
    /// separate cache so it does not alter any NPC type's authored TrailCacheLength or AI.
    /// </summary>
    public class MadnessGlobalNPC : GlobalNPC
    {
        private const int TrailCount = 12;

        private readonly Vector2[] positions = new Vector2[TrailCount];
        private readonly Rectangle[] frames = new Rectangle[TrailCount];
        private readonly float[] rotations = new float[TrailCount];
        private readonly float[] scales = new float[TrailCount];
        private readonly int[] spriteDirections = new int[TrailCount];
        private bool initialized;

        public override bool InstancePerEntity => true;

        private static bool LocalMadnessActive
            => !Main.dedServ && Main.LocalPlayer.active && !Main.LocalPlayer.dead
            && Main.LocalPlayer.HasBuff(ModContent.BuffType<Madness>());

        private static bool IsEnemy(NPC npc)
            => npc.active && !npc.friendly && !npc.townNPC && npc.lifeMax > 5;

        public override void PostAI(NPC npc)
        {
            if (!LocalMadnessActive || !IsEnemy(npc))
            {
                initialized = false;
                return;
            }

            if (!initialized)
            {
                for (int i = 0; i < TrailCount; i++)
                {
                    CacheState(npc, i);
                }
                initialized = true;
                return;
            }

            for (int i = TrailCount - 1; i > 0; i--)
            {
                positions[i] = positions[i - 1];
                frames[i] = frames[i - 1];
                rotations[i] = rotations[i - 1];
                scales[i] = scales[i - 1];
                spriteDirections[i] = spriteDirections[i - 1];
            }
            CacheState(npc, 0);
        }

        private void CacheState(NPC npc, int index)
        {
            positions[index] = npc.Center;
            frames[index] = npc.frame;
            rotations[index] = npc.rotation;
            scales[index] = npc.scale;
            spriteDirections[index] = npc.spriteDirection;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!LocalMadnessActive || !IsEnemy(npc) || !initialized)
            {
                return true;
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Color madnessYellow = new Color(255, 205, 35);
            Vector2? lastDrawnPosition = null;

            for (int i = TrailCount - 1; i >= 0; i--)
            {
                if (Vector2.DistanceSquared(positions[i], npc.Center) < 1f
                    || (lastDrawnPosition.HasValue
                        && Vector2.DistanceSquared(positions[i], lastDrawnPosition.Value) < 1f)
                    || frames[i].Width <= 0 || frames[i].Height <= 0)
                {
                    continue;
                }
                lastDrawnPosition = positions[i];

                float age = (i + 1f) / TrailCount;
                float opacity = MathHelper.Lerp(0.12f, 0.025f, age);
                Color color = Color.Lerp(drawColor, madnessYellow, 0.8f) * opacity;
                SpriteEffects effects = spriteDirections[i] < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 drawPosition = positions[i] - screenPos + new Vector2(0f, npc.gfxOffY);
                Vector2 origin = frames[i].Size() / 2f;

                spriteBatch.Draw(texture, drawPosition, frames[i], color, rotations[i], origin,
                    scales[i], effects, 0f);
            }

            return true;
        }
    }
}
