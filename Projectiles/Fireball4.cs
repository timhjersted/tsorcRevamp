using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Fireball4 : ModProjectile
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

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2BetsyFireball;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.InfernoFork, Main.rand.NextVector2Circular(2, 2)).noGravity = true;
            }
        }
        public override void Kill(int timeLeft)
        {
            Projectile.penetrate = 2;
            Projectile.width = Projectile.width << 3;
            Projectile.height = Projectile.height << 3;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;

            // do explosion
            Projectile.Damage();

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 70; i++)
            {
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, .1f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.Torch, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(Main.rand.NextVector2CircularEdge(15, 15), 130, Projectile.Center, 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 1.5f).noGravity = true;
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2CircularEdge(24, 24), Vector2.Zero, ModContent.ProjectileType<FireballInferno2>(), Projectile.damage / 2, 0, default, 3);
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
