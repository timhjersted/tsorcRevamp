using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class ZirconRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Grants Weapon Imbue: Fire buff when worn, which provides" +
								"\n10% increased melee damage and all swords inflict fire damage." + 
								"\nPlus Thorns Effect and +6 Defense.");
        }
 
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }
		
		 public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(Mod.GetItem("EphemeralDust"), 30);
			recipe.AddIngredient(Mod.GetItem("DarkSoul"), 9000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			player.AddBuff(BuffID.WeaponImbueFire, 60, false);    
			player.statDefense += 6;
			player.thorns = 1f;
        }
 
    }
}

