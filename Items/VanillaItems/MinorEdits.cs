using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class MinorEdits : GlobalItem
    {
        public static float BotCWellFedStaminaRegen = 5f;
        public static float BotCPlentySatisfiedStaminaRegen = 10f;
        public static float BotCExquisitelyStuffedStaminaRegen = 15f;
        public const float WormScarfResistBonus = 3f;
        public const float TurtleSetResistBonus = 2f;
        public override void SetDefaults(Item item)
        {
            /*if ((item.type == ItemID.StaffofRegrowth || item.type == ItemID.AcornAxe) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                item.createTile = -1; //block placing grass, thus allowing use
            }*/
            if (item.type == ItemID.DivingHelmet)
            {
                item.accessory = true;
            }
            if (item.type == ItemID.MoltenPickaxe)
            {
                item.useTime = 15;
                item.useAnimation = 15;
            }
            if (item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard || item.type == ItemID.DungeonFishingCrate || item.type == ItemID.DungeonFishingCrateHard)
            {
                ItemID.Sets.OpenableBag[item.type] = false;
            }
            if ((item.type == ItemID.ExtendoGrip || item.type == ItemID.ArchitectGizmoPack || item.type == ItemID.HandOfCreation || item.type == ItemID.Toolbelt || item.type == ItemID.Toolbox) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                item.accessory = false;
            }
        }
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (item.type == ItemID.ManaCloakStar)
            {
                if (player.manaMagnet)
                {
                    grabRange += 100;
                }
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if ((item.type == ItemID.DirtRod || item.type == ItemID.BoneWand || item.type == ItemID.BuilderPotion) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return true;
        }
        public override void UpdateEquip(Item item, Player player)
        {
            base.UpdateEquip(item, player);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.MechanicalEye)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "RealBossName", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalEye.Tooltip")));
                }
            }
            if (item.type == ItemID.WormScarf)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceScarf", LangUtils.GetTextValue("CommonItemTooltip.DRStat", 17 + WormScarfResistBonus)));
                }
            }
            if (item.type == ItemID.WarmthPotion)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ColdResistance", LangUtils.GetTextValue("Buffs.VanillaBuffs.Warmth", 30)));
                }
            }
            if (item.type == ItemID.EndurancePotion)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistancePot", LangUtils.GetTextValue("CommonItemTooltip.DRStat", 10 + tsorcGlobalBuff.EnduranceResistBonus)));
                }
            }
            if (item.type == ItemID.FrozenTurtleShell)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceIceBarrier", LangUtils.GetTextValue("Items.VanillaItems.FrozenTurtleShell", 50, 25 + tsorcGlobalBuff.IceBarrierResistBonus)));
                }
            }
            if (item.type == ItemID.FrozenShield)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceIceBarrier", LangUtils.GetTextValue("Items.VanillaItems.FrozenTurtleShell", 50, 25 + tsorcGlobalBuff.IceBarrierResistBonus)));
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe1 = Recipe.Create(ItemID.FragmentSolar, 40)
            .AddIngredient(ItemID.SolarSolenianBanner, 1)
            .AddIngredient(ItemID.SolarSrollerBanner, 1)
            .AddIngredient(ItemID.SolarCoriteBanner, 1)
            .AddIngredient(ItemID.SolarCrawltipedeBanner, 1)
            .AddIngredient(ItemID.SolarDrakomireBanner, 1)
            .AddTile(TileID.LunarCraftingStation);
            recipe1.Register();

            Recipe recipe2 = Recipe.Create(ItemID.FragmentVortex, 40)
            .AddIngredient(ItemID.VortexHornetBanner, 1)
            .AddIngredient(ItemID.VortexHornetQueenBanner, 1)
            .AddIngredient(ItemID.VortexRiflemanBanner, 1)
            .AddIngredient(ItemID.VortexSoldierBanner, 1)
            .AddTile(TileID.LunarCraftingStation);
            recipe2.Register();

            Recipe recipe3 = Recipe.Create(ItemID.FragmentNebula, 40)
            .AddIngredient(ItemID.NebulaBrainBanner, 1)
            .AddIngredient(ItemID.NebulaBeastBanner, 1)
            .AddIngredient(ItemID.NebulaSoldierBanner, 1)
            .AddIngredient(ItemID.NebulaHeadcrabBanner, 1)
            .AddTile(TileID.LunarCraftingStation);
            recipe3.Register();

            Recipe recipe4 = Recipe.Create(ItemID.FragmentStardust, 40)
            .AddIngredient(ItemID.StardustLargeCellBanner, 1)
            .AddIngredient(ItemID.StardustJellyfishBanner, 1)
            .AddIngredient(ItemID.StardustSoldierBanner, 1)
            .AddIngredient(ItemID.StardustSpiderBanner, 1)
            .AddIngredient(ItemID.StardustWormBanner, 1)
            .AddTile(TileID.LunarCraftingStation);
            recipe4.Register();

            Recipe VitalCrystal = Recipe.Create(ItemID.AegisCrystal)
                .AddCondition(tsorcRevampWorld.AdventureModeEnabled)
                .AddIngredient(ItemID.LifeCrystal, 3)
                .AddIngredient(ModContent.ItemType<DarkSoul>(), 6000)
                .AddTile(TileID.DemonAltar);
            VitalCrystal.Register();

            Recipe AegisFruit = Recipe.Create(ItemID.AegisFruit)
                .AddCondition(tsorcRevampWorld.AdventureModeEnabled)
                .AddIngredient(ItemID.LifeFruit, 3)
                .AddIngredient(ModContent.ItemType<DarkSoul>(), 5000)
                .AddTile(TileID.DemonAltar);
            AegisFruit.Register();

            Recipe ArcaneCrystal = Recipe.Create(ItemID.ArcaneCrystal)
                .AddCondition(tsorcRevampWorld.AdventureModeEnabled)
                .AddIngredient(ItemID.ManaCrystal, 3)
                .AddIngredient(ModContent.ItemType<DarkSoul>(), 2000)
                .AddTile(TileID.DemonAltar);
            ArcaneCrystal.Register();

            Recipe ShimmerArrow = Recipe.Create(ItemID.ShimmerArrow, 5)
                .AddCondition(tsorcRevampWorld.AdventureModeEnabled)
                .AddIngredient(ItemID.WoodenArrow, 5)
                .AddIngredient(ModContent.ItemType<DarkSoul>(), 1)
                .AddTile(TileID.DemonAltar);
            ShimmerArrow.Register();

        }
    }
}
