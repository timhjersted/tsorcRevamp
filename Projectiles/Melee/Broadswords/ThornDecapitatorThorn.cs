using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class ThornDecapitatorThorn : ModProjectile
    {
        public int Frames = 1;
        public int ProjectileLifetime = 360 * 2;
        public Vector2 CircularEdge;
        public int Timer = 0;
        public int CircleSize = 100;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.frame = Main.rand.Next(Frames);
            CircularEdge = Main.rand.NextVector2CircularEdge(CircleSize, CircleSize);
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.damage /= 2;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.velocity = (Projectile.DirectionTo(player.Center + CircularEdge) * 5) + player.velocity / 2;
            Timer++;
            if (Timer > 60 || Projectile.Center.Distance(player.Center + CircularEdge) < 5)
            {
                CircularEdge = Main.rand.NextVector2CircularEdge(CircleSize, CircleSize);
                Timer = 0;
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.4f, PitchVariance = 0.1f });
            Dust.NewDust(Projectile.Center, 100, 100, DustID.FlameBurst, 0f, 0f, 250, Color.DarkRed, 2.5f);
        }
    }
}