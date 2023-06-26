using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    public class FlameOfTheAbyss : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 16;
            Item.rare = ItemRarityID.Orange;
            Item.value = 50000;
            Item.maxStack = 250;
        }
    }
}
