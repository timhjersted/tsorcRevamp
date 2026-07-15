using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>Final encounter phase. Weapon kit and unique drop are placeholders until designed.</summary>
    public class SpiritOfKhaios : PuppetNPC
    {
        protected override string InvaderTitle => "Spirit of Khaios";
        protected override Color PuppetSkinColor => new Color(0xF4, 0x86, 0x67);

        protected override int HeadArmorItemType => ItemID.SpookyHelmet;
        protected override int BodyArmorItemType => ItemID.GhostShirt;
        protected override int LegsArmorItemType => ItemID.None;
        protected override int HeadArmorDyeItemType => ItemID.ReflectiveGoldDye;
        protected override int BodyArmorDyeItemType => ItemID.SilverDye;

        protected override bool HasWings => true;
        protected override bool ShowWingsWhenGrounded => true;
        protected override int WingsAccessoryItemType => ItemID.WingsSolar;
        protected override int WingsDyeItemType => ItemID.BlackDye;

        protected override int[] AccessoryItemTypes => new int[]
        {
            ItemID.GlassSlipper,
            ItemID.AngelHalo,
            ItemID.CrossNecklace,
            ItemID.PaladinsShield,
        };
        protected override int[] AccessoryDyeItemTypes => new int[]
        {
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
        };

        protected override EnemyFlightConfig FlightConfig => EnemyFlightConfig.Default;
        protected override int RandomTakeoffChance => 8;
        protected override float FlightHpEscalationFrac => 0.5f;

        protected override int MeleeWeaponItemType => ModContent.ItemType<Wyrmkiller>();
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ItemID.SkyFracture;
        protected override int MeleeDamage => 52;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => 34;
        protected override float TopSpeed => 3f;
        protected override float Acceleration => 0.105f;
        protected override float MeleeRange => 84f;
        protected override int MeleeTelegraphTicks => 45;
        protected override bool SlowDownBeforeMelee => false;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 225, 120);
        protected override float MagicRange => 720f;
        protected override float MinMagicRange => 150f;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicRecoveryTicks => 55;
        protected override int MagicCooldownAfterUse => 240;
        protected override Color MagicTelegraphFlashColor => new Color(125, 235, 255);
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6f;
        protected override bool EvadesProjectiles => true;
        protected override int PreemptiveQuickStepChance => 50;
        protected override int QuickStepRecoveryTicks => 16;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, 3, 520f);
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
            NPC.lifeMax = 11000;
            NPC.defense = 30;
            NPC.damage = 0;
            NPC.knockBackResist = 0.12f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 24000f;
            NPC.boss = true;
            NPC.npcSlots = 6f;
            Music = MusicID.Boss3; // Independent phase-two slot; replace when Khaios's track is chosen.

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 42f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.Agility = 0.78f;
            globalNPC.CanDodgeroll = true;
            globalNPC.CanJumpToEvade = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
            globalNPC.TeleportMaxCharges = -1;
            EvasiveProfile.Khaios(globalNPC);
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
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

        // Sky Fracture reproduces the vanilla item's cadence: three blades four ticks apart,
        // followed by a short reuse pause. Cast length is selected deterministically so clients
        // agree on the animation without needing a random duration packet.
        private const int ShortFractureTicks = 2 * 60;
        private const int LongFractureTicks = 4 * 60;
        private const int FinalStandFractureTicks = 6 * 60;
        private const int FractureCycleTicks = 30;
        private const float SkyFractureLeadStrength = 0.7f;
        private const float SkyFractureMaxLeadTicks = 24f;
        private int skyFractureCastCount;
        private int activeSkyFractureDuration;

        protected override void DoMagicAttack()
        {
            skyFractureCastCount++;
            int roll = ((NPC.whoAmI * 397) ^ (skyFractureCastCount * 7919)) & 0xFFFF;
            int percent = roll % 100;
            bool finalStand = NPC.life < NPC.lifeMax * 0.2f;

            activeSkyFractureDuration = finalStand && percent < 10
                ? FinalStandFractureTicks
                : percent < (finalStand ? 35 : 25)
                    ? LongFractureTicks
                    : ShortFractureTicks;
            _magicAttackTicksOverride = activeSkyFractureDuration;
            if (Main.netMode == NetmodeID.Server)
            {
                NPC.netUpdate = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(skyFractureCastCount);
            writer.Write(activeSkyFractureDuration);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            skyFractureCastCount = reader.ReadInt32();
            activeSkyFractureDuration = reader.ReadInt32();
            if (activeSkyFractureDuration > 0)
            {
                _magicAttackTicksOverride = activeSkyFractureDuration;
            }
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            int elapsed = activeSkyFractureDuration - ticksRemaining;
            int cycleTick = elapsed % FractureCycleTicks;
            if (cycleTick != 0 && cycleTick != 4 && cycleTick != 8)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                FireHostileSkyFracture();
            }
        }

        private void FireHostileSkyFracture()
        {
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            Vector2 castOrigin = NPC.Center + new Vector2(NPC.direction * 10f, -8f);
            float distance = Vector2.Distance(castOrigin, target.Center);
            float leadTicks = MathHelper.Clamp(distance / 14f, 0f, SkyFractureMaxLeadTicks)
                * SkyFractureLeadStrength;
            Vector2 aimPoint = target.Center + target.velocity * leadTicks;

            // Vanilla Sky Fracture chooses a visible point in a 20-60 px ring around the caster,
            // retrying when terrain blocks that point, then blends the target vector toward the
            // direct firing vector. The target here is our deterministic led player position.
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawn = castOrigin;
            for (int i = 0; i < 50; i++)
            {
                spawn = castOrigin + angle.ToRotationVector2() * Main.rand.NextFloat(20f, 60f);
                Vector2 outward = (spawn - castOrigin).SafeNormalize(Vector2.UnitX) * 8f;
                if (Collision.CanHit(castOrigin, 0, 0, spawn + outward, 0, 0))
                {
                    break;
                }
                angle = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            Vector2 directVelocity = (aimPoint - castOrigin).SafeNormalize(Vector2.UnitY) * 14f;
            Vector2 velocity = (aimPoint - spawn).SafeNormalize(directVelocity) * 14f;
            velocity = Vector2.Lerp(velocity, directVelocity, 0.25f);

            int projectileIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, velocity,
                ProjectileID.SkyFracture, MagicDamage, 4f, Main.myPlayer);
            if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
            {
                return;
            }
            Projectile projectile = Main.projectile[projectileIndex];
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.netUpdate = true;
        }

        // Melee-only (RangedWeaponItemType = -1) — the ranged phase never fires, but the abstract
        // base still requires an implementation.
        protected override void DoRangedAttack() { }
    }
}
