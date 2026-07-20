using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class ChickenGlowingMushroomSkewer : ModItem
    {
        public static int Healing = 150;
        public static int BaseSickness = 30;
        public static int ExquisitelyStuffedDuration = 1200;
        public static int PhilosophersStoneEfficiency = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Healing, ExquisitelyStuffedDuration, BaseSickness);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item2;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17;
            Item.height = 54;
            Item.width = 54;
            Item.maxStack = Item.CommonMaxStack;
            Item.scale = .6f;
            Item.value = 750;
            Item.buffType = BuffID.WellFed3;
            Item.buffTime = ExquisitelyStuffedDuration * 60;
            Item.rare = ItemRarityID.Lime;
        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness))
            {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // Tier-aware heal: Classic full, Unkindled half, BotC zero.
            int heal = player.GetModPlayer<tsorcRevampPlayer>().ApplyHealing(Healing);
            if (heal > 0)
            {
                player.statLife += heal;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(heal, true);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? BaseSickness * 60 / 4 * 3 / PhilosophersStoneEfficiency : BaseSickness * 60);
            }
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int Sickness = BaseSickness / 4 * 3 / PhilosophersStoneEfficiency;
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Formatting", Language.GetTextValue("Mods.tsorcRevamp.Items.MushroomSkewer.Sickness").FormatWith(Sickness)));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GlowingMushroomSkewer>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DeadChicken>(), 1);
            recipe.AddIngredient(ItemID.ShroomiteBar, 1);
            recipe.AddTile(TileID.Campfire);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<GlowingMushroomSkewer>(), 1);
            recipe2.AddIngredient(ModContent.ItemType<CookedChicken>(), 1);
            recipe2.AddIngredient(ItemID.ShroomiteBar, 1);
            recipe2.AddTile(TileID.Campfire);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ModContent.ItemType<ChickenMushroomSkewer>(), 1);
            recipe3.AddIngredient(ItemID.GlowingMushroom, 2);
            recipe3.AddIngredient(ItemID.ShroomiteBar, 1);
            recipe3.AddTile(TileID.Campfire);

            recipe3.Register();
        }
    }
}
