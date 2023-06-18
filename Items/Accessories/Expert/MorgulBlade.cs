using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class MorgulBlade : ModItem
    {
        public static float BadSummonDmgMultiplier = 35f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BadSummonDmgMultiplier);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) *= 1f - BadSummonDmgMultiplier / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().MaxMinionTurretMultiplier = 2;
        }
    }
}