using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class SmoughHelmet : ModItem 
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Smough's Helmet");
            Tooltip.SetDefault("Increases damage dealt by 15%");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 4;
            Item.vanity = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.15f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
