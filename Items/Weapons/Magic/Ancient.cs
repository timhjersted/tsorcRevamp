using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Ancient : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Creates a Sandstorm using a long forgotten spell.");

        }

        public override void SetDefaults() {

            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTurn = true;
            item.useAnimation = 25;
            item.useTime = 25;
            item.maxStack = 1;
            item.damage = 80;
            item.autoReuse = true;
            item.knockBack = (float)4;
            item.scale = (float)1;
            item.UseSound = SoundID.Item34;
            //item.projectile=Sandstorm;
            item.rare = ItemRarityID.Red;
            item.shootSpeed = (float)10;
            item.crit = 2;
            item.mana = 14;
            item.noMelee = true;
            item.value = PriceByRarity.Red_10;
            item.magic = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 25);
            recipe.AddIngredient(ItemID.SandBlock, 150);
            recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 120000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            int spread = 10;
            float num48 = 14f;

            Vector2 speed, position = player.position;

            speed.X = ((Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f)) + Main.rand.Next(-spread, spread);
            speed.Y = ((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f)) + Main.rand.Next(-spread, spread);
            float num51 = (float)Math.Sqrt((double)((speed.X * speed.X) + (speed.Y * speed.Y)));
            num51 = num48 / num51;
            speed.X *= num51;
            speed.Y *= num51;

            if ((player.direction == -1) && ((Main.mouseX + Main.screenPosition.X) > (player.position.X + player.width * 0.5f))) {
                player.direction = 1;
            }
            if ((player.direction == 1) && ((Main.mouseX + Main.screenPosition.X) < (player.position.X + player.width * 0.5f))) {
                player.direction = -1;
            }

            if (player.direction == 1) {
                player.itemRotation = (float)Math.Atan2((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f), (Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            }
            else {
                player.itemRotation = (float)Math.Atan2((player.position.Y + player.height * 0.5f) - (Main.mouseY + Main.screenPosition.Y), (player.position.X + player.width * 0.5f) - (Main.mouseX + Main.screenPosition.X));
            }

            position.X += player.width * 0.5f;
            position.Y += player.height * 0.5f;
            int damage = (int)(item.damage * player.magicDamage);
            float knockback = player.inventory[player.selectedItem].knockBack;

            Projectile.NewProjectile(position, speed, ModContent.ProjectileType<Projectiles.Sandstorm>(), damage, knockback, player.whoAmI);

            return true;
        }
    }
}
