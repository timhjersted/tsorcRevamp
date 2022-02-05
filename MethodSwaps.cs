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
using System.Net;
using Microsoft.Xna.Framework.Audio;
using ReLogic.Utilities;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using On.Terraria.Utilities;
using tsorcRevamp.UI;

namespace tsorcRevamp {
    class MethodSwaps {

        internal static void ApplyMethodSwaps() {
            On.Terraria.Player.Spawn += SpawnPatch;

            On.Terraria.WorldGen.UpdateLunarApocalypse += StopMoonLord;

            On.Terraria.Recipe.FindRecipes += SoulSlotRecipesPatch;

            On.Terraria.Player.TileInteractionsCheckLongDistance += SignTextPatch;

            On.Terraria.NPC.SpawnNPC += BossZenPatch;

            On.Terraria.Main.DrawMenu += DownloadMapButton;

            On.Terraria.Main.DrawPlayer += StaminaBar;

            On.Terraria.Main.DrawPlayer += CurseMeter;

            On.Terraria.Main.StartInvasion += BlockInvasions;

            //On.Terraria.NPC.AI_037_Destroyer += DestroyerAIRevamp;

            On.Terraria.Utilities.NPCUtils.TargetClosestOldOnesInvasion += OldOnesArmyPatch;


            On.Terraria.NPC.AI_111_DD2LightningBug += LightningBugTeleport;

            On.Terraria.Player.QuickBuff += CustomQuickBuff;
            On.Terraria.Player.QuickMana += CustomQuickMana;
            On.Terraria.Player.QuickHeal += CustomQuickHeal;

            On.Terraria.UI.ChestUI.LootAll += PotionBagLootAllPatch;

            On.Terraria.Player.HasUnityPotion += HasWormholePotion;
            On.Terraria.Player.TakeUnityPotion += ConsumeWormholePotion;
        }

        private static void PotionBagLootAllPatch(On.Terraria.UI.ChestUI.orig_LootAll orig)
        {
            if (Main.LocalPlayer.HasItem(ModContent.ItemType<PotionBag>()))
            {
                Player player = Main.player[Main.myPlayer];
                Item[] inventory;
                if (player.chest > -1)
                {
                    inventory = Main.chest[player.chest].item;
                }
                else if (player.chest == -3)
                {
                    inventory = player.bank2.item;
                }

                else if (player.chest == -4)
                {
                    inventory = player.bank3.item;
                }
                else
                {
                    inventory = player.bank.item;
                }

                for (int i = 0; i < 40; i++)
                {
                    if (inventory[i].type > 0 && PotionBagUIState.IsValidPotion(inventory[i]))
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().ShiftClickSlot(inventory, ItemSlot.Context.BankItem, i);
                        if(Main.netMode == 1 && player.chest >= -1)
                        {
                            NetMessage.SendData(32, -1, -1, null, player.chest, i);
                        }
                    }
                }
            }

            orig();
        }

        private static void CustomQuickBuff(On.Terraria.Player.orig_QuickBuff orig, Player player)
        {
            if (player.noItems)
                return;

            Item[] PotionBagItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;

            for (int i = 0; i < 58; i++)
            {
                CheckUseBuffPotion(player.inventory[i], player);
            }
            for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
            {
                if (!(bool)PotionBagItems[i]?.favorited)
                {
                    CheckUseBuffPotion(PotionBagItems[i], player);
                }
            }
        }

        private static void CheckUseBuffPotion(Item item, Player player)
        {
            if (player.CountBuffs() == Player.MaxBuffs)
                return;

            if (item.stack <= 0 || item.type <= 0 || item.buffType <= 0 || item.summon || item.buffType == 90)
                return;

            int buffType = item.buffType;
            bool validItem = ItemLoader.CanUseItem(item, player);
            for (int j = 0; j < Player.MaxBuffs; j++)
            {
                if (buffType == BuffID.FairyBlue && (player.buffType[j] == BuffID.FairyBlue || player.buffType[j] == BuffID.FairyRed || player.buffType[j] == BuffID.FairyGreen))
                {
                    validItem = false;
                    break;
                }
                if (player.buffType[j] == buffType)
                {
                    validItem = false;
                    break;
                }
                if (Main.meleeBuff[buffType] && Main.meleeBuff[player.buffType[j]])
                {
                    validItem = false;
                    break;
                }
            }

            if (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType])
            {
                for (int k = 0; k < Player.MaxBuffs; k++)
                {
                    if (Main.lightPet[player.buffType[k]] && Main.lightPet[item.buffType])
                        validItem = false;

                    if (Main.vanityPet[player.buffType[k]] && Main.vanityPet[item.buffType])
                        validItem = false;
                }
            }

