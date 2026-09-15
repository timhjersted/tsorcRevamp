using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Attack spec — Abyss Shard / PuppetNPC.AbyssShardFire / AbyssShard.
    // Tell: 46t 2x2-pixelated abyss breach at the snapped tile surface while purple motes rise from it.
    // Birth: the shard starts FULLY buried (tip inside the surface tile) and rises 14t to full height;
    // sound, shake and a dust burst fire the moment it breaks the surface. Life: rise + 20t hold are
    // damaging. Death: 18t non-damaging sink all the way back under, then a 10t fade while buried.
    // Hitbox: the above-ground part of the drawn sprite, sliced per frame from the texture (Colliding).
    // Draw order: behind solid tiles (Projectile.hide + DrawBehind), which is what hides the buried length.
    // Multiplayer: spawn/damage server authoritative; shader phase derives from identity; dust is cosmetic.
    class AbyssShard : ModProjectile
    {
        const int PortalTicks     = 46;
        const int RiseTicks       = 14;
        const int HoldTicks       = 20;
        const int SinkTicks       = 18;
        const int BuriedFadeTicks = 10;
        const int RiseStart       = PortalTicks;
        const int HoldStart       = RiseStart + RiseTicks;
        const int SinkStart       = HoldStart + HoldTicks;
        const int BuriedFadeStart = SinkStart + SinkTicks;
        const int TotalTicks      = BuriedFadeStart + BuriedFadeTicks;
        // Buried depth = the sprite's full length plus this, so the tip sits inside the surface tile
        // rather than flush with its top edge (flush reads as a sliver already poking out).
        const float BuryMargin = 8f;
        static readonly Vector2 PortalSize = new(80f, 52f);

        // Per-frame hitbox profile, built once from the texture: [frame][band] = (left, right) pixel
        // offsets from the shard's centreline, one band per HitboxBandLength px of shard length.
        const int HitboxBandLength = 10;
        // Pixels at or below this alpha are anti-aliasing fringe, not blade.
        const byte HitboxAlphaThreshold = 40;
        static Point[][] _hitboxBands;
        static int _hitboxShardLength;

        bool _grounded;
        bool _popped;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssShard";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            // Spawn/ground-snap footprint only. The real hitbox is the drawn shard's shape, in Colliding.
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = TotalTicks;
            // REQUIRED for DrawBehind below to mean anything. Main.CacheProjDraws calls DrawBehind for
            // EVERY active projectile, but Main.DrawProjectiles only skips hidden ones - so without this
            // the shard drew twice: once correctly behind the tiles, then again on top of them.
            Projectile.hide = true;
        }

        public override void AI()
        {
            if (!_grounded)
            {
                SnapToGround();
                _grounded = true;

                // Sprite variant from identity, not Main.rand in OnSpawn: OnSpawn only runs on the machine
                // that called NewProjectile (the server), and frame isn't in the projectile sync packet, so
                // every client saw variant 0. identity IS synced, so all machines pick the same variant.
                Projectile.frame = Projectile.identity % Main.projFrames[Type];
            }

            int elapsed = TotalTicks - Projectile.timeLeft;

            if (elapsed < RiseStart)
            {
                Projectile.alpha = 255;
                if (!Main.dedServ && elapsed % 4 == 0)
                {
                    float telegraphProgress = elapsed / (float)PortalTicks;
                    SpawnSurfaceDust(true, telegraphProgress,
                        1 + (int)(telegraphProgress * 2f));
                }
                return;
            }

            if (!_popped)
            {
                _popped = true;
                Pop();
            }

            int sinceRise = elapsed - RiseStart;
            if (elapsed < BuriedFadeStart)
            {
                // Fully opaque while any part of it can be above ground.
                Projectile.alpha = 0;
                Lighting.AddLight(Projectile.Center, new Color(138, 40, 214).ToVector3() * 0.82f);

                if (!Main.dedServ && sinceRise % 2 == 0)
                {
                    float remaining = 1f - sinceRise / (float)(BuriedFadeStart - RiseStart);
                    SpawnSurfaceDust(false, remaining, 2);
                }
            }
            else
            {
                // Only fades once fully buried; the tiles already hide it, so this just covers spots
                // with no solid tile under the surface (a platform or a ledge edge).
                float fade = MathHelper.Clamp((elapsed - BuriedFadeStart) / (float)BuriedFadeTicks, 0f, 1f);
                Projectile.alpha = (int)(fade * 255f);
            }
        }

        // Deerclops finds ground by walking the target's column; we don't have easy access to a
        // player reference here, so just probe straight down (then up as a fallback) from the
        // spawn point - good enough since the boss side already spawns roughly at ground height.
        void SnapToGround()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Center.Y / 16f);
            int foundY = -1;

            for (int i = 0; i < 60; i++)
            {
                int y = tileY + i;
                if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                {
                    foundY = y;
                    break;
                }
            }

            if (foundY < 0)
            {
                for (int i = 1; i < 60; i++)
                {
                    int y = tileY - i;
                    if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                    {
                        foundY = y;
                        break;
                    }
                }
            }

            if (foundY >= 0)
            {
                Projectile.Bottom = new Vector2(tileX * 16f + 8f, foundY * 16f);
            }
        }

        void SpawnSurfaceDust(bool upward, float intensity, int count)
        {
            Vector2 surface = Projectile.Bottom - new Vector2(0f, 2f);
            float direction = upward ? -1f : 1f;
            for (int i = 0; i < count; i++)
            {
                bool bright = Main.rand.NextBool(3);
                int dustType = bright ? DustID.PurpleTorch : DustID.ShadowbeamStaff;
                Color tint = bright ? new Color(194, 62, 255) : new Color(34, 5, 62);
                float speed = upward
                    ? Main.rand.NextFloat(0.65f, 1.65f + intensity * 0.45f)
                    : Main.rand.NextFloat(1.4f, 3.5f);
                Vector2 velocity = new(Main.rand.NextFloat(-0.55f, 0.55f), direction * speed);
                float scale = bright ? Main.rand.NextFloat(0.46f, 0.76f) : Main.rand.NextFloat(0.54f, 0.86f);
                Dust dust = Dust.NewDustPerfect(
                    surface + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-2f, 2f)),
                    dustType, velocity, bright ? 80 : 145, tint, scale);
                dust.noGravity = true;
                dust.noLight = !bright;
                if (upward && !bright)
                {
                    // fadeIn is a grow-to-scale target. Using the post-jitter scale guarantees this
                    // dark background layer actually billows before it naturally shrinks away.
                    dust.fadeIn = dust.scale + Main.rand.NextFloat(0.22f, 0.38f);
                }
            }
        }

        /// <summary>The shard is buried in terrain at both ends of its lifecycle, so it belongs in
        /// the behind-NPCs-and-tiles pass. Solid blocks then occlude the submerged portion naturally.
        /// Only takes effect because SetDefaults sets Projectile.hide.</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            // Frames point right and are drawn rotated upward, so the frame WIDTH (200px) is the shard's height.
            float buryDepth = source.Width * Projectile.scale + BuryMargin;

            float raised = GetRaised(elapsed);

            if (elapsed >= RiseStart)
            {
                Vector2 shardBottom = Projectile.Bottom + new Vector2(0f, buryDepth * (1f - raised));
                Color shardColor = GetAlpha(lightColor) ?? lightColor;
                // Every authored frame points right; rotating all frames the same way makes every shard
                // rise upward. Origin is the frame's left-middle, i.e. the shard's base.
                Vector2 origin = new Vector2(0f, source.Height * 0.5f);
                Main.EntitySpriteDraw(texture, shardBottom - Main.screenPosition, source, shardColor,
                    -MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // The breach draws AFTER the shard: its shader restarts the spritebatch with LinearClamp, which
            // would blur the pixel-art shard if drawn first. It fades out across the rise instead of popping.
            if (elapsed < HoldStart)
            {
                float telegraph = MathHelper.Clamp(elapsed / (float)PortalTicks, 0f, 1f);
                float portalOpacity = 0.52f + telegraph * 0.38f;
                if (elapsed >= RiseStart)
                {
                    float riseProgress = (elapsed - RiseStart) / (float)RiseTicks;
                    portalOpacity *= 1f - riseProgress;
                }

                Vector2 portalCenter = Projectile.Bottom - new Vector2(0f, PortalSize.Y * 0.5f - 2f);
                float phase = (Projectile.identity * 0.173f) % 1f;
                ArtoriasVFX.DrawAbyssShardPortal(portalCenter, PortalSize, telegraph, portalOpacity, phase);
            }
            return false;
        }

        /// <summary>1 = full height, 0 = fully buried. Rise and sink both ease, so it slows into and out of
        /// the hold. Shared by PreDraw and Colliding so the hitbox moves exactly with the drawn shard.</summary>
        float GetRaised(int elapsed)
        {
            if (elapsed >= RiseStart && elapsed < HoldStart)
            {
                float rise = (elapsed - RiseStart) / (float)RiseTicks;
                return MathHelper.SmoothStep(0f, 1f, rise);
            }

            if (elapsed >= HoldStart && elapsed < SinkStart)
            {
                return 1f;
            }

            if (elapsed >= SinkStart && elapsed < BuriedFadeStart)
            {
                float sink = (elapsed - SinkStart) / (float)SinkTicks;
                return 1f - MathHelper.SmoothStep(0f, 1f, sink);
            }

            return 0f;
        }

        /// <summary>The hitbox is the ABOVE-GROUND part of the drawn shard: a stack of HitboxBandLength
        /// slices, each as wide as that slice's opaque pixels, so it tapers, leans and rises/sinks with the
        /// sprite. WHEN it can hit is still CanHitPlayer's call.</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Hostile hits are resolved on the client of the player being hit (Projectile.Damage), so the
            // server never needs this - and it has no texture to read.
            if (Main.dedServ)
            {
                return null;
            }

            if (_hitboxBands == null)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
                _hitboxBands = BuildHitboxBands(texture, Main.projFrames[Type]);
                _hitboxShardLength = texture.Width;
            }

            int elapsed = TotalTicks - Projectile.timeLeft;
            float raised = GetRaised(elapsed);
            float scale = Projectile.scale;
            float buryDepth = _hitboxShardLength * scale + BuryMargin;
            float surfaceY = Projectile.Bottom.Y;
            // Same maths as PreDraw: the base is buryDepth under the surface when buried, on it at full height.
            float baseY = surfaceY + buryDepth * (1f - raised);
            float bandLength = HitboxBandLength * scale;
            Point[] frameBands = _hitboxBands[Projectile.frame];

            for (int band = 0; band < frameBands.Length; band++)
            {
                Point extent = frameBands[band];
                int bandWidth = extent.Y - extent.X;
                if (bandWidth <= 0)
                {
                    continue;
                }

                // Band k spans k to k+1 band lengths above the base. The part below the surface is inside
                // the ground where no player can stand, so each slice is clipped to the surface.
                float bandBottom = baseY - band * bandLength;
                float bandTop = bandBottom - bandLength;
                if (bandTop >= surfaceY)
                {
                    continue;
                }

                float visibleBottom = System.Math.Min(bandBottom, surfaceY);
                int sliceHeight = (int)System.Math.Ceiling(visibleBottom - bandTop);
                Rectangle slice = new Rectangle(
                    (int)(Projectile.Bottom.X + extent.X * scale),
                    (int)bandTop,
                    (int)(bandWidth * scale),
                    sliceHeight);
                if (slice.Intersects(targetHitbox))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reads each frame's opaque pixels into width bands. Built from the texture itself so
        /// redrawn art can never leave a stale hardcoded hitbox behind. Frames point right and draw rotated
        /// upward about their left-middle, so texture x = height above the base and (texture y - half the
        /// frame height) = horizontal offset from the centreline. Empty bands stay (0,0), i.e. zero width.</summary>
        static Point[][] BuildHitboxBands(Texture2D texture, int frameCount)
        {
            int frameHeight = texture.Height / frameCount;
            int halfFrameHeight = frameHeight / 2;
            int bandCount = (texture.Width + HitboxBandLength - 1) / HitboxBandLength;
            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            Point[][] bands = new Point[frameCount][];
            for (int frame = 0; frame < frameCount; frame++)
            {
                bands[frame] = new Point[bandCount];

                for (int band = 0; band < bandCount; band++)
                {
                    int left = int.MaxValue;
                    int right = int.MinValue;
                    int bandStartX = band * HitboxBandLength;
                    int bandEndX = System.Math.Min(texture.Width, bandStartX + HitboxBandLength);

                    for (int x = bandStartX; x < bandEndX; x++)
                    {
                        for (int y = 0; y < frameHeight; y++)
                        {
                            int pixelIndex = (frame * frameHeight + y) * texture.Width + x;
                            if (pixels[pixelIndex].A <= HitboxAlphaThreshold)
                            {
                                continue;
                            }

                            // A pixel at row y covers horizontal offsets [y - half, y - half + 1).
                            int offset = y - halfFrameHeight;
                            left = System.Math.Min(left, offset);
                            right = System.Math.Max(right, offset + 1);
                        }
                    }

                    if (right > left)
                    {
                        bands[frame][band] = new Point(left, right);
                    }
                }
            }

            return bands;
        }

        public override void Unload()
        {
            _hitboxBands = null;
        }

        /// <summary>Fires as the shard's tip breaks the surface (start of the rise).</summary>
        void Pop()
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, strength: 2.5f, frames: 8);

            if (Main.dedServ)
            {
                return;
            }

            SpawnSurfaceDust(false, 1f, 14);
        }

        // Damaging from the moment the tip breaks the surface until the hold ends; the sink is harmless.
        public override bool CanHitPlayer(Player target)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            bool risen = elapsed >= RiseStart;
            bool beforeSink = elapsed < SinkStart;
            return risen && beforeSink;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false);
            target.AddBuff(BuffID.Bleeding, 16 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 16 * 60, false);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 255 - Projectile.alpha);
        }
    }
}
