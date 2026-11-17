using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Delayed fire brand placed around a player by Marik's staff rituals.
    /// ai[0]: target player. ai[1]: 0 constellation, 1 cross, 2 orbit. ai[2]: brand index in the pattern.
    /// Spawn velocity carries the brand's angle around the anchor (synced, unlike rotation) and is zeroed on tick 1.
    /// Lifecycle: pattern-line dust tell -> brand forms at its point -> charges facing the player -> released at
    /// the player -> dies on a tile or the player with a 70-dust burst (OnKill).</summary>
    class CinderRemoteFlame : ModProjectile
    {
        const int HoldTicks = 30;
        const int ConstellationBirthTellTicks = 40;
        const int ConstellationVisibleHoldTicks = 24;
        const int FirelinkBirthTellTicks = 40;
        const int FirelinkVisibleHoldTicks = 20;
        const int AshenBirthTellTicks = 24;
        const int AshenAppearanceStepTicks = 8;
        const int AshenPostAppearancePauseTicks = 36;

        // Pattern geometry, shared with Marik's spawner so the dust outline and the brands always agree.
        public const int ConstellationCount = 7;
        public const float ConstellationRadius = 190f;
        public const int FirelinkCount = 4;
        public const float FirelinkRadius = 206.25f;
        public const int AshenCount = 6;
        public const float AshenRadius = 440f;

        // Vanilla CultistBossFireBall sheet: 4 vertical frames, flame head at the TOP of each frame,
        // so the sprite leads with rotation = travel angle + 90°. Vanilla advances a frame every ~1.5 ticks.
        const int FireballFrameCount = 4;
        const int FireballTicksPerFrame = 3;
        const float SpriteLeadOffset = MathHelper.PiOver2;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CultistBossFireBall;

        public static int CountForMode(int mode)
        {
            if (mode == 0)
            {
                return ConstellationCount;
            }
            if (mode == 1)
            {
                return FirelinkCount;
            }
            return AshenCount;
        }

        public static float RadiusForMode(int mode)
        {
            if (mode == 0)
            {
                return ConstellationRadius;
            }
            if (mode == 1)
            {
                return FirelinkRadius;
            }
            return AshenRadius;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FireballFrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ReleaseTick((int)Projectile.ai[1]) ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;

            // Tick 1: read the pattern angle out of the synced spawn velocity, then hold still.
            if (Projectile.localAI[0] == 1)
            {
                Projectile.localAI[1] = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
            }

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Player target = Main.player[targetIndex];
            int mode = (int)Projectile.ai[1];
            float baseAngle = Projectile.localAI[1];
            int birthTick = BirthTick(mode);
            int releaseTick = ReleaseTick(mode);

            if (Projectile.localAI[0] < birthTick)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 255;
                Projectile.scale = 0f;
                SpawnPatternTelegraph(mode, baseAngle, birthTick);
                SpawnBirthTelegraph(mode, birthTick);
            }
            else if (Projectile.localAI[0] <= releaseTick)
            {
                Projectile.velocity = Vector2.Zero;
                float visibleTicks = Projectile.localAI[0] - birthTick + 1f;
                Projectile.alpha = Math.Max(0, 255 - (int)(visibleTicks * 42f));
                Projectile.scale = MathHelper.Lerp(0.20f, 1f, MathHelper.Clamp(visibleTicks / 10f, 0f, 1f));
                ChargeVFX(mode, baseAngle, target, birthTick, releaseTick);

                // Charging brands aim their flame head at the player: the release direction is readable before it fires.
                Vector2 toTarget = target.Center - Projectile.Center;
                Projectile.rotation = toTarget.ToRotation() + SpriteLeadOffset;
            }
            else if (Projectile.localAI[0] == releaseTick + 1)
            {
                float releaseSpeed = 7.5f;
                if (mode == 1)
                {
                    releaseSpeed = 9f;
                }
                else if (mode == 2)
                {
                    releaseSpeed = 11f;
                }

                Vector2 releaseDirection = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = releaseDirection * releaseSpeed;
                Projectile.tileCollide = true;
                ReleaseVFX(mode, baseAngle);

                float releasePitch = 0.3f;
                if (mode == 1)
                {
                    releasePitch = -0.15f;
                }
                else if (mode == 2)
                {
                    releasePitch = 0.1f;
                }
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.35f, Pitch = releasePitch }, Projectile.Center);
            }

            if (Projectile.localAI[0] > releaseTick)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + SpriteLeadOffset;
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        -Projectile.velocity * 0.07f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        90, Color.Gold, 0.8f);
                    dust.noGravity = true;
                }
            }

            // Animate the 4-frame sheet whenever the brand is visible (the old code drew the whole sheet as one frame).
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FireballTicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FireballFrameCount;
            }

            Color lightColor = new Color(1f, 0.68f, 0.22f);
            if (mode == 1)
            {
                lightColor = new Color(1f, 0.16f, 0.04f);
            }
            else if (mode == 2)
            {
                lightColor = new Color(1f, 0.42f, 0.08f);
            }
            Lighting.AddLight(Projectile.Center, lightColor.ToVector3() * Projectile.scale * 0.4f);
        }

        int BirthTick(int mode)
        {
            if (mode == 0)
            {
                return ConstellationBirthTellTicks + 1;
            }
            if (mode == 1)
            {
                return FirelinkBirthTellTicks + 1;
            }
            if (mode == 2)
            {
                return AshenBirthTellTicks + (int)Projectile.ai[2] * AshenAppearanceStepTicks;
            }
            return 1;
        }

        static int ReleaseTick(int mode)
        {
            if (mode == 0)
            {
                return ConstellationBirthTellTicks + 1 + ConstellationVisibleHoldTicks;
            }
            if (mode == 1)
            {
                return FirelinkBirthTellTicks + 1 + FirelinkVisibleHoldTicks;
            }
            if (mode == 2)
            {
                return AshenBirthTellTicks + (AshenCount - 1) * AshenAppearanceStepTicks + AshenPostAppearancePauseTicks;
            }
            return HoldTicks;
        }

        /// <summary>Draws this brand's share of the whole pattern in dust before any brand exists, so the player
        /// reads the shape first: constellation = a 7-point star (edge to the vertex two steps on), cross = a spoke
        /// into the anchor (the four spokes meet on the player), orbit = the ring arc to the next brand.
        /// The anchor is where the player stood at spawn: this brand's point minus its angle x radius.</summary>
        void SpawnPatternTelegraph(int mode, float baseAngle, int birthTick)
        {
            if (Main.dedServ)
            {
                return;
            }

            int count = CountForMode(mode);
            float radius = RadiusForMode(mode);
            Vector2 anchor = Projectile.Center - baseAngle.ToRotationVector2() * radius;
            float progress = MathHelper.Clamp(Projectile.localAI[0] / Math.Max(1f, birthTick), 0f, 1f);
            float stepAngle = MathHelper.TwoPi / count;

            // Sparse early, denser as the brands approach: 1 mote per brand every other tick, then every tick.
            bool emitThisTick = progress > 0.5f || ((int)Projectile.localAI[0] % 2) == 0;
            if (!emitThisTick)
            {
                return;
            }

            float along = Main.rand.NextFloat();
            Vector2 motePosition;
            Vector2 moteVelocity;
            int dustType;
            Color dustColor;

            if (mode == 0)
            {
                Vector2 starPoint = anchor + (baseAngle + stepAngle * 2f).ToRotationVector2() * radius;
                Vector2 edge = starPoint - Projectile.Center;
                motePosition = Projectile.Center + edge * along;
                moteVelocity = edge.SafeNormalize(Vector2.Zero) * 0.6f;
                dustType = DustID.GoldFlame;
                dustColor = Color.Gold;
            }
            else if (mode == 1)
            {
                Vector2 spoke = anchor - Projectile.Center;
                motePosition = Projectile.Center + spoke * along;
                moteVelocity = spoke.SafeNormalize(Vector2.Zero) * 0.9f;
                dustType = DustID.RedTorch;
                dustColor = Color.OrangeRed;
            }
            else
            {
                // The orbit ring follows the player live, matching how the orbit brands track them.
                Vector2 ringCenter = Main.player[(int)Projectile.ai[0]].Center;
                float arcAngle = baseAngle + stepAngle * along;
                motePosition = ringCenter + arcAngle.ToRotationVector2() * radius;
                moteVelocity = (arcAngle + MathHelper.PiOver2).ToRotationVector2() * 0.7f;
                dustType = DustID.OrangeTorch;
                dustColor = Color.Orange;
            }

            float moteScale = MathHelper.Lerp(0.55f, 1.05f, progress);
            Dust mote = Dust.NewDustPerfect(motePosition, dustType, moteVelocity, 90, dustColor, moteScale);
            mote.noGravity = true;
        }

        void SpawnBirthTelegraph(int mode, int birthTick)
        {
            if (Main.dedServ)
            {
                return;
            }

            int tellTicks = AshenBirthTellTicks;
            if (mode == 0)
            {
                tellTicks = ConstellationBirthTellTicks;
            }
            else if (mode == 1)
            {
                tellTicks = FirelinkBirthTellTicks;
            }

            float tellElapsed = Projectile.localAI[0] - (birthTick - tellTicks);
            if (tellElapsed < 0f)
            {
                return;
            }

            float progress = MathHelper.Clamp(tellElapsed / tellTicks, 0f, 1f);
            int count = 1 + (int)(progress * 2f);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(20f, 20f) * MathHelper.Lerp(1f, 0.25f, progress);
                bool redMote = mode == 1 && (i + (int)Projectile.localAI[0]) % 2 == 0;
                bool orangeMote = mode == 0 && (i + (int)Projectile.localAI[0]) % 3 == 0;

                int dustType = DustID.GoldFlame;
                Color dustColor = Color.Gold;
                if (redMote)
                {
                    dustType = DustID.RedTorch;
                    dustColor = Color.OrangeRed;
                }
                else if (orangeMote)
                {
                    dustType = DustID.OrangeTorch;
                    dustColor = Color.Orange;
                }

                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, dustType,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.8f, 2.4f, progress),
                    90, dustColor, Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        void ChargeVFX(int mode, float baseAngle, Player target, int birthTick, int releaseTick)
        {
            if (mode == 2)
            {
                // Ashen Orbit: the brands circle the player clockwise and tighten from 440 to 360px. Runs on every
                // machine (it moves the projectile); only the dust below is client-side.
                float orbitCharge = MathHelper.Clamp((Projectile.localAI[0] - birthTick) / Math.Max(1f, releaseTick - birthTick), 0f, 1f);
                float orbitAngle = baseAngle + (Projectile.localAI[0] - birthTick) * 0.018f;
                Projectile.Center = target.Center + orbitAngle.ToRotationVector2() * MathHelper.Lerp(AshenRadius, 360f, orbitCharge);
            }

            if (Main.dedServ)
            {
                return;
            }

            float charge = MathHelper.Clamp((Projectile.localAI[0] - birthTick) / Math.Max(1f, releaseTick - birthTick), 0f, 1f);
            if (mode == 0) // Constellation brands: inward-curling gold sparks and late white glints.
            {
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    float spiral = baseAngle + Projectile.localAI[0] * 0.24f;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + spiral.ToRotationVector2() * (4f + charge * 5f), DustID.OrangeTorch,
                        -spiral.ToRotationVector2() * (0.8f + charge), 80, Color.Orange, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
                if (charge > 0.72f && ((int)Projectile.localAI[0]) % 4 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.6f, 0.6f),
                        120, Color.LightGoldenrodYellow, 0.5f);
                    glint.noGravity = true;
                }
            }
            else if (mode == 1) // Cardinal brands: red-orange sparks draw a compact cross through each mark.
            {
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    Vector2 axis = baseAngle.ToRotationVector2();
                    Vector2 crossAxis = axis.RotatedBy(MathHelper.PiOver2);
                    bool perpendicular = ((int)Projectile.localAI[0] / 2) % 2 == 0;
                    Vector2 direction = perpendicular ? crossAxis : axis;
                    int dustType = perpendicular ? DustID.RedTorch : DustID.OrangeTorch;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - direction * 7f, dustType,
                        direction * (1.35f + charge * 1.3f), 80, Color.OrangeRed, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
            }
            else // Ashen Orbit: a clockwise trail behind each circling brand.
            {
                float angle = baseAngle + (Projectile.localAI[0] - birthTick) * 0.018f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 tangent = (angle + MathHelper.PiOver2).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - tangent * 3f, DustID.OrangeTorch, tangent * (1.1f + charge),
                        80, Color.Orange, 0.52f + charge * 0.28f);
                    dust.noGravity = true;
                }
                if (charge > 0.7f && ((int)Projectile.localAI[0]) % 3 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.55f, 0.55f),
                        110, Color.LightGoldenrodYellow, 0.46f);
                    glint.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 70; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool glint = i % 5 == 0;

                int dustType = DustID.GoldFlame;
                float minSpeed = 1.8f;
                float maxSpeed = 6.8f;
                int alpha = 100;
                Color dustColor = Color.Gold;
                float minScale = 0.65f;
                float maxScale = 1.35f;
                if (glint)
                {
                    dustType = DustID.AncientLight;
                    minSpeed = 5f;
                    maxSpeed = 10f;
                    alpha = 70;
                    dustColor = Color.LightGoldenrodYellow;
                    minScale = 0.4f;
                    maxScale = 0.8f;
                }

                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType,
                    direction * Main.rand.NextFloat(minSpeed, maxSpeed), alpha, dustColor,
                    Main.rand.NextFloat(minScale, maxScale));
                dust.noGravity = true;
            }
        }

        void ReleaseVFX(int mode, float baseAngle)
        {
            if (Main.dedServ)
            {
                return;
            }

            int sparks = mode == 2 ? 6 : 4;
            for (int i = 0; i < sparks; i++)
            {
                float angle = MathHelper.TwoPi * i / sparks + baseAngle;
                if (mode == 1)
                {
                    angle = baseAngle + (i - (sparks - 1) * 0.5f) * 0.22f;
                }

                Vector2 velocity = angle.ToRotationVector2() * (1.6f + Main.rand.NextFloat(1.2f));
                int dustType = mode == 1 && i % 2 == 0 ? DustID.RedTorch : DustID.OrangeTorch;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 70, Color.OrangeRed, 0.75f);
                dust.noGravity = true;
            }

            if (mode == 0 || mode == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(2.2f, 2.2f),
                        90, Color.LightGoldenrodYellow, 0.65f);
                    glint.noGravity = true;
                }
            }
        }
    }
}
