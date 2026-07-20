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
            Projectile.tileCollide = true;
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

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.position);
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f, 100, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 1.5f;
            }
        }
    }
}
