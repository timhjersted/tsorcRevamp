using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Head)]
public class MagmaHelmet : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increase melee critical strike chance by 10%");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 9;
        Item.rare = ItemRarityID.LightRed;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Melee) += 10;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MoltenHelmet, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
