using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class BloodredMossClump : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("[c/ffbf00:Heals 30 HP, with only 5 seconds of potion sickness.]" +
                                "\n[c/ffbf00:Removes bleeding and poisoned.]" +
                                "\nA supply of these may be essential for exploring some areas." +                            
                                "\nCan still be used to remove poison and bleeding while under the" +
                                "\neffect of potion sickness. However, it will not heal any HP."); */
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 25;
            Item.consumable = true;
            Item.maxStack = 360;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 35;
            Item.useTime = 35;
            Item.UseSound = SoundID.Item21;
            Item.value = 2000;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool? UseItem(Player player)
        {
            int buffIndex = 0;

            foreach (int buffType in player.buffType)
            {

                if ((buffType == BuffID.Bleeding) || (buffType == BuffID.Poisoned))
                {
                    player.buffTime[buffIndex] = 0;
                }
                buffIndex++;
            }

            if (!player.HasBuff(BuffID.PotionSickness) && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.statLife += 30;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(30, true);
                player.AddBuff(BuffID.PotionSickness, 300);
            }
            return true;
        }
    }
}
