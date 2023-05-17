using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ManHunter : ModNPC
    {
        public int archerBoltDamage = 20;
        public override void SetDefaults()
        {
            AIType = NPCID.SkeletonArcher;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 25;
            NPC.lifeMax = 250;
            NPC.defense = 10;
            NPC.value = 1000;
            NPC.scale = 0.9f;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 48;
            NPC.knockBackResist = 0.7f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManHunterBanner>();

            AnimationType = NPCID.SkeletonArcher;
            Main.npcFrameCount[NPC.type] = 20;

            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 14;
                NPC.value = 1500;
                NPC.damage = 50;
                archerBoltDamage = 30;
            }


        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            NPC.defense = (int)(NPC.defense * (2 / 3));
            archerBoltDamage = (int)(archerBoltDamage / 2);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 36));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 34));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 34));
            npcLoot.Add(new CommonDrop(ItemID.HealingPotion, 100, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.HolyArrow, 4, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 2, 1, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 8));
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (!Main.hardMode && !spawnInfo.Player.ZoneMeteor && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight) return 0.1f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.03f;
                if (spawnInfo.Player.ZoneRockLayerHeight) return 0.04f;
            }
            if (Main.hardMode && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneBeach && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) return 0.02f;

            return chance;
        }

        public override void AI()
        {
            tsorcRevampAIs.ArcherAI(NPC, ProjectileID.WoodenArrowHostile, 14, 11, 120, 1.3f, 0.08f, canTeleport: false);
        }



        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Man Hunter Gore 3").Type, 1f);
                }
            }
        }
    }
}