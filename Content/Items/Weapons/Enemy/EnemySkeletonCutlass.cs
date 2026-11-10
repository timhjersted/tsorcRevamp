using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only sword the Skeleton Warrior carries. Deliberately draws the vanilla Cutlass (permanent
    /// texture sharing, not a placeholder): 40x48, blade up-right, pommel (2,45), basket guard y28-43, tip (37,1).
    /// Never dropped and has no recipe; stats copy the Cutlass so the swing clock matches it.</summary>
    public class EnemySkeletonCutlass : ModItem
    {
        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ItemID.Cutlass, "Item");

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.damage = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.UseSound = SoundID.Item1;
            Item.knockBack = 4f;
            Item.value = 0;
        }
    }
}
