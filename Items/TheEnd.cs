using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items {
    class TheEnd : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("As the remains of the Mindflayer King lay before you, you look up and out to the horizon." +
                               "\nYou can feel the darkness starting to subside, but your heart is not at peace." +
                               "\nAttraidies was known for his games. Even in death you suspect his sway over the world has not ended." +
                               "\nYou remember the magic spell that he put on Aaron and wonder if he cast the same spell on himself..." +
                               "\nLooking down, you notice this Chaos relic burning in the ashes. You pick it up. Could it be the end?" +
                               "\nOne last congratulations on beating the game! I hope you enjoyed it! - Tim Hjersted and the Revamp Team");
        }

        public override void SetDefaults() {
            item.maxStack = 1;
            item.width = 20;
            item.height = 20;
            item.rare = ItemRarityID.Pink;
        }
    }
}
