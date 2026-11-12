using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Red Knight: the RedKnightPuppet body with the Red attack pool (TryStartRed), its stats, spawns and loot.
    /// </summary>
    [AutoloadBossHead]
    public class RedKnight : RedKnightPuppet, IHumanoidMeleeHitEffects
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(RedKnight)) + "_Head_Boss";

        #region Defaults
        NPCDespawnHandler despawnHandler;

        protected override string InvaderTitle => "Red Knight";

        const string SpearLeapName = "Leaping Spear Thrust";
        const int SpearLeapStrikeTicks = 16;
        const float SpearLeapMinimumRange = 180f;
        const float SpearLeapMaximumRange = 360f;
        static readonly MeleeCombo[] BossSpearCombos = BuildBossSpearCombos();

        protected override MeleeCombo[] MeleeComboPoolOverride => BossSpearCombos;
        protected override int RangedStartMeleeComboChance => 10;
        protected override float RangedStartComboMaxRange => SpearLeapMaximumRange;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override int LeapSlamDownswingTicks => SpearLeapStrikeTicks;
        protected override float LeapAttackUpSpeed => 8.4f;
        protected override float LeapAttackForwardSpeed => 6f;
        protected override float LeapLandingStandoff => 32f;
        protected override float LeapSlamCarryRotation => MathHelper.PiOver4;
        protected override float LeapSlamImpactRotation => MathHelper.PiOver4;

        static MeleeCombo[] BuildBossSpearCombos()
        {
            MeleeCombo[] combos = BuildKnightCombos(Color.OrangeRed);
            combos[0].RecoveryTicks = 24;
            combos[0].CooldownAfterUse = 30;
            combos[1].RecoveryTicks = 24;
            combos[1].CooldownAfterUse = 55;
            combos[2].RecoveryTicks = 30;
            combos[2].CooldownAfterUse = 70;

            // Stronger push-in, with the original live windows and roll-safe pauses intact:
            // double thrust 86px/step (3.6px/t); skewer 73px thrust + 48px rise (3.06/2.16px/t).
            for (int i = 0; i < combos[0].Steps.Length; i++)
            {
                combos[0].Steps[i].ForwardPushMult = 2f;
            }
            combos[1].Steps[0].ForwardPushMult = 1.7f;
            combos[1].Steps[1].ForwardPushMult = 1.2f;

            // Level spear thrust: 40t tell, ~56t flight at 2.6–5.9px/t over 180/270/360px;
            // a 16t landing punch extends the grip in 6t, live 6..15 (<22t roll), then 40t recovery.
            // The narrow arc is deliberate: reach comes from the shaft extension, not a sword sweep.
            MeleeCombo leap = new MeleeCombo
            {
                Name = SpearLeapName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Mid,
                RangedStartOnly = true,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 120,
                RecoveryTicks = 40,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 40,
                        AttackTicks = 90,
                        DamageMult = 1.25f,
                        ReachMult = 1f,
                        SwingSpeedMult = 1f,
                        LeapStrikeRange = 110f,
                    }
                }
            };

            return new[] { combos[0], combos[1], combos[2], leap };
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == SpearLeapName)
            {
                return NPC.HasValidTarget && CanSpearLeapTo(Main.player[NPC.target]);
            }

            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        bool CanSpearLeapTo(Player target)
        {
            float distance = NPC.Distance(target.Center);
            float heightDifference = System.Math.Abs(target.Bottom.Y - NPC.Bottom.Y);
            if (distance < SpearLeapMinimumRange || distance > SpearLeapMaximumRange
                || heightDifference > 32f || NPC.velocity.Y != 0f)
            {
                return false;
            }

            float horizontalGap = target.Center.X - NPC.Center.X;
            int direction = horizontalGap >= 0f ? 1 : -1;
            float travel = System.Math.Max(0f, System.Math.Abs(horizontalGap) - LeapLandingStandoff);
            float airtime = 2f * LeapAttackUpSpeed / 0.3f;
            float speed = travel / airtime;

            // Check the whole body parabola and its landing before committing to a slow jump.
            return KnightHopPlanner.HasSafeHop(NPC, target.Center, direction, direction, speed,
                advancing: true, hopSpeedY: LeapAttackUpSpeed, gravity: 0.3f, minimumForwardClearance: 24f);
        }

        protected override void ModifyOpeningMeleeComboStep(string comboName, Player target, ref MeleeComboStep step)
        {
            if (comboName == SpearLeapName && !CanSpearLeapTo(target))
            {
                // A player or terrain change invalidated the launch during the tell. Settle harmlessly
                // into the authored recovery rather than jumping into a wall or starting an unreachable thrust.
                step.Motion = ComboMotion.Feint;
                step.AttackTicks = 12;
            }
        }

        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
        {
            if (ActiveMeleeComboName == SpearLeapName)
            {
                return false;
            }

            return base.CanDamageWithMeleeStep(step, progress);
        }

        protected override bool CanDamageWithLeapSlamStep(MeleeComboStep step, float progress)
        {
            return progress >= 6f / SpearLeapStrikeTicks && progress < 1f;
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);
            if (combo.Name == SpearLeapName && !Main.dedServ && elapsed >= total - 30 && elapsed % 2 == 0)
            {
                Dust glint = Dust.NewDustPerfect(AttackSpearTipWorld, DustID.Torch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 80, default, 1.2f);
                glint.noGravity = true;
            }
        }

        public override void SetStaticDefaults()
        {
            // The puppet body is drawn by the base; Main.npcFrameCount 1 matches the placeholder bestiary sprite.
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 4;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            // A humanoid puppet deals no body-contact damage; every hit is a spear, spell or bomb.
            NPC.damage = 0;
            NPC.defense = 41;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 20000; // life / 1.25 in HM
            NPC.knockBackResist = 0.3f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.12 of full knockback per ordinary hit
            NPC.lavaImmune = true;
            NPC.rarity = 2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);

            if (!Main.hardMode)
            {
                NPC.defense = 14;
                NPC.value = 12500;
                redKnightsGreatDamage = 36; // real expert damage, like every field below
                redKnightsSpearDamage = 48;
                redMagicDamage = 28;
                redKnightsBombDamage = 60;
                NPC.boss = true;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 4000;
                NPC.defense = 75;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 152; // real expert damage
                redKnightsSpearDamage = 128;
                redMagicDamage = 104;
                redKnightsBombDamage = 160;
            }
            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            redKnightGlobalNPC.Agility = 0.3f;
            redKnightGlobalNPC.DirectionalDodgeRolls = true;
            redKnightGlobalNPC.PreserveJumpFacingUntilLanding = true;

            // Poise: needs sustained pressure to stagger (poise damage = weapon knockback). Tunable lever.
            redKnightGlobalNPC.PoiseMax = 40f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            redKnightGlobalNPC.NavSearchRadius = 80;
            redKnightGlobalNPC.NavGiveUpTicks = 200;
            redKnightGlobalNPC.RemembersLastKnownPos = true;
            // This authored ranged/melee hybrid kites direct melee pressure, while distant projectile hits
            // temporarily collapse the band and make it pursue. KiteRangeMax is the complete movement opt-in.
            redKnightGlobalNPC.KiteRangeMin = 8f;
            redKnightGlobalNPC.KiteRangeMax = 15f;
            redKnightGlobalNPC.KiteLooseness = 0.28f;

            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;

            // Start from the knight-family retreat profile, then remove its random on-hit blink below.
            EvasiveProfile.RedKnight(redKnightGlobalNPC);
            redKnightGlobalNPC.EvasiveRetreatJump = false;
            redKnightGlobalNPC.EvasiveRetreatDash = false;
            redKnightGlobalNPC.EvasiveDodgeRoll = true;
            // Teleport is now an authored sub-50% attack, never a random hit reaction. The remaining
            // retreat reactions are deliberately infrequent so multihit weapons cannot make RK spasm.
            redKnightGlobalNPC.EvasiveTeleportAway = false;
            redKnightGlobalNPC.EvasiveOnHitChanceDenominator = 6;
            redKnightGlobalNPC.EvasiveOnHitCooldownTicks = 120;
        }
        #endregion

        #region AI
        public override void AI()
        {
            if (!Main.hardMode)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }

            base.AI();
        }

        private protected override bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC)
        {
            return specialAttacks.TryStartRed(NPC, target, globalNPC);
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        #region Loot
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {

            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.RegenerationPotion, 30));
            npcLoot.Add(hmCondition);
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 20);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop);
            shmCondition.OnSuccess(drop2);
            npcLoot.Add(shmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<FlameOfTheAbyss>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<StaminaVessel>()));
        }
        #endregion

        #region Debuffs
        // PuppetMeleeHitbox calls this on the hit player's machine after Hurt.
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false); // loss of flight mobility
        }
        #endregion
    }
}
