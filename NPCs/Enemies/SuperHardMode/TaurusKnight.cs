using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// "The bull of the Foundry": a rare SHM great-hammer knight in Fossil armour, rendered as a puppet Player.
    /// Heavy-tell, big-punish, and very aggressive — it closes fast, never swings at air, and its Quake Slam
    /// Cripples every player within 60 tiles to force a ground fight. Design and every number below:
    /// tsorcDocs/EnemyRevampPlans/TaurusKnight_Puppet_Proposal.md (handoff: TaurusKnight_Puppet_Handoff.md).
    ///
    /// Two attack paths, both server-authoritative:
    ///  - Melee (Hammer Fall / Bull's Temper / Quake Slam / Leap Smash) is a bespoke legacy MeleeCombo table driven by
    ///    the shared combo engine, picked from a bag in ReactiveComboIndex.
    ///  - Bull Rush, Trident Javelin, Bull Breath and Brimstone Rain are set-pieces on the base's AttackPhase.Custom,
    ///    one Custom phase per stage (so every stage change is a synced phase change), picked from a second bag that
    ///    also deals Leap Smash. The class name and type are load-bearing: scripted events, the banner and the
    ///    spawn/balance registries reference TaurusKnight.
    /// </summary>
    [AutoloadBossHead]
    public class TaurusKnight : PuppetNPC
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(TaurusKnight)) + "_Head_Boss";

        #region Tuning

        private const string HammerFallName = "Hammer Fall";
        private const string BullsTemperName = "Bull's Temper";
        private const string QuakeSlamName = "Quake Slam";
        private const string LeapSmashName = "Leap Smash";

        // Indices into TaurusCombos; the bags and ReactiveComboIndex name combos by these.
        private const int HammerFallIndex = 0;
        private const int BullsTemperIndex = 1;
        private const int QuakeSlamIndex = 2;
        private const int LeapSmashIndex = 3;

        // Raw hostile-hitbox damage (the engine doubles hostile hits). Blade hits multiply this by the step's DamageMult.
        private const int BaseMeleeDamage = 40;
        // Kept from the legacy kit.
        private const int TridentDamage = 25;
        private const int BreathDamage = 20;
        private const int BrimstoneDamage = 50;
        // Quake Slam's shockwave fronts deal 0.7x melee.
        private const float QuakeWaveDamageMult = 0.7f;

        private const float PhaseOneTopSpeed = 3.0f;
        private const float PhaseTwoTopSpeed = 3.4f;
        // Phase 2 trims recoveries (never tells) by 15%.
        private const float PhaseTwoRecoveryMult = 0.85f;
        private const float PhaseTwoHealthFraction = 0.5f;

        // Reach: hitbox = ComboReachBase * 0.7 * ReachMult. 56 * 0.7 * 1.1 = 43px vs the measured visual reach (grip texel
        // (6,40) to the far head corner (46,16) of the 46x46 Doomhammer = 46.6 texels * 1.0 draw scale = 47px): -7%.
        // (2026-10-28: drawn 1.6 -> 1.0, too big in game; the reach base was scaled with it from 90.)
        private const float HammerReachBase = 56f;
        private const float HammerDrawScale = 1.0f;
        // Centre-to-centre slack for the target's own half-width when testing whether a swing can connect.
        private const float TargetHalfWidth = 10f;

        // §3.1 "never swing at air": the reach + push test runs at selection and again TelegraphFacingCommitTicks before
        // release. While it fails, the tell holds in the cocked pose and keeps chasing, for at most this many ticks,
        // then gives up and re-picks.
        private const int MaxTelegraphHoldTicks = 30;
        private const int TelegraphCommitTicks = 10;
        private const float ClosingSpeedMult = 1.8f;
        private const float TelegraphAdvanceSpeedMult = 1.0f;
        private const float QuakeStartRange = 150f;

        // Hammer Fall's head reaches the floor 4 ticks after the strike's peak (tick EaseInTicks).
        private const int HammerFloorContactDelay = 4;

        // Quake Slam: impact tick (EaseInTicks 10 + 4 to the floor), the Crippled pulse and the planted punish window.
        private const int QuakeImpactTick = 14;
        private const float QuakeCrippleRadius = 60f * 16f;
        private const int QuakeCrippleTicks = 30 * 60;
        private const int QuakeRecoveryTicks = 50;

        #endregion

        #region Combo table

        private static MeleeComboStep HammerSwing(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            float hitWindowEnd,
            int pauseAfter,
            float damageMult,
            float forwardPushMult,
            float reachMult)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = easeOutDecay,
                HitWindowEnd = hitWindowEnd,
            };

        // Hammer Fall — timing sheet (attack-timing-design §3)
        // Poses:   -1.62 -> 2.05 rad (envelope 210°, live ≈ 168°). Identity: a vertical hammer chop, not a sweep, so it
        //          is deliberately under the 180° live default. OverheadArc (not VerticalChop) so the logical wind-up
        //          plays: the head dips to the floor, then is hauled up two-handed (settle 0.4).
        // Tell:    30t on screen (ComboTelegraphMultiplier 1, MinComboTelegraphTicks 20). Keeps walking in at chase
        //          speed; facing tracks until 10t before release.
        // Strike:  Weighted 8 / 0 / 36, k 7 -> peak 26.9°/t over AttackTicks 44.
        // Live:    HitWindowEnd 0.32 = (8 + 36*1.204/7)/44 -> 14t, under the 22t roll. Head on the floor at tick 12:
        //          stone/ember burst at the head and a small shake. No wave.
        // Open:    tail 30t + recovery 28t = 58t punish (18t of it the hammer stays down).
        // Move:    ForwardPushMult 0.8 -> 2.4 px/t (2.7 in phase 2), 34px of it inside the live window.
        // Counter: roll through / roll away / step back; 1 roll. Damage 1.0x.
        private static readonly MeleeComboStep HammerFallStep = HammerSwing(
            ComboMotion.OverheadArc, 30, 44, 8, 36, 7f, 0.32f,
            pauseAfter: 0, damageMult: 1.0f, forwardPushMult: 0.8f, reachMult: 1.1f);

        // Bull's Temper — 2(+1)-hit string.
        // Step 1:  Hammer Fall (0.9x, push 0.6), then a 10t pause that holds the low pose (6t linger).
        // Step 2:  UnderhandArc 2.05 -> -1.40 (envelope 199°, live ≈ 160°: an uppercut off the floor). Starts on step 1's
        //          end pose, so no snap. Weighted 8 / 0 / 30, k 7 -> 28.6°/t over 38t; HitWindowEnd 0.346 -> 13t live.
        //          Goes live 30 (tail) + 10 (pause) + ~4 (ease-in) = ~44t after step 1's live window ends: >= 30, so
        //          it is fair unconditionally. 1.0x, push 0.8.
        // Step 3:  Hammer Fall again (1.15x, push 1.0), CONDITIONAL (ShouldContinueMeleeCombo): step 2 hit, or the target
        //          is within 140px in front. 12t pause; the re-raise from -1.40 to -1.62 is only 13°.
        //          Live ~46t after step 2's window ends.
        // Open:    final tail 30t + recovery 34t. Counter: each step separately rollable; 2-3 rolls. Every hit is > 40t
        //          from the last, so post-hit immunity never eats a hit.
        private static readonly MeleeComboStep TemperFinisherStep = HammerSwing(
            ComboMotion.OverheadArc, 0, 44, 8, 36, 7f, 0.32f,
            pauseAfter: 0, damageMult: 1.15f, forwardPushMult: 1.0f, reachMult: 1.1f);

        // Quake Slam — signature heavy (HeavyCommit, hyper-armor through tell + strike).
        // Poses:   -1.75 -> 2.20 rad (envelope 226°, live ≈ 181°).
        // Tell:    52t planted (the only planted tell): hammer raised and held, sandy-orange embers converge on the head,
        //          helm eyes flare.
        // Strike:  Weighted 10 / 0 / 36, k 8 -> peak 28.9°/t over 46t; HitWindowEnd 0.335 -> 15t live.
        // Impact:  tick 14 (peak + 4): a pair of TaurusQuakeShockwave fronts, 30 tiles to EACH side (wall stops them),
        //          shake + debris. Same tick, each client Cripples its own player within 60 tiles for 30s flat.
        // Open:    hammer planted 50t, hyper-armor OFF — the fight's big punish window.
        // Counter: roll the blow; jump (or roll) the thin wave fronts. 1 roll + 1 jump. Crippled is unavoidable by design.
        // Select:  300t cooldown; the opening pick once in range; extra bag weight in phase 2. Damage 1.6x, waves 0.7x.
        private static readonly MeleeComboStep QuakeSlamStep = HammerSwing(
            ComboMotion.GroundSlam, 52, 46, 10, 36, 8f, 0.335f,
            pauseAfter: 0, damageMult: 1.6f, forwardPushMult: 0f, reachMult: 1.2f);

        // Leap Smash — gap closer, 200-420px, landing-timed LeapSlam.
        // Tell:    34t, hammer cocked overhead (arm-only pose). Leap solved by BeginLeapAttack; resolves on real landing.
        // Reach:   ReliableJumpStartRange = 63.3t airtime * 6.45 px/t (TopSpeed * 2.15) + 41px blade - 24 margin = 425px at
        //          phase-1 speed, so the 420px cap is reachable; selection is also gated by that function.
        // Impact:  dust ring + shake, NO shockwave (keeps Quake Slam special). Recovery 34t. Damage 1.3x.
        // Counter: roll the landing / run under the apex.
        private static readonly MeleeComboStep LeapSmashStep = new MeleeComboStep
        {
            Motion = ComboMotion.LeapSlam,
            TelegraphTicks = 34,
            AttackTicks = 90,
            DamageMult = 1.3f,
            ReachMult = 1.05f,
            SwingSpeedMult = 1f,
            Ease = SwingEaseStyle.Smooth,
        };

        private static readonly MeleeCombo[] TaurusCombos = new[]
        {
            new MeleeCombo
            {
                Name = HammerFallName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 0,
                RecoveryTicks = 28,
                MoveBrake = 0.05f,
                Steps = new[] { HammerFallStep },
            },
            new MeleeCombo
            {
                Name = BullsTemperName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 150,
                RecoveryTicks = 34,
                MoveBrake = 0.05f,
                Steps = new[]
                {
                    HammerSwing(ComboMotion.OverheadArc, 30, 44, 8, 36, 7f, 0.32f,
                        pauseAfter: 10, damageMult: 0.9f, forwardPushMult: 0.6f, reachMult: 1.1f),
                    HammerSwing(ComboMotion.UnderhandArc, 0, 38, 8, 30, 7f, 0.346f,
                        pauseAfter: 12, damageMult: 1.0f, forwardPushMult: 0.8f, reachMult: 1.1f),
                    TemperFinisherStep,
                },
            },
            new MeleeCombo
            {
                Name = QuakeSlamName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 300,
                RecoveryTicks = QuakeRecoveryTicks,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.5f,
                Steps = new[] { QuakeSlamStep },
            },
            new MeleeCombo
            {
                Name = LeapSmashName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.DarkOrange,
                CooldownAfterUse = 0,
                RecoveryTicks = 34,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.05f,
                Steps = new[] { LeapSmashStep },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => TaurusCombos;

        #endregion

        #region Loadout, stats and engine levers

        protected override string InvaderTitle => "Taurus Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;

        protected override int HeadArmorItemType => ItemID.FossilHelm;
        protected override int BodyArmorItemType => ItemID.FossilShirt;
        protected override int LegsArmorItemType => ItemID.FossilPants;

        protected override int MeleeWeaponItemType => ModContent.ItemType<TaurusGreathammer>();
        // The trident is wired as the "magic" weapon purely so the puppet can hold it during the Custom javelin stage
        // with its own grip. MagicRange 0 keeps the base from ever casting with it.
        protected override int MagicWeaponItemType => ModContent.ItemType<TaurusEarthTridentProp>();
        protected override float MagicRange => 0f;
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => BaseMeleeDamage;
        protected override int RangedDamage => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Hammer;
        protected override bool ShowWeaponDuringNeutral => true;

        protected override float TopSpeed => _phaseTwo ? PhaseTwoTopSpeed : PhaseOneTopSpeed;
        protected override float Acceleration => 0.09f;
        protected override float MeleeRange => 60f;
        protected override float ComboReachBase => HammerReachBase;
        // Fall's reach + live-window push is ~87px centre-to-centre, so any swing started from here connects.
        protected override float MeleeEngageRange => 85f;
        protected override float ComboMaxStartRange => 560f;
        protected override float ClosingDistanceSpeedMult => ClosingSpeedMult;
        protected override int ClosingDistanceMaxTicks => 150;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        // §3.1: no blanket brake in the tell; ApplyComboTelegraphPressure walks him in at chase speed instead.
        // Quake Slam plants by returning 0 here and braking in its own telegraph hook.
        protected override bool SlowDownBeforeMelee => false;
        protected override float ComboTelegraphAdvanceSpeedMult
        {
            get
            {
                if (QuakeSlamActive)
                {
                    return 0f;
                }

                if (_telegraphHoldTicks > 0)
                {
                    return ClosingSpeedMult;
                }

                return TelegraphAdvanceSpeedMult;
            }
        }
        protected override float ComboTelegraphAdvanceStopDistance => 40f;
        protected override int TelegraphFacingCommitTicks => TelegraphCommitTicks;
        protected override int MeleeRecoveryTicks => 28;
        protected override bool CanStab => false;

        // Upgrade-ladder animation axes (steps 1-5).
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override float LogicalWindupSettleFraction => 0.4f;
        protected override bool UseCompositeArmSwing => true;
        // One-handed only for the javelin throw; every hammer pose is two-handed.
        protected override bool UseTwoHandedCompositeSwing => !JavelinStageActive;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override int MeleeComboInterStepLingerTicks => 6;
        // Normal-enemy tails (shorter than a greatsword boss's 30). Quake Slam's whole recovery is the planted hammer.
        protected override int MeleeRecoveryLingerTicks => QuakeSlamActive ? QuakeRecoveryTicks : 18;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        // Quake Slam is hyper-armored from the first tell tick (§4.3); every other tell stays staggerable.
        protected override bool HyperArmorDuringTelegraph => QuakeSlamActive && Phase == AttackPhase.MeleeComboTelegraph;

        // Doomhammer art: the haft runs from the butt (1,42) to the head (32,14) along the sword-convention 45° diagonal, so
        // no rotation offset. Two-handed grip near the butt at texel (6,40).
        protected override Vector2 MeleeHandleNorm => new Vector2(6f / 46f, 40f / 46f);
        protected override float MeleeWeaponDrawScale => HammerDrawScale;
        protected override float LeapAttackForwardSpeed => TopSpeed * 2.15f;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapSlamCarryRotation => -1.62f;
        protected override float LeapSlamImpactRotation => 2.05f;

        protected override Color MeleeTelegraphFlashColor => new Color(255, 150, 60);

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            // Every overhead is a Hammer Fall and the only underhand is Temper's uppercut, so the arcs key on the motion.
            // -1.62 = cocked behind the head; 2.05 = the hammer head on the floor in front.
            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = -1.62f;
                endRotation = 2.05f;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = 2.05f;
                endRotation = -1.40f;
            }
            else if (motion == ComboMotion.GroundSlam)
            {
                startRotation = -1.75f;
                endRotation = 2.20f;
            }
        }

        // ActiveMeleeComboName is never cleared, so combo-scoped knobs also check the phase.
        private bool QuakeSlamActive =>
            ActiveMeleeComboName == QuakeSlamName
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboRecovery);

        public override void SetStaticDefaults()
        {
            // The legacy 16-frame sheet stays as the bestiary/map sprite; the puppet body is what draws in the world.
            Main.npcFrameCount[Type] = 16;
            NPCID.Sets.TrailCacheLength[Type] = 10;
            NPCID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.width = 20;
            NPC.height = 42;
            NPC.damage = 0; // Contact damage is only ever live during Bull Rush (see UpdateBullRushContactDamage).
            NPC.defense = 50;
            NPC.timeLeft = 22000;
            NPC.lifeMax = 5400; // New Enemy Balance overrides this (Systems/EnemyBalance.cs, 20000).
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 40000;
            NPC.knockBackResist = 0.01f;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TaurusKnightBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 40;
            globalNPC.CanUseRopes = true;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.PoiseStaggerResetsAI = true;
        }

        // Never despawn except by timing out.
        public override bool CheckActive()
        {
            return false;
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Set-pieces own velocity outright (DoCustomTick), except the javelin aim, which keeps walking in at half pace
            // so a ranged pick never stops the chase.
            if (Phase == AttackPhase.Custom)
            {
                if (_stage != TaurusStage.JavelinAim)
                {
                    return;
                }

                speedMult *= JavelinWalkSpeedMult;
            }

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 4,
                // SF4's re-aggro radius, not the attack range.
                attackRange: 700f);
        }

        #endregion

        #region State

        // Phase 2 (<= 50% HP). Latched on every peer from the synced NPC.life, and synced for late joiners.
        private bool _phaseTwo;

        private enum TaurusStage : byte
        {
            None,
            RushTell,
            RushCharge,
            RushSkid,
            RushTurnaround,
            JavelinAim,
            JavelinRecovery,
            BreathTell,
            Breathing,
            BreathRecovery,
            RainTell,
            RainRecovery,
        }

        // The active set-piece stage and its full length. Each stage is its own Custom phase, so elapsed = _stageTicks -
        // ticksRemaining. Both synced.
        private TaurusStage _stage;
        private int _stageTicks;
        // Swing-space weapon angle for the current Custom stage, written by DoCustomTick before TickWeaponAnim reads it.
        private float _setPieceWeaponRotation;

        // Bull Rush: locked at the end of its tell (synced).
        private int _rushDirection = 1;
        private float _rushStopX;
        private int _rushCount;
        private bool _rushWhiffed;
        // Ticks into the in-range hammer swing, or -1 before it starts. Synced: the server starts it and flushes a
        // snapshot; clients predict the same trigger from their own positions until that arrives.
        private int _rushSwingTicks = -1;
        // Server only: a contact or the swing reached the target (ReportAttackHit / OnBladeHit).
        private bool _rushConnected;
        // Local: the pose the rush ended on, so the turnaround eases from where the hammer actually is.
        private float _rushEndPose;

        // Trident Javelin volley progress (synced).
        private int _javelinThrowsDone;
        private int _javelinThrowsTotal;

        // Bull Breath: world-space angle of the stream, turned toward the target at a capped rate (synced start).
        private float _breathAngle;

        // Server-only selection state: not synced because only the server ever selects.
        private enum RangedOption
        {
            LeapSmash,
            BullRush,
            Javelin,
            Breath,
            Rain,
        }
        private const int RangedOptionCount = 5;
        private readonly int[] _rangedCooldowns = new int[RangedOptionCount];
        private int _rangedBag;
        private int _lastRangedOption = -1;
        private int _rangedPickDelay;
        private bool _leapSmashRequested;
        private readonly List<int> _meleeBag = new List<int>();
        private int _lastMeleeCombo = -1;
        private bool _openingQuakeDone;
        private bool _abortTelegraphRequested;

        // Local: ticks the current tell has been held waiting for the target to come into reach.
        private int _telegraphHoldTicks;

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_phaseTwo);
            writer.Write((byte)_stage);
            writer.Write((short)_stageTicks);
            writer.Write((sbyte)_rushDirection);
            writer.Write(_rushStopX);
            writer.Write((byte)_rushCount);
            writer.Write(_rushWhiffed);
            writer.Write((byte)_javelinThrowsDone);
            writer.Write((byte)_javelinThrowsTotal);
            writer.Write(_breathAngle);
            writer.Write((short)_rushSwingTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phaseTwo = reader.ReadBoolean();
            _stage = (TaurusStage)reader.ReadByte();
            _stageTicks = reader.ReadInt16();
            _rushDirection = reader.ReadSByte();
            _rushStopX = reader.ReadSingle();
            _rushCount = reader.ReadByte();
            _rushWhiffed = reader.ReadBoolean();
            _javelinThrowsDone = reader.ReadByte();
            _javelinThrowsTotal = reader.ReadByte();
            _breathAngle = reader.ReadSingle();
            _rushSwingTicks = reader.ReadInt16();
        }

        #endregion

        #region Spawn, death, loot

        // Legacy spawn rolls, used only with New Enemy Spawns off; otherwise the registry (Systems/EnemySpawns.cs) decides.
        // No spawn announcement: the old "A Taurus Knight is close..." chat line was removed on 2026-10-21.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (EnemySpawns.Enabled)
            {
                return 0f;
            }

            Player player = spawnInfo.Player;
            bool desert = player.ZoneDesert || player.ZoneUndergroundDesert;
            bool dungeon = player.ZoneDungeon;
            // SpawnTileY is a tile row, so the underworld starts 200 rows above the bottom (not 200*16).
            bool inHell = spawnInfo.SpawnTileY >= Main.maxTilesY - 200;

            // Only one at a time. AnyNPCs checks active slots; despawned slots keep their old type.
            if (NPC.AnyNPCs(NPC.type))
            {
                return 0f;
            }

            if (tsorcRevampWorld.SuperHardMode && desert && Main.rand.NextBool(45))
            {
                return 1f;
            }

            if (tsorcRevampWorld.SuperHardMode && dungeon && Main.rand.NextBool(50))
            {
                return 1f;
            }

            if (tsorcRevampWorld.SuperHardMode && inHell && Main.rand.NextBool(30))
            {
                return 1f;
            }

            return 0f;
        }

        public override void OnKill()
        {
            if (Main.dedServ)
            {
                return;
            }

            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            for (int i = 0; i < 6; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<RedTitanite>(), 1, 3, 5));
        }

        #endregion

        #region Per-tick driver

        public override void AI()
        {
            UpdatePhaseTwoLatch();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                TickSelectionCooldowns();
                TrySelectRangedOption();
            }

            base.AI();

            // A tell that waited MaxTelegraphHoldTicks for the target to come into reach gives up here, after the engine's
            // tick, so the held PhaseTimer can't release the swing on the way out.
            if (_abortTelegraphRequested)
            {
                _abortTelegraphRequested = false;
                if (Phase == AttackPhase.MeleeComboTelegraph)
                {
                    EnterPhase(AttackPhase.Idle, 0);
                    NPC.netUpdate = true;
                }
            }

            if (Phase != AttackPhase.Custom && _stage != TaurusStage.None)
            {
                // Stagger, a teleport or any other interruption dropped the set-piece; forget it.
                _stage = TaurusStage.None;
            }

            UpdateBullRushContactDamage();
            UpdateSetPiecePoise();
            EmitPhaseTwoAura();
        }

        private void UpdatePhaseTwoLatch()
        {
            if (_phaseTwo || NPC.lifeMax <= 0)
            {
                return;
            }

            float healthFraction = NPC.life / (float)NPC.lifeMax;
            if (healthFraction > PhaseTwoHealthFraction)
            {
                return;
            }

            // Driven by the synced NPC.life, so every peer flips on the same tick without a decision of its own.
            _phaseTwo = true;
            NPC.netUpdate = true;

            if (Main.dedServ)
            {
                return;
            }

            // Readable, not a stat jump: a roar, a sand-and-ember burst off the Fossil armour, and the eyes stay lit.
            SoundEngine.PlaySound(SoundID.DD2_OgreRoar with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 5f, 18, distanceFalloff: 900f);
            for (int i = 0; i < 60; i++)
            {
                bool ember = i % 3 == 0;
                int dustType = DustID.Sand;
                Color tint = new Color(214, 170, 110);
                if (ember)
                {
                    dustType = DustID.OrangeTorch;
                    tint = default;
                }

                Dust burst = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 20f), dustType,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f), 60, tint,
                    Main.rand.NextFloat(1.1f, 1.7f));
                burst.noGravity = true;
            }
        }

        private Vector2 HelmPosition => NPC.Top + new Vector2(NPC.direction * 6f, 12f);

        private void EmitPhaseTwoAura()
        {
            if (Main.dedServ || Main.gamePaused)
            {
                return;
            }

            // Helm eyes: a small warm glow in both phases, brighter and burning in phase 2.
            float eyeGlow = _phaseTwo ? 0.7f : 0.25f;
            Lighting.AddLight(HelmPosition, new Vector3(1f, 0.45f, 0.1f) * eyeGlow);

            if (!_phaseTwo)
            {
                return;
            }

            if (Main.GameUpdateCount % 4 == 0)
            {
                Dust ember = Dust.NewDustPerfect(HelmPosition + Main.rand.NextVector2Circular(4f, 3f), DustID.OrangeTorch,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.6f, 1.4f)), 60, default,
                    Main.rand.NextFloat(0.8f, 1.2f));
                ember.noGravity = true;
            }

            if (Main.GameUpdateCount % 6 == 0)
            {
                Vector2 onArmour = NPC.Center + new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-16f, 18f));
                Dust sand = Dust.NewDustPerfect(onArmour, DustID.Sand, new Vector2(0f, 0.6f), 110,
                    new Color(214, 170, 110), Main.rand.NextFloat(0.7f, 1f));
                sand.noGravity = false;
            }
        }

        #endregion

        #region Melee selection, aggression and combo hooks

        /// <summary>Whether this step's reach plus the ground its forward push covers inside the live window connects
        /// from <paramref name="distance"/> (centre to centre). Owl Father's WouldForwardPushedSwingConnect, measured over
        /// the armed ticks rather than the whole step, because a Weighted swing's tail cannot hit.</summary>
        private bool WouldStepConnect(MeleeComboStep step, float distance)
        {
            float reach = ComboReachBase * 0.7f * step.ReachMult;
            float liveTicks = step.AttackTicks;
            if (step.HitWindowEnd > 0f)
            {
                liveTicks = step.AttackTicks * step.HitWindowEnd;
            }

            float pushDistance = ComboForwardPushTopSpeed * step.ForwardPushMult * liveTicks;
            return distance <= reach + pushDistance + TargetHalfWidth;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == LeapSmashName)
            {
                // Dealt only by the ranged bag (TrySelectRangedOption), never by the engine's own ranged-start roll.
                return _leapSmashRequested;
            }

            if (combo.Name == QuakeSlamName)
            {
                return distance <= QuakeStartRange;
            }

            // §3.1 "never swing at air": the first step must be able to connect from here.
            return WouldStepConnect(combo.Steps[0], distance);
        }

        protected override int ReactiveComboIndex(float distance, ComboRangeBand band, int[] ready)
        {
            if (ready.Length <= LeapSmashIndex)
            {
                return -1;
            }

            if (_leapSmashRequested)
            {
                return ready[LeapSmashIndex] > 0 ? LeapSmashIndex : -1;
            }

            // §4.3: open the fight with Quake Slam so Crippled is up for the melee exchanges.
            if (!_openingQuakeDone && ready[QuakeSlamIndex] > 0)
            {
                return QuakeSlamIndex;
            }

            // Bag dealing (attack-quality-pass §6): cooldowns and range narrow the bag, the bag picks, no immediate repeats.
            // Phase 2 deals Temper and Quake twice per bag (Quake's weight-up).
            for (int i = _meleeBag.Count - 1; i >= 0; i--)
            {
                int comboIndex = _meleeBag[i];
                if (ready[comboIndex] <= 0)
                {
                    _meleeBag.RemoveAt(i);
                }
            }

            if (_meleeBag.Count == 0)
            {
                RefillMeleeBag(ready);
            }

            if (_meleeBag.Count == 0)
            {
                return -1;
            }

            int drawPosition = Main.rand.Next(_meleeBag.Count);
            bool repeatsLast = _meleeBag[drawPosition] == _lastMeleeCombo;
            if (repeatsLast && _meleeBag.Count > 1)
            {
                for (int i = 0; i < _meleeBag.Count; i++)
                {
                    if (_meleeBag[i] != _lastMeleeCombo)
                    {
                        drawPosition = i;
                        break;
                    }
                }
            }

            int chosen = _meleeBag[drawPosition];
            _meleeBag.RemoveAt(drawPosition);
            _lastMeleeCombo = chosen;
            return chosen;
        }

        private void RefillMeleeBag(int[] ready)
        {
            List<int> entries = new List<int> { HammerFallIndex, HammerFallIndex, BullsTemperIndex, QuakeSlamIndex };
            if (_phaseTwo)
            {
                entries = new List<int> { HammerFallIndex, BullsTemperIndex, BullsTemperIndex, QuakeSlamIndex, QuakeSlamIndex };
            }

            foreach (int comboIndex in entries)
            {
                if (ready[comboIndex] > 0)
                {
                    _meleeBag.Add(comboIndex);
                }
            }
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            DebugAttackLabel = combo.Name;
            _telegraphHoldTicks = 0;

            if (combo.Name == QuakeSlamName)
            {
                _openingQuakeDone = true;
            }

            if (combo.Name == LeapSmashName && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _rangedCooldowns[(int)RangedOption.LeapSmash] = LeapSmashCooldownTicks;
            }
        }

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (_phaseTwo)
            {
                combo.RecoveryTicks = (int)(combo.RecoveryTicks * PhaseTwoRecoveryMult);
            }
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == BullsTemperName && nextStepIndex == 2)
            {
                // The finisher only follows a connected uppercut or a target still close in front (checked before the
                // pause re-faces): backing off more than 140px cancels it.
                float horizontalGap = target.Center.X - NPC.Center.X;
                bool inFront = Math.Sign(horizontalGap) == NPC.direction;
                bool close = NPC.Distance(target.Center) <= 140f;
                return previousStepHit || (inFront && close);
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == QuakeSlamName)
            {
                // The only planted tell (ComboTelegraphAdvanceSpeedMult is 0 for it).
                NPC.velocity.X *= 0.7f;
                EmitQuakeTell(step, elapsed, total);
                return;
            }

            if (combo.Name == LeapSmashName)
            {
                return;
            }

            HoldTellUntilInReach(step, elapsed, total);
        }

        /// <summary>
        /// §3.1: TelegraphFacingCommitTicks before release, re-check that the first step can connect. If not, hold the tell
        /// in the cocked pose (PhaseTimer pinned, so facing keeps tracking) while ComboTelegraphAdvanceSpeedMult chases at
        /// closing speed, for up to MaxTelegraphHoldTicks; then the server gives up and re-picks. Clients hold too (no
        /// local give-up), so a slow snapshot can't make them swing at air the server never released.
        /// </summary>
        private void HoldTellUntilInReach(MeleeComboStep step, int elapsed, int total)
        {
            if (elapsed == 0)
            {
                _telegraphHoldTicks = 0;
            }

            int holdAtElapsed = total - TelegraphCommitTicks - 1;
            if (elapsed != holdAtElapsed || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            if (WouldStepConnect(step, distance))
            {
                return;
            }

            // The engine decrements PhaseTimer right after this hook; adding one back holds elapsed in place.
            PhaseTimer++;
            _telegraphHoldTicks++;

            bool heldTooLong = _telegraphHoldTicks > MaxTelegraphHoldTicks;
            if (heldTooLong && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _abortTelegraphRequested = true;
            }
        }

        private void EmitQuakeTell(MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            float progress = (elapsed + 1f) / Math.Max(1, total);
            float headReach = ComboReachBase * 0.7f * step.ReachMult * 0.85f;
            Vector2 hammerHead = PuppetWeaponTipPosition(headReach);

            // Sandy-orange embers converge on the hammer head, denser as the slam nears.
            int emberCount = 1;
            if (progress > 0.5f)
            {
                emberCount = 2;
            }

            for (int i = 0; i < emberCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * MathHelper.Lerp(56f, 18f, progress);
                Dust ember = Dust.NewDustPerfect(hammerHead + offset, DustID.OrangeTorch, -offset * 0.07f, 50,
                    new Color(255, 170, 80), MathHelper.Lerp(0.8f, 1.5f, progress));
                ember.noGravity = true;
            }

            // Helm eyes flare through the tell.
            Lighting.AddLight(HelmPosition, new Vector3(1f, 0.5f, 0.12f) * (0.5f + progress));
            Lighting.AddLight(hammerHead, new Vector3(1f, 0.55f, 0.2f) * progress);

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.DD2_OgreRoar with { Volume = 0.55f, Pitch = 0.2f }, NPC.Center);
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (step.Motion == ComboMotion.LeapSlam)
            {
                return;
            }

            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            bool live = elapsed < step.HitWindowEnd * total;
            if (live && !Main.dedServ && elapsed >= step.EaseInTicks / 2)
            {
                // Grit flung off the hammer head while it is armed (and only then: no dust advertises a dead blade).
                Dust grit = Dust.NewDustPerfect(PuppetWeaponTipPosition(bladeReach * 0.85f), DustID.Sand,
                    -PuppetWeaponDirection * Main.rand.NextFloat(0.5f, 1.5f), 90, new Color(214, 170, 110),
                    Main.rand.NextFloat(0.9f, 1.3f));
                grit.noGravity = true;
            }

            if (combo.Name == QuakeSlamName && elapsed == QuakeImpactTick)
            {
                QuakeImpact(step);
                return;
            }

            int floorContactTick = step.EaseInTicks + HammerFloorContactDelay;
            if (step.Motion == ComboMotion.OverheadArc && elapsed == floorContactTick)
            {
                HammerFloorImpact(bladeReach);
            }
        }

        private void HammerFloorImpact(float bladeReach)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 head = PuppetWeaponTipPosition(bladeReach * 0.85f);
            for (int i = 0; i < 16; i++)
            {
                bool ember = i % 3 == 0;
                int dustType = DustID.Stone;
                if (ember)
                {
                    dustType = DustID.OrangeTorch;
                }

                Dust chip = Dust.NewDustPerfect(head, dustType,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 4f)), 60, default,
                    Main.rand.NextFloat(0.9f, 1.4f));
                chip.noGravity = ember;
            }

            UsefulFunctions.ScreenShake(head, 2f, 8, distanceFalloff: 450f);
            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.5f, Pitch = -0.3f }, head);
        }

        private void QuakeImpact(MeleeComboStep step)
        {
            float headReach = ComboReachBase * 0.7f * step.ReachMult * 0.85f;
            Vector2 head = PuppetWeaponTipPosition(headReach);
            Vector2 impact = head;
            if (PuppetGroundDustWave.TryFindGroundY(head.X, NPC.Bottom.Y, out float groundY))
            {
                impact = new Vector2(head.X, groundY);
            }

            // Server: the two shockwave fronts. Spawned at their own height; they snap their feet to the floor.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int waveDamage = (int)(MeleeDamage * QuakeWaveDamageMult);
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector2 waveStart = impact + new Vector2(direction * 8f, -16f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), waveStart, Vector2.Zero,
                        ModContent.ProjectileType<TaurusQuakeShockwave>(), waveDamage, 4f, Main.myPlayer, direction);
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            // The quake: every client checks its own player against the impact on this (synced) tick and Cripples it
            // locally — no server-to-client AddBuff. Flat 30s, deliberately unavoidable: it strips wings and extra jumps.
            Player local = Main.LocalPlayer;
            bool inQuake = local.active && !local.dead
                && Vector2.DistanceSquared(local.Center, impact) <= QuakeCrippleRadius * QuakeCrippleRadius;
            if (inQuake)
            {
                local.AddBuff(ModContent.BuffType<Crippled>(), QuakeCrippleTicks);
            }

            SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 1f, Pitch = -0.2f }, impact);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.4f }, impact);
            UsefulFunctions.ScreenShake(impact, 9f, 26, distanceFalloff: QuakeCrippleRadius + 240f);

            // Debris at the head.
            for (int i = 0; i < 40; i++)
            {
                bool ember = i % 4 == 0;
                int dustType = DustID.Sand;
                Color tint = new Color(214, 170, 110);
                if (ember)
                {
                    dustType = DustID.OrangeTorch;
                    tint = default;
                }

                Dust debris = Dust.NewDustPerfect(impact + new Vector2(Main.rand.NextFloat(-12f, 12f), -2f), dustType,
                    new Vector2(Main.rand.NextFloat(-5f, 5f), -Main.rand.NextFloat(2f, 7f)), 60, tint,
                    Main.rand.NextFloat(1.1f, 1.8f));
                debris.noGravity = ember;
            }

            // Ground-rumble pulse across the Crippled radius: puffs on the floor so the 60-tile reach is visible.
            for (int i = 0; i < 48; i++)
            {
                float x = impact.X + Main.rand.NextFloat(-QuakeCrippleRadius, QuakeCrippleRadius);
                if (!PuppetGroundDustWave.TryFindGroundY(x, impact.Y, out float pulseGroundY))
                {
                    continue;
                }

                Dust rumble = Dust.NewDustPerfect(new Vector2(x, pulseGroundY - 2f), DustID.Sand,
                    new Vector2(0f, -Main.rand.NextFloat(1f, 2.5f)), 120, new Color(196, 160, 104),
                    Main.rand.NextFloat(1f, 1.5f));
                rumble.noGravity = false;
            }
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (ActiveMeleeComboName != LeapSmashName || Main.dedServ)
            {
                return;
            }

            // Leap Smash impact: dust ring + shake, deliberately no shockwave.
            Vector2 impact = NPC.Bottom - new Vector2(0f, 4f);
            UsefulFunctions.DustRing(impact, 40f, DustID.Sand, 24, 4f);
            for (int i = 0; i < 20; i++)
            {
                Dust chip = Dust.NewDustPerfect(impact + new Vector2(Main.rand.NextFloat(-16f, 16f), 0f), DustID.Stone,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(1.5f, 4.5f)), 60, default,
                    Main.rand.NextFloat(0.9f, 1.3f));
                chip.noGravity = false;
            }

            UsefulFunctions.ScreenShake(impact, 4f, 12, distanceFalloff: 600f);
            SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.9f }, impact);
        }

        protected override void DoMeleeAttack()
        {
            // Legacy single-swing fallback; only reachable if every combo is unselectable at point-blank range.
            PlayMeleeSwingSound();
            TryMeleeHit();
        }

        protected override void DoRangedAttack()
        {
        }

        #endregion

        #region Set-piece selection (server)

        private const int LeapSmashCooldownTicks = 240;
        private const int BullRushCooldownTicks = 300;
        private const int BullRushPhaseTwoCooldownTicks = 240;
        private const int JavelinCooldownTicks = 260;
        private const int BreathCooldownTicks = 420;
        private const int RainCooldownTicks = 600;
        // A breather after any set-piece before the next ranged pick, and a per-tick roll so closing on foot still happens.
        private const int RangedPickGapTicks = 20;
        private const int RangedPickChanceDenominator = 15;

        private void TickSelectionCooldowns()
        {
            for (int i = 0; i < RangedOptionCount; i++)
            {
                if (_rangedCooldowns[i] > 0)
                {
                    _rangedCooldowns[i]--;
                }
            }

            if (_rangedPickDelay > 0)
            {
                _rangedPickDelay--;
            }
        }

        /// <summary>
        /// Server: outside melee reach, deal one ranged option from a bag (Leap Smash, Bull Rush, Javelin, and in phase 2
        /// Breath and Rain). Range and terrain gates narrow the bag; the bag picks without an immediate repeat.
        /// </summary>
        private void TrySelectRangedOption()
        {
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.ClosingDistance || Phase == AttackPhase.CasualStroll;
            if (!neutral || _rangedPickDelay > 0 || !NPC.HasValidTarget || NPC.velocity.Y != 0f)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool busy = globalNPC.StaggerTimer > 0 || globalNPC.TeleportCountdown > 0 || globalNPC.DodgeTimer > 0;
            if (busy || !Main.rand.NextBool(RangedPickChanceDenominator))
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            // §3.1: ranged picks are only selectable outside melee reach.
            if (distance <= MeleeEngageRange)
            {
                return;
            }

            int eligible = EligibleRangedOptions(target, distance);
            if (eligible == 0)
            {
                return;
            }

            // Refill when nothing still in the bag is eligible; never deal the same option twice running if avoidable.
            if ((_rangedBag & eligible) == 0)
            {
                _rangedBag = (1 << RangedOptionCount) - 1;
            }

            int choices = _rangedBag & eligible;
            int withoutLast = choices;
            if (_lastRangedOption >= 0)
            {
                withoutLast = choices & ~(1 << _lastRangedOption);
            }

            if (withoutLast != 0)
            {
                choices = withoutLast;
            }

            RangedOption option = (RangedOption)PickRandomBit(choices);
            bool started = StartRangedOption(option, target, distance);
            if (!started)
            {
                return;
            }

            _rangedBag &= ~(1 << (int)option);
            _lastRangedOption = (int)option;
        }

        private static int PickRandomBit(int mask)
        {
            List<int> bits = new List<int>();
            for (int i = 0; i < RangedOptionCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    bits.Add(i);
                }
            }

            return bits[Main.rand.Next(bits.Count)];
        }

        private int EligibleRangedOptions(Player target, float distance)
        {
            int eligible = 0;
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            float leapMaxRange = Math.Min(420f, ReliableJumpStartRange(LeapSmashStep));
            bool leapReady = _rangedCooldowns[(int)RangedOption.LeapSmash] <= 0;
            if (leapReady && distance >= 200f && distance <= leapMaxRange && verticalGap < 64f)
            {
                eligible |= 1 << (int)RangedOption.LeapSmash;
            }

            bool rushReady = _rangedCooldowns[(int)RangedOption.BullRush] <= 0;
            if (rushReady && distance >= 160f && distance <= 560f && verticalGap < 48f && lineOfSight
                && HasGroundAlongRushLine(Math.Sign(target.Center.X - NPC.Center.X)))
            {
                eligible |= 1 << (int)RangedOption.BullRush;
            }

            bool javelinReady = _rangedCooldowns[(int)RangedOption.Javelin] <= 0;
            if (javelinReady && distance >= 300f && distance <= 750f && lineOfSight)
            {
                eligible |= 1 << (int)RangedOption.Javelin;
            }

            if (!_phaseTwo)
            {
                return eligible;
            }

            bool breathReady = _rangedCooldowns[(int)RangedOption.Breath] <= 0;
            if (breathReady && distance <= 420f && verticalGap < 160f && lineOfSight)
            {
                eligible |= 1 << (int)RangedOption.Breath;
            }

            bool rainReady = _rangedCooldowns[(int)RangedOption.Rain] <= 0;
            if (rainReady && distance >= 300f && distance <= 900f)
            {
                eligible |= 1 << (int)RangedOption.Rain;
            }

            return eligible;
        }

        /// <summary>Bull Rush gate: solid footing under each of the first 6 tiles of the charge line (no charging off
        /// cliffs from the start; the charge also stops at a ledge mid-run).</summary>
        private bool HasGroundAlongRushLine(int direction)
        {
            if (direction == 0)
            {
                return false;
            }

            for (int tile = 1; tile <= 6; tile++)
            {
                Vector2 underFoot = new Vector2(NPC.Center.X + direction * tile * 16f - 8f, NPC.Bottom.Y);
                bool supported = Collision.SolidCollision(underFoot, 16, 20, true);
                if (!supported)
                {
                    return false;
                }
            }

            return true;
        }

        private bool StartRangedOption(RangedOption option, Player target, float distance)
        {
            _rangedPickDelay = RangedPickGapTicks;

            if (option == RangedOption.LeapSmash)
            {
                _leapSmashRequested = true;
                bool started = TryStartMeleeCombo(distance, rangedStartOnly: true);
                _leapSmashRequested = false;
                return started;
            }

            if (option == RangedOption.BullRush)
            {
                int cooldown = BullRushCooldownTicks;
                if (_phaseTwo)
                {
                    cooldown = BullRushPhaseTwoCooldownTicks;
                }

                _rangedCooldowns[(int)RangedOption.BullRush] = cooldown;
                _rushCount = 0;
                _rushWhiffed = false;
                DebugAttackLabel = "Bull Rush";
                EnterStage(TaurusStage.RushTell, RushTellTicks);
                return true;
            }

            if (option == RangedOption.Javelin)
            {
                _rangedCooldowns[(int)RangedOption.Javelin] = JavelinCooldownTicks;
                _javelinThrowsDone = 0;
                _javelinThrowsTotal = 1;
                if (_phaseTwo)
                {
                    _javelinThrowsTotal = JavelinVolleyCount;
                }

                DebugAttackLabel = "Trident Javelin";
                EnterStage(TaurusStage.JavelinAim, JavelinFirstAimTicks);
                return true;
            }

            if (option == RangedOption.Breath)
            {
                _rangedCooldowns[(int)RangedOption.Breath] = BreathCooldownTicks;
                DebugAttackLabel = "Bull Breath";
                EnterStage(TaurusStage.BreathTell, BreathTellTicks);
                return true;
            }

            _rangedCooldowns[(int)RangedOption.Rain] = RainCooldownTicks;
            DebugAttackLabel = "Brimstone Rain";
            EnterStage(TaurusStage.RainTell, RainTellTicks);
            return true;
        }

        #endregion

        #region Set-piece state machine (AttackPhase.Custom, one phase per stage)

        // Bull Rush — identity move (160-560px). Bespoke: ChargeChop re-faces every tick and deals no damage.
        // Tell:    36t, hammer hauled up and cocked overhead (Hammer Fall's -1.62 start), hoof-stamp puffs at ticks 12 and
        //          26 + snort, eyes flare. The direction and the stop point (180px PAST the target's x) lock on the last tick.
        // Charge:  0 -> 9.5 px/t over 12t (quadratic ease-in), cruise until past the stop point, a wall, a ledge, a
        //          connect, or 70t. Body contact damage only while speed >= 50% of top (1.3x). Hyper-armor on.
        // Swing:   once the target is in front and within blade reach + the ease-in's travel (~115px at top speed), the
        //          hammer comes down while he keeps running: Weighted 6 / 22, k 7 over 28t, -1.62 -> 2.05 (210°), peak
        //          ~41°/t, live 9.8t (rollable). A started swing overrides the stop point and plays out; it can carry on
        //          into the skid. Damage 1.0x blade.
        // On hit:  body contact knocks back 9 along the line (OnHitPlayer); any connect ends the run into a 16t skid.
        // Whiff:   24t skid with a dust trail, then a 40t turnaround (punish; armor off).
        // Counter: jump it (~46px tall body), roll through the swing (~10t live, well inside 22t of i-frames), or leave its
        //          row. Rolling away does not work: the overshoot is the point, and the tell is 36t.
        // Phase 2: a whiffed first rush turns around (the 40t turnaround doubles as the second tell, > the 30t gap rule)
        //          and charges again. Max 2.
        private const int RushTellTicks = 36;
        private const float RushTopSpeed = 9.5f;
        private const int RushAccelerationTicks = 12;
        private const float RushOvershootPixels = 180f;
        private const int RushMaxChargeTicks = 70;
        private const float RushContactSpeedFraction = 0.5f;
        private const float RushKnockback = 9f;
        private const float RushDamageMult = 1.3f;
        private const int RushHitSkidTicks = 16;
        private const int RushWhiffSkidTicks = 24;
        private const int RushTurnaroundTicks = 40;
        private const int RushStampTickA = 12;
        private const int RushStampTickB = 26;
        private const int MaxRushesPhaseTwo = 2;
        private static readonly WeightedSwing RushStrikeCurve = new WeightedSwing(6, 22, 7f);
        private const float RushStrikeReachMult = 1.1f;
        // Vertical window for the in-range swing: the target must be roughly in the charge's row.
        private const float RushStrikeMaxVerticalGap = 56f;

        // Trident Javelin — ranged answer (300-750px).
        // Prop:    the hammer is swapped for the Earth Trident at the tell's first tick. Grip texel (8,67) of 16x108 at 0.9
        //          scale, 60px to the tip.
        // Tell:    58t: raised above the head over 10t, held there, then thrown forward onto the solved release angle over
        //          the last 10t. A dotted aim arc along the solved trajectory shows for the last 18t; earthy dust at the
        //          tip; Item60. (2026-10-28: 28 -> 58t so the raised trident reads before the throw.)
        // Release: from the tip. Lob solved for gravity 0.1/t² to land 60px PAST the player's feet (fixes the land-short):
        //          14 px/t, ~28t across 400px. An on-time throw passes through a standing player at knee height.
        // Phase 2: 3 javelins, 14t apart, re-aimed each throw. Recovery 20t (17 in phase 2), the hammer swapped back.
        private const int JavelinFirstAimTicks = 58;
        private const int JavelinReaimTicks = 14;
        private const int JavelinRecoveryTicks = 20;
        private const int JavelinVolleyCount = 3;
        private const float JavelinSpeed = 14f;
        private const float JavelinOvershootPixels = 60f;
        private const float JavelinWalkSpeedMult = 0.5f;
        private const float JavelinGripToTipPixels = 67f * JavelinDrawScale;
        private const float JavelinDrawScale = 0.9f;
        // Raised-above-the-head hold, in facing space: -100° = straight up, tipped slightly back.
        private const float JavelinRaisedFacingAngle = -100f * MathHelper.Pi / 180f;
        private const int JavelinRaiseTicks = 10;
        private const int JavelinThrowSwingTicks = 10;
        private const int JavelinAimArcTicks = 18;

        // Bull Breath — phase 2, <= 420px.
        // Tell:    40t, hammer planted head-down in front, a shrinking torch ring + light swell on the HELM, Item119.
        // Breath:  36t, one FireBreath every 2t from the helm (+10px forward). Aim starts on the player and turns at most
        //          1.2°/t toward them, within 80° of facing: walking or rolling out of the cone works, standing still
        //          doesn't. Planted. Recovery 30t (25 in phase 2, which is the only phase it exists in).
        private const int BreathTellTicks = 40;
        private const int BreathTicks = 36;
        private const int BullBreathRecoveryTicks = 30;
        private const int BreathInterval = 2;
        private const float BreathSpeed = 12f;
        private const float BreathTurnRadiansPerTick = 1.2f * MathHelper.Pi / 180f;
        private const float BreathMaxAngleFromFacing = 80f * MathHelper.Pi / 180f;
        private const float BreathMouthForward = 10f;

        // Brimstone Rain — phase 2, 300-900px, Destined Death colours and debuff.
        // Tell:    40t: hammer raised skyward with crimson and soot spiralling up off the head (30t, Item170), then struck
        //          down onto the floor (10t). The strike spawns the volley.
        // Birth:   10 drop points 450px above the player across 500px (lowered under a ceiling), swept from one side at 6t
        //          intervals; each shows 30t of converging crimson dust before its flame appears (TaurusBrimstoneFlame).
        // Life:    5 px/t falls (~90t), sparse crimson/soot trail; tile or player -> small Destined Death burst.
        // Debuff:  DestinedDeath 600t per hit; 40t post-hit immunity caps one rain at ~2 stacks on a player standing in it.
        // Recovery 24t (20 in phase 2).
        private const int RainTellTicks = 40;
        private const int RainRaiseTicks = 30;
        private const int RainRecoveryTicks = 24;
        private const int RainDropCount = 10;
        private const float RainSpanPixels = 500f;
        private const float RainHeightPixels = 450f;
        private const int RainDropIntervalTicks = 6;

        // Swing-space poses for the Custom stages (0 = the sprite's 45° up-forward diagonal; world = rotation - 45°).
        private const float HammerChargeCarryPose = -1.62f; // cocked behind the head (Hammer Fall's start)
        private const float HammerHoldPose = -0.30f;       // the engine's ordinary carry (HoldRotation)
        private const float HammerPlantedPose = 2.0f;      // head on the floor in front
        private const float HammerSkywardPose = -0.785f;   // straight up
        private const float HammerFloorStrikePose = 2.05f;

        private bool JavelinStageActive => Phase == AttackPhase.Custom && _stage == TaurusStage.JavelinAim;

        // Weapon held in each stage: the trident only while aiming.
        private int StageWeapon(TaurusStage stage)
        {
            if (stage == TaurusStage.JavelinAim)
            {
                return MagicWeaponItemType;
            }

            return MeleeWeaponItemType;
        }

        private float RecoveryScale => _phaseTwo ? PhaseTwoRecoveryMult : 1f;

        /// <summary>Enters a stage as a fresh Custom phase. EnterPhase bumps the puppet's phase sequence, so the stage change
        /// reaches clients in the next snapshot; timer-driven transitions also run on clients by prediction.</summary>
        private void EnterStage(TaurusStage stage, int ticks)
        {
            _stage = stage;
            _stageTicks = Math.Max(1, ticks);
            StartCustomAttack(_stageTicks, StageWeapon(stage), swingPose: false);
            NPC.netUpdate = true;
        }

        /// <summary>Last tick of the last stage: drop the set-piece; the engine returns to Idle when the phase expires.</summary>
        private void EndSetPiece()
        {
            _stage = TaurusStage.None;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _rangedPickDelay = RangedPickGapTicks;
            }
        }

        protected override bool SlowDownDuringCustom => false;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        protected override float? CustomWeaponRotation => Phase == AttackPhase.Custom ? _setPieceWeaponRotation : null;
        // The trident's sprite points UP; +45° puts it on the sword convention so the arm and the shaft agree (the arm
        // aims along swing-space rotation - 45°, the drawn trident along rotation + 45° - 90°).
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override Vector2 MagicGripNorm => new Vector2(0.5f, 67f / 108f);
        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == MagicWeaponItemType)
            {
                return JavelinDrawScale;
            }

            return base.GetHeldRangedDrawScale(itemType);
        }

        protected override void DoCustomAttack()
        {
            // Stage entry cues. Runs where the stage starts and on a client that adopts it from a snapshot.
            if (Main.dedServ)
            {
                return;
            }

            if (_stage == TaurusStage.RushTell)
            {
                SoundEngine.PlaySound(SoundID.DD2_OgreRoar with { Volume = 0.5f, Pitch = 0.45f }, NPC.Center);
            }
            else if (_stage == TaurusStage.RushCharge)
            {
                SoundEngine.PlaySound(SoundID.DD2_OgreGroundPound with { Volume = 0.6f, Pitch = 0.1f }, NPC.Bottom);
            }
            else if (_stage == TaurusStage.JavelinAim && _javelinThrowsDone == 0)
            {
                // Mod-wide cue: a weapon first appearing in a ranged telegraph.
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.6f }, NPC.Center);
            }
            else if (_stage == TaurusStage.BreathTell)
            {
                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }
            else if (_stage == TaurusStage.RainTell)
            {
                SoundEngine.PlaySound(SoundID.Item170 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int elapsed = _stageTicks - ticksRemaining;
            bool lastTick = ticksRemaining <= 1;

            switch (_stage)
            {
                case TaurusStage.RushTell:
                    TickRushTell(target, elapsed, lastTick);
                    break;
                case TaurusStage.RushCharge:
                    TickRushCharge(target, elapsed, lastTick);
                    break;
                case TaurusStage.RushSkid:
                    TickRushSkid(lastTick);
                    break;
                case TaurusStage.RushTurnaround:
                    TickRushTurnaround(target, elapsed, lastTick);
                    break;
                case TaurusStage.JavelinAim:
                    TickJavelinAim(target, elapsed, lastTick);
                    break;
                case TaurusStage.JavelinRecovery:
                    TickJavelinRecovery(elapsed, lastTick);
                    break;
                case TaurusStage.BreathTell:
                    TickBreathTell(target, elapsed, lastTick);
                    break;
                case TaurusStage.Breathing:
                    TickBreathing(target, elapsed, lastTick);
                    break;
                case TaurusStage.BreathRecovery:
                    TickPlantedRecovery(HammerPlantedPose, lastTick);
                    break;
                case TaurusStage.RainTell:
                    TickRainTell(target, elapsed, lastTick);
                    break;
                case TaurusStage.RainRecovery:
                    TickPlantedRecovery(HammerFloorStrikePose, lastTick);
                    break;
            }
        }

        private void FaceTarget(Player target)
        {
            int face = Math.Sign(target.Center.X - NPC.Center.X);
            if (face == 0)
            {
                face = NPC.direction;
            }

            NPC.direction = face;
            NPC.spriteDirection = face;
        }

        private void FaceDirection(int direction)
        {
            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        // ── Bull Rush ──

        private void TickRushTell(Player target, int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.8f;
            FaceTarget(target);
            // Hauls the hammer up from the carry to the cocked pose over the first half of the tell, then holds it.
            float raise = MathHelper.Clamp((elapsed + 1f) / (RushTellTicks * 0.5f), 0f, 1f);
            _setPieceWeaponRotation = MathHelper.SmoothStep(HammerHoldPose, HammerChargeCarryPose, raise);

            bool stampTick = elapsed == RushStampTickA || elapsed == RushStampTickB;
            if (stampTick)
            {
                EmitHoofStamp();
            }

            if (!Main.dedServ)
            {
                float progress = (elapsed + 1f) / RushTellTicks;
                Lighting.AddLight(HelmPosition, new Vector3(1f, 0.45f, 0.1f) * (0.4f + progress));
            }

            if (lastTick)
            {
                LockRushLine(target);
                EnterStage(TaurusStage.RushCharge, RushMaxChargeTicks);
            }
        }

        /// <summary>Locks the charge's direction and its stop point 180px past the target's current x. Runs on every peer
        /// (timer-driven); the server's values overwrite the prediction in the stage-change snapshot.</summary>
        private void LockRushLine(Player target)
        {
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            if (direction == 0)
            {
                direction = NPC.direction;
            }

            _rushDirection = direction;
            _rushStopX = target.Center.X + direction * RushOvershootPixels;
            _rushConnected = false;
            _rushSwingTicks = -1;
            _rushCount++;
            FaceDirection(direction);
        }

        /// <summary>Whether the target is in front, in the charge's row, and close enough that the swing's blade reaches
        /// them by the strike's peak: blade reach plus the ground covered during the ease-in at the current speed.</summary>
        private bool IsTargetInRushStrikeRange(Player target, float speed)
        {
            float horizontalGap = (target.Center.X - NPC.Center.X) * _rushDirection;
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            float bladeReach = ComboReachBase * 0.7f * RushStrikeReachMult;
            float strikeRange = bladeReach + speed * RushStrikeCurve.EaseInTicks + TargetHalfWidth;
            return horizontalGap > 0f && horizontalGap <= strikeRange && verticalGap <= RushStrikeMaxVerticalGap;
        }

        /// <summary>Advances the in-range hammer swing (charge and skid both call this; the swing can span the stage
        /// change). Server arms the tracked blade on the first tick and disarms it once the live window passes.</summary>
        private void TickRushSwing()
        {
            if (_rushSwingTicks == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    TryMeleeHit(ComboReachBase * 0.7f * RushStrikeReachMult);
                }

                PlayMeleeSwingSound();
            }

            bool live = _rushSwingTicks <= RushStrikeCurve.LiveTicks;
            if (live)
            {
                TickBladeHit();
            }
            else
            {
                StopMeleeHit();
            }

            _setPieceWeaponRotation = RushStrikeCurve.Apply(HammerChargeCarryPose, HammerFloorStrikePose, _rushSwingTicks);
            _rushSwingTicks++;
        }

        private bool RushSwingActive => _rushSwingTicks >= 0;
        private bool RushSwingFinished => _rushSwingTicks >= RushStrikeCurve.TotalTicks;

        private float RushSpeedAt(int elapsed)
        {
            float ramp = MathHelper.Clamp((elapsed + 1f) / RushAccelerationTicks, 0f, 1f);
            return RushTopSpeed * ramp * ramp;
        }

        private void TickRushCharge(Player target, int elapsed, bool lastTick)
        {
            FaceDirection(_rushDirection);
            float speed = RushSpeedAt(elapsed);
            NPC.velocity.X = _rushDirection * speed;

            // The hammer stays cocked until the target is in reach, then comes down without breaking stride. Every peer
            // runs the trigger on its own positions; the server's start is flushed in a snapshot and wins.
            bool startSwing = !RushSwingActive && IsTargetInRushStrikeRange(target, speed);
            if (startSwing)
            {
                _rushSwingTicks = 0;
                RequestNetworkSnapshot();
            }

            UpdateRushHammer();
            // Vanilla fighter step-up, so a one-block bump in the floor doesn't read as a wall.
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
            AfterimageTicks = 3;

            if (!Main.dedServ && elapsed % 2 == 0)
            {
                Dust kick = Dust.NewDustPerfect(NPC.Bottom + new Vector2(-_rushDirection * 8f, -2f), DustID.Sand,
                    new Vector2(-_rushDirection * Main.rand.NextFloat(1f, 3f), -Main.rand.NextFloat(0.5f, 2f)), 90,
                    new Color(214, 170, 110), Main.rand.NextFloat(1f, 1.5f));
                kick.noGravity = false;
            }

            // Ending the charge is the server's decision (it reads the server-only contact result); a client keeps
            // charging until the stage-change snapshot arrives, or the 70t cap ends it locally.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (lastTick)
                {
                    EnterRushSkid();
                }
                return;
            }

            // A swing in progress overrides the stop point: he keeps running until the hammer has come down.
            bool pastStopPoint = _rushDirection * (NPC.Center.X - _rushStopX) >= 0f;
            bool stopPointEnds = pastStopPoint && !RushSwingActive;
            bool swingDone = RushSwingActive && RushSwingFinished;
            bool blocked = IsRushBlocked(speed);
            bool atLedge = IsRushAtLedge(speed);
            if (_rushConnected || stopPointEnds || swingDone || blocked || atLedge || lastTick)
            {
                EnterRushSkid();
            }
        }

        /// <summary>Hammer pose for the charge and the skid: cocked until the swing starts, the swing while it plays, then
        /// held on the floor pose it ended on.</summary>
        private void UpdateRushHammer()
        {
            if (!RushSwingActive)
            {
                _setPieceWeaponRotation = HammerChargeCarryPose;
            }
            else if (!RushSwingFinished)
            {
                TickRushSwing();
            }
            else
            {
                StopMeleeHit();
                _setPieceWeaponRotation = HammerFloorStrikePose;
            }

            _rushEndPose = _setPieceWeaponRotation;
        }

        protected override void OnBladeHit(Player player)
        {
            // Server: the rush's hammer swing connected, so the run ends into the short skid.
            bool rushStage = _stage == TaurusStage.RushCharge || _stage == TaurusStage.RushSkid;
            if (rushStage)
            {
                _rushConnected = true;
            }
        }

        private bool IsRushBlocked(float speed)
        {
            // A wall two tiles or taller directly ahead (the lowest 20px are left to StepUp).
            Vector2 probe = NPC.position + new Vector2(_rushDirection * (speed + 4f), 0f);
            return Collision.SolidCollision(probe, NPC.width, NPC.height - 20);
        }

        private bool IsRushAtLedge(float speed)
        {
            // No footing within 3 tiles below the leading foot: stop rather than charge off a cliff.
            float frontX = NPC.Center.X + _rushDirection * (NPC.width * 0.5f + speed + 8f);
            Vector2 probe = new Vector2(frontX - 8f, NPC.Bottom.Y);
            return !Collision.SolidCollision(probe, 16, 48, true);
        }

        private void EnterRushSkid()
        {
            _rushWhiffed = !_rushConnected;
            int skidTicks = RushHitSkidTicks;
            if (_rushWhiffed)
            {
                skidTicks = RushWhiffSkidTicks;
            }

            EnterStage(TaurusStage.RushSkid, skidTicks);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.4f, Pitch = -0.6f }, NPC.Bottom);
        }

        private void TickRushSkid(bool lastTick)
        {
            FaceDirection(_rushDirection);
            UpdateRushHammer();
            // 0.885/t takes top speed to ~0.5 px/t over the 24t whiff skid.
            NPC.velocity.X *= 0.885f;

            if (!Main.dedServ && Math.Abs(NPC.velocity.X) > 1f)
            {
                Dust skid = Dust.NewDustPerfect(NPC.Bottom + new Vector2(_rushDirection * 6f, -2f), DustID.Sand,
                    new Vector2(_rushDirection * Main.rand.NextFloat(0.5f, 2f), -Main.rand.NextFloat(0.5f, 1.5f)), 90,
                    new Color(214, 170, 110), Main.rand.NextFloat(1f, 1.6f));
                skid.noGravity = false;
            }

            if (!lastTick)
            {
                return;
            }

            bool doubleRush = _phaseTwo && _rushWhiffed && _rushCount < MaxRushesPhaseTwo;
            int turnaroundTicks = (int)(RushTurnaroundTicks * RecoveryScale);
            if (doubleRush)
            {
                // The second rush's tell is this full-length turnaround (not trimmed: it is a tell, not a recovery).
                turnaroundTicks = RushTurnaroundTicks;
            }

            EnterStage(TaurusStage.RushTurnaround, turnaroundTicks);
        }

        private void TickRushTurnaround(Player target, int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.8f;
            StopMeleeHit();

            // The hammer comes back up from wherever the rush left it: to the cocked pose if a second rush follows,
            // otherwise to the ordinary carry.
            bool repeating = _phaseTwo && _rushWhiffed && _rushCount < MaxRushesPhaseTwo;
            float restPose = HammerHoldPose;
            if (repeating)
            {
                restPose = HammerChargeCarryPose;
            }

            float settle = (elapsed + 1f) / _stageTicks;
            _setPieceWeaponRotation = MathHelper.SmoothStep(_rushEndPose, restPose, settle);

            // Turns back to the target halfway through: the back half of the turnaround is the readable re-face.
            if (elapsed >= _stageTicks / 2)
            {
                FaceTarget(target);
            }
            else
            {
                FaceDirection(_rushDirection);
            }

            if (repeating)
            {
                bool stampTick = elapsed == RushStampTickA || elapsed == RushStampTickB;
                if (stampTick)
                {
                    EmitHoofStamp();
                }
            }

            if (!lastTick)
            {
                return;
            }

            if (repeating)
            {
                LockRushLine(target);
                EnterStage(TaurusStage.RushCharge, RushMaxChargeTicks);
                return;
            }

            EndSetPiece();
        }

        private void EmitHoofStamp()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.8f, Pitch = 0.2f }, NPC.Bottom);
            for (int i = 0; i < 12; i++)
            {
                Dust puff = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -2f), DustID.Sand,
                    new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), -Main.rand.NextFloat(1f, 2.5f)), 90,
                    new Color(214, 170, 110), Main.rand.NextFloat(1f, 1.5f));
                puff.noGravity = false;
            }
        }

        /// <summary>Contact damage exists only while the rush is fast: set every tick on every peer from the synced stage
        /// (contact is resolved on the touched player's own machine).</summary>
        private void UpdateBullRushContactDamage()
        {
            bool rushStage = Phase == AttackPhase.Custom
                && (_stage == TaurusStage.RushCharge || _stage == TaurusStage.RushSkid);
            bool fastEnough = Math.Abs(NPC.velocity.X) >= RushTopSpeed * RushContactSpeedFraction;
            if (rushStage && fastEnough)
            {
                // Matches a 1.3x blade hit: hostile hitboxes are doubled by the engine, NPC contact is not.
                NPC.damage = (int)(MeleeDamage * RushDamageMult * 2f);
                return;
            }

            NPC.damage = 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            bool rushStage = _stage == TaurusStage.RushCharge || _stage == TaurusStage.RushSkid;
            if (!rushStage)
            {
                return;
            }

            // Runs on the hit player's machine: heavy knockback along the charge line (tiles still stop it), then tell the
            // server so it ends the charge early (OnAttackHitReported).
            target.velocity = new Vector2(_rushDirection * RushKnockback, -4f);
            ReportAttackHit();
        }

        protected override void OnAttackHitReported()
        {
            if (_stage == TaurusStage.RushCharge)
            {
                _rushConnected = true;
            }
        }

        // ── Trident Javelin ──

        private Vector2 JavelinTipPosition(Vector2 velocity)
        {
            return PuppetHandPosition + velocity.SafeNormalize(new Vector2(NPC.direction, 0f)) * JavelinGripToTipPixels;
        }

        /// <summary>Launch velocity for a lob landing 60px past the target's feet, from <paramref name="source"/>.</summary>
        private Vector2 SolveJavelinVelocity(Player target, Vector2 source)
        {
            int side = Math.Sign(target.Center.X - source.X);
            if (side == 0)
            {
                side = NPC.direction;
            }

            Vector2 landingPoint = new Vector2(target.Center.X + side * JavelinOvershootPixels, target.Bottom.Y - 8f);
            return UsefulFunctions.BallisticTrajectory(source, landingPoint, JavelinSpeed, TaurusJavelin.Gravity,
                highAngle: false, fallback: true);
        }

        private Vector2 SolveJavelinFromTip(Player target)
        {
            // The tip depends on the aim and the aim on the tip: one refinement pass from the hand is enough at 60px.
            Vector2 fromHand = SolveJavelinVelocity(target, PuppetHandPosition);
            Vector2 tip = JavelinTipPosition(fromHand);
            return SolveJavelinVelocity(target, tip);
        }

        private void TickJavelinAim(Player target, int elapsed, bool lastTick)
        {
            FaceTarget(target);
            Vector2 launchVelocity = SolveJavelinFromTip(target);

            // Facing-space angle of the throw (0 = straight ahead, negative = up); +45° converts facing space to swing
            // space. Three beats: raise the trident above the head, hold it there, throw forward onto the release angle.
            // A short re-aim (phase-2 volley) compresses the raise and throw into its 14t.
            float facingAngle = (float)Math.Atan2(launchVelocity.Y, launchVelocity.X * NPC.direction);
            int raiseTicks = Math.Min(JavelinRaiseTicks, _stageTicks / 3);
            int throwTicks = Math.Min(JavelinThrowSwingTicks, _stageTicks / 2);
            int throwStart = _stageTicks - throwTicks;
            float aimPose = JavelinRaisedFacingAngle;
            if (elapsed < raiseTicks)
            {
                float raise = (elapsed + 1f) / raiseTicks;
                aimPose = MathHelper.SmoothStep(facingAngle, JavelinRaisedFacingAngle, raise);
            }
            else if (elapsed >= throwStart)
            {
                float throwProgress = (elapsed - throwStart + 1f) / throwTicks;
                aimPose = MathHelper.SmoothStep(JavelinRaisedFacingAngle, facingAngle, throwProgress);
            }

            _setPieceWeaponRotation = aimPose + MathHelper.PiOver4;

            // The release tip (along the throw) seeds the aim arc and the spawn; the held tip (along the raised trident)
            // carries the earthy dust, so the dust stays on the prop during the hold.
            Vector2 tip = JavelinTipPosition(launchVelocity);
            Vector2 heldDirection = new Vector2((float)Math.Cos(aimPose) * NPC.direction, (float)Math.Sin(aimPose));
            Vector2 heldTip = PuppetHandPosition + heldDirection * JavelinGripToTipPixels;
            if (!Main.dedServ)
            {
                EmitJavelinTell(heldTip, tip, launchVelocity, elapsed);
            }

            if (!lastTick)
            {
                return;
            }

            ReleaseJavelin(tip, launchVelocity);
            _javelinThrowsDone++;
            if (_javelinThrowsDone < _javelinThrowsTotal)
            {
                EnterStage(TaurusStage.JavelinAim, JavelinReaimTicks);
                return;
            }

            EnterStage(TaurusStage.JavelinRecovery, (int)(JavelinRecoveryTicks * RecoveryScale));
        }

        private void EmitJavelinTell(Vector2 heldTip, Vector2 tip, Vector2 launchVelocity, int elapsed)
        {
            if (elapsed % 2 == 0)
            {
                Dust earth = Dust.NewDustPerfect(heldTip + Main.rand.NextVector2Circular(3f, 3f), DustID.Dirt,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 70, new Color(150, 110, 70), Main.rand.NextFloat(0.9f, 1.2f));
                earth.noGravity = true;
            }

            // The aim line: a dotted arc along the solved trajectory for the last 18 ticks of the first aim (and the
            // whole of each shorter re-aim), stopping where it would meet a tile.
            bool showArc = _stageTicks - elapsed <= JavelinAimArcTicks;
            if (!showArc || elapsed % 3 != 0)
            {
                return;
            }

            Vector2 position = tip;
            Vector2 velocity = launchVelocity;
            for (int step = 1; step <= 60; step++)
            {
                velocity.Y += TaurusJavelin.Gravity;
                position += velocity;
                if (Collision.SolidCollision(position, 2, 2))
                {
                    break;
                }

                if (step % 5 == 0)
                {
                    Dust dot = Dust.NewDustPerfect(position, DustID.Sand, Vector2.Zero, 120, new Color(214, 170, 110), 0.8f);
                    dot.noGravity = true;
                    dot.fadeIn = 0.4f;
                }
            }
        }

        private void ReleaseJavelin(Vector2 tip, Vector2 launchVelocity)
        {
            PlayThrowSound();
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, launchVelocity,
                ModContent.ProjectileType<TaurusJavelin>(), TridentDamage, 3f, Main.myPlayer);
        }

        private void TickJavelinRecovery(int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.85f;
            // The re-gripped hammer settles from a raised pose back to the carry.
            float settle = (elapsed + 1f) / _stageTicks;
            _setPieceWeaponRotation = MathHelper.SmoothStep(-0.9f, -0.30f, settle);

            if (lastTick)
            {
                EndSetPiece();
            }
        }

        // ── Bull Breath ──

        private Vector2 BreathMouth => HelmPosition + new Vector2(NPC.direction * BreathMouthForward, 0f);

        private void TickBreathTell(Player target, int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.8f;
            FaceTarget(target);
            _setPieceWeaponRotation = HammerPlantedPose;

            if (!Main.dedServ)
            {
                float progress = (elapsed + 1f) / BreathTellTicks;
                if (elapsed % 2 == 0)
                {
                    // The legacy shrinking torch ring, now centred on the helm and far lighter (it spawned 48 dust a tick).
                    float radius = MathHelper.Lerp(48f, 6f, progress);
                    UsefulFunctions.DustRing(HelmPosition, radius, DustID.Torch, 8, 1.5f);
                }

                Lighting.AddLight(HelmPosition, new Vector3(1f, 0.5f, 0.1f) * (1f + 3f * progress));
            }

            if (lastTick)
            {
                // The stream starts on the player; the server's angle overwrites the prediction in the snapshot.
                Vector2 toTarget = target.Center - BreathMouth;
                _breathAngle = toTarget.ToRotation();
                EnterStage(TaurusStage.Breathing, BreathTicks);
            }
        }

        private void TickBreathing(Player target, int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.7f;
            _setPieceWeaponRotation = HammerPlantedPose;

            // Turn toward the player at a capped rate, and never past 80° off the facing horizontal.
            float desired = (target.Center - BreathMouth).ToRotation();
            float turn = MathHelper.Clamp(MathHelper.WrapAngle(desired - _breathAngle), -BreathTurnRadiansPerTick, BreathTurnRadiansPerTick);
            _breathAngle = MathHelper.WrapAngle(_breathAngle + turn);
            float facingAngle = 0f;
            if (NPC.direction < 0)
            {
                facingAngle = MathHelper.Pi;
            }

            float offFacing = MathHelper.Clamp(MathHelper.WrapAngle(_breathAngle - facingAngle), -BreathMaxAngleFromFacing, BreathMaxAngleFromFacing);
            _breathAngle = facingAngle + offFacing;

            Lighting.AddLight(BreathMouth, new Vector3(1f, 0.5f, 0.1f) * 2f);

            if (elapsed % BreathInterval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float jitter = Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 velocity = (_breathAngle + jitter).ToRotationVector2() * BreathSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), BreathMouth, velocity,
                    ModContent.ProjectileType<FireBreath>(), BreathDamage, 0f, Main.myPlayer);
            }

            if (lastTick)
            {
                EnterStage(TaurusStage.BreathRecovery, (int)(BullBreathRecoveryTicks * RecoveryScale));
            }
        }

        private void TickPlantedRecovery(float plantedPose, bool lastTick)
        {
            NPC.velocity.X *= 0.8f;
            _setPieceWeaponRotation = plantedPose;

            if (lastTick)
            {
                EndSetPiece();
            }
        }

        // ── Brimstone Rain ──

        private void TickRainTell(Player target, int elapsed, bool lastTick)
        {
            NPC.velocity.X *= 0.7f;
            FaceTarget(target);

            if (elapsed < RainRaiseTicks)
            {
                _setPieceWeaponRotation = HammerSkywardPose;
                if (!Main.dedServ)
                {
                    EmitRainSpiral(elapsed);
                }
            }
            else
            {
                // Strike the floor over the last 10 ticks.
                float strike = (elapsed - RainRaiseTicks + 1f) / (RainTellTicks - RainRaiseTicks);
                _setPieceWeaponRotation = MathHelper.SmoothStep(HammerSkywardPose, HammerFloorStrikePose, strike);
            }

            if (!lastTick)
            {
                return;
            }

            BrimstoneStrike(target);
            EnterStage(TaurusStage.RainRecovery, (int)(RainRecoveryTicks * RecoveryScale));
        }

        private void EmitRainSpiral(int elapsed)
        {
            float headReach = ComboReachBase * 0.7f * 0.85f;
            Vector2 hammerHead = PuppetWeaponTipPosition(headReach);
            for (int i = 0; i < 2; i++)
            {
                // Crimson and soot spiralling up off the head (Red Knight Destined Death palette).
                float angle = elapsed * 0.4f + i * MathHelper.Pi;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * 10f, 0f);
                bool soot = (elapsed + i) % 4 == 0;
                int dustType = DustID.RedTorch;
                Color tint = TaurusBrimstoneFlame.CrimsonBody;
                if (soot)
                {
                    dustType = DustID.Shadowflame;
                    tint = TaurusBrimstoneFlame.BlackSoot;
                }

                Dust mote = Dust.NewDustPerfect(hammerHead + offset, dustType, new Vector2(-offset.X * 0.05f, -2.5f), 50,
                    tint, 1.3f);
                mote.noGravity = true;
            }

            Lighting.AddLight(hammerHead, new Vector3(0.84f, 0.09f, 0.16f));
        }

        private void BrimstoneStrike(Player target)
        {
            if (!Main.dedServ)
            {
                Vector2 head = PuppetWeaponTipPosition(ComboReachBase * 0.7f * 0.85f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.3f }, head);
                UsefulFunctions.ScreenShake(head, 3f, 10, distanceFalloff: 600f);
                for (int i = 0; i < 20; i++)
                {
                    Dust spark = Dust.NewDustPerfect(head, DustID.Torch,
                        new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(1f, 4f)), 40,
                        TaurusBrimstoneFlame.HotSpark, Main.rand.NextFloat(0.9f, 1.3f));
                    spark.noGravity = false;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // A sweep from one side so the volley is a pattern to run along or against, not a wall.
            int sweepFrom = Main.rand.NextBool() ? -1 : 1;
            float spacing = RainSpanPixels / (RainDropCount - 1);
            for (int i = 0; i < RainDropCount; i++)
            {
                float x = target.Center.X + sweepFrom * (RainSpanPixels * 0.5f - i * spacing);
                float y = FindRainDropY(x, target.Center.Y);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, y), Vector2.Zero,
                    ModContent.ProjectileType<TaurusBrimstoneFlame>(), BrimstoneDamage, 0f, Main.myPlayer,
                    i * RainDropIntervalTicks);
            }
        }

        /// <summary>450px above the target, or 2 tiles under the first ceiling between, so no flame starts inside rock.</summary>
        private static float FindRainDropY(float x, float targetY)
        {
            float y = targetY;
            float topY = targetY - RainHeightPixels;
            while (y > topY)
            {
                float nextY = y - 16f;
                if (Collision.SolidCollision(new Vector2(x - 8f, nextY - 16f), 16, 16))
                {
                    return Math.Min(targetY - 48f, nextY + 16f);
                }

                y = nextY;
            }

            return topY;
        }

        /// <summary>Poise for the Custom stages, written after the engine's own flags (it knows nothing of Custom): tells
        /// are staggerable, the charge/skid and the breath stream are hyper-armored, recoveries are open.</summary>
        private void UpdateSetPiecePoise()
        {
            if (Phase != AttackPhase.Custom)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool telegraph = _stage == TaurusStage.RushTell
                || _stage == TaurusStage.JavelinAim
                || _stage == TaurusStage.BreathTell
                || _stage == TaurusStage.RainTell;
            bool committed = _stage == TaurusStage.RushCharge
                || _stage == TaurusStage.RushSkid
                || _stage == TaurusStage.Breathing;

            // Rain's floor strike (its last 10 ticks) is the commit cue.
            int rainElapsed = _stageTicks - PhaseTimer;
            if (_stage == TaurusStage.RainTell && rainElapsed >= RainRaiseTicks)
            {
                telegraph = false;
                committed = true;
            }

            globalNPC.AttackCommitted = committed;
            globalNPC.AttackTelegraphing = telegraph && !committed;
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            _stage = TaurusStage.None;
            _telegraphHoldTicks = 0;
            _abortTelegraphRequested = false;
            npc.damage = 0;
        }

        #endregion
    }
}
