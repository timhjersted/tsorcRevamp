using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class MorgulBlade : ModItem
    {
        public static float BadSummonDmgMultiplier = 33f;
        public static float BadSummonTagStrengthMult = 33f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BadSummonDmgMultiplier, BadSummonTagStrengthMult);
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
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength *= 1f - BadSummonTagStrengthMult / 100f;
        }
    }
}