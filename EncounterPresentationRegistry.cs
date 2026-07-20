using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.GravelordNito;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp
{
    public enum BossClassification
    {
        RequiredMajor,
        OptionalMajor,
        OptionalGreat,
        OptionalField,
        EliteEnemy
    }

    public enum BossEncounterEra
    {
        PreHardmode,
        Hardmode,
        SuperHardmode
    }

    public enum EncounterBannerStyle
    {
        LegendDefeated,
        GreatEnemyDefeated,
        EnemyDefeated,
        GreatInvaderVanquished,
        InvaderVanquished
    }

    public sealed class BossEncounterPresentation
    {
        internal readonly HashSet<int> CompletionNpcTypes = new();

        public int PrimaryNpcType { get; }
        public int ClueIndex { get; }
        public int ProgressionOrder { get; }
        public int FollowingClueIndex { get; }
        public BossClassification Classification { get; }
        public BossEncounterEra Era { get; }
        public bool AlwaysRevealClue { get; }

        public bool IsRequired => Classification == BossClassification.RequiredMajor;
        public bool ShowsDefeatBanner => Classification != BossClassification.EliteEnemy;

        public Color TomeColor => Classification switch
        {
            BossClassification.RequiredMajor => new Color(225, 72, 72),
            BossClassification.OptionalMajor => new Color(182, 92, 224),
            BossClassification.OptionalGreat => new Color(238, 143, 55),
            BossClassification.OptionalField => new Color(235, 211, 70),
            _ => new Color(175, 185, 195)
        };

        public EncounterBannerStyle DefeatBanner => Classification switch
        {
            BossClassification.RequiredMajor => EncounterBannerStyle.LegendDefeated,
            BossClassification.OptionalMajor => EncounterBannerStyle.LegendDefeated,
            BossClassification.OptionalGreat => EncounterBannerStyle.GreatEnemyDefeated,
            _ => EncounterBannerStyle.EnemyDefeated
        };

        internal BossEncounterPresentation(int primaryNpcType, int clueIndex, int progressionOrder, int followingClueIndex, BossClassification classification, BossEncounterEra era, bool alwaysRevealClue)
        {
            PrimaryNpcType = primaryNpcType;
            ClueIndex = clueIndex;
            ProgressionOrder = progressionOrder;
            FollowingClueIndex = followingClueIndex;
            Classification = classification;
            Era = era;
            AlwaysRevealClue = alwaysRevealClue;
            CompletionNpcTypes.Add(primaryNpcType);
        }

        public LocalizedText ClassificationText => Language.GetText($"Mods.tsorcRevamp.EncounterPresentation.Classifications.{Classification}");

        public string FormatTitle(string name)
        {
            return $"{name}\n({ClassificationText.Value})";
        }
    }

    /// <summary>
    /// Player-facing encounter metadata. This deliberately does not control NPC.boss, progression,
    /// multiplayer locking, music, health bars, loot, or save data.
    /// </summary>
    public static class EncounterPresentationRegistry
    {
        private static readonly Dictionary<int, BossEncounterPresentation> ByPrimaryNpcType = new();
        private static readonly Dictionary<int, BossEncounterPresentation> ByCompletionNpcType = new();
        private static readonly Dictionary<int, BossEncounterPresentation> ByClueIndex = new();

        private static readonly HashSet<int> RequiredMajorClues = new()
        {
            6, 15,
            17, 19, 22, 23, 24, 26, 28, 30,
            34, 35, 36, 39, 40, 44
        };

        private static readonly HashSet<int> OptionalMajorClues = new() { 27, 33, 43, 47 };
        private static readonly HashSet<int> OptionalFieldClues = new() { 1, 7, 14 };
        private static readonly HashSet<int> AlwaysRevealedClues = new() { 46, 47 };

        public static IEnumerable<BossEncounterPresentation> Entries => ByPrimaryNpcType.Values;

        public static void Initialize()
        {
            Unload();

            RegisterEra(tsorcRevampWorld.PreHardmodeBossIDs.Keys, BossEncounterEra.PreHardmode);
            RegisterEra(tsorcRevampWorld.HardmodeBossIDs.Keys, BossEncounterEra.Hardmode);
            RegisterEra(tsorcRevampWorld.SHMBossIDs.Keys, BossEncounterEra.SuperHardmode);

            // Boss Checklist entries which are not currently represented in the Tome's arena lists.
            Register(ModContent.NPCType<GravelordNito>(), 11, 11, 12, BossClassification.RequiredMajor, BossEncounterEra.PreHardmode);
            Register(ModContent.NPCType<IceGigas>(), 0, 0, 0, BossClassification.OptionalField, BossEncounterEra.Hardmode);
            Register(ModContent.NPCType<Gigas>(), 0, 0, 0, BossClassification.OptionalField, BossEncounterEra.Hardmode);
            Register(ModContent.NPCType<BlackKnight>(), 0, 0, 0, BossClassification.EliteEnemy, BossEncounterEra.Hardmode);
            Register(ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>(), 0, 29, 30, BossClassification.RequiredMajor, BossEncounterEra.Hardmode);
            Register(ModContent.NPCType<SoulOfCinder>(), 47, 47, 48, BossClassification.OptionalMajor, BossEncounterEra.SuperHardmode, true);

            AddCompletionTypes(ModContent.NPCType<NPCs.Bosses.Slogra>(), ModContent.NPCType<NPCs.Bosses.Gaibon>());
            AddCompletionTypes(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>(), ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());
            AddCompletionTypes(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>());
            AddCompletionTypes(ModContent.NPCType<NPCs.Bosses.Cataluminance>(), ModContent.NPCType<NPCs.Bosses.RetinazerV2>(), ModContent.NPCType<NPCs.Bosses.SpazmatismV2>());
            AddCompletionTypes(NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
        }

        public static void Unload()
        {
            ByPrimaryNpcType.Clear();
            ByCompletionNpcType.Clear();
            ByClueIndex.Clear();
        }

        private static void RegisterEra(IEnumerable<int> npcTypes, BossEncounterEra era)
        {
            NPC sample = new();
            foreach (int npcType in npcTypes)
            {
                sample.SetDefaults(npcType);
                int clueIndex = sample.rarity;
                int progressionOrder = sample.rarity;
                int followingClueIndex = progressionOrder + 1;
                BossClassification classification = ClassificationForNpc(npcType, clueIndex);

                if (npcType == ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>())
                {
                    clueIndex = 48;
                    progressionOrder = 5;
                    followingClueIndex = 6;
                }
                else if (npcType == NPCID.EyeofCthulhu)
                {
                    followingClueIndex = 48;
                }

                Register(npcType, clueIndex, progressionOrder, followingClueIndex, classification, era, AlwaysRevealedClues.Contains(clueIndex));
            }
        }

        private static BossClassification ClassificationForNpc(int npcType, int clueIndex)
        {
            if (npcType == ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>()) return BossClassification.RequiredMajor;

            if (npcType == NPCID.EyeofCthulhu
                || npcType == NPCID.SkeletronHead
                || npcType == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()
                || npcType == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                || npcType == NPCID.Plantera
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>()
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()
                || npcType == ModContent.NPCType<NPCs.Bosses.Pinwheel.Pinwheel>())
            {
                return BossClassification.OptionalGreat;
            }

            if (npcType == NPCID.KingSlime
                || npcType == NPCID.QueenBee
                || npcType == ModContent.NPCType<RedKnight>()
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>()
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                || npcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>())
            {
                return BossClassification.OptionalField;
            }

            if (RequiredMajorClues.Contains(clueIndex)) return BossClassification.RequiredMajor;
            if (OptionalMajorClues.Contains(clueIndex)) return BossClassification.OptionalMajor;
            if (OptionalFieldClues.Contains(clueIndex)) return BossClassification.OptionalField;
            return BossClassification.OptionalGreat;
        }

        private static void Register(int npcType, int clueIndex, int progressionOrder, int followingClueIndex, BossClassification classification, BossEncounterEra era, bool alwaysRevealClue = false)
        {
            BossEncounterPresentation entry = new(npcType, clueIndex, progressionOrder, followingClueIndex, classification, era, alwaysRevealClue);
            ByPrimaryNpcType[npcType] = entry;
            ByCompletionNpcType[npcType] = entry;
            if (clueIndex > 0 && !ByClueIndex.ContainsKey(clueIndex))
            {
                ByClueIndex[clueIndex] = entry;
            }
        }

        private static void AddCompletionTypes(int primaryNpcType, params int[] additionalNpcTypes)
        {
            if (!ByPrimaryNpcType.TryGetValue(primaryNpcType, out BossEncounterPresentation entry)) return;

            foreach (int npcType in additionalNpcTypes)
            {
                entry.CompletionNpcTypes.Add(npcType);
                ByCompletionNpcType[npcType] = entry;
            }
        }

        public static bool TryGet(int primaryNpcType, out BossEncounterPresentation entry)
        {
            return ByPrimaryNpcType.TryGetValue(primaryNpcType, out entry);
        }

        public static bool TryGetByClue(int clueIndex, out BossEncounterPresentation entry)
        {
            return ByClueIndex.TryGetValue(clueIndex, out entry);
        }

        public static LocalizedText GetClassifiedDisplayName(int primaryNpcType, LocalizedText baseName)
        {
            if (!TryGet(primaryNpcType, out BossEncounterPresentation entry)) return baseName;
            return Language.GetText("Mods.tsorcRevamp.EncounterPresentation.ClassifiedName").WithFormatArgs(baseName.Value, entry.ClassificationText.Value);
        }

        public static bool TryGetCompletedEncounter(NPC defeatedNpc, out BossEncounterPresentation entry)
        {
            if (!ByCompletionNpcType.TryGetValue(defeatedNpc.type, out entry)) return false;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI != defeatedNpc.whoAmI && entry.CompletionNpcTypes.Contains(npc.type))
                {
                    entry = null;
                    return false;
                }
            }
            return true;
        }

        public static string GetBannerText(EncounterBannerStyle style)
        {
            return Language.GetTextValue($"Mods.tsorcRevamp.EncounterPresentation.Banners.{style}");
        }
    }
}
