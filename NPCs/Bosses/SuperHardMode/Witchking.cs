using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Accessories.Defensive;
using tsorcRevamp.Content.Items.Accessories.Defensive.CovenantOfArtorias;
using tsorcRevamp.Content.Items.Armor.Summon.WitchkingSet;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Items.Weapons.Summon.Whips;
using tsorcRevamp.Content.Projectiles.Enemy.Witchking;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    /// <summary>
    /// The Witchking as a PuppetNPC: the Witchking
    /// armour set, the Witchking's Sword in BOTH hands — fire blade in front, void blade behind.
    ///   Phase 1 (100–50%): alternating fire/void strings, the simultaneous two-sword moves (ComboBackArc: Witch's Cross,
    ///     Scissor Cut, Echo Cut), Fell Leap, Backstep Re-entry; ranged: Void Flame and Hellfire Plunge (fire walls).
    ///   50%: "The Black Captain's Mace" — a shriek, the fire blade gutters out, the mace whip is drawn, a Barrow Wight
    ///     Phantom rises.
    ///   Phase 2 (50–0%): mace whip (front) + void sword (back) in one-then-the-other cadence, Morgul Lash, Void Flame
    ///     ×3, Hellfire Plunge ×3 walls (the fire sword re-forms for it), Grond's Toll, more phantoms.
    ///   Death: "The Unmaking" — arms spread, 3s of black dust, fifty skulls.
    /// Kept from the legacy boss: the DispelShadow damage gate + 10% last stand, the HP-gated scream + pull (rebuilt as
    /// the telegraphed Nazgûl Shriek), the Abyss curse (a 1s refresh while he lives) and the anti-flight debuffs.
    /// Set-pieces run on AttackPhase.Custom, started only by the server and pushed with a phase snapshot.
    /// </summary>
    [AutoloadBossHead]
    class Witchking : PuppetNPC, IHumanoidMeleeHitEffects, IEnemyWhipOwner
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(Witchking)) + "_Head_Boss";

        NPCDespawnHandler despawnHandler;

        #region Tuning constants
        // Declared Expert Mode damage before player defense (EnemyDamage.Projectile). ESTIMATES pending the bands doc.
        public const int WitchCutDamage = 190;
        public const int VoidFlameDamage = 150;
        public const int HellfireWallDamage = 220;
        public const int MaceLashDamage = 200;
        public const int GrondHeadDamage = 250;
        public const int GrondWaveDamage = 170;
        public const int DeathSkullDamage = 250;

        // On-hit debuffs. Fire: pink burst + Miasma buildup + On Fire. Void: Weight of Shadow +
        // Withered Armor + Withered Weapon. Abyss is NOT on hit: the fight-long curse already covers it.
        public const int FireOnFireTicks = 6 * 60;
        public const int VoidDebuffTicks = 8 * 60;

        // Witchking's Sword is 100x100: pommel at the bottom-left corner, grip texels ~4-10 along the diagonal, guard
        // ~12-22, tip at (99,0). Grip (7,92); at 0.8 the grip-to-tip reach is (99-7)·√2·0.8 ≈ 104px. Hitbox =
        // ComboReachBase 140 × 0.7 = 98px, inside 10% of the visible blade.
        private const float SwordDrawScale = 0.8f;
        private static readonly Vector2 SwordHandleNorm = new Vector2(0.07f, 0.92f);
        private const float SwordBladeWidth = 30f;
        private const float SwordReachBase = 140f;
        private const float SwordTipReach = 100f;
        // Hellfire Plunge holds the fire sword in a reverse grip: the blade leaves the bottom of the fist, 90° clockwise
        // from the forearm, so with the arm out level the blade points straight down into the floor.
        private const float ReverseGripOffset = MathHelper.PiOver2;
        // WitchkingMace.png (48x54, the coiled whip icon) held at its handle.
        private static readonly Vector2 MaceHandleNorm = new Vector2(0.2f, 0.8f);

        // Arc poses (attack-timing-design §3): 224° envelope ≈ 180° live. Overhead and underhand share endpoints so
        // alternating chains never snap. Witch's Cross is the signature heavy at a wider 235° envelope.
        private const float CutHighPose = -1.55f;
        private const float CutLowPose = 2.36f;
        private const float CrossHighPose = -1.62f;
        private const float CrossLowPose = 2.45f;

        // Witch Cut clock: in 8 / out 32 / k 6 → peak 224/(8/3 + 32·(1-e^-6)/6) = 28.0°/t, live 8 + 32·1.204/6 = 14.4t
        // of 40 (< 22: rollable). With a 6t pause the next step goes live 36t after this one's live window ends (≥ 30).
        private const int CutEaseIn = 8;
        private const int CutEaseOut = 32;
        private const float CutDecay = 6f;
        private const int StringPause = 6;
        private const float StringDamageMult = 0.85f;

        // Opening tell of the light cuts. The logical windup dips to the arc's far end then raises the full arc back to
        // its start, so it must stay slower than the strike: at 30t with 40% spent settling, the tell peaks under
        // ~19°/t against the strike's ~26°/t (SwingPreview --profile). 24t made the tell the fastest beat (37°/t).
        private const int LightTellTicks = 30;
        private const float WindupSettleFraction = 0.4f;

        // Witch's Cross clock: in 10 / out 45 / k 7 → peak 24.1°/t, live 17.7t of 55.
        private const int CrossEaseIn = 10;
        private const int CrossEaseOut = 45;
        private const float CrossDecay = 7f;

        // Echo Cut: the void blade trails the fire blade by this many ticks on the same arc. Union live window
        // 4.4-20.4 = 16t, still < 22, so one roll beats both images.
        private const int EchoDelayTicks = 6;

        // Fell Leap: 6–14 tile gap-closer. Low arc (0.55 height) for the cramped cave ceiling; aims 24px past the
        // player so an on-time leap lands through them, not short. Swings in the air once past the apex within 120px.
        private const float FellLeapMinRange = 96f;
        private const float FellLeapMaxRange = 230f;
        private const float FellLeapStrikeRange = 120f;
        private const float FellLeapHeightMult = 0.55f;
        private const float FellLeapOvershoot = 24f;
        // Backstep Re-entry unlocks once the player has hugged him (inside 3 tiles) for 1.5s.
        private const float HugDistance = 48f;
        private const int HugTicksToUnlock = 90;

        // Void Flame: 11–40 tiles, line of sight. One blast in phase 1, a 40° fan of three in phase 2.
        private const float VoidFlameMinRange = 176f;
        private const float VoidFlameMaxRange = 640f;
        private const float VoidFlameFanDegrees = 40f;

        // Whip timing (the shared enemy whip): opening tell 40t, a mid-combo tell is the preceding pause (20t). The
        // lash is live on lash ticks 8–19 (12t, rollable).
        //   Sword → lash: the cut's live window ends at 14.4; 25.6 tail + 20 pause + 8 = 54t to the lash going live.
        //   Lash → sword: the lash ends live at 19; 11 rest + 10 pause + 4.4 = 25t < 30, so that cut is CONDITIONAL
        //     (player still in front and in reach, checked before the pause re-faces).
        //   Morgul Lash: the cut follows only a latch, after a 26t pause, going live ≥ 41t after the latch hit so post-hit
        //     immunity (40t) can't eat it; the 30t tether drags the player in during that pause.
        private const int MidComboLashTell = 20; // 14 made the shared clock sweep 170° at 37°/t, faster than a cut
        private const int LashAttackTicks = 30;
        private const int MorgulPause = 26;
        // Lash → cut pause: long enough that the arm swings from the lash finish to the low cut start (78°) slower
        // than the cut itself.
        private const int LashToCutPause = 10;
        // The lash arm: raised (-0.6) flung through to low-forward (1.0); the whip projectile owns the real
        // motion, this only has to read as a throw and sit near the neighbouring cuts' poses.
        private const float LashStartPose = -0.6f; // < π from the cut poses (2.36 / -1.55), so hand-offs never wrap
        private const float LashEndPose = 1.0f;

        // Hellfire Plunge. Raise 30 → plunge 10 → 90t of flame building from the buried blade → the
        // walls erupt (phase 2: two more, 90t apart) → hold 20 → pull out 20.
        private const int PlungeRaiseTicks = 30;
        private const int PlungeStrikeTicks = 10;
        private const int PlungeImpactTick = PlungeRaiseTicks + PlungeStrikeTicks;
        private const int PlungeWallIntervalTicks = 90;
        private const int PlungeHoldTicks = 20;
        private const int PlungePullTicks = 20;
        private const float PlungeMinRange = 96f;
        private const int PlungeCooldownTicks = 12 * 60;
        private const float ArmLevelForward = MathHelper.PiOver4; // swing space: world angle = rotation − 45°
        private const float ArmLevelBackward = MathHelper.PiOver4 - MathHelper.Pi;

        // Grond's Toll (phase 2 signature, HP ≤ 40%): the whirl and slam live in WitchkingGrondMace; these are the
        // owner's pose beats, which must match it.
        private const float GrondHealthGate = 0.40f;
        private const int GrondCooldownTicks = 20 * 60;

        // Barrow Summons: back sword raised 60t over a void-smoke column, the phantom rises at t60. One alive, two
        // below 25%; the next summons waits 30s after the last phantom dies.
        private const int SummonRaiseTicks = 60;
        private const int SummonTotalTicks = 90;
        private const int SummonCooldownTicks = 30 * 60;
        private const float SummonMinPlayerDistance = 8f * 16f;
        private const float SecondPhantomHealthGate = 0.25f;

        // The Black Captain's Mace (50%): shriek release at 40, fire gutters 40–90, mace drawn at 90, phantom at 120.
        private const int TransitionMaceTick = 90;
        private const int TransitionSummonTick = 120;
        private const int TransitionTotalTicks = 150;

        // The Unmaking: 180t of black dust, then fifty skulls and death on the same tick.
        private const int UnmakingBuildTicks = 180;
        private const int UnmakingSlackTicks = 60;
        private const int DeathSkullCount = 50;

        // Nazgûl Shriek. HP fractions, in order; index 1 (50%) is the phase transition, the last one is
        // the 10% last stand.
        private static readonly float[] ShriekThresholds = { 0.75f, 0.50f, 0.25f, 0.10f };
        private const int TransitionShriekIndex = 1;
        private const float LastStandFraction = 0.10f;
        private const int ShriekTellTicks = 40;
        private const int ShriekHoldTicks = 30;
        private const int PullTicks = 8 * 60;
        private const float PullRadius = 60f * 16f;
        private const float WeaponsRaisedStraightUp = -MathHelper.PiOver4;

        // Abyss fight curse: refreshed every tick for each client's own player within this range.
        private const float AbyssCurseRadius = 150f * 16f;
        private const int AbyssRefreshTicks = 60;
        // Anti-flight near him, GRK's pattern on the targeted player's own machine.
        private const float AntiFlightRadius = 700f;

        private const int ImmuneTextIntervalTicks = 30;

        private static readonly Color FireColor = new Color(255, 120, 40);
        private static readonly Color VoidColor = new Color(150, 70, 220);

        private const string WitchCutName = "Witch Cut";
        private const string EmberAndAshName = "Ember and Ash";
        private const string AshAndEmberName = "Ash and Ember";
        private const string TwoFiresOneAshName = "Two Fires, One Ash";
        private const string TwoAshesOneFireName = "Two Ashes, One Fire";
        private const string FigureEightName = "Advancing Figure Eight";
        private const string WitchsCrossName = "Witch's Cross";
        private const string ScissorCutName = "Scissor Cut";
        private const string EchoCutName = "Echo Cut";
        private const string EmberAshScissorName = "Ember, Ash, Scissor";
        private const string FellLeapName = "Fell Leap";
        private const string BackstepReentryName = "Backstep Re-entry";
        private const string VoidFlameName = "Void Flame";
        private const string MaceLashName = "Mace Lash";
        private const string CleaveAndLashName = "Cleave and Lash";
        private const string LashAndCleaveName = "Lash and Cleave";
        private const string AshAshMaceName = "Ash, Ash, Mace";
        private const string MorgulLashName = "Morgul Lash";
        private const string VoidCutName = "Void Cut";
        private const string VoidFellLeapName = "Fell Leap (Void)";
        #endregion

        private enum CustomKind : byte { None, Shriek, Transition, BarrowSummons, HellfirePlunge, GrondsToll, Unmaking }

        // Shield (DispelShadow gate). Both synced both ways — the legacy boss only ever sent `true`, so MP clients kept
        // the shield down through the last stand and dealt full damage.
        private bool _defenseBroken;
        private bool _lastStand;
        private bool _phaseTwo;

        // Set-pieces. Kind, total length and the wall count are synced; elapsed is derived from the phase timer.
        private CustomKind _customKind = CustomKind.None;
        private int _customTotalTicks;
        private int _customElapsed;
        private int _plungeWallCount;
        private Vector2 _summonPoint;
        private bool _unmakingStarted;
        private bool _unmakingComplete;

        // Shriek: _shrieksQueued counts thresholds crossed, _shrieksStarted how many have played. Server decides.
        private int _shrieksQueued;
        private int _shrieksStarted;
        private int _pullTicksRemaining;
        private bool _pullPermanent;

        // Server-only scheduling.
        private int _plungeCooldown = PlungeCooldownTicks / 2;
        private int _grondCooldown = GrondCooldownTicks / 2;
        private int _summonCooldown = SummonCooldownTicks;
        private int _huggedTicks;
        private bool _nextWitchCutUsesBackHand;

        // Whip: the aim each peer tracks (DBK pattern: tracks through the tell, locks for its last 10 ticks; the
        // projectile syncs its velocity at the lock). _whipComboActive: the running combo contains a lash.
        private Vector2 _lockedWhipAim = Vector2.UnitX;
        private bool _whipComboActive;
        private MeleeCombo _runningCombo;
        private int _comboStepsCompleted;
        private bool _leapImpactSpawned;

        // Local per machine: throttles the "Immune" combat text; a set-piece just ended and left its pose item in hand.
        private int _immuneTextCooldown;
        private bool _displayResetPending;

        #region Puppet loadout
        // An invader boss, like Gwyn and Artorias: "Invaded by Witchking" on spawn (the PuppetNPC default). His defeat
        // banner comes from his registered boss encounter.
        protected override string InvaderTitle => "Witchking";
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override Color PuppetSkinColor => Color.Black;

        protected override int HeadArmorItemType => ModContent.ItemType<WitchkingHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<WitchkingRobe>();
        protected override int LegsArmorItemType => ModContent.ItemType<WitchkingPants>();

        // The shield tell (replaces the old LivingOceanDye redraw): a shimmering dark dye while the DispelShadow shield
        // holds, undyed once it's broken. PuppetNPC re-applies dyes when these change.
        private int ShieldDye => _defenseBroken ? 0 : ItemID.ShadowflameHadesDye;
        protected override int HeadArmorDyeItemType => ShieldDye;
        protected override int BodyArmorDyeItemType => ShieldDye;
        protected override int LegsArmorDyeItemType => ShieldDye;

        protected override int MeleeWeaponItemType => _phaseTwo ? ModContent.ItemType<WitchkingMace>() : ModContent.ItemType<WitchkingsSword>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(WitchCutDamage);
        protected override int RangedDamage => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => SwordHandleNorm;
        protected override float MeleeWeaponDrawScale => SwordDrawScale;
        protected override float MeleeBladeWidth => SwordBladeWidth;

        protected override float TopSpeed => _phaseTwo ? 2.3f : 2.0f;
        protected override float Acceleration => 0.06f;

        // Weapon descriptors (immutable; PuppetWeapon is shared by reference, so these are compared to the equipped one).
        private PuppetWeapon _fireSword;
        private PuppetWeapon _voidSword;
        private PuppetWeapon _reverseFireSword;
        private PuppetWeapon _maceCoiled;
        private PuppetWeapon _maceLashing;

        private PuppetWeapon FireSword => _fireSword ??= CreateSword(0f);
        private PuppetWeapon VoidSword => _voidSword ??= CreateSword(0f);
        private PuppetWeapon ReverseFireSword => _reverseFireSword ??= CreateSword(ReverseGripOffset);
        private PuppetWeapon MaceCoiled => _maceCoiled ??= CreateMace(hidden: false);
        private PuppetWeapon MaceLashing => _maceLashing ??= CreateMace(hidden: true);

        private static PuppetWeapon CreateSword(float rotationOffset)
        {
            return new PuppetWeapon(
                ModContent.ItemType<WitchkingsSword>(),
                handleNorm: SwordHandleNorm,
                drawScale: SwordDrawScale,
                bladeWidth: SwordBladeWidth,
                archetype: WeaponArchetype.Broadsword,
                rotationOffset: rotationOffset);
        }

        // The mace's real visual while lashing is the whip projectile, so its icon hides then.
        private static PuppetWeapon CreateMace(bool hidden)
        {
            return new PuppetWeapon(
                ModContent.ItemType<WitchkingMace>(),
                handleNorm: MaceHandleNorm,
                drawScale: 1f,
                archetype: WeaponArchetype.Whip,
                hideHeldSprite: hidden);
        }
        #endregion

        #region Melee combos
        // No legacy slash path: the combo pool is the only melee.
        protected override float MeleeRange => 0f;
        protected override float ComboReachBase => SwordReachBase;
        protected override float MeleeEngageRange => 110f;
        protected override float ComboMaxStartRange => 200f;
        protected override float RangedStartComboMaxRange => VoidFlameMaxRange;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int MeleeComboInterStepLingerTicks => 3;
        // A hand joining the swing crossfades from its carry pose over 6 ticks (base 4): the void hand going from
        // the carry to the low underhand start covers ~150°, which at 4 ticks outran the strike. Every pause that
        // switches hands is ≥ 6.
        protected override int DualWieldHandBlendTicks => 6;
        protected override bool SlowDownBeforeMelee => false;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override float LogicalWindupSettleFraction => WindupSettleFraction;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        // Slashes are per-hand VanillaSwordArcs spawned in OnMeleeComboAttackTick, not the legacy Slash.png sprite.
        protected override bool HasSlashVFX => false;

        // The Custom set-pieces pose the composite arms (raised, plunged, spread wide).
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        // A twin-blade knight is never seen unarmed: both weapons stay drawn while he walks.
        protected override bool ShowWeaponDuringNeutral => true;

        private bool InComboPose =>
            Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause
            || Phase == AttackPhase.MeleeComboRecovery;

        // Fell Leap's landing-timed leap and its poses, scoped to the leap (ActiveMeleeComboName is never cleared).
        private bool FellLeapActive =>
            (ActiveMeleeComboName == FellLeapName || ActiveMeleeComboName == VoidFellLeapName)
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        protected override bool UseLandingTimedLeapSlam => FellLeapActive;
        protected override float LeapAttackTargetLeadTicks => 12f;
        protected override float LeapAttackAscentTrackingStrength => 0.12f;
        protected override float LeapSlamCarryRotation => FellLeapActive ? CutHighPose : base.LeapSlamCarryRotation;
        protected override float LeapSlamImpactRotation => FellLeapActive ? CutLowPose : base.LeapSlamImpactRotation;
        // Negative standoff = aim past the player, so an on-time leap lands through them instead of short.
        protected override float LeapLandingStandoff => FellLeapActive ? -FellLeapOvershoot : 0f;

        // One Weighted step. Armed while angular speed >= 30% of peak: ease-in + out·ln(1/0.3)/k ticks.
        private static MeleeComboStep WitchStep(
            ComboMotion motion,
            ComboHand hand,
            int telegraphTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            int pauseAfter = 0,
            float damageMult = 1f,
            ComboBackArc backArc = ComboBackArc.Mirror,
            int backDelayTicks = 0,
            float forwardPushMult = 0f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float liveTailTicks = easeOutTicks * MathF.Log(1f / 0.3f) / easeOutDecay;
            float hitWindowEnd = MathHelper.Clamp((easeInTicks + liveTailTicks) / attackTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                Hand = hand,
                BackArc = backArc,
                BackDelayTicks = backDelayTicks,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = 1f,
                ForwardPushMult = forwardPushMult,
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

        // The light cut used by every string: Witch Cut clock, string spacing.
        private static MeleeComboStep Cut(ComboMotion motion, ComboHand hand, int telegraphTicks, int pauseAfter = 0, float damageMult = 1f)
        {
            return WitchStep(motion, hand, telegraphTicks, CutEaseIn, CutEaseOut, CutDecay, pauseAfter, damageMult);
        }

        // A mace lash: the front arm sweeps flat on the Whip ease while the whip projectile owns the hit (DamageMult 0
        // never arms the blade). Its tell is the projectile's: the step's telegraph, or the preceding pause.
        private static MeleeComboStep Lash(int telegraphTicks, int pauseAfter = 0)
        {
            return new MeleeComboStep
            {
                Motion = ComboMotion.HorizontalSweep,
                Hand = ComboHand.Front,
                TelegraphTicks = telegraphTicks,
                AttackTicks = LashAttackTicks,
                PostStepPause = pauseAfter,
                DamageMult = 0f,
                ReachMult = 1f,
                ForwardPushMult = 0f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Whip,
                HitWindowEnd = 0f,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        private static bool IsLashStep(MeleeComboStep step) => step.Motion == ComboMotion.HorizontalSweep && step.Ease == SwingEaseStyle.Whip;

        private static MeleeComboStep LeapStep(ComboHand hand)
        {
            return new MeleeComboStep
            {
                Motion = ComboMotion.LeapSlam,
                Hand = hand,
                TelegraphTicks = LightTellTicks,
                AttackTicks = 90, // safety net; the landing-timed leap ends on touchdown
                PostStepPause = 0,
                DamageMult = 1.15f,
                ReachMult = 1f,
                ForwardPushMult = 0f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
                HitWindowEnd = 1f,
                LeapStrikeRange = FellLeapStrikeRange,
                LeapHeightMult = FellLeapHeightMult,
                LeapForwardSpeedMult = 1f,
            };
        }

        // Phase-1 pool. Timing sheet (60t = 1s; player: 22t roll i-frames, next roll 30t after the last
        // STARTED, 40t post-hit immunity):
        //
        // Move                 Tell  Strike (in/out, k)   Live         Notes
        // Witch Cut             30   8 / 32, k6           14.4t        alternates fire/void each use
        // Ember and Ash et al.  30   8 / 32, k6, pause 6  14.4t each   next live 36t after this live ends (≥30 ✓);
        //                                                              peak-to-peak 46t (≥40) so both can land → ×0.85
        // 3-step strings        30   same                 14.4t each   step 3 only after a hit or with the player still
        //                                                              in front and in reach (stops whiff chains)
        // Figure Eight          30   same ×4              14.4t each   steps 3–4 conditional the same way; ≤4 rolls
        // Witch's Cross         44   10 / 45, k7, Both    17.7t        HyperArmor; 37t tail + 60t recovery punish
        // Scissor Cut           36   8 / 32, k6, Reverse  14.4t        fire falls while void rises; blades cross at
        //                                                              the peak; one hit max (shared hit list)
        // Echo Cut              30   8 / 32, k6, Delay 6  4.4–20.4t    one rapid double image; one roll beats it
        // Fell Leap             30   landing-timed leap   swing        6–14 tiles, aimed 24px past; roll away mid-air
        // Backstep Re-entry     30   hop back + Both cut  14.4t        only after 1.5s of hugging; HyperArmor
        // Void Flame            30   back rising cut      flame        11–40 tiles + LOS; flame forms at the void tip
        private static readonly MeleeCombo[] PhaseOneCombos =
        {
            new MeleeCombo
            {
                Name = WitchCutName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = FireColor,
                CooldownAfterUse = 60,
                RecoveryTicks = 25,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, LightTellTicks),
                },
            },
            new MeleeCombo
            {
                Name = EmberAndAshName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = FireColor,
                CooldownAfterUse = 120,
                RecoveryTicks = 30,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = AshAndEmberName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 120,
                RecoveryTicks = 30,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Back, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Front, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = TwoFiresOneAshName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = FireColor,
                CooldownAfterUse = 180,
                RecoveryTicks = 36,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Front, 0, StringPause, StringDamageMult),
                    Cut(ComboMotion.OverheadArc, ComboHand.Back, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = TwoAshesOneFireName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 180,
                RecoveryTicks = 36,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Back, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, StringPause, StringDamageMult),
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = FigureEightName,
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 70, 30),
                CooldownAfterUse = 300,
                RecoveryTicks = 45,
                HeavyCommit = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, StringPause, StringDamageMult),
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, 0, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = WitchsCrossName,
                BaseWeight = 50,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 40, 40),
                CooldownAfterUse = 300,
                RecoveryTicks = 60,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.4f,
                Steps = new[]
                {
                    WitchStep(ComboMotion.OverheadArc, ComboHand.Both, 44, CrossEaseIn, CrossEaseOut, CrossDecay, damageMult: 1.25f),
                },
            },
            new MeleeCombo
            {
                Name = ScissorCutName,
                BaseWeight = 55,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(200, 80, 160),
                CooldownAfterUse = 240,
                RecoveryTicks = 40,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[]
                {
                    WitchStep(ComboMotion.OverheadArc, ComboHand.Both, 36, CutEaseIn, CutEaseOut, CutDecay,
                        damageMult: 1.15f, backArc: ComboBackArc.Reverse),
                },
            },
            new MeleeCombo
            {
                Name = EchoCutName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(220, 100, 120),
                CooldownAfterUse = 200,
                RecoveryTicks = 34,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    WitchStep(ComboMotion.OverheadArc, ComboHand.Both, LightTellTicks, CutEaseIn, CutEaseOut, CutDecay,
                        damageMult: 1.05f, backArc: ComboBackArc.Delayed, backDelayTicks: EchoDelayTicks),
                },
            },
            new MeleeCombo
            {
                Name = EmberAshScissorName,
                BaseWeight = 40,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(230, 60, 120),
                CooldownAfterUse = 300,
                RecoveryTicks = 45,
                HeavyCommit = true,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Front, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, StringPause, StringDamageMult),
                    WitchStep(ComboMotion.OverheadArc, ComboHand.Both, 0, CutEaseIn, CutEaseOut, CutDecay,
                        damageMult: StringDamageMult, backArc: ComboBackArc.Reverse),
                },
            },
            new MeleeCombo
            {
                Name = FellLeapName,
                BaseWeight = 55,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(255, 90, 60),
                CooldownAfterUse = 260,
                RecoveryTicks = 40,
                RangedStartOnly = true,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[] { LeapStep(ComboHand.Both) },
            },
            new MeleeCombo
            {
                Name = BackstepReentryName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 60, 60),
                CooldownAfterUse = 300,
                RecoveryTicks = 40,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    // Owl Father's backstep, adapted: hop away with both blades rising overhead, then re-enter.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.BackstepRaise,
                        Hand = ComboHand.Both,
                        TelegraphTicks = LightTellTicks,
                        AttackTicks = 40,
                        PostStepPause = 7,
                        DamageMult = 0f,
                        ReachMult = 1f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                    WitchStep(ComboMotion.OverheadArc, ComboHand.Both, 0, CutEaseIn, CutEaseOut, CutDecay,
                        damageMult: 1.2f, forwardPushMult: 1.05f),
                },
            },
            new MeleeCombo
            {
                Name = VoidFlameName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 300,
                RecoveryTicks = 30,
                RangedStartOnly = true,
                MoveBrake = 0.5f,
                Steps = new[]
                {
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, LightTellTicks, 0, 0.8f),
                },
            },
        };

        // Phase-2 pool: the mace whip in front, the void sword behind, one-then-the-other.
        //
        // Move              Tell        Notes
        // Mace Lash          40         5–12 tiles; 12t live lash, rollable
        // Cleave and Lash    30 → 20    void over, then the lash 54t after the cut's live window ends
        // Lash and Cleave    40 → 10    the cut only if the player is still in front and in reach (25t gap)
        // Ash, Ash, Mace     30, 6, 20  two void cuts, the lash as finisher
        // Morgul Lash        40 → 26    a latching lash; the rising cut follows ONLY a latch, ≥41t after the hit
        // Void Cut           30         filler single cut
        // Fell Leap (Void)   30         the leap with the void blade only
        private static readonly MeleeCombo[] PhaseTwoCombos =
        {
            new MeleeCombo
            {
                Name = MaceLashName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(120, 60, 160),
                CooldownAfterUse = 120,
                RecoveryTicks = 30,
                MoveBrake = 0.4f,
                Steps = new[] { Lash(WitchkingMaceWhip.OpeningTelegraphTicks) },
            },
            new MeleeCombo
            {
                Name = CleaveAndLashName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 150,
                RecoveryTicks = 34,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Back, LightTellTicks, MidComboLashTell, StringDamageMult),
                    Lash(0),
                },
            },
            new MeleeCombo
            {
                Name = LashAndCleaveName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(120, 60, 160),
                CooldownAfterUse = 150,
                RecoveryTicks = 34,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    Lash(WitchkingMaceWhip.OpeningTelegraphTicks, LashToCutPause),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, 0, StringDamageMult),
                },
            },
            new MeleeCombo
            {
                Name = AshAshMaceName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 220,
                RecoveryTicks = 40,
                HeavyCommit = true,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    Cut(ComboMotion.OverheadArc, ComboHand.Back, LightTellTicks, StringPause, StringDamageMult),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, MidComboLashTell, StringDamageMult),
                    Lash(0),
                },
            },
            new MeleeCombo
            {
                Name = MorgulLashName,
                BaseWeight = 55,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(90, 30, 140),
                CooldownAfterUse = 360,
                RecoveryTicks = 40,
                HyperArmor = true,
                MoveBrake = 0.5f,
                Steps = new[]
                {
                    Lash(WitchkingMaceWhip.OpeningTelegraphTicks, MorgulPause),
                    Cut(ComboMotion.UnderhandArc, ComboHand.Back, 0, 0, 1.1f),
                },
            },
            new MeleeCombo
            {
                Name = VoidCutName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 60,
                RecoveryTicks = 25,
                MoveBrake = 0.3f,
                Steps = new[] { Cut(ComboMotion.OverheadArc, ComboHand.Back, LightTellTicks) },
            },
            new MeleeCombo
            {
                Name = VoidFellLeapName,
                BaseWeight = 50,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = VoidColor,
                CooldownAfterUse = 260,
                RecoveryTicks = 40,
                RangedStartOnly = true,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[] { LeapStep(ComboHand.Back) },
            },
        };

        // One cached pool (PuppetNPC builds it once); CanSelectMeleeCombo gates each phase's half. Void Flame is shared.
        private static readonly MeleeCombo[] AllCombos = PhaseOneCombos.Concat(PhaseTwoCombos).ToArray();
        private static readonly HashSet<string> PhaseOneComboNames = new HashSet<string>(PhaseOneCombos.Select(combo => combo.Name));
        private static readonly HashSet<string> PhaseTwoComboNames =
            new HashSet<string>(PhaseTwoCombos.Select(combo => combo.Name)) { VoidFlameName };

        protected override MeleeCombo[] MeleeComboPoolOverride => AllCombos;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            HashSet<string> phaseNames = PhaseOneComboNames;
            if (_phaseTwo)
            {
                phaseNames = PhaseTwoComboNames;
            }
            if (!phaseNames.Contains(combo.Name))
            {
                return false;
            }

            switch (combo.Name)
            {
                case WitchsCrossName:
                    return healthFraction < 0.8f;
                case EmberAshScissorName:
                    return healthFraction < 0.7f;
                case FellLeapName:
                case VoidFellLeapName:
                    return distance >= FellLeapMinRange && distance <= FellLeapMaxRange;
                case BackstepReentryName:
                    return _huggedTicks >= HugTicksToUnlock;
                case MaceLashName:
                case MorgulLashName:
                    return distance >= 80f && distance <= WitchkingMaceWhip.ReachPixels * 0.9f;
                case VoidFlameName:
                {
                    bool inBand = distance >= VoidFlameMinRange && distance <= VoidFlameMaxRange;
                    Player target = Main.player[NPC.target];
                    bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
                    return inBand && lineOfSight;
                }
            }
            return true;
        }

        // Server-only: Witch Cut alternates fire and void. The customized combo copy reaches clients in the snapshot.
        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (combo.Name != WitchCutName)
            {
                return;
            }

            if (_nextWitchCutUsesBackHand)
            {
                combo.Steps[0].Hand = ComboHand.Back;
                combo.InitialFlashColor = VoidColor;
            }
            _nextWitchCutUsesBackHand = !_nextWitchCutUsesBackHand;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _runningCombo = combo;
            _comboStepsCompleted = 0;
            _leapImpactSpawned = false;
            _whipComboActive = combo.Steps != null && combo.Steps.Any(IsLashStep);
            if (_whipComboActive && NPC.HasValidTarget)
            {
                _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);
            }
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            bool stillInFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
            float followUpReach = ComboReachBase * 0.7f * 1.18f + 36f;
            bool insideReach = NPC.Distance(target.Center) <= followUpReach;

            // Morgul Lash's cut only follows a latch: without one there is no one to cut.
            if (comboName == MorgulLashName)
            {
                return previousStepHit;
            }

            // Lash → cut goes live 25t after the lash, inside a late roller's gap, so it needs the player still in
            // front and in reach (a player who rolled through or away isn't followed).
            if (comboName == LashAndCleaveName)
            {
                return previousStepHit || (stillInFront && insideReach);
            }

            // The third and later cuts of a string: only after a hit, or with the player still in front and in reach,
            // so a player who rolled through or backed off isn't chased by whiffs. (The spacing itself is fair.)
            bool longString = comboName == TwoFiresOneAshName
                || comboName == TwoAshesOneFireName
                || comboName == FigureEightName
                || comboName == EmberAshScissorName;
            if (longString && nextStepIndex >= 2)
            {
                return previousStepHit || (stillInFront && insideReach);
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (!InComboPose)
            {
                return;
            }

            float highPose = CutHighPose;
            float lowPose = CutLowPose;
            if (ActiveMeleeComboName == WitchsCrossName)
            {
                highPose = CrossHighPose;
                lowPose = CrossLowPose;
            }

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = highPose;
                endRotation = lowPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = lowPose;
                endRotation = highPose;
            }
            else if (motion == ComboMotion.HorizontalSweep)
            {
                // Only the mace lashes use HorizontalSweep.
                startRotation = LashStartPose;
                endRotation = LashEndPose;
            }
        }

        // Tells: the opening lash spawns its whip (its own 40t telegraph); Void Flame forms at the void tip.
        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (IsLashStep(step))
            {
                if (elapsed < total - 10 && NPC.HasValidTarget)
                {
                    _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);
                }
                if (elapsed == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnMaceWhip(total, combo.Name == MorgulLashName);
                }
                return;
            }

            if (combo.Name == VoidFlameName && !Main.dedServ)
            {
                // Violet motes converge on the void tip and a flame grows there through the tell.
                float progress = elapsed / (float)Math.Max(1, total);
                Vector2 voidTip = PuppetBackHandPosition + PuppetBackWeaponDirection * SwordTipReach;
                Vector2 offset = Main.rand.NextVector2CircularEdge(60f, 60f) * (1f - progress);
                Dust mote = Dust.NewDustPerfect(voidTip + offset, DustID.Shadowflame, -offset * 0.08f, 80, default, 1.1f);
                mote.noGravity = true;
                Dust core = Dust.NewDustPerfect(voidTip, DustID.PurpleTorch, Vector2.Zero, 60, default, 0.6f + progress * 1.2f);
                core.noGravity = true;
            }
        }

        // A mid-combo lash's whip spawns at the start of the preceding pause; that pause is its tell.
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            _comboStepsCompleted++;
            bool nextExists = _runningCombo.Steps != null && _comboStepsCompleted < _runningCombo.Steps.Length;
            if (!nextExists || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            MeleeComboStep nextStep = _runningCombo.Steps[_comboStepsCompleted];
            if (IsLashStep(nextStep))
            {
                if (NPC.HasValidTarget)
                {
                    _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);
                }
                SpawnMaceWhip(step.PostStepPause, latch: false);
            }
        }

        private void SpawnMaceWhip(int telegraphTicks, bool latch)
        {
            float latchFlag = 0f;
            if (latch)
            {
                latchFlag = 1f;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), PuppetHandPosition, _lockedWhipAim,
                ModContent.ProjectileType<WitchkingMaceWhip>(), EnemyDamage.Projectile(MaceLashDamage), 4f,
                Main.myPlayer, NPC.whoAmI, latchFlag, telegraphTicks);
        }

        // The Dark Blood Knight's aim: lead the target 6 ticks, clamp the pitch to ±36° so the rope reads as a lash.
        private Vector2 CalculateWhipAim(Player target)
        {
            Vector2 delta = target.Center + target.velocity * 6f - PuppetHandPosition;
            float x = NPC.direction * Math.Max(1f, Math.Abs(delta.X));
            float yLimit = Math.Abs(x) * 0.72f;
            Vector2 aim = new Vector2(x, MathHelper.Clamp(delta.Y, -yLimit, yLimit));
            return aim.SafeNormalize(new Vector2(NPC.direction, 0f));
        }

        // IEnemyWhipOwner (the mace whip reads these every tick on every machine).
        public Vector2 WhipAnchor => PuppetHandPosition;
        public Vector2 WhipAim => _lockedWhipAim;
        // Not in recovery: a lash step's own 30 ticks cover the lash, and a combo cut short must not leave a whip
        // lashing with no arm behind it.
        public bool IsWhipActive => _phaseTwo && _whipComboActive
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        // Arcs: one VanillaSwordArc per swinging blade, on the step's exact Weighted clock. A Reverse back blade sweeps
        // the other way; a Delayed back blade gets its arc when its own strike starts. Void Flame fires on the peak.
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            SpawnLiveBladeDust(step);

            int peakTick = (int)Math.Round(step.EaseInTicks * total / (float)Math.Max(1, step.AttackTicks));
            bool voidFlameRelease = combo.Name == VoidFlameName && elapsed == peakTick;
            if (voidFlameRelease && !Main.dedServ)
            {
                Vector2 voidTip = PuppetBackHandPosition + PuppetBackWeaponDirection * SwordTipReach;
                SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.8f, Pitch = -0.2f }, voidTip);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || step.DamageMult <= 0f)
            {
                return;
            }

            if (voidFlameRelease)
            {
                ReleaseVoidFlames();
            }

            bool arcMotion = step.Motion == ComboMotion.OverheadArc || step.Motion == ComboMotion.UnderhandArc;
            if (!arcMotion)
            {
                return;
            }

            float sweep = MathHelper.ToRadians(220f);
            if (step.Motion == ComboMotion.UnderhandArc)
            {
                sweep = -sweep;
            }

            bool frontSwings = step.Hand != ComboHand.Back;
            bool backSwings = step.Hand != ComboHand.Front;

            int backStartTick = 0;
            if (step.Hand == ComboHand.Both && step.BackArc == ComboBackArc.Delayed)
            {
                backStartTick = (int)Math.Round(step.BackDelayTicks * total / (float)Math.Max(1, step.AttackTicks));
            }

            float backSweep = sweep;
            if (step.Hand == ComboHand.Both && step.BackArc == ComboBackArc.Reverse)
            {
                backSweep = -sweep;
            }

            if (frontSwings && elapsed == 0)
            {
                SpawnBladeArc(step, total, sweep, trackBackHand: false);
            }
            if (backSwings && elapsed == backStartTick)
            {
                SpawnBladeArc(step, total - backStartTick, backSweep, trackBackHand: true);
            }
        }

        // Server: the flame(s) leave the void tip toward the nearest valid player (1, or a 40° fan of 3 in phase 2).
        private void ReleaseVoidFlames()
        {
            Vector2 voidTip = PuppetBackHandPosition + PuppetBackWeaponDirection * SwordTipReach;
            int targetIndex = NPC.target;
            float nearest = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                {
                    continue;
                }
                float candidateDistance = candidate.Distance(voidTip);
                if (candidateDistance < nearest)
                {
                    nearest = candidateDistance;
                    targetIndex = i;
                }
            }

            Vector2 aim = (Main.player[targetIndex].Center - voidTip).SafeNormalize(new Vector2(NPC.direction, 0f));
            int count = 1;
            if (_phaseTwo)
            {
                count = 3;
            }
            float spread = MathHelper.ToRadians(VoidFlameFanDegrees);
            for (int i = 0; i < count; i++)
            {
                float offset = 0f;
                if (count > 1)
                {
                    offset = MathHelper.Lerp(-spread / 2f, spread / 2f, i / (float)(count - 1));
                }
                Vector2 velocity = aim.RotatedBy(offset) * WitchkingVoidFlame.LaunchSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), voidTip, velocity, ModContent.ProjectileType<WitchkingVoidFlame>(),
                    EnemyDamage.Projectile(VoidFlameDamage), 0f, Main.myPlayer, targetIndex);
            }
        }

        // Fell Leap impact: both (or the void) blades come down in a short arc, with a dust burst under the feet.
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Bottom);
                for (int i = 0; i < 30; i++)
                {
                    int dustType = i % 2 == 0 ? DustID.Shadowflame : DustID.Torch;
                    if (_phaseTwo)
                    {
                        dustType = DustID.Shadowflame;
                    }
                    Dust burst = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(16f, 8f), NPC.width + 32, 12, dustType, 0f, -2f, 60, default, 1.5f);
                    burst.velocity.X *= 3f;
                    burst.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || _leapImpactSpawned)
            {
                return;
            }

            _leapImpactSpawned = true;
            float sweep = MathHelper.ToRadians(150f);
            if (step.Hand != ComboHand.Back)
            {
                SpawnBladeArc(step, 10, sweep, trackBackHand: false);
            }
            if (step.Hand != ComboHand.Front)
            {
                SpawnBladeArc(step, 10, sweep, trackBackHand: true);
            }
        }

        private void SpawnBladeArc(MeleeComboStep step, int duration, float sweep, bool trackBackHand)
        {
            Vector2 bladeDirection = PuppetWeaponDirection;
            Color darkColor = new Color(70, 18, 0);
            Color bodyColor = new Color(235, 95, 25);
            Color coreColor = new Color(255, 200, 110);
            int dustType = DustID.Torch;
            if (trackBackHand)
            {
                bladeDirection = PuppetBackWeaponDirection;
                darkColor = new Color(22, 0, 40);
                bodyColor = new Color(135, 55, 205);
                coreColor = new Color(215, 165, 255);
                dustType = DustID.Shadowflame;
            }

            float hitWindowEnd = step.HitWindowEnd > 0f ? step.HitWindowEnd : 0.5f;
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;

            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Weighted,
                WeightedEaseInTicks = step.EaseInTicks,
                WeightedEaseOutTicks = step.EaseOutTicks,
                WeightedEaseOutDecay = step.EaseOutDecay,
                Duration = duration,
                StartAngle = bladeDirection.ToRotation(),
                SweepAngle = sweep * NPC.direction,
                Radius = bladeReach,
                Opacity = 0.55f,
                FadeInFraction = 0.04f,
                FadeOutFraction = 1f - hitWindowEnd,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.6f,
                BodyOpacity = 0.75f,
                CoreOpacity = 0.25f,
                TipSparkleOpacity = 0f,
                DarkColor = darkColor,
                BodyColor = bodyColor,
                CoreColor = coreColor,
                DustType = dustType,
                DustColor = bodyColor,
                DustCount = 1,
                DustAlpha = 70,
                DustScale = 0.9f,
                DustRadiusFraction = 0.92f,
                DustAngularSpread = 0.45f,
                DustTangentialSpeed = 1.0f,
                DustVelocityJitter = 0.3f,
                DustInheritAnchorVelocity = 0.15f,
                DrawTipSparkle = false,
                TrackPuppetBlade = true,
                TrackPuppetBackHand = trackBackHand,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer, settings, Vector2.Zero, hostile: false);
        }

        // Dust along each swinging blade while it's live: fire embers on the front, shadowflame on the back.
        private void SpawnLiveBladeDust(MeleeComboStep step)
        {
            if (Main.dedServ || step.DamageMult <= 0f)
            {
                return;
            }

            bool frontSwings = step.Hand != ComboHand.Back && !_phaseTwo;
            bool backSwings = step.Hand != ComboHand.Front;
            if (frontSwings)
            {
                float along = Main.rand.NextFloat(0.3f, 1f) * SwordTipReach;
                Vector2 bladePoint = PuppetHandPosition + PuppetWeaponDirection * along;
                Dust fire = Dust.NewDustPerfect(bladePoint, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f), 40, default, 1.3f);
                fire.noGravity = true;
            }
            if (backSwings)
            {
                float along = Main.rand.NextFloat(0.3f, 1f) * SwordTipReach;
                Vector2 bladePoint = PuppetBackHandPosition + PuppetBackWeaponDirection * along;
                Dust shadow = Dust.NewDustPerfect(bladePoint, DustID.Shadowflame, Main.rand.NextVector2Circular(1f, 1f), 40, default, 1.2f);
                shadow.noGravity = true;
            }
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (step.DamageMult > 0f && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, Pitch = -0.25f, PitchVariance = 0.15f }, NPC.Center);
            }
            base.DoComboMeleeHit(step);
        }

        protected override void DoMeleeAttack() { } // all melee is the combo pool
        protected override void DoRangedAttack() { }
        #endregion

        #region On-hit effects
        // Runs on the hit player's machine (PuppetMeleeHitbox.OnHitPlayer). The hand says which blade connected.
        public void OnHumanoidMeleeHit(Player target, PuppetHandSlot hand)
        {
            if (hand == PuppetHandSlot.Back)
            {
                ApplyVoidHit(target);
                return;
            }
            ApplyFireHit(target);
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            ApplyFireHit(target);
        }

        // Fire: a pink burst in the Miasma colour, Miasma buildup, On Fire. Shared with the fire walls.
        public static void ApplyFireHit(Player target)
        {
            target.AddBuff(BuffID.OnFire, FireOnFireTicks);
            MiasmaBuildup.Apply(target);

            if (!Main.dedServ)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust pink = Dust.NewDustDirect(target.position, target.width, target.height, DustID.PinkTorch,
                        0f, 0f, 60, default, 1.4f);
                    pink.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    pink.noGravity = true;
                }
            }
        }

        // Void: Weight of Shadow, Withered Armor, Withered Weapon. Shared with Void Flame and the phantom's blade.
        public static void ApplyVoidHit(Player target)
        {
            target.AddBuff(ModContent.BuffType<WeightOfShadow>(), VoidDebuffTicks);
            target.AddBuff(BuffID.WitheredArmor, VoidDebuffTicks);
            target.AddBuff(BuffID.WitheredWeapon, VoidDebuffTicks);
        }
        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            NPC.damage = 0; // puppet convention: only the blades, the whip and the spells hurt
            NPC.defense = 65;
            NPC.lifeMax = 170000; // pending BossDpsBands (the legacy boss died in 28s to the top weapon)
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream");
            NPC.value = 350000;
            NPC.knockBackResist = 0.15f; // poise dial; the PoiseProfiles entry (0.15 / 120) is authoritative
            NPC.rarity = 32;
            NPC.boss = true;
            // Allows the witchking to properly spawn in the arena
            NPC.lavaImmune = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Witchking.DespawnHandler"), Color.Purple, DustID.PurpleTorch);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.4f;
            globalNPC.DirectionalDodgeRolls = true;
            globalNPC.PreserveJumpFacingUntilLanding = true;
            // The legacy random knockback/teleport-on-hit (rolled client-side) is replaced by the Red Knight trims:
            // an occasional dodge roll is the only on-hit reaction (1 in 6, 120t apart).
            EvasiveProfile.RedKnight(globalNPC);
            globalNPC.EvasiveRetreatJump = false;
            globalNPC.EvasiveRetreatDash = false;
            globalNPC.EvasiveDodgeRoll = true;
            globalNPC.EvasiveTeleportAway = false;
            globalNPC.EvasiveOnHitChanceDenominator = 6;
            globalNPC.EvasiveOnHitCooldownTicks = 120;
            globalNPC.PoiseStaggerResetsAI = true;

            // Losing line of sight brings a void-smoke blink, so the cramped cave can't be used to kite him forever.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
        }

        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active || despawnHandler.IsDespawning)
            {
                return;
            }

            // A set-piece that was cut short (a stagger reset, a despawn reset) must not leave its kind behind.
            bool customEnded = _customKind != CustomKind.None && Phase != AttackPhase.Custom;
            if (customEnded && _customKind != CustomKind.Unmaking)
            {
                _customKind = CustomKind.None;
                _displayResetPending = true;
            }

            EquipHands();
            UpdateShield();
            TickSchedulers();
            TryStartSetPiece();

            base.AI();

            // The Unmaking is immune throughout; re-assert after the base tick in case anything cleared it.
            if (_unmakingStarted)
            {
                NPC.dontTakeDamage = true;
            }

            // Hellfire Plunge buries the blade in the floor: drawn in the behind-tiles pass so the tiles cover it.
            NPC.behindTiles = _customKind == CustomKind.HellfirePlunge && Phase == AttackPhase.Custom;

            // After a set-piece the held item is still its pose item (the base only re-picks it when a new attack
            // starts). The weapons stay drawn in neutral, so put the current melee weapon back in hand.
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (_displayResetPending && neutral)
            {
                SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                _displayResetPending = false;
            }

            TickPull();
            ApplyFightCurses();
            SpawnAmbientBladeDust();

            if (_immuneTextCooldown > 0)
            {
                _immuneTextCooldown--;
            }

            DebugAttackLabel = SetPieceDebugLabel();
            Lighting.AddLight(NPC.Center, new Color(120, 60, 170).ToVector3() * 0.3f);
        }

        // Every peer equips the same hands from synced state: the void sword behind always; in front the fire sword
        // (phase 1), the mace (phase 2: its icon hides while the whip projectile is out), or the reverse-grip fire sword
        // for Hellfire Plunge in either phase.
        private void EquipHands()
        {
            PuppetWeapon desiredFront = FireSword;
            if (_phaseTwo)
            {
                desiredFront = MaceCoiled;
                if (MaceWhipOut())
                {
                    desiredFront = MaceLashing;
                }
            }
            if (_customKind == CustomKind.HellfirePlunge && Phase == AttackPhase.Custom)
            {
                desiredFront = ReverseFireSword;
            }

            if (FrontHandWeapon != desiredFront)
            {
                EquipWeapon(PuppetHandSlot.Front, desiredFront);
            }
            if (BackHandWeapon != VoidSword)
            {
                EquipWeapon(PuppetHandSlot.Back, VoidSword);
            }
        }

        private bool MaceWhipOut()
        {
            int whipType = ModContent.ProjectileType<WitchkingMaceWhip>();
            int grondType = ModContent.ProjectileType<WitchkingGrondMace>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                bool ours = projectile.active && (int)projectile.ai[0] == NPC.whoAmI;
                if (ours && (projectile.type == whipType || projectile.type == grondType))
                {
                    return true;
                }
            }
            return false;
        }

        // Re-track the mid-combo lash aim through the readable part of its pause tell; lock for the last 10 ticks.
        public override void PostAI()
        {
            base.PostAI();

            bool trackingPauseTell = IsWhipActive
                && Phase == AttackPhase.MeleeComboPause
                && PhaseTimer > 10
                && NPC.HasValidTarget;
            if (trackingPauseTell)
            {
                _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);
            }
        }

        #region Shield (DispelShadow gate + last stand)
        private void UpdateShield()
        {
            // A Barrow Blade / Forgotten Gaia Sword hit applies DispelShadow (an NPC buff, synced by vanilla). Once
            // broken the shield stays down until the last stand.
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                _defenseBroken = true;
            }

            // Below 10% the shield comes back once and must be dispelled again. Server decides and syncs both flags.
            bool lastStandReached = NPC.life > 0 && NPC.life < NPC.lifeMax * LastStandFraction;
            if (lastStandReached && !_lastStand && _defenseBroken && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _lastStand = true;
                _defenseBroken = false;
                int dispelIndex = NPC.FindBuffIndex(ModContent.BuffType<Buffs.DispelShadow>());
                if (dispelIndex >= 0)
                {
                    NPC.DelBuff(dispelIndex);
                }
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Witchking.Restore"), Color.Purple);
                NPC.netUpdate = true;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyShieldCap(ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyShieldCap(ref modifiers);
        }

        private void ApplyShieldCap(ref NPC.HitModifiers modifiers)
        {
            if (_defenseBroken)
            {
                return;
            }

            modifiers.SetMaxDamage(1);

            // One "Immune" text per 30 ticks instead of one per hit (the legacy boss spammed dozens a second).
            if (_immuneTextCooldown <= 0)
            {
                _immuneTextCooldown = ImmuneTextIntervalTicks;
                Rectangle textArea = new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10);
                CombatText.NewText(textArea, Color.Crimson, LangUtils.GetTextValue("NPCs.Witchking.Immune"), true, false);
            }
        }
        #endregion

        #region Set-piece scheduling
        // Server-only: queue shrieks as HP crosses each threshold, count down the set-piece cooldowns, and track how long
        // the player has hugged him (Backstep Re-entry's unlock).
        private void TickSchedulers()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float lifeFraction = NPC.life / (float)NPC.lifeMax;
            while (_shrieksQueued < ShriekThresholds.Length && lifeFraction < ShriekThresholds[_shrieksQueued])
            {
                _shrieksQueued++;
                NPC.netUpdate = true;
            }

            if (_plungeCooldown > 0)
            {
                _plungeCooldown--;
            }
            if (_grondCooldown > 0)
            {
                _grondCooldown--;
            }
            if (_phaseTwo && CountPhantoms() < PhantomCap && _summonCooldown > 0)
            {
                _summonCooldown--;
            }

            bool hugged = NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) < HugDistance;
            if (hugged)
            {
                _huggedTicks = Math.Min(_huggedTicks + 1, HugTicksToUnlock * 2);
            }
            else
            {
                _huggedTicks = 0;
            }
        }

        private int PhantomCap => NPC.life < NPC.lifeMax * SecondPhantomHealthGate ? 2 : 1;

        private int CountPhantoms()
        {
            int phantomType = ModContent.NPCType<BarrowWightPhantom>();
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.type == phantomType && (int)other.ai[0] == NPC.whoAmI)
                {
                    count++;
                }
            }
            return count;
        }

        // Server-only. Priority: a queued shriek (the 50% one is the phase transition) > Barrow Summons > Grond's Toll >
        // Hellfire Plunge. Only from a free moment, so no set-piece ever cuts a swing short.
        private void TryStartSetPiece()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _unmakingStarted || !NPC.HasValidTarget)
            {
                return;
            }

            bool free = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (!free)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            bool grounded = NPC.velocity.Y == 0f;

            if (_shrieksStarted < _shrieksQueued)
            {
                bool transition = _shrieksStarted == TransitionShriekIndex;
                _shrieksStarted++;
                if (transition)
                {
                    _summonPoint = PickSummonPoint(target);
                    BeginSetPiece(CustomKind.Transition, TransitionTotalTicks, ModContent.ItemType<WitchkingsSword>());
                    return;
                }
                BeginSetPiece(CustomKind.Shriek, ShriekTellTicks + ShriekHoldTicks, MeleeWeaponItemType);
                return;
            }

            bool summonReady = _phaseTwo && _summonCooldown <= 0 && CountPhantoms() < PhantomCap;
            if (summonReady)
            {
                _summonPoint = PickSummonPoint(target);
                _summonCooldown = SummonCooldownTicks;
                BeginSetPiece(CustomKind.BarrowSummons, SummonTotalTicks, MeleeWeaponItemType);
                return;
            }

            bool grondReady = _phaseTwo && _grondCooldown <= 0 && grounded && NPC.life < NPC.lifeMax * GrondHealthGate;
            if (grondReady)
            {
                _grondCooldown = GrondCooldownTicks;
                BeginSetPiece(CustomKind.GrondsToll, WitchkingGrondMace.TotalTicks, MeleeWeaponItemType);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), PuppetHandPosition, Vector2.Zero,
                    ModContent.ProjectileType<WitchkingGrondMace>(), EnemyDamage.Projectile(GrondHeadDamage), 6f,
                    Main.myPlayer, NPC.whoAmI, NPC.target);
                return;
            }

            bool plungeReady = _plungeCooldown <= 0 && grounded && distance >= PlungeMinRange;
            if (plungeReady)
            {
                _plungeCooldown = PlungeCooldownTicks;
                _plungeWallCount = 1;
                if (_phaseTwo)
                {
                    _plungeWallCount = 3;
                }
                int lastEruption = PlungeImpactTick + PlungeWallIntervalTicks * _plungeWallCount;
                BeginSetPiece(CustomKind.HellfirePlunge, lastEruption + PlungeHoldTicks + PlungePullTicks, ModContent.ItemType<WitchkingsSword>());
            }
        }

        private void BeginSetPiece(CustomKind kind, int totalTicks, int poseItemType)
        {
            _customKind = kind;
            _customTotalTicks = totalTicks;
            _customElapsed = 0;
            StartCustomAttack(totalTicks, poseItemType, swingPose: true);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // ≥ 8 tiles from every player, about 10 tiles to the side of him away from his target (the other side if that is
        // too close to someone); the phantom passes through walls, so a point inside rock is still valid.
        private Vector2 PickSummonPoint(Player target)
        {
            float awaySide = Math.Sign(NPC.Center.X - target.Center.X);
            if (awaySide == 0f)
            {
                awaySide = 1f;
            }

            Vector2[] candidates =
            {
                NPC.Center + new Vector2(awaySide * 160f, -40f),
                NPC.Center + new Vector2(-awaySide * 160f, -40f),
                NPC.Center + new Vector2(0f, -120f),
            };
            foreach (Vector2 candidate in candidates)
            {
                bool clearOfPlayers = true;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && player.Distance(candidate) < SummonMinPlayerDistance)
                    {
                        clearOfPlayers = false;
                        break;
                    }
                }
                if (clearOfPlayers)
                {
                    return candidate;
                }
            }
            return candidates[0];
        }

        private string SetPieceDebugLabel()
        {
            if (Phase != AttackPhase.Custom)
            {
                return null;
            }

            switch (_customKind)
            {
                case CustomKind.Shriek:
                    return "Nazgûl Shriek";
                case CustomKind.Transition:
                    return "The Black Captain's Mace";
                case CustomKind.BarrowSummons:
                    return "Barrow Summons";
                case CustomKind.HellfirePlunge:
                    return $"Hellfire Plunge ({_plungeWallCount} wall{(_plungeWallCount > 1 ? "s" : "")})";
                case CustomKind.GrondsToll:
                    return "Grond's Toll";
                case CustomKind.Unmaking:
                    return $"The Unmaking ({_customElapsed}/{UnmakingBuildTicks})";
            }
            return null;
        }
        #endregion

        #region Set-pieces
        protected override bool CustomAttackCommitted
        {
            get
            {
                switch (_customKind)
                {
                    case CustomKind.Shriek:
                    case CustomKind.Transition:
                    case CustomKind.BarrowSummons:
                    case CustomKind.Unmaking:
                        return true;
                    // Armoured through the raise and the plunge; the 90t with the blade in the floor is the window to
                    // break his poise — a stagger cancels the walls.
                    case CustomKind.HellfirePlunge:
                        return _customElapsed < PlungeImpactTick;
                    case CustomKind.GrondsToll:
                        return _customElapsed < WitchkingGrondMace.ImpactTick;
                }
                return false;
            }
        }

        protected override float? CustomWeaponRotation
        {
            get
            {
                int elapsed = _customElapsed;
                switch (_customKind)
                {
                    case CustomKind.Shriek:
                        return WeaponsRaisedStraightUp;
                    case CustomKind.Transition:
                        if (elapsed < TransitionMaceTick)
                        {
                            return WeaponsRaisedStraightUp;
                        }
                        return null;
                    case CustomKind.HellfirePlunge:
                        return PlungeArmRotation(elapsed);
                    case CustomKind.GrondsToll:
                        if (elapsed < WitchkingGrondMace.SlamStartTick)
                        {
                            return WeaponsRaisedStraightUp;
                        }
                        if (elapsed < WitchkingGrondMace.ImpactTick + 20)
                        {
                            float slam = (elapsed - WitchkingGrondMace.SlamStartTick) / (float)(WitchkingGrondMace.ImpactTick - WitchkingGrondMace.SlamStartTick);
                            return MathHelper.Lerp(WeaponsRaisedStraightUp, CutLowPose, MathHelper.Clamp(slam, 0f, 1f));
                        }
                        return null;
                    case CustomKind.Unmaking:
                        return ArmLevelForward;
                }
                return null;
            }
        }

        protected override float? CustomBackWeaponRotation
        {
            get
            {
                int elapsed = _customElapsed;
                switch (_customKind)
                {
                    case CustomKind.Shriek:
                        return WeaponsRaisedStraightUp;
                    case CustomKind.Transition:
                        if (elapsed < TransitionMaceTick)
                        {
                            return WeaponsRaisedStraightUp;
                        }
                        return null;
                    case CustomKind.BarrowSummons:
                        if (elapsed < SummonRaiseTicks)
                        {
                            return WeaponsRaisedStraightUp;
                        }
                        return null;
                    case CustomKind.Unmaking:
                        return ArmLevelBackward;
                }
                return null;
            }
        }

        // Hellfire Plunge arm: raise overhead → strike down to level-forward (the reverse-grip blade then points
        // straight down into the floor) → hold → pull back to the carry.
        private float PlungeArmRotation(int elapsed)
        {
            int pullStart = _customTotalTicks - PlungePullTicks;
            if (elapsed < PlungeRaiseTicks)
            {
                float raise = MathHelper.SmoothStep(0f, 1f, elapsed / (float)PlungeRaiseTicks);
                return MathHelper.Lerp(MeleeCarryRotation, CutHighPose, raise);
            }
            if (elapsed < PlungeImpactTick)
            {
                float strike = (elapsed - PlungeRaiseTicks) / (float)PlungeStrikeTicks;
                float eased = strike * strike; // accelerates into the floor
                return MathHelper.Lerp(CutHighPose, ArmLevelForward, eased);
            }
            if (elapsed < pullStart)
            {
                return ArmLevelForward;
            }
            float pull = MathHelper.SmoothStep(0f, 1f, (elapsed - pullStart) / (float)PlungePullTicks);
            return MathHelper.Lerp(ArmLevelForward, MeleeCarryRotation, pull);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            _customElapsed = _customTotalTicks - ticksRemaining;
            int elapsed = _customElapsed;

            switch (_customKind)
            {
                case CustomKind.Shriek:
                    TickShriekTell(elapsed);
                    if (elapsed == ShriekTellTicks)
                    {
                        ReleaseShriek();
                    }
                    break;
                case CustomKind.Transition:
                    TickTransition(elapsed);
                    break;
                case CustomKind.BarrowSummons:
                    TickSummonColumn(elapsed, SummonRaiseTicks);
                    if (elapsed == SummonRaiseTicks)
                    {
                        SpawnPhantom();
                    }
                    break;
                case CustomKind.HellfirePlunge:
                    TickHellfirePlunge(elapsed);
                    break;
                case CustomKind.Unmaking:
                    TickUnmaking(elapsed);
                    break;
            }

            if (ticksRemaining == 1 && _customKind != CustomKind.Unmaking)
            {
                _customKind = CustomKind.None;
                _displayResetPending = true;
            }
        }

        // Violet motes converge on him from a shrinking ring, so the release reads as power gathering.
        private void TickShriekTell(int elapsed)
        {
            if (elapsed >= ShriekTellTicks || Main.dedServ)
            {
                return;
            }

            float collapse = 1f - elapsed / (float)ShriekTellTicks;
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(200f, 200f) * collapse;
                Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.ShadowbeamStaff, -offset * 0.05f, 80, default, 1.2f);
                mote.noGravity = true;
            }
        }

        // The legacy scream's own sound, dust, flash and shockwave; the effect projectiles are spawned once, by the
        // server (the legacy AI() spawned one copy per machine). Starts the 8s pull — permanent at the last stand.
        private void ReleaseShriek()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Lotr/WitchkingScream"), NPC.Center);
                // The legacy scream's two radial bursts (dust 114 CrimsonSpray and 130 Firework_Red), unchanged.
                for (int i = 0; i < 20; i++)
                {
                    Vector2 firstSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    Dust.NewDustDirect(NPC.Center, 0, 0, DustID.CrimsonSpray, firstSpeed.X, firstSpeed.Y, 0, default, 2f).noGravity = true;
                    Vector2 secondSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Firework_Red, secondSpeed.X, secondSpeed.Y, 0, default, 2f).noGravity = true;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
            }

            _pullTicksRemaining = PullTicks;
            _pullPermanent = _lastStand;
        }

        // The Black Captain's Mace: shriek (0–40 tell, release at 40), the fire blade gutters and its embers implode
        // into the hand (40–90), the mace drops out of void smoke at 90 (phase 2 begins on every peer on this tick),
        // the void-smoke column builds over the summon point (90–120) and the first phantom rises at 120.
        private void TickTransition(int elapsed)
        {
            TickShriekTell(elapsed);
            if (elapsed == ShriekTellTicks)
            {
                ReleaseShriek();
            }

            bool guttering = elapsed > ShriekTellTicks && elapsed < TransitionMaceTick;
            if (guttering && !Main.dedServ)
            {
                Vector2 fireTip = PuppetHandPosition + PuppetWeaponDirection * SwordTipReach;
                float flare = 1f - (elapsed - ShriekTellTicks) / (float)(TransitionMaceTick - ShriekTellTicks);
                Vector2 offset = Main.rand.NextVector2Circular(40f, 40f) * flare;
                Dust ember = Dust.NewDustPerfect(fireTip + offset, DustID.Torch, (PuppetHandPosition - (fireTip + offset)) * 0.06f, 40, default, 1.6f * flare + 0.4f);
                ember.noGravity = true;
            }

            if (elapsed == TransitionMaceTick)
            {
                // The held item must follow the hand: the base re-applies the Custom pose item every tick, and the back
                // void sword only draws while the held item matches the front weapon. So restart the rest of the
                // set-piece posed with the mace (PhaseTimer becomes 59 after this tick's decrement → elapsed 91 next).
                _phaseTwo = true;
                StartCustomAttack(TransitionTotalTicks - TransitionMaceTick, ModContent.ItemType<WitchkingMace>(), swingPose: true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item153 with { Volume = 0.9f, Pitch = -0.5f }, PuppetHandPosition);
                    for (int i = 0; i < 30; i++)
                    {
                        Dust smoke = Dust.NewDustDirect(PuppetHandPosition - new Vector2(12f), 24, 24, DustID.Wraith, 0f, 0f, 100, default, 1.6f);
                        smoke.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        smoke.noGravity = true;
                    }
                }
            }

            if (elapsed >= TransitionMaceTick)
            {
                TickSummonColumn(elapsed - TransitionMaceTick, TransitionSummonTick - TransitionMaceTick);
            }
            if (elapsed == TransitionSummonTick)
            {
                _summonCooldown = SummonCooldownTicks;
                SpawnPhantom();
            }
        }

        // A column of void smoke rising at the summon point, thickening until the phantom appears.
        private void TickSummonColumn(int elapsed, int buildTicks)
        {
            if (elapsed > buildTicks || Main.dedServ)
            {
                return;
            }

            float build = elapsed / (float)Math.Max(1, buildTicks);
            int count = 1 + (int)(4f * build);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = _summonPoint + new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-40f, 40f));
                Dust smoke = Dust.NewDustPerfect(position, DustID.Wraith, new Vector2(0f, -1.6f), 100, default, 1.4f);
                smoke.noGravity = true;
                if (Main.rand.NextBool(3))
                {
                    Dust wisp = Dust.NewDustPerfect(position, DustID.Shadowflame, new Vector2(0f, -1.2f), 80, default, 1.1f);
                    wisp.noGravity = true;
                }
            }
            Lighting.AddLight(_summonPoint, 0.3f * build, 0.1f * build, 0.45f * build);
        }

        private void SpawnPhantom()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.NewNPC(NPC.GetSource_FromAI(), (int)_summonPoint.X, (int)_summonPoint.Y + 23,
                ModContent.NPCType<BarrowWightPhantom>(), 0, NPC.whoAmI);
        }

        // Hellfire Plunge: at the impact the blade is in the floor; flame builds up out of the ground around it for
        // 90t, then the walls erupt both ways (phase 2: every 90t, three times).
        private void TickHellfirePlunge(int elapsed)
        {
            float floorY = (int)(NPC.Bottom.Y / 16f) * 16f;
            Vector2 bladePoint = new Vector2(PuppetHandPosition.X, floorY);

            if (elapsed == PlungeImpactTick && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.9f, Pitch = -0.5f }, bladePoint);
                for (int i = 0; i < 25; i++)
                {
                    Dust spark = Dust.NewDustPerfect(bladePoint, DustID.Torch, new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-5f, -1f)), 40, default, 1.6f);
                    spark.noGravity = true;
                }
            }

            int lastEruption = PlungeImpactTick + PlungeWallIntervalTicks * _plungeWallCount;
            bool building = elapsed > PlungeImpactTick && elapsed <= lastEruption;
            if (building && !Main.dedServ)
            {
                int sinceImpact = elapsed - PlungeImpactTick;
                float wallBuild = (sinceImpact % PlungeWallIntervalTicks) / (float)PlungeWallIntervalTicks;
                int flameCount = 1 + (int)(5f * wallBuild);
                for (int i = 0; i < flameCount; i++)
                {
                    Vector2 position = bladePoint + new Vector2(Main.rand.NextFloat(-24f, 24f) * (0.4f + wallBuild), 0f);
                    Dust flame = Dust.NewDustPerfect(position, DustID.Torch, new Vector2(0f, -Main.rand.NextFloat(1f, 3f + 4f * wallBuild)), 40, default, 1.2f + wallBuild);
                    flame.noGravity = true;
                }
                Lighting.AddLight(bladePoint, 0.9f * wallBuild, 0.4f * wallBuild, 0.1f * wallBuild);
            }

            bool eruption = elapsed > PlungeImpactTick && elapsed <= lastEruption && (elapsed - PlungeImpactTick) % PlungeWallIntervalTicks == 0;
            if (eruption && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 wallCenter = new Vector2(bladePoint.X, floorY - WitchkingFireWall.WallHeight / 2f);
                int wallType = ModContent.ProjectileType<WitchkingFireWall>();
                int wallDamage = EnemyDamage.Projectile(HellfireWallDamage);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), wallCenter, Vector2.Zero, wallType, wallDamage, 0f, Main.myPlayer, 1f, floorY);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), wallCenter, Vector2.Zero, wallType, wallDamage, 0f, Main.myPlayer, -1f, floorY);
            }
        }
        #endregion

        #region The Unmaking (death)
        // GRK's CheckDead pattern: lethal damage pins him at 1 HP, immune, and starts the set-piece; on the burst tick
        // the server lets him die, so OnKill, loot and the boss-down flags land with the skulls.
        public override bool CheckDead()
        {
            if (_unmakingComplete)
            {
                return base.CheckDead();
            }

            if (!_unmakingStarted)
            {
                _unmakingStarted = true;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                _pullTicksRemaining = 0;
                _pullPermanent = false;
                BeginSetPiece(CustomKind.Unmaking, UnmakingBuildTicks + UnmakingSlackTicks, MeleeWeaponItemType);
            }

            NPC.life = 1;
            return false;
        }

        // Arms spread to both sides, black dust spiralling in from a shrinking ring for 3s, light failing; then fifty
        // skulls 7.2° apart, the scream, and death on the same tick.
        private void TickUnmaking(int elapsed)
        {
            NPC.velocity.X = 0f;
            float build = MathHelper.Clamp(elapsed / (float)UnmakingBuildTicks, 0f, 1f);

            if (!Main.dedServ && elapsed < UnmakingBuildTicks)
            {
                if (elapsed == 0 || elapsed == 120)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.8f }, NPC.Center);
                }

                int count = 1 + (int)(5f * build);
                float radius = MathHelper.Lerp(240f, 30f, build);
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Vector2 inward = -offset * 0.04f + offset.RotatedBy(MathHelper.PiOver2) * 0.02f;
                    Dust darkness = Dust.NewDustPerfect(NPC.Center + offset, DustID.Wraith, inward, 100, Color.Black, 1.4f);
                    darkness.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, new Color(120, 60, 170).ToVector3() * (0.6f * (1f - build)));
            }

            if (elapsed != UnmakingBuildTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 60; i++)
                {
                    Dust burst = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Wraith, 0f, 0f, 60, Color.Black, 2f);
                    burst.velocity = Main.rand.NextVector2Circular(9f, 9f);
                    burst.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 450, 20);
            int skullDamage = EnemyDamage.Projectile(DeathSkullDamage);
            float step = MathHelper.TwoPi / DeathSkullCount;
            for (int i = 0; i < DeathSkullCount; i++)
            {
                float angle = i * step + Main.rand.NextFloat(-0.026f, 0.026f); // ±1.5° jitter
                Vector2 velocity = angle.ToRotationVector2() * WitchkingDeathSkull.Speed;
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, velocity,
                    ModContent.ProjectileType<WitchkingDeathSkull>(), skullDamage, 0f, Main.myPlayer);
            }

            _unmakingComplete = true;
            NPC.dontTakeDamage = false;
            NPC.life = 0;
            NPC.checkDead();
        }
        #endregion

        // The pull runs on each client for its OWN player (the owner of that velocity), only within 60 tiles. Same
        // formula as the legacy Pull(): stronger as he weakens and the further away you are; vertical pull only while
        // airborne (avoids the no-jump bug with Torn Wings).
        private void TickPull()
        {
            if (_pullTicksRemaining <= 0 && !_pullPermanent)
            {
                return;
            }

            if (_pullTicksRemaining > 0)
            {
                _pullTicksRemaining--;
            }

            if (Main.dedServ)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead || NPC.Distance(player.Center) > PullRadius)
            {
                return;
            }

            float xDiff = player.Center.X - NPC.Center.X;
            float yDiff = player.Center.Y - NPC.Center.Y;
            float xSign = Math.Sign(xDiff);
            float ySign = Math.Sign(yDiff);

            // 1.25 per shriek played (the legacy 1-5 range).
            float strength = 1.25f * _shrieksStarted;

            player.velocity.X -= (xDiff / 4000f + strength * 0.050f * xSign) * (1f + strength * 0.115f);
            if (player.velocity.Y != 0f)
            {
                player.velocity.Y -= yDiff / 800f + strength * 0.03f * ySign;
            }

            player.AddBuff(ModContent.BuffType<WitchkingsGrasp>(), 5, false);
            player.GetModPlayer<tsorcRevampPlayer>().PullStrength = (int)strength;

            // Dust streaming off the player toward him, denser as the pull strengthens.
            Rectangle playerRect = new Rectangle(0, 0, 20, 45);
            for (int x = 0; x < (int)strength; x++)
            {
                Vector2 dustPosition = player.position + Main.rand.NextVector2FromRectangle(playerRect);
                Vector2 velocity = new Vector2(10f, 0).RotatedBy(dustPosition.AngleTo(NPC.Center)) * Main.rand.NextFloat(1f, Math.Abs(xDiff) / 45f);
                dustPosition += velocity;
                Dust.NewDustPerfect(dustPosition, DustID.ShadowbeamStaff, velocity, Scale: Main.rand.NextFloat(0.8f, 1.5f)).noGravity = true;
            }
        }

        // Abyss: every client re-applies a short Abyss to its own player while he's alive and near, so it ends by
        // itself ~1s after he dies or despawns, clears on death and catches late joiners. Anti-flight on the
        // targeted player's own machine, GRK's pattern. The Covenant of Artorias still cures Abyss.
        private void ApplyFightCurses()
        {
            if (Main.dedServ)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead)
            {
                return;
            }

            float distance = NPC.Distance(player.Center);
            if (distance < AbyssCurseRadius)
            {
                player.AddBuff(ModContent.BuffType<Abyss>(), AbyssRefreshTicks);
            }

            if (NPC.target == Main.myPlayer && distance < AntiFlightRadius)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);
            }
        }

        // Identity at a glance: an ember off the fire tip (phase 1, and the re-formed blade of a plunge) and a wisp off
        // the void tip every few ticks.
        private void SpawnAmbientBladeDust()
        {
            if (Main.dedServ || Main.GameUpdateCount % 6 != 0)
            {
                return;
            }

            bool fireBladeOut = !_phaseTwo || _customKind == CustomKind.HellfirePlunge;
            if (fireBladeOut)
            {
                Vector2 fireTip = PuppetHandPosition + PuppetWeaponDirection * SwordTipReach;
                Dust ember = Dust.NewDustPerfect(fireTip, DustID.Torch, new Vector2(0f, -0.8f), 60, default, 1.1f);
                ember.noGravity = true;
            }

            Vector2 voidTip = PuppetBackHandPosition + PuppetBackWeaponDirection * SwordTipReach;
            Dust wisp = Dust.NewDustPerfect(voidTip, DustID.Shadowflame, new Vector2(0f, -0.6f), 60, default, 1f);
            wisp.noGravity = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_defenseBroken);
            writer.Write(_lastStand);
            writer.Write(_phaseTwo);
            writer.Write((byte)_shrieksQueued);
            writer.Write((byte)_shrieksStarted);
            writer.Write((byte)_customKind);
            writer.Write((short)_customTotalTicks);
            writer.Write((byte)_plungeWallCount);
            writer.WriteVector2(_summonPoint);
            writer.Write((short)_pullTicksRemaining);
            writer.Write(_pullPermanent);
            writer.Write(_unmakingStarted);
            writer.WriteVector2(_lockedWhipAim);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _defenseBroken = reader.ReadBoolean();
            _lastStand = reader.ReadBoolean();
            _phaseTwo = reader.ReadBoolean();
            _shrieksQueued = reader.ReadByte();
            _shrieksStarted = reader.ReadByte();
            _customKind = (CustomKind)reader.ReadByte();
            _customTotalTicks = reader.ReadInt16();
            _plungeWallCount = reader.ReadByte();
            _summonPoint = reader.ReadVector2();
            _pullTicksRemaining = reader.ReadInt16();
            _pullPermanent = reader.ReadBoolean();
            _unmakingStarted = reader.ReadBoolean();
            _lockedWhipAim = reader.ReadVector2();
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<WitchkingBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeSapphire));
            // One GuardianSoul rule (the legacy table added it twice, so a first non-expert kill dropped two).
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonAdventureModeRule, ModContent.ItemType<BrokenDarkMagicRing>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WitchkingsSword>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WitchkingMace>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BewitchedTitanite>(), 1, 15, 20));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CovenantOfArtoriasItem>()));
            npcLoot.Add(notExpertCondition);
        }
    }
}
