using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class SwordOfLordGwynDrawFix : GlobalProjectile
    {
        public override void PostAI(Projectile projectile)
        {
            if (projectile.type == ModContent.ProjectileType<SwordOfLordGwynSlash>())
            {
                projectile.hide = false;
            }
        }
    }
}
