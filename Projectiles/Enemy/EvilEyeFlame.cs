using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EvilEyeFlame : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.6f;
        }

        // ai[0] == 1: weak homing for the projectile's first ~20 ticks (a "seeker" variant
        // EvilEye occasionally mixes into its Circle/HoverBob volleys), then flies straight.
        const float SeekTicks = 20f;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, 0.2f, 0.4f, 1f);

            if (Projectile.ai[0] == 1f && Projectile.timeLeft > 120f - SeekTicks)
            {
                int targetIndex = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                Vector2 toTarget = Main.player[targetIndex].Center - Projectile.Center;
                if (toTarget != Vector2.Zero)
                {
                    toTarget.Normalize();
                    Vector2 desiredVel = toTarget * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.06f);
                }
                int seekDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0f, 0f, 100, default, 1.2f);
                Main.dust[seekDust].noGravity = true;
                Main.dust[seekDust].velocity *= 0.2f;
            }

            // Continuous three-layer trail, emitted BEHIND the projectile along -velocity so it reads
            // as exhaust rather than a halo. Previously the only extra sparkle was the Electric dust
            // above, which is gated to the seeker variant's first 20 ticks - so the trail visibly
            // died partway through the flight.
            Vector2 back = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // Ambient: soft, slow, wide - sets the colour bed the sharper layers sit on.
            if (Main.rand.NextBool(2))
            {
                Dust ambient = Dust.NewDustPerfect(Projectile.Center - back * Main.rand.NextFloat(4f, 16f)
                    + Main.rand.NextVector2Circular(4f, 4f), DustID.BlueTorch,
                    -back * Main.rand.NextFloat(0.2f, 0.7f), 140, default, Main.rand.NextFloat(1.0f, 1.5f));
                ambient.noGravity = true;
            }

            // Midground: directional, drifts outward from the flight line.
            if (Main.rand.NextBool(3))
            {
                Vector2 spread = new Vector2(-back.Y, back.X) * Main.rand.NextFloat(-0.6f, 0.6f);
                Dust motion = Dust.NewDustPerfect(Projectile.Center - back * Main.rand.NextFloat(2f, 10f),
                    DustID.Electric, -back * Main.rand.NextFloat(0.6f, 1.4f) + spread, 120, default,
                    Main.rand.NextFloat(0.6f, 0.95f));
                motion.noGravity = true;
            }

            // Foreground: sparse, small, bright glints for crispness.
            if (Main.rand.NextBool(7))
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.IceTorch, -back * Main.rand.NextFloat(0.3f, 1.1f), 60, default,
                    Main.rand.NextFloat(0.45f, 0.7f));
                glint.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.EvilEyeFlameImpact);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.position);
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 1.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool seeking = Projectile.ai[0] == 1f && Projectile.timeLeft > 120f - SeekTicks;
            EnemyVFX.DrawEvilEyeFlame(Projectile.Center, Projectile.velocity, seeking);
            return true;
        }
    }
}
