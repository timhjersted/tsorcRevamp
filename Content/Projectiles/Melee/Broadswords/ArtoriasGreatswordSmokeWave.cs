using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Chaos;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// The Artorias Greatsword flip-slam's smoke wall: the boss's own curse-smoke floor wave (ChaosShockwave shadow
    /// variant: abyss flame geysers, grey smoke and ChaosImpactCloud gores, hugging the terrain), made friendly.
    /// Spawn with ai[0] = direction (-1/1), ai[1] = arming delay in ticks, ai[2] = run length in px (above 1.5).
    /// Hits each enemy once.
    /// </summary>
    class ArtoriasGreatswordSmokeWave : ChaosShockwave
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.height = 56;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
    }
}
