using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Damage companion to the EvilEyeTeleportBurst shader (EnemyShaderBurst). A brief hitbox timed to
    /// the flash beat of that burst so a player caught standing in the teleport blast actually gets
    /// hurt by it - matching how DemonSpirit's own explosion damages the moment it detonates, rather
    /// than the teleport being purely decorative.
    /// </summary>
    public class EvilEyeTeleportBlast : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float Radius = 90f;
        const int ActiveStart = 3;
        const int ActiveEnd = 16;

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ActiveEnd + 2;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.ai[0]++;
        }

        public override bool? CanDamage() => Projectile.ai[0] >= ActiveStart && Projectile.ai[0] <= ActiveEnd;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center()) <= Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Purely a hitbox - the visual is the paired EnemyShaderBurst spawned alongside it.
            return false;
        }
    }
}
