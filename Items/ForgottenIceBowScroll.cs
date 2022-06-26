using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class ForgottenIceBowScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Used with 1 Soul of Artorias and 200000 Dark Soul at a Demon Altar\nSold by a powerful sorcerer");
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
