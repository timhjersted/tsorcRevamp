using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    /// <summary>Optional source-NPC effects applied when a shared humanoid melee hitbox connects.</summary>
    public interface IHumanoidMeleeHitEffects
    {
        void OnHumanoidMeleeHit(Player target);
    }

    public sealed class HumanoidMeleeMove
    {
        public CombatComboMove ComboMove { get; }
        public int TelegraphTicks { get; }
        public int ActiveTicks { get; }
        public int FollowThroughTicks { get; }
        public float HopSpeedX { get; }
        public float HopSpeedY { get; }
        public int TelegraphRunUpTicks { get; }
        public float TelegraphRunUpAcceleration { get; }
        public float TelegraphRunUpTopSpeed { get; }
        public int Damage { get; }
        public float Knockback { get; }
        public int HitboxReach { get; }
        public int HitboxHeight { get; }
        public float MaximumVerticalOffset { get; }
        public bool RequiresLineOfSight { get; }
        public Color TelegraphColor { get; }
        public SoundStyle AttackSound { get; }

        public int TotalTicks => TelegraphTicks + ActiveTicks + FollowThroughTicks;

        public HumanoidMeleeMove(
            CombatComboMove comboMove,
            int telegraphTicks,
            int activeTicks,
            int followThroughTicks,
            float hopSpeedX,
            float hopSpeedY,
            int telegraphRunUpTicks,
            float telegraphRunUpAcceleration,
            float telegraphRunUpTopSpeed,
            int damage,
            float knockback,
            int hitboxReach,
            int hitboxHeight,
            float maximumVerticalOffset,
            bool requiresLineOfSight,
            Color telegraphColor,
            SoundStyle attackSound)
        {
            ComboMove = comboMove;
            TelegraphTicks = Math.Max(1, telegraphTicks);
            ActiveTicks = Math.Max(1, activeTicks);
            FollowThroughTicks = Math.Max(1, followThroughTicks);
            HopSpeedX = Math.Max(0f, hopSpeedX);
            HopSpeedY = Math.Max(0f, hopSpeedY);
            TelegraphRunUpTicks = Math.Max(0, telegraphRunUpTicks);
            TelegraphRunUpAcceleration = Math.Max(0f, telegraphRunUpAcceleration);
            TelegraphRunUpTopSpeed = Math.Max(0f, telegraphRunUpTopSpeed);
            Damage = Math.Max(1, damage);
            Knockback = Math.Max(0f, knockback);
            HitboxReach = Math.Max(1, hitboxReach);
            HitboxHeight = Math.Max(1, hitboxHeight);
            MaximumVerticalOffset = Math.Max(0f, maximumVerticalOffset);
            RequiresLineOfSight = requiresLineOfSight;
            TelegraphColor = telegraphColor;
            AttackSound = attackSound;
        }

        public bool CanStart(NPC npc)
        {
            if (npc.velocity.Y != 0f)
            {
                return false;
            }

            if (!ComboMove.IsInRange(npc))
            {
                return false;
            }

            Player target = Main.player[npc.target];
            if (Math.Abs(target.Center.Y - npc.Center.Y) > MaximumVerticalOffset)
            {
                return false;
            }

            return !RequiresLineOfSight || Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
        }
    }

    /// <summary>
    /// Two deliberately small reusable melee moves for ordinary humanoid fighters. Presentation remains enemy-local.
    /// </summary>
    public sealed class HumanoidMeleeProfile
    {
        public HumanoidMeleeMove CloseHop { get; }
        public HumanoidMeleeMove LongHop { get; }
        public int OpenerCooldownTicks { get; }
        public int PostSequenceNeutralTicks { get; }
        public bool GuardPressureUnblockable { get; }
        public int GuardPressureTelegraphTicks { get; }
        private Func<NPC, bool> OpenerCondition { get; }

        private HumanoidMeleeProfile(
            HumanoidMeleeMove closeHop,
            HumanoidMeleeMove longHop,
            int openerCooldownTicks,
            int postSequenceNeutralTicks,
            bool guardPressureUnblockable,
            int guardPressureTelegraphTicks,
            Func<NPC, bool> openerCondition)
        {
            CloseHop = closeHop;
            LongHop = longHop;
            OpenerCooldownTicks = openerCooldownTicks;
            PostSequenceNeutralTicks = postSequenceNeutralTicks;
            GuardPressureUnblockable = guardPressureUnblockable;
            GuardPressureTelegraphTicks = Math.Max(1, guardPressureTelegraphTicks);
            OpenerCondition = openerCondition;
        }

        public bool CanStartOpener(NPC npc)
        {
            return OpenerCondition == null || OpenerCondition(npc);
        }

        public static HumanoidMeleeProfile Standard(
            int closeDamage,
            int longDamage,
            bool guardPressureUnblockable = false,
            int guardPressureTelegraphTicks = 90)
        {
            return new HumanoidMeleeProfile(
                new HumanoidMeleeMove(
                    new CombatComboMove(CombatComboMoveKey.CloseHopMelee, 0f, 82f, canRepeat: true, weight: 1.25f),
                    telegraphTicks: 14, activeTicks: 10, followThroughTicks: 12,
                    hopSpeedX: 2.7f, hopSpeedY: 2.3f,
                    telegraphRunUpTicks: 0, telegraphRunUpAcceleration: 0f, telegraphRunUpTopSpeed: 0f,
                    damage: closeDamage, knockback: 2.5f, hitboxReach: 62, hitboxHeight: 44,
                    maximumVerticalOffset: 56f, requiresLineOfSight: false,
                    telegraphColor: Color.Orange, attackSound: SoundID.Item1),
                new HumanoidMeleeMove(
                    new CombatComboMove(CombatComboMoveKey.LongHopMelee, 112f, 240f, canRepeat: false, weight: 0.85f),
                    telegraphTicks: 24, activeTicks: 24, followThroughTicks: 16,
                    hopSpeedX: 6.4f, hopSpeedY: 5.1f,
                    telegraphRunUpTicks: 0, telegraphRunUpAcceleration: 0f, telegraphRunUpTopSpeed: 0f,
                    damage: longDamage, knockback: 3.5f, hitboxReach: 78, hitboxHeight: 48,
                    maximumVerticalOffset: 96f, requiresLineOfSight: true,
                    telegraphColor: Color.OrangeRed, attackSound: SoundID.Item1),
                openerCooldownTicks: 85,
                postSequenceNeutralTicks: 58,
                guardPressureUnblockable: guardPressureUnblockable,
                guardPressureTelegraphTicks: guardPressureTelegraphTicks,
                openerCondition: null);
        }

        public static HumanoidMeleeProfile Elite(
            int closeDamage,
            int longDamage,
            int closeTelegraphTicks = 12,
            int longTelegraphTicks = 22,
            bool guardPressureUnblockable = false,
            int guardPressureTelegraphTicks = 90,
            Func<NPC, bool> openerCondition = null,
            bool leonhardRunUp = false)
        {
            return new HumanoidMeleeProfile(
                new HumanoidMeleeMove(
                    new CombatComboMove(CombatComboMoveKey.CloseHopMelee, 0f, 90f, canRepeat: true, weight: 1.35f),
                    telegraphTicks: closeTelegraphTicks, activeTicks: 10, followThroughTicks: 11,
                    hopSpeedX: 3f, hopSpeedY: 2.5f,
                    telegraphRunUpTicks: leonhardRunUp ? 18 : 0,
                    telegraphRunUpAcceleration: leonhardRunUp ? 0.12f : 0f,
                    telegraphRunUpTopSpeed: leonhardRunUp ? 2.2f : 0f,
                    damage: closeDamage, knockback: 3.5f, hitboxReach: 70, hitboxHeight: 48,
                    maximumVerticalOffset: 64f, requiresLineOfSight: false,
                    telegraphColor: Color.OrangeRed, attackSound: SoundID.Item1),
                new HumanoidMeleeMove(
                    new CombatComboMove(CombatComboMoveKey.LongHopMelee, 112f, 280f, canRepeat: false, weight: 1f),
                    telegraphTicks: longTelegraphTicks, activeTicks: 26, followThroughTicks: 18,
                    hopSpeedX: 7.1f, hopSpeedY: 5.5f,
                    telegraphRunUpTicks: leonhardRunUp ? 20 : 0,
                    telegraphRunUpAcceleration: leonhardRunUp ? 0.22f : 0f,
                    telegraphRunUpTopSpeed: leonhardRunUp ? 4.5f : 0f,
                    damage: longDamage, knockback: 4.5f, hitboxReach: 92, hitboxHeight: 52,
                    maximumVerticalOffset: 108f, requiresLineOfSight: true,
                    telegraphColor: Color.OrangeRed, attackSound: SoundID.Item1),
                openerCooldownTicks: 70,
                postSequenceNeutralTicks: 50,
                guardPressureUnblockable: guardPressureUnblockable,
                guardPressureTelegraphTicks: guardPressureTelegraphTicks,
                openerCondition: openerCondition);
        }
    }
}
