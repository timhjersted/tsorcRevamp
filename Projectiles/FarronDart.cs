using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FarronDart : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 6, 8), Color.White, Projectile.rotation, new Vector2(3, 4), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // change the hitbox size, centered about the original projectile center. This makes the projectile have small aoe.
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);

            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            if (Projectile.velocity.X > 0) //if going right
            {
                for (int d = 0; d < 4; d++)
                {
                    int num44 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 2), Projectile.width, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                    Main.dust[num44].noGravity = true;
                    Main.dust[num44].velocity *= 0f;
                }

                for (int d = 0; d < 4; d++)
                {
                    int num45 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 2), Projectile.width - 4, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                    Main.dust[num45].noGravity = true;
                    Main.dust[num45].velocity *= 0f;
                    Main.dust[num45].fadeIn *= 1f;
                }
            }
            else //if going left
            {
                for (int d = 0; d < 4; d++)
                {
                    int num44 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 1), Projectile.width, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                    Main.dust[num44].noGravity = true;
                    Main.dust[num44].velocity *= 0f;
                }

                for (int d = 0; d < 4; d++)
                {
                    int num45 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 1), Projectile.width - 4, Projectile.height, 68, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                    Main.dust[num45].noGravity = true;
                    Main.dust[num45].velocity *= 0f;
                    Main.dust[num45].fadeIn *= 1f;
                }
            }

            Lighting.AddLight(Projectile.Center, .200f, .200f, .350f);


            if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 0f)
            {
                if (Main.player[Projectile.owner].channel)
                {
                    float num48 = 10f;
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
                        float num60 = 10f;
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
            Projectile.rotation += 0.3f * (float)Projectile.direction;

        }

        public override void OnKill(int timeLeft)
        {
            for (int d = 0; d < 14; d++)
            {
                int dust = Dust.NewDust(Projectile.Center, 8, 8, 68, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.35f }, Projectile.position);

        }
    }
}
