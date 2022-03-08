using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ExpulsorCannon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Four shot burst" +
                                "\nOnly the first shot consumes ammo" +
                                "\nFires a spread of four bullets with each shot");
        }
        public override void SetDefaults() {
            item.width = 56;
            item.height = 24;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 16;
            item.useTime = 4;
            item.reuseDelay = 18;
            item.damage = 50;
            item.knockBack = 3;
            item.crit = 5;
            item.autoReuse = true;
            item.UseSound = SoundID.Item11;
            item.rare = ItemRarityID.Purple;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 13;
            item.useAmmo = AmmoID.Bullet;
            item.noMelee = true;
            item.value = PriceByRarity.Purple_11;
            item.ranged = true;
            item.autoReuse = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("QuadroCannon"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 30);
            //recipe.AddIngredient(mod.GetItem("SoulOfBlight"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfChaos"), 1);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 90);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 280000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            int ShotAmt = 4;
            int spread = 24;
            float spreadMult = 0.05f;
            type = ModContent.ProjectileType<Projectiles.PhazonRound>();
            for (int i = 0; i < ShotAmt; i++) {
                float vX = speedX + Main.rand.Next(-spread, spread + 1) * spreadMult;
                float vY = speedY + Main.rand.Next(-spread, spread + 1) * spreadMult;
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
