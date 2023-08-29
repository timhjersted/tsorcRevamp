using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions;

public class ShockwavePotion : ModItem
{

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Hold DOWN to increase fall speed \nCreate a damaging shockwave when you land \nwhich grows in strength based on distance fallen");
    }
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 30;
        Item.useStyle = ItemUseStyleID.EatFood;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.useTurn = true;
        Item.UseSound = SoundID.Item3;
        Item.maxStack = 9999;
        Item.consumable = true;
        Item.rare = ItemRarityID.Blue;
        Item.value = 5000;
        Item.buffType = ModContent.BuffType<Buffs.Shockwave>();
        Item.buffTime = 12600;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BottledWater, 1);
        recipe.AddIngredient(ItemID.Blinkroot, 1);
        recipe.AddIngredient(ItemID.SoulofLight, 1);
        recipe.AddIngredient(ItemID.Meteorite, 1);
        recipe.AddTile(TileID.Bottles);

        recipe.Register();
    }
}
