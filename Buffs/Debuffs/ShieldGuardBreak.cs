using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Applied by the Active Shields Revamp when a block exhausts the player's stamina/mana ("guard break").
    /// Reduces stamina regeneration, blocks item use, and suppresses wing/rocket flight for its duration.
    /// The accompanying Ichor/Slow and "cannot raise shield" lock are handled by tsorcRevampActiveShieldPlayer.
    /// Kept distinct from <see cref="ShieldCooldown"/> (which belongs to the magic-shield scroll spells)
    /// so a broken physical guard does not also lock the player out of casting Magic Shield.
    /// </summary>
    public class ShieldGuardBreak : CooldownDebuff
    {
        public const int DurationTicks = 2 * 60;
        public const float StaminaRegenMultiplier = 0.2f;

        public override bool PlaysSoundOnLastTick => false;

        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            // noItems blocks weapons and ordinary healing-item activation. The custom Souls quick-use paths
            // also check these debuffs explicitly because they begin flask animations without ItemCheck.
            player.noItems = true;
            player.wingTime = 0f;
            player.canRocket = false;
            player.rocketTime = 0;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= StaminaRegenMultiplier;
        }
    }
}
