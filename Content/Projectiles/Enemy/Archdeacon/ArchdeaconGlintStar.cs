using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;

namespace tsorcRevamp.Content.Projectiles.Enemy.Archdeacon
{
    // The Archdeacon's glintstone star: every star pattern (Phalanx, Starfall, Prison, orb rings, Comet) is this projectile.
    // Art: ArchdeaconGlintStar.png is a copy of IlluminantHomingStar (34x90 blue comet, round head at the BOTTOM, tail on
    // top), so the bottom edge leads. While hanging it is drawn as a twinkling HomingStarStar glint instead.
    // Birth: blooms in a pale-blue mist puff and hangs for HangTicks (ai[0]) pointing along its velocity, harmless.
    // Life: launches from rest and gains Acceleration (ai[2]) px/t every tick up to its top speed. Starting at 0 is the
    //       fairness rule: at 0.2 it covers only 40px in its first 20t, so even stars that form near the player are readable.
    // Death: tile or player = sparkle burst; FlightTicks after launch it fades out over FadeTicks.
    // MP: hang/launch/flight run identically on every peer from synced ai[]. Re-aim and homing are decided by the owner
    // (the server) and pushed with netUpdate.
    public class ArchdeaconGlintStar : ModProjectile
    {
        public enum Mode
        {
            // Flies along the direction it was spawned with.
            Fixed = 0,
            // Re-aims at the nearest player the tick it launches (Phalanx).
            ReaimAtLaunch = 1,
            // Big comet: re-aims at launch and steers toward the nearest player for its first CometHomingTicks.
            Comet = 2,
        }

        public const float StarTopSpeed = 10f;
        public const float CometTopSpeed = 12f;
        private const int FlightTicks = 150;
        private const int FadeTicks = 15;
        private const int CometHomingTicks = 30;
        private const float CometTurnRadiansPerTick = 0.035f;

        public static readonly Color GlintColor = new Color(130, 200, 255);
        public static readonly Color GlintCore = new Color(225, 245, 255);

        private int HangTicks => (int)Projectile.ai[0];
        private Mode StarMode => (Mode)(int)Projectile.ai[1];
        private float Acceleration => Projectile.ai[2];
        private bool IsComet => StarMode == Mode.Comet;

        // Local tick count since spawn. Every peer spawns the star on the same tick, so they stay in step.
        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private bool Launched => Age > HangTicks;
        private int FlightAge => Age - HangTicks;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        // Harmless while hanging and while fading.
        public override bool? CanDamage()
        {
            if (!Launched || Projectile.timeLeft <= FadeTicks)
            {
                return false;
            }

            return null;
        }

        // Hanging stars hold still; their velocity is only a direction until launch.
        public override bool ShouldUpdatePosition() => Launched;

        public override void AI()
        {
            Age++;

            if (Age == 1)
            {
                // Lifetime is hang + flight; set here so the spawner never has to touch timeLeft (not synced).
                Projectile.timeLeft = HangTicks + FlightTicks + FadeTicks;

                if (!Main.dedServ)
                {
                    MistPuff(10);
                }
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            if (!Launched)
            {
                Projectile.rotation = direction.ToRotation() - MathHelper.PiOver2;
                Lighting.AddLight(Projectile.Center, GlintColor.ToVector3() * 0.35f);

                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    Dust twinkle = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.TintableDustLighted,
                        Vector2.Zero, 60, GlintColor, 0.7f);
                    twinkle.noGravity = true;
                }

                return;
            }

            if (FlightAge == 1)
            {
                Launch(direction);
                direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            }

            // Comet homing: owner-only steering toward the nearest player, re-sent every 6t.
            bool homing = IsComet && FlightAge <= CometHomingTicks;
            if (homing && Projectile.owner == Main.myPlayer)
            {
                Player target = NearestPlayer();
                if (target != null)
                {
                    float wanted = (target.Center - Projectile.Center).ToRotation();
                    float current = direction.ToRotation();
                    float turned = current.AngleTowards(wanted, CometTurnRadiansPerTick);
                    direction = turned.ToRotationVector2();

                    if (FlightAge % 6 == 0)
                    {
                        Projectile.netUpdate = true;
                    }
                }
            }

            float topSpeed = StarTopSpeed;
            if (IsComet)
            {
                topSpeed = CometTopSpeed;
            }

