using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Items.Placeable.Trophies
{
    public class TheSorrowTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<TheSorrowTrophyTile>();
        public override int Rarity => ItemRarityID.LightRed;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}