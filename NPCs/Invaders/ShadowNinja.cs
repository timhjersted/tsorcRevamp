using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;

using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Items.Weapons.Ranged.Bows;
namespace tsorcRevamp.NPCs.Invaders
{
    public class ShadowNinja : InvaderNPC
    {
        protected override string InvaderTitle => "Shadow Ninja";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 90;
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

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyYellowTail>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyNinjaStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyTaintedBow>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyCaltrop>();

        protected override int MeleeDamage => 48;
        protected override int RangedDamage => 34;
        protected override int SecondaryRangedDamage => 42;
        protected override int MagicDamage => 28;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Rapier;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.Throwables;

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
        protected override float MeleeRange => 78f;
        protected override float StabRange => 210f;
        protected override float ComboMaxStartRange => 245f;
        protected override int MeleeComboChance => 92;
        protected override float ComboTelegraphMultiplier => 1.75f;
        protected override int MinComboTelegraphTicks => 34;
        protected override float StabLungeSpeedMult => 2.35f;
        protected override int StabTelegraphTicks => 44;
        protected override int StabAttackTicks => 9;
        protected override int StabRecoveryTicks => 30;
        protected override int StabCooldownAfterUse => 85;
        protected override bool CanStab => true;
        protected override int CasualStrollChance => 0;

        protected override float InvaderJumpPower => 10.5f;
        protected override float InvaderJumpBoost => 7f;
        protected override bool InvaderCanDoubleJump => true;
        protected override float InvaderDoubleJumpPower => 7f;

        protected override int TeleportTelegraphTicks => 105;
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
            globalNPC.DoubleJumpPower = InvaderDoubleJumpPower;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<YellowTail>(), 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TaintedBow>(), 8));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 900, 1400));
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
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.25f }, NPC.Center);
            TryMeleeHit(reach: MeleeRange * 0.85f);
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.18f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.62f);
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.28f }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.22f);
            Vector2 aimAt = target.Center + target.velocity * 12f;
            Vector2 toTarget = aimAt - muzzle;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            if (IsSecondaryRangedActive)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.75f, PitchVariance = 0.1f }, NPC.Center);
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-4f, 4f));
                Vector2 velocity = toTarget.RotatedBy(spread) * 11.3f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyTaintedArrow>(),
                    SecondaryRangedDamage,
                    3f,
                    Main.myPlayer);
                return;
            }

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.52f, PitchVariance = 0.22f }, NPC.Center);
            float starSpread = MathHelper.ToRadians(Main.rand.NextFloat(-7f, 7f));
            Vector2 starVelocity = toTarget.RotatedBy(starSpread) * 11f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                starVelocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderThrowingStar>(),
                RangedDamage,
                2f,
                Main.myPlayer);
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
            Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(NPC.GetSource_FromThis(), origin, throwTarget, MagicDamage, 1.5f, Main.myPlayer);
        }
    }
}
