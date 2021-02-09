using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons {
    public class GaeBolg : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Gae Bolg");
            Tooltip.SetDefault("Pierce reality \nCan be upgraded into its mythical form with 70,000 Dark Souls");
        }

        public override void SetDefaults() {
            item.damage = 50;
            item.knockBack = 6.6f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 20;
            item.useTime = 20;
            item.shootSpeed = 8;
            
            item.height = 40;
            item.width = 40;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 200000;
            item.rare = ItemRarityID.Pink;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.GaeBolg>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Gungnir);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
