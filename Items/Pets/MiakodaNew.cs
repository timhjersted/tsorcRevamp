using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
    class MiakodaNew : ModItem //think of ways to not make it mostly obsolete by the time it's obtained :(
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - New Moon Form");
            Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                                "\nwho has vowed to help you find your wife and defeat Attraidies." +
                                "\nMiakoda is an indigenous name that means \"Power of the Moon\"." +
                                "\n[c/00ff00:Revamped Mode:] New Moon Form - Imbues weapons with Midas when you get a crit," +
                                "\nboosting enemy coin drops (2 second duration, 12 second cooldown)" +
                                "\n+5% movespeed, +90% more while boost is active" +
                                "\nKnockback immunity and half damage taken while boost is active" +
                                "\nCan switch between forms at an altar");
        }
        public override void SetDefaults()
        {
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaNew>();
            item.buffType = ModContent.BuffType<Buffs.MiakodaNew>();
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
                recipe.AddIngredient(mod.GetItem("MiakodaCrescent"));
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
        }
    }
}
