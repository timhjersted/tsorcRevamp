using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class ArgentPeacemaker : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Argent Peacemaker");
            Tooltip.SetDefault("Deals extra damage to corrupt/crimson creatures");
        }

        public override void SetDefaults()
        {
            item.damage = 70;
            item.ranged = true;
            item.width = 52;
            item.height = 42;
            item.useTime = 13;
            item.useAnimation = 13;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true; //so the item's animation doesn't do damage
            item.knockBack = 4;
            item.value = 250000;
            item.scale = 0.9f;
            item.rare = ItemRarityID.Pink;
            item.crit = 5;
            item.UseSound = SoundID.Item40;
            //item.autoReuse = true;
            item.shoot = mod.ProjectileType("APShot");
            item.shootSpeed = 26f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(null, "Silversix");
            recipe.AddIngredient(ItemID.HallowedBar, 10);
            recipe.AddIngredient(ItemID.PixieDust, 35);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, -2);
        }

        int ammoleft = 6;
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            ammoleft--;
            if (ammoleft > 0)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    if (Main.rand.Next(2) == 0)
                    {
                        if (player.direction == 1)
                        {
                            Projectile.NewProjectile(player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                        }
                        if (player.direction == -1)
                        {
                            Projectile.NewProjectile(player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                        }
                    }
                }

                if (player.direction == 1)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                }
                if (player.direction == -1)
                {
                    Projectile.NewProjectile(player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                }

                ammoleft = 6;
                return true;
            }
        }
    }
}
