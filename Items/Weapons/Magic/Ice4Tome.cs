using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice4Tome : ModItem {


        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 4 Tome");
            Tooltip.SetDefault("A lost legendary tome.");
        }
        public override void SetDefaults() {
            item.damage = 120;
            item.height = 10;
            item.knockBack = 0f;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.channel = true;
            item.autoReuse = true;
            item.shootSpeed = 11;
            item.magic = true;
            item.noMelee = true;
            item.mana = 100;
            item.useAnimation = 20;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.value = 600000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice4Ball>();
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            int count = 0;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                if (Main.projectile[i].modProjectile != null && Main.projectile[i].modProjectile is Projectiles.Ice4Ball)
                {
                    Projectiles.Ice4Ball iceBall = (Projectiles.Ice4Ball)Main.projectile[i].modProjectile;
                    if (iceBall.isChanneled)
                    {
                        count++;
                    }
                }
            }
            if (count < 10) return true;
            else return false;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Ice3Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
