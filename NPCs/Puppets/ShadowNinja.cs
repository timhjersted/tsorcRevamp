using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Melee;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;
using EnemyCaltrop = tsorcRevamp.Content.Items.Weapons.Enemy.EnemyCaltrop;

namespace tsorcRevamp.NPCs.Puppets
{
    [AutoloadBossHead]
    public class ShadowNinja : PuppetNPC
    {
        private const string ThreeHitStandardName = "3-Hit Standard";
        private const float ThreeHitOverhandStart = -1.55f;
        private const float ThreeHitOverhandEnd = 2.36f;
        private const int ThreeHitSwingTicks = 36;
        private const int ThreeHitEaseInTicks = 10;
        private const int ThreeHitEaseOutTicks = 26;
        private const float ThreeHitEaseOutDecay = 7f;
        private const float ThreeHitWindowEnd = 0.35f;
        private const int ThreeHitResetTicks = 30;
        private const int ThreeHitReverseTicks = 3;

        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/ShadowNinja_Head_Boss";

        protected override string InvaderTitle => "Shadow Ninja";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: SecondaryRangedRange);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<ShadowNinjaMask>();
        protected override int BodyArmorItemType => ModContent.ItemType<ShadowNinjaTop>();
        protected override int LegsArmorItemType => ModContent.ItemType<ShadowNinjaBottoms>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<ShadowSickle>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyNinjaStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyPoisonBow>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyCaltrop>();

        protected override int MeleeDamage => 48;
        protected override int RangedDamage => 34;
        protected override int SecondaryRangedDamage => 42;
        protected override int MagicDamage => 28;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.Throwables;

        protected override bool UseCompositeArmSwing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;

        private bool ThreeHitStandardActive => ActiveMeleeComboName == ThreeHitStandardName
            && (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery);

        // Poses: -1.55 -> 2.36 -> -1.55 rad, a 224-degree envelope for each swipe.
        // Tell: 35t on screen. Strike: 10t rise, 26t decay (k=7); ~184° is armed.
        // Chain: first 30t pause visibly re-raises for overhand 2; second pause holds 3t at
        // the shared overhand/underhand endpoint, producing the quick "1, 1-2" finish.
        // The first reset is an intentional harmless sweep, not an active fourth strike.
        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (combo.Name != ThreeHitStandardName)
            {
                return;
            }

