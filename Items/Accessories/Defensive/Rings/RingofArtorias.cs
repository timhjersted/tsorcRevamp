using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class RingofArtorias : ModItem
    {
        public float PositiveCurseStatBoost = 34f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PositiveCurseStatBoost);
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
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.CursePositiveStatsMultiplier += PositiveCurseStatBoost / 100f;
            player.buffImmune[BuffID.Frozen] = true;
            player.buffImmune[BuffID.Blackout] = true;
            player.buffImmune[BuffID.Obstructed] = true;
            player.buffImmune[BuffID.Venom] = true;
        }

    }
}

