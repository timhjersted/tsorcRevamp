using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragoonGear : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Gear worn by Dragoons.\n" +
                                "200% melee damage if falling.\n" +
                                "No damage from falling.\n" +
                                "Faster Jump, which also results in a higher jump.\n" +
                                "Press the Dragoon Boots key to toggle high jump (default Z)");

        }

        public override void SetDefaults() {

            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Purple;
            item.value = PriceByRarity.Purple_11;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(mod.GetItem("DragoonBoots"), 1);
            recipe.AddIngredient(mod.GetItem("DragonHorn"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.noFallDmg = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonHorn = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }

    }
}
