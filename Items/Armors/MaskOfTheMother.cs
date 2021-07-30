using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheMother : ModItem
    {

        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 26;
            item.height = 30;
            item.value = 10000;
            item.rare = ItemRarityID.Orange;
        }

        public override void DrawHair(ref bool drawHair, ref bool drawAltHair)
        {
            drawAltHair = true;
        }
    }
}
