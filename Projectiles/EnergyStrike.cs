using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class EnergyStrike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 8;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.timeLeft = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;

        }

        public override void AI()
        {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)
            {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
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
            if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 0f)
            {
                if (Main.player[Projectile.owner].channel)
                {
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                if (Projectile.position.X + (float)(Projectile.width / 2) > Main.player[Projectile.owner].position.X + (float)(Main.player[Projectile.owner].width / 2))
                {
                    if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<EnergyField>(), Projectile.damage, 3f, Projectile.owner);
                }
                else
                {
                    if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -4), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<EnergyField>(), Projectile.damage, 3f, Projectile.owner);
                }
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


    }
}
