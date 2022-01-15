using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class DevilsScythe : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Devil's Scythe");
            Tooltip.SetDefault("Casts a hellfire scythe.");
        }
        public override void SetDefaults() {
            item.width = 26;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 15;
            item.damage = 40;
            item.knockBack = 5;
            item.scale = 0.9f;
            item.UseSound = SoundID.Item8;
            item.crit = 8;
            item.rare = ItemRarityID.Orange;
            item.mana = 14;
            item.noMelee = true;
            item.value = PriceByRarity.Orange_3;
            item.magic = true;
        }

        public override bool UseItem(Player player) {
            float num48 = .6f;
            float speedX = ((Main.mouseX + Main.screenPosition.X) - (player.position.X + player.width * 0.5f));
            float speedY = ((Main.mouseY + Main.screenPosition.Y) - (player.position.Y + player.height * 0.5f));
            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
            num51 = num48 / num51;
            speedX *= num51;
            speedY *= num51;
            Projectile.NewProjectile(new Vector2(player.position.X + (player.width * 0.5f), player.position.Y + (player.height * 0.5f)), new Vector2((float)speedX, (float)speedY), ModContent.ProjectileType<Projectiles.DevilSickle>(), (int)(player.inventory[player.selectedItem].damage * player.magicDamage), 3, player.whoAmI);
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.DemonScythe, 1);
            recipe.AddIngredient(ItemID.HellstoneBar, 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
