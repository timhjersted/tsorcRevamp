using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Hellfire Volley's flame: a spinning Destined Death blaze that sheds embers as it flies. It bounces exactly once:
    /// the first solid tile it touches turns it toward the nearest player, and the next tile or player ends it.
    ///   ai[0] = mode. 0 = the rising fan, launched at the ceiling; it re-aims at the player with a small sideways offset
    ///           so the three don't stack on one pixel. 1 = the aimed cluster; it re-aims straight at the nearest player.
    ///   ai[1] = 1 once it has bounced.
    ///   ai[2] = mode 0 only: offset slot (-1, 0, +1) × AimOffsetStep px beside the player.
    /// The bounce aim uses no randomness, so every peer computes the same turn; the owner (server) also sends a netUpdate.
    /// No ceiling (open sky): a mode-0 flame that hasn't touched a tile after RiseFallbackTicks re-aims in mid-air
    /// instead, so it never sails off unseen. Deliberate.
    /// Sprite: DestinedDeathBlaze, 5 frames of 86x82, flame tongues point UP the frame; drawn centred and spun, so there
    /// is no leading edge.
    /// </summary>
    public class WitchkingHellfireFlame : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        public const float RiseSpeed = 8f;
        public const float AimedSpeed = 10f;
        private const float BounceSpeed = 9f;
        private const float AimOffsetStep = 20f;
        private const int RiseFallbackTicks = 45;
        private const int Lifetime = 240;
        private const int GrowTicks = 8;
        private const int FadeTicks = 15;
        private const int FrameCount = 5;
        private const int TicksPerFrame = 5;
        private const float DrawScale = 0.55f;
        private const float SpinSpeed = 0.28f;
        private const int DestinedDeathTicks = 600;
        private const int KillDustCount = 60;

        private bool RiseMode => Projectile.ai[0] < 0.5f;
        private bool HasBounced => Projectile.ai[1] > 0.5f;
        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.rotation += SpinSpeed;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            bool riseStalled = RiseMode && !HasBounced && Age >= RiseFallbackTicks;
            if (riseStalled)
            {
                Bounce();
            }

            Lighting.AddLight(Projectile.Center, 0.7f, 0.08f, 0.05f);

            if (Main.dedServ)
            {
                return;
            }

            // Shed flame: crimson and shadow embers thrown back off the spin, plus a dark smoke wisp now and then.
            for (int i = 0; i < 2; i++)
            {
                int dustType = DustID.RedTorch;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.Shadowflame;
                }
                Vector2 shed = -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1.2f, 1.2f);
                Dust ember = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), dustType, shed,
                    80, default, Main.rand.NextFloat(1.1f, 1.6f));
                ember.noGravity = true;
            }
            if (Main.rand.NextBool(4))
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center, DustID.Wraith, -Projectile.velocity * 0.1f, 120, Color.Black, 1.3f);
                smoke.noGravity = true;
            }
        }

        // First tile = the bounce; the next one ends it.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (HasBounced)
            {
                return true;
            }

            Bounce();
            return false;
        }

        // Turn toward the nearest player (rise mode: beside them by the offset slot) at BounceSpeed, with a burst where it
        // turned. Deterministic on every peer; the owner also pushes a netUpdate in case positions disagreed.
        private void Bounce()
        {
            Projectile.ai[1] = 1f;

            Player target = null;
            float nearest = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                {
                    continue;
                }
                float candidateDistance = candidate.DistanceSQ(Projectile.Center);
                if (candidateDistance < nearest)
                {
                    nearest = candidateDistance;
                    target = candidate;
                }
            }

            if (target != null)
            {
                Vector2 aimPoint = target.Center;
                if (RiseMode)
                {
                    aimPoint.X += Projectile.ai[2] * AimOffsetStep;
                }
                Vector2 aim = (aimPoint - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = aim * BounceSpeed;
            }
            else
            {
                Projectile.velocity = -Projectile.velocity;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);
                for (int i = 0; i < 16; i++)
                {
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1.5f);
                    spark.noGravity = true;
                }
            }
        }

        // Runs on the hit player's machine; the mod relays the kill to the server (RequestKillHitProjectile).
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), DestinedDeathTicks);
            Projectile.Kill();
        }

        // Every ending (tile, player, timeout) bursts in Destined Death crimson and black.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center);
            for (int i = 0; i < KillDustCount; i++)
            {
                int dustType = DustID.RedTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.Shadowflame;
                }
                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(5f, 5f), 60, default, Main.rand.NextFloat(1.2f, 1.9f));
                burst.noGravity = true;
            }
            for (int i = 0; i < 8; i++)
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center, DustID.Wraith, Main.rand.NextVector2Circular(2f, 2f), 120, Color.Black, 1.6f);
                smoke.noGravity = true;
            }
        }

        // Grows in over GrowTicks as it leaves the tip, fades over the last FadeTicks of its lifetime.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;

            float grow = MathHelper.Clamp(Age / (float)GrowTicks, 0.3f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White * fade,
                Projectile.rotation, origin, DrawScale * grow, SpriteEffects.None, 0f);
            return false;
        }
    }
}
