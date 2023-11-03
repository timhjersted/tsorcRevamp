using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.NPCs.Bosses.Pinwheel;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Utilities;

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
        public static List<ScriptedEvent> EnabledEvents;

        //Stores the events that have been triggered by the player and are currently active. It will run the RunEvent() code for each of these as long as they remain active.
        public static List<ScriptedEvent> RunningEvents;

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
            Pinwheel,
            LothricKnightCatacombs,
            FireLurkerAmbush1,
            Death,
            BlackKnightSHMDungeon,
            RedKnightOolicileForest,
            BlackKnightHallowed,
            QueenSlimeEvent,
            GreatRedKnightTropicalIsland,
            GreatRedKnightInDesert,
            AncestralSpirit,
            SkeletronHidden,
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
            PrimeFight,
            KrakenFight,
            GwynTombVision,
            AbyssPortal,
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
            EoL,
            DungeonGuardian,
            OldManEvent,
            DualSandsprogAmbush1

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

            //PINWHEEL
            ScriptedEvent Pinwheel = new ScriptedEvent(new Vector2(4140, 924), 30, ModContent.NPCType<NPCs.Bosses.Pinwheel.Pinwheel>(), DustID.BoneTorch, true, true, true, LangUtils.GetTextValue("Events.Pinwheel"), Color.Black, false);

            //LOTHRIC BLACK KNIGHT IN CATACOMBS OF THE DROWNED
            ScriptedEvent LothricKnightCatacombs = new ScriptedEvent(new Vector2(4137, 895), 15, ModContent.NPCType<NPCs.Enemies.LothricBlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            LothricKnightCatacombs.SetCustomStats(1100, 8, 40, 1500);

            //FIRELURKER AMBUSH 1 - Path of Ambition main room
            List<int> FireLurkerAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.FireLurker>(), ModContent.NPCType<NPCs.Enemies.FireLurker>() };
            List<Vector2> FireLurkerAmbush1EnemyLocations = new List<Vector2>() { new Vector2(3559, 1248), new Vector2(3629, 1248) };
            ScriptedEvent FireLurkerAmbush1 = new ScriptedEvent(new Vector2(3591, 1248), 6, FireLurkerAmbush1EnemyTypeList, FireLurkerAmbush1EnemyLocations, DustID.DungeonWater, true, false, false, LangUtils.GetTextValue("Events.FireLurker"), Color.Red, false, default, FireLurkerPainCustomAction);
            FireLurkerAmbush1.SetCustomStats(500, 12, 70, 650);
            FireLurkerAmbush1.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.GreenBlossom>() }, new List<int>() { 5 }, true);

            //DEATH
            ScriptedEvent Death = new ScriptedEvent(new Vector2(1066, 529), 30, ModContent.NPCType<NPCs.Bosses.Death>(), DustID.BoneTorch, true, true, true, LangUtils.GetTextValue("Events.Death"), Color.Black, false);

            //BLACK KNIGHT IN BLUE SHM DUNGEON
            ScriptedEvent BlackKnightSHMDungeon = new ScriptedEvent(new Vector2(2282, 1650), 30, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightSHMDungeon.SetCustomStats(25000, 30, 140, 16000);

            //RED KNIGHT IN OOLICILE FOREST
            ScriptedEvent RedKnightOolicileForest = new ScriptedEvent(new Vector2(5596, 926), 10, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.RedKnight2"), Color.Purple, false, default, RedKnightMountainCustomAction);
            RedKnightOolicileForest.SetCustomDrops(new List<int>() { ItemID.GreaterHealingPotion, ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<SoulCoin>() }, new List<int>() { 4, 3, 2, 40 });
            RedKnightOolicileForest.SetCustomStats(1000, 9, 55, 1250);

            //BLACK KNIGHT IN HALLOWED CAVES
            ScriptedEvent BlackKnightHallowed = new ScriptedEvent(new Vector2(7454, 1413), 40, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, false, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightHallowed.SetCustomStats(8000, 20, 80, 5000);

            //QUEEN SLIME
            ScriptedEvent QueenSlimeEvent = new ScriptedEvent(new Vector2(7059, 1289), 25, NPCID.QueenSlimeBoss, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.QueenSlime"), Color.Blue, false);

            //GREAT RED KNIGHT IN DESERT
            ScriptedEvent GreatRedKnightInDesert = new ScriptedEvent(new Vector2(2229, 856), 100, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GreatRedKnight>(), DustID.Shadowflame, true, false, true, LangUtils.GetTextValue("Events.GreatRedKnightInvasion"), Color.Red, false, SuperHardModeCustomCondition);
            GreatRedKnightInDesert.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<Humanity>() }, new List<int>() { 2, 2, 2 });
            GreatRedKnightInDesert.SetCustomStats(null, null, null, 20000);

            //GREAT RED KNIGHT ON FLOATING TROPICAL ISLAND
            ScriptedEvent GreatRedKnightTropicalIsland = new ScriptedEvent(new Vector2(7874, 390), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GreatRedKnight>(), DustID.Shadowflame, true, false, true, LangUtils.GetTextValue("Events.GreatRedKnightInvasion"), Color.Red, false, null, SetNightCustomAction);
            GreatRedKnightInDesert.SetCustomDrops(new List<int>() { ItemID.SuperHealingPotion, ItemID.RagePotion, ModContent.ItemType<HolyWarElixir>() }, new List<int>() { 5, 3, 1 });
            GreatRedKnightInDesert.SetCustomStats(null, null, null, 20000);

            //Ancestral Spirit
            ScriptedEvent AncestralSpiritEvent = new ScriptedEvent(new Vector2(4043, 143), 30, NPCID.Deerclops, DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AncestralSpirit"), Color.Blue, false, null, SetNightCustomAction);

            //SkeletronHidden
            ScriptedEvent SkeletronHiddenEvent = new ScriptedEvent(new Vector2(5563, 1676), 16, NPCID.SkeletronHead, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.SkeletronHidden"), Color.Blue, false, null, SetNightCustomAction);

            //SkeletronHidden
            ScriptedEvent OldManEvent = new ScriptedEvent(new Vector2(4979, 1398), 64, NPCID.OldMan, DustID.WhiteTorch, true, true, true, "default", Color.White, false, () => { return !NPC.AnyNPCs(NPCID.OldMan) && !NPC.AnyNPCs(NPCID.SkeletronHead) && !NPC.downedBoss3; });

            //EoC
            ScriptedEvent EoCEvent = new ScriptedEvent(new Vector2(3900, 1138), 20, NPCID.EyeofCthulhu, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.EoC"), Color.Blue, false, null, SetNightCustomAction);

            //EoW1
            ScriptedEvent EoW1Event = new ScriptedEvent(new Vector2(3633, 996), 46, NPCID.EaterofWorldsHead, DustID.Shadowflame, false, true, true, LangUtils.GetTextValue("Events.EoW"), Color.Purple, false, PreEoWCustomCondition);

            //EMPRESS OF LIGHT
            ScriptedEvent EoL = new ScriptedEvent(new Vector2(4484, 355), 100, NPCID.HallowBoss, DustID.RainbowTorch, false, true, true, LangUtils.GetTextValue("Events.EoL"), Main.DiscoColor, false, EoLDownedCondition);

            //LITCH KING
            ScriptedEvent LitchKing = new ScriptedEvent(new Vector2(364, 1897), 40, ModContent.NPCType<EarthFiendLich>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.LichKing"), Color.Gold, false);

            //THE HUNTER
            ScriptedEvent TheHunter = new ScriptedEvent(new Vector2(296, 1560), 36, ModContent.NPCType<NPCs.Bosses.TheHunter>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Hunter"), Color.DarkGreen, false);

            //THE RAGE
            ScriptedEvent TheRage = new ScriptedEvent(new Vector2(7000, 1845), 30, ModContent.NPCType<NPCs.Bosses.TheRage>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Rage"), Color.Red, false);

            //ANCIENT DEMON (FORGOTTEN CITY, CLOSE TO FIRE TEMPLE)
            ScriptedEvent AncientDemon = new ScriptedEvent(new Vector2(5317, 1800), 25, ModContent.NPCType<NPCs.Bosses.AncientDemon>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.AncientDemon"), Color.MediumPurple, false);
            AncientDemon.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<DarkSoul>() }, new List<int>() { 1, 5000 });

            //ANCIENT OOLACILE DEMON (EARLY-GAME)
            ScriptedEvent AODE = new ScriptedEvent(new Vector2(5652, 971), 27, ModContent.NPCType<NPCs.Bosses.AncientOolacileDemon>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.AncientOolacileDemon"), Color.MediumPurple, false);
            AODE.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<DarkSoul>() }, new List<int>() { 1, 1500 });

            //GOBLIN SUMMONER IN WMF
            ScriptedEvent GoblinWizardWMF = new ScriptedEvent(new Vector2(7153, 411), 20, NPCID.GoblinSummoner, DustID.MagicMirror, true, true, false, LangUtils.GetTextValue("Events.GoblinSummoner1"), Color.MediumPurple, false);
            GoblinWizardWMF.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<DarkSoul>() }, new List<int>() { 1, 1500 });

            //GOBLIN SUMMONER IN THE CLOUDS (WMF)
            ScriptedEvent GoblinWizardClouds = new ScriptedEvent(new Vector2(7822, 118), 40, NPCID.GoblinSummoner, DustID.MagicMirror, true, false, false, LangUtils.GetTextValue("Events.GoblinSummoner2"), Color.MediumPurple, false);
            GoblinWizardClouds.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<DarkSoul>() }, new List<int>() { 1, 1500 });

            //ICE GOLEM WYVERN COMBO
            List<int> Golem2EnemyTypeList = new List<int>() { NPCID.WyvernHead, NPCID.IceGolem };
            List<Vector2> Golem2EnemyLocations = new List<Vector2>() { new Vector2(7776, 829), new Vector2(7800, 868) };
            ScriptedEvent Golem2 = new ScriptedEvent(new Vector2(7900, 868), 30, Golem2EnemyTypeList, Golem2EnemyLocations, DustID.Snow, true, false, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.BlueViolet, false, null, StormCustomAction); //

            //ICE GOLEM - FROZEN OCEAN
            ScriptedEvent IceGolemEvent = new ScriptedEvent(new Vector2(7651, 1020), 20, NPCID.IceGolem, DustID.MagicMirror, true, true, false, LangUtils.GetTextValue("Events.IceGolem"), Color.Blue, false);

            //KING SLIME
            ScriptedEvent KingSlimeEvent = new ScriptedEvent(new Vector2(5995, 1117), 20, NPCID.KingSlime, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.KingSlime"), Color.Blue, false);

            //HERO OF LUMELIA FIGHT
            ScriptedEvent HeroofLumeliaFight = new ScriptedEvent(new Vector2(2229, 854), 60, ModContent.NPCType<NPCs.Bosses.HeroofLumelia>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.HeroOfLumelia"), Color.LightGoldenrodYellow, false, LumeliaCustomCondition);//location previously was 4413, 717, near village

            //FIRE LURKER PATH OF PAIN
            ScriptedEvent FireLurkerPain = new ScriptedEvent(new Vector2(3245, 1252), 9, ModContent.NPCType<NPCs.Enemies.FireLurker>(), DustID.CursedTorch, true, true, true, LangUtils.GetTextValue("Events.FireLurker"), Color.Purple, false, default, FireLurkerPainCustomAction);
            FireLurkerPain.SetCustomStats(1800, 12, 85, 1500);
            FireLurkerPain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion }, new List<int>() { 3, 4 });

            //RED KNIGHT IN PATH OF PAIN
            ScriptedEvent RedKnightPain = new ScriptedEvent(new Vector2(3897, 1219), 20, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.RedKnight1"), Color.Purple, false, default, RedKnightPainCustomAction);
            RedKnightPain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<WorldRune>() }, new List<int>() { 2, 3, 4 });
            RedKnightPain.SetCustomStats(2660, 10, 65, 3350);

            //RED KNIGHT IN TWIN PEAKS MOUNTAIN
            ScriptedEvent RedKnightTwinMountain = new ScriptedEvent(new Vector2(3287, 495), 10, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.RedKnight2"), Color.Purple, false, default, RedKnightMountainCustomAction);
            RedKnightTwinMountain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion, ItemID.AmmoReservationPotion }, new List<int>() { 3, 4, 5 });
            RedKnightTwinMountain.SetCustomStats(2000, 10, 55, 2500);

            //JUNGLE WYVERN
            ScriptedEvent JungleWyvernEvent = new ScriptedEvent(new Vector2(4331, 1713), 16, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>(), DustID.CursedTorch, true, true, true, LangUtils.GetTextValue("Events.JungleWyvern"), Color.Green, false);

            //SEATH THE SCALELESS
            ScriptedEvent SeathEvent = new ScriptedEvent(new Vector2(7737, 1546), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>(), DustID.FireworkFountain_Blue, true, true, true, LangUtils.GetTextValue("Events.SeathTheScaleless"), Color.Blue, false);

            //WYVERN MAGE
            List<int> WyvernMageEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>(), ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>() };
            List<Vector2> WyvernLocations = new List<Vector2>() { new Vector2(7192, 364), new Vector2(7192, 364) };
            ScriptedEvent WyvernMageEvent = new ScriptedEvent(new Vector2(7192, 364), 40, WyvernMageEnemyTypeList, WyvernLocations, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.WyvernMage"), Color.LightCyan, false, null, StormCustomAction);

            //SLOGRA and GAIBON
            //This one works a little different from the others, because it's an event with two bosses that spawns them in an action instead of normally
            //As such, it doesn't "save". Instead, it simply has a custom condition that returns "false" if the boss has truly been beaten. Without this, it would save after just running once...
            List<int> SoggyandGaibonEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.Slogra>(), ModContent.NPCType<NPCs.Bosses.Gaibon>() };
            List<Vector2> SoggyandGaibonLocations = new List<Vector2>() { new Vector2(6192, 1297), new Vector2(6192, 1167) };
            ScriptedEvent SlograAndGaibonEvent = new ScriptedEvent(new Vector2(6192, 1267), 30, SoggyandGaibonEnemyTypeList, SoggyandGaibonLocations, DustID.Shadowflame, false, true, true, LangUtils.GetTextValue("Events.SlograAndGaibon"), Color.Purple, false, SlograGaibonCondition);
            //SERRIS
            //Like Slogra and Gaibon, this one works a little different due to spawning two bosses.
            List<int> SerrisEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>() };
            List<Vector2> SerrisEnemyLocations = new List<Vector2>() { new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(MathHelper.Pi / 3), new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(-MathHelper.Pi / 3), new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(MathHelper.Pi) };
            ScriptedEvent SerrisEvent = new ScriptedEvent(new Vector2(1136, 956), 30, SerrisEnemyTypeList, SerrisEnemyLocations, DustID.FireworkFountain_Blue, false, true, true, LangUtils.GetTextValue("Events.Serris"), Color.Blue, false, SerrisCustomCondition);

            //MARILITH 
            ScriptedEvent MarilithEvent = new ScriptedEvent(new Vector2(3235, 1770), 100, ModContent.NPCType<MarilithIntro>(), DustID.RedTorch, false, true, true, LangUtils.GetTextValue("Events.Marilith"), Color.Red, false, MarilithCustomCondition, disablePeaceCandle: true);

            //SKELETRON PRIME
            ScriptedEvent PrimeEvent = new ScriptedEvent(new Vector2(5090, 1103), 75, ModContent.NPCType<NPCs.Bosses.PrimeV2.PrimeIntro>(), DustID.RedTorch, false, false, true, LangUtils.GetTextValue("Events.TheMachine"), Color.Gray, false, PrimeCustomCondition);

            //KRAKEN
            ScriptedEvent KrakenEvent = new ScriptedEvent(new Vector2(1821, 1702), 30, ModContent.NPCType<WaterFiendKraken>(), DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.WaterFiendKraken"), Color.Blue, false, SuperHardModeCustomCondition);

            //GWYN's TOMB VISIONS
            ScriptedEvent GwynsTombEvent = new ScriptedEvent(new Vector2(670, 1164), 150, ModContent.NPCType<NPCs.Special.GwynBossVision>(), DustID.RedTorch, false, true, true, LangUtils.GetTextValue("Events.GwynTombVisions"), default, false, GwynsTombVisionCustomCondition);

            //ABYSS PORTAL
            ScriptedEvent AbyssPortalEvent = new ScriptedEvent(new Vector2(670, 1164), 9999999, ModContent.NPCType<NPCs.Special.AbyssPortal>(), DustID.RedTorch, false, false, false, LangUtils.GetTextValue("Events.AbyssPortal"), default, false, AbyssPortalCustomCondition);

            //GWYN
            ScriptedEvent GwynEvent = new ScriptedEvent(new Vector2(832, 1244), 16, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.Gwyn"), Color.Red, false);

            //ABYSMAL OOLACILE SORCERER
            ScriptedEvent AbysmalOolacileSorcererEvent = new ScriptedEvent(new Vector2(6721, 1905), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(), DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AbysmalOolacileSorcerer"), Color.Red, false, SuperHardModeCustomCondition);

            //WITCHKING
            ScriptedEvent WitchkingEvent = new ScriptedEvent(new Vector2(2484, 1795), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.Witchking"), Color.Red, false, SuperHardModeCustomCondition);

            //BLIGHT
            ScriptedEvent BlightEvent = new ScriptedEvent(new Vector2(8174, 866), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(), DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.Blight"), Color.Blue, false, SuperHardModeCustomCondition);
            //BlightEvent.SetCustomStats(50000, 30, 50);

            //CHAOS
            ScriptedEvent ChaosEvent = new ScriptedEvent(new Vector2(6415, 1888), 50, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Chaos"), Color.Red, false, SuperHardModeCustomCondition);

            //WYVERN MAGE SHADOW-SHM
            List<int> WyvernShadowEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>() };
            List<Vector2> WyvernShadowLocations = new List<Vector2>() { new Vector2(6432, 196), new Vector2(6432, 196) };
            ScriptedEvent WyvernMageShadowEvent = new ScriptedEvent(new Vector2(6432, 196), 20, WyvernShadowEnemyTypeList, WyvernShadowLocations, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.WyvernMageShadow"), Color.LightBlue, false, SuperHardModeCustomCondition);

            //DARK CLOUD
            ScriptedEvent DarkCloudEvent = new ScriptedEvent(new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.DarkCloud"), Color.LightCyan, false, SuperHardModeCustomCondition);

            //ARTORIAS
            ScriptedEvent ArtoriasEvent = new ScriptedEvent(new Vector2(5344, 1692), 15, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Artorias"), Color.Gold, false, SuperHardModeCustomCondition);
            //ArtoriasEvent.SetCustomDrops(new List<int>() { ItemID.RodofDiscord, ModContent.ItemType<Items.DestructionElement>() }, new List<int>() { 1, 4 });

            //BLACK KNIGHT IN FORGOTTEN CITY
            ScriptedEvent BlackKnightCity = new ScriptedEvent(new Vector2(4508, 1745), 20, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightCity.SetCustomStats(3000, 10, 60, 3500);

            //ATTRAIDIES THE SORROW EVENT
            ScriptedEvent AttraidiesTheSorrowEvent = new ScriptedEvent(new Vector2(8216.5f, 1630), 30, ModContent.NPCType<NPCs.Special.AttraidiesApparition>(), DustID.ShadowbeamStaff, false, true, true, LangUtils.GetTextValue("Events.SorrowAttraidies"), Color.OrangeRed, false, AttraidiesTheSorrowCondition);

            //TWIN EATER OF WORLDS FIGHT
            ScriptedEvent TwinEoWFight = new ScriptedEvent(new Vector2(3245, 1215), 20, default, DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.TwinEaters"), Color.Purple, false, TwinEoWCustomCondition, TwinEoWAction);

            //DUNLENDING AMBUSH
            List<int> DunledingAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>() };
            List<Vector2> DunledingAmbushEnemyLocations = new List<Vector2>() { new Vector2(4697, 858), new Vector2(4645, 858), new Vector2(4645, 841) };
            ScriptedEvent DunledingAmbush = new ScriptedEvent(new Vector2(4666, 856), 10, DunledingAmbushEnemyTypeList, DunledingAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.DunlendingAmbush"), Color.Red, false, PreEoCCustomCondition, DundledingAmbushAction);
            if (Main.netMode == NetmodeID.SinglePlayer && Main.expertMode)
            {
                DunledingAmbush.SetCustomStats((int?)(player.statLifeMax2 * .5f), null, (int?)(player.statLifeMax2 * 0.10f) + 25); //damage doesn't double for Expert
            }
            DunledingAmbush.SetCustomDrops(new List<int>() { ModContent.ItemType<DodgerollMemo>() }, new List<int>() { 1 }, true);


            //ALIEN AMBUSH
            List<int> AlienAmbushEnemyTypeList = new List<int>() { NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet, NPCID.VortexHornet };
            List<Vector2> AlienAmbushEnemyLocations = new List<Vector2>() { new Vector2(6069, 69), new Vector2(6010, 79), new Vector2(6010, 79), new Vector2(6079, 79), new Vector2(6041, 69), new Vector2(6079, 79) };
            ScriptedEvent AlienAmbush = new ScriptedEvent(new Vector2(6041, 79), 60, AlienAmbushEnemyTypeList, AlienAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.AlienAmbush"), Color.Red, false, PreMechCustomCondition, AlienAmbushAction);



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
            ScriptedEvent SpawnUndeadMerchant = new ScriptedEvent(new Vector2(1686, 963), 50, default, 31, false, false, false, "", default, false, UndeadMerchantCondition, UndeadMerchantAction);

            //GOBLIN TINKERER  SPAWN EVENT
            ScriptedEvent SpawnGoblin = new ScriptedEvent(new Vector2(4456, 1744), 100, default, 31, true, true, false, "", default, false, TinkererCondition, TinkererAction);

            //MECHANIC SPAWN EVENT
            ScriptedEvent SpawnMechanic = new ScriptedEvent(new Vector2(294, 1366), 100, default, 31, true, true, false, "", default, false, MechanicCondition, MechanicAction);

            //WIZARD SPAWN EVENT
            ScriptedEvent SpawnWizard = new ScriptedEvent(new Vector2(7322, 603), 40, default, 31, true, true, false, "", default, true, WizardCondition, WizardAction);

            //BOULDERFALL EVENT 1 - EARTH TEMPLE ENTRANCE
            ScriptedEvent BoulderfallEvent1 = new ScriptedEvent(new Vector2(4378, 922), 6, default, default, true, false, false, "", default, false, default, BoulderfallEvent1Action);

            //BOULDERFALL EVENT 2 - BLUE DUNGEON BRICK PARKOUR ROOM IN MOUNTAIN
            ScriptedEvent BoulderfallEvent2 = new ScriptedEvent(new Vector2(3518, 429), 2, default, default, true, false, false, "", default, false, default, BoulderfallEvent2Action);

            //BOULDERFALL EVENT 3 - TWIN PEAK RIGHTMOST ENTRANCE
            ScriptedEvent BoulderfallEvent3 = new ScriptedEvent(new Vector2(3665, 360), 6, default, default, true, false, false, "", default, false, default, BoulderfallEvent3Action);

            //FIREBOMB HOLLOW AMBUSH - ON BRIDGE AT TWIN PEAKS - ONLY ONCE
            List<int> FirebombHollowAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.FirebombHollow>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> FirebombHollowAmbushEnemyLocations = new List<Vector2>() { new Vector2(3386, 367), new Vector2(3451, 367) };
            ScriptedEvent FirebombHollowAmbush = new ScriptedEvent(new Vector2(3418, 364), 10, FirebombHollowAmbushEnemyTypeList, FirebombHollowAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.FirebombHollowAmbush"), Color.Red, false, default, FirebombHollowAmbushAction);

            //LEONHARD PHASE 1 EVENT - BY ADAMANTITE GATE ACROSS BRIDGE FROM WIZARDS HOUSE
            ScriptedEvent LeonhardPhase1Event = new ScriptedEvent(new Vector2(3314, 355), 34, ModContent.NPCType<NPCs.Special.LeonhardPhase1>(), 54, true, false, true, LangUtils.GetTextValue("Events.Leonhard1"), Color.Red, false, LeonhardPhase1Undefeated);

            //HOLLOW AMBUSH 1 - BOTTOM RIGHT OF EARTH TEMPLE
            List<int> HollowAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> HollowAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4446, 1211), new Vector2(4456, 1211) };
            ScriptedEvent HollowAmbush1 = new ScriptedEvent(new Vector2(4422, 1210), 10, HollowAmbush1EnemyTypeList, HollowAmbush1EnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.HollowAmbush1"), Color.Red, false, PreEoCCustomCondition, null);

            //GOBLIN AMBUSH 1 - RIGHT OF WORLD SPAWN
            List<int> GoblinAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.AbandonedStump>(), NPCID.GoblinSorcerer, NPCID.GoblinScout };
            List<Vector2> GoblinAmbush1EnemyLocations = new List<Vector2>() { new Vector2(5012, 851), new Vector2(5013, 823), new Vector2(5049f, 839) };
            ScriptedEvent GoblinAmbush1 = new ScriptedEvent(new Vector2(5028, 837), 18, GoblinAmbush1EnemyTypeList, GoblinAmbush1EnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.GoblinAmbush1"), Color.Red, false);
            GoblinAmbush1.SetCustomStats(400, null, null); //I haven't set this one to save so players can farm the goblin scout and tattered cloth if they really feel the need to
            GoblinAmbush1.SetCustomDrops(new List<int>() { ItemID.TatteredCloth }, new List<int>() { 1 }, true);

            //SANDSPROG AMBUSH 1 - IN LONG SANDY ROOM LEFTMOST OF CORRUPTION TEMPLE
            List<int> DualSandsprog1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.MountedSandsprogMage>(), ModContent.NPCType<NPCs.Enemies.MountedSandsprog>() };
            List<Vector2> DualSandsprog1EnemyLocations = new List<Vector2>() { new Vector2(2606, 806), new Vector2(2673, 817) };
            ScriptedEvent DualSandsprogAmbush1 = new ScriptedEvent(new Vector2(2637, 807.5f), 9, DualSandsprog1EnemyTypeList, DualSandsprog1EnemyLocations, DustID.GemTopaz, true, true, false, LangUtils.GetTextValue("Events.DualSandsprogAmbush1"), Color.Red, false, null, null);
            DualSandsprogAmbush1.SetCustomStats(400, null, null, 300);
            DualSandsprogAmbush1.SetCustomDrops(new List<int>() { ItemID.SandBoots }, new List<int>() { 1 }, true);

            //SHADOW MAGE AMBUSH - IN TUNNEL AFTER TWIN EOW FIGHT
            List<int> ShadowMageAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowSoldier>(), ModContent.NPCType<NPCs.Enemies.ShadowMage>() };
            List<Vector2> ShadowMageAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4029, 1429), new Vector2(4074, 1399) };
            ScriptedEvent ShadowMageAmbush1 = new ScriptedEvent(new Vector2(4060, 1418), 10, ShadowMageAmbush1EnemyTypeList, ShadowMageAmbush1EnemyLocations, DustID.CursedTorch, true, false, false, LangUtils.GetTextValue("Events.ShadowMageAmbush"), Color.Red, false, PreSkeletronCustomCondition, null);
            ShadowMageAmbush1.SetCustomStats(700, 18, null); // Lowers the mage's HP, and raises the soldiers

            //BRIDGE AMBUSH 1 - ON BRIDGE POST EOW
            List<int> BridgeAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.HollowSoldier>(), ModContent.NPCType<NPCs.Enemies.ManHunter>(), ModContent.NPCType<NPCs.Enemies.TibianAmazon>() };
            List<Vector2> BridgeAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4593, 858), new Vector2(4640, 858), new Vector2(4643f, 841), new Vector2(4588f, 858) };
            ScriptedEvent BridgeAmbush1 = new ScriptedEvent(new Vector2(4615, 852), 6, BridgeAmbush1EnemyTypeList, BridgeAmbush1EnemyLocations, DustID.Cloud, true, false, false, LangUtils.GetTextValue("Events.BridgeAmbush1"), Color.Red, false, PostEoWCustomCondition, null);

            //LOTHRIC AMBUSH 1 - IN ROOM BELOW ARTORIAS BOSS FIGHT ROOM, APPROACHING JUNGLE PYRAMID FROM FORGOTTEN CITY
            List<int> LothricAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.LothricKnight>(), ModContent.NPCType<NPCs.Enemies.LothricSpearKnight>() };
            List<Vector2> LothricAmbush1EnemyLocations = new List<Vector2>() { new Vector2(5148, 1757), new Vector2(5197, 1757) };
            ScriptedEvent LothricAmbush1 = new ScriptedEvent(new Vector2(5173, 1750), 6, LothricAmbush1EnemyTypeList, LothricAmbush1EnemyLocations, DustID.DungeonWater, true, false, false, LangUtils.GetTextValue("Events.LothricAmbush1"), Color.Red, false, PreMechCustomCondition, null);
            LothricAmbush1.SetCustomStats(null, null, null, 500);
            LothricAmbush1.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.GreenBlossom>() }, new List<int>() { 5 }, true);

            //LOTHRIC AMBUSH 2 - IN ROOM BEFORE TRIPLE ENCHANTED SWORDS, UNDER EARTH TEMPLE ENTRANCE
            List<int> LothricAmbush2EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.LothricKnight>() };
            List<Vector2> LothricAmbush2EnemyLocations = new List<Vector2>() { new Vector2(4596, 946) };
            ScriptedEvent LothricAmbush2 = new ScriptedEvent(new Vector2(4574, 945), 12, LothricAmbush2EnemyTypeList, LothricAmbush2EnemyLocations, DustID.DungeonWater, true, false, false, LangUtils.GetTextValue("Events.LothricAmbush2"), Color.Red, false, PreMechCustomCondition, null);
            LothricAmbush2.SetCustomStats(null, null, 70, 600); // Lower damage than normal, slightly more souls than normal
            LothricAmbush2.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.RadiantLifegem>() }, new List<int>() { 5 });

            ScriptedEvent HellkiteDragonEvent = new ScriptedEvent(new Vector2(4282, 405), 200, ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.HellkiteDragon"), new Color(175, 75, 255), false, SuperHardModeCustomCondition, SetNightCustomAction);

            ScriptedEvent DungeonGuardianEvent = new ScriptedEvent(new Vector2(4228, 1800), 20, NPCID.DungeonGuardian, DustID.WhiteTorch, false, true, false, "default", new Color(175, 75, 255), false, () => !NPC.downedBoss3);



            //Every enum and ScriptedEvent has to get paired up here
            ScriptedEventDict = new Dictionary<ScriptedEventType, ScriptedEvent>(){

                {ScriptedEventType.Pinwheel,Pinwheel},
                {ScriptedEventType.LothricKnightCatacombs,LothricKnightCatacombs},
                {ScriptedEventType.FireLurkerAmbush1, FireLurkerAmbush1},
                {ScriptedEventType.Death, Death},
                {ScriptedEventType.BlackKnightSHMDungeon, BlackKnightSHMDungeon},
                {ScriptedEventType.RedKnightOolicileForest, RedKnightOolicileForest},
                {ScriptedEventType.QueenSlimeEvent, QueenSlimeEvent},
                {ScriptedEventType.BlackKnightHallowed, BlackKnightHallowed},
                {ScriptedEventType.GreatRedKnightTropicalIsland, GreatRedKnightTropicalIsland},
                {ScriptedEventType.GreatRedKnightInDesert, GreatRedKnightInDesert},
                {ScriptedEventType.AncestralSpirit, AncestralSpiritEvent},
                {ScriptedEventType.OldManEvent, OldManEvent},
                {ScriptedEventType.SkeletronHidden, SkeletronHiddenEvent},
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
                {ScriptedEventType.PrimeFight, PrimeEvent},
                {ScriptedEventType.KrakenFight, KrakenEvent},
                {ScriptedEventType.AbyssPortal, AbyssPortalEvent},
                {ScriptedEventType.GwynTombVision, GwynsTombEvent},
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
                {ScriptedEventType.EoL, EoL},
                {ScriptedEventType.DungeonGuardian, DungeonGuardianEvent },
                {ScriptedEventType.DualSandsprogAmbush1, DualSandsprogAmbush1 },


            };

            ScriptedEventValues = new Dictionary<ScriptedEventType, bool>();
            foreach (ScriptedEventType currentEvent in ScriptedEventDict.Keys)
            {
                ScriptedEventValues.Add(currentEvent, false);
            }

            //Add everything to InactiveEvents to start fresh.
            //If the player is NOT loading a fresh world, then this will get wiped later and re-loaded with only the appropriate events.
            EnabledEvents = new List<ScriptedEvent>();
            foreach (KeyValuePair<ScriptedEventType, ScriptedEvent> eventValuePair in ScriptedEventDict)
            {
                EnabledEvents.Add(eventValuePair.Value);
            }

            QueuedEvents = new List<ScriptedEvent>();
            RunningEvents = new List<ScriptedEvent>();
            DisabledEvents = new List<ScriptedEvent>();
            NetworkEvents = new List<NetworkEvent>();
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

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) || NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()) || NPC.AnyNPCs(ModContent.NPCType<MarilithIntro>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool PrimeCustomCondition()
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>())) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.PrimeV2.PrimeIntro>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GwynsTombVisionCustomCondition()
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>())) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Special.GwynBossVision>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool AbyssPortalCustomCondition()
        {
            if (tsorcRevampWorld.SuperHardMode && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Special.AbyssPortal>()) && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))
            {
                return true;
            }
            else
            {
                return false;
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

        public static bool TwinEoWCustomCondition()
        {
            if (NPC.AnyNPCs(NPCID.EaterofWorldsHead) || NPC.AnyNPCs(NPCID.EaterofWorldsBody) || NPC.AnyNPCs(NPCID.EaterofWorldsTail))
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
        public static EventActionStatus ExampleCustomAction(ScriptedEvent thisEvent)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Dust.NewDust(Main.player[i].position, 30, 30, DustID.GreenFairy, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 255);
            }

            if (thisEvent.eventTimer > 900)
            {
                //UsefulFunctions.BroadcastText("The example scripted event ends...", Color.Green);
                return EventActionStatus.CompletedEvent;
            }
            return EventActionStatus.Continue;
        }


        //This is an example artorias custom action. It spawns meteors and displays text every so often, and also changes the projectile damage for Artorias. Most enemies will require a very small change for their projectile damage changes to work (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static EventActionStatus ArtoriasCustomAction(ScriptedEvent thisEvent)
        {
            //Spawning meteors:
            if (Main.rand.NextBool(200))
            {
                //UsefulFunctions.BroadcastText("Artorias rains fire from the Abyss...", Color.Gold);
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Projectile.NewProjectile(new EntitySource_Misc("Scripted Event"), (float)Main.player[i].position.X - 100 + Main.rand.Next(200), (float)Main.player[i].position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), thisEvent.eventNPCs[0].npc.damage / 4, 2f, Main.myPlayer);
                    }
                }
            }

            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Bosses.SuperHardMode.Artorias ourArtorias = (NPCs.Bosses.SuperHardMode.Artorias)thisEvent.eventNPCs[0].npc.ModNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, their damage stats might not be public.
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case blackBreathDamage and phantomSeekerDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourArtorias.blackBreathDamage = 40;
                ourArtorias.phantomSeekerDamage = 50;
            }
            return EventActionStatus.Continue;
        }


        public static EventActionStatus StormCustomAction(ScriptedEvent thisEvent)
        {
            //typeof(Main).GetMethod("StartRain", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            return EventActionStatus.EndAction;
        }

        public static EventActionStatus SetNightCustomAction(ScriptedEvent thisEvent)
        {
            //UsefulFunctions.BroadcastText("Time shifts forward...", Color.Purple);
            Main.dayTime = false;
            Main.time = 0;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
            return EventActionStatus.EndAction;
        }


        //This is an example custom action that just changes the damage of an NPC's projectile. Most enemies will require a very small change for this to work with them (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static EventActionStatus BlackKnightCustomAction(ScriptedEvent thisEvent)
        {
            //Changing projectile damage:
            //First, we make sure the NPC is the one we're talking about. This isn't strictly necessary since we know it should be that one, but it's good practice.
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Enemies.BlackKnight ourKnight = (NPCs.Enemies.BlackKnight)thisEvent.eventNPCs[0].npc.ModNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, the variable that controls the damage for its projectile might not be public (read: probably isn't).
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case spearDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                //ourKnight.redKnightsSpearDamage = 20;
            }
            return EventActionStatus.EndAction;
        }

        //FIRE LURKER PAIN CUSTOM ACTION
        public static EventActionStatus FireLurkerPainCustomAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.FireLurker>())
            {
                NPCs.Enemies.FireLurker ourFireLurker = (NPCs.Enemies.FireLurker)thisEvent.eventNPCs[0].npc.ModNPC;

                ourFireLurker.lostSoulDamage = 16; //was 23, then 13
            }
            return EventActionStatus.EndAction;
        }

        //RED KNIGHT PAIN CUSTOM ACTION
        public static EventActionStatus RedKnightPainCustomAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.RedKnight>())
            {
                NPCs.Enemies.RedKnight ourRedKnightPain = (NPCs.Enemies.RedKnight)thisEvent.eventNPCs[0].npc.ModNPC;
                ourRedKnightPain.redKnightsSpearDamage = 17;
                ourRedKnightPain.redMagicDamage = 13;
                ourRedKnightPain.redKnightsGreatDamage = 15;
            }
            return EventActionStatus.EndAction;
        }

        //RED KNIGHT MOUNTAIN CUSTOM ACTION
        public static EventActionStatus RedKnightMountainCustomAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.RedKnight>())
            {
                NPCs.Enemies.RedKnight ourRedKnight = (NPCs.Enemies.RedKnight)thisEvent.eventNPCs[0].npc.ModNPC;
                ourRedKnight.redKnightsSpearDamage = 15;
                ourRedKnight.redMagicDamage = 11;
                ourRedKnight.redKnightsGreatDamage = 13;
            }
            return EventActionStatus.EndAction;
        }

        public static EventActionStatus UndeadMerchantAction(ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 1686 * 16, 963 * 16, ModContent.NPCType<NPCs.Friendly.UndeadMerchant>());
            return EventActionStatus.CompletedEvent;
        }

        //i dont want this event to last forever, so just spawn the tinkerer and immediately end the event
        //... is what it SHOULD do?
        public static EventActionStatus TinkererAction(ScriptedEvent thisEvent)
        {
            NPC.savedGoblin = true;
            NPC goblinNPC = NPC.NewNPCDirect(new EntitySource_Misc("Scripted Event"), 4456 * 16, 1744 * 16, NPCID.GoblinTinkerer);
            goblinNPC.homeTileX = 4449;
            goblinNPC.homeTileY = 1740;
            return EventActionStatus.CompletedEvent;
        }

        //ALIEN AMBUSH SPAWN DUSTS 
        public static EventActionStatus AlienAmbushAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, thisEvent.centerpoint);
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
            return EventActionStatus.Continue;
        }


        //DUNDLEDING AMBUSH SPAWN DUSTS
        public static EventActionStatus DundledingAmbushAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, thisEvent.centerpoint);
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
            return EventActionStatus.Continue;
        }

        //BOULDERFALL EVENT 1 ACTION

        public static EventActionStatus BoulderfallEvent1Action(ScriptedEvent thisEvent)
        {
            Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(4401 * 16, 895 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropLeft>(), 70, 1);
            return EventActionStatus.CompletedEvent;
        }

        //BOULDERFALL EVENT 2 ACTION

        public static EventActionStatus BoulderfallEvent2Action(ScriptedEvent thisEvent)
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
                return EventActionStatus.CompletedEvent;
            }
            return EventActionStatus.Continue;
        }

        //BOULDERFALL EVENT 3 ACTION

        public static EventActionStatus BoulderfallEvent3Action(ScriptedEvent thisEvent)
        {
            Projectile.NewProjectile(new EntitySource_Misc("ScriptedEvent"), new Vector2(3639 * 16, 349 * 16), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.BoulderDropRight>(), 70, 1);
            return EventActionStatus.CompletedEvent;
        }

        //FIREBOMB HOLLOW AMBUSH SPAWN DUSTS
        public static EventActionStatus FirebombHollowAmbushAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventTimer == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, thisEvent.centerpoint);
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
            return EventActionStatus.Continue;
        }

        public static EventActionStatus MechanicAction(ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 277 * 16, 1366 * 16, NPCID.Mechanic);
            NPC.savedMech = true;
            return EventActionStatus.CompletedEvent;
        }

        public static EventActionStatus WizardAction(ScriptedEvent thisEvent)
        {
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), 7322 * 16, 603 * 16, NPCID.Wizard);
            NPC.savedWizard = true;
            return EventActionStatus.CompletedEvent;
        }

        public static EventActionStatus TwinEoWAction(ScriptedEvent thisEvent)
        {
            bool validPlayer = false;

            if (thisEvent.eventTimer == 0)
            {
                NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)thisEvent.centerpoint.X - 100, (int)thisEvent.centerpoint.Y, NPCID.EaterofWorldsHead);
                NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)thisEvent.centerpoint.X + 100, (int)thisEvent.centerpoint.Y, NPCID.EaterofWorldsHead);
            }
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].ZoneCorrupt && !Main.player[i].dead)
                {
                    validPlayer = true;
                }
            }

            if (!validPlayer)
            {
                return EventActionStatus.FailedEvent;
            }
            else
            {
                if (!NPC.AnyNPCs(NPCID.EaterofWorldsHead) && !NPC.AnyNPCs(NPCID.EaterofWorldsBody) && !NPC.AnyNPCs(NPCID.EaterofWorldsTail))
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && Main.player[i].GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                        {
                            Item.NewItem(new EntitySource_Misc("Scripted Event"), Main.player[i].Center, ModContent.ItemType<Items.EstusFlaskShard>());
                        }
                    }
                }
                return EventActionStatus.Continue;
            }
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
            EnabledEvents = new List<ScriptedEvent>();

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
                    EnabledEvents.Add(eventValuePair.Value);
                }
            }
        }

        //Experimenting with spreading the checks out over a long period so each one isn't running every tick
        //Counts up each time PlayerScriptedEventCheck is called (aka every tick)
        //int tick = 0;
        //How many ticks (plus one) should the checks be spread out over?
        //int tickSpread = 20;
        public static void ScriptedEventCheck()
        {
            RestoreQueuedEvents();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                DrawNetworkEvents(Main.LocalPlayer);
                return;
            }

            for (int index = 0; index < Main.maxPlayers; index++)
            {
                if (!Main.player[index].active)
                {
                    continue;
                }

                //Check if the player is in range of any inactive events
                for (int i = 0; i < EnabledEvents.Count; i++)
                {
                    if (EnabledEvents[i].condition())
                    {
                        float distance = Vector2.DistanceSquared(Main.player[index].position, EnabledEvents[i].centerpoint);

                        if (distance < EnabledEvents[i].radius * 6 && !Main.player[index].dead && EnabledEvents[i].bossEvent && !EnabledEvents[i].disablePeaceCandle)
                        {
                            Main.player[index].AddBuff(BuffID.PeaceCandle, 30, false);
                        }

                        if (!EnabledEvents[i].square)
                        {

                            //If the player is nearby, display some dust to make the region visible to them
                            //This has a Math.Sqrt in it, but that's fine because this code only runs for the handful-at-most events that will be onscreen at a time
                            if (EnabledEvents[i].eventNPCs != null)
                            {
                                if ((EnabledEvents[i].visible && distance < 6000000) || EnabledEvents[i].eventNPCs[0].type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>() && distance < 50000000
                                        || EnabledEvents[i].eventNPCs[0].type == NPCID.HallowBoss && distance < 50000000)
                                {
                                    //Add the event to the list of events that need to be synced to clients. These will be sent to the client once we're done here.
                                    if (Main.netMode == NetmodeID.Server && EnabledEvents[i].visible)
                                    {
                                        bool duplicate = false;
                                        for (int j = 0; j < NetworkEvents.Count; j++)
                                        {
                                            if (NetworkEvents[j].centerpoint == EnabledEvents[i].centerpoint)
                                            {
                                                duplicate = true;
                                            }
                                        }

                                        if (!duplicate)
                                        {
                                            NetworkEvents.Add(new NetworkEvent(EnabledEvents[i].centerpoint, EnabledEvents[i].radius, EnabledEvents[i].dustID, EnabledEvents[i].square, false));
                                        }
                                    }

                                    DrawCircularEvent(EnabledEvents[i].centerpoint, EnabledEvents[i].radius, EnabledEvents[i].dustID, false);
                                }
                            }

                            if (distance < EnabledEvents[i].radius && !Main.player[index].dead)
                            {
                                if (EnabledEvents[i].visible)
                                {
                                    for (int j = 0; j < 100; j++)
                                    {
                                        Dust.NewDustPerfect(EnabledEvents[i].centerpoint, EnabledEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                    }
                                }
                                RunningEvents.Add(EnabledEvents[i]);
                                EnabledEvents.RemoveAt(i);
                            }
                        }
                        //Do the same thing, but square
                        else
                        {
                            if (EnabledEvents[i].visible && distance < 6000000)
                            {
                                bool duplicate = false;
                                for (int j = 0; j < NetworkEvents.Count; j++)
                                {
                                    if (NetworkEvents[j].centerpoint == EnabledEvents[i].centerpoint)
                                    {
                                        duplicate = true;
                                    }
                                }
                                if (!duplicate)
                                {
                                    NetworkEvents.Add(new NetworkEvent(EnabledEvents[i].centerpoint, EnabledEvents[i].radius, EnabledEvents[i].dustID, EnabledEvents[i].square, true));
                                }

                                DrawSquareEvent(EnabledEvents[i].centerpoint, EnabledEvents[i].radius, EnabledEvents[i].dustID, false);
                            }

                            float sqrtRadius = (float)Math.Sqrt(EnabledEvents[i].radius);
                            if (!Main.player[index].dead && (Math.Abs(Main.player[index].position.X - EnabledEvents[i].centerpoint.X) < sqrtRadius) && (Math.Abs(Main.player[index].position.Y - EnabledEvents[i].centerpoint.Y) < sqrtRadius))
                            {
                                for (int j = 0; j < 100; j++)
                                {
                                    Dust.NewDustPerfect(EnabledEvents[i].centerpoint, EnabledEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                }

                                RunningEvents.Add(EnabledEvents[i]);
                                EnabledEvents.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            //Send events that need to be drawn to the clients
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 150 == 0)
            {
                for (int i = 0; i < QueuedEvents.Count; i++)
                {
                    if (QueuedEvents[i].condition())
                    {
                        //Add the network event to the list of events that need to be drawn. These will be sent to the client once we're done here.
                        if (QueuedEvents[i].visible && QueuedEvents[i].eventCooldownTimer < 300)
                        {
                            if (QueuedEvents[i].centerpoint.Y < 2000)
                            {
                                UsefulFunctions.BroadcastText("[DEBUG] Adding broken centerpoint");
                            }

                            bool duplicate = false;
                            for (int j = 0; j < NetworkEvents.Count; j++)
                            {
                                if (NetworkEvents[j].centerpoint == QueuedEvents[i].centerpoint)
                                {
                                    duplicate = true;
                                }
                            }
                            if (!duplicate)
                            {
                                NetworkEvents.Add(new NetworkEvent(QueuedEvents[i].centerpoint, QueuedEvents[i].radius, QueuedEvents[i].dustID, QueuedEvents[i].square, true));
                            }
                        }
                    }
                }

                SendDrawnEvents();
            }

            //Run any active events
            for (int i = 0; i < RunningEvents.Count; i++)
            {
                RunningEvents[i].RunEvent();
            }
        }


        public static void RestoreQueuedEvents()
        {
            //Initialize the list if needed
            if (QueuedEvents == null)
            {
                QueuedEvents = new List<ScriptedEvent>();
            }

            for (int i = QueuedEvents.Count - 1; i >= 0 && QueuedEvents.Count > 0; i--)
            {

                //Do not re-add a queued event if it has been disabled
                if (!IsEventDisabled(QueuedEvents[i]) && QueuedEvents[i].condition() && !QueuedEvents[i].blockedBossEvent)
                {
                    //Wait 5 seconds (300 ticks) and show the player a flashing dust warning before it reactivates
                    if (QueuedEvents[i].eventCooldownTimer > 0)
                    {
                        QueuedEvents[i].eventCooldownTimer--;
                        if (QueuedEvents[i].square)
                        {
                            DrawSquareEvent(QueuedEvents[i].centerpoint, QueuedEvents[i].radius, QueuedEvents[i].dustID, true);
                        }
                        else
                        {
                            DrawCircularEvent(QueuedEvents[i].centerpoint, QueuedEvents[i].radius, QueuedEvents[i].dustID, true);
                        }
                    }
                    else
                    {
                        //Add a delay before the event circle reactivates
                        QueuedEvents[i].eventCooldownTimer = 300;

                        //Longer in multiplayer
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            QueuedEvents[i].eventCooldownTimer = 600;
                        }

                        EnabledEvents.Add(QueuedEvents[i]);
                        QueuedEvents.Remove(QueuedEvents[i]);
                    }
                }
            }
        }

        public static void SendDrawnEvents()
        {
            ModPacket eventPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            eventPacket.Write((byte)tsorcPacketID.SyncEventDust);
            eventPacket.Write(NetworkEvents.Count);
            //UsefulFunctions.BroadcastText("Sending " + NetworkEvents.Count + " event(s)");

            int i = 0;
            foreach (NetworkEvent thisEvent in NetworkEvents)
            {
                //UsefulFunctions.BroadcastText("Sending event:");
                i++;
                eventPacket.WriteVector2(thisEvent.centerpoint);
                //UsefulFunctions.BroadcastText("Centerpoint: " + thisEvent.centerpoint);
                eventPacket.Write((float)thisEvent.radius);
                //UsefulFunctions.BroadcastText("Radius: " + thisEvent.radius);
                eventPacket.Write((int)thisEvent.dustID);
                //UsefulFunctions.BroadcastText("DustID:" + thisEvent.dustID);
                eventPacket.Write(thisEvent.square);
                //UsefulFunctions.BroadcastText("Square:" + thisEvent.square);
                eventPacket.Write(thisEvent.queued);
                //UsefulFunctions.BroadcastText("Queued:" + thisEvent.queued);

                if (thisEvent.queued)
                {
                    //UsefulFunctions.BroadcastText("Sending queued event");
                }
                if (thisEvent.centerpoint.Y < 2000)
                {
                    //UsefulFunctions.BroadcastText("Sending broken centerpoint y " + thisEvent.centerpoint.Y);
                }
            }

            eventPacket.Send();

            NetworkEvents = new List<NetworkEvent>();
        }

        public static void DrawNetworkEvents(Player player)
        {
            //Check if the player is near any networked events and give them the peace candle buff if so
            if (NetworkEvents != null)
            {
                for (int i = NetworkEvents.Count - 1; i >= 0 && NetworkEvents.Count > 0; i--)
                {
                    if (NetworkEvents[i].queued)
                    {
                        //Main.NewText("queued");
                    }
                    float distance = Vector2.DistanceSquared(player.position, NetworkEvents[i].centerpoint);

                    if (!NetworkEvents[i].square)
                    {
                        if (distance < 6000000)
                        {
                            DrawCircularEvent(NetworkEvents[i].centerpoint, NetworkEvents[i].radius, NetworkEvents[i].dustID, NetworkEvents[i].queued);
                        }
                        if (distance < NetworkEvents[i].radius * 6)
                        {
                            player.AddBuff(BuffID.PeaceCandle, 2);
                        }

                        if (distance < NetworkEvents[i].radius && !NetworkEvents[i].queued && !Main.LocalPlayer.dead)
                        {
                            for (int j = 0; j < 100; j++)
                            {
                                Dust.NewDustPerfect(NetworkEvents[i].centerpoint, NetworkEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                            }
                            NetworkEvents.Remove(NetworkEvents[i]);
                        }
                    }
                    //Do the same thing, but square
                    else
                    {
                        float sqrtRadius = (float)Math.Sqrt(NetworkEvents[i].radius);
                        if (distance < 6000000)
                        {
                            DrawSquareEvent(NetworkEvents[i].centerpoint, NetworkEvents[i].radius, NetworkEvents[i].dustID, NetworkEvents[i].queued);
                        }

                        if (!NetworkEvents[i].queued && !Main.LocalPlayer.dead && (Math.Abs(player.position.X - NetworkEvents[i].centerpoint.X) < sqrtRadius) && (Math.Abs(player.position.Y - NetworkEvents[i].centerpoint.Y) < sqrtRadius))
                        {
                            for (int j = 0; j < 100; j++)
                            {
                                Dust.NewDustPerfect(NetworkEvents[i].centerpoint, NetworkEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                            }
                            NetworkEvents.Remove(NetworkEvents[i]);
                        }
                    }
                }
            }

        }

        public static bool IsEventDisabled(ScriptedEvent currentEvent)
        {
            foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
            {
                if (pair.Value == currentEvent)
                {
                    if (ScriptedEventValues[pair.Key] == true)
                    {
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        public static void DrawCircularEvent(Vector2 centerpoint, float radius, int dustID, bool queued = false)
        {
            float sqrtRadius = (float)Math.Sqrt(radius);

            bool EoL = (dustID == DustID.RainbowTorch);

            int dustPerTick = 20;
            if (queued)
            {
                dustPerTick = 1;
                if (Main.GameUpdateCount % 60 == 0)
                {
                    dustPerTick = 150;
                }
            }
            float speed = 2f;
            for (int j = 0; j < dustPerTick; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(sqrtRadius, sqrtRadius);
                Vector2 dustPos = centerpoint + dir;
                if (Collision.CanHit(centerpoint, 0, 0, dustPos, 0, 0) || EoL)
                {
                    Vector2 dustVel = new Vector2(speed, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);

                    Dust thisDust;

                    if (EoL)
                    {
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200, Main.DiscoColor);
                    }
                    else
                    {
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200);
                    }

                    thisDust.noGravity = true;
                }
            }
        }

        public static void DrawSquareEvent(Vector2 centerpoint, float radius, int dustID, bool queued = false)
        {
            float sqrtRadius = (float)Math.Sqrt(radius);

            int dustPerTick = 20;
            float speed = 2f;
            Vector2 dustPos;
            Vector2 dustVel;
            Dust thisDust;
            for (int j = 0; j < dustPerTick; j++)
            {
                int side = Main.rand.Next(0, 4);
                if (side == 0)
                {
                    dustPos = new Vector2(centerpoint.X + sqrtRadius, centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                    if (Collision.CanHit(centerpoint, 0, 0, dustPos, 0, 0))
                    {
                        dustVel = new Vector2(0, speed);
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200);
                        thisDust.noGravity = true;
                    }
                }
                if (side == 1)
                {
                    dustPos = new Vector2(centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), centerpoint.Y + sqrtRadius);
                    if (Collision.CanHit(centerpoint, 0, 0, dustPos, 0, 0))
                    {
                        dustVel = new Vector2(-speed, 0);
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200);
                        thisDust.noGravity = true;
                    }
                }
                if (side == 2)
                {
                    dustPos = new Vector2(centerpoint.X - sqrtRadius, centerpoint.Y + Main.rand.NextFloat(-sqrtRadius, sqrtRadius));
                    if (Collision.CanHit(centerpoint, 0, 0, dustPos, 0, 0))
                    {
                        dustVel = new Vector2(0, -speed);
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200);
                        thisDust.noGravity = true;
                    }
                }
                if (side == 3)
                {
                    dustPos = new Vector2(centerpoint.X + Main.rand.NextFloat(-sqrtRadius, sqrtRadius), centerpoint.Y - sqrtRadius);
                    if (Collision.CanHit(centerpoint, 0, 0, dustPos, 0, 0))
                    {
                        dustVel = new Vector2(speed, 0);
                        thisDust = Dust.NewDustPerfect(dustPos, dustID, dustVel, 200);
                        thisDust.noGravity = true;
                    }
                }
            }

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



        //The list of NPCs spawned by this event
        public List<EventNPC> eventNPCs;

        //Stores which players have not died while this event is active
        public List<int> livingPlayers = new List<int>();

        //Does it have special loot that only drops from the final npc to die? That is done here
        public List<int> FinalNPCCustomDrops;
        public List<int> FinalNPCDropAmounts;

        //The text an event should display
        public string eventText;
        //The color it should display it in
        public Color eventTextColor;
        //The dust an event should use
        public int dustID;
        //Controls whether an event is saved. If not, it will reappear upon either player death or game load.
        public bool save;
        //Controls whether the event's range is made visible to the player with dust
        public bool visible;
        //Controls whether this is a boss event or not
        //If it is, then it will never re-activate while any boss in its spawn list is alive
        //It will also provide a peace candle effect
        bool? checkedBossResult = null;
        public bool bossEvent
        {
            get
            {
                //This means the check only ever has to happen once, and its result is saved
                if (checkedBossResult != null)
                {
                    return checkedBossResult.Value;
                }
                else
                {
                    if (eventNPCs == null || eventNPCs.Count == 0)
                    {
                        checkedBossResult = false;
                        return false;
                    }

                    for (int i = 0; i < eventNPCs.Count; i++)
                    {
                        NPC npc = new NPC();
                        npc.SetDefaults(eventNPCs[i].type);
                        if (npc.boss)
                        {
                            checkedBossResult = true;
                            return true;
                        }
                    }
                }


                checkedBossResult = false;
                return false;
            }
        }

        public bool blockedBossEvent
        {
            get
            {
                if (!bossEvent)
                {
                    return false;
                }

                //Block the event from reappearing if any of the bosses in it are still alive
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].boss)
                    {
                        for (int j = 0; j < eventNPCs.Count; j++)
                        {
                            if (Main.npc[i].type == eventNPCs[j].type)
                            {
                                return true;
                            }
                        }
                    }
                }

                //Or if they have already been slain
                for (int i = 0; i < eventNPCs.Count; i++)
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(eventNPCs[i].type)))
                    {
                        return true;
                    }
                }


                return false;
            }
        }

        //Is this an event that has no associated NPC?
        public bool noNPCEvent
        {
            get
            {
                if (eventNPCs == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

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
        public Func<ScriptedEvent, EventActionStatus> CustomAction = null;

        //Generic multipurpose timer that custom actions can use to time things
        public int eventTimer = 0;

        public int eventCooldownTimer = 300;

        /// <summary>
        /// Prevents the peace candle effect from happening, even if it's a boss
        /// </summary>
        internal bool disablePeaceCandle;

        //This basically just creates a list spawn event with 1 entry
        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, int? npcType = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, bool bossEvent = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<ScriptedEvent, EventActionStatus> customAction = null, bool disablePeaceCandle = false)
        {
            List<int> npcList = null;
            if (npcType != null)
            {
                npcList = new List<int> { npcType.GetValueOrDefault() };
            }

            List<Vector2> npcCoords = null;
            if (npcType != null)
            {
                npcCoords = new List<Vector2> { rangeCenterpoint };
            }

            this.disablePeaceCandle = disablePeaceCandle;

            ConstructScriptedEvent(rangeCenterpoint, rangeRadius, npcList, npcCoords, DustType, saveEvent, visibleRange, bossEvent, flavorText, flavorTextColor, squareRange, customCondition, customAction);
        }

        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs = null, List<Vector2> coords = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, bool bossEvent = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<ScriptedEvent, EventActionStatus> customAction = null)
        {
            ConstructScriptedEvent(rangeCenterpoint, rangeRadius, npcs, coords, DustType, saveEvent, visibleRange, bossEvent, flavorText, flavorTextColor, squareRange, customCondition, customAction);
        }

        public void ConstructScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs = null, List<Vector2> coords = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, bool bossEvent = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<ScriptedEvent, EventActionStatus> customAction = null)
        {
            rangeDetectionMode = true;
            //Player position is stored as 16 times block distances
            centerpoint = rangeCenterpoint * 16;
            //Radius is stored squared, because comparing the squares of distances is WAY faster than comparing their true values
            radius = (float)Math.Pow(rangeRadius * 16, 2);

            if (npcs == null)
            {
                eventNPCs = null;
            }
            else
            {
                eventNPCs = new List<EventNPC>();
                for (int i = 0; i < npcs.Count; i++)
                {
                    eventNPCs.Add(new EventNPC(npcs[i], coords[i]));
                }
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
        }


        public void SetCustomStatsForOne(int npcIndex, int? health = null, int? defense = null, int? damage = null, int? souls = null)
        {
            eventNPCs[npcIndex].customHealth = health;
            eventNPCs[npcIndex].customDefense = defense;
            eventNPCs[npcIndex].customDamage = damage;
            eventNPCs[npcIndex].customSouls = souls;
        }

        public void SetCustomStats(int? health = null, int? defense = null, int? damage = null, int? souls = null)
        {
            for (int i = 0; i < eventNPCs.Count; i++)
            {
                eventNPCs[i].customHealth = health;
                eventNPCs[i].customDefense = defense;
                eventNPCs[i].customDamage = damage;
                eventNPCs[i].customSouls = souls;
            }
        }

        public void SetCustomDropsForOne(List<int> dropIDs, List<int> dropStackSizes, int npcIndex)
        {
            eventNPCs[npcIndex].extraLootItems = dropIDs;
            eventNPCs[npcIndex].extraLootAmounts = dropStackSizes;
        }

        public void SetCustomDrops(List<int> dropIDs, List<int> dropStackSizes, bool dropForFinalNPCOnly = true)
        {
            if (dropForFinalNPCOnly)
            {
                FinalNPCCustomDrops = dropIDs;
                FinalNPCDropAmounts = dropStackSizes;
            }
            else
            {
                for (int i = 0; i < eventNPCs.Count; i++)
                {
                    eventNPCs[i].extraLootItems = dropIDs;
                    eventNPCs[i].extraLootAmounts = dropStackSizes;
                }
            }
        }

        //Runs the event
        public void RunEvent()
        {
            //If this is its first time running, spawn the NPC's and display the text
            if (eventTimer == 0)
            {
                if (!noNPCEvent)
                {
                    SpawnNPCs();
                }
                if (eventText != "default")
                {
                    UsefulFunctions.BroadcastText(eventText, eventTextColor);
                }
            }

            if (livingPlayers == null || livingPlayers.Count == 0)
            {
                livingPlayers = new List<int>();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active)
                    {
                        livingPlayers.Add(i);
                    }
                }
            }

            //If it has a custom action, then run it
            //If it returns EndAction, mark its action as finished and do not run it again
            //If it returns FailedEvent then immediately mark the event as failed and end it
            //If it returns CompletedEvent then immediately mark the event as completed and end it
            if (hasCustomAction && !finishedCustomAction)
            {
                EventActionStatus status = CustomAction(this);
                if (status == EventActionStatus.EndAction)
                {
                    finishedCustomAction = true;
                }
                if (status == EventActionStatus.FailedEvent)
                {
                    EndEvent(false);
                    return;
                }
                if (status == EventActionStatus.CompletedEvent)
                {
                    EndEvent(true);
                    return;
                }
            }

            //Updates timer *after* running actions
            eventTimer++;

            //Only perform these checks if an event has NPCs
            //No NPC events must be ended by their actions
            if (!noNPCEvent)
            {
                if (!bossEvent)
                {
                    //Check if every player on the livingPlayers list is still alive
                    for (int i = livingPlayers.Count - 1; i >= 0; i--)
                    {
                        //If any player is alive, do nothing
                        if (Main.player[livingPlayers[i]].active && Main.player[livingPlayers[i]].dead)
                        {
                            livingPlayers.RemoveAt(i);
                        }
                    }

                    //If none are, then the event is failed.
                    if (livingPlayers.Count == 0)
                    {
                        EndEvent(false);
                        return;
                    }
                }

                //If the NPC is dead or if the custom action set endEvent to true, remove it from active events
                //If so, and this is marked as an event that should be saved, then do so by getting the key for this event and marking it as finished in ScriptedEventValues
                //Otherwise add it back to InactiveEvents
                bool oneAlive = false;
                for (int i = 0; i < eventNPCs.Count; i++)
                {
                    if (eventNPCs[i].killed == false)
                    {
                        //If it's not marked as killed by a player, is indeed alive, and is the proper type then the event isn't over
                        //(The type check is to ensure the index of the NPC was not replaced with another)
                        if (eventNPCs[i].npc.active && eventNPCs[i].npc.type == eventNPCs[i].type)
                        {
                            oneAlive = true;
                        }
                        else
                        {
                            //If they aren't marked as killed by a player, but also are dead or the wrong type, then they despawned. End the event as failed.
                            EndEvent(false);
                            return;
                        }
                    }
                }

                //If none are alive, and none despawned, then they have all been killed by the player. End the event as a success.
                if (!oneAlive)
                {
                    EndEvent(true);
                }
            }
        }

        public void SpawnNPCs()
        {
            for (int i = 0; i < eventNPCs.Count; i++)
            {
                eventNPCs[i].index = NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)eventNPCs[i].spawnCoords.X * 16, (int)eventNPCs[i].spawnCoords.Y * 16, eventNPCs[i].type);

                NPC thisNPC = eventNPCs[i].npc;

                thisNPC.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().ScriptedEventOwner = this;
                thisNPC.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().ScriptedEventIndex = i;

                if (eventNPCs[i].customHealth != null)
                {
                    thisNPC.lifeMax = eventNPCs[i].customHealth.Value;
                    thisNPC.life = eventNPCs[i].customHealth.Value;
                }
                if (eventNPCs[i].customDefense != null)
                {
                    thisNPC.defense = eventNPCs[i].customDefense.Value;
                }
                if (eventNPCs[i].customDamage != null)
                {
                    thisNPC.damage = eventNPCs[i].customDamage.Value;
                }
                if (eventNPCs[i].customSouls != null)
                {
                    if (Main.expertMode)
                    {
                        eventNPCs[i].npc.value = eventNPCs[i].customSouls.Value * 25;
                    }
                    else
                    {
                        eventNPCs[i].npc.value = eventNPCs[i].customSouls.Value * 10;
                    }
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    UsefulFunctions.SyncNPCExtraStats(eventNPCs[i].npc);
                }
            }
        }

        public void EndEvent(bool eventCompleted)
        {

            //UsefulFunctions.BroadcastText("Ending event with status " + eventCompleted);
            //Save the event if it's marked as a saved event and it is 'completed' (either by a customaction forcibly ending it, or by all the NPC's being killed)
            if (eventCompleted)
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
                    tsorcScriptedEvents.QueuedEvents.Add(this);
                }
            }
            //Otherwise if it wasn't completed, then despawn the NPC's and re-add it to DisabledEvents to be re-initialized once the player respawns
            else
            {
                tsorcScriptedEvents.QueuedEvents.Add(this);
                if (!noNPCEvent)
                {
                    if (eventNPCs.Count > 0)
                    {
                        foreach (EventNPC thisEventNPC in eventNPCs)
                        {
                            NPC thisNPC = thisEventNPC.npc;
                            if (thisNPC.active && thisNPC.type == thisEventNPC.type && !thisNPC.boss)
                            {
                                //UsefulFunctions.BroadcastText("[DEBUG] Event failed, despawning NPC");
                                thisNPC.active = false;
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, thisNPC.whoAmI);
                                for (int i = 0; i < 60; i++)
                                {
                                    Dust.NewDustDirect(thisNPC.position, thisNPC.width, thisNPC.height, dustID, Main.rand.Next(-5, 5), Main.rand.Next(-12, 12), 150, default, 3f).noGravity = true;
                                }
                            }
                        }
                    }
                }
            }

            tsorcScriptedEvents.RunningEvents.Remove(this);
            livingPlayers = null;

            eventTimer = 0;
            if (!noNPCEvent)
            {
                for (int i = 0; i < eventNPCs.Count; i++)
                {
                    eventNPCs[i].killed = false;
                }
            }

            finishedCustomAction = false;
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

        //Is it a queued event?
        public bool queued;

        public NetworkEvent(Vector2 position, float range, int DustType, bool squareRange, bool queuedEvent)
        {
            if (position.Y < 2000)
            {
                UsefulFunctions.BroadcastText("Broken center");
            }
            centerpoint = position;
            radius = range;
            dustID = DustType;
            square = squareRange;
            queued = queuedEvent;
        }
    }

    public class EventNPC
    {
        //The type of the NPC
        public int type;

        //The index of the NPC in the main array
        public int index;

        //Whether it has been killed
        public bool killed;

        //Where it should spawn
        public Vector2 spawnCoords;

        //Extra loot it should drop
        public List<int> extraLootItems;
        public List<int> extraLootAmounts;

        //Custom stats it should have
        public int? customHealth;
        public int? customDamage;
        public int? customDefense;
        public int? customSouls;

        public NPC npc
        {
            get
            {
                return Main.npc[index];
            }
        }

        public EventNPC(int type, Vector2 coords)
        {
            this.type = type;
            spawnCoords = coords;
        }
    }

    public enum EventActionStatus
    {
        Continue,
        EndAction,
        FailedEvent,
        CompletedEvent
    }

}