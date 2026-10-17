using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Death;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Death
{
    /// <summary>
    /// Damageable Reaper summon used by Death's second phase formation attacks.
    /// ai[0] = owner NPC index
    /// ai[1] = signed slot number; positive rotates clockwise, negative counter-clockwise
    /// ai[2] = formation center X
    /// ai[3] = formation center Y
    /// Strong5 charge mode repurposes ai[2] as the spawn angle and ai[3] as the locked dash angle.
    /// Strong6 squad mode (bit 19) repurposes the low byte of ai[1] as the firing-line slot 1-4 and
    /// keeps ai[2]/ai[3] as the fixed anchor the summon holds for the whole move.
    /// </summary>
    class DeathReaper : ModNPC
    {
        const float FormationRadius = 300f;
        const float OrbitRadiansPerSecond = MathHelper.PiOver2;
        const float SpiralRadiusPerSecond = -300f;
        const float LaserSpeed = 20f;
        const int LaserDamage = 333;
        const int FormationTicks = 60;
        const int IdleEndTicks = 90;
        internal const int OrbitEndTicks = 330;
        const int FreezeEndTicks = 420;
        const int SpiralEndTicks = 540;
        const int LifetimeTicks = 600;
        const int ScatterModeBit = 1 << 17;
        internal const int Strong5ChargeModeBit = 1 << 18;
        const int Strong5OrbitTicks = 45;
        const int Strong5WaitTicks = 15;

        const float Strong5OrbitRadius = 100f;
        const float Strong5MaxSpeed = 20f;
        const float Strong5SickleMaxSpeed = 10f;
        const float Strong5SickleMaxDistanceFromOwner = 1400f;
        // Distance a rushing reaper covers while fading out, so the fade keeps pace on every client.
        const float Strong5FadeTravelDistance = 600f;

        internal const int Strong6ModeBit = 1 << 19;
        // Strong move 6 (Absolute Death only): #1/#4 sweep cardinal fans, #2/#3 fire aimed
        // bolt strings and finish with synchronised pincers. All timings are relative to the move's
        // attack phase, which the owner exposes through Strong6AttackStartTicks/EndTicks.
        const int Strong6CardinalIntervalTicks = 12;
        const float Strong6CardinalSpeed = 8f;
        const float Strong6CardinalRotationDegreesPerSecond = 90f;
        const int Strong6FreeFireEndTicks = 4 * 60;
        const int Strong6StreamShotCount = 5;
        const int Strong6StreamDurationTicks = 20;
        const int Strong6StreamShotIntervalTicks = Strong6StreamDurationTicks / (Strong6StreamShotCount - 1);
        const float Strong6BoltSpeed = 15f;
        const float Strong6PincerSpreadDegrees = 30f;
        const int Strong6SoloPincerFirstTick = 270;
        const int Strong6SoloPincerSecondTick = 330;
        const int Strong6JointPincerFirstTick = 390;
        const int Strong6JointPincerSecondTick = 450;

        public override string Texture => "Terraria/Images/NPC_" + NPCID.Reaper;
        // Placeholder sprite: the vanilla Reaper sheet stands in until a bespoke one exists.

        int EncodedSlot => (int)NPC.ai[1];
        int Slot => EncodedSlot & 0xFF;
        int SlotCount => System.Math.Max(1, (EncodedSlot >> 8) & 0xFF);
        int RotationDirection => (EncodedSlot & (1 << 16)) != 0 ? 1 : -1;
        bool ScatterMode => (EncodedSlot & ScatterModeBit) != 0;
        bool Strong5ChargeMode => (EncodedSlot & Strong5ChargeModeBit) != 0;
        bool Strong6Mode => (EncodedSlot & Strong6ModeBit) != 0;
        /// <summary>Strong-move-6 firing-line slot: 1-4, numbered left to right.</summary>
        int Strong6Index => Slot;
        int Age => (int)NPC.localAI[0];
        Vector2 FormationCenter => new Vector2(NPC.ai[2], NPC.ai[3]);
        float BaseAngle => Slot * MathHelper.TwoPi / SlotCount;
        float intendedFacingAngle;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 0f;
            NPC.width = 32;
            NPC.height = 48;
            NPC.lifeMax = 1200;
            NPC.life = 1200;
            NPC.damage = 74;
            NPC.defense = 30;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = false;
            NPC.friendly = false;
            NPC.dontTakeDamage = false;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.chaseable = true;
            NPC.ShowNameOnHover = true;
            // Vanilla Reaper's sounds (NPCID.Reaper sets NPCHit49 / NPCDeath51 in NPC.SetDefaults).
            NPC.HitSound = SoundID.NPCHit49;
            NPC.DeathSound = SoundID.NPCDeath51;
        }

        public override void AI()
        {
            DeathBossBase owner = GetOwnerBoss();
            if (owner == null)
            {
                NPC.active = false;
                return;
            }

            if (Age == 0)
            {
                ConfigureOwnerStats(owner);
            }

            NPC.localAI[0]++;
            if (Age % 30 == 0)
            {
                NPC.netUpdate = true;
            }
            NPC.velocity = Vector2.Zero;

            Color lightColor = GetLightColor();
            Lighting.AddLight(NPC.Center, lightColor.ToVector3() * 2.2f);

            if (Strong5ChargeMode)
            {
                TickStrong5Charge(owner);
                ApplyConfiguredFacing(owner);
                UpdateContactDamageForFade(owner);
                return;
            }

            if (ScatterMode)
            {
                TickScatterReaper();
                ApplyConfiguredFacing(owner);
                UpdateContactDamageForFade(owner);
                return;
            }

            if (Strong6Mode)
            {
                TickStrong6Squad(owner);
                ApplyConfiguredFacing(owner);
                UpdateContactDamageForFade(owner);
                return;
            }

            if (Age < FormationTicks)
            {
                TickFormation();
            }
            else if (Age < IdleEndTicks)
            {
                SetPose(BaseAngle, FormationRadius);
                intendedFacingAngle = BaseAngle;
                NPC.alpha = 0;
            }
            else if (Age < OrbitEndTicks)
            {
                TickOrbit();
            }
            else if (Age < FreezeEndTicks)
            {
                SetPose(BaseAngle, FormationRadius);
                intendedFacingAngle = BaseAngle;
                if ((Age - OrbitEndTicks) % GetLaserInterval() == 0)
                {
                    FireConfiguredLasers(BaseAngle);
                }
            }
            else if (Age < SpiralEndTicks)
            {
                TickSpiral();
            }
            else if (Age < LifetimeTicks)
            {
                TickFadeOutSpiral();
            }
            else
            {
                NPC.active = false;
            }

            ApplyConfiguredFacing(owner);
            UpdateContactDamageForFade(owner);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float intensity = Strong5ChargeMode && Age >= Strong5OrbitTicks ? 1.15f : 1f;
            Color glowColor = GetOwnerBoss()?.VfxGlowColor ?? new Color(140, 35, 205);
            Texture2D texture = TextureAssets.Npc[NPCID.Reaper].Value;
            // Vanilla flips this sheet when spriteDirection == 1; using anything else mirrors the aura
            // against the Reaper it is drawn behind.
            SpriteEffects effects = DeathVFX.GetVanillaNpcFlip(NPC.spriteDirection);
            Vector2 position = NPC.Center + new Vector2(0f, NPC.gfxOffY);
            Vector2 origin = NPC.frame.Size() * 0.5f;

            // Shader-driven aura, shaped by the Reaper's own frame.
            DeathVFX.DrawShaderGlow(
                texture,
                NPC.frame,
                position,
                NPC.rotation,
                origin,
                NPC.scale,
                effects,
                glowColor,
                NPC.Opacity * intensity,
                haloRadius: 3.5f,
                haloDirections: 10,
                bloomScale: 1.25f,
                bloomOpacity: 0.28f,
                rimOpacity: 0.55f,
                coreOpacity: 0.45f,
                coreWhiteMix: 0.3f,
                phase: Slot * 0.7f);
            return true;
        }

        void TickStrong5Charge(DeathBossBase owner)
        {
            if (!owner.NPC.HasValidTarget)
            {
                NPC.active = false;
                return;
            }

            Player target = Main.player[owner.NPC.target];
            float spawnAngle = NPC.ai[2];
            Vector2 spawnDirection = spawnAngle.ToRotationVector2();

            if (Age < Strong5OrbitTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, Age / (float)Strong5OrbitTicks);
                NPC.Center = Vector2.Lerp(owner.NPC.Center, owner.NPC.Center + spawnDirection * Strong5OrbitRadius, progress);
                NPC.velocity = Vector2.Zero;
                intendedFacingAngle = spawnAngle;
                NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 15f, 0f, 1f));
                return;
            }

            int dashStartAge = Strong5OrbitTicks + Strong5WaitTicks;
            if (Age == Strong5OrbitTicks)
            {
                NPC.Center = owner.NPC.Center + spawnDirection * Strong5OrbitRadius;
                NPC.velocity = Vector2.Zero;
                NPC.ai[3] = (target.Center - NPC.Center).ToRotation();
                NPC.alpha = 0;
                NPC.netUpdate = true;
            }

            if (Age < dashStartAge)
            {
                NPC.Center = owner.NPC.Center + spawnDirection * Strong5OrbitRadius;
                NPC.velocity = Vector2.Zero;
                intendedFacingAngle = (target.Center - NPC.Center).ToRotation();
                NPC.alpha = 0;
                return;
            }

            float dashAngle = NPC.ai[3];
            Vector2 dashDirection = dashAngle.ToRotationVector2();
            float accelerationProgress = MathHelper.Clamp(
                (Age - dashStartAge) / Math.Max(1f, owner.ReaperStrong5AccelerationTicks), 0f, 1f);
            float speed = MathHelper.SmoothStep(0f, 1f, accelerationProgress) * Strong5MaxSpeed;
            NPC.velocity = dashDirection * speed;
            intendedFacingAngle = dashAngle;
            NPC.alpha = 0;

            if (owner.ReaperStrong5TrailSickleIntervalTicks > 0
                && (Age - dashStartAge) % owner.ReaperStrong5TrailSickleIntervalTicks == 0)
            {
                FireStrong5Sickle(owner, dashDirection);
            }

            // Dissolve once the rush has carried the reaper well past Death instead of circling back.
            float fadeDistance = owner.ReaperStrong5FadeDistance;
            float travelledPast = Vector2.Distance(NPC.Center, owner.NPC.Center) - fadeDistance;
            if (fadeDistance > 0f && travelledPast > 0f)
            {
                NPC.alpha = (int)MathHelper.Clamp(travelledPast / Strong5FadeTravelDistance * 255f, 0f, 255f);
                if (travelledPast >= Strong5FadeTravelDistance)
                {
                    NPC.active = false;
                }
            }
        }

        void FireStrong5Sickle(DeathBossBase owner, Vector2 direction)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Stop the trailing sickle volley once the reaper has left Death's side.
            const float maxDistanceSquared = Strong5SickleMaxDistanceFromOwner * Strong5SickleMaxDistanceFromOwner;
            if (Vector2.DistanceSquared(NPC.Center, owner.NPC.Center) > maxDistanceSquared)
            {
                return;
            }

            int index = Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction,
                ModContent.ProjectileType<DeathSickleProjectile>(),
                owner.ReaperSickleDamage,
                0f,
                Main.myPlayer,
                direction.X,
                direction.Y,
                Strong5SickleMaxSpeed);

            if (index >= 0 && index < Main.maxProjectiles)
            {
                Main.projectile[index].localAI[2] = 1f;
                Main.projectile[index].netUpdate = true;
            }
        }

        void TickScatterReaper()
        {
            DeathBossBase owner = GetOwnerBoss();
            if (owner == null)
            {
                NPC.active = false;
                return;
            }

            int layer = Slot / 3;
            int layerSlot = Slot % 3;
            float radius = layer == 0 ? owner.ScatterInnerRadius : owner.ScatterOuterRadius;
            // The two rings stay staggered by 60 degrees; the whole formation shares one random offset.
            float baseAngle = owner.ScatterFormationAngle
                + layerSlot * MathHelper.TwoPi / 3f
                + (layer == 1 ? MathHelper.Pi / 3f : 0f);

            if (Age < FormationTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, Age / (float)FormationTicks);
                Vector2 destination = owner.NPC.Center + baseAngle.ToRotationVector2() * radius;
                NPC.Center = Vector2.Lerp(FormationCenter, destination, progress);
                intendedFacingAngle = baseAngle;
                NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 45f, 0f, 1f));
                return;
            }

            // No orbiting any more: the summon holds its slot on the ring around the boss.
            NPC.Center = owner.NPC.Center + baseAngle.ToRotationVector2() * radius;
            intendedFacingAngle = baseAngle;
            NPC.alpha = 0;

            if (Age < owner.ScatterFadeStartTicks)
            {
                int fireStart = layer == 0 ? owner.ScatterInnerStartTicks : owner.ScatterOuterStartTicks;
                int fireInterval = Math.Max(1, owner.ScatterFireIntervalTicks);
                if (Age >= fireStart && (Age - fireStart) % fireInterval == 0)
                {
                    FireScatterSickles(baseAngle, owner, Age - fireStart);
                }
            }

            if (Age >= owner.ScatterFadeStartTicks)
            {
                if (Age >= owner.ScatterDurationTicks)
                {
                    NPC.active = false;
                    return;
                }

                float fade = (Age - owner.ScatterFadeStartTicks) / (float)(owner.ScatterDurationTicks - owner.ScatterFadeStartTicks);
                NPC.alpha = (int)MathHelper.Lerp(0f, 255f, fade);
            }
        }


        /// <summary>
        /// Two mirrored streams per summon: d is its fixed outward slot direction, and dt starts
        /// growing (+90 degrees per second) when the firing phase begins, so the streams swirl apart
        /// from the moment the volley starts rather than from the summon's spawn.
        /// </summary>
        void FireScatterSickles(float outwardAngle, DeathBossBase owner, int streamAge)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float swirl = MathHelper.ToRadians(owner.ScatterSwirlDegreesPerSecond)
                * Math.Max(0, streamAge) / 60f;
            FireScatterStream(outwardAngle + swirl, owner);
            FireScatterStream(outwardAngle - swirl, owner);
        }

        /// <summary>Two sickles per stream, both along the exact stream direction: fast then slow.</summary>
        void FireScatterStream(float angle, DeathBossBase owner)
        {
            FireSickleFan(angle, 1, 0f, owner.ScatterFastSickleSpeed, owner);
            FireSickleFan(angle, 1, 0f, owner.ScatterSlowSickleSpeed, owner);
        }

        void FireSickleFan(float angle, int count, float spreadDegrees, float speed, DeathBossBase owner)
        {
            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(spreadDegrees);
            float startAngle = angle - (count - 1) * spread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (startAngle + spread * i).ToRotationVector2();
                int index = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * speed,
                    ModContent.ProjectileType<DeathSickleProjectile>(),
                    owner.ReaperSickleDamage,
                    0f,
                    Main.myPlayer,
                    direction.X,
                    direction.Y,
                    speed);

                // localAI[2] = 2 marks the scatter volley, which is dropped after flying 1500px.
                if (index >= 0 && index < Main.maxProjectiles)
                {
                    Main.projectile[index].localAI[2] = 2f;
                    Main.projectile[index].netUpdate = true;
                }
            }
        }

        /// <summary>
        /// Strong move 6: four summons hold a fixed firing line either side of the boss for the
        /// whole eight-second attack. #1/#4 sweep cardinal fans, #2/#3 fire aimed strings.
        /// </summary>
        void TickStrong6Squad(DeathBossBase owner)
        {
            NPC.Center = FormationCenter;
            NPC.rotation = 0f;
            NPC.velocity = Vector2.Zero;

            int attackStart = owner.Strong6AttackStartTicks;
            int attackEnd = owner.Strong6AttackEndTicks;

            if (Age < attackStart)
            {
                // The line drops in with the boss and only opens fire after the one-second wait.
                NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 45f, 0f, 1f));
                intendedFacingAngle = GetPlayerAngle();
                return;
            }

            if (Age > attackEnd)
            {
                NPC.alpha = (int)MathHelper.Lerp(0f, 255f, MathHelper.Clamp(
                    (Age - attackEnd) / (float)Math.Max(1, owner.Strong6RecoveryTicks),
                    0f,
                    1f));
                if (Age >= attackEnd + owner.Strong6RecoveryTicks)
                {
                    NPC.active = false;
                }

                return;
            }

            NPC.alpha = 0;
            int elapsed = Age - attackStart;
            if (Strong6Index == 1 || Strong6Index == 4)
            {
                TickStrong6CardinalSweep(elapsed, owner);
            }
            else
            {
                TickStrong6AimedFire(elapsed, owner);
            }
        }

        /// <summary>
        /// Slots #1 and #4 spray four sickles along the cardinals around d1 every 12 ticks. d1
        /// starts pointing straight down and sweeps at 90 degrees per second: #1 counter-clockwise,
        /// #4 mirrored the other way.
        /// </summary>
        void TickStrong6CardinalSweep(int elapsed, DeathBossBase owner)
        {
            float sweep = MathHelper.ToRadians(Strong6CardinalRotationDegreesPerSecond) * elapsed / 60f;
            float baseAngle = Strong6Index == 1 ? MathHelper.PiOver2 - sweep : MathHelper.PiOver2 + sweep;
            intendedFacingAngle = baseAngle;

            if (elapsed % Strong6CardinalIntervalTicks != 0)
            {
                return;
            }

            for (int direction = 0; direction < 4; direction++)
            {
                FireSickleFan(
                    baseAngle + direction * MathHelper.PiOver2,
                    1,
                    0f,
                    Strong6CardinalSpeed,
                    owner);
            }
        }

        /// <summary>
        /// Slots #2 and #3: for the first four seconds each rolls its own 0.5-1.5s cadence and fires
        /// five-bolt strings, then solo pincers at 4.5s and 5.5s and two synchronised pincers at
        /// 6.5s and 7.5s.
        /// </summary>
        void TickStrong6AimedFire(int elapsed, DeathBossBase owner)
        {
            float playerAngle = GetPlayerAngle();
            intendedFacingAngle = playerAngle;

            if (elapsed < Strong6FreeFireEndTicks
                && TryGetStrong6FreeFireShot(elapsed, playerAngle, out float freeFireAngle))
            {
                FireStrong6Bolt(freeFireAngle, owner);
            }

            if (TryGetStrong6PincerShot(elapsed, playerAngle, owner, out float pincerAngle))
            {
                // Each pincer is two simultaneous five-bolt strings, one either side of the aim.
                float spread = MathHelper.ToRadians(Strong6PincerSpreadDegrees);
                FireStrong6Bolt(pincerAngle - spread, owner);
                FireStrong6Bolt(pincerAngle + spread, owner);
            }
        }

        void FireStrong6Bolt(float angle, DeathBossBase owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 direction = angle.ToRotationVector2();
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * Strong6BoltSpeed,
                ModContent.ProjectileType<DeathBolt>(),
                owner.ReaperBoltDamage,
                0f,
                Main.myPlayer,
                direction.X,
                direction.Y,
                owner.ReaperBoltColorValue);
        }

        /// <summary>
        /// Rebuilds the free-fire cadence from the move clock: each string locks onto the player
        /// once and then releases five bolts over 20 ticks. Deriving the schedule instead of
        /// storing counters keeps every client in step without extra sync slots.
        /// </summary>
        bool TryGetStrong6FreeFireShot(int elapsed, float playerAngle, out float angle)
        {
            angle = 0f;
            int eventTick = 0;
            for (int eventIndex = 0; eventTick < Strong6FreeFireEndTicks; eventIndex++)
            {
                eventTick += RollStrong6Interval(eventIndex);
                if (eventTick > elapsed)
                {
                    return false;
                }

                if (TryStrong6StringShotAt(elapsed, eventTick, playerAngle, out angle))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>0.5-1.5s cadence, deterministic per summon and event so clients agree.</summary>
        int RollStrong6Interval(int eventIndex)
        {
            return 30 + (int)(DeterministicRoll(NPC.whoAmI * 73856093 ^ eventIndex * 19349663) * 61f);
        }

        bool TryGetStrong6PincerShot(int elapsed, float playerAngle, DeathBossBase owner, out float angle)
        {
            // One of #2/#3 takes the first solo pincer and the other the second; both fire the
            // final two together. Both summons derive the same choice from the owner index.
            bool firstIsTwo = DeterministicRoll(owner.NPC.whoAmI * 73856093 ^ 0x5F3759DF) < 0.5f;
            bool isTwo = Strong6Index == 2;
            int soloTick = isTwo == firstIsTwo
                ? Strong6SoloPincerFirstTick
                : Strong6SoloPincerSecondTick;
            return TryStrong6StringShotAt(elapsed, soloTick, playerAngle, out angle)
                || TryStrong6StringShotAt(elapsed, Strong6JointPincerFirstTick, playerAngle, out angle)
                || TryStrong6StringShotAt(elapsed, Strong6JointPincerSecondTick, playerAngle, out angle);
        }

        /// <summary>
        /// A string is five bolts spread evenly across 20 ticks. The aim is locked on the string's
        /// first tick and reused for the rest of the burst, so the volley goes where the player was
        /// at release instead of following them.
        /// </summary>
        bool TryStrong6StringShotAt(int elapsed, int startTick, float playerAngle, out float angle)
        {
            angle = 0f;
            int shotTick = elapsed - startTick;
            if (shotTick < 0)
            {
                return false;
            }

            if (shotTick == 0)
            {
                NPC.localAI[3] = playerAngle;
                angle = playerAngle;
                return true;
            }

            if (shotTick > Strong6StreamDurationTicks || shotTick % Strong6StreamShotIntervalTicks != 0)
            {
                return false;
            }

            angle = NPC.localAI[3];
            return true;
        }

        float GetPlayerAngle()
        {
            DeathBossBase owner = GetOwnerBoss();
            if (owner != null && owner.NPC.HasValidTarget)
            {
                return (Main.player[owner.NPC.target].Center - NPC.Center).ToRotation();
            }

            return -MathHelper.PiOver2;
        }

        static float DeterministicRoll(int seed)
        {
            uint x = unchecked((uint)seed) * 747796405u + 2891336453u;
            x ^= x >> 15;
            x *= 2246822519u;
            x ^= x >> 13;
            return (x & 0xFFFFFFu) / 16777216f;
        }

        void TickFormation()
        {
            float progress = MathHelper.SmoothStep(0f, 1f, Age / (float)FormationTicks);
            Vector2 start = FormationCenter;
            Vector2 destination = FormationCenter + BaseAngle.ToRotationVector2() * FormationRadius;

            NPC.Center = Vector2.Lerp(start, destination, progress);
            intendedFacingAngle = BaseAngle;
            NPC.alpha = (int)MathHelper.Lerp(255f, 0f, MathHelper.Clamp(Age / 45f, 0f, 1f));
        }

        void TickOrbit()
        {
            int orbitAge = Age - IdleEndTicks;
            float angle = BaseAngle + RotationDirection * orbitAge / 60f * OrbitRadiansPerSecond;
            SetPose(angle, FormationRadius);
            intendedFacingAngle = angle;

            int burstInterval = GetBurstInterval();
            int groupAge = orbitAge % GetOrbitVolleyInterval();
            if (groupAge == 0)
            {
                FireConfiguredLasers(angle, allowMultiShot: false);
                NPC.localAI[1] = Math.Max(0, GetBurstShotCount() - 1);
                NPC.localAI[2] = burstInterval;
            }
            else
            {
                if (NPC.localAI[2] > 0f)
                {
                    NPC.localAI[2]--;
                }

                if (NPC.localAI[2] <= 0f && NPC.localAI[1] > 0f)
                {
                    FireConfiguredLasers(angle, allowMultiShot: false);
                    NPC.localAI[1]--;
                    NPC.localAI[2] = burstInterval;
                }
            }
        }

        void TickSpiral()
        {
            int spiralAge = Age - FreezeEndTicks;
            float angle = BaseAngle + RotationDirection * spiralAge / 60f * OrbitRadiansPerSecond;
            float radius = FormationRadius + spiralAge / 60f * SpiralRadiusPerSecond;
            SetPose(angle, radius);
            intendedFacingAngle = angle;

            if (spiralAge % GetLaserInterval() == 0)
            {
                FireConfiguredLasers(angle);
            }
        }

        void TickFadeOutSpiral()
        {
            int spiralAge = Age - FreezeEndTicks;
            float angle = BaseAngle + RotationDirection * spiralAge / 60f * OrbitRadiansPerSecond;
            float radius = FormationRadius + spiralAge / 60f * SpiralRadiusPerSecond;
            SetPose(angle, radius);
            intendedFacingAngle = angle;

            float fade = (Age - SpiralEndTicks) / (float)(LifetimeTicks - SpiralEndTicks);
            NPC.alpha = (int)MathHelper.Lerp(0f, 255f, fade);
        }

        void SetPose(float angle, float radius)
        {
            Vector2 radial = angle.ToRotationVector2();
            NPC.Center = FormationCenter + radial * radius;
        }


        void FireConfiguredLasers(float angle, bool allowMultiShot = true)
        {
            DeathBossBase owner = GetOwnerBoss();
            int count = allowMultiShot ? owner?.ReaperLaserCount ?? 1 : 1;
            if (count <= 1)
            {
                FireLaser(angle);
                return;
            }

            float spread = MathHelper.ToRadians(owner?.ReaperLaserSpreadDegrees ?? 0f);
            float startAngle = angle - (count - 1) * spread * 0.5f;
            for (int i = 0; i < count; i++)
            {
                FireLaser(startAngle + spread * i);
            }
        }

        int GetBurstInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperBurstIntervalTicks ?? 5);
        }

        int GetOrbitVolleyInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperOrbitVolleyIntervalTicks ?? 60);
        }

        int GetLaserInterval()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperLaserIntervalTicks ?? 8);
        }

        int GetBurstShotCount()
        {
            return Math.Max(1, GetOwnerBoss()?.ReaperBurstShotCount ?? 3);
        }

        void FireLaser(float angle)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 direction = angle.ToRotationVector2();
            DeathBossBase owner = GetOwnerBoss();
            if (owner != null && owner.ReaperStrong2UsesSickles)
            {
                float speed = owner.ReaperStrong2SickleSpeed;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * speed,
                    ModContent.ProjectileType<DeathSickleProjectile>(),
                    owner.ReaperSickleDamage,
                    0f,
                    Main.myPlayer,
                    direction.X,
                    direction.Y,
                    speed);
                return;
            }

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * LaserSpeed,
                ModContent.ProjectileType<DeathReaperLaser>(),
                LaserDamage,
                0f,
                Main.myPlayer);
        }

        DeathBossBase GetOwnerBoss()
        {
            int ownerIndex = (int)NPC.ai[0];
            // Treat an inactive owner slot as missing so reapers despawn with the boss.
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active)
            {
                return Main.npc[ownerIndex].ModNPC as DeathBossBase;
            }

            return null;
        }

        void ConfigureOwnerStats(DeathBossBase owner)
        {
            int difficultyMultiplier = Main.masterMode ? 3 : Main.expertMode ? 2 : 1;
            NPC.lifeMax = owner.ReaperLifeMax * difficultyMultiplier;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = NPC.lifeMax;
            }
            NPC.defense = owner.ReaperDefenseStat;
            EnemyDamage.SetContact(NPC, owner.ReaperContactDamage);
            NPC.netUpdate = true;
        }

        /// <summary>
        /// A summon is only "really there" once it is fully faded in: while NPC.alpha is non-zero it
        /// is fading in or out, so its contact damage is stripped for those frames.
        /// </summary>
        void UpdateContactDamageForFade(DeathBossBase owner)
        {
            if (NPC.alpha > 0)
            {
                if (NPC.damage != 0)
                {
                    NPC.damage = 0;
                    NPC.defDamage = 0;
                    NPC.netUpdate = true;
                }

                return;
            }

            EnemyDamage.SetContact(NPC, owner.ReaperContactDamage);
        }

        void ApplyConfiguredFacing(DeathBossBase owner)
        {
            NPC.rotation = 0f;
            float facingAngle = intendedFacingAngle;
            if (owner.CurrentReaperFacingMode == DeathBossBase.ReaperFacingMode.Player && owner.NPC.HasValidTarget)
            {
                Player target = Main.player[owner.NPC.target];
                facingAngle = (target.Center - NPC.Center).ToRotation();
            }

            int direction = facingAngle.ToRotationVector2().X < 0f ? -1 : 1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        Color GetLightColor()
        {
            return GetOwnerBoss()?.ReaperLightColor ?? Color.MediumPurple;
        }


        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            DeathBossBase owner = GetOwnerBoss();
            if (owner == null || !owner.NPC.active || owner.NPC.life <= 0)
            {
                return;
            }

            int playerIndex = NPC.lastInteraction;
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
            {
                return;
            }

            int damage = Math.Max(1, NPC.lifeMax / 2);
            int hitDirection = Math.Sign(owner.NPC.Center.X - NPC.Center.X);
            if (hitDirection == 0)
            {
                hitDirection = 1;
            }

            owner.NPC.SimpleStrikeNPC(damage, hitDirection, false, 0f, DamageClass.Generic);
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void FindFrame(int currentFrame)
        {
            NPC.frame.Y = 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            for (int i = 0; i < NPC.localAI.Length; i++)
            {
                writer.Write(NPC.localAI[i]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            for (int i = 0; i < NPC.localAI.Length; i++)
            {
                NPC.localAI[i] = reader.ReadSingle();
            }
        }
    }
}
