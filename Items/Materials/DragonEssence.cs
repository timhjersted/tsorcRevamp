using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class DragonEssence : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            ItemID.Sets.ItemIconPulse[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 30;
            Item.rare = ItemRarityID.Cyan;
            Item.value = 1000;
            Item.maxStack = Item.CommonMaxStack;
        }

         public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.10f, 0.33f, 0.75f);
        }
    }
}
