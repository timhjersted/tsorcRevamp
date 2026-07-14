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

            // Every invader gets a pack-specific default automatically. Explicit entries
            // below are registered afterwards and therefore replace this fallback.
            foreach (ModNPC npc in ModContent.GetContent<ModNPC>())
            {
                if (npc is NPCs.Invaders.InvaderNPC)
                {
                    Assignments[npc.Type] = new TrackAssignment(
                        Path("SlugBattle"),
                        Path("Invader"),
                        MusicID.Boss1,
                        SceneEffectPriority.BossMedium);
                }
            }

            Register("Pinwheel", "Boss18", "Boss18", MusicID.Boss2);
            Register("TheMachine", "Boss11", "Boss11", MusicID.Boss2);
            Register("RedKnight", "Sandstorm", "Event", MusicID.Boss1, SceneEffectPriority.BossLow);
            Register("GreatRedKnight", "Boss13", "Boss13", MusicID.Boss1);
            Register("BlackKnight", "Boss15", null, MusicID.Boss1);
            Register("LeonhardPhase1", "Invader", "Invader", MusicID.Boss1);
            Register("LothricBlackKnight", "Invader", null, MusicID.Boss1);
            RegisterGroup(new[] { "AncientOolacileDemon", "AncientDemonOfTheAbyss", "AncientDemon" }, "Boss15", "Boss15", MusicID.Boss2);
            Register("HeroofLumelia", "Boss2", "Boss2", MusicID.Boss2);
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
            Register("TrueDeath", null, "Boss19", MusicID.Boss2);
            Register("DarkCloud", "Boss11", "Boss20", MusicID.Boss2);
            Register("Death", "Boss13", "Boss13", MusicID.Boss2);
            RegisterGroup(new[] { "WyvernMageShadow", "GhostDragonHead", "HellkiteDragonHead" }, "God-DevouringSerpent", "God-DevouringSerpent", MusicID.Boss2);
            RegisterGroup(new[] { "SeathTheScalelessHead", "PrimordialCrystal" }, "Boss9", "Boss9", MusicID.Boss2);
            Register("AbysmalOolacileSorcerer", "Boss14", "Boss14", MusicID.Boss3);
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
