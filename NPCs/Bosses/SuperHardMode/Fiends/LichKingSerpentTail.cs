using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends
{
    class LichKingSerpentTail : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lich King Serpent");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            AnimationType = 10;
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 21;
            NPC.height = 14;
            NPC.aiStyle = 6;
            NPC.timeLeft = 750;
            NPC.damage = 90;
            NPC.defense = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0;
            NPC.lifeMax = 60000000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 500;

            bodyTypes = new int[43];
            int bodyID = ModContent.NPCType<LichKingSerpentBody>();
            for (int i = 0; i < 43; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;


        public override bool CheckActive()
        {
            return false;
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<LichKingSerpentHead>(), bodyTypes, ModContent.NPCType<LichKingSerpentTail>(), 45, .8f, 22, 0.25f, false, false, false, true, true); //30f was 10f
        }
    }
}