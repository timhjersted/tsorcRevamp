using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Items.Placeable.Relics
{
    public class TheSorrowRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheSorrowRelicTile>();
        public override int Width => 30;
        public override int Height => 50;
    }
}