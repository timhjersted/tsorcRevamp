using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class ManOfWar : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 26;
            NPC.height = 26;
            Main.npcFrameCount[NPC.type] = 7;
            AnimationType = NPCID.GreenJellyfish;
            NPC.aiStyle = 18;
            NPC.timeLeft = 750;
            NPC.damage = 120;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 2000;
            NPC.alpha = 20;
            NPC.scale = .7f;
            NPC.knockBackResist = 0.3f;
            NPC.noGravity = true;
            NPC.value = 1250;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.Frozen] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManOfWarBanner>();
            if (Main.hardMode) { NPC.lifeMax = 500; NPC.defense = 30; NPC.value = 550; }
        }


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            float chance = 0;

            if (Main.hardMode && spawnInfo.Water)
            {
                chance = 0.5f;
            }
            if (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3)
            {
                chance *= 4;
            }

            return chance;
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                target.AddBuff(BuffID.PotionSickness, 3600); //evil! pure evil!
            }
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.3f, 0.3f, 200, default, 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 71, 0.2f, 0.2f, 200, default, 2f);
            }
        }
    }
}
