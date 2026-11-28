using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Gores;

namespace tsorcRevamp.Content.Projectiles.Enemy.GravelordNito
{
    // Invisible melee HITBOX for Nito's sword swings. The visible blade is drawn by the boss itself
    // (a loose sword layer BEHIND its body), so this projectile draws nothing — it only mirrors the
    // exact same arc via the shared GravelordNito.Slash* helpers so the hitbox tracks the visual.
    class NitoSwordSlash : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        int OwnerIndex => (int)Projectile.ai[0];
        // ai[1] = swing kind + timing style * 8 (see GravelordNito.SpawnSlash).
        int Kind => (int)Projectile.ai[1] % 8;
        int Style => (int)Projectile.ai[1] / 8;
        int Dir => Projectile.ai[2] >= 0f ? 1 : -1;
        int Timer => (int)Projectile.localAI[0];
        int SwingTicks => global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashStyleTicks(Style);

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 330;
            Projectile.height = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18; // real length is set from the style in OnSpawn
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = SwingTicks;
        }

        // Only a hitbox while the blade is still moving fast (speed >= 30% of peak); after that the eased
        // tail is pure follow-through/recovery and must not hurt. Legacy swings stay live throughout.
        public override bool? CanDamage() =>
            Timer <= global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashArmedTicks(Style) ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }
            bool phaseTwo = owner.ModNPC is global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito nito
                && nito.IsPhaseTwo;

            // Eased pose progress (0 to ~1.12), the same value the boss uses to draw the visible blade.
            float progress = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashEasedProgress(Style, Timer);
            // Use the boss rig's shared vertical correction so collision and the shader remain on the
            // visible blade if its ground anchoring is tuned again.
            Projectile.Center = owner.Center
                + new Vector2(0f, global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.GroundSinkPixels)
                + global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashOffset(Kind, Dir, progress);
            Projectile.rotation = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashWorldAngle(Kind, Dir, progress);

            if (Timer > global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashArmedTicks(Style))
            {
                return; // eased tail: the boss still draws the blade, but no dust advertises a dead hitbox
            }

            // The shader carries the blade silhouette. Emit unthrottled body motes plus a bright edge
            // glint along the hilt-to-tip line; phase two uses RedTorch + Shadowflame (not gore-gated
            // Blood alone) so readability holds even with Blood & Gore disabled.
            if (!Main.dedServ)
            {
                Vector2 blade = Projectile.rotation.ToRotationVector2();
                Vector2 hilt = Projectile.Center - blade * 40f;
                Vector2 tip = Projectile.Center + blade * 150f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 position = Vector2.Lerp(hilt, tip, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(7f, 7f);
                    int dustType;
                    if (phaseTwo)
                    {
                        dustType = i == 0 ? DustID.RedTorch : DustID.Shadowflame;
                    }
                    else
                    {
                        dustType = i == 0 ? DustID.Shadowflame : DustID.BoneTorch;
                    }

                    Vector2 velocity = phaseTwo
                        ? blade * Main.rand.NextFloat(0.4f, 1.6f) + new Vector2(0f, Main.rand.NextFloat(-2.6f, -0.8f))
                        : blade * Main.rand.NextFloat(0.6f, 2.2f);
                    Dust body = Dust.NewDustPerfect(position, dustType, velocity, Main.rand.Next(35, 58), default,
                        Main.rand.NextFloat(0.95f, 1.35f));
                    body.noGravity = true;
                }

                Vector2 glintPosition = Vector2.Lerp(hilt, tip, Main.rand.NextFloat(0.45f, 1f));
                Dust glint = Dust.NewDustPerfect(glintPosition, DustID.AncientLight,
                    blade * Main.rand.NextFloat(1.4f, 3.2f), Main.rand.Next(0, 26),
                    phaseTwo ? new Color(255, 165, 175) : new Color(220, 200, 245),
                    Main.rand.NextFloat(0.55f, 0.85f));
                glint.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Kind == 1)
            {
                // Downward Overhead Cleave impact staggers and slows the player for 2 seconds.
                Stagger.Apply(target, 2 * 60);
                target.AddBuff(BuffID.Slow, 2 * 60);
            }

            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            if (owner?.ModNPC is global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito nito
                && nito.IsPhaseTwo)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 hilt = Projectile.Center - Projectile.rotation.ToRotationVector2() * 60f;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * 170f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(new Vector2(targetHitbox.X, targetHitbox.Y), new Vector2(targetHitbox.Width, targetHitbox.Height), hilt, tip, Kind == 2 ? 34f : 48f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // The crescent follows the eased blade (normalised 0-1 over the arc), so it decelerates with
            // the sword instead of racing ahead of it on a linear clock. Legacy: identical to before.
            float easedProgress = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashEasedProgress(Style, Timer);
            float progress = MathHelper.Clamp(
                easedProgress / global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashEndProgress(Style), 0f, 1f);
            Vector2 blade = Projectile.rotation.ToRotationVector2();
            Vector2 center = Projectile.Center + blade * 50f;
            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            bool phaseTwo = owner?.ModNPC is global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito nito
                && nito.IsPhaseTwo;
            NitoVFX.DrawSlash(center, Projectile.rotation, progress, 0.9f, Kind, Dir,
                Projectile.identity * 0.517f + Kind * 0.31f, phaseTwo);
            return false;
        }
    }

    class NitoBoneShard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bone;

        // Optional spin-up telegraph. ai[0] = 0 keeps the original fire-immediately behaviour (used by
        // Hollow Command's radial burst, which is its own telegraph); Bone Volley passes 60 so its
        // shards materialise, hang and spin for a full second first.
        int ChargeTicks => (int)Projectile.ai[0];
        int OwnerIndex => (int)Projectile.ai[1];
        float FanOffset => Projectile.ai[2];
        int Timer => (int)Projectile.localAI[0];
        float LaunchSpeed => Projectile.localAI[1];

        const float ShardGravity = 0.08f;   // px/tick^2 added to velocity.Y every tick after launch
        const float MinFlightTicks = 12f;   // point-blank shots still get a readable arc
        const float MaxFlightTicks = 190f;  // stays inside the shard's 210-tick timeLeft
        // ChargeTicks > 0 guard matters: CanDamage/ShouldUpdatePosition can be queried BEFORE the first
        // AI() tick, when Timer is still 0 — without it a zero-charge shard (Hollow Command's radial
        // burst) would read as "charging" and spend its first frame frozen and harmless.
        bool Charging => ChargeTicks > 0 && Timer <= ChargeTicks;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Remember the speed the spawner asked for, then hold position; the launch below restores
            // it along a freshly-aimed vector.
            Projectile.localAI[1] = Projectile.velocity.Length();
            if (ChargeTicks > 0)
            {
                Projectile.timeLeft += ChargeTicks;
                Projectile.velocity = Vector2.Zero;
            }
        }

        // A shard hanging in the air waiting to launch must not clip on terrain or deal damage.
        public override bool ShouldUpdatePosition() => !Charging;
        public override bool? CanDamage() => Charging ? false : null;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Charging)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.42f; // spin in place — the actual "incoming" tell
                Lighting.AddLight(Projectile.Center, 0.22f, 0.2f, 0.28f);

                if (!Main.dedServ)
                {
                    float chargeProgress = MathHelper.Clamp(Timer / (float)ChargeTicks, 0f, 1f);
                    int chargeCount = 1 + (int)(chargeProgress * 2f);

                    for (int i = 0; i < chargeCount; i++)
                    {
                        Vector2 spawnOffset = Main.rand.NextVector2CircularEdge(16f + (1f - chargeProgress) * 10f, 16f + (1f - chargeProgress) * 10f);
                        int chargeType = i % 2 == 0 ? DustID.BoneTorch : DustID.Shadowflame;
                        Dust chargeDust = Dust.NewDustPerfect(Projectile.Center + spawnOffset, chargeType,
                            -spawnOffset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.8f, 1.8f),
                            Main.rand.Next(40, 65), default, Main.rand.NextFloat(0.75f, 1.15f));
                        chargeDust.noGravity = true;
                    }

                    if (Timer % 2 == 0)
                    {
                        Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                            DustID.Smoke, Main.rand.NextVector2Circular(0.8f, 0.8f), 155,
                            new Color(52, 44, 58), Main.rand.NextFloat(0.5f, 0.7f));
                        smoke.noGravity = true;
                        smoke.noLight = true;
                        smoke.fadeIn = Main.rand.NextFloat(1.25f, 1.5f);
                    }
                }

                return;
            }
            if (Timer == ChargeTicks + 1 && ChargeTicks > 0)
            {
                LaunchAtTarget();
            }
            Projectile.rotation += Projectile.velocity.X * 0.04f;
            Projectile.velocity.Y += ShardGravity;
            Lighting.AddLight(Projectile.Center, 0.16f, 0.16f, 0.22f);

            if (!Main.dedServ)
            {
                // Interpolate 2-3 motes across the velocity step so 9.5-10.8 px/t flight stays <= 4.5 px gap.
                const int TrailCount = 3;
                for (int i = 0; i < TrailCount; i++)
                {
                    float fraction = (i + Main.rand.NextFloat()) / TrailCount;
                    Vector2 trailPos = Projectile.Center - Projectile.velocity * fraction + Main.rand.NextVector2Circular(4f, 4f);
                    int trailType = i == 0 ? DustID.Shadowflame : DustID.BoneTorch;
                    Dust trail = Dust.NewDustPerfect(trailPos, trailType,
                        Projectile.velocity * -0.08f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                        Main.rand.Next(35, 60), default, Main.rand.NextFloat(0.85f, 1.25f));
                    trail.noGravity = true;
                }

                Dust glint = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.AncientLight, Projectile.velocity * -0.14f, Main.rand.Next(0, 26),
                    new Color(225, 220, 240), Main.rand.NextFloat(0.5f, 0.8f));
                glint.noGravity = true;

                if (Timer % 2 == 0)
                {
                    Dust mist = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.Smoke,
                        Projectile.velocity * -0.04f + Main.rand.NextVector2Circular(0.4f, 0.4f), 160,
                        new Color(48, 42, 56), Main.rand.NextFloat(0.5f, 0.72f));
                    mist.noGravity = true;
                    mist.noLight = true;
                    mist.fadeIn = Main.rand.NextFloat(1.25f, 1.5f);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.45f, Pitch = 0.2f }, Projectile.Center);

            // 3-layer shatter burst: 12 midground bone/shadow, 7 fast glints, 5 billowing dark smoke.
            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                int dustType = i % 3 == 0 ? DustID.Shadowflame : DustID.BoneTorch;
                Dust body = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(2f, 8f),
                    dustType, direction * Main.rand.NextFloat(1.8f, 4.6f), Main.rand.Next(35, 60),
                    default, Main.rand.NextFloat(0.9f, 1.35f));
                body.noGravity = true;
            }

            for (int i = 0; i < 7; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                    direction * Main.rand.NextFloat(4.5f, 7.8f), Main.rand.Next(0, 25),
                    new Color(230, 225, 240), Main.rand.NextFloat(0.5f, 0.85f));
                spark.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(2f, 10f),
                    DustID.Smoke, direction * Main.rand.NextFloat(0.5f, 1.6f), 155,
                    new Color(46, 40, 54), Main.rand.NextFloat(0.55f, 0.75f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.55f);
            }
        }

        ///<summary>Aims at release with a partial ballistic velocity lead (6-16 ticks) so steady
        ///sideways walking gets clipped while still leaving room for a direction reversal, jump, or roll.</summary>
        void LaunchAtTarget()
        {
            float speed = LaunchSpeed > 0.1f ? LaunchSpeed : 9.5f;
            Vector2 aim = new Vector2(speed, 0f);
            if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs)
            {
                NPC owner = Main.npc[OwnerIndex];
                if (owner.active && owner.target >= 0 && owner.target < Main.maxPlayers)
                {
                    Player target = Main.player[owner.target];
                    if (target.active && !target.dead)
                    {
                        Vector2 rawToTarget = target.Center - Projectile.Center;
                        float initialFlightTicks = MathHelper.Clamp(rawToTarget.Length() / speed, MinFlightTicks, MaxFlightTicks);
                        float leadTicks = MathHelper.Clamp(initialFlightTicks * 0.45f, 6f, 16f);
                        Vector2 predictedTarget = target.Center + target.velocity * leadTicks;
                        Vector2 toTarget = predictedTarget - Projectile.Center;
                        float flightTicks = MathHelper.Clamp(toTarget.Length() / speed, MinFlightTicks, MaxFlightTicks);

                        // AI() runs BEFORE the position update, and this same AI call adds gravity once
                        // more after the launch, so after N ticks the shard has dropped g*N*(N+1)/2 px
                        // (not g*N^2/2). Cancel that drop with extra upward launch velocity.
                        float gravityDrop = ShardGravity * flightTicks * (flightTicks + 1f) * 0.5f;
                        aim = new Vector2(toTarget.X / flightTicks, (toTarget.Y - gravityDrop) / flightTicks);
                    }
                }
            }
            Projectile.velocity = aim.RotatedBy(FanOffset);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
                Vector2 launchDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 10; i++)
                {
                    int dustType = i < 4 ? DustID.AncientLight : DustID.BoneTorch;
                    Vector2 sparkVel = launchDir.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(2.5f, 6.2f);
                    Dust snap = Dust.NewDustPerfect(Projectile.Center, dustType, sparkVel,
                        i < 4 ? 10 : 40, default, i < 4 ? Main.rand.NextFloat(0.55f, 0.85f) : Main.rand.NextFloat(0.9f, 1.25f));
                    snap.noGravity = true;
                }
            }

            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Charging)
            {
                return true; // no motion trail on a shard that isn't moving yet — just the spinning bone
            }
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - direction * 24f;
            float progress = MathHelper.Clamp(1f - Projectile.timeLeft / 210f, 0.08f, 0.72f);
            NitoVFX.DrawSoulTrail(center, direction.ToRotation(), new Vector2(78f, 24f), progress, 0.62f,
                Projectile.identity * 0.437f);
            return true;
        }
    }

    class NitoDeathNova : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const float ExpandSpeed = 10.5f;
        internal const float RingHalfThickness = 28f; // also read by NitoGraveHand to size its blast dust
        internal const int HandBlastVisualTicks = 90;
        internal const int HandBlastPulseCount = 5;
        internal const int HandBlastPulseSpacing = 8;
        internal const int HandBlastPulseTicks = HandBlastVisualTicks - (HandBlastPulseCount - 1) * HandBlastPulseSpacing;
        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 260f;
        // ai[1] is deliberately opt-in: Quietus and the other existing nova users retain their
        // current debuffs/dust, while a grave-hand detonation gets the Destined Death treatment.
        bool GraveHandDetonation => Projectile.ai[1] > 0.5f;
        // ai[1] = 2: the same Destined Death blast drawn as a harmless visual (Quietus body burst).
        bool VisualOnly => Projectile.ai[1] > 1.5f;
        float Radius => Projectile.localAI[0];
        int DamageTicks => (int)(MaxRadius / ExpandSpeed) + 3;
        float CollisionHalfThickness => GraveHandDetonation ? RingHalfThickness * 1.25f : RingHalfThickness;
        int Age => (int)Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 620;
            Projectile.height = 620;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.aiStyle = 0;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = DamageTicks;
            if (GraveHandDetonation)
            {
                Projectile.timeLeft = System.Math.Max(1, HandBlastVisualTicks - Age);
            }
        }

        // Shared by collision, dust and drawing: five expanding damage rings overlap across 90 ticks.
        // Each ring lasts 58 ticks; starts at 0/8/16/24/32 leave no gap in the sequence.
        internal static bool HandBlastRingAt(int age, int pulse, float maxRadius, out float radius, out float opacity)
        {
            int pulseAge = age - pulse * HandBlastPulseSpacing;
            radius = 0f;
            opacity = 0f;
            if (pulseAge < 0 || pulseAge >= HandBlastPulseTicks)
            {
                return false;
            }

            radius = maxRadius * (pulseAge + 1f) / HandBlastPulseTicks;
            opacity = MathHelper.Clamp((HandBlastPulseTicks - pulseAge) / 8f, 0f, 1f);
            return true;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[2]++;
            Projectile.localAI[0] += ExpandSpeed;

            if (!Main.dedServ)
            {
                // Decorate each hand-blast ring with a restrained wraith wake. Other novas retain
                // 34–66 dust per tick, scaling density with their radius to avoid a sparse dotted ring.
                int dustCount = System.Math.Clamp((int)(Radius * 0.11f), 34, 66);
                if (GraveHandDetonation)
                {
                    dustCount = 0;
                    for (int pulse = 0; pulse < HandBlastPulseCount; pulse++)
                    {
                        if (!HandBlastRingAt(Age - 1, pulse, MaxRadius, out float ringRadius, out float ringOpacity))
                        {
                            continue;
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 radial = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                            Vector2 position = Projectile.Center + radial * ringRadius;
                            Dust dust = Dust.NewDustPerfect(position, DustID.Wraith, radial * 1.5f,
                                115, new Color(24, 2, 20), 0.85f * ringOpacity);
                            dust.noGravity = true;
                            dust.noLight = true;
                        }
                    }
                }

                for (int i = 0; i < dustCount; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 radial = angle.ToRotationVector2();
                    Vector2 pos = Projectile.Center + radial * (Radius + Main.rand.NextFloat(-7f, 7f));
                    Dust dust = Dust.NewDustPerfect(pos,
                        Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.BoneTorch,
                        radial * 2.4f, Main.rand.Next(40, 62), default, Main.rand.NextFloat(1f, 1.45f));
                    dust.noGravity = true;
                }

                if (!GraveHandDetonation)
                {
                    // Foreground leading-edge sparks give the shockwave ring crisp contrast.
                    for (int i = 0; i < 10; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 radial = angle.ToRotationVector2();
                        Vector2 sparkPos = Projectile.Center + radial * (Radius + Main.rand.NextFloat(-4f, 6f));
                        Dust spark = Dust.NewDustPerfect(sparkPos, DustID.AncientLight,
                            radial * Main.rand.NextFloat(3.2f, 5.5f), Main.rand.Next(0, 26),
                            new Color(220, 195, 250), Main.rand.NextFloat(0.55f, 0.9f));
                        spark.noGravity = true;
                    }
                }

                int pulseAge = Age - 1;
                if (GraveHandDetonation && pulseAge % HandBlastPulseSpacing == 0
                    && pulseAge / HandBlastPulseSpacing < HandBlastPulseCount)
                {
                    int pulse = pulseAge / HandBlastPulseSpacing;
                    float strength = 1f - pulse * 0.15f;
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f * strength, Pitch = -0.3f + pulse * 0.12f }, Projectile.Center);
                    for (int i = 0; i < 32; i++)
                    {
                        Vector2 direction = Main.rand.NextVector2Unit();
                        Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(8f, MaxRadius * 0.8f);
                        Dust dust = Dust.NewDustPerfect(position, DustID.Wraith,
                            direction * Main.rand.NextFloat(1.5f, 4f) * strength, 110,
                            new Color(32, 4, 24), Main.rand.NextFloat(0.65f, 1.1f));
                        dust.noGravity = true;
                        dust.noLight = true;
                    }
                }

                float lightFade = 1f;
                if (GraveHandDetonation)
                {
                    lightFade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
                }

                Vector3 lightColor = new Vector3(0.45f, 0.35f, 0.65f);
                if (GraveHandDetonation)
                {
                    lightColor = new Vector3(0.52f, 0.08f, 0.12f);
                }

                Lighting.AddLight(Projectile.Center, lightColor * lightFade);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Radius > 2f)
            {
                if (GraveHandDetonation)
                {
                    NitoVFX.DrawGraveHandNova(Projectile.Center, Age - 1, MaxRadius, CollisionHalfThickness);
                }
                else
                {
                    NitoVFX.DrawQuietusNova(Projectile.Center, Radius, MaxRadius, RingHalfThickness, 0.95f);
                }

                float flashProgress = MathHelper.Clamp(Radius / 88f, 0f, 1f);
                if (flashProgress < 1f)
                {
                    NitoVFX.DrawDeathFlash(Projectile.Center, Vector2.One * 176f,
                        flashProgress, 0.72f * (1f - flashProgress));
                }
            }
            return false;
        }

        public override bool? CanDamage() => VisualOnly ? false : null;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            if (GraveHandDetonation)
            {
                for (int pulse = 0; pulse < HandBlastPulseCount; pulse++)
                {
                    if (!HandBlastRingAt(Age - 1, pulse, MaxRadius, out float ringRadius, out _))
                    {
                        continue;
                    }

                    if (distClosest <= ringRadius + CollisionHalfThickness && distFarthest >= ringRadius - CollisionHalfThickness)
                    {
                        return true;
                    }
                }

                return false;
            }

            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Darkness, 5 * 60);
            target.AddBuff(BuffID.Slow, 2 * 60);
            if (GraveHandDetonation)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 10 * 60);
            }
        }
    }

    class NitoGraveSpike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSwordDance";

        const int RiseTicks = 7;
        const int HoldTicks = 80;
        // 12 -> 60: the blade used to vanish almost the instant it started withdrawing. It now sinks
        // slowly AND fades out over the same window (see the `fade` term in PreDraw).
        const int SinkTicks = 60;
        int TelegraphTicks => (int)Projectile.ai[0];
        float HeightScale => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 54;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Vector2 floorAnchor = Projectile.Bottom;
            Projectile.height = (int)(54 * HeightScale);
            Projectile.Bottom = floorAnchor;
            Projectile.timeLeft = TelegraphTicks + RiseTicks + HoldTicks + SinkTicks;
        }

        public override bool? CanDamage() => Erupted && !Sinking;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float bottom = Projectile.position.Y + Projectile.height;
            if (!Erupted)
            {
                if (!Main.dedServ)
                {
                    float telegraphProgress = TelegraphTicks > 0
                        ? MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f)
                        : 1f;
                    int tellCount = 2 + (int)(telegraphProgress * 2f);
                    for (int i = 0; i < tellCount; i++)
                    {
                        int dustType = i % 2 == 0 ? DustID.RedTorch : DustID.BoneTorch;
                        Vector2 dustPos = new Vector2(
                            Projectile.position.X + Main.rand.NextFloat(Projectile.width),
                            bottom - Main.rand.NextFloat(2f, 12f));
                        Dust tell = Dust.NewDustPerfect(dustPos, dustType,
                            new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-2.6f, -0.8f)),
                            Main.rand.Next(35, 60), default, Main.rand.NextFloat(0.85f, 1.25f));
                        tell.noGravity = true;
                    }
                }
                return;
            }
            if (Timer == TelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.55f, Pitch = -0.5f }, Projectile.Center);
                // Bursting out of the ground throws grave-fire at the breach point.
                NitoVFX.PyreBurst(new Vector2(Projectile.Center.X, bottom), 26, 4.5f, 1.2f, 20f, 8f);
            }
            if (Timer == TelegraphTicks + RiseTicks + HoldTicks + 1)
            {
                // ...and again as it begins withdrawing, so the exit reads as deliberate rather than
                // the sprite simply being switched off.
                NitoVFX.PyreBurst(new Vector2(Projectile.Center.X, bottom), 18, 3.2f, 1f, 18f, 8f);
            }
            if (!Sinking && !Main.dedServ)
            {
                int smolderType = Main.rand.NextBool(3) ? DustID.RedTorch : DustID.BoneTorch;
                Dust smolder = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    smolderType, 0f, Main.rand.NextFloat(-1.8f, -0.7f), Main.rand.Next(45, 65), default,
                    Main.rand.NextFloat(0.8f, 1.15f));
                smolder.noGravity = true;
            }
        }

        float RiseProgress()
        {
            if (!Erupted)
            {
                return 0f;
            }
            if (Sinking)
            {
                return 1f - (Timer - TelegraphTicks - RiseTicks - HoldTicks) / (float)SinkTicks;
            }
            return MathHelper.Min(1f, (Timer - TelegraphTicks) / (float)RiseTicks);
        }

        ///<summary>Blades erupt from the ground, so they must render BEHIND the tiles — otherwise the
        ///buried portion draws over the floor and the whole spike reads as floating on top of the
        ///terrain instead of being driven through it.</summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = RiseProgress();
            float bottomY = Projectile.position.Y + Projectile.height + 2f;
            float telegraphProgress = TelegraphTicks > 0
                ? MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f)
                : 1f;
            // Withdrawing now fades as well as sinks (progress runs 1 -> 0 across SinkTicks).
            float fade = Sinking ? MathHelper.Clamp(progress, 0f, 1f) : 1f;
            float riftOpacity = Sinking ? MathHelper.Clamp(progress, 0f, 1f) : 0.82f;
            NitoVFX.DrawGroundRift(new Vector2(Projectile.Center.X, bottomY - 4f),
                new Vector2(96f * HeightScale, 44f), telegraphProgress, riftOpacity);

            if (progress <= 0f)
            {
                return false;
            }
            NitoVFX.DrawGraveEruption(Projectile.Center + new Vector2(0f, Projectile.height * 0.12f),
                new Vector2(74f * HeightScale, Projectile.height * 1.35f), progress, 0.48f * fade);
            // The sword-dance blade (GravelordNitoSwordDance.png, 58x224 — already a tall, straight,
            // upright blade shape) SLIDES up out of the ground and back down (translate) instead of
            // growing in place (a Y-squash, which read like the blade flopping over in 3D). Drawn at
            // full — now DOUBLE — size the whole time; the un-emerged fraction is simply pushed below
            // the floor line. Undyed: the sprite's own crimson reads.
            // Scale must be UNIFORM (X == Y): the old code derived scaleX from Projectile.width (the
            // narrow 26px HITBOX, unrelated to the art) against the texture's width, independently of
            // scaleY — squashing the tall, slender texture into a short, fat, visibly "warped" shape.
            const float SizeScale = 2f; // twice as big as the old draw
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float scale = Projectile.height / (float)texture.Height * SizeScale;
            float drawnHeight = texture.Height * scale; // == Projectile.height * SizeScale
            float emerge = (1f - progress) * drawnHeight; // pushes the blade down into the floor when not fully risen
            Vector2 bottom = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f + emerge) - Main.screenPosition;
            Main.EntitySpriteDraw(texture, bottom, null, Color.White * fade, 0f, new Vector2(texture.Width / 2f, texture.Height), new Vector2(scale, scale), SpriteEffects.None, 0);
            return false;
        }
    }

    ///<summary>
    ///A pair of these erupts flanking the player, holds for a beat, then CLAPS together on the spot
    ///the player was standing when they rose — detonating a blast at the meeting point. The two hands
    ///share a convergence centre (ai[1]); ai[2]'s low bit identifies the right-hand exploder, while
    ///values 2/3 mark the alternate outward-detonation response to a player rolling through either
    ///hand before the clap. Each breach burns only for the stationary hold, then dissipates as soon
    ///as its hand starts moving.
    ///</summary>
    class NitoGraveHand : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        int TelegraphTicks => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 18;
        float ConvergeCenterX => Projectile.ai[1];
        // ai[2] bitflags: bit 0 = right-hand exploder, bit 1 = outward-detonation branch, bit 2 = Phase 2 tempo.
        bool IsExploder => ((int)Projectile.ai[2] & 1) == 1;
        bool OutwardDetonation => ((int)Projectile.ai[2] & 2) != 0;
        bool IsPhaseTwo => ((int)Projectile.ai[2] & 4) != 0;
        int Side => IsExploder ? 1 : -1;
        int Timer => (int)Projectile.localAI[0];

        const int GrabTicks = 16;    // the initial damaging emergence
        int PauseTicks => IsPhaseTwo ? 24 : 32;      // beat before closing (was 60 — now forces a real jump or roll-through)
        // 30% faster than the old 6.0 / 5.2: 150px closes in 19-22 ticks.
        float ConvergeSpeed => IsPhaseTwo ? 7.8f : 6.76f;
        const float OutwardSpeed = 7.8f;   // 30% over 6. OutwardTravelDistance / OutwardSpeed drives the timeLeft, so it self-adjusts
        internal const float InitialHandOffset = 150f;
        const float OutwardTravelDistance = 300f;
        const int MaxConvergeTicks = 150; // failsafe so a hand can never chase forever
        const float BlastRadius = 125f * 1.25f;
        internal const int HandHeight = 156;

        // Cached motion trail: oldPos samples (one per tick) drawn as fading ghost hands behind the
        // real one while it is travelling. Stride 4 x 20 samples = ghosts 4/8/12/16 ticks back, i.e.
        // ~15px apart at converge speed and ~24px apart on the outward retreat.
        const int TrailLength = 20;
        const int TrailStride = 4;
        const float TrailOpacity = 0.55f;

        // Blast dust reach. The damaging ring's outer edge is the nova radius plus its half thickness;
        // the dust is meant to fly 25% past that. Reach layers are noGravity and vanilla dust only
        // damps X by 0.99/tick, so speed = distance / DustFlightTicks is close to what actually flies.
        const float DamageRingOuterRadius = BlastRadius + NitoDeathNova.RingHalfThickness * 1.25f;
        const float DustReachRadius = DamageRingOuterRadius * 1.25f;
        const float DustFlightTicks = 16f;

        int EmergeTick => TelegraphTicks;
        int ConvergeStartTick => TelegraphTicks + GrabTicks + PauseTicks;
        bool Converging => !OutwardDetonation && Timer > ConvergeStartTick;
        internal bool HasStartedMoving => OutwardDetonation || Converging;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            // DOUBLED (was 86x78) on request — the grasping hand reads as a set piece now, not a prop.
            // This is the one deliberate gameplay change in the shader pass: the grab area doubles with
            // it, so visual and hitbox stay in parity. Halve these two numbers to revert.
            Projectile.width = 172;
            Projectile.height = HandHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = ConvergeStartTick + MaxConvergeTicks + 20;
        }

        ///<summary>Hands claw up out of the ground, so like the blades they belong behind the tiles —
        ///otherwise the wrist draws on top of the floor it is supposedly bursting through.</summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        // The retreating hands are a telegraph, not two moving hitboxes: their separate novas are the
        // punishment. The normal version remains dangerous on emergence and while sweeping inward.
        public override bool? CanDamage() =>
            !OutwardDetonation && ((Timer > EmergeTick && Timer < EmergeTick + GrabTicks) || Converging);

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Timer <= EmergeTick)
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int dustType = i % 2 == 0 ? DustID.BoneTorch : DustID.RedTorch;
                        Dust tell = Dust.NewDustDirect(Projectile.BottomLeft - new Vector2(0f, 10f), Projectile.width, 12,
                            dustType, Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-2.6f, -0.9f),
                            Main.rand.Next(38, 60), default, Main.rand.NextFloat(0.9f, 1.3f));
                        tell.noGravity = true;
                    }
                }
                return;
            }
            if (Timer == EmergeTick + 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
                // Heavy ground-break thud. Only the exploder hand plays it, so the pair erupting on the
                // same tick sounds once instead of doubling up.
                if (IsExploder)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                        with { Volume = 0.7f }, Projectile.Center);
                }
                for (int i = 0; i < 22; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 80, default, 1.1f);
                }
                NitoVFX.PyreBurst(Projectile.Bottom, 30, 5f, 1.3f, 30f, 10f);
                // The breach burns through the held telegraph; its hand link dismisses it as soon as
                // this projectile begins either inward or outward movement.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Bottom, Vector2.Zero,
                        ModContent.ProjectileType<NitoPyreFire>(), Projectile.damage / 2, 0f,
                        Projectile.owner, Projectile.whoAmI + 1f);
                }
                return;
            }

            // A dodge through either hand during the pre-clap window changes the whole pair's answer.
            // Detection and state mutation are server-authoritative; ai[2] then synchronizes the branch.
            if (!OutwardDetonation && Timer <= ConvergeStartTick && Main.netMode != NetmodeID.MultiplayerClient
                && RollingPlayerIntersects())
            {
                ArmPairForOutwardDetonation();
            }

            if (OutwardDetonation)
            {
                RunOutwardDetonation();
                return;
            }

            if (!Converging)
            {
                return; // the held beat
            }

            int dir = ConvergeCenterX >= Projectile.Center.X ? 1 : -1;
            Projectile.velocity = new Vector2(dir * ConvergeSpeed, 0f);
            NitoVFX.PyreBurst(Projectile.Bottom + new Vector2(0f, -6f), 2, 2.2f, 0.9f, 26f, 10f);

            // Met in the middle: detonate. Only the flagged hand spawns the blast so a pair produces
            // one explosion, but BOTH retire on contact.
            if (System.Math.Abs(Projectile.Center.X - ConvergeCenterX) <= ConvergeSpeed + 1f)
            {
                if (IsExploder)
                {
                    Vector2 blastCenter = new Vector2(ConvergeCenterX, Projectile.Center.Y);
                    Detonate(blastCenter);
                }
                Projectile.Kill();
            }
        }

        bool RollingPlayerIntersects()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox)
                    && player.GetModPlayer<global::tsorcRevamp.tsorcRevampPlayer>().isDodging)
                {
                    return true;
                }
            }
            return false;
        }

        void ArmPairForOutwardDetonation()
        {
            int handType = ModContent.ProjectileType<NitoGraveHand>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hand = Main.projectile[i];
                if (!hand.active || hand.type != handType || ((int)hand.ai[2] & 2) != 0
                    || System.Math.Abs(hand.ai[1] - ConvergeCenterX) > 0.5f
                    || System.Math.Abs(System.Math.Abs(hand.Center.X - ConvergeCenterX) - InitialHandOffset) > 4f)
                {
                    continue;
                }

                hand.ai[2] = (int)hand.ai[2] | 2;
                int handSide = ((int)hand.ai[2] & 1) == 1 ? 1 : -1;
                hand.velocity = new Vector2(handSide * OutwardSpeed, 0f);
                hand.timeLeft = System.Math.Max(hand.timeLeft,
                    (int)(OutwardTravelDistance / OutwardSpeed) + 20);
                hand.netUpdate = true;
            }
        }

        void RunOutwardDetonation()
        {
            float targetX = ConvergeCenterX + Side * (InitialHandOffset + OutwardTravelDistance);
            float remaining = System.Math.Abs(targetX - Projectile.Center.X);
            float traveled = System.Math.Max(0f,
                System.Math.Abs(Projectile.Center.X - ConvergeCenterX) - InitialHandOffset);
            float progress = MathHelper.Clamp(traveled / OutwardTravelDistance, 0f, 1f);
            Projectile.velocity = new Vector2(Side * OutwardSpeed, 0f);

            EmitOutwardTelegraph(progress);
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.035f, 0.07f) * (0.35f + 0.65f * progress));

            if (remaining <= OutwardSpeed + 1f)
            {
                Projectile.Center = new Vector2(targetX, Projectile.Center.Y);
                Projectile.velocity = Vector2.Zero;
                Detonate(Projectile.Center);
                Projectile.Kill();
            }
        }

        void EmitOutwardTelegraph(float progress)
        {
            if (Main.netMode == NetmodeID.Server || Timer % 2 != 0)
            {
                return;
            }

            int count = 1 + (int)(progress * 4f);
            Vector2 wakeDirection = new Vector2(-Side, -0.35f);
            for (int i = 0; i < count; i++)
            {
                bool blood = Main.rand.NextBool(3);
                Vector2 position = Projectile.Center
                    + Main.rand.NextVector2Circular(Projectile.width * 0.28f, Projectile.height * 0.3f);
                Vector2 velocity = wakeDirection * Main.rand.NextFloat(0.45f, 1.8f + progress * 1.4f)
                    + Main.rand.NextVector2Circular(0.55f, 0.75f);
                Dust d = Dust.NewDustPerfect(position, blood ? DustID.Blood : DustID.Wraith, velocity,
                    blood ? 55 : 120,
                    blood ? new Color(140, 16, 26) : new Color(14, 2, 20),
                    Main.rand.NextFloat(0.55f, 0.92f + progress * 0.28f));
                d.noGravity = !blood || Main.rand.NextBool(3);
                d.noLight = !blood;
                if (!blood)
                {
                    d.fadeIn = Main.rand.NextFloat(0.85f, 1.18f);
                }
            }
        }

        void Detonate(Vector2 blastCenter)
        {
            if (!Main.dedServ)
            {
                UsefulFunctions.ScreenShake(blastCenter, 7f, 16);
                SpawnDestinedDeathBlastDust(blastCenter);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // ai[1] opts this nova into the 90-tick, five-ring Destined Death detonation.
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), blastCenter, Vector2.Zero,
                    ModContent.ProjectileType<NitoDeathNova>(), Projectile.damage, 6f, Projectile.owner,
                    BlastRadius, 1f);
            }
        }

        static void SpawnDestinedDeathBlastDust(Vector2 center)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            // INTERIOR BODY: densely fills most of the true 125px detonation radius. Blood falls;
            // some wraith matter also takes gravity so the blast collapses into a deathly rain.
            for (int i = 0; i < 160; i++)
            {
                bool blood = i % 3 == 0 || Main.rand.NextBool(5);
                Vector2 direction = Main.rand.NextVector2Unit();
                float radius = (float)System.Math.Sqrt(Main.rand.NextFloat()) * BlastRadius * 0.88f;
                Dust d = Dust.NewDustPerfect(center + direction * radius,
                    blood ? DustID.Blood : DustID.Wraith,
                    direction * Main.rand.NextFloat(1.4f, 5.8f)
                        + new Vector2(0f, Main.rand.NextFloat(-1.8f, 0.8f)),
                    blood ? 45 : 105,
                    blood ? new Color(150, 18, 28) : new Color(12, 2, 18),
                    Main.rand.NextFloat(0.62f, 1.22f));
                d.noGravity = !blood && !Main.rand.NextBool(3);
                d.noLight = !blood;
                if (!blood && d.noGravity)
                {
                    d.fadeIn = Main.rand.NextFloat(1.28f, 1.58f);
                }
            }

            // SHOCK SPRAY: dust thrown out from inside the blast, each speed solved so it ends up at
            // 80-100% of DustReachRadius (25% past the damaging ring) after DustFlightTicks. Radial only
            // and noGravity, so the reach is predictable instead of being bent into a fall by gravity.
            // These are soft, decorative particles; the shader ring stays the authoritative hit radius.
            for (int i = 0; i < 130; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool blood = i % 2 == 0;
                float spawnRadius = Main.rand.NextFloat(BlastRadius * 0.15f, BlastRadius * 0.95f);
                float flySpeed = (DustReachRadius - spawnRadius) / DustFlightTicks * Main.rand.NextFloat(0.8f, 1f);
                Dust d = Dust.NewDustPerfect(center + direction * spawnRadius, blood ? DustID.Blood : DustID.Wraith,
                    direction * flySpeed,
                    blood ? 35 : 105,
                    blood ? new Color(175, 20, 34) : new Color(18, 2, 26),
                    Main.rand.NextFloat(1f, 1.4f));
                d.noGravity = true;
                d.noLight = !blood;
            }

            // REACH RING: an even circle of dust starting ON the damaging ring's outer edge and pushed
            // out to DustReachRadius, so the blast visibly overshoots its hit area by 25% in every
            // direction. Angle is evenly spaced (not random) so the ring has no gaps.
            const int ReachRingCount = 84;
            float reachRingSpeed = (DustReachRadius - DamageRingOuterRadius) / DustFlightTicks;
            float reachRingStartAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ReachRingCount; i++)
            {
                float angle = reachRingStartAngle + MathHelper.TwoPi * i / ReachRingCount;
                Vector2 direction = angle.ToRotationVector2();
                bool blood = i % 2 == 0;
                Dust d = Dust.NewDustPerfect(center + direction * DamageRingOuterRadius, blood ? DustID.Blood : DustID.Wraith,
                    direction * reachRingSpeed,
                    blood ? 35 : 105,
                    blood ? new Color(175, 20, 34) : new Color(18, 2, 26),
                    Main.rand.NextFloat(1f, 1.3f));
                d.noGravity = true;
                d.noLight = !blood;
            }

            // FALLING AFTERBODY: a broad, slower fill both inside and just outside the nova. Every
            // mote takes gravity, giving blood and wraith fragments a visible descending aftermath.
            for (int i = 0; i < 64; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool blood = Main.rand.NextBool(3);
                Dust d = Dust.NewDustPerfect(
                    center + direction * Main.rand.NextFloat(12f, BlastRadius * 1.35f),
                    blood ? DustID.Blood : DustID.Wraith,
                    direction * Main.rand.NextFloat(0.5f, 2.6f)
                        + new Vector2(0f, Main.rand.NextFloat(-4.2f, -1.2f)),
                    blood ? 70 : 145,
                    blood ? new Color(135, 12, 24) : new Color(8, 1, 14),
                    Main.rand.NextFloat(0.5f, 0.82f));
                d.noGravity = false;
                d.noLight = !blood;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphProgress = MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f);
            float activeProgress = Timer <= TelegraphTicks
                ? telegraphProgress * 0.3f
                : MathHelper.Clamp((Timer - TelegraphTicks) / 10f, 0f, 1f);
            float fade = MathHelper.Clamp((Projectile.timeLeft - 4f) / 12f, 0f, 1f);
            // Once either branch starts moving, stop drawing the breach under the projectile; the
            // hole belongs to the ground and must not skate outward with a retreating hand.
            if (!Converging && !OutwardDetonation)
            {
                NitoVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 7f),
                    new Vector2(Projectile.width * 1.35f, 48f), telegraphProgress, 0.8f * fade);
            }

            Vector2 handSize = new Vector2(Projectile.width * 1.15f, Projectile.height * 1.35f);

            // Cached trail: only while travelling (the held beat is stationary, so every sample would
            // stack on the hand). Drawn BEFORE the real hand so the newest, brightest copy is on top.
            if (Converging || OutwardDetonation)
            {
                for (int ghost = TrailStride; ghost < Projectile.oldPos.Length; ghost += TrailStride)
                {
                    Vector2 ghostPosition = Projectile.oldPos[ghost];
                    if (ghostPosition == Vector2.Zero)
                    {
                        continue; // sample not written yet
                    }

                    Vector2 ghostCenter = ghostPosition + Projectile.Size * 0.5f;
                    if (Vector2.DistanceSquared(ghostCenter, Projectile.Center) < 36f)
                    {
                        continue; // hasn't moved 6px yet: an exact overlap would just brighten the hand
                    }

                    float ghostFade = 1f - ghost / (float)Projectile.oldPos.Length;
                    NitoVFX.DrawGraveHand(ghostCenter, handSize, activeProgress, 0.82f * fade * ghostFade * TrailOpacity);
                }
            }

            NitoVFX.DrawGraveHand(Projectile.Center, handSize, activeProgress, 0.82f * fade);
            return true;
        }
    }

    ///<summary>
    ///The red-and-black grave-fire used by Nito's pyres. A standalone phase-two pyre burns for three
    ///seconds. A grave-hand-linked instance stores handIndex+1 in ai[0], stops damaging the instant
    ///that hand moves, and dissipates over eight frames so no stationary shader is left behind.
    ///</summary>
    class NitoPyreFire : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int BurnTicks = 180;  // 3 seconds
        const int FadeTicks = 30;
        const int HandDismissTicks = 8;
        int Timer => (int)Projectile.localAI[0];
        int LinkedHandIndex => (int)Projectile.ai[0] - 1;
        bool LinkedToHand => Projectile.ai[0] > 0f;
        bool DismissingWithHand => Projectile.ai[1] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 110;
            Projectile.height = 70;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BurnTicks + FadeTicks;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Spawned at the breach (a ground point). NewProjectile centres the box on that point, so
            // lifting by HALF the height puts its bottom on the floor. The old 0.75 lift left the
            // hitbox and the fire floating about a tile above the ground.
            Projectile.position.Y -= Projectile.height * 0.5f;
        }

        // Stops biting once it starts guttering out, so the visual tail is never an invisible hitbox.
        public override bool? CanDamage() => !DismissingWithHand && Timer < BurnTicks;

        // Only the blaze flames burn, not the plume around them. Each flame is a circle about the size of
        // its drawn fire (26px radius at scale 1); faint flames (under 35% opacity) at the top of their
        // climb or just born do not count, so nothing hurts that the player cannot clearly see.
        // Every machine computes the same flames from the synced timer, so the hit player's check agrees.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < FlameCount; i++)
            {
                if (!TryGetFlame(i, out Vector2 flamePosition, out float flameScale, out _, out float flameOpacity, out _))
                {
                    continue;
                }
                if (flameOpacity < 0.35f)
                {
                    continue;
                }

                float flameRadius = 26f * flameScale;
                Vector2 closest = Vector2.Clamp(flamePosition, targetHitbox.TopLeft(), targetHitbox.BottomRight());
                float distance = Vector2.Distance(flamePosition, closest);
                if (distance <= flameRadius)
                {
                    return true;
                }
            }
            return false;
        }

        // The Destined Death blaze sprites promise this: each hit adds one Destined Death stack
        // (DestinedDeath.ReApply increments Stacks on every AddBuff) and sets the player on fire.
        // Runs on the hit player's machine; buff sync carries it.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), DestinedDeath.DefaultDurationTicks);
            target.AddBuff(BuffID.OnFire, 3 * 60);
        }

        // Rising blaze flames: each one is born at the floor somewhere across the patch, spins as it
        // climbs straight up past the plume's ~150px top, and fades out on the way. Stateless: every
        // flame's cycle comes from the projectile's own timer plus a hash of (flame, cycle), so nothing
        // is stored and every machine gets the same flames for both drawing and collision.
        const int FlameCount = 14;
        const int FlameLifeTicks = 54;
        const float FlameRiseHeight = 200f;
        const float FlameSpinTurns = 1.1f; // full turns over one flame's life
        const int FlameTicksPerFrame = 6;

        bool TryGetFlame(int i, out Vector2 position, out float scale, out float rotation, out float opacity, out int frameIndex)
        {
            position = Vector2.Zero;
            scale = 0f;
            rotation = 0f;
            opacity = 0f;
            frameIndex = 2;

            // Births are staggered evenly so a new flame appears below while older ones are still rising.
            float birthDelay = i * FlameLifeTicks / (float)FlameCount;
            float flameTime = Timer - birthDelay;
            if (flameTime < 0f)
            {
                return false;
            }

            int cycle = (int)(flameTime / FlameLifeTicks);
            float cycleStart = birthDelay + cycle * FlameLifeTicks;
            if (cycleStart > BurnTicks)
            {
                return false; // no new flames once the patch starts guttering out
            }

            float life = (flameTime - cycle * FlameLifeTicks) / FlameLifeTicks;
            int seedIndex = i * 31 + cycle * 7 + Projectile.identity * 101;
            float seedX = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.HashToUnit(seedIndex);
            float seedSize = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.HashToUnit(seedIndex + 1);
            float seedSpin = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.HashToUnit(seedIndex + 2);

            float scatterHalfWidth = Projectile.width * 0.55f;
            float flameX = Projectile.Center.X + (seedX * 2f - 1f) * scatterHalfWidth;
            float riseHeight = FlameRiseHeight * (0.75f + 0.4f * seedSize);
            float flameY = Projectile.Bottom.Y - 8f - life * riseHeight;
            position = new Vector2(flameX, flameY);

            float spinDirection = 1f;
            if (seedSpin < 0.5f)
            {
                spinDirection = -1f;
            }
            rotation = seedSpin * MathHelper.TwoPi + spinDirection * life * FlameSpinTurns * MathHelper.TwoPi;

            // Big at the base, shrinking as it burns off. 12% fade-in so births never pop.
            scale = MathHelper.Lerp(0.85f, 0.4f, life) * (0.85f + 0.3f * seedSize);
            float fadeIn = MathHelper.Clamp(life / 0.12f, 0f, 1f);
            float fadeOut = (float)System.Math.Pow(1f - life, 1.3f);
            opacity = fadeIn * fadeOut;

            // Frames 2-4 only: frames 0-1 are tiny sparks, and cycling through them read as flicker.
            frameIndex = 2 + ((int)(flameTime / FlameTicksPerFrame) + i) % 3;
            return true;
        }

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            if (LinkedToHand && !DismissingWithHand && Timer > 2 && LinkedHandHasDeparted())
            {
                Projectile.ai[1] = 1f;
                Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, HandDismissTicks);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.netUpdate = true;
                }
            }

            float dismissalFade = DismissingWithHand
                ? MathHelper.Clamp(Projectile.timeLeft / (float)HandDismissTicks, 0f, 1f)
                : 1f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.16f, 0.1f) * dismissalFade);
            if (DismissingWithHand)
            {
                return;
            }
            if (Timer < BurnTicks && !Main.dedServ)
            {
                // 3-layer lingering fire column: 2 rising body flames + 1 fast ember + billowing dark smoke every 2t.
                for (int i = 0; i < 2; i++)
                {
                    int flameType = i == 0 ? DustID.RedTorch : DustID.OrangeTorch;
                    Vector2 flamePos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-46f, 46f), Main.rand.NextFloat(-16f, -2f));
                    Dust flame = Dust.NewDustPerfect(flamePos, flameType,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-4.2f, -1.8f)),
                        Main.rand.Next(35, 65), default, Main.rand.NextFloat(1.0f, 1.48f));
                    flame.noGravity = true;
                }

                Vector2 sparkPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-24f, -4f));
                Dust spark = Dust.NewDustPerfect(sparkPos, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-0.9f, 0.9f), Main.rand.NextFloat(-5.4f, -3.2f)),
                    Main.rand.Next(10, 35), default, Main.rand.NextFloat(0.55f, 0.85f));
                spark.noGravity = true;

                if (Timer % 2 == 0)
                {
                    Vector2 smokePos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-42f, 42f), Main.rand.NextFloat(-26f, -8f));
                    Dust smoke = Dust.NewDustPerfect(smokePos, DustID.Smoke,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.7f)),
                        155, new Color(28, 12, 16), Main.rand.NextFloat(0.55f, 0.8f));
                    smoke.noGravity = true;
                    smoke.noLight = true;
                    smoke.fadeIn = Main.rand.NextFloat(1.35f, 1.65f);
                }
            }
            else if (Timer == BurnTicks)
            {
                NitoVFX.PyreBurst(Projectile.Center, 22, 3.4f, 1.1f, 44f, 16f);
            }
        }

        bool LinkedHandHasDeparted()
        {
            if (LinkedHandIndex < 0 || LinkedHandIndex >= Main.maxProjectiles)
            {
                return true;
            }
            Projectile hand = Main.projectile[LinkedHandIndex];
            return !hand.active || hand.type != ModContent.ProjectileType<NitoGraveHand>()
                || hand.ModProjectile is not NitoGraveHand graveHand || graveHand.HasStartedMoving;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = DismissingWithHand
                ? MathHelper.Clamp(Projectile.timeLeft / (float)HandDismissTicks, 0f, 1f)
                : Timer < BurnTicks
                ? MathHelper.Clamp(Timer / 12f, 0f, 1f)                        // quick flare-up
                : MathHelper.Clamp(1f - (Timer - BurnTicks) / (float)FadeTicks, 0f, 1f); // smooth guttering
            float progress = MathHelper.Clamp(Timer / (float)BurnTicks, 0f, 1f);
            NitoVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 6f),
                new Vector2(Projectile.width * 1.3f, 46f), 1f, 0.72f * fade);
            NitoVFX.DrawGraveEruption(Projectile.Center,
                new Vector2(Projectile.width * 1.1f, Projectile.height * 1.5f), progress, 0.6f * fade);

            // The rising blaze flames (see TryGetFlame) drawn over the plume.
            const int BlazeFrameCount = 5;
            Texture2D blazeTexture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze))).Value;
            int blazeFrameHeight = blazeTexture.Height / BlazeFrameCount;
            Vector2 blazeOrigin = new Vector2(blazeTexture.Width * 0.5f, blazeFrameHeight * 0.5f);
            for (int i = 0; i < FlameCount; i++)
            {
                if (!TryGetFlame(i, out Vector2 flamePosition, out float flameScale, out float flameRotation,
                    out float flameOpacity, out int frameIndex))
                {
                    continue;
                }
                Rectangle blazeFrame = new Rectangle(0, frameIndex * blazeFrameHeight, blazeTexture.Width, blazeFrameHeight);
                Vector2 flameScreenPosition = flamePosition - Main.screenPosition;
                Main.EntitySpriteDraw(blazeTexture, flameScreenPosition, blazeFrame, Color.White * (flameOpacity * fade),
                    flameRotation, blazeOrigin, flameScale, SpriteEffects.None, 0);
            }
            return false;
        }
    }

    ///<summary>
    ///A grave-spike raining down from above (GravelordSpike.png) — the ceiling/sky counterpart to
    ///NitoBoneShard's shot-and-thrown fans. Used by Sword Rain and Gravelord Judgment, both of which
    ///are already conceptually "things falling from above the player."
    ///</summary>
    class NitoCeilingSpike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordSpike";

        int DelayTicks => (int)Projectile.ai[0];
        // ai[1] = world Y of the arena floor (Nito's feet when last grounded). 0 = always hit tiles.
        float ArenaFloorY => Projectile.ai[1];
        int Timer => (int)Projectile.localAI[0];

        // Tile collision switches on this far above the floor line, so the spike meets the floor
        // surface even when Nito's terrain sink put his feet a little below it.
        const float FloorCatchMargin = 48f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.aiStyle = 0;
        }

        public override bool ShouldUpdatePosition() => Timer > DelayTicks;

        public override bool? CanDamage() => Timer > DelayTicks;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // sprite points down at rest

            // Falls through ceilings, ledges and blocks above the fight, then lands on the arena floor.
            // Every machine runs this from synced ai[1] and position, so tileCollide agrees everywhere.
            bool reachedFloorLevel = Projectile.Bottom.Y >= ArenaFloorY - FloorCatchMargin;
            Projectile.tileCollide = ArenaFloorY <= 0f || reachedFloorLevel;

            if (Timer <= DelayTicks)
            {
                Lighting.AddLight(Projectile.Center, 0.22f, 0.18f, 0.3f);
                return;
            }
            if (Timer == DelayTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.65f, Pitch = -0.15f }, Projectile.Center);
            }
            Projectile.velocity.Y += 0.22f;
            if (Projectile.velocity.Y > 15f)
            {
                Projectile.velocity.Y = 15f;
            }
            Lighting.AddLight(Projectile.Center, 0.18f, 0.1f, 0.12f);

            if (!Main.dedServ)
            {
                // Interpolate 3 body motes across the fall step (<= 5px gap at 15 px/t) + 1 edge glint.
                const int FallTrailCount = 3;
                for (int i = 0; i < FallTrailCount; i++)
                {
                    float fraction = (i + Main.rand.NextFloat()) / FallTrailCount;
                    Vector2 trailPos = Projectile.Center - Projectile.velocity * fraction + Main.rand.NextVector2Circular(5f, 5f);
                    int dustType = i == 0 ? DustID.RedTorch : DustID.BoneTorch;
                    Dust trail = Dust.NewDustPerfect(trailPos, dustType,
                        Projectile.velocity * -0.1f + Main.rand.NextVector2Circular(0.45f, 0.45f),
                        Main.rand.Next(35, 60), default, Main.rand.NextFloat(0.85f, 1.3f));
                    trail.noGravity = true;
                }

                Dust glint = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.AncientLight, Projectile.velocity * -0.15f, Main.rand.Next(0, 26),
                    new Color(235, 210, 220), Main.rand.NextFloat(0.5f, 0.85f));
                glint.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Timer <= DelayTicks && DelayTicks > 0)
            {
                float progress = MathHelper.Clamp(Timer / (float)DelayTicks, 0f, 1f);
                NitoVFX.DrawRainPortal(Projectile.Center, new Vector2(118f, 54f), progress, 0.92f);
            }
            else
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                NitoVFX.DrawSoulTrail(Projectile.Center - direction * 28f, direction.ToRotation(),
                    new Vector2(92f, 26f), 0.45f, 0.55f, Projectile.identity * 0.437f);
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f, Pitch = -0.3f }, Projectile.Center);

            // 3-layer ground shatter (~30 motes): 14 body + 8 fast sparks + 4 billowing dark smoke + 4 stone chips.
            for (int i = 0; i < 14; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                int dustType = i % 3 == 0 ? DustID.RedTorch : DustID.BoneTorch;
                Dust body = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(2f, 10f),
                    dustType, direction * Main.rand.NextFloat(1.8f, 4.8f) + new Vector2(0f, -1.2f),
                    Main.rand.Next(35, 60), default, Main.rand.NextFloat(0.95f, 1.4f));
                body.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                int sparkType = i % 2 == 0 ? DustID.Torch : DustID.AncientLight;
                Dust spark = Dust.NewDustPerfect(Projectile.Center, sparkType,
                    direction * Main.rand.NextFloat(4.5f, 8.2f) + new Vector2(0f, -1.5f),
                    Main.rand.Next(0, 25), default, Main.rand.NextFloat(0.5f, 0.85f));
                spark.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(2f, 10f),
                    DustID.Smoke, direction * Main.rand.NextFloat(0.6f, 1.8f) + new Vector2(0f, -0.8f),
                    155, new Color(42, 36, 46), Main.rand.NextFloat(0.55f, 0.78f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.6f);
            }

            for (int i = 0; i < 4; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Stone, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-5.5f, -2f)),
                    20, default, Main.rand.NextFloat(0.9f, 1.35f));
            }
        }
    }

    class NitoMiasmaCloud : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 190; // was 95 — lingers twice as long
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            // 0.98 -> 0.995: at the old drag the (already slow) cloud shed most of its speed within
            // half a second and stalled almost on top of Nito. Combined with the doubled launch speed
            // this is what actually carries the breath across the arena.
            Projectile.velocity *= 0.995f;
            Projectile.rotation += 0.03f;

            if (!Main.dedServ)
            {
                // Unthrottled 2-mote body trail interpolated across velocity, in the Miasma violet and pink
                // (RainbowMk2 takes its colour from the dust colour). Billowing dark-violet Smoke carries the gas.
                const int CloudTrailCount = 2;
                for (int i = 0; i < CloudTrailCount; i++)
                {
                    float fraction = (i + Main.rand.NextFloat()) / CloudTrailCount;
                    Vector2 trailPos = Projectile.Center - Projectile.velocity * fraction + Main.rand.NextVector2Circular(24f, 24f);
                    Color bodyColor = MiasmaPlayer.MiasmaColor;
                    if (i == 0)
                    {
                        bodyColor = MiasmaPlayer.MiasmaPinkColor;
                    }
                    Dust body = Dust.NewDustPerfect(trailPos, DustID.RainbowMk2,
                        Projectile.velocity * -0.06f + Main.rand.NextVector2Circular(0.9f, 0.9f),
                        Main.rand.Next(40, 65), bodyColor, Main.rand.NextFloat(0.85f, 1.4f));
                    body.noGravity = true;
                }

                if ((int)Projectile.localAI[0] % 2 == 0)
                {
                    Dust smog = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                        DustID.Smoke, Projectile.velocity * -0.04f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                        155, new Color(34, 14, 46), Main.rand.NextFloat(0.55f, 0.8f));
                    smog.noGravity = true;
                    smog.noLight = true;
                    smog.fadeIn = Main.rand.NextFloat(1.35f, 1.6f);
                }
            }

            Lighting.AddLight(Projectile.Center, MiasmaPlayer.MiasmaColor.ToVector3() * 0.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / 190f, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);
            NitoVFX.DrawMiasma(Projectile.Center, new Vector2(74f, 70f), Projectile.rotation,
                progress, 0.88f * fade, Projectile.identity * 0.613f, miasmaPalette: true);
            return true;
        }

        public const int MiasmaBuildupPerHit = 35;

        // Runs on the hit player's machine; MiasmaBuildup.Apply reports the gain to the server, which owns the meter.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Buffs.Debuffs.MiasmaBuildup.Apply(target, MiasmaBuildupPerHit,
                global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.MiasmaProcTicks);
            target.AddBuff(BuffID.Darkness, 3 * 60);
        }
    }

    ///<summary>
    ///Short-range ground-slam dust and dirt-clod cloud thrown forward by Nito's downward Overhead Cleave.
    ///Punishes backward rolling out of the heavy cleave with 2s Stagger and 2s Slow while staying cleanly
    ///rollable (14 live ticks &lt; 22-tick roll i-frames) or jumpable. Sold entirely by gravity dirt clods,
    ///3-layer ash dust, and dark-gray NitoImpactCloud smoke gores (retinted ChaosImpactCloud).
    ///ai[0] = facing direction (-1/1), ai[1] = phaseTwo (1 = phase two ember accents).
    ///</summary>
    class NitoSlamCloud : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const int LiveTicks = 14;
        const int TotalTicks = 28;
        const int ImpactDebuffTicks = 2 * 60;

        int Direction => Projectile.ai[0] >= 0f ? 1 : -1;
        bool PhaseTwo => Projectile.ai[1] > 0.5f;
        int Timer => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 164;
            Projectile.height = 86;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Spawned at the floor anchor; lift the box so its bottom sits flush on the ground and roll forward.
            Projectile.position.Y -= Projectile.height;
            Projectile.velocity = new Vector2(Direction * 4.5f, 0f);
        }

        public override bool? CanDamage() => Timer <= LiveTicks ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.X *= 0.90f;
            Projectile.velocity.Y = 0f;

            if (Main.dedServ)
            {
                return;
            }

            if (Timer == 1)
            {
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 0.85f, Pitch = -0.35f }, Projectile.Bottom);
                UsefulFunctions.ScreenShake(Projectile.Bottom, 6.5f, 14, 6f, 450f);

                // Dirt clods and stone grit kicked up off the floor with gravity ON so they arc and shower down.
                for (int i = 0; i < 28; i++)
                {
                    Vector2 clodPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-64f, 64f), Main.rand.NextFloat(-6f, 2f));
                    Vector2 clodVel = new Vector2(Direction * Main.rand.NextFloat(1.2f, 6.8f) + Main.rand.NextFloat(-2.4f, 2.4f),
                        Main.rand.NextFloat(-9.5f, -3.2f));
                    Dust.NewDustPerfect(clodPos, DustID.Dirt, clodVel, 0, default, Main.rand.NextFloat(1.05f, 1.55f));
                }

                for (int i = 0; i < 14; i++)
                {
                    Vector2 gritPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-56f, 56f), Main.rand.NextFloat(-4f, 2f));
                    Vector2 gritVel = new Vector2(Direction * Main.rand.NextFloat(1.6f, 7.2f) + Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-8.2f, -2.5f));
                    Dust.NewDustPerfect(gritPos, DustID.Stone, gritVel, 20, default, Main.rand.NextFloat(0.95f, 1.45f));
                }

                // Dark-gray smoke gores (retinted Chaos ground-slam clouds) erupting up and forward.
                int[] goreTypes =
                {
                    ModContent.GoreType<NitoImpactCloud1>(),
                    ModContent.GoreType<NitoImpactCloud2>(),
                    ModContent.GoreType<NitoImpactCloud3>(),
                };
                for (int i = 0; i < 5; i++)
                {
                    Vector2 gorePos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-52f, 52f), Main.rand.NextFloat(-28f, -10f));
                    Vector2 goreVel = new Vector2(Direction * Main.rand.NextFloat(1.4f, 4.2f) + Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(-3.8f, -1.2f));
                    Gore.NewGore(Projectile.GetSource_FromThis(), gorePos, goreVel,
                        goreTypes[i % goreTypes.Length], Main.rand.NextFloat(0.85f, 1.25f));
                }

                // Midground billowing dark-gray ash cloud + foreground sparks for crisp impact readability.
                for (int i = 0; i < 20; i++)
                {
                    Vector2 cloudPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-68f, 68f), Main.rand.NextFloat(-28f, -2f));
                    Vector2 cloudVel = new Vector2(Direction * Main.rand.NextFloat(1.8f, 5.4f) + Main.rand.NextFloat(-1.4f, 1.4f),
                        Main.rand.NextFloat(-3.6f, -0.6f));
                    Dust ash = Dust.NewDustPerfect(cloudPos, DustID.Smoke, cloudVel, 145,
                        new Color(62, 58, 66), Main.rand.NextFloat(0.6f, 0.85f));
                    ash.noGravity = true;
                    ash.noLight = true;
                    ash.fadeIn = Main.rand.NextFloat(1.4f, 1.75f);
                }

                for (int i = 0; i < 12; i++)
                {
                    Vector2 sparkPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-52f, 52f), Main.rand.NextFloat(-18f, -2f));
                    Vector2 sparkVel = new Vector2(Direction * Main.rand.NextFloat(3.5f, 8.5f), Main.rand.NextFloat(-5.2f, -1.2f));
                    int sparkType = PhaseTwo ? DustID.RedTorch : DustID.BoneTorch;
                    if (i % 3 == 0)
                    {
                        sparkType = DustID.AncientLight;
                    }

                    Dust spark = Dust.NewDustPerfect(sparkPos, sparkType, sparkVel, Main.rand.Next(0, 35),
                        default, Main.rand.NextFloat(0.55f, 0.95f));
                    spark.noGravity = true;
                }
            }

            if (Timer <= LiveTicks)
            {
                // Rolling ground cloud while the hitbox is live so visual volume matches the active window.
                for (int i = 0; i < 3; i++)
                {
                    Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-72f, 72f), Main.rand.NextFloat(-42f, -4f));
                    Vector2 dustVel = new Vector2(Direction * Main.rand.NextFloat(1.2f, 3.6f), Main.rand.NextFloat(-2.4f, -0.4f));
                    if (i == 0)
                    {
                        Dust clod = Dust.NewDustPerfect(dustPos, DustID.Dirt,
                            new Vector2(Direction * Main.rand.NextFloat(1.5f, 4.2f), Main.rand.NextFloat(-5.5f, -2.2f)),
                            0, default, Main.rand.NextFloat(0.95f, 1.35f));
                        clod.noGravity = false;
                    }
                    else
                    {
                        Dust smoke = Dust.NewDustPerfect(dustPos, DustID.Smoke, dustVel, 145,
                            new Color(58, 56, 62), Main.rand.NextFloat(0.58f, 0.82f));
                        smoke.noGravity = true;
                        smoke.noLight = true;
                        smoke.fadeIn = Main.rand.NextFloat(1.35f, 1.65f);
                    }
                }

                if (Timer == 6 || Timer == 12)
                {
                    int variant = Main.rand.Next(3);
                    int goreType = ModContent.GoreType<NitoImpactCloud3>();
                    if (variant == 0)
                    {
                        goreType = ModContent.GoreType<NitoImpactCloud1>();
                    }
                    else if (variant == 1)
                    {
                        goreType = ModContent.GoreType<NitoImpactCloud2>();
                    }
                    Vector2 trailPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-44f, 44f), Main.rand.NextFloat(-24f, -8f));
                    Vector2 trailVel = new Vector2(Direction * Main.rand.NextFloat(1.2f, 3.2f), Main.rand.NextFloat(-2.8f, -1f));
                    Gore.NewGore(Projectile.GetSource_FromThis(), trailPos, trailVel, goreType, Main.rand.NextFloat(0.75f, 1.05f));
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Stagger.Apply(target, ImpactDebuffTicks);
            target.AddBuff(BuffID.Slow, ImpactDebuffTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
