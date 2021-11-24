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
            item.width = 34;
            item.height = 40;
            item.consumable = true;
            item.maxStack = 99;
            item.value = 1000;
            item.rare = ItemRarityID.Blue;
            item.useTime = 45;
            item.useAnimation = 45;
            item.UseSound = mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EvilLaugh");
            item.scale = 1f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player)
        {
            if (!NPC.AnyNPCs(mod.NPCType("AttraidiesIllusion")))
            {
                NPC.SpawnOnPlayer(player.whoAmI, mod.NPCType("AttraidiesIllusion"));
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}