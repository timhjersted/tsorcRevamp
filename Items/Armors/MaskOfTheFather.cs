using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheFather : ModItem
    {

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 34;
            Item.height = 48;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void DrawHair(ref bool drawHair, ref bool drawAltHair)
        {
            drawAltHair = true;
        }
    }
}
