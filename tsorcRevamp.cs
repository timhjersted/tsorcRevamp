using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using Terraria.UI;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;
using Terraria.GameContent.UI;
using System.Collections;
using tsorcRevamp.UI;
using System;

namespace tsorcRevamp {
    public class tsorcRevamp : Mod {

        public static ModHotKey toggleDragoonBoots;
        public static int DarkSoulCustomCurrencyId;
        public static BitArray KillAllowed;
        public static BitArray PlaceAllowed;
        public static BitArray Unbreakable;

        internal BonfireUIState BonfireUIState;
        private UserInterface _bonfireUIState;

        public override void Load() {
            toggleDragoonBoots = RegisterHotKey("Dragoon Boots", "Z");

            DarkSoulCustomCurrencyId = CustomCurrencyManager.RegisterCurrency(new DarkSoulCustomCurrency(ModContent.ItemType<DarkSoul>(), 99999L));

            //On.Terraria.NPC.SpawnSkeletron += SkeletronPatch;

            On.Terraria.UI.ChestUI.DepositAll += DepositAllPatch;

            On.Terraria.Player.Spawn += SpawnPatch;

            //On.Terraria.WorldGen.TriggerLunarApocalypse += StopLunarApocalypse;

            On.Terraria.WorldGen.UpdateLunarApocalypse += StopMoonLord;

            BonfireUIState = new BonfireUIState();
            if (!Main.dedServ) BonfireUIState.Activate();
            _bonfireUIState = new UserInterface();
            if (!Main.dedServ) _bonfireUIState.SetState(BonfireUIState);

            KillAllowed = new BitArray(471);
            PlaceAllowed = new BitArray(471);
            Unbreakable = new BitArray(471);
            PopulateArrays();
        }

