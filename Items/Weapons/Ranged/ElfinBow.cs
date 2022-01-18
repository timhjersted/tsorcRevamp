using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ElfinBow : ModItem {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Never miss again\n" +
                "Unleashes a storm of enchanted arrows that hunt down any enemy you select");
        }
        public override void SetDefaults() {
            item.autoReuse = true;
            item.damage = 200;
            item.height = 58;
            item.knockBack = 5;
            item.noMelee = true;
            item.ranged = true;
            item.rare = ItemRarityID.Red;
            item.scale = 0.9f;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 9;
            item.useAmmo = AmmoID.Arrow;
            item.useAnimation = 40;
            item.useTime = 9;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = PriceByRarity.Red_10;
            item.width = 14;
        }

        Projectile thisProjectile;
        public override void HoldItem(Player player)
        {
            if (Main.GameUpdateCount % 5 == 0)
            {
                int? closest = UsefulFunctions.GetClosestEnemyNPC(Main.MouseWorld);
                if (closest != null)
                {
                    if (thisProjectile == null || thisProjectile.active == false || thisProjectile.type != ModContent.ProjectileType<Projectiles.ElfinTargeting>())
                    {
                        thisProjectile = Projectile.NewProjectileDirect(Main.npc[closest.Value].position, Vector2.Zero, ModContent.ProjectileType<Projectiles.ElfinTargeting>(), 0, 0, Main.myPlayer, closest.Value);
                    }
                    else
                    {
                        thisProjectile.ai[0] = closest.Value;
                        thisProjectile.timeLeft = 15;
                    }
                }
            }
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            int randomness = 12;
            int target = -1;
            if(thisProjectile != null && thisProjectile.type == ModContent.ProjectileType<Projectiles.ElfinTargeting>())
            {
                target = thisProjectile.whoAmI;
                randomness = 3;
            }
            Vector2 projVel = Main.rand.NextVector2CircularEdge(randomness, randomness);
            Projectile.NewProjectile(player.Center, projVel, ModContent.ProjectileType<Projectiles.ElfinArrow>(), item.damage, item.knockBack, Main.myPlayer, target);
            return false;
        }       
    }
}
