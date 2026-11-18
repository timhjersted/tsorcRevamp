using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Gwyn;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

// Deliberately outside the SuperHardMode namespace: GlobalNPC scales every class in that namespace
// except exactly Gwyn, which would multiply this boss's already final health a second time.
namespace tsorcRevamp.NPCs.Bosses.Primordial
{
    /// <summary>
    /// Prime Gwyn: the optional superboss after Gwyn. Every shared attack stays in Gwyn.cs; this file holds
    /// the three-phase tuning, the combo pool twins and the five Prime-only moves.
    ///
    /// Phases: I 100-60%, II 60-25%, III below 25%. He starts on Gwyn's half-health kit with Wrath lit.
    /// Harder comes from less neutral time and new mix-ups. Roll fairness stays fixed: live windows are
    /// 22t or less and consecutive live windows stay at least 30t apart unless avoidable another way.
    ///
    /// Multiplayer: the server latches phases, picks combos and casts, and spawns projectiles. Phase and
    /// transition state are synced in ExtraAI so every machine reads the same tuning and invulnerability.
    /// </summary>
    [AutoloadBossHead]
    class PrimeGwyn : Gwyn
    {
        enum PrimePhase : byte
        {
            First,
            Second,
            Third,
        }

        enum PrimeTransition : byte
        {
            None,
            CinderNova,
            FirstDawn,
        }

        protected override string InvaderTitle => "Prime Gwyn, the First Lord";

        // ── Core stats ───────────────────────────────────────────────────────────
        const int PrimeLifeMax = 1500000;
        const int PrimeSoulValue = 3000000;
        const int PrimeMeleeDamage = 120;
        const float SetPieceDamageMult = 1.25f;
        const float SecondPhaseHealth = 0.60f;
        const float ThirdPhaseHealth = 0.25f;
        const float PrimeRainStartRange = 1150f; // outer strips only, so the wide arena stays usable

        // Per-phase values, indexed by PrimePhase.
        static readonly float[] CooldownMults = { 0.75f, 0.6f, 0.5f };
        static readonly float[] RecoveryMults = { 0.75f, 0.62f, 0.5f };
        static readonly float[] UnderOverStrikeMults = { 1.25f, 1.4f, 1.5f };
        static readonly float[] ThreeHitStrikeMults = { 1.25f, 1.3f, 1.3f };
        static readonly int[] ComboChances = { 86, 90, 94 };
        static readonly int[] FlurryRecoveries = { 135, 110, 90 };
        static readonly int[] FlurryTells = { 44, 40, 40 };
        static readonly int[] ThreeHitTells = { 32, 30, 30 };
        static readonly int[] CinderfallTells = { 44, 40, 40 };
        static readonly int[] SpearCastTells = { 32, 28, 26 };
        static readonly int[] GraspTells = { 60, 50, 45 };
        static readonly int[] NovaCharges = { 60, 50, 45 };
        static readonly int[] EmbraceRecoveries = { 135, 110, 90 };
        static readonly int[] StormWaves = { 3, 3, 4 };
        static readonly int[] StormIntervals = { 45, 38, 34 };
        static readonly int[] StormNodes = { 4, 4, 6 };
        static readonly float[] StormSpearSpeeds = { 12f, 14f, 16f }; // 16 crosses the 340px ring in 21t
        static readonly int[] VolleyEvents = { 3, 3, 4 };
        static readonly int[] VolleyNodes = { 2, 3, 3 };
        static readonly float[] PrimeNovaThresholds = { 0.80f };

        // Punish floors: blade tail plus recovery never drops below these, unless Gwyn's own move is shorter.
        const int LightPunishFloor = 12;
        const int StringPunishFloor = 36;
        const int HeavyPunishFloor = 40;

        PrimePhase _primePhase = PrimePhase.First;
        PrimeTransition _transition = PrimeTransition.None;
        int _transitionTimer;
        bool _transitionInvulnerabilityApplied;
        bool _novaRequested;      // server only
        int _summonGateTimer;     // server only
        int _eruptionCooldown = 300; // server only

        int PhaseIndex => (int)_primePhase;

        // ── Identity and inherited tuning ────────────────────────────────────────
        protected override bool TooEarly => false;
        protected override bool HalfHealthMovesUnlocked => true;
        protected override int MeleeDamage => PrimeMeleeDamage;
        protected override int MeleeComboChance => ComboChances[PhaseIndex];
        protected override float RainOfDeathStartRange => PrimeRainStartRange;
        protected override float[] NovaHealthThresholds => PrimeNovaThresholds;
        protected override int NovaChargeTicks => NovaCharges[PhaseIndex];

        protected override int ScaleCooldown(int ticks)
        {
            int scaledTicks = (int)Math.Round(ticks * CooldownMults[PhaseIndex]);
            return scaledTicks;
        }

        protected override int ScaleAttackDamage(int baseDamage)
        {
            int scaledDamage = (int)Math.Round(baseDamage * SetPieceDamageMult);
            return scaledDamage;
        }

