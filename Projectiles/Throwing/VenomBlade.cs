using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Projectiles.Throwing
{
    class VenomBlade : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.height = 14;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
            Projectile.width = 14;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Poisoned, 0, 0, 50, default, 1.1f);
            Main.dust[dust].noGravity = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 5 * 60);

            if (Main.rand.NextFloat() < 0.20f)
            {
                target.AddBuff(BuffID.Venom, 4 * 60, false);
            }

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + Projectile.width, Projectile.position.Y + Projectile.height, 0, 0, ModContent.ProjectileType<VenomBladeField>(), Projectile.damage / 3, 1f, Projectile.owner);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Color alphalowered = Color.White;
            Texture2D textureGlow = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VenomBladeGlowmask];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);  
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(textureGlow, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), alphalowered, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Texture2D glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VenomBladeGlowmask];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 4; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 0.6f;  
                float alpha = 0.5f * (1f - (i / 6f));  

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

                Main.EntitySpriteDraw(
                    glowTexture,
                    drawPosition + offset,
                    null,
                    Color.White * alpha,  
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
