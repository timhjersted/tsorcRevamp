using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Items.Placeable.Trophies
{
    public class AncestralSpiritTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<AncestralSpiritTrophyTile>();
        public override int Rarity => ItemRarityID.Orange;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}