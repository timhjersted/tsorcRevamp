using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp
{
    public static class tsorcScriptedEvents
    {
        /**
         * Scripted event class! Kinda a long boi so I tried to keep it well documented
         * This can handle all the location-triggered special spawns and events we want to implement
         * It works like this: The parameters of each event (location, enemy, detection radius, etc) are saved in a dictionary in InitializeScriptedEvents.
         * On mod load, that dictionary is loaded into a list: InactiveEvents
         * As the game runs, it checks whether the player entered the activation range for any event.
         * If they have, it is removed from InactiveEvents (meaning it is no longer getting checked) and into ActiveEvents
         * Once in there, it spawns the specified NPC and watches it. Once it dies, the event removes itself from ActiveEvents.
         * It saves the status of each event in such a way that should make it resistant to corruption due to events being added, changed, or removed.
         * 
         * TODO:
         * Add the ability to restrict the event based on the status of the game world, for example only letting certain ones run in SuperHardMode
         * Add a second way to define events: A square bounded by two Vector2 points
         * For single point/radius type events, implement the option to check within a square radius instead of a circular one
         * Implement an option to save if an event has been defeated, and if so not spawn it again (potentially the ability to choose between not spawning it again *ever* vs not doing so until the player dies next)
         * Implement the option to pass custom event functions, allowing for more dynamic things than just "spawn NPC when player gets in range"
         * Using this, we could have many of the events that currently occur in sign dialogue occur in-game instead. Examples are many instances of NPC dialogue or boss spawns.
         * 
         * Another idea: Add the option to spawn particles around the edge of/within the detection range, so that players know it's there and can willingly trigger it (ex, for boss fights so they don't trigger it by accident)
         * 
         * **/

        //This is a dictionary that will store all the info for each of our events to keep them nice and neat!
        public static Dictionary<ScriptedEventType, ScriptedEvent> ScriptedEventDict;
        //This is a dictionary that will store whether or not each event has run its course and should no longer be activated
        //The contents of this dictionary are saved and loaded across sessions
        public static Dictionary<ScriptedEventType, bool> ScriptedEventValues;
        //Stores the events that have not been triggered by the player. It will check if the player is within any of these
        public static List<ScriptedEvent> InactiveEvents;
        //Stores the events that have been triggered by the player and are currently active. It will run the RunEvent() code for each of these as long as they remain active.
        public static List<ScriptedEvent> ActiveEvents;
        //Stores events that the player has triggered and are no longer active. Upon player death, these will be restored to InactiveEvents.
        public static List<ScriptedEvent> DisabledEvents;


       

        //Each scripted event should have a definition here. There's only one now, so I added some theoretical examples commented out
        public enum ScriptedEventType
        {
            DarkCloudPyramidFight,
            ArtoriasFight
            //AncientDemonAmbush,
            //HellkiteDragonAttack,
            //Frogpocalypse2_TheFroggening,
        }

        //Contains all the info defining each scripted event, and loads it all into the dictionary
        //It also initializes the other dictionary and lists
        private static void InitializeScriptedEvents()
        {
            ScriptedEvent DarkCloudEvent = new ScriptedEvent(SuperHardModeCustomCondition, new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, "Your shadow self has manifested from your darkest fears...", Color.Blue, false);
            ScriptedEvent ArtoriasEvent = new ScriptedEvent(ArtoriasCustomCondition, new Vector2(5344, 1692), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), DustID.GoldFlame, false, true, "Artorias, the Abysswalker arrives to tear you from this plane...", Color.Blue, false, ArtoriasCustomAction, false);
            //ScriptedEvent FrogpocalypseEvent = new ScriptedEvent(SuperHardModeCustomCondition, new Vector2(5728, 1460), 120, ModContent.NPCType<NPCs.Enemies.MutantGigatoad>(), DustID.GreenTorch, default, true, "The Abyssal Toad rises to assist in debugging...", Color.Green);


            ScriptedEventDict = new Dictionary<ScriptedEventType, ScriptedEvent>(){
                {ScriptedEventType.DarkCloudPyramidFight, DarkCloudEvent},
                {ScriptedEventType.ArtoriasFight, ArtoriasEvent}
                //Example 2: {ScriptedEventType.Frogpocalypse2_TheFroggening, FrogpocalypseEvent}
            };

            ScriptedEventValues = new Dictionary<ScriptedEventType, bool>();
            InactiveEvents = new List<ScriptedEvent>();
            ActiveEvents = new List<ScriptedEvent>();
            DisabledEvents = new List<ScriptedEvent>();
        }

        #region customconditions
        //You can make custom conditions like this: Just write a function that takes no arguments and returns a bool
        //When it's time to run the event this function will be executed, and if false the event will not run
        public static bool SuperHardModeCustomCondition()
        {            
            return tsorcRevampWorld.SuperHardMode;            
        }

        public static bool ArtoriasCustomCondition()
        {
            if(tsorcRevampWorld.SuperHardMode && Main.bloodMoon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region customactions
        //You can make custom actions like this, and pass them as arguments to the event!
        public static void ExampleCustomAction(Player player, int npcID)
        {
            Main.NewText("Custom!!");
        }

        public static void ArtoriasCustomAction(Player player, int npcID)
        {
            if (Main.rand.Next(200) == 0)
            {
                Main.NewText("Artorias rains fire from the sky...", Color.Gold);
                for (int i = 0; i < 10; i++)
                {
                    Projectile.NewProjectile((float)player.position.X - 100 + Main.rand.Next(200), (float)player.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), Main.npc[npcID].damage / 4, 2f, 255);
                }
            }
        }
        #endregion

        public static void SaveScriptedEvents(TagCompound tag)
        {
            //Converts the keys from enums into strings, because apparently it isn't a huge fan of enums
            List<string> stringList = ScriptedEventValues.Keys.ToList().ConvertAll(enumMember => enumMember.ToString());
            tag.Add("event_types", stringList);
            tag.Add("event_values", ScriptedEventValues.Values.ToList());
        }

        //Called upon mod load, adds all our events to a list
        //The advantage of having them in a list instead of a dictionary is that we can skip entries
        //If we have enough of these that checking them risks becoming a performance issue, we could spread the checks out over multiple ticks instead of having them all run every single tick
        //The simplest example: Check the even #'d events on even ticks, check the odd ones on odd ticks.
        //We could spread the checks out over a full second or longer if we wanted to though, reducing the performance hit to 1/60th what it otherwise would be.
        public static void LoadScriptedEvents(TagCompound tag)
        {
            InitializeScriptedEvents();

            if (tag.ContainsKey("event_types"))
            {
                //Converts the keys from strings into enums, then puts both keys and values into ScriptedEventValues
                List<ScriptedEventType> event_types = tag.Get<List<string>>("event_types").ConvertAll(stringType => (ScriptedEventType)Enum.Parse(typeof(ScriptedEventType), stringType));
                List<bool> event_values = tag.Get<List<bool>>("event_values");
                for (int i = 0; i < event_types.Count; i++)
                {
                    ScriptedEventValues.Add(event_types[i], event_values[i]);
                }
            }

            //Once that's done, parse though the main dictionary of events.
            //First check if there's an entry in ScriptedEventValues for each entry. If not, add one and set it to false.
            //This means there's no need to worry if the tag didn't contain the key: In that case it will just create every entry from scratch and define them as false
            //Second, add every scripted event that has its value set to false to InactiveEvents
            foreach (KeyValuePair<ScriptedEventType, ScriptedEvent> eventValuePair in ScriptedEventDict)
            {
                if (!ScriptedEventValues.ContainsKey(eventValuePair.Key))
                {
                    ScriptedEventValues.Add(eventValuePair.Key, false);
                }
                if (!ScriptedEventValues[eventValuePair.Key])
                {
                    InactiveEvents.Add(eventValuePair.Value);
                }
            }
        }

        //Experimenting with spreading the checks out over a long period so each one isn't running every tick
        //Counts up each time PlayerScriptedEventCheck is called (aka every tick)
        //int tick = 0;
        //How many ticks (plus one) should the checks be spread out over?
        //int tickSpread = 20;
        public static void PlayerScriptedEventCheck(Player player)
        {
            //Check if the player is in range of any inactive events
            for (int i = 0; i < InactiveEvents.Count; i++)
            {
                if (InactiveEvents[i].condition())
                {
                    float distance = Vector2.DistanceSquared(player.position, InactiveEvents[i].centerpoint);
                    int dustPerTick = 20;
                    float speed = 2;
                    if (!InactiveEvents[i].square)
                    {
                        //If the player is nearby, display some dust to make the region visible to them
                        //This has a Math.Sqrt in it, but that's fine because this code only runs for the handful-at-most events that will be onscreen at a time
                        if (InactiveEvents[i].visible && distance < 6000000)
                        {
                            float sqrtRadius = (float)Math.Sqrt(InactiveEvents[i].radius);
                            for (int j = 0; j < dustPerTick; j++)
                            {
                                Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
                                Vector2 dustPos = InactiveEvents[i].centerpoint + dir * sqrtRadius;
                                if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                {
                                    Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 2) * speed;
                                    Dust dustID = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                    dustID.noGravity = true;
                                }
                            }                            
                        }
                        if (distance < InactiveEvents[i].radius)
                        {
                           
                            for (int j = 0; j < 100; j++)
                            {
                                Dust.NewDustPerfect(InactiveEvents[i].centerpoint, InactiveEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                            }
                            ActiveEvents.Add(InactiveEvents[i]);
                            InactiveEvents.RemoveAt(i);
                        }
                        
                    }
                    //Do the same thing, but square
                    else
                    {
                        float sqrtRadius = (float)Math.Sqrt(InactiveEvents[i].radius);
                        if (InactiveEvents[i].visible && distance < 6000000)
                        {
                            Vector2 dustPos;
                            Vector2 dustVel;
                            Dust dustID;
                            for (int j = 0; j < dustPerTick; j++)
                            {
                                int side = Main.rand.Next(0, 4);
                                if (side == 0)
                                {
                                    dustPos = new Vector2(InactiveEvents[i].centerpoint.X + sqrtRadius, InactiveEvents[i].centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                                    if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                    {
                                        dustVel = new Vector2(0, speed);
                                        dustID = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                        dustID.noGravity = true;
                                    }
                                }
                                if (side == 1)
                                {
                                    dustPos = new Vector2(InactiveEvents[i].centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), InactiveEvents[i].centerpoint.Y + sqrtRadius);
                                    if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                    {
                                        dustVel = new Vector2(-speed, 0);
                                        dustID = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                        dustID.noGravity = true;
                                    }
                                }
                                if (side == 2)
                                {
                                    dustPos = new Vector2(InactiveEvents[i].centerpoint.X - sqrtRadius, InactiveEvents[i].centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                                    if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                    {
                                        dustVel = new Vector2(0, -speed);
                                        dustID = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                        dustID.noGravity = true;
                                    }
                                }
                                if (side == 3)
                                {
                                    dustPos = new Vector2(InactiveEvents[i].centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), InactiveEvents[i].centerpoint.Y - sqrtRadius);
                                    if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                    {
                                        dustVel = new Vector2(speed, 0);
                                        dustID = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                        dustID.noGravity = true;
                                    }
                                }

                            }                            
                        }


                        if ((Math.Abs(player.position.X - InactiveEvents[i].centerpoint.X) < sqrtRadius) && (Math.Abs(player.position.Y - InactiveEvents[i].centerpoint.Y) < sqrtRadius))
                        {
                            for (int j = 0; j < 100; j++)
                            {
                                Dust.NewDustPerfect(InactiveEvents[i].centerpoint, InactiveEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                            }
                            ActiveEvents.Add(InactiveEvents[i]);
                            InactiveEvents.RemoveAt(i);
                        }
                    }
                }
            }

            //Run any active events
            for (int i = 0; i < ActiveEvents.Count; i++)
            {
               ActiveEvents[i].RunEvent(player);
            }
        }

        public static void RefreshEvents()
        {
            for (int i = 0; i < DisabledEvents.Count; i++)
            {
                InactiveEvents.Add(DisabledEvents[i]);
                DisabledEvents.RemoveAt(i);
            }
        }
    }

    //Class to keep each scripted event encapsulated
    public class ScriptedEvent
    {
        //Condition controls when the event an occur. If it's false, the event will not run.
        //For example, if you only want an event to run in superhardmode, you'd pass tsorcRevampMain.SuperHardMode as condition
        //If you only wanted it to occur between certain times, you would pass (Main.time > 0700 && Main.time < 1800), for example.

        public Func<bool> condition;
        readonly int npcToSpawn;
        bool spawnedNPC;
        int spawnedNPCID;
        readonly string eventText;
        Color eventTextColor;
        public int dustID;
        //Controls whether an event is saved. If not, it will reappear upon either player death or game load.
        public bool save;
        //Controls whether the event's range is made visible to the player with dust
        public bool visible;

        //ScriptedEvents have two modes: Checking if the player is within range of one specific point, or if they're in a region between two points
        //Is it in the first mode?
        public bool rangeDetectionMode;
        //What is the centerpoint of the region?
        public Vector2 centerpoint;
        //What is the radius in blocks it should check around that centerpoint?
        public float radius;
        //Is it checking if they're in a square range around a point, or a circular one?
        public bool square;

        public bool hasCustomAction = false;
        Action<Player, int> CustomAction = null;
        public bool runActionOnce = false;

        //
        public ScriptedEvent(Func<bool> customCondition, Vector2 rangeCenterpoint, float rangeRadius, int npcType, int dustType, bool saveEvent = false, bool visibleRange = true, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Action<Player, int> customAction = null, bool runCustomActionOnce = false)
        {
            condition = customCondition;
            rangeDetectionMode = true;
            square = squareRange;
            //Player position is stored as 16 times block distances
            centerpoint = rangeCenterpoint * 16;
            //Radius is stored squared, because comparing the squares of distances is WAY faster than comparing their true values
            radius = (rangeRadius * 16) * (rangeRadius * 16);
            npcToSpawn = npcType;
            eventText = flavorText;
            eventTextColor = flavorTextColor;
            dustID = dustType;
            visible = visibleRange;
            save = saveEvent;

            if(customAction != null)
            {
                hasCustomAction = true;
                CustomAction = customAction;
            }
            runActionOnce = runCustomActionOnce;
        }

        //Runs the event. For the basic one, it just tracks the spawned NPC, and removes itself from the ActiveEvents list once that NPC dies
        //Once I make it so you can pass custom event functions, those will get run here too
        public void RunEvent(Player player) {
            if (!spawnedNPC)
            {
                SpawnNPC();
                spawnedNPC = true;
                if (eventText != "default")
                {
                    Main.NewText(eventText, eventTextColor);
                }
            }

            if (hasCustomAction)
            {
                CustomAction(player, spawnedNPCID);
                if (runActionOnce)
                {
                    hasCustomAction = false;
                }
            }

            //If the NPC is dead, remove it from active events
            //If it's a boss, confirmed the player actually killed it by checking Slain (as opposed to dying to it or letting it despawn other ways)
            //If so, and this is marked as "save", then do so by getting the key for this event and marking it as finished in ScriptedEventValues
            //Otherwise add it back to InactiveEvents
            if (!Main.npc[spawnedNPCID].active)
            {
                if (tsorcRevampWorld.Slain.ContainsKey(npcToSpawn) && save){
                    foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                    {
                        if (pair.Value == this)
                        {
                            tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                        }
                    }
                    
                } else
                {
                    tsorcScriptedEvents.DisabledEvents.Add(this);
                }
                tsorcScriptedEvents.ActiveEvents.Remove(this);
                spawnedNPC = false;
            }
        }

        public void SpawnNPC()
        {
            spawnedNPCID = NPC.NewNPC((int)centerpoint.X, (int)centerpoint.Y, npcToSpawn);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPCID, 0f, 0f, 0f, 0);
            }
        }
    }

    public class Ref<T>
    {
        public Ref(T value)
        {
            Value = value;
        }
        public T Value { get; set; }

        public static implicit operator T(Ref<T> reference) => reference.Value;    
    }
}