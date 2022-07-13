using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class OldChainCoif : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+3 flat minion damage");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 1;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 3;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OldChainArmor>() && legs.type == ModContent.ItemType<OldChainGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
            player.GetAttackSpeed(DamageClass.Summon) += 0.1f;
        }
    }
}
