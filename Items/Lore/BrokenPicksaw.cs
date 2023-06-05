using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Lore
{
    class BrokenPicksaw : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("An ancient and powerful tool seems to have been stripped for parts\n" +
                        "[c/00ffd4:The remnants have been drained of their power, and quietly hum with Attraidies' dark magic.]\n" +
                        "[c/ffbf00:It seems if you want the power to break Lihzahrd bricks you'll have to take it from him]...\n" +
                        "This item no longer serves any purpose, and should be discarded"); */
        }

        public override void SetDefaults()
        {
            Item.width = 21;
            Item.height = 21;
            Item.rare = ItemRarityID.White;
            Item.value = 1000;
        }
    }
}
