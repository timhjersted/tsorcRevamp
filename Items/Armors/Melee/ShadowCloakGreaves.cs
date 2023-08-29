using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Legs)]
public class ShadowCloakGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("18% increased movement speed" +
            "\nIncreases melee speed by 27%");
    }
    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.defense = 8;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }

    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.17f;
        player.GetAttackSpeed(DamageClass.Melee) += 0.27f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShadowGreaves, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
        recipe.AddTile(TileID.DemonAltar);
        
        recipe.Register();
    }
}

