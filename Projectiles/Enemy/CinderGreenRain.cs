using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>A high green lob which gently corrects toward its marked player after reaching its apex.</summary>
    class CinderGreenRain : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyBioSpitBall";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 220;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.30f;
            int targetIndex = (int)Projectile.ai[0];
            if (Projectile.velocity.Y > 0f && targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                if (target.active && !target.dead)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 10f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.018f);
                }
            }
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch,
                    -Projectile.velocity * 0.06f, 120, Color.LimeGreen, 0.9f);
                dust.noGravity = true;
            }
        }
    }
}
