using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Phoenix
{
    public class PhoenixBoomCrit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.extraUpdates = 100;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Summon;
        }
        public override void AI()
        {
            Projectile.Kill();
        }
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.4f, PitchVariance = 0.1f });
            Dust.NewDust(Projectile.Center, 100, 100, DustID.FlameBurst, 0f, 0f, 250, Color.DarkRed, 2.5f);
        }
    }
}