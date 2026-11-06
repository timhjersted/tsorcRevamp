using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells
{
    /// <summary>
    /// Briar Lash vine. Follows a cubic Bezier the owner locked and showed as a corridor (four controls, synced in
    /// ExtraAI), cached as 32 arc-length samples so draw and damage read the same points.
    ///   age 1..20      the tip travels the whole arc (SmoothStep in arc length) - LIVE, 8px radius tip only
    ///   age 21..38     harmless retraction back along the same arc into the casting hand, then removed
    ///   age 1000..1012 cancelled (owner staggered / died / target lost): frozen in place, fades out
    ///   age 2000..2120 LATCHED: the tip hit a player, so the vine wraps them and tethers them to the free hand. Hold
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
        const int Cancelled = 1000;
        const int CancelFadeTicks = 12;
        const float TipRadius = 8f;
        const float DrawWidth = 20f;
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
        const int CoilCount = 2;
        const int CoilSegments = 10;
        const float CoilDrawWidth = 14f;

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
            // Hidden so DrawBehind can draw it over players: the coils wrap the held player's body.
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
            float[] cumulative = new float[SampleCount + 1];

            for (int i = 0; i <= SampleCount; i++)
            {
                float t = i / (float)SampleCount;
                float inverse = 1f - t;
                samples[i] = inverse * inverse * inverse * controls[0]
                    + 3f * inverse * inverse * t * controls[1]
                    + 3f * inverse * t * t * controls[2]
                    + t * t * t * controls[3];

                if (i > 0)
                {
                    arcLength += Vector2.Distance(samples[i - 1], samples[i]);
                }

                cumulative[i] = arcLength;
            }

            for (int i = 0; i <= SampleCount; i++)
            {
                arcFraction[i] = arcLength > 0f ? cumulative[i] / arcLength : i / (float)SampleCount;
            }
        }

        /// <summary>Point at an arc-length fraction (0 = halberd head, 1 = locked target point).</summary>
        Vector2 PointAt(float fraction)
        {
            fraction = MathHelper.Clamp(fraction, 0f, 1f);

            for (int i = 1; i <= SampleCount; i++)
            {
                if (_arcFraction[i] >= fraction)
                {
                    float span = _arcFraction[i] - _arcFraction[i - 1];
                    float local = span > 0f ? (fraction - _arcFraction[i - 1]) / span : 0f;
                    return Vector2.Lerp(_samples[i - 1], _samples[i], local);
                }
            }

            return _samples[SampleCount];
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

                // Tether: halberd tip to the held player's centre, sagging 24px. Rebuilt every tick on every machine
                // so it follows both bodies; the Sentry keeps fighting while it holds.
                NPC owner = Main.npc[(int)Projectile.ai[0]];
                if (owner.active && owner.ModNPC is DarkrootSentry sentry)
                {
                    _controls[0] = sentry.VineAnchorPosition;
                }

                _controls[3] = held.Center;
                Vector2 span = _controls[3] - _controls[0];
                Vector2 sag = new Vector2(0f, 24f);
                _controls[1] = _controls[0] + span * 0.33f + sag;
                _controls[2] = _controls[0] + span * 0.66f + sag;
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

                // One coil snaps per counted attack.
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
                // Latched -> released on this machine: the remaining coils burst off the player.
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

            float tipProgress = TipProgress(Age);
            if (tipProgress <= 0.001f)
            {
                return false;
            }

            float opacity = 1f;
            if (Age >= Cancelled && Age < LatchBase)
            {
                opacity = MathHelper.Clamp(1f - (Age - Cancelled) / CancelFadeTicks, 0f, 1f);
            }

            // One quad per sampled segment up to the tip; each carries its arc-length range so grain and taper run
            // continuously across segment seams. Quads overlap by 2px to hide joins.
            int visibleSegments = 0;
            WoodQuad[] quads = new WoodQuad[SampleCount];
            for (int i = 1; i <= SampleCount; i++)
            {
                float segmentStart = _arcFraction[i - 1];
                if (segmentStart >= tipProgress)
                {
                    break;
                }

                float segmentEnd = _arcFraction[i];
                Vector2 from = _samples[i - 1];
                Vector2 to = _samples[i];
                Vector2 delta = to - from;
                float length = delta.Length();

                quads[visibleSegments] = new WoodQuad
                {
                    Center = (from + to) * 0.5f,
                    Size = new Vector2(length + 2f, DrawWidth),
                    Rotation = delta.ToRotation(),
                    Origin = new Vector2(0.5f, 0.5f),
                    SegmentRange = new Vector2(segmentStart, segmentEnd),
                    ArcPixels = _arcLength,
                };
                visibleSegments++;
            }

            System.Array.Resize(ref quads, visibleSegments);

            // Active is repurposed for this technique: the arc fraction occupied by the 16px tip, so the shader can
            // swell and point the tip at a fixed pixel size whatever the arc length. Set only here.
            float tipFraction = _arcLength > 0f ? 16f / _arcLength : 0.05f;
            if (Latched)
            {
                // Wrapped: no tip swell. A near-zero tip fraction makes the shader draw plain cable to the end.
                tipFraction = 0.001f;
            }

            // Thorns and leaves first, so the cable drawn over them hides each one's root.
            DarkrootWoodVFX.Draw("DarkrootVineThorns", quads, tipProgress, tipFraction, opacity);
            DarkrootWoodVFX.Draw("DarkrootVine", quads, tipProgress, tipFraction, opacity);

            if (Latched)
            {
                // Coils around the held body, one per remaining strike (one snaps per counted attack): 14x6px
                // ellipses 8px above and 10px below centre. Progress 1 with a tiny Active draws plain cable.
                Player held = Main.player[_heldPlayer];
                int coilsLeft = CoilCount - _strikes;
                List<WoodQuad> coilQuads = new List<WoodQuad>();

                for (int coil = 0; coil < coilsLeft; coil++)
                {
                    float coilOffsetY = 10f;
                    if (coil == 0)
                    {
                        coilOffsetY = -8f;
                    }

                    Vector2 coilCenter = held.Center + new Vector2(0f, coilOffsetY);

                    for (int i = 0; i < CoilSegments; i++)
                    {
                        float startAngle = MathHelper.TwoPi * i / CoilSegments;
                        float endAngle = MathHelper.TwoPi * (i + 1) / CoilSegments;
                        Vector2 from = coilCenter + new Vector2(14f * (float)System.Math.Cos(startAngle), 6f * (float)System.Math.Sin(startAngle));
                        Vector2 to = coilCenter + new Vector2(14f * (float)System.Math.Cos(endAngle), 6f * (float)System.Math.Sin(endAngle));
                        Vector2 delta = to - from;
                        float grainStart = (coil * CoilSegments + i) * 0.03f;

                        coilQuads.Add(new WoodQuad
                        {
                            Center = (from + to) * 0.5f,
                            Size = new Vector2(delta.Length() + 2f, CoilDrawWidth),
                            Rotation = delta.ToRotation(),
                            Origin = new Vector2(0.5f, 0.5f),
                            SegmentRange = new Vector2(grainStart, grainStart + 0.03f),
                            // ~6.5px per coil segment over a 0.03 range: keeps the bark twist at the cable's scale.
                            ArcPixels = 220f,
                        });
                    }
                }

                DarkrootWoodVFX.Draw("DarkrootVine", coilQuads.ToArray(), 1f, 0.001f, opacity);
            }

            return false;
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

            if (hasPath)
            {
                BuildSamples(_controls, _samples, _arcFraction, out _arcLength);
                _hasPath = true;
            }
        }
    }
}
