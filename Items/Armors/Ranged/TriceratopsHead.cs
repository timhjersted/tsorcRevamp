using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged;

[AutoloadEquip(EquipType.Head)]
public class TriceratopsHead : ModItem 
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases ranged critical strike chance by 12%");
    }
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 20;
        Item.defense = 5;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.fromItem(Item);
    }

    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Ranged) += 12;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.FossilHelm, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        recipe2.AddIngredient(ItemID.GladiatorHelmet, 1);
        recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 3500);
        recipe2.AddTile(TileID.DemonAltar);

        recipe2.Register();
    }
}
