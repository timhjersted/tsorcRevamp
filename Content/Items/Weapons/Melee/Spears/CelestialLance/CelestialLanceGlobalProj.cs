using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.CelestialLance;

public class CelestialLanceGlobalProj : GlobalProjectile
{
    public override bool PreAI(Projectile projectile)
    {
        if (projectile.type == ProjectileID.StarVeilStar && projectile.ai[1] != 0)
        {
            projectile.CritChance = (int)projectile.ai[1];
            projectile.DamageType = DamageClass.Melee;
        }
        return base.PreAI(projectile);
    }
}