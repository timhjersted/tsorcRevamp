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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.WoodenArrow, 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Items.Ammo.Bolt>(), 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Items.Weapons.Melee.ThrowingAxe>(), 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Items.Weapons.Ranged.Thrown.ThrowingSpear>(), 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.Gel, 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.ThrowingKnife, 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Items.Weapons.Ranged.Thrown.RoyalThrowingSpear>(), 100);
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
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.FrostburnArrow, 100);
        }
    }

    [LegacyName("GigaDrill")]
    [LegacyName("SweatyCyclopsForge")]
    [LegacyName("ReforgedOldCrossbow")]
    [LegacyName("ReforgedOldLongbow")]
    [LegacyName("AdamantiteFlail")]
    [LegacyName("CopperSpear")]
    [LegacyName("EnchangedMorningStar")]
    [LegacyName("ForgottenAxe")]
    [LegacyName("ForgottenGreatAxe")]
    [LegacyName("ForgottenKotetsu")]
    [LegacyName("ForgottenLongSword")]
    [LegacyName("ForgottenMetalKnuckles")]
    [LegacyName("GoldSpear")]
    [LegacyName("IronSpear")]
    [LegacyName("MythrilFlail")]
    [LegacyName("OldAxe")]
    [LegacyName("OldBroadsword")]
    [LegacyName("OldDoubleAxe")]
    [LegacyName("OldLongsword")]
    [LegacyName("OldMace")]
    [LegacyName("OldMorningStar")]
    [LegacyName("OldPoisonDagger")]
    [LegacyName("OldRapier")]
    [LegacyName("OldSabre")]
    [LegacyName("OldTwoHandedSword")]
    [LegacyName("OrcishHalberd")]
    [LegacyName("ReforgedOldAxe")]
    [LegacyName("ReforgedOldBroadsword")]
    [LegacyName("ReforgedOldDoubleAxe")]
    [LegacyName("ReforgedOldHalberd")]
    [LegacyName("ReforgedOldLongsword")]
    [LegacyName("ReforgedOldMace")]
    [LegacyName("ReforgedOldMorningStar")]
    [LegacyName("ReforgedOldPoisonDagger")]
    [LegacyName("ReforgedOldRapier")]
    [LegacyName("ReforgedOldSabre")]
    [LegacyName("ReforgedOldTwoHandedSword")]
    [LegacyName("SilverFlail")]
    [LegacyName("SilverSpear")]
    [LegacyName("SoulShekelCrate")]
    [LegacyName("AttractionPotion")]
    [LegacyName("SpikedBuckler")]
    [LegacyName("SpikedNecklace")]
    [LegacyName("CopperRing")]
    public class SoulCoinCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 1000 soul coins" +
                                "\nGiven as weregild for an item that no longer exists" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<Items.SoulCoin>(), 1000);
        }
    }

    
    public class MeteorShotCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Meteor Bullets" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.MeteorShot, 100);
        }
    }

    public class UnholyArrowCrate : ItemCrates
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crate containing 100 Unholy Arrows" +
                                "\n{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(player.GetSource_ItemUse(Item), ItemID.UnholyArrow, 100);
        }
    }
}