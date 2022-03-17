using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GreyWolfRing : ModItem {
        public override string Texture => "tsorcRevamp/Items/Accessories/WolfRing";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("One of the rings worn by Artorias." +
                                "\nImmunity to the on-fire, bleeding, poisoned, and broken-armor debuffs." + 
								"\n+23 defense within the Abyss, +16 defense otherwise." + 
								"\nSwords inflict fire damage." +
								"\n+8 HP Regen. +100 Mana.");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.defense = 16;
			item.lifeRegen = 8;
            item.accessory = true;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("WolfRing"), 1);
			recipe.AddIngredient(mod.GetItem("BandOfSupremeCosmicPower"), 1);
			recipe.AddIngredient(mod.GetItem("PoisonbloodRing"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.magmaStone = true;
			player.statManaMax2 += 100;
			player.buffImmune[BuffID.OnFire] = true;
			player.buffImmune[BuffID.BrokenArmor] = true;
			player.buffImmune[BuffID.Bleeding] = true;
			player.buffImmune[BuffID.Poisoned] = true;
            

            if (Main.bloodMoon) { // Apparently this is the flag used in the Abyss?
				player.statDefense += 7;
			}
        }

    }
} 