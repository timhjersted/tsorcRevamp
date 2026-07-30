using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Immutable tuning for the shared ranged/melee combo cadence. Assigning a preset plus a combo pool is the
    /// complete opt-in: health, recent damage, and sequence variance drive it independently of guard pressure.
    /// Unassigned enemies retain their existing attack timing exactly.
    /// </summary>
    public sealed class CombatTempoProfile
    {
        private static readonly CombatTempoProfile StandardPreset = new CombatTempoProfile(
            twoAttackHealthFraction: 0.70f,
            threeAttackHealthFraction: 0.40f,
            fourAttackHealthFraction: 0.25f,
            baseContinueChance: 0.70f,
            additionalLinkChance: 0.10f,
            chainGapMinTicks: 10,
            chainGapMaxTicks: 22,
            postComboNeutralFraction: 0.70f,
            minimumRecoveryTicks: 42,
            recoveryPerExtraAttackTicks: 18,
            sequenceVariance: 0.08f,
            recentDamageWindowTicks: 180,
            recentDamageFractionForMaxUrgency: 0.20f,
            recentDamageContinueBonus: 0.12f,
            recentDamageRetentionPerTick: 0.985f);

        private static readonly CombatTempoProfile ElitePreset = new CombatTempoProfile(
            twoAttackHealthFraction: 0.90f,
            threeAttackHealthFraction: 0.70f,
            fourAttackHealthFraction: 0.40f,
            baseContinueChance: 0.76f,
            additionalLinkChance: 0.08f,
            chainGapMinTicks: 8,
            chainGapMaxTicks: 18,
            postComboNeutralFraction: 0.60f,
            minimumRecoveryTicks: 38,
            recoveryPerExtraAttackTicks: 17,
            sequenceVariance: 0.08f,
            recentDamageWindowTicks: 180,
            recentDamageFractionForMaxUrgency: 0.16f,
            recentDamageContinueBonus: 0.12f,
            recentDamageRetentionPerTick: 0.985f);

        public float TwoAttackHealthFraction { get; }
        public float ThreeAttackHealthFraction { get; }
        public float FourAttackHealthFraction { get; }
        public float BaseContinueChance { get; }
        public float AdditionalLinkChance { get; }
        public int ChainGapMinTicks { get; }
        public int ChainGapMaxTicks { get; }
        public float PostComboNeutralFraction { get; }
        public int MinimumRecoveryTicks { get; }
        public int RecoveryPerExtraAttackTicks { get; }
        public float SequenceVariance { get; }
        public int RecentDamageWindowTicks { get; }
        public float RecentDamageFractionForMaxUrgency { get; }
        public float RecentDamageContinueBonus { get; }
        public float RecentDamageRetentionPerTick { get; }

        private CombatTempoProfile(
            float twoAttackHealthFraction,
            float threeAttackHealthFraction,
            float fourAttackHealthFraction,
            float baseContinueChance,
            float additionalLinkChance,
            int chainGapMinTicks,
            int chainGapMaxTicks,
            float postComboNeutralFraction,
            int minimumRecoveryTicks,
            int recoveryPerExtraAttackTicks,
            float sequenceVariance,
            int recentDamageWindowTicks,
            float recentDamageFractionForMaxUrgency,
            float recentDamageContinueBonus,
            float recentDamageRetentionPerTick)
        {
            TwoAttackHealthFraction = twoAttackHealthFraction;
            ThreeAttackHealthFraction = threeAttackHealthFraction;
            FourAttackHealthFraction = fourAttackHealthFraction;
            BaseContinueChance = baseContinueChance;
            AdditionalLinkChance = additionalLinkChance;
            ChainGapMinTicks = chainGapMinTicks;
            ChainGapMaxTicks = chainGapMaxTicks;
            PostComboNeutralFraction = postComboNeutralFraction;
            MinimumRecoveryTicks = minimumRecoveryTicks;
            RecoveryPerExtraAttackTicks = recoveryPerExtraAttackTicks;
            SequenceVariance = sequenceVariance;
            RecentDamageWindowTicks = recentDamageWindowTicks;
            RecentDamageFractionForMaxUrgency = recentDamageFractionForMaxUrgency;
            RecentDamageContinueBonus = recentDamageContinueBonus;
            RecentDamageRetentionPerTick = recentDamageRetentionPerTick;
        }

        /// <summary>Default cadence: isolated attacks above 70%, up to two below 70%, three below 40%, four below 25%.</summary>
        public static void Standard(tsorcRevampGlobalNPC globalNPC, params CombatComboMove[] comboMoves)
        {
            globalNPC.ConfigureCombatTempo(StandardPreset, comboMoves);
        }

        /// <summary>Elite cadence: chaining begins below 90%, reaches three below 70%, and four below 40%.</summary>
        public static void Elite(tsorcRevampGlobalNPC globalNPC, params CombatComboMove[] comboMoves)
        {
            globalNPC.ConfigureCombatTempo(ElitePreset, comboMoves);
        }

        /// <summary>Compatibility shorthand for projectile-only pools with no range or repeat restrictions.</summary>
        public static void Standard(tsorcRevampGlobalNPC globalNPC, params int[] comboAttackTypes)
        {
            globalNPC.ConfigureCombatTempo(StandardPreset, BuildAnyRangeMoves(comboAttackTypes));
        }

        /// <summary>Compatibility shorthand for projectile-only pools with no range or repeat restrictions.</summary>
        public static void Elite(tsorcRevampGlobalNPC globalNPC, params int[] comboAttackTypes)
        {
            globalNPC.ConfigureCombatTempo(ElitePreset, BuildAnyRangeMoves(comboAttackTypes));
        }

        private static CombatComboMove[] BuildAnyRangeMoves(int[] comboAttackTypes)
        {
            if (comboAttackTypes == null || comboAttackTypes.Length == 0)
            {
                return Array.Empty<CombatComboMove>();
            }

            CombatComboMove[] moves = new CombatComboMove[comboAttackTypes.Length];
            for (int i = 0; i < comboAttackTypes.Length; i++)
            {
                moves[i] = CombatComboMove.AnyRange(comboAttackTypes[i]);
            }
            return moves;
        }

        public int GetMaximumChainLength(NPC npc)
        {
            float lifeFraction = npc.lifeMax > 0 ? npc.life / (float)npc.lifeMax : 1f;
            if (lifeFraction < FourAttackHealthFraction)
            {
                return 4;
            }
            if (lifeFraction < ThreeAttackHealthFraction)
            {
                return 3;
            }
            if (lifeFraction <= TwoAttackHealthFraction)
            {
                return 2;
            }
            return 1;
        }

        public float GetRecentDamageUrgency(float recentDamageFraction)
        {
            return MathHelper.Clamp(recentDamageFraction / RecentDamageFractionForMaxUrgency, 0f, 1f);
        }

        public float GetContinueChance(int maximumChainLength, float recentDamageUrgency, float sequenceVariance)
        {
            float chance = BaseContinueChance
                + Math.Max(0, maximumChainLength - 2) * AdditionalLinkChance
                + recentDamageUrgency * RecentDamageContinueBonus
                + sequenceVariance;
            return MathHelper.Clamp(chance, 0.05f, 0.95f);
        }

        public int GetChainGapTicks(NPC npc, float recentDamageUrgency, float sequenceVariance)
        {
            float lifeUrgency = npc.lifeMax > 0 ? 1f - npc.life / (float)npc.lifeMax : 0f;
            float urgency = MathHelper.Clamp(lifeUrgency * 0.65f + recentDamageUrgency * 0.35f + sequenceVariance, 0f, 1f);
            return (int)Math.Round(MathHelper.Lerp(ChainGapMaxTicks, ChainGapMinTicks, urgency));
        }

        public int GetRecoveryTicks(int authoredNeutralTicks, int attacksCompleted)
        {
            // A one-move sequence preserves the attack's authored neutral cooldown exactly. A real combo replaces
            // part of that cooldown with a clear forced recovery which grows with every additional move.
            if (attacksCompleted <= 1)
            {
                return Math.Max(0, authoredNeutralTicks);
            }

            int authoredRecovery = (int)Math.Round(authoredNeutralTicks * PostComboNeutralFraction);
            int comboRecovery = MinimumRecoveryTicks + (attacksCompleted - 1) * RecoveryPerExtraAttackTicks;
            return Math.Max(authoredRecovery, comboRecovery);
        }
    }
}
