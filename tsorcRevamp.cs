using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using tsorcRevamp.Items;
using tsorcRevamp.UI;
using static tsorcRevamp.ILEdits;
using static tsorcRevamp.MethodSwaps;

namespace tsorcRevamp
{


    public class tsorcRevamp : Mod
    {

        public static ModKeybind toggleDragoonBoots;
        public static ModKeybind reflectionShiftKey;
        public static bool isAdventureMap = false;
        public static int DarkSoulCustomCurrencyId;
        internal bool UICooldown = false;
        internal bool worldButtonClicked = false;
        public static List<int> KillAllowed;
        public static List<int> PlaceAllowed;
        public static List<int> Unbreakable;
        public static List<int> IgnoredTiles;
        public static List<int> CrossModTiles;
        public static List<int> PlaceAllowedModTiles;

        internal BonfireUIState BonfireUIState;
        internal UserInterface _bonfireUIState; //"but zeo!", you say
        internal DarkSoulCounterUIState DarkSoulCounterUIState;
        internal UserInterface _darkSoulCounterUIState; //"prefacing a name with an underscore is supposed to be for private fields!"
        internal UserInterface EmeraldHeraldUserInterface;
        internal EstusFlaskUIState EstusFlaskUIState;
        internal UserInterface _estusFlaskUIState; //"but reader! i dont care!" says zeo
        internal PotionBagUIState PotionUIState;
        internal UserInterface PotionBagUserInterface;
        internal CustomMapUIState DownloadUIState;
        internal UserInterface DownloadUI;

        public static FieldInfo AudioLockInfo;
        public static FieldInfo ActiveSoundInstancesInfo;
        public static FieldInfo AreSoundsPausedInfo;
        public static FieldInfo TrackedSoundsInfo;

        public static Effect TheAbyssEffect;
        //public static Effect AttraidiesEffect;

        public static bool MusicNeedsUpdate = false;
        public static bool justUpdatedMusic = false;
        public static bool ReloadNeeded = false;
        public static bool SpecialReloadNeeded = false;
        public static bool DownloadingMusic = false;
        public static float MusicDownloadProgress = 0;
        public static ModKeybind DodgerollKey;
        //public static ModHotKey SwordflipKey;

        internal static bool[] CustomDungeonWalls;

        public static bool ActuationBypassActive = false;

        public override void Load()
        {
            toggleDragoonBoots = KeybindLoader.RegisterKeybind(this, "Dragoon Boots", Microsoft.Xna.Framework.Input.Keys.Z);
            reflectionShiftKey = KeybindLoader.RegisterKeybind(this, "Reflection Shift", Microsoft.Xna.Framework.Input.Keys.O);
            DodgerollKey = KeybindLoader.RegisterKeybind(this, "Dodge Roll", Microsoft.Xna.Framework.Input.Keys.LeftAlt);
            //SwordflipKey = KeybindLoader.RegisterKeybind(this, "Sword Flip", Microsoft.Xna.Framework.Input.Keys.P);

            DarkSoulCustomCurrencyId = CustomCurrencyManager.RegisterCurrency(new DarkSoulCustomCurrency(ModContent.ItemType<SoulShekel>(), 99999L));

            BonfireUIState = new BonfireUIState();
            if (!Main.dedServ) BonfireUIState.Activate();
            _bonfireUIState = new UserInterface();
            if (!Main.dedServ) _bonfireUIState.SetState(BonfireUIState);

            DarkSoulCounterUIState = new DarkSoulCounterUIState();
            //if (!Main.dedServ) DarkSoulCounterUIState.Activate();
            _darkSoulCounterUIState = new UserInterface();
            if (!Main.dedServ) _darkSoulCounterUIState.SetState(DarkSoulCounterUIState);

            EstusFlaskUIState = new EstusFlaskUIState();
            //if (!Main.dedServ) EstusFlaskUIState.Activate();
            _estusFlaskUIState = new UserInterface();
            if (!Main.dedServ) _estusFlaskUIState.SetState(EstusFlaskUIState);

            PotionUIState = new PotionBagUIState();
            PotionBagUserInterface = new UserInterface();
            if (!Main.dedServ) PotionBagUserInterface.SetState(PotionUIState);

            
            DownloadUIState = new CustomMapUIState();
            DownloadUI = new UserInterface();
            if (!Main.dedServ) DownloadUI.SetState(DownloadUIState);
            

            ApplyMethodSwaps();
            ApplyILs();
            PopulateArrays();
            if (!Main.dedServ)
            {
                Main.QueueMainThreadAction(TransparentTextureHandler.TransparentTextureFix);
            }

            if (!Main.dedServ)
            {
                tsorcRevamp Instance = this;

                    TheAbyssEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TheAbyssShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    Filters.Scene["tsorcRevamp:TheAbyss"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(TheAbyssEffect), "TheAbyssShaderPass").UseImage("Images/Misc/noise"), EffectPriority.Low);
                    

                //AttraidiesEffect = Instance.GetEffect("Effects/ScreenFilters/AttraidiesShader");
                //Filters.Scene["tsorcRevamp:AttraidiesShader"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(AttraidiesEffect), "AttraidiesShaderPass").UseImage("Images/Misc/noise"), EffectPriority.Low);

                EmeraldHeraldUserInterface = new UserInterface();
            }

            /*
            if (!Main.dedServ)
            {
                Main.instance.LoadNPC(NPCID.TheDestroyer);
                TextureAssets.Npc[NPCID.TheDestroyer] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_134");
                Main.instance.LoadNPC(NPCID.TheDestroyerBody);
                TextureAssets.Npc[NPCID.TheDestroyerBody] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_135");
                Main.instance.LoadNPC(NPCID.TheDestroyerTail);
                TextureAssets.Npc[NPCID.TheDestroyerTail] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_136");
                Main.instance.LoadGore(156);
                TextureAssets.Gore[156] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/Gore_156");
                Main.instance.LoadNPC(NPCID.Probe);
                TextureAssets.Npc[NPCID.Probe] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_139");
            }*/

            UpdateCheck();
        }

