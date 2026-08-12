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
            // Clear BEFORE LoadDynamicEvents(): its per-event skip check reads CompletedDynamicEvents
            // (`if (dEvent.SaveOnCompletion && CompletedDynamicEvents.Contains(dEvent.EventID)) continue;`), so any
            // tome-authored dynamic event that had already completed was previously left out of the reload no matter
            // how many times this tome was used — InitializeScriptedEvents() only resets HARDCODED events'
            // completion tracking (ScriptedEventValues); it never touched this list. That's why a custom event that
            // finished last save stayed permanently "done" even after resetting.
            int clearedCompletedCount = tsorcRevampWorld.CompletedDynamicEvents.Count;
            tsorcRevampWorld.CompletedDynamicEvents = new System.Collections.Generic.List<string>();

            tsorcScriptedEvents.InitializeScriptedEvents();
            tsorcScriptedEvents.LoadDynamicEvents();
            tsorcRevampWorld.NewSlain = new System.Collections.Generic.Dictionary<NPCDefinition, int>();
            // Both calls above are silent on success (no exceptions on failure either), so without this the
            // item appeared to do nothing when used. Confirm it actually ran.
            Main.NewText($"World events reset: {tsorcScriptedEvents.ScriptedEventDict.Count} scripted events reinitialized, " +
                         $"{tsorcScriptedEvents.DynamicEvents.Count} dynamic events reloaded, {clearedCompletedCount} completed dynamic event(s) un-completed, slain-boss tracking cleared.", Microsoft.Xna.Framework.Color.Lime);
            return true;
        }
    }
}
