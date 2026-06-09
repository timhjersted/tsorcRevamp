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

            if (player.altFunctionUse == 2) // Right click
            {
                if (enemyUI.SelectedNpcType != 0)
                {
                    enemyUI.SelectedNpcType = 0;
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
                        configUI.RefreshList();
                        tsorcScriptedEvents.SaveDynamicEvents();

                        NPC temp = new NPC();
                        temp.SetDefaults(npcToRemove.NpcID);
                        Main.NewText($"Removed placed {temp.TypeName} from event.");
                        return true;
                    }
                }

                // Otherwise, Right click creates a new event!
                Vector2 mousePos = Main.MouseWorld;
                bool eventTooClose = false;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
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
                    newEvent.MapCondition = "RemixMapCondition";
                }
                else if (tsorcRevampWorld.OnlyAdventureMap)
                {
                    newEvent.MapCondition = "OnlyAdventureMapCondition";
                }
                else
                {
                    newEvent.MapCondition = "";
                }

                tsorcScriptedEvents.DynamicEvents.Add(newEvent);
                tsorcScriptedEvents.SaveDynamicEvents();
                Main.NewText("Created new Dynamic Event Trigger at " + newEvent.CenterX + ", " + newEvent.CenterY);

                // Immediately open it in the configurator
                configUI.SetEvent(newEvent);
                configUI.Show();
            }
            else // Left click
            {
                // Left click: Check if clicking on/near an existing event center to open it
                Vector2 mousePos = Main.MouseWorld;
                DynamicSpawnEvent closestEvent = null;
                float closestDist = float.MaxValue;
                foreach (var ev in tsorcScriptedEvents.DynamicEvents)
                {
                    float dist = Vector2.Distance(mousePos, new Vector2(ev.CenterX * 16 + 8, ev.CenterY * 16 + 8));
                    if (dist < 48 && dist < closestDist)
                    {
                        closestDist = dist;
                        closestEvent = ev;
                    }
                }

                if (closestEvent != null)
                {
                    configUI.SetEvent(closestEvent);
                    configUI.Show();
                    return true;
                }

                // Left click: If selecting an NPC, place it
                if (enemyUI.SelectedNpcType != 0)
                {
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
