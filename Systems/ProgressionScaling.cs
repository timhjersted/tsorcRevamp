using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.PrimeV2;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;

namespace tsorcRevamp.Systems
{
    /// <summary>
    /// World-progress scaling for enemies: how much tougher they get, and how much harder they hit, as bosses die.
    ///
    /// Progress is two boss counts read from NewSlain (server-synced to clients through WorldData) and cached:
    ///   - Hardmode: the seven bosses that gate Attraidies (Rage, Sorrow, Hunter, Destroyer, Triad, The Machine, Golem).
    ///   - Super Hardmode: the twelve SHM bosses, plus Gwyn (counted, but the ramps already cap at twelve).
    ///
    /// Enemy HP (LifeScale): every non-boss enemy in the EnemyBalance registry, modded and vanilla.
    ///   Before SHM it ramps 1.0 -> 2.0 over the Hardmode bosses. In SHM it starts again at 1.0 and ramps to 1.5 over the
    ///   SHM bosses; the registry's shm values are the bump at SHM start. Gated by the New Enemy Balance toggle.
    ///   Applied wherever EnemyBalance writes lifeMax (ModeScaledLife, ApplyEnemySetDefaults). Every machine runs SetDefaults
    ///   on its own NPC copy, and a client keeps its own lifeMax when the spawn packet says life == lifeMax, so the value must
    ///   come from synced world state, never from a server-only hook.
    ///
    /// Enemy damage (DamageTakenScale): one rule for every hit an NPC or hostile projectile lands on a player, bosses included,
    ///   contact and attacks alike: +2% per Hardmode boss and the SHM subtle ramp (+20% over the twelve) on top. This replaced
    ///   the per-class SHMScale multipliers on attack damage and the subtle ramp on contact damage. Always on.
    ///
    /// Bosses keep their own SHM HP and defense ramp in tsorcRevampGlobalNPC.SetDefaults (SHM namespace only).
    /// </summary>
    public static class ProgressionScaling
    {
        /// <summary>Enemy HP multiplier with all seven Hardmode bosses down. Linear in between: each adds (cap - 1) / 7.</summary>
        public const float HardmodeLifeScaleCap = 2.0f;

        /// <summary>Damage taken from enemies, per Hardmode boss down: 2% each, 14% at seven.</summary>
        public const float HardmodeDamageTakenPerBoss = 0.02f;

        public const int HardmodeBossCount = 7;

        /// <summary>SHM HP ramp cap (ShmScale): enemy HP in SHM, plus SHM boss HP and a few speeds and ranges.</summary>
        public const float ShmScaleCap = 1.5f;

        /// <summary>SHM subtle ramp cap (SubtleShmScale): damage taken, SHM defense, projectile and movement speeds.</summary>
        public const float SubtleShmScaleCap = 1.2f;

        /// <summary>SHM bosses the ramps divide by. Gwyn is the 13th and counted, but changes nothing past the cap.</summary>
        public const int ShmBossCount = 12;

        // Progress cache. The counts are read on every NPC spawn, every hit on a player and per tick by some AIs, and each
        // recount is ~20 dictionary lookups. The cache key is what can change a count: the NewSlain instance (world load,
        // client receiving world data), its size (an entry is added the first time each boss is slain) and the two vanilla
        // flags the Hardmode count falls back to.
        private static Dictionary<NPCDefinition, int> cachedSlainList;
        private static int cachedSlainCount = -1;
        private static bool cachedDestroyerFlag;
        private static bool cachedGolemFlag;
        private static int cachedHardmodeBossesDowned;
        private static int cachedShmBossesDowned;

        private static void RefreshIfStale()
        {
            Dictionary<NPCDefinition, int> slain = tsorcRevampWorld.NewSlain;

            if (slain == null)
            {
                cachedSlainList = null;
                cachedSlainCount = -1;
                cachedHardmodeBossesDowned = 0;
                cachedShmBossesDowned = 0;
                return;
            }

            bool isStale = slain != cachedSlainList
                || slain.Count != cachedSlainCount
                || NPC.downedMechBoss1 != cachedDestroyerFlag
                || NPC.downedGolemBoss != cachedGolemFlag;

            if (!isStale)
            {
                return;
            }

            cachedSlainList = slain;
            cachedSlainCount = slain.Count;
            cachedDestroyerFlag = NPC.downedMechBoss1;
            cachedGolemFlag = NPC.downedGolemBoss;

            // ---- Hardmode: the seven bosses that gate Attraidies ----
            int hardmodeCount = 0;

            int[] hardmodeModBosses =
            {
                ModContent.NPCType<TheRage>(),
                ModContent.NPCType<TheSorrow>(),
                ModContent.NPCType<TheHunter>(),
                ModContent.NPCType<TheMachine>()
            };

            foreach (int bossType in hardmodeModBosses)
            {
                if (slain.ContainsKey(new NPCDefinition(bossType)))
                {
                    hardmodeCount++;
                }
            }

            // Vanilla flags back up the vanilla bosses in case a kill never reached NewSlain (e.g. a map that ships them as downed).
            if (slain.ContainsKey(new NPCDefinition(NPCID.TheDestroyer)) || NPC.downedMechBoss1)
            {
                hardmodeCount++;
            }

            if (slain.ContainsKey(new NPCDefinition(NPCID.Golem)) || NPC.downedGolemBoss)
            {
                hardmodeCount++;
            }

            // Cataluminance writes both twins into NewSlain on death, so either record counts as the Triad.
            bool cataluminanceDown = slain.ContainsKey(new NPCDefinition(ModContent.NPCType<Cataluminance>()));
            bool twinsDown = slain.ContainsKey(new NPCDefinition(NPCID.Retinazer)) && slain.ContainsKey(new NPCDefinition(NPCID.Spazmatism));

            if (cataluminanceDown || twinsDown)
            {
                hardmodeCount++;
            }

            // ---- Super Hardmode: twelve bosses plus Gwyn ----
            int shmCount = 0;

            int[] shmBosses =
            {
                ModContent.NPCType<WaterFiendKraken>(),
                ModContent.NPCType<FireFiendMarilith>(),
                ModContent.NPCType<EarthFiendLich>(),
                ModContent.NPCType<WyvernMageShadow>(),
                ModContent.NPCType<HellkiteDragonHead>(),
                ModContent.NPCType<SeathTheScalelessHead>(),
                ModContent.NPCType<GrandOccultist>(),
                ModContent.NPCType<Artorias>(),
                ModContent.NPCType<Blight>(),
                ModContent.NPCType<Chaos>(),
                ModContent.NPCType<DarkCloud>(),
                ModContent.NPCType<Witchking>(),
                ModContent.NPCType<Gwyn>()
            };

            foreach (int bossType in shmBosses)
            {
                if (slain.ContainsKey(new NPCDefinition(bossType)))
                {
                    shmCount++;
                }
            }

            cachedHardmodeBossesDowned = hardmodeCount;
            cachedShmBossesDowned = shmCount;
        }

