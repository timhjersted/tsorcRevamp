using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class AbyssScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("To close the seal to the Abyss, you must defeat 6 lords of The Abyss:" +
                                "\nArtorias, The Blight, The Wyvern Mage Shadow, Seath the Scaleless, and Chaos." +
                                "\nWith a lord soul from each of these beings," +
                                "\nyou will be able to summon the final lord: Gwyn, Lord of Cinder." +
                                "\nTo craft the summoning item for each lord, you will need to return to eight familiar places" +
                                "\nand collect a unique item dropped from an enemy you will find there: The Western Ocean, the Underground," +
                                "\nthe Corruption, the Jungle, the Dungeon, the Underworld and the Eastern Ocean."); */
        }
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 50000;
            Item.rare = ItemRarityID.Purple;
        }
    }
}