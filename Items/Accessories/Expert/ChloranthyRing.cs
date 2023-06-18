using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing : ModItem
    {
        public static float StaminaRecoverySpeed = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaRecoverySpeed);
        public override void SetStaticDefaults()
        {
                               // [c/ffbf00:text] is a great yellow for highlights
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.defense = 2;
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
