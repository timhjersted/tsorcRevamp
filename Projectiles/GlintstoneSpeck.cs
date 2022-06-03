using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class GlintstoneSpeck : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 6, 8), Color.White, Projectile.rotation, new Vector2(3, 4), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
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
            Projectile.rotation += 0.3f * (float)Projectile.direction;

            Projectile.ai[0]++;

            if (Projectile.ai[0] == 1)
            {
                Projectile.velocity.X += Main.rand.NextFloat(-3.5f, 3.5f);
                Projectile.velocity.Y += Main.rand.NextFloat(-5, -1);
            }

            if (Projectile.ai[0] == 15)
            {
                Projectile.velocity = Vector2.Zero;
            }

            if (Projectile.ai[0] > 125)
            {
                Vector2 vector = Projectile.position;
                float num34 = 300f;
                bool targetAcquired = false;

                for (int num4 = 0; num4 < 200; num4++)
                {
                    NPC nPC2 = Main.npc[num4];
                    if (nPC2.CanBeChasedBy(this))
                    {
                        float num5 = Vector2.Distance(nPC2.Center, Projectile.Center);
                        if (num5 < num34 && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, nPC2.position, nPC2.width, nPC2.height))
                        {
                            num34 = num5;
                            vector = nPC2.Center;
                            targetAcquired = true;
                        }
                    }
                }

                float shotSpeed = 10f;
                Vector2 vector7 = vector - Projectile.Center;
                vector7.Normalize();
                vector7 *= shotSpeed;
                if (targetAcquired)
                {
                    Projectile.velocity = vector7;
                    Projectile.timeLeft = 10;
                }
            }

        }

        public override void Kill(int timeLeft)
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
