using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Non-damaging animated sprite explosion (Explosion_WhitePurpleCloud, a 7-frame 98x98 strip)
    /// that plays once over the Moonfury bomb blast, LAYERED ON TOP of the existing
    /// BlackKnightMoonfuryBlast procedural shader rather than replacing it. Spawned from
    /// EnemyMoonfuryBomb.OnKill, which both Black Knight and Great Black Knight share (same bomb
    /// projectile for both), so this covers both knights from one spawn point.
    /// </summary>
    class BlackKnightBombExplosionSprite : ModProjectile
    {
        // 7 frames * 7 ticks/frame = 42t to play through, then holds on the last frame for the
        // remainder of timeLeft — close to the 50t shader burst duration (EnemyVFX.cs) so the two
        // finish together rather than the sprite outlasting or cutting off the shader.
        const int FrameTicks = 7;
        const int HoldTicks = 8;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Explosion_WhitePurpleCloud";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Main.projFrames[Type] * FrameTicks + HoldTicks;
            // Native frame is 98x98; scale up so the sprite comfortably fills the blast area.
            Projectile.scale = 1.25f;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.frame >= Main.projFrames[Projectile.type] - 1)
            {
                // Held on the last frame; timeLeft (sized to cover play-through plus HoldTicks) kills it.
                return;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameTicks)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);

            // Origin at exact center of the frame (49, 49)
            Vector2 origin = new Vector2(tex.Width / 2f, frameHeight / 2f);

            // Draw centered dead-center on the explosion center.
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                tex,
                drawPos,
                src,
                Color.White * 0.9f,
                0f,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
