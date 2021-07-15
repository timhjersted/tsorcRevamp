using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class CrimsonPotion : ModItem {

        public override void SetDefaults() {
            item.width = 14;
            item.height = 24;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 1000;
            item.buffType = ModContent.BuffType<Buffs.CrimsonDrain>();
            item.buffTime = 18000;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                tooltips.Insert(3, new TooltipLine(mod, "RevampCrimsonDrain1", "[c/00ff00:Revamped Mode:] Enemies within a ten tile radius are inflicted with"));
                tooltips.Insert(4, new TooltipLine(mod, "RevampCrimsonDrain2", "[c/00ff00:Revamped Mode:] Crimson Burn, which drains 8 life per second, 16 in hard mode."));
            }
            else {
                tooltips.Insert(3, new TooltipLine(mod, "", "Enemies within a ten tile radius take damage"));
            }

        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ThornsPotion, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
