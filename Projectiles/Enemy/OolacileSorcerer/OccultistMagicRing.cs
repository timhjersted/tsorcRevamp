using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.OolacileSorcerer
{
    /// <summary>
    /// The payload Star Cascade's electric ring bursts into. It is NOT a shot that appears already moving:
    /// it forms in place, spinning and fading up from nothing over 30 ticks, and only then launches along
    /// the direction it was given. Those 30 ticks are the player's read — while forming it deals no damage.
    /// ai[0] = launch speed (0 falls back to the default).
    /// Sprite: BlueMagicRing.png, 68x204 = three 68x68 frames.
    /// </summary>
    public class OccultistMagicRing : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/BlueMagicRing";

        private const int FormTicks = 30;
        private const int LifetimeTicks = FormTicks + 260;
        private const int DeathFadeTicks = 16;
        private const float DefaultLaunchSpeed = 8f;
        private const int TicksPerFrame = 6;

        /// <summary>Direction the ring launches in, parked in velocity at a tiny magnitude while it forms so
        /// it syncs to clients for free and the sprite can already lean the right way.</summary>
        private Vector2 LaunchDirection => Projectile.velocity.SafeNormalize(Vector2.UnitY);
        private float LaunchSpeed => Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultLaunchSpeed;
        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.light = 0.5f;
            Projectile.alpha = 255;
            Projectile.scale = 0.25f;
        }

        // Harmless while it is still forming — the fade-in IS the telegraph.
        public override bool? CanDamage() => Age < FormTicks ? false : null;

        public override void AI()
        {
            Age++;
            Projectile.rotation += 0.16f;

            if (Age <= FormTicks)
            {
                // Hold position and swell into existence. Velocity carries the aim at 1/100th magnitude so it
                // survives the trip to clients without moving the ring.
                float formProgress = Age / (float)FormTicks;
                Projectile.velocity = LaunchDirection * 0.01f;
                Projectile.alpha = (int)(255f * (1f - formProgress));
                Projectile.scale = MathHelper.Lerp(0.25f, 1f, formProgress);

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 orbit = Main.rand.NextVector2CircularEdge(30f, 30f) * formProgress;
                    Dust mote = Dust.NewDustPerfect(Projectile.Center + orbit, DustID.Electric,
                        -orbit * 0.05f, 80, default, 0.9f * formProgress + 0.3f);
                    mote.noGravity = true;
                }

                if (Age == FormTicks)
                {
                    Projectile.velocity = LaunchDirection * LaunchSpeed;
                    Projectile.alpha = 0;
                    Projectile.scale = 1f;
                    if (!Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item72 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.Center);
                    }
                }
                return;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Electric, 0f, 0f, 120, default, 0.8f);
                trail.noGravity = true;
                trail.velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.45f, 0.8f);

            // Timing out on screen fades instead of popping, and stops hurting while it dissipates.
            if (Projectile.timeLeft < DeathFadeTicks)
            {
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)DeathFadeTicks));
                Projectile.scale = Projectile.timeLeft / (float)DeathFadeTicks;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 3;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
            for (int i = 0; i < 70; i++)
            {
                Vector2 outward = Main.rand.NextVector2Circular(4.5f, 4.5f);
                Dust shard = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, outward, 60, default,
                    Main.rand.NextFloat(0.9f, 1.7f));
                shard.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 120);
        }
    }
}
