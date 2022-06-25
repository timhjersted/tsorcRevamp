using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyThrowingKnifeSmall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("(O_O;)");
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = 2;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 34; //was 8
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = .5f; //was .8
            Projectile.tileCollide = true;
            Projectile.width = 18; //was 8
            AIType = ProjectileID.WoodenArrowFriendly;
        }


        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 0;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 1f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/im-sorry") with { Volume = 0.3f }, Projectile.Center);
        }

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

}
