using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Throwing
{
    class EphemeralThrowingSpear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 19;
            Projectile.height = 19;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.height = 14;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.scale = 0.9f;
            Projectile.tileCollide = false;
            Projectile.width = 14;
            DrawOffsetX = -10;
        }
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.ai[0] >= 15f)
            {
                Projectile.ai[0] = 15f;
                Projectile.velocity.Y += 0.1f;
            }
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);

            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 245, 0, 0, 50, default, 1.2f);
            Main.dust[dust].noGravity = true;
        }
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 projPosition = new Vector2(Projectile.position.X, Projectile.position.Y);
                Dust.NewDust(projPosition, Projectile.width, Projectile.height, 245, 0f, 0f, 0, default, 1f);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 245, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 50, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 106, 34, 106), Color.White, Projectile.rotation, new Vector2(16, 16), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
