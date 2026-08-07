using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Damage companion to the ElandVenomImpact splat shader. That splat is drawn at 92px across, but
    /// the burst that draws it is a pure-VFX projectile (CanDamage() => false), so before this the
    /// corrosive splash was entirely cosmetic - it promised a wide area of effect over a 14px spit
    /// that had already resolved. This gives the visible splat a matching hitbox.
    ///
    /// Deliberately brief: it arms for a short window at the moment of impact so it reads as the
    /// splash connecting, rather than becoming a lingering damage zone (StickySpitCloud already fills
    /// that role for the sticky variant).
    /// </summary>
    public class ElandVenomSplash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        // Matches the 92px draw size of the ElandVenomImpact burst.
        const float Radius = 46f;
        const int ActiveStart = 2;
        const int ActiveEnd = 14;

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ActiveEnd + 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.ai[0]++;
        }

        public override bool? CanDamage() => Projectile.ai[0] >= ActiveStart && Projectile.ai[0] <= ActiveEnd;

        // True circle rather than the square AABB, so the corners of the hitbox don't hit people
        // standing outside the visible splat.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 8 * 60, false);
        }

        // Purely a hitbox - the visual is the paired EnemyShaderBurst spawned alongside it.
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
