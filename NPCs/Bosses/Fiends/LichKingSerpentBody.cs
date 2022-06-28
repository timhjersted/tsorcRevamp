using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    class LichKingSerpentBody : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            AnimationType = 10;
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 21;
            NPC.height = 14;
            NPC.aiStyle = 6;
            NPC.timeLeft = 750;
            NPC.damage = 190;
            NPC.defense = 28;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0;
            NPC.lifeMax = 60000000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 460;

            bodyTypes = new int[43];
            int bodyID = ModContent.NPCType<LichKingSerpentBody>();
            for (int i = 0; i < 43; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lich King Serpent");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / tsorcRevampGlobalNPC.expertScale);
            NPC.defense = NPC.defense += 12;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }


        public override void AI()
        {

            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<LichKingSerpentHead>(), bodyTypes, ModContent.NPCType<LichKingSerpentTail>(), 45, .8f, 22, 0.25f, false, false, false, true, true);
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Lich King Serpent Body Gore").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Lich King Serpent Body Gore").Type, 1f);
                }
            }
        }
    }
}