using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Items.Materials
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
            Item.maxStack = 30;
            Item.value = 2;
        }
    }
}
