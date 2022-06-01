using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.HandsOn)]

    public class BandOfSupremeCosmicPower : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Band of Supreme Cosmic Power");
            Tooltip.SetDefault("+4 life regen and increases max mana by 80");
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.lifeRegen = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("BandOfGreatCosmicPower").Type, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 15000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }

        public override void UpdateEquip(Player player) {
            player.statManaMax2 += 80;
        }

    }
}
