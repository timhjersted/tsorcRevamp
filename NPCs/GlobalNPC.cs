using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.ItemCrates;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Specialist;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.Dominatrix;
using tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using tsorcRevamp;

namespace tsorcRevamp.NPCs
{
    public class tsorcRevampGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public enum NavActionType
        {
            None,
            Walk,
            JumpTo,
            Drop,
            DropThroughPlatform
        }

        float enemyValue;
        float multiplier = 1f;
        float divisorMultiplier = 1f;
        int DarkSoulQuantity;
        public Player lastHitPlayerSummoner = Main.LocalPlayer;
        public Player lastHitPlayerRanger = Main.LocalPlayer;
        public Player lastHitPlayerShadowSickle = Main.LocalPlayer;

        public float SummonTagFlatDamage;
        public float SummonTagCriticalStrikeChance;
        public float FinalSummonCriticalStrikeChance;
        public float SummonTagScalingDamage;
        public float SummonTagArmorPenetration;
        public bool markedByCrystalNunchaku;
        public bool markedByDetonationSignal;
        public bool markedByDominatrix;
        public bool markedByDragoonLash;
        public bool markedBySupremeDragoonLash;
        public bool markedByEnchantedWhip;
        public bool markedByNightsCracker;
        public bool markedByPolarisLeash;
        public bool markedByPyrosulfate;
        public bool markedByPyromethane;
        public bool markedBySearingLash;
        public bool markedByTerraFall;
        public bool markedByTerraFallShard;
        public bool markedByUrumi;
        public bool markedByRustedChain;
        public bool markedByLeatherWhip;
        public bool markedByWitchkingMace;
        public bool markedBySnapthorn;
        public bool markedBySpinalTap;
        public bool markedByFirecracker;
        public bool markedByCoolWhip;
        public bool markedByDurendal;
        public bool markedByMorningStar;
        public bool markedByDarkHarvest;
        public bool markedByKaleidoscope;
        public bool Insane;

        public float CrystalNunchakuStacks = 10;
        public bool CrystalNunchakuProc = false;
        public int CrystalNunchakuUpdateTick = 0;
        public Player CrystalNunchakuWielder;

        public int NightsCrackerStacks = 0;
        public int TerraFallStacks = 0;

        public bool Scorched;
        public int ScorchMarks = 0;
        public float SuperScorchDuration = 0f;
        public bool Shocked;
        public int ShockMarks = 0;
        public float SuperShockDuration = 0f;
        public bool Sunburnt;
        public bool Awestruck;
        public int SunburnMarks = 0;
        public float SuperSunburnDuration = 0f;

        //Stores the event this NPC belongs to
        public ScriptedEvent ScriptedEventOwner;

        //Stores which NPC in that event this is
        public int ScriptedEventIndex;

        //Whatever custom expert scaling we want goes here. For reference 1 eliminates all expert mode doubling, and 2 is normal expert mode scaling.
        public static double expertScale = 2;

        public bool DarkInferno;
        public bool AbyssInferno;
        public bool WitchkingCurse;
        public bool AbyssalSinking;
        public bool CCShocked;
        public bool Ignited;
        public bool CrimsonBurn;
        public bool ToxicCatDrain;
        public bool ResetToxicCatBlobs;
        public bool ViruCatDrain;
        public bool ResetViruCatBlobs;
        public bool BiohazardDrain;
        public bool ResetBiohazardBlobs;
        public bool ElectrocutedEffect;
        public bool ElectrocutedEffect2;
        public bool ElectrocutedEffect3;
        public bool PolarisElectrocutedEffect;
        public bool CrescentMoonlight;
        public bool Soulstruck;
        public bool PhazonCorruption;

        public int LionheartMarks = 0;

        public bool Sundered;

        public bool Venomized;
        public bool Electrified;
        public bool Irradiated;
        public bool IrradiatedByShroom;

        public int CritColorTier = 0;


        //Custom AI personality paramaters

        /// <summary>
        /// How likely it is to dash at the player if it is far away.
        /// Range: 0.00001 - 2.5
        /// </summary>
        public float Aggression = -1;

        /// <summary>
        /// Controls how quickly it gets bored (and thus how long it waits before teleporting, if it has that ability).
        /// Range: 0.5 - 2
        /// </summary>
        public float Patience = -1;

        /// <summary>
        /// How likely it is to try and run if it is low on health.
        /// Range: 0 - 0.3
        /// </summary>
        public float Cowardice = -1;

        /// <summary>
        /// Improves the likelihood of performing low-weighted attacks.
        /// Range: 0 - 0.3
        /// </summary>
        public float Adeptness = -1;

        /// <summary>
        /// Modifies movement speed and acceleration.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Swiftness = -1;

        /// <summary>
        /// Modifies how often it fires projectiles.
        /// Range: 0.6 - 1.4
        /// </summary>
        public float CastingSpeed = -1;

        /// <summary>
        /// Modifies base health, size, and contact damage.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Strength = -1;

        /// <summary>
        /// Controls how often it tries to roll through or jumps over projectiles.
        /// Range: 0.2 - 0.6
        /// </summary>
        public float Agility = -1;


        //Custom AI execution values
        public int DoorBreakProgress;
        public bool Fleeing;
        public bool Initialized;
        public int PounceTimer;
        public int PounceCooldown;
        public int DodgeTimer;
        public int DodgeCooldown;
        public int BoredTimer;
        public int FighterPostAttackPauseTimer;
        public int FighterAttacksSincePause;
        public bool FighterRangedHitInterruptedPause;
        public int FighterRangedStandShotsRemaining;  // >0 = standing-fire mode; decrement each shot, exit when zero
        public int FighterNoLosPursuitBoostTimer;

        // Navigation intelligence: 0 = dumb, 1 = smart pathfinding (default), 2 = waypoint scan (future)
        public int NavigationTier = 0;
        // Vertical jump power ceiling — default 8f reproduces vanilla hardcoded behavior at base stats
        public float MaxJumpPower = 8f;
        // Horizontal momentum added when jumping a gap
        public float MaxJumpBoost = 4f;
        // Whether this NPC can perform a mid-air second jump
        public bool CanDoubleJump = false;
        // Tracks whether the double jump has been used this airborne phase (reset on landing)
        public bool UsedDoubleJump = false;
        // Strength of the mid-air second jump
        public float DoubleJumpPower = 6f;
        // Counts consecutive frames stuck against a wall; triggers an escape jump when too high
        public int StuckTimer = 0;
        // Ledge run-up: when StuckTimer first reaches 8 against an obstacle, the NPC
        // reverses briefly (LedgeRunUpTimer > 0) to build clearance, then charges forward
        // and makes a powered running jump.  Prevents the endless ledge-bounce loop
        // where the NPC is pressed too close to a wall to clear the ledge corner.
        public int LedgeRunUpTimer = 0;
        public int LedgeRunUpDirection = 0;
        public int LedgeVaultTimer = 0;
        public int LedgeVaultDirection = 0;
        public int NavJumpCooldown = 0;
        public bool CanStopToFire = false;
        // Tier 2 navigation: temporary world-space X target for "go around" ledge routing
        public Vector2 WaypointTarget = Vector2.Zero;
        // How many frames remain on the active waypoint (0 = none)
        public int WaypointTimer = 0;
        public NavActionType WaypointAction = NavActionType.None;
        public int WaypointSearchCooldown = 0;
        public float LastWaypointDistance = 0f;
        public int WaypointNoProgressTimer = 0;
        public const int MaxNavRouteSteps = 10;
        public Vector2[] NavRouteTargets = new Vector2[MaxNavRouteSteps];
        public NavActionType[] NavRouteActions = new NavActionType[MaxNavRouteSteps];
        public int NavRouteIndex = 0;
        public int NavRouteCount = 0;
        public int NavRouteTimer = 0;
        public int NavRouteNoProgressTimer = 0;
        public float LastNavRouteDistance = 0f;
        public int NavBlockedDirection = 0;
        public int NavBlockedDirectionTimer = 0;
        public int NavExploreTimer = 0;
        public int NavExploreDirection = 0;
        public int SmartFurniturePassTimer = 0;
        public int SmartFurniturePassDirection = 0;
        public int SmartFurniturePassCooldown = 0;
        // Frames spent voluntarily halted at a ledge; used to cap ledge-camping.
        public int LedgeHaltTimer = 0;
        // When true, FighterAI will halt at the edge of a significant drop when it already has
        // line of sight to the player.  Disabled by default — only opt in for enemies that are
        // supposed to hold the high ground (e.g. ranged enemies that shouldn't charge off ledges).
        public bool HaltAtLedge = false;
        // When true, the NPC teleports through solid walls it cannot navigate around.
        public bool CanPassThroughWalls = false;
        // Counts ticks the NPC has been grounded and blocked by a wall; triggers teleport at threshold.
        public int GhostWallTimer = 0;
        // Set true each frame the NPC runs the mod's custom BasicAI/Fighter/Archer AI. Lets PostAI apply
        // confusion (reversed movement) only to these NPCs — vanilla-AI NPCs already handle Confused themselves,
        // so we must not double-flip them. Consumed (reset) in PostAI.
        public bool RunningCustomFighterAI = false;
        // Teleport visual tuning. Defaults reproduce the current smoke-flash behavior.
        public int TeleportTelegraphTime = 140;
        public int TeleportDustType = DustID.Smoke;
        public Color TeleportDustColor = Color.White;
        public float TeleportDustScale = 0.8f;
        public int TeleportDustCount = 20;
        // Whether this NPC has limited-use gap-closing teleports (2 total uses, 10s cooldown, 40-tile minimum)
        public bool WeakTeleport = false;
        // How many weak teleport charges remain for this NPC instance. These do not recharge.
        public int WeakTeleportUses = 2;
        // Cooldown frames remaining before the next WeakTeleport charge can fire
        public int WeakTeleportCooldown = 0;
        // Frames since the NPC last reached the player; triggers bored walk when it exceeds WeakTeleportBoredThreshold.
        public int WeakTeleportReachTimer = 0;
        public int WeakTeleportBoredThreshold = 7200;
        // Bored walk phase: 0=normal, 1=standstill (2s), 2=walk away (5s), 3=pause (2s), 4=walk back (2s)
        public int WeakTeleportBoredPhase = 0;
        // Countdown for the current bored walk phase
        public int WeakTeleportBoredTimer = 0;
        // How long regular fighter pathing tries before using its fallback bored behavior.
        public int BoredomThreshold = 900;
        public string LastNavIntent = "none";
        public string LastWaypointResult = "none";
        public int WaypointSearchFailures = 0;
        public int LastNavDebugLogTick = 0;

        public bool needsNetUpdate;
        public float ProjectileTimer;
        public float ProjectileTimerCap
        {
            get
            {
                return CurrentAttack.timerCap;
            }
        }
        public float ProjectileTelegraphStart
        {
            get
            {
                return ProjectileTimerCap - 24;
            }
        }
        public float ArcherAimDirection;
        public Vector2 LockedShotVector;
        public int TeleportCountdown;
        public List<tsorcRevampAIs.ProjectileData> AttackList = new List<tsorcRevampAIs.ProjectileData>();
        public int AttackIndex;
        public int AttackSucceeded = -1;
        public tsorcRevampAIs.ProjectileData CurrentAttack
        {
            get
            {
                return AttackList[AttackIndex];
            }
        }
        public int NextAttackIndex;
        private Vector2 TeleportTelegraphInternal;
        public Vector2 TeleportTelegraph
        {
            get
            {
                return TeleportTelegraphInternal;
            }
            set
            {
                //This lets it automatically trigger a netupdate whenever this variable is set to something new
                needsNetUpdate = true;
                TeleportTelegraphInternal = value;
            }
        }

        //Stores the targeting, tracking, and despawning information for a NPC
        public NPCDespawnHandler DespawnHandler;
        private static HashSet<int> defeatedPillars = new HashSet<int>();

        public override void ResetEffects(NPC npc)
        {
            DarkInferno = false;
            AbyssInferno = false;
            WitchkingCurse = false;
            AbyssalSinking = false;
            CCShocked = false;
            Ignited = false;
            CrimsonBurn = false;
            ToxicCatDrain = false;
            ResetToxicCatBlobs = false;
            ViruCatDrain = false;
            ResetViruCatBlobs = false;
            BiohazardDrain = false;
            ResetBiohazardBlobs = false;
            ElectrocutedEffect = false;
            ElectrocutedEffect2 = false;
            ElectrocutedEffect3 = false;
            PolarisElectrocutedEffect = false;
            CrescentMoonlight = false;
            Soulstruck = false;
            PhazonCorruption = false;
            Venomized = false;
            Electrified = false;
            Irradiated = false;
            IrradiatedByShroom = false;
            markedByCrystalNunchaku = false;
            markedByDetonationSignal = false;
            markedByDominatrix = false;
            markedByDragoonLash = false;
            markedBySupremeDragoonLash = false;
            markedByEnchantedWhip = false;
            markedByNightsCracker = false;
            markedByPolarisLeash = false;
            markedByPyrosulfate = false;
            markedByPyromethane = false;
            markedBySearingLash = false;
            markedByTerraFall = false;
            markedByTerraFallShard = false;
            markedByUrumi = false;
            markedByLeatherWhip = false;
            markedBySnapthorn = false;
            markedBySpinalTap = false;
            markedByFirecracker = false;
            markedByCoolWhip = false;
            markedByDurendal = false;
            markedByMorningStar = false;
            markedByDarkHarvest = false;
            markedByKaleidoscope = false;
            Sundered = false;
            Scorched = false;
            Shocked = false;
            Sunburnt = false;
            Insane = false;
        }

        public override bool PreAI(NPC npc)
        {
            if (needsNetUpdate)
            {
                needsNetUpdate = false;
                npc.netUpdate = true;
            }
            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            // Confusion: the mod's custom AI computes movement toward the player and ignores npc.confused, so a
            // confused enemy still walked straight at you (only showing the "?" emote). Here, after the AI has
            // run, we reverse a confused custom-AI enemy's horizontal movement so it stumbles AWAY instead.
            // Vertical velocity (jumps) is left intact so it still hops terrain — just in the wrong direction.
            // Only applies to NPCs that ran our BasicAI this frame; vanilla-AI NPCs handle Confused on their own.
            if (RunningCustomFighterAI && npc.confused && npc.target >= 0 && npc.target < Main.maxPlayers)
            {
                int away = npc.Center.X < Main.player[npc.target].Center.X ? -1 : 1;
                float speed = Math.Max(Math.Abs(npc.velocity.X), 1.5f);
                npc.velocity.X = away * speed;
                npc.direction = away;
                npc.spriteDirection = away;
            }
            RunningCustomFighterAI = false;

            if (!CanPassThroughWalls)
                return;

            // ── Ghost wall teleport ───────────────────────────────────────────────
            // When blocked by a solid wall for ~25 frames, scan for a valid landing
            // spot on the far side and instantly relocate there.
            //
            // BUG FIX: the original code required onGround (velocity.Y == 0) before
            // accumulating GhostWallTimer.  FighterAI's jump code fires every few
            // frames against an impassable wall, setting velocity.Y < 0, which made
            // onGround false and reset the timer to 0 every cycle — the threshold was
            // never reached and the teleport (and its dust) never fired.
            // Fix: accumulate whenever a wall tile is present in the forward column,
            // regardless of vertical velocity.  The timer drains twice as fast when
            // the column is clear so normal single-frame wall-grazes don't trigger.
            //
            // Additional guard: require the NPC to actually be moving forward (velocity in
            // its facing direction > 0.2 px/tick).  Without this, enemies with custom AI
            // that decelerate to 0 during attacks (e.g. GhostOfAHollowWarrior's slash) could
            // silently accumulate the timer and teleport mid-animation.
            float _ghostFwdVel = npc.direction * npc.velocity.X;
            bool wallBlocked = _ghostFwdVel > 0.2f && IsWallBlockingAhead(npc);

            if (wallBlocked)
                GhostWallTimer++;
            else
                GhostWallTimer = Math.Max(0, GhostWallTimer - 2);

            if (GhostWallTimer >= 25)
            {
                // Only queue if no teleport is already in progress.
                if (TeleportCountdown == 0 && TryGhostWallTeleport(npc))
                    ProjectileTimer = 0f;
                // Reset regardless — let FighterAI's direction-flip move the NPC
                // away before the next accumulation attempt starts.
                GhostWallTimer = 0;
            }
        }

        // ── Ghost wall teleport helpers ───────────────────────────────────────────

