using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class Moonfury : ModItem {

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
            item.value = 50000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.MoonfuryBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BlueMoon, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
