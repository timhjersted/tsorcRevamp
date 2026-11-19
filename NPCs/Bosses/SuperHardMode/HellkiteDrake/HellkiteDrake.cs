using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.BossItems;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDrake
{
    [AutoloadBossHead]
    public class HellkiteDrake : ModNPC, IDebugAttackLabel, IStaggerable
    {
        public enum DrakeState
        {
            Settle,
            Ground,
            Turn,
            Bite,
            Breath,
            TailSlam,
            Takeoff,
            AirApproach,
            AirBreath,
            Land,
            Recover,
            // The quick walk-in before each bite of the triple bite; also the attack-bag card for the whole chain.
            BiteChain,
            // Crouch, a low ballistic hop solved from the target's position, jaws snapping on arrival, landing settle.
            LeapBite,
            // Big wing beats carry it up and backwards off the ground, breathing a long cone back at the target on the way.
            RetreatBreath,
            // One wing beat and pawing claws, then a sprint along the ground aimed past the target; the body is the weapon.
            Charge,
            GrabBite,
            GrabHold,
            GrabToss,
            DestinedDeathRain,
            AirTurn,
            AirDive,
            Collapse
        }

        public const float ArtScale = 1f;
        public const int BiteTell = 30;
        public const int BiteLive = 16;
        public const int BiteHoldTicks = 8;
        public const int BiteRetractTicks = 18;
        // Triple bite: walk in for 28 ticks, then a normal bite, three times. Advance 28 + tell 30 + the 42 ticks of
        // live/hold/retract left over from the previous bite puts each bite's live window 100 ticks after the last.
        public const int BiteChainBites = 3;
        public const int BiteChainAdvanceTicks = 28;
        public const float BiteChainWalkSpeed = 3.4f;
        // The jaws aim before the torso swings over at tick 50 and go live while the neck is still in transit.
        public const int TurnBiteAimLockTick = 40;
        public const int TurnBiteLiveStart = 54;
        // The swung head hangs ~365px out on the new side. The neck slides the snout up to TurnBiteReach px and a
        // hop carries the whole body up to 144px back or 128px forward to reach across the crossover lane.
        public const float TurnBiteReach = 130f;
        public const float TurnBiteHopBackSpeed = 9f;
        public const float TurnBiteHopForwardSpeed = 8f;
        public const float TurnBiteMaxRange = 600f;
        private const float TurnThreshold = 80f;
        private const int TurnFinishTick = TurnBiteLiveStart + BiteLive + BiteHoldTicks + BiteRetractTicks;
        // Leap bite: 40-tick crouch, lock the intercept at tick 30, downstroke at launch. The longer arcs are
        // 44–82 ticks with the same manual gravity on every peer. Only the last 12 flight ticks and first 6
        // grounded ticks can bite, then the jaws retract and the normal 30-tick recovery starts.
        public const float LeapMinRange = 240f;
        public const float LeapMaxRange = 1550f;
        public const int LeapTell = 40;
        public const int LeapLockTick = 30;
        public const float LeapGravity = 0.3f;
        public const int LeapMinAirTicks = 44;
        public const int LeapMaxAirTicks = 82;
        public const int LeapBiteLiveLead = 12;
        public const int LeapBiteLandLive = 6;
        public const float LeapBiteReach = 96f;
        // The snout is solved to finish this far PAST where the target stood at the lock, so standing still is a hit.
        public const float LeapOvershoot = 40f;
        // Takeoff ignores its cooldown and the ground minimum once the target is this far away.
        public const float FarFlightRange = 1000f;
        public const int BreathTell = 70;
        public const int BreathLive = 90;
        public const int BreathCloseTicks = 12;
        public const int BreathShotInterval = 3;
        public const float BreathSweepDegreesPerTick = 0.6f;
        private const int RainTell = 60;
        private const int RainAimLock = 42;
        private const int RainWaves = 4;
        private const int RainFlamesPerWave = 4;
        private const int RainWaveInterval = 6;
        private const int RainLastWave = RainTell + (RainWaves - 1) * RainWaveInterval;
        private const int RainFollowThrough = 18;
        private const int RainCooldownTicks = 300;
        private const float RainFieldWidth = 50f * 16f;
        private const int RainImpactDamage = 68;
        public const int TailTell = 46;
        public const int TailLive = 12;
        // Retreat breath: the answer to a target hugging the front (under the neck up to 200px past the mouth).
        // 30-tick wing-spread tell, then a 120-tick climb 480px back (30 tiles) and 288px up. The breath opens two
        // thirds of the way through the climb and burns 70 ticks, so it finishes while hovering at the far end.
        public const int RetreatTell = 30;
        public const int RetreatMoveTicks = 120;
        public const int RetreatFireTick = RetreatTell + 80;
        public const int RetreatBreathLive = 70;
        public const float RetreatBack = 480f;
        public const float RetreatUp = 288f;
        // Long flame: 12 px/tick for LongFlameLifetime ticks = 804px (50 tiles). Lanes at +-5.7 degrees open the cone
        // to ~160px (10 tiles) across at full range.
        public const float LongFlameSpeed = 12f;
        public const int LongFlameLifetime = 67;
        public const float LongFlameSpreadDegrees = 5.7f;
        // Strafe telegraph: the near foot lifts at takeoff and slams down on this tick.
        public const int StrafeStompTick = 12;
        // Ground charge: 45-tick tell (one wing beat, claws pawing; run solved at tick 35), then a run whose speed
        // rises and falls on a sine curve, averaging ChargeAverageSpeed px/tick and peaking at 1.57x that. The body
        // is aimed to stop ChargeOvershoot px past where the target stood, then skids for 16 ticks.
        public const int ChargeTell = 45;
        public const int ChargeLockTick = 35;
        public const float ChargeAverageSpeed = 9f;
        public const float ChargeOvershoot = 200f;
        public const float ChargeMinRange = 150f;
        public const float ChargeMaxRange = 700f;
        public const int ChargeSkidTicks = 16;
        public const int ChargeAttack = 5;
        public const int NearFootAttack = 6;
        public const int FarFootAttack = 7;
        public const int FootfallTicks = 6;
        private const int FootfallDamage = 60;
        private const float PursuitWalkSpeed = 5.2f;
        private const float PriorityLeapRange = 420f;
        private const int LeapLandingCooldown = 70;
        private const int RecoveryTicks = 30;
        public const int GrabBiteAttack = 8;
        public const int GrabThrowAttack = 9;
        private const int GrabTell = 48;
        private const int GrabHoldDuration = 52;
        private const int GrabTossDuration = 18;
        private const int GrabBiteDamage = 80;
        private const int GrabThrowDamage = 75;
        private const float GrabMaxRange = 200f;
        private const float GrabThrowSpeed = 20f;
        public const int AirTell = 44;
        public const int AirLive = 96;
        public const int AirStrafeLive = 186;
        public const byte AirBreathHover = 0;
        public const byte AirBreathStrafe = 1;
        public const byte AirBreathSkyward = 2;
        private const byte AirClawSlam = 3;
        private const byte AirLandingBite = 4;
        private const float FlightClimb = 320f;
        private const float FlightCruiseSpeed = 17f;
        private const float AirStrafeSpeed = 14f;
        private const float AirStrafeOvershoot = 80f * 16f;
        private const int AirTurnFlipTick = 9;
        private const int AirTurnTicks = 18;
        private const int AirDiveTell = 32;
        private const int AirDiveBiteLandTicks = 9;
        private const int AirSlamDamage = 105;
        public const int AirLandingBiteAttack = 10;
        private const int CollapseImpactTick = 13;
        private const int CollapseRestTicks = 88;
        private const int CollapsePlumeTicks = 18;
        private const float CollapsePlumeHeight = 8f * 16f;
        private const int SlamWaveTicks = 11;
        private const int MeteorDamage = 60;
        // A target standing this far above the nearest floor counts as "in the sky" (10 tiles).
        public const float SkyTargetHeight = 160f;
        // Aim limits in radians; positive pitch points the jaw down.
        public const float AirAimDownLimit = 1.43f;
        public const float AirAimUpLimit = 1.26f;
        public const int HeadDefense = 0;
        public const int NeckDefense = 150;
        public const int BodyDefense = 500;
        public const int TailDefense = 400;
        private const int TurnStretchFactor = 3;
        private const int TurnTicks = 104 * TurnStretchFactor;
        private const int TurnNeckTicks = 70 * TurnStretchFactor;
        private const int TurnNeckRootTicks = 30 * TurnStretchFactor;
        private const int TurnBodyTick = 50;
        private const int TurnNeckLeadTick = 24 * TurnStretchFactor;
        private const int TurnHeadTick = 9 * TurnStretchFactor;
        private const int GroundMinimum = 90;
        private const float LegWalkPlaybackRate = 2f;
        // Pivot about the planted near claw (source contact spans x=390–439), aligned with NPC.Bottom.
        private static readonly Vector2 SourceFloor = new Vector2(414f, 474f);
        private static readonly Vector2 SnoutSocket = new Vector2(16f, 372f);
        // Frame pixel (48,86) sits in the open jaw's gap, just below the upper teeth.
        private static readonly Vector2 BreathMouthSocket = new Vector2(54f, 374f);
        private static readonly Vector2 GrabMouthSocket = new Vector2(48f, 374f);
        private static readonly Vector2 GrabEyeSocket = new Vector2(59f, 351f);
        private static readonly int[] WalkTicks = { 13, 8, 6, 4, 13, 8, 6, 4 };
        private static readonly int[] WingCycle = { 3, 2, 1, 1, 2, 3, 4, 4 };
        private static readonly int[] LeapWingRows = { 0, 2, 1, 2, 3, 4, 4, 3, 2, 3 };
        private const int LeapWingTellTicks = 15;
        private static readonly int[] TurnWingRows = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1, 0 };
        private static readonly int[] NeckParts = { 16, 17, 1, 2, 3, 15 };
        private static readonly float[] NeckTurnAngles = { 0.55f, 0.75f, 0.9f, 1f, 1f };
        private static readonly float[] TailRaiseAngles = { -0.35f, -0.50f, -0.60f, -0.62f };
        private static readonly float[] TailSlamAngles = { 0.42f, 0.48f, 0.72f, 0.75f };
        private static readonly Vector2[] TurnFootContacts =
        {
            new Vector2(414f, 473f),
            new Vector2(426f, 473f),
            new Vector2(446f, 473f),
            new Vector2(468f, 473f),
            new Vector2(494f, 465f),
            new Vector2(462f, 463f),
            new Vector2(466f, 441f),
            new Vector2(442f, 469f)
        };
        // Centers of the claws in each leg frame, measured 10px above the visible sole.
        private static readonly Vector2[] NearClawPixels =
        {
            new Vector2(49f, 153f), new Vector2(61f, 153f), new Vector2(81f, 153f), new Vector2(94f, 153f),
            new Vector2(114f, 145f), new Vector2(89f, 143f), new Vector2(67f, 121f), new Vector2(45f, 149f),
            new Vector2(120f, 113f), new Vector2(138f, 105f)
        };
        private static readonly Vector2[] FarClawPixels =
        {
            new Vector2(45f, 137f), new Vector2(57f, 137f), new Vector2(77f, 137f), new Vector2(93f, 137f),
            new Vector2(111f, 129f), new Vector2(88f, 127f), new Vector2(64f, 107f), new Vector2(43f, 133f),
            new Vector2(109f, 105f), new Vector2(122f, 93f)
        };
        private static readonly Vector2[] TailRest =
        {
            new Vector2(504f, 332f),
            new Vector2(604f, 360f),
            new Vector2(752f, 360f),
            new Vector2(872f, 256f),
            new Vector2(800f, 188f)
        };
        private static readonly Vector2[] FallenNeck =
        {
            new Vector2(297f, 426f), new Vector2(277f, 429f),
            new Vector2(245f, 438f), new Vector2(161f, 442f), new Vector2(98f, 447f)
        };
        private static readonly Vector2[] FallenTail =
        {
            new Vector2(606f, 426f), new Vector2(756f, 444f),
            new Vector2(887f, 451f), new Vector2(815f, 447f)
        };

        private static readonly Part[] Parts =
        {
            new Part("Torso", 298, 160, new Vector2(400f, 332f), new Vector2(194f, 70f), 1),
            new Part("Neck1Forward", 160, 184, new Vector2(280f, 388f), new Vector2(38f, 126f), 1),
            new Part("Neck2", 112, 116, new Vector2(260f, 392f), new Vector2(94f, 62f), 1),
            new Part("Neck3", 94, 98, new Vector2(176f, 392f), new Vector2(74f, 58f), 1),
            new Part("Tail1", 124, 108, new Vector2(504f, 332f), new Vector2(16f, 46f), 1),
            new Part("Tail2", 178, 88, new Vector2(604f, 360f), new Vector2(16f, 46f), 1),
            new Part("Tail3", 172, 138, new Vector2(752f, 360f), new Vector2(18f, 114f), 1),
            new Part("TailTip", 124, 96, new Vector2(872f, 256f), new Vector2(90f, 74f), 1),
            new Part("ShoulderFill", 204, 136, new Vector2(400f, 332f), new Vector2(100f, 88f), 1),
            new Part("WingNear", 524, 466, new Vector2(316f, 302f), new Vector2(20f, 290f), 5),
            new Part("WingFar", 286, 318, new Vector2(270f, 300f), new Vector2(156f, 190f), 5),
            new Part("WingNearWalk", 324, 70, new Vector2(316f, 302f), new Vector2(10f, 50f), 8),
            new Part("WingFarWalk", 148, 54, new Vector2(270f, 300f), new Vector2(24f, 48f), 8),
            new Part("LegNear", 178, 168, new Vector2(492f, 354f), new Vector2(110f, 44f), 10),
            new Part("LegFar", 164, 152, new Vector2(416f, 364f), new Vector2(104f, 38f), 10),
            new Part("HeadMouth", 154, 130, new Vector2(112f, 364f), new Vector2(106f, 76f), 3),
            new Part("NeckRoot", 160, 184, new Vector2(320f, 344f), new Vector2(78f, 114f), 1),
            new Part("Neck1Mid", 160, 184, new Vector2(300f, 366f), new Vector2(58f, 120f), 1),
            new Part("ShoulderScales", 160, 184, new Vector2(400f, 332f), new Vector2(158f, 102f), 1),
            new Part("NeckRootExtension", 160, 184, new Vector2(320f, 344f), new Vector2(78f, 114f), 1),
            new Part("Neck1MidExtension", 160, 184, new Vector2(300f, 366f), new Vector2(58f, 120f), 1),
            new Part("Neck1ForwardExtension", 160, 184, new Vector2(280f, 388f), new Vector2(38f, 126f), 1),
            new Part("ShoulderSkin", 160, 208, new Vector2(400f, 332f), new Vector2(158f, 102f), 1)
        };
        private static readonly float NearClawPlantX = Parts[13].RestPivot.X - Parts[13].Origin.X + NearClawPixels[0].X - SourceFloor.X;

        internal readonly int[] partHitboxes = { -1, -1, -1, -1, -1, -1, -1, -1 };
        private readonly Bone[] bones = new Bone[23];
        private readonly Vector2[] tailJoints = new Vector2[5];
        private readonly Vector2[] turnJoints = new Vector2[6];
        private static Asset<Texture2D> eyeFlare;
        private AttackBag<DrakeState> attackBag;
        private AttackBag<byte> airAttackBag;
        private Vector2 lockedAim = -Vector2.UnitX;
        private Vector2 biteWindupAim = -Vector2.UnitX;
        private Vector2 lockedImpact;
        private Vector2 waypoint;
        private Vector2 home;
        private Vector2 previousMouth;
        private Vector2 previousTail;
        private float walkClock;
        private float biteSpeed = 7.5f;
        private float biteReach = 96f;
        private float currentBiteExtension;
        private byte airBreathMode;
        private bool airTurnToPursue;
        private int airBiteStart = -1;
        private int airTouchdown = -1;
        private int collapseTouchdown = -1;
        private int collapseImpactSequence = -1;
        private float landingDrift;
        // 0 = not chaining; 1..BiteChainBites = which bite of the triple bite is in progress.
        private byte biteChainStep;
        private bool turnBite;
        private bool turnBreath;
        private NPCDespawnHandler despawnHandler;
        // Leap bite: planned airtime of the hop, and the state tick it actually touched down on (-1 while not landed).
        private int leapAirTicks = LeapMinAirTicks;
        private int leapLandTick = -1;
        private int leapCooldown;
        private float leapLaunchY;
        private int meteorTimer = 68;
        private int meteorsScattered;
        private int meteorsDirected;
        private int directedMeteorIndex;
        internal int GrabTarget { get; private set; } = -1;
        private int grabContactTick;
        // Ground charge: length of the run in ticks. Cut short on the authority if a wall or ledge stops it.
        private int chargeTicks = 60;
        private int flightCooldown;
        private int rainCooldown;
        private int tailCooldown;
        private int groundedTicks;
        private int turnFacing = -1;
        private int sequence;
        private int visualSequence = -1;
        private int breathSoundSequence = -1;
        private int airSlamSoundSequence = -1;
        private bool wasGrounded;
        private bool attackHopPending;
        private int airborneLandingTicks;
        private int wingFlapCount;
        private bool feetWereGrounded;
        private int blockedTicks;
        private bool initialized;

        public DrakeState State => (DrakeState)(int)NPC.ai[0];
        public int StateTimer => (int)NPC.ai[1];
        public int Facing
        {
            get
            {
                if (State == DrakeState.Turn)
                {
                    return StateTimer >= TurnBodyTick ? -turnFacing : turnFacing;
                }

                return NPC.ai[2] < 0f ? -1 : 1;
            }
        }
        public int Sequence => sequence;
        public Vector2 Mouth { get; private set; }
        public Vector2 Jaw { get; private set; }
        public Vector2 TailEnd { get; private set; }
        public Vector2 TailJoint { get; private set; }
        public Vector2 TailBase { get; private set; }
        public Vector2 BreathDirection { get; private set; }
        public bool Airborne => State == DrakeState.Takeoff || State == DrakeState.AirApproach
            || State == DrakeState.AirBreath || State == DrakeState.Land || State == DrakeState.RetreatBreath
            || State == DrakeState.AirTurn || State == DrakeState.AirDive;

        // Either breath made from the air; they share the neck aim, jaw timing, mouth glow and sounds.
        private bool AirBreathing => State == DrakeState.AirBreath || State == DrakeState.RetreatBreath;
        private bool AirDivingBite => State == DrakeState.AirDive && airBreathMode == AirLandingBite;
        private bool AirDivingSlam => State == DrakeState.AirDive && airBreathMode == AirClawSlam;
        private bool AirFeetExtended => (AirDivingSlam && StateTimer >= 13)
            || (AirDivingBite && (airBiteStart >= 0 || NPC.collideY))
            || State == DrakeState.Land && (NPC.collideY || Math.Abs(NPC.Bottom.Y - waypoint.Y) < 48f);
        private bool GroundBiting => State == DrakeState.Bite || State == DrakeState.GrabBite;
        private int BiteWindupTicks => State == DrakeState.Bite ? BiteTell : GrabTell;

        // Ticks the takeoff spends crouched on the ground before lifting. A planned strafe holds it for the stomp:
        // foot up for StrafeStompTick ticks, slam, then 14 ticks for the dirt to roll out before the wings take over.
        private int TakeoffCrouchTicks
        {
            get
            {
                if (airBreathMode == AirBreathStrafe)
                {
                    return StrafeStompTick + 14;
                }

                return 12;
            }
        }

        // State tick the flames start on for the current aerial breath.
        private int AirTellTicks
        {
            get
            {
                if (State == DrakeState.RetreatBreath)
                {
                    return RetreatFireTick;
                }

                return AirTell;
            }
        }
        public string DebugAttackLabel
        {
            get
            {
                string label = "Hellkite Drake " + DebugLabels.Humanize(State.ToString());

                if (State == DrakeState.AirApproach || State == DrakeState.AirBreath || State == DrakeState.AirDive)
                {
                    if (airBreathMode == AirBreathStrafe)
                    {
                        label += " (Flyby Sweep)";
                    }
                    else if (airBreathMode == AirBreathSkyward)
                    {
                        label += " (Skyward)";
                    }
                    else if (airBreathMode == AirClawSlam)
                    {
                        label += " (Claw Slam)";
                    }
                    else if (airBreathMode == AirLandingBite)
                    {
                        label += " (Landing Bite)";
                    }
                    else
                    {
                        label += " (Hover)";
                    }
                }

                if (State == DrakeState.Bite && biteChainStep > 0)
                {
                    label += " " + biteChainStep + "/" + BiteChainBites;
                }

                if (State == DrakeState.Turn && turnBite)
                {
                    label += " (Bite)";
                }
                else if (State == DrakeState.Turn && turnBreath)
                {
                    label += " (Breath)";
                }

                return label + " " + StateTimer;
            }
        }

        // Live flame ticks of the current aerial breath: the strafe burns longer so its run crosses the target.
        private int AirLiveTicks
        {
            get
            {
                if (State == DrakeState.RetreatBreath)
                {
                    return RetreatBreathLive;
                }

                if (airBreathMode == AirBreathStrafe)
                {
                    return AirStrafeLive;
                }

                return AirLive;
            }
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Daybreak] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 140;
            NPC.height = 148;
            NPC.scale = 1f;
            NPC.aiStyle = -1;
            NPC.lifeMax = 475000;
            NPC.defense = BodyDefense;
            NPC.damage = 0;
            NPC.boss = true;
            NPC.knockBackResist = 0f;
            NPC.value = 250000f;
            NPC.rarity = 31;
            NPC.lavaImmune = true;
            NPC.npcSlots = 10f;
            NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit13;
            Music = MusicID.Boss2;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.HellkiteDrake.DespawnHandler"),
                new Color(175, 75, 255), DustID.OrangeTorch);
        }

        public override bool CheckActive()
        {
            return false;
        }

        // ── Wild Drake (Primordial world) ─────────────────────────────────────────────────────────────────────────────
        // A Drake from natural Primordial spawning is an ordinary enemy on the same NPC: no boss flag, bar, music or item
        // loot, a quarter of the boss soul value, and no defeat record. The boss from a rematch or event is unchanged.
        public const float WildSoulValue = 62500f;

        /// <summary>Server-decided at spawn, synced in ExtraAI. Clients rebuild the boss flag and music from it.</summary>
        public bool Wild;

        public override void OnSpawn(IEntitySource source)
        {
            bool naturalSpawn = source is EntitySource_SpawnNPC;
            if (naturalSpawn && tsorcRevampWorld.PrimordialAge)
            {
                Wild = true;
                ApplyWildState();
                NPC.netUpdate = true;
            }
        }

        private void ApplyWildState()
        {
            NPC.boss = false;
            NPC.value = WildSoulValue;
            Music = -1;
        }

        public override void BossLoot(ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Every item drop sits behind "not a wild Drake"; a wild one gives Dark Souls only (from NPC.value).
            LeadingConditionRule bossLoot = new LeadingConditionRule(new NotWildDrakeCondition());
            bossLoot.OnSuccess(ItemDropRule.BossBag(ModContent.ItemType<HellkiteBag>()));
            bossLoot.OnSuccess(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule,
                ModContent.ItemType<GuardianSoul>()));

            LeadingConditionRule classicLoot = new LeadingConditionRule(new Conditions.NotExpert());
            classicLoot.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HellkiteStone>()));
            classicLoot.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HiRyuuSpear>()));
            classicLoot.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 12, 16));
            classicLoot.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 600, 700));
            bossLoot.OnSuccess(classicLoot);
            npcLoot.Add(bossLoot);
        }

        private sealed class NotWildDrakeCondition : IItemDropRuleCondition
        {
            public bool CanDrop(DropAttemptInfo info)
            {
                return info.npc == null || !(info.npc.ModNPC is HellkiteDrake drake && drake.Wild);
            }

            public bool CanShowItemDropInUI()
            {
                return true;
            }

            public string GetConditionDescription()
            {
                return null;
            }
        }

        public override bool CheckDead()
        {
            if (State == DrakeState.Collapse && collapseTouchdown >= 0 && StateTimer - collapseTouchdown >= CollapseRestTicks)
            {
                return true;
            }

            if (State != DrakeState.Collapse && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[2] = Facing;
                collapseTouchdown = -1;
                EnterState(DrakeState.Collapse);
            }

            NPC.life = 1;
            NPC.dontTakeDamage = true;
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 && State != DrakeState.Collapse)
            {
                EmitHitDust(NPC.Center, hit, bareHead: false);
            }
        }

        internal static void EmitHitDust(Vector2 center, NPC.HitInfo hit, bool bareHead)
        {
            if (Main.dedServ)
            {
                return;
            }

            int dustType = bareHead ? DustID.Blood : DustID.Stone;
            int count = bareHead ? 4 : 6;
            for (int i = 0; i < count; i++)
            {
                Vector2 position = center + Main.rand.NextVector2Circular(10f, 8f);
                Vector2 drift = new Vector2(hit.HitDirection * Main.rand.NextFloat(0.7f, 2.8f), Main.rand.NextFloat(-2.5f, -0.6f));
                Dust.NewDustPerfect(position, dustType, drift, 0, default, Main.rand.NextFloat(0.55f, 0.85f));
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ShareHitImmunity(player.whoAmI, null);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            ShareHitImmunity(projectile.owner, projectile);
        }

        internal void ShareHitImmunity(int playerIndex, Projectile projectile)
        {
            int immuneTime = 0;
            if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
            {
                immuneTime = NPC.immune[playerIndex];
                for (int i = 0; i < partHitboxes.Length; i++)
                {
                    int childIndex = partHitboxes[i];
                    if (childIndex >= 0 && childIndex < Main.maxNPCs && Main.npc[childIndex].active)
                    {
                        immuneTime = Math.Max(immuneTime, Main.npc[childIndex].immune[playerIndex]);
                    }
                }
            }

            if (immuneTime > 0 && playerIndex >= 0 && playerIndex < Main.maxPlayers)
            {
                NPC.immune[playerIndex] = Math.Max(NPC.immune[playerIndex], immuneTime);
            }

            if (projectile != null && projectile.usesLocalNPCImmunity)
            {
                int localCooldown = projectile.localNPCImmunity[NPC.whoAmI];
                for (int i = 0; i < partHitboxes.Length; i++)
                {
                    int childIndex = partHitboxes[i];
                    if (childIndex >= 0 && childIndex < Main.maxNPCs)
                    {
                        localCooldown = Math.Max(localCooldown, projectile.localNPCImmunity[childIndex]);
                    }
                }

                if (localCooldown != 0)
                {
                    projectile.localNPCImmunity[NPC.whoAmI] = localCooldown;
                }
            }

            for (int i = 0; i < partHitboxes.Length; i++)
            {
                int childIndex = partHitboxes[i];
                if (childIndex < 0 || childIndex >= Main.maxNPCs || !Main.npc[childIndex].active)
                {
                    continue;
                }

                if (immuneTime > 0 && playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    Main.npc[childIndex].immune[playerIndex] = Math.Max(Main.npc[childIndex].immune[playerIndex], immuneTime);
                }

                if (projectile != null && projectile.usesLocalNPCImmunity)
                {
                    int localCooldown = projectile.localNPCImmunity[NPC.whoAmI];
                    if (localCooldown != 0)
                    {
                        projectile.localNPCImmunity[childIndex] = localCooldown;
                    }
                }
            }
        }

        public Vector2 GetPartWorldCenter(int partIndex)
        {
            Part part = Parts[partIndex];
            Vector2 sourceCenter = part.RestPivot + new Vector2(part.Width * 0.5f, part.Height * 0.5f) - part.Origin;
            return ToWorld(bones[partIndex].Map(sourceCenter));
        }

        public override bool? CanFallThroughPlatforms()
        {
            return false;
        }

        public override void AI()
        {
            // Enforce exact body defense every tick so global SuperHardMode defense scaling does not alter it.
            NPC.defense = BodyDefense;
            NPC.realLife = NPC.whoAmI;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (State == DrakeState.Collapse)
            {
                TickCollapse(authority);
                return;
            }

            if (authority)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
                if (!NPC.active)
                {
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    }

                    return;
                }

                if (NPC.HasValidTarget && Vector2.DistanceSquared(NPC.Center, Main.player[NPC.target].Center) > 36000000f)
                {
                    despawnHandler.BeginDespawn(true);
                }

                if (despawnHandler.IsDespawning && NPC.ai[3] == 0f)
                {
                    NPC.ai[3] = 1f;
                    EnterState(DrakeState.Settle);
                }
            }

            if (NPC.ai[3] != 0f || !NPC.HasValidTarget)
            {
                NPC.dontTakeDamage = NPC.ai[3] != 0f;
                NPC.noGravity = false;
                NPC.velocity.X *= 0.9f;
                UpdatePose();
                return;
            }

            if (!initialized)
            {
                if (!authority)
                {
                    UpdatePose();
                    return;
                }

                NPC.ai[2] = -1f;
                if (NPC.HasValidTarget && Main.player[NPC.target].Center.X > NPC.Center.X)
                {
                    NPC.ai[2] = 1f;
                }

                home = NPC.Bottom;
                initialized = true;
                EnterState(DrakeState.Settle);
            }

            NPC.timeLeft = 750;
            NPC.noTileCollide = false;
            NPC.noGravity = Airborne;
            NPC.spriteDirection = Facing;
            NPC.direction = Facing;
            Player target = Main.player[NPC.target];
            bool onGround = NPC.collideY && NPC.velocity.Y >= 0f;
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.AttackTelegraphing = false;
            global.AttackCommitted = State != DrakeState.Ground && State != DrakeState.Recover;

            if (authority)
            {
                flightCooldown = Math.Max(0, flightCooldown - 1);
                rainCooldown = Math.Max(0, rainCooldown - 1);
                tailCooldown = Math.Max(0, tailCooldown - 1);
                leapCooldown = Math.Max(0, leapCooldown - 1);
                if (State != DrakeState.Settle && State != DrakeState.GrabHold && State != DrakeState.GrabToss)
                {
                    TickMeteorRain(target);
                }
                if (onGround && !Airborne)
                {
                    groundedTicks++;
                }

                int hitboxType = ModContent.NPCType<HellkiteDrakeHitbox>();
                for (int slot = 0; slot < partHitboxes.Length; slot++)
                {
                    int currentId = partHitboxes[slot];
                    bool valid = currentId >= 0 && currentId < Main.maxNPCs
                        && Main.npc[currentId].active
                        && Main.npc[currentId].type == hitboxType
                        && (int)Main.npc[currentId].ai[0] == NPC.whoAmI
                        && (int)Main.npc[currentId].ai[1] == slot;
                    if (!valid)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC candidate = Main.npc[i];
                            if (candidate.active && candidate.type == hitboxType
                                && (int)candidate.ai[0] == NPC.whoAmI && (int)candidate.ai[1] == slot)
                            {
                                partHitboxes[slot] = i;
                                valid = true;
                                break;
                            }
                        }
                    }

                    if (!valid)
                    {
                        int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
                            hitboxType, NPC.whoAmI, NPC.whoAmI, slot);
                        if (spawned >= 0 && spawned < Main.maxNPCs)
                        {
                            partHitboxes[slot] = spawned;
                            Main.npc[spawned].realLife = NPC.whoAmI;
                            Main.npc[spawned].netUpdate = true;
                        }
                    }
                }
            }

            previousMouth = Mouth;
            previousTail = TailJoint;
            UpdatePose();
            int previousNearRow = bones[13].FrameRow;
            int previousFarRow = bones[14].FrameRow;
            bool landed = onGround && !wasGrounded && State != DrakeState.Settle;
            bool leapTouchdown = landed && State == DrakeState.LeapBite && StateTimer > LeapTell;
            bool airSlamTouchdown = landed && AirDivingSlam && StateTimer >= AirDiveTell;
            bool substantialLanding = landed && (attackHopPending || leapTouchdown || airborneLandingTicks >= 6);
            bool heavyLanding = leapTouchdown || airborneLandingTicks >= 30;
            if (onGround)
            {
                attackHopPending = false;
                airborneLandingTicks = 0;
            }
            else
            {
                airborneLandingTicks++;
            }
            bool strafeStomp = State == DrakeState.Takeoff && airBreathMode == AirBreathStrafe
                && StateTimer == StrafeStompTick;
            bool turnStep = State == DrakeState.Turn && StateTimer == TurnBodyTick + 24 && onGround;
            wasGrounded = onGround;

            switch (State)
            {
                case DrakeState.Settle:
                    NPC.velocity.X *= 0.8f;
                    if (authority && onGround)
                    {
                        home = NPC.Bottom;
                        EnterState(DrakeState.Ground);
                    }
                    break;

                case DrakeState.Ground:
                    TickGround(target, onGround, authority);
                    break;

                case DrakeState.Turn:
                    NPC.velocity.X = 0f;

                    if (authority && turnBite && StateTimer == TurnBiteAimLockTick)
                    {
                        // Commit while the neck is turning. Its later movement keeps the jaws on this fixed intercept.
                        lockedImpact = PredictBite(target, TurnBiteLiveStart - TurnBiteAimLockTick + 5, 135f);

                        // The torso has yet to flip. Solve the hop from the approaching jaws on the new side.
                        int biteFacing = -turnFacing;
                        float snoutGap = (lockedImpact.X - NPC.Bottom.X) * biteFacing - 365f;
                        biteSpeed = MathHelper.Clamp(snoutGap / BiteLive, -TurnBiteHopBackSpeed, TurnBiteHopForwardSpeed);
                        NPC.netUpdate = true;
                    }

                    // Small hop as the jaws go out, like the ground bite's, so the whole drake commits to the snap.
                    if (turnBite && StateTimer == TurnBiteLiveStart - 2 && onGround)
                    {
                        NPC.velocity.Y = -3.8f;
                        attackHopPending = true;
                    }

                    if (turnBite && StateTimer >= TurnBiteLiveStart && StateTimer < TurnBiteLiveStart + BiteLive)
                    {
                        int hopDirection = Facing * Math.Sign(biteSpeed);
                        Vector2 hopProbe = NPC.Bottom + new Vector2(hopDirection * (NPC.width * 0.5f + 20f), 2f);
                        bool hopSupported = !onGround || Supported(hopProbe) || Supported(hopProbe + Vector2.UnitY * 16f);

                        if (hopSupported && !NPC.collideX)
                        {
                            NPC.velocity.X = Facing * biteSpeed;
                        }
                    }

                    if (authority && turnBite && StateTimer == TurnBiteLiveStart)
                    {
                        SpawnAttack(3, 75);
                    }

                    if (authority && StateTimer == TurnBodyTick)
                    {
                        NPC.ai[2] = -turnFacing;
                        NPC.netUpdate = true;
                    }

                    if (authority && StateTimer >= TurnFinishTick)
                    {
                        if (turnBreath)
                        {
                            lockedAim = AimAt(target.Center - Mouth, 0.55f);
                            EnterState(DrakeState.Breath);
                        }
                        else if (turnBite)
                        {
                            EnterState(DrakeState.Recover);
                        }
                        else
                        {
                            EnterState(DrakeState.Ground);
                        }
                    }
                    break;

                case DrakeState.Bite:
                case DrakeState.GrabBite:
                    bool grabbing = State == DrakeState.GrabBite;
                    int biteTellTicks = BiteWindupTicks;
                    Player biteTarget = target;
                    if (grabbing && GrabTarget >= 0 && GrabTarget < Main.maxPlayers)
                    {
                        biteTarget = Main.player[GrabTarget];
                    }
                    NPC.velocity.X *= 0.78f;
                    if (authority && StateTimer == biteTellTicks - 6 && biteTarget.active && !biteTarget.dead)
                    {
                        PlanJawLunge(biteTarget, 11, 104f);
                        NPC.netUpdate = true;
                    }

                    // Carry the jaws toward the intercept. Back up if the player is running under the snout.
                    int biteDirection = Facing * Math.Sign(biteSpeed);
                    if (StateTimer == biteTellTicks - 2 && onGround)
                    {
                        Vector2 hopProbe = NPC.Bottom + new Vector2(biteDirection * (NPC.width * 0.5f + 48f), 2f);
                        if (Supported(hopProbe) || Supported(hopProbe + Vector2.UnitY * 16f))
                        {
                            NPC.velocity.Y = -3.8f;
                            attackHopPending = true;
                            NPC.velocity.X = Facing * biteSpeed * 0.75f;
                        }
                    }

                    if (StateTimer >= biteTellTicks && StateTimer < biteTellTicks + BiteLive)
                    {
                        Vector2 lungeProbe = NPC.Bottom + new Vector2(biteDirection * (NPC.width * 0.5f + 20f), 2f);
                        bool lungeSupported = !onGround || Supported(lungeProbe) || Supported(lungeProbe + Vector2.UnitY * 16f);
                        if (lungeSupported && !NPC.collideX)
                        {
                            NPC.velocity.X = Facing * biteSpeed;
                        }
                        else
                        {
                            NPC.velocity.X = 0f;
                        }
                    }

                    if (authority && StateTimer == biteTellTicks)
                    {
                        if (grabbing)
                        {
                            SpawnAttack(GrabBiteAttack, GrabBiteDamage);
                        }
                        else
                        {
                            SpawnAttack(0, 75);
                        }
                    }

                    if (authority && StateTimer >= biteTellTicks + BiteLive + BiteHoldTicks + BiteRetractTicks)
                    {
                        if (biteChainStep == 0)
                        {
                            EnterState(DrakeState.Recover);
                        }
                        else if (biteChainStep < BiteChainBites)
                        {
                            EnterState(DrakeState.BiteChain);
                        }
                        else
                        {
                            biteChainStep = 0;
                            EnterState(DrakeState.Recover);
                        }
                    }
                    break;

                case DrakeState.GrabHold:
                    NPC.velocity.X *= 0.72f;
                    if (GrabTarget < 0 || GrabTarget >= Main.maxPlayers
                        || !Main.player[GrabTarget].active || Main.player[GrabTarget].dead)
                    {
                        if (authority)
                        {
                            EnterState(DrakeState.Recover);
                        }
                        break;
                    }

                    Player held = Main.player[GrabTarget];
                    if (StateTimer < GrabHoldDuration)
                    {
                        // Artorias's player pin accepts a center and draw tilt. Clip each step with the player's
                        // full collider so the jaws can swing beside a wall without dragging the victim through it.
                        Vector2 grip = ToWorld(bones[15].Map(GrabMouthSocket));
                        grip.Y = Math.Min(grip.Y, NPC.Bottom.Y - held.height * 0.5f - 2f);
                        Vector2 pull = Vector2.Clamp(grip - held.Center, new Vector2(-26f), new Vector2(26f));
                        Vector2 safePull = Collision.TileCollision(held.position, pull, held.width, held.height, true, true, (int)held.gravDir);
                        tsorcRevampPlayer heldPlayer = held.GetModPlayer<tsorcRevampPlayer>();
                        heldPlayer.ImpaleWorldPosition = held.Center + safePull;
                        heldPlayer.ImpaleDrawRotation = Facing * MathHelper.PiOver2 + bones[15].Rotation * Projection();
                        heldPlayer.ImpaleFreezeTimer = 2;
                    }
                    else if (authority)
                    {
                        EnterState(DrakeState.GrabToss);
                        Vector2 throwVelocity = new Vector2(Facing * GrabThrowSpeed, -8f);
                        // Send the phase first so the victim stops pinning before the launch carrier arrives.
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                        }
                        int throwIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), held.Center, throwVelocity,
                            ModContent.ProjectileType<HellkiteDrakeAttack>(), GrabThrowDamage, 0f, Main.myPlayer,
                            NPC.whoAmI, GrabThrowAttack, held.whoAmI);
                        tsorcGlobalProjectile.SetDefenseTraits(throwIndex, AttackDefenseTraits.BypassesActiveShield);
                    }
                    break;

                case DrakeState.GrabToss:
                    NPC.velocity.X *= 0.72f;
                    if (authority && StateTimer >= GrabTossDuration)
                    {
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.Charge:
                    int runTick = StateTimer - ChargeTell;

                    if (runTick < 0)
                    {
                        NPC.velocity.X *= 0.8f;
                    }

                    if (authority && StateTimer == ChargeLockTick)
                    {
                        // Solve the run 10 ticks before it starts: far enough to carry the body ChargeOvershoot past
                        // the target, at ChargeAverageSpeed. biteSpeed holds the average speed for the pose and clients.
                        float chargeGap = (target.Center.X - NPC.Center.X) * Facing;
                        float chargeTravel = MathHelper.Clamp(chargeGap + ChargeOvershoot, 300f, 1300f);
                        chargeTicks = (int)(chargeTravel / ChargeAverageSpeed);
                        biteSpeed = chargeTravel / chargeTicks;
                        NPC.netUpdate = true;
                    }

                    if (runTick >= 0 && runTick < chargeTicks)
                    {
                        // Sine speed curve: slow off the mark, fastest mid-run, slowing into the skid. Its area is 1,
                        // so the run covers the solved distance exactly when nothing stops it.
                        float runProgress = runTick / (float)chargeTicks;
                        float runCurve = MathF.Sin(MathHelper.Pi * runProgress) * MathHelper.PiOver2;
                        float runSpeed = Math.Max(2f, biteSpeed * runCurve);
                        Vector2 runProbe = NPC.Bottom + new Vector2(Facing * (NPC.width * 0.5f + 24f), 2f);
                        bool runSupported = !onGround || Supported(runProbe) || Supported(runProbe + Vector2.UnitY * 16f);

                        if (runSupported && !NPC.collideX)
                        {
                            NPC.velocity.X = Facing * runSpeed;
                        }
                        else if (authority && runTick > 2)
                        {
                            // Wall or ledge: end the run here and go straight to the skid.
                            chargeTicks = runTick;
                            NPC.netUpdate = true;
                        }
                    }
                    else if (runTick >= chargeTicks)
                    {
                        NPC.velocity.X *= 0.82f;
                    }

                    if (authority && runTick == 0)
                    {
                        SpawnAttack(ChargeAttack, 95);
                    }

                    if (authority && runTick >= chargeTicks + ChargeSkidTicks)
                    {
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.LeapBite:
                    int leapTick = StateTimer - LeapTell;

                    if (leapTick < 0)
                    {
                        NPC.velocity.X *= 0.8f;
                    }

                    if (authority && StateTimer == LeapLockTick)
                    {
                        if (!TryPlanLeap(target, out Vector2 leapLaunch, out leapAirTicks))
                        {
                            leapCooldown = LeapLandingCooldown;
                            EnterState(DrakeState.Recover);
                            break;
                        }
                        biteSpeed = leapLaunch.X;
                        leapLaunchY = leapLaunch.Y;
                        biteReach = LeapBiteReach;
                        lockedAim = new Vector2(Facing * MathF.Cos(0.65f), MathF.Sin(0.65f));
                        NPC.netUpdate = true;
                    }

                    if (leapTick >= 0 && leapLandTick < 0)
                    {
                        // Apply our own gravity so the committed trajectory matches the route test on every peer.
                        NPC.noGravity = true;
                        if (leapTick == 0)
                        {
                            if (authority && !onGround)
                            {
                                leapCooldown = LeapLandingCooldown;
                                EnterState(DrakeState.Recover);
                                break;
                            }
                            NPC.velocity.Y = leapLaunchY;
                            attackHopPending = true;
                            if (authority)
                            {
                                NPC.netUpdate = true;
                            }
                        }
                        else
                        {
                            NPC.velocity.Y += LeapGravity;
                        }
                        NPC.velocity.X = Facing * biteSpeed;
                    }
                    else if (leapLandTick >= 0)
                    {
                        NPC.velocity.X *= 0.7f;
                    }

                    if (authority && leapLandTick < 0 && leapTick == leapAirTicks - LeapBiteLiveLead)
                    {
                        SpawnAttack(4, 85);
                    }

                    // Touchdown: floor under the feet, a wall in the way, or 40 ticks overdue (it hopped off a ledge).
                    bool leapOverdue = leapTick > leapAirTicks + 40;

                    if (authority && leapLandTick < 0 && leapTick > 6 && (onGround || NPC.collideX || leapOverdue))
                    {
                        leapLandTick = StateTimer;
                        leapCooldown = LeapLandingCooldown;
                        groundedTicks = 0;
                        NPC.netUpdate = true;
                    }

                    if (authority && leapLandTick >= 0 && StateTimer >= leapLandTick + BiteHoldTicks + BiteRetractTicks)
                    {
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.BiteChain:
                    // Walk in with the jaw parted, stopping once the mouth is about four tiles off the target.
                    float chainGap = (PredictBite(target, 16, 140f).X - Mouth.X) * Facing;
                    float chainSpeed = 0f;

                    if (chainGap > 70f)
                    {
                        chainSpeed = Facing * BiteChainWalkSpeed;
                    }

                    Vector2 chainProbe = NPC.Bottom + new Vector2(Facing * (NPC.width / 2f + 12f), 2f);
                    bool chainSupport = Supported(chainProbe) || Supported(chainProbe + Vector2.UnitY * 12f);

                    if (!chainSupport || NPC.collideX)
                    {
                        chainSpeed = 0f;
                    }

                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, chainSpeed, 0.2f);

                    if (authority && (target.Center.X - NPC.Center.X) * Facing < -80f)
                    {
                        // The target got behind the body: drop the chain so the ground state can answer with a turn.
                        biteChainStep = 0;
                        EnterState(DrakeState.Ground);
                    }
                    else if (authority && StateTimer >= BiteChainAdvanceTicks)
                    {
                        biteChainStep++;
                        PlanJawLunge(target, 10, 92f);
                        biteWindupAim = lockedAim;
                        EnterState(DrakeState.Bite);
                    }
                    break;

                case DrakeState.Breath:
                    NPC.velocity.X *= 0.75f;
                    if (authority && StateTimer == BreathTell - 12)
                    {
                        lockedAim = AimAt(target.Center - ToWorld(BreathMouthSocket), 0.55f);
                        NPC.netUpdate = true;
                    }

                    if (authority && StateTimer >= BreathTell && StateTimer < BreathTell + BreathLive
                        && (StateTimer - BreathTell) % BreathShotInterval == 0)
                    {
                        SpawnBreath(70);
                    }

                    if (authority && StateTimer >= BreathTell + BreathLive + BreathCloseTicks)
                    {
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.DestinedDeathRain:
                    NPC.velocity.X *= 0.75f;
                    if (authority && StateTimer == RainAimLock)
                    {
                        Vector2 overhead = Mouth - Vector2.UnitY * 224f;
                        if (!Collision.CanHitLine(Mouth, 1, 1, overhead, 1, 1))
                        {
                            rainCooldown = RainCooldownTicks;
                            EnterState(DrakeState.Recover);
                            break;
                        }

                        // Freeze the footprint before releasing any shots so every peer sees the same field.
                        float fieldMargin = RainFieldWidth * 0.5f + 48f;
                        float fieldCenter = MathHelper.Clamp(target.Center.X, fieldMargin, Main.maxTilesX * 16f - fieldMargin);
                        float floor = MathHelper.Clamp(target.Bottom.Y, NPC.Bottom.Y - 80f, NPC.Bottom.Y + 112f);
                        lockedImpact = new Vector2(fieldCenter, floor);
                        NPC.netUpdate = true;
                    }

                    int volleyTick = StateTimer - RainTell;
                    if (authority && volleyTick >= 0 && StateTimer <= RainLastWave && volleyTick % RainWaveInterval == 0)
                    {
                        int waveIndex = volleyTick / RainWaveInterval;
                        int laneCount = RainWaves * RainFlamesPerWave;
                        for (int i = 0; i < RainFlamesPerWave; i++)
                        {
                            int lane = waveIndex + i * RainWaves;
                            float fraction = lane / (float)(laneCount - 1);
                            float laneOffset = MathHelper.Lerp(-RainFieldWidth * 0.5f, RainFieldWidth * 0.5f, fraction);
                            Vector2 landing = lockedImpact + new Vector2(laneOffset, -HellkiteDrakeDeathFlame.HitHeight * 0.5f);
                            int flightTicks = 64 + waveIndex * 4;
                            Vector2 velocity = HellkiteDrakeDeathFlame.LaunchVelocity(Mouth, landing, flightTicks);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, velocity,
                                ModContent.ProjectileType<HellkiteDrakeDeathFlame>(), RainImpactDamage, 1.5f, Main.myPlayer,
                                ai2: lane);
                        }
                    }

                    if (authority && StateTimer >= RainLastWave + RainFollowThrough)
                    {
                        rainCooldown = RainCooldownTicks;
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.TailSlam:
                    NPC.velocity.X *= 0.7f;
                    if (authority && StateTimer == TailTell)
                    {
                        SpawnAttack(2, 90);
                    }

                    if (authority && StateTimer >= TailTell + TailLive + 18)
                    {
                        tailCooldown = 150;
                        EnterState(DrakeState.Recover);
                    }
                    break;

                case DrakeState.Takeoff:
                case DrakeState.AirApproach:
                case DrakeState.AirBreath:
                case DrakeState.RetreatBreath:
                case DrakeState.Land:
                case DrakeState.AirTurn:
                case DrakeState.AirDive:
                    TickAir(target, onGround, authority);
                    break;

                case DrakeState.Recover:
                    NPC.velocity.X *= 0.8f;
                    if (authority && StateTimer >= RecoveryTicks)
                    {
                        EnterState(DrakeState.Ground);
                        UpdatePose();
                        TickGround(target, onGround, authority);
                    }
                    break;
            }

            if (!Airborne && State != DrakeState.Turn && State != DrakeState.LeapBite && Math.Abs(NPC.velocity.X) > 0.1f)
            {
                // One clock unit per 1.7px walked. The charge caps the stride at 3 units a tick so the legs sprint
                // without blurring; backing up runs the cycle in reverse so the feet don't moonwalk.
                float stride = Math.Abs(NPC.velocity.X) / 1.7f;

                if (State == DrakeState.Charge)
                {
                    stride = Math.Min(stride, 3f);
                }

                if (Math.Sign(NPC.velocity.X) != Facing)
                {
                    stride = -stride;
                }

                walkClock += stride + 62f;
                walkClock %= 62f;
            }

            UpdatePose();
            bool nearFootfall = landed || strafeStomp || turnStep;
            bool farFootfall = landed;
            if (onGround && feetWereGrounded && !Airborne && State != DrakeState.Turn
                && State != DrakeState.LeapBite && Math.Abs(NPC.velocity.X) > 0.1f)
            {
                // Row 0 is forward touchdown. Reverse walking plants when the lifted foot returns to row 3.
                int plantRow = Math.Sign(NPC.velocity.X) == Facing ? 0 : 3;
                nearFootfall |= bones[13].FrameRow == plantRow && previousNearRow != plantRow;
                farFootfall |= bones[14].FrameRow == plantRow && previousFarRow != plantRow;
            }

            feetWereGrounded = onGround && NPC.velocity.Y >= 0f;
            bool chargeRunning = State == DrakeState.Charge && StateTimer >= ChargeTell && StateTimer < ChargeTell + chargeTicks;
            if (nearFootfall || farFootfall)
            {
                if (authority && !chargeRunning)
                {
                    int footDamage = airSlamTouchdown ? AirSlamDamage : FootfallDamage;
                    if (nearFootfall)
                    {
                        SpawnAttack(NearFootAttack, footDamage);
                    }
                    if (farFootfall)
                    {
                        SpawnAttack(FarFootAttack, footDamage);
                    }
                }

                if (!Main.dedServ && !strafeStomp && !leapTouchdown && !airSlamTouchdown)
                {
                    Vector2 contact = GetClawPosition(nearFootfall ? 13 : 14);
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.5f, Pitch = -0.1f }, contact);
                    if (!substantialLanding)
                    {
                        UsefulFunctions.ScreenShake(contact, 0.25f, 5, 6f, 280f);
                    }
                }
            }

            for (int slot = 0; slot < partHitboxes.Length; slot++)
            {
                int childIndex = partHitboxes[slot];
                if (childIndex >= 0 && childIndex < Main.maxNPCs && Main.npc[childIndex].active
                    && Main.npc[childIndex].ModNPC is HellkiteDrakeHitbox hitbox)
                {
                    hitbox.SyncToDrake(this, slot);
                }
            }

            if (!Main.dedServ)
            {
                bool syncedSlam = AirDivingSlam && airTouchdown >= 0 && StateTimer - airTouchdown < 15;
                if ((airSlamTouchdown || syncedSlam) && airSlamSoundSequence != sequence)
                {
                    airSlamSoundSequence = sequence;
                    Vector2 impact = GetClawPosition(13);
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 1.2f, Pitch = -0.32f }, impact);
                    UsefulFunctions.ScreenShake(impact, 7f, 13, distanceFalloff: 650f);
                    EmitClawGrit(13, 15);
                    EmitClawGrit(14, 12);
                }

                // Actual ground contact covers bite hops and flight landings even after an attack changes state.
                if (substantialLanding && !airSlamTouchdown)
                {
                    float strength = heavyLanding ? 4.5f : 2.5f;
                    int frames = heavyLanding ? 12 : 9;
                    UsefulFunctions.ScreenShake(NPC.Bottom, strength, frames, distanceFalloff: 540f);
                    if (leapTouchdown)
                    {
                        EmitClawGrit(13, 20);
                        EmitClawGrit(14, 18);
                    }
                    else
                    {
                        EmitClawGrit(13, heavyLanding ? 9 : 5);
                        EmitClawGrit(14, heavyLanding ? 7 : 4);
                    }
                }

                TickPresentation(leapTouchdown);
            }

            NPC.ai[1]++;
        }

        private void TickCollapse(bool authority)
        {
            NPC.timeLeft = 750;
            NPC.dontTakeDamage = true;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            bool onFloor = NPC.collideY && NPC.velocity.Y >= 0f;
            if (authority && collapseTouchdown < 0 && (onFloor || StateTimer >= 240))
            {
                collapseTouchdown = StateTimer;
                NPC.netUpdate = true;
            }
            NPC.behindTiles = collapseTouchdown >= 0;

            if (collapseTouchdown < 0)
            {
                NPC.velocity.X *= 0.93f;
                NPC.velocity.Y = Math.Clamp(NPC.velocity.Y + 0.82f, -3f, 18f);
            }
            else
            {
                NPC.velocity.X = 0f;
                NPC.velocity.Y = 2f;
            }

            if (collapseTouchdown >= 0)
            {
                int groundedTime = StateTimer - collapseTouchdown;
                if (authority && groundedTime >= CollapseRestTicks)
                {
                    // Finish through the normal boss kill path. Loot and progression occur once, after the collapse.
                    NPC.life = 0;
                    NPC.checkDead();
                    return;
                }

                int plumeTime = groundedTime - CollapseImpactTick;
                if (!Main.dedServ && plumeTime >= 0 && plumeTime < CollapsePlumeTicks)
                {
                    if (collapseImpactSequence != sequence)
                    {
                        collapseImpactSequence = sequence;
                        UsefulFunctions.ScreenShake(NPC.Bottom, 12f, 24, distanceFalloff: 900f);
                    }

                    // The leading edge tops out eight tiles above each column's local floor. Dense grit stays low.
                    float progress = plumeTime / (float)(CollapsePlumeTicks - 1);
                    float height = CollapsePlumeHeight * MathF.Sin(progress * MathHelper.PiOver2);
                    for (int i = 0; i < 14; i++)
                    {
                        float offset = Main.rand.NextFloat(-230f, 230f);
                        float worldX = NPC.Bottom.X + offset;
                        if (!TryGroundSurface(worldX, NPC.Bottom.Y - 30f, NPC.Bottom.Y + 38f, out float floorY))
                        {
                            continue;
                        }

                        float fraction = i == 0 ? 1f : Main.rand.NextFloat(0.18f, 1f);
                        float rise = height * fraction;
                        Vector2 origin = new Vector2(worldX, floorY - rise - 2f);
                        if (Collision.SolidCollision(origin - Vector2.One, 2, 2))
                        {
                            continue;
                        }

                        float lift = 1f + 5f * (1f - progress);
                        Vector2 drift = new Vector2(offset * Main.rand.NextFloat(0.016f, 0.035f), -lift);
                        Dust dust = Dust.NewDustPerfect(origin, DustID.Dirt, drift, 30, default, Main.rand.NextFloat(1f, 1.9f));
                        dust.noGravity = i % 3 == 0;
                    }
                }
            }

            NPC.ai[1]++;
        }

        private void TickMeteorRain(Player target)
        {
            meteorTimer--;
            if (meteorTimer > 0)
            {
                return;
            }

            bool intensified = NPC.life * 2 < NPC.lifeMax;
            if (meteorsScattered == 0 && meteorsDirected == 0)
            {
                meteorsScattered = 1;
                directedMeteorIndex = 0;
                if (intensified)
                {
                    meteorsScattered = Main.rand.Next(2, 4);
                    meteorsDirected = Main.rand.Next(3, 7);
                }
            }

            bool directed = meteorsDirected > 0 && (meteorsScattered == 0 || Main.rand.NextBool());
            int laneIndex = -1;
            if (directed)
            {
                laneIndex = directedMeteorIndex;
                directedMeteorIndex++;
                meteorsDirected--;
            }
            else
            {
                meteorsScattered--;
            }

            int flightTicks = Main.rand.Next(38, 55);
            float targetX = RollMeteorLane(target.Center.X, target.velocity.X, flightTicks, intensified, laneIndex, Main.rand);
            float worldRight = Main.maxTilesX * 16f - 112f;
            Vector2 aim = new Vector2(Math.Clamp(targetX, 112f, worldRight), target.Center.Y + 6f);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                float spawnX = Math.Clamp(aim.X + Main.rand.NextFloat(-235f, 235f), 112f, worldRight);
                float spawnY = Math.Max(128f, aim.Y - Main.rand.NextFloat(410f, 610f));
                Vector2 origin = new Vector2(spawnX, spawnY);
                Vector2 aboveArena = Vector2.Lerp(origin, aim, 0.72f);
                if (aim.Y - spawnY < 215f || Collision.SolidCollision(origin - new Vector2(30f, 31f), 60, 62)
                    || !Collision.CanHitLine(origin, 12, 12, aboveArena, 12, 12))
                {
                    continue;
                }

                Vector2 velocity = (aim - origin) / flightTicks;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity, ModContent.ProjectileType<DragonMeteor>(),
                    MeteorDamage, 2f, Main.myPlayer, ai2: NPC.whoAmI + 1);
                break;
            }

            if (meteorsScattered > 0 || meteorsDirected > 0)
            {
                meteorTimer = Main.rand.Next(7, 12);
            }
            else if (intensified)
            {
                meteorTimer = Main.rand.Next(1, 5) * (2 * 60);
            }
            else
            {
                meteorTimer = Main.rand.Next(60, 91);
            }
        }

        // A volley alternates between the sprint intercept, space ahead, and space the player just left.
        internal static float RollMeteorLane(float playerX, float velocity, int flightTicks, bool intensified, int laneIndex,
            Terraria.Utilities.UnifiedRandom random)
        {
            if (laneIndex < 0)
            {
                if (intensified)
                {
                    int scatterSide = random.NextBool() ? -1 : 1;
                    return playerX + scatterSide * random.NextFloat(340f, 900f);
                }

                float width = random.NextBool(4) ? 34f : 440f;
                return playerX + random.NextFloat(-width, width);
            }

            float intercept = PredictX(playerX, velocity, flightTicks, 425f);
            int travelSide = Math.Sign(velocity);
            if (Math.Abs(velocity) < 1.25f)
            {
                travelSide = random.NextBool() ? -1 : 1;
            }

            switch (laneIndex % 3)
            {
                case 0:
                    return intercept + random.NextFloat(-64f, 64f);
                case 1:
                    return intercept + travelSide * random.NextFloat(70f, 190f);
                default:
                    return playerX - travelSide * random.NextFloat(85f, 245f);
            }
        }

        private void TickGround(Player target, bool onGround, bool authority)
        {
            float forward = (target.Center.X - Mouth.X) * Facing;
            float interceptX = PredictX(target.Center.X, target.velocity.X, 24, 220f);
            float predictedForward = (interceptX - Mouth.X) * Facing;
            float leadFromTarget = predictedForward - forward;
            float behind = (target.Center.X - TailBase.X) * -Facing;
            Player rearTarget = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                {
                    continue;
                }

                float rearDistance = (candidate.Center.X - TailBase.X) * -Facing;
                if (rearDistance >= 65f && rearDistance <= 260f
                    && Math.Abs(candidate.Bottom.Y - NPC.Bottom.Y) < 110f)
                {
                    rearTarget = candidate;
                    break;
                }
            }

            float desiredSpeed = 0f;
            if (predictedForward > 34f)
            {
                desiredSpeed = Facing * Math.Min(PursuitWalkSpeed, (predictedForward - 22f) * 0.08f);
            }
            else if (forward < -60f && behind < 50f)
            {
                // Clear space for the jaws when the target gets under the neck.
                desiredSpeed = -Facing * 2.5f;
            }

            Vector2 probe = NPC.Bottom + new Vector2(Math.Sign(desiredSpeed) * (NPC.width / 2f + 12f), 2f);
            bool support = Supported(probe) || Supported(probe + Vector2.UnitY * 12f);
            if (!support || NPC.collideX)
            {
                desiredSpeed = 0f;
            }

            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredSpeed, 0.12f);
            if (!authority || !onGround)
            {
                return;
            }

            bool outOfRange = Vector2.DistanceSquared(target.Center, NPC.Center) > FarFlightRange * FarFlightRange;
            bool tryFlight = outOfRange && (StateTimer <= 1 || StateTimer % 24 == 0);
            float crossoverDistance = (target.Center.X - NPC.Center.X) * -Facing;
            if (crossoverDistance > TurnThreshold)
            {
                if (tryFlight && StartFlight(target))
                {
                    return;
                }
                turnFacing = Facing;
                NPC.velocity.X = 0f;
                biteChainStep = 0;

                // Give the crossover priority over the normal attack bag. Flame covers farther behind
                // than the bite, measured from the snout after it swings to the new side.
                Vector2 newMouthOffset = BreathMouthSocket - SourceFloor;
                newMouthOffset.X *= Facing;
                Vector2 turnedMouth = NPC.Bottom + newMouthOffset * ArtScale;
                Vector2 toTarget = target.Center - turnedMouth;
                turnBite = crossoverDistance <= TurnBiteMaxRange && Math.Abs(target.Center.Y - NPC.Center.Y) < 175f;
                turnBreath = !turnBite && toTarget.X * -Facing > 0f && toTarget.LengthSquared() <= 420f * 420f
                    && Collision.CanHitLine(turnedMouth, 1, 1, target.Center, 1, 1);
                EnterState(DrakeState.Turn);
                return;
            }

            // Try immediately after recovery, then space out retries while walking into range.
            if (StateTimer > 1 && StateTimer % 6 != 0)
            {
                return;
            }

            Vector2 leapLaunch = Vector2.Zero;
            int leapDuration = 0;
            bool canLeap = leapCooldown == 0 && predictedForward >= LeapMinRange && forward <= LeapMaxRange
                && TryPlanLeap(target, out leapLaunch, out leapDuration);

            // Favor a clear leap over taking flight on open, level ground, even beyond the old flight threshold.
            if (canLeap && predictedForward >= PriorityLeapRange)
            {
                leapLandTick = -1;
                leapAirTicks = leapDuration;
                biteSpeed = leapLaunch.X;
                leapLaunchY = leapLaunch.Y;
                biteReach = LeapBiteReach;
                EnterState(DrakeState.LeapBite);
                return;
            }

            if (tryFlight && StartFlight(target))
            {
                return;
            }

            attackBag ??= new AttackBag<DrakeState>(cards =>
            {
                cards.Add(DrakeState.Bite);
                cards.Add(DrakeState.GrabBite);
                cards.Add(DrakeState.BiteChain);
                cards.Add(DrakeState.Breath);
                cards.Add(DrakeState.DestinedDeathRain);
                cards.Add(DrakeState.TailSlam);
                cards.Add(DrakeState.Takeoff);
                cards.Add(DrakeState.LeapBite);
                cards.Add(DrakeState.LeapBite);
                cards.Add(DrakeState.LeapBite);
                cards.Add(DrakeState.RetreatBreath);
                cards.Add(DrakeState.Charge);
            });

            bool Eligible(DrakeState move)
            {
                if (move == DrakeState.Bite)
                {
                    return forward >= -15f && forward <= 295f && predictedForward >= -42f && predictedForward <= 250f
                        && Math.Abs(target.Center.Y - Mouth.Y) < 175f
                        && Collision.CanHitLine(Mouth, 1, 1, target.Center, 1, 1);
                }

                if (move == DrakeState.GrabBite)
                {
                    return forward >= 12f && forward <= GrabMaxRange && predictedForward >= -14f && predictedForward <= GrabMaxRange + 25f
                        && Math.Abs(target.Center.Y - Mouth.Y) < 112f
                        && Collision.CanHitLine(Mouth, 1, 1, target.Center, 1, 1);
                }

                if (move == DrakeState.BiteChain)
                {
                    // Wider than the single bite's 220px: the chain walks its own way in.
                    return forward >= -15f && forward <= 420f && Math.Abs(target.Center.Y - Mouth.Y) < 175f
                        && Collision.CanHitLine(Mouth, 1, 1, target.Center, 1, 1);
                }

                if (move == DrakeState.Breath)
                {
                    Vector2 mappedMouth = bones[15].Map(BreathMouthSocket);
                    Vector2 breathOrigin = ToWorld(mappedMouth);
                    float breathForward = (target.Center.X - breathOrigin.X) * Facing;
                    return breathForward >= 90f && breathForward <= 420f
                        && Vector2.DistanceSquared(target.Center, breathOrigin) <= 420f * 420f
                        && Collision.CanHitLine(breathOrigin, 1, 1, target.Center, 1, 1);
                }

                if (move == DrakeState.DestinedDeathRain)
                {
                    Vector2 overhead = Mouth - Vector2.UnitY * 320f;
                    return rainCooldown == 0 && forward >= -200f && forward < PriorityLeapRange
                        && Math.Abs(target.Bottom.Y - NPC.Bottom.Y) < 160f
                        && Collision.CanHitLine(Mouth, 1, 1, overhead, 1, 1);
                }

                if (move == DrakeState.TailSlam)
                {
                    return rearTarget != null && tailCooldown == 0;
                }

                if (move == DrakeState.LeapBite)
                {
                    return canLeap;
                }

                if (move == DrakeState.Charge)
                {
                    // Target 150-700px ahead of the snout (the head is the ram, so they must start in front of it) on
                    // roughly level ground, with a clear, supported lane at least as far as the target. The run
                    // itself stops at any ledge or wall beyond that.
                    float bodyGap = (target.Center.X - NPC.Center.X) * Facing;
                    bool inChargeRange = forward >= ChargeMinRange && forward <= ChargeMaxRange;
                    bool chargeLevel = Math.Abs(target.Bottom.Y - NPC.Bottom.Y) < 120f;

                    if (!inChargeRange || !chargeLevel)
                    {
                        return false;
                    }

                    Vector2 chargeEnd = NPC.Bottom + new Vector2(Facing * bodyGap, 0f);
                    Vector2 chargeMiddle = NPC.Bottom + new Vector2(Facing * bodyGap * 0.5f, 0f);
                    bool laneSupported = (Supported(chargeEnd + Vector2.UnitY * 2f) || Supported(chargeEnd + Vector2.UnitY * 18f))
                        && (Supported(chargeMiddle + Vector2.UnitY * 2f) || Supported(chargeMiddle + Vector2.UnitY * 18f));
                    Vector2 chargeLift = new Vector2(0f, -24f);
                    return laneSupported && ClearRoute(NPC.Bottom + chargeLift, chargeEnd + chargeLift);
                }

                if (move == DrakeState.RetreatBreath)
                {
                    // Target hugging the front: anywhere from under the chest (340px behind the mouth) to 200px past
                    // the mouth, but not behind the body. Shares the flight cooldown, and needs the whole climb clear.
                    bool huggingFront = forward >= -340f && forward <= 200f
                        && (target.Center.X - NPC.Center.X) * Facing > -40f;

                    if (!huggingFront || flightCooldown > 0 || groundedTicks < GroundMinimum)
                    {
                        return false;
                    }

                    Vector2 retreatEnd = NPC.Bottom + new Vector2(-Facing * RetreatBack, -RetreatUp);
                    return ClearRoute(NPC.Bottom - Vector2.UnitY * 8f, retreatEnd, true);
                }

                bool shouldWalkIn = forward > 150f && support && !NPC.collideX
                    && Math.Abs(target.Bottom.Y - NPC.Bottom.Y) < 100f;
                if (shouldWalkIn && forward <= LeapMaxRange)
                {
                    return false;
                }
                return flightCooldown == 0 && groundedTicks >= GroundMinimum && behind < 65f;
            }

            int Priority(DrakeState move)
            {
                if (leadFromTarget >= 55f)
                {
                    if (move == DrakeState.LeapBite || move == DrakeState.Charge)
                    {
                        return 2;
                    }
                    if (move == DrakeState.BiteChain || move == DrakeState.Breath)
                    {
                        return 1;
                    }
                }
                else if (leadFromTarget <= -55f && predictedForward < 170f)
                {
                    if (move == DrakeState.Bite || move == DrakeState.GrabBite || move == DrakeState.RetreatBreath)
                    {
                        return 1;
                    }
                }

                return 0;
            }

            if (attackBag.TryDraw(Eligible, out DrakeState chosen, Priority))
            {
                biteChainStep = 0;

                if (chosen == DrakeState.TailSlam)
                {
                    if (!FindLanding(rearTarget.Center.X, NPC.Bottom.Y - 80f, out Vector2 landing)
                        || !Collision.CanHitLine(TailBase, 1, 1, landing - Vector2.UnitY * 8f, 1, 1))
                    {
                        EnterState(DrakeState.Recover);
                        return;
                    }

                    lockedImpact = landing - Vector2.UnitY * 6f;
                }

                if (chosen == DrakeState.Takeoff)
                {
                    if (!StartFlight(target))
                    {
                        flightCooldown = 180;
                    }
                    return;
                }

                if (chosen == DrakeState.LeapBite)
                {
                    leapLandTick = -1;
                    leapAirTicks = leapDuration;
                    biteSpeed = leapLaunch.X;
                    leapLaunchY = leapLaunch.Y;
                    biteReach = LeapBiteReach;
                }

                if (chosen == DrakeState.RetreatBreath)
                {
                    groundedTicks = 0;
                    blockedTicks = 0;
                }

                if (chosen == DrakeState.Bite || chosen == DrakeState.GrabBite)
                {
                    PlanJawLunge(target, 12, 112f);
                    biteWindupAim = lockedAim;
                }
                else
                {
                    lockedAim = AimAt(target.Center - ToWorld(BreathMouthSocket), 0.55f);
                }

                if (chosen == DrakeState.GrabBite)
                {
                    GrabTarget = target.whoAmI;
                }

                EnterState(chosen);
            }
        }

        private bool StartFlight(Player target)
        {
            Vector2 launch = NPC.Bottom;
            Vector2 climb = NPC.Bottom - Vector2.UnitY * FlightClimb;
            if (!ClearRoute(launch, climb, true))
            {
                climb = NPC.Bottom - Vector2.UnitY * 248f;
                if (!ClearRoute(launch, climb, true))
                {
                    return false;
                }
            }

            waypoint = climb;
            airAttackBag ??= new AttackBag<byte>(cards =>
            {
                cards.Add(AirBreathHover);
                cards.Add(AirBreathStrafe);
                cards.Add(AirClawSlam);
                cards.Add(AirLandingBite);
            });
            bool hasFloor = FindLanding(target.Center.X, target.Bottom.Y - 64f, out Vector2 floor);
            if (!hasFloor || floor.Y - target.Bottom.Y > SkyTargetHeight)
            {
                airBreathMode = AirBreathSkyward;
            }
            else
            {
                float flightSide = Math.Sign(target.Center.X - NPC.Center.X);
                float relativeMotion = target.velocity.X * flightSide;
                int AirPriority(byte move)
                {
                    if (relativeMotion > 3.5f && move == AirBreathStrafe)
                    {
                        return 1;
                    }
                    if (relativeMotion < -3.5f && (move == AirClawSlam || move == AirLandingBite))
                    {
                        return 1;
                    }
                    return 0;
                }

                if (!airAttackBag.TryDraw(_ => true, out airBreathMode, AirPriority))
                {
                    airBreathMode = AirBreathHover;
                }
            }

            groundedTicks = 0;
            blockedTicks = 0;
            EnterState(DrakeState.Takeoff);
            return true;
        }

        private void TickAir(Player target, bool onGround, bool authority)
        {
            bool checkingFlightEnvelope = State == DrakeState.Takeoff || State == DrakeState.AirApproach
                || State == DrakeState.AirBreath && airBreathMode == AirBreathStrafe;
            if (authority && checkingFlightEnvelope && !onGround && StateTimer > 12
                && !AirSpaceClear(NPC.Bottom + NPC.velocity * 2f))
            {
                NPC.velocity *= 0.2f;
                StartLanding();
                return;
            }

            if (authority)
            {
                blockedTicks = NPC.collideX || NPC.collideY ? blockedTicks + 1 : 0;
            }

            if (State == DrakeState.Takeoff)
            {
                if (StateTimer < TakeoffCrouchTicks)
                {
                    NPC.velocity *= 0.6f;
                    return;
                }

                Vector2 climb = waypoint - NPC.Bottom;
                NPC.velocity = Vector2.Lerp(NPC.velocity, climb.SafeNormalize(-Vector2.UnitY) * 9f, 0.19f);
                if (authority && (blockedTicks > 8 || StateTimer > TakeoffCrouchTicks + 73))
                {
                    StartLanding();
                    return;
                }

                if (authority && climb.LengthSquared() < 576f)
                {
                    EnterState(DrakeState.AirApproach);
                }
            }
            else if (State == DrakeState.AirApproach)
            {
                if (authority && (StateTimer <= 1 || StateTimer % 12 == 0))
                {
                    if ((target.Center.X - NPC.Center.X) * Facing < -140f)
                    {
                        airTurnToPursue = true;
                        EnterState(DrakeState.AirTurn);
                        return;
                    }

                    bool diving = airBreathMode == AirClawSlam || airBreathMode == AirLandingBite;
                    if (diving)
                    {
                        float landingOffset = -Facing * 430f;
                        if (airBreathMode == AirClawSlam)
                        {
                            landingOffset = Facing * NearClawPlantX;
                        }

                        float diveInterceptX = PredictX(target.Center.X, target.velocity.X, AirDiveTell + 18, 340f);
                        float landingX = diveInterceptX + landingOffset;
                        bool hasFloor = FindLanding(landingX, target.Bottom.Y - 64f, out lockedImpact);
                        float heightGap = lockedImpact.Y - target.Bottom.Y;
                        if (hasFloor && Math.Abs(heightGap) < 96f && Math.Abs(landingX - lockedImpact.X) <= 40f)
                        {
                            float lead = airBreathMode == AirLandingBite ? 84f : 18f;
                            waypoint = lockedImpact - new Vector2(Facing * lead, 300f);
                        }
                        else
                        {
                            airBreathMode = AirBreathHover;
                            diving = false;
                        }
                    }

                    if (!diving)
                    {
                        float standOff = 690f;
                        float altitude = target.Bottom.Y - FlightClimb;
                        if (airBreathMode == AirBreathStrafe)
                        {
                            standOff = 835f;
                        }
                        else if (airBreathMode == AirBreathSkyward)
                        {
                            altitude = target.Center.Y + 70f;
                        }
                        float flightInterceptX = PredictX(target.Center.X, target.velocity.X, 24, 290f);
                        waypoint = new Vector2(flightInterceptX - Facing * standOff, altitude);
                    }
                    NPC.netUpdate = true;
                }

                Vector2 difference = waypoint - NPC.Bottom;
                float remaining = difference.Length();
                float engageRange = 58f + Math.Min(62f, Math.Abs(target.velocity.X) * 6f);
                float speed = Math.Min(FlightCruiseSpeed, remaining * 0.12f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, difference.SafeNormalize(Vector2.Zero) * speed, 0.18f);
                if (authority && (blockedTicks > 10 || StateTimer > 330))
                {
                    StartLanding();
                }
                else if (authority && remaining < engageRange)
                {
                    if (airBreathMode == AirClawSlam || airBreathMode == AirLandingBite)
                    {
                        airBiteStart = -1;
                        airTouchdown = -1;
                        Vector2 restMouth = lockedImpact + new Vector2(Facing * 398f, -102f);
                        Vector2 plannedBite = new Vector2(lockedImpact.X + Facing * 430f, target.Center.Y);
                        lockedAim = AimAt(plannedBite - restMouth, 0.6f);
                        EnterState(DrakeState.AirDive);
                    }
                    else
                    {
                        lockedAim = AimAt(target.Center - Mouth, AirAimDownLimit);
                        if (airBreathMode == AirBreathStrafe)
                        {
                            lockedImpact = new Vector2(target.Center.X + Facing * AirStrafeOvershoot, waypoint.Y);
                        }
                        EnterState(DrakeState.AirBreath);
                    }
                }
            }
            else if (State == DrakeState.AirBreath)
            {
                int liveTicks = AirLiveTicks;
                int liveTick = StateTimer - AirTell;
                bool breathing = liveTick >= 0 && liveTick < liveTicks;

                bool strafing = airBreathMode == AirBreathStrafe;
                Vector2 aimPoint = target.Center;
                if (strafing)
                {
                    // Track the player on the way in, then carry the sweep forward as the mouth crosses them.
                    float ahead = (target.Center.X - Mouth.X) * Facing;
                    float pass = Math.Clamp((140f - ahead) / 260f, 0f, 1f);
                    Vector2 groundAhead = new Vector2(Mouth.X + Facing * 225f, lockedImpact.Y + FlightClimb - 22f);
                    aimPoint = Vector2.Lerp(aimPoint, groundAhead, pass);
                }

                if (liveTick < liveTicks)
                {
                    Vector2 toTarget = aimPoint - Mouth;
                    float wantedPitch = MathF.Atan2(toTarget.Y, Math.Max(14f, toTarget.X * Facing));
                    SteerAirAim(wantedPitch, 0.052f);
                }

                Vector2 wantedVelocity;
                if (strafing)
                {
                    float forwardSpeed = breathing ? AirStrafeSpeed : 2f;
                    float altitudeCorrection = MathHelper.Clamp((waypoint.Y - NPC.Bottom.Y) * 0.07f, -5f, 5f);
                    wantedVelocity = new Vector2(Facing * forwardSpeed, altitudeCorrection);
                }
                else
                {
                    float standOffX = PredictX(target.Center.X, target.velocity.X, 12, 130f) - Facing * 690f;
                    float hoverY = target.Bottom.Y - FlightClimb;
                    if (airBreathMode == AirBreathSkyward)
                    {
                        hoverY = target.Center.Y + 70f;
                    }
                    wantedVelocity = new Vector2(MathHelper.Clamp((standOffX - NPC.Bottom.X) * 0.045f, -8f, 8f),
                        MathHelper.Clamp((hoverY - NPC.Bottom.Y) * 0.045f, -6f, 6f));
                }

                NPC.velocity = Vector2.Lerp(NPC.velocity, wantedVelocity, 0.12f);

                if (authority && breathing && liveTick % BreathShotInterval == 0)
                {
                    SpawnBreath(68, true);
                }

                // Clients steer the aim from their own copy of the target; resync so the head never drifts far.
                if (authority && StateTimer % 20 == 0)
                {
                    NPC.netUpdate = true;
                }

                bool acrossField = strafing && (NPC.Bottom.X - lockedImpact.X) * Facing >= 0f;
                bool playerBehind = !strafing && (target.Center.X - NPC.Center.X) * Facing < -140f;
                if (authority && playerBehind)
                {
                    airTurnToPursue = true;
                    EnterState(DrakeState.AirTurn);
                }
                else if (authority && (acrossField || strafing && liveTick >= liveTicks))
                {
                    airTurnToPursue = false;
                    EnterState(DrakeState.AirTurn);
                }
                else if (authority && (StateTimer >= AirTell + liveTicks + BreathCloseTicks || blockedTicks > 8))
                {
                    StartLanding();
                }
            }
            else if (State == DrakeState.AirTurn)
            {
                if (authority && StateTimer == AirTurnFlipTick)
                {
                    NPC.ai[2] = -NPC.ai[2];
                    NPC.netUpdate = true;
                }

                float turnSpeed = StateTimer < AirTurnFlipTick ? 0f : Facing * 10f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(turnSpeed, -1.25f), 0.22f);
                if (authority && StateTimer >= AirTurnTicks)
                {
                    if (airTurnToPursue)
                    {
                        EnterState(DrakeState.AirApproach);
                    }
                    else
                    {
                        StartLanding();
                    }
                }
            }
            else if (State == DrakeState.AirDive)
            {
                TickAirDive(onGround, authority);
            }
            else if (State == DrakeState.RetreatBreath)
            {
                int moveTick = StateTimer - RetreatTell;
                int fireTick = StateTimer - RetreatFireTick;
                bool retreatBreathing = fireTick >= 0 && fireTick < RetreatBreathLive;

                // Climb up and back on a sine-eased speed curve: still at both ends, fastest mid-climb. The curve's
                // area is 1, so the body covers exactly RetreatBack x RetreatUp over RetreatMoveTicks.
                Vector2 retreatVelocity = Vector2.Zero;

                if (moveTick >= 0 && moveTick < RetreatMoveTicks)
                {
                    float climbProgress = moveTick / (float)RetreatMoveTicks;
                    float speedCurve = MathF.Sin(MathHelper.Pi * climbProgress) * MathHelper.PiOver2;
                    float backSpeed = RetreatBack / RetreatMoveTicks * speedCurve;
                    float upSpeed = RetreatUp / RetreatMoveTicks * speedCurve;
                    retreatVelocity = new Vector2(-Facing * backSpeed, -upSpeed);
                }

                NPC.velocity = Vector2.Lerp(NPC.velocity, retreatVelocity, 0.25f);

                // Track from the actual mouth, which moves back and up as the wings beat.
                if (fireTick < RetreatBreathLive)
                {
                    Vector2 toTarget = target.Center - Mouth;
                    float forwardDistance = Math.Max(30f, toTarget.X * Facing);
                    float wantedPitch = MathF.Atan2(toTarget.Y, forwardDistance);
                    SteerAirAim(wantedPitch, 0.036f);
                }

                if (authority && retreatBreathing && fireTick % BreathShotInterval == 0)
                {
                    SpawnBreath(72, true);
                }

                if (authority && StateTimer % 20 == 0)
                {
                    NPC.netUpdate = true;
                }

                if (authority && (StateTimer >= RetreatFireTick + RetreatBreathLive + BreathCloseTicks || blockedTicks > 8))
                {
                    StartLanding();
                }
            }
            else if (State == DrakeState.Land)
            {
                if (authority && onGround && StateTimer > 1)
                {
                    groundedTicks = 0;
                    flightCooldown = 300;
                    home = NPC.Bottom;
                    EnterState(DrakeState.Recover);
                    return;
                }

                float distanceX = waypoint.X - NPC.Bottom.X;
                float height = waypoint.Y - NPC.Bottom.Y;
                // Approach the touchdown from behind the glide so horizontal motion continues onto the floor.
                float glideLead = landingDrift * Math.Clamp(height / 8f, 0f, 26f);
                float speedX = MathHelper.Clamp(landingDrift + (distanceX - glideLead) * 0.085f, -10f, 10f);
                float minimumDrift = landingDrift * 0.65f;
                if (height < 48f && Math.Abs(distanceX) < 68f && speedX * landingDrift < minimumDrift * landingDrift)
                {
                    speedX = minimumDrift;
                }
                float speedY = MathHelper.Clamp(height * 0.095f, 3f, 12f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(speedX, speedY), 0.18f);
                if (authority && (blockedTicks > 12 || StateTimer > 180))
                {
                    StartLanding();
                }
            }
        }

        private void TickAirDive(bool onGround, bool authority)
        {
            if (StateTimer < AirDiveTell)
            {
                NPC.velocity *= 0.79f;
                return;
            }

            Vector2 toLanding = lockedImpact - NPC.Bottom;
            if (authority && AirDivingBite && airBiteStart < 0 && (toLanding.Y < 120f || onGround))
            {
                airBiteStart = StateTimer;
                SpawnAttack(AirLandingBiteAttack, 85);
                NPC.netUpdate = true;
            }

            if (onGround)
            {
                if (authority && airTouchdown < 0)
                {
                    airTouchdown = StateTimer;
                    NPC.netUpdate = true;
                }
                NPC.velocity.X *= 0.78f;
                NPC.velocity.Y = 2f;
            }
            else
            {
                float speedX = MathHelper.Clamp(toLanding.X * 0.07f, -12f, 12f);
                if (Math.Abs(toLanding.X) < 35f && AirDivingBite)
                {
                    speedX += Facing * 3f;
                }
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, speedX, 0.23f);
                NPC.velocity.Y = Math.Min(NPC.velocity.Y + 1.65f, 23f);
            }

            if (authority && airTouchdown >= 0 && StateTimer - airTouchdown >= 18)
            {
                groundedTicks = 0;
                flightCooldown = 300;
                home = NPC.Bottom;
                EnterState(DrakeState.Recover);
            }
            else if (authority && airTouchdown < 0 && StateTimer > AirDiveTell + 75)
            {
                StartLanding();
            }
        }

        // Turn the synced breath aim toward wantedPitch by at most turnRate radians this tick. Pitch is positive-down
        // and clamped to the aerial aim limits; lockedAim is rebuilt on the current facing.
        private void SteerAirAim(float wantedPitch, float turnRate)
        {
            wantedPitch = MathHelper.Clamp(wantedPitch, -AirAimUpLimit, AirAimDownLimit);
            float aimPitch = MathF.Atan2(lockedAim.Y, lockedAim.X * Facing);
            float pitchStep = MathHelper.Clamp(wantedPitch - aimPitch, -turnRate, turnRate);
            aimPitch += pitchStep;
            lockedAim = new Vector2(Facing * MathF.Cos(aimPitch), MathF.Sin(aimPitch));
        }

        private void StartLanding()
        {
            landingDrift = MathHelper.Clamp(NPC.velocity.X, -7f, 7f);
            if (Math.Abs(landingDrift) < 2f)
            {
                landingDrift = State == DrakeState.RetreatBreath ? -Facing * 4.5f : Facing * 4.5f;
            }

            float landingX = NPC.Center.X + landingDrift * 20f;
            float searchY = NPC.Bottom.Y - 24f;
            if (!FindLanding(landingX, searchY, out waypoint, fullBody: true)
                && !FindLanding(landingX, searchY, out waypoint)
                && !FindLanding(home.X, home.Y - 80f, out waypoint, fullBody: true))
            {
                // Descend against normal tile collision until a surface catches the support core.
                waypoint = NPC.Bottom + Vector2.UnitY * 160f;
            }

            blockedTicks = 0;
            EnterState(DrakeState.Land);
        }

        private bool FindLanding(float nearX, float startY, out Vector2 landing, bool fullBody = false)
        {
            for (int i = 0; i < 17; i++)
            {
                int offset = (i + 1) / 2 * 40;
                if (i % 2 == 0)
                {
                    offset = -offset;
                }

                float x = MathHelper.Clamp(nearX + offset, 80f, Main.maxTilesX * 16f - 80f);
                int startTile = Math.Max(10, (int)(startY / 16f));
                int lastTile = Math.Min(Main.maxTilesY - 10, startTile + 70);
                for (int y = startTile; y < lastTile; y++)
                {
                    Vector2 bottom = new Vector2(x, y * 16f);
                    if (!Supported(bottom + new Vector2(-38f, 2f))
                        || !Supported(bottom + new Vector2(38f, 2f)))
                    {
                        continue;
                    }

                    Vector2 top = bottom - new Vector2(NPC.width / 2f, NPC.height);
                    if (!Collision.SolidCollision(top, NPC.width, NPC.height)
                        && (!fullBody || AirSpaceClear(bottom)))
                    {
                        landing = bottom;
                        return true;
                    }
                }
            }

            landing = Vector2.Zero;
            return false;
        }

        private static bool Supported(Vector2 position)
        {
            int x = (int)(position.X / 16f);
            int y = (int)(position.Y / 16f);
            if (!WorldGen.InWorld(x, y, 5))
            {
                return false;
            }

            Tile tile = Framing.GetTileSafely(x, y);
            return tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType]
                || Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0);
        }

        private static bool TryGroundSurface(float worldX, float fromY, float throughY, out float groundY)
        {
            int tileX = (int)(worldX / 16f);
            int firstRow = Math.Max(5, (int)(fromY / 16f));
            int lastRow = Math.Min(Main.maxTilesY - 6, (int)(throughY / 16f));
            for (int tileY = firstRow; tileY <= lastRow; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY, 5))
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                int tileType = tile.TileType;
                bool platform = Main.tileSolidTop[tileType] && tile.TileFrameY == 0;
                if (!tile.HasUnactuatedTile || (!Main.tileSolid[tileType] && !platform))
                {
                    continue;
                }

                // A half-block raises its solid edge at y+8. A downward slope meets this x inside the tile.
                float surface = tileY * 16f;
                float localX = worldX - tileX * 16f;
                if (tile.IsHalfBlock)
                {
                    surface += 8f;
                }
                else if (tile.Slope == SlopeType.SlopeDownLeft)
                {
                    surface += localX;
                }
                else if (tile.Slope == SlopeType.SlopeDownRight)
                {
                    surface += 16f - localX;
                }

                if (surface >= fromY && surface <= throughY)
                {
                    groundY = surface;
                    return true;
                }
            }

            groundY = throughY;
            return false;
        }

        private void EmitClawGrit(int partIndex, int count)
        {
            Vector2 claw = GetClawPosition(partIndex);
            float floor = NPC.Bottom.Y;
            if (!TryGroundSurface(claw.X, floor - 24f, floor + 32f, out float gritY))
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float offset = Main.rand.NextFloat(-15f, 15f);
                float travel = Math.Sign(offset) * Main.rand.NextFloat(2f, 5.2f) + NPC.velocity.X * 0.22f;
                Vector2 position = new Vector2(claw.X + offset, gritY - 3f);
                Vector2 drift = new Vector2(travel, Main.rand.NextFloat(-2.2f, -0.5f));
                Dust.NewDustPerfect(position, DustID.Dirt, drift, 0, default, Main.rand.NextFloat(0.8f, 1.45f));
            }
        }

        private bool TryPlanLeap(Player target, out Vector2 launchVelocity, out int airtime)
        {
            launchVelocity = Vector2.Zero;
            airtime = LeapMinAirTicks;
            float snoutX = ToWorld(SnoutSocket).X;
            float gap = (target.Center.X - snoutX) * Facing;
            if (gap < LeapMinRange - 135f || gap > LeapMaxRange + 355f
                || Math.Abs(target.Bottom.Y - NPC.Bottom.Y) >= 170f)
            {
                return false;
            }

            // Lead the snout to the intercept. The feet land a head length behind it, on the sampled floor.
            int previewTicks = Math.Clamp((int)(gap / 18f), LeapMinAirTicks, LeapMaxAirTicks);
            float interceptX = PredictX(target.Center.X, target.velocity.X, previewTicks + LeapTell - LeapLockTick, 425f);
            float interceptGap = (interceptX - snoutX) * Facing;
            float lungeForward = LeapBiteReach * MathF.Cos(0.65f);
            float travel = MathHelper.Clamp(interceptGap - lungeForward + LeapOvershoot, 140f, 1600f);
            airtime = Math.Clamp((int)(travel / 18f), LeapMinAirTicks, LeapMaxAirTicks);
            float landingX = NPC.Bottom.X + Facing * travel;
            if (!TryGroundSurface(landingX, NPC.Bottom.Y - 72f, NPC.Bottom.Y + 104f, out float surfaceY))
            {
                return false;
            }

            float landingDelta = surfaceY - NPC.Bottom.Y;
            float speedX = travel / airtime;
            float speedY = landingDelta / airtime - LeapGravity * (airtime - 1) * 0.5f;
            // Match the discrete launch and gravity steps. The two-pixel margin lets the feet meet the surface.
            for (int tick = 1; tick <= airtime; tick++)
            {
                float height = speedY * tick + LeapGravity * tick * (tick - 1) * 0.5f;
                Vector2 bottom = NPC.Bottom + new Vector2(Facing * speedX * tick, height - 2f);
                Vector2 topLeft = bottom - new Vector2(NPC.width * 0.5f, NPC.height);
                if (Collision.SolidCollision(topLeft, NPC.width, NPC.height))
                {
                    return false;
                }
            }

            launchVelocity = new Vector2(speedX, speedY);
            return true;
        }

        internal static float PredictX(float position, float velocity, int ticksAhead, float maxLead)
        {
            if (Math.Abs(velocity) < 0.65f)
            {
                return position;
            }

            float runningSpeed = Math.Clamp(velocity, -14f, 14f);
            return position + Math.Clamp(runningSpeed * ticksAhead, -maxLead, maxLead);
        }

        private static Vector2 PredictBite(Player target, int ticksAhead, float maxLead)
        {
            float x = PredictX(target.Center.X, target.velocity.X, ticksAhead, maxLead);
            float y = target.Center.Y + Math.Clamp(target.velocity.Y * 4f, -34f, 26f);
            return new Vector2(x, y);
        }

        private void PlanJawLunge(Player target, int ticksAhead, float maxLead)
        {
            // Measure from the neutral snout. The animated neck follows this lock without feeding back into it.
            Vector2 toIntercept = PredictBite(target, ticksAhead, maxLead) - ToWorld(SnoutSocket);
            float distance = Math.Max(0f, toIntercept.Length() - 18f);
            lockedAim = AimAt(toIntercept, 0.75f);
            biteReach = Math.Clamp(distance * 0.65f, 80f, 112f);
            float horizontalReach = biteReach * Math.Abs(lockedAim.X);
            biteSpeed = Math.Clamp((toIntercept.X * Facing - horizontalReach * 0.78f) / 9f, -4.8f, 11.5f);
        }

        private bool ClearRoute(Vector2 start, Vector2 end, bool airEnvelope = false)
        {
            float distance = Vector2.Distance(start, end);
            int samples = Math.Max(1, (int)Math.Ceiling(distance / 12f));
            for (int i = 1; i <= samples; i++)
            {
                Vector2 bottom = Vector2.Lerp(start, end, i / (float)samples);
                Vector2 top = bottom - new Vector2(NPC.width / 2f, NPC.height);
                if (Collision.SolidCollision(top, NPC.width, NPC.height))
                {
                    return false;
                }

                if (airEnvelope && !AirSpaceClear(bottom))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AirSpaceClear(Vector2 bottom)
        {
            // Reserve the full wing-and-tail envelope while flying above the mountain.
            // Its lower edge stays above the support line so a flat launch floor remains valid.
            float left = -418f;
            if (Facing > 0)
            {
                left = -502f;
            }

            Vector2 top = bottom + new Vector2(left, -468f);
            return !Collision.SolidCollision(top, 920, 456);
        }

        private Vector2 AimAt(Vector2 difference, float maxPitch)
        {
            float pitch = MathF.Atan2(difference.Y, Math.Max(30f, difference.X * Facing));
            pitch = MathHelper.Clamp(pitch, -maxPitch, maxPitch);
            return new Vector2(Facing * MathF.Cos(pitch), MathF.Sin(pitch));
        }

        private void EnterState(DrakeState next)
        {
            NPC.ai[0] = (float)next;
            NPC.ai[1] = 0f;
            sequence++;
            NPC.netUpdate = true;
        }

        private void SpawnAttack(int kind, int damage)
        {
            Vector2 origin = Mouth;
            if (kind == NearFootAttack || kind == FarFootAttack)
            {
                origin = GetClawPosition(kind == NearFootAttack ? 13 : 14);
            }

            int projectileIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, Vector2.Zero,
                ModContent.ProjectileType<HellkiteDrakeAttack>(), damage, 0f, Main.myPlayer,
                NPC.whoAmI, kind, sequence);
            if (kind == ChargeAttack || kind == GrabBiteAttack)
            {
                tsorcGlobalProjectile.SetDefenseTraits(projectileIndex, AttackDefenseTraits.BypassesActiveShield);
            }
        }

        internal void TryGrabPlayer(Player victim, int biteSequence)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || State != DrakeState.GrabBite
                || sequence != biteSequence || GrabTarget != victim.whoAmI || !victim.active || victim.dead
                || StateTimer < GrabTell || StateTimer > GrabTell + BiteLive + 8)
            {
                return;
            }

            UpdatePose();
            // The victim's hit hook already resolved i-frames. Allow brief packet transit near the posed jaws.
            if (Vector2.DistanceSquared(Mouth, victim.Center) > 150f * 150f
                || !Collision.CanHitLine(Jaw, 1, 1, victim.Center, 1, 1))
            {
                return;
            }

            grabContactTick = Math.Min(StateTimer, GrabTell + BiteLive - 1);
            EnterState(DrakeState.GrabHold);
        }

        private void SpawnBreath(int damage, bool longRange = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            UpdatePose();
            // Emit three lanes from the gap between the jaws. They fan outward by ±9.5° as they travel.
            // The long-range breath uses faster, longer-lived puffs on a tighter ±5.7° fan (see LongFlame constants).
            Vector2 forward = BreathDirection.SafeNormalize(new Vector2(Facing, 0f));
            Vector2 sideways = new Vector2(-forward.Y, forward.X);
            int flameType = ModContent.ProjectileType<HellkiteDrakeFlame>();
            float laneSpreadDegrees = 9.5f;
            float flameSpeed = DemonBreath.Speed;

            if (longRange)
            {
                flameType = ModContent.ProjectileType<HellkiteDrakeLongFlame>();
                laneSpreadDegrees = LongFlameSpreadDegrees;
                flameSpeed = LongFlameSpeed;
            }

            for (int lane = -1; lane <= 1; lane++)
            {
                Vector2 spawnPosition = Mouth + sideways * (lane * 6f);
                float spreadAngle = lane * MathHelper.ToRadians(laneSpreadDegrees) + Main.rand.NextFloat(-0.045f, 0.045f);
                float speed = flameSpeed * Main.rand.NextFloat(0.92f, 1.08f);
                Vector2 velocity = forward.RotatedBy(spreadAngle) * speed;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPosition, velocity,
                    flameType, damage, 0f, Main.myPlayer,
                    (float)DemonTier.Oolacile, NPC.whoAmI);
            }
        }

        private Vector2 GetBreathAim()
        {
            Vector2 direction = lockedAim;
            int sweepTick = StateTimer - BreathTell;
            if (State == DrakeState.Breath && sweepTick >= -8)
            {
                // Match Oolacile's 54-degree sweep, mirrored about the committed aim for each facing.
                float totalDegrees = BreathSweepDegreesPerTick * BreathLive;
                int aimedTick = Math.Clamp(sweepTick, 0, BreathLive - 1);
                float offsetDegrees = -totalDegrees * 0.5f + BreathSweepDegreesPerTick * aimedTick;
                if (sweepTick < 0)
                {
                    float ready = (sweepTick + 8f) / 8f;
                    offsetDegrees *= MathHelper.SmoothStep(0f, 1f, ready);
                }
                else if (sweepTick >= BreathLive)
                {
                    float finish = MathHelper.Clamp((sweepTick - BreathLive) / (float)BreathCloseTicks, 0f, 1f);
                    offsetDegrees *= 1f - MathHelper.SmoothStep(0f, 1f, finish);
                }
                direction = direction.RotatedBy(MathHelper.ToRadians(offsetDegrees) * Facing);
            }

            return direction;
        }

        public void OnStagger(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && State != DrakeState.Collapse)
            {
                biteChainStep = 0;

                if (Airborne)
                {
                    StartLanding();
                }
                else
                {
                    EnterState(DrakeState.Recover);
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
            writer.Write(sequence);
            writer.Write(turnFacing);
            writer.Write(walkClock);
            writer.Write(biteSpeed);
            writer.Write(biteReach);
            writer.Write(airBreathMode);
            writer.Write(airTurnToPursue);
            writer.Write(airBiteStart);
            writer.Write(airTouchdown);
            writer.Write(collapseTouchdown);
            writer.Write(landingDrift);
            writer.Write(biteChainStep);
            writer.Write(turnBite);
            writer.Write(turnBreath);
            writer.Write(leapAirTicks);
            writer.Write(leapLandTick);
            writer.Write(leapLaunchY);
            writer.Write(GrabTarget);
            writer.Write(grabContactTick);
            writer.Write(chargeTicks);
            writer.Write(lockedAim.X);
            writer.Write(lockedAim.Y);
            writer.Write(biteWindupAim.X);
            writer.Write(biteWindupAim.Y);
            writer.Write(lockedImpact.X);
            writer.Write(lockedImpact.Y);
            writer.Write(waypoint.X);
            writer.Write(waypoint.Y);
            writer.Write(home.X);
            writer.Write(home.Y);
            writer.Write(Wild);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            initialized = reader.ReadBoolean();
            sequence = reader.ReadInt32();
            turnFacing = reader.ReadInt32();
            walkClock = reader.ReadSingle();
            biteSpeed = reader.ReadSingle();
            biteReach = reader.ReadSingle();
            airBreathMode = reader.ReadByte();
            airTurnToPursue = reader.ReadBoolean();
            airBiteStart = reader.ReadInt32();
            airTouchdown = reader.ReadInt32();
            collapseTouchdown = reader.ReadInt32();
            landingDrift = reader.ReadSingle();
            biteChainStep = reader.ReadByte();
            turnBite = reader.ReadBoolean();
            turnBreath = reader.ReadBoolean();
            leapAirTicks = reader.ReadInt32();
            leapLandTick = reader.ReadInt32();
            leapLaunchY = reader.ReadSingle();
            GrabTarget = reader.ReadInt32();
            grabContactTick = reader.ReadInt32();
            chargeTicks = reader.ReadInt32();
            lockedAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            biteWindupAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            lockedImpact = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            waypoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            home = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Wild = reader.ReadBoolean();
            if (Wild)
            {
                ApplyWildState();
            }
        }

        private int WalkRow(float playbackRate)
        {
            if (State == DrakeState.Turn || Math.Abs(NPC.velocity.X) < 0.1f)
            {
                return 0;
            }

            float clock = walkClock * playbackRate;
            clock %= 62f;
            for (int i = 0; i < WalkTicks.Length; i++)
            {
                if (clock < WalkTicks[i])
                {
                    return i;
                }

                clock -= WalkTicks[i];
            }

            return 0;
        }

        private float Projection()
        {
            float profile = 1f;
            if (State == DrakeState.AirTurn)
            {
                // Show the narrow profile at the facing swap so the wings and neck do not jump across the body.
                float halfTurn = Math.Clamp((StateTimer - AirTurnFlipTick) / (float)AirTurnFlipTick, -1f, 1f);
                profile = Math.Max(0.14f, Math.Abs(MathF.Sin(halfTurn * MathHelper.PiOver2)));
            }
            return -Facing * profile;
        }

        private Vector2 ToWorld(Vector2 source)
        {
            Vector2 offset = source - SourceFloor;
            offset.X *= Projection();
            return NPC.Bottom + offset * ArtScale;
        }

        private Vector2 ToSource(Vector2 world)
        {
            Vector2 source = (world - NPC.Bottom) / ArtScale;
            source.X /= Projection();
            return source + SourceFloor;
        }

        private void UpdatePose()
        {
            if (State == DrakeState.Collapse)
            {
                PoseCollapse();
                return;
            }

            bool banking = State == DrakeState.AirTurn;
            bool preparingFlyby = State == DrakeState.AirBreath && airBreathMode == AirBreathStrafe && StateTimer < AirTell;
            float turnPoseTick = StateTimer;
            if (State == DrakeState.Turn && StateTimer > TurnBodyTick)
            {
                // Keep the authored motion up to the body flip, then bring the neck and tail around for pursuit.
                float finishTicks = TurnFinishTick - TurnBodyTick;
                float progress = MathHelper.Clamp((StateTimer - TurnBodyTick) / finishTicks, 0f, 1f);
                turnPoseTick = MathHelper.Hermite(TurnBodyTick, finishTicks, TurnTicks, 0f, progress);
            }
            float turnClock = turnPoseTick / TurnStretchFactor;
            float phase = walkClock / 62f * MathHelper.TwoPi;
            if (Airborne)
            {
                phase = StateTimer * 0.07f;
            }

            int walkRow = WalkRow(1f);
            int legWalkRow = WalkRow(LegWalkPlaybackRate);
            int mouthRow = 0;
            float breathClosing = 0f;
            bool raining = State == DrakeState.DestinedDeathRain;
            float rainHeadLift = 0f;
            if (raining)
            {
                float raise = MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / 34f, 0f, 1f));
                float lower = MathHelper.SmoothStep(0f, 1f, Math.Clamp((StateTimer - RainLastWave) / (float)RainFollowThrough, 0f, 1f));
                rainHeadLift = raise * (1f - lower);
            }
            bool holdingGrip = State == DrakeState.GrabHold;
            bool tossingGrip = State == DrakeState.GrabToss;
            bool posingGroundBite = GroundBiting || holdingGrip || tossingGrip;
            int biteTellTicks = BiteWindupTicks;
            int bitePoseTick = StateTimer;
            if (holdingGrip)
            {
                bitePoseTick = Math.Min(grabContactTick + StateTimer, GrabTell + BiteLive + BiteHoldTicks);
            }
            else if (tossingGrip)
            {
                bitePoseTick = GrabTell + BiteLive + BiteHoldTicks + StateTimer;
            }

            // Leap bite clocks, shared by the jaw, crouch, wings, legs and neck below.
            // leapCrouch 0..1 = how deep the pre-launch crouch is; leapSnap 0..1 = how far out the jaws are.
            bool leaping = State == DrakeState.LeapBite;
            bool posingDescendingBite = leaping || AirDivingBite;
            bool leapAirborne = leaping && StateTimer >= LeapTell && leapLandTick < 0;
            float leapCrouch = 0f;
            float leapSnap = 0f;

            if (leaping && StateTimer < LeapTell)
            {
                leapCrouch = MathHelper.SmoothStep(0f, 1f, StateTimer / (float)LeapTell);
            }
            else if (leapAirborne)
            {
                // Uncoil over the first 8 flight ticks, then throw the jaws out over 8 ticks ending as the hit goes live.
                int flightTick = StateTimer - LeapTell;
                leapCrouch = 1f - MathHelper.Clamp(flightTick / 8f, 0f, 1f);
                int snapStart = leapAirTicks - LeapBiteLiveLead - 8;
                float snapProgress = MathHelper.Clamp((flightTick - snapStart) / 8f, 0f, 1f);
                leapSnap = 1f - MathF.Pow(1f - snapProgress, 3f);
            }
            else if (leaping)
            {
                // Landed: hold the bite 8 ticks, then draw the neck back in over 18 (cosine).
                int landedTicks = StateTimer - leapLandTick;
                float retractProgress = MathHelper.Clamp((landedTicks - BiteHoldTicks) / (float)BiteRetractTicks, 0f, 1f);
                leapSnap = 0.5f * (1f + MathF.Cos(MathHelper.Pi * retractProgress));
            }
            else if (AirDivingBite)
            {
                float coil = MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / 14f, 0f, 1f));
                float release = MathHelper.SmoothStep(0f, 1f, Math.Clamp((StateTimer - 24f) / 15f, 0f, 1f));
                leapCrouch = 0.82f * coil * (1f - release);
                if (airTouchdown >= 0)
                {
                    float retract = Math.Clamp((StateTimer - airTouchdown - 5f) / 13f, 0f, 1f);
                    leapSnap = MathHelper.SmoothStep(1f, 0f, retract);
                }
                else
                {
                    float height = lockedImpact.Y - NPC.Bottom.Y;
                    leapSnap = MathHelper.SmoothStep(0f, 1f, Math.Clamp((200f - height) / 130f, 0f, 1f));
                }
            }

            if (leaping)
            {
                mouthRow = 1;

                if (leapAirborne || leapSnap > 0.5f)
                {
                    mouthRow = 2;
                }
            }
            else if (AirDivingBite)
            {
                mouthRow = leapSnap > 0.32f ? 2 : 1;
            }

            if (posingGroundBite)
            {
                mouthRow = 1;
                int jawOpenLead = 6;
                if (State == DrakeState.GrabBite)
                {
                    jawOpenLead = 14;
                }

                bool openForBite = !holdingGrip && bitePoseTick >= biteTellTicks - jawOpenLead
                    && bitePoseTick < biteTellTicks + BiteLive + BiteHoldTicks;
                bool openToToss = tossingGrip && StateTimer < 7;
                if (openForBite || openToToss)
                {
                    mouthRow = 2;
                }
            }
            else if (State == DrakeState.BiteChain)
            {
                // Jaw stays parted through the walk-in so the chain reads as one continuous threat.
                mouthRow = 1;
            }
            else if (State == DrakeState.Breath || AirBreathing)
            {
                int tellTicks = BreathTell;
                int liveTicks = BreathLive;
                if (AirBreathing)
                {
                    tellTicks = AirTellTicks;
                    liveTicks = AirLiveTicks;
                }

                int releaseEnd = tellTicks + liveTicks;
                float closingProgress = MathHelper.Clamp((StateTimer - releaseEnd) / (float)BreathCloseTicks, 0f, 1f);
                breathClosing = MathHelper.SmoothStep(0f, 1f, closingProgress);
                int openLead = preparingFlyby ? 30 : 16;
                if (StateTimer >= tellTicks - openLead && StateTimer < tellTicks - 8)
                {
                    mouthRow = 1;
                }
                else if (StateTimer >= tellTicks - 8 && StateTimer < releaseEnd)
                {
                    mouthRow = 2;
                }
                else if (StateTimer >= releaseEnd && StateTimer < releaseEnd + BreathCloseTicks / 2)
                {
                    mouthRow = 1;
                }
            }
            else if (raining && StateTimer >= 30 && StateTimer < RainLastWave + 12)
            {
                mouthRow = 1;
                if (StateTimer >= RainTell - 12 && StateTimer < RainLastWave + 5)
                {
                    mouthRow = 2;
                }
            }

            // Ground charge clocks. chargeRear 0..1..0 = the head rearing back through the first 35 tell ticks;
            // chargeLow 0..1 = the head dropped to ram height (down over the last 12 tell ticks, back up in the skid).
            bool charging = State == DrakeState.Charge;
            float chargeRear = 0f;
            float chargeLow = 0f;

            if (charging)
            {
                int chargeRunTick = StateTimer - ChargeTell;
                chargeRear = MathF.Sin(MathHelper.Pi * MathHelper.Clamp(StateTimer / 35f, 0f, 1f));
                chargeLow = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((StateTimer - (ChargeTell - 12f)) / 12f, 0f, 1f));

                if (chargeRunTick >= chargeTicks)
                {
                    float skidProgress = MathHelper.Clamp((chargeRunTick - chargeTicks) / (float)ChargeSkidTicks, 0f, 1f);
                    chargeLow = 1f - MathHelper.SmoothStep(0f, 1f, skidProgress);
                }

                // Jaw parted for the whole charge.
                mouthRow = 1;
            }

            float bodyDrop = 0f;
            if (!Airborne && (walkRow == 1 || walkRow == 5))
            {
                bodyDrop = 4f;
            }

            if (leaping)
            {
                // Sink 10px into the crouch before launch, and again briefly as the landing is absorbed.
                bodyDrop = 10f * leapCrouch;

                if (leapLandTick >= 0)
                {
                    float settle = MathHelper.Clamp((StateTimer - leapLandTick) / 14f, 0f, 1f);
                    bodyDrop = 10f * MathF.Sin(MathHelper.Pi * settle);
                }
            }

            for (int i = 0; i < Parts.Length; i++)
            {
                bones[i] = new Bone(Parts[i].RestPivot, Parts[i].RestPivot, 0f);
                if (i < 13)
                {
                    bones[i].Position.Y += bodyDrop;
                }
            }

            bones[11].FrameRow = walkRow;
            bones[12].FrameRow = (walkRow + 7) % 8;
            // Cycle only the feet faster; the synced walk clock still drives the slower body and wings.
            bones[13].FrameRow = legWalkRow;
            bones[14].FrameRow = (legWalkRow + 4) % 8;
            if (Airborne)
            {
                bones[13].FrameRow = 9;
                bones[14].FrameRow = 8;
                bool retreatPlanted = State == DrakeState.RetreatBreath && StateTimer < RetreatTell;
                if (retreatPlanted || AirFeetExtended)
                {
                    bones[13].FrameRow = 0;
                    bones[14].FrameRow = 4;
                }
                else if (AirDivingSlam && StateTimer >= 5)
                {
                    bones[13].FrameRow = 6;
                }

                if (State == DrakeState.Takeoff && StateTimer < TakeoffCrouchTicks)
                {
                    bones[13].FrameRow = 8;
                    if (airBreathMode == AirBreathStrafe)
                    {
                        // Row 6 lifts the claw off the floor before row 0 lands the stomp.
                        bones[13].FrameRow = StateTimer < StrafeStompTick ? 6 : 0;
                        bones[14].FrameRow = 4;
                    }
                }
            }

            if (charging)
            {
                if (StateTimer < 30)
                {
                    // The tell's single wing beat: the folded wings run up through their eight lift frames and back
                    // (2 ticks each) while the shoulders swing up 0.35 / 0.28 rad and down again.
                    float beat = MathF.Sin(MathHelper.Pi * StateTimer / 30f);
                    int beatStep = Math.Clamp(StateTimer / 2, 0, TurnWingRows.Length - 1);
                    bones[11].FrameRow = TurnWingRows[beatStep];
                    bones[12].FrameRow = (TurnWingRows[beatStep] + 7) % 8;
                    bones[11].Rotation = -0.35f * beat;
                    bones[12].Rotation = -0.28f * beat;
                }
                else
                {
                    // Wings held slightly lifted off the back for the run.
                    bones[11].Rotation = -0.1f * chargeLow;
                    bones[12].Rotation = -0.08f * chargeLow;
                }
            }

            if (leaping)
            {
                // The hop's ground speed would spin the walk cycle, so the leap picks its own frames: planted feet
                // (rows 0 / 4) on the ground, the two tucked flight rows (9 near, 8 far) in the air, wings folded on
                // row 0 and lifted at the shoulder by up to 0.16 / 0.12 rad while airborne.
                bones[11].FrameRow = 0;
                bones[12].FrameRow = 7;
                bones[13].FrameRow = 0;
                bones[14].FrameRow = 4;

                if (leapAirborne)
                {
                    float wingLift = MathHelper.Clamp((StateTimer - LeapTell) / 8f, 0f, 1f);
                    bones[11].Rotation = -0.16f * wingLift;
                    bones[12].Rotation = -0.12f * wingLift;
                    bones[13].FrameRow = 9;
                    bones[14].FrameRow = 8;
                }
            }
            if (State == DrakeState.Turn)
            {
                // Lift the folded wings around the body flip, then ease them down as the tail settles.
                // Rows and shoulder angles follow the synced turn timer; drawing never advances the animation.
                int nearStep = Math.Clamp((int)(turnClock / 7f), 0, TurnWingRows.Length - 1);
                int farStep = Math.Clamp((int)((turnClock - 6f) / 7f), 0, TurnWingRows.Length - 1);
                bones[11].FrameRow = TurnWingRows[nearStep];
                bones[12].FrameRow = (TurnWingRows[farStep] + 7) % 8;

                float nearRaise = MathHelper.Clamp(turnClock / 24f, 0f, 1f);
                float nearFold = MathHelper.Clamp((turnClock - 24f) / 64f, 0f, 1f);
                nearRaise = MathHelper.SmoothStep(0f, 1f, nearRaise);
                nearFold = MathHelper.SmoothStep(0f, 1f, nearFold);
                bones[11].Rotation = -0.16f * nearRaise * (1f - nearFold);

                float farRaise = MathHelper.Clamp((turnClock - 6f) / 26f, 0f, 1f);
                float farFold = MathHelper.Clamp((turnClock - 32f) / 66f, 0f, 1f);
                farRaise = MathHelper.SmoothStep(0f, 1f, farRaise);
                farFold = MathHelper.SmoothStep(0f, 1f, farFold);
                bones[12].Rotation = -0.12f * farRaise * (1f - farFold);

                // Rock the chest over the planted feet as the throat rises, then lower it after the flip.
                // Rotate the torso and attached shoulder layers around SourceFloor without scaling the art.
                float chestAngle = TurnChestAngle(turnClock);
                Vector2 chestOffset = Parts[0].RestPivot - SourceFloor;
                Vector2 chestPosition = SourceFloor + chestOffset.RotatedBy(chestAngle);
                bones[0] = new Bone(Parts[0].RestPivot, chestPosition, chestAngle);

                for (int i = 8; i <= 12; i++)
                {
                    bones[i].Position = bones[0].Map(Parts[i].RestPivot);
                    bones[i].Rotation += chestAngle;
                }

                // Take one normal-speed step around the flip instead of stretching eight leg frames
                // over the whole turn. Six game ticks per pose preserve the original walking cadence.
                float stepClock = StateTimer - (TurnBodyTick - 24f);
                int stepRow = Math.Clamp((int)(stepClock / 6f), 0, 8) % 8;
                float stepProgress = MathHelper.Clamp(stepClock / 48f, 0f, 1f);
                float stepArc = MathF.Sin(MathHelper.Pi * stepProgress);
                Vector2 footWorld = NPC.Bottom + new Vector2(turnFacing * 18f * stepArc, -1f - 22f * stepArc);
                Vector2 footOffset = TurnFootContacts[stepRow] - Parts[13].RestPivot;
                bones[13].Position = ToSource(footWorld) - footOffset;
                bones[13].FrameRow = stepRow;

                float braceIn = MathHelper.Clamp(turnClock / 8f, 0f, 1f);
                float braceOut = MathHelper.Clamp((turnClock - 58f) / 20f, 0f, 1f);
                braceIn = MathHelper.SmoothStep(0f, 1f, braceIn);
                braceOut = MathHelper.SmoothStep(0f, 1f, braceOut);
                bones[14].Position += new Vector2(-8f, 8f) * braceIn * (1f - braceOut);
            }

            float neckBend = 0.04f * MathF.Sin(phase);
            float secondBend = -0.03f * MathF.Sin(phase - 0.4f);
            float thirdBend = 0.025f * MathF.Sin(phase - 0.8f);
            float headLead = 0f;
            if (banking)
            {
                // Lift the jaw into the turn before the chest carries the rest of the rig over.
                float leadClock = Math.Clamp(StateTimer / 15f, 0f, 1f);
                headLead = MathF.Sin(MathHelper.Pi * leadClock);
                neckBend += 0.13f * headLead;
                secondBend += 0.16f * headLead;
                thirdBend += 0.19f * headLead;
            }
            if (raining)
            {
                // Raise the links with the jaw so the muzzle points skyward without separating from the throat.
                neckBend += 0.36f * rainHeadLift;
                secondBend += 0.19f * rainHeadLift;
                thirdBend += 0.12f * rainHeadLift;
            }
            float gripSway = 0f;
            float gripLift = 0f;
            float gripThrust = 0f;
            if (holdingGrip || tossingGrip)
            {
                // Raise the captured player, shake across the front of the torso twice, then whip the jaw out.
                // The held socket rides the posed head, so the player and the silhouette share the same motion.
                float gripClock = StateTimer;
                if (tossingGrip)
                {
                    gripClock += GrabHoldDuration;
                }
                float raise = MathHelper.SmoothStep(0f, 1f, Math.Clamp(gripClock / 8f, 0f, 1f));
                float settle = MathHelper.SmoothStep(1f, 0f, Math.Clamp((gripClock - GrabHoldDuration) / GrabTossDuration, 0f, 1f));
                float thrash = Math.Clamp((gripClock - 8f) / 36f, 0f, 1f);
                float flick = MathHelper.SmoothStep(0f, 1f, Math.Clamp((gripClock - 44f) / 8f, 0f, 1f));
                gripSway = MathF.Sin(thrash * MathHelper.TwoPi * 2f) * MathF.Sin(thrash * MathHelper.Pi);
                gripLift = raise * settle;
                gripThrust = (-26f + gripSway * 58f + flick * 68f) * gripLift;
                neckBend += (0.10f + gripSway * 0.08f) * gripLift;
                secondBend += gripSway * 0.10f;
            }

            currentBiteExtension = 0f;
            float biteAimWeight = 0f;
            Vector2 bitePoseAim = lockedAim;
            if (posingGroundBite)
            {
                float lockProgress = MathHelper.Clamp((bitePoseTick - (biteTellTicks - 6f)) / 6f, 0f, 1f);
                float aimBlend = MathHelper.SmoothStep(0f, 1f, lockProgress);
                bitePoseAim = Vector2.Lerp(biteWindupAim, lockedAim, aimBlend);

                float curl = MathHelper.Clamp(bitePoseTick / (float)biteTellTicks, 0f, 1f);
                if (bitePoseTick >= biteTellTicks)
                {
                    curl = MathHelper.Lerp(1f, -0.2f, MathHelper.Clamp((bitePoseTick - biteTellTicks) / 6f, 0f, 1f));
                }

                if (bitePoseTick >= biteTellTicks + BiteLive + BiteHoldTicks)
                {
                    int retractTick = bitePoseTick - (biteTellTicks + BiteLive + BiteHoldTicks);
                    float retractT = MathHelper.Clamp(retractTick / (float)BiteRetractTicks, 0f, 1f);
                    curl *= 1f - MathHelper.SmoothStep(0f, 1f, retractT);
                }

                neckBend += 0.45f * curl;
                secondBend -= 0.85f * curl;
                thirdBend += 0.4f * curl;
                float drop = MathHelper.Clamp((bitePoseTick - biteTellTicks + 2f) / 6f, 0f, 1f);
                if (bitePoseTick >= biteTellTicks + BiteLive + BiteHoldTicks)
                {
                    int retractTick = bitePoseTick - (biteTellTicks + BiteLive + BiteHoldTicks);
                    float retractT = MathHelper.Clamp(retractTick / (float)BiteRetractTicks, 0f, 1f);
                    drop *= 1f - MathHelper.SmoothStep(0f, 1f, retractT);
                }

                neckBend -= 0.25f * drop;

                if (bitePoseTick < biteTellTicks)
                {
                    float windupAim = MathHelper.Clamp((bitePoseTick - (biteTellTicks - 10f)) / 10f, 0f, 1f);
                    biteAimWeight = MathHelper.SmoothStep(0f, 0.35f, windupAim);
                }
                else if (bitePoseTick < biteTellTicks + BiteLive)
                {
                    float lungeProgress = MathHelper.Clamp((bitePoseTick - biteTellTicks + 1f) / 7f, 0f, 1f);
                    float easedLunge = 1f - MathF.Pow(1f - lungeProgress, 3f);
                    currentBiteExtension = biteReach * easedLunge;
                    biteAimWeight = MathHelper.Lerp(0.35f, 1f, easedLunge);
                }
                else if (bitePoseTick < biteTellTicks + BiteLive + BiteHoldTicks)
                {
                    currentBiteExtension = biteReach;
                    biteAimWeight = 1f;
                }
                else
                {
                    int retractElapsed = bitePoseTick - (biteTellTicks + BiteLive + BiteHoldTicks);
                    float retractProgress = MathHelper.Clamp(retractElapsed / (float)BiteRetractTicks, 0f, 1f);
                    float retractFactor = 0.5f * (1f + MathF.Cos(MathHelper.Pi * retractProgress));
                    currentBiteExtension = biteReach * retractFactor;
                    biteAimWeight = retractFactor;
                }
            }

            if (charging)
            {
                // Rear the whole neck back 0.2 rad during the tell, then drop it 0.15 rad (head ~45px lower) to ram.
                // Negative rotation is head-down. The neck stays rigid, so no link seams are stressed.
                neckBend += 0.2f * chargeRear - 0.15f * chargeLow;
            }

            if (posingDescendingBite)
            {
                // Same S-coil as the bite's windup while crouching, then the same neck extension driven by leapSnap.
                neckBend += 0.45f * leapCrouch;
                secondBend -= 0.85f * leapCrouch;
                thirdBend += 0.4f * leapCrouch;
                float descendingReach = AirDivingBite ? 78f : biteReach;
                currentBiteExtension = descendingReach * leapSnap;
                biteAimWeight = leapSnap;
            }

            // 0..1 blend onto the aerial aim. Starts AirTell ticks before the flames (tick 0 for the normal aerial
            // breath, tick 66 of the retreat) and takes 20 ticks, so the neck and head come round together.
            float airAimClock = (StateTimer - (AirTellTicks - AirTell)) / 20f;
            float aimIn = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(airAimClock, 0f, 1f));

            if (AirBreathing)
            {
                // Bend the whole neck toward the aerial aim so the head is not the only part pointing at the target.
                // Rotation is negative-down, the opposite sign to the aim pitch. Neck1 takes 22%, Neck2 44%, Neck3 64%;
                // the head carries the full angle. Eases back out as the jaw closes.
                float airPitch = -MathF.Atan2(lockedAim.Y, lockedAim.X * Facing);
                float neckAim = airPitch * aimIn * (1f - breathClosing);
                neckBend += 0.22f * neckAim;
                secondBend += 0.22f * neckAim;
                thirdBend += 0.20f * neckAim;
            }

            // Share the bend outside the turn so the overlapping throat links stay aligned while attacking.
            Vector2 neckRoot = bones[0].Map(Parts[16].RestPivot);
            if (posingGroundBite)
            {
                // The lowered resting neck needs jaw clearance during the lunge. Raise the chain
                // 26px over the final eight tell ticks, keeping the drawn jaw and damage sockets together.
                float jawClearance = MathHelper.Clamp((bitePoseTick - biteTellTicks + 8f) / 8f, 0f, 1f);
                if (bitePoseTick >= biteTellTicks + BiteLive + BiteHoldTicks)
                {
                    int retractTick = bitePoseTick - (biteTellTicks + BiteLive + BiteHoldTicks);
                    float retractT = MathHelper.Clamp(retractTick / (float)BiteRetractTicks, 0f, 1f);
                    jawClearance *= 1f - MathHelper.SmoothStep(0f, 1f, retractT);
                }

                neckRoot.Y -= 26f * MathHelper.SmoothStep(0f, 1f, jawClearance);
            }
            else if (posingDescendingBite)
            {
                // Same 26px jaw clearance as the bite, so the open mouth clears the floor at touchdown.
                neckRoot.Y -= 26f * leapSnap;
            }
            else if (State == DrakeState.Breath)
            {
                // Give the opened jaw clearance over flat ground while the flame sweeps downward.
                float jawLift = MathHelper.Clamp((StateTimer - BreathTell + 16f) / 16f, 0f, 1f);
                neckRoot.Y -= 26f * MathHelper.SmoothStep(0f, 1f, jawLift) * (1f - breathClosing);
            }
            neckRoot.Y -= 34f * gripLift;

            bones[16] = new Bone(Parts[16].RestPivot, neckRoot, neckBend);
            bones[17] = new Bone(Parts[17].RestPivot, bones[16].Map(Parts[17].RestPivot), neckBend);
            bones[1] = new Bone(Parts[1].RestPivot, bones[17].Map(Parts[1].RestPivot), neckBend);
            bones[2] = new Bone(Parts[2].RestPivot, bones[1].Map(Parts[2].RestPivot), neckBend + secondBend);
            bones[3] = new Bone(Parts[3].RestPivot, bones[2].Map(Parts[3].RestPivot), neckBend + secondBend + thirdBend);
            Vector2 headPosition = bones[3].Map(Parts[15].RestPivot);
            float headAngle = 0.02f * MathF.Sin(phase - 0.8f);
            headAngle += 0.35f * headLead;
            if (posingGroundBite || State == DrakeState.Breath || AirBreathing)
            {
                Vector2 aim = lockedAim;
                float aimWeight = 1f - breathClosing;
                if (posingGroundBite)
                {
                    aim = bitePoseAim;
                    float windup = MathHelper.Clamp(bitePoseTick / 16f, 0f, 1f);
                    aimWeight = MathHelper.SmoothStep(0f, 1f, windup);
                    if (bitePoseTick >= biteTellTicks + BiteLive + BiteHoldTicks)
                    {
                        aimWeight = biteAimWeight;
                    }
                }

                if (State == DrakeState.Breath)
                {
                    aim = GetBreathAim();
                    float windup = MathHelper.Clamp(StateTimer / 20f, 0f, 1f);
                    aimWeight *= MathHelper.SmoothStep(0f, 1f, windup);
                }

                headAngle = -MathF.Atan2(aim.Y, aim.X * Facing);
                float upperLimit = 0.65f;
                if (State == DrakeState.Breath)
                {
                    upperLimit = 1.15f;
                }
                else if (AirBreathing)
                {
                    upperLimit = AirAimUpLimit;
                }
                float lowerLimit = AirBreathing ? -AirAimDownLimit : -1.15f;
                headAngle = MathHelper.Clamp(headAngle, lowerLimit, upperLimit);
                headAngle = MathHelper.Lerp(0.02f * MathF.Sin(phase - 0.8f), headAngle, aimWeight);

                if (State == DrakeState.RetreatBreath)
                {
                    // The retreat spends 66 ticks climbing before it aims; keep the head at rest until then.
                    headAngle = MathHelper.Lerp(0.02f * MathF.Sin(phase - 0.8f), headAngle, aimIn);
                }
            }

            if (posingDescendingBite)
            {
                // Tip the head onto the leap's fixed 0.65 rad downward aim as the jaws go out.
                float leapHeadAngle = -MathF.Atan2(lockedAim.Y, lockedAim.X * Facing);
                leapHeadAngle = MathHelper.Clamp(leapHeadAngle, -1.15f, 0.65f);
                headAngle = MathHelper.Lerp(headAngle, leapHeadAngle, leapSnap);
            }

            if (raining)
            {
                headAngle = MathHelper.Lerp(headAngle, 1.33f, rainHeadLift);
            }

            if ((posingGroundBite || posingDescendingBite) && (currentBiteExtension > 0.01f || biteAimWeight > 0.01f))
            {
                // Extend the neck chain 5–7 tiles along the locked aim vector and steer each link toward the bite angle.
                Vector2 sourceAim = new Vector2(-bitePoseAim.X * Facing, bitePoseAim.Y).SafeNormalize(-Vector2.UnitX);
                bones[16].Position += sourceAim * (currentBiteExtension * 0.12f);
                bones[17].Position += sourceAim * (currentBiteExtension * 0.25f);
                bones[1].Position += sourceAim * (currentBiteExtension * 0.42f);
                bones[2].Position += sourceAim * (currentBiteExtension * 0.65f);
                bones[3].Position += sourceAim * (currentBiteExtension * 0.88f);
                headPosition += sourceAim * currentBiteExtension;

                bones[1].Rotation = MathHelper.Lerp(bones[1].Rotation, headAngle * 0.55f, biteAimWeight);
                bones[2].Rotation = MathHelper.Lerp(bones[2].Rotation, headAngle * 0.75f, biteAimWeight);
                bones[3].Rotation = MathHelper.Lerp(bones[3].Rotation, headAngle * 0.92f, biteAimWeight);
            }

            if (holdingGrip || tossingGrip)
            {
                currentBiteExtension = Math.Max(currentBiteExtension, gripThrust);
                Vector2 gripOffset = new Vector2(-gripThrust, gripSway * -18f);
                bones[1].Position += gripOffset * 0.18f;
                bones[2].Position += gripOffset * 0.48f;
                bones[3].Position += gripOffset * 0.82f;
                headPosition += gripOffset;
                float liftedAngle = 0.14f + gripSway * 0.30f;
                headAngle = MathHelper.Lerp(headAngle, liftedAngle, gripLift);
            }

            bones[15] = new Bone(Parts[15].RestPivot, headPosition, headAngle);
            if (State == DrakeState.Turn)
            {
                PoseTurnNeck(turnClock);
            }

            if (State == DrakeState.Turn && turnBite)
            {
                // The thrust starts during the neck swing. Collision opens at tick 54, after the jaw starts to extend.
                int snapTick = StateTimer - TurnBiteLiveStart;
                float snapAmount = 0f;

                if (snapTick < 0)
                {
                    float anticipation = Math.Clamp((StateTimer - TurnBiteAimLockTick) / 14f, 0f, 1f);
                    snapAmount = MathHelper.SmoothStep(0f, 0.28f, anticipation);
                }
                else if (snapTick < BiteLive)
                {
                    float lungeProgress = MathHelper.Clamp((snapTick + 1f) / 7f, 0f, 1f);
                    snapAmount = 1f - 0.72f * MathF.Pow(1f - lungeProgress, 3f);
                }
                else if (snapTick >= BiteLive && snapTick < BiteLive + BiteHoldTicks)
                {
                    snapAmount = 1f;
                }
                else if (snapTick >= BiteLive + BiteHoldTicks)
                {
                    int retractElapsed = snapTick - (BiteLive + BiteHoldTicks);
                    float retractProgress = MathHelper.Clamp(retractElapsed / (float)BiteRetractTicks, 0f, 1f);
                    snapAmount = 0.5f * (1f + MathF.Cos(MathHelper.Pi * retractProgress));
                }

                // Tip the head at the locked spot first. The head already wears the new facing, so pitch is measured
                // against the new side (-turnFacing) and the sign undone by the body's current projection. Positive =
                // jaw down; capped at 1.3 rad (74 degrees) so a target under the chin gets a downward stab.
                Vector2 headWorld = ToWorld(bones[15].Position);
                Vector2 headToImpact = lockedImpact - headWorld;
                float snapPitch = MathF.Atan2(headToImpact.Y, headToImpact.X * -turnFacing);
                snapPitch = MathHelper.Clamp(snapPitch, -0.75f, 1.3f);
                float steering = Math.Min(1f, snapAmount * 1.45f);
                bones[15].Rotation = snapPitch * turnFacing * Facing * steering;

                // Then slide the neck so the snout tip (head source pixel 16,372) lands on the spot, up to
                // TurnBiteReach px. Re-solved every tick from the live pose, so the turn's own neck drift is absorbed.
                Vector2 snoutWorld = ToWorld(bones[15].Map(SnoutSocket));
                Vector2 lunge = lockedImpact - snoutWorld;
                float lungeLength = lunge.Length();

                if (lungeLength > TurnBiteReach)
                {
                    lunge *= TurnBiteReach / lungeLength;
                }

                lunge *= snapAmount;
                currentBiteExtension = lunge.Length();
                Vector2 snapOffset = new Vector2(lunge.X * Projection(), lunge.Y);
                // The neck root stays seated in the chest (weight 0) so no gap opens at the shoulder; the stretch is
                // taken by the outer links, which PreDraw pads with filler copies.
                bones[17].Position += snapOffset * 0.05f;
                bones[1].Position += snapOffset * 0.15f;
                bones[2].Position += snapOffset * 0.5f;
                bones[3].Position += snapOffset * 0.85f;
                bones[15].Position += snapOffset;

                // Jaw parts once the head has crossed, gapes for the snap, and half-closes through the retract.
                if (StateTimer >= TurnBiteLiveStart - 9 && snapTick < BiteLive + BiteHoldTicks)
                {
                    mouthRow = 2;
                }
                else if (StateTimer >= TurnBiteLiveStart - 32 && snapTick < BiteLive + BiteHoldTicks + BiteRetractTicks / 2)
                {
                    mouthRow = 1;
                }
            }

            bones[15].FrameRow = mouthRow;

            // The dorsal crest belongs to the shoulder. Flex it slightly around the torso pivot so
            // the long scales stay above the back instead of following a full neck rotation into the floor.
            bones[18] = new Bone(Parts[18].RestPivot, bones[0].Map(Parts[18].RestPivot), bones[0].Rotation);
            if (State == DrakeState.Turn)
            {
                float crestProgress = MathHelper.Clamp(turnClock / 32f, 0f, 1f);
                bones[18].Rotation += 0.08f * MathF.Sin(MathHelper.Pi * crestProgress);
            }

            bones[19] = bones[16];
            bones[20] = bones[17];
            bones[21] = bones[1];
            bones[22] = new Bone(Parts[22].RestPivot, bones[0].Map(Parts[22].RestPivot), bones[0].Rotation);

            for (int i = 0; i < tailJoints.Length; i++)
            {
                tailJoints[i] = TailRest[i] + Vector2.UnitY * bodyDrop;
            }

            if (State == DrakeState.TailSlam)
            {
                // Rotate each tail segment forward-kinematically from TailRest instead of pulling the
                // upward-curled TailTip with FABRIK IK, which flipped the tip upside down on impact.
                float floorDeltaAngle = MathHelper.Clamp((ToSource(lockedImpact).Y - SourceFloor.Y) / 260f, -0.22f, 0.22f);
                for (int i = 0; i < 4; i++)
                {
                    float raiseAngle = TailRaiseAngles[i];
                    float slamAngle = TailSlamAngles[i] + floorDeltaAngle;
                    float segmentAngle;
                    if (StateTimer < TailTell)
                    {
                        float raiseProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(StateTimer / 36f, 0f, 1f));
                        segmentAngle = raiseAngle * raiseProgress;
                    }
                    else if (StateTimer < TailTell + 9)
                    {
                        float slamProgress = MathHelper.Clamp((StateTimer - TailTell) / 9f, 0f, 1f);
                        slamProgress *= slamProgress;
                        segmentAngle = MathHelper.Lerp(raiseAngle, slamAngle, slamProgress);
                    }
                    else if (StateTimer < TailTell + TailLive + 4)
                    {
                        segmentAngle = slamAngle;
                    }
                    else
                    {
                        int recoverElapsed = StateTimer - (TailTell + TailLive + 4);
                        float recoverProgress = MathHelper.Clamp(recoverElapsed / 14f, 0f, 1f);
                        segmentAngle = slamAngle * (1f - MathHelper.SmoothStep(0f, 1f, recoverProgress));
                    }

                    Vector2 restSegment = TailRest[i + 1] - TailRest[i];
                    tailJoints[i + 1] = tailJoints[i] + restSegment.RotatedBy(segmentAngle);
                }
            }
            else
            {
                for (int i = 1; i < tailJoints.Length; i++)
                {
                    float weight = i / 4f;
                    tailJoints[i].Y += MathF.Sin(phase - weight * 1.3f) * weight * 20f;
                    tailJoints[i].X += MathF.Sin(phase - 0.9f) * weight * 5f;
                    if (banking)
                    {
                        float delay = 2f + 4f * weight;
                        float sweep = Math.Clamp((StateTimer - delay) / (AirTurnTicks - delay), 0f, 1f);
                        float drag = MathF.Sin(MathHelper.Pi * sweep) * weight;
                        tailJoints[i] += new Vector2(21f, 66f) * drag;
                    }
                }
            }

            if (State == DrakeState.Turn)
            {
                PoseTurnTail(turnClock);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 restDirection = TailRest[i + 1] - TailRest[i];
                    Vector2 posedDirection = tailJoints[i + 1] - tailJoints[i];
                    float rotation = posedDirection.ToRotation() - restDirection.ToRotation();
                    bones[4 + i] = new Bone(Parts[4 + i].RestPivot, tailJoints[i], rotation);
                }
            }

            if (banking)
            {
                // A 2D bank around the chest. The near and far wing pivots rotate in opposite directions.
                float bank = MathF.Sin(MathHelper.Pi * Math.Clamp(StateTimer / (float)AirTurnTicks, 0f, 1f));
                float tilt = -0.14f * bank;
                Vector2 pivot = bones[0].Position;
                for (int i = 0; i < bones.Length; i++)
                {
                    bones[i].Position = pivot + (bones[i].Position - pivot).RotatedBy(tilt);
                    bones[i].Rotation += tilt;
                }
                for (int i = 0; i < tailJoints.Length; i++)
                {
                    tailJoints[i] = pivot + (tailJoints[i] - pivot).RotatedBy(tilt);
                }

                bones[9].Rotation += 0.51f * bank;
                bones[10].Rotation -= 0.37f * bank;
            }

            // These source sockets include the 32px lowered head placement and still point to the same art pixels.
            Vector2 mouthSocket = SnoutSocket;
            if (State == DrakeState.Breath || AirBreathing || raining)
            {
                mouthSocket = BreathMouthSocket;
            }
            Mouth = ToWorld(bones[15].Map(mouthSocket));
            Jaw = ToWorld(bones[15].Map(new Vector2(76f, 368f)));
            TailBase = ToWorld(tailJoints[0]);
            TailJoint = ToWorld(tailJoints[3]);
            TailEnd = ToWorld(tailJoints[4]);
            BreathDirection = GetBreathAim();
        }

        private void PoseCollapse()
        {
            int landedTicks = StateTimer - collapseTouchdown;
            float fold = 0f;
            float settle = 0f;
            if (collapseTouchdown >= 0)
            {
                fold = MathHelper.SmoothStep(0f, 1f, Math.Clamp(landedTicks / (float)CollapseImpactTick, 0f, 1f));
                settle = MathHelper.SmoothStep(0f, 1f, Math.Clamp((landedTicks - 2f) / 18f, 0f, 1f));
            }

            Vector2 torso = Parts[0].RestPivot + new Vector2(8f, 49f) * fold;
            bones[0] = new Bone(Parts[0].RestPivot, torso, -0.08f * fold);
            for (int i = 8; i <= 14; i++)
            {
                Vector2 pivot = Parts[i].RestPivot;
                bones[i] = new Bone(pivot, bones[0].Map(pivot), bones[0].Rotation);
            }
            bones[18] = new Bone(Parts[18].RestPivot, bones[0].Map(Parts[18].RestPivot), bones[0].Rotation);
            bones[22] = new Bone(Parts[22].RestPivot, bones[0].Map(Parts[22].RestPivot), bones[0].Rotation);

            // Keep the soles at the ground as the legs switch to tucked frames under the falling chest.
            bones[13].FrameRow = 0;
            bones[14].FrameRow = 4;
            if (collapseTouchdown < 0 || landedTicks >= 7)
            {
                bones[13].FrameRow = 9;
                bones[14].FrameRow = 8;
            }
            if (collapseTouchdown >= 0)
            {
                for (int partIndex = 13; partIndex <= 14; partIndex++)
                {
                    Vector2 claw = GetClawPosition(partIndex);
                    float groundY = NPC.Bottom.Y;
                    if (TryGroundSurface(claw.X, groundY - 24f, groundY + 26f, out float localFloor))
                    {
                        groundY = localFloor;
                    }
                    bones[partIndex].Position.Y += (groundY - 10f - claw.Y) / ArtScale;
                }
            }

            bones[11].FrameRow = 0;
            bones[12].FrameRow = 7;
            float wingFold = MathF.Sin(MathHelper.Pi * Math.Clamp(landedTicks / 24f, 0f, 1f));
            bones[11].Rotation -= 0.21f * wingFold;
            bones[12].Rotation -= 0.16f * wingFold;

            Vector2 neckRoot = Parts[16].RestPivot;
            bones[16] = new Bone(neckRoot, bones[0].Map(neckRoot), bones[0].Rotation);
            for (int i = 1; i < NeckParts.Length; i++)
            {
                int partIndex = NeckParts[i];
                Vector2 rest = Parts[partIndex].RestPivot;
                Vector2 fallen = FallenNeck[i - 1];
                float worldX = ToWorld(fallen).X;
                float floorY = NPC.Bottom.Y;
                if (settle > 0f && TryGroundSurface(worldX, floorY - 30f, floorY + 32f, out float localFloor))
                {
                    fallen.Y += (localFloor - floorY) / ArtScale;
                }

                bones[partIndex] = new Bone(rest, Vector2.Lerp(rest, fallen, settle), 0f);
            }
            for (int i = 0; i < NeckParts.Length - 1; i++)
            {
                int partIndex = NeckParts[i];
                Bone next = bones[NeckParts[i + 1]];
                Vector2 restLink = next.Rest - bones[partIndex].Rest;
                Vector2 posedLink = next.Position - bones[partIndex].Position;
                bones[partIndex].Rotation = MathHelper.WrapAngle(posedLink.ToRotation() - restLink.ToRotation());
            }
            bones[15].Rotation = -0.12f * fold * (1f - settle);
            bones[15].FrameRow = landedTicks < CollapseImpactTick ? 1 : 0;
            bones[19] = bones[16];
            bones[20] = bones[17];
            bones[21] = bones[1];

            tailJoints[0] = bones[0].Map(TailRest[0]);
            for (int i = 1; i < tailJoints.Length; i++)
            {
                float delay = i * 2.7f;
                float tailFall = MathHelper.SmoothStep(0f, 1f, Math.Clamp((landedTicks - delay) / 19f, 0f, 1f));
                if (collapseTouchdown < 0)
                {
                    tailFall = 0f;
                }
                Vector2 fallen = FallenTail[i - 1];
                float worldX = ToWorld(fallen).X;
                float floorY = NPC.Bottom.Y;
                if (tailFall > 0f && TryGroundSurface(worldX, floorY - 30f, floorY + 32f, out float localFloor))
                {
                    fallen.Y += (localFloor - floorY) / ArtScale;
                }
                tailJoints[i] = Vector2.Lerp(TailRest[i], fallen, tailFall);
            }
            for (int i = 0; i < tailJoints.Length - 1; i++)
            {
                Vector2 restLink = TailRest[i + 1] - TailRest[i];
                Vector2 posedLink = tailJoints[i + 1] - tailJoints[i];
                float rotation = MathHelper.WrapAngle(posedLink.ToRotation() - restLink.ToRotation());
                bones[4 + i] = new Bone(Parts[4 + i].RestPivot, tailJoints[i], rotation);
            }

            Mouth = ToWorld(bones[15].Map(SnoutSocket));
            Jaw = ToWorld(bones[15].Map(new Vector2(76f, 368f)));
            TailBase = ToWorld(tailJoints[0]);
            TailJoint = ToWorld(tailJoints[3]);
            TailEnd = ToWorld(tailJoints[4]);
            currentBiteExtension = 0f;
        }

        private static float TurnChestAngle(float turnClock)
        {
            float chestRise = MathHelper.Clamp(turnClock / 18f, 0f, 1f);
            float chestLower = MathHelper.Clamp((turnClock - 18f) / 42f, 0f, 1f);
            chestRise = MathHelper.SmoothStep(0f, 1f, chestRise);
            chestLower = MathHelper.SmoothStep(0f, 1f, chestLower);
            return 0.06f * chestRise * (1f - chestLower);
        }

        private void PoseTurnNeck(float turnClock)
        {
            float neckLeadClock = TurnNeckLeadTick / (float)TurnStretchFactor;
            float neckSettleClock = TurnNeckTicks / (float)TurnStretchFactor;
            float neckRootClock = TurnNeckRootTicks / (float)TurnStretchFactor;
            // Keep the frame-72 neck milestone independent of the frame-50 body flip to preserve its pace.
            // Smootherstep and Hermite share a 0.6 pose-tick/tick speed at that milestone, avoiding a pause.
            const float flipNeckClock = 55.25364f;
            const float passageRate = 0.6f;
            float neckClock;
            if (turnClock < neckLeadClock)
            {
                float advance = MathHelper.Clamp(turnClock / neckLeadClock, 0f, 1f);
                float advanceSquared = advance * advance;
                float advanceCubed = advanceSquared * advance;
                float easePolynomial = 6f * advanceSquared - 15f * advance + 10f;
                float easedAdvance = advanceCubed * easePolynomial;
                float carriedClock = passageRate * turnClock;
                float remainingClock = flipNeckClock - passageRate * neckLeadClock;
                neckClock = carriedClock + remainingClock * easedAdvance;
            }
            else
            {
                float settleDuration = neckSettleClock - neckLeadClock;
                float release = MathHelper.Clamp((turnClock - neckLeadClock) / settleDuration, 0f, 1f);
                neckClock = MathHelper.Hermite(flipNeckClock, passageRate * settleDuration, neckSettleClock, 0f, release);
            }

            float neckChestAngle = TurnChestAngle(neckClock);
            Vector2 neckChestOffset = Parts[0].RestPivot - SourceFloor;
            Vector2 neckChestPosition = SourceFloor + neckChestOffset.RotatedBy(neckChestAngle);
            Bone neckChest = new Bone(Parts[0].RestPivot, neckChestPosition, neckChestAngle);

            // Overlap the swing and extension so the neck carries speed through the delayed torso flip.
            // New-facing world targets are independent of Facing; only the final approach eases to rest.
            float lead = MathHelper.Clamp(neckClock / 32f, 0f, 1f);
            lead = MathHelper.SmoothStep(0f, 1f, lead);
            Vector2 oldRootOffset = neckChest.Map(Parts[NeckParts[0]].RestPivot) - SourceFloor;
            oldRootOffset.X *= -turnFacing;
            // Slide the hidden attachment through the chest as it swivels, giving the snout enough
            // reach to emerge past the new-facing body before the torso flips, without stretching links.
            Vector2 rootLead = new Vector2(-turnFacing * 80f * lead, 0f);
            turnJoints[0] = NPC.Bottom + (oldRootOffset + rootLead) * ArtScale;
            int lastJoint = turnJoints.Length - 1;

            for (int i = 0; i < lastJoint; i++)
            {
                Vector2 restSegment = Parts[NeckParts[i + 1]].RestPivot - Parts[NeckParts[i]].RestPivot;
                float angle = MathHelper.Pi * NeckTurnAngles[i] * lead + neckChestAngle;
                Vector2 segment = restSegment.RotatedBy(angle);
                segment.X *= -turnFacing;
                turnJoints[i + 1] = turnJoints[i] + segment * ArtScale;
            }

            if (neckClock > 16f)
            {
                float settle = (neckClock - 16f) / (neckSettleClock - 16f);
                settle = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(settle, 0f, 1f));
                float rootSettle = (neckClock - 16f) / (neckRootClock - 16f);
                rootSettle = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(rootSettle, 0f, 1f));
                Vector2 newRootOffset = oldRootOffset;
                newRootOffset.X = -newRootOffset.X;
                Vector2 restingRoot = NPC.Bottom + newRootOffset * ArtScale;
                Vector2 headOffset = neckChest.Map(Parts[15].RestPivot) - SourceFloor;
                headOffset.X *= turnFacing;
                Vector2 restingHead = NPC.Bottom + headOffset * ArtScale;
                Vector2 root = Vector2.Lerp(turnJoints[0], restingRoot, rootSettle);
                Vector2 target = Vector2.Lerp(turnJoints[lastJoint], restingHead, settle);

                // Guide every joint toward the resting shape before solving its lengths. Keeping a
                // curled seed until the last tick leaves a different valid bend that would snap at release.
                for (int i = 0; i <= lastJoint; i++)
                {
                    Vector2 restingOffset = neckChest.Map(Parts[NeckParts[i]].RestPivot) - SourceFloor;
                    restingOffset.X *= turnFacing;
                    Vector2 restingJoint = NPC.Bottom + restingOffset * ArtScale;
                    turnJoints[i] = Vector2.Lerp(turnJoints[i], restingJoint, settle);
                }

                for (int pass = 0; pass < 10; pass++)
                {
                    turnJoints[lastJoint] = target;
                    for (int i = lastJoint - 1; i >= 0; i--)
                    {
                        float length = Vector2.Distance(Parts[NeckParts[i]].RestPivot, Parts[NeckParts[i + 1]].RestPivot) * ArtScale;
                        Vector2 direction = (turnJoints[i] - turnJoints[i + 1]).SafeNormalize(Vector2.UnitY);
                        turnJoints[i] = turnJoints[i + 1] + direction * length;
                    }

                    turnJoints[0] = root;
                    for (int i = 0; i < lastJoint; i++)
                    {
                        float length = Vector2.Distance(Parts[NeckParts[i]].RestPivot, Parts[NeckParts[i + 1]].RestPivot) * ArtScale;
                        Vector2 direction = (turnJoints[i + 1] - turnJoints[i]).SafeNormalize(Vector2.UnitY);
                        turnJoints[i + 1] = turnJoints[i] + direction * length;
                    }
                }

            }

            for (int i = 0; i < lastJoint; i++)
            {
                int partIndex = NeckParts[i];
                // Give each neck layer its own facing so the torso flip cannot mirror an already turned neck.
                float textureTurnClock = neckLeadClock;
                if (i < 3)
                {
                    textureTurnClock = 30f + i * 4f;
                }

                int textureFacing = -turnFacing;
                if (neckClock < textureTurnClock)
                {
                    textureFacing = turnFacing;
                }

                int horizontalSign = textureFacing * Facing;
                Vector2 restSegment = Parts[NeckParts[i + 1]].RestPivot - Parts[partIndex].RestPivot;
                restSegment.X *= horizontalSign;
                Vector2 posedSegment = ToSource(turnJoints[i + 1]) - ToSource(turnJoints[i]);
                float angle = posedSegment.ToRotation() - restSegment.ToRotation();
                bones[partIndex] = new Bone(Parts[partIndex].RestPivot, ToSource(turnJoints[i]), angle);
                bones[partIndex].HorizontalSign = horizontalSign;
            }

            int headFacing = turnFacing;
            if (StateTimer >= TurnHeadTick)
            {
                headFacing = -turnFacing;
            }

            bones[15] = new Bone(Parts[15].RestPivot, ToSource(turnJoints[lastJoint]), 0f);
            bones[15].HorizontalSign = headFacing * Facing;
        }

        private void PoseTurnTail(float turnClock)
        {
            // Carry the tail through the torso flip in world space. Stagger joint rotations from base
            // to tip over authored pose ticks 0–104. Every section starts rotating with the head; longer distal
            // durations give follow-through without a delayed start or an imperceptibly slow first phase.
            Vector2 canonicalRoot = tailJoints[0];
            Vector2 oldOffset = canonicalRoot - SourceFloor;
            oldOffset.X *= -turnFacing;
            Vector2 newOffset = oldOffset;
            newOffset.X = -newOffset.X;
            float rootProgress = MathHelper.Clamp(turnClock / 52f, 0f, 1f);
            rootProgress = MathHelper.SmoothStep(0f, 1f, rootProgress);
            Vector2 worldJoint = NPC.Bottom + Vector2.Lerp(oldOffset, newOffset, rootProgress) * ArtScale;
            tailJoints[0] = ToSource(worldJoint);
            Vector2 previousCanonical = canonicalRoot;

            for (int i = 0; i < 4; i++)
            {
                Vector2 nextCanonical = tailJoints[i + 1];
                Vector2 canonicalSegment = nextCanonical - previousCanonical;
                float restAngle = canonicalSegment.ToRotation();
                float turnAngle = MathHelper.WrapAngle(MathHelper.Pi - 2f * restAngle);
                if (i < 2 && turnAngle > 0f)
                {
                    // Coil above the back instead of sweeping the long proximal sections through the floor.
                    turnAngle -= MathHelper.TwoPi;
                }

                float duration = 80f + i * 8f;
                float progress = MathHelper.Clamp(turnClock / duration, 0f, 1f);
                float easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
                float remaining = 1f - progress;
                progress = easedProgress + 0.35f * progress * remaining * remaining;
                Vector2 worldSegment = canonicalSegment.RotatedBy(turnAngle * progress);
                worldSegment.X *= -turnFacing;
                Vector2 nextWorld = worldJoint + worldSegment * ArtScale;
                tailJoints[i + 1] = ToSource(nextWorld);

                // Keep each section's artwork independent of the torso's facing. Handedness changes
                // at that section's own coil midpoint, carrying dorsal spikes into the new resting profile.
                int textureFacing = turnFacing;
                if (progress >= 0.5f)
                {
                    textureFacing = -turnFacing;
                }

                int horizontalSign = textureFacing * Facing;
                Vector2 textureSegment = TailRest[i + 1] - TailRest[i];
                textureSegment.X *= horizontalSign;
                Vector2 sourceSegment = tailJoints[i + 1] - tailJoints[i];
                float rotation = sourceSegment.ToRotation() - textureSegment.ToRotation();
                bones[i + 4] = new Bone(Parts[i + 4].RestPivot, tailJoints[i], rotation);
                bones[i + 4].HorizontalSign = horizontalSign;
                worldJoint = nextWorld;
                previousCanonical = nextCanonical;
            }
        }

        public static Vector2 ClipLine(Vector2 start, Vector2 end)
        {
            Vector2 difference = end - start;
            int samples = Math.Max(1, (int)Math.Ceiling(difference.Length() / 6f));
            Vector2 previous = start;
            for (int i = 1; i <= samples; i++)
            {
                Vector2 current = Vector2.Lerp(start, end, i / (float)samples);
                if (Collision.SolidCollision(current - new Vector2(3f), 6, 6))
                {
                    return previous;
                }

                previous = current;
            }

            return end;
        }

        internal Vector2 GetClawPosition(int partIndex)
        {
            Bone leg = bones[partIndex];
            Part part = Parts[partIndex];
            Vector2[] clawPixels = partIndex == 13 ? NearClawPixels : FarClawPixels;
            Vector2 sourceClaw = part.RestPivot - part.Origin + clawPixels[leg.FrameRow];
            return ToWorld(leg.Map(sourceClaw));
        }

        public bool DamageGeometry(int kind, out Vector2 start, out Vector2 end, out Vector2 sweptStart, out Vector2 sweptEnd)
        {
            // Projectiles tick after NPC movement. Refresh the sockets after the engine's tile-collided translation.
            UpdatePose();
            start = Mouth;
            end = Mouth;
            sweptStart = Mouth;
            sweptEnd = Mouth;
            if (NPC.ai[3] != 0f || State == DrakeState.Collapse)
            {
                return false;
            }

            if (kind == NearFootAttack || kind == FarFootAttack)
            {
                Vector2 claw = GetClawPosition(kind == NearFootAttack ? 13 : 14);
                Vector2 groundProbe = claw + new Vector2(0f, 14f);
                if (!Supported(groundProbe) && !Supported(groundProbe + Vector2.UnitY * 12f))
                {
                    return false;
                }

                start = claw - Vector2.UnitX * 18f;
                end = claw + Vector2.UnitX * 18f;
                sweptStart = start;
                sweptEnd = end;
                return true;
            }

            bool isCurrentBite = kind == 0 && State == DrakeState.Bite
                || kind == GrabBiteAttack && State == DrakeState.GrabBite;
            bool biteLive = isCurrentBite && StateTimer >= BiteWindupTicks && StateTimer < BiteWindupTicks + BiteLive;
            bool turnBiteLive = kind == 3 && State == DrakeState.Turn && turnBite
                && StateTimer >= TurnBiteLiveStart && StateTimer < TurnBiteLiveStart + BiteLive;

            bool leapStillLive = leapLandTick < 0 || StateTimer < leapLandTick + LeapBiteLandLive;
            bool leapBiteLive = kind == 4 && State == DrakeState.LeapBite
                && StateTimer >= LeapTell + leapAirTicks - LeapBiteLiveLead && leapStillLive;
            bool diveBiteLive = kind == AirLandingBiteAttack && AirDivingBite && airBiteStart >= 0
                && StateTimer >= airBiteStart && StateTimer < airBiteStart + 25
                && (airTouchdown < 0 || StateTimer < airTouchdown + AirDiveBiteLandTicks);

            if (biteLive || turnBiteLive || leapBiteLive || diveBiteLive)
            {
                start = Jaw;
                end = ClipLine(Jaw, Mouth);
                sweptStart = previousMouth;
                if (Vector2.DistanceSquared(sweptStart, Mouth) > 16000f)
                {
                    sweptStart = Mouth;
                }

                sweptEnd = ClipLine(sweptStart, Mouth);
                return true;
            }

            int chargeRunTick = StateTimer - ChargeTell;

            if (kind == ChargeAttack && State == DrakeState.Charge && chargeRunTick >= 0 && chargeRunTick < chargeTicks)
            {
                // Sweep the ram from last tick's snout. The same carrier checks both posed claws.
                start = Mouth;
                end = ClipLine(Mouth, Mouth + new Vector2(-Facing * 130f, 0f));
                sweptStart = previousMouth;

                if (Vector2.DistanceSquared(sweptStart, Mouth) > 16000f)
                {
                    sweptStart = Mouth;
                }

                sweptEnd = ClipLine(sweptStart, Mouth);
                return true;
            }

            if (kind == 2 && State == DrakeState.TailSlam && StateTimer >= TailTell && StateTimer < TailTell + TailLive)
            {
                Vector2 tailMid = ToWorld(tailJoints[1]);
                start = tailMid;
                end = ClipLine(tailMid, TailJoint);
                sweptStart = previousTail;
                if (Vector2.DistanceSquared(sweptStart, TailJoint) > 40000f)
                {
                    sweptStart = TailJoint;
                }

                sweptEnd = ClipLine(sweptStart, TailJoint);
                return true;
            }

            return false;
        }

        private void TickPresentation(bool leapTouchdown)
        {
            if (visualSequence != sequence)
            {
                visualSequence = sequence;
                if (State == DrakeState.GrabHold)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/flesh-stab") with { Volume = 0.8f }, Mouth);
                    UsefulFunctions.ScreenShake(Mouth, 3f, 10, distanceFalloff: 440f);
                }
                else if (State == DrakeState.GrabToss)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/player-flick") with { Volume = 0.9f }, Mouth);
                    UsefulFunctions.ScreenShake(Mouth, 3f, 10, distanceFalloff: 440f);
                }
                else if (State == DrakeState.DestinedDeathRain)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl")
                    {
                        Volume = 0.75f,
                        Pitch = -0.15f
                    }, Mouth);
                }
                else if (State == DrakeState.Charge && StateTimer < ChargeTell)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/dragon-grunt") with { Volume = 0.8f }, Mouth);
                }
                else if (State == DrakeState.TailSlam)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f, Pitch = -0.6f }, TailBase);
                }
                else if (State == DrakeState.BiteChain && biteChainStep == 0 || State == DrakeState.Turn && turnBite
                    || State == DrakeState.LeapBite || State == DrakeState.GrabBite || AirDivingBite)
                {
                    // A short, higher snarl opens the bites and marks the turns that will snap.
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl")
                    {
                        Volume = 0.6f,
                        Pitch = 0.25f
                    }, Mouth);
                }
            }

            if (State == DrakeState.Takeoff && airBreathMode == AirBreathStrafe && StateTimer == StrafeStompTick)
            {
                // Strafe telegraph: the raised foot slams down. A thud, a little shake, and a low wave of dirt that
                // rises off the floor and rolls forward ~6 tiles in the direction the strafe will fly.
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.9f, Pitch = -0.3f }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 10, distanceFalloff: 500f);

                for (int i = 0; i < 40; i++)
                {
                    Vector2 stompOrigin = NPC.Bottom + new Vector2(Facing * Main.rand.NextFloat(-10f, 40f), -Main.rand.NextFloat(0f, 6f));
                    Vector2 stompDrift = new Vector2(Facing * Main.rand.NextFloat(1.5f, 6f), Main.rand.NextFloat(-3.2f, -0.8f));
                    Dust stompDust = Dust.NewDustPerfect(stompOrigin, DustID.Dirt, stompDrift, 0, default, Main.rand.NextFloat(1.1f, 1.7f));
                    stompDust.noGravity = i % 3 == 0;
                }
            }

            if (State == DrakeState.Charge)
            {
                Vector2 chargeNearFoot = GetClawPosition(13) + Vector2.UnitY * 6f;
                Vector2 chargeFarFoot = GetClawPosition(14) + Vector2.UnitY * 6f;
                int chargeRunTick = StateTimer - ChargeTell;

                // Tell: the near claw paws the ground (dirt flicked backwards every 5 ticks) and the one wing beat whooshes.
                if (chargeRunTick < 0 && StateTimer % 5 == 0)
                {
                    Vector2 paw = new Vector2(-Facing * Main.rand.NextFloat(2f, 4.5f), Main.rand.NextFloat(-2.6f, -0.8f));
                    Dust.NewDustPerfect(chargeNearFoot, DustID.Dirt, paw, 0, default, Main.rand.NextFloat(1f, 1.4f));
                    Dust.NewDustPerfect(chargeNearFoot, DustID.Dirt, paw * 0.6f, 0, default, Main.rand.NextFloat(0.8f, 1.1f));
                }

                if (StateTimer == 10)
                {
                    PlayWingFlap();
                }

                // Run: dirt thrown up behind both feet every other tick; footfall thuds come from the walk cycle above.
                if (chargeRunTick >= 0 && chargeRunTick < chargeTicks && StateTimer % 2 == 0)
                {
                    Vector2 spray = new Vector2(-Facing * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(-3f, -0.8f));
                    Dust.NewDustPerfect(chargeNearFoot, DustID.Dirt, spray, 0, default, Main.rand.NextFloat(1f, 1.5f));
                    Dust.NewDustPerfect(chargeFarFoot, DustID.Dirt, spray * 0.8f, 0, default, Main.rand.NextFloat(0.9f, 1.3f));
                }

                // Skid: claws plough forward, dirt and smoke pushed ahead of the feet, one grinding thud as it starts.
                if (chargeRunTick >= chargeTicks && chargeRunTick < chargeTicks + ChargeSkidTicks)
                {
                    if (chargeRunTick == chargeTicks)
                    {
                        SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.8f, Pitch = -0.4f }, NPC.Bottom);
                        UsefulFunctions.ScreenShake(NPC.Bottom, 2.5f, 8, distanceFalloff: 450f);
                    }

                    Vector2 plough = new Vector2(Facing * Main.rand.NextFloat(1.5f, 5f), Main.rand.NextFloat(-2.4f, -0.5f));
                    Dust.NewDustPerfect(chargeNearFoot, DustID.Dirt, plough, 0, default, Main.rand.NextFloat(1f, 1.5f));
                    Dust.NewDustPerfect(chargeFarFoot, DustID.Smoke, plough * 0.4f, 140, new Color(120, 100, 85), 1.2f);
                }
            }

            if (State == DrakeState.LeapBite)
            {
                // Feet: the planted near claw is NPC.Bottom; the far claw stands ~74px toward the head (source x 340).
                Vector2 nearFoot = NPC.Bottom;
                Vector2 farFoot = ToWorld(new Vector2(340f, SourceFloor.Y));
                int leapTick = StateTimer - LeapTell;

                // Tell: claws scrabble, one clod of dirt per foot every 4 ticks, thrown backwards.
                if (leapTick < 0 && StateTimer % 4 == 0)
                {
                    Vector2 scrabble = new Vector2(-Facing * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(-2.2f, -0.6f));
                    Dust.NewDustPerfect(nearFoot, DustID.Dirt, scrabble, 0, default, Main.rand.NextFloat(0.9f, 1.3f));
                    Dust.NewDustPerfect(farFoot, DustID.Dirt, scrabble * 0.8f, 0, default, Main.rand.NextFloat(0.9f, 1.3f));
                }

                // Launch: a kick of dirt behind both feet and a wing-beat whoosh.
                if (leapTick == 0)
                {
                    PlayWingFlap();

                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 kickOrigin = nearFoot;

                        if (i % 2 == 0)
                        {
                            kickOrigin = farFoot;
                        }

                        Vector2 kick = new Vector2(-Facing * Main.rand.NextFloat(1.5f, 5f), Main.rand.NextFloat(-3.5f, -0.8f));
                        Dust.NewDustPerfect(kickOrigin, DustID.Dirt, kick, 0, default, Main.rand.NextFloat(1f, 1.4f));
                    }
                }

                // Play the heavy slam on physical ground contact. Foot grit comes from the posed claws.
                if (leapTouchdown)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                    {
                        Volume = 0.55f,
                        Pitch = 0.05f
                    }, NPC.Bottom);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.45f }, Mouth);
                }
            }

            bool biteSnapTick = GroundBiting && StateTimer == BiteWindupTicks + 3;
            bool turnSnapTick = State == DrakeState.Turn && turnBite && StateTimer == TurnBiteLiveStart + 3;

            bool diveSnapTick = AirDivingBite && airBiteStart >= 0 && StateTimer == airBiteStart + 3;
            if (biteSnapTick || turnSnapTick || diveSnapTick)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.45f }, Mouth);
                UsefulFunctions.ScreenShake(Mouth, 2.2f, 8, distanceFalloff: 420f);
            }

            if (AirDivingSlam && StateTimer < AirDiveTell && StateTimer % 4 == 0)
            {
                // Mark the claw's locked landing point while the wings hold the drake in the air.
                Vector2 stompPoint = lockedImpact + new Vector2(-Facing * NearClawPlantX, -3f);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 start = stompPoint + new Vector2(Main.rand.NextFloat(-27f, 27f), 0f);
                    Vector2 drift = new Vector2(Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(-2.6f, -1.2f));
                    Dust.NewDustPerfect(start, DustID.Stone, drift, 50, default, 1.4f).noGravity = true;
                }
            }

            int waveTick = StateTimer - airTouchdown;
            if (AirDivingSlam && airTouchdown >= 0 && waveTick >= 0 && waveTick < SlamWaveTicks)
            {
                Vector2 claw = GetClawPosition(13);
                float bodyFloor = NPC.Bottom.Y;
                if (TryGroundSurface(claw.X, bodyFloor - 24f, bodyFloor + 32f, out float slamFloor))
                {
                    // Walk both fronts along local surfaces so the ripple climbs steps and stops at gaps.
                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        float lastY = slamFloor;
                        for (int step = 1; step <= waveTick + 1; step++)
                        {
                            float worldX = claw.X + direction * step * 15f;
                            if (!TryGroundSurface(worldX, lastY - 16f, lastY + 18f, out float surfaceY))
                            {
                                break;
                            }

                            lastY = surfaceY;
                            if (step == waveTick + 1)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    Vector2 position = new Vector2(worldX + Main.rand.NextFloat(-3f, 3f), surfaceY - 2f);
                                    Vector2 drift = new Vector2(direction * Main.rand.NextFloat(2f, 4.5f), Main.rand.NextFloat(-1.5f, -0.4f));
                                    Dust.NewDustPerfect(position, DustID.Dirt, drift, 25, default, Main.rand.NextFloat(0.95f, 1.4f));
                                }
                            }
                        }
                    }
                }
            }

            if (State == DrakeState.GrabBite && StateTimer < GrabTell + BiteLive && StateTimer % 3 == 0)
            {
                Vector2 glowPoint = Vector2.Lerp(Jaw, Mouth, Main.rand.NextFloat());
                Dust ember = Dust.NewDustPerfect(glowPoint, DustID.RedTorch, new Vector2(0f, -0.7f), 90, default, 0.9f);
                ember.noGravity = true;
                Lighting.AddLight(Mouth, 0.55f, 0.05f, 0.02f);
            }
            else if ((State == DrakeState.GrabHold && StateTimer % 7 == 0)
                || (State == DrakeState.GrabToss && StateTimer < 6))
            {
                Vector2 grip = ToWorld(bones[15].Map(GrabMouthSocket));
                Vector2 spray = new Vector2(Facing * Main.rand.NextFloat(1f, 4f), -1.4f);
                Dust.NewDustPerfect(grip, DustID.Blood, spray, 80, default, 1.1f);
            }

            if (State == DrakeState.DestinedDeathRain)
            {
                int volleyTick = StateTimer - RainTell;
                bool firing = volleyTick >= 0 && StateTimer <= RainLastWave && volleyTick % RainWaveInterval == 0;
                if (firing)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.4f }, Mouth);
                }

                if (firing || (StateTimer >= 24 && StateTimer < RainTell && StateTimer % 2 == 0))
                {
                    float heat = Math.Clamp((StateTimer - 24f) / (RainTell - 24f), 0f, 1f);
                    Lighting.AddLight(Mouth, 0.38f + 0.42f * heat, 0.05f, 0.08f);
                    int emberCount = firing ? 8 : 2;
                    for (int i = 0; i < emberCount; i++)
                    {
                        Vector2 spot = Mouth + Main.rand.NextVector2Circular(6f, 5f);
                        Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), -0.7f - 2.4f * heat);
                        int dustType = i % 3 == 0 ? DustID.Wraith : DustID.RedTorch;
                        Dust ember = Dust.NewDustPerfect(spot, dustType, velocity, 80, default, 0.8f + heat * 0.5f);
                        ember.noGravity = true;
                    }
                }
            }

            int tell = BreathTell;
            if (AirBreathing)
            {
                tell = AirTellTicks;
            }

            if ((State == DrakeState.Breath || AirBreathing) && StateTimer < tell)
            {
                // Reuse Oolacile's mouth smoulder and swell, including its orange accent palette.
                float progress = StateTimer / (float)tell;
                float emberScale = 0.45f;
                int emberCount = 1;
                if (progress >= 0.4f)
                {
                    float swell = (progress - 0.4f) / 0.6f;
                    emberScale = MathHelper.Lerp(0.6f, 1.9f, swell);
                    emberCount = 2;
                }

                for (int i = 0; i < emberCount; i++)
                {
                    Vector2 spot = Mouth + Main.rand.NextVector2Circular(3f, 3f);
                    Dust ember = Dust.NewDustPerfect(spot, DemonElement.FlameDust(DemonTier.Oolacile),
                        Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, emberScale);
                    ember.noGravity = true;
                }
                if (progress >= 0.4f && Main.rand.NextBool(4))
                {
                    DemonElement.SpawnBreathAccent(DemonTier.Oolacile, Mouth - new Vector2(4f), 8, 8, Vector2.Zero, emberScale);
                }
                if (State == DrakeState.AirBreath && airBreathMode == AirBreathStrafe && progress >= 0.28f && StateTimer % 3 == 0)
                {
                    Vector2 origin = Mouth + Main.rand.NextVector2Circular(6f, 4f);
                    Vector2 drift = new Vector2(-Facing * Main.rand.NextFloat(2.8f, 5.2f), Main.rand.NextFloat(-1.5f, -0.6f));
                    Dust soot = Dust.NewDustPerfect(origin, DustID.Smoke, drift, 125, new Color(92, 58, 46), 1.4f);
                    soot.noGravity = true;
                }
                Lighting.AddLight(Mouth, DemonElement.BreathLightColor(DemonTier.Oolacile) * (0.3f + emberScale * 0.4f));
            }

            int liveTicks = BreathLive;
            if (AirBreathing)
            {
                liveTicks = AirLiveTicks;
            }
            if ((State == DrakeState.Breath || AirBreathing) && StateTimer >= tell && StateTimer < tell + liveTicks)
            {
                int breathTick = StateTimer - tell;
                if (breathSoundSequence != sequence && breathTick < 12)
                {
                    // Give clients that resync just past release a short window to hear the onset once.
                    breathSoundSequence = sequence;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl")
                    {
                        Volume = 0.75f,
                        Pitch = -0.15f
                    }, Mouth);
                }
                else if (breathTick > 0 && breathTick % 12 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.7f, Pitch = -0.35f }, Mouth);
                }
            }
            if ((State == DrakeState.Breath || AirBreathing) && StateTimer == tell + liveTicks)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, Mouth);
            }

            if (State == DrakeState.TailSlam && StateTimer < TailTell && StateTimer % 3 == 0)
            {
                Dust mark = Dust.NewDustPerfect(lockedImpact + new Vector2(Main.rand.NextFloat(-18f, 18f), 0f),
                    DustID.Dirt, new Vector2(0f, -0.8f), 80, new Color(190, 126, 65), 0.9f);
                mark.noGravity = true;
            }

            if (State == DrakeState.TailSlam && StateTimer == TailTell + 9)
            {
                // Owl Father ground slam impact sound, screen shake, and fire/dirt ground burst along the slammed tail.
                Vector2 slamStart = ToWorld(tailJoints[2]);
                Vector2 slamEnd = TailJoint;
                Vector2 impactCenter = Vector2.Lerp(slamStart, slamEnd, 0.5f);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                {
                    Volume = 0.55f,
                    Pitch = 0.12f
                }, impactCenter);
                UsefulFunctions.ScreenShake(impactCenter, 3.75f, 11, distanceFalloff: 560f);

                for (int i = 0; i < 32; i++)
                {
                    Vector2 dustPosition = Vector2.Lerp(slamStart, slamEnd, Main.rand.NextFloat());
                    dustPosition.Y += Main.rand.NextFloat(-6f, 6f);
                    float horizontalSpeed = Main.rand.NextFloat(-6.5f, 6.5f);
                    Vector2 dustVelocity = new Vector2(horizontalSpeed, Main.rand.NextFloat(-4.2f, -0.5f));
                    int dustType = DustID.Torch;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.GoldFlame;
                    }
                    else if (i % 2 == 0)
                    {
                        dustType = DustID.OrangeTorch;
                    }

                    Dust fireDust = Dust.NewDustPerfect(dustPosition, dustType, dustVelocity, 80, default, Main.rand.NextFloat(1.3f, 2f));
                    fireDust.noGravity = true;
                }

                for (int i = 0; i < 26; i++)
                {
                    Vector2 dirtPosition = Vector2.Lerp(slamStart, slamEnd, Main.rand.NextFloat());
                    Vector2 dirtVelocity = new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-4.5f, -1f));
                    Dust.NewDustPerfect(dirtPosition, DustID.Dirt, dirtVelocity, 0, default, Main.rand.NextFloat(1.05f, 1.45f));
                }
            }

            int flapInterval = State == DrakeState.RetreatBreath ? 28 : 30;
            if (Airborne && StateTimer % flapInterval == 0)
            {
                PlayWingFlap();
            }

            // Retreat breath: every down-beat while still low (the tell and the first 40 climb ticks) blows a sheet of
            // dirt forward along the ground from under the wings, and the lift-off itself shakes the screen once.
            if (State == DrakeState.RetreatBreath && StateTimer < RetreatTell + 40 && StateTimer % 14 == 0)
            {
                for (int i = 0; i < 18; i++)
                {
                    Vector2 gustOrigin = NPC.Bottom + new Vector2(Main.rand.NextFloat(-150f, 150f), -Main.rand.NextFloat(0f, 10f));
                    Vector2 gust = new Vector2(Facing * Main.rand.NextFloat(2f, 6.5f), Main.rand.NextFloat(-2.4f, -0.4f));
                    Dust.NewDustPerfect(gustOrigin, DustID.Dirt, gust, 0, default, Main.rand.NextFloat(0.9f, 1.4f));
                }
            }

            if (State == DrakeState.RetreatBreath && StateTimer == RetreatTell)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 2.5f, 10, distanceFalloff: 500f);
            }
        }

        private void PlayWingFlap()
        {
            // Match The Rage's full-volume flaps, with pitch variation on every third beat.
            wingFlapCount = (wingFlapCount + 1) % 3;
            SoundStyle wingSound = SoundID.Item32 with { Volume = 1f, Pitch = 0f };
            if (wingFlapCount == 0)
            {
                wingSound = wingSound with { PitchVariance = 1f };
            }

            SoundEngine.PlaySound(wingSound, NPC.Center);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UpdatePose();
            int nearLeg = bones[13].FrameRow;
            int farLeg = bones[14].FrameRow;
            int wingNear = 11;
            int wingFar = 12;
            int nearWingRow = bones[11].FrameRow;
            int farWingRow = bones[12].FrameRow;
            bool fallingToCollapse = State == DrakeState.Collapse && collapseTouchdown < 0;
            int leapWingClock = StateTimer - (LeapTell - LeapWingTellTicks);
            bool leapWings = State == DrakeState.LeapBite && leapWingClock >= 0
                && (leapLandTick < 0 || StateTimer - leapLandTick < 4);
            if (Airborne || fallingToCollapse || leapWings)
            {
                wingNear = 9;
                wingFar = 10;
                int flap = StateTimer / 4 % WingCycle.Length;

                if (State == DrakeState.RetreatBreath)
                {
                    // Big, heavy beats for the retreat: 7 ticks per wing frame instead of 4 (one full beat per 56 ticks).
                    flap = StateTimer / 7 % WingCycle.Length;
                }

                nearWingRow = WingCycle[flap];
                farWingRow = WingCycle[(flap + 1) % WingCycle.Length];

                if (State == DrakeState.Takeoff && StateTimer < TakeoffCrouchTicks)
                {
                    nearWingRow = 2;
                    farWingRow = 2;
                }

                if (AirFeetExtended)
                {
                    nearWingRow = 2;
                    farWingRow = 2;
                }

                if (fallingToCollapse)
                {
                    nearWingRow = 2;
                    farWingRow = 1;
                }
                if (leapWings)
                {
                    // Raise through rows 2 and 1, then drive both wings into the downstroke on the launch tick.
                    int rowIndex = Math.Min(leapWingClock / 3, LeapWingRows.Length - 1);
                    int row = leapLandTick < 0 ? LeapWingRows[rowIndex] : 0;
                    nearWingRow = row;
                    farWingRow = row;
                }
            }

            int mouthRow = bones[15].FrameRow;

            if (State == DrakeState.Charge)
            {
                // Borrow the red unblockable silhouette from the weapon tell. Mask it with the same
                // posed sprite frames and draw every glow layer before any of the solid rig.
                int runTick = StateTimer - ChargeTell;
                float pulse = 0.85f + 0.15f * MathF.Sin(StateTimer * 0.21f);
                float heat = MathHelper.Lerp(0.35f, 0.6f, Math.Clamp(StateTimer / (float)ChargeTell, 0f, 1f));
                if (runTick >= 0)
                {
                    heat = 1f;
                }
                if (runTick >= chargeTicks)
                {
                    heat = 1f - Math.Clamp((runTick - chargeTicks) / (float)ChargeSkidTicks, 0f, 1f);
                }

                Color innerGlow = new Color(38, 9, 5, 0) * (heat * pulse);
                Color outerGlow = new Color(16, 3, 2, 0) * (heat * pulse);
                for (int i = 0; i < Parts.Length; i++)
                {
                    if (i == 9 || i == 10)
                    {
                        continue;
                    }

                    Bone bone = bones[i];
                    foreach (Vector2 direction in AttackTelegraphDraw.GlowDirections)
                    {
                        DrawPartAtBone(spriteBatch, i, bone.FrameRow, bone, screenPos - direction * 5f, outerGlow);
                        DrawPartAtBone(spriteBatch, i, bone.FrameRow, bone, screenPos - direction * 2.5f, innerGlow);
                    }
                }
            }

            // Shade the rear leg without fading it. Color multiplication also scales alpha,
            // so restore the body's alpha to keep the leg opaque over the background.
            Color farLegColor = drawColor * 0.85f;
            farLegColor.A = drawColor.A;
            DrawPart(spriteBatch, wingFar, farWingRow, screenPos, drawColor * 0.8f);
            DrawPart(spriteBatch, 14, farLeg, screenPos, farLegColor);
            DrawPart(spriteBatch, 13, nearLeg, screenPos, drawColor);
            DrawPart(spriteBatch, 8, 0, screenPos, drawColor);
            for (int i = 7; i >= 4; i--)
            {
                DrawPart(spriteBatch, i, 0, screenPos, drawColor);
            }

            DrawPart(spriteBatch, 22, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 19, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 20, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 21, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 16, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 17, 0, screenPos, drawColor);

            // When the neck stretches 5–7 tiles during Bite, draw interpolated Neck3/Neck2 links so no gaps open.
            // The turn bite stretches the same links. Its neck layers each carry their own mirror and a large turn
            // rotation, so a filler there copies the link it repeats instead of blending toward its neighbour.
            bool turnStretch = State == DrakeState.Turn && turnBite;
            bool neckStretching = GroundBiting || State == DrakeState.GrabHold || State == DrakeState.GrabToss
                || State == DrakeState.LeapBite || turnStretch || AirDivingBite;
            bool drawExtraNeckLinks = neckStretching && currentBiteExtension > 4f;
            if (drawExtraNeckLinks)
            {
                Bone extraTip = new Bone(Parts[3].RestPivot,
                    Vector2.Lerp(bones[3].Position, bones[15].Position, 0.5f),
                    MathHelper.Lerp(bones[3].Rotation, bones[15].Rotation, 0.5f));

                if (turnStretch)
                {
                    extraTip.Rotation = bones[3].Rotation;
                    extraTip.HorizontalSign = bones[3].HorizontalSign;
                }

                DrawPartAtBone(spriteBatch, 3, 0, extraTip, screenPos, drawColor);
            }

            DrawPart(spriteBatch, 3, 0, screenPos, drawColor);
            if (drawExtraNeckLinks)
            {
                Bone extraMid = new Bone(Parts[2].RestPivot,
                    Vector2.Lerp(bones[2].Position, bones[3].Position, 0.5f),
                    MathHelper.Lerp(bones[2].Rotation, bones[3].Rotation, 0.5f));

                if (turnStretch)
                {
                    extraMid.Rotation = bones[2].Rotation;
                    extraMid.HorizontalSign = bones[2].HorizontalSign;
                }

                DrawPartAtBone(spriteBatch, 2, 0, extraMid, screenPos, drawColor);
            }

            DrawPart(spriteBatch, 2, 0, screenPos, drawColor);
            if (drawExtraNeckLinks)
            {
                Bone extraBase = new Bone(Parts[2].RestPivot,
                    Vector2.Lerp(bones[1].Position, bones[2].Position, 0.5f),
                    MathHelper.Lerp(bones[1].Rotation, bones[2].Rotation, 0.5f));

                if (turnStretch)
                {
                    extraBase.Rotation = bones[2].Rotation;
                    extraBase.HorizontalSign = bones[2].HorizontalSign;
                }

                DrawPartAtBone(spriteBatch, 2, 0, extraBase, screenPos, drawColor);
            }

            DrawPart(spriteBatch, 1, 0, screenPos, drawColor);

            if (State == DrakeState.Turn)
            {
                DrawPart(spriteBatch, 15, mouthRow, screenPos, drawColor);
            }

            DrawPart(spriteBatch, 18, 0, screenPos, drawColor);
            DrawPart(spriteBatch, 0, 0, screenPos, drawColor);
            DrawPart(spriteBatch, wingNear, nearWingRow, screenPos, drawColor);
            if (State != DrakeState.Turn)
            {
                DrawPart(spriteBatch, 15, mouthRow, screenPos, drawColor);
            }

            // Orange breath telegraph lines are deliberately omitted per user specification; the neck
            // raise, jaw opening, growl, and mouth smoulder/ember swell serve as the breath tell.

            if (ModContent.GetInstance<tsorcRevampGameplayConfig>().DebugMode)
            {
                Utils.DrawBorderString(spriteBatch, DebugAttackLabel, NPC.Top - screenPos - new Vector2(90f, 40f),
                    Color.White, 0.7f);
            }

            return false;
        }

        private void DrawPart(SpriteBatch spriteBatch, int index, int row, Vector2 screenPos, Color color)
        {
            DrawPartAtBone(spriteBatch, index, row, bones[index], screenPos, color);
        }

        private void DrawPartAtBone(SpriteBatch spriteBatch, int index, int row, Bone bone, Vector2 screenPos, Color color)
        {
            Part part = Parts[index];
            part.Asset ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(HellkiteDrake)) + "_" + part.Name);
            Texture2D texture = part.Asset.Value;
            Rectangle frame = new Rectangle(0, Math.Clamp(row, 0, part.Rows - 1) * part.Height, part.Width, part.Height);
            Vector2 origin = part.Origin;
            float projection = Projection();
            SpriteEffects effects = SpriteEffects.None;
            float rotation = bone.Rotation * Math.Sign(projection);
            if (projection * bone.HorizontalSign < 0f)
            {
                effects = SpriteEffects.FlipHorizontally;
                origin.X = frame.Width - origin.X;
            }

            Vector2 scale = new Vector2(ArtScale * Math.Abs(projection), ArtScale);
            Vector2 drawPosition = ToWorld(bone.Position) - screenPos;
            spriteBatch.Draw(texture, drawPosition, frame, color, rotation, origin, scale, effects, 0f);
            if (index == 15 && State == DrakeState.GrabBite && StateTimer < GrabTell + BiteLive)
            {
                Vector2 eye = ToWorld(bone.Map(GrabEyeSocket)) - screenPos;
                float intensity = MathHelper.Lerp(0.52f, 1f, Math.Clamp(StateTimer / (float)GrabTell, 0f, 1f));
                float pulse = 0.88f + 0.12f * MathF.Sin(StateTimer * 0.38f);
                eyeFlare ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
                Texture2D gleam = eyeFlare.Value;
                Vector2 gleamScale = new Vector2(16f / gleam.Width, 10f / gleam.Height) * ArtScale * pulse;
                spriteBatch.Draw(gleam, eye, null, new Color(252, 22, 22, 0) * intensity, rotation,
                    gleam.Size() * 0.5f, gleamScale, SpriteEffects.None, 0f);
                Rectangle pixel = new Rectangle(0, 0, 1, 1);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, eye, pixel, new Color(255, 29, 30) * intensity, rotation,
                    new Vector2(0.5f), new Vector2(5f, 3f) * ArtScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, eye, pixel, new Color(255, 131, 109) * intensity, rotation,
                    new Vector2(0.5f), new Vector2(2f) * ArtScale, SpriteEffects.None, 0f);
            }
        }

        internal static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 difference = end - start;
            Rectangle pixel = new Rectangle(0, 0, 1, 1);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, pixel, color, difference.ToRotation(),
                new Vector2(0f, 0.5f), new Vector2(difference.Length(), width), SpriteEffects.None, 0f);
        }

        private sealed class Part
        {
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;
            public readonly Vector2 RestPivot;
            public readonly Vector2 Origin;
            public readonly int Rows;
            public Asset<Texture2D> Asset;

            public Part(string name, int width, int height, Vector2 restPivot, Vector2 origin, int rows)
            {
                Name = name;
                Width = width;
                Height = height;
                RestPivot = restPivot;
                Origin = origin;
                Rows = rows;
            }
        }

        private struct Bone
        {
            public Vector2 Rest;
            public Vector2 Position;
            public float Rotation;
            public int HorizontalSign;
            public int FrameRow;

            public Bone(Vector2 rest, Vector2 position, float rotation)
            {
                Rest = rest;
                Position = position;
                Rotation = rotation;
                HorizontalSign = 1;
                FrameRow = 0;
            }

            public readonly Vector2 Map(Vector2 source)
            {
                Vector2 offset = source - Rest;
                offset.X *= HorizontalSign;
                return Position + offset.RotatedBy(Rotation);
            }
        }
    }

    // Per-part hurtbox NPC bound to HellkiteDrake via NPC.realLife so each anatomical region
    // (Head = 0 def, Neck = 150 def, Tail = 180 def) applies its own defense to shared boss HP.
    public class HellkiteDrakeHitbox : ModNPC
    {
        private static readonly int[] SlotPartIndices = { 15, 1, 2, 3, 4, 5, 6, 7 };
        private static readonly int[] SlotWidths = { 96, 76, 74, 68, 78, 86, 82, 72 };
        private static readonly int[] SlotHeights = { 84, 76, 74, 68, 68, 64, 72, 68 };

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Content.Projectiles.InvisibleNothingProj));

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryModifiers);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 72;
            NPC.height = 72;
            NPC.aiStyle = -1;
            NPC.lifeMax = 475000;
            NPC.defense = HellkiteDrake.NeckDefense;
            NPC.damage = 0;
            NPC.knockBackResist = 0f;
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit13;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override bool PreKill()
        {
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 && !NPC.dontTakeDamage)
            {
                bool bareHead = (int)NPC.ai[1] == 0;
                HellkiteDrake.EmitHitDust(NPC.Center, hit, bareHead);
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }

        public override void AI()
        {
            int ownerIndex = (int)NPC.ai[0];
            int slot = (int)NPC.ai[1];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || slot < 0 || slot >= SlotPartIndices.Length)
            {
                NPC.active = false;
                return;
            }

            NPC owner = Main.npc[ownerIndex];
            if (!owner.active || owner.ModNPC is not HellkiteDrake drake)
            {
                NPC.active = false;
                return;
            }

            drake.partHitboxes[slot] = NPC.whoAmI;
            SyncToDrake(drake, slot);
        }

        internal void SyncToDrake(HellkiteDrake drake, int slot)
        {
            NPC owner = drake.NPC;
            NPC.realLife = owner.whoAmI;
            NPC.lifeMax = owner.lifeMax;
            NPC.life = owner.life;
            NPC.dontTakeDamage = owner.dontTakeDamage;
            NPC.timeLeft = 750;

            int desiredWidth = SlotWidths[slot];
            int desiredHeight = SlotHeights[slot];
            if (NPC.width != desiredWidth || NPC.height != desiredHeight)
            {
                NPC.width = desiredWidth;
                NPC.height = desiredHeight;
            }

            if (slot == 0)
            {
                NPC.defense = HellkiteDrake.HeadDefense;
            }
            else if (slot <= 3)
            {
                NPC.defense = HellkiteDrake.NeckDefense;
            }
            else
            {
                NPC.defense = HellkiteDrake.TailDefense;
            }

            int partIndex = SlotPartIndices[slot];
            NPC.Center = drake.GetPartWorldCenter(partIndex);
            NPC.velocity = Vector2.Zero;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            int ownerIndex = (int)NPC.ai[0];
            int slot = (int)NPC.ai[1];
            if (slot > 0 && ownerIndex >= 0 && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active && Main.npc[ownerIndex].ModNPC is HellkiteDrake drake)
            {
                // Yield to the 0-defense head hitbox when a projectile touches both head and neck.
                int headIndex = drake.partHitboxes[0];
                if (headIndex >= 0 && headIndex < Main.maxNPCs && Main.npc[headIndex].active
                    && projectile.Colliding(projectile.Hitbox, Main.npc[headIndex].Hitbox))
                {
                    return false;
                }
            }

            return null;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            int ownerIndex = (int)NPC.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is HellkiteDrake drake)
            {
                drake.ShareHitImmunity(player.whoAmI, null);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            int ownerIndex = (int)NPC.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is HellkiteDrake drake)
            {
                drake.ShareHitImmunity(projectile.owner, projectile);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }
    }

    public class HellkiteDrakeAttack : ModProjectile
    {
        private readonly bool[] hitPlayers = new bool[Main.maxPlayers];
        private Vector2 start;
        private Vector2 end;
        private Vector2 sweptStart;
        private Vector2 sweptEnd;
        private Vector2 chargeNearClaw;
        private Vector2 chargeFarClaw;
        private int grabBiteTarget = -1;
        private bool live;
        private bool ending;
        private bool released;

        // Collision follows the posed jaw or tail; the visible sprite is drawn by the NPC.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Content.Projectiles.InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            int kind = (int)Projectile.ai[1];
            if (kind == HellkiteDrake.GrabThrowAttack)
            {
                int throwTarget = (int)Projectile.ai[2];
                live = throwTarget >= 0 && throwTarget < Main.maxPlayers
                    && Main.player[throwTarget].active && !Main.player[throwTarget].dead;
                if (!live)
                {
                    Projectile.Kill();
                    return;
                }

                Player tossed = Main.player[throwTarget];
                if (!released)
                {
                    CooldownSlot = ImmunityCooldownID.Bosses;
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, 5);
                    Projectile.width = tossed.width + 8;
                    Projectile.height = tossed.height + 8;
                    if (Main.netMode != NetmodeID.MultiplayerClient || throwTarget == Main.myPlayer)
                    {
                        tsorcRevampPlayer modPlayer = tossed.GetModPlayer<tsorcRevampPlayer>();
                        modPlayer.ImpaleFreezeTimer = 0;
                        modPlayer.ImpaleDrawRotation = 0f;
                        tossed.velocity = Projectile.velocity;
                    }
                    released = true;
                }

                // Artorias uses a separate hit at release. Track only this victim until their client resolves it.
                Projectile.Center = tossed.Center;
                return;
            }

            bool footfall = kind == HellkiteDrake.NearFootAttack || kind == HellkiteDrake.FarFootAttack;
            if (footfall)
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, HellkiteDrake.FootfallTicks);
            }

            live = false;
            if (!ending && ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC owner = Main.npc[ownerIndex];
                if (owner.active && owner.ModNPC is HellkiteDrake drake
                    && (footfall || drake.Sequence == (int)Projectile.ai[2]))
                {
                    live = drake.DamageGeometry(kind, out Vector2 posedStart, out Vector2 posedEnd,
                        out Vector2 posedSweptStart, out Vector2 posedSweptEnd);
                    if (live)
                    {
                        grabBiteTarget = drake.GrabTarget;
                        start = posedStart;
                        end = posedEnd;
                        sweptStart = posedSweptStart;
                        sweptEnd = posedSweptEnd;
                        if (kind == HellkiteDrake.ChargeAttack)
                        {
                            chargeNearClaw = drake.GetClawPosition(13);
                            chargeFarClaw = drake.GetClawPosition(14);
                        }
                    }
                }
            }

            if (!live && !ending)
            {
                // A carrier can arrive one packet before its owner's transition. Wait briefly with damage disabled.
                if (!released && Projectile.timeLeft > 168)
                {
                    return;
                }

                ending = true;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 12);
            }

            if (!ending)
            {
                released = true;
                Vector2 minimum = Vector2.Min(Vector2.Min(start, end), Vector2.Min(sweptStart, sweptEnd));
                Vector2 maximum = Vector2.Max(Vector2.Max(start, end), Vector2.Max(sweptStart, sweptEnd));
                if (kind == HellkiteDrake.ChargeAttack)
                {
                    minimum = Vector2.Min(minimum, Vector2.Min(chargeNearClaw, chargeFarClaw));
                    maximum = Vector2.Max(maximum, Vector2.Max(chargeNearClaw, chargeFarClaw));
                }
                Projectile.position = minimum - new Vector2(32f);
                Projectile.width = Math.Max(12, (int)(maximum.X - minimum.X) + 64);
                Projectile.height = Math.Max(12, (int)(maximum.Y - minimum.Y) + 64);
            }
        }

        public override bool? CanDamage()
        {
            return live && !ending;
        }

        public override bool CanHitPlayer(Player target)
        {
            int kind = (int)Projectile.ai[1];
            if (kind == HellkiteDrake.GrabBiteAttack && target.whoAmI != grabBiteTarget)
            {
                return false;
            }

            if (kind == HellkiteDrake.GrabThrowAttack && target.whoAmI != (int)Projectile.ai[2])
            {
                return false;
            }

            return !hitPlayers[target.whoAmI];
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if ((int)Projectile.ai[1] == HellkiteDrake.GrabThrowAttack)
            {
                modifiers.Knockback *= 0f;
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    info.Dodgeable = false;
                };
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            hitPlayers[target.whoAmI] = true;
            if ((int)Projectile.ai[1] != HellkiteDrake.GrabBiteAttack)
            {
                return;
            }

            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active
                || Main.npc[ownerIndex].ModNPC is not HellkiteDrake drake)
            {
                return;
            }

            int biteSequence = (int)Projectile.ai[2];
            if (Main.netMode == NetmodeID.MultiplayerClient && target.whoAmI == Main.myPlayer)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.ReportDrakeGrabBite);
                packet.Write((short)ownerIndex);
                packet.Write(biteSequence);
                packet.Send();
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                drake.TryGrabPlayer(target, biteSequence);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int kind = (int)Projectile.ai[1];
            if (kind == HellkiteDrake.GrabThrowAttack)
            {
                return projHitbox.Intersects(targetHitbox);
            }

            float width = 26f;
            if (kind == 2)
            {
                width = 28f;
            }
            else if (kind == HellkiteDrake.ChargeAttack)
            {
                // Ground charge: the whole lowered head, not a jaw-sized line.
                width = 60f;
            }

            float point = 0f;
            bool primary = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                start, end, width, ref point);
            bool swept = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                sweptStart, sweptEnd, width, ref point);
            Vector2 targetCenter = targetHitbox.Center.ToVector2();
            if ((primary || swept) && Collision.CanHitLine(start, 1, 1, targetCenter, 1, 1))
            {
                return true;
            }

            if (kind == HellkiteDrake.ChargeAttack)
            {
                // Each claw is tracked separately so empty space between the head and feet stays safe.
                Vector2 size = new Vector2(64f, 28f);
                bool nearHit = Utils.CenteredRectangle(chargeNearClaw, size).Intersects(targetHitbox)
                    && Collision.CanHitLine(chargeNearClaw, 1, 1, targetCenter, 1, 1);
                bool farHit = Utils.CenteredRectangle(chargeFarClaw, size).Intersects(targetHitbox)
                    && Collision.CanHitLine(chargeFarClaw, 1, 1, targetCenter, 1, 1);
                return nearHit || farHit;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            int kind = (int)Projectile.ai[1];
            if (Main.dedServ || !released || kind == HellkiteDrake.NearFootAttack || kind == HellkiteDrake.FarFootAttack
                || kind == HellkiteDrake.GrabThrowAttack)
            {
                return;
            }

            for (int i = 0; i < 16; i++)
            {
                Vector2 position = Vector2.Lerp(start, end, Main.rand.NextFloat());
                Dust ember = Dust.NewDustPerfect(position, DustID.Torch,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 0.75f);
                ember.noGravity = true;
            }
        }
    }

    // The retreat breath's puff: the same flame, living long enough at 12 px/tick to carry 50 tiles (804px).
    // Growth, the 14-tick fade, tile collision and the death gutter all scale off PuffLifetimeTicks in DemonBreath.
    public class HellkiteDrakeLongFlame : HellkiteDrakeFlame
    {
        protected override int PuffLifetimeTicks => HellkiteDrake.LongFlameLifetime;
    }

    // Reuse Oolacile's flame sprite, orange dust, On Fire hit and complete projectile lifecycle.
    // ai[0] is the Oolacile palette; ai[1] is the owning Drake's NPC slot.
    public class HellkiteDrakeFlame : DemonBreath
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 36;
            Projectile.height = 36;
        }

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[1];
            bool ownerValid = ownerIndex >= 0 && ownerIndex < Main.maxNPCs;
            if (ownerValid)
            {
                NPC owner = Main.npc[ownerIndex];
                ownerValid = owner.active && owner.ModNPC is HellkiteDrake drake && drake.State != HellkiteDrake.DrakeState.Collapse;
            }

            if (!ownerValid)
            {
                Projectile.hostile = false;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 14);
            }

            base.AI();
        }

    }

    public class HellkiteDrakeDeathFlame : ModProjectile
    {
        public const int HitHeight = 34;
        private const int HitWidth = 28;
        private const int AirLifetime = 132;
        private const int GroundLifetime = 3 * 60;
        private const int GroundDamage = 50;
        private const float Gravity = 0.5f;
        private const float GroundMode = 1f;
        private const float GroundDrawSink = 4f;

        // Trim the transparent margins of the three full 86x82 blaze frames for the ground base and wisps.
        private static readonly Rectangle[] FlameFrames =
        {
            new Rectangle(22, 2 * 82 + 22, 38, 36),
            new Rectangle(14, 3 * 82 + 18, 56, 48),
            new Rectangle(6, 4 * 82 + 12, 68, 56),
        };
        private static readonly Color FlameCoreTint = new Color(255, 103, 125);
        private static readonly Color FlameWispTint = new Color(247, 70, 105);
        private static readonly Color SootTint = new Color(34, 10, 17);
        private static Asset<Texture2D> flameCoreTexture;

        private bool Grounded => Projectile.ai[1] == GroundMode;
        private int Age => (int)Projectile.ai[0];
        private int Lane => (int)Projectile.ai[2];
        private int Lifetime => Grounded ? GroundLifetime : AirLifetime;
        private Vector2? floorImpact;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        // AI applies gravity before the engine moves the projectile, so sum steps 1 through travelTicks.
        internal static Vector2 LaunchVelocity(Vector2 muzzle, Vector2 landing, int travelTicks)
        {
            Vector2 displacement = landing - muzzle;
            float fallSpeed = Gravity * (travelTicks + 1) * 0.5f;
            return displacement / travelTicks - new Vector2(0f, fallSpeed);
        }

        public override void SetDefaults()
        {
            Projectile.width = HitWidth;
            Projectile.height = HitHeight;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = GroundLifetime + 2;
        }

        public override bool ShouldUpdatePosition() => !Grounded;

        public override bool? CanDamage() => Age >= 4 && Age < Lifetime;

        public override void AI()
        {
            // ai[0] carries age to clients joining during a volley or a ground flame's fade.
            if (Age >= Lifetime)
            {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0]++;

            Projectile.tileCollide = !Grounded;
            Projectile.hide = Grounded;
            if (!Grounded)
            {
                Projectile.velocity.Y += Gravity;
            }

            if (Main.dedServ)
            {
                return;
            }

            float fade = MathHelper.Clamp((Lifetime - Age) / 14f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.56f, 0.055f, 0.08f) * fade);

            // Red sparks leave the boundary quickly. Larger dark motes lag behind the lob or curl away from the ground.
            int emberInterval = Grounded ? 3 : 2;
            int sootInterval = Grounded ? 5 : 3;
            int dustClock = Age + Lane;
            Vector2 fireBase = Grounded ? Projectile.Bottom - Vector2.UnitY * 9f : Projectile.Center;
            if (dustClock % emberInterval == 0)
            {
                Vector2 position = fireBase + Main.rand.NextVector2Circular(10f, 7f);
                Vector2 drift = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-3f, -1.2f));
                Dust ember = Dust.NewDustPerfect(position, DustID.RedTorch, drift + Projectile.velocity * 0.14f,
                    70, default, Main.rand.NextFloat(0.7f, 1.25f) * fade);
                ember.noGravity = true;
            }

            if (dustClock % sootInterval == 0)
            {
                Vector2 position = fireBase + Main.rand.NextVector2Circular(10f, 7f);
                Vector2 drift = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.5f, -0.45f));
                Dust soot = Dust.NewDustPerfect(position, DustID.Wraith, drift - Projectile.velocity * 0.09f,
                    100, SootTint, Main.rand.NextFloat(0.9f, 1.4f) * fade);
                soot.noGravity = true;
                soot.noLight = true;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitFloor = !Grounded && oldVelocity.Y > 0f && Projectile.velocity.Y < oldVelocity.Y;
            if (hitFloor)
            {
                // The hook runs before movement. Start at the engine's resolved impact, including slope corrections.
                Vector2 contact = Projectile.Bottom + Projectile.velocity;

                // A wide shot can clip the higher side of a step. Check for floor under the center first,
                // then under the edges. The solver scans nearby rows, so sweep 24px in two 12px steps.
                const int footingStep = 12;
                Vector2 footingSweep = Vector2.UnitY * footingStep;
                for (int sample = 0; sample < 5; sample++)
                {
                    int step = (sample + 1) / 2 * 7;
                    if (sample % 2 != 0)
                    {
                        step = -step;
                    }

                    Vector2 footingProbe = contact + new Vector2(step - 1f, -footingStep - 2f);
                    Vector2 footing = Collision.TileCollision(footingProbe, footingSweep, 2, footingStep);
                    if (footing.Y >= footingStep)
                    {
                        footingProbe += footingSweep;
                        footing = Collision.TileCollision(footingProbe, footingSweep, 2, footingStep);
                    }
                    if (footing.Y >= footingStep)
                    {
                        continue;
                    }

                    contact = footingProbe + new Vector2(1f, footingStep + footing.Y);
                    break;
                }

                floorImpact = contact;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 fireCenter = contact - Vector2.UnitY * (HitHeight * 0.5f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), fireCenter, Vector2.Zero,
                        Type, GroundDamage, 0f, Main.myPlayer, ai1: GroundMode, ai2: Lane);
                }
            }
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), DemonElement.DestinedDeathTicks);
            if (!Grounded)
            {
                // This runs on the victim. The global projectile hook relays the kill to the server.
                Projectile.Kill();
            }
            else if (!Main.dedServ)
            {
                EmitBurst(target.Center, 8, 1f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Grounded || Age >= AirLifetime)
            {
                Vector2 gutter = Grounded ? Projectile.Bottom - Vector2.UnitY * 6f : Projectile.Center;
                EmitBurst(gutter, 5, 0.6f);
                return;
            }

            Vector2 origin = floorImpact ?? Projectile.Center;
            float rise = floorImpact.HasValue ? 1.6f : 0.3f;
            EmitBurst(origin, 16, rise);
        }

        private static void EmitBurst(Vector2 origin, int count, float rise)
        {
            for (int i = 0; i < count; i++)
            {
                bool soot = i % 4 == 0;
                int dustType = soot ? DustID.Wraith : DustID.RedTorch;
                Vector2 velocity = Main.rand.NextVector2Circular(3.4f, 2.2f) - Vector2.UnitY * rise;
                Color tint = soot ? SootTint : Color.White;
                Dust mote = Dust.NewDustPerfect(origin, dustType, velocity, 85, tint, Main.rand.NextFloat(0.8f, 1.35f));
                mote.noGravity = true;
                mote.noLight = soot;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Grounded)
            {
                behindNPCsAndTiles.Add(index);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D blaze = TextureAssets.Projectile[Type].Value;
            // The Ancient Demon's breath puff supplies the compact fire core. The blaze frames supply torn edges.
            flameCoreTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBreath)));
            Texture2D core = flameCoreTexture.Value;
            Rectangle coreFrame = core.Bounds;
            float grow = MathHelper.Clamp(Age / 7f, 0.2f, 1f);
            float fade = MathHelper.Clamp((Lifetime - Age) / 14f, 0f, 1f);
            bool grounded = Grounded;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 ground = Projectile.Bottom - Main.screenPosition + Vector2.UnitY * GroundDrawSink;
            Vector2 forward = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            int spin = Lane % 2 == 0 ? -1 : 1;

            if (grounded)
            {
                // Rotate the rear flame around its own center. The tile pass clips the buried edge.
                Rectangle baseFrame = FlameFrames[2];
                Vector2 basePosition = ground - Vector2.UnitY * (9f * grow);
                Vector2 baseScale = new Vector2((HitWidth - 4f) / baseFrame.Width, (HitHeight - 4f) / baseFrame.Height) * grow;
                float baseSpin = Lane * 2.399963f - Age * 0.17f * spin;
                Main.EntitySpriteDraw(blaze, basePosition, baseFrame, FlameWispTint * (fade * 0.85f),
                    baseSpin, baseFrame.Size() * 0.5f, baseScale, SpriteEffects.None, 0f);
            }

            // Stagger four particle lives per flame. Each wisp spins, grows, and fades before recycling,
            // while its lower opacity distinguishes the edges from the persistent damaging core.
            for (int i = 0; i < 4; i++)
            {
                int life = (grounded ? 23 : 17) + i * 4;
                float phase = ((Age + Lane * 3 + i * 11) % life) / (float)life;
                float opacity = MathF.Sin(phase * MathHelper.Pi) * fade;
                float angle = Age * 0.075f * spin + i * 2.399963f + Lane;
                float phaseSpin = angle + phase * 2.6f * spin;
                float width = (12f + i * 3.5f) * MathHelper.Lerp(0.55f, 1.15f, phase) * grow;
                Vector2 wispPosition;
                if (grounded)
                {
                    float sideDrift = MathF.Sin(angle) * MathHelper.Lerp(3f, 11f, phase);
                    float rise = MathHelper.Lerp(6f, 43f, phase);
                    wispPosition = ground + new Vector2(sideDrift, -rise) * grow;
                }
                else
                {
                    Vector2 orbit = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.85f);
                    Vector2 wake = orbit * MathHelper.Lerp(6f, 12f, phase) - forward * phase * 12f;
                    wispPosition = center + wake * grow;
                }

                bool denseWisp = i % 2 == 0;
                Texture2D wispTexture = denseWisp ? core : blaze;
                Rectangle frame = denseWisp ? coreFrame : FlameFrames[Math.Min(2, (int)(phase * 3f))];
                float scale = width / frame.Width;
                Main.EntitySpriteDraw(wispTexture, wispPosition, frame, FlameWispTint * (opacity * 0.68f),
                    phaseSpin, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            float coreSpin = Lane * 2.399963f + Age * 0.13f * spin;
            float coreWidth = HitWidth;
            if (grounded)
            {
                center = ground - Vector2.UnitY * (12f * grow);
                coreWidth *= 0.78f;
            }
            float coreScale = coreWidth / coreFrame.Width * grow;
            Main.EntitySpriteDraw(core, center, coreFrame, FlameCoreTint * fade, coreSpin,
                coreFrame.Size() * 0.5f, coreScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
