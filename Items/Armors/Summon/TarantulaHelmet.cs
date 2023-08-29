using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon;

[AutoloadEquip(EquipType.Head)]
public class TarantulaHelmet : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("\nIncreases whip damage by 25%" +
            "\nSet Bonus: Increases whip range by 30% and summon attack speed by 20%" +
            "\nIncreases critical strike damage by 25%");
    }
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.defense = 12;
        Item.rare = ItemRarityID.Pink;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.25f;
    }
    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        return body.type == ModContent.ItemType<TarantulaCarapace>() && legs.type == ModContent.ItemType<TarantulaLegs>();
    }
    public override void UpdateArmorSet(Player player)
    {
        player.whipRangeMultiplier += 0.3f;
        player.GetAttackSpeed(DamageClass.Summon) += 0.2f;
        player.GetModPlayer<tsorcRevampPlayer>().CritDamage250 = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SpiderMask);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        recipe2.AddIngredient(ItemID.SpiderMask);
        recipe2.AddIngredient(ItemID.OrichalcumMask);
        recipe2.AddTile(TileID.DemonAltar);

        recipe2.Register();
    }
}