using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories;
using tsorcRevamp.Content.Items.Accessories.Other.SporePowder;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    public class DworcAlchemist : DworcCasterPuppet
    {
        protected override CasterKind Kind => CasterKind.Alchemist;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.npcSlots = 5;
            NPC.lifeMax = 250;
            NPC.defense = 7;
            NPC.value = 1250;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodoomasterBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 14;
                NPC.value = 2500;
                NPC.knockBackResist = 0.1f;
            }

            // Poise/knockback are resolved last by GlobalNPC.PopulatePoiseProfiles.
        }
        //excuse me while i drop Every Potion Known To Mankind holy hell
        //these dudes oughtta be called alchemists or something
        //actually you know what? i have the ability to change that
        //goodbye dworc voodoomaster, hello dworc alchemist
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            Player player = Main.player[NPC.target];

            npcLoot.Add(ItemDropRule.Common(ItemID.SpellTome, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 22));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.HealingPotion, 35, 3, 5));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 33, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 22));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 32));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofVenom, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 25));
            //npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.BossItems.CursedSkull>(), 50));
            //npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Defensive.BandOfCosmicPower>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 5, 5, 9));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SporePowderItem>(), 100));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>(), 5, 1));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 14));
        }


        //Spawns in the Jungle Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

            float chance = 0f;

            if (spawnInfo.Water) return 0f;

            // Jungle enemies stay out of the snow biome, even where it borders the jungle.
            if (spawnInfo.Player.ZoneSnow)
            {
                return 0f;
            }

            if ((spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneJungle) && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight) return 0.004f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.008f;
                if (spawnInfo.Player.ZoneRockLayerHeight) return 0.065f;
            }
            if (Main.hardMode && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneBeach && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) return 0.005f;

            return chance;
        }

        #endregion

        #region Gore
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}
