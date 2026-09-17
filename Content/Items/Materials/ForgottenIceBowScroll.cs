using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Materials
{
    class ForgottenIceBowScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.width = 12;
            Item.height = 12;
            Item.value = 5000000;
        }

    }
}
