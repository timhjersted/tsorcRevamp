using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Legs)]
public class CrystalGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Dazzling armor cut from crystal." +
            "\nIncreases movement speed by 21%");
    }

    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.21f;
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 10;
        Item.rare = ItemRarityID.Pink;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MythrilGreaves);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        recipe2.AddIngredient(ItemID.MythrilGreaves);
        recipe2.AddIngredient(ItemID.OrichalcumLeggings);
        recipe2.AddTile(TileID.DemonAltar);

        recipe2.Register();
    }
}

