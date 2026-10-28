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
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy.DarkrootImpSpells;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Darkroot Imp — SHM flying curse-caster. Hovers 220-380px from its target (FloaterAI: flying A*, keeps line
    // of sight so shots never spawn in walls) and deals one of four attacks from an AttackBag:
    //   Cursed Breath  75t mouth ember (Massacre-style smoulder → swell) / 54t breath sweeping 40° across you / 45t recovery
    //   Acid Fan       30t charge converging on the orb / 5-shot 40° fan from the orb (empowered: a 2nd fan 45t later) / 45t
    //   Bile Mortar    40t orb swell / 3 lobbed globs 8t apart, solved to land at and past you, each leaves a 3s acid pool
    //                  (empowered: 5 globs) / 50t
    //   Swoop          40t pull-back + scream / distance-solved dive that overshoots you by 200px / 50t slow drift (punish window)
    // Below 50% life ("empowered"): an extra Breath card, 2 fans, 5 globs.
    // The sprite only flaps, so every tell is dust + sound + doubled flap tempo. Art faces LEFT (spriteDirection -1 = unflipped).
    // MP: the server picks attacks and locks every aim (sweep angle/side, dive velocity/length); those + state/timers are synced
    // in SendExtraAI and both sides then run the same timeline. Projectiles spawn on the server only; dust/sound on clients only.
    // Was "Ephemeral Oolacile Demon" (OolacileDemon) until 2026-10; LegacyName keeps old saves' kill counts mapped to it.
    [LegacyName("OolacileDemon")]
    class DarkrootImp : ModNPC, IStaggerable, IDebugAttackLabel
    {
        enum DemonAttack : byte
        {
            None,
            CursedBreath,
            AcidFan,
            BileMortar,
            Swoop,
        }

        // Damage is the raw spawn value, same convention as before the rework (breath kept at its old 35).
        const int CursedBreathDamage = 35;
        const int AcidSpitDamage = 36;
        const int BileGlobDamage = 40;
        const int AcidPoolDamage = 24;

        // Hover band and speed for FloaterAI (px, px/tick).
        const float HoverMin = 220f;
        const float HoverMax = 380f;
        const float HoverTopSpeed = 4.5f;
        const float HoverAcceleration = 0.07f;

        // Neutral pause between attacks, after each attack's own recovery (ticks).
        const int NeutralPauseMin = 40;
        const int NeutralPauseMax = 70;

        // Cursed Breath. Reach = 10 px/t x 44t lifetime ≈ 440px, so the gate stops at 420.
        const int BreathWindupTicks = 75;
        const int BreathTicks = 54;
        const int BreathRecoveryTicks = 45;
        const int BreathShotInterval = 3;
        const float BreathSpeed = 10f;
        const float BreathSweepHalfDegrees = 20f;
        const float BreathMaxRange = 420f;

        // Acid Fan.
        const int FanChargeTicks = 30;
        const int FanSecondTick = 75;
        const int FanRecoveryTicks = 45;
        const int FanShots = 5;
        const float FanSpreadDegrees = 40f;
        const float FanSpeed = 8f;
        const float FanMaxRange = 760f;

        // Bile Mortar. Each glob is solved to arrive in MortarFlightTicks; offsets are px along the demon→player line
        // (positive = past the player), so the volley covers the player's spot and the ground behind them.
        const int MortarChargeTicks = 40;
        const int MortarLobInterval = 8;
        const int MortarRecoveryTicks = 50;
        const int MortarGlobs = 3;
        const int MortarGlobsEmpowered = 5;
        const int MortarFlightTicks = 44;
        const float MortarMinRange = 160f;
        const float MortarMaxRange = 720f;
        const int MortarFloorScanTiles = 25;
        static readonly float[] MortarLandingOffsets = { 0f, 80f, -60f, 150f, -120f };

        // Swoop.
        const int SwoopRearTicks = 40;
        const int SwoopRecoveryTicks = 50;
        const float SwoopSpeed = 13f;
        const float SwoopOvershoot = 200f;
        const int SwoopMinDiveTicks = 12;
        const int SwoopMaxDiveTicks = 50;
        const float SwoopMinRange = 140f;
        const float SwoopMaxRange = 560f;

        // Animation: 5-frame wing flap, a frame every FrameTicks; telegraphs flap twice as fast. Frame 1 = wings raised (dive pose).
        const int FrameTicks = 6;
        const int DiveFrame = 1;

        DemonAttack State = DemonAttack.None;
        int AttackTimer;
        int AttackCooldown = 90;
        bool empowered;
        float sweepCenterAngle;
        int sweepSide = 1;
        Vector2 lockedVelocity;
        int swoopDiveTicks;

        // Server-only selection state (never synced; clients follow State).
        AttackBag<DemonAttack> attackBag;
        bool bagWasEmpowered;

        readonly FloaterAI.FloaterState floatState = new FloaterAI.FloaterState();

        ///<summary>Mouth on the sprite: frame px (40,47) of a 104x100 frame, 1.1 scale; the frame centre draws 16px above NPC.Center.</summary>
        Vector2 Mouth => NPC.Center + new Vector2(NPC.spriteDirection * 13f, -19f);

        ///<summary>The green orb in its hand: frame px (32,70). Acid comes from here.</summary>
        Vector2 Orb => NPC.Center + new Vector2(NPC.spriteDirection * 22f, 6f);

        bool BreathReleasing => State == DemonAttack.CursedBreath && AttackTimer > BreathWindupTicks && AttackTimer <= BreathWindupTicks + BreathTicks;

        bool Diving => State == DemonAttack.Swoop && AttackTimer > SwoopRearTicks && AttackTimer <= SwoopRearTicks + swoopDiveTicks;

        bool IsTelegraphing
        {
            get
            {
                if (State == DemonAttack.CursedBreath)
                {
                    return AttackTimer <= BreathWindupTicks;
                }

                if (State == DemonAttack.AcidFan)
                {
                    bool firstCharge = AttackTimer <= FanChargeTicks;
                    bool secondCharge = empowered && AttackTimer > FanChargeTicks + 15 && AttackTimer <= FanSecondTick;
                    return firstCharge || secondCharge;
                }

                if (State == DemonAttack.BileMortar)
                {
                    return AttackTimer <= MortarChargeTicks;
                }

                if (State == DemonAttack.Swoop)
                {
                    return AttackTimer <= SwoopRearTicks;
                }

                return false;
            }
        }

        public string DebugAttackLabel => State switch
        {
            DemonAttack.CursedBreath => "Cursed Breath",
            DemonAttack.AcidFan => "Acid Fan",
            DemonAttack.BileMortar => "Bile Mortar",
            DemonAttack.Swoop => "Swoop",
            _ => "Hover",
        };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 5;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.width = 70;
            NPC.height = 70;
            NPC.aiStyle = -1; // was 22: vanilla's wraith AI ran AND this AI(), so every movement step applied twice per tick
            NPC.damage = 63;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.defense = 70;
            NPC.noTileCollide = false;
            NPC.behindTiles = true;
            NPC.lavaImmune = true;
            NPC.DeathSound = new SoundStyle("tsorcRevamp/Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 3500;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.2f;
            NPC.noGravity = true;
            NPC.value = 12000; // life / 2.5 : was 1875 with 4500 health
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkrootImpBanner>();
        }

        // Legacy spawn rules, used only while the "New Enemy Spawns" config is off (Systems/EnemySpawns.cs replaces them).
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool inHell = spawnInfo.SpawnTileY >= Main.maxTilesY - 200;

            if (NPC.AnyNPCs(ModContent.NPCType<DarkrootImp>()))
            {
                return 0;
            }

            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneCrimson && Main.rand.NextBool(20))
            {
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDesert && Main.rand.NextBool(30))
            {
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneUndergroundDesert && Main.rand.NextBool(20))
            {
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && inHell && Main.rand.NextBool(16))
            {
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && inHell && Main.rand.NextBool(8))
            {
                return 1;
            }

            return 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write(empowered);
            writer.Write(sweepCenterAngle);
            writer.Write((sbyte)sweepSide);
            writer.WriteVector2(lockedVelocity);
            writer.Write(swoopDiveTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (DemonAttack)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            empowered = reader.ReadBoolean();
            sweepCenterAngle = reader.ReadSingle();
            sweepSide = reader.ReadSByte();
            lockedVelocity = reader.ReadVector2();
            swoopDiveTicks = reader.ReadInt32();
        }

        ///<summary>Poise break: whatever it was winding up or releasing is cancelled. Already-launched projectiles keep flying.</summary>
        public void OnStagger(NPC npc)
        {
            State = DemonAttack.None;
            AttackTimer = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = 60;
                NPC.netUpdate = true;
            }
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            // Staggered: wings falter — it sags and drifts, no attacks.
            if (globalNPC.StaggerTimer > 0)
            {
                NPC.velocity *= 0.9f;
                NPC.velocity.Y += 0.08f;
                return;
            }

            if (State == DemonAttack.None)
            {
                NPC.TargetClosest(false);
            }

            Player target = Main.player[NPC.target];
            bool targetValid = NPC.HasValidTarget && target.active && !target.dead;

            if (!targetValid)
            {
                State = DemonAttack.None;
                NPC.velocity *= 0.95f;
                NPC.velocity.Y -= 0.05f;
                NPC.EncourageDespawn(10);
                return;
            }

            if (State == DemonAttack.None)
            {
                // Neutral: hover in the band, face the target, and count down to the next pick.
                FloaterAI.Run(NPC, target, floatState, HoverTopSpeed, HoverAcceleration, HoverMin, HoverMax);
                FaceTowards(target.Center.X);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ChooseAttack(target);
                }

                return;
            }

            AttackTimer++;

            if (IsTelegraphing)
            {
                globalNPC.AttackTelegraphing = true;
            }
            else
            {
                globalNPC.AttackCommitted = true;
            }

            switch (State)
            {
                case DemonAttack.CursedBreath:
                    TickCursedBreath(target);
                    break;
                case DemonAttack.AcidFan:
                    TickAcidFan(target);
                    break;
                case DemonAttack.BileMortar:
                    TickBileMortar(target);
                    break;
                case DemonAttack.Swoop:
                    TickSwoop(target);
                    break;
            }
        }

        // Server-only. Deals the next attack from the bag; a card whose gate is closed waits in the bag.
        // Nothing open (no line of sight, say) → keep hovering and look again in 20 ticks.
        void ChooseAttack(Player target)
        {
            bool isEmpowered = NPC.life <= NPC.lifeMax / 2;

            if (attackBag == null)
            {
                attackBag = new AttackBag<DemonAttack>(FillAttackBag);
            }

            // Phase change: deal the empowered bag (extra Breath card) at once instead of finishing the old one.
            if (isEmpowered != bagWasEmpowered)
            {
                bagWasEmpowered = isEmpowered;
                attackBag.Clear();
            }

            float distance = NPC.Distance(target.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool targetHasFloor = TryFindFloor(target.Center.X, target.Bottom.Y - 32f, out _);

            bool IsOpen(DemonAttack attack)
            {
                if (!lineOfSight)
                {
                    return false;
                }

                if (attack == DemonAttack.CursedBreath)
                {
                    return distance <= BreathMaxRange;
                }

                if (attack == DemonAttack.AcidFan)
                {
                    return distance <= FanMaxRange;
                }

                if (attack == DemonAttack.BileMortar)
                {
                    bool inBand = distance >= MortarMinRange && distance <= MortarMaxRange;
                    return inBand && targetHasFloor;
                }

                if (attack == DemonAttack.Swoop)
                {
                    return distance >= SwoopMinRange && distance <= SwoopMaxRange;
                }

                return false;
            }

            if (!attackBag.TryDraw(IsOpen, out DemonAttack picked))
            {
                AttackCooldown = 20;
                return;
            }

            State = picked;
            AttackTimer = 0;
            empowered = isEmpowered;
            NPC.netUpdate = true;
        }

        // Fan x2 is the bread-and-butter card; empowered adds a second Breath.
        void FillAttackBag(List<DemonAttack> cards)
        {
            cards.Add(DemonAttack.CursedBreath);
            cards.Add(DemonAttack.AcidFan);
            cards.Add(DemonAttack.AcidFan);
            cards.Add(DemonAttack.BileMortar);
            cards.Add(DemonAttack.Swoop);

            if (bagWasEmpowered)
            {
                cards.Add(DemonAttack.CursedBreath);
            }
        }

        void EndAttack()
        {
            State = DemonAttack.None;
            AttackTimer = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = Main.rand.Next(NeutralPauseMin, NeutralPauseMax + 1);
                NPC.netUpdate = true;
            }
        }

        void TickCursedBreath(Player target)
        {
            int breathEnd = BreathWindupTicks + BreathTicks;
            NPC.velocity *= 0.9f;

            // Windup: plant and smoulder at the mouth, tracking the player until the breath locks.
            if (AttackTimer <= BreathWindupTicks)
            {
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f });
                }

                float progress = AttackTimer / (float)BreathWindupTicks;
                EmitMouthEmber(progress);

                // Lock the sweep on the last windup tick: centred on the player, starting from a random side.
                if (AttackTimer == BreathWindupTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toTarget = target.Center - Mouth;
                    sweepCenterAngle = toTarget.ToRotation();
                    sweepSide = Main.rand.NextBool() ? 1 : -1;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Breath: the aim sweeps from one side of the locked line to the other, 40° total, a puff every 3 ticks.
            if (AttackTimer <= breathEnd)
            {
                float sweepProgress = (AttackTimer - BreathWindupTicks) / (float)BreathTicks;
                float halfSweep = MathHelper.ToRadians(BreathSweepHalfDegrees);
                float sweepOffset = MathHelper.Lerp(-halfSweep, halfSweep, sweepProgress) * sweepSide;
                float angle = sweepCenterAngle + sweepOffset;
                Vector2 aim = angle.ToRotationVector2();

                FaceTowards(NPC.Center.X + aim.X);

                if (AttackTimer % 12 == 1)
                {
                    PlaySound(SoundID.Item34 with { Volume = 0.6f });
                }

                bool shotTick = (AttackTimer - BreathWindupTicks) % BreathShotInterval == 1;
                if (shotTick && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = aim * BreathSpeed + Main.rand.NextVector2Circular(0.6f, 0.6f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, velocity, ModContent.ProjectileType<DarkrootImpBreath>(), CursedBreathDamage, 0f, Main.myPlayer);
                }

                return;
            }

            if (AttackTimer >= breathEnd + BreathRecoveryTicks)
            {
                EndAttack();
            }
        }

        void TickAcidFan(Player target)
        {
            int lastVolleyTick = FanChargeTicks;
            if (empowered)
            {
                lastVolleyTick = FanSecondTick;
            }

            NPC.velocity *= 0.9f;
            FaceTowards(target.Center.X);

            if (AttackTimer == 1)
            {
                PlaySound(SoundID.Item103 with { Volume = 0.5f });
            }

            if (IsTelegraphing)
            {
                // The charge visibly gathers into the orb: dust converging from 40px to 6px over each charge window.
                int chargeStart = 0;
                int chargeEnd = FanChargeTicks;
                if (AttackTimer > FanChargeTicks)
                {
                    chargeStart = FanChargeTicks + 15;
                    chargeEnd = FanSecondTick;
                }

                float progress = (AttackTimer - chargeStart) / (float)(chargeEnd - chargeStart);
                EmitOrbGather(progress);
            }

            bool firstVolley = AttackTimer == FanChargeTicks;
            bool secondVolley = empowered && AttackTimer == FanSecondTick;

            if (firstVolley || secondVolley)
            {
                PlaySound(SoundID.Item20);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // The second fan is offset half a step, so its gaps fall where the first fan's shots were.
                    float halfStepOffset = 0f;
                    if (secondVolley)
                    {
                        halfStepOffset = 0.5f;
                    }

                    FireAcidFan(target, halfStepOffset);
                }
            }

            if (AttackTimer >= lastVolleyTick + FanRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Server-only. FanShots spit balls spread evenly across FanSpreadDegrees, centred on the player.
        void FireAcidFan(Player target, float stepOffset)
        {
            Vector2 orb = Orb;
            float centerAngle = (target.Center - orb).ToRotation();
            float spread = MathHelper.ToRadians(FanSpreadDegrees);
            float step = spread / (FanShots - 1);

            for (int i = 0; i < FanShots; i++)
            {
                float shotAngle = centerAngle - spread * 0.5f + step * (i + stepOffset);
                Vector2 velocity = shotAngle.ToRotationVector2() * FanSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), orb, velocity, ModContent.ProjectileType<DarkrootImpAcid>(), AcidSpitDamage, 0f, Main.myPlayer, DarkrootImpAcid.ModeSpit);
            }
        }

        void TickBileMortar(Player target)
        {
            int globCount = MortarGlobs;
            if (empowered)
            {
                globCount = MortarGlobsEmpowered;
            }

            int lastLobTick = MortarChargeTicks + (globCount - 1) * MortarLobInterval;

            NPC.velocity *= 0.9f;
            FaceTowards(target.Center.X);

            if (AttackTimer <= MortarChargeTicks)
            {
                // Rise a little while the orb swells — it lobs from above.
                NPC.velocity.Y -= 0.05f;

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item20 with { Pitch = -0.5f, Volume = 0.7f });
                }

                float progress = AttackTimer / (float)MortarChargeTicks;
                EmitOrbSwell(progress);
                return;
            }

            int ticksIntoLobs = AttackTimer - MortarChargeTicks;
            bool lobTick = ticksIntoLobs % MortarLobInterval == 0 && AttackTimer <= lastLobTick;

            if (lobTick)
            {
                PlaySound(SoundID.Item17);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int globIndex = ticksIntoLobs / MortarLobInterval;
                    LobBileGlob(target, MortarLandingOffsets[globIndex]);
                }
            }

            if (AttackTimer >= lastLobTick + MortarRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Server-only. Aims one glob at the floor under (player + offset along the demon→player line), re-reading the player
        // each lob so a moving target is tracked. Discrete solve for velocity-then-position integration with gravity g:
        //   after T ticks, dy = T·vy + g·T(T+1)/2  →  vy = (dy − g·T(T+1)/2) / T,  vx = dx / T.
        void LobBileGlob(Player target, float landingOffset)
        {
            float awayDirection = Math.Sign(target.Center.X - NPC.Center.X);
            if (awayDirection == 0f)
            {
                awayDirection = 1f;
            }

            float landingX = target.Center.X + landingOffset * awayDirection;
            float scanStartY = target.Bottom.Y - 32f;

            if (!TryFindFloor(landingX, scanStartY, out float floorY))
            {
                // No floor under that offset (a ledge edge): land on the player's own floor instead.
                landingX = target.Center.X;
                if (!TryFindFloor(landingX, scanStartY, out floorY))
                {
                    return;
                }
            }

            Vector2 orb = Orb;
            Vector2 landing = new Vector2(landingX, floorY - 8f);
            float flightTicks = MortarFlightTicks;
            float gravity = DarkrootImpAcid.GlobGravity;
            float gravityDrop = gravity * flightTicks * (flightTicks + 1f) * 0.5f;

            Vector2 velocity;
            velocity.X = (landing.X - orb.X) / flightTicks;
            velocity.Y = (landing.Y - orb.Y - gravityDrop) / flightTicks;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), orb, velocity, ModContent.ProjectileType<DarkrootImpAcid>(), BileGlobDamage, 0f, Main.myPlayer, DarkrootImpAcid.ModeGlob, AcidPoolDamage);
        }

        void TickSwoop(Player target)
        {
            int diveEnd = SwoopRearTicks + swoopDiveTicks;

            // Rear: pull back and up away from the player, screaming, shadowflame swirling off the body.
            if (AttackTimer <= SwoopRearTicks)
            {
                FaceTowards(target.Center.X);

                Vector2 away = (NPC.Center - target.Center).SafeNormalize(-Vector2.UnitY);
                Vector2 rearVelocity = away * 3.5f + new Vector2(0f, -1.2f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, rearVelocity, 0.1f);

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.DD2_WyvernScream with { Volume = 0.7f });
                }

                EmitSwoopRear();

                // Lock the dive on the last rear tick: through the player and SwoopOvershoot px past, so it can't stop short.
                if (AttackTimer == SwoopRearTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toTarget = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    Vector2 diveTarget = target.Center + toTarget * SwoopOvershoot;
                    float diveDistance = Vector2.Distance(NPC.Center, diveTarget);
                    int diveTicks = (int)Math.Ceiling(diveDistance / SwoopSpeed);

                    lockedVelocity = toTarget * SwoopSpeed;
                    swoopDiveTicks = Math.Clamp(diveTicks, SwoopMinDiveTicks, SwoopMaxDiveTicks);
                    NPC.netUpdate = true;
                }

                return;
            }

            // Dive: locked velocity. Hitting terrain ends it early (server decides; clients follow the synced timer).
            if (AttackTimer <= diveEnd)
            {
                if (AttackTimer == SwoopRearTicks + 1)
                {
                    PlaySound(SoundID.Item32 with { Volume = 0.9f, Pitch = -0.3f });
                }

                NPC.velocity = lockedVelocity;
                FaceTowards(NPC.Center.X + lockedVelocity.X);
                EmitSwoopTrail();

                // Skip the first dive tick: collideX/Y still describe the rear, which may have backed it into a wall.
                bool pastFirstDiveTick = AttackTimer > SwoopRearTicks + 1;
                bool hitTerrain = NPC.collideX || NPC.collideY;
                if (pastFirstDiveTick && hitTerrain && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AttackTimer = diveEnd;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Recovery: coasts to a stop — the window to punish it.
            NPC.velocity *= 0.92f;

            if (AttackTimer >= diveEnd + SwoopRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Face a world X. The art faces left, and vanilla flips the sprite when spriteDirection == 1, so spriteDirection = facing.
        void FaceTowards(float worldX)
        {
            if (worldX > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }

            NPC.spriteDirection = NPC.direction;
        }

        // First solid (or platform) tile at or below startY, scanning MortarFloorScanTiles down. floorY = that tile's top edge.
        static bool TryFindFloor(float worldX, float startY, out float floorY)
        {
            int tileX = (int)(worldX / 16f);
            int startTileY = (int)(startY / 16f);

            for (int tileY = startTileY; tileY < startTileY + MortarFloorScanTiles; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY, 5))
                {
                    break;
                }

                Tile tile = Main.tile[tileX, tileY];
                bool solidFloor = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

                if (solidFloor)
                {
                    floorY = tileY * 16f;
                    return true;
                }
            }

            floorY = 0f;
            return false;
        }

        void PlaySound(SoundStyle sound)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, NPC.Center);
            }
        }

        // Cursed Breath tell, Massacre's mouth ember in curse green: a small steady smoulder for the first 40%, then it
        // swells from 0.6x to 1.9x and doubles in density until the breath releases. Plum accents once it swells.
        void EmitMouthEmber(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 mouth = Mouth;
            float scale = 0.45f;
            int count = 1;

            if (progress >= 0.4f)
            {
                float swell = (progress - 0.4f) / 0.6f;
                scale = MathHelper.Lerp(0.6f, 1.9f, swell);
                count = 2;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = mouth + Main.rand.NextVector2Circular(3f, 3f);
                Dust ember = Dust.NewDustPerfect(spot, DustID.CursedTorch, Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                ember.noGravity = true;
            }

            if (progress >= 0.4f && Main.rand.NextBool(4))
            {
                PowerfulCurseBuildup.SpawnAccentDust(mouth - new Vector2(4f), 8, 8, Vector2.Zero, scale);
            }

            Lighting.AddLight(mouth, 0.15f * scale, 0.5f * scale, 0.1f * scale);
        }

        // Acid Fan tell: green motes drawn in from 40px to 6px around the orb as the charge completes.
        void EmitOrbGather(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 orb = Orb;
            float radius = MathHelper.Lerp(40f, 6f, progress);

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust mote = Dust.NewDustPerfect(orb + offset, DustID.CursedTorch, -offset * 0.08f, 40, default, MathHelper.Lerp(0.8f, 1.5f, progress));
                mote.noGravity = true;
            }

            Lighting.AddLight(orb, 0.1f, 0.4f * progress + 0.1f, 0.05f);
        }

        // Bile Mortar tell: the orb swells and bubbles, bubbles rising off it faster as the charge completes.
        void EmitOrbSwell(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 orb = Orb;
            float radius = MathHelper.Lerp(4f, 12f, progress);
            Vector2 spot = orb + Main.rand.NextVector2Circular(radius, radius);
            Vector2 rise = new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f) * (1f + progress));
            Dust bubble = Dust.NewDustPerfect(spot, DustID.GreenTorch, rise, 0, default, MathHelper.Lerp(0.9f, 1.8f, progress));
            bubble.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Dust glow = Dust.NewDustPerfect(orb + Main.rand.NextVector2Circular(radius, radius), DustID.CursedTorch, Vector2.Zero, 40, default, 1.2f + progress);
                glow.noGravity = true;
            }

            Lighting.AddLight(orb, 0.1f, 0.5f * progress + 0.1f, 0.05f);
        }

        void EmitSwoopRear()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                Dust shadow = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 80, default, 1.6f);
                shadow.noGravity = true;
                shadow.velocity = (shadow.position - NPC.Center).SafeNormalize(Vector2.Zero) * -1.5f;
            }
        }

        void EmitSwoopTrail()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 80, default, 1.8f);
                trail.noGravity = true;
                trail.velocity = -lockedVelocity * 0.15f;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (Diving)
            {
                NPC.frame.Y = DiveFrame * frameHeight;
                return;
            }

            // Telegraphs (and the breath itself) flap twice as fast — the sprite has no other pose to tell with.
            int flapRate = 1;
            if (IsTelegraphing || BreathReleasing)
            {
                flapRate = 2;
            }

            NPC.frameCounter += flapRate;

            if (NPC.frameCounter >= FrameTicks)
            {
                NPC.frameCounter = 0;
                int frame = NPC.frame.Y / frameHeight + 1;

                if (frame >= Main.npcFrameCount[NPC.type])
                {
                    frame = 0;
                }

                NPC.frame.Y = frame * frameHeight;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60, false); //-20 HP curse
            }

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<FracturingArmor>(), 60 * 60, false); //armor reduced on hit
                target.AddBuff(BuffID.CursedInferno, 3 * 60, false); // was Frostbite, which didn't fit a curse-fire demon
            }
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 1").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<RedTitanite>(), 1, 1, 2));
        }
    }
}
