using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class RingofArtorias : ModItem
    {   
        public static float Dmg = 7f;
        public static int ArmorPen = 7;
        public float PositiveCurseStatBoost = 34f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, ArmorPen, PositiveCurseStatBoost);
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
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.GetArmorPenetration(DamageClass.Generic) += ArmorPen;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.CursePositiveStatsMultiplier += PositiveCurseStatBoost / 100f;
        }

    }
}

