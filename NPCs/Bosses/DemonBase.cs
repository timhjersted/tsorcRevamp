using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    // Shared AI for the demon family — three tiers of one walking caster on the same 16-frame sheet:
    //   Oolacile Demon (Bosses/OolacileDemon, early PHM boss) · Defiled Demon (Bosses/DefiledDemon, late PHM boss)
    //   · Ancient Demon of the Abyss (Enemies/SuperHardMode/AncientDemon, SHM elite).
    // A tier supplies stats, its DemonTier (palette + on-hit debuff, Content/Projectiles/Enemy/DemonFamily), its signature
    // card(s), gores and loot; everything else lives here. Plan: tsorcDocs/AncientDemonFamily_RevampProposal.md.
    //
    // Neutral: FighterAI walk straight at the player (no kite band — the body itself is a threat that forces movement).
    // Every NeutralPause the server deals a card from an AttackBag:
    //   Demon Breath  mouth-ember tell (staggerable for 2/3, then a commit flash) → rises ~3 tiles, sweeps a breath stream
    //                 across the player → panting recovery. Phase 2 on T2/T3: a second, reversed sweep.
    //   Cinder Lob    horn gather (one pulse per shell) → 3/5/7 fireballs solved to land on and past the player.
    //   Demon Leap    skid back → a distance-solved leap that lands its body on the player (on a cooldown, and as a
    //                 gap-closer past LeapBagMaxRange) — a dodge check, not a reposition.
    //   + the tier's signature card(s).
    // Outside the bag: Punisher (one heavy shot when the player is past 40 tiles or 90t out of sight), Lost-Sight Blink (2s without sight,
    // 1s warning, checked every neutral tick), and the one-time phase-2 Roar at 50% life (it refills the bag).
    // The sheet only walks and jumps, so every tell is dust, light, an additive body flash and sound — off the body only
    // (no floor markers, aim lines or shrinking rings).
    // Poise: only the breath tell can be broken (AttackTelegraphing); other tells and actives are hyper-armored
    // (AttackCommitted); every recovery has no armor — those are the punish windows.
    // MP: the server deals cards and locks every aim (sweep angle/side, leap velocity, landing tick); those + state/timers
    // are synced in SendExtraAI and every machine runs the same timeline. Projectiles spawn on the server; dust/sound on clients.
    abstract class DemonBase : ModNPC, IStaggerable, IDebugAttackLabel, IHitReactor
    {
        protected enum DemonAttack : byte
        {
            None,
            Breath,
            CinderLob,
            Punisher,
            Leap,
            Roar,
            SpiritRing,   // T1 signature
            FireFountain, // T2 signature
            DoomOrb,      // T3 signature
            DeathsGrasp,  // T3 phase-2 signature
        }

        // Per-tier tables, indexed by (int)Tier = { Oolacile, Defiled, Ancient }. Ticks unless noted.
        static readonly int[] NeutralPauseTicks = { 150, 120, 100 };
        const int NeutralPauseRandom = 40;

        static readonly int[] BreathTellTicks = { 70, 60, 50 };
        static readonly int[] BreathSweepTicks = { 45, 60, 75 };
        static readonly float[] BreathDegreesPerTick = { 1.2f, 1.5f, 1.8f };
        static readonly int[] BreathRecoveryTicks = { 45, 40, 35 };
        const int BreathShotInterval = 3;
        const float BreathMaxRange = 420f;   // DemonBreath reach is 9 px/t x 50t = 450px
        const float BreathRiseVelocity = -2.4f; // net -2.1 px/t after gravity: ~48px (3 tiles) over the commit third of the tell
        const int BreathCeilingTiles = 7;    // no rise unless this much air sits above its head

        static readonly int[] LobCounts = { 3, 5, 7 };
        const int LobTellTicks = 35;
        const int LobInterval = 8;
        const int LobRecoveryTicks = 30;
        const float LobMinRange = 120f;
        const float LobMaxRange = 640f;
        // px along the demon→player line; positive = past the player. Shell i uses offset i.
        static readonly float[] LobLandingOffsets = { 0f, 60f, 110f, -40f, 160f, 20f, 200f };

        const int PunisherTellTicks = 40;
        const int PunisherRecoveryTicks = 30;
        const float PunisherMinRange = 640f;  // 40 tiles
        const int PunisherNoSightTicks = 90;  // 1.5s unseen; fires before the 2s blink

        static readonly int[] LeapTellTicks = { 30, 26, 22 };
        static readonly int[] LeapRecoveryTicks = { 35, 30, 25 };
        const int LeapCooldownTicks = 480;
        const float LeapBagMinRange = 100f;
        const float LeapBagMaxRange = 400f;   // 25 tiles; past this the leap is a gap-closer
        const float LeapGapCloserMaxRange = 640f;
        const float LeapHorizontalSpeed = 11f;    // airtime = distance / this, clamped below
        const int LeapMinAirTicks = 28;
        const int LeapMaxAirTicks = 50;           // 640px → 50t, vy ≈ -7.6, apex ≈ 6 tiles
        const int LeapBailTicks = 150;            // never landed (fell into a pit): end the attack anyway
        const float NpcGravity = 0.3f;            // vanilla NPC gravity, added after AI() each tick

        // Aggressive blink: 2s without sight → 1s warning (FighterAI's teleport mist) → appears near the player.
        const int BlinkNoSightTicks = 120;
        const int BlinkTellTicks = 60;
        const int BlinkRangeTiles = 25;

        const int RoarTicks = 60;
        const int FloorScanTiles = 25;

        // Sprite geometry: 204x138 frames, art faces LEFT, frame bottom drawn 4px below the hitbox bottom.
        const float FrameWidth = 204f;
        const float FrameHeight = 138f;
        const float MouthFrameX = 78f;
        const float MouthFrameY = 53f;
        const float HornsFrameX = 106f;
        const float HornsFrameY = 18f;

        protected DemonAttack State = DemonAttack.None;
        protected int AttackTimer;
        protected int AttackCooldown = 90;
        protected bool phaseTwo;
        float sweepCenterAngle;
        int sweepSide = 1;
        Vector2 leapVelocity;
        int leapLandedTick;

        // Local only, derived from the synced timeline each tick (no sync needed).
        bool breathHover;
        protected float bodyFlash; // 0..1 additive tint re-draw, set by tells each tick
        protected Color bodyFlashTint; // reset to the tier tint each tick; a tell may recolour it
        bool statsInitialized;

        // Server-only selection state (never synced; clients follow State).
        AttackBag<DemonAttack> attackBag;
        bool bagIsPhaseTwo;
        int leapCooldown = LeapCooldownTicks / 2;
        int noSightTicks;
        bool punisherFiredUnseen; // the 90t lost-sight shot already went this unseen stretch; cleared on regaining sight
        int hitReactLockout;

        protected NPCDespawnHandler despawnHandler;

        // Raw spawn damage per projectile family; a tier sets these in InitializeStats (world-flag dependent).
        protected int breathDamage;
        protected int lobDamage;
        protected int punisherDamage;

        protected abstract DemonTier Tier { get; }

        /// <summary>World-flag stats (NPC.downedBoss1 etc.) — unreliable in SetDefaults, so this runs on the first AI tick.</summary>
        protected abstract void InitializeStats();

        /// <summary>Add the tier's signature card(s) to a bag refill. phaseTwo = below 50% life (after the Roar).</summary>
        protected abstract void AddSignatureCards(List<DemonAttack> cards, bool isPhaseTwo);

        /// <summary>Server-only gate for a signature card. A closed card waits in the bag.</summary>
        protected abstract bool IsSignatureOpen(DemonAttack attack, float distance, bool lineOfSight);

        /// <summary>Run one tick of a signature attack. Must set the poise flags and call EndAttack when done.</summary>
        protected abstract void TickSignature(Player target);

        /// <summary>Death gores (client only).</summary>
        protected abstract void SpawnDeathGores();

        protected virtual float WalkSpeed => 1f;

        protected int TierIndex => (int)Tier;

        protected tsorcRevampGlobalNPC Poise => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

        protected Vector2 Mouth => FramePoint(MouthFrameX, MouthFrameY);

        protected Vector2 Horns => FramePoint(HornsFrameX, HornsFrameY);

        bool Leaping => State == DemonAttack.Leap && AttackTimer > LeapTellTicks[TierIndex] && leapLandedTick == 0;

        public string DebugAttackLabel
        {
            get
            {
                if (State == DemonAttack.None)
                {
                    return "Walk";
                }

                return DebugLabels.Humanize(State.ToString());
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        /// <summary>Levers every tier shares. Tier SetDefaults sets stats first, then calls this.</summary>
        protected void SetSharedDefaults()
        {
            AnimationType = NPCID.GoblinWarrior; // vanilla walk/jump framing for the 16-frame sheet
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;

        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write(phaseTwo);
            writer.Write(sweepCenterAngle);
            writer.Write((sbyte)sweepSide);
            writer.WriteVector2(leapVelocity);
            writer.Write(leapLandedTick);
            SendSignatureState(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (DemonAttack)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            phaseTwo = reader.ReadBoolean();
            sweepCenterAngle = reader.ReadSingle();
            sweepSide = reader.ReadSByte();
            leapVelocity = reader.ReadVector2();
            leapLandedTick = reader.ReadInt32();
            ReceiveSignatureState(reader);
        }

        /// <summary>A tier's own synced signature state (locked aims etc.). Read in the same order it is written.</summary>
        protected virtual void SendSignatureState(BinaryWriter writer)
        {
        }

        protected virtual void ReceiveSignatureState(BinaryReader reader)
        {
        }

        ///<summary>Poise break: whatever it was winding up or releasing is cancelled. Already-launched projectiles keep flying.</summary>
        public void OnStagger(NPC npc)
        {
            State = DemonAttack.None;
            AttackTimer = 0;
            leapLandedTick = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = 60;
                NPC.netUpdate = true;
            }
        }

        public override void AI()
        {
            if (!statsInitialized)
            {
                InitializeStats();
                statsInitialized = true;
            }

            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            tsorcRevampGlobalNPC globalNPC = Poise;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            bodyFlash = 0f;
            bodyFlashTint = DemonElement.TintColor(Tier);

            if (hitReactLockout > 0)
            {
                hitReactLockout--;
            }

            // Staggered: it reels, smoke pouring off its shoulders. No attacks; the global stagger handles the freeze.
            if (globalNPC.StaggerTimer > 0)
            {
                NPC.velocity.X *= 0.9f;
                EmitExhaust(1);
                return;
            }

            Player target = Main.player[NPC.target];
            bool targetValid = NPC.HasValidTarget && target.active && !target.dead;

            if (!targetValid)
            {
                State = DemonAttack.None;
                return;
            }

            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (lineOfSight)
                {
                    noSightTicks = 0;
                    punisherFiredUnseen = false;
                }
                else
                {
                    noSightTicks++;
                }
            }

            // Idle element read: an ember curls off the mouth every 6 ticks (every 3 after the phase-2 roar).
            int smoulderRate = 6;
            if (phaseTwo)
            {
                smoulderRate = 3;
            }

            if (!Main.dedServ && Main.rand.NextBool(smoulderRate))
            {
                Vector2 smoulderSpot = Mouth + Main.rand.NextVector2Circular(4f, 4f);
                Dust smoulder = Dust.NewDustPerfect(smoulderSpot, DemonElement.FlameDust(Tier), new Vector2(0f, -0.8f), 80, default, 0.9f);
                smoulder.noGravity = true;
            }

            if (State == DemonAttack.None)
            {
                // Neutral: walk (FighterAI also runs any queued blink), count down, then the server deals the next card.
                tsorcRevampAIs.FighterAI(NPC, WalkSpeed, 0.1f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false);

                tsorcRevampGlobalNPC neutralGlobal = Poise;
                bool blinkBusy = neutralGlobal.TeleportCountdown > 0 || neutralGlobal.TeleportAppearanceTimer > 0;

                // Lost-sight punisher: 90t unseen → one shot at the player's position, once per unseen stretch, even
                // mid-pause. It overrides the neutral cooldown; the blink then follows once the shot's recovery ends.
                bool unseenForPunisher = noSightTicks >= PunisherNoSightTicks && !punisherFiredUnseen;
                bool grounded = NPC.velocity.Y == 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient && unseenForPunisher && grounded && !blinkBusy)
                {
                    punisherFiredUnseen = true;
                    StartAttack(DemonAttack.Punisher);
                    return;
                }

                // Lost-Sight Blink, checked every neutral tick (not just when a card is due) so 2s unseen means 2s.
                if (Main.netMode != NetmodeID.MultiplayerClient && noSightTicks >= BlinkNoSightTicks && !blinkBusy)
                {
                    noSightTicks = 0;
                    tsorcRevampAIs.QueueTeleport(NPC, BlinkRangeTiles, true, BlinkTellTicks);
                    AttackCooldown = Math.Max(AttackCooldown, BlinkTellTicks + 30);
                    NPC.netUpdate = true;
                }

                if (leapCooldown > 0)
                {
                    leapCooldown--;
                }

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ChooseAttack(target, lineOfSight);
                }

                return;
            }

            AttackTimer++;

            switch (State)
            {
                case DemonAttack.Breath:
                    TickBreath(target);
                    break;
                case DemonAttack.CinderLob:
                    TickCinderLob(target);
                    break;
                case DemonAttack.Punisher:
                    TickPunisher(target);
                    break;
                case DemonAttack.Leap:
                    TickLeap();
                    break;
                case DemonAttack.Roar:
                    TickRoar();
                    break;
                default:
                    TickSignature(target);
                    break;
            }
        }

        // Server-only. Priority: the one-time Roar → punisher (far) → gap-closer leap → a card from the bag.
        // Nothing open → keep walking and look again in 20 ticks. (The blink is checked every neutral tick in AI.)
        void ChooseAttack(Player target, bool lineOfSight)
        {
            tsorcRevampGlobalNPC globalNPC = Poise;
            bool teleportBusy = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0;
            bool grounded = NPC.velocity.Y == 0f;

            if (teleportBusy || !grounded)
            {
                AttackCooldown = 10;
                return;
            }

            bool belowHalf = NPC.life <= NPC.lifeMax / 2;
            if (belowHalf && !phaseTwo)
            {
                phaseTwo = true;
                StartAttack(DemonAttack.Roar);
                return;
            }

            float distance = NPC.Distance(target.Center);
            bool farAway = distance > PunisherMinRange;

            if (farAway)
            {
                StartAttack(DemonAttack.Punisher);
                return;
            }

            bool playerGrounded = target.velocity.Y == 0f;
            bool gapToClose = distance > LeapBagMaxRange && distance <= LeapGapCloserMaxRange;
            if (gapToClose && leapCooldown == 0 && lineOfSight && playerGrounded)
            {
                StartAttack(DemonAttack.Leap);
                return;
            }

            if (attackBag == null)
            {
                attackBag = new AttackBag<DemonAttack>(FillAttackBag);
            }

            if (phaseTwo != bagIsPhaseTwo)
            {
                bagIsPhaseTwo = phaseTwo;
                attackBag.Clear();
            }

            bool targetHasFloor = TryFindFloor(target.Center.X, target.Bottom.Y - 32f, out _);

            bool IsOpen(DemonAttack attack)
            {
                if (attack == DemonAttack.Breath)
                {
                    return lineOfSight && distance <= BreathMaxRange;
                }

                if (attack == DemonAttack.CinderLob)
                {
                    bool inBand = distance >= LobMinRange && distance <= LobMaxRange;
                    return lineOfSight && inBand && targetHasFloor;
                }

                if (attack == DemonAttack.Leap)
                {
                    bool inBand = distance >= LeapBagMinRange && distance <= LeapBagMaxRange;
                    return leapCooldown == 0 && lineOfSight && inBand && playerGrounded;
                }

                return IsSignatureOpen(attack, distance, lineOfSight);
            }

            if (!attackBag.TryDraw(IsOpen, out DemonAttack picked))
            {
                AttackCooldown = 20;
                return;
            }

            StartAttack(picked);
        }

        // Breath and Lob x2 are the bread and butter; one Leap; then the tier's signature card(s).
        void FillAttackBag(List<DemonAttack> cards)
        {
            cards.Add(DemonAttack.Breath);
            cards.Add(DemonAttack.Breath);
            cards.Add(DemonAttack.CinderLob);
            cards.Add(DemonAttack.CinderLob);
            cards.Add(DemonAttack.Leap);
            AddSignatureCards(cards, bagIsPhaseTwo);
        }

        // Server-only.
        void StartAttack(DemonAttack attack)
        {
            State = attack;
            AttackTimer = 0;
            leapLandedTick = 0;

            if (attack == DemonAttack.Leap)
            {
                leapCooldown = LeapCooldownTicks;
            }

            NPC.netUpdate = true;
        }

        protected void EndAttack()
        {
            State = DemonAttack.None;
            AttackTimer = 0;
            leapLandedTick = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = NeutralPauseTicks[TierIndex] + Main.rand.Next(NeutralPauseRandom + 1);
                NPC.netUpdate = true;
            }
        }

        void TickBreath(Player target)
        {
            int tier = TierIndex;
            int tellTicks = BreathTellTicks[tier];
            int commitTick = tellTicks * 2 / 3;
            int sweepTicks = BreathSweepTicks[tier];

            // Phase 2 on the later tiers: sweep back the other way straight after the first pass.
            int sweepCount = 1;
            if (phaseTwo && Tier != DemonTier.Oolacile)
            {
                sweepCount = 2;
            }

            int sweepEnd = tellTicks + sweepTicks * sweepCount;
            int recoveryEnd = sweepEnd + BreathRecoveryTicks[tier];

            // Tell: plant and smoulder at the mouth, tracking the player. Breakable until the commit flash at 2/3, then
            // it rises ~3 tiles (if there's headroom) and the sweep locks on the last tick.
            if (AttackTimer <= tellTicks)
            {
                if (AttackTimer <= commitTick)
                {
                    Poise.AttackTelegraphing = true;
                }
                else
                {
                    Poise.AttackCommitted = true;
                }

                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f });
                }

                float progress = AttackTimer / (float)tellTicks;
                EmitMouthEmber(progress);
                bodyFlash = progress * 0.35f;

                if (AttackTimer == commitTick)
                {
                    breathHover = HasHeadroom(BreathCeilingTiles);
                    CommitCue(Mouth);
                }

                if (AttackTimer > commitTick && breathHover)
                {
                    NPC.velocity.Y = BreathRiseVelocity;
                }

                if (AttackTimer == tellTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    sweepCenterAngle = (target.Center - Mouth).ToRotation();
                    sweepSide = 1;
                    if (Main.rand.NextBool())
                    {
                        sweepSide = -1;
                    }

                    NPC.netUpdate = true;
                }

                return;
            }

            // Sweep: the aim crosses the locked line at BreathDegreesPerTick, centred on where the player stood.
            if (AttackTimer <= sweepEnd)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;

                // Hold the hover: -0.3 cancels the gravity vanilla adds after AI().
                if (breathHover)
                {
                    NPC.velocity.Y = -NpcGravity;
                }

                int sweepTick = AttackTimer - tellTicks - 1;
                int sweepIndex = sweepTick / sweepTicks;
                int ticksIntoSweep = sweepTick % sweepTicks;
                float degreesPerTick = BreathDegreesPerTick[tier];
                float totalDegrees = degreesPerTick * sweepTicks;
                float offsetDegrees = -totalDegrees * 0.5f + degreesPerTick * ticksIntoSweep;

                if (sweepIndex == 1)
                {
                    offsetDegrees = -offsetDegrees;
                }

                float angle = sweepCenterAngle + MathHelper.ToRadians(offsetDegrees) * sweepSide;
                Vector2 aim = angle.ToRotationVector2();
                FaceTowards(NPC.Center.X + aim.X);

                if (sweepTick % 12 == 0)
                {
                    PlaySound(SoundID.Item34 with { Volume = 0.6f });
                }

                Lighting.AddLight(Mouth, DemonElement.LightColor(Tier));

                bool shotTick = sweepTick % BreathShotInterval == 0;
                if (shotTick && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = aim * DemonBreath.Speed + Main.rand.NextVector2Circular(0.6f, 0.6f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, velocity, ModContent.ProjectileType<DemonBreath>(), breathDamage, 0f, Main.myPlayer, (float)Tier);
                }

                return;
            }

            // Recovery: lands, pants smoke. No armor — the punish window.
            NPC.velocity.X *= 0.9f;
            EmitExhaust(2);

            if (AttackTimer == sweepEnd + 1)
            {
                PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f });
            }

            if (AttackTimer >= recoveryEnd)
            {
                EndAttack();
            }
        }

        void TickCinderLob(Player target)
        {
            int lobCount = LobCounts[TierIndex];
            int lastLobTick = LobTellTicks + 1 + (lobCount - 1) * LobInterval;

            // Tell: fire gathers between the horns, one bright pulse per shell queued.
            if (AttackTimer <= LobTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.5f });
                }

                float progress = AttackTimer / (float)LobTellTicks;
                EmitGather(Horns, MathHelper.Lerp(36f, 6f, progress), progress);

                int pulseInterval = Math.Max(1, LobTellTicks / lobCount);
                if (AttackTimer % pulseInterval == 0)
                {
                    EmitBurst(Horns, 8, 2.5f);
                }

                return;
            }

            // Release: one shell every LobInterval, each re-aimed at the player's current spot plus its landing offset.
            if (AttackTimer <= lastLobTick)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                int ticksIntoLobs = AttackTimer - LobTellTicks - 1;
                if (ticksIntoLobs % LobInterval == 0)
                {
                    int lobIndex = ticksIntoLobs / LobInterval;
                    PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.3f });
                    EmitBurst(Horns, 10, 3f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        LaunchFireball(target, LobLandingOffsets[lobIndex], DemonFireball.ModeLob, lobDamage);
                    }
                }

                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= lastLobTick + LobRecoveryTicks)
            {
                EndAttack();
            }
        }

        void TickPunisher(Player target)
        {
            if (AttackTimer <= PunisherTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f });
                }

                float progress = AttackTimer / (float)PunisherTellTicks;
                EmitGather(Horns, MathHelper.Lerp(70f, 8f, progress), progress);
                bodyFlash = progress * 0.25f;

                if (AttackTimer == PunisherTellTicks)
                {
                    PlaySound(SoundID.Item73 with { Volume = 0.7f, Pitch = -0.4f });
                    EmitBurst(Horns, 20, 4f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        FirePunisher(target);
                    }
                }

                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= PunisherTellTicks + PunisherRecoveryTicks)
            {
                EndAttack();
            }
        }

        /// <summary>Server-only. The punisher's one shot, from the horns. Default: a heavy DemonFireball onto the player.</summary>
        protected virtual void FirePunisher(Player target)
        {
            LaunchFireball(target, 0f, DemonFireball.ModeHeavy, punisherDamage);
        }

        void TickLeap()
        {
            Player target = Main.player[NPC.target];
            int tellTicks = LeapTellTicks[TierIndex];
            Poise.AttackCommitted = true;

            // Tell: skids back ~8px, kicking dust off its feet, body flaring.
            if (AttackTimer <= tellTicks)
            {
                FaceTowards(target.Center.X);
                NPC.velocity.X = -NPC.direction * 0.27f;
                bodyFlash = 0.4f * AttackTimer / tellTicks;

                // Smoke kicked forward off the feet as it digs in.
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 kickVelocity = new Vector2(NPC.direction * Main.rand.NextFloat(1f, 3f), -Main.rand.NextFloat(0.5f, 2f));
                    Dust kick = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 6f), NPC.width, 6, DustID.Smoke, 0f, 0f, 100, default, 1.4f);
                    kick.velocity = kickVelocity;
                }

                if (AttackTimer == 1)
                {
                    PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.4f, Pitch = 0.3f });
                }

                if (AttackTimer == tellTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    leapVelocity = SolveLeap(target);
                    NPC.netUpdate = true;
                }

                return;
            }

            if (AttackTimer == tellTicks + 1)
            {
                NPC.velocity = leapVelocity;
                PlaySound(SoundID.Item32 with { Volume = 0.8f, Pitch = -0.5f });
                EmitBurst(NPC.Bottom, 14, 3f);
            }

            // Airborne: constant horizontal speed (aiStyle -1 has no air friction, but walls zero it), gravity does the arc.
            if (leapLandedTick == 0)
            {
                NPC.velocity.X = leapVelocity.X;
                if (NPC.collideX)
                {
                    leapVelocity.X = 0f;
                }

                if (!Main.dedServ)
                {
                    Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DemonElement.FlameDust(Tier), 0f, 0f, 80, default, 1.6f);
                    trail.noGravity = true;
                    trail.velocity = -NPC.velocity * 0.15f;
                }

                bool settled = AttackTimer > tellTicks + 4 && NPC.velocity.Y == 0f;
                bool bail = AttackTimer > tellTicks + LeapBailTicks;
                if ((settled || bail) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    leapLandedTick = AttackTimer;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Landed: thud, then a recovery with no armor.
            Poise.AttackCommitted = false;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer == leapLandedTick + 1)
            {
                PlaySound(SoundID.DeerclopsStep with { Volume = 0.9f });
                EmitBurst(NPC.Bottom, 24, 4f);
                if (!Main.dedServ)
                {
                    UsefulFunctions.ScreenShake(NPC.Bottom, 4f, 12, 6f, 600f);
                }
            }

            EmitExhaust(1);

            if (AttackTimer >= leapLandedTick + LeapRecoveryTicks[TierIndex])
            {
                EndAttack();
            }
        }

        // Server-only. Launch velocity that lands the demon's feet on the player's floor, centred on where they stood when
        // the skid tell ended — a body slam the player must roll or run out from under. Discrete solve for
        // velocity-then-position with gravity g after T ticks:
        //   dy = T·vy + g·T(T+1)/2  →  vy = (dy − g·T(T+1)/2) / T,  vx = dx / T.
        Vector2 SolveLeap(Player target)
        {
            float landingX = target.Center.X;

            float floorY;
            if (!TryFindFloor(landingX, target.Bottom.Y - 32f, out floorY))
            {
                floorY = target.Bottom.Y;
            }

            float deltaX = landingX - NPC.Center.X;
            float deltaY = floorY - NPC.Bottom.Y;
            int airTicks = (int)(Math.Abs(deltaX) / LeapHorizontalSpeed);
            airTicks = Math.Clamp(airTicks, LeapMinAirTicks, LeapMaxAirTicks);

            float gravityDrop = NpcGravity * airTicks * (airTicks + 1f) * 0.5f;
            Vector2 velocity;
            velocity.X = deltaX / airTicks;
            velocity.Y = (deltaY - gravityDrop) / airTicks;
            velocity.Y = Math.Max(velocity.Y, -12f);
            return velocity;
        }

        // Phase 2 begins: a one-time 60t hyper-armored roar. A fire shockwave rolls outward from the body; the palette
        // brightens from here on (EmitIdleSmoulder reads phaseTwo). The bag was flagged for refill when this was chosen.
        void TickRoar()
        {
            Poise.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            bodyFlash = 0.6f * (1f - AttackTimer / (float)RoarTicks);

            if (AttackTimer == 1)
            {
                PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.9f, Pitch = -0.2f });
                PlaySound(SoundID.Roar with { Volume = 0.8f });

                if (!Main.dedServ)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 8f, 30, 6f, 1200f);
                }
            }

            // Expanding fire ring for the first 24 ticks, 12px/tick (a shockwave, not a tell — it deals no damage).
            if (AttackTimer <= 24 && !Main.dedServ)
            {
                float radius = AttackTimer * 12f;
                int flameDust = DemonElement.FlameDust(Tier);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Dust ring = Dust.NewDustPerfect(NPC.Center + direction * radius, flameDust, direction * 3f, 60, default, 2f);
                    ring.noGravity = true;
                }
            }

            if (AttackTimer >= RoarTicks)
            {
                EndAttack();
            }
        }

        // Server-only. One fireball from the horns onto the floor under (player + offset along the demon→player line).
        // Same discrete solve as SolveLeap with DemonFireball.Gravity; airtime grows with distance.
        protected void LaunchFireball(Player target, float landingOffset, float mode, int damage)
        {
            float awayDirection = Math.Sign(target.Center.X - NPC.Center.X);
            if (awayDirection == 0f)
            {
                awayDirection = 1f;
            }

            float landingX = target.Center.X + landingOffset * awayDirection;
            float scanStartY = target.Bottom.Y - 32f;

            if (!TryFindFloor(landingX, scanStartY, out float floorY))
            {
                // No floor under that offset (a ledge edge): land on the player's own floor instead.
                landingX = target.Center.X;
                if (!TryFindFloor(landingX, scanStartY, out floorY))
                {
                    floorY = target.Bottom.Y;
                }
            }

            Vector2 origin = Horns;
            Vector2 landing = new Vector2(landingX, floorY - 8f);
            float flightTicks = MathHelper.Clamp(Math.Abs(landing.X - origin.X) / 6f, 28f, 60f);
            float gravityDrop = DemonFireball.Gravity * flightTicks * (flightTicks + 1f) * 0.5f;

            Vector2 velocity;
            velocity.X = (landing.X - origin.X) / flightTicks;
            velocity.Y = (landing.Y - origin.Y - gravityDrop) / flightTicks;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity, ModContent.ProjectileType<DemonFireball>(), damage, 0f, Main.myPlayer, (float)Tier, mode);
        }

        // The commit cue: a tier-coloured flash at the spot the attack comes from + a pitch-up chime. "Too late to interrupt."
        protected void CommitCue(Vector2 position)
        {
            PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.5f });

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float flashColor = UsefulFunctions.ColorToFloat(DemonElement.TintColor(Tier));
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<TelegraphFlash>(), 0, 0f, Main.myPlayer, flashColor);
            }
        }

        // World position of a pixel on the sprite frame. The frame is centred on the hitbox in X and its bottom sits 4px
        // below the hitbox bottom (vanilla NPC draw). Art faces left, so flipped (spriteDirection 1) mirrors X.
        Vector2 FramePoint(float frameX, float frameY)
        {
            float offsetX = (frameX - FrameWidth * 0.5f) * -NPC.spriteDirection;
            float worldY = NPC.Bottom.Y + 4f + NPC.gfxOffY - FrameHeight + frameY;
            return new Vector2(NPC.Center.X + offsetX, worldY);
        }

        // Face a world X. Art faces left and vanilla flips the sprite when spriteDirection == 1, so spriteDirection = facing.
        protected void FaceTowards(float worldX)
        {
            if (worldX > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }

            NPC.spriteDirection = NPC.direction;
        }

        // True if no solid tile sits within `tiles` above the head (across the hitbox width).
        protected bool HasHeadroom(int tiles)
        {
            int leftTile = (int)(NPC.position.X / 16f);
            int rightTile = (int)((NPC.position.X + NPC.width) / 16f);
            int headTile = (int)(NPC.position.Y / 16f);

            for (int tileX = leftTile; tileX <= rightTile; tileX++)
            {
                for (int tileY = headTile - tiles; tileY < headTile; tileY++)
                {
                    if (UsefulFunctions.IsTileReallySolid(tileX, tileY))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // First solid (or platform) tile at or below startY, scanning FloorScanTiles down. floorY = that tile's top edge.
        protected static bool TryFindFloor(float worldX, float startY, out float floorY)
        {
            int tileX = (int)(worldX / 16f);
            int startTileY = (int)(startY / 16f);

            for (int tileY = startTileY; tileY < startTileY + FloorScanTiles; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY, 5))
                {
                    break;
                }

                Tile tile = Main.tile[tileX, tileY];
                bool solidFloor = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

                if (solidFloor)
                {
                    floorY = tileY * 16f;
                    return true;
                }
            }

            floorY = 0f;
            return false;
        }

        protected void PlaySound(SoundStyle sound)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, NPC.Center);
            }
        }

        // Breath tell, Massacre's mouth ember in the tier palette: a small steady smoulder for the first 40%, then it swells
        // 0.6x → 1.9x and doubles in density until the breath releases. Accent motes once it swells.
        void EmitMouthEmber(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 mouth = Mouth;
            float scale = 0.45f;
            int count = 1;

            if (progress >= 0.4f)
            {
                float swell = (progress - 0.4f) / 0.6f;
                scale = MathHelper.Lerp(0.6f, 1.9f, swell);
                count = 2;
            }

            int flameDust = DemonElement.FlameDust(Tier);
            for (int i = 0; i < count; i++)
            {
                Vector2 spot = mouth + Main.rand.NextVector2Circular(3f, 3f);
                Dust ember = Dust.NewDustPerfect(spot, flameDust, Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                ember.noGravity = true;
            }

            if (progress >= 0.4f && Main.rand.NextBool(4))
            {
                DemonElement.SpawnAccent(Tier, mouth - new Vector2(4f), 8, 8, Vector2.Zero, scale);
            }

            Lighting.AddLight(mouth, DemonElement.LightColor(Tier) * (0.3f + scale * 0.4f));
        }

        /// <summary>Tier-coloured motes drawn inward from `radius` px onto `center` (horn gather, body converge).</summary>
        protected void EmitGather(Vector2 center, float radius, float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            int flameDust = DemonElement.FlameDust(Tier);
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust mote = Dust.NewDustPerfect(center + offset, flameDust, -offset * 0.08f, 40, default, MathHelper.Lerp(0.8f, 1.6f, progress));
                mote.noGravity = true;
            }

            Lighting.AddLight(center, DemonElement.LightColor(Tier) * (0.2f + progress * 0.6f));
        }

        /// <summary>A small tier-coloured burst (cast pulses, shell release, leap take-off and landing).</summary>
        protected void EmitBurst(Vector2 center, int count, float speed)
        {
            if (Main.dedServ)
            {
                return;
            }

            int flameDust = DemonElement.FlameDust(Tier);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                Dust flame = Dust.NewDustPerfect(center, flameDust, velocity, 60, default, Main.rand.NextFloat(1.1f, 1.8f));
                flame.noGravity = true;
            }
        }

        // Recovery / stagger read: dark smoke rolling off the mouth and shoulders and sinking (gravity on).
        void EmitExhaust(int count)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = Mouth + Main.rand.NextVector2Circular(14f, 10f);
                Dust smoke = Dust.NewDustPerfect(spot, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), 0.6f), 120, Color.DimGray, 1.3f);
                smoke.noGravity = false;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // Contact (body and leap). T1 On Fire 3s; T2 Curse +25; T3 Destined Death +1 + Abyss. Bleeding on all,
            // Fracturing Armor on the later two.
            DemonElement.ApplyHitDebuff(target, Tier, 3 * 60, 25);
            target.AddBuff(BuffID.Bleeding, 150);

            if (Tier != DemonTier.Oolacile)
            {
                target.AddBuff(ModContent.BuffType<FracturingArmor>(), 60 * 60);
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            Poise.RequestHitReaction(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Poise.RequestHitReaction(NPC, false);
        }

        /// <summary>
        /// The legacy demons' hit reaction, kept but only in neutral so it can never bail it out of a recovery:
        /// melee point-blank → 1/12 blink away; melee within 300px → 1/5 hop back; ranged → 1/24 blink, 1/3 hop forward
        /// when > 200px off. hitReactLockout (40t) stops hop chains. Server/singleplayer only (see IHitReactor).
        /// </summary>
        void IHitReactor.OnServerHit(NPC npc, bool melee)
        {
            if (State != DemonAttack.None || hitReactLockout > 0)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = Poise;
            bool canBlink = globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;
            float distance = npc.Distance(Main.player[npc.target].Center);

            if (melee)
            {
                if (distance < 100f && canBlink && Main.rand.NextBool(12))
                {
                    tsorcRevampAIs.QueueTeleport(npc, BlinkRangeTiles, true, BlinkTellTicks);
                    hitReactLockout = 40;
                    return;
                }

                if (distance < 300f && Main.rand.NextBool(5))
                {
                    npc.velocity.Y = Main.rand.NextFloat(-5f, -3f);
                    npc.velocity.X += npc.direction * Main.rand.NextFloat(-10f, -7f);
                    hitReactLockout = 40;
                    npc.netUpdate = true;
                }

                return;
            }

            if (canBlink && Main.rand.NextBool(24))
            {
                tsorcRevampAIs.QueueTeleport(npc, BlinkRangeTiles, true, BlinkTellTicks);
                hitReactLockout = 40;
                return;
            }

            bool grounded = npc.velocity.Y == 0f;
            if (distance > 200f && grounded && Main.rand.NextBool(3))
            {
                npc.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                npc.velocity.X += npc.direction * Main.rand.NextFloat(8f, 11f);
                hitReactLockout = 40;
                npc.netUpdate = true;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ || NPC.life > 0)
            {
                return;
            }

            // Death: double roar, a 50-dust tier burst and a heavy shake, then the tier's gores.
            SoundEngine.PlaySound(SoundID.NPCDeath10 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            EmitBurst(NPC.Center, 50, 7f);
            UsefulFunctions.ScreenShake(NPC.Center, 10f, 30, 6f, 1200f);
            SpawnDeathGores();
        }

        // Leap afterimages, tier-tinted, behind the body.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Leaping)
            {
                return true;
            }

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Color tint = DemonElement.TintColor(Tier);
            int trailLength = NPCID.Sets.TrailCacheLength[Type];

            for (int i = 1; i < trailLength; i++)
            {
                Vector2 oldCenter = NPC.oldPos[i] + NPC.Size * 0.5f;
                float alpha = 0.45f * (1f - i / (float)trailLength);
                Color afterimageColor = tint * alpha;
                afterimageColor.A = 0;
                DrawBodyFrame(spriteBatch, texture, oldCenter, screenPos, afterimageColor);
            }

            return true;
        }

        // Body flash: the same frame re-drawn on top with A = 0 (additive) in the tier tint, strength bodyFlash.
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (bodyFlash <= 0f)
            {
                return;
            }

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Color flashColor = bodyFlashTint * bodyFlash;
            flashColor.A = 0;
            DrawBodyFrame(spriteBatch, texture, NPC.Center, screenPos, flashColor);
        }

        // Draws the current frame the way vanilla positions it: centred on `center` in X, bottom 4px under the hitbox bottom.
        protected void DrawBodyFrame(SpriteBatch spriteBatch, Texture2D texture, Vector2 center, Vector2 screenPos, Color color)
        {
            Rectangle frame = NPC.frame;
            Vector2 origin = frame.Size() * 0.5f;
            float bottomY = center.Y + NPC.height * 0.5f;
            Vector2 drawPosition = new Vector2(center.X, bottomY + 4f + NPC.gfxOffY - frame.Height * NPC.scale * 0.5f) - screenPos;

            SpriteEffects effects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                effects = SpriteEffects.FlipHorizontally;
            }

            spriteBatch.Draw(texture, drawPosition, frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
        }
    }
}
