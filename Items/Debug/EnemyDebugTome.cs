using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.UI;

namespace tsorcRevamp.Items.Debug
{
    class EnemyDebugTome : ModItem
    {
        public static bool JustClosedUI = false;

        public override string Texture => "tsorcRevamp/Items/Debug/EnemyDebugTome";

        public override void SetStaticDefaults()
        {
            // Tooltip
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot; // For pointing at the mouse
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.noMelee = true;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; // Enables right-click
        }

        public override bool CanUseItem(Player player)
        {
            if (JustClosedUI)
            {
                return false;
            }

            // Prevent use if interacting with UI
            if (player.mouseInterface)
                return false;

            var configUI = ModContent.GetInstance<tsorcRevamp>().SpawnPointConfigUI;
            var enemyUI = ModContent.GetInstance<tsorcRevamp>().EnemySelectionUI;

            if (configUI.Visible && configUI.panel.ContainsPoint(Main.MouseScreen))
            {
                return false;
            }

            if (enemyUI.Visible && enemyUI.panel.ContainsPoint(Main.MouseScreen))
            {
                return false;
            }

            if (player.altFunctionUse == 2) // Right click
            {
                if (enemyUI.SelectedNpcType != 0)
                {
                    enemyUI.SelectedNpcType = 0;
                    enemyUI.QuickAddMode = false;
                    Main.NewText("Cancelled NPC placement.");
                    return false;
                }

                // If config menu is open, try to remove a placed NPC on right click
                if (configUI.Visible && configUI.CurrentEvent != null)
                {
                    var ev = configUI.CurrentEvent;
                    DynamicSpawnEntry npcToRemove = null;
                    float closestNpcDist = float.MaxValue;
                    foreach (var npc in ev.Npcs)
                    {
                        Vector2 npcPos = new Vector2(npc.SpawnX * 16 + 8, npc.SpawnY * 16 + 16);
                        float dist = Vector2.Distance(Main.MouseWorld, npcPos);
                        if (dist < 24 && dist < closestNpcDist)
                        {
                            closestNpcDist = dist;
                            npcToRemove = npc;
                        }
                    }

                    if (npcToRemove != null)
                    {
                        ev.Npcs.Remove(npcToRemove);

                        NPC temp = new NPC();
                        temp.SetDefaults(npcToRemove.NpcID);

                        // A quick-add event IS its single NPC. Removing it would leave an invisible, unclickable
                        // orphan event, so delete the whole event instead and close the menu.
                        if (ev.SingleNpcMarker)
                        {
                            tsorcScriptedEvents.DynamicEvents.Remove(ev);
                            tsorcScriptedEvents.SaveDynamicEvents();
                            configUI.Hide();
                            Main.NewText($"Deleted quick-add event ({temp.TypeName}).");
                        }
                        else
                        {
                            configUI.RefreshList();
                            tsorcScriptedEvents.SaveDynamicEvents();
                            Main.NewText($"Removed placed {temp.TypeName} from event.");
                        }
                        return true;
                    }
                }

                // Otherwise, Right click creates a new event!
                Vector2 mousePos = Main.MouseWorld;
                bool eventTooClose = false;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev)) continue;
                    float dist = Vector2.Distance(mousePos, new Vector2(ev.CenterX * 16 + 8, ev.CenterY * 16 + 8));
                    if (dist < 32) // Within 2 tiles of an existing event center
                    {
                        eventTooClose = true;
                        break;
                    }
                }

                if (eventTooClose)
                {
                    Main.NewText("Cannot place event: Too close to an existing event.");
                    return false;
                }

                // Create new event
                var newEvent = new DynamicSpawnEvent();
                newEvent.EventID = System.Guid.NewGuid().ToString();
                newEvent.CenterX = (int)(Main.MouseWorld.X / 16);
                newEvent.CenterY = (int)(Main.MouseWorld.Y / 16);
                newEvent.Radius = (float)System.Math.Pow(30 * 16, 2); // default (30 tiles squared pixel distance)
                newEvent.TriggerDust = DustID.Shadowflame;
                newEvent.SaveOnCompletion = true; // default
                if (tsorcRevampWorld.RemixMap)
                {
                    newEvent.WorldCondition = "RemixMapCondition";
                }
                else if (tsorcRevampWorld.OnlyAdventureMap)
                {
                    newEvent.WorldCondition = "OnlyAdventureMapCondition";
                }
                else
                {
                    newEvent.WorldCondition = "";
                }
                newEvent.MapCondition = ""; // Spawn condition defaults to None

                tsorcScriptedEvents.DynamicEvents.Add(newEvent);
                tsorcScriptedEvents.SaveDynamicEvents();
                Main.NewText("Created new Dynamic Event Trigger at " + newEvent.CenterX + ", " + newEvent.CenterY);

