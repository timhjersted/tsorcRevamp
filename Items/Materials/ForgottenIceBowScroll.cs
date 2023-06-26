using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class ForgottenIceBowScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Pink;
            Item.width = 12;
            Item.height = 12;
            Item.value = 5000000;
        }

    }
}
