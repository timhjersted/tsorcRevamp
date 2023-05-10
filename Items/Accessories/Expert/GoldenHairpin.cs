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

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
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
                player.arrowDamage *= 1.2f;
            }
            if (ammoType == 1)
            {
                player.bulletDamage *= 1.2f;
            }
            if (ammoType == 2)
            {
                player.specialistDamage *= 1.2f;
            }
            if (ammoType >= 3)
            {
                ammoType = 0;
            }
        }
    }
}
