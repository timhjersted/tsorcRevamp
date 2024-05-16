using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Sentry
{
    public class GaleforceProjectile : FloatingSentryProjectile
    {
        public override int ShotCooldown => (int)(1.5f * 60f);
        public override int SentryShotCooldownReductionOnSpawn => 10;
        public override int ProjectileFrameCount => 6;
        public override int ProjectileWidth => 54;
        public override int ProjectileHeight => 58;
        public override DamageClass ProjectileDamageType => DamageClass.Summon;
        public override bool ContactDamage => false;
        public override bool CanShoot => true;
        public override int ShotProjectileType => ModContent.ProjectileType<GaleforceArrow>();
        public override float ProjectileInitialVelocity => 20f;
        public override bool PlaysSoundOnShot => true;
        public override SoundStyle ShootSoundStyle => new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GaleforceShot");
        public override float ShootSoundVolume => 0.2f; // was 0.6f
        public override bool SpawnsDust => true;
        public override int ProjectileDustID => DustID.Smoke;
        public override void CustomAI()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.rotation = Projectile.DirectionTo(Main.MouseWorld).ToRotation();
            }
        }
    }
}