        protected override int PierceCooldownAfterUse => ScaleCooldown(base.PierceCooldownAfterUse);
        protected override int PierceRecoveryTicks => EmbraceRecoveries[PhaseIndex];
        protected override int BoomerangCooldownAfterUse => ScaleCooldown(base.BoomerangCooldownAfterUse);
        protected override int HomingVolleyCooldownAfterUse => ScaleCooldown(base.HomingVolleyCooldownAfterUse);
        protected override int HomingVolleySwingTelegraphTicks => SpearCastTells[PhaseIndex];
        protected override int AbyssShardCooldownAfterUse => ScaleCooldown(base.AbyssShardCooldownAfterUse);
        protected override int TendrilCooldownAfterUse => ScaleCooldown(base.TendrilCooldownAfterUse);
        protected override int TendrilTelegraphTicks => GraspTells[PhaseIndex];

        protected override int StormWaveCount => StormWaves[PhaseIndex];
        protected override int StormWaveIntervalTicks => StormIntervals[PhaseIndex];
        protected override int StormNodesPerWave => StormNodes[PhaseIndex];
        protected override float StormSpearSpeed => StormSpearSpeeds[PhaseIndex];
        protected override int SpearVolleyEvents => VolleyEvents[PhaseIndex];
        protected override int SpearVolleyNodesPerEvent => VolleyNodes[PhaseIndex];

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.lifeMax = PrimeLifeMax;
            NPC.value = PrimeSoulValue;

            // Runs on every machine, so the white-hot aura and flame wings need no ignition event or sync.
            _wrathActive = true;
        }

        // Killing Prime Gwyn must not end Super Hardmode the way the story fight does.
        protected override void OnFinalDeath()
        {
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_primePhase);
            writer.Write((byte)_transition);
            writer.Write((short)_transitionTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _primePhase = (PrimePhase)reader.ReadByte();
            _transition = (PrimeTransition)reader.ReadByte();
            _transitionTimer = reader.ReadInt16();
        }

        // ── Combo pool ───────────────────────────────────────────────────────────
        // Gwyn's table is static and steps get written during a fight, so Prime owns a deep copy. Indices
        // 0-21 match Gwyn. New V2 twins are real pool entries because clients resolve clips by pool index.
        const int TwinRoleCount = 5; // Cleave, Guillotine, Judgment, Riposte, Backhand
        const int SecondPhaseTwinStart = 22;
        const int ThirdPhaseTwinStart = SecondPhaseTwinStart + TwinRoleCount;
        const int FalseDawnIndex = ThirdPhaseTwinStart + TwinRoleCount;
        const int SolarChainIndex = FalseDawnIndex + 1;
        const int PrimePoolLength = SolarChainIndex + 1;

        const string FalseDawnName = "False Dawn";
        const string SolarChainName = "Solar Chain";

        // Sunspin turns at a fixed rate, so 21 attack ticks is about 337 degrees and stays inside one roll.
        const int PrimeSunspinTicks = 21;

        // Five swipes plus the leap is six rolls (180 stamina). The breather after swipe three matters
        // because this rhythm is faster than the stamina regeneration delay.
        const int FlurryLeapStepIndex = 5;
        const int FlurryBreatherStepIndex = 2;
        const int FlurryBreatherPauseTicks = 45;

        // windup, active, recovery per role. Hit windows are fractions of active, so live time follows it.
        static readonly int[,] SecondPhaseClipTimings =
        {
            { 24, 16, 10 },
            { 40, 20, 36 },
            { 55, 22, 36 },
            { 18, 17, 26 },
            { 20, 12, 10 },
        };

        static readonly int[,] ThirdPhaseClipTimings =
        {
            { 22, 16, 9 },
            { 40, 20, 36 },
            { 51, 20, 36 },
            { 18, 17, 26 },
            { 20, 12, 10 },
        };

        static readonly PuppetAttackClip[][] PhaseClips = BuildPhaseClips();
        static readonly MeleeCombo[] PrimeCombos = BuildPrimePool();

        protected override MeleeCombo[] MeleeComboPoolOverride => PrimeCombos;

        static PuppetAttackClip[][] BuildPhaseClips()
        {
            PuppetAttackClip[][] clips = new PuppetAttackClip[3][];
            clips[0] = new PuppetAttackClip[TwinRoleCount];
            clips[1] = new PuppetAttackClip[TwinRoleCount];
            clips[2] = new PuppetAttackClip[TwinRoleCount];

            for (int role = 0; role < TwinRoleCount; role++)
            {
                PuppetAttackClip halfHealthClip = BaseComboPool[CB_CLEAVE_HALF + role].RuntimeV2Clip;
                clips[0][role] = halfHealthClip;
                clips[1][role] = RetimedClip(halfHealthClip, halfHealthClip.Name,
                    SecondPhaseClipTimings[role, 0], SecondPhaseClipTimings[role, 1], SecondPhaseClipTimings[role, 2]);
                clips[2][role] = RetimedClip(halfHealthClip, halfHealthClip.Name,
                    ThirdPhaseClipTimings[role, 0], ThirdPhaseClipTimings[role, 1], ThirdPhaseClipTimings[role, 2]);
            }

            return clips;
        }

