using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Ranged.Crossbows;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;

using tsorcRevamp.Items.Weapons.Melee.Axes;
namespace tsorcRevamp.NPCs.Invaders
{
    public class StuddedLeatherWarrior : InvaderNPC
    {
        protected override string InvaderTitle => "Studded Leather Warrior";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: RangedRange);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<StuddedLeatherHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<StuddedLeatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<StuddedLeatherGreaves>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyForgottenRuneAxe>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyFireFlask>();

        protected override int MeleeDamage => 32;
        protected override int RangedDamage => 18;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;

        // Off-hand shield — basic early Iron Shield (also what HollowSoldier drops).
        protected override int ShieldItemType => ModContent.ItemType<IronShield>();

        // ── Axe draw tuning ─────────────────────────────────────────────────────────
        // The ForgottenRuneAxe sprite (46×38) carries its mass in the head and doesn't sit on
        // the broadsword diagonal the base draw assumes, so the default grip leaves the head
        // detached from the hand and the swing reading wrong.  These are starting values —
        // fine-tune them in-game until the grip sits in the hand and the head leads the swing.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.18f, 0.82f);
        protected override float MeleeWeaponDrawScale => 0.85f;
        // Rotate the axe ~60° so it sits straight up (12 o'clock) in the hand instead of ~10 o'clock.
        // Applied × facing for mirror symmetry; tune this value to dial the held angle.
        protected override float MeleeWeaponRotationOffset => 1.0f;

        // ── Composite-arm swing experiment ──────────────────────────────────────────
        // Enabled ONLY on this enemy so the new continuous-arm swing can be A/B tested against
        // the legacy 4-frame path before touching any other invader.  Flip the global
        // InvaderNPC.CompositeArmSwingMasterEnable off to fall back instantly.
        protected override bool UseCompositeArmSwing => true;

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 440f;
        protected override float MinRangedRange => 260f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedCooldownAfterUse => 360;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 35;
        protected override Color RangedTelegraphFlashColor => new Color(255, 120, 40);

        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<HeavyCrossbow>();
        protected override int SecondaryRangedDamage => 24;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;
        protected override float SecondaryRangedRange => 560f;
        protected override float SecondaryRangedMinRange => 220f;
        protected override int SecondaryRangedTelegraphTicks => 34;
        protected override int SecondaryRangedCooldownAfterUse => 170;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int SecondaryRangedChance
        {
            get
            {
                float hpFrac = NPC.lifeMax > 0 ? (float)NPC.life / NPC.lifeMax : 1f;
                if (hpFrac <= 0.35f)
                {
                    return 70;
                }
                if (hpFrac <= 0.65f)
                {
                    return 82;
                }
                return 90;
            }
        }
        protected override int SecondaryStandingRangedChance => 70;
        protected override Color SecondaryRangedFlashColor => Color.White;

        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 30, 60 },
            new int[] { 20 },
            new int[] { 18, 18, 18 },
            new int[] { 30, 30, 30, 5, 5 },
        };

        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 10, 0, 12, 30 };

        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Yellow,
            Color.Cyan,
            Color.LightYellow,
            Color.Red,
        };

        protected override int[] SecondaryRangedBurstChances => new int[] { 90, 45, 55, 30, 10 };

        protected override float TopSpeed => 2.65f;
        protected override float Acceleration => 0.095f;
        protected override float MeleeRange => 82f;
        protected override float StabRange => 150f;
        protected override float ComboMaxStartRange => 210f;
        protected override int MeleeComboChance => 85;
        protected override float ComboTelegraphMultiplier => 1.45f;

        protected override int MeleeTelegraphTicks => 36;
        protected override int StabTelegraphTicks => 40;
        protected override int StabAttackTicks => 8;
        protected override int StabRecoveryTicks => 34;
        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(180, 180, 180);
        protected override int CasualStrollChance => 10;
        // Stab is a thrust pose — reads wrong for an axe (an axe has no poke), so it's disabled;
        // the axe gets its reach from the combo swings + Charge/Leap gap-closers instead.
        protected override bool CanStab => false;

        protected override float InvaderJumpPower => 10f;
        protected override float InvaderJumpBoost => 6f;

        protected override Color MeleeTelegraphFlashColor => Color.White;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 2600;
            NPC.defense = 16;
            NPC.damage = 0;
            NPC.knockBackResist = 0.22f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 14000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;

            // Grounded shield-and-blade evasion (now SF4-aware): punish ranged kiting with a
            // LeapForward, answer pokes with a hyper-armored RunningDash, RetreatDash to reset.
            EvasiveProfile.LothricKnight(globalNPC);
            // Disciplined reactive guard (pre-emptive + on-hit block); pairs with ShieldItemType.
            ShieldProfile.LothricKnight(globalNPC);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Guaranteed: the full studded leather set it wears + the iron shield it carries.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherHelmet>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherArmor>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherGreaves>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<IronShield>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ForgottenRuneAxe>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Items.Weapons.Throwing.FireFlask>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 500, 750));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<StuddedLeatherWarrior>());
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

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            // Block takes precedence — only leap/dash away if the guard didn't snap up.
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ReactiveBlockTimer <= 0)
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ReactiveBlockTimer <= 0)
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
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

            if (IsSecondaryRangedActive)
            {
                SoundEngine.PlaySound(SoundID.Item98 with { Volume = 0.75f, PitchVariance = 0.1f }, NPC.Center);
                Vector2 muzzle = NPC.Center + new Vector2(0f, -NPC.height * 0.20f);
                Vector2 aimAt = target.Center + new Vector2(0f, -target.height * 0.25f);
                Vector2 toTarget = aimAt - muzzle;
                if (toTarget == Vector2.Zero)
                {
                    toTarget = new Vector2(NPC.direction, 0f);
                }

                toTarget.Normalize();
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-3f, 3f));
                Vector2 velocity = toTarget.RotatedBy(spread) * 14f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderCrossbowBolt>(),
                    SecondaryRangedDamage,
                    4f,
                    Main.myPlayer);
                return;
            }

            PlayThrowSound();
            Vector2 flaskMuzzle = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            Vector2 flaskTarget = target.Center + target.velocity * 18f;
            Vector2 flaskVelocity = UsefulFunctions.BallisticTrajectory(flaskMuzzle, flaskTarget, 7.5f, 0.18f, highAngle: false, fallback: true);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                flaskMuzzle,
                flaskVelocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyFireFlask>(),
                RangedDamage,
                3f,
                Main.myPlayer);
        }
    }
}
