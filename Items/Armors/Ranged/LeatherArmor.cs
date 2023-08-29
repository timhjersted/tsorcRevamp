using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged;

[LegacyName("OldStuddedLeatherArmor")]
[LegacyName("OldLeatherArmor")]
[AutoloadEquip(EquipType.Body)]
public class LeatherArmor : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases ranged damage by 2 flat" +
            "\nSet bonus: 20% less chance to consume ammo");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 4;
        Item.rare = ItemRarityID.Green;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Ranged).Flat += 2;
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<LeatherHelmet>() && legs.type == ModContent.ItemType<LeatherGreaves>();
    }
    public override void UpdateArmorSet(Player player)
    {
        player.ammoCost80 = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Leather, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
