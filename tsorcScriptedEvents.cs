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
         * Once in there, it spawns the specified NPCs and watches them. Once all the NPCs associated with an event die, the event removes itself from ActiveEvents.
         * If the player dies and respawns, the world is reloaded, or other similar things then the events are all re-added to InactiveEvents and can be activated again
         * If an event is set to save, however, it will never activate again once the player has finished it.
         * It saves the status of each event in such a way that should make it resistant to corruption due to events being added, changed, or removed.
         * However, since enum names are how it identifies events, if you change one it will reset its save status to 'never run'.
         * 
         * 
         * How to add a scripted NPC event:
         * Go to public enum ScriptedEventType below and add an entry for your event
         * Go to InitializeScriptedEvents, and add your scripted event.
         * Finally, pair your enum and ScriptedEvent up in ScriptedEventDict
         *
         * The format for a ScriptedEvent is the following:
         * ScriptedEvent [YourEventType] = new ScriptedEvent(position, detection radius, [NPC ID = -1], [Dust = 31], [save event: false], [visible detection range: false], [text to display: none], [text color: none], [custom condition: none], [custom scripted action: none], [only run action once: false]);
         * Alternatively, you don't have to spawn an NPC! Events can exist without one and simply run a custom action event function instead.
         * That's a lot! For reference, variables in brackets [like this] are not necessary. If you don't specify them, they will default to whatever's in the box
         * 
         * Here's an explaination for what each variable there means:
         * 1) Position: A "Vector2" variable containing the position (in tiles) to spawn the NPC. Created like 'new Vector2([Position X], [Position Y])'
         * 2) Detection radius, again in tiles.
         * 3) NPC ID: The ID for the NPC in question. For vanilla enemies, you can get this by using 'NPCID.[EnemyName]'. For modded enemies, you can do it like ModContent.NPCType<[Path.To.Enemy.File]>()
         * Note: You don't actually need to spawn an NPC! This field is optional. If you don't want to, like in ExampleNoNPCScriptedEvent, simply put 'default' in the spot where you would put the NPC's ID.
         * 4) What dust to spawn for the event. This controls both what appears around the NPC as it spawns, but also the dust that appears at the edge of its detection range (if that is enabled)
         * 5) Save event: Should this event be permanantly saved once it's completed? If not, it will reappear once the player dies and respawns. Useful for bosses, and will also be used for minibosses once they're in.
         * 6) Visible detection range: Should it show a ring or square of dust outlining the range of the event? Defaults to off. Helpful to highlight optional events, otherwise players wouldn't know anything is there.
         * 7) Text to display: If you want the event to output some flavor text in chat when it runs, you can put that here.
         * 8) Text color: If you have text, what color should it be? You can specify with either new Color(Red, Green, Blue), or 'Color.[ColorName]'.
         * 
         * 9 and 10) The final few paramaters don't actually take variables, they take whole functions.
         * The first is Custom Condition. This lets you specify when an event should happen.
         * Some basic condition functions are provided below: NormalModeCondition, NightCustomCondition, HardModeCondition, etc.
         * Each of these returns true under its stated conditions, and may be enough for now. However, it's easy to add more!
         * Just write a function that returns 'true' when you want the event to occur, and pass it as an argument
         * ExampleCondition exists as an example for how to do this. You can create conditions as complex as you'd like!
         * 
         * The final main parameter is Custom Scripted Action.
         * This allows you to pass a function to be run, much like Custom Condition.
         * The difference is that the Custom Scripted Action function will be run for as long as the event is active
         * You can create a Custom Scripted Action function similarly to a Custom Condition. The difference is that this function also takes a Player and an Int as parameters
         * The player is the player who triggered the event, and the int is the ID for the NPC that spawned 
         * This action function must return a bool. If this event has no NPC associated, that bool tells the event handler whether or not to end the event. False = do not end, true = end it.
         * On the other hand, if an event *does* have an NPC, it will ignore that. Those are connected to the life of their NPC instead, and will end automatically when the NPC dies. 
         * 
         * 
         * Adding custom stats:
         * To customize the stats of a spawned NPC, add this line below your event line
         * '[Event Name].SetCustomStats(Health, Defense, Damage);'
         * An example of this is below ArtoriasEvent, giving the spawned Artorias dramatically weakened stats
         * * Note: The damage stat here can not change the projectile damage for enemies, since the damage of each projectile is hardcoded independent of their true stats. 
         * That value MUST be changed via scripting instead. ArtoriasCustomAction shows an example of this.
         * 
         * Adding custom drops:
         * To add drops to a spawned NPC, add this line below your event line
         * [Event Name].SetCustomDrops(new List<int>() { [ItemID 1], [ItemID 2], [ItemID 3], etc etc etc});
         * For vanilla items, get their ID with 'ItemID.[ItemName]'
         * For modded items, get it with 'ModContent.ItemType<Path.To.That.Specific.Item>()'
         * 
         * Spawning a list of enemies instead of a single one:
         * In this case, simply replace NPC ID with a List of the ID's of the enemies you want to spawn. They do not all have to be the same enemy!
         * Then, follow it up with a list of the coordinates of each enemy in the swarm. The coordinates are passed as Vector2's, and an example is the ExampleHarpySwarm
         * 
         * TODO:
         * Minibosses
         * Potentially make it easier to modify enemy projectile damage. Not sure if that's feasable, though.
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



        //Each scripted event should have a definition here. I added some theoretical examples commented out
        //This name is what the event handler uses to save an event, and marks them as unique.
        public enum ScriptedEventType
        {
            AbysmalOolacileSorcererFight,
            WitchkingFight,
            WyvernMageShadowFight,
            ChaosFight,
            BlightFight,
            DarkCloudPyramidFight,
            ArtoriasFight,
            ExampleBlackKnightFight,
            ExampleHarpySwarm,
            ExampleNoNPCScriptEvent,
            SpawnGoblin

            //AncientDemonAmbush,
            //HellkiteDragonAttack,
            //Frogpocalypse2_TheFroggening,
        }

        //Contains all the info defining each scripted event, and loads it all into the dictionary
        //It also initializes the other dictionary and lists
        public static void InitializeScriptedEvents()
        {
            //ABYSMAL OOLACILE SORCERER
            ScriptedEvent AbysmalOolacileSorcererEvent = new ScriptedEvent(new Vector2(6721, 1905), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(), DustID.MagicMirror, true, true, "The Abysmal Oolacile Sorcerer shall now disembowel you...", Color.Red, false, SuperHardModeCustomCondition);

            //WITCHKING
            ScriptedEvent WitchkingEvent = new ScriptedEvent(new Vector2(2484, 1795), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(), DustID.MagicMirror, true, true, "The Witchking has been waiting for you...", Color.Red, false, SuperHardModeCustomCondition);
            
            //BLIGHT
            ScriptedEvent BlightEvent = new ScriptedEvent(new Vector2(8174, 866), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(), DustID.MagicMirror, true, true, "The Blight surfaces from the ocean!", Color.Blue, false, SuperHardModeCustomCondition);
            //BlightEvent.SetCustomStats(50000, 30, 50);

            //CHAOS
            ScriptedEvent ChaosEvent = new ScriptedEvent(new Vector2(6415, 1888), 20, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(), DustID.MagicMirror, true, true, "Chaos has entered this dimension!", Color.Red, false, SuperHardModeCustomCondition);

            //WYVERN MAGE 
            ScriptedEvent WyvernMageShadowEvent = new ScriptedEvent(new Vector2(6342, 246), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.WyvernMageShadow>(), DustID.MagicMirror, true, true, "The Wyvern Mage has been freed from its cage!", Color.Blue, false, SuperHardModeCustomCondition);
          
            //DARK CLOUD
            ScriptedEvent DarkCloudEvent = new ScriptedEvent(new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, "Your shadow self has manifested from your darkest fears...", Color.Blue, false, SuperHardModeCustomCondition);

            //ARTORIAS
            ScriptedEvent ArtoriasEvent = new ScriptedEvent(new Vector2(5344, 1692), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), DustID.GoldFlame, true, true, "Artorias, the Abysswalker arrives to tear you from this plane...", Color.Gold, false, TheAbyssCustomCondition, ArtoriasCustomAction);
            ArtoriasEvent.SetCustomStats(50000, 30, 50);
            //ArtoriasEvent.SetCustomDrops(new List<int>() { ItemID.RodofDiscord, ModContent.ItemType<Items.DestructionElement>() }, new List<int>() { 1, 4 });

            //BLACK KNIGHT
            ScriptedEvent ExampleBlackKnightFight = new ScriptedEvent(new Vector2(506, 867), 20, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, false, true, "A Black Knight is hunting you...", Color.Purple, true, default, ExampleBlackKnightCustomAction);
            ExampleBlackKnightFight.SetCustomStats(1500, 10, 50);
            ExampleBlackKnightEvent.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.DarkSoul>() }, new List<int>() { 555 });


            List<int> HarpySwarmEnemyTypeList = new List<int>() { NPCID.Harpy, NPCID.Harpy, NPCID.Harpy, NPCID.Harpy, NPCID.Harpy };
            List<Vector2> HarpySwarmEnemyLocations = new List<Vector2>() { new Vector2(525, 837), new Vector2(545, 837), new Vector2(505, 837), new Vector2(525, 817), new Vector2(525, 857) };
            ScriptedEvent ExampleHarpySwarm = new ScriptedEvent(new Vector2(525, 837), 50, HarpySwarmEnemyTypeList, HarpySwarmEnemyLocations, DustID.BlueFairy, false, true, "A Swarm of Harpies appears!", Color.Cyan, false, NormalModeCustomCondition);
            ExampleHarpySwarm.SetCustomStats(50, 5, 30);
            List<int> HarpyDropList = new List<int>() { ModContent.ItemType<Items.DarkSoul>(), ItemID.Feather };
            List<int> HarpyDropCounts = new List<int>() { 50, 10 };
            ExampleHarpySwarm.SetCustomDrops(HarpyDropList, HarpyDropCounts);

            ScriptedEvent ExampleNoNPCScriptEvent = new ScriptedEvent(new Vector2(456, 867), 60, default, DustID.GreenFairy, default, true, "The example scripted event has begun...", Color.Green, false, ExampleCondition, ExampleCustomAction);

            //ScriptedEvent FrogpocalypseEvent = new ScriptedEvent(SuperHardModeCustomCondition, new Vector2(5728, 1460), 120, ModContent.NPCType<NPCs.Enemies.MutantGigatoad>(), DustID.GreenTorch, default, true, "The Abyssal Toad rises to assist in debugging...", Color.Green);

            ScriptedEvent SpawnGoblin = new ScriptedEvent(new Vector2(4456, 1744), 100, null, 31, true, true, "", default, true, TinkererCondition, TinkererAction);

            //Every enum and ScriptedEvent has to get paired up here
            ScriptedEventDict = new Dictionary<ScriptedEventType, ScriptedEvent>(){
                {ScriptedEventType.AbysmalOolacileSorcererFight, AbysmalOolacileSorcererEvent},
                {ScriptedEventType.WitchkingFight, WitchkingEvent},
                {ScriptedEventType.ChaosFight, ChaosEvent},
                {ScriptedEventType.WyvernMageShadowFight, WyvernMageShadowEvent},
                {ScriptedEventType.BlightFight, BlightEvent},
                {ScriptedEventType.DarkCloudPyramidFight, DarkCloudEvent},
                {ScriptedEventType.ArtoriasFight, ArtoriasEvent},
                {ScriptedEventType.ExampleBlackKnightFight, ExampleBlackKnightFight},
                {ScriptedEventType.ExampleHarpySwarm, ExampleHarpySwarm},
                {ScriptedEventType.ExampleNoNPCScriptEvent, ExampleNoNPCScriptEvent},
                //{ScriptedEventType.Frogpocalypse2_TheFroggening, FrogpocalypseEvent}
                {ScriptedEventType.SpawnGoblin, SpawnGoblin }
                
            };

            ScriptedEventValues = new Dictionary<ScriptedEventType, bool>();
            foreach (ScriptedEventType currentEvent in ScriptedEventDict.Keys)
            {
                ScriptedEventValues.Add(currentEvent, false);
            }


            //Add everything to InactiveEvents to start fresh.
            //If the player is NOT loading a fresh world, then this will get wiped later and re-loaded with only the appropriate events.
            InactiveEvents = new List<ScriptedEvent>();
            foreach (KeyValuePair<ScriptedEventType, ScriptedEvent> eventValuePair in ScriptedEventDict)
            {
                InactiveEvents.Add(eventValuePair.Value);                
            }

            ActiveEvents = new List<ScriptedEvent>();
            DisabledEvents = new List<ScriptedEvent>();
        }



        #region customconditions
        //You can make custom conditions like this: Just write a function that takes no arguments and returns a bool
        //When it's time to run the event this function will be executed, and if false the event will not run

        //This condition is an example. If it's day and in the morning, it returns 'true'. If not, it returns false.
        public static bool ExampleCondition()
        {
            if ((Main.dayTime) && (Main.time < 24000))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //This condition returns true if the world is in superhardmode
        public static bool NormalModeCustomCondition()
        {
            return !Main.hardMode;
        }
        public static bool NightCustomCondition()
        {
            return !Main.dayTime;
        }
        //This condition returns true if the world is in superhardmode
        public static bool HardModeCustomCondition()
        {
            return Main.hardMode;
        }
        //This condition returns true if the world is in superhardmode
        public static bool SuperHardModeCustomCondition()
        {
            return tsorcRevampWorld.SuperHardMode;
        }

        //This condition returns true if the player is in The Abyss
        public static bool TheAbyssCustomCondition()
        {
            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool TinkererCondition() {
            return !NPC.AnyNPCs(NPCID.GoblinTinkerer);
        }
        #endregion

        #region customactions


        //You can make custom actions like this, and pass them as arguments to the event!
        static int exampleTimer = 0;
        public static bool ExampleCustomAction(Player player, int npcID)
        {
            Dust.NewDust(player.position, 30, 30, DustID.GreenFairy, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 255);
            if (exampleTimer > 900)
            {
                Main.NewText("The example scripted event ends...", Color.Green);
                return true;
            }
            exampleTimer++;
            return false;
        }


        //This is an example artorias custom action. It spawns meteors and displays text every so often, and also changes the projectile damage for Artorias. Most enemies will require a very small change for their projectile damage changes to work (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static bool ArtoriasCustomAction(Player player, int npcID)
        {
            //Spawning meteors:
            if (Main.rand.Next(200) == 0)
            {
                Main.NewText("Artorias rains fire from the Abyss...", Color.Gold);
                for (int i = 0; i < 10; i++)
                {
                    Projectile.NewProjectile((float)player.position.X - 100 + Main.rand.Next(200), (float)player.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), Main.npc[npcID].damage / 4, 2f, 255);
                }
            }

            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (Main.npc[npcID].type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Bosses.SuperHardMode.Artorias ourArtorias = (NPCs.Bosses.SuperHardMode.Artorias)Main.npc[npcID].modNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, their damage stats might not be public.
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case blackBreathDamage and phantomSeekerDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourArtorias.blackBreathDamage = 40;
                ourArtorias.phantomSeekerDamage = 50;
            }
            return false;
        }

        //This is an example custom action that just changes the damage of an NPC's projectile. Most enemies will require a very small change for this to work with them (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static bool ExampleBlackKnightCustomAction(Player player, int npcID)
        {
            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (Main.npc[npcID].type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Enemies.BlackKnight ourKnight = (NPCs.Enemies.BlackKnight)Main.npc[npcID].modNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, the variable that controls the damage for its projectile might not be public (read: probably isn't).
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case spearDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourKnight.spearDamage = 40;
            }
            return true;
        }

        //i dont want this event to last forever, so just spawn the tinkerer and immediately end the event
        //... is what it SHOULD do?
        public static bool TinkererAction(Player player, int npcID) {
            NPC.NewNPC(4456 * 16, 1744 * 16, NPCID.GoblinTinkerer);
            return true;
        }

        #endregion

        public static void SaveScriptedEvents(TagCompound tag)
        {
            //Converts the keys from enums into strings, because apparently it isn't a huge fan of enums
            List<string> stringList = ScriptedEventValues.Keys.ToList().ConvertAll(enumMember => enumMember.ToString());
            tag.Add("event_types", stringList);
            tag.Add("event_values", ScriptedEventValues.Values.ToList());
        }

        //Called upon mod load, but ONLY if the mod already has a .twld file.
        //Adds all our events to a list, InactiveEvents
        //The advantage of having them in a list instead of a dictionary is that we can skip entries
        public static void LoadScriptedEvents(TagCompound tag)
        {
            if (tag.ContainsKey("event_types"))
            {
                //Converts the keys from strings into enums, then puts both keys and values into ScriptedEventValues
                List<string> eventTypeStrings = tag.Get<List<string>>("event_types");
                List<bool> event_values = tag.Get<List<bool>>("event_values");

                for (int i = 0; i < eventTypeStrings.Count; i++)
                {
                    ScriptedEventType scriptedEventOut;

                    //If it contains a matching event
                    if (Enum.TryParse(eventTypeStrings[i], out scriptedEventOut))
                    {
                        //And doesn't already contain that key (just in case)
                        if (!ScriptedEventValues.ContainsKey(scriptedEventOut))
                        {
                            ScriptedEventValues.Add(scriptedEventOut, event_values[i]);
                        }
                        else
                        {
                            ScriptedEventValues[scriptedEventOut] = event_values[i];
                        }
                    }
                    else
                    {
                        Main.NewText("ERROR: Failed to convert string " + eventTypeStrings[i] + "to enum. Please report this!! You can do so in our discord: https://discord.gg/kSptDbe", Color.Red);
                    }               
                }
            }

            //First, refresh the InactiveEvents list. It is initialized as full, containing every event, just in case the player loads a world without a .twld file.
            InactiveEvents = new List<ScriptedEvent>();

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

        public Func<bool> condition = DefaultCondition;
        public bool scriptedEvent = false;
        public bool useListSpawns = false;
        List<int> NPCIDs;
        List<Vector2> NPCCoordinates;
        List<int> spawnedNPCIDs = new List<int>();
        List<bool> NPCDroppedLoot = new List<bool>();

        bool hasCustomDrops = false;
        List<int> CustomDrops;
        List<int> DropAmounts;

        readonly int npcToSpawn;
        bool runOnce;
        int spawnedNPCID = -1;
        readonly string eventText;
        Color eventTextColor;
        public int dustID;
        //Controls whether an event is saved. If not, it will reappear upon either player death or game load.
        public bool save;
        //Controls whether the event's range is made visible to the player with dust
        public bool visible;


        public bool endEvent = false;

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
        public Func<Player, int, bool> CustomAction = null;
        public bool runActionOnce = false;

        bool healthChange = false;
        public int newMaxLife;
        bool defChange = false;
        public int newDefense;
        bool damageChange = false;
        public int newDamage;


        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, int? npcType = null, int dustType = 31, bool saveEvent = false, bool visibleRange = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<Player, int, bool> customAction = null, bool runCustomActionOnce = false)
        {
            rangeDetectionMode = true;
            //Player position is stored as 16 times block distances
            centerpoint = rangeCenterpoint * 16;
            //Radius is stored squared, because comparing the squares of distances is WAY faster than comparing their true values
            radius = (rangeRadius * 16) * (rangeRadius * 16);

            if (npcType != null)
            {
                npcToSpawn = npcType.GetValueOrDefault();
            }
            else
            {
                scriptedEvent = true;
            }

            eventText = flavorText;
            eventTextColor = flavorTextColor;
            dustID = dustType;
            save = saveEvent;
            visible = visibleRange;
            square = squareRange;

            if (customCondition != null)
            {
                condition = customCondition;
            }

            if (customAction != null)
            {
                hasCustomAction = true;
                CustomAction = customAction;
            }
            runActionOnce = runCustomActionOnce;
        }
        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs, List<Vector2> coords, int dustType = 31, bool saveEvent = false, bool visibleRange = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<Player, int, bool> customAction = null, bool runCustomActionOnce = false)
        {
            rangeDetectionMode = true;
            //Player position is stored as 16 times block distances
            centerpoint = rangeCenterpoint * 16;
            //Radius is stored squared, because comparing the squares of distances is WAY faster than comparing their true values
            radius = (rangeRadius * 16) * (rangeRadius * 16);

            useListSpawns = true;
            NPCCoordinates = coords;
            NPCIDs = npcs;

            eventText = flavorText;
            eventTextColor = flavorTextColor;
            dustID = dustType;
            save = saveEvent;
            visible = visibleRange;
            square = squareRange;

            if (customCondition != null)
            {
                condition = customCondition;
            }

            if (customAction != null)
            {
                hasCustomAction = true;
                CustomAction = customAction;
            }
            runActionOnce = runCustomActionOnce;
        }


        public void SetCustomStats(int? health = null, int? defense = null, int? damage = null)
        {
            if (health != null)
            {
                healthChange = true;
                newMaxLife = health.GetValueOrDefault();
            }
            if (defense != null)
            {
                defChange = true;
                newDefense = defense.GetValueOrDefault();
            }
            if (damage != null)
            {
                damageChange = true;
                newDamage = damage.GetValueOrDefault();
            }
        }

        public void SetCustomDrops(List<int> dropIDs, List<int> dropStackSizes)
        {
            CustomDrops = dropIDs;
            DropAmounts = dropStackSizes;
            hasCustomDrops = true;
        }

        //Runs the event. For the basic one, it just tracks the spawned NPC, and removes itself from the ActiveEvents list once that NPC dies
        //Once I make it so you can pass custom event functions, those will get run here too
        public void RunEvent(Player player)
        {
            if (!runOnce)
            {
                if (!scriptedEvent)
                {
                    SpawnNPC();
                }
                if (eventText != "default")
                {
                    Main.NewText(eventText, eventTextColor);
                }
                if (hasCustomAction)
                {
                    endEvent = CustomAction(player, spawnedNPCID);
                }
                runOnce = true;
            }
            else
            {
                if (hasCustomAction && !runActionOnce)
                {
                    endEvent = CustomAction(player, spawnedNPCID);
                }
            }

            if (!scriptedEvent)
            {
                //If the NPC is dead, remove it from active events
                //If it's a boss, confirmed the player actually killed it by checking Slain (as opposed to dying to it or letting it despawn other ways)
                //If so, and this is marked as "save", then do so by getting the key for this event and marking it as finished in ScriptedEventValues
                //Otherwise add it back to InactiveEvents
                if (!useListSpawns)
                {
                    if (!Main.npc[spawnedNPCID].active)
                    {
                        if (tsorcRevampWorld.Slain.ContainsKey(npcToSpawn) && save)
                        {
                            foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                            {
                                if (pair.Value == this)
                                {
                                    tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                                }
                            }

                        }
                        else
                        {
                            tsorcScriptedEvents.DisabledEvents.Add(this);
                        }

                        if (hasCustomDrops)
                        {
                            for (int i = 0; i < CustomDrops.Count; i++)
                            {
                                Item.NewItem(Main.npc[spawnedNPCID].getRect(), CustomDrops[i], DropAmounts[i]);
                            }
                        }
                        tsorcScriptedEvents.ActiveEvents.Remove(this);
                        runOnce = false;
                    }
                }
                else
                {
                    bool oneAlive = false;
                    for (int i = 0; i < spawnedNPCIDs.Count; i++)
                    {

                        if (Main.npc[spawnedNPCIDs[i]].active)
                        {
                            oneAlive = true;
                        }
                        else
                        {
                            if (tsorcRevampWorld.Slain.ContainsKey(npcToSpawn) && save)
                            {
                                foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                                {
                                    if (pair.Value == this)
                                    {
                                        tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                                    }
                                }

                            }
                            else
                            {
                                tsorcScriptedEvents.DisabledEvents.Add(this);
                            }

                            if (hasCustomDrops && !NPCDroppedLoot[i])
                            {
                                NPCDroppedLoot[i] = true;
                                for (int j = 0; j < CustomDrops.Count; j++)
                                {
                                    Item.NewItem(Main.npc[spawnedNPCIDs[i]].getRect(), CustomDrops[j], DropAmounts[j]);
                                }
                            }
                        }
                    }
                    if (!oneAlive)
                    {

                        tsorcScriptedEvents.ActiveEvents.Remove(this);
                        NPCDroppedLoot = new List<bool>();
                        runOnce = false;
                    }

                }
            }
            else if (endEvent)
            {
                if (save)
                {
                    foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                    {
                        if (pair.Value == this)
                        {
                            tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                        }
                    }
                }
                else
                {
                    tsorcScriptedEvents.DisabledEvents.Add(this);
                }
                tsorcScriptedEvents.ActiveEvents.Remove(this);
            }
        }

        public void SpawnNPC()
        {
            if (useListSpawns)
            {
                for (int i = 0; i < NPCIDs.Count; i++)
                {
                    spawnedNPCIDs.Add(NPC.NewNPC((int)NPCCoordinates[i].X * 16, (int)NPCCoordinates[i].Y * 16, NPCIDs[i]));
                    NPCDroppedLoot.Add(false);
                    if (healthChange)
                    {
                        Main.npc[spawnedNPCIDs[i]].lifeMax = newMaxLife;
                        Main.npc[spawnedNPCIDs[i]].life = newMaxLife;
                    }
                    if (defChange)
                    {
                        Main.npc[spawnedNPCIDs[i]].defense = newDefense;
                    }
                    if (damageChange)
                    {
                        Main.npc[spawnedNPCIDs[i]].damage = newDamage;
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPCID, 0f, 0f, 0f, 0);
                    }
                }
            }
            else
            {
                spawnedNPCID = NPC.NewNPC((int)centerpoint.X, (int)centerpoint.Y, npcToSpawn);
                if (healthChange)
                {
                    Main.npc[spawnedNPCID].lifeMax = newMaxLife;
                    Main.npc[spawnedNPCID].life = newMaxLife;
                }
                if (defChange)
                {
                    Main.npc[spawnedNPCID].defense = newDefense;
                }
                if (damageChange)
                {
                    Main.npc[spawnedNPCID].damage = newDamage;
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPCID, 0f, 0f, 0f, 0);
                }
            }
        }

        public static bool DefaultCondition()
        {
            return true;
        }
    }
}