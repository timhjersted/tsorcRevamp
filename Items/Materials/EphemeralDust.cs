using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class EphemeralDust : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 14;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
            Item.maxStack = Item.CommonMaxStack;
        }
    }
}
