using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Owl Father's companion owl. Spawned alongside him (see OwlFatherInvader.OnSpawn), wanders the
    /// battlefield on FloaterAI, alternates between distant dive-bombs, airborne molten-orb volleys,
    /// and tree-perched eye beams. It is killed by Owl Father when his health crosses 50% (see
    /// OwlFatherInvader's threshold check) — that kill is what triggers Owl Father's spectral form,
    /// not anything this class tracks on its own.
    /// </summary>
    public class OwlCompanion : ModNPC
    {
        // Art file is "Owl.png", not "OwlCompanion.png" — the class name and sprite filename never
        // matched, so autoload silently threw MissingResourceException at load. Point at the real file.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/Owl";

        private enum OwlState
        {
            Wander,
            DiveBombApproach,
            DiveBombWindup,
            DiveBombing,
            FlyingToPerch,
            PerchLanding,
            EyeBeamTelegraph,
            EyeBeamRecovery,
            MoltenOrbTelegraph,
            MoltenOrbVolley,
        }

        // ── Tuning (first pass — all NEEDS IN-GAME CALIBRATION) ─────────────────────
        private const float WanderTopSpeed = 4.5f;
        private const float WanderAccel = 0.09f;
        private const float WanderHoverMin = 140f;
        private const float WanderHoverMax = 320f;
        private const int IdleFlightMinTicks = 120;
        private const int IdleFlightMaxTicks = 180;
        private const int DiveBombChance = 35;
        private const int PerchChance = 30;
        private const int DiveBombCooldownTicks = 10 * 60;
        private const int DiveBombWindupTicks = 34;
        private const float DiveBombStartDistance = 410f;
        private const int DiveBombApproachMaxTicks = 240;
        private const int DiveBombMaxTicks = 70;
        private const float DiveBombStagingHorizontal = 320f;
        private const float DiveBombStagingHeight = 260f;
        private const float DiveBombExitHorizontal = 360f;
        private const float DiveBombExitHeight = 240f;
        // Molten Orb was the stronger ranged attack at 20 damage. Every phase-one owl attack now
        // uses that value, so neither the eye beam nor the committed dive is an accidental downgrade.
        private const int OwlAttackDamage = 20;
        private const int TreeSearchRadiusTiles = 45;
        private const float PerchHeightAbovePlayer = 96f;
        private const int PerchLandingTicks = 12;
        private const int EyeBeamTelegraphTicks = 90; // per design: "90 tick growing glowing telegraph"
        private const int EyeBeamRecoveryTicks = 30;
        private const float EyeBeamSpeed = 9f;
        private const float EyeBeamSeparation = 6f; // twin shots, one per eye
        private const int MoltenOrbTelegraphTicks = 45;
        private const int MoltenOrbShotInterval = 30;
        private const int MoltenOrbVolleyTicks = 2 * MoltenOrbShotInterval + 1;
        private const int MoltenOrbCooldownTicks = 400;

        private FloaterAI.FloaterState _floaterState = new();
        private readonly NPC _flightDestination = new NPC { active = true, width = 32, height = 32 };
        private OwlState _state = OwlState.Wander;
        private int _stateTimer;
        private int _decisionTimer;
        private int _diveBombCooldown;
        private int _moltenOrbCooldown;
        private Vector2 _diveBombTargetVelocity;
        private Vector2 _diveBombStartPoint;
        private Vector2 _diveBombControlPoint;
        private Vector2 _diveBombExitPoint;
        private Vector2 _diveStagingPoint;
        private Vector2 _perchPoint;
        private Point _perchTile;
        private int _eyeBeamLookFrame = FramePerchForward;
        private int _syncTimer;

        private bool IsPerched => _state == OwlState.PerchLanding
            || _state == OwlState.EyeBeamTelegraph || _state == OwlState.EyeBeamRecovery;

        /// <summary>Owl Father's NPC index, set at spawn (ai[0]). Used only to notice if he despawns
        /// by some OTHER means (e.g. the whole encounter resetting) — the normal 50%-health kill is
        /// driven entirely from Owl Father's side, not read from here.</summary>
        private int OwnerNpcIndex => (int)NPC.ai[0];

        public override void SetStaticDefaults()
        {
            // First-pass frame ranges from the raw sprite sheet — a perched/blinking idle loop, a
            // wing-flap cycle for wandering flight, and a swept-back glide pose for the dive. Confirm
            // against the actual sheet in-game and adjust the ranges below if they're off.
            Main.npcFrameCount[Type] = 13;
        }

        // Confirmed against the actual 13-frame sheet (contact-sheet render):
        //   0,1,3,5,6,7 — forward-facing perch/blink idle loop (no strong left/right asymmetry)
        //   2           — head turned to screen-RIGHT (single visible eye, tufts left) — aim-right art
        //   4           — head turned to screen-LEFT (mirror of 2) — aim-left art
        //   8           — single steeper dive/bank pose, distinct from the flight loop below
        //   9,10,11,12  — 4-frame wingbeat flight loop
        private static readonly int[] IdleBlinkFrames = { 0, 1, 3, 5, 6, 7 };
        private static readonly int[] FlyFrames = { 9, 10, 11, 12 };
        private const int DiveFrame = 8;
        private const int FramePerchForward = 0;
        private const int FrameLookRight = 2;
        private const int FrameLookLeft = 4;

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.lifeMax = 1; // not a combat target — see class doc; killed outright by Owl Father
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.aiStyle = -1;
            NPC.damage = 0; // toggled on only during the DiveBombing window below
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 0f;
            NPC.npcSlots = 0f; // doesn't count against the boss's own slot budget
            NPC.friendly = false;
            NPC.lavaImmune = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _diveBombCooldown = DiveBombCooldownTicks / 2; // don't dive immediately on spawn
            if (Main.netMode != NetmodeID.MultiplayerClient)
                BeginIdleFlight();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_state);
            writer.Write(_stateTimer);
            writer.Write(_decisionTimer);
            writer.Write(_diveBombCooldown);
            writer.Write(_moltenOrbCooldown);
            writer.WriteVector2(_diveBombTargetVelocity);
            writer.WriteVector2(_diveBombStartPoint);
            writer.WriteVector2(_diveBombControlPoint);
            writer.WriteVector2(_diveBombExitPoint);
            writer.WriteVector2(_diveStagingPoint);
            writer.WriteVector2(_perchPoint);
            writer.Write(_perchTile.X);
            writer.Write(_perchTile.Y);
            writer.Write(_eyeBeamLookFrame);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _state = (OwlState)reader.ReadByte();
            _stateTimer = reader.ReadInt32();
            _decisionTimer = reader.ReadInt32();
            _diveBombCooldown = reader.ReadInt32();
            _moltenOrbCooldown = reader.ReadInt32();
            _diveBombTargetVelocity = reader.ReadVector2();
            _diveBombStartPoint = reader.ReadVector2();
            _diveBombControlPoint = reader.ReadVector2();
            _diveBombExitPoint = reader.ReadVector2();
            _diveStagingPoint = reader.ReadVector2();
            _perchPoint = reader.ReadVector2();
            _perchTile = new Point(reader.ReadInt32(), reader.ReadInt32());
            _eyeBeamLookFrame = reader.ReadInt32();
        }

        public override void AI()
        {
            // Owner despawned/died by some means other than the 50%-health trigger (which kills this
            // NPC directly and never reaches this check) — nothing left to escort, so just leave too.
            if (OwnerNpcIndex < 0 || OwnerNpcIndex >= Main.maxNPCs || !Main.npc[OwnerNpcIndex].active
                || Main.npc[OwnerNpcIndex].ModNPC is not OwlFatherInvader)
            {
                // The owner's spawn packet can arrive later than this companion's packet.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
                return;
            }

            NPC.noGravity = true;
            NPC.damage = _state == OwlState.DiveBombing ? OwlAttackDamage : 0;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Decisions, navigation and projectile creation belong to the server. Clients
                // extrapolate flight, but pin a perched owl and animate its synced telegraph clock.
                if (IsPerched)
                    HoldPerch();
                if (_state == OwlState.DiveBombWindup)
                    SpawnDiveTelegraphDust();
                else if (_state == OwlState.MoltenOrbTelegraph)
                    SpawnMoltenOrbTelegraphDust();
                else if (_state == OwlState.EyeBeamTelegraph)
                    SpawnEyeBeamTelegraphDust();
                else if (_state == OwlState.DiveBombing)
                    SpawnDiveTrail();
                _stateTimer = Math.Max(0, _stateTimer - 1);
                UpdateFrame();
                return;
            }

            Player target = NPC.HasValidTarget && _state != OwlState.Wander
                ? Main.player[NPC.target] : FindClosestPlayer();
            if (target != null)
                NPC.target = target.whoAmI;
            else if (_state != OwlState.Wander)
                BeginIdleFlight();

            if ((IsPerched || _state == OwlState.FlyingToPerch) && !IsPerchValid(target))
                BeginIdleFlight();

            if (_diveBombCooldown > 0)
            {
                _diveBombCooldown--;
            }
            if (_moltenOrbCooldown > 0)
                _moltenOrbCooldown--;

            switch (_state)
            {
                case OwlState.Wander:
                    TickWander(target);
                    break;
                case OwlState.DiveBombApproach:
                    TickDiveBombApproach(target);
                    break;
                case OwlState.DiveBombWindup:
                    TickDiveBombWindup(target);
                    break;
                case OwlState.DiveBombing:
                    TickDiveBombing();
                    break;
                case OwlState.FlyingToPerch:
                    TickFlyingToPerch(target);
                    break;
                case OwlState.PerchLanding:
                    TickPerchLanding();
                    break;
                case OwlState.EyeBeamTelegraph:
                    TickEyeBeamTelegraph(target);
                    break;
                case OwlState.EyeBeamRecovery:
                    TickEyeBeamRecovery();
                    break;
                case OwlState.MoltenOrbTelegraph:
                case OwlState.MoltenOrbVolley:
                    TickMoltenOrbAttack(target);
                    break;
            }

            if (IsPerched)
                HoldPerch();
            if (++_syncTimer >= 15)
            {
                _syncTimer = 0;
                NPC.netUpdate = true;
            }
            UpdateFrame();
        }

        private Player FindClosestPlayer()
        {
            Player closest = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                float distSq = Vector2.DistanceSquared(player.Center, NPC.Center);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    closest = player;
                }
            }
            return closest;
        }

        // ── Wander ────────────────────────────────────────────────────────────────
        private void ChangeState(OwlState state, int ticks)
        {
            _state = state;
            _stateTimer = ticks;
            NPC.netUpdate = true;
        }

        private void BeginIdleFlight(bool forceTakeoff = true)
        {
            NPC.damage = 0;
            NPC.rotation = 0f;
            ChangeState(OwlState.Wander, 0);
            _decisionTimer = Main.rand.Next(IdleFlightMinTicks, IdleFlightMaxTicks + 1);
            _floaterState = new FloaterAI.FloaterState();
            // Ordinary state changes take off instead of carrying a downward velocity into the
            // ground. A completed dive deliberately opts out so it can fly through the player
            // before easing back into its idle circuit.
            if (forceTakeoff)
                NPC.velocity.Y = Math.Min(NPC.velocity.Y, -2f);
        }

        private void FlyIdle(Player target)
        {
            FloaterAI.Run(NPC, target, _floaterState,
                topSpeed: WanderTopSpeed, accel: WanderAccel,
                hoverMin: WanderHoverMin, hoverMax: WanderHoverMax);
            if (target != null && NPC.Center.Y > target.Top.Y - 100f)
                NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -WanderTopSpeed, 0.18f);
            if (NPC.collideY && NPC.velocity.Y >= 0f)
                NPC.velocity.Y = -WanderTopSpeed;
        }

        private void TickWander(Player target)
        {
            FlyIdle(target);

            if (target == null)
            {
                return;
            }

            if (--_decisionTimer > 0)
            {
                return;
            }
            _decisionTimer = Main.rand.Next(IdleFlightMinTicks, IdleFlightMaxTicks + 1);

            int roll = Main.rand.Next(100);
            if (_diveBombCooldown <= 0 && roll < DiveBombChance)
            {
                int side = NPC.Center.X < target.Center.X ? -1 : 1;
                _diveStagingPoint = target.Center
                    + new Vector2(side * DiveBombStagingHorizontal, -DiveBombStagingHeight);
                ChangeState(OwlState.DiveBombApproach, DiveBombApproachMaxTicks);
            }
            else if (roll >= DiveBombChance && roll < DiveBombChance + PerchChance)
            {
                if (TryFindTreePerch(target, out Vector2 treePoint, out Point treeTile))
                {
                    _perchPoint = treePoint;
                    _perchTile = treeTile;
                    ChangeState(OwlState.FlyingToPerch, 0);
                }
            }
            else if (_moltenOrbCooldown <= 0
                && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
                ChangeState(OwlState.MoltenOrbTelegraph, MoltenOrbTelegraphTicks);
            // else: keep wandering this cycle.
        }

        /// <summary>Nearest reachable tree branch above the player. Scan only at a perch decision.</summary>
        private bool TryFindTreePerch(Player target, out Vector2 perchPoint, out Point perchTile)
        {
            perchPoint = Vector2.Zero;
            perchTile = Point.Zero;
            int centerTileX = (int)(target.Center.X / 16f);
            int centerTileY = (int)(target.Top.Y / 16f);
            float bestDistSq = float.MaxValue;
            bool found = false;

            int minX = System.Math.Max(1, centerTileX - TreeSearchRadiusTiles);
            int maxX = System.Math.Min(Main.maxTilesX - 2, centerTileX + TreeSearchRadiusTiles);
            int minY = System.Math.Max(1, centerTileY - TreeSearchRadiusTiles);
            int maxY = System.Math.Min(Main.maxTilesY - 2,
                (int)((target.Top.Y - PerchHeightAbovePlayer) / 16f));

            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                for (int tileY = minY; tileY <= maxY; tileY++)
                {
                    Tile tile = Main.tile[tileX, tileY];
                    if (!IsTreePerchTile(tile))
                    {
                        continue;
                    }

                    Vector2 candidate = new Vector2(tileX * 16f + 8f, tileY * 16f - NPC.height * 0.5f);
                    if (Collision.SolidCollision(candidate - NPC.Size * 0.5f, NPC.width, NPC.height)
                        || !Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
                            candidate - NPC.Size * 0.5f, NPC.width, NPC.height)
                        || !Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1))
                        continue;
                    float distSq = Vector2.DistanceSquared(NPC.Center, candidate);
                    if (distSq >= bestDistSq)
                    {
                        continue;
                    }

                    bestDistSq = distSq;
                    perchPoint = candidate;
                    perchTile = new Point(tileX, tileY);
                    found = true;
                }
            }

            return found;
        }

        private static bool IsTreePerchTile(Tile tile)
        {
            if (!tile.HasTile || tile.IsActuated || !TileID.Sets.IsATreeTrunk[tile.TileType])
                return false;
            // Same branch/crown frame groups used by vanilla NPC.FindTreeBranch.
            int column = tile.TileFrameX / 22;
            int group = tile.TileFrameY / 66;
            return (column == 3 && (group == 0 || group == 3))
                || (column == 4 && (group == 1 || group == 3))
                || (column == 2 && group == 3);
        }

        private bool IsPerchValid(Player target)
        {
            if (target == null || !WorldGen.InWorld(_perchTile.X, _perchTile.Y, 1)
                || _perchPoint.Y + NPC.height * 0.5f > target.Top.Y - PerchHeightAbovePlayer
                || Math.Abs(_perchPoint.X - target.Center.X) > TreeSearchRadiusTiles * 16f)
                return false;
            Tile tree = Main.tile[_perchTile.X, _perchTile.Y];
            return IsTreePerchTile(tree)
                && !Collision.SolidCollision(_perchPoint - NPC.Size * 0.5f, NPC.width, NPC.height);
        }

        private void HoldPerch()
        {
            NPC.Center = _perchPoint;
            NPC.velocity = Vector2.Zero;
            NPC.netOffset = Vector2.Zero;
            NPC.rotation = 0f;
        }

        // ── Dive bomb ─────────────────────────────────────────────────────────────
        private void TickDiveBombApproach(Player target)
        {
            if (target == null || --_stateTimer <= 0)
            {
                BeginIdleFlight();
                return;
            }
            int side = NPC.Center.X < target.Center.X ? -1 : 1;
            _diveStagingPoint = target.Center
                + new Vector2(side * DiveBombStagingHorizontal, -DiveBombStagingHeight);
            _flightDestination.Center = _diveStagingPoint;
            FloaterAI.Run(NPC, _flightDestination, _floaterState,
                topSpeed: WanderTopSpeed + 1.5f, accel: 0.14f,
                hoverMin: 0f, hoverMax: 0f, navRadius: TreeSearchRadiusTiles,
                bobAmplitude: 0f, driftAmount: 0f);
            if (Vector2.DistanceSquared(NPC.Center, _diveStagingPoint) <= 28f * 28f
                && CanStartDive(target))
            {
                NPC.velocity = Vector2.Zero;
                ChangeState(OwlState.DiveBombWindup, DiveBombWindupTicks);
            }
        }

        private bool CanStartDive(Player target) => target != null
            && NPC.Distance(target.Center) >= DiveBombStartDistance
            && NPC.Center.Y <= target.Top.Y - 160f
            && Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
                target.position, target.width, target.height);

        private void TickDiveBombWindup(Player target)
        {
            if (!CanStartDive(target))
            {
                ChangeState(OwlState.DiveBombApproach, DiveBombApproachMaxTicks);
                return;
            }

            // Hold the distant staging point. Never chase into point-blank range during the tell.
            NPC.velocity = Vector2.Lerp(NPC.velocity, (_diveStagingPoint - NPC.Center) * 0.05f, 0.2f);
            NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
            SpawnDiveTelegraphDust();

            if (--_stateTimer > 0)
            {
                return;
            }

            // Build one broad bowl-shaped curve from the high staging point, through the predicted
            // player position, and back up on the opposite side. Solving the quadratic control point
            // from the desired midpoint guarantees the curve passes through the dodge point at t=.5.
            // Cross the upper body rather than the feet so a standing player's floor does not
            // clip the owl out of its recovery arc. The 32px owl still overlaps the player here.
            Vector2 impactPoint = target.Center + target.velocity * 8f - new Vector2(0f, 8f);
            float stagingSide = Math.Sign(_diveStagingPoint.X - impactPoint.X);
            if (stagingSide == 0f)
                stagingSide = NPC.Center.X < impactPoint.X ? -1f : 1f;

            _diveBombStartPoint = NPC.Center;
            _diveBombExitPoint = impactPoint
                + new Vector2(-stagingSide * DiveBombExitHorizontal, -DiveBombExitHeight);
            _diveBombControlPoint = impactPoint * 2f
                - (_diveBombStartPoint + _diveBombExitPoint) * 0.5f;
            _diveBombTargetVelocity = EvaluateDiveCurve(1f / DiveBombMaxTicks) - NPC.Center;
            NPC.velocity = _diveBombTargetVelocity;
            NPC.damage = OwlAttackDamage;
            // The launch frame already advances to the first curve sample.
            ChangeState(OwlState.DiveBombing, DiveBombMaxTicks - 1);
            SoundEngine.PlaySound(SoundID.Zombie112 with
            {
                Volume = 0.78f,
                PitchVariance = 0.08f
            }, NPC.Center);
            NPC.netUpdate = true;
        }

        private void TickDiveBombing()
        {
            int elapsedTicks = DiveBombMaxTicks - _stateTimer;
            float nextProgress = MathHelper.Clamp(
                (elapsedTicks + 1f) / DiveBombMaxTicks, 0f, 1f);
            Vector2 nextPoint = EvaluateDiveCurve(nextProgress);
            _diveBombTargetVelocity = nextPoint - NPC.Center;
            NPC.velocity = _diveBombTargetVelocity;
            NPC.direction = NPC.velocity.X < 0f ? -1 : 1;
            SpawnDiveTrail();

            // A floor graze around the bowl's lowest point should not cancel the upswing. Terraria
            // zeros the blocked downward component, then the next curve sample carries the owl up.
            bool blockingCollision = NPC.collideX || (NPC.collideY && nextProgress < 0.48f);
            if (--_stateTimer <= 0 || blockingCollision
                || (nextProgress >= 0.9f
                    && Vector2.DistanceSquared(NPC.Center, _diveBombExitPoint) <= 28f * 28f))
            {
                EndDiveBomb();
            }
        }

        private Vector2 EvaluateDiveCurve(float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * _diveBombStartPoint
                + 2f * inverse * progress * _diveBombControlPoint
                + progress * progress * _diveBombExitPoint;
        }

        private void EndDiveBomb()
        {
            NPC.damage = 0;
            NPC.rotation = 0f;
            _diveBombCooldown = DiveBombCooldownTicks;
            BeginIdleFlight(forceTakeoff: false);
        }

        private void SpawnDiveTelegraphDust()
        {
            if (Main.dedServ)
                return;
            float progress = 1f - _stateTimer / (float)DiveBombWindupTicks;
            if (Main.rand.NextFloat() < 0.2f + progress * 0.5f)
                Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 60, default,
                    MathHelper.Lerp(0.7f, 1.3f, progress)).noGravity = true;
        }

        private void SpawnDiveTrail()
        {
            if (!Main.dedServ)
                Dust.NewDustPerfect(NPC.Center, DustID.Torch,
                    -NPC.velocity * 0.1f + Main.rand.NextVector2Circular(1f, 1f), 60, default,
                    Main.rand.NextFloat(1.1f, 1.6f)).noGravity = true;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Contact damage only during the committed sweep — NPC.damage is 0 the rest of the time,
            // so vanilla's own automatic contact-damage check simply can't fire outside this state.
            return _state == OwlState.DiveBombing;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurt)
        {
            // Only ever fires from the CanHitPlayer window above, so this is always a dive-bomb hit.
            // BuffID 148 — "Rabies" is its internal name, but it's vanilla's feral-bite debuff.
            target.AddBuff(BuffID.Rabies, 5 * 60);
        }

        // ── Perch + eye-beam ──────────────────────────────────────────────────────
        private void TickFlyingToPerch(Player target)
        {
            Vector2 toPerch = _perchPoint - NPC.Center;
            NPC.direction = toPerch.X < 0f ? -1 : 1;

            if (toPerch.Length() > 12f)
            {
                Vector2 desired = toPerch.SafeNormalize(Vector2.Zero) * WanderTopSpeed;
                NPC.velocity = Vector2.Lerp(NPC.velocity, desired, WanderAccel);

                // Give up and go back to wandering if the perch point turns out to be unreachable
                // (e.g. tiles changed under it) rather than hovering at it forever.
                if (++_stateTimer > 300)
                {
                    BeginIdleFlight();
                }
                return;
            }

            NPC.velocity = Vector2.Zero;
            NPC.Center = _perchPoint;
            ChangeState(OwlState.PerchLanding, PerchLandingTicks);
        }

        private void TickPerchLanding()
        {
            HoldPerch();
            if (--_stateTimer <= 0)
            {
                ChangeState(OwlState.EyeBeamTelegraph, EyeBeamTelegraphTicks);
            }
        }

        // Flight frames 9-12 share the same visible profile eye at texture pixel (33, 17).
        private Vector2 EyePosition => NPC.Center + new Vector2(
            NPC.direction * (IsPerched ? 6f : 13f), (IsPerched ? -10f : -3f) + NPC.gfxOffY);

        private void TickEyeBeamTelegraph(Player target)
        {
            NPC.velocity = Vector2.Zero;

            if (target != null)
            {
                Vector2 aim = target.Center - NPC.Center;
                NPC.direction = aim.X < 0f ? -1 : 1;

                // Only two turned-head frames exist (screen-left / screen-right); a mostly-vertical
                // aim (target well above or below) has no dedicated art, so it just holds the
                // symmetric forward-facing pose per the authored sheet.
                if (System.Math.Abs(aim.X) < System.Math.Abs(aim.Y) * 0.5f)
                {
                    _eyeBeamLookFrame = FramePerchForward;
                }
                else
                {
                    _eyeBeamLookFrame = aim.X > 0f ? FrameLookRight : FrameLookLeft;
                }
            }

            SpawnEyeBeamTelegraphDust();

            if (--_stateTimer > 0)
                return;

            if (target != null && Collision.CanHitLine(EyePosition, 1, 1, target.Center, 1, 1))
                FireEyeBeams(target);
            ChangeState(OwlState.EyeBeamRecovery, EyeBeamRecoveryTicks);
        }

        private void SpawnEyeBeamTelegraphDust()
        {
            if (Main.dedServ)
                return;
            float progress = 1f - _stateTimer / (float)EyeBeamTelegraphTicks;
            Lighting.AddLight(EyePosition, 0.9f * progress, 0.7f * progress, 0.1f * progress);
            if (Main.rand.NextFloat() < 0.15f + progress * 0.5f)
            {
                Dust glow = Dust.NewDustPerfect(EyePosition, DustID.GoldFlame,
                    Vector2.Zero, 100, default, MathHelper.Lerp(0.6f, 1.8f, progress));
                glow.noGravity = true;
                glow.velocity *= 0.2f;
            }

        }

        private void FireEyeBeams(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || target == null)
            {
                return;
            }

            Vector2 aim = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 sideOffset = new Vector2(-aim.Y, aim.X) * EyeBeamSeparation;

            foreach (Vector2 origin in new[] { EyePosition - sideOffset, EyePosition + sideOffset })
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, aim * EyeBeamSpeed,
                    ModContent.ProjectileType<OwlEyeFlame>(), OwlAttackDamage, 1f, Main.myPlayer);
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = 0.3f }, NPC.Center);
        }

        private void TickEyeBeamRecovery()
        {
            if (--_stateTimer > 0)
            {
                return;
            }
            BeginIdleFlight();
        }

        // ── Airborne molten-orb volley ───────────────────────────────────────────
        private void TickMoltenOrbAttack(Player target)
        {
            FlyIdle(target);
            if (target == null)
            {
                BeginIdleFlight();
                return;
            }
            if (_state == OwlState.MoltenOrbTelegraph)
            {
                SpawnMoltenOrbTelegraphDust();
                if (--_stateTimer <= 0)
                {
                    _moltenOrbCooldown = MoltenOrbCooldownTicks;
                    ChangeState(OwlState.MoltenOrbVolley, MoltenOrbVolleyTicks);
                }
                return;
            }

            int elapsed = MoltenOrbVolleyTicks - _stateTimer;
            if (elapsed % MoltenOrbShotInterval == 0
                && Collision.CanHitLine(EyePosition, 1, 1, target.Center, 1, 1))
            {
                int shot = elapsed / MoltenOrbShotInterval;
                Vector2 origin = EyePosition + new Vector2(0f, -8f);
                Vector2 velocity = new Vector2((shot - 1) * 1.6f, -6f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity,
                    ModContent.ProjectileType<EnemyGreatFireAxeFireball>(), OwlAttackDamage, 2f, Main.myPlayer,
                    ai0: 0f, ai1: -15f, ai2: NPC.whoAmI + 1f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, PitchVariance = 0.2f }, origin);
            }
            if (--_stateTimer <= 0)
                BeginIdleFlight();
        }

        private void SpawnMoltenOrbTelegraphDust()
        {
            if (Main.dedServ)
                return;
            float progress = MathHelper.Clamp(1f - _stateTimer / (float)MoltenOrbTelegraphTicks, 0f, 1f);
            for (int mote = 0; mote < 2; mote++)
            {
                Vector2 origin = EyePosition + Main.rand.NextVector2Circular(0.6f, 0.4f);
                Dust dust = Dust.NewDustPerfect(origin, DustID.YellowTorch,
                    new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), -1.2f - progress * 1.2f),
                    60, Color.Yellow, MathHelper.Lerp(0.6f, 1.0f, progress));
                dust.noGravity = true;
            }
            Lighting.AddLight(EyePosition, 0.3f + progress * 0.6f, 0.2f + progress * 0.5f, 0.04f);
        }

        // ── Frame animation ───────────────────────────────────────────────────────
        // Owl.png is a 40x520 strip (13 frames of 40x40) — the sprite frame is taller than the
        // 32x32 hitbox (NPC.width/height), so the frame math below uses this, not NPC.height.
        private const int SpriteFrameSize = 40;

        private void UpdateFrame()
        {
            // aiStyle -1 means nothing sets these automatically — derive facing from actual
            // horizontal motion for the states that don't already aim NPC.direction themselves
            // (dive/eye-beam set it explicitly against the target instead, and are left alone here
            // since their velocity is momentarily zero or dive-locked, not a reliable direction cue).
            if (!IsPerched && _state != OwlState.DiveBombWindup && System.Math.Abs(NPC.velocity.X) > 0.05f)
            {
                NPC.direction = NPC.velocity.X < 0f ? -1 : 1;
            }
            NPC.rotation = _state == OwlState.DiveBombing
                ? _diveBombTargetVelocity.ToRotation() + (NPC.direction < 0 ? MathHelper.Pi : 0f)
                : 0f;
            // This sheet faces right; vanilla flips NPC sprites when spriteDirection == 1.
            NPC.spriteDirection = -NPC.direction;

            if (_state == OwlState.EyeBeamTelegraph || _state == OwlState.EyeBeamRecovery)
            {
                // The turned-head frames are pre-authored per screen direction, not a symmetric pose
                // meant to be mirrored — force no-flip so FrameLookRight/FrameLookLeft draw as-is.
                NPC.spriteDirection = -1;
                NPC.frame.Y = _eyeBeamLookFrame * SpriteFrameSize;
                return;
            }

            if (_state == OwlState.DiveBombing)
            {
                NPC.frame.Y = DiveFrame * SpriteFrameSize;
                return;
            }

            int[] frames = IsPerched ? IdleBlinkFrames : FlyFrames;
            if (IsPerched)
                NPC.spriteDirection = -1;
            if (Array.IndexOf(frames, NPC.frame.Y / SpriteFrameSize) < 0)
            {
                NPC.frame.Y = frames[0] * SpriteFrameSize;
                NPC.frameCounter = 0;
            }
            NPC.frameCounter++;
            if (NPC.frameCounter >= 6)
            {
                NPC.frameCounter = 0;
                int currentFrame = (int)(NPC.frame.Y / SpriteFrameSize);
                int currentIndex = System.Array.IndexOf(frames, currentFrame);
                int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % frames.Length;
                NPC.frame.Y = frames[nextIndex] * SpriteFrameSize;
            }
        }

        // Killed via Owl Father's NPC.StrikeInstantKill() call at the 50%-health threshold (see
        // OwlFatherInvader) — that routes through the normal StrikeNPC/checkDead pipeline, so OnKill
        // fires exactly like any other death regardless of dontTakeDamage above.
        public override void OnKill()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 150; i++)
            {
                float angle = MathHelper.TwoPi * i / 150f + Main.rand.NextFloat(-0.025f, 0.025f);
                Dust blood = Dust.NewDustPerfect(NPC.Center, DustID.Blood,
                    angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 5.5f), 0, default,
                    Main.rand.NextFloat(1f, 1.8f));
                blood.noGravity = true;
            }
        }
    }
}
