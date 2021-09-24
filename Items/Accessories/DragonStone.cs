using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragonStone : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Provides immunity to most flying creatures and darkness." +
                                "\nPluis immunity to knockback and fire blocks.");
        }

        public override void SetDefaults() {
            item.width = 26;
            item.height = 26;
            item.accessory = true;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofFlight, 70);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.buffImmune[BuffID.Darkness] = true;
            player.immune = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragonStone = true;
        }


    }
}
