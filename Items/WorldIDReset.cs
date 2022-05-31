using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class WorldIDReset : ModItem
    {
        public override void SetStaticDefaults()
        {

        DisplayName.SetDefault("{TROUBLESHOOTING ITEM!!} World ID Reset");
        
        Tooltip.SetDefault("Debug Item used for restoring corrupted maps. Can cause minimap corruption if you are not trying to do that!!" +
                "\nHere are the steps to restore your map. It may not work, but there is a chance it will:" +
                "\n1) Use this item" +
                "\n2) Leave the world" +
                "\n3) Rejoin the world with the character you want to have a restored minimap" +
                "\n4) Delete this item and never use it again" +
                "\nWarnings:" +
                "\nThis will only work for one character per world. Attempting to restore a second character's minimap will break the minimap of the first." +
                "\nThis will reset your spawn to the world spawn next time you load the map. This is expected." +
                "\nThis works by temporarily editing your World ID to trick Terraria into recognizing your old minimap as correct." +
                "\nThis is completely unteseted with other mods. Tred very lightly." +
                "\nIf you have entered other copies of the the custom map on this character since the update, there is a nonzero chance your minimap was overwritten" +
                "\nIf so it will not be recoverable.");
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
            Main.PlaySound(4, -1, -1, 43);
			Main.worldID = VariousConstants.CUSTOM_MAP_WORLD_ID;
			return true;
        }
    }
}