        private void PopulateArrays()
        {
            #region KillAllowed list
            KillAllowed = new List<int>() {
                2, //grass
                3, //small plants
                4, // torch
                5, //tree trunk
                6, //iron
                7, //copper
                9, //silver
                10, //closed door
                11, //open door
                12, //Heart crystal
                13, //bottles and jugs
                14, //table
                15, //chairs
                16, //anvil
                17, //furnance
                18, //workbench
                20, //sapling
                21, //chests
                23, //corruption grass
                24, //small corruption plants
                27, //sunflower
                28, //pot
                29, //piggy bank
                31, //shadow orb
                32, //corruption barbs
                33, //candle
                34, //bronze chandellier
                35, //silver c
                36, //gold c
                37, //meteorite
                42, //chain lantern
                49, //water candle
                50, //books
                51, //cobweb
                52, //vines
                53, //sand
                55, //Sign 
                //56, //obsidian (removed at tim's request)
                60, //jungle grass
                61, //small jungle plants
                62, //jungle vines
                67, //Amethyst 
                69, //thorns
                72, //mushroom stalks
                71, //small mushrooms
                73, //plants
                74, //plants
                78, //clay pot
                //79, //bed
                80, //cactus
                81, //corals
                82, //new herbs
                83, //grown herbs
                84, //bloomed herbs
                85, //tombstone
                86, //loom
                87, //piano
                88, //drawer
                89, //bench
                90, //bathtub
                91, //banner
                92, //lamp post
                93, //tiki torch
                94, //keg
                95, //chinese lantern
                96, //cooking pot
                97, //safe
                98, //skull candle
                99, //trash can
                100, //candlabra
                101, //bookcase
                102, //throne
                103, //bowl
                104, //grandfather clock
                //105, //statue
                106, //sawmill
                107, //cobalt
                108, //mythril
                109, //Hallowed Grass
                110, //Hallowed Plants 
                111, //adamantite
                112, //ebonsand
                114, //tinkerer's workbench
                115, //Hallowed Vines 
                116, //pearlsand
                125, //crystal ball
                126, //discoball
                128, //mannequin
                129, //crystal shard
                133, //adamantite forge
                134, //mythril anvil
                138, //boulder
                141, //explosives
                165, //ambient objects (stalagmites / stalactites, icicles)
                172, //sinks
                173, //platinum candelabra
                174, //platinum candle
                184, //moss growth
                185, //ambient objects (small rocks, small coin stashes, gem stashes)
                186, //ambient objects (bone piles, large rocks, large coin stashes)
                187, //ambient objects (mossy / lava rocks, spider eggs, misc bg furniture (cave tents, etc)) sword shrine
                201, //tall grass (crimson)
                205, //crimson vines
                207, //water fountain
                211, //Chlorophyte Ore
                218, //meat grinder
                227, //strange plant
                228, //dye vat
                233, ////ambient objects (background leafy jungle plants)
                238, //Plantera Bulb
                240, //trophies
                242, //big paintings
                245, //tall paintings
                246, //wide paintings
                270, //firefly in a bottle
                271, //lightning bug in a bottle
                275, //bunny cage
                276, //squirrel cage
                277, //mallard cage
                278, //duck cage
                279, //bird cage
                280, //blue jay cage
                281, //cardinal cage
                282, //fish bowl
                283, //heavy work bench
                285, //snail cage
                286, //glowing snail cage
                287, //ammo box
                288, //monarch jar
                289, //purple emperor jar
                290, //red admiral jar
                291, //ulysses jar
                292, //sulphur jar
                293, //tree nymph jar
                294, //zebra swallowtail jar
                295, //julia jar
                296, //scorpion cage
                297, //black scorpion cage
                298, //frog cage
                299, //mouse cage
                300, //bone welder
                301, //flesh cloning vat
                302, //glass kiln
                307, //steampunk boiler
                309, //penguin cage
                310, //worm cage
                316, //blue jellyfish jar
                317, //green jellyfish jar
                318, //pink jellyfish jar
                319, //ship in a bottle
                310, //seaweed planter
                324, //seashell variants
                337, //number and letter statues
                339, //grasshopper cage
                352, //crimson thorn
                354, //bewitching table
                355, //alchemy table
                358, //gold bird cage
                359, //gold bunny cage
                360, //gold butterfly jar
                361, //gold frog cage
                362, //gold grasshopper cage
                363, //gold mouse cage
                364, //gold worm cage
                372, //peace candle
                377, //sharpening station
                378, //target dummy
                382, //flower vines
                390, //lava lamp
                391, //enchanted nightcrawler cage
                392, //buggy cage
                393, //grubby cage
                394, //sluggy cage
                413, //red squirrel cage
                414, //gold squirrel cage
                463, //defenders forge
                TileID.Titanium,
                TileID.Pumpkins, //the harvestable kind, not the block
                TileID.BreakableIce

            };

            #endregion
            //--------
            #region PlaceAllowed list
            PlaceAllowed = new List<int>() {
                4,  //torch
                10, //Closed Door
                11, //Open Door  
                13, //bottles
                15, //chairs
                16, //anvil
                17, //furnace
                18, //workbench
                20, //sapling
                21, //chests
                27, //sunflower
                28, //pot
                29, //piggy bank
                33, //candle
                34, //bronze chandellier
                35, //silver chandellier
                36, //gold chandellier
                42, //chain lantern
                49, //water candle
                50, //books
                73, //plants
                74, //plants
                78, //clay pot
                //79, //bed
                81, //corals
                82, //new herbs
                83, //grown herbs
                84, //bloomed herbs
                85, //tombstone
                86, //loom
                87, //piano
                88, //drawer
                89, //bench
                90, //bathtub
                91, //banner
                92, //lamp post
                93, //tiki torch
                94, //keg
                95, //chinese lantern
                96, //cooking pot
                97, //safe
                98, //skull candle
                99, //trash can
                100, //candlabra
                101, //bookcase
                102, //throne
                103, //bowl
                104, //grandfather clock
                105, //statue
                106, //sawmill
                114, //tinkerer's workbench
                125, //crystal ball
                126, //discoball
                128, //mannequin
                129, //crystal shard
                132, //lever
                133, //adamantite forge
                134, //mythril anvil
                149, //festive lights
                172, //sinks
                173, //platinum candelabra
                174, //platinum candle
                207, //water fountain
                218, //meat grinder
                228, //dye vat
                240, //trophies
                242, //big paintings
                245, //tall paintings
                246, //wide paintings
                270, //firefly in a bottle
                271, //lightning bug in a bottle
                275, //bunny cage
                276, //squirrel cage
                277, //mallard cage
                278, //duck cage
                279, //bird cage
                280, //blue jay cage
                281, //cardinal cage
                282, //fish bowl
                283, //heavy work bench
                285, //snail cage
                286, //glowing snail cage
                287, //ammo box
                288, //monarch jar
                289, //purple emperor jar
                290, //red admiral jar
                291, //ulysses jar
                292, //sulphur jar
                293, //tree nymph jar
                294, //zebra swallowtail jar
                295, //julia jar
                296, //scorpion cage
                297, //black scorpion cage
                298, //frog cage
                299, //mouse cage
                300, //bone welder
                301, //flesh cloning vat
                302, //glass kiln
                307, //steampunk boiler
                309, //penguin cage
                310, //worm cage
                316, //blue jellyfish jar
                317, //green jellyfish jar
                318, //pink jellyfish jar
                319, //ship in a bottle
                310, //seaweed planter
                324, //seashell variants
                337, //number and letter statues
                339, //grasshopper cage
                354, //bewitching table
                355, //alchemy table
                358, //gold bird cage
                359, //gold bunny cage
                360, //gold butterfly jar
                361, //gold frog cage
                362, //gold grasshopper cage
                363, //gold mouse cage
                364, //gold worm cage
                372, //peace candle
                377, //sharpening station
                378, //target dummy
                390, //lava lamp
                391, //enchanted nightcrawler cage
                392, //buggy cage
                393, //grubby cage
                394, //sluggy cage
                413, //red squirrel cage
                414, //gold squirrel cage
                463, //defender's forge
                TileID.Pumpkins //the harvestable kind, not the block

            };
            #endregion
            //--------
            #region Unbreakable list
            Unbreakable = new List<int>() {
                19, //Wood platform
                55, //sign
                105, //statues
                132, //lever
                130, //active stone block
                131, //inactive stone block
                135, //pressure plates
                136, //switch
                137, //dart trap
                TileID.Rope,
                TileID.Chain,
                TileID.VineRope,
                TileID.SilkRope,
                TileID.WebRope,
                TileID.LunarMonolith //410
            };
            #endregion
            //--------
            #region IgnoredTiles list
            IgnoredTiles = new List<int>() {
                3, //tall grass
                24, //tall grass (corruption)
                32, //corruption thorn
                51, //cobweb
                52, //vines
                61, //tall grass (jungle)
                62, //jungle vines
                69, //jungle thorn
                73, //tall grass (misc)
                74, //tall jungle plants
                82, //plants (growing)
                83, //plants (matured)
                84, //plants (blooming)
                85, //tombstone
                115, //hallowed vines
                129, //crystal shard
                165, //ambient objects (stalagmites / stalactites, icicles)
                184, //moss growth
                185, //ambient objects (small rocks, small coin stashes, gem stashes)
                186, //ambient objects (bone piles, large rocks, large coin stashes)
                187, //ambient objects (mossy / lava rocks, spider eggs, misc bg furniture (cave tents, etc))
                201, //tall grass (crimson)
                205, //crimson vines
                227, //strange plant
                233, //ambient objects (background leafy jungle plants)
                324, //sea shells
                352, //crimson thorn
                382 //flower vines
            };
            #endregion
            //--------
            #region CrossModTiles list
            CrossModTiles = new List<int>();

            Mod MagicStorage;
            if (ModLoader.TryGetMod("MagicStorage", out MagicStorage))
            {
                CrossModTiles.Add(MagicStorage.Find<ModTile>("CraftingAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("RemoteAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageComponent").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageHeart").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageUnit").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageConnector").Type);
            }

