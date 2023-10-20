using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class SoulSkellyItemR : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul Skelly Right");
            /* Tooltip.SetDefault("Right-Click once placed to acquire a Soul of a Nameless Soldier (800 souls)" +
            "\nGives Soul of a Proud Knight (2000 souls) outside of Pre-HM" +
            "\nUsed by mapmakers for placing around the map as loot"); */
        }

        public override void SetDefaults()
        {
            //item.CloneDefaults(ItemID.Furnace);
            Item.createTile = ModContent.TileType<SoulSkellyR>();
            Item.placeStyle = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.width = 42;
            Item.height = 18;
            Item.value = 0;
        }
    }
}