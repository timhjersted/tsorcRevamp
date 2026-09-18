

using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.OolacileSorcerer;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer
{
    /// <summary>
    /// Star Cascade's thrown electric ring. It does NOT fly at the player forever — it is aimed at an
    /// assigned point INSIDE the arena, brakes onto it, then hangs there spinning until it bursts into a
    /// fan of <see cref="OccultistMagicRing"/>. The old version flew straight through the floor with tile
    /// collision off and only detonated when its life ran out, usually underground and off screen.
    /// ai[0] = damage handed to each child ring. ai[1]/ai[2] = the world point to park on.
    /// Sprite: BlueElectricRing.png, 102x408 = four 102x102 frames.
    /// </summary>
    public class OccultistStarBig : ModProjectile
    {

        private const int TravelTicks = 70;
        private const int HoldTicks = 55;
        private const int Lifetime = TravelTicks + HoldTicks;
        private const int ChildRingCount = 5;
        private const float ChildLaunchSpeed = 8f;
        private const float ArriveRadius = 26f;
        private const float MaxTravelSpeed = 13f;
        private const int TicksPerFrame = 5;

        // The hitbox sits inside the drawn ring rather than spanning its full 102px, so the parked hazard
        // reads as "the ring band is dangerous" instead of claiming a circle it visibly does not fill.
        private const int HitboxSize = 64;

        private int ChildDamage => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : Projectile.damage;
        private Vector2 ParkTarget => new Vector2(Projectile.ai[1], Projectile.ai[2]);
        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false; // it must be able to cross terrain to reach its assigned spot
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime;
            Projectile.light = 0.7f;
            Projectile.scale = 1f;
        }

        public override void AI()
        {
            Age++;
            // Spins on its own axis the whole way, independent of travel, so a parked ring still reads as live.
            Projectile.rotation += 0.11f;

            bool parked = ParkTarget == Vector2.Zero;
            if (!parked)
            {
                Vector2 toTarget = ParkTarget - Projectile.Center;
                float distance = toTarget.Length();

                if (distance <= ArriveRadius || Age > TravelTicks)
                {
                    // Arrived (or ran out of travel budget): brake hard and hang here.
                    Projectile.velocity *= 0.82f;
                }
                else
                {
                    // Steer onto the spot, easing off as it closes so it settles rather than overshooting.
                    float approachSpeed = Math.Min(MaxTravelSpeed, distance * 0.14f + 2f);
                    Vector2 desired = toTarget.SafeNormalize(Vector2.Zero) * approachSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.18f);
                }
            }
            else
            {
                Projectile.velocity *= 0.97f;
            }

            // Last 20 ticks: swell and brighten. This is a bomb about to go off, and the growth is the
            // "get clear" read. Deliberately OUTSIDE the client guard — Projectile.scale feeds Hitbox, so
            // running it on clients only would give the server a smaller hazard than players can see.
            if (Projectile.timeLeft < 20)
            {
                Projectile.scale = 1f + (1f - Projectile.timeLeft / 20f) * 0.35f;
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 orbit = Main.rand.NextVector2CircularEdge(42f, 42f);
                    Dust mote = Dust.NewDustPerfect(Projectile.Center + orbit, DustID.Electric,
                        -orbit * 0.06f, 80, default, Main.rand.NextFloat(0.9f, 1.5f));
                    mote.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 0.25f, 0.55f, 0.95f);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = -0.2f }, Projectile.Center);
                UsefulFunctions.ScreenShake(Projectile.Center, 4f, 12, distanceFalloff: 700f);
                for (int i = 0; i < 70; i++)
                {
                    Vector2 outward = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.3f, 1f);
                    Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, outward, 40, default,
                        Main.rand.NextFloat(1.2f, 2.2f));
                    burst.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The children do not fly yet: each forms in place over 30 ticks before launching along this
            // direction, so the burst is telegraphed rather than instant.
            for (int i = 0; i < ChildRingCount; i++)
            {
                float angle = MathHelper.TwoPi * i / ChildRingCount;
                Vector2 direction = angle.ToRotationVector2();
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, direction * 0.01f,
                    ModContent.ProjectileType<OccultistMagicRing>(), ChildDamage, 1f, Main.myPlayer,
                    ChildLaunchSpeed);
            }
        }
    }
}
