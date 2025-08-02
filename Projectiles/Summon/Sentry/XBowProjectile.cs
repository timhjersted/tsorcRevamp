using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Sentry
{
    public class XBowProjectile : FloatingSentryProjectile
    {
        public override int ShotCooldown => (int)(0.25f * 60f);
        public override int SentryShotCooldownReductionOnSpawn => 10;
        public override int ProjectileFrameCount => 2;
        public override int ProjectileWidth => 62;
        public override int ProjectileHeight => 66;
        public override DamageClass ProjectileDamageType => DamageClass.Summon;
        public override bool ContactDamage => false;
        public override bool CanShoot
        {
            //Only fire if at least one enemy is within line of sight!
            get
            {
                for(int i = 0; i < Main.npc.Length; i++)
                {
                    if (Main.npc[i].active && Collision.CanHitLine(Projectile.Center, 1, 1, Main.npc[i].Center, 1, 1))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public override int ShotProjectileType => ModContent.ProjectileType<XBowArrow>();
        public override float ProjectileInitialVelocity => 30f;
        public override bool PlaysSoundOnShot => true;
        public override SoundStyle ShootSoundStyle => SoundID.Item99;
        public override float ShootSoundVolume => 0.50f; // was 0.6f
        public override bool SpawnsDust => false;
        public override int ProjectileDustID => 54;
        public override void CustomAI()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.rotation = Projectile.DirectionTo(Main.MouseWorld).ToRotation();
            }
        }
    }
}