using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing : ModItem
    {
        public static float StaminaRecoverySpeed = 15f;
        /// <summary>Percentage points of shield-hold movement slow this ring cancels (see
        /// tsorcRevampActiveShieldPlayer.ApplyBlockSlow). Ring II replaces this rather than stacking.</summary>
        public static float ShieldSlowReduction = 4f;
        /// <summary>Percent cut from the post-spend stamina regen DELAY (tsorcRevampStaminaPlayer.PauseStaminaRegen).
        /// A different axis from the regen rate — you start recovering sooner rather than recovering faster, which
        /// is what's actually felt between swings. Nothing else in the game modifies this timer.</summary>
        public static float RegenDelayReduction = 10f;
        /// <summary>Percent of normal stamina regen retained while a shield is RAISED (normally zero).</summary>
        public static float BlockStaminaRegen = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaRecoverySpeed);
        public override void SetStaticDefaults()
        {
            // [c/ffbf00:text] is a great yellow for highlights
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRecoverySpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 4;
            player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing1 = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is ChloranthyRing2)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
