using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons {
    public class DragoonLance : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Dragoon Lance");
            Tooltip.SetDefault("A spear forged from the fang of the Dragoon Serpent.");
        }

        public override void SetDefaults() {
            item.damage = 100;
            item.knockBack = 15f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 1;
            item.shootSpeed = 8;
            
            item.height = 74;
            item.width = 74;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 200000;
            item.rare = ItemRarityID.Pink;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.DragoonLance>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("GaeBolg"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
