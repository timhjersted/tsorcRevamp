using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellAbyssStormBall : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Dark Wave");
    }
    public override void SetDefaults()
    {
        Projectile.aiStyle = 23;
        Projectile.hostile = true;
        Projectile.height = 16;
        Projectile.light = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 8;
        Projectile.scale = 1.2f;
        Projectile.tileCollide = true;
        Projectile.width = 16;
        Projectile.timeLeft = 0;
    }

    #region AI
    public override void AI()
    {
        if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
        {
            Projectile.soundDelay = 10;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
        }
        Vector2 arg_2675_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
        int arg_2675_1 = Projectile.width;
        int arg_2675_2 = Projectile.height;
        int arg_2675_3 = 15;
        float arg_2675_4 = 0f;
        float arg_2675_5 = 0f;
        int arg_2675_6 = 100;
        Color newColor = default(Color);
        int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
        Dust expr_2684 = Main.dust[num47];
        expr_2684.velocity *= 0.3f;
        Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
        Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
        Main.dust[num47].noGravity = true;
        if (Projectile.type == 34)
        {
            Projectile.rotation += 0.3f * (float)Projectile.direction;
        }
        else
        {
            if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f)
            {
                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
            }
        }
        if (Projectile.velocity.Y > 16f)
        {
            Projectile.velocity.Y = 16f;
            return;
        }
    }
    #endregion

    public override void Kill(int timeLeft)
    {
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0, 0, ModContent.ProjectileType<EnemySpellAbyssStorm>(), Projectile.damage, 8f, Projectile.owner);

        Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
        Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
    }
}