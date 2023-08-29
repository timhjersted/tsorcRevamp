using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[LegacyName("AncientBrassGreaves")]
[AutoloadEquip(EquipType.Legs)]
public class BrassGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases movement speed by 18%");
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 11;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.18f;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.PlatinumGreaves, 1);
        recipe.AddIngredient(ItemID.BeeWax, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2600);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}

