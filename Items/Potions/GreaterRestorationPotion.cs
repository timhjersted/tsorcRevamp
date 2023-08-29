using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions;

class GreaterRestorationPotion : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Applies 40 seconds of Potion Sickness");
    }
    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.maxStack = 9999;
        Item.potion = true;
        Item.healLife = 125;
        Item.rare = ItemRarityID.Green;
        Item.consumable = true;
        Item.value = 10000;
        Item.UseSound = SoundID.Item3;
    }
    public override void GetHealLife(Player player, bool quickHeal, ref int healValue)
    {
        healValue = 125;
    }
    public override bool? UseItem(Player player)
    {
        player.ClearBuff(BuffID.PotionSickness);
        player.AddBuff(BuffID.PotionSickness, 2400);
        return base.UseItem(player);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(2);
        recipe.AddIngredient(ItemID.GreaterHealingPotion, 2);
        recipe.AddIngredient(ItemID.ChlorophyteOre, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 750);
        recipe.AddTile(TileID.Bottles);

        recipe.Register();
    }
}
