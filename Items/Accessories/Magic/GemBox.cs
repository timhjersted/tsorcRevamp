using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class GemBox : ModItem
    {
        public static float AtkSpeed = 100f;
        public static float BadDmgMult = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed, BadDmgMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetAttackSpeed(DamageClass.Magic) += AtkSpeed / 100f;
            player.GetDamage(DamageClass.Magic) *= 1f - BadDmgMult / 100f;
        }
    }
}
