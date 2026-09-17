using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Content.Items.Placeable.Trophies
{
    public class TheHunterTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<TheHunterTrophyTile>();
        public override int Rarity => ItemRarityID.LightRed;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}