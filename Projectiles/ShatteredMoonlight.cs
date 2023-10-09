using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Projectiles
{
    class ShatteredMoonlight : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EnchantedBoomerang);
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 0.2f;
            Projectile.DamageType = DamageClass.Melee;
        }

        public static Vector2 RotateVector(Vector2 origin, Vector2 vecToRot, float rot)
        {
            float newPosX = (float)(Math.Cos(rot) * (vecToRot.X - origin.X) - Math.Sin(rot) * (vecToRot.Y - origin.Y) + origin.X);
            float newPosY = (float)(Math.Sin(rot) * (vecToRot.X - origin.X) + Math.Cos(rot) * (vecToRot.Y - origin.Y) + origin.Y);
            return new Vector2(newPosX, newPosY);
        }
        public static float RotationTo(Vector2 startPos, Vector2 endPos)
        {
            return (float)Math.Atan2(endPos.Y - startPos.Y, endPos.X - startPos.X);
        }

        public static void AIBoomerang(Projectile p, ref float[] ai, Vector2 position = default(Vector2), int width = -1, int height = -1, bool playSound = true, //whether or not to make whirring noise of boomerang
                                                                                                                                           float maxDistance = 9f, //Not sure, but higher means faster return speed
                                                                                                                                           int returnDelay = 35, //ticks before returning
                                                                                                                                           float speedInterval = 0.4f, //acceleration for return; if boomerang is fast, keep high
                                                                                                                                           float rotationInterval = 0.4f, //how fast the boomerang rotates
                                                                                                                                           bool direct = false) //not tested
        {
            if (position == default(Vector2)) { position = Main.player[p.owner].position; }
            if (width == -1) { width = Main.player[p.owner].width; }
            if (height == -1) { height = Main.player[p.owner].height; }
            Vector2 center = position + new Vector2(width * 0.5f, height * 0.5f);
            if (playSound && p.soundDelay == 0)
            {
                p.soundDelay = 8;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7, p.Center);
            }
            if (ai[0] == 0f)
            {
                ai[1] += 1f;
                if (ai[1] >= returnDelay)
                {
                    ai[0] = 1f;
                    ai[1] = 0f;
                    p.netUpdate = true;
                }
            }
            else
            {
                p.tileCollide = false;
                float distPlayerX = center.X - p.Center.X;
                float distPlayerY = center.Y - p.Center.Y;
                float distPlayer = (float)Math.Sqrt((double)(distPlayerX * distPlayerX + distPlayerY * distPlayerY));
                if (distPlayer > 3000f)
                {
                    p.Kill();
                }
                if (direct)
                {
                    p.velocity = RotateVector(default(Vector2), new Vector2(speedInterval, 0f), RotationTo(p.Center, center));
                }
                else
                {
                    distPlayer = maxDistance / distPlayer;
                    distPlayerX *= distPlayer;
                    distPlayerY *= distPlayer;
                    if (p.velocity.X < distPlayerX)
                    {
                        p.velocity.X += speedInterval;
                        if (p.velocity.X < 0f && distPlayerX > 0f) { p.velocity.X += speedInterval; }
                    }
                    else
                    if (p.velocity.X > distPlayerX)
                    {
                        p.velocity.X -= speedInterval;
                        if (p.velocity.X > 0f && distPlayerX < 0f) { p.velocity.X -= speedInterval; }
                    }
                    if (p.velocity.Y < distPlayerY)
                    {
                        p.velocity.Y += speedInterval;
                        if (p.velocity.Y < 0f && distPlayerY > 0f) { p.velocity.Y += speedInterval; }
                    }
                    else
                    if (p.velocity.Y > distPlayerY)
                    {
                        p.velocity.Y -= speedInterval;
                        if (p.velocity.Y > 0f && distPlayerY < 0f) { p.velocity.Y -= speedInterval; }
                    }
                }
                if (Main.myPlayer == p.owner)
                {
                    Rectangle rectangle = p.Hitbox;
                    Rectangle value = new Rectangle((int)position.X, (int)position.Y, width, height);
                    if (rectangle.Intersects(value)) { p.Kill(); }
                }
            }
            p.rotation += rotationInterval * (float)p.direction;
        }

        public override bool PreAI()
        {
            if (Projectile.ai[0] < 2)
            {
                AIBoomerang(Projectile, ref Projectile.ai, default(Vector2), -1, -1, true, 15f, 15, 1.2f, .8f, false);
            }

            if (Projectile.ai[0] == 2)
            {
                Projectile.friendly = false;
                Projectile.hostile = true;
                Projectile.width = 16;
                Projectile.height = 16;
                Projectile.penetrate = -1;
                Projectile.tileCollide = true;
                Projectile.DamageType = DamageClass.Ranged;

                Projectile.ai[1] += 1f; // Use a timer to wait 12 ticks before applying gravity.
                if (Projectile.ai[1] >= 15f)
                {
                    Projectile.ai[1] = 15f;
                    Projectile.velocity.Y = Projectile.velocity.Y + 0.12f;
                }
                if (Projectile.velocity.Y > 16f)
                {
                    Projectile.velocity.Y = 16f;
                }
                Projectile.rotation += 0.4f * (float)Projectile.direction;

            }

            if (Main.rand.NextBool(6))
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 89, 0, 0, 50, default(Color), 1f)];
                dust2.velocity *= 0;
                dust2.noGravity = true;
            }

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.ai[0] < 2)
            {
                Projectile.ai[0] = 1f;
                Projectile.velocity.X = -Projectile.velocity.X;
                Projectile.velocity.Y = -Projectile.velocity.Y;
                Projectile.netUpdate = true;
            }

            if (Projectile.ai[0] == 2)
            {
                Projectile.Kill();
            }

            for (int i = 0; i <= 5; i++)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 89, Projectile.velocity.X * -0.5f, Projectile.velocity.Y * -0.5f, 50, default(Color), 0.8f)];
                dust2.noGravity = true;
            }

            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i <= 5; i++)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 89, Projectile.velocity.X * -0.5f, Projectile.velocity.Y * -0.5f, 50, default(Color), 0.8f)];
                dust2.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Color alphalowered = Color.White * .4f;
            Texture2D textureGlow = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ShatteredMoonlightGlowmask];
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), lightColor, Projectile.rotation, new Vector2(12, 12), Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(textureGlow, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), alphalowered, Projectile.rotation, new Vector2(12, 12), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == 2)
            {
                if (!Projectile.active)
                {
                    return;
                }

                Projectile.timeLeft = 0;

                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                    for(int i = 0; i < Main.CurrentFrameFlags.ActivePlayersCount; i++)
                    {
                        Item.NewItem(Projectile.GetSource_FromThis(), Projectile.position, ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>(), 1, false, -1);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                        int arg_92_1 = Projectile.width;
                        int arg_92_2 = Projectile.height;
                        int arg_92_3 = 89;
                        float arg_92_4 = 0f;
                        float arg_92_5 = 0f;
                        int arg_92_6 = 0;
                        Color newColor = default(Color);
                        Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                    }
                }
                Projectile.active = false;
            }
        }
    }
}