using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class DeadChicken : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = 12;
            Item.width = 24;
            Item.maxStack = 30;
            Item.value = 2;
        }
    }
}
