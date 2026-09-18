using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only pyromancy staff held by the Oolacile Cultist above half health.</summary>
    public class OolacileCultistStaff : ModItem
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
