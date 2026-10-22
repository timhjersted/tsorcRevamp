using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // The four projectiles of Taurus Knight's kit, kept together because they only exist for him
    // (NPCs/Enemies/SuperHardMode/TaurusKnight.cs). Design: tsorcDocs/EnemyRevampPlans/TaurusKnight_Puppet_Proposal.md.

    /// <summary>
    /// Quake Slam's ground shockwave: one thin front travelling along the floor away from the impact. Spawned as a
    /// mirrored pair, so the two together cover 30 tiles to EACH side (8 px/t x 60t = 480px).
    /// A thin, low front on purpose: jumping it or rolling through it both work (the HeavyPounceShockwave idea, with
    /// the range Taurus needs). ai[0] = travel direction (-1 / +1).
    /// </summary>
    public class TaurusQuakeShockwave : ModProjectile
    {
        public const float TravelSpeed = 8f;
        public const int LifetimeTicks = 60;
        // The pair leaves from under the hammer head on the impact tick; 4 harmless ticks let the impact dust lead
        // the damage and stop the blade hit and the wave stacking on a player standing at the head.
        private const int ArmDelayTicks = 4;
        // Ground-riding limits, in pixels per tick of travel. A climb over 2 tiles is a wall and ends the front,
        // which is the only thing allowed to stop it short; a drop deeper than 5 tiles is a pit edge.
        private const float MaxClimbPixels = 32f;
        private const float MaxDropPixels = 80f;
        private const int StartHeight = 30;
        private const int EndHeight = 56;
        private const int FadeTicks = 14;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private float Direction => Projectile.ai[0] >= 0f ? 1f : -1f;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = StartHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ArmDelayTicks;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];

            // Ride the floor: find the surface under the NEXT position, then keep the front's bottom on it. Same
            // exposed-surface lookup as the puppet ground waves, so the front climbs stairs and follows slopes.
            float nextX = Projectile.Center.X + Direction * TravelSpeed;
            bool foundGround = PuppetGroundDustWave.TryFindGroundY(nextX, Projectile.Bottom.Y, out float groundY);
            if (!foundGround)
            {
                Projectile.Kill();
                return;
            }

            float verticalStep = groundY - Projectile.Bottom.Y;
            bool hitWall = verticalStep < -MaxClimbPixels;
            bool fellOffEdge = verticalStep > MaxDropPixels;
            if (hitWall || fellOffEdge)
            {
                Projectile.Kill();
                return;
            }

            // Grows taller as it travels, keeping its feet on the ground, so the jump window is earliest near Taurus.
            float growth = MathHelper.Clamp(age / LifetimeTicks, 0f, 1f);
            int height = (int)MathHelper.Lerp(StartHeight, EndHeight, growth);
            float bottomX = Projectile.Bottom.X;
            Projectile.height = height;
            Projectile.Bottom = new Vector2(bottomX, groundY);
            Projectile.velocity = new Vector2(Direction * TravelSpeed, 0f);

            if (Main.dedServ)
            {
                return;
            }

            // Sand-and-ember crest leaning away from the impact; it thins over the last FadeTicks instead of popping.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            int dustCount = (int)MathHelper.Lerp(1f, 5f, fade);
            for (int i = 0; i < dustCount; i++)
            {
                float rise = Main.rand.NextFloat(0f, Projectile.height);
                float rake = rise * 0.3f * Direction;
                Vector2 position = Projectile.Bottom + new Vector2(rake + Main.rand.NextFloat(-6f, 6f), -rise);
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(1.2f, 3f), -Main.rand.NextFloat(0.4f, 1.6f));
                Dust sand = Dust.NewDustPerfect(position, DustID.Sand, velocity, 90, new Color(214, 170, 110),
                    Main.rand.NextFloat(0.9f, 1.4f) * fade);
                sand.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 emberVelocity = new Vector2(Direction * Main.rand.NextFloat(1f, 2.5f), -Main.rand.NextFloat(1f, 3f));
                Dust ember = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-8f, 8f), -4f),
                    DustID.OrangeTorch, emberVelocity, 60, default, Main.rand.NextFloat(0.9f, 1.3f) * fade);
                ember.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust grit = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), 0f),
                    DustID.Stone, new Vector2(Direction * Main.rand.NextFloat(1.5f, 3.5f), -Main.rand.NextFloat(1f, 3f)),
                    120, default, Main.rand.NextFloat(0.8f, 1.2f) * fade);
                grit.noGravity = false;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A wall or pit ends the front early: break it up into a puff where it stopped rather than vanishing.
            for (int i = 0; i < 12; i++)
            {
                Dust sand = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-8f, 8f), -Main.rand.NextFloat(0f, 20f)),
                    DustID.Sand, Main.rand.NextVector2Circular(2f, 2f), 110, new Color(214, 170, 110), Main.rand.NextFloat(0.8f, 1.2f));
                sand.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    /// <summary>
    /// Taurus Knight's thrown Earth Trident. Flies on a lob solved by the thrower (gravity <see cref="Gravity"/>) to
    /// land past the player. Hitting a player bursts it with no cloud. Hitting the ground EMBEDS it: it shudders for
    /// <see cref="EmbedShudderTicks"/> (the tell), then crumbles and the server spawns a <see cref="TaurusSandCloud"/>.
    /// ai[0] = 0 flying / 1 embedded. ai[1] = 1 once it has been killed by touching a player.
    /// </summary>
    public class TaurusJavelin : ModProjectile
    {
        public const float Gravity = 0.1f;
        public const int EmbedShudderTicks = 30;
        private const int FlightLifetimeTicks = 240;
        private const float DrawScale = 0.8f;
        // The sprite is 16x108 with the tip at the top. The hitbox sits on the head, so the origin is the head too
        // (texel 8,14): the tip leads, the shaft trails, and ground contact is tip contact.
        private static readonly Vector2 HeadOrigin = new Vector2(8f, 14f);
        // How far the tip sinks into the ground on embedding, px.
        private const float EmbedDepth = 10f;
        private const float CloudHeightAboveGround = 40f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EarthTrident));

        private bool Embedded => Projectile.ai[0] >= 1f;
        private bool HitPlayer => Projectile.ai[1] >= 1f;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            // Penetrate -1: a hostile hit resolves on the victim's machine and never reaches the server's copy, so
            // the authority ends the javelin itself (the overlap check in AI) instead of trusting penetrate.
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FlightLifetimeTicks;
            Projectile.light = 0.3f;
        }

        public override bool? CanDamage() => !Embedded;

        public override void AI()
        {
            if (Embedded)
            {
                TickEmbedded();
                return;
            }

            Projectile.velocity.Y += Gravity;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.Dirt,
                    -Projectile.velocity * 0.1f, 80, new Color(150, 110, 70), Main.rand.NextFloat(0.8f, 1.1f));
                trail.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Authority-side contact: a player in the head's path ends it here with no cloud (see SetDefaults).
            // A rolling (immune) player is passed through, so a roll-through still leaves the cloud behind them.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool touchable = player.active && !player.dead && !player.immune;
                if (touchable && player.Hitbox.Intersects(Projectile.Hitbox))
                {
                    Projectile.ai[1] = 1f;
                    Projectile.netUpdate = true;
                    Projectile.Kill();
                    return;
                }
            }
        }

        private void TickEmbedded()
        {
            Projectile.velocity = Vector2.Zero;

            if (Main.dedServ)
            {
                return;
            }

            // The tell: sand trickles off the shaft faster as the burst nears.
            float progress = 1f - Projectile.timeLeft / (float)EmbedShudderTicks;
            int trickleChance = (int)MathHelper.Lerp(4f, 1f, progress);
            if (Main.rand.NextBool(Math.Max(1, trickleChance)))
            {
                Vector2 shaftDirection = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
                Vector2 alongShaft = -shaftDirection * Main.rand.NextFloat(0f, 70f);
                Dust sand = Dust.NewDustPerfect(Projectile.Center + alongShaft, DustID.Sand,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.2f, 1f)), 80,
                    new Color(214, 170, 110), Main.rand.NextFloat(0.8f, 1.2f));
                sand.noGravity = false;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Embedded)
            {
                return false;
            }

            // Embed: freeze on the tile, tip sunk a little way in, pointing the way it was travelling. Every machine
            // embeds from its own tile collision, so the state needs no extra sync.
            Projectile.ai[0] = 1f;
            Projectile.rotation = oldVelocity.ToRotation() + MathHelper.PiOver2;
            Projectile.position += oldVelocity.SafeNormalize(Vector2.UnitY) * EmbedDepth;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = EmbedShudderTicks;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            bool crumbled = Embedded && !HitPlayer;

            if (crumbled && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 cloudCenter = Projectile.Center - new Vector2(0f, CloudHeightAboveGround);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), cloudCenter, Vector2.Zero,
                    ModContent.ProjectileType<TaurusSandCloud>(), 0, 0f, Main.myPlayer);
            }

            if (Main.dedServ)
            {
                return;
            }

            if (crumbled)
            {
                // The trident crumbles away into the cloud it leaves.
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = -0.3f }, Projectile.Center);
                Vector2 shaftDirection = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
                for (int i = 0; i < 40; i++)
                {
                    Vector2 alongShaft = -shaftDirection * Main.rand.NextFloat(0f, 80f);
                    Dust sand = Dust.NewDustPerfect(Projectile.Center + alongShaft, DustID.Sand,
                        Main.rand.NextVector2Circular(2.5f, 2.5f) - new Vector2(0f, 1f), 70,
                        new Color(214, 170, 110), Main.rand.NextFloat(1f, 1.6f));
                    sand.noGravity = Main.rand.NextBool(2);
                }
                return;
            }

            // Player hit or a timed-out throw: an earthy burst, no cloud.
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 40; i++)
            {
                Dust dirt = Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, Main.rand.NextVector2Circular(4f, 4f),
                    60, new Color(150, 110, 70), Main.rand.NextFloat(0.9f, 1.4f));
                dirt.noGravity = Main.rand.NextBool(3);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

            // Shudder while embedded: a cosmetic jitter that grows toward the burst (client-local randomness).
            if (Embedded)
            {
                float progress = 1f - Projectile.timeLeft / (float)EmbedShudderTicks;
                drawPosition += Main.rand.NextVector2Circular(1f, 1f) * MathHelper.Lerp(0.5f, 2.5f, progress);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, HeadOrigin, DrawScale,
                SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>
    /// The sand cloud an embedded TaurusJavelin bursts into. Zero damage. A player touching it is blinded with
    /// <see cref="BuffID.Blackout"/> for <see cref="BlackoutTicks"/> (vanilla's LongerExpertDebuff doubles it in Expert,
    /// 2.5x in Master; Wolf Ring immunity is an intended counter). Lingers <see cref="LingerTicks"/>, then thins out over
    /// <see cref="FadeTicks"/>. Zero-damage projectiles never reach OnHitPlayer, so every client checks its own
    /// Main.LocalPlayer against the cloud; the server only spawns it.
    /// </summary>
    public class TaurusSandCloud : ModProjectile
    {
        public const float Radius = 96f;
        public const int LingerTicks = 180;
        public const int FadeTicks = 30;
        public const int BlackoutTicks = 120;
        private const int GrowTicks = 12;
        // Once it has thinned below this strength the cloud no longer blinds, so the last wisps are harmless.
        private const float BlindingStrength = 0.35f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LingerTicks + FadeTicks;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];

            // Strength: billows in over GrowTicks, holds, then thins over the final FadeTicks.
            float growIn = MathHelper.Clamp(age / GrowTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float strength = Math.Min(growIn, fadeOut);
            float currentRadius = Radius * MathHelper.Lerp(0.5f, 1f, growIn);

            if (Main.dedServ)
            {
                return;
            }

            EmitCloudDust(currentRadius, strength);

            Player local = Main.LocalPlayer;
            bool localCanBeBlinded = local.active && !local.dead && strength >= BlindingStrength;
            if (!localCanBeBlinded)
            {
                return;
            }

            // Touching = the nearest point of the player's hitbox lies inside the cloud's circle.
            Rectangle playerBox = local.Hitbox;
            float nearestX = MathHelper.Clamp(Projectile.Center.X, playerBox.Left, playerBox.Right);
            float nearestY = MathHelper.Clamp(Projectile.Center.Y, playerBox.Top, playerBox.Bottom);
            float distanceToPlayer = Vector2.Distance(Projectile.Center, new Vector2(nearestX, nearestY));
            if (distanceToPlayer <= currentRadius)
            {
                local.AddBuff(BuffID.Blackout, BlackoutTicks);
            }
        }

        private void EmitCloudDust(float currentRadius, float strength)
        {
            int dustCount = (int)Math.Round(5f * strength);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(currentRadius, currentRadius * 0.8f);
                Vector2 drift = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.5f, 0.2f));
                Dust sand = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Sand, drift, 120,
                    new Color(196, 160, 104), Main.rand.NextFloat(1.4f, 2.2f) * strength);
                sand.noGravity = true;
                sand.fadeIn = 0.6f;
            }

            if (Main.rand.NextFloat() < 0.6f * strength)
            {
                Vector2 offset = Main.rand.NextVector2Circular(currentRadius * 0.8f, currentRadius * 0.6f);
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.1f, 0.5f)), 150,
                    new Color(150, 126, 90), Main.rand.NextFloat(1.6f, 2.6f) * strength);
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    /// <summary>
    /// Brimstone Rain flame, in the Red Knight's Destined Death palette. Spawned by the server at its drop point with
    /// ai[0] = ticks to wait before its marker (staggers the volley into a pattern). Then <see cref="MarkerTicks"/> of
    /// crimson dust converging on the drop point (the spawn telegraph), then it falls at <see cref="FallSpeed"/> until a
    /// tile or a player ends it in a small Destined Death burst. A hit applies <see cref="DestinedDeath"/>.
    /// </summary>
    public class TaurusBrimstoneFlame : ModProjectile
    {
        public const int MarkerTicks = 30;
        public const float FallSpeed = 5f;
        private const int FallLifetimeTicks = 160;
        private const int FadeTicks = 15;
        private const int FrameCount = 5;
        private const int TicksPerFrame = 5;
        public const int DestinedDeathTicks = 600;

        // Red Knight Destined Death palette (RedKnightAttackProjectiles): crimson body, black soot, hot sparks.
        public static readonly Color CrimsonBody = new Color(214, 22, 42);
        public static readonly Color BlackSoot = new Color(16, 3, 8);
        public static readonly Color HotSpark = new Color(255, 176, 96);

        // Intentional texture share: the DestinedDeathBlaze sheet is already Destined Death-coloured.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        private int WaitTicks => (int)Projectile.ai[0];
        private float Age => Projectile.localAI[0];
        private bool Falling => Age > WaitTicks + MarkerTicks;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
        }

        public override bool? CanDamage() => Falling;

        public override void AI()
        {
            // timeLeft is not part of projectile sync, so every machine sets it from the synced delay on tick one.
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = WaitTicks + MarkerTicks + FallLifetimeTicks;
            }

            Projectile.localAI[0]++;

            if (!Falling)
            {
                Projectile.velocity = Vector2.Zero;
                bool inMarker = Age > WaitTicks;
                if (inMarker && !Main.dedServ)
                {
                    float markerProgress = (Age - WaitTicks) / MarkerTicks;
                    EmitMarkerDust(markerProgress);
                }
                return;
            }

            // First falling tick: drop straight down and start colliding with tiles (each machine, no sync needed).
            if (Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = new Vector2(0f, FallSpeed);
                Projectile.tileCollide = true;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = -0.2f }, Projectile.Center);
                }
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.timeLeft / (float)FadeTicks);
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.84f, 0.09f, 0.16f) * 0.6f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                bool soot = Main.rand.NextBool(4);
                int dustType = DustID.RedTorch;
                Color tint = CrimsonBody;
                if (soot)
                {
                    dustType = DustID.Shadowflame;
                    tint = BlackSoot;
                }

                Dust trail = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), dustType,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.5f, 1.5f)), 60, tint,
                    Main.rand.NextFloat(1f, 1.4f));
                trail.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Authority-side contact so the server's copy also bursts on the player (see TaurusJavelin).
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool touchable = player.active && !player.dead && !player.immune;
                if (touchable && player.Hitbox.Intersects(Projectile.Hitbox))
                {
                    Projectile.Kill();
                    return;
                }
            }
        }

        private void EmitMarkerDust(float markerProgress)
        {
            // Crimson motes spiral in to the drop point, tightening as the flame is about to appear.
            float ringRadius = MathHelper.Lerp(44f, 6f, markerProgress);
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 offset = angle.ToRotationVector2() * ringRadius;
                Vector2 inward = -offset * 0.08f;
                bool soot = Main.rand.NextBool(4);
                int dustType = DustID.RedTorch;
                Color tint = CrimsonBody;
                if (soot)
                {
                    dustType = DustID.Shadowflame;
                    tint = BlackSoot;
                }

                Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, dustType, inward, 50, tint,
                    MathHelper.Lerp(0.8f, 1.4f, markerProgress));
                mote.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.84f, 0.09f, 0.16f) * 0.4f * markerProgress);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), DestinedDeathTicks);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || !Falling)
            {
                return;
            }

            // Scaled-down Red Knight fireball burst: crimson body, every 4th mote black soot, plus hot sparks.
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.45f, Pitch = -0.1f }, Projectile.Center);
            for (int i = 0; i < 24; i++)
            {
                bool soot = i % 4 == 0;
                int dustType = DustID.RedTorch;
                Color tint = CrimsonBody;
                if (soot)
                {
                    dustType = DustID.Shadowflame;
                    tint = BlackSoot;
                }

                Dust body = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(3.5f, 3.5f),
                    50, tint, Main.rand.NextFloat(1.1f, 1.7f));
                body.noGravity = true;
            }

            for (int i = 0; i < 10; i++)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(5f, 5f) - new Vector2(0f, 1.5f), 40, HotSpark, Main.rand.NextFloat(0.8f, 1.2f));
                spark.noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Falling)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            // Emissive fire: drawn at full brightness, faded only by the end-of-life alpha.
            Color drawColor = Color.White * (1f - Projectile.alpha / 255f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor, 0f, origin, 0.75f, SpriteEffects.None, 0);
            return false;
        }
    }
}
