using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class GoldenHairpin : ModItem
    {
        public static int ammoType = 0;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases either arrow, bullet or rocket damage by 17% multiplicatively" +
                "\nAmmo type is decided by a timer that picks one of the three regularly" +
                "\nSwitches every 7 seconds");

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Expert;
            Item.expert = true;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void UpdateEquip(Player player)
        {
            if (Main.GameUpdateCount % 420 == 0)
            {
                ammoType++;
            }
            if (ammoType == 0)
            {
                player.arrowDamage *= 1.17f;
            }
            if (ammoType == 1)
            {
                player.bulletDamage *= 1.17f;
            }
            if (ammoType == 2)
            {
                player.rocketDamage *= 1.17f;
            }
            if (ammoType >= 3)
            {
                ammoType = 0;
            }
        }
    }
}
