using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace tsorcRevamp.NPCs.AI
{
    /// <summary>
    /// Movement-only flight helper for any NPC that wants to take off, hover, strafe, and dive.
    /// Pure AI — has no opinion on how the NPC is drawn.
    ///   • Player-puppet NPCs (InvaderNPC, EnemySpriteRenderer users) read
    ///     <see cref="WingsActiveThisTick"/> to drive vanilla wing flap animation.
    ///   • Custom-sprite NPCs (TibianValkyrie, etc.) read <see cref="WingAnimPhase"/>
    ///     to advance their own wing-frame strip via EnemyWingsDrawLayer.
    ///
    /// Calling pattern (from NPC.AI()):
    ///   _flight ??= new EnemyFlightController(MyFlightConfig);
    ///   _flight.Tick(NPC, Main.player[NPC.target]);
    ///   if (_flight.IsAirborne) { /* skip FighterAI, fly */ } else { /* normal ground AI */ }
    ///
    /// External requests:
    ///   _flight.RequestTakeoff(reason);   — switches Grounded → TakeOff if cooldown ready
    ///   _flight.RequestDive(targetPos);   — switches Hover/Strafe → DiveAttack
    ///   _flight.RequestLand();            — switches any aerial mode → Land
    /// </summary>
    public class EnemyFlightController
    {
        public EnemyFlightConfig Config;
        public FlightMode Mode { get; private set; } = FlightMode.Grounded;
        public int ModeTimer { get; private set; }
        public int FlightTicksRemaining { get; private set; }
        public int CooldownRemaining { get; private set; }
        /// <summary>Ticks of low/zero movement while airborne.  Triggers ceiling-clearance
        /// scan and eventually a forced land if we can't unstick.</summary>
        public int StuckTicks { get; private set; }
        /// <summary>+1 / -1 / 0 horizontal scan direction when seeking ceiling clearance.</summary>
        private int _clearanceSeekDir;

        /// <summary>Current dive/hover/strafe target in world coordinates.</summary>
        public Vector2 TargetWaypoint { get; private set; }

        /// <summary>True when the wings should be visibly flapping this tick.
        /// False during glide / strafe / dive-fold.  Drives flap-and-glide cadence.</summary>
        public bool WingsActiveThisTick { get; private set; }

        /// <summary>0..1 phase of the wing flap cycle.  Only advances when WingsActiveThisTick.
        /// Custom-sprite renderers sample this to pick a wing strip frame.</summary>
        public float WingAnimPhase { get; private set; }

        /// <summary>True for any non-Grounded mode.</summary>
        public bool IsAirborne => Mode != FlightMode.Grounded;

        /// <summary>True for DiveAttack mode — the NPC AI may want to spawn a hitbox.</summary>
        public bool IsDiving => Mode == FlightMode.DiveAttack;

        public EnemyFlightController(EnemyFlightConfig config)
        {
            Config = config;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public requests
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Request takeoff.  Ignored if already airborne or cooldown not ready.</summary>
        public bool RequestTakeoff()
        {
            if (Mode != FlightMode.Grounded || CooldownRemaining > 0) return false;
            EnterMode(FlightMode.TakeOff, Config.TakeOffTicks);
            return true;
        }

        /// <summary>Switch to dive toward an explicit point (e.g. player.Center).</summary>
        public bool RequestDive(Vector2 worldTarget)
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return false;
            TargetWaypoint = worldTarget;
            EnterMode(FlightMode.DiveAttack, Config.DiveAttackTicks);
            return true;
        }

        /// <summary>Switch to a horizontal strafing pass at the current altitude.</summary>
        public bool RequestStrafe(Vector2 worldTarget)
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return false;
            TargetWaypoint = worldTarget;
            EnterMode(FlightMode.Strafe, Config.StrafeTicks);
            return true;
        }

        /// <summary>Begin landing sequence.</summary>
        public void RequestLand()
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return;
            EnterMode(FlightMode.Land, Config.LandTicks);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Per-tick update
        // ─────────────────────────────────────────────────────────────────────

        public void Tick(NPC npc, Player target)
        {
            if (CooldownRemaining > 0 && Mode == FlightMode.Grounded) CooldownRemaining--;
            if (ModeTimer > 0) ModeTimer--;

            // ── Stuck detection (airborne only) ──────────────────────────────────
            // Accumulate ticks when barely moving while airborne — either pinned against
            // a ceiling, jammed in a cliff edge, or pushed flat against a wall.
            // 60 ticks -> start ceiling-clearance scan; 180 ticks -> force land.
            if (IsAirborne && Mode != FlightMode.Land && Mode != FlightMode.TakeOff)
            {
                if (npc.velocity.LengthSquared() < 0.6f * 0.6f)
                    StuckTicks++;
                else
                    StuckTicks = Math.Max(0, StuckTicks - 2);

                if (StuckTicks > 180)
                {
                    EnterMode(FlightMode.Land, Config.LandTicks);
                    StuckTicks = 0;
                }
            }
            else
            {
                StuckTicks = 0;
            }

            switch (Mode)
            {
                case FlightMode.Grounded:
                    npc.noGravity = false;
                    WingsActiveThisTick = false;
                    break;

                case FlightMode.TakeOff:
                    TickTakeOff(npc, target);
                    break;

                case FlightMode.Hover:
                    TickHover(npc, target);
                    break;

                case FlightMode.Strafe:
                    TickStrafe(npc, target);
                    break;

                case FlightMode.DiveAttack:
                    TickDive(npc, target);
                    break;

                case FlightMode.Land:
                    TickLand(npc, target);
                    break;
            }

            // Decrement total airborne budget; force landing if it runs out.
            if (IsAirborne && Mode != FlightMode.Land)
            {
                FlightTicksRemaining--;
                if (FlightTicksRemaining <= 0) EnterMode(FlightMode.Land, Config.LandTicks);
            }

            // Advance wing flap phase only while flapping (glide freezes mid-cycle).
            if (WingsActiveThisTick)
            {
                WingAnimPhase += Config.WingFlapSpeed;
                if (WingAnimPhase >= 1f) WingAnimPhase -= 1f;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mode handlers
        // ─────────────────────────────────────────────────────────────────────

        private void TickTakeOff(NPC npc, Player target)
        {
            npc.noGravity = true;
            // If head is blocked by a ceiling, suppress climb and slide sideways toward open air.
            if (HeadIsBlocked(npc))
            {
                npc.velocity.Y = Math.Max(npc.velocity.Y, 0.5f); // gentle drop away from ceiling
                int seek = SeekCeilingClearanceDir(npc);
                if (seek != 0)
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, seek * Config.HoverTopSpeed, 0.18f);
                WingsActiveThisTick = true; // wings keep working even while pinned
            }
            else
            {
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -Config.TakeOffSpeed, 0.20f);
                npc.velocity.X *= 0.85f;
                WingsActiveThisTick = true;
            }

            if (ModeTimer <= 0 || ReachedHoverAltitude(npc))
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
        }

        private void TickHover(NPC npc, Player target)
        {
            npc.noGravity = true;
            // Hover above the player at HoverAltitude, with a side bias so we don't sit
            // directly overhead (gives a more menacing "circle the prey" feel).
            float sideOffset = npc.Center.X < target.Center.X ? -Config.HoverSideOffset : Config.HoverSideOffset;
            Vector2 desired = new Vector2(target.Center.X + sideOffset, target.Center.Y - Config.HoverAltitude);

            // ── Ceiling-clearance routing ─────────────────────────────────────
            // If desired altitude or the path up is blocked, shift horizontally to find
            // a column with overhead clearance, then climb there.
            if (HeadIsBlocked(npc) || StuckTicks > 60)
            {
                int seek = _clearanceSeekDir != 0 ? _clearanceSeekDir : SeekCeilingClearanceDir(npc);
                if (seek == 0) seek = npc.direction; // fall back to current facing
                _clearanceSeekDir = seek;
                desired = new Vector2(npc.Center.X + seek * 96f, npc.Center.Y + 4f); // slide sideways slightly downward
            }
            else
            {
                _clearanceSeekDir = 0;
            }
            TargetWaypoint = desired;

            Vector2 toTarget = desired - npc.Center;
            Vector2 desiredVel = toTarget;
            float dist = desiredVel.Length();
            if (dist > 0.01f)
            {
                desiredVel.Normalize();
                desiredVel *= MathHelper.Min(dist * 0.06f, Config.HoverTopSpeed);
            }
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, 0.10f);

            // Glide unless we're climbing or turning sharply.  Ceiling-pinned: keep flapping
            // (visually: wings still trying to lift).
            bool climbing = npc.velocity.Y < -0.4f;
            bool turning  = Math.Abs(desiredVel.X - npc.velocity.X) > 0.6f;
            bool stuck    = HeadIsBlocked(npc) || StuckTicks > 30;
            WingsActiveThisTick = climbing || turning || stuck;

            // Face the player while hovering
            npc.direction = target.Center.X < npc.Center.X ? -1 : 1;
            npc.spriteDirection = npc.direction;

            if (ModeTimer <= 0)
            {
                // Default back to hover refresh; outer AI is expected to trigger dive/strafe.
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
            }
        }

        private void TickStrafe(NPC npc, Player target)
        {
            npc.noGravity = true;
            // Maintain altitude, accelerate horizontally toward TargetWaypoint.X past the player.
            float dx = TargetWaypoint.X - npc.Center.X;
            float desiredVx = Math.Sign(dx) * Config.HoverTopSpeed * 1.3f;
            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredVx, 0.18f);

            // Gentle vertical correction so we don't drift off altitude.
            float yErr = (target.Center.Y - Config.HoverAltitude) - npc.Center.Y;
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, MathHelper.Clamp(yErr * 0.06f, -2f, 2f), 0.15f);

            npc.direction = desiredVx < 0 ? -1 : 1;
            npc.spriteDirection = npc.direction;
            WingsActiveThisTick = false; // glide

            if (ModeTimer <= 0 || Math.Abs(dx) < 16f)
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
        }

        private void TickDive(NPC npc, Player target)
        {
            npc.noGravity = true;
            Vector2 toTarget = TargetWaypoint - npc.Center;
            if (toTarget.LengthSquared() > 1f) toTarget.Normalize();
            npc.velocity += toTarget * Config.DiveAcceleration;
            float maxSpeed = Config.DiveTopSpeed;
            if (npc.velocity.Length() > maxSpeed)
            {
                Vector2 n = npc.velocity;
                n.Normalize();
                npc.velocity = n * maxSpeed;
            }

            // First half: wings folded for the dive read.  Last quarter: pull up — flap to brake.
            float t = Config.DiveAttackTicks > 0
                ? 1f - (float)ModeTimer / Config.DiveAttackTicks
                : 1f;
            WingsActiveThisTick = t > 0.75f;

            npc.direction = npc.velocity.X < 0 ? -1 : 1;
            npc.spriteDirection = npc.direction;

            // End-of-dive: pull back up to Hover.
            if (ModeTimer <= 0 || npc.Center.Y >= target.Center.Y - 16f)
            {
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
            }
        }

        private void TickLand(NPC npc, Player target)
        {
            // Descend toward ground.  Compare FEET (not center) to ground tile so we don't
            // declare "landed" while the body is still half a tile above the surface.
            float groundY  = FindGroundY(npc);
            float feetY    = npc.position.Y + npc.height;
            float distFeet = groundY - feetY;

            // Phase 1 — still well above ground: powered descent with flapping.
            if (distFeet > 24f)
            {
                npc.noGravity = true;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, Config.LandSpeed, 0.20f);
                npc.velocity.X *= 0.90f;
                WingsActiveThisTick = true;
                return;
            }

            // Phase 2 — close to ground: drop noGravity so vanilla physics finish the landing.
            // Wings tuck just before touchdown.
            npc.noGravity = false;
            WingsActiveThisTick = false;

            // Phase 3 — feet have touched ground (velocity zeroed by collision) OR safety timeout.
            // Either path: cooldown + return to Grounded so ground AI resumes.
            if (npc.velocity.Y == 0f || ModeTimer <= 0)
            {
                CooldownRemaining = Config.CooldownTicks;
                EnterMode(FlightMode.Grounded, 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void EnterMode(FlightMode mode, int duration)
        {
            Mode = mode;
            ModeTimer = duration;
            if (mode == FlightMode.TakeOff)
                FlightTicksRemaining = Config.MaxFlightTicks;
        }

        private bool ReachedHoverAltitude(NPC npc)
        {
            // Reached cruising altitude if vertical velocity has flattened off.
            return npc.velocity.Y > -0.5f;
        }

        /// <summary>True if the NPC's head is pressed against a solid tile (4px above body top).</summary>
        private bool HeadIsBlocked(NPC npc)
        {
            int leftTile  = (int)(npc.position.X / 16f);
            int rightTile = (int)((npc.position.X + npc.width - 1f) / 16f);
            int topTile   = (int)((npc.position.Y - 4f) / 16f);
            for (int x = leftTile; x <= rightTile; x++)
            {
                if (x < 0 || x >= Main.maxTilesX || topTile < 0 || topTile >= Main.maxTilesY) continue;
                Tile t = Framing.GetTileSafely(x, topTile);
                if (t.HasUnactuatedTile && Main.tileSolid[t.TileType])
                    return true;
            }
            return false;
        }

        /// <summary>Scan ±96px from the NPC for the side with more overhead clearance.
        /// Returns +1 (right), -1 (left), or 0 if neither side is clearly better.</summary>
        private int SeekCeilingClearanceDir(NPC npc)
        {
            int yCheck = (int)((npc.position.Y - 16f) / 16f);
            int npcX   = (int)(npc.Center.X / 16f);

            int leftClear  = ScanColumnsForOpenCeiling(npcX - 6, yCheck);
            int rightClear = ScanColumnsForOpenCeiling(npcX + 6, yCheck);

            if (leftClear == rightClear) return 0;
            return rightClear > leftClear ? 1 : -1;
        }

        /// <summary>Count of how many tile-rows above (yStart, x) are non-solid (more = more clearance).</summary>
        private int ScanColumnsForOpenCeiling(int x, int yStart)
        {
            if (x < 0 || x >= Main.maxTilesX) return 0;
            int clear = 0;
            for (int y = yStart; y > yStart - 8 && y >= 0; y--)
            {
                Tile t = Framing.GetTileSafely(x, y);
                if (t.HasUnactuatedTile && Main.tileSolid[t.TileType]) break;
                clear++;
            }
            return clear;
        }

        /// <summary>Scan downward from the NPC for the first tile that can stop a fall.
        /// Recognises solid tiles AND platforms (jump-through tiles like Wood Platform).
        /// Without the platform check, an invader landing on a wood platform would never
        /// register "landed" — FindGroundY would skip past the platform looking for a deeper
        /// fully-solid tile, and TickLand would keep flapping forever.</summary>
        private float FindGroundY(NPC npc)
        {
            int x = (int)(npc.Center.X / 16f);
            int yStart = (int)(npc.Center.Y / 16f);
            for (int y = yStart; y < yStart + 64; y++)
            {
                if (y < 0 || y >= Main.maxTilesY) break;
                Tile t = Framing.GetTileSafely(x, y);
                if (!t.HasUnactuatedTile) continue;
                // Solid tile: standard ground.  TileSolidTop: platforms (Wood Platform etc.).
                if (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType])
                    return y * 16f;
            }
            return npc.Center.Y + 1024f; // no ground found (fall forever)
        }
    }

    public enum FlightMode
    {
        Grounded,
        TakeOff,
        Hover,
        Strafe,
        DiveAttack,
        Land
    }

    public struct EnemyFlightConfig
    {
        /// <summary>Pixels above the player to maintain during Hover.</summary>
        public float HoverAltitude;
        /// <summary>Horizontal offset from directly-above-player during Hover (creates side circling).</summary>
        public float HoverSideOffset;
        /// <summary>Cruise speed while hovering / strafing.</summary>
        public float HoverTopSpeed;
        /// <summary>Upward velocity during TakeOff.</summary>
        public float TakeOffSpeed;
        /// <summary>Per-tick velocity accumulation during DiveAttack.</summary>
        public float DiveAcceleration;
        /// <summary>Cap on dive velocity magnitude.</summary>
        public float DiveTopSpeed;
        /// <summary>Downward velocity during Land.</summary>
        public float LandSpeed;
        /// <summary>Max total airborne time before forced landing.</summary>
        public int   MaxFlightTicks;
        /// <summary>Cooldown after landing before next takeoff allowed.</summary>
        public int   CooldownTicks;

        /// <summary>Phase durations.</summary>
        public int   TakeOffTicks;
        public int   HoverDwellTicks;
        public int   StrafeTicks;
        public int   DiveAttackTicks;
        public int   LandTicks;

        /// <summary>Wing flap cycle speed (phase units per tick while flapping).</summary>
        public float WingFlapSpeed;

        /// <summary>Reasonable defaults for medium-size invaders.</summary>
        public static EnemyFlightConfig Default => new EnemyFlightConfig
        {
            HoverAltitude     = 200f,
            HoverSideOffset   = 80f,
            HoverTopSpeed     = 4.5f,
            TakeOffSpeed      = 8f,
            DiveAcceleration  = 0.55f,
            DiveTopSpeed      = 11f,
            LandSpeed         = 6f,
            MaxFlightTicks    = 720,   // 12 s
            CooldownTicks     = 240,   // 4 s
            TakeOffTicks      = 35,
            HoverDwellTicks   = 90,
            StrafeTicks       = 60,
            DiveAttackTicks   = 50,
            LandTicks         = 90,
            WingFlapSpeed     = 0.08f, // ~12 ticks per flap
        };
    }
}
