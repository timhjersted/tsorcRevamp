using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemyBolt4Bolt : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/Bolt4Bolt";

    public override void SetDefaults()
    {
        Projectile.width = 250;
        Projectile.height = 450;
        Projectile.penetrate = 16;
        Projectile.aiStyle = 4;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.light = 0.8f;
        Main.projFrames[Projectile.type] = 16;
    }

    public override bool PreAI()
    {
        if (Projectile.ai[0] == 0)
        {
            Projectile.velocity.X *= 0.001f;
            Projectile.velocity.Y *= 0.001f;
            Projectile.ai[0] = 1;
        }

        Projectile.frameCounter++;
        Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

        if (Projectile.frame >= 16)
        {
            Projectile.frame = 15;
        }
        if (Projectile.frameCounter > 71)
        { // (projFrames * 4.5) - 1
            Projectile.alpha += 15;
        }

        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }
        return true;
    }
}