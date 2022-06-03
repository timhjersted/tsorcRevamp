using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonFieldBall : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
        }


        #region Kill
        public override bool PreKill(int timeLeft)
        {

            Projectile.timeLeft = 0;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonField>(), Projectile.damage, 1f, Projectile.owner);
            Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int arg_1394_1 = Projectile.width;
            int arg_1394_2 = Projectile.height;
            int arg_1394_3 = 15;
            float arg_1394_4 = 0f;
            float arg_1394_5 = 0f;
            int arg_1394_6 = 100;
            Color newColor = default(Color);
            int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
            Main.dust[num41].noGravity = true;
            Dust expr_13B1 = Main.dust[num41];
            expr_13B1.velocity *= 2f;
            Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
            int arg_1422_1 = Projectile.width;
            int arg_1422_2 = Projectile.height;
            int arg_1422_3 = 15;
            float arg_1422_4 = 0f;
            float arg_1422_5 = 0f;
            int arg_1422_6 = 100;
            newColor = default(Color);
            num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);


            Projectile.active = false;
            return true;
        }
        #endregion
    }
}

