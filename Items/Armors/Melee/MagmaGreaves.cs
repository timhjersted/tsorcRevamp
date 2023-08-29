using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Legs)]
public class MagmaGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases movement and melee speed by 18%");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 10;
        Item.rare = ItemRarityID.LightRed;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.18f;
        player.GetAttackSpeed(DamageClass.Melee) += 0.18f;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MoltenGreaves, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}

