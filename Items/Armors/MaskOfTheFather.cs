using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheFather : ModItem
    {
        public static float Dmg = 14f;
        public static float CritDmgIncrease = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, CritDmgIncrease);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.defense = 8;
            Item.width = 34;
            Item.height = 48;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().MaskOfTheFather = true;
        }
    }
}
