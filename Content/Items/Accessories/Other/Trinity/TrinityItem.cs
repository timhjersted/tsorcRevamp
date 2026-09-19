using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Other.Trinity
{
    [LegacyName("Trinity")]
    public class TrinityItem : ModItem
    {
        public const float LifeThreshold = 50f;
        public override void SetStaticDefaults()
        {
        }

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

            // Gain trinity buff if hp is below 50%
            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.AddBuff(ModContent.BuffType<Buffs.Accessories.TrinityBuff>(), 2 * 60); // 2 seconds
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                modPlayer.SetAuraState(tsorcAuraState.TripleThreat);
                modPlayer.effectRadius = 250f;
            }
        }
    }
}
