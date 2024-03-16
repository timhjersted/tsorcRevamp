using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Items.Placeable.Trophies
{
    public class SpazmatismTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<SpazmatismTrophyTile>();
        public override int Rarity => ItemRarityID.Lime;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}