using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;


namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class GaeBolgThrown : ModdedSpearProjectileThrown
    {
        public override int HitboxSize => 24;
        public override int ChargedShaderID => ItemID.StardustDye;

        public override float Light => 0.6f;
        public override int Penetrate => 4;
        public override float Scale => 1.1f;
        public override int TimeLeft => 240;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.Frostburn2, 210);
            }
        }

        public override void AIDust()
        {
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void PreKillDust()
        {
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 0, default, 1f);
            }
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 spearTipOffset = origin.RotatedBy(Projectile.rotation);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + spearTipOffset;

            for (int i = 1; i <= 4; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 1f;  
                float alpha = 0.35f * (1f - (i / 6f));  

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