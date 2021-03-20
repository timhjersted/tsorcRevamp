using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{

    public class ReforgedOldMorningStar : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.scale = 0.8f;
            item.useAnimation = 60;
            item.useTime = 60;
            item.damage = 13;
            item.knockBack = 7f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.White;
            item.shootSpeed = 10;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 12000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.OldMorningStar>();
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldMorningStar"));
            //recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
