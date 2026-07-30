using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public partial class tsorcRevampGlobalNPC
    {
        /// <summary>Null keeps legacy attack timing. Assign through CombatTempoProfile.Standard/Elite.</summary>
        public CombatTempoProfile CombatTempo;
        private CombatComboMove[] CombatComboMoves = Array.Empty<CombatComboMove>();

        public int CombatComboStep { get; private set; }
        public int CombatComboMaximum { get; private set; } = 1;
        public int CombatComboRecoveryTimer { get; private set; }
        public float CombatTempoRecentDamageFraction { get; private set; }
        public int CombatTempoRecentDamageTimer { get; private set; }
        public float CombatTempoSequenceVariance { get; private set; }
        public bool InCombatComboRecovery => CombatTempo != null && CombatComboRecoveryTimer > 0;

        internal void ConfigureCombatTempo(CombatTempoProfile profile, CombatComboMove[] comboMoves)
        {
            CombatTempo = profile;
            CombatComboMoves = comboMoves == null || comboMoves.Length == 0
                ? Array.Empty<CombatComboMove>()
                : (CombatComboMove[])comboMoves.Clone();
            ResetCombatTempoSequence(clearRecovery: true);
        }

        internal bool CanAttackParticipateInCombatCombo(int attackType)
        {
            for (int i = 0; i < CombatComboMoves.Length; i++)
            {
                if (CombatComboMoves[i].Key == attackType)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryGetCombatComboMove(int moveKey, out CombatComboMove move)
        {
            for (int i = 0; i < CombatComboMoves.Length; i++)
            {
                if (CombatComboMoves[i].Key == moveKey)
                {
                    move = CombatComboMoves[i];
                    return true;
                }
            }

            move = default;
            return false;
        }

        internal void UpdateCombatTempo(NPC npc)
        {
            if (CombatTempo == null)
            {
                return;
            }

            if (CombatComboRecoveryTimer > 0)
            {
                CombatComboRecoveryTimer--;
                if (CombatComboRecoveryTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.netUpdate = true;
                }
            }

            if (CombatTempoRecentDamageTimer > 0)
            {
                CombatTempoRecentDamageTimer--;
                CombatTempoRecentDamageFraction *= CombatTempo.RecentDamageRetentionPerTick;
                if (CombatTempoRecentDamageTimer == 0 || CombatTempoRecentDamageFraction < 0.0001f)
                {
                    CombatTempoRecentDamageTimer = 0;
                    CombatTempoRecentDamageFraction = 0f;
                }
            }

            UpdateHumanoidMeleeTimers();
        }

        internal void RegisterCombatTempoDamage(NPC npc, int damageDone)
        {
            if (CombatTempo == null || Main.netMode == NetmodeID.MultiplayerClient || damageDone <= 0 || npc.lifeMax <= 0)
            {
                return;
            }

            CombatTempoRecentDamageFraction = MathHelper.Clamp(
                CombatTempoRecentDamageFraction + damageDone / (float)npc.lifeMax,
                0f,
                1f);
            CombatTempoRecentDamageTimer = CombatTempo.RecentDamageWindowTicks;
        }

        internal bool TryChooseCombatComboFollowup(
            NPC npc,
            int completedAttackType,
            bool attackEndsCombo,
            Func<int, bool> extraEligibility,
            out int followupMoveKey,
            out int attacksCompleted,
            out int completedGuardPressureStacks)
        {
            followupMoveKey = 0;
            completedGuardPressureStacks = 0;
            if (CombatTempo == null || Main.netMode == NetmodeID.MultiplayerClient)
            {
                attacksCompleted = 1;
                return false;
            }

            if (!CanAttackParticipateInCombatCombo(completedAttackType))
            {
                attacksCompleted = 1;
                completedGuardPressureStacks = CompleteGuardPressureSequence(npc);
                ResetCombatTempoSequence(clearRecovery: false);
                return false;
            }

            if (CombatComboStep == 0)
            {
                CombatComboMaximum = CombatTempo.GetMaximumChainLength(npc);
                CombatTempoSequenceVariance = Main.rand.NextFloat(-CombatTempo.SequenceVariance, CombatTempo.SequenceVariance);
            }

            CombatComboStep++;
            attacksCompleted = CombatComboStep;
            float damageUrgency = CombatTempo.GetRecentDamageUrgency(CombatTempoRecentDamageFraction);
            float continueChance = CombatTempo.GetContinueChance(
                CombatComboMaximum,
                damageUrgency,
                CombatTempoSequenceVariance);
            continueChance = MathHelper.Clamp(continueChance + GetGuardPressureContinueBonus(npc), 0.05f, 0.95f);

            float eligibleWeight = 0f;
            if (!attackEndsCombo && CombatComboStep < CombatComboMaximum)
            {
                for (int i = 0; i < CombatComboMoves.Length; i++)
                {
                    CombatComboMove candidate = CombatComboMoves[i];
                    bool repeatAllowed = candidate.Key != completedAttackType || candidate.CanRepeat;
                    bool additionalCheckPassed = extraEligibility == null || extraEligibility(candidate.Key);
                    if (repeatAllowed && candidate.IsInRange(npc) && additionalCheckPassed)
                    {
                        eligibleWeight += candidate.Weight;
                    }
                }
            }

            if (eligibleWeight > 0f && Main.rand.NextFloat() < continueChance)
            {
                float randomValue = Main.rand.NextFloat(eligibleWeight);
                float runningWeight = 0f;
                for (int i = 0; i < CombatComboMoves.Length; i++)
                {
                    CombatComboMove candidate = CombatComboMoves[i];
                    bool repeatAllowed = candidate.Key != completedAttackType || candidate.CanRepeat;
                    bool additionalCheckPassed = extraEligibility == null || extraEligibility(candidate.Key);
                    if (!repeatAllowed || !candidate.IsInRange(npc) || !additionalCheckPassed)
                    {
                        continue;
                    }

                    runningWeight += candidate.Weight;
                    if (randomValue < runningWeight)
                    {
                        followupMoveKey = candidate.Key;
                        return true;
                    }
                }
            }

            completedGuardPressureStacks = CompleteGuardPressureSequence(npc);
            ResetCombatTempoSequence(clearRecovery: false);
            return false;
        }

        internal int GetCombatComboGapTicks(NPC npc)
        {
            if (CombatTempo == null)
            {
                return 0;
            }

            float damageUrgency = CombatTempo.GetRecentDamageUrgency(CombatTempoRecentDamageFraction);
            int unmodifiedGapTicks = CombatTempo.GetChainGapTicks(npc, damageUrgency, CombatTempoSequenceVariance);
            return ApplyGuardPressureToChainGap(npc, unmodifiedGapTicks);
        }

        internal int GetCombatComboFollowupTimer(NPC npc, int authoredNeutralTicks)
        {
            if (CombatTempo == null)
            {
                return 0;
            }

            int chainGapTicks = GetCombatComboGapTicks(npc);

            CombatComboRecoveryTimer = 0;
            return Math.Max(0, authoredNeutralTicks - chainGapTicks);
        }

        internal void BeginCombatComboRecovery(int authoredNeutralTicks, int attacksCompleted)
        {
            if (CombatTempo == null)
            {
                return;
            }

            CombatComboRecoveryTimer = CombatTempo.GetRecoveryTicks(authoredNeutralTicks, attacksCompleted);
        }

        internal void ResetCombatTempoSequence(bool clearRecovery)
        {
            CombatComboStep = 0;
            CombatComboMaximum = 1;
            CombatTempoSequenceVariance = 0f;
            CancelGuardPressureSequenceSnapshot();
            if (clearRecovery)
            {
                CombatComboRecoveryTimer = 0;
            }
            CancelHumanoidMelee(clearOpenerCooldown: clearRecovery);
        }

        internal int EndCombatTempoSequenceWithoutFollowup(NPC npc)
        {
            int completedGuardPressureStacks = CompleteGuardPressureSequence(npc);
            ResetCombatTempoSequence(clearRecovery: false);
            return completedGuardPressureStacks;
        }

        internal void SendCombatTempo(BinaryWriter writer)
        {
            bool enabled = CombatTempo != null;
            writer.Write(enabled);
            if (!enabled)
            {
                return;
            }

            writer.Write(CombatComboStep);
            writer.Write(CombatComboMaximum);
            writer.Write(CombatComboRecoveryTimer);
            writer.Write(CombatTempoRecentDamageFraction);
            writer.Write(CombatTempoRecentDamageTimer);
            writer.Write(CombatTempoSequenceVariance);
            SendHumanoidMelee(writer);
        }

        internal void ReceiveCombatTempo(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                ResetCombatTempoSequence(clearRecovery: true);
                CombatTempoRecentDamageFraction = 0f;
                CombatTempoRecentDamageTimer = 0;
                return;
            }

            CombatComboStep = reader.ReadInt32();
            CombatComboMaximum = reader.ReadInt32();
            CombatComboRecoveryTimer = reader.ReadInt32();
            CombatTempoRecentDamageFraction = reader.ReadSingle();
            CombatTempoRecentDamageTimer = reader.ReadInt32();
            CombatTempoSequenceVariance = reader.ReadSingle();
            ReceiveHumanoidMelee(reader);
        }
    }
}
