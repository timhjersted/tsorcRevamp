using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class SoulSkellyWall2Item : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul Skelly Wall 2");
            /* Tooltip.SetDefault("Right-Click once placed to acquire a Soul of a Nameless Soldier (800 souls)" +
            "\nGives Soul of a Proud Knight (2000 souls) outside of Pre-HM" +
            "\nUsed by mapmakers for placing around the map as loot"); */
        }

        public override void SetDefaults()
        {
            //item.CloneDefaults(ItemID.Furnace);
            Item.createTile = ModContent.TileType<SoulSkellyWall2>();
            Item.placeStyle = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.width = 30;
            Item.height = 48;
            Item.value = 0;
        }
    }
}