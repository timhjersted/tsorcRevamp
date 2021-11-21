using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using Terraria.UI;
using System.Collections.Generic;
using System;
using System.Reflection;
using ReLogic.Graphics;
using System.IO;

namespace tsorcRevamp {
    class MethodSwaps {

        internal static void ApplyMethodSwaps() {
            On.Terraria.Player.Spawn += SpawnPatch;

            On.Terraria.WorldGen.UpdateLunarApocalypse += StopMoonLord;

            On.Terraria.Recipe.FindRecipes += SoulSlotRecipesPatch;

            On.Terraria.NPC.TypeToHeadIndex += MapHeadPatch;

            On.Terraria.Player.TileInteractionsCheckLongDistance += SignTextPatch;

            On.Terraria.NPC.SpawnNPC += BossZenPatch;

            On.Terraria.Main.DrawMenu += DownloadMapButton;
        }

        //allow spawns to be set outside a valid house (for bonfires)
        internal static void SpawnPatch(On.Terraria.Player.orig_Spawn orig, Player self) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Main.InitLifeBytes();
                if (self.whoAmI == Main.myPlayer) {
                    if (Main.mapTime < 5) {
                        Main.mapTime = 5;
                    }
                    Main.quickBG = 10;
                    self.FindSpawn();
                    if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                        if (!Player.CheckSpawn(self.SpawnX, self.SpawnY)) {
                            self.SpawnX = -1;
                            self.SpawnY = -1;
                        } 
                    }
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

