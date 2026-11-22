using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Abyss Drop's shrapnel (Acolyte of Grand Occultist): six of these leave a burst Dark Orb in an upward 180-degree fan at
    /// 4 px/t, arc over under light gravity and fall. 120-tick life with a 15-tick shrink/fade; walls kill them with a puff.
    /// Single-frame sprite, spun as it flies.
    /// </summary>
    public class OolacileDarkBead : ModProjectile
    {
        private const int Lifetime = 120;
        private const int FadeTicks = 15;
        private const float Gravity = 0.12f;
        private const float MaxFallSpeed = 8f;

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            if (Projectile.timeLeft < FadeTicks)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
            Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);

            if (Projectile.timeLeft < FadeTicks)
            {
                float fade = Projectile.timeLeft / (float)FadeTicks;
                Projectile.scale = fade;
                Projectile.alpha = (int)(255f * (1f - fade));
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 50, default, 1.2f);
                trail.noGravity = true;
                trail.velocity *= 0.3f;
            }

            Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 14; i++)
            {
                Dust puff = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(3f, 3f), 50, default, 1.3f);
                puff.noGravity = true;
            }
        }
    }
}
