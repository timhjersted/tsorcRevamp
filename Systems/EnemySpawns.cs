using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Systems
{
    /// <summary>What a spawn rule can look at: the spawn attempt itself plus per-attempt cached counts.</summary>
    public sealed class SpawnContext
    {
        public NPCSpawnInfo Info;
        public Player Player;

        private readonly Dictionary<int, int> activeCounts = new();

        public void Reset(NPCSpawnInfo info)
        {
            Info = info;
            Player = info.Player;
            activeCounts.Clear();
        }

        /// <summary>Live NPCs of one type. Counted once per attempt, however many rules ask.</summary>
        public int CountActive(int npcType)
        {
            if (!activeCounts.TryGetValue(npcType, out int count))
            {
                count = NPC.CountNPCS(npcType);
                activeCounts[npcType] = count;
            }

            return count;
        }
    }

    /// <summary>A named yes/no test on a spawn attempt. Combine with &amp; (and), | (or) and ! (not) to build readable names.</summary>
    public sealed class SpawnCondition
    {
        private readonly Func<SpawnContext, bool> test;

        public SpawnCondition(Func<SpawnContext, bool> test)
        {
            this.test = test;
        }

        public bool IsMet(SpawnContext context)
        {
            return test(context);
        }

        public static SpawnCondition operator &(SpawnCondition left, SpawnCondition right)
        {
            return new SpawnCondition(context => left.test(context) && right.test(context));
        }

        public static SpawnCondition operator |(SpawnCondition left, SpawnCondition right)
        {
            return new SpawnCondition(context => left.test(context) || right.test(context));
        }

        public static SpawnCondition operator !(SpawnCondition condition)
        {
            return new SpawnCondition(context => !condition.test(context));
        }
    }

    /// <summary>
    /// Central registry of natural spawns for modded enemies, switched by the "New Enemy Spawns" gameplay config.
    ///
    /// The vanilla spawn pool always contains key 0 = "let vanilla pick" with weight 1.0, so every weight here is relative to
    /// that: 0.1 is roughly one attempt in eleven when nothing else is valid. Weights of all valid enemies are normalized,
    /// so a weight only means something next to its neighbours. Overall spawn frequency is capped separately by player health
    /// in tsorcRevampGlobalNPC.EditSpawnRate.
    ///
    /// One Spawn line = one enemy + one condition + a weight per world stage (phm / hm / shm). A stage left out means the
    /// enemy does NOT spawn there (unlike the HP registry, nothing is inherited). If several lines of one enemy match, the
    /// highest weight wins, except that a line marked stop: true decides on the spot, like an early return in the old code
    /// (write those first and in the old order). max caps how many of that enemy may be alive. Registered enemies have their
    /// own SpawnChance replaced; vanilla enemies and enemies not listed here are untouched.
    ///
    /// Zones, depths and the other test names are plain Player / world checks, defined once below so the registry reads as
    /// "Zone.Jungle &amp; Depth.BelowSurface &amp; Time.Night". Add a name here the first time an enemy needs a new test.
    /// </summary>
    public class EnemySpawns : ModSystem
    {
        public static bool Enabled
        {
            get
            {
                tsorcRevampGameplayConfig config = ModContent.GetInstance<tsorcRevampGameplayConfig>();
                return config != null && config.NewSpawnBalance;
            }
        }

        private sealed class SpawnRule
        {
            public SpawnCondition Condition;

            // Indexed by stage: 0 = pre-Hardmode, 1 = Hardmode, 2 = Super Hardmode. 0 = does not spawn there.
            public float[] Weights = new float[3];

            // 0 = no cap. Otherwise the rule is skipped once this many of the enemy are alive.
            public int MaxActive;

            // True = when this rule applies it decides the weight and the later rules are not looked at.
            public bool Stop;
        }

        private sealed class ExclusiveZone
        {
            public SpawnCondition Condition;

            // Entries a failed name lookup skipped keep weight 0 and are ignored.
            public (int Type, float Weight)[] Pool;
        }

        private static readonly Dictionary<int, List<SpawnRule>> Rules = new();
        private static readonly List<ExclusiveZone> ExclusiveZones = new();
        private static readonly SpawnContext Context = new();

        #region Named tests

        public static class Zone
        {
            public static readonly SpawnCondition Jungle = new(context => context.Player.ZoneJungle);
            public static readonly SpawnCondition Corruption = new(context => context.Player.ZoneCorrupt);
            public static readonly SpawnCondition Crimson = new(context => context.Player.ZoneCrimson);
            public static readonly SpawnCondition Hallow = new(context => context.Player.ZoneHallow);
            public static readonly SpawnCondition Snow = new(context => context.Player.ZoneSnow);
            public static readonly SpawnCondition Desert = new(context => context.Player.ZoneDesert);
            public static readonly SpawnCondition UndergroundDesert = new(context => context.Player.ZoneUndergroundDesert);
            public static readonly SpawnCondition Dungeon = new(context => context.Player.ZoneDungeon);
            public static readonly SpawnCondition Meteor = new(context => context.Player.ZoneMeteor);
            public static readonly SpawnCondition Beach = new(context => context.Player.ZoneBeach);
            public static readonly SpawnCondition Graveyard = new(context => context.Player.ZoneGraveyard);
            public static readonly SpawnCondition Glowshroom = new(context => context.Player.ZoneGlowshroom);

            // Vanilla's plain forest (no other biome in range).
            public static readonly SpawnCondition Forest = new(context => context.Player.ZoneForest);

            // Plain underground and plain caverns: no other biome in range.
            public static readonly SpawnCondition NormalUnderground = new(context => context.Player.ZoneNormalUnderground);
            public static readonly SpawnCondition NormalCaverns = new(context => context.Player.ZoneNormalCaverns);

            // The player is inside the jungle temple (vanilla's Lihzahrd flag on the spawn attempt).
            public static readonly SpawnCondition Lihzahrd = new(context => context.Info.Lihzahrd);
        }

        // Vertical bands from the player's vanilla height zones. The old SpawnHelper bands compared pixel positions against
        // tile-sized rockLayer multiples, which made them far wider than their names; these are the bands the names say.
        public static class Depth
        {
            public static readonly SpawnCondition Sky = new(context => context.Player.ZoneSkyHeight);
            public static readonly SpawnCondition Surface = new(context => context.Player.ZoneOverworldHeight);
            public static readonly SpawnCondition Underground = new(context => context.Player.ZoneDirtLayerHeight);
            public static readonly SpawnCondition Cavern = new(context => context.Player.ZoneRockLayerHeight);
            public static readonly SpawnCondition Underworld = new(context => context.Player.ZoneUnderworldHeight);
            public static readonly SpawnCondition BelowSurface = Underground | Cavern;

            // Anywhere on the ground: surface, underground or cavern. No sky, no underworld.
            public static readonly SpawnCondition Ground = Surface | BelowSurface;

            // Bands that read the SPAWN TILE's row as a fraction of the map's height, as the old by-tile helpers did, so they follow
            // the world's size (the 2000-tall map and the 2400-tall expansion both put the lower half at row 1000 / 1200). They
            // are not tied to the vanilla zones: on the adventure map the surface is at row 875 (1164 expanded).
            public static SpawnCondition Rows(float fromFraction, float toFraction)
            {
                return new SpawnCondition(context =>
                    context.Info.SpawnTileY >= Main.maxTilesY * fromFraction && context.Info.SpawnTileY < Main.maxTilesY * toFraction);
            }

            // Row 1000 and below on the 2000-tall map: the lower half of the world.
            public static readonly SpawnCondition LowerHalf = Rows(0.5f, 1f);

            // 10% to 60% of the map height: from below the top of the sky down to the top of the deep caves (rows 200-1200).
            public static readonly SpawnCondition SurfaceToUpperCaves = Rows(0.1f, 0.6f);

            // 50% to 60%: just under the surface down to the top of the deep caves (rows 1000-1200).
            public static readonly SpawnCondition UpperCaves = Rows(0.5f, 0.6f);

            // 30% to 60% of the map height: the old by-tile Underground and Cavern bands together.
            public static readonly SpawnCondition Caves = Rows(0.3f, 0.6f);

            // 80% to 100% of the map height: the old by-tile Underworld band, which starts above vanilla's underworld.
            public static readonly SpawnCondition BottomFifth = Rows(0.8f, 1f);

            // The last 400 rows of the map, whatever its height.
            public static readonly SpawnCondition BottomRows = new(context => context.Info.SpawnTileY >= Main.maxTilesY - 400);
        }

        public static class Time
        {
            public static readonly SpawnCondition Day = new(context => Main.dayTime);
            public static readonly SpawnCondition Night = !Day;
            public static readonly SpawnCondition BloodMoon = new(context => Main.bloodMoon);
        }

        public static class Weather
        {
            public static readonly SpawnCondition Raining = new(context => Main.raining);

            // The player stands inside a rain zone (vanilla ZoneRain), as opposed to it merely raining somewhere in the world.
            public static readonly SpawnCondition RainZone = new(context => context.Player.ZoneRain);
        }

        // Whether the tile the enemy would spawn above is water.
        public static class Water
        {
            public static readonly SpawnCondition Wet = new(context => context.Info.Water);
            public static readonly SpawnCondition Dry = !Wet;
        }

        public static class Town
        {
            // No town NPCs near the player (vanilla's Player.townNPCs counts them, half-weighted when further away).
            public static readonly SpawnCondition None = new(context => context.Player.townNPCs <= 0f);
            public static readonly SpawnCondition AtMostOne = new(context => context.Player.townNPCs <= 1f);

            // Under one town NPC counted: allows a single distant one (they count half).
            public static readonly SpawnCondition UnderOne = new(context => context.Player.townNPCs < 1f);
        }

        public static class Invasion
        {
            // Vanilla invasions (goblins, pirates and so on) replace the normal pool, so most enemies stay out of them.
            public static readonly SpawnCondition None = new(context => !context.Info.Invasion);

            // Neither the Pumpkin Moon nor the Frost Moon is running.
            public static readonly SpawnCondition NoMoonEvent = new(context => !Main.pumpkinMoon && !Main.snowMoon);
        }

        public static class Region
        {
            // The western ocean of the adventure map. Map X is unchanged by the 2400-tall expansion, so no transform is needed.
            public static readonly SpawnCondition WesternSea = new(context => context.Info.SpawnTileX < 900);

            // The ocean strips: within 800 tiles of either map edge, measured at the spawn tile.
            public static readonly SpawnCondition Ocean = new(context =>
                context.Info.SpawnTileX < 800 || context.Info.SpawnTileX > Main.maxTilesX - 800);

            // Outside the map columns between two fractions of the width, measured at the spawn tile.
            public static SpawnCondition OutsideSpan(float fromFraction, float toFraction)
            {
                return new SpawnCondition(context =>
                    context.Info.SpawnTileX < Main.maxTilesX * fromFraction || context.Info.SpawnTileX > Main.maxTilesX * toFraction);
            }

            // The far-west coast: the spawn tile is within 800 tiles of the west edge. (WesternSea is 900, Ocean is both edges.)
            public static readonly SpawnCondition WestCoast = new(context => context.Info.SpawnTileX < 800);

            // The far-west jungle coast: the player's pixel X is under 3600 (tile 225). Map X is unchanged by the expansion.
            public static readonly SpawnCondition TropicalOcean = new(context => context.Player.position.X < 3600f);

            // The player is above row 1430 of the original 2000-tall map, measured at tile column 4615 (the Machine Temple's
            // column) and mapped for the expanded world.
            public static readonly SpawnCondition AboveTempleFloor = new(context =>
                (int)(context.Player.Center.Y / 16f) < ExpandedWorldTransform.MapTileY(4615, 1430));

            // The Remix Map's obsidian vault: the player is between tile columns 6083-6847 and rows 1664-1999. Remix coordinates, not mapped.
            public static readonly SpawnCondition RemixVaultBox = new(context =>
            {
                int playerTileX = (int)(context.Player.Center.X / 16f);
                int playerTileY = (int)(context.Player.Center.Y / 16f);
                return playerTileX > 6083 && playerTileX < 6847 && playerTileY > 1664 && playerTileY < 1999;
            });

            // The icy east coast: the spawn tile is within 800 tiles of the east edge. (Region.Ocean is both edges.)
            public static readonly SpawnCondition FrozenOcean = new(context => context.Info.SpawnTileX > Main.maxTilesX - 800);

            // Within a third of the map's width of the world spawn, measured at the spawn tile.
            public static readonly SpawnCondition InnerThird = new(context =>
                Math.Abs(context.Info.SpawnTileX - Main.spawnTileX) < Main.maxTilesX / 3);

            // The Mushroom Creature's cave on the adventure map: tiles 2550-2850 by rows 1300-1650 of the original map, mapped
            // for the expanded world.
            public static readonly SpawnCondition MushroomCaves = new(context =>
            {
                Point boxMin = ExpandedWorldTransform.MapTile(2550, 1300);
                Point boxMax = ExpandedWorldTransform.MapTile(2850, 1650);
                return context.Info.SpawnTileX >= boxMin.X && context.Info.SpawnTileX <= boxMax.X
                    && context.Info.SpawnTileY >= boxMin.Y && context.Info.SpawnTileY <= boxMax.Y;
            });

            // The two inland strips between 20-35% and 65-80% of the map width, measured at the player's feet.
            public static readonly SpawnCondition SideLands = new(context =>
            {
                int playerTileX = (int)(context.Player.Bottom.X + 8f) / 16;
                bool west = playerTileX > Main.maxTilesX * 0.2f && playerTileX < Main.maxTilesX * 0.35f;
                bool east = playerTileX > Main.maxTilesX * 0.65f && playerTileX < Main.maxTilesX * 0.8f;
                return west || east;
            });
        }

        // The tile type the enemy would spawn above.
        public static class SpawnTile
        {
            public static SpawnCondition Is(params int[] tileTypes)
            {
                return new SpawnCondition(context => Array.IndexOf(tileTypes, context.Info.SpawnTileType) >= 0);
            }
        }

        public static class Mode
        {
            // False in the server's Adventure Mode setting, which places its own enemies by hand.
            public static readonly SpawnCondition Sandbox = new(context => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode);

            // Expert and Master worlds.
            public static readonly SpawnCondition Expert = new(context => Main.expertMode);

            // The Remix Map world.
            public static readonly SpawnCondition Remix = new(context => tsorcRevampWorld.RemixMap);

            // The original adventure map (not the Remix Map, not a sandbox world).
            public static readonly SpawnCondition AdventureMap = new(context => tsorcRevampWorld.OnlyAdventureMap && !tsorcRevampWorld.RemixMap);
        }

        // The wall behind the tile the enemy would spawn above.
        public static class Wall
        {
            public static SpawnCondition Is(params int[] wallTypes)
            {
                return new SpawnCondition(context =>
                {
                    int wall = Main.tile[context.Info.SpawnTileX, context.Info.SpawnTileY].WallType;
                    return Array.IndexOf(wallTypes, wall) >= 0;
                });
            }

            // The wall behind the PLAYER's top-left tile instead of the spawn tile, as the old room checks read it.
            public static SpawnCondition AtPlayer(params int[] wallTypes)
            {
                return new SpawnCondition(context =>
                {
                    int wall = Main.tile[(int)context.Player.position.X / 16, (int)context.Player.position.Y / 16].WallType;
                    return Array.IndexOf(wallTypes, wall) >= 0;
                });
            }
        }

        // Shape of the ground around the spawn tile. All of these are false near the edge of the world.
        public static class Terrain
        {
            // The spawn tile is any grass.
            public static readonly SpawnCondition OnGrass = new(context => TileID.Sets.Conversion.Grass[context.Info.SpawnTileType]);

            // The spawn tile is not a half block or a slope.
            public static readonly SpawnCondition LevelFooting = new(context =>
            {
                Tile tile = Main.tile[context.Info.SpawnTileX, context.Info.SpawnTileY];
                return !tile.IsHalfBlock && !tile.RightSlope && !tile.LeftSlope;
            });

            // No wall 5, 8 or 12 tiles above the spawn tile: open to the sky or a tall cave.
            public static readonly SpawnCondition WallFreeAbove = new(context =>
            {
                int tileX = context.Info.SpawnTileX;
                int tileY = context.Info.SpawnTileY;
                return Main.tile[tileX, tileY - 5].WallType == WallID.None
                    || Main.tile[tileX, tileY - 8].WallType == WallID.None
                    || Main.tile[tileX, tileY - 12].WallType == WallID.None;
            });

            // A dirt, mud or plank wall 2 tiles above the spawn tile: the mouth of a hand-dug cave.
            public static readonly SpawnCondition DirtWallTwoAbove = new(context =>
            {
                int wall = Main.tile[context.Info.SpawnTileX, context.Info.SpawnTileY - 2].WallType;
                return wall == WallID.DirtUnsafe || wall == WallID.MudUnsafe || wall == WallID.Planked;
            });

            // The spawn tile is grass and level (not a half block or a slope).
            public static readonly SpawnCondition GrassFooting = new(context =>
            {
                if (!WorldGen.InWorld(context.Info.SpawnTileX, context.Info.SpawnTileY, 15))
                {
                    return false;
                }

                Tile tile = Main.tile[context.Info.SpawnTileX, context.Info.SpawnTileY];
                bool level = !tile.IsHalfBlock && !tile.RightSlope && !tile.LeftSlope;
                return TileID.Sets.Conversion.Grass[context.Info.SpawnTileType] && level;
            });

            // Open air above: no wall 5, 8 or 12 tiles up, or the mud wall of a jungle cave 2 tiles up.
            public static readonly SpawnCondition OpenAbove = new(context =>
            {
                if (!WorldGen.InWorld(context.Info.SpawnTileX, context.Info.SpawnTileY, 15))
                {
                    return false;
                }

                int tileX = context.Info.SpawnTileX;
                int tileY = context.Info.SpawnTileY;
                bool openWall = Main.tile[tileX, tileY - 5].WallType == WallID.None
                    || Main.tile[tileX, tileY - 8].WallType == WallID.None
                    || Main.tile[tileX, tileY - 12].WallType == WallID.None;
                return openWall || Main.tile[tileX, tileY - 2].WallType == WallID.MudUnsafe;
            });

            // A jungle grass nook for the Jungle Sentree: the spawn tile is jungle grass in front of no wall or a mud wall,
            // level jungle grass on both sides, no grass or mud (or slope) above either of them, and a clear column 8 tiles up.
            public static readonly SpawnCondition JungleGrassNook = new(context =>
            {
                if (!WorldGen.InWorld(context.Info.SpawnTileX, context.Info.SpawnTileY, 15))
                {
                    return false;
                }

                int tileX = context.Info.SpawnTileX;
                int tileY = context.Info.SpawnTileY;

                if (!TileID.Sets.Conversion.JungleGrass[context.Info.SpawnTileType])
                {
                    return false;
                }

                int wall = Main.tile[tileX, tileY].WallType;

                if (wall != WallID.None && wall != WallID.MudUnsafe)
                {
                    return false;
                }

                Tile left = Main.tile[tileX - 1, tileY];
                Tile right = Main.tile[tileX + 1, tileY];
                Tile aboveLeft = Main.tile[tileX - 1, tileY - 1];
                Tile aboveRight = Main.tile[tileX + 1, tileY - 1];
                Tile ceiling = Main.tile[tileX, tileY - 8];

                bool leftLevelGrass = left.TileType == TileID.JungleGrass && !left.IsHalfBlock && !left.LeftSlope;
                bool rightLevelGrass = right.TileType == TileID.JungleGrass && !right.IsHalfBlock && !right.RightSlope;
                bool aboveLeftOpen = aboveLeft.TileType != TileID.JungleGrass && aboveLeft.TileType != TileID.Mud && !aboveLeft.IsHalfBlock && !aboveLeft.RightSlope;
                bool aboveRightOpen = aboveRight.TileType != TileID.JungleGrass && aboveRight.TileType != TileID.Mud && !aboveRight.IsHalfBlock && !aboveRight.LeftSlope;
                bool ceilingOpen = ceiling.TileType != TileID.JungleGrass && ceiling.TileType != TileID.Mud;

                return leftLevelGrass && rightLevelGrass && aboveLeftOpen && aboveRightOpen && ceilingOpen;
            });

            // Level grass on the next tile to the right, with open ground (no grass or dirt, not a slope) above it.
            public static readonly SpawnCondition FlatGrassToTheRight = new(context =>
            {
                if (!WorldGen.InWorld(context.Info.SpawnTileX, context.Info.SpawnTileY, 15))
                {
                    return false;
                }

                Tile neighbor = Main.tile[context.Info.SpawnTileX + 1, context.Info.SpawnTileY];
                Tile above = Main.tile[context.Info.SpawnTileX + 1, context.Info.SpawnTileY - 1];
                bool neighborLevelGrass = neighbor.TileType == TileID.Grass && !neighbor.IsHalfBlock && !neighbor.RightSlope;
                bool aboveOpen = above.TileType != TileID.Grass && above.TileType != TileID.Dirt && !above.IsHalfBlock && !above.LeftSlope;
                return neighborLevelGrass && aboveOpen;
            });
        }

        // Live NPCs of another type. (An enemy's own cap is the max: argument of Spawn.)
        public static class Alive
        {
            public static SpawnCondition None<TNpc>() where TNpc : ModNPC
            {
                return new SpawnCondition(context => context.CountActive(ModContent.NPCType<TNpc>()) == 0);
            }
        }

        // Whether a boss has been killed in this world (the mod's own record, not a vanilla flag).
        public static class Slain
        {
            public static readonly SpawnCondition OolacileKnight = new(context =>
                tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileKnight>())));

            public static readonly SpawnCondition TheSorrow = new(context =>
                tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())));

            public static readonly SpawnCondition TheRage = new(context =>
                tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())));

            public static readonly SpawnCondition WyvernMage = new(context =>
                tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())));

            public static readonly SpawnCondition WyvernMageShadow = new(context =>
                tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>())));
        }

        // The world's stage as a plain test, for rules that gate on it instead of carrying a weight per stage.
        public static class Progress
        {
            // Hardmode or later (Super Hardmode keeps the Hardmode flag on).
            public static readonly SpawnCondition Hardmode = new(context => Main.hardMode);
        }

        // Vanilla progression flags. The mod's replacement bosses set the same flags.
        public static class Downed
        {
            public static readonly SpawnCondition Eye = new(context => NPC.downedBoss1);
            public static readonly SpawnCondition EaterOrBrain = new(context => NPC.downedBoss2);
            public static readonly SpawnCondition Skeletron = new(context => NPC.downedBoss3);
        }

        #endregion

        #region Shared names

        private static readonly SpawnCondition Evil = Zone.Corruption | Zone.Crimson;
        private static readonly SpawnCondition AnyDesert = Zone.Desert | Zone.UndergroundDesert;

        // None of the themed biomes: the plain forest and its caves. Snow and dungeon are not excluded.
        private static readonly SpawnCondition Neutral = !(Evil | Zone.Desert | Zone.Hallow | Zone.Jungle | Zone.Meteor);

        // Jungle with no evil biome or dungeon mixed in.
        private static readonly SpawnCondition PlainJungle = Zone.Jungle & !Zone.Dungeon & !Evil;

        private static readonly SpawnCondition QuietArea = Town.None & Invasion.None;

        // The dungeon mini-bosses keep to one at a time between them. Dungeon Mage waits for them but not for other mages.
        private static readonly SpawnCondition NoDungeonMiniBoss =
            Alive.None<AttraidiesIllusion>()
            & Alive.None<AttraidiesManifestation>()
            & Alive.None<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>();

        private static readonly SpawnCondition NoDungeonMiniBossOrMage = NoDungeonMiniBoss & Alive.None<DungeonMage>();

        // Jungle or meteor ground with no dungeon or evil biome mixed in: where the Dworc family lives.
        private static readonly SpawnCondition DworcLand = (Zone.Jungle | Zone.Meteor) & !Zone.Dungeon & !Evil;

        // Dry jungle outside the dungeon: the two Dworc hunters.
        private static readonly SpawnCondition HunterJungle = Water.Dry & !Zone.Dungeon & Zone.Jungle;

        // The same jungle in the lower half of the map (row 1000 and below on the 2000-tall map). Their underground lines only
        // fire there, a limit added to keep them out of the sky.
        private static readonly SpawnCondition LowerJungle = Depth.LowerHalf & HunterJungle;

        // The hollow family's shared limits: no invasion, at most one town NPC, dry, away from glowing mushrooms.
        private static readonly SpawnCondition HollowQuiet = Invasion.None & Town.AtMostOne & Water.Dry & !Zone.Glowshroom;

        private static readonly SpawnCondition HollowQuietNoSnow = HollowQuiet & !Zone.Snow;

        // The Hollow Spearman and Warrior also never spawn in the evil biomes.
        private static readonly SpawnCondition HollowCleanLand = HollowQuietNoSnow & !Evil;

        private static readonly SpawnCondition DryNoMushrooms = Water.Dry & !Zone.Glowshroom;

        // The Lothric knights' limits: dry, away from glowing mushrooms, at most one town NPC near.
        private static readonly SpawnCondition LothricLand = DryNoMushrooms & Town.AtMostOne;

        // Ordinary ground: not jungle, evil, snow, beach, sky or underworld. The Lothric knights keep to it.
        private static readonly SpawnCondition MainlandGround =
            !(Zone.Jungle | Evil | Zone.Snow | Zone.Beach | Depth.Sky | Depth.Underworld);

        private static readonly SpawnCondition MainlandGroundNoHallow = MainlandGround & !Zone.Hallow;

        private static readonly SpawnCondition EarlyBoss = Downed.Eye | Downed.EaterOrBrain;
        private static readonly SpawnCondition MidBoss = Downed.EaterOrBrain | Downed.Skeletron;

        // The rocky cave walls of the Catacombs of the Drowned (wall IDs as TEdit shows them).
        private static readonly SpawnCondition CatacombWalls =
            Wall.Is(WallID.Cave8Unsafe, WallID.RocksUnsafe3, WallID.RocksUnsafe4, WallID.Rocks2Echo, WallID.Rocks3Echo);

        // The green dungeon walls of the Forgotten City.
        private static readonly SpawnCondition GreenDungeonWalls = Wall.Is(WallID.GreenDungeonSlabUnsafe, WallID.GreenDungeonUnsafe);

        // Dunlending country: no snow, no invasion, at most one town NPC near.
        private static readonly SpawnCondition DunlendingLand = Invasion.None & !Zone.Snow & Town.AtMostOne;

        // The Tibian Amazon and Valkyrie: no invasion, no snow, no town NPCs near.
        private static readonly SpawnCondition TibianLand = Invasion.None & !Zone.Snow & Town.None;

        // Hallow with no meteor, jungle, dungeon or evil biome mixed in: the Willowisp's home.
        private static readonly SpawnCondition PlainHallow = Zone.Hallow & !Zone.Meteor & !Zone.Jungle & !Zone.Dungeon & !Evil;

        // Where the Necromancer can spawn: dry, no town NPCs near, not jungle or meteor.
        private static readonly SpawnCondition NecromancerLand = Town.None & Water.Dry & !(Zone.Jungle | Zone.Meteor);

        // The Machine Temple: green dungeon slab walls, above the temple floor.
        private static readonly SpawnCondition MachineTemple = Wall.Is(WallID.GreenDungeonSlabUnsafe) & Region.AboveTempleFloor;

        // Where an Abandoned Stump can sit: a level grass tile with open air above and a flat grass tile beside it.
        private static readonly SpawnCondition StumpSpot = Terrain.GrassFooting & Terrain.OpenAbove & Terrain.FlatGrassToTheRight;

        // The Wyvern Fortress in the sky: meteorite brick or glass walls.
        private static readonly SpawnCondition WyvernFortress = Depth.Sky & Wall.Is(WallID.MeteoriteBrick, WallID.Glass);

        // Basilisk country: evil biomes and desert, away from the themed zones, dry land below the surface or on it at night.
        private static readonly SpawnCondition BasiliskGround =
            (Evil | AnyDesert)
            & !(Zone.Dungeon | Zone.Jungle | Zone.Hallow | Zone.Meteor)
            & (Depth.BelowSurface | (Depth.Surface & Time.Night))
            & Water.Dry
            & Town.None;

        #endregion

        public override void PostSetupContent()
        {
            Rules.Clear();
            ExclusiveZones.Clear();
            BuildRegistry();
        }

        public override void Unload()
        {
            Rules.Clear();
            ExclusiveZones.Clear();
        }

        /// <summary>0 = pre-Hardmode, 1 = Hardmode, 2 = Super Hardmode.</summary>
        public static int CurrentStageIndex()
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                return 2;
            }

            if (Main.hardMode)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>The highest weight among an enemy's rules that apply right now, or 0.</summary>
        private static float BestWeight(int npcType, List<SpawnRule> rules, SpawnContext context, int stageIndex)
        {
            float best = 0f;

            foreach (SpawnRule rule in rules)
            {
                float weight = rule.Weights[stageIndex];

                if (weight <= 0f || (weight <= best && !rule.Stop))
                {
                    continue;
                }

                if (rule.MaxActive > 0 && context.CountActive(npcType) >= rule.MaxActive)
                {
                    continue;
                }

                if (!rule.Condition.IsMet(context))
                {
                    continue;
                }

                if (rule.Stop)
                {
                    return weight;
                }

                best = weight;
            }

            return best;
        }

        /// <summary>
        /// Called from the top of tsorcRevampGlobalNPC.EditSpawnPool, before that method's own clears and removals, so those
        /// still win over anything added here. Replaces each registered enemy's own pool entry with the registry's.
        /// </summary>
        public static void Apply(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!Enabled || Rules.Count == 0)
            {
                return;
            }

            Context.Reset(spawnInfo);
            int stageIndex = CurrentStageIndex();

            foreach (KeyValuePair<int, List<SpawnRule>> entry in Rules)
            {
                pool.Remove(entry.Key);

                float weight = BestWeight(entry.Key, entry.Value, Context, stageIndex);

                if (weight > 0f)
                {
                    pool[entry.Key] = weight;
                }
            }
        }

        /// <summary>
        /// Called from EditSpawnPool after its invasion block. Where an exclusive zone applies, empties the whole pool and puts
        /// only that zone's enemies in it. Later zones win where two overlap.
        /// </summary>
        public static void ApplyExclusiveZones(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (!Enabled || ExclusiveZones.Count == 0)
            {
                return;
            }

            Context.Reset(spawnInfo);

            foreach (ExclusiveZone zone in ExclusiveZones)
            {
                if (!zone.Condition.IsMet(Context))
                {
                    continue;
                }

                pool.Clear();

                foreach ((int type, float weight) in zone.Pool)
                {
                    if (weight > 0f)
                    {
                        pool[type] = weight;
                    }
                }
            }
        }

        /// <summary>The registry's view of one spot, for /spawnpool: each registered enemy that can spawn there and its share.</summary>
        public static List<(string Name, float Weight)> Snapshot(NPCSpawnInfo spawnInfo)
        {
            SpawnContext context = new();
            context.Reset(spawnInfo);
            int stageIndex = CurrentStageIndex();
            List<(string Name, float Weight)> found = new();

            // An exclusive zone replaces everything else; the last matching one wins, as in ApplyExclusiveZones.
            ExclusiveZone activeZone = null;

            foreach (ExclusiveZone zone in ExclusiveZones)
            {
                if (zone.Condition.IsMet(context))
                {
                    activeZone = zone;
                }
            }

            if (activeZone != null)
            {
                foreach ((int type, float weight) in activeZone.Pool)
                {
                    if (weight > 0f)
                    {
                        string name = NPCLoader.GetNPC(type)?.Name ?? NPCID.Search.GetName(type);
                        found.Add((name + " (exclusive zone)", weight));
                    }
                }

                return found;
            }

            foreach (KeyValuePair<int, List<SpawnRule>> entry in Rules)
            {
                float weight = BestWeight(entry.Key, entry.Value, context, stageIndex);

                if (weight > 0f)
                {
                    found.Add((NPCLoader.GetNPC(entry.Key).Name, weight));
                }
            }

            return found;
        }

        private void Spawn(string npcName, SpawnCondition condition, float phm = 0f, float hm = 0f, float shm = 0f, int max = 0, bool stop = false)
        {
            if (!Mod.TryFind(npcName, out ModNPC modNpc))
            {
                Mod.Logger.Warn($"EnemySpawns: no ModNPC named '{npcName}', spawn line skipped.");
                return;
            }

            if (!Rules.TryGetValue(modNpc.Type, out List<SpawnRule> rules))
            {
                rules = new List<SpawnRule>();
                Rules[modNpc.Type] = rules;
            }

            SpawnRule rule = new SpawnRule();
            rule.Condition = condition;
            rule.Weights[0] = phm;
            rule.Weights[1] = hm;
            rule.Weights[2] = shm;
            rule.MaxActive = max;
            rule.Stop = stop;
            rules.Add(rule);
        }

        // For enemies whose old chance doubled at night and doubled again in a blood moon (which only happens at night): writes the
        // base line, a night line at x2 and a blood moon line at x4.
        private void SpawnNightBoosted(string npcName, SpawnCondition condition, float phm = 0f, float hm = 0f, float shm = 0f, int max = 0)
        {
            Spawn(npcName, condition, phm, hm, shm, max);
            Spawn(npcName, condition & Time.Night, phm * 2f, hm * 2f, shm * 2f, max);
            Spawn(npcName, condition & Time.BloodMoon, phm * 4f, hm * 4f, shm * 4f, max);
        }

        // An area where the whole pool is replaced: when the condition holds, everything is cleared (vanilla's pick included)
        // and only the listed enemies spawn. Vanilla enemies can be listed by name.
        private void SpawnOnly(SpawnCondition condition, params (string Name, float Weight)[] enemies)
        {
            ExclusiveZone zone = new ExclusiveZone();
            zone.Condition = condition;
            zone.Pool = new (int Type, float Weight)[enemies.Length];

            for (int i = 0; i < enemies.Length; i++)
            {
                int npcType;

                if (Mod.TryFind(enemies[i].Name, out ModNPC modNpc))
                {
                    npcType = modNpc.Type;
                }
                else if (!NPCID.Search.TryGetId(enemies[i].Name, out npcType))
                {
                    Mod.Logger.Warn($"EnemySpawns: no NPC named '{enemies[i].Name}', skipped in an exclusive zone.");
                    continue;
                }

                zone.Pool[i] = (npcType, enemies[i].Weight);
            }

            ExclusiveZones.Add(zone);
        }

        private void BuildRegistry()
        {
            // Alphabetical by enemy. Notes say where a line differs from the enemy's old SpawnChance.

            // Weight 1.3 is the old value. The terrain test is strict, so valid spots are rare.
            Spawn("AbandonedStump", Time.Day & Neutral & StumpSpot, phm: 1.3f, hm: 1.3f, shm: 1.3f, max: 2);

            // Super Hardmode only; gates of one in 10 and one in 5 are folded in as 1/N.
            Spawn("AbyssLurker", Invasion.None & Water.Dry & Evil, shm: 0.1f);
            Spawn("AbyssLurker", Invasion.None & Water.Dry & Depth.Underworld, shm: 0.2f);

            // Super Hardmode only. The old tests compared the player's pixel position with tile counts, so the "deep cavern" and
            // "outer map" checks matched almost everywhere; the tile bands they were written for are used here: rows from 60% of
            // the map down to the underworld, and the outer 30% on each side. A blood moon doubles every weight.
            Spawn("AncientDemonOfTheAbyss", Depth.Underworld, shm: 0.001f);
            Spawn("AncientDemonOfTheAbyss", Depth.Underworld & Region.OutsideSpan(0.3f, 0.7f), shm: 0.002f);
            Spawn("AncientDemonOfTheAbyss", Depth.Rows(0.6f, 1f) & !Depth.Underworld, shm: 0.0005f);
            Spawn("AncientDemonOfTheAbyss", Depth.Underworld & Time.BloodMoon, shm: 0.002f);
            Spawn("AncientDemonOfTheAbyss", Depth.Underworld & Region.OutsideSpan(0.3f, 0.7f) & Time.BloodMoon, shm: 0.004f);
            Spawn("AncientDemonOfTheAbyss", Depth.Rows(0.6f, 1f) & !Depth.Underworld & Time.BloodMoon, shm: 0.001f);

            // Wyvern Fortress: common while the Wyvern Mage lives, rare after it falls, and back in Super Hardmode until the Shadow falls.
            Spawn("Archdeacon", WyvernFortress & !Slain.WyvernMage, hm: 0.3f);
            Spawn("Archdeacon", WyvernFortress & Slain.WyvernMage, hm: 0.01f);
            Spawn("Archdeacon", WyvernFortress & !Slain.WyvernMageShadow, shm: 0.2f);

            Spawn("ArmoredWraith", Zone.Meteor & (Depth.Surface | Depth.Cavern), phm: 0.04f, hm: 0.04f, shm: 0.04f);
            Spawn("ArmoredWraith", Zone.Meteor & Depth.Underground, phm: 0.033f, hm: 0.033f, shm: 0.033f);

            // Old rule: separate one-in-240 and one-in-280 gates stacked in the underground jungle; folded into the weights.
            // Super Hardmode also runs the Hardmode lines (the old tests used Main.hardMode). Blood moon doubled the SHM lines only.
            Spawn("Assassin", PlainJungle & !Depth.Surface, hm: 0.008f, shm: 0.008f);
            Spawn("Assassin", PlainJungle & Depth.Surface, hm: 0.004f, shm: 0.004f);
            Spawn("Assassin", Zone.Dungeon | Zone.Hallow | Zone.Snow | AnyDesert, hm: 0.005f, shm: 0.005f);
            Spawn("Assassin", Depth.Surface & Time.Night, hm: 0.003f, shm: 0.003f);
            Spawn("Assassin", Depth.Underground, shm: 0.002f);
            Spawn("Assassin", Depth.Cavern, shm: 0.001f);
            Spawn("Assassin", Depth.Underground & Time.BloodMoon, shm: 0.004f);
            Spawn("Assassin", Depth.Cavern & Time.BloodMoon, shm: 0.002f);

            Spawn("AttraidiesIllusion", Zone.Dungeon & NoDungeonMiniBossOrMage, phm: 0.02f, hm: 0.0125f, shm: 0.0125f);
            Spawn("AttraidiesIllusion", Depth.Underworld, phm: 0.033f, max: 1);
            Spawn("AttraidiesIllusion", Depth.Cavern, hm: 0.00525f);

            // The Hardmode cavern line was weight 1 behind a one-in-100 gate, folded into the weight.
            Spawn("AttraidiesManifestation", Zone.Dungeon & NoDungeonMiniBossOrMage, phm: 0.04f);
            Spawn("AttraidiesManifestation", Depth.BelowSurface & Water.Dry, hm: 0.01f);

            // The old depth tests used the pixel-versus-tile helpers; the ground bands are the intent (everything but sky and underworld).
            Spawn("BarrowWight", Town.None & !Zone.Meteor & Downed.Skeletron & Depth.SurfaceToUpperCaves & Region.SideLands, phm: 0.005f, hm: 0.005f, shm: 0.005f);
            Spawn("BarrowWight", Town.None & !Zone.Meteor & Zone.Dungeon, phm: 0.0083f, hm: 0.033f, shm: 0.008f);
            Spawn("BarrowWight", Town.None & !Zone.Meteor & Depth.Sky, hm: 0.0567f, shm: 0.025f);
            Spawn("BarrowWight", Town.None & !Zone.Meteor & Zone.Snow, hm: 0.033f, shm: 0.008f);

            // Super Hardmode only. Night doubles the weight and a blood moon doubles it again; the surface line is night only.
            SpawnNightBoosted("BarrowWightNemesis", Depth.Sky | Zone.Dungeon | Zone.Snow, shm: 0.17f);
            SpawnNightBoosted("BarrowWightNemesis", Depth.Surface & Time.Night, shm: 0.02f);

            Spawn("BasiliskHunter", BasiliskGround, shm: 0.15f, max: 2);
            Spawn("BasiliskShifter", BasiliskGround, hm: 0.15f, shm: 0.075f, max: 2);
            Spawn("BasiliskWalker", BasiliskGround, phm: 0.15f, hm: 0.05f, max: 2);

            // Every line was weight 1 behind a random gate (one in 100 to 250), folded into the weights.
            Spawn("BlackKnight", Town.AtMostOne & !Zone.Meteor & !Zone.Dungeon & !Evil & Depth.Surface & Downed.Skeletron & Time.Night, phm: 0.004f, hm: 0.004f, shm: 0.004f);
            Spawn("BlackKnight", Town.AtMostOne & Zone.Dungeon, hm: 0.01f, shm: 0.01f);
            Spawn("BlackKnight", Town.AtMostOne & !Evil & !Zone.Beach, hm: 0.004f, shm: 0.004f);
            Spawn("BlackKnight", Town.AtMostOne & Depth.Underworld, hm: 0.0063f, shm: 0.0063f);

            Spawn("Byakhee", QuietArea & Zone.Desert & Depth.Surface & Time.Night, hm: 0.0627f, shm: 0.0627f);
            Spawn("Byakhee", QuietArea & Zone.Desert & Depth.Surface & Time.Day, hm: 0.0368f, shm: 0.0368f);
            Spawn("Byakhee", QuietArea & Zone.UndergroundDesert & Time.Day, hm: 0.038f, shm: 0.038f);
            Spawn("Byakhee", QuietArea & Weather.RainZone & Depth.Surface & Time.Day, hm: 0.0433f, shm: 0.0433f);
            Spawn("Byakhee", QuietArea & Weather.RainZone & Depth.Surface & Time.Night, hm: 0.0855f, shm: 0.0855f);

            // Hardmode and later; gates (one in 15 to one in 40) are folded in as 1/N. Plain hallow gets richer as it goes deeper; the
            // east coast is the Sorrow's, more often until it has been slain.
            Spawn("ClericOfSorrow", Town.None & PlainHallow, hm: 0.025f, shm: 0.025f);
            Spawn("ClericOfSorrow", Town.None & PlainHallow & Depth.Underground, hm: 0.0286f, shm: 0.0286f);
            Spawn("ClericOfSorrow", Town.None & PlainHallow & Depth.Cavern, hm: 0.04f, shm: 0.04f);
            Spawn("ClericOfSorrow", Town.None & Region.FrozenOcean & !Slain.TheSorrow, hm: 0.0667f, shm: 0.0667f);
            Spawn("ClericOfSorrow", Town.None & Region.FrozenOcean & Slain.TheSorrow, hm: 0.04f, shm: 0.04f);

            // Was 0.2 times vanilla's sky-spawn chance, which is about 1 in the sky.
            Spawn("CloudBat", Depth.Sky, hm: 0.2f, shm: 0.2f);

            // Super Hardmode only: evil biomes above the rock layer, outside the dungeon.
            Spawn("CorruptedElemental", Evil & !Zone.Dungeon & (Depth.Sky | Depth.Surface | Depth.Underground), shm: 0.5f);

            // Super Hardmode only: the jungle below the surface. The old gate of one in two is folded in.
            Spawn("CorruptedHornet", Zone.Jungle & !Depth.Surface, shm: 0.5f);

            // CosmicCrystalLizard: deliberately NOT registered. Its own SpawnChance scales with the player's luck (0.02 x (1 + luck)),
            // which a registry weight cannot do, and an unregistered enemy keeps its own SpawnChance with New Enemy Spawns on.

            // The old rules capped these at two alive. Their random gates (one in 15 to 1000) are folded into the weights.
            Spawn("CrazedDemonSpirit", Zone.Meteor, hm: 0.04f, shm: 0.04f, max: 2);
            Spawn("CrazedDemonSpirit", Slain.TheSorrow & Depth.Underworld, phm: 0.003f, hm: 0.07f, shm: 0.07f, max: 2);
            Spawn("CrazedDemonSpirit", Zone.Crimson, shm: 0.15f, max: 2);

            // Super Hardmode only, one alive at a time. Standing in both snow and hallow doubles the weight, and a blood moon doubles
            // it again; the surface rate is lower than the one below it.
            Spawn("CrystalKnight", (Zone.Snow | Zone.Hallow) & Depth.Surface, shm: 0.2f, max: 1);
            Spawn("CrystalKnight", (Zone.Snow | Zone.Hallow) & !Depth.Surface, shm: 0.36f, max: 1);
            Spawn("CrystalKnight", Zone.Snow & Zone.Hallow & Depth.Surface, shm: 0.4f, max: 1);
            Spawn("CrystalKnight", Zone.Snow & Zone.Hallow & !Depth.Surface, shm: 0.72f, max: 1);
            Spawn("CrystalKnight", (Zone.Snow | Zone.Hallow) & Depth.Surface & Time.BloodMoon, shm: 0.4f, max: 1);
            Spawn("CrystalKnight", (Zone.Snow | Zone.Hallow) & !Depth.Surface & Time.BloodMoon, shm: 0.72f, max: 1);
            Spawn("CrystalKnight", Zone.Snow & Zone.Hallow & Depth.Surface & Time.BloodMoon, shm: 0.8f, max: 1);
            Spawn("CrystalKnight", Zone.Snow & Zone.Hallow & !Depth.Surface & Time.BloodMoon, shm: 1.44f, max: 1);

            // Super Hardmode only, one alive at a time, not on the ocean strips or in the dungeon.
            SpawnNightBoosted("DarkBloodKnight", !Zone.Dungeon & (Depth.Underworld | Zone.Crimson) & !Region.Ocean & Depth.Surface, shm: 0.25f, max: 1);
            SpawnNightBoosted("DarkBloodKnight", !Zone.Dungeon & (Depth.Underworld | Zone.Crimson) & !Region.Ocean & !Depth.Surface, shm: 0.3f, max: 1);

            // Super Hardmode only, one alive at a time. The old test allowed one distant town NPC (UnderOne).
            SpawnNightBoosted("DarkKnight", Town.UnderOne & (Zone.Corruption | Zone.Dungeon) & !Zone.Meteor & !Zone.Jungle & !Depth.Underworld & !Zone.Hallow & !Region.Ocean, shm: 0.2f, max: 1);

            Spawn("DemonElemental", Depth.Underworld, phm: 0.067f, hm: 0.01f, shm: 0.01f, max: 2);
            Spawn("DemonElemental", Zone.Crimson, hm: 0.04f, shm: 0.04f, max: 2);
            Spawn("DemonElemental", Zone.Desert, hm: 0.022f, shm: 0.022f, max: 2);

            // DemonLordApocalypse

            // The Hardmode rate adds the old one-in-1000 second chance to one in 45.
            Spawn("DemonSpirit", Depth.Underworld, phm: 0.022f, hm: 0.023f, shm: 0.023f, max: 2);

            // Dungeon from Hardmode, richer in Super Hardmode, which adds the evil biomes and the underworld.
            Spawn("DemonWheel", Zone.Dungeon, hm: 0.1f, shm: 0.5f);
            Spawn("DemonWheel", Evil, shm: 0.5f);
            Spawn("DemonWheel", Depth.Underworld, shm: 0.25f);

            Spawn("DungeonMage", Zone.Dungeon & NoDungeonMiniBoss, phm: 0.05f, hm: 0.05f, shm: 0.05f);

            // Pre-Hardmode and Hardmode only, on purpose. (The old Super Hardmode line divided by zero but sat behind the Hardmode
            // line, which always returned first, so it used to spawn there at the Hardmode half rate.)
            // The overworld day and night lines had stopped firing: a "spawn row 1000 or deeper" test was added to keep the enemy out
            // of the sky, but the adventure map's surface is at row 875 (1164 expanded), so it cut the surface off too. They are
            // written here for the real surface, with no row test (the surface zone already excludes the sky). The underground band
            // keeps the row test: UpperCaves, just under the surface.
            Spawn("Dunlending", DunlendingLand & Depth.Surface & Time.Day, phm: 0.067f, hm: 0.034f);
            Spawn("Dunlending", DunlendingLand & Depth.Surface & Time.Night, phm: 0.125f, hm: 0.0625f);
            Spawn("Dunlending", DunlendingLand & Depth.UpperCaves & !Evil, phm: 0.0835f, hm: 0.042f);

            // The old depth scaling (x1.3 underground, x1.5 cavern) is written out; blood moon doubled the base only here.
            Spawn("DworcAbysswalker", DworcLand, shm: 0.1f, max: 1);
            Spawn("DworcAbysswalker", DworcLand & Depth.Underground, shm: 0.13f, max: 1);
            Spawn("DworcAbysswalker", DworcLand & Depth.Cavern, shm: 0.15f, max: 1);
            Spawn("DworcAbysswalker", DworcLand & Time.BloodMoon, shm: 0.2f, max: 1);

            Spawn("DworcAlchemist", Water.Dry & DworcLand & Depth.Surface, phm: 0.004f, hm: 0.004f, shm: 0.004f, max: 1);
            Spawn("DworcAlchemist", Water.Dry & DworcLand & Depth.Underground, phm: 0.008f, hm: 0.008f, shm: 0.008f, max: 1);
            Spawn("DworcAlchemist", Water.Dry & DworcLand & Depth.Cavern, phm: 0.065f, hm: 0.065f, shm: 0.065f, max: 1);
            Spawn("DworcAlchemist", Water.Dry & Zone.Jungle & !Zone.Meteor & !Zone.Beach & !Evil, hm: 0.005f, shm: 0.005f, max: 1);

            // Both hunters also have a surface-jungle line (0.265 and 0.2). It had stopped firing because of the same row-1000 test
            // that was meant to keep them out of the sky; it is written for the real surface with no row test.
            Spawn("DworcFleshhunter", HunterJungle & Depth.Surface, phm: 0.265f);
            Spawn("DworcFleshhunter", LowerJungle & Depth.BelowSurface, phm: 0.31f);
            Spawn("DworcVenomsniper", HunterJungle & Depth.Surface, phm: 0.2f);
            Spawn("DworcVenomsniper", LowerJungle & Depth.BelowSurface, phm: 0.345f);

            Spawn("DworcVoodooShaman", Water.Dry & DworcLand & Depth.Surface & Time.Day, hm: 0.01f, shm: 0.01f, max: 1);
            Spawn("DworcVoodooShaman", Water.Dry & DworcLand & Depth.Surface & Time.Night, hm: 0.035f, shm: 0.035f, max: 1);
            Spawn("DworcVoodooShaman", Water.Dry & DworcLand & Depth.Underground, hm: 0.025f, shm: 0.025f, max: 1);
            Spawn("DworcVoodooShaman", Water.Dry & DworcLand & Depth.Cavern, hm: 0.03f, shm: 0.03f, max: 1);
            Spawn("DworcVoodooShaman", Water.Dry & Region.TropicalOcean & Zone.Jungle, hm: 0.045f, shm: 0.045f, max: 1);

            // Hardmode and Super Hardmode share one weight, as in the old rule (it tested Main.hardMode, which stays true in SHM).
            Spawn("Eland", Zone.Jungle & !Evil & Depth.BelowSurface, phm: 0.01f, hm: 0.25f, shm: 0.25f);
            Spawn("Eland", Zone.Jungle & !Evil & Depth.Surface & Weather.Raining & Time.Night, hm: 0.15f, shm: 0.15f);

            // Was 0.3 behind a one-in-150 random gate, folded into the weight (0.3 / 150).
            Spawn("EvilEye", Zone.Hallow & Depth.Surface, hm: 0.002f, shm: 0.002f, max: 1);

            // Caves are 30-60% of the map height; the underworld lines are the bottom fifth. Both were by-tile helpers. The random
            // gates (one in 350 and one in 150) are folded into the weights.
            Spawn("FallenNecromancer", Town.None & !Zone.Jungle & !Zone.Meteor & Depth.Rows(0.3f, 0.6f) & Region.OutsideSpan(0.45f, 0.75f), hm: 0.0029f, shm: 0.0029f);
            Spawn("FallenNecromancer", Town.None & !Zone.Jungle & !Zone.Meteor & Depth.Rows(0.8f, 1f), hm: 0.0067f, shm: 0.0067f);

            // Lines are in the old order and each one decides (stop). Three old lines were hidden behind broader ones and never
            // fired; they are made live: the post-boss underground (0.09) and the Super Hardmode overworld (0.13) now sit before the
            // line that used to hide them, and the Super Hardmode dungeon (0.17) is that stage's weight on the dungeon line.
            Spawn("FirebombHollow", HollowQuiet & Zone.Graveyard, phm: 0.05f, hm: 0.05f, shm: 0.05f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & GreenDungeonWalls, phm: 0.1f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & EarlyBoss & Zone.Forest & !Region.Ocean & !Evil & Time.Day, phm: 0.01f, hm: 0.01f, shm: 0.01f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & EarlyBoss & Zone.Forest & !Region.Ocean & !Evil & Time.Night, phm: 0.02f, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & EarlyBoss & Depth.BelowSurface & !(Region.Ocean | Zone.Jungle | Zone.Hallow | Evil | Zone.Snow), phm: 0.09f, hm: 0.09f, shm: 0.09f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & Depth.BelowSurface & !(Region.Ocean | Zone.Jungle | Zone.Hallow | Evil | Zone.Snow), phm: 0.05f, hm: 0.05f, shm: 0.05f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & Zone.Dungeon, phm: 0.1f, hm: 0.05f, shm: 0.17f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & Zone.Lihzahrd, hm: 0.2f, shm: 0.2f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & Depth.Surface & !(Region.Ocean | Zone.Jungle | Evil), shm: 0.13f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & !(Region.Ocean | Zone.Jungle | Evil | Depth.Underworld), shm: 0.02f, stop: true);
            Spawn("FirebombHollow", HollowQuiet & Zone.Desert & !Region.Ocean, shm: 0.15f, stop: true);

            // The old rolls were one in 6 (pre-Hardmode underworld), one in 5 (Hardmode underworld), one in 20 (evil biomes in
            // Hardmode, the Super Hardmode underworld), folded into the weights.
            Spawn("FireLurker", Invasion.None & Water.Dry & Depth.Underworld, phm: 0.167f, hm: 0.2f, shm: 0.05f);
            Spawn("FireLurker", Invasion.None & Water.Dry & Evil, hm: 0.05f);

            // The graveyard catacomb override is deliberately huge (weight 3): inside those walls it is nearly all that spawns.
            // FrozenGigasStatue

            Spawn("GhostOfAHollowWarrior", Invasion.None & !Zone.Snow & Town.AtMostOne & Water.Dry & !Evil & !Zone.Glowshroom & Zone.Graveyard & CatacombWalls, phm: 3f);

            // Super Hardmode only. The old lines were already in descending order, so the highest weight wins as before.
            Spawn("GhostOfTheDarkmoonKnight", Zone.Dungeon & Region.OutsideSpan(0.6f, 0.8f), shm: 0.6f);
            Spawn("GhostOfTheDarkmoonKnight", Zone.Dungeon & Depth.Underworld, shm: 0.5f);
            Spawn("GhostOfTheDarkmoonKnight", Zone.Dungeon & Time.BloodMoon, shm: 0.4f);
            Spawn("GhostOfTheDarkmoonKnight", Zone.Dungeon, shm: 0.2f);
            Spawn("GhostOfTheDarkmoonKnight", Zone.Forest & Time.Night, shm: 0.3f);
            Spawn("GhostOfTheDarkmoonKnight", Zone.Hallow, shm: 0.1f);

            // The catacomb rate is also huge (2.5). The Machine Temple rate lives in GlobalNPC.EditSpawnPool.
            Spawn("GhostOfTheDrowned", Zone.Graveyard & CatacombWalls, phm: 2.5f, hm: 2.5f, shm: 2.5f);
            Spawn("GhostOfTheDrowned", Water.Wet & Zone.NormalUnderground, hm: 0.15f, shm: 0.15f);

            // The old Super Hardmode dungeon lines (0.05 and 0.1) sat behind the Hardmode dungeon lines and never fired; they are now
            // that stage's weight on the dungeon line.
            Spawn("GhostOfTheForgottenKnight", Downed.Skeletron & Zone.Dungeon, phm: 0.16f, stop: true);
            Spawn("GhostOfTheForgottenKnight", Zone.Dungeon, hm: 0.1f, shm: 0.05f, stop: true);
            Spawn("GhostOfTheForgottenKnight", Downed.Skeletron & Zone.Graveyard, phm: 0.2f, hm: 0.2f, shm: 0.2f);

            Spawn("GhostOfTheForgottenWarrior", Downed.Skeletron & Zone.Dungeon, phm: 0.25f, stop: true);
            Spawn("GhostOfTheForgottenWarrior", Zone.Dungeon, hm: 0.12f, shm: 0.1f, stop: true);
            Spawn("GhostOfTheForgottenWarrior", Zone.Graveyard, phm: 0.2f, hm: 0.2f, shm: 0.2f);

            // Gigas

            // Super Hardmode only, and never in the server's Adventure Mode. The random gates (one in 450 to 650) are folded in.
            Spawn("GreatBlackKnight", Mode.Sandbox & Town.AtMostOne & !Zone.Meteor & !Zone.Dungeon & !Evil & Depth.Surface & Time.Night, shm: 0.0015f);
            Spawn("GreatBlackKnight", Mode.Sandbox & Town.AtMostOne & Zone.Dungeon, shm: 0.0022f);
            Spawn("GreatBlackKnight", Mode.Sandbox & Town.AtMostOne & !Evil & !Zone.Beach, shm: 0.0017f);
            Spawn("GreatBlackKnight", Mode.Sandbox & Town.AtMostOne & Depth.Underworld, shm: 0.002f);

            // Super Hardmode only, corruption only, in the old order. The old water line (0.025) sat behind the plain corruption line
            // (0.25) and never fired; it is now written before it.
            Spawn("GuardianCorruptor", Zone.Corruption & Depth.Surface & Time.Night, shm: 0.5f, stop: true);
            Spawn("GuardianCorruptor", Zone.Corruption & Depth.BelowSurface, shm: 0.375f, stop: true);
            Spawn("GuardianCorruptor", Zone.Corruption & Water.Wet, shm: 0.025f, stop: true);
            Spawn("GuardianCorruptor", Zone.Corruption, shm: 0.25f, stop: true);

            // HeroofLumelia

            // The old rule is a long list of early returns, so every line here decides (stop) and they are in the old order. The
            // Super Hardmode overworld line (0.25) used to sit behind the broad 0.23 line and never fired; it is now written before it.
            Spawn("HollowSoldier", HollowQuietNoSnow & SpawnTile.Is(TileID.GreenDungeonBrick), phm: 0.12f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & GreenDungeonWalls, phm: 0.12f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.Lihzahrd, hm: 0.2f, shm: 0.2f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.NormalCaverns, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.Desert & Depth.Surface, hm: 0.05f, shm: 0.05f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.UndergroundDesert, hm: 0.07f, shm: 0.07f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & SpawnTile.Is(TileID.BlueDungeonBrick), hm: 0.18f, shm: 0.18f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & SpawnTile.Is(TileID.TungstenBrick), hm: 0.15f, shm: 0.15f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Depth.Surface & !(Region.Ocean | Zone.Jungle | Evil), shm: 0.25f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & !(Region.Ocean | Zone.Jungle | Evil | Depth.Underworld), shm: 0.23f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.Desert, shm: 0.13f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Zone.Dungeon & !Depth.Underworld, shm: 0.16f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & Mode.Expert & Time.BloodMoon & MidBoss, phm: 0.03f, hm: 0.03f, shm: 0.03f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & MidBoss & Depth.Surface & Time.Day & !(Region.Ocean | Evil), phm: 0.035f, hm: 0.035f, shm: 0.035f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & MidBoss & Depth.Surface & Time.Night & !(Region.Ocean | Evil), phm: 0.075f, hm: 0.075f, shm: 0.075f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & MidBoss & Depth.BelowSurface & !(Zone.Jungle | Evil), phm: 0.07f, hm: 0.07f, shm: 0.07f, stop: true);
            Spawn("HollowSoldier", HollowQuietNoSnow & (Downed.EaterOrBrain | (Downed.Skeletron & !(Region.Ocean | Evil))), phm: 0.025f, hm: 0.025f, shm: 0.025f, stop: true);

            // Same early-return list as the Soldier, so every line decides (stop) in the old order. Their own limits add no evil
            // biomes. The Super Hardmode overworld line (0.13) used to sit behind the broad 0.115 line and never fired; it is now
            // written before it. The old sky exclusion, which only covered the very top rows of the map, is not carried over.
            Spawn("HollowSpearman", HollowCleanLand & SpawnTile.Is(TileID.GreenDungeonBrick), phm: 0.07f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & GreenDungeonWalls, phm: 0.05f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.Lihzahrd, hm: 0.05f, shm: 0.05f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.NormalCaverns, hm: 0.01f, shm: 0.01f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.Desert & Depth.Surface, hm: 0.025f, shm: 0.025f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.UndergroundDesert, hm: 0.035f, shm: 0.035f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & SpawnTile.Is(TileID.BlueDungeonBrick), hm: 0.09f, shm: 0.09f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & SpawnTile.Is(TileID.TungstenBrick), hm: 0.08f, shm: 0.08f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Depth.Surface & !(Region.Ocean | Zone.Jungle), shm: 0.13f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & !(Region.Ocean | Zone.Jungle | Depth.Underworld), shm: 0.115f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.Desert & !Region.Ocean, shm: 0.15f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Zone.Dungeon & !Depth.Underworld, shm: 0.08f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Mode.Expert & Time.BloodMoon & Depth.Surface & !Region.Ocean, phm: 0.04f, hm: 0.04f, shm: 0.04f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & Mode.Expert & Time.BloodMoon & !(Region.Ocean | Zone.Jungle | Zone.Hallow | Depth.Sky | Depth.Underworld), phm: 0.02f, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("HollowSpearman", HollowCleanLand & (Mode.Expert | EarlyBoss) & Depth.BelowSurface & !(Region.Ocean | Zone.Jungle | Zone.Hallow), phm: 0.0425f, hm: 0.0425f, shm: 0.0425f, stop: true);

            // Same list as the Spearman (Lihzahrd temple 0.1 instead of 0.05) plus three surface lines and a last catch-all.
            Spawn("HollowWarrior", HollowCleanLand & SpawnTile.Is(TileID.GreenDungeonBrick), phm: 0.07f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & GreenDungeonWalls, phm: 0.05f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.Lihzahrd, hm: 0.1f, shm: 0.1f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.NormalCaverns, hm: 0.01f, shm: 0.01f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.Desert & Depth.Surface, hm: 0.025f, shm: 0.025f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.UndergroundDesert, hm: 0.035f, shm: 0.035f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & SpawnTile.Is(TileID.BlueDungeonBrick), hm: 0.09f, shm: 0.09f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & SpawnTile.Is(TileID.TungstenBrick), hm: 0.08f, shm: 0.08f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Depth.Surface & !(Region.Ocean | Zone.Jungle), shm: 0.13f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & !(Region.Ocean | Zone.Jungle | Depth.Underworld), shm: 0.115f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.Desert & !Region.Ocean, shm: 0.15f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Zone.Dungeon & !Depth.Underworld, shm: 0.08f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Mode.Expert & Time.BloodMoon & Depth.Surface & !Region.Ocean, phm: 0.04f, hm: 0.04f, shm: 0.04f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & Mode.Expert & Time.BloodMoon & !(Region.Ocean | Zone.Jungle | Zone.Hallow | Depth.Sky | Depth.Underworld), phm: 0.02f, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & (Mode.Expert | EarlyBoss) & Depth.Surface & Time.Day & !(Region.Ocean | Zone.Jungle | Zone.Hallow | Zone.Beach), phm: 0.025f, hm: 0.025f, shm: 0.025f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & (Mode.Expert | EarlyBoss) & Depth.Surface & Time.Night, phm: 0.05f, hm: 0.05f, shm: 0.05f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & (Mode.Expert | EarlyBoss) & Depth.BelowSurface & !(Region.Ocean | Zone.Jungle | Zone.Hallow), phm: 0.0425f, hm: 0.0425f, shm: 0.0425f, stop: true);
            Spawn("HollowWarrior", HollowCleanLand & (Mode.Expert | (EarlyBoss & !(Depth.Underworld | Zone.Jungle))), phm: 0.02f, hm: 0.02f, shm: 0.02f, stop: true);

            // Only in the bottom 400 rows of the map: the chasm floor, Gwyn's tomb caves, and the heart of the abyss. The two abyss
            // rooms also clear the whole pool (see SpawnOnly at the end), so there only Phantoms spawn.
            Spawn("HumanityPhantom", Depth.BottomRows & Depth.Cavern & Wall.Is(WallID.SpiderUnsafe), phm: 2f, hm: 2f, shm: 2f);
            Spawn("HumanityPhantom", Depth.BottomRows & (Depth.Cavern | Depth.Underworld) & Wall.Is(WallID.ObsidianBrickUnsafe, WallID.TitanstoneBlock), phm: 1.5f, hm: 1.5f, shm: 1.5f);
            Spawn("HumanityPhantom", Depth.BottomRows & Region.InnerThird & Wall.Is(WallID.StarlitHeavenWallpaper), phm: 10f, hm: 10f, shm: 10f);
            Spawn("HumanityPhantom", Depth.BottomRows & Mode.Remix & (Depth.Cavern | Depth.Underworld) & SpawnTile.Is(TileID.Titanstone) & Wall.Is(WallID.StarlitHeavenWallpaper), phm: 10f, hm: 10f, shm: 10f);

            // Hydra

            // Super Hardmode only; the gate of one in 1500 is folded in. The old test compared the player's pixel position with tile
            // rows, which put it in the sky; the cavern, between the rock layer and the underworld, is what it was written for.
            Spawn("HydrisElemental", Water.Dry & Evil & !Zone.Dungeon & Depth.Cavern, shm: 0.00067f);

            // Super Hardmode only; gates (one in 6 to one in 60) are folded in as 1/N. Underground and caverns, or the bottom fifth of
            // the map. The old Bone Block dungeon line returned 0 by mistake (its gate is the same one in 20 as its neighbours), so it
            // never spawned anything; it now spawns like them.
            Spawn("HydrisNecromancer", Depth.BelowSurface & Zone.Hallow, shm: 0.05f);
            Spawn("HydrisNecromancer", Depth.BelowSurface & Zone.Glowshroom, shm: 0.05f);
            Spawn("HydrisNecromancer", Depth.BelowSurface & Zone.UndergroundDesert, shm: 0.05f);
            Spawn("HydrisNecromancer", Depth.BelowSurface & Zone.Hallow & Time.BloodMoon, shm: 0.167f);
            Spawn("HydrisNecromancer", Depth.BelowSurface & Region.OutsideSpan(0.35f, 0.75f), shm: 0.05f);
            Spawn("HydrisNecromancer", Depth.BelowSurface & Zone.Dungeon & SpawnTile.Is(TileID.BoneBlock), shm: 0.05f);
            Spawn("HydrisNecromancer", !Depth.BelowSurface & Depth.BottomFifth, shm: 0.0167f);
            Spawn("HydrisNecromancer", !Depth.BelowSurface & Depth.BottomFifth & Region.OutsideSpan(0.35f, 0.75f), shm: 0.033f);

            // IceGigas

            // Super Hardmode only. The old rule returned the snow line (0.1) first, which hid the richer rock-layer line (0.2) in the
            // snowy east; the highest weight wins here, so both work. The east is the map columns past 70% of the width.
            Spawn("IceSkeleton", Zone.Snow, shm: 0.1f);
            Spawn("IceSkeleton", Region.OutsideSpan(0f, 0.7f) & Depth.Underground, shm: 0.134f);
            Spawn("IceSkeleton", Region.OutsideSpan(0f, 0.7f) & Depth.Cavern, shm: 0.2f);

            // High weight because the terrain test is strict. Alive cap of two, as in the old rule.
            Spawn("JungleSentree", Zone.Jungle & Terrain.JungleGrassNook, phm: 0.6f, hm: 0.6f, shm: 0.6f, max: 2);

            // Dungeon-jungle lines wait for the other dungeon mini-bosses and the Dungeon Mage.
            Spawn("JungleWyvernJuvenileHead", Downed.Skeletron & Zone.Jungle & Zone.Dungeon & NoDungeonMiniBossOrMage, phm: 0.05f, hm: 0.05f, shm: 0.05f);
            Spawn("JungleWyvernJuvenileHead", Water.Wet & Region.WestCoast, hm: 0.04f, shm: 0.04f, max: 2);

            // Rare, Super Hardmode only. Used to be a pool.Add in tsorcRevampGlobalNPC.EditSpawnPool.
            Spawn("KnightOfGwyn", Zone.Dungeon, shm: 0.01f);

            // Daytime grass with open air above, away from the evil, desert, jungle and meteor biomes. One alive at a time.
            Spawn("LivingShroomThief", Time.Day & Water.Dry & Terrain.OnGrass & Terrain.WallFreeAbove & !(Evil | Zone.Desert | Zone.Jungle | Zone.Meteor), phm: 0.17f, hm: 0.17f, shm: 0.17f, max: 1);

            Spawn("Locust", Zone.Crimson & Water.Dry, shm: 0.2f, max: Locust.PopulationCap);

            // Super Hardmode plain ground, the dungeon, and (very rarely) plain ground once Skeletron is down. The old Hardmode
            // dungeon line (0.005) sat behind the plain dungeon line (0.001) and never fired; it is now the Hardmode weight there.
            Spawn("LothricBlackKnight", DryNoMushrooms & MainlandGroundNoHallow, shm: 0.002f);
            Spawn("LothricBlackKnight", DryNoMushrooms & Zone.Dungeon, phm: 0.001f, hm: 0.005f, shm: 0.005f);
            Spawn("LothricBlackKnight", DryNoMushrooms & Downed.Skeletron & MainlandGroundNoHallow, phm: 0.00003f, hm: 0.00003f, shm: 0.00003f);

            // The dungeon line returns first in the old rule, so it stops (the Super Hardmode line would have given 0.03 there).
            // The Hardmode temple line used to be a pool.Add in tsorcRevampGlobalNPC.EditSpawnPool. The old rule had two identical
            // overworld lines (0.005, then 0.015) so the second never fired; it is read as the night rate, matching the Spear Knight.
            Spawn("LothricKnight", LothricLand & Zone.Dungeon, phm: 0.02f, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("LothricKnight", LothricLand & MainlandGroundNoHallow, shm: 0.03f);
            Spawn("LothricKnight", LothricLand & Time.BloodMoon & Downed.Skeletron & Depth.Surface, phm: 0.02f, hm: 0.02f, shm: 0.02f);
            Spawn("LothricKnight", LothricLand & Time.BloodMoon & Downed.Skeletron & MainlandGround, phm: 0.02f, hm: 0.02f, shm: 0.02f);
            Spawn("LothricKnight", LothricLand & Downed.Skeletron & Depth.Surface & Time.Day & MainlandGround, phm: 0.005f, hm: 0.005f, shm: 0.005f);
            Spawn("LothricKnight", LothricLand & Downed.Skeletron & Depth.Surface & Time.Night & MainlandGround, phm: 0.015f, hm: 0.015f, shm: 0.015f);
            Spawn("LothricKnight", LothricLand & Downed.Skeletron & MainlandGround, phm: 0.003f, hm: 0.003f, shm: 0.003f);
            Spawn("LothricKnight", SpawnTile.Is(TileID.LihzahrdBrick) & Zone.Lihzahrd, hm: 0.08f, shm: 0.08f);

            // The old Super Hardmode dungeon line (0.05) sat behind the plain dungeon line and never fired; it is now that stage's weight.
            Spawn("LothricSpearKnight", LothricLand & Zone.Dungeon, phm: 0.02f, hm: 0.02f, shm: 0.05f);
            Spawn("LothricSpearKnight", LothricLand & MainlandGroundNoHallow, shm: 0.02f);
            Spawn("LothricSpearKnight", LothricLand & Time.BloodMoon & Downed.Skeletron & Depth.Surface, phm: 0.02f, hm: 0.02f, shm: 0.02f);
            Spawn("LothricSpearKnight", LothricLand & Time.BloodMoon & Downed.Skeletron & MainlandGround, phm: 0.02f, hm: 0.02f, shm: 0.02f);
            Spawn("LothricSpearKnight", LothricLand & Downed.Skeletron & Depth.Surface & Time.Day & !Zone.Jungle, phm: 0.005f, hm: 0.005f, shm: 0.005f);
            Spawn("LothricSpearKnight", LothricLand & Downed.Skeletron & Depth.Surface & Time.Night & !Zone.Jungle, phm: 0.015f, hm: 0.015f, shm: 0.015f);
            Spawn("LothricSpearKnight", LothricLand & Downed.Skeletron & MainlandGroundNoHallow, phm: 0.003f, hm: 0.003f, shm: 0.003f);
            Spawn("LothricSpearKnight", SpawnTile.Is(TileID.LihzahrdBrick) & Zone.Lihzahrd, hm: 0.08f, shm: 0.08f);

            // The old rule gave up above tile row 1000 (added to keep it out of the sky), so the underground lines are the lower half
            // of the map. That cutoff also hid the pre-Hardmode overworld line (0.1), because the surface is at row 875 (1164
            // expanded); that line is written for the real surface with no row test.
            Spawn("ManHunter", PlainJungle & !Zone.Meteor & Depth.Surface, phm: 0.1f);
            Spawn("ManHunter", Depth.LowerHalf & PlainJungle & !Zone.Meteor & Depth.Underground, phm: 0.03f);
            Spawn("ManHunter", Depth.LowerHalf & PlainJungle & !Zone.Meteor & Depth.Cavern, phm: 0.04f);
            Spawn("ManHunter", Depth.LowerHalf & !(Zone.Meteor | Zone.Beach | Evil), hm: 0.02f, shm: 0.02f);

            // Hardmode and later, in water only; the dungeon is richer, and everything is four times as likely outside the inner third
            // of the map (measured from the world spawn).
            Spawn("ManOfWar", Water.Wet, hm: 0.25f, shm: 0.25f);
            Spawn("ManOfWar", Water.Wet & Zone.Dungeon, hm: 0.5f, shm: 0.5f);
            Spawn("ManOfWar", Water.Wet & !Region.InnerThird, hm: 1f, shm: 1f);
            Spawn("ManOfWar", Water.Wet & Zone.Dungeon & !Region.InnerThird, hm: 2f, shm: 2f);

            // Rare by design: the old rule used one-in-7030 to one-in-20005 gates, folded into the weights as 1/N. Hardmode only.
            Spawn("MarilithSpiritTwin", Zone.Jungle & Depth.Surface & Time.Night, hm: 0.00005f);
            Spawn("MarilithSpiritTwin", Zone.Jungle & Time.BloodMoon, hm: 0.00008f);
            Spawn("MarilithSpiritTwin", Depth.Underworld, hm: 0.00014f);
            Spawn("MarilithSpiritTwin", Zone.Beach & Time.BloodMoon, hm: 0.000125f);
            Spawn("MarilithSpiritTwin", Depth.Sky & Time.BloodMoon, hm: 0.000125f);

            // Super Hardmode only; the gate of one in 45 is folded in.
            Spawn("Massacre", Depth.Underworld | (Depth.Surface & Time.Night), shm: 0.0222f);

            // MindflayerKingServant
            // MindflayerServant
            // MinotaurMage

            // The old rule also excluded the very top rows of the map, which no one can reach.
            Spawn("MountedSandsprog", Invasion.None & !Zone.Snow & AnyDesert, phm: 0.05f, hm: 0.05f, shm: 0.05f);

            Spawn("MountedSandsprogMage", Invasion.None & !Zone.Snow & AnyDesert, phm: 0.05f, hm: 0.05f, shm: 0.05f);

            // One cave on the original adventure map only.
            Spawn("MushroomCreature", Mode.AdventureMap & Region.MushroomCaves, phm: 0.5f, hm: 0.5f, shm: 0.5f);

            // Was weight 1 behind a one-in-15 gate, folded into the weight. The sea line had no alive cap in the old rule; the
            // jungle line did. "Underground jungle" used the old Cavern helper, which compared pixels to tiles and in practice
            // matched almost the whole jungle above the underworld; this uses the real underground bands instead. The night-only
            // condition on the jungle line was removed in dbfcce830 (any time of day now).
            Spawn("MutantToad", Water.Wet & Region.WesternSea, hm: 0.07f, shm: 0.07f);
            Spawn("MutantToad", Zone.Jungle & Depth.BelowSurface, phm: 0.07f, hm: 0.07f, shm: 0.07f, max: 1);

            // The old gates (one in 44 to one in 3000) are folded in as 1/N. Underworld is the old by-tile bottom fifth and Caves the
            // old Underground and Cavern bands (30-60% of map height). Hardmode lines also apply in Super Hardmode.
            Spawn("Necromancer", NecromancerLand & Zone.Dungeon, phm: 0.00033f, hm: 0.01f, shm: 0.01f);
            Spawn("Necromancer", NecromancerLand & Depth.BottomFifth, phm: 0.0045f, hm: 0.0227f, shm: 0.0227f);
            Spawn("Necromancer", NecromancerLand & Region.OutsideSpan(0.35f, 0.75f) & Depth.Caves, phm: 0.0005f);
            Spawn("Necromancer", NecromancerLand & Zone.Hallow & Depth.Caves, hm: 0.0111f, shm: 0.0111f);
            Spawn("Necromancer", NecromancerLand & (Evil | Zone.UndergroundDesert) & Depth.Caves, hm: 0.01f, shm: 0.01f);

            // The tropical-ocean line comes first in the old rule, so it stops. Water on the ground bands (dirt layer included, as the
            // old comment explains, because the adventure map has little cavern water).
            Spawn("ObsidianJellyfish", Water.Wet & Region.TropicalOcean & Zone.Jungle, hm: 0.045f, shm: 0.045f, stop: true);
            Spawn("ObsidianJellyfish", Water.Wet & (Zone.Meteor | Evil) & Depth.Ground, phm: 0.35f);
            Spawn("ObsidianJellyfish", Water.Wet & (Zone.Meteor | Evil | Zone.Dungeon) & Depth.Ground, hm: 0.45f, shm: 0.45f);

            // Crimson: the Catacombs' pink tiled wall is the Cultist's richest spot (used to be a pool.Add in tsorcRevampGlobalNPC.EditSpawnPool).
            Spawn("OolacileCultist", Town.None & Zone.Crimson & (Depth.BelowSurface | (Depth.Surface & Time.Night)), phm: 0.05f, hm: 0.05f, shm: 0.05f);
            Spawn("OolacileCultist", Wall.Is(WallID.PinkDungeonTileUnsafe), phm: 0.2f, hm: 0.2f, shm: 0.2f);

            // Super Hardmode only, one alive at a time; gates (one in 8 to one in 30) are folded in as 1/N.
            Spawn("OolacileDemon", Zone.Crimson, shm: 0.05f, max: 1);
            Spawn("OolacileDemon", Zone.Desert, shm: 0.0333f, max: 1);
            Spawn("OolacileDemon", Zone.UndergroundDesert, shm: 0.05f, max: 1);
            Spawn("OolacileDemon", Depth.Underworld, shm: 0.0625f, max: 1);
            Spawn("OolacileDemon", Depth.Underworld & Time.BloodMoon, shm: 0.125f, max: 1);

            // Super Hardmode only, one alive at a time, dry; gates (one in 15 to one in 35) are folded in as 1/N. The chat message the
            // old rule broadcast on a roll now comes from the knight's OnSpawn. The first line is the pre-kill sighting in the
            // jungle sky and surface; the rest are hunts once it has been slain.
            Spawn("OolacileKnight", Water.Dry & Zone.Jungle & (Depth.Sky | Depth.Surface) & !Slain.OolacileKnight, shm: 0.05f, max: 1);
            Spawn("OolacileKnight", Water.Dry & Zone.Dungeon & Time.BloodMoon & Slain.OolacileKnight, shm: 0.0667f, max: 1);
            Spawn("OolacileKnight", Water.Dry & Zone.Meteor & Time.BloodMoon & Slain.OolacileKnight, shm: 0.04f, max: 1);
            Spawn("OolacileKnight", Water.Dry & Zone.Dungeon & Slain.OolacileKnight, shm: 0.0286f, max: 1);

            // Super Hardmode only, one alive at a time; gates (one in 8 to one in 70) are folded in as 1/N. The old "ocean" test and
            // "gray layer" test mixed tile and pixel units (the ocean covered the west 43% of the map; the gray layer ran through
            // the underworld); this uses the ocean strips and the cavern band they were written for.
            Spawn("OolacileSorcerer", Zone.Jungle & !Zone.Dungeon & !Evil & (Depth.Sky | Depth.Surface) & !Region.Ocean, shm: 0.0143f, max: 1);
            Spawn("OolacileSorcerer", Region.Ocean & !(Depth.Sky | Depth.Surface), shm: 0.04f, max: 1);
            Spawn("OolacileSorcerer", Time.BloodMoon & Zone.Jungle & !Zone.Dungeon & !Evil & !(Depth.Sky | Depth.Surface) & !Region.Ocean, shm: 0.0333f, max: 1);
            Spawn("OolacileSorcerer", Zone.Crimson, shm: 0.05f, max: 1);
            Spawn("OolacileSorcerer", Depth.Cavern, shm: 0.02f, max: 1);
            Spawn("OolacileSorcerer", Time.BloodMoon & Zone.Dungeon, shm: 0.125f, max: 1);

            Spawn("Parasprite", Zone.Hallow & Water.Dry & Downed.Skeletron & Depth.SurfaceToUpperCaves, phm: 0.15f, hm: 0.15f, shm: 0.15f);

            // Hardmode and later, in the underworld or the evil caverns; the gate of one in 200 is folded in. The old cavern test
            // compared pixel positions with tile rows; the cavern band it was written for is used.
            Spawn("ParasyticWormHead", Depth.Underworld | (Evil & Depth.Cavern), hm: 0.005f, shm: 0.005f);

            // Super Hardmode only: the jungle below the surface. The old gate of one in 25 is folded in.
            Spawn("Plaguesmith", Zone.Jungle & !Depth.Surface, shm: 0.04f);

            // QuaraClutchCrab

            // Hallow is its home now (the jungle got crowded) and it comes more often until the Rage falls; gates (one in 5 to one in 45)
            // are folded in as 1/N. Hardmode lines also apply in Super Hardmode.
            Spawn("QuaraHydromancer", Water.Dry & Zone.Hallow & Depth.Surface & Time.Night & Slain.TheRage, hm: 0.033f, shm: 0.033f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Hallow & Depth.BelowSurface & Slain.TheRage, hm: 0.033f, shm: 0.033f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Hallow & Depth.BelowSurface & !Slain.TheRage, hm: 0.1f, shm: 0.1f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Lihzahrd, hm: 0.022f, shm: 0.022f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Desert, hm: 0.022f, shm: 0.022f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Hallow, shm: 0.1f);
            Spawn("QuaraHydromancer", Water.Dry & Zone.Glowshroom, shm: 0.2f);

            // QuaraMantassin

            // Beach is its home; it is also displaced into the underground desert. Gates (one in 18 to one in 140) are folded in
            // as 1/N. No town NPCs, dungeon or Pumpkin / Frost Moon.
            Spawn("QuaraPincher", Town.None & Invasion.NoMoonEvent & !Zone.Dungeon & Zone.Beach, phm: 0.0071f, hm: 0.033f, shm: 0.0556f);
            Spawn("QuaraPincher", Town.None & Invasion.NoMoonEvent & !Zone.Dungeon & Zone.UndergroundDesert, hm: 0.0222f, shm: 0.0455f);

            // The old rule is a list of early returns, so every line stops, in the old order: in Super Hardmode an evil overworld
            // spot still gets the 0.0125 line before the 0.13 one.
            Spawn("RedCloudHunter", Zone.Dungeon, phm: 0.01f, stop: true);
            Spawn("RedCloudHunter", !Evil & !Zone.Beach & Zone.Jungle, hm: 0.02f, shm: 0.02f, stop: true);
            Spawn("RedCloudHunter", Zone.Hallow & !Zone.Dungeon, hm: 0.01f, shm: 0.01f, stop: true);
            Spawn("RedCloudHunter", Depth.Surface & (Zone.Desert | Evil | Zone.Beach | Zone.Jungle), hm: 0.0125f, shm: 0.0125f, stop: true);
            Spawn("RedCloudHunter", Zone.Lihzahrd, hm: 0.15f, shm: 0.15f, stop: true);
            Spawn("RedCloudHunter", Evil, shm: 0.13f, stop: true);
            Spawn("RedCloudHunter", Depth.Surface & (Zone.Jungle | Evil), shm: 0.1f, stop: true);
            Spawn("RedCloudHunter", AnyDesert, shm: 0.13f, stop: true);
            Spawn("RedCloudHunter", Zone.Dungeon, shm: 0.01f, stop: true);

            // The old gates (one in 300 to one in 1250) are folded in as 1/N. Hardmode lines also apply in Super Hardmode.
            Spawn("RedKnight", Zone.Dungeon & !Evil, hm: 0.00083f, shm: 0.00083f);
            Spawn("RedKnight", Zone.Meteor & !Evil & Depth.Cavern, hm: 0.0008f, shm: 0.0008f);
            Spawn("RedKnight", Zone.Dungeon, shm: 0.002f);
            Spawn("RedKnight", Depth.Underworld, hm: 0.00091f, shm: 0.0033f);

            // The lava-wall line (0.15) was meant to be the richer spot inside the underworld, but the old plain underworld line (0.1)
            // returned first and hid it; with the highest weight winning here, it now works. Hardmode adds the underground desert.
            Spawn("RingedKnight", Town.AtMostOne & Depth.Underworld, phm: 0.1f, hm: 0.1f, shm: 0.1f);
            Spawn("RingedKnight", Town.AtMostOne & Depth.Underworld & Wall.Is(WallID.LavaMossBlockWall, WallID.LavaUnsafe1, WallID.LavaUnsafe2), phm: 0.15f, hm: 0.15f, shm: 0.15f);
            Spawn("RingedKnight", Town.AtMostOne & Zone.UndergroundDesert, hm: 0.1f, shm: 0.1f);

            // Sahagin

            Spawn("Sandsprog", Invasion.None & !Zone.Snow & AnyDesert, phm: 0.04f, hm: 0.04f, shm: 0.04f);

            Spawn("SandsprogMage", Invasion.None & !Zone.Snow & AnyDesert, phm: 0.04f, hm: 0.04f, shm: 0.04f);

            // SerpentOfTheAbyssBody

            // Super Hardmode only, in the underworld, one alive at a time. Richer in the outer 30% of the map on each side, and in a
            // blood moon.
            Spawn("SerpentOfTheAbyssHead", Depth.Underworld, shm: 0.02f, max: 1);
            Spawn("SerpentOfTheAbyssHead", Depth.Underworld & Time.BloodMoon, shm: 0.067f, max: 1);
            Spawn("SerpentOfTheAbyssHead", Depth.Underworld & Region.OutsideSpan(0.3f, 0.7f), shm: 0.067f, max: 1);
            Spawn("SerpentOfTheAbyssHead", Depth.Underworld & Region.OutsideSpan(0.3f, 0.7f) & Time.BloodMoon, shm: 0.2f, max: 1);

            // Gate of one in 40 folded in as 0.025. Hardmode and later.
            Spawn("ShadowMage", Zone.Dungeon | Depth.Underworld, hm: 0.025f, shm: 0.025f);

            // Super Hardmode only, never in the dungeon; gates (one in 35 to one in 80) are folded in as 1/N. The old gray-layer test
            // mixed units and ran through the underworld; the cavern band is used.
            Spawn("SlograII", !Zone.Dungeon & Zone.Jungle & Time.Night & (Depth.Sky | Depth.Surface), shm: 0.025f);
            Spawn("SlograII", !Zone.Dungeon & Depth.Underground, shm: 0.0167f);
            Spawn("SlograII", !Zone.Dungeon & Depth.Cavern, shm: 0.0125f);
            Spawn("SlograII", !Zone.Dungeon & Zone.Jungle, shm: 0.0286f);

            // Was any spot with more than five snow tiles, read from the local client's scene. Snow zone is the server-safe equivalent.
            Spawn("SnowOwl", Zone.Snow & Water.Dry, phm: 0.2f, hm: 0.2f);

            // The old rule multiplied vanilla's cavern spawn chance (1 below the rock layer, above the underworld) by 0.15 before
            // Hardmode and 0.07 in Hardmode. Its Super Hardmode line (0.01) sat behind the Hardmode one and never fired; it is now live.
            Spawn("StoneGolem", Depth.Cavern, phm: 0.15f, hm: 0.07f, shm: 0.01f);

            // Super Hardmode only, one alive at a time; gates (one in 30 to one in 50) are folded in as 1/N. The old underworld line tested
            // a tile row against a pixel value and never fired; it is live now. The "nearby" chat message the old rule sent on a
            // roll now comes from the knight's OnSpawn.
            Spawn("TaurusKnight", AnyDesert, shm: 0.0222f, max: 1);
            Spawn("TaurusKnight", Zone.Dungeon, shm: 0.02f, max: 1);
            Spawn("TaurusKnight", Depth.Underworld, shm: 0.0333f, max: 1);

            // Super Hardmode only: sky 0.5 and meteor 0.75; night doubles it and a blood moon doubles it again.
            SpawnNightBoosted("Tetsujin", Depth.Sky, shm: 0.5f);
            SpawnNightBoosted("Tetsujin", Zone.Meteor, shm: 0.75f);

            // Another session changed this rule (uncommitted at the time of the port); this matches the working tree: locked until the
            // first boss falls, no Super Hardmode, none in meteor or evil biomes, none in the desert.
            Spawn("TibianAmazon", TibianLand & !Zone.Desert & !Zone.Dungeon & Downed.Eye & !(Zone.Meteor | Evil) & Depth.Ground, phm: 0.0542f, hm: 0.0542f);

            // Early returns in the old order; the lines never overlap, so none needs to stop. Hardmode lines also apply in Super Hardmode.
            Spawn("TibianValkyrie", TibianLand & Depth.Surface & !Zone.Crimson & Time.Night, phm: 0.0427f);
            Spawn("TibianValkyrie", TibianLand & Depth.Surface & !Zone.Crimson & Time.Day, phm: 0.038f);
            Spawn("TibianValkyrie", TibianLand & !Zone.Meteor & !Zone.Jungle & !Zone.Dungeon & !Evil & Depth.BelowSurface, phm: 0.0494f);
            Spawn("TibianValkyrie", TibianLand & !Zone.Meteor & !Zone.Jungle & Zone.Dungeon & Depth.BelowSurface, phm: 0.03857f);
            Spawn("TibianValkyrie", TibianLand & !(Zone.Meteor | Zone.Jungle | Evil) & Depth.Surface & Time.Night, hm: 0.025f, shm: 0.025f);
            Spawn("TibianValkyrie", TibianLand & !(Zone.Meteor | Zone.Jungle | Evil) & Depth.Surface & Time.Day, hm: 0.018f, shm: 0.018f);
            Spawn("TibianValkyrie", TibianLand & !(Zone.Meteor | Zone.Jungle | Evil) & Depth.BelowSurface, hm: 0.027f, shm: 0.027f);

            // Gates (one in 30 to one in 200) are folded in as 1/N. The old rule only kept the Hardmode line off the east coast;
            // now every line stays off it.
            Spawn("Tonberry", Water.Dry & !Region.FrozenOcean, hm: 0.005f, shm: 0.01f);
            Spawn("Tonberry", Water.Dry & !Region.FrozenOcean & Zone.Dungeon, shm: 0.033f);
            Spawn("Tonberry", Water.Dry & !Region.FrozenOcean & Zone.Jungle, shm: 0.0133f);

            // The old Sky lines compared pixels to tiles and could only match the very top rows of the map; this is the sky biome.
            // Cavern lines for the dungeon and crimson exceptions are merged into one.
            Spawn("UndeadCaster", Depth.Cavern & !Zone.Jungle, phm: 0.035f);
            Spawn("UndeadCaster", Depth.Sky & Time.Day, phm: 0.05f);
            Spawn("UndeadCaster", Depth.Sky & Time.Night, phm: 0.025f);
            Spawn("UndeadCaster", Zone.Snow, phm: 0.1f);

            // Super Hardmode only: an old else-if chain, so the lines decide in order.
            Spawn("VampireBat", Evil, shm: 0.125f, stop: true);
            Spawn("VampireBat", Depth.Underworld, shm: 0.0167f, stop: true);
            Spawn("VampireBat", Zone.Hallow, shm: 0.03f, stop: true);

            // Locked until the first boss is down, one alive at a time. Gates (one in 300 to one in 1200) are folded in as 1/N; the
            // bigger weights apply outside the middle 30-70% of the map's width.
            Spawn("Warlock", Downed.Eye & Depth.Cavern, phm: 0.00083f, max: 1);
            Spawn("Warlock", Downed.Eye & Depth.Cavern & Region.OutsideSpan(0.3f, 0.7f), phm: 0.00233f, max: 1);
            Spawn("Warlock", Downed.Eye & (Depth.BelowSurface | Zone.Jungle), hm: 0.00167f, shm: 0.00167f, max: 1);
            Spawn("Warlock", Downed.Eye & (Depth.BelowSurface | Zone.Jungle) & Region.OutsideSpan(0.3f, 0.7f), hm: 0.00333f, shm: 0.00333f, max: 1);

            // WaterSpirit

            // Hardmode and later; gates (one in 15 to one in 85) are folded in as 1/N. The ocean line is the west coast only.
            Spawn("Willowisp", Town.None & PlainHallow, hm: 0.0118f, shm: 0.0118f);
            Spawn("Willowisp", Town.None & PlainHallow & Depth.Underground, hm: 0.0182f, shm: 0.0182f);
            Spawn("Willowisp", Town.None & PlainHallow & Depth.Cavern, hm: 0.0333f, shm: 0.0333f);
            Spawn("Willowisp", Town.None & Region.WestCoast, hm: 0.0667f, shm: 0.0667f);

            // Exclusive zones. Written last because the later one wins where two overlap, as the old clears did.
            // Machine Temple: Hardmode and later, above the temple floor, behind the green dungeon slab wall. Wet and dry spots differ.
            SpawnOnly(Progress.Hardmode & MachineTemple & Water.Wet,
                ("GreenJellyfish", 10f), ("MutantToad", 2f), ("GhostOfTheDrowned", 2f));
            SpawnOnly(Progress.Hardmode & MachineTemple & Water.Dry,
                ("GhostOfTheDrowned", 3f), ("MutantToad", 1f));

            // Humanity Phantom rooms: only Phantoms spawn. The Starlit Heaven wallpaper room, and the Remix Map's obsidian vault.
            SpawnOnly(Wall.AtPlayer(WallID.StarlitHeavenWallpaper), ("HumanityPhantom", 10f));
            SpawnOnly(Mode.Remix & Region.RemixVaultBox & Wall.AtPlayer(WallID.ObsidianBrickUnsafe), ("HumanityPhantom", 10f));
        }
    }

    /// <summary>/spawnpool: what the spawn registry would put in the pool at the player's position, with each enemy's share.</summary>
    public class SpawnPoolCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "spawnpool";

        public override string Description => "List the registry enemies that can spawn at your position and their share of attempts";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!EnemySpawns.Enabled)
            {
                caller.Reply("New Enemy Spawns is off in the gameplay config; the registry is not in use.", Color.Orange);
                return;
            }

            Player player = caller.Player;
            NPCSpawnInfo spawnInfo = new NPCSpawnInfo();
            spawnInfo.Player = player;
            spawnInfo.SpawnTileX = (int)(player.Center.X / 16f);
            spawnInfo.SpawnTileY = (int)(player.Bottom.Y / 16f);
            spawnInfo.Water = player.wet;

            List<(string Name, float Weight)> found = EnemySpawns.Snapshot(spawnInfo);
            found.Sort((first, second) => second.Weight.CompareTo(first.Weight));

            string[] stageNames = { "pre-Hardmode", "Hardmode", "Super Hardmode" };
            caller.Reply($"Spawn registry at your position ({stageNames[EnemySpawns.CurrentStageIndex()]}):", Color.Cyan);

            // Vanilla's own pick is one lump of weight 1.0; shares ignore enemies that are not in the registry.
            float total = 1f;

            foreach ((string Name, float Weight) entry in found)
            {
                total += entry.Weight;
            }

            foreach ((string Name, float Weight) entry in found)
            {
                caller.Reply($"  {entry.Name}: weight {entry.Weight:0.###}, {entry.Weight / total * 100f:0.#}% of attempts", Color.White);
            }

            caller.Reply($"  vanilla pick: weight 1, {1f / total * 100f:0.#}% (legacy mod enemies not in the registry are not counted)", Color.Gray);
        }
    }
}
