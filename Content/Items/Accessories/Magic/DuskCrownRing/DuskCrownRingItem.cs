using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Magic.DuskCrownRing
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class DuskCrownRingItem : ModItem
    {
        public const float MagicDmg = 100f;
        public const float LifeRegen = 8f;
        public const float BadMaxLife = 45f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MagicDmg, LifeRegen / 2f, BadMaxLife);
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


        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Magic) += MagicDmg / 100f;
            player.lifeRegen += (int)LifeRegen;
        }
    }
}