        static MeleeCombo CopyCombo(MeleeCombo combo)
        {
            combo.Steps = (MeleeComboStep[])combo.Steps.Clone();
            return combo;
        }

        static MeleeCombo[] BuildPrimePool()
        {
            MeleeCombo[] sourcePool = BaseComboPool;
            MeleeCombo[] pool = new MeleeCombo[PrimePoolLength];

            for (int i = 0; i < sourcePool.Length; i++)
            {
                pool[i] = CopyCombo(sourcePool[i]);
            }

            pool[CB_SPIN].Steps[0].AttackTicks = PrimeSunspinTicks;

            MeleeComboStep[] flurrySteps =
            {
                FlurrySwipe(ComboMotion.UnderhandArc, FlurryTelegraphTicks, 0.8f, 1.1f, FlurrySwipePush),
                FlurrySwipe(ComboMotion.OverheadArc, 0, 0.8f, 1.1f, FlurrySwipePush),
                FlurrySwipe(ComboMotion.UnderhandArc, 0, 0.8f, 1.1f, FlurrySwipePush),
                FlurrySwipe(ComboMotion.OverheadArc, 0, 0.8f, 1.1f, FlurrySwipePush),
                FlurrySwipe(ComboMotion.UnderhandArc, 0, 0.8f, 1.1f, FlurrySwipePush),
                FlurryLeap(0, 1.3f),
            };
            flurrySteps[FlurryBreatherStepIndex].PostStepPause = FlurryBreatherPauseTicks;
            pool[CB_FLURRY].Steps = flurrySteps;

            for (int role = 0; role < TwinRoleCount; role++)
            {
                MeleeCombo secondTwin = CopyCombo(sourcePool[CB_CLEAVE_HALF + role]);
                secondTwin.RuntimeV2Clip = PhaseClips[1][role];
                pool[SecondPhaseTwinStart + role] = secondTwin;

                MeleeCombo thirdTwin = CopyCombo(sourcePool[CB_CLEAVE_HALF + role]);
                thirdTwin.RuntimeV2Clip = PhaseClips[2][role];
                pool[ThirdPhaseTwinStart + role] = thirdTwin;
            }

            // False Dawn timing sheet
            // Poses -1.62 -> 2.29 rad: 224 degree envelope, about 180 live. Tell 46t on screen: 20t raise,
            // glint, 26t hold. Authored 10/45 becomes 8/36 through the shared x1.25: peak about 29 deg/t,
            // live 15t. A roll at the glint ends at 22, inside its 22-30 gap, as the blade goes live at 26.
            // Reach 1.14 keeps this a blade-only cut with no fire crescent. Recovery 20t after a 29t tail.
            pool[FalseDawnIndex] = new MeleeCombo
            {
                Name = FalseDawnName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 280,
                RecoveryTicks = 27,
                Steps = new[]
                {
                    WeightedSwordSwing(ComboMotion.OverheadArc, 40, 10, 45, 7f, 0, 1.4f, 1.14f, 0.6f),
                },
            };

            // Solar Chain timing: 24t tell, 8t live of 12 active, 10t recovery. With the 6t teleport gap
            // the next live window starts about 43t after this one ends, so each link is separately rollable.
            MeleeCombo solarChain = CopyCombo(sourcePool[CB_BACKHAND_HALF]);
            solarChain.Name = SolarChainName;
            solarChain.BaseWeight = 2;
            solarChain.Preferred = ComboRangeBand.Any;
            solarChain.CooldownAfterUse = 0;
            solarChain.InitialFlashColor = Color.Gold;
            solarChain.RuntimeV2Clip = RetimedClip(PhaseClips[0][4], SolarChainName, 24, 12, 10);
            pool[SolarChainIndex] = solarChain;

            return pool;
        }

        protected override int HealthVariant(int comboIndex)
        {
            int halfHealthIndex = base.HealthVariant(comboIndex);
            bool isTwinRole = halfHealthIndex >= CB_CLEAVE_HALF && halfHealthIndex <= CB_BACKHAND_HALF;
            if (!isTwinRole || _primePhase == PrimePhase.First)
            {
                return halfHealthIndex;
            }

            int role = halfHealthIndex - CB_CLEAVE_HALF;
            int twinStart = SecondPhaseTwinStart;
            if (_primePhase == PrimePhase.Third)
            {
                twinStart = ThirdPhaseTwinStart;
            }
            return twinStart + role;
        }

