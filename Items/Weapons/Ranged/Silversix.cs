using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Silversix : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Silversix");
            Tooltip.SetDefault("Deals extra damage to corrupt/crimson creatures"
                                +"\nDoesn't require ammo");
        }

        public override void SetDefaults()
        {
            item.damage = 40;
            item.ranged = true;
            item.width = 48;
            item.height = 34;
            item.useTime = 14;
            item.useAnimation = 14;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 4;
            item.value = 100000;
            item.scale = 0.9f;
            item.rare = ItemRarityID.LightRed;
            item.crit = 5;
            item.UseSound = SoundID.Item40;
            item.shoot = mod.ProjectileType("SSShot");
            item.shootSpeed = 22f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Revolver);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 0);
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
