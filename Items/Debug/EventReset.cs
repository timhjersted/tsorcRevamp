using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Items.Debug
{
    class EventReset : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 21;
            Item.height = 21;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
        }


        public override bool? UseItem(Player player)
        {
            tsorcScriptedEvents.InitializeScriptedEvents();
            tsorcScriptedEvents.LoadDynamicEvents();
            tsorcRevampWorld.NewSlain = new System.Collections.Generic.Dictionary<NPCDefinition, int>();
            // Both calls above are silent on success (no exceptions on failure either), so without this the
            // item appeared to do nothing when used. Confirm it actually ran.
            Main.NewText($"World events reset: {tsorcScriptedEvents.ScriptedEventDict.Count} scripted events reinitialized, " +
                         $"{tsorcScriptedEvents.DynamicEvents.Count} dynamic events reloaded, slain-boss tracking cleared.", Microsoft.Xna.Framework.Color.Lime);
            return true;
        }
    }
}
