using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // PLACEHOLDER sprite (copy of PurpleGemStaff) — replace with bespoke art (a dark wood shaft with a blue-violet orb
    // held in a cage head, matching the Warlock's robe). Marik and the Archdeacon already show this same silhouette.
    // 42x40, shaft runs from butt (0,39) to gem tip (41,0); the Warlock grips it 30% up from the butt.
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
