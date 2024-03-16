using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Items.Placeable.Relics
{
    public class TheTriadRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheTriadRelicTile>();
        public override int Width => 36;
        public override int Height => 50;
    }
}