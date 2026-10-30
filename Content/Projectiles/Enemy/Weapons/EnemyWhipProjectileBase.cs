using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// An enemy-owned whip lash. Vanilla's whip simulation is player-animation-dependent, so this projectile owns one
    /// shared set of control points for both drawing and collision. Extracted from the Dark Blood Knight's Dark Harvest
    /// lash (its subclass keeps every number) so the Witchking's mace whip can share it.
    ///
    /// Life: a telegraph that tracks <see cref="IEnemyWhipOwner.WhipAim"/> and locks for its last 10 ticks (the rope
    /// trails low behind, then lifts), a lash that releases from the exact cocked curve, damage only on lash ticks
    /// DamageStart..DamageEnd, then a retract. With <see cref="LatchOnHit"/> a hit latches the tip to the player and
    /// tows them toward the owner for AttachedDuration (velocity only, so tiles stop them), then retracts.
    /// ai[0] = owner NPC whoAmI. ai[1]/ai[2] are free for subclasses.
    /// </summary>
    public abstract class EnemyWhipProjectileBase : ModProjectile
    {
        #region Tuning (override per whip)
        protected abstract int SegmentCount { get; }
        protected virtual int TelegraphDuration => 46;
        protected virtual int LashDuration => 30;
        protected virtual int ReleaseBlendTicks => 6;
        protected abstract float MaximumReach { get; }
        protected virtual int DamageStart => 8;
        protected virtual int DamageEnd => 19;
        protected virtual int AttachedDuration => 30;
        protected virtual int AttachedRetractDuration => 12;
        protected virtual float PullStopDistance => 82f;
        protected virtual float PullAcceleration => 1.35f;
        protected virtual float MaximumPullSpeed => 13f;
        /// <summary>True: a hit latches the tip to the player and tows them in. False: the lash just carries on.</summary>
        protected virtual bool LatchOnHit => true;
        /// <summary>Collision half-thickness of each rope segment, px.</summary>
        protected virtual float RopeThickness => 18f;
        protected virtual int LashDustType => DustID.Blood;
        protected virtual int TetherDustType => DustID.Blood;
        #endregion

        private readonly List<Vector2> _controlPoints = new List<Vector2>();
        private readonly List<Vector2> _telegraphPoints = new List<Vector2>();
        private readonly List<Vector2> _lashPoints = new List<Vector2>();
        private int _attachedPlayer = -1;
        private int _attachedAge;
        private int _attachedRetractAge = -1;
        private Vector2 _attachedRetractStart;
        protected int Age => (int)Projectile.localAI[0];
        protected int LashAge => Age - TelegraphDuration;
        protected IReadOnlyList<Vector2> ControlPoints => _controlPoints;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphDuration + LashDuration + AttachedDuration
                + AttachedRetractDuration + 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
            => _attachedPlayer < 0
                && _attachedRetractAge < 0
                && LashAge >= DamageStart
                && LashAge <= DamageEnd
                    ? null
                    : false;

        /// <summary>On-hit effects for the hit player (runs on that player's machine).</summary>
        protected virtual void OnWhipHitPlayer(Player target) { }

        /// <summary>Fires once on the tick the lash goes live (sound + dust at the tip by default).</summary>
        protected virtual void OnLashLive(Vector2 tip)
        {
            SoundEngine.PlaySound(SoundID.Item153 with { Volume = 0.8f, Pitch = -0.08f }, tip);
            if (!Main.dedServ)
            {
                for (int i = 0; i < 8; i++)
                {
                    Dust dust = Dust.NewDustPerfect(tip, LashDustType,
                        Main.rand.NextVector2Circular(2.2f, 2.2f), 70, default, 0.9f);
                    dust.noGravity = true;
                }
            }
        }

        public override void AI()
        {
            if (!TryGetOwner(out NPC ownerNPC, out IEnemyWhipOwner owner)
                || !owner.IsWhipActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.WhipAnchor;
            Projectile.direction = ownerNPC.direction;
            Projectile.spriteDirection = ownerNPC.direction;

            if (_attachedPlayer >= 0)
            {
                UpdateAttachedWhip(ownerNPC, owner);
                Projectile.localAI[0]++;
                return;
            }

            if (_attachedRetractAge >= 0)
            {
                BuildAttachedRetractPoints(owner.WhipAnchor);
                if (++_attachedRetractAge >= AttachedRetractDuration)
                    Projectile.Kill();
                Projectile.localAI[0]++;
                return;
            }

            if (Age >= TelegraphDuration + LashDuration)
            {
                Projectile.Kill();
                return;
            }

            // Track throughout the readable portion of the tell, then commit for its last ten ticks.
            // The owner performs the same lock, so clients derive the same curve from synchronized NPC
            // state while the server sends one final projectile aim snapshot at commitment.
            if (Age < TelegraphDuration - 10)
                Projectile.velocity = owner.WhipAim;
            else if (Age == TelegraphDuration - 10 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.netUpdate = true;

            BuildControlPoints(owner.WhipAnchor);

            if (Age == TelegraphDuration)
                SoundEngine.PlaySound(SoundID.Item152 with { Volume = 0.7f, Pitch = -0.12f }, Projectile.Center);

            if (LashAge == DamageStart)
            {
                OnLashLive(WhipTip);
            }

            Projectile.localAI[0]++;
        }

        private void UpdateAttachedWhip(NPC ownerNPC, IEnemyWhipOwner owner)
        {
            if (_attachedPlayer < 0 || _attachedPlayer >= Main.maxPlayers)
            {
                BeginAttachedRetract(WhipTip);
                return;
            }

            Player target = Main.player[_attachedPlayer];
            if (!target.active || target.dead)
            {
                BeginAttachedRetract(WhipTip);
                return;
            }

            BuildTetherPoints(owner.WhipAnchor, target.Center, _controlPoints, SegmentCount);
            ApplyCollisionRespectingPull(ownerNPC, target);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(7f, 9f),
                    TetherDustType, Main.rand.NextVector2Circular(1.5f, 1.5f), 70, default, 0.8f);
                dust.noGravity = true;
            }

            _attachedAge++;
            if (_attachedAge >= AttachedDuration)
            {
                StopPullMomentum(ownerNPC, target);
                BeginAttachedRetract(target.Center);
            }
        }

        private void ApplyCollisionRespectingPull(NPC owner, Player target)
        {
            // Resolve this on the server and on the grabbed player's own client. Velocity keeps solid
            // tiles authoritative; the tether never teleports the player through terrain.
            if (Main.netMode == NetmodeID.MultiplayerClient && target.whoAmI != Main.myPlayer)
                return;

            Vector2 toOwner = owner.Center - target.Center;
            float distance = toOwner.Length();
            if (distance <= PullStopDistance)
            {
                StopPullMomentum(owner, target);
                return;
            }

            Vector2 pullDirection = toOwner / distance;
            float desiredSpeed = MathHelper.Clamp((distance - PullStopDistance) / 7f, 2.5f, MaximumPullSpeed);
            float currentPullSpeed = Vector2.Dot(target.velocity, pullDirection);
            if (currentPullSpeed < desiredSpeed)
            {
                target.velocity += pullDirection
                    * Math.Min(PullAcceleration, desiredSpeed - currentPullSpeed);
            }
        }

        private static void StopPullMomentum(NPC owner, Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && target.whoAmI != Main.myPlayer)
                return;

            Vector2 pullDirection = (owner.Center - target.Center).SafeNormalize(Vector2.Zero);
            float inwardSpeed = Vector2.Dot(target.velocity, pullDirection);
            if (inwardSpeed > 0f)
                target.velocity -= pullDirection * inwardSpeed;
        }

        private void BeginAttachedRetract(Vector2 startTip)
        {
            _attachedRetractStart = startTip;
            _attachedPlayer = -1;
            _attachedRetractAge = 0;
            Projectile.hostile = false;
            Projectile.netUpdate = true;
        }

        protected Vector2 WhipTip => _controlPoints.Count > 0 ? _controlPoints[^1] : Projectile.Center;

        private void BuildControlPoints(Vector2 anchor)
        {
            if (Age < TelegraphDuration)
            {
                BuildTelegraphPoints(anchor, Age / (float)Math.Max(1, TelegraphDuration - 1), _controlPoints);
                return;
            }

            int lashAge = LashAge;
            BuildLashPoints(anchor, lashAge, _lashPoints);
            if (lashAge >= ReleaseBlendTicks)
            {
                CopyPoints(_lashPoints, _controlPoints);
                return;
            }

            // Release from the exact final cocked curve rather than replacing it with a new whip
            // shape. Damage remains disabled until two ticks after this hand-off is complete.
            BuildTelegraphPoints(anchor, 1f, _telegraphPoints);
            float release = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(lashAge / (float)ReleaseBlendTicks, 0f, 1f));
            _controlPoints.Clear();
            for (int i = 0; i < SegmentCount; i++)
                _controlPoints.Add(Vector2.Lerp(_telegraphPoints[i], _lashPoints[i], release));
        }

        private void BuildTelegraphPoints(Vector2 anchor, float progress, List<Vector2> destination)
        {
            destination.Clear();

            float settle = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp((progress - 0.72f) / 0.28f, 0f, 1f));
            float facing = Projectile.spriteDirection;
            Vector2 behind = new Vector2(-facing, 0f);

            // The loose whip begins trailing low behind the knight, then the handle lifts first and
            // pulls a broad wave upward. The final pose is compact enough not to look like a hitbox,
            // but puts the tip unmistakably behind the attacker before the forward release.
            Vector2 startControl1 = anchor + behind * 18f + Vector2.UnitY * 24f;
            Vector2 startControl2 = anchor + behind * 72f + Vector2.UnitY * 62f;
            Vector2 startTip = anchor + behind * 148f + Vector2.UnitY * 48f;

            Vector2 raisedControl1 = anchor + behind * 8f - Vector2.UnitY * 34f;
            Vector2 raisedControl2 = anchor + behind * 50f - Vector2.UnitY * 100f;
            Vector2 raisedTip = anchor + behind * 122f - Vector2.UnitY * 66f;

            // A small final draw-back makes the ten-tick aim lock legible instead of leaving the
            // telegraph frozen. It never advances toward the player.
            raisedControl2 += behind * (10f * settle) - Vector2.UnitY * (4f * settle);
            raisedTip += behind * (16f * settle) - Vector2.UnitY * (5f * settle);

            for (int i = 0; i < SegmentCount; i++)
            {
                float u = i / (SegmentCount - 1f);
                Vector2 loosePoint = CubicBezier(
                    anchor, startControl1, startControl2, startTip, u);
                Vector2 raisedPoint = CubicBezier(
                    anchor, raisedControl1, raisedControl2, raisedTip, u);

                // Send the lift from handle to tip instead of rotating the entire rope as one rigid
                // shape. At u=0 the hand leads; the far tip begins and finishes its rise later.
                float wave = MathHelper.SmoothStep(0f, 1f,
                    MathHelper.Clamp((progress - u * 0.18f) / 0.54f, 0f, 1f));
                destination.Add(Vector2.Lerp(loosePoint, raisedPoint, wave));
            }
        }

        private void BuildLashPoints(Vector2 anchor, int lashAge, List<Vector2> destination)
        {
            destination.Clear();
            destination.Add(anchor);

            Vector2 aim = Projectile.velocity.SafeNormalize(new Vector2(Projectile.direction, 0f));
            float progress = MathHelper.Clamp(lashAge / (float)LashDuration, 0f, 1f);
            float extension;
            float sweepOffset;
            float bend;

            if (lashAge <= 10)
            {
                float extend = MathHelper.SmoothStep(0f, 1f, lashAge / 10f);
                extension = MathHelper.Lerp(0.08f, 1f, extend);
                sweepOffset = MathHelper.Lerp(-0.72f, 0.04f, extend) * Projectile.spriteDirection;
                bend = MathHelper.Lerp(0.52f, 0.08f, extend) * Projectile.spriteDirection;
            }
            else if (lashAge <= 18)
            {
                extension = 1f;
                sweepOffset = MathHelper.Lerp(0.04f, 0.14f, (lashAge - 10f) / 8f) * Projectile.spriteDirection;
                bend = 0.08f * Projectile.spriteDirection;
            }
            else
            {
                float retract = MathHelper.SmoothStep(0f, 1f, (lashAge - 18f) / 12f);
                extension = MathHelper.Lerp(1f, 0.04f, retract);
                sweepOffset = MathHelper.Lerp(0.14f, 0.02f, retract) * Projectile.spriteDirection;
                bend = MathHelper.Lerp(0.08f, 0.38f, retract) * Projectile.spriteDirection;
            }

            float segmentLength = MaximumReach * extension / (SegmentCount - 1f);
            Vector2 point = anchor;
            for (int i = 1; i < SegmentCount; i++)
            {
                float u = i / (SegmentCount - 1f);
                float taper = (1f - u) * bend * MathF.Sin(u * MathHelper.Pi);
                float ripple = MathF.Sin(u * MathHelper.TwoPi + progress * MathHelper.Pi) * 0.025f * (1f - u);
                Vector2 tangent = aim.RotatedBy(sweepOffset + taper + ripple);
                point += tangent * segmentLength;
                destination.Add(point);
            }
        }

        private void BuildAttachedRetractPoints(Vector2 anchor)
        {
            float progress = MathHelper.SmoothStep(0f, 1f,
                _attachedRetractAge / (float)Math.Max(1, AttachedRetractDuration - 1));
            Vector2 aim = Projectile.velocity.SafeNormalize(new Vector2(Projectile.direction, 0f));
            Vector2 restingTip = anchor + aim * (MaximumReach * 0.04f);
            BuildTetherPoints(anchor, Vector2.Lerp(_attachedRetractStart, restingTip, progress), _controlPoints, SegmentCount);
        }

        private static void BuildTetherPoints(Vector2 anchor, Vector2 tip, List<Vector2> destination, int segmentCount)
        {
            destination.Clear();
            Vector2 line = tip - anchor;
            Vector2 normal = line.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float bend = MathHelper.Clamp(line.Length() * 0.055f, 4f, 18f);
            Vector2 control1 = anchor + line * 0.32f + normal * bend;
            Vector2 control2 = anchor + line * 0.68f - normal * bend * 0.35f;

            for (int i = 0; i < segmentCount; i++)
            {
                float progress = i / (segmentCount - 1f);
                destination.Add(CubicBezier(anchor, control1, control2, tip, progress));
            }
        }

        private static Vector2 CubicBezier(
            Vector2 start, Vector2 control1, Vector2 control2, Vector2 end, float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * inverse * start
                + 3f * inverse * inverse * progress * control1
                + 3f * inverse * progress * progress * control2
                + progress * progress * progress * end;
        }

        private static void CopyPoints(List<Vector2> source, List<Vector2> destination)
        {
            destination.Clear();
            destination.AddRange(source);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_controlPoints.Count != SegmentCount)
                return false;

            float collisionPoint = 0f;
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                if (Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(), targetHitbox.Size(),
                    _controlPoints[i], _controlPoints[i + 1], RopeThickness, ref collisionPoint))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            OnWhipHitPlayer(target);
            if (_attachedPlayer >= 0 || _attachedRetractAge >= 0)
                return;

            // Credit the owner's combo step (hit-confirm branches read it); forwarded to the server in MP.
            if (TryGetOwner(out NPC ownerNPC, out _) && ownerNPC.ModNPC is PuppetNPC puppet)
                puppet.ReportAttackHit();

            if (!LatchOnHit)
                return;

            _attachedPlayer = target.whoAmI;
            _attachedAge = 0;
            Projectile.hostile = false;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.NPCHit13 with
            {
                Volume = 0.52f,
                Pitch = -0.28f,
            }, target.Center);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_attachedPlayer);
            writer.Write(_attachedAge);
            writer.Write(_attachedRetractAge);
            writer.WriteVector2(_attachedRetractStart);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _attachedPlayer = reader.ReadInt32();
            _attachedAge = reader.ReadInt32();
            _attachedRetractAge = reader.ReadInt32();
            _attachedRetractStart = reader.ReadVector2();
            if (_attachedPlayer >= 0 || _attachedRetractAge >= 0)
                Projectile.hostile = false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_controlPoints.Count != SegmentCount)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                DrawSegment(texture, i, _controlPoints[i], _controlPoints[i + 1]);
            }
            return false;
        }

        /// <summary>Draws rope segment <paramref name="segment"/> from <paramref name="from"/> toward
        /// <paramref name="to"/>. Default: a frame of a vertical sheet (sprite facing down), stretched to the gap.</summary>
        protected virtual void DrawSegment(Texture2D texture, int segment, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            Rectangle source = GetSegmentSource(texture, segment);
            Vector2 origin = source.Size() * 0.5f;
            float rotation = delta.ToRotation() - MathHelper.PiOver2;
            float stretch = Math.Max(0.5f, delta.Length() / Math.Max(1f, source.Height));
            Color color = Lighting.GetColor(from.ToTileCoordinates());

            Main.EntitySpriteDraw(texture, from - Main.screenPosition, source, color,
                rotation, origin, new Vector2(1f, stretch), SpriteEffects.None, 0);
        }

        /// <summary>The sheet region for a segment (handle, links, tip).</summary>
        protected abstract Rectangle GetSegmentSource(Texture2D texture, int segment);

        protected bool TryGetOwner(out NPC ownerNPC, out IEnemyWhipOwner owner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC candidate = Main.npc[ownerIndex];
                if (candidate.active && candidate.ModNPC is IEnemyWhipOwner whipOwner)
                {
                    ownerNPC = candidate;
                    owner = whipOwner;
                    return true;
                }
            }

            ownerNPC = null;
            owner = null;
            return false;
        }
    }
}