        /// <summary>
        /// Returns true when the tile column immediately in front of the NPC (in its
        /// current facing direction) contains at least one solid tile at body level —
        /// i.e. the NPC is walking into a wall it cannot step over normally.
        /// </summary>
        private static bool IsWallBlockingAhead(NPC npc)
        {
            int dir = npc.direction == 0 ? 1 : npc.direction;
            int frontTileX = dir == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int feetTileY   = (int)((npc.position.Y + npc.height) / 16f);
            int bodyHtTiles = (int)Math.Ceiling(npc.height / 16.0);

            for (int row = feetTileY - bodyHtTiles; row < feetTileY; row++)
            {
                if (UsefulFunctions.IsTileReallySolid(frontTileX, row))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans ahead for a valid open landing spot on the far side of the wall,
        /// teleports the NPC there, and spawns grey smoke at both positions.
        ///
        /// Valid landing: solid floor, PLUS at least 2 tiles wide × 3 tiles tall of clear
        /// air space (landing column AND the next column in the direction of travel).
        /// The 2-wide requirement prevents the NPC landing on a 1-tile ledge next to a
        /// slope or wall where it immediately gets stuck again.
        ///
        /// Scans up to 10 tiles wide (wall thickness), then up to 4 tiles beyond,
        /// checking ±3 tile Y variation to handle steps/ramps.
        ///
        /// Returns false if the wall leads into solid earth (no teleport occurs).
        /// </summary>
        private static bool TryGhostWallTeleport(NPC npc)
        {
            int dir = npc.direction == 0 ? 1 : npc.direction;
            int frontTileX  = dir == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int feetTileY   = (int)((npc.position.Y + npc.height) / 16f);
            int bodyHtTiles = (int)Math.Ceiling(npc.height / 16.0);

            // Minimum vertical clearance: always at least 3 tiles even for short NPCs.
            int minClearance = Math.Max(bodyHtTiles, 3);

            // ── Phase 1: find where the wall ends ────────────────────────────────
            // Scan forward until we find a column where the NPC's body would fit.
            int wallEndX = -1;
            for (int i = 0; i <= 10; i++)
            {
                int tx = frontTileX + dir * i;
                bool columnClear = true;
                for (int row = feetTileY - bodyHtTiles; row < feetTileY; row++)
                {
                    if (UsefulFunctions.IsTileReallySolid(tx, row))
                    {
                        columnClear = false;
                        break;
                    }
                }
                if (columnClear)
                {
                    if (i == 0) return false; // no actual wall in front (shouldn't happen)
                    wallEndX = tx;
                    break;
                }
            }
            if (wallEndX == -1) return false; // wall > 10 tiles thick / solid earth

            // ── Phase 2: find a valid floor at or near wall exit ─────────────────
            // Try the same Y level first, then offset up/down to handle steps/ramps.
            int[] yOffsets = { 0, -1, 1, -2, 2, -3, 3 };
            for (int xi = 0; xi <= 4; xi++)
            {
                int tx = wallEndX + dir * xi;
                foreach (int yOff in yOffsets)
                {
                    int groundTile = feetTileY + yOff;

                    // Floor must be solid at the landing column.
                    if (!UsefulFunctions.IsTileReallySolid(tx, groundTile))
                        continue;

                    // ── 2-wide × 3-tall clearance check ──────────────────────────
                    // Both the landing column (tx) and the adjacent column in the travel
                    // direction (tx + dir) must have minClearance rows of clear air above
                    // the floor.  This stops the NPC landing on a 1-tile ledge next to a
                    // slope/wall where it would immediately get wedged.
                    bool bodyFits = true;
                    int adjX = tx + dir;

                    for (int col = 0; col < 2 && bodyFits; col++)
                    {
                        int checkX = col == 0 ? tx : adjX;
                        for (int row = groundTile - minClearance; row < groundTile; row++)
                        {
                            if (UsefulFunctions.IsTileReallySolid(checkX, row))
                            {
                                bodyFits = false;
                                break;
                            }
                        }
                    }
                    if (!bodyFits) continue;

                    // ── Valid spot found — queue a telegraphed teleport ──────────
                    // Use the same TeleportCountdown / TeleportTelegraph mechanism as
                    // QueueTeleport so the player sees the ring-flash telegraph at both
                    // origin and destination, and ExecuteQueuedTeleport handles the dust
                    // and the actual position change when the countdown expires.
                    Vector2 destPos = new Vector2(
                        tx * 16f + 8f - npc.width  / 2f,
                        groundTile * 16f - npc.height
                    );
                    Vector2 destCenter = destPos + new Vector2(npc.width / 2f, npc.height / 2f);

                    tsorcRevampGlobalNPC gNpc = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                    int telegraphTime = gNpc.TeleportTelegraphTime;
                    gNpc.TeleportCountdown = telegraphTime;
                    gNpc.TeleportTelegraph = destCenter;
                    npc.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            npc.whoAmI, telegraphTime);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), destCenter, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            ai1: telegraphTime);
                    }

                    return true;
                }
            }

            return false; // far side is solid earth
        }

        // ── BFS pathfinding: pre-allocated static buffers (zero GC cost per scan) ──────────────
        private const int BFS_R    = 80;
        private const int BFS_DIM  = BFS_R * 2 + 2;   // 162 × 162
        private const int BFS_QCAP = 32768;
        private const int BFS_QMSK = BFS_QCAP - 1;

        private static readonly int[]    _bfsQX  = new int  [BFS_QCAP];
        private static readonly int[]    _bfsQY  = new int  [BFS_QCAP];
        private static readonly bool[,]  _bfsVis = new bool [BFS_DIM, BFS_DIM];
        private static readonly short[,] _bfsAnX = new short[BFS_DIM, BFS_DIM];
        private static readonly short[,] _bfsAnY = new short[BFS_DIM, BFS_DIM];
        private static readonly NavActionType[,] _bfsAnAction = new NavActionType[BFS_DIM, BFS_DIM];
        private static readonly NavActionType[,] _bfsEdgeAction = new NavActionType[BFS_DIM, BFS_DIM];
        private static readonly short[,] _bfsParentX = new short[BFS_DIM, BFS_DIM];
        private static readonly short[,] _bfsParentY = new short[BFS_DIM, BFS_DIM];
        private const int BFS_PATH_CAP = 256;
        private static readonly short[] _bfsPathX = new short[BFS_PATH_CAP];
        private static readonly short[] _bfsPathY = new short[BFS_PATH_CAP];
        private static readonly NavActionType[] _bfsPathAction = new NavActionType[BFS_PATH_CAP];

        /// <summary>
        /// BFS from the NPC's current floor tile to the player's floor tile.
        /// Navigates walks, ledge-falls, jumps, and platform drops.
        /// Returns the world-space centre of the first waypoint tile on the path.
        /// No heap allocation — uses static buffers.
        /// </summary>
        internal static bool BfsFindWaypoint(NPC npc, float maxJumpPower, float maxJumpBoost,
                                              out Vector2 waypoint)
        {
            return BfsFindWaypoint(npc, maxJumpPower, maxJumpBoost, out waypoint, out _);
        }

        internal static bool BfsFindWaypoint(NPC npc, float maxJumpPower, float maxJumpBoost,
                                              out Vector2 waypoint, out NavActionType action)
        {
            waypoint = Vector2.Zero;
            action = NavActionType.None;
            Span<Vector2> routeTargets = stackalloc Vector2[MaxNavRouteSteps];
            Span<NavActionType> routeActions = stackalloc NavActionType[MaxNavRouteSteps];
            if (BfsFindRoute(npc, maxJumpPower, maxJumpBoost, routeTargets, routeActions, out int routeCount) && routeCount > 0)
            {
                waypoint = routeTargets[0];
                action = routeActions[0];
                return true;
            }

            return false;
        }

        internal static bool BfsFindRoute(NPC npc, float maxJumpPower, float maxJumpBoost,
                                          Span<Vector2> routeTargets, Span<NavActionType> routeActions, out int routeCount)
        {
            routeCount = 0;
            int sx = (int)(npc.Center.X / 16f);
            int sy = BfsFindStandableFloorY(npc, sx, (int)((npc.position.Y + npc.height + 4f) / 16f), 8, 12);
            int tx = (int)(Main.player[npc.target].Center.X / 16f);
            int ty = BfsFindTargetFloorY(npc, ref tx, (int)((Main.player[npc.target].position.Y
                            + Main.player[npc.target].height + 4f) / 16f));

            if (sy < 0 || ty < 0)
            {
                return false;
            }

            if (Math.Abs(sx - tx) <= 1 && Math.Abs(sy - ty) <= 1) return false;

            // Max upward tile reach: v²/(2g·tileSize), g≈0.4 px/frame², tileSize=16
            int jumpH = Math.Max(2, (int)(maxJumpPower * maxJumpPower / 12.8f));
            // Conservative horizontal reach during a jump arc
            int jumpW = Math.Max(1, Math.Min((int)maxJumpBoost, 5));

            Array.Clear(_bfsVis, 0, _bfsVis.Length);
            int head = 0, tail = 0;

            // Enqueue start node (anchor = itself = no step taken yet)
            _bfsVis[BFS_R, BFS_R] = true;
            _bfsAnX[BFS_R, BFS_R] = (short)sx;
            _bfsAnY[BFS_R, BFS_R] = (short)sy;
            _bfsAnAction[BFS_R, BFS_R] = NavActionType.None;
            _bfsEdgeAction[BFS_R, BFS_R] = NavActionType.None;
            _bfsParentX[BFS_R, BFS_R] = (short)sx;
            _bfsParentY[BFS_R, BFS_R] = (short)sy;
            _bfsQX[0] = sx; _bfsQY[0] = sy;
            tail = 1;

            int startScore = BfsTileScore(sx, sy, tx, ty);
            int bestScore = startScore;
            int bestX = sx;
            int bestY = sy;

            while (head != tail)
            {
                int cx = _bfsQX[head], cy = _bfsQY[head];
                head = (head + 1) & BFS_QMSK;

                int aox = cx - sx + BFS_R, aoy = cy - sy + BFS_R;
                short anX = _bfsAnX[aox, aoy], anY = _bfsAnY[aox, aoy];
                NavActionType anAction = _bfsAnAction[aox, aoy];
                bool isStart = cx == sx && cy == sy;

                if (!isStart)
                {
                    int score = BfsTileScore(cx, cy, tx, ty);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = cx;
                        bestY = cy;
                    }
                }

                if (Math.Abs(cx - tx) <= 2 && Math.Abs(cy - ty) <= 1)
                {
                    return BfsBuildCompressedRoute(npc, sx, sy, cx, cy, routeTargets, routeActions, out routeCount);
                }

                // ── Walk left / right ─────────────────────────────────────────────
                for (int d = -1; d <= 1; d += 2)
                {
                    int nx = cx + d;

                    short ax = isStart ? (short)nx : anX;
                    short ay = isStart ? (short)cy : anY;
                    NavActionType nextAction = isStart ? NavActionType.Walk : anAction;

                    if (BfsCanStand(npc, nx, cy))
                    {
                        if (isStart)
                        {
                            BfsFindWalkCommitment(npc, sx, sy, d, tx, ty, out ax, out ay);
                        }
                        BfsEnqueue(ref tail, nx, cy, ax, ay, sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy - 1))
                    {
                        // One-tile rises should be treated as ordinary terrain, not a full jump.
                        BfsEnqueue(ref tail, nx, cy - 1, ax, (short)(isStart ? cy - 1 : ay), sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy + 1))
                    {
                        // One-tile drops/slopes are walkable. Do not convert them into jump waypoints.
                        BfsEnqueue(ref tail, nx, cy + 1, ax, (short)(isStart ? cy + 1 : ay), sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy - 2))
                    {
                        NavActionType stepJumpAction = isStart ? NavActionType.JumpTo : anAction;
                        BfsEnqueue(ref tail, nx, cy - 2, ax, (short)(isStart ? cy - 2 : ay), sx, sy, cx, cy, stepJumpAction, NavActionType.JumpTo);
                    }
                    else
                    {
                        // Gap — fall to next landing (max 20 tiles)
                        for (int fy = cy + 2; fy <= cy + 20; fy++)
                        {
                            if (UsefulFunctions.IsTileReallySolid(nx, fy)) break;
                            if (BfsCanStand(npc, nx, fy))
                            {
                                NavActionType dropAction = isStart ? NavActionType.Drop : anAction;
                                BfsEnqueue(ref tail, nx, fy, ax, ay, sx, sy, cx, cy, dropAction, NavActionType.Drop);
                                break;
                            }
                        }
                    }
                }

                // ── Jump up to higher floors ──────────────────────────────────────
                for (int dh = 1; dh <= jumpH; dh++)
                {
                    int ry = cy - dh;
                    // Try landing at this level BEFORE checking whether the path is blocked —
                    // IsTileReallySolid(cx, ry) would be true for the target floor tile itself,
                    // which previously caused the loop to break before ever checking for a landing.
                    for (int jdx = -jumpW; jdx <= jumpW; jdx++)
                    {
                        int jx = cx + jdx;
                        if (BfsCanStand(npc, jx, ry))
                        {
                            short ax = isStart ? (short)jx : anX;
                            short ay = isStart ? (short)ry  : anY;
                            NavActionType jumpAction = isStart ? NavActionType.JumpTo : anAction;
                            BfsEnqueue(ref tail, jx, ry, ax, ay, sx, sy, cx, cy, jumpAction, NavActionType.JumpTo);
                        }
                    }
                    // Stop ascending if the vertical path is blocked (solid ceiling above).
                    if (UsefulFunctions.IsTileReallySolid(cx, ry)) break;
                }

                // ── Drop through platform ─────────────────────────────────────────
                Tile floorTile = Framing.GetTileSafely(cx, cy);
                if (floorTile.HasTile && !floorTile.IsActuated && TileID.Sets.Platforms[floorTile.TileType])
                {
                    for (int fy = cy + 1; fy <= cy + 20; fy++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(cx, fy)) break;
                        if (BfsCanStand(npc, cx, fy))
                        {
                            short ax = isStart ? (short)cx : anX;
                            short ay = isStart ? (short)fy  : anY;
                            NavActionType platformDropAction = isStart ? NavActionType.DropThroughPlatform : anAction;
                            BfsEnqueue(ref tail, cx, fy, ax, ay, sx, sy, cx, cy, platformDropAction, NavActionType.DropThroughPlatform);
                            break;
                        }
                    }
                }
            }

            // If the exact player floor was not representable in the simplified graph
            // (common around stairs, half-blocks, platforms, furniture, or cramped houses),
            // still return the first step toward the best reachable node. This keeps bored
            // tiered enemies from doing nothing just because the final tile failed BFS.
            if ((bestX != sx || bestY != sy) && bestScore <= startScore - 4)
            {
                return BfsBuildCompressedRoute(npc, sx, sy, bestX, bestY, routeTargets, routeActions, out routeCount);
            }

            return false;
        }

        private static bool BfsBuildCompressedRoute(NPC npc, int sx, int sy, int endX, int endY,
                                                    Span<Vector2> routeTargets, Span<NavActionType> routeActions, out int routeCount)
        {
            routeCount = 0;
            int pathCount = 0;
            int cx = endX;
            int cy = endY;

            while ((cx != sx || cy != sy) && pathCount < BFS_PATH_CAP)
            {
                int ox = cx - sx + BFS_R;
                int oy = cy - sy + BFS_R;
                if ((uint)ox >= BFS_DIM || (uint)oy >= BFS_DIM)
                {
                    break;
                }

                _bfsPathX[pathCount] = (short)cx;
                _bfsPathY[pathCount] = (short)cy;
                _bfsPathAction[pathCount] = _bfsEdgeAction[ox, oy] == NavActionType.None ? NavActionType.Walk : _bfsEdgeAction[ox, oy];

                int px = _bfsParentX[ox, oy];
                int py = _bfsParentY[ox, oy];
                if (px == cx && py == cy)
                {
                    break;
                }

                cx = px;
                cy = py;
                pathCount++;
            }

            if (pathCount == 0)
            {
                return false;
            }

            int maxSteps = Math.Min(routeTargets.Length, MaxNavRouteSteps);
            NavActionType currentAction = NavActionType.None;
            int lastAddedX = sx;
            int lastAddedY = sy;

            for (int i = pathCount - 1; i >= 0 && routeCount < maxSteps; i--)
            {
                int x = _bfsPathX[i];
                int y = _bfsPathY[i];
                NavActionType rawAction = _bfsPathAction[i] == NavActionType.None ? NavActionType.Walk : _bfsPathAction[i];
                NavActionType action = BfsNormalizeRouteAction(lastAddedX, lastAddedY, x, y, rawAction);
                bool isFinal = i == 0;
                bool actionChanged = routeCount == 0 || action != currentAction;
                bool longWalkCommit = action == NavActionType.Walk && Math.Abs(x - lastAddedX) >= 6;
                bool verticalChange = Math.Abs(y - lastAddedY) >= 2;
                bool tinyOpeningWalk = routeCount == 0
                    && action == NavActionType.Walk
                    && !isFinal
                    && Math.Abs(x - sx) <= 1
                    && Math.Abs(y - sy) <= 1;

                if (!tinyOpeningWalk && (actionChanged || longWalkCommit || verticalChange || isFinal))
                {
                    routeTargets[routeCount] = BfsFloorToNpcCenter(npc, x, y);
                    routeActions[routeCount] = action;
                    routeCount++;
                    currentAction = action;
                    lastAddedX = x;
                    lastAddedY = y;
                }
            }

            return routeCount > 0;
        }

        private static NavActionType BfsNormalizeRouteAction(int fromX, int fromY, int toX, int toY, NavActionType rawAction)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = toY - fromY; // positive means the target floor is lower.

            if (rawAction == NavActionType.DropThroughPlatform)
            {
                return NavActionType.DropThroughPlatform;
            }

            if (dy > 1)
            {
                return NavActionType.Drop;
            }

            if (dy == 1)
            {
                return NavActionType.Walk;
            }

            if (dy == 0)
            {
                return rawAction == NavActionType.JumpTo && dx > 4 ? NavActionType.JumpTo : NavActionType.Walk;
            }

            if (dy == -1)
            {
                return NavActionType.Walk;
            }

            return NavActionType.JumpTo;
        }

        private static int BfsTileScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(x - targetX) * 2 + Math.Abs(y - targetY) * 3;
        }

        private static Vector2 BfsFloorToNpcCenter(NPC npc, int floorX, int floorY)
        {
            return new Vector2(floorX * 16f + 8f, floorY * 16f - npc.height / 2f);
        }

        private static void BfsFindWalkCommitment(NPC npc, int startX, int startY, int direction, int targetX, int targetY, out short anchorX, out short anchorY)
        {
            anchorX = (short)(startX + direction);
            anchorY = (short)startY;

            int bestX = anchorX;
            int bestY = anchorY;
            int bestScore = BfsTileScore(bestX, bestY, targetX, targetY);
            int previousY = startY;

            for (int step = 1; step <= 18; step++)
            {
                int x = startX + step * direction;
                int y = previousY;

                if (!BfsCanStand(npc, x, y))
                {
                    int adjustedY = -1;
                    for (int dy = -2; dy <= 3; dy++)
                    {
                        if (BfsCanStand(npc, x, previousY + dy))
                        {
                            adjustedY = previousY + dy;
                            break;
                        }
                    }

                    if (adjustedY < 0)
                    {
                        break;
                    }

                    y = adjustedY;
                }

                if (UsefulFunctions.IsTileReallySolid(x, y - 1) || UsefulFunctions.IsTileReallySolid(x, y - 2))
                {
                    break;
                }

                int score = BfsTileScore(x, y, targetX, targetY);
                if (score <= bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }

                previousY = y;

                if (step >= 6 && (Math.Abs(x - targetX) <= 2 || score > bestScore + 10))
                {
                    break;
                }
            }

            anchorX = (short)bestX;
            anchorY = (short)bestY;
        }

        private static int BfsFindStandableFloorY(NPC npc, int x, int approximateFloorY, int scanUp, int scanDown)
        {
            int bestY = -1;
            int bestDistance = int.MaxValue;
            for (int dy = -scanUp; dy <= scanDown; dy++)
            {
                int floorY = approximateFloorY + dy;
                if (!BfsCanStand(npc, x, floorY))
                {
                    continue;
                }

                int distance = Math.Abs(dy);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestY = floorY;
                }
            }
            return bestY;
        }

        private static int BfsFindTargetFloorY(NPC npc, ref int targetX, int approximateFloorY)
        {
            int bestX = targetX;
            int bestY = -1;
            int bestScore = int.MaxValue;

            for (int dx = -2; dx <= 2; dx++)
            {
                int x = targetX + dx;
                int y = BfsFindStandableFloorY(npc, x, approximateFloorY, 8, 16);
                if (y < 0)
                {
                    continue;
                }

                int score = Math.Abs(dx) * 4 + Math.Abs(y - approximateFloorY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }
            }

            if (bestY >= 0)
            {
                targetX = bestX;
                return bestY;
            }

            return BfsFindStandableFloorY(npc, targetX, approximateFloorY, 8, 20);
        }

        /// <summary>True if the NPC (2 tiles tall) can stand at (x, floorY).</summary>
        internal static bool BfsCanStand(NPC npc, int x, int floorY)
        {
            int widthTiles = Math.Max(1, (int)Math.Ceiling(npc.width / 16f));
            int leftX = x - (widthTiles - 1) / 2;
            int rightX = leftX + widthTiles - 1;

            bool hasFloor = false;
            for (int tx = leftX; tx <= rightX; tx++)
            {
                if (UsefulFunctions.IsTileReallySolid(tx, floorY))
                {
                    hasFloor = true;
                    break;
                }

                Tile t = Framing.GetTileSafely(tx, floorY);
                if (t.HasTile && !t.IsActuated && TileID.Sets.Platforms[t.TileType])
                {
                    hasFloor = true;
                    break;
                }
            }

            if (!hasFloor)
            {
                return false;
            }

            int bodyTiles = Math.Max(2, (int)Math.Ceiling(npc.height / 16f));
            for (int tx = leftX; tx <= rightX; tx++)
            {
                for (int y = floorY - bodyTiles; y < floorY; y++)
                {
                    if (UsefulFunctions.IsTileReallySolid(tx, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Add (x,y) to the BFS queue if in-bounds and not yet visited.</summary>
        internal static void BfsEnqueue(ref int tail, int x, int y,
                                         short ax, short ay, int sx, int sy, int parentX, int parentY,
                                         NavActionType action, NavActionType edgeAction)
        {
            int ox = x - sx + BFS_R, oy = y - sy + BFS_R;
            if ((uint)ox >= BFS_DIM || (uint)oy >= BFS_DIM) return;
            if (_bfsVis[ox, oy]) return;
            _bfsVis[ox, oy] = true;
            _bfsAnX[ox, oy] = ax; _bfsAnY[ox, oy] = ay;
            _bfsAnAction[ox, oy] = action;
            _bfsEdgeAction[ox, oy] = edgeAction;
            _bfsParentX[ox, oy] = (short)parentX;
            _bfsParentY[ox, oy] = (short)parentY;
            _bfsQX[tail] = x; _bfsQY[tail] = y;
            tail = (tail + 1) & BFS_QMSK;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(DoorBreakProgress);
            binaryWriter.Write(DodgeTimer);
            binaryWriter.Write(ProjectileTimer);
            binaryWriter.Write(TeleportCountdown);
            binaryWriter.WriteVector2(TeleportTelegraph);

            binaryWriter.Write(Aggression);
            binaryWriter.Write(Patience);
            binaryWriter.Write(Cowardice);
            binaryWriter.Write(Adeptness);
            binaryWriter.Write(Swiftness);
            binaryWriter.Write(CastingSpeed);
            binaryWriter.Write(Strength);
            binaryWriter.Write(Agility);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            DoorBreakProgress = binaryReader.ReadInt32();
            DodgeTimer = binaryReader.ReadInt32();
            ProjectileTimer = binaryReader.ReadSingle();
            TeleportCountdown = binaryReader.ReadInt32();
            TeleportTelegraph = binaryReader.ReadVector2();

            Aggression = binaryReader.ReadSingle();
            Patience = binaryReader.ReadSingle();
            Cowardice = binaryReader.ReadSingle();
            Adeptness = binaryReader.ReadSingle();
            Swiftness = binaryReader.ReadSingle();
            CastingSpeed = binaryReader.ReadSingle();
            Strength = binaryReader.ReadSingle();
            Agility = binaryReader.ReadSingle();
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.KingSlime)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>()));
            }
            if (npc.type == NPCID.EyeofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            }
            if (npc.type == NPCID.BrainofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 4, 8));
            }
            if (npc.type == NPCID.Plantera)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.DukeFishron)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
            }
            if (npc.type == NPCID.Golem)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 2, 4));
            }
            if (npc.type == NPCID.HallowBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 3, 6));
            }
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 4, 8));
            }
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 5, 10));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 5, 10));
            }
        }
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            int playerX = (int)(Main.LocalPlayer.Center.X / 16f);
            int playerY = (int)(Main.LocalPlayer.Center.Y / 16f);
            Player player = spawnInfo.Player;

            //VANILLA AND SOME MOD NPC SPAWN EDITS

            //PRE-HARD MODE

            // Arazium's Mountain Caverns (not in water)
            if (!spawnInfo.Water && (spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneOverworldHeight) && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.DirtUnsafe1 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.DirtUnsafe2) && !Main.hardMode)
            {
                pool.Add(NPCID.Skeleton, 0.02f);
            }

            //jungle
            if (spawnInfo.Player.ZoneJungle && !Main.hardMode)
            {
                //pool.Add(the type of the npc, what chance you want it to spawn with);
                pool.Add(NPCID.LostGirl, 0.005f);
                pool.Add(NPCID.Salamander2, 0.03f);
            }
            //corrupt (not in water)
            if (spawnInfo.Player.ZoneCorrupt && !spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.CochinealBeetle, 0.02f);
                pool.Add(NPCID.GiantShelly, 0.02f);
            }
            //corrupt (in water)
            if (spawnInfo.Player.ZoneCorrupt && spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.Squid, 0.02f);
            }
            //crimson
            if (spawnInfo.Player.ZoneCrimson && !Main.hardMode)
            {
                pool.Add(NPCID.LacBeetle, 0.02f);
                pool.Add(NPCID.Drippler, 0.2f);
                pool.Add(NPCID.BloodCrawler, 0.002f);
                pool.Add(NPCID.BloodCrawlerWall, 0.002f);
            }
            //meteor
            if (spawnInfo.Player.ZoneMeteor && !Main.hardMode)
            {
                pool.Add(NPCID.GraniteFlyer, 0.4f);
                pool.Add(NPCID.Salamander4, 0.4f);
                pool.Add(NPCID.MeteorHead, 0.01f);
            }
            //graveyard
            if (spawnInfo.Player.ZoneGraveyard && !Main.hardMode)
            {
                pool.Add(NPCID.BigMisassembledSkeleton, 0.03f);
                pool.Add(NPCID.BoneThrowingSkeleton2, 0.03f);
            }

            //HARD MODE SECTION

            //golem temple
            if (spawnInfo.SpawnTileType == TileID.LihzahrdBrick && spawnInfo.Lihzahrd && Main.hardMode)
            {
                pool.Add(NPCID.DesertDjinn, 0.075f);
                pool.Add(NPCID.DiabolistWhite, 0.02f); //was 0.1
                pool.Add(ModContent.NPCType<Enemies.LothricSpearKnight>(), 0.08f);
                pool.Add(ModContent.NPCType<Enemies.LothricKnight>(), 0.08f);

            }

            //desert or underground desert and dungeon(shadow temple)
            if ((spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) && spawnInfo.Player.ZoneDungeon && Main.hardMode)
            {
                pool.Add(NPCID.DiabolistRed, 0.01f);
            }

            //machine temple (in water)
            if (spawnInfo.Water && playerY < 1430 && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 98 && Main.hardMode)
            {            
                // 98 = WallID.GreenDungeonSlabUnsafe
                
                    // Clear the existing pool to ensure no other NPCs can spawn
                    pool.Clear();

                    // Add specific NPCs back into the pool with their spawn weights
                    pool.Add(NPCID.GreenJellyfish, 10f);                  
                    pool.Add(ModContent.NPCType<Enemies.MutantToad>(), 2f);
                    pool.Add(ModContent.NPCType<Enemies.GhostOfTheDrowned>(), 2f);



            }
            //machine temple (not in water)
            if (!spawnInfo.Water && playerY < 1430 && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 98 && Main.hardMode)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.GhostOfTheDrowned>(), 3f);
                pool.Add(ModContent.NPCType<Enemies.MutantToad>(), 1f);

            }
            //sky
            if (spawnInfo.Player.ZoneSkyHeight && Main.hardMode)
            {
                pool.Add(NPCID.GoblinSummoner, 0.01f);
            }
            if (spawnInfo.Player.ZoneHallow && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.hardMode)
            {
                pool.Add(NPCID.Gastropod, 0.18f);
            }
            if (spawnInfo.Player.ZoneHallow && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.hardMode && !tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.Pixie, 0.40f);
                pool.Add(NPCID.Unicorn, 0.09f);
                pool.Add(NPCID.RainbowSlime, 0.01f);
            }
            //ocean water (outer thirds of the map)
            if (spawnInfo.Water && Main.hardMode && (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3))
            {
                pool.Add(NPCID.SandsharkHallow, 0.3f);
            }

            if (spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneRockLayerHeight && Main.hardMode)
            {
                pool.Add(NPCID.Derpling, 0.25f);
                pool.Add(NPCID.GiantFlyingFox, 0.25f);
            }

            //SUPER HARD MODE SECTION
            if (spawnInfo.Player.ZoneJungle && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.BoneLee, 0.05f);
            }

            //mushroom
            if (spawnInfo.Player.ZoneGlowshroom && tsorcRevampWorld.SuperHardMode) 
            {
                pool.Add(NPCID.StardustWormHead, 0.1f); //.1 is 3% 
                pool.Add(NPCID.StardustCellBig, 0.02f); //.5 is 16%
                pool.Add(NPCID.StardustJellyfishBig, 0.3f);
                pool.Add(NPCID.StardustSpiderBig, 0.6f);
                pool.Add(NPCID.StardustSoldier, 1f);
                pool.Add(NPCID.ShimmerSlime, 0.25f);
            }
            //underground 
            if (spawnInfo.Player.ZoneUnderworldHeight && !spawnInfo.Player.ZoneDungeon && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.SolarCrawltipedeHead, 0.002f);
                pool.Add(NPCID.SolarSroller, 0.4f); //.5 is 16%
                pool.Add(NPCID.SolarCorite, 0.01f);
                pool.Add(NPCID.SolarSpearman, 0.4f);
                pool.Add(NPCID.SolarDrakomire, 0.4f);
                pool.Add(NPCID.SolarSolenian, 0.6f); 
            }
            //catacombs
            if (spawnInfo.SpawnTileType == TileID.BoneBlock && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.NebulaBrain, 0.2f); //.1 is 3%
                pool.Add(NPCID.NebulaHeadcrab, 0.4f); //.1 is 3%
                pool.Add(NPCID.NebulaSoldier, 0.4f); //.1 is 3%
            }
            //spaceships or flesh background of crimson biome
            if ((spawnInfo.SpawnTileType == TileID.MartianConduitPlating || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.Flesh || spawnInfo.Player.ZoneCrimson) && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.VortexLarva, 2f); //.1 is 3%
            }
            // molten sky temple
            if (spawnInfo.Player.ZoneUnderworldHeight && (spawnInfo.SpawnTileType == TileID.MeteoriteBrick || spawnInfo.SpawnTileType == TileID.HeavenforgeBrick || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.HeavenforgeBrickWall) && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.VortexRifleman, 0.1f); //.1 is 3%
                pool.Add(NPCID.VortexHornet, 0.02f); //.5 is 16%
                pool.Add(NPCID.VortexSoldier, 0.3f);
                pool.Add(NPCID.Paladin, 0.6f);
            }
            if (tsorcRevampWorld.RemixMap) // If it is Remix Map
            {
                // wyvern mage prison (remix map)
                if (spawnInfo.Player.ZoneMeteor && (spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneOverworldHeight) && spawnInfo.Player.ZoneCorrupt && tsorcRevampWorld.SuperHardMode)
                {
                    pool.Add(NPCID.SolarCorite, 0.35f);
                }
                // great foundry (remix map)
                if ((spawnInfo.SpawnTileType == TileID.Cog || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.TinPlating) && tsorcRevampWorld.SuperHardMode)
                {
                    pool.Add(NPCID.SolarSolenian, 0.2f);
                    pool.Add(NPCID.HellArmoredBones, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesSpikeShield, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesMace, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesSword, 0.1f);
                }

                if ((spawnInfo.Water && spawnInfo.SpawnTileType == TileID.Coralstone || spawnInfo.SpawnTileType == TileID.ReefBlock || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.HallowUnsafe2) && Main.hardMode)
                {
                    pool.Add(NPCID.CreatureFromTheDeep, 0.6f);
                    pool.Add(NPCID.Shark, 0.6f);
                }
            }            

            bool invasion = Main.invasionType != 0;
            if (!tsorcRevampWorld.SuperHardMode && (player.Center.X > 82016 || player.Center.X < 74560 || player.Center.Y > 16000))
            {
                invasion = false;
            }

            if (spawnInfo.Player.ZoneTowerSolar || spawnInfo.Player.ZoneTowerNebula || spawnInfo.Player.ZoneTowerStardust || spawnInfo.Player.ZoneTowerVortex || spawnInfo.Player.ZoneOldOneArmy || invasion)
            {
                List<int> blockedNPCs = new List<int>();

                foreach (int id in pool.Keys)
                {
                    ModNPC modNPC = NPCLoader.GetNPC(id);

                    if (modNPC != null && modNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
                    {
                        blockedNPCs.Add(id);
                    }
                }

                foreach (int id in blockedNPCs)
                {
                    pool.Remove(id);
                }
            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
            }

            if ((playerX > 6083 && playerX < 6847 && playerY > 1664 && playerY < 1999) && Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.ObsidianBrickUnsafe && tsorcRevampWorld.RemixMap)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
            }

            if (tsorcRevampWorld.TheEnd)
            {
                pool.Clear(); //stop NPC spawns in The End 
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            // Added these intermediate variables to fix the spawn rate
            // Without them, the spawn rate changes will mostly be ignored by the conversion back to int
            // on every if statement assignment
            float trueSpawnRate = (float)spawnRate;
            float trueMaxSpawns = (float)maxSpawns;

            //reduces max spawns by 30% and rate by 50% until player exceeds 160 health
            if (player.statLifeMax2 <= 160)
            {
                trueSpawnRate = trueSpawnRate * 1.5f;
                trueMaxSpawns = trueMaxSpawns * 0.7f;
            }
            //reduces max spawns by 20% and spawn rate by 40% after 160 health
            if (player.statLifeMax2 > 160 && player.statLifeMax2 <= 200)
            {
                trueSpawnRate = trueSpawnRate * 1.4f;
                trueMaxSpawns = trueMaxSpawns * 0.8f;
            }
            //reduces max spawns by 10% and spawn rate by 30% from 200-400 health
            if (player.statLifeMax2 > 200 && player.statLifeMax2 <= 400)
            {
                trueSpawnRate = trueSpawnRate * 1.3f;
                trueMaxSpawns = trueMaxSpawns * 0.9f;
            }
            //only reduces spawn rate by 20% above 400 health
            if (player.statLifeMax2 > 400)
            {
                trueSpawnRate = trueSpawnRate * 1.2f;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff || player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()))
            {
                trueSpawnRate = 9999999;//Higher is less spawns
                trueMaxSpawns = 0;
            }

            //Peace candles do not activate if there is a) an invasion and b) the player is near the center of the world.
            if ((Main.invasionType == 0 || player.Center.X > 82016 || player.Center.X < 74560 || player.Center.Y > 16000))
            {
                if (player.HasBuff(BuffID.PeaceCandle))
                {
                    trueSpawnRate = 9999999;
                    trueMaxSpawns = 0;
                }
            }
            else
            {
                if (Main.invasionType == 1)
                {
                    player.buffImmune[BuffID.PeaceCandle] = true;
                    player.ZonePeaceCandle = false;
                    trueSpawnRate /= 2;
                    trueMaxSpawns *= 3;
                }
            }

            if (player.ZoneTowerSolar || player.ZoneTowerNebula || player.ZoneTowerStardust || player.ZoneTowerVortex)
            {
                trueSpawnRate /= 2;
                trueMaxSpawns = trueMaxSpawns * 1.5f;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().EnterTheAbyss)
            {
                trueSpawnRate /= 2;
                trueMaxSpawns *= 2;
            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                trueSpawnRate /= 10; //Origin of the Abyss. All spawns blocked other than Humanity Phantoms
            }

            spawnRate = (int)Math.Round(trueSpawnRate);
            maxSpawns = (int)Math.Round(trueMaxSpawns);
        }

        //vanilla npc changes moved to separate file

        public override void OnKill(NPC npc)
        {
            Player LocalPlayer = Main.LocalPlayer;

            if (npc.type == NPCID.Golem && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.EmpressOfLight.Forcefield"), Color.Cyan);
            }
            if (tsorcRevampWorld.RemixMap)
            {
                if ((npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerStardust ||
                    npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerSolar) &&
                    ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    defeatedPillars.Add(npc.type);

                    if (defeatedPillars.Contains(NPCID.LunarTowerVortex) &&
                        defeatedPillars.Contains(NPCID.LunarTowerStardust) &&
                        defeatedPillars.Contains(NPCID.LunarTowerNebula) &&
                        defeatedPillars.Contains(NPCID.LunarTowerSolar))
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.PillarsForcefield"), Color.Teal);
                    }
                }
            }
            if (npc.boss)
                {                  
                    foreach (Player player in Main.player)
                    {
                        if (!player.active) { continue; }
                        player.GetModPlayer<tsorcRevampPlayer>().bossMagnet = true;
                        player.GetModPlayer<tsorcRevampPlayer>().bossMagnetTimer = 300; //5 seconds of increased grab range, check GlobalItem::GrabStyle and GrabRange
                    }
                }

            if (Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
            {
                if (Main.rand.NextBool(2))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }

                if (Main.rand.NextBool(12))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }
            }

            #region Dark Souls & Consumable Souls Drops

            if (Soulstruck)
            {
                divisorMultiplier = 0.9f; //10% increase
            }

            if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss)
            { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)
                if (Main.masterMode)
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 20);
                }
                else
                if (Main.expertMode)
                { //npc.value is the amount of coins they drop
                    enemyValue = (int)npc.value / (divisorMultiplier * 25); //all enemies drop more money in expert mode, so the divisor is larger to compensate
                }
                else
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 15);
                }


                multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(Main.LocalPlayer);

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                #region Bosses drop souls once
                if (npc.boss)
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npc.type)))
                    {
                        DarkSoulQuantity = 0;
                    }
                    else
                    {
                        // check whether the SHM boss was killed
                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.EarthFiendLich>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()) /*|| npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()) gwyn CLOSES the abyss portal!*/
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.SHM.BossDeath"), Color.Orange); 
                        }

                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>())
                        {
                            tsorcRevampWorld.isHellkiteDragonDead = true;
                        }

                        if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && Main.invasionType == 0)
                        {
                            Main.StartInvasion();
                        }

                        if ((npc.type == ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && Main.invasionType == 0)
                        {
                            Main.StartInvasion(3);
                        }

                        tsorcRevampWorld.PopulatePairedBosses();
                        //Paired bosses have to have their slain entries work different
                        if (tsorcRevampWorld.PairedBosses.Contains(npc.type))
                        {
                            for (int i = 0; i < tsorcRevampWorld.PairedBosses.Count; i++)
                            {
                                if (tsorcRevampWorld.PairedBosses[i] == npc.type)
                                {
                                    int pairedNPCOffset = -1;
                                    if (i % 2 == 0)
                                    {
                                        pairedNPCOffset = 1;
                                    }

                                    //If the other boss is not alive, then add them both. If not, don't.
                                    if (!NPC.AnyNPCs(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]))
                                    {
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]), 1);
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
                        }
                    }
                }
                #endregion

                #region EoW drops souls in a unique way
                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)))
                {

                    DarkSoulQuantity = 24; //*72 for soul drops per eater, 1728 souls per one whole eater

                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }
                #endregion

                if (DarkSoulQuantity > 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f + (0.0005f * Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult);
                //Main.NewText(chance);

                if (!(npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.Creeper))
                {

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 15) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 55) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((enemyValue >= 150) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
            #endregion


            #region Event saving and custom drops code

            if (ScriptedEventOwner != null && ScriptedEventOwner.eventNPCs[ScriptedEventIndex].type == npc.type)
            {
                ScriptedEventOwner.eventNPCs[ScriptedEventIndex].killed = true;

                if (ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems != null)
                {
                    for (int i = 0; i < ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems.Count; i++)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems[i], ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootAmounts[i]);
                    }
                }

                bool oneAlive = false;
                foreach (EventNPC eventNPC in ScriptedEventOwner.eventNPCs)
                {
                    if (!eventNPC.killed)
                    {
                        oneAlive = true;
                    }
                }

                if (!oneAlive)
                {
                    if (ScriptedEventOwner.FinalNPCCustomDrops != null)
                    {
                        for (int i = 0; i < ScriptedEventOwner.FinalNPCCustomDrops.Count; i++)
                        {
                            Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.FinalNPCCustomDrops[i], ScriptedEventOwner.FinalNPCDropAmounts[i]);
                        }
                    }
                }
            }
            #endregion
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (DodgeTimer > 0)
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(BuffID.BetsysCurse) && tsorcRevampPlayer.DragonStonePotency)
            {
                modifiers.Defense -= 40 * DragonStone.Potency - 40;
            }
            if (npc.HasBuff(BuffID.Ichor) && tsorcRevampPlayer.DragonStonePotency)
            {
                modifiers.Defense -= 15 * DragonStone.Potency - 15;
            }
            if (Sundered && modifiers.DamageType == DamageClass.Magic)
            {
                modifiers.FinalDamage *= 1f + OrbOfFlame.MagicSunder / 100f;
            }
            if (AbyssalSinking)
            {
                modifiers.FinalDamage *= 1.09f;
            }
            if (Ignited)
            {
                modifiers.FlatBonusDamage += MagmaBreastplate.OnHitDmg;
            }
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().ConditionOverload)
            {
                float debuffCounter = 0;
                foreach (int buffType in npc.buffType)
                {
                    if (Main.debuff[buffType] && !(BuffID.Sets.IsATagBuff[buffType]))
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<MythrilRamDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ScorchingDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ShockedDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<SunburnDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<Heatstroke>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<Charmed>())
                    {
                        debuffCounter++;
                    }
                }
                if (debuffCounter > 1)
                {
                    modifiers.FinalDamage += debuffCounter * ConditionOverload.Dmg / 100f;
                }
            }
        }
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player projectileOwner = Main.player[projectile.owner];
            var modPlayerProjectileOwner = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            if (ProjectileID.Sets.IsAWhip[projectile.type])
            {
                modifiers.CritDamage -= (200f - modPlayerProjectileOwner.WhipCritDamage) / 100f;
            }
            float SummonTagDamageMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            SummonTagFlatDamage = 0f;
            SummonTagCriticalStrikeChance = 0;
            SummonTagScalingDamage = 0f;
            SummonTagArmorPenetration = 0f;
            #region Individual Whip debuff effects
            #region Modded Whips
            if (markedByRustedChain)
            {
                SummonTagArmorPenetration += RustedChain.SummonTagArmorPen;
            }
            //if(markedByUrumi) has no effect anymore, the whip is focused on dealing damage by itself and does not buff minions
            //if(markedByEnchantedWhip) only has a special effect
            //if(markedByDominatrix) only has a special effect
            //if(markedBySearingLash) has no effect anymore, the whip is focused on dealing damage by itself and does not buff minions
            if (markedByCrystalNunchaku && CrystalNunchakuProc)
            {
                SummonTagScalingDamage += CrystalNunchakuStacks * (CrystalNunchaku.MaxSummonTagScalingDamage / 10f) / 100f;
            }
            if (markedByPyrosulfate)
            {
                SummonTagFlatDamage += Pyrosulfate.SummonTagDamage;
            }
            if (markedByPyromethane)
            {
                SummonTagCriticalStrikeChance += Pyromethane.SummonTagCrit;
            }
            //if(markedByNightsCracker) is used by the whip itself
            //if (markedByPolarisLeash) only has a special effect
            //if (markedByDragoonLash) only has a special effect
            //if(markedByTerraFall) is used by the whip itself
            if (markedByTerraFallShard)
            {
                SummonTagFlatDamage += TerraFallItem.TagDmg;
                SummonTagCriticalStrikeChance += TerraFallItem.TagCrit;
            }
            if (markedByDetonationSignal)
            {
                SummonTagScalingDamage += DetonationSignal.SummonTagScalingDamage / 100f;
            }
            #endregion
            #region Vanilla Whips
            if (markedByLeatherWhip)
            {
                SummonTagFlatDamage += SummonerEdits.LeatherWhipTagDmg;
            }
            if (markedBySnapthorn)
            {
                SummonTagFlatDamage += SummonerEdits.SnapthornTagDmg;
            }
            if (markedBySpinalTap)
            {
                SummonTagFlatDamage += SummonerEdits.SpinalTapTagDmg;
            }
            if (markedByFirecracker)
            {
                SummonTagScalingDamage += SummonerEdits.FirecrackerScalingDmg / 100f;
            }
            if (markedByCoolWhip)
            {
                SummonTagFlatDamage += SummonerEdits.CoolWhipTagDmg;
            }
            if (markedByDurendal)
            {
                SummonTagFlatDamage += SummonerEdits.DurendalTagDmg;
            }
            if (markedByMorningStar)
            {
                SummonTagFlatDamage += SummonerEdits.MorningStarTagDmg;
                SummonTagCriticalStrikeChance += SummonerEdits.MorningStarTagCritChance;
            }
            if (markedByDarkHarvest)
            {
                SummonTagFlatDamage += SummonerEdits.DarkHarvestTagDmg;
            }
            if (markedByKaleidoscope)
            {
                SummonTagFlatDamage += SummonerEdits.KaleidoscopeTagDmg;
                SummonTagCriticalStrikeChance += SummonerEdits.KaleidoscopeTagCritChance;
            }
            #endregion
            #endregion
            #region Summon Tag Damage Calculation and Special Effects
            if (projectile.IsMinionOrSentryRelated)
            {
                #region Minion effects
                /*if (((Scorched || Shocked || Sunburnt) && (SuperScorchDuration > 0 || SuperShockDuration > 0 || SuperSunburnDuration > 0)) || Awestruck)
                {
                    SummonTagCriticalStrikeChance += ScorchingPoint.SummonTagCrit;
                }*/
                #endregion
                #region Modded Whip Special Effects
                //Crystal Nunchaku Effect located in ModifyIncomingHit
                //Dragoon Lash effect at the bottom
                if (markedByEnchantedWhip && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(EnchantedWhip.BaseDamage * EnchantedWhip.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(Main.rand.Next(641), -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(-Main.rand.Next(641), -800) + npc.Center;
                    int Move = Main.rand.Next(2);
                    switch (Move)
                    {
                        case 0:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, Vector2.One, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                            case 1:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, Vector2.One, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                    }
                }     
                if (markedByDominatrix && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int ThornDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(DominatrixItem.BaseDmg * DominatrixItem.ThornDmgScaling / 100f);
                    Vector2 ThornMovement1 = new Vector2(2, 0);
                    Vector2 ThornMovement2 = new Vector2(-2, 0);
                    Vector2 ThornMovement3 = new Vector2(0, 2);
                    Vector2 ThornMovement4 = new Vector2(0, -2);
                    Vector2 ThornMovement5 = new Vector2(2, 2);
                    Vector2 ThornMovement6 = new Vector2(-2, -2);
                    Vector2 ThornMovement7 = new Vector2(2, -2);
                    Vector2 ThornMovement8 = new Vector2(-2, 2);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement1, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement2, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement3, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement4, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement5, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement6, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement7, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement8, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                }
                if (markedByPolarisLeash && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(PolarisLeashItem.BaseDamage * PolarisLeashItem.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(Main.rand.Next(641), -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(-Main.rand.Next(641), -800) + npc.Center;
                    int Move = Main.rand.Next(2);
                    switch (Move)
                    {
                        case 0:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, Vector2.One, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                        case 1:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, Vector2.One, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                    }
                }
                if (markedByDetonationSignal) //Detonation Signal effect
                {
                    int buffIndex = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), npc.Top, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT2Explosion, 0, 0, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 1.5f, PitchVariance = 0.3f }, npc.Top);
                    npc.AddBuff(ModContent.BuffType<DetonationSignalBuff>(), DetonationSignal.BonusContactDamageDuration * 60);
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<DetonationSignalDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                #endregion
                #region Vanilla Whip Special Effects
                if (markedByFirecracker)
                {
                    int buffIndex = 0;
                    if (Main.myPlayer == projectileOwner.whoAmI)
                    {
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.FireWhipProj, 0, 0f, projectile.owner);
                    }
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<FirecrackerDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                if (markedByDarkHarvest)
                {
                    if (Main.myPlayer == projectileOwner.whoAmI)
                    {
                        Projectile DarkHarvestProj = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.ScytheWhipProj, (int)(20f * SummonTagDamageMultiplier), 0f, projectile.owner, 1f, npc.whoAmI);
                    }
                    Projectile.EmitBlackLightningParticles(npc);
                }
                if (markedByKaleidoscope)
                {
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.RainbowRodHit, new ParticleOrchestraSettings
                    {
                        PositionInWorld = npc.Center
                    });
                }
                #endregion

                modifiers.FlatBonusDamage += SummonTagFlatDamage * SummonTagDamageMultiplier * modPlayerProjectileOwner.SummonTagStrength;
                modifiers.ScalingBonusDamage += SummonTagScalingDamage * SummonTagDamageMultiplier;
                modifiers.ArmorPenetration += SummonTagArmorPenetration * modPlayerProjectileOwner.SummonTagStrength;
                FinalSummonCriticalStrikeChance = projectile.CritChance + (SummonTagCriticalStrikeChance * modPlayerProjectileOwner.SummonTagStrength);

                modPlayerProjectileOwner.OverCrit((int)FinalSummonCriticalStrikeChance, projectile.DamageType, ref modifiers, out CritColorTier, ProjectileID.Sets.IsAWhip[projectile.type], projectile, npc.Hitbox);

            }
            if (markedByDragoonLash && (projectile.IsMinionOrSentryRelated || ProjectileID.Sets.IsAWhip[projectile.type])) //has to be outside of the main if since this is supposed to also be procced on whip-hit
            {
                int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(DragoonLash.BaseDamage);
                if (projectileOwner.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer >= 1 && Main.myPlayer == projectileOwner.whoAmI)
                {
                    Projectile Fireball = Projectile.NewProjectileDirect(Projectile.GetSource_None(), projectileOwner.Center, (npc.Center - projectileOwner.Center) * 0.1f, ProjectileID.Flamelash, WhipDamage, 1f, Main.myPlayer, 1);
                }
            }
            if (markedBySupremeDragoonLash && (projectile.IsMinionOrSentryRelated || ProjectileID.Sets.IsAWhip[projectile.type])) //has to be outside of the main if since this is supposed to also be procced on whip-hit
            {
                int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(SupremeDragoonLash.BaseDamage);
                if (projectileOwner.GetModPlayer<tsorcRevampPlayer>().SupremeDragoonLashFireBreathTimer >= 1 && Main.myPlayer == projectileOwner.whoAmI)
                {
                    Projectile RgbFireball = Projectile.NewProjectileDirect(Projectile.GetSource_None(), projectileOwner.Center, (npc.Center - projectileOwner.Center) * 0.1f, ProjectileID.RainbowRodBullet, WhipDamage, 1f, Main.myPlayer, 1);
                }
            }
            #endregion

            #region BotC Whip Debuff Damage Scaling (disabled)
            /*int WhipDebuffCounter = 0;
            if (projectile.IsMinionOrSentryRelated && Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse &&
                  projectile.type != ProjectileID.DD2BallistraProj && projectile.type != ProjectileID.DD2ExplosiveTrapT1Explosion && projectile.type != ProjectileID.DD2ExplosiveTrapT2Explosion
                && projectile.type != ProjectileID.DD2ExplosiveTrapT3Explosion && projectile.type != ProjectileID.DD2FlameBurstTowerT1Shot && projectile.type != ProjectileID.DD2FlameBurstTowerT2Shot
                && projectile.type != ProjectileID.DD2FlameBurstTowerT3Shot | projectile.type != ProjectileID.DD2LightningAuraT1 && projectile.type != ProjectileID.DD2LightningAuraT2
                && projectile.type != ProjectileID.DD2LightningAuraT3 && projectile.type != ProjectileID.HoundiusShootiusFireball && projectile.type != ProjectileID.SpiderEgg && projectile.type != ProjectileID.BabySpider
                && projectile.type != ProjectileID.FrostBlastFriendly && projectile.type != ProjectileID.MoonlordTurretLaser && projectile.type != ProjectileID.RainbowCrystalExplosion
                && projectile.type != ModContent.ProjectileType<GaleForceProjectile>())
            {
                foreach (int buffType in npc.buffType)
                {
                    if (BuffID.Sets.IsAnNPCWhipDebuff[buffType])
                    {
                        WhipDebuffCounter++;
                    }
                }
                if (markedBySearingLash && modPlayerProjectileOwner.SearingLashStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByNightsCracker && modPlayerProjectileOwner.NightsCrackerStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByTerraFall && modPlayerProjectileOwner.TerraFallStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (WhipDebuffCounter > Darksign.WhipDebuffCounterCap)
                {
                    WhipDebuffCounter = Darksign.WhipDebuffCounterCap;
                }
                modifiers.FinalDamage *= 0.1f + (WhipDebuffCounter * Darksign.MinionDamageReductionDecrease / 100f);
            }*/
            #endregion
            if (npc.type == NPCID.DukeFishron && projectileOwner.wet)
            {
                modifiers.FinalDamage *= 2;
            }
            if (npc.type == NPCID.DukeFishron && projectileOwner.wet && projectile.DamageType == DamageClass.Melee)
            {
                modifiers.FinalDamage *= 2;
            }
        }
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (npc.type == NPCID.DukeFishron && player.wet)
            {
                modifiers.FinalDamage *= 4;
            }
            /*if (item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                int critLevel = (int)(Math.Floor(player.GetWeaponCrit(player.HeldItem) / 100f));
                if (Main.rand.Next(1, 101) <= player.GetWeaponCrit(player.HeldItem) - (100 * critLevel))
                {
                    modifiers.SetCrit();
                }
                if (critLevel >= 1)
                {
                    modifiers.SetCrit();
                    if (Main.rand.Next(1, 101) <= player.GetWeaponCrit(player.HeldItem) - (100 * critLevel))
                    {
                        modifiers.CritDamage += 1;
                    }
                }
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                    }
                }
            }*/
        }
        private static void TriggerNoLosPursuitBoost(NPC npc, Player player)
        {
            if (player == null || !player.active || player.dead)
            {
                return;
            }

            if (!Collision.CanHitLine(npc.Center, 1, 1, player.Center, 1, 1))
            {
                tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                globalNPC.FighterNoLosPursuitBoostTimer = 180;
                globalNPC.BoredTimer = 0;
                globalNPC.WeakTeleportReachTimer = 0;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TriggerNoLosPursuitBoost(npc, player);

            //If this hit takes it below 1/5th health, roll a chance to flee based on its Cowardice trait
            if (npc.life > npc.lifeMax / 5 && npc.life - damageDone < npc.lifeMax / 5)
            {
                if (Main.rand.NextFloat() < npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Cowardice && !npc.boss)
                {
                    Fleeing = true;
                }
            }

            if (!CrystalNunchakuProc && CrystalNunchakuStacks > 0 && markedByCrystalNunchaku)
            {
                CrystalNunchakuStacks -= 1;
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                TriggerNoLosPursuitBoost(npc, Main.player[projectile.owner]);
            }

            if (projectile.friendly && projectile.DamageType != DamageClass.Melee)
            {
                tsorcRevampGlobalNPC hitGlobalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                hitGlobalNPC.FighterRangedHitInterruptedPause = hitGlobalNPC.FighterPostAttackPauseTimer > 0
                                                              || hitGlobalNPC.FighterRangedStandShotsRemaining > 0;
                hitGlobalNPC.FighterPostAttackPauseTimer = 0;
                hitGlobalNPC.FighterRangedStandShotsRemaining = 0;
                hitGlobalNPC.BoredTimer = 0;
            }

            Player player = Main.player[projectile.owner];
            var modPlayer = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            if (projectile.IsMinionOrSentryRelated && CritColorTier > 0)
            {
                modPlayer.OverCritColor(npc.Hitbox, damageDone, CritColorTier);
            }
            //If this hit takes it below 1/5th health, roll a chance to flee based on its Cowardice trait
            if (npc.life > npc.lifeMax / 5 && npc.life - damageDone < npc.lifeMax / 5)
            {
                if (Main.rand.NextFloat() < npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Cowardice && !npc.boss)
                {
                    Fleeing = true;
                }
            }

            #region Vanilla Whips applying their modded counterparts
            if (projectile.type == ProjectileID.BlandWhip)
            {
                npc.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ThornWhip)
            {
                npc.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.ThornWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.BoneWhip)
            {
                npc.AddBuff(ModContent.BuffType<SpinalTapDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.FireWhip)
            {
                npc.AddBuff(ModContent.BuffType<FirecrackerDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.CoolWhip)
            {
                npc.AddBuff(ModContent.BuffType<CoolWhipDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.SwordWhip)
            {
                npc.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.MaceWhip)
            {
                npc.AddBuff(ModContent.BuffType<MorningStarDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ScytheWhip)
            {
                npc.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.ScytheWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.RainbowWhip)
            {
                npc.AddBuff(ModContent.BuffType<KaleidoscopeDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            #endregion
            #region Crystal Nunchaku effects
            if (!CrystalNunchakuProc && !(CrystalNunchakuStacks == 0) && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku && !projectile.IsMinionOrSentryRelated)
            {
                CrystalNunchakuStacks -= 1;
            }
            #endregion
            if (hit.DamageType == DamageClass.Ranged && damageDone > npc.life && modPlayer.BoneRing)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                }
            }
            #region Ranged Weapons
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain && (projectile.type == ModContent.ProjectileType<ToxicCatDetonator>() || projectile.type == ModContent.ProjectileType<ToxicCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetToxicCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<ToxicCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<ToxicCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<ToxicCatExplosion>(), (int)(projectile.damage * 1.8f), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ToxicCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }

                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<ToxicCatDetonator>() && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 300 * (tags / 12f), 45 * (tags / 12f));
                        }
                    }

                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ViruCatDrain && (projectile.type == ModContent.ProjectileType<VirulentCatDetonator>() || projectile.type == ModContent.ProjectileType<VirulentCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetViruCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<VirulentCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<VirulentCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        //Main.NewText(pitch);
                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<VirulentCatExplosion>(), (projectile.damage * 2), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ViruCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<VirulentCatDetonator>())
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400 * (tags / 12f), 50 * (tags / 12f));
                            }
                        }
                    }
                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain && (projectile.type == ModContent.ProjectileType<BiohazardDetonator>() || projectile.type == ModContent.ProjectileType<BiohazardExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetBiohazardBlobs = true;
                int tags;


                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<BiohazardShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<BiohazardShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<BiohazardExplosion>(), (projectile.damage * 2), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.BiohazardDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<BiohazardDetonator>() && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500 * (tags / 12f), 60 * (tags / 12f));
                        }
                    }
                }
            }
            #endregion
        }
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (DodgeTimer > 0)
            {
                return false;
            }
            return base.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (DodgeTimer > 0)
            {
                return false;
            }
            return base.CanBeHitByProjectile(npc, projectile);
        }
        Texture2D LionheartMarksSprite;
        Texture2D ScorchMarksSprite;
        Texture2D ShockMarksSprite;
        Texture2D SunburnMarksSprite;
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<LionheartMark>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks > 0)
            {
                LionheartMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Weapons/LionheartMarkVisual");
                Rectangle LionheartSourceRectangle = new Rectangle(0, 0 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks)
                {
                    case 1:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 4 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 2:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 3 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 3:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 2 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 4:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 1 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 5:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 0 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                }
                Main.EntitySpriteDraw(LionheartMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks * LionheartMarksSprite.Height / 5 - 100), LionheartSourceRectangle, Color.White, 0, LionheartSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks > 0)
            {
                ScorchMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/ScorchingMarkVisual");
                Rectangle ScorchingMarkSourceRectangle = new Rectangle(0, 0 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks)
                {
                    case 1:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 5 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 4 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 3 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 2 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 1 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 0 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                }
                Main.EntitySpriteDraw(ScorchMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ScorchMarksSprite.Height / 6 * ScorchMarks - 100), ScorchingMarkSourceRectangle, Color.White, 0, ScorchingMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks > 0)
            {
                ShockMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/ShockedMarkVisual");
                Rectangle ShockedMarkSourceRectangle = new Rectangle(0, 0 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks)
                {
                    case 1:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 5 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 4 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 3 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 2 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 1 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 0 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                }
                Main.EntitySpriteDraw(ShockMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ShockMarksSprite.Height / 6 * ShockMarks - 100), ShockedMarkSourceRectangle, Color.White, 0, ShockedMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks > 0)
            {
                SunburnMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/SunburntMarkVisual");
                Rectangle SunburnMarkSourceRectangle = new Rectangle(0, 0 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks)
                {
                    case 1:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 5 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 4 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 3 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 2 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 1 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 0 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6); 
                            break;
                        }
                }
                Main.EntitySpriteDraw(SunburnMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, SunburnMarksSprite.Height / 6 * SunburnMarks - 100), SunburnMarkSourceRectangle, Color.White, 0, SunburnMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }

            if (DodgeTimer > 0 && Main.GameUpdateCount % 10 < 5)
            {
                return false;
            }

            bool preDraw = base.PreDraw(npc, spriteBatch, screenPos, drawColor);

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            float staminaMax = (int)(300 * (1 - globalNPC.Agility));
            float staminaCurrent = staminaMax - globalNPC.DodgeCooldown;
            float staminaPercentage = (float)staminaCurrent / staminaMax;
            if (globalNPC.DodgeCooldown != 0)
            {
                float abovePlayer = 45f; //how far above the player should the bar be?
                Texture2D barFill = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_full");
                Texture2D barEmpty = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_empty");

                //this is the position on the screen. it should remain relatively constant unless the window is resized
                Point barOrigin = (npc.Center - new Vector2(barEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint();
                //Main.NewText("" + barOrigin.X + ", " + barOrigin.Y);

                Rectangle emptyDestination = new Rectangle(barOrigin.X, barOrigin.Y, barEmpty.Width, barEmpty.Height);

                //empty bar has detailing, so offset the filled bar's destination
                int padding = 5;
                //scale the width by the stam percentage
                Rectangle fillDestination = new Rectangle(barOrigin.X + padding, barOrigin.Y, (int)(staminaPercentage * barFill.Width), barFill.Height);

                Main.spriteBatch.Draw(barEmpty, emptyDestination, Color.White);
                Main.spriteBatch.Draw(barFill, fillDestination, Color.DodgerBlue);
            }
            return preDraw;
        }

        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                List<IItemDropRule> ruleList = globalLoot.Get();
                for (int i = 0; i < ruleList.Count; i++)
                {
                    string s = ruleList[i].ToString();
                    if (s == "Terraria.GameContent.ItemDropRules.MechBossSpawnersDropRule")
                    {
                        globalLoot.Remove(ruleList[i]);
                    }
                }
            }
        }

        public override bool PreKill(NPC npc)
        {
            for (int i = 0; i < tsorcRevamp.BannedItems.Count; i++)
            {
                NPCLoader.blockLoot.Add(tsorcRevamp.BannedItems[i]);
            }

            return base.PreKill(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            #region Dragon Stone Potency for Vanilla Buffs
            if (npc.HasBuff(BuffID.OnFire) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 4 * DragonStone.Potency - 4;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.OnFire3) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 15 * DragonStone.Potency - 15;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.CursedInferno) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 24 * DragonStone.Potency - 24;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Frostburn) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 8 * DragonStone.Potency - 8;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Frostburn2) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 25 * DragonStone.Potency - 25;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.ShadowFlame) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 15 * DragonStone.Potency - 15;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Poisoned) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 6 * DragonStone.Potency - 6;
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Venom) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 30 * DragonStone.Potency - 30;
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Daybreak) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = (100 * DragonStone.Potency - 100) / 2; //2x weaker with Daybreak
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            #endregion
            if (Ignited)
            {
                int DoTPerS = 20;
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        DoTPerS += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        DoTPerS += 25;
                    }
                }
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (Venomized)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)ToxicShot.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)ToxicShot.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Electrified)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)AlienGun.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)AlienGun.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Irradiated)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (IrradiatedByShroom)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage * 2.28f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage * 2.28f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Scorched)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(10);
                if (SuperScorchDuration > 0)
                {
                    DoTPerS *= 3;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Shocked)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(30);
                if (SuperShockDuration > 0)
                {
                    DoTPerS *= 3;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Sunburnt)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(110);
                if (SuperSunburnDuration > 0)
                {
                    DoTPerS *= 3;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Awestruck)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(440);
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (DarkInferno)
            {
                int DoTPerS = 20;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        DoTPerS += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        DoTPerS += 25;
                    }
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                var N = npc;
                for (int j = 0; j < 6; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (AbyssInferno)
            {
                int DoTPerS = 121;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency / 2;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        DoTPerS += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        DoTPerS += 25;
                    }
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                var N = npc;
                if (Main.rand.NextBool(7))
                {
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkTorch, 0f, 0f, 100, Color.Pink, 2f);
                    Main.dust[dustIndex].noGravity = false; 
                    Main.dust[dustIndex].velocity *= 1.05f;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }
                if (Main.rand.NextBool(8)) 
                { 
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, 223, 0f, 0f, 100, Color.Pink, 1.6f);
                    Main.dust[dustIndex].noGravity = true; 
                    Main.dust[dustIndex].velocity *= 0.99f;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }
            }

            if (WitchkingCurse)
            {
                int DoTPerS = 401;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= (DragonStone.Potency / 2);
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                npc.damage = (int)(npc.damage * 0.8f);
                npc.defense = Math.Max(0, npc.defDefense - 40);
                npc.velocity *= 0.95f;

                var N = npc;
            }

            if (AbyssalSinking)
            {
                npc.defense = Math.Max(0, npc.defDefense - 24);
                npc.velocity *= 0.98f;
            }

            if (CCShocked)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(100);
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                /*var N = npc;
                for (int j = 0; j < 6; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }*/
            }

            if (PhazonCorruption)
            {
                int DoTPerS = 21;
                if (npc.lifeRegen > 0)
                    {
                        npc.lifeRegen = 0;
                    }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * (tsorcRevampWorld.SuperHardMode ? 4 : 1);

                damage += DoTPerS * (tsorcRevampWorld.SuperHardMode ? 4 : 1); 

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 185, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust].noGravity = true;

                int dust2 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.FireworkFountain_Blue, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust2].noGravity = true;
            }

            if (CrimsonBurn)
            {
                int DoTPerS = 41;

                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }

                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        DoTPerS += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        DoTPerS += 25;
                    }
                }

                npc.lifeRegen -= DoTPerS * (Main.hardMode ? 2 : 1);

                damage += DoTPerS * (Main.hardMode ? 2 : 1); 

                npc.defense = Math.Max(0, npc.defDefense - 5);
                
                var N = npc;
                for (int j = 0; j < 5; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 5, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ToxicCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ToxicCatShotCount++;
                    }
                }
                if (ToxicCatShotCount >= 4)
                { //this is to make it worth the players time stickying more than 3 times
                    npc.lifeRegen -= ToxicCatShotCount * 3 * 3; //Use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ToxicCatShotCount * 2 * 3;
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
            }

            if (ViruCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ViruCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<VirulentCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ViruCatShotCount++;
                    }
                }
                if (ViruCatShotCount >= 4)
                {
                    npc.lifeRegen -= ViruCatShotCount * 3 * 5; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ViruCatShotCount * 2 * 5;
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
            }

            if (BiohazardDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int BiohazardShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<BiohazardShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        BiohazardShotCount++;
                    }
                }
                if (BiohazardShotCount >= 4)
                {
                    npc.lifeRegen -= BiohazardShotCount * 12 * 2; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= BiohazardShotCount * 9 * 2;
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
            }

            if (ElectrocutedEffect)
            {
                int DoTPerS = 6;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (ElectrocutedEffect2)
            {
                int DoTPerS = 36;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= (DragonStone.Potency / 2);
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            
            if (ElectrocutedEffect3)
            {
                int DoTPerS = 116;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= (DragonStone.Potency / 2);
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (PolarisElectrocutedEffect)
            {
                int DoTPerS = 35;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (CrescentMoonlight)
            {
                int DoTPerS = 26;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
                if (Main.hardMode)
                {
                    npc.lifeRegen -= DoTPerS * 2;
                    damage += DoTPerS;
                }
            }
        }

        public override void ModifyShop(NPCShop shop)
        {

            switch (shop.NpcType)
            {
                case NPCID.Merchant:
                    {
                        shop.Add(ItemID.Bottle);
                        break;
                    }
                case NPCID.DyeTrader:
                    {
                        //Basic dyes (most others can be crafted from a combination of these)
                        int price = 5;
                        shop.Add(new Item(ItemID.RedDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.OrangeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.YellowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.LimeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GreenDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.TealDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.CyanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.SkyBlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.VioletDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PinkDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlackDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        price = 25;

                        //Special Dyes (Aka the cool ones)
                        shop.Add(new Item(ItemID.FogboundDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.MushroomDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleOozeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveObsidianDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ShadowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MirageDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.TwilightDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BurningHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShadowflameHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.PhaseDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShiftingSandsDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GelDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingFlameDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingOceanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MidnightRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });


                        break;
                    }
                case NPCID.SkeletonMerchant:
                    {
                        shop.Add(new Item(ModContent.ItemType<Firebomb>())
                        {
                            shopCustomPrice = 5,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ModContent.ItemType<EternalCrystal>())
                        {
                            shopCustomPrice = 2000,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.GoblinTinkerer:
                    {
                        shop.Add(new Item(ModContent.ItemType<Pulsar>())
                        {
                            shopCustomPrice = 800,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });


                        shop.Add(new Item(ModContent.ItemType<ToxicCatalyzer>())
                        {
                            shopCustomPrice = 800,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.Dryad:
                    {
                        shop.Add(new Item(ModContent.ItemType<SeedBag>())
                        {
                            shopCustomPrice = 5,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        },
                        new Condition("", () => Main.LocalPlayer.HasItem(ItemID.Blowpipe) || Main.LocalPlayer.HasItem(ItemID.Blowgun) || Main.LocalPlayer.HasItem(ModContent.ItemType<ToxicShot>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<AlienGun>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<OmegaSquadRifle>())));
                        break;
                    }
                case NPCID.Mechanic:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                        }

                        shop.Add(new Item(ModContent.ItemType<LoveRing>())
                        {
                            shopCustomPrice = 5000, 
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.Cyborg:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            if (item.Item.type == ItemID.DryRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.WetRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.LavaRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.HoneyRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                        }
                        break;
                    }
                default: break;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            //Set the default value of each if it has not been custom-tuned
            if (npc.boss)
            {
                //Disables all of them by default for bosses
                //Can be re-enabled in that specific bosses SetDefaults by giving these another value there
                if (Aggression == -1)
                {
                    Aggression = 0.0000001f;
                }
                if (Patience == -1)
                {
                    Patience = 1;
                }
                if (Cowardice == -1)
                {
                    Cowardice = 0;
                }
                if (Adeptness == -1)
                {
                    Adeptness = 0;
                }
                if (Swiftness == -1)
                {
                    Swiftness = 1;
                }
                if (CastingSpeed == -1)
                {
                    CastingSpeed = 1;
                }
                if (Strength == -1)
                {
                    Strength = 1;
                }
                if (Agility == -1)
                {
                    Agility = 0;
                }
            }
            else
            {
                if (Aggression == -1)
                {
                    Aggression = Main.rand.NextFloat(0.00001f, 2.5f);
                }
                if (Patience == -1)
                {
                    Patience = Main.rand.NextFloat(0.5f, 2);
                }
                if (Cowardice == -1)
                {
                    Cowardice = Main.rand.NextFloat(0, 0.3f);
                }
                if (Adeptness == -1)
                {
                    Adeptness = Main.rand.NextFloat(0, 0.3f);
                }
                if (Swiftness == -1)
                {
                    Swiftness = Main.rand.NextFloat(0.7f, 1.3f);
                }
                if (CastingSpeed == -1)
                {
                    CastingSpeed = Main.rand.NextFloat(0.6f, 1.4f);
                }
                if (Strength == -1)
                {
                    Strength = Main.rand.NextFloat(0.85f, 1.2f);
                }
                if (Agility == -1)
                {
                    Agility = Main.rand.NextFloat(0.2f, 0.6f);
                }
            }

            //Only mess with it if it's one of our bosses
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (npc.boss && !Main.expertMode)
                {
                    //Bosses are 1.3x weaker in normal mode
                    //Doing it like this means we can simply set npc.lifeMax to exactly value we want their expert mode health to be, saving us a headache.
                    //Rounded, because casting to an int truncates it which causes slight inaccuracies later on
                    npc.lifeMax = (int)Math.Round(npc.lifeMax / 1.3f);
                }
                else
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode") && (npc.ModNPC.GetType() != typeof(NPCs.Bosses.SuperHardMode.Gwyn))
)
                    {
                        base.SetDefaults(npc);
                        npc.lifeMax = (int)(tsorcRevampWorld.SHMScale * npc.lifeMax);
                        npc.defense = (int)(tsorcRevampWorld.SubtleSHMScale * npc.defense);
                        npc.damage = (int)(tsorcRevampWorld.SubtleSHMScale * npc.damage);
                    }
                }
            }
        }

        //This method lets us scale the stats of NPC's in expert mode.
        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            if (Main.player[1] != null && Main.player[1].active && Main.player[1].name == "MPTestDummy")
            {
                numPlayers -= 1;
            }

            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                //Counter expert mode automatic scaling
                npc.lifeMax = (int)Math.Round(npc.lifeMax / 2f);

                //Add 70% to the boss's health per extra player
                //npc.lifeMax = (int)Math.Round(npc.lifeMax * (1f + (0.7f * ((float)bossLifeScale - 1f))));

                //Add our scaling
                npc.lifeMax = (int)(npc.lifeMax * (1f + ((numPlayers - 1f) * .4f))); // was .5
                return;
            }
        }
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            Player LocalPlayer = Main.LocalPlayer;
            if (npc.active && !npc.friendly && Main.rand.NextBool((int)(100f / OrbOfDeception.EssenceThiefOnKillChance)) && npc.life <= 0)
            {
                if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfDeception>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 0, 1);
                }
                else if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfFlame>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 1, 1);
                }
                else if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 2, 1);
                }
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ElectrocutedEffect)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }

            if (ElectrocutedEffect2)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .45f);
                Main.dust[dust].noGravity = true;
            }

            if (ElectrocutedEffect3)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .5f);
                Main.dust[dust].noGravity = true;
            }

            if (PolarisElectrocutedEffect)
            {
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(10))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (ViruCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (BiohazardDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, -2f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (WitchkingCurse)
            {
                drawColor = Color.OrangeRed;
                Lighting.AddLight(npc.position, 0.23f, 0.125f, 0.065f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 5, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), 1f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (AbyssalSinking)
            {
                Lighting.AddLight(npc.position, 0.015f, 0.155f, 0.165f);
                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 217, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), 1.8f); ;
                    Main.dust[dust].velocity *= 0.5f;
                    Main.dust[dust].noGravity = false;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CrescentMoonlight)
            {
                drawColor = Color.White;

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 164, npc.velocity.X * 0f, 0f, 100, default(Color), 1f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = false;
                Main.dust[dust].velocity += npc.velocity;
            }

            if (Soulstruck)
            {
                Lighting.AddLight(npc.Center, .4f, .4f, .850f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 68, 0, 0, 30, default(Color), 1.25f);
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                }
            }
        }

        //AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)

        /*
                 * A cleaned up (and edited) copy of Worm AI.

                 * headType/tailType : the type of the head, body, and tail of the worm, respectively.
                 * bodyTypes: An array of the body types. NOTE: Array must at least be as long as the body length - 2!
                 * wormLength : the total length of the worm.
                 * partDistanceAddon : and addon to the distance between parts of the worm.
                 * maxSpeed : the fastest the worm can accellerate to.
                 * gravityResist : how much resistance on the X axis the worm has when it is out of tiles. was 0.07f
                    //higher values cause the wvyern's 'gravity' towards the player to increase
                    //lower values basically == longer passes
                 * fly : If true, acts like a Wvyern.
                 * split : If true, worm will split when parts of it die.
                 * ignoreTiles : If true, Allows the worm to move outside of tiles as if it were in them. (ignored if fly is true)
                 * spawnTileDust : If true, worm will spawn tile dust when it digs through tiles.
                 * soundEffects : If true, will produce a digging sound when nearing the player.

                 * that array works like this: say you have a worm that is 5 segments long
                 * you would make the body array have 3 ids in it and they would go in order they would appear on the worm from the head
                 * the array *must* be 2 less than the total length of the worm or it will not work
        */


        //ai[0] = ID of piece behind it
        //ai[1] = ID of piece ahead of it
        //ai[2] = Relates to length of worms
        //ai[3] = ID of worm head
        //npc.localAI[0] = place in the queue to sync itself, used to spread the syncing out
        #region AIWorm
        public static void AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)
        {
            //Flip sprite so it's always facing the right way            
            if (npc.type == headType)
            {
                if (npc.velocity.X < 0f || Math.Abs(npc.velocity.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                else if (npc.velocity.X > 0f)
                {
                    npc.spriteDirection = -1;
                }
            }
            else
            {
                if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X || Math.Abs(npc.position.X - Main.npc[(int)npc.ai[1]].position.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
                {
                    npc.spriteDirection = -1;
                }
            }
            //If it splits, ignore the health of the head and keep its own healthbar
            //If it doesn't, set its real health to the health of the head
            if (split)
            {
                npc.realLife = -1;
            }
            else if (npc.ai[3] > 0f)
            {
                npc.realLife = (int)npc.ai[3];
            }

            //Don't do *any* spawning if we're a multiplayer client
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Tick down the sync counter, and if it hits 1 then sync them.
                if (npc.localAI[0] == 1 && npc.localAI[0] > 0)
                {
                    npc.netUpdate = true;
                    npc.localAI[0] = -1;
                }
                else
                {
                    npc.localAI[0]--;
                }

                //And the piece behind it does not exist
                if (npc.ai[0] == 0f)
                {
                    //If we're a head and flying type, spawn the rest of the worm
                    if (fly && npc.type == headType)
                    {
                        //Set its the head's head id, actual health, and ID to itself
                        npc.ai[3] = (float)npc.whoAmI;
                        npc.realLife = npc.whoAmI;

                        //Store the head's index in npcID. This will get updated as we go through each piece.
                        int npcID = npc.whoAmI;

                        //Spawn the rest of the worm. For each piece...
                        for (int m = 0; m < wormLength - 1; m++)
                        {
                            //If we're the last piece, make the worm type the tail. If not, make it the body type corrosponding to its position on the list
                            int npcType = (m == wormLength - 2 ? tailType : bodyTypes[m]);

                            //Spawn the npc
                            int newnpcID = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), npcType, npc.whoAmI);

                            //Set the new piece's Head ID to the head
                            Main.npc[newnpcID].ai[3] = (float)npc.whoAmI;

                            //Set its real health to the head's
                            Main.npc[newnpcID].realLife = npc.whoAmI;

                            //Set its "previous piece id" to the id of the previous spawned piece
                            Main.npc[newnpcID].ai[1] = (float)npcID;

                            //Set the previous piece's "next piece id" to the id of the newly spawned piece
                            Main.npc[npcID].ai[0] = (float)newnpcID;

                            //Set their localAI to a number that grows as each segment is spawned
                            Main.npc[npcID].localAI[0] = 2 + (m * 2);

                            //Ask the server to sync it right away (might be triggering the net spam limit and causing the issues!!)
                            //NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newnpcID);

                            //Store the current piece's ID in npcID, so that the next piece can use it
                            npcID = newnpcID;
                        }
                        //Immediately update
                        npc.netUpdate = true;
                    }
                    //If we're a grounded type and not the tail, just spawn the piece behind itself
                    else if (npc.type != tailType)
                    {
                        if (npc.type == headType)
                        {
                            if (!split)
                            {
                                npc.ai[3] = (float)npc.whoAmI;
                                npc.realLife = npc.whoAmI;
                            }
                            npc.ai[2] = (float)(wormLength - 2);
                            int nextPiece = (bodyTypes.Length == 0 ? tailType : bodyTypes[0]);
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), nextPiece, npc.whoAmI);
                        }
                        else
                        if ((npc.type != headType && npc.type != tailType) && npc.ai[2] > 0f)
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), bodyTypes[wormLength - 3 - (int)npc.ai[2]], npc.whoAmI);
                        }
                        else
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), tailType, npc.whoAmI);
                        }
                        if (!split)
                        {
                            Main.npc[(int)npc.ai[0]].ai[3] = npc.ai[3];
                            Main.npc[(int)npc.ai[0]].realLife = npc.realLife;
                        }
                        Main.npc[(int)npc.ai[0]].ai[1] = (float)npc.whoAmI;
                        Main.npc[(int)npc.ai[0]].ai[2] = npc.ai[2] - 1f;
                        npc.netUpdate = true;
                    }
                }

                //if npc can split, check if pieces are dead and if so split.
                if (split)
                {
                    //If the piece in front and behind it are dead, then die too
                    if (!Main.npc[(int)npc.ai[1]].active && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a head and the piece behind it dies, then die
                    if (npc.type == headType && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a tail and the piece in front of it dies, then die
                    if (npc.type == tailType && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If the piece isn't a head or tail, and the piece in front of it dies, then become a head
                    if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.type = headType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[0];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[0] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }

                    //If the piece isn't a head or tail, and the piece behind it dies, then become a head
                    else if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.type = tailType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[1];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[1] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }
                }

                //If it can't split, die if it is incomplete 
                else
                {
                    //If it's not a head and the piece in front of it is dead (or the wrong aiStyle, just in-case a new npc took its slot) then die
                    if (npc.type != headType && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }

                    //If it's not a tail and the piece behind it is dead then die
                    if (npc.type != tailType && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }
                }
                /**
                if (!npc.active && Main.netMode == NetmodeID.Server) 
                { 
                    NetMessage.SendData(28, -1, -1, "", npc.whoAmI, 1, 0f, 0f, -1); 
                }**/
            }
            int tileX = (int)(npc.position.X / 16f) - 1;
            int tileCenterX = (int)((npc.Center.X) / 16f) + 2;
            int tileY = (int)(npc.position.Y / 16f) - 1;
            int tileCenterY = (int)((npc.Center.Y) / 16f) + 2;
            if (tileX < 0) { tileX = 0; }
            if (tileCenterX > Main.maxTilesX) { tileCenterX = Main.maxTilesX; }
            if (tileY < 0) { tileY = 0; }
            if (tileCenterY > Main.maxTilesY) { tileCenterY = Main.maxTilesY; }
            bool canMove = false;
            if (fly || ignoreTiles) { canMove = true; }


            if (!canMove || spawnTileDust)
            {
                for (int tX = tileX; tX < tileCenterX; tX++)
                {
                    for (int tY = tileY; tY < tileCenterY; tY++)
                    {
                        if (Main.tile[tX, tY] != null && ((Main.tile[tX, tY].HasTile && (Main.tileSolid[(int)Main.tile[tX, tY].TileType] || (Main.tileSolidTop[(int)Main.tile[tX, tY].TileType] && Main.tile[tX, tY].TileFrameY == 0))) || Main.tile[tX, tY].LiquidAmount > 64))
                        {
                            Vector2 tPos;
                            tPos.X = (float)(tX * 16);
                            tPos.Y = (float)(tY * 16);
                            if (npc.position.X + (float)npc.width > tPos.X && npc.position.X < tPos.X + 16f && npc.position.Y + (float)npc.height > tPos.Y && npc.position.Y < tPos.Y + 16f)
                            {
                                canMove = true;
                                if (spawnTileDust && (Main.rand.Next(100)) == 0 && Main.tile[tX, tY].HasTile)
                                {
                                    WorldGen.KillTile(tX, tY, true, true, false);
                                }
                            }
                        }
                    }
                }
            }


            if (!canMove && npc.type == headType)
            {
                Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                int playerCheckDistance = 1000;
                bool canMove2 = true;
                for (int m3 = 0; m3 < 255; m3++)
                {
                    if (Main.player[m3].active)
                    {
                        Rectangle rectangle2 = new Rectangle((int)Main.player[m3].position.X - playerCheckDistance, (int)Main.player[m3].position.Y - playerCheckDistance, playerCheckDistance * 2, playerCheckDistance * 2);
                        if (rectangle.Intersects(rectangle2))
                        {
                            canMove2 = false;
                            break;
                        }
                    }
                }
                if (canMove2) { canMove = true; }
            }



            Vector2 npcCenter = npc.Center;
            float playerCenterX = Main.player[npc.target].Center.X;
            float playerCenterY = Main.player[npc.target].Center.Y;
            playerCenterX = (float)((int)(playerCenterX / 16f) * 16); playerCenterY = (float)((int)(playerCenterY / 16f) * 16);
            npcCenter.X = (float)((int)(npcCenter.X / 16f) * 16); npcCenter.Y = (float)((int)(npcCenter.Y / 16f) * 16);
            playerCenterX -= npcCenter.X; playerCenterY -= npcCenter.Y;
            float dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {

                npcCenter = npc.Center;
                float offsetX = Main.npc[(int)npc.ai[1]].Center.X - npcCenter.X;
                float offsetY = Main.npc[(int)npc.ai[1]].Center.Y - npcCenter.Y;

                npc.rotation = (float)Math.Atan2((double)offsetY, (double)offsetX) + 1.57f;
                dist = (float)Math.Sqrt((double)(offsetX * offsetX + offsetY * offsetY));
                dist = (dist - (float)npc.width - (float)partDistanceAddon) / dist;
                offsetX *= dist;
                offsetY *= dist;
                npc.velocity = default(Vector2);
                npc.position.X += offsetX;
                npc.position.Y += offsetY;
            }
            else
            {
                if (!canMove)
                {
                    npc.velocity.Y += 0.11f;
                    if (npc.velocity.Y > maxSpeed) { npc.velocity.Y = maxSpeed; }
                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.4)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X -= gravityResist * 1.1f; } else { npc.velocity.X += gravityResist * 1.1f; }
                    }
                    else
                    if (npc.velocity.Y == maxSpeed)
                    {
                        if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                        else
                        if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                    }
                    else
                    if (npc.velocity.Y > 4f)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X += gravityResist * 0.9f; } else { npc.velocity.X -= gravityResist * 0.9f; }
                    }
                }
                else
                {
                    if (soundEffects && npc.soundDelay == 0)
                    {
                        float distSoundDelay = dist / 40f;
                        if (distSoundDelay < 10f) { distSoundDelay = 10f; }
                        if (distSoundDelay > 20f) { distSoundDelay = 20f; }
                        npc.soundDelay = (int)distSoundDelay;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    }
                    dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
                    float absPlayerCenterX = Math.Abs(playerCenterX);
                    float absPlayerCenterY = Math.Abs(playerCenterY);
                    float newSpeed = maxSpeed / dist;
                    playerCenterX *= newSpeed;
                    playerCenterY *= newSpeed;
                    bool dontFall = false;
                    if (fly)
                    {
                        if (((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f) || (npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > gravityResist / 2f && dist < 300f)
                        {
                            dontFall = true;
                            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed) { npc.velocity *= 1.1f; }
                        }
                    }
                    if (!dontFall)
                    {
                        if ((npc.velocity.X > 0f && playerCenterX > 0f) || (npc.velocity.X < 0f && playerCenterX < 0f) || (npc.velocity.Y > 0f && playerCenterY > 0f) || (npc.velocity.Y < 0f && playerCenterY < 0f))
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist; }
                            if ((double)Math.Abs(playerCenterY) < (double)maxSpeed * 0.2 && ((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f)))
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist * 2f; } else { npc.velocity.Y -= gravityResist * 2f; }
                            }
                            if ((double)Math.Abs(playerCenterX) < (double)maxSpeed * 0.2 && ((npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)))
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist * 2f; } else { npc.velocity.X -= gravityResist * 2f; }
                            }
                        }
                        else
                        if (absPlayerCenterX > absPlayerCenterY)
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist * 1.1f; }

                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist; } else { npc.velocity.Y -= gravityResist; }
                            }
                        }
                        else
                        {
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist * 1.1f; }
                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist; } else { npc.velocity.X -= gravityResist; }
                            }
                        }
                    }
                }
                npc.rotation = (float)Math.Atan2((double)npc.velocity.Y, (double)npc.velocity.X) + 1.57f;
                if (npc.type == headType)
                {
                    if (canMove)
                    {
                        if (npc.localAI[0] != 1f) { npc.netUpdate = true; }
                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f) { npc.netUpdate = true; }
                        npc.localAI[0] = 0f;
                    }
                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                    {
                        npc.netUpdate = true;
                        return;
                    }
                }
            }
        }

        #endregion
        /*public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var TownAnimals = new HashSet<int> {NPCID.TownDog, NPCID.TownCat, NPCID.TownBunny};
            Texture2D Hat = null;
            if (Main.xMas)
            {
                Hat = Main.Assets.Request<Texture2D>("Images/Item_588").Value;
            }
            if (npc.townNPC && !TownAnimals.Contains(npc.type))
            {
                Vector2 position = npc.Center - screenPos - new Vector2(0f, (npc.height / 2));//
                position -= new Vector2(0f, 4f);
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (npc.direction == -1)
                {
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }
                spriteBatch.Draw(Hat, position, null, Color.White, 0f, new Vector2(Hat.Width / 2, Hat.Height / 2), npc.scale, spriteEffects, 0f);
            }
        }*/
    }

    ///<summary> 
    ///Handles boss despawning and targeting.
    ///This exists to simplify AI code.
    ///Create an instance of this class in SetDefaults, call targetAndDespawn(npcID) at the start of their AI, and removing any existing targeting or despawning.
    ///</summary>
    public class NPCDespawnHandler
    {
        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="despawnFlavorText">The custom text this boss displays when it despawns</param>
        ///<param name="textColor">The color of the despawn text</param>
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        ///<param name="range">The boss will despawn if any player gets further away than this. -1 means infinite range.</param>
        public NPCDespawnHandler(string despawnFlavorText, Color textColor, int DustType, float range = -1)
        {
            despawnText = despawnFlavorText;
            despawnTextColor = textColor;
            despawnDustType = DustType;

            if (range > 0) //Pre-emptively square it so we don't have to do so later
            {
                range *= range;
            }
            despawnRange = range;
        }

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        ///<param name="range">The boss will despawn if any player gets further away than this. -1 means infinite range.</param>
        public NPCDespawnHandler(int DustType, float range = -1)
        {
            despawnDustType = DustType;
            if (range > 0)
            {
                range *= range;
            }
            despawnRange = range;
        }

        readonly string despawnText;
        readonly Color despawnTextColor;
        readonly int despawnDustType;
        bool hasTargeted = false;
        int targetCount = 0;
        readonly int[] targetIDs = new int[256];
        readonly bool[] targetAlive = new bool[256];
        int despawnTime = -1;
        float despawnRange;
        int OutOfBoundsTimer = 600;

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary>         
        ///<param name="npcID">The ID of the NPC in question.</param>
        public bool TargetAndDespawn(int npcID)
        {

            //When despawning, we set timeLeft to 240. If that's been done, we don't need to check for players or target anyone anymore.
            if (despawnTime < 0)
            {
                //Only run this once. Gets all active players and throws them into these arrays so we can track their status.
                if (!hasTargeted)
                {
                    foreach (Player player in Main.player)
                    {
                        //For some reason, Main.player always has 255 entries. This ensures we're only pulling real players from it.
                        if (player.active && player.name != "MPTestDummy")
                        {
                            targetIDs[targetCount] = player.whoAmI;
                            targetAlive[targetCount] = true;
                            targetCount++;
                        }
                    }
                    hasTargeted = true;
                }


                //Go through the target list. If everyone has died once, despawn. Else, target the closest one that has not yet died.
                //It's important that it only targets players who haven't died, because otherwise one living player could hide far away while the other repeatedly respawned and fought the boss.
                //With this, it will intentionally seek out those it has not yet killed instead.
                bool viableTarget = false;
                float closestPlayerDistance = float.MaxValue;
                float oldTarget = Main.npc[npcID].target;
                bool foundOutOfBoundsPlayer = false;

                //Iterate through all tracked players in the array
                for (int i = 0; i < targetCount; i++)
                {
                    //For each of them, check if they're dead. If so, mark it down in targetAlive.
                    if (Main.player[targetIDs[i]].dead && targetAlive[i])
                    {
                        targetAlive[i] = false;
                    }
                    else if (targetAlive[i] && Main.player[targetIDs[i]].active)
                    {
                        //If it found a player that hasn't been killed yet, then don't despawn
                        viableTarget = true;
                        //Check if they're the closest one, and if so target them
                        float distance = Vector2.DistanceSquared(Main.player[targetIDs[i]].position, Main.npc[npcID].position);
                        if (distance < closestPlayerDistance)
                        {
                            closestPlayerDistance = distance;
                            Main.npc[npcID].target = targetIDs[i];
                        }
                        if (despawnRange > 0 && !foundOutOfBoundsPlayer && Vector2.DistanceSquared(Main.player[targetIDs[i]].Center, tsorcRevampWorld.BossIDsAndCoordinates[Main.npc[npcID].type]) * 16 > despawnRange)
                        {
                            if (OutOfBoundsTimer == 600)
                            {
                                UsefulFunctions.BroadcastText(Main.npc[npcID].TypeName + " " + LangUtils.GetTextValue("NPCs.BossOutOfRange"), Color.Yellow);
                            }
                            OutOfBoundsTimer--;

                            //If players have been out of bounds for more than 10 seconds, then despawn the boss
                            if (OutOfBoundsTimer == 0)
                            {
                                for (int j = 0; j < targetAlive.Length; j++)
                                {
                                    targetAlive[j] = false;
                                }
                            }
                            foundOutOfBoundsPlayer = true;
                        }
                    }
                }

                //If a npc changes targets, sync it
                if (oldTarget != Main.npc[npcID].target)
                {
                    Main.npc[npcID].netUpdate = true;
                }

                //If there's no player that has not yet died, then despawn.
                if (!viableTarget)
                {
                    if (despawnText != null)
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Player.AllDied"), Color.Yellow);
                        UsefulFunctions.BroadcastText(despawnText, despawnTextColor);
                    }
                    despawnTime = 240;
                }
            }
            else
            {
                //Adios
                if (despawnTime == 0)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                        Main.dust[dustID].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        UsefulFunctions.DespawnFlash(Main.npc[npcID].Center);
                    }
                    Main.npc[npcID].active = false;
                }
                else
                {
                    int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 1f);
                    Main.dust[dustID].noGravity = true;
                    despawnTime--;
                }

                //The frame before despawning, we return true to let the NPC's AI know it's about to get despawned. This allows it to do anything it needs to with that information (like re-actuating the pyramid)
                if (despawnTime == 1)
                {
                    return true;
                }
            }
            return false;
        }


    }


    public static class tsorcRevampAIs
    {
        ///<summary> 
        ///Walking AI that walks toward the player. Can be used with SimpleProjectile to fire projectiles, or LeapAtPlayer to leap when the player is close
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="doorBreakingDamage">Setting this above 0 lets the npc break doors, and sets much damage should it deal when it hits them. Doors have 10 "health"</param>
        ///<param name="hatesLight">Should it run away during daylight?</param>
        ///<param name="randomSound">What sound should it randomly play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Accelerates twice as fast when below this % health</param> 
        ///<param name="enrageTopSpeed">Its new top speed when enraged</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        public static void FighterAI(NPC npc, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true)
        {
            npc.aiStyle = -1;
            BasicAI(npc, topSpeed, acceleration, brakingPower, false, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping, canDodgeroll, canPounce);
        }

        ///<summary> 
        ///Special version of the fighter ai, stopping to shoot when the player is within range. Gets bored if it doesn't have line of sight to the player, and if it can teleport it will attempt to warp to a position with a clean shot.
        ///Uses npc.ai[2] to control aim direction!! Do not set it yourself if an NPC uses ArcherAI
        ///</summary>         
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="projectileType">The ID of the projectile you want to shoot</param>
        ///<param name="projectileDamage">Damage of the projectile. Multiplied by 2 by default, and then 2 again in expert mode</param>
        ///<param name="projectileVelocity">Speed of the projectile</param>
        ///<param name="projectileCooldown">Sets the delay (in ticks) between shots</param>
        ///<param name="topSpeed">The max speed it can run at</param>
        ///<param name="acceleration">How quickly it can speed up</param>
        ///<param name="brakingPower">How quickly it can slow down</param>
        ///<param name="canTeleport">Lets it teleport near the player when it gets bored instead of walking around randomly</param>
        ///<param name="hatesLight">Should it run away during daylight? (UNIMPLEMENTED!)</param>
        ///<param name="shootSound">What sound should it play?</param>
        ///<param name="soundFrequency">How often does it play its sound?</param>
        ///<param name="enragePercent">Below this percent health, doubles speed and acceleration</param>
        ///<param name="lavaJumping">Lets it hop around in lava</param>
        ///<param name="projectileGravity">How much is the projectile's y velocity reduced each tick? Set 0 for projectiles with no gravity. If your projectile has custom gravity dropoff, stick that here.</param>
        ///<param name="shootSound">The type of sound to play when it shoots. Defaults to bow.</param>
        public static void ArcherAI(NPC npc, int projectileType, int projectileDamage, float projectileVelocity, int projectileCooldown, float topSpeed = 1f, float acceleration = .07f, float brakingPower = .2f, bool canTeleport = false, int doorBreakingDamage = 4, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercent = 0, float enrageTopSpeed = 0, bool lavaJumping = false, float projectileGravity = 0.035f, SoundStyle? shootSound = null, bool canDodgeroll = true, bool canPounce = false, Color? telegraphColor = null)
        {
            BasicAI(npc, topSpeed, acceleration, brakingPower, true, canTeleport, doorBreakingDamage, hatesLight, randomSound, soundFrequency, enragePercent, enrageTopSpeed, lavaJumping, canDodgeroll, false);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (telegraphColor == null)
            {
                telegraphColor = Color.Gray;
            }

            //Set default shoot sound
            if (shootSound == null)
            {
                shootSound = SoundID.Item5;
            }

            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        projectileDamage = (int)(tsorcRevampWorld.SHMScale * projectileDamage);
                        projectileVelocity = (int)(tsorcRevampWorld.SubtleSHMScale * projectileVelocity);
                    }
                }
            }

            npc.aiStyle = -1;
            if (npc.confused)
            {
                globalNPC.ArcherAimDirection = 0f; // won't try to stop & aim if confused
            }
            else
            {
                if (globalNPC.ProjectileTimer > 0f)
                    globalNPC.ProjectileTimer -= 1f; // decrement fire & reload counter

                // Don't let airborne state abort a shot once the telegraph has already fired.
                // Nav-tiered recovery states (waypoints / ledge run-up) need to keep counting
                // so the shaman can finish a pathing escape and still reach the telegraph window.
                bool inTelegraphWindow = globalNPC.ProjectileTimer <= (projectileCooldown / 2 + 15) && globalNPC.ProjectileTimer > (projectileCooldown / 2);
                bool pathRecoveryActive = globalNPC.NavigationTier >= 1 && (globalNPC.WaypointTimer > 0 || globalNPC.LedgeRunUpTimer > 0);
                if (npc.justHit || (npc.velocity.Y != 0f && !inTelegraphWindow && !pathRecoveryActive) || globalNPC.ProjectileTimer <= 0f)
                {
                    globalNPC.ProjectileTimer = (int)(projectileCooldown * globalNPC.CastingSpeed); //Reset firing time
                    globalNPC.ArcherAimDirection = 0f; //Not aiming
                    // If standing-fire has remaining shots and we're only resetting due to cooldown,
                    // immediately re-enter aiming state for the next volley shot.
                    if (!npc.justHit && globalNPC.FighterRangedStandShotsRemaining > 0)
                        globalNPC.ArcherAimDirection = 3f;
                }

                //Check if we're in range of and can hit the player
                if (!globalNPC.CanPassThroughWalls && Vector2.Distance(npc.Center, Main.player[npc.target].Center) < 700f && Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1) && npc.velocity.Y == 0)
                {
                    //If so, set boredom to 0
                    globalNPC.BoredTimer = 0;

                    //If it's not aiming yet, then slow down, aim, and start its cooldown
                    if (globalNPC.ArcherAimDirection == 0)
                    {
                        //Aim at them, and start the shot cooldown
                        npc.velocity.X *= 0.5f;
                        globalNPC.ArcherAimDirection = 3f;
                        globalNPC.ProjectileTimer = (int)(projectileCooldown * globalNPC.CastingSpeed);

                        // Standing-fire roll: tier-2 NPCs may plant their feet and fire N shots
                        // before resuming pursuit. High Aggression skips this; high Patience adds shots.
                        if (globalNPC.CanStopToFire && globalNPC.NavigationTier >= 2 && globalNPC.FighterRangedStandShotsRemaining == 0
                            && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float stopBeforeChance = GetStandingFireChance(globalNPC, 0.1f);
                            if (Main.rand.NextFloat() < stopBeforeChance)
                            {
                                // 1 shot at low Patience, up to 3 shots at high Patience
                                globalNPC.FighterRangedStandShotsRemaining = 1 + Main.rand.Next(0, 1 + (int)globalNPC.Patience);
                            }
                        }
                    }

                    // Standing-fire: fully pin velocity so the NPC holds position and
                    // shows a standing animation frame rather than a walk/jump frame.
                    if (globalNPC.FighterRangedStandShotsRemaining > 0)
                    {
                        npc.velocity.X = 0f;
                        npc.velocity.Y = 0f;
                    }
                    else
                    {
                        npc.velocity.X *= 0.9f; // decelerate to stop & shoot
                        npc.velocity.Y = 0f;    // suppress jump-frame animation while aiming
                    }
                    npc.spriteDirection = npc.direction; // match animation to facing

                    // Telegraph fires 15 ticks before the shot: lock the aim direction now so
                    // a dodge-roll behind the enemy can't redirect the incoming projectile.
                    if (globalNPC.ProjectileTimer - 15 == (projectileCooldown / 2))
                    {
                        globalNPC.LockedShotVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, projectileVelocity, projectileGravity);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawnPosition = npc.position;
                            if (npc.direction == 1)
                            {
                                spawnPosition.X += npc.width;
                            }
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, npc.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(telegraphColor.Value));
                        }
                    }

                    //Fire at halfway through: first half of delay is aim, 2nd half is cooldown
                    if (globalNPC.ProjectileTimer == (projectileCooldown / 2))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Spawn the shot just in front of the NPC rather than at its center. These projectiles
                            // are spawned player-owned (friendly) and flipped hostile a frame later, so a spawn
                            // overlapping the shooter's own hitbox lets the still-friendly projectile damage it.
                            Vector2 shotDir = globalNPC.LockedShotVector.SafeNormalize(new Vector2(npc.direction, 0f));
                            Vector2 shotSpawn = npc.Center + shotDir * (npc.width / 2f + 10f);
                            Projectile.NewProjectile(npc.GetSource_FromThis(), shotSpawn.X, shotSpawn.Y, globalNPC.LockedShotVector.X, globalNPC.LockedShotVector.Y, projectileType, projectileDamage, 0f, Main.myPlayer);
                        }

                        SoundEngine.PlaySound(shootSound.Value);

                        // Consume one standing-fire charge per shot
                        if (globalNPC.FighterRangedStandShotsRemaining > 0)
                        {
                            if (--globalNPC.FighterRangedStandShotsRemaining == 0)
                            {
                                // All charges spent — exit standing-fire and resume pursuit
                                globalNPC.ArcherAimDirection = 0f;
                                npc.TargetClosest(true);
                            }
                        }
                    }

                    // Only track the player visually while we haven't yet committed to a shot direction
                    if (!inTelegraphWindow)
                    {
                        Vector2 aimVector = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center, projectileVelocity);

                        if (Math.Abs(aimVector.Y) > Math.Abs(aimVector.X) * 2f) // target steeply above/below NPC
                        {
                            if (aimVector.Y > 0f)
                                globalNPC.ArcherAimDirection = 1f; // aim downward
                            else
                                globalNPC.ArcherAimDirection = 5f; // aim upward
                        }
                        else if (Math.Abs(aimVector.X) > Math.Abs(aimVector.Y) * 2f) // target on level with NPC
                            globalNPC.ArcherAimDirection = 3f;  //  aim straight ahead
                        else if (aimVector.Y > 0f) // target is below NPC
                            globalNPC.ArcherAimDirection = 2f;  //  aim slight downward
                        else // target is not below NPC
                            globalNPC.ArcherAimDirection = 4f;  //  aim slight upward
                    }
                }
                //If we're out of range of the player, don't aim at them
                else
                {
                    globalNPC.ArcherAimDirection = 0;
                    globalNPC.FighterRangedStandShotsRemaining = 0; // abort standing-fire if target leaves range
                }
            }

            npc.ai[2] = globalNPC.ArcherAimDirection;
        }



        //Todo:
        //Upgrade gap-jumping code to scale jump x and  y velocity with gap size, up to a limit
        //Upgrade wall-jumping code to scale jump height with how tall the wall in front of it is. Also let it recognize walls with gaps in them.
        //More complex "bored" check than simple velocity. Right now it can get bored if it takes too long doing things that require it to move slow.
        private static void BasicAI(NPC npc, float topSpeed, float acceleration, float brakingPower, bool isArcher, bool canTeleport = false, int doorBreakingDamage = 0, bool hatesLight = false, SoundStyle? randomSound = null, int soundFrequency = 1000, float enragePercentage = 0, float enrageTopSpeed = 0, bool lavaJumping = false, bool canDodgeroll = true, bool canPounce = true)
        {
            npc.noTileCollide = false;

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RunningCustomFighterAI = true; // mark for PostAI's confusion handling
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
            }
            topSpeed *= globalNPC.Swiftness;
            acceleration *= globalNPC.Swiftness;
            if (globalNPC.WaypointSearchCooldown > 0)
            {
                globalNPC.WaypointSearchCooldown--;
            }
            if (globalNPC.NavBlockedDirectionTimer > 0)
            {
                globalNPC.NavBlockedDirectionTimer--;
                if (globalNPC.NavBlockedDirectionTimer == 0)
                {
                    globalNPC.NavBlockedDirection = 0;
                }
            }
            if (globalNPC.NavExploreTimer > 0)
            {
                globalNPC.NavExploreTimer--;
                if (globalNPC.NavExploreTimer == 0)
                {
                    globalNPC.NavExploreDirection = 0;
                }
            }
            if (globalNPC.FighterNoLosPursuitBoostTimer > 0)
            {
                globalNPC.FighterNoLosPursuitBoostTimer--;
                topSpeed *= 1.25f;
                acceleration *= 1.35f;
            }

            if (!globalNPC.Initialized)
            {
                //Make damage and health scale with strength
                npc.damage = (int)(npc.damage * globalNPC.Strength);
                npc.life = (int)(npc.life * globalNPC.Strength);
                npc.lifeMax = (int)(npc.lifeMax * globalNPC.Strength);
                npc.scale *= (float)Math.Pow(globalNPC.Strength, 0.5f); //Make 'scale' only increase with the square root of strength, to make it change less dramatically

                //Make low-frequency attacks somewhat more likely
                foreach (ProjectileData data in globalNPC.AttackList)
                {
                    data.timerCap = (int)(data.timerCap * globalNPC.CastingSpeed);
                    if (data.weight < 1)
                    {
                        data.weight += (1 - data.weight) * globalNPC.Adeptness;
                    }
                }

                globalNPC.Initialized = true;
            }

            // WeakTeleport bored walk — if the NPC gave up pursuing the player, briefly
            // disengage, pause, then hand control back to normal pursuit.
            // Teleport charges are intentionally not restored here; WeakTeleport is a
            // strict two-use-per-NPC fallback, not a pursuit-cycle resource.
            if (globalNPC.WeakTeleport && globalNPC.WeakTeleportBoredPhase > 0)
            {
                if (npc.justHit)
                {
                    // Player found us during our break — resume normal pursuit
                    globalNPC.WeakTeleportBoredPhase = 0;
                    globalNPC.WeakTeleportReachTimer = 0;
                    globalNPC.WeakTeleportCooldown = 0;
                }
                else
                {
                    npc.TargetClosest(false);
                    globalNPC.WeakTeleportBoredTimer--;

                    switch (globalNPC.WeakTeleportBoredPhase)
                    {
                        case 1: // Stand still (2 seconds = 120 frames)
                            npc.velocity.X *= 0.85f;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 2;
                                globalNPC.WeakTeleportBoredTimer = 300; // walk away 5 s
                                npc.direction = Main.player[npc.target].Center.X < npc.Center.X ? 1 : -1;
                                npc.spriteDirection = npc.direction;
                            }
                            break;

                        case 2: // Walk away from the player (5 seconds = 300 frames)
                            if (npc.velocity.X < topSpeed && npc.direction == 1) npc.velocity.X += acceleration;
                            else if (npc.velocity.X > -topSpeed && npc.direction == -1) npc.velocity.X -= acceleration;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 3;
                                globalNPC.WeakTeleportBoredTimer = 120; // pause 2 s
                            }
                            break;

                        case 3: // Pause (2 seconds = 120 frames)
                            npc.velocity.X *= 0.85f;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                globalNPC.WeakTeleportBoredPhase = 4;
                                globalNPC.WeakTeleportBoredTimer = 120; // walk back 2 s
                                npc.direction *= -1; // turn around (toward player)
                                npc.spriteDirection = npc.direction;
                            }
                            break;

                        case 4: // Walk back toward the player briefly (2 seconds = 120 frames)
                            if (npc.velocity.X < topSpeed && npc.direction == 1) npc.velocity.X += acceleration;
                            else if (npc.velocity.X > -topSpeed && npc.direction == -1) npc.velocity.X -= acceleration;
                            if (globalNPC.WeakTeleportBoredTimer <= 0)
                            {
                                // Resume pursuit without restoring spent weak teleport charges.
                                globalNPC.WeakTeleportBoredPhase = 0;
                                globalNPC.WeakTeleportReachTimer = 0;
                                globalNPC.WeakTeleportCooldown = 0;
                            }
                            break;
                    }
                    return; // skip all normal movement, attacking, and boredom tracking
                }
            }

            bool earlyLineOfSight = Main.player[npc.target].CanHit(npc);
            bool earlyDifferentFloor = earlyLineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;
            bool shouldRequestWaypoint = globalNPC.NavigationTier >= 1
                && globalNPC.WaypointTimer == 0
                && globalNPC.WaypointSearchCooldown == 0
                && globalNPC.TeleportCountdown == 0
                && globalNPC.DodgeTimer == 0
                && globalNPC.PounceTimer == 0
                && (!earlyLineOfSight || earlyDifferentFloor || globalNPC.BoredTimer > 0 || globalNPC.StuckTimer >= 12);

            if (shouldRequestWaypoint)
            {
                globalNPC.LastNavIntent = !earlyLineOfSight ? "early:no-los"
                    : earlyDifferentFloor ? "early:different-floor"
                    : globalNPC.BoredTimer > 0 ? "early:bored"
                    : "early:stuck";
                bool forceWaypoint = globalNPC.BoredTimer >= globalNPC.BoredomThreshold || globalNPC.StuckTimer >= 20;
                TrySetFighterWaypoint(npc, globalNPC, forceWaypoint);
            }

            //If it has at least one attack, perform it
            if (globalNPC.AttackList.Count > 0)
            {
                bool crossableGapTowardPlayer = HasCrossableGapTowardPlayer(npc, globalNPC, out int gapTravelDirection);
                if (crossableGapTowardPlayer)
                {
                    npc.direction = gapTravelDirection;
                    npc.spriteDirection = gapTravelDirection;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    if (globalNPC.CurrentAttack.needsLineOfSight)
                    {
                        globalNPC.ProjectileTimer = 0f;
                    }
                }
                float committedAttackLeadTime = globalNPC.CurrentAttack.type == ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>()
                    ? 90f
                    : ProjectileTelegraphTime;
                bool inCommittedAttack = globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - committedAttackLeadTime;
                bool navigationNeedsControl = globalNPC.NavigationTier >= 1
                    && globalNPC.CurrentAttack.needsLineOfSight
                    && !inCommittedAttack
                    && (globalNPC.WaypointTimer > 0 || globalNPC.LedgeRunUpTimer > 0 || globalNPC.LedgeVaultTimer > 0 || globalNPC.StuckTimer >= 8);
                if (!crossableGapTowardPlayer && !navigationNeedsControl)
                {
                    SimpleProjectile(npc);
                }
                else if (navigationNeedsControl)
                {
                    globalNPC.ProjectileTimer = 0f;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    globalNPC.ArcherAimDirection = 0f;
                }
            }

            if (globalNPC.PounceTimer > 0)
            {
                globalNPC.PounceTimer--;

                if (globalNPC.PounceTimer % 5 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.position;
                        spawnPosition.Y += npc.height;
                        spawnPosition.X += Main.rand.NextFloat(npc.width);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, new Vector2(0, 2), ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer);
                    }
                }

                if (globalNPC.PounceTimer == 0)
                {
                    float pounceSpeed = topSpeed * 5;
                    bool hasTrajectory = false;
                    while (!hasTrajectory)
                    {
                        Vector2 trajectory = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center + new Vector2(0, -100), pounceSpeed, npc.gravity, false, false);
                        if (trajectory == Vector2.Zero)
                        {
                            pounceSpeed += topSpeed * 2;

                            //If it requires more than 20 units of speed to make it to the player, give up and just launch normally instead of using a ballistic trajectory
                            if (pounceSpeed > 20)
                            {
                                npc.velocity = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center + new Vector2(0, -100), 20);
                                npc.netUpdate = true;
                                break;
                            }
                        }
                        else
                        {
                            hasTrajectory = true;
                            npc.velocity = trajectory;
                            npc.netUpdate = true;
                        }
                    }
                }
            }
            else if (globalNPC.PounceCooldown > 0)
            {
                globalNPC.PounceCooldown--;
            }

            if (globalNPC.DodgeTimer > 0)
            {
                npc.rotation += MathHelper.TwoPi / 30f * npc.direction;
                npc.velocity.X = 5 * npc.direction;

                globalNPC.DodgeTimer--;
                if (globalNPC.DodgeTimer == 0)
                {
                    npc.velocity.X = 0;
                }
            }
            else
            {
                npc.rotation = 0;

                if (globalNPC.DodgeCooldown > 0)
                {
                    globalNPC.DodgeCooldown--;
                }
            }

            //Stop moving when teleporting, and handle the logic to execute it
            if (globalNPC.TeleportCountdown > 0)
            {
                globalNPC.BoredTimer = 0;
                npc.velocity.X = 0;
                globalNPC.TeleportCountdown--;
                if (globalNPC.TeleportCountdown == 0)
                {
                    ExecuteQueuedTeleport(npc);
                }
            }

            //Block firing and reset cooldowns if it's busy doing other things
            if (globalNPC.TeleportCountdown > 0 || globalNPC.BoredTimer < 0 || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                globalNPC.ProjectileTimer = 0;
                globalNPC.ArcherAimDirection = 0;
            }

            //Apply scaling to SHM enemies
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (!npc.boss)
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
                    {
                        topSpeed *= tsorcRevampWorld.SHMScale;
                        acceleration *= tsorcRevampWorld.SubtleSHMScale;
                        enrageTopSpeed *= tsorcRevampWorld.SHMScale;
                    }
                }
            }


            //If it has a sound to play, roll a chance for playing it
            if (randomSound != null && Main.rand.Next(soundFrequency) <= 0)
            {
                SoundEngine.PlaySound(randomSound.Value, npc.Center);
            }

            //If we can enrage, do that
            if (npc.life < (float)npc.lifeMax * enragePercentage)
            {
                acceleration *= 2;
                topSpeed = enrageTopSpeed;
            }

            //If it can jump in lava and is in lava, do that
            if (lavaJumping && npc.lavaWet)
            {
                npc.velocity.Y -= 2;
            }

            //If just hit, then it's not bored
            if (npc.justHit)
            {
                globalNPC.BoredTimer = 0;
                if (globalNPC.WeakTeleport)
                {
                    // Being hit also resets the reach timer so the NPC doesn't give up immediately
                    // when the player finds it before it found LOS.
                    globalNPC.WeakTeleportReachTimer = 0;
                }
            }

            //If fleeing, despawn as soon as it's offscreen (via timeLeft running out)
            if (globalNPC.Fleeing || (hatesLight && Main.dayTime && (npc.position.Y / 16f) < Main.worldSurface))
            {
                globalNPC.BoredTimer = -999;
                npc.timeLeft = 10;
            }

            //If bored, target the closest player it has line of sight to. If it doesn't have los to any, just target the closest one.
            if (globalNPC.BoredTimer != 0)
            {
                float distance = 9999999;
                int target = -1;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        if (Main.player[i].CanHit(npc))
                        {
                            float playerDistance = Main.player[i].Distance(npc.Center);
                            if (playerDistance < distance)
                            {
                                distance = playerDistance;
                                target = i;
                            }
                        }
                    }
                    if (target != -1)
                    {
                        npc.target = target;
                    }
                    else
                    {
                        npc.TargetClosest(false);
                    }
                }
            }

            // Compute line of sight early so waypoint cancel logic can use it before movement
            bool lineOfSight = Main.player[npc.target].CanHit(npc);
            bool playerOnDirectEngageFloor = lineOfSight && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < 32f;
            if (playerOnDirectEngageFloor)
            {
                ClearFighterWaypoint(globalNPC);
                globalNPC.NavExploreTimer = 0;
                globalNPC.NavExploreDirection = 0;
                globalNPC.NavBlockedDirection = 0;
                globalNPC.NavBlockedDirectionTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
            }

            // WeakTeleport: limited-use gap-closing teleport for non-teleporter enemies.
            // Up to 2 total charges for this NPC, 10-second cooldown between each, minimum 40-tile range.
            if (globalNPC.WeakTeleport)
            {
                if (globalNPC.WeakTeleportCooldown > 0)
                    globalNPC.WeakTeleportCooldown--;

                if (!lineOfSight &&
                    globalNPC.WeakTeleportUses > 0 &&
                    globalNPC.WeakTeleportCooldown == 0 &&
                    globalNPC.TeleportCountdown == 0 &&
                    globalNPC.WeakTeleportBoredPhase == 0 &&
                    npc.Distance(Main.player[npc.target].Center) > 640f && // 40 tiles minimum
                    Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dest = FindWeakTeleportDestination(npc, Main.player[npc.target]);
                    if (dest != Vector2.Zero)
                    {
                        int telegraphTime = globalNPC.TeleportTelegraphTime;
                        globalNPC.TeleportCountdown = telegraphTime;
                        globalNPC.TeleportTelegraph = dest;
                        npc.velocity = Vector2.Zero;
                        globalNPC.WeakTeleportUses--;
                        globalNPC.WeakTeleportCooldown = 600; // 10 seconds
                        globalNPC.BoredTimer = 0;
                        globalNPC.WaypointTimer = 0;
                        globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                        globalNPC.WaypointNoProgressTimer = 0;
                        globalNPC.LastWaypointDistance = 0f;
                        globalNPC.NavRouteIndex = 0;
                        globalNPC.NavRouteCount = 0;
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                        globalNPC.WeakTeleportReachTimer = 0;
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            npc.whoAmI, telegraphTime);
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), dest, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer,
                            ai1: telegraphTime);
                    }
                }
            }

            // Waypoint navigation: tick down and cancel if LOS restored or destination reached
            if (globalNPC.WaypointTimer > 0)
            {
                globalNPC.WaypointTimer--;
                if (globalNPC.WaypointTimer <= 0)
                {
                    globalNPC.WaypointTarget = Vector2.Zero;
                    globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                    globalNPC.WaypointNoProgressTimer = 0;
                    globalNPC.LastWaypointDistance = 0f;
                }
                // Drop waypoints (BFS found a platform-drop path) have same X as the NPC;
                // don't cancel them on X proximity — let Y proximity signal completion instead.
                bool isDropWaypoint = globalNPC.WaypointTarget.Y > npc.Center.Y + 48f
                    && Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) < 32f;
                bool reachedX = Math.Abs(npc.Center.X - globalNPC.WaypointTarget.X) < 8f;
                bool reachedJumpTarget = Math.Abs(npc.Center.X - globalNPC.WaypointTarget.X) < 24f
                    && Math.Abs(npc.Center.Y - globalNPC.WaypointTarget.Y) < 40f;
                bool droppedToTarget = isDropWaypoint
                    && Math.Abs(npc.Center.Y - globalNPC.WaypointTarget.Y) < 32f;

                // Only cancel on LOS when the player is at roughly the same elevation.
                // If they're on a different floor (e.g. above a platform ceiling), the NPC has
                // LOS through the platform but can't actually engage — keep the waypoint so it
                // continues navigating to a staircase/ledge rather than spinning back to centre.
                // 32px (~2 tiles) — only cancel the waypoint when the player is truly on the
                // same floor and directly engageable.  64 was too wide: the different-floor
                // BFS would set a waypoint only for cancelOnLos to destroy it one tick later.
                bool playerOnSameFloor = Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < 32f;
                bool cancelOnLos = lineOfSight && playerOnSameFloor;

                bool reachedWaypoint = globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.JumpTo
                    ? reachedJumpTarget
                    : reachedX && !isDropWaypoint;

                float waypointDistance = npc.Distance(globalNPC.WaypointTarget);
                if (globalNPC.LastWaypointDistance <= 0f || waypointDistance < globalNPC.LastWaypointDistance - 4f)
                {
                    globalNPC.LastWaypointDistance = waypointDistance;
                    globalNPC.WaypointNoProgressTimer = 0;
                }
                else
                {
                    globalNPC.WaypointNoProgressTimer++;
                }

                bool waypointStalled = globalNPC.WaypointNoProgressTimer > 90;

                if (cancelOnLos || reachedWaypoint || droppedToTarget || waypointStalled)
                {
                    bool routeStepComplete = !cancelOnLos && !waypointStalled && (reachedWaypoint || droppedToTarget)
                        && globalNPC.NavRouteCount > 0
                        && globalNPC.NavRouteIndex + 1 < globalNPC.NavRouteCount;
                    if (routeStepComplete)
                    {
                        globalNPC.NavRouteIndex++;
                        globalNPC.NavRouteTimer = 0;
                        globalNPC.NavRouteNoProgressTimer = 0;
                        globalNPC.WaypointTarget = globalNPC.NavRouteTargets[globalNPC.NavRouteIndex];
                        globalNPC.WaypointAction = globalNPC.NavRouteActions[globalNPC.NavRouteIndex];
                        globalNPC.WaypointTimer = 420;
                        globalNPC.WaypointNoProgressTimer = 0;
                        globalNPC.LastWaypointDistance = npc.Distance(globalNPC.WaypointTarget);
                        globalNPC.LastNavRouteDistance = globalNPC.LastWaypointDistance;
                        globalNPC.LastNavIntent = "route:advance";
                        globalNPC.LastWaypointResult = $"route:{globalNPC.NavRouteIndex + 1}/{globalNPC.NavRouteCount} {globalNPC.WaypointAction}";
                        npc.netUpdate = true;
                        goto afterWaypointState;
                    }

                    Vector2 stalledWaypoint = globalNPC.WaypointTarget;
                    globalNPC.WaypointTimer = 0;
                    globalNPC.WaypointTarget = Vector2.Zero;
                    globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
                    globalNPC.WaypointNoProgressTimer = 0;
                    globalNPC.LastWaypointDistance = 0f;
                    globalNPC.NavRouteIndex = 0;
                    globalNPC.NavRouteCount = 0;
                    globalNPC.NavRouteTimer = 0;
                    globalNPC.NavRouteNoProgressTimer = 0;
                    globalNPC.LastNavRouteDistance = 0f;
                    if (waypointStalled)
                    {
                        globalNPC.WaypointSearchCooldown = Math.Max(globalNPC.WaypointSearchCooldown, 30);
                        globalNPC.LastWaypointResult = "fail:waypoint-stalled";
                        int stalledDirection = Math.Abs(stalledWaypoint.X - npc.Center.X) < 8f
                            ? (npc.direction == 0 ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X) : npc.direction)
                            : Math.Sign(stalledWaypoint.X - npc.Center.X);
                        MarkNavDirectionBlocked(globalNPC, stalledDirection, 180);
                        StartNavExplore(npc, globalNPC, -stalledDirection, 180);
                    }

                    // Immediately chain to the next BFS step when a waypoint is completed
                    // but the player is still not reachable at the same floor level.
                    // Without this there is a ~2 s gap (bfsFallback interval) during which the
                    // NPC reverts to "face player center X" and walks the wrong way.
                    // Chain to the next BFS step whenever a waypoint is completed/reached but
                    // the player is still not on the same floor.  Don't require BoredTimer > 20
                    // here — the hard BoredTimer reset (LOS + close Y) keeps it at 0 for
                    // different-floor cases, so the old guard would silently skip chaining.
                    if (!cancelOnLos && !waypointStalled && globalNPC.NavigationTier >= 1)
                    {
                        globalNPC.LastNavIntent = "waypoint:chain";
                        TrySetFighterWaypoint(npc, globalNPC, true);
                    }
                }
                afterWaypointState: ;
            }

            // Face the active waypoint first. Player-facing has a dead zone to avoid jitter
            // when directly underneath the player, but waypoint steering must not inherit it.
            if (globalNPC.WaypointTimer > 0)
            {
                float waypointDeltaX = globalNPC.WaypointTarget.X - npc.Center.X;
                if (Math.Abs(waypointDeltaX) > 4f)
                {
                    npc.direction = waypointDeltaX < 0f ? -1 : 1;
                    npc.spriteDirection = npc.direction;
                }
            }
            else if (!playerOnDirectEngageFloor && globalNPC.NavExploreTimer > 0 && globalNPC.NavExploreDirection != 0)
            {
                npc.direction = globalNPC.NavExploreDirection;
                npc.spriteDirection = npc.direction;
            }
            else if (Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 30)
            {
                int desiredDirection;
                if (Main.player[npc.target].Center.X <= npc.Center.X)
                {
                    desiredDirection = -1;
                }
                else
                {
                    desiredDirection = 1;
                }
                if (!playerOnDirectEngageFloor && globalNPC.NavBlockedDirectionTimer > 0 && desiredDirection == globalNPC.NavBlockedDirection)
                {
                    desiredDirection *= -1;
                    globalNPC.LastNavIntent = "avoid:blocked-direction";
                }
                npc.direction = desiredDirection;
                if (globalNPC.BoredTimer < 0)
                {
                    npc.direction *= -1;
                }
                npc.spriteDirection = npc.direction;
            }

            //If moving more than max speed, then slow down
            if (globalNPC.PounceCooldown <= 240)
            {
                if (npc.velocity.X > topSpeed)
                {
                    npc.velocity.X -= brakingPower;
                    if (npc.velocity.X < 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
                if (npc.velocity.X < -topSpeed)
                {
                    npc.velocity.X += brakingPower;
                    if (npc.velocity.X > 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
            }

            // Post-attack standoff: tier 2 enemies may briefly hold position after a few attacks,
            // but ordinary LOS should not stop pursuit.
            bool inStandoff = false;
            if (globalNPC.FighterPostAttackPauseTimer > 0)
            {
                globalNPC.FighterPostAttackPauseTimer--;
            }
            if (globalNPC.CanStopToFire && !globalNPC.CanPassThroughWalls && globalNPC.NavigationTier >= 2 && (globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0) && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
            {
                inStandoff = true;
                if (globalNPC.FighterRangedStandShotsRemaining > 0)
                    npc.velocity.X = 0f; // hard stop: hold position while firing a burst
                else
                    npc.velocity.X *= 0.8f; // gradual stop: post-attack breather
            }

            //Accelerate in the direction they are facing (unless the npc is an aiming archer)
            if ((!isArcher || globalNPC.ArcherAimDirection == 0) && !inStandoff)
            {
                if (npc.velocity.X < topSpeed && npc.direction == 1)
                {
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X > topSpeed)
                    {
                        npc.velocity.X = topSpeed;
                    }
                }
                else
                {
                    if (npc.velocity.X > -topSpeed && npc.direction == -1)
                    {
                        npc.velocity.X -= acceleration;
                        if (npc.velocity.X < -topSpeed)
                        {
                            npc.velocity.X = -topSpeed;
                        }
                    }
                }
            }


            // Ledge-halt: optionally stop before a significant drop when we already have LOS.
            // Only halts if dropping would put the NPC meaningfully lower than the player —
            // tiny drops and same-elevation crossings are left alone so jump/gap logic can handle them.
            // A hard cap of 180 frames prevents indefinite ledge-camping.
            if (!globalNPC.CanPassThroughWalls && globalNPC.HaltAtLedge && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
            {
                int aheadX = npc.direction == -1
                    ? (int)(npc.position.X / 16f) - 1
                    : (int)((npc.position.X + npc.width) / 16f);
                int belowY = (int)(npc.position.Y + npc.height + 8f) / 16;

                if (!UsefulFunctions.IsTileReallySolid(aheadX, belowY))
                {
                    // Scan downward for solid ground (ignores water/air). Cap at 10 tiles.
                    int dropDepth = 10;
                    for (int dy = 1; dy <= 10; dy++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(aheadX, belowY + dy))
                        {
                            dropDepth = dy;
                            break;
                        }
                    }

                    // Where would we land, in world-space Y pixels?
                    float landingWorldY = (belowY + dropDepth) * 16f;
                    float playerWorldY = Main.player[npc.target].Center.Y;

                    // Halt only if landing would put us more than 3 tiles below the player
                    // (losing elevation), AND the drop is at least 4 tiles deep (not a tiny step).
                    bool wouldLoseElevation = landingWorldY > playerWorldY + 48f;
                    bool dropIsSignificant = dropDepth >= 4;
                    bool shouldHalt = wouldLoseElevation && dropIsSignificant && globalNPC.LedgeHaltTimer < 180;

                    if (shouldHalt)
                    {
                        npc.velocity.X = 0f;
                        globalNPC.LedgeHaltTimer++;
                    }
                    else
                    {
                        globalNPC.LedgeHaltTimer = 0;
                    }
                }
                else
                {
                    globalNPC.LedgeHaltTimer = 0;
                }
            }
            else
            {
                globalNPC.LedgeHaltTimer = 0;
            }

            //Jumping and platform falling code, copied and edited from Firebomb Hollow
            int x_in_front;
            if (npc.direction == -1)
            {
                x_in_front = (int)(npc.position.X / 16f) - 1;
            }
            else
            {
                x_in_front = (int)((npc.position.X + npc.width) / 16f);
            }

            int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
            //Dust.DrawDebugBox(new Rectangle(x_in_front * 16, y_above_feet * 16, 16, 16));
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            bool standing_on_solid_tile = false;
            bool navActionHandledJump = false;
            if (globalNPC.NavJumpCooldown > 0)
            {
                globalNPC.NavJumpCooldown--;
            }

            //Check if standing on a solid tile
            int x_left_edge = (int)npc.position.X / 16;
            int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (UsefulFunctions.IsTileReallySolid(l, y_below_feet)) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                    }
                }
            }

            // NavigationTier 0 compatibility path: preserve the original FighterAI terrain
            // behavior exactly, with no waypoint/BFS/ledge-run-up leakage.
            if (globalNPC.NavigationTier < 1 && standing_on_solid_tile)
            {
                if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                {
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2))
                    {
                        if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 3))
                        {
                            npc.velocity.Y = -8f;
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = -7f;
                            npc.netUpdate = true;
                        }
                    }
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1))
                    {
                        npc.velocity.Y = -6f;
                        npc.netUpdate = true;
                    }
                    else if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet))
                    {
                        npc.velocity.Y = -5f;
                        npc.netUpdate = true;
                    }
                    else if (npc.directionY < 0 && !UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet) && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, y_below_feet))
                    {
                        npc.velocity.Y = -8f;
                        npc.velocity.X += 4f * npc.direction;
                        npc.netUpdate = true;
                    }

                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        globalNPC.BoredTimer = 0;
                        if (Main.GameUpdateCount % 60 == 0)
                        {
                            npc.velocity.X = 0.5f * -npc.direction;
                            globalNPC.DoorBreakProgress += doorBreakingDamage;
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);
                            if (globalNPC.DoorBreakProgress >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                globalNPC.DoorBreakProgress = 0;
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    globalNPC.BoredTimer = 999;
                                    npc.velocity.X = 0;
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0);
                                }
                            }
                        }
                    }
                }
            }

            if (standing_on_solid_tile && globalNPC.NavigationTier >= 1 && globalNPC.NavJumpCooldown == 0
                && HasCrossableGapTowardPlayer(npc, globalNPC, out int immediateGapDirection))
            {
                float jumpPower = MathHelper.Clamp(globalNPC.MaxJumpPower * 0.9f, 6.5f, globalNPC.MaxJumpPower);
                float gapBoost = MathHelper.Clamp(Math.Max(topSpeed * 1.8f, globalNPC.MaxJumpBoost * 0.6f), 2.25f, Math.Max(3f, globalNPC.MaxJumpBoost));
                npc.direction = immediateGapDirection;
                npc.spriteDirection = immediateGapDirection;
                npc.velocity.X = immediateGapDirection * gapBoost;
                npc.velocity.Y = -jumpPower;
                globalNPC.StuckTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
                globalNPC.NavJumpCooldown = 18;
                globalNPC.FighterRangedStandShotsRemaining = 0;
                globalNPC.ProjectileTimer = 0f;
                npc.netUpdate = true;
                navActionHandledJump = true;
            }

            bool playerDirectlyEngageableForJumping = lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;

            if (!playerDirectlyEngageableForJumping && globalNPC.NavRouteCount == 0 && standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1
                && globalNPC.NavJumpCooldown == 0 && globalNPC.StuckTimer >= 8
                && Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 32f)
            {
                int blockedDirection = npc.direction == 0
                    ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X)
                    : npc.direction;
                if (blockedDirection == 0)
                {
                    blockedDirection = 1;
                }

                MarkNavDirectionBlocked(globalNPC, blockedDirection, 210);
                StartNavExplore(npc, globalNPC, -blockedDirection, 150);
                npc.velocity.X = globalNPC.NavExploreDirection * Math.Max(topSpeed * 0.9f, 1.1f);
                globalNPC.StuckTimer = 0;
                globalNPC.LedgeRunUpTimer = 0;
                globalNPC.LedgeRunUpDirection = 0;
                globalNPC.LedgeVaultTimer = 0;
                globalNPC.LedgeVaultDirection = 0;
                globalNPC.NavJumpCooldown = 10;
                ClearFighterWaypoint(globalNPC);
                globalNPC.LastNavIntent = "local:block-memory";
                globalNPC.LastWaypointResult = "local:explore-away";
                npc.netUpdate = true;
                navActionHandledJump = true;
            }

            if (!standing_on_solid_tile && globalNPC.NavigationTier >= 1 && globalNPC.LedgeVaultTimer > 0)
            {
                int vaultDirection = globalNPC.LedgeVaultDirection == 0 ? npc.direction : globalNPC.LedgeVaultDirection;
                globalNPC.LedgeVaultTimer--;
                npc.direction = vaultDirection;
                npc.spriteDirection = vaultDirection;

                int vaultElapsed = 30 - globalNPC.LedgeVaultTimer;
                if (vaultElapsed < 7 && npc.velocity.Y < -1f)
                {
                    npc.velocity.X *= 0.65f;
                }
                else
                {
                    float vaultBoost = MathHelper.Clamp(Math.Max(topSpeed * 1.25f, globalNPC.MaxJumpBoost * 0.3f), 0.85f, 1.65f);
                    npc.velocity.X = vaultDirection * vaultBoost;
                }

                if (globalNPC.LedgeVaultTimer == 0 || npc.velocity.Y >= 0f)
                {
                    globalNPC.LedgeVaultTimer = 0;
                    globalNPC.LedgeVaultDirection = 0;
                }
            }

            if (standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1 && globalNPC.WaypointTimer > 0)
            {
                float waypointDeltaX = globalNPC.WaypointTarget.X - npc.Center.X;
                int waypointDir = Math.Abs(waypointDeltaX) < 4f
                    ? npc.direction
                    : Math.Sign(waypointDeltaX);

                if (globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.JumpTo && globalNPC.NavJumpCooldown == 0)
                {
                    float waypointDeltaY = globalNPC.WaypointTarget.Y - npc.Center.Y;
                    bool tinyHeightJump = waypointDeltaY > -24f && Math.Abs(waypointDeltaX) < 96f;
                    if (waypointDeltaY > 8f || tinyHeightJump)
                    {
                        // Defensive guard: lower/small-height waypoints are walk/drop steering,
                        // not jump commands. Local gap logic handles true gap jumps separately.
                        npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                        npc.spriteDirection = npc.direction;
                        if (Math.Abs(npc.velocity.X) < topSpeed * 0.75f)
                        {
                            npc.velocity.X = npc.direction * topSpeed * 0.75f;
                        }
                        globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.Walk;
                    }
                    else
                    {
                        float upwardTiles = Math.Max(0f, -waypointDeltaY / 16f);
                        float jumpPower = MathHelper.Clamp(4.8f + upwardTiles * 1.35f, 5.5f, Math.Max(globalNPC.MaxJumpPower, 8f));
                        bool mostlyVerticalJump = Math.Abs(waypointDeltaX) < 18f && waypointDeltaY < -24f;
                        float horizontalBoost = mostlyVerticalJump
                            ? 0f
                            : MathHelper.Clamp(Math.Abs(waypointDeltaX) / 24f, 0.75f, Math.Max(globalNPC.MaxJumpBoost, 2f));
                        if (mostlyVerticalJump)
                        {
                            waypointDir = Main.player[npc.target].Center.X < npc.Center.X ? -1 : 1;
                        }
                        npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                        npc.spriteDirection = npc.direction;
                        npc.velocity.X = npc.direction * horizontalBoost;
                        npc.velocity.Y = -jumpPower;
                        globalNPC.StuckTimer = 0;
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                        if (mostlyVerticalJump)
                        {
                            globalNPC.LedgeVaultTimer = 26;
                            globalNPC.LedgeVaultDirection = npc.direction;
                        }
                        globalNPC.NavJumpCooldown = 24;
                        npc.netUpdate = true;
                        navActionHandledJump = true;
                    }
                }

                if (globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.Drop || globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.DropThroughPlatform)
                {
                    npc.direction = waypointDir == 0 ? npc.direction : waypointDir;
                    npc.spriteDirection = npc.direction;
                    if (Math.Abs(npc.velocity.X) < topSpeed * 0.75f)
                    {
                        npc.velocity.X = npc.direction * topSpeed * 0.75f;
                    }
                }
            }

            //If standing on solid tile
            if (standing_on_solid_tile && !navActionHandledJump && globalNPC.NavigationTier >= 1)
            {
                //Moving forward, or blocked and ready to let tiered navigation plan an escape.
                if (npc.velocity.X * npc.direction > 0f || (globalNPC.NavigationTier >= 1 && globalNPC.StuckTimer >= 3))
                {
                    // Jump power scaled by per-enemy MaxJumpPower (NavigationTier >= 1) or vanilla 8f
                    float jumpPower = globalNPC.NavigationTier >= 1 ? globalNPC.MaxJumpPower : 8f;

                    // ── Ledge run-up ──────────────────────────────────────────────
                    // When StuckTimer first reaches a low threshold (NPC has been stopped by the
                    // same obstacle for ~8 frames), initiate a back-up: reverse velocity
                    // until geometry shows usable headroom, then fire a ledge-clear jump.
                    // This solves the "stuck in a pit
                    // with a 1-tile ledge" case where the NPC is pressed too close to
                    // the wall to clear the ledge corner with a vertical-only jump.
                    int leftFrontX = (int)(npc.position.X / 16f) - 1;
                    int rightFrontX = (int)((npc.position.X + npc.width) / 16f);
                    bool obstacleLeft =
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet    ) ||
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet - 1) ||
                        UsefulFunctions.IsTileReallySolid(leftFrontX, y_above_feet - 2);
                    bool obstacleRight =
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet    ) ||
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet - 1) ||
                        UsefulFunctions.IsTileReallySolid(rightFrontX, y_above_feet - 2);
                    bool anyObstacleAhead = npc.direction == -1 ? obstacleLeft : obstacleRight;

                    // Tiered navigation should commit to a ledge escape quickly instead of
                    // spending several frames doing local wall jumps under the overhang.
                    if (!playerDirectlyEngageableForJumping && globalNPC.NavRouteCount == 0 && globalNPC.NavigationTier >= 1 && globalNPC.StuckTimer >= 6
                        && (anyObstacleAhead || obstacleLeft || obstacleRight) && globalNPC.LedgeRunUpTimer == 0)
                    {
                        int playerDirection = Main.player[npc.target].Center.X < npc.Center.X ? -1 : 1;
                        if (!anyObstacleAhead)
                        {
                            if (playerDirection == -1 && obstacleLeft)
                            {
                                npc.direction = -1;
                            }
                            else if (playerDirection == 1 && obstacleRight)
                            {
                                npc.direction = 1;
                            }
                            else if (obstacleLeft)
                            {
                                npc.direction = -1;
                            }
                            else if (obstacleRight)
                            {
                                npc.direction = 1;
                            }
                            npc.spriteDirection = npc.direction;
                        }

                        globalNPC.LedgeRunUpTimer = 18;
                        globalNPC.LedgeRunUpDirection = npc.direction == 0 ? 1 : npc.direction;
                        MarkNavDirectionBlocked(globalNPC, globalNPC.LedgeRunUpDirection, 150);
                    }

                    if (globalNPC.LedgeRunUpTimer > 0)
                    {
                        int ledgeDirection = globalNPC.LedgeRunUpDirection == 0 ? npc.direction : globalNPC.LedgeRunUpDirection;
                        if (ledgeDirection == 0)
                        {
                            ledgeDirection = 1;
                        }

                        if (globalNPC.LedgeRunUpTimer > 1)
                        {
                            int backoffDirection = -ledgeDirection;
                            npc.direction = backoffDirection;
                            npc.spriteDirection = backoffDirection;
                            npc.velocity.X = backoffDirection * Math.Max(topSpeed * 0.85f, 1.1f);
                            globalNPC.LedgeRunUpTimer--;
                            globalNPC.LastNavIntent = "ledge:backoff";
                            globalNPC.LastWaypointResult = "ledge:building-clearance";
                            goto skipNormalJumps;
                        }

                        npc.direction = ledgeDirection;
                        npc.spriteDirection = ledgeDirection;
                        if (globalNPC.NavJumpCooldown == 0)
                        {
                            // Player-like ledge vault: go mostly straight up first, then drift
                            // toward the ledge after the head has time to clear the overhang.
                            npc.velocity.X = 0f;
                            npc.velocity.Y = -(jumpPower * 1.08f);
                            globalNPC.StuckTimer = 0;
                            globalNPC.LedgeRunUpTimer = 0;
                            globalNPC.LedgeRunUpDirection = 0;
                            globalNPC.LedgeVaultTimer = 30;
                            globalNPC.LedgeVaultDirection = ledgeDirection;
                            globalNPC.NavJumpCooldown = 18;
                            globalNPC.LastNavIntent = "ledge:vault";
                            globalNPC.LastWaypointResult = "ledge:jump-after-backoff";
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.X = 0f;
                            globalNPC.LedgeRunUpTimer = 1;
                        }
                        // Skip normal jump logic while the run-up is active
                        goto skipNormalJumps;
                    }

                    // Smart navigation enemies should avoid repeated desperation wall-jumps.
                    // Let BFS / ledge-run-up choose the escape instead of bouncing in place.
                    bool mayJump = globalNPC.NavigationTier < 1;
                    bool mayStepUpOneTile = globalNPC.NavigationTier >= 1
                        && globalNPC.WaypointTimer == 0
                        && globalNPC.StuckTimer < 6
                        && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet)
                        && !UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1)
                        && !UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2);

                    //3 blocks above ground level (head height) blocked
                    if (mayJump && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2))
                    {
                        //4 blocks above ground level (over head) blocked
                        if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 3))
                        {
                            npc.velocity.Y = -jumpPower; //Jump with full power (for 4+ block steps)
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = -jumpPower * 0.875f; //Jump with 87.5% power (for 3 block steps)
                            npc.netUpdate = true;
                        }
                    }
                    //For everything else, head height clear:
                    else if (mayJump && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1))
                    {
                        //2 blocks above ground level (mid body height) blocked
                        npc.velocity.Y = -jumpPower * 0.75f; //Jump with 75% power (for 2 block steps)
                        npc.netUpdate = true;
                    }
                    else if ((mayJump || mayStepUpOneTile) && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet))
                    {
                        // 1-tile step: smart-nav enemies should not visibly jump here.
                        // Use only a tiny lift plus forward pressure so this reads as a step.
                        if (npc.velocity.Y > -2.2f)
                            npc.velocity.Y = -2.2f;
                        float minHSpeed = Math.Max(topSpeed * 0.55f, 1.5f);
                        if (npc.direction ==  1 && npc.velocity.X < minHSpeed)  npc.velocity.X = minHSpeed;
                        if (npc.direction == -1 && npc.velocity.X > -minHSpeed) npc.velocity.X = -minHSpeed;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        // No wall obstacle ahead — check whether the floor continues.
                        // If the tile directly ahead at foot level is missing, there's a gap or drop.
                        if (!UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet))
                        {
                            // Scan up to 8 tiles horizontally and 20 tiles deep for a landing.
                            // gapWidth = horizontal distance to the far edge (0 = pure step-down).
                            // dropDepth = how many tiles lower the landing is (0 = same elevation).
                            int gapWidth  = -1; // -1 until a landing is found
                            int dropDepth =  0;
                            const int maxLandingScanDepth = 20;
                            bool waypointWantsForwardTravel = globalNPC.WaypointTimer > 0
                                && (Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) <= 16f
                                    || Math.Sign(globalNPC.WaypointTarget.X - npc.Center.X) == npc.direction);

                            for (int scan = 0; scan <= 8; scan++)
                            {
                                if (gapWidth >= 0) break;
                                int cx = x_in_front + scan * npc.direction;
                                for (int dy = 0; dy <= maxLandingScanDepth; dy++)
                                {
                                    if (UsefulFunctions.IsTileReallySolid(cx, y_below_feet + dy))
                                    {
                                        gapWidth  = scan;
                                        dropDepth = dy;
                                        break;
                                    }
                                }
                            }

                            // Only drop toward a pit when the player is clearly lower (~4 tiles).
                            // Same-level and above cases: halt so jump/BFS logic handles traversal instead.
                            bool playerClearlyBelow = Main.player[npc.target].Center.Y > npc.Center.Y + 64f;

                            if (gapWidth < 0)
                            {
                                // No landing found within scan range — very deep or wide pit.
                                // Only walk off the edge when the player is clearly below.
                                if (!playerClearlyBelow && !waypointWantsForwardTravel)
                                {
                                    npc.velocity.X = 0f;
                                    if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                        globalNPC.BoredTimer = 60;
                                }
                            }
                            else if (gapWidth == 0)
                            {
                                // Pure step-down (floor at same X but lower Y).
                                // Small drops (≤ 3 tiles): let gravity handle it naturally.
                                // Large drops: halt unless player is clearly below.
                                if (dropDepth > 3 && !playerClearlyBelow && !waypointWantsForwardTravel)
                                {
                                    npc.velocity.X = 0f;
                                    if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                        globalNPC.BoredTimer = 60;
                                }
                            }
                            else
                            {
                                // Genuine horizontal gap: jump across if reachable, else halt.
                                // Base cap of 8 tiles so all enemies cross typical platform gaps;
                                // NavigationTier >= 1 enemies scale further with their MaxJumpBoost.
                                float maxJumpable = globalNPC.NavigationTier >= 1
                                    ? Math.Max(8f, globalNPC.MaxJumpBoost + 3f)
                                    : 8f;

                                if (gapWidth <= maxJumpable)
                                {
                                    // Boost just enough to clear the gap, proportional to width.
                                    // Capped at the NPC's jump boost (or 4f floor for tier-0 enemies).
                                    float boostCap = globalNPC.NavigationTier >= 1
                                        ? Math.Max(globalNPC.MaxJumpBoost, 4f)
                                        : 4f;
                                    float horizontalBoost = MathHelper.Clamp(gapWidth * 0.7f, 1.5f, boostCap);
                                    npc.velocity.Y  = -jumpPower;
                                    npc.velocity.X += horizontalBoost * npc.direction;
                                    npc.netUpdate = true;
                                }
                                else
                                {
                                    // Gap too wide to jump — halt so the NPC doesn't walk off.
                                    if (!waypointWantsForwardTravel)
                                    {
                                        npc.velocity.X = 0f;
                                        if (globalNPC.NavigationTier >= 1 && globalNPC.BoredTimer == 0)
                                            globalNPC.BoredTimer = 60;
                                    }
                                }
                            }
                        }
                    }

                    //Door breaking
                    //First, it checks if the tile in front of it is solid, a door, and the npc can break it
                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        globalNPC.BoredTimer = 0; // not bored if working on breaking a door
                        if (Main.GameUpdateCount % 60 == 0)  //  knock once per second
                        {
                            npc.velocity.X = 0.5f * -npc.direction; //  slight recoil from hitting it
                            globalNPC.DoorBreakProgress += doorBreakingDamage;  //  increase door damage counter
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);  //  kill door ? when door not breaking too? can fail=true; effect only would make more sense, to make knocking sound
                            if (globalNPC.DoorBreakProgress >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                globalNPC.DoorBreakProgress = 0; //Reset counter

                                //Try to open door
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    //If the door is stuck set the npc to bored
                                    globalNPC.BoredTimer = 999;
                                    npc.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    //If it didn't fail sync the door opening
                                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0); // ??
                                }
                            }
                        }
                    }
                    skipNormalJumps: ; // target for the ledge-run-up early-exit goto
                }
            }


            //Can fall through platforms
            bool standing_on_platforms = true;
            bool atLeastOnePlatform = false;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (TileID.Sets.Platforms[Main.tile[l, y_below_feet].TileType])
                    {
                        atLeastOnePlatform = true;
                    }
                    else
                    {
                        if (Main.tile[l, y_below_feet].HasTile)
                        {
                            standing_on_platforms = false;
                        }
                    }
                }
            }

            // Drop through platforms when player is below.
            // Threshold is 64px (4 tiles) to avoid accidental drops on tiny height differences.
            // Gate on low horizontal speed: noTileCollide disables ALL tile collision, so a fast-moving
            // NPC would clip through walls. Only drop when nearly stopped horizontally.
            bool playerIsBelow = Main.player[npc.target].Center.Y > npc.Center.Y + 32f;
            // BFS may route the NPC through a platform drop: waypoint Y is below current position.
            bool bfsWantsDrop = globalNPC.WaypointTimer > 0
                && globalNPC.WaypointTarget.Y > npc.Center.Y + 48f
                && Math.Abs(globalNPC.WaypointTarget.X - npc.Center.X) < 32f;
            bool navWantsPlatformDrop = globalNPC.WaypointTimer > 0
                && globalNPC.WaypointAction == tsorcRevampGlobalNPC.NavActionType.DropThroughPlatform;
            bool shouldDropPlatform = globalNPC.NavigationTier >= 1
                ? (playerIsBelow || bfsWantsDrop || navWantsPlatformDrop)
                : playerIsBelow && (globalNPC.BoredTimer > 60 || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < 300);

            if (standing_on_platforms && atLeastOnePlatform && shouldDropPlatform && (globalNPC.NavigationTier < 1 || Math.Abs(npc.velocity.X) < 2f || navWantsPlatformDrop))
            {
                npc.noTileCollide = true;
            }

            // Reset double jump and wind down StuckTimer when standing (velocity.Y == 0)
            if (npc.velocity.Y == 0f)
            {
                globalNPC.UsedDoubleJump = false;
            }

            // StuckTimer: detect ground-level movement blockage and attempt an escape jump
            if (standing_on_solid_tile && globalNPC.NavigationTier >= 1)
            {
                bool tryingToMoveForward = (npc.direction == 1 && npc.velocity.X >= 0f) ||
                                           (npc.direction == -1 && npc.velocity.X <= 0f);
                if (tryingToMoveForward && Math.Abs(npc.velocity.X) < 0.5f)
                {
                    globalNPC.StuckTimer++;
                }
                else
                {
                    // Don't erase wall-contact progress too aggressively. Brief wiggles,
                    // tiny recoil, or overhang checks should not wipe the timer back to 0.
                    if (Math.Abs(npc.velocity.X) > topSpeed * 0.5f)
                        globalNPC.StuckTimer = Math.Max(0, globalNPC.StuckTimer - 1);
                    // NPC is moving freely — also drain the run-up timer so a previous
                    // stuck episode doesn't leave a stale back-up in progress.
                    if (globalNPC.LedgeRunUpTimer > 0 && Math.Abs(npc.velocity.X) > topSpeed * 0.5f
                        && Math.Sign(npc.velocity.X) == npc.direction)
                    {
                        globalNPC.LedgeRunUpTimer = 0;
                        globalNPC.LedgeRunUpDirection = 0;
                    }
                }

                if (globalNPC.StuckTimer > 30)
                {
                    int blockedDirection = npc.direction == 0 ? Math.Sign(npc.velocity.X) : npc.direction;
                    globalNPC.StuckTimer = 0;
                    globalNPC.LedgeRunUpTimer = 0; // cancel any pending run-up; BFS takes over
                    globalNPC.LedgeRunUpDirection = 0;
                    ClearFighterWaypoint(globalNPC);
                    MarkNavDirectionBlocked(globalNPC, blockedDirection, 240);
                    StartNavExplore(npc, globalNPC, -blockedDirection, 180);
                    // Run BFS immediately rather than via BoredTimer — rerouting must fire
                    // even when the NPC has LOS or an old waypoint is steering it into a wall.
                    if (globalNPC.NavigationTier >= 1)
                    {
                        globalNPC.LastNavIntent = "stuck:reroute";
                        TrySetFighterWaypoint(npc, globalNPC, true);
                    }
                    else if (globalNPC.BoredTimer < 21)
                    {
                        globalNPC.BoredTimer = 21;
                    }
                    npc.netUpdate = true;
                }
            }

            // Double jump: apex-triggered mid-air second jump for capable enemies
            if (globalNPC.CanDoubleJump && !globalNPC.UsedDoubleJump && globalNPC.NavigationTier >= 1)
            {
                // Fire when clearly falling (player is still above us) — velocity.Y > 1.5f avoids
                // triggering on the first few frames after stepping off a ledge
                if (!standing_on_solid_tile && npc.velocity.Y > 1.5f && npc.directionY < 0)
                {
                    npc.velocity.Y = -globalNPC.DoubleJumpPower;
                    globalNPC.UsedDoubleJump = true;
                    npc.netUpdate = true;
                }
            }

            // Refresh after movement phase — tile state may have changed (platform drop, etc.)
            lineOfSight = Main.player[npc.target].CanHit(npc);

            // "LOS but player significantly above/below" behaves like no-LOS for boredom.
            // Threshold aligned with the different-floor BFS trigger (48px = 3 tiles) so both
            // systems agree on what "different floor" means.
            bool playerOnDifferentLevel = lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;

            if (globalNPC.BoredTimer >= 0)
            {
                //Increase boredom if it's stuck on a wall it can't pass through, walking back and forth above the player, or can teleport but can't see the player
                if (!lineOfSight || playerOnDifferentLevel)
                {
                    globalNPC.BoredTimer++;

                    //Time it takes to get bored scales with how long it takes to accelerate
                    if (globalNPC.BoredTimer > globalNPC.BoredomThreshold * globalNPC.Patience)
                    {
                        if (!canTeleport)
                        {
                            if (globalNPC.NavigationTier >= 1)
                            {
                                globalNPC.BoredTimer = globalNPC.BoredomThreshold;
                                globalNPC.LastNavIntent = "bored:path-retry";
                                TrySetFighterWaypoint(npc, globalNPC, true);
                            }
                            else
                            {
                                globalNPC.BoredTimer = -540;
                                if (globalNPC.WaypointTimer == 0)
                                    npc.direction *= -1;
                            }
                        }
                        else
                        {
                            //Try to teleport somewhere it has line of sight to the player
                            if (globalNPC.TeleportCountdown == 0)
                            {
                                QueueTeleport(npc, 50, true, globalNPC.TeleportTelegraphTime);
                            }
                        }
                    }

                    // BFS waypoint: trigger as soon as the NPC becomes bored, then keep a
                    // slower fallback rescan while it remains stuck/bored.
                    bool justBecameBored = globalNPC.BoredTimer == 1;
                    bool stuckRescan = globalNPC.StuckTimer >= 20 && globalNPC.StuckTimer % 60 == 0;
                    bool bfsFallback  = Main.GameUpdateCount % 120 == 0;

                    if (globalNPC.NavigationTier >= 1 &&
                        globalNPC.WaypointTimer == 0 &&
                        (justBecameBored || stuckRescan || bfsFallback))
                    {
                        globalNPC.LastNavIntent = justBecameBored ? "bored:first-frame"
                            : stuckRescan ? "bored:stuck-rescan"
                            : "bored:fallback-rescan";
                        TrySetFighterWaypoint(npc, globalNPC, justBecameBored || stuckRescan);
                    }
                }
                //If it's not stuck not and it's not bored decrease the boredom counter
                else if (globalNPC.BoredTimer > 0)
                {
                    globalNPC.BoredTimer -= 1;
                    if (globalNPC.BoredTimer < 0)
                    {
                        globalNPC.BoredTimer = 0;
                    }
                }
            }
            else
            {
                //Always increase it if it's negative (aka bored)
                globalNPC.BoredTimer++;
            }

            // Only hard-reset boredom when the player is truly on the same floor (32px = 2 tiles).
            // The old 80px threshold was killing BoredTimer for players one floor up, preventing
            // boredom BFS from ever firing in the most common stuck scenario.
            if (!globalNPC.Fleeing && lineOfSight && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f)
            {
                globalNPC.BoredTimer = 0;
            }

            // ── Different-floor BFS trigger ───────────────────────────────────────
            // When the NPC has LOS but the player is grounded on a meaningfully
            // different floor (> 3 tiles of vertical separation), BoredTimer is
            // constantly reset to 0 by the block above, so the standard BFS trigger
            // (BoredTimer > 20) never fires.  The NPC just paces left-right forever.
            // Fix: fire BFS independently every ~3 s when this condition persists.
            // Stagger the check by NPC id so all NPCs don't BFS on the same frame.
            if (globalNPC.NavigationTier >= 1 && globalNPC.WaypointTimer == 0 && lineOfSight
                && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f  // > 3 tiles apart vertically
                && Main.player[npc.target].velocity.Y == 0f                          // player is standing (not falling)
                && ((npc.whoAmI + (int)Main.GameUpdateCount) % 60 == 0))             // every ~1 s, staggered per NPC
            {
                globalNPC.LastNavIntent = "los:different-floor";
                TrySetFighterWaypoint(npc, globalNPC, true);
            }

            // WeakTeleport reach tracking: count how long the NPC has been unable to reach the player.
            // "Reached" means LOS within 600px (~38 tiles). After the configured threshold
            // without reaching, briefly disengage, then turn back and resume pursuit.
            if (globalNPC.NavigationTier < 1 && globalNPC.WeakTeleport && globalNPC.WeakTeleportBoredPhase == 0)
            {
                if (lineOfSight && npc.Distance(Main.player[npc.target].Center) < 600f)
                {
                    // NPC is engaging the player; stop counting toward the bored-walk fallback.
                    if (globalNPC.WeakTeleportReachTimer > 0) globalNPC.WeakTeleportReachTimer = 0;
                }
                else
                {
                    globalNPC.WeakTeleportReachTimer++;
                    if (globalNPC.WeakTeleportReachTimer >= globalNPC.WeakTeleportBoredThreshold)
                    {
                        // Start at phase 1 (standstill). The state machine sets direction when
                        // it transitions into phase 2 (walk-away).
                        globalNPC.WeakTeleportBoredPhase = 1;
                        globalNPC.WeakTeleportBoredTimer = 120; // stand still 2 s
                        globalNPC.WeakTeleportReachTimer = 0;
                    }
                }
            }

            LogFighterNavDebug(npc, globalNPC, lineOfSight);

            //Dodging
            if (globalNPC.BoredTimer == 0 && globalNPC.TeleportCountdown == 0 && globalNPC.DodgeCooldown == 0)
            {
                if (canDodgeroll && npc.Distance(Main.player[npc.target].Center) > 160)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        //If a projectile is within 100 units of the NPC and is within 0.3 radian angle of being aimed at them, then try to dodge
                        if (Main.projectile[i].active && Main.projectile[i].friendly && Main.projectile[i].damage > 0 && Main.projectile[i].DistanceSQ(npc.Center) < 40000 && UsefulFunctions.CompareAngles(Main.projectile[i].velocity, UsefulFunctions.Aim(Main.projectile[i].Center, npc.Center, 1)) < 0.3f)
                        {
                            if (Main.rand.NextFloat() < globalNPC.Agility)
                            {
                                bool heightToJump = true;
                                for (int j = 0; j < 8; j++)
                                {
                                    if (UsefulFunctions.IsTileReallySolid(npc.Center + new Vector2(0, -j)))
                                    {
                                        heightToJump = false;
                                        break;
                                    }
                                }
                                //Randomly choose whether to roll or jump
                                if (Main.rand.NextBool() && heightToJump)
                                {
                                    npc.velocity.Y -= 8;
                                }
                                else
                                {
                                    globalNPC.DodgeTimer = 30;
                                }

                                globalNPC.DodgeCooldown = (int)(300 * (1 - globalNPC.Agility));
                            }

                            npc.netUpdate = true;
                            break;
                        }
                    }
                }


                //Pouncing
                if (canPounce && globalNPC.PounceCooldown == 0 && lineOfSight)
                {
                    if (npc.DistanceSQ(Main.player[npc.target].Center) > 40000 / globalNPC.Aggression)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextFloat() * 180 < globalNPC.Aggression)
                        {
                            globalNPC.PounceTimer = 30;
                            globalNPC.PounceCooldown = 300;
                            npc.netUpdate = true;
                        }
                    }
                }
            }
        }





        /// <summary>
        /// Searches for a valid teleport landing spot near <paramref name="player"/> that has
        /// direct line of sight back to the player. Returns the world-space center position of
        /// the landing spot, or <see cref="Vector2.Zero"/> if no valid spot was found.
        /// </summary>
        private static Vector2 FindWeakTeleportDestination(NPC npc, Player player)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                // Random horizontal offset: 35-50 tiles from player, random side.
                // WeakTeleport should help the NPC re-enter pursuit, not appear on top of the player.
                float offsetX = Main.rand.NextFloat(35f, 50f) * 16f * (Main.rand.NextBool() ? 1f : -1f);
                // Small vertical scatter so the NPC can land on platforms above/below player
                float offsetY = Main.rand.NextFloat(-5f, 5f) * 16f;
                Vector2 candidate = player.Center + new Vector2(offsetX, offsetY);

                int tileX = (int)(candidate.X / 16f);
                int tileY = (int)(candidate.Y / 16f);

                // Skip positions inside solid walls
                if (UsefulFunctions.IsTileReallySolid(tileX, tileY) ||
                    UsefulFunctions.IsTileReallySolid(tileX, tileY - 1))
                    continue;

                // Find the first ground tile below (solid tile or platform, within 6 tiles)
                int groundY = -1;
                for (int dy = 0; dy <= 6; dy++)
                {
                    Tile t = Framing.GetTileSafely(tileX, tileY + dy);
                    if (UsefulFunctions.IsTileReallySolid(tileX, tileY + dy) ||
                        (t.HasTile && TileID.Sets.Platforms[t.TileType]))
                    {
                        groundY = tileY + dy;
                        break;
                    }
                }
                if (groundY == -1) continue;

                // World-space center of the NPC if it were standing on that tile
                Vector2 centerAtDest = new Vector2(tileX * 16f + 8f, groundY * 16f - npc.height / 2f);

                // Require clear LOS from landing spot center to player
                if (!Collision.CanHitLine(centerAtDest, 1, 1, player.Center, 1, 1))
                    continue;

                return centerAtDest;
            }
            return Vector2.Zero;
        }

        //AI snippits go here! Simply call these in the npc's main AI function to add them
        #region AI Snippets

        public static int ProjectileTelegraphTime = 25;

        private static float GetStandingFireChance(tsorcRevampGlobalNPC globalNPC, float baseChance)
        {
            float aggressionMultiplier = 1f;
            if (globalNPC.Aggression >= 0f)
            {
                aggressionMultiplier = MathHelper.Clamp(1f - globalNPC.Aggression / 2.5f, 0f, 1f);
            }

            return MathHelper.Clamp(baseChance * aggressionMultiplier, 0f, 1f);
        }

        private static bool HasCrossableGapTowardPlayer(NPC npc, tsorcRevampGlobalNPC globalNPC, out int travelDirection)
        {
            travelDirection = 0;
            if (globalNPC.NavigationTier < 1 || npc.velocity.Y != 0f)
            {
                return false;
            }

            float playerDeltaX = Main.player[npc.target].Center.X - npc.Center.X;
            if (Math.Abs(playerDeltaX) < 48f)
            {
                return false;
            }

            travelDirection = playerDeltaX < 0f ? -1 : 1;
            int aheadX = travelDirection == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int belowFeetY = (int)(npc.position.Y + npc.height + 8f) / 16;

            const int maxLandingScanDepth = 6;
            const int maxGapStartScan = 3;
            float maxJumpable = Math.Max(4f, Math.Min(8f, globalNPC.MaxJumpBoost + 2f));

            for (int gapStart = 0; gapStart <= maxGapStartScan; gapStart++)
            {
                int gapX = aheadX + gapStart * travelDirection;
                if (tsorcRevampGlobalNPC.BfsCanStand(npc, gapX, belowFeetY))
                {
                    continue;
                }

                for (int scan = gapStart + 1; scan <= maxJumpable; scan++)
                {
                    int scanX = aheadX + scan * travelDirection;
                    for (int dy = 0; dy <= maxLandingScanDepth; dy++)
                    {
                        if (tsorcRevampGlobalNPC.BfsCanStand(npc, scanX, belowFeetY + dy))
                        {
                            // Only jump same-level gaps. One-tile drops/slopes should be walked
                            // down naturally instead of being treated like pits.
                            return dy == 0;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        private static bool IsFighterStandableTile(int x, int y)
        {
            if (UsefulFunctions.IsTileReallySolid(x, y))
            {
                return true;
            }

            if (Main.tile.Width > x && Main.tile.Height > y && x >= 0 && y >= 0)
            {
                Tile tile = Main.tile[x, y];
                return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
            }

            return false;
        }

        private static void ClearFighterWaypoint(tsorcRevampGlobalNPC globalNPC)
        {
            globalNPC.WaypointTimer = 0;
            globalNPC.WaypointTarget = Vector2.Zero;
            globalNPC.WaypointAction = tsorcRevampGlobalNPC.NavActionType.None;
            globalNPC.WaypointNoProgressTimer = 0;
            globalNPC.LastWaypointDistance = 0f;
            globalNPC.NavRouteIndex = 0;
            globalNPC.NavRouteCount = 0;
            globalNPC.NavRouteTimer = 0;
            globalNPC.NavRouteNoProgressTimer = 0;
            globalNPC.LastNavRouteDistance = 0f;
        }

        private static void MarkNavDirectionBlocked(tsorcRevampGlobalNPC globalNPC, int blockedDirection, int duration = 180)
        {
            if (blockedDirection == 0)
            {
                return;
            }

            globalNPC.NavBlockedDirection = Math.Sign(blockedDirection);
            globalNPC.NavBlockedDirectionTimer = Math.Max(globalNPC.NavBlockedDirectionTimer, duration);
        }

        private static void StartNavExplore(NPC npc, tsorcRevampGlobalNPC globalNPC, int preferredDirection, int duration = 180)
        {
            int direction = preferredDirection == 0
                ? Math.Sign(Main.player[npc.target].Center.X - npc.Center.X)
                : Math.Sign(preferredDirection);

            if (direction == 0)
            {
                direction = npc.direction == 0 ? 1 : npc.direction;
            }

            if (globalNPC.NavBlockedDirectionTimer > 0 && direction == globalNPC.NavBlockedDirection)
            {
                direction *= -1;
            }

            globalNPC.NavExploreDirection = direction;
            globalNPC.NavExploreTimer = Math.Max(globalNPC.NavExploreTimer, duration);
            globalNPC.FighterNoLosPursuitBoostTimer = Math.Max(globalNPC.FighterNoLosPursuitBoostTimer, 90);
            globalNPC.BoredTimer = Math.Max(globalNPC.BoredTimer, 1);
            npc.direction = direction;
            npc.spriteDirection = direction;
        }

        private static bool TrySetFighterWaypoint(NPC npc, tsorcRevampGlobalNPC globalNPC, bool force = false)
        {
            if (globalNPC.NavigationTier < 1 || globalNPC.WaypointTimer > 0)
            {
                globalNPC.LastWaypointResult = globalNPC.NavigationTier < 1 ? "skip:tier0" : "skip:active";
                return false;
            }
            if (!force && globalNPC.WaypointSearchCooldown > 0)
            {
                globalNPC.LastWaypointResult = $"skip:cooldown-{globalNPC.WaypointSearchCooldown}";
                return false;
            }

            Span<Vector2> routeTargets = stackalloc Vector2[tsorcRevampGlobalNPC.MaxNavRouteSteps];
            Span<tsorcRevampGlobalNPC.NavActionType> routeActions = stackalloc tsorcRevampGlobalNPC.NavActionType[tsorcRevampGlobalNPC.MaxNavRouteSteps];
            if (tsorcRevampGlobalNPC.BfsFindRoute(npc, globalNPC.MaxJumpPower, globalNPC.MaxJumpBoost, routeTargets, routeActions, out int routeCount))
            {
                int routeStart = 0;
                while (routeStart < routeCount - 1 && !IsUsefulFighterWaypoint(npc, routeTargets[routeStart], routeActions[routeStart], routeCount - routeStart))
                {
                    routeStart++;
                }

                Vector2 waypoint = routeTargets[routeStart];
                tsorcRevampGlobalNPC.NavActionType action = routeActions[routeStart];
                if (!IsUsefulFighterWaypoint(npc, waypoint, action))
                {
                    globalNPC.WaypointSearchFailures++;
                    globalNPC.LastWaypointResult = $"fail:useless-{action} x{globalNPC.WaypointSearchFailures}";
                    globalNPC.WaypointSearchCooldown = force ? 12 : 30;
                    bool directLos = Main.player[npc.target].CanHit(npc) && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;
                    if (!directLos && (force || globalNPC.WaypointSearchFailures >= 3))
                    {
                        int preferredDirection = Math.Sign(Main.player[npc.target].Center.X - npc.Center.X);
                        StartNavExplore(npc, globalNPC, preferredDirection, 150);
                        globalNPC.LastNavIntent = "explore:useless-waypoint";
                    }
                    return false;
                }

                int copiedRouteCount = Math.Min(routeCount - routeStart, tsorcRevampGlobalNPC.MaxNavRouteSteps);
                for (int i = 0; i < copiedRouteCount; i++)
                {
                    globalNPC.NavRouteTargets[i] = routeTargets[routeStart + i];
                    globalNPC.NavRouteActions[i] = routeActions[routeStart + i];
                }
                globalNPC.NavRouteIndex = 0;
                globalNPC.NavRouteCount = copiedRouteCount;
                globalNPC.NavRouteTimer = 0;
                globalNPC.NavRouteNoProgressTimer = 0;
                globalNPC.LastNavRouteDistance = npc.Distance(waypoint);
                globalNPC.WaypointTarget = waypoint;
                globalNPC.WaypointTimer = 420;
                globalNPC.WaypointAction = action;
                globalNPC.LastWaypointDistance = npc.Distance(waypoint);
                globalNPC.WaypointNoProgressTimer = 0;
                globalNPC.NavExploreTimer = 0;
                globalNPC.NavExploreDirection = 0;
                globalNPC.BoredTimer = Math.Max(globalNPC.BoredTimer, 1);
                globalNPC.WaypointSearchFailures = 0;
                string skippedPrefix = routeStart > 0 ? $"skip{routeStart} " : "";
                globalNPC.LastWaypointResult = $"{skippedPrefix}route:{globalNPC.NavRouteCount} set:{action} ({waypoint.X / 16f:F1},{waypoint.Y / 16f:F1})";
                globalNPC.WaypointSearchCooldown = force ? 10 : 20;
                npc.netUpdate = true;
                return true;
            }

            globalNPC.WaypointSearchFailures++;
            globalNPC.LastWaypointResult = $"fail:bfs x{globalNPC.WaypointSearchFailures}";
            globalNPC.WaypointSearchCooldown = force ? 20 : 45;
            bool hasDirectLos = Main.player[npc.target].CanHit(npc) && Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) < 32f;
            if (!hasDirectLos && (force || globalNPC.WaypointSearchFailures >= 3))
            {
                int preferredDirection = Math.Sign(Main.player[npc.target].Center.X - npc.Center.X);
                StartNavExplore(npc, globalNPC, preferredDirection, 180);
                globalNPC.LastNavIntent = "explore:bfs-failed";
            }
            return false;
        }

        private static bool IsUsefulFighterWaypoint(NPC npc, Vector2 waypoint, tsorcRevampGlobalNPC.NavActionType action, int remainingRouteSteps = 1)
        {
            Player player = Main.player[npc.target];
            Vector2 delta = waypoint - npc.Center;

            if (action == tsorcRevampGlobalNPC.NavActionType.Walk && Math.Abs(delta.X) < 18f && Math.Abs(delta.Y) < 18f)
            {
                return remainingRouteSteps > 1;
            }
            if (action == tsorcRevampGlobalNPC.NavActionType.Walk && Math.Abs(delta.Y) > 40f)
            {
                return false;
            }
            if (action == tsorcRevampGlobalNPC.NavActionType.JumpTo && Math.Abs(delta.X) < 16f && Math.Abs(delta.Y) < 24f)
            {
                return false;
            }

            bool playerClearlyAbove = player.Center.Y < npc.Center.Y - 48f;
            bool waypointBelowNpc = waypoint.Y > npc.Center.Y + 18f;
            if (playerClearlyAbove && waypointBelowNpc && action == tsorcRevampGlobalNPC.NavActionType.JumpTo)
            {
                return false;
            }

            float currentDistance = npc.Distance(player.Center);
            float waypointDistance = Vector2.Distance(waypoint, player.Center);
            if (waypointDistance > currentDistance + 160f && action != tsorcRevampGlobalNPC.NavActionType.Drop)
            {
                return false;
            }

            return true;
        }

        private static void LogFighterNavDebug(NPC npc, tsorcRevampGlobalNPC globalNPC, bool lineOfSight)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().DebugMode || globalNPC.NavigationTier < 1)
            {
                return;
            }

            bool interesting = globalNPC.BoredTimer > 0
                || globalNPC.StuckTimer > 0
                || globalNPC.WaypointTimer > 0
                || globalNPC.WaypointSearchFailures > 0
                || globalNPC.NavExploreTimer > 0
                || globalNPC.NavBlockedDirectionTimer > 0
                || !lineOfSight
                || Math.Abs(Main.player[npc.target].Center.Y - npc.Center.Y) > 48f;
            if (!interesting)
            {
                return;
            }

            int now = (int)Main.GameUpdateCount;
            if (now - globalNPC.LastNavDebugLogTick < 60)
            {
                return;
            }
            globalNPC.LastNavDebugLogTick = now;

            try
            {
                string separator = Path.DirectorySeparatorChar.ToString();
                string logDir = Main.SavePath + separator + "Logs";
                Directory.CreateDirectory(logDir);
                string logPath = logDir + separator + "tsorcRevamp-nav.log";
                Player player = Main.player[npc.target];
                string waypoint = globalNPC.WaypointTimer > 0
                    ? $"{globalNPC.WaypointAction}@({globalNPC.WaypointTarget.X / 16f:F1},{globalNPC.WaypointTarget.Y / 16f:F1})/{globalNPC.WaypointTimer}"
                    : "none";
                string route = globalNPC.NavRouteCount > 0
                    ? $"{globalNPC.NavRouteIndex + 1}/{globalNPC.NavRouteCount}"
                    : "none";
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) dist={npc.Distance(player.Center):F0} los={lineOfSight} yDiff={player.Center.Y - npc.Center.Y:F0} tier={globalNPC.NavigationTier} bored={globalNPC.BoredTimer} stuck={globalNPC.StuckTimer} route={route} wp={waypoint} intent={globalNPC.LastNavIntent} result={globalNPC.LastWaypointResult} cd={globalNPC.WaypointSearchCooldown} wpNoProg={globalNPC.WaypointNoProgressTimer} blocked={globalNPC.NavBlockedDirection}/{globalNPC.NavBlockedDirectionTimer} explore={globalNPC.NavExploreDirection}/{globalNPC.NavExploreTimer} ledgeRun={globalNPC.LedgeRunUpTimer} vault={globalNPC.LedgeVaultTimer} jumpCd={globalNPC.NavJumpCooldown} stopFire={globalNPC.CanStopToFire}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // Debug logging should never affect NPC AI.
            }
        }


        public static bool SimpleProjectile(NPC npc)
        {
            return SimpleProjectile(npc, true);
        }

        ///<summary> 
        ///Fires a projectile with various parameters. Uses any timer variable you give it, and goes in the npc's AI() function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="actuallyFire">This lets you use a condition to block the projectile from firing unless it is true (such as having line of sight to the player)</param>
        public static bool SimpleProjectile(NPC npc, bool actuallyFire = true)
        {
            //Get the globalnpc for this NPC, which holds important data
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            //This should only not equal -1 on the frame an attack successfully fires. This resets it afterward.
            globalNPC.AttackSucceeded = -1;

            //Do not fire if it needs line of sight and does not have it
            if (globalNPC.CurrentAttack.needsLineOfSight && !Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
            {
                actuallyFire = false;
            }

            //If the color was not set, use white
            if (globalNPC.CurrentAttack.color == null)
            {
                globalNPC.CurrentAttack.color = Color.White;
            }

            //Increment the timer. Stop increasing it once we reach the telegraph time. Only continue once it is actually firing. Once it is actually firing do not stop incrementing the timer, so that it can not stop firing after telegraphing a shot.
            if (globalNPC.ProjectileTimer < globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime || actuallyFire || globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime)
            {
                globalNPC.ProjectileTimer++;

                //Spawn a telegraph flash once the telegraph time is reached
                if (globalNPC.ProjectileTimer == 1 + globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime)
                {
                    Vector2 spawnPosition = npc.position;
                    if (npc.direction == 1)
                    {
                        spawnPosition.X += npc.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(npc.GetSource_FromThis(), spawnPosition, npc.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(globalNPC.CurrentAttack.color.Value));
                    }
                }
            }

            //If it's supposed to stop moving when firing, then do so
            if (globalNPC.CanStopToFire && globalNPC.CurrentAttack.stopBefore && !globalNPC.CanPassThroughWalls)
            {
                bool inTelegraphWindow = globalNPC.ProjectileTimer > globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime;
                float stopBeforeChance = GetStandingFireChance(globalNPC, globalNPC.CurrentAttack.stopBeforeChance);

                if (inTelegraphWindow && Main.rand.NextFloat() < stopBeforeChance)
                {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0f; // suppress jump-frame animation while aiming

                    // Standing-fire roll: on the first frame of the telegraph window, tier-2 NPCs
                    // may commit to firing N shots in a row without resuming movement.
                    // Aggression lowers the chance to stand; Patience raises the burst count.
                    if (globalNPC.CanStopToFire && globalNPC.NavigationTier >= 2 && globalNPC.FighterRangedStandShotsRemaining == 0
                        && globalNPC.ProjectileTimer == globalNPC.CurrentAttack.timerCap - ProjectileTelegraphTime + 1
                        && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float aggressionFraction = Math.Clamp(globalNPC.Aggression / 2.5f, 0f, 1f);
                        if (Main.rand.NextFloat() > aggressionFraction)
                        {
                            globalNPC.FighterRangedStandShotsRemaining = 1 + Main.rand.Next(0, 1 + (int)globalNPC.Patience);
                        }
                    }
                }
            }

            if (globalNPC.ProjectileTimer >= globalNPC.CurrentAttack.timerCap)
            {
                globalNPC.ProjectileTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (globalNPC.CurrentAttack.overshoot == null)
                    {
                        globalNPC.CurrentAttack.overshoot = Vector2.Zero;
                    }
                    Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center + globalNPC.CurrentAttack.overshoot.Value, globalNPC.CurrentAttack.velocity, globalNPC.CurrentAttack.gravity);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, globalNPC.CurrentAttack.type, globalNPC.CurrentAttack.damage, 0f, Main.myPlayer, globalNPC.CurrentAttack.ai0, globalNPC.CurrentAttack.ai1);
                }
                if (globalNPC.CurrentAttack.sound != null)
                {
                    SoundEngine.PlaySound(globalNPC.CurrentAttack.sound.Value, npc.Center);
                }

                globalNPC.AttackSucceeded = globalNPC.AttackIndex;
                RegisterFighterAttack(npc);
                globalNPC.AttackIndex = globalNPC.NextAttackIndex;
                globalNPC.NextAttackIndex = WeightedRandomAttackSelection(globalNPC);

                // Consume one standing-fire charge. When exhausted, exit standing mode.
                if (globalNPC.FighterRangedStandShotsRemaining > 0)
                {
                    if (--globalNPC.FighterRangedStandShotsRemaining == 0)
                    {
                        npc.TargetClosest(true); // resume pursuit
                    }
                }
            }

            return false;
        }

        public static void RegisterFighterAttack(NPC npc, int attacksBeforePause = 4, int pauseTicks = 60)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.NavigationTier < 2)
            {
                return;
            }

            globalNPC.FighterAttacksSincePause++;
            if (globalNPC.FighterAttacksSincePause >= attacksBeforePause)
            {
                globalNPC.FighterAttacksSincePause = 0;
                globalNPC.FighterPostAttackPauseTimer = pauseTicks;
                globalNPC.BoredTimer = 0;
            }
        }

        /// <summary>
        /// Picks a random attack from AttackList based on the weight of each entry
        /// </summary>
        /// <param name="globalNPC">The NPC being operated on</param>
        /// <returns></returns>
        public static int WeightedRandomAttackSelection(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.AttackList.Count == 0 || globalNPC.AttackList.Count == 1)
            {
                return 0;
            }
            float weightMax = 0;
            foreach (ProjectileData data in globalNPC.AttackList)
            {
                weightMax += data.weight;
            }

            float randomVal = Main.rand.NextFloat(weightMax);

            float runningTotal = 0;
            for (int i = 0; i < globalNPC.AttackList.Count; i++)
            {
                runningTotal += globalNPC.AttackList[i].weight;
                if (randomVal < runningTotal)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Simple class which holds all the data relevant to firing a projectile
        /// </summary>
        public class ProjectileData
        {
            public int timerCap;
            public int type;
            public int damage;
            public float velocity;
            public SoundStyle? sound;
            public float gravity;
            public float ai0;
            public float ai1;
            public Vector2? overshoot;
            public Color? color;
            public bool stopBefore;
            public bool needsLineOfSight;
            public float weight;
            public Func<NPC, bool> condition;
            public float stopBeforeChance;

            public ProjectileData(int projectileType, int timerCap, int projectileDamage, float projectileVelocity, SoundStyle? shootSound = null, float projectileGravity = 0.035f, float ai0 = 0, float ai1 = 0, Vector2? overshoot = null, Color? telegraphColor = null, bool stopBeforeFiring = true, bool needsLineOfSight = true, float weight = 1, Func<NPC, bool> condition = null, float stopBeforeChance = 0.1f)
            {
                type = projectileType;
                this.timerCap = timerCap;
                damage = projectileDamage;
                velocity = projectileVelocity;
                sound = shootSound;
                gravity = projectileGravity;
                this.ai0 = ai0;
                this.ai1 = ai1;
                this.overshoot = overshoot;
                color = telegraphColor;
                stopBefore = stopBeforeFiring;
                this.needsLineOfSight = needsLineOfSight;
                this.weight = weight;
                this.condition = condition;
                this.stopBeforeChance = stopBeforeChance;
            }
        }

        ///<summary> 
        ///Lets the npc leap at players who are close, does not use any ai slots, and goes in an npc's ai function
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="hopSpeedX">How fast it leaps horizontally</param>
        ///<param name="hopSpeedY">How fast it leaps vertically</param>
        ///<param name="minimumSpeed">How fast it has to be running to be allowed to hop</param>
        ///<param name="hopRange">It leaps at the player when it is this close to them</param>
        public static void LeapAtPlayer(NPC npc, float hopSpeedX, float hopSpeedY, float minimumSpeed, float hopRange = 64)
        {
            //If the player is within range and if the npc is moving fast enough to be allowed to hop, then hop
            if (npc.velocity.Y == 0f && Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < hopRange && Math.Abs(npc.Center.Y - Main.player[npc.target].Center.Y) < hopRange && ((npc.direction > 0 && npc.velocity.X >= minimumSpeed) || (npc.direction < 0 && npc.velocity.X <= -minimumSpeed)))
            {
                npc.velocity.X = hopSpeedX * npc.direction;
                npc.velocity.Y = -hopSpeedY;
                npc.netUpdate = true;
            }
        }

        ///<summary> 
        ///Calculates a position to teleport the NPC to. Returns null if there is no valid position.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static Vector2? GenerateTeleportPosition(NPC npc, int range, bool requireLineofSight = true)
        {
            //Do not teleport if the player is way way too far away (stops enemies following you home if you mirror away)
            if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            { // far away from target; 2000 pixels = 125 blocks
                return null;
            }

            //Try 100 times at most
            for (int i = 0; i < 100; i++)
            {
                //Pick a random point to target. Make sure it's at least 11 blocks away from the player to avoid cheap hits.
                Vector2 teleportTarget = Vector2.Zero;
                if (range < 13)
                {
                    range = 13;
                }
                teleportTarget.X = Main.rand.Next(11, range);
                if (Main.rand.NextBool())
                {
                    teleportTarget.X *= -1;
                }

                //Move teleportTarget up a few blocks, since in the next step the algorithm will search downward from this point to find a valid landing spot
                teleportTarget.Y -= 12;

                //Add the player's position to it to convert it to an actual tile coordinate
                teleportTarget += Main.player[npc.target].position / 16;

                //Starting from the point we picked, go down one block at a time until we find hit a solid block
                bool odd = false;
                for (int y = 0; Math.Abs(y) < range / 2;)
                {
                    if (odd)
                    {
                        y *= -1;
                        y++;
                        odd = !odd;
                    }
                    else
                    {
                        y *= -1;
                        odd = !odd;
                    }
                    if (UsefulFunctions.IsTileReallySolid((int)teleportTarget.X, (int)teleportTarget.Y + y))
                    {
                        //Skip to the next tile if any of the following is true:

                        // If there are solid blocks in the way, leaving no room to teleport to
                        if (Collision.SolidTiles((int)teleportTarget.X - 1, (int)teleportTarget.X + 1, (int)teleportTarget.Y + y - 4, (int)teleportTarget.Y + y - 1))
                        {
                            //Main.NewText("Fail 1");
                            continue;
                        }

                        //If it requires line of sight, and there is not a clear path, and it has not tried at least 50 times, then skip to the next try
                        else if (requireLineofSight && !(Collision.CanHit(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2) && Collision.CanHitLine(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2)))
                        {
                            //Main.NewText("Fail 3");
                            continue;
                        }

                        //If the selected tile has lava above it, and the npc isn't immune
                        else if (Main.tile[(int)teleportTarget.X, (int)teleportTarget.Y + y - 1].LiquidType == LiquidID.Lava && !npc.lavaImmune)
                        {
                            //Main.NewText("Fail 4");
                            continue;
                        }

                        //Then teleport and return
                        teleportTarget.X = ((int)teleportTarget.X * 16 - npc.width / 2); //Center npc at target
                        teleportTarget.Y = (((int)teleportTarget.Y + y) * 16 - npc.height); //Subtract npc.height from y so block is under feet
                        npc.TargetClosest(true);
                        npc.netUpdate = true;

                        if(teleportTarget.Length() < 400)
                        {
                            UsefulFunctions.BroadcastText("Teleport error!");
                            UsefulFunctions.BroadcastText("NPC Name: " + npc.GivenOrTypeName);
                            UsefulFunctions.BroadcastText("Target coordinates: " + teleportTarget);
                            UsefulFunctions.BroadcastText("Please report this to our discord!");
                        }
                        return teleportTarget;
                    }
                }
            }

            return null;
        }


        ///<summary> 
        ///Teleports the NPC to a random position within a specified range around the player, includes effects. Does not teleport the enemy if no safe location exists.
        ///Will not teleport enemies right next to the player. Teleports enemies somewhere with line of sight to the player by default.
        ///</summary>
        ///<param name="npc">The npc itself this function will run on</param>
        ///<param name="range">The max range from the player it can teleport. Minimum is 12 blocks.</param>
        ///<param name="requireLineofSight">Try to teleport somewhere that has line of sight to the player</param>
        public static void TeleportImmediately(NPC npc, int range, bool requireLineofSight = true)
        {
            QueueTeleport(npc, range, requireLineofSight, 60);
            ExecuteQueuedTeleport(npc);
        }

        public static void QueueTeleport(NPC npc, int range, bool requireLineofSight = true, int TeleportTelegraphTime = 140)
        {
            Vector2? potentialNewPos;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    potentialNewPos = GenerateTeleportPosition(npc, range, requireLineofSight);
                    if (potentialNewPos.HasValue && (!requireLineofSight || (Collision.CanHit(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1))))
                    {
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown = TeleportTelegraphTime;
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph = potentialNewPos.Value;
                        SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, npc.whoAmI, TeleportTelegraphTime);
                            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: TeleportTelegraphTime);
                        }

                        break;
                    }
                }
            }
        }

        private static void SpawnTeleportMist(Vector2 position, Vector2 direction, int width, int height, tsorcRevampGlobalNPC globalNPC)
        {
            for (int i = 0; i < globalNPC.TeleportDustCount; i++)
            {
                Vector2 randomVelocity = direction * Main.rand.NextFloat(2.5f, 5.5f)
                    + Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(width * 0.4f, height * 0.4f),
                    globalNPC.TeleportDustType, randomVelocity, 150, globalNPC.TeleportDustColor, globalNPC.TeleportDustScale);
                dust.noGravity = true;
                dust.fadeIn = 0.45f;
            }
        }

        public static void ExecuteQueuedTeleport(NPC npc)
        {
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph == Vector2.Zero)
            {
                return;
            }
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            SoundEngine.PlaySound(SoundID.Item8, npc.Center);


            Vector2 diff = globalNPC.TeleportTelegraph - npc.Center;
            float length = diff.Length();
            if (length > 0f)
                diff /= length;

            SpawnTeleportMist(npc.Center, diff, npc.width, npc.height, globalNPC);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
            }

            npc.Center = globalNPC.TeleportTelegraph;

            SpawnTeleportMist(npc.Center, -diff, npc.width, npc.height, globalNPC);
        }

        public static void FighterOnHit(NPC npc, bool melee)
        {
            if (melee)
            {
                npc.localAI[1] = 80f; // was 100
                npc.knockBackResist = 0.09f;
                // Abort any standing-fire burst — the NPC will be knocked airborne anyway
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().FighterRangedStandShotsRemaining = 0;

                //TELEPORT MELEE
                if (Main.rand.NextBool(18))
                {
                    TeleportImmediately(npc, 25, true);
                }
                //WHEN HIT, CHANCE TO JUMP BACKWARDS 
                else if (Main.rand.NextBool(8))
                {
                    //npc.TargetClosest(false);
                    npc.velocity.Y = -8f;
                    npc.velocity.X = -4f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }
                //WHEN HIT, CHANCE TO DASH STEP BACKWARDS 
                else if (Main.rand.NextBool(8))
                {
                    npc.velocity.Y = -4f;
                    npc.velocity.X = -7f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }
                else if (Main.rand.NextBool(4))
                {
                    npc.TargetClosest(true);
                    npc.velocity.Y = -7f;
                    npc.velocity.X = -10f * npc.direction;
                    npc.localAI[1] = 150f;
                    npc.netUpdate = true;
                }

            }
            if (!melee && Main.rand.NextBool())
            {
                if (Main.rand.NextBool(4))
                {

                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                    Main.dust[dust].noGravity = true;

                    npc.velocity.Y = -9f;
                    npc.velocity.X = 4f * npc.direction;
                    npc.TargetClosest(true);

                    if ((float)npc.direction * npc.velocity.X > 4)
                    {
                        npc.velocity.X = (float)npc.direction * 4;
                    }
                    npc.netUpdate = true;
                }
                if (Main.rand.NextBool(6))
                {

                    npc.ai[0] = 0f;
                    npc.velocity.Y = -5f;
                    npc.velocity.X *= 4f; // burst forward
                    npc.TargetClosest(true);

                    npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                    if ((float)npc.direction * npc.velocity.X > 5)
                    {
                        npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                    }
                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.NextBool(8))
                    {
                        npc.TargetClosest(true);
                        npc.spriteDirection = npc.direction;
                        npc.ai[0] = 0f;
                        npc.velocity.Y = -6f;
                    }
                    npc.netUpdate = true;
                }
                if (npc.Distance(Main.player[npc.target].Center) > 300 && Main.rand.NextBool(24))
                {
                    TeleportImmediately(npc, 20, false);
                }
            }

        }
        #region Red Knight Hit AI
        public static void RedKnightOnHit(NPC npc, bool melee) //ref int stunlockBreak
        {
            /*
            // Ensure that the stunlockBreak timer is always decreasing
            stunlockBreak--;

            // Increment the stunlockBreak timer
            stunlockBreak += 600;

            // Check if the stunlockBreak timer is greater than or equal to 3000
            if (stunlockBreak >= 2000)
            {
                
                // Set knockback to 0 and decrement the stunlockBreak timer
                npc.knockBackResist = 0;
                
            }
 
            if (stunlockBreak < 0)
            {
                stunlockBreak = 0;
            }
            */
            if (melee)
            {
                // Ensures melee can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(10);

                    switch (randomChoice)
                    {
                        case 0:
                            npc.ai[1] = 0f;
                            break;

                        case 1:
                            npc.ai[1] = 700f;
                            break;

                        case 2:
                            npc.ai[1] = 200f;
                            break;

                        case 3:
                            npc.ai[1] = 800f;
                            break;
                        case 4:
                            // Big jump back - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.ai[1] = 0f;
                            }
                            break;
                        case 5:
                            // Small dash back - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -8f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;


                            }
                            // Alt dash - Bomb
                            else if (Main.rand.NextBool(4))
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash back - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }                          
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(4))
                            {
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            {
                                // Poison TP
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -6f * npc.direction;
                                npc.ai[1] = 260f;
                            }
                            break;
                        case 8:
                            //Small dash back - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(2))
                            {
                                // Jump high
                                npc.TargetClosest(true);
                                npc.velocity.Y = -11f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;

                            }
                            break;
                        case 9:
                            // Dash back - Poison
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 280f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 280f;
                            }
                            break;


                    }
                    npc.netUpdate = true;
                }
                else
                {
                    //npc.knockBackResist = 0;
                }

                //npc.knockBackResist = 0.4f; //was 0.9            
            }

            if (!melee)
            {
                tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (globalNPC.FighterRangedHitInterruptedPause || globalNPC.FighterPostAttackPauseTimer > 0 || globalNPC.FighterRangedStandShotsRemaining > 0)
                {
                    globalNPC.FighterRangedHitInterruptedPause = false;
                    globalNPC.FighterPostAttackPauseTimer = 0;
                    globalNPC.FighterRangedStandShotsRemaining = 0;
                    globalNPC.BoredTimer = 0;
                    npc.TargetClosest(true);
                    float distance = npc.Distance(Main.player[npc.target].Center);
                    if (distance > 320f)
                    {
                        npc.ai[1] = Main.rand.NextBool() ? 90f : 830f;
                    }
                    else
                    {
                        npc.velocity.Y = -5f;
                        npc.velocity.X += npc.direction * 5f;
                    }
                    npc.netUpdate = true;
                    return;
                }

                // Ensures ranged can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(9);

                    switch (randomChoice)
                    {
                        case 0:
                            // Burst forward
                            if (Main.rand.NextBool(5))
                            {
                                npc.velocity.Y = -9f;
                                npc.velocity.X = 4f * npc.direction;
                                npc.TargetClosest(true);

                                if ((float)npc.direction * npc.velocity.X > 4)
                                {
                                    npc.velocity.X = (float)npc.direction * 3;  //  3 was 4 - this caps the top speed
                                }
                                npc.netUpdate = true;
                            }
                            break;

                        case 1:
                            // Burst forward
                            if (Main.rand.NextBool(6))
                            {
                                npc.velocity.Y = -6f;
                                npc.velocity.X *= 4f; // burst forward
                                npc.TargetClosest(true);

                                npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                                if ((float)npc.direction * npc.velocity.X > 5)
                                {
                                    npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                                }

                                // Chance to jump after dash
                                if (Main.rand.NextBool(6))
                                {
                                    npc.TargetClosest(true);
                                    npc.spriteDirection = npc.direction;
                                    npc.velocity.Y = -6f;
                                }

                                npc.netUpdate = true;
                            }
                            break;

                        case 2:
                            // Teleport
                            if (npc.Distance(Main.player[npc.target].Center) > 400 && Main.rand.NextBool(4))
                            {
                                TeleportImmediately(npc, 15, false);
                            }
                            break;

                        case 3:
                            // Dash backwards - Poison
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 290f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 290f;
                            }
                            break;
                        case 4:
                            // Chance to big jump backwards - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 5:
                            // Small dash backwards - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = 6f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;
                            }
                            // Alt dash backwards - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash backwards - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(4))
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            // Poision Teleport
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -5f * npc.direction;
                                npc.ai[1] = 250f;
                            }
                            break;
                        case 8:
                            // Small dash backwards - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            // Jump high, slightly forward
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = 3f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 9:
                            // Attack interrupt; for Great Red Knight it cycles to DD2 attack at 1/2 health
                            npc.ai[1] = 700f;
                            break;
                    }
                    npc.netUpdate = true;
                }
            }
        }
        #endregion

        #region Gwyn Hit AI
        public static void GwynOnHit(NPC npc, bool melee) //ref int stunlockBreak
        {

            if (melee)
            {
                // Ensures melee can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(10);

                    switch (randomChoice)
                    {
                        case 0:
                            npc.ai[1] = 50f;
                            break;

                        case 1:
                            npc.ai[1] = 700f;
                            break;

                        case 2:
                            npc.ai[1] = 200f;
                            break;

                        case 3:
                            npc.ai[1] = 800f;
                            break;
                        case 4:
                            // Big jump back - Spear
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.ai[1] = 50f;
                            }
                            break;
                        case 5:
                            // Small dash back - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -8f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;


                            }
                            // Alt dash - Bomb
                            else
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash back - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 50f;
                            }
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(2))
                            {
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.netUpdate = true;
                            }
                            else
                            {
                                // Poison TP
                                npc.spriteDirection = npc.direction;
                                TeleportImmediately(npc, 22, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -6f * npc.direction;
                                npc.ai[1] = 260f;
                            }
                            break;
                        case 8:
                            //Small dash back - Spear
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                // Jump high
                                npc.TargetClosest(true);
                                npc.velocity.Y = -11f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 130f;

                            }
                            break;
                        case 9:
                            // Dash back - Poison
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 280f;
                                npc.netUpdate = true;
                            }
                            else
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 280f;
                            }
                            break;


                    }
                    npc.netUpdate = true;
                }
                else
                {
                    npc.knockBackResist = 0;
                }

                npc.knockBackResist = 0.4f; //was 0.9            
            }

            if (!melee)
            {
                // Ensures ranged can't interrupt attack once the flash telegraph triggers
                if ((npc.ai[1] < 155f) || (npc.ai[1] > 180f && npc.ai[1] < 300f) || (npc.ai[1] > 325f && npc.ai[1] < 900f) || npc.ai[1] > 925f)
                {
                    int randomChoice = Main.rand.Next(9);

                    switch (randomChoice)
                    {
                        case 0:
                            // Burst forward
                            if (Main.rand.NextBool(4))
                            {
                                npc.velocity.Y = -9f;
                                npc.velocity.X = 4f * npc.direction;
                                npc.TargetClosest(true);

                                if ((float)npc.direction * npc.velocity.X > 4)
                                {
                                    npc.velocity.X = (float)npc.direction * 3;  //  3 was 4 - this caps the top speed
                                }
                                npc.netUpdate = true;
                            }
                            break;

                        case 1:
                            // Burst forward
                            if (Main.rand.NextBool(6))
                            {
                                npc.velocity.Y = -6f;
                                npc.velocity.X *= 4f; // burst forward
                                npc.TargetClosest(true);

                                npc.velocity.X += (float)npc.direction * 5f;  //  accellerate fwd; can happen midair
                                if ((float)npc.direction * npc.velocity.X > 5)
                                {
                                    npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed
                                }

                                // Chance to jump after dash
                                if (Main.rand.NextBool(6))
                                {
                                    npc.TargetClosest(true);
                                    npc.spriteDirection = npc.direction;
                                    npc.velocity.Y = -6f;
                                }

                                npc.netUpdate = true;
                            }
                            break;

                        case 2:
                            // Teleport
                            if (npc.Distance(Main.player[npc.target].Center) > 400 && Main.rand.NextBool(3))
                            {
                                TeleportImmediately(npc, 15, false);
                            }
                            break;

                        case 3:
                            // Dash backwards - Poison
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 290f;
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 290f;
                            }
                            break;
                        case 4:
                            // Chance to big jump backwards - Spear
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -9f;
                                npc.velocity.X = -9f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 5:
                            // Small dash backwards - Bomb
                            if (Main.rand.NextBool(2))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -6f;
                                npc.velocity.X = 6f * npc.direction;
                                npc.ai[1] = 860f;
                                npc.netUpdate = true;
                            }
                            // Alt dash backwards - Bomb
                            else
                            {
                                npc.ai[1] = 850f;
                                npc.TargetClosest(true);
                                npc.velocity.Y = -4f;
                                npc.velocity.X = -9f * npc.direction;
                            }
                            break;
                        case 6:
                            // Big dash backwards - Bomb
                            if (Main.rand.NextBool(4))
                            {
                                npc.TargetClosest(true);
                                npc.ai[1] = 880f;
                                npc.velocity.Y = -8f;
                                npc.velocity.X = -11f * npc.direction;
                                npc.netUpdate = true;
                            }
                            break;
                        case 7:
                            // Teleport
                            if (Main.rand.NextBool(4))
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.netUpdate = true;
                            }
                            else if (Main.rand.NextBool(4))
                            // Poision Teleport
                            {
                                TeleportImmediately(npc, 20, true);
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = -5f * npc.direction;
                                npc.ai[1] = 250f;
                            }
                            break;
                        case 8:
                            // Small dash backwards - Spear
                            if (Main.rand.NextBool(3))
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -3f;
                                npc.velocity.X = -7f * npc.direction;
                                npc.ai[1] = 140f;
                                npc.netUpdate = true;
                            }
                            else
                            // Jump high, slightly forward
                            {
                                npc.TargetClosest(true);
                                npc.velocity.Y = -10f;
                                npc.velocity.X = 3f * npc.direction;
                                npc.ai[1] = 130f;
                                npc.netUpdate = true;
                            }
                            break;
                        case 9:
                            // Attack interrupt; for Great Red Knight it cycles to DD2 attack at 1/2 health
                            npc.ai[1] = 700f;
                            break;
                    }
                    npc.netUpdate = true;
                }
            }
        }
        #endregion
        #endregion
    }
}
