using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Ice1Ball : ModProjectile
    {

        //Whether or not the projectile is actively being channeled by a player. Public so that the tome can check this.
        public bool isChanneled;
        //How many other projectiles exist and are being channeled. If more than 3, just don't make the sound.
        int projCount = 0;
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.height = 12;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.width = 12;
            Projectile.timeLeft = 300;


            //Iterate through the projectile array
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                //For each, check if it's modded. If so, check if it's the Ice1Ball.
                if (Main.projectile[i].ModProjectile != null && Main.projectile[i].ModProjectile is Projectiles.Ice1Ball)
                {
                    //Cast it to an Ice1Ball so we can check if it's currently being channeled, and make sure it's still active
                    if (((Projectiles.Ice1Ball)Main.projectile[i].ModProjectile).isChanneled && Main.projectile[i].active)
                    {
                        //If so, then up the count
                        projCount++;
                    }
                }
            }
        }

        public override void AI()
        {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f && projCount <= 3)
            {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.8f }, Projectile.Center);
            }
            Vector2 arg_2675_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
            int arg_2675_1 = Projectile.width;
            int arg_2675_2 = Projectile.height;
            int arg_2675_3 = 135;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            if (Main.rand.NextBool(2))
            {
                int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
                Dust expr_2684 = Main.dust[num47];
                expr_2684.velocity *= 0.3f;

                Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].noGravity = true;
            }

            int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 135, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);


            for (int i = 0; i < 2; i++)
            {
                Main.dust[n1337].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 0.8f;
            }
            isChanneled = false;
            if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 0f)
            {
                if (Main.player[Projectile.owner].channel)
                {
                    isChanneled = true;
                    float num48 = 12f;
                    Vector2 vector6 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                    float num49 = (float)Main.mouseX + Main.screenPosition.X - vector6.X;
                    float num50 = (float)Main.mouseY + Main.screenPosition.Y - vector6.Y;
                    float num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    if (num51 > num48)
                    {
                        num51 = num48 / num51;
                        num49 *= num51;
                        num50 *= num51;
                        int num52 = (int)(num49 * 1000f);
                        int num53 = (int)(Projectile.velocity.X * 1000f);
                        int num54 = (int)(num50 * 1000f);
                        int num55 = (int)(Projectile.velocity.Y * 1000f);
                        if (num52 != num53 || num54 != num55)
                        {
                            Projectile.netUpdate = true;
                        }
                        Projectile.velocity.X = num49;
                        Projectile.velocity.Y = num50;
                    }
                    else
                    {
                        int num56 = (int)(num49 * 1000f);
                        int num57 = (int)(Projectile.velocity.X * 1000f);
                        int num58 = (int)(num50 * 1000f);
                        int num59 = (int)(Projectile.velocity.Y * 1000f);
                        if (num56 != num57 || num58 != num59)
                        {
                            Projectile.netUpdate = true;
                        }
                        Projectile.velocity.X = num49;
                        Projectile.velocity.Y = num50;
                    }
                }
                else
                {
                    if (Projectile.ai[0] == 0f)
                    {
                        isChanneled = false;
                        Projectile.ai[0] = 1f;
                        Projectile.netUpdate = true;
                        float num60 = 12f;
                        Vector2 vector7 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                        float num61 = (float)Main.mouseX + Main.screenPosition.X - vector7.X;
                        float num62 = (float)Main.mouseY + Main.screenPosition.Y - vector7.Y;
                        float num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
                        if (num63 == 0f)
                        {
                            vector7 = new Vector2(Main.player[Projectile.owner].position.X + (float)(Main.player[Projectile.owner].width / 2), Main.player[Projectile.owner].position.Y + (float)(Main.player[Projectile.owner].height / 2));
                            num61 = Projectile.position.X + (float)Projectile.width * 0.5f - vector7.X;
                            num62 = Projectile.position.Y + (float)Projectile.height * 0.5f - vector7.Y;
                            num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
                        }
                        num63 = num60 / num63;
                        num61 *= num63;
                        num62 *= num63;
                        Projectile.velocity.X = num61;
                        Projectile.velocity.Y = num62;
                        if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                        {
                            Projectile.Kill();
                        }
                    }
                }
            }
            if (Projectile.type == 34)
            {
                Projectile.rotation += 0.3f * (float)Projectile.direction;
            }
            else
            {
                if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f)
                {
                    Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
                }
            }
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                if (projCount <= 3)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
                }
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), Projectile.velocity.X, 5, ModContent.ProjectileType<Ice1Icicle>(), (int)(Projectile.damage), 3f, Projectile.owner);
                }
                for (int num40 = 0; num40 < 20; num40++)
                {
                    Color newColor = default(Color);
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                        int arg_1394_1 = Projectile.width;
                        int arg_1394_2 = Projectile.height;
                        int arg_1394_3 = 135;
                        float arg_1394_4 = 0f;
                        float arg_1394_5 = 0f;
                        int arg_1394_6 = 100;
                        int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 1.8f);
                        Main.dust[num41].noGravity = true;
                        Dust expr_13B1 = Main.dust[num41];
                        expr_13B1.velocity *= 2f;
                    }
                    Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                    int arg_1422_1 = Projectile.width;
                    int arg_1422_2 = Projectile.height;
                    int arg_1422_3 = 135;
                    float arg_1422_4 = 0f;
                    float arg_1422_5 = 0f;
                    int arg_1422_6 = 100;
                    newColor = default(Color);
                    int n11 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1.8f);
                    Main.dust[n11].noGravity = true;
                    Main.dust[n11].velocity *= 1.5f;

                }
            }
            if (Projectile.owner == Main.myPlayer)
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, (float)Projectile.owner, 0f, 0f, 0);

                }
                Projectile.active = false;
            }
        }
    }
}