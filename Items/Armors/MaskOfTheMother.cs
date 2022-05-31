using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheMother : ModItem
    {

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 26;
            Item.height = 30;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void DrawHair(ref bool drawHair, ref bool drawAltHair)
        {
            drawAltHair = true;
        }
    }
}
