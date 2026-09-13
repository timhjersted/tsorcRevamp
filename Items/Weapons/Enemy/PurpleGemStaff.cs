using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    /// <summary>Presentation-only staff used by Soul of Cinder's purple/blue spell memories.</summary>
    public class PurpleGemStaff : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/PurpleGemStaff";

        public override void SetStaticDefaults() => Item.staff[Type] = true;

        public override void SetDefaults()
        {
            Item.width = 40;
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
