using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic;

[AutoloadEquip(EquipType.Legs)]
public class AnkorWatLeggings : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases magic damage by 15% and movement speed by 10%");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 10;
        Item.value = 100000;
        Item.rare = ItemRarityID.Yellow;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.1f;
        player.GetDamage(DamageClass.Magic) += 0.15f;

        if(player.HasBuff(BuffID.ShadowDodge))
        {
            player.moveSpeed += 0.1f;
            player.GetDamage(DamageClass.Magic) += 0.15f;
        }
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedGreaves, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}

