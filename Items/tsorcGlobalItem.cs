using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public class tsorcGlobalItem : GlobalItem
    {
        public override void OpenVanillaBag(string context, Player player, int arg)
        {  
            
            if (context == "bossBag" && arg == ItemID.KingSlimeBossBag)
            {
                player.QuickSpawnItem(ItemID.GoldCoin, 10);
                player.QuickSpawnItem(ItemID.Katana);
            }

            if (context == "bossBag" && arg == ItemID.EyeOfCthulhuBossBag)
            {
                player.QuickSpawnItem(ItemID.HermesBoots, 2);
                player.QuickSpawnItem(ItemID.HerosHat, 2);
                player.QuickSpawnItem(ItemID.HerosPants, 2);
                player.QuickSpawnItem(ItemID.HerosShirt, 2);
            }

            /*if (context == "bossBag" && arg == ItemID.EaterOfWorldsBossBag)
            {
                player.QuickSpawnItem(mod.ItemType("DarkSoul"), 00);
            }*/

        }
    }
}