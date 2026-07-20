using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusThrown : ModdedSpearProjectileThrown
    {
        public override int HitboxSize => 30;
        public override int ChargedShaderID => ItemID.RedDye;

        public override int ExtraUpdates => 1;
        public override float Light => 0.7f;
        public override float Scale => 1.2f;
        public override int TimeLeft => 300;

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == 1)
            {
                for (int i = 0; i < 150; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitX);
                    float speed = Main.rand.NextFloat(5.5f, 19f);

                    int dust1 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemRuby, 0f, 0f, 70, default, 1.55f);
                    Main.dust[dust1].velocity = direction * speed;
                    Main.dust[dust1].noGravity = true;

                    int dust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Fireworks, 0f, 0f, 70, default, 1.95f);
                    Main.dust[dust2].velocity = direction * (speed * 1.2f);
                    Main.dust[dust2].noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

                Projectile.penetrate = 15;
                Vector2 oldCenter = Projectile.Center;
                Projectile.width = 320;
                Projectile.height = 320;
                Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                Projectile.damage /= 2;
                Projectile.Damage();
            }
        }

        public override void AIDust()
        {
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemRuby, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void PreKillDust()
        {
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Fireworks, 0, 0, 0, default, 1.4f);
            }
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Vector2 spearTipOffset = origin.RotatedBy(Projectile.rotation);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + spearTipOffset;

            for (int i = 1; i <= 5; i++)
            {
                Vector2 offset = Projectile.velocity * -i * 1.2f;
                float alpha = 0.4f * (1f - (i / 6f));

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