using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;

using tsorcRevamp.Items.Weapons.Melee.Flails;
using System;
namespace tsorcRevamp.NPCs.Puppets
{
    public class BlackNinja : PuppetNPC, IFlailAnchor
    {
        protected override string InvaderTitle => "Black Ninja";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 65;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 2,
                attackRange: RangedRange);
        }

        protected override int HeadArmorItemType => ItemID.NinjaHood;
        protected override int BodyArmorItemType => ItemID.NinjaShirt;
        protected override int LegsArmorItemType => ItemID.NinjaPants;

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyDiamondCrusher>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyNinjaStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemySmokeBomb>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyCaltrop>();

        protected override int MeleeDamage => 28;
        protected override int RangedDamage => 16;
        protected override int SecondaryRangedDamage => 1;
        protected override int MagicDamage => 14;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Flail;

        // The flail's visual is its ball + chain projectile, so don't draw the held item icon.
        protected override bool HideHeldMeleeSprite => true;

        // The smoke bomb (secondary) renders bigger than a throwing star, and sparks a lit fuse.
        protected override float GetHeldRangedDrawScale(int itemType)
            => itemType == SecondaryRangedWeaponItemType
                ? 0.78f
                : base.GetHeldRangedDrawScale(itemType) * 0.5f;
        protected override bool HeldRangedFuseSparks(int itemType)
            => itemType == SecondaryRangedWeaponItemType;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.Throwables;

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 430f;
        protected override float MinRangedRange => 150f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedAttackTicks => 8;
        protected override int RangedRecoveryTicks => 38;
        protected override int RangedCooldownAfterUse => 150;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 20;
        protected override Color RangedTelegraphFlashColor => Color.White;

        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 16 },
            new int[] { 10, 10 },
            new int[] { 8, 8, 18 },
            new int[] { 6, 6, 6, 6 },
        };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 4, 8, 12, 18 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Cyan,
            Color.LightYellow,
            Color.Yellow,
            Color.Red,
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 85, 60, 40, 20, 8 };

        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Throw;
        protected override float SecondaryRangedRange => 360f;
        protected override float SecondaryRangedMinRange => 120f;
        protected override int SecondaryRangedTelegraphTicks => 60;
        protected override int SecondaryRangedAttackTicks => 10;
        protected override int SecondaryRangedRecoveryTicks => 62;
        protected override int SecondaryRangedCooldownAfterUse => 420;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int SecondaryRangedChance => 25;
        protected override int SecondaryStandingRangedChance => 65;
        protected override Color SecondaryRangedFlashColor => new Color(100, 100, 100);

        protected override float MagicRange => 330f;
        protected override float MinMagicRange => 170f;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicAttackTicks => 10;
        protected override int MagicRecoveryTicks => 50;
        protected override int MagicCooldownAfterUse => 360;
        protected override Color MagicTelegraphFlashColor => new Color(80, 80, 80);

        public Vector2 GetFlailAnchor()
            => NPC.Center + new Vector2(NPC.direction * 2f, -NPC.height * 0.2f + 10f);

        protected override float TopSpeed => 2.85f;
        protected override float Acceleration => 0.105f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 180f;
        protected override float ComboMaxStartRange => 225f;
        protected override int MeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1.45f;
        protected override int MinComboTelegraphTicks => 60;
        protected override int MeleeTelegraphTicks => 60;
        protected override Color MeleeTelegraphFlashColor => Color.LightYellow;
        protected override int CasualStrollChance => 6;

        protected override float PuppetJumpPower => 10.2f;
        protected override float PuppetJumpBoost => 6.4f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6.2f;

        protected override int TeleportTelegraphTicks => 120;
        protected override int TeleportDustCount => 24;
        protected override Color TeleportDustTint => new Color(70, 70, 70);
        protected override int TeleportDustTypeId => DustID.Smoke;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 1800;
            NPC.defense = 10;
            NPC.damage = 0;
            NPC.knockBackResist = 0.24f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 9000f;
            NPC.boss = true;
            NPC.npcSlots = 4f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 24f;
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
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaHood));
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaShirt));
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaPants));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DiamondCrusher>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Items.Weapons.Throwing.Caltrop>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 300, 500));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<BlackNinja>());
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

        protected override void OnRangedBurstStarted(bool secondary)
        {
            // Lit-fuse: when the smoke bomb appears in hand, start the 3 s fuse at half volume and
            // stash its slot so the bomb cuts it the instant it detonates.
            if (secondary && !Main.dedServ)
            {
                if (SoundEngine.TryGetActiveSound(Projectiles.Enemy.Weapons.PuppetSmokeBomb.ActiveFuseSlot, out var prev))
                    prev.Stop();
                // Item172 is the longer 3 s fuse — saved for a future bigger bomb:
                // ...ActiveFuseSlot = SoundEngine.PlaySound(SoundID.Item172 with { Volume = 0.5f }, NPC.Center);
                Projectiles.Enemy.Weapons.PuppetSmokeBomb.ActiveFuseSlot =
                    SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.5f }, NPC.Center);
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.2f);
            Vector2 velocity = target.Center - origin;
            if (velocity == Vector2.Zero)
            {
                velocity = new Vector2(NPC.direction, 0f);
            }
            velocity.Normalize();
            velocity *= step.Motion == ComboMotion.VerticalChop ? 9.5f : 12f;

            bool spin = step.Motion == ComboMotion.Spin;
            int damage = (int)(MeleeDamage * step.DamageMult);
            Projectile projectile = Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(),
                origin,
                spin ? Vector2.Zero : velocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyDiamondCrusherBall>(),
                damage,
                3.5f,
                Main.myPlayer,
                NPC.whoAmI,
                spin ? 1f : 0f);

            projectile.timeLeft = spin ? Math.Max(34, step.AttackTicks + 8) : Math.Max(44, step.AttackTicks + 31);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.56f, PitchVariance = 0.22f }, NPC.Center);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.24f);
            Vector2 aimAt = target.Center + target.velocity * 12f;

            if (IsSecondaryRangedActive)
            {
                PlayThrowSound();
                Vector2 smokeVelocity = UsefulFunctions.BallisticTrajectory(origin, aimAt, 7.2f, 0.18f, highAngle: false, fallback: true);
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    origin,
                    smokeVelocity,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetSmokeBomb>(),
                    SecondaryRangedDamage,
                    0f,
                    Main.myPlayer);
                return;
            }

            Vector2 toTarget = aimAt - origin;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            PlayThrowSound();
            Vector2 starVelocity = toTarget.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-7f, 7f))) * 10.5f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                starVelocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyNinjaStarProj>(),
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
            PlayThrowSound();
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            Vector2 throwTarget = target.Center + target.velocity * 16f;
            Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(NPC.GetSource_FromThis(), origin, throwTarget, MagicDamage, 1.4f, Main.myPlayer);
        }
    }
}