            float speed = Math.Min(Projectile.velocity.Length() + Acceleration, topSpeed);
            Projectile.velocity = direction * speed;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / (float)FadeTicks));
            }

            Lighting.AddLight(Projectile.Center, GlintColor.ToVector3() * 0.6f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust sparkle = Dust.NewDustPerfect(Projectile.Center - direction * 8f + Main.rand.NextVector2Circular(4f, 4f), DustID.TintableDustLighted,
                    -direction * Main.rand.NextFloat(0.5f, 1.5f), 40, GlintCore, 0.8f);
                sparkle.noGravity = true;
            }
        }

        // From rest: speed 0 + the first tick's Acceleration. Walls only matter once it moves.
        private void Launch(Vector2 direction)
        {
            Projectile.tileCollide = true;

            bool reaims = StarMode == Mode.ReaimAtLaunch || IsComet;
            if (reaims && Projectile.owner == Main.myPlayer)
            {
                Player target = NearestPlayer();
                if (target != null)
                {
                    direction = (target.Center - Projectile.Center).SafeNormalize(direction);
                    Projectile.netUpdate = true;
                }
            }

            Projectile.velocity = direction * 0.01f;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.45f, Pitch = 0.3f, PitchVariance = 0.2f }, Projectile.Center);
                MistPuff(6);
            }
        }

        // Every Archdeacon attack applies both debuffs for HitDebuffNormalTicks (4s) in Normal mode.
        public const int HitDebuffNormalTicks = 4 * 60;

        // Shared on-hit debuffs for every Archdeacon spell. Neither is a vanilla LongerExpertDebuff, so AddBuff would not
        // stretch them; Crippled.ScaledTicks applies the same mode multiplier vanilla uses (x1 Normal, x2 Expert, x2.5 Master).
        // Runs on the hit player's machine; AddBuff there syncs like any other debuff.
        public static void ApplyHitDebuffs(Player target)
        {
            int ticks = Crippled.ScaledTicks(HitDebuffNormalTicks);
            target.AddBuff(BuffID.ManaSickness, ticks);
            target.AddBuff(BuffID.WitheredArmor, ticks);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            ApplyHitDebuffs(target);
        }

        // A star needs a 16x16 pocket of air to bloom in, or it bursts on its first flying tick.
        public static bool SpotIsOpen(Vector2 point)
        {
            Vector2 topLeft = point - new Vector2(8f);
            bool insideWorld = topLeft.X > 32f && topLeft.Y > 32f && point.X < Main.maxTilesX * 16f - 32f && point.Y < Main.maxTilesY * 16f - 32f;

            return insideWorld && !Collision.SolidCollision(topLeft, 16, 16);
        }

        // Server-side placement for aimed stars: open air, a clear line to aimPoint, and at least minPlayerDistance from
        // every player (the from-rest launch needs room to be readable). Tries the planned spot, then rings of 24/48px
        // around it, then pulling it up to 30% of the way toward aimPoint. False = no fair spot: skip that star.
        public static bool TryFindSpawnSpot(Vector2 desired, Vector2 aimPoint, float minPlayerDistance, out Vector2 spot)
        {
            const int RingDirections = 8;
            float[] ringRadii = { 0f, 24f, 48f };

            foreach (float radius in ringRadii)
            {
                int directions = 1;
                if (radius > 0f)
                {
                    directions = RingDirections;
                }

                for (int i = 0; i < directions; i++)
                {
                    Vector2 candidate = desired + (MathHelper.TwoPi * i / directions).ToRotationVector2() * radius;
                    if (SpotIsFair(candidate, aimPoint, minPlayerDistance))
                    {
                        spot = candidate;
                        return true;
                    }
                }
            }

            for (int step = 1; step <= 3; step++)
            {
                Vector2 candidate = Vector2.Lerp(desired, aimPoint, step * 0.1f);
                if (SpotIsFair(candidate, aimPoint, minPlayerDistance))
                {
                    spot = candidate;
                    return true;
                }
            }

            spot = desired;
            return false;
        }

        private static bool SpotIsFair(Vector2 point, Vector2 aimPoint, float minPlayerDistance)
        {
            if (!SpotIsOpen(point) || !Collision.CanHitLine(point, 1, 1, aimPoint, 1, 1))
            {
                return false;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Vector2.Distance(point, player.Center) < minPlayerDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private Player NearestPlayer()
        {
            Player nearest = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                float distance = Vector2.DistanceSquared(player.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = player;
                }
            }

            return nearest;
        }

        // Soft pale-blue smoke around the star, like glintstone conjured out of mist.
        private void MistPuff(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust mist = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f), DustID.Smoke,
                    Main.rand.NextVector2Circular(0.8f, 0.8f), 120, new Color(170, 210, 255), Main.rand.NextFloat(1.2f, 1.8f));
                mist.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = 1f - Projectile.alpha / 255f;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            // Alpha 0 = additive glow.
            Color glow = new Color(GlintCore.R, GlintCore.G, GlintCore.B, 0) * opacity;

            if (!Launched)
            {
                // Twinkling glint that grows in over 12t, flaring over the last 8t before launch.
                Texture2D starTexture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar))).Value;
                float growth = MathHelper.Clamp(Age / 12f, 0f, 1f);
                float twinkle = 0.85f + 0.15f * (float)Math.Sin(Age * 0.5f);
                bool flaring = HangTicks - Age < 8;
                float flare = 1f;
                if (flaring)
                {
                    flare = 1.5f;
                }

                float starScale = growth * twinkle * flare * 0.8f;
                if (IsComet)
                {
                    starScale *= 1.5f;
                }

                Main.EntitySpriteDraw(starTexture, drawCenter, null, glow, Age * 0.08f, starTexture.Size() * 0.5f, starScale, SpriteEffects.None, 0);
                return false;
            }

            // Comet: the round head (≈ y 75 of 90) sits on the hitbox, the tail streams behind.
            Texture2D cometTexture = TextureAssets.Projectile[Type].Value;
            Vector2 headOrigin = new Vector2(cometTexture.Width * 0.5f, cometTexture.Height * 0.83f);
            float cometScale = 0.6f;
            if (IsComet)
            {
                cometScale = 1f;
            }

            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                {
                    continue;
                }

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 afterimage = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(cometTexture, afterimage, null, glow * (fade * 0.35f), Projectile.rotation, headOrigin, cometScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(cometTexture, drawCenter, null, glow, Projectile.rotation, headOrigin, cometScale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            bool fadedOut = timeLeft <= 0;
            if (fadedOut || Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = 0.4f }, Projectile.Center);

            int burstCount = 18;
            if (IsComet)
            {
                burstCount = 36;
            }

            for (int i = 0; i < burstCount; i++)
            {
                Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Circular(4f, 4f), 30, GlintCore, 1.1f);
                burst.noGravity = true;
            }

            MistPuff(5);
        }
    }
}
