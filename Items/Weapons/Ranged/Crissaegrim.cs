using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class Crissaegrim : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A powerful, throwable sword made of 30 lost souls of the night" +
                                "\nA dark aura endlessly manifests four blades in the wielder's hand, and will" +
                                "\nreturn them if the wielder's throw misses the one for whom it was intended.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.width = 20;
            item.height = 20;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 30;
            item.useTime = 30;
            item.useTurn = true;
            item.damage = 50;
            item.autoReuse = true;
            item.UseSound = SoundID.Item1;
            item.noMelee = true;
            item.ranged = true;
            item.value = 200000;
            item.noUseGraphic = true;
            item.shoot = ProjectileID.PurificationPowder;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofNight, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            float num48 = 14f; //this controls the range
            float mySpeedX = Main.mouseX + Main.screenPosition.X - (player.position.X + player.width * 0.5f);
            float mySpeedY = Main.mouseY + Main.screenPosition.Y - (player.position.Y + player.height * 0.5f);
            float num51 = (float)Math.Sqrt((double)((mySpeedX * mySpeedX) + (mySpeedY * mySpeedY)));
            num51 = num48 / num51;
            mySpeedX *= num51;
            mySpeedY *= num51;

            if ((player.direction == -1) && ((Main.mouseX + Main.screenPosition.X) > (player.position.X + player.width * 0.5f))) {
                player.direction = 1;
            }
            if ((player.direction == 1) && ((Main.mouseX + Main.screenPosition.X) < (player.position.X + player.width * 0.5f))) {
                player.direction = -1;
            }

            if (player.direction == 1) {
                player.itemRotation = (float)Math.Atan2((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f),
                (Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            }
            else player.itemRotation = (float)Math.Atan2((player.position.Y + player.height * 0.5f) - (Main.mouseY + Main.screenPosition.Y), (player.position.X + player.width * 0.5f) - (Main.mouseX + Main.screenPosition.X));

            for (int i = 0; i < 4; i++) {
                Vector2 shiftedSpeed = new Vector2(mySpeedX, mySpeedY).RotatedByRandom(MathHelper.ToRadians(8));
                float speedOffset = 1f - (Main.rand.NextFloat() * 0.2f);
                shiftedSpeed *= speedOffset;
                Projectile.NewProjectile(
                                (float)player.position.X + (player.width * 0.5f),
                                (float)player.position.Y + (player.height * 0.5f),
                                (float)shiftedSpeed.X,
                                (float)shiftedSpeed.Y,
                                ModContent.ProjectileType<Projectiles.Crissaegrim>(),
                                (int)(item.damage * player.magicDamage),
                                item.knockBack,
                                Main.myPlayer
                                );

            }
            return false;
        }

    }
}
