using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items 
{
    class DisabledRecipe : ModItem 
    {
        public override void SetStaticDefaults() 
        {
            // Tooltip.SetDefault("This recipe has been disabled!\nYou will have to find this item in the world!");
        }

        public override void SetDefaults() 
        {
            Item.rare = ItemRarityID.Red;
        }
    }
}
