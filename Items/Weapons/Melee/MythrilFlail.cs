using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class MythrilFlail : ModItem {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.useAnimation = 44;
            item.useTime = 44;
            item.damage = 45;
            item.knockBack = 8;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = 13;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = PriceByRarity.LightRed_4;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.MythrilBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Chain, 3);
            recipe.AddIngredient(ItemID.MythrilBar, 17);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
