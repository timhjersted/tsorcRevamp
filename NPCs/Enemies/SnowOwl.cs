using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    class SnowOwl : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Snow Owl");
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GiantBat];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.CaveBat);
            AnimationType = NPCID.GiantBat;
            aiType = NPCID.CaveBat;
            NPC.lifeMax = 15;
            NPC.damage = 20;
            NPC.scale = 1.1f;
            NPC.knockBackResist = .4f;
            NPC.value = 100;
            NPC.defense = 10;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.width = 24;
            NPC.height = 34;

            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.SnowOwlBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (Main.snowTiles > 5)
            {
                chance = 0.2f;
            }
            
            //Otherwise it spawns in the frozen ocean and gets stuck in the ceiling
            if (spawnInfo.Water)
            {
                chance = 0;
            }
            return chance;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Snow Owl Gore 1"), 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Snow Owl Gore 2"), 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Snow Owl Gore 3"), 1.1f);

            }
        }
    }
}