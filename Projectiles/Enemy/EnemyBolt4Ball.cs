using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemyBolt4Ball : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults() {
            Projectile.aiStyle = 4; //maybe 11 (shadow orb) instead
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.light = 0.8f;
            Projectile.timeLeft = 50;
        }

        public void Kill() {

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
            
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemyBolt4Bolt>(), Projectile.damage, 8f, Projectile.owner);
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
            newColor = default;
            num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
            

            if (Projectile.owner == Main.myPlayer) {
                if (Main.netMode != NetmodeID.SinglePlayer) {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, (float)Projectile.owner, 0f, 0f, 0);
                }
            }
            Projectile.active = false;
        }
    }
}
