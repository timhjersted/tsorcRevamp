using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Debug
{
    class EnemyDebugTome : ModItem
    {
        public static bool JustClosedUI = false;

        // Track physical presses independently of vanilla item use, which can discard a click after UI hover.
        private static bool previousLeft;
        private static bool previousRight;


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
            if (Main.dedServ || player.whoAmI != Main.myPlayer || Main.mapFullscreen)
            {
                return false;
            }

            var configUI = ModContent.GetInstance<tsorcRevamp>().SpawnPointConfigUI;
            var enemyUI = ModContent.GetInstance<tsorcRevamp>().EnemySelectionUI;

            // Placement clicks are handled in UpdatePlacementInput. Vanilla item use can reject a press
            // before CanUseItem runs (or leave releaseUseItem false after UI interaction).
            if (enemyUI.SelectedNpcType != 0 || enemyUI.MovingEvent != null)
            {
                return false;
            }

            if (configUI.Visible && configUI.panel.ContainsPoint(Main.MouseScreen))
            {
                return false;
            }

            if (enemyUI.Visible && enemyUI.panel.ContainsPoint(Main.MouseScreen))
            {
                return false;
            }

            if (JustClosedUI || player.mouseInterface)
            {
                return false;
            }

            Vector2 mousePos = tsorcRevampPlayer.RealMouseWorld;
            DynamicSpawnEvent markerEvent = FindEventMarker(mousePos);
            if (markerEvent != null)
            {
                if (enemyUI.Visible)
                {
                    enemyUI.Hide();
                }

                if (player.altFunctionUse != 2 && Main.netMode == NetmodeID.SinglePlayer)
                {
                    if (configUI.Visible)
                    {
                        configUI.Hide();
                    }

                    enemyUI.MovingEvent = markerEvent;
                    // Wait for release so the press that picks up the marker cannot also drop it.
                    JustClosedUI = true;
                    return false;
                }

                configUI.SetEvent(markerEvent);
                configUI.Show();
                return true;
            }

            if (player.altFunctionUse == 2) // Right click
            {
                // NOTE: right-click no longer deletes anything (removed placed NPCs / quick-add events). Deletion is
                // intentionally button-only now (the config panel's delete button) to prevent accidental loss. If the
                // config menu is open, swallow the right-click so it can't accidentally spawn a new event mid-edit.
                if (configUI.Visible && configUI.CurrentEvent != null)
                {
                    return false;
                }

                // Otherwise, Right click creates a new event!
                bool eventTooClose = false;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev))
                    {
                        continue;
                    }

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
                // A placed NPC in the open regular event can be grabbed independently of its trigger.
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

                // The ring, area around the book, and individual NPC spawns still open the event settings.
                DynamicSpawnEvent closestEvent = null;
                float closestDist = float.MaxValue;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev))
                    {
                        continue;
                    }

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
                    if (enemyUI.Visible)
                    {
                        enemyUI.Hide();
                    }

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

        internal static DynamicSpawnEvent FindEventMarker(Vector2 mousePos)
        {
            DynamicSpawnEvent closestEvent = null;
            float closestDistance = float.MaxValue;
            var events = tsorcScriptedEvents.DynamicEvents;
            if (events == null)
            {
                return null;
            }

            foreach (var dynamicEvent in events)
            {
                if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(dynamicEvent))
                {
                    continue;
                }

                Vector2 center = new Vector2(dynamicEvent.CenterX * 16f + 8f, dynamicEvent.CenterY * 16f + 8f);
                float hitRadius = 22f; // Covers the 28-by-30 book sprite, including its corners.
                if (dynamicEvent.SingleNpcMarker)
                {
                    if (dynamicEvent.Npcs.Count != 1 || dynamicEvent.Npcs[0].NpcID == 0)
                    {
                        continue;
                    }

                    DynamicSpawnEntry spawn = dynamicEvent.Npcs[0];
                    Vector2 bottom = tsorcRevampSystems.NpcSpawnBottom(spawn.SpawnX, spawn.SpawnY);
                    // Nearby sprites use their hitbox dimensions; distant markers need no NPC initialization.
                    if (Vector2.DistanceSquared(mousePos, bottom) > 3000f * 3000f)
                    {
                        continue;
                    }

                    NPC markerNpc = new NPC();
                    markerNpc.SetDefaults(spawn.NpcID);
                    center = bottom - new Vector2(0f, markerNpc.height / 2f);
                    hitRadius = Math.Max(24f, Math.Max(markerNpc.width, markerNpc.height) / 2f);
                }

                float distance = Vector2.Distance(mousePos, center);
                if (distance < hitRadius && distance < closestDistance)
                {
                    closestEvent = dynamicEvent;
                    closestDistance = distance;
                }
            }

            return closestEvent;
        }

        // Called from ModSystem.UpdateUI after the editor panels update. UIElement hit tests use the current
        // mouse position, while vanilla's mouseInterface/releaseUseItem can still describe the previous click.
        internal static void UpdatePlacementInput()
        {
            bool leftPressed = Main.mouseLeft && !previousLeft;
            bool rightPressed = Main.mouseRight && !previousRight;
            previousLeft = Main.mouseLeft;
            previousRight = Main.mouseRight;

            Player player = Main.LocalPlayer;
            if (player.HeldItem.type != ModContent.ItemType<EnemyDebugTome>() || Main.gameMenu || Main.mapFullscreen)
            {
                return;
            }

            var mod = ModContent.GetInstance<tsorcRevamp>();
            var enemyUI = mod.EnemySelectionUI;
            var configUI = mod.SpawnPointConfigUI;
            if ((enemyUI.SelectedNpcType == 0 && enemyUI.MovingEvent == null) || JustClosedUI ||
                (enemyUI.Visible && enemyUI.panel.ContainsPoint(Main.MouseScreen)) ||
                (configUI.Visible && configUI.panel.ContainsPoint(Main.MouseScreen)))
            {
                return;
            }

            if (enemyUI.MovingEvent != null)
            {
                if (rightPressed)
                {
                    enemyUI.MovingEvent = null;
                    JustClosedUI = true;
                    Main.NewText("Event move cancelled.");
                    return;
                }

                if (!leftPressed || player.mouseInterface)
                {
                    return;
                }

                var movingEvent = enemyUI.MovingEvent;
                int markerType = enemyUI.MovingEventNpcType;
                enemyUI.MovingEvent = null;
                JustClosedUI = true; // Consume this press if item use runs after UpdateUI.

                tsorcRevampSystems.GetPlacementTile(markerType, out int tileX, out int tileY);
                Point destination = new Point(tileX, tileY);
                if (tsorcScriptedEvents.TryMoveDynamicEvent(movingEvent, destination, out string error))
                {
                    Main.NewText($"Event placed at ({tileX}, {tileY}).");
                }
                else
                {
                    Main.NewText($"{error} Event move cancelled.", Color.OrangeRed);
                }

                return;
            }

            if (rightPressed)
            {
                enemyUI.SelectedNpcType = 0;
                enemyUI.QuickAddMode = false;
                JustClosedUI = true; // Prevent the same press from creating an event in CanUseItem.
                Main.NewText("Cancelled NPC placement.");
                return;
            }

            if (!leftPressed)
            {
                return;
            }

            if (enemyUI.QuickAddMode)
            {
                // Quick Add keeps the selected enemy for the next click.
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
                return;
            }

            if (configUI.Visible && configUI.CurrentEvent != null)
            {
                var npc = new DynamicSpawnEntry();
                npc.NpcID = enemyUI.SelectedNpcType;
                npc.NpcName = tsorcScriptedEvents.GetNpcStableName(enemyUI.SelectedNpcType);
                tsorcRevampSystems.GetPlacementTile(enemyUI.SelectedNpcType, out int placeX, out int placeY);
                npc.SpawnX = placeX;
                npc.SpawnY = placeY;
                configUI.CurrentEvent.Npcs.Add(npc);
                configUI.RefreshList();
                tsorcScriptedEvents.SaveDynamicEvents();

                NPC tempNpc = new NPC();
                tempNpc.SetDefaults(npc.NpcID);
                Main.NewText($"Placed {tempNpc.TypeName} at ({npc.SpawnX}, {npc.SpawnY})");
                enemyUI.SelectedNpcType = 0;
                JustClosedUI = true;
            }
        }
    }
}
