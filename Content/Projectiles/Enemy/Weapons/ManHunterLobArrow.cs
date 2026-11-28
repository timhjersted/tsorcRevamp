using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Flaming arrow of a Man Hunter's lob volley (the Hero of Lumelia's artillery archers and, in Hardmode, wild Man
    /// Hunters). Two phases, both deterministic from the spawn data so every machine agrees without syncing positions:
    ///   1. Climb: the archer looses it nearly straight up; it flies at constant speed for AscendTicks and leaves the screen.
    ///   2. Descent: it reappears FallHeight above its landing point and falls onto it at constant speed, either straight down
    ///      or at 45 degrees (arriving from the upper left or upper right), so it always comes down from above.
    /// ai[0] / ai[1] = the landing point (world px). ai[2] = descent direction: -1 down-left, 0 straight down, +1 down-right.
    /// The re-entry is deliberate: a real arc at 100 tiles peaks near 25 tiles and lands at a shallow angle, never from above.
    /// </summary>
    public class ManHunterLobArrow : ModProjectile
    {
        // Intentional permanent texture reuse of the vanilla flaming arrow sprite (the Man Hunter's own arrow).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlamingArrow;

        // Climb: px per tick along the launch direction, and how long it lasts (60t x 25px = 1500px of path; at the ~8.5
        // degree lean that is ~1483px = 92.7 tiles of height, so it clears the 90 tiles of sky the archer requires).
        public const float AscendSpeed = 25f;
        public const int AscendTicks = 60;
        // Descent: vertical px per tick (a 45-degree arrow also moves this fast sideways) and its duration. 67t x 15px = 1005px,
        // more than half a 1440p screen above the target, so the arrow enters from above the top edge rather than popping in.
        public const float FallSpeed = 15f;
        public const int FallTicks = 67;
        public const float FallHeight = FallSpeed * FallTicks;
        /// <summary>Ticks from release to impact when nothing is hit on the way.</summary>
        public const int TotalFlightTicks = AscendTicks + FallTicks;

        // Ticks of life left after the intended impact, in case the arrow falls past its landing point over a pit.
        private const int OvershootTicks = 150;
        // The last ticks before timeLeft runs out fade the arrow instead of popping it.
        private const int FadeOutTicks = 15;
        // Reticle ring radius at the landing point (px). Flat ellipse: a ground marker, not a sphere.
        private const float ReticleRadiusX = 26f;
        private const float ReticleRadiusY = 5f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            // localAI[1] counts ticks since spawn (localAI is per-machine; every machine ticks its own copy identically).
            Projectile.localAI[1] += 1f;
            float age = Projectile.localAI[1];

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = TotalFlightTicks + OvershootTicks;
            }

            Vector2 landing = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            // Re-entry: jump to the top of the descent line and take its velocity. The start is placed so that after exactly
            // FallTicks of movement the arrow is on the landing point (start = landing - direction * velocity * FallTicks).
            if (age == AscendTicks)
            {
                int direction = (int)Projectile.ai[2];
                Projectile.Center = landing + new Vector2(-direction * FallHeight, -FallHeight);
                Projectile.velocity = new Vector2(direction * FallSpeed, FallSpeed);
            }

            // Sprite tip leads: the vanilla arrow art points up, so rotate by a quarter turn from the velocity heading.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.timeLeft < FadeOutTicks)
            {
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)FadeOutTicks));
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0.28f, 0.06f);

            // The re-entry tick teleports the arrow, so no trail dust on it (it would draw a streak across the sky).
            if (age != AscendTicks && Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                    Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 100, default, 1.1f);
                ember.noGravity = true;
                ember.velocity *= 0.4f;
            }

            // Landing reticle: a flat ring of embers at the landing point that thickens as the arrow comes down. Dust only,
            // so it needs no draw code.
            float progress = MathHelper.Clamp(age / TotalFlightTicks, 0f, 1f);
            float spawnChance = MathHelper.Lerp(0.25f, 0.9f, progress);

            if (progress < 1f && Main.rand.NextFloat() < spawnChance)
            {
                Vector2 ringOffset = Main.rand.NextVector2CircularEdge(ReticleRadiusX, ReticleRadiusY);
                // 50% transparent ground telegraph: DustID.Torch ignores dust.alpha in GetAlpha (returns A=25 with RGB),
                // so halving RGB via Color(128, 128, 128) is what actually renders it at 50% intensity.
                Dust reticleDust = Dust.NewDustPerfect(landing + ringOffset, DustID.Torch, new Vector2(0f, -0.3f), 128, new Color(128, 128, 128), 1.2f); // dust-lint: ok
                reticleDust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Matches the Man Hunter's direct flaming arrow (vanilla hostile type-82 burn): 1-in-3 for 7s, rolled on the
            // hit player's machine.
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire, 420);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Every death path (tile, player, timeout) lands here: a burst of embers and the tile-hit puff.
            if (!Main.dedServ)
            {
                for (int i = 0; i < 24; i++)
                {
                    Dust burst = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                        Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-3f, 0.5f), 100, default, 1.3f);
                    burst.noGravity = true;
                }
            }

            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }
    }
}
