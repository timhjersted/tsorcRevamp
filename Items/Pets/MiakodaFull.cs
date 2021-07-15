using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
    class MiakodaFull : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override string Texture => "tsorcRevamp/Items/Pets/Miakoda";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - Full Moon Form");
            Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                                "\nwho has vowed to help you find your wife and defeat Attraidies." +
                                "\nMiakoda is an indigenous name that means \"Power of the Moon\"." +
                                "\n[c/00ff00:Revamped Mode:] Full Moon Form - Heals you when you get a crit" +
                                "\n(12 second cooldown - heal scales with max hp)" +
                                "\n+3% damage reduction" +
                                "\nCan switch between forms at an altar");

        }
        public override void SetDefaults()
        {
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>();
            item.buffType = ModContent.BuffType<Buffs.MiakodaFull>();
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
                recipe.AddIngredient(mod.GetItem("MiakodaCrescent"));
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
