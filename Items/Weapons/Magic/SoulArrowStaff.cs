using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class SoulArrowStaff : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul Arrow Staff");
            Tooltip.SetDefault("Shoots a lightly homing soul arrow" +
                                "\nCan be upgraded with 4000 Dark Souls");
        }
        public override void SetDefaults()
        {
            item.autoReuse = true;
            item.width = 40;
            item.height = 40;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 40;
            item.useTime = 40;
            item.damage = 24;
            item.knockBack = 4.5f;
            item.mana = 6;
            item.UseSound = SoundID.Item8;
            item.shootSpeed = 7;
            item.noMelee = true;
            item.value = 3000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.SoulArrow>();
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 0);
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
		{
            
            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 25f;
			if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
			{
				position += muzzleOffset;
                position.Y += -14;
            }
            float mySpeedX = Main.mouseX + Main.screenPosition.X - position.X;
            float mySpeedY = Main.mouseY + Main.screenPosition.Y - position.Y;
            float speedAbs = (float)Math.Sqrt((mySpeedX * mySpeedX) + (mySpeedY * mySpeedY));
            speedAbs = 7f / speedAbs; // for speed consistency
            mySpeedX *= speedAbs;
            mySpeedY *= speedAbs;
            Projectile.NewProjectile(new Vector2(position.X, position.Y), new Vector2(mySpeedX, mySpeedY), ModContent.ProjectileType<Projectiles.SoulArrow>(), damage, knockBack, player.whoAmI);

			return false;
		}

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("WoodenWand"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
