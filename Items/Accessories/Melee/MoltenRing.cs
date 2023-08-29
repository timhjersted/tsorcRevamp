using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee;

[AutoloadEquip(EquipType.HandsOn)]

public class MoltenRing : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("\n+10% Melee Damage and Magma Stone effect");
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 22;
        Item.accessory = true;
        Item.value = PriceByRarity.Orange_3;
        Item.rare = ItemRarityID.Orange;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MagmaStone);
        recipe.AddIngredient(ItemID.HellstoneBar);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Melee) += 0.1f;
        player.magmaStone = true;
    }

}