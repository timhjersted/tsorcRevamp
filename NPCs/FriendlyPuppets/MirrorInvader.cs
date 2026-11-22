using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;
using tsorcRevamp.Systems;
using tsorcRevamp.Systems.EstusFlask;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Content.Items.Accessories.Mobility.Wings;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Copies the triggering player's appearance and hotbar weapons, then adapts current named-invader timings to
    /// their real item use. Summoners unlock one-use echoes of defeated invaders at 90% health, excluding Kahlrun.
    ///
    /// Authority:
    ///   server — target, host, state, active weapon slot (shuffled bag), movement (SmartFighter4 waypoint mode), HP,
    ///            Estus, roll decisions.
    ///   host   — weapon use through the swap, stamina/mana pools; reports wrapper state back in OwnerFlags.
    ///   clients — display clone (the triggering player's look), tell visuals, flask/roll poses, sounds.
    ///
    /// ai[0] = host (MirrorPuppetNPC.OwnerIndex), ai[1] = triggering player (look + loadout source), ai[2] = target
    /// player (-1 none), ai[3] = <see cref="InvaderState"/>. Loadout, active slot and Estus charges ride in SendExtraAI.
    /// One primary echo lives at a time. Death returns a summon card until the finite defeated-invader pool is spent.
    /// </summary>
    [AutoloadBossHead]
    public class MirrorInvader : HostileMirrorPuppetNPC, IEnemyWhipOwner
    {
        // PLACEHOLDER sprite (copy of StuddedLeatherWarrior) — replace with bespoke art.
        // Overrides MirrorPuppetNPC's InvisibleNothingProj so NPC browsers and the Bestiary have a visible icon.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(MirrorInvader));

        // PLACEHOLDER sprite (copy of StuddedLeatherWarrior_Head_Boss) — replace with bespoke art.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(MirrorInvader)) + "_Head_Boss";

        private static readonly SoundStyle InvasionBannerSound = new SoundStyle("tsorcRevamp/Sounds/DarkSouls/invader-announcement") with { Volume = 0.7f };

        public enum InvaderState
        {
            Fight = 0,
            /// <summary>Lowering one weapon and raising the next: no attacks (the switch doubles as a tell, §12.10).</summary>
            SwitchingWeapon = 1,
            /// <summary>Wants Estus: stops attacking, backs off a crowding player, then drinks.</summary>
            PreparingEstus = 2,
            DrinkingEstus = 3,
            /// <summary>The player's roll: same length, speed and i-frames, paid from the invader's stamina.</summary>
            Rolling = 4,
            /// <summary>Nobody left to fight: sinks away and is removed. Invulnerable.</summary>
            Dissolving = 5,
            /// <summary>Raising a minion staff: a ghost add forms at the summon point, then arrives (§12.9).</summary>
            SummoningAdd = 6,
            Whipping = 7
        }

        /// <summary>Hotbar slots copied (all 10, §12.11 #2).</summary>
        private const int HotbarSlots = 10;

        // ── Tuning (ESTIMATE — first pass) ────────────────────────────────────────────────────────
        /// <summary>Authored life, used only where the EnemyBalance registry has no value (pre-Hardmode debug spawns).
        /// HM/SHM health lives in Systems/EnemyBalance.cs.</summary>
        private const int UnregisteredLifeMax = 20000;
        private const float KnockbackTaken = 0.4f;

        private const int WeaponSwitchTicks = 20;
        private const int RetargetIntervalTicks = 30;
        /// <summary>Players further than this are not hunted (90 tiles).</summary>
        private const float TargetLeash = 90f * 16f;
        /// <summary>A player who hit it within this long is preferred as the target ("last hit by").</summary>
        private const int LastHitMemoryTicks = 180;
        /// <summary>A different player at under this fraction of the current target's distance takes over as target.</summary>
        private const float RetargetCloserFraction = 0.5f;
        /// <summary>The host must stay within this distance (100 tiles) — its client simulates the invader's projectiles
        /// and terrain; past it the server hands the swap to the target or the nearest living player.</summary>
        private const float HostMaxDistance = 100f * 16f;
        /// <summary>No valid target for this long (10 s): the fight is over and it dissolves.</summary>
        private const int LostTargetDissolveTicks = 600;
        private const int DissolveTicks = 60;

        /// <summary>Neutral and pursuit speeds in px/tick; SF4 still solves terrain and jump paths.</summary>
        private const float MinTopSpeed = 5.5f;
        private const float MaxTopSpeed = 7.5f;
        private const float Acceleration = 0.22f;
        /// <summary>During a wrapper recovery it walks at this fraction of its speed (§12.6 "slow walk").</summary>
        private const float RecoveryWalkSpeedFraction = 0.4f;
        /// <summary>Planted while attacking: per-tick horizontal velocity kept during a tell or burst.</summary>
        private const float AttackingVelocityRetention = 0.85f;
        /// <summary>Stand this much further than a shot's minimum firing distance, so it isn't refused at the edge.</summary>
        private const float ShotDistanceMargin = 48f;
        private const float EngagementSlack = 12f;
        /// <summary>The waypoint is re-sent to SF4 only once the desired spot moves 2 tiles (SF4 replans on any move).</summary>
        private const float WaypointRefreshDistance = 32f;
        /// <summary>SmartFighter4 door value: opens doors, doesn't destroy them (SF4's own default).</summary>
        private const int DoorOpenValue = 4;
        /// <summary>SF4 A* search window (tiles; SF4 caps it at 80).</summary>
        private const int NavSearchRadiusTiles = 80;
        /// <summary>SF4 jump ceilings (launch / held boost), the same as PuppetNPC's defaults.</summary>
        private const float JumpPower = 10f;
        private const float JumpBoost = 6f;
        /// <summary>Too close for its shot this long, or out of reach with its weapon this long: switch weapon.</summary>
        private const int TooCloseSwitchTicks = 30;
        private const int OutOfReachSwitchTicks = 120;

        /// <summary>Winded below 15% of max stamina, fights again at 60% (same hysteresis as the Soul Mimic). While winded
        /// it backs off to 4.5 tiles if the target is within 3, else holds.</summary>
        private const float WindedEnterFraction = 0.15f;
        private const float WindedExitFraction = 0.6f;
        private const float WindedNearDistance = 3f * 16f;
        private const float WindedSettleDistance = 4.5f * 16f;

        /// <summary>Evasion: reacts to a given threat with this chance, one decision per lockout. Lower than the Soul
        /// Mimic's 0.75 — an enemy that rolls everything is a wall, not a duel.</summary>
        private const float EvadeChance = 0.55f;
        private const int EvadeDecisionLockoutTicks = 75;
        private const int EvadeRetryLockoutTicks = 30;
        /// <summary>A player within 8 tiles, facing it, mid-swing with a melee weapon, is a melee threat.</summary>
        private const float MeleeThreatRange = 8f * 16f;
        private const float JumpEvadeSpeed = 7.5f;
        private const float StrafeDistance = 5f * 16f;
        private const int StrafeTicks = 24;

        /// <summary>Estus (§12.4): at most 3 charges; each drink heals this fraction of max life over the player's
        /// default heal time. The server can't read a player's Estus stats, so these are fixed, not copied.</summary>
        private const int MaxEstusCharges = 3;
        private const float EstusHealLifeFraction = 0.12f;
        private const float EstusDrinkLifeFraction = 0.4f;
        private static readonly int EstusDrinkTicks = (int)(EstusFlaskPlayer.EstusDrinkTimerMaxBase * 60f);
        private static readonly int EstusHealTicks = (int)EstusFlaskPlayer.DefaultEstusHealingTimerMax;
        private const float EstusSpaceNearDistance = 4f * 16f;
        private const float EstusSpaceSettleDistance = 6f * 16f;
        private const int EstusSpaceTimeoutTicks = 150;
        private const float DrinkingVelocityRetention = 0.9f;
        private const float DrinkPoseStartFraction = 0.05f;

        // Summoner ritual: 48t raise + 12t sweep, then 30t harmless materialization on validated ground.
        // The 90% health unlock latches; later deaths return a summon card after a 90t replacement delay.
        private const int SummonHoldTicks = 48;
        private const int SummonSweepTicks = 12;
        private const int SummonCooldownTicks = 90;
        private const float SummonHealthFraction = 0.9f;
        private const float SummonPointDistance = 3f * 16f;
        /// <summary>Exact echo life in every difficulty, requested 2026-11-19.</summary>
        internal const int MirrorAddLife = 25000;
        private const int SummonGroundSearchTiles = 12;

        /// <summary>Fallback weapons when fewer than 2 usable ones were copied (§12.11 #5): always a melee and a ranged
        /// option. The pistol fires basic ammo (UsesBasicAmmo), so it works whatever the host carries.</summary>
        private static readonly int[] FallbackWeaponTypes = { ItemID.NightsEdge, ItemID.FlintlockPistol };
        private const int MinUsableWeapons = 2;

        /// <summary>OwnerFlags bits (host → server). Attacking = a tell or burst in progress (the wrapper's Tell/Using).</summary>
        private const byte OwnerFlagWinded = 1;
        private const byte OwnerFlagCanRoll = 2;
        private const byte OwnerFlagAttacking = 4;
        private const byte OwnerFlagTooClose = 8;
        private const byte OwnerFlagRecovering = 16;
        private const byte OwnerFlagUseFailed = 32;
        private const byte OwnerFlagSummoner = 64;
        private const byte OwnerFlagShotBlocked = 128;

        // Loadout (synced): item type + prefix per usable hotbar weapon. Items are rebuilt from them on every machine.
        private readonly int[] _loadoutTypes = new int[HotbarSlots];
        private readonly int[] _loadoutPrefixes = new int[HotbarSlots];
        private int _loadoutCount;
        private int _activeSlot;
        private Item[] _loadoutItems;
        private bool _loadoutItemsDirty = true;
        private int _estusCharges = MaxEstusCharges;

        // Minion staffs from the hotbar (synced): each one calls adds instead of being used. Which one the current cast
        // shows, and where its add arrives.
        private readonly int[] _summonStaffTypes = new int[HotbarSlots];
        private int _summonStaffCount;
        private int _summonStaffShown;
        private Vector2 _summonPoint;
        private Item _shownStaffItem;

        // Server only.
        private readonly List<int> _weaponBag = new List<int>();
        private int _retargetTimer;
        private int _lostTargetTicks;
        private int _lastLife;
        private int _lastHitPlayer = -1;
        private int _lastHitTicksAgo = int.MaxValue / 2;
        private bool _wasRecovering;
        private bool _wasUseFailed;
        private int _tooCloseTicks;
        private int _outOfReachTicks;
        private int _rollDirection;
        private int _rollCooldown;
        private int _evadeLockout;
        private int _strafeTicksLeft;
        private int _strafeDirection;
        private Vector2 _heldWaypoint;
        private bool _hasHeldWaypoint;
        private Vector2 _vantageWaypoint;
        private bool _hasVantageWaypoint;
        private int _vantageRefreshTicks;
        private Vector2 _vantageTargetPos;
        private int _estusHealTicksLeft;
        private float _estusHealPerTick;
        private float _estusHealAccumulator;
        private int _summonCooldown;
        private bool _summonsUnlocked;
        private bool _summonerEligible;
        private int _queuedInvaderType;
        private int _lastSummonedType;
        private readonly List<int> _echoBag = new List<int>();
        private bool _echoRosterInitialized;
        private NPC _activeEcho;
        private MirrorEchoNPC _activeEchoTag;
        private bool _summonCard;
        private bool _chooseAction = true;
        private int _actionSerial;
        internal int EncounterId;
        private int _hostRevision;
        private int _localHostRevision = -1;
        private int _pursuitTicks;
        private int _whipProjectile = -1;
        private Vector2 _whipAim = Vector2.UnitX;
        private int _whipRecovery;

        // ── Movement attacks and wing flight (ESTIMATE — first pass; ported from Owl Father's leaps and jump shot) ─────
        /// <summary>The movement a use is built on. Chosen by the server at the action boundary, synced, and tagged
        /// with <see cref="_moveSerial"/> so the owner's tell for it can be matched.</summary>
        private enum MoveAction : byte
        {
            None,
            /// <summary>Jump; a bow/gun/discrete spell releases at the apex.</summary>
            ApexShot,
            /// <summary>Low gap-closing hop with one swing on approach or landing.</summary>
            PursuitLeap,
            /// <summary>Tall committed leap below half health: one apex correction, faster fall, one swing.</summary>
            HighSlam,
            /// <summary>Flight: locked dive through the target's position with one swing, back to hover.</summary>
            AirPass,
            /// <summary>Flight: drop out of the wings into a solved dive and one swing; ends the sortie.</summary>
            AirSlam,
            /// <summary>Flight: a shot, cast, channel or whip from a station-kept hover.</summary>
            HoverAttack,
            /// <summary>Flight: a gliding strafe pass with a spaced shot burst.</summary>
            StrafeVolley
        }

        private enum MovePhase : byte
        {
            /// <summary>Waiting for, then running, the owner's tell. Ordinary planting/hovering applies.</summary>
            Tell,
            /// <summary>Launched: the jump, dive or pass owns velocity.</summary>
            Air,
            /// <summary>Touched down (or the pass ended): a short planted hold before normal behaviour resumes.</summary>
            Landed
        }

        /// <summary>Vanilla NPC gravity (px/tick²); every arc below is solved against it.</summary>
        private const float LeapGravity = 0.3f;
        /// <summary>Leaps land this far short of the target (Owl Father's standoff) and keep this terrain margin.</summary>
        private const float LeapLandingStandoff = 24f;
        private const float LeapTerrainMargin = 24f;
        private const float LeapMinForwardSpeed = 1.5f;
        /// <summary>Spear thrust reach for leap and pass admission (their projectile, not a drawn blade).</summary>
        private const float SpearStrikeReach = 140f;
        /// <summary>The swing starts once the target is within reach plus this many ticks of current approach speed.</summary>
        private const int LeapStrikeLeadTicks = 8;
        /// <summary>...or this many ticks before predicted touchdown, so a miss still swings on landing.</summary>
        private const int LeapTouchdownLeadTicks = 6;
        /// <summary>An action whose tell never starts (owner busy, out of reach) is dropped after this long.</summary>
        private const int MoveTellTimeoutTicks = 120;
        private const int MoveAirMaxTicks = 150;
        /// <summary>The owner may start a leap/pass/slam tell this far out; the server already checked admission.</summary>
        private const float MeleeMoveActionMaxDistance = 900f;
        /// <summary>Arc solver: ticks simulated, fall-speed cap and the deepest drop accepted below the launch (6 tiles).</summary>
        private const int ArcMaxTicks = 150;
        private const float ArcMaxFallSpeed = 16f;
        private const float ArcMaxDrop = 6f * 16f;

        // Apex shot: Owl Father's forced-jump thresholds (3 tiles above, or 1 tile with a blocked line), rise 3–7 tiles.
        private const float ApexForcedHeightAbove = 48f;
        private const float ApexForcedBlockedHeightAbove = 16f;
        private const float ApexMinRise = 48f;
        private const float ApexMaxRise = 112f;
        /// <summary>Random jump shots rise ~120px (Owl's 8.5 px/t launch, 28 ticks to the apex).</summary>
        private const float ApexRandomRise = 120f;
        private const int ApexRandomChancePercent = 15;
        private const float ApexMaxForwardSpeed = 5f;
        private const int ApexMinTellTicks = 30;

        // Pursuit jump-cut: 6 px/t launch = 60px apex, 40t flat airtime, up to 9 px/t forward (Owl's low pursuit slam).
        private const float PursuitLaunchSpeed = 6f;
        private const float PursuitMaxForwardSpeed = 9f;
        private const int PursuitTellTicks = 36;
        private const int PursuitLandingHoldTicks = 10;
        private const int PursuitRecoveryTicks = 30;
        private const int PursuitCooldownTicks = 150;
        private const int PursuitChancePercent = 60;
        /// <summary>Closer than reach + this, an ordinary cut is used instead of a leap.</summary>
        private const float PursuitMinExtraDistance = 48f;

        // High slam (below half health): 10.5 px/t launch = 184px apex, 35t up; the fall runs at 2x gravity (~25t).
        private const float HighSlamLaunchSpeed = 10.5f;
        private const float HighSlamMaxForwardSpeed = 6f;
        private const float HighSlamDescentGravityMult = 2f;
        private const int HighSlamTellTicks = 54;
        private const int HighSlamLandingHoldTicks = 24;
        private const int HighSlamRecoveryTicks = 40;
        private const int HighSlamCooldownTicks = 360;
        private const float HighSlamMinDistance = 160f;
        private const int HighSlamChancePercent = 50;
        private const float HighSlamHealthFraction = 0.5f;
        /// <summary>Leaps need the target roughly level: within 6 tiles vertically.</summary>
        private const float LeapMaxHeightGap = 96f;

        // Flight budget from the copied wings: 240 + 2 x fly time, clamped to 300–600 ticks (fly time capped at 180).
        private const int FlightTicksBase = 240;
        private const int FlightTicksPerFlyTime = 2;
        private const int MinFlightTicks = 300;
        private const int MaxFlightTicks = 600;
        private const int DefaultWingFlyTime = 100;
        private const int MaxCountedWingFlyTime = 180;
        private const int FlightCooldownTicks = 240;

        // Flight decisions (server).
        private const int TakeoffCheckIntervalTicks = 30;
        private const int RandomTakeoffPercent = 8;
        private const float TakeoffTargetAboveDistance = 6f * 16f;
        private const int TakeoffTargetAirborneTicks = 45;
        private const int SortieMaxAttacks = 2;
        private const int FlightLowBudgetTicks = 120;
        private const int AirIdleLandTicks = 150;
        private const int NoSightLandTicks = 120;
        private const int AirActionBudgetMargin = 30;
        private const int AirActionRecheckTicks = 10;
        /// <summary>Budget a hover shot/cast/channel needs left: the longest memory tell (75) + its use + margin.</summary>
        private const int HoverAttackBudgetTicks = 150;
        /// <summary>After a wing landing: 20 ticks planted, then 30 at the recovery walk speed.</summary>
        private const int LandingPlantedTicks = 20;
        private const int LandingSlowWalkTicks = 30;

        // Hover placement per weapon family (px above / beside the target).
        private const float MeleeHoverAltitude = 150f;
        private const float MeleeHoverSideOffset = 140f;
        private const float RangedHoverAltitude = 170f;
        private const float RangedHoverMinSideOffset = 120f;
        private const float RangedHoverMaxSideOffset = 300f;
        private const float WhipHoverAltitude = 110f;
        private const float WhipHoverSideOffset = 200f;
        /// <summary>The whip is only used from a hover at most this high above the target.</summary>
        private const float AerialWhipMaxAltitude = 160f;

        // Air pass: 40t hover tell, dive to 120px past the target (kept 32px above its feet), one swing within reach.
        private const int AirPassTellTicks = 40;
        private const float AirPassOvershoot = 120f;
        private const float AirPassFeetClearance = 32f;
        private const float AirPassMinDistance = 120f;
        private const float AirPassMaxDistance = 420f;
        private const int AirPassStrikeLeadTicks = 6;
        private const int AirPassRecoveryTicks = 30;
        private const int AirPassHoverHoldTicks = 30;

        // Air-to-ground slam: Owl Father's Greatfire Dive (43t tell, ~9 px/t dive, 16–40t airtime).
        private const int AirSlamTellTicks = 43;
        private const float AirSlamMinDistance = 100f;
        private const float AirSlamMaxDistance = 360f;
        private const float AirSlamDiveSpeed = 9f;
        private const float AirSlamMinDiveTicks = 16f;
        private const float AirSlamMaxDiveTicks = 40f;
        private const int AirSlamLandingHoldTicks = 10;
        private const int AirSlamRecoveryTicks = 30;

        // Strafe volley: 34t hover tell, glide 240px past the target, 3 shots 16t apart.
        private const int StrafeVolleyTellTicks = 34;
        private const int StrafeVolleyShots = 3;
        private const int StrafeVolleyGapTicks = 16;
        private const int StrafeVolleyRecoveryTicks = 36;
        private const float StrafeVolleyReach = 480f;
        private const float StrafeVolleyPassOffset = 240f;
        private const int StrafeVolleyChancePercent = 35;

        // Wings (synced): the functional pair's equip slot (0 = cannot fly) and the pair the player shows (0 = none).
        private int _wingFunctionalSlot;
        private int _wingDisplaySlot;
        private EnemyFlightController _flight;

        // Movement action (synced): what, which serial, which phase, the locked horizontal launch speed, the fall boost.
        private MoveAction _moveAction;
        private int _moveSerial = -1;
        private MovePhase _movePhase;
        private float _moveLaunchVx;
        private float _moveDescentGravityMult = 1f;
        // Wing-landing punish window (synced; counts down on every machine).
        private int _landingRecoveryTicks;

        // Server only.
        private int _movePhaseTicks;
        private int _moveTicksToApex;
        private bool _moveApexRetargeted;
        private bool _lastRangedMoveWasJump;
        private int _pursuitCooldown;
        private int _highSlamCooldown;
        private int _sortieAttacks;
        private bool _sortieChannelUsed;
        private int _takeoffCheckTimer;
        private int _targetAirborneTicks;
        private int _noSightTicks;
        private int _airIdleTicks;
        private bool _flightWasAirborne;

        // Owner side, streamed to the server: the move serial the owner's current tell was started for.
        private int _ownerTellSerial = -1;

        private enum CombatMemory : byte
        {
            LumeliaQuickslash,
            LumeliaUnderOver,
            MarikHeavy,
            MarikFury,
            WarriorCrossbow,
            OwlBow,
            RangedQuickVolley,
            RangedFlurry,
            NinjaCast,
            MarikCast,
            DarkHarvest,
            SummonEcho
        }

        private CombatMemory _memory;
        private readonly List<CombatMemory> _attackBag = new List<CombatMemory>();
        private CombatMemory _previousMemory;
        private bool _hasPreviousMemory;
        private int _memoryStartUses;
        private int _lastChargedUse;
        private int _lastChargedWhipSerial = -1;
        internal override bool UsesFixedCombatResources => true;
        protected override bool LogsMeleeClock => ModContent.GetInstance<tsorcRevampConfig>().DebugMode;

        public Vector2 WhipAnchor
        {
            get
            {
                if (DisplayClone != null)
                {
                    Vector2 hand = DisplayClone.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full,
                        _whipAim.ToRotation() - MathHelper.PiOver2);
                    return NPC.Center + hand - DisplayClone.Center;
                }

                return NPC.Center + new Vector2(NPC.direction * 12f, -8f);
            }
        }

        public Vector2 WhipAim => _whipAim;
        public bool IsWhipActive => NPC.active && State == InvaderState.Whipping;

        // All machines: a local timer for the current state (reset when ai[3] changes).
        private InvaderState _lastSeenState = InvaderState.Fight;
        private int _stateTimer;
        private int _receivedStateTimer = -1;

        // Host only.
        private InvaderState _ownerLastState = InvaderState.Fight;

        public InvaderState State
        {
            get
            {
                return (InvaderState)(int)NPC.ai[3];
            }
            set
            {
                NPC.ai[3] = (int)value;
            }
        }

        private int TriggerIndex => (int)NPC.ai[1];

        private int TargetIndex => (int)NPC.ai[2];

        protected override int LookSourceIndex => TriggerIndex;

        // Sinking away: the ground must hide the buried part even under Hunter vision.
        internal override bool StaysBehindSolidTiles => State == InvaderState.Dissolving;

        // Server: whether the loadout has been copied from a player (SpawnFor, or the first AI tick of a manual spawn).
        private bool _loadoutSnapshotted;

        protected override Item ActiveWeapon
        {
            get
            {
                if (_loadoutItemsDirty || _loadoutItems == null)
                {
                    // Rebuilt only when the synced loadout changes, so the wrapper's per-item state (its "weapon changed"
                    // check, the muted sound) keeps pointing at the same Item objects.
                    _loadoutItems = new Item[HotbarSlots];

                    for (int slot = 0; slot < _loadoutCount; slot++)
                    {
                        Item copy = new Item();
                        copy.SetDefaults(_loadoutTypes[slot]);
                        copy.Prefix(_loadoutPrefixes[slot]);
                        _loadoutItems[slot] = copy;
                    }

                    _loadoutItemsDirty = false;
                }

                bool slotValid = _activeSlot >= 0 && _activeSlot < _loadoutCount;

                if (!slotValid)
                {
                    return null;
                }

                return _loadoutItems[_activeSlot];
            }
        }

        public override void SetStaticDefaults()
        {
            // Intentionally does not call base.SetStaticDefaults(), which hides friendly/test mirror puppets from the Bestiary.
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.lifeMax = UnregisteredLifeMax;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.dontTakeDamage = false;
            NPC.immortal = false;
            NPC.HitSound = SoundID.PlayerHit;
            NPC.DeathSound = SoundID.PlayerKilled;
            NPC.knockBackResist = KnockbackTaken;
            NPC.value = 0f;
            NPC.ai[2] = -1f;

            // SmartFighter4 navigation. NavSearchRadius is the A* window in tiles: left at its default 0, SF4 clamps
            // it to 1, finds no span near any waypoint, reports Blocked and brakes, so the invader stood still.
            // Ropes are a per-NPC opt-in (doors come from the value passed to Run). Jump numbers match puppets.
            tsorcRevampGlobalNPC navigation = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            navigation.NavSearchRadius = NavSearchRadiusTiles;
            navigation.CanUseRopes = true;
            navigation.MaxJumpPower = JumpPower;
            navigation.MaxJumpBoost = JumpBoost;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            // EnemyBalanceNPC skips npc.boss; apply this invader's EnemyBalance registry HP here with multiplayer scaling.
            if (EnemyBalance.TryGetEnemyExpertHp(Type, out int expertHp))
            {
                NPC.lifeMax = (int)Math.Round(EnemyBalance.ModeScaledLife(NPC, expertHp) * balance);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                return true;
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        // ── Spawning (server / single player) ─────────────────────────────────────────────────────

        /// <summary>Spawns an invader copying <paramref name="trigger"/> at <paramref name="feetPosition"/> (bottom centre).
        /// Server or single player only. The step-7 event ring will call this; for now /mimictest invader does.</summary>
        public static int SpawnFor(Player trigger, Vector2 feetPosition)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int invaderType = ModContent.NPCType<MirrorInvader>();
            int npcIndex = NPC.NewNPC(trigger.GetSource_FromThis(), (int)feetPosition.X, (int)feetPosition.Y, invaderType, 0, trigger.whoAmI, trigger.whoAmI, trigger.whoAmI, (int)InvaderState.Fight);

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                return -1;
            }

            NPC npc = Main.npc[npcIndex];
            MirrorInvader invader = (MirrorInvader)npc.ModNPC;
            invader.SnapshotFrom(trigger);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
            }

            return npcIndex;
        }

        /// <summary>
        /// Server: copy <paramref name="trigger"/>'s fight — every usable hotbar weapon as type + prefix (§12.8 filter), the
        /// minion staffs apart (they call adds, §12.9), the fallbacks if fewer than 2 weapons (§12.11 #5), their defense —
        /// and make them the trigger (look), host and target. Callers: SpawnFor, and the first AI tick of an invader
        /// spawned some other way (a cheat/spawner mod's plain NewNPC), so a manual spawn is armed too.
        /// </summary>
        private void SnapshotFrom(Player trigger)
        {
            EncounterId = MirrorEchoSystem.NextEncounterId++;
            _loadoutSnapshotted = true;
            _loadoutCount = 0;
            _summonStaffCount = 0;
            _summonerEligible = HasSummonerLoadout(trigger);
            NPC.ai[0] = trigger.whoAmI;
            NPC.ai[1] = trigger.whoAmI;
            NPC.ai[2] = trigger.whoAmI;

            // Every usable hotbar weapon (§12.8 filter), as type + prefix. Ammo weapons fire the basic ammo of their type
            // (UsesBasicAmmo), so they only need one to exist. Minion staffs are kept apart: they call adds (§12.9).
            for (int slot = 0; slot < HotbarSlots; slot++)
            {
                Item item = trigger.inventory[slot];
                WeaponCategory category = Categorize(item);

                if (category == WeaponCategory.SummonStaff)
                {
                    _summonStaffTypes[_summonStaffCount] = item.type;
                    _summonStaffCount++;
                    continue;
                }

                if (category == WeaponCategory.Unusable)
                {
                    continue;
                }

                if (item.useAmmo > 0 && MirrorPuppetSwap.BasicAmmoFor(item.useAmmo) == null)
                {
                    continue;
                }

                _loadoutTypes[_loadoutCount] = item.type;
                _loadoutPrefixes[_loadoutCount] = item.prefix;
                _loadoutCount++;
            }

            // Fewer than 2 usable weapons (a pure summoner, an empty hotbar): add the fallbacks it doesn't already have.
            if (_loadoutCount < MinUsableWeapons)
            {
                for (int i = 0; i < FallbackWeaponTypes.Length; i++)
                {
                    int fallbackType = FallbackWeaponTypes[i];
                    bool alreadyHas = Array.IndexOf(_loadoutTypes, fallbackType, 0, _loadoutCount) >= 0;

                    if (alreadyHas || _loadoutCount >= HotbarSlots)
                    {
                        continue;
                    }

                    _loadoutTypes[_loadoutCount] = fallbackType;
                    _loadoutPrefixes[_loadoutCount] = 0;
                    _loadoutCount++;
                }
            }

            _loadoutItemsDirty = true;
            _activeSlot = 0;
            _estusCharges = MaxEstusCharges;

            // The copied player's defense; health comes from the EnemyBalance registry (SetDefaults).
            NPC.defense = trigger.statDefense;
            NPC.defDefense = trigger.statDefense;
            NPC.direction = trigger.Center.X < NPC.Center.X ? -1 : 1;
            _lastLife = NPC.life;

            // ── Wings: the FUNCTIONAL pair decides flight; the SHOWN pair is what the player displays ──
            // Vanilla: functional accessory slots 3–9 (later slots win, hidden ones don't show), vanity 13–19 override
            // the look. The Wing Harness slot (SupersonicWingSlot) overrides both when enabled, as its ApplyEquipEffects
            // and tsorcRevampPlayer.UpdateVisibleAccessories do for the real player. Equipment items are synced, so the
            // server reads them directly rather than the player's client-resolved wingsLogic / wings.
            _wingFunctionalSlot = 0;
            _wingDisplaySlot = 0;

            for (int slot = 3; slot < 10; slot++)
            {
                Item accessory = trigger.armor[slot];
                bool wearable = trigger.IsItemSlotUnlockedAndUsable(slot) && !accessory.IsAir && accessory.wingSlot > 0;

                if (!wearable)
                {
                    continue;
                }

                if (_wingFunctionalSlot == 0)
                {
                    _wingFunctionalSlot = accessory.wingSlot;
                }

                if (!trigger.hideVisibleAccessory[slot])
                {
                    _wingDisplaySlot = accessory.wingSlot;
                }
            }

            for (int slot = 13; slot < 20; slot++)
            {
                Item vanity = trigger.armor[slot];

                if (trigger.IsItemSlotUnlockedAndUsable(slot) && !vanity.IsAir && vanity.wingSlot > 0)
                {
                    _wingDisplaySlot = vanity.wingSlot;
                }
            }

            ModAccessorySlot harness = LoaderManager.Get<AccessorySlotLoader>().Get(ModContent.GetInstance<SupersonicWingSlot>().Type, trigger);

            if (harness != null && harness.IsEnabled())
            {
                Item harnessWings = harness.FunctionalItem;
                Item harnessVanity = harness.VanityItem;
                bool harnessWorn = harnessWings != null && !harnessWings.IsAir && harnessWings.wingSlot > 0;
                bool harnessVanityWorn = harnessVanity != null && !harnessVanity.IsAir && harnessVanity.wingSlot > 0;

                if (harnessWorn)
                {
                    _wingFunctionalSlot = harnessWings.wingSlot;
                    _wingDisplaySlot = 0;

                    if (harnessVanityWorn)
                    {
                        _wingDisplaySlot = harnessVanity.wingSlot;
                    }
                    else if (!harness.HideVisuals)
                    {
                        _wingDisplaySlot = harnessWings.wingSlot;
                    }
                }
            }

            NPC.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(EncounterId);
            writer.Write(_hostRevision);
            writer.Write((byte)_memory);
            writer.Write(_actionSerial);
            writer.Write(_chooseAction);
            writer.Write(_whipRecovery);
            writer.Write(_whipAim.X);
            writer.Write(_whipAim.Y);
            writer.Write((byte)_loadoutCount);

            for (int slot = 0; slot < _loadoutCount; slot++)
            {
                writer.Write(_loadoutTypes[slot]);
                writer.Write((byte)_loadoutPrefixes[slot]);
            }

            writer.Write((byte)_activeSlot);
            writer.Write((byte)_estusCharges);

            writer.Write((byte)_summonStaffCount);

            for (int i = 0; i < _summonStaffCount; i++)
            {
                writer.Write(_summonStaffTypes[i]);
            }

            writer.Write((byte)_summonStaffShown);
            writer.Write(_summonPoint.X);
            writer.Write(_summonPoint.Y);
            writer.Write(_stateTimer);

            // Wings, the current movement action and the flight controller, read back in this order.
            writer.Write(_wingFunctionalSlot);
            writer.Write(_wingDisplaySlot);
            writer.Write((byte)_moveAction);
            writer.Write(_moveSerial);
            writer.Write((byte)_movePhase);
            writer.Write(_moveLaunchVx);
            writer.Write(_moveDescentGravityMult);
            writer.Write((short)_landingRecoveryTicks);

            bool hasFlight = _flight != null;
            writer.Write(hasFlight);

            if (hasFlight)
            {
                _flight.WriteNetworkState(writer);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            EncounterId = reader.ReadInt32();
            _hostRevision = reader.ReadInt32();
            _memory = (CombatMemory)reader.ReadByte();
            _actionSerial = reader.ReadInt32();
            _chooseAction = reader.ReadBoolean();
            _whipRecovery = reader.ReadInt32();
            _whipAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            int count = reader.ReadByte();
            bool loadoutChanged = count != _loadoutCount;

            for (int slot = 0; slot < count; slot++)
            {
                int type = reader.ReadInt32();
                int prefix = reader.ReadByte();
                bool slotChanged = slot >= HotbarSlots || type != _loadoutTypes[slot] || prefix != _loadoutPrefixes[slot];

                if (slot < HotbarSlots)
                {
                    _loadoutTypes[slot] = type;
                    _loadoutPrefixes[slot] = prefix;
                }

                loadoutChanged = loadoutChanged || slotChanged;
            }

            _loadoutCount = Math.Min(count, HotbarSlots);
            _activeSlot = reader.ReadByte();
            _estusCharges = reader.ReadByte();

            int staffCount = reader.ReadByte();

            for (int i = 0; i < staffCount; i++)
            {
                int staffType = reader.ReadInt32();

                if (i < HotbarSlots)
                {
                    _summonStaffTypes[i] = staffType;
                }
            }

            _summonStaffCount = Math.Min(staffCount, HotbarSlots);
            _summonStaffShown = reader.ReadByte();

            // Pixel coordinates can exceed a short on large worlds; preserve the actual arrival spot on clients.
            float summonX = reader.ReadSingle();
            float summonY = reader.ReadSingle();
            _summonPoint = new Vector2(summonX, summonY);
            _receivedStateTimer = reader.ReadInt32();

            _wingFunctionalSlot = reader.ReadInt32();
            _wingDisplaySlot = reader.ReadInt32();
            _moveAction = (MoveAction)reader.ReadByte();
            _moveSerial = reader.ReadInt32();
            _movePhase = (MovePhase)reader.ReadByte();
            _moveLaunchVx = reader.ReadSingle();
            _moveDescentGravityMult = reader.ReadSingle();
            _landingRecoveryTicks = reader.ReadInt16();
            bool hasFlight = reader.ReadBoolean();

            if (hasFlight)
            {
                _flight ??= new EnemyFlightController(BuildFlightConfig());
                _flight.ReadNetworkState(reader);
            }

            if (loadoutChanged)
            {
                _loadoutItemsDirty = true;
            }
        }

        public override void OnKill()
        {
            RemoveAdds();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<MirrorInvader>());

            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        /// <summary>Server: its ghost adds go with it (dead or dissolved). Removed, not killed, so they drop nothing.
        /// Callers: OnKill and the end of Dissolving.</summary>
        private void RemoveAdds()
        {
            MirrorEchoSystem.RemoveFamily(this);
            _activeEcho = null;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Guaranteed (§12.11 #4).
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MimicRune>()));
        }

        // ── AI (server decides; every machine runs the local state timer and visuals) ──────────────

        public override void AI()
        {
            base.AI();

            // First-spawn invasion banner: localAI[0] is unsynced, so every client announces once independently.
            if (Main.netMode != NetmodeID.Server && NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = 1f;
                tsorcRevamp.ShowAnnouncementBanner(
                    Language.GetTextValue("Mods.tsorcRevamp.EncounterPresentation.Banners.InvadedBy", DisplayName.Value),
                    Color.White);
                SoundEngine.PlaySound(InvasionBannerSound);
            }

            InvaderState state = State;

            if (state != _lastSeenState)
            {
                bool startedRoll = state == InvaderState.Rolling && !Main.dedServ;
                bool finishedEstus = _lastSeenState == InvaderState.DrinkingEstus && !Main.dedServ;
                bool startedDissolve = state == InvaderState.Dissolving && !Main.dedServ;

                if (startedRoll)
                {
                    // The player's own roll thud (tsorcRevampPlayer.TryStartDodgeroll).
                    SoundStyle rollSound = new SoundStyle("tsorcRevamp/Sounds/DarkSouls/roll1")
                    {
                        Volume = 0.1f,
                        PitchVariance = 0.4f
                    };
                    SoundEngine.PlaySound(rollSound, NPC.Center);
                }

                if (finishedEstus)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item3, NPC.Center);
                }

                if (startedDissolve)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.5f }, NPC.Center);
                }

                bool startedSummon = state == InvaderState.SummoningAdd && !Main.dedServ;
                bool finishedSummon = _lastSeenState == InvaderState.SummoningAdd && !Main.dedServ;

                if (startedSummon)
                {
                    SoundEngine.PlaySound(SoundID.Item44 with { Pitch = -0.3f }, NPC.Center);
                }

                // The add arrives: a burst of ghost dust where it formed (the add's own spawn is a plain NewNPC).
                if (finishedSummon)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = 0.4f, Volume = 0.7f }, _summonPoint);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 burstVelocity = Main.rand.NextVector2Circular(4f, 4f);
                        Dust burst = Dust.NewDustPerfect(_summonPoint + new Vector2(0f, -21f), DustID.DungeonSpirit, burstVelocity, 0, default, 1.5f);
                        burst.noGravity = true;
                    }
                }

                _lastSeenState = state;
                _stateTimer = Math.Max(0, _receivedStateTimer);
                _receivedStateTimer = -1;
            }
            else if (_receivedStateTimer >= 0)
            {
                _stateTimer = _receivedStateTimer;
                _receivedStateTimer = -1;
            }
            else
            {
                _stateTimer++;
            }

            bool rolling = state == InvaderState.Rolling;
            bool dissolving = state == InvaderState.Dissolving;

            // A roll is immune for its whole length, like the player's; dissolving can't be hit either.
            NPC.dontTakeDamage = rolling || dissolving;
            NPC.behindTiles = dissolving;

            if (dissolving && !Main.dedServ)
            {
                // Sinks into a dark pool, with silver glints: the reflection going out.
                Vector2 poolPosition = NPC.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), -2f);
                Dust poolDust = Dust.NewDustPerfect(poolPosition, DustID.Asphalt, new Vector2(0f, -0.2f), 0, default, 1.3f);
                poolDust.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    Dust glint = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.SilverCoin, 0f, -1f, 0, default, 0.8f);
                    glint.noGravity = true;
                }
            }

            // Summon telegraph (every machine): ghost dust spiralling in on the summon point, denser as the cast goes on, so
            // the arrival spot is readable before anything is there (attack-quality-pass §7).
            if (state == InvaderState.SummoningAdd && !Main.dedServ)
            {
                float castProgress = Math.Clamp(_stateTimer / (float)(SummonHoldTicks + SummonSweepTicks), 0f, 1f);
                int gatherCount = 1 + (int)(castProgress * 3f);
                Vector2 formCenter = _summonPoint + new Vector2(0f, -21f);

                for (int i = 0; i < gatherCount; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(40f, 40f) * (1f - castProgress * 0.5f);
                    Vector2 inward = -offset * 0.08f;
                    Dust gather = Dust.NewDustPerfect(formCenter + offset, DustID.DungeonSpirit, inward, 0, default, 1.1f);
                    gather.noGravity = true;
                }
            }

            // ── Every machine: wing flight and launched movement attacks own velocity, so clients don't drift between
            // syncs. noGravity and the flight steering aren't in vanilla's NPC sync; the controller sets them each tick. ──
            if (_wingFunctionalSlot > 0)
            {
                _flight ??= new EnemyFlightController(BuildFlightConfig());

                // Hover placement for the held weapon, from synced state so clients steer like the server: melee hangs
                // close above for a pass, whips low, shots and casts out at their standing distance (70% of it sideways).
                Item heldWeapon = ActiveWeapon;
                float hoverAltitude = MeleeHoverAltitude;
                float hoverSideOffset = MeleeHoverSideOffset;

                if (heldWeapon != null)
                {
                    WeaponCategory heldCategory = Categorize(heldWeapon);

                    if (heldCategory == WeaponCategory.Whip)
                    {
                        hoverAltitude = WhipHoverAltitude;
                        hoverSideOffset = WhipHoverSideOffset;
                    }
                    else if (heldCategory != WeaponCategory.SwingMelee && !IsSpear(heldWeapon))
                    {
                        GetCombatEngagementRange(heldWeapon, out float heldPreferredDistance, out _);
                        hoverAltitude = RangedHoverAltitude;
                        hoverSideOffset = MathHelper.Clamp(heldPreferredDistance * 0.7f, RangedHoverMinSideOffset, RangedHoverMaxSideOffset);
                    }
                }

                _flight.Config.HoverAltitude = hoverAltitude;
                _flight.Config.HoverSideOffset = hoverSideOffset;
                int flightTargetIndex = Math.Clamp(TargetIndex, 0, Main.maxPlayers - 1);
                _flight.Tick(NPC, Main.player[flightTargetIndex]);
            }

            bool ballisticAction = _moveAction == MoveAction.ApexShot || _moveAction == MoveAction.PursuitLeap
                || _moveAction == MoveAction.HighSlam || _moveAction == MoveAction.AirSlam;

            if (ballisticAction && _movePhase == MovePhase.Air)
            {
                // Locked horizontal speed; past the apex the high slam falls at boosted gravity (vanilla adds the base 0.3).
                NPC.velocity.X = _moveLaunchVx;
                bool falling = NPC.velocity.Y >= 0f;

                if (falling && _moveDescentGravityMult > 1f)
                {
                    NPC.velocity.Y += LeapGravity * (_moveDescentGravityMult - 1f);
                }
            }

            if (_landingRecoveryTicks > 0)
            {
                _landingRecoveryTicks--;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Spawned without SpawnFor (a spawner mod, /npc): copy the nearest living player now. ai[] is all 0 then, so
            // without this it would be an unarmed copy of player 0.
            if (!_loadoutSnapshotted)
            {
                Player nearestPlayer = null;
                float nearestPlayerDistance = float.MaxValue;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];

                    if (!candidate.active || candidate.dead)
                    {
                        continue;
                    }

                    float candidateDistance = Vector2.Distance(candidate.Center, NPC.Center);

                    if (candidateDistance < nearestPlayerDistance)
                    {
                        nearestPlayerDistance = candidateDistance;
                        nearestPlayer = candidate;
                    }
                }

                if (nearestPlayer == null)
                {
                    return;
                }

                SnapshotFrom(nearestPlayer);
            }

            if (dissolving)
            {
                NPC.velocity.X = 0f;

                // Folds its wings and drops to sink away.
                if (_flight != null && _flight.IsAirborne)
                {
                    _flight.EndFlightNow();
                    NPC.netUpdate = true;
                }

                if (_stateTimer >= DissolveTicks)
                {
                    RemoveAdds();
                    NPC.active = false;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }

                return;
            }

            // ── Who hit it last (NPC.lastInteraction reaches the server with every player strike) ──
            _lastHitTicksAgo++;

            if (NPC.life < _lastLife)
            {
                int hitter = NPC.lastInteraction;
                bool hitterValid = hitter >= 0 && hitter < Main.maxPlayers;

                if (hitterValid)
                {
                    _lastHitPlayer = hitter;
                    _lastHitTicksAgo = 0;
                }
            }

            _lastLife = NPC.life;

            // ── Target: keep the current one; on the retarget timer prefer whoever just hit it, else a much nearer player ──
            Player target = null;
            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxPlayers;

            if (targetIndexValid)
            {
                target = Main.player[TargetIndex];
            }

            bool targetValid = target != null && target.active && !target.dead && Vector2.Distance(target.Center, NPC.Center) <= TargetLeash;

            if (!targetValid)
            {
                target = null;
            }

            _retargetTimer--;

            if (target == null || _retargetTimer <= 0)
            {
                _retargetTimer = RetargetIntervalTicks;
                Player nearest = null;
                float nearestDistance = TargetLeash;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];

                    if (!candidate.active || candidate.dead)
                    {
                        continue;
                    }

                    float candidateDistance = Vector2.Distance(candidate.Center, NPC.Center);

                    if (candidateDistance < nearestDistance)
                    {
                        nearestDistance = candidateDistance;
                        nearest = candidate;
                    }
                }

                Player recentHitter = null;
                bool hitterRemembered = _lastHitPlayer >= 0 && _lastHitTicksAgo <= LastHitMemoryTicks;

                if (hitterRemembered)
                {
                    Player hitterPlayer = Main.player[_lastHitPlayer];
                    bool hitterAvailable = hitterPlayer.active && !hitterPlayer.dead && Vector2.Distance(hitterPlayer.Center, NPC.Center) <= TargetLeash;

                    if (hitterAvailable)
                    {
                        recentHitter = hitterPlayer;
                    }
                }

                if (target == null)
                {
                    target = recentHitter ?? nearest;
                }
                else if (recentHitter != null)
                {
                    target = recentHitter;
                }
                else if (nearest != null && nearest != target)
                {
                    float currentDistance = Vector2.Distance(target.Center, NPC.Center);

                    if (nearestDistance < currentDistance * RetargetCloserFraction)
                    {
                        target = nearest;
                    }
                }
            }

            int newTargetIndex = -1;

            if (target != null)
            {
                newTargetIndex = target.whoAmI;
            }

            if (newTargetIndex != TargetIndex)
            {
                NPC.ai[2] = newTargetIndex;
                NPC.netUpdate = true;
            }

            // Nobody left to fight (everyone dead, gone or out of range) for long enough: the fight is over.
            if (target == null)
            {
                _lostTargetTicks++;

                if (_moveAction != MoveAction.None)
                {
                    EndMoveAction();
                }

                if (_flight != null && _flight.IsAirborne)
                {
                    _flight.RequestLand();
                    NPC.netUpdate = true;
                    return;
                }

                NPC.velocity.X *= 0.8f;

                if (_lostTargetTicks >= LostTargetDissolveTicks)
                {
                    State = InvaderState.Dissolving;
                    NPC.netUpdate = true;
                }

                return;
            }

            _lostTargetTicks = 0;

            // ── Host: the client that runs the swap. Re-assigned if it died, left or is too far to simulate it ──
            Player host = Main.player[OwnerIndex];
            bool hostValid = host.active && !host.dead && Vector2.Distance(host.Center, NPC.Center) <= HostMaxDistance;

            if (!hostValid)
            {
                // Old-owner holdouts cannot transfer their Player identity. Retire that action and give the new
                // host a full switch/tell instead of replaying an attack half-way through its native animation.
                foreach (Projectile projectile in Main.ActiveProjectiles)
                {
                    MirrorPuppetProjectile tag = projectile.GetGlobalProjectile<MirrorPuppetProjectile>();

                    if (tag.FromMirrorPuppet && tag.PuppetNpcIndex == NPC.whoAmI)
                    {
                        projectile.damage = 0;
                        projectile.Kill();

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.KillProjectile, -1, -1, null,
                                projectile.identity, projectile.owner);
                        }
                    }
                }

                NPC.ai[0] = target.whoAmI;
                _hostRevision++;
                OwnerFlags = 0;
                ResetUseWrapper();

                // The new host never fires a stale apex shot or leap swing: the action is dropped, the flight continues.
                if (_moveAction != MoveAction.None)
                {
                    EndMoveAction();
                }

                State = InvaderState.SwitchingWeapon;
                _chooseAction = true;
                NPC.netUpdate = true;
                return;
            }

            // ── Timers ──
            if (_rollCooldown > 0)
            {
                _rollCooldown--;
            }

            if (_evadeLockout > 0)
            {
                _evadeLockout--;
            }

            if (_strafeTicksLeft > 0)
            {
                _strafeTicksLeft--;
            }

            if (_pursuitCooldown > 0)
            {
                _pursuitCooldown--;
            }

            if (_highSlamCooldown > 0)
            {
                _highSlamCooldown--;
            }

            // A target off the ground this long is flying (or falling far), not just hopping.
            if (target.velocity.Y != 0f)
            {
                _targetAirborneTicks++;
            }
            else
            {
                _targetAirborneTicks = 0;
            }

            // A wing landing that wasn't a slam opens the punish window: planted, then the slow recovery walk.
            bool airborneNow = _flight != null && _flight.IsAirborne;
            bool justLanded = _flightWasAirborne && !airborneNow && _moveAction != MoveAction.AirSlam;
            _flightWasAirborne = airborneNow;

            if (justLanded)
            {
                _landingRecoveryTicks = LandingPlantedTicks + LandingSlowWalkTicks;
                NPC.netUpdate = true;
            }

            // ── Dodge roll in progress: full speed for the first half, then coast and decay (the player's profile) ──
            if (rolling)
            {
                if (_stateTimer < RollTicks / 2)
                {
                    NPC.velocity.X = _rollDirection * RollSpeed;
                }
                else
                {
                    NPC.velocity.X *= RollTailRetention;
                }

                if (_stateTimer >= RollTicks)
                {
                    State = InvaderState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            // ── Estus: heal-over-time from a finished drink ──
            if (_estusHealTicksLeft > 0)
            {
                _estusHealTicksLeft--;
                _estusHealAccumulator += _estusHealPerTick;
                int wholeHeal = (int)_estusHealAccumulator;

                if (wholeHeal > 0)
                {
                    _estusHealAccumulator -= wholeHeal;
                    NPC.life = Math.Min(NPC.lifeMax, NPC.life + wholeHeal);
                    _lastLife = NPC.life;
                }

                if (_estusHealTicksLeft % 10 == 0)
                {
                    NPC.netUpdate = true;
                }
            }

            if (state == InvaderState.DrinkingEstus)
            {
                NPC.velocity.X *= DrinkingVelocityRetention;

                if (_stateTimer >= EstusDrinkTicks)
                {
                    int healAmount = (int)(NPC.lifeMax * EstusHealLifeFraction);
                    _estusCharges--;
                    _estusHealTicksLeft = Math.Max(1, EstusHealTicks);
                    _estusHealPerTick = healAmount / (float)_estusHealTicksLeft;
                    _estusHealAccumulator = 0f;
                    NPC.HealEffect(healAmount);
                    State = InvaderState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (state == InvaderState.SwitchingWeapon && _stateTimer >= WeaponSwitchTicks)
            {
                State = InvaderState.Fight;
                state = InvaderState.Fight;
                NPC.netUpdate = true;
            }

            bool attacking = (OwnerFlags & OwnerFlagAttacking) != 0;
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;
            float targetDistance = Vector2.Distance(NPC.Center, target.Center);
            bool airborne = _flight != null && _flight.IsAirborne;

            // Death re-arms the summon card. The 90% unlock latches, so healing cannot disable replacements.
            bool targetSummoner = HasSummonerLoadout(target);
            _summonerEligible |= targetSummoner || (OwnerFlags & OwnerFlagSummoner) != 0;
            _summonsUnlocked |= _summonerEligible && NPC.life <= NPC.lifeMax * SummonHealthFraction;

            if (_activeEcho != null && (!_activeEcho.active || _activeEcho.life <= 0
                || _activeEcho.GetGlobalNPC<MirrorEchoNPC>() != _activeEchoTag))
            {
                MirrorEchoSystem.RemoveFamily(this);
                _activeEcho = null;
                _activeEchoTag = null;
                _summonCard = _echoBag.Count > 0;
                _summonCooldown = SummonCooldownTicks;
            }

            if (_summonCooldown > 0)
            {
                _summonCooldown--;
            }

            if (state == InvaderState.SummoningAdd)
            {
                NPC.velocity.X *= 0.8f;
                FaceTarget(target);

                if (_stateTimer >= SummonHoldTicks + SummonSweepTicks)
                {
                    // Scope SetDefaults and OnSpawn as well as AI-created descendants to this encounter.
                    // This catches Owl Father's immediate companion before its first update.
                    int addIndex = -1;
                    MirrorEchoSystem.SpawningFor = this;

                    try
                    {
                        if (_queuedInvaderType != ModContent.NPCType<Kahlrun>())
                        {
                            addIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)_summonPoint.X,
                                (int)_summonPoint.Y, _queuedInvaderType, Target: target.whoAmI);
                        }
                    }
                    finally
                    {
                        MirrorEchoSystem.SpawningFor = null;
                    }

                    if (addIndex >= 0 && addIndex < Main.maxNPCs)
                    {
                        _activeEcho = Main.npc[addIndex];
                        _activeEchoTag = _activeEcho.GetGlobalNPC<MirrorEchoNPC>();
                        _activeEchoTag.PrimaryEcho = true;
                        _activeEcho.lifeMax = MirrorAddLife;
                        _activeEcho.life = MirrorAddLife;
                        _activeEcho.direction = NPC.direction;
                        _activeEcho.netUpdate = true;
                        _echoBag.Remove(_queuedInvaderType);
                        _lastSummonedType = _queuedInvaderType;
                        _summonCard = false;

                        if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                        {
                            Mod.Logger.Info($"[MirrorCombat] echo {_activeEcho.FullName}: life {_activeEcho.life}/{_activeEcho.lifeMax}, remaining {_echoBag.Count}, encounter {EncounterId}");
                        }
                    }

                    _summonCooldown = SummonCooldownTicks;
                    State = InvaderState.Fight;
                    _chooseAction = true;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (state == InvaderState.Whipping)
            {
                NPC.velocity.X *= 0.8f;

                // Dark Blood Knight's real lash tracks for 36 ticks, locks for ten, lashes for 30.
                if (_stateTimer < 36)
                {
                    FaceTarget(target);
                    _whipAim = (target.Center + target.velocity * 6f - WhipAnchor).SafeNormalize(Vector2.UnitX);
                    NPC.netUpdate = true;
                }

                bool whipAlive = _whipProjectile >= 0 && _whipProjectile < Main.maxProjectiles
                    && Main.projectile[_whipProjectile].active
                    && Main.projectile[_whipProjectile].ModProjectile is BloodKnightDarkHarvestWhip
                    && (int)Main.projectile[_whipProjectile].ai[0] == NPC.whoAmI;

                if (_stateTimer >= 76 && !whipAlive)
                {
                    // An aerial lash held its hover point through the tell, lash and latch; free it with the recovery.
                    if (_flight != null && _flight.HoverAnchor.HasValue)
                    {
                        _flight.HoverAnchor = null;
                    }

                    _whipRecovery = 42;
                    State = InvaderState.Fight;
                    _chooseAction = true;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (_whipRecovery > 0)
            {
                _whipRecovery--;

                if (!airborne)
                {
                    NPC.velocity.X *= 0.8f;
                }

                return;
            }

            // Wing landing, first part: planted, facing the target (the slow walk is applied with the movement below).
            if (_landingRecoveryTicks > LandingSlowWalkTicks)
            {
                NPC.velocity.X *= 0.8f;
                FaceTarget(target);
                return;
            }

            bool readyForAction = state == InvaderState.Fight && !attacking
                && (OwnerFlags & OwnerFlagRecovering) == 0 && !winded;
            bool firstSummon = _lastSummonedType == 0;

            if (_summonsUnlocked && _activeEcho == null && firstSummon
                && (!_echoRosterInitialized || _echoBag.Count > 0))
            {
                _summonCard = true;
            }

            if (_chooseAction && readyForAction)
            {
                // Rituals need validated ground: in the air the summon card waits until the wings land (TickFlight).
                if (!airborne && firstSummon && _summonCard && _summonCooldown == 0 && TryQueueEcho(target))
                {
                    return;
                }

                SelectCombatMemory(includeSummon: !airborne);

                if (_memory == CombatMemory.SummonEcho)
                {
                    if (TryQueueEcho(target))
                    {
                        return;
                    }

                    SelectCombatMemory(includeSummon: false);
                }

                _chooseAction = false;
                _actionSerial++;

                // The movement this use is built on rides in the same sync as the cleared _chooseAction, so the owner
                // can't start an unrelated tell before it knows about the action.
                ChooseMoveAction(target);
                NPC.netUpdate = true;
            }

            // Flight: an air action that didn't fit at the action boundary (range, line of sight) is re-checked here.
            bool airActionRecheck = airborne && readyForAction && !_chooseAction && _moveAction == MoveAction.None
                && _memory != CombatMemory.DarkHarvest && Main.GameUpdateCount % AirActionRecheckTicks == 0;

            if (airActionRecheck)
            {
                ChooseMoveAction(target);

                if (_moveAction != MoveAction.None)
                {
                    NPC.netUpdate = true;
                }
            }

            // Aerial lash: only from a hover close above the target, and it counts toward the sortie.
            bool whipAltitudeOk = !airborne || NPC.Center.Y >= target.Center.Y - AerialWhipMaxAltitude;
            bool whipSortieOk = !airborne || _sortieAttacks < SortieMaxAttacks;

            if (readyForAction && _memory == CombatMemory.DarkHarvest
                && whipAltitudeOk && whipSortieOk
                && targetDistance <= BloodKnightDarkHarvestWhip.MaximumReachPixels
                && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                if (airborne)
                {
                    _flight.HoverAnchor = NPC.Center;
                    _sortieAttacks++;
                }

                _whipAim = (target.Center - WhipAnchor).SafeNormalize(Vector2.UnitX);
                State = InvaderState.Whipping;
                _whipProjectile = Projectile.NewProjectile(NPC.GetSource_FromAI(), WhipAnchor,
                    _whipAim, ModContent.ProjectileType<BloodKnightDarkHarvestWhip>(),
                    45, 0f, Main.myPlayer, NPC.whoAmI);
                NPC.netUpdate = true;
                return;
            }

            // ── Estus: never mid-attack; back off a player within 4 tiles first (gives up after 2.5 s and drinks) ──
            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool wantsEstus = lowLife && _estusCharges > 0 && _estusHealTicksLeft == 0;
            // Estus is drunk on the ground: in the air the wings land first (TickFlight).
            bool canPrepare = (state == InvaderState.Fight || state == InvaderState.PreparingEstus) && !airborne;
            Player crowdingPlayer = null;

            if (canPrepare && wantsEstus)
            {
                float nearestCrowdDistance = EstusSpaceNearDistance;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];

                    if (!candidate.active || candidate.dead)
                    {
                        continue;
                    }

                    float candidateDistance = Vector2.Distance(candidate.Center, NPC.Center);

                    if (candidateDistance < nearestCrowdDistance)
                    {
                        nearestCrowdDistance = candidateDistance;
                        crowdingPlayer = candidate;
                    }
                }

                bool gaveUpOnSpace = state == InvaderState.PreparingEstus && _stateTimer >= EstusSpaceTimeoutTicks;
                bool hasSpace = crowdingPlayer == null || gaveUpOnSpace;

                if (hasSpace && !attacking)
                {
                    State = InvaderState.DrinkingEstus;
                    NPC.netUpdate = true;
                    return;
                }

                if (state == InvaderState.Fight)
                {
                    State = InvaderState.PreparingEstus;
                    state = InvaderState.PreparingEstus;
                    NPC.netUpdate = true;
                }
            }
            else if (state == InvaderState.PreparingEstus)
            {
                State = InvaderState.Fight;
                state = InvaderState.Fight;
                NPC.netUpdate = true;
            }

            // ── Evasion: a player shot about to hit, or a player swinging at it up close → roll / hop / sidestep ──
            // Never mid-attack: a committed tell or burst is the player's punish window (§12.10).
            // Allowed during Recovery so it can dodge incoming threats between bursts instead of standing helpless.
            bool canEvade = state == InvaderState.Fight && !attacking
                && !airborne && _moveAction == MoveAction.None;
            bool threatFound = false;
            bool threatIsMelee = false;
            Vector2 threatVelocity = Vector2.Zero;
            Vector2 threatOrigin = NPC.Center;

            if (canEvade)
            {
                threatFound = FindIncomingProjectile(true, out _, out threatVelocity, out threatOrigin);
            }

            if (canEvade && !threatFound)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player swinger = Main.player[i];

                    if (!swinger.active || swinger.dead || swinger.itemAnimation <= 0)
                    {
                        continue;
                    }

                    Item swingerWeapon = swinger.HeldItem;
                    bool meleeSwing = swingerWeapon != null && swingerWeapon.damage > 0
                        && (swingerWeapon.CountsAsClass(DamageClass.Melee)
                            || swingerWeapon.CountsAsClass(DamageClass.SummonMeleeSpeed)
                            || !swingerWeapon.noMelee);
                    bool facingUs = Math.Sign(NPC.Center.X - swinger.Center.X) == swinger.direction;
                    bool inReach = Vector2.Distance(swinger.Center, NPC.Center) <= MeleeThreatRange;

                    if (meleeSwing && facingUs && inReach)
                    {
                        threatFound = true;
                        threatIsMelee = true;
                        threatOrigin = swinger.Center;
                        break;
                    }
                }
            }

            if (threatFound && _evadeLockout == 0)
            {
                bool reacts = Main.rand.NextFloat() < EvadeChance;
                _evadeLockout = reacts ? EvadeDecisionLockoutTicks : EvadeRetryLockoutTicks;

                if (reacts)
                {
                    bool grounded = NPC.velocity.Y == 0f;
                    bool canRoll = (OwnerFlags & OwnerFlagCanRoll) != 0 && _rollCooldown == 0 && grounded;
                    bool mostlyHorizontal = Math.Abs(threatVelocity.X) > Math.Abs(threatVelocity.Y);
                    int awayDirection = Math.Sign(NPC.Center.X - threatOrigin.X);

                    if (awayDirection == 0)
                    {
                        awayDirection = -NPC.direction;
                    }

                    if (canRoll)
                    {
                        // A level shot is rolled THROUGH (the i-frames carry it past); a swing or a lob is rolled away from.
                        int rollDirection = awayDirection;

                        if (!threatIsMelee && mostlyHorizontal)
                        {
                            rollDirection = -Math.Sign(threatVelocity.X);
                        }

                        bool rollPathSafe = IsRollPathSafe(rollDirection);

                        if (!rollPathSafe)
                        {
                            rollDirection = -rollDirection;
                            rollPathSafe = IsRollPathSafe(rollDirection);
                        }

                        if (rollPathSafe)
                        {
                            _rollDirection = rollDirection;
                            _rollCooldown = RollTicks + RollCooldownTicks;
                            NPC.direction = rollDirection;
                            State = InvaderState.Rolling;
                            NPC.netUpdate = true;
                            return;
                        }
                    }

                    if (grounded && !threatIsMelee && mostlyHorizontal)
                    {
                        NPC.velocity.Y = -JumpEvadeSpeed;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        _strafeDirection = awayDirection;
                        _strafeTicksLeft = StrafeTicks;
                    }
                }
            }

            // ── Weapon choice: a shuffled bag of loadout slots (attack-quality-pass §6), filtered by range ──
            // A new weapon after every burst (the recovery edge), when its shot keeps being refused for distance, when
            // the current one can't reach for a while, or when a use failed to start (no ammo/mana on the host).
            Item activeItem = ActiveWeapon;
            float preferredDistance = RangedPreferred;
            float maximumDistance = RangedMaximum;
            float minimumShotDistance = 0f;

            if (activeItem != null)
            {
                GetCombatEngagementRange(activeItem, out preferredDistance, out maximumDistance);
                minimumShotDistance = MinimumFiringDistance(activeItem);
            }

            bool recovering = (OwnerFlags & OwnerFlagRecovering) != 0;
            bool useFailed = (OwnerFlags & OwnerFlagUseFailed) != 0;
            bool tooClose = (OwnerFlags & OwnerFlagTooClose) != 0;
            bool justEnteredRecovery = recovering && !_wasRecovering;
            bool justFailed = useFailed && !_wasUseFailed;
            if (justEnteredRecovery || justFailed)
            {
                _chooseAction = true;
            }

            _wasRecovering = recovering;
            _wasUseFailed = useFailed;

            if (tooClose)
            {
                _tooCloseTicks++;
            }
            else
            {
                _tooCloseTicks = 0;
            }

            if (targetDistance > maximumDistance)
            {
                _outOfReachTicks++;
            }
            else
            {
                _outOfReachTicks = 0;
            }

            bool wantsNewWeapon = justEnteredRecovery || justFailed || _tooCloseTicks >= TooCloseSwitchTicks || _outOfReachTicks >= OutOfReachSwitchTicks;
            bool canSwitch = state == InvaderState.Fight && !attacking && _loadoutCount > 1 && _moveAction == MoveAction.None;

            if (wantsNewWeapon && canSwitch)
            {
                _tooCloseTicks = 0;
                _outOfReachTicks = 0;
                int pickedSlot = -1;

                // Refill: every slot but the current one, shuffled (never an immediate repeat).
                if (_weaponBag.Count == 0)
                {
                    for (int slot = 0; slot < _loadoutCount; slot++)
                    {
                        if (slot != _activeSlot)
                        {
                            _weaponBag.Add(slot);
                        }
                    }

                    for (int i = _weaponBag.Count - 1; i > 0; i--)
                    {
                        int swapIndex = Main.rand.Next(i + 1);
                        int held = _weaponBag[i];
                        _weaponBag[i] = _weaponBag[swapIndex];
                        _weaponBag[swapIndex] = held;
                    }
                }

                // First slot in bag order that fits the current distance; none fits → the bag's head anyway (it walks
                // to that weapon's distance).
                for (int i = 0; i < _weaponBag.Count; i++)
                {
                    Item candidate = _loadoutItems[_weaponBag[i]];
                    GetCombatEngagementRange(candidate, out _, out float candidateMaximum);
                    float candidateMinimum = MinimumFiringDistance(candidate);
                    bool fits = targetDistance <= candidateMaximum && targetDistance >= candidateMinimum;

                    if (fits)
                    {
                        pickedSlot = _weaponBag[i];
                        break;
                    }
                }

                if (pickedSlot < 0 && _weaponBag.Count > 0)
                {
                    pickedSlot = _weaponBag[0];
                }

                if (pickedSlot >= 0 && pickedSlot != _activeSlot)
                {
                    _weaponBag.Remove(pickedSlot);
                    _activeSlot = pickedSlot;
                    _chooseAction = true;
                    State = InvaderState.SwitchingWeapon;
                    state = InvaderState.SwitchingWeapon;
                    NPC.netUpdate = true;

                    activeItem = ActiveWeapon;
                    GetCombatEngagementRange(activeItem, out preferredDistance, out maximumDistance);
                    minimumShotDistance = MinimumFiringDistance(activeItem);
                }
            }

            // ── Movement attacks and wing flight own velocity ahead of the planted brake and SF4 ──
            NPC.target = target.whoAmI;
            bool moveActionOwnsVelocity = TickMoveAction(target);

            if (moveActionOwnsVelocity)
            {
                return;
            }

            if (airborne)
            {
                TickFlight(target, attacking, winded);
                return;
            }

            if (TryTakeOff(target, attacking, winded, state))
            {
                return;
            }

            // ── Movement (SmartFighter4 waypoint mode toward a spot at the weapon's distance, on its own side) ──
            // Lumelia's sustained closing pressure: solve terrain through SF4, never teleport through a wall.
            _pursuitTicks = Math.Max(0, _pursuitTicks - 1);
            float topSpeed = MinTopSpeed;

            if (targetDistance > maximumDistance || !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                _pursuitTicks = 60;
            }

            if (_pursuitTicks > 0)
            {
                topSpeed = MaxTopSpeed;
            }

            // Planted while a tell or burst plays (the player's punish read), and drinking Estus slows it like the player.
            if (attacking)
            {
                NPC.velocity.X *= AttackingVelocityRetention;

                // Borrow Lumelia's distance-shaped step during ordinary cuts. Native held projectiles keep their own
                // movement; the server uses the streamed use index/timer, and tile collision remains enabled.
                if (CurrentUsePhase == UsePhase.Using && Categorize(activeItem) == WeaponCategory.SwingMelee)
                {
                    MeleeCombo combo = CurrentMemoryCombo;
                    int stepIndex = Math.Clamp(PatternUseIndex, 0, combo.Steps.Length - 1);
                    MeleeComboStep step = combo.Steps[stepIndex];
                    float stepDistance = step.StepInDistance;

                    if (stepDistance <= 0f)
                    {
                        stepDistance = 28f;
                    }

                    float previous = Utilities.SwingEase.ApplyWeighted(0f, stepDistance, PatternUseTicks - 1,
                        step.AttackTicks, step.EaseInTicks, step.EaseOutTicks, step.EaseOutDecay);
                    float current = Utilities.SwingEase.ApplyWeighted(0f, stepDistance, PatternUseTicks,
                        step.AttackTicks, step.EaseInTicks, step.EaseOutTicks, step.EaseOutDecay);
                    int facing = UseState.Direction;
                    NPC.velocity.X = facing * (current - previous);
                }

                FaceTarget(target);
                return;
            }

            // The wrapper's recovery and the slow part of a wing landing both walk at the recovery speed.
            if (recovering || _landingRecoveryTicks > 0)
            {
                topSpeed *= RecoveryWalkSpeedFraction;
            }

            int sideOfTarget = Math.Sign(NPC.Center.X - target.Center.X);

            if (sideOfTarget == 0)
            {
                sideOfTarget = -NPC.direction;
            }

            Vector2 waypoint;

            if (state == InvaderState.PreparingEstus)
            {
                if (crowdingPlayer == null)
                {
                    NPC.velocity.X *= 0.8f;
                    return;
                }

                int sideOfCrowder = Math.Sign(NPC.Center.X - crowdingPlayer.Center.X);

                if (sideOfCrowder == 0)
                {
                    sideOfCrowder = -NPC.direction;
                }

                waypoint = crowdingPlayer.Center + new Vector2(sideOfCrowder * EstusSpaceSettleDistance, 0f);
            }
            else if (_strafeTicksLeft > 0)
            {
                waypoint = NPC.Center + new Vector2(_strafeDirection * StrafeDistance, 0f);
            }
            else if (winded)
            {
                // Catching its breath: only gives ground if the target is right on top of it.
                float gapToTarget = Math.Abs(NPC.Center.X - target.Center.X);

                if (gapToTarget >= WindedNearDistance)
                {
                    NPC.velocity.X *= 0.8f;
                    FaceTarget(target);
                    return;
                }

                waypoint = target.Center + new Vector2(sideOfTarget * WindedSettleDistance, 0f);
            }
            else
            {
                // A shot stands outside its minimum firing distance; anything else at its preferred distance.
                float standDistance = preferredDistance;

                if (minimumShotDistance > 0f)
                {
                    standDistance = Math.Max(standDistance, minimumShotDistance + ShotDistanceMargin);
                }

                bool rangedWeapon = activeItem != null && !activeItem.IsAir
                    && (minimumShotDistance > 0f || preferredDistance >= RangedPreferred);
                bool shotBlocked = (OwnerFlags & OwnerFlagShotBlocked) != 0;

                if (_vantageRefreshTicks > 0)
                {
                    _vantageRefreshTicks--;
                }

                if (rangedWeapon)
                {
                    bool currentSpotClear = !shotBlocked && HasClearShotPath(NPC.Center, target.Center, target.velocity, activeItem);
                    bool targetShifted = Vector2.Distance(target.Center, _vantageTargetPos) > 96f;
                    bool needsVantageScan = !_hasVantageWaypoint || _vantageRefreshTicks <= 0 || targetShifted || !currentSpotClear;

                    if (needsVantageScan)
                    {
                        // Scan nearby standable columns (favoring high ground above the target) that have a clear shot path.
                        float minVantageDist = Math.Max(112f, minimumShotDistance + ShotDistanceMargin);
                        float maxVantageDist = Math.Max(minVantageDist + 48f, maximumDistance - 32f);
                        int centerTileX = (int)(NPC.Center.X / 16f);
                        int baseTileY = (int)((NPC.Bottom.Y - 1f) / 16f);
                        float bestScore = float.NegativeInfinity;
                        Vector2 bestPoint = Vector2.Zero;
                        bool foundVantage = false;

                        for (int offsetX = -14; offsetX <= 14; offsetX += 2)
                        {
                            int tileX = centerTileX + offsetX;

                            if (tileX < 4 || tileX >= Main.maxTilesX - 4)
                            {
                                continue;
                            }

                            int minTileY = Math.Max(4, baseTileY - 10);
                            int maxTileY = Math.Min(Main.maxTilesY - 4, baseTileY + 10);

                            for (int tileY = minTileY; tileY <= maxTileY; tileY++)
                            {
                                Tile floorTile = Main.tile[tileX, tileY];
                                bool standable = UsefulFunctions.IsTileReallySolid(tileX, tileY)
                                    || (floorTile.HasUnactuatedTile && TileID.Sets.Platforms[floorTile.TileType]);

                                if (!standable)
                                {
                                    continue;
                                }

                                bool headroomClear = !UsefulFunctions.IsTileReallySolid(tileX, tileY - 1)
                                    && !UsefulFunctions.IsTileReallySolid(tileX, tileY - 2)
                                    && !UsefulFunctions.IsTileReallySolid(tileX, tileY - 3);

                                if (!headroomClear)
                                {
                                    continue;
                                }

                                Vector2 candidateCenter = new Vector2(tileX * 16f + 8f, tileY * 16f - NPC.height * 0.5f);
                                float distToTarget = Vector2.Distance(candidateCenter, target.Center);

                                if (distToTarget < minVantageDist || distToTarget > maxVantageDist)
                                {
                                    continue;
                                }

                                if (!HasClearShotPath(candidateCenter, target.Center, target.velocity, activeItem))
                                {
                                    continue;
                                }

                                // Prioritize nearby high ground above the target, while staying near standDistance.
                                float elevationAboveTarget = Math.Clamp((target.Center.Y - candidateCenter.Y) / 16f, -6f, 6f);
                                float elevationAboveSelf = Math.Clamp((NPC.Center.Y - candidateCenter.Y) / 16f, -4f, 6f);
                                float travelDistance = Vector2.Distance(NPC.Center, candidateCenter);
                                float score = elevationAboveTarget * 25f
                                    + elevationAboveSelf * 10f
                                    - Math.Abs(distToTarget - standDistance) * 0.12f
                                    - travelDistance * 0.08f;

                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestPoint = candidateCenter;
                                    foundVantage = true;
                                }

                                // Highest open surface in this column checked; move to next column.
                                break;
                            }
                        }

                        _hasVantageWaypoint = foundVantage;
                        _vantageWaypoint = bestPoint;
                        _vantageTargetPos = target.Center;
                        _vantageRefreshTicks = 45;
                    }

                    if (_hasVantageWaypoint)
                    {
                        waypoint = _vantageWaypoint;
                    }
                    else if (!currentSpotClear)
                    {
                        // No clear standoff perch found: climb directly over terrain toward the target for clearance.
                        waypoint = target.Center;
                    }
                    else
                    {
                        waypoint = target.Center + new Vector2(sideOfTarget * standDistance, 0f);
                    }
                }
                else
                {
                    _hasVantageWaypoint = false;
                    waypoint = target.Center + new Vector2(sideOfTarget * standDistance, 0f);
                }
            }

            bool needRepositionForClearance = (OwnerFlags & OwnerFlagShotBlocked) != 0;
            float horizontalGap = Math.Abs(waypoint.X - NPC.Center.X);
            bool closeEnough = !needRepositionForClearance && horizontalGap <= EngagementSlack && Math.Abs(waypoint.Y - NPC.Center.Y) < 64f;

            if (closeEnough)
            {
                NPC.velocity.X *= 0.8f;
                FaceTarget(target);
                return;
            }

            bool waypointDrifted = !_hasHeldWaypoint || Vector2.Distance(waypoint, _heldWaypoint) > WaypointRefreshDistance;

            if (waypointDrifted)
            {
                _heldWaypoint = waypoint;
                _hasHeldWaypoint = true;
            }

            SmartFighter4AI.Run(NPC, topSpeed, Acceleration, doorBreakingDamage: DoorOpenValue, movementWaypoint: _heldWaypoint);
        }

        /// <summary>Server: face the target while standing (moving, SF4 faces the way it walks). Callers: the planted,
        /// winded and in-position branches of the movement block.</summary>
        internal static bool HasSummonerLoadout(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().startingClass == StartingClass.Summoner
                || player.GetModPlayer<MirrorSummonerPlayer>().ReportedSummonerClass)
            {
                return true;
            }

            for (int slot = 0; slot < HotbarSlots; slot++)
            {
                Item item = player.inventory[slot];

                if (!item.IsAir && item.damage > 0 && item.CountsAsClass(DamageClass.Summon))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryQueueEcho(Player target)
        {
            if (!_echoRosterInitialized)
            {
                _echoRosterInitialized = true;
                int[] roster =
                {
                    ModContent.NPCType<StuddedLeatherWarrior>(), ModContent.NPCType<RedKnight>(),
                    ModContent.NPCType<BlackKnight>(), ModContent.NPCType<GreatRedKnight>(),
                    ModContent.NPCType<GreatBlackKnight>(), ModContent.NPCType<BlackNinja>(),
                    ModContent.NPCType<ShadowNinja>(), ModContent.NPCType<AbyssalNinja>(),
                    ModContent.NPCType<Blaidd>(), ModContent.NPCType<Caliche>(),
                    ModContent.NPCType<DreadWraith>(), ModContent.NPCType<CursedDragon>(),
                    ModContent.NPCType<Ulhan>(), ModContent.NPCType<OwlFather>(),
                    ModContent.NPCType<HeroofLumelia>(), ModContent.NPCType<SpiritOfKhaios>(),
                    ModContent.NPCType<Marik>()
                };

                foreach (int candidate in roster)
                {
                    if (candidate != ModContent.NPCType<Kahlrun>() && tsorcRevampWorld.BossDefeated(candidate))
                    {
                        _echoBag.Add(candidate);
                    }
                }

                for (int i = _echoBag.Count - 1; i > 0; i--)
                {
                    int swapIndex = Main.rand.Next(i + 1);
                    int savedType = _echoBag[i];
                    _echoBag[i] = _echoBag[swapIndex];
                    _echoBag[swapIndex] = savedType;
                }

            }

            if (_echoBag.Count == 0)
            {
                return false;
            }

            int towardTarget = Math.Sign(target.Center.X - NPC.Center.X);

            if (towardTarget == 0)
            {
                towardTarget = NPC.direction;
            }

            float pointX = NPC.Center.X + towardTarget * SummonPointDistance;
            int pointTileX = (int)(pointX / 16f);
            int feetTileY = (int)((NPC.Bottom.Y - 1f) / 16f);

            // Refuse unsafe ground rather than producing an invader inside a wall or over a pit.
            for (int drop = 0; drop <= SummonGroundSearchTiles; drop++)
            {
                int groundTileY = feetTileY + drop;
                Vector2 summonFeet = new Vector2(pointX, groundTileY * 16f);

                if (!WorldGen.InWorld(pointTileX, groundTileY, 5)
                    || !UsefulFunctions.IsTileReallySolid(pointTileX, groundTileY)
                    || Collision.SolidCollision(summonFeet - new Vector2(22f, 64f), 44, 64))
                {
                    continue;
                }

                _summonPoint = summonFeet;
                _queuedInvaderType = _echoBag[0];
                _summonStaffShown = (_summonStaffShown + 1) % Math.Max(1, _summonStaffCount);
                State = InvaderState.SummoningAdd;
                NPC.netUpdate = true;
                return true;
            }

            _summonCooldown = 60;
            return false;
        }

        // ── Movement attacks and wing flight (server) ───────────────────────────────────────────────────

        /// <summary>Server: pick the movement the next use is built on, or None for an ordinary planted use. Ground
        /// leaps and apex shots must pass the arc solver; air actions need sortie room and flight budget. Callers: the
        /// action boundary, and every 10 ticks while hovering with nothing chosen.</summary>
        private void ChooseMoveAction(Player target)
        {
            _moveAction = MoveAction.None;
            _movePhase = MovePhase.Tell;
            _movePhaseTicks = 0;
            _moveDescentGravityMult = 1f;
            _moveApexRetargeted = false;
            // A fresh serial per decision, so the owner's tell for an older action can never launch this one.
            _moveSerial++;

            if (_memory == CombatMemory.SummonEcho || _memory == CombatMemory.DarkHarvest)
            {
                return;
            }

            Item weapon = ActiveWeapon;

            if (weapon == null)
            {
                return;
            }

            WeaponCategory category = Categorize(weapon);
            bool bladeMelee = category == WeaponCategory.SwingMelee || IsSpear(weapon);
            bool discreteShot = category == WeaponCategory.Shot || category == WeaponCategory.NearInstantShot;
            Vector2 toTarget = target.Center - NPC.Center;
            float distance = toTarget.Length();
            float horizontalGap = Math.Abs(toTarget.X);
            bool lineClear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool airborne = _flight != null && _flight.IsAirborne;
            MoveAction chosen = MoveAction.None;

            if (airborne)
            {
                bool hovering = _flight.Mode == FlightMode.Hover || _flight.Mode == FlightMode.Strafe;
                bool sortieOpen = hovering && lineClear && _sortieAttacks < SortieMaxAttacks;
                int budget = _flight.FlightTicksRemaining;
                bool targetGrounded = target.velocity.Y == 0f;

                if (sortieOpen && bladeMelee)
                {
                    // A grounded target gets the slam (it ends the flight); a target in the air gets the pass.
                    bool slamBudget = budget >= AirSlamTellTicks + AirActionBudgetMargin;
                    bool slamFits = targetGrounded && slamBudget && distance >= AirSlamMinDistance && distance <= AirSlamMaxDistance;
                    bool passBudget = budget >= AirPassTellTicks + _flight.Config.DiveAttackTicks + AirActionBudgetMargin;
                    bool passFits = !targetGrounded && passBudget && distance >= AirPassMinDistance && distance <= AirPassMaxDistance;

                    if (slamFits)
                    {
                        chosen = MoveAction.AirSlam;
                    }
                    else if (passFits)
                    {
                        chosen = MoveAction.AirPass;
                    }
                }
                else if (sortieOpen && discreteShot)
                {
                    bool strafeBudget = budget >= StrafeVolleyTellTicks + _flight.Config.StrafeTicks + AirActionBudgetMargin;
                    bool strafeInReach = horizontalGap <= StrafeVolleyReach;
                    bool strafeRolled = Main.rand.Next(100) < StrafeVolleyChancePercent;

                    if (strafeBudget && strafeInReach && strafeRolled)
                    {
                        chosen = MoveAction.StrafeVolley;
                    }
                    else if (budget >= HoverAttackBudgetTicks)
                    {
                        chosen = MoveAction.HoverAttack;
                    }
                }
                else if (sortieOpen && category == WeaponCategory.Channelled && !_sortieChannelUsed && budget >= HoverAttackBudgetTicks)
                {
                    // One locked beam per sortie.
                    chosen = MoveAction.HoverAttack;
                    _sortieChannelUsed = true;
                }

                if (chosen != MoveAction.None)
                {
                    _sortieAttacks++;
                    _airIdleTicks = 0;
                }
            }
            else if (NPC.velocity.Y == 0f)
            {
                bool leapTargetLevel = Math.Abs(toTarget.Y) <= LeapMaxHeightGap;

                if (bladeMelee && lineClear && leapTargetLevel)
                {
                    // Admission = farthest solved landing (capped forward speed x flat airtime) + reach - standoff -
                    // terrain margin, as PuppetNPC.ReliableJumpStartRange; beyond it the leap would land short.
                    float reach = GetStrikeReach(weapon);
                    float pursuitAirTicks = 2f * PursuitLaunchSpeed / LeapGravity;
                    float pursuitAdmission = PursuitMaxForwardSpeed * pursuitAirTicks + reach - LeapLandingStandoff - LeapTerrainMargin;
                    float highSlamAscentTicks = HighSlamLaunchSpeed / LeapGravity;
                    float highSlamAirTicks = highSlamAscentTicks + highSlamAscentTicks / (float)Math.Sqrt(HighSlamDescentGravityMult);
                    float highSlamAdmission = HighSlamMaxForwardSpeed * highSlamAirTicks + reach - LeapLandingStandoff - LeapTerrainMargin;

                    bool lowHealth = NPC.life <= NPC.lifeMax * HighSlamHealthFraction;
                    bool highSlamInBand = horizontalGap >= HighSlamMinDistance && horizontalGap <= highSlamAdmission;
                    bool highSlamWanted = lowHealth && _highSlamCooldown == 0 && highSlamInBand && Main.rand.Next(100) < HighSlamChancePercent;
                    bool pursuitInBand = horizontalGap > reach + PursuitMinExtraDistance && horizontalGap <= pursuitAdmission;
                    bool pursuitWanted = _pursuitCooldown == 0 && pursuitInBand && Main.rand.Next(100) < PursuitChancePercent;

                    if (highSlamWanted)
                    {
                        ComputeLaunchVelocity(MoveAction.HighSlam, target, out Vector2 slamVelocity, out float slamDescent, out _);

                        if (TrySolveBallisticArc(slamVelocity, slamDescent))
                        {
                            chosen = MoveAction.HighSlam;
                        }
                    }

                    if (chosen == MoveAction.None && pursuitWanted)
                    {
                        ComputeLaunchVelocity(MoveAction.PursuitLeap, target, out Vector2 pursuitVelocity, out float pursuitDescent, out _);

                        if (TrySolveBallisticArc(pursuitVelocity, pursuitDescent))
                        {
                            chosen = MoveAction.PursuitLeap;
                        }
                    }
                }
                else if (discreteShot)
                {
                    // Owl Father's jump shot: forced when the target is 3+ tiles up, or 1+ tile up behind cover; else a
                    // 15% roll, never twice in a row.
                    float heightAbove = NPC.Center.Y - target.Center.Y;
                    bool forcedHigh = heightAbove >= ApexForcedHeightAbove;
                    bool forcedBlocked = heightAbove >= ApexForcedBlockedHeightAbove && !lineClear;
                    bool randomJump = !_lastRangedMoveWasJump && Main.rand.Next(100) < ApexRandomChancePercent;

                    if (forcedHigh || forcedBlocked || randomJump)
                    {
                        ComputeLaunchVelocity(MoveAction.ApexShot, target, out Vector2 apexVelocity, out float apexDescent, out _);

                        if (TrySolveBallisticArc(apexVelocity, apexDescent))
                        {
                            chosen = MoveAction.ApexShot;
                        }
                    }

                    _lastRangedMoveWasJump = chosen == MoveAction.ApexShot;
                }
            }

            _moveAction = chosen;

            if (chosen != MoveAction.None && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorMove] chose {chosen} serial {_moveSerial}: weapon {weapon.Name}, distance {distance:0}, airborne {airborne}, sortie {_sortieAttacks}");
            }
        }

        /// <summary>Server: the launch velocity a ground leap or apex shot uses from where it stands now, solved as Owl
        /// Father's (PuppetNPC.BeginLeapAttack, OwlFather.ModifyRangedBurst). Apex shot: rise = height gap clamped to
        /// 48–112px (120 for a random hop), drifting toward the shot's standing distance. Leaps: horizontal speed =
        /// (gap - 24px standoff) / airtime, capped. Callers: ChooseMoveAction (feasibility) and TickMoveAction (launch).</summary>
        private void ComputeLaunchVelocity(MoveAction action, Player target, out Vector2 launchVelocity, out float descentGravityMult, out int ticksToApex)
        {
            float gapX = target.Center.X - NPC.Center.X;
            int towardTarget = 1;

            if (gapX < 0f)
            {
                towardTarget = -1;
            }

            float travelDistance = Math.Max(0f, Math.Abs(gapX) - LeapLandingStandoff);
            float launchSpeedY;
            float velocityX;
            descentGravityMult = 1f;

            if (action == MoveAction.ApexShot)
            {
                float heightAbove = NPC.Center.Y - target.Center.Y;
                float rise = ApexRandomRise;

                if (heightAbove >= ApexForcedBlockedHeightAbove)
                {
                    rise = MathHelper.Clamp(heightAbove, ApexMinRise, ApexMaxRise);
                }

                // v = sqrt(2·g·h) puts the apex exactly `rise` px up; ticks to apex = v / g.
                launchSpeedY = (float)Math.Sqrt(2f * LeapGravity * rise);
                ticksToApex = Math.Max(1, (int)Math.Round(launchSpeedY / LeapGravity));

                GetCombatEngagementRange(ActiveWeapon, out float preferredDistance, out _);
                float standDistance = Math.Max(preferredDistance, MinimumFiringDistance(ActiveWeapon) + ShotDistanceMargin);
                float standX = target.Center.X - towardTarget * standDistance;
                float hopTicks = 2f * ticksToApex;
                velocityX = MathHelper.Clamp((standX - NPC.Center.X) / hopTicks, -ApexMaxForwardSpeed, ApexMaxForwardSpeed);
            }
            else if (action == MoveAction.HighSlam)
            {
                launchSpeedY = HighSlamLaunchSpeed;
                ticksToApex = (int)Math.Round(launchSpeedY / LeapGravity);
                descentGravityMult = HighSlamDescentGravityMult;

                // The boosted fall is shorter than the climb by sqrt(gravity multiplier).
                float descentTicks = ticksToApex / (float)Math.Sqrt(descentGravityMult);
                float slamSpeed = MathHelper.Clamp(travelDistance / (ticksToApex + descentTicks), LeapMinForwardSpeed, HighSlamMaxForwardSpeed);
                velocityX = towardTarget * slamSpeed;
            }
            else
            {
                launchSpeedY = PursuitLaunchSpeed;
                ticksToApex = (int)Math.Round(launchSpeedY / LeapGravity);
                float airTicks = 2f * ticksToApex;
                float pursuitSpeed = MathHelper.Clamp(travelDistance / airTicks, LeapMinForwardSpeed, PursuitMaxForwardSpeed);
                velocityX = towardTarget * pursuitSpeed;
            }

            launchVelocity = new Vector2(velocityX, -launchSpeedY);
        }

        /// <summary>Server: whether a jump launched now lands safely, stepping the real hitbox tick by tick. Fails on a
        /// ceiling or wall on the way up, liquid at the landing, a landing more than 6 tiles below the launch, or no
        /// landing within 150 ticks (a pit). Platforms count as ground on the way down. Callers: ChooseMoveAction (each
        /// candidate) and TickMoveAction (re-checked from wherever the tell ended).</summary>
        private bool TrySolveBallisticArc(Vector2 launchVelocity, float descentGravityMult)
        {
            Vector2 position = NPC.position;
            Vector2 velocity = launchVelocity;

            for (int tick = 0; tick < ArcMaxTicks; tick++)
            {
                bool rising = velocity.Y < 0f;
                float gravity = LeapGravity;

                if (!rising)
                {
                    gravity *= descentGravityMult;
                }

                Vector2 previousPosition = position;
                velocity.Y = Math.Min(velocity.Y + gravity, ArcMaxFallSpeed);
                position += velocity;

                if (rising)
                {
                    bool blockedRising = Collision.SolidCollision(position, NPC.width, NPC.height);

                    if (blockedRising)
                    {
                        return false;
                    }

                    continue;
                }

                bool touchedDown = Collision.SolidCollision(position, NPC.width, NPC.height, true);

                if (!touchedDown)
                {
                    continue;
                }

                float drop = previousPosition.Y - NPC.position.Y;
                bool wetLanding = Collision.WetCollision(previousPosition, NPC.width, NPC.height);
                return drop <= ArcMaxDrop && !wetLanding;
            }

            return false;
        }

        /// <summary>Server: drop the current movement action (finished, cancelled, or the host changed) and free any
        /// hover anchor it held.</summary>
        private void EndMoveAction()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorMove] end {_moveAction} serial {_moveSerial} in {_movePhase} after {_movePhaseTicks}t");
            }

            _moveAction = MoveAction.None;
            _movePhase = MovePhase.Tell;
            _movePhaseTicks = 0;
            _moveDescentGravityMult = 1f;
            _moveApexRetargeted = false;

            if (_flight != null)
            {
                _flight.HoverAnchor = null;
            }

            NPC.netUpdate = true;
        }

        private void EnterMovePhase(MovePhase phase)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorMove] {_moveAction} serial {_moveSerial}: {_movePhase} -> {phase} after {_movePhaseTicks}t, velocity {NPC.velocity}");
            }

            _movePhase = phase;
            _movePhaseTicks = 0;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Server, every tick: runs the current movement action. True while it owns velocity (the caller then skips the
        /// planted brake, flight decisions and SF4).
        ///   Tell   — waits for the owner's tell for this serial (dropped after 120t or if the owner starts something
        ///            else), then launches when the tell reaches its launch point: an apex shot ticks-to-apex early so
        ///            the held shot leaves at the top, everything else when the authored tell ends.
        ///   Air    — the jump/dive/pass. High slam: one apex retarget. Touchdown or pass end -> Landed.
        ///   Landed — a short hold (planted, or hovering after a pass), then normal behaviour resumes.
        /// </summary>
        private bool TickMoveAction(Player target)
        {
            if (_moveAction == MoveAction.None)
            {
                return false;
            }

            _movePhaseTicks++;
            bool ownerToldThis = _ownerTellSerial == _moveSerial;
            bool ownerInTell = CurrentUsePhase == UsePhase.Tell;
            bool ballistic = _moveAction == MoveAction.ApexShot || _moveAction == MoveAction.PursuitLeap
                || _moveAction == MoveAction.HighSlam;

            if (_movePhase == MovePhase.Tell)
            {
                if (!ownerToldThis)
                {
                    bool ownerBusyElsewhere = CurrentUsePhase == UsePhase.Tell || CurrentUsePhase == UsePhase.Using;

                    if (ownerBusyElsewhere || _movePhaseTicks > MoveTellTimeoutTicks)
                    {
                        EndMoveAction();
                    }

                    return false;
                }

                // Released without a launch (the hold cap, or a reset on the owner): nothing left to move.
                if (!ownerInTell)
                {
                    EndMoveAction();
                    return false;
                }

                if (_moveAction == MoveAction.HoverAttack)
                {
                    // Station-keep from the tell's start so the locked aim line's origin stays put.
                    _flight.HoverAnchor = NPC.Center;
                    EnterMovePhase(MovePhase.Air);
                    return true;
                }

                Vector2 launchVelocity = Vector2.Zero;
                float descentGravityMult = 1f;
                int launchLeadTicks = 0;

                if (ballistic)
                {
                    ComputeLaunchVelocity(_moveAction, target, out launchVelocity, out descentGravityMult, out int ticksToApex);

                    if (_moveAction == MoveAction.ApexShot)
                    {
                        launchLeadTicks = ticksToApex;
                    }
                }

                if (TellTicksRemaining > launchLeadTicks)
                {
                    return false;
                }

                if (ballistic)
                {
                    bool arcSafe = TrySolveBallisticArc(launchVelocity, descentGravityMult);
                    bool standing = NPC.velocity.Y == 0f;

                    if (!arcSafe || !standing)
                    {
                        EndMoveAction();
                        return false;
                    }

                    NPC.velocity = launchVelocity;
                    _moveLaunchVx = launchVelocity.X;
                    _moveDescentGravityMult = descentGravityMult;
                    FaceTarget(target);

                    if (_moveAction == MoveAction.PursuitLeap)
                    {
                        _pursuitCooldown = PursuitCooldownTicks;
                    }
                    else if (_moveAction == MoveAction.HighSlam)
                    {
                        _highSlamCooldown = HighSlamCooldownTicks;
                    }
                }
                else if (_moveAction == MoveAction.AirPass)
                {
                    // Dive through the target's position to 120px past it, kept 32px above its feet so it never ploughs
                    // into the floor; the controller returns to Hover at the waypoint with the remaining budget.
                    Vector2 approach = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
                    Vector2 waypoint = target.Center + approach * AirPassOvershoot;
                    float lowestWaypointY = target.Bottom.Y - AirPassFeetClearance - NPC.height * 0.5f;
                    waypoint.Y = Math.Min(waypoint.Y, lowestWaypointY);

                    if (!_flight.RequestDive(waypoint))
                    {
                        EndMoveAction();
                        return false;
                    }
                }
                else if (_moveAction == MoveAction.AirSlam)
                {
                    // Owl Father's Greatfire Dive: drop out of the wings into one ballistic line ending a standoff short
                    // of the target. Airtime from a 9 px/t dive, clamped 16–40t; gravity solved into the launch.
                    _flight.EndFlightNow();
                    int towardTarget = 1;

                    if (target.Center.X < NPC.Center.X)
                    {
                        towardTarget = -1;
                    }

                    Vector2 aimPoint = new Vector2(target.Center.X - towardTarget * LeapLandingStandoff, target.Center.Y);
                    Vector2 diveGap = aimPoint - NPC.Center;
                    float diveTicks = MathHelper.Clamp(diveGap.Length() / AirSlamDiveSpeed, AirSlamMinDiveTicks, AirSlamMaxDiveTicks);
                    float gravityDrop = 0.5f * LeapGravity * diveTicks * diveTicks;
                    float diveVelocityX = diveGap.X / diveTicks;
                    float diveVelocityY = (diveGap.Y - gravityDrop) / diveTicks;

                    NPC.velocity = new Vector2(diveVelocityX, diveVelocityY);
                    _moveLaunchVx = diveVelocityX;
                    _moveDescentGravityMult = 1f;
                    _sortieAttacks = SortieMaxAttacks;
                    FaceTarget(target);
                }
                else if (_moveAction == MoveAction.StrafeVolley)
                {
                    float side = Math.Sign(NPC.Center.X - target.Center.X);

                    if (side == 0f)
                    {
                        side = -NPC.direction;
                    }

                    Vector2 passEnd = target.Center + new Vector2(-side * StrafeVolleyPassOffset, 0f);

                    if (!_flight.RequestStrafe(passEnd))
                    {
                        EndMoveAction();
                        return false;
                    }
                }

                EnterMovePhase(MovePhase.Air);
                return true;
            }

            if (_movePhase == MovePhase.Air)
            {
                if (_moveAction == MoveAction.HoverAttack)
                {
                    bool ownerDone = !ownerToldThis || CurrentUsePhase == UsePhase.Recovery || CurrentUsePhase == UsePhase.Ready;

                    if (ownerDone)
                    {
                        EndMoveAction();
                    }

                    return true;
                }

                if (_moveAction == MoveAction.StrafeVolley)
                {
                    if (_flight.Mode != FlightMode.Strafe)
                    {
                        EnterMovePhase(MovePhase.Landed);
                    }

                    return true;
                }

                if (_moveAction == MoveAction.AirPass)
                {
                    if (_flight.Mode != FlightMode.DiveAttack)
                    {
                        EnterMovePhase(MovePhase.Landed);
                    }

                    return true;
                }

                // Ballistic (leaps, apex shot, air slam). High slam: re-aim once as it tips over the apex, against the
                // boosted fall's real airtime, so walking away during the long climb doesn't guarantee a whiff.
                bool pastApex = NPC.velocity.Y >= 0f;

                if (_moveAction == MoveAction.HighSlam && pastApex && !_moveApexRetargeted)
                {
                    _moveApexRetargeted = true;
                    float descentTicks = Math.Max(6f, HighSlamLaunchSpeed / LeapGravity / (float)Math.Sqrt(HighSlamDescentGravityMult));
                    float retargetGap = target.Center.X - NPC.Center.X;
                    int retargetDirection = 1;

                    if (retargetGap < 0f)
                    {
                        retargetDirection = -1;
                    }

                    float retargetTravel = Math.Max(0f, Math.Abs(retargetGap) - LeapLandingStandoff);
                    float retargetSpeed = MathHelper.Clamp(retargetTravel / descentTicks, 0f, HighSlamMaxForwardSpeed);
                    _moveLaunchVx = retargetDirection * retargetSpeed;
                    NPC.direction = retargetDirection;
                    NPC.netUpdate = true;
                }

                // Touchdown: falling stopped with standable ground right under the feet (not the zero crossing at the apex).
                Vector2 feetProbe = new Vector2(NPC.position.X, NPC.position.Y + NPC.height);
                bool groundUnderFeet = Collision.SolidCollision(feetProbe, NPC.width, 2, true);
                bool touchedDown = _movePhaseTicks > 3 && NPC.velocity.Y == 0f && groundUnderFeet;

                if (touchedDown || _movePhaseTicks > MoveAirMaxTicks)
                {
                    EnterMovePhase(MovePhase.Landed);
                }

                return true;
            }

            // Landed: planted hold after a jump; a hover hold after a pass. Air actions wait for the owner's burst to
            // finish so ending the action doesn't cut the volley short (ChooseTarget refuses uses in the air without one).
            int holdTicks = 0;

            switch (_moveAction)
            {
                case MoveAction.PursuitLeap:
                    holdTicks = PursuitLandingHoldTicks;
                    break;
                case MoveAction.HighSlam:
                    holdTicks = HighSlamLandingHoldTicks;
                    break;
                case MoveAction.AirSlam:
                    holdTicks = AirSlamLandingHoldTicks;
                    break;
                case MoveAction.AirPass:
                    holdTicks = AirPassHoverHoldTicks;
                    break;
            }

            bool groundLanding = ballistic || _moveAction == MoveAction.AirSlam;

            if (groundLanding)
            {
                NPC.velocity.X *= 0.7f;
            }

            bool holdDone = _movePhaseTicks >= holdTicks;
            bool ownerStillUsing = !groundLanding && CurrentUsePhase == UsePhase.Using;

            if (holdDone && !ownerStillUsing)
            {
                EndMoveAction();
            }

            return true;
        }

        /// <summary>Server, every airborne tick without a launched action: when to land. The controller (ticked on every
        /// machine in AI) does the flying and the hover placement. Lands after the sortie's second attack, on a low budget,
        /// after 150 idle ticks, after 120 ticks with a grounded target out of sight, when winded, or when the next step
        /// needs the ground (Estus, a ritual, a flail or yoyo).</summary>
        private void TickFlight(Player target, bool attacking, bool winded)
        {
            bool ownerBusy = attacking || (OwnerFlags & OwnerFlagRecovering) != 0;
            bool hovering = _flight.Mode == FlightMode.Hover || _flight.Mode == FlightMode.Strafe;
            bool canLandNow = !ownerBusy && _moveAction == MoveAction.None && State == InvaderState.Fight && hovering;

            if (!canLandNow)
            {
                _airIdleTicks = 0;
                return;
            }

            _airIdleTicks++;
            bool lineClear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (!lineClear && target.velocity.Y == 0f)
            {
                _noSightTicks++;
            }
            else
            {
                _noSightTicks = 0;
            }

            Item weapon = ActiveWeapon;
            WeaponCategory category = Categorize(weapon);
            bool flailOrYoyo = category == WeaponCategory.HeldProjectileMelee && !IsSpear(weapon);
            bool groundOnlyWeapon = flailOrYoyo || category == WeaponCategory.SummonStaff || category == WeaponCategory.Unusable;
            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool wantsEstus = lowLife && _estusCharges > 0 && _estusHealTicksLeft == 0;
            bool ritualDue = _summonsUnlocked && _summonCard && _summonCooldown == 0 && _activeEcho == null;
            bool sortieDone = _sortieAttacks >= SortieMaxAttacks;
            bool budgetLow = _flight.FlightTicksRemaining < FlightLowBudgetTicks;
            bool idleTooLong = _airIdleTicks > AirIdleLandTicks;
            bool lostSight = _noSightTicks > NoSightLandTicks;
            bool wantsLand = sortieDone || budgetLow || idleTooLong || lostSight || winded || groundOnlyWeapon || wantsEstus || ritualDue;

            if (!wantsLand)
            {
                return;
            }

            _flight.RequestLand();
            _airIdleTicks = 0;
            _noSightTicks = 0;
            NPC.netUpdate = true;

            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorMove] land: sortie {_sortieAttacks}, budget {_flight.FlightTicksRemaining}, idle {idleTooLong}, sight {lostSight}, weapon {groundOnlyWeapon}, estus {wantsEstus}, ritual {ritualDue}");
            }
        }

        /// <summary>Server, grounded, checked every 30 ticks: take wing when the copied player has functional wings and the
        /// fight calls for it — the target flying (45+ ticks off the ground), 6+ tiles up and unreachable, or a shot
        /// blocked from the ground but clear from hover height — or on an 8% roll. Never mid-attack, winded, during a
        /// landing's punish window, with a flail/yoyo/staff out, or when Estus or a ritual is due.</summary>
        private bool TryTakeOff(Player target, bool attacking, bool winded, InvaderState state)
        {
            if (_flight == null || _flight.IsAirborne || _flight.CooldownRemaining > 0)
            {
                return false;
            }

            bool recovering = (OwnerFlags & OwnerFlagRecovering) != 0;
            bool busy = state != InvaderState.Fight || attacking || winded || recovering || _moveAction != MoveAction.None;

            if (busy || _landingRecoveryTicks > 0 || NPC.velocity.Y != 0f)
            {
                return false;
            }

            _takeoffCheckTimer--;

            if (_takeoffCheckTimer > 0)
            {
                return false;
            }

            _takeoffCheckTimer = TakeoffCheckIntervalTicks;

            Item weapon = ActiveWeapon;
            WeaponCategory category = Categorize(weapon);
            bool flailOrYoyo = category == WeaponCategory.HeldProjectileMelee && !IsSpear(weapon);
            bool groundOnlyWeapon = flailOrYoyo || category == WeaponCategory.SummonStaff || category == WeaponCategory.Unusable;
            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool wantsEstus = lowLife && _estusCharges > 0 && _estusHealTicksLeft == 0;
            bool ritualDue = _summonsUnlocked && _summonCard && _summonCooldown == 0 && _activeEcho == null;

            if (groundOnlyWeapon || wantsEstus || ritualDue)
            {
                return false;
            }

            float heightAbove = NPC.Center.Y - target.Center.Y;
            bool lineClear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool targetFlying = _targetAirborneTicks >= TakeoffTargetAirborneTicks;
            bool targetHighOutOfReach = heightAbove >= TakeoffTargetAboveDistance && (!lineClear || targetFlying);
            bool rangedFamily = category == WeaponCategory.Shot || category == WeaponCategory.NearInstantShot
                || category == WeaponCategory.Channelled;
            Vector2 hoverPoint = NPC.Center - new Vector2(0f, RangedHoverAltitude);
            bool shotNeedsHeight = rangedFamily && !lineClear && Collision.CanHitLine(hoverPoint, 1, 1, target.Center, 1, 1);
            bool randomTakeoff = Main.rand.Next(100) < RandomTakeoffPercent;

            if (!targetFlying && !targetHighOutOfReach && !shotNeedsHeight && !randomTakeoff)
            {
                return false;
            }

            if (!_flight.RequestTakeoff())
            {
                return false;
            }

            _sortieAttacks = 0;
            _sortieChannelUsed = false;
            _airIdleTicks = 0;
            _noSightTicks = 0;
            NPC.netUpdate = true;

            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorMove] takeoff: flying {targetFlying}, high {targetHighOutOfReach}, shot {shotNeedsHeight}, random {randomTakeoff}, budget {_flight.Config.MaxFlightTicks}");
            }

            return true;
        }

        /// <summary>Every machine: the flight config for the copied functional wings — Owl Father's pacing with hover
        /// drift, a strafe arc and ceiling headroom opted in. Budget per sortie = 240 + 2 x the wing's fly time (capped at
        /// 180), clamped to 300–600 ticks; unknown or modded wings without stats count as 100. Callers: AI, ReceiveExtraAI.</summary>
        private EnemyFlightConfig BuildFlightConfig()
        {
            int flyTime = DefaultWingFlyTime;
            bool statsKnown = _wingFunctionalSlot > 0 && _wingFunctionalSlot < ArmorIDs.Wing.Sets.Stats.Length;

            if (statsKnown && ArmorIDs.Wing.Sets.Stats[_wingFunctionalSlot].FlyTime > 0)
            {
                flyTime = Math.Min(MaxCountedWingFlyTime, ArmorIDs.Wing.Sets.Stats[_wingFunctionalSlot].FlyTime);
            }

            int flightBudget = Math.Clamp(FlightTicksBase + flyTime * FlightTicksPerFlyTime, MinFlightTicks, MaxFlightTicks);

            return new EnemyFlightConfig
            {
                HoverAltitude = MeleeHoverAltitude,
                HoverSideOffset = MeleeHoverSideOffset,
                HoverTopSpeed = 4f,
                TakeOffSpeed = 6f,
                DiveAcceleration = 0.5f,
                DiveTopSpeed = 10.5f,
                LandSpeed = 5.5f,
                MaxFlightTicks = flightBudget,
                CooldownTicks = FlightCooldownTicks,
                TakeOffTicks = 34,
                HoverDwellTicks = 110,
                StrafeTicks = 55,
                DiveAttackTicks = 44,
                LandTicks = 80,
                WingFlapSpeed = 0.065f,
                HoverDriftSpeed = 0.28f,
                HoverDriftMax = 25f,
                HoverFlapRise = 18f,
                HoverFlapBurstTicks = 18,
                StrafeArcHeight = 32f,
                CeilingClearanceFraction = 0.33f,
                CeilingMinPadding = 24f,
            };
        }

        /// <summary>Reach for leap/pass admission and the strike trigger: the sprite diagonal at use scale + 16px (as
        /// GetEngagementRange's melee branch, never its sword-beam maximum); spears use their thrust reach.</summary>
        private static float GetStrikeReach(Item weapon)
        {
            if (weapon == null)
            {
                return 0f;
            }

            if (IsSpear(weapon))
            {
                return SpearStrikeReach;
            }

            float spriteDiagonal = (float)Math.Sqrt(weapon.width * weapon.width + weapon.height * weapon.height) * weapon.scale;
            return spriteDiagonal + 16f;
        }

        /// <summary>Spears (vanilla's Spears set, or a projectile on the spear AI): the only held-projectile melee used in
        /// leaps and flight. Flails and yoyos stay on the ground.</summary>
        private static bool IsSpear(Item item)
        {
            if (item == null || item.IsAir)
            {
                return false;
            }

            if (ItemID.Sets.Spears[item.type])
            {
                return true;
            }

            if (item.shoot <= ProjectileID.None)
            {
                return false;
            }

            if (!ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile sample))
            {
                return false;
            }

            return sample.aiStyle == ProjAIStyleID.Spear;
        }

        private void SelectCombatMemory(bool includeSummon = true)
        {
            WeaponCategory category = Categorize(ActiveWeapon);
            bool magic = ActiveWeapon != null && ActiveWeapon.CountsAsClass(DamageClass.Magic);
            List<CombatMemory> available = new List<CombatMemory>();

            if (category == WeaponCategory.Whip)
            {
                available.Add(CombatMemory.DarkHarvest);
            }
            else if (category == WeaponCategory.SwingMelee || category == WeaponCategory.HeldProjectileMelee)
            {
                available.Add(CombatMemory.LumeliaQuickslash);
                available.Add(CombatMemory.LumeliaUnderOver);
                available.Add(CombatMemory.MarikHeavy);

                if (NPC.life < NPC.lifeMax * 0.5f)
                {
                    available.Add(CombatMemory.MarikFury);
                }
            }
            else if (magic || category == WeaponCategory.Channelled)
            {
                available.Add(CombatMemory.NinjaCast);
                available.Add(CombatMemory.MarikCast);
            }
            else
            {
                available.Add(CombatMemory.WarriorCrossbow);
                available.Add(CombatMemory.OwlBow);

                if (category != WeaponCategory.Channelled)
                {
                    available.Add(CombatMemory.RangedQuickVolley);
                    available.Add(CombatMemory.RangedFlurry);
                }
            }

            if (includeSummon && _summonsUnlocked && _summonCard && _summonCooldown == 0 && _activeEcho == null)
            {
                available.Add(CombatMemory.SummonEcho);
            }

            _attackBag.RemoveAll(memory => !available.Contains(memory));

            if (_attackBag.Count == 0)
            {
                _attackBag.AddRange(available);

                for (int i = _attackBag.Count - 1; i > 0; i--)
                {
                    int swapIndex = Main.rand.Next(i + 1);
                    CombatMemory saved = _attackBag[i];
                    _attackBag[i] = _attackBag[swapIndex];
                    _attackBag[swapIndex] = saved;
                }

                if (_hasPreviousMemory && _attackBag.Count > 1 && _attackBag[0] == _previousMemory)
                {
                    CombatMemory saved = _attackBag[0];
                    _attackBag[0] = _attackBag[1];
                    _attackBag[1] = saved;
                }
            }

            _memory = _attackBag[0];
            _attackBag.RemoveAt(0);
            _previousMemory = _memory;
            _hasPreviousMemory = true;

            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                Mod.Logger.Info($"[MirrorCombat] {_memory}, weapon {ActiveWeapon?.Name}, life {NPC.life}/{NPC.lifeMax}, encounter {EncounterId}");
            }
        }

        protected override void ConfigureUsePattern(Item weapon, WeaponCategory category, ref UsePattern pattern)
        {
            // These are current encounter timings, applied to the copied weapon's real ItemCheck and projectile art.
            // Held weapons retain their own simulation; ordinary blades replay the source's Weighted clock below.
            switch (_memory)
            {
                case CombatMemory.LumeliaQuickslash:
                case CombatMemory.LumeliaUnderOver:
                case CombatMemory.MarikHeavy:
                case CombatMemory.MarikFury:
                    MeleeCombo combo = CurrentMemoryCombo;
                    pattern.TellTicks = combo.Steps[0].TelegraphTicks;
                    pattern.BurstMax = combo.Steps.Length;
                    pattern.RecoveryTicks = combo.RecoveryTicks;
                    pattern.AimLockTicks = 12;
                    pattern.GapTicks = 12;

                    if (category == WeaponCategory.HeldProjectileMelee)
                    {
                        pattern.BurstMax = 1;
                        pattern.TellTicks = Math.Max(40, pattern.TellTicks);
                    }

                    break;
                case CombatMemory.WarriorCrossbow:
                    pattern.TellTicks = 28;
                    pattern.BurstMax = 2;
                    pattern.GapTicks = 16;
                    pattern.RecoveryTicks = 32;
                    pattern.AimLockTicks = 10;
                    break;
                case CombatMemory.OwlBow:
                    pattern.TellTicks = 34;
                    pattern.BurstMax = 3;
                    pattern.GapTicks = 14;
                    pattern.RecoveryTicks = 34;
                    pattern.AimLockTicks = 10;
                    break;
                case CombatMemory.RangedQuickVolley:
                    pattern.TellTicks = 26;
                    pattern.BurstMax = 3;
                    pattern.GapTicks = 6;
                    pattern.RecoveryTicks = 30;
                    pattern.AimLockTicks = 8;
                    break;
                case CombatMemory.RangedFlurry:
                    // Sustained player-like rapid fire: 5 shots for normal bows/guns, up to 7 for fast auto-fire weapons.
                    pattern.TellTicks = 32;
                    pattern.BurstMax = weapon.useTime <= 12 ? 7 : 5;
                    pattern.GapTicks = weapon.useTime <= 12 ? 0 : 4;
                    pattern.RecoveryTicks = 40;
                    pattern.AimLockTicks = 8;
                    break;
                case CombatMemory.NinjaCast:
                    pattern.TellTicks = 60;
                    pattern.BurstMax = 1;
                    pattern.RecoveryTicks = 50;
                    pattern.ChannelTicks = 36;
                    pattern.AimLockTicks = 24;
                    break;
                case CombatMemory.MarikCast:
                    pattern.TellTicks = 75;
                    pattern.BurstMax = 2;
                    pattern.GapTicks = 30;
                    pattern.RecoveryTicks = 82;
                    pattern.ChannelTicks = 36;
                    pattern.AimLockTicks = 24;
                    break;
            }

            if (category == WeaponCategory.NearInstantShot)
            {
                pattern.AimLockTicks = 24;
            }

            // Charged bows (e.g. Artemis Bow, useTime 60) scale shot speed and accuracy with charge time:
            // releasing early at 36 ticks fires slow, inaccurate arrows that land short.
            if (weapon.channel && weapon.useAmmo == AmmoID.Arrow)
            {
                int fullChargeTicks = Math.Clamp(weapon.useTime, 36, 90);
                pattern.ChannelTicks = Math.Max(pattern.ChannelTicks, fullChargeTicks);
            }

            _memoryStartUses = UsesStartedTotal;

            // A movement action waiting for its tell: tag this tell with the action's serial (the server launches on it)
            // and give it the action's own timing. One use, built on the jump, dive or pass; the hold is ShouldHoldTell.
            bool moveActionWaiting = _moveAction != MoveAction.None && _movePhase == MovePhase.Tell;

            if (!moveActionWaiting)
            {
                return;
            }

            _ownerTellSerial = _moveSerial;

            switch (_moveAction)
            {
                case MoveAction.ApexShot:
                    pattern.TellTicks = Math.Max(pattern.TellTicks, ApexMinTellTicks);
                    pattern.BurstMax = 1;
                    pattern.GapTicks = 0;
                    break;
                case MoveAction.PursuitLeap:
                    pattern.TellTicks = PursuitTellTicks;
                    pattern.BurstMax = 1;
                    pattern.GapTicks = 0;
                    pattern.RecoveryTicks = PursuitRecoveryTicks;
                    break;
                case MoveAction.HighSlam:
                    pattern.TellTicks = HighSlamTellTicks;
                    pattern.BurstMax = 1;
                    pattern.GapTicks = 0;
                    pattern.RecoveryTicks = HighSlamRecoveryTicks;
                    break;
                case MoveAction.AirPass:
                    pattern.TellTicks = AirPassTellTicks;
                    pattern.BurstMax = 1;
                    pattern.GapTicks = 0;
                    pattern.RecoveryTicks = AirPassRecoveryTicks;
                    break;
                case MoveAction.AirSlam:
                    pattern.TellTicks = AirSlamTellTicks;
                    pattern.BurstMax = 1;
                    pattern.GapTicks = 0;
                    pattern.RecoveryTicks = AirSlamRecoveryTicks;
                    break;
                case MoveAction.StrafeVolley:
                    pattern.TellTicks = StrafeVolleyTellTicks;
                    pattern.BurstMax = StrafeVolleyShots;
                    pattern.GapTicks = StrafeVolleyGapTicks;
                    pattern.RecoveryTicks = StrafeVolleyRecoveryTicks;
                    break;
            }
        }

        /// <summary>Owner side, past the authored tell: keep holding the use for a launched movement action until its
        /// release point. Apex shot: the top of the jump. Leaps and the slam: past the apex with the target inside reach
        /// plus 8 ticks of approach, or 6 ticks before touchdown. Air pass: within reach plus 6 ticks of dive speed.
        /// Not launched yet: keep holding. Touched down: swing now.</summary>
        protected override bool ShouldHoldTell()
        {
            bool holdsForThisAction = _moveAction != MoveAction.None && _ownerTellSerial == _moveSerial;

            if (!holdsForThisAction)
            {
                return false;
            }

            if (_movePhase == MovePhase.Tell)
            {
                return true;
            }

            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxPlayers;

            if (_movePhase == MovePhase.Landed || !targetIndexValid)
            {
                return false;
            }

            Player target = Main.player[TargetIndex];
            float distance = Vector2.Distance(NPC.Center, target.Center);
            float reach = GetStrikeReach(ActiveWeapon);

            switch (_moveAction)
            {
                case MoveAction.ApexShot:
                    return NPC.velocity.Y < 0f;
                case MoveAction.AirPass:
                    float passStrikeRange = reach + _flight.Config.DiveTopSpeed * AirPassStrikeLeadTicks;
                    return distance > passStrikeRange;
                case MoveAction.PursuitLeap:
                case MoveAction.HighSlam:
                case MoveAction.AirSlam:
                    bool pastApex = NPC.velocity.Y >= 0f || _moveAction == MoveAction.AirSlam;
                    float strikeRange = reach + Math.Abs(NPC.velocity.X) * LeapStrikeLeadTicks;
                    bool inStrikeRange = distance <= strikeRange;
                    bool touchdownSoon = false;

                    // Feet-to-floor gap over fall speed = ticks to touchdown (8 tiles scanned under the centre column).
                    if (NPC.velocity.Y > 0f)
                    {
                        int column = (int)(NPC.Center.X / 16f);
                        int feetRow = (int)(NPC.Bottom.Y / 16f);

                        for (int row = feetRow; row < feetRow + 8; row++)
                        {
                            Tile tile = Framing.GetTileSafely(column, row);
                            bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

                            if (standable)
                            {
                                float groundGap = row * 16f - NPC.Bottom.Y;
                                touchdownSoon = groundGap / NPC.velocity.Y <= LeapTouchdownLeadTicks;
                                break;
                            }
                        }
                    }

                    bool strikeNow = pastApex && (inStrikeRange || touchdownSoon);
                    return !strikeNow;
                default:
                    return false;
            }
        }

        protected override void WriteOwnerExtras(BinaryWriter writer)
        {
            base.WriteOwnerExtras(writer);

            // Which movement action the owner's current tell belongs to: the server launches the jump/dive on it.
            writer.Write(_ownerTellSerial);
        }

        protected override void ReadOwnerExtras(BinaryReader reader)
        {
            base.ReadOwnerExtras(reader);
            _ownerTellSerial = reader.ReadInt32();
        }

        private MeleeCombo CurrentMemoryCombo
        {
            get
            {
                switch (_memory)
                {
                    case CombatMemory.LumeliaUnderOver:
                        return HeroofLumelia.HeroCombos[1];
                    case CombatMemory.MarikHeavy:
                        return Marik.CinderCombos[2];
                    case CombatMemory.MarikFury:
                        return Marik.CinderCombos[3];
                    default:
                        return HeroofLumelia.HeroCombos[0];
                }
            }
        }

        protected override MeleeComboStep? AuthoredMeleeStep
        {
            get
            {
                MeleeCombo combo = CurrentMemoryCombo;
                int stepIndex = Math.Clamp(UsesStartedTotal - _memoryStartUses - 1, 0, combo.Steps.Length - 1);
                return combo.Steps[stepIndex];
            }
        }

        private void FaceTarget(Player target)
        {
            int facing = Math.Sign(target.Center.X - NPC.Center.X);

            if (facing != 0 && facing != NPC.direction)
            {
                NPC.direction = facing;
                NPC.netUpdate = true;
            }
        }

        /// <summary>Shots (not near-instant ones) refuse to fire inside effective speed x 20 ticks (the wrapper's rule,
        /// HostileMirrorPuppetNPC.TooCloseToFire). 0 for everything else. Callers: weapon choice and movement.</summary>
        private static void GetCombatEngagementRange(Item item, out float preferredDistance, out float maximumDistance)
        {
            if (Categorize(item) == WeaponCategory.Whip)
            {
                preferredDistance = 300f;
                maximumDistance = BloodKnightDarkHarvestWhip.MaximumReachPixels;
                return;
            }

            GetEngagementRange(item, out preferredDistance, out maximumDistance);
        }

        private static float MinimumFiringDistance(Item item)
        {
            WeaponCategory category = Categorize(item);
            bool shotRules = category == WeaponCategory.Shot || category == WeaponCategory.Channelled;

            if (!shotRules)
            {
                return 0f;
            }

            return Math.Min(EffectiveShotSpeed(item) * ShotReactionTicks, RangedMaximum - ShotDistanceMargin * 2f);
        }

        // ── Host side: stamina flags, roll cost, the target for the wrapper ─────────────────────────

        protected override void UpdateOwnerSide(Player owner)
        {
            if (_localHostRevision != _hostRevision)
            {
                _localHostRevision = _hostRevision;
                ResetUseWrapper();
            }

            // Starting class is client-local. The established owner-state stream carries this fact to the server.
            if (HasSummonerLoadout(owner))
            {
                _summonerEligible = true;
            }

            // Winded: its own stamina pool ran low → no new attacks, and the server's movement gives it a breather.
            // Fixed 200-point encounter pool, with 15% / 60% hysteresis.
            float staminaMax = 200f;
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (!winded && StaminaCurrent <= staminaMax * WindedEnterFraction)
            {
                winded = true;
            }
            else if (winded && StaminaCurrent >= staminaMax * WindedExitFraction)
            {
                winded = false;
            }

            // The server starts a roll; its fixed encounter cost is paid once here on the executing host.
            InvaderState currentState = State;
            bool rollJustStarted = currentState == InvaderState.Rolling && _ownerLastState != InvaderState.Rolling;

            if (rollJustStarted)
            {
                float rollCost = 30f;
                StaminaCurrent = Math.Max(0f, StaminaCurrent - rollCost);
            }

            if (currentState == InvaderState.Whipping && _lastChargedWhipSerial != _actionSerial)
            {
                _lastChargedWhipSerial = _actionSerial;
                StaminaCurrent = Math.Max(0f, StaminaCurrent - 30f);
            }

            _ownerLastState = currentState;

            // Wrapper state for the server (as of last tick's decision; one tick of lag is fine for movement).
            bool attacking = CurrentUsePhase == UsePhase.Tell || CurrentUsePhase == UsePhase.Using
                || UseState.ItemAnimation > 0 || HasAttachedWeapon;
            byte flags = 0;

            if (winded)
            {
                flags |= OwnerFlagWinded;
            }

            if (StaminaCurrent >= 30f)
            {
                flags |= OwnerFlagCanRoll;
            }

            if (attacking)
            {
                flags |= OwnerFlagAttacking;
            }

            if (TooCloseToFire)
            {
                flags |= OwnerFlagTooClose;
            }

            if (CurrentUsePhase == UsePhase.Recovery)
            {
                flags |= OwnerFlagRecovering;
            }

            if (LastUseFailed)
            {
                flags |= OwnerFlagUseFailed;
            }

            if (_summonerEligible)
            {
                flags |= OwnerFlagSummoner;
            }

            if (ShotBlockedByTerrain)
            {
                flags |= OwnerFlagShotBlocked;
            }

            OwnerFlags = flags;
        }

        protected override void AfterWeaponUse(Player owner, Item weapon)
        {
            base.AfterWeaponUse(owner, weapon);
            int startedUses = UsesStartedTotal - _lastChargedUse;
            _lastChargedUse = UsesStartedTotal;

            if (startedUses > 0 && weapon != null)
            {
                float cost = 15f;

                if (weapon.useAnimation >= HeavyUseTime)
                {
                    cost = 30f;
                }

                StaminaCurrent = Math.Max(0f, StaminaCurrent - cost * startedUses);
            }
        }

        protected override Vector2 GetTargetVelocity(Player owner)
        {
            int targetIndex = TargetIndex;

            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
            {
                return Vector2.Zero;
            }

            Player target = Main.player[targetIndex];

            if (!target.active || target.dead)
            {
                return Vector2.Zero;
            }

            return target.velocity;
        }

        protected override bool ChooseTarget(Player owner, out Vector2 targetCenter)
        {
            targetCenter = NPC.Center;

            // Only while fighting; winded = no new attacks until the pool recovers.
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (State != InvaderState.Fight || winded || _chooseAction || _whipRecovery > 0 || _memory == CombatMemory.DarkHarvest)
            {
                return false;
            }

            // In the air every use is a synced air action (it counts toward the sortie); the planted part of a wing
            // landing is a punish window with no attacks.
            bool airborne = _flight != null && _flight.IsAirborne;

            if (airborne && _moveAction == MoveAction.None)
            {
                return false;
            }

            if (_landingRecoveryTicks > LandingSlowWalkTicks)
            {
                return false;
            }

            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxPlayers;

            if (!targetIndexValid)
            {
                return false;
            }

            Player target = Main.player[TargetIndex];

            if (!target.active || target.dead)
            {
                return false;
            }

            Item weapon = ActiveWeapon;

            if (weapon == null || weapon.IsAir)
            {
                return false;
            }

            // Anti-whiff: only attack inside the weapon's reach (the server walks it in); shots also need a clear line.
            GetCombatEngagementRange(weapon, out _, out float maximumDistance);
            float distance = Vector2.Distance(NPC.Center, target.Center);
            bool rangedWeapon = maximumDistance > SpearLikeMaximum;

            // A leap, pass or slam closes the gap itself: its admission was checked by the server, so the tell may
            // start from further out and the use is held until strike range (ShouldHoldTell).
            bool meleeMoveAction = _moveAction == MoveAction.PursuitLeap || _moveAction == MoveAction.HighSlam
                || _moveAction == MoveAction.AirPass || _moveAction == MoveAction.AirSlam;

            if (meleeMoveAction)
            {
                maximumDistance = Math.Max(maximumDistance, MeleeMoveActionMaxDistance);
            }

            if (distance > maximumDistance)
            {
                return false;
            }

            // An apex shot jumps to clear the cover that blocks the line from the ground.
            bool lineCheckSkipped = _moveAction == MoveAction.ApexShot;

            if (rangedWeapon && !lineCheckSkipped && !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            targetCenter = target.Center;
            return true;
        }

        // ── Display ──────────────────────────────────────────────────────────────────────────────

        protected override void DecorateDisplayClone(Player clone)
        {
            base.DecorateDisplayClone(clone);

            if (State == InvaderState.Whipping)
            {
                clone.itemAnimation = 0;
                clone.heldProj = -1;
                clone.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                    _whipAim.ToRotation() - MathHelper.PiOver2);
            }

            // The flask draw layer reads the drawn player's own Estus state, so setting the CLONE's copy puts the flask
            // in the invader's hand; the raised drinking arm starts 5% into the drink, as for the player.
            EstusFlaskPlayer cloneEstus = clone.GetModPlayer<EstusFlaskPlayer>();
            bool drinkingEstus = State == InvaderState.DrinkingEstus;
            cloneEstus.IsDrinking = drinkingEstus;
            cloneEstus.EstusDrinkTimer = drinkingEstus ? _stateTimer / 60f : 0f;
            cloneEstus.EstusDrinkTimerMax = EstusDrinkTicks / 60f;
            cloneEstus.EstusChargesCurrent = _estusCharges;
            cloneEstus.EstusChargesMax = MaxEstusCharges;

            bool estusPose = drinkingEstus && _stateTimer >= EstusDrinkTicks * DrinkPoseStartFraction;

            if (estusPose)
            {
                clone.bodyFrame = PlayerFrames.Use2.ToRectangle();
            }

            // Summon cast: the staff in hand, held at the top of its swing, then swept down as the add arrives. Vanilla's
            // own pose maths (ItemCheck_ApplyUseStyle) from a hand-set use timer; body frame by the same thirds PlayerFrame
            // uses for a swing (1 = raised, 2 = level, 3 = low).
            bool castingSummon = State == InvaderState.SummoningAdd && _summonStaffCount > 0;

            if (State == InvaderState.SummoningAdd && _summonStaffCount == 0)
            {
                clone.itemAnimation = 0;
                clone.heldProj = -1;
                clone.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.Pi);
            }

            if (castingSummon)
            {
                int staffType = _summonStaffTypes[Math.Clamp(_summonStaffShown, 0, _summonStaffCount - 1)];

                if (_shownStaffItem == null || _shownStaffItem.type != staffType)
                {
                    _shownStaffItem = new Item();
                    _shownStaffItem.SetDefaults(staffType);
                }

                const int poseTimerMax = 30;
                int sweepTicks = Math.Max(0, _stateTimer - SummonHoldTicks);
                float sweepProgress = Math.Clamp(sweepTicks / (float)SummonSweepTicks, 0f, 1f);
                int poseTimer = Math.Max(1, (int)Math.Round(poseTimerMax * (1f - sweepProgress)));

                clone.inventory[0] = _shownStaffItem;
                clone.selectedItem = 0;
                clone.lastVisualizedSelectedItem = _shownStaffItem;
                clone.itemAnimationMax = poseTimerMax;
                clone.itemAnimation = poseTimer;
                clone.ItemCheck_ApplyUseStyle(clone.HeightOffsetHitboxCenter, _shownStaffItem, Item.GetDrawHitbox(staffType, clone));

                float timerFraction = poseTimer / (float)poseTimerMax;
                int bodyFrameIndex = 3;

                if (timerFraction >= 0.666f)
                {
                    bodyFrameIndex = 1;
                }
                else if (timerFraction >= 0.333f)
                {
                    bodyFrameIndex = 2;
                }

                clone.bodyFrame.Y = clone.bodyFrame.Height * bodyFrameIndex;
            }

            // Dodge roll: one full tumble over the roll's length with tucked legs (tsorcRevampPlayer.UpdateDodging).
            clone.fullRotation = 0f;

            if (State == InvaderState.Rolling)
            {
                float rollProgress = Math.Clamp(_stateTimer / (float)RollTicks, 0f, 1f);
                int rollFacing = NPC.direction == 0 ? 1 : NPC.direction;
                clone.fullRotation = rollFacing * rollProgress * MathHelper.TwoPi;
                clone.fullRotationOrigin = RollRotationOrigin;
                clone.legFrame = PlayerFrames.Jump.ToRectangle();
            }

            // Wings: the pair the copied player shows (PlayerFrame re-derives clone.wings from armor only, which misses
            // the Wing Harness slot). In flight a hidden pair still shows the functional wings. Frames follow the
            // controller as PuppetNPC does: folded on the ground, open glide while hovering, flap phase otherwise.
            bool flying = _flight != null && _flight.IsAirborne;
            int shownWings = _wingDisplaySlot;

            if (flying && shownWings <= 0)
            {
                shownWings = _wingFunctionalSlot;
            }

            clone.wings = shownWings;

            if (!flying)
            {
                clone.wingFrame = 0;
                clone.controlJump = false;
                return;
            }

            bool hoverOrStrafe = _flight.Mode == FlightMode.Hover || _flight.Mode == FlightMode.Strafe;
            bool gliding = !_flight.WingsActiveThisTick && hoverOrStrafe;
            clone.controlJump = _flight.WingsActiveThisTick;
            clone.wingTime = clone.wingTimeMax;

            if (gliding)
            {
                clone.wingFrame = 1;
            }
            else
            {
                clone.wingFrame = (int)(_flight.WingAnimPhase * 4f) % 4;
            }
        }

        /// <summary>Dissolving: sinks into the ground over its length (drawn behind tiles), slow then faster.</summary>
        protected override Vector2 DisplayDrawOffset
        {
            get
            {
                if (State != InvaderState.Dissolving)
                {
                    return Vector2.Zero;
                }

                float sinkProgress = Math.Clamp(_stateTimer / (float)DissolveTicks, 0f, 1f);
                return new Vector2(0f, sinkProgress * sinkProgress * NPC.height);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A hit chips the reflection (a few silver flecks); death shatters it.
            int fleckCount = 3;

            if (NPC.life <= 0)
            {
                fleckCount = 50;
            }

            for (int i = 0; i < fleckCount; i++)
            {
                Dust fleck = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.SilverCoin, hit.HitDirection * 2f, -2f, 0, default, 1.1f);
                fleck.noGravity = NPC.life <= 0;
            }

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Asphalt, hit.HitDirection, -1.5f, 0, default, 1.3f);
                }
            }
        }
    }

    public class MirrorSummonerPlayer : ModPlayer
    {
        internal bool ReportedSummonerClass;
        private bool _lastReportedClass;
        private int _reportTicks;

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            bool summonerClass = Player.GetModPlayer<tsorcRevampPlayer>().startingClass == StartingClass.Summoner;
            _reportTicks--;

            if (_reportTicks > 0 && summonerClass == _lastReportedClass)
            {
                return;
            }

            _lastReportedClass = summonerClass;
            _reportTicks = 300;
            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncMirrorSummonerClass);
            packet.Write(summonerClass);
            packet.Send();
        }

        internal static void ReceiveClass(BinaryReader reader, int sender)
        {
            bool summonerClass = reader.ReadBoolean();

            if (Main.netMode == NetmodeID.Server && sender >= 0 && sender < Main.maxPlayers)
            {
                Main.player[sender].GetModPlayer<MirrorSummonerPlayer>().ReportedSummonerClass = summonerClass;
            }
        }
    }

    // Echo ownership must cover native invader OnSpawn helpers, later NPC children and projectile children.
    // Kept with the encounter; normal invaders never enter this scope and retain their progression behavior.
    public class MirrorEchoSystem : ModSystem
    {
        internal static MirrorInvader SpawningFor;
        internal static MirrorInvader CleaningFor;
        internal static int NextEncounterId = 1;

        public override void Load()
        {
            On_NPC.NewNPC += SpawnEchoDescendant;
            On_NPC.AI += RunEchoNPC;
            On_Projectile.AI += RunEchoProjectile;
            On_Projectile.Kill += KillEchoProjectile;
        }

        public override void Unload()
        {
            On_NPC.NewNPC -= SpawnEchoDescendant;
            On_NPC.AI -= RunEchoNPC;
            On_Projectile.AI -= RunEchoProjectile;
            On_Projectile.Kill -= KillEchoProjectile;
            SpawningFor = null;
            CleaningFor = null;
        }

        private static void RunEchoNPC(On_NPC.orig_AI original, NPC npc)
        {
            MirrorInvader previous = SpawningFor;
            SpawningFor ??= npc.GetGlobalNPC<MirrorEchoNPC>().OwnerActor;

            try
            {
                original(npc);
            }
            finally
            {
                SpawningFor = previous;
            }
        }

        private static void RunEchoProjectile(On_Projectile.orig_AI original, Projectile projectile)
        {
            MirrorInvader previous = SpawningFor;
            SpawningFor ??= projectile.GetGlobalProjectile<MirrorEchoProjectile>().OwnerActor;

            try
            {
                original(projectile);
            }
            finally
            {
                SpawningFor = previous;
            }
        }

        private static void KillEchoProjectile(On_Projectile.orig_Kill original, Projectile projectile)
        {
            MirrorInvader previous = SpawningFor;
            SpawningFor ??= projectile.GetGlobalProjectile<MirrorEchoProjectile>().OwnerActor;

            try
            {
                original(projectile);
            }
            finally
            {
                SpawningFor = previous;
            }
        }

        private static int SpawnEchoDescendant(On_NPC.orig_NewNPC original, IEntitySource source,
            int spawnX, int spawnY, int type, int start, float ai0, float ai1, float ai2, float ai3, int target)
        {
            MirrorInvader previous = SpawningFor;

            if (source is EntitySource_Parent parent)
            {
                if (parent.Entity is NPC parentNPC)
                {
                    MirrorEchoNPC tag = parentNPC.GetGlobalNPC<MirrorEchoNPC>();

                    if (IsCleaning(tag.OwnerIndex, tag.EncounterId))
                    {
                        return Main.maxNPCs;
                    }

                    SpawningFor ??= tag.OwnerActor;
                }
                else if (parent.Entity is Projectile parentProjectile)
                {
                    MirrorEchoProjectile tag = parentProjectile.GetGlobalProjectile<MirrorEchoProjectile>();

                    if (IsCleaning(tag.OwnerIndex, tag.EncounterId))
                    {
                        return Main.maxNPCs;
                    }

                    SpawningFor ??= tag.OwnerActor;
                }
            }

            try
            {
                if (SpawningFor != null && (type == ModContent.NPCType<Kahlrun>()
                    || IsCleaning(SpawningFor.NPC.whoAmI, SpawningFor.EncounterId)))
                {
                    return Main.maxNPCs;
                }

                return original(source, spawnX, spawnY, type, start, ai0, ai1, ai2, ai3, target);
            }
            finally
            {
                SpawningFor = previous;
            }
        }

        internal static void RemoveFamily(MirrorInvader owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            MirrorInvader previous = CleaningFor;
            MirrorInvader previousSpawn = SpawningFor;
            CleaningFor = owner;
            SpawningFor = owner;

            try
            {
                foreach (NPC candidate in Main.ActiveNPCs)
                {
                    MirrorEchoNPC echo = candidate.GetGlobalNPC<MirrorEchoNPC>();

                    if (echo.OwnerIndex == owner.NPC.whoAmI && echo.EncounterId == owner.EncounterId)
                    {
                        echo.BeginDissolve(candidate);
                    }
                }

                foreach (Projectile projectile in Main.ActiveProjectiles)
                {
                    MirrorEchoProjectile echo = projectile.GetGlobalProjectile<MirrorEchoProjectile>();

                    if (echo.OwnerIndex == owner.NPC.whoAmI && echo.EncounterId == owner.EncounterId)
                    {
                        projectile.damage = 0;
                        projectile.Kill();
                    }
                }
            }
            finally
            {
                CleaningFor = previous;
                SpawningFor = previousSpawn;
            }
        }

        internal static bool IsCleaning(int ownerIndex, int encounterId)
        {
            return CleaningFor != null && CleaningFor.NPC.whoAmI == ownerIndex
                && CleaningFor.EncounterId == encounterId;
        }
    }

    public class MirrorEchoNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        internal int OwnerIndex = -1;
        internal int EncounterId;
        private int _arrivalAge;
        internal bool PrimaryEcho;
        private int _dissolveTicks;
        private bool _dissolving;

        internal void BeginDissolve(NPC npc)
        {
            if (_dissolving)
            {
                return;
            }

            _dissolveTicks = 30;
            _dissolving = true;
            npc.dontTakeDamage = true;
            npc.damage = 0;
            npc.netUpdate = true;
        }

        internal MirrorInvader OwnerActor
        {
            get
            {
                if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs
                    && Main.npc[OwnerIndex].active
                    && Main.npc[OwnerIndex].ModNPC is MirrorInvader owner
                    && owner.EncounterId == EncounterId)
                {
                    return owner;
                }

                return null;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            MirrorInvader owner = MirrorEchoSystem.SpawningFor;

            if (owner == null)
            {
                return;
            }

            OwnerIndex = owner.NPC.whoAmI;
            EncounterId = owner.EncounterId;
            npc.SpawnedFromStatue = true;
            npc.value = 0f;
            npc.boss = false;

            if (npc.ModNPC is PuppetNPC puppet)
            {
                puppet.MirrorShell = true;
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (OwnerIndex < 0)
            {
                return true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && OwnerActor == null)
            {
                BeginDissolve(npc);
            }

            if (_dissolving)
            {
                _dissolveTicks = Math.Max(0, _dissolveTicks - 1);
                npc.alpha = 255 - _dissolveTicks * 255 / 30;
                npc.dontTakeDamage = true;
                npc.damage = 0;
                npc.velocity *= 0.8f;

                if (!Main.dedServ)
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                        DustID.DungeonSpirit, 0f, -1f, 100, default, 1.2f);
                    dust.noGravity = true;
                }

                if (_dissolveTicks == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.active = false;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                }

                return false;
            }

            npc.value = 0f;
            npc.boss = false;
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SuppressGlobalOnKillDrops = true;

            if (_arrivalAge < 30)
            {
                _arrivalAge++;
                npc.alpha = 255 - _arrivalAge * 255 / 30;
                npc.dontTakeDamage = true;
                npc.velocity = Vector2.Zero;
                return false;
            }

            if (_arrivalAge == 30)
            {
                _arrivalAge++;
                npc.dontTakeDamage = false;
                npc.alpha = 0;
            }

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (OwnerIndex < 0)
            {
                return;
            }

            npc.boss = false;
            npc.value = 0f;

            // Native first-AI balance initialization and mounted invaders must not inflate the echo pool.
            if (PrimaryEcho && npc.lifeMax != MirrorInvader.MirrorAddLife)
            {
                npc.lifeMax = MirrorInvader.MirrorAddLife;
                npc.life = Math.Min(npc.life, npc.lifeMax);
                npc.netUpdate = true;
            }
        }

        public override bool PreKill(NPC npc)
        {
            return OwnerIndex < 0;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write((short)OwnerIndex);

            if (OwnerIndex >= 0)
            {
                writer.Write(EncounterId);
                writer.Write(PrimaryEcho);
                writer.Write((byte)_arrivalAge);
                writer.Write((byte)_dissolveTicks);
                writer.Write(_dissolving);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            OwnerIndex = reader.ReadInt16();

            if (OwnerIndex >= 0)
            {
                EncounterId = reader.ReadInt32();
                PrimaryEcho = reader.ReadBoolean();
                _arrivalAge = reader.ReadByte();
                _dissolveTicks = reader.ReadByte();
                _dissolving = reader.ReadBoolean();
                npc.boss = false;
                npc.value = 0f;
            }
        }
    }

    public class MirrorEchoProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        internal int OwnerIndex = -1;
        internal int EncounterId;

        internal MirrorInvader OwnerActor
        {
            get
            {
                if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs && Main.npc[OwnerIndex].active
                    && Main.npc[OwnerIndex].ModNPC is MirrorInvader owner && owner.EncounterId == EncounterId)
                {
                    return owner;
                }

                return null;
            }
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            MirrorInvader owner = MirrorEchoSystem.SpawningFor;

            if (owner != null && MirrorEchoSystem.IsCleaning(owner.NPC.whoAmI, owner.EncounterId))
            {
                projectile.damage = 0;
                projectile.active = false;
                return;
            }

            if (source is EntitySource_Parent parent)
            {
                if (parent.Entity is NPC parentNPC)
                {
                    MirrorEchoNPC tag = parentNPC.GetGlobalNPC<MirrorEchoNPC>();

                    if (MirrorEchoSystem.IsCleaning(tag.OwnerIndex, tag.EncounterId))
                    {
                        // Parent Kill still plays its native impact. Suppress gameplay children during encounter
                        // cleanup, otherwise a death burst can create a fresh unowned hazard after the scan passed.
                        projectile.damage = 0;
                        projectile.active = false;
                        return;
                    }

                    owner ??= tag.OwnerActor;
                }
                else if (parent.Entity is Projectile parentProjectile)
                {
                    MirrorEchoProjectile tag = parentProjectile.GetGlobalProjectile<MirrorEchoProjectile>();

                    if (MirrorEchoSystem.IsCleaning(tag.OwnerIndex, tag.EncounterId))
                    {
                        projectile.damage = 0;
                        projectile.active = false;
                        return;
                    }

                    owner ??= tag.OwnerActor;
                }
            }

            if (owner != null)
            {
                OwnerIndex = owner.NPC.whoAmI;
                EncounterId = owner.EncounterId;
                projectile.netUpdate = true;
            }
        }

        public override bool PreAI(Projectile projectile)
        {
            if (OwnerIndex >= 0 && OwnerActor == null && Main.netMode != NetmodeID.MultiplayerClient)
            {
                projectile.damage = 0;
                projectile.Kill();
                return false;
            }

            return true;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write((short)OwnerIndex);

            if (OwnerIndex >= 0)
            {
                writer.Write(EncounterId);
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            OwnerIndex = reader.ReadInt16();

            if (OwnerIndex >= 0)
            {
                EncounterId = reader.ReadInt32();
            }
        }
    }
}
