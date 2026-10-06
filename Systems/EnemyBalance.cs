using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems
{
    /// <summary>Which world stage an enemy health value applies from.</summary>
    public enum EnemyStage
    {
        PreHardmode,
        Hardmode,
        SuperHardmode
    }

    /// <summary>
    /// Registry of enemy health, switched by the "New Enemy Balance" gameplay config (default on).
    ///
    /// Bosses (BuildRegistry): every entry is the Expert health the boss had before the pass and the Expert health it
    /// should have now. The pair becomes a multiplier on lifeMax during SetDefaults, which runs BEFORE vanilla's
    /// difficulty scaling and the mod's own Normal / Master / SHM / multiplayer scaling, so all of those still stack on
    /// top of the new number. "Old" must be the Expert health actually seen on the boss bar (solo, before SHM scaling),
    /// not the constant in the class: some classes bake in their own multipliers, so the two differ.
    ///
    /// Bosses that reset lifeMax mid-fight (Pinwheel and Dark Cloud phase 2, The Machine) read
    /// LifeMultiplier / ScaleLife themselves so the retune follows them.
    ///
    /// Regular enemies (BuildEnemyRegistry): each number IS the Expert health, solo. Nothing to convert: in Expert the
    /// enemy ends up with exactly that much, in Master 1.275x (3 x MasterEnemyLifeScale / 2), in a Normal world half (the same 1 : 2 : 3 ratios as
    /// vanilla), and world progress on top (ProgressionScaling: x1.0-2.0 over the Hardmode bosses, then x1.0-1.5 over the SHM bosses). The registry value replaces
    /// whatever the class and vanilla's Hardmode stat-budget bump would have produced, so it no longer drifts with
    /// progression; damage and defense keep the vanilla bump. Casters with zero contact damage are opted in to vanilla's
    /// Expert / Master scaling (vanilla skips it for them), so they scale like every other enemy.
    /// </summary>
    public class EnemyBalance : ModSystem
    {
        public static bool Enabled
        {
            get
            {
                tsorcRevampGameplayConfig config = ModContent.GetInstance<tsorcRevampGameplayConfig>();
                return config != null && config.NewEnemyBalance;
            }
        }

        // npc type -> lifeMax multiplier, derived from the (old, new) pair of each registration below.
        private static readonly Dictionary<int, float> LifeMultipliers = new();

        // (npc type, life bar number) -> multiplier, for bosses that reset lifeMax to a fresh bar mid-fight. Bar 1 is the
        // spawn value and lives in LifeMultipliers; bar 2 and later are read by the boss's own code through PhaseLifeMultiplier.
        private static readonly Dictionary<(int NpcType, int Bar), float> PhaseLifeMultipliers = new();

        // Regular enemies: Expert health by world stage, plus optional condition variants within a stage.
        private sealed class EnemyHpEntry
        {
            // Indexed by EnemyStage. 0 = no value of its own; the lookup falls back to the nearest earlier stage that has one.
            public readonly int[] StageHp = new int[3];

            // Checked first, in registration order. The first variant whose stage matches and whose condition is true wins.
            public readonly List<(EnemyStage Stage, Func<bool> Condition, int ExpertHp)> Variants = new();
        }

        private static readonly Dictionary<int, EnemyHpEntry> EnemyHpEntries = new();

        public override void PostSetupContent()
        {
            LifeMultipliers.Clear();
            PhaseLifeMultipliers.Clear();
            EnemyHpEntries.Clear();
            BuildRegistry();
            BuildEnemyRegistry();
            BuildVanillaEnemyRegistry();
        }

        public override void Unload()
        {
            LifeMultipliers.Clear();
            PhaseLifeMultipliers.Clear();
            EnemyHpEntries.Clear();
        }

        /// <summary>True when the type has an entry in the regular-enemy HP registry, whether or not the New Enemy Balance toggle is on. The balance logger samples kills of exactly these enemies.</summary>
        public static bool IsRegisteredEnemy(int npcType)
        {
            return EnemyHpEntries.ContainsKey(npcType);
        }

        /// <summary>The registered Expert health for a regular enemy in the current world stage. False when the toggle is off or the type is unlisted.</summary>
        public static bool TryGetEnemyExpertHp(int npcType, out int expertHp)
        {
            expertHp = 0;

            if (!Enabled || !EnemyHpEntries.TryGetValue(npcType, out EnemyHpEntry entry))
            {
                return false;
            }

            EnemyStage stage = EnemyStage.PreHardmode;

            if (tsorcRevampWorld.SuperHardMode)
            {
                stage = EnemyStage.SuperHardmode;
            }
            else if (Main.hardMode)
            {
                stage = EnemyStage.Hardmode;
            }

            foreach (var variant in entry.Variants)
            {
                if (variant.Stage == stage && variant.Condition())
                {
                    expertHp = variant.ExpertHp;
                    return true;
                }
            }

            for (int stageIndex = (int)stage; stageIndex >= 0; stageIndex--)
            {
                if (entry.StageHp[stageIndex] > 0)
                {
                    expertHp = entry.StageHp[stageIndex];
                    return true;
                }
            }

            return false;
        }

        /// <summary>Master-mode HP trim for regular enemies (bosses use vanilla's bossAdjustment, also 0.85). Master ends at 3 x 0.85 / 2 = 1.275x Expert. No multiplayer scaling.</summary>
        public const float MasterEnemyLifeScale = 0.85f;

        /// <summary>The registry's Expert health converted for the current game mode (Normal 0.5, Expert 1, Master 1.275) and world progress (ProgressionScaling.LifeScale).</summary>
        public static int ModeScaledLife(NPC npc, int expertHp)
        {
            float modeMultiplier = Main.GameModeInfo.EnemyMaxLifeMultiplier / 2f;

            if (Main.masterMode)
            {
                modeMultiplier *= MasterEnemyLifeScale;
            }

            // Hardmode ramp before SHM, SHM ramp in SHM; covers every registry enemy, not just SHM-namespace classes.
            float progressScale = ProgressionScaling.LifeScale(npc);

            return (int)Math.Round(expertHp * modeMultiplier * progressScale);
        }

        /// <summary>
        /// For enemies that pick their health on the first AI tick (stage or rarity rolls), which is after vanilla's scaling has run.
        /// Call it after the class's own lifeMax assignment: when the registry has a value it replaces that, with the mode multiplier
        /// applied here instead. rarityMultiplier is the class's own roll (1 for none). Returns false, changing nothing, when unregistered.
        /// </summary>
        public static bool ApplyLateLife(NPC npc, float rarityMultiplier = 1f)
        {
            if (!TryGetEnemyExpertHp(npc.type, out int expertHp))
            {
                return false;
            }

            npc.lifeMax = ModeScaledLife(npc, (int)Math.Round(expertHp * rarityMultiplier));
            npc.life = npc.lifeMax;
            return true;
        }

        /// <summary>
        /// Called from tsorcRevampGlobalNPC.SetDefaults (modded types) and the end of VanillaChanges.SetDefaults (vanilla types, after
        /// its absolute lifeMax values). Sets the Normal-world value (and every puppet's value) here, with world progress applied; the
        /// SHM block in tsorcRevampGlobalNPC skips registry enemies, so nothing scales twice. Expert and Master are finished
        /// in EnemyBalanceNPC.ApplyDifficultyAndPlayerScaling, after vanilla's stat-budget bump has used the class's own lifeMax.
        /// </summary>
        public static void ApplyEnemySetDefaults(NPC npc)
        {
            if (!TryGetEnemyExpertHp(npc.type, out int expertHp))
            {
                return;
            }

            bool isPuppet = npc.ModNPC is NPCs.Puppets.PuppetNPC puppet && puppet.UsesPuppetDifficultyScaling;
            float progressScale = ProgressionScaling.LifeScale(npc);

            if (isPuppet)
            {
                // Puppets already halve vanilla's Expert x2 in the mod's own hook, so the authored value is the Expert value everywhere.
                // The progress scale rides along through that pipeline, so it is applied here once for every mode.
                npc.lifeMax = (int)Math.Round(expertHp * progressScale);
                npc.life = npc.lifeMax;
                return;
            }

            if (npc.damage == 0)
            {
                // Vanilla skips Expert / Master scaling for zero contact damage (casters). Opt them back in, and keep the Hardmode
                // stat-budget bump off them so their defense stays exactly what the class sets.
                NPCID.Sets.NeedsExpertScaling[npc.type] = true;
                NPCID.Sets.DontDoHardmodeScaling[npc.type] = true;
            }

            if (!Main.expertMode)
            {
                npc.lifeMax = (int)Math.Round(expertHp / 2f * progressScale);
                npc.life = npc.lifeMax;
            }
        }

        /// <summary>Multiplier to apply to an authored health value for this NPC type. 1 when the toggle is off or the type is unlisted.</summary>
        public static float LifeMultiplier(int npcType)
        {
            if (!Enabled)
            {
                return 1f;
            }

            if (!LifeMultipliers.TryGetValue(npcType, out float multiplier))
            {
                return 1f;
            }

            return multiplier;
        }

        /// <summary>Multiplier for a later life bar (2 = the second) that the boss sets itself. 1 when the toggle is off or the bar is unlisted.</summary>
        public static float PhaseLifeMultiplier(int npcType, int bar)
        {
            if (!Enabled)
            {
                return 1f;
            }

            if (!PhaseLifeMultipliers.TryGetValue((npcType, bar), out float multiplier))
            {
                return 1f;
            }

            return multiplier;
        }

        /// <summary>Scales a hard-coded health constant by the NPC type's registered multiplier.</summary>
        public static int ScaleLife(int npcType, int authoredLife)
        {
            return (int)Math.Round(authoredLife * LifeMultiplier(npcType));
        }

        /// <summary>Applies the registered multiplier to an NPC's lifeMax. Called from SetDefaults.</summary>
        public static void ApplyLifeMultiplier(NPC npc)
        {
            float multiplier = LifeMultiplier(npc.type);
            if (multiplier == 1f)
            {
                return;
            }

            npc.lifeMax = (int)Math.Round(npc.lifeMax * multiplier);
            npc.life = npc.lifeMax;
        }

        private void Register(int npcType, int oldExpertHp, int newExpertHp)
        {
            if (oldExpertHp <= 0)
            {
                return;
            }

            LifeMultipliers[npcType] = newExpertHp / (float)oldExpertHp;
        }

        private void RegisterMod(string npcName, int oldExpertHp, int newExpertHp)
        {
            if (!Mod.TryFind(npcName, out ModNPC modNpc))
            {
                Mod.Logger.Warn($"EnemyBalance: no ModNPC named '{npcName}', entry skipped.");
                return;
            }

            Register(modNpc.Type, oldExpertHp, newExpertHp);
        }

        private void RegisterPhaseMod(string npcName, int bar, int oldExpertHp, int newExpertHp)
        {
            if (!Mod.TryFind(npcName, out ModNPC modNpc))
            {
                Mod.Logger.Warn($"EnemyBalance: no ModNPC named '{npcName}', phase entry skipped.");
                return;
            }

            PhaseLifeMultipliers[(modNpc.Type, bar)] = newExpertHp / (float)oldExpertHp;
        }

        /// <summary>Registers a regular enemy's Expert health per stage. A stage left out inherits the nearest earlier one.</summary>
        private void Enemy(string npcName, int phm = 0, int hm = 0, int shm = 0)
        {
            if (!Mod.TryFind(npcName, out ModNPC modNpc))
            {
                Mod.Logger.Warn($"EnemyBalance: no ModNPC named '{npcName}', enemy entry skipped.");
                return;
            }

            EnemyHpEntry entry = new EnemyHpEntry();
            entry.StageHp[(int)EnemyStage.PreHardmode] = phm;
            entry.StageHp[(int)EnemyStage.Hardmode] = hm;
            entry.StageHp[(int)EnemyStage.SuperHardmode] = shm;
            EnemyHpEntries[modNpc.Type] = entry;
        }

        /// <summary>Registers a vanilla enemy's Expert health per stage, same rules as Enemy().</summary>
        private void Vanilla(int npcType, int phm = 0, int hm = 0, int shm = 0)
        {
            EnemyHpEntry entry = new EnemyHpEntry();
            entry.StageHp[(int)EnemyStage.PreHardmode] = phm;
            entry.StageHp[(int)EnemyStage.Hardmode] = hm;
            entry.StageHp[(int)EnemyStage.SuperHardmode] = shm;
            EnemyHpEntries[npcType] = entry;
        }

        /// <summary>An extra Expert health for one stage when a condition holds (the class picks its HP by progression flag). Register after Enemy(), most specific first.</summary>
        private void EnemyVariant(string npcName, EnemyStage stage, Func<bool> condition, int expertHp)
        {
            if (!Mod.TryFind(npcName, out ModNPC modNpc) || !EnemyHpEntries.TryGetValue(modNpc.Type, out EnemyHpEntry entry))
            {
                Mod.Logger.Warn($"EnemyBalance: no registered enemy named '{npcName}', variant skipped.");
                return;
            }

            entry.Variants.Add((stage, condition, expertHp));
        }

        private void BuildRegistry()
        {
            // ---- Pre-Hardmode ----
            Register(NPCID.EyeofCthulhu, 3640, 4000);
            RegisterMod("Kahlrun", 1500, 2500);
            RegisterMod("VesselOfSouls", 6000, 6000); // base raised in VesselOfSouls.cs (was 3000), so no multiplier
            RegisterMod("RedKnight", 3000, 3000); // base raised in RedKnight.cs (was 2500); scripted events override HP via SetCustomStats
            RegisterMod("OwlFather", 4000, 7000);
            Register(NPCID.KingSlime, 2800, 3000);

            // Eater of Worlds: +1000 on a worm of roughly 18000 total, spread over every segment type.
            Register(NPCID.EaterofWorldsHead, 18000, 19000);
            Register(NPCID.EaterofWorldsBody, 18000, 19000);
            Register(NPCID.EaterofWorldsTail, 18000, 19000);

            Register(NPCID.BrainofCthulhu, 2125, 7000);

            // Pinwheel: two life bars. It heals into the second one mid-fight.
            RegisterMod("Pinwheel", 2000, 3000);
            RegisterPhaseMod("Pinwheel", 2, 5000, 6000);

            Register(NPCID.QueenBee, 5320, 7000);
            RegisterMod("Gaibon", 4000, 6000);
            RegisterMod("Slogra", 4000, 6000);
            Register(NPCID.SkeletronHead, 8000, 9000);
            RegisterMod("GravelordNito", 4000, 12000);
            RegisterMod("AncientOolacileDemon", 4800, 10000);
            RegisterMod("BlackNinja", 6000, 6000); // base raised in BlackNinja.cs (was 1800 -> 6000); registry now keeps it there (was 8000)
            RegisterMod("AncestralSpirit", 11900, 12000);
            RegisterMod("JungleWyvernHead", 22000, 25000);
            RegisterMod("AncientDemon", 10000, 18000);
            RegisterMod("StuddedLeatherWarrior", 4000, 4000); // base raised in StuddedLeatherWarrior.cs (was 2600), so no multiplier

            // ---- Hardmode ----
            Register(NPCID.WallofFlesh, 19600, 20000);
            Register(NPCID.QueenSlimeBoss, 28800, 30000);
            RegisterMod("TheRage", 18000, 40000);
            RegisterMod("WyvernMage", 20000, 40000);
            RegisterMod("TheSorrow", 24000, 60000);
            RegisterMod("MechaDragonHead", 61000, 40000);
            RegisterMod("HeroofLumelia", 8000, 50000);
            RegisterMod("Death", 32000, 60000);
            RegisterMod("SerrisHead", 6000, 6000); // unchanged: its damage is gated by a unique mechanic
            RegisterMod("SerrisX", 10000, 10000); // unchanged, same reason
            RegisterMod("TheHunter", 35000, 70000);

            // Vanilla Destroyer segments mirror the head's life, so scaling all three keeps them consistent.
            Register(NPCID.TheDestroyer, 202500, 200000);
            Register(NPCID.TheDestroyerBody, 202500, 200000);
            Register(NPCID.TheDestroyerTail, 202500, 200000);

            // The Machine: the fight's real pool is core (35000, set in HandleLife) + six arms of 15000 each, about 125000 in
            // Expert. The 20000 -> 30000 pair is the SetDefaults value the table was built from, so it is x1.5 on all of it.
            RegisterMod("TheMachine", 20000, 30000);
            RegisterMod("PrimeBeam", 15000, 22500);
            RegisterMod("PrimeIon", 15000, 22500);
            RegisterMod("PrimeBuzzsaw", 15000, 22500);
            RegisterMod("PrimeGatling", 15000, 22500);
            RegisterMod("PrimeSiege", 15000, 22500);
            RegisterMod("PrimeWelder", 15000, 22500);

            // The vanilla twins each carry about 37500 in Expert; the table lists Retinazer, so Spazmatism moves with it.
            Register(NPCID.Retinazer, 37500, 50000);
            Register(NPCID.Spazmatism, 37500, 50000);

            RegisterMod("Cataluminance", 120000, 120000); // unchanged

            Register(NPCID.Plantera, 67200, 70000);
            Register(NPCID.Golem, 90000, 100000);
            Register(NPCID.DukeFishron, 169000, 180000);
            Register(NPCID.HallowBoss, 198800, 200000);

            // Moon Lord: the table gives one total for the whole fight. Vanilla Expert is head 67500 + core 75000 +
            // two hands of 37500 = 217500, so the three part types share one multiplier.
            Register(NPCID.MoonLordHead, 217500, 400000);
            Register(NPCID.MoonLordHand, 217500, 400000);
            Register(NPCID.MoonLordCore, 217500, 400000);

            // ---- Okiku chain ----
            RegisterMod("DamnedSoul", 17250, 35000);
            RegisterMod("ShadowDragonHead", 126000, 130000);
            RegisterMod("DarkDragonMask", 24000, 30000);
            RegisterMod("Okiku", 22400, 25000);
            RegisterMod("BrokenOkiku", 15400, 20000);
            RegisterMod("DarkShogunMask", 75000, 75000); // unchanged
            RegisterMod("Attraidies", 400000, 400000); // unchanged

            // ---- Twins V2 ----
            RegisterMod("RetinazerV2", 90000, 120000);
            RegisterMod("SpazmatismV2", 90000, 120000);

            // ---- Other ----
            RegisterMod("LeonhardPhase1", 2500, 2500); // unchanged

            // ---- Super Hardmode (health shown is before SHM boss-count scaling, which still applies) ----
            RegisterMod("GreatRedKnight", 30000, 350000);
            RegisterMod("EarthFiendLich", 350000, 460000);
            RegisterMod("Witchking", 170000, 200000);
            RegisterMod("FireFiendMarilith", 400000, 525000);
            RegisterMod("WaterFiendKraken", 325000, 450000);
            RegisterMod("SeathTheScalelessHead", 475000, 600000);
            RegisterMod("Blight", 300000, 450000);
            RegisterMod("GrandOccultist", 210000, 400000); // the rebuilt Abysmal Oolacile Sorcerer
            RegisterMod("Artorias", 250000, 500000);
            RegisterMod("Chaos", 450000, 500000);
            RegisterMod("Gwyn", 750000, 1000000);
            RegisterMod("HellkiteDragonHead", 650000, 650000); // unchanged
            RegisterMod("WyvernMageShadow", 250000, 250000); // unchanged
            RegisterMod("GhostDragonHead", 600000, 600000); // unchanged; Wyvern Mage Shadow's dragon, the larger part of that fight (segments share the head's life)
            RegisterMod("DarkCloud", 300000, 300000); // unchanged
            RegisterPhaseMod("DarkCloud", 2, 700000, 700000); // unchanged; phase 2 is 700000 x players, with no Master or SHM scaling

            // ---- Lunar events ----
            Register(NPCID.LunarTowerSolar, 50000, 100000);
            Register(NPCID.LunarTowerNebula, 50000, 100000);
            Register(NPCID.LunarTowerVortex, 50000, 100000);
            Register(NPCID.LunarTowerStardust, 50000, 100000);
            Register(NPCID.CultistBoss, 135000, 250000);
            Register(NPCID.MartianSaucer, 35294, 60000);
        }

        /// <summary>
        /// Regular enemies. Every number is the Expert health, solo (see the class summary). Edit the numbers freely;
        /// "authored" in each comment is what the class itself sets, for reference. A stage left out inherits the one before it.
        /// Every value is shown before the world-progress scaling (ProgressionScaling.LifeScale), which still applies on top.
        /// </summary>
        private void BuildEnemyRegistry()
        {
            // ---- Enemies that first appear before Hardmode (their HM / SHM values follow on the same line) ----
            Enemy("SnowOwl", phm: 150, hm: 250, shm: 3000); // fodder; authored 15 / 50 / 50
            Enemy("UndeadCaster", phm: 100); // variant; authored 30; caster, contact damage 0
            EnemyVariant("UndeadCaster", EnemyStage.PreHardmode, () => NPC.downedBoss3, 500); // after Skeletron: authored 120
            EnemyVariant("UndeadCaster", EnemyStage.PreHardmode, () => NPC.downedBoss1, 300); // after the Eye of Cthulhu: authored 60
            Enemy("MutantToad", phm: 400, hm: 800, shm: 12000); // fodder; authored 40 / 200 / 450
            Enemy("DworcFleshhunter", phm: 400, hm: 800, shm: 8000); // fodder; authored 50 / 100 / 100
            Enemy("DworcVenomsniper", phm: 300, hm: 600, shm: 6000); // fodder; authored 50 / 100 / 100
            Enemy("FirebombHollow", phm: 200, hm: 1600, shm: 8000); // fodder / strong / standard; authored 60 / 500 / 2000
            Enemy("StoneGolem", phm: 300, hm: 600, shm: 6000); // fodder; authored 60 / 120 / 350
            Enemy("ArmoredWraith", phm: 250, hm: 500, shm: 5000); // fodder / ? / fodder; authored 75 / 75
            Enemy("MountedSandsprog", phm: 450, hm: 900, shm: 9000); // fodder / standard / standard; authored 75 / 300 / 900
            Enemy("Sandsprog", phm: 400, hm: 800, shm: 8000); // fodder / fodder / standard; authored 75 / 200 / 900
            Enemy("SandsprogMage", phm: 400, hm: 800, shm: 8000); // fodder / fodder / standard; authored 75 / 200 / 900
            Enemy("DungeonMage", phm: 500, hm: 1000, shm: 10000); // fodder / ? / fodder; authored 160 / 1050; caster, contact damage 0
            Enemy("MountedSandsprogMage", phm: 500, hm: 1000, shm: 15000); // fodder / standard / standard; authored 80 / 300 / 900
            Enemy("Parasprite", phm: 200, hm: 800, shm: 1600); // fodder; authored 90 / 90 / 90
            Enemy("TibianValkyrie", phm: 300, hm: 600, shm: 6000); // strong; authored 90 / 260 / 700
            Enemy("BasiliskWalker", phm: 400, hm: 800); // fodder / standard; authored 100 / 250
            Enemy("GhostOfAHollowWarrior", phm: 300, hm: 600, shm: 6000); // standard; authored 100 / 250 / 1000
            Enemy("GhostOfTheForgottenWarrior", phm: 600, hm: 800, shm: 8000); // standard / fodder / standard; authored 100 / 200 / 1000
            Enemy("HollowWarrior", phm: 300, hm: 600, shm: 6000); // standard; authored 100 / 250 / 1000
            Enemy("OolacileCultist", phm: 600, hm: 5000, shm: 12000); // strong; authored 200 / 450 / 3000; caster, contact damage 0; puppet
            Enemy("TibianAmazon", phm: 500, hm: 1000, shm: 10000); // strong; authored 100 / 250
            Enemy("HollowSpearman", phm: 300, hm: 600, shm: 6000); // standard; authored 105 / 270 / 1000
            Enemy("AbandonedStump", phm: 250, hm: 500, shm: 5000); // standard / standard / fodder; authored 120 / 240 / 500
            Enemy("FireLurker", phm: 1000, hm: 2000, shm: 12000); // standard; authored 120 / 250 / 1200
            Enemy("ManHunter", phm: 400, hm: 800, shm: 8000); // standard / fodder / fodder; authored 125 / 250 / 600
            Enemy("GhostOfTheForgottenKnight", phm: 700, hm: 1400, shm: 14000); // standard / fodder / standard; authored 150 / 200 / 1000
            Enemy("BarrowWight", phm: 900, hm: 3500, shm: 18000); // standard / standard / fodder; authored 180 / 180 / 180
            Enemy("AttraidiesIllusion", phm: 500); // standard; authored 400; caster, contact damage 0
            Enemy("AttraidiesManifestation", phm: 1000, hm: 10000); // standard / strong; authored 400 / 800; caster, contact damage 0
            Enemy("JungleSentree", phm: 450, hm: 1250, shm: 9000); // standard / fodder / fodder; authored 200 / 400 / 650
            Enemy("Archdeacon", phm: 1500, hm: 3000, shm: 12000); // strong; authored 500; caster, contact damage 0
            Enemy("DemonElemental", phm: 1000, hm: 2000, shm: 12000); // strong / strong / standard; authored 250 / 500 / 2000
            Enemy("Eland", phm: 2000, hm: 6250, shm: 25000); // strong; authored 500 / 1000 / 20000; caster, contact damage 0
            Enemy("HollowSoldier", phm: 350, hm: 600, shm: 6000); // strong / strong / standard; authored 250 / 500 / 1500
            Enemy("Necromancer", phm: 3000, hm: 6000, shm: 24000); // strong; authored 800; caster, contact damage 0
            Enemy("QuaraPincher", phm: 1000, hm: 2000, shm: 20000); // strong / strong / standard; authored 1200 / 1200 / 1200
            Enemy("RedCloudHunter", phm: 1500, hm: 3000, shm: 18000); // strong / strong / standard; authored 600 / 700 / 2000
            Enemy("LothricKnight", phm: 2000, hm: 6000, shm: 24000); // strong / elite / strong; authored 750 / 1400 / 2500
            Enemy("LothricSpearKnight", phm: 2000, hm: 4000, shm: 24000); // strong; authored 750 / 1200 / 3000
            Enemy("Warlock", phm: 1500, hm: 3000); // elite / strong; authored 750 / 1500; caster, contact damage 0
            Enemy("BlackKnight", phm: 10000, hm: 20000, shm: 40000); // elite / elite / strong; authored 1000 / 2000 / 4000
            Enemy("LothricBlackKnight", phm: 2500, hm: 5000, shm: 30000); // elite / elite / strong; authored 1000 / 1500 / 5000
            Enemy("JungleWyvernJuvenileHead", phm: 3000, hm: 6000, shm: 12000); // standard; authored 1250 / 2500 / 3000; HP set high on purpose: piercing weapons hit every segment
            Enemy("Dunlending", phm: 50, hm: 1450, shm: 11000); // ? / fodder / fodder; authored 45 / 400
            Enemy("RingedKnight", phm: 1000, hm: 4000, shm: 12000); // ? / strong / strong; authored 400 / 800 / 2500

            // ---- Enemies that first appear in Hardmode ----
            Enemy("CloudBat", hm: 500); // fodder; authored 100
            Enemy("Willowisp", hm: 1500, shm: 9000); // fodder; authored 150 / 350
            Enemy("ClericOfSorrow", hm: 4000, shm: 24000); // strong; authored 400 / 1200; puppet
            Enemy("GhostOfTheDrowned", phm: 175, hm: 3500, shm: 15000); // standard; authored 150 / 450 / 1300; caster, contact damage 0
            Enemy("ShadowMage", hm: 3000, shm: 9000); // fodder; authored 450 / 1350; caster, contact damage 0
            Enemy("QuaraHydromancer", hm: 3500, shm: 12000); // strong / fodder; authored 250 / 250; caster, contact damage 0
            Enemy("Byakhee", hm: 2500, shm: 7200); // standard / fodder; authored 300 / 700
            Enemy("BasiliskShifter", hm: 3500); // strong; authored 350
            Enemy("DworcVoodooShaman", hm: 1500); // standard; authored 750; caster, contact damage 0
            Enemy("DemonSpirit", phm: 800, hm: 1600); // ? / standard; authored 400
            Enemy("EvilEye", hm: 9000); // strong; authored 400
            Enemy("Assassin", hm: 1000, shm: 6000); // standard; authored 500 / 2000
            Enemy("CrazedDemonSpirit", phm: 1000, hm: 2000, shm: 12000); // ? / standard / standard; authored 500 / 1000
            Enemy("DworcAlchemist", phm: 500, hm: 1000); // ? / strong; authored 250 / 500; caster, contact damage 0
            Enemy("Tonberry", hm: 3000, shm: 18000); // strong / standard; authored 1500 / 3500; caster, contact damage 0
            Enemy("ParasyticWormHead", hm: 3000); // elite; authored 1500
            Enemy("FallenNecromancer", hm: 8000); // elite; authored 4000
            Enemy("MarilithSpiritTwin", hm: 20000); // mini-boss; authored 10000

            // ---- Enemies that first appear in Super Hardmode (HP shown before the x1.0-1.5 SHM progress scaling, which still applies) ----
            Enemy("Locust", shm: 900); // fodder; authored 300
            Enemy("ManOfWar", shm: 3000); // fodder; authored 800
            Enemy("DemonWheel", shm: 4000); // standard; authored 1000
            Enemy("VampireBat", shm: 4000); // standard; authored 1300
            Enemy("CrystalKnight", shm: 50000); // standard; authored 2800; caster, contact damage 0
            Enemy("DarkKnight", shm: 40000); // standard; authored 3000; caster, contact damage 0
            Enemy("AbyssLurker", shm: 20000); // standard; authored 1600
            Enemy("DarkBloodKnight", shm: 40000); // standard; authored 3200; caster, contact damage 0
            Enemy("HydrisElemental", shm: 10000); // standard; authored 1600
            Enemy("DworcAbysswalker", shm: 15000); // standard; authored 3500; caster, contact damage 0
            Enemy("HydrisNecromancer", shm: 30000); // standard; authored 3500; caster, contact damage 0
            Enemy("CorruptedElemental", shm: 12000); // standard; authored 2000
            Enemy("CorruptedHornet", shm: 9000); // standard; authored 2000
            Enemy("GuardianCorruptor", shm: 12000); // standard; authored 2200
            Enemy("IceSkeleton", shm: 7000); // standard; authored 2200
            Enemy("BarrowWightNemesis", shm: 16000); // strong; authored 2500
            Enemy("BasiliskHunter", shm: 18000); // strong; authored 2500
            Enemy("OolacileSorcerer", shm: 15000); // strong; authored 5500; caster, contact damage 0
            Enemy("GhostOfTheDarkmoonKnight", shm: 15000); // strong; authored 3000
            Enemy("Tetsujin", shm: 20000); // strong; authored 3400
            Enemy("OolacileDemon", shm: 16000); // strong; authored 3500
            Enemy("SlograII", shm: 24000); // strong; authored 4000
            Enemy("OolacileKnight", shm: 18000); // strong; authored 5400
            Enemy("TaurusKnight", shm: 20000); // strong; authored 5400
            Enemy("Plaguesmith", shm: 50000); // strong; authored 8250
            Enemy("AncientDemonOfTheAbyss", shm: 35000); // elite; authored 15000
            Enemy("Massacre", shm: 90000); // elite; authored 20000
            Enemy("SerpentOfTheAbyssHead", shm: 50000); // elite; authored 24000
            Enemy("GreatBlackKnight", shm: 80000); // elite; authored 50000; caster, contact damage 0

            // ---- Events: no natural spawn (placed by the map, summoned by a boss or event, or spawned by another enemy), alphabetical ----
            Enemy("BarrowWightPhantom", shm: 18000); // standard; authored 1250
            Enemy("DemonLordApocalypse", phm: 150000); // mini-boss; authored 40000
            Enemy("DestroyerLaserProbe", phm: 150, hm: 198, shm: 247); // fodder; authored 75 / 75 / 75
            Enemy("DiscipleOfAttraidies", phm: 6000); // elite; authored 4500; caster, contact damage 0
            Enemy("FrozenGigasStatue", phm: 5000); // standard; authored 250; caster, contact damage 0
            Enemy("Gigas", phm: 100000, hm: 100000, shm: 400000); // mini-boss / mini-boss / elite; authored 32000 / 32000 / 32000
            Enemy("Hydra", phm: 400000); // mini-boss; authored 100000
            Enemy("IceGigas", phm: 50000, hm: 50000, shm: 350000); // mini-boss / mini-boss / elite; authored 22000 / 22000 / 22000
            Enemy("KnightOfGwyn", shm: 200000); // elite; authored 25000
            Enemy("MindflayerIllusion", phm: 1500); // strong; authored 1000; caster, contact damage 0
            Enemy("MindflayerKingServant", phm: 200); // standard; authored 200; caster, contact damage 0
            Enemy("MindflayerServant", phm: 70); // fodder; authored 70; caster, contact damage 0
            Enemy("MinotaurMage", phm: 900, hm: 1800, shm: 10800); // standard / standard / fodder; authored 155 / 155 / 155
            Enemy("PrimeLaserProbe", phm: 150, hm: 198, shm: 247); // fodder; authored 75 / 75 / 75
            Enemy("QuaraClutchCrab", phm: 500, hm: 2000, shm: 7200); // standard / standard / fodder; authored 400 / 400 / 400
            Enemy("QuaraMantassin", phm: 3000); // elite; authored 800
            Enemy("Sahagin", phm: 300, hm: 3000, shm: 18000); // fodder; authored 44 / 44 / 44
            Enemy("SerpentOfTheAbyssBody", shm: 20000); // elite; authored 10000
            Enemy("SerpentOfTheAbyssTail", shm: 20000); // elite; authored 10000
            Enemy("SpellboundGhoul", phm: 900); // standard; authored 150
            Enemy("WaterSpirit", phm: 1500); // strong; authored 600

            // ---- Not registered ----
            // Legacy copies kept for A/B reference; natural spawning is disabled in code. Not registered.
            //   FirebombHollowOriginal: authored 60 / 500 / 2000 (legacy/disabled spawn)
            //   GhostOfAHollowWarriorOriginal: authored 100 / 250 / 1000 (legacy/disabled spawn)
            //   GhostOfTheDrownedOriginal: authored 150 / 450 / 1300 (legacy/disabled spawn)
            //   HollowSoldierOriginal: authored 250 / 500 / 1500 (legacy/disabled spawn)
            //   HollowSpearmanOriginal: authored 105 / 270 / 1000 (legacy/disabled spawn)
            //   HollowWarriorOriginal: authored 100 / 250 / 1000 (legacy/disabled spawn)
            //   LothricBlackKnightOriginal: authored 1000 / 1500 / 5000 (legacy/disabled spawn)
            //   LothricKnightOriginal: authored 750 / 1400 / 2500 (legacy/disabled spawn)
            //   LothricSpearKnightOriginal: authored 750 / 1200 / 3000 (legacy/disabled spawn)
            //   RedKnightTest: authored 2500 / 2500 / 4000 (legacy/disabled spawn)
            //   RingedKnightOriginal: authored 400 / 800 / 2500 (legacy/disabled spawn)
            // Projectile-like helpers, critters and one-hit spawns (HP of 20 or less). Not registered.
            //   CosmicCrystalLizard: authored 18
            //   GaibonFireball: authored 1
            //   HumanityPhantom: authored in code
            //   LivingShroomThief: authored 16
            //   MarilithSeeker: authored 1
            //   MushroomCreature: authored 0
            //   ObsidianJellyfish: authored 4 / 6 / 6
            //   PinwheelFireball: authored 1
            //   ResentfulSeedling: authored 14
            //   ViciousSpit: authored 1
            //   KhaiosTransitionOrb: authored 1
            //   OwlCompanion: authored 1
            //   OwlFireDiveCompanion: authored 1
            // Wyvern and worm body / leg / tail segments: they mirror the life of the head (authored 60,000,000 and up), so the head carries the HP. Not registered.
            //   JungleWyvernJuvenileBody: authored 60000000 (segment: mirrors the head)
            //   JungleWyvernJuvenileBody2: authored 60000000 (segment: mirrors the head)
            //   JungleWyvernJuvenileBody3: authored 60000000 (segment: mirrors the head)
            //   JungleWyvernJuvenileLegs: authored 60000000 (segment: mirrors the head)
            //   JungleWyvernJuvenileTail: authored 60000000 (segment: mirrors the head)
            //   ParasyticWormBody: authored 91000000 (segment: mirrors the head)
            //   ParasyticWormTail: authored 91000000 (segment: mirrors the head)
            // Special cases. Not registered.
            //   CrystalSentry: fixed 50,000 in every world tier by CrystalSentry.FixedLife (summoned by a boss)
            // Puppet bosses and encounters, tracked in the boss section above or owned by other work. Not registered.
            //   RedKnight: authored 3000 / 3000 / 4000
            //   AbyssalNinja: authored in code
            //   BlackNinja: authored 6000
            //   Blaidd: authored 3000
            //   CursedDragon: authored 25000
            //   DreadWraith: authored 2400
            //   Kahlrun: authored 1500
            //   Khaios: authored 9000
            //   Marik: authored 450000
            //   OwlFather: authored 4000
            //   ShadowNinja: authored 30000
            //   SpiritOfKhaios: authored 11000
            //   StuddedLeatherWarrior: authored 4000
            //   Ulhan: authored 11000
        }

        /// <summary>
        /// Vanilla enemies, as Expert HP per stage like the mod enemies above. Values started from the HP each one actually had
        /// (vanilla stats + VanillaChanges + vanilla's Expert scaling, solo; shown as "old" on each line), then: a stage with no
        /// HP change of its own got HM = PHM x2 (or the old HM value when that was higher) and SHM = HM x5. Stages an enemy is
        /// removed from (VanillaChanges.AI block list) are
        /// left out. "after Plantera" is vanilla's Hardmode stat-budget bump for weak enemies, which the registry value replaces.
        /// </summary>
        private void BuildVanillaEnemyRegistry()
        {
            // ---- Vanilla enemies that first appear before Hardmode (HM = PHM x2 and SHM = HM x5 unless the old values had their own) ----
            Vanilla(NPCID.AngryBones, phm: 160); // old phm 160 / removed in HM
            Vanilla(NPCID.AngryBonesBig, phm: 140); // old phm 140 / removed in HM
            Vanilla(NPCID.AngryBonesBigHelmet, phm: 240); // old phm 240 / removed in HM
            Vanilla(NPCID.AngryBonesBigMuscle, phm: 140); // old phm 140 / removed in HM
            Vanilla(NPCID.AnomuraFungus, phm: 460, hm: 920, shm: 1150); // old phm 460 / hm 460 / shm 1150; kept: has SHM scaling
            Vanilla(NPCID.Antlion, phm: 90, hm: 180); // old phm 90 / hm 98 / removed in SHM
            Vanilla(NPCID.BlueJellyfish, phm: 68, hm: 148); // old phm 68 / hm 148 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.BoneSerpentHead, phm: 1000, hm: 2000, shm: 5000); // old phm 1000 / hm 1000 / shm 5000; kept: has SHM scaling
            Vanilla(NPCID.CaveBat, phm: 32, hm: 140); // old phm 32 / hm 140 (176 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.CochinealBeetle, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.Crab, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.Crawdad, phm: 100, hm: 200, shm: 1000); // old phm 100 / hm 110 (220 after Plantera) / shm 220
            Vanilla(NPCID.Crawdad2, phm: 100, hm: 200, shm: 1000); // old phm 100 / hm 110 (220 after Plantera) / shm 220
            Vanilla(NPCID.Crimera, phm: 80, hm: 176); // old phm 80 / hm 176 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.CursedSkull, phm: 106, hm: 212); // old phm 106 / hm 116 / removed in SHM
            Vanilla(NPCID.CyanBeetle, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.Dandelion, phm: 100, hm: 220); // old phm 100 / hm 220 (330 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.DarkCaster, phm: 100); // old phm 100 / removed in HM; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.Demon, phm: 280, hm: 560, shm: 1400); // old phm 280 / hm 280 (308 after Plantera) / shm 1400; kept: has SHM scaling
            Vanilla(NPCID.DemonEye, phm: 120, hm: 264); // old phm 120 / hm 264 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.DemonEyeOwl, phm: 150, hm: 330); // old phm 150 / hm 330 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.DevourerHead, phm: 400, hm: 800, shm: 4000); // old phm 400 / hm 400 (440 after Plantera) / shm 440
            Vanilla(NPCID.DungeonSlime, phm: 300, hm: 600, shm: 1500); // old phm 300 / hm 330 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.EaterofSouls, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.FireImp, phm: 112, hm: 224, shm: 560); // old phm 112 / hm 112 / shm 560; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.FlyingAntlion, phm: 120, hm: 240); // old phm 120 / hm 132 / removed in SHM
            Vanilla(NPCID.FlyingFish, phm: 40, hm: 176, shm: 880); // old phm 40 / hm 176 (220 after Plantera) / shm 220; kept old HM: higher than PHM x2
            Vanilla(NPCID.FungiBulb, phm: 180, hm: 360, shm: 494); // old phm 180 / hm 198 (396 after Plantera) / shm 494; kept: has SHM scaling
            Vanilla(NPCID.GiantFlyingAntlion, phm: 180, hm: 360); // old phm 180 / hm 198 / removed in SHM
            Vanilla(NPCID.GiantShelly, phm: 100, hm: 220, shm: 1100); // old phm 100 / hm 220 / shm 220; kept old HM: higher than PHM x2
            Vanilla(NPCID.GiantShelly2, phm: 100, hm: 220, shm: 1100); // old phm 100 / hm 220 / shm 220; kept old HM: higher than PHM x2
            Vanilla(NPCID.GiantWalkingAntlion, phm: 220, hm: 440, shm: 550); // old phm 220 / hm 220 (242 after Plantera) / shm 550; kept: has SHM scaling
            Vanilla(NPCID.GiantWormHead, phm: 120, hm: 240); // old phm 120 / hm 132 (264 after Plantera) / removed in SHM
            Vanilla(NPCID.Gnome, phm: 50, hm: 274, shm: 550); // old phm 50 / hm 274 (330 after Plantera) / shm 550; kept old HM: higher than PHM x2; kept: has SHM scaling
            Vanilla(NPCID.GoblinScout, phm: 160, hm: 320, shm: 1600); // old phm 160 / hm 176 (352 after Plantera) / shm 352
            Vanilla(NPCID.GoldenSlime, phm: 600, hm: 1200, shm: 6000); // old phm 600 / hm 600 (660 after Plantera) / shm 660
            Vanilla(NPCID.GraniteFlyer, phm: 80, hm: 160, shm: 440); // old phm 80 / hm 88 (176 after Plantera) / shm 440; kept: has SHM scaling
            Vanilla(NPCID.GraniteGolem, phm: 220, hm: 440, shm: 1100); // old phm 220 / hm 242 / shm 1100; kept: has SHM scaling
            Vanilla(NPCID.GreekSkeleton, phm: 140, hm: 280, shm: 700); // old phm 140 / hm 154 (308 after Plantera) / shm 700; kept: has SHM scaling
            Vanilla(NPCID.GreenEye, phm: 120, hm: 264); // old phm 120 / hm 264 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.Harpy, phm: 200, hm: 400, shm: 1000); // old phm 200 / hm 220 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.Hellbat, phm: 92, hm: 184); // old phm 92 / hm 100 / removed in SHM
            Vanilla(NPCID.Hornet, phm: 96, hm: 192); // old phm 96 / hm 104 (210 after Plantera) / removed in SHM
            Vanilla(NPCID.HornetFatty, phm: 100, hm: 200, shm: 500); // old phm 100 / hm 110 (220 after Plantera) / shm 500; kept: has SHM scaling
            Vanilla(NPCID.HornetHoney, phm: 84, hm: 168, shm: 420); // old phm 84 / hm 92 (184 after Plantera) / shm 420; kept: has SHM scaling
            Vanilla(NPCID.HornetLeafy, phm: 76, hm: 152, shm: 380); // old phm 76 / hm 82 / shm 380; kept: has SHM scaling
            Vanilla(NPCID.HornetSpikey, phm: 84, hm: 168, shm: 420); // old phm 84 / hm 92 (184 after Plantera) / shm 420; kept: has SHM scaling
            Vanilla(NPCID.HornetStingy, phm: 76, hm: 152, shm: 418); // old phm 76 / hm 82 (166 after Plantera) / shm 418; kept: has SHM scaling
            Vanilla(NPCID.IceBat, phm: 60, hm: 132); // old phm 60 / hm 132 (198 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.IceSlime, phm: 60, hm: 264, shm: 1320); // old phm 60 / hm 264 (330 after Plantera) / shm 330; kept old HM: higher than PHM x2
            Vanilla(NPCID.JungleBat, phm: 68, hm: 148); // old phm 68 / hm 148 (224 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.LacBeetle, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.LarvaeAntlion, phm: 60, hm: 264, shm: 1320); // old phm 60 / hm 264 (330 after Plantera) / shm 330; kept old HM: higher than PHM x2
            Vanilla(NPCID.LavaSlime, phm: 100, hm: 220); // old phm 100 / hm 220 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.LostGirl, phm: 1500, hm: 3000, shm: 15000); // old phm 1500 / hm 1500 / shm 1500
            Vanilla(NPCID.ManEater, phm: 260, hm: 520); // old phm 260 / hm 260 (286 after Plantera) / removed in SHM
            Vanilla(NPCID.MeteorHead, phm: 52, hm: 104, shm: 260); // old phm 52 / hm 52 (56 after Plantera) / shm 260; kept: has SHM scaling
            Vanilla(NPCID.MotherSlime, phm: 180, hm: 360); // old phm 180 / hm 198 (396 after Plantera) / removed in SHM
            Vanilla(NPCID.MushiLadybug, phm: 440, hm: 880, shm: 1100); // old phm 440 / hm 440 / shm 1100; kept: has SHM scaling
            Vanilla(NPCID.Nymph, phm: 1500, hm: 3000, shm: 15000); // old phm 1500 / hm 1500 / shm 1500
            Vanilla(NPCID.PinkJellyfish, phm: 140, hm: 280, shm: 350); // old phm 140 / hm 140 (154 after Plantera) / shm 350; kept: has SHM scaling
            Vanilla(NPCID.Piranha, phm: 60, hm: 132); // old phm 60 / hm 132 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.PurpleEye, phm: 120, hm: 264); // old phm 120 / hm 264 (396 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.Salamander, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander2, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander3, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander4, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander5, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander6, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander7, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander8, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.Salamander9, phm: 130, hm: 260, shm: 1300); // old phm 130 / hm 142 (286 after Plantera) / shm 286
            Vanilla(NPCID.SandSlime, phm: 100, hm: 220); // old phm 100 / hm 220 (330 after Plantera) / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.SeaSnail, phm: 80, hm: 176, shm: 880); // old phm 80 / hm 176 / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.Shark, phm: 1000, hm: 2000, shm: 2500); // old phm 1000 / hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.ShimmerSlime, phm: 2500, hm: 5000, shm: 25000); // old phm 2500 / hm 2500 / shm 2500
            Vanilla(NPCID.Skeleton, phm: 120, hm: 240, shm: 600); // old phm 120 / hm 132 (264 after Plantera) / shm 600; kept: has SHM scaling
            Vanilla(NPCID.SlimeSpiked, phm: 100, hm: 220, shm: 1100); // old phm 100 / hm 220 (330 after Plantera) / shm 330; kept old HM: higher than PHM x2
            Vanilla(NPCID.Snatcher, phm: 120, hm: 240); // old phm 120 / hm 132 / removed in SHM
            Vanilla(NPCID.SnowFlinx, phm: 140, hm: 280); // old phm 140 / hm 154 / removed in SHM
            Vanilla(NPCID.SpikedIceSlime, phm: 120, hm: 264); // old phm 120 / hm 264 / removed in SHM; kept old HM: higher than PHM x2
            Vanilla(NPCID.SpikedJungleSlime, phm: 130, hm: 260); // old phm 130 / hm 142 / removed in SHM
            Vanilla(NPCID.SporeBat, phm: 32, hm: 140, shm: 700); // old phm 32 / hm 140 (176 after Plantera) / shm 176; kept old HM: higher than PHM x2
            Vanilla(NPCID.SporeSkeleton, phm: 120, hm: 240, shm: 1200); // old phm 120 / hm 132 (264 after Plantera) / shm 264
            Vanilla(NPCID.Squid, phm: 60, hm: 132, shm: 660); // old phm 60 / hm 132 (198 after Plantera) / shm 198; kept old HM: higher than PHM x2
            Vanilla(NPCID.Tim, phm: 4000, hm: 8000, shm: 40000); // old phm 4000 / hm 4000 / shm 4000
            Vanilla(NPCID.TombCrawlerHead, phm: 600, hm: 1200); // old phm 600 / hm 600 / removed in SHM
            Vanilla(NPCID.Tumbleweed, phm: 600, hm: 1200, shm: 6000); // old phm 600 / hm 600 / shm 600
            Vanilla(NPCID.UndeadMiner, phm: 140, hm: 280); // old phm 140 / hm 154 (308 after Plantera) / removed in SHM
            Vanilla(NPCID.UndeadViking, phm: 140, hm: 280); // old phm 140 / hm 154 / removed in SHM
            Vanilla(NPCID.VoodooDemon, phm: 500, hm: 1000, shm: 5000); // old phm 500 / hm 500 / shm 500
            Vanilla(NPCID.Vulture, phm: 1000, hm: 2000, shm: 2500); // old phm 1000 / hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.WalkingAntlion, phm: 160, hm: 320); // old phm 160 / hm 176 / removed in SHM
            Vanilla(NPCID.WallCreeper, phm: 160, hm: 320, shm: 1600); // old phm 160 / hm 176 / shm 176
            Vanilla(NPCID.WallCreeperWall, phm: 160, hm: 320); // old phm 160 / hm 176 / removed in SHM
            Vanilla(NPCID.ZombieMushroom, phm: 360, hm: 720, shm: 3600); // old phm 360 / hm 360 (396 after Plantera) / shm 396
            Vanilla(NPCID.ZombieMushroomHat, phm: 440, hm: 880, shm: 4400); // old phm 440 / hm 440 / shm 440

            // ---- Vanilla enemies that first appear in Hardmode (SHM = HM x5 unless the old values had their own) ----
            Vanilla(NPCID.AnglerFish, hm: 180, shm: 450); // old hm 180 / shm 450; kept: has SHM scaling
            Vanilla(NPCID.AngryNimbus, hm: 300, shm: 1500); // old hm 300 / shm 300; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.AngryTrapper, hm: 600, shm: 1500); // old hm 600 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.Arapaima, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.ArmoredSkeleton, hm: 520, shm: 1300); // old hm 520 / shm 1300; kept: has SHM scaling
            Vanilla(NPCID.ArmoredViking, hm: 1000, shm: 2500); // old hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.BigMimicCorruption, hm: 7000, shm: 10500); // old hm 7000 / shm 10500; kept: has SHM scaling
            Vanilla(NPCID.BigMimicCrimson, hm: 7000, shm: 10500); // old hm 7000 / shm 10500; kept: has SHM scaling
            Vanilla(NPCID.BigMimicHallow, hm: 7000, shm: 10500); // old hm 7000 / shm 10500; kept: has SHM scaling
            Vanilla(NPCID.BigMimicJungle, hm: 7000, shm: 10500); // old hm 7000 / shm 10500; kept: has SHM scaling
            Vanilla(NPCID.BlackRecluse, hm: 700, shm: 1750); // old hm 700 / shm 1750; kept: has SHM scaling
            Vanilla(NPCID.BlackRecluseWall, hm: 700, shm: 1750); // old hm 700 / shm 1750; kept: has SHM scaling
            Vanilla(NPCID.BloodFeeder, hm: 300, shm: 750); // old hm 300 / shm 750; kept: has SHM scaling
            Vanilla(NPCID.BloodJelly, hm: 300, shm: 750); // old hm 300 / shm 750; kept: has SHM scaling
            Vanilla(NPCID.BloodMummy, hm: 900, shm: 2250); // old hm 900 / shm 2250; kept: has SHM scaling
            Vanilla(NPCID.BlueArmoredBones, hm: 1000, shm: 2000); // old hm 1000 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.BlueArmoredBonesMace, hm: 700, shm: 1400); // old hm 700 / shm 1400; kept: has SHM scaling
            Vanilla(NPCID.BlueArmoredBonesNoPants, hm: 1100, shm: 2200); // old hm 1100 / shm 2200; kept: has SHM scaling
            Vanilla(NPCID.BlueArmoredBonesSword, hm: 1000, shm: 2000); // old hm 1000 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.BoneLee, hm: 2000, shm: 10000); // old hm 2000 / shm 2000
            Vanilla(NPCID.ChaosElemental, hm: 792, shm: 1980); // old hm 792 / shm 1980; kept: has SHM scaling
            Vanilla(NPCID.Clinger, hm: 1300, shm: 3250); // old hm 1300 / shm 3250; kept: has SHM scaling
            Vanilla(NPCID.Clown, hm: 8000, shm: 40000); // old hm 8000 / shm 8000
            Vanilla(NPCID.Corruptor, hm: 460, shm: 1150); // old hm 460 / shm 1150; kept: has SHM scaling
            Vanilla(NPCID.CorruptSlime, hm: 340, shm: 850); // old hm 340 / shm 850; kept: has SHM scaling
            Vanilla(NPCID.CrimsonAxe, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.CultistArcherBlue, hm: 420, shm: 2100); // old hm 420 / shm 420
            Vanilla(NPCID.CultistArcherWhite, hm: 420, shm: 2100); // old hm 420 / shm 420
            Vanilla(NPCID.CursedHammer, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.DarkMummy, hm: 360, shm: 900); // old hm 360 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.Derpling, hm: 600, shm: 1500); // old hm 600 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.DesertBeast, hm: 540, shm: 1350); // old hm 540 / shm 1350; kept: has SHM scaling
            Vanilla(NPCID.DesertDjinn, hm: 220, shm: 550); // old hm 220 / shm 550; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.DesertGhoul, hm: 360, shm: 900); // old hm 360 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.DesertGhoulCorruption, hm: 360, shm: 900); // old hm 360 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.DesertGhoulCrimson, hm: 2000, shm: 5000); // old hm 2000 / shm 5000; kept: has SHM scaling
            Vanilla(NPCID.DesertGhoulHallow, hm: 360, shm: 900); // old hm 360 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.DesertLamiaDark, hm: 700, shm: 3500); // old hm 700 / shm 700
            Vanilla(NPCID.DesertLamiaLight, hm: 700, shm: 3500); // old hm 700 / shm 700
            Vanilla(NPCID.DesertScorpionWalk, hm: 640, shm: 1600); // old hm 640 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.DesertScorpionWall, hm: 640, shm: 1600); // old hm 640 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.DiabolistRed, hm: 200, shm: 400); // old hm 200 / shm 400; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.DiabolistWhite, hm: 250, shm: 500); // old hm 250 / shm 500; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.DiggerHead, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.DuneSplicerHead, hm: 1200, shm: 3000); // old hm 1200 / shm 3000; kept: has SHM scaling
            Vanilla(NPCID.DungeonSpirit, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.EnchantedSword, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.FlyingSnake, hm: 520, shm: 1300); // old hm 520 / shm 1300; kept: has SHM scaling
            Vanilla(NPCID.FungoFish, hm: 280, shm: 700); // old hm 280 / shm 700; kept: has SHM scaling
            Vanilla(NPCID.Gastropod, hm: 220, shm: 550); // old hm 220 / shm 550; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.GiantBat, hm: 210, shm: 524); // old hm 210 (230 after Plantera) / shm 524; kept: has SHM scaling
            Vanilla(NPCID.GiantCursedSkull, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.GiantFlyingFox, hm: 440, shm: 1100); // old hm 440 / shm 1100; kept: has SHM scaling
            Vanilla(NPCID.GiantFungiBulb, hm: 1000, shm: 2500); // old hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.GiantTortoise, hm: 940, shm: 2350); // old hm 940 / shm 2350; kept: has SHM scaling
            Vanilla(NPCID.GreenJellyfish, hm: 240, shm: 600); // old hm 240 / shm 600; kept: has SHM scaling
            Vanilla(NPCID.HellArmoredBones, hm: 800, shm: 1600); // old hm 800 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.HellArmoredBonesMace, hm: 1000, shm: 2000); // old hm 1000 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.HellArmoredBonesSpikeShield, hm: 900, shm: 1800); // old hm 900 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.HellArmoredBonesSword, hm: 1000, shm: 2000); // old hm 1000 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.IceElemental, hm: 200, shm: 500); // old hm 200 / shm 500; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.IceGolem, hm: 12000, shm: 18000); // old hm 12000 / shm 18000; kept: has SHM scaling
            Vanilla(NPCID.IceMimic, hm: 1000, shm: 2500); // old hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.IceTortoise, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.IchorSticker, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.IcyMerman, hm: 560, shm: 1400); // old hm 560 / shm 1400; kept: has SHM scaling
            Vanilla(NPCID.IlluminantBat, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.IlluminantSlime, hm: 360, shm: 900); // old hm 360 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.JungleCreeper, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.JungleCreeperWall, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.Lavabat, hm: 320, shm: 800); // old hm 320 / shm 800; kept: has SHM scaling
            Vanilla(NPCID.LightMummy, hm: 400, shm: 1000); // old hm 400 / shm 1000; kept: has SHM scaling
            Vanilla(NPCID.Lihzahrd, hm: 800, shm: 1600); // old hm 800 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.LihzahrdCrawler, hm: 800, shm: 1600); // old hm 800 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.Medusa, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.Mimic, hm: 1000, shm: 2500); // old hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.MossHornet, hm: 440, shm: 1100); // old hm 440 / shm 1100; kept: has SHM scaling
            Vanilla(NPCID.Moth, hm: 2000, shm: 3000); // old hm 2000 / shm 3000; kept: has SHM scaling
            Vanilla(NPCID.Mummy, hm: 260, shm: 650); // old hm 260 (286 after Plantera) / shm 650; kept: has SHM scaling
            Vanilla(NPCID.Necromancer, hm: 300, shm: 600); // old hm 300 / shm 600; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.NecromancerArmored, hm: 450, shm: 900); // old hm 450 / shm 900; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.Paladin, hm: 10000, shm: 15000); // old hm 10000 / shm 15000; kept: has SHM scaling
            Vanilla(NPCID.Pixie, hm: 300, shm: 750); // old hm 300 / shm 750; kept: has SHM scaling
            Vanilla(NPCID.PossessedArmor, hm: 520, shm: 1300); // old hm 520 / shm 1300; kept: has SHM scaling
            Vanilla(NPCID.RaggedCaster, hm: 400, shm: 800); // old hm 400 / shm 800; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.RaggedCasterOpenCoat, hm: 450, shm: 900); // old hm 450 / shm 900; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.RainbowSlime, hm: 1600, shm: 4000); // old hm 1600 / shm 4000; kept: has SHM scaling
            Vanilla(NPCID.RedDevil, hm: 4000, shm: 20000); // old hm 4000 / shm 4000
            Vanilla(NPCID.RockGolem, hm: 2000, shm: 3000); // old hm 2000 / shm 3000; kept: has SHM scaling
            Vanilla(NPCID.RuneWizard, hm: 600, shm: 3000); // old hm 600 / shm 600; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.RustyArmoredBonesAxe, hm: 1100, shm: 2200); // old hm 1100 / shm 2200; kept: has SHM scaling
            Vanilla(NPCID.RustyArmoredBonesFlail, hm: 800, shm: 1600); // old hm 800 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.RustyArmoredBonesSword, hm: 900, shm: 1800); // old hm 900 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.RustyArmoredBonesSwordNoArmor, hm: 800, shm: 1600); // old hm 800 / shm 1600; kept: has SHM scaling
            Vanilla(NPCID.SandElemental, hm: 14000, shm: 21000); // old hm 14000 / shm 21000; kept: has SHM scaling
            Vanilla(NPCID.SandShark, hm: 720, shm: 1800); // old hm 720 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.SandsharkCorrupt, hm: 760, shm: 1900); // old hm 760 / shm 1900; kept: has SHM scaling
            Vanilla(NPCID.SandsharkCrimson, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.SandsharkHallow, hm: 900, shm: 2250); // old hm 900 / shm 2250; kept: has SHM scaling
            Vanilla(NPCID.SeekerHead, hm: 2000, shm: 5000); // old hm 2000 / shm 5000; kept: has SHM scaling
            Vanilla(NPCID.SkeletonArcher, hm: 210, shm: 525); // old hm 210 / shm 525; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.Slimer, hm: 120, shm: 300); // old hm 120 / shm 300; kept: has SHM scaling
            Vanilla(NPCID.Unicorn, hm: 800, shm: 2000); // old hm 800 / shm 2000; kept: has SHM scaling
            Vanilla(NPCID.WanderingEye, hm: 600, shm: 1500); // old hm 600 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.Werewolf, hm: 700, shm: 1750); // old hm 700 / shm 1750; kept: has SHM scaling
            Vanilla(NPCID.Wolf, hm: 600, shm: 1500); // old hm 600 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.Wraith, hm: 1000, shm: 2500); // old hm 1000 / shm 2500; kept: has SHM scaling
            Vanilla(NPCID.WyvernHead, hm: 12000, shm: 60000); // old hm 12000 / shm 12000

            // ---- Vanilla event: Blood Moon ----
            Vanilla(NPCID.BloodZombie, phm: 150, hm: 300, shm: 750); // old phm 150 / hm 164 (330 after Plantera) / shm 750; kept: has SHM scaling
            Vanilla(NPCID.CorruptBunny, phm: 140, hm: 280, shm: 1400); // old phm 140 / hm 154 / shm 154
            Vanilla(NPCID.CorruptGoldfish, phm: 200, hm: 400, shm: 2000); // old phm 200 / hm 220 / shm 220
            Vanilla(NPCID.CorruptPenguin, phm: 140, hm: 280, shm: 1400); // old phm 140 / hm 154 (308 after Plantera) / shm 308
            Vanilla(NPCID.CrimsonBunny, phm: 150, hm: 300, shm: 1500); // old phm 150 / hm 164 (330 after Plantera) / shm 330
            Vanilla(NPCID.CrimsonGoldfish, phm: 220, hm: 440, shm: 2200); // old phm 220 / hm 242 / shm 242
            Vanilla(NPCID.CrimsonPenguin, phm: 150, hm: 300, shm: 1500); // old phm 150 / hm 164 (330 after Plantera) / shm 330
            Vanilla(NPCID.Drippler, phm: 100, hm: 200, shm: 500); // old phm 100 / hm 110 / shm 500; kept: has SHM scaling
            Vanilla(NPCID.ZombieMerman, phm: 800, hm: 1600, shm: 8000); // old phm 800 / hm 800 / shm 800
            Vanilla(NPCID.BloodEelHead, hm: 12000, shm: 60000); // old hm 12000 / shm 12000
            Vanilla(NPCID.BloodNautilus, hm: 14000, shm: 70000); // old hm 14000 / shm 14000
            Vanilla(NPCID.BloodSquid, hm: 1500, shm: 7500); // old hm 1500 / shm 1500
            Vanilla(NPCID.GoblinShark, hm: 10000, shm: 50000); // old hm 10000 / shm 10000

            // ---- Vanilla event: Frost Legion ----
            Vanilla(NPCID.MisterStabby, hm: 480, shm: 2400); // old hm 480 / shm 480
            Vanilla(NPCID.SnowBalla, hm: 440, shm: 2200); // old hm 440 / shm 440
            Vanilla(NPCID.SnowmanGangsta, hm: 400, shm: 2000); // old hm 400 / shm 400

            // ---- Vanilla event: Frost Moon ----
            Vanilla(NPCID.Flocko, hm: 675, shm: 1012); // old hm 675 / shm 1012; kept: has SHM scaling
            Vanilla(NPCID.Krampus, hm: 3750, shm: 5625); // old hm 3750 / shm 5625; kept: has SHM scaling
            Vanilla(NPCID.PresentMimic, hm: 1350, shm: 2025); // old hm 1350 / shm 2025; kept: has SHM scaling
            Vanilla(NPCID.Yeti, hm: 5250, shm: 7875); // old hm 5250 / shm 7875; kept: has SHM scaling

            // ---- Vanilla event: Goblin Army ----
            Vanilla(NPCID.GoblinArcher, phm: 100, hm: 200, shm: 1000); // old phm 100 / hm 100 / shm 100; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.GoblinPeon, phm: 140, hm: 280, shm: 1400); // old phm 140 / hm 154 (308 after Plantera) / shm 308
            Vanilla(NPCID.GoblinSorcerer, phm: 80, hm: 160, shm: 800); // old phm 80 / hm 80 / shm 80; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.GoblinThief, phm: 200, hm: 400, shm: 2000); // old phm 200 / hm 220 / shm 220
            Vanilla(NPCID.GoblinWarrior, phm: 250, hm: 500, shm: 2500); // old phm 250 / hm 250 (274 after Plantera) / shm 274
            Vanilla(NPCID.GoblinSummoner, hm: 2000, shm: 3000); // old hm 2000 / shm 3000; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2

            // ---- Vanilla event: Halloween & Party ----
            Vanilla(NPCID.Ghost, phm: 100, hm: 220, shm: 1100); // old phm 100 / hm 220 (330 after Plantera) / shm 330; kept old HM: higher than PHM x2
            Vanilla(NPCID.Raven, phm: 70, hm: 230, shm: 1150); // old phm 70 / hm 230 (308 after Plantera) / shm 308; kept old HM: higher than PHM x2
            Vanilla(NPCID.SlimeMasked, phm: 50, hm: 274, shm: 1370); // old phm 50 / hm 274 (330 after Plantera) / shm 330; kept old HM: higher than PHM x2
            Vanilla(NPCID.ZombieDoctor, phm: 80, hm: 176, shm: 440); // old phm 80 / hm 176 / shm 440; kept old HM: higher than PHM x2; kept: has SHM scaling
            Vanilla(NPCID.ZombiePixie, phm: 68, hm: 136, shm: 340); // old phm 68 / hm 74 (148 after Plantera) / shm 340; kept: has SHM scaling

            // ---- Vanilla event: Lunar Events ----
            Vanilla(NPCID.NebulaBeast, hm: 1700, shm: 2550); // old hm 1700 / shm 2550; kept: has SHM scaling
            Vanilla(NPCID.NebulaBrain, hm: 1300, shm: 1950); // old hm 1300 / shm 1950; kept: has SHM scaling; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.NebulaHeadcrab, hm: 1000, shm: 1500); // old hm 1000 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.NebulaSoldier, hm: 1400, shm: 2100); // old hm 1400 / shm 2100; kept: has SHM scaling
            Vanilla(NPCID.SolarCorite, hm: 1200, shm: 1800); // old hm 1200 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.SolarDrakomire, hm: 1600, shm: 2400); // old hm 1600 / shm 2400; kept: has SHM scaling
            Vanilla(NPCID.SolarDrakomireRider, hm: 1600, shm: 2400); // old hm 1600 / shm 2400; kept: has SHM scaling
            Vanilla(NPCID.SolarSolenian, hm: 1600, shm: 2400); // old hm 1600 / shm 2400; kept: has SHM scaling
            Vanilla(NPCID.SolarSpearman, hm: 2000, shm: 10000); // old hm 2000 / shm 2000
            Vanilla(NPCID.SolarSroller, hm: 1400, shm: 2100); // old hm 1400 / shm 2100; kept: has SHM scaling
            Vanilla(NPCID.StardustCellBig, hm: 600, shm: 900); // old hm 600 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.StardustCellSmall, hm: 600, shm: 900); // old hm 600 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.StardustJellyfishBig, hm: 3000, shm: 4500); // old hm 3000 / shm 4500; kept: has SHM scaling
            Vanilla(NPCID.StardustSoldier, hm: 1400, shm: 2100); // old hm 1400 / shm 2100; kept: has SHM scaling
            Vanilla(NPCID.StardustSpiderBig, hm: 1600, shm: 2400); // old hm 1600 / shm 2400; kept: has SHM scaling
            Vanilla(NPCID.StardustSpiderSmall, hm: 400, shm: 600); // old hm 400 / shm 600; kept: has SHM scaling
            Vanilla(NPCID.StardustWormHead, hm: 2400, shm: 3600); // old hm 2400 / shm 3600; kept: has SHM scaling
            Vanilla(NPCID.VortexHornet, hm: 1000, shm: 1500); // old hm 1000 / shm 1500; kept: has SHM scaling
            Vanilla(NPCID.VortexHornetQueen, hm: 2000, shm: 3000); // old hm 2000 / shm 3000; kept: has SHM scaling
            Vanilla(NPCID.VortexLarva, hm: 400, shm: 600); // old hm 400 / shm 600; kept: has SHM scaling
            Vanilla(NPCID.VortexRifleman, hm: 1600, shm: 2400); // old hm 1600 / shm 2400; kept: has SHM scaling
            Vanilla(NPCID.VortexSoldier, hm: 1400, shm: 2100); // old hm 1400 / shm 2100; kept: has SHM scaling

            // ---- Vanilla event: Martian Madness ----
            Vanilla(NPCID.BrainScrambler, hm: 700, shm: 1050); // old hm 700 / shm 1050; kept: has SHM scaling
            Vanilla(NPCID.GigaZapper, hm: 1200, shm: 1800); // old hm 1200 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.GrayGrunt, hm: 1500, shm: 2250); // old hm 1500 / shm 2250; kept: has SHM scaling
            Vanilla(NPCID.MartianDrone, hm: 600, shm: 900); // old hm 600 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.MartianEngineer, hm: 800, shm: 1200); // old hm 800 / shm 1200; kept: has SHM scaling
            Vanilla(NPCID.MartianOfficer, hm: 600, shm: 900); // old hm 600 / shm 900; kept: has SHM scaling
            Vanilla(NPCID.MartianProbe, shm: 1000); // old shm 1000
            Vanilla(NPCID.MartianTurret, hm: 400, shm: 600); // old hm 400 / shm 600; kept: has SHM scaling
            Vanilla(NPCID.MartianWalker, hm: 4000, shm: 6000); // old hm 4000 / shm 6000; kept: has SHM scaling
            Vanilla(NPCID.RayGunner, hm: 700, shm: 1050); // old hm 700 / shm 1050; kept: has SHM scaling
            Vanilla(NPCID.Scutlix, hm: 1200, shm: 1800); // old hm 1200 / shm 1800; kept: has SHM scaling
            Vanilla(NPCID.ScutlixRider, hm: 700, shm: 1050); // old hm 700 / shm 1050; kept: has SHM scaling

            // ---- Vanilla event: Old One's Army ----
            Vanilla(NPCID.DD2DarkMageT1, phm: 1000, hm: 2000, shm: 10000); // old phm 1000 / hm 1000 / shm 1000; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.DD2GoblinBomberT1, phm: 100, hm: 200, shm: 1000); // old phm 100 / hm 110 / shm 110
            Vanilla(NPCID.DD2GoblinT1, phm: 60, hm: 132, shm: 660); // old phm 60 / hm 132 / shm 132; kept old HM: higher than PHM x2
            Vanilla(NPCID.DD2JavelinstT1, phm: 120, hm: 240, shm: 1200); // old phm 120 / hm 132 / shm 132
            Vanilla(NPCID.DD2SkeletonT1, phm: 50, hm: 110, shm: 550); // old phm 50 / hm 110 / shm 110; kept old HM: higher than PHM x2
            Vanilla(NPCID.DD2WyvernT1, phm: 120, hm: 240, shm: 1200); // old phm 120 / hm 132 (264 after Plantera) / shm 264
            Vanilla(NPCID.DD2DarkMageT3, hm: 4000, shm: 20000); // old hm 4000 / shm 4000; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.DD2DrakinT2, hm: 1800, shm: 9000); // old hm 1800 / shm 1800
            Vanilla(NPCID.DD2DrakinT3, hm: 6000, shm: 30000); // old hm 6000 / shm 6000
            Vanilla(NPCID.DD2GoblinBomberT2, hm: 400, shm: 2000); // old hm 400 / shm 400
            Vanilla(NPCID.DD2GoblinBomberT3, hm: 1400, shm: 7000); // old hm 1400 / shm 1400
            Vanilla(NPCID.DD2GoblinT2, hm: 340, shm: 1700); // old hm 340 / shm 340
            Vanilla(NPCID.DD2GoblinT3, hm: 1120, shm: 5600); // old hm 1120 / shm 1120
            Vanilla(NPCID.DD2JavelinstT2, hm: 600, shm: 3000); // old hm 600 / shm 600
            Vanilla(NPCID.DD2JavelinstT3, hm: 2000, shm: 10000); // old hm 2000 / shm 2000
            Vanilla(NPCID.DD2KoboldFlyerT2, hm: 340, shm: 1700); // old hm 340 / shm 340
            Vanilla(NPCID.DD2KoboldFlyerT3, hm: 1160, shm: 5800); // old hm 1160 / shm 1160
            Vanilla(NPCID.DD2KoboldWalkerT2, hm: 520, shm: 2600); // old hm 520 / shm 520
            Vanilla(NPCID.DD2KoboldWalkerT3, hm: 1600, shm: 8000); // old hm 1600 / shm 1600
            Vanilla(NPCID.DD2LightningBugT3, hm: 1000, shm: 5000); // old hm 1000 / shm 1000
            Vanilla(NPCID.DD2OgreT2, hm: 10000, shm: 50000); // old hm 10000 / shm 10000
            Vanilla(NPCID.DD2OgreT3, hm: 26000, shm: 130000); // old hm 26000 / shm 26000
            Vanilla(NPCID.DD2SkeletonT3, hm: 960, shm: 4800); // old hm 960 / shm 960
            Vanilla(NPCID.DD2WitherBeastT2, hm: 1000, shm: 5000); // old hm 1000 / shm 1000
            Vanilla(NPCID.DD2WitherBeastT3, hm: 2800, shm: 14000); // old hm 2800 / shm 2800
            Vanilla(NPCID.DD2WyvernT2, hm: 360, shm: 1800); // old hm 360 / shm 360
            Vanilla(NPCID.DD2WyvernT3, hm: 1200, shm: 6000); // old hm 1200 / shm 1200

            // ---- Vanilla event: Pirate Invasion ----
            Vanilla(NPCID.Parrot, hm: 200, shm: 1000); // old hm 200 / shm 200
            Vanilla(NPCID.PirateCaptain, hm: 6000, shm: 30000); // old hm 6000 / shm 6000
            Vanilla(NPCID.PirateCorsair, hm: 900, shm: 4500); // old hm 900 / shm 900
            Vanilla(NPCID.PirateCrossbower, hm: 700, shm: 3500); // old hm 700 / shm 700
            Vanilla(NPCID.PirateDeadeye, hm: 450, shm: 2250); // old hm 450 / shm 450
            Vanilla(NPCID.PirateDeckhand, hm: 600, shm: 3000); // old hm 600 / shm 600

            // ---- Vanilla event: Pumpkin Moon ----
            Vanilla(NPCID.HeadlessHorseman, hm: 6500, shm: 9750); // old hm 6500 / shm 9750; kept: has SHM scaling
            Vanilla(NPCID.Hellhound, hm: 2700, shm: 4050); // old hm 2700 / shm 4050; kept: has SHM scaling
            Vanilla(NPCID.Poltergeist, hm: 1875, shm: 2812); // old hm 1875 / shm 2812; kept: has SHM scaling
            Vanilla(NPCID.Splinterling, hm: 1800, shm: 2700); // old hm 1800 / shm 2700; kept: has SHM scaling

            // ---- Vanilla event: Solar Eclipse ----
            Vanilla(NPCID.Butcher, hm: 1400, shm: 7000); // old hm 1400 / shm 1400
            Vanilla(NPCID.CreatureFromTheDeep, hm: 800, shm: 4000); // old hm 800 / shm 800
            Vanilla(NPCID.DeadlySphere, hm: 50, shm: 250); // old hm 50 / shm 50
            Vanilla(NPCID.DrManFly, hm: 500, shm: 2500); // old hm 500 / shm 500; 0 contact damage, so vanilla skipped the Expert x2
            Vanilla(NPCID.Fritz, hm: 540, shm: 2700); // old hm 540 / shm 540
            Vanilla(NPCID.Mothron, hm: 12000, shm: 60000); // old hm 12000 / shm 12000
            Vanilla(NPCID.MothronSpawn, hm: 1400, shm: 7000); // old hm 1400 / shm 1400
            Vanilla(NPCID.Reaper, hm: 1400, shm: 7000); // old hm 1400 / shm 1400
            Vanilla(NPCID.SwampThing, hm: 900, shm: 4500); // old hm 900 / shm 900
            Vanilla(NPCID.ThePossessed, hm: 1200, shm: 6000); // old hm 1200 / shm 1200
            Vanilla(NPCID.Vampire, hm: 1500, shm: 7500); // old hm 1500 / shm 1500
            Vanilla(NPCID.VampireBat, hm: 1500, shm: 7500); // old hm 1500 / shm 1500

            // Not registered:
            //   Always removed by the VanillaChanges.AI block list (zombies, slimes, Christmas / Halloween extras, Bound NPCs, ...).
            //   Bosses, boss parts and minions (Destroyer / EoW segments, Creeper, Prime arms, Golem parts, Plantera hooks, Moon Lord
            //   parts, Hungry, Probes, Bees, Sharkrons, Queen Slime minions, Cultist clones, Martian Saucer parts, lunar towers).
            //   Event bosses with boss bars: Pumpking, Mourning Wood, Everscream, Santa-NK1, Ice Queen, Betsy.
            //   Body and tail segments of vanilla worms: every one but Eater of Worlds shares the head's life (realLife), so the head entry covers it.
            //   Not real enemies: Dungeon Guardian, Chattering Teeth Bomb, Force Bubble, Lunatic Devotee, Mothron Egg, Pirate Ship Cannon,
            //   Tortured Soul, DD2 test NPC, critters, town NPCs, projectile NPCs.
        }
    }

    /// <summary>Applies the registries. Vanilla types' SetDefaults work happens at the end of VanillaChanges.SetDefaults,
    /// because that hook assigns absolute lifeMax values and would otherwise overwrite this one depending on hook order.
    /// Boss multipliers for modded types are applied here in SetDefaults; regular enemies are applied from
    /// tsorcRevampGlobalNPC.SetDefaults (modded) or VanillaChanges.SetDefaults (vanilla) in Normal worlds, and from
    /// ApplyDifficultyAndPlayerScaling below (both) in Expert and Master.</summary>
    public class EnemyBalanceNPC : GlobalNPC
    {
        public override void SetDefaults(NPC npc)
        {
            if (npc.type >= NPCID.Count)
            {
                EnemyBalance.ApplyLifeMultiplier(npc);
            }
        }

        // Runs inside vanilla's NPC.ScaleStats, after the Hardmode stat-budget bump and the Expert x2 / Master x3 life multiplier.
        // Replaces the result with the registry's Expert value scaled by the mode (Expert 1, Master 1.275) and by world progress
        // (ProgressionScaling.LifeScale, inside ModeScaledLife).
        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
        {
            if (npc.boss)
            {
                return;
            }

            if (npc.ModNPC is NPCs.Puppets.PuppetNPC puppet && puppet.UsesPuppetDifficultyScaling)
            {
                return;
            }

            if (!EnemyBalance.TryGetEnemyExpertHp(npc.type, out int expertHp))
            {
                return;
            }

            npc.lifeMax = EnemyBalance.ModeScaledLife(npc, expertHp);
        }

        // Debug aid: with DebugMode on, every registered spawn logs its final health so the registry can be checked in game.
        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
        {
            tsorcRevampGameplayConfig config = ModContent.GetInstance<tsorcRevampGameplayConfig>();
            if (config == null || !config.DebugMode)
            {
                return;
            }

            bool hasEnemyHp = EnemyBalance.TryGetEnemyExpertHp(npc.type, out int expertHp);
            float multiplier = EnemyBalance.LifeMultiplier(npc.type);

            if (hasEnemyHp)
            {
                Mod.Logger.Info($"EnemyBalance: {npc.TypeName} spawned with lifeMax {npc.lifeMax} (registry Expert HP {expertHp}, game mode {Main.GameMode}).");
            }
            else if (multiplier != 1f)
            {
                Mod.Logger.Info($"EnemyBalance: {npc.TypeName} spawned with lifeMax {npc.lifeMax} (x{multiplier:0.###}, game mode {Main.GameMode}).");
            }
        }
    }
}
