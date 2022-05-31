using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyArrowOfBard : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arrow of Bard");
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 10;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.8f;
            Projectile.tileCollide = true;
            Projectile.width = 5;
            aiType = ProjectileID.WoodenArrowFriendly;
            Projectile.aiStyle = 1;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 0;
            Main.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 0.5f);
            }
            return true;
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
                Main.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
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
