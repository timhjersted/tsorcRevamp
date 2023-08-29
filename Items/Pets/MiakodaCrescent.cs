using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets;

class MiakodaCrescent : ModItem
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Miakoda - Crescent Moon Form");
        Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                            "\nwho has vowed to help you find your wife and defeat Attraidies." +
                            "\nMiakoda is an indigenous name that means \"Power of the Moon\"." +
                            "\nCrescent Moon Form - Imbues weapons with Crescent Moonlight" +
                            "\nwhen you get a crit (2 second duration, 12 second cooldown)" +
                            "\n+3% damage, +7% more while weapon imbue is active" +
                            "\nCan switch between forms at an altar");
    }
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.DD2PetGhost);
        Item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaCrescent>();
        Item.buffType = ModContent.BuffType<Buffs.MiakodaCrescent>();
    }

    public override void UseStyle(Player player, Rectangle rectangle)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
        {
            player.AddBuff(Item.buffType, 3600, true);
        }
    }

    public override void AddRecipes()
    {
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<MiakodaFull>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<MiakodaNew>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
