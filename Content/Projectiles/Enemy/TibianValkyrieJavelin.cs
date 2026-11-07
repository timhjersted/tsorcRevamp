using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Enemy;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Gilded Javelin: the Tibian Valkyrie's own held spear, thrown. Same art and draw scale as the spear in her hand,
    /// so the release reads as the weapon leaving the fist. Tip leads (the art's head sits top-left, a -135 degree shaft).
    /// Life: 10px/t launch, 0.12px/t^2 gravity, one gold mote per 4t. Death: a tile sticks it (harmless, fades 40t);
    /// a player hit bursts ~20 gold/grey motes; the 150t lifetime fades over its last 12t.
    /// ai[0] = 0 flying, 1 stuck. Spawned by the server only.
    /// </summary>
    public class TibianValkyrieJavelin : ModProjectile
    {
        public const float LaunchSpeed = 10f;
        public const float Gravity = 0.12f;
        public const float DrawScale = 0.75f;
        const int LifeTicks = 150;
        const int FadeTicks = 12;
        public const int MaxFlightTicks = LifeTicks - FadeTicks;
        const int StuckFadeTicks = 40;

        // Measured from the 104x104 art: head (1, 1) -> butt (102, 102). The tip texel is pinned to Projectile.Center.
        static readonly Vector2 TipTexel = new Vector2(1.5f, 1.5f);
        // Art shaft angle (-135 degrees): adding 3pi/4 to the travel angle points the tip along the velocity.
        const float SpriteRotationOffset = 3f * MathHelper.PiOver4;

        bool Stuck => Projectile.ai[0] == 1f;

        public override string Texture => EnemyGildedIronSpear.ArtPath;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeTicks;
        }

        public override void AI()
        {
            if (Stuck)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            Projectile.velocity.Y += Gravity;
            Projectile.rotation = Projectile.velocity.ToRotation() + SpriteRotationOffset;

            if (!Main.dedServ && Projectile.timeLeft % 4 == 0)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 80, default, 0.9f);
                mote.noGravity = true;
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            return !Stuck;
        }

        // A wall or floor catches the point: the spear stays where it landed, harmless, and fades over 40t.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Stuck)
            {
                return false;
            }

            Projectile.ai[0] = 1f;
            Projectile.rotation = oldVelocity.ToRotation() + SpriteRotationOffset;
            Projectile.position += oldVelocity * 0.6f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = StuckFadeTicks;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
                for (int i = 0; i < 8; i++)
                {
                    Dust.NewDust(Projectile.Center - new Vector2(6f), 12, 12, DustID.Stone, 0f, 0f, 0, default, 0.9f);
                }
            }

            return false;
        }

        // Hostile projectiles pierce players by default; the hit ends the throw (tsorcGlobalProjectile relays the kill in MP).
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A stuck spear or a timed-out flight has already faded; only a strike gets the burst.
            bool faded = Stuck || timeLeft <= 0;
            int moteCount = faded ? 4 : 20;
            for (int i = 0; i < moteCount; i++)
            {
                int dustType = i % 2 == 0 ? DustID.GoldFlame : DustID.Silver;
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 60, default, 1.1f);
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Opacity: stuck spears fade across their 40t, flights fade over the last 12t of the lifetime.
            float opacity = 1f;
            if (Stuck)
            {
                opacity = Projectile.timeLeft / (float)StuckFadeTicks;
            }
            else if (Projectile.timeLeft < FadeTicks)
            {
                opacity = Projectile.timeLeft / (float)FadeTicks;
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(lightColor) * opacity;
            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, TipTexel, DrawScale, SpriteEffects.None, 0);
            return false;
        }

        /// <summary>
        /// Earliest reachable intercept at up to LaunchSpeed, including the remaining tell and target motion.
        /// Uses the projectile's exact gravity-before-movement steps; zero means unreachable before the flight fades.
        /// Selection, lock and release share this solve instead of falling back to a throw that cannot connect.
        /// </summary>
        public static Vector2 SolveLaunch(Vector2 start, Vector2 targetPosition, Vector2 targetVelocity, int releaseDelay, out Vector2 aimPoint)
        {
            float speedSquared = LaunchSpeed * LaunchSpeed;
            aimPoint = targetPosition;

            for (int flightTicks = 1; flightTicks <= MaxFlightTicks; flightTicks++)
            {
                int predictionTicks = releaseDelay + flightTicks;
                Vector2 predictedTarget = targetPosition + targetVelocity * predictionTicks;
                Vector2 gap = predictedTarget - start;
                float gravityDrop = Gravity * flightTicks * (flightTicks + 1f) * 0.5f;
                Vector2 velocity = new Vector2(gap.X / flightTicks, (gap.Y - gravityDrop) / flightTicks);
                if (velocity.LengthSquared() <= speedSquared)
                {
                    aimPoint = predictedTarget;
                    return velocity;
                }
            }

            return Vector2.Zero;
        }
    }
}
