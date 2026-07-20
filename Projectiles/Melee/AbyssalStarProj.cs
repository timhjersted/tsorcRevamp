using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Melee
{
    class AbyssalStarProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = 3;
            Projectile.timeLeft = 2300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = 6;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(10f);
            if (Projectile.timeLeft < 2200f)
            {
                Projectile.tileCollide = false;
                Projectile.velocity = (Projectile.velocity + Projectile.DirectionTo(Main.player[Projectile.owner].Center)) * 0.98f;
                if (Main.player[Projectile.owner].Hitbox.Intersects(Projectile.Hitbox))
                {
                    Projectile.Kill();
                }
            }
            int dust1 = Dust.NewDust(Projectile.Center - new Vector2(6, 16), 1, 1, 223, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust1].noGravity = true;
            Main.dust[dust1].velocity *= 0.2f;
            int dust2 = Dust.NewDust(Projectile.Center + new Vector2(6, 9), 1, 1, 223, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust2].noGravity = true;
            Main.dust[dust2].velocity *= 0.2f;
            Projectile.localAI[0] = 0;
            Lighting.AddLight(Projectile.Center, 0.8f, 0.1f, 0.7f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = 0f - oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = 0f - oldVelocity.Y;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
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

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 7; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 1.2f;  
                float alpha = 0.55f * (1f - (i / 6f));  

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
        
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60);
        }
    }
}