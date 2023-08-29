using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Body)]
public class SmoughArmor : ModItem
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Smough's Armor");
        Tooltip.SetDefault("Increases the effectiveness of the Dragon Crest Shield" +
            "\nSet Bonus: Decreases all attack speed by 50% multiplicatively but grants 100% increased critical strike chance");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.defense = 7;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetModPlayer<tsorcRevampPlayer>().SmoughShieldSkills = true;
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return head.type == ModContent.ItemType<SmoughHelmet>() && legs.type == ModContent.ItemType<SmoughGreaves>();
    }
    public override void UpdateArmorSet(Player player)
    {
        player.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction = true;
        player.GetAttackSpeed(DamageClass.Generic) /= 2;
        player.GetCritChance(DamageClass.Generic) += 100;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CopperChainmail);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
