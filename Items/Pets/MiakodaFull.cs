using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
    class MiakodaFull : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - Full Moon Form");
            Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                                "\nwho has vowed to help you find your wife and defeat Attraidies." +
                                "\nMiakoda is an indigenous name that means \"Power of the Moon\"." +
                                "\nFull Moon Form - Heals you when you get a crit" +
                                "\n(12 second cooldown - heal scales with max hp)" +
                                "\n+3% damage reduction" +
                                "\nCan switch between forms at an altar");

        }
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DD2PetGhost);
            Item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>();
            Item.buffType = ModContent.BuffType<Buffs.MiakodaFull>();
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
                recipe.AddIngredient(Mod.Find<ModItem>("MiakodaCrescent").Type);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(Mod.Find<ModItem>("MiakodaNew").Type);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
                recipe.AddTile(TileID.DemonAltar);

                recipe.Register();
            }
        }
    }
}
