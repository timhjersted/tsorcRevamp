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
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 1000000;
            item.rare = ItemRarityID.LightRed;
        }
		
		 public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 12);
            recipe.AddIngredient(mod.GetItem("EphemeralDust"), 30);
			recipe.AddIngredient(mod.GetItem("DarkSoul"), 9000);
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

