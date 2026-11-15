using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private const float EnclosingOrbitSpeed = 0.028f;
        private const float OverheadOrbitSpeed = 0.034f;
        private const float EnclosingRadius = 540f;
        private const float OverheadRadius = 440f;
        private const float OverheadHeight = 680f;
        private const int OverheadShapeTicks = 145;
        private const int EnclosingShapeTicks = 210;
        private const int OverheadActiveTicks = 300;
        private const int EnclosingActiveTicks = 330;
        internal const int OrbitTellTicks = 44;
        internal const int OrbitFireTicks = 20;
        private const int OrbitBeamSpacing = OrbitTellTicks + OrbitFireTicks + DestroyerHullLaser.FadeTicks + 4;
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
        private const int ProbeKamikazeLockTiles = 6;
        private const int ProbeKamikazeFlightTicks = 60;
        private const float ProbeBarrageMinDistance = 320f;
        private const int RepairRange = 480;
        private const int RepairWindup = 32;
        private const int RepairDuration = 240;
        private const int RepairCooldown = 420;
        private const int BeamCap = 3;
        private const int PortTell = 20;
        private const int RippleDelay = 8;
        private const int RippleRepeat = 180;
        private const int ChargeRange = 1800;
        private const int ChargeOvershoot = 400;
        private const float ChargeSpeed = 30f;
        private const int MawLock = DestroyerMawBeam.TellTicks;
        private const int DashReaim = 12;

        private enum Move : byte
        {
            None,
            BroadsidePass,
            CurtainPass,
            HeadCharge,
            EnclosingCoil,
            HullGlowSpin,
            MawBeam,
            RippleBroadside,
            BombDrop,
            MineDrop,
            MissileBarrage,
            ProbeBarrage
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
            Retreat
        }

        private Move move;
        private Beat beat;
        private int phase = 1;
        private int timer;
        private int duration = 180;
        private int sequence;
        private int chainLength;
        private int pendingPhase;
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
        private int attacksBeforeNextOrbit;
        private float hullTurn;
        private bool announcedBeat;
        private bool bodyPoseForMaw;
        private AttackBag<Move> attackBag;
        private int probeOwner = -1;
        private bool legacyProbe;
        private bool fireBreathProbe;
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

        public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        {
            return npc.type == NPCID.TheDestroyer || npc.type == NPCID.TheDestroyerBody
                || npc.type == NPCID.TheDestroyerTail || npc.type == NPCID.Probe;
        }

        public string DebugAttackLabel
        {
            get
            {
                string label = move == Move.None ? beat.ToString() : DebugLabels.Humanize(move.ToString());
                return $"P{phase} · {label}";
            }
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPCID.TheDestroyer] = 10;
            NPCID.Sets.TrailingMode[NPCID.TheDestroyer] = 0;
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
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            if (npc.type == NPCID.Probe)
            {
                legacyProbe = bitReader.ReadBit();
                fireBreathProbe = bitReader.ReadBit();
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
            if (npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
            {
                return TryGetHead(npc, out NPC owner) && owner.GetGlobalNPC<TheDestroyer>().SegmentHeat(spawnOrdinal) >= 1f;
            }

            if (npc.type == NPCID.Probe || !TryGetHead(npc, out NPC head))
            {
                return true;
            }

            TheDestroyer state = head.GetGlobalNPC<TheDestroyer>();
            return state.beat != Beat.Retreat;
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
                RunMovement(npc, NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, false);
                if (npc.active && spawnOrdinal > 0 && TryGetHead(npc, out NPC head))
                {
                    TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
                    if (authority && (headState.move == Move.EnclosingCoil || headState.move == Move.HullGlowSpin))
                    {
                        hullTurn = MathHelper.WrapAngle(npc.rotation - previousRotation);
                    }

                    if (!Main.dedServ && npc.type == NPCID.TheDestroyerBody)
                    {
                        bool curtainTelegraph = (headState.move == Move.CurtainPass || headState.move == Move.RippleBroadside)
                            && headState.beat == Beat.Shape && headState.timer > 0 && headState.timer <= 60;
                        if (curtainTelegraph)
                        {
                            // Classic Old Destroyer spiraling torch-ribbon telegraph, reserved sparingly for full-hull curtain barrages.
                            Vector2 headDirection = head.velocity.SafeNormalize((head.rotation - MathHelper.PiOver2).ToRotationVector2());
                            Vector2 dustOffset = (headDirection * 48f).RotatedBy(MathHelper.ToRadians(6f * headState.timer));
                            Vector2 dustVelocity = (headDirection * 12f).RotatedBy(MathHelper.PiOver2) / 6f;
                            Dust.NewDustPerfect(npc.Center + dustOffset, DustID.Torch, dustVelocity, Scale: 3f).noGravity = true;
                        }
                        else if (headState.move == Move.BombDrop && headState.beat == Beat.Shape && spawnOrdinal % 5 == 0 && headState.timer % 3 == 0)
                        {
                            Vector2 offset = Main.rand.NextVector2Circular(12f, 8f) + new Vector2(0f, 14f);
                            Dust.NewDustPerfect(npc.Center + offset, DustID.RedTorch, new Vector2(0f, 1.5f), Scale: 1.5f).noGravity = true;
                        }
                        else if (headState.move == Move.MineDrop && headState.beat == Beat.Shape
                            && spawnOrdinal >= 15 && spawnOrdinal <= 70 && spawnOrdinal % 5 == 0 && headState.timer % 3 == 0)
                        {
                            Vector2 offset = Main.rand.NextVector2Circular(12f, 8f) + new Vector2(0f, 14f);
                            Dust.NewDustPerfect(npc.Center + offset, DustID.OrangeTorch, new Vector2(0f, 1.5f), Scale: 1.55f).noGravity = true;
                        }
                        else if (headState.move == Move.MissileBarrage && headState.beat == Beat.Active)
                        {
                            int telegraphAge = headState.MissileBayAge(spawnOrdinal);
                            if (telegraphAge > 0 && telegraphAge <= MissileTelegraphTicks)
                            {
                                if (telegraphAge == 1)
                                {
                                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.45f, Pitch = -0.35f }, npc.Center);
                                }

                                // Double the plume along each bay's local exhaust axis. Keep the scatter small so it stays attached to the bay.
                                float progress = telegraphAge / (float)MissileTelegraphTicks;
                                Vector2 segmentSide = npc.rotation.ToRotationVector2();
                                int puffsPerSide = 1 + (int)(progress * 2f);
                                for (int side = -1; side <= 1; side += 2)
                                {
                                    Vector2 exhaustDirection = segmentSide * side;
                                    Vector2 bayOrigin = npc.Center + exhaustDirection * (HullPortOffset * npc.scale);
                                    for (int i = 0; i < puffsPerSide; i++)
                                    {
                                        float exhaustSpeed = Main.rand.NextFloat(2.8f, 7.2f) * (0.65f + 0.65f * progress);
                                        Vector2 exhaustVelocity = exhaustDirection * exhaustSpeed + Main.rand.NextVector2Circular(0.9f, 0.9f);
                                        Dust exhaust = Dust.NewDustPerfect(bayOrigin + Main.rand.NextVector2Circular(5f, 5f),
                                            DustID.Smoke, exhaustVelocity, 95,
                                            new Color(165, 170, 175), Main.rand.NextFloat(0.95f, 1.45f) * (0.85f + 0.35f * progress));
                                        exhaust.noGravity = true;
                                        exhaust.noLight = true;
                                        exhaust.fadeIn = 1.35f;
                                    }
                                }
                            }
                        }
                        else if (headState.beat == Beat.Active)
                        {
                            int portAge = headState.PortAge(spawnOrdinal);
                            if (portAge > 0 && portAge <= PortTell && portAge % 3 == 0)
                            {
                                Vector2 offset = Main.rand.NextVector2Circular(14f, 14f);
                                Dust.NewDustPerfect(npc.Center + offset, DustID.Torch, -offset * 0.15f, Scale: 1.5f).noGravity = true;
                            }
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
                    int probeCount = 4;
                    ReleaseProbes(npc, probeCount);
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
                        Vector2 orbitOffset = new Vector2(prowlCos * 420f * heading, prowlWave * 260f);
                        Vector2 targetPoint = player.Center + orbitOffset;
                        Vector2 desiredProwl = (targetPoint - npc.Center).SafeNormalize(Vector2.UnitX * heading) * (13f + phase * 1.2f);
                        SteerHead(npc, desiredProwl, 0.048f, 0.08f);

                        if (authority && timer >= 30 && timer % 75 == 0 && CountActiveProbes(npc) < 2)
                        {
                            EmitHarassLaser(npc, player);
                        }
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
                        case Move.BroadsidePass:
                        case Move.CurtainPass:
                        case Move.RippleBroadside:
                            RunPass(npc, player, authority);
                            break;
                        case Move.EnclosingCoil:
                        case Move.HullGlowSpin:
                            RunOrbit(npc, authority);
                            break;
                        case Move.HeadCharge:
                            RunCharge(npc, player, authority);
                            break;
                        case Move.MawBeam:
                            RunMaw(npc, player, authority);
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
                    cards.Add(Move.BroadsidePass);
                    cards.Add(Move.BroadsidePass);
                    cards.Add(Move.CurtainPass);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.HeadCharge);
                    cards.Add(Move.EnclosingCoil);
                    cards.Add(Move.EnclosingCoil);
                    cards.Add(Move.HullGlowSpin);
                    cards.Add(Move.HullGlowSpin);
                    cards.Add(Move.HullGlowSpin);
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
                        cards.Add(Move.RippleBroadside);
                        cards.Add(Move.ProbeBarrage);
                        cards.Add(Move.ProbeBarrage);
                    }
                    else if (phase == 3)
                    {
                        cards.Add(Move.HeadCharge);
                        cards.Add(Move.MineDrop);
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

            // Wait for range or an eligible non-repeat. AttackBag's default fallback permits repeats.
            bool drew = attackBag.TryDraw(candidate => candidate != attackBag.Last
                && (candidate != Move.HeadCharge || npc.Distance(player.Center) <= ChargeRange)
                && (candidate != Move.EnclosingCoil || canCircle && airborne)
                && (candidate != Move.HullGlowSpin || canSpin)
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

        private void StartAttack(NPC npc, Player player, Move nextMove)
        {
            move = nextMove;
            sequence = 0;
            vulnerable = false;
            bodyPoseForMaw = false;
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
            if (move == Move.EnclosingCoil || move == Move.HullGlowSpin)
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
                case Move.BroadsidePass:
                case Move.RippleBroadside:
                case Move.CurtainPass:
                    float basePassAngle = heading > 0 ? 0f : MathHelper.Pi;
                    float tiltDegrees = Main.rand.NextFloat(18f, 44f) * (Main.rand.NextBool() ? 1f : -1f);
                    if (move == Move.CurtainPass)
                    {
                        tiltDegrees = Main.rand.NextFloat(14f, 34f) * (npc.Center.Y <= player.Center.Y ? 1f : -1f);
                    }

                    angle = basePassAngle + MathHelper.ToRadians(tiltDegrees);
                    sweepSign = npc.Center.Y <= player.Center.Y ? 1 : -1;
                    Vector2 passDirection = angle.ToRotationVector2();
                    Vector2 passNormal = passDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                    float lateralOffset = move == Move.CurtainPass ? -330f : -280f;
                    goal = anchor - passDirection * 460f + passNormal * lateralOffset;
                    break;
                case Move.HeadCharge:
                case Move.ProbeBarrage:
                    goal = anchor + (npc.Center - anchor).SafeNormalize(-Vector2.UnitX) * 420f;
                    break;
                case Move.EnclosingCoil:
                case Move.HullGlowSpin:
                    float orbitRadius = EnclosingRadius;
                    if (move == Move.HullGlowSpin)
                    {
                        anchor.Y -= OverheadHeight;
                        orbitRadius = OverheadRadius;
                    }

                    sweepSign = -heading;
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
                case Move.BombDrop:
                case Move.MineDrop:
                    goal = anchor + new Vector2(-heading * 460f, -240f);
                    foreach (NPC segment in Main.ActiveNPCs)
                    {
                        if (segment.type == NPCID.TheDestroyerBody && segment.realLife == npc.whoAmI)
                        {
                            segment.localAI[1] = 0f;
                        }
                    }
                    break;
                case Move.MissileBarrage:
                    goal = anchor + new Vector2(-heading * 420f, -200f);
                    break;
            }

            SetBeat(npc, Beat.Setup, 140);
        }

        private void RunPass(NPC npc, Player player, bool authority)
        {
            bool curtain = move == Move.CurtainPass;
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= duration))
                {
                    if (player != null)
                    {
                        anchor = player.Center;
                        Vector2 toAnchor = anchor - npc.Center;
                        float cross = Vector2.Dot(toAnchor, angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2));
                        if (cross >= 0f)
                        {
                            sweepSign = 1;
                        }
                        else
                        {
                            sweepSign = -1;
                        }
                    }

                    SetBeat(npc, Beat.Shape, 60);
                }

                return;
            }

            Vector2 passDirection = angle.ToRotationVector2();
            if (beat == Beat.Shape)
            {
                float shapeSpeed = curtain ? 13.5f : 15f;
                SteerHead(npc, passDirection * shapeSpeed, 0.055f, 0.12f);
                if (authority && timer >= duration)
                {
                    int extraRipple = 0;
                    if (move == Move.RippleBroadside)
                    {
                        extraRipple = RippleRepeat;
                    }

                    SetBeat(npc, Beat.Active, LastEmitterTick(npc) + PortTell + HullTell + DestroyerHullLaser.FireTicks + DestroyerHullLaser.FadeTicks + extraRipple);
                }

                return;
            }

            float firingSpeed = 10.5f;
            if (curtain)
            {
                firingSpeed = 11.5f;
            }

            if (authority && move == Move.RippleBroadside && player != null && timer == RippleRepeat - 55)
            {
                // Curve the head back toward the player before the 180-tick repeat ripple starts so the body sweeps across the arena again.
                angle = (player.Center - npc.Center).ToRotation() - sweepSign * 0.0025f * timer;
                npc.netUpdate = true;
            }

            // Gently curve the pass around the target so the worm weaves dynamically while firing.
            float curveOffset = (float)Math.Sin(timer * 0.035f) * 0.18f + sweepSign * 0.0025f * timer;
            Vector2 currentPassDirection = (angle + curveOffset).ToRotationVector2();
            SteerHead(npc, currentPassDirection * firingSpeed, 0.045f, 0.10f);

            if (authority)
            {
                Vector2 fireDirection = currentPassDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                if (player != null)
                {
                    Vector2 toPlayer = player.Center - npc.Center;
                    if (Vector2.Dot(toPlayer, fireDirection) < -60f)
                    {
                        fireDirection = -fireDirection;
                    }
                }

                EmitVolley(npc, timer - PortTell - 1, fireDirection);
                if (move == Move.RippleBroadside)
                {
                    EmitVolley(npc, timer - PortTell - 1 - RippleRepeat, fireDirection);
                }

                if (timer >= duration)
                {
                    if (curtain && sequence == 0 && player != null)
                    {
                        sequence = 1;
                        heading = -heading;
                        anchor = player.Center;
                        float nextBaseAngle = heading > 0 ? 0f : MathHelper.Pi;
                        float nextTilt = Main.rand.NextFloat(-32f, 32f);
                        angle = nextBaseAngle + MathHelper.ToRadians(nextTilt);
                        Vector2 nextDirection = angle.ToRotationVector2();
                        Vector2 nextNormal = nextDirection.RotatedBy(MathHelper.PiOver2 * sweepSign);
                        goal = anchor - nextDirection * 420f - nextNormal * 280f;
                        SetBeat(npc, Beat.Setup, 130);
                    }
                    else
                    {
                        FinishAttack(npc);
                    }
                }
            }
        }

        private void RunOrbit(NPC npc, bool authority)
        {
            bool overhead = move == Move.HullGlowSpin;
            float radius = overhead ? OverheadRadius : EnclosingRadius;
            float orbitSpeed = overhead ? OverheadOrbitSpeed : EnclosingOrbitSpeed;
            int shapeTicks = overhead ? OverheadShapeTicks : EnclosingShapeTicks;
            if (beat == Beat.Setup)
            {
                // Brake on the way to the rim so a tight approach cannot turn into a loop around the entry point.
                Vector2 toEntry = goal - npc.Center;
                float distance = toEntry.Length();
                float entrySpeed = Math.Clamp(distance * 0.09f, 4f, 17f);
                Vector2 entryDirection = toEntry.SafeNormalize(Vector2.UnitX);
                npc.velocity = Vector2.Lerp(npc.velocity, entryDirection * entrySpeed, 0.13f);
                if (npc.velocity.LengthSquared() > 0.5f)
                {
                    float entryRotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    npc.rotation = npc.rotation.AngleTowards(entryRotation, 0.16f);
                }

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
            if (beat == Beat.Active)
            {
                orbitAge += shapeTicks;
            }

            float angularStep = sweepSign * orbitSpeed;
            float orbitAngle = angle + angularStep * orbitAge;
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
            if (!authority)
            {
                return;
            }

            if (beat == Beat.Shape)
            {
                if (timer >= duration)
                {
                    int firingTicks = overhead ? OverheadActiveTicks : EnclosingActiveTicks;
                    SetBeat(npc, Beat.Active, firingTicks);
                }

                return;
            }

            int beamLifetime = OrbitTellTicks + OrbitFireTicks + DestroyerHullLaser.FadeTicks;
            int beamAge = timer - orbitBeamStart;
            if (timer >= duration || (sequence == 2 && beamAge >= beamLifetime))
            {
                Vector2 exitDirection = new Vector2(heading, 0.45f);
                goal = npc.Center + exitDirection * 480f;
                vulnerable = true;
                SetBeat(npc, Beat.Recovery, 65);
                return;
            }

            if (sequence < 2 && timer + beamLifetime <= duration && (sequence == 0 || beamAge >= OrbitBeamSpacing))
            {
                float fireMiddle;
                if (overhead)
                {
                    // Cross the underside from one flank to the other. Each live arc is about 39 degrees.
                    float lean = sequence == 0 ? -0.24f : 0.31f;
                    fireMiddle = MathHelper.PiOver2 + sweepSign * lean;
                }
                else
                {
                    // Sweep across the center from opposite banks. The short beams leave exits near the rim.
                    fireMiddle = sequence == 0 ? 0f : MathHelper.Pi;
                }

                // Lead the actual vent by the charge and half of the sweep, accounting for the hull update on the spawn tick.
                float leadTicks = OrbitTellTicks + OrbitFireTicks * 0.5f;
                if (overhead)
                {
                    leadTicks -= 2f;
                }

                float tellAngle = fireMiddle - angularStep * leadTicks;
                int portSide = overhead ? -sweepSign : sweepSign;
                NPC emitter = null;
                float bestAngleError = 0.16f;
                foreach (NPC segment in Main.ActiveNPCs)
                {
                    if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI)
                    {
                        continue;
                    }

                    TheDestroyer segmentState = segment.GetGlobalNPC<TheDestroyer>();
                    int ordinal = segmentState.spawnOrdinal;
                    float turnError = Math.Abs(segmentState.hullTurn - angularStep);
                    if (ordinal < 4 || ordinal == orbitBeamOrdinal || turnError > 0.006f)
                    {
                        continue;
                    }

                    Vector2 fromAnchor = segment.Center - anchor;
                    float distanceToAnchor = fromAnchor.Length();
                    if (Math.Abs(distanceToAnchor - radius) > 96f)
                    {
                        continue;
                    }

                    Vector2 portDirection = segment.rotation.ToRotationVector2() * portSide;
                    Vector2 radialDirection = fromAnchor / distanceToAnchor;
                    float radialAlignment = Vector2.Dot(radialDirection, portDirection);
                    float expectedAlignment = overhead ? radialAlignment : -radialAlignment;
                    float angleError = Math.Abs(MathHelper.WrapAngle(portDirection.ToRotation() - tellAngle));
                    if (expectedAlignment >= 0.95f && angleError < bestAngleError)
                    {
                        bestAngleError = angleError;
                        emitter = segment;
                    }
                }

                if (emitter != null)
                {
                    Vector2 direction = emitter.rotation.ToRotationVector2() * portSide;
                    Vector2 muzzle = emitter.Center + direction * (HullPortOffset * emitter.scale);
                    int portMode = overhead ? DestroyerHullLaser.OverheadPort : DestroyerHullLaser.EnclosingPort;
                    if (portSide < 0)
                    {
                        portMode++;
                    }

                    int projectileIndex = Projectile.NewProjectile(emitter.GetSource_FromAI(), muzzle, direction,
                        ModContent.ProjectileType<DestroyerHullLaser>(), EnemyDamage.Projectile(LaserDamage), 0f,
                        Main.myPlayer, OrbitTellTicks, emitter.whoAmI, portMode);
                    if (projectileIndex < Main.maxProjectiles)
                    {
                        sequence++;
                        orbitBeamOrdinal = emitter.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                        orbitBeamStart = timer;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        private void RunCharge(NPC npc, Player player, bool authority)
        {
            if (beat == Beat.Setup)
            {
                bool arrived = TravelTo(npc, goal, 17f);
                if (authority && (arrived || timer >= duration))
                {
                    SetBeat(npc, Beat.Windup, ChargeTell);
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
                    float dashDistance = Math.Min(1600f, Vector2.Distance(npc.Center, lockedOrigin) + ChargeOvershoot);
                    SetBeat(npc, Beat.Active, (int)MathF.Ceiling(dashDistance / 25f));
                }

                return;
            }

            if (beat == Beat.Active)
            {
                SteerHead(npc, lockedDirection * 25f, 0.035f, 0.22f);
                if (authority && timer >= duration)
                {
                    if (++sequence < chainLength)
                    {
                        SetBeat(npc, Beat.Windup, ChargeTell);
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
                    SetBeat(npc, Beat.Active, 95);
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

                        int ordinal = segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal;
                        if (ordinal <= 0 || ordinal % 5 != 0)
                        {
                            continue;
                        }

                        // MineDrop uses 4 fewer eligible fifth-segments (15..70 instead of 5..80) and caps at 8 dropped mines.
                        if (dropMines && (ordinal < 15 || ordinal > 70 || sequence >= 8))
                        {
                            continue;
                        }

                        bool isAbovePlayer = segment.Center.Y < player.Center.Y - 24f
                            && Math.Abs(segment.Center.X - player.Center.X) <= 640f;
                        if (isAbovePlayer)
                        {
                            Vector2 dropPosition = segment.Center + new Vector2(0f, 18f);
                            if (dropMines && Collision.SolidCollision(dropPosition - new Vector2(12f, 12f), 24, 24))
                            {
                                continue;
                            }

                            segment.localAI[1] = 1f;
                            sequence++;
                            Vector2 dropVelocity = new Vector2(npc.velocity.X * 0.12f, 2.5f);
                            if (dropMines)
                            {
                                Projectile.NewProjectile(segment.GetSource_FromAI(), dropPosition, dropVelocity,
                                    ModContent.ProjectileType<ProximityMine>(), EnemyDamage.Projectile(BombDamage), 2f,
                                    Main.myPlayer, 0f, 0f, npc.whoAmI + 1);
                            }
                            else
                            {
                                Projectile.NewProjectile(segment.GetSource_FromAI(), dropPosition, dropVelocity,
                                    ModContent.ProjectileType<EnemyFirebomb>(), EnemyDamage.Projectile(BombDamage), 9f,
                                    Main.myPlayer, 0f, 0f, 2f);
                            }

                            SoundEngine.PlaySound(SoundID.Item1, dropPosition);
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

            if (authority)
            {
                // 8 segments (every 3rd segment from 9 to 30), 60 ticks between telegraph starts, firing 2 missiles 90 ticks after each telegraph starts (16 missiles total).
                int launchTickOffset = timer - MissileTelegraphTicks;
                if (launchTickOffset >= 0 && launchTickOffset % MissileSegmentInterval == 0)
                {
                    int missileStep = launchTickOffset / MissileSegmentInterval;
                    if (missileStep >= 0 && missileStep < MissileSegmentCount)
                    {
                        int targetOrdinal = MissileFirstOrdinal + missileStep * MissileSegmentStride;
                        foreach (NPC segment in Main.ActiveNPCs)
                        {
                            if (segment.type != NPCID.TheDestroyerBody || segment.realLife != npc.whoAmI
                                || segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal != targetOrdinal)
                            {
                                continue;
                            }

                            Vector2 segmentSide = segment.rotation.ToRotationVector2();
                            Vector2 toTarget = -Vector2.UnitY;
                            float targetPlayerArg = 0f;
                            if (player != null)
                            {
                                toTarget = (player.Center - segment.Center).SafeNormalize(-Vector2.UnitY);
                                targetPlayerArg = player.whoAmI + 1f;
                            }

                            for (int side = -1; side <= 1; side += 2)
                            {
                                Vector2 launchOrigin = segment.Center + segmentSide * (HullPortOffset * segment.scale * side);
                                Vector2 launchHeading = (toTarget + segmentSide * (0.45f * side)).SafeNormalize(segmentSide * side);
                                Projectile.NewProjectile(segment.GetSource_FromAI(), launchOrigin,
                                    launchHeading * ProximityMineRocket.Speed,
                                    ModContent.ProjectileType<ProximityMineRocket>(), EnemyDamage.Projectile(MissileDamage),
                                    2f, Main.myPlayer, 0f, targetPlayerArg, npc.whoAmI + 1);
                            }

                            if (!Main.dedServ)
                            {
                                SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.75f, Pitch = -0.1f }, segment.Center);
                            }
                            break;
                        }
                    }
                }

                if (timer >= duration)
                {
                    FinishAttack(npc);
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
                if (authority && timer == duration - MawLock)
                {
                    if (player != null)
                    {
                        lockedDirection = (player.Center - npc.Center).SafeNormalize(facing);
                        Vector2 targetMotion = player.velocity;
                        float crossing = Vector2.Dot(targetMotion, lockedDirection.RotatedBy(MathHelper.PiOver2));
                        if (crossing >= 0f)
                        {
                            sweepSign = 1;
                        }
                        else
                        {
                            sweepSign = -1;
                        }
                    }

                    lockedOrigin = npc.Center + lockedDirection * (28f * npc.scale);
                    npc.netUpdate = true;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), lockedOrigin, lockedDirection,
                        ModContent.ProjectileType<DestroyerMawBeam>(), EnemyDamage.Projectile(MawDamage), 0f,
                        Main.myPlayer, npc.whoAmI, lockedDirection.ToRotation(), sweepSign * phase);
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
                float beamAngle = DestroyerMawBeam.SweepAngle(lockedDirection.ToRotation(), sweepSign * phase, timer - 1);
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

        private int ChargeTell
        {
            get
            {
                return phase switch
                {
                    1 => 50,
                    2 => 35,
                    _ => 25
                };
            }
        }

        private int HullTell => 60 - phase * 10;

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
            int prowl = Main.rand.Next(110, 175);
            if (phase == 3)
            {
                prowl = Main.rand.Next(75, 125);
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

        private int EmitterStride
        {
            get => phase == 1 ? 7 : 6;
        }

        private int LastEmitterTick(NPC npc)
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

            return (lastOrdinal / EmitterStride - 1) * RippleDelay + 1;
        }

        private void EmitVolley(NPC head, int rippleTick, Vector2 direction)
        {
            if (rippleTick < 0 || rippleTick % RippleDelay != 0)
            {
                return;
            }

            int ordinal = (rippleTick / RippleDelay + 1) * EmitterStride;

            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI
                    || segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal != ordinal)
                {
                    continue;
                }

                Vector2 origin = segment.Center;
                Vector2 segmentNormal = segment.rotation.ToRotationVector2() * sweepSign;
                if (Vector2.Dot(segmentNormal, direction) < 0f)
                {
                    segmentNormal = -segmentNormal;
                }

                Vector2 aimedDirection = Vector2.Lerp(direction, segmentNormal, 0.55f);
                Vector2 beamVelocity = aimedDirection.SafeNormalize(direction);
                Projectile.NewProjectile(segment.GetSource_FromAI(), origin, beamVelocity,
                    ModContent.ProjectileType<DestroyerHullLaser>(), EnemyDamage.Projectile(LaserDamage), 0f,
                    Main.myPlayer, HullTell, segment.whoAmI, 0.22f);
                return;
            }
        }

        private void EmitHarassLaser(NPC head, Player player)
        {
            int targetOrdinal = Main.rand.Next(1, 9) * 8;
            foreach (NPC segment in Main.ActiveNPCs)
            {
                if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI
                    || segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal != targetOrdinal)
                {
                    continue;
                }

                Vector2 toPlayer = (player.Center - segment.Center).SafeNormalize(Vector2.UnitY);
                float spread = Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 beamVelocity = toPlayer.RotatedBy(spread);
                Projectile.NewProjectile(segment.GetSource_FromAI(), segment.Center, beamVelocity,
                    ModContent.ProjectileType<DestroyerHullLaser>(), EnemyDamage.Projectile(LaserDamage), 0f,
                    Main.myPlayer, HullTell + 10, segment.whoAmI, 0.20f);
                return;
            }
        }

        private int PortAge(int ordinal)
        {
            bool lasers = move == Move.BroadsidePass || move == Move.CurtainPass || move == Move.RippleBroadside;
            int stride = EmitterStride;
            if (!lasers || ordinal <= 0 || ordinal % stride != 0)
            {
                return -1;
            }

            int age = timer - (ordinal / stride - 1) * RippleDelay;
            if (move == Move.RippleBroadside && age > RippleRepeat)
            {
                age -= RippleRepeat;
            }

            return age;
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

        private static float ShotHeat(int age, int tellTicks, int liveTicks)
        {
            if (age <= 0)
            {
                return 0f;
            }

            if (age < tellTicks)
            {
                return MathHelper.Lerp(0.25f, 0.82f, age / (float)tellTicks);
            }

            int coolingAge = age - tellTicks - liveTicks;
            if (coolingAge < 0)
            {
                return 1f;
            }

            float cooling = MathHelper.Clamp(coolingAge / (float)DestroyerHullLaser.FadeTicks, 0f, 1f);
            return MathHelper.Lerp(0.64f, 0f, cooling);
        }

        private float SegmentHeat(int ordinal)
        {
            if (ordinal <= 0)
            {
                return 0f;
            }

            // Only a fully armed port reaches 1. Transitional light and charging red stay below contact damage.
            if (move == Move.MissileBarrage)
            {
                if (beat == Beat.Active)
                {
                    return ShotHeat(MissileBayAge(ordinal), MissileTelegraphTicks, MissileHotTicks);
                }

                return 0f;
            }

            if (move == Move.EnclosingCoil || move == Move.HullGlowSpin)
            {
                if (beat == Beat.Active && orbitBeamOrdinal == ordinal)
                {
                    int beamAge = timer - orbitBeamStart + 1;
                    return ShotHeat(beamAge, OrbitTellTicks, OrbitFireTicks);
                }

                if (move == Move.HullGlowSpin && beat == Beat.Shape)
                {
                    float heatWave = MathF.Sin(timer * 0.09f - ordinal * 0.23f);
                    return 0.22f + 0.18f * heatWave;
                }

                return 0f;
            }

            if (beat == Beat.Burrow || beat == Beat.Erupt || beat == Beat.Windup)
            {
                float progress = MathHelper.Clamp(timer / (float)duration, 0f, 1f);
                float bandOrdinal = NPC.GetDestroyerSegmentsCount() * (1f - progress);
                float distanceToBand = Math.Abs(ordinal - bandOrdinal);
                return Math.Max(0f, 0.58f * (1f - distanceToBand / 10f));
            }

            if ((move == Move.BombDrop || move == Move.MineDrop) && (beat == Beat.Shape || beat == Beat.Active) && ordinal % 5 == 0)
            {
                if (move == Move.BombDrop || ordinal >= 15 && ordinal <= 70)
                {
                    return 0.62f;
                }
            }

            if (beat == Beat.Active)
            {
                int age = PortAge(ordinal);
                return ShotHeat(age, PortTell + HullTell, DestroyerHullLaser.FireTicks);
            }

            return 0f;
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
                    if (existingState.probeOwner == head.whoAmI && existingState.probeKamikazeTimer == 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void ReleaseProbes(NPC head, int count)
        {
            bool hasFireBreathProbe = false;
            foreach (NPC existing in Main.ActiveNPCs)
            {
                if (existing.type == NPCID.Probe)
                {
                    TheDestroyer existingState = existing.GetGlobalNPC<TheDestroyer>();
                    if (existingState.probeOwner == head.whoAmI && existingState.fireBreathProbe)
                    {
                        hasFireBreathProbe = true;
                        break;
                    }
                }
            }

            int spacing = NPC.GetDestroyerSegmentsCount() / (count + 1);
            for (int i = 1; i <= count; i++)
            {
                int ordinal = spacing * i;
                foreach (NPC segment in Main.ActiveNPCs)
                {
                    if (segment.type != NPCID.TheDestroyerBody || segment.realLife != head.whoAmI || segment.ai[2] != 0f
                        || segment.GetGlobalNPC<TheDestroyer>().spawnOrdinal != ordinal)
                    {
                        continue;
                    }

                    int probeIndex = NPC.NewNPC(segment.GetSource_FromAI(), (int)segment.Center.X, (int)segment.Bottom.Y, NPCID.Probe);
                    if (probeIndex < Main.maxNPCs)
                    {
                        segment.ai[2] = 1f;
                        segment.netUpdate = true;
                        if (!hasFireBreathProbe)
                        {
                            hasFireBreathProbe = true;
                            Main.npc[probeIndex].GetGlobalNPC<TheDestroyer>().fireBreathProbe = true;
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: probeIndex);
                        }
                    }

                    break;
                }
            }
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
            if (npc.type == NPCID.Probe && probeOwner >= 0 && !probeExploded && Main.netMode != NetmodeID.MultiplayerClient)
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

            if (headState != null)
            {
                if (headState.move != Move.None)
                {
                    // Probes chill on independent laser beams during key boss moves, opting into healing or recovery instead.
                    npc.localAI[0] = 0f;
                }
                else if ((Main.GameUpdateCount + npc.whoAmI) % 2 == 0 && npc.localAI[0] > 1f)
                {
                    // Double the recovery time between probe laser beams during Prowl.
                    npc.localAI[0] -= 1f;
                }
            }

            if (repairCooldown > 0 && authority)
            {
                repairCooldown--;
            }

            bool canRepair = headState != null && head.life < head.lifeMax
                && headState.move != Move.ProbeBarrage
                && headState.beat != Beat.Burrow && headState.beat != Beat.Erupt && headState.beat != Beat.Retreat;
            if (repairSegment >= 0)
            {
                NPC segment = Main.npc[repairSegment];
                bool inRange = segment.active && segment.realLife == probeOwner && npc.Distance(segment.Center) <= RepairRange;
                if (authority && (!canRepair || !inRange))
                {
                    repairSegment = -1;
                    repairStart = -1;
                    npc.netUpdate = true;
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

                npc.velocity = Vector2.Zero;
                npc.localAI[0] = 0f;
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
                }

                return false;
            }

            if (authority && canRepair && repairCooldown == 0 && (Main.GameUpdateCount + npc.whoAmI * 11) % 60 == 0)
            {
                int channels = 0;
                float nearestDistance = RepairRange * RepairRange;
                int closest = -1;
                foreach (NPC candidate in Main.ActiveNPCs)
                {
                    if (candidate.type == NPCID.Probe)
                    {
                        TheDestroyer probe = candidate.GetGlobalNPC<TheDestroyer>();
                        if (probe.probeOwner == probeOwner && probe.repairSegment >= 0)
                        {
                            channels++;
                        }
                    }
                    else if (candidate.realLife == probeOwner && (candidate.type == NPCID.TheDestroyerBody || candidate.type == NPCID.TheDestroyerTail))
                    {
                        float distance = Vector2.DistanceSquared(candidate.Center, npc.Center);
                        if (distance <= nearestDistance)
                        {
                            nearestDistance = distance;
                            closest = candidate.whoAmI;
                        }
                    }
                }

                if (closest >= 0 && channels < BeamCap)
                {
                    repairSegment = closest;
                    repairStart = 0;
                    repairCooldown = RepairCooldown;
                    npc.netUpdate = true;
                }
            }

            return true;
        }

        internal static bool CanRepair(NPC probe, NPC segment, NPC head)
        {
            if (!probe.active || probe.type != NPCID.Probe || probe.life * 4 < probe.lifeMax
                || !segment.active || segment.realLife != head.whoAmI
                || !head.active || head.type != NPCID.TheDestroyer || probe.Distance(segment.Center) > RepairRange)
            {
                return false;
            }

            TheDestroyer probeState = probe.GetGlobalNPC<TheDestroyer>();
            TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
            return probeState.probeOwner == head.whoAmI && probeState.repairSegment == segment.whoAmI
                && probeState.probeKamikazeTimer == 0 && !probeState.fireBreathProbe
                && headState.move != Move.ProbeBarrage
                && headState.beat != Beat.Burrow && headState.beat != Beat.Erupt && headState.beat != Beat.Retreat;
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

            if (!TryGetHead(npc, out NPC head))
            {
                return true;
            }

            TheDestroyer state = head.GetGlobalNPC<TheDestroyer>();
            if (npc.type != NPCID.TheDestroyer)
            {
                float segmentHeat = state.SegmentHeat(spawnOrdinal);
                DrawHull(npc, spriteBatch, screenPos, drawColor, segmentHeat, npc.type, false);
                return false;
            }

            float glow = state.phase == 3 ? 0.72f : 0.10f;
            if (state.beat == Beat.Burrow || state.beat == Beat.Erupt)
            {
                glow = Math.Max(glow, 0.85f);
            }
            else if (state.beat == Beat.Recovery && state.vulnerable)
            {
                float fraction = MathHelper.Clamp(state.timer / (float)state.duration, 0f, 1f);
                glow = MathHelper.Lerp(1f, glow, fraction);
            }
            else if (state.beat == Beat.Windup)
            {
                int bodyCount = NPC.GetDestroyerSegmentsCount();
                float fraction = MathHelper.Clamp(state.timer / (float)state.duration, 0f, 1f);
                float band = bodyCount * (1f - fraction);
                float difference = Math.Abs(band);
                glow = Math.Max(glow, 1f - difference / (state.move == Move.MawBeam ? 16f : 9f));
            }
            else if (state.move == Move.HullGlowSpin && (state.beat == Beat.Shape || state.beat == Beat.Active))
            {
                glow = Math.Max(glow, 0.84f);
            }

            if (state.move == Move.HeadCharge && state.beat == Beat.Active)
            {
                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = npc.oldPos.Length - 1; i > 0; i -= 2)
                {
                    if (npc.oldPos[i] != Vector2.Zero)
                    {
                        Vector2 trailCenter = npc.oldPos[i] + npc.Size * 0.5f;
                        float opacity = 0.2f * (1f - i / (float)npc.oldPos.Length);
                        spriteBatch.Draw(texture, trailCenter - screenPos, npc.frame, Color.OrangeRed * opacity,
                            npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                    }
                }
            }

            DrawHull(npc, spriteBatch, screenPos, drawColor, glow, npc.type);
            return false;
        }

        internal static void DrawHull(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float glow, int vanillaType, bool drawIdleEffects = true)
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
            if (!drawIdleEffects && glow <= 0f)
            {
                spriteBatch.Draw(texture, drawPosition, frame, drawColor, npc.rotation, origin, npc.scale, effects, 0f);
                return;
            }

            ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader(GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Color heatColor = drawIdleEffects ? Color.OrangeRed : new Color(255, 48, 27);
                shader.UseColor(Color.Lerp(Color.Black, heatColor, glow));
                shader.Apply(null);
                Color opaqueColor = new Color((int)drawColor.R, (int)drawColor.G, (int)drawColor.B, 255);
                spriteBatch.Draw(texture, drawPosition, frame, opaqueColor, npc.rotation, origin, npc.scale, effects, 0f);
                if (drawColor != Color.Black || !drawIdleEffects)
                {
                    Texture2D emissive = TextureAssets.Dest[vanillaType - NPCID.TheDestroyer].Value;
                    Color ports = new Color(255, 255, 255, 0);
                    if (!drawIdleEffects)
                    {
                        ports = new Color(255, 60, 35, 0) * (glow * 0.70f);
                        if (glow >= 1f)
                        {
                            ports = new Color(255, 220, 185, 0);
                        }
                    }

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
                        float spacing = 44f * npc.scale;
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
