using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class Celestriad : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("All spells cost 1 mana to cast");
        }

        public override void SetDefaults() {
            item.width = 22;
            item.height = 26;
            item.accessory = true;
            item.value = 20000000;
            item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            //recipe.AddIngredient(mod.GetItem("GoldenHairpin"), 1337);
            //recipe.AddIngredient(mod.GetItem("GemBox"), 1337);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 60);
            recipe.AddIngredient(mod.GetItem("SoulOfBlight"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 400000);
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
