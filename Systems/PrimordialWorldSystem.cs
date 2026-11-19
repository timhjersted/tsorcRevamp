using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Bosses.Primordial;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDrake;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// The Primordial world: the Ember of the First Dawn's permanent transformation, its 40-second cutscene, the tile, wall
    /// and liquid conversion, the arrival at Prime Gwyn's arena, and the world rules that hold afterwards (spawns, boss
    /// health and damage, bonfire travel, arena debuffs, respawn rejoin).
    ///
    /// Multiplayer: the server decides everything. Scene progress travels in world data (NetSend), so clients and late
    /// joiners run the same deterministic per-cell rule on the tiles they hold, at the same scene tick. No per-tile packets.
    /// </summary>
    public class PrimordialWorldSystem : ModSystem
    {
        public enum ConversionState : byte
        {
            None,
            Running,
            Done
        }

        /// <summary>Where the scene happens. Adventure uses the authored shots and the Hellkite arena; Sandbox (also used
        /// on Remix maps) shows five places around the Ember's user and summons Prime ahead of them.</summary>
        public enum SiteMode : byte
        {
            Adventure,
            Sandbox
        }

        private enum TileRule : byte
        {
            Keep,
            Replace,
            Remove,
            AshPlant,
            Bonfire
        }

        // ── Scene timeline, in ticks from the confirmed Ember use ───────────────────────────────────────────────────────
        public const int SceneTicks = 2670;
        private const int ShotLength = 420;
        private const int ShotFadeTicks = 30;
        private static readonly int[] ShotStarts = { 60, 540, 1020, 1500, 1980 };
        private const int ConversionEndTick = 2340;
        private const int SkipTargetTick = 2400;
        private const int ArrivalTick = 2460;
        private const int FadeInStartTick = 2580;
        private const int SectionLeadTicks = 100;
        private const int FocusLeadTicks = 30;

        // Reveal: each shot's held-back rectangle spreads out from its coordinate between these two points of the shot.
        private const int RevealStartOffset = 40;

        // "Everything in the rectangle". Finite on purpose: the reveal turns radius into int tile bounds.
        private const float FullRevealRadius = 100000f;
        private const int RevealEndOffset = ShotLength - 40;

        // Shot 1 pan (legacy tiles): island at 352,850, glide east to the open sea, the lava front 45 tiles ahead.
        private const int PanStartOffset = 90;
        private const int PanTicks = 270;
        private const float PanFrontLeadTiles = 45f;
        private static readonly Vector2 PanTargetLegacy = new Vector2(620, 860);
        private const float PanFinalFrontX = 720f;

        // Authored shots on the adventure map, legacy (2000-space) tiles; ExpandedWorldTransform maps them.
        private static readonly Vector2[] AdventureShotsLegacy =
        {
            new Vector2(352, 850),   // Island on the Nora Sea
            new Vector2(6200, 873),  // Jungle Ruins
            new Vector2(4182, 709),  // Ashen Peak Village
            new Vector2(2166, 730),  // Desert
            new Vector2(2677, 1204)  // Old One's Tree
        };

        // Shot 1's held-back rectangle covers the whole pan: X 252-719, legacy rows 790-919.
        private static readonly Rectangle PanRectLegacy = new Rectangle(252, 790, 468, 130);
        private const int HeldRectWidth = 200;
        private const int HeldRectHeight = 120;

        // Sandbox shots: tile offsets from the Ember user, so all five show the world around them changing.
        private static readonly Point[] SandboxShotOffsets =
        {
            new Point(0, 0),
            new Point(-220, 0),
            new Point(220, 0),
            new Point(-440, 0),
            new Point(440, 0)
        };

        // Arrival (legacy tiles): 100 tiles east of the ring edge, standing on the ground near legacy row 728.
        private static readonly Vector2 LandingLegacy = new Vector2(4500, 728);
        private const int LandingGroundSearchUp = 40;
        private const int LandingGroundSearchDown = 80;
        private const float SandboxPrimeDistance = 480f;
        public const int RejoinImmunityTicks = 120;

        // Spawns, rolled by the pool itself because pool weights are relative (a lone 0.1 entry would always win).
        private const float WildDrakeChance = 0.05f;
        private const float AncientDemonChance = 0.1f;

        // Older bosses: one Expert health total, and a hit multiplier chosen so an ordinary hit lands near Gwyn's weapon.
        public const int OlderBossExpertLife = 500000;
        private const float OlderBossTargetHit = 95f;
        private const float OlderBossDamageMultiplierMax = 5f;
        private const float OlderBossDamageMultiplierFallback = 2f;
        public const float OlderBossHitSearchRange = 4000f;
        public const int EaterOfWorldsSegmentEstimate = 60;

        // Prime's arena: every player this close to him is grounded, the way Gwyn's coward ring grounds his target.
        public const float PrimeArenaDebuffRange = 3000f;

        // A bonfire is lit when its tile frame sits one 74-pixel animation sheet lower (Tiles/BonfireCheckpoint.cs).
        private const int BonfireLitFrameOffset = 74;

        // Water Fiend Kraken's chamber keeps its water: the fight floods and drains it with water. The same bounds as the
        // Kraken's own flood check, X in tiles (unchanged by the expansion), Y in legacy tiles mapped at X 1820.
        private const int KrakenChamberLeft = 1560;
        private const int KrakenChamberRight = 2011;
        private const int KrakenChamberLegacyTop = 1660;
        private const int KrakenChamberLegacyBottom = 1744;
        private const int KrakenChamberMapX = 1820;

        private const string SceneId = "PrimordialDawn";
        private const int RevealDustPerTick = 10;

        // ── Saved and synced state ──────────────────────────────────────────────────────────────────────────────────────
        public static ConversionState Conversion { get; private set; } = ConversionState.None;

        /// <summary>Scene tick on this machine, or -1 when no scene is playing.</summary>
        public static int SceneTick { get; private set; } = -1;

        public static SiteMode Site { get; private set; } = SiteMode.Adventure;

        /// <summary>Players have been brought to the arena (or the conversion finished after a reload). Prime's ring waits for it.</summary>
        public static bool ArrivalDone { get; private set; }

        /// <summary>World position of the landing point, feet on the ground. Synced so a client's Ember can return there.</summary>
        public static Vector2 LandingWorld { get; private set; }

        private static Point sandboxCenterTile;
        private static int sceneLeadPlayer = -1;
        private static bool resumePending;

        public static bool SceneRunning => SceneTick >= 0;

        // ── Per-machine conversion state ────────────────────────────────────────────────────────────────────────────────
        private static TileRule[] tileRules;
        private static ushort[] tileTargets;
        private static ushort[] wallTargets;

        private static Vector2[] shotCentersTile = new Vector2[0];
        private static Rectangle[] heldRects = new Rectangle[0];
        private static int sweptColumns;
        private static float[] revealedRadius = new float[0];
        private static readonly List<Point> changedCells = new List<Point>();
        private static Rectangle krakenChamber;

        /// <summary>Server: non-saving boss rings won in the age, by event name. They stay won instead of re-arming on respawn.</summary>
        private static readonly HashSet<string> clearedRings = new HashSet<string>();

        public override void Load()
        {
            On_Main.UpdateTime_SpawnTownNPCs += BlockTownNpcSpawns;
        }

        public override void Unload()
        {
            On_Main.UpdateTime_SpawnTownNPCs -= BlockTownNpcSpawns;
            tileRules = null;
            tileTargets = null;
            wallTargets = null;
        }

        // Town NPCs and their shops leave with the old world and never move back in.
        private static void BlockTownNpcSpawns(On_Main.orig_UpdateTime_SpawnTownNPCs orig)
        {
            if (tsorcRevampWorld.PrimordialAge)
            {
                return;
            }

            orig();
        }

        #region Conversion rule tables

        /// <summary>
        /// Builds the per-type lookup the conversion uses. Names come from TileID/WallID so the rule reads the way the
        /// design states it; a name missing from this Terraria version is skipped. Vanilla solid blocks not listed become
        /// Obsidian Brick. Modded tiles are left alone.
        /// </summary>
        public override void PostSetupContent()
        {
            Dictionary<string, int> tileIds = ReadIdConstants(typeof(TileID));
            Dictionary<string, int> wallIds = ReadIdConstants(typeof(WallID));

            tileRules = new TileRule[TileLoader.TileCount];
            tileTargets = new ushort[TileLoader.TileCount];

            // Default: every vanilla solid, full, non-object block becomes Obsidian Brick.
            for (int type = 0; type < TileID.Count; type++)
            {
                bool plainSolidBlock = Main.tileSolid[type] && !Main.tileSolidTop[type] && !Main.tileFrameImportant[type];
                if (plainSolidBlock)
                {
                    tileRules[type] = TileRule.Replace;
                    tileTargets[type] = TileID.ObsidianBrick;
                }
            }

            // Kept: every brick and slab, plus the named blocks below.
            foreach (KeyValuePair<string, int> pair in tileIds)
            {
                bool brickOrSlab = pair.Key.Contains("Brick") || pair.Key.Contains("Slab");
                if (brickOrSlab && pair.Value < tileRules.Length)
                {
                    tileRules[pair.Value] = TileRule.Keep;
                }
            }

            string[] keptTiles =
            {
                "Stone", "Ash", "AshGrass", "Cloud", "RainCloud", "SnowCloud", "BoneBlock", "Spikes", "WoodenSpikes",
                "Marble", "Granite", "Glass", "ActiveStoneBlock", "InactiveStoneBlock",
                "LivingFire", "LivingCursedFire", "LivingDemonFire", "LivingFrostFire", "LivingIchor", "LivingUltrabrightFire",
                "Copper", "Iron", "Silver", "Gold", "Tin", "Lead", "Tungsten", "Platinum", "Demonite", "Crimtane", "Meteorite",
                "Cobalt", "Palladium", "Mythril", "Orichalcum", "Adamantite", "Titanium", "Chlorophyte", "LunarOre",
                "Sapphire", "Ruby", "Emerald", "Topaz", "Amethyst", "Diamond",
                "AshWood", "LavaMoss", "LavaMossBlock", "TreeAsh", "AshPlants", "AshVines"
            };
            SetTileRule(tileIds, keptTiles, TileRule.Keep, 0);

            string[] toAshGrass =
            {
                "Grass", "CorruptGrass", "JungleGrass", "MushroomGrass", "HallowedGrass", "CrimsonGrass",
                "GolfGrass", "GolfGrassHallowed", "CorruptJungleGrass", "CrimsonJungleGrass"
            };
            SetTileRule(tileIds, toAshGrass, TileRule.Replace, TileID.AshGrass);

            string[] toAsh =
            {
                "Dirt", "Mud", "Silt", "Slush", "ClayBlock",
                "IceBlock", "CorruptIce", "HallowedIce", "FleshIce", "BreakableIce",
                "HardenedSand", "CorruptHardenedSand", "CrimsonHardenedSand", "HallowHardenedSand"
            };
            SetTileRule(tileIds, toAsh, TileRule.Replace, TileID.Ash);

            SetTileRule(tileIds, new[] { "Ebonstone", "Crimstone", "Pearlstone" }, TileRule.Replace, TileID.Stone);

            string[] toLavaMoss =
            {
                "GreenMoss", "BrownMoss", "RedMoss", "BlueMoss", "PurpleMoss", "KryptonMoss", "XenonMoss", "ArgonMoss",
                "VioletMoss", "RainbowMoss"
            };
            SetTileRule(tileIds, toLavaMoss, TileRule.Replace, TileID.LavaMoss);

            string[] toLavaMossBlock = { "ArgonMossBlock", "KryptonMossBlock", "XenonMossBlock", "VioletMossBlock", "RainbowMossBlock" };
            SetTileRule(tileIds, toLavaMossBlock, TileRule.Replace, TileID.LavaMossBlock);

            string[] toAshWood =
            {
                "WoodBlock", "Ebonwood", "RichMahogany", "Pearlwood", "Shadewood", "BorealWood", "PalmWood",
                "LivingWood", "LivingMahogany", "DynastyWood", "SpookyWood"
            };
            SetTileRule(tileIds, toAshWood, TileRule.Replace, TileID.AshWood);

            string[] toObsidianBrick =
            {
                "Hellstone", "Sand", "Ebonsand", "Crimsand", "Pearlsand", "Sandstone", "CorruptSandstone", "CrimsonSandstone",
                "HallowSandstone", "DesertFossil", "SnowBlock", "LeafBlock", "LivingMahoganyLeaves"
            };
            SetTileRule(tileIds, toObsidianBrick, TileRule.Replace, TileID.ObsidianBrick);

            SetTileRule(tileIds, new[] { "Trees" }, TileRule.Replace, TileID.TreeAsh);

            string[] toAshPlants =
            {
                "Plants", "Plants2", "JunglePlants", "JunglePlants2", "CorruptPlants", "CrimsonPlants",
                "HallowedPlants", "HallowedPlants2", "MushroomPlants"
            };
            SetTileRule(tileIds, toAshPlants, TileRule.AshPlant, TileID.AshPlants);

            string[] toAshVines = { "Vines", "JungleVines", "HallowedVines", "CrimsonVines", "VineFlowers", "MushroomVines", "CorruptVines" };
            SetTileRule(tileIds, toAshVines, TileRule.Replace, TileID.AshVines);

            // Removed with no drops: ropes and chains, chimney smoke, and growth that cannot stand on ash or obsidian.
            string[] removed =
            {
                "Rope", "SilkRope", "VineRope", "WebRope", "Chain", "ChimneySmoke",
                "Cactus", "PalmTree", "MushroomTrees", "Saplings", "ImmatureHerbs", "MatureHerbs", "BloomingHerbs",
                "DyePlants", "PlantDetritus", "Sunflower", "Pumpkins", "Bamboo", "LilyPad", "Cattail", "SeaOats",
                "OasisPlants", "Coral"
            };
            SetTileRule(tileIds, removed, TileRule.Remove, 0);

            int bonfireType = ModContent.TileType<Tiles.BonfireCheckpoint>();
            tileRules[bonfireType] = TileRule.Bonfire;

            BuildWallTargets(wallIds);
        }

        private static Dictionary<string, int> ReadIdConstants(Type idClass)
        {
            Dictionary<string, int> ids = new Dictionary<string, int>();
            foreach (FieldInfo field in idClass.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(ushort))
                {
                    ids[field.Name] = (ushort)field.GetRawConstantValue();
                }
            }

            return ids;
        }

        private static void SetTileRule(Dictionary<string, int> tileIds, string[] names, TileRule rule, ushort target)
        {
            foreach (string name in names)
            {
                if (tileIds.TryGetValue(name, out int type) && type < tileRules.Length)
                {
                    tileRules[type] = rule;
                    tileTargets[type] = target;
                }
            }
        }

        // Wall targets by name. 0 means "keep". Natural earth walls cycle through the four underworld rock walls in name
        // order, so neighbouring variants stay varied instead of becoming one flat wall.
        private static void BuildWallTargets(Dictionary<string, int> wallIds)
        {
            wallTargets = new ushort[WallLoader.WallCount];
            ushort[] underworldRockWalls = { WallID.LavaUnsafe1, WallID.LavaUnsafe2, WallID.LavaUnsafe3, WallID.LavaUnsafe4 };
            int rockWallIndex = 0;

            List<string> sortedNames = new List<string>(wallIds.Keys);
            sortedNames.Sort(StringComparer.Ordinal);

            foreach (string name in sortedNames)
            {
                int wall = wallIds[name];
                if (wall <= 0 || wall >= wallTargets.Length)
                {
                    continue;
                }

                bool isFall = wall == WallID.Waterfall || wall == WallID.Honeyfall || wall == WallID.SandFall || wall == WallID.SnowFall;
                bool isPlantWall = name.StartsWith("Grass") || name.StartsWith("Flower") || name.StartsWith("CorruptGrass")
                    || name.StartsWith("HallowedGrass") || name.StartsWith("CrimsonGrass") || name.StartsWith("LivingLeaf")
                    || name.StartsWith("Mushroom");
                bool isWood = name.Contains("Wood") && !name.Contains("AshWood");
                bool isFence = name.Contains("Fence");
                bool isBrick = name.Contains("Brick");
                bool isEarthWall = !isBrick && (name.StartsWith("Dirt") || name.StartsWith("Mud") || name.StartsWith("Jungle")
                    || name.StartsWith("Cave"));
                bool isIceWall = name.StartsWith("Ice") && !isBrick && !name.StartsWith("IceFloe");
                bool isSnowWall = name.StartsWith("SnowWall");

                if (isFall)
                {
                    wallTargets[wall] = WallID.Lavafall;
                }
                else if (isPlantWall)
                {
                    wallTargets[wall] = WallID.Stone;
                }
                else if (isWood && isFence)
                {
                    wallTargets[wall] = WallID.AshWoodFence;
                }
                else if (isWood)
                {
                    wallTargets[wall] = WallID.AshWood;
                }
                else if (isEarthWall)
                {
                    wallTargets[wall] = underworldRockWalls[rockWallIndex % underworldRockWalls.Length];
                    rockWallIndex++;
                }
                else if (isIceWall)
                {
                    wallTargets[wall] = WallID.Cave8Unsafe;
                }
                else if (isSnowWall)
                {
                    wallTargets[wall] = WallID.RocksUnsafe1;
                }
            }
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Applies the Primordial rule to one cell. Idempotent: a converted cell maps to itself, so the sweep, the shot
        /// reveals and the final pass can overlap safely. Keeps slopes, half blocks, wires, actuators and the actuated
        /// (inactive) state; clears block paint. Returns true if anything changed, so the caller can reframe it.
        /// </summary>
        private static bool ConvertCell(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            bool changed = false;

            if (tile.HasTile)
            {
                ushort type = tile.TileType;
                TileRule rule = TileRule.Keep;
                if (type < tileRules.Length)
                {
                    rule = tileRules[type];
                }

                if (rule == TileRule.Replace && tileTargets[type] != type)
                {
                    tile.TileType = tileTargets[type];
                    tile.TileColor = PaintID.None;
                    changed = true;
                }
                else if (rule == TileRule.AshPlant)
                {
                    // Ash plants have 11 one-tile styles (frameX 0-180). Picking by position keeps every machine identical.
                    int style = (x * 7 + y * 13) % 11;
                    tile.TileType = TileID.AshPlants;
                    tile.TileFrameX = (short)(style * 18);
                    tile.TileFrameY = 0;
                    tile.TileColor = PaintID.None;
                    changed = true;
                }
                else if (rule == TileRule.Remove)
                {
                    tile.HasTile = false;
                    tile.TileFrameX = 0;
                    tile.TileFrameY = 0;
                    changed = true;
                }
                else if (rule == TileRule.Bonfire && tile.TileFrameY >= BonfireLitFrameOffset)
                {
                    // Every bonfire goes out; relighting uses the normal right-click flow.
                    tile.TileFrameY -= BonfireLitFrameOffset;
                    changed = true;
                }
            }

            ushort wall = tile.WallType;
            if (wall > 0 && wall < wallTargets.Length && wallTargets[wall] != 0 && wallTargets[wall] != wall)
            {
                tile.WallType = wallTargets[wall];
                tile.WallColor = PaintID.None;
                changed = true;
            }

            // Water and honey become lava at the same level. Shimmer is left alone, and so is the Kraken's chamber.
            bool krakenWater = krakenChamber.Contains(x, y);
            if (tile.LiquidAmount > 0 && !krakenWater && (tile.LiquidType == LiquidID.Water || tile.LiquidType == LiquidID.Honey))
            {
                tile.LiquidType = LiquidID.Lava;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Reframes converted cells without breaking anything: noBreak keeps objects and unsupported growth from dropping
        /// items, and noLiquidCheck keeps new lava from waking the liquid simulation next to held-back water. With
        /// withNeighbours, the four neighbours are reframed too so block edges merge (used on screen, where it shows).
        /// </summary>
        private static void ReframeChangedCells(bool withNeighbours)
        {
            bool previousLiquidCheck = WorldGen.noLiquidCheck;
            WorldGen.noLiquidCheck = true;

            foreach (Point cell in changedCells)
            {
                WorldGen.TileFrame(cell.X, cell.Y, true, true);
                Framing.WallFrame(cell.X, cell.Y, true);

                if (!withNeighbours)
                {
                    continue;
                }

                WorldGen.TileFrame(cell.X - 1, cell.Y, false, true);
                WorldGen.TileFrame(cell.X + 1, cell.Y, false, true);
                WorldGen.TileFrame(cell.X, cell.Y - 1, false, true);
                WorldGen.TileFrame(cell.X, cell.Y + 1, false, true);
            }

            WorldGen.noLiquidCheck = previousLiquidCheck;
            changedCells.Clear();
        }

        private static bool IsHeldBack(int x, int y)
        {
            foreach (Rectangle rect in heldRects)
            {
                if (rect.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Converts whole columns from sweptColumns up to (not including) endColumn. Off-screen sweep work, so only
        /// the changed cells are reframed. includeHeld also converts the held-back shot rectangles (final or skip pass).</summary>
        private static void SweepColumns(int endColumn, bool includeHeld)
        {
            int lastColumn = Math.Min(endColumn, Main.maxTilesX - 1);
            int lastRow = Main.maxTilesY - 1;

            for (int x = Math.Max(sweptColumns, 1); x < lastColumn; x++)
            {
                for (int y = 1; y < lastRow; y++)
                {
                    if (!includeHeld && IsHeldBack(x, y))
                    {
                        continue;
                    }

                    if (ConvertCell(x, y))
                    {
                        changedCells.Add(new Point(x, y));
                    }
                }

                ReframeChangedCells(false);
            }

            sweptColumns = Math.Max(sweptColumns, endColumn);
        }

        /// <summary>Converts the ring of a held-back rectangle between the last radius and this one (tiles, from the shot
        /// coordinate). On screen, so neighbours are reframed as well.</summary>
        private static void RevealShotRing(int shot, float radius)
        {
            float previousRadius = revealedRadius[shot];
            if (radius <= previousRadius)
            {
                return;
            }

            Rectangle rect = heldRects[shot];
            Vector2 center = shotCentersTile[shot];
            int left = Math.Max(rect.Left, (int)(center.X - radius) - 1);
            int right = Math.Min(rect.Right, (int)(center.X + radius) + 2);
            int top = Math.Max(rect.Top, (int)(center.Y - radius) - 1);
            int bottom = Math.Min(rect.Bottom, (int)(center.Y + radius) + 2);
            float previousSquared = previousRadius * previousRadius;
            float radiusSquared = radius * radius;

            for (int x = Math.Max(left, 1); x < Math.Min(right, Main.maxTilesX - 1); x++)
            {
                for (int y = Math.Max(top, 1); y < Math.Min(bottom, Main.maxTilesY - 1); y++)
                {
                    float distanceSquared = Vector2.DistanceSquared(center, new Vector2(x, y));
                    bool inBand = distanceSquared > previousSquared && distanceSquared <= radiusSquared;
                    if (inBand && ConvertCell(x, y))
                    {
                        changedCells.Add(new Point(x, y));
                    }
                }
            }

            ReframeChangedCells(true);
            revealedRadius[shot] = radius;
        }

        /// <summary>Finishes the whole conversion at once on this machine: skip, a reload mid-scene, or the end of the scene.
        /// Every held rectangle is included, so nothing can be left half-changed.</summary>
        private static void FinishConversionNow()
        {
            sweptColumns = 0;
            SweepColumns(Main.maxTilesX, true);

            for (int shot = 0; shot < revealedRadius.Length; shot++)
            {
                revealedRadius[shot] = FullRevealRadius;
            }

            Conversion = ConversionState.Done;
        }

        #endregion

        #region Shots

        /// <summary>Rebuilds this machine's shot coordinates and held-back rectangles from synced data.</summary>
        private static void PrepareShots()
        {
            shotCentersTile = new Vector2[ShotStarts.Length];
            heldRects = new Rectangle[ShotStarts.Length];
            revealedRadius = new float[ShotStarts.Length];

            for (int shot = 0; shot < ShotStarts.Length; shot++)
            {
                Vector2 center;
                if (Site == SiteMode.Adventure)
                {
                    center = ExpandedWorldTransform.MapTile(AdventureShotsLegacy[shot]);
                }
                else
                {
                    Point offset = SandboxShotOffsets[shot];
                    float clampedX = MathHelper.Clamp(sandboxCenterTile.X + offset.X, 120, Main.maxTilesX - 120);
                    center = new Vector2(clampedX, sandboxCenterTile.Y + offset.Y);
                }

                shotCentersTile[shot] = center;
                heldRects[shot] = new Rectangle((int)center.X - HeldRectWidth / 2, (int)center.Y - HeldRectHeight / 2, HeldRectWidth, HeldRectHeight);
            }

            if (Site == SiteMode.Adventure)
            {
                // The pan rectangle shifts with the map transform like every other legacy coordinate.
                Vector2 panTopLeft = ExpandedWorldTransform.MapTile(new Vector2(PanRectLegacy.Left, PanRectLegacy.Top));
                heldRects[0] = new Rectangle((int)panTopLeft.X, (int)panTopLeft.Y, PanRectLegacy.Width, PanRectLegacy.Height);
            }
        }

        /// <summary>Front radius of a shot's reveal at this shot-relative tick, in tiles. Shot 1 on the adventure map follows
        /// the camera pan (same smoothstep as CutsceneSystem), staying 45 tiles ahead of the camera centre.</summary>
        private static float RevealRadius(int shot, int shotTick)
        {
            Rectangle rect = heldRects[shot];
            Vector2 center = shotCentersTile[shot];
            float farX = Math.Max(Math.Abs(rect.Left - center.X), Math.Abs(rect.Right - center.X));
            float farY = Math.Max(Math.Abs(rect.Top - center.Y), Math.Abs(rect.Bottom - center.Y));
            float fullRadius = (float)Math.Sqrt(farX * farX + farY * farY) + 1f;

            bool panShot = shot == 0 && Site == SiteMode.Adventure;
            if (!panShot)
            {
                float progress = MathHelper.Clamp((shotTick - RevealStartOffset) / (float)(RevealEndOffset - RevealStartOffset), 0f, 1f);
                float eased = progress * progress * (3f - 2f * progress);
                return fullRadius * eased;
            }

            float panTargetX = ExpandedWorldTransform.MapTile(PanTargetLegacy).X;
            float panDistance = panTargetX - center.X;

            if (shotTick < PanStartOffset)
            {
                float openProgress = MathHelper.Clamp((shotTick - ShotFadeTicks) / (float)(PanStartOffset - ShotFadeTicks), 0f, 1f);
                return PanFrontLeadTiles * openProgress;
            }

            if (shotTick < PanStartOffset + PanTicks)
            {
                float panProgress = (shotTick - PanStartOffset) / (float)PanTicks;
                float eased = panProgress * panProgress * (3f - 2f * panProgress);
                return PanFrontLeadTiles + panDistance * eased;
            }

            // Hold on the sea while the front runs on to X 720 (the transform only moves rows, so X is unchanged), then
            // everything left in the rectangle once the shot fades.
            float finalFront = PanFinalFrontX - center.X;
            float holdProgress = MathHelper.Clamp((shotTick - PanStartOffset - PanTicks) / (float)(ShotLength - ShotFadeTicks - PanStartOffset - PanTicks), 0f, 1f);
            float heldFront = PanFrontLeadTiles + panDistance;
            float front = MathHelper.Lerp(heldFront, Math.Max(heldFront, finalFront), holdProgress);
            if (shotTick >= ShotLength - ShotFadeTicks)
            {
                front = fullRadius;
            }

            return front;
        }

        /// <summary>Black over the whole screen: in and out of each shot, and through the arrival.</summary>
        private static float FadeAlpha(int tick)
        {
            if (tick < ShotFadeTicks)
            {
                return tick / (float)ShotFadeTicks;
            }

            if (tick >= FadeInStartTick)
            {
                return 1f - MathHelper.Clamp((tick - FadeInStartTick) / (float)(SceneTicks - FadeInStartTick), 0f, 1f);
            }

            for (int shot = 0; shot < ShotStarts.Length; shot++)
            {
                int start = ShotStarts[shot];
                int end = start + ShotLength;
                if (tick < start || tick >= end)
                {
                    continue;
                }

                if (tick < start + ShotFadeTicks)
                {
                    return 1f - (tick - start) / (float)ShotFadeTicks;
                }

                if (tick >= end - ShotFadeTicks)
                {
                    return (tick - (end - ShotFadeTicks)) / (float)ShotFadeTicks;
                }

                return 0f;
            }

            return 1f;
        }

        #endregion

        #region Starting the transformation

        /// <summary>Server or single player: validates a confirmed Ember use and starts the scene. A rejected use explains why.</summary>
        public static void RequestTransformation(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !player.active || player.dead)
            {
                return;
            }

            // A world that only has the age flag (set by the Ember before the conversion existed) may still transform once.
            bool gwynSlain = tsorcRevampWorld.BossDefeated(ModContent.NPCType<Gwyn>());
            bool alreadyTransformed = Conversion != ConversionState.None;
            bool blocked = !gwynSlain || tsorcRevampWorld.BossAlive || alreadyTransformed || CutsceneSystem.IsActive;
            if (blocked)
            {
                return;
            }

            bool adventureSite = tsorcRevampWorld.OnlyAdventureMap && !tsorcRevampWorld.RemixMap;
            Site = SiteMode.Sandbox;
            if (adventureSite)
            {
                Site = SiteMode.Adventure;
            }

            sandboxCenterTile = player.Center.ToTileCoordinates();
            sceneLeadPlayer = player.whoAmI;
            LandingWorld = FindLandingWorld(player);
            ArrivalDone = false;

            // Order matters: the scene state must be set before EnterPrimordialAge sends world data.
            Conversion = ConversionState.Running;
            SceneTick = 0;
            StartSceneLocally();

            RemoveOrdinaryNpcs();
            ResetBossRings();

            CutsceneCast cast = new CutsceneCast { LeadPlayer = player.whoAmI, AllowSkip = true };
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    cast.Players.Add(i);
                }
            }

            Vector2 firstShotWorld = shotCentersTile[0] * 16f;
            CutsceneSystem.Start(SceneId, SceneTicks, CutsceneFocus.AtPoint(firstShotWorld), -1, cast);

            tsorcRevampWorld.EnterPrimordialAge();

            // EnterPrimordialAge sends nothing when the flag was already set, and clients start the scene from world data.
            SendWorldData();
        }

        /// <summary>Feet position of the arrival point. Adventure: the ground nearest legacy 4500,728. Sandbox: the user.</summary>
        private static Vector2 FindLandingWorld(Player player)
        {
            if (Site != SiteMode.Adventure)
            {
                return player.Bottom;
            }

            Vector2 legacyTile = ExpandedWorldTransform.MapTile(LandingLegacy);
            int column = (int)legacyTile.X;
            int startRow = (int)legacyTile.Y - LandingGroundSearchUp;
            int endRow = (int)legacyTile.Y + LandingGroundSearchDown;

            // First solid tile with three free rows above it: room for a standing player.
            for (int y = Math.Max(startRow, 4); y < Math.Min(endRow, Main.maxTilesY - 1); y++)
            {
                bool ground = WorldGen.SolidTile(column, y);
                bool headroom = !WorldGen.SolidTile(column, y - 1) && !WorldGen.SolidTile(column, y - 2) && !WorldGen.SolidTile(column, y - 3);
                if (ground && headroom)
                {
                    return new Vector2(column * 16f + 8f, y * 16f);
                }
            }

            return legacyTile * 16f;
        }

        /// <summary>Every machine, at scene start or on joining mid-scene: shots, rectangles and the bonfire list.</summary>
        private static void StartSceneLocally()
        {
            PrepareShots();
            sweptColumns = 0;
            tsorcRevampWorld.LitBonfireList?.Clear();

            // Adventure map only: elsewhere the chamber bounds mean nothing, and the Kraken's flood is off outside Adventure Mode.
            krakenChamber = Rectangle.Empty;
            if (Site == SiteMode.Adventure)
            {
                int chamberTop = ExpandedWorldTransform.MapTileY(KrakenChamberMapX, KrakenChamberLegacyTop);
                int chamberBottom = ExpandedWorldTransform.MapTileY(KrakenChamberMapX, KrakenChamberLegacyBottom);
                int chamberWidth = KrakenChamberRight - KrakenChamberLeft + 1;
                int chamberHeight = chamberBottom - chamberTop + 1;
                krakenChamber = new Rectangle(KrakenChamberLeft, chamberTop, chamberWidth, chamberHeight);
            }
        }

        /// <summary>Server: every non-boss NPC leaves with the old world, town NPCs and their shops included.</summary>
        private static void RemoveOrdinaryNpcs()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.boss)
                {
                    continue;
                }

                bool partOfBoss = npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss;
                if (partOfBoss)
                {
                    continue;
                }

                npc.active = false;
                npc.netSkip = -1;
                npc.life = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: i);
                }
            }
        }

        /// <summary>
        /// Server, once: every boss ring returns. Hardcoded boss events lose their saved completion and go back to the
        /// enabled list; completed dynamic events whose NPC list holds a boss are cleared and reloaded. The Hellkite village
        /// ring stays off, because Prime uses its site and wild Drakes fill its role.
        /// </summary>
        private static void ResetBossRings()
        {
            if (tsorcScriptedEvents.ScriptedEventDict == null || tsorcScriptedEvents.ScriptedEventValues == null)
            {
                return;
            }

            // Non-saving boss rings won before the age wait in DisabledEvents for a respawn. Send them back now, so they are
            // not mistaken for rings won in the age.
            clearedRings.Clear();
            List<ScriptedEvent> parkedBossRings = tsorcScriptedEvents.DisabledEvents.FindAll(parked => parked.bossEvent);
            foreach (ScriptedEvent parkedRing in parkedBossRings)
            {
                tsorcScriptedEvents.DisabledEvents.Remove(parkedRing);
                tsorcScriptedEvents.EnabledEvents.Add(parkedRing);
            }

            foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
            {
                bool isHellkiteVillage = pair.Key == tsorcScriptedEvents.ScriptedEventType.HellkiteDragonEvent;
                if (!pair.Value.bossEvent || isHellkiteVillage)
                {
                    continue;
                }

                tsorcScriptedEvents.ScriptedEventValues[pair.Key] = false;

                bool alreadyListed = tsorcScriptedEvents.EnabledEvents.Contains(pair.Value)
                    || tsorcScriptedEvents.RunningEvents.Contains(pair.Value)
                    || tsorcScriptedEvents.QueuedEvents.Contains(pair.Value);
                if (!alreadyListed)
                {
                    tsorcScriptedEvents.DisabledEvents.Remove(pair.Value);
                    tsorcScriptedEvents.EnabledEvents.Add(pair.Value);
                }
            }

            int clearedDynamic = 0;
            foreach (DynamicSpawnEvent dynamicEvent in tsorcScriptedEvents.DynamicEvents)
            {
                if (!tsorcRevampWorld.CompletedDynamicEvents.Contains(dynamicEvent.EventID))
                {
                    continue;
                }

                bool hasBoss = false;
                foreach (DynamicSpawnEntry entry in dynamicEvent.Npcs)
                {
                    bool knownType = ContentSamples.NpcsByNetId.TryGetValue(entry.NpcID, out NPC sample);
                    if (knownType && sample.boss)
                    {
                        hasBoss = true;
                    }
                }

                if (hasBoss)
                {
                    tsorcRevampWorld.CompletedDynamicEvents.Remove(dynamicEvent.EventID);
                    clearedDynamic++;
                }
            }

            if (clearedDynamic > 0)
            {
                tsorcScriptedEvents.LoadDynamicEvents();
            }
        }

        #endregion

        #region Scene script

        /// <summary>Server or single player: ordinary events and invasions do not happen in the Primordial world.</summary>
        public override void PostUpdateWorld()
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return;
            }

            bool eventRunning = Main.invasionType != 0 || Main.bloodMoon || Main.eclipse || Main.pumpkinMoon || Main.snowMoon;
            if (eventRunning)
            {
                Main.invasionType = 0;
                Main.invasionSize = 0;
                Main.bloodMoon = false;
                Main.eclipse = false;
                Main.pumpkinMoon = false;
                Main.snowMoon = false;
                SendWorldData();
            }

            if (Main.slimeRain)
            {
                Main.StopSlimeRain(false);
            }

            // A won non-saving ring is parked in DisabledEvents (the only thing that list holds) until a respawn re-arms it.
            // In the age it stays won: remember it, and EventConditionMet keeps it off.
            if (tsorcScriptedEvents.DisabledEvents != null && !SceneRunning)
            {
                foreach (ScriptedEvent parkedEvent in tsorcScriptedEvents.DisabledEvents)
                {
                    if (!parkedEvent.bossEvent || parkedEvent.save)
                    {
                        continue;
                    }

                    string ringId = RingId(parkedEvent);
                    if (ringId != null)
                    {
                        clearedRings.Add(ringId);
                    }
                }
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.gameMenu)
            {
                return;
            }

            // A save made mid-scene resumes here: the server or single player finishes the conversion before play goes on.
            if (resumePending && Main.netMode != NetmodeID.MultiplayerClient)
            {
                resumePending = false;
                StartSceneLocally();
                FinishConversionNow();
                CompleteArrivalBookkeeping();
                SceneTick = -1;
                SendWorldData();
            }

            if (SceneRunning)
            {
                AdvanceScene();
            }
        }

        /// <summary>One scene tick on this machine. The sweep and reveals run everywhere; camera, sections, teleports and
        /// skip handling run on the server (or in single player).</summary>
        private static void AdvanceScene()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            int tick = SceneTick;

            // Skip lands on the black after shot 5; the rest of the conversion finishes behind it.
            if (authority && CutsceneSystem.SkipRequested && tick < SkipTargetTick)
            {
                CutsceneSystem.SetSkipAllowed(false);
                JumpTo(SkipTargetTick);
                CutsceneSystem.FinishIn(SceneTicks - SkipTargetTick);
                SendWorldData();
                tick = SceneTick;
            }

            // Background sweep: the whole width between tick 0 and ConversionEndTick, held rectangles excluded.
            if (tick < ConversionEndTick)
            {
                int columnTarget = (int)((long)Main.maxTilesX * (tick + 1) / ConversionEndTick);
                SweepColumns(columnTarget, false);
            }

            for (int shot = 0; shot < ShotStarts.Length; shot++)
            {
                int shotTick = tick - ShotStarts[shot];
                if (shotTick >= 0 && shotTick < ShotLength)
                {
                    RevealShotRing(shot, RevealRadius(shot, shotTick));
                    SpawnRevealDust(shot, shotTick);
                }
                else if (shotTick == ShotLength)
                {
                    RevealShotRing(shot, FullRevealRadius);
                }
            }

            if (authority)
            {
                RunAuthorityCues(tick);
            }

            if (tick == ArrivalTick)
            {
                ApplyLocalArrival();
            }

            SceneTick++;
            if (SceneTick >= SceneTicks)
            {
                EndScene(authority);
            }
        }

        /// <summary>Server cues at fixed ticks: map sections ahead of each shot, camera cuts, the pan, the arrival.</summary>
        private static void RunAuthorityCues(int tick)
        {
            for (int shot = 0; shot < ShotStarts.Length; shot++)
            {
                int start = ShotStarts[shot];
                if (tick == Math.Max(0, start - SectionLeadTicks))
                {
                    SendShotSections(shot);
                }

                if (tick == Math.Max(0, start - FocusLeadTicks))
                {
                    CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(shotCentersTile[shot] * 16f), 0);
                }

                if (shot == 0 && Site == SiteMode.Adventure && tick == start + PanStartOffset)
                {
                    Vector2 panTarget = ExpandedWorldTransform.MapTile(PanTargetLegacy) * 16f;
                    CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(panTarget), PanTicks);
                }

                if (tick == start + 2)
                {
                    PlayShotSound(shot);
                }
            }

            if (tick == ArrivalTick)
            {
                TeleportPlayersToLanding();
                CutsceneSystem.SetFocus(CutsceneFocus.AtPoint(LandingWorld - new Vector2(0f, 24f)), 0);
            }
        }

        // Server sends each client the map sections a shot needs, then frames them, so a distant shot is never empty.
        private static void SendShotSections(int shot)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            Rectangle rect = heldRects[shot];
            int firstSectionX = Netplay.GetSectionX(Math.Max(rect.Left - 70, 0));
            int lastSectionX = Netplay.GetSectionX(Math.Min(rect.Right + 70, Main.maxTilesX - 1));
            int firstSectionY = Netplay.GetSectionY(Math.Max(rect.Top - 40, 0));
            int lastSectionY = Netplay.GetSectionY(Math.Min(rect.Bottom + 40, Main.maxTilesY - 1));

            for (int client = 0; client < Main.maxPlayers; client++)
            {
                if (!Main.player[client].active || Netplay.Clients[client] == null || !Netplay.Clients[client].IsActive)
                {
                    continue;
                }

                for (int sectionX = firstSectionX; sectionX <= lastSectionX; sectionX++)
                {
                    for (int sectionY = firstSectionY; sectionY <= lastSectionY; sectionY++)
                    {
                        NetMessage.SendSection(client, sectionX, sectionY);
                    }
                }

                NetMessage.SendData(MessageID.TileFrameSection, client, -1, null, firstSectionX, firstSectionY, lastSectionX, lastSectionY);
            }
        }

        private static void PlayShotSound(int shot)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath with { Volume = 0.7f, Pitch = -0.6f });
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = -0.8f + shot * 0.05f });
        }

        // Embers along the advancing front, on screen only. Cosmetic, so each client rolls its own.
        private static void SpawnRevealDust(int shot, int shotTick)
        {
            if (Main.dedServ || shotTick < ShotFadeTicks || shotTick >= ShotLength - ShotFadeTicks)
            {
                return;
            }

            float radiusTiles = revealedRadius[shot];
            if (radiusTiles <= 0f || radiusTiles >= FullRevealRadius)
            {
                return;
            }

            Vector2 centerWorld = shotCentersTile[shot] * 16f;
            Rectangle screen = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

            for (int i = 0; i < RevealDustPerTick; i++)
            {
                Vector2 frontPoint = centerWorld + Main.rand.NextVector2Unit() * radiusTiles * 16f;
                if (!screen.Contains(frontPoint.ToPoint()))
                {
                    continue;
                }

                Dust ember = Dust.NewDustPerfect(frontPoint, DustID.Torch, new Vector2(0f, -Main.rand.NextFloat(1f, 3f)), 0, default, Main.rand.NextFloat(1.4f, 2.2f));
                ember.noGravity = true;
            }
        }

        /// <summary>Server: brings every living player to the landing point behind the black screen.</summary>
        private static void TeleportPlayersToLanding()
        {
            if (Site != SiteMode.Adventure)
            {
                return;
            }

            Vector2 standPosition = LandingWorld - new Vector2(Player.defaultWidth / 2f, Player.defaultHeight);
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                player.Teleport(standPosition, TeleportationStyleID.DebugTeleport);
                player.velocity = Vector2.Zero;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, i, standPosition.X, standPosition.Y, TeleportationStyleID.DebugTeleport);
                }
            }

            CompleteArrivalBookkeeping();
            SendWorldData();
        }

        /// <summary>Server: the world spawn moves to the landing point, so anyone without a relit bonfire respawns there.</summary>
        private static void CompleteArrivalBookkeeping()
        {
            ArrivalDone = true;

            if (Site == SiteMode.Adventure && LandingWorld != Vector2.Zero)
            {
                Main.spawnTileX = (int)(LandingWorld.X / 16f);
                Main.spawnTileY = (int)(LandingWorld.Y / 16f) - 1;
            }
        }

        // Every machine at the arrival tick: the local player forgets a bed or bonfire spawn from the old world.
        private static void ApplyLocalArrival()
        {
            if (Main.dedServ || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            {
                return;
            }

            Main.LocalPlayer.RemoveSpawn();
        }

        private static void EndScene(bool authority)
        {
            // The sweep and the five reveals already covered the world; only a machine that is somehow behind (a late
            // joiner whose sweep started mid-scene) needs the remaining columns.
            if (sweptColumns < Main.maxTilesX)
            {
                SweepColumns(Main.maxTilesX, true);
            }

            Conversion = ConversionState.Done;
            SceneTick = -1;

            if (!authority)
            {
                return;
            }

            if (!ArrivalDone)
            {
                CompleteArrivalBookkeeping();
            }

            if (Site == SiteMode.Sandbox)
            {
                SummonPrimeNearLead();
            }

            SendWorldData();
        }

        // Sandbox and Remix worlds have no arena: Prime arrives a short way ahead of whoever used the Ember.
        private static void SummonPrimeNearLead()
        {
            if (sceneLeadPlayer < 0 || sceneLeadPlayer >= Main.maxPlayers || !Main.player[sceneLeadPlayer].active || tsorcRevampWorld.BossAlive)
            {
                return;
            }

            Player lead = Main.player[sceneLeadPlayer];
            Vector2 summonBottom = new Vector2(lead.Center.X + lead.direction * SandboxPrimeDistance, lead.Bottom.Y);
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.Summon"), 255, 190, 90);
            NPC.NewNPC(new EntitySource_BossSpawn(lead), (int)summonBottom.X, (int)summonBottom.Y, ModContent.NPCType<PrimeGwyn>(), 0, 0f, 0f, 0f, 0f, lead.whoAmI);
        }

        /// <summary>Moves this machine's scene to a later tick, finishing the conversion first when jumping past the sweep.</summary>
        private static void JumpTo(int tick)
        {
            if (tick >= ConversionEndTick && SceneTick < ConversionEndTick)
            {
                FinishConversionNow();
            }

            SceneTick = tick;
        }

        private static void SendWorldData()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        #endregion

        #region Fade overlay

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!SceneRunning)
            {
                return;
            }

            layers.Insert(0, new LegacyGameInterfaceLayer("tsorcRevamp: Primordial Dawn Fade", delegate
            {
                float alpha = FadeAlpha(SceneTick);
                if (alpha <= 0f)
                {
                    return true;
                }

                // MagicPixel is 1x1000: the 1x1 source rect keeps this a plain screen-sized fill.
                Rectangle screen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, screen, new Rectangle(0, 0, 1, 1), Color.Black * alpha);
                return true;
            }, InterfaceScaleType.None));
        }

        #endregion

        #region Save, load and sync

        public override void OnWorldLoad()
        {
            ResetState();
        }

        public override void OnWorldUnload()
        {
            ResetState();
        }

        private static void ResetState()
        {
            Conversion = ConversionState.None;
            SceneTick = -1;
            Site = SiteMode.Adventure;
            ArrivalDone = false;
            LandingWorld = Vector2.Zero;
            sandboxCenterTile = Point.Zero;
            sceneLeadPlayer = -1;
            resumePending = false;
            sweptColumns = 0;
            changedCells.Clear();
            krakenChamber = Rectangle.Empty;
            clearedRings.Clear();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["PrimordialClearedRings"] = new List<string>(clearedRings);
            tag["PrimordialConversion"] = (byte)Conversion;
            tag["PrimordialSite"] = (byte)Site;
            tag["PrimordialArrivalDone"] = ArrivalDone;
            tag["PrimordialLanding"] = LandingWorld;
            tag["PrimordialSandboxX"] = sandboxCenterTile.X;
            tag["PrimordialSandboxY"] = sandboxCenterTile.Y;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Conversion = (ConversionState)tag.GetByte("PrimordialConversion");
            Site = (SiteMode)tag.GetByte("PrimordialSite");
            ArrivalDone = tag.GetBool("PrimordialArrivalDone");
            if (tag.ContainsKey("PrimordialLanding"))
            {
                LandingWorld = tag.Get<Vector2>("PrimordialLanding");
            }

            sandboxCenterTile = new Point(tag.GetInt("PrimordialSandboxX"), tag.GetInt("PrimordialSandboxY"));

            clearedRings.Clear();
            foreach (string ringId in tag.GetList<string>("PrimordialClearedRings"))
            {
                clearedRings.Add(ringId);
            }

            // Saved mid-scene (autosave or quit): the vanilla tiles are half converted, so finish on the first update.
            resumePending = Conversion == ConversionState.Running;
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((byte)Conversion);
            writer.Write(SceneTick);
            writer.Write((byte)Site);
            writer.Write(ArrivalDone);
            writer.WriteVector2(LandingWorld);
            writer.Write(sandboxCenterTile.X);
            writer.Write(sandboxCenterTile.Y);
        }

        /// <summary>
        /// Clients follow the server's scene through world data. A client that is not yet playing the scene starts it at the
        /// server's tick (scene start or a late join); one that is far behind jumps (a skip). Small drift is left alone.
        /// </summary>
        public override void NetReceive(BinaryReader reader)
        {
            ConversionState serverConversion = (ConversionState)reader.ReadByte();
            int serverTick = reader.ReadInt32();
            Site = (SiteMode)reader.ReadByte();
            ArrivalDone = reader.ReadBoolean();
            LandingWorld = reader.ReadVector2();
            sandboxCenterTile = new Point(reader.ReadInt32(), reader.ReadInt32());

            if (serverTick >= 0 && !SceneRunning)
            {
                StartSceneLocally();
                Conversion = ConversionState.Running;
                if (serverTick >= ConversionEndTick)
                {
                    FinishConversionNow();
                }

                SceneTick = serverTick;
                return;
            }

            if (serverTick >= 0 && SceneRunning && serverTick > SceneTick + 30)
            {
                JumpTo(serverTick);
                return;
            }

            if (serverTick < 0 && SceneRunning && serverConversion == ConversionState.Done)
            {
                FinishConversionNow();
                SceneTick = -1;
            }

            if (!SceneRunning)
            {
                Conversion = serverConversion;
            }
        }

        #endregion

        #region World rules used elsewhere

        /// <summary>True while the given player should hear `Tomb`: above ground in the Primordial world, or during the scene.</summary>
        public static bool WantsTombMusic(Player player)
        {
            if (SceneRunning)
            {
                return true;
            }

            return tsorcRevampWorld.PrimordialAge && (player.ZoneOverworldHeight || player.ZoneSkyHeight);
        }

        /// <summary>A Hellkite Drake from natural Primordial spawning: not a boss, no boss music, souls only.</summary>
        public static bool IsWildDrake(NPC npc)
        {
            return npc.ModNPC is HellkiteDrake drake && drake.Wild;
        }

        /// <summary>
        /// Replaces the spawn pool in the Primordial world. Only wild Hellkite Drakes on the surface and Ancient Demons
        /// underground; nothing during the scene. Returns false outside the age so the normal pool runs.
        /// </summary>
        public static bool ReplaceSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return false;
            }

            pool.Clear();
            if (SceneRunning || CutsceneSystem.IsActive || tsorcRevampWorld.BossAlive)
            {
                return true;
            }

            Player player = spawnInfo.Player;
            bool surface = player.ZoneOverworldHeight || player.ZoneSkyHeight;
            bool underground = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight || player.ZoneUnderworldHeight;
            int drakeType = ModContent.NPCType<HellkiteDrake>();

            if (surface && !NPC.AnyNPCs(drakeType) && Main.rand.NextFloat() < WildDrakeChance)
            {
                pool[drakeType] = 1f;
            }
            else if (underground && Main.rand.NextFloat() < AncientDemonChance)
            {
                pool[ModContent.NPCType<NPCs.Bosses.Demons.AncientDemon>()] = 1f;
            }

            return true;
        }

        // Condition names whose own rule still applies in the age: story, Abyss and map-scoped checks, and the two rings
        // at the Hellkite site. Every other boss ring ignores progress conditions (pre-Eye, pre-Hardmode, ...).
        private static readonly HashSet<string> KeptEventConditions = new HashSet<string>
        {
            "HellkiteDrakeCondition", "AbyssPortalCustomCondition", "TheAbyssCustomCondition", "GwynsTombVisionCustomCondition",
            "AttraidiesTheSorrowCondition", "LeonhardRemixSecretCondition", "ThoriumActiveCondition", "PrimeGwynRingCondition"
        };

        /// <summary>
        /// Scripted-event gate. Outside the age, the event's own condition. In the age: nothing during the scene; ordinary
        /// ambushes off; boss rings return regardless of progress conditions, but keep map scoping (Remix and Adventure
        /// conditions) and the story/Abyss rules above. Dynamic events with combined (lambda) conditions keep theirs.
        /// </summary>
        public static bool EventConditionMet(ScriptedEvent scriptedEvent)
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return scriptedEvent.condition();
            }

            if (SceneRunning)
            {
                return false;
            }

            if (!scriptedEvent.bossEvent)
            {
                return scriptedEvent.noNPCEvent && scriptedEvent.condition();
            }

            // A non-saving ring won in the age stays won. Saving rings record their own completion.
            if (!scriptedEvent.save && clearedRings.Count > 0)
            {
                string ringId = RingId(scriptedEvent);
                bool wonInAge = ringId != null && clearedRings.Contains(ringId);
                if (wonInAge)
                {
                    return false;
                }
            }

            string conditionName = scriptedEvent.condition.Method.Name;
            bool combinedCondition = conditionName.Contains("<");
            if (combinedCondition || KeptEventConditions.Contains(conditionName) || conditionName.Contains("Adventure"))
            {
                return scriptedEvent.condition();
            }

            if (conditionName.Contains("Remix"))
            {
                return tsorcRevampWorld.RemixMap;
            }

            return true;
        }

        /// <summary>Saved name of a scripted event: its dynamic event ID, or its hardcoded event type. Null if neither.</summary>
        private static string RingId(ScriptedEvent scriptedEvent)
        {
            if (!string.IsNullOrEmpty(scriptedEvent.DynamicEventID))
            {
                return scriptedEvent.DynamicEventID;
            }

            foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
            {
                if (pair.Value == scriptedEvent)
                {
                    return pair.Key.ToString();
                }
            }

            return null;
        }

        /// <summary>Prime Gwyn's ring at the Hellkite site: adventure map only, after the arrival, while he is not out.</summary>
        public static bool PrimeRingAvailable()
        {
            return tsorcRevampWorld.PrimordialAge && ArrivalDone && Site == SiteMode.Adventure
                && tsorcRevampWorld.OnlyAdventureMap && !tsorcRevampWorld.RemixMap
                && !NPC.AnyNPCs(ModContent.NPCType<PrimeGwyn>());
        }

        /// <summary>A boss without its own Super Hardmode values: vanilla bosses and the mod's pre-SHM bosses.</summary>
        public static bool IsOlderBoss(NPC npc)
        {
            if (!npc.boss)
            {
                return false;
            }

            if (npc.ModNPC == null)
            {
                return true;
            }

            string nameSpace = npc.ModNPC.GetType().Namespace ?? "";
            return !nameSpace.Contains("SuperHardMode") && !nameSpace.Contains("Primordial");
        }

        /// <summary>One older boss's hit multiplier: its ordinary contact hit raised to about 95, never below 1, at most 5.
        /// Bosses that deal no contact damage use a flat fallback.</summary>
        public static float OlderBossDamageMultiplier(NPC boss)
        {
            if (boss.defDamage <= 0)
            {
                return OlderBossDamageMultiplierFallback;
            }

            float multiplier = OlderBossTargetHit / boss.defDamage;
            return MathHelper.Clamp(multiplier, 1f, OlderBossDamageMultiplierMax);
        }

        #endregion
    }

    /// <summary>
    /// Per-NPC Primordial rules: older-boss health (set during SetDefaults/ScaleStats on every machine, so server and
    /// clients agree), Pinwheel's mid-fight life bars, The Machine's arms, the biome checks of vanilla bosses whose biome
    /// the age removed, and the wild Drake's despawn.
    /// </summary>
    public class PrimordialBossNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // Pinwheel writes its second bar (5000 x HealthScale) and its clones' bars into life/lifeMax inside its AI, in its
        // own numbers. The factor covers both bars, and its AI runs in its own numbers (PreAI/PostAI convert around it).
        private const int PinwheelSecondBarLife = 5000;

        // The Machine's core life is recomputed every tick as its own 35,000 pool (TheMachine.HandleLife) plus the six arms'
        // life. The arms take the damage, so they carry the scaling and the core pool stays as authored.
        private const int MachineCoreLife = 35000;
        private const int MachineArmCount = 6;

        // Wild Drake leaves when no living player has been within this range (px) for this long (ticks). Server only.
        private const float WildDrakeDespawnRange = 3200f;
        private const int WildDrakeDespawnTicks = 600;

        private enum BossHome : byte
        {
            Jungle,
            Corruption,
            Crimson,
            Hallow,
            Snow
        }

        // Vanilla bosses that enrage, flee or despawn when their target leaves a biome the age has removed.
        private static readonly Dictionary<int, BossHome> BossHomes = new Dictionary<int, BossHome>
        {
            { NPCID.QueenBee, BossHome.Jungle },
            { NPCID.Plantera, BossHome.Jungle },
            { NPCID.PlanterasHook, BossHome.Jungle },
            { NPCID.EaterofWorldsHead, BossHome.Corruption },
            { NPCID.BrainofCthulhu, BossHome.Crimson },
            { NPCID.HallowBoss, BossHome.Hallow },
            { NPCID.Deerclops, BossHome.Snow }
        };

        // Each player's own zone flag while a biome boss runs its AI. One NPC's AI runs at a time, so one buffer serves all.
        private static readonly bool[] savedZones = new bool[Main.maxPlayers];

        /// <summary>Pinwheel's Primordial life factor; 0 for every other NPC. Its AI runs in its own numbers.</summary>
        private float ownUnitsFactor;
        private bool inOwnUnits;
        private int savedLife;
        private int savedLifeMax;
        private int ownUnitsLife;
        private int ownUnitsLifeMax;

        private bool zonesSwapped;
        private int wildDrakeFarTicks;

        /// <summary>
        /// The Machine's arms take their share of 500,000 here. So does any older boss that vanilla scaling skips:
        /// ScaleStats returns before every scaling hook for a zero-damage NPC without NeedsExpertScaling (Pinwheel), and
        /// such a boss never gets Expert doubling, so its own life is its Expert value.
        /// </summary>
        public override void SetDefaults(NPC npc)
        {
            ownUnitsFactor = 0f;
            wildDrakeFarTicks = 0;
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return;
            }

            bool machineArm = npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeBeam || npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeBuzzsaw
                || npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeGatling || npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeIon
                || npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeSiege || npc.ModNPC is NPCs.Bosses.PrimeV2.PrimeWelder;
            if (machineArm)
            {
                float armLifeTotal = MachineArmCount * (float)NPCs.Bosses.PrimeV2.TheMachine.PrimeArmHealth;
                float armScale = (PrimordialWorldSystem.OlderBossExpertLife - MachineCoreLife) / armLifeTotal;
                npc.lifeMax = Math.Max(1, (int)Math.Round(npc.lifeMax * armScale));
                return;
            }

            bool scaleStatsSkips = !NPCID.Sets.NeedsExpertScaling[npc.type]
                && (npc.lifeMax <= 5 || npc.damage == 0 || npc.friendly || npc.townNPC);
            if (scaleStatsSkips)
            {
                ApplyPrimordialLife(npc, false);
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return;
            }

            ApplyPrimordialLife(npc, true);
        }

        /// <summary>
        /// Older bosses get 500,000 Expert health. The ratio to the boss's own Expert value is applied on top of what
        /// scaling already did, so Normal, Master and multiplayer scaling keep their usual effect. expertScaled: vanilla
        /// doubled this boss for Expert, so its own Expert value is twice its unscaled life. Paired bosses split the
        /// total by their own proportions; Eater of Worlds splits it across its segments.
        /// </summary>
        private void ApplyPrimordialLife(NPC npc, bool expertScaled)
        {
            if (!PrimordialWorldSystem.IsOlderBoss(npc))
            {
                return;
            }

            if (!ContentSamples.NpcsByNetId.TryGetValue(npc.type, out NPC sample) || sample.lifeMax <= 0)
            {
                return;
            }

            float share = 1f;
            bool eaterSegment = npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail;
            if (eaterSegment)
            {
                share = 1f / PrimordialWorldSystem.EaterOfWorldsSegmentEstimate;
            }
            else
            {
                tsorcRevampWorld.PopulatePairedBosses();
                int pairIndex = tsorcRevampWorld.PairedBosses.IndexOf(npc.type);
                if (pairIndex >= 0)
                {
                    int partnerIndex = pairIndex + 1;
                    if (pairIndex % 2 == 1)
                    {
                        partnerIndex = pairIndex - 1;
                    }

                    int partnerType = tsorcRevampWorld.PairedBosses[partnerIndex];
                    if (ContentSamples.NpcsByNetId.TryGetValue(partnerType, out NPC partnerSample) && partnerSample.lifeMax > 0)
                    {
                        share = sample.lifeMax / (float)(sample.lifeMax + partnerSample.lifeMax);
                    }
                }
            }

            float ownExpertLife = sample.lifeMax;
            if (expertScaled)
            {
                ownExpertLife *= 2f;
            }

            bool pinwheel = npc.ModNPC is NPCs.Bosses.Pinwheel.Pinwheel;
            if (pinwheel)
            {
                ownExpertLife += PinwheelSecondBarLife;
            }

            float scale = PrimordialWorldSystem.OlderBossExpertLife * share / ownExpertLife;
            npc.lifeMax = Math.Max(1, (int)Math.Round(npc.lifeMax * scale));

            if (pinwheel)
            {
                ownUnitsFactor = scale;
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return true;
            }

            // Pinwheel's AI sees its life in its own numbers. Ceiling, so a boss with any life left never reads 0 (dying).
            if (ownUnitsFactor > 0f)
            {
                savedLife = npc.life;
                savedLifeMax = npc.lifeMax;
                ownUnitsLifeMax = Math.Max(1, (int)Math.Round(npc.lifeMax / ownUnitsFactor));
                ownUnitsLife = (int)Math.Ceiling(npc.life / ownUnitsFactor);
                npc.lifeMax = ownUnitsLifeMax;
                npc.life = ownUnitsLife;
                inOwnUnits = true;
            }

            // A biome boss sees every player as standing in its biome for the length of its AI.
            if (BossHomes.TryGetValue(npc.type, out BossHome home))
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    savedZones[player.whoAmI] = SwapZone(player, home, true);
                }

                zonesSwapped = true;
            }

            return true;
        }

        public override void PostAI(NPC npc)
        {
            // Back to real numbers. Values the AI did not touch come back exactly; new ones are scaled. A life of 0 or 1 is
            // the boss's own dying / held-at-1 state and stays as written.
            if (inOwnUnits)
            {
                inOwnUnits = false;

                int realLifeMax = savedLifeMax;
                if (npc.lifeMax != ownUnitsLifeMax)
                {
                    realLifeMax = Math.Max(1, (int)Math.Round(npc.lifeMax * ownUnitsFactor));
                }

                int realLife = savedLife;
                if (npc.life != ownUnitsLife)
                {
                    realLife = npc.life;
                    if (npc.life > 1)
                    {
                        realLife = (int)Math.Round(npc.life * ownUnitsFactor);
                    }
                }

                npc.lifeMax = realLifeMax;
                npc.life = Math.Min(realLife, realLifeMax);
            }

            if (zonesSwapped)
            {
                zonesSwapped = false;
                BossHome home = BossHomes[npc.type];
                foreach (Player player in Main.ActivePlayers)
                {
                    SwapZone(player, home, savedZones[player.whoAmI]);
                }
            }

            bool wildDrake = Main.netMode != NetmodeID.MultiplayerClient && PrimordialWorldSystem.IsWildDrake(npc);
            if (wildDrake)
            {
                bool playerNear = false;
                foreach (Player player in Main.ActivePlayers)
                {
                    if (!player.dead && Vector2.Distance(player.Center, npc.Center) < WildDrakeDespawnRange)
                    {
                        playerNear = true;
                    }
                }

                wildDrakeFarTicks++;
                if (playerNear)
                {
                    wildDrakeFarTicks = 0;
                }

                if (wildDrakeFarTicks >= WildDrakeDespawnTicks)
                {
                    npc.active = false;
                    npc.netSkip = -1;
                    npc.life = 0;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                    }
                }
            }
        }

        /// <summary>Sets one player's zone flag for a boss's home biome and returns what it was.</summary>
        private static bool SwapZone(Player player, BossHome home, bool value)
        {
            bool previous;
            switch (home)
            {
                case BossHome.Jungle:
                    previous = player.ZoneJungle;
                    player.ZoneJungle = value;
                    break;
                case BossHome.Corruption:
                    previous = player.ZoneCorrupt;
                    player.ZoneCorrupt = value;
                    break;
                case BossHome.Crimson:
                    previous = player.ZoneCrimson;
                    player.ZoneCrimson = value;
                    break;
                case BossHome.Hallow:
                    previous = player.ZoneHallow;
                    player.ZoneHallow = value;
                    break;
                default:
                    previous = player.ZoneSnow;
                    player.ZoneSnow = value;
                    break;
            }

            return previous;
        }
    }

    /// <summary>Per-player Primordial rules: older-boss hit damage, Prime's arena grounding, and respawn rejoin.</summary>
    public class PrimordialPlayer : ModPlayer
    {
        /// <summary>
        /// Older-boss damage. A hit from an older boss (or a segment sharing its life) uses that boss's multiplier. A hit
        /// from a projectile or a non-boss add uses the multiplier of the nearest older boss within 4000px, so shots still
        /// in flight and summoned adds keep it. Wild Drakes and Ancient Demons are never boosted.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!tsorcRevampWorld.PrimordialAge)
            {
                return;
            }

            PlayerDeathReason source = modifiers.DamageSource;
            bool enemyHit = source != null && source.SourcePlayerIndex == -1
                && (source.SourceNPCIndex != -1 || source.SourceProjectileLocalIndex != -1);
            if (!enemyHit)
            {
                return;
            }

            NPC sourceBoss = null;
            if (source.SourceNPCIndex >= 0 && source.SourceNPCIndex < Main.maxNPCs)
            {
                NPC attacker = Main.npc[source.SourceNPCIndex];
                if (attacker.realLife >= 0 && attacker.realLife < Main.maxNPCs)
                {
                    attacker = Main.npc[attacker.realLife];
                }

                bool regularPrimordialEnemy = attacker.type == ModContent.NPCType<HellkiteDrake>()
                    || attacker.type == ModContent.NPCType<NPCs.Bosses.Demons.AncientDemon>();
                if (regularPrimordialEnemy)
                {
                    return;
                }

                if (PrimordialWorldSystem.IsOlderBoss(attacker))
                {
                    sourceBoss = attacker;
                }
            }

            if (sourceBoss == null)
            {
                float closestDistance = PrimordialWorldSystem.OlderBossHitSearchRange;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    float distance = Vector2.Distance(npc.Center, Player.Center);
                    if (distance < closestDistance && PrimordialWorldSystem.IsOlderBoss(npc))
                    {
                        closestDistance = distance;
                        sourceBoss = npc;
                    }
                }
            }

            if (sourceBoss == null)
            {
                return;
            }

            modifiers.FinalDamage *= PrimordialWorldSystem.OlderBossDamageMultiplier(sourceBoss);
        }

        // The jungle summons check ZoneJungle, which the age removed with the jungle grass and mud. Their user counts as in
        // the jungle for the use; UpdateBiomes recomputes the flag next tick. Runs before the item's own CanUseItem/UseItem.
        public override bool CanUseItem(Item item)
        {
            bool jungleSummon = item.type == ModContent.ItemType<Content.Items.BossItems.CursedSkull>()
                || item.type == ModContent.ItemType<Content.Items.BossItems.GrassyEgg>()
                || item.type == ModContent.ItemType<Content.Items.BossItems.PottedPlanteraBulb>();
            if (tsorcRevampWorld.PrimordialAge && jungleSummon)
            {
                Player.ZoneJungle = true;
            }

            return true;
        }

        // Prime's arena grounds everyone in it, not only his target: Torn Wings and Grapple Malfunction, refreshed each tick.
        public override void PreUpdateBuffs()
        {
            // Buffs belong to the owning client; other machines learn them through the normal buff sync.
            if (!tsorcRevampWorld.PrimordialAge || Player.dead || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            int primeType = ModContent.NPCType<PrimeGwyn>();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                bool primeNearby = npc.type == primeType && Vector2.Distance(npc.Center, Player.Center) < PrimordialWorldSystem.PrimeArenaDebuffRange;
                if (primeNearby)
                {
                    Player.AddBuff(ModContent.BuffType<TornWings>(), 2, false);
                    Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2, false);
                    return;
                }
            }
        }

        // A player who dies while Prime is out comes back to the landing point with a moment of immunity.
        public override void OnRespawn()
        {
            bool primeAlive = NPC.AnyNPCs(ModContent.NPCType<PrimeGwyn>());
            bool rejoin = tsorcRevampWorld.PrimordialAge && primeAlive && PrimordialWorldSystem.Site == PrimordialWorldSystem.SiteMode.Adventure
                && PrimordialWorldSystem.LandingWorld != Vector2.Zero;
            if (!rejoin || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            Vector2 standPosition = PrimordialWorldSystem.LandingWorld - new Vector2(Player.width / 2f, Player.height);
            Player.Teleport(standPosition, TeleportationStyleID.DebugTeleport);
            Player.immune = true;
            Player.immuneTime = PrimordialWorldSystem.RejoinImmunityTicks;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, standPosition.X, standPosition.Y, TeleportationStyleID.DebugTeleport);
            }
        }
    }
}
