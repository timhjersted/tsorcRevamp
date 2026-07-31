using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public partial class tsorcRevampGlobalNPC
    {
        // B1 is an independent cadence modifier. It never enables CombatTempo, expands a health-based chain cap,
        // chooses a move, or shortens an authored tell.
        public const int GuardPressureRecoveryTicks = 75;
        private const float GuardPressureNeutralReductionPerStack = 0.20f;
        private const float GuardPressureContinueBonusPerStack = 0.035f;
        private const int GuardPressureChainGapReductionPerStack = 1;
        private const int GuardPressureMinimumChainGapTicks = 4;

        public int GuardPressureSequenceStacks { get; private set; }
        public int GuardPressureSequencePlayer { get; private set; } = -1;
        public int GuardPressureRecoveryTimer { get; private set; }
        public bool InGuardPressureRecovery => GuardPressureRecoveryTimer > 0;
        public AttackDefenseTraits ActiveAttackDefenseTraits { get; private set; }
        public bool ActiveAttackBypassesShield =>
            (ActiveAttackDefenseTraits & AttackDefenseTraits.BypassesActiveShield) != 0;

        internal bool IsMaximumGuardPressureReady(NPC npc)
        {
            if (!npc.HasValidTarget)
            {
                return false;
            }

            return GetActiveGuardPressureSequenceStacks(npc) >= GuardPressureMaxBlocks
                || (BlockedRecently >= GuardPressureMaxBlocks && BlockedRecentlyByPlayer == npc.target);
        }

        internal void SetActiveAttackDefenseTraits(NPC npc, AttackDefenseTraits traits)
        {
            if (ActiveAttackDefenseTraits == traits)
            {
                return;
            }

            ActiveAttackDefenseTraits = traits;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        internal void CancelActiveAttackDefenseTraits()
        {
            ActiveAttackDefenseTraits = AttackDefenseTraits.None;
        }

        /// <summary>
        /// Snapshots pressure for one attack sequence. Later blocks cannot change the sequence already in progress,
        /// and another player cannot inherit pressure accumulated by the target who raised the shield.
        /// </summary>
        internal int TryBeginGuardPressureSequence(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || InGuardPressureRecovery || !npc.HasValidTarget)
            {
                return 0;
            }

            if (GuardPressureSequenceStacks > 0)
            {
                if (GuardPressureSequencePlayer == npc.target)
                {
                    return 0;
                }

                CancelGuardPressureSequenceSnapshot();
                npc.netUpdate = true;
            }

            if (BlockedRecently <= 0 || BlockedRecentlyByPlayer != npc.target)
            {
                return 0;
            }

            GuardPressureSequenceStacks = Math.Clamp(BlockedRecently, 1, GuardPressureMaxBlocks);
            GuardPressureSequencePlayer = BlockedRecentlyByPlayer;
            npc.netUpdate = true;
            return GuardPressureSequenceStacks;
        }

        internal int GetActiveGuardPressureSequenceStacks(NPC npc)
        {
            return GuardPressureSequenceStacks > 0 && GuardPressureSequencePlayer == npc.target
                ? GuardPressureSequenceStacks
                : 0;
        }

        internal float GetGuardPressureContinueBonus(NPC npc)
        {
            return GetActiveGuardPressureSequenceStacks(npc) * GuardPressureContinueBonusPerStack;
        }

        internal int ApplyGuardPressureToChainGap(NPC npc, int unmodifiedGapTicks)
        {
            int reduction = GetActiveGuardPressureSequenceStacks(npc) * GuardPressureChainGapReductionPerStack;
            return Math.Max(GuardPressureMinimumChainGapTicks, unmodifiedGapTicks - reduction);
        }

        internal int GetGuardPressureCompressedNeutralTicks(int authoredNeutralTicks, int pressureStacks)
        {
            pressureStacks = Math.Clamp(pressureStacks, 0, GuardPressureMaxBlocks);
            float multiplier = 1f - pressureStacks * GuardPressureNeutralReductionPerStack;
            return Math.Max(0, (int)Math.Round(authoredNeutralTicks * multiplier));
        }

        /// <summary>
        /// Captures a sequence and returns a neutral-time skip. Bespoke timelines can cap the result before an
        /// exact-frame authored event; the snapshot still owns only cadence and cannot cross that boundary.
        /// </summary>
        internal int TryGetGuardPressureNeutralSkip(NPC npc, int neutralTicksToCompress, int maximumSafeSkip)
        {
            int startedStacks = TryBeginGuardPressureSequence(npc);
            if (startedStacks <= 0 || neutralTicksToCompress <= 0 || maximumSafeSkip <= 0)
            {
                return 0;
            }

            int compressedNeutralTicks = GetGuardPressureCompressedNeutralTicks(neutralTicksToCompress, startedStacks);
            return Math.Min(maximumSafeSkip, neutralTicksToCompress - compressedNeutralTicks);
        }

        /// <summary>
        /// Applies a newly captured snapshot to the neutral portion of the current projectile timer. The timer is
        /// capped at the telegraph boundary, so even pressure captured late in neutral can never skip the full tell.
        /// </summary>
        internal void TryApplyGuardPressureToProjectileNeutral(NPC npc, int authoredNeutralTicks)
        {
            // When this fighter promises a melee unblockable climax, do not let an ordinary ranged scheduler
            // snapshot and consume the fourth stack while terrain or distance temporarily prevents the finisher.
            if (CombatMeleeProfile != null && CombatMeleeProfile.GuardPressureUnblockable
                && IsMaximumGuardPressureReady(npc))
            {
                return;
            }

            int maximumSafeSkip = Math.Max(0, (int)Math.Floor(authoredNeutralTicks - ProjectileTimer));
            int skippedNeutralTicks = TryGetGuardPressureNeutralSkip(npc, authoredNeutralTicks, maximumSafeSkip);
            if (skippedNeutralTicks <= 0)
            {
                return;
            }

            ProjectileTimer += skippedNeutralTicks;
            npc.netUpdate = true;
        }

        /// <summary>
        /// Ends the snapshot after a completed attack sequence. Stacks one through three remain banked until decay;
        /// a four-stack sequence consumes the bank and creates the player's stationary punish window.
        /// </summary>
        internal int CompleteGuardPressureSequence(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return 0;
            }

            int completedStacks = GetActiveGuardPressureSequenceStacks(npc);
            bool hadSnapshot = GuardPressureSequenceStacks > 0;
            CancelGuardPressureSequenceSnapshot();

            if (completedStacks >= GuardPressureMaxBlocks)
            {
                ClearGuardPressure(npc);
                GuardPressureRecoveryTimer = Math.Max(GuardPressureRecoveryTimer, GuardPressureRecoveryTicks);
                FighterPostAttackPauseTimer = 0;
                FighterRangedStandShotsRemaining = 0;
                ArcherAimDirection = 0f;
                AttackTelegraphing = false;
                AttackCommitted = false;
            }

            if (hadSnapshot)
            {
                npc.netUpdate = true;
            }

            return completedStacks;
        }

        internal void CancelGuardPressureSequenceSnapshot()
        {
            GuardPressureSequenceStacks = 0;
            GuardPressureSequencePlayer = -1;
        }

        private void UpdateGuardPressureBehavior(NPC npc)
        {
            if (GuardPressureRecoveryTimer <= 0)
            {
                return;
            }

            GuardPressureRecoveryTimer--;
            if (GuardPressureRecoveryTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        internal void SendGuardPressureBehavior(BinaryWriter writer)
        {
            writer.Write(GuardPressureSequenceStacks);
            writer.Write(GuardPressureSequencePlayer);
            writer.Write(GuardPressureRecoveryTimer);
            writer.Write((ushort)ActiveAttackDefenseTraits);
        }

        internal void ReceiveGuardPressureBehavior(BinaryReader reader)
        {
            GuardPressureSequenceStacks = reader.ReadInt32();
            GuardPressureSequencePlayer = reader.ReadInt32();
            GuardPressureRecoveryTimer = reader.ReadInt32();
            ActiveAttackDefenseTraits = (AttackDefenseTraits)reader.ReadUInt16();
        }
    }
}