        //stop moon lord from spawning after pillars are killed (adventure mode only)
        internal static void StopMoonLord(On.Terraria.WorldGen.orig_UpdateLunarApocalypse orig) {
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

        //allow souls in the soul slot to be included in calculations for craftable recipes
        internal static void SoulSlotRecipesPatch(On.Terraria.Recipe.orig_FindRecipes orig) {
            int num = Main.availableRecipe[Main.focusRecipe];
            float num2 = Main.availableRecipeY[Main.focusRecipe];
            for (int i = 0; i < Recipe.maxRecipes; i++) {
                Main.availableRecipe[i] = 0;
            }
            Main.numAvailableRecipes = 0;
            if (Main.guideItem.type > 0 && Main.guideItem.stack > 0 && Main.guideItem.Name != "") {
                for (int j = 0; j < Recipe.maxRecipes && Main.recipe[j].createItem.type != 0; j++) {
                    for (int k = 0; k < Recipe.maxRequirements && Main.recipe[j].requiredItem[k].type != 0; k++) {
                        if (Main.guideItem.IsTheSameAs(Main.recipe[j].requiredItem[k]) || Main.recipe[j].useWood(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useSand(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useIronBar(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].useFragment(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].AcceptedByItemGroups(Main.guideItem.type, Main.recipe[j].requiredItem[k].type) || Main.recipe[j].usePressurePlate(Main.guideItem.type, Main.recipe[j].requiredItem[k].type)) {
                            Main.availableRecipe[Main.numAvailableRecipes] = j;
                            Main.numAvailableRecipes++;
                            break;
                        }
                    }
                }
            }
            else {
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                Item[] array = null;
                Item item = null;
                array = Main.player[Main.myPlayer].inventory;
                for (int l = 0; l < 58; l++) {
                    item = array[l];
                    if (item.stack > 0) {
                        if (dictionary.ContainsKey(item.netID)) {
                            dictionary[item.netID] += item.stack;
                        }
                        else {
                            dictionary[item.netID] = item.stack;
                        }
                    }
                }
                //new
                item = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
                if (item.stack > 0) {
                    if (dictionary.ContainsKey(item.netID)) {
                        dictionary[item.netID] += item.stack;
                    }
                    else {
                        dictionary[item.netID] = item.stack;
                    }
                }
                //end new
                if (Main.player[Main.myPlayer].chest != -1) {
                    if (Main.player[Main.myPlayer].chest > -1) {
                        array = Main.chest[Main.player[Main.myPlayer].chest].item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -2) {
                        array = Main.player[Main.myPlayer].bank.item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -3) {
                        array = Main.player[Main.myPlayer].bank2.item;
                    }
                    else if (Main.player[Main.myPlayer].chest == -4) {
                        array = Main.player[Main.myPlayer].bank3.item;
                    }
                    for (int m = 0; m < 40; m++) {
                        item = array[m];
                        if (item.stack > 0) {
                            if (dictionary.ContainsKey(item.netID)) {
                                dictionary[item.netID] += item.stack;
                            }
                            else {
                                dictionary[item.netID] = item.stack;
                            }
                        }
                    }
                }
                for (int n = 0; n < Recipe.maxRecipes && Main.recipe[n].createItem.type != 0; n++) {
                    bool flag = true;
                    if (flag) {
                        for (int num3 = 0; num3 < Recipe.maxRequirements && Main.recipe[n].requiredTile[num3] != -1; num3++) {
                            if (!Main.player[Main.myPlayer].adjTile[Main.recipe[n].requiredTile[num3]]) {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag) {
                        for (int num4 = 0; num4 < Recipe.maxRequirements; num4++) {
                            item = Main.recipe[n].requiredItem[num4];
                            if (item.type == 0) {
                                break;
                            }
                            int num5 = item.stack;
                            bool flag2 = false;
                            foreach (int key in dictionary.Keys) {
                                if (Main.recipe[n].useWood(key, item.type) || Main.recipe[n].useSand(key, item.type) || Main.recipe[n].useIronBar(key, item.type) || Main.recipe[n].useFragment(key, item.type) || Main.recipe[n].AcceptedByItemGroups(key, item.type) || Main.recipe[n].usePressurePlate(key, item.type)) {
                                    num5 -= dictionary[key];
                                    flag2 = true;
                                }
                            }
                            if (!flag2 && dictionary.ContainsKey(item.netID)) {
                                num5 -= dictionary[item.netID];
                            }
                            if (num5 > 0) {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag) {
                        bool num9 = !Main.recipe[n].needWater || Main.player[Main.myPlayer].adjWater || Main.player[Main.myPlayer].adjTile[172];
                        bool flag3 = !Main.recipe[n].needHoney || Main.recipe[n].needHoney == Main.player[Main.myPlayer].adjHoney;
                        bool flag4 = !Main.recipe[n].needLava || Main.recipe[n].needLava == Main.player[Main.myPlayer].adjLava;
                        bool flag5 = !Main.recipe[n].needSnowBiome || Main.player[Main.myPlayer].ZoneSnow;
                        if (!(num9 && flag3 && flag4 && flag5)) {
                            flag = false;
                        }
                    }
                    if (flag && RecipeHooks.RecipeAvailable(Main.recipe[n])) {
                        Main.availableRecipe[Main.numAvailableRecipes] = n;
                        Main.numAvailableRecipes++;
                    }
                }
            }
            for (int num6 = 0; num6 < Main.numAvailableRecipes; num6++) {
                if (num == Main.availableRecipe[num6]) {
                    Main.focusRecipe = num6;
                    break;
                }
            }
            if (Main.focusRecipe >= Main.numAvailableRecipes) {
                Main.focusRecipe = Main.numAvailableRecipes - 1;
            }
            if (Main.focusRecipe < 0) {
                Main.focusRecipe = 0;
            }
            float num7 = Main.availableRecipeY[Main.focusRecipe] - num2;
            for (int num8 = 0; num8 < Recipe.maxRecipes; num8++) {
                Main.availableRecipeY[num8] -= num7;
            }
        }

        //stop npc heads from displaying on the map
        private static int MapHeadPatch(On.Terraria.NPC.orig_TypeToHeadIndex orig, int type) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && (!(Main.EquipPage == 1) || Main.mapFullscreen)) {
                NPC npc = new NPC();
                npc.SetDefaults(type);
                //Mechanic is hidden until any mech boss is killed
                if (npc.type == NPCID.Mechanic && !NPC.downedMechBossAny)
                {
                    return 0;
                }
                //Goblin is hidden until the Jungle Wyvern is killed
                else if (npc.type == NPCID.GoblinTinkerer && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()))
                {
                    return 0;
                    
                }
                //Wizard is hidden until The Sorrow is killed
                else if (npc.type == NPCID.Wizard && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
                {
                    return 0;
                }
                else {
                    return orig(type);
                }

            }
            else { return orig(type); }
        }

        //stop sign text from drawing when the player is too far away / does not have line of sight to the sign
        internal static void SignTextPatch(On.Terraria.Player.orig_TileInteractionsCheckLongDistance orig, Player self, int myX, int myY) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && Main.tileSign[Main.tile[myX, myY].type]) {
                if (Main.tile[myX, myY] == null) {
                    Main.tile[myX, myY] = new Tile();
                }
                if (!Main.tile[myX, myY].active()) {
                    return;
                }
                if (Main.tile[myX, myY].type == 21) {
                    orig(self, myX, myY);
                }
                if (Main.tile[myX, myY].type == 88) {
                    orig(self, myX, myY);
                }
                if (Main.tileSign[Main.tile[myX, myY].type]) {
                    Vector2 signPos = new Vector2(myX * 16, myY * 16);
                    Vector2 toSign = signPos - self.position;
                    if (Collision.CanHitLine(self.position, 0, 0, signPos, 0, 0) && toSign.Length() < 240) { 
                        self.noThrow = 2;
                        int num3 = Main.tile[myX, myY].frameX / 18;
                        int num4 = Main.tile[myX, myY].frameY / 18;
                        num3 %= 2;
                        int num7 = myX - num3;
                        int num5 = myY - num4;
                        Main.signBubble = true;
                        Main.signX = num7 * 16 + 16;
                        Main.signY = num5 * 16;
                        int num6 = Sign.ReadSign(num7, num5);
                        if (num6 != -1) {
                            Main.signHover = num6;
                            self.showItemIcon = false;
                            self.showItemIcon2 = -1;
                        }
                    }
                }
                TileLoader.MouseOverFar(myX, myY);
            }
            else {
                orig(self, myX, myY);
            }
        }

        //boss zen actually zens
        internal static void BossZenPatch(On.Terraria.NPC.orig_SpawnNPC orig) {
            bool BossZen = false;

            for (int i = 0; i < Main.maxPlayers; i++) {
                if (!Main.player[i].active || Main.player[i].dead) { continue; }
                if (Main.player[i].HasBuff(ModContent.BuffType<Buffs.BossZenBuff>())) {
                    BossZen = true;
                    break;
                }
            }

            if (BossZen) { return; }
            else {
                orig();
            }
        }

        internal static void DownloadMapButton(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
            orig(self, gameTime);
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            if (Main.menuMode == 16) {
                string downloadText = "Want the Custom Map? Click here to install!";

                string dataDir = Main.SavePath + "\\Mod Configs\\tsorcRevampData";
                string fileName = "\\tsorcMusic.tmod";
                string worldsFolder = Main.SavePath + "\\Worlds";

                Vector2 textOrigin = Main.fontMouseText.MeasureString(downloadText);

                Vector2 position = new Vector2((Main.screenWidth / 2) - (textOrigin.X * 0.5f), 120 + (80 * 6));
                Main.spriteBatch.Begin();
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, downloadText, position, Color.White);
                Main.spriteBatch.End();
                Rectangle x = new Rectangle((int)position.X - 2, (int)position.Y - 2, (int)(textOrigin.X * 1.5f), (int)(textOrigin.Y * 1.5f));
                if (Main.mouseX > position.X && Main.mouseX < position.X + textOrigin.X) {
                    if (Main.mouseY > position.Y && Main.mouseY < position.Y + textOrigin.Y) {
                        if (Main.mouseLeft) {
                            if (File.Exists(dataDir + fileName)) {
                                if (!File.Exists(worldsFolder + fileName)) {

                                    FileInfo fileToCopy = new FileInfo(dataDir + fileName);
                                    mod.Logger.Info("Attempting to copy world.");
                                    try {
                                        fileToCopy.CopyTo(worldsFolder + fileName, false);
                                    }
                                    catch (System.Security.SecurityException e) {
                                        mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                                    }
                                    catch (Exception e) {
                                        mod.Logger.Warn("World copy failed ({0}).", e);
                                    }
                                }
                                else {
                                    mod.Logger.Info("World already exists.");
                                }

                            }
                        }

                    }
                }
            }
        }

    }
}
