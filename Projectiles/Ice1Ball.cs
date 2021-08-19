using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Ice1Ball : ModProjectile {

        //Whether or not the projectile is actively being channeled by a player. Public so that the tome can check this.
        public bool isChanneled;
        //How many other projectiles exist and are being channeled. If more than 3, just don't make the sound.
        int projCount = 0;
        public override void SetDefaults()
        {
            projectile.friendly = true;
            projectile.height = 12;
            projectile.magic = true;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 12;


            //Iterate through the projectile array
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                //For each, check if it's modded. If so, check if it's the Ice1Ball.
                if (Main.projectile[i].modProjectile != null && Main.projectile[i].modProjectile is Projectiles.Ice1Ball)
                {
                    //Cast it to an Ice1Ball so we can check if it's currently being channeled, and make sure it's still active
                    if (((Projectiles.Ice1Ball)Main.projectile[i].modProjectile).isChanneled && Main.projectile[i].active)
                    {
                        //If so, then up the count
                        projCount++;
                    }
                }
            }
        }

        public override void AI() {
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f && projCount <= 3) {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9, 0.8f);
            }
            Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
            int arg_2675_1 = projectile.width;
            int arg_2675_2 = projectile.height;
            int arg_2675_3 = 15;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            if (Main.rand.Next(2) == 0)
            {
                int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
                Dust expr_2684 = Main.dust[num47];
                expr_2684.velocity *= 0.3f;

                Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].noGravity = true;
            }

            int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 172, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);


            for (int i = 0; i < 2; i++)
            {
                Main.dust[n1337].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 0.8f;
            }
            isChanneled = false;
            if (Main.myPlayer == projectile.owner && projectile.ai[0] == 0f) {
                if (Main.player[projectile.owner].channel) {
                    isChanneled = true;
                    float num48 = 12f;
                    Vector2 vector6 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
                    float num49 = (float)Main.mouseX + Main.screenPosition.X - vector6.X;
                    float num50 = (float)Main.mouseY + Main.screenPosition.Y - vector6.Y;
                    float num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    if (num51 > num48) {
                        num51 = num48 / num51;
                        num49 *= num51;
                        num50 *= num51;
                        int num52 = (int)(num49 * 1000f);
                        int num53 = (int)(projectile.velocity.X * 1000f);
                        int num54 = (int)(num50 * 1000f);
                        int num55 = (int)(projectile.velocity.Y * 1000f);
                        if (num52 != num53 || num54 != num55) {
                            projectile.netUpdate = true;
                        }
                        projectile.velocity.X = num49;
                        projectile.velocity.Y = num50;
                    }
                    else {
                        int num56 = (int)(num49 * 1000f);
                        int num57 = (int)(projectile.velocity.X * 1000f);
                        int num58 = (int)(num50 * 1000f);
                        int num59 = (int)(projectile.velocity.Y * 1000f);
                        if (num56 != num57 || num58 != num59) {
                            projectile.netUpdate = true;
                        }
                        projectile.velocity.X = num49;
                        projectile.velocity.Y = num50;
                    }
                }
                else {
                    if (projectile.ai[0] == 0f) {
                        isChanneled = false;
                        projectile.ai[0] = 1f;
                        projectile.netUpdate = true;
                        float num60 = 12f;
                        Vector2 vector7 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
                        float num61 = (float)Main.mouseX + Main.screenPosition.X - vector7.X;
                        float num62 = (float)Main.mouseY + Main.screenPosition.Y - vector7.Y;
                        float num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
                        if (num63 == 0f) {
                            vector7 = new Vector2(Main.player[projectile.owner].position.X + (float)(Main.player[projectile.owner].width / 2), Main.player[projectile.owner].position.Y + (float)(Main.player[projectile.owner].height / 2));
                            num61 = projectile.position.X + (float)projectile.width * 0.5f - vector7.X;
                            num62 = projectile.position.Y + (float)projectile.height * 0.5f - vector7.Y;
                            num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
                        }
                        num63 = num60 / num63;
                        num61 *= num63;
                        num62 *= num63;
                        projectile.velocity.X = num61;
                        projectile.velocity.Y = num62;
                        if (projectile.velocity.X == 0f && projectile.velocity.Y == 0f) {
                            projectile.Kill();
                        }
                    }
                }
            }
            if (projectile.type == 34) {
                projectile.rotation += 0.3f * (float)projectile.direction;
            }
            else {
                if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f) {
                    projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
                }
            }
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }

        public override void Kill(int timeLeft) {
            if (!projectile.active) {
                return;
            }
            projectile.timeLeft = 0;
            {
                if(projCount <=3) Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 27);
                Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), projectile.velocity.X, 5, ModContent.ProjectileType<Ice1Icicle>(), (int)(projectile.damage), 3f, projectile.owner);
                for (int num40 = 0; num40 < 20; num40++) {
                    Color newColor = default(Color);
                    if (Main.rand.Next(2) == 0)
                    {
                        Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                        int arg_1394_1 = projectile.width;
                        int arg_1394_2 = projectile.height;
                        int arg_1394_3 = 15;
                        float arg_1394_4 = 0f;
                        float arg_1394_5 = 0f;
                        int arg_1394_6 = 100;
                        int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 1.8f);
                        Main.dust[num41].noGravity = true;
                        Dust expr_13B1 = Main.dust[num41];
                        expr_13B1.velocity *= 2f;
                    }
                    Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                    int arg_1422_1 = projectile.width;
                    int arg_1422_2 = projectile.height;
                    int arg_1422_3 = 172;
                    float arg_1422_4 = 0f;
                    float arg_1422_5 = 0f;
                    int arg_1422_6 = 100;
                    newColor = default(Color);
                    int n11 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1.8f);
                    Main.dust[n11].noGravity = true;
                    Main.dust[n11].velocity *= 1.5f;

                }
            }
            if (projectile.owner == Main.myPlayer) {
                if (Main.netMode != NetmodeID.SinglePlayer) {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, (float)projectile.owner, 0f, 0f, 0);

                }
                projectile.active = false;
            }
        }
    }
}