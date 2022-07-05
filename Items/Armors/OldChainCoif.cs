using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class OldChainCoif : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+1 flat minion damage");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 1;
            Item.value = 3000;
            Item.rare = ItemRarityID.White;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 1f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OldChainArmor>() && legs.type == ModContent.ItemType<OldChainGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
        }
    }
}
