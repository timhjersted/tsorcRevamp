using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Ranged;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    public class Ulhan : PuppetNPC
    {
        private enum BowMovement : byte { Stand, Walk, Run, Jump }
        private readonly List<BowMovement> _movementBag = new List<BowMovement>();
        private BowMovement _movement;
        private BowMovement _lastMovement = BowMovement.Jump;
        private float _jumpVx;
        private float _jumpUpSpeed = 8.5f;
        private Vector2 _shotAim = Vector2.UnitX;
        private bool _closingForExplosive;
        private int _approachTicks;
        private bool _caltropsPending;
        private Vector2 _caltropTarget;
        private readonly List<(int Slot, int Identity)> _caltrops = new List<(int, int)>();
        private const float JumpGravity = 0.3f;
        private int JumpApexTicks => Math.Max(1, (int)Math.Round(_jumpUpSpeed / JumpGravity));

        protected override string InvaderTitle => "Ulhan";
        protected override int HeadArmorItemType => ModContent.ItemType<MirkwoodElvenBlondeHairStyle>();
        protected override int BodyArmorItemType => ModContent.ItemType<MirkwoodElvenLeatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<MirkwoodElvenLeggings>();
        protected override int MeleeWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyTaintedBow>();
        protected override int SecondaryRangedWeaponItemType => RangedWeaponItemType;
        protected override int RangedDamage => EnemyDamage.Projectile(50);
        protected override int SecondaryRangedDamage => EnemyDamage.Projectile(75);
        protected override int MagicWeaponItemType => ModContent.ItemType<Content.Items.Weapons.Enemy.EnemyCaltrop>();
        protected override int MagicDamage => EnemyDamage.Projectile(50);
        protected override float MinMagicRange => 80f;
        protected override float MagicRange => _caltropsPending && NPC.HasValidTarget && NPC.velocity.Y == 0f
            && Math.Abs(Main.player[NPC.target].Center.Y - NPC.Center.Y) <= 64f
            && Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) ? 240f : 0f;
        protected override int MagicPreferenceChance => 100;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicAttackTicks => 15;
        protected override int MagicRecoveryTicks => 60;
        protected override int MagicCooldownAfterUse => 480;
        protected override Color MagicTelegraphFlashColor => Color.White;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override float TopSpeed => 2.8f;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Bow;
        protected override float RangedRange => 900f;
        protected override float MinRangedRange => _closingForExplosive ? float.MaxValue : 0f;
        protected override float SecondaryRangedRange => 200f;
        protected override float SecondaryRangedMinRange => 0f;
        protected override bool SecondaryRangedAvailable => _closingForExplosive;
        protected override int SecondaryRangedChance => 100;
        protected override int RangedTelegraphTicks => 42;
        protected override int RangedRecoveryTicks => 36;
        protected override int RangedCooldownAfterUse => 90;
        protected override int SecondaryRangedTelegraphTicks => 48;
        protected override int SecondaryRangedRecoveryTicks => 60;
        protected override int SecondaryRangedCooldownAfterUse => 0;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int StandingRangedChance => _movement == BowMovement.Stand ? 100 : 0;
        protected override int SecondaryStandingRangedChance => 100;
        protected override int[][] PrimaryRangedBurstPatterns => _movement == BowMovement.Jump
            ? new[] { Array.Empty<int>() } : new[] { new[] { 45, 45 } };
        protected override bool AllowRangedPatternsAirborne => true;
        protected override Color RangedTelegraphFlashColor => new Color(175, 100, 220);
        protected override Color SecondaryRangedFlashColor => new Color(255, 150, 40);
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause;
        protected override bool UseBowStringDrawPose => true;
        // TaintedBow: right-facing 18x46; riser texel (14,23), left-edge string rows 2..43.
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.78f, 0.5f);
        protected override float GetHeldRangedDrawScale(int itemType) => 0.9f;
        protected override Rectangle GetBowStringTexels(int itemType) => new Rectangle(0, 2, 2, 42);
        protected override string NockedBowArrowTexture => IsSecondaryRangedActive
            ? "tsorcRevamp/Content/Projectiles/Enemy/Weapons/UlhanExplosiveArrow"
            : "Terraria/Images/Projectile_" + ProjectileID.UnholyArrow;
        protected override float BowAimAngle => (float)Math.Atan2(_shotAim.Y, _shotAim.X * NPC.direction);

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 4000;
            NPC.defense = 16;
            EnemyDamage.SetContact(NPC, 0);
            NPC.knockBackResist = 0.22f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
            => npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<ArcherSpiritBell>()));

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            ClearCaltrops();
            var definition = new Terraria.ModLoader.Config.NPCDefinition(Type);
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                tsorcRevampWorld.NewSlain.Add(definition, 1);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
            }
        }

        protected override void FaceTargetForRangedShot(Player target)
        {
            // The final eight tell ticks and release retain the synchronized shot's facing.
            if (Phase == AttackPhase.RangedAttack || PhaseTimer <= 8)
                NPC.direction = NPC.spriteDirection = _shotAim.X < 0f ? -1 : 1;
            else
                base.FaceTargetForRangedShot(target);
        }

        public override void AI()
        {
            DebugAttackLabel = Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack
                || Phase == AttackPhase.MagicRecovery ? "Caltrop Throw"
                : Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.RangedRecovery
                ? IsSecondaryRangedActive ? "Explosive Arrow" : _movement + " Unholy Bow Shot"
                : _closingForExplosive ? "Explosive Arrow Approach" : null;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (_closingForExplosive && --_approachTicks <= 0)
                {
                    // Unreachable terrain must not starve the normal bow kit forever.
                    _closingForExplosive = false;
                    NPC.netUpdate = true;
                }
                if ((Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.CrossbowBurstPause)
                    && PhaseTimer > 8 && NPC.HasValidTarget)
                {
                    _shotAim = SolveShotAim(IsSecondaryRangedActive ? 10f : 13f);
                    if (PhaseTimer % 6 == 0 || PhaseTimer == 9)
                        NPC.netUpdate = true;
                }
            }
            AttackPhase previousPhase = Phase;
            base.AI();
            if (Main.netMode == NetmodeID.MultiplayerClient && previousPhase == AttackPhase.MagicTelegraph
                && Phase == AttackPhase.MagicAttack)
                PlayThrowSound();
            if (Phase == AttackPhase.CrossbowBurstPause)
                DoRangedTelegraphVFX(false, MathHelper.Clamp(1f - PhaseTimer / 45f, 0f, 1f));
            if (Phase == AttackPhase.MagicTelegraph && !Main.dedServ && Main.GameUpdateCount % 4 == 0)
            {
                // These are the shared magic/throw body's authored Use2 and Use1 hand offsets.
                Vector2 hand = NPC.Center + (PhaseTimer > MagicTelegraphTicks * 0.5f
                    ? new Vector2(4f * NPC.direction, -8f) : new Vector2(-8f * NPC.direction, -9f));
                Dust dust = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Smoke, new Vector2(0f, -0.25f), 160, Color.LightGray, 0.55f);
                dust.noGravity = true;
            }
        }

        protected override void OnMagicTelegraphStarting()
        {
            _caltropsPending = false;
            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(4f * NPC.direction, 2f);
            Vector2 offset = target.Center + target.velocity * 18f - origin;
            if (offset.LengthSquared() > 240f * 240f)
                offset = offset.SafeNormalize(new Vector2(NPC.direction, 0f)) * 240f;
            _caltropTarget = origin + offset;
            NPC.netUpdate = true;
        }

        protected override void DoMagicAttack()
        {
            PlayThrowSound();
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            // Match the forward Use3 hand displayed on the release frame, on every peer.
            Vector2 origin = NPC.Center + new Vector2(4f * NPC.direction, 2f);
            _caltrops.RemoveAll(tracked => !Main.projectile[tracked.Slot].active
                || Main.projectile[tracked.Slot].identity != tracked.Identity);
            foreach (int slot in Content.Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(
                NPC.GetSource_FromThis(), origin, _caltropTarget, MagicDamage, 1.5f, Main.myPlayer))
            {
                if (slot >= 0 && slot < Main.maxProjectiles && Main.projectile[slot].active)
                    _caltrops.Add((slot, Main.projectile[slot].identity));
            }
        }

        protected override void OnPartyWipeDespawnStarted()
        {
            base.OnPartyWipeDespawnStarted();
            ClearCaltrops();
        }

        private void ClearCaltrops()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            foreach (var tracked in _caltrops)
            {
                Projectile projectile = Main.projectile[tracked.Slot];
                if (projectile.active && projectile.identity == tracked.Identity
                    && projectile.type == ModContent.ProjectileType<Content.Projectiles.Enemy.Weapons.EnemyCaltrop>())
                    projectile.Kill();
            }
            _caltrops.Clear();
        }

        protected override void RunMovementAI(float speedMult)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RemembersLastKnownPos = true;
            if (_movement == BowMovement.Walk && (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack || Phase == AttackPhase.CrossbowBurstPause))
                speedMult *= 0.45f;
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: _closingForExplosive ? 160f : 480f);
        }

        private BowMovement DrawMovement()
        {
            if (_movementBag.Count == 0)
            {
                _movementBag.AddRange(new[] { BowMovement.Stand, BowMovement.Walk, BowMovement.Run, BowMovement.Jump });
                for (int i = _movementBag.Count - 1; i > 0; i--)
                {
                    int swap = Main.rand.Next(i + 1);
                    BowMovement held = _movementBag[i];
                    _movementBag[i] = _movementBag[swap];
                    _movementBag[swap] = held;
                }
                int last = _movementBag.Count - 1;
                if (_movementBag[last] == _lastMovement)
                {
                    BowMovement held = _movementBag[last];
                    _movementBag[last] = _movementBag[0];
                    _movementBag[0] = held;
                }
            }
            int index = _movementBag.Count - 1;
            _lastMovement = _movementBag[index];
            _movementBag.RemoveAt(index);
            return _lastMovement;
        }

        protected override void ModifyRangedBurst(bool secondary, ref int itemType, ref RangedStyle style,
            ref Color flashColor, ref int telegraphTicks, ref int attackTicks, ref int recoveryTicks)
        {
            _movement = secondary ? BowMovement.Stand : DrawMovement();
            _jumpUpSpeed = 8.5f;
            if (secondary)
                _closingForExplosive = false;
            else if (NPC.HasValidTarget && NPC.velocity.Y == 0f)
            {
                Player target = Main.player[NPC.target];
                float heightAbove = PuppetBowGripPosition.Y - target.Center.Y;
                if (heightAbove >= 48f || (heightAbove >= 16f
                    && !Collision.CanHitLine(PuppetBowGripPosition, 1, 1, target.Center, 1, 1)))
                {
                    _movement = BowMovement.Jump;
                    _jumpUpSpeed = (float)Math.Sqrt(2f * JumpGravity * MathHelper.Clamp(heightAbove, 48f, 112f));
                }
            }
            // Only launch a scripted hop from the ground; airborne navigation already owns its arc.
            if (_movement == BowMovement.Jump && NPC.velocity.Y != 0f)
                _movement = BowMovement.Stand;
            if (_movement == BowMovement.Jump && NPC.HasValidTarget)
            {
                _jumpVx = (Main.player[NPC.target].Center.X < NPC.Center.X ? -1 : 1) * TopSpeed * 1.6f;
                telegraphTicks = JumpApexTicks;
                attackTicks = JumpApexTicks * 2;
            }
            _shotAim = SolveShotAim(secondary ? 10f : 13f);
            NPC.netUpdate = true;
        }

        protected override bool HasRangedJumpOverride => _movement == BowMovement.Jump;
        protected override void TickRangedJumpOverride()
        {
            bool telegraph = Phase == AttackPhase.RangedTelegraph;
            int elapsed = telegraph ? JumpApexTicks - PhaseTimer : JumpApexTicks * 2 - PhaseTimer;
            if (telegraph && elapsed < 0)
            {
                NPC.velocity.X *= 0.8f;
                return;
            }
            if (!telegraph && elapsed > 2 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }
            NPC.velocity.X = _jumpVx;
            NPC.velocity.Y = telegraph ? -_jumpUpSpeed + JumpGravity * elapsed : JumpGravity * elapsed;
        }

        private Vector2 SolveShotAim(float speed)
        {
            if (!NPC.HasValidTarget)
                return new Vector2(NPC.direction, 0f);
            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetBowGripPosition;
            Vector2 aimAt = target.Center;
            for (int i = 0; i < 4; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / speed;
                float fallingTicks = Math.Max(0f, flightTicks - 15f);
                aimAt = target.Center + new Vector2(target.velocity.X, target.velocity.Y * 0.5f) * flightTicks
                    - new Vector2(0f, 0.1f * fallingTicks * (fallingTicks + 1f) * 0.5f);
            }
            Vector2 direction = (aimAt - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
            // Same ±45-degree articulation limit as Owl Father's bow hold.
            int facing = direction.X < 0f ? -1 : 1;
            float angle = MathHelper.Clamp((float)Math.Atan2(direction.Y, Math.Abs(direction.X)),
                -MathHelper.PiOver4, MathHelper.PiOver4);
            return new Vector2((float)Math.Cos(angle) * facing, (float)Math.Sin(angle));
        }

        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack()
        {
            if (!NPC.HasValidTarget)
                return;
            bool explosive = IsSecondaryRangedActive;
            Vector2 origin = PuppetBowGripPosition;
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, PitchVariance = 0.15f }, origin);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, _shotAim * (explosive ? 10f : 13f),
                explosive ? ModContent.ProjectileType<UlhanExplosiveArrow>() : ModContent.ProjectileType<UlhanUnholyArrow>(),
                explosive ? SecondaryRangedDamage : RangedDamage, 2f, Main.myPlayer);
            if (explosive)
            {
                _caltropsPending = true;
                NPC.netUpdate = true;
            }
            if (!explosive && IsFinalBurstShot)
            {
                _closingForExplosive = true;
                _approachTicks = 600;
                NPC.netUpdate = true;
            }
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ)
                return;
            Vector2 origin = PuppetBowGripPosition;
            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(3f, 3f),
                    secondary ? DustID.OrangeTorch : DustID.DemonTorch, -_shotAim * 0.5f, 80, default, 0.7f);
                dust.noGravity = true;
            }
            if (Main.GameUpdateCount % 6 == 0)
            {
                float reach = secondary ? UlhanExplosiveArrow.MaximumTravel : 240f;
                for (int i = 1; i <= 8; i++)
                {
                    Dust dust = Dust.NewDustPerfect(origin + _shotAim * (reach * i / 8f),
                        secondary ? DustID.OrangeTorch : DustID.DemonTorch, Vector2.Zero, 180, default, 0.35f);
                    dust.noGravity = true;
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_movement);
            writer.Write(_jumpVx);
            writer.Write(_jumpUpSpeed);
            writer.Write(_shotAim.X);
            writer.Write(_shotAim.Y);
            writer.Write(_closingForExplosive);
            writer.Write(_approachTicks);
            writer.Write(_caltropsPending);
            writer.Write(_caltropTarget.X);
            writer.Write(_caltropTarget.Y);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            // Read style first: base reconstructs the active burst pattern using this getter.
            _movement = (BowMovement)reader.ReadByte();
            _jumpVx = reader.ReadSingle();
            _jumpUpSpeed = reader.ReadSingle();
            _shotAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _closingForExplosive = reader.ReadBoolean();
            _approachTicks = reader.ReadInt32();
            _caltropsPending = reader.ReadBoolean();
            _caltropTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            base.ReceiveExtraAI(reader);
        }
    }
}
