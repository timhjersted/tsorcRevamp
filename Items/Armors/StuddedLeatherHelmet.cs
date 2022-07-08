using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class StuddedLeatherHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases crit chance by 5%");
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.defense = 1;
            Item.value = 900;
            Item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<StuddedLeatherArmor>() && legs.type == ModContent.ItemType<StuddedLeatherGreaves>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 5;
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
        }
    }
}
