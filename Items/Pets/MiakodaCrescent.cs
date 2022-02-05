using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
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
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaCrescent>();
            item.buffType = ModContent.BuffType<Buffs.MiakodaCrescent>();
        }

        public override void UseStyle(Player player)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            {
                player.AddBuff(item.buffType, 3600, true);
            }
        }

        public override void AddRecipes()
        {
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(mod.GetItem("MiakodaFull"));
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(mod.GetItem("MiakodaNew"));
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
        }
    }
}
