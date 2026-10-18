using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // PLACEHOLDER sprite (copy of PurpleGemStaff) — replace with the green gem staff art.
    // 42x40, shaft runs from butt (0,39) to gem tip (41,0); Archdeacon grips it 30% up from the butt.
    /// <summary>Presentation-only staff held by the Archdeacon.</summary>
    public class ArchdeaconStaff : ModItem
    {

        public override void SetStaticDefaults() => Item.staff[Type] = true;

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
