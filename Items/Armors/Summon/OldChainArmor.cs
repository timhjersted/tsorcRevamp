using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class OldChainArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("+1 flat minion damage\nSet Bonus: Increases your max number of minions by 1\nA Flinx Fur Coat will also proc this set bonus");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 1;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<OldChainCoif>() && legs.type == ModContent.ItemType<OldChainGreaves>(); 
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
        }
    }
}
