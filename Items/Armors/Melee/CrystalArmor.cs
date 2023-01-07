using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class CrystalArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Crystal armor vibrates with a mysterious energy" + 
                "\nIncreases melee critical strike chance by 26%");
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += 26f;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 14;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilChainmail);
            recipe2.AddIngredient(ItemID.OrichalcumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
