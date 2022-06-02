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
                                "\nDrops 600 souls of utterly corrupt darkness, among other random things." +
                                "\n\"You feel compelled to try this...\"");
        }
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 40;
            Item.consumable = true;
            Item.maxStack = 99;
            Item.value = 1000;
            Item.rare = ItemRarityID.Blue;
            Item.useTime = 45;
            Item.useAnimation = 45;
            new Terraria.Audio.SoundStyle(
            Item.UseSound = new Terraria.Audio.SoundStyle("Sounds/Custom/EvilLaugh");
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player)
        {
            if (!NPC.AnyNPCs(Mod.Find<ModNPC>("AttraidiesIllusion").Type))
            {
                NPC.SpawnOnPlayer(player.whoAmI, Mod.Find<ModNPC>("AttraidiesIllusion").Type);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}