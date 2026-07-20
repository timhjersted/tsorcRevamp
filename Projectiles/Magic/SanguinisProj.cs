using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Projectiles.Magic
{
    class SanguinisProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 200;
            Projectile.timeLeft = 300;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            int num40 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 183, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 2.8f);
            Main.dust[num40].noGravity = true;
            if (Main.rand.NextBool(15))
            {
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 235, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1.6f);
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }

            int? closestEnemy = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
            if (closestEnemy.HasValue && Projectile.timeLeft < 270)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.npc[closestEnemy.Value].Center, 0.15f, 10, Main.npc[closestEnemy.Value].velocity, false);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), 3 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;  

            for (int i = 0; i < Main.rand.Next(17, 22); i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(22f, 22f),
                    235,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(1.8f, 2.3f)
                );
                dust.noGravity = true;
            }
        }
    }
}