            Mod MagicStorageExtra;
            if (ModLoader.TryGetMod("MagicStorageExtra", out MagicStorageExtra))
            {
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("CraftingAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("CreativeStorageUnit").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("RemoteAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageComponent").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageHeart").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageUnit").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageConnector").Type);
            }
            #endregion
            //--------
            #region PlaceAllowedModTiles list
            PlaceAllowedModTiles = new List<int>()
            {
                Find<ModTile>("EnemyBannerTile").Type,
                Find<ModTile>("SweatyCyclopsForge").Type

            };

            #endregion
            //--------
            #region CustomDungeonTiles list
            CustomDungeonWalls = new bool[231];
            for (int i = 0; i < 231; i++)
            {
                CustomDungeonWalls[i] = false;
            }
            CustomDungeonWalls[0] = true; //no wall
            CustomDungeonWalls[34] = true; //sandstone brick wall
            CustomDungeonWalls[63] = true; //flower wall
            CustomDungeonWalls[65] = true; //grass wall
            CustomDungeonWalls[71] = true; //ice wall
            #endregion
        }

        public override void Unload()
        {
            toggleDragoonBoots = null;
            reflectionShiftKey = null;
            KillAllowed = null;
            PlaceAllowed = null;
            Unbreakable = null;
            IgnoredTiles = null;
            tsorcRevampWorld.Slain = null;
            //the following sun and moon texture changes are failsafes. they should be set back to default in PreSaveAndQuit 
            TextureAssets.Sun = ModContent.Request<Texture2D>("Terraria/Images/Sun", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            TextureAssets.Sun2 = ModContent.Request<Texture2D>("Terraria/Images/Sun2");
            TextureAssets.Sun3 = ModContent.Request<Texture2D>("Terraria/Images/Sun3");

            for (int i = 0; i < TextureAssets.Moon.Length; i++)
            {
                TextureAssets.Moon[i] = ModContent.Request<Texture2D>("Terraria/Images/Moon_" + i);
            }
            DarkSoulCounterUIState.ConfigInstance = null;

            TransparentTextureHandler.TransparentTextures.Clear();
            TransparentTextureHandler.TransparentTextures = null;

            /*
            for (int m = 1; m < Main.maxMusic; m++)
            {
                if (Main.music[m] != null)
                {
                    if (Main.music[m].IsPlaying)
                    {
                        Main.music[m].Stop(Microsoft.Xna.Framework.Audio.AudioStopOptions.Immediate);
                    }
                }
            }*/

            UnloadILs();
            CustomDungeonWalls = null;
            DodgerollKey = null;
            //SwordflipKey = null;

            /* IIRC this was to change the Destroyer's texture, which was never fully implemented?
             * I think there's a new way to do it now though
            if (!Main.dedServ)
            {
                Main.NPCLoaded[NPCID.TheDestroyer] = false;
                Main.NPCLoaded[NPCID.TheDestroyerBody] = false;
                Main.NPCLoaded[NPCID.TheDestroyerTail] = false;
                Main.NPCLoaded[NPCID.Probe] = false;
                Main.goreLoaded[156] = false;
            }*/
        }
        public override void AddRecipes()
        {
            ModRecipeHelper.AddRecipes();

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                RecipeHelper.EditRecipes();
            }
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            int message = reader.ReadByte(); //(byte) 1;

            //Sync Soul Slot
            if (message == tsorcPacketID.SyncSoulSlot)
            {
                byte player = reader.ReadByte(); //player.whoAmI;
                tsorcRevampPlayer modPlayer = Main.player[player].GetModPlayer<tsorcRevampPlayer>();
                modPlayer.SoulSlot.Item = ItemIO.Receive(reader);
                if (Main.netMode == NetmodeID.Server)
                {
                    modPlayer.SendSingleItemPacket(1, modPlayer.SoulSlot.Item, -1, whoAmI);
                }
            }

            //Sync Event Dust
            else if (message == tsorcPacketID.SyncEventDust)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    tsorcScriptedEvents.NetworkEvents = new List<NetworkEvent>();

                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 center = reader.ReadVector2();
                        float radius = reader.ReadSingle();
                        int dustID = reader.ReadInt32();
                        bool square = reader.ReadBoolean();

                        tsorcScriptedEvents.NetworkEvents.Add(new NetworkEvent(center, radius, dustID, square));
                    }
                }
            }

            //Sync time change
            else if (message == tsorcPacketID.SyncTimeChange)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    Main.dayTime = !Main.dayTime;
                    Main.time = 0;

                    if (Main.dayTime)
                    {
                        UsefulFunctions.BroadcastText("You shift time forward and a new day begins...", new Color(175, 75, 255));
                    }
                    else UsefulFunctions.BroadcastText("You shift time forward and a new night begins...", new Color(175, 75, 255));

                    NetMessage.SendData(MessageID.WorldData);
                }
            }

            else if (message == tsorcPacketID.DispelShadow)
            {
                int npcID = reader.ReadInt32();
                Main.npc[npcID].AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
            }

            else if (message == tsorcPacketID.DropSouls)
            {
                Vector2 position = reader.ReadVector2();
                int count = reader.ReadInt32();
                if (Main.netMode == NetmodeID.Server)
                {
                    UsefulFunctions.BroadcastText("Dropping " + count + "souls");
                    //You can not drop items in a stack larger than 32766 in multiplayer, because the stack size gets converted to a short when syncing
                    while (count > 32000)
                    {
                        //UsefulFunctions.ServerText("Dropping " + 32000 + "souls");
                        Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), position + Main.rand.NextVector2Circular(10, 10), ModContent.ItemType<Items.DarkSoul>(), 32000);
                        count -= 32000;
                    }

                    Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), position, ModContent.ItemType<Items.DarkSoul>(), count);
                    //UsefulFunctions.NewItemInstanced(position, new Vector2(1, 1), ModContent.ItemType<Items.DarkSoul>(), count);
                }
            }

            else if (message == tsorcPacketID.SyncPlayerDodgeroll)
            {
                //First, check whether this is a new packet originating from a client who just rolled, or a packet that has been bounced by the server to the other clients
                bool bounced = reader.ReadBoolean();
                byte who = reader.ReadByte();

                //If we're a client, go ahead and sync based on it. If we're the server, only sync and bounce it if this is a new packet
                if (Main.netMode == NetmodeID.MultiplayerClient || !bounced)
                {
                    //Sync everything
                    Player player = Main.player[who];
                    tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                    modPlayer.forceDodgeroll = true;
                    modPlayer.wantedDodgerollDir = reader.ReadSByte();
                    player.velocity = reader.ReadVector2();

                    //If we're the server in specific, bounce it to the other clients, passing "true" as the bounced flag to ensure this only happens once
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket rollPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                        rollPacket.Write(tsorcPacketID.SyncPlayerDodgeroll);
                        rollPacket.Write(true);
                        rollPacket.Write((byte)player.whoAmI);
                        rollPacket.Write(modPlayer.wantedDodgerollDir);
                        rollPacket.WriteVector2(player.velocity);

                        //Iterate through all active clients and send it specifically to them
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active && i != player.whoAmI)
                            {
                                rollPacket.Send(i);
                            }
                        }
                    }
                }
            }
            else if (message == tsorcPacketID.SyncBonfire)
            {
                if (tsorcRevampWorld.LitBonfireList == null)
                {
                    tsorcRevampWorld.LitBonfireList = new List<Vector2>();
                }

                Vector2 bonfireLocation = reader.ReadVector2();
                if (!tsorcRevampWorld.LitBonfireList.Contains(bonfireLocation))
                {
                    tsorcRevampWorld.LitBonfireList.Add(bonfireLocation);
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }

            }


            /**
            //For synced random
            //Recieves the seed from the server, and passes it off to UsefulFunctions.RecieveRandPacket which uses it to instantiate the new random generator
            else if (message == tsorcPacketID.SyncRandom)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    byte seed = reader.ReadByte();
                    Main.NewText("Client recieved seed:" + seed);
                    UsefulFunctions.RecieveRandPacket(seed);
                }
            }**/

            else
            {
                Logger.InfoFormat("[tsorcRevamp] Sync failed. Unknown message ID: {0}", message);
            }
        }

        public override void PostSetupContent()
        {
            #region Boss Checklist Compatibility
            Mod bossChecklist;
            if (ModLoader.TryGetMod("BossChecklist", out bossChecklist)){  //See https://github.com/JavidPack/BossChecklist/wiki/Support-using-Mod-Call for instructions


                // AddBoss, bossname, order or value in terms of vanilla bosses, inline method for retrieving downed value.
                /*
                public const float SlimeKing = 1f;
                public const float EyeOfCthulhu = 2f;
                public const float EaterOfWorlds = 3f;
                public const float QueenBee = 4f;
                public const float Skeletron = 5f;
                public const float WallOfFlesh = 6f;
                public const float TheTwins = 7f;
                public const float TheDestroyer = 8f;
                public const float SkeletronPrime = 9f;
                public const float Plantera = 10f;
                public const float Golem = 11f;
                public const float DukeFishron = 12f;
                public const float LunaticCultist = 13f;
                public const float Moonlord = 14f;*/



                // PRE-HM

                bossChecklist.Call(
                    "AddMiniBoss", // Name of the call
                    2.01f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Special.LeonhardPhase1>() },
                    this, // Mod
                    "???", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Special.LeonhardPhase1>())), // Downed variable (the one keeping track the boss has been defeated once)
                    0,
                    0,
                    new List<int> { ModContent.ItemType<Items.StaminaVessel>(), ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>(), ModContent.ItemType<Items.NamelessSoldierSoul>(), ModContent.ItemType<Items.SoulShekel>() }, // List containing all the loot to show in the bestiary
                    $"Explore.", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/LeonhardPhase1");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    3.9f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Slogra>(), ModContent.NPCType<NPCs.Bosses.Gaibon>() },
                    this, // Mod
                    "Slogra and Gaibon", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Slogra>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.TomeOfSlograAndGaibon>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.SlograBag>(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), ModContent.ItemType<Items.Accessories.BloodbiteRing>() }, // List containing all the loot to show in the bestiary
                    $"Found in the depths of the Meteor Temple.", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/SlograAndGaibon");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    5.01f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()/*, ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody>(), ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody2>(), ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody3>(), ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernLegs>(), ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernTail>()*/ },
                    this, // Mod
                    "Jungle Wyvern", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.JungleFeather>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.JungleWyvernBag>(), ModContent.ItemType<Items.Accessories.ChloranthyRing>(), ItemID.Sapphire, ItemID.Ruby, ItemID.Topaz, ItemID.Diamond, ItemID.Emerald, ItemID.Amethyst, ItemID.NecroHelmet, ItemID.NecroBreastplate, ItemID.NecroGreaves }, // List containing all the loot to show in the bestiary
                    $"Found in the depths of the Forgotten City.", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/JungleWyvern");




                // HM

                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    6.2f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.TheRage>() },
                    this, // Mod
                    "The Rage", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.FieryEgg>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.TheRageBag>(), ModContent.ItemType<Items.CrestOfFire>(), ItemID.CobaltDrill }, // List containing all the loot to show in the bestiary
                    $"Found in the depths of the earth.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    6.4f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.TheSorrow>() },
                    this, // Mod
                    "The Sorrow", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.WateryEgg>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.TheSorrowBag>(), ModContent.ItemType<Items.CrestOfWater>(), ItemID.AdamantiteDrill }, // List containing all the loot to show in the bestiary
                    $"Follow the clues given after beating the previous boss.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    6.5f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.TheHunter>() },
                    this, // Mod
                    "The Hunter", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheHunter>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.GrassyEgg>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.TheHunterBag>(), ModContent.ItemType<Items.CrestOfEarth>(), ItemID.Drax, ItemID.WaterWalkingBoots }, // List containing all the loot to show in the bestiary
                    $"Follow the clues given after beating the previous boss.", // Guide to fight the boss
                    "");



                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    7.1f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Death>() },
                    this, // Mod
                    "Death", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Death>())), // Downed variable (the one keeping track the boss has been defeated once)
                    0,
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.DeathBag>(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), ModContent.ItemType<Items.Weapons.Magic.WallTome>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), ItemID.MidnightRainbowDye }, // List containing all the loot to show in the bestiary
                    $"???", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    7.2f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>() },
                    this, // Mod
                    "Wyvern Mage & Mecha Wyvern", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.WingOfTheFallen>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.WyvernMageBag>(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), ModContent.ItemType<Items.Weapons.Melee.LionheartGunblade>(), ModContent.ItemType<Items.Weapons.Magic.LampTome>(), ModContent.ItemType<Items.Accessories.GemBox>(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), ModContent.ItemType<Items.Accessories.BloodbiteRing>() }, // List containing all the loot to show in the bestiary
                    $"Found high atop a mountain in a great fortress.", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/WyvernMage");



                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    7.3f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>() },
                    this, // Mod
                    "Serris", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.SerrisBait>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.SerrisBag>(), ModContent.ItemType<Items.Potions.DemonDrugPotion>(), ModContent.ItemType<Items.Potions.ArmorDrugPotion>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>() }, // List containing all the loot to show in the bestiary
                    $"???", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/Serris");



                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.01f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>() },
                    this, // Mod
                    "Attraidies", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.MindCube>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>() }, // List containing all the loot to show in the bestiary
                    $"Gather all the Crests in order to craft the Mind Cube."); // Guide to fight the boss



                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.02f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>() },
                    this, // Mod
                    "Attraidies (True)", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.AttraidiesBag>(), ModContent.ItemType<Items.TheEnd>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.SoulOfAttraidies>(), ModContent.ItemType<Items.Weapons.Magic.BloomShards>(), ItemID.Picksaw }, // List containing all the loot to show in the bestiary
                    $"Use the Mindflayer Illusion Relic dropped by Attraidies' illusion.", // Guide to fight the boss
                    "", // Despawning Message
                    "",
                    "",
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>())));





                // SHM

                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.1f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>() },
                    this, // Mod
                    "Hellkite Dragon", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.HellkiteStone>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.HellkiteBag>(), ModContent.ItemType<Items.DragonEssence>(), ModContent.ItemType<Items.Accessories.DragonStone>() }, // List containing all the loot to show in the bestiary
                    $"Can attack when you least expect it. Alternatively, craft and use the Hellkite Stone.", // Guide to fight the boss
                    "", // Despawning Message
                    "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/HellkiteDragon");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.2f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>() },
                    this, // Mod
                    "Water Fiend Kraken", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.DyingWaterCrystal>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.KrakenBag>(), ModContent.ItemType<Items.Accessories.DragonHorn>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.FairyInABottle>(), ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>() }, // List containing all the loot to show in the bestiary
                    $"Seek out the Realm of the Kraken. There you will find the materials needed to craft Dying Water Crystal.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.3f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>() },
                    this, // Mod
                    "Earth Fiend Lich", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.DyingEarthCrystal>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.LichBag>(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.FairyInABottle>(), ModContent.ItemType<Items.Weapons.Magic.Bolt3Tome>(), ModContent.ItemType<Items.Accessories.DragoonBoots>(), ModContent.ItemType<Items.Weapons.Melee.ForgottenGaiaSword>() }, // List containing all the loot to show in the bestiary
                    $"Find the Lair of the Lich King. There you will find the materials needed to craft Dying Earth Crystal.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.4f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>() },
                    this, // Mod
                    "Witchking", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>())), // Downed variable (the one keeping track the boss has been defeated once)
                    0,
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.WitchkingBag>(), ModContent.ItemType<BrokenStrangeMagicRing>(), ModContent.ItemType<Items.Weapons.Melee.WitchkingsSword>(), ModContent.ItemType<Items.Armors.WitchkingHelmet>(), ModContent.ItemType<Items.Armors.WitchkingTop>(), ModContent.ItemType<Items.Armors.WitchkingBottoms>(), ModContent.ItemType<Items.Accessories.RingOfPower>(),
                    ModContent.ItemType<Items.Accessories.GoldenHairpin>(), ModContent.ItemType<GuardianSoul>(), ModContent.ItemType<Items.BossItems.DarkMirror>(), ModContent.ItemType<Items.Accessories.CovenantOfArtorias>() }, // List containing all the loot to show in the bestiary
                    $"Spawns rarely around the world, though more commonly during Blood Moons. Alternatively, find him in his lair deep underground, shrouded in extreme darkness.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.5f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>() },
                    this, // Mod
                    "Fire Fiend Marilith", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.DyingFireCrystal>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.MarilithBag>(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), ModContent.ItemType<Items.GuardianSoul>(), ModContent.ItemType<Items.Weapons.Melee.ForgottenRisingSun>(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>() },// List containing all the loot to show in the bestiary
                    $"Found in a place serving as a source of extreme darkness. There you will also find the crafting materials for the Dying Fire Crystal.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.6f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>() },
                    this, // Mod
                    "Seath the Scaleless", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.StoneOfSeath>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.SeathBag>(), ModContent.ItemType<Items.DragonEssence>(), ModContent.ItemType<Items.BequeathedSoul>(), ModContent.ItemType<Items.Accessories.BlueTearstoneRing>(), ModContent.ItemType<Items.PurgingStone>(), ModContent.ItemType<Items.Accessories.DragonWings>() },// List containing all the loot to show in the bestiary
                    $"Found deep underground in a place of extreme cold. Alternatively, craft and use a Stone of Seath.", // Guide to fight the boss
                    "");


                bossChecklist.Call(
                    "AddBoss", // Name of the call
                    12.65f, // Tier (look above)
                    new List<int>() { ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>() },
                    this, // Mod
                    "Abysmal Oolacile Sorcerer", // Boss Name
                    (Func<bool>)(() => tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.ItemType<Items.BossItems.AbysmalStone>(),
                    0,
                    new List<int> { ModContent.ItemType<Items.BossBags.OolacileSorcererBag>(), ModContent.ItemType<Items.Potions.HealingElixir>(), ModContent.ItemType<Items.Accessories.DuskCrownRing>(), ModContent.ItemType<Items.Humanity>(), ModContent.ItemType<Items.PurgingStone>(), ModContent.ItemType<Items.RedTitanite>() },// List containing all the loot to show in the bestiary
                    $"Found deep in the pits of the earth. Alternatively, craft and use an Abysmal Stone.", // Guide to fight the boss
                    "");



            }
            #endregion
        }

        internal void UpdateCheck()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData";
            string changelogPath = dataDir + separator + "tsorcChangelog.txt"; //Downloaded changelog from the github
            string curVersionPath = dataDir + separator + "tsorcCurrentVer.txt"; //Stored file recording current map and music mod versions
            string musicTempPath = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to

            //Check if the data directory exists, if not then create it
            if (!Directory.Exists(dataDir))
            {
                CreateDataDirectory();
            }

            //If it finds a music mod in the data folder, do the second phase of loading it
            if (File.Exists(musicTempPath))
            {
                InstallMusicMod();
            }

            //Download the changelog. *Not* async, because the next function requires it (and it's extremely small).
            //Could rewrite this to work async later if impact on load time isn't neglicable. Something something premature optimization.

            //changed it to async
            ChangelogDownload();
            //if we can't use synchronous, just async and manually sleep
            //i dont care. get over it.
            Thread.Sleep(500);

            //If it exists, read from it. If not, put a warning in the log that it failed to download.
            if (File.Exists(changelogPath))
            {
                string mapString = "";
                string musicString = "";

                //Pull the version numbers from the file
                using (StreamReader reader = File.OpenText(changelogPath))
                {
                    string currentString = "";

                    while ((currentString = reader.ReadLine()) != null)
                    {
                        if (currentString.Contains("MAP ") && mapString == "")
                        {
                            mapString = currentString;
                        }
                        if (currentString.Contains("MUSIC ") && musicString == "")
                        {
                            musicString = currentString;
                        }
                        if (mapString != "" && musicString != "")
                        {
                            break;
                        }
                    }
                }
                if (mapString == "" || musicString == "")
                {
                    Logger.Warn("WARNING: Failed to read version data from downloaded changelog! This will prevent the mod from downloading the map, music mod, or updates!");
                }
                else
                {
                    //Simplify them
                    mapString = mapString.TrimStart("MAP ".ToCharArray());
                    if (musicString.Contains("MUSIC MOD "))
                    {
                        musicString = musicString.TrimStart("MUSIC MOD ".ToCharArray());
                    }
                    else
                    {
                        musicString = musicString.TrimStart("MUSIC ".ToCharArray());
                    }
                    mapString = mapString.Replace(".", "");
                    mapString = mapString.Replace(" ", "");
                    musicString = musicString.Replace(".", "");
                    musicString = musicString.Replace(" ", "");

                    //Append 0's to both strings to make them fixed-length, so that different sized version numbers are all read the same
                    int initialLength = mapString.Length;
                    for (int i = 0; i < 10 - initialLength; i++)
                    {
                        mapString += "0";
                    }

                    initialLength = musicString.Length;
                    for (int i = 0; i < 10 - initialLength; i++)
                    {
                        musicString += "0";
                    }

                    //If no stored version file exists, create it with version 000000
                    if (!File.Exists(curVersionPath))
                    {
                        using (StreamWriter versionFile = new StreamWriter(curVersionPath))
                        {
                            versionFile.WriteLine("000000");
                            versionFile.WriteLine("000000");
                        }
                    }

                    //Ensure that it now does exist and read the first line for the map version.
                    //If it's less than the current map string, download the new one and update the stored version.
                    //If the music one is less, then flag it as needing an update so the UI can display that to the user
                    if (File.Exists(curVersionPath))
                    {
                        string[] curVersionFile = File.ReadAllLines(curVersionPath);

                        if (Int32.Parse(curVersionFile[0]) < Int32.Parse(mapString))
                        {
                            if (MapDownload())
                            {
                                curVersionFile[0] = mapString;
                            }
                        }
                        if (Int32.Parse(curVersionFile[1]) < Int32.Parse(musicString))
                        {
                            if (justUpdatedMusic)
                            {
                                curVersionFile[1] = musicString;
                            }
                            else
                            {
                                MusicNeedsUpdate = true; //Not setting music current version just yet, that happens once the file is *actually* downloaded
                            }
                        }
                        File.WriteAllLines(curVersionPath, curVersionFile);
                    }


                }
            }
            else
            {
                Logger.Warn("Failed to download or read changelog.");
            }


            //TryCopyMap();
        }

        //Returns true if download successful
        public bool MapDownload()
        {
            char separator = Path.DirectorySeparatorChar;
            string filePath = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData" + separator + "tsorcBaseMap.wld";

            if (File.Exists(filePath))
            {
                Logger.Info("Deleting outdated world template.");
                File.Delete(filePath);
            }
            Logger.Info("Attempting to download updated world template.");
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileAsync(new Uri(VariousConstants.MAP_URL), filePath);
                    client.DownloadFileCompleted += TryCopyMap;
                }

                return true;
            }
            catch (WebException e)
            {
                Logger.Warn("Automatic world download failed ({0}). Connection to the internet failed or the file's location has changed.", e);
            }

            catch (Exception e)
            {
                Logger.Warn("Automatic world download failed ({0}).", e);
            }
            return false;
        }


        //Checks if there is already a copy of the adventure map in the Worlds folder, and if not automatically copies one there.
        public static void TryCopyMap(object sender = null, AsyncCompletedEventArgs downloadEvent = null)
        {
            char separator = Path.DirectorySeparatorChar;
            string userMapFileName = separator + "TheStoryofRedCloud.wld";
            string worldsFolder = Main.SavePath + separator + "Worlds";
            string dataDir = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData";
            string baseMapFileName = separator + "tsorcBaseMap.wld";

            FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
            DirectoryInfo worlds = new DirectoryInfo(worldsFolder);
            bool worldExists = false;
            log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;

            if (!Directory.Exists(worldsFolder))
            {
                try
                {
                    Directory.CreateDirectory(worldsFolder);
                }
                catch (UnauthorizedAccessException e)
                {
                    thisLogger.Error("World directory creation failed ({0}). Try again with administrator privileges?", e);
                }
                catch (Exception e)
                {
                    thisLogger.Error("World directory creation failed ({0}).", e);
                }
            }

            foreach (FileInfo file in worlds.GetFiles("*.wld"))
            {
                if (file.FullName.Contains("TheStoryofRedCloud"))
                {
                    worldExists = true;
                    break;
                }
            }

            if (!worldExists && File.Exists(dataDir + baseMapFileName))
            {
                thisLogger.Info("Attempting to copy world.");
                try
                {
                    fileToCopy.CopyTo(worldsFolder + userMapFileName, false);
                }
                catch (System.Security.SecurityException e)
                {
                    thisLogger.Error("World copy failed ({0}). Try again with administrator privileges?", e);
                }
                catch (Exception e)
                {
                    thisLogger.Error("World copy failed ({0}).", e);
                }
            }
        }

        public static void MusicDownload()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;
            char separator = Path.DirectorySeparatorChar;
            string musicTempPath = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to

            thisLogger.Info("Attempting to download music file.");
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += MusicDownloadProgressChanged;
                    client.DownloadFileCompleted += MusicDownloadCompleted;
                    client.DownloadFileAsync(new Uri(VariousConstants.MUSIC_MOD_URL), musicTempPath);

                    DownloadingMusic = true;
                }
            }
            catch (WebException e)
            {
                thisLogger.Warn("Automatic music download failed ({0}). Connection to the internet failed or the file's location has changed.", e);
            }
            catch (Exception e)
            {
                thisLogger.Warn("Automatic world download failed ({0}).", e);
            }
        }

        public void ChangelogDownload()
        {
            char separator = Path.DirectorySeparatorChar;
            string changelogPath = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData" + separator + "tsorcChangelog.txt";

            Logger.Info("Attempting to download changelog.");
            if (File.Exists(changelogPath))
            {
                File.Delete(changelogPath);
            }
            //discard
            //i do not care about "best practice", i want it to work
            _ = GetChangelogAsync(changelogPath);
        }

        public static async Task GetChangelogAsync(string dir) {
            try {
                using HttpClient client = new();
                using var netstream = await client.GetStreamAsync(new Uri(VariousConstants.CHANGELOG_URL));
                using var fs = new FileStream(dir, FileMode.CreateNew);
                await netstream.CopyToAsync(fs);
            }
            catch (Exception e) {
                //at least it isnt log and throw
                log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;
                thisLogger.InfoFormat("GetChangelogAsync threw error {0}", e);
            }
        }

        public void CreateDataDirectory()
        {
            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData";
            Logger.Info("Directory " + dataDir + " not found. Creating directory.");
            try
            {
                Directory.CreateDirectory(dataDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Error("Data directory creation failed ({0}). Try again with administrator privileges?", e);
            }
            catch (Exception e)
            {
                Logger.Error("Data directory creation failed ({0}).", e);
            }
        }

        public static void MusicDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs downloadEvent)
        {
            MusicDownloadProgress = downloadEvent.ProgressPercentage;
        }

        public static void MusicDownloadCompleted(object sender, AsyncCompletedEventArgs downloadEvent)
        {
            MusicDownloadProgress = 0;
            DownloadingMusic = false;
            DisableMusicAndReload();
        }

        //Performs the tasks necessary to replace the old music mod file with the newly downloaded one
        public static void InstallMusicMod()
        {
            char separator = Path.DirectorySeparatorChar;
            string musicTempPath = Main.SavePath + separator + "Mod Configs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to
            string musicFinalPath = Main.SavePath + separator + "Mods" + separator + "tsorcMusic.tmod"; //Where the music mod should be moved to upon reload
            string configPath = Main.SavePath + separator + "Mods" + separator + "enabled.json"; //Where the config dedicing what mods to load is

            //First, check if the music mod is still enabled
            bool musicLoaded = false;
            using (StreamReader reader = File.OpenText(configPath))
            {
                string currentString = "";

                while ((currentString = reader.ReadLine()) != null)
                {
                    if (currentString.Contains("tsorcMusic"))
                    {
                        musicLoaded = true;
                        break;
                    }
                }
            }


            //If so, do not move it. Instead enable a special flag to prompt a reload once the initial load finishes (can't do it here while we're mid-load, or an error would occur)
            if (musicLoaded)
            {
                SpecialReloadNeeded = true;
            }

            //If not, then it's safe to delete the old one and move the new one in
            //Also, it flags music as updated so that the stored version file gets updated later
            //One more reload is required for enabling to take effect
            else
            {
                if (File.Exists(musicFinalPath))
                {
                    File.Delete(musicFinalPath);
                }

                File.Move(musicTempPath, musicFinalPath);
                justUpdatedMusic = true;
                ReloadNeeded = true;
            }
        }

        //Uses reflection to disable the music mod and force a reload
        public static void DisableMusicAndReload()
        {
            object[] modParam = new object[1] { "tsorcMusic" };
            typeof(ModLoader).GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
            typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
        }


        //Adds the music mod to the enabled config and forces a reload
        public static void EnableMusicAndReload()
        {
            object[] modParam = new object[1] { "tsorcMusic" };
            typeof(ModLoader).GetMethod("EnableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
            typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
            ReloadNeeded = false;
            MusicNeedsUpdate = false;
        }
    }




    public class tsorcPacketID
    {
        //Bytes because packets use bytes
        public const byte SyncSoulSlot = 1;
        public const byte SyncEventDust = 2;
        public const byte SyncTimeChange = 3;
        public const byte DispelShadow = 4;
        public const byte DropSouls = 5;
        public const byte SyncPlayerDodgeroll = 6;
        public const byte SyncBonfire = 7;
    }

    //config moved to separate file

    public class TilePlaceCode : GlobalItem
    {

        public override bool CanUseItem(Item item, Player player)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (item.createWall > 0)
                {
                    return false; //prevent placing walls
                }
                if (item.createTile > -1)
                {

                    if (tsorcRevamp.PlaceAllowed.Contains(item.createTile))
                    {
                        return true; //allow placing tiles in PlaceAllowed
                    }

                    else if (tsorcRevamp.CrossModTiles.Contains(item.createTile))
                    {
                        return true; //allow placing certain tiles from other mods
                    }

                    else if (tsorcRevamp.PlaceAllowedModTiles.Contains(item.createTile))
                    {
                        return true; //allow placing certain tiles from this mod
                    }

                    else return false; //disallow using item if it places other tiles
                }
                return true; //allow using items if they do not create tiles
            }
            return base.CanUseItem(item, player); //use default value
        }
    }

    public class TileKillCode : GlobalTile
    {

        public override bool CanKillTile(int x, int y, int type, ref bool blockDamaged)
        {
            if (tsorcRevamp.ActuationBypassActive)
            {
                return true;
            }
            
            if (Main.tile[x, y - 1].TileType == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {

                if (Main.tile[x, y - 1].TileType == TileID.Statues)
                {
                    return false;
                }

                bool right = !Main.tile[x + 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x + 1, y].TileType);
                bool left = !Main.tile[x - 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x - 1, y].TileType);
                bool below = !Main.tile[x, y - 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y - 1].TileType);
                bool above = !Main.tile[x, y + 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y + 1].TileType);
                if (x < 10 || x > Main.maxTilesX - 10)
                {//sanity
                    return false;
                }
                else if (y < 10 || y > Main.maxTilesY - 10)
                {//sanity 
                    return false;
                }
                else if (Main.tile[x, y] == null)
                {//sanity
                    return false;
                }
                else if (tsorcRevamp.KillAllowed.Contains(type))
                {//always allow KillAllowed
                    return true;
                }
                else if (tsorcRevamp.CrossModTiles.Contains(type))
                {//allow breaking placeable modded tiles from other mods
                    return true;
                }
                else if (tsorcRevamp.PlaceAllowedModTiles.Contains(type))
                {//allow breaking placeable modded tiles
                    return true;
                }
                else if (tsorcRevamp.Unbreakable.Contains(type))
                {//always disallow Unbreakable	
                    return false;
                }
                else if (right && left)
                {//if a tile has no neighboring tiles horizontally, allow breaking
                    return true;
                }
                else if (below && above)
                {//if a tile has no neighboring tiles vertically, allow breaking
                    return true;
                }
                else return false; //disallow breaking tiles otherwise
            }

            return base.CanKillTile(x, y, type, ref blockDamaged); //use default value
        }

        public override bool CanExplode(int x, int y, int type)
        {

            if (Main.tile[x, y - 1].TileType == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (Main.tile[x, y - 1].TileType == TileID.Statues)
                {
                    return false;
                }
                bool right = !Main.tile[x + 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x + 1, y].TileType);
                bool left = !Main.tile[x - 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x - 1, y].TileType);
                bool below = !Main.tile[x, y - 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y - 1].TileType);
                bool above = !Main.tile[x, y + 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y + 1].TileType);
                bool CanDestroy = false;
                if (type == TileID.Ebonsand || type == TileID.Amethyst || type == TileID.ShadowOrbs)
                { //shadow temple / corruption chasm stuff that gets blown up
                    CanDestroy = true;
                }

                //check cankilltiles stuff
                if ((right && left) || (above && below) || tsorcRevamp.KillAllowed.Contains(type) || (x < 10 || x > Main.maxTilesX - 10) || (y < 10 || y > Main.maxTilesY - 10) || (!Main.tile[x, y].HasTile))
                {
                    CanDestroy = true;
                }
                if (Main.tileDungeon[Main.tile[x, y].TileType]
                    || type == TileID.Silver
                    || type == TileID.Cobalt
                    || type == TileID.Mythril
                    || type == TileID.Adamantite
                    || (tsorcRevamp.Unbreakable.Contains(type))
                )
                {
                    CanDestroy = false;
                }
                if (!Main.hardMode && type == TileID.Hellstone)
                {
                    CanDestroy = false;
                }
                return CanDestroy;
            }



            else return base.CanExplode(x, y, type);
            

        }

        public override bool Slope(int i, int j, int type)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            else return base.Slope(i, j, type);
        }
    }

    public class WallKillCode : GlobalWall
    {
        public override void KillWall(int i, int j, int type, ref bool fail)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                fail = true;
            }
        }
        public override bool CanExplode(int i, int j, int type)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return base.CanExplode(i, j, type);
        }
    }
    public class MiscGlobalTile : GlobalTile
    {

        bool vortexNotif = false;
        bool nebulaNotif = false;
        bool stardustNotif = false;
        bool solarNotif = false;

        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                Player player = Main.LocalPlayer;
                var pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
                var distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

                if (Main.tile[i, j].TileType == TileID.LunarMonolith && distance <= 800f && !player.dead && Main.tile[i, j].TileFrameY > 54)
                { //frameY > 54 means enabled

                    int style = Main.tile[i, j].TileFrameX / 36;
                    switch (style)
                    {
                        case 0:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerVortex) && !tsorcRevampWorld.DownedVortex)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerVortex, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveVortex = true;
                                    NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!vortexNotif)
                                    {
                                        UsefulFunctions.BroadcastText("The power of this monolith is bound to Attraidies", Color.Teal);
                                        UsefulFunctions.BroadcastText("Defeating him and returning here may allow you to release it...", Color.Teal);
                                        vortexNotif = true;
                                    }
                                }
                            }
                            break;

                        case 1:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerNebula) && !tsorcRevampWorld.DownedNebula)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerNebula, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveNebula = true;
                                    NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!nebulaNotif)
                                    {
                                        UsefulFunctions.BroadcastText("The power of this monolith is bound to Attraidies", Color.Pink);
                                        UsefulFunctions.BroadcastText("Defeating him and returning here may allow you to release it...", Color.Pink);
                                        nebulaNotif = true;
                                    }
                                }
                            }
                            break;

                        case 2:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerStardust) && !tsorcRevampWorld.DownedStardust)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerStardust, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveStardust = true;
                                    NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!stardustNotif)
                                    {
                                        UsefulFunctions.BroadcastText("The power of this monolith is bound to Attraidies", Color.Cyan);
                                        UsefulFunctions.BroadcastText("Defeating him and returning here may allow you to release it...", Color.Cyan);
                                        stardustNotif = true;
                                    }
                                }
                            }
                            break;

                        case 3:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerSolar) && !tsorcRevampWorld.DownedSolar)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerSolar, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveSolar = true;
                                    NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!solarNotif)
                                    {
                                        UsefulFunctions.BroadcastText("The power of this monolith is bound to Attraidies", Color.OrangeRed);
                                        UsefulFunctions.BroadcastText("Defeating him and returning here may allow you to release it...", Color.OrangeRed);
                                        solarNotif = true;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            base.NearbyEffects(i, j, type, closer);
        }
    }

    //tConfig played nice with partially transparent textures, tModloader doesn't. This class helps fix that
    //Handles premultiplication, allowing textures with alpha transparency to be displayed correctly
    //To add a texture, add an enum for it under TransparentTextureType, and a KeyValuePair for it in the TransparentTextures dictionary like the others below
    //You can then get your premultiplied Texture2D from it wherever it's needed via TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.(YourEnum)]; 
    public static class TransparentTextureHandler
    {
        public static Dictionary<TransparentTextureType, Texture2D> TransparentTextures;
        public enum TransparentTextureType
        {
            PhasedMatterBlast,
            AntiGravityBlast,
            EnemyPlasmaOrb,
            ManaShield,
            CrazedOrb,
            MasterBuster,
            AntiMaterialRound,
            GlaiveBeam,
            GlaiveBeamItemGlowmask,
            GlaiveBeamHeldGlowmask,
            GenericLaser,
            GenericLaserTargeting,
            DarkLaser,
            DarkLaserTargeting,
            PulsarGlowmask,
            GWPulsarGlowmask,
            PolarisGlowmask,
            ToxicCatalyzerGlowmask,
            VirulentCatalyzerGlowmask,
            BiohazardGlowmask,
            HealingElixirGlowmask,
            ShatteredMoonlightGlowmask,
            GreySlashGlowmask,
            DarkDivineSpark,
            UltimaWeapon,
            UltimaWeaponGlowmask,
            DarkUltimaWeapon,
            DarkUltimaWeaponGlowmask,
            ReflectionShift,
            PhazonRound,
            MoonlightGreatsword,
            MoonlightGreatswordGlowmask,
            EstusFlask,
            ElfinArrow,
            ElfinTargeting,
            HumanityPhantom,
            BarbarousThornBladeGlowmask,
            RedLaser,
            RedLaserTransparent,
            Lightning,
            BulletHellLaser
        }

        //All textures with transparency will have to get run through this function to get premultiplied
        public static void TransparentTextureFix()
        {
            //Generates the dictionary of textures
            TransparentTextures = new Dictionary<TransparentTextureType, Texture2D>()
            {
                {TransparentTextureType.PhasedMatterBlast, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/PhasedMatterBlast", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.AntiGravityBlast, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/AntiGravityBlast", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.EnemyPlasmaOrb, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyPlasmaOrb", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ManaShield, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/ManaShield", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.CrazedOrb, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/CrazedOrb", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MasterBuster, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/MasterBuster", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.AntiMaterialRound, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/AntiMaterialRound", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeam, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GlaiveBeamLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeamItemGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/GlaiveBeam_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeamHeldGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/GlaiveBeamHeld_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GenericLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GenericLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GenericLaserTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GenericLaserTargeting", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/DarkLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkLaserTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/DarkLaserTargeting", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PulsarGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Pulsar_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GWPulsarGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/GWPulsar_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PolarisGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Polaris_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ToxicCatalyzerGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/ToxicCatalyzer_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.VirulentCatalyzerGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/VirulentCatalyzer_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BiohazardGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Biohazard_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.HealingElixirGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Potions/HealingElixir_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkDivineSpark, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkDivineSpark", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ShatteredMoonlightGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/ShatteredMoonlight_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GreySlashGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/GreySlash_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.UltimaWeapon, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/UltimaWeaponTransparent", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.UltimaWeaponGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/UltimaWeaponGlowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkUltimaWeapon, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkUltimaWeapon", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkUltimaWeaponGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkUltimaWeaponGlowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ReflectionShift, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Accessories/ReflectionShift", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PhazonRound, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/PhazonRound", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MoonlightGreatsword, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/MoonlightGreatsword", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MoonlightGreatswordGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/MoonlightGreatsword_Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.EstusFlask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/EstusFlask_drinking", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ElfinArrow, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/ElfinArrow", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ElfinTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/ElfinTargeting", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.HumanityPhantom, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/HumanityPhantom", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BarbarousThornBladeGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/BarbarousThornBlade_Glow", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.RedLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/RedLaserBeam", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.RedLaserTransparent, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyRedLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad)}, //A transparent and non-transparent version of this exists because the current focused energy beam laser projectile stacks a lot of beam midsections on top of each other, which fucks up transparency
                {TransparentTextureType.Lightning, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyLightningStrike", ReLogic.Content.AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BulletHellLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Gwyn/BulletHellLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad)}

            };


            //Runs each entry through the XNA's premultiplication function
            foreach (Texture2D textureEntry in TransparentTextures.Values)
            {
                Color[] buffer = new Color[textureEntry.Width * textureEntry.Height];
                textureEntry.GetData(buffer);
                for (int j = 0; j < buffer.Length; j++)
                {
                    buffer[j] = Color.FromNonPremultiplied(buffer[j].R, buffer[j].G, buffer[j].B, buffer[j].A);
                }
                textureEntry.SetData(buffer);
            }
        }
    };

}