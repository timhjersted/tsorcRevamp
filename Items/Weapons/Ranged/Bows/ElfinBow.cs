using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Ranged;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class ElfinBow : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.damage = 180;
            Item.height = 58;
            Item.knockBack = 5;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ItemRarityID.Red;
            Item.scale = 0.9f;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 9;
            Item.useAmmo = AmmoID.Arrow;
            Item.useAnimation = 9;
            Item.useTime = 9;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Red_10;
            Item.width = 14;
        }

        Projectile thisProjectile;
        public override void HoldItem(Player player)
        {
            if (Main.GameUpdateCount % 5 == 0)
            {
                int? closest = UsefulFunctions.GetClosestEnemyNPC(Main.MouseWorld);
                if (closest != null)
                {
                    if (thisProjectile == null || thisProjectile.active == false || thisProjectile.type != ModContent.ProjectileType<ElfinTargeting>())
                    {
                        thisProjectile = Projectile.NewProjectileDirect(player.GetSource_ItemUse(Item), Main.npc[closest.Value].position, Vector2.Zero, ModContent.ProjectileType<ElfinTargeting>(), 0, 0, player.whoAmI, closest.Value);
                    }
                    else
                    {
                        thisProjectile.ai[0] = closest.Value;
                        thisProjectile.timeLeft = 15;
                    }
                }
            }
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            int randomness = 1;
            int target = -1;
            if (thisProjectile != null && thisProjectile.type == ModContent.ProjectileType<ElfinTargeting>())
            {
                target = thisProjectile.whoAmI;
            }

            Vector2 projVel = speed + Main.rand.NextVector2CircularEdge(randomness, randomness);
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, projVel, ModContent.ProjectileType<ElfinArrow>(), damage, knockBack, player.whoAmI, target);
            return false;
        }
    }
}
