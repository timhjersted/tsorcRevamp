using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only upright coral hydromancy staff held by the Quara Hydromancer.
    /// 22x86 upright sprite with crescent coral head, glowing white orb, ornamental crossguard, and vertical wood shaft.</summary>
    public class QuaraHydromancerStaff : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 86;
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