            combo.Steps = (MeleeComboStep[])combo.Steps.Clone();
            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];
                step.Motion = i == 2 ? ComboMotion.UnderhandArc : ComboMotion.OverheadArc;
                step.TelegraphTicks = i == 0 ? 20 : 0;
                step.AttackTicks = ThreeHitSwingTicks;
                step.PostStepPause = i == 0 ? ThreeHitResetTicks
                    : i == 1 ? ThreeHitReverseTicks : 0;
                step.Ease = SwingEaseStyle.Weighted;
                step.EaseInTicks = ThreeHitEaseInTicks;
                step.EaseOutTicks = ThreeHitEaseOutTicks;
                step.EaseOutDecay = ThreeHitEaseOutDecay;
                step.HitWindowEnd = ThreeHitWindowEnd;
                combo.Steps[i] = step;
            }
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion,
            ref float startRotation, ref float endRotation)
        {
            if (!ThreeHitStandardActive)
            {
                if (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                    || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery)
                {
                    WeaponArchetypeTables.SetBroadswordComboArc(ActiveMeleeComboName, motion,
                        ref startRotation, ref endRotation);
                }
                return;
            }

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = ThreeHitOverhandStart;
                endRotation = ThreeHitOverhandEnd;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = ThreeHitOverhandEnd;
                endRotation = ThreeHitOverhandStart;
            }
        }

        protected override bool UseCompositeArmForAdditionalPhase =>
            IsSecondaryRangedActive && (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack || Phase == AttackPhase.CrossbowBurstPause);
        protected override bool UseBowStringDrawPose => true;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override Vector2 GetHeldRangedGripNorm(int itemType) =>
            itemType == SecondaryRangedWeaponItemType ? new Vector2(0.57f, 0.48f)
                : base.GetHeldRangedGripNorm(itemType);
        protected override float GetHeldRangedDrawScale(int itemType) =>
            itemType == RangedWeaponItemType ? 0.5f
                : itemType == SecondaryRangedWeaponItemType ? 0.9f
                : base.GetHeldRangedDrawScale(itemType);
        protected override string GetHeldRangedDrawTexturePath(int itemType) =>
            itemType == SecondaryRangedWeaponItemType
                ? "tsorcRevamp/Content/Items/Weapons/Enemy/PoisonBowDraw"
                : base.GetHeldRangedDrawTexturePath(itemType);
        // PoisonBow's baked string occupies x=7..8, y=16..43 in its 28x58 sprite.
        protected override Rectangle GetBowStringTexels(int itemType) =>
            itemType == SecondaryRangedWeaponItemType ? new Rectangle(7, 16, 2, 28)
                : Rectangle.Empty;
        protected override Color BowStringColor => new Color(55, 170, 115);
        protected override string NockedBowArrowTexture =>
            "tsorcRevamp/Content/Projectiles/Ranged/Ammo/TaintedArrowProjectile";

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 470f;
        protected override float MinRangedRange => 170f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedAttackTicks => 8;
        protected override int RangedRecoveryTicks => 42;
        protected override int RangedCooldownAfterUse => 190;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 25;
        protected override Color RangedTelegraphFlashColor => new Color(150, 120, 255);

        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 12 },
            new int[] { 10, 10 },
            new int[] { 8, 8, 18 },
            new int[] { 6, 6, 6, 6 },
        };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 4, 8, 14, 22 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Cyan,
            new Color(150, 120, 255),
            Color.Yellow,
            Color.Red,
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 80, 65, 45, 25, 10 };

        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Bow;
        protected override float SecondaryRangedRange => 650f;
        protected override float SecondaryRangedMinRange => 310f;
        protected override int SecondaryRangedTelegraphTicks => 54;
        protected override int SecondaryRangedAttackTicks => 8;
        protected override int SecondaryRangedRecoveryTicks => 58;
        protected override int SecondaryRangedCooldownAfterUse => 250;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int SecondaryRangedChance => 42;
        protected override int SecondaryStandingRangedChance => 85;
        protected override Color SecondaryRangedFlashColor => new Color(90, 220, 100);

        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 18 },
            new int[] { 20, 20 },
            new int[] { 12, 12, 28 },
            new int[] { 8, 8, 8, 8 },
        };
        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 8, 14, 20, 32 };
        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Cyan,
            Color.LightYellow,
            Color.Yellow,
            Color.Red,
        };
        protected override int[] SecondaryRangedBurstChances => new int[] { 75, 50, 35, 20, 8 };

        protected override float MagicRange => 430f;
        protected override float MinMagicRange => 210f;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicAttackTicks => 10;
        protected override int MagicRecoveryTicks => 58;
        protected override int MagicCooldownAfterUse => 520;
        protected override Color MagicTelegraphFlashColor => new Color(95, 95, 95);

        protected override float TopSpeed => 3.35f;
        protected override float Acceleration => 0.12f;
        protected override float BrakingPower => 0.24f;
        protected override float MeleeRange => 65f;
        protected override float MeleeBladeWidth => 18f;
        protected override float MeleeWeaponDrawScale => 1.25f;
        protected override float ComboMaxStartRange => 245f;
        protected override int MeleeComboChance => 92;
        protected override float ComboTelegraphMultiplier => 1.75f;
        protected override int MinComboTelegraphTicks => 34;
        protected override int CasualStrollChance => 0;

        protected override float PuppetJumpPower => 10.5f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        protected override int TeleportTelegraphTicks => 30;
        protected override int TeleportDustCount => 34;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => new Color(70, 70, 80);
        protected override float TeleportDustScale => 1.05f;

        protected override Color MeleeTelegraphFlashColor => Color.White;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.16f, 0.84f);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 6200;
            NPC.defense = 34;
            NPC.damage = 0;
            NPC.knockBackResist = 0.12f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 26000f;
            NPC.boss = true;
            NPC.npcSlots = 6f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 32f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 160;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<YellowTail>(), 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TaintedBow>(), 8));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>(), 1, 900, 1400));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<ShadowNinja>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Content.Items.StaminaDroplet>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        public override void AI()
        {
            base.AI();
            if (Phase == AttackPhase.RangedRecovery && IsSecondaryRangedActive)
            {
                SetDisplayWeapon(SecondaryRangedWeaponItemType, swing: false);
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.25f }, NPC.Center);
            TryMeleeHit(reach: MeleeRange * 0.8f);
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.28f }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void DoRangedAttack()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(IsSecondaryRangedActive
                    ? SoundID.Item5 with { Volume = 0.75f, PitchVariance = 0.1f }
                    : SoundID.Item1 with { Volume = 0.52f, PitchVariance = 0.22f }, NPC.Center);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 muzzle = IsSecondaryRangedActive ? PuppetBowGripPosition
                : NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.22f);
            Vector2 aimAt = target.Center + target.velocity * 12f;
            Vector2 toTarget = aimAt - muzzle;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            if (IsSecondaryRangedActive)
            {
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-4f, 4f));
                Vector2 velocity = toTarget.RotatedBy(spread) * 11.3f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<ShadowNinjaPoisonArrow>(),
                    SecondaryRangedDamage,
                    3f,
                    Main.myPlayer);
                return;
            }

            float starSpread = MathHelper.ToRadians(Main.rand.NextFloat(-7f, 7f));
            Vector2 starVelocity = toTarget.RotatedBy(starSpread) * 11f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                starVelocity,
                ModContent.ProjectileType<ShadowNinjaStarProj>(),
                RangedDamage,
                2f,
                Main.myPlayer);
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (!secondary || Main.dedServ)
            {
                return;
            }

            Vector2 origin = PuppetBowGripPosition;
            float angle = BowAimAngle;
            Vector2 aim = new Vector2(NPC.direction * (float)System.Math.Cos(angle),
                (float)System.Math.Sin(angle));
            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust spark = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Poisoned, -aim * 0.5f, 80, default, 0.65f);
                spark.noGravity = true;
            }
            if (Main.GameUpdateCount % 6 == 0)
            {
                for (int i = 1; i <= 8; i++)
                {
                    Dust marker = Dust.NewDustPerfect(origin + aim * (240f * i / 8f),
                        DustID.Poisoned, Vector2.Zero, 180, default, 0.35f);
                    marker.noGravity = true;
                }
            }
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            Vector2 throwTarget = target.Center + target.velocity * 18f;
            Content.Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(NPC.GetSource_FromThis(), origin, throwTarget, MagicDamage, 1.5f, Main.myPlayer);
        }
    }
}
