using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Darkmoon;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public partial class GhostOfTheDarkmoonKnight
    {
        // SwingPreview: 224deg envelope; Mooncut tell26, Weighted8/28 k6, peak27.5deg/t,
        // live15 + harmless21 + recovery24. Crescent tell40, Weighted8/32 k6,
        // peak25.6deg/t, live16 + harmless24 + recovery36; wave releases at peak8.
        static MeleeComboStep CutStep(ComboMotion motion, int tell, int tail, float damage) => new MeleeComboStep
        {
            Motion = motion, TelegraphTicks = tell, AttackTicks = 8 + tail,
            DamageMult = damage, ReachMult = 1f, SwingSpeedMult = 1f,
            Ease = SwingEaseStyle.Weighted, EaseInTicks = 8, EaseOutTicks = tail, EaseOutDecay = 6f,
            HitWindowEnd = (8f + tail * 1.204f / 6f) / (8 + tail),
        };
        static readonly MeleeCombo[] Combos = {
            new MeleeCombo { Name = CutName, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                RecoveryTicks = 24, InitialFlashColor = Accent, Steps = new[] { CutStep(ComboMotion.OverheadArc, 26, 28, 1f) } },
            new MeleeCombo { Name = LeapName, BaseWeight = 100, Preferred = ComboRangeBand.Mid, RangedStartOnly = true,
                RecoveryTicks = 52, InitialFlashColor = Accent, Steps = new[] { new MeleeComboStep {
                    Motion = ComboMotion.LeapSlam, TelegraphTicks = 40, AttackTicks = 120, DamageMult = 1.25f,
                    ReachMult = 1f, SwingSpeedMult = 1f, LeapForwardSpeedMult = 1f, LeapHeightMult = 1f } } },
            new MeleeCombo { Name = CrescentName, BaseWeight = 100, Preferred = ComboRangeBand.Any, RangedStartOnly = true,
                RecoveryTicks = 36, InitialFlashColor = Accent, Steps = new[] { CutStep(ComboMotion.UnderhandArc, 40, 32, 280f / 240f) } },
        };
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction) => combo.Name == _requestedCombo;
        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress) => progress < step.HitWindowEnd;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }
        public int MeleeDangerRemaining => Phase == AttackPhase.MeleeComboTelegraph ? PhaseTimer + (ActiveMeleeComboName == LeapName ? 120 : 15)
            : Phase == AttackPhase.MeleeComboAttack ? Math.Max(0, ActiveMeleeComboName == LeapName ? PhaseTimer : 15 - ((ActiveMeleeComboName == CutName ? 36 : 40) - PhaseTimer)) : 0;

        public override void AI()
        {
            _age++;
            TickDown(ref _cutCooldown); TickDown(ref _leapCooldown); TickDown(ref _crescentCooldown);
            TickDown(ref _barrageCooldown); TickDown(ref _vigilCooldown); TickDown(ref _guardCooldown);
            TickDown(ref _restTicks); TickDown(ref _blockSoundCooldown); TickDown(ref _approachTicks); TickDown(ref _healRetryTicks);
            if (Server && !NPC.HasValidTarget) NPC.TargetClosest();
            if (Server && _serial == 0) { _serial = DarkmoonVigilController.NewSerial(); Snapshot(); }
            if (Server && !_phaseTwo && NPC.life < NPC.lifeMax * 0.5f) { _phaseTwo = true; Snapshot(); }
            bool shots = HasOwnedShots();
            if (shots) _graceTicks = 30; else TickDown(ref _graceTicks);
            bool hasVigil = Vigil != null;
            if (Server && _vigilWasPresent && !hasVigil) { _vigilCooldown = 600; Snapshot(); }
            _vigilWasPresent = hasVigil;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (Server)
            {
                if (Neutral) _neutralTicks++; else _neutralTicks = 0;
                if (Neutral && NPC.HasValidTarget && NPC.velocity.X * NPC.direction < -0.2f)
                { if (++_kiteTicks >= 180) { _kiteTicks = 0; _approachTicks = 90; Snapshot(); } }
                else _kiteTicks = 0;
                UpdateGuard(g);
                if (Neutral && NPC.HasValidTarget && NPC.velocity.Y == 0f && _restTicks == 0 && _graceTicks == 0
                    && !shots && g.StaggerTimer == 0 && g.TeleportCountdown == 0 && !InFinisherPunishWindow
                    && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height)) SelectMove(Main.player[NPC.target]);
            }
            AttackPhase before = Phase;
            base.AI();
            g.AttackCommitted = false; g.AttackTelegraphing = !Neutral;
            if (!Neutral && _guardUp) DropGuard();
            if (_guardUp && Neutral) NPC.direction = NPC.spriteDirection = _guardFacing;
            g.ShieldGuarding = _guardUp && Neutral;
            g.SuppressGhostWallTeleport |= SuppressGhostWallTeleport;
            if (Server && before == AttackPhase.Healing && Neutral && _estusCharges == 0) { _restTicks = 30; Trace("estus-complete"); Snapshot(); }
            if (Server && Casting && (_castTarget < 0 || _castTarget >= Main.maxPlayers || !Main.player[_castTarget].active || Main.player[_castTarget].dead)) OnStagger(NPC);
            if (Server && Neutral && _cast != Cast.None) { _completedActions++; _cast = Cast.None; Snapshot(); }
            DebugAttackLabel = _guardUp ? "Darkmoon guard" : Phase == AttackPhase.Healing ? "Darkmoon Estus" : Casting ? _cast == Cast.Barrage ? $"Shadow Barrage ({_shotCount})" : "Darkmoon Vigil" : !Neutral ? ActiveMeleeComboName : hasVigil ? "Vigil — melee pursuit" : null;
            if (!Main.dedServ && Main.rand.NextBool(3)) Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y, 120, Accent, 0.65f);
        }
        static void TickDown(ref int value) { if (value > 0) value--; }
        void Trace(string kind)
        {
            if (Server && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                Mod.Logger.Info("DARKMOON " + System.Text.Json.JsonSerializer.Serialize(new {
                    kind, serial = _serial, npc = NPC.whoAmI, tick = Main.GameUpdateCount,
                    phase = Phase.ToString(), remaining = PhaseTimer, shots = _shotCount,
                    releases = _releasedMask, guard = _guardUp, guardTicks = _guardTicks,
                    guardCooldown = _guardCooldown, life = NPC.life, phaseTwo = _phaseTwo }));
        }
        void Snapshot() { NPC.netUpdate = true; RequestNetworkSnapshot(); }
        void SelectMove(Player target)
        {
            float distance = NPC.Distance(target.Center);
            if (!Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) return;
            if (_estusCharges > 0 && _healRetryTicks == 0 && NPC.life < NPC.lifeMax * 0.5f && (Vigil == null || Vigil.QuietFor(150))) { DropGuard(); return; }
            if (_completedActions > 0 && _age >= 90 && _vigilCooldown == 0 && Vigil == null && distance >= 128 && distance <= 480 && DarkmoonVigilController.CanForm(NPC.Center))
            { _vigilAnchor = NPC.Center; StartCast(Cast.Vigil, target); return; }
            _bag ??= new AttackBag<Move>(cards => { cards.Add(Move.Cut); cards.Add(Move.Cut); cards.Add(Move.Leap); cards.Add(Move.Crescent); cards.Add(Move.Barrage); });
            bool Eligible(Move move)
            {
                if (_bag.Last == move && _neutralTicks < 30) return false;
                switch (move)
                {
                    case Move.Cut: return _cutCooldown == 0 && distance <= 72 && Math.Abs(target.Center.Y - NPC.Center.Y) < 48 && (Vigil == null || Vigil.CanMeleeStart(26, 14));
                    case Move.Leap: return _leapCooldown == 0 && distance >= 96 && distance <= 256 && CanLeap(target) && (Vigil == null || Vigil.CanMeleeStart(66, 14));
                    case Move.Crescent: return _crescentCooldown == 0 && Vigil == null && distance >= 64 && distance <= 384;
                    case Move.Barrage: return _barrageCooldown == 0 && Vigil == null && distance >= 160 && distance <= 640;
                    default: return false;
                }
            }
            if (!_bag.TryDraw(Eligible, out Move chosen)) return;
            DropGuard(); NPC.direction = NPC.spriteDirection = target.Center.X < NPC.Center.X ? -1 : 1;
            if (chosen == Move.Barrage) { StartCast(Cast.Barrage, target); return; }
            _requestedCombo = chosen == Move.Cut ? CutName : chosen == Move.Leap ? LeapName : CrescentName;
            _releasedMask = 0;
            if (TryStartMeleeCombo(distance, rangedStartOnly: chosen != Move.Cut)) Snapshot();
        }
        bool CanLeap(Player target)
        {
            float dx = target.Center.X + Math.Sign(target.Center.X - NPC.Center.X) * 32f - NPC.Center.X;
            if (Math.Abs(target.Bottom.Y - NPC.Bottom.Y) > 48f) return false;
            for (int t = 4; t <= 40; t += 4)
                if (Collision.SolidCollision(NPC.position + new Vector2(dx * t / 40f, -6f * t + 0.15f * t * t), NPC.width, NPC.height)) return false;
            Vector2 landing = new Vector2(NPC.position.X + dx, target.Bottom.Y - NPC.height);
            return !Collision.SolidCollision(landing, NPC.width, NPC.height) && Collision.TileCollision(landing, new Vector2(0, 16), NPC.width, NPC.height).Y < 16f;
        }
        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (elapsed == 0 && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
            if (combo.Name == CrescentName && elapsed <= total - 10 && Server && NPC.HasValidTarget)
            { _castAim = (Main.player[NPC.target].Center - PuppetWeaponTipPosition(63)).SafeNormalize(new Vector2(NPC.direction, 0)); if (elapsed == total - 10) Snapshot(); }
            if (!Main.dedServ && elapsed % 2 == 0) DarkmoonVisuals.Motes(PuppetWeaponTipPosition(63), Accent, 2, 8f);
        }
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Server && combo.Name == CrescentName && elapsed >= 8 && _releasedMask == 0)
            {
                _releasedMask = 1;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), PuppetWeaponTipPosition(63), _castAim * 12f, ModContent.ProjectileType<DarkmoonCrescent>(), EnemyDamage.Projectile(CrescentExpertDamage), 3f, Main.myPlayer, NPC.whoAmI, _serial);
                Snapshot();
            }
        }
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            if (!Server) return;
            _completedActions++;
            if (ActiveMeleeComboName == CutName) _cutCooldown = 24 + 30;
            if (ActiveMeleeComboName == LeapName) _leapCooldown = 52 + 240;
            if (ActiveMeleeComboName == CrescentName) _crescentCooldown = 36 + 180;
            Snapshot();
        }
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        { if (!Main.dedServ) { DarkmoonVisuals.Motes(NPC.Bottom, new Color(55, 55, 65), 24, 18); UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8, distanceFalloff: 450); } }
        bool HasOwnedShots()
        {
            if (_serial == 0) return false;
            for (int i = 0; i < Main.maxProjectiles; i++) if (Main.projectile[i].active && Main.projectile[i].ModProjectile is DarkmoonOwnedShot shot && shot.OwnedBy(NPC.whoAmI, _serial) && shot.IsLive) return true;
            return false;
        }
    }
}
