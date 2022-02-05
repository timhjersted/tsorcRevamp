using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Starfall : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summon a rain of homing stars\n'Your very own meteor shower'");
        }
        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;

            item.knockBack = 6;
            item.scale = 0.9f;
            
            item.rare = ItemRarityID.Cyan;
            item.shootSpeed = 10;
            item.noMelee = true;
            item.value = PriceByRarity.Cyan_9;
            item.magic = true;

            item.UseSound = SoundID.Item25;
            item.mana = 14;
            item.damage = 76;
            item.useAnimation = item.useTime = 15;
            item.shoot = ModContent.ProjectileType<Projectiles.StarfallProjectile>();
            item.autoReuse = true;
            
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
            for (int i = 0; i < 3; i++) {
                Vector2 pos = new Vector2(player.Center.X + Main.rand.Next(151) * (0f - player.direction) + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                pos.X = (pos.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                pos.Y -= 100 * i; //they all spawn simultaneously, so offset them vertically to slightly stagger them
                float velX = Main.mouseX + Main.screenPosition.X - pos.X + Main.rand.Next(-40, 41) * 0.03f;
                float velY = Main.mouseY + Main.screenPosition.Y - pos.Y;
                if (velY < 0f) {
                    velY *= -1f;
                }
                if (velY < 20f) {
                    velY = 20f;
                }
                Vector2 h = Vector2.Normalize(new Vector2(velX, velY)) * item.shootSpeed;
                h.Y += Main.rand.Next(-40, 41) * 0.02f;
                Projectile.NewProjectile(pos.X, pos.Y, h.X * 1.5f, h.Y * 1.5f, type, damage, knockBack, player.whoAmI, 0f, 0f);
            }
            return false;
        }
    }
}
