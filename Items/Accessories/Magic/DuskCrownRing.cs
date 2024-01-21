using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class DuskCrownRing : ModItem
    {
        public static float MagicDmg = 100f;
        public static int MagicCrit = 50;
        public static int LifeRegen = 7;
        public static float BadMaxLife = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MagicDmg, MagicCrit, LifeRegen, BadMaxLife);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }


        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 = (int)(player.statLifeMax2 * (1f - BadMaxLife / 100f));
            player.GetDamage(DamageClass.Magic) += MagicDmg / 100f;
            player.GetCritChance(DamageClass.Magic) += MagicCrit;
            player.lifeRegen += LifeRegen;
        }
    }
}