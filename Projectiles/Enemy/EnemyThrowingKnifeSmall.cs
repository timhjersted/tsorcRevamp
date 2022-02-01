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

            projectile.aiStyle = 2;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.height = 34; //was 8
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.scale = .5f; //was .8
            projectile.tileCollide = true;
            projectile.width = 18; //was 8
            aiType = ProjectileID.WoodenArrowFriendly;
        }

        
        public override bool PreKill(int timeLeft)
        {
            projectile.type = 0;
            Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.PlaySound(SoundLoader.customSoundType, (int)projectile.position.X, (int)projectile.position.Y, mod.GetSoundSlot(Terraria.ModLoader.SoundType.Custom, "Sounds/DarkSouls/im-sorry"), 0.3f, 0.0f);
        }

        #region Kill
        public void Kill()
        {
            //int num98 = -1;
            if (!projectile.active)
            {
                return;
            }
            projectile.timeLeft = 0;
            {
                
                Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(projectile.position.X, projectile.position.Y);
                    int arg_92_1 = projectile.width;
                    int arg_92_2 = projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            projectile.active = false;
        }
        #endregion
    }

}
