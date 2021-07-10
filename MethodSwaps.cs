using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using Terraria.UI;

namespace tsorcRevamp {
    class MethodSwaps {

        internal static void ApplyMethodSwaps() {
            On.Terraria.Player.Spawn += SpawnPatch;

            On.Terraria.WorldGen.UpdateLunarApocalypse += StopMoonLord;

            On.Terraria.UI.ChestUI.TryPlacingInChest += ShiftClickPatch;

            On.Terraria.UI.ChestUI.DepositAll += DepositAllPatch;

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

        //stop souls from being shift clicked into chests
        internal static bool ShiftClickPatch(On.Terraria.UI.ChestUI.orig_TryPlacingInChest orig, Item I, bool justCheck) {
            if (I.type == ModContent.ItemType<DarkSoul>()) { return false; } 
            bool flag = false;
            Player player = Main.player[Main.myPlayer];
            Item[] item = player.bank.item;
            if (player.chest > -1) {
                item = Main.chest[player.chest].item;
                flag = Main.netMode == 1;
            }
            else if (player.chest == -2) {
                item = player.bank.item;
            }
            else if (player.chest == -3) {
                item = player.bank2.item;
            }
            else if (player.chest == -4) {
                item = player.bank3.item;
            }
            bool flag2 = false;
            
            if (I.maxStack > 1) {
                for (int i = 0; i < 40; i++) {
                    if (item[i].stack >= item[i].maxStack || !I.IsTheSameAs(item[i])) {
                        continue;
                    }
                    int num = I.stack;
                    if (I.stack + item[i].stack > item[i].maxStack) {
                        num = item[i].maxStack - item[i].stack;
                    }
                    if (justCheck) {
                        flag2 = flag2 || num > 0;
                        break;
                    }
                    I.stack -= num;
                    item[i].stack += num;
                    Main.PlaySound(7);
                    if (I.stack <= 0) {
                        I.SetDefaults();
                        if (flag) {
                            NetMessage.SendData(32, -1, -1, null, player.chest, i);
                        }
                        break;
                    }
                    if (item[i].type == 0) {
                        item[i] = I.Clone();
                        I.SetDefaults();
                    }
                    if (flag) {
                        NetMessage.SendData(32, -1, -1, null, player.chest, i);
                    }
                }
            }
            if (I.stack > 0) {
                for (int j = 0; j < 40; j++) {
                    if (item[j].stack != 0) {
                        continue;
                    }
                    if (justCheck) {
                        flag2 = true;
                        break;
                    }
                    Main.PlaySound(7);
                    item[j] = I.Clone();
                    I.SetDefaults();
                    if (flag) {
                        NetMessage.SendData(32, -1, -1, null, player.chest, j);
                    }
                    break;
                }
            }
            return flag2;
        }

        //stop souls from being deposit all-ed into chests.
        internal static void DepositAllPatch(On.Terraria.UI.ChestUI.orig_DepositAll orig) { 

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

    }
}