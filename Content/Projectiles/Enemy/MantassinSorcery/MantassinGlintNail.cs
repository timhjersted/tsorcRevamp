using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.MantassinSorcery
{
    // The Mantassin's Glintstone Nail, after Elden Ring's finger sorcery (user's reference art, 2026-10-21): a dark slate
    // crystal head trailing a long silver-white streak. One projectile for all three nail spells.
    // Art: MantassinGlintNail.png, 10x26, 1 frame, TIP AT THE TOP (rotation = velocity angle + 90 degrees). Drawn under the
    //      MantassinNailGlint shader star; the trail is a MantassinNailTrail strip along the recorded path.
    // ai[0] = hang ticks (forms in place, harmless, pointing at its target).
    // ai[1] = max tile bounces + acceleration as the fraction (4.6 = 4 bounces, 0.6 px/t^2). Packed: ai[] is full.
    // ai[2] = target player index (tracks it while hanging, re-aimed at launch, homes in on bounces), or -1 = fixed aim.
    // Birth: converging silver motes + a mist puff + paired leaf glints while it forms over FormTicks.
    // Life: accelerates from rest to TopSpeed; trail 15px -> 200px with speed. Leaf glints shed along the trail.
    // Bounces: reflect, then blend toward the target by bounce / maxBounces (the last bounce aims straight at it),
    //          never back into the wall it hit; speed drops to 45% so each redirect starts slow and readable.
    // Death: tile (past its bounces) or timeout = harmless DyingTicks retract (trail shrinks to 15px, head shatters);
    //        player hit = vanilla kill, same shatter in OnKill.
    // MP: hang/launch/flight/reflect run on every peer from synced ai[] and velocity. The re-aim and the bounce homing are
    //     decided by the owner (the server) and pushed with netUpdate, the same split ArchdeaconGlintStar uses.
    public class MantassinGlintNail : ModProjectile
    {
        public const float TopSpeed = 16f;
        private const int FormTicks = 20;
        private const int LaunchFlareTicks = 10;
        private const int FlightTicks = 240;
        private const int DyingTicks = 12;
        private const float TrailMinLength = 15f;
        private const float TrailMaxLength = 200f;
        private const float TrailWidth = 10f;
        private const float BounceSpeedKeep = 0.45f;
        private const int TrailPointCount = 40;

        private int HangTicks => (int)Projectile.ai[0];
        private int MaxBounces => (int)Projectile.ai[1];
        private float Acceleration => Projectile.ai[1] - MaxBounces;
        private int TargetIndex => (int)Projectile.ai[2];

        // Local tick count since spawn. Every peer spawns the nail on the same tick, so they stay in step.
        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int Bounces
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        // 0 = alive, 1..DyingTicks = retracting.
        private int DyingTimer
        {
            get => (int)Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        private bool Launched => Age > HangTicks;

        // Client-only visual state: the recorded path (newest first) and the trail length when it started dying.
        private Vector2[] trailPoints;
        private int trailPointsFilled;
        private float dyingStartTrailLength;

        /// <summary>True while it is still forming/hanging (a poise break on the caster shatters these).</summary>
        public bool IsHanging => !Launched && DyingTimer == 0;

        /// <summary>Spawns a nail (server only) and returns its index, or -1. `aim` only needs a direction.</summary>
        public static int Spawn(IEntitySource source, Vector2 position, Vector2 aim, int damage, int hangTicks,
            int maxBounces, float acceleration, int targetPlayer)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            Vector2 direction = aim.SafeNormalize(Vector2.UnitX) * 0.01f;
            float packedBounces = maxBounces + MathHelper.Clamp(acceleration, 0.05f, 0.95f);
            return Projectile.NewProjectile(source, position, direction, ModContent.ProjectileType<MantassinGlintNail>(),
                damage, 0.5f, Main.myPlayer, hangTicks, packedBounces, targetPlayer);
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        // Harmless while forming/hanging and while retracting.
        public override bool? CanDamage()
        {
            if (!Launched || DyingTimer > 0)
            {
                return false;
            }

            return null;
        }

        // Hanging and dying nails hold still; their velocity is only a direction until launch.
        public override bool ShouldUpdatePosition() => Launched && DyingTimer == 0;

        public override void AI()
        {
            Age++;

            if (Age == 1)
            {
                // Lifetime is hang + flight + retract; set here so the spawner never has to touch timeLeft (not synced).
                Projectile.timeLeft = HangTicks + FlightTicks + DyingTicks + 2;
                if (!Main.dedServ)
                {
                    BirthPuff();
                }
            }

            RecordTrailPoint();

            if (DyingTimer > 0)
            {
                DyingTimer++;
                if (DyingTimer > DyingTicks)
                {
                    Projectile.Kill();
                }
                return;
            }

            // Flight timeout: retract instead of vanishing at full length.
            if (Projectile.timeLeft <= DyingTicks + 1)
            {
                StartDying();
                return;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Player target = TargetPlayer();

            if (!Launched)
            {
                // Hanging: point at the target. Every peer does this from its own copy of the player, so it is only the
                // look; the server's re-aim at launch is what counts.
                if (target != null)
                {
                    direction = (target.Center - Projectile.Center).SafeNormalize(direction);
                    Projectile.velocity = direction * 0.01f;
                }
                Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
                Lighting.AddLight(Projectile.Center, EnemyVFX.MantassinMid.ToVector3() * 0.35f);

                if (!Main.dedServ && Age <= FormTicks && Main.rand.NextBool(2))
                {
                    // Converging motes: born on a 26px ring, drifting into the head.
                    Vector2 offset = Main.rand.NextVector2CircularEdge(26f, 26f);
                    Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, DustID.TintableDustLighted,
                        -offset * 0.09f, 60, EnemyVFX.MantassinMid, 0.7f);
                    mote.noGravity = true;
                }
                return;
            }

            if (Age == HangTicks + 1)
            {
                Launch(direction, target);
                direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            }

            float speed = Math.Min(Projectile.velocity.Length() + Acceleration, TopSpeed);
            Projectile.velocity = direction * speed;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, EnemyVFX.MantassinMid.ToVector3() * 0.6f);

            if (!Main.dedServ)
            {
                // Leaf glints: every 6t a mirrored pair of tiny flecks peels off the trail behind the head (the little
                // paired buds in the reference art), drifting outward and back.
                if (Age % 6 == 0)
                {
                    Vector2 normal = new Vector2(-direction.Y, direction.X);
                    Vector2 budPoint = Projectile.Center - direction * 18f;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Dust bud = Dust.NewDustPerfect(budPoint + normal * side * 4f, DustID.TintableDustLighted,
                            normal * side * 0.9f - direction * 0.6f, 40, EnemyVFX.MantassinCore, 0.65f);
                        bud.noGravity = true;
                    }
                }

                if (Main.rand.NextBool(3))
                {
                    Dust mote = Dust.NewDustPerfect(Projectile.Center - direction * 6f + Main.rand.NextVector2Circular(3f, 3f),
                        DustID.TintableDustLighted, -direction * Main.rand.NextFloat(0.4f, 1.2f), 60, EnemyVFX.MantassinMid, 0.6f);
                    mote.noGravity = true;
                }
            }
        }

        // From rest: speed 0 + the first tick's Acceleration. Walls only matter once it moves.
        private void Launch(Vector2 direction, Player target)
        {
            Projectile.tileCollide = true;

            if (target != null && Projectile.owner == Main.myPlayer)
            {
                direction = (target.Center - Projectile.Center).SafeNormalize(direction);
                Projectile.netUpdate = true;
            }
            Projectile.velocity = direction * 0.01f;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.45f, Pitch = 0.4f, PitchVariance = 0.2f }, Projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    Dust puff = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(1.5f, 1.5f) - direction * 1.5f, 80, EnemyVFX.MantassinMid, 0.8f);
                    puff.noGravity = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (DyingTimer > 0)
            {
                return false;
            }

            if (Bounces >= MaxBounces)
            {
                StartDying();
                return false;
            }
            Bounces++;

            // Reflect off whichever axis the tile stopped. wallNormal points out of the wall, away from the surface hit.
            Vector2 reflected = oldVelocity;
            Vector2 wallNormal = Vector2.Zero;
            if (Projectile.velocity.X != oldVelocity.X)
            {
                reflected.X = -oldVelocity.X;
                wallNormal.X = Math.Sign(-oldVelocity.X);
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                reflected.Y = -oldVelocity.Y;
                wallNormal.Y = Math.Sign(-oldVelocity.Y);
            }
            Vector2 direction = reflected.SafeNormalize(-oldVelocity.SafeNormalize(Vector2.UnitY));

            // Homing: 25% / 50% / 75% / 100% toward the target over 4 bounces (bounce / maxBounces, so the last bounce
            // always aims straight at it). Rejected if the blend would point back into the wall it just hit.
            Player target = TargetPlayer();
            if (target != null && Projectile.owner == Main.myPlayer)
            {
                float homing = Bounces / (float)Math.Max(1, MaxBounces);
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(direction);
                Vector2 blended = Vector2.Lerp(direction, toTarget, homing).SafeNormalize(direction);
                Vector2 outward = wallNormal.SafeNormalize(Vector2.Zero);
                float awayFromWall = Vector2.Dot(blended, outward);

                if (outward == Vector2.Zero || awayFromWall > 0.15f)
                {
                    direction = blended;
                }
                Projectile.netUpdate = true;
            }

            float bounceSpeed = oldVelocity.Length() * BounceSpeedKeep;
            Projectile.velocity = direction * bounceSpeed;

            EnemyShaderBurst.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, EnemyVFXBurstKind.MantassinGlintFlash);
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.35f, Pitch = 0.6f + Bounces * 0.08f }, Projectile.Center);
                for (int i = 0; i < 10; i++)
                {
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                        (wallNormal + Main.rand.NextVector2Circular(1f, 1f)) * Main.rand.NextFloat(1f, 3f), 40, EnemyVFX.MantassinCore, 0.8f);
                    spark.noGravity = true;
                }
            }
            return false;
        }

        private void StartDying()
        {
            dyingStartTrailLength = CurrentTrailLength();
            DyingTimer = 1;
            Projectile.tileCollide = false;
            Shatter();
        }

        public override void OnKill(int timeLeft)
        {
            // A retracting nail already shattered when it started dying; only the player-hit kill needs it here.
            if (DyingTimer == 0)
            {
                Shatter();
            }
        }

        private void Shatter()
        {
            EnemyShaderBurst.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, EnemyVFXBurstKind.MantassinGlintFlash);
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f, Pitch = 0.5f, PitchVariance = 0.15f }, Projectile.Center);
            for (int i = 0; i < 40; i++)
            {
                Dust shard = Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond,
                    Main.rand.NextVector2Circular(3.5f, 3.5f), 60, default, 0.9f);
                shard.noGravity = true;
            }
            for (int i = 0; i < 24; i++)
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 40, EnemyVFX.MantassinMid, 0.9f);
                glint.noGravity = true;
            }
        }

        private Player TargetPlayer()
        {
            int index = TargetIndex;
            if (index < 0 || index >= Main.maxPlayers)
            {
                return null;
            }

            Player player = Main.player[index];
            if (!player.active || player.dead)
            {
                return null;
            }
            return player;
        }

        private void RecordTrailPoint()
        {
            if (Main.dedServ)
            {
                return;
            }
            trailPoints ??= new Vector2[TrailPointCount];

            for (int i = TrailPointCount - 1; i > 0; i--)
            {
                trailPoints[i] = trailPoints[i - 1];
            }
            trailPoints[0] = Projectile.Center;
            trailPointsFilled = Math.Min(trailPointsFilled + 1, TrailPointCount);
        }

        // 15px -> 200px with speed while flying; shrinks back to 15px over the retract.
        private float CurrentTrailLength()
        {
            if (!Launched)
            {
                return 0f;
            }

            if (DyingTimer > 0)
            {
                float retract = DyingTimer / (float)DyingTicks;
                return MathHelper.Lerp(dyingStartTrailLength, TrailMinLength, MathHelper.Clamp(retract, 0f, 1f));
            }

            float speedFraction = Projectile.velocity.Length() / TopSpeed;
            return MathHelper.Lerp(TrailMinLength, TrailMaxLength, MathHelper.Clamp(speedFraction, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float formFraction = MathHelper.Clamp(Age / (float)FormTicks, 0f, 1f);
            float opacity = formFraction;
            float headScale = formFraction;

            if (DyingTimer > 0)
            {
                float dyingFraction = DyingTimer / (float)DyingTicks;
                opacity = 1f - dyingFraction;
                headScale = 0f; // the head shattered when it started dying; only the trail retracts
            }

            DrawTrail(opacity);

            // Pre-launch flare over the last LaunchFlareTicks of the hang: "this one fires next".
            float flare = 0f;
            int ticksToLaunch = HangTicks - Age;
            if (!Launched && ticksToLaunch < LaunchFlareTicks)
            {
                flare = 1f - ticksToLaunch / (float)LaunchFlareTicks;
            }

            if (headScale > 0f)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                Vector2 origin = texture.Size() * 0.5f;
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White * opacity,
                    Projectile.rotation, origin, headScale, SpriteEffects.None, 0);
            }

            Vector2 forward = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
            EnemyVFX.DrawMantassinNailGlint(Projectile.Center + forward * 6f, 30f * Math.Max(headScale, 0.6f), opacity, flare);
            return false;
        }

        // Walks the recorded path from the head until the wanted trail length, clipping the last segment to fit.
        private void DrawTrail(float opacity)
        {
            float wantedLength = CurrentTrailLength();
            if (trailPoints == null || trailPointsFilled < 2 || wantedLength <= 1f)
            {
                return;
            }

            Vector2[] points = new Vector2[trailPointsFilled];
            points[0] = Projectile.Center;
            int count = 1;
            float travelled = 0f;

            for (int i = 1; i < trailPointsFilled; i++)
            {
                Vector2 previous = points[count - 1];
                float segment = Vector2.Distance(previous, trailPoints[i]);
                if (segment < 0.01f)
                {
                    continue;
                }

                if (travelled + segment >= wantedLength)
                {
                    float keep = (wantedLength - travelled) / segment;
                    points[count] = Vector2.Lerp(previous, trailPoints[i], keep);
                    count++;
                    break;
                }

                travelled += segment;
                points[count] = trailPoints[i];
                count++;
            }

            EnemyVFX.DrawMantassinNailTrail(points, count, TrailWidth, opacity);
        }

        private void BirthPuff()
        {
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.3f, Pitch = 0.2f, PitchVariance = 0.2f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust mist = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 120, EnemyVFX.MantassinMid, 1.0f);
                mist.noGravity = true;
            }

            // The reference art's paired leaf glints, sprouting either side of the forming head.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 velocity = new Vector2(side * Main.rand.NextFloat(0.6f, 1.2f), Main.rand.NextFloat(-0.8f, 0.2f));
                    Dust bud = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, velocity, 40, EnemyVFX.MantassinCore, 0.7f);
                    bud.noGravity = true;
                }
            }
        }
    }
}
