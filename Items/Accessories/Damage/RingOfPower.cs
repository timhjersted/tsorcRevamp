using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class RingOfPower : ModItem
    {
        public static float Crit = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Crit);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.AddBuff(BuffID.Darkness, 2, false);
            player.AddBuff(BuffID.Battle, 2, false);
            player.GetCritChance(DamageClass.Generic) += Crit;
        }

    }
}

