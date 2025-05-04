using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Materials
{
    class BrokenDarkMagicRing : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 20;
            Item.height = 20;
        }
    }
}
