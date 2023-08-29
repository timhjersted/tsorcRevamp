using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions;

public class BattlefrontPotion : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases damage by 15% and critical strike chance by 15%" +
                           "\nGrants enhanced Thorns and the Battle potion effect" +
                           "\nDoes not stack with Demon Drug, Armor Drug, or Strength Potions.");
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
        Item.buffType = ModContent.BuffType<Buffs.Battlefront>();
        Item.buffTime = 28800;
    }

    public override bool? UseItem(Player player)
    {
        int currentBuff = 0;
        foreach (int buffType in player.buffType)
        {
            if (buffType == ModContent.BuffType<Buffs.Strength>() || buffType == ModContent.BuffType<Buffs.DemonDrug>() || buffType == ModContent.BuffType<Buffs.ArmorDrug>())
            {
                player.DelBuff(currentBuff);
            }
            currentBuff++;
        }
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(4);
        recipe.AddIngredient(ItemID.BattlePotion, 4);
        recipe.AddIngredient(ItemID.ThornsPotion, 4);
        recipe.AddIngredient(ItemID.WrathPotion, 4);
        recipe.AddIngredient(ItemID.RagePotion, 4);
        recipe.AddIngredient(ItemID.SoulofNight, 4);
        recipe.AddTile(TileID.Bottles);

        recipe.Register();
    }
}
