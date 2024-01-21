using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class DragonStone : ModItem
    {
        public static int Potency = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Potency);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().DragonStoneImmunity = true;
            tsorcRevampPlayer.DragonStonePotency = true;
        }
        //dropped by hellkite dragon
    }
}
