using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Fireball2 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 120;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void AI()
        {
            Projectile.rotation += 0.25f;

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.InfernoFork, Main.rand.NextVector2Circular(2, 2)).noGravity = true;
            }

            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }

        public override void Kill(int timeLeft)
        {
            Projectile.penetrate = 2;
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;

            // do explosion
            Projectile.Damage();

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 70; i++)
            {
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, .1f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 0.7f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.Torch, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(Main.rand.NextVector2CircularEdge(15, 15), 130, Projectile.Center, 160, default, 1.5f).noGravity = true;
            }
        }
        Vector2 dustPos()
        {
            return Main.rand.NextVector2Circular(Projectile.width / 6, Projectile.height / 6) + Projectile.Center;
        }
        Vector2 dustVel()
        {
            return Main.rand.NextVector2Circular(7, 7);
        }
    }
}
