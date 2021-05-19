using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items {
    class Epilogue : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Congratulations, Red, you have vanquished the final guardian of the Abyss... " +
                               "\nHaving defeated Gwyn, the portal that Attraidies opened at the moment of his death has closed. " +
                               "\nAt last, you can finally feel the energies of the corruption losing their hold on the world," +
                               "\nthe darkness receding back into the earth, growing in harmony with the light once again." +
                               "\nYou think of Elizabeth, waiting for you, and rejoice upon the thought of your return home." +
                               "\nYou take a moment to think about all that you have been through... Forever you shall be known" +
                               "\nas the hero of the age. Centuries from now, elders will tell your story. The story of Red Cloud.");
        }

        public override void SetDefaults() {
            item.maxStack = 1;
            item.width = 20;
            item.height = 20;
            item.rare = ItemRarityID.Pink;
        }
    }
}
