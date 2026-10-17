using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Throwing;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Hollow Spearman's thrown spear: the player's ThrowingSpear flight and sprite, but hostile from SetDefaults.
    /// The Spearman used to spawn ThrowingSpear and flip friendly/hostile afterwards; those flags are not synced, so in
    /// multiplayer every client saw a friendly spear that could never hit them. Spawn with ai[0] = 15 so gravity applies
    /// from the first tick (ThrowingSpear counts ai[0] up to 15 before dropping).
    /// </summary>
    class HollowSpearmanThrownSpear : ThrowingSpear
    {
        // Normal-mode ticks. Broken Armor stretches in Expert/Master on its own; Crippled goes through ScaledTicks.
        private const int CrippledTicks = 3 * 60;
        private const int BrokenArmorTicks = 3 * 60;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ThrowingSpear));

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.friendly = false;
            Projectile.hostile = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), Crippled.ScaledTicks(CrippledTicks));
            target.AddBuff(BuffID.BrokenArmor, BrokenArmorTicks);
        }
    }
}
