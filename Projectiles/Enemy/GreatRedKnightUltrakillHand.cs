using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class GreatRedKnightUltrakillHand : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.InsanityShadowHostile;

        /// <summary>Ticks each hand lives. Has to outlast the LAST hand's staggered bolt:
        /// index 14 fires at <see cref="TriggerTick"/> = 30 + 14*41 = 604, plus the bolt tail and
        /// the alpha fade-out.</summary>
        public const int Lifetime = 720;

        /// <summary>Ticks between consecutive hands firing their bolt (was 11).</summary>
        public const int StaggerTicks = 41;

        // The bolt is now RedKnightLightningLane, the kit's shader lightning, instead of the old
        // GenericLaser-based GreatRedKnightHandBeam. 30t telegraph rather than Stage D's 40t or
        // Stormbreaker's 46t: those are sky columns that appear out of nowhere, whereas the hand
        // itself has already been drifting on screen as the telegraph, and fifteen staggered
        // single shots need to read snappy rather than ponderous. Active window matches the rest
        // of the kit (12t). Length 900 > Stage D's 640 because this is one aimed shot that has to
        // reach the player from wherever the hand drifted to, not a fixed ground-anchored pattern.
        public const int BoltTelegraphTicks = 30;
        public const int BoltActiveTicks = 12;
        public const float BoltLength = 900f;

        public override void SetStaticDefaults()
        {
            // Mirror the source projectile's live metadata instead of a numeric-ID-era hardcode.
            // In the current Terraria build InsanityShadowHostile is ID 965 and has one frame;
            // treating it as the old ID 734's eight-frame sheet sliced the hand into thin strips.
            Main.projFrames[Type] = Main.projFrames[ProjectileID.InsanityShadowHostile];
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            int sourceFrameCount = System.Math.Max(1,
                Main.projFrames[ProjectileID.InsanityShadowHostile]);
            if (sourceFrameCount > 1)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= 6)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame = (Projectile.frame + 1) % sourceFrameCount;
                }
            }
            else
            {
                Projectile.frame = 0;
            }

            int currentAge = Lifetime - Projectile.timeLeft;

            // Slow down to a very slow drift after initial launch trajectory
            if (currentAge > 30)
            {
                Projectile.velocity *= 0.93f;
            }

            int index = (int)Projectile.ai[0];
            int triggerTick = 30 + index * StaggerTicks;

            // Fire the shader lightning lane at this hand's assigned staggered tick. The lane is a
            // STATIC line (ShouldUpdatePosition() => false), so the aim is baked in here rather
            // than tracked like the old beam did.
            if (currentAge == triggerTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 aimDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    aimDir,
                    ModContent.ProjectileType<RedKnightLightningLane>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer,
                    ai0: BoltTelegraphTicks,
                    ai1: BoltActiveTicks,
                    ai2: BoltLength
                );
            }

            // Linger until the lane has finished its telegraph + strike + 12t fade, then fade out.
            int dieTick = triggerTick + BoltTelegraphTicks + BoltActiveTicks + 18;
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
            int sourceFrameCount = System.Math.Max(1,
                Main.projFrames[ProjectileID.InsanityShadowHostile]);
            int sourceFrame = Utils.Clamp(Projectile.frame, 0, sourceFrameCount - 1);
            int frameHeight = tex.Height / sourceFrameCount;
            Rectangle src = new Rectangle(0, sourceFrame * frameHeight, tex.Width, frameHeight);
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
