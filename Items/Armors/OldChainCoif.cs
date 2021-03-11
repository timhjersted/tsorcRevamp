using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class OldChainCoif : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 20;
            item.defense = 1;
            item.value = 3000;
            item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OldChainArmor>() && legs.type == ModContent.ItemType<OldChainGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.statDefense += 3;
        }
    }
}
