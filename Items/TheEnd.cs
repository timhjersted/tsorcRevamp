using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items {
    class TheEnd : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("As the burning remains of the Mindflayer King lay before you, you look up and out to the horizon." +
                               "\nYou feel relieved, but notice your heart is still not at peace. Attraidies was known for his games." +
                               "\nEven in death you suspect his sway over the world has not ended." +
                               "\nYou remember the magic spell that he put on Aaron and wonder if he cast the same spell on himself..." +
                               "\nLooking down, you notice a Picksaw lying in the ashes, still hot to the touch..." +
                               "\nCongratulations on beating the game! We hope you enjoyed it!- Tim Hjersted & the Revamp Team");
        }

        public override void SetDefaults() {
            Item.maxStack = 1;
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
        }
    }
}
