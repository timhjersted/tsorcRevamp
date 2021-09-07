using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    class TestBoss : ModNPC
    {
        public override void SetDefaults()
        {

            Main.npcFrameCount[npc.type] = 6;
            npc.npcSlots = 10;
            npc.aiStyle = 0;
            npc.width = 80;
            npc.height = 100;
            npc.damage = 1;
            npc.defense = 10;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = Int32.MaxValue;
            npc.friendly = false;
            npc.boss = true;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.knockBackResist = 0;
            npc.value = 150000;

            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
        }


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = Int32.MaxValue;
        }



        public override void AI()
        {
            npc.life = npc.lifeMax;
        }



        public override void NPCLoot()
        {
           
        }
    }
}