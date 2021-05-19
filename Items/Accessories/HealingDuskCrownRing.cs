using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class HealingDuskCrownRing : ModItem {
        public override string Texture => "tsorcRevamp/Items/Accessories/DuskCrownRing";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("This magic crown-shaped ring was granted to Princess Dusk of Oolacile upon her birth." +
                                "\nThe ringstone doubles magic damage, reduces mana use by 50% and boosts magic crit by 50%" +
                                "\nbut at the cost of one-half Max HP. Your previous max HP is restored" +
                                "\nwhen the ring is removed. Healing enchantment provides +9 Life Regen and gifts the reborn with full health.");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
			item.lifeRegen = 9;
            item.accessory = true;
            item.value = 5250000;
            item.rare = ItemRarityID.Pink;
        }
		
		public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DuskCrownRing"), 1);
			recipe.AddIngredient(mod.GetItem("Humanity"), 5);
			recipe.AddIngredient(mod.GetItem("BlueTitanite"), 7);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 28000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
		
        public override void UpdateEquip(Player player) {
            player.statLifeMax2 /= 2;
			player.manaCost -= 0.5f;
			player.magicDamage *= 2;
			player.magicCrit += 50;
			player.spawnMax = true;
			player.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing = true;
			
        }
		
		public override bool CanEquipAccessory(Player player, int slot)	{
			return !(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DuskCrownRing);
		}
    }
}