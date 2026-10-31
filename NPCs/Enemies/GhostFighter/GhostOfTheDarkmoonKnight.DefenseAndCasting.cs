using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Projectiles.Enemy.Darkmoon;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public partial class GhostOfTheDarkmoonKnight
    {
        protected override int ShieldItemType => ModContent.ItemType<IceboundMythrilAegis>();
        protected override string ShieldDrawTexturePath => "tsorcRevamp/Content/Items/Accessories/Defensive/Shields/IceboundMythrilAegis_Shield";
        protected override bool ShieldVisible => _guardUp && Neutral;
        protected override bool ShieldRaisedForPose => ShieldVisible;
        protected override float? FreeBackArmFacingAngle => ShieldVisible ? 0f : null;
        protected override float? FreeFrontArmFacingAngle => BarragePose ? BarrageArmAngle : null;
        protected override int EstusChargesMax => 1;
        protected override float EstusHealFraction => 0.35f;
        protected override int HealAnimationTicks => 120;
        protected override float HealLifeCapFraction => 1f;
        protected override float SecondHealThreshold => 0f;
        protected override float RecentDamageThreshold => float.MaxValue;
        protected override int HealCooldownTicks => 600;
        protected override float FleeToHealDistance => 0f;
        protected override int FleeToHealMaxTicks => 1;
        protected override float FirstHealThreshold => CanDrink ? 0.5f : 0f;
        bool CanDrink => Neutral && _healRetryTicks == 0 && NPC.life < NPC.lifeMax * 0.5f && !_guardUp && _restTicks == 0
            && _graceTicks == 0 && !HasOwnedShots() && (Vigil == null || Vigil.QuietFor(150));
        protected override bool UseAuthoredMagicCastPose => true;
        protected override int MagicTelegraphTicks => _cast == Cast.Vigil ? 72 : 42;
        protected override int MagicAttackTicks => _cast == Cast.Vigil ? 1 : (_shotCount - 1) * 15 + 12;
        protected override int MagicRecoveryTicks => _cast == Cast.Vigil ? 11 : 36;
        protected override int MagicCooldownAfterUse => 0;
        protected override int MagicTelegraphFlashLeadTicks => _cast == Cast.Vigil ? 18 : 12;
        protected override Color MagicTelegraphFlashColor => Accent;
        protected override bool UseCompositeArmForAdditionalPhase => !BarragePose && Casting;
        protected override void ModifyAdditionalPhaseWeaponRotation(ref float rotation)
        {
            if (_cast == Cast.Vigil) rotation = Phase == AttackPhase.MagicTelegraph
                ? MathHelper.SmoothStep(-0.3f, -1.55f, MathHelper.Clamp((72 - PhaseTimer) / 48f, 0f, 1f)) : -1.55f;
        }
        void StartCast(Cast cast, Player target)
        {
            DropGuard(); _cast = cast; _castTarget = target.whoAmI;
            _shotCount = _phaseTwo ? 8 : 4; _releasedMask = 0;
            _castAim = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0));
            NPC.direction = NPC.spriteDirection = _castAim.X < 0 ? -1 : 1;
            if (cast == Cast.Barrage) _barrageCooldown = (_shotCount == 8 ? 300 : 240) + 42 + MagicAttackTicks + 36;
            OnMagicTelegraphStarting(); EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks); Trace("cast-start"); Snapshot();
        }
        public static float BarrageRecoil(int cycle) => cycle < 4 ? cycle / 4f : MathHelper.Clamp((12 - cycle) / 8f, 0, 1);
        float BarrageArmAngle
        {
            get
            {
                float aim = (float)Math.Atan2(_castAim.Y, _castAim.X * NPC.direction);
                if (Phase == AttackPhase.MagicTelegraph)
                {
                    int elapsed = 42 - PhaseTimer;
                    return MathHelper.SmoothStep(-1.1f, aim - 0.35f, MathHelper.Clamp(elapsed / 18f, 0f, 1f))
                        + 0.35f * MathHelper.SmoothStep(0, 1, MathHelper.Clamp((elapsed - 30) / 12f, 0, 1));
                }
                if (Phase == AttackPhase.MagicRecovery) return aim;
                return aim - BarrageRecoil(Math.Max(0, MagicAttackTicks - PhaseTimer) % 15) * 0.28f;
            }
        }
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Server && _cast == Cast.Barrage && progress <= 30f / 42f && NPC.HasValidTarget)
            {
                _castAim = (Main.player[_castTarget].Center - PuppetHandPosition).SafeNormalize(new Vector2(NPC.direction, 0));
                NPC.direction = NPC.spriteDirection = _castAim.X < 0 ? -1 : 1;
                if (progress >= 29f / 42f) Snapshot();
            }
        }
        protected override void DoMagicAttack()
        {
            _magicAttackTicksOverride = MagicAttackTicks;
            if (!Server) return;
            if (_cast == Cast.Vigil)
            {
                if (DarkmoonVigilController.CanForm(_vigilAnchor))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), _vigilAnchor, Vector2.Zero, ModContent.ProjectileType<DarkmoonVigilController>(), 0, 0, Main.myPlayer, _castTarget, NPC.whoAmI, _serial);
                    _vigilWasPresent = true;
                }
                else _vigilCooldown = 600;
            }
            else ReleaseBolt(0);
            Snapshot();
        }
        protected override void DoMagicTick(int remaining)
        {
            if (!Server || _cast != Cast.Barrage) return;
            NPC.direction = NPC.spriteDirection = _castAim.X < 0 ? -1 : 1;
            int elapsed = MagicAttackTicks - remaining + 1;
            if (elapsed % 15 == 0 && elapsed / 15 < _shotCount) ReleaseBolt(elapsed / 15);
        }
        void ReleaseBolt(int ordinal)
        {
            if ((_releasedMask & (1 << ordinal)) != 0) return;
            _releasedMask |= 1 << ordinal;
            Trace("barrage-release");
            Projectile.NewProjectile(NPC.GetSource_FromAI(), PuppetHandPosition, _castAim * 14f,
                ModContent.ProjectileType<DarkmoonShadowShot>(), EnemyDamage.Projectile(BoltExpertDamage), 3, Main.myPlayer, NPC.whoAmI, _serial);
            Snapshot();
        }
        void UpdateGuard(tsorcRevampGlobalNPC g)
        {
            if (!Neutral || g.StaggerTimer > 0 || _restTicks > 0 || _graceTicks > 0 || HasOwnedShots() || NPC.velocity.Y != 0 || g.TeleportCountdown > 0)
            { DropGuard(); return; }
            if (_guardUp)
            { if (--_guardTicks <= 0) DropGuard(); else NPC.direction = NPC.spriteDirection = _guardFacing; return; }
            if (_guardCooldown > 0 || !NPC.HasValidTarget) return;
            Player target = Main.player[NPC.target];
            int incoming = PredictThreatSide();
            float distance = NPC.Distance(target.Center);
            bool closing = distance > 72 && distance < 640 && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (incoming == 0 && !closing) return;
            _guardFacing = incoming == 0 ? Math.Sign(target.Center.X - NPC.Center.X) : incoming;
            if (_guardFacing == 0) _guardFacing = NPC.direction;
            _guardUp = true; _guardTicks = 180; NPC.direction = NPC.spriteDirection = _guardFacing; Trace("guard-raise"); Snapshot();
        }
        int PredictThreatSide()
        {
            float soonest = 19; int side = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || !p.friendly || p.damage <= 0) continue;
                Vector2 gap = p.Center - NPC.Center, relative = p.velocity - NPC.velocity;
                float vv = relative.LengthSquared();
                float t = vv > 0.001f ? -Vector2.Dot(gap, relative) / vv : 0;
                if (t < 0 || t > 18 || t > soonest) continue;
                Vector2 near = gap + relative * t;
                if (Math.Abs(near.X) > (p.width + NPC.width) * 0.5f + 6 || Math.Abs(near.Y) > (p.height + NPC.height) * 0.5f + 6) continue;
                soonest = t; side = Math.Sign(gap.X);
            }
            if (side == 0 && NPC.HasValidTarget)
            { Player p = Main.player[NPC.target]; if (p.itemAnimation > 0 && NPC.Distance(p.Center) <= 96) side = Math.Sign(p.Center.X - NPC.Center.X); }
            return side;
        }
        void DropGuard()
        {
            if (!_guardUp) return;
            _guardUp = false; _guardTicks = 0; _guardCooldown = 120; if (Server) { Trace("guard-drop"); Snapshot(); }
        }
        bool Blocks(Vector2 source)
        {
            bool block = _guardUp && Neutral && Math.Sign(source.X - NPC.Center.X) == _guardFacing;
            if (block && !Main.dedServ && _blockSoundCooldown == 0)
            { _blockSoundCooldown = 12; SoundEngine.PlaySound(SoundID.Dig, NPC.Center); DarkmoonVisuals.Motes(NPC.Center + new Vector2(_guardFacing * 16, 0), Color.LightBlue, 5, 6); }
            return block;
        }
        public override bool? CanBeHitByItem(Player player, Item item) => Blocks(player.Center) ? false : base.CanBeHitByItem(player, item);
        public override bool? CanBeHitByProjectile(Projectile projectile) => Blocks(projectile.Center) ? false : base.CanBeHitByProjectile(projectile);
        public override void OnStagger(NPC npc)
        {
            if (Server)
            {
                if (Phase == AttackPhase.Healing || Phase == AttackPhase.FleeToHeal) _healRetryTicks = 600;
                for (int i = 0; i < Main.maxProjectiles; i++)
                    if (Main.projectile[i].active && Main.projectile[i].ModProjectile is DarkmoonOwnedShot shot && shot.OwnedBy(NPC.whoAmI, _serial)) shot.BeginFade();
                if (_cast == Cast.Vigil && Phase == AttackPhase.MagicTelegraph) _vigilCooldown = 600;
                Trace("poise-interruption");
                _cast = Cast.None; _releasedMask = 0; _graceTicks = 30; DropGuard();
            }
            base.OnStagger(npc);
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (BarragePose && Phase != AttackPhase.MagicRecovery)
            {
                int elapsed = Phase == AttackPhase.MagicTelegraph ? 42 - PhaseTimer : MagicAttackTicks - PhaseTimer;
                bool show = Phase == AttackPhase.MagicTelegraph ? elapsed >= 10 : elapsed % 15 >= 4 && elapsed / 15 < _shotCount - 1;
                if (show) DarkmoonVisuals.DrawHandOrb(PuppetHandPosition, Phase == AttackPhase.MagicTelegraph ? MathHelper.Clamp((elapsed - 10) / 20f, 0.25f, 1) : 0.6f + 0.4f * (elapsed % 15 - 4) / 11f);
                if (Phase == AttackPhase.MagicTelegraph) DarkmoonVisuals.Line(PuppetHandPosition, PuppetHandPosition + _castAim * 700, Accent * 0.45f, 1f);
            }
            if (_cast == Cast.Vigil && Phase == AttackPhase.MagicTelegraph)
            {
                float grow = MathHelper.Clamp((72 - PhaseTimer - 24) / 30f, 0, 1);
                DarkmoonVisuals.DrawMoon(_vigilAnchor + new Vector2(-80, -88), grow, 0.65f, 0, 0);
                DarkmoonVisuals.DrawMoon(_vigilAnchor + new Vector2(80, -88), grow, 0.4f, 0, 1);
            }
        }
        public override void SendExtraAI(BinaryWriter w)
        {
            base.SendExtraAI(w);
            w.Write(_serial); w.Write(_phaseTwo); w.Write((byte)_cast); w.Write(_shotCount); w.Write(_releasedMask); w.Write(_castTarget);
            w.Write(_castAim.X); w.Write(_castAim.Y); w.Write(_vigilAnchor.X); w.Write(_vigilAnchor.Y);
            w.Write(_guardUp); w.Write(_guardTicks); w.Write(_guardCooldown); w.Write(_guardFacing);
            w.Write(_restTicks); w.Write(_graceTicks); w.Write(_approachTicks); w.Write(_kiteTicks); w.Write(_healRetryTicks);
        }
        public override void ReceiveExtraAI(BinaryReader r)
        {
            base.ReceiveExtraAI(r);
            _serial = r.ReadInt32(); _phaseTwo = r.ReadBoolean(); _cast = (Cast)r.ReadByte(); _shotCount = r.ReadInt32(); _releasedMask = r.ReadInt32(); _castTarget = r.ReadInt32();
            _castAim = new Vector2(r.ReadSingle(), r.ReadSingle()); _vigilAnchor = new Vector2(r.ReadSingle(), r.ReadSingle());
            _guardUp = r.ReadBoolean(); _guardTicks = r.ReadInt32(); _guardCooldown = r.ReadInt32(); _guardFacing = r.ReadInt32();
            _restTicks = r.ReadInt32(); _graceTicks = r.ReadInt32(); _approachTicks = r.ReadInt32(); _kiteTicks = r.ReadInt32(); _healRetryTicks = r.ReadInt32();
        }
    }
}
