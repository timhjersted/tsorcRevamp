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

            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                multiplier += 0.25f;
            }
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                multiplier += 0.15f;
            }
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SOADrain) {
                multiplier += 0.4f;
            }

            DarkSoulQuantity = (int)(multiplier * enemyValue);

            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
        }
        public override void OpenVanillaBag(string context, Player player, int arg) {

            if (context == "bossBag") {
                if (arg == ItemID.KingSlimeBossBag) {
                    player.QuickSpawnItem(ItemID.GoldCoin, 10);
                    player.QuickSpawnItem(ItemID.Katana);
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.KingSlime)) {
                        BossBagSouls(NPCID.KingSlime, player);
                    }
                }

                if (arg == ItemID.EyeOfCthulhuBossBag) {
                    player.QuickSpawnItem(ItemID.HermesBoots);
                    player.QuickSpawnItem(ItemID.HerosHat);
                    player.QuickSpawnItem(ItemID.HerosPants);
                    player.QuickSpawnItem(ItemID.HerosShirt);
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.EyeofCthulhu)) {
                        BossBagSouls(NPCID.EyeofCthulhu, player);
                    }
                }
                if (arg == ItemID.EaterOfWorldsBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.EaterofWorldsHead)) {//not a mistake. eow segments drop 8 silver (comparable to a voodoo demon) so just copy the eoc soul drop amount
                        BossBagSouls(NPCID.EaterofWorldsHead, player);
                    }
                }
                if (arg == ItemID.BrainOfCthulhuBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.BrainofCthulhu)) {
                        BossBagSouls(NPCID.BrainofCthulhu, player);
                    }
                }
                if (arg == ItemID.QueenBeeBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.QueenBee)) {
                        BossBagSouls(NPCID.QueenBee, player);
                    }
                }
                if (arg == ItemID.WallOfFleshBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.WallofFlesh)) {
                        BossBagSouls(NPCID.WallofFlesh, player);
                    }
                }
                if (arg == ItemID.SkeletronBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead)) {
                        BossBagSouls(NPCID.SkeletronHead, player);
                    }
                    player.QuickSpawnItem(ModContent.ItemType<Miakoda>());
                }
                if (arg == ItemID.DestroyerBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.TheDestroyer)) {
                        BossBagSouls(NPCID.TheDestroyer, player);
                    }
                    player.QuickSpawnItem(ModContent.ItemType<RTQ2>());
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfCorruption>(), 2);
                }
                if (arg == ItemID.TwinsBossBag) {
                    if ((!tsorcRevampWorld.Slain.ContainsKey(NPCID.Retinazer)) || (!tsorcRevampWorld.Slain.ContainsKey(NPCID.Spazmatism))) { //idk which it will use
                        BossBagSouls(NPCID.Retinazer, player);
                    }
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSky>(), 2);
                }
                if (arg == ItemID.SkeletronPrimeBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronPrime)) {
                        BossBagSouls(NPCID.SkeletronPrime, player);
                    }
                    player.QuickSpawnItem(ItemID.AngelWings);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSteel>(), 2);
                }
                if (arg == ItemID.PlanteraBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.Plantera)) {
                        BossBagSouls(NPCID.Plantera, player);
                    }
                }
                if (arg == ItemID.GolemBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.Golem)) {
                        BossBagSouls(NPCID.Golem, player);
                    }
                }
                if (arg == ItemID.FishronBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.DukeFishron)) {
                        BossBagSouls(NPCID.DukeFishron, player);
                    }
                }
                if (arg == ItemID.MoonLordBossBag) {
                    if (!tsorcRevampWorld.Slain.ContainsKey(NPCID.MoonLordCore)) {
                        BossBagSouls(NPCID.MoonLordCore, player);
                    }
                }
            }
        }
    }
}