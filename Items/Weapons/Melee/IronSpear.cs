using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class IronSpear : ModItem {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            item.damage = 8;
            item.knockBack = 4f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 31;
            item.useTime = 31;
            item.shootSpeed = 3.7f;
            //item.shoot = ProjectileID.DarkLance;

            item.height = 32;
            item.width = 32;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 1000;
            item.rare = ItemRarityID.White;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.IronSpear>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronBar, 10);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.Anvils);
            recipe.AddRecipe();
        }
    }
}
