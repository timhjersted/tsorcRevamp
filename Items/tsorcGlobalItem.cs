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
                    BossBagSouls(NPCID.KingSlime, player);
                }

                if (arg == ItemID.EyeOfCthulhuBossBag) {
                    player.QuickSpawnItem(ItemID.HermesBoots);
                    player.QuickSpawnItem(ItemID.HerosHat);
                    player.QuickSpawnItem(ItemID.HerosPants);
                    player.QuickSpawnItem(ItemID.HerosShirt);
                    BossBagSouls(NPCID.EyeofCthulhu, player);
                }
                if (arg == ItemID.EaterOfWorldsBossBag) {
                    BossBagSouls(NPCID.EyeofCthulhu, player); //not a mistake. eow segments drop 8 silver (comparable to a voodoo demon) so just copy the eoc soul drop amount
                }
                if (arg == ItemID.BrainOfCthulhuBossBag) {
                    BossBagSouls(NPCID.BrainofCthulhu, player);
                }
                if (arg == ItemID.QueenBeeBossBag) {
                    BossBagSouls(NPCID.QueenBee, player);
                }
                if (arg == ItemID.WallOfFleshBossBag) {
                    BossBagSouls(NPCID.WallofFlesh, player);
                }
                if (arg == ItemID.SkeletronBossBag) {
                    BossBagSouls(NPCID.SkeletronHead, player);
                }
                if (arg == ItemID.DestroyerBossBag) {
                    BossBagSouls(NPCID.TheDestroyer, player);
                    player.QuickSpawnItem(ModContent.ItemType<RTQ2>());
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfCorruption>(), 2);
                }
                if (arg == ItemID.TwinsBossBag) {
                    BossBagSouls(NPCID.Retinazer, player);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSky>(), 2);
                }
                if (arg == ItemID.SkeletronPrimeBossBag) {
                    BossBagSouls(NPCID.SkeletronPrime, player);
                    player.QuickSpawnItem(ItemID.AngelWings);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSteel>(), 2);
                }
                if (arg == ItemID.PlanteraBossBag) {
                    BossBagSouls(NPCID.Plantera, player);
                }
                if (arg == ItemID.GolemBossBag) {
                    BossBagSouls(NPCID.Golem, player);
                }
                if (arg == ItemID.FishronBossBag) {
                    BossBagSouls(NPCID.DukeFishron, player);
                }
                if (arg == ItemID.MoonLordBossBag) {
                    BossBagSouls(NPCID.MoonLordCore, player);
                }
            }
        }
    }
}