        protected override bool IsV2ClipEligible(PuppetAttackClip clip)
        {
            bool inCurrentPhase = Array.IndexOf(PhaseClips[PhaseIndex], clip) >= 0;
            return inCurrentPhase;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (_transition != PrimeTransition.None)
            {
                return false;
            }

            // A chain link is the only legal pick while it is being started, and never otherwise.
            if (_solarChainStrikeReady)
            {
                return combo.Name == SolarChainName;
            }
            if (combo.Name == SolarChainName)
            {
                return false;
            }
            if (combo.Name == FalseDawnName && _primePhase == PrimePhase.First)
            {
                return false;
            }

            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        /// <summary>Layers the phase on top of Gwyn's half-health pass. Each phase sets its own value
        /// because a flat speed-up would push tells, gaps and blade speed over the fairness limits.</summary>
        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            base.CustomizeMeleeCombo(ref combo, healthFraction);

            bool usesAuthoredTiming = combo.RuntimeV2Clip != null
                || combo.Steps == null
                || combo.Name == FalseDawnName;
            if (usesAuthoredTiming)
            {
                return;
            }

            // Gwyn's pass already applied x1.25 strike and x0.75 recovery, except on the Flurry.
            bool isFlurry = combo.Name == WrathFlurryName;
            float totalStrikeMult = HalfHealthStrikeSpeedMult;
            if (isFlurry || combo.Name == UnderOverName)
            {
                totalStrikeMult = UnderOverStrikeMults[PhaseIndex];
            }
            else if (combo.Name == ThreeHitName)
            {
                totalStrikeMult = ThreeHitStrikeMults[PhaseIndex];
            }

            float strikeMult = totalStrikeMult / HalfHealthStrikeSpeedMult;
            float recoveryMult = RecoveryMults[PhaseIndex] / HalfHealthRecoveryMult;
            if (isFlurry)
            {
                strikeMult = totalStrikeMult;
                recoveryMult = RecoveryMults[PhaseIndex];
            }

            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];
                bool isWeighted = step.Ease == SwingEaseStyle.Weighted && step.EaseOutDecay > 0f;
                bool isHitConfirm = i == HitConfirmStepIndex;
                if (!isWeighted || isHitConfirm)
                {
                    continue;
                }

