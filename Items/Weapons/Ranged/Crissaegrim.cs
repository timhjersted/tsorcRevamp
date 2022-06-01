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
            Item.rare = ItemRarityID.LightRed;
            Item.width = 20;
            Item.height = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useTurn = true;
            Item.damage = 50;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.value = PriceByRarity.LightRed_4;
            Item.noUseGraphic = true;
            Item.shoot = ProjectileID.PurificationPowder;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SoulofNight, 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack) {
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
                                (int)(Item.damage * player.magicDamage),
                                Item.knockBack,
                                Main.myPlayer
                                );

            }
            return false;
        }

    }
}
