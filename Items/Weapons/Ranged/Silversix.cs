using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Silversix : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Silversix");
            Tooltip.SetDefault("Deals extra damage to corrupt/crimson creatures"
                                + "\nDoesn't require ammo");
        }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 48;
            Item.height = 34;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4;
            Item.value = 100000;
            Item.scale = 0.9f;
            Item.rare = ItemRarityID.LightRed;
            Item.crit = 5;
            Item.UseSound = SoundID.Item40;
            Item.shoot = Mod.Find<ModProjectile>("SSShot").Type;
            Item.shootSpeed = 22f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Revolver);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 0);
        }

        int ammoleft = 6;
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
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
                            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                        }
                        if (player.direction == -1)
                        {
                            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                        }
                    }
                }

                if (player.direction == 1)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(-0.2f, -1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                }
                if (player.direction == -1)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, new Vector2(Main.rand.NextFloat(0.2f, 1.1f), Main.rand.NextFloat(-1.1f, -1.6f)), ModContent.ProjectileType<Projectiles.ShulletBellLight>(), 0, 0, Main.myPlayer);
                }

                ammoleft = 6;
                return true;
            }
        }
    }
}
