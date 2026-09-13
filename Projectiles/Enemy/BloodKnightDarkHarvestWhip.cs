using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Enemy-owned Dark Harvest lash. Vanilla's whip simulation is player-animation-dependent, so
    /// this projectile owns one shared set of points for both drawing and collision.
    /// </summary>
    public class BloodKnightDarkHarvestWhip : ModProjectile
    {
        private const int SegmentCount = 30;
        private const int TelegraphDuration = 46;
        private const int LashDuration = 30;
        private const int ReleaseBlendTicks = 6;
        private const float WhipLength = 20f * 16f;
        private const int DamageStart = 8;
        private const int DamageEnd = 19;

        private readonly List<Vector2> _controlPoints = new List<Vector2>(SegmentCount);
        private readonly List<Vector2> _telegraphPoints = new List<Vector2>(SegmentCount);
        private readonly List<Vector2> _lashPoints = new List<Vector2>(SegmentCount);
        private int Age => (int)Projectile.localAI[0];
        private int LashAge => Age - TelegraphDuration;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/DarkHarvestWhip";

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
            Projectile.timeLeft = TelegraphDuration + LashDuration + 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
            => LashAge >= DamageStart && LashAge <= DamageEnd ? null : false;

        public override void AI()
        {
            if (!TryGetOwner(out DarkBloodKnight owner)
                || !owner.IsBloodWhipActive
                || Age >= TelegraphDuration + LashDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.BloodWhipAnchor;
            Projectile.direction = owner.NPC.direction;
            Projectile.spriteDirection = owner.NPC.direction;

            // Track throughout the readable portion of the tell, then commit for its last ten ticks.
            // The owner performs the same lock, so clients derive the same curve from synchronized NPC
            // state while the server sends one final projectile aim snapshot at commitment.
            if (Age < TelegraphDuration - 10)
                Projectile.velocity = owner.BloodWhipAim;
            else if (Age == TelegraphDuration - 10 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.netUpdate = true;

            BuildControlPoints(owner.BloodWhipAnchor);

            if (Age == TelegraphDuration)
                SoundEngine.PlaySound(SoundID.Item152 with { Volume = 0.7f, Pitch = -0.12f }, Projectile.Center);

            if (LashAge == DamageStart)
            {
                SoundEngine.PlaySound(SoundID.Item153 with { Volume = 0.8f, Pitch = -0.08f }, WhipTip);
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(WhipTip,
                            Projectile.ai[1] > 0.5f ? DustID.Shadowflame : DustID.Blood,
                            Main.rand.NextVector2Circular(2.2f, 2.2f), 70, default, 0.9f);
                        dust.noGravity = true;
                    }
                }
            }

            Projectile.localAI[0]++;
        }

        private Vector2 WhipTip => _controlPoints.Count > 0 ? _controlPoints[^1] : Projectile.Center;

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

            float segmentLength = WhipLength * extension / (SegmentCount - 1f);
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
                    _controlPoints[i], _controlPoints[i + 1], 18f, ref collisionPoint))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            DarkBloodKnight.ApplyBloodArrowDebuffs(target, blackfire: false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_controlPoints.Count != SegmentCount)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            for (int i = 0; i < _controlPoints.Count - 1; i++)
            {
                Vector2 from = _controlPoints[i];
                Vector2 to = _controlPoints[i + 1];
                Vector2 delta = to - from;
                int frame = SegmentFrame(i);
                Rectangle source = texture.Frame(1, 5, 0, frame);
                Vector2 origin = source.Size() * 0.5f;
                float rotation = delta.ToRotation() - MathHelper.PiOver2;
                float stretch = Math.Max(0.5f, delta.Length() / Math.Max(1f, source.Height));
                Color color = Lighting.GetColor(from.ToTileCoordinates());

                Main.EntitySpriteDraw(texture, from - Main.screenPosition, source, color,
                    rotation, origin, new Vector2(1f, stretch), SpriteEffects.None, 0);
            }
            return false;
        }

        private static int SegmentFrame(int segment)
        {
            if (segment == 0)
                return 0;
            if (segment >= SegmentCount - 2)
                return 4;
            return 1 + segment % 3;
        }

        private bool TryGetOwner(out DarkBloodKnight knight)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC owner = Main.npc[ownerIndex];
                if (owner.active && owner.ModNPC is DarkBloodKnight bloodKnight)
                {
                    knight = bloodKnight;
                    return true;
                }
            }

            knight = null;
            return false;
        }
    }
}
