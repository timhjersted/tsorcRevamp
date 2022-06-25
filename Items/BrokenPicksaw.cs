using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class BrokenPicksaw : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An ancient and powerful tool seems to have been stripped for parts\n" +
                        "The remnants have been drained of their power, and quietly hum with dark magic\n" +
                        "It seems you'll need to find something else that will let you break Lihzahrd Bricks...\n" +
                        "This item no longer serves any purpose, and should be discarded");
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
