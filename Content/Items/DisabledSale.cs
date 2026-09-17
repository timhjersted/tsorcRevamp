using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items
{
    class DisabledSale : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.value = 999999999;
        }
    }
}
