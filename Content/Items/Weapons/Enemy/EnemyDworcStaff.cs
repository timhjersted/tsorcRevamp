using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // Upright Witch Doctor extraction. Grip (15,27), crooked casting tip (6,1) in the 18x42 PNG.
    public class EnemyDworcStaff : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 42;
            Item.damage = 0;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.DamageType = DamageClass.Magic;
        }
    }
}
