using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class StuddedLeatherArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases damage by 10%" +
                "\nSet bonus: Increases all attack speed by 15%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 2;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.1f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<StuddedLeatherHelmet>() && legs.type == ModContent.ItemType<StuddedLeatherGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
        }
    }
}
