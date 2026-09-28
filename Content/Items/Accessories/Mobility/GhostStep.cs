using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Mobility
{
    // PLACEHOLDER sprite (copy of OwlRing) — replace with bespoke art
    public class GhostStep : ModItem
    {
        /// <summary>Extra invulnerable frames. The roll's i-frames ARE its duration (isDodging gates every hit), so this
        /// lengthens the dash itself — see dodgeDuration in tsorcRevampPlayerDodgeRoll.cs.</summary>
        public const int ExtraImmunityFrames = 2;

        /// <summary>How much farther the dash travels than the roll it replaces would have, in tiles.</summary>
        public const int ExtraDistanceTiles = 3;

        /// <summary>Flat cut, in frames, from every post-spend stamina regen delay (applied in PauseStaminaRegen).</summary>
        public const int RegenDelayReductionFrames = 20;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ExtraImmunityFrames, ExtraDistanceTiles, RegenDelayReductionFrames);

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<tsorcRevampPlayer>().GhostStepEquipped = true;
        }
    }
}
