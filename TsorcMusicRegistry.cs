using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    public enum MusicWorldScope
    {
        Any,
        Adventure,
        Remix
    }

    public sealed class TsorcMusicRegistry : ModSceneEffect
    {
        private enum RuleKind
        {
            Biome,
            Area,
            Event
        }

        private sealed class TrackAssignment
        {
            public readonly string MainTrack;
            public readonly string RemixTrack;
            public readonly int Fallback;
            public readonly SceneEffectPriority Priority;

            public TrackAssignment(string mainTrack, string remixTrack, int fallback, SceneEffectPriority priority)
            {
                MainTrack = mainTrack;
                RemixTrack = remixTrack;
                Fallback = fallback;
                Priority = priority;
            }
        }

        private sealed class ConditionalRule
        {
            public readonly Func<Player, bool> Condition;
            public readonly TrackAssignment Assignment;
            public readonly MusicWorldScope Scope;
            public readonly RuleKind Kind;

            public ConditionalRule(Func<Player, bool> condition, TrackAssignment assignment, MusicWorldScope scope, RuleKind kind)
            {
                Condition = condition;
                Assignment = assignment;
                Scope = scope;
                Kind = kind;
            }
        }

        private static readonly Dictionary<int, TrackAssignment> Assignments = new();
        private static readonly List<ConditionalRule> ConditionalRules = new();
        private static readonly Rectangle ForgottenCityEntrance = new Rectangle(4340, 1335, 560, 155);
        private static readonly Rectangle ForgottenCityDistrict = new Rectangle(4080, 1450, 1140, 375);
        // Native 2400-tile map coordinates from the reviewed 1.5.07 outline.
        // Keep the chasm, Fire Temple, arena above and eastern vertical cavern outside these bounds.
        private static readonly Rectangle ArtoriasWestRuins = new Rectangle(4245, 2008, 587, 222);
        private static readonly Rectangle ArtoriasEastRuins = new Rectangle(4862, 2000, 1283, 220);
        private static readonly Point[] ArtoriasEastCeiling =
        {
            new Point(4862, 2039),
            new Point(4886, 2030),
            new Point(4910, 2029),
            new Point(4918, 2023),
            new Point(4934, 2023),
            new Point(4958, 2028),
            new Point(4966, 2025),
            new Point(5030, 2025),
            new Point(5046, 2028),
            new Point(5054, 2034),
            new Point(5062, 2037),
            new Point(5070, 2035),
            new Point(5094, 2035),
            new Point(5110, 2053),
            new Point(5118, 2053),
            new Point(5126, 2052),
            new Point(5134, 2045),
            new Point(5158, 2042),
            new Point(5174, 2043),
            new Point(5182, 2038),
            new Point(5198, 2036),
            new Point(5206, 2027),
            new Point(5214, 2027),
            new Point(5222, 2028),
            new Point(5230, 2034),
            new Point(5262, 2034),
            new Point(5270, 2044),
            new Point(5278, 2040),
            new Point(5286, 2039),
            new Point(5318, 2039),
            new Point(5326, 2045),
            new Point(5350, 2036),
            new Point(5382, 2037),
            new Point(5398, 2035),
            new Point(5414, 2037),
            new Point(5446, 2035),
            new Point(5478, 2043),
            new Point(5486, 2040),
            new Point(5494, 2040),
            new Point(5502, 2045),
            new Point(5510, 2043),
            new Point(5550, 2045),
            new Point(5582, 2051),
            new Point(5598, 2052),
            new Point(5614, 2051),
            new Point(5630, 2047),
            new Point(5638, 2047),
            new Point(5646, 2051),
            new Point(5654, 2048),
            new Point(5662, 2042),
            new Point(5694, 2031),
            new Point(5710, 2031),
            new Point(5734, 2028),
            new Point(5758, 2029),
            new Point(5782, 2024),
            new Point(5790, 2020),
            new Point(5822, 2020),
            new Point(5830, 2022),
            new Point(5846, 2033),
            new Point(5894, 2027),
            new Point(5950, 2034),
            new Point(5974, 2042),
            new Point(5998, 2043),
            new Point(6006, 2046),
            new Point(6014, 2046),
            new Point(6030, 2058),
            new Point(6038, 2059),
            new Point(6062, 2060),
            new Point(6078, 2050),
            new Point(6086, 2047),
            new Point(6110, 2047),
            new Point(6118, 2058),
            new Point(6126, 2092),
            new Point(6142, 2094),
            new Point(6145, 2144)
        };
        private static ulong cachedTick = ulong.MaxValue;
        private static TrackAssignment cachedAssignment;
        private static int cachedMusic = -1;

        public override int Music
        {
            get
            {
                RefreshSelection();
                return cachedMusic;
            }
        }

        public override SceneEffectPriority Priority
        {
            get
            {
                RefreshSelection();
                return cachedAssignment?.Priority ?? SceneEffectPriority.None;
            }
        }

        public override bool IsSceneEffectActive(Player player)
        {
            RefreshSelection();
            return cachedAssignment != null;
        }

        internal static void Populate()
        {
            Assignments.Clear();
            ConditionalRules.Clear();

            RegisterAreaMusic(player => !Main.hardMode && Main.dayTime && IsInForgottenCity(player),
                mainTrack: "ForgottenCity", fallback: MusicID.Dungeon, scope: MusicWorldScope.Adventure);
            RegisterAreaMusic(player => !Main.hardMode && !Main.dayTime && IsInForgottenCity(player),
                mainTrack: "GreatUndergroundRivers", fallback: MusicID.Dungeon, scope: MusicWorldScope.Adventure);
            RegisterAreaMusic(player => !Main.hardMode && IsInForgottenCity(player),
                mainTrack: "ForgottenCity", remixTrack: "ForgottenCity", fallback: MusicID.Dungeon, scope: MusicWorldScope.Remix);
            RegisterAreaMusic(player => Main.hardMode && IsInForgottenCity(player),
                remixTrack: "HardmodeCity", scope: MusicWorldScope.Remix);
            RegisterAreaMusic(player =>
            {
                if (!tsorcRevampWorld.SuperHardMode || !player.ZoneGlowshroom
                    || player.ZoneOverworldHeight || player.ZoneSkyHeight)
                {
                    return false;
                }

                // Leave vertical expansion open below the surface. This X band encloses Mushroom
                // Caverns while the height flags exclude the separate mountain mushrooms above.
                int playerTileX = player.Center.ToTileCoordinates().X;
                return playerTileX >= 3300 && playerTileX < 4200;
            }, mainTrack: "Pillars", priority: SceneEffectPriority.Environment, scope: MusicWorldScope.Adventure);
            RegisterAreaMusic(IsInArtoriasRuins, mainTrack: "Witchlands",
                priority: SceneEffectPriority.Environment, scope: MusicWorldScope.Adventure);

            // Every invader gets a pack-specific default automatically. Explicit entries
            // below are registered afterwards and therefore replace this fallback.
            //
            // ArtoriasPhantom is excluded: the NPC-encounter scan below has no priority tiebreak
            // between two active matches — whichever active NPC it finds LAST in Main.npc[] simply
            // overwrites cachedAssignment. The phantom is a companion spawned WHILE Artorias himself
            // is still alive and active, so if it had its own (generic) entry here, the fight's music
            // would flip to this default every time the phantom was out and only return to Artorias's
            // Boss6 track (registered below) once it faded, instead of playing Boss6 the whole fight.
            foreach (ModNPC npc in ModContent.GetContent<ModNPC>())
            {
                if (npc is NPCs.Puppets.PuppetNPC && npc is not NPCs.Enemies.SuperHardMode.CrystalKnight
                    && npc is not NPCs.Enemies.SuperHardMode.DarkrootSentry
                    && npc is not NPCs.Bosses.SuperHardMode.ArtoriasPhantom)
                {
                    Assignments[npc.Type] = new TrackAssignment(
                        Path("SlugBattle"),
                        Path("Invader"),
                        MusicID.Boss1,
                        SceneEffectPriority.BossMedium);
                }
            }

            // Hemoglobin Shark (vanilla NPCID.GoblinShark) - rare, tough Blood Moon ocean miniboss.
            // The only vanilla-NPC entry here, so it's assigned directly rather than via Register(),
            // which looks NPCs up by name in this mod's own ModNPC registry.
            Assignments[NPCID.GoblinShark] = new TrackAssignment(Path("Invader"), Path("Invader"), MusicID.Boss1, SceneEffectPriority.BossLow);

            Register("Pinwheel", "Boss18", "Boss18", MusicID.Boss2);
            Register("TheMachine", "Boss11", "Boss11", MusicID.Boss2);
            // tsorcMusic assigns this exact track to vanilla Skeletron (NPCID.SkeletronHead).
            // Keep the lookup here so the music pack remains optional and does not need an edit.
            Register("GravelordNito", "Sandstorm", null, MusicID.Boss1, SceneEffectPriority.BossHigh);
            Register("VesselOfSouls", "Boss15", "Boss15", MusicID.Boss1);
            Register("RedKnight", "Sandstorm", "Event", MusicID.Boss1, SceneEffectPriority.BossLow);
            Register("GreatRedKnight", "Boss13", "Boss13", MusicID.Boss1);
            Register("BlackKnight", "Boss15", null, MusicID.Boss1);
            Register("LeonhardPhase1", "Invader", "Invader", MusicID.Boss1);
            Register("Blaidd", "Boss4", "Invader", MusicID.Boss1);
            Register("Gigas", "GoblinInvasion", "GoblinInvasion", MusicID.GoblinInvasion, SceneEffectPriority.BossLow);
            Register("IceGigas", "Boss12", "Boss12", MusicID.Boss2);
            Register("LothricBlackKnight", "Invader", null, MusicID.Boss1);
            RegisterGroup(new[] { "OolacileDemon", "AncientDemon", "DefiledDemon" }, "Boss15", "Boss15", MusicID.Boss2);
            Register("HeroofLumelia", "Boss2", "Boss2", MusicID.Boss2);
            Register("OwlFather", "Boss19", "Boss19", MusicID.Boss2);
            Register("JungleWyvernHead", "Boss10", "Boss10", MusicID.Boss2);
            Register("TheRage", "Boss7", "Boss7", MusicID.Boss2);
            Register("TheSorrow", "Boss12", "Boss12", MusicID.Boss2);
            Register("TheHunter", "Boss10", "Boss22", MusicID.Boss2);
            RegisterGroup(new[] { "Cataluminance", "RetinazerV2", "SpazmatismV2" }, "Boss8", "Boss8", MusicID.Boss2);
            Register("Artorias", "Boss6", "Boss6", MusicID.Boss2);
            Register("Witchking", "Boss14", "Boss14", MusicID.Boss2);
            Register("Blight", "Pillars", "Boss19", MusicID.Boss2);
            RegisterGroup(new[] { "WyvernMage", "MechaDragonHead" }, "Boss12", "Boss12", MusicID.Boss2);
            RegisterGroup(new[] { "Gaibon", "Slogra" }, "Boss7", "Boss7", MusicID.Boss2);
            RegisterGroup(new[] { "SerrisHead", "SerrisX" }, "Boss19", "Boss19", MusicID.Boss2);
            RegisterGroup(new[] { "EarthFiendLich", "LichKingSerpentHead" }, "Boss16", "Boss4", MusicID.Boss2);
            Register("FireFiendMarilith", "Boss16", "Boss16", MusicID.Boss2);
            Register("WaterFiendKraken", "Boss12", "Boss21", MusicID.Boss2);
            Register("Chaos", "Boss14", "Boss19", MusicID.Boss2);
            Register("DarkCloud", "Boss11", "Boss20", MusicID.Boss2);
            RegisterGroup(new[] { "Death", "TrueDeath" }, "Boss13", "Boss13", MusicID.Boss2);
            RegisterGroup(new[] { "WyvernMageShadow", "GhostDragonHead", "HellkiteDragonHead" }, "God-DevouringSerpent", "God-DevouringSerpent", MusicID.Boss2);
            RegisterGroup(new[] { "SeathTheScalelessHead", "PrimordialCrystal" }, "Boss9", "Boss9", MusicID.Boss2);
            Register("GrandOccultist", "Boss14", "Boss14", MusicID.Boss3);
            Register("Gwyn", "Gwyn", "Gwyn", MusicID.Boss2);
            RegisterGroup(new[] { "DarkShogunMask", "DarkDragonMask", "Okiku", "BrokenOkiku" }, "Boss5", "Boss5", MusicID.Boss2, SceneEffectPriority.BossHigh);
            RegisterGroup(new[] { "AttraidiesMimic", "Attraidies" }, "Boss8", "Boss8", MusicID.Boss2);
        }

        internal static void Clear()
        {
            Assignments.Clear();
            ConditionalRules.Clear();
            cachedAssignment = null;
            cachedMusic = -1;
            cachedTick = ulong.MaxValue;
        }

        public static bool HasActiveOverride()
        {
            RefreshSelection();
            return cachedAssignment != null;
        }

        public static void RegisterBiomeMusic(Func<Player, bool> condition, string mainTrack = null, string remixTrack = null,
            int fallback = -1, SceneEffectPriority priority = SceneEffectPriority.BiomeMedium, MusicWorldScope scope = MusicWorldScope.Any)
        {
            RegisterConditionalMusic(condition, mainTrack, remixTrack, fallback, priority, scope, RuleKind.Biome);
        }

        public static void RegisterAreaMusic(Func<Player, bool> condition, string mainTrack = null, string remixTrack = null,
            int fallback = -1, SceneEffectPriority priority = SceneEffectPriority.BiomeHigh, MusicWorldScope scope = MusicWorldScope.Any)
        {
            RegisterConditionalMusic(condition, mainTrack, remixTrack, fallback, priority, scope, RuleKind.Area);
        }

        public static void RegisterEventMusic(Func<Player, bool> condition, string mainTrack = null, string remixTrack = null,
            int fallback = -1, SceneEffectPriority priority = SceneEffectPriority.Event, MusicWorldScope scope = MusicWorldScope.Any)
        {
            RegisterConditionalMusic(condition, mainTrack, remixTrack, fallback, priority, scope, RuleKind.Event);
        }

        public static bool IsInLegacyAdventureArea(Player player, Rectangle legacyTileArea)
        {
            Vector2 topLeft = ExpandedWorldTransform.MapWorld(legacyTileArea.TopLeft() * 16f) / 16f;
            Vector2 bottomRight = ExpandedWorldTransform.MapWorld(legacyTileArea.BottomRight() * 16f) / 16f;
            Rectangle currentTileArea = new Rectangle(
                (int)topLeft.X,
                (int)topLeft.Y,
                (int)(bottomRight.X - topLeft.X),
                (int)(bottomRight.Y - topLeft.Y));
            return currentTileArea.Contains(player.Center.ToTileCoordinates());
        }

        public static bool IsInRemixArea(Player player, Rectangle remixTileArea)
        {
            return remixTileArea.Contains(player.Center.ToTileCoordinates());
        }

        private static bool IsInForgottenCity(Player player)
        {
            // The city's lower streets moved with the rest of the district. Use the entrance's
            // tile offset for both bounds so the underworld fold does not stretch the music area.
            Point entrance = ForgottenCityEntrance.Location;
            Point mappedEntrance = ExpandedWorldTransform.MapTile(entrance.X, entrance.Y);
            Point cityOffset = mappedEntrance - entrance;
            Point playerTile = player.Center.ToTileCoordinates() - cityOffset;

            return ForgottenCityEntrance.Contains(playerTile) || ForgottenCityDistrict.Contains(playerTile);
        }

        private static bool IsInArtoriasRuins(Player player)
        {
            if (!tsorcRevampWorld.ExpandedAdventure)
            {
                return false;
            }

            Point playerTile = player.Center.ToTileCoordinates();

            if (ArtoriasWestRuins.Contains(playerTile))
            {
                return true;
            }

            if (!ArtoriasEastRuins.Contains(playerTile))
            {
                return false;
            }

            // Follow the fixed roof outline across the eastern district. Interpolating tile Y
            // between authored points excludes the arena and tree cavern above the ruins.
            for (int i = 1; i < ArtoriasEastCeiling.Length; i++)
            {
                Point previous = ArtoriasEastCeiling[i - 1];
                Point next = ArtoriasEastCeiling[i];

                if (playerTile.X <= next.X)
                {
                    float segmentProgress = (playerTile.X - previous.X) / (float)(next.X - previous.X);
                    float ceilingY = MathHelper.Lerp(previous.Y, next.Y, segmentProgress);
                    return playerTile.Y >= ceilingY;
                }
            }

            return false;
        }

        private static void RegisterConditionalMusic(Func<Player, bool> condition, string mainTrack, string remixTrack,
            int fallback, SceneEffectPriority priority, MusicWorldScope scope, RuleKind kind)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            ConditionalRules.Add(new ConditionalRule(
                condition,
                new TrackAssignment(Path(mainTrack), Path(remixTrack), fallback, priority),
                scope,
                kind));
            cachedTick = ulong.MaxValue;
        }

        private static void Register(string npcName, string mainTrack, string remixTrack, int fallback, SceneEffectPriority priority = SceneEffectPriority.BossMedium)
        {
            if (ModContent.GetInstance<tsorcRevamp>().TryFind(npcName, out ModNPC npc))
            {
                Assignments[npc.Type] = new TrackAssignment(Path(mainTrack), Path(remixTrack), fallback, priority);
            }
        }

        private static void RegisterGroup(IEnumerable<string> npcNames, string mainTrack, string remixTrack, int fallback, SceneEffectPriority priority = SceneEffectPriority.BossMedium)
        {
            foreach (string npcName in npcNames)
            {
                Register(npcName, mainTrack, remixTrack, fallback, priority);
            }
        }

        private static string Path(string track) => track == null ? null : "Sounds/Music/" + track;

        private static void RefreshSelection()
        {
            ulong tick = (ulong)Main.GameUpdateCount;
            if (cachedTick == tick)
            {
                return;
            }

            cachedTick = tick;
            cachedAssignment = null;
            cachedMusic = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && Assignments.TryGetValue(npc.type, out TrackAssignment assignment))
                {
                    cachedAssignment = assignment;
                }
            }

            // NPC encounters always win. Otherwise event > area > biome, and later
            // registrations replace earlier registrations within the same category.
            if (cachedAssignment == null && Main.LocalPlayer.active)
            {
                RuleKind? selectedKind = null;
                foreach (ConditionalRule rule in ConditionalRules)
                {
                    if (ScopeMatches(rule.Scope) && rule.Condition(Main.LocalPlayer) &&
                        (!selectedKind.HasValue || (int)rule.Kind >= (int)selectedKind.Value))
                    {
                        cachedAssignment = rule.Assignment;
                        selectedKind = rule.Kind;
                    }
                }
            }

            if (cachedAssignment == null)
            {
                return;
            }

            if (tsorcRevampWorld.RemixMap && TryResolve("tsorcXelvaaMusic", cachedAssignment.RemixTrack, out cachedMusic))
            {
                return;
            }

            if (TryResolve("tsorcMusic", cachedAssignment.MainTrack, out cachedMusic))
            {
                return;
            }

            cachedMusic = cachedAssignment.Fallback;
            if (cachedMusic < 0)
            {
                cachedAssignment = null;
            }
        }

        private static bool ScopeMatches(MusicWorldScope scope)
        {
            return scope switch
            {
                MusicWorldScope.Adventure => tsorcRevampWorld.OnlyAdventureMap && !tsorcRevampWorld.RemixMap,
                MusicWorldScope.Remix => tsorcRevampWorld.RemixMap,
                _ => true
            };
        }

        private static bool TryResolve(string modName, string trackPath, out int music)
        {
            music = 0;
            if (trackPath == null || !ModLoader.TryGetMod(modName, out Mod musicMod) || !MusicLoader.MusicExists(musicMod, trackPath))
            {
                return false;
            }

            music = MusicLoader.GetMusicSlot(musicMod, trackPath);
            return music > 0;
        }
    }

    public sealed class TsorcMusicRegistrySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            TsorcMusicRegistry.Populate();
        }

        public override void Unload()
        {
            TsorcMusicRegistry.Clear();
        }
    }
}
