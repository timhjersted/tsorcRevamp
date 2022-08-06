using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class AquamarineRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("5% increased magic damage" +
                                "\n+40 mana");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.05f;
            player.statManaMax2 += 40;
        }

    }
}
