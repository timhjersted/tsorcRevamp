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
        /// A single hit in Expert Mode is approximately four times the spawn damage in this mod.
        /// Projectile-based melee hitboxes use this path too; they are not NPC contact damage.
        /// Scale the requested Expert hit by the current mode's enemy-damage ratio before
        /// converting to spawn damage. Rounding can shift the resulting hit by a few points.
        /// </summary>
        public static int Projectile(int expertDamageBeforeDefense)
        {
            if (expertDamageBeforeDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(expertDamageBeforeDefense));

            return RoundDamage(expertDamageBeforeDefense * ExpertDamageScale / 4d);
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
