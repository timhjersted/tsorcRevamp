using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Armor.DarkmoonKnightSet;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Axes;
using tsorcRevamp.Content.Projectiles.Enemy.Darkmoon;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public partial class GhostOfTheDarkmoonKnight : PuppetNPC, IHumanoidMeleeHitEffects
    {
        public const int CutExpertDamage = 240, LeapExpertDamage = 300;
        public const int CrescentBladeExpertDamage = 280, CrescentExpertDamage = 256, BoltExpertDamage = 240;
        const string CutName = "Mooncut", LeapName = "Moonstep Cleave", CrescentName = "Crescent Severance";
        enum Move : byte { Cut, Leap, Crescent, Barrage }
        enum Cast : byte { None, Barrage, Vigil }
        Cast _cast;
        AttackBag<Move> _bag;
        string _requestedCombo;
        int _serial, _age, _completedActions, _neutralTicks, _restTicks, _graceTicks, _healRetryTicks;
        int _cutCooldown, _leapCooldown, _crescentCooldown, _barrageCooldown, _vigilCooldown;
        int _guardTicks, _guardCooldown, _guardFacing = 1;
        int _kiteTicks, _approachTicks, _shotCount, _releasedMask, _castTarget;
        bool _phaseTwo, _guardUp, _vigilWasPresent;
        Vector2 _castAim = Vector2.UnitX, _vigilAnchor;
        static Color Accent => new Color(155, 105, 235);
        public int EncounterSerial => _serial;
        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
        bool Casting => Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack || Phase == AttackPhase.MagicRecovery;
        bool BarragePose => _cast == Cast.Barrage && Casting;
        public DarkmoonVigilController Vigil => DarkmoonVigilController.Find(_serial);

        protected override string InvaderTitle => "Ghost of the Darkmoon Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int CasualStrollChance => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<DarkmoonKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<DarkmoonKnightBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<DarkmoonKnightGreaves>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyBarrowWightSword>();
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => _cast == Cast.Barrage ? -1 : MeleeWeaponItemType;
        protected override int MeleeDamage => EnemyDamage.Projectile(CutExpertDamage);
        protected override int RangedDamage => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => false;
        protected override bool WeaponSheathed => BarragePose;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => new Vector2(6f / 52f, 45f / 52f);
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float ComboReachBase => 90f;
        protected override float MeleeBladeWidth => 12f;
        protected override float MeleeRange => 0f;
        protected override float MeleeEngageRange => 72f;
        protected override float ComboMaxStartRange => 384f;
        protected override float RangedStartComboMaxRange => 384f;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int PhaseTelegraphMinimumTicks => 20;
        protected override int MeleeRecoveryLingerTicks => ActiveMeleeComboName == LeapName ? 16 : 6;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override float LogicalWindupSettleFraction => 0.43f;
        protected override Color SlashVFXColor => Accent;
        protected override float SlashVFXOpacity => 0.45f;
        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.09f;
        protected override float PuppetJumpPower => 11f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override int LeapSlamDownswingTicks => 14;
        protected override float LeapAttackUpSpeed => 6f;
        protected override float LeapAttackForwardSpeed => 9f;
        protected override float LeapAttackMinimumForwardSpeed => 0f;
        protected override float LeapMinLungeDistance => 0f;
        protected override float LeapLandingStandoff => -32f;
        protected override float LeapSlamCarryRotation => -1.55f;
        protected override float LeapSlamImpactRotation => 2.36f;
        protected override bool SuppressGhostWallTeleport => base.SuppressGhostWallTeleport || _graceTicks > 0 || _restTicks > 0 || _guardUp || HasOwnedShots();
        protected override Color PuppetSkinColor => Color.Black;
        protected override bool HasSpectralOverlay => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => new Color(155, 165, 210);
        protected override float SpectralCoreTintStrength => 0.25f;
        protected override float SpectralCoreOpacity => 0.8f;
        protected override Color SpectralHaloColor => new Color(90, 75, 150);
        protected override int SpectralHaloCopyCount => 6;
        protected override float SpectralHaloRadius => 3f;
        protected override float SpectralHaloOpacity => 0.18f;
        protected override float SpectralTrailOpacity => 0.25f;
        protected override int TeleportTelegraphTicks => 80;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => new Color(55, 55, 65);
        protected override int OnKillDustCount => 0;

        protected override MeleeCombo[] MeleeComboPoolOverride => Combos;
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float start, ref float end)
        {
            if (motion == ComboMotion.OverheadArc) { start = -1.55f; end = 2.36f; }
            if (motion == ComboMotion.UnderhandArc) { start = 2.36f; end = -1.55f; }

            // The logical windup eases carry -> arc END -> arc START. With this 224deg arc that dips the blade
            // under the body and whirls it ~220deg back up around the hand. Collapsing the end onto the start
            // for the tell turns it into a straight ease from carry into the raised (or low) guard, then a hold.
            if (Phase == AttackPhase.MeleeComboTelegraph && (motion == ComboMotion.OverheadArc || motion == ComboMotion.UnderhandArc))
            {
                end = start;
            }
        }

        public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
        public override void SetDefaults()
        {
            NPC.npcSlots = 5; NPC.width = 20; NPC.height = 40; NPC.aiStyle = -1;
            NPC.damage = 0; NPC.defense = 65; NPC.lifeMax = 3000; NPC.value = 12000;
            NPC.knockBackResist = 0.3f; NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
            Banner = Type; BannerItem = ModContent.ItemType<Banners.GhostOfTheDarkmoonKnightBanner>();
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.CanPassThroughWalls = true; g.HasGhostAfterimages = false;
            g.KiteRangeMin = 5; g.KiteRangeMax = 10; g.KiteLooseness = 0.7f;
            g.PatrolMode = PatrolMode.Wander; g.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
            g.NavSearchRadius = 50; g.MaxJumpPower = 11; g.MaxJumpBoost = 7; g.CanUseRopes = true;
            g.TeleportTelegraphTime = 80; g.TeleportDustType = DustID.Smoke;
            g.TeleportDustColor = new Color(55, 55, 65); g.TeleportDustScale = 0.65f; g.TeleportDustCount = 20;
        }
        protected override void RunMovementAI(float speedMult)
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.KiteRangeMax = _guardUp || _approachTicks > 0 || Vigil != null ? 0f : 10f;
            if (Neutral && _restTicks == 0 && _graceTicks == 0)
                SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * (_guardUp ? 1f : speedMult), acceleration: Acceleration, doorBreakingDamage: 3, attackRange: 700f);
            else NPC.velocity.X *= 0.65f;
        }
        public void OnHumanoidMeleeHit(Player target) => target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 180);
        public override void OnKill()
        {
            if (Server)
            {
                Vector2 riseVelocity = new Vector2(0f, -0.45f);
                int cloudType = ModContent.ProjectileType<DarkmoonDeathCloud>();
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, riseVelocity, cloudType, 0, 0f, Main.myPlayer);
            }
        }

        public override void ModifyNPCLoot(NPCLoot loot)
        {
            loot.Add(ItemDropRule.Common(ModContent.ItemType<GigantAxe>(), 10));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 2));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 4));
            loot.Add(ItemDropRule.Common(ItemID.FlaskofVenom));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 2));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 4));
            loot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 3));
        }
    }
}
