using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Vanity
{
    [AutoloadEquip(EquipType.Head)]
    public class TheHunterMask : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
    }
}