using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Materials.Souls
{
    public class GreatSerpentSoul : Soul
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 22;
            Item.height = 28;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.52f, 0.22f, 0.85f);
        }
    }
}
