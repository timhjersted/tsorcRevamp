using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    /// <summary>
    /// Converts damage declared for Expert Mode into the values Terraria expects before player
    /// defense, damage variation, and other hit modifiers are applied.
    /// </summary>
    public static class EnemyDamage
    {
        /// <summary>
        /// Use as the damage argument to Projectile.NewProjectile for one hostile projectile hit.
        /// Projectile-based melee hitboxes use this path too; they are not NPC contact damage.
        /// The hit lands at the declared Expert value scaled by the difficulty's enemy-damage ratio:
        /// half in Normal, 1x in Expert, 1.5x in Master, and the slider's share in Journey.
        /// Rounding can shift the resulting hit by a few points.
        /// Not valid for vanilla types in ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling
        /// (bombs, rockets, ...) or reflected projectiles: vanilla skips the difficulty multiplier for them.
        /// </summary>
        public static int Projectile(int expertDamageBeforeDefense)
        {
            if (expertDamageBeforeDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(expertDamageBeforeDefense));

            // Vanilla already scales the hit by difficulty: Projectile.Damage doubles the spawn damage, and
            // CombinedHooks.ModifyHitByProjectile multiplies it by EnemyDamageMultiplier (1 / 2 / 3, or the
            // Journey slider). So the spawn value is the Expert hit / (2 x 2) in every mode - scaling it here
            // too applied the ratio twice (Normal hit 1/4, Master 2.25x).
            return RoundDamage(expertDamageBeforeDefense / 4d);
        }

        /// <summary>
        /// Set contact damage in SetDefaults, or on the server when changing it during AI.
        /// The Expert Mode baseline is scaled for the current difficulty and restored on spawn
        /// after Terraria's automatic NPC stat scaling. Terraria does not multiply NPC.damage
        /// again when contact occurs.
        /// </summary>
        public static void SetContact(NPC npc, int expertDamageBeforeDefense)
        {
            if (expertDamageBeforeDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(expertDamageBeforeDefense));

            int scaledDamage = ScaleContact(expertDamageBeforeDefense);
            bool changed = npc.damage != scaledDamage || npc.defDamage != scaledDamage;
            npc.GetGlobalNPC<DeclaredContactDamage>().ExpertValue = expertDamageBeforeDefense;
            npc.damage = scaledDamage;
            npc.defDamage = npc.damage;
            if (npc.active && changed)
                npc.netUpdate = true;
        }

        internal static int ScaleContact(int expertDamageBeforeDefense)
            => RoundDamage(expertDamageBeforeDefense * ExpertDamageScale);

        private static double ExpertDamageScale
            => Main.GameModeInfo.EnemyDamageMultiplier / GameModeData.ExpertMode.EnemyDamageMultiplier;

        private static int RoundDamage(double damage)
            => checked((int)Math.Round(damage, MidpointRounding.AwayFromZero));
    }

    internal sealed class DeclaredContactDamage : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        internal int? ExpertValue;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (ExpertValue.HasValue)
            {
                int scaledDamage = EnemyDamage.ScaleContact(ExpertValue.Value);
                bool changed = npc.damage != scaledDamage || npc.defDamage != scaledDamage;
                npc.damage = scaledDamage;
                npc.defDamage = npc.damage;
                if (changed)
                    npc.netUpdate = true;
            }
        }
    }
}
