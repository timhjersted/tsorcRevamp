using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class RedCloudHunter : ModNPC
    {

        public int archerBoltDamage = 25; //was 85, whoa, how did no one complain about this?

        public override void SetDefaults()
        {
            AIType = NPCID.SkeletonArcher;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 52;
            NPC.lifeMax = 1150;
            NPC.scale = 0.9f;
            NPC.defense = 30;
            NPC.value = 6500;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 48;
            NPC.knockBackResist = 0.6f;
            NPC.rarity = 3;
            Banner = NPC.type;
            NPC.buffImmune[BuffID.Confused] = true;
            BannerItem = ModContent.ItemType<Banners.RedCloudHunterBanner>();

            AnimationType = NPCID.SkeletonArcher;
            Main.npcFrameCount[NPC.type] = 20;

            if (Main.hardMode)
            {
                NPC.defense = 24;
                NPC.value = 3500;
                NPC.damage = 40;
                archerBoltDamage = 65;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1750;
                NPC.defense = 70;
                NPC.value = 3900;
                NPC.damage = 70;
                archerBoltDamage = 85;
            }

        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            NPC.defense = (int)(NPC.defense * (2 / 3));
            archerBoltDamage = (int)(archerBoltDamage / 2);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 6));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.HolyArrow, 1, 30, 60));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.UnicornHorn, 1, 1, 3));
        }

        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;


            if (!Main.hardMode && spawnInfo.Player.ZoneDungeon) return 0.01f;

            if (Main.hardMode && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && !spawnInfo.Player.ZoneBeach && spawnInfo.Player.ZoneJungle) return 0.02f;
            if (Main.hardMode && spawnInfo.Player.ZoneHallow && !spawnInfo.Player.ZoneDungeon) return 0.01f;
            if (Main.hardMode && spawnInfo.Player.ZoneOverworldHeight && (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneJungle)) return 0.0125f;

            if (Main.hardMode && spawnInfo.Lihzahrd) return 0.15f;

            if (tsorcRevampWorld.SuperHardMode && (spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && (spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.1f;
            if (tsorcRevampWorld.SuperHardMode && (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert)) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon) return 0.01f; //.08% is 4.28%
            return chance;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.ArcherAI(NPC, ProjectileID.FlamingArrow, 22, 13, 100, 2, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 2.6f);
        }

        #region Gore
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
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

                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Cloud Hunter Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}