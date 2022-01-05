using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class DodgerollMemo : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("It looks like a diary entry. It reads as follows:" +
                                "\n\"There have been rumours of bandits in the forest lately," +
                                "\nso father has been apprehensive about letting me go into the" +
                                "\nforest alone to pick mushrooms... Pfft, what does he know?" +
                                "\nI may be small, but I'm nimble and have mastered the technique" +
                                "\nof the dodgeroll! I'll be fine as long as I don't run out of stamina...\"" +
                                "\nJudging by how bandits were in possesion of this note, I guess it" +
                                "\ndidn't go too well for them. Perhaps I can make good use of this technique?");
        }

        public override void SetDefaults()
        {
            item.width = 32;
            item.height = 32;
            item.consumable = false;
            item.maxStack = 1;
            item.value = 5000;
            item.rare = ItemRarityID.Orange;
        }
    }
}