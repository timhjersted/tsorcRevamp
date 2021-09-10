using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class MeteorTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A legendary spell tome that calls down a meteor storm");
        }
        public override void SetDefaults() {
            item.damage = 90;
            item.height = 10;
            item.knockBack = 4;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6;
            item.noMelee = true;
            item.magic = true;
            item.mana = 70;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 120;
            item.value = 5000000;
            item.width = 34;
            item.autoReuse = true;
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
            Projectile.NewProjectile((float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200), player.position.Y - 800.0f,
                (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Meteor>(), (int)(item.damage *player.magicDamage), 2.0f, player.whoAmI);
            return true;
        }
    }
}
