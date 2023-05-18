using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class ZombieWormBody : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override void SetDefaults()
        {
            AnimationType = 10;
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 38;
            NPC.height = 24;
            NPC.aiStyle = 6;
            NPC.timeLeft = 750;
            NPC.damage = 40;
            NPC.defense = 24;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0;
            NPC.lifeMax = 91000000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 460;

            bodyTypes = new int[13];
            int bodyID = ModContent.NPCType<ZombieWormBody>();
            for (int i = 0; i < 13; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<ZombieWormHead>(), bodyTypes, ModContent.NPCType<ZombieWormTail>(), 15, .4f, 8, 0.07f, false, false, false, true, true);
        }
    }
}