using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Ranged.BlueTearstoneRing
{
    public class BlueTearstoneRing : ModItem
    {
        public const float DmgCap = 25f;
        public const float SecondsForMaxDmg = 1.5f;
        public const float DmgPerTick = DmgCap / SecondsForMaxDmg / 60f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgCap, SecondsForMaxDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ModContent.RarityType<DarkBlue>();
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BlueTearstoneRingPlayer>().Equipped = true;
        }
    }
}
