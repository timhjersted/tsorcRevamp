using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class DamagedMechanicalScrap : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 21;
            Item.height = 21;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
