using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.Items.Pets;

namespace tsorcRevamp.Items.BossBags {
	
	public abstract class BossBag : ModItem {
		
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Treasure Bag");
			Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
			
		}

		public override void SetDefaults() {
			item.maxStack = 999;
			item.consumable = true;
			item.width = 24;
			item.height = 24;
			item.rare = ItemRarityID.Cyan;
			item.expert = true;
		}

		public override bool CanRightClick() {
			return true;
		}
		
    }
	public class TheHunterBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(ModContent.NPCType<TheHunter>())) { //if the boss has been killed
				if (Slain[ModContent.NPCType<TheHunter>()] == 0) { //and the key value is 0
					VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheHunter>(), player); //give the player souls
					Slain[ModContent.NPCType<TheHunter>()] = 1; //set the value to 1
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<Accessories.WaterShoes>());
            player.QuickSpawnItem(ModContent.ItemType<CrestOfEarth>(), 2);
			player.QuickSpawnItem(ItemID.Drax);
		}
    }
	public class TheRageBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheRage>();
		public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(ModContent.NPCType<TheRage>())) {
				if (Slain[ModContent.NPCType<TheRage>()] == 0) {
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheRage>(), player);
					Slain[ModContent.NPCType<TheRage>()] = 1;
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<CrestOfFire>(), 2);
			player.QuickSpawnItem(ItemID.CobaltDrill);
		}
	}
	public class TheSorrowBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheSorrow>();
		public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(ModContent.NPCType<TheSorrow>())) {
				if (Slain[ModContent.NPCType<TheSorrow>()] == 0) {
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheSorrow>(), player);
					Slain[ModContent.NPCType<TheSorrow>()] = 1;
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<CrestOfWater>(), 2);
			player.QuickSpawnItem(ItemID.AdamantiteDrill);
		}
	}

	public class VanillaBossBag : GlobalItem {
        public static void AddBossBagSouls(int EnemyID, Player player) {
            NPC npc = new NPC();
            npc.SetDefaults(EnemyID);
            float enemyValue = (int)npc.value / 25;
            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);

            int DarkSoulQuantity = (int)(multiplier * enemyValue);

            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
        }

        public static void SoulsOnFirstBag(int EnemyID, Player player) {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID)) {
                if (Slain[EnemyID] == 0) {
                    AddBossBagSouls(EnemyID, player);
                    Slain[EnemyID] = 1;
                }
            }
        }

        public override bool PreOpenVanillaBag(string context, Player player, int arg) {
            
            if (context == "bossBag" && arg == ItemID.KingSlimeBossBag) { //re-implement king slime bag to stop blacklisted items from dropping in adventure mode
                player.QuickSpawnItem(ItemID.RoyalGel);
                player.QuickSpawnItem(ItemID.Solidifier);
                player.QuickSpawnItem(ItemID.GoldCoin, 11);
                player.QuickSpawnItem(ItemID.Katana);
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaHood); }
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaShirt); }
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaPants); }
                if (Main.rand.Next(7) == 0) { player.QuickSpawnItem(ItemID.KingSlimeMask); }
                if (Main.rand.Next(10) == 0) { player.QuickSpawnItem(ItemID.KingSlimeTrophy); }
                if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimeGun); }
                if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) { //no hooks or saddles in adventure mode
                    if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimeHook); }
                    if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimySaddle); }
                }
                SoulsOnFirstBag(NPCID.KingSlime, player);
                return false;
            } 
        
            return base.PreOpenVanillaBag(context, player, arg);
        }
        public override void OpenVanillaBag(string context, Player player, int arg) {
            var Slain = tsorcRevampWorld.Slain;
            if (context == "bossBag") {
                if (arg == ItemID.EyeOfCthulhuBossBag) {
                    player.QuickSpawnItem(ItemID.HermesBoots);
                    player.QuickSpawnItem(ItemID.HerosHat);
                    player.QuickSpawnItem(ItemID.HerosPants);
                    player.QuickSpawnItem(ItemID.HerosShirt);
                    SoulsOnFirstBag(NPCID.EyeofCthulhu, player);
                }
                if (arg == ItemID.EaterOfWorldsBossBag) {
                    SoulsOnFirstBag(NPCID.EyeofCthulhu, player);
                }
                if (arg == ItemID.BrainOfCthulhuBossBag) {
                    SoulsOnFirstBag(NPCID.BrainofCthulhu, player);
                }
                if (arg == ItemID.QueenBeeBossBag) {
                    SoulsOnFirstBag(NPCID.QueenBee, player);
                }
                if (arg == ItemID.WallOfFleshBossBag) {
                    SoulsOnFirstBag(NPCID.WallofFlesh, player);
                }
                if (arg == ItemID.SkeletronBossBag) {
                    SoulsOnFirstBag(NPCID.SkeletronHead, player);
                    if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) player.QuickSpawnItem(ModContent.ItemType<Miakoda>());
                    if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) player.QuickSpawnItem(ModContent.ItemType<MiakodaFull>());
                }
                if (arg == ItemID.DestroyerBossBag) {
                    SoulsOnFirstBag(NPCID.TheDestroyer, player);
                    player.QuickSpawnItem(ModContent.ItemType<RTQ2>());
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfCorruption>(), 2);
                }
                if (arg == ItemID.TwinsBossBag) {
                    /* 
                    * picture the following:
                    * Twins are killed. Spazmatism is added to Slain, and the player opens a bag and receives souls
                    * then, Twins are killed again. Retinazer is added to slain this time, and the player opens a bag and gets souls again
                    * to prevent this, we need to make sure we haven't opened a bag from Spazmatism when we open a bag in Retinazer's context
                    */
                    if (Slain.ContainsKey(NPCID.Retinazer)) {
                        if (Slain[NPCID.Retinazer] == 0) {
                            bool SpazmatismDowned = Slain.TryGetValue(NPCID.Spazmatism, out int value);
                            //if SpazmatismDowned evaluates to true, int value is set to the value pair of Spazmatism's key, which stores if a bag has been opened
                            if (!SpazmatismDowned || value == 0) { //if Spazmatism is not in Slain, or no twins bag has been opened in Spazmatism's context
                                AddBossBagSouls(NPCID.Retinazer, player);
                                Slain[NPCID.Retinazer] = 1;
                            }
                        }
                    }
                    else if (Slain.ContainsKey(NPCID.Spazmatism)) { //dont need to check if Retinazer is downed, since this is only run if Retinazer is not in Slain
                        if (Slain[NPCID.Spazmatism] == 0) {
                            AddBossBagSouls(NPCID.Spazmatism, player);
                            Slain[NPCID.Spazmatism] = 1;
                        }
                    }
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSky>(), 2);
                }
                if (arg == ItemID.SkeletronPrimeBossBag) {
                    SoulsOnFirstBag(NPCID.SkeletronPrime, player);
                    player.QuickSpawnItem(ItemID.AngelWings);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSteel>(), 2);
                }
                if (arg == ItemID.PlanteraBossBag) {
                    SoulsOnFirstBag(NPCID.Plantera, player);
                }
                if (arg == ItemID.GolemBossBag) {
                    SoulsOnFirstBag(NPCID.Golem, player);
                    player.QuickSpawnItem(ItemID.Picksaw);
                }
                if (arg == ItemID.FishronBossBag) {
                    SoulsOnFirstBag(NPCID.DukeFishron, player);
                }
                if (arg == ItemID.MoonLordBossBag) {
                    if (Slain.ContainsKey(NPCID.MoonLordCore)) {
                        if (Slain[NPCID.MoonLordCore] == 0) {
                            int enemyValue = 40000; // 1 platinum / 25
                            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);

                            int DarkSoulQuantity = (int)(multiplier * enemyValue);

                            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                            Slain[NPCID.MoonLordCore] = 1;
                        }
                    }
                }
            }
        }
    }
}
