using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

public class EnemyThrowingKnife : ModProjectile
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Sharpened Blade");
    }
    public override void SetDefaults()
    {

        Projectile.aiStyle = 1; //2 makes it spin but has heavy gravity, 
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.height = 8;
        Projectile.penetrate = 2;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = 0.8f;
        Projectile.tileCollide = true;
        Projectile.width = 8;
        //AIType = ProjectileID.WoodenArrowFriendly; //gives more gravity
    }

    public override void AI()
    {
        //if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
        //{
        //	projectile.soundDelay = 10;
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 17);
        //}

        //projectile.rotation += 1f;
        if (Main.rand.NextBool(5))
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 50, Color.WhiteSmoke, 1.0f);
            Main.dust[dust].noGravity = false;
        }
        //Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

        //if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
        //{
        //   float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
        //    projectile.velocity.X *= accel;
        //    projectile.velocity.Y *= accel;
        //}


    }
    #region PreKill
    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 0; 
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Dig with { Volume = 0.5f }, Projectile.Center);
        for (int i = 0; i < 10; i++)
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 1, 0, 0, 0, default, 1f);
        }
        return true;
    }
    #endregion

    #region Kill
    public void Kill()
    {
        //int num98 = -1;
        if (!Projectile.active)
        {
            return;
        }
        Projectile.timeLeft = 0;
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                int arg_92_1 = Projectile.width;
                int arg_92_2 = Projectile.height;
                int arg_92_3 = 7;
                float arg_92_4 = 0f;
                float arg_92_5 = 0f;
                int arg_92_6 = 0;
                Color newColor = default(Color);
                Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
            }
        }
        Projectile.active = false;
    }
    #endregion
}
