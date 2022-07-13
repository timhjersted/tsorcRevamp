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
            Tooltip.SetDefault("+10% minion damage\nSet Bonus: Increases your max number of minions by 1\n+10% whip speed");
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
            player.GetDamage(DamageClass.Summon) += 0.1f;
        }
    }
}
