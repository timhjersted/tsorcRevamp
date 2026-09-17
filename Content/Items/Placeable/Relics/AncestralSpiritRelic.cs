using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Content.Items.Placeable.Relics
{
    public class AncestralSpiritRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<AncestralSpiritRelicTile>();
        public override int Width => 42;
        public override int Height => 50;
    }
}