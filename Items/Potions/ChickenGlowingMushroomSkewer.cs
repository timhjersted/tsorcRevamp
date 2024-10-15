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
        public static int ExquisitelyStuffedDuration = 900;
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
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.statLife += Healing;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(Healing, true);
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
            recipe.AddIngredient(ItemID.RichMahogany, 1);
            recipe.AddIngredient(ItemID.GlowingMushroom, 1);
            recipe.AddIngredient(ModContent.ItemType<DeadChicken>(), 1);
            recipe.AddIngredient(ItemID.PixieDust, 1);
            recipe.AddTile(TileID.Campfire);

            recipe.Register();
        }
    }
}