        private void StopMoonLord(On.Terraria.WorldGen.orig_UpdateLunarApocalypse orig) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {

                if (!NPC.LunarApocalypseIsUp) {
                    return;
                }
                //bool flag = false; this bool was used to check for moon lord's core
                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                bool flag5 = false;
                for (int i = 0; i < 200; i++) {
                    if (Main.npc[i].active) {
                        switch (Main.npc[i].type) {
                            /*
                            case 398:
                                flag = true;
                                break;
                            */
                            case 517:
                                flag2 = true;
                                break;
                            case 422:
                                flag3 = true;
                                break;
                            case 507:
                                flag4 = true;
                                break;
                            case 493:
                                flag5 = true;
                                break;
                        }
                    }
                }
                if (!flag2) {
                    NPC.TowerActiveSolar = false;
                }
                if (!flag3) {
                    NPC.TowerActiveVortex = false;
                }
                if (!flag4) {
                    NPC.TowerActiveNebula = false;
                }
                if (!flag5) {
                    NPC.TowerActiveStardust = false;
                }
                if (!NPC.TowerActiveSolar && !NPC.TowerActiveVortex && !NPC.TowerActiveNebula && !NPC.TowerActiveStardust/* && !flag*/) { 
                    //WorldGen.StartImpendingDoom();
                    //recreate the effects of StartImpendingDoom, minus the part about spawning moon lord
                    NPC.LunarApocalypseIsUp = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        WorldGen.GetRidOfCultists();
                    }
                }

            }
            else {
                orig();
            }
        }


        /*
        private void SkeletronPatch(On.Terraria.NPC.orig_SpawnSkeletron orig) {
            if (ModContent.GetInstance<tsorcRevampConfig>().RenameSkeletron) {
                bool flag = true;
                bool flag2 = false;
                Vector2 vector = Vector2.Zero;
                int num = 0;
                int num2 = 0;
                for (int i = 0; i < 200; i++) {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<NPCs.Bosses.GravelordNito>()) {
                        flag = false;
                        break;
                    }
                }
                for (int j = 0; j < 200; j++) {
                    if (Main.npc[j].active) {
                        if (Main.npc[j].type == NPCID.OldMan) {
                            flag2 = true;
                            Main.npc[j].ai[3] = 1f;
                            vector = Main.npc[j].position;
                            num = Main.npc[j].width;
                            num2 = Main.npc[j].height;
                            if (Main.netMode == NetmodeID.Server) {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, j, 0f, 0f, 0f, 0, 0, 0);
                            }
                        }
                        else if (Main.npc[j].type == NPCID.Clothier) {
                            flag2 = true;
                            vector = Main.npc[j].position;
                            num = Main.npc[j].width;
                            num2 = Main.npc[j].height;
                        }
                    }
                }
                if (flag && flag2) {
                    int num3 = NPC.NewNPC((int)vector.X + num / 2, (int)vector.Y + num2 / 2, ModContent.NPCType<NPCs.Bosses.GravelordNito>(), 0, 0f, 0f, 0f, 0f, 255);
                    Main.npc[num3].netUpdate = true;
                    if (Main.netMode == NetmodeID.SinglePlayer) {
                        Main.NewText("Gravelord Nito has awoken!", 175, 75, 255);
                        return;
                    }
                    else if (Main.netMode == NetmodeID.Server) {
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Gravelord Nito has awoken!"), new Color(175, 75, 255));
                    }
                }
            }
            else {
                orig();
            }
        }
        */

        public override void UpdateUI(GameTime gameTime) {
            if (BonfireUIState.Visible) {
                _bonfireUIState?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1) {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: BonfireUI",
                    delegate {
                        if (BonfireUIState.Visible) {

                            _bonfireUIState.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }


        private void PopulateArrays() {
            #region KillAllowed bit array 
            KillAllowed[2] = true; //grass
            KillAllowed[3] = true; //small plants
            KillAllowed[4] = true; // torch
            KillAllowed[5] = true; //tree trunk
            KillAllowed[6] = true; //iron
            KillAllowed[7] = true; //copper
            KillAllowed[9] = true; //silver
            KillAllowed[10] = true; //closed door
            KillAllowed[11] = true; //open door
            KillAllowed[12] = true; //Heart crystal
            KillAllowed[13] = true; //bottles and jugs
            KillAllowed[14] = true; //table
            KillAllowed[15] = true; //chairs
            KillAllowed[16] = true; //anvil
            KillAllowed[17] = true; //furnance
            KillAllowed[18] = true; //workbench
            KillAllowed[20] = true; //sapling
            KillAllowed[21] = true; //chests
            KillAllowed[23] = true; //corruption grass
            KillAllowed[24] = true; //small corruption plants
            KillAllowed[27] = true; //sunflower
            KillAllowed[28] = true; //pot
            KillAllowed[29] = true; //piggy bank
            KillAllowed[31] = true; //shadow orb
            KillAllowed[32] = true; //corruption barbs
            KillAllowed[33] = true; //candle
            KillAllowed[34] = true; //bronze chandellier
            KillAllowed[35] = true; //silver c
            KillAllowed[36] = true; //gold c
            KillAllowed[37] = true; //meteorite
            KillAllowed[42] = true; //chain lantern
            KillAllowed[49] = true; //water candle
            KillAllowed[50] = true; //books
            KillAllowed[51] = true; //cobweb
            KillAllowed[52] = true; //vines
            KillAllowed[53] = true; //sand
            KillAllowed[55] = true; //Sign 
            KillAllowed[56] = true; //obsidian
            KillAllowed[60] = true; //jungle grass
            KillAllowed[61] = true; //small jungle plants
            KillAllowed[62] = true; //jungle vines
            KillAllowed[67] = true; //Amethyst 
            KillAllowed[69] = true; //thorns
            KillAllowed[72] = true; //mushroom stalks
            KillAllowed[71] = true; //small mushrooms
            KillAllowed[73] = true; //plants
            KillAllowed[74] = true; //plants
            KillAllowed[78] = true; //clay pot
            KillAllowed[79] = true; //bed
            KillAllowed[80] = true; //cactus
            KillAllowed[81] = true; //corals
            KillAllowed[82] = true; //new herbs
            KillAllowed[83] = true; //grown herbs
            KillAllowed[84] = true; //bloomed herbs
            KillAllowed[85] = true; //tombstone
            KillAllowed[86] = true; //loom
            KillAllowed[87] = true; //piano
            KillAllowed[88] = true; //drawer
            KillAllowed[89] = true; //bench
            KillAllowed[90] = true; //bathtub
            KillAllowed[91] = true; //banner
            KillAllowed[92] = true; //lamp post
            KillAllowed[93] = true; //tiki torch
            KillAllowed[94] = true; //keg
            KillAllowed[95] = true; //chinese lantern
            KillAllowed[96] = true; //cooking pot
            KillAllowed[97] = true; //safe
            KillAllowed[98] = true; //skull candle
            KillAllowed[99] = true; //trash can
            KillAllowed[100] = true; //candlabra
            KillAllowed[101] = true; //bookcase
            KillAllowed[102] = true; //throne
            KillAllowed[103] = true; //bowl
            KillAllowed[104] = true; //grandfather clock
            KillAllowed[105] = true; //statue
            KillAllowed[106] = true; //sawmill
            KillAllowed[107] = true; //cobalt
            KillAllowed[108] = true; //mythril
            KillAllowed[109] = true; //Hallowed Grass
            KillAllowed[110] = true; //Hallowed PlantsKillAllowed[ 
            KillAllowed[111] = true; //adamantite
            KillAllowed[112] = true; //ebonsand
            KillAllowed[114] = true; //tinkerer's workbench
            KillAllowed[115] = true; //Hallowed Vines 
            KillAllowed[116] = true; //pearlsand
            KillAllowed[125] = true; //crystal ball
            KillAllowed[126] = true; //discoball
            KillAllowed[128] = true; //mannequin
            KillAllowed[129] = true; //crystal shard
            KillAllowed[133] = true; //adamantite forge
            KillAllowed[134] = true; //mythril anvil
            KillAllowed[138] = true; //boulder
            KillAllowed[141] = true; //explosives
            KillAllowed[172] = true; //sinks
            KillAllowed[173] = true; //platinum candelabra
            KillAllowed[174] = true; //platinum candle
            KillAllowed[187] = true; //background foliage / sword shrine
            KillAllowed[207] = true; //water fountain
            KillAllowed[218] = true; //meat grinder
            KillAllowed[228] = true; //dye vat
            KillAllowed[240] = true; //trophies
            KillAllowed[242] = true; //big paintings
            KillAllowed[245] = true; //tall paintings
            KillAllowed[246] = true; //wide paintings
            KillAllowed[270] = true; //firefly in a bottle
            KillAllowed[271] = true; //lightning bug in a bottle
            KillAllowed[275] = true; //bunny cage
            KillAllowed[276] = true; //squirrel cage
            KillAllowed[277] = true; //mallard cage
            KillAllowed[278] = true; //duck cage
            KillAllowed[279] = true; //bird cage
            KillAllowed[280] = true; //blue jay cage
            KillAllowed[281] = true; //cardinal cage
            KillAllowed[282] = true; //fish bowl
            KillAllowed[283] = true; //heavy work bench
            KillAllowed[285] = true; //snail cage
            KillAllowed[286] = true; //glowing snail cage
            KillAllowed[287] = true; //ammo box
            KillAllowed[288] = true; //monarch jar
            KillAllowed[289] = true; //purple emperor jar
            KillAllowed[290] = true; //red admiral jar
            KillAllowed[291] = true; //ulysses jar
            KillAllowed[292] = true; //sulphur jar
            KillAllowed[293] = true; //tree nymph jar
            KillAllowed[294] = true; //zebra swallowtail jar
            KillAllowed[295] = true; //julia jar
            KillAllowed[296] = true; //scorpion cage
            KillAllowed[297] = true; //black scorpion cage
            KillAllowed[298] = true; //frog cage
            KillAllowed[299] = true; //mouse cage
            KillAllowed[300] = true; //bone welder
            KillAllowed[301] = true; //flesh cloning vat
            KillAllowed[302] = true; //glass kiln
            KillAllowed[307] = true; //steampunk boiler
            KillAllowed[309] = true; //penguin cage
            KillAllowed[310] = true; //worm cage
            KillAllowed[316] = true; //blue jellyfish jar
            KillAllowed[317] = true; //green jellyfish jar
            KillAllowed[318] = true; //pink jellyfish jar
            KillAllowed[319] = true; //ship in a bottle
            KillAllowed[310] = true; //seaweed planter
            KillAllowed[324] = true; //seashell variants
            KillAllowed[337] = true; //number and letter statues
            KillAllowed[339] = true; //grasshopper cage
            KillAllowed[354] = true; //bewitching table
            KillAllowed[355] = true; //alchemy table
            KillAllowed[358] = true; //gold bird cage
            KillAllowed[359] = true; //gold bunny cage
            KillAllowed[360] = true; //gold butterfly jar
            KillAllowed[361] = true; //gold frog cage
            KillAllowed[362] = true; //gold grasshopper cage
            KillAllowed[363] = true; //gold mouse cage
            KillAllowed[364] = true; //gold worm cage
            KillAllowed[372] = true; //peace candle
            KillAllowed[377] = true; //sharpening station
            KillAllowed[378] = true; //target dummy
            KillAllowed[390] = true; //lava lamp
            KillAllowed[391] = true; //enchanted nightcrawler cage
            KillAllowed[392] = true; //buggy cage
            KillAllowed[393] = true; //grubby cage
            KillAllowed[394] = true; //sluggy cage
            KillAllowed[413] = true; //red squirrel cage
            KillAllowed[414] = true; //gold squirrel cage

            #endregion
            //--------
            #region PlaceAllowed bit array
            PlaceAllowed[4] = true;  //torch
            PlaceAllowed[10] = true; //Closed Door
            PlaceAllowed[11] = true; //Open Door  
            PlaceAllowed[13] = true; //bottles
            PlaceAllowed[15] = true; //chairs
            PlaceAllowed[16] = true; //anvil
            PlaceAllowed[17] = true; //furnace
            PlaceAllowed[18] = true; //workbench
            PlaceAllowed[20] = true; //sapling
            PlaceAllowed[21] = true; //chests
            PlaceAllowed[27] = true; //sunflower
            PlaceAllowed[28] = true; //pot
            PlaceAllowed[29] = true; //piggy bank
            PlaceAllowed[33] = true; //candle
            PlaceAllowed[34] = true; //bronze chandellier
            PlaceAllowed[35] = true; //silver chandellier
            PlaceAllowed[36] = true; //gold chandellier
            PlaceAllowed[42] = true; //chain lantern
            PlaceAllowed[49] = true; //water candle
            PlaceAllowed[50] = true; //books
            PlaceAllowed[55] = true; //Sign 
            PlaceAllowed[73] = true; //plants
            PlaceAllowed[74] = true; //plants
            PlaceAllowed[78] = true; //clay pot
            PlaceAllowed[79] = true; //bed
            PlaceAllowed[81] = true; //corals
            PlaceAllowed[82] = true; //new herbs
            PlaceAllowed[83] = true; //grown herbs
            PlaceAllowed[84] = true; //bloomed herbs
            PlaceAllowed[85] = true; //tombstone
            PlaceAllowed[86] = true; //loom
            PlaceAllowed[87] = true; //piano
            PlaceAllowed[88] = true; //drawer
            PlaceAllowed[89] = true; //bench
            PlaceAllowed[90] = true; //bathtub
            PlaceAllowed[91] = true; //banner
            PlaceAllowed[92] = true; //lamp post
            PlaceAllowed[93] = true; //tiki torch
            PlaceAllowed[94] = true; //keg
            PlaceAllowed[95] = true; //chinese lantern
            PlaceAllowed[96] = true; //cooking pot
            PlaceAllowed[97] = true; //safe
            PlaceAllowed[98] = true; //skull candle
            PlaceAllowed[99] = true; //trash can
            PlaceAllowed[100] = true; //candlabra
            PlaceAllowed[101] = true; //bookcase
            PlaceAllowed[102] = true; //throne
            PlaceAllowed[103] = true; //bowl
            PlaceAllowed[104] = true; //grandfather clock
            PlaceAllowed[105] = true; //statue
            PlaceAllowed[106] = true; //sawmill
            PlaceAllowed[114] = true; //tinkerer's workbench
            PlaceAllowed[125] = true; //crystal ball
            PlaceAllowed[126] = true; //discoball
            PlaceAllowed[128] = true; //mannequin
            PlaceAllowed[129] = true; //crystal shard
            PlaceAllowed[132] = true; //lever
            PlaceAllowed[133] = true; //adamantite forge
            PlaceAllowed[134] = true; //mythril anvil
            PlaceAllowed[149] = true; //festive lights
            PlaceAllowed[172] = true; //sinks
            PlaceAllowed[173] = true; //platinum candelabra
            PlaceAllowed[174] = true; //platinum candle
            PlaceAllowed[207] = true; //water fountain
            PlaceAllowed[218] = true; //meat grinder
            PlaceAllowed[228] = true; //dye vat
            PlaceAllowed[240] = true; //trophies
            PlaceAllowed[242] = true; //big paintings
            PlaceAllowed[245] = true; //tall paintings
            PlaceAllowed[246] = true; //wide paintings
            PlaceAllowed[270] = true; //firefly in a bottle
            PlaceAllowed[271] = true; //lightning bug in a bottle
            PlaceAllowed[275] = true; //bunny cage
            PlaceAllowed[276] = true; //squirrel cage
            PlaceAllowed[277] = true; //mallard cage
            PlaceAllowed[278] = true; //duck cage
            PlaceAllowed[279] = true; //bird cage
            PlaceAllowed[280] = true; //blue jay cage
            PlaceAllowed[281] = true; //cardinal cage
            PlaceAllowed[282] = true; //fish bowl
            PlaceAllowed[283] = true; //heavy work bench
            PlaceAllowed[285] = true; //snail cage
            PlaceAllowed[286] = true; //glowing snail cage
            PlaceAllowed[287] = true; //ammo box
            PlaceAllowed[288] = true; //monarch jar
            PlaceAllowed[289] = true; //purple emperor jar
            PlaceAllowed[290] = true; //red admiral jar
            PlaceAllowed[291] = true; //ulysses jar
            PlaceAllowed[292] = true; //sulphur jar
            PlaceAllowed[293] = true; //tree nymph jar
            PlaceAllowed[294] = true; //zebra swallowtail jar
            PlaceAllowed[295] = true; //julia jar
            PlaceAllowed[296] = true; //scorpion cage
            PlaceAllowed[297] = true; //black scorpion cage
            PlaceAllowed[298] = true; //frog cage
            PlaceAllowed[299] = true; //mouse cage
            PlaceAllowed[300] = true; //bone welder
            PlaceAllowed[301] = true; //flesh cloning vat
            PlaceAllowed[302] = true; //glass kiln
            PlaceAllowed[307] = true; //steampunk boiler
            PlaceAllowed[309] = true; //penguin cage
            PlaceAllowed[310] = true; //worm cage
            PlaceAllowed[316] = true; //blue jellyfish jar
            PlaceAllowed[317] = true; //green jellyfish jar
            PlaceAllowed[318] = true; //pink jellyfish jar
            PlaceAllowed[319] = true; //ship in a bottle
            PlaceAllowed[310] = true; //seaweed planter
            PlaceAllowed[324] = true; //seashell variants
            PlaceAllowed[337] = true; //number and letter statues
            PlaceAllowed[339] = true; //grasshopper cage
            PlaceAllowed[354] = true; //bewitching table
            PlaceAllowed[355] = true; //alchemy table
            PlaceAllowed[358] = true; //gold bird cage
            PlaceAllowed[359] = true; //gold bunny cage
            PlaceAllowed[360] = true; //gold butterfly jar
            PlaceAllowed[361] = true; //gold frog cage
            PlaceAllowed[362] = true; //gold grasshopper cage
            PlaceAllowed[363] = true; //gold mouse cage
            PlaceAllowed[364] = true; //gold worm cage
            PlaceAllowed[372] = true; //peace candle
            PlaceAllowed[377] = true; //sharpening station
            PlaceAllowed[378] = true; //target dummy
            PlaceAllowed[390] = true; //lava lamp
            PlaceAllowed[391] = true; //enchanted nightcrawler cage
            PlaceAllowed[392] = true; //buggy cage
            PlaceAllowed[393] = true; //grubby cage
            PlaceAllowed[394] = true; //sluggy cage
            PlaceAllowed[413] = true; //red squirrel cage
            PlaceAllowed[414] = true; //gold squirrel cage
            #endregion
            //--------
            #region Unbreakable bit array
            Unbreakable[19] = true; //Wood platform
            Unbreakable[55] = true; //sign
            Unbreakable[132] = true; //lever
            Unbreakable[130] = true; //active stone block
            Unbreakable[131] = true; //inactive stone block
            Unbreakable[135] = true; //pressure plates
            Unbreakable[136] = true; //switch
            Unbreakable[137] = true; //dart trap
            #endregion
        }



        private void DepositAllPatch(On.Terraria.UI.ChestUI.orig_DepositAll orig) { //block dark souls from being deposit all-ed into chests.

            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Player player = Main.player[Main.myPlayer];
                if (player.chest > -1) {
                    ChestUI.MoveCoins(player.inventory, Main.chest[player.chest].item);
                }
                else if (player.chest == -3) {
                    ChestUI.MoveCoins(player.inventory, player.bank2.item);
                }
                else if (player.chest == -4) {
                    ChestUI.MoveCoins(player.inventory, player.bank3.item);
                }
                else {
                    ChestUI.MoveCoins(player.inventory, player.bank.item);
                }
                for (int num = 49; num >= 10; num--) {
                    if (player.inventory[num].stack > 0 && player.inventory[num].type > ItemID.None && !player.inventory[num].favorited && player.inventory[num].type != ModContent.ItemType<DarkSoul>()) {
                        if (player.inventory[num].maxStack > 1) {
                            for (int i = 0; i < 40; i++) {
                                if (player.chest > -1) {
                                    Chest chest = Main.chest[player.chest];
                                    if (chest.item[i].stack >= chest.item[i].maxStack || !player.inventory[num].IsTheSameAs(chest.item[i])) {
                                        continue;
                                    }
                                    int num2 = player.inventory[num].stack;
                                    if (player.inventory[num].stack + chest.item[i].stack > chest.item[i].maxStack) {
                                        num2 = chest.item[i].maxStack - chest.item[i].stack;
                                    }
                                    player.inventory[num].stack -= num2;
                                    chest.item[i].stack += num2;
                                    Main.PlaySound(SoundID.Grab);
                                    if (player.inventory[num].stack <= 0) {
                                        player.inventory[num].SetDefaults();
                                        if (Main.netMode == NetmodeID.MultiplayerClient) {
                                            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, player.chest, i);
                                        }
                                        break;
                                    }
                                    if (chest.item[i].type == ItemID.None) {
                                        chest.item[i] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                    }
                                    if (Main.netMode == NetmodeID.MultiplayerClient) {
                                        NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, player.chest, i);
                                    }
                                }
                                else if (player.chest == -3) {
                                    if (player.bank2.item[i].stack < player.bank2.item[i].maxStack && player.inventory[num].IsTheSameAs(player.bank2.item[i])) {
                                        int num3 = player.inventory[num].stack;
                                        if (player.inventory[num].stack + player.bank2.item[i].stack > player.bank2.item[i].maxStack) {
                                            num3 = player.bank2.item[i].maxStack - player.bank2.item[i].stack;
                                        }
                                        player.inventory[num].stack -= num3;
                                        player.bank2.item[i].stack += num3;
                                        Main.PlaySound(SoundID.Grab);
                                        if (player.inventory[num].stack <= 0) {
                                            player.inventory[num].SetDefaults();
                                            break;
                                        }
                                        if (player.bank2.item[i].type == ItemID.None) {
                                            player.bank2.item[i] = player.inventory[num].Clone();
                                            player.inventory[num].SetDefaults();
                                        }
                                    }
                                }
                                else if (player.chest == -4) {
                                    if (player.bank3.item[i].stack < player.bank3.item[i].maxStack && player.inventory[num].IsTheSameAs(player.bank3.item[i])) {
                                        int num4 = player.inventory[num].stack;
                                        if (player.inventory[num].stack + player.bank3.item[i].stack > player.bank3.item[i].maxStack) {
                                            num4 = player.bank3.item[i].maxStack - player.bank3.item[i].stack;
                                        }
                                        player.inventory[num].stack -= num4;
                                        player.bank3.item[i].stack += num4;
                                        Main.PlaySound(SoundID.Grab);
                                        if (player.inventory[num].stack <= 0) {
                                            player.inventory[num].SetDefaults();
                                            break;
                                        }
                                        if (player.bank3.item[i].type == ItemID.None) {
                                            player.bank3.item[i] = player.inventory[num].Clone();
                                            player.inventory[num].SetDefaults();
                                        }
                                    }
                                }
                                else if (player.bank.item[i].stack < player.bank.item[i].maxStack && player.inventory[num].IsTheSameAs(player.bank.item[i])) {
                                    int num5 = player.inventory[num].stack;
                                    if (player.inventory[num].stack + player.bank.item[i].stack > player.bank.item[i].maxStack) {
                                        num5 = player.bank.item[i].maxStack - player.bank.item[i].stack;
                                    }
                                    player.inventory[num].stack -= num5;
                                    player.bank.item[i].stack += num5;
                                    Main.PlaySound(SoundID.Grab);
                                    if (player.inventory[num].stack <= 0) {
                                        player.inventory[num].SetDefaults();
                                        break;
                                    }
                                    if (player.bank.item[i].type == ItemID.None) {
                                        player.bank.item[i] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                    }
                                }
                            }
                        }
                        if (player.inventory[num].stack > 0) {
                            if (player.chest > -1) {
                                for (int j = 0; j < 40; j++) {
                                    if (Main.chest[player.chest].item[j].stack == 0) {
                                        Main.PlaySound(SoundID.Grab);
                                        Main.chest[player.chest].item[j] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                        if (Main.netMode == NetmodeID.MultiplayerClient) {
                                            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, player.chest, j);
                                        }
                                        break;
                                    }
                                }
                            }
                            else if (player.chest == -3) {
                                for (int k = 0; k < 40; k++) {
                                    if (player.bank2.item[k].stack == 0) {
                                        Main.PlaySound(SoundID.Grab);
                                        player.bank2.item[k] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                        break;
                                    }
                                }
                            }
                            else if (player.chest == -4) {
                                for (int l = 0; l < 40; l++) {
                                    if (player.bank3.item[l].stack == 0) {
                                        Main.PlaySound(SoundID.Grab);
                                        player.bank3.item[l] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                        break;
                                    }
                                }
                            }
                            else {
                                for (int m = 0; m < 40; m++) {
                                    if (player.bank.item[m].stack == 0) {
                                        Main.PlaySound(SoundID.Grab);
                                        player.bank.item[m] = player.inventory[num].Clone();
                                        player.inventory[num].SetDefaults();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else {
                orig();
            }
        }

        private void SpawnPatch(On.Terraria.Player.orig_Spawn orig, Player self) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Main.InitLifeBytes();
                if (self.whoAmI == Main.myPlayer) {
                    if (Main.mapTime < 5) {
                        Main.mapTime = 5;
                    }
                    Main.quickBG = 10;
                    self.FindSpawn();
                    Main.maxQ = true;
                }
                if (Main.netMode == NetmodeID.MultiplayerClient && self.whoAmI == Main.myPlayer) {
                    NetMessage.SendData(MessageID.SpawnPlayer, -1, -1, null, Main.myPlayer);
                    Main.gameMenu = false;
                }
                self.headPosition = Vector2.Zero;
                self.bodyPosition = Vector2.Zero;
                self.legPosition = Vector2.Zero;
                self.headRotation = 0f;
                self.bodyRotation = 0f;
                self.legRotation = 0f;
                self.lavaTime = self.lavaMax;
                if (self.statLife <= 0) {
                    int num = self.statLifeMax2 / 2;
                    self.statLife = 100;
                    if (num > self.statLife) {
                        self.statLife = num;
                    }
                    self.breath = self.breathMax;
                    if (self.spawnMax) {
                        self.statLife = self.statLifeMax2;
                        self.statMana = self.statManaMax2;
                    }
                }
                self.immune = true;
                if (self.dead) {
                    PlayerHooks.OnRespawn(self);
                }
                self.dead = false;
                self.immuneTime = 0;
                self.active = true;
                if (self.SpawnX >= 0 && self.SpawnY >= 0) {
                    self.position.X = self.SpawnX * 16 + 8 - self.width / 2;
                    self.position.Y = self.SpawnY * 16 - self.height;
                }
                else {
                    self.position.X = Main.spawnTileX * 16 + 8 - self.width / 2;
                    self.position.Y = Main.spawnTileY * 16 - self.height;
                    for (int i = Main.spawnTileX - 1; i < Main.spawnTileX + 2; i++) {
                        for (int j = Main.spawnTileY - 3; j < Main.spawnTileY; j++) {
                            if (Main.tile[i, j] != null) {
                                if (Main.tileSolid[Main.tile[i, j].type] && !Main.tileSolidTop[Main.tile[i, j].type]) {
                                    WorldGen.KillTile(i, j);
                                }
                                if (Main.tile[i, j].liquid > 0) {
                                    Main.tile[i, j].lava(lava: false);
                                    Main.tile[i, j].liquid = 0;
                                    WorldGen.SquareTileFrame(i, j);
                                }
                            }
                        }
                    }
                }
                self.wet = false;
                self.wetCount = 0;
                self.lavaWet = false;
                self.fallStart = (int)(self.position.Y / 16f);
                self.fallStart2 = self.fallStart;
                self.velocity.X = 0f;
                self.velocity.Y = 0f;
                for (int k = 0; k < 3; k++) {
                    self.UpdateSocialShadow();
                }
                self.oldPosition = self.position + self.BlehOldPositionFixer;
                self.talkNPC = -1;
                if (self.whoAmI == Main.myPlayer) {
                    Main.npcChatCornerItem = 0;
                }
                if (self.pvpDeath) {
                    self.pvpDeath = false;
                    self.immuneTime = 300;
                    self.statLife = self.statLifeMax;
                }
                else {
                    self.immuneTime = 60;
                }
                if (self.whoAmI == Main.myPlayer) {
                    Main.BlackFadeIn = 255;
                    Main.renderNow = true;
                    if (Main.netMode == NetmodeID.MultiplayerClient) {
                        Netplay.newRecent();
                    }
                    Main.screenPosition.X = self.position.X + self.width / 2 - Main.screenWidth / 2;
                    Main.screenPosition.Y = self.position.Y + self.height / 2 - Main.screenHeight / 2;
                }
            }
            else {
                orig(self);
            }
        }
        /*
        private void StopLunarApocalypse(On.Terraria.WorldGen.orig_TriggerLunarApocalypse orig) {
            //if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
            if (false) {
                // DO NOTHING LOL
            }
            else {
                orig();
            }
        }
        */
        public override void Unload() {
            toggleDragoonBoots = null;
            KillAllowed = null;
            PlaceAllowed = null;
            Unbreakable = null;
            tsorcRevampWorld.Slain = null;
        }
        #region permanent potion recipes
        public static void PermaPotionRecipeS(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeA(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 3);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeB(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 2);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeC(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>());
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        #endregion
        public override void AddRecipes() {

            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                #region add s tier potion recipes
                PermaPotionRecipeS(this, ModContent.ItemType<ArmorDrugPotion>(), ModContent.ItemType<PermanentArmorDrugPotion>());
                PermaPotionRecipeS(this, ModContent.ItemType<BattlefrontPotion>(), ModContent.ItemType<PermanentBattlefrontPotion>());
                PermaPotionRecipeS(this, ModContent.ItemType<DemonDrugPotion>(), ModContent.ItemType<PermanentDemonDrugPotion>());
                PermaPotionRecipeS(this, ModContent.ItemType<StrengthPotion>(), ModContent.ItemType<PermanentStrengthPotion>());
                PermaPotionRecipeS(this, ModContent.ItemType<SoulSiphonPotion>(), ModContent.ItemType<PermanentSoulSiphonPotion>());
                PermaPotionRecipeS(this, ItemID.EndurancePotion, ModContent.ItemType<PermanentEndurancePotion>());
                PermaPotionRecipeS(this, ItemID.LifeforcePotion, ModContent.ItemType<PermanentLifeforcePotion>());
                PermaPotionRecipeS(this, ItemID.ManaRegenerationPotion, ModContent.ItemType<PermanentManaRegenerationPotion>());
                #endregion
                #region add a tier recipes
                PermaPotionRecipeA(this, ItemID.Ale, ModContent.ItemType<PermanentAle>());
                PermaPotionRecipeA(this, ItemID.CalmingPotion, ModContent.ItemType<PermanentCalmingPotion>());
                PermaPotionRecipeA(this, ItemID.ArcheryPotion, ModContent.ItemType<PermanentArcheryPotion>());
                PermaPotionRecipeA(this, ItemID.BattlePotion, ModContent.ItemType<PermanentBattlefrontPotion>());
                PermaPotionRecipeA(this, ModContent.ItemType<CrimsonPotion>(), ModContent.ItemType<PermanentCrimsonPotion>());
                PermaPotionRecipeA(this, ItemID.FlaskofCursedFlames, ModContent.ItemType<PermanentFlaskOfCursedFlames>());
                PermaPotionRecipeA(this, ItemID.FlaskofIchor, ModContent.ItemType<PermanentFlaskOfIchor>());
                PermaPotionRecipeA(this, ItemID.FlaskofVenom, ModContent.ItemType<PermanentFlaskOfVenom>());
                PermaPotionRecipeA(this, ItemID.MagicPowerPotion, ModContent.ItemType<PermanentMagicPowerPotion>());
                PermaPotionRecipeA(this, ItemID.RagePotion, ModContent.ItemType<PermanentRagePotion>());
                PermaPotionRecipeA(this, ItemID.WrathPotion, ModContent.ItemType<PermanentWrathPotion>());
                PermaPotionRecipeA(this, ItemID.SpelunkerPotion, ModContent.ItemType<PermanentSpelunkerPotion>());
                PermaPotionRecipeA(this, ItemID.SwiftnessPotion, ModContent.ItemType<PermanentSwiftnessPotion>());
                PermaPotionRecipeA(this, ItemID.SummoningPotion, ModContent.ItemType<PermanentSummoningPotion>());
                #endregion
                #region add b tier recipes
                PermaPotionRecipeB(this, ModContent.ItemType<BoostPotion>(), ModContent.ItemType<PermanentBoostPotion>());
                PermaPotionRecipeB(this, ItemID.AmmoReservationPotion, ModContent.ItemType<PermanentAmmoReservationPotion>());
                PermaPotionRecipeB(this, ItemID.CratePotion, ModContent.ItemType<PermanentCratePotion>());
                PermaPotionRecipeB(this, ItemID.FishingPotion, ModContent.ItemType<PermanentFishingPotion>());
                PermaPotionRecipeB(this, ItemID.SonarPotion, ModContent.ItemType<PermanentSonarPotion>());
                PermaPotionRecipeB(this, ItemID.FlaskofFire, ModContent.ItemType<PermanentFlaskOfFire>());
                PermaPotionRecipeB(this, ItemID.FlaskofGold, ModContent.ItemType<PermanentFlaskOfGold>());
                PermaPotionRecipeB(this, ItemID.FlaskofNanites, ModContent.ItemType<PermanentFlaskOfNanites>());
                PermaPotionRecipeB(this, ItemID.GillsPotion, ModContent.ItemType<PermanentGillsPotion>());
                PermaPotionRecipeB(this, ItemID.HeartreachPotion, ModContent.ItemType<PermanentHeartreachPotion>());
                PermaPotionRecipeB(this, ItemID.IronskinPotion, ModContent.ItemType<PermanentIronskinPotion>());
                PermaPotionRecipeB(this, ItemID.MiningPotion, ModContent.ItemType<PermanentMiningPotion>());
                PermaPotionRecipeB(this, ItemID.RegenerationPotion, ModContent.ItemType<PermanentRegenerationPotion>());
                PermaPotionRecipeB(this, ModContent.ItemType<ShockwavePotion>(), ModContent.ItemType<PermanentShockwavePotion>());
                PermaPotionRecipeB(this, ItemID.TitanPotion, ModContent.ItemType<PermanentTitanPotion>());
                PermaPotionRecipeB(this, ItemID.InfernoPotion, ModContent.ItemType<PermanentInfernoPotion>());
                #endregion
                #region add c tier recipes
                PermaPotionRecipeC(this, ModContent.ItemType<ShockwavePotion>(), ModContent.ItemType<PermanentShockwavePotion>());
                PermaPotionRecipeC(this, ItemID.BuilderPotion, ModContent.ItemType<PermanentBuilderPotion>());
                PermaPotionRecipeC(this, ItemID.ShinePotion, ModContent.ItemType<PermanentShinePotion>());
                PermaPotionRecipeC(this, ItemID.TrapsightPotion, ModContent.ItemType<PermanentDangersensePotion>());
                PermaPotionRecipeC(this, ItemID.FeatherfallPotion, ModContent.ItemType<PermanentFeatherfallPotion>());
                PermaPotionRecipeC(this, ItemID.FlaskofParty, ModContent.ItemType<PermanentFlaskOfParty>());
                PermaPotionRecipeC(this, ItemID.FlaskofPoison, ModContent.ItemType<PermanentFlaskOfPoison>());
                PermaPotionRecipeC(this, ItemID.FlipperPotion, ModContent.ItemType<PermanentFlipperPotion>());
                PermaPotionRecipeC(this, ItemID.HunterPotion, ModContent.ItemType<PermanentHunterPotion>());
                PermaPotionRecipeC(this, ItemID.InvisibilityPotion, ModContent.ItemType<PermanentInvisibilityPotion>());
                PermaPotionRecipeC(this, ItemID.NightOwlPotion, ModContent.ItemType<PermanentNightOwlPotion>());
                PermaPotionRecipeC(this, ItemID.ThornsPotion, ModContent.ItemType<PermanentThornsPotion>());
                PermaPotionRecipeC(this, ItemID.WarmthPotion, ModContent.ItemType<PermanentWarmthPotion>());
                PermaPotionRecipeC(this, ItemID.WaterWalkingPotion, ModContent.ItemType<PermanentWaterWalkingPotion>());
                #endregion
                #region special perma recipes
                ModRecipe recipe = new ModRecipe(this);
                recipe.AddIngredient(GetItem("DarkSoul"), 50000);
                recipe.AddIngredient(ItemID.GravitationPotion, 20);
                recipe.AddIngredient(ItemID.SoulofFlight, 1);
                recipe.AddIngredient(GetItem("EternalCrystal"), 3);
                recipe.SetResult(ModContent.ItemType<PermanentGravitationPotion>());
                recipe.AddRecipe();

                recipe = new ModRecipe(this);
                recipe.AddIngredient(GetItem("DarkSoul"), 25000);
                recipe.AddIngredient(ItemID.ObsidianSkinPotion, 20);
                recipe.AddIngredient(ItemID.SoulofLight, 1);
                recipe.AddIngredient(GetItem("EternalCrystal"), 2);
                recipe.SetResult(ModContent.ItemType<PermanentObsidianSkinPotion>());
                recipe.AddRecipe();
                #endregion 
            }
            RecipeHelper.EditRecipes();

            ModRecipe recipe1 = new ModRecipe(this);
            recipe1.AddIngredient(ItemID.FallenStar);
            recipe1.AddIngredient(ItemID.Gel, 2);
            recipe1.AddIngredient(ItemID.Bottle, 10);
            recipe1.AddTile(TileID.Bottles);
            recipe1.SetResult(ItemID.LesserManaPotion, 10);
            recipe1.AddRecipe();
        }

    }

    //config moved to separate file

    public class TilePlaceCode : GlobalItem {

        public override bool CanUseItem(Item item, Player player) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                if (item.createWall > 0) {
                    return false; //prevent placing walls
                }
                if (item.createTile > -1) {
                    if (tsorcRevamp.PlaceAllowed[item.createTile]) {
                        return true; //allow placing tiles in PlaceAllowed
                    }
                    else return false; //disallow using item if it places other tiles
                }
                return true; //allow using items if they do not create tiles
            }
            return base.CanUseItem(item, player); //use default value
        }
    }

    public class TileKillCode : GlobalTile {

        public override bool CanKillTile(int x, int y, int type, ref bool blockDamaged) {


            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {

                bool right = !Main.tile[x + 1, y].active();
                bool left = !Main.tile[x - 1, y].active();
                bool below = !Main.tile[x, y - 1].active();
                bool above = !Main.tile[x, y + 1].active();
                if (x < 10 || x > Main.maxTilesX - 10) {//sanity
                    return false;
                }
                else if (y < 10 || y > Main.maxTilesY - 10) {//sanity 
                    return false;
                }
                else if (Main.tile[x, y] == null) {//sanity
                    return false;
                }
                else if (tsorcRevamp.KillAllowed[type]) {//always allow KillAllowed
                    return true;
                }
                else if (tsorcRevamp.Unbreakable[type]) {//always disallow Unbreakable	
                    return false;
                }
                else if (right && left) {//if a tile has no neighboring tiles horizontally, allow breaking
                    return true;
                }
                else if (below && above) {//if a tile has no neighboring tiles vertically, allow breaking
                    return true;
                }
                else return false; //disallow breaking tiles otherwise
            }
            return base.CanKillTile(x, y, type, ref blockDamaged); //use default value
        }

        public override bool CanExplode(int x, int y, int type) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                bool right = !Main.tile[x + 1, y].active();
                bool left = !Main.tile[x - 1, y].active();
                bool below = !Main.tile[x, y - 1].active();
                bool above = !Main.tile[x, y + 1].active();
                bool CanDestroy = false;
                if (type == TileID.Ebonsand || type == TileID.Amethyst || type == TileID.ShadowOrbs) { //shadow temple / corruption chasm stuff that gets blown up
                    CanDestroy = true;
                }

                //check cankilltiles stuff
                if ((right && left) || (above && below) || tsorcRevamp.KillAllowed[type] || (x < 10 || x > Main.maxTilesX - 10) || (y < 10 || y > Main.maxTilesY - 10) || (!Main.tile[x, y].active())) {
                    CanDestroy = true;
                }
                if (Main.tileDungeon[Main.tile[x, y].type]
                    || type == TileID.Silver
                    || type == TileID.Cobalt
                    || type == TileID.Mythril
                    || type == TileID.Adamantite
                    || (tsorcRevamp.Unbreakable[type])
                ) {
                    CanDestroy = false;
                }
                if (!Main.hardMode && type == TileID.Hellstone) {
                    CanDestroy = false;
                }
                return CanDestroy;
            }
            else return base.CanExplode(x, y, type);

        }

        public override bool Slope(int i, int j, int type) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                return false;
            }
            else return base.Slope(i, j, type);
        }
    }

    public class WallKillCode : GlobalWall {
        public override void KillWall(int i, int j, int type, ref bool fail) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                fail = true;
            }
        }
        public override bool CanExplode(int i, int j, int type) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                return false;
            }
            return base.CanExplode(i, j, type);
        }
    }
    public class MiscGlobalTile : GlobalTile {
        public override void NearbyEffects(int i, int j, int type, bool closer) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                Player player = Main.LocalPlayer;
                var pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
                var distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

                if (Main.tile[i, j].type == TileID.LunarMonolith && distance <= 800f && !player.dead && Main.tile[i, j].frameY > 54) { //frameY > 54 means enabled
                    int style = Main.tile[i, j].frameX / 36;
                    switch (style) {
                        case 0:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerVortex) && !tsorcRevampWorld.DownedVortex) {
                                int p = NPC.NewNPC((i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerVortex, 1);
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                NPC.TowerActiveVortex = true;
                                NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                                NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                            }
                            break;

                        case 1:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerNebula) && !tsorcRevampWorld.DownedNebula) {
                                int p = NPC.NewNPC((i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerNebula, 1);
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                NPC.TowerActiveNebula = true;
                                NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                                NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                            }
                            break;

                        case 2:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerStardust) && !tsorcRevampWorld.DownedStardust) {
                                int p = NPC.NewNPC((i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerStardust, 1);
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                NPC.TowerActiveStardust = true;
                                NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                                NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                            }
                            break;

                        case 3:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerSolar) && !tsorcRevampWorld.DownedSolar) {
                                int p = NPC.NewNPC((i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerSolar, 1);
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                NPC.TowerActiveSolar = true;
                                NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                                NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                            }
                            break;
                    }
                } 
            }

            base.NearbyEffects(i, j, type, closer);
        }
    }
}