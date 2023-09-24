using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ToxicCatDetonator : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 145;

            //These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -9;
            DrawOriginOffsetY = -9;
        }

        public int toxiccatdetotimer;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 32, 32, 32), Color.White, Projectile.rotation, new Vector2(16, 16), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void AI()
        {
            //Change these two variables to affect the rotation of your projectile
            float rotationsPerSecond = 1.2f;
            bool rotateClockwise = true;
            //The rotation is set here
            Projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 6f);

            //ANIMATION
            if (toxiccatdetotimer > 40)
            {
                toxiccatdetotimer = 0;
            }
            if (++Projectile.frameCounter >= 10) //ticks spent on each frame
            {
                Projectile.frameCounter = 0;

                if (++Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }

            if (Projectile.localAI[0] == 0f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                Projectile.localAI[0] += 1f;
            }

            Lighting.AddLight(Projectile.position, 0.325f, 0.59f, 0.17f);

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft >= 139 && Projectile.timeLeft < 142)
            {
                for (int d = 0; d < 2; d++)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X - 7, Projectile.position.Y - 7), 24, 24, 75, Projectile.velocity.X * .5f, Projectile.velocity.Y * .5f, 100, default(Color), .8f)];
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9 with { Volume = 0.8f });
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9 with { Volume = 0.8f });
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9 with { Volume = 0.4f });
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1.2f, Projectile.velocity.Y * 1.2f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
        }

    }
}
