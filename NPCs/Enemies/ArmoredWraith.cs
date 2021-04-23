using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    class ArmoredWraith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Armored Wraith");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.Wraith];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.Wraith);
            animationType = NPCID.Wraith;
            aiType = NPCID.CaveBat;
            npc.lifeMax = 150;
            npc.damage = 95;
            npc.scale = 1f;
            npc.knockBackResist = 0;
            npc.value = 500;
            npc.defense = 25;
            npc.alpha = 100;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.ArmoredWraithBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.player.ZoneMeteor && !Main.dayTime && spawnInfo.player.ZoneOverworldHeight)
            {
                chance = .04f;
            }
            if (spawnInfo.player.ZoneMeteor && !Main.dayTime && spawnInfo.player.ZoneDirtLayerHeight)
            {
                chance = .033f;
            }
            if (spawnInfo.player.ZoneMeteor && !Main.dayTime && spawnInfo.player.ZoneRockLayerHeight)
            {
                chance = .25f;
            }

            return chance;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(5) == 0)
            {
                target.AddBuff(BuffID.BrokenArmor, 10800, true);
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 25; i++)
            {
                int dustType = 109;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = false;
                dust.alpha = 120;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 40; i++) // I'm not convinced this is actually working
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 109, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 120, default(Color), .8f);
                }
            }
        }
    }
}