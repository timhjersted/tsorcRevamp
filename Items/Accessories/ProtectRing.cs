using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class ProtectRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Ring that guards against death." +
								"\nPuts \"protect\" on wearer (+30 defense).");
        }
 
        public override void SetDefaults() {
            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.value = 75000;
            item.rare = ItemRarityID.Orange;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
			recipe.AddIngredient(ItemID.Emerald, 1);
			recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			player.AddBuff(ModContent.BuffType<Buffs.Protect>(), 60, false);
        }
 
    }
}

