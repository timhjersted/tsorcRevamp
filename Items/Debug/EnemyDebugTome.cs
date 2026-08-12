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
            Item.useAnimation = 5;
            Item.useTime = 5;
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
                    enemyUI.MovingEvent = null;
                    Main.NewText("Cancelled NPC placement.");
                    return false;
                }

                // NOTE: right-click no longer deletes anything (removed placed NPCs / quick-add events). Deletion is
                // intentionally button-only now (the config panel's delete button) to prevent accidental loss. If the
                // config menu is open, swallow the right-click so it can't accidentally spawn a new event mid-edit.
                if (configUI.Visible && configUI.CurrentEvent != null)
                {
                    return false;
                }

                // Otherwise, Right click creates a new event!
                Vector2 mousePos = tsorcRevampPlayer.RealMouseWorld;
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
                    // Also count clicking ON the event's (often much larger) detection ring — matches the
                    // ring-hit-test added to the left-click "open this event" branch below, same rationale.
                    float ringRadius = (float)System.Math.Sqrt(ev.Radius);
                    if (System.Math.Abs(dist - ringRadius) < 24)
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
                newEvent.CenterX = (int)(tsorcRevampPlayer.RealMouseWorld.X / 16);
                newEvent.CenterY = (int)(tsorcRevampPlayer.RealMouseWorld.Y / 16);
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
                    // Relocating a single-NPC event: move both the event center and its one NPC to the new tile.
                    if (enemyUI.MovingEvent != null)
                    {
                        var moveEv = enemyUI.MovingEvent;
                        int moveType = moveEv.Npcs.Count > 0 ? moveEv.Npcs[0].NpcID : enemyUI.SelectedNpcType;
                        tsorcRevampSystems.GetPlacementTile(moveType, out int tileX, out int tileY);

                        moveEv.CenterX = tileX;
                        moveEv.CenterY = tileY;
                        if (moveEv.Npcs.Count > 0)
                        {
                            moveEv.Npcs[0].SpawnX = tileX;
                            moveEv.Npcs[0].SpawnY = tileY;
                        }
                        tsorcScriptedEvents.SaveDynamicEvents();

                        enemyUI.SelectedNpcType = 0;
                        enemyUI.MovingEvent = null;

                        NPC moved = new NPC();
                        moved.SetDefaults(moveEv.Npcs.Count > 0 ? moveEv.Npcs[0].NpcID : 0);
                        Main.NewText($"Moved {moved.TypeName} event to ({tileX}, {tileY})");
                        return true;
                    }

                    if (enemyUI.QuickAddMode)
                    {
                        // Quick Add: each placement becomes its own single-NPC event using the panel defaults.
                        tsorcRevampSystems.GetPlacementTile(enemyUI.SelectedNpcType, out int tileX, out int tileY);

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
                        entry.NpcName = tsorcScriptedEvents.GetNpcStableName(enemyUI.SelectedNpcType);
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
                        npc.NpcName = tsorcScriptedEvents.GetNpcStableName(enemyUI.SelectedNpcType);
                        tsorcRevampSystems.GetPlacementTile(enemyUI.SelectedNpcType, out int placeX, out int placeY);
                        npc.SpawnX = placeX;
                        npc.SpawnY = placeY;
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
                        // Use sprite center + a radius derived from actual NPC dimensions (matches the marker).
                        Vector2 npcCenter = tsorcRevampSystems.NpcSpawnCenter(npc.NpcID, npc.SpawnX, npc.SpawnY);
                        float grabRadius = tsorcRevampSystems.NpcHitRadius(npc.NpcID);
                        float dist = Vector2.Distance(tsorcRevampPlayer.RealMouseWorld, npcCenter);
                        if (dist < grabRadius && dist < closestNpcDist)
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

                // Priority 2b: A single-NPC (quick-add) event that's already open — clicking its NPC grabs the
                // whole event to relocate it (the NPC is the event marker, so moving it moves the event).
                if (configUI.Visible && configUI.CurrentEvent != null && configUI.CurrentEvent.SingleNpcMarker)
                {
                    var ev = configUI.CurrentEvent;
                    if (ev.Npcs.Count > 0)
                    {
                        var npc = ev.Npcs[0];
                        Vector2 npcCenter = tsorcRevampSystems.NpcSpawnCenter(npc.NpcID, npc.SpawnX, npc.SpawnY);
                        float grabRadius = tsorcRevampSystems.NpcHitRadius(npc.NpcID);
                        if (Vector2.Distance(tsorcRevampPlayer.RealMouseWorld, npcCenter) < grabRadius)
                        {
                            NPC tempSize = new NPC();
                            tempSize.SetDefaults(npc.NpcID);
                            enemyUI.SelectedNpcType = npc.NpcID;
                            enemyUI.QuickAddMode = false;
                            enemyUI.MovingEvent = ev;
                            Main.NewText($"Grabbed {tempSize.TypeName} event. Left click to move it, right click to cancel.");
                            return true;
                        }
                    }
                }

                // Priority 3: Check if clicking on/near an existing event center, its detection RING, or any placed
                // NPC sprite. Checking the ring matters because it's usually the most visually prominent part of an
                // event (radii of dozens of tiles are common) — without it, clicking anywhere on a large ring except
                // its exact 3-tile center silently fell through to P4 (Quick Add) instead of opening the event.
                Vector2 mousePos = tsorcRevampPlayer.RealMouseWorld;
                DynamicSpawnEvent closestEvent = null;
                float closestDist = float.MaxValue;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev)) continue;

                    Vector2 evCenter = new Vector2(ev.CenterX * 16 + 8, ev.CenterY * 16 + 8);
                    float centerDist = Vector2.Distance(mousePos, evCenter);

                    // Event center / book marker
                    if (centerDist < 48 && centerDist < closestDist)
                    {
                        closestDist = centerDist;
                        closestEvent = ev;
                    }

                    // Detection ring (click within a band around its circumference).
                    float ringRadius = (float)System.Math.Sqrt(ev.Radius);
                    float ringDist = System.Math.Abs(centerDist - ringRadius);
                    if (ringDist < 24 && ringDist < closestDist)
                    {
                        closestDist = ringDist;
                        closestEvent = ev;
                    }

                    // Placed NPC sprites
                    foreach (var npc in ev.Npcs)
                    {
                        Vector2 npcCenter = tsorcRevampSystems.NpcSpawnCenter(npc.NpcID, npc.SpawnX, npc.SpawnY);
                        float clickRadius = tsorcRevampSystems.NpcHitRadius(npc.NpcID);
                        float dist = Vector2.Distance(mousePos, npcCenter);
                        if (dist < clickRadius && dist < closestDist)
                        {
                            closestDist = dist;
                            closestEvent = ev;
                        }
                    }
                }

                if (closestEvent != null)
                {
                    // Opening an event closes the Quick Add panel.
                    if (enemyUI.Visible) enemyUI.Hide();
                    configUI.SetEvent(closestEvent);
                    configUI.Show();
                    return true;
                }

                // Priority 4: Clicked empty space with nothing near -> open the Quick Add panel.
                // Don't open if the event config is already showing; clicking empty space while editing an event
                // should do nothing rather than opening a second panel.
                if (!enemyUI.Visible && !configUI.Visible)
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
