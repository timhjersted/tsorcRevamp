using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Chaos's fan/ring fireball. Behaviourally identical to vanilla Fireball (same defaults, same AI,
    ///same texture) but hostile and non-tile-colliding FROM SetDefaults instead of being patched after
    ///spawn. Neither `hostile` nor `tileCollide` is part of the projectile sync packet, so the old
    ///post-spawn assignment never reached other clients — every remote peer ran a tile-colliding copy.
    ///</summary>
    class ChaosFireball : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Fireball);
            AIType = ProjectileID.Fireball;

            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
        }
    }
}
