using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class CookedChicken : ModItem
    {
        public static int Healing = 100;
        public static int BaseSickness = 30;
        public static int PhilosophersStoneEfficiency = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BaseSickness);
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
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = 2;
            Item.width = 20;
            Item.healLife = Healing;
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
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.AddBuff(BuffID.PotionSickness, player.pStone ? BaseSickness * 60 / 4 * 3 / PhilosophersStoneEfficiency : BaseSickness * 60);

                return true;
            }
            return false;
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
            recipe.AddIngredient(ModContent.ItemType<DeadChicken>(), 1);
            recipe.AddTile(TileID.CookingPots);

            recipe.Register();
        }
    }
}
