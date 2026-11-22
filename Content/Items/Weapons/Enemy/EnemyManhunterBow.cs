using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only bow the Man Hunter carries and shoots. Deliberately draws the vanilla Wooden Bow
    /// (permanent texture sharing, not a placeholder): 16x32, facing right, riser x10..15 / y12..19, string inset at
    /// x2..3 / y6..25 with the limb tips at x0..5 above and below it.</summary>
    public class EnemyManhunterBow : ModItem
    {
        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ItemID.WoodenBow, "Item");

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 32;
            Item.damage = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item5;
            Item.value = 0;
        }
    }
}
