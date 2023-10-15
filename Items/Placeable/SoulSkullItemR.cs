using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class SoulSkullItemR : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul Skull Right");
            /* Tooltip.SetDefault("Right-Click once placed to acquire a Soul of a Lost Undead (200 souls)" +
            "\nGives Soul of a Nameless Soldier (800 souls) outside of Pre-HM" +
            "\nUsed by mapmakers for placing around the map as loot"); */
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ArmorStatue);
            Item.createTile = ModContent.TileType<SoulSkullR>();
            Item.placeStyle = 0;
        }
    }
}