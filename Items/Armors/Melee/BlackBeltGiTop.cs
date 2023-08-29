using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Body)]
public class BlackBeltGiTop : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("+20% melee damage" +
            "\nSet Bonus: +20% Melee Speed" +
            "\nZen meditation adds amazing +13 life regen" +
            "\nDefense is capped at 30" +
            "\nDamage reduction is converted into movement speed");
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 2;
        Item.rare = ItemRarityID.Pink;
        Item.value = PriceByRarity.fromItem(Item);
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Melee) += 0.20f;
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<BlackBeltHairStyle>() && legs.type == ModContent.ItemType<BlackBeltGiPants>();
    }

    public override void UpdateArmorSet(Player player)
    {
        player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
        if (player.statDefense >= 30)
        {
            player.statDefense = 30;
        }
        player.lifeRegen += 13;
        player.moveSpeed += player.endurance;
        player.endurance = 0f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MythrilChainmail, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
