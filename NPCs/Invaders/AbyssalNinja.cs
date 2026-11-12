using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Melee.AbyssalNinjaSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee;
using tsorcRevamp.Content.Items.Weapons.Ranged.Crossbows;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    // Design/spec cards: tsorcDocs/AbyssalNinja_Rework_Proposal.md.
    // One owner per sequence: the base owns sword combos/Estus; this class owns custom attacks.
    [AutoloadBossHead]
    public partial class AbyssalNinja : PuppetNPC
    {
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(AbyssalNinja)) + "_Head_Boss";
        public static bool ConfigHasWings = true;
        public static int ConfigWingsItemType = ItemID.DemonWings;
        protected override string InvaderTitle => "Abyssal Ninja";
        protected override int HeadArmorItemType => ModContent.ItemType<AbyssalNinjaMask>();
        protected override int BodyArmorItemType => ModContent.ItemType<AbyssalNinjaTop>();
        protected override int LegsArmorItemType => ModContent.ItemType<AbyssalNinjaBottoms>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyAbyssalNinjaSword>();
        protected override int RangedWeaponItemType => ModContent.ItemType<AbyssalStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<HeavyCrossbow>();
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override int MeleeDamage => EnemyDamage.Projectile(320);
        protected override int RangedDamage => EnemyDamage.Projectile(220);
        protected override int SecondaryRangedDamage => EnemyDamage.Projectile(280);
        protected override int EstusChargesMax => 3;
        protected override bool CanBeginEstusHeal => Neutral && _move == Move.None && _pendingTier <= _tier && !_withdrawing && !HasOwnedHazards();
        protected override bool HoldAttackSelection => true; // Local bag selects ALL offense, never a legacy fallback.
        protected override bool CanStab => false;
        protected override bool AllowReactiveDefense => false;
        protected override int CasualStrollChance => 0;
        protected override int AbandonedDespawnTicks => 600;
        protected override float TopSpeed => 3.8f;
        protected override float Acceleration => 0.14f;
        protected override float MeleeRange => 114f;
        protected override float ComboReachBase => 80f / 0.7f;
        protected override float MeleeBladeWidth => 18f;
        protected override Vector2 MeleeHandleNorm => new Vector2(16f / 86f, 68f / 82f);
        protected override float MeleeWeaponDrawScale => 0.84f;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override float LogicalWindupSettleFraction => 0.45f;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override float ComboTelegraphAdvanceSpeedMult => 0f;
        protected override float MeleeEngageRange => 144f;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(170, 125, 240);
        protected override float SlashVFXOpacity => 0.6f;
        protected override bool HasWings => ConfigHasWings;
        protected override int WingsAccessoryItemType => ConfigWingsItemType;
        // Both automatic takeoff routes are suppressed; custom attacks own their flight.
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => float.MaxValue;
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;
        protected override int TeleportTelegraphTicks => 120;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => AbyssColor;

        private const string VeilName = "Veil Cut";
        private const string UndertowName = "Undertow Reversal";
        private const string LeapName = "Abyss Leap";
        private const float HighPose = -1.55f;
        private const float LowPose = 2.36f;
        private static readonly Color AbyssColor = new Color(150, 100, 220);
        private enum Move : byte { None, Veil, Undertow, Blackstep, Leap, Eclipse, Needles, Meridian, Nightfall, Crossbow, Transition }
        private Move _move;
        private Move _selecting;
        private AttackBag<Move> _bag;
        private readonly int[] _reuse = new int[11];
        private int _tier;
        private int _pendingTier;
        private int _encounterId;
        private int _fieldAge;
        private Vector2 _fieldAnchor;
        private bool _withdrawing;
        private int _whiffStreak;
        private bool _sequenceHit;
        private int _pressureBand;
        internal int EncounterId => _encounterId;
        internal bool EncounterActive => NPC.active && NPC.life > 0 && !_withdrawing;
        private bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.ClosingDistance || Phase == AttackPhase.CasualStroll;

        // L: 24 tell + 35 attack (7/28 k7), cutoff .338, 23 harmless tail +24 recovery.
        // M: 32 tell + 40 attack (8/32 k6), cutoff .3605, 25.6 tail +10 pause =>35.6 gap.
        // 224-degree envelopes; collision is disarmed before the long decaying finish.
        private static MeleeComboStep Cut(ComboMotion motion, bool light, int tell, int pause, float damage, float travel)
            => new MeleeComboStep { Motion = motion, TelegraphTicks = tell, AttackTicks = light ? 35 : 40,
                PostStepPause = pause, DamageMult = damage / 320f, ReachMult = 1f, SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted, EaseInTicks = light ? 7 : 8, EaseOutTicks = light ? 28 : 32,
                EaseOutDecay = light ? 7 : 6, HitWindowEnd = light ? 0.338f : 0.3605f, StepInDistance = travel };

        private static MeleeComboStep LeapStep(int tell, int pause) => new MeleeComboStep {
            Motion = ComboMotion.LeapSlam, TelegraphTicks = tell, AttackTicks = 100, PostStepPause = pause,
            DamageMult = 360f / 320f, ReachMult = 1f, SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
            HitWindowEnd = 1f, LeapStrikeRange = 110f, LeapHeightMult = 1f, LeapForwardSpeedMult = 1f };
        private static readonly MeleeCombo[] SwordPool = {
            new MeleeCombo { Name = VeilName, BaseWeight = 1, Preferred = ComboRangeBand.Close, RecoveryTicks = 24,
                Steps = new[] { Cut(ComboMotion.OverheadArc, true, 24, 0, 320, 40) } },
            new MeleeCombo { Name = UndertowName, BaseWeight = 1, Preferred = ComboRangeBand.Close, RecoveryTicks = 36,
                Steps = new[] { Cut(ComboMotion.OverheadArc, false, 32, 10, 300, 48),
                    Cut(ComboMotion.UnderhandArc, false, 0, 0, 320, 32) } },
            new MeleeCombo { Name = LeapName, BaseWeight = 1, Preferred = ComboRangeBand.Mid, RangedStartOnly = true,
                RecoveryTicks = 38, MoveBrake = 0f, Steps = new[] { LeapStep(36, 0) } }
        };
        protected override MeleeCombo[] MeleeComboPoolOverride => SwordPool;
        protected override bool UseLandingTimedLeapSlam => ActiveMeleeComboName == LeapName;
        protected override float LeapSlamCarryRotation => HighPose;
        protected override float LeapSlamImpactRotation => LowPose;
        protected override float LeapAttackForwardSpeed => 6f;
        protected override float LeapAttackUpSpeed => 9.5f;

        public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
        public override void SetDefaults()
        {
            NPC.width = 20; NPC.height = 42; NPC.lifeMax = (int)(30000 * tsorcRevampWorld.SHMScale);
            NPC.defense = 65; NPC.damage = 0; NPC.knockBackResist = 0.15f; NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50000f; NPC.boss = true; NPC.npcSlots = 10f;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.PoiseMax = 120f; g.PoiseStaggerResetsAI = true; g.CanUseRopes = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            _fieldAnchor = NPC.Center;
            if (Authority) { _encounterId = Main.rand.Next(1, 1 << 22); NPC.netUpdate = true; }
        }
        private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
        protected override void RunMovementAI(float speedMult)
        {
            if (_withdrawing || Phase == AttackPhase.Custom || IsSwordSequence || Phase == AttackPhase.MeleeComboRecovery)
                return;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 80; g.RemembersLastKnownPos = true;
            g.CanDoubleJump = true;
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: 700f);
        }
        private bool IsSwordSequence => Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause;

        public override void AI()
        {
            if (Authority && NPC.lifeMax > 0)
                _pendingTier = Math.Max(_pendingTier, NPC.life <= NPC.lifeMax * 0.30f ? 2 : NPC.life <= NPC.lifeMax * 0.65f ? 1 : 0);
            if (_fieldAge < 60) _fieldAge++;
            if (Authority) for (int i = 0; i < _reuse.Length; i++) if (_reuse[i] > 0) _reuse[i]--;
            base.AI();
            if (_withdrawing) return;
            if (Neutral) SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            if (Phase == AttackPhase.Custom)
            {
                if (_firePending && Authority) { _firePending = false; FireShot(); }
                // Sample AFTER base.TickWeaponAnim so damage uses this tick's visible custom pose.
                if (_customLive && Authority)
                {
                    if (!_customArmed) { DoComboMeleeHit(new MeleeComboStep { DamageMult = _customDamage / 320f, ReachMult = 1f }); _customArmed = true; }
                    TickBladeHit();
                }
                else if (_customArmed) { StopMeleeHit(); _customArmed = false; }
            }
            else if (_stage != Stage.None) ClearCustom();
            if (!Authority || !Neutral || _withdrawing) return;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (g.StaggerTimer > 0 || g.TeleportCountdown > 0 || g.DodgeTimer > 0 || InFinisherPunishWindow) return;
            // Leave a neutral tick for the base Estus selector before dealing another attack.
            if (_move != Move.None) { FinishSequence(); return; }
            if (!NPC.HasValidTarget || NPC.velocity.Y != 0f || _fieldAge < 60 || HasOwnedHazards()) return;
            int unlocked = _pendingTier;
            if (unlocked > _tier)
            {
                _tier = unlocked; _bag?.Clear(); _move = Move.Transition;
                BeginStage(Stage.Transition, 60); NPC.netUpdate = true; return;
            }
            ChooseMove(Main.player[NPC.target]);
        }

        private void ChooseMove(Player target)
        {
            float distance = NPC.Distance(target.Center);
            int band = _pressureBand;
            if (distance < 120) band = 0;
            else if (distance > 168 && distance < 296) band = 1;
            else if (distance > 344) band = 2;
            if (band != _pressureBand) { _pressureBand = band; _bag?.Clear(); }
            _bag ??= new AttackBag<Move>(cards => {
                cards.Add(Move.Veil); cards.Add(Move.Veil); cards.Add(Move.Undertow); cards.Add(Move.Undertow);
                cards.Add(Move.Blackstep); cards.Add(Move.Leap); cards.Add(Move.Leap); cards.Add(Move.Eclipse);
                cards.Add(Move.Needles); cards.Add(Move.Crossbow);
                if (_tier > 0) { cards.Add(Move.Meridian); cards.Add(Move.Nightfall); }
            });
            Move next;
            if (_whiffStreak >= 2 && Eligible(Move.Leap, target)) next = Move.Leap;
            else if (!_bag.TryDraw(move => Eligible(move, target), out next)) return;
            _move = next; _sequenceHit = false;
            _lockedTarget = NPC.target; _facing = target.Center.X < NPC.Center.X ? -1 : 1;
            if (next == Move.Veil || next == Move.Undertow || next == Move.Leap)
            {
                _selecting = next;
                if (!TryStartMeleeCombo(distance, rangedStartOnly: next == Move.Leap)) { _move = Move.None; }
                _selecting = Move.None;
            }
            else StartSpecial(next, target);
            NPC.netUpdate = true;
        }
        private bool Eligible(Move move, Player target)
        {
            if (_reuse[(int)move] > 0 || !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) return false;
            float d = NPC.Distance(target.Center), dy = NPC.Center.Y - target.Center.Y;
            switch (move)
            {
                case Move.Veil: case Move.Undertow: return d <= 144 && Math.Abs(dy) <= 48;
                case Move.Blackstep: return d <= 112 && Math.Abs(dy) < 40 && CanBackflip();
                case Move.Leap: return d >= 144 && d <= 300 && TryLeapPath(target);
                case Move.Eclipse: return dy >= 56 && dy <= 192 && Math.Abs(target.Center.X - NPC.Center.X) <= 224 && TryEclipsePath(target);
                case Move.Needles: return d >= 192 && d <= 640;
                case Move.Crossbow: return d >= 350 && d <= 600;
                case Move.Meridian: return _tier > 0 && d >= 160 && d <= 320 && Math.Abs(dy) < 40 && TryMeridianPath(target, out _);
                case Move.Nightfall: return _tier > 0 && d >= 256 && d <= 640 && TryNightfallSite(target, out _, out _);
                default: return false;
            }
        }
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
            => (_selecting == Move.Veil && combo.Name == VeilName) || (_selecting == Move.Undertow && combo.Name == UndertowName)
                || (_selecting == Move.Leap && combo.Name == LeapName);
        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            // Preview passes health directly; runtime latches it at the safe phase transition.
            bool evolved = healthFraction <= 0.65f || _tier > 0;
            if (evolved && combo.Name == UndertowName)
                combo.Steps = new[] { Cut(ComboMotion.OverheadArc, false, 32, 10, 300, 48),
                    Cut(ComboMotion.UnderhandArc, false, 0, 10, 320, 32), Cut(ComboMotion.OverheadArc, false, 0, 0, 340, 32) };
            if (evolved && combo.Name == LeapName) combo.Steps = new[] { LeapStep(36, 40), LeapStep(0, 0) };
        }
        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == LeapName) return TryLeapPath(target) && NPC.Distance(target.Center) <= 300;
            return Math.Abs(target.Center.Y - NPC.Center.Y) <= 48 && NPC.Distance(target.Center) <= 80 + 64 + 19
                && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)
                && (previousStepHit || NPC.direction * (target.Center.X - NPC.Center.X) >= 0);
        }
        protected override void ModifyNextMeleeComboStep(string comboName, int nextStepIndex, Player target, ref MeleeComboStep step)
        {
            if (comboName != LeapName) step.StepInDistance = MathHelper.Clamp(Math.Abs(target.Center.X - NPC.Center.X) - 40, 0, 64);
        }
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc) { startRotation = HighPose; endRotation = LowPose; }
            if (motion == ComboMotion.UnderhandArc) { startRotation = LowPose; endRotation = HighPose; }
            // The harmless preparation need not tour the entire damaging arc before raising.
            if (Phase == AttackPhase.MeleeComboTelegraph && motion == ComboMotion.OverheadArc) endRotation = 0.7f;
        }
        protected override void OnBladeHit(Player player) { _sequenceHit = true; }
        // Required base callbacks; normal offense is exclusively selected by the local bag.
        protected override void DoMeleeAttack() => TryMeleeHit(80f);
        protected override void DoRangedAttack() { if (Authority) FireShot(); }
        protected override void OnLeapSlamLanded(MeleeComboStep step) { Smoke(NPC.Bottom, 22); }
        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        { if (elapsed % 5 == 0) Smoke(PuppetWeaponTipPosition(80), 1); }
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (elapsed == step.EaseInTicks) Cue(SoundID.Item1);
            if (combo.Name == UndertowName && _tier > 0 && step.Motion == ComboMotion.UnderhandArc && elapsed >= 15 && elapsed % 4 == 0)
                Smoke(PuppetWeaponTipPosition(80), 3);
        }
        private void FinishSequence()
        {
            int[] cooldowns = { 0, 70, 180, 420, 300, 300, 240, 600, 720, 240, 0 };
            _reuse[(int)_move] = cooldowns[(int)_move];
            if (_move == Move.Veil || _move == Move.Undertow) _whiffStreak = _sequenceHit ? 0 : _whiffStreak + 1;
            else if (_move == Move.Leap) _whiffStreak = 0;
            _move = Move.None; NPC.netUpdate = true;
        }
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc); ClearCustom(); KillOwnedHazards();
            if (Authority && _move != Move.None) FinishSequence();
        }
        protected override void OnPartyWipeDespawnStarted()
        { base.OnPartyWipeDespawnStarted(); _withdrawing = true; ClearCustom(); KillOwnedHazards(); NPC.netUpdate = true; }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AbyssalStar>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AbyssalFabric>(), 1, 3, 3));
        }
        public override void OnKill()
        {
            KillOwnedHazards();
            if (!Authority) return;
            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<AbyssalNinja>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Content.Items.StaminaVessel>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
            }
        }
        private void Smoke(Vector2 center, int count)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++) {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8, 5), DustID.Smoke,
                    Main.rand.NextVector2Circular(2.5f, 2f), 115, AbyssColor, Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
            }
        }
        private void Cue(Terraria.Audio.SoundStyle sound) { if (!Main.dedServ) SoundEngine.PlaySound(sound with { Volume = 0.65f }, NPC.Center); }
    }
}
