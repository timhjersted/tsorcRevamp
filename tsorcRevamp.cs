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

namespace tsorcRevamp {
    public class tsorcRevamp : Mod {
        public static ModHotKey toggleDragoonBoots;
        public override void Load() {
            toggleDragoonBoots = RegisterHotKey("Dragoon Boots", "Z");

            On.Terraria.NPC.SpawnSkeletron += SkeletronPatch;

            On.Terraria.UI.ChestUI.DepositAll += DepositAllPatch;
        }


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
        public override void Unload() {
            toggleDragoonBoots = null;
            
        }
        #region permanent potion recipes
        public static void PermaPotionRecipeS(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 150000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeA(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 50000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeB(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 25000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeC(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddIngredient(IngredientPotion, 20);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        #endregion
        public override void AddRecipes() {
            RecipeHelper.EditRecipes();
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
            PermaPotionRecipeB(this, ModContent.ItemType<ShockwavePotion>(), ModContent.ItemType<PermanentShockwavePotion>());
            PermaPotionRecipeB(this, ItemID.BuilderPotion, ModContent.ItemType<PermanentBuilderPotion>());
            PermaPotionRecipeB(this, ItemID.ShinePotion, ModContent.ItemType<PermanentShinePotion>());
            PermaPotionRecipeB(this, ItemID.TrapsightPotion, ModContent.ItemType<PermanentDangersensePotion>());
            PermaPotionRecipeB(this, ItemID.FeatherfallPotion, ModContent.ItemType<PermanentFeatherfallPotion>());
            PermaPotionRecipeB(this, ItemID.FlaskofParty, ModContent.ItemType<PermanentFlaskOfParty>());
            PermaPotionRecipeB(this, ItemID.FlaskofPoison, ModContent.ItemType<PermanentFlaskOfPoison>());
            PermaPotionRecipeB(this, ItemID.FlipperPotion, ModContent.ItemType<PermanentFlipperPotion>());
            PermaPotionRecipeB(this, ItemID.HunterPotion, ModContent.ItemType<PermanentHunterPotion>());
            PermaPotionRecipeB(this, ItemID.InvisibilityPotion, ModContent.ItemType<PermanentInvisibilityPotion>());
            PermaPotionRecipeB(this, ItemID.NightOwlPotion, ModContent.ItemType<PermanentNightOwlPotion>());
            PermaPotionRecipeB(this, ItemID.ThornsPotion, ModContent.ItemType<PermanentThornsPotion>());
            PermaPotionRecipeB(this, ItemID.WarmthPotion, ModContent.ItemType<PermanentWarmthPotion>());
            PermaPotionRecipeB(this, ItemID.WaterWalkingPotion, ModContent.ItemType<PermanentWaterWalkingPotion>());
            #endregion
            #region special perma recipes
            ModRecipe recipe = new ModRecipe(this);
            recipe.AddIngredient(ItemID.GravitationPotion, 20);
            recipe.AddIngredient(GetItem("DarkSoul"), 50000);
            recipe.AddIngredient(ItemID.SoulofFlight, 1);
            recipe.SetResult(ModContent.ItemType<PermanentGravitationPotion>());
            recipe.AddRecipe();

            recipe = new ModRecipe(this);
            recipe.AddIngredient(ItemID.ObsidianSkinPotion, 20);
            recipe.AddIngredient(GetItem("DarkSoul"), 25000);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.SetResult(ModContent.ItemType<PermanentObsidianSkinPotion>());
            recipe.AddRecipe();
            #endregion
        }

    }

    [Label("Config")]
    [BackgroundColor(30, 60, 40, 220)]
    public class tsorcRevampConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Label("Adventure Mode")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Adventure mode prevents breaking and placing most blocks.\nLeave this enabled if you're playing with the custom map!")]
        [DefaultValue(true)]
        public bool AdventureMode { get; set; }
        [Label("Souls Drop on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Drop all your Dark Souls when you die.\nIf \"Delete Dropped Souls on Death\" is enabled, \nyour Souls will drop after old Souls are deleted.\nDefaults to On")]
        [DefaultValue(true)]
        public bool SoulsDropOnDeath { get; set; }

        [Label("Delete Dropped Souls on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Any Dark Souls in the world will be deleted when a player dies.\nEven if this option is disabled, your Souls will be deleted \nif over 400 items are active in the world after you die, \nor if you exit the game while your Souls are still on the ground.\nDefaults to On")]
        [DefaultValue(true)]
        public bool DeleteDroppedSoulsOnDeath { get; set; }

        [Label("Rename Skeletron")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Renames Skeletron to Gravelord Nito.\nOnly turn this off if you are experiencing \ncrashes or other strange behavior when \nyou attempt to summon Skeletron.\nDefaults to On")]
        [DefaultValue(true)]
        public bool RenameSkeletron { get; set; }

        [Label("Legacy Mode")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Legacy mode disables new additions from the Revamp team.\nTurn this on if you want to play the original \nStory of Red Cloud experience as it was in tConfig. \nSome changes and improvements will not be disabled. \nRequires a reload. \nDefaults to Off")]
        [DefaultValue(false)]
        [ReloadRequired]
        //todo items must be manually tagged as legacy. make sure we got them all
        public bool LegacyMode { get; set; }
    }

    public class TilePlaceCode : GlobalItem {

        public static int[] allowed = { //these can always be placed
			//if you can think of a more graceful way to do this, please share
			4,  //torch
			10, //Closed Door
			11, //Open Door  
			13, //bottles
			15, //chairs
			16, //anvil
			17, //furnance
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
			55, //Sign 
			73, //plants
			74, //plants
			78, //clay pot
			79, //bed
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


    };
        public override bool CanUseItem(Item item, Player player) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                if (item.createWall > 0) {
                    return false; //prevent placing walls
                }
                if (item.createTile > -1) {
                    foreach (int id in allowed) {
                        if (item.createTile == id) {
                            return true; //allow placing of tiles in the Allowed array
                        }
                    }
                    return false; //disallow using item if it places other tiles
                }
                return true; //allow using items if they do not create tiles
            }
            return base.CanUseItem(item, player); //use default value
        }
    }

    public class TileKillCode : GlobalTile {
        public List<int> allowed = new List<int>() { //These can always be destroyed
		//if you can think of a more graceful way to do this, please share
			67, //Amethyst 
			12, //Heart crystal
			2, //grass
			3, //small plants
			4, // torch
			5, //tree trunk
			6, //iron
			7, //copper
			9, //silver
			10, //closed door
			11, //open door
			12, //bottles
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
			56, //obsidian
			60, //jungle grass
			61, //small jungle plants
			62, //jungle vines
			69, //thorns
			72, //mushroom stalks
			71, //small mushrooms
			73, //plants
			74, //plants
			78, //clay pot
			79, //bed
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
			105, //statue
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
		};
        public List<int> unbreakable = new List<int>()
        {
            19, //Wood Platform 
			55, //sign
            132, //lever
			130, //active stone block
            131, //inactive stone block
			135, //pressure plates
			136, //switch
			137 //dart trap
		};
        public override bool CanKillTile(int x, int y, int type, ref bool blockDamaged) {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                bool right = !Main.tile[x + 1, y].active();
                bool left = !Main.tile[x - 1, y].active();
                bool below = !Main.tile[x, y - 1].active();
                bool above = !Main.tile[x, y + 1].active();
                if (x < 10 || x > Main.maxTilesX - 10) {//sanity
                    return true;
                }
                else if (y < 10 || y > Main.maxTilesY - 10) {//sanity
                    return true;
                }
                else if (Main.tile[x, y] == null) {//sanity
                    return true;
                }
                else if (allowed.Contains(type)) {//always allow Allowed
                    return true;
                }
                else if (unbreakable.Contains(type)) {//always disallow Unbreakable	
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
            bool right = !Main.tile[x + 1, y].active();
            bool left = !Main.tile[x - 1, y].active();
            bool below = !Main.tile[x, y - 1].active();
            bool above = !Main.tile[x, y + 1].active();
            bool CanDestroy = false;
            {
                if (type == TileID.Ebonsand || type == TileID.Amethyst || type == TileID.ShadowOrbs) { //shadow temple / corruption chasm stuff that gets blown up
                    CanDestroy = true;
                }

                //check cankilltiles stuff
                if ((right && left) || (above && below) || allowed.Contains(type) || (x < 10 || x > Main.maxTilesX - 10) || (y < 10 || y > Main.maxTilesY - 10) || (!Main.tile[x, y].active())) {
                    CanDestroy = true;
                }
                if (Main.tileDungeon[Main.tile[x, y].type]
                    || type == TileID.Silver
                    || type == TileID.Cobalt
                    || type == TileID.Mythril
                    || type == TileID.Adamantite
                    || (unbreakable.Contains(type))
                ) {
                    CanDestroy = false;
                }
                if (!Main.hardMode && type == TileID.Hellstone) {
                    CanDestroy = false;
                }
                return CanDestroy;
            }

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
}