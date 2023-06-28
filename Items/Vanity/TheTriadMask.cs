using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Vanity
{
    [AutoloadEquip(EquipType.Head)]
    public class TheTriadMask : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
    }
}