using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Content.Items.Placeable
{
    public class SoapstonePlaceable : ModItem
    {

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(BonfirePlaceable));

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("You probably shouldn't have this.");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.Bookcase);
            Item.createTile = ModContent.TileType<SoapstoneTile>();
            Item.placeStyle = 0;
        }
    }
}