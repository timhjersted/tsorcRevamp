using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
	class PoisonFieldBall : ModProjectile {

		public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 0.3f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
		}
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(BuffID.Poisoned, 360);
            }
        }

        public override void AI() {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f) {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
            }
            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Poisoned, 0, 0, 100, default(Color), 1.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= .5f;
                Main.dust[dust].fadeIn = 0.3f;
            }

            Vector2 arg_2675_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
            int arg_2675_1 = Projectile.width;
            int arg_2675_2 = Projectile.height;
            int arg_2675_3 = 74;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 1f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;

            if (Projectile.velocity.Y > 16f) {
                Projectile.velocity.Y = 16f;
                return;
            }
        }

        public override void Kill(int timeLeft) {
            if (!Projectile.active) {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
                Projectile.NewProjectile(Projectile.position.X + Projectile.width / 2, Projectile.position.Y + Projectile.height / 2, Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<PoisonField>(), Projectile.damage, 1f, Projectile.owner);

                for (int num40 = 0; num40 < 40; num40++) {
                    Vector2 arg_1394_0 = new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y);
                    int arg_1394_1 = Projectile.width;
                    int arg_1394_2 = Projectile.height;
                    int arg_1394_3 = 74;
                    float arg_1394_4 = 0f;
                    float arg_1394_5 = 0f;
                    int arg_1394_6 = 100;
                    Color newColor = default;
                    int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 1f);
                    Main.dust[num41].noGravity = true;
                    Dust expr_13B1 = Main.dust[num41];
                    expr_13B1.velocity *= 1.2f;
                }
            }
            if (Projectile.owner == Main.myPlayer) {
                if (Main.netMode != NetmodeID.SinglePlayer) {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, (float)Projectile.owner, 0f, 0f, 0);
                }
            }
            Projectile.active = false;
        }
    }
}
