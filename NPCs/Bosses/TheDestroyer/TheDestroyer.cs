using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.TheDestroyer
{
    internal sealed class TheDestroyer : GlobalNPC, IDebugAttackLabel
    {
        internal const float HullPortOffset = 18f;
        private const float HullSegmentSpacing = 44f;
        private const float EnclosingOrbitSpeed = 0.025f;
        private const float EnclosingSecondOrbitSpeed = 0.0235f;
        private const float OverheadOrbitSpeed = 0.019f;
        private const float HourglassOrbitSpeed = 0.0275f;
        private const float EnclosingRadius = 820f;
        private const float OverheadRadius = 440f;
        private const float OverheadHeight = 680f;
        private const float HourglassHeight = 560f;
        private const int OverheadShapeTicks = 220;
        private const int HourglassShapeTicks = 264;
        private const int EnclosingShapeTicks = 260;
        private const int OverheadActiveTicks = 360;
        private const int HourglassActiveTicks = 630;
        private const int HourglassWaves = 4;
        private const int HourglassPortsPerWave = 5;
        private const int HourglassTellTicks = 50;
        internal const int HourglassFireTicks = 24;
        private const int HourglassBeamLifetime = HourglassTellTicks + HourglassFireTicks + DestroyerHullLaser.FadeTicks + DestroyerHullLaser.PoseWaitTicks;
        private const int HourglassWaveSpacing = 94;
        private const float ReactorOrbitSpeed = 0.0205f;
        private const int ReactorShapeTicks = 245;
        private const int ReactorReversalTicks = 112;
        private const int ReactorActiveTicks = 430;
        private const int ReactorWaves = 3;
        private const int ReactorTellTicks = 43;
        internal const int ReactorFireTicks = 154;
        private const int ThrashTellTicks = 56;
        private const int ThrashSettleTicks = 13;
        private const int ThrashDiveTicks = 34;
        private const int ThrashImpactTicks = 24;
        private const int ThrashFirstSweep = 92;
        private const int ThrashSecondSweep = 112;
        private const int ThrashFinalSweep = 106;
        internal const int ThrashFireTicks = ThrashFirstSweep + ThrashSecondSweep + ThrashFinalSweep;
        private const int EnclosingFirstLapTicks = 251;
        private const int EnclosingSecondLapTicks = 267;
        private const int EnclosingActiveTicks = EnclosingFirstLapTicks + EnclosingSecondLapTicks;
        private const int EnclosingFirstVolleyDelay = 16;
        internal const int OrbitTellTicks = 44;
        internal const int OrbitFireTicks = 20;
        private const int EnclosingFirstTellTicks = OrbitTellTicks + 15;
        internal const int EnclosingFirstFireTicks = 60;
        internal const int EnclosingSecondFireTicks = EnclosingFirstFireTicks + 60;
        private const int OrbitBeamLifetime = OrbitTellTicks + OrbitFireTicks + DestroyerHullLaser.FadeTicks + DestroyerHullLaser.PoseWaitTicks;
        private const int OrbitBeamSpacing = OrbitBeamLifetime + 4;
        private const float MaxCoilSpeed = 24f;
        private const float EruptionReentryRange = 640f;
        // The old projectile's 40 spawn damage was multiplied by four on hit. Declare the Expert hit here.
        internal const int LaserDamage = 160;
        private const int MawDamage = 200;
        private const int BombDamage = 170;
        private const int MissileDamage = 155;
        private const int MissileTelegraphTicks = 90;
        private const int MissileSegmentInterval = 60;
        private const int MissileSegmentStride = 3;
        private const int MissileSegmentCount = 8;
        private const int MissileFirstOrdinal = 9;
        private const int MissileHotTicks = 12;
        private const int ProbeBreathDamage = 120;
        private const int ProbeShotDamage = 135;
        private const int ProbeExplosionDamage = 150;
        private const int ProbeBreathTelegraphTicks = 90;
        private const int ProbeBreathFireTicks = 60;
        private const int ProbeLaserTellTicks = 60;
        private const int ProbeLaserCooldown = 4 * 60;
        private const int ProbeLaserBusyTicks = ProbeLaserTellTicks + DestroyerHullLaser.ProbeLiveTicks + DestroyerHullLaser.FadeTicks;
        private const int ProbeKamikazeLockTiles = 6;
        private const int ProbeKamikazeFlightTicks = 60;
        private const float ProbeBarrageMinDistance = 320f;
        private const int RepairRange = 480;
        private const int RepairWindup = 32;
        private const int RepairDuration = 240;
        private const int RepairCooldown = 420;
        private const int SpecialistRepairInterval = 8 * 60;
        private const int ProbeWaveAttackers = 4;
        private const int ProbeWaveCount = 3;
        private const int ProbeWaveOpeningTicks = 165;
        private const int BeamCap = 3;
        internal const int PortTell = 20;
        internal const int HullArmingTicks = 32;
        private const float WarmContactMultiplier = 0.5f;
        private const int PayloadHotTicks = 12;
        private const int BombBayTellTicks = 24;
        private const int BombBayFlashFadeTicks = 10;
        internal const int RippleDelay = 8;
        private const int RippleSegmentCount = 30;
        private const int RippleSegmentDelay = 2;
        private const int RippleTellTicks = 42;
        private const int RippleWaveGap = 120;
        private const int RippleWaveSpacing = (RippleSegmentCount - 1) * RippleSegmentDelay + DestroyerHullLaser.RippleFireTicks + RippleWaveGap;
        private const float RippleEntryDistance = 1690f;
        private const float RippleEntryHeight = 430f;
        private const int RippleSetupTicks = 230;
        private const int RippleShapeTicks = 310;
        private const float RippleShapeSpeed = 16f;
        private const float RippleSteering = 0.17f;
        private const float PassLaneHeight = 300f;
        private const int DriveThroughPortCount = 6;
        private const int DriveThroughFirstPort = 4;
        private const int DriveThroughBaselineStride = 4;
        private const int BroadsidePortStride = 6;
        private const int CurtainPortStride = 6;
        private const float CurtainPassSpeed = 10.5f;
        private const float CurtainRunoutDistance = CurtainPassSpeed * (DestroyerHullLaser.CurtainRunFireTicks - DestroyerHullLaser.FireTicks);
        private const float CurtainBoundaryProbe = 6f * 16f;
        private const int DriveThroughTicks = 390;
        private const int DriveThroughSetupTicks = 170;
        private const int DriveThroughMinTell = 32;
        private const float DriveThroughEntry = 1480f;
        private const float DriveThroughExit = 1610f;
        private const int ChargeRange = 1800;
        private const int ChargeOvershoot = 400;
        private const float ChargeStandOff = 760f;
        private const float ChargeMinimumLineupDistance = 315f;
        private const float RamBrakeDistance = 17f * 16f;
        private const float RamAimDistance = 4f * 16f;
        private const float RamApproachSpeed = 12f;
        private const float RamTrackingSpeed = 4.7f;
        private const float RamSpeed = 25f;
        private const float RamTellSpeed = 2.5f;
        internal const int RamBeamTellTicks = 38;
        private const int RamTrackingTimeout = 150;
        private const int RamAccelerationTicks = 30;
        private const float RamSideAlignment = 0.82f;
        private const int RamNeckOrdinal = 2;
        private const int RamShortPairCount = 50;
        private const int MawLock = DestroyerMawBeam.TellTicks;
        private const int DashReaim = 12;
        private const float MawPerchOffset = 475f;
        private const float MawPerchHeight = 88f;
        private const int RunawayWindupTicks = 55;
        private const int RunawayRampTicks = 15;
        private const int RunawayFirstSwing = 80;
        private const int RunawayFirstPause = 12;
        private const int RunawaySecondSwing = 86;
        private const int RunawaySecondPause = 14;
        private const int RunawayFinalSwing = 72;
        internal const int RunawayFireTicks = RunawayFirstSwing + RunawayFirstPause + RunawaySecondSwing + RunawaySecondPause + RunawayFinalSwing;

        private static readonly (float StartDegrees, float EndDegrees, int SwingTicks, int PauseTicks)[] runawaySweeps =
        {
            (-68f, 57f, RunawayFirstSwing, RunawayFirstPause),
            (57f, -72f, RunawaySecondSwing, RunawaySecondPause),
            (-72f, 35f, RunawayFinalSwing, 0)
        };

        private static readonly (Vector2 Offset, float AimDegrees, int Ticks)[] thrashSweeps =
        {
            (new Vector2(110f, 66f), 55f, ThrashFirstSweep),
            (new Vector2(-106f, -138f), -68f, ThrashSecondSweep),
            (new Vector2(76f, 49f), 60f, ThrashFinalSweep)
        };
        private const float ThrashOpeningDegrees = -58f;

        // Live angles in radians. The uneven spacing leaves a wider lane as the beams sweep together.
        private static readonly float[] enclosingFirstVolleyAngles = { 0.61f, MathHelper.Pi + 1.60f, 2.52f };
        private static readonly float[] enclosingSecondVolleyAngles = { 0.49f, 1.29f, 2.64f };

        private enum Move : byte
        {
            None,
            BroadsidePass2,
            CurtainCall2,
            HeadCharge,
            EnclosingCoil,
            HullGlowSpin,
            MawBeam,
            RippleBroadside,
            BombDrop,
            MineDrop,
            MissileBarrage,
            ProbeBarrage,
            BroadsidePass1,
            CurtainCall1,
            HourglassSpin,
            ReactorBreaker,
            RunawayMaw,
            HullThrash
        }

        private enum Beat : byte
        {
            Prowl,
            Setup,
            Shape,
            Windup,
            Active,
            Recovery,
            Burrow,
            Erupt,
            Retreat,
            RamLineup
        }

        internal enum HullHeat : byte
        {
            Cold,
            Arming,
            Warm,
            Hot
        }

        private static BasicEffect hullLightingEffect;
        private static Asset<Texture2D> bombBayFlare;
        private static readonly VertexPositionColorTexture[] hullVertices = new VertexPositionColorTexture[9];
        private static readonly short[] hullIndices =
        {
            0, 1, 3, 1, 4, 3,
            1, 2, 4, 2, 5, 4,
            3, 4, 6, 4, 7, 6,
            4, 5, 7, 5, 8, 7
        };

        private Move move;
        private Beat beat;
        private int phase = 1;
        private int timer;
        private int duration = 180;
        private int sequence;
        private int chainLength;
        private int pendingPhase;
        private int pendingProbeWaves;
        private int releasedProbeWaves;
        private int releasedWaveAttackers;
        private bool supportedProbeWave;
        private int spawnOrdinal;
        private int sweepSign = 1;
        private int heading = 1;
        private float angle;
        private Vector2 anchor;
        private Vector2 goal;
        private Vector2 lockedDirection = Vector2.UnitX;
        private Vector2 lockedOrigin;
        private bool vulnerable;
        private int orbitBeamOrdinal;
        private int orbitBeamStart;
        private int driveThroughResolvedPorts;
        private int driveThroughLastShot;
        private int driveThroughFadeUntil;
        private int curtainFiredPorts;
        private bool curtainReachedBoundary;
        private bool curtainTurning;
        private int ramLiveTicks;
        private bool ramBackedOff;
        private int attacksBeforeNextOrbit;
        private float hullTurn;
        private Vector2 hullTravel;
        private int payloadReleaseTick = -1;
        private int bombBayTellTick = -1;
        private int bombBaySide;
        private HullHeat beamHeat;
        private float beamArmingProgress;
        private ulong beamHeatTick;
        private bool announcedBeat;
        private bool bodyPoseForMaw;
        private bool openedFight;
        private AttackBag<Move> attackBag;
        private int probeOwner = -1;
        private bool legacyProbe;
        private bool fireBreathProbe;
        private bool repairOnlyProbe;
        private int probeOpeningTicks;
        private int probeLaserCooldown = ProbeLaserTellTicks;
        private bool probeExploded;
        private int repairSegment = -1;
        private int repairStart = -1;
        private int repairCooldown;
        private int probeBreathTimer;
        private int probeBreathCooldown;
        private int probeKamikazeTimer;
        private Vector2 probeAimDirection = Vector2.UnitX;

        public override bool InstancePerEntity => true;
        internal bool IsRevampProbe => probeOwner >= 0;
        internal bool IsLegacyProbe => legacyProbe;
        internal bool IsRetreating => beat == Beat.Retreat;
        internal bool IsCurtainCallTwo => move == Move.CurtainCall2;
        internal bool IsRunawayMaw => move == Move.RunawayMaw;
        internal bool IsRunawayMawFiring => IsRunawayMaw && beat == Beat.Active;
        internal bool IsHullThrash => move == Move.HullThrash;
        internal bool IsHullThrashFiring => IsHullThrash && beat == Beat.Active;
        internal bool IsRamLiningUp => move == Move.HeadCharge && (beat == Beat.Shape || beat == Beat.Active);
        internal int RamBeamLiveTime => move == Move.HeadCharge && beat == Beat.Active
            && Math.Abs(lockedDirection.X) >= RamSideAlignment ? Math.Max(0, duration - timer + 1) : 0;

        public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        {
            return npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody
                || npc.type == NPCID.TheDestroyerTail || npc.type == NPCID.Probe;
        }

        public string DebugAttackLabel
        {
            get
            {
                string label = move switch
                {
                    Move.None => beat.ToString(),
                    Move.BroadsidePass1 => "Broadside Pass 1",
                    Move.BroadsidePass2 => "Broadpass 2",
                    Move.CurtainCall1 => "Curtain Call 1",
                    Move.CurtainCall2 => "Curtain Call 2",
                    _ => DebugLabels.Humanize(move.ToString())
                };
                return $"P{phase} · {label}";
            }
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPCID.TheDestroyer] = 10;
            NPCID.Sets.TrailingMode[NPCID.TheDestroyer] = 0;
        }

        public override void Unload()
        {
            BasicEffect effectToDispose = hullLightingEffect;
            hullLightingEffect = null;
            bombBayFlare = null;

            if (effectToDispose != null && !effectToDispose.IsDisposed)
            {
                // Mod unloading can run on a worker. FNA releases GPU effects only on the main thread.
                Main.QueueMainThreadAction(effectToDispose.Dispose);
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
        {
            if (npc.type == NPCID.Probe)
            {
                // Raise solo Expert HP from 300 to 500. The ratio retains vanilla difficulty and player-count scaling.
                npc.lifeMax = npc.lifeMax * 500 / 300;
            }
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (npc.type == NPCID.Probe && source is EntitySource_Parent { Entity: NPC parent })
            {
                if (parent.type == NPCID.TheDestroyer)
                {
                    probeOwner = parent.whoAmI;
                }
                else if (parent.type == NPCID.TheDestroyerBody)
                {
                    probeOwner = parent.realLife;
                }
                else if (parent.ModNPC is Original.TheDestroyerOriginalBody)
                {
                    legacyProbe = true;
                }
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            if (npc.type == NPCID.Probe)
            {
                bitWriter.WriteBit(legacyProbe);
                bitWriter.WriteBit(fireBreathProbe);
                bitWriter.WriteBit(repairOnlyProbe);
                writer.Write((short)probeOwner);
                writer.Write((short)repairSegment);
                writer.Write((short)repairStart);
                writer.Write((short)probeBreathTimer);
                writer.Write((short)probeKamikazeTimer);
                writer.WriteVector2(probeAimDirection);
                return;
            }

            if (npc.type != NPCID.TheDestroyer)
            {
                writer.Write((byte)spawnOrdinal);
                writer.Write((short)payloadReleaseTick);
                writer.Write((short)bombBayTellTick);
                writer.Write((sbyte)bombBaySide);
                return;
            }

            writer.Write((byte)move);
            writer.Write((byte)beat);
            writer.Write((byte)phase);
            writer.Write((short)timer);
            writer.Write((short)duration);
            writer.Write((byte)sequence);
            writer.Write((byte)pendingPhase);
            writer.Write((sbyte)sweepSign);
            writer.Write((sbyte)heading);
            writer.Write(angle);
            writer.WriteVector2(anchor);
            writer.WriteVector2(goal);
            writer.WriteVector2(lockedDirection);
            writer.WriteVector2(lockedOrigin);
            writer.Write(vulnerable);
            writer.Write((byte)orbitBeamOrdinal);
            writer.Write((short)orbitBeamStart);
            writer.Write((byte)attacksBeforeNextOrbit);
            writer.Write(bodyPoseForMaw);
            writer.Write(curtainTurning);
            writer.Write((byte)curtainFiredPorts);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            if (npc.type == NPCID.Probe)
            {
                legacyProbe = bitReader.ReadBit();
                fireBreathProbe = bitReader.ReadBit();
                repairOnlyProbe = bitReader.ReadBit();
                probeOwner = reader.ReadInt16();
                repairSegment = reader.ReadInt16();
                repairStart = reader.ReadInt16();
                probeBreathTimer = reader.ReadInt16();
                probeKamikazeTimer = reader.ReadInt16();
                probeAimDirection = reader.ReadVector2();
                return;
            }

            if (npc.type != NPCID.TheDestroyer)
            {
                spawnOrdinal = reader.ReadByte();
                payloadReleaseTick = reader.ReadInt16();
                bombBayTellTick = reader.ReadInt16();
                bombBaySide = reader.ReadSByte();
                return;
            }

            Move nextMove = (Move)reader.ReadByte();
            Beat nextBeat = (Beat)reader.ReadByte();
            if (nextMove != move || nextBeat != beat)
            {
                announcedBeat = false;
            }

            move = nextMove;
            beat = nextBeat;
            phase = reader.ReadByte();
            timer = reader.ReadInt16();
            duration = reader.ReadInt16();
            sequence = reader.ReadByte();
            pendingPhase = reader.ReadByte();
            sweepSign = reader.ReadSByte();
            heading = reader.ReadSByte();
            angle = reader.ReadSingle();
            anchor = reader.ReadVector2();
            goal = reader.ReadVector2();
            lockedDirection = reader.ReadVector2();
            lockedOrigin = reader.ReadVector2();
            vulnerable = reader.ReadBoolean();
            orbitBeamOrdinal = reader.ReadByte();
            orbitBeamStart = reader.ReadInt16();
            attacksBeforeNextOrbit = reader.ReadByte();
            bodyPoseForMaw = reader.ReadBoolean();
            curtainTurning = reader.ReadBoolean();
            curtainFiredPorts = reader.ReadByte();
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.type == NPCID.TheDestroyer && vulnerable)
            {
                modifiers.FinalDamage *= 1.5f;
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.type == NPCID.Probe)
            {
                return !repairOnlyProbe;
            }

            HullHeat heat = GetHullHeat(npc, out _);
            return heat >= HullHeat.Warm;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (npc.type != NPCID.Probe && GetHullHeat(npc, out _) == HullHeat.Warm)
            {
                modifiers.FinalDamage *= WarmContactMultiplier;
            }
        }

        public override bool CheckActive(NPC npc)
        {
            if (npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
            {
                if (TryGetHead(npc, out NPC head) && head.GetGlobalNPC<TheDestroyer>().IsRetreating)
                {
                    return base.CheckActive(npc);
                }

                return false;
            }

            return base.CheckActive(npc);
        }

        internal void RunFight(NPC npc)
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (npc.type != NPCID.TheDestroyer)
            {
                float previousRotation = npc.rotation;
                Vector2 previousPosition = npc.position;
                RunMovement(npc, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, false);
                hullTravel = npc.position - previousPosition;
                hullTurn = MathHelper.WrapAngle(npc.rotation - previousRotation);
                if (npc.active && spawnOrdinal > 0 && TryGetHead(npc, out NPC head))
                {
                    TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
                    if (!Main.dedServ && headState.move == Move.HourglassSpin
                        && (headState.beat == Beat.Shape || headState.beat == Beat.Active))
                    {
                        EmitHourglassFire(npc, head.velocity);
                    }

                    bool bombPass = headState.move == Move.BombDrop && headState.beat == Beat.Active;
                    if (npc.type == NPCID.TheDestroyerBody && headState.move == Move.MissileBarrage && headState.beat == Beat.Active)
                    {
                        int missileAge = headState.MissileBayAge(spawnOrdinal);
                        if (missileAge > 0 && missileAge < MissileTelegraphTicks + MissileHotTicks)
                        {
                            RunMissileBay(npc, head, missileAge, authority);
                        }
                    }

                    if (authority && bombPass && bombBayTellTick >= 0 && payloadReleaseTick < 0 && headState.timer >= bombBayTellTick + BombBayTellTicks)
                    {
                        // Release after the body follows the head so the projectile and flash share the bay's current pose.
                        Vector2 dropPosition = BombBayPosition(npc);
                        Vector2 dropVelocity = new Vector2(head.velocity.X * 0.12f, 2.5f);
                        int projectileIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), dropPosition, dropVelocity,
                            ModContent.ProjectileType<EnemyFirebomb>(), EnemyDamage.Projectile(BombDamage), 9f,
                            Main.myPlayer, 0f, 0f, EnemyFirebomb.DestroyerDrop);
                        if (projectileIndex < Main.maxProjectiles)
                        {
                            payloadReleaseTick = headState.timer;
                            npc.netUpdate = true;
                            if (!Main.dedServ)
                            {
                                SoundEngine.PlaySound(SoundID.Item1, dropPosition);
                            }
                        }
                    }

                    if (!Main.dedServ && npc.type == NPCID.TheDestroyerBody)
                    {
                        HullHeat heat = GetHullHeat(npc, out _);
                        bool droppingPayload = headState.move == Move.BombDrop || headState.move == Move.MineDrop;
                        bool launchingPayload = headState.move == Move.MineDrop || headState.move == Move.MissileBarrage;
                        if (headState.move == Move.ReactorBreaker && heat >= HullHeat.Warm
                            && (Main.GameUpdateCount + npc.whoAmI) % 4 == 0)
                        {
                            // Throw fire off the outside of the rotating hull, trailing the segment's actual travel.
                            Vector2 outward = npc.rotation.ToRotationVector2() * -headState.sweepSign;
                            Vector2 vent = npc.Center + outward * (HullPortOffset * npc.scale);
                            Vector2 drift = outward * Main.rand.NextFloat(1.8f, 4.8f) - hullTravel * 0.14f;
                            drift += Main.rand.NextVector2Circular(2f, 2f);
                            Dust flame = Dust.NewDustPerfect(vent, DustID.OrangeTorch, drift, Scale: Main.rand.NextFloat(1.05f, 1.65f));
                            flame.noGravity = true;
                            Lighting.AddLight(vent, 0.60f, 0.20f, 0.045f);
                        }

                        if (headState.move == Move.HullThrash && heat >= HullHeat.Warm && Main.rand.NextBool(9))
                        {
                            int side = Main.rand.NextBool() ? 1 : -1;
                            Vector2 outward = npc.rotation.ToRotationVector2() * side;
                            Vector2 vent = npc.Center + outward * (HullPortOffset * npc.scale);
                            Vector2 sparkVelocity = outward * Main.rand.NextFloat(4f, 10f) - hullTravel * 0.20f;
                            sparkVelocity += Main.rand.NextVector2Circular(2f, 3f);
                            Dust spark = Dust.NewDustPerfect(vent, DustID.Electric, sparkVelocity, Scale: Main.rand.NextFloat(0.85f, 1.3f));
                            spark.noGravity = true;
                        }

                        if (launchingPayload && heat == HullHeat.Hot && (Main.GameUpdateCount + npc.whoAmI) % 4 == 0)
                        {
                            Vector2 sparkVelocity = Main.rand.NextVector2Circular(2.7f, 2.7f);
                            Dust spark = Dust.NewDustPerfect(npc.Center, DustID.Torch, sparkVelocity, Scale: 0.9f);
                            spark.noGravity = true;
                        }

                        if (bombPass)
                        {
                            float flash = BombBayFlash(headState);
                            if (flash > 0f)
                            {
                                Vector2 bay = BombBayPosition(npc);
                                Lighting.AddLight(bay, flash, flash * flash * 0.55f, flash * 0.10f);
                                bool justReleased = payloadReleaseTick == headState.timer;
                                if (justReleased || payloadReleaseTick < 0 && headState.timer % 5 == 0)
                                {
                                    int count = justReleased ? 6 : 1;
                                    for (int i = 0; i < count; i++)
                                    {
                                        Vector2 speed = Main.rand.NextVector2Circular(2.4f, 1.8f) + Vector2.UnitY * 1.2f;
                                        Dust spark = Dust.NewDustPerfect(bay, DustID.Torch, speed, Scale: 0.8f);
                                        spark.noGravity = true;
                                    }
                                }
                            }
                        }

                        if (droppingPayload && headState.beat == Beat.Shape && heat == HullHeat.Warm && headState.timer % 3 == 0)
                        {
                            Vector2 offset = Main.rand.NextVector2Circular(12f, 12f) + new Vector2(0f, 14f);
                            int dustType = headState.move == Move.BombDrop ? DustID.RedTorch : DustID.OrangeTorch;
                            Dust.NewDustPerfect(npc.Center + offset, dustType, new Vector2(0f, 1.5f), Scale: 1.65f).noGravity = true;
                        }
                    }
                }

                return;
            }

            if (authority && (npc.target < 0 || npc.target >= Main.maxPlayers
                || !Main.player[npc.target].active || Main.player[npc.target].dead))
            {
                npc.TargetClosest();
            }

            bool hasTarget = npc.target >= 0 && npc.target < Main.maxPlayers
                && Main.player[npc.target].active && !Main.player[npc.target].dead;
            if (authority && !hasTarget && beat != Beat.Retreat)
            {
                move = Move.None;
                vulnerable = false;
                SetBeat(npc, Beat.Retreat, 0);
            }

            bool directed = beat != Beat.Retreat;
            RunMovement(npc, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, false, directed);
            if (!npc.active || beat == Beat.Retreat)
            {
                return;
            }

            if (!Main.dedServ)
            {
                DecorateTerrainTravel(npc);
            }

            // Grant the Updraft (ThermalRise) buff for the entire Destroyer fight, matching the legacy fight.
            int updraftBuff = ModContent.BuffType<Buffs.ThermalRise>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidatePlayer = Main.player[i];
                if (candidatePlayer.active && !candidatePlayer.dead
                    && Vector2.DistanceSquared(candidatePlayer.Center, npc.Center) <= 4500f * 4500f)
                {
                    candidatePlayer.AddBuff(updraftBuff, 60);
                }
            }

            Player player = null;
            if (hasTarget)
            {
                player = Main.player[npc.target];
            }

            if (authority && player != null)
            {
                if (!openedFight)
                {
                    openedFight = true;
                    StartAttack(npc, player, Move.HeadCharge, skipSetup: true);
                }

                // Latch each crossed milestone once, including a hit that crosses more than one.
                while (pendingProbeWaves < ProbeWaveCount
                    && npc.life * 4 <= npc.lifeMax * (ProbeWaveCount - pendingProbeWaves))
                {
                    pendingProbeWaves++;
                }

                if (releasedProbeWaves < pendingProbeWaves)
                {
                    ReleaseProbeWave(npc);
                }

                if (npc.life * 10 <= npc.lifeMax * 3)
                {
                    pendingPhase = 3;
                }
                else if (npc.life * 10 <= npc.lifeMax * 6)
                {
                    pendingPhase = Math.Max(pendingPhase, 2);
                }

                if (beat == Beat.Prowl && pendingPhase > phase)
                {
                    // Start the transition after the current pattern drains. Shown tells keep their shots.
                    phase++;
                    attackBag?.Clear();
                    anchor = player.Center;
                    if (player.Center.X >= npc.Center.X)
                    {
                        heading = 1;
                    }
                    else
                    {
                        heading = -1;
                    }

                    SetBeat(npc, Beat.Burrow, 65);
                }
            }

            timer++;
            switch (beat)
            {
                case Beat.Prowl:
                    if (player != null)
                    {
                        float prowlWave = (float)Math.Sin((Main.GameUpdateCount + npc.whoAmI * 17) * 0.032f);
                        float prowlCos = (float)Math.Cos((Main.GameUpdateCount + npc.whoAmI * 17) * 0.021f);
                        Vector2 orbitOffset = new Vector2(prowlCos * 300f * heading, prowlWave * 180f);
                        Vector2 targetPoint = player.Center + orbitOffset;
                        float prowlSpeed = 13f + phase * 1.2f;
                        if (!npc.WithinRange(player.Center, 700f))
                        {
                            targetPoint = player.Center;
                            prowlSpeed = 19f;
                        }

                        Vector2 desiredProwl = (targetPoint - npc.Center).SafeNormalize(Vector2.UnitX * heading) * prowlSpeed;
                        SteerHead(npc, desiredProwl, 0.065f, 0.08f);
                    }

                    if (authority && timer >= duration && player != null)
                    {
                        PickAttack(npc, player);
                    }
                    break;
                case Beat.Burrow:
                    Vector2 diveVelocity = new Vector2(heading * 10f, 17f);
                    SteerHead(npc, diveVelocity, 0.055f, 0.10f);
                    if (!Main.dedServ && timer % 3 == 0)
                    {
                        Vector2 dustPosition = FindSurface(new Vector2(npc.Center.X, anchor.Y));
                        Dust.NewDustPerfect(dustPosition, DustID.Dirt, new Vector2(Main.rand.NextFloat(-4f, 4f), -3f), Scale: 1.8f);
                    }

                    if (!announcedBeat && !Main.dedServ)
                    {
                        announcedBeat = true;
                        SoundEngine.PlaySound(SoundID.Roar, anchor);
                        UsefulFunctions.ScreenShake(anchor, 6f, 70);
                    }

                    if (authority && timer >= duration)
                    {
                        goal = FindSurface(anchor - Vector2.UnitX * heading * 360f);
                        SetBeat(npc, Beat.Erupt, 65);
                    }
                    break;
                case Beat.Erupt:
                    Vector2 climbTarget = anchor - Vector2.UnitY * 180f;
                    if (player != null)
                    {
                        climbTarget = player.Center - Vector2.UnitY * 160f;
                    }

                    Vector2 eruptVelocity = (climbTarget - npc.Center).SafeNormalize(-Vector2.UnitY) * 18f;
                    SteerHead(npc, eruptVelocity, 0.062f, 0.12f);
                    if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                    {
                        Vector2 dustPosition = goal + Main.rand.NextVector2Circular(46f, 8f);
                        Dust.NewDustPerfect(dustPosition, DustID.Dirt, -Vector2.UnitY * Main.rand.NextFloat(2f, 7f), Scale: 2.1f);
                    }

                    if (timer >= duration)
                    {
                        // Keep climbing before attacking. Hold the synced timer at its minimum duration until the head nears the point above the target.
                        timer = duration;
                        if (authority && player != null)
                        {
                            float distanceToClimbTargetSquared = Vector2.DistanceSquared(npc.Center, climbTarget);
                            if (distanceToClimbTargetSquared <= EruptionReentryRange * EruptionReentryRange)
                            {
                                PickAttack(npc, player);
                            }
                        }
                    }
                    break;
                default:
                    switch (move)
                    {
                        case Move.BroadsidePass2:
                        case Move.CurtainCall2:
                            RunPass(npc, player, authority);
                            break;
                        case Move.RippleBroadside:
                            RunRippleBroadside(npc, player, authority);
                            break;
                        case Move.BroadsidePass1:
                        case Move.CurtainCall1:
                            RunFirstPass(npc, player, authority);
                            break;
                        case Move.EnclosingCoil:
                        case Move.HullGlowSpin:
                        case Move.HourglassSpin:
                        case Move.ReactorBreaker:
                            RunOrbit(npc, authority);
                            break;
                        case Move.HeadCharge:
                            RunCharge(npc, player, authority);
                            break;
                        case Move.MawBeam:
                            RunMaw(npc, player, authority);
                            break;
                        case Move.RunawayMaw:
                            RunRunawayMaw(npc, player, authority);
                            break;
                        case Move.HullThrash:
                            RunHullThrash(npc, player, authority);
                            break;
                        case Move.BombDrop:
                        case Move.MineDrop:
                            RunBombDrop(npc, player, authority);
                            break;
                        case Move.MissileBarrage:
                            RunMissileBarrage(npc, player, authority);
                            break;
                        case Move.ProbeBarrage:
                            RunProbeBarrage(npc, player, authority);
                            break;
                    }
                    break;
            }
        }

        private void PickAttack(NPC npc, Player player)
        {
            if (attackBag == null)
            {
                attackBag = new AttackBag<Move>(cards =>
                {
                    cards.Add(Move.BroadsidePass2);
                    cards.Add(Move.BroadsidePass2);
                    cards.Add(Move.CurtainCall2);
                    cards.Add(Move.BroadsidePass1);
                    cards.Add(Move.CurtainCall1);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.EnclosingCoil);
                    cards.Add(Move.EnclosingCoil);
                    cards.Add(Move.HullGlowSpin);
                    cards.Add(Move.HullGlowSpin);
                    cards.Add(Move.HullGlowSpin);
                    cards.Add(Move.HourglassSpin);
                    cards.Add(Move.ReactorBreaker);
                    cards.Add(Move.RunawayMaw);
                    cards.Add(Move.BombDrop);
                    cards.Add(Move.BombDrop);
                    cards.Add(Move.MineDrop);
                    cards.Add(Move.MineDrop);
                    cards.Add(Move.MissileBarrage);
                    cards.Add(Move.ProbeBarrage);
                    if (phase == 2)
                    {
                        cards.Add(Move.HeadCharge);
                        cards.Add(Move.BombDrop);
                        cards.Add(Move.MawBeam);
                        cards.Add(Move.MawBeam);
                        cards.Add(Move.RippleBroadside);
                        cards.Add(Move.ProbeBarrage);
                        cards.Add(Move.ProbeBarrage);
                    }
                    else if (phase == 3)
                    {
                        cards.Add(Move.HullThrash);
                        cards.Add(Move.HullThrash);
                        cards.Add(Move.HeadCharge);
                        cards.Add(Move.MineDrop);
                        cards.Add(Move.MawBeam);
                        cards.Add(Move.MawBeam);
                        cards.Add(Move.MawBeam);
                        cards.Add(Move.RippleBroadside);
                        cards.Add(Move.ProbeBarrage);
                        cards.Add(Move.ProbeBarrage);
                    }
                });
            }

            bool withinOrbitRange = npc.WithinRange(player.Center, 1100f);
            bool canCircle = attacksBeforeNextOrbit == 0 && withinOrbitRange;
            bool airborne = canCircle && player.gravDir > 0f;
            if (airborne)
            {
                // Require seven clear tiles below the player. Check one tile at a time to catch thin platforms.
                Vector2 floorProbe = Vector2.UnitY * 16f;
                Vector2 position = player.position;
                for (int i = 0; i < 7; i++)
                {
                    Vector2 floorTravel = Collision.TileCollision(position, floorProbe, player.width, player.height);
                    if (floorTravel.Y < floorProbe.Y - 1f)
                    {
                        airborne = false;
                        break;
                    }

                    position += floorProbe;
                }
            }

            Vector2 overheadApproach = player.Center - Vector2.UnitY * (OverheadHeight - OverheadRadius);
            bool canSpin = canCircle && npc.life * 2 < npc.lifeMax && npc.Center.Y + 96f <= player.Center.Y
                && overheadApproach.Y > 320f && Collision.CanHitLine(player.Center, 1, 1, overheadApproach, 1, 1);
            Vector2 hourglassApproach = player.Center - Vector2.UnitY * (HourglassHeight - OverheadRadius);
            bool canHourglass = canCircle && npc.life * 2 < npc.lifeMax && hourglassApproach.Y > 320f
                && Collision.CanHitLine(player.Center, 1, 1, hourglassApproach, 1, 1);
            bool hasMawPerch = npc.life * 2 < npc.lifeMax && withinOrbitRange
                && (HasMawPerch(player.Center, 1) || HasMawPerch(player.Center, -1));
            bool canThrash = phase == 3 && npc.life * 10 < npc.lifeMax * 3 && hasMawPerch;

            // Lead the final phase with the thrash if the head has room to start the beam.
            if (canThrash && beat == Beat.Erupt)
            {
                StartAttack(npc, player, Move.HullThrash);
                return;
            }

            // Wait for range or an eligible non-repeat. AttackBag's default fallback permits repeats.
            bool drew = attackBag.TryDraw(candidate => candidate != attackBag.Last
                && (candidate != Move.HeadCharge || npc.Distance(player.Center) <= ChargeRange)
                && (candidate != Move.RippleBroadside || npc.WithinRange(player.Center, 950f))
                && (candidate != Move.EnclosingCoil || canCircle && airborne)
                && (candidate != Move.HullGlowSpin || canSpin)
                && (candidate != Move.HourglassSpin && candidate != Move.ReactorBreaker || canHourglass)
                && (candidate != Move.RunawayMaw || hasMawPerch)
                && (candidate != Move.HullThrash || canThrash)
                && (candidate != Move.BombDrop || (npc.life * 4 < npc.lifeMax * 3 && npc.life * 10 >= npc.lifeMax * 3))
                && (candidate != Move.MineDrop || npc.life * 10 < npc.lifeMax * 3)
                && (candidate != Move.MissileBarrage || npc.life * 2 < npc.lifeMax)
                && (candidate != Move.ProbeBarrage || CountActiveProbes(npc) >= 4), out Move nextMove);
            if (!drew)
            {
                move = Move.None;
                SetBeat(npc, Beat.Prowl, 45);
                return;
            }

            StartAttack(npc, player, nextMove);
        }

        private void StartAttack(NPC npc, Player player, Move nextMove, bool skipSetup = false)
        {
            move = nextMove;
            sequence = 0;
            vulnerable = false;
            bodyPoseForMaw = false;
            curtainTurning = false;
            curtainFiredPorts = 0;
            anchor = player.Center;
            goal = anchor;
            if (player.Center.X >= npc.Center.X)
            {
                heading = 1;
            }
            else
            {
                heading = -1;
            }

            chainLength = phase;
            if (move == Move.EnclosingCoil || move == Move.HullGlowSpin || move == Move.HourglassSpin || move == Move.ReactorBreaker)
            {
                attacksBeforeNextOrbit = 2;
                orbitBeamOrdinal = 0;
                orbitBeamStart = 0;
            }
            else if (attacksBeforeNextOrbit > 0)
            {
                attacksBeforeNextOrbit--;
            }

            switch (move)
            {
                case Move.BroadsidePass2:
                case Move.CurtainCall2:
                    sweepSign = npc.Center.Y <= player.Center.Y ? 1 : -1;
                    SetPassLane();
                    break;
                case Move.RippleBroadside:
                    sweepSign = npc.Center.Y <= player.Center.Y ? 1 : -1;
                    goal = anchor - new Vector2(heading * RippleEntryDistance, sweepSign * RippleEntryHeight);
                    break;
                case Move.BroadsidePass1:
                case Move.CurtainCall1:
                    bool curtain = move == Move.CurtainCall1;
                    sweepSign = npc.Center.Y <= player.Center.Y ? 1 : -1;
                    float baseAngle = heading > 0 ? 0f : MathHelper.Pi;
                    float tilt;
                    if (curtain)
                    {
                        tilt = Main.rand.NextFloat(14f, 34f) * sweepSign;
                    }
                    else
                    {
                        int tiltSign = Main.rand.NextBool() ? 1 : -1;
                        tilt = Main.rand.NextFloat(18f, 44f) * tiltSign;
                    }

                    angle = baseAngle + MathHelper.ToRadians(tilt);
                    Vector2 firstPassDirection = angle.ToRotationVector2();
                    Vector2 firstPassNormal = firstPassDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                    float sideOffset = curtain ? -330f : -280f;
                    goal = anchor - firstPassDirection * 460f + firstPassNormal * sideOffset;
                    break;
                case Move.HeadCharge:
                    ramBackedOff = false;
                    goal = anchor + (npc.Center - anchor).SafeNormalize(-Vector2.UnitX) * ChargeStandOff;
                    break;
                case Move.ProbeBarrage:
                    goal = anchor + (npc.Center - anchor).SafeNormalize(-Vector2.UnitX) * 420f;
                    break;
                case Move.EnclosingCoil:
                case Move.HullGlowSpin:
                case Move.HourglassSpin:
                case Move.ReactorBreaker:
                    float orbitRadius = EnclosingRadius;
                    if (move == Move.HullGlowSpin)
                    {
                        anchor.Y -= OverheadHeight;
                        orbitRadius = OverheadRadius;
                    }
                    else if (move == Move.HourglassSpin || move == Move.ReactorBreaker)
                    {
                        anchor.Y -= HourglassHeight;
                        orbitRadius = OverheadRadius;
                    }

                    sweepSign = -heading;
                    if (move == Move.HourglassSpin || move == Move.ReactorBreaker)
                    {
                        heading = Main.rand.NextBool() ? 1 : -1;
                    }

                    Vector2 fromOrbitCenter = npc.Center - anchor;
                    float distanceFromOrbitCenter = fromOrbitCenter.Length();
                    angle = fromOrbitCenter.ToRotation();
                    if (distanceFromOrbitCenter > orbitRadius)
                    {
                        // Approach from the tangent when outside the ring so the trailing hull follows into the curve.
                        float tangentAngle = MathF.Acos(orbitRadius / distanceFromOrbitCenter);
                        angle += sweepSign * tangentAngle;
                    }

                    goal = anchor + angle.ToRotationVector2() * orbitRadius;
                    break;
                case Move.MawBeam:
                    Vector2 fromPlayer = npc.Center - anchor;
                    float currentDistance = fromPlayer.Length();
                    Vector2 perchDirection = fromPlayer.SafeNormalize(-Vector2.UnitX * heading);
                    // Aim to place the head 20-40 tiles (320-640 px) from the player along its current side so the body stays open.
                    float targetDistance = MathHelper.Clamp(currentDistance, 380f, 560f);
                    goal = anchor + perchDirection * targetDistance;
                    break;
                case Move.RunawayMaw:
                case Move.HullThrash:
                    if (!HasMawPerch(anchor, heading))
                    {
                        heading = -heading;
                    }

                    goal = anchor + new Vector2(-heading * MawPerchOffset, -MawPerchHeight);
                    break;
                case Move.BombDrop:
                case Move.MineDrop:
                    goal = anchor + new Vector2(-heading * 460f, -240f);
                    foreach (NPC segment in Main.ActiveNPCs)
                    {
                        if (segment.type == NPCID.TheDestroyerBody && segment.realLife == npc.whoAmI)
                        {
                            segment.localAI[1] = 0f;
                            TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                            if (segmentState.payloadReleaseTick >= 0 || segmentState.bombBayTellTick >= 0)
                            {
                                segmentState.payloadReleaseTick = -1;
                                segmentState.bombBayTellTick = -1;
                                segmentState.bombBaySide = 0;
                                segment.netUpdate = true;
                            }
                        }
                    }
                    break;
                case Move.MissileBarrage:
                    goal = anchor + new Vector2(-heading * 420f, -200f);
                    break;
            }

            if (skipSetup)
            {
                SetBeat(npc, Beat.Windup, ChargeAimTicks);
            }
            else
            {
                bool longSetup = move == Move.BroadsidePass2 || move == Move.CurtainCall2;
                int setupTicks = longSetup ? DriveThroughSetupTicks : 140;
                if (move == Move.RippleBroadside)
                {
                    setupTicks = RippleSetupTicks;
                }

                SetBeat(npc, Beat.Setup, setupTicks);
            }
        }

        private void SetPassLane()
        {
            float tilt = move == Move.BroadsidePass2 ? 0.36f : 0.21f;
            Vector2 direction = new Vector2(heading, -sweepSign * tilt);
            angle = direction.ToRotation();
            Vector2 laneCenter = anchor - Vector2.UnitY * (sweepSign * CurrentPassHeight);
            goal = laneCenter - angle.ToRotationVector2() * DriveThroughEntry;
        }

        private float CurrentPassHeight => move == Move.BroadsidePass2 ? 440f : PassLaneHeight;

        private void RunFirstPass(NPC npc, Player player, bool authority)
        {
            bool curtain = move == Move.CurtainCall1;
            Vector2 passDirection = angle.ToRotationVector2();
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= duration))
                {
                    if (player != null)
                    {
                        anchor = player.Center;
                        Vector2 passNormal = passDirection.RotatedBy(MathHelper.PiOver2);
                        float cross = Vector2.Dot(anchor - npc.Center, passNormal);
                        sweepSign = cross >= 0f ? 1 : -1;
                    }

                    SetBeat(npc, Beat.Shape, 60);
                }

                return;
            }

            int stride = FirstPassStride;
            if (beat == Beat.Shape)
            {
                float shapeSpeed = curtain ? 13.5f : 15f;
                SteerHead(npc, passDirection * shapeSpeed, 0.055f, 0.12f);
                if (authority && timer >= duration)
                {
                    int lastOrdinal = 0;
                    foreach (NPC segment in Main.ActiveNPCs)
                    {
                        if (segment.type == NPCID.TheDestroyerBody && segment.realLife == npc.whoAmI)
                        {
                            int ordinal = segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                            lastOrdinal = Math.Max(lastOrdinal, ordinal);
                        }
                    }

                    int lastEmitterTick = Math.Max(0, lastOrdinal / stride - 1) * RippleDelay + 1;
                    int passTicks = lastEmitterTick + PortTell + HullTell + DestroyerHullLaser.FireTicks + DestroyerHullLaser.FadeTicks;
                    SetBeat(npc, Beat.Active, passTicks);
                }

                return;
            }

            // Restore the original curved pass and let its eight-tick ripple run down the hull.
            float curveOffset = MathF.Sin(timer * 0.035f) * 0.18f + sweepSign * 0.0025f * timer;
            Vector2 currentDirection = (angle + curveOffset).ToRotationVector2();
            float firingSpeed = curtain ? 11.5f : 10.5f;
            SteerHead(npc, currentDirection * firingSpeed, 0.045f, 0.10f);
            if (!authority)
            {
                return;
            }

            int rippleTick = timer - PortTell - 1;
            if (rippleTick >= 0 && rippleTick % RippleDelay == 0)
            {
                int emitterOrdinal = (rippleTick / RippleDelay + 1) * stride;
                Vector2 fireDirection = currentDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                if (player != null && Vector2.Dot(player.Center - npc.Center, fireDirection) < -60f)
                {
                    fireDirection = -fireDirection;
                }

                foreach (NPC segment in Main.ActiveNPCs)
                {
                    if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI
                        || segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal != emitterOrdinal)
                    {
                        continue;
                    }

                    Vector2 normal = segment.rotation.ToRotationVector2();
                    int side = Vector2.Dot(normal, fireDirection) >= 0f ? 1 : -1;
                    Vector2 aim = Vector2.Lerp(fireDirection, normal * side, 0.55f);
                    SpawnHullBeam(segment, DestroyerHullLaser.OriginalPassPort, side, HullTell, aim.SafeNormalize(fireDirection));
                    break;
                }
            }

            if (timer < duration)
            {
                return;
            }

            if (curtain && sequence == 0 && player != null)
            {
                sequence = 1;
                heading = -heading;
                anchor = player.Center;
                float returnAngle = heading > 0 ? 0f : MathHelper.Pi;
                angle = returnAngle + MathHelper.ToRadians(Main.rand.NextFloat(-32f, 32f));
                Vector2 returnDirection = angle.ToRotationVector2();
                Vector2 returnNormal = returnDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                goal = anchor - returnDirection * 420f - returnNormal * 280f;
                SetBeat(npc, Beat.Setup, 130);
            }
            else
            {
                FinishAttack(npc);
            }
        }

        private void RunPass(NPC npc, Player player, bool authority)
        {
            bool curtain = move == Move.CurtainCall2;
            int stride = curtain ? CurtainPortStride : BroadsidePortStride;
            float speedLimit = curtain ? CurtainPassSpeed : 9.5f;
            // The base exit and duration fit four-segment spacing. Carry the wider rear ports across the same lane.
            int extraSegments = (stride - DriveThroughBaselineStride) * (DriveThroughPortCount - 1);
            float extraTravel = extraSegments * HullSegmentSpacing * npc.scale;
            if (curtain)
            {
                extraTravel += CurtainRunoutDistance;
            }

            if (beat == Beat.Setup)
            {
                if (authority && player != null && timer % 24 == 0 && !player.WithinRange(anchor, 120f))
                {
                    anchor = player.Center;
                    SetPassLane();
                    npc.netUpdate = true;
                }

                Vector2 toEntry = goal - npc.Center;
                float distance = toEntry.Length();
                float approachSpeed = Math.Min(distance * 0.08f, 18f);
                Vector2 entryVelocity = toEntry.SafeNormalize(Vector2.Zero) * approachSpeed;
                GlideHead(npc, entryVelocity, 0.18f);

                if (authority && (distance <= 28f || timer >= duration))
                {
                    driveThroughResolvedPorts = 0;
                    driveThroughLastShot = -RippleDelay;
                    driveThroughFadeUntil = 0;
                    curtainReachedBoundary = false;
                    curtainTurning = false;
                    int extraTicks = (int)MathF.Ceiling(extraTravel / speedLimit);
                    SetBeat(npc, Beat.Active, DriveThroughTicks + extraTicks);
                }

                return;
            }

            // The drive-through passes carry the whole firing section across the arena before they turn.
            Vector2 passDirection = angle.ToRotationVector2();
            Vector2 passNormal = new Vector2(-passDirection.Y, passDirection.X);
            Vector2 laneCenter = anchor - Vector2.UnitY * (sweepSign * CurrentPassHeight);
            Vector2 fromLaneCenter = npc.Center - laneCenter;
            float alongLane = Vector2.Dot(fromLaneCenter, passDirection);
            float offLane = Vector2.Dot(fromLaneCenter, passNormal);
            float steerAngle = Math.Clamp(-offLane * 0.005f, -0.68f, 0.68f);
            float exitDistance = DriveThroughExit + extraTravel;

            float speed = Math.Clamp((exitDistance - alongLane) * 0.12f, 0.1f, speedLimit);
            Vector2 desiredVelocity = passDirection.RotatedBy(steerAngle) * speed;
            if (curtain && curtainTurning)
            {
                int turnSide = heading * sweepSign;
                Vector2 returnDirection = passDirection.RotatedBy(MathHelper.PiOver2 * turnSide);
                desiredVelocity = returnDirection * speedLimit;
            }

            if (timer <= 28)
            {
                GlideHead(npc, desiredVelocity, 0.14f);
            }
            else
            {
                SteerHead(npc, desiredVelocity, 0.016f, 0.14f);
            }

            if (!authority)
            {
                return;
            }

            bool complete = timer >= duration;
            int fireTicks = curtain ? DestroyerHullLaser.CurtainRunFireTicks : DestroyerHullLaser.FireTicks;
            int beamLifetime = PortTell + HullTell + fireTicks + DestroyerHullLaser.FadeTicks + 1;
            bool canSchedule = timer + beamLifetime <= duration;
            if (player != null && timer >= 28 && canSchedule && !curtainTurning)
            {
                EmitDriveThroughPair(npc, player, passDirection, speedLimit);
            }

            int allPorts = (1 << DriveThroughPortCount) - 1;
            bool portsResolved = driveThroughResolvedPorts == allPorts;
            if (curtain)
            {
                if (!curtainReachedBoundary && alongLane >= 420f)
                {
                    bool headInSolid = Collision.SolidCollision(npc.Center - new Vector2(8f), 16, 16);
                    bool playerReachedWall = false;
                    if (player != null)
                    {
                        Vector2 ahead = player.Center + Vector2.UnitX * (heading * CurtainBoundaryProbe);
                        playerReachedWall = !Collision.CanHitLine(player.Center, 1, 1, ahead, 1, 1);
                    }

                    curtainReachedBoundary = headInSolid || playerReachedWall;
                }

                // Start the bend at the boundary once the firing section is committed. The tail can finish its sweep on the old line.
                if (!curtainTurning && (portsResolved || !canSchedule)
                    && (curtainReachedBoundary || alongLane >= exitDistance - 64f))
                {
                    curtainTurning = true;
                    npc.netUpdate = true;
                }

                complete |= curtainTurning && timer >= driveThroughFadeUntil;
            }
            else
            {
                complete |= portsResolved && timer >= driveThroughFadeUntil;
            }

            if (complete)
            {
                if (curtain && sequence == 0 && player != null)
                {
                    sequence = 1;
                    curtainTurning = false;
                    heading = -heading;
                    sweepSign = -sweepSign;
                    anchor = player.Center;
                    SetPassLane();
                    SetBeat(npc, Beat.Setup, DriveThroughSetupTicks);
                }
                else
                {
                    FinishAttack(npc);
                }
            }
        }

        private void RunRippleBroadside(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                if (authority && player != null && timer % 24 == 0 && !player.WithinRange(anchor, 150f))
                {
                    anchor = player.Center;
                    goal = anchor - new Vector2(heading * RippleEntryDistance, sweepSign * RippleEntryHeight);
                    npc.netUpdate = true;
                }

                Vector2 toEntry = goal - npc.Center;
                float distance = toEntry.Length();
                float speed = Math.Min(distance * 0.08f, 18f);
                GlideHead(npc, toEntry.SafeNormalize(Vector2.Zero) * speed, 0.18f);
                if (authority && distance <= 38f)
                {
                    SetBeat(npc, Beat.Shape, RippleShapeTicks);
                }
                else if (authority && timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            float laneY = anchor.Y - sweepSign * PassLaneHeight;
            float laneCorrection = Math.Clamp((laneY - npc.Center.Y) * 0.04f, -4f, 4f);
            float passSpeed = beat == Beat.Shape ? RippleShapeSpeed : 0f;
            GlideHead(npc, new Vector2(heading * passSpeed, laneCorrection), RippleSteering);
            if (!authority)
            {
                return;
            }

            if (beat == Beat.Shape)
            {
                // Lay the hull across the arena before committing its ports. Select one contiguous run along the actual chain.
                float headLead = (npc.Center.X - anchor.X) * heading;
                if (headLead >= 840f && timer % 4 == 0)
                {
                    Vector2 target = player?.Center ?? anchor;
                    int firstOrdinal = FindRippleRun(npc, target);
                    if (firstOrdinal > 0)
                    {
                        sequence = firstOrdinal;
                        int lastWarning = RippleWaveSpacing + (RippleSegmentCount - 1) * RippleSegmentDelay;
                        int activeTicks = lastWarning + RippleTellTicks + DestroyerHullLaser.RippleFireTicks + DestroyerHullLaser.RippleFadeTicks + 2;
                        SetBeat(npc, Beat.Active, activeTicks);
                        return;
                    }
                }

                if (timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            int waveTick = timer - 1;
            bool secondWave = waveTick >= RippleWaveSpacing;
            if (secondWave)
            {
                waveTick -= RippleWaveSpacing;
            }

            int step = waveTick / RippleSegmentDelay;
            if (step < RippleSegmentCount && waveTick % RippleSegmentDelay == 0)
            {
                int ordinal = sequence + step;
                if (secondWave)
                {
                    ordinal = sequence + RippleSegmentCount - 1 - step;
                }

                foreach (NPC segment in Main.ActiveNPCs)
                {
                    if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI)
                    {
                        continue;
                    }

                    TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                    if (segmentState.spawnOrdinal == ordinal)
                    {
                        int portSide = heading * sweepSign;
                        SpawnHullBeam(segment, DestroyerHullLaser.RipplePort, portSide, RippleTellTicks);
                        break;
                    }
                }
            }

            if (timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void RunOrbit(NPC npc, bool authority)
        {
            bool overhead = move == Move.HullGlowSpin;
            bool hourglass = move == Move.HourglassSpin;
            bool reactor = move == Move.ReactorBreaker;
            bool enclosing = !overhead && !hourglass && !reactor;
            float radius = enclosing ? EnclosingRadius : OverheadRadius;
            float orbitSpeed = overhead ? OverheadOrbitSpeed : EnclosingOrbitSpeed;
            int shapeTicks = overhead ? OverheadShapeTicks : EnclosingShapeTicks;
            if (hourglass)
            {
                orbitSpeed = HourglassOrbitSpeed;
                shapeTicks = HourglassShapeTicks;
            }
            else if (reactor)
            {
                orbitSpeed = ReactorOrbitSpeed;
                shapeTicks = ReactorShapeTicks;
            }

            if (beat == Beat.Setup)
            {
                // Brake on the way to the rim so a tight approach cannot turn into a loop around the entry point.
                Vector2 toEntry = goal - npc.Center;
                float distance = toEntry.Length();
                float entrySpeed = Math.Clamp(distance * 0.09f, 4f, 17f);
                Vector2 entryDirection = toEntry.SafeNormalize(Vector2.UnitX);
                GlideHead(npc, entryDirection * entrySpeed, 0.13f);

                if (authority && distance <= 96f)
                {
                    angle = (npc.Center - anchor).ToRotation();
                    SetBeat(npc, Beat.Shape, shapeTicks);
                }
                else if (authority && timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            if (beat == Beat.Recovery)
            {
                TravelTo(npc, goal, 13f);
                if (authority && timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            int orbitAge = timer;
            if (beat == Beat.Active && !reactor)
            {
                orbitAge += shapeTicks;
            }

            float sweptAngle = orbitSpeed * orbitAge;
            bool secondLap = enclosing && beat == Beat.Active && timer > EnclosingFirstLapTicks;
            if (secondLap)
            {
                // Apply the slower speed only to time spent on lap two so the orbit stays continuous.
                int slowerTicks = timer - EnclosingFirstLapTicks;
                sweptAngle -= (orbitSpeed - EnclosingSecondOrbitSpeed) * slowerTicks;
                orbitSpeed = EnclosingSecondOrbitSpeed;
            }

            float angularStep = sweepSign * orbitSpeed;
            float orbitAngle = angle + sweepSign * sweptAngle;
            Vector2 radial = orbitAngle.ToRotationVector2();
            Vector2 orbitPoint = anchor + radial * radius;
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2 * sweepSign);
            Vector2 correction = (orbitPoint - npc.Center) * 0.055f;
            Vector2 desiredVelocity = tangent * (radius * orbitSpeed) + correction;
            float desiredSpeed = desiredVelocity.Length();
            if (desiredSpeed > MaxCoilSpeed)
            {
                float speedScale = MaxCoilSpeed / desiredSpeed;
                desiredVelocity *= speedScale;
            }

            SteerHead(npc, desiredVelocity, 0.085f, 0.14f);
            if (hourglass && !Main.dedServ)
            {
                EmitHourglassFire(npc, npc.velocity);
            }

            if (!authority)
            {
                return;
            }

            if (beat == Beat.Shape)
            {
                if (timer >= duration)
                {
                    int firingTicks = overhead ? OverheadActiveTicks : EnclosingActiveTicks;
                    if (hourglass)
                    {
                        firingTicks = HourglassActiveTicks;
                    }
                    else if (reactor)
                    {
                        firingTicks = ReactorActiveTicks;
                        angle += angularStep * duration;
                    }

                    SetBeat(npc, Beat.Active, firingTicks);
                }

                return;
            }

            int beamAge = timer - orbitBeamStart;
            int reactorLifetime = ReactorTellTicks + ReactorFireTicks + DestroyerHullLaser.FadeTicks;
            bool reactorPassFinished = reactor && orbitBeamStart > 0 && beamAge > reactorLifetime;
            if (reactorPassFinished && sequence < ReactorWaves)
            {
                // Reverse only after the live beam and its fade have finished. The hull follows the head through the turn.
                angle = (npc.Center - anchor).ToRotation();
                sweepSign = -sweepSign;
                orbitBeamStart = 0;
                SetBeat(npc, Beat.Shape, ReactorReversalTicks);
                return;
            }

            if (timer >= duration || (overhead && sequence == 2 && beamAge >= OrbitBeamLifetime)
                || (hourglass && sequence == HourglassWaves && beamAge >= HourglassBeamLifetime)
                || reactorPassFinished)
            {
                Vector2 exitDirection = new Vector2(heading, 0.45f);
                goal = npc.Center + exitDirection * 480f;
                vulnerable = true;
                SetBeat(npc, Beat.Recovery, 65);
                return;
            }

            if (hourglass)
            {
                RunHourglassVolley(npc, angularStep, beamAge);
                return;
            }

            if (reactor)
            {
                if (orbitBeamStart > 0 || sequence >= ReactorWaves || timer + reactorLifetime >= duration)
                {
                    return;
                }

                // The center pass fires inward from the opposite arc. The outside passes start at mirrored bearings.
                bool outward = sequence != 1;
                int side = outward ? -sweepSign : sweepSign;
                float offset = 0f;
                if (outward)
                {
                    offset = (sequence == 0 ? heading : -heading) * 0.25f;
                }

                float lead = angularStep * (ReactorTellTicks + ReactorFireTicks * 0.5f - 1f);
                float tellAngle = MathHelper.PiOver2 + offset - lead;
                NPC emitter = FindOrbitEmitter(npc, radius, angularStep, tellAngle, side, outward);
                if (emitter != null)
                {
                    int beam = SpawnHullBeam(emitter, DestroyerHullLaser.ReactorPort, side, ReactorTellTicks);
                    if (beam < Main.maxProjectiles)
                    {
                        sequence++;
                        orbitBeamStart = timer;
                        orbitBeamOrdinal = emitter.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                        npc.netUpdate = true;
                    }
                }

                return;
            }

            if (overhead)
            {
                if (sequence >= 2 || timer + OrbitBeamLifetime > duration || sequence > 0 && beamAge < OrbitBeamSpacing)
                {
                    return;
                }

                // Sweep the underside from the opening flank to the opposite one.
                float lean = sequence == 0 ? -0.24f : 0.31f;
                float fireMiddle = MathHelper.PiOver2 + sweepSign * lean;
                float leadTicks = OrbitTellTicks + OrbitFireTicks * 0.5f - 2f;
                float tellAngle = fireMiddle - angularStep * leadTicks;
                int side = -sweepSign;
                NPC emitter = FindOrbitEmitter(npc, radius, angularStep, tellAngle, side, true);
                if (emitter != null)
                {
                    int projectileIndex = SpawnHullBeam(emitter, DestroyerHullLaser.OverheadPort, side, OrbitTellTicks);
                    if (projectileIndex < Main.maxProjectiles)
                    {
                        sequence++;
                        orbitBeamOrdinal = emitter.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                        orbitBeamStart = timer;
                        npc.netUpdate = true;
                    }
                }

                return;
            }

            bool firstLap = !secondLap;
            int lap = firstLap ? 0 : 1;
            int lapEnd = firstLap ? EnclosingFirstLapTicks : EnclosingActiveTicks;
            int tellTicks = firstLap ? EnclosingFirstTellTicks : OrbitTellTicks;
            int fireTicks = firstLap ? EnclosingFirstFireTicks : EnclosingSecondFireTicks;
            int beamLifetime = tellTicks + fireTicks + DestroyerHullLaser.FadeTicks + DestroyerHullLaser.PoseWaitTicks;
            if (sequence > lap || timer < EnclosingFirstVolleyDelay || timer + beamLifetime > lapEnd)
            {
                return;
            }

            float[] volleyAngles = firstLap ? enclosingFirstVolleyAngles : enclosingSecondVolleyAngles;
            float windupAngle = angularStep * tellTicks;
            NPC[] emitters = new NPC[volleyAngles.Length];
            for (int i = 0; i < emitters.Length; i++)
            {
                float tellAngle = volleyAngles[i] - windupAngle;
                NPC emitter = FindOrbitEmitter(npc, radius, angularStep, tellAngle, sweepSign, false, emitters);
                if (emitter == null)
                {
                    // A port across the ring can sweep the same axis when the near side is still closing.
                    emitter = FindOrbitEmitter(npc, radius, angularStep, tellAngle + MathHelper.Pi, sweepSign, false, emitters);
                    if (emitter == null)
                    {
                        return;
                    }
                }

                emitters[i] = emitter;
            }

            float portMode = firstLap ? DestroyerHullLaser.EnclosingPort : DestroyerHullLaser.ExtendedEnclosingPort;
            if (!firstLap && npc.life * 2 <= npc.lifeMax)
            {
                portMode += DestroyerHullLaser.OverchargeFlag;
            }

            if (TrySpawnOrbitVolley(emitters, portMode, sweepSign, tellTicks))
            {
                sequence = lap + 1;
                npc.netUpdate = true;
            }
        }

        private void RunHourglassVolley(NPC npc, float angularStep, int beamAge)
        {
            if (sequence >= HourglassWaves || timer + HourglassBeamLifetime > duration
                || (sequence > 0 && beamAge < HourglassWaveSpacing))
            {
                return;
            }

            // Cant each nozzle once so the waist falls below the coil. The saved cant turns with its segment.
            int bank = sequence % 2 == 0 ? heading : -heading;
            float middleAngle = bank > 0 ? 0.77f : MathHelper.Pi - 0.77f;
            float lead = angularStep * (HourglassTellTicks + HourglassFireTicks * 0.5f - 1f);
            Vector2 waist = anchor + Vector2.UnitY * HourglassHeight;
            if (npc.HasValidTarget)
            {
                Vector2 drift = Main.player[npc.target].Center - waist;
                waist += Vector2.Clamp(drift, new Vector2(-96f), new Vector2(96f));
            }

            NPC[] emitters = new NPC[HourglassPortsPerWave];
            Span<Vector2> aims = stackalloc Vector2[HourglassPortsPerWave];
            for (int i = 0; i < emitters.Length; i++)
            {
                float fanOffset = (i - (HourglassPortsPerWave - 1) * 0.5f) * 0.12f;
                float tellAngle = middleAngle + fanOffset - lead;
                NPC emitter = FindOrbitEmitter(npc, OverheadRadius, angularStep, tellAngle, sweepSign, false, emitters);
                if (emitter == null)
                {
                    return;
                }

                emitters[i] = emitter;
                Vector2 portDirection = emitter.rotation.ToRotationVector2() * sweepSign;
                Vector2 muzzle = emitter.Center + portDirection * (HullPortOffset * emitter.scale);
                Vector2 futureMuzzle = anchor + (muzzle - anchor).RotatedBy(lead);
                Vector2 futureAim = (waist - futureMuzzle).SafeNormalize(portDirection.RotatedBy(lead));
                aims[i] = futureAim.RotatedBy(-lead);
            }

            if (TrySpawnOrbitVolley(emitters, DestroyerHullLaser.HourglassPort, sweepSign, HourglassTellTicks, aims))
            {
                sequence++;
                orbitBeamStart = timer;
                npc.netUpdate = true;
            }
        }

        private static bool TrySpawnOrbitVolley(NPC[] emitters, float portMode, int side, int tellTicks,
            ReadOnlySpan<Vector2> aims = default)
        {
            Span<int> spawnedBeams = stackalloc int[emitters.Length];
            for (int i = 0; i < emitters.Length; i++)
            {
                Vector2? aim = null;
                if (!aims.IsEmpty)
                {
                    aim = aims[i];
                }

                int projectileIndex = SpawnHullBeam(emitters[i], portMode, side, tellTicks, aim);
                if (projectileIndex >= Main.maxProjectiles)
                {
                    for (int j = 0; j < i; j++)
                    {
                        Main.projectile[spawnedBeams[j]].Kill();
                    }

                    return false;
                }

                spawnedBeams[i] = projectileIndex;
            }

            foreach (int projectileIndex in spawnedBeams)
            {
                if (Main.projectile[projectileIndex].ModProjectile is DestroyerHullLaser laser)
                {
                    laser.JoinVolley(emitters);
                }
            }

            return true;
        }

        private NPC FindOrbitEmitter(NPC head, float radius, float angularStep, float tellAngle, int side, bool overhead, NPC[] selectedEmitters = null)
        {
            NPC emitter = null;
            float bestScore = float.MaxValue;
            float angleTolerance = move == Move.HourglassSpin ? 0.27f : 0.20f;
            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI)
                {
                    continue;
                }

                TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                int ordinal = segmentState.spawnOrdinal;
                float turnError = Math.Abs(segmentState.hullTurn - angularStep);
                if (ordinal < 4 || (overhead && ordinal == orbitBeamOrdinal) || turnError > 0.006f)
                {
                    continue;
                }

                if (selectedEmitters != null && Array.IndexOf(selectedEmitters, segment) >= 0)
                {
                    continue;
                }

                Vector2 fromAnchor = segment.Center - anchor;
                float distanceToAnchor = fromAnchor.Length();
                float innerAllowance = overhead ? 96f : 155f;
                if (distanceToAnchor < radius - innerAllowance || distanceToAnchor > radius + 96f)
                {
                    continue;
                }

                Vector2 portDirection = segment.rotation.ToRotationVector2() * side;
                Vector2 radialDirection = fromAnchor / distanceToAnchor;
                float radialAlignment = Vector2.Dot(radialDirection, portDirection);
                float expectedAlignment = overhead ? radialAlignment : -radialAlignment;
                float angleError = Math.Abs(MathHelper.WrapAngle(portDirection.ToRotation() - tellAngle));
                if (expectedAlignment < 0.95f || angleError >= angleTolerance)
                {
                    continue;
                }

                float score = angleError;
                if (!overhead)
                {
                    // Keep the emitter inside the chamber when turns overlap by favoring the inner turn.
                    score += (distanceToAnchor - radius) * 0.0025f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    emitter = segment;
                }
            }

            return emitter;
        }

        private static int SpawnHullBeam(NPC emitter, float portMode, int side, int timingTicks, Vector2? initialAim = null)
        {
            Vector2 direction = emitter.rotation.ToRotationVector2() * side;
            Vector2 muzzle = emitter.Center + direction * (HullPortOffset * emitter.scale);
            if (portMode == DestroyerHullLaser.HourglassPort && initialAim.HasValue)
            {
                // The integer picks the socket, and the fractional part sends its fixed cant to clients.
                float cant = MathHelper.WrapAngle(initialAim.Value.ToRotation() - direction.ToRotation());
                portMode += 0.5f + MathHelper.Clamp(cant, -1.5f, 1.5f) / MathHelper.TwoPi;
            }

            if (side < 0)
            {
                portMode++;
            }

            Vector2 beamDirection = initialAim ?? direction;
            return Projectile.NewProjectile(emitter.GetSource_FromAI(), muzzle, beamDirection,
                ModContent.ProjectileType<DestroyerHullLaser>(), EnemyDamage.Projectile(LaserDamage), 0f,
                Main.myPlayer, timingTicks, emitter.whoAmI, portMode);
        }

        private static bool TrySpawnHullPair(NPC emitter, int portMode, int timingTicks)
        {
            int first = SpawnHullBeam(emitter, portMode, 1, timingTicks);
            if (first >= Main.maxProjectiles)
            {
                return false;
            }

            int second = SpawnHullBeam(emitter, portMode, -1, timingTicks);
            if (second >= Main.maxProjectiles)
            {
                Main.projectile[first].Kill();
                return false;
            }

            return true;
        }

        private void RunCharge(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= duration))
                {
                    SetBeat(npc, Beat.Windup, ChargeAimTicks);
                }

                return;
            }

            if (beat == Beat.Windup)
            {
                Vector2 destination = anchor;
                if (player != null)
                {
                    destination = player.Center;
                }

                Vector2 toTarget = destination - npc.Center;
                Vector2 aim = toTarget.SafeNormalize(lockedDirection);
                float distance = toTarget.Length();
                float aimSpeed = Math.Clamp((distance - 370f) * 0.045f, 3.5f, 6.5f);
                SteerHead(npc, aim * aimSpeed, 0.075f, 0.14f);
                DecorateMouth(npc, DustID.Torch, SoundID.ForceRoar);
                if (authority && !ramBackedOff && distance < ChargeMinimumLineupDistance)
                {
                    SetRamBackoff(npc, destination);
                    return;
                }

                if (!authority || timer < duration)
                {
                    return;
                }

                Vector2 headForward = npc.velocity.SafeNormalize(aim);
                if (Vector2.Dot(headForward, aim) < 0.997f && timer < duration + 42)
                {
                    return;
                }

                lockedDirection = aim;
                lockedOrigin = destination;
                float approachDistance = Math.Max(0f, distance - RamBrakeDistance);
                int approachTicks = Math.Max(75, (int)MathF.Ceiling(approachDistance / RamApproachSpeed) + 60);
                SetBeat(npc, Beat.RamLineup, approachTicks);
                return;
            }

            if (beat == Beat.RamLineup)
            {
                Vector2 destination = player?.Center ?? lockedOrigin;
                Vector2 toTarget = destination - npc.Center;
                float distanceToTarget = toTarget.Length();
                Vector2 aim = toTarget.SafeNormalize(lockedDirection);
                Vector2 approachDirection = npc.velocity.SafeNormalize(aim);
                float alignment = Vector2.Dot(approachDirection, aim);
                bool canBrake = player != null && distanceToTarget <= RamBrakeDistance;

                if (!authority || !canBrake)
                {
                    float approach = MathHelper.Clamp((alignment + 0.15f) / 0.9f, 0f, 1f);
                    float speed = MathHelper.Lerp(RamTellSpeed, RamApproachSpeed, approach);
                    SteerHead(npc, aim * speed, 0.12f, 0.23f);
                    if (authority && timer >= duration)
                    {
                        FinishAttack(npc);
                    }

                    return;
                }

                GlideHead(npc, aim * RamTrackingSpeed, 0.32f);
                SetBeat(npc, Beat.Shape, RamTrackingTimeout);
                return;
            }

            if (beat == Beat.Shape)
            {
                Vector2 destination = player?.Center ?? lockedOrigin;
                Vector2 toTarget = destination - npc.Center;
                float distance = toTarget.Length();
                Vector2 aim = toTarget.SafeNormalize(lockedDirection);
                float retreatSpeed = 0f;
                if (player != null)
                {
                    retreatSpeed = Math.Clamp(Vector2.Dot(player.velocity, aim), 0f, 9.5f);
                }

                // Brake early and keep aiming. Slow to a crawl at the edge of the commitment radius if the head needs to turn.
                float remaining = Math.Max(0f, distance - RamAimDistance);
                float closingSpeed = Math.Clamp(remaining * 0.4f, 0.75f, RamTrackingSpeed);
                // Match the outward part of the player's motion separately so the distance taper cannot stall outside four tiles.
                float catchup = retreatSpeed * MathHelper.Clamp(timer / 16f, 0f, 1f);
                float trackingSpeed = Math.Min(closingSpeed + catchup, RamApproachSpeed - 1f);
                GlideHead(npc, aim * trackingSpeed, 0.36f);
                Vector2 forward = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                float alignment = Vector2.Dot(forward, aim);
                if (!authority)
                {
                    return;
                }

                if (timer == 1)
                {
                    bool canWarnNeck = Math.Abs(aim.X) >= RamSideAlignment && alignment >= 0.988f;
                    foreach (NPC segment in Main.ActiveNPCs)
                    {
                        if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI)
                        {
                            continue;
                        }

                        TheDestroyer neckState = segment.GetGlobalNPC<TheDestroyer>();
                        int ordinal = neckState.spawnOrdinal;
                        if (sequence == 0 && ordinal >= 1 && ordinal <= RamShortPairCount)
                        {
                            SpawnHullBeam(segment, DestroyerHullLaser.ShortPairPort, 1, DestroyerHullLaser.ShortPairTellTicks);
                        }

                        if (canWarnNeck && ordinal == RamNeckOrdinal
                            && neckState.CanSweepPairedPorts(segment, DestroyerHullLaser.RamLength))
                        {
                            TrySpawnHullPair(segment, DestroyerHullLaser.RamPort, 0);
                        }
                    }
                }

                if (player != null && distance <= RamAimDistance && alignment >= 0.985f)
                {
                    lockedDirection = aim;
                    lockedOrigin = destination;
                    npc.velocity = lockedDirection * RamTellSpeed;
                    // Budget the distance lost to the 30-tick ramp so the head still clears the target.
                    float rampTravelLost = (RamSpeed - RamTellSpeed) * (RamAccelerationTicks - 1) * 0.5f;
                    float dashDistance = distance + ChargeOvershoot + rampTravelLost;
                    ramLiveTicks = Math.Clamp((int)MathF.Ceiling(dashDistance / RamSpeed), RamAccelerationTicks, 70);
                    SetBeat(npc, Beat.Active, ramLiveTicks);
                }
                else if (timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                float progress = MathHelper.Clamp(timer / (float)RamAccelerationTicks, 0f, 1f);
                float speed = MathHelper.SmoothStep(RamTellSpeed, RamSpeed, progress);
                SteerHead(npc, lockedDirection * speed, 0.016f, 1f);
                if (authority && timer >= duration)
                {
                    if (++sequence < chainLength)
                    {
                        ramBackedOff = false;
                        SetBeat(npc, Beat.Windup, ChargeAimTicks);
                    }
                    else
                    {
                        vulnerable = true;
                        SetBeat(npc, Beat.Recovery, 65 + 15 * (chainLength - 1));
                    }
                }

                return;
            }

            Vector2 recoveryDrift = lockedDirection * MathHelper.Lerp(16f, 9f, MathHelper.Clamp(timer / (float)Math.Max(1, duration), 0f, 1f));
            SteerHead(npc, recoveryDrift, 0.04f, 0.10f);
            if (!Main.dedServ && timer % 4 == 0)
            {
                Dust.NewDustPerfect(Mouth(npc), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), Scale: 1.8f);
            }

            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void SetRamBackoff(NPC npc, Vector2 destination)
        {
            ramBackedOff = true;
            anchor = destination;
            Vector2 outward = (npc.Center - destination).SafeNormalize(-lockedDirection);
            goal = destination + outward * ChargeStandOff;
            SetBeat(npc, Beat.Setup, 140);
        }

        private void RunBombDrop(NPC npc, Player player, bool authority)
        {
            bool dropMines = move == Move.MineDrop;
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= duration))
                {
                    if (player != null)
                    {
                        anchor = player.Center;
                        if (player.Center.X >= npc.Center.X)
                        {
                            heading = 1;
                        }
                        else
                        {
                            heading = -1;
                        }
                    }

                    SetBeat(npc, Beat.Shape, 55);
                }

                return;
            }

            float targetY = anchor.Y - 240f;
            if (player != null)
            {
                targetY = player.Center.Y - 240f;
            }

            float verticalCorrection = MathHelper.Clamp((targetY - npc.Center.Y) * 0.06f, -6f, 6f);
            if (beat == Beat.Shape)
            {
                Vector2 shapeVelocity = new Vector2(heading * 14.5f, verticalCorrection);
                SteerHead(npc, shapeVelocity, 0.06f, 0.12f);
                if (authority && timer >= duration)
                {
                    int passTicks = 95;
                    if (!dropMines)
                    {
                        passTicks += BombBayFlashFadeTicks;
                    }

                    SetBeat(npc, Beat.Active, passTicks);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                Vector2 activeVelocity = new Vector2(heading * 13.5f, verticalCorrection);
                SteerHead(npc, activeVelocity, 0.055f, 0.10f);
                if (authority && player != null && timer % 4 == 0)
                {
                    foreach (NPC segment in Main.ActiveNPCs)
                    {
                        if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI || segment.localAI[1] != 0f)
                        {
                            continue;
                        }

                        TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                        int ordinal = segmentState.spawnOrdinal;
                        if (ordinal <= 0 || ordinal % 5 != 0)
                        {
                            continue;
                        }

                        if (!dropMines)
                        {
                            if (segmentState.bombBayTellTick >= 0 || timer + BombBayTellTicks + BombBayFlashFadeTicks > duration)
                            {
                                continue;
                            }

                            // Start the tell on approach so the moving bay is still over the lane at release.
                            Vector2 travel = segmentState.hullTravel;
                            Vector2 minimumTravel = new Vector2(-18f, -8f);
                            Vector2 maximumTravel = new Vector2(18f, 8f);
                            travel = Vector2.Clamp(travel, minimumTravel, maximumTravel);
                            Vector2 expectedPosition = segment.Center + travel * BombBayTellTicks;
                            bool overLane = expectedPosition.Y < player.Center.Y - 24f
                                && Math.Abs(expectedPosition.X - player.Center.X) <= 640f;
                            Vector2 normal = segment.rotation.ToRotationVector2();
                            if (segment.Center.Y < player.Center.Y - 24f && overLane && Math.Abs(normal.Y) >= 0.35f)
                            {
                                segmentState.bombBaySide = normal.Y >= 0f ? 1 : -1;
                                segmentState.bombBayTellTick = timer;
                                segment.netUpdate = true;
                            }

                            continue;
                        }

                        // MineDrop uses 4 fewer eligible fifth-segments (15..70 instead of 5..80) and caps at 8 dropped mines.
                        if (ordinal < 15 || ordinal > 70 || sequence >= 8)
                        {
                            continue;
                        }

                        bool isAbovePlayer = segment.Center.Y < player.Center.Y - 24f
                            && Math.Abs(segment.Center.X - player.Center.X) <= 640f;
                        if (isAbovePlayer)
                        {
                            Vector2 dropPosition = segment.Center + new Vector2(0f, 18f);
                            if (Collision.SolidCollision(dropPosition - new Vector2(12f, 12f), 24, 24))
                            {
                                continue;
                            }

                            Vector2 dropVelocity = new Vector2(npc.velocity.X * 0.12f, 2.5f);
                            int projectileIndex = Projectile.NewProjectile(segment.GetSource_FromAI(), dropPosition, dropVelocity,
                                ModContent.ProjectileType<ProximityMine>(), EnemyDamage.Projectile(BombDamage), 2f,
                                Main.myPlayer, 0f, 0f, npc.whoAmI + 1);

                            if (projectileIndex < Main.maxProjectiles)
                            {
                                segment.localAI[1] = 1f;
                                segmentState.payloadReleaseTick = timer;
                                segment.netUpdate = true;
                                sequence++;
                                if (sequence == 8)
                                {
                                    npc.netUpdate = true;
                                }

                                if (!Main.dedServ)
                                {
                                    SoundEngine.PlaySound(SoundID.Item1, dropPosition);
                                }
                            }
                        }
                    }
                }

                if (authority && timer >= duration)
                {
                    if (dropMines)
                    {
                        // 120-tick recovery after the proximity mine drop.
                        vulnerable = true;
                        SetBeat(npc, Beat.Recovery, 120);
                    }
                    else
                    {
                        FinishAttack(npc);
                    }
                }

                return;
            }

            Vector2 recoveryVelocity = new Vector2(heading * 10.5f, verticalCorrection * 0.5f);
            SteerHead(npc, recoveryVelocity, 0.045f, 0.08f);
            if (!Main.dedServ && timer % 4 == 0)
            {
                Dust.NewDustPerfect(Mouth(npc), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), Scale: 1.7f);
            }

            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void RunMissileBarrage(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 16f);
                if (authority && (arrived || timer >= 95))
                {
                    int barrageDuration = (MissileSegmentCount - 1) * MissileSegmentInterval + MissileTelegraphTicks + 30;
                    SetBeat(npc, Beat.Active, barrageDuration);
                }

                return;
            }

            if (player != null)
            {
                anchor = Vector2.Lerp(anchor, player.Center, 0.04f);
            }

            // Weave in an open figure-8 orbit around the player without firing any lasers while the 8 missile segments telegraph and launch.
            float orbitPhase = timer * 0.022f;
            Vector2 orbitOffset = new Vector2(MathF.Cos(orbitPhase) * 440f * heading, MathF.Sin(orbitPhase * 1.5f) * 210f - 90f);
            Vector2 targetPoint = anchor + orbitOffset;
            Vector2 desiredVelocity = (targetPoint - npc.Center).SafeNormalize(Vector2.UnitX * heading) * 12.5f;
            SteerHead(npc, desiredVelocity, 0.052f, 0.10f);

            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private static void RunMissileBay(NPC segment, NPC head, int age, bool authority)
        {
            Vector2 portNormal = segment.rotation.ToRotationVector2();
            Vector2 aim = -Vector2.UnitY;
            float targetPlayerArg = 0f;
            bool launch = age == MissileTelegraphTicks;
            if (authority && launch && head.HasValidTarget)
            {
                Player player = Main.player[head.target];
                aim = (player.Center - segment.Center).SafeNormalize(aim);
                targetPlayerArg = player.whoAmI + 1f;
            }

            if (!Main.dedServ)
            {
                if (age == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.45f, Pitch = -0.35f }, segment.Center);
                }
                else if (launch)
                {
                    SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.75f, Pitch = -0.1f }, segment.Center);
                }
            }

            float charge = MathHelper.Clamp(age / (float)MissileTelegraphTicks, 0f, 1f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 outward = portNormal * side;
                Vector2 nozzle = segment.Center + outward * (HullPortOffset * segment.scale);
                if (authority && launch)
                {
                    Vector2 launchHeading = (aim + outward * 0.45f).SafeNormalize(outward);
                    Projectile.NewProjectile(segment.GetSource_FromAI(), nozzle, launchHeading * ProximityMineRocket.Speed,
                        ModContent.ProjectileType<ProximityMineRocket>(), EnemyDamage.Projectile(MissileDamage),
                        2f, Main.myPlayer, 0f, targetPlayerArg, head.whoAmI + 1);
                }

                if (Main.dedServ)
                {
                    continue;
                }

                float brightness = MathHelper.Lerp(0.16f, 0.82f, charge);
                if (age > MissileTelegraphTicks)
                {
                    brightness *= 1f - (age - MissileTelegraphTicks) / (float)MissileHotTicks;
                }

                Lighting.AddLight(nozzle, brightness, brightness * 0.42f, brightness * 0.16f);
                Lighting.AddLight(nozzle + outward * 34f, brightness * 0.34f, brightness * 0.16f, brightness * 0.07f);
                if (age < 10 || age > MissileTelegraphTicks)
                {
                    continue;
                }

                int dustCount = charge < 0.5f ? 2 : 3;
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 position = nozzle + outward * Main.rand.NextFloat(3f, 14f);
                    float speed = Main.rand.NextFloat(7f, 13f) * MathHelper.Lerp(0.65f, 1.1f, charge);
                    Vector2 velocity = outward * speed + Main.rand.NextVector2Circular(1.3f, 1.3f);
                    Dust exhaust = Dust.NewDustPerfect(position, DustID.Smoke, velocity, 75,
                        new Color(176, 180, 184), Main.rand.NextFloat(0.95f, 1.4f));
                    exhaust.noGravity = true;
                    exhaust.noLight = true;
                    exhaust.fadeIn = 1.45f;
                }

                if (launch || age % 12 == 0)
                {
                    int cloudCount = launch || charge >= 0.65f ? 2 : 1;
                    for (int i = 0; i < cloudCount; i++)
                    {
                        Vector2 position = nozzle + outward * Main.rand.NextFloat(15f, 26f) - new Vector2(12f);
                        Vector2 velocity = outward * Main.rand.NextFloat(2.5f, 5f) + Main.rand.NextVector2Circular(1.1f, 1.1f);
                        Gore.NewGore(segment.GetSource_FromAI(), position, velocity, Main.rand.Next(61, 64), Main.rand.NextFloat(0.48f, 0.72f));
                    }
                }
            }
        }

        private void RunMaw(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                if (player != null)
                {
                    anchor = player.Center;
                    Vector2 fromPlayer = npc.Center - anchor;
                    Vector2 perchDirection = fromPlayer.SafeNormalize(-Vector2.UnitX * heading);
                    float desiredPerchDistance = MathHelper.Clamp(fromPlayer.Length(), 380f, 560f);
                    goal = anchor + perchDirection * desiredPerchDistance;
                }

                float distanceToPlayer = Vector2.Distance(npc.Center, anchor);
                bool inStanceRange = distanceToPlayer >= 320f && distanceToPlayer <= 640f && timer >= 16;
                bool arrived = TravelTo(npc, goal, 15f);
                if (authority && (inStanceRange || arrived || timer >= 75))
                {
                    goal = npc.Center;
                    bodyPoseForMaw = true;
                    if (player != null)
                    {
                        lockedDirection = (player.Center - npc.Center).SafeNormalize(lockedDirection);
                    }

                    // 44-tick serpentine S-bend that curves only the neck near the head while leaving the body in its open stance.
                    SetBeat(npc, Beat.Shape, 44);
                }

                return;
            }

            Vector2 facing = lockedDirection;
            if (beat == Beat.Shape || beat == Beat.Windup && timer < duration - MawLock)
            {
                if (player != null)
                {
                    facing = (player.Center - npc.Center).SafeNormalize(lockedDirection);
                }
            }

            if (beat == Beat.Shape)
            {
                float progress = MathHelper.Clamp(timer / (float)Math.Max(1, duration), 0f, 1f);
                // Weave a single sinusoidal S-bend with the head over ~6 segment lengths so the neck bends like a snake without coiling the body.
                float sBendAngle = MathF.Sin(progress * MathHelper.TwoPi) * 0.72f * heading;
                float slitherSpeed = MathHelper.Lerp(11.5f, 2.5f, progress);
                Vector2 slitherVelocity = facing.RotatedBy(sBendAngle) * slitherSpeed;
                SteerHead(npc, slitherVelocity, 0.11f, 0.22f);
                if (authority && timer >= duration)
                {
                    if (player != null)
                    {
                        lockedDirection = (player.Center - npc.Center).SafeNormalize(facing);
                    }
                    else
                    {
                        lockedDirection = facing;
                    }

                    sweepSign = heading;
                    if (player != null)
                    {
                        Vector2 acrossAim = lockedDirection.RotatedBy(MathHelper.PiOver2);
                        float crossing = Vector2.Dot(player.velocity, acrossAim);
                        if (Math.Abs(crossing) > 0.5f)
                        {
                            sweepSign = Math.Sign(crossing);
                        }
                    }

                    int windupTicks = 75;
                    if (phase == 3)
                    {
                        windupTicks = 55;
                    }

                    SetBeat(npc, Beat.Windup, windupTicks);
                }

                return;
            }

            if (beat == Beat.Windup)
            {
                if (timer <= duration - MawLock)
                {
                    float centerAngle = lockedDirection.ToRotation();
                    if (player != null)
                    {
                        Vector2 toPlayer = player.Center - npc.Center;
                        centerAngle = toPlayer.SafeNormalize(lockedDirection).ToRotation();
                        Vector2 predicted = toPlayer + player.velocity * 12f;
                        float predictedAngle = predicted.SafeNormalize(lockedDirection).ToRotation();
                        centerAngle = centerAngle.AngleTowards(predictedAngle, 0.18f);
                    }

                    // Telegraph from the edge of the arc so the target starts inside the sweep.
                    float openingAngle = centerAngle - sweepSign * DestroyerMawBeam.SweepArc * 0.5f;
                    facing = openingAngle.ToRotationVector2();
                }

                if (authority && timer == duration - MawLock)
                {
                    lockedDirection = facing;
                    lockedOrigin = npc.Center + lockedDirection * (28f * npc.scale);
                    npc.netUpdate = true;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), lockedOrigin, lockedDirection,
                        ModContent.ProjectileType<DestroyerMawBeam>(), EnemyDamage.Projectile(MawDamage), 0f,
                        Main.myPlayer, npc.whoAmI, lockedDirection.ToRotation(), sweepSign);
                }

                // Hold the head in place so the body and S-bent neck stay in their open stance without coiling.
                npc.velocity *= 0.78f;
                if (timer >= duration - MawLock)
                {
                    facing = lockedDirection;
                    npc.rotation = lockedDirection.ToRotation() + MathHelper.PiOver2;
                }
                else
                {
                    float currentFacing = npc.rotation - MathHelper.PiOver2;
                    float nextFacing = currentFacing.AngleTowards(facing.ToRotation(), 0.08f);
                    npc.rotation = nextFacing + MathHelper.PiOver2;
                }

                DecorateMouth(npc, DustID.SolarFlare, SoundID.Item15);
                if (!Main.dedServ && timer >= duration - MawLock && timer % 3 == 0)
                {
                    Vector2 sparkPosition = Mouth(npc) + facing * 24f;
                    Vector2 sparkVelocity = facing.RotatedBy(sweepSign * MathHelper.PiOver2) * 5f;
                    Dust.NewDustPerfect(sparkPosition, DustID.Torch, sparkVelocity, Scale: 2.1f).noGravity = true;
                }

                if (authority && timer >= duration)
                {
                    SetBeat(npc, Beat.Active, DestroyerMawBeam.LiveTicks);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                float beamAngle = DestroyerMawBeam.SweepAngle(lockedDirection.ToRotation(), sweepSign, timer - 1);
                // Pivot the jaws in place at the end of the S-curved neck so the body stays in its open stance.
                npc.velocity = Vector2.Zero;
                npc.rotation = beamAngle + MathHelper.PiOver2;
                if (authority && timer >= duration)
                {
                    vulnerable = true;
                    SetBeat(npc, Beat.Recovery, 90);
                }

                return;
            }

            Vector2 postMawDrift = (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * MathHelper.Lerp(3f, 11f, MathHelper.Clamp(timer / 90f, 0f, 1f));
            SteerHead(npc, postMawDrift, 0.045f, 0.10f);
            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void RunRunawayMaw(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                Vector2 toPerch = goal - npc.Center;
                float distance = toPerch.Length();
                float speed = Math.Clamp(distance * 0.08f, 2f, 16f);
                GlideHead(npc, toPerch.SafeNormalize(-Vector2.UnitX * heading) * speed, 0.14f);
                if (authority && distance <= 60f)
                {
                    Vector2 target = player?.Center ?? anchor;
                    float distanceToTarget = npc.Distance(target);
                    if (distanceToTarget < 275f || distanceToTarget > 770f
                        || !Collision.CanHitLine(npc.Center, 1, 1, target, 1, 1))
                    {
                        FinishAttack(npc);
                        return;
                    }

                    lockedDirection = (target - npc.Center).SafeNormalize(Vector2.UnitX * heading);
                    sweepSign = Main.rand.NextBool() ? 1 : -1;
                    SetBeat(npc, Beat.Windup, RunawayWindupTicks);
                }
                else if (authority && timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            if (beat == Beat.Windup)
            {
                npc.velocity *= 0.71f;
                float firstAngle = lockedDirection.ToRotation() + MathHelper.ToRadians(runawaySweeps[0].StartDegrees * sweepSign);
                npc.rotation = npc.rotation.AngleTowards(firstAngle + MathHelper.PiOver2, 0.13f);
                DecorateMouth(npc, DustID.SolarFlare, SoundID.Item15);
                if (authority && timer == duration - MawLock)
                {
                    Vector2 facing = firstAngle.ToRotationVector2();
                    Projectile.NewProjectile(npc.GetSource_FromAI(), Mouth(npc), facing,
                        ModContent.ProjectileType<DestroyerMawBeam>(), EnemyDamage.Projectile(MawDamage), 0f,
                        Main.myPlayer, npc.whoAmI, firstAngle, DestroyerMawBeam.RunawayMode);
                }

                if (authority && timer >= duration)
                {
                    SetBeat(npc, Beat.Active, RunawayFireTicks);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                int elapsed = timer - 1;
                float degrees = runawaySweeps[^1].EndDegrees;
                foreach (var sweep in runawaySweeps)
                {
                    int span = sweep.SwingTicks + sweep.PauseTicks;
                    if (elapsed >= span)
                    {
                        elapsed -= span;
                        continue;
                    }

                    // Integrate a trapezoidal turn speed so the jaw eases onto the arc, settles, and reverses without snapping.
                    float travelTicks = Math.Min(elapsed, sweep.SwingTicks);
                    float fullTravel = sweep.SwingTicks - RunawayRampTicks;
                    float turnTravel = travelTicks - RunawayRampTicks * 0.5f;
                    if (travelTicks < RunawayRampTicks)
                    {
                        turnTravel = travelTicks * travelTicks / (2f * RunawayRampTicks);
                    }
                    else if (travelTicks > sweep.SwingTicks - RunawayRampTicks)
                    {
                        float remaining = sweep.SwingTicks - travelTicks;
                        turnTravel = fullTravel - remaining * remaining / (2f * RunawayRampTicks);
                    }

                    degrees = MathHelper.Lerp(sweep.StartDegrees, sweep.EndDegrees, turnTravel / fullTravel);
                    break;
                }

                float facing = lockedDirection.ToRotation() + MathHelper.ToRadians(degrees * sweepSign);
                npc.velocity = Vector2.Zero;
                npc.rotation = facing + MathHelper.PiOver2;
                if (authority && timer >= duration)
                {
                    vulnerable = true;
                    SetBeat(npc, Beat.Recovery, 78);
                }

                return;
            }

            // Hold the exit pose while the beam gutters out at the mouth, then let the head drift away.
            if (timer > 14)
            {
                Vector2 facing = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                SteerHead(npc, facing * 11f, 0.045f, 0.08f);
            }

            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void RunHullThrash(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                Vector2 toPerch = goal - npc.Center;
                float distance = toPerch.Length();
                float speed = Math.Clamp(distance * 0.09f, 2f, 18f);
                GlideHead(npc, toPerch.SafeNormalize(-Vector2.UnitX * heading) * speed, 0.18f);
                if (authority && distance <= 42f)
                {
                    Vector2 target = player?.Center ?? anchor;
                    Vector2 targetOffset = target - npc.Center;
                    float sideDistance = Math.Abs(targetOffset.X);
                    bool inSweepRange = sideDistance >= 300f && Math.Abs(targetOffset.Y) <= sideDistance * 0.70f
                        && targetOffset.LengthSquared() <= 780f * 780f;
                    if (!inSweepRange || !Collision.CanHitLine(npc.Center, 1, 1, target, 1, 1))
                    {
                        FinishAttack(npc);
                        return;
                    }

                    anchor = target;
                    heading = target.X >= npc.Center.X ? 1 : -1;
                    SetBeat(npc, Beat.Windup, ThrashTellTicks);
                }
                else if (authority && timer >= duration)
                {
                    FinishAttack(npc);
                }

                return;
            }

            if (beat == Beat.Windup)
            {
                npc.velocity *= 0.70f;
                float aim = (anchor - npc.Center).ToRotation();
                float openingAngle = aim + MathHelper.ToRadians(ThrashOpeningDegrees * heading);
                npc.rotation = npc.rotation.AngleTowards(openingAngle + MathHelper.PiOver2, 0.13f);
                DecorateMouth(npc, DustID.SolarFlare, SoundID.ForceRoar);
                if (authority && timer == duration - DestroyerMawBeam.TellTicks)
                {
                    Vector2 facing = (npc.rotation - MathHelper.PiOver2).ToRotationVector2();
                    Projectile.NewProjectile(npc.GetSource_FromAI(), Mouth(npc), facing,
                        ModContent.ProjectileType<DestroyerMawBeam>(), EnemyDamage.Projectile(MawDamage), 0f,
                        Main.myPlayer, npc.whoAmI, 0f, DestroyerMawBeam.ThrashMode);
                }

                if (authority && timer >= duration)
                {
                    lockedOrigin = npc.Center;
                    SetBeat(npc, Beat.Active, ThrashFireTicks);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                // The head lunges and recoils along three curved strokes. Each endpoint brings the turn and travel to rest.
                int elapsed = timer;
                Vector2 startOffset = Vector2.Zero;
                Vector2 offset = thrashSweeps[^1].Offset;
                float startAim = ThrashOpeningDegrees;
                float aimOffset = thrashSweeps[^1].AimDegrees;
                int stroke = 0;
                foreach (var sweep in thrashSweeps)
                {
                    if (elapsed <= sweep.Ticks)
                    {
                        float progress = MathHelper.Clamp(elapsed / (float)sweep.Ticks, 0f, 1f);
                        float eased = progress * progress * (3f - 2f * progress);
                        float bow = MathF.Sin(progress * MathHelper.Pi);
                        float bowDirection = stroke == 1 ? -1f : 1f;
                        offset = Vector2.Lerp(startOffset, sweep.Offset, eased);
                        offset.X += bowDirection * 42f * bow * bow;
                        aimOffset = MathHelper.Lerp(startAim, sweep.AimDegrees, eased);
                        break;
                    }

                    elapsed -= sweep.Ticks;
                    startOffset = sweep.Offset;
                    startAim = sweep.AimDegrees;
                    stroke++;
                }

                offset.X *= heading;
                Vector2 pathCenter = lockedOrigin + offset;
                Vector2 travel = pathCenter - npc.Center;
                if (travel.LengthSquared() > 10f * 10f)
                {
                    travel = travel.SafeNormalize(Vector2.Zero) * 10f;
                }

                npc.velocity = Collision.TileCollision(npc.position, travel, npc.width, npc.height);
                // Aim from the resolved head position so walls can shorten the whip without jerking the beam.
                Vector2 aimOrigin = npc.Center + npc.velocity;
                float centerAngle = (anchor - aimOrigin).ToRotation();
                float shotAngle = centerAngle + MathHelper.ToRadians(aimOffset * heading);
                npc.rotation = npc.rotation.AngleTowards(shotAngle + MathHelper.PiOver2, 0.037f);
                if (authority && timer >= duration)
                {
                    npc.velocity = Vector2.Zero;
                    if (TryFindThrashLanding(npc.Center, heading, out Vector2 landing))
                    {
                        goal = landing;
                        lockedOrigin = npc.Center;
                        SetBeat(npc, Beat.Shape, ThrashSettleTicks + ThrashDiveTicks);
                    }
                    else
                    {
                        vulnerable = true;
                        SetBeat(npc, Beat.Recovery, 86);
                    }
                }

                return;
            }

            if (beat == Beat.Shape)
            {
                if (timer <= ThrashSettleTicks)
                {
                    npc.velocity = Vector2.Zero;
                    return;
                }

                // Let the beam fade before the head pitches down into the floor. Stop at the first surface swept by the hitbox.
                int diveAge = timer - ThrashSettleTicks;
                float progress = MathHelper.Clamp(diveAge / (float)ThrashDiveTicks, 0f, 1f);
                float headBottom = npc.height * 0.5f;
                Vector2 impactCenter = goal - Vector2.UnitY * headBottom;
                float centerX = MathHelper.SmoothStep(lockedOrigin.X, impactCenter.X, progress);
                float centerY = MathHelper.Lerp(lockedOrigin.Y, impactCenter.Y, progress * progress);
                Vector2 travel = new Vector2(centerX, centerY) - npc.Center;
                npc.velocity = Collision.TileCollision(npc.position, travel, npc.width, npc.height);
                if (npc.velocity.LengthSquared() > 0.1f)
                {
                    npc.rotation = npc.rotation.AngleTowards(npc.velocity.ToRotation() + MathHelper.PiOver2, 0.26f);
                }

                if (authority)
                {
                    bool touchedFloor = npc.velocity.Y < travel.Y - 0.08f;
                    bool finishedDive = timer >= duration;
                    if (finishedDive && !touchedFloor)
                    {
                        Vector2 floorProbe = Collision.TileCollision(npc.position + npc.velocity, Vector2.UnitY * 3f, npc.width, npc.height);
                        touchedFloor = floorProbe.Y < 2.9f;
                    }

                    if (touchedFloor)
                    {
                        goal = npc.Center + npc.velocity + Vector2.UnitY * headBottom;
                        sequence = 1;
                    }

                    if (touchedFloor || finishedDive || Math.Abs(npc.velocity.X - travel.X) > 1f)
                    {
                        vulnerable = true;
                        SetBeat(npc, Beat.Recovery, 86);
                    }
                }

                return;
            }

            if (sequence > 0 && timer <= ThrashImpactTicks)
            {
                npc.velocity = Vector2.Zero;
                if (!Main.dedServ && !announcedBeat)
                {
                    announcedBeat = true;
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.85f, Pitch = -0.35f }, goal);
                    UsefulFunctions.ScreenShake(goal, 12f, 22, distanceFalloff: 960f);
                    for (int i = 0; i < 34; i++)
                    {
                        Vector2 origin = goal + Main.rand.NextVector2Circular(28f, 5f) - Vector2.UnitY * 5f;
                        Vector2 drift = new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-6.8f, -1.5f));
                        Dust.NewDustPerfect(origin, DustID.Stone, drift, Scale: Main.rand.NextFloat(1f, 1.8f));

                        Color soil = new Color(148, 119, 95);
                        Dust cloud = Dust.NewDustPerfect(origin, DustID.Smoke, drift * 0.57f, 65, soil, Main.rand.NextFloat(1.4f, 2.4f));
                        cloud.noGravity = true;
                    }
                }

                return;
            }

            if (timer <= ThrashSettleTicks)
            {
                npc.velocity = Vector2.Zero;
                return;
            }

            Vector2 exit = new Vector2(-heading, -0.85f).SafeNormalize(-Vector2.UnitY);
            SteerHead(npc, exit * 12f, 0.085f, 0.11f);
            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private void RunProbeBarrage(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= 95))
                {
                    SetBeat(npc, Beat.Windup, 75);
                }

                return;
            }

            if (beat == Beat.Windup)
            {
                Vector2 destination = anchor;
                if (player != null && timer < duration - DashReaim)
                {
                    destination = player.Center;
                }

                Vector2 aim = (destination - npc.Center).SafeNormalize(lockedDirection);
                if (timer < duration - DashReaim)
                {
                    SteerHead(npc, aim * 8.5f, 0.075f, 0.12f);
                }
                else
                {
                    if (authority && timer == duration - DashReaim)
                    {
                        if (player != null)
                        {
                            destination = player.Center;
                        }

                        lockedDirection = (destination - npc.Center).SafeNormalize(aim);
                        lockedOrigin = destination;
                        npc.netUpdate = true;
                    }

                    SteerHead(npc, lockedDirection * 7.5f, 0.05f, 0.12f);
                }

                DecorateMouth(npc, DustID.Torch, SoundID.ForceRoar);
                if (authority && timer >= duration)
                {
                    // 180 ticks = 6 coordinated probe shots spaced 30 ticks apart while the head performs pure ram passes.
                    SetBeat(npc, Beat.Active, 180);
                }

                return;
            }

            if (beat == Beat.Active)
            {
                // The main Destroyer chills on hull lasers and performs two head-charge passes while the probes fire their 6-shot barrage.
                if (timer <= 55)
                {
                    SteerHead(npc, lockedDirection * 24f, 0.035f, 0.22f);
                }
                else if (timer <= 95)
                {
                    Vector2 reaimTarget = anchor;
                    if (player != null)
                    {
                        reaimTarget = player.Center;
                    }

                    Vector2 reaimDirection = (reaimTarget - npc.Center).SafeNormalize(lockedDirection);
                    SteerHead(npc, reaimDirection * 11.5f, 0.075f, 0.14f);
                    if (authority && timer == 95)
                    {
                        lockedDirection = reaimDirection;
                        npc.netUpdate = true;
                    }

                    DecorateMouth(npc, DustID.Torch, SoundID.ForceRoar);
                }
                else if (timer <= 150)
                {
                    SteerHead(npc, lockedDirection * 24f, 0.035f, 0.22f);
                }
                else
                {
                    SteerHead(npc, lockedDirection * 13f, 0.045f, 0.10f);
                }

                if (authority && timer >= duration)
                {
                    vulnerable = true;
                    SetBeat(npc, Beat.Recovery, 60);
                }

                return;
            }

            Vector2 recoveryDrift = lockedDirection * MathHelper.Lerp(14f, 9f, MathHelper.Clamp(timer / (float)Math.Max(1, duration), 0f, 1f));
            SteerHead(npc, recoveryDrift, 0.04f, 0.10f);
            if (!Main.dedServ && timer % 4 == 0)
            {
                Dust.NewDustPerfect(Mouth(npc), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), Scale: 1.8f);
            }

            if (authority && timer >= duration)
            {
                FinishAttack(npc);
            }
        }

        private int ChargeAimTicks
        {
            get
            {
                return phase switch
                {
                    1 => 23,
                    2 => 18,
                    _ => 14
                };
            }
        }

        private int HullTell => 60 - phase * 10;
        private int FirstPassStride => phase == 1 ? 7 : 6;

        private void SetBeat(NPC npc, Beat nextBeat, int ticks)
        {
            beat = nextBeat;
            duration = ticks;
            timer = 0;
            announcedBeat = false;
            npc.netUpdate = true;
        }

        private void FinishAttack(NPC npc)
        {
            move = Move.None;
            vulnerable = false;
            bodyPoseForMaw = false;
            int prowl = Main.rand.Next(65, 105);
            if (phase == 3)
            {
                prowl = Main.rand.Next(55, 85);
            }

            SetBeat(npc, Beat.Prowl, prowl);
        }

        private static void SteerHead(NPC npc, Vector2 desiredVelocity, float maxTurnRadians, float acceleration)
        {
            float desiredSpeed = desiredVelocity.Length();
            if (desiredSpeed < 0.1f)
            {
                desiredSpeed = 0.1f;
            }

            float currentSpeed = npc.velocity.Length();
            float currentAngle = npc.rotation - MathHelper.PiOver2;
            if (currentSpeed > 0.1f)
            {
                currentAngle = npc.velocity.ToRotation();
            }

            float targetAngle = desiredVelocity.SafeNormalize(currentAngle.ToRotationVector2()).ToRotation();
            float nextAngle = currentAngle.AngleTowards(targetAngle, maxTurnRadians);
            float nextSpeed = MathHelper.Lerp(currentSpeed, desiredSpeed, acceleration);
            npc.velocity = nextAngle.ToRotationVector2() * nextSpeed;
            npc.rotation = nextAngle + MathHelper.PiOver2;
        }

        private static void GlideHead(NPC npc, Vector2 desiredVelocity, float acceleration)
        {
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, acceleration);
            if (npc.velocity.LengthSquared() > 0.5f)
            {
                float travelRotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.rotation = npc.rotation.AngleTowards(travelRotation, 0.16f);
            }
        }

        private static bool TravelTo(NPC npc, Vector2 point, float speed)
        {
            Vector2 displacement = point - npc.Center;
            float distance = displacement.Length();
            float targetSpeed = Math.Clamp(distance * 0.08f, 9f, speed);
            Vector2 desiredVelocity = displacement.SafeNormalize((npc.rotation - MathHelper.PiOver2).ToRotationVector2()) * targetSpeed;
            SteerHead(npc, desiredVelocity, 0.06f, 0.12f);
            return distance <= 96f;
        }

        private static Vector2 FindSurface(Vector2 near)
        {
            int tileX = Math.Clamp((int)(near.X / 16f), 10, Main.maxTilesX - 11);
            int centerY = Math.Clamp((int)(near.Y / 16f), 30, Main.maxTilesY - 31);
            for (int offset = 0; offset <= 24; offset++)
            {
                int tileY = centerY + offset;
                if (WorldGen.SolidTile(tileX, tileY) && !WorldGen.SolidTile(tileX, tileY - 1))
                {
                    return new Vector2(tileX * 16f + 8f, tileY * 16f);
                }
            }

            return new Vector2(tileX * 16f + 8f, centerY * 16f + 32f);
        }

        private static bool TryFindThrashLanding(Vector2 headCenter, int side, out Vector2 landing)
        {
            const int width = 38;
            const int height = 24;
            Vector2 probe = headCenter + new Vector2(side * 154f - width * 0.5f, -12f);
            landing = Vector2.Zero;
            if (Collision.SolidCollision(probe, width, height))
            {
                return false;
            }

            Vector2 step = Vector2.UnitY * 12f;
            for (int i = 0; i < 27; i++)
            {
                Vector2 travel = Collision.TileCollision(probe, step, width, height);
                if (travel.Y < step.Y - 0.1f)
                {
                    landing = probe + new Vector2(width * 0.5f, height + travel.Y);
                    Vector2 aboveFloor = landing - Vector2.UnitY * 36f;
                    return Collision.CanHitLine(headCenter, 1, 1, aboveFloor, 1, 1);
                }

                probe += step;
            }

            return false;
        }

        private static bool HasMawPerch(Vector2 target, int side)
        {
            Vector2 perch = target + new Vector2(-side * MawPerchOffset, -MawPerchHeight);
            return !Collision.SolidCollision(perch - new Vector2(24f), 48, 48)
                && Collision.CanHitLine(target, 1, 1, perch, 1, 1);
        }

        private static void EmitHourglassFire(NPC segment, Vector2 headTravel)
        {
            if (!segment.WithinRange(Main.LocalPlayer.Center, 1440f))
            {
                return;
            }

            // The original spin vents size-3 torch flames 48 px out along the head's normal across the entire hull.
            Vector2 flameDrift = new Vector2(-headTravel.Y, headTravel.X);
            Vector2 flameOffset = flameDrift.SafeNormalize(Vector2.UnitY) * 48f;
            Dust flame = Dust.NewDustPerfect(segment.Center + flameOffset, DustID.Torch, flameDrift / 6f, Scale: 3f);
            flame.noGravity = true;
        }

        private static void DecorateTerrainTravel(NPC head)
        {
            Vector2 center = head.Center;
            Vector2 previousCenter = head.oldPosition + head.Size * 0.5f;
            Vector2 travel = center - previousCenter;
            float distance = travel.Length();
            if (distance < 1f || distance > 64f)
            {
                return;
            }

            // Follow the head's core in half-tile steps so a ram through a one-tile wall still kicks up dirt.
            const int probeSize = 14;
            Vector2 probeOffset = new Vector2(probeSize * 0.5f);
            Vector2 direction = travel / distance;
            int sampleCount = (int)MathF.Ceiling(distance / 8f);
            Vector2 previousSample = previousCenter;
            bool wasBuried = Collision.SolidCollision(previousSample - probeOffset, probeSize, probeSize);
            bool touchedSolid = wasBuried;
            Vector2 lastBuriedSample = previousSample;
            for (int i = 1; i <= sampleCount; i++)
            {
                Vector2 sample = Vector2.Lerp(previousCenter, center, i / (float)sampleCount);
                bool buried = Collision.SolidCollision(sample - probeOffset, probeSize, probeSize);
                if (buried)
                {
                    touchedSolid = true;
                    lastBuriedSample = sample;
                }
                else if (wasBuried)
                {
                    // Throw the debris into the open side of the crossing, along the actual heading.
                    Vector2 burstOrigin = Vector2.Lerp(previousSample, sample, 0.5f) + direction * 4f;
                    for (int j = 0; j < 20; j++)
                    {
                        Vector2 position = burstOrigin + Main.rand.NextVector2Circular(9f, 9f);
                        Vector2 sprayDirection = direction.RotatedBy(Main.rand.NextFloat(-1.15f, 1.15f));
                        Vector2 velocity = sprayDirection * Main.rand.NextFloat(3f, 11f);
                        float scale = Main.rand.NextFloat(0.8f, 1.55f);
                        Dust.NewDustPerfect(position, DustID.Dirt, velocity, Scale: scale);
                    }

                    Color soilColor = new Color(137, 99, 68);
                    for (int j = 0; j < 8; j++)
                    {
                        Vector2 position = burstOrigin + Main.rand.NextVector2Circular(12f, 12f);
                        Vector2 velocity = direction * Main.rand.NextFloat(1.5f, 4f);
                        velocity += Main.rand.NextVector2Circular(1.8f, 1.8f);
                        Dust puff = Dust.NewDustPerfect(position, DustID.Smoke, velocity, 105, soilColor, 0.9f);
                        puff.noGravity = true;
                    }
                }

                previousSample = sample;
                wasBuried = buried;
            }

            if (touchedSolid && Main.GameUpdateCount % 3 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = lastBuriedSample + Main.rand.NextVector2Circular(15f, 15f);
                    Vector2 velocity = -direction * Main.rand.NextFloat(1f, 3.5f);
                    velocity += Main.rand.NextVector2Circular(2f, 2f);
                    Dust.NewDustPerfect(position, DustID.Dirt, velocity, Scale: 1.1f);
                }
            }
        }

        private void DecorateMouth(NPC npc, int dustType, SoundStyle sound)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 mouth = Mouth(npc);
            if (!announcedBeat)
            {
                announcedBeat = true;
                SoundEngine.PlaySound(sound, mouth);
            }

            Vector2 offset = Main.rand.NextVector2CircularEdge(44f, 44f);
            Dust.NewDustPerfect(mouth + offset, dustType, -offset / 10f, Scale: 1.6f).noGravity = true;
        }

        internal static Vector2 Mouth(NPC head)
        {
            Vector2 facing = (head.rotation - MathHelper.PiOver2).ToRotationVector2();
            return head.Center + facing * (28f * head.scale);
        }

        internal static bool CanFireHullPort(NPC segment)
        {
            // The follower moves through position corrections. NPC.velocity is zero on moving body pieces.
            Vector2 travel = segment.GetGlobalNPC<TheDestroyer>().hullTravel;
            return Math.Abs(travel.X) >= Math.Abs(travel.Y) * 0.30f;
        }

        private void EmitDriveThroughPair(NPC head, Player player, Vector2 direction, float speed)
        {
            if (timer - driveThroughLastShot < RippleDelay)
            {
                return;
            }

            int maximumTell = PortTell + HullTell;
            bool curtain = move == Move.CurtainCall2;
            int stride = curtain ? CurtainPortStride : BroadsidePortStride;
            int fireTicks = curtain ? DestroyerHullLaser.CurtainRunFireTicks : DestroyerHullLaser.FireTicks;
            int portMode = curtain ? DestroyerHullLaser.CurtainRunPort : DestroyerHullLaser.DriveThroughPort;
            // Start the tell ahead of the crossing so the live part straddles the target's lane.
            float halfLive = fireTicks * 0.5f;
            float leadDistance = speed * (maximumTell + halfLive);
            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI)
                {
                    continue;
                }

                TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                int offset = segmentState.spawnOrdinal - DriveThroughFirstPort;
                if (offset < 0 || offset >= DriveThroughPortCount * stride || offset % stride != 0)
                {
                    continue;
                }

                int portBit = 1 << (offset / stride);
                if ((driveThroughResolvedPorts & portBit) != 0)
                {
                    continue;
                }

                Vector2 toPlayer = player.Center - segment.Center;
                float altitude = toPlayer.Y * sweepSign;
                Vector2 normal = segment.rotation.ToRotationVector2();
                Vector2 forward = new Vector2(normal.Y, -normal.X);
                float ahead = Vector2.Dot(toPlayer, forward);
                float travel = Vector2.Dot(segmentState.hullTravel, forward);
                float alignment = Vector2.Dot(forward, direction);
                float remainingLane = Vector2.Dot(toPlayer, direction);
                if (remainingLane < -180f && alignment > 0.9f && travel > 0f)
                {
                    // Resolve missed ports too so the head can leave once the volley has swept past.
                    driveThroughResolvedPorts |= portBit;
                    continue;
                }

                if (altitude < 70f || altitude > 820f || toPlayer.LengthSquared() > 1150f * 1150f
                    || ahead > leadDistance || ahead < 148f || travel < speed * 0.7f
                    || alignment < 0.94f
                    || !segmentState.CanSweepPairedPorts(segment, DestroyerHullLaser.PassLength))
                {
                    continue;
                }

                int tellTicks = Math.Clamp((int)MathF.Round(ahead / speed - halfLive), DriveThroughMinTell, maximumTell);
                if (TrySpawnHullPair(segment, portMode, tellTicks))
                {
                    driveThroughResolvedPorts |= portBit;
                    driveThroughLastShot = timer;
                    int fadeUntil = timer + tellTicks + fireTicks + DestroyerHullLaser.FadeTicks + 1;
                    driveThroughFadeUntil = Math.Max(driveThroughFadeUntil, fadeUntil);
                    if (curtain && (curtainFiredPorts & portBit) == 0)
                    {
                        int previousPorts = curtainFiredPorts;
                        curtainFiredPorts |= portBit;
                        head.netUpdate = true;
                        if (previousPorts != 0)
                        {
                            // Open only newly bracketed gaps. Skipped and reserved long ports never get short pairs.
                            int first = DriveThroughFirstPort + System.Numerics.BitOperations.TrailingZeroCount((uint)curtainFiredPorts) * stride;
                            int last = DriveThroughFirstPort + System.Numerics.BitOperations.Log2((uint)curtainFiredPorts) * stride;
                            int oldFirst = DriveThroughFirstPort + System.Numerics.BitOperations.TrailingZeroCount((uint)previousPorts) * stride;
                            int oldLast = DriveThroughFirstPort + System.Numerics.BitOperations.Log2((uint)previousPorts) * stride;
                            foreach (NPC gapSegment in Main.ActiveNPCs)
                            {
                                if (gapSegment.type != NPCID.TheDestroyerBody || gapSegment.realLife != head.whoAmI)
                                {
                                    continue;
                                }

                                int ordinal = gapSegment.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                                bool newGap = ordinal > first && ordinal < last && (ordinal < oldFirst || ordinal > oldLast);
                                if (newGap && (ordinal - DriveThroughFirstPort) % stride != 0)
                                {
                                    SpawnHullBeam(gapSegment, DestroyerHullLaser.CurtainShortPairPort, 1, 0);
                                }
                            }
                        }
                    }

                    break;
                }
            }
        }

        private bool CanSweepPairedPorts(NPC segment, float length)
        {
            Vector2 normal = segment.rotation.ToRotationVector2();
            Vector2 previousCenter = segment.Center - hullTravel;
            Vector2 previousNormal = normal.RotatedBy(-hullTurn);
            float reach = length + HullPortOffset * segment.scale;
            return !DestroyerHullLaser.ExceedsSweepSpeed(previousCenter, previousNormal, segment.Center, normal, reach)
                && !DestroyerHullLaser.ExceedsSweepSpeed(previousCenter, -previousNormal, segment.Center, -normal, reach);
        }

        private int FindRippleRun(NPC head, Vector2 target)
        {
            Span<int> ports = stackalloc int[Main.maxNPCs];
            ports.Fill(-1);
            Span<float> hitColumns = stackalloc float[Main.maxNPCs];
            Vector2 direction = Vector2.UnitX * heading;
            float brakeDrift = head.velocity.X * (1f - RippleSteering) / RippleSteering;
            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI)
                {
                    continue;
                }

                TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                Vector2 offset = segment.Center - target;
                float gap = -offset.Y * sweepSign;
                Vector2 forward = (segment.rotation - MathHelper.PiOver2).ToRotationVector2();
                if (segmentState.spawnOrdinal <= 0 || Math.Abs(offset.X) > 1760f || gap < 40f || gap > 665f
                    || Vector2.Dot(forward, direction) < 0.81f
                    || !segmentState.CanSweepPairedPorts(segment, DestroyerHullLaser.RippleLength))
                {
                    continue;
                }

                // Score the row by where its real normals meet player height once the head brakes.
                Vector2 normal = new Vector2(-forward.Y, forward.X);
                float projectedX = offset.X - offset.Y * normal.X / normal.Y;
                ports[segmentState.spawnOrdinal] = segment.whoAmI;
                hitColumns[segmentState.spawnOrdinal] = Math.Abs(projectedX + brakeDrift);
            }

            int remaining = 0;
            int bestOrdinal = 0;
            float bestError = 1150f;
            for (int ordinal = 1; ordinal < ports.Length; ordinal++)
            {
                if (ports[ordinal] < 0)
                {
                    remaining = 0;
                    continue;
                }

                remaining++;
                if (remaining >= RippleSegmentCount)
                {
                    int first = ordinal - RippleSegmentCount + 1;
                    float front = Main.npc[ports[first]].Center.X;
                    float rear = Main.npc[ports[ordinal]].Center.X;
                    float center = (front + rear) * 0.5f;
                    float closestRay = float.MaxValue;
                    for (int index = first; index <= ordinal; index++)
                    {
                        closestRay = Math.Min(closestRay, hitColumns[index]);
                    }

                    if (closestRay > 120f)
                    {
                        continue;
                    }

                    float error = Math.Abs(center - target.X) + closestRay * 0.65f;
                    if (error < bestError)
                    {
                        bestError = error;
                        bestOrdinal = first;
                    }
                }
            }

            return bestOrdinal;
        }

        private Vector2 BombBayPosition(NPC segment)
        {
            Vector2 axis = segment.rotation.ToRotationVector2();
            float offset = HullPortOffset * segment.scale * bombBaySide;
            return segment.Center + axis * offset;
        }

        private float BombBayFlash(TheDestroyer headState)
        {
            if (bombBayTellTick < 0 || headState.move != Move.BombDrop || headState.beat != Beat.Active)
            {
                return 0f;
            }

            if (payloadReleaseTick >= 0)
            {
                int elapsed = headState.timer - payloadReleaseTick;
                float fade = MathHelper.Clamp(1f - elapsed / (float)BombBayFlashFadeTicks, 0f, 1f);
                return fade * fade;
            }

            int chargeAge = headState.timer - bombBayTellTick;
            float charge = MathHelper.Clamp(chargeAge / (float)BombBayTellTicks, 0f, 1f);
            return 0.56f * charge * charge;
        }

        private int MissileBayAge(int ordinal)
        {
            int offset = ordinal - MissileFirstOrdinal;
            if (offset < 0 || offset >= MissileSegmentCount * MissileSegmentStride || offset % MissileSegmentStride != 0)
            {
                return -1;
            }

            int missileStep = offset / MissileSegmentStride;
            return timer - missileStep * MissileSegmentInterval;
        }

        private static HullHeat ShotHeat(int age, int tellTicks, int liveTicks, out float armingProgress)
        {
            armingProgress = 0f;
            if (age <= 0)
            {
                return HullHeat.Cold;
            }

            if (age < HullArmingTicks)
            {
                armingProgress = age / (float)HullArmingTicks;
                return HullHeat.Arming;
            }

            if (age < tellTicks)
            {
                return HullHeat.Warm;
            }

            int coolingAge = age - tellTicks - liveTicks;
            if (coolingAge < 0)
            {
                return HullHeat.Hot;
            }

            return coolingAge < DestroyerHullLaser.FadeTicks ? HullHeat.Warm : HullHeat.Cold;
        }

        internal HullHeat UpdateBeamHeat(int age, int tellTicks, int liveTicks)
        {
            HullHeat heat = ShotHeat(age, tellTicks, liveTicks, out float progress);
            ulong tick = Main.GameUpdateCount;
            // The synced projectile supplies the clock. When two beams share a vent, show the hotter one.
            if (beamHeatTick != tick || heat > beamHeat)
            {
                beamHeatTick = tick;
                beamHeat = heat;
                beamArmingProgress = progress;
            }

            return heat;
        }

        private HullHeat GetHullHeat(NPC npc, out float armingProgress)
        {
            armingProgress = 0f;
            if (!TryGetHead(npc, out NPC head))
            {
                return HullHeat.Cold;
            }

            TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
            if (headState.beat == Beat.Retreat)
            {
                return HullHeat.Cold;
            }

            bool hourglass = headState.move == Move.HourglassSpin;
            bool fieryOrbit = hourglass || headState.move == Move.ReactorBreaker;
            bool heatedOrbit = headState.move == Move.EnclosingCoil || fieryOrbit;
            bool thrashing = headState.move == Move.HullThrash;
            bool thrashImpact = thrashing && headState.beat == Beat.Recovery && headState.sequence > 0
                && headState.timer <= ThrashImpactTicks;
            if (heatedOrbit && headState.beat == Beat.Active
                || hourglass && headState.beat == Beat.Shape
                || thrashing && (headState.beat == Beat.Active || headState.beat == Beat.Shape)
                || thrashImpact)
            {
                return HullHeat.Hot;
            }

            if (npc.type == NPCID.TheDestroyer)
            {
                bool probeRam = move == Move.ProbeBarrage && (timer <= 55 || timer > 95 && timer <= 150);
                bool ramming = beat == Beat.Active && (move == Move.HeadCharge || probeRam);
                return ramming ? HullHeat.Hot : HullHeat.Warm;
            }

            if (spawnOrdinal <= 0)
            {
                return HullHeat.Cold;
            }

            HullHeat scheduledHeat = HullHeat.Cold;
            if (heatedOrbit)
            {
                // The overhead spins heat up on the climb. The enclosing coil warns as the ring closes.
                int armingAge = headState.timer - (EnclosingShapeTicks - HullArmingTicks - 32);
                if (fieryOrbit)
                {
                    armingAge = headState.timer;
                    if (headState.beat == Beat.Shape)
                    {
                        armingAge += HullArmingTicks;
                    }
                }

                bool heating = headState.beat == Beat.Shape || fieryOrbit && headState.beat == Beat.Setup;
                if (heating && armingAge > 0)
                {
                    if (armingAge < HullArmingTicks)
                    {
                        armingProgress = armingAge / (float)HullArmingTicks;
                        scheduledHeat = HullHeat.Arming;
                    }
                    else
                    {
                        scheduledHeat = HullHeat.Warm;
                    }
                }
                else if (headState.beat == Beat.Recovery && headState.timer < DestroyerHullLaser.FadeTicks)
                {
                    scheduledHeat = HullHeat.Warm;
                }
            }
            else if (thrashing)
            {
                if (headState.beat == Beat.Setup && headState.sequence == 0 && headState.timer < HullArmingTicks)
                {
                    armingProgress = headState.timer / (float)HullArmingTicks;
                    scheduledHeat = HullHeat.Arming;
                }
                else if (headState.beat == Beat.Setup || headState.beat == Beat.Windup
                    || headState.beat == Beat.Recovery && headState.timer <= 20)
                {
                    scheduledHeat = HullHeat.Warm;
                }
            }
            else if (headState.move == Move.MissileBarrage && headState.beat == Beat.Active)
            {
                int missileAge = headState.MissileBayAge(spawnOrdinal);
                scheduledHeat = ShotHeat(missileAge, MissileTelegraphTicks, MissileHotTicks, out armingProgress);
            }
            else if ((headState.move == Move.BombDrop || headState.move == Move.MineDrop) && spawnOrdinal % 5 == 0)
            {
                bool canDrop = headState.move == Move.BombDrop || spawnOrdinal >= 15 && spawnOrdinal <= 70 && headState.sequence < 8;
                if (headState.beat == Beat.Shape && canDrop)
                {
                    int elapsed = headState.timer;
                    if (elapsed < HullArmingTicks)
                    {
                        armingProgress = elapsed / (float)HullArmingTicks;
                        scheduledHeat = HullHeat.Arming;
                    }
                    else
                    {
                        scheduledHeat = HullHeat.Warm;
                    }
                }
                else if (headState.beat == Beat.Active)
                {
                    if (payloadReleaseTick >= 0)
                    {
                        int payloadAge = headState.timer - payloadReleaseTick;
                        int shotAge = HullArmingTicks + Math.Max(0, payloadAge);
                        scheduledHeat = ShotHeat(shotAge, HullArmingTicks, PayloadHotTicks, out armingProgress);
                    }
                    else if (canDrop)
                    {
                        scheduledHeat = HullHeat.Warm;
                    }
                }
            }
            else if (headState.beat == Beat.Active && (headState.move == Move.BroadsidePass1 || headState.move == Move.CurtainCall1))
            {
                int stride = headState.FirstPassStride;
                if (spawnOrdinal % stride == 0)
                {
                    int stagger = (spawnOrdinal / stride - 1) * RippleDelay;
                    int portAge = headState.timer - stagger;
                    if (portAge > 0 && portAge <= PortTell)
                    {
                        armingProgress = portAge / (float)HullArmingTicks;
                        scheduledHeat = HullHeat.Arming;
                    }
                }
            }

            if (Main.GameUpdateCount - beamHeatTick <= 1 && beamHeat >= scheduledHeat && beamHeat != HullHeat.Cold)
            {
                armingProgress = beamArmingProgress;
                return beamHeat;
            }

            return scheduledHeat;
        }

        internal static bool TryGetHead(NPC segment, out NPC head)
        {
            if (segment.type == NPCID.TheDestroyer)
            {
                head = segment;
                return head.active;
            }

            int index = segment.realLife;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active && Main.npc[index].type == NPCID.TheDestroyer)
            {
                head = Main.npc[index];
                return true;
            }

            head = null;
            return false;
        }

        private static int CountActiveProbes(NPC head)
        {
            int count = 0;
            foreach (NPC existing in Main.ActiveNPCs)
            {
                if (existing.type == NPCID.Probe && existing.life * 4 >= existing.lifeMax)
                {
                    TheDestroyer existingState = existing.GetGlobalNPC<TheDestroyer>();
                    if (existingState.probeOwner == head.whoAmI && existingState.probeKamikazeTimer == 0 && !existingState.repairOnlyProbe)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void ReleaseProbeWave(NPC head)
        {
            bool hasFireBreathProbe = false;
            NPC healer = null;
            foreach (NPC existing in Main.ActiveNPCs)
            {
                if (existing.type != NPCID.Probe)
                {
                    continue;
                }

                TheDestroyer state = existing.GetGlobalNPC<TheDestroyer>();
                if (state.probeOwner == head.whoAmI)
                {
                    hasFireBreathProbe |= state.fireBreathProbe;
                    if (state.repairOnlyProbe)
                    {
                        healer = existing;
                    }
                }
            }

            if (!supportedProbeWave)
            {
                if (healer == null)
                {
                    if (!TryReleaseProbe(head, 7, repairOnly: true, fireBreath: false))
                    {
                        return;
                    }
                }
                else
                {
                    TheDestroyer healerState = healer.GetGlobalNPC<TheDestroyer>();
                    if (healerState.repairSegment < 0)
                    {
                        healerState.repairCooldown = 0;
                    }
                }

                supportedProbeWave = true;
            }

            int spacing = NPC.GetDestroyerSegmentsCount() / (ProbeWaveAttackers + 1);
            while (releasedWaveAttackers < ProbeWaveAttackers)
            {
                int ordinal = spacing * (releasedWaveAttackers + 1) + releasedProbeWaves;
                if (!TryReleaseProbe(head, ordinal, repairOnly: false, fireBreath: !hasFireBreathProbe))
                {
                    return;
                }

                hasFireBreathProbe = true;
                releasedWaveAttackers++;
            }

            releasedProbeWaves++;
            releasedWaveAttackers = 0;
            supportedProbeWave = false;
        }

        private static bool TryReleaseProbe(NPC head, int desiredOrdinal, bool repairOnly, bool fireBreath)
        {
            NPC source = null;
            int bestDistance = int.MaxValue;
            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI || segment.ai[2] != 0f)
                {
                    continue;
                }

                int ordinal = segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                int distance = Math.Abs(ordinal - desiredOrdinal);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    source = segment;
                }
            }

            if (source == null)
            {
                return false;
            }

            int probeIndex = NPC.NewNPC(source.GetSource_FromAI(), (int)source.Center.X, (int)source.Bottom.Y, NPCID.Probe);
            if (probeIndex >= Main.maxNPCs)
            {
                return false;
            }

            source.ai[2] = 1f;
            source.netUpdate = true;
            NPC probe = Main.npc[probeIndex];
            TheDestroyer state = probe.GetGlobalNPC<TheDestroyer>();
            state.fireBreathProbe = fireBreath;
            state.repairOnlyProbe = repairOnly;
            if (repairOnly)
            {
                state.repairSegment = source.whoAmI;
                state.repairStart = 0;
            }
            else
            {
                state.probeOpeningTicks = ProbeWaveOpeningTicks;
                state.probeLaserCooldown = 15;
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, number: probeIndex);
            }

            return true;
        }

        private static void OrientProbe(NPC npc, Vector2 direction)
        {
            Vector2 safeDirection = direction.SafeNormalize(Vector2.UnitX);
            if (safeDirection.X >= 0f)
            {
                npc.direction = 1;
            }
            else
            {
                npc.direction = -1;
            }

            npc.spriteDirection = npc.direction;
            if (npc.spriteDirection == -1)
            {
                npc.rotation = safeDirection.ToRotation() + MathHelper.Pi;
            }
            else
            {
                npc.rotation = safeDirection.ToRotation();
            }
        }

        public override void OnKill(NPC npc)
        {
            if (npc.type == NPCID.Probe && probeOwner >= 0 && !repairOnlyProbe && !probeExploded
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                probeExploded = true;
                Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Vector2.Zero,
                    ModContent.ProjectileType<DestroyerProbeExplosion>(), EnemyDamage.Projectile(ProbeExplosionDamage), 5f,
                    Main.myPlayer);
            }
            else if (npc.type == NPCID.TheDestroyer && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int mineType = ModContent.ProjectileType<ProximityMine>();
                int rocketType = ModContent.ProjectileType<ProximityMineRocket>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active || proj.ai[2] != npc.whoAmI + 1)
                    {
                        continue;
                    }

                    if (proj.type == mineType)
                    {
                        proj.Kill();
                    }
                    else if (proj.type == rocketType && proj.ModProjectile is ProximityMineRocket rocket)
                    {
                        rocket.Disarm();
                    }
                }
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (npc.type != NPCID.Probe || probeOwner < 0 || probeOwner >= Main.maxNPCs)
            {
                return true;
            }

            NPC head = Main.npc[probeOwner];
            TheDestroyer headState = null;
            if (head.active && head.type == NPCID.TheDestroyer)
            {
                headState = head.GetGlobalNPC<TheDestroyer>();
            }

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (npc.target < 0 || npc.target >= Main.maxPlayers
                || !Main.player[npc.target].active || Main.player[npc.target].dead))
            {
                npc.TargetClosest(false);
            }

            Player targetPlayer = null;
            if (npc.target >= 0 && npc.target < Main.maxPlayers && Main.player[npc.target].active && !Main.player[npc.target].dead)
            {
                targetPlayer = Main.player[npc.target];
            }

            if (repairOnlyProbe)
            {
                npc.localAI[0] = 0f;
                if (headState == null || headState.IsRetreating)
                {
                    if (authority)
                    {
                        npc.active = false;
                        npc.netUpdate = true;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                        }
                    }

                    return false;
                }

                if (!RunProbeRepair(npc, head, headState, authority))
                {
                    Vector2 arenaCenter = head.Center;
                    if (head.HasValidTarget)
                    {
                        arenaCenter = Main.player[head.target].Center;
                    }

                    int side = npc.whoAmI % 2 == 0 ? 1 : -1;
                    float bob = MathF.Sin((Main.GameUpdateCount + npc.whoAmI * 11) * 0.032f) * 24f;
                    Vector2 perch = arenaCenter + new Vector2(side * 600f, -130f + bob);
                    Vector2 drift = Vector2.Clamp((perch - npc.Center) * 0.045f, new Vector2(-5.5f), new Vector2(5.5f));
                    npc.velocity = Vector2.Lerp(npc.velocity, drift, 0.09f);
                    OrientProbe(npc, (head.Center - npc.Center).SafeNormalize(Vector2.UnitX));
                    if (!Main.dedServ)
                    {
                        Lighting.AddLight(npc.Center, 0.08f, 0.34f, 0.13f);
                        if (Main.GameUpdateCount % 8 == 0)
                        {
                            Vector2 offset = Main.rand.NextVector2Circular(9f, 9f);
                            Dust spark = Dust.NewDustPerfect(npc.Center + offset, DustID.GreenTorch, -npc.velocity * 0.25f, Scale: 0.9f);
                            spark.noGravity = true;
                        }
                    }
                }

                return false;
            }

            if (authority && probeOpeningTicks > 0)
            {
                probeOpeningTicks--;
            }

            if (authority && probeLaserCooldown > 0)
            {
                probeLaserCooldown--;
            }

            // Below 25% life: stop shooting lasers or healing, lightly home in until 6 tiles away, then fly straight for 60 ticks and explode.
            if (npc.life * 4 < npc.lifeMax || probeKamikazeTimer > 0)
            {
                npc.localAI[0] = 0f;
                probeBreathTimer = 0;
                if (repairSegment >= 0)
                {
                    repairSegment = -1;
                    repairStart = -1;
                }

                if (probeKamikazeTimer == 0)
                {
                    Vector2 desiredDirection = probeAimDirection;
                    if (targetPlayer != null)
                    {
                        desiredDirection = (targetPlayer.Center - npc.Center).SafeNormalize(probeAimDirection);
                    }

                    float currentSpeed = npc.velocity.Length();
                    float currentAngle = desiredDirection.ToRotation();
                    if (currentSpeed > 0.1f)
                    {
                        currentAngle = npc.velocity.ToRotation();
                    }

                    float nextAngle = currentAngle.AngleTowards(desiredDirection.ToRotation(), 0.042f);
                    float nextSpeed = MathHelper.Lerp(currentSpeed, 8.2f, 0.08f);
                    npc.velocity = nextAngle.ToRotationVector2() * nextSpeed;
                    probeAimDirection = nextAngle.ToRotationVector2();
                    OrientProbe(npc, probeAimDirection);

                    if (!Main.dedServ && Main.GameUpdateCount % 3 == 0)
                    {
                        Dust spark = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(12f, 12f),
                            DustID.Torch, -npc.velocity * 0.25f, Scale: 1.4f);
                        spark.noGravity = true;
                    }

                    float lockDistancePixels = ProbeKamikazeLockTiles * 16f;
                    if (authority && targetPlayer != null && Vector2.Distance(npc.Center, targetPlayer.Center) <= lockDistancePixels)
                    {
                        probeAimDirection = npc.velocity.SafeNormalize(desiredDirection);
                        npc.velocity = probeAimDirection * 8.2f;
                        probeKamikazeTimer = 1;
                        npc.netUpdate = true;
                    }
                }
                else
                {
                    // Locked on: travel straight forward along probeAimDirection for 60 ticks, then blow up.
                    npc.velocity = probeAimDirection * 8.2f;
                    OrientProbe(npc, probeAimDirection);
                    if (!Main.dedServ)
                    {
                        if (probeKamikazeTimer == 1)
                        {
                            SoundEngine.PlaySound(SoundID.Item15, npc.Center);
                        }

                        if (probeKamikazeTimer % 2 == 0)
                        {
                            Dust fuseDust = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(14f, 14f),
                                DustID.OrangeTorch, -npc.velocity * 0.3f, Scale: 1.65f);
                            fuseDust.noGravity = true;
                        }
                    }

                    probeKamikazeTimer++;
                    if (authority && probeKamikazeTimer > ProbeKamikazeFlightTicks)
                    {
                        probeExploded = true;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<DestroyerProbeExplosion>(), EnemyDamage.Projectile(ProbeExplosionDamage), 5f,
                            Main.myPlayer);
                        npc.life = 0;
                        npc.HitEffect();
                        npc.checkDead();
                        npc.active = false;
                        npc.netUpdate = true;
                    }
                }

                return false;
            }

            // Coordinated Probe Barrage: all active probes stay >= 20 tiles (320 px) from any player, build a white dust telegraph, and fire 6 coordinated shots spaced 30 ticks apart.
            if (headState != null && headState.move == Move.ProbeBarrage)
            {
                npc.localAI[0] = 0f;
                probeBreathTimer = 0;
                if (repairSegment >= 0)
                {
                    repairSegment = -1;
                    repairStart = -1;
                }

                Player closestPlayer = targetPlayer;
                float closestDistanceSquared = float.MaxValue;
                foreach (Player candidatePlayer in Main.ActivePlayers)
                {
                    if (!candidatePlayer.dead)
                    {
                        float distanceSquared = Vector2.DistanceSquared(npc.Center, candidatePlayer.Center);
                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            closestPlayer = candidatePlayer;
                        }
                    }
                }

                Vector2 toTarget = probeAimDirection;
                if (targetPlayer != null)
                {
                    toTarget = (targetPlayer.Center - npc.Center).SafeNormalize(probeAimDirection);
                }

                probeAimDirection = toTarget;
                OrientProbe(npc, probeAimDirection);

                if (closestPlayer != null)
                {
                    Vector2 fromClosest = npc.Center - closestPlayer.Center;
                    float distanceToClosest = fromClosest.Length();
                    Vector2 radialOutward = fromClosest.SafeNormalize(-toTarget);
                    // Stay at least 20 tiles (320 px) away from any player; orbit around 24 tiles (384 px) standoff.
                    float desiredStandoff = ProbeBarrageMinDistance + 64f;
                    if (distanceToClosest < ProbeBarrageMinDistance + 24f)
                    {
                        Vector2 retreatVelocity = radialOutward * 8.5f;
                        npc.velocity = Vector2.Lerp(npc.velocity, retreatVelocity, 0.14f);
                    }
                    else
                    {
                        float orbitSign = npc.whoAmI % 2 == 0 ? 1f : -1f;
                        Vector2 tangent = radialOutward.RotatedBy(MathHelper.PiOver2 * orbitSign) * 3.2f;
                        Vector2 radialCorrection = radialOutward * MathHelper.Clamp((desiredStandoff - distanceToClosest) * 0.08f, -4.5f, 4.5f);
                        npc.velocity = Vector2.Lerp(npc.velocity, tangent + radialCorrection, 0.09f);
                    }
                }

                Vector2 nozzle = npc.Center + probeAimDirection * 14f;
                if (headState.beat == Beat.Windup && !Main.dedServ)
                {
                    if (headState.timer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.55f, Pitch = 0.35f }, nozzle);
                    }

                    float progress = MathHelper.Clamp(headState.timer / (float)Math.Max(1, headState.duration), 0f, 1f);
                    float ringRadius = MathHelper.Lerp(42f, 10f, progress);
                    int moteCount = 1 + (int)(progress * 2f);
                    for (int i = 0; i < moteCount; i++)
                    {
                        Vector2 ringOffset = Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                        int dustType = DustID.WhiteTorch;
                        if (i % 2 == 1)
                        {
                            dustType = DustID.SilverFlame;
                        }

                        Dust telegraphDust = Dust.NewDustPerfect(nozzle + ringOffset, dustType,
                            -ringOffset * 0.15f + npc.velocity, Scale: MathHelper.Lerp(0.95f, 1.65f, progress));
                        telegraphDust.noGravity = true;
                    }

                    if (headState.timer % 2 == 0)
                    {
                        Dust coreDust = Dust.NewDustPerfect(nozzle + Main.rand.NextVector2Circular(4f, 4f),
                            DustID.WhiteTorch, probeAimDirection * 1.1f, Scale: MathHelper.Lerp(1.0f, 1.9f, progress));
                        coreDust.noGravity = true;
                    }

                    Lighting.AddLight(nozzle, 0.75f * progress, 0.75f * progress, 0.85f * progress);
                }
                else if (headState.beat == Beat.Active)
                {
                    int barrageTick = headState.timer - 20;
                    if (!Main.dedServ && barrageTick >= -10 && barrageTick <= 150)
                    {
                        int cycleTick = (barrageTick + 30) % 30;
                        if (cycleTick >= 20 && cycleTick < 30 && headState.timer % 2 == 0)
                        {
                            Vector2 glintOffset = Main.rand.NextVector2CircularEdge(14f, 14f);
                            Dust glint = Dust.NewDustPerfect(nozzle + glintOffset, DustID.WhiteTorch,
                                -glintOffset * 0.18f + npc.velocity, Scale: 1.3f);
                            glint.noGravity = true;
                        }
                    }

                    // 6 coordinated shots spaced 30 ticks apart (timer = 20, 50, 80, 110, 140, 170).
                    if (barrageTick >= 0 && barrageTick <= 150 && barrageTick % 30 == 0)
                    {
                        if (authority)
                        {
                            Vector2 shotVelocity = probeAimDirection * 14.5f;
                            int shotIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), nozzle, shotVelocity,
                                ModContent.ProjectileType<TetsujinLaser>(), EnemyDamage.Projectile(ProbeShotDamage), 2f,
                                Main.myPlayer);
                            if (shotIndex >= 0 && shotIndex < Main.maxProjectiles)
                            {
                                Main.projectile[shotIndex].tileCollide = false;
                                Main.projectile[shotIndex].timeLeft = 300;
                                Main.projectile[shotIndex].netUpdate = true;
                            }
                        }

                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.7f, Pitch = 0.15f }, nozzle);
                            for (int i = 0; i < 12; i++)
                            {
                                float spreadAngle = MathHelper.Lerp(-0.55f, 0.55f, i / 11f);
                                Vector2 burstVelocity = probeAimDirection.RotatedBy(spreadAngle) * Main.rand.NextFloat(2.2f, 5.8f);
                                int dustType = DustID.WhiteTorch;
                                if (i % 3 == 0)
                                {
                                    dustType = DustID.SilverFlame;
                                }

                                Dust shotDust = Dust.NewDustPerfect(nozzle, dustType, burstVelocity, Scale: 1.55f);
                                shotDust.noGravity = true;
                            }

                            Lighting.AddLight(nozzle, 0.95f, 0.95f, 1.0f);
                        }
                    }
                }

                return false;
            }

            // Slower Fire-Breath Probe: moves slower toward a nearby player, builds a 90-tick dust telegraph, then emits 60 ticks of fire breath.
            if (fireBreathProbe)
            {
                npc.localAI[0] = 0f;
                if (probeBreathCooldown > 0 && authority)
                {
                    probeBreathCooldown--;
                }

                Vector2 toPlayer = probeAimDirection;
                if (targetPlayer != null)
                {
                    toPlayer = (targetPlayer.Center - npc.Center).SafeNormalize(probeAimDirection);
                }

                if (probeBreathTimer == 0)
                {
                    Vector2 desiredVelocity = toPlayer * 4.6f;
                    npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.06f);
                    probeAimDirection = toPlayer;
                    OrientProbe(npc, probeAimDirection);

                    if (authority && probeBreathCooldown <= 0 && targetPlayer != null
                        && Vector2.Distance(npc.Center, targetPlayer.Center) <= 320f)
                    {
                        probeAimDirection = toPlayer;
                        probeBreathTimer = 1;
                        npc.netUpdate = true;
                    }

                    return false;
                }

                int totalBreathCycle = ProbeBreathTelegraphTicks + ProbeBreathFireTicks;
                if (probeBreathTimer <= ProbeBreathTelegraphTicks)
                {
                    npc.velocity *= 0.92f;
                    float aimAngle = probeAimDirection.ToRotation().AngleTowards(toPlayer.ToRotation(), 0.035f);
                    probeAimDirection = aimAngle.ToRotationVector2();
                    OrientProbe(npc, probeAimDirection);

                    if (!Main.dedServ)
                    {
                        if (probeBreathTimer == 1)
                        {
                            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.3f }, npc.Center);
                        }

                        Vector2 nozzle = npc.Center + probeAimDirection * 14f;
                        float progress = probeBreathTimer / (float)ProbeBreathTelegraphTicks;
                        int moteCount = 1 + (int)(progress * 3f);
                        float ringRadius = MathHelper.Lerp(48f, 12f, progress);
                        for (int i = 0; i < moteCount; i++)
                        {
                            Vector2 ringOffset = Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                            int dustType = DustID.Torch;
                            if (i % 2 == 1)
                            {
                                dustType = DustID.OrangeTorch;
                            }

                            Dust gather = Dust.NewDustPerfect(nozzle + ringOffset, dustType,
                                -ringOffset * 0.14f + npc.velocity, Scale: MathHelper.Lerp(0.95f, 1.85f, progress));
                            gather.noGravity = true;
                        }

                        if (probeBreathTimer % 2 == 0)
                        {
                            Dust core = Dust.NewDustPerfect(nozzle + Main.rand.NextVector2Circular(4f, 4f),
                                DustID.Torch, probeAimDirection * 1.2f, Scale: MathHelper.Lerp(1.0f, 2.2f, progress));
                            core.noGravity = true;
                        }

                        Lighting.AddLight(nozzle, 0.9f * progress, 0.45f * progress, 0.1f * progress);
                    }

                    probeBreathTimer++;
                    if (authority && probeBreathTimer == ProbeBreathTelegraphTicks + 1)
                    {
                        npc.netUpdate = true;
                    }

                    return false;
                }

                if (probeBreathTimer <= totalBreathCycle)
                {
                    npc.velocity *= 0.90f;
                    float aimAngle = probeAimDirection.ToRotation().AngleTowards(toPlayer.ToRotation(), 0.022f);
                    probeAimDirection = aimAngle.ToRotationVector2();
                    OrientProbe(npc, probeAimDirection);
                    Vector2 nozzle = npc.Center + probeAimDirection * 14f;

                    if (authority && probeBreathTimer % 2 == 0)
                    {
                        float spread = Main.rand.NextFloat(-0.07f, 0.07f);
                        Vector2 breathVelocity = probeAimDirection.RotatedBy(spread) * 8.5f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), nozzle, breathVelocity,
                            ModContent.ProjectileType<FireBreath>(), EnemyDamage.Projectile(ProbeBreathDamage), 0f,
                            Main.myPlayer);
                    }

                    if (!Main.dedServ)
                    {
                        if (probeBreathTimer % 8 == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.28f, Pitch = 0.15f }, nozzle);
                        }

                        Lighting.AddLight(nozzle, 1.1f, 0.55f, 0.15f);
                    }

                    probeBreathTimer++;
                    if (authority && probeBreathTimer > totalBreathCycle)
                    {
                        probeBreathTimer = 0;
                        probeBreathCooldown = 120;
                        npc.netUpdate = true;
                    }

                    return false;
                }
            }

            // The server schedules probe lasers explicitly. Vanilla's shot counter would create a duplicate.
            npc.localAI[0] = 0f;
            if (repairSegment >= 0)
            {
                return !RunProbeRepair(npc, head, headState, authority);
            }

            if (authority && probeLaserCooldown == 0 && headState != null && !headState.IsRetreating && targetPlayer != null
                && npc.WithinRange(targetPlayer.Center, DestroyerHullLaser.ProbeLength)
                && Collision.CanHit(npc.position, npc.width, npc.height, targetPlayer.position, targetPlayer.width, targetPlayer.height))
            {
                Vector2 aim = (targetPlayer.Center - npc.Center).SafeNormalize(probeAimDirection);
                int laser = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, aim,
                    ModContent.ProjectileType<DestroyerHullLaser>(), EnemyDamage.Projectile(LaserDamage), 0f,
                    Main.myPlayer, ProbeLaserTellTicks, npc.whoAmI, -1f);
                if (laser < Main.maxProjectiles)
                {
                    probeLaserCooldown = ProbeLaserCooldown;
                }
            }

            if (probeLaserCooldown > ProbeLaserCooldown - ProbeLaserBusyTicks)
            {
                return true;
            }

            return !RunProbeRepair(npc, head, headState, authority);
        }

        private bool AllowsRepair(NPC head, TheDestroyer headState)
        {
            if (headState == null || headState.IsRetreating || head.life >= head.lifeMax)
            {
                return false;
            }

            return repairOnlyProbe || (headState.move != Move.ProbeBarrage
                && headState.beat != Beat.Burrow && headState.beat != Beat.Erupt);
        }

        private bool RunProbeRepair(NPC npc, NPC head, TheDestroyer headState, bool authority)
        {
            if (authority && repairCooldown > 0)
            {
                repairCooldown--;
            }

            bool canRepair = AllowsRepair(head, headState);
            if (repairSegment >= 0)
            {
                NPC segment = Main.npc[repairSegment];
                bool linked = segment.active && segment.realLife == probeOwner
                    && (segment.type == NPCID.TheDestroyerBody || segment.type == NPCID.TheDestroyerTail);
                bool inRange = linked && npc.WithinRange(segment.Center, RepairRange);
                bool canApproach = repairOnlyProbe && repairStart >= 0;
                if (authority && canRepair && canApproach && !inRange)
                {
                    // Pick up a nearer link if the hull passed the probe during its approach.
                    int nearby = FindNearestRepairSegment(npc, RepairRange * RepairRange);
                    if (nearby >= 0)
                    {
                        repairSegment = nearby;
                        repairStart = 0;
                        segment = Main.npc[nearby];
                        linked = true;
                        inRange = true;
                        npc.netUpdate = true;
                    }
                }

                if (!canRepair || !linked || (!inRange && !canApproach))
                {
                    if (authority)
                    {
                        StopRepair(npc);
                    }

                    return false;
                }

                npc.localAI[0] = 0f;
                if (!inRange)
                {
                    Vector2 toHull = (segment.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                    npc.velocity = Vector2.Lerp(npc.velocity, toHull * 10f, 0.12f);
                    OrientProbe(npc, toHull);
                    return true;
                }

                if (!Main.dedServ)
                {
                    Lighting.AddLight(npc.Center, 0.10f, 0.55f, 0.24f);
                    if (repairStart >= 0 && repairStart % 3 == 0)
                    {
                        Vector2 offset = Main.rand.NextVector2Circular(22f, 22f);
                        Dust.NewDustPerfect(npc.Center + offset, DustID.GreenTorch, -offset / 8f, Scale: 1.4f).noGravity = true;
                    }
                }

                if (repairOnlyProbe)
                {
                    Vector2 toHull = segment.Center - npc.Center;
                    float distance = toHull.Length();
                    Vector2 direction = toHull.SafeNormalize(Vector2.UnitX);
                    float followSpeed = Math.Clamp((distance - 210f) * 0.06f, 0f, 10f);
                    npc.velocity = Vector2.Lerp(npc.velocity, direction * followSpeed, 0.15f);
                    OrientProbe(npc, direction);
                }
                else
                {
                    npc.velocity = Vector2.Zero;
                }

                if (repairStart >= 0 && ++repairStart >= RepairWindup && authority)
                {
                    repairStart = -1;
                    npc.netUpdate = true;
                    int beam = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<DestroyerRepairBeam>(), 0, 0f, Main.myPlayer, npc.whoAmI, repairSegment, probeOwner);
                    if (beam >= Main.maxProjectiles)
                    {
                        StopRepair(npc);
                    }
                    else if (repairOnlyProbe)
                    {
                        repairCooldown = SpecialistRepairInterval - RepairWindup - 1;
                    }
                }

                return true;
            }

            bool scheduled = repairOnlyProbe || (probeOpeningTicks == 0 && (Main.GameUpdateCount + npc.whoAmI * 11) % 60 == 0);
            if (!authority || !canRepair || repairCooldown > 0 || !scheduled)
            {
                return false;
            }

            int channels = 0;
            if (!repairOnlyProbe)
            {
                foreach (NPC candidate in Main.ActiveNPCs)
                {
                    if (candidate.type != NPCID.Probe)
                    {
                        continue;
                    }

                    TheDestroyer probe = candidate.GetGlobalNPC<TheDestroyer>();
                    if (probe.probeOwner == probeOwner && !probe.repairOnlyProbe && probe.repairSegment >= 0)
                    {
                        channels++;
                    }
                }
            }

            float maxDistance = repairOnlyProbe ? float.MaxValue : RepairRange * RepairRange;
            int closest = FindNearestRepairSegment(npc, maxDistance);
            if (closest < 0 || channels >= BeamCap)
            {
                return false;
            }

            repairSegment = closest;
            repairStart = 0;
            if (!repairOnlyProbe)
            {
                repairCooldown = RepairCooldown;
            }

            npc.netUpdate = true;
            return true;
        }

        private int FindNearestRepairSegment(NPC probe, float closestDistanceSquared)
        {
            int closest = -1;
            foreach (NPC candidate in Main.ActiveNPCs)
            {
                if (candidate.realLife != probeOwner || (candidate.type != NPCID.TheDestroyerBody && candidate.type != NPCID.TheDestroyerTail))
                {
                    continue;
                }

                float distance = Vector2.DistanceSquared(candidate.Center, probe.Center);
                if (distance <= closestDistanceSquared)
                {
                    closestDistanceSquared = distance;
                    closest = candidate.whoAmI;
                }
            }

            return closest;
        }

        internal static bool CanRepair(NPC probe, NPC segment, NPC head)
        {
            if (!probe.active || probe.type != NPCID.Probe
                || !segment.active || segment.realLife != head.whoAmI
                || (segment.type != NPCID.TheDestroyerBody && segment.type != NPCID.TheDestroyerTail)
                || !head.active || head.type != NPCID.TheDestroyer || probe.Distance(segment.Center) > RepairRange)
            {
                return false;
            }

            TheDestroyer probeState = probe.GetGlobalNPC<TheDestroyer>();
            TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
            return probeState.probeOwner == head.whoAmI && probeState.repairSegment == segment.whoAmI
                && (probeState.repairOnlyProbe || probe.life * 4 >= probe.lifeMax)
                && probeState.probeKamikazeTimer == 0 && !probeState.fireBreathProbe
                && probeState.AllowsRepair(head, headState);
        }

        internal static int RepairTicks => RepairDuration;

        internal static void StopRepair(NPC probe)
        {
            if (probe.active && probe.type == NPCID.Probe)
            {
                TheDestroyer state = probe.GetGlobalNPC<TheDestroyer>();
                state.repairSegment = -1;
                state.repairStart = -1;
                probe.netUpdate = true;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.type == NPCID.Probe)
            {
                return true;
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return true;
            }

            HullHeat heat = GetHullHeat(npc, out float armingProgress);
            Vector2 spriteOrigin = frame.Size() * 0.5f;
            bool flipped = npc.spriteDirection == 1;
            SpriteEffects effects = flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (npc.type == NPCID.TheDestroyer && move == Move.HeadCharge && beat == Beat.Active)
            {
                for (int i = npc.oldPos.Length - 1; i > 0; i--)
                {
                    if (npc.oldPos[i] != Vector2.Zero)
                    {
                        Vector2 trailCenter = npc.oldPos[i] + npc.Size * 0.5f;
                        float opacity = 0.16f * (1f - i / (float)npc.oldPos.Length);
                        Color trailColor = drawColor * opacity;
                        spriteBatch.Draw(texture, trailCenter - screenPos, frame, trailColor,
                            npc.rotation, spriteOrigin, npc.scale, effects, 0f);
                    }
                }
            }

            // Sample across the rotating sprite. Shared grid vertices interpolate the light into a continuous shadow.
            Vector2 spriteCenter = npc.Center;
            for (int row = 0; row < 3; row++)
            {
                float acrossHeight = row * 0.5f;
                for (int column = 0; column < 3; column++)
                {
                    float acrossWidth = column * 0.5f;
                    Vector2 localPosition = new Vector2(frame.Width * acrossWidth, frame.Height * acrossHeight) - spriteOrigin;
                    Vector2 worldPosition = spriteCenter + (localPosition * npc.scale).RotatedBy(npc.rotation);
                    Vector3 sampledLight = Lighting.GetSubLight(worldPosition) * Lighting.GlobalBrightness;
                    Color light = npc.GetAlpha(new Color(sampledLight));
                    float frameX = flipped ? 1f - acrossWidth : acrossWidth;
                    Vector2 uv = new Vector2((frame.X + frameX * frame.Width) / texture.Width,
                        (frame.Y + acrossHeight * frame.Height) / texture.Height);
                    hullVertices[row * 3 + column] = new VertexPositionColorTexture(new Vector3(worldPosition, 0f), light, uv);
                }
            }

            GraphicsDevice device = spriteBatch.GraphicsDevice;
            if (hullLightingEffect == null || hullLightingEffect.IsDisposed)
            {
                hullLightingEffect = new BasicEffect(device)
                {
                    VertexColorEnabled = true,
                    TextureEnabled = true,
                    FogEnabled = false,
                    LightingEnabled = false
                };
            }

            spriteBatch.End();
            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
                device.SamplerStates[0] = SamplerState.PointClamp;
                hullLightingEffect.Texture = texture;
                hullLightingEffect.World = Matrix.CreateTranslation(-screenPos.X, -screenPos.Y, 0f);
                hullLightingEffect.View = Main.GameViewMatrix.TransformationMatrix;
                Viewport viewport = device.Viewport;
                hullLightingEffect.Projection = Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, -1f, 1f);
                hullLightingEffect.CurrentTechnique.Passes[0].Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, hullVertices, 0, hullVertices.Length,
                    hullIndices, 0, hullIndices.Length / 3);

                if (heat != HullHeat.Cold)
                {
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                        DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    try
                    {
                        Vector2 drawPosition = spriteCenter - screenPos;
                        if (heat == HullHeat.Arming)
                        {
                            // Keep the matte warning readable in darkness while the vent's emissive is still off.
                            Color ambient = hullVertices[4].Color;
                            Color shutterLight = new Color(Math.Max(ambient.R, (byte)80), Math.Max(ambient.G, (byte)80),
                                Math.Max(ambient.B, (byte)80), ambient.A);
                            Vector2 shutterOrigin = new Vector2(3f, 5f);
                            float separation = MathHelper.Lerp(2.5f, 11f, armingProgress);
                            for (int side = -1; side <= 1; side += 2)
                            {
                                int sourceSide = flipped ? -side : side;
                                int sourceX = frame.X + frame.Width / 2 + sourceSide * 10 - 3;
                                Rectangle shutter = new Rectangle(sourceX, frame.Y + frame.Height / 2 - 5, 6, 10);
                                Vector2 localOffset = new Vector2(separation * side * npc.scale, 0f);
                                Vector2 shutterPosition = drawPosition + localOffset.RotatedBy(npc.rotation);
                                spriteBatch.Draw(texture, shutterPosition, shutter, shutterLight,
                                    npc.rotation, shutterOrigin, npc.scale, effects, 0f);
                            }
                        }
                        else
                        {
                            Texture2D emissive = TextureAssets.Dest[npc.type - NPCID.TheDestroyer].Value;
                            Color light = new Color(255, 64, 28, 0);
                            if (heat == HullHeat.Hot)
                            {
                                // Keep the same vent silhouette. Solar Dye adds amber motion over the hot core only.
                                light = new Color(255, 215, 155, 0);
                            }
                            else if (npc.type == NPCID.TheDestroyerBody && TryGetHead(npc, out NPC missileOwner))
                            {
                                TheDestroyer ownerState = missileOwner.GetGlobalNPC<TheDestroyer>();
                                if (ownerState.move == Move.MissileBarrage && ownerState.beat == Beat.Active)
                                {
                                    int bayAge = ownerState.MissileBayAge(spawnOrdinal);
                                    if (bayAge >= HullArmingTicks && bayAge < MissileTelegraphTicks)
                                    {
                                        float progress = (bayAge - HullArmingTicks) / (float)(MissileTelegraphTicks - HullArmingTicks);
                                        light = Color.Lerp(new Color(255, 82, 36, 0), new Color(255, 190, 105, 0), progress);
                                    }
                                }
                            }

                            spriteBatch.Draw(emissive, drawPosition, frame, light, npc.rotation, spriteOrigin, npc.scale, effects, 0f);
                            if (bombBayTellTick >= 0 && TryGetHead(npc, out NPC owner))
                            {
                                float flash = BombBayFlash(owner.GetGlobalNPC<TheDestroyer>());
                                if (flash > 0f)
                                {
                                    bombBayFlare ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_flare8_vfx", AssetRequestMode.ImmediateLoad);
                                    Texture2D flareTexture = bombBayFlare.Value;
                                    Vector2 flareOrigin = flareTexture.Size() * 0.5f;
                                    Vector2 bay = BombBayPosition(npc) - screenPos;
                                    float flareWidth = MathHelper.Lerp(26f, 48f, flash) * npc.scale;
                                    Color chargeColor = new Color(255, 65, 18, 0);
                                    Color releaseColor = new Color(255, 236, 170, 0);
                                    Color flareColor = payloadReleaseTick >= 0 ? releaseColor : chargeColor;
                                    spriteBatch.Draw(flareTexture, bay, null, flareColor * flash,
                                        npc.rotation, flareOrigin, flareWidth / flareTexture.Width, SpriteEffects.None, 0f);
                                }
                            }

                            if (heat == HullHeat.Hot)
                            {
                                int dyeIndex = GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye);
                                ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader(dyeIndex, Main.LocalPlayer);
                                shader.UseColor(Color.OrangeRed);
                                shader.Apply(null);
                                Color flare = new Color(255, 182, 82, 0);
                                spriteBatch.Draw(emissive, drawPosition, frame, flare,
                                    npc.rotation, spriteOrigin, npc.scale, effects, 0f);
                            }
                        }
                    }
                    finally
                    {
                        spriteBatch.End();
                    }
                }
            }
            finally
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }

            return false;
        }

        internal static void DrawHull(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float glow, int vanillaType)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - screenPos;
            SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader(GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                shader.UseColor(Color.Lerp(Color.Black, Color.OrangeRed, glow));
                shader.Apply(null);
                Color opaqueColor = new Color((int)drawColor.R, (int)drawColor.G, (int)drawColor.B, 255);
                spriteBatch.Draw(texture, drawPosition, frame, opaqueColor, npc.rotation, origin, npc.scale, effects, 0f);
                if (drawColor != Color.Black)
                {
                    Texture2D emissive = TextureAssets.Dest[vanillaType - NPCID.TheDestroyer].Value;
                    Color ports = new Color(255, 255, 255, 0);
                    spriteBatch.Draw(emissive, drawPosition, frame, ports, npc.rotation, origin, npc.scale, effects, 0f);
                }
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
            }
        }

        internal static void RunMovement(NPC npc, int headType, int bodyType, int tailType, bool legacy, bool directed = false)
        {
            if (npc.ai[3] >= 0f && npc.type != headType)
            {
                npc.realLife = (int)npc.ai[3];
            }

            if (legacy && Main.netMode != NetmodeID.MultiplayerClient && (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead))
            {
                npc.TargetClosest();
            }

            if (npc.type == headType && !npc.active)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Clean up all segments and owned probes with DespawnedByHandler so PostAI never revives a pinned event worm.
                    for (int segmentIndex = 0; segmentIndex < Main.maxNPCs; segmentIndex++)
                    {
                        NPC other = Main.npc[segmentIndex];
                        if (!other.active)
                        {
                            continue;
                        }

                        bool isSegment = segmentIndex == npc.whoAmI || other.realLife == npc.whoAmI;
                        bool isOwnedProbe = !legacy && other.type == NPCID.Probe && other.GetGlobalNPC<TheDestroyer>().probeOwner == npc.whoAmI;
                        if (isSegment || isOwnedProbe)
                        {
                            other.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnedByHandler = true;
                            other.active = false;
                            other.netUpdate = true;
                        }
                    }
                }

                return;
            }

            int previousSegment = (int)npc.ai[1];
            bool previousInBounds = previousSegment >= 0 && previousSegment < Main.maxNPCs;
            npc.alpha = 0;
            npc.dontTakeDamage = false;
            npc.timeLeft = Math.Max(npc.timeLeft, 3600);

            if (npc.type != headType)
            {
                bool brokenLink = false;
                bool previousKilled = false;
                int headIndex = npc.realLife;
                bool headInBounds = headIndex >= 0 && headIndex < Main.maxNPCs;
                if (!previousInBounds)
                {
                    brokenLink = true;
                }
                else if (legacy && Main.npc[previousSegment].life <= 0)
                {
                    brokenLink = true;
                    previousKilled = true;
                }
                else if (!legacy && headInBounds && Main.npc[headIndex].life <= 0)
                {
                    brokenLink = true;
                    previousKilled = true;
                }
                else if (!Main.npc[previousSegment].active || Main.npc[previousSegment].life <= 0)
                {
                    brokenLink = true;
                }

                if (brokenLink && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (previousKilled)
                    {
                        npc.life = 0;
                        npc.HitEffect();
                        npc.checkDead();
                    }
                    else
                    {
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnedByHandler = true;
                        npc.active = false;
                        npc.netUpdate = true;
                    }

                    return;
                }

                if (!legacy)
                {
                    if (!brokenLink)
                    {
                        Vector2 displacement = Main.npc[previousSegment].Center - npc.Center;
                        float spacing = HullSegmentSpacing * npc.scale;
                        Vector2 towardPrevious = displacement.SafeNormalize(Vector2.Zero);
                        npc.Center += towardPrevious * (displacement.Length() - spacing);
                        if (towardPrevious != Vector2.Zero)
                        {
                            npc.rotation = towardPrevious.ToRotation() + MathHelper.PiOver2;
                        }
                    }

                    npc.velocity = Vector2.Zero;
                    return;
                }
            }

            //Spawn segments
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.ai[0] == 0f && npc.type == headType)
                {
                    npc.ai[3] = npc.whoAmI;
                    npc.netUpdate = true;
                    npc.realLife = npc.whoAmI;
                    int newSegmentIndex = 0;
                    int previousIndex = npc.whoAmI;
                    int bodyCount = legacy ? 80 : NPC.GetDestroyerSegmentsCount();
                    for (int j = 0; j <= bodyCount; j++)
                    {
                        int segmentType = bodyType;
                        if (j == bodyCount)
                        {
                            segmentType = tailType;
                        }

                        newSegmentIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)npc.height), segmentType, npc.whoAmI);
                        if (newSegmentIndex >= Main.maxNPCs)
                        {
                            break;
                        }

                        Main.npc[newSegmentIndex].ai[3] = npc.whoAmI;
                        Main.npc[newSegmentIndex].realLife = npc.whoAmI;
                        Main.npc[newSegmentIndex].ai[1] = previousIndex;
                        Main.npc[newSegmentIndex].alpha = 0;
                        Main.npc[newSegmentIndex].dontTakeDamage = false;
                        Main.npc[previousIndex].ai[0] = newSegmentIndex;
                        Main.npc[previousIndex].netUpdate = true;
                        if (!legacy)
                        {
                            Main.npc[newSegmentIndex].GetGlobalNPC<TheDestroyer>().spawnOrdinal = j + 1;
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: newSegmentIndex);
                        }
                        previousIndex = newSegmentIndex;
                    }
                }
            }

            if (directed)
            {
                return;
            }

            if (npc.target < 0 || npc.target >= Main.maxPlayers)
            {
                if (npc.type != headType && npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
                {
                    npc.target = Main.npc[(int)npc.ai[3]].target;
                }

                if (npc.target < 0 || npc.target >= Main.maxPlayers)
                {
                    npc.target = 0;
                }
            }

            float movementSpeed = 16f;
            bool burrowing = false;
            bool retreating = !Main.player[npc.target].active || Main.player[npc.target].dead;
            if (legacy && retreating)
            {
                burrowing = false;
                npc.velocity.Y += 1f;
                if ((double)npc.position.Y > Main.worldSurface * 16.0)
                {
                    npc.velocity.Y += 1f;
                    movementSpeed = 32f;
                }

                if (npc.type == headType && Main.netMode != NetmodeID.MultiplayerClient
                    && npc.position.Y > Main.rockLayer * 16.0
                    && npc.position.Y > Main.player[npc.target].Center.Y + 1200f)
                {
                    for (int segmentIndex = 0; segmentIndex < Main.maxNPCs; segmentIndex++)
                    {
                        NPC other = Main.npc[segmentIndex];
                        if (other.active && (segmentIndex == npc.whoAmI || other.realLife == npc.whoAmI))
                        {
                            other.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnedByHandler = true;
                            other.active = false;
                            other.netUpdate = true;
                        }
                    }

                    return;
                }
            }

            int minimumTileX = (int)(npc.position.X / 16f) - 1;
            int maximumTileX = (int)((npc.position.X + (float)npc.width) / 16f) + 2;
            int minimumTileY = (int)(npc.position.Y / 16f) - 1;
            int maximumTileY = (int)((npc.position.Y + (float)npc.height) / 16f) + 2;
            if (minimumTileX < 0)
            {
                minimumTileX = 0;
            }

            if (maximumTileX > Main.maxTilesX)
            {
                maximumTileX = Main.maxTilesX;
            }

            if (minimumTileY < 0)
            {
                minimumTileY = 0;
            }

            if (maximumTileY > Main.maxTilesY)
            {
                maximumTileY = Main.maxTilesY;
            }

            if (!burrowing)
            {
                Vector2 tilePosition = default(Vector2);
                for (int tileX = minimumTileX; tileX < maximumTileX; tileX++)
                {
                    for (int tileY = minimumTileY; tileY < maximumTileY; tileY++)
                    {
                        if (Main.tile[tileX, tileY] != null && ((Main.tile[tileX, tileY].HasUnactuatedTile && (Main.tileSolid[Main.tile[tileX, tileY].TileType] || (Main.tileSolidTop[Main.tile[tileX, tileY].TileType] && Main.tile[tileX, tileY].TileFrameY == 0))) || Main.tile[tileX, tileY].LiquidAmount > 64))
                        {
                            tilePosition.X = tileX * 16;
                            tilePosition.Y = tileY * 16;
                            if (npc.position.X + (float)npc.width > tilePosition.X && npc.position.X < tilePosition.X + 16f && npc.position.Y + (float)npc.height > tilePosition.Y && npc.position.Y < tilePosition.Y + 16f)
                            {
                                burrowing = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!burrowing)
            {
                if (!Main.dedServ && (npc.type != bodyType || npc.ai[2] != 1f))
                {
                    Lighting.AddLight((int)((npc.position.X + (float)(npc.width / 2)) / 16f), (int)((npc.position.Y + (float)(npc.height / 2)) / 16f), 0.3f, 0.1f, 0.05f);
                }

                npc.localAI[1] = 1f;
                if (npc.type == headType)
                {
                    Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                    int playerRange = 1000;
                    bool farBelowPlayers = true;
                    if (npc.position.Y > Main.player[npc.target].position.Y)
                    {
                        for (int playerIndex = 0; playerIndex < 255; playerIndex++)
                        {
                            if (Main.player[playerIndex].active && !Main.player[playerIndex].dead)
                            {
                                Rectangle rectangle2 = new Rectangle((int)Main.player[playerIndex].position.X - playerRange, (int)Main.player[playerIndex].position.Y - playerRange, playerRange * 2, playerRange * 2);
                                if (rectangle.Intersects(rectangle2))
                                {
                                    farBelowPlayers = false;
                                    break;
                                }
                            }
                        }

                        if (farBelowPlayers)
                        {
                            burrowing = true;
                        }

                    }
                }
            }
            else
            {
                npc.localAI[1] = 0f;
            }

            if (!legacy && retreating)
            {
                burrowing = false;
                npc.velocity.Y += 1f;
                if ((double)npc.position.Y > Main.worldSurface * 16.0)
                {
                    npc.velocity.Y += 1f;
                    movementSpeed = 32f;
                }

                if (npc.type == headType && Main.netMode != NetmodeID.MultiplayerClient
                    && npc.position.Y > Main.rockLayer * 16.0
                    && npc.position.Y > Main.player[npc.target].Center.Y + 1200f)
                {
                    // Mark head, segments, and owned probes with DespawnedByHandler so PostAI does not resurrect the head.
                    for (int segmentIndex = 0; segmentIndex < Main.maxNPCs; segmentIndex++)
                    {
                        NPC other = Main.npc[segmentIndex];
                        if (!other.active)
                        {
                            continue;
                        }

                        bool isSegment = segmentIndex == npc.whoAmI || other.realLife == npc.whoAmI;
                        bool isOwnedProbe = other.type == NPCID.Probe && other.GetGlobalNPC<TheDestroyer>().probeOwner == npc.whoAmI;
                        if (isSegment || isOwnedProbe)
                        {
                            other.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnedByHandler = true;
                            other.active = false;
                            other.netUpdate = true;
                        }
                    }

                    return;
                }
            }

            float turnAcceleration = 0.1f;
            float chaseAcceleration = 0.15f;
            Vector2 wormCenter = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
            float offsetX = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
            float offsetY = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
            offsetX = (int)(offsetX / 16f) * 16;
            offsetY = (int)(offsetY / 16f) * 16;
            wormCenter.X = (int)(wormCenter.X / 16f) * 16;
            wormCenter.Y = (int)(wormCenter.Y / 16f) * 16;
            offsetX -= wormCenter.X;
            offsetY -= wormCenter.Y;
            float distance = (float)Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
            if (npc.type != headType && npc.ai[1] >= 0f && npc.ai[1] < Main.maxNPCs)
            {
                NPC previous = Main.npc[(int)npc.ai[1]];
                wormCenter = npc.Center;
                offsetX = previous.Center.X - wormCenter.X;
                offsetY = previous.Center.Y - wormCenter.Y;

                npc.rotation = (float)Math.Atan2(offsetY, offsetX) + 1.57f;
                distance = (float)Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
                int segmentSpacing = (int)(44f * npc.scale);
                if (distance < 0.001f)
                {
                    return;
                }

                distance = (distance - segmentSpacing) / distance;
                offsetX *= distance;
                offsetY *= distance;
                npc.velocity = Vector2.Zero;
                npc.position.X += offsetX;
                npc.position.Y += offsetY;
                return;
            }

            if (!burrowing)
            {
                if (!retreating && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.TargetClosest();
                }

                npc.velocity.Y += 0.15f;
                if (npc.velocity.Y > movementSpeed)
                {
                    npc.velocity.Y = movementSpeed;
                }

                if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)movementSpeed * 0.4)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X -= turnAcceleration * 1.1f;
                    }

                    else
                    {
                        npc.velocity.X += turnAcceleration * 1.1f;
                    }

                }
                else if (npc.velocity.Y == movementSpeed)
                {
                    if (npc.velocity.X < offsetX)
                    {
                        npc.velocity.X += turnAcceleration;
                    }

                    else if (npc.velocity.X > offsetX)
                    {
                        npc.velocity.X -= turnAcceleration;
                    }

                }
                else if (npc.velocity.Y > 4f)
                {
                    if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X += turnAcceleration * 0.9f;
                    }

                    else
                    {
                        npc.velocity.X -= turnAcceleration * 0.9f;
                    }

                }
            }
            else
            {
                if (!Main.dedServ && npc.soundDelay == 0)
                {
                    float soundInterval = distance / 40f;
                    if (soundInterval < 10f)
                    {
                        soundInterval = 10f;
                    }

                    if (soundInterval > 20f)
                    {
                        soundInterval = 20f;
                    }

                    npc.soundDelay = (int)soundInterval;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.position);
                }

                distance = (float)Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
                float horizontalDistance = Math.Abs(offsetX);
                float verticalDistance = Math.Abs(offsetY);
                if (distance < 0.001f)
                {
                    return;
                }

                float speedRatio = movementSpeed / distance;
                offsetX *= speedRatio;
                offsetY *= speedRatio;
                if (((npc.velocity.X > 0f && offsetX > 0f) || (npc.velocity.X < 0f && offsetX < 0f)) && ((npc.velocity.Y > 0f && offsetY > 0f) || (npc.velocity.Y < 0f && offsetY < 0f)))
                {
                    if (npc.velocity.X < offsetX)
                    {
                        npc.velocity.X += chaseAcceleration;
                    }

                    else if (npc.velocity.X > offsetX)
                    {
                        npc.velocity.X -= chaseAcceleration;
                    }

                    if (npc.velocity.Y < offsetY)
                    {
                        npc.velocity.Y += chaseAcceleration;
                    }

                    else if (npc.velocity.Y > offsetY)
                    {
                        npc.velocity.Y -= chaseAcceleration;
                    }

                }

                if ((npc.velocity.X > 0f && offsetX > 0f) || (npc.velocity.X < 0f && offsetX < 0f) || (npc.velocity.Y > 0f && offsetY > 0f) || (npc.velocity.Y < 0f && offsetY < 0f))
                {
                    if (npc.velocity.X < offsetX)
                    {
                        npc.velocity.X += turnAcceleration;
                    }

                    else if (npc.velocity.X > offsetX)
                    {
                        npc.velocity.X -= turnAcceleration;
                    }

                    if (npc.velocity.Y < offsetY)
                    {
                        npc.velocity.Y += turnAcceleration;
                    }

                    else if (npc.velocity.Y > offsetY)
                    {
                        npc.velocity.Y -= turnAcceleration;
                    }

                    if ((double)Math.Abs(offsetY) < (double)movementSpeed * 0.2 && ((npc.velocity.X > 0f && offsetX < 0f) || (npc.velocity.X < 0f && offsetX > 0f)))
                    {
                        if (npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y += turnAcceleration * 2f;
                        }

                        else
                        {
                            npc.velocity.Y -= turnAcceleration * 2f;
                        }

                    }

                    if ((double)Math.Abs(offsetX) < (double)movementSpeed * 0.2 && ((npc.velocity.Y > 0f && offsetY < 0f) || (npc.velocity.Y < 0f && offsetY > 0f)))
                    {
                        if (npc.velocity.X > 0f)
                        {
                            npc.velocity.X += turnAcceleration * 2f;
                        }

                        else
                        {
                            npc.velocity.X -= turnAcceleration * 2f;
                        }

                    }
                }
                else if (horizontalDistance > verticalDistance)
                {
                    if (npc.velocity.X < offsetX)
                    {
                        npc.velocity.X += turnAcceleration * 1.1f;
                    }

                    else if (npc.velocity.X > offsetX)
                    {
                        npc.velocity.X -= turnAcceleration * 1.1f;
                    }

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)movementSpeed * 0.5)
                    {
                        if (npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y += turnAcceleration;
                        }

                        else
                        {
                            npc.velocity.Y -= turnAcceleration;
                        }

                    }
                }
                else
                {
                    if (npc.velocity.Y < offsetY)
                    {
                        npc.velocity.Y += turnAcceleration * 1.1f;
                    }

                    else if (npc.velocity.Y > offsetY)
                    {
                        npc.velocity.Y -= turnAcceleration * 1.1f;
                    }

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)movementSpeed * 0.5)
                    {
                        if (npc.velocity.X > 0f)
                        {
                            npc.velocity.X += turnAcceleration;
                        }

                        else
                        {
                            npc.velocity.X -= turnAcceleration;
                        }

                    }
                }
            }

            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
            if (npc.type != headType)
            {
                return;
            }

            if (burrowing)
            {
                if (npc.localAI[0] != 1f)
                {
                    npc.netUpdate = true;
                }

                npc.localAI[0] = 1f;
            }
            else
            {
                if (npc.localAI[0] != 0f)
                {
                    npc.netUpdate = true;
                }

                npc.localAI[0] = 0f;
            }

            if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
            {
                npc.netUpdate = true;
            }

        }
    }
}
