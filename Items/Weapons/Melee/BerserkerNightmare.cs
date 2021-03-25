using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class BerserkerNightmare : ModItem { 

        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.useAnimation = 44;
            item.useTime = 44;
            item.damage = 49;
            item.knockBack = 8;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = 13;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 205000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.BerserkerSphere>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.DaoofPow, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
