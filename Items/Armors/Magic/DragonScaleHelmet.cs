using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic;

[LegacyName("AncientDragonScaleHelmet")]
[AutoloadEquip(EquipType.Head)]
public class DragonScaleHelmet : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("It is made of razor sharp dragon scales.\nThorns Effect");
    }
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 20;
        Item.defense = 3;
        Item.rare = ItemRarityID.LightPurple;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.thorns += 1f;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MythrilHood, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3500);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
