using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Ranged;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Specialist
{
    public class Marksman : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 500;
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
            Item.shoot = ModContent.ProjectileType<MarksmanShot>();
            Item.shootSpeed = 26f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Materials.FlameOfTheAbyss>(), 5);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, -2);
        }

        //int ammoleft = 6;
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Coin>()] < 4)
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

                    if (Main.myPlayer == player.whoAmI)
                    {
                        Projectile.NewProjectile(source, position, speed, ModContent.ProjectileType<Coin>(), 0, 0, player.whoAmI);
                    }
                }
                return false;
            }
            else
            {
                if (!Main.dedServ)
                {
                    Vector2 aimDir = speed.SafeNormalize(Vector2.UnitX * player.direction);
                    Vector2 muzzlePos = position + aimDir * 38f;

                    // Reduced forward-cone muzzle flash matching MarksmanShot's impact dust (111).
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 dustVel = aimDir.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.5f, 6.5f);
                        Dust flash = Dust.NewDustPerfect(muzzlePos, 111, dustVel, 30, default, 0.85f);
                        flash.noGravity = true;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 sparkVel = aimDir.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.8f, 2.2f);
                        Dust spark = Dust.NewDustPerfect(muzzlePos, 111, sparkVel, 30, default, 0.65f);
                        spark.noGravity = true;
                    }
                }

                return true;
            }
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
    }
}
