using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheChild : ModItem
    {
        public static float MoveSpeedMult = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeedMult);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.defense = 7;
            Item.width = 44;
            Item.height = 40;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeedMult / 100f;
        }
    }
}
