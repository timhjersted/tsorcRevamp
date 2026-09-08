using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Dread Wraith's Book of Skulls cast. A bespoke hostile projectile rather than vanilla
    /// ProjectileID.BookOfSkullsSkull, because that one is friendly, gravity-affected and dies on tile
    /// contact — fired from an NPC it just nosed into the floor a tile or two out. This version flies
    /// flat, ignores terrain, and homes gently so it pressures without being unavoidable.
    /// Borrows the vanilla skull sprite.
    /// </summary>
    public class DreadWraithSkull : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BookOfSkullsSkull;

        /// <summary>Ticks of straight flight before homing engages, so a point-blank cast can still be
        /// sidestepped instead of curving into the player's face immediately.</summary>
        private const int HomingDelayTicks = 16;
        /// <summary>Tick at which homing SHUTS OFF and the skull commits to a straight line. Without this
        /// the skulls never stop correcting and end up orbiting the player forever.</summary>
        private const int HomingEndTicks = 95;
        /// <summary>Below this distance homing also stops, so a skull that reaches the player flies THROUGH
        /// rather than circling back for another pass.</summary>
        private const float HomingMinDistance = 70f;
        /// <summary>Fraction of the way the velocity turns toward the target each tick. Deliberately weak —
        /// this is pressure, not a guided missile.</summary>
        private const float HomingStrength = 0.028f;
        private const float BaseSpeed = 5.4f;
        /// <summary>Ticks each of the 3 sprite frames is held.</summary>
        private const int FrameHoldTicks = 6;

        // Per-skull variation, assigned by the caster. Without these every skull in a volley computes the
        // identical homing solution and they collapse into a single stacked clump.
        private float SpeedMultiplier => Projectile.ai[0] == 0f ? 1f : Projectile.ai[0];
        private float AimOffsetRadians => Projectile.ai[1];

        /// <summary>The vanilla skull sprite is a 3-frame vertical strip. Without declaring that, the whole
        /// sheet draws at once — which is why every skull looked like three skulls stacked on top of
        /// each other.</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;   // the whole reason the vanilla skull failed here
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 260;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            // The skull FACE stays upright and level while it chases — it should read as a head looking at
            // you, not a tumbling rock. Animate through the 3-frame strip instead of rotating the sprite.
            Projectile.rotation = 0f;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= FrameHoldTicks)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.10f, 0.42f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.Shadowflame,
                    Projectile.velocity * -0.12f,
                    120,
                    default,
                    Main.rand.NextFloat(0.9f, 1.4f));
                trail.noGravity = true;
            }

            bool homingWindowOpen = Projectile.localAI[0] >= HomingDelayTicks
                && Projectile.localAI[0] <= HomingEndTicks;

            if (!homingWindowOpen)
            {
                return;
            }

            Player target = FindNearestPlayer();

            if (target == null)
            {
                return;
            }

            Vector2 toTarget = target.Center - Projectile.Center;

            // Close enough: commit and fly through. Continuing to steer here is what produced the
            // orbiting clump — the skull can never satisfy a target it is already on top of.
            if (toTarget.Length() < HomingMinDistance)
            {
                return;
            }

            // Each skull aims at its OWN offset angle off the player rather than dead centre, so a volley
            // stays spread out instead of every member converging on the same point.
            float skullTopSpeed = BaseSpeed * SpeedMultiplier;
            Vector2 desired = toTarget.SafeNormalize(Projectile.velocity).RotatedBy(AimOffsetRadians) * skullTopSpeed;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, HomingStrength);

            if (Projectile.velocity.Length() > skullTopSpeed)
            {
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * skullTopSpeed;
            }
        }

        private Player FindNearestPlayer()
        {
            Player closest = null;
            float closestDistance = 2000f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];

                if (!candidate.active || candidate.dead)
                {
                    continue;
                }

                float distance = Vector2.Distance(candidate.Center, Projectile.Center);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Shadowflame,
                    Main.rand.NextVector2Circular(3.5f, 3.5f),
                    100,
                    default,
                    Main.rand.NextFloat(1f, 1.7f));
                burst.noGravity = true;
            }
        }
    }
}
