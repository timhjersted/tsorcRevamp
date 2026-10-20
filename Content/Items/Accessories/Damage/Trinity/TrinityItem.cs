using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.Trinity
{
    public class TrinityItem : ModItem
    {
        public const float CritChance = 100f;
        public const int Duration = 2;
        public const int Cooldown = 4;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Cooldown, CritChance, Duration);

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<TrinityPlayer>().Equipped = true;
        }
    }
}
