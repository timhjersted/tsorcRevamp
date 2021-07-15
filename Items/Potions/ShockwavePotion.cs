using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class ShockwavePotion : ModItem {
        public override void SetDefaults() {
            item.width = 24;
            item.height = 30;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 5000;
            item.buffType = ModContent.BuffType<Buffs.Shockwave>();
            item.buffTime = 12600;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                tooltips.Insert(3, new TooltipLine(mod, "RevampShockwave1", "[c/00ff00:Revamped Mode:] Hold DOWN to increase fall speed."));
                tooltips.Insert(4, new TooltipLine(mod, "RevampShockwave2", "[c/00ff00:Revamped Mode:] Create a damaging shockwave when you land"));
                tooltips.Insert(5, new TooltipLine(mod, "RevampShockwave3", "[c/00ff00:Revamped Mode:] which grows in strength based on distance fallen."));
            }
            else {
                tooltips.Insert(3, new TooltipLine(mod, "", "Enemies take damage when you land."));
            }

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Blinkroot, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(ItemID.Meteorite, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
