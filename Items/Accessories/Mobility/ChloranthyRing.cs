using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing : ModItem
    {
        //used to be 15 at first, but back then it didn't reduce your delay or act as a magnet for stam droplets!!!
        public const float StaminaRecoverySpeed = 8f;
        /// <summary>Percent cut from the post-spend stamina regen DELAY (tsorcRevampStaminaPlayer.PauseStaminaRegen).
        /// A different axis from the regen rate — you start recovering sooner rather than recovering faster, which
        /// is what's actually felt between swings. Nothing else in the game modifies this timer.</summary>
        public const float RegenDelayReduction = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaRecoverySpeed, RegenDelayReduction);
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
