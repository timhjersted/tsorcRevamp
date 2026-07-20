using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class OwlFatherMask : ModItem
    {
        public static float MoveSpeed = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.value = 10000;
            Item.rare = ItemRarityID.Yellow;
            Item.defense = 10;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OwlFatherArmor>() && legs.type == ModContent.ItemType<OwlFatherGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.OwlFatherMask.SetBonus");
            player.GetDamage(DamageClass.Melee) += 0.15f;
        }
    }
}
