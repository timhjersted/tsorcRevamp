using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    public class tsorcGlobalItem : GlobalItem {
        public static void BossBagSouls(int EnemyID, Player player) {
            NPC npc = new NPC();
            npc.SetDefaults(EnemyID);
            float enemyValue = (int)npc.value / 25;
            float multiplier = 1f;
            int DarkSoulQuantity;

            if (player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                multiplier += 0.25f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                multiplier += 0.15f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain) {
                multiplier += 0.4f;
            }

            DarkSoulQuantity = (int)(multiplier * enemyValue);

            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
        }
        public override void OpenVanillaBag(string context, Player player, int arg) {
            var Slain = tsorcRevampWorld.Slain;
            if (context == "bossBag") {
                if (arg == ItemID.KingSlimeBossBag) {
                    player.QuickSpawnItem(ItemID.GoldCoin, 10);
                    player.QuickSpawnItem(ItemID.Katana);
                    if (Slain.ContainsKey(NPCID.KingSlime)) {
                        if (Slain[NPCID.KingSlime] == 0) {
                            BossBagSouls(NPCID.KingSlime, player);
                            Slain[NPCID.KingSlime] = 1;
                        }
                    }
                }

                if (arg == ItemID.EyeOfCthulhuBossBag) {
                    player.QuickSpawnItem(ItemID.HermesBoots);
                    player.QuickSpawnItem(ItemID.HerosHat);
                    player.QuickSpawnItem(ItemID.HerosPants);
                    player.QuickSpawnItem(ItemID.HerosShirt);
                    if (Slain.ContainsKey(NPCID.EyeofCthulhu)) {
                        if (Slain[NPCID.EyeofCthulhu] == 0) {
                            BossBagSouls(NPCID.EyeofCthulhu, player);
                            Slain[NPCID.EyeofCthulhu] = 1;
                        }
                    }
                }
                if (arg == ItemID.EaterOfWorldsBossBag) {
                    if (Slain.ContainsKey(NPCID.EaterofWorldsHead)) {
                        if (Slain[NPCID.EaterofWorldsHead] == 0) {
                            BossBagSouls(NPCID.EyeofCthulhu, player);
                            Slain[NPCID.EaterofWorldsHead] = 1;
                        }
                    }
                }
                if (arg == ItemID.BrainOfCthulhuBossBag) {
                    if (Slain.ContainsKey(NPCID.BrainofCthulhu)) {
                        if (Slain[NPCID.BrainofCthulhu] == 0) {
                            BossBagSouls(NPCID.BrainofCthulhu, player);
                            Slain[NPCID.BrainofCthulhu] = 1;
                        }
                    }
                }
                if (arg == ItemID.QueenBeeBossBag) {
                    if (Slain.ContainsKey(NPCID.QueenBee)) {
                        if (Slain[NPCID.QueenBee] == 0) {
                            BossBagSouls(NPCID.QueenBee, player);
                            Slain[NPCID.QueenBee] = 1;
                        }
                    }
                }
                if (arg == ItemID.WallOfFleshBossBag) {
                    if (Slain.ContainsKey(NPCID.WallofFlesh)) {
                        if (Slain[NPCID.WallofFlesh] == 0) {
                            BossBagSouls(NPCID.WallofFlesh, player);
                            Slain[NPCID.WallofFlesh] = 1;
                        }
                    }
                }
                if (arg == ItemID.SkeletronBossBag) {
                    if (Slain.ContainsKey(NPCID.SkeletronHead)) {
                        if (Slain[NPCID.SkeletronHead] == 0) {
                            BossBagSouls(NPCID.SkeletronHead, player);
                            Slain[NPCID.SkeletronHead] = 1;
                        }
                    }
                    player.QuickSpawnItem(ModContent.ItemType<Miakoda>());
                }
                if (arg == ItemID.DestroyerBossBag) {
                    if (Slain.ContainsKey(NPCID.TheDestroyer)) {
                        if (Slain[NPCID.TheDestroyer] == 0) {
                            BossBagSouls(NPCID.TheDestroyer, player);
                            Slain[NPCID.TheDestroyer] = 1;
                        }
                    }
                    player.QuickSpawnItem(ModContent.ItemType<RTQ2>());
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfCorruption>(), 2);
                }
                if (arg == ItemID.TwinsBossBag) {
                    if ((Slain.ContainsKey(NPCID.Retinazer)) || (Slain.ContainsKey(NPCID.Spazmatism))) { //idk which it will use
                        if (Slain[NPCID.Retinazer] == 0) {
                            BossBagSouls(NPCID.Retinazer, player);
                            Slain[NPCID.Retinazer] = 1;
                        }
                    }
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSky>(), 2);
                }
                if (arg == ItemID.SkeletronPrimeBossBag) {
                    if (Slain.ContainsKey(NPCID.SkeletronPrime)) {
                        if (Slain[NPCID.SkeletronPrime] == 0) {
                            BossBagSouls(NPCID.SkeletronPrime, player);
                            Slain[NPCID.SkeletronPrime] = 1;
                        }
                    }
                    player.QuickSpawnItem(ItemID.AngelWings);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSteel>(), 2);
                }
                if (arg == ItemID.PlanteraBossBag) {
                    if (Slain.ContainsKey(NPCID.Plantera)) {
                        if (Slain[NPCID.Plantera] == 0) {
                            BossBagSouls(NPCID.Plantera, player);
                            Slain[NPCID.Plantera] = 1;
                        }
                    }
                }
                if (arg == ItemID.GolemBossBag) {
                    if (Slain.ContainsKey(NPCID.Golem)) {
                        if (Slain[NPCID.Golem] == 0) {
                            BossBagSouls(NPCID.Golem, player);
                            Slain[NPCID.Golem] = 1;
                        }
                    }
                }
                if (arg == ItemID.FishronBossBag) {
                    if (Slain.ContainsKey(NPCID.DukeFishron)) {
                        if (Slain[NPCID.DukeFishron] == 0) {
                            BossBagSouls(NPCID.DukeFishron, player);
                            Slain[NPCID.DukeFishron] = 1;
                        }
                    }
                }
                if (arg == ItemID.MoonLordBossBag) {
                    if (Slain.ContainsKey(NPCID.MoonLordCore)) {
                        if (Slain[NPCID.MoonLordCore] == 0) {
                            BossBagSouls(NPCID.MoonLordCore, player);
                            Slain[NPCID.MoonLordCore] = 1;
                        }
                    }
                }
            }
        }
    }
}