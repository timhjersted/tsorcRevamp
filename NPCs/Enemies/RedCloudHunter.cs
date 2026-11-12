using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.Enemy.ManhunterSet;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Cloud Hunter (class name kept for banners, spawns and saves): a rare elite sniper in the Man Hunter's
    /// gear, black-dyed. Kites at long range from high ground, shoots fast frostburn arrows in volleys that
    /// start early, steps through a player who hits him and answers that crossing with a snap Turnabout Shot.
    /// </summary>
    public class RedCloudHunter : HunterArcherPuppet
    {
        // ── Frostburn Arrow ──────────────────────────────────────────────────────────────────────────────────
        // Legacy ArcherAI: 100t cycle, aim 50t, aim locked (red flash) 15t before the release, 13 px/t.
        // Legacy spawn damage 30 / 45 / 75 x4 = Expert 120 / 180 / 300 by tier.
        private const int PreHardmodeArrowExpertDamage = 120;
        private const int HardmodeArrowExpertDamage = 180;
        private const int SuperHardmodeArrowExpertDamage = 300;
        private const float ArrowSpeedPixels = 13f;
        private const int DrawTicks = 50;
        private const int AimLockTickCount = 15;
        private const int ReleaseTicks = 6;
        // Release 6 + recovery 30 + cooldown 14 = the legacy 50t between the shot and the next aim.
        private const int ShotRecoveryTicks = 30;
        private const int ShotCooldownTicks = 14;
        // Legacy CombatTempoProfile.Elite: each chained shot added 17t to the post-chain recovery.
        private const int ChainRecoveryTicksPerExtraShot = 17;
        private const float MaxShotRange = 700f;
        private static readonly Color FrostGlintLight = new Color(0.2f, 0.45f, 0.6f);

        // ── Turnabout Shot ───────────────────────────────────────────────────────────────────────────────────
        // After a forward QuickStep that crossed through the target: one snap shot into its back.
        // 30t draw, aim locked 12t before the release, 24t recovery, no volley, 6s cooldown.
        private const int TurnaboutDrawTicks = 30;
        private const int TurnaboutAimLockTicks = 12;
        private const int TurnaboutRecoveryTicks = 24;
        private const int TurnaboutCooldownTicks = 360;
        private const float TurnaboutMinRange = 48f;
        private const float TurnaboutMaxRange = 400f;
        // The queued shot must start within this many ticks of the step ending, or the Turnabout is dropped.
        private const int TurnaboutQueueTicks = 12;

        // ── Movement (tuned by the 2026 nav pass; type defaults on every peer) ──────────────────────────────
        // Floaty high jumps: 0.75 gravity with 11 jump power / 2 boost (apex ~16.8 tiles); a 3.2 px/t
        // horizontal jump cap keeps launches inside what the nav planner predicts.
        private const float GravityMultiplier = 0.75f;
        private const float BaseTopSpeed = 2f;
        private const float EnragedTopSpeed = 2.6f;
        private const float EnrageLifeFraction = 0.3f;

        // ── Shadewood Bow geometry (16x32, facing right; same silhouette as the Wooden Bow) ──────────────────
        // Riser centre (13, 16); string inset at x2..3 / y6..25, limb tips at x0..5 above and below it.
        private static readonly Vector2 ShadewoodBowGripNorm = new Vector2(13f / 16f, 0.5f);
        private static readonly Rectangle ShadewoodBowStringTexels = new Rectangle(2, 6, 2, 20);
        // The sprite's own string pixel colour, so the live string matches the baked one.
        private static readonly Color ShadewoodBowStringColor = new Color(24, 31, 44);

        // ── Volleys ──────────────────────────────────────────────────────────────────────────────────────────
        // One static table so every peer rebuilds the same pause row from the synced pattern index. Each entry is
        // the CrossbowBurstPause before the next shot: release 6 + pause 17/22/27 = the legacy 23-33t spacing.
        private static readonly int[][] VolleyPatterns =
        {
            new int[0],
            new[] { 17 }, new[] { 22 }, new[] { 27 },
            new[] { 17, 17 }, new[] { 22, 22 }, new[] { 27, 27 },
            new[] { 17, 17, 17 }, new[] { 22, 22, 22 }, new[] { 27, 27, 27 },
        };
        // Weights per HP band, approximating Elite's 0.76 continue chance (8 in 10): one shot above 90% HP,
        // mostly two at or below, mostly three under 70%, mostly four under 40%.
        private static readonly int[] SingleShotChances = { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly int[] TwoShotChances = { 20, 27, 27, 26, 0, 0, 0, 0, 0, 0 };
        private static readonly int[] ThreeShotChances = { 0, 7, 7, 6, 27, 27, 26, 0, 0, 0 };
        private static readonly int[] FourShotChances = { 0, 0, 0, 0, 7, 7, 6, 27, 27, 26 };

        // Damage 0 and no swing hand ever selects the back slot, so the carried bow never tests a blade capsule.
        private static readonly PuppetWeapon CarriedShadewoodBow = new PuppetWeapon(
            ModContent.ItemType<EnemyRedCloudHunterBow>(),
            damage: 0,
            handleNorm: ShadewoodBowGripNorm,
            drawScale: 1f,
            bladeWidth: 1f);

        // Tier arrow damage, picked in SetDefaults from world flags (identical on every peer).
        private int _arrowExpertDamage = PreHardmodeArrowExpertDamage;
        // Synced: true from the Turnabout trigger until its recovery starts. Picks the 30t draw, 12t lock and
        // 24t recovery, so a client needs it to raise the bow over the right span.
        private bool _turnabout;
        // Server only: cooldown, queue expiry and last tick's QuickStepTimer (to spot the step ending).
        private int _turnaboutCooldown;
        private int _turnaboutQueueTimer;
        private int _lastQuickStepTimer;

        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────────
        public override bool UsesPuppetDifficultyScaling => false;
        protected override string InvaderTitle => "Cloud Hunter";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool EvadesProjectiles => false;
        protected override int HeadArmorItemType => ModContent.ItemType<ManhunterHat>();
        protected override int BodyArmorItemType => ModContent.ItemType<ManhunterJerkin>();
        protected override int LegsArmorItemType => ModContent.ItemType<ManhunterBoots>();
        // Black coat and trousers, undyed hat so the face keeps its skin tone. The jerkin's baked hands go
        // charcoal too and read as black gloves.
        protected override int BodyArmorDyeItemType => ItemID.BlackDye;
        protected override int LegsArmorDyeItemType => ItemID.BlackDye;
        protected override float Acceleration => 0.07f;
        // Legacy enrage: faster below 30% life. NPC.life is synced, so every peer agrees.
        protected override float TopSpeed => NPC.life < NPC.lifeMax * EnrageLifeFraction ? EnragedTopSpeed : BaseTopSpeed;

        // PuppetNPC writes these onto the GlobalNPC every tick, so they must be overrides, not SetDefaults values.
        protected override float PuppetJumpPower => 11f;
        protected override float PuppetJumpBoost => 2f;
        protected override float QuickStepForwardRoom => 56f;

        // No melee: the bow and the Turnabout Shot are his whole kit.
        protected override int MeleeWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override float MeleeRange => 0f;

        // Bow (far hand at rest, shared string-draw pose to shoot; see HunterArcherPuppet).
        protected override PuppetWeapon CarriedBow => CarriedShadewoodBow;
        protected override Vector2 BowGripNorm => ShadewoodBowGripNorm;
        protected override Rectangle BowStringTexels => ShadewoodBowStringTexels;
        protected override Color BowStringColor => ShadewoodBowStringColor;
        protected override int ArrowProjectileType => ModContent.ProjectileType<EnemyFrostburnArrow>();
        protected override float ArrowSpeed => ArrowSpeedPixels;
        protected override int AimLockTicks => _turnabout ? TurnaboutAimLockTicks : AimLockTickCount;
        protected override int ChainRecoveryPerExtraShot => ChainRecoveryTicksPerExtraShot;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyRedCloudHunterBow>();
        protected override int RangedDamage => EnemyDamage.Projectile(_arrowExpertDamage);
        protected override float RangedRange => MaxShotRange;
        // Legacy had no minimum: cornered, he still shoots point-blank.
        protected override float MinRangedRange => 0f;
        // Read when a shot is set up and by the base raise; _turnabout is set before the queued shot starts.
        protected override int RangedTelegraphTicks => _turnabout ? TurnaboutDrawTicks : DrawTicks;
        protected override int RangedAttackTicks => ReleaseTicks;
        protected override int RangedRecoveryTicks => _turnabout ? TurnaboutRecoveryTicks : ShotRecoveryTicks;
        protected override int RangedCooldownAfterUse => ShotCooldownTicks;
        protected override Color RangedTelegraphFlashColor => Color.Red;
        protected override int[][] PrimaryRangedBurstPatterns => VolleyPatterns;
        protected override int[] PrimaryRangedBurstChances
        {
            get
            {
                float healthFraction = NPC.life / (float)NPC.lifeMax;

                if (healthFraction < 0.40f)
                {
                    return FourShotChances;
                }

                if (healthFraction < 0.70f)
                {
                    return ThreeShotChances;
                }

                if (healthFraction <= 0.90f)
                {
                    return TwoShotChances;
                }

                return SingleShotChances;
            }
        }
        protected override string NockedBowArrowTexture => "Terraria/Images/Projectile_" + ProjectileID.FrostburnArrow;

        // ── Cloak ────────────────────────────────────────────────────────────────────────────────────────────
        // GlobalNPC's Evasive cloak sets NPC.alpha (240 = ~6% visible); this fades the held bow with the body. The
        // puppet draws a transparent placeholder texture, so the Hollow Warrior ghost effect stands in for
        // GlobalNPC's cloak shimmer. IsInvisible is synced, so every peer toggles it together. The cloak may last
        // through a draw (legacy); the red flash projectile and the arrowhead glint dust stay fully visible.
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;
        protected override bool HasSpectralOverlay => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsInvisible;
        protected override bool UseHollowWarriorGhostEffect => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override float SpectralCoreTintStrength => 0.3f;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.npcSlots = 5;
            NPC.rarity = 3;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            // Authored HP is a placeholder: Systems/EnemyBalance.cs sets the real tier values.
            NPC.lifeMax = 600;
            NPC.defense = 18;
            NPC.value = 5210; // life / 1.15 bc rare : was 650
            NPC.knockBackResist = 0.6f;
            NPC.buffImmune[BuffID.Confused] = true;
            EnemyDamage.SetContact(NPC, 0);
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedCloudHunterBanner>();
            _arrowExpertDamage = PreHardmodeArrowExpertDamage;

            if (Main.hardMode)
            {
                NPC.lifeMax = 700;
                NPC.defense = 40;
                NPC.value = 4000; // was 350
                _arrowExpertDamage = HardmodeArrowExpertDamage;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 58;
                NPC.value = 8000; // life / 2.5 : was 390
                _arrowExpertDamage = SuperHardmodeArrowExpertDamage;
            }

            // Legacy archer levers, unchanged: take high ground, reposition toward the last-known position, kite
            // 10-40 tiles, close the gap faster between shots but stop to fire.
            tsorcRevampGlobalNPC hunterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            hunterGlobalNPC.MaxNavigationJumpSpeed = 3.2f;
            hunterGlobalNPC.PrefersHighGround = true;
            hunterGlobalNPC.RemembersLastKnownPos = true;
            hunterGlobalNPC.NavSearchRadius = 80;
            hunterGlobalNPC.KiteRangeMin = 10f;
            hunterGlobalNPC.KiteRangeMax = 40f;
            hunterGlobalNPC.KiteLooseness = 0.2f;
            hunterGlobalNPC.CanAdvanceAndShoot = true;
            hunterGlobalNPC.CanFireWhileAdvancing = false;
            hunterGlobalNPC.AdvanceAndShootSpeedMultiplier = 1.5f;
            hunterGlobalNPC.AdvanceAndShootAccelerationMultiplier = 1.4f;

            // LOS-loss blink (the legacy canTeleport migration: Normal style).
            hunterGlobalNPC.CanTeleport = true;

            // Threat cloak: a hit has a 10% chance to cloak him for 3s (faint 6% silhouette).
            hunterGlobalNPC.CanGoInvisible = true;
            hunterGlobalNPC.InvisibleAlpha = 240;
            EvasiveProfile.EvasiveCloak(hunterGlobalNPC, cloakChance: 0.10f, threatRange: 0);

            // On-hit QuickStep: 85% cross completely through the target (the Turnabout Shot's trigger), else backstep.
            hunterGlobalNPC.EvasiveQuickStep = true;
            hunterGlobalNPC.QuickStepSpeed = 6.5f;
            hunterGlobalNPC.QuickStepForwardChance = 0.85f;
            hunterGlobalNPC.QuickStepMaxForwardTicks = 40;

            // Poise (a stagger guarantees a cloak reveal) + knockback flinch are tuned centrally in
            // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) — not here.
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.AmmoReservationPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.HolyArrow, 1, 30, 60));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 3, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 6, 8));

            // Archer Spirit Bell: guaranteed on the world's first kill, the legacy 1-in-10 after. The first-kill check
            // reads NewSlain, which OnKill fills in; loot resolves before OnKill, so the first kill still drops it.
            IItemDropRule bellFirstKill = ItemDropRule.ByCondition(new FirstBossKillRule(requireBoss: false), ModContent.ItemType<ArcherSpiritBell>());
            bellFirstKill.OnFailedConditions(ItemDropRule.Common(ModContent.ItemType<ArcherSpiritBell>(), 10));
            npcLoot.Add(bellFirstKill);
        }

        public override void OnKill()
        {
            // Record this type's first kill in the shared world slain history (server), then sync it to clients.
            if (Main.netMode != NetmodeID.MultiplayerClient && tsorcRevampWorld.NewSlain.TryAdd(new NPCDefinition(NPC.type), 1))
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        public override void AI()
        {
            // tML resets this multiplier each tick. Apply it before navigation so the planner and the engine use
            // the same gravity.
            NPC.GravityMultiplier *= GravityMultiplier;

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority)
            {
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

                if (_turnaboutCooldown > 0)
                {
                    _turnaboutCooldown--;
                }

                // Turnabout trigger: the tick a QuickStep ends, if it carried him past the target (the target is now
                // behind the step direction). QueueRangedFollowUp runs one fully telegraphed single shot as soon as
                // he is neutral and grounded; _turnabout gives it the 30t draw.
                bool stepJustEnded = _lastQuickStepTimer > 0 && globalNPC.QuickStepTimer == 0;
                _lastQuickStepTimer = globalNPC.QuickStepTimer;

                if (stepJustEnded && _turnaboutCooldown == 0 && IsNeutral && NPC.HasValidTarget)
                {
                    Player target = Main.player[NPC.target];
                    float targetSideAlongStep = (target.Center.X - NPC.Center.X) * globalNPC.QuickStepDir;
                    bool crossedTarget = targetSideAlongStep < 0f;
                    float distance = NPC.Distance(target.Center);
                    bool inRange = distance >= TurnaboutMinRange && distance <= TurnaboutMaxRange;
                    bool clearShot = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

                    if (crossedTarget && inRange && clearShot)
                    {
                        _turnabout = true;
                        _turnaboutCooldown = TurnaboutCooldownTicks;
                        _turnaboutQueueTimer = TurnaboutQueueTicks;
                        // The snap draw replaces the step's post-step pause.
                        globalNPC.QuickStepRecoveryTimer = 0;
                        QueueRangedFollowUp(false, NPC.target, TurnaboutQueueTicks);
                        NPC.netUpdate = true;
                    }
                }

                // A Turnabout that never started (lost sight, left the ground) expires with its queue; drop the flag so
                // the next ordinary draw keeps its 50t timing.
                if (_turnabout && IsNeutral)
                {
                    _turnaboutQueueTimer--;

                    if (_turnaboutQueueTimer < 0)
                    {
                        _turnabout = false;
                        NPC.netUpdate = true;
                    }
                }
            }

            base.AI();

            // The puppet body never deals contact damage; the arrows are the only hits.
            NPC.damage = 0;

            DebugAttackLabel = null;

            if (InShotPose || Phase == AttackPhase.RangedRecovery)
            {
                DebugAttackLabel = "Frostburn Arrow";

                if (_turnabout)
                {
                    DebugAttackLabel = "Turnabout Shot";
                }
            }
        }

        // The Turnabout ends with its recovery; the recovery length was already read when the shot was set up.
        // Every peer sees the same synced phase change, so they clear the flag together.
        protected override void OnShotRecoveryStarted(bool authority)
        {
            _turnabout = false;
        }

        protected override void DoMeleeAttack()
        {
        }

        // Full draw: a frost glint and pale light at the nocked arrowhead. Dust ignores the cloak's alpha, so while
        // cloaked the glint is thickened (every 2t, larger) to keep the draw readable.
        protected override void DoArrowheadVFX(Vector2 arrowhead)
        {
            Lighting.AddLight(arrowhead, FrostGlintLight.ToVector3());

            bool cloaked = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsInvisible;
            uint glintInterval = 4;
            float glintScale = 0.9f;

            if (cloaked)
            {
                glintInterval = 2;
                glintScale = 1.3f;
            }

            if (Main.GameUpdateCount % glintInterval == 0)
            {
                Dust glint = Dust.NewDustPerfect(arrowhead, DustID.IceTorch, new Vector2(0f, -0.3f), 100, default, glintScale);
                glint.noGravity = true;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }

            if (NPC.life > 0)
            {
                return;
            }

            for (int i = 0; i < 25; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default, 1f);
            }

            if (Main.dedServ)
            {
                return;
            }

            int[] goreNumbers = { 1, 2, 3, 2, 3 };

            foreach (int goreNumber in goreNumbers)
            {
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                int goreType = Mod.Find<ModGore>("Red Cloud Hunter Gore " + goreNumber).Type;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity, goreType, 1f);
            }
        }

        // Subclass state first, then HunterArcherPuppet, then PuppetNPC, read back in the same order.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_turnabout);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _turnabout = reader.ReadBoolean();
            base.ReceiveExtraAI(reader);
        }
    }
}
