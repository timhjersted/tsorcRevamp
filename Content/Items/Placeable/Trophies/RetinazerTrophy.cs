using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Content.Items.Placeable.Trophies
{
    public class RetinazerTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<RetinazerTrophyTile>();
        public override int Rarity => ItemRarityID.Lime;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}