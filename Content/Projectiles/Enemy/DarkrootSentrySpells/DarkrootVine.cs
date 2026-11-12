using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells
{
    /// <summary>
    /// Briar Lash vine. Follows a cubic Bezier the owner locked and showed as a corridor (four controls, synced in
    /// ExtraAI), cached as 32 arc-length samples so draw and damage read the same points.
    ///   age 1..20      the tip travels the whole arc (SmoothStep in arc length) - LIVE, 8px radius tip only
    ///   age 21..38     harmless retraction back along the same arc into the earth, then removed
    ///   age 1000..1012 cancelled (owner staggered / died / target lost): frozen in place, fades out
    ///   age 2000..2120 LATCHED: the tip hit a player, so woody strands pin their torso to ground anchors. Hold
    ///                  (no movement, no roll; attacking still works) for up to 2s. Two attacks by the held player
    ///                  snap it early; so does staggering or killing the Sentry. The Sentry gets one punish attack.
    /// The cable behind the tip is visual only. A wall appearing in front of the tip retracts it instead.
    /// MP: the hit is only known on the victim's machine (OnHitPlayer), so the victim asks the server to latch and
    /// to count each attack (tsorcPacketID.DarkrootVineRequest); the server decides and syncs _heldPlayer/_strikes.
    /// The victim predicts its own Hold for up to 20t while that request is in flight.
    /// </summary>
    public class DarkrootVine : DarkrootHazard
    {
        // 20t to cover up to 640px of arc (Briar Lash reaches 560px): ~32px/tick at the far end.
        public const int ExtendTicks = 20;
        public const int RetractTicks = 18;
        public const int SampleCount = 32;
        public const float MinimumSpawnDistance = 192f;
        public const float MaximumPathLength = 640f;
        private const int BindingCount = 4;
        private const float SegmentStep = 18f;
        const int Cancelled = 1000;
        const int CancelFadeTicks = 12;
        const float TipRadius = 8f;
        const int LatchBase = 2000;
        public const int HoldTicks = 120;
        public const int StrikesToBreak = 2;
        // Minimum gap between counted attacks, so one auto-swing or a pinned charge (ItemAnimationJustStarted every
        // tick) cannot count twice. Earliest possible break: 20t after the grab.
        const int StrikeSpacing = 20;
        const int LatchPredictTicks = 20;
        // Late-latch allowance (ticks past the extend) and reach (px from the tip) for a request still in flight.
        const int LatchLagTicks = 6;
        const float LatchReach = 96f;

        public const byte RequestLatch = 0;
        public const byte RequestStrike = 1;

        readonly Vector2[] _controls = new Vector2[4];
        readonly Vector2[] _samples = new Vector2[SampleCount + 1];
        readonly float[] _arcFraction = new float[SampleCount + 1];
        float _arcLength;
        float _retractFrom = 1f;
        bool _hasPath;
        int _heldPlayer = -1;
        int _strikes;
        private readonly Vector2[] _groundAnchors = new Vector2[BindingCount];
        private readonly Vector2[] _bindingControls = new Vector2[4];
        private readonly Vector2[] _bindingSamples = new Vector2[SampleCount + 1];
        private readonly float[] _bindingFractions = new float[SampleCount + 1];
        private byte _groundAnchorMask;
        private Vector2 _bindingCenter;
        // Single-frame 2x2-grid art: 24x12 bark runs left to right; 12x14 thorn points up with its root at the bottom.
        private static Asset<Texture2D> segmentTexture;
        private static Asset<Texture2D> thornTexture;

        // Server only: the owner has spent its one punish on this grab; earliest tick the next attack may count.
        public bool PunishClaimed;
        uint _nextStrikeTick;

        // Local only: Hold predicted while my latch request is in flight, and change detection for effects.
        int _predictTicks;
        uint _nextLocalStrikeTick;
        int _seenStrikes;
        bool _wasLatched;

        public override bool IsLive => _hasPath && Age >= 1f && Age <= ExtendTicks;

        bool Latched => Age >= LatchBase && _heldPlayer >= 0;

        /// <summary>Index of the player this vine is holding, or -1.</summary>
        public int HeldPlayer => Latched ? _heldPlayer : -1;

        public override void SetDefaults()
        {
            // The body attachments draw over the grabbed player, with the same path used by the live tip.
            Projectile.hide = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
        }

        /// <summary>Server, right after spawning: store the locked controls and build the cached samples.</summary>
        public void SetPath(Vector2[] controls)
        {
            for (int i = 0; i < 4; i++)
            {
                _controls[i] = controls[i];
            }

            BuildSamples(_controls, _samples, _arcFraction, out _arcLength);
            _hasPath = true;
            Projectile.netUpdate = true;
        }

        /// <summary>Samples a cubic Bezier into <paramref name="samples"/> (SampleCount + 1 points) and the cumulative
        /// arc-length fraction at each. Shared with the owner, which validates and draws the same corridor.</summary>
        public static void BuildSamples(Vector2[] controls, Vector2[] samples, float[] arcFraction, out float arcLength)
        {
            arcLength = 0f;
            for (int i = 0; i <= SampleCount; i++)
            {
                float fraction = i / (float)SampleCount;
                float inverse = 1f - fraction;
                samples[i] = inverse * inverse * inverse * controls[0]
                    + 3f * inverse * inverse * fraction * controls[1]
                    + 3f * inverse * fraction * fraction * controls[2]
                    + fraction * fraction * fraction * controls[3];

                if (i > 0)
                {
                    arcLength += Vector2.Distance(samples[i - 1], samples[i]);
                }

                arcFraction[i] = arcLength;
            }

            for (int i = 0; i <= SampleCount; i++)
            {
                arcFraction[i] = arcLength > 0f ? arcFraction[i] / arcLength : i / (float)SampleCount;
            }
        }

        /// <summary>Point at an arc-length fraction (0 = ground origin, 1 = locked target point).</summary>
        Vector2 PointAt(float fraction)
        {
            return PointOnPath(_samples, _arcFraction, fraction);
        }

        private static Vector2 PointOnPath(Vector2[] samples, float[] arcFractions, float fraction)
        {
            fraction = MathHelper.Clamp(fraction, 0f, 1f);

            for (int i = 1; i <= SampleCount; i++)
            {
                if (arcFractions[i] >= fraction)
                {
                    float span = arcFractions[i] - arcFractions[i - 1];
                    float local = span > 0f ? (fraction - arcFractions[i - 1]) / span : 0f;
                    return Vector2.Lerp(samples[i - 1], samples[i], local);
                }
            }

            return samples[SampleCount];
        }

        /// <summary>Find a nearby supported surface with room for a vine and a clear line to the target.
        /// Shared by the launch planner and the latched body's ground anchors; search distance is in tiles.</summary>
        public static bool TryFindGround(Vector2 near, Vector2 sightTarget, int searchTiles, out Vector2 floor)
        {
            floor = Vector2.Zero;
            int tileX = (int)(near.X / 16f);
            int baseTileY = (int)(near.Y / 16f);
            for (int i = 0; i <= searchTiles * 2; i++)
            {
                int offset = (i + 1) / 2;
                if (i % 2 == 0)
                {
                    offset = -offset;
                }

                int tileY = baseTileY + offset;
                if (!WorldGen.InWorld(tileX, tileY, 1))
                {
                    continue;
                }

                Tile support = Framing.GetTileSafely(tileX, tileY);
                bool supported = support.HasTile && !support.IsActuated
                    && (Main.tileSolid[support.TileType] || Main.tileSolidTop[support.TileType]);
                if (!supported)
                {
                    continue;
                }

                float surfaceY = tileY * 16f;
                if (support.IsHalfBlock)
                {
                    surfaceY += 8f;
                }

                Vector2 candidate = new(near.X, surfaceY);
                bool roomAbove = !Collision.SolidCollision(candidate + new Vector2(-6f, -12f), 12, 12);
                bool visible = Collision.CanHitLine(sightTarget, 1, 1, candidate + new Vector2(0f, -10f), 1, 1);
                if (roomAbove && visible)
                {
                    floor = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Tip position along the arc, as a fraction, for a given age.</summary>
        float TipProgress(float age)
        {
            if (age >= LatchBase)
            {
                return 1f;
            }

            if (age >= Cancelled)
            {
                return _retractFrom;
            }

            if (age <= ExtendTicks)
            {
                return MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / ExtendTicks, 0f, 1f));
            }

            float retract = MathHelper.Clamp((age - ExtendTicks) / RetractTicks, 0f, 1f);
            return _retractFrom * (1f - MathHelper.SmoothStep(0f, 1f, retract));
        }

        public override void Disarm()
        {
            if (Latched)
            {
                Release("disarmed");
                return;
            }

            if (Age >= Cancelled)
            {
                return;
            }

            _retractFrom = TipProgress(Age);
            Age = Cancelled;
            Projectile.netUpdate = true;
        }

        /// <summary>Server: the victim reports the live tip hit them. Accepted while the tip is live (plus a lag
        /// allowance) and near them; starts the hold and hands the owner its punish target.</summary>
        public void TryLatch(Player player)
        {
            bool tipWasLive = _hasPath && Age >= 1f && Age <= ExtendTicks + LatchLagTicks;
            bool nearTip = player.Distance(Projectile.Center) < LatchReach;

            if (!tipWasLive || !nearTip || !player.active || player.dead)
            {
                return;
            }

            _heldPlayer = player.whoAmI;
            _strikes = 0;
            _bindingCenter = player.Center;
            _groundAnchorMask = 0;
            for (int i = 0; i < BindingCount; i++)
            {
                float offset = i < 2 ? 76f : 44f;
                if (i % 2 == 0)
                {
                    offset = -offset;
                }

                Vector2 search = player.Bottom + new Vector2(offset, 0f);
                if (TryFindGround(search, player.Center, 24, out Vector2 floor))
                {
                    _groundAnchors[i] = floor;
                    _groundAnchorMask |= (byte)(1 << i);
                }
            }

            // The launch root is always a valid earth attachment if local ledges leave no room for side roots.
            if (_groundAnchorMask == 0)
            {
                _groundAnchors[0] = _controls[0] + new Vector2(0f, 8f);
                _groundAnchorMask = 1;
            }

            Age = LatchBase;
            Projectile.timeLeft = HoldTicks + 60;
            Projectile.netUpdate = true;

            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (owner.active && owner.ModNPC is DarkrootSentry sentry)
            {
                sentry.OnVineLatched(player);
            }
        }

        /// <summary>Server: the held player started an attack. Two counted attacks, at least StrikeSpacing apart,
        /// snap the vine.</summary>
        public void RegisterStrike(int playerIndex)
        {
            bool fromHeldPlayer = Latched && playerIndex == _heldPlayer;
            if (!fromHeldPlayer || Main.GameUpdateCount < _nextStrikeTick)
            {
                return;
            }

            _strikes++;
            _nextStrikeTick = Main.GameUpdateCount + StrikeSpacing;
            Projectile.netUpdate = true;

            if (_strikes >= StrikesToBreak)
            {
                Release("broken");
            }
        }

        /// <summary>Server: end the hold. The tethered vine freezes where it is and fades like a cancel.</summary>
        void Release(string reason)
        {
            if (_heldPlayer >= 0 && _heldPlayer < Main.maxPlayers)
            {
                _bindingCenter = Main.player[_heldPlayer].Center;
            }

            _heldPlayer = -1;
            _retractFrom = 1f;
            Age = Cancelled;
            Projectile.netUpdate = true;

            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (owner.active && owner.ModNPC is DarkrootSentry sentry)
            {
                sentry.Trace("vine-release-" + reason);
            }
        }

        /// <summary>Victim's machine: send a latch or strike request for this vine. Singleplayer applies it directly.</summary>
        void SendRequest(byte action, Player player)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (action == RequestLatch)
                {
                    TryLatch(player);
                }
                else
                {
                    RegisterStrike(player.whoAmI);
                }

                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write(tsorcPacketID.DarkrootVineRequest);
            packet.Write(action);
            packet.Write((byte)Projectile.owner);
            packet.Write((short)Projectile.identity);
            packet.Send();
        }

        public override void AI()
        {
            DisarmIfOrphaned();

            // Predicted Hold on the victim while the server has not confirmed the latch yet.
            if (_predictTicks > 0 && !Latched)
            {
                _predictTicks--;
                Main.LocalPlayer.AddBuff(ModContent.BuffType<Hold>(), 2);
            }

            if (Latched)
            {
                Player held = Main.player[_heldPlayer];

                // Keep the launch root planted in the earth. Its end follows the held body; the short bindings
                // fan out to their own server-selected ground points instead of forming closed loops.
                _bindingCenter = held.Center;
                _controls[3] = held.Center + new Vector2(-held.direction * 7f, 7f);
                Vector2 span = _controls[3] - _controls[0];
                Vector2 rise = new(0f, -12f);
                _controls[1] = _controls[0] + span * 0.33f + rise;
                _controls[2] = _controls[0] + span * 0.66f + rise;
                BuildSamples(_controls, _samples, _arcFraction, out _arcLength);
                Projectile.Center = held.Center;
                Age++;

                // The victim refreshes its own Hold in 2t slices, so a release frees them at once, and reports each
                // new attack with any damaging item to the server.
                if (_heldPlayer == Main.myPlayer)
                {
                    held.AddBuff(ModContent.BuffType<Hold>(), 2);
                    _predictTicks = 0;

                    bool attackStarted = held.ItemAnimationJustStarted && held.HeldItem.damage > 0;
                    if (attackStarted && Main.GameUpdateCount >= _nextLocalStrikeTick)
                    {
                        _nextLocalStrikeTick = Main.GameUpdateCount + StrikeSpacing;
                        SendRequest(RequestStrike, held);
                    }
                }

                // One pair of body-to-ground strands snaps per counted attack.
                if (!Main.dedServ && _strikes > _seenStrikes)
                {
                    DarkrootWoodVFX.Motes(held.Center, 10, 10f, false, DustID.WoodFurniture);
                    SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.8f, Pitch = -0.2f }, held.Center);
                }

                _seenStrikes = _strikes;
                _wasLatched = true;

                bool holdOver = Age >= LatchBase + HoldTicks;
                bool heldGone = !held.active || held.dead;
                if (Main.netMode != NetmodeID.MultiplayerClient && (holdOver || heldGone))
                {
                    if (holdOver)
                    {
                        Release("timeout");
                    }
                    else
                    {
                        Release("player-gone");
                    }
                }

                return;
            }

            if (_wasLatched)
            {
                // Latched -> released on this machine: the remaining bindings burst off the player.
                _wasLatched = false;
                if (!Main.dedServ)
                {
                    DarkrootWoodVFX.Motes(_controls[3], 16, 12f, false, DustID.WoodFurniture);
                    DarkrootWoodVFX.Motes(_controls[3], 8, 10f, false, DustID.GrassBlades);
                    SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = 0.3f }, _controls[3]);
                }
            }

            if (!_hasPath)
            {
                // A client can see the spawn packet one update before the path arrives in ExtraAI.
                return;
            }

            float previousProgress = TipProgress(Age);
            Age++;
            float progress = TipProgress(Age);

            // Server: a wall that appeared across the marked corridor retracts the lash from where it stopped.
            if (Main.netMode != NetmodeID.MultiplayerClient && Age <= ExtendTicks)
            {
                Vector2 previousTip = PointAt(previousProgress);
                Vector2 tip = PointAt(progress);
                bool blocked = !Collision.CanHitLine(previousTip - new Vector2(4f), 8, 8, tip - new Vector2(4f), 8, 8);

                if (blocked)
                {
                    _retractFrom = previousProgress;
                    Age = ExtendTicks;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.Center = PointAt(TipProgress(Age));

            bool retracted = Age > ExtendTicks + RetractTicks && Age < Cancelled;
            bool fadedOut = Age >= Cancelled + CancelFadeTicks && Age < LatchBase;
            if (retracted || fadedOut)
            {
                Projectile.Kill();
                return;
            }

            // Leafy splinters run along the spine behind the tip while it extends.
            if (!Main.dedServ && Age <= ExtendTicks && (int)Age % 2 == 0)
            {
                Vector2 behindTip = PointAt(progress * Main.rand.NextFloat(0.4f, 0.95f));
                Dust splinter = Dust.NewDustPerfect(behindTip, DustID.GrassBlades, Main.rand.NextVector2Circular(0.6f, 0.6f), 90, default, 0.8f);
                splinter.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!_hasPath)
            {
                return false;
            }

            // Only the moving tip hurts: its swept previous-to-current path, 8px radius.
            Vector2 previousTip = PointAt(TipProgress(Age - 1f));
            Vector2 tip = PointAt(TipProgress(Age));
            return MeleeBladeCollision.SegmentIntersectsRect(previousTip, tip, TipRadius * 2f, targetHitbox);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.dedServ)
            {
                return;
            }

            DarkrootWoodVFX.Motes(target.Center, 16, 8f, false, DustID.WoodFurniture);
            DarkrootWoodVFX.Motes(target.Center, 8, 6f, false, DustID.GrassBlades);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f }, target.Center);

            // Runs on the victim's machine only: ask the server to latch, and hold myself meanwhile.
            if (target.whoAmI == Main.myPlayer && !Latched)
            {
                _predictTicks = LatchPredictTicks;
                SendRequest(RequestLatch, target);
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_hasPath)
            {
                return false;
            }

            float progress = TipProgress(Age);
            if (progress <= 0.001f)
            {
                return false;
            }

            float opacity = 1f;
            if (Age >= Cancelled && Age < LatchBase)
            {
                opacity = MathHelper.Clamp(1f - (Age - Cancelled) / CancelFadeTicks, 0f, 1f);
            }

            float rootGrowth = MathHelper.Clamp(progress * 8f, 0f, 1f);
            if (Age <= ExtendTicks)
            {
                rootGrowth = 1f;
            }

            DrawSprout(_controls[0] + new Vector2(0f, 8f), rootGrowth, opacity);
            DrawSpritePath(_samples, _arcFraction, _arcLength, progress, opacity, _groundAnchorMask == 0);

            for (int i = 0; i < BindingCount; i++)
            {
                bool anchorPresent = (_groundAnchorMask & (1 << i)) != 0;
                if (!anchorPresent || i / 2 < _strikes)
                {
                    continue;
                }

                int side = i % 2 == 0 ? -1 : 1;
                float attachHeight = i < 2 ? -8f : 9f;
                Vector2 attachment = _bindingCenter + new Vector2(side * 7f, attachHeight);
                Vector2 root = _groundAnchors[i] - new Vector2(0f, 6f);
                Vector2 span = attachment - root;
                Vector2 rise = new(0f, -8f);
                _bindingControls[0] = root;
                _bindingControls[1] = root + span / 3f + rise;
                _bindingControls[2] = root + span * (2f / 3f) + rise;
                _bindingControls[3] = attachment;
                BuildSamples(_bindingControls, _bindingSamples, _bindingFractions, out float length);
                DrawSprout(_groundAnchors[i], 0.6f, opacity);
                DrawSpritePath(_bindingSamples, _bindingFractions, length, 1f, opacity, false);
            }

            return false;
        }

        /// <summary>Short harmless root at the ground source, used by both the telegraph and the active vine.</summary>
        public static void DrawSprout(Vector2 ground, float growth, float opacity)
        {
            if (Main.dedServ || opacity <= 0f || growth <= 0f)
            {
                return;
            }

            segmentTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(DarkrootVine)) + "_Segment");
            Texture2D texture = segmentTexture.Value;
            int length = (int)MathHelper.Clamp(growth * 16f, 2f, texture.Width);
            Rectangle frame = new(0, 0, length, texture.Height);
            Color lighting = Lighting.GetColor(ground.ToTileCoordinates());
            Color color = Color.Lerp(lighting, Color.White, 0.25f) * opacity;
            Vector2 origin = new(0f, texture.Height * 0.5f);
            Main.EntitySpriteDraw(texture, ground - Main.screenPosition, frame, color, -MathHelper.PiOver2, origin, 1f, SpriteEffects.None);
        }

        /// <summary>Repeated bark links follow the same arc-length samples as the moving collision tip.
        /// Small side thorns are decorative; only the pointed end has the 16px-wide damaging sweep.</summary>
        public static void DrawSpritePath(Vector2[] samples, float[] fractions, float arcLength, float progress, float opacity, bool pointed)
        {
            if (Main.dedServ || arcLength <= 0f || opacity <= 0f)
            {
                return;
            }

            segmentTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(DarkrootVine)) + "_Segment");
            thornTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(DarkrootVine)) + "_Thorn");
            Texture2D segment = segmentTexture.Value;
            Texture2D thorn = thornTexture.Value;
            Vector2 segmentOrigin = segment.Size() * 0.5f;
            Vector2 thornOrigin = new(thorn.Width * 0.5f, thorn.Height - 2f);
            float visibleLength = arcLength * MathHelper.Clamp(progress, 0f, 1f);
            int link = 0;
            for (float distance = 0f; distance < visibleLength; distance += SegmentStep)
            {
                float endDistance = Math.Min(distance + SegmentStep, visibleLength);
                Vector2 from = PointOnPath(samples, fractions, distance / arcLength);
                Vector2 to = PointOnPath(samples, fractions, endDistance / arcLength);
                Vector2 delta = to - from;
                float length = delta.Length();
                if (length < 0.1f)
                {
                    continue;
                }

                Vector2 center = (from + to) * 0.5f;
                Color lighting = Lighting.GetColor(center.ToTileCoordinates());
                Color color = Color.Lerp(lighting, Color.White, 0.25f) * opacity;
                float rotation = delta.ToRotation();
                Vector2 scale = new((length + 2f) / segment.Width, 1f);
                Main.EntitySpriteDraw(segment, center - Main.screenPosition, null, color, rotation, segmentOrigin, scale, SpriteEffects.None);

                if (link % 3 == 1 && endDistance < visibleLength - 10f)
                {
                    int side = link % 2 == 0 ? -1 : 1;
                    Vector2 normal = delta.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * side;
                    Vector2 root = center + normal * 3f;
                    float thornRotation = normal.ToRotation() + MathHelper.PiOver2;
                    Main.EntitySpriteDraw(thorn, root - Main.screenPosition, null, color, thornRotation, thornOrigin, 0.8f, SpriteEffects.None);
                }

                link++;
            }

            if (pointed && visibleLength > 2f)
            {
                Vector2 tip = PointOnPath(samples, fractions, progress);
                float behind = Math.Max(0f, progress - 10f / arcLength);
                Vector2 direction = (tip - PointOnPath(samples, fractions, behind)).SafeNormalize(Vector2.UnitX);
                Vector2 tipBase = tip - direction * 6f;
                Color lighting = Lighting.GetColor(tip.ToTileCoordinates());
                Color color = Color.Lerp(lighting, Color.White, 0.25f) * opacity;
                Main.EntitySpriteDraw(thorn, tipBase - Main.screenPosition, null, color, direction.ToRotation() + MathHelper.PiOver2,
                    thornOrigin, 1f, SpriteEffects.None);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_hasPath);
            for (int i = 0; i < 4; i++)
            {
                writer.WriteVector2(_controls[i]);
            }

            writer.Write(_retractFrom);
            writer.Write((short)_heldPlayer);
            writer.Write((byte)_strikes);
            writer.Write(_groundAnchorMask);
            writer.WriteVector2(_bindingCenter);
            for (int i = 0; i < BindingCount; i++)
            {
                writer.WriteVector2(_groundAnchors[i]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool hasPath = reader.ReadBoolean();
            for (int i = 0; i < 4; i++)
            {
                _controls[i] = reader.ReadVector2();
            }

            _retractFrom = reader.ReadSingle();
            _heldPlayer = reader.ReadInt16();
            _strikes = reader.ReadByte();
            _groundAnchorMask = reader.ReadByte();
            _bindingCenter = reader.ReadVector2();
            for (int i = 0; i < BindingCount; i++)
            {
                _groundAnchors[i] = reader.ReadVector2();
            }

            if (hasPath)
            {
                BuildSamples(_controls, _samples, _arcFraction, out _arcLength);
                _hasPath = true;
            }
        }
    }
}
