using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Chaos's sickle, thrown in rings by Scythe Lunge and one at a time by Orbital Sickle. Vanilla
    ///DemonSickle defaults/AI/texture, but hostile and non-tile-colliding from SetDefaults — see
    ///<see cref="ChaosFireball"/> for why patching those after spawn desyncs.
    ///</summary>
    class ChaosDemonSickle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DemonSickle;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.DemonSickle);
            AIType = ProjectileID.DemonSickle;

            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
        }
    }
}
