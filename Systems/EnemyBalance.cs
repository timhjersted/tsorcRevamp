using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// Registry of enemy health retunes, switched by the "New Enemy Balance" gameplay config (default on).
    ///
    /// Every entry is the Expert health the enemy had before the pass and the Expert health it should have now.
    /// The pair becomes a multiplier on lifeMax during SetDefaults, which runs BEFORE vanilla's difficulty scaling
    /// and the mod's own Normal / Master / SHM / multiplayer scaling, so all of those still stack on top of the
    /// new number. "Old" must be the Expert health actually seen on the boss bar (solo, before SHM scaling),
    /// not the constant in the class: some classes bake in their own multipliers, so the two differ.
    ///
    /// Bosses that reset lifeMax mid-fight (Pinwheel and Dark Cloud phase 2, The Machine) read
    /// LifeMultiplier / ScaleLife themselves so the retune follows them.
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

        public override void PostSetupContent()
        {
            LifeMultipliers.Clear();
            PhaseLifeMultipliers.Clear();
            BuildRegistry();
        }

        public override void Unload()
        {
            LifeMultipliers.Clear();
            PhaseLifeMultipliers.Clear();
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
            RegisterMod("Gwyn", 750000, 1000000);
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
    }

    /// <summary>Applies the registry to modded NPCs. Vanilla types are applied at the end of VanillaChanges.SetDefaults,
    /// because that hook assigns absolute lifeMax values and would otherwise overwrite this one depending on hook order.</summary>
    public class EnemyBalanceNPC : GlobalNPC
    {
        public override void SetDefaults(NPC npc)
        {
            if (npc.type >= NPCID.Count)
            {
                EnemyBalance.ApplyLifeMultiplier(npc);
            }
        }

        // Debug aid: with DebugMode on, every registered spawn logs its final health so the registry can be checked in game.
        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
        {
            tsorcRevampGameplayConfig config = ModContent.GetInstance<tsorcRevampGameplayConfig>();
            if (config == null || !config.DebugMode || EnemyBalance.LifeMultiplier(npc.type) == 1f)
            {
                return;
            }

            Mod.Logger.Info($"EnemyBalance: {npc.TypeName} spawned with lifeMax {npc.lifeMax} (x{EnemyBalance.LifeMultiplier(npc.type):0.###}, game mode {Main.GameMode}).");
        }
    }
}
