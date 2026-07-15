using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile throwing-star fired by PuppetNPC subclasses.
    /// Reuses the AbyssalStarProj sprite.
    /// hostile=true / friendly=false so it damages players, not NPCs.
    /// No homing — flies straight and bounces off tiles.
    /// </summary>
    public class PuppetThrowingStar : ModProjectile
    {
        // Reuse the player-version sprite; no need to duplicate the asset.
        public override string Texture => "tsorcRevamp/Projectiles/Melee/AbyssalStarProj";

        public override void SetDefaults()
        {
            Projectile.width      = 28;
            Projectile.height     = 28;
            Projectile.hostile    = true;
            Projectile.friendly   = false;
            Projectile.penetrate  = 1;
            Projectile.timeLeft   = 180;
            Projectile.tileCollide = true;
            Projectile.scale      = 0.5f;
        }

        public override void AI()
        {
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(12f);

            // Dust trail
            int d = Dust.NewDust(Projectile.Center - new Vector2(6, 6), 1, 1, 223,
                Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 0, default, 1.1f);
            Main.dust[d].noGravity = true;

            Lighting.AddLight(Projectile.Center, 0.6f, 0.05f, 0.5f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * 0.6f;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y * 0.6f;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(tex,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White,
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None, 0);
            return false;
        }
    }
}
