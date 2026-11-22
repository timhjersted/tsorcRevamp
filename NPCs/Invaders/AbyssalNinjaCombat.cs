using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    public partial class AbyssalNinja
    {
        private enum Stage : byte { None, Transition, BackTell, BackCut, BackAir, EclipseTell, EclipseAir,
            NeedleReady, NeedleTell, NeedleFire, CrossbowReady, CrossbowTell, CrossbowFire,
            MeridianTell, MeridianDash, MeridianWait, NightTell, NightVanish, NightAppear, NightHold, NightDive, Recovery }
        private Stage _stage;
        private int _stageTicks;
        private bool _inCustomTick;
        private int _facing = 1;
        private int _lockedTarget = -1;
        private float _poseRotation = -0.3f;
        private float _bodySpin;
        private bool _customLive;
        private bool _customArmed;
        private int _customDamage;
        private Vector2 _aim = Vector2.UnitX;
        private Vector2 _origin;
        private Vector2 _destination;
        private Vector2 _landing;
        private Vector2 _launch;
        private int _strikeAt;
        private int _shotsFired;
        private int _nextShot;
        private int _pattern;
        private int _clearTicks;
        private int _passes;
        private bool _nightCut;
        private bool _firePending;
        private bool _landingEffectPlayed;
        private static readonly int[][] CrossbowPauses = { Array.Empty<int>(), new[] { 30, 60 }, new[] { 30, 30, 30, 5, 5 } };
        protected override bool SlowDownDuringCustom => false;
        protected override float? CustomWeaponRotation => _stage != Stage.None ? _poseRotation : null;
        protected override float PuppetBodySpinRotation => _stage != Stage.None ? _bodySpin : 0f;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom || Neutral;
        protected override bool CustomAttackCommitted => _stage == Stage.MeridianDash || _stage == Stage.NightDive || _customLive;
        protected override bool ForceWingsSpread => IsSwordSequence && ActiveMeleeComboName == LeapName
            || (Phase == AttackPhase.Custom && (_stage == Stage.EclipseAir || _stage == Stage.NightTell
                || _stage == Stage.NightAppear || _stage == Stage.NightHold || _stage == Stage.NightDive));
        protected override bool SuppressSlashVFXForCurrentPhase => Phase == AttackPhase.Custom
            || (Phase == AttackPhase.MeleeComboAttack && ((ActiveMeleeComboName == VeilName && PhaseTimer < 23)
                || (ActiveMeleeComboName == UndertowName && PhaseTimer < 25)));
        protected override float MeleeCompositeArmRotationOffset => _move == Move.Crossbow && Phase == AttackPhase.Custom && _facing < 0 ? -2 * _poseRotation : 0;
        protected override Vector2? ResolveCompositeHandAnchor(float rotation, Player.CompositeArmStretchAmount stretch)
        {
            // Exact vanilla GetFrontHandPosition, also used when a dedicated server has no draw Player.
            float radius = stretch == Player.CompositeArmStretchAmount.Full ? 10 : stretch == Player.CompositeArmStretchAmount.ThreeQuarters ? 8
                : stretch == Player.CompositeArmStretchAmount.Quarter ? 6 : 4;
            float angle = rotation + MathHelper.PiOver2;
            return NPC.Center + new Vector2(0, NPC.gfxOffY) + angle.ToRotationVector2() * radius
                + new Vector2(-4 * NPC.direction, -2) + new Vector2(0, 3 * NPC.direction).RotatedBy(angle);
        }
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            if (_bodySpin == 0) return;
            Vector2 pivot = PuppetBodySpinPivot;
            origin = pivot + (origin - pivot).RotatedBy(_bodySpin);
            tip = pivot + (tip - pivot).RotatedBy(_bodySpin);
        }
        private void StartSpecial(Move move, Player target)
        {
            _poseRotation = -0.3f; _bodySpin = 0; _passes = 0;
            switch (move)
            {
                case Move.Blackstep: BeginStage(Stage.BackTell, 28); break;
                case Move.Eclipse: BeginStage(Stage.EclipseTell, 30); break;
                case Move.Needles: BeginStage(Stage.NeedleReady, 30); break;
                case Move.Crossbow:
                    int roll = Main.rand.Next(100); _pattern = roll < 55 ? 0 : roll < 90 ? 1 : 2;
                    BeginStage(Stage.CrossbowReady, 30); break;
                case Move.Meridian: BeginStage(Stage.MeridianTell, 44); break;
                case Move.Nightfall: BeginStage(Stage.NightTell, 32); break;
            }
        }
        private void BeginStage(Stage stage, int ticks)
        {
            _stage = stage; _stageTicks = ticks; _customLive = false; _customArmed = false; StopMeleeHit();
            if (stage == Stage.BackAir || stage == Stage.EclipseAir || stage == Stage.NightDive) _landingEffectPlayed = false;
            int weapon = stage == Stage.NeedleReady || stage == Stage.NeedleTell || stage == Stage.NeedleFire ? RangedWeaponItemType
                : stage == Stage.CrossbowReady || stage == Stage.CrossbowTell || stage == Stage.CrossbowFire ? SecondaryRangedWeaponItemType : MeleeWeaponItemType;
            if (weapon == SecondaryRangedWeaponItemType) ConfigureCustomRangedWeapon(weapon, RangedStyle.Crossbow);
            else if (weapon == RangedWeaponItemType) ConfigureCustomRangedWeapon(weapon, RangedStyle.Throw);
            // The base decrements PhaseTimer after this hook; preserve a full first frame on handoff.
            StartCustomAttack(ticks + (_inCustomTick ? 1 : 0), weapon);
            DebugAttackLabel = _move.ToString() + " / " + stage;
            NPC.netUpdate = true;
        }
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (_stage == Stage.None) return;
            _inCustomTick = true;
            try
            {
                int elapsed = Math.Max(0, _stageTicks - ticksRemaining);
                bool last = ticksRemaining <= 1;
                NPC.direction = NPC.spriteDirection = _facing;
                NPC.dontTakeDamage = _stage == Stage.NightVanish;
                _customLive = false;
                if (!ValidLockedTarget() && _move != Move.Transition && _stage != Stage.Recovery)
                {
                    if (Authority) { KillOwnedHazards(); Recover(28); }
                    return;
                }
                Player target = ValidLockedTarget() ? Main.player[_lockedTarget] : null;
                switch (_stage)
                {
                    case Stage.Transition:
                        NPC.velocity.X *= 0.75f; _poseRotation = HighPose;
                        if (elapsed % 4 == 0) Smoke(PuppetWeaponTipPosition(80), 4);
                        if (elapsed == 0) Cue(SoundID.Item8);
                        break;
                    case Stage.BackTell:
                        Brake(); _poseRotation = MathHelper.SmoothStep(-0.3f, LowPose, (elapsed + 1) / 28f);
                        TellSmoke(elapsed);
                        if (last && Authority) BeginStage(Stage.BackCut, 40);
                        break;
                    case Stage.BackCut:
                        _poseRotation = AbyssalNinjaMotion.BackCut(elapsed);
                        _customLive = elapsed <= 14; _customDamage = 300;
                        if (elapsed < 15) NPC.velocity.X = _facing * WeightedTravelSpeed(elapsed, 32);
                        else if (elapsed == 15) { NPC.velocity = new Vector2(-_facing * 4, -7.2f); Smoke(NPC.Bottom, 18); }
                        else NPC.velocity.X = -_facing * 4;
                        if (elapsed >= 15) _bodySpin = AbyssalNinjaMotion.BackSpin(elapsed, _facing);
                        if (last && Authority) BeginStage(Stage.BackAir, 100);
                        break;
                    case Stage.BackAir:
                        _poseRotation = MathHelper.Lerp(_poseRotation, -0.3f, 0.1f);
                        _bodySpin = 0; NPC.velocity.X = -_facing * 4;
                        if (elapsed > 1 && NPC.velocity.Y == 0) LandingEffect();
                        if ((elapsed > 1 && NPC.velocity.Y == 0) || last) { if (Authority) Recover(30); }
                        break;
                    case Stage.EclipseTell:
                        Brake(); _poseRotation = MathHelper.SmoothStep(-0.3f, LowPose, (elapsed + 1) / 30f); TellSmoke(elapsed);
                        if (elapsed == 18 && Authority && !LockEclipse(target)) { Recover(24); break; }
                        if (last && Authority) { NPC.velocity = _launch; BeginStage(Stage.EclipseAir, 100); }
                        break;
                    case Stage.EclipseAir:
                        NPC.velocity.X = _launch.X;
                        // Only a short intercept is live. The remainder of the somersault is cosmetic.
                        _bodySpin = AbyssalNinjaMotion.RisingSpin(elapsed, _strikeAt, _facing);
                        _poseRotation = AbyssalNinjaMotion.RisingCut(elapsed, _strikeAt);
                        _customLive = elapsed >= _strikeAt && elapsed < _strikeAt + 10; _customDamage = 340;
                        if (elapsed > _strikeAt + 18) _bodySpin = 0;
                        if (elapsed > 4 && NPC.velocity.Y == 0) LandingEffect();
                        if ((elapsed > 4 && NPC.velocity.Y == 0) || last) { if (Authority) Recover(32); }
                        break;
                    case Stage.NeedleReady: case Stage.CrossbowReady:
                        Brake(); _poseRotation = 0;
                        if (last && Authority) { _shotsFired = 0; BeginStage(_move == Move.Needles ? Stage.NeedleTell : Stage.CrossbowTell,
                            _move == Move.Needles ? 40 : 32 + (_pattern == 1 ? 10 : _pattern == 2 ? 30 : 0)); }
                        break;
                    case Stage.NeedleTell: case Stage.CrossbowTell:
                        Brake();
                        int lockTick = _stageTicks - 12;
                        if (elapsed <= lockTick && Authority)
                        {
                            _aim = (target.Center - PuppetHandPosition).SafeNormalize(new Vector2(_facing, 0));
                            _facing = _aim.X < 0 ? -1 : 1;
                            if (elapsed == lockTick) NPC.netUpdate = true;
                        }
                        _poseRotation = _move == Move.Crossbow ? RangedPoseAngle() : MathHelper.SmoothStep(-1.2f, 0.3f, (elapsed + 1f) / _stageTicks);
                        TellSmoke(elapsed);
                        if (elapsed == lockTick && Authority) SpawnTelegraphFlash(_move == Move.Crossbow
                            ? (_pattern == 0 ? Color.White : _pattern == 1 ? Color.Yellow : Color.Red) : AbyssColor);
                        if (last && Authority) { _nextShot = 0; _clearTicks = 0; BeginStage(_move == Move.Needles ? Stage.NeedleFire : Stage.CrossbowFire, 420); }
                        break;
                    case Stage.NeedleFire: case Stage.CrossbowFire:
                        Brake(); _poseRotation = _move == Move.Crossbow ? RangedPoseAngle() : 0.3f - 0.25f * (1 - MathHelper.Clamp((elapsed - _nextShot + 12) / 12f, 0, 1));
                        int count = _move == Move.Needles ? 3 : CrossbowPauses[_pattern].Length + 1;
                        if (Authority && _shotsFired < count && elapsed >= _nextShot)
                        {
                            _firePending = true; _shotsFired++;
                            _nextShot = elapsed + (_move == Move.Needles ? 12 : _shotsFired < count ? 10 + CrossbowPauses[_pattern][_shotsFired - 1] : 0);
                            NPC.netUpdate = true;
                        }
                        if (_shotsFired >= count)
                        {
                            if (Authority) _clearTicks = HasOwnedHazards() ? 0 : _clearTicks + 1;
                            int sinceLastShot = elapsed - _nextShot + (_move == Move.Needles ? 12 : 0);
                            if (Authority && _clearTicks >= 30 && sinceLastShot >= (_move == Move.Needles ? 36 : 50)) Recover(1);
                        }
                        if (last && Authority) { KillOwnedHazards(); Recover(36); }
                        break;
                    case Stage.MeridianTell:
                        Brake(); _poseRotation = MathHelper.SmoothStep(-0.3f, HighPose, (elapsed + 1) / 44f); TellSmoke(elapsed);
                        if (elapsed <= 32 && Authority)
                        {
                            _origin = NPC.Center;
                            if (!TryMeridianPath(target, out _destination)) { Recover(24); break; }
                            _facing = _destination.X < NPC.Center.X ? -1 : 1;
                            if (elapsed == 32)
                            {
                                float gap = Math.Abs(target.Center.X - _origin.X);
                                float wanted = MathHelper.Clamp(gap / Math.Max(1, Math.Abs(_destination.X - _origin.X)), 0.15f, 0.85f);
                                _strikeAt = Math.Clamp((int)(32 * wanted) - 5, 3, 22);
                                NPC.netUpdate = true;
                            }
                        }
                        if (last && Authority)
                        {
                            BeginStage(Stage.MeridianDash, 32);
                        }
                        break;
                    case Stage.MeridianDash:
                        NPC.noGravity = true;
                        float speed = Math.Abs(_destination.X - _origin.X) / 25f;
                        float fraction = elapsed < 6 ? (elapsed + 0.5f) / 6f : elapsed < 24 ? 1f : (31.5f - elapsed) / 8f;
                        Vector2 desired = new Vector2(_facing * speed * fraction, 0);
                        Vector2 safe = Collision.TileCollision(NPC.position, desired, NPC.width, NPC.height);
                        NPC.velocity = safe;
                        _poseRotation = AbyssalNinjaMotion.PassingCut(elapsed, _strikeAt);
                        _customLive = elapsed >= _strikeAt && elapsed < _strikeAt + 10; _customDamage = 340;
                        if ((safe.X != desired.X || last) && Authority)
                        {
                            _destination = NPC.Center + safe;
                            SpawnSeam(_origin, _destination); _passes++;
                            NPC.velocity = Vector2.Zero; NPC.noGravity = false;
                            BeginStage(Stage.MeridianWait, 82);
                        }
                        break;
                    case Stage.MeridianWait:
                        Brake(); _poseRotation = MathHelper.Lerp(_poseRotation, -0.3f, 0.1f);
                        if (last && Authority)
                        {
                            if (_tier >= 2 && _passes < 2 && EligibleReturn(target)) BeginStage(Stage.MeridianTell, 44);
                            else Recover(0);
                        }
                        break;
                    case Stage.NightTell:
                        Brake(); _poseRotation = MathHelper.SmoothStep(-0.3f, HighPose, (elapsed + 1) / 32f); TellSmoke(elapsed);
                        if (elapsed == 20 && Authority)
                        {
                            if (!TryNightfallSite(target, out _destination, out _landing)) { Recover(24); break; }
                            NPC.netUpdate = true;
                        }
                        if (last && Authority) { Smoke(NPC.Center, 30); BeginStage(Stage.NightVanish, 12); }
                        break;
                    case Stage.NightVanish:
                        NPC.noGravity = true; NPC.velocity = Vector2.Zero; _poseRotation = HighPose;
                        if (last && Authority)
                        {
                            if (!ClearBody(_destination) || !PathClear(_destination, _landing)) { Recover(24); break; }
                            NPC.Center = _destination;
                            _facing = _landing.X < NPC.Center.X ? -1 : 1; // Face the committed descent after crossing sides.
                            NPC.netUpdate = true;
                            BeginStage(Stage.NightAppear, 12); Smoke(NPC.Center, 30);
                        }
                        break;
                    case Stage.NightAppear: case Stage.NightHold:
                        NPC.alpha = _stage == Stage.NightAppear ? (int)(255 * (1 - (elapsed + 1) / 12f)) : 0;
                        NPC.noGravity = true; NPC.velocity = Vector2.Zero; _poseRotation = HighPose;
                        if (last && Authority)
                        {
                            if (_stage == Stage.NightAppear) BeginStage(Stage.NightHold, 30);
                            else {
                                float drop = _landing.Y - NPC.Center.Y;
                                float time = (float)Math.Sqrt(2 * Math.Max(1, drop) / 0.3f);
                                _launch = new Vector2(MathHelper.Clamp((_landing.X - NPC.Center.X) / time, -7, 7), 0);
                                NPC.velocity = _launch; NPC.noGravity = false; _nightCut = false; _strikeAt = -1;
                                BeginStage(Stage.NightDive, 110);
                            }
                        }
                        break;
                    case Stage.NightDive:
                        NPC.noGravity = false; NPC.velocity.X = elapsed > 3 && NPC.velocity.Y == 0 ? 0 : _launch.X;
                        if (elapsed > 3 && NPC.velocity.Y == 0) LandingEffect();
                        float remainingDrop = _landing.Y - NPC.Center.Y;
                        if (!_nightCut && (remainingDrop <= Math.Max(52, NPC.velocity.Y * 10) || (elapsed > 3 && NPC.velocity.Y == 0)))
                        { _nightCut = true; _strikeAt = elapsed; }
                        if (_nightCut)
                        {
                            int cutTick = elapsed - _strikeAt;
                            _poseRotation = AbyssalNinjaMotion.FallingCut(cutTick);
                            _customLive = cutTick < 10; _customDamage = 400;
                            if (cutTick >= 10) NPC.velocity.X *= 0.7f;
                            if (cutTick >= 38 && NPC.velocity.Y == 0 && Authority) Recover(48);
                        }
                        if (last && Authority) Recover(48);
                        break;
                    case Stage.Recovery:
                        NPC.noGravity = false; NPC.dontTakeDamage = false; Brake(); _bodySpin = 0;
                        if (elapsed > 8) _poseRotation = MathHelper.Lerp(_poseRotation, -0.3f, 0.1f);
                        break;
                }
                if (_customLive && elapsed % 2 == 0) Smoke(PuppetWeaponTipPosition(80), 1);
            }
            finally
            {
                // Clients hold the last authored frame until the server's next stage snapshot.
                if (!Authority && ticksRemaining <= 1 && Phase == AttackPhase.Custom) PhaseTimer = 2;
                _inCustomTick = false;
            }
        }
        private bool ValidLockedTarget() => _lockedTarget >= 0 && _lockedTarget < Main.maxPlayers
            && Main.player[_lockedTarget].active && !Main.player[_lockedTarget].dead;
        private void Recover(int ticks) { NPC.noGravity = false; NPC.dontTakeDamage = false; NPC.alpha = 0; _bodySpin = 0; BeginStage(Stage.Recovery, Math.Max(1, ticks)); }
        private void Brake() { NPC.velocity.X *= 0.7f; }
        private void LandingEffect()
        {
            if (_landingEffectPlayed) return;
            _landingEffectPlayed = true; Smoke(NPC.Bottom, 24); Cue(SoundID.Item14);
        }
        private void TellSmoke(int elapsed) { if (elapsed % 4 == 0) Smoke(PuppetHandPosition, 2); if (elapsed == 0) Cue(SoundID.Item7); }
        private float RangedPoseAngle() => _aim.ToRotation() - (_facing < 0 ? MathHelper.Pi : 0);
        private static float WeightedTravelSpeed(int tick, float distance)
        {
            float shape = tick < 8 ? tick * tick / 64f : (float)Math.Exp(-6 * (tick - 8) / 32f);
            return distance / (8f / 3 + 32f * (1 - (float)Math.Exp(-6)) / 6) * shape;
        }
        private void ClearCustom()
        {
            _stage = Stage.None; _bodySpin = 0; _customLive = _customArmed = _firePending = false; _inCustomTick = false;
            NPC.alpha = 0;
            NPC.noGravity = false; NPC.dontTakeDamage = false; StopMeleeHit();
        }
        private void FireShot()
        {
            bool bolt = _move == Move.Crossbow;
            Vector2 muzzle = PuppetHandPosition + (bolt ? new Vector2(2 * _facing, -2) + _aim * 35 : Vector2.Zero);
            int type = bolt ? ModContent.ProjectileType<AbyssalNinjaBolt>() : ModContent.ProjectileType<AbyssalNinjaStar>();
            Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, _aim * (bolt ? 14 : 12), type,
                bolt ? SecondaryRangedDamage : RangedDamage, bolt ? 4 : 2, Main.myPlayer, NPC.whoAmI, 0, _encounterId);
        }
        private void SpawnSeam(Vector2 start, Vector2 end)
        {
            int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), start, Vector2.Zero,
                ModContent.ProjectileType<AbyssalMeridianSeam>(), EnemyDamage.Projectile(300), 3, Main.myPlayer,
                NPC.whoAmI, 0, _encounterId);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is AbyssalMeridianSeam seam)
            { seam.End = end; Main.projectile[index].netUpdate = true; }
        }
        private bool Owned(Projectile p) => p.active && p.ai[0] == NPC.whoAmI && p.ai[2] == _encounterId
            && (p.ModProjectile is AbyssalNinjaShot || p.ModProjectile is AbyssalMeridianSeam);
        private bool HasOwnedHazards()
        {
            for (int i = 0; i < Main.maxProjectiles; i++) if (Owned(Main.projectile[i]) && Main.projectile[i].hostile) return true;
            return false;
        }
        private void KillOwnedHazards()
        {
            if (!Authority) return;
            for (int i = 0; i < Main.maxProjectiles; i++) if (Owned(Main.projectile[i])) Main.projectile[i].Kill();
        }

        private bool ClearBody(Vector2 center) => !Collision.SolidCollision(center - NPC.Size * 0.5f, NPC.width, NPC.height);
        private bool PathClear(Vector2 from, Vector2 to)
        {
            int samples = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / 8));
            for (int i = 0; i <= samples; i++) if (!ClearBody(Vector2.Lerp(from, to, i / (float)samples))) return false;
            return true;
        }
        private bool Ground(float x, float aroundFeetY, out Vector2 center)
        {
            int tileX = (int)(x / 16), tileY = (int)(aroundFeetY / 16);
            for (int offset = -8; offset <= 14; offset++)
            {
                int y = tileY + offset;
                if (!WorldGen.InWorld(tileX, y, 3) || !WorldGen.SolidTile(tileX, y)) continue;
                Vector2 candidate = new Vector2(x, y * 16 - NPC.height / 2f);
                if (ClearBody(candidate)) { center = candidate; return true; }
            }
            center = default; return false;
        }
        private bool ArcClear(Vector2 start, Vector2 velocity, float duration)
        {
            for (float t = 0; t < duration - 2; t += 2)
                if (!ClearBody(start + velocity * t + new Vector2(0, 0.15f * t * t))) return false;
            return true;
        }
        private bool TryLeapPath(Player target)
        {
            if (!Ground(target.Center.X, target.Bottom.Y, out Vector2 end)) return false;
            float disc = 9.5f * 9.5f + 0.6f * (end.Y - NPC.Center.Y);
            if (disc < 0) return false;
            float time = (9.5f + (float)Math.Sqrt(disc)) / 0.3f;
            float vx = (end.X - NPC.Center.X) / time;
            return time <= 95 && Math.Abs(vx) <= 6 && ArcClear(NPC.Center, new Vector2(vx, -9.5f), time);
        }
        private bool CanBackflip()
        {
            int facing = NPC.HasValidTarget && Main.player[NPC.target].Center.X < NPC.Center.X ? -1 : 1;
            return Ground(NPC.Center.X - facing * 192, NPC.Bottom.Y, out Vector2 end)
                && ArcClear(NPC.Center, new Vector2(-facing * 4, -7.2f), 48) && ClearBody(end);
        }
        private bool TryEclipsePath(Player target)
        {
            float rise = MathHelper.Clamp(NPC.Center.Y - target.Center.Y + 24, 64, 216);
            float up = Math.Min(11, (float)Math.Sqrt(0.6f * rise));
            float time = up / 0.3f;
            float vx = (target.Center.X - NPC.Center.X) / Math.Max(1, time);
            return Math.Abs(vx) <= 7 && Ground(NPC.Center.X + vx * time * 2, NPC.Bottom.Y, out _)
                && ArcClear(NPC.Center, new Vector2(vx, -up), time * 2);
        }
        private bool LockEclipse(Player target)
        {
            if (!TryEclipsePath(target)) return false;
            float up = Math.Min(11, (float)Math.Sqrt(0.6f * MathHelper.Clamp(NPC.Center.Y - target.Center.Y + 24, 64, 216)));
            float time = up / 0.3f;
            _launch = new Vector2(MathHelper.Clamp((target.Center.X - NPC.Center.X) / time, -7, 7), -up);
            _facing = _launch.X < 0 ? -1 : 1;
            _strikeAt = Math.Max(1, (int)time - 5); NPC.netUpdate = true; return true;
        }
        private bool TryMeridianPath(Player target, out Vector2 endpoint)
        {
            int dir = target.Center.X < NPC.Center.X ? -1 : 1;
            bool grounded = Ground(target.Center.X + dir * 64, NPC.Bottom.Y, out endpoint);
            return grounded && Math.Abs(endpoint.Y - NPC.Center.Y) <= 4 && PathClear(NPC.Center, endpoint);
        }
        private bool EligibleReturn(Player target) => NPC.Distance(target.Center) >= 160 && NPC.Distance(target.Center) <= 320 && TryMeridianPath(target, out _);
        private bool TryNightfallSite(Player target, out Vector2 arrival, out Vector2 landing)
        {
            arrival = landing = default;
            if (!Ground(target.Center.X, target.Bottom.Y, out Vector2 ground)) return false;
            int farSide = target.Center.X < NPC.Center.X ? -1 : 1;
            foreach (int side in new[] { farSide, -farSide })
                foreach (int height in new[] { 192, 144 })
                {
                    Vector2 site = ground + new Vector2(side * 144, -height);
                    if (!ClearBody(site) || !PathClear(site, ground) || !Collision.CanHitLine(site, 1, 1, target.Center, 1, 1)) continue;
                    float time = (float)Math.Sqrt(2 * height / 0.3f);
                    if (!ArcClear(site, new Vector2(-side * 144 / time, 0), time)) continue;
                    arrival = site; landing = ground; return true;
                }
            return false;
        }
    }
}
