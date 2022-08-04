using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class Epilogue : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("[c/ffbf00:Congratulations! You have unlocked the true ending of the game!]" +
                               "\nHaving defeated Gwyn, the portal that Attraidies opened at the moment of his death has closed." +
                               "\nAt last, you can finally feel the energies of the corruption losing their hold on the world," +
                               "\nthe darkness receding back into the earth, growing in harmony with the light once again." +
                               "\nYou think of Asha, waiting for you, and rejoice upon the thought of your return home." +
                               "\nYou take a moment to think about all that you have been through... Forever you shall be known" +
                               "\nas the hero of the age. Centuries from now, elders will be telling your story. The story of Red Cloud.");
        }

        public override void SetDefaults()
        {
            Item.maxStack = 1;
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
        }
    }
}