            if (player.whoAmI == Main.myPlayer && item.type == ItemID.Carrot && !Main.cEd)
                validItem = false;

            if (buffType == BuffID.FairyBlue)
            {
                buffType = Main.rand.Next(3);
                if (buffType == 0)
                    buffType = BuffID.FairyBlue;

                if (buffType == 1)
                    buffType = BuffID.FairyRed;

                if (buffType == 2)
                    buffType = BuffID.FairyGreen;
            }

            if (validItem)
            {
                UsePotion(item, player);
            }
        }

        private static void CustomQuickMana(On.Terraria.Player.orig_QuickMana orig, Player player)
        {
            if (player.noItems || player.statMana == player.statManaMax2)
            {
                return;
            }
            Item selectedItem = player.QuickMana_GetItemToUse();
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }

            Item[] PotionBagItems = player.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;
            
            for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
            {
                if (PotionBagItems[i] != null && PotionBagItems[i].type != 0 && (player.potionDelay == 0 || !PotionBagItems[i].potion) && ItemLoader.CanUseItem(PotionBagItems[i], player) && player.GetHealMana(PotionBagItems[i], true) > player.GetHealMana(selectedItem, true))
                {
                    selectedItem = PotionBagItems[i];
                }
            }

            UsePotion(selectedItem, player);
            
        }
        
        private static void CustomQuickHeal(On.Terraria.Player.orig_QuickHeal orig, Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            tsorcRevampEstusPlayer estusPlayer = player.GetModPlayer<tsorcRevampEstusPlayer>();

            if (modPlayer.BearerOfTheCurse && player.statLife < player.statLifeMax2)
            {
                if (player == Main.LocalPlayer && !player.mouseInterface && estusPlayer.estusChargesCurrent > 0 && player.itemAnimation == 0
                && player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift && !modPlayer.isDodging && !estusPlayer.isDrinking && !player.HasBuff(BuffID.Stoned) && !player.HasBuff(BuffID.Frozen))
                {
                    estusPlayer.isDrinking = true;
                    estusPlayer.estusDrinkTimer = 0;
                    player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 120);
                    player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 120);
                }
                return;
            }

            if (player.noItems || player.statLife == player.statLifeMax2 || player.potionDelay > 0)
                return;

            Item selectedItem = player.QuickHeal_GetItemToUse();
            if (selectedItem == null)
            {
                selectedItem = new Item();
                selectedItem.SetDefaults(0);
            }
            Item[] PotionBagItems = modPlayer.PotionBagItems;
            if (!player.HasBuff(BuffID.PotionSickness))
            {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++)
                {
                    if (PotionBagItems[i] != null && PotionBagItems[i].potion && ItemLoader.CanUseItem(PotionBagItems[i], player) && (selectedItem == null || player.GetHealLife(PotionBagItems[i], true) > player.GetHealLife(selectedItem, true)))
                    {
                        selectedItem = PotionBagItems[i];
                    }
                }
                
                UsePotion(selectedItem, player);                
            }
        }

        //Generic "use item" code. Since items can be any or all of the 3 categories at once, this handles all of it.
        private static void UsePotion(Item item, Player player)
        {
            Main.PlaySound(item.UseSound, player.position);
            if (item.potion)
            {
                if (item.type == 227)
                {
                    player.potionDelay = player.restorationDelayTime;
                    player.AddBuff(BuffID.PotionSickness, player.potionDelay);
                }
                else
                {
                    player.potionDelay = player.potionDelayTime;
                    player.AddBuff(BuffID.PotionSickness, player.potionDelay);
                }
            }

            ItemLoader.UseItem(item, player);
            
            int healLife = player.GetHealLife(item, true);
            int healMana = player.GetHealMana(item, true);
            player.statLife += healLife;
            player.statMana += healMana;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;

            if (player.statMana > player.statManaMax2)
                player.statMana = player.statManaMax2;

            if (healLife > 0 && Main.myPlayer == player.whoAmI)
                player.HealEffect(healLife, true);

            if (healMana > 0)
            {
                player.AddBuff(BuffID.ManaSickness, 300);
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(healMana);
            }

            if(item.buffType > 0)
            {
                int buffTime = item.buffTime;
                if(buffTime == 0)
                {
                    buffTime = 3600;
                }
                player.AddBuff(item.buffType, buffTime);
            }

            Main.PlaySound(item.UseSound, player.position);

            if (ItemLoader.ConsumeItem(item, player))
                item.stack--;

            if (item.stack <= 0)
                item.TurnToAir();

            Recipe.FindRecipes();
        }

        private static void OldOnesArmyPatch(NPCUtils.orig_TargetClosestOldOnesInvasion orig, NPC searcher, bool faceTarget, Vector2? checkPosition)
        {
            if(!Terraria.GameContent.Events.DD2Event.Ongoing)
            {
                searcher.TargetClosest(faceTarget);
            }
            else
            {
                orig(searcher, faceTarget, checkPosition);
            }
        }

        private static void LightningBugTeleport(On.Terraria.NPC.orig_AI_111_DD2LightningBug orig, NPC self)
        {
            //Put extra things you want it to do before it runs its normal ai code here
            orig(self); //Run its normal ai
            //Put extra things you want it to do once it is done with its normal ai here. Before vs after usually doesn't matter unless you're trying to manipulate its ai somehow.
        }


        //allow spawns to be set outside a valid house (for bonfires)
        internal static void SpawnPatch(On.Terraria.Player.orig_Spawn orig, Player self) {
            Main.InitLifeBytes();
            if (self.whoAmI == Main.myPlayer) {
                if (Main.mapTime < 5) {
                    Main.mapTime = 5;
                }
                Main.quickBG = 10;
                self.FindSpawn();
                if (!SpawnCheck(self)) {
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

        //Checks if a bed or bonfire is within 10 blocks of a player
        internal static bool SpawnCheck(Player self)
        {

            for (int i = -10; i < 10; i++) {
                for (int j = -10; j < 10; j++)
                {
                    if (i + self.SpawnX > 0 && j + self.SpawnY > 0)
                    {
                        if(Main.tile.GetUpperBound(0) > self.SpawnX && Main.tile.GetUpperBound(1) > self.SpawnY) { 
                            if (Main.tile[i + self.SpawnX, j + self.SpawnY] != null)
                            {
                                Tile thisTile = Main.tile[i + self.SpawnX, j + self.SpawnY];
                                if (thisTile.active())
                                {
                                    if (thisTile.type == TileID.Beds || thisTile.type == ModContent.TileType<Tiles.BonfireCheckpoint>())
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
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
            tsorcRevamp thisMod = (tsorcRevamp)mod;

            if (Main.mouseLeftRelease)
            {
                thisMod.UICooldown = false;
            }

           
            if (Main.menuMode == 16) {

                string downloadText = "Copy new Story of Red Cloud Adventure Map to Worlds Folder";

                if (thisMod.worldButtonClicked)
                {
                    downloadText = "Map copied! Hit back and select it to begin!";
                }

                Color downloadTextColor = Main.DiscoColor;
                string dataDir = Main.SavePath + "\\Mod Configs\\tsorcRevampData";

                string baseMapFileName = "\\tsorcBaseMap.wld";
                string userMapFileName = "\\TheStoryofRedCloud.wld";
                string worldsFolder = Main.SavePath + "\\Worlds";

                Vector2 downloadTextOrigin = Main.fontMouseText.MeasureString(downloadText);
                float textScale = 2;
                Vector2 downloadTextPosition = new Vector2((Main.screenWidth / 2) - (downloadTextOrigin.X * 0.5f * textScale), 120 + (80 * 6));

                
                if (Main.mouseX > downloadTextPosition.X && Main.mouseX < downloadTextPosition.X + (downloadTextOrigin.X * textScale)) {
                    if (Main.mouseY > downloadTextPosition.Y && Main.mouseY < downloadTextPosition.Y + (downloadTextOrigin.Y * textScale)) {

                        downloadTextColor = Color.Yellow;

                        if (Main.mouseLeft && !thisMod.UICooldown) {
                            thisMod.worldButtonClicked = true;
                            thisMod.UICooldown = true;
                            if (File.Exists(dataDir + baseMapFileName)) {
                                if (!File.Exists(worldsFolder + userMapFileName)) {

                                    FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                                    mod.Logger.Info("Attempting to copy world.");
                                    try
                                    {
                                        fileToCopy.CopyTo(worldsFolder + userMapFileName, false);
                                    }
                                    catch (System.Security.SecurityException e) {
                                        mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                                    }
                                    catch (Exception e) {
                                        mod.Logger.Warn("World copy failed ({0}).", e);
                                    }
                                }
                                else {
                                    mod.Logger.Info("World already exists. Making renamed copy.");
                                    FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                                    try
                                    {
                                        string newFileName;
                                        bool validName = false;
                                        int worldCount = 1;
                                        do
                                        {
                                            newFileName = "\\TheStoryOfRedCloud_" + worldCount.ToString() + ".wld";
                                            if(File.Exists(worldsFolder + newFileName))
                                            {
                                                worldCount++;
                                                if (worldCount > 255)
                                                {
                                                    mod.Logger.Warn("World copy failed, too many copies.");
                                                }
                                            }
                                            else
                                            {
                                                validName = true;
                                            }
                                        } while (!validName);

                                        fileToCopy.CopyTo(worldsFolder + newFileName, false);
                                    }
                                    catch (System.Security.SecurityException e)
                                    {
                                        mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                                    }
                                    catch (Exception e)
                                    {
                                        mod.Logger.Warn("World copy failed ({0}).", e);
                                    }
                                }
                            }
                        }
                    }
                }
                Main.spriteBatch.Begin();
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, downloadText, new Vector2(downloadTextPosition.X+2, downloadTextPosition.Y+2), Color.Black, 0, Vector2.Zero, textScale, SpriteEffects.None, 0);
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, downloadText, downloadTextPosition, downloadTextColor, 0, Vector2.Zero, textScale, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }

            else if(Main.menuMode == 0)
            {
                string musicModDir = Main.SavePath + "\\Mods\\tsorcMusic.tmod";
                //This goes here so that it runs *after* the first reload has finished and the game has transitioned back to the main menu.
                //Do *not* want to initiate a second reload in the middle of the first.
                if (tsorcRevamp.ReloadNeeded)
                {
                    tsorcRevamp.EnableMusicAndReload();
                }
                if (tsorcRevamp.SpecialReloadNeeded)
                {
                    object[] modParam = new object[1] { "tsorcMusic" };
                    typeof(ModLoader).GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
                    typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
                }

                //Only display this if necessary
                else if (!File.Exists(musicModDir) || tsorcRevamp.MusicNeedsUpdate)
                {
                    String musicText = "";
                    if (tsorcRevamp.DownloadingMusic)
                    {
                        musicText = "Download in progress: " + (int)tsorcRevamp.MusicDownloadProgress + "%";
                    }
                    else if (!File.Exists(musicModDir))
                    {
                        musicText = "Click here to get the Story of Red Cloud music mod!";
                    }
                    else if (tsorcRevamp.MusicNeedsUpdate)
                    {
                        musicText = "Music mod update available, click here to download!";
                    }
                   
                    float musicTextScale = 2;
                    Vector2 musicTextOrigin = Main.fontMouseText.MeasureString(musicText);
                    Vector2 musicTextPosition = new Vector2((Main.screenWidth / 2) - musicTextOrigin.X * 0.5f * musicTextScale, 70 + (80 * 6));
                    Color musicTextColor = Main.DiscoColor;

                    if ((Main.mouseX > musicTextPosition.X && Main.mouseX < musicTextPosition.X + (musicTextOrigin.X * musicTextScale)) && !tsorcRevamp.DownloadingMusic)
                    {
                        if (Main.mouseY > musicTextPosition.Y && Main.mouseY < musicTextPosition.Y + (musicTextOrigin.Y * musicTextScale))
                        {
                            musicTextColor = Color.Yellow;

                            if (Main.mouseLeft)
                            {                                
                                tsorcRevamp.MusicDownload();                                
                            }
                        }
                    }
         
                    Main.spriteBatch.Begin();
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, musicText, new Vector2(musicTextPosition.X + 2, musicTextPosition.Y + 2), Color.Black, 0, Vector2.Zero, musicTextScale, SpriteEffects.None, 0);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, Main.fontMouseText, musicText, musicTextPosition, musicTextColor, 0, Vector2.Zero, musicTextScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                }               
            }      
            
            else
            {
                thisMod.worldButtonClicked = false;
            }
        }

        internal static void StaminaBar(On.Terraria.Main.orig_DrawPlayer orig, Main self, Player drawPlayer, Vector2 Position, float rotation, Vector2 rotationOrigin, float shadow)
        {
            orig(self, drawPlayer, Position, rotation, rotationOrigin, shadow);

            if (drawPlayer.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                float staminaCurrent = drawPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                float staminaMax = drawPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
                float staminaPercentage = (float)staminaCurrent / staminaMax;
                if (staminaPercentage < 1f && !drawPlayer.dead)
                {
                    float abovePlayer = 45f; //how far above the player should the bar be?
                    Texture2D barFill = ModContent.GetTexture("tsorcRevamp/Textures/StaminaBar_full");
                    Texture2D barEmpty = ModContent.GetTexture("tsorcRevamp/Textures/StaminaBar_empty");

                    //this is the position on the screen. it should remain relatively constant unless the window is resized
                    Point barOrigin = (drawPlayer.Center - new Vector2(barEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint();
                    //Main.NewText("" + barOrigin.X + ", " + barOrigin.Y);

                    Rectangle emptyDestination = new Rectangle(barOrigin.X, barOrigin.Y, barEmpty.Width, barEmpty.Height);

                    //empty bar has detailing, so offset the filled bar's destination
                    int padding = 5;
                    //scale the width by the stam percentage
                    Rectangle fillDestination = new Rectangle(barOrigin.X + padding, barOrigin.Y, (int)(staminaPercentage * barFill.Width), barFill.Height);

                    Main.spriteBatch.Draw(barEmpty, emptyDestination, Color.White);
                    Main.spriteBatch.Draw(barFill, fillDestination, Color.White);
                }
            }
        }

        public static Texture2D Crop(Texture2D image, Rectangle source)
        {
            Texture2D croppedImage = new Texture2D(image.GraphicsDevice, source.Width, source.Height);

            Color[] imageData = new Color[image.Width * image.Height];
            Color[] cropData = new Color[source.Width * source.Height];

            image.GetData<Color>(imageData);

            int index = 0;

            for (int y = source.Y; y < source.Height; y++)
            {
                for (int x = source.X; x < source.Width; x++)
                {
                    cropData[index] = imageData[y * image.Width + x];
                    index++;
                }
            }
            croppedImage.SetData<Color>(cropData);
            return croppedImage;
        }

        internal static void CurseMeter(On.Terraria.Main.orig_DrawPlayer orig, Main self, Player drawPlayer, Vector2 Position, float rotation, Vector2 rotationOrigin, float shadow)
        {
            orig(self, drawPlayer, Position, rotation, rotationOrigin, shadow);

            if (drawPlayer.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                float curseCurrent = drawPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel;
                float powerfulCurseCurrent = drawPlayer.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel;

                float curseMax = 100;
                float cursePercentage = (float)curseCurrent / curseMax;
                cursePercentage = Utils.Clamp(cursePercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                //Main.NewText(cursePercentage);

                float powerfulCurseMax = 500;
                float powerfulCursePercentage = (float)powerfulCurseCurrent / powerfulCurseMax;
                powerfulCursePercentage = Utils.Clamp(powerfulCursePercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                if ((cursePercentage > 0.01f || powerfulCursePercentage > 0.01f) && !drawPlayer.dead) //0f wasn't working because aparently the minimum % it sits at is 0.01f, so dumb
                {
                    float abovePlayer = 82f; //how far above the player should the bar be?
                    Texture2D meterFull = ModContent.GetTexture("tsorcRevamp/Textures/CurseMeter_full");
                    Texture2D powerfulMeterFull = ModContent.GetTexture("tsorcRevamp/Textures/CurseMeter_powerfulFull");
                    Texture2D meterEmpty = ModContent.GetTexture("tsorcRevamp/Textures/CurseMeter_empty");


                    //this is the position on the screen. it should remain relatively constant unless the window is resized
                    Point barOrigin = (drawPlayer.Center - new Vector2(meterEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint(); //As they are all the same size, they can use the same origin

                    Rectangle barDestination = new Rectangle(barOrigin.X, barOrigin.Y, meterEmpty.Width, meterEmpty.Height);
                    Rectangle fullBarDestination = new Rectangle(barOrigin.X, barOrigin.Y + (int)(meterFull.Height * (1 - cursePercentage)), meterEmpty.Width, (int)(meterFull.Height));
                    Rectangle powerfulFullBarDestination = new Rectangle(barOrigin.X, barOrigin.Y + (int)(powerfulMeterFull.Height * (1 - powerfulCursePercentage)), meterEmpty.Width, (int)(powerfulMeterFull.Height));


                    Main.spriteBatch.Draw(meterEmpty, barDestination, Color.White);
                    Main.spriteBatch.Draw(Crop(meterFull, new Rectangle(0, (int)(meterFull.Height * (1 - cursePercentage)), meterFull.Width, meterFull.Height)), fullBarDestination, Color.White); 
                    Main.spriteBatch.Draw(Crop(powerfulMeterFull, new Rectangle(0, (int)(powerfulMeterFull.Height * (1 - powerfulCursePercentage)), powerfulMeterFull.Width, powerfulMeterFull.Height)), powerfulFullBarDestination, Color.White); 
                }
            }
        }

        internal static void BlockInvasions(On.Terraria.Main.orig_StartInvasion orig, int type)
        {
            //The game sets time to 0 at the start of the day *right* before it checks to naturally spawn invasions.
            //Only applies to adventure mode
            if (Main.time == 0 && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return;
            }
            else
            {
                orig(type);
            }
        }


        private static void DestroyerAIRevamp(On.Terraria.NPC.orig_AI_037_Destroyer orig, NPC npc)
        {
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
                npc.TargetClosest();

            if (npc.type >= 134 && npc.type <= 136)
            {
                npc.velocity.Length();
                if (npc.type == 134 || (npc.type != 134 && Main.npc[(int)npc.ai[1]].alpha < 128))
                {
                    if (npc.alpha != 0)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int num = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 182, 0f, 0f, 100, default(Color), 2f);
                            Main.dust[num].noGravity = true;
                            Main.dust[num].noLight = true;
                        }
                    }

                    npc.alpha -= 42;
                    if (npc.alpha < 0)
                        npc.alpha = 0;
                }
            }

            if (npc.type > 134)
            {
                bool flag = false;
                if (npc.ai[1] <= 0f)
                    flag = true;
                else if (Main.npc[(int)npc.ai[1]].life <= 0)
                    flag = true;

                if (flag)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                }
            }

            if (Main.netMode != 1)
            {
                if (npc.ai[0] == 0f && npc.type == 134)
                {
                    npc.ai[3] = npc.whoAmI;
                    npc.realLife = npc.whoAmI;
                    int num2 = 0;
                    int num3 = npc.whoAmI;
                    int num4 = 80;
                    for (int j = 0; j <= num4; j++)
                    {
                        int num5 = 135;
                        if (j == num4)
                            num5 = 136;

                        num2 = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)npc.height), num5, npc.whoAmI);
                        Main.npc[num2].ai[3] = npc.whoAmI;
                        Main.npc[num2].realLife = npc.whoAmI;
                        Main.npc[num2].ai[1] = num3;
                        Main.npc[num3].ai[0] = num2;
                        NetMessage.SendData(23, -1, -1, null, num2);
                        num3 = num2;
                    }
                }

                if (npc.type == 135)
                {
                    npc.localAI[0] += Main.rand.Next(4);
                    if (npc.localAI[0] >= (float)Main.rand.Next(1400, 26000))
                    {
                        npc.localAI[0] = 0f;
                        npc.TargetClosest();
                        if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                        {
                            Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)(npc.height / 2));
                            float num6 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector.X + (float)Main.rand.Next(-20, 21);
                            float num7 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector.Y + (float)Main.rand.Next(-20, 21);
                            float num8 = (float)Math.Sqrt(num6 * num6 + num7 * num7);
                            num8 = 8f / num8;
                            num6 *= num8;
                            num7 *= num8;
                            num6 += (float)Main.rand.Next(-20, 21) * 0.05f;
                            num7 += (float)Main.rand.Next(-20, 21) * 0.05f;
                            int num9 = 22;
                            if (Main.expertMode)
                                num9 = 18;

                            int num10 = 100;
                            vector.X += num6 * 5f;
                            vector.Y += num7 * 5f;
                            int num11 = Projectile.NewProjectile(vector.X, vector.Y, num6, num7, num10, num9, 0f, Main.myPlayer);
                            Main.projectile[num11].timeLeft = 300;
                            npc.netUpdate = true;
                        }
                    }
                }
            }

            int num12 = (int)(npc.position.X / 16f) - 1;
            int num13 = (int)((npc.position.X + (float)npc.width) / 16f) + 2;
            int num14 = (int)(npc.position.Y / 16f) - 1;
            int num15 = (int)((npc.position.Y + (float)npc.height) / 16f) + 2;
            if (num12 < 0)
                num12 = 0;

            if (num13 > Main.maxTilesX)
                num13 = Main.maxTilesX;

            if (num14 < 0)
                num14 = 0;

            if (num15 > Main.maxTilesY)
                num15 = Main.maxTilesY;

            bool flag2 = false;
            if (!flag2)
            {
                Vector2 vector2 = default(Vector2);
                for (int k = num12; k < num13; k++)
                {
                    for (int l = num14; l < num15; l++)
                    {
                        if (Main.tile[k, l] != null && ((Main.tile[k, l].nactive() && (Main.tileSolid[Main.tile[k, l].type] || (Main.tileSolidTop[Main.tile[k, l].type] && Main.tile[k, l].frameY == 0))) || Main.tile[k, l].liquid > 64))
                        {
                            vector2.X = k * 16;
                            vector2.Y = l * 16;
                            if (npc.position.X + (float)npc.width > vector2.X && npc.position.X < vector2.X + 16f && npc.position.Y + (float)npc.height > vector2.Y && npc.position.Y < vector2.Y + 16f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!flag2)
            {
                if (npc.type != 135 || npc.ai[2] != 1f)
                    Lighting.AddLight((int)((npc.position.X + (float)(npc.width / 2)) / 16f), (int)((npc.position.Y + (float)(npc.height / 2)) / 16f), 0.3f, 0.1f, 0.05f);

                npc.localAI[1] = 1f;
                if (npc.type == 134)
                {
                    Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                    int num16 = 1000;
                    bool flag3 = true;
                    if (npc.position.Y > Main.player[npc.target].position.Y)
                    {
                        for (int m = 0; m < 255; m++)
                        {
                            if (Main.player[m].active)
                            {
                                Rectangle rectangle2 = new Rectangle((int)Main.player[m].position.X - num16, (int)Main.player[m].position.Y - num16, num16 * 2, num16 * 2);
                                if (rectangle.Intersects(rectangle2))
                                {
                                    flag3 = false;
                                    break;
                                }
                            }
                        }

                        if (flag3)
                            flag2 = true;
                    }
                }
            }
            else
            {
                npc.localAI[1] = 0f;
            }

            float num17 = 16f;
            if (Main.dayTime || Main.player[npc.target].dead)
            {
                flag2 = false;
                npc.velocity.Y += 1f;
                if ((double)npc.position.Y > Main.worldSurface * 16.0)
                {
                    npc.velocity.Y += 1f;
                    num17 = 32f;
                }

                if ((double)npc.position.Y > Main.rockLayer * 16.0)
                {
                    for (int n = 0; n < 200; n++)
                    {
                        if (Main.npc[n].aiStyle == npc.aiStyle)
                            Main.npc[n].active = false;
                    }
                }
            }

            float num18 = 0.1f;
            float num19 = 0.15f;
            Vector2 vector3 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
            float num20 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
            float num21 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
            num20 = (int)(num20 / 16f) * 16;
            num21 = (int)(num21 / 16f) * 16;
            vector3.X = (int)(vector3.X / 16f) * 16;
            vector3.Y = (int)(vector3.Y / 16f) * 16;
            num20 -= vector3.X;
            num21 -= vector3.Y;
            float num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {
                try
                {
                    vector3 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                    num20 = Main.npc[(int)npc.ai[1]].position.X + (float)(Main.npc[(int)npc.ai[1]].width / 2) - vector3.X;
                    num21 = Main.npc[(int)npc.ai[1]].position.Y + (float)(Main.npc[(int)npc.ai[1]].height / 2) - vector3.Y;
                }
                catch
                {
                }

                npc.rotation = (float)Math.Atan2(num21, num20) + 1.57f;
                num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
                int num23 = (int)(44f * npc.scale);
                num22 = (num22 - (float)num23) / num22;
                num20 *= num22;
                num21 *= num22;
                npc.velocity = Vector2.Zero;
                npc.position.X += num20;
                npc.position.Y += num21;
                return;
            }

            if (!flag2)
            {
                npc.TargetClosest();
                npc.velocity.Y += 0.15f;
                if (npc.velocity.Y > num17)
                    npc.velocity.Y = num17;

                if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.4)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X -= num18 * 1.1f;
                    else
                        npc.velocity.X += num18 * 1.1f;
                }
                else if (npc.velocity.Y == num17)
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18;
                }
                else if (npc.velocity.Y > 4f)
                {
                    if (npc.velocity.X < 0f)
                        npc.velocity.X += num18 * 0.9f;
                    else
                        npc.velocity.X -= num18 * 0.9f;
                }
            }
            else
            {
                if (npc.soundDelay == 0)
                {
                    float num24 = num22 / 40f;
                    if (num24 < 10f)
                        num24 = 10f;

                    if (num24 > 20f)
                        num24 = 20f;

                    npc.soundDelay = (int)num24;
                    Main.PlaySound(15, (int)npc.position.X, (int)npc.position.Y);
                }

                num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
                float num25 = Math.Abs(num20);
                float num26 = Math.Abs(num21);
                float num27 = num17 / num22;
                num20 *= num27;
                num21 *= num27;
                if (((npc.velocity.X > 0f && num20 > 0f) || (npc.velocity.X < 0f && num20 < 0f)) && ((npc.velocity.Y > 0f && num21 > 0f) || (npc.velocity.Y < 0f && num21 < 0f)))
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num19;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num19;

                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num19;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num19;
                }

                if ((npc.velocity.X > 0f && num20 > 0f) || (npc.velocity.X < 0f && num20 < 0f) || (npc.velocity.Y > 0f && num21 > 0f) || (npc.velocity.Y < 0f && num21 < 0f))
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18;

                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num18;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num18;

                    if ((double)Math.Abs(num21) < (double)num17 * 0.2 && ((npc.velocity.X > 0f && num20 < 0f) || (npc.velocity.X < 0f && num20 > 0f)))
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y += num18 * 2f;
                        else
                            npc.velocity.Y -= num18 * 2f;
                    }

                    if ((double)Math.Abs(num20) < (double)num17 * 0.2 && ((npc.velocity.Y > 0f && num21 < 0f) || (npc.velocity.Y < 0f && num21 > 0f)))
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X += num18 * 2f;
                        else
                            npc.velocity.X -= num18 * 2f;
                    }
                }
                else if (num25 > num26)
                {
                    if (npc.velocity.X < num20)
                        npc.velocity.X += num18 * 1.1f;
                    else if (npc.velocity.X > num20)
                        npc.velocity.X -= num18 * 1.1f;

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.5)
                    {
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y += num18;
                        else
                            npc.velocity.Y -= num18;
                    }
                }
                else
                {
                    if (npc.velocity.Y < num21)
                        npc.velocity.Y += num18 * 1.1f;
                    else if (npc.velocity.Y > num21)
                        npc.velocity.Y -= num18 * 1.1f;

                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num17 * 0.5)
                    {
                        if (npc.velocity.X > 0f)
                            npc.velocity.X += num18;
                        else
                            npc.velocity.X -= num18;
                    }
                }
            }

            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
            if (npc.type != 134)
                return;

            if (flag2)
            {
                if (npc.localAI[0] != 1f)
                    npc.netUpdate = true;

                npc.localAI[0] = 1f;
            }
            else
            {
                if (npc.localAI[0] != 0f)
                    npc.netUpdate = true;

                npc.localAI[0] = 0f;
            }

            if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                npc.netUpdate = true;
        }


        internal static bool HasWormholePotion(On.Terraria.Player.orig_HasUnityPotion orig, Player self) {
            bool hasWormhole = false;
            for (int i = 0; i < 58; i++) {
                if (self.inventory[i].type == ItemID.WormholePotion && self.inventory[i].stack > 0) {
                    hasWormhole = true;
                    break;
                }
            }

            if (!hasWormhole) {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++) {
                    if (self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[i].type == ItemID.WormholePotion) {
                        hasWormhole = true;
                        break;
                    }
                }
            }

            return hasWormhole;
        }

        internal static void ConsumeWormholePotion(On.Terraria.Player.orig_TakeUnityPotion orig, Player self) {
            int wormholeSlot = 0;
            bool potionBag = false;

            for (int i = 0; i < 58; i++) {
                if (self.inventory[i].type == ItemID.WormholePotion && self.inventory[i].stack > 0) {
                    wormholeSlot = i;
                    break;
                }
            }

            if (wormholeSlot == 0) {
                for (int i = 0; i < PotionBagUIState.POTION_BAG_SIZE; i++) {
                    if (self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[i].type == ItemID.WormholePotion) {
                        wormholeSlot = i;
                        potionBag = true;
                        break;
                    }
                }
            }

            Item wormholePotion;

            if (potionBag) {
                wormholePotion = self.GetModPlayer<tsorcRevampPlayer>().PotionBagItems[wormholeSlot];
            }
            else {
                wormholePotion = self.inventory[wormholeSlot];
            }

            if (ItemLoader.ConsumeItem(wormholePotion, self)) {
                wormholePotion.stack--;
            }
            if (wormholePotion.stack <= 0) {
                wormholePotion.TurnToAir();
            }
            
        }
    }
}
