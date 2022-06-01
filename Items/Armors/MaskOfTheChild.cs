using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheChild : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 44;
            Item.height = 40;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }
    }
}
