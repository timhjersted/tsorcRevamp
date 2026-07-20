using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class BloodredMossClump : ModItem
    {
        public static int Healing = 30;
        public static int Sickness = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Healing, Sickness);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 25;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
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

            // Tier-aware heal: Classic full, Unkindled half, BotC zero.
            // (Bleed/poison cure above always runs regardless of tier.)
            if (!player.HasBuff(BuffID.PotionSickness))
            {
                int heal = player.GetModPlayer<tsorcRevampPlayer>().ApplyHealing(Healing);
                if (heal > 0)
                {
                    player.statLife += heal;
                    if (player.statLife > player.statLifeMax2)
                    {
                        player.statLife = player.statLifeMax2;
                    }
                    player.HealEffect(heal, true);
                    player.AddBuff(BuffID.PotionSickness, player.pStone ? Sickness * 60 / 4 * 3 : Sickness * 60);
                }
            }
            return true;
        }
    }
}
