using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Lore
{
    class DodgerollMemo : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
        }
    }
}