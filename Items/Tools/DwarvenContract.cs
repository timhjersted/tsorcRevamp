using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Tools
{
    class DwarvenContract : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("A contract for a dwarf guard.\n" + "Will summon a dwarf to guard a piece of property.");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = false;
            Item.value = 10000;
            Item.rare = ItemRarityID.Quest;
            Item.maxStack = Item.CommonMaxStack;
        }
    }
}