using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.Shield)]
    public class MythrilBulwark: ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Rolling through enemies makes them take 25% more damage");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.defense = 3;
            Item.expert = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilBulwark = true;
        }
    }
}

