using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class FlameJetItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Flame Jet");
            // Tooltip.SetDefault("Creates a jet of flame that players can only pass by dodging");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ArmorStatue);
            Item.createTile = ModContent.TileType<FlameJet>();
            Item.placeStyle = 0;
        }
    }
}