using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.Enemy.OolacileSorcerer;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    /// <summary>
    /// The Grand Occultist of Oolacile, rebuilt on the PuppetNPC system. It keeps the old boss's class name
    /// on purpose: the summon item (AbysmalStone), the boss-checklist slot, the downed flag, the encounter
    /// broadcast and the hjson dialogue all bind by type, so the swap needs no rewiring. The pre-rework
    /// implementation lives on as read-only reference in AbysmalOolacileSorcererOriginal.
    ///
    /// Two phases that ESCALATE — the same composed caster choosing to stop pulling punches, not losing
    /// control:
    ///   100%-50% — a pure caster. Kites at 15-25 tiles and casts five telegraphed dark spells from the
    ///     Grand Oolacile Staff, plus Strafing Flight as an aerial reposition. No melee weapon at all.
    ///   50%-0%  — draws a great axe-maul. The staff NEVER leaves the kit: melee combos and spells now
    ///     compete for every pick, and the Staff-Axe Weave casts from the free hand between its own swings.
    /// Set-pieces (Strafing Flight, Locust Swarm, Abyssal Rain, Winged Axe Rush, the 50% transition,
    /// Corruption Surge at 30%, and the 0-HP desperation) run on AttackPhase.Custom; every staff cast runs through the base MAGIC phase, so
    /// the boss animates and fires from the staff tip for free (enemy-redesign G0.7 / G4).
    /// </summary>
    [AutoloadBossHead]
    class AbysmalOolacileSorcerer : PuppetNPC
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss".
        // PLACEHOLDER sprite (copy of AbysmalOolacileSorcererOriginal_Head_Boss) — replace with bespoke art.
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/AbysmalOolacileSorcerer_Head_Boss";

        // The encounter registry already broadcasts this boss's arrival, so the invader banner would double up.
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override string InvaderTitle => "Grand Occultist of Oolacile";

        #region Tuning constants
        // ── Movement ───────────────────────────────────────────────────────────────
        private const float CasterTopSpeed = 2.0f;
        private const float CasterAcceleration = 0.09f;
        private const float ArmedTopSpeed = 3.2f;
        private const float ArmedAcceleration = 0.14f;
        // Phase 1 keeps a 15-25 tile band (backpedalling while facing the player). Phase 2 drops the band so
        // SmartFighter4 walks straight in.
        private const float KiteMinTiles = 15f;
        private const float KiteMaxTiles = 25f;

        // ── Staff geometry ─────────────────────────────────────────────────────────
        // GrandOolacileStaff.png is 60x64; the shaft runs butt (0,63) to gem (55,0), an 83.6px diagonal.
        // Grip sits 15% up from the butt, leaving 71.2px of shaft at scale 1 -> 60px at the 0.85 draw scale.
        private const float StaffDrawScale = 0.85f;
        private const float StaffTipReach = 60f;

        // ── Axe geometry ───────────────────────────────────────────────────────────
        // GrandOolacileAxe.png is 112x104, broadsword convention: haft butt (4,101) -> head (107,6), a 140px
        // diagonal. Held 25% along that haft, so at 0.7 draw scale the head reaches 79px past the hand and
        // 24px of butt trails behind it: a ~103px weapon against a 64px body, the same 1.6x a great weapon
        // reads at on Artorias. The first pass used 0.9 and rendered as a flagpole twice the boss's height.
        // ComboReachBase 113 * 0.7 * ReachMult 1.0 = 79px, so the hitbox IS the visible axe.
        private const float AxeDrawScale = 0.7f;
        private const float AxeComboReach = 113f;

        // Three hits from the Occultist within the 30-second buildup window will proc Madness even
        // when the player spaces them out. The buildup system decays by one point per second, so 45
        // per confirmed axe contact leaves a small margin without allowing two immediate hits to proc.
        private const int MeleeMadnessBuildup = 45;
        private const int MeleeMadnessWindowTicks = 30 * 60;
        private static readonly Color MadnessYellow = new Color(255, 220, 45);

        // ── Spell timings (all in ticks; on-screen values, no multiplier) ──────────
        private const int BoltVolleyTelegraph = 40;
        private const int BoltVolleyChannel = 45;
        private const int BoltVolleyInterval = 9;
        private const int BoltVolleyRecovery = 60;
        private const int BoltVolleyCooldown = 120;

        private const int DarkFanTelegraph = 50;
        private const int DarkFanRecovery = 50;
        private const int DarkFanCooldown = 240;

        private const int SeekerOrbTelegraph = 60;
        private const int SeekerOrbRecovery = 40;
        private const int SeekerOrbCooldown = 480;

        private const int StarCascadeTelegraph = 45;
        // Count and cadence both ramp with health, following the TEST boss's own Lerp(40, 25, 1 - lifeRatio)
        // shape. Above half health it is a readable three-ring cast; below, six on a tighter beat. The
        // channel is count x interval, so _magicAttackTicksOverride runs 120t at worst and 180t at best.
        private const int StarCascadeRingsHighHp = 3;
        private const int StarCascadeRingsLowHp = 6;
        private const int StarCascadeIntervalHighHp = 40;
        private const int StarCascadeIntervalLowHp = 25;
        private const int StarCascadeRecovery = 60;
        private const int StarCascadeCooldown = 420;

        // The kit's ONE interruptible channel: staggerable for its first 55 ticks, hyper-armored for the
        // last 25 (commit chime + flash marks the flip). Every other telegraph is committed from frame 1.
        private const int DarkBeadTelegraph = 80;
        private const int DarkBeadCommitTicks = 25;
        private const int DarkBeadChannel = 30;
        private const int DarkBeadInterval = 4;
        private const int DarkBeadRecovery = 90;
        private const int DarkBeadCooldown = 600;

        // ── Projectile speeds / spreads ────────────────────────────────────────────
        private const float BoltSpeed = 9.5f;
        private const float FanBoltSpeed = 11f;
        private const float FanSpreadDegrees = 18f;
        private const float DarkBeadSpeed = 10f;
        private const float DarkBeadSpreadDegrees = 13f;
        private const int DarkBeadCount = 7;
        private const float SeekerOrbSpeed = 3.5f;
        private const float StarSpeed = 9f;
        // Where the thrown rings park, as offsets from the player. Dealt in order so no two rings of one
        // cast ever share a spot; the list runs above, far left, far right, then wider and higher for the
        // extra rings the low-health cast adds.
        private static readonly Vector2[] StarSpotOffsets =
        {
            new Vector2(0f, -260f),
            new Vector2(-420f, -150f),
            new Vector2(420f, -150f),
            new Vector2(-270f, -350f),
            new Vector2(270f, -350f),
            new Vector2(0f, -470f),
        };
        private const int StarSpotClearanceSteps = 12;
        private const float StarSpotClearanceStep = 26f;
        private const int AimLeadTicks = 8;

        // ── Set-piece timings ──────────────────────────────────────────────────────
        private const int StrafeAscendTicks = 30;
        private const int StrafeRunTicks = 240;
        private const int StrafeLandTicks = 50;
        private const int StrafeBoltInterval = 30;
        private const float StrafeAltitude = 420f;
        private const float StrafeSweepHalfWidth = 380f;
        private const float StrafeFlySpeed = 7.5f;
        private const int StrafeCooldown = 360;

        private const int RainTelegraphTicks = 60;
        private const int RainBoltCount = 8;
        private const int RainBoltInterval = 26;
        // Each strike marks its ground spot for 20 ticks, then the bolt is born 500px up and falls at 10px/t
        // (50 more ticks of travel) — 70 ticks of total warning per bolt.
        private const int RainMarkerTicks = 20;
        private const float RainSpawnHeight = 500f;
        private const float RainBoltSpeed = 10f;
        private const float RainSpreadWidth = 520f;
        private const int RainLandTicks = 80;
        private const int RainCooldown = 480;

        // Locust Swarm: a 45t takeoff/read, then one locust every 3t while the Occultist crosses a
        // 15x4-tile cloud band over the player. The Locust's own 30t arrival fade is the per-creature
        // birth tell; the 60t landing tail is intentionally unarmored.
        private const int LocustSwarmTelegraphTicks = 45;
        private const int LocustSwarmCount = 50;
        private const int LocustSwarmSpawnInterval = 3;
        private const int LocustSwarmStreamTicks = LocustSwarmCount * LocustSwarmSpawnInterval;
        private const int LocustSwarmLandTicks = 60;
        private const int LocustSwarmCooldown = 20 * 60;
        private const float LocustCloudWidth = 15f * 16f;
        private const float LocustCloudHeight = 4f * 16f;
        private const float LocustCloudAltitude = 9f * 16f;
        private const float LocustSwarmFlySpeed = 7f;

        // Winged Axe Rush: 36t to clear the ground, a 40t fully locked line tell, an 18t harmless
        // omnidirectional rush, then a real 26t tracked axe arc. Only the first 18t of that arc are live;
        // the rest is visible follow-through before a full second of vulnerable landing recovery.
        private const int WingDashAscendTicks = 36;
        private const int WingDashTelegraphTicks = 40;
        private const int WingDashTravelTicks = 18;
        private const int WingDashSwingTicks = 26;
        private const int WingDashRecoveryTicks = 60;
        private const int WingDashTotalTicks = WingDashAscendTicks + WingDashTelegraphTicks
            + WingDashTravelTicks + WingDashSwingTicks + WingDashRecoveryTicks;
        private const int WingDashCooldown = 12 * 60;
        private const float WingDashStageHorizontal = 13f * 16f;
        private const float WingDashStageHeight = 8f * 16f;
        private const float WingDashOvershoot = 4f * 16f;
        private const float WingDashMaxDistance = 24f * 16f;
        private const float WingDashTopSpeed = 24f;
        private static readonly WeightedSwing WingDashSwingCurve = new WeightedSwing(13, 13, 4f);

        private const int TransitionTicks = 90;

        // 100 ticks of swell: 66 telegraphing, 34 committed. The ring's own expansion is the damage.
        private const int SurgeTelegraphTicks = 66;
        private const int SurgeCommitTicks = 34;
        private const int SurgeRecoveryTicks = 120;
        private const float SurgeRingRadius = 520f;

        // 900 ticks (15s): ascend 120, plunge 120, then 660 of escalating spiral fire.
        private const int DesperationAscendTicks = 120;
        private const int DesperationPlungeTicks = 120;
        private const int DesperationSpiralTicks = 660;
        private const int DesperationTotalTicks = DesperationAscendTicks + DesperationPlungeTicks + DesperationSpiralTicks;
        private const float DesperationAscendSpeed = 9f;
        private const float DesperationPlungeSpeed = 16f;
        private const float DesperationSpiralSpeed = 7f;

        // ── Phase 2 aggression ramp ────────────────────────────────────────────────
        // Below these HP fractions a finished attack has this chance to skip its recovery outright, so the
        // next pick starts immediately. Dark Bead Barrage and Corruption Surge are exempt (see SkipRecovery).
        private const float RecoverySkipHighHp = 0.50f;
        private const float RecoverySkipLowHp = 0.30f;
        private const int RecoverySkipHighChance = 33;
        private const int RecoverySkipLowChance = 50;

        // Beyond this the boss stops being fightable and starts punishing the runaway (TEST-boss behaviour,
        // new to the live fight): doubled projectile speed and a 5 HP/second bleed.
        private const float AbandonDistance = 3000f;
        private const int AbandonDrainInterval = 60;
        private const int AbandonDrainAmount = 5;

        private const int AbyssDebuffTicks = 30 * 60 * 60;

        // ── Combo names (also the debug HUD labels) ────────────────────────────────
        private const string AbyssalSlamName = "Abyssal Slam";
        private const string CorruptionFlurryName = "Corruption Flurry";
        private const string DarkJudgmentName = "Dark Judgment";
        private const string StaffAxeWeaveName = "Staff-Axe Weave";
        private const string DestinedAxeCastName = "Destined Axe Cast";
        private const string WingedAxeRushName = "Winged Axe Rush";

        // Shared arc endpoints for every axe swing, so chained steps start exactly where the last one ended
        // and no pause ever snaps. Envelope 3.80 rad = 217.7 deg (~174 deg of it live).
        private const float AxeHighPose = -1.55f;
        private const float AxeLowPose = 2.25f;
        // Dark Judgment alone swings wider: 3.97 rad = 227.4 deg envelope, ~182 deg live.
        private const float JudgmentHighPose = -1.62f;
        private const float JudgmentLowPose = 2.35f;
        #endregion

        private enum Spell : byte
        {
            BoltVolley,
            DarkFan,
            SeekerOrb,
            StarCascade,
            DarkBeadBarrage,
        }

        private enum SetPiece : byte
        {
            None,
            PhaseTransition,
            StrafingFlight,
            LocustSwarm,
            AbyssalRain,
            WingedAxeRush,
            CorruptionSurge,
            Desperation,
        }

        private enum RangeBand : byte
        {
            Close,
            Mid,
            Far,
        }

        private const int SpellCount = 5;

        // ── Synced state ───────────────────────────────────────────────────────────
        private Spell _spell;                       // the cast the server rolled for the current magic phase
        // Star Cascade's ring count and beat, LOCKED IN at cast start. Reading NPC.life live instead would
        // change the channel's length and firing schedule mid-cast if the boss crossed 50% while casting,
        // which skips or doubles rings.
        private byte _starCascadeRings = StarCascadeRingsHighHp;
        private byte _starCascadeInterval = StarCascadeIntervalHighHp;
        private SetPiece _setPiece = SetPiece.None; // which Custom-phase script is running
        private bool _phase2Triggered;              // the 50% transition has STARTED (one-shot guard)
        private bool _axeDrawn;                     // the transition finished: the axe and its combos are live
        private bool _surgeDone;                    // Corruption Surge has fired (one-shot at 30%)
        private bool _desperationStarted;
        private bool _desperationComplete;          // CheckDead may now let the boss actually die
        private readonly int[] _spellCooldowns = new int[SpellCount];
        private int _strafeCooldown;
        private int _locustSwarmCooldown;
        private int _rainCooldown;
        private int _wingDashCooldown;
        private bool _wingDashLocked;
        private Vector2 _wingDashAimPoint;
        private Vector2 _wingDashEndPoint;
        private Vector2 _wingDashDirection = Vector2.UnitX;

        // ── Local-only state ───────────────────────────────────────────────────────
        private int _attackLabelTimer;
        private int _abandonDrainTimer;
        private int _seekerOrbIndex = -1;           // the one live Seeker Orb, so only one exists at a time
        private int _strafeDir = 1;
        private int _locustPassDirection = 1;
        private int _locustGroupId;
        private bool _wingDashBladeArmed;
        private bool _wingDashLockCuePlayed;
        private float _desperationSpiralAngle;
        private float _auraBonus = 1f;
        private static Effect _auraEffect;
        private AttackPhase _lastTickPhase = AttackPhase.Idle;
        private bool _unskippableRecovery;
        // Server-only spell bag. Each spell appears as many times as its band/phase weight; a refill never
        // repeats the spell that just played (attack-quality-pass section 6).
        private readonly List<Spell> _spellBag = new List<Spell>();
        private Spell _lastSpell = Spell.DarkBeadBarrage;

        private NPCDespawnHandler _despawnHandler;

        #region Puppet loadout
        // Spectre Robe + Pants in brown-and-black, a Plantera Mask burning in Hades dye, Spooky Wings to
        // match the robe. All vanilla items with real equip slots — no wrapper ModItems needed.
        protected override int HeadArmorItemType => ItemID.PlanteraMask;
        protected override int BodyArmorItemType => ItemID.SpectreRobe;
        protected override int LegsArmorItemType => ItemID.SpectrePants;
        protected override int HeadArmorDyeItemType => ItemID.BurningHadesDye;
        protected override int BodyArmorDyeItemType => ItemID.BrownAndBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.BrownAndBlackDye;
        protected override int WingsDyeItemType => ItemID.BrownAndBlackDye;

        protected override Color PuppetSkinColor => new Color(38, 22, 26);
        protected override float PuppetDrawScale => 1.15f;

        // The staff is fixed across both phases. The axe only exists once the 50% transition finishes, which
        // is also what resolves MeleeArchetype from None to Hammer and unlocks the combo system.
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyOccultistStaff>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyDestinedDeathFlask>();
        protected override int MeleeWeaponItemType =>
            _axeDrawn ? ModContent.ItemType<EnemyOccultistAxe>() : -1;

        protected override int MeleeDamage => 52;
        protected override int RangedDamage => 25;
        protected override int MagicDamage => 56;

        // A boss never disengages to drink.
        protected override int EstusChargesMax => 0;
        protected override bool DespawnsOnPartyWipe => false;

        protected override float TopSpeed => _axeDrawn ? ArmedTopSpeed : CasterTopSpeed;
        protected override float Acceleration => _axeDrawn ? ArmedAcceleration : CasterAcceleration;

        // ── Wings: hidden while grounded, manifest for any flying set-piece ────────
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.SpookyWings;
        protected override bool ShowWingsWhenGrounded => false;
        // No autonomous flight at all — bespoke aerial set-pieces command takeoff.
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => 99999f;
        #endregion

        #region Destined Death Flask
        // Studded Leather Warrior's proven Throw phase supplies the raised-hand wind-up and forward
        // release. This attack gets one flask per cast and a true 20-second post-use cooldown.
        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 680f;
        protected override float MinRangedRange => 160f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedAttackTicks => 12;
        protected override int RangedRecoveryTicks => 50;
        protected override int RangedCooldownAfterUse => 20 * 60;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 100;
        protected override Color RangedTelegraphFlashColor => new Color(190, 20, 45);
        // Preserve the staff as the dominant ranged language whenever both options are ready. Once a
        // spell enters its short global cooldown, the flask remains eligible and gets its turn.
        protected override int MagicPreferenceChance => 75;
        #endregion

        #region Magic phase (every staff cast)
        protected override float MagicRange => 1600f;
        protected override float MinMagicRange => 0f;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => 0.25f;
        protected override float MagicCastEndRotation => -1.05f;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override int MagicWeaponRecoveryHoldTicks => 22;
        protected override Color MagicTelegraphFlashColor => new Color(190, 20, 45);
        protected override bool BrakeDuringMagicCast => false;
        // Grip 15% up the 60x64 staff's butt-to-gem diagonal: texel (8, 54).
        protected override Vector2 MagicGripNorm => new Vector2(0.14f, 0.84f);

        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == ModContent.ItemType<EnemyOccultistStaff>())
            {
                return StaffDrawScale;
            }
            if (itemType == ModContent.ItemType<EnemyDestinedDeathFlask>())
            {
                return 0.8f;
            }
            return base.GetHeldRangedDrawScale(itemType);
        }

        // Each spell owns its own wind-up, channel, recovery and cooldown. The server rolls the spell in
        // OnMagicTelegraphStarting, i.e. BEFORE the base reads MagicTelegraphTicks, and the roll rides to
        // clients in the same snapshot as the phase.
        protected override int MagicTelegraphTicks => _spell switch
        {
            Spell.BoltVolley => Tel(BoltVolleyTelegraph),
            Spell.DarkFan => Tel(DarkFanTelegraph),
            Spell.SeekerOrb => Tel(SeekerOrbTelegraph),
            Spell.StarCascade => Tel(StarCascadeTelegraph),
            Spell.DarkBeadBarrage => DarkBeadTelegraph,
            _ => 40,
        };

        protected override int MagicAttackTicks => 8;

        protected override int MagicRecoveryTicks => _spell switch
        {
            Spell.BoltVolley => BoltVolleyRecovery,
            Spell.DarkFan => DarkFanRecovery,
            Spell.SeekerOrb => SeekerOrbRecovery,
            Spell.StarCascade => StarCascadeRecovery,
            Spell.DarkBeadBarrage => DarkBeadRecovery,
            _ => 50,
        };

        // GLOBAL casting pace only — the "breathe" beat between two spells. Each spell's own long cooldown
        // lives in _spellCooldowns and gates that spell's bag entries; putting it here instead would lock
        // the boss out of ALL casting for ten seconds after one Dark Bead Barrage.
        protected override int MagicCooldownAfterUse => _axeDrawn ? 45 : 90;

        // Phase 2 casts faster but is punished just as hard: telegraphs scale to 70%, recoveries do not.
        // Dark Bead Barrage keeps its full 80-tick channel — it is the fight's one stagger window.
        private int Tel(int baseTicks)
        {
            if (!_axeDrawn)
            {
                return baseTicks;
            }
            return Math.Max(20, (int)(baseTicks * 0.7f));
        }

        // The flash fires this many ticks before the telegraph ends. Dark Bead's is the real commit cue —
        // the moment poise can no longer interrupt it.
        protected override int MagicTelegraphFlashLeadTicks =>
            _spell == Spell.DarkBeadBarrage ? DarkBeadCommitTicks : 14;

        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.MagicTelegraph
            || Phase == AttackPhase.MagicAttack
            || IsHoldingMagicWeaponDuringRecovery
            || Phase == AttackPhase.Custom;
        #endregion

        #region Axe combos (phase 2)
        protected override WeaponArchetype MeleeArchetype =>
            _axeDrawn ? WeaponArchetype.Hammer : WeaponArchetype.None;

        // Hammer, not Axe, on purpose: MeleeWeaponIsSingleBladed defaults on for the Axe archetype and
        // mirrors the sprite on upswings so the cutting edge leads. This head is double-ended (axe right,
        // hammer left), so neither side trails and the mirror would only risk an upside-down head.
        protected override bool MeleeWeaponIsSingleBladed => false;

        protected override Vector2 MeleeHandleNorm => new Vector2(0.266f, 0.743f);
        protected override float MeleeWeaponDrawScale => AxeDrawScale;
        protected override float MeleeBladeWidth => 52f;
        protected override float MeleeRange => 80f;
        protected override float StabRange => 200f;
        protected override float ComboReachBase => AxeComboReach;
        protected override float MeleeEngageRange => 100f;
        protected override float ComboMaxStartRange => 240f;
        // Only Destined Axe Cast is RangedStartOnly, so this extended band cannot make the close-range
        // swings start an implausible approach from across the arena.
        protected override float RangedStartComboMaxRange => 720f;

        // Authored tells are on-screen tells: no 1.35x multiplier, and a floor low enough that the shortest
        // one (32t) is not stretched.
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 22;
        // Half the wind-up is spent easing the axe up into the cocked pose. At the 0.25 default the raise
        // covered its 71 degrees in 8 ticks and PEAKED FASTER THAN THE STRIKE (26.8 vs 25.0 deg/tick), which
        // kills the contrast the whole move depends on; at 0.5 the raise tops out around 13 deg/tick.
        protected override float LogicalWindupSettleFraction => 0.5f;
        // Hold the finished pose for 12 ticks before the blade eases back to the carry. Without it a combo
        // that ends low (the overhead finishers) starts its recovery with a 15-degree jerk back up, and the
        // follow-through reads as cut off rather than as weight settling.
        protected override int MeleeRecoveryLingerTicks => 12;
        // 12 ticks of pure hold at the top of every inter-step pause. Endpoints are shared, so the remaining
        // drift is a no-op and no step ever starts with a snap.
        protected override int MeleeComboInterStepLingerTicks => 12;

        // Inside engage range roughly two picks in three swing; beyond it only about one in three commits to
        // closing, so mid-range stays a caster's band even in phase 2.
        protected override int MeleeComboChance => 62;
        protected override int RangedStartMeleeComboChance => 35;

        protected override bool SlowDownBeforeMelee => false;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        // Heavy tells and strikes are hyper-armored; recoveries stay staggerable as the punish windows.
        protected override bool HyperArmorDuringTelegraph => true;

        // The axe's arc is deliberately shown in the same yellow as its Madness buildup. The shared
        // slash renderer follows the real hand-to-tip sweep, so this stays honest to the hitbox.
        protected override bool HasSlashVFX => _axeDrawn;
        protected override Color SlashVFXColor => MadnessYellow;
        protected override float SlashVFXOpacity => 0.48f;
        protected override float SlashVFXScale => 1.05f;

        /// <summary>
        /// One axe swing. Armed while angular speed is at or above 30% of peak, i.e. for
        /// easeIn + easeOut*ln(1/0.3)/decay ticks of the easeIn+easeOut total.
        /// </summary>
        private static MeleeComboStep AxeStep(
            ComboMotion motion,
            int telegraphTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            int pauseAfter = 0,
            float damageMult = 1f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float liveTailTicks = easeOutTicks * MathF.Log(1f / 0.3f) / easeOutDecay;
            float hitWindowEnd = MathHelper.Clamp((easeInTicks + liveTailTicks) / attackTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                Hand = ComboHand.Front,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = 1f,
                ForwardPushMult = 0f, // no push: with no melee brake the navigator keeps walking in through the swing
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = easeOutDecay,
                HitWindowEnd = hitWindowEnd,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        // Timing sheet (60 ticks = 1s). Player budget: 22t of roll i-frames, the next roll 30t after the last
        // one STARTED, 40t of post-hit immunity. Peak/live/sweep columns are MEASURED by SwingPreview
        // --profile, not solved: every arc sweeps 218 deg (Judgment 227), and no strike is slower than its
        // own wind-up (tells peak 14-20 deg/t against strikes at 25-30).
        //
        // Combo / step        Tell  Strike(in/out,k)  Peak     Live   Tail  Pause  Next live vs prev live end
        // Abyssal Slam  #1     32    11 / 30, k7      25.0/t   18t    23t    12    -
        //               #2      -     9 / 28, k7      27.8/t   15t    22t     -    +34t   (>= 30, unconditional OK)
        //   recovery 34 -> 56t punish window. Both hits land 53t apart, clear of the 40t immunity.
        // Corruption    #1     36    10 / 26, k7      27.9/t   16t    20t    14    -
        //   Flurry      #2      -     9 / 24, k7      30.3/t   15t    18t    14    +34t
        //               #3      -     9 / 24, k7      30.3/t   15t    18t     -    +32t
        //   recovery 42 -> 60t punish. Three separately rollable hits, each 47-50t apart.
        // Dark Judgment #1     46    14 / 34, k6      20.5/t   22t    26t     -    -
        //   recovery 48 -> 74t punish. 22t live exactly fills a roll's i-frames, so only a well-timed roll
        //   clears it — the heaviest swing in the kit is also the tightest to dodge, and it never opens
        //   inside 60px (CanSelectMeleeCombo) where its 46t tell would be hidden behind the boss's own body.
        // Staff-Axe     #1     34    10 / 26, k7      27.9/t   16t    20t    34    -
        //   Weave       #2      -     9 / 24, k7      30.3/t   15t    18t    34    +54t
        //               #3      -    10 / 26, k7      27.9/t   16t    20t     -    +52t
        //   Each 34t pause casts a pair of bolts from the FREE hand: 14t of gathering dust at the hand, then
        //   release at 5.5px/t, so from melee range the bolt is ~20t of travel — 34t of total warning.
        //   recovery 40 -> 60t punish.
        private static readonly MeleeCombo[] AxeCombos =
        {
            new MeleeCombo
            {
                Name = AbyssalSlamName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(190, 40, 60),
                CooldownAfterUse = 120,
                RecoveryTicks = 34,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    AxeStep(ComboMotion.OverheadArc, 32, 11, 30, 7f, pauseAfter: 12),
                    AxeStep(ComboMotion.UnderhandArc, 0, 9, 28, 7f, damageMult: 0.9f),
                },
            },
            new MeleeCombo
            {
                Name = CorruptionFlurryName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(220, 60, 80),
                CooldownAfterUse = 200,
                RecoveryTicks = 42,
                MoveBrake = 0.2f,
                Steps = new[]
                {
                    AxeStep(ComboMotion.OverheadArc, 36, 10, 26, 7f, pauseAfter: 14, damageMult: 0.85f),
                    AxeStep(ComboMotion.UnderhandArc, 0, 9, 24, 7f, pauseAfter: 14, damageMult: 0.85f),
                    AxeStep(ComboMotion.OverheadArc, 0, 9, 24, 7f, damageMult: 1f),
                },
            },
            new MeleeCombo
            {
                Name = DarkJudgmentName,
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(120, 0, 20),
                CooldownAfterUse = 420,
                RecoveryTicks = 48,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.4f,
                Steps = new[]
                {
                    AxeStep(ComboMotion.GroundSlam, 46, 14, 34, 6f, damageMult: 1.9f),
                },
            },
            new MeleeCombo
            {
                Name = StaffAxeWeaveName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(150, 30, 150),
                CooldownAfterUse = 180,
                RecoveryTicks = 40,
                MoveBrake = 0.2f,
                Steps = new[]
                {
                    AxeStep(ComboMotion.OverheadArc, 34, 10, 26, 7f, pauseAfter: 34, damageMult: 0.85f),
                    AxeStep(ComboMotion.UnderhandArc, 0, 9, 24, 7f, pauseAfter: 34, damageMult: 0.85f),
                    AxeStep(ComboMotion.OverheadArc, 0, 10, 26, 7f, damageMult: 0.85f),
                },
            },
            new MeleeCombo
            {
                Name = DestinedAxeCastName,
                BaseWeight = 42,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = new Color(150, 10, 35),
                CooldownAfterUse = 10 * 60,
                RecoveryTicks = 36,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    // One coherent step owns release, embed, the readable pause, and the retrieval leap.
                    // There is deliberately no bonus pickup swing: reclaiming the axe ends in a punishable recovery.
                    AxeStep(ComboMotion.ThrownWeaponRetrieve, 46, 0, 240, 7f, damageMult: 0f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => AxeCombos;

        protected override int ThrownComboWeaponProjectileType =>
            ModContent.ProjectileType<OccultistThrownAxe>();

        protected override int ThrownWeaponReadTicks => 16;

        protected override int SpawnThrownComboWeapon()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return -1;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = Main.dedServ
                ? NPC.Center + new Vector2(NPC.direction * 14f, -NPC.height * 0.22f)
                : PuppetHandPosition;
            const float throwSpeed = 12.5f;
            float leadTicks = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / throwSpeed, 8f, 36f);
            Vector2 aimAt = target.Center + target.velocity * leadTicks;
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(
                origin, aimAt, throwSpeed, 0.18f, highAngle: false, fallback: true);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = -0.3f }, origin);
            }

            return Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                velocity,
                ThrownComboWeaponProjectileType,
                (int)(MeleeDamage * 0.9f),
                6f,
                Main.myPlayer,
                0f,
                NPC.whoAmI);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            bool comboPoseActive = Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery;
            if (!comboPoseActive)
            {
                return;
            }

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = AxeHighPose;
                endRotation = AxeLowPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = AxeLowPose;
                endRotation = AxeHighPose;
            }
            else if (motion == ComboMotion.GroundSlam)
            {
                startRotation = JudgmentHighPose;
                endRotation = JudgmentLowPose;
            }
        }
        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.TrailCacheLength[NPC.type] = 8;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.NeedsExpertScaling[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = 30;
            NPC.height = 44;
            NPC.damage = 0; // puppets deal damage through weapon hitboxes and projectiles, never contact
            NPC.knockBackResist = 0.15f; // the PoiseProfiles entry is the real authority
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.npcSlots = 10f; // matches the original: nothing else spawns during the fight
            NPC.timeLeft = 22500;

            // Tier neighbours (EnemyStatInventory): Witchking 170k/65def/350k value, Artorias 250k/75/750k.
            NPC.lifeMax = 210000;
            NPC.defense = 65;
            NPC.value = 430000;
            NPC.rarity = 38;

            _despawnHandler = new NPCDespawnHandler(
                LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.DespawnHandler"), Color.DarkRed, DustID.Firework_Red);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            globalNPC.NavSearchRadius = 70;
            globalNPC.MaxJumpPower = 9.5f;
            globalNPC.Agility = 0.3f;
        }

        // Oolacile was the town the Abyss consumed — its occultist curses everyone present the moment the
        // fight begins, for the whole fight and then some.
        public override void OnSpawn(IEntitySource source)
        {
            NPC.netUpdate = true;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Abyss>(), AbyssDebuffTicks);
                }
            }
        }

        public override bool CheckActive() => false;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RemembersLastKnownPos = true;

            if (_axeDrawn)
            {
                // Phase 2 walks straight in; the axe is the point.
                globalNPC.KiteRangeMin = 0f;
                globalNPC.KiteRangeMax = 0f;
                globalNPC.CanWalkBackwards = false;
                SmartFighter4AI.Run(NPC,
                    topSpeed: TopSpeed * speedMult,
                    acceleration: Acceleration,
                    doorBreakingDamage: 6,
                    attackRange: MeleeEngageRange);
                return;
            }

            // Phase 1 holds 15-25 tiles, backpedalling while facing the player. KiteLooseness lets it
            // occasionally fail to retreat, so a committed player can still corner it.
            globalNPC.KiteRangeMin = KiteMinTiles;
            globalNPC.KiteRangeMax = KiteMaxTiles;
            globalNPC.KiteLooseness = 0.15f;
            globalNPC.CanWalkBackwards = true;
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 6,
                attackRange: MagicRange);
        }

        public override void AI()
        {
            _despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active)
            {
                return;
            }

            if (_despawnHandler.IsDespawning)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity *= 0.85f;
                // This returns before base.AI(), so DoCustomTick never gets to end an aerial set-piece.
                // Restore what one would have restored, or a despawning boss hangs in the air.
                _setPiece = SetPiece.None;
                NPC.noGravity = false;
                return;
            }

            TickCooldowns();
            TryStartPhaseTransition();
            TryStartCorruptionSurge();

            base.AI();
            TickWingDashBladeAfterPose();

            // The fallback one-shot melee phase is rarely selected once the axe combo pool is active,
            // but it still gets the same blade-attached tell if it is chosen by a future selector.
            if (!Main.dedServ && _axeDrawn)
            {
                if (Phase == AttackPhase.MeleeTelegraph)
                {
                    EmitMadnessAxeVFX(0.55f, live: false);
                }
                else if (Phase == AttackPhase.MeleeAttack)
                {
                    EmitMadnessAxeVFX(1f, live: true);
                }
            }

            TryStartWingedAxeRush();
            TryStartLocustSwarm();
            TryStartStrafingFlight();
            TryStartAbyssalRain();
            TrySkipRecovery();
            ApplyAttackArmor();
            PunishAbandonment();
            SpawnAmbientEffects();
        }

        private void TickCooldowns()
        {
            if (_attackLabelTimer > 0 && --_attackLabelTimer == 0)
            {
                DebugAttackLabel = null;
            }

            for (int i = 0; i < SpellCount; i++)
            {
                if (_spellCooldowns[i] > 0)
                {
                    _spellCooldowns[i]--;
                }
            }
            if (_strafeCooldown > 0)
            {
                _strafeCooldown--;
            }
            if (_locustSwarmCooldown > 0)
            {
                _locustSwarmCooldown--;
            }
            if (_rainCooldown > 0)
            {
                _rainCooldown--;
            }
            if (_wingDashCooldown > 0)
            {
                _wingDashCooldown--;
            }

            // Only one Seeker Orb may be alive; clear the slot the moment the old one dies.
            bool orbGone = _seekerOrbIndex < 0
                || _seekerOrbIndex >= Main.maxProjectiles
                || !Main.projectile[_seekerOrbIndex].active
                || Main.projectile[_seekerOrbIndex].type != ModContent.ProjectileType<OolacileDarkOrb>();
            if (orbGone)
            {
                _seekerOrbIndex = -1;
            }
        }

        // ── Phase 2 aggression ramp ────────────────────────────────────────────────
        // As HP drops, a finished attack's recovery has a chance to be cut to nothing, so whatever the next
        // pick rolls simply starts at once. No follow-up map and no combo bookkeeping: deleting this one
        // method and its call fully reverts the feature.
        // Dark Bead Barrage (90t) and Corruption Surge (120t) are exempt — they exist to guarantee one long,
        // certain opening, and making those skippable risks a run of attacks with no punish window at all.
        private void TrySkipRecovery()
        {
            AttackPhase previousPhase = _lastTickPhase;
            _lastTickPhase = Phase;

            if (Main.netMode == NetmodeID.MultiplayerClient || Phase == previousPhase)
            {
                return;
            }

            bool inRecovery = Phase == AttackPhase.MagicRecovery
                || Phase == AttackPhase.MeleeComboRecovery
                || Phase == AttackPhase.MeleeRecovery;
            if (!inRecovery)
            {
                return;
            }

            if (_unskippableRecovery)
            {
                _unskippableRecovery = false;
                return;
            }
            if (Phase == AttackPhase.MagicRecovery && _spell == Spell.DarkBeadBarrage)
            {
                return;
            }

            float healthFraction = NPC.life / (float)NPC.lifeMax;
            int skipChance = 0;
            if (healthFraction <= RecoverySkipLowHp)
            {
                skipChance = RecoverySkipLowChance;
            }
            else if (healthFraction <= RecoverySkipHighHp)
            {
                skipChance = RecoverySkipHighChance;
            }

            if (skipChance > 0 && Main.rand.Next(100) < skipChance)
            {
                PhaseTimer = 1;
                NPC.netUpdate = true;
            }
        }

        /// <summary>The debug HUD's current-attack line. Not inherited — every puppet boss defines its own
        /// (enemy-redesign G7), writing into the base's DebugAttackLabel.</summary>
        private void SetAttackLabel(string name, int ticks = 90)
        {
            DebugAttackLabel = name;
            _attackLabelTimer = ticks;
        }

        #region Hyper-armor bookkeeping
        // Runs after base.AI(), which has just recomputed the flags for this tick, so these writes hold
        // until the next tick's recompute. Every telegraph in the kit is committed from frame 1 EXCEPT the
        // first 55 ticks of Dark Bead Barrage — the fight's one interruptible channel.
        private void ApplyAttackArmor()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            bool darkBeadChannel = Phase == AttackPhase.MagicTelegraph && _spell == Spell.DarkBeadBarrage;
            if (darkBeadChannel)
            {
                bool committed = PhaseTimer <= DarkBeadCommitTicks;
                globalNPC.AttackCommitted = committed;
                globalNPC.AttackTelegraphing = !committed;
                return;
            }

            bool castingCommitted = Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack;
            bool surgeCommitted = Phase == AttackPhase.Custom && _setPiece == SetPiece.CorruptionSurge
                && PhaseTimer <= SurgeCommitTicks;
            bool locustTelegraphing = Phase == AttackPhase.Custom && _setPiece == SetPiece.LocustSwarm
                && PhaseTimer > LocustSwarmStreamTicks + LocustSwarmLandTicks;
            bool locustCommitted = Phase == AttackPhase.Custom && _setPiece == SetPiece.LocustSwarm
                && PhaseTimer > LocustSwarmLandTicks
                && PhaseTimer <= LocustSwarmStreamTicks + LocustSwarmLandTicks;
            bool wingDashTelegraphing = Phase == AttackPhase.Custom && _setPiece == SetPiece.WingedAxeRush
                && PhaseTimer > WingDashRecoveryTicks + WingDashSwingTicks + WingDashTravelTicks;
            bool wingDashCommitted = Phase == AttackPhase.Custom && _setPiece == SetPiece.WingedAxeRush
                && PhaseTimer > WingDashRecoveryTicks
                && PhaseTimer <= WingDashRecoveryTicks + WingDashSwingTicks + WingDashTravelTicks;
            bool setPieceCommitted = Phase == AttackPhase.Custom
                && (_setPiece == SetPiece.PhaseTransition
                    || _setPiece == SetPiece.StrafingFlight
                    || _setPiece == SetPiece.AbyssalRain
                    || _setPiece == SetPiece.Desperation);

            if (castingCommitted || surgeCommitted || locustCommitted || wingDashCommitted || setPieceCommitted)
            {
                globalNPC.AttackCommitted = true;
                globalNPC.AttackTelegraphing = false;
            }
            else if ((Phase == AttackPhase.Custom && _setPiece == SetPiece.CorruptionSurge)
                || locustTelegraphing || wingDashTelegraphing)
            {
                globalNPC.AttackTelegraphing = true;
            }
        }

        public override void OnStagger(NPC npc)
        {
            // Cancel whatever the subclass owns, then let the base clear the shared puppet state. Corruption
            // Surge is the only Custom set-piece a stagger can reach (the rest are hyper-armored), and its
            // one-shot flag stays set so it cannot re-fire.
            _setPiece = SetPiece.None;
            _wingDashBladeArmed = false;
            _wingDashLockCuePlayed = false;
            _wingDashLocked = false;
            NPC.noGravity = false;
            Flight?.RequestLand();
            base.OnStagger(npc);
        }
        #endregion

        #region Spell selection
        // Bag entries per spell by distance band and phase. Weight 0 removes the spell from that bag
        // entirely — Dark Fan, Seeker Orb and Dark Bead all need room to land, so none of them appear at
        // point-blank range.
        //                            P1 Close  Mid  Far   P2 Close  Mid  Far
        private static readonly int[,] SpellWeights =
        {
            { 3, 5, 5, 2, 3, 4 }, // BoltVolley
            { 0, 3, 4, 0, 2, 3 }, // DarkFan
            { 0, 2, 3, 0, 1, 2 }, // SeekerOrb
            { 2, 3, 3, 1, 2, 2 }, // StarCascade
            { 0, 1, 2, 0, 2, 3 }, // DarkBeadBarrage
        };

        private RangeBand CurrentBand()
        {
            if (!NPC.HasValidTarget)
            {
                return RangeBand.Far;
            }

            float tiles = NPC.Distance(Main.player[NPC.target].Center) / 16f;
            if (tiles < 6f)
            {
                return RangeBand.Close;
            }
            if (tiles <= 15f)
            {
                return RangeBand.Mid;
            }
            return RangeBand.Far;
        }

        // Server-side, and called before the base reads MagicTelegraphTicks, so the whole cast is shaped by
        // the spell this picks.
        protected override void OnMagicTelegraphStarting()
        {
            _spell = DrawSpell();
            _spellCooldowns[(int)_spell] = SpellCooldownFor(_spell);
            NPC.netUpdate = true;
        }

        private int SpellCooldownFor(Spell spell) => spell switch
        {
            Spell.BoltVolley => BoltVolleyCooldown,
            Spell.DarkFan => DarkFanCooldown,
            Spell.SeekerOrb => SeekerOrbCooldown,
            Spell.StarCascade => StarCascadeCooldown,
            Spell.DarkBeadBarrage => DarkBeadCooldown,
            _ => 150,
        };

        // Shuffled bag without replacement, refilled from the CURRENT band and phase weights. Dealing from a
        // bag instead of rolling weights independently is what keeps the rare spells from starving.
        private Spell DrawSpell()
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                for (int i = _spellBag.Count - 1; i >= 0; i--)
                {
                    Spell candidate = _spellBag[i];
                    bool ready = _spellCooldowns[(int)candidate] <= 0;
                    bool orbSlotFree = candidate != Spell.SeekerOrb || _seekerOrbIndex < 0;
                    if (ready && orbSlotFree && candidate != _lastSpell)
                    {
                        _spellBag.RemoveAt(i);
                        _lastSpell = candidate;
                        return candidate;
                    }
                }
                RefillSpellBag();
            }

            // Everything else is on cooldown or gated out: the bread-and-butter volley always works.
            _lastSpell = Spell.BoltVolley;
            return Spell.BoltVolley;
        }

        private void RefillSpellBag()
        {
            _spellBag.Clear();

            int bandColumn = (int)CurrentBand();
            if (_axeDrawn)
            {
                bandColumn += 3;
            }

            for (int spell = 0; spell < SpellCount; spell++)
            {
                int weight = SpellWeights[spell, bandColumn];
                for (int entry = 0; entry < weight; entry++)
                {
                    _spellBag.Add((Spell)spell);
                }
            }

            for (int i = _spellBag.Count - 1; i > 0; i--)
            {
                int swapIndex = Main.rand.Next(i + 1);
                Spell swapped = _spellBag[i];
                _spellBag[i] = _spellBag[swapIndex];
                _spellBag[swapIndex] = swapped;
            }
        }
        #endregion

        #region Spell casts
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 tip = StaffTipPosition();

            if (_spell == Spell.DarkBeadBarrage)
            {
                // The signature channel: a wide vortex dragging inward to the staff, far bigger than any
                // other tell, so "this is the scary one" reads from across the arena.
                for (int i = 0; i < 4; i++)
                {
                    float radius = MathHelper.Lerp(150f, 20f, progress);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust mote = Dust.NewDustPerfect(tip + offset, DustID.Shadowflame,
                        -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(2f, 7f, progress), 60, default,
                        MathHelper.Lerp(1.2f, 2.4f, progress));
                    mote.noGravity = true;
                }
                Lighting.AddLight(tip, 0.8f * progress + 0.3f, 0.1f, 0.25f * progress + 0.1f);

                if (PhaseTimer == DarkBeadCommitTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = 0.5f }, NPC.Center);
                }
                return;
            }

            // Every other cast: red-black motes converging on the gem, tightening and speeding up as the
            // wind-up runs out.
            float gatherRadius = MathHelper.Lerp(44f, 8f, progress);
            int moteCount = 1 + (progress > 0.55f ? 1 : 0);
            for (int i = 0; i < moteCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(gatherRadius, gatherRadius);
                int dustType = Main.rand.NextBool(3) ? DustID.CrimsonSpray : DustID.Shadowflame;
                Dust mote = Dust.NewDustPerfect(tip + offset, dustType,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 3.4f, progress), 70, default,
                    MathHelper.Lerp(0.9f, 1.6f, progress));
                mote.noGravity = true;
            }
            Lighting.AddLight(tip, 0.35f + 0.45f * progress, 0.08f, 0.12f + 0.15f * progress);

            if (_spell == Spell.StarCascade)
            {
                // The ring itself winds up at the tip — an electric-blue circle tracing wider and brighter
                // over the tell, so the payload is visible and COLOUR-MATCHED before anything is thrown.
                // Blue here, not the kit's red: this is the one cast that fires the electric rings.
                float ringRadius = 6f + progress * 16f;
                for (int i = 0; i < 2; i++)
                {
                    float traceAngle = progress * MathHelper.TwoPi * 3f + i * MathHelper.Pi;
                    Vector2 trace = traceAngle.ToRotationVector2() * ringRadius;
                    Dust core = Dust.NewDustPerfect(tip + trace, DustID.Electric, Vector2.Zero, 30, default,
                        0.7f + progress * 1.3f);
                    core.noGravity = true;
                }
                Lighting.AddLight(tip, 0.15f * progress, 0.4f * progress, 0.7f * progress);
            }
        }

        protected override void DoMagicAttack()
        {
            SetAttackLabel(SpellDisplayName(_spell), 110);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            }

            switch (_spell)
            {
                case Spell.BoltVolley:
                    // A channel: DoMagicTick fires one bolt every 9 ticks across the 45-tick window.
                    _magicAttackTicksOverride = BoltVolleyChannel;
                    break;

                case Spell.StarCascade:
                    _starCascadeRings = (byte)StarCascadeRingCount();
                    _starCascadeInterval = (byte)StarCascadeInterval();
                    _magicAttackTicksOverride = _starCascadeRings * _starCascadeInterval;
                    break;

                case Spell.DarkBeadBarrage:
                    _magicAttackTicksOverride = DarkBeadChannel;
                    break;

                case Spell.DarkFan:
                    CastDarkFan();
                    break;

                case Spell.SeekerOrb:
                    CastSeekerOrb();
                    break;
            }
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            switch (_spell)
            {
                case Spell.BoltVolley:
                {
                    int elapsed = BoltVolleyChannel - ticksRemaining;
                    if (elapsed % BoltVolleyInterval == 0)
                    {
                        FireAimedBolt(BoltSpeed, 0f);
                    }
                    break;
                }

                case Spell.StarCascade:
                {
                    int elapsed = _starCascadeRings * _starCascadeInterval - ticksRemaining;
                    if (elapsed % _starCascadeInterval == 0)
                    {
                        // Deal spots in order so the rings of one cast never stack on each other.
                        CastStar(elapsed / _starCascadeInterval);
                    }
                    break;
                }

                case Spell.DarkBeadBarrage:
                {
                    // Seven beads from the centre outwards, alternating sides: 0, -1, +1, -2, +2, -3, +3.
                    int elapsed = DarkBeadChannel - ticksRemaining;
                    if (elapsed % DarkBeadInterval != 0)
                    {
                        break;
                    }

                    int index = elapsed / DarkBeadInterval;
                    if (index >= DarkBeadCount)
                    {
                        break;
                    }

                    int step = (index + 1) / 2;
                    int side = (index % 2 == 0) ? 1 : -1;
                    FireAimedBolt(DarkBeadSpeed, step * side * DarkBeadSpreadDegrees);
                    break;
                }
            }
        }

        private void CastDarkFan()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            for (int i = -2; i <= 2; i++)
            {
                FireAimedBolt(FanBoltSpeed, i * FanSpreadDegrees);
            }
        }

        private void CastSeekerOrb()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = StaffTipPosition();
            Vector2 velocity = (target.Center - tip).SafeNormalize(new Vector2(NPC.direction, 0f)) * SeekerOrbSpeed;

            // ai[0] = the player it homes on, ai[1] = 1 selects the slow "Seeker Orb" tuning.
            _seekerOrbIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, velocity,
                ModContent.ProjectileType<OolacileDarkOrb>(), 59, 1f, Main.myPlayer,
                target.whoAmI, 1f);
            NPC.netUpdate = true;
        }

        // Three rings above half health, six below, on a beat that tightens the same way the TEST boss's
        // own star attack did.
        private int StarCascadeRingCount() =>
            NPC.life <= NPC.lifeMax / 2 ? StarCascadeRingsLowHp : StarCascadeRingsHighHp;

        private int StarCascadeInterval() =>
            NPC.life <= NPC.lifeMax / 2 ? StarCascadeIntervalLowHp : StarCascadeIntervalHighHp;

        /// <summary>
        /// Throws one electric ring at its own reserved spot inside the arena. The ring parks there and
        /// spins until it bursts, so unlike the old star it can never sail off through the floor.
        /// Works identically grounded or airborne: the launch direction is simply staff-tip to spot, which
        /// from a hovering boss already points down into the arena.
        /// </summary>
        private void CastStar(int spotIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = StaffTipPosition();
            Vector2 spot = FindOpenSpot(target.Center + StarSpotOffsets[spotIndex % StarSpotOffsets.Length], target.Center);
            Vector2 velocity = (spot - tip).SafeNormalize(new Vector2(NPC.direction, 0f)) * StarSpeed;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = 0.25f }, NPC.Center);
            }

            // ai[0] = damage per child ring, ai[1]/ai[2] = the point to park on.
            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, velocity,
                ModContent.ProjectileType<OccultistStarBig>(), 60, 1f, Main.myPlayer,
                MagicDamage, spot.X, spot.Y);
        }

        /// <summary>
        /// Nudges a desired park spot out of solid terrain by walking it back toward the player, so a ring
        /// aimed at a wall settles in the open instead of hanging inside a hillside. Falls back to the
        /// anchor itself if nothing along the way is clear.
        /// </summary>
        private static Vector2 FindOpenSpot(Vector2 desired, Vector2 anchor)
        {
            Vector2 towardAnchor = (anchor - desired).SafeNormalize(Vector2.UnitY);

            for (int step = 0; step < StarSpotClearanceSteps; step++)
            {
                Vector2 candidate = desired + towardAnchor * (step * StarSpotClearanceStep);
                if (!Collision.SolidCollision(candidate - new Vector2(34f), 68, 68))
                {
                    return candidate;
                }
            }

            return anchor;
        }

        private void FireAimedBolt(float speed, float offsetDegrees)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = StaffTipPosition();
            Vector2 aimPoint = target.Center + target.velocity * AimLeadTicks;
            Vector2 direction = (aimPoint - tip).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 velocity = direction.RotatedBy(MathHelper.ToRadians(offsetDegrees)) * speed * AbandonSpeedMultiplier();

            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, velocity,
                ModContent.ProjectileType<OolacileBolt>(), MagicDamage, 1f, Main.myPlayer);
        }

        private static string SpellDisplayName(Spell spell) => spell switch
        {
            Spell.BoltVolley => "Dark Bolt Volley",
            Spell.DarkFan => "Dark Fan",
            Spell.SeekerOrb => "Seeker Orb",
            Spell.StarCascade => "Star Cascade",
            Spell.DarkBeadBarrage => "Dark Bead Barrage",
            _ => "Dark Spell",
        };

        // The hand pose only exists where the puppet is drawn; a dedicated server estimates the tip from the
        // cast's end angle (the same fallback Kahlrun and the Oolacile Cultist use).
        private Vector2 StaffTipPosition()
        {
            if (!Main.dedServ)
            {
                return PuppetWeaponTipPosition(StaffTipReach);
            }

            float worldAngle = MagicCastEndRotation;
            if (NPC.direction != 1)
            {
                worldAngle = MathHelper.Pi - MagicCastEndRotation;
            }
            return NPC.Center + new Vector2(NPC.direction * 5f, -4f) + worldAngle.ToRotationVector2() * StaffTipReach;
        }
        #endregion

        #region Staff-Axe Weave (phase 2 identity)
        // Two bolts leave the FREE hand during each of the Weave's 34-tick inter-step pauses. The axe stays
        // in the swinging hand, so this is bare-handed sorcery rather than a second staff.
        private const int WeaveGatherTicks = 14;
        private const float WeaveBoltSpeed = 5.5f;
        private const float WeaveSpreadDegrees = 11f;
        private int _weaveCastTimer = -1;

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);

            if (ActiveMeleeComboName == StaffAxeWeaveName && step.PostStepPause > 0)
            {
                _weaveCastTimer = 0;
            }
        }

        private void TickWeaveCast()
        {
            if (_weaveCastTimer < 0)
            {
                return;
            }

            // The pause is the only place this runs; anything else (a stagger, the combo ending) cancels it.
            if (Phase != AttackPhase.MeleeComboPause)
            {
                _weaveCastTimer = -1;
                return;
            }

            Vector2 hand = PuppetBackHandPosition;

            if (_weaveCastTimer < WeaveGatherTicks)
            {
                if (!Main.dedServ)
                {
                    float progress = _weaveCastTimer / (float)WeaveGatherTicks;
                    Vector2 offset = Main.rand.NextVector2CircularEdge(26f * (1f - progress) + 5f, 26f * (1f - progress) + 5f);
                    Dust mote = Dust.NewDustPerfect(hand + offset, DustID.Shadowflame, -offset * 0.12f, 60, default,
                        MathHelper.Lerp(0.9f, 1.7f, progress));
                    mote.noGravity = true;
                    Lighting.AddLight(hand, 0.5f * progress + 0.15f, 0.06f, 0.2f * progress);

                    if (_weaveCastTimer == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                    }
                }
                _weaveCastTimer++;
                return;
            }

            _weaveCastTimer = -1;
            SetAttackLabel("Staff-Axe Weave (bolt)", 40);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 direction = (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 velocity = direction.RotatedBy(MathHelper.ToRadians(i * WeaveSpreadDegrees)) * WeaveBoltSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), hand, velocity,
                    ModContent.ProjectileType<OolacileBolt>(), MagicDamage, 1f, Main.myPlayer);
            }
        }
        #endregion

        #region Set-pieces
        private bool FreeToAct =>
            Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;

        // The 50% transition. Deliberately NOT server-gated: NPC.life is synced, so every peer crosses the
        // threshold on the same tick, which is tighter than waiting for a phase packet (enemy-redesign G0.7).
        private void TryStartPhaseTransition()
        {
            bool belowHalf = NPC.life > 0 && NPC.life <= NPC.lifeMax / 2;
            if (!belowHalf || _phase2Triggered)
            {
                return;
            }

            _phase2Triggered = true;
            _setPiece = SetPiece.PhaseTransition;
            SetAttackLabel("Phase 2 — Draws the Axe", TransitionTicks);
            StartCustomAttack(TransitionTicks, ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
        }

        private void TryStartCorruptionSurge()
        {
            bool belowThirty = NPC.life > 0 && NPC.life <= NPC.lifeMax * 0.3f;
            if (!belowThirty || _surgeDone || !_axeDrawn || _desperationStarted)
            {
                return;
            }
            if (!FreeToAct || !NPC.HasValidTarget)
            {
                return;
            }

            _surgeDone = true;
            _setPiece = SetPiece.CorruptionSurge;
            SetAttackLabel("Corruption Surge", SurgeTelegraphTicks + SurgeCommitTicks);
            StartCustomAttack(SurgeTelegraphTicks + SurgeCommitTicks, ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
        }

        private void TryStartStrafingFlight()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            bool blocked = !FreeToAct || _strafeCooldown > 0 || _desperationStarted
                || !NPC.HasValidTarget || NPC.velocity.Y != 0f || Flight == null || Flight.IsAirborne;
            if (blocked || !Main.rand.NextBool(90))
            {
                return;
            }

            _strafeCooldown = StrafeCooldown;
            _setPiece = SetPiece.StrafingFlight;
            _strafeDir = Main.player[NPC.target].Center.X < NPC.Center.X ? -1 : 1;
            SetAttackLabel("Strafing Flight", 120);
            StartCustomAttack(StrafeAscendTicks + StrafeRunTicks + StrafeLandTicks,
                ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
        }

        private void TryStartLocustSwarm()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool blocked = !FreeToAct || _locustSwarmCooldown > 0 || _desperationStarted
                || !NPC.HasValidTarget || NPC.velocity.Y != 0f || Flight == null || Flight.IsAirborne
                || Locust.CountActive() >= Locust.PopulationCap;
            if (blocked || !Main.rand.NextBool(90))
            {
                return;
            }

            Player target = Main.player[NPC.target];
            _locustSwarmCooldown = LocustSwarmCooldown;
            _locustPassDirection = NPC.Center.X <= target.Center.X ? 1 : -1;
            // NPC.ai[] is float-backed; every integer through 2^24 remains exact across synchronization.
            _locustGroupId = Main.rand.Next(1, 16_777_216);
            _setPiece = SetPiece.LocustSwarm;
            SetAttackLabel("Locust Swarm",
                LocustSwarmTelegraphTicks + LocustSwarmStreamTicks + LocustSwarmLandTicks);
            StartCustomAttack(LocustSwarmTelegraphTicks + LocustSwarmStreamTicks + LocustSwarmLandTicks,
                ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
        }

        private void TryStartAbyssalRain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            bool blocked = !_axeDrawn || !FreeToAct || _rainCooldown > 0 || _desperationStarted
                || !NPC.HasValidTarget || NPC.velocity.Y != 0f || Flight == null || Flight.IsAirborne;
            if (blocked || !Main.rand.NextBool(110))
            {
                return;
            }

            _rainCooldown = RainCooldown;
            _setPiece = SetPiece.AbyssalRain;
            SetAttackLabel("Abyssal Rain", 120);
            StartCustomAttack(RainTelegraphTicks + RainBoltCount * RainBoltInterval + RainLandTicks,
                ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
        }

        private void TryStartWingedAxeRush()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool blocked = !_axeDrawn || !FreeToAct || _wingDashCooldown > 0 || _desperationStarted
                || !NPC.HasValidTarget || NPC.velocity.Y != 0f || Flight == null || Flight.IsAirborne;
            if (blocked)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            bool clearOpening = Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
                target.position, target.width, target.height);
            if (distance < 120f || distance > 760f || !clearOpening || !Main.rand.NextBool(80))
            {
                return;
            }

            _wingDashCooldown = WingDashCooldown;
            _wingDashLocked = false;
            _wingDashBladeArmed = false;
            _wingDashDirection = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
            _wingDashAimPoint = target.Center;
            _wingDashEndPoint = target.Center;
            _setPiece = SetPiece.WingedAxeRush;
            SetAttackLabel(WingedAxeRushName, WingDashTotalTicks);
            StartCustomAttack(WingDashTotalTicks, ModContent.ItemType<EnemyOccultistAxe>());
            NPC.netUpdate = true;
        }

        // Flying set-pieces and the desperation all drive NPC.velocity themselves.
        protected override bool SlowDownDuringCustom =>
            _setPiece != SetPiece.StrafingFlight
            && _setPiece != SetPiece.LocustSwarm
            && _setPiece != SetPiece.AbyssalRain
            && _setPiece != SetPiece.WingedAxeRush
            && _setPiece != SetPiece.Desperation;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_setPiece == SetPiece.WingedAxeRush)
                {
                    return WingDashWeaponRotation();
                }
                // Magic-pose space: 0 = level forward, negative = up.
                if (_setPiece == SetPiece.PhaseTransition || _setPiece == SetPiece.CorruptionSurge)
                {
                    return -MathHelper.PiOver2; // staff thrust straight up
                }
                if (_setPiece == SetPiece.AbyssalRain)
                {
                    return -1.25f; // arms wide, staff raised toward the sky it is seeding
                }
                if (_setPiece == SetPiece.LocustSwarm)
                {
                    return -0.65f; // staff held into the oncoming cloud during the aerial pass
                }
                if (_setPiece == SetPiece.StrafingFlight && NPC.HasValidTarget)
                {
                    Player target = Main.player[NPC.target];
                    Vector2 toTarget = target.Center - NPC.Center;
                    float forwardX = Math.Max(1f, Math.Abs(toTarget.X));
                    return MathHelper.Clamp(MathF.Atan2(toTarget.Y, forwardX), -1.3f, 1.3f);
                }
                return null;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            switch (_setPiece)
            {
                case SetPiece.PhaseTransition:
                    TickPhaseTransition(TransitionTicks - ticksRemaining, ticksRemaining);
                    break;

                case SetPiece.StrafingFlight:
                    TickStrafingFlight(StrafeAscendTicks + StrafeRunTicks + StrafeLandTicks - ticksRemaining, ticksRemaining);
                    break;

                case SetPiece.LocustSwarm:
                    TickLocustSwarm(
                        LocustSwarmTelegraphTicks + LocustSwarmStreamTicks + LocustSwarmLandTicks - ticksRemaining,
                        ticksRemaining);
                    break;

                case SetPiece.AbyssalRain:
                    TickAbyssalRain(
                        RainTelegraphTicks + RainBoltCount * RainBoltInterval + RainLandTicks - ticksRemaining,
                        ticksRemaining);
                    break;

                case SetPiece.WingedAxeRush:
                    TickWingedAxeRush(WingDashTotalTicks - ticksRemaining, ticksRemaining);
                    break;

                case SetPiece.CorruptionSurge:
                    TickCorruptionSurge(SurgeTelegraphTicks + SurgeCommitTicks - ticksRemaining, ticksRemaining);
                    break;

                case SetPiece.Desperation:
                    TickDesperation(DesperationTotalTicks - ticksRemaining, ticksRemaining);
                    break;
            }
        }

        private void TickPhaseTransition(int elapsed, int ticksRemaining)
        {
            NPC.velocity.X *= 0.8f;

            if (!Main.dedServ)
            {
                // Black smoke boiling off the robe, thickening the whole 90 ticks.
                float progress = elapsed / (float)TransitionTicks;
                int moteCount = 2 + (int)(progress * 5f);
                for (int i = 0; i < moteCount; i++)
                {
                    Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith,
                        0f, -2f, 120, Color.Black, MathHelper.Lerp(1.4f, 2.8f, progress));
                    smoke.noGravity = true;
                    smoke.velocity = new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-3.4f, -1f));
                }

                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f, Pitch = -0.6f }, NPC.Center);
                }
            }

            if (ticksRemaining != 1)
            {
                return;
            }

            DrawTheAxe();
        }

        // The reveal itself. _phase2Triggered guards re-entry; _axeDrawn is what actually flips the weapon,
        // the movement band and the spell weights, so the axe appears on the last frame of the ceremony.
        private void DrawTheAxe()
        {
            if (_axeDrawn)
            {
                return;
            }

            _axeDrawn = true;
            _setPiece = SetPiece.None;
            NPC.defense = 55; // wards down: the aggressor is more exposed than the kiter was
            _spellBag.Clear();
            NPC.netUpdate = true;

            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1f, Pitch = -0.6f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 8f, 22, distanceFalloff: 900f);
            for (int i = 0; i < 90; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.4f, 1f);
                int dustType = Main.rand.NextBool(3) ? DustID.CrimsonSpray : DustID.Wraith;
                Dust burst = Dust.NewDustPerfect(NPC.Center, dustType, outward, 60, default,
                    Main.rand.NextFloat(1.6f, 3f));
                burst.noGravity = true;
            }
        }

        private void TickStrafingFlight(int elapsed, int ticksRemaining)
        {
            // Clients never ran the server's takeoff request; RequestTakeoff ignores repeats.
            if (Flight != null && !Flight.IsAirborne && elapsed < StrafeAscendTicks + StrafeRunTicks)
            {
                Flight.RequestTakeoff();
            }
            NPC.noGravity = true;

            if (!NPC.HasValidTarget)
            {
                EndFlightSetPiece();
                return;
            }

            Player target = Main.player[NPC.target];
            FacePlayer(target);

            if (elapsed < StrafeAscendTicks)
            {
                // Climb to the strafing altitude directly above the player.
                Vector2 stage = target.Center - new Vector2(0f, StrafeAltitude);
                NPC.velocity = Vector2.Lerp(NPC.velocity,
                    (stage - NPC.Center).SafeNormalize(Vector2.Zero) * StrafeFlySpeed, 0.14f);
                EmitFlightDust();
                return;
            }

            if (elapsed < StrafeAscendTicks + StrafeRunTicks)
            {
                // Sweep back and forth across the player at altitude, reversing at the band edges, firing
                // one aimed bolt every 30 ticks.
                float offsetX = NPC.Center.X - target.Center.X;
                if (offsetX * _strafeDir > StrafeSweepHalfWidth)
                {
                    _strafeDir = -_strafeDir;
                }

                Vector2 stage = target.Center + new Vector2(_strafeDir * StrafeSweepHalfWidth, -StrafeAltitude);
                NPC.velocity = Vector2.Lerp(NPC.velocity,
                    (stage - NPC.Center).SafeNormalize(Vector2.Zero) * StrafeFlySpeed, 0.09f);
                EmitFlightDust();

                // Every third volley drops an electric ring instead of a bolt, so Star Cascade's payload also
                // appears in flight — from up here the staff-tip-to-spot launch already points downward, so
                // the rings rain into the arena and park there. Volleys 2 and 5 of the eight in a run.
                int runElapsed = elapsed - StrafeAscendTicks;
                if (runElapsed % StrafeBoltInterval == 0)
                {
                    int volleyIndex = runElapsed / StrafeBoltInterval;
                    if (volleyIndex % 3 == 2)
                    {
                        CastStar(volleyIndex / 3);
                    }
                    else
                    {
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.55f, Pitch = -0.1f }, NPC.Center);
                        }
                        FireAimedBolt(BoltSpeed, 0f);
                    }
                }
                return;
            }

            // Descend and land. The 50-tick tail is the punish window.
            Flight?.RequestLand();
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.5f, 10f);

            if (ticksRemaining == 1)
            {
                EndFlightSetPiece();
            }
        }

        private void TickLocustSwarm(int elapsed, int ticksRemaining)
        {
            if (Flight != null && !Flight.IsAirborne
                && elapsed < LocustSwarmTelegraphTicks + LocustSwarmStreamTicks)
            {
                Flight.RequestTakeoff();
            }
            NPC.noGravity = true;

            if (!NPC.HasValidTarget)
            {
                EndFlightSetPiece();
                return;
            }

            Player target = Main.player[NPC.target];
            FacePlayer(target);

            if (elapsed < LocustSwarmTelegraphTicks)
            {
                Vector2 stage = LocustStagePoint(target, -_locustPassDirection * LocustCloudWidth * 0.5f);
                FlyToward(stage, LocustSwarmFlySpeed, 0.15f);
                EmitFlightDust();
                TelegraphLocustCloud(target, elapsed / (float)LocustSwarmTelegraphTicks);

                if (elapsed == 0 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = -0.55f }, NPC.Center);
                }
                return;
            }

            if (elapsed < LocustSwarmTelegraphTicks + LocustSwarmStreamTicks)
            {
                int streamElapsed = elapsed - LocustSwarmTelegraphTicks;
                float progress = streamElapsed / (float)Math.Max(1, LocustSwarmStreamTicks - 1);
                float horizontalOffset = MathHelper.Lerp(-LocustCloudWidth * 0.5f,
                    LocustCloudWidth * 0.5f, progress) * _locustPassDirection;
                Vector2 stage = LocustStagePoint(target, horizontalOffset);
                FlyToward(stage, LocustSwarmFlySpeed, 0.2f);
                EmitFlightDust();

                if (streamElapsed % LocustSwarmSpawnInterval == 0)
                {
                    SpawnBossLocust(stage);

                    if (!Main.dedServ && streamElapsed % (LocustSwarmSpawnInterval * 10) == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = -0.35f }, NPC.Center);
                    }
                }
                return;
            }

            // The cast has paid off. Landing is a real punish window: no hyper-armor and no more spawns.
            Flight?.RequestLand();
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.5f, 10f);

            if (ticksRemaining == 1)
            {
                EndFlightSetPiece();
            }
        }

        private void SpawnBossLocust(Vector2 cloudPoint)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || Locust.CountActive() >= Locust.PopulationCap)
            {
                return;
            }

            Vector2 preferred = cloudPoint
                + new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-LocustCloudHeight * 0.5f,
                    LocustCloudHeight * 0.5f));
            Vector2 spawnPoint = FindOpenLocustSpawn(preferred);
            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPoint.X, (int)spawnPoint.Y,
                ModContent.NPCType<Locust>(), Target: NPC.target, ai0: 1f, ai1: _locustGroupId);
            if (spawned >= Main.maxNPCs)
            {
                return;
            }

            NPC locust = Main.npc[spawned];
            locust.Center = spawnPoint;
            locust.velocity = new Vector2(_locustPassDirection * Main.rand.NextFloat(0.8f, 2.2f),
                Main.rand.NextFloat(-1.1f, 1.1f));
            locust.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
            }
        }

        private void TelegraphLocustCloud(Player target, float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            int dustCount = 2 + (int)(progress * 3f);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 cloudPoint = target.Center + new Vector2(
                    Main.rand.NextFloat(-LocustCloudWidth * 0.5f, LocustCloudWidth * 0.5f),
                    -LocustCloudAltitude + Main.rand.NextFloat(-LocustCloudHeight * 0.5f,
                        LocustCloudHeight * 0.5f));
                Dust smoke = Dust.NewDustPerfect(cloudPoint, DustID.Wraith,
                    new Vector2(0f, Main.rand.NextFloat(-0.4f, 0.4f)), 110, Color.Black,
                    MathHelper.Lerp(0.9f, 1.7f, progress));
                smoke.noGravity = true;
            }
            Lighting.AddLight(target.Center - new Vector2(0f, LocustCloudAltitude),
                0.18f * progress, 0.01f, 0.01f);
        }

        private Vector2 LocustStagePoint(Player target, float horizontalOffset)
        {
            Vector2 stage = target.Center + new Vector2(horizontalOffset, -LocustCloudAltitude);
            float ceiling = FindCeilingY(stage, 20);
            if (ceiling > 0f)
            {
                stage.Y = Math.Max(stage.Y, ceiling + NPC.height * 0.5f + 8f);
            }

            for (int step = 0; step < 8
                && Collision.SolidCollision(stage - NPC.Size * 0.5f, NPC.width, NPC.height); step++)
            {
                stage.Y += 16f;
            }
            return stage;
        }

        private void FlyToward(Vector2 stage, float maxSpeed, float inertia)
        {
            Vector2 desired = (stage - NPC.Center) * 0.14f;
            if (desired.Length() > maxSpeed)
            {
                desired = desired.SafeNormalize(Vector2.Zero) * maxSpeed;
            }
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, inertia);
        }

        private Vector2 FindOpenLocustSpawn(Vector2 preferred)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 candidate = attempt == 0
                    ? preferred
                    : preferred + Main.rand.NextVector2Circular(28f, 28f);
                if (!Collision.SolidCollision(candidate - new Vector2(9f), 18, 18))
                {
                    return candidate;
                }
            }
            return NPC.Center;
        }

        private void TickAbyssalRain(int elapsed, int ticksRemaining)
        {
            int rainTicks = RainBoltCount * RainBoltInterval;

            if (Flight != null && !Flight.IsAirborne && elapsed < RainTelegraphTicks + rainTicks)
            {
                Flight.RequestTakeoff();
            }
            NPC.noGravity = true;

            if (!NPC.HasValidTarget)
            {
                EndFlightSetPiece();
                return;
            }

            Player target = Main.player[NPC.target];
            FacePlayer(target);

            if (elapsed < RainTelegraphTicks)
            {
                // Rise and hold while the sky above the arena fills with forming bolts.
                Vector2 stage = target.Center - new Vector2(0f, StrafeAltitude * 0.8f);
                NPC.velocity = Vector2.Lerp(NPC.velocity,
                    (stage - NPC.Center).SafeNormalize(Vector2.Zero) * StrafeFlySpeed, 0.12f);
                EmitFlightDust();

                if (!Main.dedServ)
                {
                    float progress = elapsed / (float)RainTelegraphTicks;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 skyPoint = target.Center
                            + new Vector2(Main.rand.NextFloat(-RainSpreadWidth, RainSpreadWidth), -RainSpawnHeight)
                            + Main.rand.NextVector2Circular(50f, 50f);
                        Dust seed = Dust.NewDustPerfect(skyPoint, DustID.Shadowflame,
                            new Vector2(0f, Main.rand.NextFloat(-0.8f, -0.2f)), 70, default,
                            MathHelper.Lerp(0.9f, 2f, progress));
                        seed.noGravity = true;
                    }

                    if (elapsed == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
                    }
                }
                return;
            }

            if (elapsed < RainTelegraphTicks + rainTicks)
            {
                NPC.velocity *= 0.9f;
                EmitFlightDust();

                int rainElapsed = elapsed - RainTelegraphTicks;
                int strikeIndex = rainElapsed / RainBoltInterval;
                int strikePhase = rainElapsed % RainBoltInterval;
                // Bolts walk across the arena left to right so a player can read where the next one lands.
                float strikeX = target.Center.X - RainSpreadWidth
                    + RainSpreadWidth * 2f * strikeIndex / Math.Max(1, RainBoltCount - 1);
                Vector2 groundPoint = FindGroundBelow(new Vector2(strikeX, target.Center.Y - 200f));

                if (strikePhase < RainMarkerTicks)
                {
                    MarkRainStrike(groundPoint, strikePhase / (float)RainMarkerTicks);
                }
                else if (strikePhase == RainMarkerTicks)
                {
                    LaunchRainBolt(groundPoint);
                }
                return;
            }

            Flight?.RequestLand();
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.5f, 10f);

            if (ticksRemaining == 1)
            {
                EndFlightSetPiece();
            }
        }

        // The 20-tick ground read: a ring of motes dragging inward onto the spot the bolt will hit.
        private void MarkRainStrike(Vector2 groundPoint, float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                float radius = MathHelper.Lerp(70f, 10f, progress);
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius * 0.35f);
                Dust mote = Dust.NewDustPerfect(groundPoint + offset, DustID.CrimsonSpray,
                    -offset * 0.1f, 50, default, MathHelper.Lerp(1f, 1.9f, progress));
                mote.noGravity = true;
            }
            Lighting.AddLight(groundPoint, 0.5f * progress, 0.05f, 0.1f);
        }

        private void LaunchRainBolt(Vector2 groundPoint)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = -0.5f }, groundPoint);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 origin = groundPoint - new Vector2(0f, RainSpawnHeight);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, new Vector2(0f, RainBoltSpeed),
                ModContent.ProjectileType<OolacileBolt>(), MagicDamage, 1f, Main.myPlayer);
        }

        private void TickCorruptionSurge(int elapsed, int ticksRemaining)
        {
            NPC.velocity.X *= 0.8f;

            if (!Main.dedServ)
            {
                // Abyss dragged inward from a full arena's width — the radius the dust sweeps IS the radius
                // the ring will cover.
                float progress = elapsed / (float)(SurgeTelegraphTicks + SurgeCommitTicks);
                for (int i = 0; i < 6; i++)
                {
                    float radius = MathHelper.Lerp(SurgeRingRadius, 30f, progress);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.Shadowflame,
                        -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(3f, 11f, progress), 50, default,
                        MathHelper.Lerp(1.4f, 2.8f, progress));
                    mote.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, 1.1f * progress, 0.12f, 0.3f * progress);

                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = -0.8f }, NPC.Center);
                }
                if (ticksRemaining == SurgeCommitTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.9f, Pitch = 0.6f }, NPC.Center);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer,
                        UsefulFunctions.ColorToFloat(new Color(190, 20, 45)));
                }
            }

            if (ticksRemaining != 1)
            {
                return;
            }

            if (!Main.dedServ)
            {
                UsefulFunctions.ScreenShake(NPC.Center, 12f, 26, distanceFalloff: 1200f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<OccultistAbyssRing>(), 70, 1f, Main.myPlayer, SurgeRingRadius);
            }

            _setPiece = SetPiece.None;
            // The surge's long recovery is its whole point, so the HP skip roll must not touch it.
            _unskippableRecovery = true;
            EnterPhase(AttackPhase.MagicRecovery, SurgeRecoveryTicks);
        }

        private void TickDesperation(int elapsed, int ticksRemaining)
        {
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;

            if (Flight != null && !Flight.IsAirborne)
            {
                Flight.RequestTakeoff();
            }

            if (NPC.HasValidTarget)
            {
                FacePlayer(Main.player[NPC.target]);
            }
            EmitFlightDust();

            if (elapsed < DesperationAscendTicks)
            {
                NPC.velocity.X *= 0.92f;
                NPC.velocity.Y = -DesperationAscendSpeed;
                return;
            }

            if (elapsed < DesperationAscendTicks + DesperationPlungeTicks)
            {
                if (elapsed == DesperationAscendTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = -0.7f }, NPC.Center);
                }
                NPC.velocity.Y = DesperationPlungeSpeed;
                AfterimageTicks = Math.Max(AfterimageTicks, 3);
                return;
            }

            // Spiral phase: hover in place and sweep an accelerating pinwheel of bolts outward. The arms
            // rotate, so standing still is never safe and the gaps close as the 11 seconds run down.
            float spiralProgress = (elapsed - DesperationAscendTicks - DesperationPlungeTicks)
                / (float)DesperationSpiralTicks;
            NPC.velocity *= 0.9f;

            int interval = (int)MathHelper.Lerp(10f, 4f, spiralProgress);
            int arms = 3 + (int)(spiralProgress * 3f);
            _desperationSpiralAngle += MathHelper.Lerp(0.18f, 0.34f, spiralProgress);

            if (elapsed % interval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int arm = 0; arm < arms; arm++)
                {
                    float angle = _desperationSpiralAngle + MathHelper.TwoPi * arm / arms;
                    Vector2 velocity = angle.ToRotationVector2() * DesperationSpiralSpeed;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                        ModContent.ProjectileType<OolacileBolt>(), MagicDamage, 1f, Main.myPlayer);
                }
            }

            if (elapsed % interval == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.4f, Pitch = 0.3f }, NPC.Center);
            }

            if (ticksRemaining != 1)
            {
                return;
            }

            _desperationComplete = true;
            NPC.dontTakeDamage = false;
            /*if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.StrikeInstantKill();
                if (Main.rand.NextBool(240))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 2);
                    projVelocity.Y -= 5;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<OolacileDarkOrb>(), darkOrbDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24, NPC.Center);
                    NPCSpawningTimer = 1f;
                    SecondAttackCounter = 0;
                }

                if (Main.rand.NextBool(30))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 8);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<OolacileSeeker>(), seekerDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPCSpawningTimer = 1f;
                }
            }*/
        }

        private void TickWingedAxeRush(int elapsed, int ticksRemaining)
        {
            int dashStart = WingDashAscendTicks + WingDashTelegraphTicks;
            int swingStart = dashStart + WingDashTravelTicks;
            int recoveryStart = swingStart + WingDashSwingTicks;

            if (Flight != null && !Flight.IsAirborne && elapsed < recoveryStart)
            {
                Flight.RequestTakeoff();
            }
            NPC.noGravity = true;

            if (!NPC.HasValidTarget)
            {
                StopWingDashBlade();
                EndFlightSetPiece();
                return;
            }

            Player target = Main.player[NPC.target];

            if (elapsed < WingDashAscendTicks)
            {
                _wingDashLockCuePlayed = false;
                float approachSide = Math.Sign(_wingDashDirection.X);
                if (approachSide == 0f)
                {
                    approachSide = NPC.direction;
                }
                Vector2 stage = WingDashStagePoint(target, -approachSide * WingDashStageHorizontal);
                FlyToward(stage, 9f, 0.17f);
                FacePlayer(target);
                EmitFlightDust();
                return;
            }

            if (elapsed == WingDashAscendTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                LockWingDash(target);
            }

            if (elapsed < dashStart)
            {
                NPC.velocity *= 0.82f;
                FaceWingDashDirection();
                TelegraphWingDash(elapsed - WingDashAscendTicks);
                EmitFlightDust();
                return;
            }

            if (elapsed < swingStart)
            {
                FaceWingDashDirection();
                if (_wingDashLocked)
                {
                    int dashElapsed = elapsed - dashStart;
                    int remaining = Math.Max(1, WingDashTravelTicks - dashElapsed);
                    Vector2 desiredVelocity = (_wingDashEndPoint - NPC.Center) / remaining;
                    if (desiredVelocity.Length() > WingDashTopSpeed)
                    {
                        desiredVelocity = desiredVelocity.SafeNormalize(_wingDashDirection) * WingDashTopSpeed;
                    }
                    NPC.velocity = desiredVelocity;
                    AfterimageTicks = Math.Max(AfterimageTicks, 4);
                    EmitWingDashTrail();

                    if (dashElapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.5f }, NPC.Center);
                        UsefulFunctions.ScreenShake(NPC.Center, 5f, 12, distanceFalloff: 850f);
                    }
                }
                else
                {
                    // A client may spend a frame here before the server's locked vector arrives.
                    NPC.velocity *= 0.85f;
                }
                return;
            }

            if (elapsed < recoveryStart)
            {
                NPC.velocity *= 0.68f;
                FaceWingDashDirection();
                int swingElapsed = elapsed - swingStart;

                if (swingElapsed == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        TryMeleeHit(AxeComboReach * AxeDrawScale);
                        _wingDashBladeArmed = true;
                    }
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item1 with
                        {
                            Volume = 0.95f,
                            Pitch = -0.5f,
                            PitchVariance = 0.08f,
                        }, NPC.Center);
                    }
                }

                if (_wingDashBladeArmed && swingElapsed > (int)MathF.Ceiling(WingDashSwingCurve.LiveTicks))
                {
                    StopWingDashBlade();
                }
                EmitWingDashSwingDust();
                return;
            }

            StopWingDashBlade();
            Flight?.RequestLand();
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.5f, 10f);

            if (ticksRemaining == 1)
            {
                _wingDashLocked = false;
                EndFlightSetPiece();
            }
        }

        private float WingDashWeaponRotation()
        {
            int elapsed = Math.Clamp(WingDashTotalTicks - PhaseTimer, 0, WingDashTotalTicks);
            int dashStart = WingDashAscendTicks + WingDashTelegraphTicks;
            int swingStart = dashStart + WingDashTravelTicks;
            int recoveryStart = swingStart + WingDashSwingTicks;

            if (elapsed < WingDashAscendTicks)
            {
                return MeleeCarryRotation;
            }
            if (elapsed < dashStart)
            {
                float windup = (elapsed - WingDashAscendTicks) / (float)WingDashTelegraphTicks;
                const float settleFraction = 0.42f;
                if (windup < settleFraction)
                {
                    float settle = MathHelper.SmoothStep(0f, 1f, windup / settleFraction);
                    return MathHelper.Lerp(MeleeCarryRotation, AxeLowPose, settle);
                }

                float raise = MathHelper.SmoothStep(0f, 1f,
                    (windup - settleFraction) / (1f - settleFraction));
                return MathHelper.Lerp(AxeLowPose, AxeHighPose, raise);
            }
            if (elapsed < swingStart)
            {
                return AxeHighPose;
            }
            if (elapsed < recoveryStart)
            {
                return WingDashSwingCurve.Apply(AxeHighPose, AxeLowPose, elapsed - swingStart);
            }

            int recoveryElapsed = elapsed - recoveryStart;
            if (recoveryElapsed < MeleeRecoveryLingerTicks)
            {
                return AxeLowPose;
            }
            float settleProgress = (recoveryElapsed - MeleeRecoveryLingerTicks)
                / (float)Math.Max(1, WingDashRecoveryTicks - MeleeRecoveryLingerTicks);
            return MathHelper.Lerp(AxeLowPose, MeleeCarryRotation,
                MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(settleProgress, 0f, 1f)));
        }

        private void LockWingDash(Player target)
        {
            const float leadTicks = 8f;
            _wingDashAimPoint = target.Center + target.velocity * leadTicks;
            Vector2 toAim = _wingDashAimPoint - NPC.Center;
            if (toAim.Length() > WingDashMaxDistance)
            {
                toAim = toAim.SafeNormalize(new Vector2(NPC.direction, 0f)) * WingDashMaxDistance;
                _wingDashAimPoint = NPC.Center + toAim;
            }

            _wingDashDirection = toAim.SafeNormalize(new Vector2(NPC.direction, 0f));
            _wingDashEndPoint = ClampWingDashEndpoint(_wingDashAimPoint + _wingDashDirection * WingDashOvershoot);
            _wingDashLocked = true;
            FaceWingDashDirection();
            NPC.netUpdate = true;
        }

        private Vector2 ClampWingDashEndpoint(Vector2 desired)
        {
            Vector2 start = NPC.Center;
            Vector2 delta = desired - start;
            float distance = Math.Min(delta.Length(), WingDashMaxDistance + WingDashOvershoot);
            Vector2 direction = delta.SafeNormalize(_wingDashDirection);
            Vector2 lastOpen = start;

            for (float sampleDistance = 8f; sampleDistance <= distance; sampleDistance += 8f)
            {
                Vector2 candidate = start + direction * sampleDistance;
                if (Collision.SolidCollision(candidate - NPC.Size * 0.5f, NPC.width, NPC.height))
                {
                    break;
                }
                lastOpen = candidate;
            }
            return lastOpen;
        }

        private Vector2 WingDashStagePoint(Player target, float horizontalOffset)
        {
            Vector2 stage = target.Center + new Vector2(horizontalOffset, -WingDashStageHeight);
            float ceiling = FindCeilingY(stage, 18);
            if (ceiling > 0f)
            {
                stage.Y = Math.Max(stage.Y, ceiling + NPC.height * 0.5f + 8f);
            }
            for (int step = 0; step < 8
                && Collision.SolidCollision(stage - NPC.Size * 0.5f, NPC.width, NPC.height); step++)
            {
                stage.Y += 16f;
            }
            return stage;
        }

        private void FaceWingDashDirection()
        {
            if (Math.Abs(_wingDashDirection.X) < 0.05f)
            {
                return;
            }
            int direction = _wingDashDirection.X < 0f ? -1 : 1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        private void TickWingDashBladeAfterPose()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !_wingDashBladeArmed
                || Phase != AttackPhase.Custom || _setPiece != SetPiece.WingedAxeRush)
            {
                return;
            }
            TickBladeHit();
        }

        private void StopWingDashBlade()
        {
            if (!_wingDashBladeArmed)
            {
                return;
            }
            StopMeleeHit();
            _wingDashBladeArmed = false;
        }

        private void TelegraphWingDash(int telegraphElapsed)
        {
            if (Main.dedServ || !_wingDashLocked)
            {
                return;
            }

            if (!_wingDashLockCuePlayed)
            {
                _wingDashLockCuePlayed = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.75f, Pitch = 0.2f }, NPC.Center);
            }

            float progress = MathHelper.Clamp(telegraphElapsed / (float)WingDashTelegraphTicks, 0f, 1f);
            int lineMotes = 2 + (int)(progress * 3f);
            for (int i = 0; i < lineMotes; i++)
            {
                Vector2 point = Vector2.Lerp(NPC.Center, _wingDashEndPoint, Main.rand.NextFloat());
                bool blood = (i + telegraphElapsed) % 3 != 0;
                Dust line = Dust.NewDustPerfect(point, blood ? DustID.Blood : DustID.Wraith,
                    _wingDashDirection * Main.rand.NextFloat(0.2f, 1.2f), blood ? 55 : 145,
                    blood ? Color.DarkRed : Color.Black, Main.rand.NextFloat(0.75f, 1.25f));
                line.noGravity = true;
                line.noLight = !blood;
            }

            Vector2 axeTip = PuppetWeaponTipPosition(AxeComboReach * AxeDrawScale);
            Dust gather = Dust.NewDustPerfect(axeTip + Main.rand.NextVector2Circular(24f, 24f),
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                Main.rand.NextVector2Circular(0.8f, 0.8f), 100, default,
                MathHelper.Lerp(0.7f, 1.35f, progress));
            gather.noGravity = true;
            gather.color = MadnessYellow;
            Lighting.AddLight(axeTip, 0.85f * progress, 0.65f * progress, 0.08f * progress);
            EmitMadnessAxeVFX(MathHelper.Lerp(0.45f, 0.9f, progress), live: false);
        }

        private void EmitWingDashTrail()
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                int dustType = i == 0 ? DustID.Blood : DustID.Wraith;
                Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType,
                    -NPC.velocity.X * 0.25f, -NPC.velocity.Y * 0.25f,
                    dustType == DustID.Wraith ? 150 : 60, default, Main.rand.NextFloat(1.1f, 1.8f));
                trail.noGravity = true;
            }
        }

        private void EmitWingDashSwingDust()
        {
            if (Main.dedServ)
            {
                return;
            }
            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(AxeComboReach * AxeDrawScale);
            for (int i = 0; i < 3; i++)
            {
                Vector2 point = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.35f, 1f));
                Dust slash = Dust.NewDustPerfect(point, i == 0 ? DustID.GoldFlame : DustID.YellowTorch,
                    PuppetWeaponDirection.RotatedBy(MathHelper.PiOver2 * NPC.direction)
                        * Main.rand.NextFloat(0.5f, 2.2f), 35, MadnessYellow,
                    Main.rand.NextFloat(0.8f, 1.35f));
                slash.noGravity = true;
            }
            Lighting.AddLight(tip, 1.0f, 0.78f, 0.1f);
        }

        /// <summary>
        /// Emits the small, blade-attached yellow tell for every axe swing. This is intentionally
        /// client-only: the server owns the hit and buildup while clients decorate the same hand-to-tip
        /// line with light and dust.
        /// </summary>
        private void EmitMadnessAxeVFX(float intensity, bool live)
        {
            if (Main.dedServ || !_axeDrawn)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(AxeComboReach * AxeDrawScale);
            Vector2 bladeDirection = (tip - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            Lighting.AddLight(Vector2.Lerp(hand, tip, 0.72f),
                1.05f * intensity, 0.82f * intensity, 0.12f * intensity);

            // One compact body mote during the tell, two motes while the blade is live. Keeping the
            // scales below 1.2 preserves the chunky Terraria-pixel silhouette instead of making a fog.
            int count = live ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.25f, 1f));
                Vector2 velocity = bladeDirection.RotatedBy(MathHelper.PiOver2 * NPC.direction)
                    * Main.rand.NextFloat(0.35f, live ? 1.7f : 0.9f);
                Dust dust = Dust.NewDustPerfect(point,
                    i == 0 ? DustID.GoldFlame : DustID.YellowTorch,
                    velocity, 25, MadnessYellow,
                    Main.rand.NextFloat(0.55f, live ? 1.1f : 0.85f));
                dust.noGravity = true;
            }
        }

        private void EndFlightSetPiece()
        {
            _setPiece = SetPiece.None;
            NPC.noGravity = false;
            Flight?.RequestLand();
        }

        private void FacePlayer(Player target)
        {
            int faceDirection = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = faceDirection;
            NPC.spriteDirection = faceDirection;
        }

        private void EmitFlightDust()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }

            Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith,
                0f, 0f, 140, Color.Black, 1.8f);
            trail.noGravity = true;
            trail.velocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.3f, 1.4f));
        }

        // World Y of the first solid tile bottom above the point. The Locust pass uses this to stay under
        // low arena ceilings instead of forcing its nine-tile default altitude through terrain.
        private static float FindCeilingY(Vector2 worldPos, int maxTilesUp)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }

            for (int distance = 2; distance <= maxTilesUp; distance++)
            {
                int y = tileY - distance;
                if (y <= 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return (y + 1) * 16f;
                }
            }
            return -1f;
        }

        // Scans down for the first solid tile so a rain strike marks the floor rather than mid-air.
        private static Vector2 FindGroundBelow(Vector2 from)
        {
            int tileX = (int)(from.X / 16f);
            int startY = (int)(from.Y / 16f);
            for (int tileY = startY; tileY < startY + 60 && tileY < Main.maxTilesY - 10; tileY++)
            {
                if (tileX < 0 || tileX >= Main.maxTilesX)
                {
                    break;
                }
                if (Main.tile[tileX, tileY].HasTile && Main.tileSolid[Main.tile[tileX, tileY].TileType])
                {
                    return new Vector2(from.X, tileY * 16f);
                }
            }
            return new Vector2(from.X, from.Y + 400f);
        }
        #endregion

        #region Melee hooks
        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (step.DamageMult <= 0f)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.45f, PitchVariance = 0.1f }, NPC.Center);
            }
            base.DoComboMeleeHit(step);
        }

        protected override void OnBladeHit(Player player)
        {
            base.OnBladeHit(player);
            MadnessBuildup.Apply(player, MeleeMadnessBuildup, MeleeMadnessWindowTicks);
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _weaveCastTimer = -1;
            SetAttackLabel(combo.Name, 120);

            if (combo.Name == DarkJudgmentName && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = -0.5f }, NPC.Center);
            }
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);
            float progress = MathHelper.Clamp(elapsed / (float)Math.Max(1, total), 0f, 1f);
            EmitMadnessAxeVFX(MathHelper.Lerp(0.35f, 0.75f, progress), live: false);

            if (combo.Name != DestinedAxeCastName || Main.dedServ)
            {
                return;
            }

            Vector2 axeTip = PuppetWeaponTipPosition(AxeComboReach * AxeDrawScale);
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float radius = MathHelper.Lerp(42f, 5f, progress);
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                bool blood = i % 3 != 0;
                Dust gather = Dust.NewDustPerfect(axeTip + offset,
                    blood ? DustID.Blood : DustID.Wraith,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.4f, 5.2f, progress),
                    blood ? 65 : 150, default,
                    blood ? Main.rand.NextFloat(0.6f, 1.05f) : Main.rand.NextFloat(0.45f, 0.8f));
                gather.noGravity = true;
                gather.noLight = !blood;
            }
            Lighting.AddLight(axeTip, 0.48f * progress, 0.01f, 0.02f);
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);
            if (step.DamageMult <= 0f)
            {
                return;
            }

            float progress = MathHelper.Clamp(elapsed / (float)Math.Max(1, total), 0f, 1f);
            if (progress <= step.HitWindowEnd)
            {
                EmitMadnessAxeVFX(1f, live: true);
            }
        }

        // Dark Judgment swings wide and slow enough that it must not open from point-blank, where its
        // 46-tick tell is off-screen behind the boss's own body.
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == DarkJudgmentName)
            {
                return distance >= 60f;
            }
            if (combo.Name == DestinedAxeCastName)
            {
                // Keep the cast distinct from the close kit and inside the proven ballistic envelope.
                return distance >= 160f && distance <= RangedStartComboMaxRange;
            }
            return true;
        }

        protected override void DoMeleeAttack()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.45f }, NPC.Center);
            }
            TryMeleeHit();
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            base.OnRangedBurstStarted(secondary);
            SetAttackLabel("Destined Death Flask",
                RangedTelegraphTicks + RangedAttackTicks + RangedRecoveryTicks);
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float radius = MathHelper.Lerp(54f, 7f, progress);
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                bool blood = i % 3 != 0;
                Dust gather = Dust.NewDustPerfect(hand + offset, blood ? DustID.Blood : DustID.Wraith,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 5.5f, progress),
                    blood ? 70 : 150, default, blood ? Main.rand.NextFloat(0.65f, 1.05f)
                        : Main.rand.NextFloat(0.5f, 0.85f));
                gather.noGravity = true;
                gather.noLight = !blood;
            }
            Lighting.AddLight(hand, 0.45f * progress, 0.015f, 0.025f);
        }

        protected override void DoRangedAttack()
        {
            PlayThrowSound();
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = Main.dedServ
                ? NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f)
                : PuppetHandPosition;
            const float flaskSpeed = 13f;
            float leadTicks = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / flaskSpeed, 8f, 32f);
            Vector2 predictedFeet = target.Bottom + target.velocity * leadTicks;
            Vector2 groundTarget = FindGroundBelow(predictedFeet - new Vector2(0f, 96f));
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(origin, groundTarget, flaskSpeed,
                0.18f, highAngle: false, fallback: true);

            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<OccultistDestinedDeathFlask>(), RangedDamage, 3f, Main.myPlayer);
        }
        #endregion

        #region Ambience, distance punishment and draw
        private void SpawnAmbientEffects()
        {
            TickWeaveCast();

            if (Main.dedServ)
            {
                return;
            }

            // The original boss's identity smoke: black wraith dust boiling off the robe, thicker below a
            // quarter health.
            bool nearDeath = NPC.life <= NPC.lifeMax / 4;
            int interval = nearDeath ? 2 : 4;
            if (Main.GameUpdateCount % (uint)interval == 0)
            {
                Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith,
                    NPC.velocity.X, NPC.velocity.Y, nearDeath ? 140 : 210, Color.Black, nearDeath ? 3f : 2f);
                smoke.noGravity = true;
            }

            // Readability light. The puppet body, its armor and the held weapon all sample the world light
            // map, so in an unlit night arena the whole silhouette goes dark — the same problem Artorias
            // solves with a flat white light. White, not red: a red light on dark-red robes is exactly the
            // contrast that made the boss hard to pick out in the first place.
            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1.15f);
            // A small red bleed a bit further out keeps the abyss colour in the scene without tinting the
            // body itself.
            Lighting.AddLight(NPC.Center + new Vector2(0f, 24f), 0.35f, 0.05f, 0.1f);
        }

        // Running away is not a strategy: past 3000px the boss's bolts fly at double speed and the runaway
        // bleeds 5 HP a second until they come back.
        private float AbandonSpeedMultiplier()
        {
            if (!NPC.HasValidTarget || NPC.Distance(Main.player[NPC.target].Center) <= AbandonDistance)
            {
                return 1f;
            }
            return 2f;
        }

        private void PunishAbandonment()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (NPC.Distance(target.Center) <= AbandonDistance)
            {
                _abandonDrainTimer = 0;
                return;
            }

            _abandonDrainTimer++;
            if (_abandonDrainTimer < AbandonDrainInterval)
            {
                return;
            }

            _abandonDrainTimer = 0;
            target.statLife -= AbandonDrainAmount;
            if (target.statLife <= 0)
            {
                target.KillMe(PlayerDeathReason.ByCustomReason(
                    LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.DespawnHandler")), 10, 0);
            }
        }

        /// <summary>
        /// OFF. The CatAura halo blends ADDITIVELY over a 40x56 puppet, which washed the body out badly
        /// enough to read as transparent — an always-on aura is simply too much light for a sprite this
        /// small. The effect itself is kept and still works; the plan is to re-enable it per-attack as a
        /// TELL (most likely for the casts that apply a player debuff) rather than as a permanent halo.
        /// Flip this to true to see it again.
        /// </summary>
        private const bool AbyssAuraEnabled = false;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Drawn before base.PreDraw so the halo sits BEHIND the body whenever it is switched back on.
            if (AbyssAuraEnabled)
            {
                DrawAbyssAura();
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        // The kept CatAura halo. Intensity tracks health, and spikes while a spell is winding up so the
        // boss visibly gathers power behind every cast. Gated by AbyssAuraEnabled — see above.
        private void DrawAbyssAura()
        {
            if (Main.dedServ)
            {
                return;
            }

            float castSwell = Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.Custom ? 1f : 0f;
            _auraBonus *= 0.9f;
            _auraBonus += 0.1f + castSwell * 0.12f;

            _auraEffect ??= ModContent
                .Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Effect effect = _auraEffect;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float healthFraction = MathHelper.Clamp(NPC.life / (float)NPC.lifeMax, 0f, 1f);
            float colorIntensity = 0.3f + _auraBonus * 0.2f;
            int auraSize = 165 + (int)((1f - healthFraction) * 45f);

            Rectangle sourceRectangle = new Rectangle(0, 0, auraSize, auraSize);
            Vector2 origin = sourceRectangle.Size() / 2f;

            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseVoronoi.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(new Color(130, 8, 30).ToVector4() * colorIntensity * 1.2f);
            effect.Parameters["ringProgress"].SetValue(0.6f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.2f);
            effect.Parameters["scaleFactor"].SetValue(3.5f);
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, NPC.Center - Main.screenPosition, sourceRectangle,
                Color.White, 0f, origin, NPC.scale * 1.1f, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }
        #endregion

        #region Death, loot and sync
        // The desperation sequence: at 0 HP the boss refuses to die for 15 seconds and spends them on a
        // bullet-hell final stand. dontTakeDamage is deliberate — it is a scripted death, not a heal.
        public override bool CheckDead()
        {
            if (_desperationComplete)
            {
                return true;
            }

            NPC.life = 1;
            NPC.dontTakeDamage = true;

            if (_desperationStarted)
            {
                return false;
            }

            _desperationStarted = true;
            _setPiece = SetPiece.Desperation;
            SetAttackLabel("The Abyss Consumes", DesperationTotalTicks);
            StartCustomAttack(DesperationTotalTicks, ModContent.ItemType<EnemyOccultistStaff>());
            NPC.netUpdate = true;
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<OolacileSorcererBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 2, 4));

            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HealingElixir>(), 1, 5, 10));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 5, 8));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfOccultist>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
        }

        public override void OnKill()
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.Defeated"), 160, 160, 160);
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1.1f });

            // The Abyss curse was cast for this fight; killing its author lifts it. Abyss.cs otherwise only
            // clears via the Covenant of Artorias ring, so this is new behaviour, not a preservation.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && player.HasBuff(ModContent.BuffType<Abyss>()))
                {
                    player.ClearBuff(ModContent.BuffType<Abyss>());
                }
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    int goreIndex = 1 + i % 3;
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity,
                        Mod.Find<ModGore>("Oolacile Sorcerer Gore " + goreIndex).Type, 1.35f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<BossDeath>(), 0, 0, Main.myPlayer, 1,
                    UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_spell);
            writer.Write(_starCascadeRings);
            writer.Write(_starCascadeInterval);
            writer.Write((byte)_setPiece);
            writer.Write(_phase2Triggered);
            writer.Write(_axeDrawn);
            writer.Write(_surgeDone);
            writer.Write(_desperationStarted);
            writer.Write(_desperationComplete);
            writer.Write((sbyte)_strafeDir);
            writer.Write((sbyte)_locustPassDirection);
            writer.Write(_locustGroupId);
            writer.Write((short)Math.Clamp(_seekerOrbIndex, -1, short.MaxValue));
            writer.Write((short)Math.Clamp(_strafeCooldown, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(_locustSwarmCooldown, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(_rainCooldown, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(_wingDashCooldown, 0, short.MaxValue));
            writer.Write(_wingDashLocked);
            writer.WriteVector2(_wingDashAimPoint);
            writer.WriteVector2(_wingDashEndPoint);
            writer.WriteVector2(_wingDashDirection);
            for (int i = 0; i < SpellCount; i++)
            {
                writer.Write((short)Math.Clamp(_spellCooldowns[i], 0, short.MaxValue));
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _spell = (Spell)reader.ReadByte();
            _starCascadeRings = reader.ReadByte();
            _starCascadeInterval = reader.ReadByte();
            _setPiece = (SetPiece)reader.ReadByte();
            _phase2Triggered = reader.ReadBoolean();
            _axeDrawn = reader.ReadBoolean();
            _surgeDone = reader.ReadBoolean();
            _desperationStarted = reader.ReadBoolean();
            _desperationComplete = reader.ReadBoolean();
            _strafeDir = reader.ReadSByte();
            _locustPassDirection = reader.ReadSByte();
            _locustGroupId = reader.ReadInt32();
            _seekerOrbIndex = reader.ReadInt16();
            _strafeCooldown = reader.ReadInt16();
            _locustSwarmCooldown = reader.ReadInt16();
            _rainCooldown = reader.ReadInt16();
            _wingDashCooldown = reader.ReadInt16();
            _wingDashLocked = reader.ReadBoolean();
            _wingDashAimPoint = reader.ReadVector2();
            _wingDashEndPoint = reader.ReadVector2();
            _wingDashDirection = reader.ReadVector2();
            for (int i = 0; i < SpellCount; i++)
            {
                _spellCooldowns[i] = reader.ReadInt16();
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<BossDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }
        }
        #endregion
    }
}
