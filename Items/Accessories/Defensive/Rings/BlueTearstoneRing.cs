using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class BlueTearstoneRing : ModItem
    {
        public static float LifeThreshold = 40f;
        public static int Defense = 25;
        public static float DrIncrease = 9f;
        public static int BadMeleeDmg = 200;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold, Defense, DrIncrease, BadMeleeDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 15;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ModContent.RarityType<DarkBlue>();
        }


        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.statDefense += Defense;
                player.endurance += DrIncrease / 100f;
                player.GetDamage(DamageClass.Melee) -= BadMeleeDmg / 100;
            }
        }

    }
}