        /// <summary>How many of the seven Attraidies-gating bosses are dead, 0 to 7.</summary>
        public static int HardmodeBossesDowned
        {
            get
            {
                RefreshIfStale();
                return cachedHardmodeBossesDowned;
            }
        }

        /// <summary>How many SHM bosses are dead: 0 to 12, 13 with Gwyn. tsorcRevampWorld.SHMDowned forwards here.</summary>
        public static int ShmBossesDowned
        {
            get
            {
                RefreshIfStale();
                return cachedShmBossesDowned;
            }
        }

        /// <summary>1.0 with no SHM bosses down, linear to 1.5 at twelve. tsorcRevampWorld.SHMScale forwards here.</summary>
        public static float ShmScale
        {
            get
            {
                float progress = ShmBossesDowned / (float)ShmBossCount;

                return Math.Min(ShmScaleCap, 1f + ((ShmScaleCap - 1f) * progress));
            }
        }

        /// <summary>1.0 with no SHM bosses down, linear to 1.2 at twelve. tsorcRevampWorld.SubtleSHMScale forwards here.</summary>
        public static float SubtleShmScale
        {
            get
            {
                float progress = ShmBossesDowned / (float)ShmBossCount;

                return Math.Min(SubtleShmScaleCap, 1f + ((SubtleShmScaleCap - 1f) * progress));
            }
        }

        /// <summary>
        /// The HP multiplier an in-scope enemy gets right now: the Hardmode ramp before SHM, the SHM ramp in SHM, 1 with the
        /// toggle off. World-level (no eligibility check) so the balance logs can record it without an NPC; use LifeScale for one.
        /// </summary>
        public static float CurrentLifeScale
        {
            get
            {
                if (!EnemyBalance.Enabled)
                {
                    return 1f;
                }

                if (tsorcRevampWorld.SuperHardMode)
                {
                    return ShmScale;
                }

                float progress = Math.Min(HardmodeBossesDowned, HardmodeBossCount) / (float)HardmodeBossCount;

                return 1f + ((HardmodeLifeScaleCap - 1f) * progress);
            }
        }

        /// <summary>
        /// HP multiplier for this NPC from world progress: CurrentLifeScale for a non-boss with a registry value (modded or vanilla),
        /// 1 for everything else. The registry already leaves out boss parts, critters, town NPCs and block-listed vanilla enemies.
        /// </summary>
        public static float LifeScale(NPC npc)
        {
            if (npc.boss)
            {
                return 1f;
            }

            // Same predicate every registry write site uses, so "has a registry value" and "gets the scale" can never disagree.
            if (!EnemyBalance.TryGetEnemyExpertHp(npc.type, out _))
            {
                return 1f;
            }

            return CurrentLifeScale;
        }

        /// <summary>
        /// Multiplier on damage the player takes from NPCs and hostile projectiles: +2% per Hardmode boss, plus the SHM subtle
        /// ramp's bonus on top (additive, so 1 + 0.14 + 0.20 = 1.34 at the end of the game).
        /// </summary>
        public static float DamageTakenScale
        {
            get
            {
                int hardmodeDowned = Math.Min(HardmodeBossesDowned, HardmodeBossCount);
                float hardmodeBonus = HardmodeDamageTakenPerBoss * hardmodeDowned;
                float shmBonus = SubtleShmScale - 1f;

                return 1f + hardmodeBonus + shmBonus;
            }
        }
    }

    /// <summary>
    /// The damage half of progress scaling: one multiplier on every enemy hit, so contact damage, projectiles and puppet weapon
    /// hitboxes all ramp together without per-enemy code. Runs on the hit player's own client, which reads the same synced counts.
    /// </summary>
    public class ProgressionScalingPlayer : ModPlayer
    {
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Enemy hits only. A player source is PvP or self-damage; no player, NPC or projectile is environmental (fall, lava).
            // FinalDamage is a StatModifier, so the multiplier lands before flat reductions like the Undead Talisman's.
            PlayerDeathReason damageSource = modifiers.DamageSource;
            bool isEnemyHit = damageSource != null
                && damageSource.SourcePlayerIndex == -1
                && (damageSource.SourceNPCIndex != -1 || damageSource.SourceProjectileLocalIndex != -1);

            if (!isEnemyHit)
            {
                return;
            }

            modifiers.FinalDamage *= ProgressionScaling.DamageTakenScale;
        }
    }
}
