using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class HerosArrow : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.hostile = true;
        Projectile.height = 14;
        Projectile.penetrate = 2;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = 0.8f;
        Projectile.tileCollide = true;
        AIType = 1;
        Projectile.width = 14;
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 0;
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        for (int i = 0; i < 10; i++)
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
        }
        return true;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }
}