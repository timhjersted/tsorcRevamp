using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class ForgottenThunderBowScroll : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Materials/ForgottenIceBowScroll";
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Used with 1 Soul of Artorias and 200000 Dark Soul at a Demon Altar\nSold by a powerful sorcerer");
        }
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 5000000;
            Item.rare = ItemRarityID.Pink;
        }
    }
}