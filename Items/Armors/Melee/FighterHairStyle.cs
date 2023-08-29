using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee;

[AutoloadEquip(EquipType.Head)]
public class FighterHairStyle : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Adept at close combat" +
            "\n+17% melee crit");
    }
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 12;
        Item.defense = 2;
        Item.rare = ItemRarityID.Lime;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Melee) += 17;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.AdamantiteHelmet, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
