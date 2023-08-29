using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

public class BlackKnightSpear : ModProjectile
{
    public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Thrown/ThrowingSpear";

    public override void SetDefaults()
    {
        Projectile.aiStyle = 1;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.height = 12;
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = 0.8f;
        Projectile.tileCollide = true;
        Projectile.width = 12;
    }

    #region Kill
    public override void Kill(int timeLeft)
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
