using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Applied by the Active Shields Revamp when a block exhausts the player's stamina/mana ("guard break").
    /// Reduces stamina regeneration for its duration. The accompanying Ichor/Slow and the brief
    /// "cannot raise shield" lock are handled separately by tsorcRevampActiveShieldPlayer.
    /// Kept distinct from <see cref="ShieldCooldown"/> (which belongs to the magic-shield scroll spells)
    /// so a broken physical guard does not also lock the player out of casting Magic Shield.
    /// </summary>
    public class ShieldGuardBreak : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => false;

        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            // Sharply reduce stamina regeneration while guard-broken.
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 0.2f;
        }
    }
}
