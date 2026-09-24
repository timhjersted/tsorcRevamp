using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Chaos;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    ///<summary>
    ///Chaos: the Fiend of Chaos, SuperHardMode boss at tier 20.92.
    ///
    ///Structured as one state machine: every attack runs for a fixed number of ticks, stops spawning, then
    ///hands off to a shared Recovery window that glides Chaos into the player's reach and announces the next
    ///attack. Attacks are dealt from a shuffled per-phase bag, so the order is unpredictable but every card
    ///still comes up — which is why each one has to telegraph itself (see PlayTell).
    ///
    ///The sprite is an 8-frame wing-flap loop and nothing else: no arm, claw or crouch poses exist. Every
    ///tell and every hazard is therefore built from dust, shader projectiles, flap SPEED and draw tint.
    ///</summary>
    [AutoloadBossHead]
    class Chaos : ModNPC, IStaggerable, IDebugAttackLabel
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 130;
            NPC.height = 160;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.npcSlots = 100;
            NPC.aiStyle = -1;
            AnimationType = -1;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.damage = 100;
            NPC.defense = 80;
            NPC.lifeMax = 450000;
            NPC.value = 700000f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            // Rarity slot for the Boss Hunting Tome clue list — clue 42 is BossChecklist.ChaosDesc.
            // Was missing on Chaos, causing the hover hint to fall through to clue 0/1 (Leonhard).
            NPC.rarity = 42;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Chaos.DespawnHandler"), Color.Yellow, DustID.GoldFlame);
        }

        NPCDespawnHandler despawnHandler;

        #region State machine

        public enum AttackState : byte
        {
            Recovery = 0,
            PhaseTransition,
            FireballFan,
            ScytheLunge,
            CarpetBomb,
            FlameHover,
            RocketDash,
            FireballStorm,
            CataclysmDive,
            ShadowflameTeleport,
            LaserGrid,
            OrbitalCosmos,
            WingGale,
            VoidSingularity,
            FiendsBrood
        }

        AttackState State = AttackState.Recovery;
        AttackState NextAttack = AttackState.FireballFan;
        // Seeded to the opening attack too, so the first bag draw won't immediately repeat it.
        AttackState LastAttack = AttackState.FireballFan;

        /// <summary>DebugMode above-head readout (see IDebugAttackLabel). Names the phase alongside the move,
        /// and during Recovery names the card the bag has ALREADY drawn: with a shuffled order that upcoming
        /// pick is the most useful thing on screen while testing, and it is what the tell is announcing.</summary>
        public string DebugAttackLabel
        {
            get
            {
                if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0)
                {
                    return "Staggered";
                }

                if (State == AttackState.PhaseTransition)
                {
                    return "Phase " + phase + " Transition";
                }

                string label = "P" + phase + " " + DebugLabels.Humanize(State.ToString());

                if (State == AttackState.Recovery)
                {
                    return label + " -> " + DebugLabels.Humanize(NextAttack.ToString());
                }

                // The dive is a five-part sub-machine, so the state name alone doesn't say where it is.
                if (State == AttackState.CataclysmDive)
                {
                    string subPhase = "Climb";

                    if (divePhase == DivePhaseHold)
                    {
                        subPhase = "Hold";
                    }
                    else if (divePhase == DivePhasePlunge)
                    {
                        subPhase = "Plunge";
                    }
                    else if (divePhase == DivePhaseGrounded)
                    {
                        subPhase = "Grounded";
                    }
                    else if (divePhase == DivePhaseRise)
                    {
                        subPhase = "Rise";
                    }

                    return label + " (" + subPhase + ")";
                }

                return label;
            }
        }

        int AttackTimer;
        int recoveryLength = OpeningRecoveryTicks;
        int phase = 1;

        // True for the tick a state change happened, so the new state's tick 0 actually executes instead of
        // being skipped by the end-of-AI increment.
        bool stateJustChanged;

        // Per-attack working state. All of it is cleared by StartAttack: under the bag any attack can follow
        // any other, and the old fixed 1->2->3 order was the only thing keeping these from leaking between runs.
        int flyPosition;
        sbyte sweepDirection = 1;
        float orbitAngle;
        float orbitStartRadius;
        int dashTimer;
        // Which side of the player Chaos positions on during an ATTACK. 0 = not yet chosen; see ApproachSide.
        sbyte approachSide;
        // True once Wing Gale has closed inside GaleApproachRange and committed to the windup/flap timeline.
        bool galeInPosition;

        // Recovery rest spot. restOffset is rolled per attack cycle; restPoint is the world-space snapshot Chaos
        // actually drifts to, and driftReleased marks the point where it stops caring about that spot at all.
        Vector2 restOffset = new Vector2(300f, -60f);
        Vector2 restPoint;
        bool driftReleased;
        float driftAngle;
        // Deliberately NOT cleared by StartAttack: the recovery's tell locks the opening dash heading, and the
        // attack that follows has to read the same value the lane line was drawn from.
        Vector2 lockedAim;
        Vector2 teleportPosition;
        Vector2 gridAnchor;
        short gridXOffset;
        short gridYOffset;
        float lockedX;
        float floorY;
        float diveApexY;
        byte divePhase;
        byte diveSlamsDone;

        // The bag lives only where attacks are chosen (never on a remote client), which reads the synced
        // NextAttack instead. eligibleIndices is reused per draw to keep the draw allocation-free.
        readonly List<AttackState> attackBag = new List<AttackState>();
        readonly List<int> eligibleIndices = new List<int>();

        #endregion

        #region Tuning

        // Shared pacing
        const int TellTicks = 28;                  // tail of a recovery spent announcing the next attack
        const int OpeningRecoveryTicks = 90;
        const int PhaseTransitionTicks = 90;
        const int PhaseOpenerRecoveryTicks = 70;
        const int StaggerRecoveryTicks = 90;
        const float ApproachSpeed = 15f;           // unhurried drift in; attacks have their own speeds
        const float RecoveryEaseDistance = 220f;   // inside this, target speed ramps down linearly to 0
        const float RecoveryVelocitySmoothing = 0.06f; // per-tick lerp toward the target velocity
        const float ArrivalRadius = 70f;           // close enough — release the tether here
        // The rest spot is rolled inside this band each cycle, both sides, so Chaos doesn't favour one flank.
        const float RestDistanceMin = 230f;
        const float RestDistanceMax = 430f;
        const float RestHeightMin = -170f;
        const float RestHeightMax = 20f;
        // Free-hover drift once released: slow, gradually curving, going nowhere in particular.
        const float DriftSpeed = 1.6f;
        const float DriftTurnRate = 0.018f;        // radians/tick
        const float DriftSmoothing = 0.05f;
        const float ReleaseLeashDistance = 950f;   // only re-aims if the player has run right away

        // Fireball Fan
        const int FanFireTicks = 360;
        const int FanVolleyInterval = 45;          // 8 volleys, last one 45t before the fire window ends
        const int FanSettleTicks = 30;
        const int FanRecoveryTicks = 90;
        const float FanStandoffX = 360f;
        const float FanStandoffY = -220f;

        // Scythe Lunge (5 dashes instead of 4 once it carries over into phase 2+)
        const int LungeDashInterval = 75;
        const int LungeEvolvedDashInterval = 65;
        const int LungeTellTicks = 25;
        const int LungeAimLockTicks = 10;
        const float LungeSpeed = 35f;
        const int LungeRecoveryTicks = 80;
        // Below this, skip the dash and just throw the ring: a lurch of a few pixels reads as a glitch rather
        // than a charge. Fairness comes from the 25t tell, not from this distance, so it sits under the 260px
        // recovery anchor — at 300f the opening dash of every lunge was suppressed.
        const float LungeMinDashDistance = 150f;

        // Carpet Bomb
        const int CarpetFireTicks = 300;
        const int CarpetBombInterval = 12;         // 25 bombs (was 60 every 10t)
        const int CarpetLastBombTick = 288;
        const int CarpetFirstLegTicks = 40;        // first leg is half a leg, so the sweep centres on the player
        const int CarpetLegTicks = 80;
        const int CarpetSweepSpeed = 25;
        const float CarpetHeight = 500f;
        const int CarpetSettleTicks = 120;         // bombs launched upward need ~120t to fall back down
        const int CarpetRecoveryTicks = 100;

        // Flame Hover
        const int FlameFireTicks = 300;
        const int FlameInterval = 5;
        const int FlameRecoveryTicks = 75;
        const float FlameHoverBaseSpeed = 5f;
        const float FlameHoverSpeedRamp = 1f;      // gradually accelerates by this much over the attack's duration

        // Rocket Dash
        const int RocketFireTicks = 300;
        const int RocketDashInterval = 60;         // was 40, which left no room for a tell
        const int RocketTrailTicks = 30;
        const int RocketTrailInterval = 10;
        const float RocketDashSpeed = 35f;
        const int RocketSettleTicks = 120;
        const int RocketRecoveryTicks = 80;

        // Fireball Storm — the fight's one interruptible channel
        const int StormChannelTicks = 100;
        const int StormInterruptibleTicks = 66;    // poise can cancel the cast up to here; then it commits
        const int StormFireTicks = 240;
        const int StormBigRingInterval = 50;
        const int StormSmallRingInterval = 20;
        const int StormSettleTicks = 30;
        const int StormRecoveryTicks = 120;

        // Cataclysm Dive
        const int DiveClimbTicks = 50;
        const int DiveHoldTicks = 40;
        const int DiveEvolvedHoldTicks = 25;
        const int DiveLockLeadTicks = 20;          // X locks this long before the plunge, so a sidestep still works
        const float DiveApexHeight = 720f;
        const float DiveMinRunUp = 400f;           // less run-up than this reads badly — fizzle instead
        const float DiveCeilingPad = 80f;
        const float DiveClimbSpeed = 26f;
        const float DiveClimbInertia = 10f;
        const float DivePlungeSpeed = 38f;
        const int DivePlungeTimeoutTicks = 45;     // fell into a pit — detonate where we are
        const int DiveGroundedTicks = 100;
        const int DiveRiseTicks = 30;
        const float DiveRiseSpeed = 14f;
        const int DiveRecoveryTicks = 30;
        const int DiveFizzleRecoveryTicks = 40;
        const int DiveEvolvedSlams = 2;
        const float DiveImpactRadius = 160f;
        const int ShockwaveArmTicks = 8;
        const int BuriedSampleThreshold = 3;       // of 9 body samples, before a position counts as buried
        const float SlamCrippleRange = 500f;       // ~31 tiles: close enough that staying wide is a real choice
        const int SlamCrippleTicks = 60;           // 1 second fully pinned...
        const int SlamTornWingsTicks = 600;        // ...then 10 seconds grounded but otherwise mobile

        // Shadowflame Teleport
        const int TeleportFireTicks = 360;
        const int TeleportFlameInterval = 5;
        const float TeleportFlameSpread = 30f;
        const int TeleportCycleTicks = 120;
        const int TeleportTelegraphTicks = 80;
        const int TeleportLastArrival = 320;       // 3 arrivals: 80, 200, 320
        const float TeleportHoverDistance = 340f;  // never rides the player; stream still has travel time
        // The drawn frame, NOT the hitbox: PreDraw centres a 294x226 frame on NPC.Center while the hitbox is
        // only 130x160, so any dust meant to sit "on the body" has to use these.
        const int SpriteFrameWidth = 294;
        const int SpriteFrameHeight = 226;
        const float TeleportMinDistance = 300f;
        const float TeleportMaxDistance = 460f;

        const int TeleportRecoveryTicks = 75;

        // Laser Grid
        const int GridRoundTicks = 280;
        const int GridFireTicks = 520;             // 2 rounds; round 2's trailing vent pause becomes the recovery
        const int GridSecondVolleyTick = 50;
        const int GridTelegraphStartTick = 90;     // 40t vent pause sits between the flames and the telegraph
        const int GridBeamTick = 150;
        const int GridBeamsPerAxis = 20;
        const int GridBeamSpacing = 200;
        const float GridBeamStartY = -1000f;       // the vertical tell and bolt entry share one line
        const float GridStandoffX = 350f;          // was 600, which parked Chaos outside melee reach
        const int GridRecoveryTicks = 130;

        // Orbital Cosmos
        const int OrbitFireTicks = 270;            // ~2.25 laps at 3 degrees/tick (120t per lap)
        const int OrbitOrbInterval = 15;
        const int OrbitNovaInterval = 90;
        const int OrbitNovaOffset = 45;            // novas at t = 45, 135, 225
        // Dropped from 4: at 4 the orbit point swept ~69px/tick around the ~990px circle, well past Chaos own
        // top speed, so he permanently trailed it and cut chords across the middle instead of tracing the arc.
        const float OrbitDegreesPerTick = 3f;
        const float OrbitSpeed = 37f;              // 25% slower than the old 50
        const float OrbitInertia = 7f;             // was 5 — less snap, less jitter
        const float OrbitMinDistance = 300f;       // personal space; he circles, he does not sit on you
        const float OrbitRadius = 700f;            // the (1,1) basis makes the real radius ~990px
        const int OrbitEaseTicks = 30;
        const int OrbitSettleTicks = 70;           // breaks orbit and heads in before the window opens
        const int OrbitRecoveryTicks = 80;

        // Wing Buffet Gale — three flaps that shove the player outward into a ring boundary
        // Closed to first: the push cuts off past GalePushRange and the ring sits on the player, so an attack
        // that started with Chaos still across the arena would fade in a boundary and then never reach it.
        const float GaleApproachRange = 350f;      // inside the 300-400px band; close enough that the first
                                                    // flap's push can already reach across GalePushRange
        const float GaleApproachSpeed = 28f;       // was 12/20 — an "unhurried drift" pace, slower than even
        const float GaleApproachInertia = 8f;      // ApproachSpeed's own recovery drift; a kiting player could
                                                    // outrun it and stall the attack forever. Matched to
                                                    // DiveClimbSpeed/Inertia, a comparable "close in fast" move.
        const int GaleWindupTicks = 45;            // ring fades in during this, before any push
        const int GaleFlapInterval = 55;
        const int GaleFlaps = 3;
        const int GalePushTicks = 34;              // length of each gust; the whole window is eased, not flat
        const int GaleFireTicks = GaleWindupTicks + GaleFlaps * GaleFlapInterval;
        const int GaleRecoveryTicks = 85;
        const float GaleRingRadius = 810f;   // 30% wider; each player gets one centred on themselves
        const float GaleRingSpawnRange = 2600f;    // nobody outside the fight gets a ring
        const int GaleRingHold = 180;
        const float GaleGustAccel = 0.99f;         // peak per-tick acceleration, at the middle of a gust — was
                                                    // 1.45 uncapped (never actually reachable before the approach
                                                    // fix), cut to 0.75, nudged to 0.9 once the dodge roll could
                                                    // no longer amplify a push-spiked velocity, +10% again here
        // How much of full strength the gust still carries at its very start and end. A pure sine envelope
        // bottomed out at zero, so each flap did nothing for its first and last several ticks and all the work
        // landed in one spike. Flattening it keeps the same total shove but delivers it as steady wind.
        const float GaleGustFloor = 0.45f;
        // The gust gets STRONGER with distance, not weaker. It has to carry you the whole way out to an 810px
        // ring, and a push that tapered off meant the second flap barely moved anyone the first had already
        // blown clear.
        const float GaleNearMult = 0.9f;
        const float GaleFarMult = 1.45f;
        const float GalePushRange = 800f;

        // Void Singularity — a short cast; the tear itself outlives the attack (see ChaosSingularity)
        const int SingularityTelegraphTicks = 90;
        const int SingularityCastTicks = 120;       // 30t after the tear spawns, same gap the old 40/70 pair had
        const int SingularityRecoveryTicks = 70;
        const float SingularityPlacementRange = 450f;

        // Fiend's Brood — a short cast that leaves three destructible sigils behind
        const int BroodTelegraphTicks = 45;
        const int BroodCastTicks = 80;
        const int BroodRecoveryTicks = 75;
        const int BroodSigilCount = 3;
        const float BroodSigilRadius = 520f;       // how far out the ring of sigils is planted
        // Read by ChaosBroodSigil. Hostile projectiles deal double on hit, so 16 lands at ~32 — deliberately
        // level with Chaos's own shots (damage 100 / 6), not worse than the boss itself.
        public const int BroodBoltDamage = 16;

        #endregion

        #region Netcode

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write((byte)NextAttack);
            writer.Write((byte)LastAttack);
            writer.Write((byte)phase);
            writer.Write((short)AttackTimer);
            writer.Write((short)recoveryLength);
            writer.Write((short)flyPosition);
            writer.Write(sweepDirection);
            writer.Write(approachSide);
            writer.Write(restOffset.X);
            writer.Write(restOffset.Y);
            writer.Write(restPoint.X);
            writer.Write(restPoint.Y);
            writer.Write(driftReleased);
            writer.Write(driftAngle);
            writer.Write(orbitAngle);
            writer.Write(orbitStartRadius);
            writer.Write((short)dashTimer);
            writer.Write(lockedAim.X);
            writer.Write(lockedAim.Y);
            writer.Write(teleportPosition.X);
            writer.Write(teleportPosition.Y);
            writer.Write(gridAnchor.X);
            writer.Write(gridAnchor.Y);
            writer.Write(gridXOffset);
            writer.Write(gridYOffset);
            writer.Write(lockedX);
            writer.Write(floorY);
            writer.Write(diveApexY);
            writer.Write(divePhase);
            writer.Write(diveSlamsDone);
            writer.Write(galeInPosition);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            NextAttack = (AttackState)reader.ReadByte();
            LastAttack = (AttackState)reader.ReadByte();
            phase = reader.ReadByte();
            AttackTimer = reader.ReadInt16();
            recoveryLength = reader.ReadInt16();
            flyPosition = reader.ReadInt16();
            sweepDirection = reader.ReadSByte();
            approachSide = reader.ReadSByte();
            restOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            restPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            driftReleased = reader.ReadBoolean();
            driftAngle = reader.ReadSingle();
            orbitAngle = reader.ReadSingle();
            orbitStartRadius = reader.ReadSingle();
            dashTimer = reader.ReadInt16();
            lockedAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            teleportPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            gridAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            gridXOffset = reader.ReadInt16();
            gridYOffset = reader.ReadInt16();
            lockedX = reader.ReadSingle();
            floorY = reader.ReadSingle();
            diveApexY = reader.ReadSingle();
            divePhase = reader.ReadByte();
            diveSlamsDone = reader.ReadByte();
            galeInPosition = reader.ReadBoolean();
        }

        #endregion

        #region Drawing

        private Vector2[] oldPositions = new Vector2[5];

        public override void FindFrame(int frameHeight)
        {
            // The 8-frame wing loop is Chaos's only animation, so its SPEED is the body language: agitated
            // through a telegraph, labouring through recovery, near-still while grounded after a dive.
            int ticksPerFrame = 4;

            if (State == AttackState.Recovery)
            {
                ticksPerFrame = 8;
            }
            else if (State == AttackState.CataclysmDive && divePhase == DivePhaseGrounded)
            {
                ticksPerFrame = 10;
            }
            else if (IsTelegraphing())
            {
                ticksPerFrame = 2;
            }

            NPC.frameCounter += 1.0;

            // Reset before dividing, so the frame index can never run off the end of the sheet when
            // ticksPerFrame drops mid-loop.
            int loopTicks = ticksPerFrame * Main.npcFrameCount[NPC.type];
            if (NPC.frameCounter >= loopTicks)
            {
                NPC.frameCounter = 0;
            }

            NPC.frame.Y = (int)(NPC.frameCounter / ticksPerFrame) * frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Recovery dims the body and a telegraph brightens it — the cheapest possible "spent" vs
            // "winding up" read on a sprite that has no poses.
            float brightness = 1f;

            if (State == AttackState.Recovery)
            {
                brightness = 0.6f;
            }
            else if (IsTelegraphing())
            {
                brightness = 1.25f;
            }

            // Clamped by hand: Color * float casts straight to byte, so a brightness above 1 on an already-bright
            // channel (this base tops out at 227) wraps around and flickers the sprite dark.
            int red = Math.Min((int)((100 + drawColor.R / 2) * brightness), 255);
            int green = Math.Min((int)((100 + drawColor.G / 2) * brightness), 255);
            int blue = Math.Min((int)((100 + drawColor.B / 2) * brightness), 255);
            Color RealDrawColor = new Color(red, green, blue);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Rectangle sourceRect = NPC.frame;

            Vector2 origin2 = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);

            for (int i = 1; i < oldPositions.Length; i++)
            {
                int WhichOne = i * (-1) + oldPositions.Length;
                float alpha = 1f * (1f + i / (float)oldPositions.Length);
                Vector2 drawPos = oldPositions[WhichOne] - screenPos;
                spriteBatch.Draw(
                    texture,
                    drawPos,
                    sourceRect,
                    RealDrawColor * (alpha / 3),
                    NPC.rotation,
                    origin2,
                    NPC.scale,
                    SpriteEffects.None,
                    0f
                );
            }

            spriteBatch.Draw(
                texture,
                NPC.Center - screenPos,
                sourceRect,
                RealDrawColor,
                NPC.rotation,
                origin2,
                NPC.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        ///<summary>Whether Chaos is currently winding something up. Drives flap tempo, draw brightness and
        ///nothing else — the poise telegraph flag is set separately in AI().</summary>
        bool IsTelegraphing()
        {
            if (State == AttackState.FireballStorm && AttackTimer < StormChannelTicks)
            {
                return true;
            }

            if (State == AttackState.CataclysmDive && (divePhase == DivePhaseClimb || divePhase == DivePhaseHold))
            {
                return true;
            }

            if (State == AttackState.Recovery && AttackTimer >= recoveryLength - TellTicks)
            {
                return true;
            }

            return false;
        }

        #endregion

        public override void AI()
        {
            for (int i = oldPositions.Length - 1; i > 0; i--)
            {
                oldPositions[i] = oldPositions[i - 1];
            }
            oldPositions[1] = NPC.Center;

            // Targeting and despawning both live in the handler. The old code read Main.player[NPC.target]
            // BEFORE targeting and never refreshed it, so Chaos fled on a dead player while others lived.
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (despawnHandler.IsDespawning)
            {
                NPC.velocity.Y -= 0.35f;
                return;
            }

            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                NPC.velocity.Y -= 0.35f;
                return;
            }

            NPC.direction = 1;
            if (target.Center.X < NPC.Center.X)
            {
                NPC.direction = -1;
            }
            NPC.spriteDirection = NPC.direction;

            // Banking angle follows horizontal speed, but EASED into rather than snapped to: the raw value made
            // every small velocity change a visible twitch, and the afterimage trail multiplied it by five.
            float targetRotation = MathHelper.Clamp(NPC.velocity.X * 0.04f, -0.9f, 0.9f);
            NPC.rotation = MathHelper.Lerp(NPC.rotation, targetRotation, 0.12f);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            if (globalNPC.StaggerTimer > 0)
            {
                // OnStagger already dropped the attack; just sag and leak embers until it wears off.
                NPC.velocity *= 0.9f;
                NPC.rotation = NPC.direction * 0.35f;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(60f, 70f), DustID.Shadowflame, Vector2.UnitY * 1.5f, 120, default, 1.3f);
                }

                return;
            }

            // Phase thresholds interrupt whatever is running. `phase` is raised HERE, as the transition
            // starts, so a burst that crosses a threshold twice in one tick can't trigger it twice.
            if (Main.netMode != NetmodeID.MultiplayerClient && State != AttackState.PhaseTransition)
            {
                if (phase == 1 && NPC.life <= NPC.lifeMax * 0.66f)
                {
                    phase = 2;
                    StartAttack(AttackState.PhaseTransition);
                }
                else if (phase == 2 && NPC.life <= NPC.lifeMax * 0.33f)
                {
                    phase = 3;
                    StartAttack(AttackState.PhaseTransition);
                }
            }

            // Poise contract: an attack in progress is hyper-armored, Recovery is the deliberate punish
            // window, and the two interruptible spots are the Storm channel and the post-dive ground rest.
            if (State != AttackState.Recovery)
            {
                globalNPC.AttackCommitted = true;
            }

            if (State == AttackState.FireballStorm && AttackTimer < StormInterruptibleTicks)
            {
                globalNPC.AttackCommitted = false;
                globalNPC.AttackTelegraphing = true;
            }

            if (State == AttackState.CataclysmDive && divePhase == DivePhaseGrounded)
            {
                globalNPC.AttackCommitted = false;
            }

            stateJustChanged = false;

            switch (State)
            {
                case AttackState.Recovery:
                    RunRecovery(target);
                    break;
                case AttackState.PhaseTransition:
                    RunPhaseTransition();
                    break;
                case AttackState.FireballFan:
                    RunFireballFan(target);
                    break;
                case AttackState.ScytheLunge:
                    RunScytheLunge(target);
                    break;
                case AttackState.CarpetBomb:
                    RunCarpetBomb(target);
                    break;
                case AttackState.FlameHover:
                    RunFlameHover(target);
                    break;
                case AttackState.RocketDash:
                    RunRocketDash(target);
                    break;
                case AttackState.FireballStorm:
                    RunFireballStorm(target);
                    break;
                case AttackState.CataclysmDive:
                    RunCataclysmDive(target);
                    break;
                case AttackState.ShadowflameTeleport:
                    RunShadowflameTeleport(target);
                    break;
                case AttackState.LaserGrid:
                    RunLaserGrid(target);
                    break;
                case AttackState.OrbitalCosmos:
                    RunOrbitalCosmos(target);
                    break;
                case AttackState.WingGale:
                    RunWingGale(target);
                    break;
                case AttackState.VoidSingularity:
                    RunVoidSingularity(target);
                    break;
                case AttackState.FiendsBrood:
                    RunFiendsBrood(target);
                    break;
            }

            // Incremented last so an attack's first executed tick is t = 0, and so a state change made
            // during this tick doesn't immediately skip the new state's tick 0.
            if (!stateJustChanged)
            {
                AttackTimer++;
            }
        }

        #region State transitions and attack selection

        ///<summary>Enters a state and clears every per-attack field. Called for attacks, Recovery and the
        ///phase transition alike, so there is exactly one place that can leave stale state behind.</summary>
        void StartAttack(AttackState next)
        {
            State = next;
            AttackTimer = 0;
            stateJustChanged = true;

            flyPosition = 0;
            dashTimer = 0;
            orbitAngle = 0f;
            orbitStartRadius = 0f;
            teleportPosition = Vector2.Zero;
            gridAnchor = Vector2.Zero;
            gridXOffset = 0;
            gridYOffset = 0;
            lockedX = 0f;
            floorY = 0f;
            diveApexY = 0f;
            divePhase = DivePhaseClimb;
            diveSlamsDone = 0;
            galeInPosition = false;

            // Cleared per ATTACK, not per recovery: an attack's settle tail and the recovery that follows it are
            // one continuous glide, so they have to agree on where they are heading.
            if (next != AttackState.Recovery)
            {
                approachSide = 0;
                restPoint = Vector2.Zero;
                driftReleased = false;

                // Rolled fresh each cycle, both sides. Deriving the rest side from Chaos's current position was
                // self-reinforcing — park on the right, so the next attack starts on the right, so it parks right
                // again — which is why it looked biased toward the player's right flank.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float restSide = 1f;
                    if (Main.rand.NextBool())
                    {
                        restSide = -1f;
                    }

                    float restX = restSide * Main.rand.NextFloat(RestDistanceMin, RestDistanceMax);
                    restOffset = new Vector2(restX, Main.rand.NextFloat(RestHeightMin, RestHeightMax));
                }
            }

            // Rolled up front and synced, so the sweep never runs in opposite directions on two machines.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                sweepDirection = (sbyte)(Main.rand.NextBool() ? 1 : -1);
            }

            NPC.netUpdate = true;
        }

        ///<summary>Ends the current attack and opens the punish window. The next card is drawn HERE so the
        ///tail of the recovery can announce it — the bag took away the memorisable 1->2->3 order.</summary>
        void BeginRecovery(int ticks)
        {
            // Wipe the previous dash heading. It is deliberately NOT cleared by StartAttack (the attack has to
            // read what the tell locked), but leaving it set through the START of a recovery let PlayTell draw
            // a lane line pointing where the LAST dash went, until this tell overwrote it 18 ticks later.
            // That is a telegraph that lies, so the line stays hidden until RunRecovery locks a fresh heading.
            lockedAim = Vector2.Zero;

            // Set before the draw: DrawNextAttack reads LastAttack to avoid an immediate repeat.
            LastAttack = State;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NextAttack = DrawNextAttack();
            }

            StartAttack(AttackState.Recovery);
            recoveryLength = ticks;
        }

        ///<summary>Shuffled grab bag, following ChooseFromBag in RedKnightAttackController: draw without
        ///replacement, refill when empty, and never repeat the last card while another one is available.</summary>
        AttackState DrawNextAttack()
        {
            if (attackBag.Count == 0)
            {
                RefillAttackBag();
            }

            // Pass 1: cards that pass their gate and aren't an immediate repeat.
            eligibleIndices.Clear();
            for (int i = 0; i < attackBag.Count; i++)
            {
                if (attackBag[i] == LastAttack)
                {
                    continue;
                }

                if (IsAttackEligible(attackBag[i]))
                {
                    eligibleIndices.Add(i);
                }
            }

            // Pass 2: allow the repeat rather than stall (one card left, or everything else gated out).
            if (eligibleIndices.Count == 0)
            {
                CollectEligible();
            }

            // Pass 3: the bag had nothing usable in it at all — refill and look again.
            if (eligibleIndices.Count == 0)
            {
                RefillAttackBag();
                CollectEligible();
            }

            // Nothing's gate can be satisfied right now (no floor anywhere, say) — fall back to the opener.
            if (eligibleIndices.Count == 0)
            {
                return PhaseOpener();
            }

            int chosenIndex = eligibleIndices[Main.rand.Next(eligibleIndices.Count)];
            AttackState chosen = attackBag[chosenIndex];
            attackBag.RemoveAt(chosenIndex);
            return chosen;
        }

        ///<summary>Fills eligibleIndices with every gate-passing card, repeat included.</summary>
        void CollectEligible()
        {
            eligibleIndices.Clear();
            for (int i = 0; i < attackBag.Count; i++)
            {
                if (IsAttackEligible(attackBag[i]))
                {
                    eligibleIndices.Add(i);
                }
            }
        }

        void RefillAttackBag()
        {
            attackBag.Clear();

            if (phase == 1)
            {
                attackBag.Add(AttackState.FireballFan);
                attackBag.Add(AttackState.ScytheLunge);
                attackBag.Add(AttackState.CarpetBomb);
                attackBag.Add(AttackState.WingGale);
                attackBag.Add(AttackState.CataclysmDive);
                return;
            }

            if (phase == 2)
            {
                attackBag.Add(AttackState.FlameHover);
                attackBag.Add(AttackState.RocketDash);
                attackBag.Add(AttackState.FireballStorm);
                attackBag.Add(AttackState.CataclysmDive);
                // Carry-over card: a 3-card bag in an HP-gated phase can repeat only once or twice, so each
                // later phase keeps one evolved earlier attack (this one gains a 5th dash).
                attackBag.Add(AttackState.ScytheLunge);
                return;
            }

            attackBag.Add(AttackState.ShadowflameTeleport);
            attackBag.Add(AttackState.LaserGrid);
            attackBag.Add(AttackState.OrbitalCosmos);
            attackBag.Add(AttackState.CataclysmDive);
            attackBag.Add(AttackState.VoidSingularity);
            attackBag.Add(AttackState.FiendsBrood);
        }

        ///<summary>Gates narrow the bag; they never empty it. Only the dive has one: it needs a flat floor to
        ///slam into and to crawl its shockwaves along.</summary>
        bool IsAttackEligible(AttackState candidate)
        {
            if (candidate != AttackState.CataclysmDive)
            {
                return true;
            }

            Player target = Main.player[NPC.target];
            return FindFloorY((int)(target.Center.X / 16f), (int)(target.Bottom.Y / 16f)) > 0f;
        }

        ///<summary>The attack each phase deliberately opens on, so the player gets an early read of its pacing.</summary>
        AttackState PhaseOpener()
        {
            if (phase == 1)
            {
                return AttackState.FireballFan;
            }

            if (phase == 2)
            {
                return AttackState.FlameHover;
            }

            return AttackState.ShadowflameTeleport;
        }

        #endregion

        #region Recovery, tells and phase transition

        void RunRecovery(Player target)
        {
            DriftTowardRestPoint(target);

            // Venting embers = "I'm spent, hit me". There is deliberately no hyper-armor here.
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(55f, 65f), DustID.Torch, new Vector2(0f, 0.6f), 140, default, 1.15f);
            }

            int tellStart = recoveryLength - TellTicks;
            if (AttackTimer >= tellStart)
            {
                int tellTick = AttackTimer - tellStart;

                // The opening dash direction is locked HERE, not inside PlayTell: PlayTell is visuals-only and
                // returns early on a dedicated server, which would leave the server dashing on a different
                // heading than the clients that drew the lane for it.
                bool dashOpener = NextAttack == AttackState.ScytheLunge || NextAttack == AttackState.RocketDash;
                if (dashOpener && tellTick == TellTicks - LungeAimLockTicks)
                {
                    lockedAim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                }

                PlayTell(NextAttack, tellTick, target);
            }

            if (AttackTimer >= recoveryLength)
            {
                StartAttack(NextAttack);
            }
        }

        ///<summary>Recovery movement: pick a spot near the player ONCE, drift to it unhurriedly, then let go of it
        ///entirely and hover. Also used by the settle tails, so a long-range attack is already heading in when the
        ///punish window opens — Carpet Bomb, Laser Grid and Orbital all end 500-990px away.
        ///
        ///Deliberately NOT a tether. Re-deriving the destination from the player's live position every tick made
        ///Chaos shadow them in lockstep, which read as being rubber-banded to a fixed leash. restPoint is a
        ///world-space snapshot instead: once Chaos arrives it stops tracking the player at all and free-drifts.
        ///The only leash left is a far-distance one, so it can't strand itself across the arena.</summary>
        void DriftTowardRestPoint(Player target)
        {
            if (restPoint == Vector2.Zero)
            {
                restPoint = ClearOfTiles(target.Center + restOffset, target);
            }

            float distanceToPlayer = NPC.Distance(target.Center);

            if (driftReleased && distanceToPlayer < ReleaseLeashDistance)
            {
                // Released: a slowly curving hover that isn't aimed at anything. The flattened Y keeps it from
                // wandering into the floor or the ceiling.
                driftAngle += DriftTurnRate;
                Vector2 driftVelocity = new Vector2((float)Math.Cos(driftAngle), (float)Math.Sin(driftAngle) * 0.5f) * DriftSpeed;
                NPC.velocity = Vector2.Lerp(NPC.velocity, driftVelocity, DriftSmoothing);
                return;
            }

            // The player has run a long way off — re-aim once rather than hover on the far side of the arena.
            if (distanceToPlayer >= ReleaseLeashDistance)
            {
                restPoint = ClearOfTiles(target.Center + restOffset, target);
                driftReleased = false;
            }

            Vector2 toRestPoint = restPoint - NPC.Center;
            float distance = toRestPoint.Length();

            if (distance <= ArrivalRadius)
            {
                if (!driftReleased)
                {
                    driftReleased = true;
                    driftAngle = NPC.velocity.ToRotation();
                    NPC.netUpdate = true;
                }

                return;
            }

            // Ramp the target speed down over the last stretch so Chaos settles instead of overshooting and
            // being yanked back.
            float targetSpeed = ApproachSpeed;
            if (distance < RecoveryEaseDistance)
            {
                targetSpeed = ApproachSpeed * (distance / RecoveryEaseDistance);
            }

            Vector2 desiredVelocity = toRestPoint / distance * targetSpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, RecoveryVelocitySmoothing);
        }

        ///<summary>The last ~28 ticks of every recovery announce the next attack, using only what the sprite
        ///allows: colour-matched dust, a lane line, and the flap/tint changes driven by IsTelegraphing.</summary>
        void PlayTell(AttackState upcoming, int tellTick, Player target)
        {
            if (Main.dedServ)
            {
                return;
            }

            switch (upcoming)
            {
                case AttackState.FireballFan:
                    SpawnConvergingDust(DustID.Torch, 120f, 3, 1.4f);
                    SpawnConvergingDust(DustID.GoldFlame, 140f, 2, 1.1f);
                    break;

                case AttackState.ScytheLunge:
                case AttackState.RocketDash:
                    SpawnConvergingDust(DustID.Shadowflame, 110f, 3, 1.3f);

                    // The lane the opening dash will take. Drawn once, on the tick RunRecovery locks the
                    // heading — not every tell tick, which laid down a fresh line each frame as Chaos drifted.
                    if (tellTick == TellTicks - LungeAimLockTicks && lockedAim != Vector2.Zero)
                    {
                        DrawTelegraphLine(NPC.Center, NPC.Center + lockedAim * 500f, Color.MediumPurple);
                    }
                    break;

                case AttackState.CarpetBomb:
                    // Embers streaming UP off the wingspan: the hazard is about to come from above.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 wingSpot = NPC.Center + new Vector2(Main.rand.NextFloat(-90f, 90f), Main.rand.NextFloat(-40f, 20f));
                        Dust.NewDustPerfect(wingSpot, DustID.Shadowflame, new Vector2(0f, -2.5f), 110, default, 1.35f).noGravity = true;
                    }
                    break;

                case AttackState.FlameHover:
                    SpawnConvergingDust(DustID.Torch, 90f, 4, 1.5f);
                    break;

                case AttackState.FireballStorm:
                    // Half-density preview; the storm's own 100t channel is the real telegraph.
                    SpawnConvergingDust(DustID.GoldFlame, 130f, 2, 1.2f);
                    break;

                case AttackState.ShadowflameTeleport:
                    SpawnConvergingDust(DustID.Shadowflame, 130f, 3, 1.3f);
                    break;

                case AttackState.LaserGrid:
                    // Motes gathering into vertical streaks, matching the beams' purple.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 streak = NPC.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), Main.rand.NextFloat(-120f, 120f));
                        Dust.NewDustPerfect(streak, DustID.DemonTorch, new Vector2(0f, -3f), 100, default, 1.3f).noGravity = true;
                    }
                    break;

                case AttackState.WingGale:
                    // Dust thrown outward off the wingspan — the direction the shove will go.
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Dust.NewDustPerfect(NPC.Center + outward * 50f, DustID.Smoke, outward * 4f, 120, default, 1.6f).noGravity = true;
                    }
                    break;

                case AttackState.VoidSingularity:
                    // Motes falling INWARD, the inverse of every other tell Chaos has.
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(160f, 160f);
                        Dust.NewDustPerfect(NPC.Center + offset, DustID.Shadowflame, -offset / 10f, 80, default, 1.5f).noGravity = true;
                    }
                    break;

                case AttackState.FiendsBrood:
                    // Three sparks orbiting the chest: one per sigil about to be planted.
                    for (int i = 0; i < BroodSigilCount; i++)
                    {
                        Vector2 spark = new Vector2(85f, 0f).RotatedBy(tellTick * 0.12f + i * MathHelper.TwoPi / BroodSigilCount);
                        Dust.NewDustPerfect(NPC.Center + spark, DustID.DemonTorch, spark.RotatedBy(MathHelper.PiOver2) * 0.02f, 90, default, 1.6f).noGravity = true;
                    }
                    break;

                case AttackState.OrbitalCosmos:
                    // Spinning ring at the chest — the shape the orbit is about to trace.
                    float ringAngle = tellTick * 0.5f;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 ringOffset = new Vector2(70f, 0f).RotatedBy(ringAngle + i * MathHelper.TwoPi / 3f);
                        Dust.NewDustPerfect(NPC.Center + ringOffset, DustID.Shadowflame, ringOffset.RotatedBy(MathHelper.PiOver2) * 0.03f, 100, default, 1.3f).noGravity = true;
                    }
                    break;
            }
        }

        void RunPhaseTransition()
        {
            NPC.velocity *= 0.9f;

            if (AttackTimer == 0)
            {
                if (!Main.dedServ)
                {
                    // Chaos's own roar marks every phase break, not just the last one.
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/ChaosLaugh"), NPC.Center);

                    if (phase == 2)
                    {
                        UsefulFunctions.ScreenShake(NPC.Center, 6f, 30);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                        UsefulFunctions.ScreenShake(NPC.Center, 10f, 40);
                    }
                }

                // New phase, new deck.
                attackBag.Clear();
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 burst = Main.rand.NextVector2Circular(6f, 6f);
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(70f, 80f), DustID.Shadowflame, burst, 90, default, 1.6f).noGravity = true;
                }
            }

            // Nothing is spawned during the transition; hazards already in the air are left to resolve,
            // because they were telegraphed and killing a ChaosBlackFire spawns 3-7 firelets in PreKill.
            if (AttackTimer >= PhaseTransitionTicks)
            {
                LastAttack = AttackState.PhaseTransition;
                NextAttack = PhaseOpener();
                StartAttack(AttackState.Recovery);
                recoveryLength = PhaseOpenerRecoveryTicks;
            }
        }

        #endregion

        #region Phase 1 attacks

        void RunFireballFan(Player target)
        {
            // Stand off rather than homing onto the player: the old destination was target.Center, which put
            // the fan's spawn point inside them. 420px at 15px/tick is ~28 ticks of reaction time.
            Vector2 hover = target.Center + new Vector2(ApproachSide(target) * FanStandoffX, FanStandoffY);
            MoveToward(ClearOfTiles(hover, target), 30f, 100f);

            if (AttackTimer < FanFireTicks && AttackTimer % FanVolleyInterval == 0)
            {
                ShootProjectile(target, 15, ModContent.ProjectileType<ChaosFireball>(), 5, 45f, 22.5f, NPC.Center - new Vector2(0, 40), 0f);
            }

            if (AttackTimer >= FanFireTicks)
            {
                DriftTowardRestPoint(target);
            }

            if (AttackTimer >= FanFireTicks + FanSettleTicks)
            {
                BeginRecovery(FanRecoveryTicks);
            }
        }

        void RunScytheLunge(Player target)
        {
            // Phase 2 carries this card over with an extra, slightly faster dash.
            int dashCount = 4;
            int dashInterval = LungeDashInterval;

            if (phase >= 2)
            {
                dashCount = 5;
                dashInterval = LungeEvolvedDashInterval;
            }

            int fireTicks = dashCount * dashInterval;
            int tickInDash = AttackTimer % dashInterval;

            if (AttackTimer < fireTicks && tickInDash == 0)
            {
                Vector2 aim = lockedAim;
                if (aim == Vector2.Zero)
                {
                    aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                }

                // Too close to react to — hold position this beat instead of dashing point-blank.
                if (NPC.Distance(target.Center) >= LungeMinDashDistance)
                {
                    NPC.velocity = aim * LungeSpeed;

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/mantis_lord_horizontal_dash") with { Volume = 1.0f, PitchVariance = 0.1f }, NPC.Center);
                    }
                }

                // Half speed: at 15 the ring crossed the screen before it registered as anything.
                ShootProjectile(target, 7.5f, ModContent.ProjectileType<WeightedShadowBlast>(), 20, 0f, 18f, NPC.Center - new Vector2(0, 40), 0f);
            }
            else
            {
                NPC.velocity *= 0.97f;
            }

            // Tell for the NEXT dash: 25 ticks of converging dust, direction locked 10 ticks out so the drawn
            // lane is honest and a late sidestep still beats it.
            int nextDashIndex = (AttackTimer / dashInterval) + 1;
            bool anotherDashComing = nextDashIndex < dashCount;

            if (anotherDashComing && tickInDash >= dashInterval - LungeTellTicks)
            {
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.Shadowflame, 90f, 3, 1.3f);
                }

                if (tickInDash == dashInterval - LungeAimLockTicks)
                {
                    lockedAim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    NPC.netUpdate = true;

                    // Drawn once, on the tick the heading locks. Redrawing it every tick while Chaos drifted
                    // smeared one lane into a fan of them.
                    if (!Main.dedServ)
                    {
                        DrawTelegraphLine(NPC.Center, NPC.Center + lockedAim * 500f, Color.MediumPurple);
                    }
                }
            }

            if (AttackTimer >= fireTicks)
            {
                BeginRecovery(LungeRecoveryTicks);
            }
        }

        void RunCarpetBomb(Player target)
        {
            if (AttackTimer < CarpetFireTicks)
            {
                // Sweep above the player, reversing at the ends. flyPosition is zeroed by StartAttack; it used
                // to be reset in exactly one handoff, so under the bag a run could start 1000px off-centre.
                flyPosition += sweepDirection * CarpetSweepSpeed;

                bool atLegEnd = AttackTimer == CarpetFirstLegTicks;
                if (AttackTimer > CarpetFirstLegTicks && (AttackTimer - CarpetFirstLegTicks) % CarpetLegTicks == 0)
                {
                    atLegEnd = true;
                }

                if (atLegEnd)
                {
                    sweepDirection = (sbyte)(-sweepDirection);
                }

                Vector2 sweepTo = new Vector2(target.Center.X + flyPosition, target.Center.Y - CarpetHeight);
                MoveToward(ClearOfTiles(sweepTo, target), 30f, 10f);

                if (AttackTimer <= CarpetLastBombTick && AttackTimer % CarpetBombInterval == 0 && Main.netMode != NetmodeID.MultiplayerClient && !MuzzleBlocked(NPC.Center))
                {
                    // ai[0] is the player index ChaosBlackFire steers by. It was never set, so every bomb
                    // read Main.player[0] regardless of who Chaos was actually fighting.
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, -10), ModContent.ProjectileType<ChaosBlackFire>(), NPC.damage / 6, 1, ai0: NPC.target);
                }
            }
            else
            {
                // Settle: stop bombing and drop toward the punish anchor while the last bombs finish falling.
                DriftTowardRestPoint(target);
            }

            if (AttackTimer >= CarpetFireTicks + CarpetSettleTicks)
            {
                BeginRecovery(CarpetRecoveryTicks);
            }
        }

        #endregion

        #region Phase 2 attacks

        void RunFlameHover(Player target)
        {
            // Rides directly on the player, as it always did: the flamethrower is short-range and its density
            // at point-blank is the whole read. A stand-off was tried here and made the attack illegible.
            // Top speed creeps up over the attack's duration, so a player circle-strafing the stream feels it
            // getting harder to shake the longer they let it run.
            float hoverProgress = MathHelper.Clamp(AttackTimer / (float)FlameFireTicks, 0f, 1f);
            float hoverSpeed = FlameHoverBaseSpeed + FlameHoverSpeedRamp * hoverProgress;
            MoveToward(target.Center, hoverSpeed, 10f);

            // The flamethrower stream is the whole attack. It used to also throw a 5-fireball fan every 60
            // ticks, which just cluttered a move whose read is the wall of flame in front of you.
            if (AttackTimer < FlameFireTicks && AttackTimer % FlameInterval == 0)
            {
                ShootProjectile(target, 10, ProjectileID.Flames, 1, 0f, 0f, NPC.Center - new Vector2(0, 40), 0f, hostileVanillaFlame: true);
            }

            if (AttackTimer >= FlameFireTicks)
            {
                BeginRecovery(FlameRecoveryTicks);
            }
        }

        void RunRocketDash(Player target)
        {
            if (AttackTimer < RocketFireTicks && AttackTimer % RocketDashInterval == 0)
            {
                Vector2 aim = lockedAim;
                if (aim == Vector2.Zero)
                {
                    aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                }

                NPC.velocity = aim * RocketDashSpeed;
                dashTimer = RocketTrailTicks;

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/mantis_lord_horizontal_dash") with { Volume = 1.0f, PitchVariance = 0.1f }, NPC.Center);
                }
            }
            else if (AttackTimer < RocketFireTicks)
            {
                NPC.velocity *= 0.97f;
            }

            if (dashTimer > 0)
            {
                dashTimer--;

                if (AttackTimer % RocketTrailInterval == 0 && Main.netMode != NetmodeID.MultiplayerClient && !MuzzleBlocked(NPC.Center))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, -10), ModContent.ProjectileType<ChaosBlackFire>(), NPC.damage / 6, 1, ai0: NPC.target);
                }
            }

            int tickInDash = AttackTimer % RocketDashInterval;
            int nextDashIndex = (AttackTimer / RocketDashInterval) + 1;
            bool anotherDashComing = nextDashIndex < RocketFireTicks / RocketDashInterval;

            if (anotherDashComing && tickInDash >= RocketDashInterval - LungeTellTicks)
            {
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.DemonTorch, 100f, 3, 1.3f);
                }

                if (tickInDash == RocketDashInterval - LungeAimLockTicks)
                {
                    lockedAim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    NPC.netUpdate = true;

                    // Drawn once, on the tick the heading locks — see RunScytheLunge.
                    if (!Main.dedServ)
                    {
                        DrawTelegraphLine(NPC.Center, NPC.Center + lockedAim * 500f, Color.MediumPurple);
                    }
                }
            }

            if (AttackTimer >= RocketFireTicks)
            {
                DriftTowardRestPoint(target);
            }

            if (AttackTimer >= RocketFireTicks + RocketSettleTicks)
            {
                BeginRecovery(RocketRecoveryTicks);
            }
        }

        void RunFireballStorm(Player target)
        {
            NPC.velocity *= 0.97f;

            if (AttackTimer < StormChannelTicks)
            {
                // Fire and gold converging on the chest is the mod's shared "big cast incoming" read. The
                // first two thirds are poise-interruptible; a chime and a flash mark the commit.
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.Torch, 130f, 3, 1.5f);
                    SpawnConvergingDust(DustID.GoldFlame, 150f, 2, 1.2f);
                }

                if (AttackTimer == StormInterruptibleTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.6f }, NPC.Center);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 flash = Main.rand.NextVector2CircularEdge(7f, 7f);
                        Dust.NewDustPerfect(NPC.Center, DustID.GoldFlame, flash, 0, default, 2f).noGravity = true;
                    }
                }

                return;
            }

            int fireTick = AttackTimer - StormChannelTicks;

            if (fireTick < StormFireTicks)
            {
                // Ring angles are rolled here rather than at the call site so clients never roll their own.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (fireTick % StormBigRingInterval == 0)
                    {
                        ShootProjectile(target, 15, ModContent.ProjectileType<ChaosFireball>(), 20, Main.rand.Next(-360, 361), 18f, NPC.Center - new Vector2(0, 40), 0f);
                    }

                    if (fireTick % StormSmallRingInterval == 0)
                    {
                        ShootProjectile(target, 15, ModContent.ProjectileType<ChaosFireball>(), 10, Main.rand.Next(-360, 361), 36f, NPC.Center - new Vector2(0, 40), 0f);
                    }
                }
            }
            else
            {
                DriftTowardRestPoint(target);
            }

            if (fireTick >= StormFireTicks + StormSettleTicks)
            {
                BeginRecovery(StormRecoveryTicks);
            }
        }

        #endregion

        #region Cataclysm Dive

        const byte DivePhaseClimb = 0;
        const byte DivePhaseHold = 1;
        const byte DivePhasePlunge = 2;
        const byte DivePhaseGrounded = 3;
        const byte DivePhaseRise = 4;

        ///<summary>The one physical card: Chaos climbs, locks onto a spot on the floor, and body-slams it,
        ///throwing a ground wave each way. Built from motion + dust only, so it needs no new frames. Phase 3
        ///slams twice before resting. Every geometry failure fizzles rather than diving into nothing.</summary>
        void RunCataclysmDive(Player target)
        {
            if (divePhase == DivePhaseClimb)
            {
                if (AttackTimer == 0)
                {
                    floorY = FindFloorY((int)(target.Center.X / 16f), (int)(target.Bottom.Y / 16f));

                    if (floorY <= 0f)
                    {
                        FizzleDive();
                        return;
                    }

                    diveApexY = floorY - DiveApexHeight;

                    // Cap the apex under any ceiling above the player, so the climb can't clip the arena roof.
                    int ceilingTileY = -1;
                    int startTileY = (int)(target.Bottom.Y / 16f);
                    for (int offset = 1; offset <= 60; offset++)
                    {
                        int checkY = startTileY - offset;
                        if (checkY < 5)
                        {
                            break;
                        }

                        if (IsSolidTile((int)(target.Center.X / 16f), checkY))
                        {
                            ceilingTileY = checkY;
                            break;
                        }
                    }

                    if (ceilingTileY > 0 && diveApexY < ceilingTileY * 16f + DiveCeilingPad)
                    {
                        diveApexY = ceilingTileY * 16f + DiveCeilingPad;
                    }

                    // Not enough run-up left to read or to build speed.
                    if (floorY - diveApexY < DiveMinRunUp)
                    {
                        FizzleDive();
                        return;
                    }

                    NPC.netUpdate = true;
                }

                MoveToward(new Vector2(target.Center.X, diveApexY), DiveClimbSpeed, DiveClimbInertia);

                if (AttackTimer >= DiveClimbTicks)
                {
                    BeginDivePhase(DivePhaseHold);
                }

                return;
            }

            if (divePhase == DivePhaseHold)
            {
                NPC.velocity *= 0.85f;

                int holdTicks = DiveHoldTicks;
                if (diveSlamsDone > 0)
                {
                    holdTicks = DiveEvolvedHoldTicks;
                }

                int lockTick = holdTicks - DiveLockLeadTicks;

                if (AttackTimer == lockTick)
                {
                    lockedX = target.Center.X;
                    NPC.netUpdate = true;

                    // The committed column, drawn ONCE at the instant it commits. This used to redraw a full
                    // column every tick while the X still tracked the player, which left a row of vertical
                    // lines strung across the arena instead of one telegraph.
                    if (!Main.dedServ)
                    {
                        DrawTelegraphLine(new Vector2(lockedX, NPC.Center.Y), new Vector2(lockedX, floorY), Color.MediumPurple);
                    }
                }

                // Before the lock only the ground ring tracks the player — it is a patch of dust, so it can
                // follow every tick without smearing the way a line does.
                float ringX = lockedX;
                if (AttackTimer < lockTick)
                {
                    ringX = target.Center.X;
                }

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 ringSpot = new Vector2(ringX + Main.rand.NextFloat(-DiveImpactRadius, DiveImpactRadius), floorY - 8f);
                        Dust.NewDustPerfect(ringSpot, DustID.Shadowflame, new Vector2(0f, -1.5f), 100, default, 1.4f).noGravity = true;
                    }
                }

                if (AttackTimer >= holdTicks)
                {
                    BeginDivePhase(DivePhasePlunge);
                }

                return;
            }

            if (divePhase == DivePhasePlunge)
            {
                if (AttackTimer == 0)
                {
                    NPC.Center = new Vector2(lockedX, NPC.Center.Y);

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item72 with { Pitch = -0.7f }, NPC.Center);
                    }
                }

                // Re-set every tick so nothing else can slow the plunge.
                NPC.velocity = new Vector2(0f, DivePlungeSpeed);

                bool hitFloor = NPC.Bottom.Y >= floorY;
                bool plungeTimedOut = AttackTimer >= DivePlungeTimeoutTicks;

                if (hitFloor || plungeTimedOut)
                {
                    NPC.velocity = Vector2.Zero;

                    if (hitFloor)
                    {
                        NPC.position.Y = floorY - NPC.height;
                    }

                    if (!Main.dedServ)
                    {
                        UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_strike_ground") with { PitchVariance = 0.1f }, NPC.Center);

                        // Dirt kicked straight up off the floor — gravity on, so it arcs and falls back.
                        for (int i = 0; i < 90; i++)
                        {
                            Vector2 kickUp = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-13f, -4f));
                            Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-95f, 95f), 0f), DustID.Dirt, kickUp, 0, default, Main.rand.NextFloat(1.4f, 2.3f));
                        }

                        // Dark fire boiling up out of the impact itself.
                        for (int i = 0; i < 50; i++)
                        {
                            Vector2 burst = new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-7f, 1f));
                            Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-60f, 60f), 0f), DustID.Shadowflame, burst, 90, default, 1.8f).noGravity = true;
                        }

                        // Three clouds kicked off the impact point itself. The rest of the brood trails the
                        // shockwaves as they crawl (ChaosShockwave), so the dust follows the danger.
                        for (int i = 1; i <= 3; i++)
                        {
                            Vector2 cloudVelocity = new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-4.5f, -1.5f));
                            Gore.NewGore(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(Main.rand.NextFloat(-50f, 50f), -10f),
                                cloudVelocity, ModContent.Find<ModGore>($"tsorcRevamp/ChaosImpactCloud{i}").Type, Main.rand.NextFloat(0.9f, 1.3f));
                        }

                        // The damage wake: purple racing outward along the floor, drawn to the full reach the
                        // shockwaves cover so the dust never promises less than the hitbox delivers.
                        for (int i = 0; i < 70; i++)
                        {
                            float wakeDirection = 1f;
                            if (Main.rand.NextBool())
                            {
                                wakeDirection = -1f;
                            }

                            Vector2 wakeVelocity = new Vector2(wakeDirection * Main.rand.NextFloat(9f, 19f), Main.rand.NextFloat(-3.5f, 0.5f));
                            Dust wake = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-14f, 0f)), DustID.DemonTorch, wakeVelocity, 60, default, Main.rand.NextFloat(1.6f, 2.4f));
                            wake.noGravity = true;
                            wake.fadeIn = 1.3f;
                        }
                    }

                    // Concussion, in two stages: 3s of Crippled (everything locked) inside a 10s Torn Wings
                    // (flight only), so mobility comes back in steps instead of all at once. Applied by each
                    // machine to its OWN player, the same way a hostile projectile's
                    // OnHitPlayer does: Player.AddBuff only networks itself when called from a client, so a
                    // server-side loop over Main.player would land on nobody in multiplayer.
                    if (!Main.dedServ)
                    {
                        Player localPlayer = Main.LocalPlayer;

                        if (localPlayer.active && !localPlayer.dead && localPlayer.Distance(NPC.Bottom) <= SlamCrippleRange)
                        {
                            localPlayer.AddBuff(ModContent.BuffType<Buffs.Debuffs.Crippled>(), SlamCrippleTicks);
                            localPlayer.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), SlamTornWingsTicks);
                        }
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 wavePosition = new Vector2(NPC.Center.X, floorY - 20f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), wavePosition, Vector2.Zero, ModContent.ProjectileType<ChaosShockwave>(), NPC.damage / 6, 1, ai0: 1f, ai1: ShockwaveArmTicks);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), wavePosition, Vector2.Zero, ModContent.ProjectileType<ChaosShockwave>(), NPC.damage / 6, 1, ai0: -1f, ai1: ShockwaveArmTicks);
                    }

                    diveSlamsDone++;

                    if (phase >= 3 && diveSlamsDone < DiveEvolvedSlams)
                    {
                        BeginDivePhase(DivePhaseRise);
                    }
                    else
                    {
                        BeginDivePhase(DivePhaseGrounded);
                    }
                }

                return;
            }

            if (divePhase == DivePhaseGrounded)
            {
                // The best melee window in the fight: Chaos is sitting on the floor with no hyper-armor.
                NPC.velocity = Vector2.Zero;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(60f, 70f), DustID.Torch, new Vector2(0f, 0.8f), 140, default, 1.2f);
                }

                if (AttackTimer >= DiveGroundedTicks)
                {
                    BeginRecovery(DiveRecoveryTicks);
                }

                return;
            }

            // DivePhaseRise: shove off the floor and re-lock for the second slam.
            NPC.velocity = new Vector2(0f, -DiveRiseSpeed);

            if (AttackTimer >= DiveRiseTicks)
            {
                BeginDivePhase(DivePhaseHold);
            }
        }

        ///<summary>Moves to a dive sub-phase, restarting the tick count so each sub-phase's own tick 0 runs.</summary>
        void BeginDivePhase(byte next)
        {
            divePhase = next;
            AttackTimer = 0;
            stateJustChanged = true;
            NPC.netUpdate = true;
        }

        ///<summary>No usable floor, or no room to climb: puff out and take a short recovery rather than dive
        ///into geometry that can't carry the attack.</summary>
        void FizzleDive()
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(60f, 60f), DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f), 150, default, 1.5f).noGravity = true;
                }
            }

            BeginRecovery(DiveFizzleRecoveryTicks);
        }

        ///<summary>World (pixel) Y of the floor surface under a tile column, or -1 when there is no floor flat
        ///enough within 60 tiles. Flatness = solid ground within 2 tiles of that height across 6 columns each
        ///side, which is what ChaosShockwave needs to crawl. Callers: the dive's bag gate, the dive's own
        ///geometry resolve, and the teleport arrival clamp.</summary>
        float FindFloorY(int tileX, int startTileY)
        {
            if (tileX < 10 || tileX > Main.maxTilesX - 10)
            {
                return -1f;
            }

            int floorTileY = -1;
            for (int offset = 0; offset <= 60; offset++)
            {
                int checkY = startTileY + offset;
                if (checkY < 5 || checkY > Main.maxTilesY - 10)
                {
                    break;
                }

                if (IsSolidTile(tileX, checkY))
                {
                    floorTileY = checkY;
                    break;
                }
            }

            if (floorTileY < 0)
            {
                return -1f;
            }

            for (int column = -6; column <= 6; column++)
            {
                int neighbourX = tileX + column;
                if (neighbourX < 10 || neighbourX > Main.maxTilesX - 10)
                {
                    return -1f;
                }

                bool matched = false;
                for (int offset = -2; offset <= 2; offset++)
                {
                    if (IsSolidTile(neighbourX, floorTileY + offset))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return -1f;
                }
            }

            return floorTileY * 16f;
        }

        static bool IsSolidTile(int x, int y)
        {
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
            {
                return false;
            }

            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        #endregion

        #region Phase 3 attacks

        void RunShadowflameTeleport(Player target)
        {
            // Hover NEAR the player, never on top of them. Sitting inside the player sprite gave the stream no
            // travel time, buried the boss, and made the shots appear to radiate out of the player instead of
            // at them. This keeps whatever bearing Chaos already has and corrects only the DISTANCE, so it
            // trails loosely rather than being welded to the player position.
            Vector2 fromPlayer = NPC.Center - target.Center;
            Vector2 bearing = fromPlayer.SafeNormalize(-Vector2.UnitY);
            Vector2 hover = target.Center + bearing * TeleportHoverDistance;
            MoveToward(hover, 7f, 34f);

            if (AttackTimer < TeleportFireTicks && AttackTimer % TeleportFlameInterval == 0)
            {
                ShootProjectile(target, 10, ProjectileID.ShadowFlame, 1, 0f, 0f, NPC.Center - new Vector2(0, 40), TeleportFlameSpread, hostileVanillaFlame: true);
            }

            // Roll the destination, then show it for 80 ticks before actually moving.
            if (AttackTimer < TeleportFireTicks && AttackTimer % TeleportCycleTicks == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Rolled as a bearing plus a distance, NOT as independent X/Y offsets: those could each come up
                // near zero and drop Chaos straight onto the player. A 300px floor also gives the stream ~30
                // ticks of travel at speed 10, which is above the reaction floor.
                float arrivalBearing = Main.rand.NextFloat(MathHelper.TwoPi);
                float arrivalRange = Main.rand.NextFloat(TeleportMinDistance, TeleportMaxDistance);
                Vector2 destination = target.Center + arrivalBearing.ToRotationVector2() * arrivalRange;

                // Never blink into rock. ClearOfTiles walks the spot back toward the player until it is in air,
                // which can undercut the 300px floor — being visible beats being buried.
                teleportPosition = ClearOfTiles(destination, target);
                NPC.netUpdate = true;
            }

            int tickInCycle = AttackTimer % TeleportCycleTicks;

            if (!Main.dedServ && teleportPosition != Vector2.Zero && tickInCycle < TeleportTelegraphTicks)
            {
                // Both clouds are sized to the SPRITE frame, not the hitbox. The 130x160 hitbox is well under
                // half the 294x226 body, so a hitbox-sized cloud sat in the middle of Chaos reading as though
                // it belonged to something else entirely.
                Vector2 bodySize = new Vector2(SpriteFrameWidth, SpriteFrameHeight);

                // Arrival marker: where the body is going.
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(teleportPosition - bodySize / 2f, SpriteFrameWidth, SpriteFrameHeight, DustID.Shadowflame);
                }

                // Departure shimmer ON the body, so Chaos visibly comes apart where it stands instead of every
                // teleport dust being several hundred pixels away at the destination.
                for (int i = 0; i < 4; i++)
                {
                    Dust.NewDust(NPC.Center - bodySize / 2f, SpriteFrameWidth, SpriteFrameHeight, DustID.Shadowflame, 0f, 0f, 100, default, 1.2f);
                }
            }

            if (tickInCycle == TeleportTelegraphTicks && AttackTimer <= TeleportLastArrival && teleportPosition != Vector2.Zero)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                    for (int i = 0; i < 25; i++)
                    {
                        Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(SpriteFrameWidth / 2f, SpriteFrameHeight / 2f), DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 90, default, 1.5f).noGravity = true;
                    }
                }

                NPC.Center = teleportPosition;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(SpriteFrameWidth / 2f, SpriteFrameHeight / 2f), DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 90, default, 1.5f).noGravity = true;
                    }
                }
            }

            if (AttackTimer >= TeleportFireTicks)
            {
                BeginRecovery(TeleportRecoveryTicks);
            }
        }

        void RunLaserGrid(Player target)
        {
            MoveToward(ClearOfTiles(target.Center + new Vector2(ApproachSide(target) * GridStandoffX, 0f), target), 10f, 20f);

            int tickInRound = AttackTimer % GridRoundTicks;

            if (AttackTimer < GridFireTicks)
            {
                // The flame fan is kept, but it now lives in its own window: flames and beams are never in
                // flight together, and each round has two vent pauses the player can push damage into.
                // Vanilla Fireball is used here because this shot WANTS tile collision, which is its default.
                if (tickInRound == 0 || tickInRound == GridSecondVolleyTick)
                {
                    ShootProjectile(target, 7, ProjectileID.Fireball, 3, 22.5f, 22.5f, NPC.Center - new Vector2(0, 40), 0f);
                }

                // Capture the anchor ONCE and use it for both axes. The old code anchored the vertical beams
                // but re-read the live player position for the horizontal ones, so the horizontals missed
                // their own drawn telegraph lines whenever the player moved during it.
                if (tickInRound == GridTelegraphStartTick && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    gridAnchor = target.Center;
                    gridXOffset = (short)Main.rand.Next(-2100, -1400);
                    gridYOffset = (short)Main.rand.Next(700, 1400);
                    NPC.netUpdate = true;
                }

                // The original one-shot dust lines, unchanged: same colour, same -1000 start, same split count
                // (a world-coordinate length over 400, so ~100 dusts per line — that density IS the telegraph).
                // The only fix is the anchor: both axes now read gridAnchor, where the horizontals used to
                // re-read the live player position and so missed the lines that had been drawn for them.
                if (tickInRound == GridTelegraphStartTick && gridAnchor != Vector2.Zero && !Main.dedServ)
                {
                    for (int i = 0; i < GridBeamsPerAxis; i++)
                    {
                        Vector2 lineStart = gridAnchor + new Vector2(gridXOffset + i * GridBeamSpacing, GridBeamStartY);
                        Vector2 lineEnd = lineStart + new Vector2(0f, 3000f);
                        Dust.QuickDustLine(lineStart, lineEnd, lineEnd.Length() / 400f, Color.Purple);
                    }

                    for (int i = 0; i < GridBeamsPerAxis; i++)
                    {
                        Vector2 lineStart = gridAnchor + new Vector2(1400f, gridYOffset - i * GridBeamSpacing);
                        Vector2 lineEnd = lineStart + new Vector2(-3000f, 0f);
                        Dust.QuickDustLine(lineStart, lineEnd, (lineStart + new Vector2(0f, 3000f)).Length() / 400f, Color.Purple);
                    }
                }

                if (tickInRound == GridBeamTick && gridAnchor != Vector2.Zero && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int boltType = ModContent.ProjectileType<ChaosDemonBolt>();

                    for (int i = 0; i < GridBeamsPerAxis; i++)
                    {
                        Vector2 spawn = gridAnchor + new Vector2(gridXOffset + i * GridBeamSpacing, GridBeamStartY);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, new Vector2(0, 17), boltType, NPC.damage / 6, 1);
                    }

                    for (int i = 0; i < GridBeamsPerAxis; i++)
                    {
                        Vector2 spawn = gridAnchor + new Vector2(1400f, gridYOffset - i * GridBeamSpacing);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, new Vector2(-17, 0), boltType, NPC.damage / 6, 1);
                    }
                }
            }

            if (AttackTimer >= GridFireTicks)
            {
                BeginRecovery(GridRecoveryTicks);
            }
        }

        void RunOrbitalCosmos(Player target)
        {
            if (AttackTimer == 0)
            {
                // Start the orbit from wherever Chaos already is and ease the radius outward, so it doesn't
                // slide across the arena on the first tick. The -45 degrees compensates the (1,1) basis below.
                Vector2 fromPlayer = NPC.Center - target.Center;
                orbitAngle = MathHelper.ToDegrees(fromPlayer.ToRotation()) - 45f;
                orbitStartRadius = fromPlayer.Length() / 1.41421f;

                if (orbitStartRadius < 120f)
                {
                    orbitStartRadius = 120f;
                }

                NPC.netUpdate = true;
            }

            if (AttackTimer < OrbitFireTicks)
            {
                orbitAngle += OrbitDegreesPerTick;

                float easeProgress = MathHelper.Clamp(AttackTimer / (float)OrbitEaseTicks, 0f, 1f);
                float radius = MathHelper.Lerp(orbitStartRadius, OrbitRadius, easeProgress);

                Vector2 orbitTo = ClearOfTiles(target.Center + new Vector2(1f, 1f).RotatedBy(MathHelper.ToRadians(orbitAngle)) * radius, target);

                // ClearOfTiles walks a blocked destination back along the line TOWARD the player, and most of a
                // ~990px circle is inside rock in a cave — so the orbit point kept collapsing onto the player and
                // Chaos rushed in, then juddered as the destination flickered between open air and the collapse.
                // Never let the destination inside the personal-space ring.
                Vector2 playerToDestination = orbitTo - target.Center;
                float destinationDistance = playerToDestination.Length();

                if (destinationDistance < OrbitMinDistance)
                {
                    Vector2 destinationBearing = playerToDestination.SafeNormalize(-Vector2.UnitY);
                    orbitTo = target.Center + destinationBearing * OrbitMinDistance;
                }

                // Chaos also cannot match its own orbit point — the point sweeps faster than his top speed — so
                // he trails it and MoveToward cuts the CHORD rather than following the arc. A deep enough chord
                // passes straight through the player. If he is already inside the ring, climb out before
                // resuming the circle.
                if (NPC.Distance(target.Center) < OrbitMinDistance)
                {
                    Vector2 outward = (NPC.Center - target.Center).SafeNormalize(-Vector2.UnitY);
                    orbitTo = target.Center + outward * OrbitMinDistance;
                }

                MoveToward(orbitTo, OrbitSpeed, OrbitInertia);
            }
            else
            {
                // Break orbit and start heading in. This attack ends ~990px out, which at the recovery drift
                // speed is more than its whole punish window, so the travel is paid for here instead.
                DriftTowardRestPoint(target);
            }

            if (AttackTimer < OrbitFireTicks)
            {
                if (AttackTimer % OrbitOrbInterval == 0)
                {
                    // 60% slower: these are the slow drifting hazard the orbit leaves behind, not a snap shot.
                    ShootProjectile(target, 1.6f, ModContent.ProjectileType<ChaosCosmicOrb>(), 1, 0f, 0f, NPC.Center - new Vector2(0, 40), 0f);
                }

                if (AttackTimer % OrbitNovaInterval == OrbitNovaOffset && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ShootProjectile(target, 7, ModContent.ProjectileType<ChaosFireball>(), 20, Main.rand.Next(-360, 361), 20f, NPC.Center - new Vector2(0, 40), 0f);
                }
            }

            if (AttackTimer >= OrbitFireTicks + OrbitSettleTicks)
            {
                BeginRecovery(OrbitRecoveryTicks);
            }
        }

        #endregion

        #region Gale, Singularity and Brood

        ///<summary>Wing Buffet Gale: closes to within GaleApproachRange first, then a ring boundary fades in
        ///and three heavy flaps shove the player outward toward it. The push is capped and beatable by moving
        ///inward, so the ring only catches someone who stopped resisting — that is the whole test.</summary>
        void RunWingGale(Player target)
        {
            NPC.velocity *= 0.93f;

            if (!galeInPosition)
            {
                // The push cuts off past GalePushRange and the ring sits on the player, so an attack that
                // started with Chaos still across the arena would fade in a boundary and then never reach it.
                if (NPC.Distance(target.Center) > GaleApproachRange)
                {
                    MoveToward(ClearOfTiles(target.Center, target), GaleApproachSpeed, GaleApproachInertia);
                    return;
                }

                // Close enough — commit. Restart the tick count so the windup's own tick 0 runs this frame,
                // same trick BeginDivePhase uses to hand off between sub-phases.
                galeInPosition = true;
                AttackTimer = 0;
                stateJustChanged = true;
                NPC.netUpdate = true;
            }

            if (AttackTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // One ring per player, each centred on that player and visible only to them (see ChaosGaleRing).
                // A single shared ring could not work: centred on Chaos your position inside it was luck, and
                // centred on NPC.target it was meaningless to everyone else in the fight.
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player ringOwner = Main.player[i];

                    if (!ringOwner.active || ringOwner.dead)
                    {
                        continue;
                    }

                    // Skip anyone not actually in the fight, rather than littering the world with rings.
                    if (ringOwner.Distance(NPC.Center) > GaleRingSpawnRange)
                    {
                        continue;
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), ringOwner.Center, Vector2.Zero,
                        ModContent.ProjectileType<ChaosGaleRing>(), NPC.damage / 6, 1f,
                        ai0: GaleRingRadius, ai1: GaleRingHold, ai2: i);
                }
            }

            // Wind-up runs as long as the ring's fade-in, so nothing pushes before the boundary is readable.
            if (AttackTimer < GaleWindupTicks)
            {
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.DemonTorch, 150f, 3, 1.4f);
                }

                return;
            }

            int tickInFlap = (AttackTimer - GaleWindupTicks) % GaleFlapInterval;
            int flapIndex = (AttackTimer - GaleWindupTicks) / GaleFlapInterval;

            if (tickInFlap == 0 && flapIndex < GaleFlaps && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item32 with { Pitch = -0.4f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 4f, 12);

                // A burst of dust thrown outward along the gust, so the shove is visible before it lands.
                for (int i = 0; i < 60; i++)
                {
                    Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Dust gust = Dust.NewDustPerfect(NPC.Center + outward * 60f, DustID.Smoke, outward * Main.rand.NextFloat(9f, 17f), 130, default, 2f);
                    gust.noGravity = true;
                }
            }

            // The push itself. Applied per machine to its own player, matching the projectile force-zone rule:
            // each client is authoritative for its own player, so this must not be server-gated.
            if (tickInFlap < GalePushTicks && flapIndex < GaleFlaps && !Main.dedServ)
            {
                Player localPlayer = Main.LocalPlayer;

                if (localPlayer.active && !localPlayer.dead)
                {
                    Vector2 outward = localPlayer.Center - NPC.Center;
                    float distance = outward.Length();

                    if (distance > 1f && distance < GalePushRange)
                    {
                        // Eased in and out across the flap rather than one hard shove followed by a dead stop,
                        // but riding on a floor rather than bottoming out: the wind ramps up and releases
                        // without the gust being dead for the ticks at either end.
                        float shape = (float)Math.Sin(MathHelper.Pi * tickInFlap / GalePushTicks);
                        float envelope = MathHelper.Lerp(GaleGustFloor, 1f, shape);
                        float distanceMult = MathHelper.Lerp(GaleNearMult, GaleFarMult, distance / GalePushRange);

                        localPlayer.velocity += outward / distance * GaleGustAccel * envelope * distanceMult;
                    }
                }
            }

            if (AttackTimer >= GaleFireTicks)
            {
                BeginRecovery(GaleRecoveryTicks);
            }
        }

        ///<summary>Void Singularity: a short cast that tears open a hole in the arena, then Chaos goes back to
        ///fighting. The tear lives six seconds on its own (ChaosSingularity), which is the point — it overlaps
        ///whatever comes next instead of being a thing you wait out.</summary>
        void RunVoidSingularity(Player target)
        {
            NPC.velocity *= 0.95f;

            // Telegraph at the spot it will open, not on Chaos: the player needs to know where to avoid.
            Vector2 tearPoint = target.Center + (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * SingularityPlacementRange;
            tearPoint = ClearOfTiles(tearPoint, target);

            if (AttackTimer < SingularityTelegraphTicks)
            {
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.Shadowflame, 120f, 3, 1.5f);

                    // Motes falling inward at the destination, pre-selling both the location and the pull.
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(190f, 190f);
                        Dust mote = Dust.NewDustPerfect(tearPoint + offset, DustID.DemonTorch, -offset / 15f, 80, default, 1.5f);
                        mote.noGravity = true;
                    }
                }

                return;
            }

            if (AttackTimer == SingularityTelegraphTicks)
            {
                // No sound here: the tear plays its own as it phases in on its first tick.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), tearPoint, Vector2.Zero,
                        ModContent.ProjectileType<ChaosSingularity>(), NPC.damage / 6, 1f);
                }
            }

            if (AttackTimer >= SingularityCastTicks)
            {
                BeginRecovery(SingularityRecoveryTicks);
            }
        }

        ///<summary>Fiend's Brood: plants three destructible sigils in a ring around the player, then Chaos
        ///resumes. They snipe until killed, so for once the player is choosing between two targets rather than
        ///just dodging.</summary>
        void RunFiendsBrood(Player target)
        {
            NPC.velocity *= 0.95f;

            if (AttackTimer < BroodTelegraphTicks)
            {
                if (!Main.dedServ)
                {
                    SpawnConvergingDust(DustID.DemonTorch, 140f, 4, 1.5f);
                }

                return;
            }

            if (AttackTimer == BroodTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Evenly spaced around the player and rolled off a random bearing, so the brood never lands in
                // the same arrangement twice.
                float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int i = 0; i < BroodSigilCount; i++)
                {
                    float angle = baseAngle + MathHelper.TwoPi * i / BroodSigilCount;
                    Vector2 spot = target.Center + angle.ToRotationVector2() * BroodSigilRadius;
                    spot = ClearOfTiles(spot, target);

                    int sigil = NPC.NewNPC(NPC.GetSource_FromThis(), (int)spot.X, (int)spot.Y,
                        ModContent.NPCType<ChaosBroodSigil>(), 0, NPC.whoAmI, 0f, i * (ChaosBroodSigil.BoltInterval / BroodSigilCount));

                    if (sigil < Main.maxNPCs && Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: sigil);
                    }
                }
            }

            if (AttackTimer >= BroodCastTicks)
            {
                BeginRecovery(BroodRecoveryTicks);
            }
        }

        #endregion

        #region Shared primitives

        ///<summary>Which side of the player Chaos positions on for this attack: +1 right, -1 left. Latched on
        ///first use and held until the next attack starts (see StartAttack).
        ///
        ///Every caller used to recompute this from Chaos's current position each tick, which meant that any
        ///drift across the player's X flipped it — jumping the destination by twice the stand-off distance and
        ///reversing Chaos mid-flight. That oscillation is what read as a jittery wobble.</summary>
        int ApproachSide(Player target)
        {
            if (approachSide == 0)
            {
                approachSide = 1;
                if (NPC.Center.X < target.Center.X)
                {
                    approachSide = -1;
                }

                NPC.netUpdate = true;
            }

            return approachSide;
        }

        ///<summary>Whether Chaos's body would be BURIED at this centre — inside terrain, as opposed to merely
        ///clipping something. Samples a 3x3 grid across the sprite box rather than a single point, because at
        ///130x160 a centre-only test happily parks him with most of his body in a wall.
        ///
        ///Needs SEVERAL solid samples, not one. Chaos has noTileCollide and is bigger than three tiles, so
        ///overlapping a single block is not being stuck — and treating it as stuck made ClearOfTiles haul his
        ///destination toward the player over any stray block, which is exactly what made the orbit rush in.</summary>
        bool IsBlockedAt(Vector2 center)
        {
            int solidSamples = 0;

            for (int sampleX = -1; sampleX <= 1; sampleX++)
            {
                for (int sampleY = -1; sampleY <= 1; sampleY++)
                {
                    Vector2 sample = center + new Vector2(sampleX * NPC.width * 0.4f, sampleY * NPC.height * 0.4f);

                    if (IsTerrainWall((int)(sample.X / 16f), (int)(sample.Y / 16f)))
                    {
                        solidSamples++;
                    }
                }
            }

            return solidSamples >= BuriedSampleThreshold;
        }

        ///<summary>A solid tile that counts as WALL for a flying boss: terrain, not scenery.
        ///
        ///Excludes tileFrameImportant, which is how Terraria marks furniture and decoration. The arena is
        ///dotted with FlameJet tiles and those set Main.tileSolid true, so without this every jet read as a
        ///wall — dragging orbit and rest destinations around, and silently cancelling volleys through
        ///MuzzleBlocked — even though Chaos passes through them freely.</summary>
        static bool IsTerrainWall(int x, int y)
        {
            if (!IsSolidTile(x, y))
            {
                return false;
            }

            return !Main.tileFrameImportant[Main.tile[x, y].TileType];
        }

        ///<summary>The nearest open spot to `destination`, searched back along the line toward the player.
        ///
        ///Chaos has noTileCollide, so left alone its orbit, sweep and teleport destinations end up inside rock,
        ///where it is invisible and firing out of solid ground. The player always stands in open air, so walking
        ///the line toward them is guaranteed to escape; the player's own position is the worst case.</summary>
        Vector2 ClearOfTiles(Vector2 destination, Player target)
        {
            if (!IsBlockedAt(destination))
            {
                return destination;
            }

            Vector2 toPlayer = target.Center - destination;
            float distance = toPlayer.Length();

            if (distance < 1f)
            {
                return target.Center;
            }

            Vector2 step = toPlayer / distance * 48f;
            Vector2 probe = destination;

            for (int i = 0; i < 24; i++)
            {
                probe += step;

                if (!IsBlockedAt(probe))
                {
                    return probe;
                }
            }

            return target.Center;
        }

        ///<summary>Whether a projectile spawn point sits inside solid rock. Scenery does not count — a volley
        ///silently cancelled because the muzzle clipped a decorative block is worse than one fired through it.</summary>
        bool MuzzleBlocked(Vector2 muzzle)
        {
            return IsTerrainWall((int)(muzzle.X / 16f), (int)(muzzle.Y / 16f));
        }

        ///<summary>Eases Chaos toward a destination. Higher inertia = lazier turn. This is the only movement
        ///primitive the attacks use, so a hover, a sweep and an orbit differ only in destination and speed.</summary>
        void MoveToward(Vector2 destination, float speed, float inertia)
        {
            Vector2 toDestination = destination - NPC.Center;
            Vector2 direction = toDestination.SafeNormalize(Vector2.UnitY);
            Vector2 moveTo = direction * speed;
            NPC.velocity = (NPC.velocity * (inertia - 1f) + moveTo) / inertia;
        }

        ///<summary>Fires `count` projectiles fanned around the aim from startPosition to the target. Angles are
        ///degrees; spreadDegrees jitters each shot. This is the single choke point for every fan, ring and
        ///stream Chaos fires, hence the one server guard.
        ///
        ///`hostileVanillaFlame` is for Flames and ShadowFlame only. Both are alpha-255 sprites drawn by bespoke
        ///vanilla routines keyed on their literal type, so they cannot be wrapped in a ModProjectile (CloneDefaults
        ///+ AIType aliases the AI but never the draw, which renders them invisible). Instead they are spawned as
        ///the real vanilla type carrying tsorcGlobalProjectile.HostileVanillaMarker in ai[2] — a synced slot
        ///neither aiStyle uses — and every peer re-derives the hostile flags from it. Every other type Chaos
        ///fires is already hostile from its own SetDefaults.</summary>
        void ShootProjectile(Player target, float speed, int type, int count, float startAngle, float angleDecrement, Vector2 startPosition, float spreadDegrees, bool hostileVanillaFlame = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Never fire from inside rock. The destination clamps keep Chaos in open air, but it has
            // noTileCollide and can still be carried into a wall mid-attack.
            if (MuzzleBlocked(startPosition))
            {
                return;
            }

            float hostileMarker = 0f;
            if (hostileVanillaFlame)
            {
                hostileMarker = Content.Projectiles.tsorcGlobalProjectile.HostileVanillaMarker;
            }

            Vector2 toTarget = target.Center - startPosition;
            Vector2 aim = toTarget.SafeNormalize(Vector2.UnitX);
            float angle = startAngle;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (aim.RotatedBy(MathHelper.ToRadians(angle)) * speed).RotatedByRandom(MathHelper.ToRadians(spreadDegrees));
                int spawned = Projectile.NewProjectile(NPC.GetSource_FromThis(), startPosition, velocity, type, NPC.damage / 6, 1, ai2: hostileMarker);

                if (hostileVanillaFlame)
                {
                    // Flip locally too, so the spawn tick is already correct here; the marker is what makes
                    // every other machine agree from its own PreAI.
                    Main.projectile[spawned].hostile = true;
                    Main.projectile[spawned].friendly = false;
                    Main.projectile[spawned].tileCollide = false;
                }

                angle -= angleDecrement;
            }
        }

        ///<summary>ONE telegraph line, `strands` dust threads 5px apart — 3 reads as a thick line, 1 as a thin one.
        ///
        ///Call this on the single tick the direction commits — never every tick. QuickDustLine lays its dust
        ///wherever the endpoints are at that instant, so repeating it while either end is still moving paints a
        ///fresh line every frame: twenty ticks of a column that tracks the player becomes a picket fence across
        ///the arena rather than a telegraph. Spacing is per-pixel so long and short lines read equally solid.</summary>
        public static void DrawTelegraphLine(Vector2 start, Vector2 end, Color color, int strands = 3)
        {
            Vector2 along = end - start;
            float length = along.Length();

            if (length < 1f)
            {
                return;
            }

            Vector2 perpendicular = (along / length).RotatedBy(MathHelper.PiOver2);
            float splits = length / 14f;

            int reach = strands / 2;

            for (int strand = -reach; strand <= reach; strand++)
            {
                Vector2 shift = perpendicular * strand * 5f;
                Dust.QuickDustLine(start + shift, end + shift, splits, color);
            }
        }

        ///<summary>Dust spawned on a ring and given velocity toward Chaos's chest — the mod's shared "big cast
        ///incoming" read. Radius is where the motes appear; they reach the body in roughly 12 ticks.</summary>
        void SpawnConvergingDust(int dustType, float radius, int count, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 inward = -offset / 12f;
                Dust.NewDustPerfect(NPC.Center + offset, dustType, inward, 100, default, scale).noGravity = true;
            }
        }

        #endregion

        public void OnStagger(NPC npc)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Pitch = -0.4f }, NPC.Center);

                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(70f, 90f), DustID.Shadowflame, Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.4f).noGravity = true;
                }
            }

            // A phase transition isn't an attack and the new phase's deck depends on it finishing, so don't
            // let a stagger cut it short.
            if (State == AttackState.PhaseTransition)
            {
                return;
            }

            BeginRecovery(StaggerRecoveryTicks);
        }

        public override void OnKill() //special death animation
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)NPC.position.X, (int)NPC.position.Y, 0, 0, ModContent.ProjectileType<ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
            }

            if (tsorcRevampWorld.RemixMap && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Chaos.DarknessLifted"), new Color(255, 225, 20));
            }
        }
    }
}
