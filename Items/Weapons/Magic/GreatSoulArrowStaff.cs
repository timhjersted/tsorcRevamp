using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class GreatSoulArrowStaff : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Soul Arrow Staff");
            Tooltip.SetDefault("Shoots a lightly homing great soul arrow");
        }
        public override void SetDefaults()
        {
            item.autoReuse = true;
            item.width = 40;
            item.height = 40;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 36;
            item.useTime = 36;
            item.damage = 34;
            item.knockBack = 5.5f;
            item.mana = 10;
            item.UseSound = SoundID.Item8;
            item.shootSpeed = 7.5f;
            item.noMelee = true;
            item.value = 10000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.GreatSoulArrow>();
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 0);
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {

            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 25f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0)) {
                position += muzzleOffset;
                position.Y += -14;
            }
            float mySpeedX = Main.mouseX + Main.screenPosition.X - position.X;
            float mySpeedY = Main.mouseY + Main.screenPosition.Y - position.Y;
            float speedAbs = (float)Math.Sqrt((mySpeedX * mySpeedX) + (mySpeedY * mySpeedY));
            speedAbs = 7f / speedAbs; // for speed consistency
            mySpeedX *= speedAbs;
            mySpeedY *= speedAbs;
            Projectile.NewProjectile(new Vector2(position.X, position.Y), new Vector2(mySpeedX, mySpeedY), ModContent.ProjectileType<Projectiles.GreatSoulArrow>(), damage, knockBack);

            return false;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("SoulArrowStaff"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
