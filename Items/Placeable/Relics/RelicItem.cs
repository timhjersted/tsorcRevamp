using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles.Relics;

namespace tsorcRevamp.Items.Placeable.Relics
{
    public abstract class RelicItem : ModItem
    {
        public abstract int RelicTileType { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public override void SetDefaults()
        {
            // Vanilla has many useful methods like these, use them! This substitutes setting Item.createTile and Item.placeStyle aswell as setting a few values that are common across all placeable items
            // The place style (here by default 0) is important if you decide to have more than one relic share the same tile type (more on that in the tiles' code)
            Item.DefaultToPlaceableTile(RelicTileType, 0);

            Item.width = Width;
            Item.height = Height;
            Item.rare = ItemRarityID.Master;
            Item.master = true; // This makes sure that "Master" displays in the tooltip, as the rarity only changes the item name color
            Item.value = Item.buyPrice(0, 5);
        }
    }
}