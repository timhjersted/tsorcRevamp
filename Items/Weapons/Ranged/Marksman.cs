using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Marksman : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("For those with mechanical precision" +
                "\nRight click to throw a coin" +
                "\nShooting a coin reflects your shot into the nearest enemy weak point" +
                "\nReflected shots have increased damage, guarenteed crits, and pierce walls");
        }

        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 52;
            Item.height = 42;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true; //so the item's animation doesn't do damage
            Item.knockBack = 4;
            Item.value = 250000;
            Item.scale = 0.9f;
            Item.rare = ItemRarityID.Pink;
            Item.crit = 5;
            Item.UseSound = SoundID.Item40;
            Item.shoot = ModContent.ProjectileType<Projectiles.MarksmanShot>();
            Item.shootSpeed = 26f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Items.FlameOfTheAbyss>(), 5);
            recipe.AddIngredient(ModContent.ItemType<Items.CursedSoul>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, -2);
        }

        int ammoleft = 6;
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if(player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Coin>()] < 4)
                {
                    speed.Normalize();
                    speed *= 8;
                    if (player.direction == 1)
                    {
                        speed = speed.RotatedBy(-MathHelper.PiOver4 / 2);
                    }
                    else
                    {
                        speed = speed.RotatedBy(MathHelper.PiOver4 / 2);
                    }

                    speed += player.velocity;

                    Projectile.NewProjectile(source, position, speed, ModContent.ProjectileType<Projectiles.Coin>(), 0, 0, player.whoAmI);
                }
                return false;
            }
            else
            {
                return true;
            }            
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
    }
}
