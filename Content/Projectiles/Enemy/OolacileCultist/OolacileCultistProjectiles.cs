using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileCultist
{
    /// <summary>
    /// The Oolacile Cultist's one pyromancy projectile — every formation, Recoil Flare and the Unbinding
    /// orbs are this class with a different speed / delay / mode.
    ///   ai[0] = formation delay: the bolt sits at its spawn point flickering and growing (no damage, no movement)
    ///   ai[1] = mode (see the Mode* constants)
    ///   ai[2] = target player index, read only by ModeReaimOnRelease
    /// Launched velocity is the TOP speed; the bolt starts at 15% of it and eases up over 20 ticks.
    /// </summary>
    public class OolacileFireBolt : ModProjectile
    {
        public const float ModeAimed = 0f;
        public const float ModeReaimOnRelease = 1f;

        private const int FrameCount = 5;
        private const int TicksPerFrame = 5;
        private const int AccelerationTicks = 20;
        private const float StartSpeedFraction = 0.15f;
        private const float SpinPerTick = 0.08f;
        private const int FadeOutTicks = 12;
        private const int LifetimeTicks = 150;

        // RedFireBolt.png is 30x150: five 30x30 frames of a round fireball, so no edge needs to lead.

        private float ReleasedAge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float TopSpeed
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private bool HitTile
        {
            get => Projectile.localAI[2] > 0f;
            set => Projectile.localAI[2] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false; // switched on at release so a delayed bolt can't die while forming
            Projectile.penetrate = 1;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override bool ShouldUpdatePosition() => Projectile.ai[0] <= 0f;

        public override bool CanHitPlayer(Player target) => Projectile.ai[0] <= 0f;

        public override void AI()
        {
            if (TopSpeed <= 0f)
            {
                TopSpeed = Projectile.velocity.Length();
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            int spinDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation += SpinPerTick * spinDirection;
            Lighting.AddLight(Projectile.Center, 0.55f, 0.18f, 0.08f);

            // Forming: hold in place and flicker. The lifetime is frozen so a long formation still gets
            // its full flight time after release.
            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[0]--;
                Projectile.timeLeft = LifetimeTicks;
                Projectile.alpha = 90 + (int)(Math.Sin(Projectile.ai[0] * 0.7f) * 60f);
                SpawnFormingDust();

                if (Projectile.ai[0] <= 0f)
                {
                    ReleaseBolt();
                }
                return;
            }

            // 20-tick ease-out ramp from 15% to full speed. Point-blank shots cover ~40px in their first
            // 10 ticks instead of ~60, which is what keeps Recoil Flare rollable.
            ReleasedAge++;
            float rampProgress = MathHelper.Clamp(ReleasedAge / AccelerationTicks, 0f, 1f);
            float easedRamp = 1f - (1f - rampProgress) * (1f - rampProgress);
            float speedFraction = MathHelper.Lerp(StartSpeedFraction, 1f, easedRamp);
            Vector2 travelDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = travelDirection * TopSpeed * speedFraction;
            Projectile.tileCollide = true;

            // Timeout: fade instead of vanishing at full opacity.
            if (Projectile.timeLeft <= FadeOutTicks)
            {
                float fadeProgress = 1f - Projectile.timeLeft / (float)FadeOutTicks;
                Projectile.alpha = (int)(255f * fadeProgress);
            }
            else
            {
                Projectile.alpha = 0;
            }

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Torch, 0f, 0f, 100, default, Main.rand.NextFloat(0.9f, 1.3f));
                trail.noGravity = true;
                trail.velocity = -Projectile.velocity * 0.15f;
            }
        }

        private void ReleaseBolt()
        {
            Projectile.alpha = 0;
            bool reaim = Projectile.ai[1] == ModeReaimOnRelease;
            int targetIndex = (int)Projectile.ai[2];

            // Ember Chain: each bolt re-aims at its own release, so the player has to keep moving rather
            // than sidestep once. Every peer runs this off the synced player position.
            if (reaim && targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                if (target.active && !target.dead)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = toTarget * TopSpeed;
                    Projectile.netUpdate = true;
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = 0.2f, PitchVariance = 0.15f }, Projectile.Center);
            }
        }

        private void SpawnFormingDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            // Motes converge on the spawn point in the sprite's red/orange.
            Vector2 offset = Main.rand.NextVector2CircularEdge(18f, 18f);
            Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch,
                -offset * 0.09f, 100, default, Main.rand.NextFloat(0.8f, 1.2f));
            mote.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            HitTile = true;
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle source = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = source.Size() * 0.5f;
            Color color = Color.White * (1f - Projectile.alpha / 255f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, color,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        // Every death path (tile, player, timeout) lands here, so none of them silently vanish.
        public override void OnKill(int timeLeft)
        {
            bool timedOut = timeLeft <= 0;

            if (!Main.dedServ)
            {
                int burstCount = timedOut ? 10 : 25;
                for (int i = 0; i < burstCount; i++)
                {
                    int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.CrimsonTorch;
                    Dust burst = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                        dustType, 0f, 0f, 80, default, Main.rand.NextFloat(1f, 1.6f));
                    burst.noGravity = true;
                    burst.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                }

                if (!timedOut)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.2f, PitchVariance = 0.15f }, Projectile.Center);
                }
            }

            // Every bolt that hits a tile leaves a fire patch on the floor below (formations, Recoil Flare, Unbinding
            // orbs alike). Owner-only so exactly one peer (the server, or
            // the single-player client) spawns it.
            bool leavesPatch = HitTile;
            if (leavesPatch && Projectile.owner == Main.myPlayer)
            {
                int patchDamage = Math.Max(1, Projectile.damage / 2);
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<OolacileFirePatch>(), patchDamage, 0f, Projectile.owner);
            }
        }
    }

    /// <summary>
    /// Short-lived ground fire left wherever an OolacileFireBolt hits a tile. Dust-only (no sprite): it snaps down onto the
    /// floor below where the bolt died, shows its flames for 10 ticks before it can hurt, then burns for
    /// the rest of its 60-tick life. Deliberately dies silently if there is no floor within 4 tiles.
    /// </summary>
    public class OolacileFirePatch : ModProjectile
    {
        private const int LifetimeTicks = 60;
        private const int ArmDelayTicks = 10;
        private const int GroundSearchTiles = 4;
        private const int BurnDebuffTicks = 120;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private float Age
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.hide = true;
        }

        public override bool CanHitPlayer(Player target) => Age >= ArmDelayTicks;

        public override void AI()
        {
            if (Age == 0f)
            {
                bool grounded = SnapToGround();
                if (!grounded)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Age++;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.05f);

            if (!Main.dedServ)
            {
                // Fade the flames in over the arm delay and out over the last 15 ticks.
                float fadeIn = MathHelper.Clamp(Age / ArmDelayTicks, 0f, 1f);
                float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 15f, 0f, 1f);
                float intensity = Math.Min(fadeIn, fadeOut);

                if (Main.rand.NextFloat() < 0.35f + 0.5f * intensity)
                {
                    int dustType = Main.rand.NextBool(3) ? DustID.CrimsonTorch : DustID.Torch;
                    Dust flame = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                        dustType, 0f, 0f, 90, default, Main.rand.NextFloat(1f, 1.6f) * (0.6f + 0.4f * intensity));
                    flame.noGravity = true;
                    flame.velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-2.2f, -0.8f));
                }
            }
        }

        // Scan down from the bolt's death point for the first solid (or platform) tile top and sit on it.
        private bool SnapToGround()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int startTileY = (int)(Projectile.Center.Y / 16f);

            for (int i = 0; i <= GroundSearchTiles; i++)
            {
                int tileY = startTileY + i;
                if (!WorldGen.InWorld(tileX, tileY))
                {
                    return false;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool solidTop = tile.HasTile && !tile.IsActuated
                    && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (solidTop)
                {
                    Projectile.Bottom = new Vector2(Projectile.Center.X, tileY * 16f);
                    return true;
                }
            }

            return false;
        }

        // Runs on the hit player's machine.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, BurnDebuffTicks);
        }
    }
}
