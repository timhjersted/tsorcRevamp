using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class Celestriad : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("All spells are free to cast");
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("GoldenHairpin"), 1);
            recipe.AddIngredient(Mod.GetItem("GemBox"), 1);
            recipe.AddIngredient(Mod.GetItem("CursedSoul"), 30);
            recipe.AddIngredient(Mod.GetItem("SoulOfBlight"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 400000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            if (player.inventory[player.selectedItem].magic) {
                player.manaCost = 1f / player.inventory[player.selectedItem].mana;
            }
            
        }
    }
}
