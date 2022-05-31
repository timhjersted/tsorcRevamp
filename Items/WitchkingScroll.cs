using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class WitchkingScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("To close the portal to the Abyss you must seek out the Witchking and restore the strange ring he drops." +
                                "\nHe will appear out of the Abyss at night, and more often deeper underground, especially in dungeons." +
                                "\nThe most assured way to find him, however, is to enter the Abyss yourself, using the Covanent of Artorias ring.");
        }
        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 32;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 50000;
            Item.rare = ItemRarityID.Purple;
        }
    }
}