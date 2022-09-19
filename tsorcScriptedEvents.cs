using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Config;

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

        //For multiplayer. The server sends clients a list of events, which it stores here. They are not run client-side, and exist only so dust can be drawn indicating their presence.
        //Necessary because event conditions are dynamic. There's no way for clients to know if events have ended or not unless they run them as well, which would result in duplication.
        public static List<NetworkEvent> NetworkEvents;

        //If a boss is alive, events are placed in a queue instead of re-enabled when players respawn. They are re-enabled once the boss dies or despawns. This is to prevent events, including events to *spawn* that very boss, from being re-enabled mid-fight.
        public static List<ScriptedEvent> QueuedEvents;

        //Each scripted event should have a definition here. I added some theoretical examples commented out
        //This name is what the event handler uses to save an event, and marks them as unique.
        public enum ScriptedEventType
        {
            AlienAmbush,
            EoC,
            EoW1,
            AncientDemon,
            LitchKing,
            TheHunter,
            TheRage,
            AODE,
            GoblinWizardWMF,
            GoblinWizardClouds,
            Golem2,
            IceGolemEvent,
            KingSlimeEvent,
            HeroofLumeliaFight,
            FireLurkerPain,
            RedKnightPain,
            RedKnightTwinMountain,
            JungleWyvernFight,
            SeathFight,
            WyvernMageFight,
            SlograAndGaibonFight,
            SerrisFight,
            MarilithFight,
            KrakenFight,
            GwynFight,
            AbysmalOolacileSorcererFight,
            WitchkingFight,
            WyvernMageShadowFight,
            ChaosFight,
            BlightFight,
            DarkCloudPyramidFight,
            ArtoriasFight,
            BlackKnightCity,
            //ExampleHarpySwarm,
            //ExampleNoNPCScriptEvent,
            SpawnUndeadMerchant,
            SpawnGoblin,
            AttraidiesTheSorrowEvent,
            TwinEoWFight,
            DunledingAmbush,
            BoulderfallEvent1,
            BoulderfallEvent2,
            BoulderfallEvent3,
            FirebombHollowAmbush,
            LeonhardPhase1Event,
            HollowAmbush1,
            GoblinAmbush1,
            ShadowMageAmbush1,
            BridgeAmbush1,
            LothricAmbush1,
            LothricAmbush2,
            SpawnMechanic,
            SpawnWizard,
            HellkiteDragonEvent,
            EoL

            //AncientDemonAmbush,
            //HellkiteDragonAttack
            //Frogpocalypse2_TheFroggening,
        }

        //Contains all the info defining each scripted event, and loads it all into the dictionary
        //It also initializes the other dictionary and lists
        public static void InitializeScriptedEvents()
        {
            Player player = Main.LocalPlayer;

            //ScriptedEvent[YourEventType] = new ScriptedEvent(position, detection radius, [NPC ID = -1], [Dust = 31], [save event: false], [visible detection range: false], [text to display: none], [text color: none], [custom condition: none], [custom scripted action: none], [only run action once: false]);

            
            //EoC
            ScriptedEvent EoCEvent = new ScriptedEvent(new Vector2(3900, 1138), 20, NPCID.EyeofCthulhu, DustID.MagicMirror, true, true, "The Eye sees you!", Color.Blue, false, null, SetNightCustomAction, peaceCandleEffect: true);

            //EoW1
            ScriptedEvent EoW1Event = new ScriptedEvent(new Vector2(3633, 996), 46, NPCID.EaterofWorldsHead, DustID.Shadowflame, false, true, "The Eater of Worlds is ready to feed!", Color.Purple, false, PreEoWCustomCondition, peaceCandleEffect: true);

            //EMPRESS OF LIGHT
            ScriptedEvent EoL = new ScriptedEvent(new Vector2(4484, 355), 100, NPCID.HallowBoss, DustID.RainbowTorch, false, true, "The Empress of Light awakens!", Main.DiscoColor, false, EoLDownedCondition, peaceCandleEffect: true);

            //LITCH KING
            ScriptedEvent LitchKing = new ScriptedEvent(new Vector2(364, 1897), 40, ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>(), DustID.GoldFlame, true, true, "The Lich King awakens!", Color.Gold, false, peaceCandleEffect: true);

            //THE HUNTER
            ScriptedEvent TheHunter = new ScriptedEvent(new Vector2(296, 1560), 36, ModContent.NPCType<NPCs.Bosses.TheHunter>(), DustID.GoldFlame, true, true, "The hunt begins...", Color.DarkGreen, false, peaceCandleEffect: true);

            //THE RAGE
            ScriptedEvent TheRage = new ScriptedEvent(new Vector2(7000, 1845), 30, ModContent.NPCType<NPCs.Bosses.TheRage>(), DustID.GoldFlame, true, true, "The Rage awakens!", Color.Red, false, peaceCandleEffect: true);

            //ANCIENT DEMON (FORGOTTEN CITY, CLOSE TO FIRE TEMPLE)
            ScriptedEvent AncientDemon = new ScriptedEvent(new Vector2(5317, 1800), 25, ModContent.NPCType<NPCs.Bosses.AncientDemon>(), DustID.GoldFlame, true, true, "What did you expect to find here human?... Your hubris will be your undoing...", Color.MediumPurple, false, peaceCandleEffect: true);
            AncientDemon.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<Items.DarkSoul>() }, new List<int>() { 1, 5000 });

            //ANCIENT OOLACILE DEMON (EARLY-GAME)
            ScriptedEvent AODE = new ScriptedEvent(new Vector2(5652, 971), 27, ModContent.NPCType<NPCs.Bosses.AncientOolacileDemon>(), DustID.GoldFlame, true, true, "You foolish human... pitiful arrogance...", Color.MediumPurple, false, peaceCandleEffect: true);
            AODE.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<Items.DarkSoul>() }, new List<int>() { 1, 1500 });
            
            //GOBLIN SUMMONER IN WMF
            ScriptedEvent GoblinWizardWMF = new ScriptedEvent(new Vector2(7153, 411), 20, NPCID.GoblinSummoner, DustID.MagicMirror, true, true, "You're arrogant, Red. You were a fool to come here...", Color.MediumPurple, false);
            GoblinWizardWMF.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<Items.DarkSoul>() }, new List<int>() { 1, 1500 });

            //GOBLIN SUMMONER IN THE CLOUDS (WMF)
            ScriptedEvent GoblinWizardClouds = new ScriptedEvent(new Vector2(7822, 118), 40, NPCID.GoblinSummoner, DustID.MagicMirror, true, false, "You think you're clever, Red?", Color.MediumPurple, false);
            GoblinWizardClouds.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<Items.DarkSoul>() }, new List<int>() { 1, 1500 });

            //ICE GOLEM WYVERN COMBO
            List<int> Golem2EnemyTypeList = new List<int>() { NPCID.WyvernHead, NPCID.IceGolem };
            List<Vector2> Golem2EnemyLocations = new List<Vector2>() { new Vector2(7776, 829), new Vector2(7800, 868) };
            ScriptedEvent Golem2 = new ScriptedEvent(new Vector2(7900, 868), 30, Golem2EnemyTypeList, Golem2EnemyLocations, DustID.Snow, true, false, "!!!", Color.BlueViolet, false, null, StormCustomAction); //

            //ICE GOLEM - FROZEN OCEAN
            ScriptedEvent IceGolemEvent = new ScriptedEvent(new Vector2(7651, 1020), 20, NPCID.IceGolem, DustID.MagicMirror, true, true, "!", Color.Blue, false);

            //KING SLIME
            ScriptedEvent KingSlimeEvent = new ScriptedEvent(new Vector2(5995, 1117), 20, NPCID.KingSlime, DustID.MagicMirror, true, true, "King Slime appears!", Color.Blue, false, peaceCandleEffect: true);

            //HERO OF LUMELIA FIGHT
            ScriptedEvent HeroofLumeliaFight = new ScriptedEvent(new Vector2(4413, 717), 120, ModContent.NPCType<NPCs.Bosses.HeroofLumelia>(), DustID.OrangeTorch, true, true, "'You killed my brother, Red! ... You've unleashed hell upon this world!' A hero from Lumelia has come seeking justice... ", Color.LightGoldenrodYellow, false, LumeliaCustomCondition, peaceCandleEffect: true);
            //HeroofLumeliaFight.SetCustomStats(1600, 12, 52, 1555);
            //HeroofLumeliaFight.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion }, new List<int>() { 2, 2 });

            //FIRE LURKER PATH OF PAIN
            ScriptedEvent FireLurkerPain = new ScriptedEvent(new Vector2(3245, 1252), 9, ModContent.NPCType<NPCs.Enemies.FireLurker>(), DustID.CursedTorch, true, true, "A cursed Fire Lurker appears...", Color.Purple, false, default, FireLurkerPainCustomAction, peaceCandleEffect: true);
            FireLurkerPain.SetCustomStats(1900, 12, 85, 1755);
            FireLurkerPain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion }, new List<int>() { 2, 2 });

            //RED KNIGHT IN PATH OF PAIN
            ScriptedEvent RedKnightPain = new ScriptedEvent(new Vector2(3897, 1219), 20, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, "A Red Knight appears...", Color.Purple, false, default, RedKnightPainCustomAction, peaceCandleEffect: true);
            RedKnightPain.SetCustomStats(2700, 10, 75, 3255);

            //RED KNIGHT IN TWIN PEAKS MOUNTAIN
            ScriptedEvent RedKnightTwinMountain = new ScriptedEvent(new Vector2(3287, 495), 10, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, "A Red Knight appears...", Color.Purple, false, default, RedKnightMountainCustomAction, peaceCandleEffect: true);
            RedKnightTwinMountain.SetCustomStats(1600, 10, 65, 2055);

            //JUNGLE WYVERN
            ScriptedEvent JungleWyvernEvent = new ScriptedEvent(new Vector2(4331, 1713), 16, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>(), DustID.CursedTorch, true, true, "You have disturbed the Ancient Wyvern of the Forgotten City!", Color.Green, false, peaceCandleEffect: true);

            //SEATH THE SCALELESS
            ScriptedEvent SeathEvent = new ScriptedEvent(new Vector2(7737, 1546), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>(), DustID.FireworkFountain_Blue, true, true, "Seath the Scaleless rises!", Color.Blue, false, peaceCandleEffect: true);

            //WYVERN MAGE 
            ScriptedEvent WyvernMageEvent = new ScriptedEvent(new Vector2(7192, 364), 40, ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>(), DustID.MagicMirror, true, true, "You impress me Red! But this is where your journey ends...", Color.LightCyan, false, null, StormCustomAction, peaceCandleEffect: true);

            //SLOGRA and GAIBON
            //This one works a little different from the others, because it's an event with two bosses that spawns them in an action instead of normally
            //As such, it doesn't "save". Instead, it simply has a custom condition that returns "false" if the boss has truly been beaten. Without this, it would save after just running once...
            ScriptedEvent SlograAndGaibonEvent = new ScriptedEvent(new Vector2(6192, 1267), 30, default, DustID.Shadowflame, false, true, "Slogra and Gaibon have risen from the depths!", Color.Purple, false, SlograGaibonCondition, SlograAndGaibonCustomAction, peaceCandleEffect: true);

            //SERRIS
            //Like Slogra and Gaibon, this one works a little different due to spawning two bosses.
            ScriptedEvent SerrisEvent = new ScriptedEvent(new Vector2(1136, 956), 30, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>(), DustID.FireworkFountain_Blue, false, true, "The Twin Serris Worms have been enraged!", Color.Blue, false, SerrisCustomCondition, SerrisCustomAction, peaceCandleEffect: true);

            //MARILITH 
            ScriptedEvent MarilithEvent = new ScriptedEvent(new Vector2(3235, 1770), 100, ModContent.NPCType<NPCs.Bosses.Fiends.MarilithIntro>(), DustID.RedTorch, false, true, "default", Color.Red, false, MarilithCustomCondition, peaceCandleEffect: true);

            //KRAKEN
            ScriptedEvent KrakenEvent = new ScriptedEvent(new Vector2(1821, 1702), 30, ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>(), DustID.MagicMirror, true, true, "The Water Fiend rises!", Color.Blue, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //GWYN
            ScriptedEvent GwynEvent = new ScriptedEvent(new Vector2(832, 1244), 16, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>(), DustID.OrangeTorch, true, true, "Gwyn has awoken to bring your journey to its final end...", Color.Red, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //ABYSMAL OOLACILE SORCERER
            ScriptedEvent AbysmalOolacileSorcererEvent = new ScriptedEvent(new Vector2(6721, 1905), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(), DustID.Shadowflame, true, true, "The Abysmal Oolacile Sorcerer shall now disembowel you...", Color.Red, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //WITCHKING
            ScriptedEvent WitchkingEvent = new ScriptedEvent(new Vector2(2484, 1795), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(), DustID.OrangeTorch, true, true, "The Witchking has been waiting for you...", Color.Red, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //BLIGHT
            ScriptedEvent BlightEvent = new ScriptedEvent(new Vector2(8174, 866), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(), DustID.MagicMirror, true, true, "The Blight surfaces from the ocean!", Color.Blue, false, SuperHardModeCustomCondition, peaceCandleEffect: true);
            //BlightEvent.SetCustomStats(50000, 30, 50);

            //CHAOS
            ScriptedEvent ChaosEvent = new ScriptedEvent(new Vector2(6415, 1888), 50, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(), DustID.GoldFlame, true, true, "Chaos has entered this dimension!", Color.Red, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //WYVERN MAGE SHADOW-SHM
            ScriptedEvent WyvernMageShadowEvent = new ScriptedEvent(new Vector2(6432, 196), 20, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), DustID.MagicMirror, true, true, "The Wyvern Mage has been freed from its cage!", Color.LightBlue, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //DARK CLOUD
            ScriptedEvent DarkCloudEvent = new ScriptedEvent(new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, "Your shadow self has manifested from your darkest fears...", Color.LightCyan, false, SuperHardModeCustomCondition, peaceCandleEffect: true);

            //ARTORIAS
            ScriptedEvent ArtoriasEvent = new ScriptedEvent(new Vector2(5344, 1692), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), DustID.GoldFlame, true, true, "Artorias, the Abysswalker arrives to tear you from this plane...", Color.Gold, false, SuperHardModeCustomCondition, peaceCandleEffect: true);
            //ArtoriasEvent.SetCustomDrops(new List<int>() { ItemID.RodofDiscord, ModContent.ItemType<Items.DestructionElement>() }, new List<int>() { 1, 4 });

            //BLACK KNIGHT IN FORGOTTEN CITY
            ScriptedEvent BlackKnightCity = new ScriptedEvent(new Vector2(4508, 1745), 20, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, true, "A Black Knight appears from the shadows...", Color.Purple, true, default, BlackKnightCustomAction);
            BlackKnightCity.SetCustomStats(1750, 10, 60, 1555);

            //ATTRAIDIES THE SORROW EVENT
            ScriptedEvent AttraidiesTheSorrowEvent = new ScriptedEvent(new Vector2(8216.5f, 1630), 30, ModContent.NPCType<NPCs.Special.AttraidiesApparition>(), DustID.ShadowbeamStaff, false, true, "[c/D3D3D3:Attraidies:] \"See if you can handle this.\"", Color.OrangeRed, false, AttraidiesTheSorrowCondition, peaceCandleEffect: true);

            //TWIN EATER OF WORLDS FIGHT
            ScriptedEvent TwinEoWFight = new ScriptedEvent(new Vector2(3245, 1220), 30, default, DustID.ShadowbeamStaff, true, true, "Twin Eaters surface from the depths!", Color.Purple, false, EoWCustomCondition, TwinEoWAction, peaceCandleEffect: true);

            //DUNLEDING AMBUSH
            List<int> DunledingAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>() };
            List<Vector2> DunledingAmbushEnemyLocations = new List<Vector2>() { new Vector2(4697, 858), new Vector2(4645, 858), new Vector2(4645, 841) };
            ScriptedEvent DunledingAmbush = new ScriptedEvent(new Vector2(4666, 856), 10, DunledingAmbushEnemyTypeList, DunledingAmbushEnemyLocations, default, true, false, "Ambush!", Color.Red, false, PreEoCCustomCondition, DundledingAmbushAction);
            if (Main.netMode == NetmodeID.SinglePlayer && Main.expertMode)
            {
                DunledingAmbush.SetCustomStats((int?)(player.statLifeMax2 * .5f), null, (int?)(player.statLifeMax2 * 0.10f) + 25); //damage doesn't double for Expert
            }
            DunledingAmbush.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.DodgerollMemo>() }, new List<int>() { 1 }, true);


            //ALIEN AMBUSH
            List<int> AlienAmbushEnemyTypeList = new List<int>() { NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet };
            List<Vector2> AlienAmbushEnemyLocations = new List<Vector2>() { new Vector2(6069, 69), new Vector2(6010, 79), new Vector2(6010, 79), new Vector2(6079, 79), new Vector2(6041, 69), new Vector2(6079, 79) };
            ScriptedEvent AlienAmbush = new ScriptedEvent(new Vector2(6041, 79), 60, AlienAmbushEnemyTypeList, AlienAmbushEnemyLocations, default, true, false, "Alien life form detected", Color.Red, false, PreMechCustomCondition, AlienAmbushAction);



            //HARPY SWARM
            //List<int> HarpySwarmEnemyTypeList = new List<int>() { NPCID.Harpy, NPCID.Harpy, NPCID.Harpy, NPCID.Harpy, NPCID.Harpy };
            //List<Vector2> HarpySwarmEnemyLocations = new List<Vector2>() { new Vector2(525, 837), new Vector2(545, 837), new Vector2(505, 837), new Vector2(525, 817), new Vector2(525, 857) };
            //ScriptedEvent ExampleHarpySwarm = new ScriptedEvent(new Vector2(525, 837), 50, HarpySwarmEnemyTypeList, HarpySwarmEnemyLocations, DustID.BlueFairy, false, true, "A Swarm of Harpies appears!", Color.Cyan);
            //ExampleHarpySwarm.SetCustomStats(50, 5, 30);
            //List<int> HarpyDropList = new List<int>() { ModContent.ItemType<Items.DarkSoul>(), ItemID.Feather };
            //List<int> HarpyDropCounts = new List<int>() { 50, 10 };
            //ExampleHarpySwarm.SetCustomDrops(HarpyDropList, HarpyDropCounts);

            //EXAMPLE NO NPC SCRIPTED EVENT
            //ScriptedEvent ExampleNoNPCScriptEvent = new ScriptedEvent(new Vector2(456, 867), 60, default, DustID.GreenFairy, default, true, "The example scripted event has begun...", Color.Green, false, ExampleCondition, ExampleCustomAction);

            //ScriptedEvent FrogpocalypseEvent = new ScriptedEvent(SuperHardModeCustomCondition, new Vector2(5728, 1460), 120, ModContent.NPCType<NPCs.Enemies.MutantGigatoad>(), DustID.GreenTorch, default, true, "The Abyssal Toad rises to assist in debugging...", Color.Green);

            //UNDEAD MERCHANT SPAWN EVENT 
            ScriptedEvent SpawnUndeadMerchant = new ScriptedEvent(new Vector2(1686, 963), 50, default, 31, false, false, "", default, false, UndeadMerchantCondition, UndeadMerchantAction);

            //GOBLIN TINKERER  SPAWN EVENT
            ScriptedEvent SpawnGoblin = new ScriptedEvent(new Vector2(4456, 1744), 100, default, 31, true, true, "", default, false, TinkererCondition, TinkererAction);

            //MECHANIC SPAWN EVENT
            ScriptedEvent SpawnMechanic = new ScriptedEvent(new Vector2(294, 1366), 100, default, 31, true, true, "", default, false, MechanicCondition, MechanicAction);

            //WIZARD SPAWN EVENT
            ScriptedEvent SpawnWizard = new ScriptedEvent(new Vector2(7322, 603), 40, default, 31, true, true, "", default, true, WizardCondition, WizardAction);

            //BOULDERFALL EVENT 1 - EARTH TEMPLE ENTRANCE
            ScriptedEvent BoulderfallEvent1 = new ScriptedEvent(new Vector2(4378, 922), 6, default, default, true, false, "", default, false, default, BoulderfallEvent1Action);

            //BOULDERFALL EVENT 2 - BLUE DUNGEON BRICK PARKOUR ROOM IN MOUNTAIN
            ScriptedEvent BoulderfallEvent2 = new ScriptedEvent(new Vector2(3518, 429), 2, default, default, true, false, "", default, false, default, BoulderfallEvent2Action);

            //BOULDERFALL EVENT 3 - TWIN PEAK RIGHTMOST ENTRANCE
            ScriptedEvent BoulderfallEvent3 = new ScriptedEvent(new Vector2(3665, 360), 6, default, default, true, false, "", default, false, default, BoulderfallEvent3Action);

            //FIREBOMB HOLLOW AMBUSH - ON BRIDGE AT TWIN PEAKS - ONLY ONCE
            List<int> FirebombHollowAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.FirebombHollow>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> FirebombHollowAmbushEnemyLocations = new List<Vector2>() { new Vector2(3386, 367), new Vector2(3451, 367) };
            ScriptedEvent FirebombHollowAmbush = new ScriptedEvent(new Vector2(3418, 364), 10, FirebombHollowAmbushEnemyTypeList, FirebombHollowAmbushEnemyLocations, default, true, false, "Ambush!", Color.Red, false, default, FirebombHollowAmbushAction);

            //LEONHARD PHASE 1 EVENT - BY ADAMANTITE GATE ACROSS BRIDGE FROM WIZARDS HOUSE
            ScriptedEvent LeonhardPhase1Event = new ScriptedEvent(new Vector2(3314, 355), 34, ModContent.NPCType<NPCs.Special.LeonhardPhase1>(), 54, true, false, "You hear footsteps...", Color.Red, false, LeonhardPhase1Undefeated, peaceCandleEffect: true);

            //HOLLOW AMBUSH 1 - BOTTOM RIGHT OF EARTH TEMPLE
            List<int> HollowAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> HollowAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4446, 1211), new Vector2(4456, 1211) };
            ScriptedEvent HollowAmbush1 = new ScriptedEvent(new Vector2(4422, 1210), 10, HollowAmbush1EnemyTypeList, HollowAmbush1EnemyLocations, default, true, false, "Ambush!", Color.Red, false, PreEoCCustomCondition, null);

            //GOBLIN AMBUSH 1 - RIGHT OF WORLD SPAWN
            List<int> GoblinAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.AbandonedStump>(), NPCID.GoblinSorcerer, NPCID.GoblinScout };
            List<Vector2> GoblinAmbush1EnemyLocations = new List<Vector2>() { new Vector2(5012, 851), new Vector2(5013, 823), new Vector2(5049f, 839) };
            ScriptedEvent GoblinAmbush1 = new ScriptedEvent(new Vector2(5028, 837), 18, GoblinAmbush1EnemyTypeList, GoblinAmbush1EnemyLocations, default, true, false, "Ambush!", Color.Red, false);
            GoblinAmbush1.SetCustomStats(400, null, null); //I haven't set this one to save so players can farm the goblin scout and tattered cloth if they really feel the need to
            GoblinAmbush1.SetCustomDrops(new List<int>() { ItemID.TatteredCloth }, new List<int>() { 1 }, true);

            //SHADOW MAGE AMBUSH - IN TUNNEL AFTER TWIN EOW FIGHT
            List<int> ShadowMageAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowSoldier>(), ModContent.NPCType<NPCs.Enemies.ShadowMage>() };
            List<Vector2> ShadowMageAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4029, 1429), new Vector2(4074, 1399) };
            ScriptedEvent ShadowMageAmbush1 = new ScriptedEvent(new Vector2(4060, 1418), 10, ShadowMageAmbush1EnemyTypeList, ShadowMageAmbush1EnemyLocations, DustID.CursedTorch, true, false, "Ambush!", Color.Red, false, PreSkeletronCustomCondition, null);
            ShadowMageAmbush1.SetCustomStats(700, 18, null); // Lowers the mage's HP, and raises the soldiers

            //BRIDGE AMBUSH 1 - ON BRIDGE POST EOW
            List<int> BridgeAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.HollowSoldier>(), ModContent.NPCType<NPCs.Enemies.ManHunter>(), ModContent.NPCType<NPCs.Enemies.TibianAmazon>(), NPCID.Piranha, NPCID.Piranha, NPCID.Piranha };
            List<Vector2> BridgeAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4593, 858), new Vector2(4640, 858), new Vector2(4643f, 841), new Vector2(4588f, 858), new Vector2(4608f, 870), new Vector2(4616f, 872), new Vector2(4626f, 870) };
            ScriptedEvent BridgeAmbush1 = new ScriptedEvent(new Vector2(4615, 852), 6, BridgeAmbush1EnemyTypeList, BridgeAmbush1EnemyLocations, DustID.Cloud, true, false, "Ambush!", Color.Red, false, PostEoWCustomCondition, null);

            //LOTHRIC AMBUSH 1 - IN ROOM BELOW ARTORIAS BOSS FIGHT ROOM, APPROACHING JUNGLE PYRAMID FROM FORGOTTEN CITY
            List<int> LothricAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.LothricKnight>(), ModContent.NPCType<NPCs.Enemies.LothricSpearKnight>() };
            List<Vector2> LothricAmbush1EnemyLocations = new List<Vector2>() { new Vector2(5148, 1757), new Vector2(5197, 1757) };
            ScriptedEvent LothricAmbush1 = new ScriptedEvent(new Vector2(5173, 1750), 6, LothricAmbush1EnemyTypeList, LothricAmbush1EnemyLocations, DustID.DungeonWater, true, false, "Ambush!", Color.Red, false, PreMechCustomCondition, null);
            LothricAmbush1.SetCustomStats(null, null, null, 500);
            LothricAmbush1.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.GreenBlossom>() }, new List<int>() { 5 }, true);

            //LOTHRIC AMBUSH 2 - IN ROOM BEFORE TRIPLE ENCHANTED SWORDS, UNDER EARTH TEMPLE ENTRANCE
            List<int> LothricAmbush2EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.LothricKnight>() };
            List<Vector2> LothricAmbush2EnemyLocations = new List<Vector2>() { new Vector2(4596, 946) };
            ScriptedEvent LothricAmbush2 = new ScriptedEvent(new Vector2(4574, 945), 12, LothricAmbush2EnemyTypeList, LothricAmbush2EnemyLocations, DustID.DungeonWater, true, false, "Ambush!", Color.Red, false, PreMechCustomCondition, null);
            LothricAmbush2.SetCustomStats(null, null, 70, 600); // Lower damage than normal, slightly more souls than normal
            LothricAmbush2.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.RadiantLifegem>() }, new List<int>() { 5 });

            ScriptedEvent HellkiteDragonEvent = new ScriptedEvent(new Vector2(4282, 405), 200, ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>(), DustID.OrangeTorch, true, true, "The village is under attack! A Hellkite Dragon has come to feed...", new Color(175, 75, 255), false, SuperHardModeCustomCondition, SetNightCustomAction);


            //Every enum and ScriptedEvent has to get paired up here
            ScriptedEventDict = new Dictionary<ScriptedEventType, ScriptedEvent>(){

                
                {ScriptedEventType.AlienAmbush, AlienAmbush},
                {ScriptedEventType.EoC, EoCEvent},
                {ScriptedEventType.EoW1, EoW1Event},
                {ScriptedEventType.AncientDemon, AncientDemon},
                {ScriptedEventType.LitchKing, LitchKing},
                {ScriptedEventType.TheHunter, TheHunter},
                {ScriptedEventType.TheRage, TheRage},
                {ScriptedEventType.AODE, AODE},
                {ScriptedEventType.GoblinWizardWMF, GoblinWizardWMF},
                {ScriptedEventType.GoblinWizardClouds, GoblinWizardClouds},
                {ScriptedEventType.Golem2, Golem2},
                {ScriptedEventType.IceGolemEvent, IceGolemEvent},
                {ScriptedEventType.KingSlimeEvent, KingSlimeEvent},
                {ScriptedEventType.HeroofLumeliaFight, HeroofLumeliaFight},
                {ScriptedEventType.FireLurkerPain, FireLurkerPain},
                {ScriptedEventType.RedKnightPain, RedKnightPain},
                {ScriptedEventType.RedKnightTwinMountain, RedKnightTwinMountain},
                {ScriptedEventType.JungleWyvernFight, JungleWyvernEvent},
                {ScriptedEventType.SeathFight, SeathEvent},
                {ScriptedEventType.WyvernMageFight, WyvernMageEvent},
                {ScriptedEventType.SlograAndGaibonFight, SlograAndGaibonEvent},
                {ScriptedEventType.SerrisFight, SerrisEvent},
                {ScriptedEventType.MarilithFight, MarilithEvent},
                {ScriptedEventType.KrakenFight, KrakenEvent},
                {ScriptedEventType.GwynFight, GwynEvent},
                {ScriptedEventType.AbysmalOolacileSorcererFight, AbysmalOolacileSorcererEvent},
                {ScriptedEventType.WitchkingFight, WitchkingEvent},
                {ScriptedEventType.ChaosFight, ChaosEvent},
                {ScriptedEventType.WyvernMageShadowFight, WyvernMageShadowEvent},
                {ScriptedEventType.BlightFight, BlightEvent},
                {ScriptedEventType.DarkCloudPyramidFight, DarkCloudEvent},
                {ScriptedEventType.ArtoriasFight, ArtoriasEvent},
                {ScriptedEventType.BlackKnightCity, BlackKnightCity},
                //{ScriptedEventType.ExampleHarpySwarm, ExampleHarpySwarm},
                //{ScriptedEventType.ExampleNoNPCScriptEvent, ExampleNoNPCScriptEvent},
                //{ScriptedEventType.Frogpocalypse2_TheFroggening, FrogpocalypseEvent}
                {ScriptedEventType.SpawnUndeadMerchant, SpawnUndeadMerchant },
                {ScriptedEventType.SpawnGoblin, SpawnGoblin },
                {ScriptedEventType.AttraidiesTheSorrowEvent, AttraidiesTheSorrowEvent},
                {ScriptedEventType.TwinEoWFight, TwinEoWFight},
                {ScriptedEventType.DunledingAmbush, DunledingAmbush},
                {ScriptedEventType.BoulderfallEvent1, BoulderfallEvent1},
                {ScriptedEventType.BoulderfallEvent2, BoulderfallEvent2},
                {ScriptedEventType.BoulderfallEvent3, BoulderfallEvent3},
                {ScriptedEventType.LeonhardPhase1Event, LeonhardPhase1Event},
                {ScriptedEventType.HollowAmbush1, HollowAmbush1},
                {ScriptedEventType.GoblinAmbush1, GoblinAmbush1},
                {ScriptedEventType.ShadowMageAmbush1, ShadowMageAmbush1},
                {ScriptedEventType.BridgeAmbush1, BridgeAmbush1},
                {ScriptedEventType.LothricAmbush1, LothricAmbush1},
                {ScriptedEventType.LothricAmbush2, LothricAmbush2},
                {ScriptedEventType.FirebombHollowAmbush, FirebombHollowAmbush},
                {ScriptedEventType.SpawnMechanic, SpawnMechanic},
                {ScriptedEventType.SpawnWizard, SpawnWizard},
                {ScriptedEventType.HellkiteDragonEvent, HellkiteDragonEvent},
                {ScriptedEventType.EoL, EoL}
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


        public static bool LumeliaCustomCondition()
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>())))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //COMMON CONDITIONS
        public static bool NormalModeCustomCondition()
        {
            return !Main.hardMode;
        }
        public static bool PreEoCCustomCondition()
        {
            return !NPC.downedBoss1;
        }
        public static bool PreEoWCustomCondition()
        {
            if (NPC.downedBoss2 || NPC.AnyNPCs(NPCID.EaterofWorldsHead) || NPC.AnyNPCs(NPCID.EaterofWorldsBody) || NPC.AnyNPCs(NPCID.EaterofWorldsTail))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool PostEoWCustomCondition()
        {
            return NPC.downedBoss2;
        }
        public static bool PreSkeletronCustomCondition()
        {
            return !NPC.downedBoss3;
        }
        public static bool PreMechCustomCondition()
        {
            return !NPC.downedMechBossAny;
        }
        public static bool NightCustomCondition()
        {
            return !Main.dayTime;
        }
        
        
        public static bool HardModeCustomCondition()
        {
            return Main.hardMode;
        }
        //This condition returns true if the world is in superhardmode
        public static bool SuperHardModeCustomCondition()
        {
            return tsorcRevampWorld.SuperHardMode;
        }
        public static bool MarilithCustomCondition()
        {

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>())) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.MarilithIntro>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GolemDownedCustomCondition()
        {
            return NPC.downedGolemBoss;
        }
        public static bool EoLDownedCondition()
        {
            return !NPC.downedEmpressOfLight;
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

        public static bool EoWCustomCondition()
        {
            if(NPC.AnyNPCs(NPCID.EaterofWorldsHead) || NPC.AnyNPCs(NPCID.EaterofWorldsBody) || NPC.AnyNPCs(NPCID.EaterofWorldsTail))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool UndeadMerchantCondition()
        {
            return !NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.UndeadMerchant>());
        }

        public static bool TinkererCondition()
        {
            return !NPC.AnyNPCs(NPCID.GoblinTinkerer);
        }

        public static bool SlograGaibonCondition()
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Slogra>()) || (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Gaibon>())) || (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Slogra>())) && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Gaibon>()))))
            {
                return false;
            }
            return true;
        }

        public static bool SerrisCustomCondition()
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>())) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool AttraidiesTheSorrowCondition()
        {
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool LeonhardPhase1Undefeated()
        {
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Special.LeonhardPhase1>())))
            {
                return true;
            }
            else
            { 
                return false;
            }
        }

        public static bool MechanicCondition()
        {
            return !NPC.AnyNPCs(NPCID.Mechanic);
        }

        public static bool WizardCondition()
        {
            return !NPC.AnyNPCs(NPCID.Wizard);
        }

        #endregion

        #region customactions
        //You can make custom actions like this, and pass them as arguments to the event!
        public static bool ExampleCustomAction(Player player, ScriptedEvent thisEvent)
        {
            Dust.NewDust(player.position, 30, 30, DustID.GreenFairy, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 255);
            if (thisEvent.eventTimer > 900)
            {
                UsefulFunctions.BroadcastText("The example scripted event ends...", Color.Green);
                thisEvent.endEvent = true;
                return true;
            }
            return false;
        }


        //This is an example artorias custom action. It spawns meteors and displays text every so often, and also changes the projectile damage for Artorias. Most enemies will require a very small change for their projectile damage changes to work (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static bool ArtoriasCustomAction(Player player, ScriptedEvent thisEvent)
        {
            //Spawning meteors:
            if (Main.rand.NextBool(200))
            {
                UsefulFunctions.BroadcastText("Artorias rains fire from the Abyss...", Color.Gold);
                for (int i = 0; i < 10; i++)
                {
                    Projectile.NewProjectile(new EntitySource_Misc("Scripted Event"), (float)player.position.X - 100 + Main.rand.Next(200), (float)player.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), thisEvent.spawnedNPC.damage / 4, 2f, Main.myPlayer);
                }
            }

            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (thisEvent.spawnedNPC.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Bosses.SuperHardMode.Artorias ourArtorias = (NPCs.Bosses.SuperHardMode.Artorias)thisEvent.spawnedNPC.ModNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, their damage stats might not be public.
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case blackBreathDamage and phantomSeekerDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourArtorias.blackBreathDamage = 40;
                ourArtorias.phantomSeekerDamage = 50;
            }
            return false;
        }


        public static bool StormCustomAction(Player player, ScriptedEvent thisEvent)
        {
            //typeof(Main).GetMethod("StartRain", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            return true;
        }

        public static bool SetNightCustomAction(Player player, ScriptedEvent thisEvent)
        {
            UsefulFunctions.BroadcastText("Time shifts forward...", Color.Purple);
            Main.dayTime = false;
            Main.time = 0;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
            return true;
        }


        //This is an example custom action that just changes the damage of an NPC's projectile. Most enemies will require a very small change for this to work with them (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static bool BlackKnightCustomAction(Player player, ScriptedEvent thisEvent)
        {
            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (thisEvent.spawnedNPC.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Enemies.BlackKnight ourKnight = (NPCs.Enemies.BlackKnight)thisEvent.spawnedNPC.ModNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, the variable that controls the damage for its projectile might not be public (read: probably isn't).
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case spearDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourKnight.spearDamage = 40;
            }
            return true;
        }

        //FIRE LURKER PAIN CUSTOM ACTION
        public static bool FireLurkerPainCustomAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.spawnedNPC.type == ModContent.NPCType<NPCs.Enemies.FireLurker>())
            {
                NPCs.Enemies.FireLurker ourFireLurker = (NPCs.Enemies.FireLurker)thisEvent.spawnedNPC.ModNPC;

                ourFireLurker.lostSoulDamage = 16; //was 23, then 13
            }
            return true;
        }

        //RED KNIGHT PAIN CUSTOM ACTION
        public static bool RedKnightPainCustomAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.spawnedNPC.type == ModContent.NPCType<NPCs.Enemies.RedKnight>())
            {
                NPCs.Enemies.RedKnight ourRedKnightPain = (NPCs.Enemies.RedKnight)thisEvent.spawnedNPC.ModNPC;
                ourRedKnightPain.redKnightsSpearDamage = 26; //was 20
            }
            return true;
        }

        //RED KNIGHT MOUNTAIN CUSTOM ACTION
        public static bool RedKnightMountainCustomAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.spawnedNPC.type == ModContent.NPCType<NPCs.Enemies.RedKnight>())
            {
                NPCs.Enemies.RedKnight ourRedKnight = (NPCs.Enemies.RedKnight)thisEvent.spawnedNPC.ModNPC;
                ourRedKnight.redKnightsSpearDamage = 22; //was 19
                ourRedKnight.redMagicDamage = 22; //was 19
            }
            return true;
        }

        public static bool UndeadMerchantAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 1686 * 16, 963 * 16, ModContent.NPCType<NPCs.Friendly.UndeadMerchant>());
            thisEvent.endEvent = true;
            return true;
        }

        //i dont want this event to last forever, so just spawn the tinkerer and immediately end the event
        //... is what it SHOULD do?
        public static bool TinkererAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.savedGoblin = true;
            NPC goblinNPC = NPC.NewNPCDirect(new EntitySource_Misc("Scripted Event"), 4456 * 16, 1744 * 16, NPCID.GoblinTinkerer);
            goblinNPC.homeTileX = 4449;
            goblinNPC.homeTileY = 1740;
            thisEvent.endEvent = true;
            return true;
        }

        //TWIN EOW ACTION
        public static bool TwinEoWAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 3183 * 16, 1246 * 16, NPCID.EaterofWorldsHead);
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 3330 * 16, 1246 * 16, NPCID.EaterofWorldsHead);
            return true;
        }

        //Slogra And Gaibon Action
        public static bool SlograAndGaibonCustomAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)player.Center.X + 300, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.Slogra>());
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)player.Center.X - 300, (int)player.Center.Y, ModContent.NPCType<NPCs.Bosses.Gaibon>());
            thisEvent.endEvent = true;
            return true;
        }
        public static bool SerrisCustomAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)player.Center.X, (int)player.Center.Y + 300, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)player.Center.X, (int)player.Center.Y - 300, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            thisEvent.endEvent = true;
            return true;
        }

        //ALIEN AMBUSH SPAWN DUSTS 
        public static bool AlienAmbushAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, player.Center);
            }
            if (thisEvent.eventTimer < 20)
            {
                for (int i = 0; i < 8; i++)
                {
                    int dust1 = Dust.NewDust(new Vector2(6069 * 16, 69 * 16), 40, 52, DustID.Cloud, -5, 0, 0, default, 2f);
                    Main.dust[dust1].noGravity = true;
                    int dust2 = Dust.NewDust(new Vector2(6010 * 16, 79 * 16), 40, 52, 7, 5, 0, 0, default, 1.5f);
                    Main.dust[dust2].noGravity = true;
                    int dust3 = Dust.NewDust(new Vector2(6010 * 16, 79 * 16), 40, 52, DustID.Cloud, 5, 0, 150, default, 2f);
                    Main.dust[dust3].noGravity = true;
                    int dust4 = Dust.NewDust(new Vector2(6079 * 16, 79 * 16), 40, 52, DustID.Cloud, 5, 0, 150, default, 2f);
                    Main.dust[dust4].noGravity = true;
                    int dust5 = Dust.NewDust(new Vector2(6041 * 16, 69 * 16), 40, 52, DustID.Cloud, 5, 0, 150, default, 2f);
                    Main.dust[dust5].noGravity = true;
                    int dust6 = Dust.NewDust(new Vector2(6079 * 16, 79 * 16), 40, 52, DustID.Cloud, 5, 0, 150, default, 2f);
                    Main.dust[dust6].noGravity = true;
                }
            }
            return false;
        }


        //DUNDLEDING AMBUSH SPAWN DUSTS
        public static bool DundledingAmbushAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, player.Center);
            }
            if (thisEvent.eventTimer < 20)
            {
                for (int i = 0; i < 5; i++)
                {
                    int dust1 = Dust.NewDust(new Vector2(4697 * 16, 856 * 16), 40, 52, DustID.Grass, -5, 0, 0, default, 1.5f);
                    Main.dust[dust1].noGravity = true;
                    int dust2 = Dust.NewDust(new Vector2(4643 * 16, 856 * 16), 40, 52, 7, 5, 0, 0, default, 1.5f);
                    Main.dust[dust2].noGravity = true;
                    int dust3 = Dust.NewDust(new Vector2(4643 * 16, 839 * 16), 40, 52, DustID.Cloud, 5, 0, 150, default, 1.5f);
                    Main.dust[dust3].noGravity = true;
                }
            }
            return false;
        }

        //BOULDERFALL EVENT 1 ACTION

        public static bool BoulderfallEvent1Action(Player player, ScriptedEvent thisEvent)
        {
            Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(4401 * 16, 895 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropLeft>(), 70, 1);
            thisEvent.endEvent = true;
            return true;
        }

        //BOULDERFALL EVENT 2 ACTION

        public static bool BoulderfallEvent2Action(Player player, ScriptedEvent thisEvent)
        {
            int rand1 = Main.rand.Next(10, 40);
            int rand2 = Main.rand.Next(240, 300);

            if (thisEvent.eventTimer == 1)
            {
                Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(3515 * 16, 409 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropLeft>(), 70, 1);
            }
            if (thisEvent.eventTimer == rand1)
            {
                Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(3528 * 16, 409 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropLeft>(), 70, 1);
                thisEvent.eventTimer = 42;
            }
            if (thisEvent.eventTimer == 41)
            {
                thisEvent.eventTimer = 2;
            }
            if (thisEvent.eventTimer == 301)
            {
                thisEvent.eventTimer = 240;
            }
            if (thisEvent.eventTimer == rand2)
            {
                Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(3523 * 16, 409 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropLeft>(), 70, 1);
                thisEvent.endEvent = true;
                return true;
            }
            return false;
        }

        //BOULDERFALL EVENT 3 ACTION

        public static bool BoulderfallEvent3Action(Player player, ScriptedEvent thisEvent)
        {
            Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(3639 * 16, 349 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropRight>(), 70, 1);
            thisEvent.endEvent = true;
            return true;
        }

        //FIREBOMB HOLLOW AMBUSH SPAWN DUSTS
        public static bool FirebombHollowAmbushAction(Player player, ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, player.Center);
            }
            if (thisEvent.eventTimer < 20)
            {
                for (int i = 0; i < 5; i++)
                {
                    int dust1 = Dust.NewDust(new Vector2(3384 * 16, 365 * 16), 40, 52, 7, 5, 0, 0, default, 1.5f); //left enemy
                    Main.dust[dust1].noGravity = true;
                    int dust2 = Dust.NewDust(new Vector2(3451 * 16, 365 * 16), 40, 52, 7, -5, 0, 0, default, 1.5f); //right enemy
                    Main.dust[dust2].noGravity = true;
                }
            }
            return false;
        }

        public static bool MechanicAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 277 * 16, 1366 * 16, NPCID.Mechanic);
            NPC.savedMech = true;
            thisEvent.endEvent = true;
            return true;
        }

        public static bool WizardAction(Player player, ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 7322 * 16, 603 * 16, NPCID.Wizard);
            NPC.savedWizard = true;
            thisEvent.endEvent = true;
            return true;
        }
        #endregion

        public static void SaveScriptedEvents(TagCompound tag)
        {
            if (ScriptedEventValues != null)
            {
                //Converts the keys from enums into strings, because apparently it isn't a huge fan of enums
                List<string> stringList = ScriptedEventValues.Keys.ToList().ConvertAll(enumMember => enumMember.ToString());
                tag.Add("event_types", stringList);
                tag.Add("event_values", ScriptedEventValues.Values.ToList());
            }
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
                        UsefulFunctions.BroadcastText("ERROR: Failed to convert string " + eventTypeStrings[i] + "to enum. Please report this!! You can do so in our discord: https://discord.gg/kSptDbe", Color.Red);
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
        //Temporary list used to count the amount of *actual* events that need to be drawn
        public static int DrawnEvents;
        public static void PlayerScriptedEventCheck(Player player)
        {
            if (player.dead)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool bossAlive = false;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i] != null && Main.npc[i].active && Main.npc[i].boss)
                    {
                        bossAlive = true;
                        break;
                    }
                }

                if (QueuedEvents == null)
                {
                    QueuedEvents = new List<ScriptedEvent>();
                }

                if (!bossAlive)
                {
                    foreach(ScriptedEvent e in QueuedEvents)
                    {
                        bool savedEvent = false;
                        foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                        {
                            if (pair.Value == e)
                            {
                                if(ScriptedEventValues[pair.Key] == true)
                                {
                                    savedEvent = true;
                                }
                                break;
                            }
                        }
                        if (!savedEvent) //Do not re-add a queued event if it has been disabled (ie because the players beat it)
                        {
                            InactiveEvents.Add(e);
                        }
                    }
                    QueuedEvents = new List<ScriptedEvent>();
                }

                DrawnEvents = 0;
                //Check if the player is in range of any inactive events
                for (int i = 0; i < InactiveEvents.Count; i++)
                {
                    if (InactiveEvents[i].condition())
                    {
                        //Add the network event to the list of events that need to be drawn. These will be sent to the client once we're done here.
                        if (InactiveEvents[i].visible)
                        {
                            DrawnEvents++;
                        }

                        float distance = Vector2.DistanceSquared(player.position, InactiveEvents[i].centerpoint);
                        int dustPerTick = 20;
                        float speed = 2f;
                        if (!InactiveEvents[i].square)
                        {

                            //If the player is nearby, display some dust to make the region visible to them
                            //This has a Math.Sqrt in it, but that's fine because this code only runs for the handful-at-most events that will be onscreen at a time
                            if ((InactiveEvents[i].visible && distance < 6000000) || InactiveEvents[i].npcToSpawn == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>() && distance < 50000000
                                 || InactiveEvents[i].npcToSpawn == NPCID.HallowBoss && distance < 50000000)
                            {
                                float sqrtRadius = (float)Math.Sqrt(InactiveEvents[i].radius);
                                for (int j = 0; j < dustPerTick; j++)
                                {

                                    Vector2 dir = Main.rand.NextVector2CircularEdge(sqrtRadius, sqrtRadius);
                                    Vector2 dustPos = InactiveEvents[i].centerpoint + dir;
                                    if (Collision.CanHit(InactiveEvents[i].centerpoint, 0, 0, dustPos, 0, 0) || InactiveEvents[i].npcToSpawn == NPCID.HallowBoss)
                                    {
                                        Vector2 dustVel = new Vector2(speed, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);

                                        Dust thisDust;

                                        if(InactiveEvents[i].npcToSpawn == NPCID.HallowBoss)
                                        {
                                            thisDust = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200, Main.DiscoColor);
                                        }
                                        else
                                        {
                                            thisDust = Dust.NewDustPerfect(dustPos, InactiveEvents[i].dustID, dustVel, 200);
                                        }

                                        thisDust.noGravity = true;
                                    }
                                }
                            }
                            if (distance < InactiveEvents[i].radius * 6)
                            {
                                player.AddBuff(BuffID.PeaceCandle, 2);
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

                //Send events that need to be drawn to the clients
                if (Main.netMode == NetmodeID.Server && (NetworkEvents == null || NetworkEvents.Count != DrawnEvents || Main.GameUpdateCount % 300 == 0))
                {
                    NetworkEvents = new List<NetworkEvent>();
                    for (int i = 0; i < InactiveEvents.Count; i++)
                    {
                        if (InactiveEvents[i].condition())
                        {
                            //Add the network event to the list of events that need to be drawn. These will be sent to the client once we're done here.
                            if (InactiveEvents[i].visible)
                            {
                                NetworkEvents.Add(new NetworkEvent(InactiveEvents[i].centerpoint, InactiveEvents[i].radius, InactiveEvents[i].dustID, InactiveEvents[i].square));
                            }
                        }
                    }

                    SendDrawnEvents();
                }

                //Run any active events
                for (int i = 0; i < ActiveEvents.Count; i++)
                {
                    ActiveEvents[i].RunEvent(player);
                }
            }
            else
            {
                //Check if the player is in range of any networked events
                if (NetworkEvents != null)
                {
                    for (int i = 0; i < NetworkEvents.Count; i++)
                    {
                        float distance = Vector2.DistanceSquared(player.position, NetworkEvents[i].centerpoint);
                        int dustPerTick = 20;
                        float speed = 2f;
                        if (!NetworkEvents[i].square)
                        {
                            //If the player is nearby, display some dust to make the region visible to them
                            //This has a Math.Sqrt in it, but that's fine because this code only runs for the handful-at-most events that will be onscreen at a time
                            if (distance < 6000000)
                            {
                                float sqrtRadius = (float)Math.Sqrt(NetworkEvents[i].radius);
                                for (int j = 0; j < dustPerTick; j++)
                                {

                                    Vector2 dir = Main.rand.NextVector2CircularEdge(sqrtRadius, sqrtRadius);
                                    Vector2 dustPos = NetworkEvents[i].centerpoint + dir;
                                    if (Collision.CanHit(NetworkEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                    {
                                        Vector2 dustVel = new Vector2(speed, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                                        Dust dustID = Dust.NewDustPerfect(dustPos, NetworkEvents[i].dustID, dustVel, 200);
                                        dustID.noGravity = true;
                                    }
                                }
                            }
                            if (distance < NetworkEvents[i].radius * 6)
                            {
                                player.AddBuff(BuffID.PeaceCandle, 2);
                            }

                            if (distance < NetworkEvents[i].radius)
                            {
                                for (int j = 0; j < 100; j++)
                                {
                                    Dust.NewDustPerfect(NetworkEvents[i].centerpoint, NetworkEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                }
                            }
                        }
                        //Do the same thing, but square
                        else
                        {
                            float sqrtRadius = (float)Math.Sqrt(NetworkEvents[i].radius);
                            if (distance < 6000000)
                            {
                                Vector2 dustPos;
                                Vector2 dustVel;
                                Dust dustID;
                                for (int j = 0; j < dustPerTick; j++)
                                {
                                    int side = Main.rand.Next(0, 4);
                                    if (side == 0)
                                    {
                                        dustPos = new Vector2(NetworkEvents[i].centerpoint.X + sqrtRadius, NetworkEvents[i].centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                                        if (Collision.CanHit(NetworkEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                        {
                                            dustVel = new Vector2(0, speed);
                                            dustID = Dust.NewDustPerfect(dustPos, NetworkEvents[i].dustID, dustVel, 200);
                                            dustID.noGravity = true;
                                        }
                                    }
                                    if (side == 1)
                                    {
                                        dustPos = new Vector2(NetworkEvents[i].centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), NetworkEvents[i].centerpoint.Y + sqrtRadius);
                                        if (Collision.CanHit(NetworkEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                        {
                                            dustVel = new Vector2(-speed, 0);
                                            dustID = Dust.NewDustPerfect(dustPos, NetworkEvents[i].dustID, dustVel, 200);
                                            dustID.noGravity = true;
                                        }
                                    }
                                    if (side == 2)
                                    {
                                        dustPos = new Vector2(NetworkEvents[i].centerpoint.X - sqrtRadius, NetworkEvents[i].centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                                        if (Collision.CanHit(NetworkEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                        {
                                            dustVel = new Vector2(0, -speed);
                                            dustID = Dust.NewDustPerfect(dustPos, NetworkEvents[i].dustID, dustVel, 200);
                                            dustID.noGravity = true;
                                        }
                                    }
                                    if (side == 3)
                                    {
                                        dustPos = new Vector2(NetworkEvents[i].centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), NetworkEvents[i].centerpoint.Y - sqrtRadius);
                                        if (Collision.CanHit(NetworkEvents[i].centerpoint, 0, 0, dustPos, 0, 0))
                                        {
                                            dustVel = new Vector2(speed, 0);
                                            dustID = Dust.NewDustPerfect(dustPos, NetworkEvents[i].dustID, dustVel, 200);
                                            dustID.noGravity = true;
                                        }
                                    }
                                }
                            }

                            if ((Math.Abs(player.position.X - NetworkEvents[i].centerpoint.X) < sqrtRadius) && (Math.Abs(player.position.Y - NetworkEvents[i].centerpoint.Y) < sqrtRadius))
                            {
                                for (int j = 0; j < 100; j++)
                                {
                                    Dust.NewDustPerfect(NetworkEvents[i].centerpoint, NetworkEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                }
                            }
                        }

                    }
                }
            }
        }

        public static void SendDrawnEvents()
        {
            ModPacket eventPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            eventPacket.Write((byte)tsorcPacketID.SyncEventDust);
            eventPacket.Write(NetworkEvents.Count);

            foreach (NetworkEvent thisEvent in NetworkEvents)
            {
                eventPacket.WriteVector2(thisEvent.centerpoint);
                eventPacket.Write(thisEvent.radius);
                eventPacket.Write(thisEvent.dustID);
                eventPacket.Write(thisEvent.square);
            }

            eventPacket.Send();
        }
        public static void RefreshEvents()
        {
            bool bossAlive = false;

            for(int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i] != null && Main.npc[i].active && Main.npc[i].boss)
                {
                    bossAlive = true;
                    break;
                }
            }

            if(QueuedEvents == null)
            {
                QueuedEvents = new List<ScriptedEvent>();
            }

            foreach (ScriptedEvent currentEvent in DisabledEvents)
            {
                if (bossAlive)
                {
                    QueuedEvents.Add(currentEvent);
                }
                else
                {
                    //Only re-add event if it has not been defeated
                    //This is also checked when the queued events are re-enabled back in ScriptedEventCheck
                    bool savedEvent = false;
                    foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                    {
                        if (pair.Value == currentEvent)
                        {
                            if (ScriptedEventValues[pair.Key] == true)
                            {
                                savedEvent = true;
                            }
                            break;
                        }
                    }
                    if (!savedEvent) //Do not re-add a queued event if it has been disabled (ie because the players beat it)
                    {
                        InactiveEvents.Add(currentEvent);
                    }
                }
            }
            DisabledEvents = new List<ScriptedEvent>();
        }
    }

    //Class to keep each scripted event encapsulated
    public class ScriptedEvent
    {
        //Condition controls when the event an occur. If it's false, the event will not run.
        //For example, if you only want an event to run in superhardmode, you'd pass tsorcRevampMain.SuperHardMode as condition
        //If you only wanted it to occur between certain times, you would pass (Main.time > 0700 && Main.time < 1800), for example.

        //Custom condition
        public Func<bool> condition = DefaultCondition;
        //Is this an event that has no associated NPC?
        public bool noNPCEvent = false;
        //Does this event use a list of spawns instead of a single id?
        public bool useListSpawns = false;
        //If so, they're stored here
        public List<int> NPCIDs;
        //The coordinates of each NPC is also respectively stored here
        public List<Vector2> NPCCoordinates;
        //A list pointing to the actual active NPC's that it has spawned
        public List<NPC> spawnedNPCs = new List<NPC>();
        //Stores which NPC's have been killed if in list mode
        public List<bool> deadNPCs = new List<bool>();

        //Stores which players have not died while this event is active
        //public List<bool> livingPlayers = new List<bool>();

        //Does it have extra loot? If so, what items and how many of them should it drop?
        public bool hasCustomDrops = false;
        public List<int> CustomDrops;
        public List<int> DropAmounts;
        public bool onlyLastEnemy = false;

        //ID of the NPC to be spawned
        public readonly int npcToSpawn;
        //The ID of the NPC that the event has spawned
        public NPC spawnedNPC = null;
        //Has the npc been killed? This gets set true in NPCLoot upon the NPC getting killed by the player
        public bool npcDead = false;
        //The text an event should display
        public readonly string eventText;
        //The color it should display it in
        public Color eventTextColor;
        //The dust an event should use
        public int dustID;
        //Controls whether an event is saved. If not, it will reappear upon either player death or game load.
        public bool save;
        //Controls whether the event's range is made visible to the player with dust
        public bool visible;

        //If a custom action sets this to true, the event ends.
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

        //Does it have a custom action? If so, what?
        public bool hasCustomAction = false;
        public bool finishedCustomAction = false;
        public Func<Player, ScriptedEvent, bool> CustomAction = null;

        //Generic multipurpose timer that custom actions can use to time things
        public int eventTimer = 0;

        //If npcs have custom stats, they get stored here
        public int? newMaxLife;
        public int? newDefense;
        public int? newDamage;
        public int? newSouls;
        public bool peaceCandle;

        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, int? npcType = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<Player, ScriptedEvent, bool> customAction = null, bool peaceCandleEffect = false)
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
                noNPCEvent = true;
            }

            eventText = flavorText;
            eventTextColor = flavorTextColor;
            dustID = DustType;
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

            peaceCandle = peaceCandleEffect;
        }

        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs, List<Vector2> coords, int DustType = 31, bool saveEvent = false, bool visibleRange = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<Player, ScriptedEvent, bool> customAction = null, bool peaceCandleEffect = false)
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
            dustID = DustType;
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

            peaceCandle = peaceCandleEffect;
        }

        public void SetCustomStats(int? health = null, int? defense = null, int? damage = null, int? souls = null)
        {
            if (health != null)
            {
                newMaxLife = health;
            }
            if (defense != null)
            {
                newDefense = defense;
            }
            if (damage != null)
            {
                newDamage = damage;
            }
            if (souls != null)
            {
                newSouls = souls;
            }
        }

        public void SetCustomDrops(List<int> dropIDs, List<int> dropStackSizes, bool onlyFinalEnemy = false)
        {
            CustomDrops = dropIDs;
            DropAmounts = dropStackSizes;
            hasCustomDrops = true;
            onlyLastEnemy = onlyFinalEnemy;
        }

        //Runs the event
        public void RunEvent(Player player)
        {
            //If this is its first time running, spawn the NPC's and display the text
            if (eventTimer == 0)
            {
                if (!noNPCEvent)
                {
                    SpawnNPC();
                }
                if (eventText != "default")
                {
                    UsefulFunctions.BroadcastText(eventText, eventTextColor);
                }
            }

            //If it has a custom action, then run it. If it returns true, mark it as finished and do not run it again.
            if (hasCustomAction && !finishedCustomAction)
            {
                if (CustomAction(player, this))
                {
                    finishedCustomAction = true;
                }
            }

            //Updates timer *after* running actions
            eventTimer++;

            bool playerAlive = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                //If any player is alive, do nothing
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    playerAlive = true;
                    break;
                }
            }

            //If we reach the end without hitting one, end the event.
            if (!playerAlive)
            {
                EndEvent(false);
                return;
            }

            //If the NPC is dead or if the custom action set endEvent to true, remove it from active events
            //If so, and this is marked as an event that should be saved, then do so by getting the key for this event and marking it as finished in ScriptedEventValues
            //Otherwise add it back to InactiveEvents
            if (!useListSpawns)
            {
                if (spawnedNPC != null && !spawnedNPC.active)
                {
                    EndEvent(npcDead);
                }
            }
            else
            {
                bool oneAlive = false;
                for (int i = 0; i < deadNPCs.Count; i++)
                {
                    if (deadNPCs[i] == false)
                    {
                        oneAlive = true;
                    }
                }
                if (!oneAlive)
                {
                    endEvent = true;
                }
            }

            if (endEvent)
            {
                EndEvent(true);
            }
        }

        public void SpawnNPC()
        {
            if (useListSpawns)
            {
                for (int i = 0; i < NPCIDs.Count; i++)
                {
                    spawnedNPCs.Add(Main.npc[NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)NPCCoordinates[i].X * 16, (int)NPCCoordinates[i].Y * 16, NPCIDs[i])]);
                    deadNPCs.Add(false);
                    if (newMaxLife != null)
                    {
                        spawnedNPCs[i].lifeMax = newMaxLife.Value;
                        spawnedNPCs[i].life = newMaxLife.Value;
                    }
                    if (newDefense != null)
                    {
                        spawnedNPCs[i].defense = newDefense.Value;
                    }
                    if (newDamage != null)
                    {
                        spawnedNPCs[i].damage = newDamage.Value;
                    }
                    if (newSouls != null)
                    {
                        if (Main.expertMode)
                        {
                            spawnedNPCs[i].value = newSouls.Value * 25;
                        }
                        else
                        {
                            spawnedNPCs[i].value = newSouls.Value * 10;
                        }
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPCs[i].whoAmI);
                    }
                }
            }
            else
            {
                spawnedNPC = Main.npc[NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)centerpoint.X, (int)centerpoint.Y, npcToSpawn)];
                if (newMaxLife != null)
                {
                    spawnedNPC.lifeMax = newMaxLife.Value;
                    spawnedNPC.life = newMaxLife.Value;
                }
                if (newDefense != null)
                {
                    spawnedNPC.defense = newDefense.Value;
                }
                if (newDamage != null)
                {
                    spawnedNPC.damage = newDamage.Value;
                }
                if (newSouls != null)
                {
                    if (Main.expertMode)
                    {
                        spawnedNPC.value = newSouls.Value * 25;
                    }
                    else
                    {
                        spawnedNPC.value = newSouls.Value * 10;
                    }
                }
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPC.whoAmI, 0f, 0f, 0f, 0);
                }
            }
        }

        public void EndEvent(bool eventCompleted)
        {
            //Save the event if it's marked as a saved event and it is 'completed' (either by a customaction forcibly ending it, or by all the NPC's being killed)
            if (eventCompleted && save)
            {
                foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                {
                    if (pair.Value == this)
                    {
                        tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                    }
                }
            }
            //Otherwise if it wasn't completed, then despawn the NPC's and re-add it to DisabledEvents to be re-initialized once the player respawns
            else
            {
                tsorcScriptedEvents.InactiveEvents.Add(this);
                if (spawnedNPC != null)
                {
                    if (spawnedNPC.active && spawnedNPC.boss == false)
                    {
                        spawnedNPC.active = false;
                        for (int i = 0; i < 60; i++)
                        {
                            Dust.NewDustDirect(spawnedNPC.position, spawnedNPC.width, spawnedNPC.height, dustID, Main.rand.Next(-5, 5), Main.rand.Next(-12, 12), 150, default, 3f).noGravity = true;
                        }
                    }
                }
                if (spawnedNPCs.Count > 0)
                {
                    foreach (NPC thisNPC in spawnedNPCs)
                    {
                        if (thisNPC.active)
                        {
                            thisNPC.active = false;
                            for (int i = 0; i < 60; i++)
                            {
                                Dust.NewDustDirect(thisNPC.position, thisNPC.width, thisNPC.height, dustID, Main.rand.Next(-5, 5), Main.rand.Next(-12, 12), 150, default, 3f).noGravity = true;
                            }
                        }
                    }
                }
            }

            tsorcScriptedEvents.ActiveEvents.Remove(this);
            eventTimer = 0;
            npcDead = false;
            deadNPCs = new List<bool>();
            spawnedNPCs = new List<NPC>();
            finishedCustomAction = false;
            endEvent = false;
        }

        public static bool DefaultCondition()
        {
            return true;
        }
    }

    //Simpler class to store network events, since they only require a few points of data.
    public class NetworkEvent
    {
        //What is the centerpoint of the region?
        public Vector2 centerpoint;
        //What is the radius in blocks it should check around that centerpoint?
        public float radius;
        //What type of dust should it spawn?
        public int dustID;
        //Is it checking if they're in a square range around a point, or a circular one?
        public bool square;

        public NetworkEvent(Vector2 position, float range, int DustType, bool squareRange)
        {
            centerpoint = position;
            radius = range;
            dustID = DustType;
            square = squareRange;
        }
    }
}