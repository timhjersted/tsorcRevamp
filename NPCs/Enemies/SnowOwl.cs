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
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GiantBat];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.CaveBat);
            animationType = NPCID.GiantBat;
            aiType = NPCID.CaveBat;
            npc.lifeMax = 15;
            npc.damage = 20;
            npc.scale = 1.1f;
            npc.knockBackResist = .4f;
            npc.value = 100;
            npc.defense = 10;
            npc.buffImmune[BuffID.Confused] = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.width = 24;
            npc.height = 34;

            banner = npc.type;
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
            if (spawnInfo.water)
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
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Snow Owl Gore 1"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Snow Owl Gore 2"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Snow Owl Gore 3"), 1.1f);

            }
        }
    }
}