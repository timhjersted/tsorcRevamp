using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class QuadroCannon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Four shot burst" +
                                "\nOnly the first shot consumes ammo" +
                                "\nFires a spread of four bullets with each shot");
        }
        public override void SetDefaults() {
            item.width = 64;
            item.height = 24;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 16;
            item.useTime = 4;
            item.reuseDelay = 18;
            item.damage = 35;
            item.knockBack = 5;
            item.crit = 3;
            item.UseSound = SoundID.Item11;
            item.rare = ItemRarityID.LightRed;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 14;
            item.noMelee = true;
            item.value = 9000000;
            item.ranged = true;
            item.autoReuse = true;
            item.useAmmo = AmmoID.Bullet;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("PhazonRifle"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 25);
            recipe.AddIngredient(mod.GetItem("Humanity"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 120000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            int ShotAmt = 4;
            int spread = 24;
            float spreadMult = 0.05f;
            for (int i = 0; i < ShotAmt; i++) {
                float vX = speedX + (float)Main.rand.Next(-spread, spread + 1) * spreadMult;
                float vY = speedY + (float)Main.rand.Next(-spread, spread + 1) * spreadMult;
                Projectile.NewProjectile(position, new Vector2(vX, vY), type, damage, knockBack, player.whoAmI);
                Main.PlaySound(SoundID.Item, -1, -1, 11);
            }
            return false;
        }

        public override bool ConsumeAmmo(Player player) {
            return !(player.itemAnimation < item.useAnimation - 2);
        }
    }
}
