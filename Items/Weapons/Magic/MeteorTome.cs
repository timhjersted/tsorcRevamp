using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class MeteorTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A legendary spell tome.");
        }
        public override void SetDefaults() {
            item.damage = 500;
            item.height = 10;
            item.knockBack = 4;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6;
            item.noMelee = true;
            item.magic = true;
            item.mana = 200;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = 5000000;
            item.width = 34;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofMight, 2);
            recipe.AddIngredient(mod.GetItem("MeteorShower"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            Projectile.NewProjectile((float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200), (float)(Main.mouseY + Main.screenPosition.Y) - 500.0f,
                (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Meteor>(), (int)(item.damage *player.magicDamage), 2.0f, player.whoAmI);
            return true;
        }
    }
}
