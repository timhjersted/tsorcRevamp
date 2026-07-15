using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Mythril-tier visual prototype for Khaios. His magic loadout is intentionally left open;
    /// Wyrmkiller is currently his close-range fallback and only implemented attack.
    /// </summary>
    public class Khaios : PuppetNPC
    {
        protected override string InvaderTitle => "Khaios";

        protected override int HeadArmorItemType => ItemID.SpookyHelmet;
        protected override int BodyArmorItemType => ItemID.VortexBreastplate;
        protected override int LegsArmorItemType => ItemID.JimsLeggings;
        protected override int HeadArmorDyeItemType => ItemID.GreenandBlackDye;
        protected override int BodyArmorDyeItemType => ItemID.GreenandBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.GreenandBlackDye;

        protected override bool HasWings => true;
        protected override bool ShowWingsWhenGrounded => true;
        protected override int WingsAccessoryItemType => ItemID.JimsWings;
        protected override int WingsDyeItemType => ItemID.AcidDye;
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 190f,
            HoverSideOffset = 120f,
            HoverTopSpeed = 4.2f,
            TakeOffSpeed = 8f,
            DiveAcceleration = 0.5f,
            DiveTopSpeed = 11f,
            LandSpeed = 6f,
            MaxFlightTicks = 480,
            CooldownTicks = 210,
            TakeOffTicks = 34,
            HoverDwellTicks = 100,
            StrafeTicks = 70,
            DiveAttackTicks = 42,
            LandTicks = 75,
            WingFlapSpeed = 0.085f,
        };
        protected override int RandomTakeoffChance => 8;
        protected override float FlightHpEscalationFrac => 0.45f;

        protected override int MeleeWeaponItemType => ModContent.ItemType<Wyrmkiller>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => 44;
        protected override int RangedDamage => 0;

        protected override float TopSpeed => 2.8f;
        protected override float Acceleration => 0.1f;
        protected override float MeleeRange => 82f;
        protected override int MeleeTelegraphTicks => 45;
        protected override bool SlowDownBeforeMelee => false;
        protected override Color MeleeTelegraphFlashColor => new Color(80, 255, 120);
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6f;
        protected override bool EvadesProjectiles => true;
        protected override int PreemptiveQuickStepChance => 45;
        protected override int QuickStepRecoveryTicks => 18;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 75;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, 3, 500f);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 9000;
            NPC.defense = 28;
            NPC.damage = 0;
            NPC.knockBackResist = 0.18f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 18000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;
            Music = MusicID.Boss2; // Phase-one placeholder; intentionally distinct from the Spirit track.

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 36f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.Agility = 0.72f;
            globalNPC.CanDodgeroll = true;
            globalNPC.CanJumpToEvade = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
            globalNPC.TeleportMaxCharges = -1;
            EvasiveProfile.Khaios(globalNPC);
        }

        public override bool CheckDead()
        {
            // Phase one is a real death, but not the completion of the encounter. Clear the flags
            // consumed by GlobalNPC.OnKill so it grants neither progression nor automatic soul loot.
            NPC.boss = false;
            NPC.value = 0f;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SuppressGlobalOnKillDrops = true;
            return true;
        }

        public override void OnKill()
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsTeleportIllusion)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y,
                    ModContent.NPCType<KhaiosTransitionOrb>());
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit();
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // Melee-only (RangedWeaponItemType = -1) — the ranged phase never fires, but the abstract
        // base still requires an implementation.
        protected override void DoRangedAttack() { }
    }
}
