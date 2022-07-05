using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class OldChainArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+1 flat minion damage\nSet Bonus: Increases your max number of minions by 1");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.value = 6000;
            Item.rare = ItemRarityID.White;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 1f;
        }
    }
}
