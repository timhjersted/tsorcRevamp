using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class QuadroCannon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Four shot burst" +
                                "\nOnly the first shot consumes ammo" +
                                "\nFires a spread of four bullets with each shot");
        }
        public override void SetDefaults() {
            Item.width = 64;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 16;
            Item.useTime = 4;
            Item.reuseDelay = 18;
            Item.damage = 35;
            Item.knockBack = 5;
            Item.crit = 3;
            Item.UseSound = SoundID.Item11;
            Item.rare = ItemRarityID.Red;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 10;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.useAmmo = AmmoID.Bullet;
        }


        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 2);
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("PhazonRifle").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfAttraidies").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 25);
            recipe.AddIngredient(Mod.Find<ModItem>("Humanity").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 120000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack) {
            int ShotAmt = 4;
            int spread = 24;
            float spreadMult = 0.05f;
            type = ModContent.ProjectileType<Projectiles.PhazonRound>();

            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 15f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
            {
                position -= muzzleOffset;
            }               
                
            for (int i = 0; i < ShotAmt; i++)
            {
                float vX = speedX + Main.rand.Next(-spread, spread + 1) * spreadMult;
                float vY = speedY + Main.rand.Next(-spread, spread + 1) * spreadMult;
                Projectile.NewProjectile(position, new Vector2(vX, vY), type, damage, knockBack, player.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, -1, -1, 11);
            }
            
            return false;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return !(player.itemAnimation < Item.useAnimation - 2);
        }
    }
}
