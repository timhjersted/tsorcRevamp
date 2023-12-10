using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using static tsorcRevamp.oSpawnHelper;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Dunlending : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PossessedArmor];
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = -1;
            NPC.damage = 20;
            NPC.defDamage = 2;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 45;
            NPC.value = 220;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            AnimationType = NPCID.PossessedArmor;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DunlendingBanner>();
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dunlending Gore 3").Type, 1f);
                }
            }
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, canPounce: false);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var p = spawnInfo.Player;

            float chance = 0;

            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 1f) return 0f;


            if (p.ZoneOverworldHeight)
            {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.125f;
            }
            if (oUnderSurfaceByTile(p) || oUndergroundByTile(p) || oCavernByTile(p))
            {
                if (Main.dayTime && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) chance = 0.067f;
                if (!Main.dayTime && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) chance = 0.1f;
            }
            if (Main.hardMode)
            {
                return chance /= 2;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                return chance /= 4;
            }
            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 10));
            //npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 100));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Axes.DunlendingAxe>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BoostPotion>(), 10));
            npcLoot.Add(new CommonDrop(ItemID.ShinePotion, 25, 1, 1, 3));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 5, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.Torch, 50, 20, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.HealingPotion, 55));
        }
    }
}
