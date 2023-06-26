using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Debug
{
    class WorldIDReset : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 21;
            Item.height = 21;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
        }


        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath43);
            Main.worldID = VariousConstants.CUSTOM_MAP_WORLD_ID;
            return true;
        }
    }
}