using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.Rings;

namespace tsorcRevamp.Content.Items.Debug
{
    public class DevDefenseRing : ModItem
    {
        public const int DefenseBonus = 1000;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SilverRing));

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.defense = DefenseBonus;
            Item.accessory = true;
            Item.value = 0;
            Item.rare = ItemRarityID.Expert;
        }
    }
}
