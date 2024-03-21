using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class SoulSkellyWall1Item : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul Skelly Wall 1");
            /* Tooltip.SetDefault("Right-Click once placed to acquire a Soul of a Lost Undead (200 souls)" +
            "\nGives Soul of a Nameless Soldier (800 souls) outside of Pre-HM" +
            "\nUsed by mapmakers for placing around the map as loot"); */
        }

        public override void SetDefaults()
        {
            //item.CloneDefaults(ItemID.Furnace);
            Item.createTile = ModContent.TileType<SoulSkellyWall1>();
            Item.placeStyle = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.width = 38;
            Item.height = 44;
            Item.value = 0;
        }
    }
}