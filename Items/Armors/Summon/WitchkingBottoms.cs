using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Legs)]
    public class WitchkingBottoms : ModItem
    {    
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("+20% minion damage\nIncreases your max number of minions and turrets by 1\n+44% movement speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 19;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.2f;
            player.maxMinions += 1;
            player.maxTurrets += 1;
            player.moveSpeed += 0.44f;
        }
    }
}

