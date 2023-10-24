using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Projectiles.Summon.Sentry
{
    public class GaleforceProjectile : FloatingSentryProjectile
    {
        public override int ShotCooldown => (int)(1.5f * 60f);
        public override int SentryShotCooldownReductionOnSpawn => 10;
        public override int ProjectileFrameCount => 6;
        public override int ProjectileWidth => 20;
        public override int ProjectileHeight => 20;
        public override DamageClass ProjectileDamageType => DamageClass.Summon;
        public override bool ContactDamage => false;
        public override int ShotProjectileType => ModContent.ProjectileType<BoltProjectile>();
        public override float ProjectileInitialVelocity => 20f;
        public override int AI1 => 1;
        public override int AI2 => 0;
    }
}