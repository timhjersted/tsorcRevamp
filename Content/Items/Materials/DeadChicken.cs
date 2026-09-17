using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;

namespace tsorcRevamp.Content.Items.Materials
{
    class DeadChicken : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CookedChicken.Healing, CookedChicken.BaseSickness);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = 12;
            Item.width = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = 2;
        }
    }
}
