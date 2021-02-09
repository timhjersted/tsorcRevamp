using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons {
    public class ForgottenRadiantLance : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Forgotten Radiant Lance");
            Tooltip.SetDefault("A light glows from this great spear.");
        }

        public override void SetDefaults() {
            item.damage = 60;
            item.knockBack = 3f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 3;
            item.shootSpeed = 10;
            //item.shoot = ProjectileID.DarkLance;
            
            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 1500000;
            item.rare = ItemRarityID.Orange;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenRadiantLance>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Gungnir);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 30000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
