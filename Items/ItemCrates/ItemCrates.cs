using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.ItemCrates
{

    public abstract class ItemCrates : ModItem //Sold by merchants, holding 100 of an item. These have to exist because 1 Soul Shekel = 5 souls;
    {                                          //Merchants can't sell multiple of an item at a time and we cant sell a single arrow for 1 soul shekel, as it is far too expensive.			

        public override void SetDefaults()
        {
            Item.maxStack = 999;
            Item.consumable = true;
            Item.width = 19;
            Item.height = 19;
            Item.rare = ItemRarityID.White;
        }

        public override bool CanRightClick()
        {
            return true;
        }

    }

    public class WoodenArrowCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Wooden Arrows" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ItemID.WoodenArrow, 100);
        }
    }

    public class BoltCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Crossbow Bolts" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ModContent.ItemType<Items.Ammo.Bolt>(), 100);
        }
    }

    public class ThrowingAxeCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Throwing Axes" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Melee.ThrowingAxe>(), 100);
        }
    }

    public class ThrowingSpearCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Throwing Spears" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>(), 100);
        }
    }

    public class GelCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Gel" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ItemID.Gel, 100);
        }
    }

    public class ThrowingKnifeCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Throwing Knives" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ItemID.ThrowingKnife, 100);
        }
    }

    public class RoyalThrowingSpearCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Royal Throwing Spears" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>(), 100);
        }
    }

    public class FrostburnArrowCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Frostburn Arrows" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(ItemID.FrostburnArrow, 100);
        }
    }

}