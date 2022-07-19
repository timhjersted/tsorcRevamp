using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
    public class ShadowDragonHead : ModNPC
    {
        //private bool initiate;

        public int TimerHeal;

        public float TimerAnim;

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 42;
            NPC.aiStyle = 6;
            NPC.damage = 160;
            NPC.defense = 20;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 12600;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);

            DrawOffsetY = 45;

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

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Dragon");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        NPCDespawnHandler despawnHandler;


        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<ShadowDragonHead>(), bodyTypes, ModContent.NPCType<ShadowDragonTail>(), 25, 0f, 16f, 0.33f, true, false, true, false, false);

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 62, 0.8f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }
        }


        public override bool CheckActive()
        {
            return false;
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 500);
        }
    }
}
