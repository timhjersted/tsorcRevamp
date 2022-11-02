using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class GreenBlossom : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases stamina recovery rate by 30%");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.GreenBlossom>();
            Item.buffTime = 14400;
        }
    }
}
