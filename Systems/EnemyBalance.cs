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
    /// enemy ends up with exactly that much, in Master 1.5x, in a Normal world half (the same 1 : 2 : 3 ratios as
    /// vanilla), and Super Hardmode natives still get their x1.0-1.5 SHM scaling on top. The registry value replaces
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

        /// <summary>The registry's Expert health converted for the current game mode (Normal 0.5, Expert 1, Master 1.5) and, for Super Hardmode natives, the SHM scaling.</summary>
        public static int ModeScaledLife(NPC npc, int expertHp)
        {
            float modeMultiplier = Main.GameModeInfo.EnemyMaxLifeMultiplier / 2f;
            float shmScale = 1f;

            if (npc.ModNPC != null && npc.ModNPC.GetType().Namespace.Contains("SuperHardMode"))
            {
                shmScale = tsorcRevampWorld.SHMScale;
            }

            return (int)Math.Round(expertHp * modeMultiplier * shmScale);
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
        /// Called from tsorcRevampGlobalNPC.SetDefaults, before its SHM scaling block, so SHMScale still multiplies the
        /// value in a Normal world. Expert and Master are finished in EnemyBalanceNPC.ApplyDifficultyAndPlayerScaling,
        /// after vanilla's stat-budget bump has used the class's own lifeMax for damage and defense.
        /// </summary>
        public static void ApplyEnemySetDefaults(NPC npc)
        {
            if (npc.type < NPCID.Count || !TryGetEnemyExpertHp(npc.type, out int expertHp))
            {
                return;
            }

            bool isPuppet = npc.ModNPC is NPCs.Puppets.PuppetNPC puppet && puppet.UsesPuppetDifficultyScaling;

            if (isPuppet)
            {
                // Puppets already halve vanilla's Expert x2 in the mod's own hook, so the authored value is the Expert value everywhere.
                npc.lifeMax = expertHp;
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
                npc.lifeMax = (int)Math.Round(expertHp / 2f);
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
            RegisterMod("Kahlrun", 1500, 1800);
            RegisterMod("VesselOfSouls", 6000, 6000); // base raised in VesselOfSouls.cs (was 3000), so no multiplier
            RegisterMod("RedKnight", 3000, 3000); // base raised in RedKnight.cs (was 2500); scripted events override HP via SetCustomStats
            RegisterMod("OwlFather", 4000, 10000);
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
            RegisterMod("BlackNinja", 6000, 8000); // base raised in BlackNinja.cs (was 1800 -> 6000), registry then takes it to 8000
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
            RegisterMod("MechaDragonHead", 61000, 60000);
            RegisterMod("HeroofLumelia", 8000, 50000);
            RegisterMod("Death", 32000, 60000);
            RegisterMod("SerrisHead", 6000, 6000); // unchanged: its damage is gated by a unique mechanic
            RegisterMod("SerrisX", 10000, 10000); // unchanged, same reason
            RegisterMod("TheHunter", 35000, 90000);

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
            RegisterMod("Gwyn", 750000, 750000); // unchanged
            RegisterMod("HellkiteDragonHead", 650000, 650000); // unchanged
            RegisterMod("WyvernMageShadow", 250000, 250000); // unchanged; its Ghost Dragon (600000) is the larger part of that fight
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
        /// Super Hardmode natives show their health before the x1.0-1.5 SHM scaling, which still applies on top.
        /// </summary>
        private void BuildEnemyRegistry()
        {
            // ---- Enemies that first appear before Hardmode (their HM / SHM values follow on the same line) ----
            Enemy("SnowOwl", phm: 150, hm: 250, shm: 3000); // authored 15 / 50 / 50
            Enemy("UndeadCaster", phm: 100); // authored 30; caster, contact damage 0
            EnemyVariant("UndeadCaster", EnemyStage.PreHardmode, () => NPC.downedBoss3, 500); // after Skeletron: authored 120
            EnemyVariant("UndeadCaster", EnemyStage.PreHardmode, () => NPC.downedBoss1, 300); // after the Eye of Cthulhu: authored 60
            Enemy("MutantToad", phm: 400, hm: 3000, shm: 12000); // authored 40 / 200 / 450
            Enemy("DworcFleshhunter", phm: 400, hm: 1200, shm: 8000); // authored 50 / 100 / 100
            Enemy("DworcVenomsniper", phm: 300, hm: 1500, shm: 9000); // authored 50 / 100 / 100
            Enemy("FirebombHollow", phm: 200, hm: 4000, shm: 12000); // authored 60 / 500 / 2000
            Enemy("StoneGolem", phm: 300, hm: 3000, shm: 10000); // authored 60 / 120 / 350
            Enemy("ArmoredWraith", phm: 250, shm: 5000); // authored 75 / 75
            Enemy("MountedSandsprog", phm: 475, hm: 5000, shm: 12000); // authored 75 / 300 / 900
            Enemy("Sandsprog", phm: 400, hm: 4000, shm: 10000); // authored 75 / 200 / 900
            Enemy("SandsprogMage", phm: 400, hm: 5000, shm: 11000); // authored 75 / 200 / 900
            Enemy("DungeonMage", phm: 500, shm: 13000); // authored 160 / 1050; caster, contact damage 0
            Enemy("MountedSandsprogMage", phm: 500, hm: 9000, shm: 15000); // authored 80 / 300 / 900
            Enemy("Parasprite", phm: 190, hm: 400, shm: 900); // authored 90 / 90 / 90
            Enemy("TibianValkyrie", phm: 300, hm: 6000, shm: 20000); // authored 90 / 260 / 700
            Enemy("BasiliskWalker", phm: 400, hm: 6000); // authored 100 / 250
            Enemy("GhostOfAHollowWarrior", phm: 300, hm: 6000, shm: 10000); // authored 100 / 250 / 1000
            Enemy("GhostOfTheForgottenWarrior", phm: 600, hm: 4000, shm: 12000); // authored 100 / 200 / 1000
            Enemy("HollowWarrior", phm: 300, hm: 3000, shm: 6000); // authored 100 / 250 / 1000
            Enemy("OolacileCultist", phm: 600, hm: 10000, shm: 40000); // authored 200 / 450 / 3000; caster, contact damage 0; puppet
            Enemy("TibianAmazon", phm: 500, hm: 3000); // authored 100 / 250
            Enemy("HollowSpearman", phm: 300, hm: 4000, shm: 10000); // authored 105 / 270 / 1000
            Enemy("AbandonedStump", phm: 250, hm: 2000, shm: 8000); // authored 120 / 240 / 500
            Enemy("FireLurker", phm: 1000, hm: 7500, shm: 20000); // authored 120 / 250 / 1200
            Enemy("ManHunter", phm: 400, hm: 2000, shm: 9000); // authored 125 / 250 / 600
            Enemy("GhostOfTheForgottenKnight", phm: 700, hm: 4000, shm: 12000); // authored 150 / 200 / 1000
            Enemy("BarrowWight", phm: 1000, hm: 5000, shm: 10000); // authored 180 / 180 / 180
            Enemy("AttraidiesIllusion", phm: 500); // authored 400; caster, contact damage 0
            Enemy("AttraidiesManifestation", phm: 500, hm: 10000); // authored 400 / 800; caster, contact damage 0
            Enemy("JungleSentree", phm: 450, hm: 1200, shm: 7000); // authored 200 / 400 / 650
            Enemy("Archdeacon", phm: 1500); // authored 500; caster, contact damage 0
            Enemy("DemonElemental", phm: 1000, hm: 7000, shm: 8000); // authored 250 / 500 / 2000
            Enemy("Eland", phm: 2000, hm: 10000, shm: 25000); // authored 500 / 1000 / 20000; caster, contact damage 0
            Enemy("HollowSoldier", phm: 750, hm: 6000, shm: 10000); // authored 250 / 500 / 1500
            Enemy("Necromancer", phm: 3000); // authored 800; caster, contact damage 0
            Enemy("QuaraPincher", phm: 2000, hm: 9000, shm: 26000); // authored 1200 / 1200 / 1200
            Enemy("RedCloudHunter", phm: 2000, hm: 8000, shm: 20000); // authored 600 / 700 / 2000
            Enemy("LothricKnight", phm: 2000, hm: 15000, shm: 33000); // authored 750 / 1400 / 2500
            Enemy("LothricSpearKnight", phm: 2000, hm: 15000, shm: 25000); // authored 750 / 1200 / 3000
            Enemy("Warlock", phm: 8000, hm: 9000); // authored 750 / 1500; caster, contact damage 0
            Enemy("BlackKnight", phm: 15000, hm: 30000, shm: 50000); // authored 1000 / 2000 / 4000
            Enemy("LothricBlackKnight", phm: 8000, hm: 18000, shm: 35000); // authored 1000 / 1500 / 5000
            Enemy("JungleWyvernJuvenileHead", phm: 3000, hm: 10000, shm: 12000); // authored 1250 / 2500 / 3000

            // ---- Enemies that first appear in Hardmode ----
            Enemy("CloudBat", hm: 900); // authored 100
            Enemy("Dunlending", phm: 50, hm: 1000, shm: 11000); // authored 45 / 400
            Enemy("Willowisp", hm: 1500, shm: 11000); // authored 150 / 350
            Enemy("ClericOfSorrow", hm: 10000, shm: 25000); // authored 400 / 1200; puppet
            Enemy("GhostOfTheDrowned", phm: 175, hm: 3500, shm: 15000); // authored 150 / 450 / 1300; caster, contact damage 0
            Enemy("ShadowMage", hm: 4500, shm: 12000); // authored 450 / 1350; caster, contact damage 0
            Enemy("QuaraHydromancer", hm: 9000, shm: 16000); // authored 250 / 250; caster, contact damage 0
            Enemy("Byakhee", hm: 2500, shm: 15000); // authored 300 / 700
            Enemy("BasiliskShifter", hm: 9000); // authored 350
            Enemy("DworcVoodooShaman", hm: 9000); // authored 750; caster, contact damage 0
            Enemy("DemonSpirit", phm: 800, hm: 5000); // authored 400
            Enemy("EvilEye", hm: 15000); // authored 400
            Enemy("Assassin", hm: 9000, shm: 20000); // authored 500 / 2000
            Enemy("CrazedDemonSpirit", phm: 1000, hm: 10000, shm: 12000); // authored 500 / 1000
            Enemy("DworcAlchemist", phm: 500, hm: 9000); // authored 250 / 500; caster, contact damage 0
            Enemy("Tonberry", hm: 5000, shm: 23000); // authored 1500 / 3500; caster, contact damage 0
            Enemy("RingedKnight", phm: 1000, hm: 18000, shm: 35000); // authored 400 / 800 / 2500
            Enemy("ParasyticWormHead", hm: 4000); // authored 1500
            Enemy("FallenNecromancer", hm: 20000); // authored 4000
            Enemy("MarilithSpiritTwin", hm: 30000); // authored 10000

            // ---- Enemies that first appear in Super Hardmode (HP shown before the x1.0-1.5 SHM scaling, which still applies) ----
            Enemy("Locust", shm: 900); // authored 300
            Enemy("ManOfWar", shm: 3000); // authored 800
            Enemy("DemonWheel", shm: 4000); // authored 1000
            Enemy("VampireBat", shm: 5000); // authored 1300
            Enemy("CrystalKnight", shm: 60000); // authored 2800; caster, contact damage 0
            Enemy("DarkKnight", shm: 50000); // authored 3000; caster, contact damage 0
            Enemy("AbyssLurker", shm: 30000); // authored 1600
            Enemy("DarkBloodKnight", shm: 50000); // authored 3200; caster, contact damage 0
            Enemy("HydrisElemental", shm: 10000); // authored 1600
            Enemy("DworcAbysswalker", shm: 15000); // authored 3500; caster, contact damage 0
            Enemy("HydrisNecromancer", shm: 30000); // authored 3500; caster, contact damage 0
            Enemy("CorruptedElemental", shm: 10000); // authored 2000
            Enemy("CorruptedHornet", shm: 9000); // authored 2000
            Enemy("GuardianCorruptor", shm: 12000); // authored 2200
            Enemy("IceSkeleton", shm: 8000); // authored 2200
            Enemy("BarrowWightNemesis", shm: 16000); // authored 2500
            Enemy("BasiliskHunter", shm: 31000); // authored 2500
            Enemy("OolacileSorcerer", shm: 15000); // authored 5500; caster, contact damage 0
            Enemy("GhostOfTheDarkmoonKnight", shm: 15000); // authored 3000
            Enemy("Tetsujin", shm: 26000); // authored 3400
            Enemy("OolacileDemon", shm: 16000); // authored 3500
            Enemy("SlograII", shm: 23000); // authored 4000
            Enemy("OolacileKnight", shm: 18000); // authored 5400
            Enemy("TaurusKnight", shm: 20000); // authored 5400
            Enemy("Plaguesmith", shm: 75000); // authored 8250
            Enemy("AncientDemonOfTheAbyss", shm: 35000); // authored 15000
            Enemy("Massacre", shm: 90000); // authored 20000
            Enemy("SerpentOfTheAbyssHead", shm: 50000); // authored 24000
            Enemy("GreatBlackKnight", shm: 150000); // authored 50000; caster, contact damage 0

            // ---- Events: no natural spawn (placed by the map, summoned by a boss or event, or spawned by another enemy), alphabetical ----
            Enemy("BarrowWightPhantom", shm: 18000); // authored 1250
            Enemy("DemonLordApocalypse", phm: 150000); // authored 40000
            Enemy("DestroyerLaserProbe", phm: 150, hm: 198, shm: 247); // authored 75 / 75 / 75
            Enemy("DiscipleOfAttraidies", phm: 6000); // authored 4500; caster, contact damage 0
            Enemy("FrozenGigasStatue", phm: 5000); // authored 250; caster, contact damage 0
            Enemy("Gigas", phm: 100000, hm: 100000, shm: 400000); // authored 32000 / 32000 / 32000
            Enemy("Hydra", phm: 400000); // authored 100000
            Enemy("IceGigas", phm: 50000, hm: 50000, shm: 350000); // authored 22000 / 22000 / 22000
            Enemy("KnightOfGwyn", shm: 200000); // authored 25000
            Enemy("MindflayerIllusion", phm: 1500); // authored 1000; caster, contact damage 0
            Enemy("MindflayerKingServant", phm: 200); // authored 200; caster, contact damage 0
            Enemy("MindflayerServant", phm: 70); // authored 70; caster, contact damage 0
            Enemy("MinotaurMage", phm: 900, hm: 6000, shm: 15000); // authored 155 / 155 / 155
            Enemy("PrimeLaserProbe", phm: 150, hm: 198, shm: 247); // authored 75 / 75 / 75
            Enemy("QuaraClutchCrab", phm: 500, hm: 1200, shm: 10000); // authored 400 / 400 / 400
            Enemy("QuaraMantassin", phm: 3000); // authored 800
            Enemy("Sahagin", phm: 500, hm: 5000, shm: 10000); // authored 44 / 44 / 44
            Enemy("SerpentOfTheAbyssBody", shm: 20000); // authored 10000
            Enemy("SerpentOfTheAbyssTail", shm: 20000); // authored 10000
            Enemy("SpellboundGhoul", phm: 900); // authored 150
            Enemy("WaterSpirit", phm: 1500); // authored 600

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
    }

    /// <summary>Applies the registry to modded NPCs. Vanilla types are applied at the end of VanillaChanges.SetDefaults,
    /// because that hook assigns absolute lifeMax values and would otherwise overwrite this one depending on hook order.
    /// Boss multipliers are applied here in SetDefaults; regular enemies are applied from tsorcRevampGlobalNPC.SetDefaults
    /// (Normal worlds) and ApplyDifficultyAndPlayerScaling below (Expert and Master).</summary>
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
        // Replaces the result with the registry's Expert value scaled by the mode (Expert 1, Master 1.5) and, for Super Hardmode
        // natives, the same SHMScale that tsorcRevampGlobalNPC.SetDefaults applies to their authored value.
        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
        {
            if (npc.boss || npc.type < NPCID.Count)
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
