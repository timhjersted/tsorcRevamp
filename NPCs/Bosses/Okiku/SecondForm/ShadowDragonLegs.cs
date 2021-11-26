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
            npc.damage = 80;
            npc.defense = 20;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.lifeMax = 91000000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.knockBackResist = 0f;
            npc.dontCountMe = true;
            drawOffsetY = 50;
            bodyTypes = new int[] {
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonLegs>(),
            ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody>(),
            ModContent.NPCType<ShadowDragonLegs>(), ModContent.NPCType<ShadowDragonBody>(), ModContent.NPCType<ShadowDragonBody2>(), ModContent.NPCType<ShadowDragonBody3>()
            };
        }
        public static int[] bodyTypes;


        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Shadow Dragon");
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
        }

        public override void AI() {
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<ShadowDragonHead>(), bodyTypes, ModContent.NPCType<ShadowDragonTail>(), 25, 0.8f, 16f, 0.33f, true, false, true, false, false);

            if (Main.rand.Next(3) == 0) {
                int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
