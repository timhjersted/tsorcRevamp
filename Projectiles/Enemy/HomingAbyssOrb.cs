using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // The "delayed homing bolt": flies dead straight on its initial heading, then over a short
    // curve window bends onto a heading toward the player's CURRENT position (not where they were
    // at launch) and continues on from there - looks like it's going to miss, then bends.
    //
    // ai[0] = ticks of straight travel before the curve begins. ai[1] = duration of the curve
    // window itself. Both are set per-spawn (not fixed constants) so multi-orb patterns can
    // stagger, synchronize, or otherwise control WHEN each individual orb bends.
    class HomingAbyssOrb : ModProjectile
    {
        const float Speed = 8.5f;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/HomingAbyssOrb";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.7f;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            Animate();

            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];

            Projectile.localAI[0]++;
            float elapsed = Projectile.localAI[0];

            if (elapsed > straightTicks && elapsed <= straightTicks + curveTicks)
            {
                Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
                if (target.active && !target.dead)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Speed;
                    float curveT = (elapsed - straightTicks) / curveTicks;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.06f, 0.30f, curveT));
                }
            }

            Projectile.rotation += 0.15f;

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool white = Main.rand.NextBool(5);
                    Color tint = white ? Color.White : (Main.rand.NextBool() ? new Color(190, 90, 255) : Color.DarkViolet);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                        white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                        -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.18f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        90, tint, Main.rand.NextFloat(0.9f, 1.3f));
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];
            float elapsed = Projectile.localAI[0];
            float warning = straightTicks > 0f
                ? MathHelper.Clamp((elapsed - (straightTicks - 10f)) / 10f, 0f, 1f)
                : 1f;
            float steering = MathHelper.Clamp((elapsed - straightTicks) / curveTicks, 0f, 1f);
            float state = System.Math.Max(warning * (1f - steering), steering);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float waveTime = Main.GlobalTimeWrappedHourly * 8.4f + Projectile.identity * 0.63f;
            if (Projectile.oldPos[18] != Vector2.Zero)
            {
                for (int i = 18; i >= 4; i -= 4)
                {
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    Vector2 newerCenter = Projectile.oldPos[i - 2] + Projectile.Size * 0.5f;
                    Vector2 oldDirection = (newerCenter - oldCenter).SafeNormalize(direction);
                    if (Vector2.DistanceSquared(oldCenter, newerCenter) > 4f)
                    {
                        float age = i / 19f;
                        float freshness = 1f - age;
                        Vector2 perpendicular = new(-oldDirection.Y, oldDirection.X);
                        float lateralWave = (float)System.Math.Sin(waveTime - i * 0.72f)
                            * MathHelper.Lerp(10f, 2.5f, freshness);
                        float width = MathHelper.Lerp(9f, 28f, freshness);
                        float length = MathHelper.Lerp(28f, 86f, freshness);
                        float opacity = (float)System.Math.Pow(freshness, 1.35f) * 0.44f;
                        Vector2 wispCenter = oldCenter - oldDirection * (length * 0.25f)
                            + perpendicular * lateralWave;
                        ArtoriasVFX.DrawHomingFlameWisp(wispCenter, oldDirection,
                            state, opacity, width, length);
                    }
                }
            }
            Vector2 currentPerpendicular = new(-direction.Y, direction.X);
            float currentWave = (float)System.Math.Sin(waveTime) * 3.5f;
            ArtoriasVFX.DrawHomingFlameWisp(
                Projectile.Center - direction * 34f + currentPerpendicular * currentWave,
                direction, state, 0.88f, 32f, 104f);
            ArtoriasVFX.DrawOrb(Projectile.Center, new Vector2(48f, 48f), Projectile.rotation, state, 0.94f);
            if (warning > 0f && steering <= 0f)
            {
                ArtoriasVFX.DrawTransitionFlash(Projectile.Center, Vector2.One * 62f,
                    warning, 0.72f * warning);
            }
            return true;
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 220);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, tint, 1f);
                d.noGravity = true;
            }
        }
    }
}
