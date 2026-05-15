using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkFreezeBolt : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 0;
            Projectile.scale = 1.15f;
            Projectile.timeLeft = 400;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 10;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            if (Projectile.type == 96 && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            }

            Projectile.rotation += 0.3f * (float)Projectile.direction;
        }

        public override bool OnTileCollide(Vector2 CollideVel)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            Projectile.ai[0] += 1f;

            Projectile.velocity *= 0.5f;

            if (Projectile.ai[0] >= 3f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.Kill();
            }
            else
            {
                if (Projectile.velocity.Y > 4f)
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y * 0.8f;
                    }
                }
                else
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y;
                    }
                }
                if (Projectile.velocity.X != CollideVel.X)
                {
                    Projectile.velocity.X = -CollideVel.X;
                }
            }
            return false;
        }

        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 5; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 0.9f;  
                float alpha = 0.7f * (1f - (i / 6f));  

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    lightColor * alpha,  
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }
    }
}
