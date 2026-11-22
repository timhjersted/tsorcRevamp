using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Tibian Amazon's thrown knife: the same EnemyThrowingKnife art and 1.1 draw scale as the knife in her hand,
    /// so the release reads as the held knife leaving the fist. EnemyThrowingKnife itself is shared with other enemies
    /// (aiStyle 1, pierces twice), so the Amazon gets her own lifecycle here without touching it.
    /// The art stands point-up, so the tip leads with rotation = travel angle + pi/2.
    /// Life: straight 11px/t, no gravity or homing, one pale mote per 4t. Death: a tile sticks it (harmless, fades
    /// 30t); a player hit bursts ~14 steel motes; the 70t lifetime (770px) fades over its last 12t.
    /// ai[0] = 0 flying, 1 stuck. Spawned by the server only.
    /// </summary>
    public class TibianAmazonKnife : ModProjectile
    {
        public const float Speed = 11f;
        public const float DrawScale = 1.1f;
        const int LifeTicks = 70;
        const int FadeTicks = 12;
        const int StuckFadeTicks = 30;
        const float SpriteRotationOffset = MathHelper.PiOver2;

        bool Stuck => Projectile.ai[0] == 1f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyThrowingKnife));

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
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

            Projectile.rotation = Projectile.velocity.ToRotation() + SpriteRotationOffset;

            if (!Main.dedServ && Projectile.timeLeft % 4 == 0)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Silver, -Projectile.velocity * 0.08f, 80, default, 0.8f);
                mote.noGravity = true;
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            return !Stuck;
        }

        // A wall or floor catches the point: the knife stays where it landed, harmless, and fades over 30t.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Stuck)
            {
                return false;
            }

            Projectile.ai[0] = 1f;
            Projectile.rotation = oldVelocity.ToRotation() + SpriteRotationOffset;
            Projectile.position += oldVelocity * 0.5f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = StuckFadeTicks;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = 0.5f }, Projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    Dust.NewDust(Projectile.Center - new Vector2(4f), 8, 8, DustID.Stone, 0f, 0f, 0, default, 0.8f);
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

            // A stuck knife or a timed-out flight has already faded; only a strike gets the burst.
            bool faded = Stuck || timeLeft <= 0;
            int moteCount = 14;
            if (faded)
            {
                moteCount = 3;
            }

            for (int i = 0; i < moteCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Silver, velocity, 60, default, 1f);
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Opacity: stuck knives fade across their 30t, flights fade over the last 12t of the lifetime.
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
            Vector2 origin = texture.Size() * 0.5f;
            Color color = Projectile.GetAlpha(lightColor) * opacity;
            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, DrawScale, SpriteEffects.None, 0);
            return false;
        }
    }
}
