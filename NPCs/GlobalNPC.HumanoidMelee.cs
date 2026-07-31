using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.NPCs
{
    public partial class tsorcRevampGlobalNPC
    {
        private HumanoidMeleeProfile CombatMeleeProfile;
        private int CombatMeleeActiveKey;
        private int CombatMeleeTimer;
        private int CombatMeleeTelegraphTicks;
        private int CombatMeleeLockedDirection = 1;
        private float CombatMeleeLandingX;
        private int CombatMeleeOpenerCooldownTimer;
        private int PendingCombatComboMoveKey;
        private int PendingCombatComboGapTimer;

        public bool CombatMeleeActive => CombatMeleeProfile != null && CombatMeleeActiveKey != 0;
        public bool HasPendingCombatComboMove => PendingCombatComboMoveKey != 0;
        public int PendingCombatMoveKey => PendingCombatComboMoveKey;
        public int PendingCombatMoveGapTimer => PendingCombatComboGapTimer;
        public int ActiveCombatMeleeKey => CombatMeleeActiveKey;
        public int ActiveCombatMeleeDirection => CombatMeleeLockedDirection;
        public int ActiveCombatMeleeTimer => CombatMeleeTimer;
        public int ActiveCombatMeleeTelegraphTicks => CombatMeleeTelegraphTicks;

        public float CombatMeleeTelegraphProgress => CombatMeleeActive
            ? MathHelper.Clamp(CombatMeleeTimer / (float)Math.Max(1, CombatMeleeTelegraphTicks), 0f, 1f)
            : 0f;

        /// <summary>Normalized progress through the damaging window for enemy-local held-weapon animation.</summary>
        public float CombatMeleeActiveProgress
        {
            get
            {
                HumanoidMeleeMove move = GetHumanoidMeleeMove(CombatMeleeActiveKey);
                if (move == null)
                {
                    return 0f;
                }

                int activeStart = CombatMeleeTelegraphTicks + 1;
                return MathHelper.Clamp(
                    (CombatMeleeTimer - activeStart) / (float)Math.Max(1, move.ActiveTicks - 1),
                    0f,
                    1f);
            }
        }

        /// <summary>Normalized progress after the damaging window for weapon recovery animation.</summary>
        public float CombatMeleeFollowThroughProgress
        {
            get
            {
                HumanoidMeleeMove move = GetHumanoidMeleeMove(CombatMeleeActiveKey);
                if (move == null)
                {
                    return 0f;
                }

                int followThroughStart = CombatMeleeTelegraphTicks + 1 + move.ActiveTicks;
                return MathHelper.Clamp(
                    (CombatMeleeTimer - followThroughStart) / (float)Math.Max(1, move.FollowThroughTicks - 1),
                    0f,
                    1f);
            }
        }

        public bool InCombatMeleeHitWindow
        {
            get
            {
                HumanoidMeleeMove move = GetHumanoidMeleeMove(CombatMeleeActiveKey);
                if (move == null)
                {
                    return false;
                }

                int activeStart = CombatMeleeTelegraphTicks + 1;
                return CombatMeleeTimer >= activeStart && CombatMeleeTimer < activeStart + move.ActiveTicks;
            }
        }

        /// <summary>0 before/after the thrust, 1 at its furthest extension.</summary>
        public float CombatMeleeThrustProgress
        {
            get
            {
                HumanoidMeleeMove move = GetHumanoidMeleeMove(CombatMeleeActiveKey);
                if (move == null)
                {
                    return 0f;
                }

                int activeStart = CombatMeleeTelegraphTicks + 1;
                int presentationStart = CombatMeleeActiveKey == CombatComboMoveKey.LongHopMelee
                    ? activeStart + move.ActiveTicks / 2
                    : activeStart;
                int presentationTicks = CombatMeleeActiveKey == CombatComboMoveKey.LongHopMelee
                    ? move.ActiveTicks - move.ActiveTicks / 2 + move.FollowThroughTicks
                    : move.ActiveTicks + move.FollowThroughTicks;
                float progress = (CombatMeleeTimer - presentationStart) / (float)Math.Max(1, presentationTicks - 1);
                if (progress < 0f || progress > 1f)
                {
                    return 0f;
                }

                return (float)Math.Sin(progress * MathHelper.Pi);
            }
        }

        internal void ConfigureHumanoidMelee(HumanoidMeleeProfile profile)
        {
            CombatMeleeProfile = profile;
            CancelHumanoidMelee(clearOpenerCooldown: true);
        }

        internal bool CanHumanoidMeleeHandleMove(int moveKey)
        {
            return GetHumanoidMeleeMove(moveKey) != null;
        }

        internal bool CanExecuteHumanoidMeleeMove(NPC npc, int moveKey)
        {
            HumanoidMeleeMove move = GetHumanoidMeleeMove(moveKey);
            return move != null && move.CanStart(npc);
        }

        internal bool TickHumanoidMelee(NPC npc)
        {
            if (CombatMeleeProfile == null || CombatTempo == null || InGuardPressureRecovery)
            {
                return false;
            }

            // A killed target or a teleport must release combat-owned presentation immediately. Previously the
            // early return left the melee state (and its locked arm/weapon facing) frozen at the old location.
            bool meleeInvalidated = !npc.HasValidTarget || TeleportCountdown > 0 || TeleportAppearanceTimer > 0;
            if (meleeInvalidated)
            {
                if (CombatMeleeActive || HasPendingCombatComboMove)
                {
                    CancelHumanoidMelee(clearOpenerCooldown: false);
                    ResetCombatTempoSequence(clearRecovery: true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.netUpdate = true;
                    }
                }
                return false;
            }

            if (CombatMeleeActive)
            {
                TickActiveHumanoidMelee(npc);
                return CombatMeleeActive;
            }

            bool incompatibleBusy = StaggerTimer > 0 || TeleportCountdown > 0 || TeleportAppearanceTimer > 0
                || DodgeTimer > 0 || PounceTimer > 0 || QuickStepTimer > 0 || InSustainedEvasion || Fleeing;
            if (incompatibleBusy)
            {
                return false;
            }

            if (HasPendingCombatComboMove && CanHumanoidMeleeHandleMove(PendingCombatComboMoveKey))
            {
                if (PendingCombatComboGapTimer > 0)
                {
                    return false;
                }

                HumanoidMeleeMove queuedMove = GetHumanoidMeleeMove(PendingCombatComboMoveKey);
                if (Main.netMode != NetmodeID.MultiplayerClient && !queuedMove.CanStart(npc))
                {
                    EndInvalidQueuedCombatMove(npc);
                    return false;
                }

                StartHumanoidMelee(npc, queuedMove, consumeQueue: true);
                return true;
            }

            if (HasPendingCombatComboMove || InCombatComboRecovery || npc.velocity.Y != 0f
                || AttackTelegraphing || AttackCommitted || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }

            bool forceGuardPressureClimax = CombatMeleeProfile.GuardPressureUnblockable
                && IsMaximumGuardPressureReady(npc);
            if (!forceGuardPressureClimax && CombatMeleeOpenerCooldownTimer > 0)
            {
                return false;
            }
            if (!forceGuardPressureClimax && !CombatMeleeProfile.CanStartOpener(npc))
            {
                return false;
            }

            HumanoidMeleeMove opener = SelectHumanoidMeleeOpener(npc);
            if (opener == null)
            {
                return false;
            }

            TryBeginGuardPressureSequence(npc);
            StartHumanoidMelee(npc, opener, consumeQueue: false);
            return true;
        }

        private HumanoidMeleeMove SelectHumanoidMeleeOpener(NPC npc)
        {
            HumanoidMeleeMove close = CombatMeleeProfile.CloseHop.CanStart(npc) ? CombatMeleeProfile.CloseHop : null;
            HumanoidMeleeMove longHop = CombatMeleeProfile.LongHop.CanStart(npc) ? CombatMeleeProfile.LongHop : null;
            if (close == null)
            {
                return longHop;
            }
            if (longHop == null)
            {
                return close;
            }

            float closeWeight = close.ComboMove.Weight;
            float totalWeight = closeWeight + longHop.ComboMove.Weight;
            return Main.rand.NextFloat(totalWeight) < closeWeight ? close : longHop;
        }

        private void StartHumanoidMelee(NPC npc, HumanoidMeleeMove move, bool consumeQueue)
        {
            if (consumeQueue)
            {
                PendingCombatComboMoveKey = 0;
                PendingCombatComboGapTimer = 0;
            }

            Player target = Main.player[npc.target];
            CombatMeleeActiveKey = move.ComboMove.Key;
            CombatMeleeTimer = 0;
            if (Main.netMode != NetmodeID.MultiplayerClient && CombatMeleeProfile.GuardPressureUnblockable
                && IsMaximumGuardPressureReady(npc))
            {
                // A ranged move may have begun while the finisher was out of range. If its distance-aware follow-up
                // is melee, capture the bank here so that first executable melee becomes the promised climax.
                TryBeginGuardPressureSequence(npc);
            }
            bool guardPressureUnblockable = CombatMeleeProfile.GuardPressureUnblockable
                && GetActiveGuardPressureSequenceStacks(npc) >= GuardPressureMaxBlocks;
            CombatMeleeTelegraphTicks = guardPressureUnblockable
                ? CombatMeleeProfile.GuardPressureTelegraphTicks
                : move.TelegraphTicks;
            SetActiveAttackDefenseTraits(npc, guardPressureUnblockable
                ? AttackDefenseTraits.BypassesActiveShield
                : AttackDefenseTraits.None);
            CombatMeleeLockedDirection = target.Center.X >= npc.Center.X ? 1 : -1;
            npc.direction = CombatMeleeLockedDirection;
            npc.spriteDirection = CombatMeleeLockedDirection;
            ProjectileTimer = 0f;
            AttackTelegraphing = true;
            AttackCommitted = false;
            tsorcRevampAIs.SpawnTelegraphFlash(npc, guardPressureUnblockable ? Color.Red : move.TelegraphColor);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        private void TickActiveHumanoidMelee(NPC npc)
        {
            HumanoidMeleeMove move = GetHumanoidMeleeMove(CombatMeleeActiveKey);
            if (move == null)
            {
                CancelHumanoidMelee(clearOpenerCooldown: false);
                return;
            }

            CombatMeleeTimer++;
            npc.direction = CombatMeleeLockedDirection;
            npc.spriteDirection = CombatMeleeLockedDirection;

            int activeStart = CombatMeleeTelegraphTicks + 1;
            int activeEnd = activeStart + move.ActiveTicks;
            AttackTelegraphing = CombatMeleeTimer < activeStart;
            AttackCommitted = CombatMeleeTimer >= activeStart && CombatMeleeTimer < activeEnd;

            if (CombatMeleeTimer < activeStart)
            {
                npc.velocity.X *= 0.72f;
            }
            else if (CombatMeleeTimer == activeStart)
            {
                Player target = Main.player[npc.target];
                Vector2 launchVelocity = GetHumanoidMeleeLaunchVelocity(npc, target, move);
                npc.velocity = launchVelocity;
                SoundEngine.PlaySound(move.AttackSound with { Volume = 0.75f, PitchVariance = 0.08f }, npc.Center);
                SpawnHumanoidMeleeHitbox(npc, move);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.netUpdate = true;
                }
            }
            else
            {
                BrakeHumanoidMeleeBeforeOverlap(npc, Main.player[npc.target], move);
                if (CombatMeleeTimer >= activeEnd)
                {
                    npc.velocity.X *= 0.84f;
                }
            }

            if (CombatMeleeTimer >= CombatMeleeTelegraphTicks + move.ActiveTicks + move.FollowThroughTicks)
            {
                FinishHumanoidMelee(npc, move);
            }
        }

        private Vector2 GetHumanoidMeleeLaunchVelocity(NPC npc, Player target, HumanoidMeleeMove move)
        {
            if (CombatMeleeActiveKey != CombatComboMoveKey.LongHopMelee)
            {
                CombatMeleeLandingX = npc.Center.X + CombatMeleeLockedDirection * move.HopSpeedX * move.ActiveTicks;
                return new Vector2(CombatMeleeLockedDirection * move.HopSpeedX, -move.HopSpeedY);
            }

            // Solve one restrained ballistic arc toward a point just outside body overlap. This keeps the launch
            // smooth and readable instead of compensating for distance with a very large horizontal burst.
            float signedDistance = (target.Center.X - npc.Center.X) * CombatMeleeLockedDirection;
            float targetLead = MathHelper.Clamp(target.velocity.X * CombatMeleeLockedDirection * 3f, -8f, 20f);
            float stopDistance = GetHumanoidMeleeStopDistance(npc, target, move);
            float desiredTravel = Math.Max(0f, signedDistance + targetLead - stopDistance);
            float travelTicks = MathHelper.Clamp(desiredTravel / Math.Max(1f, move.HopSpeedX), 20f, move.ActiveTicks);
            float horizontalSpeed = MathHelper.Clamp(desiredTravel / travelTicks, move.HopSpeedX * 0.55f, move.HopSpeedX * 1.25f);
            desiredTravel = horizontalSpeed * travelTicks;
            CombatMeleeLandingX = npc.Center.X + CombatMeleeLockedDirection * desiredTravel;

            float gravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            float verticalTravel = MathHelper.Clamp(target.Center.Y - npc.Center.Y, -48f, 48f);
            float verticalSpeed = (verticalTravel - 0.5f * gravity * travelTicks * travelTicks) / travelTicks;
            verticalSpeed = MathHelper.Clamp(verticalSpeed, -move.HopSpeedY, -2f);
            return new Vector2(CombatMeleeLockedDirection * horizontalSpeed, verticalSpeed);
        }

        private void BrakeHumanoidMeleeBeforeOverlap(NPC npc, Player target, HumanoidMeleeMove move)
        {
            float signedDistance = (target.Center.X - npc.Center.X) * CombatMeleeLockedDirection;
            float remainingLandingDistance = (CombatMeleeLandingX - npc.Center.X) * CombatMeleeLockedDirection;
            if ((signedDistance > GetHumanoidMeleeStopDistance(npc, target, move) && remainingLandingDistance > 0f)
                || npc.velocity.X * CombatMeleeLockedDirection <= 0f)
            {
                return;
            }

            npc.velocity.X *= 0.35f;
            if (Math.Abs(npc.velocity.X) < 0.5f)
            {
                npc.velocity.X = 0f;
            }
        }

        private static float GetHumanoidMeleeStopDistance(NPC npc, Player target, HumanoidMeleeMove move)
        {
            float bodyClearance = npc.width * 0.5f + target.width * 0.5f + 8f;
            return Math.Max(bodyClearance, move.HitboxReach * 0.52f);
        }

        private void SpawnHumanoidMeleeHitbox(NPC npc, HumanoidMeleeMove move)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int projectileIndex = Projectile.NewProjectile(
                npc.GetSource_FromThis(), npc.Center, new Vector2(CombatMeleeLockedDirection, 0f),
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.HumanoidMeleeHitbox>(),
                move.Damage, move.Knockback, Main.myPlayer, move.HitboxReach, move.HitboxHeight);
            tsorcGlobalProjectile.SetDefenseTraits(projectileIndex, ActiveAttackDefenseTraits);
        }

        private void FinishHumanoidMelee(NPC npc, HumanoidMeleeMove completedMove)
        {
            int completedKey = CombatMeleeActiveKey;
            bool attackEndsCombo = ActiveAttackBypassesShield;
            CombatMeleeActiveKey = 0;
            CombatMeleeTimer = 0;
            CombatMeleeTelegraphTicks = 0;
            CombatMeleeLandingX = 0f;
            AttackTelegraphing = false;
            AttackCommitted = false;
            CombatMeleeOpenerCooldownTimer = CombatMeleeProfile.OpenerCooldownTicks;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                CancelActiveAttackDefenseTraits();
                return;
            }

            bool continueCombo = TryChooseCombatComboFollowup(
                npc,
                completedKey,
                attackEndsCombo,
                moveKey => !CanHumanoidMeleeHandleMove(moveKey) || CanExecuteHumanoidMeleeMove(npc, moveKey),
                out int nextMoveKey,
                out int attacksCompleted,
                out int completedGuardPressureStacks);
            SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);

            if (continueCombo)
            {
                QueueCombatComboMove(nextMoveKey, GetCombatComboGapTicks(npc));
            }
            else
            {
                int recoveryNeutralTicks = CombatMeleeProfile.PostSequenceNeutralTicks;
                if (attacksCompleted <= 1 && completedGuardPressureStacks > 0
                    && completedGuardPressureStacks < GuardPressureMaxBlocks)
                {
                    recoveryNeutralTicks = GetGuardPressureCompressedNeutralTicks(
                        recoveryNeutralTicks,
                        completedGuardPressureStacks);
                }
                BeginCombatComboRecovery(recoveryNeutralTicks, attacksCompleted);
            }

            npc.netUpdate = true;
        }

        internal void QueueCombatComboMove(int moveKey, int gapTicks)
        {
            PendingCombatComboMoveKey = moveKey;
            PendingCombatComboGapTimer = Math.Max(0, gapTicks);
        }

        internal bool TryConsumePendingCombatComboMove(int moveKey)
        {
            if (PendingCombatComboMoveKey != moveKey || PendingCombatComboGapTimer > 0)
            {
                return false;
            }

            PendingCombatComboMoveKey = 0;
            PendingCombatComboGapTimer = 0;
            return true;
        }

        internal void EndInvalidQueuedCombatMove(NPC npc)
        {
            int attacksCompleted = Math.Max(1, CombatComboStep);
            int completedGuardPressureStacks = CompleteGuardPressureSequence(npc);
            PendingCombatComboMoveKey = 0;
            PendingCombatComboGapTimer = 0;
            ResetCombatTempoSequence(clearRecovery: false);
            int recoveryNeutralTicks = CombatMeleeProfile?.PostSequenceNeutralTicks ?? 45;
            if (attacksCompleted <= 1 && completedGuardPressureStacks > 0
                && completedGuardPressureStacks < GuardPressureMaxBlocks)
            {
                recoveryNeutralTicks = GetGuardPressureCompressedNeutralTicks(
                    recoveryNeutralTicks,
                    completedGuardPressureStacks);
            }
            BeginCombatComboRecovery(recoveryNeutralTicks, attacksCompleted);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        private HumanoidMeleeMove GetHumanoidMeleeMove(int moveKey)
        {
            if (CombatMeleeProfile == null)
            {
                return null;
            }
            if (CombatMeleeProfile.CloseHop.ComboMove.Key == moveKey)
            {
                return CombatMeleeProfile.CloseHop;
            }
            if (CombatMeleeProfile.LongHop.ComboMove.Key == moveKey)
            {
                return CombatMeleeProfile.LongHop;
            }
            return null;
        }

        private void CancelHumanoidMelee(bool clearOpenerCooldown)
        {
            CombatMeleeActiveKey = 0;
            CombatMeleeTimer = 0;
            CombatMeleeTelegraphTicks = 0;
            CombatMeleeLandingX = 0f;
            PendingCombatComboMoveKey = 0;
            CancelActiveAttackDefenseTraits();
            PendingCombatComboGapTimer = 0;
            AttackTelegraphing = false;
            AttackCommitted = false;
            if (clearOpenerCooldown)
            {
                CombatMeleeOpenerCooldownTimer = 0;
            }
        }

        private void UpdateHumanoidMeleeTimers()
        {
            if (CombatMeleeOpenerCooldownTimer > 0)
            {
                CombatMeleeOpenerCooldownTimer--;
            }
            if (PendingCombatComboGapTimer > 0)
            {
                PendingCombatComboGapTimer--;
            }
        }

        private void SendHumanoidMelee(BinaryWriter writer)
        {
            writer.Write(CombatMeleeActiveKey);
            writer.Write(CombatMeleeTimer);
            writer.Write(CombatMeleeTelegraphTicks);
            writer.Write((sbyte)CombatMeleeLockedDirection);
            writer.Write(CombatMeleeLandingX);
            writer.Write(CombatMeleeOpenerCooldownTimer);
            writer.Write(PendingCombatComboMoveKey);
            writer.Write(PendingCombatComboGapTimer);
        }

        private void ReceiveHumanoidMelee(BinaryReader reader)
        {
            CombatMeleeActiveKey = reader.ReadInt32();
            CombatMeleeTimer = reader.ReadInt32();
            CombatMeleeTelegraphTicks = reader.ReadInt32();
            CombatMeleeLockedDirection = reader.ReadSByte();
            CombatMeleeLandingX = reader.ReadSingle();
            CombatMeleeOpenerCooldownTimer = reader.ReadInt32();
            PendingCombatComboMoveKey = reader.ReadInt32();
            PendingCombatComboGapTimer = reader.ReadInt32();
        }
    }
}
