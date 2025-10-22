using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class TaintedBow : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder; 
            Item.damage = 24;
            Item.height = 46;
            Item.width = 18;
            Item.knockBack = 1.25f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 7.5f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ModContent.ProjectileType<TaintedArrowProjectile>();
                velocity = velocity.SafeNormalize(Vector2.Zero) * (Item.shootSpeed + 3.8f); 
                damage = damage + 7; 
                knockback = knockback + 3; 
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.DemonBow);
            recipe.AddIngredient(ItemID.RottenChunk, 5);
            recipe.AddIngredient(ItemID.VilePowder, 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

    public class TaintedBowGlobalProjectile : GlobalProjectile
    {
        public override void AI(Projectile projectile)
        {
            if (projectile.friendly && projectile.DamageType == DamageClass.Ranged && projectile.owner >= 0)
            {
                Player player = Main.player[projectile.owner];
                if (player.HeldItem.type == ModContent.ItemType<TaintedBow>())
                {
                    if (Main.rand.NextBool(6)) 
                    {
                        int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Poisoned, 0f, 0f, 100, default, 1f);
                        Main.dust[dust].noGravity = false;
                        Main.dust[dust].velocity *= 0.5f;
                    }
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.friendly && projectile.DamageType == DamageClass.Ranged && projectile.owner >= 0)
            {
                Player player = Main.player[projectile.owner];
                if (player.HeldItem.type == ModContent.ItemType<TaintedBow>())
                {
                    target.AddBuff(BuffID.Poisoned, 180);
                }
            }
        }
    }
}