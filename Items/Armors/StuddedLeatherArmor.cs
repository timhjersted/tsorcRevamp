using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class StuddedLeatherArmor : ModItem
    {
        public static int FlatDmg = 2;
        public static float AtkSpeed = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDmg, AtkSpeed);
        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Generic).Flat += FlatDmg;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<StuddedLeatherHelmet>() && legs.type == ModContent.ItemType<StuddedLeatherGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += AtkSpeed / 100f;
        }
    }
}
