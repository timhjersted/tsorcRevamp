using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Buffs;

namespace tsorcRevamp {
    public class tsorcRevamp : Mod {
        public static ModHotKey toggleDragoonBoots;
        public override void Load() {
            toggleDragoonBoots = RegisterHotKey("Dragoon Boots", "Z");

            On.Terraria.NPC.SpawnSkeletron += SkeletronPatch;
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

        public static Dictionary<int, int> PermanentBuffs = new Dictionary<int, int>();
        public override void PostSetupContent() {
            PermanentBuffs.Add(ModContent.ItemType<PermanentObsidianSkinPotion>(), BuffID.ObsidianSkin); //0
            PermanentBuffs.Add(ModContent.ItemType<PermanentRegenerationPotion>(), BuffID.Regeneration); //1
            PermanentBuffs.Add(ModContent.ItemType<PermanentSwiftnessPotion>(), BuffID.Swiftness); //2
            PermanentBuffs.Add(ModContent.ItemType<PermanentGillsPotion>(), BuffID.Gills); //3
            PermanentBuffs.Add(ModContent.ItemType<PermanentIronskinPotion>(), BuffID.Ironskin); //4
            PermanentBuffs.Add(ModContent.ItemType<PermanentManaRegenerationPotion>(), BuffID.ManaRegeneration); //5
            PermanentBuffs.Add(ModContent.ItemType<PermanentMagicPowerPotion>(), BuffID.MagicPower); //6
            PermanentBuffs.Add(ModContent.ItemType<PermanentFeatherfallPotion>(), BuffID.Featherfall); //7
            PermanentBuffs.Add(ModContent.ItemType<PermanentSpelunkerPotion>(), BuffID.Spelunker); //8
            PermanentBuffs.Add(ModContent.ItemType<PermanentInvisibilityPotion>(), BuffID.Invisibility); //9
            PermanentBuffs.Add(ModContent.ItemType<PermanentShinePotion>(), BuffID.Shine); //10
            PermanentBuffs.Add(ModContent.ItemType<PermanentNightOwlPotion>(), BuffID.NightOwl); //11
            PermanentBuffs.Add(ModContent.ItemType<PermanentBattlePotion>(), BuffID.Battle); //12
            PermanentBuffs.Add(ModContent.ItemType<PermanentThornsPotion>(), BuffID.Thorns); //13
            PermanentBuffs.Add(ModContent.ItemType<PermanentWaterWalkingPotion>(), BuffID.WaterWalking); //14
            PermanentBuffs.Add(ModContent.ItemType<PermanentArcheryPotion>(), BuffID.Archery); //15
            PermanentBuffs.Add(ModContent.ItemType<PermanentHunterPotion>(), BuffID.Hunter); //16
            PermanentBuffs.Add(ModContent.ItemType<PermanentGravitationPotion>(), BuffID.Gravitation); //17
            PermanentBuffs.Add(ModContent.ItemType<PermanentAle>(), BuffID.Tipsy); //18
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfVenom>(), BuffID.WeaponImbueVenom); //19
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfCursedFlames>(), BuffID.WeaponImbueCursedFlames); //20
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfFire>(), BuffID.WeaponImbueFire); //21
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfGold>(), BuffID.WeaponImbueGold); //22
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfIchor>(), BuffID.WeaponImbueIchor); //23
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfNanites>(), BuffID.WeaponImbueNanites); //24
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfParty>(), BuffID.WeaponImbueConfetti); //25
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlaskOfPoison>(), BuffID.WeaponImbuePoison); //26
            PermanentBuffs.Add(ModContent.ItemType<PermanentMiningPotion>(), BuffID.Mining); //27
            PermanentBuffs.Add(ModContent.ItemType<PermanentHeartreachPotion>(), BuffID.Heartreach); //28
            PermanentBuffs.Add(ModContent.ItemType<PermanentCalmingPotion>(), BuffID.Calm); //29
            PermanentBuffs.Add(ModContent.ItemType<PermanentBuilderPotion>(), BuffID.Builder); //30
            PermanentBuffs.Add(ModContent.ItemType<PermanentTitanPotion>(), BuffID.Titan); //31
            PermanentBuffs.Add(ModContent.ItemType<PermanentFlipperPotion>(), BuffID.Flipper); //32
            PermanentBuffs.Add(ModContent.ItemType<PermanentSummoningPotion>(), BuffID.Summoning); //33
            PermanentBuffs.Add(ModContent.ItemType<PermanentDangersensePotion>(), BuffID.Dangersense); //34. also why is the item called trapsight and the buff is called dangersense. damn you Red
            PermanentBuffs.Add(ModContent.ItemType<PermanentAmmoReservationPotion>(), BuffID.AmmoReservation); //35
            PermanentBuffs.Add(ModContent.ItemType<PermanentLifeforcePotion>(), BuffID.Lifeforce); //36
            PermanentBuffs.Add(ModContent.ItemType<PermanentEndurancePotion>(), BuffID.Endurance); //37
            PermanentBuffs.Add(ModContent.ItemType<PermanentRagePotion>(), BuffID.Rage); //38
            PermanentBuffs.Add(ModContent.ItemType<PermanentInfernoPotion>(), BuffID.Inferno); //39
            PermanentBuffs.Add(ModContent.ItemType<PermanentWrathPotion>(), BuffID.Wrath); //40
            PermanentBuffs.Add(ModContent.ItemType<PermanentFishingPotion>(), BuffID.Fishing); //41
            PermanentBuffs.Add(ModContent.ItemType<PermanentSonarPotion>(), BuffID.Sonar); //42
            PermanentBuffs.Add(ModContent.ItemType<PermanentCratePotion>(), BuffID.Crate); //43
            PermanentBuffs.Add(ModContent.ItemType<PermanentWarmthPotion>(), BuffID.Warmth); //44
            PermanentBuffs.Add(ModContent.ItemType<PermanentArmorDrugPotion>(), ModContent.BuffType<ArmorDrug>()); //45
            PermanentBuffs.Add(ModContent.ItemType<PermanentBattlefrontPotion>(), ModContent.BuffType<Battlefront>()); //46
            PermanentBuffs.Add(ModContent.ItemType<PermanentBoostPotion>(), ModContent.BuffType<Boost>()); //47
            PermanentBuffs.Add(ModContent.ItemType<PermanentCrimsonPotion>(), ModContent.BuffType<CrimsonDrain>()); //48
            PermanentBuffs.Add(ModContent.ItemType<PermanentDemonDrugPotion>(), ModContent.BuffType<DemonDrug>()); //49
            PermanentBuffs.Add(ModContent.ItemType<PermanentShockwavePotion>(), ModContent.BuffType<Shockwave>()); //50
            PermanentBuffs.Add(ModContent.ItemType<PermanentStrengthPotion>(), ModContent.BuffType<Strength>()); //51
        }



        public override void Unload() {
            toggleDragoonBoots = null;
            PermanentBuffs = null;
        }

        public override void AddRecipes() {
            RecipeHelper.EditRecipes();
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
			51, //cobweb
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
                if (type == TileID.Ebonsand || type == TileID.Amethyst || type == TileID.ShadowOrbs) { //shadow temple / corruption chasm stuff that gets blown up
                    CanDestroy = true;
                }

                //check cankilltiles stuff
                if ((right && left) || (above && below) || allowed.Contains(type) || (x < 10 || x > Main.maxTilesX - 10) || (y < 10 || y > Main.maxTilesY - 10) || (!Main.tile[x, y].active())) {
                    CanDestroy = true;
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
    }
}