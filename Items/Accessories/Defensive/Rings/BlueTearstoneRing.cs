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
        public static float DR = 9f;
        public static int BadMeleeDmg = 200;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold, Defense, DR, BadMeleeDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 15;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }


        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.statDefense += Defense;
                player.endurance = DR / 100f;
                player.GetDamage(DamageClass.Melee) -= BadMeleeDmg / 100;
            }
        }

    }
}
