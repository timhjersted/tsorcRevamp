using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Content.Items.Placeable.Relics
{
    public class TheRageRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheRageRelicTile>();
        public override int Width => 32;
        public override int Height => 50;
    }
}