using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Spawn-point telegraph for Marik's Abyssal Curtain / Upheaval (Knight of Gwyn's shadow rows, moved
    /// onto Marik's staff). Dust-only and harmless: purple motes converge on the rift for ai[2] ticks, then the
    /// server fires one ShadowShot at the target player's position at that moment and the rift bursts.
    /// ai[0]: target player. ai[1]: ShadowShot launch speed (px/tick; ShadowShot accelerates 1%/tick to 10).
    /// ai[2]: tell ticks. Projectile.damage is handed to the ShadowShot.</summary>
    class CinderShadowRift : ModProjectile
    {
        // Intentional texture share: dust-only projectile, nothing is drawn (PreDraw returns false).
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        static readonly Color RiftPurple = new Color(170, 70, 255);

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            int tellTicks = (int)Projectile.ai[2];
            float progress = MathHelper.Clamp(Projectile.localAI[0] / System.Math.Max(1f, tellTicks), 0f, 1f);

            // Converging ring: 24px -> 4px, 1 mote per tick rising to 3, plus a late bright core.
            if (!Main.dedServ)
            {
                int moteCount = 1 + (int)(progress * 2f);
                for (int i = 0; i < moteCount; i++)
                {
                    float ringRadius = MathHelper.Lerp(24f, 4f, progress);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                    Vector2 inward = -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.6f, 2.2f, progress);
                    int dustType = i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch;
                    Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, dustType, inward, 90, RiftPurple,
                        MathHelper.Lerp(0.6f, 1.1f, progress));
                    mote.noGravity = true;
                }

                if (progress > 0.7f && Main.rand.NextBool(2))
                {
                    Dust core = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                        Main.rand.NextVector2Circular(0.4f, 0.4f), 120, Color.MediumPurple, 0.7f);
                    core.noGravity = true;
                }

                Lighting.AddLight(Projectile.Center, RiftPurple.ToVector3() * 0.35f * progress);
            }

            if (Projectile.localAI[0] < tellTicks)
            {
                return;
            }

            // Release: the server spawns the real shot aimed where the player is now, then the rift ends.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int targetIndex = (int)Projectile.ai[0];
                Vector2 aimPoint = Projectile.Center + Vector2.UnitY;
                if (targetIndex >= 0 && targetIndex < Main.maxPlayers && Main.player[targetIndex].active)
                {
                    aimPoint = Main.player[targetIndex].Center;
                }

                Vector2 velocity = UsefulFunctions.Aim(Projectile.Center, aimPoint, Projectile.ai[1]);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                    ModContent.ProjectileType<ShadowShot>(), Projectile.damage, 2f, Main.myPlayer);
            }

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.25f, Pitch = 0.2f, MaxInstances = 4 }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(2.6f, 2.6f);
                Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, velocity, 80, RiftPurple, 1.1f);
                burst.noGravity = true;
            }
        }
    }
}
