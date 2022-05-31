using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Legs)]
    public class DragoonGreaves2 : ModItem {
        public override string Texture => "tsorcRevamp/Items/Armors/DragoonGreaves";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Dragoon Greaves II");
            Tooltip.SetDefault("Harmonized with Earth and Water\nWhen returning from death, you respawn with full health.\nJump boost and double jump skills.");
        }

        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.value = 500000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player) {
            player.doubleJumpCloud = true;
            player.jumpBoost = true;
            player.spawnMax = true;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("DragoonGreaves"), 1);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 10);
            recipe.AddIngredient(Mod.GetItem("FlameOfTheAbyss"), 10);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

