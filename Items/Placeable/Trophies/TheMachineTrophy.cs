using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Trophies;

namespace tsorcRevamp.Items.Placeable.Trophies
{
    public class TheMachineTrophy : TrophyItem
    {
        public override int TileID => ModContent.TileType<TheMachineTrophyTile>();
        public override int Rarity => ItemRarityID.LightPurple;
        public override int Value => PriceByRarity.fromItem(Item);
    }
}