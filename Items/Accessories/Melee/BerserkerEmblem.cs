using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee;

public class BerserkerEmblem : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("11% increased melee damage and crit");
    }

    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 30;
        Item.accessory = true;
        Item.value = PriceByRarity.LightRed_4;
        Item.rare = ItemRarityID.LightRed;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.WarriorEmblem);
        recipe.AddIngredient(ItemID.EyeoftheGolem);
        recipe.AddIngredient(ItemID.HallowedBar, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Melee) += 0.11f;
        player.GetCritChance(DamageClass.Melee) += 11;
    }
}