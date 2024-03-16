using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Items.Placeable.Relics
{
    public class TheHunterRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheHunterRelicTile>();
        public override int Width => 30;
        public override int Height => 50;
    }
}