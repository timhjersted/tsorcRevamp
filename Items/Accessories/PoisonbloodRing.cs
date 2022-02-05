using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class PoisonbloodRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("One of the infamous bite rings commissioned by Sir Arstor of Carim." +
								"\nDespite the dreadful rumors surrounding its creation, this ring is an unmistakable asset," +
								"\ndue to its ability to prevent bleeding and becoming poisoned." +
                                "\n+2 HP Regeneration and 6 defense");
        }
 
        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
			item.lifeRegen = 2;
            item.accessory = true;
            item.value = PriceByRarity.Green_2;
            item.rare = ItemRarityID.Green;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("PoisonbiteRing"), 1);
			recipe.AddIngredient(mod.GetItem("BloodbiteRing"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			player.buffImmune[BuffID.Poisoned] = true;
			player.buffImmune[BuffID.Bleeding] = true;
            player.statDefense += 6;
        }
 
    }
}

