using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Legs)]
public class HollowSoldierWaistcloth : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases dodgeroll agility, especially on solid ground");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 2;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetModPlayer<tsorcRevampPlayer>().HollowSoldierAgility = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.IronGreaves);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
        recipe.AddTile(TileID.DemonAltar);
        
        recipe.Register();
    }
}

