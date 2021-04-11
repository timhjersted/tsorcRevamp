using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    class CloudBat : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloud Bat");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GiantBat];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.CaveBat);
            animationType = NPCID.GiantBat;
            aiType = NPCID.CaveBat;
            npc.lifeMax = 70;
            npc.damage = 80;
            npc.scale = 1f;
            npc.knockBackResist = .55f;
            npc.value = 350;
            npc.defense = 5;
            npc.buffImmune[BuffID.Confused] = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.CloudBatBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (Main.hardMode)
            {
                chance = SpawnCondition.Sky.Chance * 0.2f; 
            }
            return chance;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(8) == 0)
            {
                target.AddBuff(BuffID.Confused, 600, true);
            }
        }
        public override void NPCLoot()
        {
            //Gore.NewGore(npc.position, npc.velocity, "Cloud Bat Gore", 1f); Not added gore, never worked with it before -C
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 15; i++)
            {
                int dustType = 16;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 16, Main.rand.Next(-2, 2), Main.rand.Next(-2, 2), 70, default(Color), .8f);
                }
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Cloud Bat Gore"), 1f);
            }
        }
    }
}