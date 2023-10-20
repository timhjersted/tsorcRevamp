using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Whips
{

    public class EnchantedWhipFallingStar : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {

            Projectile.CloneDefaults(ProjectileID.FallingStar);
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            Dust.NewDust(Projectile.Center, Projectile.height, Projectile.width, 57, 0f, 0f, 10, Color.AliceBlue, 0.5f);
        }
    }
}
