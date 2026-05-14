using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class EphemeralThrowingAxeProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.width = 26;
            Projectile.height = 60;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI()
        {
            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.MageNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.2f;
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
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 57;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            Projectile.active = false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/EphemeralThrowingAxeProj_Glowmask").Value;
            Vector2 origin = new Vector2(glowTexture.Width / 2f, glowTexture.Height / 2f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 3; i++) 
            {
                Vector2 offset = Projectile.velocity * -i * 0.5f; 
                float alpha = 0.2f * (1f - (i / 6f)); 
                Main.EntitySpriteDraw(
                    glowTexture,
                    drawPosition + offset, 
                    null,
                    Color.White * alpha, 
                    Projectile.rotation,
                    origin,
                    1f, 
                    SpriteEffects.None,
                    0
                );
            }

            Main.EntitySpriteDraw(
                glowTexture,
                drawPosition,
                null,
                Color.White * 0.2f, 
                Projectile.rotation,
                origin,
                1f, 
                SpriteEffects.None,
                0
            );
        }
    }
}