using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {
    public class TeslaBolt : ModItem {

        public override void SetDefaults() {
            item.consumable = true;
            item.ranged = true;
            item.width = 14;
            item.height = 20;
            item.maxStack = 9999;
            item.scale = 1f;
            item.value = 3;
            item.ammo = item.type;
            item.shoot = ModContent.ProjectileType<Projectiles.RedLaserBeam>();
        }
    }
}
