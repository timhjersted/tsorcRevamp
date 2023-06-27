using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class MushroomSkewer : ModItem
    {
        public static int Healing = 55;
        public static int BaseSickness = 30;
        public static int ExquisitelyStuffedDuration = 120;
        public static int PhilosophersStoneEfficiency = 2;
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
            Item.height = 44;
            Item.width = 44;
            Item.maxStack = 9999;
            Item.scale = .6f;
            Item.value = 100;
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
            player.AddBuff(BuffID.WellFed3, ExquisitelyStuffedDuration * 60);
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int Sickness = BaseSickness;
            if (Main.LocalPlayer.pStone)
            {
                Sickness = BaseSickness / 4 * 3 / PhilosophersStoneEfficiency;
            }
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Formatting", Language.GetTextValue("Mods.tsorcRevamp.Items.MushroomSkewer.Sickness").FormatWith(Sickness)));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(ItemID.Mushroom, 1);
            recipe.AddTile(TileID.Campfire);

            recipe.Register();
        }
    }
}
