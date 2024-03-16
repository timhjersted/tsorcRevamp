using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Items.Placeable.Relics
{
    public class TheMachineRelic : RelicItem
    {
        public override int RelicTileType => ModContent.TileType<TheMachineRelicTile>();
        public override int Width => 30;
        public override int Height => 50;
    }
}