using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Delayed homing bolt: the initial curve is followed by one distance-triggered return pass.
    // The return locks its heading within three tiles, then dissipates ten tiles later.
    //
    // ai[0] = ticks of straight travel before the curve begins. ai[1] = duration of the curve
    // window itself. Both are set per-spawn (not fixed constants) so multi-orb patterns can
    // stagger, synchronize, or otherwise control WHEN each individual orb bends.
    // ai[2] = outbound (0), returning (1), or committed final pass (2).
    class HomingAbyssOrb : ModProjectile
    {
        const float Speed = 8.5f;
        const float ReturnDistance = 50f * 16f;
        const float TrackingCutoff = 3f * 16f;
        const float FinalTravelDistance = 10f * 16f;
        const int ReturnLifetime = 6 * 60;
        const int FadeTicks = 12;
        int elapsedTicks;

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
            Projectile.tileCollide = false; // Deliberately cross terrain during the outbound and return passes.
            Projectile.ignoreWater = true;
            Projectile.light = 0.7f;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            }

            Animate();

            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];
            elapsedTicks++;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
                if (target.active && !target.dead)
                {
                    float targetDistance = Vector2.Distance(Projectile.Center, target.Center);
                    bool initialCurve = elapsedTicks > straightTicks && elapsedTicks <= straightTicks + curveTicks;

                    // Distance is relative to the player, independent of Artorias's arena. The timeout
                    // fallback also gives shots chased by the player their one final attempt.
                    if (Projectile.ai[2] == 0f && elapsedTicks > straightTicks + curveTicks
                        && (targetDistance >= ReturnDistance || Projectile.timeLeft <= 60))
                    {
                        Projectile.ai[2] = 1f;
                        Projectile.timeLeft = ReturnLifetime;
                        Projectile.netUpdate = true;
                    }

                    if (Projectile.ai[2] == 1f && targetDistance <= TrackingCutoff)
                    {
                        Projectile.ai[2] = 2f;
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * Speed;
                        Projectile.timeLeft = (int)System.Math.Ceiling(FinalTravelDistance / Speed);
                        Projectile.netUpdate = true;
                    }

                    if (Projectile.ai[2] == 1f)
                    {
                        // Rotate at fixed speed so reversing a nearly opposite heading cannot stall
                        // the orb, as a velocity lerp would. Only this final pass tracks continuously.
                        float currentAngle = Projectile.velocity.ToRotation();
                        float targetAngle = (target.Center - Projectile.Center).ToRotation();
                        float returnAngle = Utils.AngleTowards(currentAngle, targetAngle, 0.065f);
                        Projectile.velocity = returnAngle.ToRotationVector2() * Speed;
                    }
                    else if (Projectile.ai[2] == 0f && initialCurve)
                    {
                        Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Speed;
                        float curveProgress = (elapsedTicks - straightTicks) / curveTicks;
                        float steeringStrength = MathHelper.Lerp(0.06f, 0.30f, curveProgress);
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, steeringStrength);
                    }

                    if ((initialCurve || Projectile.ai[2] == 1f) && elapsedTicks % 6 == 0)
                    {
                        Projectile.netUpdate = true;
                    }
                }
            }

            Projectile.rotation += 0.15f;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Projectile.alpha = (int)(255f * (1f - fade));

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool white = Main.rand.NextBool(5);
                    Color tint = Color.DarkViolet;
                    if (white)
                    {
                        tint = Color.White;
                    }
                    else if (Main.rand.NextBool())
                    {
                        tint = new Color(190, 90, 255);
                    }

                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                        white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                        -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.18f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        90, tint, Main.rand.NextFloat(0.9f, 1.3f));
                    dust.noGravity = true;
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(elapsedTicks);
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            elapsedTicks = reader.ReadInt32();
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];
            float elapsed = elapsedTicks;
            float warning = 1f;
            if (straightTicks > 0f)
            {
                warning = MathHelper.Clamp((elapsed - (straightTicks - 10f)) / 10f, 0f, 1f);
            }

            float steering = MathHelper.Clamp((elapsed - straightTicks) / curveTicks, 0f, 1f);
            float state = System.Math.Max(warning * (1f - steering), steering);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
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
                            state, opacity * fade, width, length);
                    }
                }
            }
            Vector2 currentPerpendicular = new(-direction.Y, direction.X);
            float currentWave = (float)System.Math.Sin(waveTime) * 3.5f;
            ArtoriasVFX.DrawHomingFlameWisp(
                Projectile.Center - direction * 34f + currentPerpendicular * currentWave,
                direction, state, 0.88f * fade, 32f, 104f);
            ArtoriasVFX.DrawHomingVolleyOrb(Projectile.Center, new Vector2(48f, 48f), Projectile.rotation, state, 0.94f * fade);
            if (warning > 0f && steering <= 0f)
            {
                ArtoriasVFX.DrawTransitionFlash(Projectile.Center, Vector2.One * 62f,
                    warning, 0.72f * warning * fade);
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
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            return new Color(200, 140, 255, 220) * fade;
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
