using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellGreatPoisonStrikeBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Great Poison Strike Ball");
        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 23;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.width = 16;
            projectile.height = 16;
            projectile.light = 0.8f;
            projectile.penetrate = 1;
            projectile.magic = true;
            projectile.scale = 1f;
            projectile.tileCollide = true;
        }


        #region Kill
        public override void Kill(int timeLeft)
        {

            if (!projectile.active)
            {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(6, (int)projectile.position.X, (int)projectile.position.Y, 0, 0.3f, -4.7f); //grass cut slowed down
                if (projectile.owner == Main.myPlayer)

                {

                    int poisonball = Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height)), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(), (int)(this.projectile.damage), 1f, projectile.owner);

                    Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height)), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(), (int)(this.projectile.damage), 1f, projectile.owner);
                    Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                    int arg_1394_1 = projectile.width;
                    int arg_1394_2 = projectile.height;
                    int arg_1394_3 = 15;
                    float arg_1394_4 = 0f;
                    float arg_1394_5 = 0f;
                    int arg_1394_6 = 100;
                    Color newColor = default(Color);
                    int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
                    Main.dust[num41].noGravity = true;
                    Dust expr_13B1 = Main.dust[num41];
                    expr_13B1.velocity *= 2f;
                    Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                    int arg_1422_1 = projectile.width;
                    int arg_1422_2 = projectile.height;
                    int arg_1422_3 = 15;
                    float arg_1422_4 = 0f;
                    float arg_1422_5 = 0f;
                    int arg_1422_6 = 100;
                    newColor = default(Color);
                    num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);


                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(27, -1, -1, null, poisonball, 0f, 0f, 0f, 0); // where b is the index of the projectile spawned; i.e. int b = Projectile.NewProjectile(stuff);
                    }

                }


            }

            projectile.active = false;


        }
        #endregion

    }
}