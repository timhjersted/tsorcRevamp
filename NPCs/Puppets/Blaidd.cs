using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Invader puppet — Blaidd (Wolf Mask + Plaguebringer's Cloak + Jim's Leggings + Yoraiz0r's Scowl).
    /// Uses vanilla Titanium Sword (two-handed greatsword swings matching StuddedLeatherWarrior's smooth arcs)
    /// and vanilla Shadowflame Bow (player-style bow holding and firing).
    /// </summary>
    [AutoloadBossHead]
    public class Blaidd : PuppetNPC
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/Blaidd_Head_Boss";

        protected override string InvaderTitle => "Blaidd";

        // ── Loadout (all vanilla) ─────────────────────────────────────────────────
        protected override int HeadArmorItemType => ItemID.WolfMask;
        protected override int BodyArmorItemType => ItemID.PlaguebringerChestplate; // "Plaguebringer's Cloak"
        protected override int LegsArmorItemType => ItemID.JimsLeggings;

        protected override int HeadArmorDyeItemType => ItemID.BrightBrownDye;
        protected override int BodyArmorDyeItemType => ItemID.SilverDye;
        protected override int LegsArmorDyeItemType => ItemID.SilverDye;

        // Yoraiz0r's Scowl — glowing-eye vanity accessory
        protected override int[] AccessoryItemTypes => new int[]
        {
            ItemID.Yoraiz0rDarkness,
        };
        protected override int[] AccessoryDyeItemTypes => new int[]
        {
            0,
        };

        // ── Weapons ───────────────────────────────────────────────────────────────
        protected override int MeleeWeaponItemType => ItemID.TitaniumSword;
        protected override int RangedWeaponItemType => ItemID.ShadowFlameBow;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;

        protected override int MeleeDamage => 70;
        protected override int RangedDamage => 50;

        // ── Melee Swing Polish & Offsets ──────────────────────────────────────────
        protected override Vector2 MeleeHandleNorm => new Vector2(0.12f, 0.88f);
        protected override float ComboReachBase => 96f;
        protected override float MeleeWeaponRotationOffset => 1.0f;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(200, 220, 255);

        // ── Melee Combos (Upperhand & Overhand Swings, 2-5 Swings, Recovery up to 150 ticks) ──
        private static MeleeComboStep BlaiddSwing(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter = 0,
            float damageMult = 1.0f,
            float forwardPushMult = 0.5f,
            float reachMult = 1.1f)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
            };

        private static readonly MeleeCombo[] BlaiddCombos = new[]
        {
            // 2-Swing Combos (60 ticks / 1.0s recovery)
            new MeleeCombo
            {
                Name = "Down-Up Slash",
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 60,
                MoveBrake = 0.15f,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.OverheadArc, 28, 22, pauseAfter: 8, damageMult: 0.90f, forwardPushMult: 0.50f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 24, pauseAfter: 0, damageMult: 1.10f, forwardPushMult: 0.60f),
                },
            },
            new MeleeCombo
            {
                Name = "Up-Down Slash",
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow,
                CooldownAfterUse = 60,
                MoveBrake = 0.15f,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.UnderhandArc, 28, 22, pauseAfter: 8, damageMult: 0.90f, forwardPushMult: 0.50f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 24, pauseAfter: 0, damageMult: 1.10f, forwardPushMult: 0.60f),
                },
            },

            // 3-Swing Combos (90 ticks / 1.5s recovery)
            new MeleeCombo
            {
                Name = "Triple Slash (Down-Up-Down)",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan,
                CooldownAfterUse = 90,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.OverheadArc, 28, 20, pauseAfter: 6, damageMult: 0.85f, forwardPushMult: 0.45f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 20, pauseAfter: 6, damageMult: 0.95f, forwardPushMult: 0.55f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 24, pauseAfter: 0, damageMult: 1.20f, forwardPushMult: 0.65f, reachMult: 1.15f),
                },
            },
            new MeleeCombo
            {
                Name = "Triple Slash (Up-Down-Up)",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightCyan,
                CooldownAfterUse = 90,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.UnderhandArc, 28, 20, pauseAfter: 6, damageMult: 0.85f, forwardPushMult: 0.45f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 20, pauseAfter: 6, damageMult: 0.95f, forwardPushMult: 0.55f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 24, pauseAfter: 0, damageMult: 1.20f, forwardPushMult: 0.65f, reachMult: 1.15f),
                },
            },

            // 4-Swing Combos (120 ticks / 2.0s recovery)
            new MeleeCombo
            {
                Name = "Relentless Four-Swing Flurry",
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightGreen,
                CooldownAfterUse = 120,
                MoveBrake = 0.10f,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.OverheadArc, 26, 18, pauseAfter: 5, damageMult: 0.80f, forwardPushMult: 0.45f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 18, pauseAfter: 5, damageMult: 0.85f, forwardPushMult: 0.50f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 18, pauseAfter: 5, damageMult: 0.95f, forwardPushMult: 0.55f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 22, pauseAfter: 0, damageMult: 1.25f, forwardPushMult: 0.70f, reachMult: 1.18f),
                },
            },

            // 5-Swing Combos (150 ticks / 2.5s max recovery)
            new MeleeCombo
            {
                Name = "Master Five-Swing Flurry",
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 150,
                MoveBrake = 0.08f,
                HeavyCommit = true,
                Steps = new[]
                {
                    BlaiddSwing(ComboMotion.OverheadArc, 26, 16, pauseAfter: 4, damageMult: 0.75f, forwardPushMult: 0.40f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 16, pauseAfter: 4, damageMult: 0.80f, forwardPushMult: 0.45f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 16, pauseAfter: 4, damageMult: 0.85f, forwardPushMult: 0.50f),
                    BlaiddSwing(ComboMotion.UnderhandArc, 0, 16, pauseAfter: 4, damageMult: 0.95f, forwardPushMult: 0.55f),
                    BlaiddSwing(ComboMotion.OverheadArc, 0, 24, pauseAfter: 0, damageMult: 1.35f, forwardPushMult: 0.75f, reachMult: 1.20f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => BlaiddCombos;

        // ── Ranged tuning (bow: long reach, deliberate draw) ──────────────────────
        protected override float RangedRange => 560f;
        protected override float MinRangedRange => 220f;
        protected override int RangedTelegraphTicks => 48; // bow draw
        protected override int RangedRecoveryTicks => 40;
        protected override int RangedCooldownAfterUse => 240;
        protected override Color RangedTelegraphFlashColor => new Color(170, 90, 220);

        // ── Combat tuning ─────────────────────────────────────────────────────────
        protected override float TopSpeed => 2.8f;
        protected override float Acceleration => 0.10f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f;
        protected override bool CanStab => true;
        protected override bool SlowDownBeforeMelee => false;

        protected override int MeleeTelegraphTicks => 38;
        protected override int MeleeRecoveryTicks => 24;
        protected override int StabTelegraphTicks => 40;
        protected override int StabAttackTicks => 8;
        protected override int StabRecoveryTicks => 34;

        protected override Color MeleeTelegraphFlashColor => new Color(215, 225, 255);

        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(185, 185, 195);

        protected override int CasualStrollChance => 0;

        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: 640f);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 3000;
            NPC.defense = 35;
            NPC.damage = 0; // all damage comes from the weapon hitbox
            NPC.knockBackResist = 0.18f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 12000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 38f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 400, 600));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<Blaidd>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Items.StaminaDroplet>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.55f);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.22f);
            Vector2 aimAt = target.Center + target.velocity * 10f;
            Vector2 toTarget = aimAt - muzzle;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            SoundEngine.PlaySound(SoundID.Item102 with { Volume = 0.8f, PitchVariance = 0.12f }, NPC.Center);

            float spread = MathHelper.ToRadians(Main.rand.NextFloat(-3.5f, 3.5f));
            Vector2 velocity = toTarget.RotatedBy(spread) * 12f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                velocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyShadowflameArrow>(),
                RangedDamage,
                3f,
                Main.myPlayer);
        }
    }
}
