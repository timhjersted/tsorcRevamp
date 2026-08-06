using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class GreatRedKnightUltrakillHand : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.InsanityShadowHostile;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 520;
        }

        public override void AI()
        {
            // Animated 4-frame hand sprite
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            // Slow down to a very slow drift after initial launch trajectory
            if (Projectile.timeLeft < 490)
            {
                Projectile.velocity *= 0.93f;
            }

            int index = (int)Projectile.ai[0];
            int triggerTick = 30 + index * 11;
            int currentAge = 520 - Projectile.timeLeft;

            // Spawn the red lightning beam telegraph at assigned staggered tick
            if (currentAge == triggerTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 aimDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    aimDir,
                    ModContent.ProjectileType<GreatRedKnightHandBeam>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer,
                    ai0: Projectile.whoAmI
                );
            }

            // After beam sequence completes (60t telegraph + 25t beam = 85t), fade out and die
            int dieTick = triggerTick + 85;
            if (currentAge >= dieTick)
            {
                Projectile.alpha += 15;
                if (Projectile.alpha >= 255)
                {
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[ProjectileID.InsanityShadowHostile].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
            Vector2 origin = src.Size() / 2f;
            Color color = lightColor * ((255 - Projectile.alpha) / 255f);

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                src,
                color,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
