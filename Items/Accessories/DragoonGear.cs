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
            item.rare = ItemRarityID.Pink;
            item.value = 500000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(mod.GetItem("DragoonBoots"), 1);
            recipe.AddIngredient(mod.GetItem("DragonHorn"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.noFallDmg = true;
            if (((player.gravDir == 1f) && (player.velocity.Y > 0)) || ((player.gravDir == -1f) && (player.velocity.Y < 0))) {
                player.meleeDamage *= 2;
            }
            player.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }

    }
}
