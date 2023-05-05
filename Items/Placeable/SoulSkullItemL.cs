using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class SoulSkullItemL : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ArmorStatue);
            Item.createTile = ModContent.TileType<SoulSkullL>();
            Item.placeStyle = 0;
        }
    }
}