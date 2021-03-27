using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class AbyssScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("To close the seal to the Abyss and ignite the Kiln of the First Flame," +
                                "\nyou must defeat the 6 lords of The Abyss: Artorias, Blight, The Wyvern Mage Shadow," +
                                "\nChaos, and Seath the Scaleless. With a lord soul from each of these beings," +
                                "\nyou will be able to summon the final guardian - Gwyn, Lord of Cinder." +
                                "\nTo craft the summoning item for each guardian, you will need to return to eight familiar places" +
                                "\nand collect a unique item dropped from an enemy you will find there: The Western Ocean, the Underground," +
                                "\nthe Corruption, the Jungle, the dungeon, the underworld and the Eastern Ocean.");
        }
        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 32;
            item.consumable = false;
            item.maxStack = 1;
            item.value = 50000;
            item.rare = ItemRarityID.Purple;
        }
    }
}