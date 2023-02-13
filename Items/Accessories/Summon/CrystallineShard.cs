using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    [LegacyName("Oxyale")]
    public class CrystallineShard : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases your max minions by 2" +
                               "\nIncreases your minion damage by 1.5% multiplicatively for each minion slot you have" +
                               "\nDecreases whip range by 33%" +
                               "\n\"A magical substance found only in the Crystalline Dimension\"");

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 2;
            float crystalline = (float)(0.015 * player.maxMinions);
            player.GetDamage(DamageClass.Summon) *= 1 + crystalline;
            player.whipRangeMultiplier -= 0.33f;
/*            if (player.wet)
            {
                player.gills = true;
                player.ignoreWater = true;
            }*/
        }
    }
}
