using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class DivineBoltProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.height = 58;
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            Projectile.width = 14;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.aiStyle = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            Lighting.AddLight(Projectile.Center, 0.1f, 0.6f, 0.2f); 

            if (Main.rand.NextBool(6)) 
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,               
                    Projectile.width,                 
                    Projectile.height,                 
                    229,                               
                    Projectile.velocity.X * 0.2f,     
                    Projectile.velocity.Y * 0.2f,      
                    100,                               
                    default,                           
                    0.8f                              
                );

                dust.noGravity = true;                
                dust.velocity *= 0.5f;                
            }
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
    }
}
