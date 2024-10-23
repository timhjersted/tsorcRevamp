using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

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
            Item.value = PriceByRarity.Cyan_9;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().DragonStoneImmunity = true;
            tsorcRevampPlayer.DragonStonePotency = true;
        }
    }
}
