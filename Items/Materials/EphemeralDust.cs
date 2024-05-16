using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class EphemeralDust : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.rare = ItemRarityID.Green;
            Item.value = 1000;
            Item.maxStack = Item.CommonMaxStack;
        }
    }
}
