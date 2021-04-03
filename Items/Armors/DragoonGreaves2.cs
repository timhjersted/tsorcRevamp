using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Legs)]
    public class DragoonGreaves2 : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Harmonized with Earth and Water\nWhen returning from death, you respawn with full health.\nJump boost and double jump skills.");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.defense = 15;
            item.value = 500000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player) {
            player.doubleJumpCloud = true;
            player.jumpBoost = true;
            player.spawnMax = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DragoonGreaves"), 1);
            recipe.AddIngredient(mod.GetItem("DragonScale"), 10);
            recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

