using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GrandWizardsHat : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Grand Wizard's Hat");
            Tooltip.SetDefault("25% increased magic damage, +100 mana");
        }

        public override void SetDefaults() {
            Item.width = 34;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SorcererEmblem, 1);
            recipe.AddIngredient(ItemID.WizardHat, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 5000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }

        public override void UpdateEquip(Player player) {
            player.GetDamage(DamageClass.Magic) += 0.25f;
            player.statManaMax2 += 100;
        }

    }
}
