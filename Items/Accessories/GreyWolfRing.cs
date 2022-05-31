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
            Item.width = 24;
            Item.height = 24;
            Item.defense = 16;
			Item.lifeRegen = 8;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("WolfRing"), 1);
			recipe.AddIngredient(Mod.GetItem("BandOfSupremeCosmicPower"), 1);
			recipe.AddIngredient(Mod.GetItem("PoisonbloodRing"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 100000);
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