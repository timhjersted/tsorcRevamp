using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic;
using tsorcRevamp.Content.Projectiles.Enemy.IceGigas;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    //
    // Frost terrain-controller giant, the Gigas's cold sibling. Where Gigas is a caster whose magic
    // arrives at your position, Ice Gigas makes the ARENA hostile — persistent ground spikes, freeze
    // zones, shatter traps — and makes YOU slow instead of being fast itself.
    //
    // Poise contract (mirrors Gigas): boss-tier 120f poise; every telegraph is hyper-armored from its
    // first frame EXCEPT Absolute Zero, the long channel, which is staggerable for its first two thirds.
    //
    // Combo chains: some attacks roll into a designated follow-up with a HALVED telegraph. Above 50%
    // health a combo is at most 2 moves; below 50% ("Cracked") the chance rises and chains run 3-4
    // moves — but every extra move adds +15 ticks of exhausted, punishable recovery at the end.
    public class IceGigas : ModNPC, IStaggerable, IDebugAttackLabel
    {
        enum AttackState : byte
        {
            None = 0,
            GlacialSlam,     // fists → three marching waves of ground spikes, each faster and farther
            FrostBreath,     // inhale → 70t sweeping cone of frost puffs (rapid small ticks + Chilled)
            HailstoneVolley, // 3 bursts: slow lobs / fast flat / one giant that shatters into shards
            IcicleCrown,     // walking cast: 8 icicles fire sequentially, each locking aim before launch
            RimefangSpikes,  // 3 chained eruptions under the player; the third is a 5-wide led field
            FrozenEcho,      // leaps in beside you, leaves a brittle statue of itself that shatters, blinks back
            WintersGrasp,    // Winter's Embrace: a point that pulls you in while firing 3 offset rings of icicles
            AbsoluteZero,    // Cracked: long staggerable channel → expanding freeze ring (roll through)
            IcePrison,       // Cracked: pillar cage around the player with one gap, shatters inward
            GlacialStampede, // Cracked: its only fast move — hyper-armor charge, spikes in its wake
            MirrorOfWinter,  // Cracked: blinks away, TWO armed statues rise at the player's flanks
            CrackTransition, // one-time 50% event: the giant cracks, unlocking the phase
            ComboRecovery,   // exhausted stand after a chain: +15t per extra move, fully punishable
            RimeWard,        // counter-stance: ice shell stores the hits it takes, detonates them back
            HoarfrostCreep,  // long shard carpet dominoes outward along the floor (or ceiling) and STAYS
            Undertow,        // blizzard pull band dragging players in, point-blank exhale at center
            CrystalCanopy,   // six stalactites above the player drop on an accelerating cadence
            BlackIce,        // floor glaze under the player: momentum carries for 8s (skate physics)
            HeartOfWinter,   // one-time 30% surprise: fake death → crawling pile → rebirth into stampede
            HalberdCleave,   // close range: ice halberd forms in the fist, slow overhead cock, one fast chop into the floor
            HalberdLunge,    // mid range: halberd couched like a lance, frost streak marks the path, then a dash that overshoots you
            HalberdLeap,     // halberd raised, a jump (only with headroom for the whole arc) that chops down on landing
        }

        //Attack timings (ticks). Telegraph-type constants run through Tel() so chained moves halve them.
        const int SlamTelegraphTicks = 45;
        const int SlamRecoveryTicks = 40;
        const float SlamMinJump = 14f;   //launch speeds (px/t up) the leap planner tries: Glacial Slam and Frozen Echo
        const float SlamMaxJump = 18f;
        const int BreathInhaleTicks = 40;
        const int BreathExhaleTicks = 70;
        const int BreathRecoveryTicks = 45;
        const int VolleyGatherTicks = 30;
        const int CrownGatherTicks = 20;
        const int CrownVolleyTicks = 84;  //8 icicles, one every 8t, plus the last flight start
        const int CrownRecoveryTicks = 25;
        const int RimefangCastTicks = 90;
        const int RimefangRecoveryTicks = 30;
        const int EchoGatherTicks = 20;
        const int EchoWindupTicks = 15;   //leap tell: stands planted while frost rises off its feet, then jumps in
        const float EchoLandingStandoff = 80f; //lands 5 tiles short of the player, so the statue stands right beside them
        const int GraspCastTicks = 30;
        const int ZeroChannelTicks = 120;
        const int ZeroStaggerableTicks = 80;  //first two thirds: a normal poise break cancels the channel
        const int ZeroRecoveryTicks = 35;
        const int PrisonCastTicks = 30;
        const int StampedeTelegraphTicks = 40;
        const int StampedeChargeTicks = 90;
        const int StampedeRecoveryTicks = 60;
        const int MirrorCastTicks = 30;
        const int CrackTicks = 30;
        const int WardTelegraphTicks = 25;
        const int WardHoldTicks = 90;
        const int WardRecoveryTicks = 35;
        const int CreepTelegraphTicks = 35;
        const int CreepRecoveryTicks = 30;
        const int CreepSegments = 40;     //8x the former five-segment carpet, same 44px spacing
        const int CreepDominoDelay = 8;   //ticks between segment eruptions — the domino cadence
        const int CreepMaxRiseTiles = 7;  //a column may climb this far above the last segment's surface (onto a ledge)...
        const int CreepMaxDropTiles = 10; //...or step this far down
        const int CreepClearanceTiles = 6; //open tiles needed on the exposed side, so a segment never sits inside a wall
        const int UndertowTelegraphTicks = 30;
        const int UndertowPullTicks = 180; //was 90 — twice the pull duration
        const int UndertowRecoveryTicks = 40;
        const int CanopyCastTicks = 20;
        const int GlazeTelegraphTicks = 30;
        const int GlazeRecoveryTicks = 25;
        const int HeartDeathTick = 15;    //fake-death spectacle plays, body vanishes
        const int HeartCrawlEndTick = 100; //the pile crawls toward the player until here
        const int HeartRebirthTick = 130;  //eruption telegraph 100→130, then the rebirth pulse

        //Halberd Cleave timing sheet (ticks; never chained, so no Tel()).
        //  Form 30 (weapon fades in, arm rises) + Tell 28 (creeps further back) = 58t heavy tell on screen
        //  Strike 12: cubic ease-in from -125° to +40° (165° envelope; the floor stops it, hence under 180°)
        //    live from strike tick 5 (blade already overhead and coming forward) through 4 embedded ticks = 11t
        //  Impact 8: blade stuck in the floor. Recovery 40: sag and pull free, last 15t shatter away = 46t punish
        const int HalberdFormTicks = 30;
        const int HalberdTellTicks = 28;
        const int HalberdStrikeTicks = 12;
        const int HalberdLiveFromStrikeTick = 5;
        const int HalberdEmbeddedLiveTicks = 4;
        const int HalberdImpactTicks = 8;
        const int HalberdRecoveryTicks = 40;
        const int HalberdFadeTicks = 15;
        const float HalberdCockAim = -110f;      //arm aim (degrees below forward) the form phase rises to
        const float HalberdTellAim = -125f;      //the tell creeps on back to here: the strike's start pose
        const float HalberdImpactAim = 40f;      //where the head meets the floor ~130px ahead
        const float HalberdRecoveryAim = 30f;    //lifts the head back out of the floor while recovering
        const float HalberdWristLagCocked = 30f; //pole trails the arm by this much while cocked (head further back)...
        const float HalberdWristLagImpact = 10f; //...and snaps nearly in line with it by the impact
        const float HalberdStrikeStepSpeed = 2.5f; //px/tick forward lean through the strike (~30px)
        const float HalberdStartRange = 170f;    //max horizontal centre distance to pick it: tip lands ~130px ahead, +26px blade
        const int HalberdCooldown = 120;

        //Halberd Lunge timing sheet (ticks; never chained).
        //  Form 24 (halberd fades in, couched low) + Tell 34 (drawn back, frost streak along the locked path) = 58t
        //  Dash 48: speed ∝ x(1-x)^2 (x = dash progress) — fast start, peak at 1/3 (1.78 x distance / 48), long decay.
        //    Distance solved at tell start to carry the tip 100px past the target, clamped 120-441px, so it reaches a
        //    player 30 tiles (480px) away; peak ≤ 16.4 px/t. Live the whole time it moves: a roll only has to pass
        //    through the blade, which sweeps past a player in ~12t. Jump frame while dashing.
        //  Follow-through 16 (tip dips) + Recovery 36 (last 15 shatter) = 52t punish
        const int LungeFormTicks = 24;
        const int LungeTellTicks = 34;
        const int LungeDashTicks = 48;
        const int LungeFollowTicks = 16;
        const int LungeRecoveryTicks = 36;
        const float LungeOvershoot = 100f;       //px the tip is carried past the target's position at tell start
        const float LungeTipReach = 139f;        //px from NPC.Center to the tip in the dash pose (hand 21 ahead + 118 of pole)
        const float LungeMinDistance = 120f;
        const float LungeMaxDistance = 441f;     //480px gap - 139px tip reach + 100px overshoot
        const float LungeMinStartRange = 170f;   //closer than this the cleave is the answer
        const float LungeMaxStartRange = 480f;   //30 tiles: tip + overshoot still covers it at max distance
        const float LungeTellArmAim = 100f;      //hand drawn back beside the hip
        const float LungeTellPoleAim = -2f;      //pole level, tip forward at about knee-to-waist height
        const float LungeDashArmAim = 45f;       //thrust: hand drives forward and down to belt height
        const float LungeDashPoleAim = 8f;       //tip dips onto a standing player's torso (~48px above the floor)
        const float LungeFollowArmAim = 40f;
        const float LungeFollowPoleAim = 30f;    //tip sags toward the floor as the dash dies
        const int LungeCooldown = 150;

        //Halberd Leap timing sheet (ticks; never chained). SlamPhase 0 → 1 (flight) → 2 (landed).
        //  Phase 0: form 30 (halberd fades in, arm rises) + windup 18 (stands planted, arm cocked back over the head, frost
        //    rising off the feet) = 48t tell
        //  Phase 1: planned arc, 10-14 px/t launch (apex ~5-20 tiles); the whole arc must be clear or it isn't picked.
        //    Held cocked to 60% of the flight, then a cubic chop that meets the floor on landing. Live the whole flight.
        //  Phase 2: 4 more live ticks embedded, impact hold 10, recovery 40 (last 15 shatter) = 46t punish
        const int LeapFormTicks = 30;
        const int LeapWindupTicks = 18;
        const float LeapMinJump = 10f;
        const float LeapMaxJump = 14f;
        const float LeapLandingStandoff = 110f;  //lands this far short so the head (~130px ahead at impact) hits the target
        const float LeapChopStartFraction = 0.6f; //of the flight, when the chop starts
        const int LeapEmbeddedLiveTicks = 4;
        const int LeapImpactTicks = 10;
        const int LeapRecoveryTicks = 40;
        const float LeapMinStartRange = 160f;
        const float LeapMaxStartRange = 640f;    //40 tiles
        const int LeapCooldown = 150;

        int LeapPhase0End => LeapFormTicks + LeapWindupTicks;
        int LeapLandedEnd => LeapImpactTicks + LeapRecoveryTicks;

        int LungeDashStart => LungeFormTicks + LungeTellTicks;
        int LungeFollowStart => LungeDashStart + LungeDashTicks;
        int LungeEndTick => LungeFollowStart + LungeFollowTicks + LungeRecoveryTicks;

        int HalberdStrikeStart => HalberdFormTicks + HalberdTellTicks;
        int HalberdImpactTick => HalberdStrikeStart + HalberdStrikeTicks;
        int HalberdEndTick => HalberdImpactTick + HalberdImpactTicks + HalberdRecoveryTicks;

        ///<summary>Sprite sits this many pixels lower than the hitbox so the feet plant on the
        ///ground instead of hovering above it (+Y is down; Main.NPCAddHeight adds DrawOffsetY to
        ///the draw position). Matches Holy Gigas. Anything drawn relative to NPC.Center or
        ///NPC.Bottom that should track the BODY rather than the hitbox has to add this too, which
        ///is why it is one constant rather than a literal repeated at each draw site.
        ///FrozenGigasStatue draws this same sheet by hand and shares it for the same reason.</summary>
        internal const float SpriteDrawOffsetY = 4f;

        //Projectile damage, tuned for a MID-HARDMODE boss event (hostile projectiles deal 2x this
        //on hit in normal mode). In SuperHardMode every value additionally runs through ScaleDamage
        //(×1.3 flat SHM bump; SHM progress is applied to every enemy hit in ProgressionScalingPlayer).
        int SpikeDamage => ScaleDamage(45);
        int BreathDamage => ScaleDamage(16);  //rapid ticks, so each is small
        int HailDamage => ScaleDamage(40);
        int CrownDamage => ScaleDamage(36);
        int ZoneDamage => ScaleDamage(45);
        int RingDamage => ScaleDamage(60);
        int PrisonPillarDamage => ScaleDamage(32);
        int StampedeShardDamage => ScaleDamage(32);
        int StaggerShedDamage => ScaleDamage(25);
        int WardShardDamage => ScaleDamage(28);
        int CanopyDamage => ScaleDamage(40);
        int HeartRingDamage => ScaleDamage(50);
        int CreepDamage => ScaleDamage(38); //a touch under the spikes — it lingers
        int HalberdDamage => ScaleDamage(55); //its one melee hit: heavy, slow, single
        int LungeDamage => ScaleDamage(50);   //a touch under the cleave: it covers distance
        int LeapDamage => ScaleDamage(60);    //the heaviest halberd hit: longest tell, whole-body commitment

        int ScaleDamage(int baseDamage) => SHM ? (int)(baseDamage * 1.3f) : baseDamage;

        ///<summary>Frost buildup each damaging ice hit adds (out of FrostBuildup.MaximumBuildup = 100). Applied
        ///centrally in tsorcGlobalProjectile.OnHitPlayer for every shot sourced from this NPC or its statues;
        ///the non-damaging GigasFrostBurst clouds pass the same value through their ai[1].</summary>
        public const int FrostBuildupPerHit = 25;

        AttackState State = AttackState.None;

        /// <summary>DebugMode above-head readout (see IDebugAttackLabel).</summary>
        public string DebugAttackLabel => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0
            ? "Staggered"
            : State == AttackState.None ? "Idle" : DebugLabels.Humanize(State.ToString());

        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 150;
        bool Cracked;            //below-50% phase unlocked
        int ComboIndex;          //moves completed in the current chain beyond the first
        int MaxChainMoves = 2;   //total moves allowed in the current chain (rolled at chain start)
        bool HalfTelegraph;      //this move was chained into — telegraph halved
        int ComboRecoveryTicks;  //length of the current ComboRecovery stand
        int PendingCooldown;     //cooldown to apply once ComboRecovery ends
        int lockedDir = 1;       //stampede charge / halberd lunge direction, locked at charge (tell) start
        float lungeDistance;     //server-solved halberd lunge length (px); clients reproduce the dash without a target
        int FootstepTimer;
        int FootstepCount;
        byte SlamPhase;          //0: windup; 1: committed flight; 2: landed recovery
        float SlamVelocityX;     //server-solved arc; clients reproduce it without choosing a target
        int SlamFlightTicks;
        bool SlamImpactPlayed;  //client-local presentation latch, reset by synced attack phases
        bool creepWasCeiling;    //server-side: the floor carpet may chain into the Undertow pull; the ceiling one doesn't
        bool SHM;                //SuperHardMode encounter: escalated stats + projectile damage
        bool statsInitialized;   //one-shot world-state stat application on the first AI tick
        int BaseContactDamage;   //contact damage to restore after Heart of Winter's harmless pile
        int WardStored;          //hits absorbed by the current Rime Ward shell (capped)
        bool HeartUsed;          //the 30% fake death fires once per fight
        int LastHitTimer = 9999; //ticks since it was last hit (gates Rime Ward — no ward vs nobody)
        bool undertowExhale;     //server-side: was a player point-blank at the exhale check?
        bool wasAirborne;        //client-local: tracking a jump for the high-landing shake
        float airborneApexY;     //client-local: highest NPC.Bottom.Y reached this jump (smaller = higher)

        ///<summary>Minimum apex-to-ground drop that counts as a high landing (6 tiles). A terrain leap or the
        ///Hailstone Volley hop clears this; Black Ice's -7 wind-up hop (~80px) and stair steps don't.</summary>
        const float HighLandingDropPixels = 6 * 16f;

        ///<summary>True while Heart of Winter has the body vanished (fake death → pile → pre-rebirth).</summary>
        bool HeartHidden => State == AttackState.HeartOfWinter && AttackTimer < HeartRebirthTick;

        readonly GigasTerrainNavigation TerrainNavigation = new GigasTerrainNavigation();

        ///<summary>Boss-style exit when every player has died: despawn text, a 240t dust fade, then the burst and
        ///flash. Also keeps NPC.target on a player who hasn't died yet.</summary>
        NPCDespawnHandler despawnHandler;

        //Arm rig. The attack arm is the pauldron (back) arm: it is cut out of IceGigas.png into its own layer in every
        //frame so an attack can swing it about the pauldron while the walk frames keep animating underneath. It hangs
        //in front of the belly in frames 0 and 2-10, and at the back over the cape in frames 1 and 11-15, where the
        //fist-forward arm stays in the body. _Body + _Arm recompose the original sheet pixel-for-pixel, and both are
        //snapped to the sheet's 2x2 art grid. _ArmStraight (hand-cut from frame 14's pauldron arm, hanging straight
        //down) replaces the walk arm whenever an attack poses it, always drawn over the body.
        static Asset<Texture2D> bodyTexture, walkArmTexture, straightArmTexture;

        ///<summary>Shoulder joint per body frame (frame-local px, sheet facing left): the pauldron's centroid. The
        ///torso twists through the walk cycle, so the joint wanders ±12px in X; same arm in every frame, no jump.</summary>
        static readonly Point[] ShoulderPivots =
        {
            new(107, 65), new(106, 66), new(111, 65), new(107, 65), new(103, 63), new(99, 64), new(95, 65), new(95, 65),
            new(99, 66), new(103, 65), new(108, 66), new(109, 64), new(114, 64), new(117, 66), new(118, 66), new(116, 66),
        };

        ///<summary>Pauldron centre inside IceGigas_ArmStraight.png (32x78, facing left) — frame 14's ShoulderPivot
        ///relative to where the crop was cut (107, 54). The round pauldron spins in place about it.</summary>
        static readonly Vector2 StraightArmPivot = new Vector2(11f, 12f);

        ///<summary>Aim the straight-arm texture points at unrotated: pauldron centre → fist centre is 89° below
        ///forward (hanging straight down). At this aim on frames 13/14 the rig reproduces the original pixels.</summary>
        static readonly float StraightArmRestAim = MathHelper.ToRadians(89f);

        ///<summary>Roughly where the walk arm hangs, pauldron → fist: forward (62°) in frames 0 and 2-10, straight
        ///down at the back (89°) in 1 and 11-15. The straight arm starts there on the cut, so it swings into its pose
        ///instead of popping.</summary>
        static readonly float WalkArmHangAimForward = MathHelper.ToRadians(62f);
        static readonly float WalkArmHangAimDown = MathHelper.ToRadians(89f);
        const int JumpFrame = 1;
        const int FirstArmDownWalkFrame = 11;

        //Arm pose, run on every machine from the synced State/AttackTimer. Aim is radians below "forward" (the
        //facing direction): 0 = straight out, -PI/2 = overhead, +PI/2 = straight down. A Run* method requests the
        //straight arm each tick it wants it; the request is consumed at the top of the next AI tick.
        bool armStraight;
        bool armRequested;
        float armAim;
        float armAimTarget;
        float armAimRate;

        //Halberd held in the straight arm's fist. IceGigas_Halberd.png is the GreatFrostHalberdProj sprite at half size,
        //re-blocked to 2x2 art pixels (126x126, tip top-left, butt bottom-right). Grip = 30% up the pole from the butt.
        static Asset<Texture2D> halberdTexture;
        static readonly Vector2 HalberdGrip = new Vector2(87f, 86f);
        const float HalberdGripToTip = 121f;      //px along the pole, grip → spear tip (2,0)
        const float HalberdTipAngle = -3f * MathHelper.PiOver4; //texture-space angle grip → tip: up-left
        const float ArmLengthToFist = 57f;        //straight arm: pauldron pivot → fist centre
        float halberdWristLag;                    //degrees the pole trails the arm; derived per machine from AttackTimer

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = 8; //afterimages for the stampede charge
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 110;
            //Mid-Hardmode boss-event tier (first encounter is a HM event; SHM rare-event stats are
            //applied on the first AI tick from the world flag — see InitializeStats)
            NPC.damage = 90;
            NPC.defense = 32;
            NPC.lifeMax = 22000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 150000f; //Dark Souls = value / 25 (GlobalNPC.OnKill) = 6000 — a bit under TheSorrow's 6800
            NPC.npcSlots = 10;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.1f;
            DrawOffsetY = SpriteDrawOffsetY; //plant the feet on the ground, as Holy Gigas does
            AnimationType = 28; // Zombie frame structure

            // Phase 2: SmartFighter4AI movement + beast levers (mirrors Gigas). minSurfaceWidth (in AI)
            // keeps it off 1-tile ledges; NavSearchRadius enables A*; MaxJumpPower modestly above the 8 default
            // for a strong jump (NPC.gravity is read-only, so no true heavy "weighty" fall). Tune to taste.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60; // larger window so A* can find valid flat ledges above/across to jump to (mirrors Gigas)
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
			// Match Holy Gigas: one shallow stair-step is safe support for the two-tile core.
			globalNPC.MaxSurfaceStep = 1;
            globalNPC.StableHeavyNavigation = TerrainNavigation;
            // On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
            EvasiveProfile.HeavyBeast(globalNPC);
            // Phase 1 (beast positioner): never stand still — oscillate in a large band when it can't path; wander
            // off if it can't reach you AND you stop hitting it for ~10s. Tune the band to taste.
            globalNPC.KiteRangeMin = 0f;
            globalNPC.KiteRangeMax = 30f;
            globalNPC.KiteLooseness = 0.3f;
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;

            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.IceGigas.DespawnHandler"), new Color(140, 210, 255), DustID.IceTorch);
            // It isn't NPC.boss, so a failed event would otherwise delete it instantly; let the handler fade it out.
            globalNPC.UsesOwnDespawnHandler = true;
        }

        //On-hit evasion only from true neutral: recovery frames and combo exhaustion are deliberate
        //punish windows, so the HeavyBeast reaction must not bail it out of them.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            RegisterHit();
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            RegisterHit();
            if (State == AttackState.None)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        void RegisterHit()
        {
            LastHitTimer = 0;
            //Rime Ward stores every hit it absorbs and throws them back at detonation
            if (State == AttackState.RimeWard && AttackTimer > WardTelegraphTicks && AttackTimer <= WardTelegraphTicks + WardHoldTicks
                && Main.netMode != NetmodeID.MultiplayerClient && WardStored < 8)
            {
                WardStored++;
                NPC.netUpdate = true;
            }
        }

        ///<summary>The Rime Ward shell soaks 30% of incoming damage while it holds.</summary>
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (State == AttackState.RimeWard && AttackTimer > WardTelegraphTicks && AttackTimer <= WardTelegraphTicks + WardHoldTicks)
            {
                modifiers.FinalDamage *= 0.7f;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        ///<summary>Unique weapon: the Heart of Winter tome — guaranteed on the first kill, 25% on
        ///repeat HM kills. SHM rare-event respawns pay out purely in Dark Souls (via NPC.value)
        ///instead. OnKill registers the type in NewSlain (loot rules resolve first, so the
        ///first-kill guarantee sees the pre-kill state).</summary>
        public override void ModifyNPCLoot(Terraria.ModLoader.NPCLoot npcLoot)
        {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.ByCondition(
                new NonSHMFirstKillRule(), ModContent.ItemType<HeartOfWinter>()));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write(Cracked);
            writer.Write((byte)ComboIndex);
            writer.Write((byte)MaxChainMoves);
            writer.Write(HalfTelegraph);
            writer.Write((short)ComboRecoveryTicks);
            writer.Write((sbyte)lockedDir);
            writer.Write(SHM);
            writer.Write((byte)WardStored);
            writer.Write(HeartUsed);
            writer.Write(SlamPhase);
            writer.Write(SlamVelocityX);
            writer.Write(SlamFlightTicks);
            writer.Write(lungeDistance);
            TerrainNavigation.Send(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            Cracked = reader.ReadBoolean();
            ComboIndex = reader.ReadByte();
            MaxChainMoves = reader.ReadByte();
            HalfTelegraph = reader.ReadBoolean();
            ComboRecoveryTicks = reader.ReadInt16();
            lockedDir = reader.ReadSByte();
            SHM = reader.ReadBoolean();
            WardStored = reader.ReadByte();
            HeartUsed = reader.ReadBoolean();
            SlamPhase = reader.ReadByte();
            if (SlamPhase != 2) SlamImpactPlayed = false;
            SlamVelocityX = reader.ReadSingle();
            SlamFlightTicks = reader.ReadInt32();
            lungeDistance = reader.ReadSingle();
            TerrainNavigation.Receive(reader, NPC);
        }

        ///<summary>World position of the mouth: frame pixel (78, 56) facing left, which sits 10px ahead of and 51px above
        ///NPC.Center (frame centre x 88; NPC.Center is frame y 107 once DrawOffsetY and vanilla's 4px sink are counted).
        ///The head barely bobs through the walk cycle, so one offset serves every frame.</summary>
        Vector2 MouthPosition => NPC.Center + new Vector2(NPC.direction * 10f, -51f);

        ///<summary>Chained-into moves cast faster: half telegraph.</summary>
        int Tel(int baseTicks) => HalfTelegraph ? baseTicks / 2 : baseTicks;

        ///<summary>Poise break (neutral or the Absolute Zero staggerable window): the spell disperses in
        ///a burst of frost, the current combo is dropped, and it sheds a few damaging ice chunks — the
        ///punish window has footwork in it.</summary>
        public void OnStagger(NPC npc)
        {
            TerrainNavigation.CancelForCombat(NPC);
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f, Pitch = -0.5f }, NPC.Center);
                for (int i = 0; i < 26; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ice, vel.X, vel.Y, 80, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 0.3f;
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int chunks = Main.rand.Next(3, 5);
                for (int i = 0; i < chunks; i++)
                {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-6f, -4f));
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                        ModContent.ProjectileType<GigasIceShard>(), StaggerShedDamage, 1f, Main.myPlayer, 0.25f);
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            ComboIndex = 0;
            HalfTelegraph = false;
            AttackCooldown = Math.Max(AttackCooldown, 120);
        }

        public override void AI()
        {
            // High-jump landing: a little screen shake and a slam. Tracks the apex of each airborne stretch and fires on
            // touchdown when the drop was at least HighLandingDropPixels. The old oldVelocity.Y > 2 check never fired:
            // on the landing tick collision leaves a small leftover velocity.Y, so by the time velocity.Y reads 0 the
            // previous tick's velocity is one tick of gravity. Glacial Slam plays its own bigger impact. Presentation only.
            if (!Main.dedServ)
            {
                bool airborne = NPC.velocity.Y != 0f;

                if (airborne)
                {
                    if (!wasAirborne)
                    {
                        wasAirborne = true;
                        airborneApexY = NPC.Bottom.Y;
                    }

                    airborneApexY = Math.Min(airborneApexY, NPC.Bottom.Y);
                }
                else if (wasAirborne)
                {
                    wasAirborne = false;
                    float dropPixels = NPC.Bottom.Y - airborneApexY;
                    bool highLanding = dropPixels >= HighLandingDropPixels;

                    if (highLanding && State != AttackState.GlacialSlam && !HeartHidden)
                    {
                        UsefulFunctions.ScreenShake(NPC.Bottom, 5f, 14, 6f, 600f);
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                            with { Volume = 0.4f, Pitch = 0.1f }, NPC.Bottom);
                        SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.7f, Pitch = -0.25f }, NPC.Bottom);
                    }
                }
            }
            //Arm rig: consume last tick's pose request before any early return below, so stagger, despawn or an
            //attack that stops asking all drop back to the animated walk arm. The first straight tick starts from
            //where the walk arm hangs; after that the aim eases toward the target at the requested rate.
            if (armRequested && !armStraight)
            {
                int walkFrame = NPC.frame.Y / Math.Max(NPC.frame.Height, 1);
                bool walkArmHangsDown = walkFrame == JumpFrame || walkFrame >= FirstArmDownWalkFrame;
                armAim = WalkArmHangAimForward;

                if (walkArmHangsDown)
                {
                    armAim = WalkArmHangAimDown;
                }
            }

            armStraight = armRequested;
            armRequested = false;

            if (armStraight)
            {
                armAim = MathHelper.Lerp(armAim, armAimTarget, armAimRate);
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Everyone died: stand down and let the handler run its 240t fade-out. It removes the NPC itself.
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (despawnHandler.IsDespawning)
            {
                State = AttackState.None;
                AttackTimer = 0;
                NPC.dontTakeDamage = false;
                NPC.velocity.X *= 0.8f;
                UpdateAura();
                return;
            }

            Player player = Main.player[NPC.target];
            InitializeStats();
            if (LastHitTimer < 9999)
            {
                LastHitTimer++;
            }

            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            //The 30% surprise, once per fight: interrupts anything except Absolute Zero's committed
            //tail (the channel already promised the nova) and can't fire while the body is staggered.
            if (!HeartUsed && Main.netMode != NetmodeID.MultiplayerClient && globalNPC.StaggerTimer <= 0
                && NPC.life < NPC.lifeMax * 0.3f && State != AttackState.HeartOfWinter
                && !(State == AttackState.AbsoluteZero && AttackTimer >= ZeroStaggerableTicks && AttackTimer <= ZeroChannelTicks))
            {
                HeartUsed = true;
                ComboIndex = 0;
                HalfTelegraph = false;
                StartAttack(AttackState.HeartOfWinter);
            }

            //Staggered: rooted by the global system; slumped lean + falling frost
            if (globalNPC.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.18f, 0.08f);
                if (Main.rand.NextBool(3))
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, 0f, 1f, 120, default, 1f);
                    Main.dust[dust].velocity *= 0.3f;
                }
                UpdateAura();
                return;
            }
            NPC.rotation *= 0.9f;

            if (State == AttackState.None)
            {
                if (TerrainNavigation.Update(NPC))
                {
                    FootstepEffects();
                }
                else
                {
                //Slow, weighty walk — one speed, always. The arena gets faster, not the giant.
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.5f, canTeleport: false, canPounce: false, minSurfaceWidth: 2, canWalkBackwards: true); // Combat leaps use Glacial Slam; terrain jumps require a flat landing.
                bool recoveringFromNarrowPerch = TerrainNavigation.RecoverFromNarrowPerch(NPC, globalNPC, player);
                if (NPC.lavaWet)
                {
                    NPC.velocity.Y -= 2;
                }
                FootstepEffects();

                bool terrainLeapStarted = !recoveringFromNarrowPerch && TerrainNavigation.TryBegin(NPC, player);
                if (!terrainLeapStarted && AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                if (!terrainLeapStarted && !recoveringFromNarrowPerch && Main.netMode != NetmodeID.MultiplayerClient && NPC.velocity.Y == 0f && !player.dead && player.active)
                {
                    //The 50% crack event takes priority the moment it's grounded
                    if (!Cracked && NPC.life < NPC.lifeMax / 2)
                    {
                        Cracked = true;
                        StartAttack(AttackState.CrackTransition);
                    }
                    else if (AttackCooldown <= 0 && NPC.Distance(player.Center) < 1100f)
                    {
                        PickAttack(player);
                    }
                }
                }
            }
            else
            {
                RunAttack(globalNPC, player);
            }

            UpdateAura();
        }

        ///<summary>One-shot on the first AI tick: SuperHardMode rare-event encounters get escalated
        ///stats (the world flag isn't reliable in SetDefaults). Also captures the contact damage that
        ///Heart of Winter's harmless pile must restore.</summary>
        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                NPC.lifeMax = 44000; //double the HM baseline
                NPC.life = NPC.lifeMax;
                // New Enemy Balance: the registry value replaces the line above when enabled. It is applied here, after vanilla scaling, so the helper applies the Expert / Master multiplier.
                EnemyBalance.ApplyLateLife(NPC);
                NPC.damage = 150;
                NPC.defense = 50;
                NPC.netUpdate = true;
            }
            BaseContactDamage = NPC.damage;
        }

        #region Attack selection + combo chains

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;
            bool glazeActive = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<GigasGlazeStrip>())
                {
                    glazeActive = true;
                    break;
                }
            }

            // Three weights need more than a single condition, so they are resolved here rather than
            // nested inline in the table below.
            float blackIceWeight = 0.5f;

            if (glazeActive)
            {
                blackIceWeight = 0f;      // already glazed: no point re-applying
            }
            else if (Cracked)
            {
                blackIceWeight = 1f;      // phase 2 leans on it
            }

            float absoluteZeroWeight = 0f;

            if (Cracked)
            {
                absoluteZeroWeight = distTiles < 18f ? 1.1f : 0.3f;
            }

            float glacialStampedeWeight = 0f;

            if (Cracked)
            {
                glacialStampedeWeight = distTiles > 18f && sameLevel ? 1f : 0.3f;
            }

            float horizontalGap = Math.Abs(player.Center.X - NPC.Center.X);
            bool halberdInReach = horizontalGap < HalberdStartRange && sameLevel;
            bool lungeInReach = horizontalGap >= LungeMinStartRange && horizontalGap <= LungeMaxStartRange && sameLevel;
            //The leap needs headroom: only offered when an arc clear of tiles to a landing beside the player exists right now.
            bool leapInRange = horizontalGap >= LeapMinStartRange && horizontalGap <= LeapMaxStartRange;
            bool leapPossible = leapInRange && TryPlanLeap(player, LeapLandingStandoff, LeapMinJump, LeapMaxJump, out _, out _);

            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.HalberdCleave,   halberdInReach ? 5f : 0f),   //only when the chop can land; ~40% of close-range picks
                (AttackState.HalberdLunge,    lungeInReach ? 1.1f : 0f),   //only when the dash can reach
                (AttackState.HalberdLeap,     leapPossible ? 1f : 0f),     //only with a clear arc
                (AttackState.GlacialSlam,     distTiles < 30f ? 1f : 0.4f),
                (AttackState.FrostBreath,     distTiles < 25f ? 1f : 0.2f),
                (AttackState.HailstoneVolley, distTiles > 15f ? 0.9f : 0f), //leaps in on cast, so it needs the room — mid/long range only
                (AttackState.IcicleCrown,     0.8f),
                (AttackState.RimefangSpikes,  0.8f),
                (AttackState.FrozenEcho,      1.1f),
                (AttackState.WintersGrasp,    0.6f),
                (AttackState.RimeWard,        LastHitTimer < 90 ? 0.6f : 0f), //no ward against nobody attacking
                (AttackState.HoarfrostCreep,  distTiles < 22f ? 0.85f : 0.2f),
                (AttackState.Undertow,        distTiles > 10f && distTiles < 35f && sameLevel ? 0.8f : 0f),
                (AttackState.CrystalCanopy,   0.8f),
                (AttackState.BlackIce,        blackIceWeight),
                (AttackState.AbsoluteZero,    absoluteZeroWeight),
                (AttackState.IcePrison,       Cracked ? 0.7f : 0f),
                (AttackState.GlacialStampede, glacialStampedeWeight),
                (AttackState.MirrorOfWinter,  Cracked ? 0.35f : 0f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].state == LastAttack)
                {
                    pool[i].weight *= 0.5f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            AttackState chosen = AttackState.GlacialSlam;
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    chosen = pool[i].state;
                    break;
                }
            }

            //Fresh (non-chained) attack: reset the combo and roll how long a chain MAY run this time
            ComboIndex = 0;
            HalfTelegraph = false;
            MaxChainMoves = Cracked ? Main.rand.Next(3, 5) : 2; //3-4 moves below half health
            StartAttack(chosen);
        }

        void StartAttack(AttackState state)
        {
            TerrainNavigation.CancelForCombat(NPC);
            SlamPhase = 0;
            SlamImpactPlayed = false;
            State = state;
            AttackTimer = 0;
            NPC.netUpdate = true;
        }

        ///<summary>The designated follow-up each attack may chain into (None = chain-ender). Below 50%
        ///two links escalate: breath pulls into the Undertow vacuum, and the crown becomes a canopy.</summary>
        AttackState ChainFollowUp(AttackState state) => state switch
        {
            AttackState.GlacialSlam => AttackState.FrostBreath,
            AttackState.FrostBreath => Cracked ? AttackState.Undertow : AttackState.IcicleCrown,
            AttackState.IcicleCrown => Cracked ? AttackState.CrystalCanopy : AttackState.GlacialSlam,
            AttackState.HailstoneVolley => AttackState.RimefangSpikes,
            AttackState.RimefangSpikes => AttackState.WintersGrasp,
            AttackState.BlackIce => AttackState.GlacialSlam, //slam waves you can't brake away from
            //The floor carpet pulls you back onto itself: flight, roll or grapple — pick one
            AttackState.HoarfrostCreep => creepWasCeiling ? AttackState.None : AttackState.Undertow,
            _ => AttackState.None,
        };

        void EndAttack(int cooldown)
        {
            if (TerrainNavigation.IsTraversing) return;
            LastAttack = State;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Try to chain: designated follow-up, within the rolled chain length, on a chance roll
                AttackState next = ChainFollowUp(State);
                bool canChain = next != AttackState.None
                    && ComboIndex + 1 < MaxChainMoves
                    && Main.rand.NextFloat() < (Cracked ? 0.55f : 0.3f);
                if (canChain)
                {
                    ComboIndex++;
                    HalfTelegraph = true;
                    StartAttack(next);
                    return;
                }
                //Chain over: every extra move bought +15t of exhausted, punishable recovery
                if (ComboIndex > 0)
                {
                    ComboRecoveryTicks = ComboIndex * 15;
                    //A combo already spent its "idle" time in that punishable stand, so the
                    //walk-around pause afterward shrinks per extra move chained — 22% off per
                    //extra move chained, floor 30%. Without this a 3-4 move combo racked up BOTH
                    //the recovery stand AND the same flat pause as a single move, doubling the lull.
                    PendingCooldown = (int)(cooldown * MathHelper.Clamp(1f - ComboIndex * 0.22f, 0.3f, 1f));
                    HalfTelegraph = false;
                    StartAttack(AttackState.ComboRecovery);
                    return;
                }
                //Wider jitter (was +0-60t) so back-to-back fights don't fall into a metronome.
                AttackCooldown = cooldown + Main.rand.Next(100);
                NPC.netUpdate = true;
            }
            State = AttackState.None;
            AttackTimer = 0;
            HalfTelegraph = false;
            //Safety: any exit path out of Heart of Winter restores the body
            NPC.dontTakeDamage = false;
            if (BaseContactDamage > 0)
            {
                NPC.damage = BaseContactDamage;
            }
        }

        #endregion

        #region Attack execution

        void RunAttack(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            if (player.dead || !player.active)
            {
                ComboIndex = 0;
                EndAttack(60);
                return;
            }

            //Face the player, except mid-stampede and mid-lunge (direction locked at the charge / tell start)
            bool stampedeLocked = State == AttackState.GlacialStampede && AttackTimer > Tel(StampedeTelegraphTicks);
            bool lungeLocked = State == AttackState.HalberdLunge && AttackTimer > LungeFormTicks;

            if (stampedeLocked || lungeLocked)
            {
                NPC.direction = lockedDir;
            }
            else
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.GlacialSlam:
                    RunGlacialSlam(globalNPC, player);
                    break;

                case AttackState.FrostBreath:
                    RunFrostBreath(globalNPC, player);
                    break;

                case AttackState.HailstoneVolley:
                    RunHailstoneVolley(globalNPC, player);
                    break;

                case AttackState.IcicleCrown:
                    RunIcicleCrown(globalNPC);
                    break;

                case AttackState.RimefangSpikes:
                    RunRimefangSpikes(globalNPC, player);
                    break;

                case AttackState.FrozenEcho:
                    RunFrozenEcho(globalNPC, player);
                    break;

                case AttackState.WintersGrasp:
                    RunWintersGrasp(globalNPC, player);
                    break;

                case AttackState.AbsoluteZero:
                    RunAbsoluteZero(globalNPC);
                    break;

                case AttackState.IcePrison:
                    RunIcePrison(globalNPC, player);
                    break;

                case AttackState.GlacialStampede:
                    RunGlacialStampede(globalNPC);
                    break;

                case AttackState.MirrorOfWinter:
                    RunMirrorOfWinter(globalNPC, player);
                    break;

                case AttackState.CrackTransition:
                    RunCrackTransition(globalNPC);
                    break;

                case AttackState.ComboRecovery:
                    RunComboRecovery();
                    break;

                case AttackState.RimeWard:
                    RunRimeWard(globalNPC);
                    break;

                case AttackState.HoarfrostCreep:
                    RunHoarfrostCreep(globalNPC, player);
                    break;

                case AttackState.Undertow:
                    RunUndertow(globalNPC, player);
                    break;

                case AttackState.CrystalCanopy:
                    RunCrystalCanopy(globalNPC, player);
                    break;

                case AttackState.BlackIce:
                    RunBlackIce(globalNPC, player);
                    break;

                case AttackState.HeartOfWinter:
                    RunHeartOfWinter(globalNPC, player);
                    break;

                case AttackState.HalberdCleave:
                    RunHalberdCleave(globalNPC);
                    break;

                case AttackState.HalberdLunge:
                    RunHalberdLunge(globalNPC, player);
                    break;

                case AttackState.HalberdLeap:
                    RunHalberdLeap(globalNPC, player);
                    break;

            }
        }

        // Fixed-sheet whole-body attack: 45t tell (22t chained), validated high leap to within
        // six tiles of the committed target, impact on ground contact, then 40t recovery.
        // Spike bands reach 64 tiles each side (4x); each spike releases 64px/30 frost.
        void RunGlacialSlam(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = SlamPhase < 2;
            if (SlamPhase == 0)
            {
                NPC.velocity.X *= 0.8f;
                if (!Main.dedServ)
                {
                    if (AttackTimer == 1)
                        SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.7f, Pitch = -0.4f }, NPC.Bottom);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 fist = NPC.Center + new Vector2(i == 0 ? -30f : 30f, Main.rand.NextFloat(-10f, 20f));
                        Dust dust = Dust.NewDustPerfect(fist, DustID.IceTorch, new Vector2(0f, -1f), 80, default, 1.3f);
                        dust.noGravity = true;
                    }
                }
                if (AttackTimer >= Tel(SlamTelegraphTicks) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!TryPlanLeap(player, 0f, SlamMinJump, SlamMaxJump, out Vector2 launch, out int flightTicks))
                    {
                        EndAttack(90); // No near-player landing/clear arc: re-position instead of hopping.
                        return;
                    }
                    SlamVelocityX = launch.X;
                    SlamFlightTicks = flightTicks;
                    NPC.velocity = launch;
                    SlamPhase = 1;
                    AttackTimer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (SlamPhase == 1)
            {
                NPC.velocity.X = SlamVelocityX;
                bool landed = AttackTimer > 3 && NPC.velocity.Y == 0f && NPC.oldVelocity.Y > 0f && NPC.collideY;
                if (landed && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SlamPhase = 2;
                    AttackTimer = 0;
                    NPC.velocity.X = 0f;
                    NPC.netUpdate = true;
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        for (int tile = 2; tile <= 64; tile++)
                        {
                            int delay = tile <= 24 ? (tile - 2) * 3
                                      : tile <= 44 ? 66 + (tile - 24) * 4
                                      : 146 + (tile - 44) * 5;
                            SpawnSpike(NPC.Center.X + dir * tile * 16f, delay, tile <= 44 ? 1f : 1.15f, SpikeDamage, 64f);
                        }
                    }
                }
                else if (AttackTimer > SlamFlightTicks + 90 && Main.netMode != NetmodeID.MultiplayerClient)
                    EndAttack(120); // Landing terrain changed after commitment.
                return;
            }
            // Latch presentation locally, so network latency cannot skip the landing sound/shake.
            if (!SlamImpactPlayed && !Main.dedServ)
            {
                SlamImpactPlayed = true;
                UsefulFunctions.ScreenShake(NPC.Bottom, 10f, 20);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                    with { Volume = 0.65f, Pitch = -0.1f }, NPC.Bottom);
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.75f, Pitch = -0.1f }, NPC.Bottom);
            }
            NPC.velocity.X *= 0.7f;
            if (AttackTimer >= SlamRecoveryTicks) EndAttack(150);
        }

        ///<summary>Solves a jump arc to land near the player. standoffX: px short of the player on this NPC's side to aim
        ///for (0 = on top of them, Glacial Slam; Frozen Echo stands off). Server-side.</summary>
        bool TryPlanLeap(Player player, float standoffX, float minJump, float maxJump, out Vector2 launch, out int flightTicks)
        {
            launch = Vector2.Zero;
            flightTicks = 0;
            float gravity = NPC.gravity > 0f ? NPC.gravity : 0.3f;
            float ownSide = 1f;

            if (NPC.Center.X < player.Center.X)
            {
                ownSide = -1f;
            }

            float aimX = player.Center.X + ownSide * standoffX;
            // Nearest the aim point first. Check support, full-body clearance and the entire actual arc,
            // including Terraria's gravity-before-movement order and terminal fall speed.
            for (int offset = 0; offset <= 12; offset++)
            {
                int tiles = (offset + 1) / 2 * (offset % 2 == 0 ? -1 : 1);
                float x = aimX + tiles * 16f;
                float groundY = FindGroundY(new Vector2(x, player.Bottom.Y - 48f), 24);
                if (groundY < 0f || !UsefulFunctions.IsFootprintSupported((int)(x / 16f), (int)(groundY / 16f), 2)) continue;
                Vector2 landing = new Vector2(x - NPC.width * 0.5f, groundY - NPC.height);
                if (Collision.SolidCollision(landing, NPC.width, NPC.height)) continue;
                for (float jump = minJump; jump <= maxJump; jump += 2f)
                {
                    float y = NPC.position.Y;
                    float vy = -jump;
                    int ticks = 0;
                    for (int t = 1; t <= 240; t++)
                    {
                        vy = Math.Min(vy + gravity, NPC.maxFallSpeed);
                        y += vy;
                        if (vy > 0f && y >= landing.Y) { ticks = t; break; }
                    }
                    if (ticks == 0) continue;
                    float vx = (landing.X - NPC.position.X) / ticks;
                    if (Math.Abs(vx) > 12f) continue;
                    Vector2 position = NPC.position;
                    vy = -jump;
                    bool clear = true;
                    for (int t = 1; t < ticks; t++)
                    {
                        vy = Math.Min(vy + gravity, NPC.maxFallSpeed);
                        Vector2 step = new Vector2(vx, vy);
                        Vector2 allowed = Collision.TileCollision(position, step, NPC.width, NPC.height);
                        if (Vector2.DistanceSquared(allowed, step) > 0.01f) { clear = false; break; }
                        position += step;
                        if (Collision.SolidCollision(position, NPC.width, NPC.height)) { clear = false; break; }
                    }
                    if (!clear) continue;
                    launch = new Vector2(vx, -jump);
                    flightTicks = ticks;
                    return true;
                }
            }
            return false;
        }


        public override bool? CanFallThroughPlatforms()
        {
            bool leapState = State == AttackState.GlacialSlam || State == AttackState.FrozenEcho || State == AttackState.HalberdLeap;
            bool leaping = leapState && SlamPhase == 1;

            if (leaping)
            {
                return false;
            }

            return null;
        }

        void RunFrostBreath(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int inhale = Tel(BreathInhaleTicks);
            globalNPC.AttackCommitted = AttackTimer <= inhale + BreathExhaleTicks;
            NPC.velocity.X *= 0.8f;
            Vector2 mouth = MouthPosition;

            if (AttackTimer <= inhale)
            {
                //Inhale: mist visibly SUCKED toward its head — inverted flow is the read
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                }
                for (int i = 0; i < 3; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = mouth + angle.ToRotationVector2() * Main.rand.NextFloat(40f, 110f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 120, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (mouth - pos) * 0.08f;
                }
            }
            else if (AttackTimer <= inhale + BreathExhaleTicks)
            {
                //Exhale: sweeping cone, raking from low to high
                int tick = AttackTimer - inhale;
                if (tick == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && tick % 3 == 0)
                {
                    float sweep = MathHelper.Lerp(0.45f, -0.6f, tick / (float)BreathExhaleTicks); //radians below/above horizontal
                    for (int i = 0; i < 2; i++)
                    {
                        float angle = sweep + Main.rand.NextFloat(-0.1f, 0.1f);
                        //9f launch speed + the puff's long-breath decay (GigasFrostBreathPuff.LongDecay,
                        //selected by ai[0]) reach ~450px over ~1.7s. The puff still leaves the mouth
                        //like a breath, not a dart; the distance comes from traveling longer, not faster.
                        Vector2 vel = new Vector2(NPC.direction * (float)Math.Cos(angle), (float)Math.Sin(angle)) * (9f + Main.rand.NextFloat(-1f, 1f));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<GigasFrostBreathPuff>(), BreathDamage, 1f, Main.myPlayer, GigasFrostBreathPuff.LongBreathFlag);
                    }
                }
            }
            else if (AttackTimer >= inhale + BreathExhaleTicks + BreathRecoveryTicks)
            {
                EndAttack(150);
            }
            else
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
            }
        }

        void RunHailstoneVolley(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int gather = Tel(VolleyGatherTicks);
            globalNPC.AttackCommitted = AttackTimer <= gather + 40;
            Vector2 origin = NPC.Center + new Vector2(0f, -90f);

            if (AttackTimer == 1)
            {
                //Forward leap into the cast — closes distance while throwing overhead, instead of
                //hurling from a dead stop. Only X gets damped again once it lands (velocity.Y == 0);
                //while airborne the leap carries through the whole gather-and-throw.
                NPC.velocity = new Vector2(NPC.direction * 6.5f, -9.5f);
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            }
            else if (NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.8f;
            }

            if (AttackTimer <= gather)
            {
                //Hail condensing overhead
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = origin + angle.ToRotationVector2() * Main.rand.NextFloat(15f, 55f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Snow, 0f, 0f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (origin - pos) * 0.1f;
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Burst 1: four slow, high lobs
                if (AttackTimer == gather)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 aim = player.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), 0f);
                        Vector2 vel = UsefulFunctions.BallisticTrajectory(origin, aim, 9f, 0.25f, highAngle: true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                            ModContent.ProjectileType<GigasHailstone>(), HailDamage, 2f, Main.myPlayer, 0.25f);
                    }
                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }
                //Burst 2: six fast, flat throws leading the player. This used to close with one
                //giant boulder — cut so the whole volley stays small hailstones, not a mixed size.
                if (AttackTimer == gather + 20)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 aim = player.Center + new Vector2(player.velocity.X * 15f + Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-30f, 30f));
                        Vector2 vel = UsefulFunctions.BallisticTrajectory(origin, aim, 13f, 0.12f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                            ModContent.ProjectileType<GigasHailstone>(), HailDamage, 2f, Main.myPlayer, 0.12f);
                    }
                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
                }
            }
            if (AttackTimer >= gather + 70)
            {
                EndAttack(140);
            }
            else if (AttackTimer > gather + 40 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
            }
        }

        void RunIcicleCrown(tsorcRevampGlobalNPC globalNPC)
        {
            int gather = Tel(CrownGatherTicks);
            //Walking cast: keeps lumbering (no dodge/pounce — stately) while the crown does the work
            if (!TerrainNavigation.Update(NPC))
            {
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.5f, canTeleport: false, canDodgeroll: false, canPounce: false, minSurfaceWidth: 2, canWalkBackwards: true);
                TerrainNavigation.RecoverFromNarrowPerch(NPC, globalNPC, Main.player[NPC.target]);
            }
            FootstepEffects();
            globalNPC.AttackCommitted = AttackTimer <= gather + CrownVolleyTicks;

            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            }
            if (AttackTimer == gather && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Crown arc above the shoulders; each icicle fires 8t after the previous (rolling rhythm)
                for (int i = 0; i < 8; i++)
                {
                    Vector2 pos = NPC.Center + new Vector2((i - 3.5f) * 26f, -70f - (3.5f - Math.Abs(i - 3.5f)) * 10f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                        ModContent.ProjectileType<GigasCrownIcicle>(), CrownDamage, 2f, Main.myPlayer, 20f + i * 8f);
                }
            }
            if (AttackTimer >= gather + CrownVolleyTicks + CrownRecoveryTicks)
            {
                EndAttack(150);
            }
        }

        void RunRimefangSpikes(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = AttackTimer <= Tel(RimefangCastTicks) - 30; //tail of the cast is already recovery
            NPC.velocity.X *= 0.8f;
            int start = Tel(15);
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
            }
            //Arm: raise the fist overhead through the wind-up, hammer it down at the floor on each of the three
            //casts (4t before to 6t after the spawn tick, fast 0.45 rate so it lands with weight), re-raise between
            //them, then sag low until the cast ends. The recovery tail drops back to the walk arm.
            float armAimDegrees = -95f;
            float armRate = 0.2f; //reaches ~-80° by the first cast at full telegraph (15t)

            if (AttackTimer >= start)
            {
                armAimDegrees = -70f;
                armRate = 0.15f;
            }

            for (int cast = 0; cast < 3; cast++)
            {
                int castTick = start + cast * 25;
                bool hammering = AttackTimer >= castTick - 4 && AttackTimer <= castTick + 6;

                if (hammering)
                {
                    armAimDegrees = 60f;
                    armRate = 0.45f;
                }
            }

            if (AttackTimer > start + 56)
            {
                armAimDegrees = 40f;
                armRate = 0.1f;
            }

            if (AttackTimer <= Tel(RimefangCastTicks))
            {
                armRequested = true;
                armAimTarget = MathHelper.ToRadians(armAimDegrees);
                armAimRate = armRate;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Two single spikes chase the player, then a five-wide field leads their escape
                if (AttackTimer == start || AttackTimer == start + 25)
                {
                    SpawnSpike(player.Center.X, 30, 1f, SpikeDamage, breathCloud: true);
                }
                if (AttackTimer == start + 50)
                {
                    float centerX = player.Center.X + player.velocity.X * 25f;
                    for (int i = -2; i <= 2; i++)
                    {
                        SpawnSpike(centerX + i * 26f, 30 + Math.Abs(i) * 2, 1.15f, SpikeDamage, breathCloud: true);
                    }
                }
            }
            if (AttackTimer >= Tel(RimefangCastTicks) + RimefangRecoveryTicks)
            {
                EndAttack(150);
            }
        }

        // Close-range melee. An ice halberd freezes into the fist while the arm rises (weapon out before the tell), the arm
        // creeps back over the head, then one cubic-accelerating overhead chop slams the head into the floor ahead.
        // Tell 58t, live 8t (one well-timed roll), 46t of punish as it pulls free and the halberd shatters.
        void RunHalberdCleave(tsorcRevampGlobalNPC globalNPC)
        {
            int strikeTick = AttackTimer - HalberdStrikeStart;
            bool striking = strikeTick >= 0 && strikeTick < HalberdStrikeTicks;
            globalNPC.AttackCommitted = AttackTimer <= HalberdImpactTick; //hyper armor through tell and strike, none after

            float armAimDegrees = HalberdCockAim;
            float armRate = 0.12f;
            halberdWristLag = HalberdWristLagCocked;

            if (AttackTimer > HalberdFormTicks)
            {
                armAimDegrees = HalberdTellAim;
                armRate = 0.1f;
            }

            if (striking)
            {
                //Pose is a pure function of the tick (rate 1), so every machine collides with the same blade line.
                float progress = (strikeTick + 1f) / HalberdStrikeTicks;
                float eased = progress * progress * progress;
                armAimDegrees = MathHelper.Lerp(HalberdTellAim, HalberdImpactAim, eased);
                armRate = 1f;
                halberdWristLag = MathHelper.Lerp(HalberdWristLagCocked, HalberdWristLagImpact, eased);
            }

            if (AttackTimer >= HalberdImpactTick)
            {
                armAimDegrees = HalberdImpactAim;
                armRate = 1f;
                halberdWristLag = HalberdWristLagImpact;
            }

            if (AttackTimer >= HalberdImpactTick + HalberdImpactTicks)
            {
                armAimDegrees = HalberdRecoveryAim;
                armRate = 0.05f;
            }

            armRequested = true;
            armAimTarget = MathHelper.ToRadians(armAimDegrees);
            armAimRate = armRate;

            //Lean into the chop; plant everywhere else.
            NPC.velocity.X *= 0.8f;

            if (striking)
            {
                NPC.velocity.X = NPC.direction * HalberdStrikeStepSpeed;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == HalberdStrikeStart)
            {
                int hitboxLifetime = HalberdStrikeTicks + HalberdImpactTicks + 2;
                int hitbox = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<IceGigasHalberdHitbox>(), HalberdDamage, 6f, Main.myPlayer, NPC.whoAmI);
                Main.projectile[hitbox].timeLeft = hitboxLifetime;
            }

            if (!Main.dedServ)
            {
                HalberdCleaveEffects();
            }

            if (AttackTimer >= HalberdEndTick)
            {
                EndAttack(HalberdCooldown);
            }
        }

        // Presentation for Halberd Cleave, keyed off AttackTimer: frost converging onto the pole as it forms, the swing
        // whoosh, the floor impact (burst + shake), and the shatter as it fades. Client-only.
        void HalberdCleaveEffects()
        {
            GetHalberdBlade(out Vector2 grip, out Vector2 tip);

            HalberdFormAndShatterEffects(HalberdFormTicks, HalberdEndTick);

            if (AttackTimer == HalberdStrikeStart)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.6f }, grip);
            }

            if (AttackTimer == HalberdImpactTick)
            {
                HalberdImpactEffects(tip, 6f);
            }
        }

        // The halberd head hitting the floor, client-only: ice crack sound, screen shake of the given strength, and 40
        // ice shards kicked up off the tip (half of them falling back).
        void HalberdImpactEffects(Vector2 tip, float shakeStrength)
        {
            SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Volume = 0.8f }, tip);
            UsefulFunctions.ScreenShake(tip, shakeStrength, 14, 6f, 600f);

            for (int i = 0; i < 40; i++)
            {
                Vector2 burstVelocity = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-7f, -1f));
                Dust shard = Dust.NewDustPerfect(tip, DustID.Ice, burstVelocity, 0, default, 1.4f);
                shard.noGravity = i % 2 == 0;
            }
        }

        // Mid-range gap-closer. The halberd forms couched low like a lance, the arm draws back while a frost streak on the
        // floor marks exactly how far the dash will carry, then one dash (fast start, long slide) drives the tip 100px past
        // where you stood. Roll through it, or leave the marked line; the slide-out and recovery are the punish.
        void RunHalberdLunge(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int dashTick = AttackTimer - LungeDashStart;
            bool dashing = dashTick >= 0 && dashTick < LungeDashTicks;
            globalNPC.AttackCommitted = AttackTimer < LungeFollowStart; //hyper armor through tell and dash, none after

            //Server locks the direction and solves the length once, at tell start; both sync, so every machine runs
            //the same dash and draws the same streak.
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == LungeFormTicks)
            {
                lockedDir = NPC.direction;
                float gapToTarget = Math.Abs(player.Center.X - NPC.Center.X);
                float wantedDistance = gapToTarget - LungeTipReach + LungeOvershoot;
                lungeDistance = MathHelper.Clamp(wantedDistance, LungeMinDistance, LungeMaxDistance);
                NPC.netUpdate = true;
            }

            //Pose: arm aim and pole aim (degrees below forward) per beat. The pole is held at its own aim while the arm
            //moves, so the wrist lag is whatever separates them this tick.
            float armAimDegrees = LungeTellArmAim;
            float armRate = 0.12f;
            float poleAimDegrees = LungeTellPoleAim;

            if (dashing)
            {
                armAimDegrees = LungeDashArmAim;
                armRate = 0.35f;
                poleAimDegrees = LungeDashPoleAim;
            }

            if (AttackTimer >= LungeFollowStart)
            {
                armAimDegrees = LungeFollowArmAim;
                armRate = 0.1f;
                poleAimDegrees = LungeFollowPoleAim;
            }

            armRequested = true;
            armAimTarget = MathHelper.ToRadians(armAimDegrees);
            armAimRate = armRate;

            float currentArmDegrees = MathHelper.ToDegrees(armAim);
            float wantedLag = currentArmDegrees - poleAimDegrees;
            halberdWristLag = MathHelper.Lerp(halberdWristLag, wantedLag, 0.3f);

            if (AttackTimer == 1)
            {
                halberdWristLag = wantedLag; //no swing-in from the previous attack's wrist
            }

            //Movement: planted, then the dash profile v = distance x 12/T x x(1-x)^2 (its sum over the dash is the
            //distance), then the slide dies out.
            NPC.velocity.X *= 0.8f;

            if (dashing)
            {
                float progress = (dashTick + 0.5f) / LungeDashTicks;
                float remaining = 1f - progress;
                float dashSpeed = lungeDistance * 12f / LungeDashTicks * progress * remaining * remaining;
                NPC.velocity.X = lockedDir * dashSpeed;
                //Climb one-block rises instead of stopping dead against them.
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer == LungeDashStart)
            {
                int hitboxLifetime = LungeDashTicks + 2;
                int hitbox = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<IceGigasHalberdHitbox>(), LungeDamage, 7f, Main.myPlayer, NPC.whoAmI);
                Main.projectile[hitbox].timeLeft = hitboxLifetime;
            }

            if (!Main.dedServ)
            {
                HalberdFormAndShatterEffects(LungeFormTicks, LungeEndTick);

                //Path telegraph: from tell start, frost puffs along the floor from the tip's start to where the tip will
                //stop (dash distance ahead), denser through the last 15 ticks.
                bool marking = AttackTimer > LungeFormTicks && AttackTimer < LungeDashStart;

                if (marking)
                {
                    int puffs = AttackTimer > LungeDashStart - 15 ? 3 : 1;
                    float pathStartX = NPC.Center.X + lockedDir * LungeTipReach * 0.5f;
                    float pathEndX = NPC.Center.X + lockedDir * (LungeTipReach + lungeDistance);

                    for (int i = 0; i < puffs; i++)
                    {
                        float pathX = MathHelper.Lerp(pathStartX, pathEndX, Main.rand.NextFloat());
                        Vector2 floorPoint = new Vector2(pathX, NPC.Bottom.Y - 4f);
                        Dust streak = Dust.NewDustPerfect(floorPoint, DustID.Frost, new Vector2(0f, -0.6f), 100, default, 1.1f);
                        streak.noGravity = true;
                    }
                }

                if (AttackTimer == LungeDashStart)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.8f, Pitch = -0.2f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 10, 6f, 500f);
                }

                //Wake: kicked-up frost behind the feet and a trail off the tip while the dash is fast.
                if (dashing)
                {
                    GetHalberdBlade(out _, out Vector2 tip);
                    Dust tipTrail = Dust.NewDustPerfect(tip, DustID.Frost, new Vector2(-lockedDir * 2f, 0f), 80, default, 1.3f);
                    tipTrail.noGravity = true;
                    int kick = Dust.NewDust(new Vector2(NPC.position.X, NPC.Bottom.Y - 8f), NPC.width, 8, DustID.Snow, -lockedDir * 3f, -1.5f, 100, default, 1.2f);
                    Main.dust[kick].noGravity = false;
                }
            }

            if (AttackTimer >= LungeEndTick)
            {
                EndAttack(LungeCooldown);
            }
        }

        // Halberd leap slam. Phase 0: standing planted, the halberd forms and the arm cocks back overhead. Then the server plans an arc
        // to land LeapLandingStandoff short of the player (whole arc checked clear, so it needs real headroom) and
        // launches. Phase 1: in the air, held cocked, chopping down over the last 40% of the flight so the head meets the
        // floor as it lands. Phase 2: embedded, then the pull-free recovery. Live from launch to 4 ticks after landing.
        void RunHalberdLeap(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = SlamPhase < 2 || AttackTimer <= LeapImpactTicks;

            float armAimDegrees = HalberdCockAim;
            float armRate = 0.12f;
            halberdWristLag = HalberdWristLagCocked;

            if (SlamPhase == 0)
            {
                NPC.velocity.X *= 0.8f;

                if (AttackTimer > LeapFormTicks)
                {
                    armAimDegrees = HalberdTellAim;
                    armRate = 0.1f;
                }

                if (!Main.dedServ)
                {
                    HalberdFormAndShatterEffects(LeapFormTicks, int.MaxValue);

                    if (AttackTimer > LeapFormTicks)
                    {
                        Vector2 feet = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - 6f);
                        Dust frost = Dust.NewDustPerfect(feet, DustID.Frost, new Vector2(0f, -1.5f), 100, default, 1.2f);
                        frost.noGravity = true;
                    }
                }

                if (AttackTimer >= LeapPhase0End && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!TryPlanLeap(player, LeapLandingStandoff, LeapMinJump, LeapMaxJump, out Vector2 launch, out int flightTicks))
                    {
                        EndAttack(60); //the player moved somewhere without a clear arc: drop it
                        return;
                    }

                    SlamVelocityX = launch.X;
                    SlamFlightTicks = flightTicks;
                    NPC.velocity = launch;
                    SlamPhase = 1;
                    AttackTimer = 0;
                    NPC.netUpdate = true;

                    int hitbox = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<IceGigasHalberdHitbox>(), LeapDamage, 8f, Main.myPlayer, NPC.whoAmI);
                    Main.projectile[hitbox].timeLeft = flightTicks + 100; //dies on its own once the halberd is put away
                }
            }
            else if (SlamPhase == 1)
            {
                NPC.velocity.X = SlamVelocityX;
                armAimDegrees = HalberdTellAim;
                armRate = 0.15f;

                //Chop: cubic from cocked to the impact pose over the back part of the flight (rate 1: exact per tick).
                float flightFraction = AttackTimer / (float)Math.Max(SlamFlightTicks, 1);
                float chopProgress = (flightFraction - LeapChopStartFraction) / (1f - LeapChopStartFraction);

                if (chopProgress > 0f)
                {
                    float clampedProgress = Math.Min(chopProgress, 1f);
                    float eased = clampedProgress * clampedProgress * clampedProgress;
                    armAimDegrees = MathHelper.Lerp(HalberdTellAim, HalberdImpactAim, eased);
                    armRate = 1f;
                    halberdWristLag = MathHelper.Lerp(HalberdWristLagCocked, HalberdWristLagImpact, eased);
                }

                if (AttackTimer == 1 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.8f, Pitch = -0.2f }, NPC.Bottom);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.7f }, NPC.Center);
                }

                bool landed = AttackTimer > 3 && NPC.velocity.Y == 0f && NPC.oldVelocity.Y > 0f && NPC.collideY;
                bool flightTimedOut = AttackTimer > SlamFlightTicks + 90; //landing terrain changed after commitment

                if ((landed || flightTimedOut) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SlamPhase = 2;
                    AttackTimer = 0;
                    NPC.velocity.X = 0f;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                NPC.velocity.X *= 0.7f;
                armAimDegrees = HalberdImpactAim;
                armRate = 1f;
                halberdWristLag = HalberdWristLagImpact;

                if (AttackTimer > LeapImpactTicks)
                {
                    armAimDegrees = HalberdRecoveryAim;
                    armRate = 0.05f;
                }

                //Latched locally so latency can't skip the impact (phase 2 may arrive a tick late on clients).
                if (!SlamImpactPlayed && !Main.dedServ)
                {
                    SlamImpactPlayed = true;
                    GetHalberdBlade(out _, out Vector2 tip);
                    HalberdImpactEffects(tip, 9f);
                }

                if (!Main.dedServ)
                {
                    HalberdFormAndShatterEffects(-1, LeapLandedEnd);
                }

                if (AttackTimer >= LeapLandedEnd)
                {
                    EndAttack(LeapCooldown);
                }
            }

            armRequested = true;
            armAimTarget = MathHelper.ToRadians(armAimDegrees);
            armAimRate = armRate;
        }

        // Shared halberd presentation, client-only: frost motes drifting onto the pole while it forms (the weapon freezes
        // together) and an ice shatter along it over its last HalberdFadeTicks. formTicks / endTick are the calling
        // attack's form length and final tick.
        void HalberdFormAndShatterEffects(int formTicks, int endTick)
        {
            GetHalberdBlade(out Vector2 grip, out Vector2 tip);
            Vector2 pole = tip - grip;

            if (AttackTimer == 1 && formTicks > 0)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.4f }, grip);
            }

            //Motes start 30px off the pole and drift onto it over ~12 ticks. -0.4 along the pole reaches the butt end.
            if (AttackTimer <= formTicks)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 onPole = grip + pole * Main.rand.NextFloat(-0.4f, 1f);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(30f, 30f);
                    Dust mote = Dust.NewDustPerfect(onPole + offset, DustID.Frost, -offset / 12f, 100, default, 1.2f);
                    mote.noGravity = true;
                }
            }

            int fadeStart = endTick - HalberdFadeTicks;

            if (AttackTimer == fadeStart)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.3f }, grip);
            }

            if (AttackTimer >= fadeStart)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 onPole = grip + pole * Main.rand.NextFloat(-0.4f, 1f);
                    Dust.NewDustPerfect(onPole, DustID.Ice, Main.rand.NextVector2Circular(1.5f, 1.5f), 0, default, 1.1f);
                }
            }
        }

        ///<summary>The halberd is in the fist (forming, swinging, stuck or shattering) in either halberd attack.</summary>
        internal bool HalberdOut
        {
            get
            {
                bool cleaving = State == AttackState.HalberdCleave && AttackTimer <= HalberdEndTick;
                bool lunging = State == AttackState.HalberdLunge && AttackTimer <= LungeEndTick;
                bool leaping = State == AttackState.HalberdLeap && !(SlamPhase == 2 && AttackTimer > LeapLandedEnd);
                return cleaving || lunging || leaping;
            }
        }

        ///<summary>The blade deals damage. Cleave: the strike from overhead on, plus the first embedded ticks. Lunge: the
        ///whole dash. Leap: the whole flight, plus the first embedded ticks.</summary>
        internal bool HalberdBladeLive
        {
            get
            {
                bool cleaveLive = State == AttackState.HalberdCleave
                    && AttackTimer >= HalberdStrikeStart + HalberdLiveFromStrikeTick
                    && AttackTimer < HalberdImpactTick + HalberdEmbeddedLiveTicks;
                bool lungeLive = State == AttackState.HalberdLunge
                    && AttackTimer >= LungeDashStart
                    && AttackTimer < LungeFollowStart;
                bool leapAirborne = State == AttackState.HalberdLeap && SlamPhase == 1;
                bool leapEmbedded = State == AttackState.HalberdLeap && SlamPhase == 2 && AttackTimer <= LeapEmbeddedLiveTicks;
                return cleaveLive || lungeLive || leapAirborne || leapEmbedded;
            }
        }

        ///<summary>World position of this frame's shoulder pivot, mirrored with the sprite. Same anchor as PreDraw: frame
        ///centred on the hitbox, bottom 4px below NPC.Bottom plus DrawOffsetY and the stair-step sink.</summary>
        Vector2 ShoulderWorldPosition()
        {
            int frameHeight = Math.Max(NPC.frame.Height, 1);
            int frameIndex = Math.Clamp(NPC.frame.Y / frameHeight, 0, ShoulderPivots.Length - 1);
            Point shoulder = ShoulderPivots[frameIndex];
            Vector2 frameSize = NPC.frame.Size();
            float shoulderX = shoulder.X;

            if (NPC.spriteDirection > 0) //the sheet faces left
            {
                shoulderX = frameSize.X - shoulder.X;
            }

            float drawBottomY = NPC.Bottom.Y + 4f + SpriteDrawOffsetY * NPC.scale + NPC.gfxOffY;
            Vector2 frameTopLeft = new Vector2(NPC.Center.X - frameSize.X * NPC.scale / 2f, drawBottomY - frameSize.Y * NPC.scale);
            Vector2 shoulderWorld = frameTopLeft + new Vector2(shoulderX, shoulder.Y) * NPC.scale;
            return shoulderWorld;
        }

        ///<summary>The halberd's grip (the straight arm's fist) and spear tip in world space, from the current arm aim.
        ///Aim is "degrees below forward", so facing left the world angle mirrors to PI - aim. The hitbox and the draw
        ///both use this line.</summary>
        internal void GetHalberdBlade(out Vector2 grip, out Vector2 tip)
        {
            float poleAim = armAim - MathHelper.ToRadians(halberdWristLag);
            float armWorldAngle = armAim;
            float poleWorldAngle = poleAim;

            if (NPC.spriteDirection < 0)
            {
                armWorldAngle = MathHelper.Pi - armAim;
                poleWorldAngle = MathHelper.Pi - poleAim;
            }

            Vector2 shoulder = ShoulderWorldPosition();
            grip = shoulder + armWorldAngle.ToRotationVector2() * ArmLengthToFist * NPC.scale;
            tip = grip + poleWorldAngle.ToRotationVector2() * HalberdGripToTip * NPC.scale;
        }

        // Frozen Echo. SlamPhase 0: stands planted with frost rising off its feet, then leaps (Glacial Slam's arc solver) to land
        // EchoLandingStandoff short of the player. 1: in flight. 2: freeze over the body, leave the statue right there
        // beside the player, blink 8 tiles back, then move straight on. No valid arc: freeze where it stands, as before.
        void RunFrozenEcho(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = SlamPhase < 2 || AttackTimer <= EchoGatherTicks;

            if (SlamPhase == 0)
            {
                NPC.velocity.X *= 0.8f;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 feet = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - 6f);
                        Dust frost = Dust.NewDustPerfect(feet, DustID.Frost, new Vector2(0f, -1.5f), 100, default, 1.2f);
                        frost.noGravity = true;
                    }
                }

                if (AttackTimer >= Tel(EchoWindupTicks) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    bool leapPlanned = TryPlanLeap(player, EchoLandingStandoff, SlamMinJump, SlamMaxJump, out Vector2 launch, out int flightTicks);
                    SlamPhase = 2;

                    if (leapPlanned)
                    {
                        SlamVelocityX = launch.X;
                        SlamFlightTicks = flightTicks;
                        NPC.velocity = launch;
                        SlamPhase = 1;
                    }

                    AttackTimer = 0;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (SlamPhase == 1)
            {
                NPC.velocity.X = SlamVelocityX;
                bool landed = AttackTimer > 3 && NPC.velocity.Y == 0f && NPC.oldVelocity.Y > 0f && NPC.collideY;
                bool flightTimedOut = AttackTimer > SlamFlightTicks + 90; //landing terrain changed after commitment

                if ((landed || flightTimedOut) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SlamPhase = 2;
                    AttackTimer = 0;
                    NPC.velocity.X = 0f;
                    NPC.netUpdate = true;
                }

                return;
            }

            NPC.velocity.X *= 0.8f;

            if (AttackTimer == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.7f, Pitch = -0.2f }, NPC.Bottom);
            }

            if (AttackTimer <= EchoGatherTicks)
            {
                //Mist densifying over the whole body — it's "freezing" in place
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.2f;
                }
            }

            if (AttackTimer == EchoGatherTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Leave the statue exactly where it stood...
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Bottom.X, (int)NPC.Bottom.Y, ModContent.NPCType<FrozenGigasStatue>(), 0, 300f);
                //...and blink away behind itself
                float destX = NPC.Center.X - NPC.direction * 8 * 16f;
                float groundY = FindGroundY(new Vector2(destX, NPC.Bottom.Y - 48f), 12);

                if (groundY > 0f)
                {
                    BlinkFlash();
                    NPC.Bottom = new Vector2(destX, groundY);
                    NPC.velocity = Vector2.Zero;
                    BlinkFlash();
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer >= EchoGatherTicks + 15)
            {
                // The statue is the pressure, so go straight into the next attack instead of the usual 3-4.5s
                // walk-around cooldown. EndAttack(0) still adds its 0-99t jitter, so zero it after.
                EndAttack(0);

                if (State == AttackState.None && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AttackCooldown = 0;
                    NPC.netUpdate = true;
                }
            }
        }

        // Elevated Winter's Grasp: one orb, or a simultaneous opposite-side pair below 50%.
        // Both the orb and its complete icicle ring must fit in clear air, 5-12 tiles higher.
        void RunWintersGrasp(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Tel(GraspCastTicks);
            globalNPC.AttackCommitted = AttackTimer <= cast;
            NPC.velocity.X *= 0.8f;
            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                if (AttackTimer <= cast && Main.rand.NextBool(2))
                {
                    Vector2 velocity = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 6f;
                    Dust dust = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 20f, -20f), DustID.Frost, velocity, 100, default, 1.2f);
                    dust.noGravity = true;
                }
            }
            if (AttackTimer == 10 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int side = Main.rand.NextBool() ? 1 : -1;
                int heightStart = Main.rand.Next(8);
                bool pair = NPC.life < NPC.lifeMax * 0.5f;
                bool firstFound = TryFindGraspPosition(player, side, heightStart, out Vector2 first);
                bool secondFound = TryFindGraspPosition(player, -side, heightStart, out Vector2 second);
                if (firstFound)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), first, Vector2.Zero,
                        ModContent.ProjectileType<GigasFrostZone>(), ZoneDamage, 2f, Main.myPlayer);
                if (secondFound && (pair || !firstFound))
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), second, Vector2.Zero,
                        ModContent.ProjectileType<GigasFrostZone>(), ZoneDamage, 2f, Main.myPlayer);
                // Each side is >=200px from the player: pairs stay >=400px apart in SP/MP.
                // Skip an entirely obstructed side instead of putting the orb inside solid tiles.
            }
            if (AttackTimer >= cast + 15) EndAttack(200);
        }

        static bool TryFindGraspPosition(Player player, int side, int heightStart, out Vector2 position)
        {
            for (int horizontal = 200; horizontal <= 488; horizontal += 16)
            {
                for (int step = 0; step < 8; step++)
                {
                    int height = 5 + (heightStart + step) % 8;
                    Vector2 candidate = player.Center + new Vector2(side * horizontal, -height * 16f);
                    const int clearance = 80; // Orb + icicle ring, not just its center tile.
                    if (candidate.X < clearance + 16 || candidate.Y < clearance + 16
                        || candidate.X > Main.maxTilesX * 16f - clearance - 16
                        || candidate.Y > Main.maxTilesY * 16f - clearance - 16) continue;
                    if (Collision.SolidCollision(candidate - new Vector2(clearance), clearance * 2, clearance * 2)
                        || !Collision.CanHitLine(player.Center, 1, 1, candidate, 1, 1)) continue;
                    position = candidate;
                    return true;
                }
            }
            position = Vector2.Zero;
            return false;
        }


        void RunAbsoluteZero(tsorcRevampGlobalNPC globalNPC)
        {
            //NOT halved on chain — it never chains, and the channel IS the stagger window
            if (AttackTimer <= ZeroChannelTicks)
            {
                NPC.velocity.X *= 0.75f;
                if (AttackTimer < ZeroStaggerableTicks)
                {
                    globalNPC.AttackTelegraphing = true; //staggerable: a normal poise break cancels the channel
                }
                else
                {
                    globalNPC.AttackCommitted = true;
                }
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.7f }, NPC.Center);
                }
                if (AttackTimer == ZeroStaggerableTicks)
                {
                    //Commit cue: too late to interrupt now
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.9f, Pitch = 0.3f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Cyan));
                    }
                }
                //Blizzard spiralling inward as the cold gathers
                float progress = AttackTimer / (float)ZeroChannelTicks;
                int count = 2 + (int)(progress * 4f);
                for (int i = 0; i < count; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(180f, 20f, progress) + Main.rand.NextFloat(20f);
                    Vector2 pos = NPC.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (NPC.Center - pos) * 0.06f + angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.2f;
                }
                if (AttackTimer == ZeroChannelTicks)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1f, Pitch = -0.7f }, NPC.Center);
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, vel.X, vel.Y, 40, default, 2f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<GigasFreezeRing>(), RingDamage, 8f, Main.myPlayer, 300f);
                    }
                }
            }
            else
            {
                NPC.velocity.X = 0f; //post-burst recovery: vulnerable
                if (AttackTimer >= ZeroChannelTicks + ZeroRecoveryTicks)
                {
                    EndAttack(190);
                }
            }
        }

        void RunIcePrison(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Tel(PrisonCastTicks);
            globalNPC.AttackCommitted = AttackTimer <= cast;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
            }
            if (AttackTimer == 15 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Cage of pillars on a ring around the player, with ONE gap left open
                Vector2 center = player.Center;
                int gap = Main.rand.Next(10);
                for (int i = 0; i < 10; i++)
                {
                    if (i == gap)
                    {
                        continue;
                    }
                    float angle = MathHelper.TwoPi * i / 10f;
                    Vector2 pos = center + angle.ToRotationVector2() * 128f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                        ModContent.ProjectileType<GigasPrisonPillar>(), PrisonPillarDamage, 1f, Main.myPlayer, 20f, 70f);
                }
            }
            if (AttackTimer >= cast + 20)
            {
                EndAttack(200);
            }
        }

        void RunGlacialStampede(tsorcRevampGlobalNPC globalNPC)
        {
            int tel = Tel(StampedeTelegraphTicks);
            if (AttackTimer <= tel)
            {
                globalNPC.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;
                lockedDir = NPC.direction;
                //Stomping rumble building to the charge
                if (AttackTimer % 10 == 1)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.7f, Pitch = -0.3f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 2.5f, 8, 6f, 400f);
                }
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.Bottom.Y - 10f), NPC.width, 10, DustID.Frost, 0f, -2f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
            }
            else if (AttackTimer <= tel + StampedeChargeTicks)
            {
                //The charge: its only fast move, saved for the phase where it's shocking
                globalNPC.AttackCommitted = true;
                NPC.velocity.X = lockedDir * 4f;
                int tick = AttackTimer - tel;
                if (tick % 10 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.6f, Pitch = -0.1f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 2f, 6, 6f, 350f);
                    //Spikes erupt in its WAKE — chasing it into the charge is punished
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SpawnSpike(NPC.Center.X - lockedDir * 40f, 8, 0.9f, SpikeDamage);
                    }
                }
                //Hail shaken loose from the ceiling ahead
                if (tick % 15 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float aheadX = NPC.Center.X + lockedDir * 96f;
                    float ceilingY = FindCeilingY(new Vector2(aheadX, NPC.Center.Y), 12);
                    if (ceilingY > 0f)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(aheadX + Main.rand.NextFloat(-20f, 20f), ceilingY + 10f), new Vector2(0f, 2f),
                            ModContent.ProjectileType<GigasIceShard>(), StampedeShardDamage, 1f, Main.myPlayer, 0.25f);
                    }
                }
                //Ran into a wall: skip straight to the recovery
                if (NPC.collideX && tick > 10)
                {
                    AttackTimer = tel + StampedeChargeTicks;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                //Exhausted: the biggest punish window in the kit
                NPC.velocity.X *= 0.85f;
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(NPC.Center + new Vector2(NPC.direction * 20f, -30f), 8, 8, DustID.Frost, NPC.direction * 0.5f, 0.5f, 140, default, 1.1f);
                    Main.dust[dust].velocity *= 0.3f;
                }
                if (AttackTimer >= tel + StampedeChargeTicks + StampedeRecoveryTicks)
                {
                    EndAttack(170);
                }
            }
        }

        void RunMirrorOfWinter(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int cast = Tel(MirrorCastTicks);
            globalNPC.AttackCommitted = AttackTimer <= cast;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.9f, Pitch = -0.8f }, NPC.Center);
            }
            if (AttackTimer == 15 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Two ARMED statues rise at the player's flanks — three giants, one real, a timer ticking
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = player.Center.X + side * 6 * 16f;
                    float groundY = FindGroundY(new Vector2(x, player.Bottom.Y - 48f), 20);
                    if (groundY > 0f)
                    {
                        NPC.NewNPC(NPC.GetSource_FromAI(), (int)x, (int)groundY, ModContent.NPCType<FrozenGigasStatue>(), 0, 360f, 1f);
                    }
                }
                //...and the real one blinks away
                float destX = NPC.Center.X - NPC.direction * 8 * 16f;
                float destGround = FindGroundY(new Vector2(destX, NPC.Bottom.Y - 48f), 12);
                if (destGround > 0f)
                {
                    BlinkFlash();
                    NPC.Bottom = new Vector2(destX, destGround);
                    NPC.velocity = Vector2.Zero;
                    BlinkFlash();
                    NPC.netUpdate = true;
                }
            }
            if (AttackTimer >= cast + 15)
            {
                EndAttack(220);
            }
        }

        void RunCrackTransition(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.AttackCommitted = true; //the crack itself is not an opening
            NPC.velocity.X = 0f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1f, Pitch = -0.8f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 6f, 15);
            }
            //Cracks of cold light bursting outward
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceRod, vel.X, vel.Y, 60, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            if (AttackTimer >= CrackTicks)
            {
                EndAttack(60);
            }
        }

        void RunComboRecovery()
        {
            //Fully punishable exhausted stand: heavy breath mist sinking off it, no armor at all
            NPC.velocity.X *= 0.85f;
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.Center + new Vector2(NPC.direction * 18f, -30f), 10, 10, DustID.Frost, NPC.direction * 0.4f, 0.6f, 150, default, 1.2f);
                Main.dust[dust].velocity *= 0.3f;
            }
            if (AttackTimer >= ComboRecoveryTicks)
            {
                ComboIndex = 0;
                State = AttackState.None;
                AttackTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AttackCooldown = PendingCooldown + Main.rand.Next(100);
                    NPC.netUpdate = true;
                }
            }
        }

        ///<summary>Counter-stance: an ice shell that stores the hits it absorbs (30% damage soak, see
        ///ModifyIncomingHit) and detonates them back as shards. Stop hitting it, reposition, or commit
        ///and eat the answer. Never chains.</summary>
        void RunRimeWard(tsorcRevampGlobalNPC globalNPC)
        {
            if (AttackTimer <= WardTelegraphTicks + WardHoldTicks)
            {
                globalNPC.AttackCommitted = true;
                NPC.velocity.X *= 0.7f;
                if (AttackTimer == 1)
                {
                    WardStored = 0;
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
                }
                if (AttackTimer <= WardTelegraphTicks)
                {
                    //Shell crystallizing up the body from the feet
                    float progress = AttackTimer / (float)WardTelegraphTicks;
                    for (int i = 0; i < 3; i++)
                    {
                        float y = NPC.Bottom.Y - Main.rand.NextFloat(NPC.height * progress);
                        float x = NPC.position.X + (Main.rand.NextBool() ? -4f : NPC.width);
                        int dust = Dust.NewDust(new Vector2(x, y), 4, 4, DustID.Ice, 0f, 0f, 80, default, 1.2f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 0.2f;
                    }
                }
                else
                {
                    //Holding: perimeter glints, denser the more hits it has stored
                    int density = 2 + WardStored;
                    for (int i = 0; i < density; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 pos = NPC.Center + angle.ToRotationVector2() * new Vector2(38f, 62f);
                        int glint = Dust.NewDust(pos, 4, 4, DustID.IceRod, 0f, 0f, 70, default, 0.9f);
                        Main.dust[glint].noGravity = true;
                        Main.dust[glint].velocity *= 0.15f;
                    }
                    Lighting.AddLight(NPC.Center, 0.15f + WardStored * 0.06f, 0.25f + WardStored * 0.08f, 0.4f + WardStored * 0.1f);
                }
                if (AttackTimer == WardTelegraphTicks + WardHoldTicks)
                {
                    //Detonation: everything it soaked comes back out
                    UsefulFunctions.ScreenShake(NPC.Center, 6f, 12);
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1f, Pitch = -0.6f }, NPC.Center);
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ice, vel.X, vel.Y, 50, default, 1.5f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shards = 2 + WardStored;
                        for (int i = 0; i < shards; i++)
                        {
                            Vector2 vel = (MathHelper.TwoPi * i / shards + Main.rand.NextFloat(0.25f)).ToRotationVector2() * Main.rand.NextFloat(6f, 8f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<GigasIceShard>(), WardShardDamage, 1f, Main.myPlayer, 0.1f);
                        }
                        if (WardStored >= 6)
                        {
                            //Greed answer: a short spike wave on top of the shards
                            for (int dir = -1; dir <= 1; dir += 2)
                            {
                                for (int tile = 2; tile <= 8; tile++)
                                {
                                    SpawnSpike(NPC.Center.X + dir * tile * 16f, (tile - 2) * 3, 1f, SpikeDamage);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X *= 0.8f; //long recovery — the ward was safe time for it, this is yours
                if (AttackTimer >= WardTelegraphTicks + WardHoldTicks + WardRecoveryTicks)
                {
                    EndAttack(200);
                }
            }
        }

        ///<summary>Hoarfrost Creep: pure zone denial. A ~14-tile carpet of IceWave shard segments
        ///dominoes outward from the giant along the floor — or along the CEILING when one hangs near
        ///the player (denying the grapple escape instead) — and stands for ~4 seconds. The answer is
        ///vertical: jump to higher ground, or hug whichever surface it didn't take.</summary>
        void RunHoarfrostCreep(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int tel = Tel(CreepTelegraphTicks);
            globalNPC.AttackCommitted = AttackTimer <= tel;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= tel)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.7f, Pitch = -0.5f }, NPC.Bottom);
                }
                //Frost racing along the ground away from it — the direction and reach of the carpet
                if (Main.rand.NextBool(2))
                {
                    float reach = Main.rand.NextFloat(CreepSegments * 44f);
                    Vector2 pos = new Vector2(NPC.Center.X + NPC.direction * (32f + reach), NPC.Bottom.Y - 6f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, NPC.direction * 1.5f, -1f, 110, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                if (AttackTimer == tel)
                {
                    UsefulFunctions.ScreenShake(NPC.Bottom, 4f, 12);
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.8f, Pitch = -0.3f }, NPC.Bottom);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Ceiling variant when one hangs near the player — deny the grapple escape instead
                        bool ceiling = FindCeilingY(player.Center, 12) > 0f && Main.rand.NextBool();
                        creepWasCeiling = ceiling;

                        //The carpet follows the surface it started on: each column takes the open surface nearest the last
                        //one (up to CreepMaxRiseTiles up, CreepMaxDropTiles down) that has CreepClearanceTiles of open air on
                        //its exposed side. Columns with none are skipped, so it never spawns buried inside a wall.
                        float firstX = NPC.Center.X + NPC.direction * 54f;
                        float runningSurfaceY = NPC.Bottom.Y;

                        if (ceiling)
                        {
                            runningSurfaceY = FindCeilingY(new Vector2(firstX, NPC.Center.Y), 15);
                        }

                        for (int i = 0; i < CreepSegments && runningSurfaceY > 0f; i++)
                        {
                            float x = NPC.Center.X + NPC.direction * (32f + 22f + i * 44f);
                            int tileX = (int)(x / 16f);
                            int runningTileY = (int)(runningSurfaceY / 16f);
                            int bestTileY = -1;
                            int bestStep = int.MaxValue;

                            if (ceiling)
                            {
                                runningTileY -= 1; //runningSurfaceY is the ceiling tile's underside
                            }

                            for (int tileY = runningTileY - CreepMaxRiseTiles; tileY <= runningTileY + CreepMaxDropTiles; tileY++)
                            {
                                if (!WorldGen.InWorld(tileX, tileY - CreepClearanceTiles - 1, 5) || !WorldGen.InWorld(tileX, tileY + CreepClearanceTiles + 1, 5))
                                {
                                    continue;
                                }

                                Tile surfaceTile = Main.tile[tileX, tileY];
                                bool surfaceSolid = surfaceTile.HasTile && !surfaceTile.IsActuated && Main.tileSolid[surfaceTile.TileType];

                                if (!surfaceSolid || (ceiling && Main.tileSolidTop[surfaceTile.TileType]))
                                {
                                    continue;
                                }

                                //Open side: above a floor, below a ceiling. Platforms don't block the clearance.
                                int openDirection = -1;

                                if (ceiling)
                                {
                                    openDirection = 1;
                                }

                                bool clear = true;

                                for (int k = 1; k <= CreepClearanceTiles; k++)
                                {
                                    Tile airTile = Main.tile[tileX, tileY + openDirection * k];
                                    bool blocked = airTile.HasTile && !airTile.IsActuated && Main.tileSolid[airTile.TileType] && !Main.tileSolidTop[airTile.TileType];

                                    if (blocked)
                                    {
                                        clear = false;
                                        break;
                                    }
                                }

                                int step = Math.Abs(tileY - runningTileY);

                                if (clear && step < bestStep)
                                {
                                    bestStep = step;
                                    bestTileY = tileY;
                                }
                            }

                            if (bestTileY < 0)
                            {
                                continue; //no open surface in reach in this column — the carpet skips it
                            }

                            float surfaceY = bestTileY * 16f;

                            if (ceiling)
                            {
                                surfaceY = (bestTileY + 1) * 16f;
                            }

                            runningSurfaceY = surfaceY;

                            //Domino cadence: each segment erupts a beat after the one before it
                            Vector2 center = new Vector2(x, surfaceY - 17f);

                            if (ceiling)
                            {
                                center = new Vector2(x, surfaceY + 17f);
                            }
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), center, Vector2.Zero,
                                ModContent.ProjectileType<GigasIceWaveSegment>(), CreepDamage, 2f, Main.myPlayer, i * CreepDominoDelay, ceiling ? 1f : 0f);
                        }
                    }
                }
            }
            else if (AttackTimer >= tel + CreepRecoveryTicks) //recovery, no armor
            {
                EndAttack(160);
            }
        }

        ///<summary>Blizzard pull: the zone projectile drags players toward it for 90t; anyone still
        ///point-blank at the exhale check eats a mini frost-breath burst.</summary>
        void RunUndertow(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int tel = Tel(UndertowTelegraphTicks);
            globalNPC.AttackCommitted = AttackTimer <= tel + UndertowPullTicks;
            NPC.velocity.X *= 0.8f;
            Vector2 mouth = MouthPosition;

            if (AttackTimer == 1)
            {
                undertowExhale = false;
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.8f }, NPC.Center);
            }
            if (AttackTimer <= tel)
            {
                //Deep inhale: dust lines streaming in from far away
                for (int i = 0; i < 3; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = mouth + angle.ToRotationVector2() * Main.rand.NextFloat(150f, 400f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 120, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (mouth - pos) * 0.05f;
                    Main.dust[dust].fadeIn = 0.3f;
                }
            }
            if (AttackTimer == tel && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<GigasUndertowZone>(), 0, 0f, Main.myPlayer, NPC.whoAmI, UndertowPullTicks);
            }
            //Point-blank exhale: checked once (at the same 78%-through-the-pull point as before the
            //pull got longer), then a 20-tick burst of breath puffs
            int exhaleCheckTick = tel + (int)(UndertowPullTicks * 0.78f);
            if (AttackTimer == exhaleCheckTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                undertowExhale = NPC.Distance(player.Center) < 128f;
            }
            if (undertowExhale && AttackTimer > exhaleCheckTick && AttackTimer <= exhaleCheckTick + 20 && AttackTimer % 2 == 0
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 aim = (player.Center - mouth).SafeNormalize(new Vector2(NPC.direction, 0f));
                Vector2 vel = aim.RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * (7.5f + Main.rand.NextFloat(-1f, 1f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                    ModContent.ProjectileType<GigasFrostBreathPuff>(), BreathDamage, 1f, Main.myPlayer);
            }
            if (AttackTimer >= tel + UndertowPullTicks + UndertowRecoveryTicks)
            {
                //The pull deals no damage itself (GigasUndertowZone) — its only payoff is landing
                //the point-blank exhale, which costs nothing to just resist. A normal EndAttack here
                //would let the whole move whiff for free, so — like Heart of Winter's forced flow
                //into Glacial Stampede — always continue straight into a real ground threat instead
                //of rolling the ordinary chain odds (which, entered via HoarfrostCreep, are often
                //already exhausted for this fight's combo budget).
                StartAttack(AttackState.GlacialSlam);
                return;
            }
            else if (AttackTimer > tel + UndertowPullTicks)
            {
                NPC.velocity.X *= 0.7f; //winded — heavy exhale mist, no armor
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(mouth, 8, 8, DustID.Frost, NPC.direction * 1.5f, 0.5f, 150, default, 1.2f);
                    Main.dust[dust].velocity *= 0.4f;
                }
            }
        }

        ///<summary>Six stalactites over the player's area dropping on an accelerating cadence (gaps
        ///30t → 10t). The giant is free and walking again while the last three are still falling.</summary>
        ///<summary>Crystal Canopy is three DIFFERENT drop patterns in a row, each re-centred on the
        ///player's position AT THAT MOMENT (not where they were at cast start), separated by a pause
        ///where nothing is falling — a rhythm of read-react-read-react rather than one curtain. Only
        ///the first cast (`tel`) shrinks when chained in (Tel()); the internal spacing stays fixed,
        ///matching how HailstoneVolley's burst gaps work.</summary>
        void RunCrystalCanopy(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            //Walking cast like the crown — the threat is in the sky, not the body
            if (!TerrainNavigation.Update(NPC))
            {
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.5f, canTeleport: false, canDodgeroll: false, canPounce: false, minSurfaceWidth: 2, canWalkBackwards: true);
                TerrainNavigation.RecoverFromNarrowPerch(NPC, globalNPC, player);
            }
            FootstepEffects();
            globalNPC.AttackCommitted = AttackTimer <= 35;

            int seqA = Tel(CanopyCastTicks);
            int seqB = seqA + 40; //pause: nothing falls between sequences
            int seqC = seqB + 35;
            float awayFromNPC = NPC.direction; //player already stands in this X direction from the giant

            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = 0.2f }, NPC.Center);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer == seqA)
                {
                    //Sequence A, "Curtain": drop delays close in 15/40/62/80/95/107/115 (gaps
                    //shrink 25→8) across seven columns, ordered FAR-from-the-giant (drops first) to
                    //NEAR-the-giant (drops last) so the safe lane always ends up on the giant's
                    //side, nudged 90px further from the giant than dead-centre on the player.
                    Span<int> delays = stackalloc int[] { 15, 40, 62, 80, 95, 107, 115 };
                    int columns = delays.Length;
                    float centerX = player.Center.X + awayFromNPC * 90f;
                    for (int i = 0; i < columns; i++)
                    {
                        float columnOffset = awayFromNPC * ((columns - 1) / 2f - i) * 78f;
                        SpawnCanopyIcicle(new Vector2(centerX + columnOffset, player.position.Y - 220f), delays[i]);
                    }
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                }
                else if (AttackTimer == seqB)
                {
                    //Sequence B, "Volley": four columns in a tight, fast cluster directly on the
                    //player's CURRENT spot — punishes standing still through the first pause.
                    Span<int> delays = stackalloc int[] { 10, 22, 34, 46 };
                    int columns = delays.Length;
                    for (int i = 0; i < columns; i++)
                    {
                        float columnOffset = ((columns - 1) / 2f - i) * 48f;
                        SpawnCanopyIcicle(new Vector2(player.Center.X + columnOffset, player.position.Y - 220f), delays[i]);
                    }
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = 0.5f }, NPC.Center);
                }
                else if (AttackTimer == seqC)
                {
                    //Sequence C, "Pincer": two columns drop TOGETHER well outside the player on
                    //both sides (a binary "which way do I not go" read), then a single centre
                    //column punishes whichever way they guessed a beat later.
                    float px = player.Center.X;
                    SpawnCanopyIcicle(new Vector2(px - 150f, player.position.Y - 220f), 18);
                    SpawnCanopyIcicle(new Vector2(px + 150f, player.position.Y - 220f), 18);
                    SpawnCanopyIcicle(new Vector2(px, player.position.Y - 220f), 42);
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
                }
            }
            //Recovery starts shortly after the LAST cast, not after its icicles finish resolving —
            //the giant is free and walking again while sequence C's drops are still falling, same
            //overlap design as the original single curtain.
            if (AttackTimer >= seqC + 25)
            {
                EndAttack(160);
            }
        }

        ///<summary>One stalactite for Crystal Canopy: pulled down out of ceilings toward the target
        ///column so it never spawns embedded in solid tile. Shared by all three sequences.</summary>
        void SpawnCanopyIcicle(Vector2 pos, int delay)
        {
            for (int tries = 0; tries < 10 && IsSolidAt(pos); tries++)
            {
                pos.Y += 16f;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                ModContent.ProjectileType<GigasCanopyIcicle>(), CanopyDamage, 2f, Main.myPlayer, delay);
        }

        ///<summary>Black Ice: a stomp glazes a 24-tile strip of floor under the player. Almost no
        ///threat by itself — for 8 seconds every dodge on it overshoots, and the chain into Glacial
        ///Slam is where it collects.</summary>
        void RunBlackIce(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            int tel = Tel(GlazeTelegraphTicks);
            globalNPC.AttackCommitted = AttackTimer <= tel;
            if (AttackTimer == 1)
            {
                NPC.velocity.X = 0f;
                NPC.velocity.Y = -7f; //the stomp wind-up hop (jump frame)
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.7f, Pitch = -0.4f }, NPC.Bottom);
            }
            if (AttackTimer == tel)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 12);
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.9f, Pitch = -0.2f }, NPC.Bottom);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float groundY = FindGroundY(new Vector2(player.Center.X, player.Bottom.Y - 8f), 25);
                    if (groundY > 0f)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(player.Center.X, groundY - 8f), Vector2.Zero,
                            ModContent.ProjectileType<GigasGlazeStrip>(), 0, 0f, Main.myPlayer, 960f); //twice the old 480t (8s) glaze duration
                    }
                }
            }
            if (AttackTimer >= tel + GlazeRecoveryTicks)
            {
                EndAttack(220);
            }
            else if (AttackTimer > tel)
            {
                NPC.velocity.X *= 0.8f; //recovery, no armor
            }
        }

        ///<summary>The 30% surprise: it fake-dies with the full death spectacle, crawls toward the
        ///player as a mound of blizzard, then erupts back to life with a point-blank freeze pulse and
        ///flows straight into a Glacial Stampede. The tell for attentive players: no gore, no coins.</summary>
        void RunHeartOfWinter(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;   //nothing interrupts the rebirth
            NPC.timeLeft = 600;         //don't despawn while invisible
            NPC.dontTakeDamage = AttackTimer < HeartRebirthTick;
            NPC.damage = AttackTimer < HeartRebirthTick ? 0 : BaseContactDamage;

            if (AttackTimer == 1)
            {
                NPC.velocity = Vector2.Zero;
                DeathShatterFX(); //the full death spectacle — identical to the real one
            }
            if (AttackTimer < HeartDeathTick)
            {
                //Body collapsing into the pile (already invisible via PreDraw; the dust IS the collapse)
                for (int i = 0; i < 6; i++)
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Snow, 0f, 2f, 60, default, 1.4f);
                    Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(2f, 4f));
                }
            }
            else if (AttackTimer < HeartCrawlEndTick)
            {
                //The pile: a low mound of frost... that starts crawling toward the player
                float dx = player.Center.X - NPC.Center.X;
                if (AttackTimer > 40 && Math.Abs(dx) > 6 * 16f) //waits a beat, stops 6 tiles short
                {
                    float destX = NPC.Bottom.X + Math.Sign(dx) * 2f;
                    float groundY = FindGroundY(new Vector2(destX, NPC.Bottom.Y - 48f), 6);
                    if (groundY > 0f)
                    {
                        NPC.Bottom = new Vector2(destX, groundY);
                    }
                }
                NPC.velocity = Vector2.Zero;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - Main.rand.NextFloat(22f));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, -0.4f, 110, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.25f;
                }
                if (Main.rand.NextBool(4))
                {
                    int glint = Dust.NewDust(new Vector2(NPC.position.X, NPC.Bottom.Y - 20f), NPC.width, 20, DustID.IceRod, 0f, 0f, 90, default, 0.9f);
                    Main.dust[glint].noGravity = true;
                    Main.dust[glint].velocity *= 0.1f;
                }
                Lighting.AddLight(NPC.Bottom - new Vector2(0f, 10f), 0.15f, 0.25f, 0.4f);
            }
            else if (AttackTimer < HeartRebirthTick)
            {
                //Reform: the eruption column IS the telegraph
                NPC.velocity = Vector2.Zero;
                if (AttackTimer == HeartCrawlEndTick)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                }
                if (AttackTimer == HeartCrawlEndTick + 15)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.9f, Pitch = 0.4f }, NPC.Center);
                }
                for (int i = 0; i < 5; i++)
                {
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - 4f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.IceTorch, 0f, 0f, 70, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(4f, 9f));
                }
                Lighting.AddLight(NPC.Center, 0.4f, 0.6f, 0.9f);
            }
            if (AttackTimer == HeartRebirthTick)
            {
                //Rebirth: visible again, point-blank freeze pulse, straight into the stampede
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 15);
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, vel.X, vel.Y, 40, default, 1.8f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<GigasFreezeRing>(), HeartRingDamage, 8f, Main.myPlayer, 96f);
                }
                StartAttack(AttackState.GlacialStampede); //the reveal flows into its scariest move
            }
        }

        #endregion

        #region Presence: footsteps, aura, blink

        void FootstepEffects()
        {
            if (Main.dedServ) return;
            if (NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 0.15f)
            {
                FootstepTimer++;
                if (FootstepTimer >= 26)
                {
                    FootstepTimer = 0;
                    FootstepCount++;
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.5f, Pitch = -0.1f }, NPC.Bottom);
                    if (FootstepCount % 3 == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.25f, Pitch = 0.5f }, NPC.Bottom); //ice crunch layer
                    }
                    UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8, 6f, 350f);
                    for (int i = 0; i < 4; i++)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.Bottom.Y - 6f), NPC.width, 6, DustID.Snow, NPC.velocity.X * 0.3f, -0.5f, 120, default, 0.9f);
                        Main.dust[dust].velocity *= 0.4f;
                    }
                    //Every 4th step leaves a cold glint where it trod
                    if (FootstepCount % 4 == 0)
                    {
                        int glint = Dust.NewDust(NPC.Bottom + new Vector2(-8f, -6f), 16, 6, DustID.IceRod, 0f, 0f, 80, default, 1f);
                        Main.dust[glint].noGravity = true;
                        Main.dust[glint].velocity *= 0.1f;
                    }
                }
            }
        }

        ///<summary>Cold aura: pale blue light ramping as it nears death, pulsing in the opening frames of
        ///every telegraph. Once Cracked, the body constantly sheds crystalline glints.</summary>
        void UpdateAura()
        {
            if (HeartHidden)
            {
                return; //the pile does its own faint glow — a full body aura would give it away
            }
            float intensity = 0.3f + 0.7f * (1f - NPC.life / (float)NPC.lifeMax);
            if (State != AttackState.None && State != AttackState.ComboRecovery && AttackTimer < 20)
            {
                intensity += 0.6f;
            }
            Lighting.AddLight(NPC.Center, intensity * 0.4f, intensity * 0.7f, intensity);
            //Idle frost mist — it reads cold even doing nothing
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int mist = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, -0.4f, 150, default, 0.9f);
                Main.dust[mist].noGravity = true;
                Main.dust[mist].velocity *= 0.25f;
            }
            if (Cracked && Main.rand.NextBool(6))
            {
                int glint = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceRod, 0f, 0f, 80, default, 0.9f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.2f;
            }
        }

        void BlinkFlash()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.3f }, NPC.Center);
            for (int burst = 0; burst < 25; burst++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, 0, 0, 80, default, 1.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(7f, 7f);
            }
        }

        #endregion

        #region Helpers

        ///<summary>Ground spike at the given X (ground-snapped near the giant's foot level), with an
        ///eruption delay — the building block of the slam waves, Rimefang and the stampede trail.</summary>
        ///<summary>Server-side. breathCloud: the spike also puffs a short ring of frost breath when it bursts (Rimefang).</summary>
        void SpawnSpike(float x, int delayTicks, float heightScale, int damage, float frostRange = 32f, bool breathCloud = false)
        {
            float groundY = FindGroundY(new Vector2(x, NPC.Bottom.Y - 48f), 10);
            if (groundY < 0f)
            {
                return;
            }
            float height = 54f * heightScale;
            int spike = Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY - height / 2f), Vector2.Zero,
                ModContent.ProjectileType<GigasIceSpike>(), damage, 3f, Main.myPlayer, delayTicks, heightScale, frostRange);

            //Only read by the spike's server-side OnKill, so it needs no sync.
            if (Main.projectile[spike].ModProjectile is GigasIceSpike spikeProjectile)
            {
                spikeProjectile.BreathCloud = breathCloud;
            }
        }

        ///<summary>World Y of the first solid tile top under the point (scanning down), or -1 if none in range.</summary>
        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int depth = 0; depth <= maxTilesDown; depth++)
            {
                int y = tileY + depth;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tileX, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }

        static bool IsSolidAt(Vector2 worldPos)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 5)
            {
                return true;
            }
            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        ///<summary>World Y of the first solid tile BOTTOM above the point (scanning up), or -1 if none in range.</summary>
        static float FindCeilingY(Vector2 worldPos, int maxTilesUp)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int depth = 0; depth <= maxTilesUp; depth++)
            {
                int y = tileY - depth;
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

        #endregion

        #region Shader VFX

        //Shared ice palette. Outer is deep enough to OCCLUDE (premultiplied alpha carries the dark
        //interior through alpha, not by painting a dark colour); Core is deliberately short of pure
        //white, which dissolves every ramp it tops.
        static readonly Color IceOuter = new Color(18, 40, 68);
        static readonly Color IceMid = new Color(88, 168, 226);
        static readonly Color IceCore = new Color(205, 236, 250);

        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> wardEffect;
        static Asset<Texture2D> iceCellNoise;
        static Asset<Texture2D> iceCrackNoise;


        //Rime Ward ellipse, ~6px outside the perimeter glint dust RunRimeWard already spawns at
        //NPC.Center + dir * (38, 62), so the shader and the particles agree on where the shell is.
        //The shell's jagged boundary peaks at 1.08x these axes (47.5 x 75.6px); the quad adds ~14px
        //of margin beyond that so the ellipse never meets the quad edge as a flat cut.
        const float WardAxisX = 44f;
        const float WardAxisY = 70f;
        const float WardQuadWidth = 124f;
        const float WardQuadHeight = 180f;

        static void LoadShaderAssets()
        {
            wardEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/IceGigasRimeWard", AssetRequestMode.ImmediateLoad);
            //Flat-shaded polygons used as crystal facet GEOMETRY, not as a smooth modulator
            iceCellNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "VoronoiNoise", AssetRequestMode.ImmediateLoad);
            iceCrackNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Noise_Wo14", AssetRequestMode.ImmediateLoad);
        }

        ///<summary>Spritebatch plumbing for the NPC-attached ice shell. The caller picks
        ///its technique and sets its own bespoke uniforms first; this binds the detail sampler, sets
        ///the palette/time/pixel-grid every ice technique shares, draws the quad and restores state.
        ///Called from the rime-ward draw; traveling breath material is projectile-owned.</summary>
        static void DrawIceShaderQuad(Effect effect, Texture2D primary, Texture2D detail,
            Vector2 worldCenter, Vector2 drawSize, float rotation, float opacity)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = detail;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.Parameters["OuterColor"]?.SetValue(IceOuter.ToVector3());
                effect.Parameters["MiddleColor"]?.SetValue(IceMid.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(IceCore.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"]?.SetValue(drawSize);
                //Pre-divided 2px pixel grid. Deriving this in HLSL instead costs ~12 per-pixel slots
                //at ps_2_0 (no preshader), which is the difference between fitting Reach and not.
                Vector2 pixelBlocks = Vector2.Max(drawSize, Vector2.One) * 0.5f;
                effect.Parameters["PixelGrid"]?.SetValue(
                    new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White,
                    rotation, primary.Size() * 0.5f, drawSize / primary.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }


        ///<summary>The Rime Ward shell: a thick jagged crust of ice around the whole body, rising
        ///from the feet as it forms and glittering harder the more hits it has stored. Drawn behind
        ///the sprite so the giant stays readable inside its own armour.</summary>
        void DrawRimeWardShell()
        {
            if (AttackTimer > WardTelegraphTicks + WardHoldTicks)
            {
                return; //already detonated — the shards are the effect now, not the shell
            }

            float rise = MathHelper.Clamp(AttackTimer / (float)WardTelegraphTicks, 0f, 1f);
            float stored = MathHelper.Clamp(WardStored / 8f, 0f, 1f); //8 = the RegisterHit cap

            LoadShaderAssets();
            Effect effect = wardEffect.Value;
            effect.CurrentTechnique = effect.Techniques["IceGigasRimeWard"];
            float time = Main.GlobalTimeWrappedHourly;
            effect.Parameters["ShellAxis"].SetValue(new Vector2(WardAxisX, WardAxisY));
            effect.Parameters["StoredAlphaGain"].SetValue(0.78f + stored * 0.16f);
            effect.Parameters["StoredCoreGain"].SetValue(0.55f + stored * 0.45f);
            effect.Parameters["GlintGain"].SetValue(0.6f + stored * 1.4f);
            //Constant scroll rates only: scaling Time by anything that CHANGES during the effect
            //(rise, stored) would jump the phase by whole periods, since Time is a free-running clock.
            effect.Parameters["EdgePan"].SetValue(0.5f + time * 0.008f);
            effect.Parameters["FillPan"].SetValue(0.5f + time * 0.030f);
            effect.Parameters["CrackPan"].SetValue(time * 0.105f);
            effect.Parameters["RiseOffset"].SetValue(rise - 1f);

            Vector2 center = NPC.Center + new Vector2(0f, NPC.gfxOffY + SpriteDrawOffsetY);
            DrawIceShaderQuad(effect, iceCellNoise.Value, iceCrackNoise.Value, center,
                new Vector2(WardQuadWidth, WardQuadHeight), 0f, 0.92f);
        }

        #endregion

        ///<summary>Pale-blue afterimage trail during the stampede charge (and any other high-speed moment), then the
        ///body and front-arm layers (walk arm, or the straight arm rotated about the shoulder). Also hides the body
        ///entirely during Heart of Winter's fake death.</summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (HeartHidden)
            {
                return false; //"dead" — the crawling pile is dust only
            }

            //Shader layers go down BEFORE the body (and dust draws in a later pass entirely), so
            //both land behind the existing sprite and particle work rather than covering it.

            if (!Main.dedServ && State == AttackState.RimeWard)
            {
                DrawRimeWardShell();
            }
            bool stampeding = State == AttackState.GlacialStampede && AttackTimer > Tel(StampedeTelegraphTicks) && AttackTimer <= Tel(StampedeTelegraphTicks) + StampedeChargeTicks;
            if (stampeding || NPC.velocity.Length() > 8f)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height); //bottom-center
                for (int k = NPC.oldPos.Length - 1; k > 0; k -= 2)
                {
                    //This is a hand-rolled draw, so it does not get DrawOffsetY from NPCAddHeight
                    //the way the real sprite does — apply the same shift or the trail detaches.
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height + NPC.gfxOffY + SpriteDrawOffsetY) - screenPos;
                    Color color = new Color(140, 210, 255, 0) * ((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length) * 0.6f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }

            //Body + arm layers replace vanilla's single-sheet draw. Same anchor vanilla's DrawNPC uses: frame centred
            //on the hitbox, its bottom 4px below NPC.Bottom plus DrawOffsetY and the stair-step sink (gfxOffY).
            bodyTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(IceGigas)) + "_Body", AssetRequestMode.ImmediateLoad);
            walkArmTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(IceGigas)) + "_Arm", AssetRequestMode.ImmediateLoad);
            straightArmTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(IceGigas)) + "_ArmStraight", AssetRequestMode.ImmediateLoad);
            halberdTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(IceGigas)) + "_Halberd", AssetRequestMode.ImmediateLoad);

            bool flipped = NPC.spriteDirection > 0; //the sheet faces left
            SpriteEffects bodyEffects = SpriteEffects.None;

            if (flipped)
            {
                bodyEffects = SpriteEffects.FlipHorizontally;
            }

            Color bodyColor = NPC.GetAlpha(drawColor);
            Vector2 frameSize = NPC.frame.Size();
            Vector2 frameOrigin = frameSize / 2f;
            float drawBottomY = NPC.Bottom.Y + 4f + SpriteDrawOffsetY * NPC.scale + NPC.gfxOffY;
            Vector2 drawCenter = new Vector2(NPC.Center.X, drawBottomY - frameSize.Y * NPC.scale / 2f) - screenPos;

            spriteBatch.Draw(bodyTexture.Value, drawCenter, NPC.frame, bodyColor, NPC.rotation, frameOrigin, NPC.scale, bodyEffects, 0f);

            if (!armStraight)
            {
                spriteBatch.Draw(walkArmTexture.Value, drawCenter, NPC.frame, bodyColor, NPC.rotation, frameOrigin, NPC.scale, bodyEffects, 0f);
                return false;
            }

            //Straight arm: pivot on this frame's pauldron centre, mirrored with the sprite. The texture already points
            //StraightArmRestAim below forward, so rotate by the difference. Facing left, "further down" is
            //counter-clockwise on screen (negative rotation); facing right it is clockwise — hence × spriteDirection.
            Vector2 shoulderScreen = ShoulderWorldPosition() - screenPos;
            float armRotation = (armAim - StraightArmRestAim) * NPC.spriteDirection;

            //SpriteBatch measures the origin on the flipped image, so a mirrored draw mirrors the pivot too.
            Texture2D straightArm = straightArmTexture.Value;
            Vector2 armOrigin = StraightArmPivot;

            if (flipped)
            {
                armOrigin.X = straightArm.Width - StraightArmPivot.X;
            }

            //Halberd under the arm, so the fist covers the shaft. Rotated so grip → tip runs along the pole line
            //GetHalberdBlade computes; a mirrored draw mirrors the grip and the texture's tip angle.
            if (HalberdOut)
            {
                GetHalberdBlade(out Vector2 grip, out Vector2 tip);
                float poleWorldAngle = (tip - grip).ToRotation();
                Texture2D halberd = halberdTexture.Value;
                Vector2 halberdOrigin = HalberdGrip;
                float halberdRotation = poleWorldAngle - HalberdTipAngle;

                if (flipped)
                {
                    halberdOrigin.X = halberd.Width - HalberdGrip.X;
                    halberdRotation = poleWorldAngle - (MathHelper.Pi - HalberdTipAngle);
                }

                //Fade in over the form phase, out over the last HalberdFadeTicks. The leap restarts AttackTimer each
                //SlamPhase: it forms in phase 0, is solid in flight, and fades at the end of phase 2.
                int formTicks = HalberdFormTicks;
                int endTick = HalberdEndTick;

                if (State == AttackState.HalberdLunge)
                {
                    formTicks = LungeFormTicks;
                    endTick = LungeEndTick;
                }

                if (State == AttackState.HalberdLeap)
                {
                    formTicks = LeapFormTicks;
                    endTick = int.MaxValue;

                    if (SlamPhase > 0)
                    {
                        formTicks = 1;
                    }

                    if (SlamPhase == 2)
                    {
                        endTick = LeapLandedEnd;
                    }
                }

                float formOpacity = MathHelper.Clamp(AttackTimer / (float)formTicks, 0f, 1f);
                float fadeOpacity = MathHelper.Clamp((endTick - (float)AttackTimer) / HalberdFadeTicks, 0f, 1f);
                float halberdOpacity = Math.Min(formOpacity, fadeOpacity);
                Color halberdColor = bodyColor * halberdOpacity;

                spriteBatch.Draw(halberd, grip - screenPos, null, halberdColor, halberdRotation, halberdOrigin, NPC.scale, bodyEffects, 0f);
            }

            spriteBatch.Draw(straightArm, shoulderScreen, null, bodyColor, armRotation, armOrigin, NPC.scale, bodyEffects, 0f);
            return false;
        }

        ///<summary>Runs after vanilla's zombie framing (AnimationType 28): the jump frame while the halberd lunge dashes or
        ///the halberd leap is in the air. Vanilla's walk/idle framing takes back over the tick it stops.</summary>
        public override void FindFrame(int frameHeight)
        {
            bool lungeDashing = State == AttackState.HalberdLunge && AttackTimer >= LungeDashStart && AttackTimer < LungeFollowStart;
            bool leapAirborne = State == AttackState.HalberdLeap && SlamPhase == 1;

            if (lungeDashing || leapAirborne)
            {
                NPC.frame.Y = JumpFrame * frameHeight;
            }
        }

        ///<summary>Vanilla anchors the floating health bar to the 52x110 HITBOX, but the sprite
        ///frame is 176x170 — about 60px of head and shoulders overhang above the hitbox with
        ///nothing in NPC.height to tell vanilla about it, so the default bar floats around
        ///mid-torso. Anchor explicitly to the visual sprite top instead.</summary>
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            const float frameHeight = 170f;
            position.X = NPC.Center.X;
            position.Y = NPC.Bottom.Y - frameHeight + NPC.gfxOffY + SpriteDrawOffsetY - 16f; //16px clearance above the head
            return true;
        }

        ///<summary>Death spectacle: flash-freeze, then the shatter. Heart of Winter replays this
        ///exact spectacle for its fake death — the only tell is the missing gore and coins.</summary>
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
            {
                return;
            }
            DeathShatterFX();
        }

        void DeathShatterFX()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1f, Pitch = -0.6f }, NPC.Center);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            for (int i = 0; i < 50; i++)
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.Ice, 0f, 0f, 40, default, Main.rand.NextFloat(1.4f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(6f, 6f);
            }
            for (int i = 0; i < 15; i++)
            {
                int glint = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceRod, 0f, -3f, 60, default, 1.2f);
                Main.dust[glint].noGravity = true;
            }
        }

        public override void OnKill()
        {
            //Register the first kill (server-side) so the guaranteed-tome rule flips to the 25% repeat rate
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Terraria.ModLoader.Config.NPCDefinition definition = new(NPC.type);
                if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
                {
                    tsorcRevampWorld.NewSlain.Add(definition, 1);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore3").Type, 1.1f);
            }
        }
    }
}
