using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class CursedFlames : ModProjectile
{

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.scale = 1.3f;
        Projectile.alpha = 255;
        Projectile.timeLeft = 100;
        Projectile.friendly = true;
        Projectile.penetrate = 3;
        Projectile.light = 0.8f;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 4;
    }

    public override void AI()
    {
        for (int i = 0; i < 2; i++)
        {
            int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y),
                                     Projectile.width,
                                     Projectile.height,
                                     75,
                                     Projectile.velocity.X * 0.2f,
                                     Projectile.velocity.Y * 0.2f,
                                     100,
                                     default,
                                     3f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity.X *= 0.3f;
            Main.dust[dust].velocity.Y *= 0.3f;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {

        for (int i = 0; i < 6; i++)
        {
            Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y),
                                         Projectile.width,
                                         Projectile.height,
                                         75,
                                         -Projectile.velocity.X * 0.3f,
                                         -Projectile.velocity.Y * 0.3f,
                                         100,
                                         default,
                                         0.8f);
        }
        Projectile.Kill();
        return true;
    }
}
