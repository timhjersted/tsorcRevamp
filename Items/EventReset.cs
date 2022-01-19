using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class EventReset : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Re-enables ALL events and un-saves saved ones. Dev testing item, you should not have this!!");
        }

        public override void SetDefaults()
        {
            item.width = 21;
            item.height = 21;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 45;
            item.useTime = 45;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Lime;
        }


        public override bool UseItem(Player player)
        {
            tsorcScriptedEvents.InitializeScriptedEvents();
            return true;
        }
    }
}