                // These two pauses are load-bearing: the hit-confirm pause keeps the follow-up outside
                // post-hit immunity, and the breather is the Flurry's stamina window.
                int authoredPause = step.PostStepPause;
                RescaleWeightedStep(ref step, strikeMult, recoveryMult);
                bool leadsIntoHitConfirm = i + 1 == HitConfirmStepIndex;
                bool isBreather = isFlurry && i == FlurryBreatherStepIndex;
                if (leadsIntoHitConfirm || isBreather)
                {
                    step.PostStepPause = authoredPause;
                }
                combo.Steps[i] = step;
            }

            // Tells are on-screen ticks. The base floors legacy combo tells at 30.
            int onScreenTell = 0;
            if (isFlurry)
            {
                onScreenTell = FlurryTells[PhaseIndex];
            }
            else if (combo.Name == ThreeHitName)
            {
                onScreenTell = ThreeHitTells[PhaseIndex];
            }
            else if (combo.Name == CinderfallName)
            {
                onScreenTell = CinderfallTells[PhaseIndex];
            }
            if (onScreenTell > 0)
            {
                combo.Steps[0].TelegraphTicks = (int)Math.Ceiling(onScreenTell / ComboTelegraphMultiplier);
            }

            if (isFlurry)
            {
                combo.RecoveryTicks = FlurryRecoveryTotalTicks;
                return;
            }

            MeleeComboStep lastStep = combo.Steps[combo.Steps.Length - 1];
            int tailTicks = 0;
            if (lastStep.Ease == SwingEaseStyle.Weighted)
            {
                int liveTicks = (int)Math.Ceiling(lastStep.HitWindowEnd * lastStep.AttackTicks);
                tailTicks = lastStep.AttackTicks - liveTicks;
            }

            int punishFloor = LightPunishFloor;
            if (lastStep.Motion != ComboMotion.Spin && combo.HeavyCommit)
            {
                punishFloor = HeavyPunishFloor;
            }
            else if (lastStep.Motion != ComboMotion.Spin && combo.Steps.Length > 1)
            {
                punishFloor = StringPunishFloor;
            }

            // The floor can hold recovery where it is, but never lengthens what Gwyn already had.
            int currentRecovery = combo.RecoveryTicks;
            int scaledRecovery = (int)Math.Round(currentRecovery * recoveryMult);
            int flooredRecovery = Math.Max(scaledRecovery, punishFloor - tailTicks);
            combo.RecoveryTicks = Math.Max(1, Math.Min(currentRecovery, flooredRecovery));
        }

        // ── Wrath Flurry ─────────────────────────────────────────────────────────
        protected override int FlurryRecoveryTotalTicks => FlurryRecoveries[PhaseIndex];
        protected override int FlurryFinaleStepIndex => FlurryLeapStepIndex;

        // ── False Dawn (Phase II+): the delayed cut that catches an early roll ───
        // Spec: Gwyn's sword, raised over 20t. Glint at tick 20 (gold dust at the tip plus a high chime).
        // Held until tick 46, then one overhead. Counter: roll 20-26t after the glint, or step out of reach.
        // Multiplayer: the server picks it; the glint is cosmetic on the synced telegraph clock.
        const int FalseDawnRaiseTicks = 20;
        const int FalseDawnTellTicks = 46;
        const int FalseDawnGlintDust = 28;

        bool FalseDawnSwinging => ComboSwinging(FalseDawnName);

        protected override float LogicalWindupSettleFraction
        {
            get
            {
                if (FalseDawnSwinging)
                {
                    return FalseDawnRaiseTicks / (float)FalseDawnTellTicks;
                }
                return base.LogicalWindupSettleFraction;
            }
        }

        // Settling onto the attack's own start pose turns the rest of the tell into a still hold.
        protected override float GetLogicalWindupOppositeEnd(ComboMotion motion, float attackStart, float oppositeEnd)
        {
            if (FalseDawnSwinging)
            {
                return attackStart;
            }
            return base.GetLogicalWindupOppositeEnd(motion, attackStart, oppositeEnd);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (FalseDawnSwinging && motion == ComboMotion.OverheadArc)
            {
                startRotation = RefinedRaisedPose;
                endRotation = RefinedLoweredPose;
                return;
            }
            base.ModifyMeleeArcEndpoints(motion, ref startRotation, ref endRotation);
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);

            bool isGlintTick = combo.Name == FalseDawnName && elapsed == FalseDawnRaiseTicks;
            if (!isGlintTick || Main.dedServ)
            {
                return;
            }

            Vector2 bladeTip = PuppetWeaponTipPosition(ComboReachBase * 0.7f * step.ReachMult);
            SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 1f, Pitch = 0.6f }, bladeTip);
            for (int i = 0; i < FalseDawnGlintDust; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f);
                Dust glint = Dust.NewDustPerfect(bladeTip, DustID.GoldFlame, velocity, 10, Color.White, 1.5f);
                glint.noGravity = true;
            }
            Lighting.AddLight(bladeTip, 1.4f, 1.3f, 1f);
        }

        // ── Sundering Cinderfall (Phase II+) ─────────────────────────────────────
        // Spec: the slam's peak-speed tick sends one ground wave each way at 9 px/t for 40 tiles or until
        // terrain stops it. Roll the slam, jump the wave. Same damage ratio as Winged Plunge's waves.
        const float SunderWaveSpeed = 9f;
        const float SunderWaveReachTiles = 40f;
        const float SunderWaveDamageMult = 0.55f;

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            bool isCinderfallImpact = combo.Name == CinderfallName
                && step.Motion == ComboMotion.GroundSlam
                && elapsed == step.EaseInTicks;
            bool wavesUnlocked = _primePhase != PrimePhase.First;
            if (!isCinderfallImpact || !wavesUnlocked || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int waveType = ModContent.ProjectileType<GwynGroundFireWave>();
            int waveDamage = (int)(MeleeDamage * SunderWaveDamageMult);
            Vector2 impactPosition = NPC.Bottom - new Vector2(0f, 20f);
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), impactPosition, Vector2.Zero, waveType,
                    waveDamage, 5f, Main.myPlayer, direction, SunderWaveReachTiles, SunderWaveSpeed);
            }
        }

        // ── Solar Chain (Phase II+): Flash Step becomes a teleporting string ─────
        // Spec: after Flash Step he plays a 24t-tell backhand, then teleports behind the target and repeats.
        // Two links in Phase II, three in Phase III, so at most three rolls.
        // Multiplayer: this state is server only; clients follow the V2 snapshot and the flash-step count.
        const int SecondPhaseChainLinks = 2;
        const int ThirdPhaseChainLinks = 3;
        const int ChainTeleportGapTicks = 6;
        const float ChainBehindDistance = 70f;

        int _solarChainLinksLeft;
        int _solarChainGapTimer;
        bool _solarChainArrived;
        bool _solarChainStrikeReady;

        protected override void OnFlashStepFinished(Player target)
        {
            if (_primePhase == PrimePhase.First)
            {
                return;
            }

            _solarChainLinksLeft = SecondPhaseChainLinks;
            if (_primePhase == PrimePhase.Third)
            {
                _solarChainLinksLeft = ThirdPhaseChainLinks;
            }
            _solarChainArrived = true;
            _solarChainGapTimer = 0;
        }

        void CancelSolarChain()
        {
            _solarChainLinksLeft = 0;
            _solarChainGapTimer = 0;
            _solarChainArrived = false;
            _solarChainStrikeReady = false;
        }

        public override void OnStagger(NPC npc)
        {
            CancelSolarChain();
            base.OnStagger(npc);
        }

        void TickSolarChain()
        {
            if (_solarChainLinksLeft <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                CancelSolarChain();
                return;
            }

            if (_solarChainArrived)
            {
                _solarChainArrived = false;
                StartSolarChainStrike(target);
                return;
            }

            if (_solarChainGapTimer > 0)
            {
                _solarChainGapTimer--;
                if (_solarChainGapTimer > 0)
                {
                    return;
                }

                // Prefer the target's blind side, then the other side. No standing room ends the chain.
                float behindX = target.Center.X - target.direction * ChainBehindDistance;
                float frontX = target.Center.X + target.direction * ChainBehindDistance;
                Vector2 chosenBottom = new Vector2(behindX, target.Bottom.Y);
                if (IsStandingSpotBlocked(chosenBottom))
                {
                    chosenBottom = new Vector2(frontX, target.Bottom.Y);
                }
                if (IsStandingSpotBlocked(chosenBottom))
                {
                    CancelSolarChain();
                    return;
                }

                FlashStepTo(chosenBottom, target);
                StartSolarChainStrike(target);
                return;
            }

            // The previous link is still playing until the combat machine returns to neutral.
            bool neutral = Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
            if (neutral)
            {
                _solarChainGapTimer = ChainTeleportGapTicks;
                EnterPhase(AttackPhase.NovaRecovery, ChainTeleportGapTicks + 1);
                NPC.netUpdate = true;
            }
        }

        bool IsStandingSpotBlocked(Vector2 bottom)
        {
            Vector2 topLeft = bottom - new Vector2(NPC.width * 0.5f, NPC.height);
            return Collision.SolidCollision(topLeft, NPC.width, NPC.height);
        }

        void StartSolarChainStrike(Player target)
        {
            _solarChainLinksLeft--;
            EnterPhase(AttackPhase.Idle, 0);

            // The ready flag makes Solar Chain the only combo the picker can return.
            _solarChainStrikeReady = true;
            bool started = TryStartMeleeCombo(NPC.Distance(target.Center));
            _solarChainStrikeReady = false;

            if (!started)
            {
                CancelSolarChain();
                return;
            }
            SetAttackLabel(SolarChainName, 50);
            NPC.netUpdate = true;
        }

        // ── Summoned-cast spacing ────────────────────────────────────────────────
        // Only one summoned threat at a time, with at least 150t of clear air between them.
        const int SummonedCastSpacingTicks = 150;
        const int SpearVolleyBusyTicks = 90;
        const int LightningSpearBusyTicks = 60;
        const int EruptionBusyTicks = 60;

        protected override bool SummonedCastAllowed =>
            _transition == PrimeTransition.None && _summonGateTimer <= 0 && !SetPieceRunning;

        protected override void OnSummonedCastStarted(int busyTicks)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _summonGateTimer = busyTicks + SummonedCastSpacingTicks;
            }
        }

        protected override bool CanAbyssShard => base.CanAbyssShard && SummonedCastAllowed;
        protected override bool CanHomingVolley => base.CanHomingVolley && SummonedCastAllowed;

        protected override void DoAbyssShardFire(int fireIndex)
        {
            base.DoAbyssShardFire(fireIndex);
            if (fireIndex == 0)
            {
                OnSummonedCastStarted(SpearVolleyBusyTicks);
            }
        }

        protected override void DoHomingVolleyFire()
        {
            base.DoHomingVolleyFire();
            OnSummonedCastStarted(LightningSpearBusyTicks);
        }

        // ── Eruption (Phase II+): dodged by position, never by rolling ───────────
        // Spec: 3 or 5 bombs. Each marks the floor for 60t, then lands with a 56px blast. Bombs stay 160px
        // or more apart. Every nearby player gets one bomb within 200px; the rest scatter around the target.
        // Multiplayer: the server picks all positions and spawns; each bomb animates from its own timer.
        const int SecondPhaseEruptionBombs = 3;
        const int ThirdPhaseEruptionBombs = 5;
        const int SecondPhaseEruptionCooldown = 420;
        const int ThirdPhaseEruptionCooldown = 300;
        const int EruptionRollChance = 45;
        const int EruptionDamage = 70;
        const int EruptionPlacementTries = 14;
        const float EruptionPlayerRange = 2400f;
        const float EruptionNearPlayerSpread = 200f;
        const float EruptionScatterSpread = 760f;
        const float EruptionMinimumSpacing = 160f;

        void TickEruption()
        {
            bool canAct = Main.netMode != NetmodeID.MultiplayerClient && _primePhase != PrimePhase.First;
            if (!canAct)
            {
                return;
            }
            if (_eruptionCooldown > 0)
            {
                _eruptionCooldown--;
                return;
            }
            if (!SummonedCastAllowed || !Main.rand.NextBool(EruptionRollChance))
            {
                return;
            }

            Player primaryTarget = Main.player[NPC.target];
            if (!primaryTarget.active || primaryTarget.dead)
            {
                return;
            }

            int bombCount = SecondPhaseEruptionBombs;
            _eruptionCooldown = SecondPhaseEruptionCooldown;
            if (_primePhase == PrimePhase.Third)
            {
                bombCount = ThirdPhaseEruptionBombs;
                _eruptionCooldown = ThirdPhaseEruptionCooldown;
            }

            List<Player> nearbyPlayers = new List<Player>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && NPC.Distance(player.Center) <= EruptionPlayerRange)
                {
                    nearbyPlayers.Add(player);
                }
            }

            List<Vector2> landingPoints = new List<Vector2>();
            for (int bomb = 0; bomb < bombCount; bomb++)
            {
                Player anchor = primaryTarget;
                float spread = EruptionScatterSpread;
                if (bomb < nearbyPlayers.Count)
                {
                    anchor = nearbyPlayers[bomb];
                    spread = EruptionNearPlayerSpread;
                }

                for (int attempt = 0; attempt < EruptionPlacementTries; attempt++)
                {
                    float landingX = anchor.Center.X + Main.rand.NextFloat(-spread, spread);
                    float floorY = PrimordialEruptionBomb.FindFloorY(landingX, anchor.Bottom.Y);
                    if (floorY < 0f)
                    {
                        continue;
                    }

                    Vector2 landingPoint = new Vector2(landingX, floorY - PrimordialEruptionBomb.BlastCenterHeight);
                    bool tooClose = false;
                    foreach (Vector2 existingPoint in landingPoints)
                    {
                        if (Math.Abs(existingPoint.X - landingX) < EruptionMinimumSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose)
                    {
                        landingPoints.Add(landingPoint);
                        break;
                    }
                }
            }

            foreach (Vector2 landingPoint in landingPoints)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), landingPoint, Vector2.Zero,
                    ModContent.ProjectileType<PrimordialEruptionBomb>(), ScaleAttackDamage(EruptionDamage), 4f, Main.myPlayer);
            }

            OnSummonedCastStarted(EruptionBusyTicks);
            SetAttackLabel("Eruption", 70);
            NPC.netUpdate = true;
        }

        // ── Phase transitions ────────────────────────────────────────────────────
        // Phase II opens with a Cinder Nova. Phase III is First Dawn: he rises, and three rings cross the
        // field 40t apart at 12 px/t. A ring passes a player in about 9t, so each gap clears the 30t rule.
        // He cannot be damaged during either, so that time does not count against the kill-time target.
        const int FirstDawnRiseTicks = 90;
        const int FirstDawnTelegraphTick = 75;
        const int FirstDawnFirstRingTick = 120;
        const int FirstDawnRingInterval = 40;
        const int FirstDawnRingCount = 3;
        const int FirstDawnLandTick = 215;
        const int FirstDawnEndTick = 270;
        const float FirstDawnRiseSpeed = 1.4f;
        const float FirstDawnRingRadius = 1400f;
        const float FirstDawnRingSpeed = 12f;

        protected override bool ShouldTriggerNova()
        {
            if (_novaRequested && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _novaRequested = false;
                return true;
            }
            if (_transition != PrimeTransition.None)
            {
                return false;
            }
            return base.ShouldTriggerNova();
        }

        void TickPhaseLatch()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _transition != PrimeTransition.None)
            {
                return;
            }

            // A held player is released by the grab's own ending, so the change waits for it.
            bool holdingPlayer = Phase == AttackPhase.PierceStabHold || Phase == AttackPhase.PierceStabFlick;
            if (holdingPlayer)
            {
                return;
            }

            float healthFraction = NPC.life / (float)NPC.lifeMax;
            if (_primePhase == PrimePhase.First && healthFraction <= SecondPhaseHealth)
            {
                BeginTransition(PrimePhase.Second, PrimeTransition.CinderNova);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.PrimeGwyn.SecondPhase"), 255, 150, 60);
                _novaRequested = true;
            }
            else if (_primePhase == PrimePhase.Second && healthFraction <= ThirdPhaseHealth)
            {
                BeginTransition(PrimePhase.Third, PrimeTransition.FirstDawn);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.PrimeGwyn.FirstDawn"), 255, 225, 150);
                EnterPhase(AttackPhase.NovaRecovery, FirstDawnEndTick + 5);
                SetAttackLabel("First Dawn", FirstDawnEndTick);
            }
        }

        void BeginTransition(PrimePhase nextPhase, PrimeTransition transition)
        {
            _primePhase = nextPhase;
            _transition = transition;
            _transitionTimer = 0;

            // The phase latches before anything else, so burst damage cannot trigger it twice.
            OnStagger(NPC);
            ClearSetPieces();
            NPC.velocity.X = 0f;
            NPC.netUpdate = true;
        }

        void EndTransition()
        {
            _transition = PrimeTransition.None;
            _transitionTimer = 0;
            NPC.noGravity = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                EnterPhase(AttackPhase.Idle, 0);
                NPC.netUpdate = true;
            }
        }

        // NPC.dontTakeDamage is not synced, so every machine derives it from the synced transition state.
        void ApplyTransitionInvulnerability()
        {
            bool invulnerable = _transition != PrimeTransition.None;
            if (invulnerable)
            {
                NPC.dontTakeDamage = true;
                _transitionInvulnerabilityApplied = true;
            }
            else if (_transitionInvulnerabilityApplied)
            {
                NPC.dontTakeDamage = false;
                _transitionInvulnerabilityApplied = false;
            }
        }

        void TickCinderNovaTransition()
        {
            if (_transition != PrimeTransition.CinderNova || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            _transitionTimer++;
            bool novaRunning = Phase == AttackPhase.NovaCharge
                || Phase == AttackPhase.NovaBlast
                || Phase == AttackPhase.NovaRecovery;
            if (!_novaRequested && !novaRunning && _transitionTimer > 1)
            {
                EndTransition();
            }
        }

        void TickFirstDawn()
        {
            if (_transition != PrimeTransition.FirstDawn)
            {
                return;
            }

            _transitionTimer++;
            int timer = _transitionTimer;

            // Keep the ground machine parked; a stray phase change must not start an attack here.
            if (Main.netMode != NetmodeID.MultiplayerClient && Phase != AttackPhase.NovaRecovery)
            {
                EnterPhase(AttackPhase.NovaRecovery, FirstDawnEndTick - timer + 5);
            }

            NPC.velocity.X *= 0.85f;
            if (timer < FirstDawnLandTick)
            {
                Flight?.RequestTakeoff();
                NPC.noGravity = true;
                NPC.velocity.Y = 0f;
                if (timer < FirstDawnRiseTicks)
                {
                    NPC.velocity.Y = -FirstDawnRiseSpeed;
                }
            }
            else
            {
                Flight?.RequestLand();
                NPC.noGravity = false;
            }

            if (!Main.dedServ && timer < FirstDawnLandTick)
            {
                SpawnFirstDawnDust(timer);
            }

            if (timer == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
            }

            int ringClock = timer - FirstDawnFirstRingTick;
            bool ringDue = ringClock >= 0
                && ringClock % FirstDawnRingInterval == 0
                && ringClock / FirstDawnRingInterval < FirstDawnRingCount;

            if (ringDue && !Main.dedServ)
            {
                UsefulFunctions.ScreenShake(NPC.Center, 10f, 18);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (timer == FirstDawnTelegraphTick)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<GwynCinderNovaTelegraph>(), 0, 0f, Main.myPlayer,
                        NPC.whoAmI, FirstDawnFirstRingTick - FirstDawnTelegraphTick);
                }
                if (ringDue)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<GwynCinderNova>(), ScaleAttackDamage(NovaDamage), 10f, Main.myPlayer,
                        FirstDawnRingRadius, FirstDawnRingSpeed);
                }
                if (timer >= FirstDawnEndTick)
                {
                    EndTransition();
                }
            }
        }

        void SpawnFirstDawnDust(int timer)
        {
            float progress = MathHelper.Clamp(timer / (float)FirstDawnFirstRingTick, 0f, 1f);
            int dustCount = 2 + (int)(progress * 5f);
            float gatherRadius = MathHelper.Lerp(220f, 40f, progress);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(gatherRadius, gatherRadius);
                Vector2 position = NPC.Center + offset;
                int dustType = DustID.Torch;
                if (Main.rand.NextBool())
                {
                    dustType = DustID.GoldFlame;
                }
                Dust ember = Dust.NewDustPerfect(position, dustType, -offset * 0.05f, 40, default, 1.4f);
                ember.noGravity = true;
            }
            Lighting.AddLight(NPC.Center, 1.5f, 1.2f, 0.7f);
        }

        // ── Per-tick order ───────────────────────────────────────────────────────
        // Phase latches and chain steps run before the combat machine so they win this tick's decision.
        // First Dawn runs after it so its movement has the last word before Terraria moves the NPC.
        public override void AI()
        {
            TickPhaseLatch();
            ApplyTransitionInvulnerability();
            TickSolarChain();

            if (_summonGateTimer > 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _summonGateTimer--;
            }

            base.AI();

            if (!NPC.active)
            {
                return;
            }

            TickCinderNovaTransition();
            TickFirstDawn();
            TickEruption();
        }
    }

    /// <summary>Debug spawn until the Ember of the First Dawn and the arena exist: /primegwyn.</summary>
    public class PrimeGwynSpawnCommand : ModCommand
    {
        const float SpawnDistance = 480f;

        public override CommandType Type => CommandType.World;
        public override string Command => "primegwyn";
        public override string Description => "Debug: spawn Prime Gwyn near you, for testing before his arena exists.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            int primeType = ModContent.NPCType<PrimeGwyn>();
            if (NPC.AnyNPCs(primeType))
            {
                caller.Reply("Prime Gwyn is already here.", Color.OrangeRed);
                return;
            }

            Player player = caller.Player;
            int spawnX = (int)(player.Center.X + player.direction * SpawnDistance);
            int spawnY = (int)player.Bottom.Y;
            NPC.NewNPC(new EntitySource_BossSpawn(player), spawnX, spawnY, primeType, 0, 0f, 0f, 0f, 0f, player.whoAmI);
            caller.Reply("Prime Gwyn has been summoned.", Color.Orange);
        }
    }
}
