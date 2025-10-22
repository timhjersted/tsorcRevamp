using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class ForgottenThunderBowScroll : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.value = 5000000;
            Item.rare = ItemRarityID.Red;
        }
    }
}