                // Immediately open it in the configurator
                configUI.SetEvent(newEvent);
                configUI.Show();
            }
            else // Left click
            {
                // Priority 1: If an NPC is currently selected/grabbed, place it.
                if (enemyUI.SelectedNpcType != 0)
                {
                    if (enemyUI.QuickAddMode)
                    {
                        // Quick Add: each placement becomes its own single-NPC event using the panel defaults.
                        int tileX = (int)(Main.MouseWorld.X / 16);
                        int tileY = (int)(Main.MouseWorld.Y / 16);

                        var quickEvent = new DynamicSpawnEvent();
                        quickEvent.EventID = System.Guid.NewGuid().ToString();
                        quickEvent.CenterX = tileX;
                        quickEvent.CenterY = tileY;
                        quickEvent.Radius = (float)System.Math.Pow(enemyUI.DefRadiusTiles * 16, 2);
                        quickEvent.TriggerDust = enemyUI.DefDust;
                        quickEvent.SaveOnCompletion = enemyUI.DefSave;
                        quickEvent.VisibleRing = enemyUI.DefRing;
                        quickEvent.WorldCondition = enemyUI.DefWorld ?? "";
                        quickEvent.MapCondition = enemyUI.DefSpawn ?? "";
                        quickEvent.SingleNpcMarker = true;

                        var entry = new DynamicSpawnEntry();
                        entry.NpcID = enemyUI.SelectedNpcType;
                        entry.SpawnX = tileX;
                        entry.SpawnY = tileY;
                        quickEvent.Npcs.Add(entry);

                        tsorcScriptedEvents.DynamicEvents.Add(quickEvent);
                        tsorcScriptedEvents.SaveDynamicEvents();

                        NPC temp = new NPC();
                        temp.SetDefaults(entry.NpcID);
                        Main.NewText($"Quick-added {temp.TypeName} event at ({tileX}, {tileY})");

                        // Keep the enemy on the cursor for rapid repeated placement.
                        return true;
                    }

                    if (configUI.Visible && configUI.CurrentEvent != null)
                    {
                        // Add NPC to the event
                        var ev = configUI.CurrentEvent;
                        var npc = new DynamicSpawnEntry();
                        npc.NpcID = enemyUI.SelectedNpcType;
                        npc.SpawnX = (int)(Main.MouseWorld.X / 16);
                        npc.SpawnY = (int)(Main.MouseWorld.Y / 16);
                        ev.Npcs.Add(npc);
                        configUI.RefreshList();
                        tsorcScriptedEvents.SaveDynamicEvents();

                        NPC temp = new NPC();
                        temp.SetDefaults(npc.NpcID);
                        Main.NewText($"Placed {temp.TypeName} at ({npc.SpawnX}, {npc.SpawnY})");
                    }
                    else
                    {
                        Main.NewText("Cannot place NPC: Event settings menu is closed.");
                    }

                    // Detach from cursor
                    enemyUI.SelectedNpcType = 0;
                    return true;
                }

                // Priority 2: If a multi-NPC event is open, check if clicking on a placed NPC to grab it (move mode).
                // Quick-add events are skipped here: their NPC is the marker, so clicking it just opens the event (below).
                if (configUI.Visible && configUI.CurrentEvent != null && !configUI.CurrentEvent.SingleNpcMarker)
                {
                    var ev = configUI.CurrentEvent;
                    DynamicSpawnEntry npcToGrab = null;
                    float closestNpcDist = float.MaxValue;
                    foreach (var npc in ev.Npcs)
                    {
                        Vector2 npcPos = new Vector2(npc.SpawnX * 16 + 8, npc.SpawnY * 16 + 16);
                        float dist = Vector2.Distance(Main.MouseWorld, npcPos);
                        if (dist < 24 && dist < closestNpcDist)
                        {
                            closestNpcDist = dist;
                            npcToGrab = npc;
                        }
                    }

                    if (npcToGrab != null)
                    {
                        // Grab it: re-attach to cursor and remove the placed instance.
                        enemyUI.SelectedNpcType = npcToGrab.NpcID;
                        enemyUI.QuickAddMode = false;
                        ev.Npcs.Remove(npcToGrab);
                        configUI.RefreshList();
                        tsorcScriptedEvents.SaveDynamicEvents();

                        NPC temp = new NPC();
                        temp.SetDefaults(npcToGrab.NpcID);
                        Main.NewText($"Grabbed {temp.TypeName}. Left click to place it again, right click to cancel.");
                        return true;
                    }
                }

                // Priority 3: Check if clicking on/near an existing event center (or quick-add NPC marker) to open it.
                Vector2 mousePos = Main.MouseWorld;
                DynamicSpawnEvent closestEvent = null;
                float closestDist = float.MaxValue;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev)) continue;
                    float dist = Vector2.Distance(mousePos, new Vector2(ev.CenterX * 16 + 8, ev.CenterY * 16 + 8));
                    if (dist < 48 && dist < closestDist)
                    {
                        closestDist = dist;
                        closestEvent = ev;
                    }
                }

                if (closestEvent != null)
                {
                    // Opening an event closes the Quick Add panel.
                    if (enemyUI.Visible)
                    {
                        enemyUI.Hide();
                    }
                    configUI.SetEvent(closestEvent);
                    configUI.Show();
                    return true;
                }

                // Priority 4: Clicked empty space with nothing selected -> open the Quick Add panel.
                if (!enemyUI.Visible)
                {
                    enemyUI.Show();
                    return true;
                }
            }
            return true;
        }

        public override void HoldItem(Player player)
        {
            // Reset JustClosedUI state as soon as mouse buttons are released
            if (JustClosedUI && !Main.mouseLeft && !Main.mouseRight)
            {
                JustClosedUI = false;
            }
        }
    }
}
