using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo
{
    public class TeslaBolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 14;
            Item.height = 20;
            Item.maxStack = 9999;
            Item.scale = 1f;
            Item.value = 3;
            Item.ammo = Item.type;
            Item.shoot = ModContent.ProjectileType<Projectiles.RedLaserBeam>();
        }
    }
}
