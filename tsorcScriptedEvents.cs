using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
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
         * That value MUST be changed via scripting instead. KnightOfGwynCustomAction shows an example of this.
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
        private static int GetThoriumNPCType(string name) =>
        ModLoader.TryGetMod("ThoriumMod", out Mod thorium)
            ? thorium.Find<ModNPC>(name).Type
            : NPCID.None;

        // Named method (not lambda) so ev.condition.Method.Name is stable and reflection can re-create it on load.
        public static bool ThoriumActiveCondition() => ModLoader.HasMod("ThoriumMod");
        private static readonly Func<bool> ThoriumActive = ThoriumActiveCondition;
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
            GoblinSharkTropicalIsland,
            GreatRedKnightInDesert,
            AncestralSpiritEvent,
            SkeletronHidden,
            AlienAmbush,
            EoC,
            EoW1,
            AncientDemon,
            LichKing,
            LichKingRemix,
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
            BlackKnightPain,
            RedKnightTwinMountain,
            JungleWyvernFight,
            SeathFight,
            WyvernMageFight,
            SlograAndGaibonFight,
            SerrisFight,
            MarilithFight,
            RemixMarilithFight,
            PrimeFight,
            KrakenFight,
            GwynTombVision,
            AbyssPortal,
            GwynFight,
            RemixGwynFight,
            AbysmalOolacileSorcererFight,
            RemixAbysmalOolacileSorcererFight,
            WitchkingFight,
            RemixWitchkingEvent,
            WyvernMageShadowFight,
            ChaosFight,
            ChaosEventRemix,
            BlightFight,
            DarkCloudPyramidFight,
            DarkCloudEventRemix,
            ArtoriasFight,
            BlackKnightCity,
            //ExampleHarpySwarm,
            //ExampleNoNPCScriptEvent,
            SpawnUndeadMerchant,
            SpawnGoblin,
            AttraidiesTheSorrowEvent,
            TwinEoWFight,
            DunledingAmbush,
            RemixDunledingAmbush,
            BoulderfallEvent1,
            BoulderfallEvent2,
            BoulderfallEvent3,
            FirebombHollowAmbush,
            LeonhardPhase1Event,
            LeonhardRemixEvent,
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
            RemixEoL,
            DungeonGuardian,
            OldManEvent,
            DualSandsprogAmbush1,
            DrownedAmbush1,
            DrownedAmbush2,
            MushroomCavern,
            AshCavernLeftside,
            AshCavernRightside,
            MorgulFelegLeftside,
            MorgulFelegRightside,
            HallowDemonEvent,
            ShadowTempleEvent,
            ShadowTempleEvent2,
            MoltenSkyTempleEvent,
            MoltenSkyTempleEvent2,
            SandstormElementalEvent,
            KingSlime2Event,
            AbysswalkerEvent,
            BloodLakeEvent,
            BloodBossEvent1,
            BloodBossEvent2,
            BloodBossEvent3,
            TwinsEvent,
            CatacombsEvent,
            FoundryEvent,
            FoundryEvent2,
            FrozenCathedralEvent,
            EnragedQB,
            Lunatic,
            IceGolemIsland,
            AncestralSpiritRemixEvent,
            FrozenCathedralEvent2,
            WingTrioEvent,
            SkeletronPrimeEvent,
            WyvernPrisonEvent,
            SandstormElementalEvent2,
            DeathRemix,
            DiscipleOfAttraidiesEvent,
            WyvernFortressEvent,
            SpawnLonelyFairy,
            Dutchman,
            //THORIUM COMPATIBILITY UNIQUE EVENTS
            ThunderBird,
            ThunderBird2,
            QueenJellyfish,
            Viscount,
            StarScouter,
            BoreanStrider,
            Lich,
            ForgottenOne,
            //ShadowDarkCloud1,
            //ShadowDarkCloud2,
            //ShadowDarkCloud3,
            //ShadowDarkCloud4,
            ShadowDarkCloud5


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
            ScriptedEvent Pinwheel = new ScriptedEvent(new Vector2(4139f, 923), 15, ModContent.NPCType<NPCs.Bosses.Pinwheel.Pinwheel>(), DustID.Asphalt, true, true, true, LangUtils.GetTextValue("Events.Pinwheel"), Color.Firebrick, false);

            //LOTHRIC BLACK KNIGHT IN CATACOMBS OF THE DROWNED
            ScriptedEvent LothricKnightCatacombs = new ScriptedEvent(new Vector2(4137, 895), 10, ModContent.NPCType<NPCs.Enemies.LothricBlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, LothricBlackKnightCustomAction);
            LothricKnightCatacombs.SetCustomStats(1100, 8, 40, 1500);

            //FIRELURKER AMBUSH 1 - Path of Ambition main room
            List<int> FireLurkerAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.FireLurker>(), ModContent.NPCType<NPCs.Enemies.FireLurker>() };
            List<Vector2> FireLurkerAmbush1EnemyLocations = new List<Vector2>() { new Vector2(3559, 1248), new Vector2(3629, 1248) };
            ScriptedEvent FireLurkerAmbush1 = new ScriptedEvent(new Vector2(3591, 1248), 6, FireLurkerAmbush1EnemyTypeList, FireLurkerAmbush1EnemyLocations, DustID.DungeonWater, true, false, false, LangUtils.GetTextValue("Events.FireLurker"), Color.Red, false, default, FireLurkerPainCustomAction);
            FireLurkerAmbush1.SetCustomStats(500, 12, 70, 650);
            FireLurkerAmbush1.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.GreenBlossom>() }, new List<int>() { 5 }, true);

            //DEATH
            ScriptedEvent Death = new ScriptedEvent(new Vector2(1066, 529), 30, ModContent.NPCType<NPCs.Bosses.Death>(), DustID.BoneTorch, true, true, true, LangUtils.GetTextValue("Events.Death"), Color.Black, false, OnlyAdventureMapCondition);

            ScriptedEvent DeathRemix = new ScriptedEvent(new Vector2(8091, 557), 30, ModContent.NPCType<NPCs.Bosses.Death>(), DustID.BoneTorch, true, true, true, LangUtils.GetTextValue("Events.Death"), Color.Black, false, RemixMapCondition);

            //BLACK KNIGHT IN BLUE SHM DUNGEON
            ScriptedEvent BlackKnightSHMDungeon = new ScriptedEvent(new Vector2(2282, 1650), 30, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightSHMDungeon.SetCustomStats(25000, 30, 140, 16000);
            BlackKnightSHMDungeon.SetCustomDrops(new List<int>() { ModContent.ItemType<SoulCoin>(), ModContent.ItemType<PurgingStone>() }, new List<int>() { 50, 1 });

            //RED KNIGHT IN OOLICILE FOREST
            ScriptedEvent RedKnightOolicileForest = new ScriptedEvent(new Vector2(5596, 926), 10, ModContent.NPCType<NPCs.Enemies.RedKnight>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.RedKnight2"), Color.Purple, false, default, RedKnightMountainCustomAction);
            RedKnightOolicileForest.SetCustomDrops(new List<int>() { ItemID.GreaterHealingPotion, ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<SoulCoin>() }, new List<int>() { 4, 3, 2, 50 });
            RedKnightOolicileForest.SetCustomStats(1000, 9, 55, 1250);

            //BLACK KNIGHT IN HALLOWED CAVES
            ScriptedEvent BlackKnightHallowed = new ScriptedEvent(new Vector2(7454, 1413), 40, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, false, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightHallowed.SetCustomStats(6000, 20, 80, 5000);
            BlackKnightHallowed.SetCustomDrops(new List<int>() { ModContent.ItemType<SoulCoin>(), ModContent.ItemType<PurgingStone>() }, new List<int>() { 50, 1 });

            //QUEEN SLIME
            ScriptedEvent QueenSlimeEvent = new ScriptedEvent(new Vector2(7059, 1289), 25, NPCID.QueenSlimeBoss, DustID.HallowedTorch, true, true, true, LangUtils.GetTextValue("Events.QueenSlime"), Color.Pink, false);

            //GREAT RED KNIGHT IN DESERT
            ScriptedEvent GreatRedKnightInDesert = new ScriptedEvent(new Vector2(2229, 856), 100, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GreatRedKnight>(), DustID.Shadowflame, true, false, true, LangUtils.GetTextValue("Events.GreatRedKnightInvasion"), Color.Red, false, SuperHardModeCustomCondition);
            GreatRedKnightInDesert.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<Humanity>() }, new List<int>() { 2, 2, 2 });
            GreatRedKnightInDesert.SetCustomStats(null, null, null, 20000);

            //Ancestral Spirit
            ScriptedEvent AncestralSpiritEvent = new ScriptedEvent(new Vector2(4043, 143), 30, NPCID.Deerclops, DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AncestralSpirit"), Color.Blue, false, OnlyAdventureMapCondition);

            ScriptedEvent AncestralSpiritRemixEvent = new ScriptedEvent(new Vector2(7344, 768), 30, NPCID.Deerclops, DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AncestralSpirit"), Color.Cyan, false, RemixMapCondition);
            //SkeletronHidden
            // Guarded the same way as OldManEvent (which also leads to Skeletron): without this, killing Skeletron
            // via the OTHER route (e.g. the Old Man arena) never "completes" THIS event from its own bookkeeping
            // perspective, so it kept re-triggering and re-spawning a redundant Skeletron indefinitely even though
            // the boss was already down for the world.
            ScriptedEvent SkeletronHiddenEvent = new ScriptedEvent(new Vector2(5563, 1676), 16, NPCID.SkeletronHead, 181, true, true, true, LangUtils.GetTextValue("Events.SkeletronHidden"), Color.Violet, false, SkeletronHiddenSpawnCondition, SetNightCustomAction);

            //SkeletronHidden
            ScriptedEvent OldManEvent = new ScriptedEvent(new Vector2(4979, 1398), 64, NPCID.OldMan, DustID.WhiteTorch, true, true, true, "default", Color.White, false, OldManSpawnCondition);

            //EoC
            ScriptedEvent EoCEvent = new ScriptedEvent(new Vector2(3900, 1138), 20, NPCID.EyeofCthulhu, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.EoC"), Color.Blue, false, null, SetNightCustomAction);

            //EoW1
            ScriptedEvent EoW1Event = new ScriptedEvent(new Vector2(3633, 996), 46, NPCID.EaterofWorldsHead, DustID.Shadowflame, false, true, true, LangUtils.GetTextValue("Events.EoW"), Color.Purple, false, PreEoWCustomCondition);

            //EMPRESS OF LIGHT
            ScriptedEvent EoL = new ScriptedEvent(new Vector2(4484, 350), 100, NPCID.HallowBoss, DustID.HallowedTorch, false, true, true, LangUtils.GetTextValue("Events.EoL"), Color.Pink, false, EoLDownedCondition);

            ScriptedEvent Lunatic = new ScriptedEvent(new Vector2(171, 210), 40, NPCID.CultistBoss, 15, false, true, true, LangUtils.GetTextValue("Events.Lunatic"), Color.Cyan, false, RemixMapCondition);
            Lunatic.SetCustomDrops(new List<int>() { ItemID.CelestialSigil }, new List<int>() {1});
            //LICH KING
            ScriptedEvent LichKing = new ScriptedEvent(new Vector2(364, 1897), 40, ModContent.NPCType<EarthFiendLich>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.LichKing"), Color.Gold, false, OnlyAdventureMapCondition);

            //LICH KING REMIX !!
            ScriptedEvent LichKingRemix = new ScriptedEvent(new Vector2(3152, 1685), 40, ModContent.NPCType<EarthFiendLich>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.LichKing"), Color.Gold, false, RemixMapCondition);

            //THE HUNTER
            ScriptedEvent TheHunter = new ScriptedEvent(new Vector2(296, 1560), 36, ModContent.NPCType<NPCs.Bosses.TheHunter>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Hunter"), Color.DarkGreen, false);

            //THE RAGE
            ScriptedEvent TheRage = new ScriptedEvent(new Vector2(7000, 1845), 30, ModContent.NPCType<NPCs.Bosses.TheRage>(), DustID.Torch, true, true, true, LangUtils.GetTextValue("Events.Rage"), Color.Red, false);

            //DEFILED DEMON (FORGOTTEN CITY, CLOSE TO FIRE TEMPLE)
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
            ScriptedEvent Golem2 = new ScriptedEvent(new Vector2(7900, 868), 30, Golem2EnemyTypeList, Golem2EnemyLocations, DustID.Snow, true, false, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.BlueViolet, false, OnlyAdventureMapCondition, StormCustomAction); //

            //ICE GOLEM - FROZEN OCEAN
            ScriptedEvent IceGolemEvent = new ScriptedEvent(new Vector2(7651, 1020), 20, NPCID.IceGolem, DustID.MagicMirror, true, true, false, LangUtils.GetTextValue("Events.IceGolem"), Color.Blue, false);

            //KING SLIME
            ScriptedEvent KingSlimeEvent = new ScriptedEvent(new Vector2(5995, 1117), 20, NPCID.KingSlime, DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.KingSlime"), Color.Blue, false);

            //HERO OF LUMELIA FIGHT
            ScriptedEvent HeroofLumeliaFight = new ScriptedEvent(new Vector2(2108, 849), 60, ModContent.NPCType<NPCs.Bosses.HeroofLumelia>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.HeroOfLumelia"), Color.LightGoldenrodYellow, false, LumeliaCustomCondition);//location previously was 4413, 717, near village

            //FIRE LURKER PATH OF PAIN
            ScriptedEvent FireLurkerPain = new ScriptedEvent(new Vector2(3245, 1252), 9, ModContent.NPCType<NPCs.Enemies.FireLurker>(), DustID.CursedTorch, true, true, true, LangUtils.GetTextValue("Events.FireLurker"), Color.Purple, false, default, FireLurkerPainCustomAction);
            FireLurkerPain.SetCustomStats(1800, 12, 85, 1500);
            FireLurkerPain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion }, new List<int>() { 3, 4 });

            //RED KNIGHT IN PATH OF PAIN
            ScriptedEvent BlackKnightPain = new ScriptedEvent(new Vector2(3897, 1219), 20, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), 27, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightPainCustomAction);
            BlackKnightPain.SetCustomDrops(new List<int>() { ItemID.RagePotion, ItemID.WrathPotion, ModContent.ItemType<WorldRune>() }, new List<int>() { 2, 3, 4 });
            BlackKnightPain.SetCustomStats(3560, 15, 70, 3550);

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
            List<Vector2> SoggyandGaibonLocations = new List<Vector2>() { new Vector2(6192, 1267), new Vector2(6192, 1267) };
            ScriptedEvent SlograAndGaibonEvent = new ScriptedEvent(new Vector2(6192, 1267), 30, SoggyandGaibonEnemyTypeList, SoggyandGaibonLocations, DustID.Shadowflame, false, true, true, LangUtils.GetTextValue("Events.SlograAndGaibon"), Color.Purple, false, SlograGaibonCondition);
            //SERRIS
            //Like Slogra and Gaibon, this one works a little different due to spawning two bosses.
            List<int> SerrisEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>() };
            List<Vector2> SerrisEnemyLocations = new List<Vector2>() { new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(MathHelper.Pi / 3), new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(-MathHelper.Pi / 3), new Vector2(1136, 956) + new Vector2(100, 0).RotatedBy(MathHelper.Pi) };
            ScriptedEvent SerrisEvent = new ScriptedEvent(new Vector2(1136, 956), 30, SerrisEnemyTypeList, SerrisEnemyLocations, DustID.FireworkFountain_Blue, false, true, true, LangUtils.GetTextValue("Events.Serris"), Color.Blue, false, SerrisCustomCondition);

            //MARILITH 
            ScriptedEvent MarilithEvent = new ScriptedEvent(new Vector2(3235, 1770), 100, ModContent.NPCType<MarilithIntro>(), DustID.RedTorch, false, true, true, LangUtils.GetTextValue("Events.Marilith"), Color.Red, false, MarilithCustomCondition, disablePeaceCandle: true);

            ScriptedEvent RemixMarilithEvent = new ScriptedEvent(new Vector2(305, 1914), 100, ModContent.NPCType<MarilithIntro>(), DustID.RedTorch, false, true, true, LangUtils.GetTextValue("Events.Marilith"), Color.Red, false, RemixMarilithCustomCondition, disablePeaceCandle: true);

            //SKELETRON PRIME
            ScriptedEvent PrimeEvent = new ScriptedEvent(new Vector2(5090, 1103), 75, ModContent.NPCType<NPCs.Bosses.PrimeV2.PrimeIntro>(), DustID.RedTorch, false, false, true, LangUtils.GetTextValue("Events.TheMachine"), Color.Gray, false, PrimeCustomCondition);

            //KRAKEN
            ScriptedEvent KrakenEvent = new ScriptedEvent(new Vector2(1821, 1702), 30, ModContent.NPCType<WaterFiendKraken>(), DustID.MagicMirror, true, true, true, LangUtils.GetTextValue("Events.WaterFiendKraken"), Color.Blue, false, SuperHardModeCustomCondition);

            //GWYN's TOMB VISIONS
            ScriptedEvent GwynsTombEvent = new ScriptedEvent(new Vector2(670, 1164), 150, ModContent.NPCType<NPCs.Special.GwynBossVision>(), DustID.RedTorch, false, true, true, LangUtils.GetTextValue("Events.GwynTombVisions"), default, false, GwynsTombVisionCustomCondition);

            //ABYSS PORTAL
            ScriptedEvent AbyssPortalEvent = new ScriptedEvent(new Vector2(670, 1164), 9999999, ModContent.NPCType<NPCs.Special.AbyssPortal>(), DustID.RedTorch, false, false, false, LangUtils.GetTextValue("Events.AbyssPortal"), default, false, AbyssPortalCustomCondition);

            //GWYN
            ScriptedEvent GwynEvent = new ScriptedEvent(new Vector2(832, 1244), 16, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.Gwyn"), Color.Red, false, OnlyAdventureMapCondition);
            ScriptedEvent RemixGwynEvent = new ScriptedEvent(new Vector2(822, 1241), 16, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.Gwyn"), Color.Red, false, RemixMapCondition);

            //ABYSMAL OOLACILE SORCERER
            ScriptedEvent AbysmalOolacileSorcererEvent = new ScriptedEvent(new Vector2(6721, 1905), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(), DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AbysmalOolacileSorcerer"), Color.Red, false, OnlyAdventureMapCondition);

            ScriptedEvent RemixAbysmalOolacileSorcererEvent = new ScriptedEvent(new Vector2(8239, 1870), 40, ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(), DustID.Shadowflame, true, true, true, LangUtils.GetTextValue("Events.AbysmalOolacileSorcerer"), Color.Red, false, RemixMapCondition);

            //WITCHKING
            ScriptedEvent WitchkingEvent = new ScriptedEvent(new Vector2(2484, 1795), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.Witchking"), Color.Red, false, OnlyAdventureMapCondition);

            ScriptedEvent RemixWitchkingEvent = new ScriptedEvent(new Vector2(2487, 1803), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(), DustID.CursedTorch, true, true, true, LangUtils.GetTextValue("Events.Witchking"), Color.Green, false, RemixMapCondition);

            //BLIGHT
            ScriptedEvent BlightEvent = new ScriptedEvent(new Vector2(8174, 866), 35, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(), DustID.IceTorch, true, true, true, LangUtils.GetTextValue("Events.Blight"), Color.Blue, false, SuperHardModeCustomCondition, RainCustomAction);
            //BlightEvent.SetCustomStats(50000, 30, 50);

            //CHAOS
            ScriptedEvent ChaosEvent = new ScriptedEvent(new Vector2(6415, 1888), 50, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Chaos"), Color.Red, false, OnlyAdventureMapCondition);

            ScriptedEvent ChaosEventRemix = new ScriptedEvent(new Vector2(7034, 968), 50, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.Chaos"), Color.Red, false, RemixMapCondition);

            //WYVERN MAGE SHADOW-SHM
            ScriptedEvent WyvernMageShadowEvent = new ScriptedEvent(new Vector2(6432, 196), 25, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), DustID.CrimsonTorch, true, true, true, LangUtils.GetTextValue("Events.WyvernMageShadow"), Color.OrangeRed, false, SuperHardModeCustomCondition);

            //DARK CLOUD
            ScriptedEvent DarkCloudEvent = new ScriptedEvent(new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.DarkCloud"), Color.LightCyan, false, OnlyAdventureMapConditionSHM);

            //DARK CLOUD REMIX !!
            ScriptedEvent DarkCloudEventRemix = new ScriptedEvent(new Vector2(6500, 1858), 50, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.DarkCloud"), Color.LightCyan, false, RemixMapConditionSHM);

            //ARTORIAS
            ScriptedEvent ArtoriasEvent = new ScriptedEvent(new Vector2(5344, 1692), 15, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), DustID.GoldFlame, true, true, true, LangUtils.GetTextValue("Events.Artorias"), Color.Gold, false, SuperHardModeCustomCondition);
            //ArtoriasEvent.SetCustomDrops(new List<int>() { ItemID.RodofDiscord, ModContent.ItemType<Items.DestructionElement>() }, new List<int>() { 1, 4 });

            //BLACK KNIGHT IN FORGOTTEN CITY
            ScriptedEvent BlackKnightCity = new ScriptedEvent(new Vector2(4508, 1745), 20, ModContent.NPCType<NPCs.Enemies.BlackKnight>(), DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.BlackKnight"), Color.Purple, false, default, BlackKnightCustomAction);
            BlackKnightCity.SetCustomStats(3000, 10, 60, 3500);
            BlackKnightCity.SetCustomDrops(new List<int>() { ModContent.ItemType<SoulCoin>(), ModContent.ItemType<PurgingStone>() }, new List<int>() { 50, 1 });

            //ATTRAIDIES THE SORROW EVENT
            ScriptedEvent AttraidiesTheSorrowEvent = new ScriptedEvent(new Vector2(8216.5f, 1630), 30, ModContent.NPCType<NPCs.Special.AttraidiesApparition>(), DustID.ShadowbeamStaff, false, true, true, LangUtils.GetTextValue("Events.SorrowAttraidies"), Color.OrangeRed, false, AttraidiesTheSorrowCondition);

            //TWIN EATER OF WORLDS FIGHT
            ScriptedEvent TwinEoWFight = new ScriptedEvent(new Vector2(3245, 1215), 15, default, DustID.ShadowbeamStaff, true, true, true, LangUtils.GetTextValue("Events.TwinEaters"), Color.Purple, false, TwinEoWCustomCondition, TwinEoWAction);

            //DUNLENDING AMBUSH
            List<int> DunledingAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>() };
            List<Vector2> DunledingAmbushEnemyLocations = new List<Vector2>() { new Vector2(4697, 858), new Vector2(4645, 858), new Vector2(4645, 841) };
            ScriptedEvent DunledingAmbush = new ScriptedEvent(new Vector2(4666, 856), 10, DunledingAmbushEnemyTypeList, DunledingAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.DunlendingAmbush"), Color.Red, false, OnlyAdventureMapCondition, DundledingAmbushAction);
            if (Main.netMode == NetmodeID.SinglePlayer && Main.expertMode)
            {
                DunledingAmbush.SetCustomStats((int?)(player.statLifeMax2 * .5f), null, (int?)(player.statLifeMax2 * 0.10f) + 25); //damage doesn't double for Expert
            }
            DunledingAmbush.SetCustomDrops(new List<int>() { ItemID.LifeCrystal }, new List<int>() { 1 }, true); //was DodgerollMemo — its contents are now the Game Manual's dodge roll page

            List<int> RemixDunledingAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>(), ModContent.NPCType<NPCs.Enemies.Dunlending>() };
            List<Vector2> RemixDunledingAmbushEnemyLocations = new List<Vector2>() { new Vector2(4691, 854), new Vector2(4733, 854), new Vector2(4698, 853) };
            ScriptedEvent RemixDunledingAmbush = new ScriptedEvent(new Vector2(4715, 853), 7, RemixDunledingAmbushEnemyTypeList, RemixDunledingAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.DunlendingAmbush"), Color.Red, false, RemixMapCondition, DundledingAmbushAction);
            if (Main.netMode == NetmodeID.SinglePlayer && Main.expertMode)
            {
                RemixDunledingAmbush.SetCustomStats((int?)(player.statLifeMax2 * .5f), null, (int?)(player.statLifeMax2 * 0.10f) + 25); //damage doesn't double for Expert
            }
            RemixDunledingAmbush.SetCustomDrops(new List<int>() { ItemID.LifeCrystal }, new List<int>() { 1 }, true); //was DodgerollMemo — its contents are now the Game Manual's dodge roll page


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
            
            ScriptedEvent SpawnLonelyFairy = new ScriptedEvent(new Vector2(7707, 1161), 50, default, 31, true, true, false, "", default, false, FairyCondition, FairyAction);

            //BOULDERFALL EVENT 1 - EARTH TEMPLE ENTRANCE
            ScriptedEvent BoulderfallEvent1 = new ScriptedEvent(new Vector2(4378, 922), 6, default, default, true, false, false, "", default, false, default, BoulderfallEvent1Action);

            //BOULDERFALL EVENT 2 - BLUE DUNGEON BRICK PARKOUR ROOM IN MOUNTAIN
            ScriptedEvent BoulderfallEvent2 = new ScriptedEvent(new Vector2(3518, 429), 2, default, default, true, false, false, "", default, false, default, BoulderfallEvent2Action);

            //BOULDERFALL EVENT 3 - TWIN PEAK RIGHTMOST ENTRANCE
            ScriptedEvent BoulderfallEvent3 = new ScriptedEvent(new Vector2(3665, 360), 6, default, default, true, false, false, "", default, false, default, BoulderfallEvent3Action);

            //FIREBOMB HOLLOW AMBUSH - ON BRIDGE AT TWIN PEAKS - ONLY ONCE
            List<int> FirebombHollowAmbushEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowSpearman>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> FirebombHollowAmbushEnemyLocations = new List<Vector2>() { new Vector2(3386, 367), new Vector2(3451, 367) };
            ScriptedEvent FirebombHollowAmbush = new ScriptedEvent(new Vector2(3418, 364), 10, FirebombHollowAmbushEnemyTypeList, FirebombHollowAmbushEnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.FirebombHollowAmbush"), Color.Red, false, default, FirebombHollowAmbushAction);

            //LEONHARD PHASE 1 EVENT - BY ADAMANTITE GATE ACROSS BRIDGE FROM WIZARDS HOUSE
            ScriptedEvent LeonhardPhase1Event = new ScriptedEvent(new Vector2(3314, 355), 34, ModContent.NPCType<NPCs.Special.LeonhardPhase1>(), 54, true, false, true, LangUtils.GetTextValue("Events.Leonhard1"), Color.Red, false, LeonhardPhase1Undefeated);

            //ScriptedEvent LeonhardRemixEvent = new ScriptedEvent(new Vector2(3418, 362), 35, ModContent.NPCType<NPCs.Special.LeonhardRemix>(), 60, true, true, true, LangUtils.GetTextValue("Events.Leonhard1"), Color.Red, false, LeonhardRemixSecretCondition);

            //HOLLOW AMBUSH 1 - BOTTOM RIGHT OF EARTH TEMPLE
            List<int> HollowAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.FirebombHollow>() };
            List<Vector2> HollowAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4446, 1211), new Vector2(4456, 1211) };
            ScriptedEvent HollowAmbush1 = new ScriptedEvent(new Vector2(4422, 1210), 10, HollowAmbush1EnemyTypeList, HollowAmbush1EnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.HollowAmbush1"), Color.Red, false, PreEoCCustomCondition, null);

            //GOBLIN AMBUSH 1 - RIGHT OF WORLD SPAWN
            List<int> GoblinAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.AbandonedStump>(), NPCID.GoblinSorcerer, NPCID.GoblinScout };
            List<Vector2> GoblinAmbush1EnemyLocations = new List<Vector2>() { new Vector2(5012, 851), new Vector2(5013, 823), new Vector2(5049f, 839) };
            ScriptedEvent GoblinAmbush1 = new ScriptedEvent(new Vector2(5028, 837), 18, GoblinAmbush1EnemyTypeList, GoblinAmbush1EnemyLocations, default, true, false, false, LangUtils.GetTextValue("Events.GoblinAmbush1"), Color.Red, false, OnlyAdventureMapCondition);
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
            List<int> BridgeAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.HollowWarrior>(), ModContent.NPCType<NPCs.Enemies.HollowSoldier>(), ModContent.NPCType<NPCs.Enemies.ManHunter>(), ModContent.NPCType<NPCs.Enemies.HollowSpearman>() };
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

            //GHOST OF THE DROWNED AMBUSH 1 - NEAR ENTRANCE OF CATACOMBS OF THE DROWNED
            List<int> DrownedAmbush1EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.GhostFighter.GhostOfTheDrowned>() };
            List<Vector2> DrownedAmbush1EnemyLocations = new List<Vector2>() { new Vector2(4294, 778) };
            ScriptedEvent DrownedAmbush1 = new ScriptedEvent(new Vector2(4318, 768), 11, DrownedAmbush1EnemyTypeList, DrownedAmbush1EnemyLocations, DustID.Water, true, false, false, LangUtils.GetTextValue("Events.BridgeAmbush1"), Color.Red);
            DrownedAmbush1.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.HealingElixir>() }, new List<int>() { 1 });

            //GHOST OF THE DROWNED AMBUSH 1 - NEAR ENTRANCE OF CATACOMBS OF THE DROWNED
            List<int> DrownedAmbush2EnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.GhostFighter.GhostOfTheDrowned>() };
            List<Vector2> DrownedAmbush2EnemyLocations = new List<Vector2>() { new Vector2(4117, 823) };
            ScriptedEvent DrownedAmbush2 = new ScriptedEvent(new Vector2(4090, 828), 11, DrownedAmbush2EnemyTypeList, DrownedAmbush2EnemyLocations, DustID.Water, true, false, false, LangUtils.GetTextValue("Events.BridgeAmbush1"), Color.Red);
            DrownedAmbush2.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Potions.BoostPotion>() }, new List<int>() { 2 }); 

            //Sandstorm Elemental in the Solar Island
            ScriptedEvent SandstormElementalEvent = new ScriptedEvent(new Vector2(2021, 351), 40, NPCID.SandElemental, 269, true, true, false, LangUtils.GetTextValue("Events.SandstormElementalEvent"), Color.Yellow, false, RemixMapCondition);

            //Ancient vision in the Shadow Temple
            ScriptedEvent ShadowTempleEvent = new ScriptedEvent(new Vector2(1460, 1364), 25, NPCID.AncientCultistSquidhead, 228, true, true, false, LangUtils.GetTextValue("Events.ShadowTempleEvent"), Color.Yellow);
            
            //Paladin in the Shadow Temple 
            ScriptedEvent ShadowTempleEvent2 = new ScriptedEvent(new Vector2(1734, 1297), 20, NPCID.Paladin, 133, true, true, false, LangUtils.GetTextValue("Events.ShadowTempleEvent2"), Color.Yellow);

            //Mushroom Cavern AMBUSH
            List<int> MushroomCavernEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Enemies.SuperHardMode.TaurusKnight>(), ModContent.NPCType<NPCs.Enemies.Dworc.DworcAbysswalker>(), };
            List<Vector2> MushroomCavernEnemyLocations = new List<Vector2>() { new Vector2(3690, 1545), new Vector2(3675, 1545) };
            ScriptedEvent MushroomCavern = new ScriptedEvent(new Vector2(3690, 1535), 30, MushroomCavernEnemyTypeList, MushroomCavernEnemyLocations, DustID.Water, true, true, false, LangUtils.GetTextValue("Events.BridgeAmbush1"), Color.Red);

            //Ashen Cavern Leftside - Great Demon Of The Abyss
            ScriptedEvent AshCavernLeftside = new ScriptedEvent(new Vector2(1578, 1895), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.AncientDemonOfTheAbyss>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, OnlyAdventureMapConditionSHM);

            //Ashen Cavern Rightside - Oolacile Knight
            ScriptedEvent AshCavernRightside = new ScriptedEvent(new Vector2(2382, 1882), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileKnight>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, OnlyAdventureMapConditionSHM);

            //Morgul Feleg Leftside - Oolacile Knight
            ScriptedEvent MorgulFelegLeftside = new ScriptedEvent(new Vector2(1578, 1895), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileKnight>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, RemixMapConditionSHM);

            //Morgul Feleg Rightside - Great Demon Of The Abyss
            ScriptedEvent MorgulFelegRightside = new ScriptedEvent(new Vector2(2382, 1882), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.AncientDemonOfTheAbyss>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, RemixMapConditionSHM);

            //Great Demon Of The Abyss on the Abandonned Hallowed Village (Remix Map)
            ScriptedEvent HallowDemonEvent = new ScriptedEvent(new Vector2(6761, 1081), 30, ModContent.NPCType<NPCs.Enemies.SuperHardMode.AncientDemonOfTheAbyss>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, RemixMapConditionSHM);

            //Molten Sky Temple 
            ScriptedEvent MoltenSkyTempleEvent = new ScriptedEvent(new Vector2(1040, 1865), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss.SerpentOfTheAbyssHead>(), 6, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Orange);

            //Molten Sky Temple second event
            ScriptedEvent MoltenSkyTempleEvent2 = new ScriptedEvent(new Vector2(90, 1893), 30, NPCID.MourningWood, 6, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Orange, false, RemixMapCondition, SetNightCustomAction);

            ScriptedEvent HellkiteDragonEvent = new ScriptedEvent(new Vector2(4282, 405), 200, ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>(), DustID.OrangeTorch, true, true, true, LangUtils.GetTextValue("Events.HellkiteDragon"), new Color(175, 75, 255), false, SuperHardModeCustomCondition, SetNightCustomAction);

            ScriptedEvent DungeonGuardianEvent = new ScriptedEvent(new Vector2(4228, 1800), 20, NPCID.DungeonGuardian, DustID.WhiteTorch, false, true, false, "default", new Color(175, 75, 255), false, PreSkeletronDungeonGuardianCondition);
            
            ScriptedEvent KingSlime2Event = new ScriptedEvent(new Vector2(4749, 639), 25, NPCID.KingSlime, DustID.MagicMirror, true, true, false, LangUtils.GetTextValue("Events.KingSlime"), Color.Cyan, false, RemixMapCondition);

            ScriptedEvent AbysswalkerEvent = new ScriptedEvent(new Vector2(5781, 1525), 25, ModContent.NPCType<NPCs.Enemies.Dworc.DworcAbysswalker>(), 107, true, false, true, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Lime, false, RemixMapCondition, SetNightCustomAction);

            List<int> BloodLakeEventEnemyTypeList = new List<int>() { NPCID.ZombieMerman, NPCID.EyeballFlyingFish };
            List<Vector2> BloodLakeEventEnemyLocations = new List<Vector2>() { new Vector2(2999, 889), new Vector2(3009, 889) };
            ScriptedEvent BloodLakeEvent = new ScriptedEvent(new Vector2(3004, 889), 25, BloodLakeEventEnemyTypeList, BloodLakeEventEnemyLocations, 60, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, RemixMapCondition, SetNightCustomAction); 
            BloodLakeEvent.SetCustomDrops(new List<int>() { ItemID.HealingPotion, ItemID.BloodRainBow, ItemID.VampireFrogStaff, ItemID.MoneyTrough, ItemID.SharkToothNecklace, ItemID.CombatBook }, new List<int>() { 5, 1, 1, 1, 1, 1 });

            ScriptedEvent BloodBossEvent1 = new ScriptedEvent(new Vector2(2914, 526), 25, NPCID.BloodEelHead, 60, true, true, false, LangUtils.GetTextValue("Events.BloodBossEvent1"), Color.Red, false, RemixMapCondition, SetNightCustomAction);
            BloodBossEvent1.SetCustomDrops(new List<int>() { ItemID.GreaterHealingPotion, ItemID.WrathPotion, ItemID.DripplerFlail }, new List<int>() { 5, 2, 1 });
            BloodBossEvent1.SetCustomStats(null, null, null, 7500);

            ScriptedEvent BloodBossEvent2 = new ScriptedEvent(new Vector2(2765, 620), 25, NPCID.GoblinShark, 60, true, true, false, LangUtils.GetTextValue("Events.GoblinShark"), Color.Red, false, RemixMapCondition, SetNightCustomAction);
            BloodBossEvent2.SetCustomDrops(new List<int>() { ItemID.GreaterHealingPotion, ItemID.RagePotion, ItemID.SharpTears }, new List<int>() { 5, 2, 1 });
            BloodBossEvent2.SetCustomStats(null, null, null, 7500);

            ScriptedEvent BloodBossEvent3 = new ScriptedEvent(new Vector2(2893, 610), 30, NPCID.BloodNautilus, 60, true, true, false, LangUtils.GetTextValue("Events.BloodBossEvent3"), Color.Red, false, RemixMapCondition, SetBloodMoonCustomAction);
            BloodBossEvent3.SetCustomDrops(new List<int>() { ItemID.SuperHealingPotion, ItemID.LifeforcePotion, ItemID.BloodHamaxe, ItemID.MagicQuiver, ItemID.LavaCharm }, new List<int>() { 6, 3, 1, 1, 1, 1});
            BloodBossEvent3.SetCustomStats(null, null, null, 12000);

            List<int> TwinsEventEnemyTypeList = new List<int>() { NPCID.Retinazer, NPCID.Spazmatism };
            List<Vector2> TwinsEventEnemyLocations = new List<Vector2>() { new Vector2(2864, 236), new Vector2(2904, 236) };
            ScriptedEvent TwinsEvent = new ScriptedEvent(new Vector2(2884, 236), 42, TwinsEventEnemyTypeList, TwinsEventEnemyLocations, 15, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Green, false, RemixMapCondition, SetNightCustomAction); 

            ScriptedEvent CatacombsEvent = new ScriptedEvent(new Vector2(3181, 1334), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.SlograII>(), DustID.Torch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Red, false, RemixMapCondition);
            CatacombsEvent.SetCustomStats(9000, null, null, null);

            ScriptedEvent Dutchman = new ScriptedEvent(new Vector2(597, 848), 50, NPCID.PirateShip, DustID.GoldFlame, true, false, true, LangUtils.GetTextValue("Events.Dutchman"), Color.Yellow, false, RemixMapCondition);
            Dutchman.SetCustomStats(null, null, null, 8000);

            ScriptedEvent EnragedQB = new ScriptedEvent(new Vector2(5954, 401), 50, NPCID.QueenBee, DustID.GoldFlame, true, true, false, LangUtils.GetTextValue("Events.EnragedQB"), Color.Yellow, false, RemixMapCondition);
            EnragedQB.SetCustomDrops(new List<int>() { ItemID.GreaterHealingPotion, ItemID.RagePotion, ModContent.ItemType<Items.Materials.EternalCrystal>() }, new List<int>() { 3, 1, 1 });
            EnragedQB.SetCustomStats(null, null, null, 2000);

            ScriptedEvent FoundryEvent = new ScriptedEvent(new Vector2(5229, 1254), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileKnight>(), DustID.CursedTorch, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Orange, false, RemixMapCondition);

            ScriptedEvent FoundryEvent2 = new ScriptedEvent(new Vector2(5801, 1381), 25, ModContent.NPCType<NPCs.Enemies.SuperHardMode.TaurusKnight>(), DustID.GoldFlame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Yellow, false, RemixMapCondition); 

            ScriptedEvent FrozenCathedralEvent = new ScriptedEvent(new Vector2(7635, 1735), 65, NPCID.IceQueen, 67, true, true, false, LangUtils.GetTextValue("Events.FrozenCathedralEvent"), Color.Cyan, false, RemixMapCondition, SetNightCustomAction);
            FrozenCathedralEvent.SetCustomStats(null, null, null, 10000);

            ScriptedEvent WyvernPrisonEvent = new ScriptedEvent(new Vector2(6408, 385), 32, NPCID.Pumpking, DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Orange, false, RemixMapCondition, SetNightCustomAction);

            List<int> FrozenCathedralEvent2EnemyTypeList = new List<int>() { NPCID.MourningWood, NPCID.MourningWood };
            List<Vector2> FrozenCathedralEvent2EnemyLocations = new List<Vector2>() { new Vector2(7189, 1650), new Vector2(7164, 1650) };
            ScriptedEvent FrozenCathedralEvent2 = new ScriptedEvent(new Vector2(7177, 1650), 35, FrozenCathedralEvent2EnemyTypeList, FrozenCathedralEvent2EnemyLocations, 6, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Orange, false, RemixMapCondition, SetNightCustomAction); 
            
            List<int> WingTrioEventEnemyTypeList = new List<int>() { ModContent.NPCType<NPCs.Bosses.TheRage>(), ModContent.NPCType<NPCs.Bosses.TheSorrow>(), ModContent.NPCType<NPCs.Bosses.TheHunter>() };
            List<Vector2> WingTrioEventEnemyLocations = new List<Vector2>() { new Vector2(3838, 1425), new Vector2(3879, 1425), new Vector2(3858, 1410) };
            ScriptedEvent WingTrioEvent = new ScriptedEvent(new Vector2(3858, 1420), 40, WingTrioEventEnemyTypeList, WingTrioEventEnemyLocations, 292, true, true, true, LangUtils.GetTextValue("Events.WingTrio"), Color.Yellow, false, RemixMapCondition);
            WingTrioEvent.SetCustomDrops(new List<int>() { ModContent.ItemType<Items.Materials.EternalCrystal>(), ModContent.ItemType<Items.Accessories.Trinity>(), ModContent.ItemType<Items.Accessories.Trinity>(), ModContent.ItemType<Items.Accessories.Trinity>(), ModContent.ItemType<Items.Accessories.Trinity>()}, new List<int>() { 3, 1, 1, 1, 1 });
            WingTrioEvent.SetCustomStats(null, null, null, 40000);

            ScriptedEvent SkeletronPrimeEvent = new ScriptedEvent(new Vector2(1765, 1479), 30, NPCID.SkeletronPrime, DustID.Flare, true, true, false, LangUtils.GetTextValue("Events.SkeletronPrimeRemix"), Color.Red, false, RemixMapCondition, SetNightCustomAction);
            SkeletronPrimeEvent.SetCustomDrops(new List<int>() { ItemID.SuperHealingPotion, ModContent.ItemType<Items.Materials.EternalCrystal>() }, new List<int>() { 6, 3 });

            ScriptedEvent GoblinSharkTropicalIsland = new ScriptedEvent(new Vector2(7874, 390), 40, NPCID.GoblinShark, DustID.CrimsonSpray, true, false, true, LangUtils.GetTextValue("Events.GoblinShark"), Color.Red, false, OnlyAdventureMapCondition, SetNightCustomAction);
            GoblinSharkTropicalIsland.SetCustomDrops(new List<int>() { ItemID.SuperHealingPotion, ItemID.RagePotion, ItemID.SharpTears }, new List<int>() { 5, 3, 1 });
            GoblinSharkTropicalIsland.SetCustomStats(5000, null, null, 15000); 

            ScriptedEvent IceGolemIsland = new ScriptedEvent(new Vector2(7691, 357), 40, NPCID.IceGolem, 67, true, false, true, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Blue, false, RemixMapCondition);
            IceGolemIsland.SetCustomDrops(new List<int>() { ItemID.SuperHealingPotion, ItemID.RagePotion}, new List<int>() { 5, 3 });
            IceGolemIsland.SetCustomStats(null, null, null, 10000);

            ScriptedEvent DiscipleOfAttraidiesEvent = new ScriptedEvent(new Vector2(6971, 1113), 40, ModContent.NPCType<NPCs.Enemies.DiscipleOfAttraidies>(), 15, true, true, false, LangUtils.GetTextValue("Events.DiscipleOfAttraidiesEvent"), Color.Cyan, false, RemixMapCondition);

            ScriptedEvent WyvernFortressEvent = new ScriptedEvent(new Vector2(7022, 288), 85, NPCID.WyvernHead, 16, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Cyan, false, RemixMapCondition);

            ScriptedEvent SandstormElementalEvent2 = new ScriptedEvent(new Vector2(950, 1503), 22, NPCID.SandElemental, 269, true, true, false, LangUtils.GetTextValue("Events.SandstormElementalEvent"), Color.Yellow, false, RemixMapCondition);

            //ScriptedEvent ShadowDarkCloud1 = new ScriptedEvent(new Vector2(2159, 308), 30, ModContent.NPCType<NPCs.Special.DarkCloudShadow>(), DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Purple, false, RemixMapConditionSHM);

            //ScriptedEvent ShadowDarkCloud2 = new ScriptedEvent(new Vector2(2159, 308), 30, ModContent.NPCType<NPCs.Special.DarkCloudShadow>(), DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Purple, false, RemixMapConditionSHM);

            //ScriptedEvent ShadowDarkCloud3 = new ScriptedEvent(new Vector2(2159, 308), 30, ModContent.NPCType<NPCs.Special.DarkCloudShadow>(), DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Purple, false, RemixMapConditionSHM);

            //ScriptedEvent ShadowDarkCloud4 = new ScriptedEvent(new Vector2(2159, 308), 30, ModContent.NPCType<NPCs.Special.DarkCloudShadow>(), DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Purple, false, RemixMapConditionSHM);

            ScriptedEvent ShadowDarkCloud5 = new ScriptedEvent(new Vector2(5828, 1760), 30, ModContent.NPCType<NPCs.Special.DarkCloudShadow>(), DustID.Shadowflame, true, true, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Purple, false, RemixMapConditionSHM);

            //THORIUM SECTION
            Mod ThoriumMod;
            ModLoader.TryGetMod("ThoriumMod", out ThoriumMod);
            
            ScriptedEvent ThunderBird = new(new Vector2(3818, 416), 25, GetThoriumNPCType("TheGrandThunderBird"), DustID.Electric, true, true, false, LangUtils.GetTextValue("Events.ThunderBird"), Color.Yellow, false, ThoriumActive);
            ScriptedEvent ThunderBird2 = new(new Vector2(5029, 770), 20, GetThoriumNPCType("TheGrandThunderBird"), DustID.Electric, true, false, false, LangUtils.GetTextValue("Events.ThunderBird"), Color.Yellow, false, ThoriumActive);
            ScriptedEvent QueenJellyfish = new(new Vector2(5233, 1422), 25, GetThoriumNPCType("QueenJellyfish"), DustID.CoralTorch, true, true, false, LangUtils.GetTextValue("Events.QueenJellyfish"), Color.Pink, false, ThoriumActive);
            ScriptedEvent Viscount = new(new Vector2(4660, 1432), 25, GetThoriumNPCType("Viscount"), DustID.CrimsonTorch, true, true, false, LangUtils.GetTextValue("Events.Viscount"), Color.Red, false, ThoriumActive);
            ScriptedEvent StarScouter = new(new Vector2(5412, 426), 30, GetThoriumNPCType("StarScouter"), DustID.Vortex, true, true, false, LangUtils.GetTextValue("Events.StarScouter"), Color.Green, false, ThoriumActive);
            ScriptedEvent BoreanStrider = new(new Vector2(7877, 855), 80, GetThoriumNPCType("BoreanStrider"), DustID.IceTorch, true, false, false, LangUtils.GetTextValue("Events.IceGolemWyvern"), Color.Cyan, false, ThoriumActive, RainCustomAction);
            ScriptedEvent Lich = new(new Vector2(1401, 299), 25, GetThoriumNPCType("Lich"), 181, true, true, false, LangUtils.GetTextValue("Events.Lich"), Color.Red, false, ThoriumActive, SetNightCustomAction);
            ScriptedEvent ForgottenOne = new(new Vector2(218, 1027), 35, GetThoriumNPCType("ForgottenOne"), 173, true, true, false, LangUtils.GetTextValue("Events.ForgottenOne"), Color.Blue, false, ThoriumActive);
            
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
                {ScriptedEventType.GoblinSharkTropicalIsland, GoblinSharkTropicalIsland},
                {ScriptedEventType.GreatRedKnightInDesert, GreatRedKnightInDesert},
                {ScriptedEventType.AncestralSpiritEvent, AncestralSpiritEvent},
                {ScriptedEventType.OldManEvent, OldManEvent},
                {ScriptedEventType.SkeletronHidden, SkeletronHiddenEvent},
                {ScriptedEventType.AlienAmbush, AlienAmbush},
                {ScriptedEventType.EoC, EoCEvent},
                {ScriptedEventType.EoW1, EoW1Event},
                {ScriptedEventType.AncientDemon, AncientDemon},
                {ScriptedEventType.LichKing, LichKing},
                {ScriptedEventType.LichKingRemix, LichKingRemix},
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
                {ScriptedEventType.BlackKnightPain, BlackKnightPain},
                {ScriptedEventType.RedKnightTwinMountain, RedKnightTwinMountain},
                {ScriptedEventType.JungleWyvernFight, JungleWyvernEvent},
                {ScriptedEventType.SeathFight, SeathEvent},
                {ScriptedEventType.WyvernMageFight, WyvernMageEvent},
                {ScriptedEventType.SlograAndGaibonFight, SlograAndGaibonEvent},
                {ScriptedEventType.SerrisFight, SerrisEvent},
                {ScriptedEventType.MarilithFight, MarilithEvent},
                {ScriptedEventType.RemixMarilithFight, RemixMarilithEvent},
                {ScriptedEventType.PrimeFight, PrimeEvent},
                {ScriptedEventType.KrakenFight, KrakenEvent},
                {ScriptedEventType.AbyssPortal, AbyssPortalEvent},
                {ScriptedEventType.GwynTombVision, GwynsTombEvent},
                {ScriptedEventType.GwynFight, GwynEvent},
                {ScriptedEventType.RemixGwynFight, RemixGwynEvent},
                {ScriptedEventType.AbysmalOolacileSorcererFight, AbysmalOolacileSorcererEvent},
                {ScriptedEventType.RemixAbysmalOolacileSorcererFight, RemixAbysmalOolacileSorcererEvent},
                {ScriptedEventType.WitchkingFight, WitchkingEvent},
                {ScriptedEventType.RemixWitchkingEvent, RemixWitchkingEvent},
                {ScriptedEventType.ChaosFight, ChaosEvent},
                {ScriptedEventType.ChaosEventRemix, ChaosEventRemix},
                {ScriptedEventType.WyvernMageShadowFight, WyvernMageShadowEvent},
                {ScriptedEventType.BlightFight, BlightEvent},
                {ScriptedEventType.DarkCloudPyramidFight, DarkCloudEvent},
                {ScriptedEventType.DarkCloudEventRemix, DarkCloudEventRemix},
                {ScriptedEventType.ArtoriasFight, ArtoriasEvent},
                {ScriptedEventType.BlackKnightCity, BlackKnightCity},
                //{ScriptedEventType.ExampleHarpySwarm, ExampleHarpySwarm},
                //{ScriptedEventType.ExampleNoNPCScriptEvent, ExampleNoNPCScriptEvent},
                //{ScriptedEventType.Frogpocalypse2_TheFroggening, FrogpocalypseEvent}
                {ScriptedEventType.SpawnUndeadMerchant, SpawnUndeadMerchant },
                {ScriptedEventType.SpawnGoblin, SpawnGoblin },
                {ScriptedEventType.SpawnLonelyFairy, SpawnLonelyFairy },
                {ScriptedEventType.AttraidiesTheSorrowEvent, AttraidiesTheSorrowEvent},
                {ScriptedEventType.TwinEoWFight, TwinEoWFight},
                {ScriptedEventType.DunledingAmbush, DunledingAmbush},
                {ScriptedEventType.RemixDunledingAmbush, RemixDunledingAmbush},
                {ScriptedEventType.BoulderfallEvent1, BoulderfallEvent1},
                {ScriptedEventType.BoulderfallEvent2, BoulderfallEvent2},
                {ScriptedEventType.BoulderfallEvent3, BoulderfallEvent3},
                {ScriptedEventType.LeonhardPhase1Event, LeonhardPhase1Event},
                //{ScriptedEventType.LeonhardRemixEvent, LeonhardRemixEvent},
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
                {ScriptedEventType.DrownedAmbush1, DrownedAmbush1 },
                {ScriptedEventType.DrownedAmbush2, DrownedAmbush2 },
                {ScriptedEventType.MushroomCavern, MushroomCavern },
                {ScriptedEventType.AshCavernLeftside, AshCavernLeftside },
                {ScriptedEventType.AshCavernRightside, AshCavernRightside },
                {ScriptedEventType.MorgulFelegLeftside, MorgulFelegLeftside }, 
                {ScriptedEventType.MorgulFelegRightside, MorgulFelegRightside },
                {ScriptedEventType.HallowDemonEvent, HallowDemonEvent },
                {ScriptedEventType.ShadowTempleEvent, ShadowTempleEvent },
                {ScriptedEventType.ShadowTempleEvent2, ShadowTempleEvent2 },
                {ScriptedEventType.MoltenSkyTempleEvent, MoltenSkyTempleEvent },
                {ScriptedEventType.MoltenSkyTempleEvent2, MoltenSkyTempleEvent2 },
                {ScriptedEventType.SandstormElementalEvent, SandstormElementalEvent },
                {ScriptedEventType.AbysswalkerEvent, AbysswalkerEvent },
                {ScriptedEventType.BloodLakeEvent, BloodLakeEvent },
                {ScriptedEventType.BloodBossEvent1, BloodBossEvent1 },
                {ScriptedEventType.BloodBossEvent2, BloodBossEvent2 },
                {ScriptedEventType.BloodBossEvent3, BloodBossEvent3 },
                {ScriptedEventType.TwinsEvent, TwinsEvent },
                {ScriptedEventType.CatacombsEvent, CatacombsEvent },
                {ScriptedEventType.FrozenCathedralEvent, FrozenCathedralEvent },
                {ScriptedEventType.EnragedQB, EnragedQB },
                {ScriptedEventType.FoundryEvent, FoundryEvent },
                {ScriptedEventType.FoundryEvent2, FoundryEvent2 },
                {ScriptedEventType.Lunatic, Lunatic },
                {ScriptedEventType.KingSlime2Event, KingSlime2Event },
                {ScriptedEventType.IceGolemIsland, IceGolemIsland },
                {ScriptedEventType.AncestralSpiritRemixEvent, AncestralSpiritRemixEvent },
                {ScriptedEventType.FrozenCathedralEvent2, FrozenCathedralEvent2 },
                {ScriptedEventType.WingTrioEvent, WingTrioEvent },
                {ScriptedEventType.WyvernFortressEvent, WyvernFortressEvent },
                {ScriptedEventType.SkeletronPrimeEvent, SkeletronPrimeEvent },
                {ScriptedEventType.SandstormElementalEvent2, SandstormElementalEvent2 },
                {ScriptedEventType.DeathRemix, DeathRemix },
                {ScriptedEventType.DiscipleOfAttraidiesEvent, DiscipleOfAttraidiesEvent },
                {ScriptedEventType.Dutchman, Dutchman },
                {ScriptedEventType.ThunderBird, ThunderBird },
                {ScriptedEventType.ThunderBird2, ThunderBird2 },
                {ScriptedEventType.QueenJellyfish, QueenJellyfish },
                {ScriptedEventType.Viscount, Viscount },
                {ScriptedEventType.StarScouter, StarScouter },
                {ScriptedEventType.BoreanStrider, BoreanStrider },
                {ScriptedEventType.Lich, Lich },
                {ScriptedEventType.ForgottenOne, ForgottenOne },
                //{ScriptedEventType.ShadowDarkCloud1, ShadowDarkCloud1 },
                //{ScriptedEventType.ShadowDarkCloud2, ShadowDarkCloud2 },
                //{ScriptedEventType.ShadowDarkCloud3, ShadowDarkCloud3 },
                //{ScriptedEventType.ShadowDarkCloud4, ShadowDarkCloud4 },
                {ScriptedEventType.ShadowDarkCloud5, ShadowDarkCloud5 },
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

        public static bool RemixMapCondition()
        {
            return tsorcRevampWorld.RemixMap;
        }
        public static bool OnlyAdventureMapCondition()
        {
            return tsorcRevampWorld.OnlyAdventureMap;
        }
        public static bool OnlyAdventureMapConditionSHM()
        {
            return OnlyAdventureMapCondition() && tsorcRevampWorld.SuperHardMode;
        }
        public static bool RemixMapConditionSHM()
        {
            return RemixMapCondition() && tsorcRevampWorld.SuperHardMode;
        }
        //True only on the 2400-tall Expanded Adventure world. NOTE: the expanded world also satisfies
        //OnlyAdventureMapCondition (it IS the adventure map), so existing adventure events still run there. Gate NEW,
        //expanded-only content with this. Content gated here is authored natively in 2400-space and must NOT be routed
        //through ExpandedWorldTransform (it would be double-shifted).
        public static bool ExpandedAdventureMapCondition()
        {
            return tsorcRevampWorld.ExpandedAdventure;
        }
        public static bool ExpandedAdventureMapConditionSHM()
        {
            return ExpandedAdventureMapCondition() && tsorcRevampWorld.SuperHardMode;
        }

        private static Dictionary<int, string> _vanillaIdToName;
        private static string GetVanillaFieldName(int type)
        {
            if (_vanillaIdToName == null)
            {
                _vanillaIdToName = new Dictionary<int, string>();
                foreach (var f in typeof(NPCID).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (f.IsLiteral && (f.FieldType == typeof(short) || f.FieldType == typeof(int)))
                    {
                        int id = Convert.ToInt32(f.GetRawConstantValue());
                        if (!_vanillaIdToName.ContainsKey(id))
                            _vanillaIdToName[id] = f.Name;
                    }
                }
            }
            return _vanillaIdToName.TryGetValue(type, out var name) ? name : null;
        }

        /// <summary>
        /// Returns a stable name string for <paramref name="type"/>.
        /// Modded: full mod-relative name e.g. "tsorcRevamp/LeonhardPhase1".
        /// Vanilla: NPCID field name e.g. "EyeofCthulhu" (resolvable by reflection on load).
        /// </summary>
        public static string GetNpcStableName(int type)
        {
            var modNpc = NPCLoader.GetNPC(type);
            if (modNpc != null) return modNpc.FullName;
            return GetVanillaFieldName(type) ?? type.ToString();
        }

        private static Dictionary<int, string> _vanillaItemIdToName;
        private static string GetVanillaItemFieldName(int type)
        {
            if (_vanillaItemIdToName == null)
            {
                _vanillaItemIdToName = new Dictionary<int, string>();
                foreach (var f in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (f.IsLiteral && (f.FieldType == typeof(short) || f.FieldType == typeof(int)))
                    {
                        int id = Convert.ToInt32(f.GetRawConstantValue());
                        if (!_vanillaItemIdToName.ContainsKey(id))
                            _vanillaItemIdToName[id] = f.Name;
                    }
                }
            }
            return _vanillaItemIdToName.TryGetValue(type, out var name) ? name : null;
        }

        /// <summary>
        /// Returns a stable name string for <paramref name="type"/>.
        /// Modded: full mod-relative name e.g. "tsorcRevamp/DodgerollMemo".
        /// Vanilla: ItemID field name e.g. "GreaterHealingPotion" (resolvable by reflection on load).
        /// </summary>
        public static string GetItemStableName(int type)
        {
            var modItem = ItemLoader.GetItem(type);
            if (modItem != null) return modItem.FullName;
            return GetVanillaItemFieldName(type) ?? type.ToString();
        }

        /// <summary>
        /// Resolves a stored NpcName back to its current runtime NPC type. Modded names go through
        /// ModContent.TryFind; vanilla names are NPCID field names resolved by reflection (with a raw-int fallback).
        /// Returns 0 if it can't be resolved.
        /// </summary>
        public static int ResolveNpcType(string npcName)
        {
            if (string.IsNullOrEmpty(npcName)) return 0;
            // Modded names are "Mod/Name". Only those go through TryFind (it can throw on a slash-less string).
            if (npcName.Contains('/'))
            {
                try
                {
                    if (ModContent.TryFind<ModNPC>(npcName, out ModNPC modNpc)) return modNpc.Type;
                }
                catch { /* mod not loaded / malformed name — fall through */ }
                return 0;
            }
            // Vanilla NPCID field name (e.g. "BoneThrowingSkeleton2"). Accept short OR int constants.
            var field = typeof(NPCID).GetField(npcName, BindingFlags.Public | BindingFlags.Static);
            if (field != null && (field.FieldType == typeof(short) || field.FieldType == typeof(int)))
                return Convert.ToInt32(field.GetRawConstantValue());
            if (int.TryParse(npcName, out int raw)) return raw;
            return 0;
        }

        /// <summary>
        /// Resolves a stored item name back to its current runtime item type. Modded names go through
        /// ModContent.TryFind; vanilla names are ItemID field names resolved by reflection (with a raw-int fallback).
        /// Returns 0 if it can't be resolved.
        /// </summary>
        public static int ResolveItemType(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 0;
            if (itemName.Contains('/'))
            {
                try
                {
                    if (ModContent.TryFind<ModItem>(itemName, out ModItem modItem)) return modItem.Type;
                }
                catch { /* mod not loaded / malformed name - fall through */ }
                return 0;
            }

            var field = typeof(ItemID).GetField(itemName, BindingFlags.Public | BindingFlags.Static);
            if (field != null && (field.FieldType == typeof(short) || field.FieldType == typeof(int)))
                return Convert.ToInt32(field.GetRawConstantValue());
            if (int.TryParse(itemName, out int raw)) return raw;
            return 0;
        }

        /// <summary>
        /// Whether a dynamic event should be shown/editable in the Enemy Debug Tome for the current world.
        /// Adventure-Map-only events are hidden in a Remix world, and Remix-only events are hidden in a
        /// classic Adventure world. Events with no world condition show everywhere.
        /// </summary>
        public static bool IsEventVisibleInCurrentWorld(DynamicSpawnEvent ev)
        {
            if (ev == null)
                return false;
            // Legacy events (dumped from hardcoded data) stored world type in MapCondition, not WorldCondition.
            // Check WorldCondition first; fall back to MapCondition if it contains an Adventure/Remix token.
            string worldCond = ev.WorldCondition;
            if (string.IsNullOrEmpty(worldCond) &&
                (ev.MapCondition?.Contains("OnlyAdventureMap") == true || ev.MapCondition?.Contains("RemixMap") == true))
            {
                worldCond = ev.MapCondition;
            }
            if (tsorcRevampWorld.RemixMap && worldCond?.Contains("OnlyAdventureMap") == true)
                return false;
            if (tsorcRevampWorld.OnlyAdventureMap && worldCond?.Contains("RemixMap") == true)
                return false;
            return true;
        }

        // Named (not an inline lambda) so its Method.Name survives being dumped to JSON — an anonymous lambda's
        // compiler-generated name (e.g. "<InitializeScriptedEvents>b__11_0") can't be found by reflection on load,
        // which silently made this condition a no-op and let the Old Man event re-spawn its NPC endlessly,
        // duplicating alongside the one that's already there or already turned into Skeletron.
        public static bool OldManSpawnCondition()
        {
            return !NPC.AnyNPCs(NPCID.OldMan) && !NPC.AnyNPCs(NPCID.SkeletronHead) && !NPC.downedBoss3;
        }

        // Stops the "secret" Skeletron ambush once Skeletron is down, however it was defeated (this event's own
        // spawn, or the Old Man arena). See the comment at SkeletronHiddenEvent for why this guard is needed.
        public static bool SkeletronHiddenSpawnCondition()
        {
            return !NPC.AnyNPCs(NPCID.SkeletronHead) && !NPC.downedBoss3;
        }

        // Same anonymous-lambda-name problem as OldManSpawnCondition above: this ambush should stop once Skeletron
        // is downed, but the compiler-generated name from the old inline lambda failed to resolve on load, so the
        // condition silently became "always true" and the guardian kept ambushing post-Skeletron.
        public static bool PreSkeletronDungeonGuardianCondition()
        {
            return !NPC.downedBoss3;
        }

        public static bool MarilithCustomCondition()
        {
            if (tsorcRevampWorld.RemixMap || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) || NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()) || NPC.AnyNPCs(ModContent.NPCType<MarilithIntro>()))
            {
                return false;
            }
            else
            {
                return true;
            }            
        }
        public static bool RemixMarilithCustomCondition()
        {
            if (!tsorcRevampWorld.RemixMap || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) || NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()) || NPC.AnyNPCs(ModContent.NPCType<MarilithIntro>()))
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

        public static bool CultistDownedCondition()
        {
            return !NPC.downedAncientCultist;
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

        public static bool FairyCondition()
        {
            return RemixMapCondition() && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.LonelyFairy>());
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

        public static bool LeonhardRemixSecretCondition()
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Special.LeonhardPhase1>())) && tsorcRevampWorld.RemixMap && Main.bloodMoon)
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


        //This is an example custom action (using Knight of Gwyn, which carries Artorias's old FighterAI-era attack pattern). It spawns meteors and displays text every so often, and also changes the projectile damage for the NPC. Most enemies will require a very small change for their projectile damage changes to work (the word 'public' needs to be in front of the variable controlling that projectile's damage).
        public static EventActionStatus KnightOfGwynCustomAction(ScriptedEvent thisEvent)
        {
            //Spawning meteors:
            if (Main.rand.NextBool(200))
            {
                //UsefulFunctions.BroadcastText("Knight of Gwyn rains fire from the Abyss...", Color.Gold);
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
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.KnightOfGwyn>())
            {
                //Then, we cast the NPC to our custom modded npc type. This lets us alter unique properties defined within the code of that modded NPC, such as its projectile damage values.
                NPCs.Enemies.SuperHardMode.KnightOfGwyn ourKnight = (NPCs.Enemies.SuperHardMode.KnightOfGwyn)thisEvent.eventNPCs[0].npc.ModNPC;

                //Now we can change the damages!!
                //Note: If you can't find the damages for a NPC, their damage stats might not be public.
                //It's an easy fix though: Go to the file for the NPC you want to change and find the damage variables for the projectiles you want to modify (in this case blackBreathDamage and phantomSeekerDamage) and put 'public' in front of them.
                //Then you'll be able to access them from here and set them to anything!
                ourKnight.blackBreathDamage = 40;
                ourKnight.phantomSeekerDamage = 50;
            }
            return EventActionStatus.Continue;
        }


        public static EventActionStatus RainCustomAction(ScriptedEvent thisEvent)
        {
            Main.raining = true;
            Main.rainTime = Main.rand.Next(7200, 10800);
            Main.maxRaining = 0.75f;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
            return EventActionStatus.EndAction;
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

        public static EventActionStatus SetBloodMoonCustomAction(ScriptedEvent thisEvent)
        {
            //UsefulFunctions.BroadcastText("Time shifts forward...", Color.Purple);
            Main.dayTime = false;
            Main.time = 0;
            Main.bloodMoon = true;
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

        //LOTHRIC BLACK KNIGHT CUSTOM ACTION
        public static EventActionStatus LothricBlackKnightCustomAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.LothricBlackKnight>())
            {
                NPCs.Enemies.LothricBlackKnight ourKnight = (NPCs.Enemies.LothricBlackKnight)thisEvent.eventNPCs[0].npc.ModNPC;
                ourKnight.lothricDamage = 14;
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
        public static EventActionStatus BlackKnightPainCustomAction(ScriptedEvent thisEvent)
        {
            if (thisEvent.eventNPCs[0].npc.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                NPCs.Enemies.BlackKnight ourBlackKnightPain = (NPCs.Enemies.BlackKnight)thisEvent.eventNPCs[0].npc.ModNPC;
                ourBlackKnightPain.redKnightsSpearDamage = 17;
                ourBlackKnightPain.redMagicDamage = 15;
                ourBlackKnightPain.redKnightsGreatDamage = 19;
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
            Vector2 pos = ExpandedWorldTransform.MapWorld(new Vector2(1686, 963) * 16);
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)pos.X, (int)pos.Y, ModContent.NPCType<NPCs.Friendly.UndeadMerchant>());
            return EventActionStatus.CompletedEvent;
        }

        //i dont want this event to last forever, so just spawn the tinkerer and immediately end the event
        //... is what it SHOULD do?
        public static EventActionStatus TinkererAction(ScriptedEvent thisEvent)
        {
            NPC.savedGoblin = true;
            Vector2 pos = ExpandedWorldTransform.MapWorld(new Vector2(4456, 1744) * 16);
            NPC goblinNPC = NPC.NewNPCDirect(new EntitySource_Misc("Scripted Event"), (int)pos.X, (int)pos.Y, NPCID.GoblinTinkerer);
            Microsoft.Xna.Framework.Point home = ExpandedWorldTransform.MapTile(4449, 1740);
            goblinNPC.homeTileX = home.X;
            goblinNPC.homeTileY = home.Y;
            return EventActionStatus.CompletedEvent;
        }

        public static EventActionStatus FairyAction(ScriptedEvent thisEvent)
        {
            NPC.savedGoblin = true;
            Vector2 pos = ExpandedWorldTransform.MapWorld(new Vector2(7707, 1161) * 16);
            NPC goblinNPC = NPC.NewNPCDirect(new EntitySource_Misc("Scripted Event"), (int)pos.X, (int)pos.Y, ModContent.NPCType<NPCs.Friendly.LonelyFairy>());
            Microsoft.Xna.Framework.Point home = ExpandedWorldTransform.MapTile(7707, 1161);
            goblinNPC.homeTileX = home.X;
            goblinNPC.homeTileY = home.Y;
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
            Vector2 mechPos = ExpandedWorldTransform.MapWorld(new Vector2(277, 1366) * 16);
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)mechPos.X, (int)mechPos.Y, NPCID.Mechanic);
            NPC.savedMech = true;
            return EventActionStatus.CompletedEvent;
        }

        public static EventActionStatus WizardAction(ScriptedEvent thisEvent)
        {
            Vector2 wizPos = ExpandedWorldTransform.MapWorld(new Vector2(7322, 603) * 16);
            NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)wizPos.X, (int)wizPos.Y, NPCID.Wizard);
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
                        if (Main.player[i].active && Main.player[i].GetModPlayer<tsorcRevampPlayer>().SoulsMode)
                        {
                            Item.NewItem(new EntitySource_Misc("Scripted Event"), Main.player[i].Center, ModContent.ItemType<Items.EstusFlaskShard>());
                        }
                    }
                    return EventActionStatus.CompletedEvent;
                }
                else return EventActionStatus.Continue;
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

        public static List<DynamicSpawnEvent> DynamicEvents = new List<DynamicSpawnEvent>();

        public static void LoadDynamicEvents()
        {
            EnabledEvents.RemoveAll(ev => !string.IsNullOrEmpty(ev.DynamicEventID));

            string relativePath = "Content/DynamicEvents.json";
            string fullPath = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", relativePath);
            string json = "";

            try
            {
                if (File.Exists(fullPath)) // Prioritize local file if developer has it
                {
                    json = File.ReadAllText(fullPath);
                }
                else
                {
                    var stream = ModContent.GetInstance<tsorcRevamp>().GetFileStream(relativePath);
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                // File doesn't exist yet, ignore
                return;
            }

            if (string.IsNullOrEmpty(json)) return;

            DynamicEvents = JsonConvert.DeserializeObject<List<DynamicSpawnEvent>>(json);
            if (DynamicEvents == null) return;

            // Coordinate-space normalization for the Expanded Adventure world.
            // DynamicEvents.json is stored in LEGACY 2000-space (the 135 originals mirror the hardcoded events).
            // Convert every event to the CURRENT world's runtime space right here, so ALL downstream consumers
            // (the dedup below, the tome UI, hit-testing, and the ScriptedEvents we build) work in one consistent
            // space. Identity on legacy/remix/sandbox. The inverse is applied on save (SerializeDynamicEventsToLegacyJson).
            // This also fixes a duplicate-event bug: the hardcoded-vs-dynamic dedup (further down) compares
            // dyn.CenterX against the (transformed) hardcoded centerpoint, which only matches when both are runtime-space.
            if (ExpandedWorldTransform.Active)
            {
                foreach (DynamicSpawnEvent e in DynamicEvents)
                    DynamicEventToRuntimeSpace(e);
            }

            // Deduplicate dynamic events at the exact same or very close coordinates (within 2 tiles)
            List<DynamicSpawnEvent> uniqueEvents = new List<DynamicSpawnEvent>();
            bool changed = false;
            foreach (var ev in DynamicEvents)
            {
                bool isDuplicate = false;
                foreach (var unique in uniqueEvents)
                {
                    if (Math.Abs(unique.CenterX - ev.CenterX) < 2 && Math.Abs(unique.CenterY - ev.CenterY) < 2)
                    {
                        isDuplicate = true;
                        changed = true;
                        break;
                    }
                }
                if (!isDuplicate)
                {
                    uniqueEvents.Add(ev);
                }
            }
            if (changed)
            {
                DynamicEvents = uniqueEvents;
                try
                {
                    string cleanJson = SerializeDynamicEventsToLegacyJson();
                    File.WriteAllText(fullPath, cleanJson);
                }
                catch (Exception)
                {
                    // Ignore startup write errors, it will save later anyway
                }
            }

            // Resolve every entry's runtime NpcID up front, independent of whether the event is enabled this load.
            // NpcID is normally not serialized, so this must run for ALL events (including completed ones) or the
            // editor's sprite preview would draw nothing for them.
            foreach (var dEvent in DynamicEvents)
            {
                foreach (var npc in dEvent.Npcs)
                {
                    if (!string.IsNullOrEmpty(npc.NpcName))
                        npc.NpcID = ResolveNpcType(npc.NpcName);
                    else if (npc.NpcID != 0)
                        // Legacy entry: name missing but a stored ID survives. Backfill the name so it's stable from now on.
                        npc.NpcName = GetNpcStableName(npc.NpcID);
                }

                if (dEvent.ExtraLootItemNames != null && dEvent.ExtraLootItemNames.Count > 0)
                {
                    dEvent.ExtraLootItems = new List<int>();
                    foreach (string itemName in dEvent.ExtraLootItemNames)
                    {
                        dEvent.ExtraLootItems.Add(ResolveItemType(itemName));
                    }
                }
                else if (dEvent.ExtraLootItems != null)
                {
                    dEvent.ExtraLootItemNames = new List<string>();
                    foreach (int itemType in dEvent.ExtraLootItems)
                    {
                        dEvent.ExtraLootItemNames.Add(GetItemStableName(itemType));
                    }
                }
            }

            foreach (var dEvent in DynamicEvents)
            {
                // If it's saved as completed and not repeatable, don't add it.
                if (dEvent.SaveOnCompletion && tsorcRevampWorld.CompletedDynamicEvents.Contains(dEvent.EventID))
                    continue;

                // Resolve conditions and actions via reflection.
                // The World condition (Adventure/Remix/Always) and the Spawn condition are evaluated
                // independently and ANDed together, so an event can be e.g. "Adventure Map Only" + "Hardmode Only".
                Func<bool> worldCondition = null;
                if (!string.IsNullOrEmpty(dEvent.WorldCondition))
                {
                    var method = typeof(tsorcScriptedEvents).GetMethod(dEvent.WorldCondition, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                        worldCondition = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
                }

                Func<bool> spawnCondition = null;
                if (!string.IsNullOrEmpty(dEvent.MapCondition))
                {
                    var method = typeof(tsorcScriptedEvents).GetMethod(dEvent.MapCondition, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                        spawnCondition = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
                }

                Func<bool> condition = () => (worldCondition == null || worldCondition()) && (spawnCondition == null || spawnCondition());

                Func<ScriptedEvent, EventActionStatus> action = null;
                if (!string.IsNullOrEmpty(dEvent.CustomAction))
                {
                    var method = typeof(tsorcScriptedEvents).GetMethod(dEvent.CustomAction, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                        action = (Func<ScriptedEvent, EventActionStatus>)Delegate.CreateDelegate(typeof(Func<ScriptedEvent, EventActionStatus>), method);
                }

                // NpcIDs were already resolved from NpcName in the pass above; just collect them here.
                List<int> npcTypes = new List<int>();
                List<Vector2> npcCoords = new List<Vector2>();
                foreach (var npc in dEvent.Npcs)
                {
                    npcTypes.Add(npc.NpcID);
                    npcCoords.Add(new Vector2(npc.SpawnX, npc.SpawnY));
                }

                // Construct ScriptedEvent.
                // applyWorldTransform: false — by this point dEvent's coords have ALREADY been normalized to the
                // current world's runtime space (see the normalization block right after deserialize in this method),
                // so the ScriptedEvent must NOT transform them again. Save inverts back to legacy for the file.
                ScriptedEvent newEvent = new ScriptedEvent(
                    new Vector2(dEvent.CenterX, dEvent.CenterY),
                    (float)System.Math.Sqrt(dEvent.Radius) / 16f,
                    npcTypes,
                    npcCoords,
                    dEvent.TriggerDust,
                    dEvent.SaveOnCompletion,
                    dEvent.VisibleRing,
                    false, // bossEvent
                    string.IsNullOrEmpty(dEvent.TextToDisplay) ? "default" : dEvent.TextToDisplay,
                    ParseColor(dEvent.TextColorHex),
                    dEvent.Square,
                    condition,
                    action,
                    applyWorldTransform: false
                );

                for (int i = 0; i < dEvent.Npcs.Count; i++)
                {
                    var npc = dEvent.Npcs[i];
                    if (npc.CustomHealth.HasValue || npc.CustomDamage.HasValue || npc.CustomDefense.HasValue || npc.CustomSouls.HasValue)
                    {
                        newEvent.SetCustomStatsForOne(i, npc.CustomHealth, npc.CustomDefense, npc.CustomDamage, npc.CustomSouls);
                    }
                }

                if (dEvent.ExtraLootItems != null && dEvent.ExtraLootAmounts != null && dEvent.ExtraLootItems.Count == dEvent.ExtraLootAmounts.Count)
                {
                    newEvent.SetCustomDrops(dEvent.ExtraLootItems, dEvent.ExtraLootAmounts, true);
                }
                
                // Track the EventID internally so we can save it on completion!
                newEvent.DynamicEventID = dEvent.EventID;

                EnabledEvents.Add(newEvent);
            }

            // Remove any hardcoded events from EnabledEvents that have a corresponding dynamic event (within 2 tiles)
            EnabledEvents.RemoveAll(hardcoded => {
                if (!string.IsNullOrEmpty(hardcoded.DynamicEventID)) return false;
                foreach (var dyn in DynamicEvents)
                {
                    float dx = Math.Abs(dyn.CenterX - (hardcoded.centerpoint.X / 16f));
                    float dy = Math.Abs(dyn.CenterY - (hardcoded.centerpoint.Y / 16f));
                    if (dx < 2 && dy < 2)
                    {
                        return true;
                    }
                }
                return false;
            });
        }
        
        // ---- Expanded-world coordinate-space helpers for dynamic events ----------------------------------------
        // In-memory dynamic events are kept in the CURRENT world's runtime space; the JSON file is always LEGACY
        // 2000-space. These convert between the two. Identity on non-expanded worlds (MapTile/InverseMapTile no-op).

        private static void DynamicEventToRuntimeSpace(DynamicSpawnEvent e)
        {
            Vector2 c = ExpandedWorldTransform.MapTile(new Vector2(e.CenterX, e.CenterY));
            e.CenterX = c.X; e.CenterY = c.Y;
            if (e.Npcs != null)
                foreach (DynamicSpawnEntry n in e.Npcs)
                {
                    Vector2 s = ExpandedWorldTransform.MapTile(new Vector2(n.SpawnX, n.SpawnY));
                    n.SpawnX = s.X; n.SpawnY = s.Y;
                }
        }

        private static void DynamicEventToLegacySpace(DynamicSpawnEvent e)
        {
            Vector2 c = ExpandedWorldTransform.InverseMapTile(new Vector2(e.CenterX, e.CenterY));
            e.CenterX = c.X; e.CenterY = c.Y;
            if (e.Npcs != null)
                foreach (DynamicSpawnEntry n in e.Npcs)
                {
                    Vector2 s = ExpandedWorldTransform.InverseMapTile(new Vector2(n.SpawnX, n.SpawnY));
                    n.SpawnX = s.X; n.SpawnY = s.Y;
                }
        }

        // Serialize the (runtime-space) in-memory DynamicEvents to LEGACY-space JSON for on-disk storage.
        // Temporarily converts to legacy, serializes, then restores runtime (try/finally so a serialize error
        // can't leave the in-memory list in legacy space). No-op conversion on non-expanded worlds.
        private static string SerializeDynamicEventsToLegacyJson()
        {
            if (!ExpandedWorldTransform.Active)
                return JsonConvert.SerializeObject(DynamicEvents, Formatting.Indented);

            foreach (DynamicSpawnEvent e in DynamicEvents) DynamicEventToLegacySpace(e);
            try
            {
                return JsonConvert.SerializeObject(DynamicEvents, Formatting.Indented);
            }
            finally
            {
                foreach (DynamicSpawnEvent e in DynamicEvents) DynamicEventToRuntimeSpace(e);
            }
        }

        public static void SaveDynamicEvents()
        {
            string relativePath = "Content/DynamicEvents.json";
            string fullPath = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", relativePath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                string json = SerializeDynamicEventsToLegacyJson();
                File.WriteAllText(fullPath, json);
                LoadDynamicEvents();

                // Re-link the UI's CurrentEvent reference to the newly loaded object instance
                var configUI = ModContent.GetInstance<tsorcRevamp>().SpawnPointConfigUI;
                if (configUI != null && configUI.CurrentEvent != null)
                {
                    var newEvent = DynamicEvents.Find(ev => ev.EventID == configUI.CurrentEvent.EventID);
                    if (newEvent != null)
                    {
                        configUI.CurrentEvent = newEvent;
                        // Reload replaced every entry object too; rebind the edit panel to the live NPC entry
                        // so subsequent stat keystrokes persist instead of editing an orphaned instance.
                        configUI.RebindEditingNpc();
                    }
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<tsorcRevamp>().Logger.Error("Failed to save DynamicEvents.json: " + ex.Message);
            }
        }

        public static Color ParseColor(string hex)
        {
            // Default to white (not transparent black) so events with no saved color show readable spawn text.
            if (string.IsNullOrEmpty(hex)) return Color.White;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6 && hex.Length != 8) return Color.White;
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte a = 255;
            if (hex.Length == 8) a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r, g, b, a);
        }

        public static void DumpOldEventsToJson()
        {
            int addedCount = 0;
            foreach (var pair in ScriptedEventDict)
            {
                var ev = pair.Value;
                
                // Try to prevent dumping identical events if the user runs this multiple times.
                // If the event already exists, backfill any missing NpcName values (e.g. from a pre-fix dump).
                DynamicSpawnEvent existingEvent = null;
                foreach (var existing in DynamicEvents)
                {
                    if (existing.CenterX == (int)(ev.centerpoint.X / 16) && existing.CenterY == (int)(ev.centerpoint.Y / 16) && existing.Radius == ev.radius)
                    {
                        existingEvent = existing;
                        break;
                    }
                }
                if (existingEvent != null)
                {
                    if (ev.eventNPCs != null)
                    {
                        int count = Math.Min(existingEvent.Npcs.Count, ev.eventNPCs.Count);
                        for (int i = 0; i < count; i++)
                        {
                            string existing = existingEvent.Npcs[i].NpcName;
                            // Backfill if missing OR unresolvable. A whitespace check ("Eye of Cthulhu") isn't
                            // reliable — some old display names are a single word that just happens to differ from
                            // the real NPCID field name (e.g. "Skeletron" the display name vs "SkeletronHead" the
                            // field), so actually try to resolve it against ground truth instead of guessing.
                            if (string.IsNullOrEmpty(existing) || ResolveNpcType(existing) == 0)
                                existingEvent.Npcs[i].NpcName = GetNpcStableName(ev.eventNPCs[i].type);
                            // Fix NpcID=0 entries (mod wasn't loaded at first dump time, e.g. Thorium events).
                            if (existingEvent.Npcs[i].NpcID == 0 && ev.eventNPCs[i].type != 0)
                                existingEvent.Npcs[i].NpcID = ev.eventNPCs[i].type;
                        }
                    }
                    // Fix compiler-generated condition names (anonymous lambdas whose Method.Name contains "b__").
                    if (existingEvent.MapCondition?.Contains("b__") == true && ev.condition?.Method != null)
                        existingEvent.MapCondition = ev.condition.Method.Name;
                    continue;
                }

                var dynamicEvent = new DynamicSpawnEvent();
                dynamicEvent.EventID = Guid.NewGuid().ToString();
                dynamicEvent.CenterX = (int)(ev.centerpoint.X / 16);
                dynamicEvent.CenterY = (int)(ev.centerpoint.Y / 16);
                dynamicEvent.Radius = ev.radius;
                dynamicEvent.Square = ev.square;
                dynamicEvent.TriggerDust = ev.dustID;
                dynamicEvent.VisibleRing = ev.visible;
                dynamicEvent.SaveOnCompletion = ev.save;
                dynamicEvent.TextToDisplay = ev.eventText;
                dynamicEvent.TextColorHex = $"{ev.eventTextColor.R:X2}{ev.eventTextColor.G:X2}{ev.eventTextColor.B:X2}{ev.eventTextColor.A:X2}";
                
                if (ev.condition != null && ev.condition.Method != null)
                    dynamicEvent.MapCondition = ev.condition.Method.Name;
                if (ev.CustomAction != null && ev.CustomAction.Method != null)
                    dynamicEvent.CustomAction = ev.CustomAction.Method.Name;

                if (ev.eventNPCs != null)
                {
                    foreach (var npc in ev.eventNPCs)
                    {
                        var dynamicNpc = new DynamicSpawnEntry();
                        dynamicNpc.NpcID = npc.type;
                        dynamicNpc.NpcName = GetNpcStableName(npc.type);
                        dynamicNpc.SpawnX = npc.spawnCoords.X;
                        dynamicNpc.SpawnY = npc.spawnCoords.Y;
                        dynamicNpc.CustomHealth = npc.customHealth;
                        dynamicNpc.CustomDamage = npc.customDamage;
                        dynamicNpc.CustomDefense = npc.customDefense;
                        dynamicNpc.CustomSouls = npc.customSouls;
                        dynamicEvent.Npcs.Add(dynamicNpc);
                    }
                }

                if (ev.FinalNPCCustomDrops != null && ev.FinalNPCDropAmounts != null)
                {
                    dynamicEvent.ExtraLootItems = new List<int>(ev.FinalNPCCustomDrops);
                    dynamicEvent.ExtraLootItemNames = ev.FinalNPCCustomDrops.Select(GetItemStableName).ToList();
                    dynamicEvent.ExtraLootAmounts = new List<int>(ev.FinalNPCDropAmounts);
                }

                DynamicEvents.Add(dynamicEvent);
                addedCount++;
            }

            SaveDynamicEvents();
            UsefulFunctions.BroadcastText($"Dumped {addedCount} events to JSON.");
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
                if (Main.player[index].HeldItem.type != ModContent.ItemType<Items.Debug.EnemyDebugTome>())
                {
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
                                if (EnabledEvents[i].eventNPCs != null && EnabledEvents[i].eventNPCs.Count > 0)
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
                                    // NPC events burst dust at spawn time (end of the telegraph) instead of here at trigger.
                                    if (EnabledEvents[i].visible && EnabledEvents[i].noNPCEvent)
                                    {
                                        for (int j = 0; j < 100; j++)
                                        {
                                            Dust.NewDustPerfect(EnabledEvents[i].centerpoint, EnabledEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                        }
                                    }
                                    RunningEvents.Add(EnabledEvents[i]);
                                    EnabledEvents.RemoveAt(i);
                                    i--;
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
                                    // NPC events burst dust at spawn time (end of the telegraph) instead of here at trigger.
                                    if (EnabledEvents[i].noNPCEvent)
                                    {
                                        for (int j = 0; j < 100; j++)
                                        {
                                            Dust.NewDustPerfect(EnabledEvents[i].centerpoint, EnabledEvents[i].dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3);
                                        }
                                    }

                                    RunningEvents.Add(EnabledEvents[i]);
                                    EnabledEvents.RemoveAt(i);
                                    i--;
                                }
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
            for (int i = RunningEvents.Count - 1; i >= 0; i--)
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
        public string DynamicEventID { get; set; } = null;
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
                if (eventNPCs == null || eventNPCs.Count == 0)
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

        public ScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs = null, List<Vector2> coords = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, bool bossEvent = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<ScriptedEvent, EventActionStatus> customAction = null, bool applyWorldTransform = true)
        {
            ConstructScriptedEvent(rangeCenterpoint, rangeRadius, npcs, coords, DustType, saveEvent, visibleRange, bossEvent, flavorText, flavorTextColor, squareRange, customCondition, customAction, applyWorldTransform);
        }

        public void ConstructScriptedEvent(Vector2 rangeCenterpoint, float rangeRadius, List<int> npcs = null, List<Vector2> coords = null, int DustType = 31, bool saveEvent = false, bool visibleRange = false, bool bossEvent = false, string flavorText = "default", Color flavorTextColor = new Color(), bool squareRange = false, Func<bool> customCondition = null, Func<ScriptedEvent, EventActionStatus> customAction = null, bool applyWorldTransform = true)
        {
            //Expanded Adventure (2400-tall) coordinate transform. Hardcoded events pass their coords in LEGACY
            //(2000-space), so route them through ExpandedWorldTransform: identity on legacy/remix/sandbox (inactive),
            //+200/+400 on the expanded world. World-gated, so shared events (no map condition) and remix events
            //auto-resolve correctly — remix events only run on the 2000-tall remix world where the transform is identity.
            //TWO exclusions:
            //  (1) applyWorldTransform == false: DYNAMIC / tome-authored events. These are authored in-place on the
            //      current world, so their stored coords are ALREADY in that world's space. Transforming them would
            //      double-shift (e.g. a WitchKing dynamic event saved at 2195 would reload at 2595).
            //  (2) expandedNative: hardcoded events gated ExpandedAdventureMapCondition are authored directly in
            //      2400-space and likewise must not be shifted.
            bool expandedNative = customCondition == tsorcScriptedEvents.ExpandedAdventureMapCondition || customCondition == tsorcScriptedEvents.ExpandedAdventureMapConditionSHM;
            if (applyWorldTransform && !expandedNative)
            {
                rangeCenterpoint = ExpandedWorldTransform.MapTile(rangeCenterpoint);
                if (coords != null)
                {
                    for (int i = 0; i < coords.Count; i++)
                    {
                        coords[i] = ExpandedWorldTransform.MapTile(coords[i]);
                    }
                }
            }

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
            //If this is its first time running, display the text
            if (eventTimer == 0)
            {
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

            // Fill spawn area with smoke dust warning effect for 1 second (60 frames) before actual spawn
            if (!noNPCEvent && eventTimer < 60)
            {
                for (int j = 0; j < eventNPCs.Count; j++)
                {
                    // Size the smoke cloud to the NPC's bounding box (×1.25) so the telegraph matches the enemy that appears.
                    NPC sizeRef = new NPC();
                    sizeRef.SetDefaults(eventNPCs[j].type);
                    int boxW = (int)(sizeRef.width * 1.25f);
                    int boxH = (int)(sizeRef.height * 1.25f);

                    // NPC.NewNPC places the sprite centered horizontally and bottom-aligned at (spawnCoords*16).
                    Vector2 spawnBottom = new Vector2(eventNPCs[j].spawnCoords.X * 16, eventNPCs[j].spawnCoords.Y * 16);
                    Vector2 boxTopLeft = spawnBottom - new Vector2(boxW / 2f, boxH);
                    float dustScale = MathHelper.Clamp(System.Math.Max(boxW, boxH) / 32f, 1f, 3f);

                    for (int k = 0; k < 2; k++)
                    {
                        int dust = Dust.NewDust(boxTopLeft, boxW, boxH, DustID.Smoke, 0f, 0f, 100, Color.LightGray, dustScale);
                        Main.dust[dust].velocity *= 0.4f;
                        Main.dust[dust].velocity.Y -= 0.6f; // float up slightly
                        Main.dust[dust].noGravity = true;
                    }
                }
            }

            // Spawn the NPCs after the 1-second warning effect
            if (eventTimer == 60)
            {
                if (!noNPCEvent)
                {
                    SpawnNPCs();
                }
            }

            //If it has a custom action, then run it (ensure NPCs have spawned first if applicable)
            //If it returns EndAction, mark its action as finished and do not run it again
            //If it returns FailedEvent then immediately mark the event as failed and end it
            //If it returns CompletedEvent then immediately mark the event as completed and end it
            if (hasCustomAction && !finishedCustomAction && (noNPCEvent || eventTimer >= 60))
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

            //Only perform these checks if an event has NPCs and they have actually spawned
            //No NPC events must be ended by their actions
            if (!noNPCEvent && eventTimer > 60)
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
                            // Skip the diagnostic for SelfDeactivatingNPCs (Marilith/Prime intros, etc.) — their
                            // active=false here is the intentional transform-into-boss trigger, not a real despawn,
                            // so this branch firing for them is expected and not worth alarming the player about.
                            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode && !tsorcRevamp.SelfDeactivatingNPCs.Contains(eventNPCs[i].type))
                            {
                                Main.NewText($"[Event] Torn down: NPC #{i} (type {eventNPCs[i].type}) " +
                                    $"active={eventNPCs[i].npc.active}, type now {eventNPCs[i].npc.type}", Color.Orange);
                            }
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
                // Diagnostic: a type of 0 means the NpcName failed to resolve; the "spawn" would be a no-op.
                if (eventNPCs[i].type == 0)
                {
                    if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                        Main.NewText($"[Event] NPC #{i} failed to resolve (type 0) — check its NpcName in DynamicEvents.json", Color.OrangeRed);
                    continue;
                }

                eventNPCs[i].index = NPC.NewNPC(new EntitySource_Misc("Scripted Event"), (int)eventNPCs[i].spawnCoords.X * 16, (int)eventNPCs[i].spawnCoords.Y * 16, eventNPCs[i].type);

                if (eventNPCs[i].index >= Main.maxNPCs)
                {
                    if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                        Main.NewText($"[Event] NPC.NewNPC failed for type {eventNPCs[i].type} (no free slot)", Color.OrangeRed);
                    continue;
                }

                NPC thisNPC = eventNPCs[i].npc;

                // Burst of trigger dust as the enemy materializes — the payoff at the end of the smoke telegraph.
                // (The old burst fired at trigger time; it now coincides with the actual spawn.)
                for (int d = 0; d < 50; d++)
                {
                    Dust.NewDustPerfect(thisNPC.Center, dustID, new Vector2(Main.rand.Next(-10, 10), Main.rand.Next(-10, 10)), 200, default, 3).noGravity = true;
                }

                thisNPC.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().ScriptedEventOwner = this;
                thisNPC.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().ScriptedEventIndex = i;

                // Keep event NPCs from despawning on their own (e.g. dungeon/surface enemies fleeing at dawn via
                // timeLeft). CheckActive blocks the distance-based despawn, but not timeLeft. If even one event NPC
                // despawns, the all-or-nothing alive check tears the whole (multi-NPC) event down — so pin them.
                thisNPC.timeLeft = int.MaxValue;

                if (eventNPCs[i].customHealth != null)
                {
                    thisNPC.lifeMax = eventNPCs[i].customHealth.Value;
                    thisNPC.life = eventNPCs[i].customHealth.Value;
                }
                if (eventNPCs[i].customDefense != null)
                {
                    // Set the default backing field too: mod code (debuffs, scaling) recomputes npc.defense from
                    // npc.defDefense, which would otherwise reset the custom value back to the vanilla base.
                    thisNPC.defense = eventNPCs[i].customDefense.Value;
                    thisNPC.defDefense = eventNPCs[i].customDefense.Value;
                }
                if (eventNPCs[i].customDamage != null)
                {
                    thisNPC.damage = eventNPCs[i].customDamage.Value;
                    thisNPC.defDamage = eventNPCs[i].customDamage.Value;
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
                    if (DynamicEventID != null)
                    {
                        tsorcRevampWorld.CompletedDynamicEvents.Add(DynamicEventID);
                    }
                    else
                    {
                        foreach (KeyValuePair<tsorcScriptedEvents.ScriptedEventType, ScriptedEvent> pair in tsorcScriptedEvents.ScriptedEventDict)
                        {
                            if (pair.Value == this)
                            {
                                tsorcScriptedEvents.ScriptedEventValues[pair.Key] = true;
                            }
                        }
                    }
                }
                else
                {
                    // Non-permanent event completed: park in DisabledEvents until the player dies and respawns.
                    // QueuedEvents auto-restores after a 5-second timer which is too fast for Dark Souls intent.
                    tsorcScriptedEvents.DisabledEvents.Add(this);
                }
            }
            //Otherwise if it wasn't completed, then despawn the NPC's and re-add it to QueuedEvents to be re-initialized once the player respawns
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

    public class DynamicSpawnEntry
    {
        // Resolved from NpcName each load. Normally NOT written to JSON (avoids modded-ID drift across builds),
        // but IS written as a fallback when NpcName is missing so a legacy entry never loses its identity.
        public int NpcID { get; set; }
        public bool ShouldSerializeNpcID() => string.IsNullOrEmpty(NpcName);

        /// <summary>Stable identifier. Modded: "tsorcRevamp/LeonhardPhase1". Vanilla: NPCID field name e.g. "EyeofCthulhu". Resolved to a runtime ID by LoadDynamicEvents.</summary>
        public string NpcName { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public int? CustomHealth { get; set; }
        public int? CustomDamage { get; set; }
        public int? CustomDefense { get; set; }
        public int? CustomSouls { get; set; }
    }

    public class DynamicSpawnEvent
    {
        public string EventID { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Radius { get; set; }
        public bool Square { get; set; }
        public int TriggerDust { get; set; }
        public bool VisibleRing { get; set; }
        public bool SaveOnCompletion { get; set; }
        public string TextToDisplay { get; set; }
        public string TextColorHex { get; set; }
        // World-level gate (Adventure Map Only / Remix Map Only / "" = Always). ANDed with MapCondition at runtime.
        public string WorldCondition { get; set; }
        // Spawn condition (progression / time-of-day gate). "" = None (no extra restriction).
        public string MapCondition { get; set; }
        public string CustomAction { get; set; }
        // True for "Quick Add" events: a single NPC acts as the event marker/center (no book icon drawn),
        // and the event is locked to exactly one NPC.
        public bool SingleNpcMarker { get; set; }
        public List<DynamicSpawnEntry> Npcs { get; set; } = new List<DynamicSpawnEntry>();

        // Optional custom drops, mostly used for simple single-item drops.
        public List<int> ExtraLootItems { get; set; }
        public bool ShouldSerializeExtraLootItems() => ExtraLootItems != null && (ExtraLootItemNames == null || ExtraLootItemNames.Count == 0);
        public List<string> ExtraLootItemNames { get; set; }
        public bool ShouldSerializeExtraLootItemNames() => ExtraLootItemNames != null && ExtraLootItemNames.Count > 0;
        public List<int> ExtraLootAmounts { get; set; }
        public bool ShouldSerializeExtraLootAmounts() => ExtraLootAmounts != null;
    }
}
