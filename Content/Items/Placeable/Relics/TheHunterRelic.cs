using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Content.Items.Placeable.Relics
{
    public class TheHunterRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheHunterRelicTile>();
        public override int Width => 30;
        public override int Height => 50;
    }
}