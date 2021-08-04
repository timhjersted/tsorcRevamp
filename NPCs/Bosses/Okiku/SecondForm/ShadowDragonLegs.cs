using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm {
    public class ShadowDragonLegs : ModNPC {
        public override void SetDefaults() {
            npc.width = 32;
            npc.height = 32;
            npc.aiStyle = 6;
            npc.damage = 70;
            npc.defense = 20;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.lifeMax = 4000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            Main.npcFrameCount[npc.type] = 1;
            npc.netAlways = true;
            npc.dontCountMe = true;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Shadow Dragon");
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
            npc.lifeMax = (int)(npc.lifeMax * 0.7f * bossLifeScale);
        }

        public override void AI() {
            if (!Main.npc[(int)npc.ai[1]].active) {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
            }
            if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X) {
                npc.spriteDirection = 1;
            }
            if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X) {
                npc.spriteDirection = -1;
            }
            if (Main.rand.Next(3) == 0) {
                int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {
            Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
            Color alpha = Color.White;
            SpriteEffects effects = SpriteEffects.None;
            if (npc.spriteDirection == 1) {
                effects = SpriteEffects.FlipHorizontally;
            }
            spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + npc.width / 2 - Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + npc.height - Main.npcTexture[npc.type].Height * npc.scale / Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
            npc.alpha = 255;
            return true;
        }
    }
}
