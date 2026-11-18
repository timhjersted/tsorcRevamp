using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // The 42x40 sprite runs from the lower-left heel to the upper-right cage.
    // The Warlock grips the shaft 30% of the way up from the heel.
    /// <summary>Presentation-only staff held by the Warlock.</summary>
    public class EnemyWarlockStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 40;
            Item.damage = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.DamageType = DamageClass.Magic;
        }
    }
}
