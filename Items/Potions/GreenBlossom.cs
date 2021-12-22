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
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 20;
            item.useTime = 20;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 99;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 5000;
            item.buffType = ModContent.BuffType<Buffs.GreenBlossom>();
            item.buffTime = 14400;
        }
    }
}
