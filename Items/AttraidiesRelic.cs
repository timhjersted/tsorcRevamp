using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items
{
    class AttraidiesRelic : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons a mindflayer illusion from the legion army of Attraidies!" +
                                "\nDrops 500 souls of utterly corrupt darkness." +
                                "\n\"You feel compelled to try this...\"");
        }
        public override void SetDefaults()
        {
            item.width = 34;
            item.height = 40;
            item.consumable = true;
            item.maxStack = 99;
            item.value = 1000;
            item.rare = ItemRarityID.Blue;
            item.useTime = 45;
            item.useAnimation = 45;
            item.scale = .7f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player)
        {
            Main.NewText("Attraidies Illusion has awakened!", 175, 75, 255);
            NPC.SpawnOnPlayer(Main.myPlayer, NPCID.CorruptBunny); //placeholder
            return true;
        }
    }
}