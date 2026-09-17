using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Magic
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class DuskCrownRing : ModItem
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
            player.statLifeMax2 = (int)(player.statLifeMax2 * (1f - BadMaxLife / 100f));
            player.GetDamage(DamageClass.Magic) += MagicDmg / 100f;
            player.lifeRegen += (int)LifeRegen;
        }
    }
}