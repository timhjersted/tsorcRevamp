using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Starfall : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Pulls a shooting star out of space and brings it crashing down to earth." +
                                "\nOnly the most powerful mages will be capable of casting this spell.");
        }
        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 60;
            item.useTime = 60;
            item.damage = 1100;
            item.knockBack = 6;
            item.scale = 0.9f;
            item.UseSound = SoundID.Item4;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = 10;
            item.crit = 0;
            item.mana = 800;
            item.noMelee = true;
            item.value = 10000000;
            item.magic = true;
            item.shoot = ProjectileID.FallingStar;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.FallenStar, 100);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 10);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 190000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            float num109 = item.shootSpeed;
            Vector2 vector2;
            float num110;
            float num111;

            float num112;

            vector2 = new Vector2(player.position.X + player.width * 0.5f + Main.rand.Next(201) * (0f - player.direction) + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
            vector2.X = (vector2.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
            vector2.Y -= 100;
            num110 = Main.mouseX + Main.screenPosition.X - vector2.X;
            num111 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
            if (num111 < 0f) {
                num111 *= -1f;
            }
            if (num111 < 20f) {
                num111 = 20f;
            }
            num112 = (float)Math.Sqrt(num110 * num110 + num111 * num111);
            num112 = num109 / num112;
            num110 *= num112;
            num111 *= num112;
            float speedX2 = num110 + Main.rand.Next(-30, 31) * 0.02f;
            float speedY2 = num111 + Main.rand.Next(-30, 31) * 0.02f;
            Projectile.NewProjectile(vector2.X, vector2.Y, speedX2, speedY2, type, damage, knockBack, player.whoAmI, 0f, Main.rand.Next(15));

            return false;
        }
    }
}
