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
            Projectile.aiStyle = 23;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.light = 0.8f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
        }


        #region Kill
        public override void Kill(int timeLeft)
        {

            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(6, (int)Projectile.position.X, (int)Projectile.position.Y, 0, 0.3f, -4.7f); //grass cut slowed down
                if (Projectile.owner == Main.myPlayer)

                {

                    int poisonball = Projectile.NewProjectile(new Vector2(Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height)), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(), (int)(this.Projectile.damage), 1f, Projectile.owner);

                    Projectile.NewProjectile(new Vector2(Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height)), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(), (int)(this.Projectile.damage), 1f, Projectile.owner);
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


                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(27, -1, -1, null, poisonball, 0f, 0f, 0f, 0); // where b is the index of the projectile spawned; i.e. int b = Projectile.NewProjectile(stuff);
                    }

                }


            }

            Projectile.active = false;


        }
        #endregion

    }
}