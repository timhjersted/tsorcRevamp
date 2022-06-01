using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellArmageddonBall : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.light = 1f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 8;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            //timeLeft = 0
        }

        #region AI
        public override void AI()
        {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
            {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
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
            if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f)
            {
                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
            }
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
        #endregion

        #region Kill
        public void Kill()
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * +3f), Projectile.position.Y + (float)(Projectile.height * +5.5f), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellArmageddon>(), Projectile.damage, 8f, Projectile.owner);
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
            }
            Projectile.active = false;
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(24, 900, false);
        }
    }
}