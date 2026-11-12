using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Ammo;
using tsorcRevamp.Content.Items.Armor.Magic.MimeticSet;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Rare cloaked longbowman. Walks invisible (Predator cloak), decloaks to draw an Elfin Bow, and fires one heavy
    /// leading Arrow of Bard per cycle; hyper-armored from the aim lock. Blinks away in grey smoke when hit (1 in 15),
    /// when it loses you, and after every sixth shot. No melee: it backs off from anyone who closes in. At 25% life it
    /// vanishes in smoke for 15s, then returns cloaked ~40 tiles from its prey (never while they rest at a bonfire or in town).
    /// </summary>
    public class Assassin : HunterArcherPuppet, IHitReactor
    {
        // ── Arrow of Bard shot ───────────────────────────────────────────────────────────────────────────────
        // Legacy ArcherAI values: raw 50 (= 200 Expert), 14 px/t, a 100t cycle with the aim and the green flash
        // 30t before the release. Draw 50 + release 6 + recovery 30 + cooldown 14 = 100t.
        private const int ArrowExpertDamage = 200;
        private const float ArrowSpeedPixels = 14f;
        private const int DrawTicks = 50;
        private const int AimLockTickCount = 30;
        private const int ReleaseTicks = 6;
        private const int ShotRecoveryTicks = 30;
        private const int ShotCooldownTicks = 14;
        private const float MaxShotRange = 700f;
        private static readonly Color BardGreen = new Color(90, 220, 100);

        // ── Elfin Bow geometry (14x58, facing right) ─────────────────────────────────────────────────────────
        // Riser x6..13 / y24..33 (grip centre 10, 29). String = the 2px outline column x0..1 / y10..47 between the
        // curled limb tips. Drawn at 0.75 (43px): full size would out-size the 42px body.
        private const float BowScale = 0.75f;
        private static readonly Vector2 ElfinBowGripNorm = new Vector2(10f / 14f, 29f / 58f);
        private static readonly Rectangle ElfinBowStringTexels = new Rectangle(0, 10, 2, 38);
        private static readonly Color ElfinBowStringColor = new Color(3, 37, 11);
        // The 14x50 Arrow of Bard sprite at 0.6 = 30px on the 43px bow.
        private const float NockedArrowScale = 0.6f;

        // Damage 0 and no swing hand ever selects the back slot, so the carried bow never tests a blade capsule.
        private static readonly PuppetWeapon CarriedElfinBow = new PuppetWeapon(
            ModContent.ItemType<EnemyAssassinBow>(),
            damage: 0,
            handleNorm: ElfinBowGripNorm,
            drawScale: BowScale,
            bladeWidth: 1f);

        // ── Movement ─────────────────────────────────────────────────────────────────────────────────────────
        // Legacy enrage: top speed doubles below 40% life.
        private const float BaseTopSpeed = 2f;
        private const float EnragedTopSpeed = 4f;
        private const float EnrageLifeFraction = 0.4f;

        // ── Blinks ───────────────────────────────────────────────────────────────────────────────────────────
        // Hit blink: 1 in 15 hits, farther (tiles) when the hit came from range, since it is breaking a shooter's line.
        private const int HitBlinkChance = 15;
        private const int HitBlinkMeleeRangeTiles = 30;
        private const int HitBlinkRangedRangeTiles = 40;
        // Smoke Step: after every SmokeStepShots completed releases, blink to high ground within range with LOS.
        private const int SmokeStepShots = 6;
        private const int SmokeStepRangeTiles = 25;
        private const int SmokeStepTelegraphTicks = 30;

        // ── Vanish ───────────────────────────────────────────────────────────────────────────────────────────
        // Once, when life first drops below 25%: a smoke burst, a chat line, then 15s fully gone (hidden, untouchable,
        // inert). It returns, already cloaked and with no entry dust, 36-44 tiles from the closest player, but only
        // once that player is neither resting at a bonfire (Bonfire buff) nor in town (a town NPC close by).
        private const float VanishLifeFraction = 0.25f;
        private const int VanishHiddenTicks = 900;
        private const int ReturnMinRangeTiles = 36;
        private const int ReturnMaxRangeTiles = 44;
        // Vanilla Player.townNPCs (computed on the authority by the spawn code): 1 = one town NPC close by.
        private const float TownNPCThreshold = 1f;
        private const float VanishMistRadius = 48f;

        private enum VanishState : byte
        {
            NotYet,
            Hidden,
            Returned
        }

        // Synced: picks hidden vs hunting on every peer. The countdown is server-only (clients never decide the return).
        private VanishState _vanishState = VanishState.NotYet;
        private int _vanishTimer;
        // Local: the state last applied on this machine, so each peer runs the enter/leave effects exactly once.
        private VanishState _appliedVanishState = VanishState.NotYet;

        // Server only, never synced: completed releases since the last Smoke Step. Clients never decide on it.
        private int _shotsSinceSmokeStep;
        bool introDone = false;

        private bool Vanished => _vanishState == VanishState.Hidden;

        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────────
        public override bool UsesPuppetDifficultyScaling => false;
        protected override string InvaderTitle => "Assassin";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool EvadesProjectiles => false;
        protected override int HeadArmorItemType => ModContent.ItemType<MimeticHat>();
        protected override int BodyArmorItemType => ModContent.ItemType<MimeticSuit>();
        protected override int LegsArmorItemType => ModContent.ItemType<MimeticPants>();
        protected override float Acceleration => 0.1f;

        protected override float TopSpeed
        {
            get
            {
                // NPC.life is synced, so every peer agrees on the enraged speed.
                float lifeFraction = NPC.life / (float)NPC.lifeMax;

                if (lifeFraction < EnrageLifeFraction)
                {
                    return EnragedTopSpeed;
                }

                return BaseTopSpeed;
            }
        }

        // No melee: it never closes in to fight, it backs away (kite band, SetDefaults).
        protected override int MeleeWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override float MeleeRange => 0f;

        // Bow (far hand at rest, shared string-draw pose to shoot; see HunterArcherPuppet).
        protected override PuppetWeapon CarriedBow => CarriedElfinBow;
        protected override Vector2 BowGripNorm => ElfinBowGripNorm;
        protected override Rectangle BowStringTexels => ElfinBowStringTexels;
        protected override float BowDrawScale => BowScale;
        protected override Color BowStringColor => ElfinBowStringColor;
        protected override int ArrowProjectileType => ModContent.ProjectileType<EnemyArrowOfBard>();
        protected override float ArrowSpeed => ArrowSpeedPixels;
        protected override int AimLockTicks => AimLockTickCount;
        protected override bool HyperArmorFromAimLock => true;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyAssassinBow>();
        protected override int RangedDamage => EnemyDamage.Projectile(ArrowExpertDamage);
        protected override float RangedRange => MaxShotRange;
        // Shoots point-blank, as the legacy archer did.
        protected override float MinRangedRange => 0f;
        protected override int RangedTelegraphTicks => DrawTicks;
        protected override int RangedAttackTicks => ReleaseTicks;
        protected override int RangedRecoveryTicks => ShotRecoveryTicks;
        protected override int RangedCooldownAfterUse => ShotCooldownTicks;
        protected override Color RangedTelegraphFlashColor => Color.Green;
        protected override string NockedBowArrowTexture => UsefulFunctions.RefactorableFilepath(typeof(ArrowOfBardProjectile));
        protected override float NockedBowArrowDrawScale => NockedArrowScale;

        // ── Cloak ────────────────────────────────────────────────────────────────────────────────────────────
        // GlobalNPC's Predator cloak sets NPC.alpha, which fades the puppet body; this fades the held bow with it.
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;
        // The puppet draws a transparent placeholder texture, so GlobalNPC's cloak shimmer draws nothing. The
        // Hollow Warrior ghost effect (the puppet port of the same shimmer waves + travel trail) stands in while
        // cloaked. IsInvisible is synced, so every peer toggles it together. Scale 1 keeps the bow grip unmoved.
        protected override bool HasSpectralOverlay => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsInvisible;
        protected override bool UseHollowWarriorGhostEffect => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override float SpectralCoreTintStrength => 0.3f;

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.npcSlots = 3;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 500;
            NPC.defense = 40;
            NPC.value = 5000; // life / 1 bc rare : was 460
            NPC.knockBackResist = 0.2f;
            NPC.rarity = 3;
            EnemyDamage.SetContact(NPC, 0);
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AssassinBanner>();

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 60;
                NPC.value = 20000; // life / 1 bc rare : was 690
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Legacy archer levers: blink to elevated firing spots, reposition toward the last-known position.
            globalNPC.PrefersHighGround = true;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;

            // Hold inside 15 tiles (the legacy standoff), back away (cloaked) from anyone inside 6.
            globalNPC.KiteRangeMin = 6f;
            globalNPC.KiteRangeMax = 15f;
            globalNPC.KiteLooseness = 0.35f;
            globalNPC.CanAdvanceAndShoot = false;

            // LOS-loss blink (the legacy canTeleport migration: Normal style) in heavy grey smoke.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;

            // Predator cloak: invisible while moving and not telling an attack; a hit reveals it for 90t.
            globalNPC.CanGoInvisible = true;
            globalNPC.InvisibleAlpha = 220;
            globalNPC.InvisibilityStyle = InvisibilityStyle.Predator;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ArrowOfBard>(), 6, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofCursedFlames, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 36));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 28));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 22));
            npcLoot.Add(ItemDropRule.Common(ItemID.HolyArrow, 1, 100, 150));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 6));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }

        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            // One chat line per Assassin, picked by where its closest player is. BroadcastText sends from the server
            // and does nothing on clients, so only the authority runs it.
            if (!introDone && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = UsefulFunctions.GetClosestPlayer(NPC.Center);

                if (Main.hardMode && !Main.dayTime && target.ZoneJungle && !target.ZoneOverworldHeight && !target.ZoneDungeon && !target.ZoneCorrupt && !target.ZoneCrimson)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Track"), 175, 75, 255);
                }
                else if (Main.hardMode && !Main.dayTime && target.ZoneJungle && !target.ZoneDungeon && !target.ZoneCorrupt && !target.ZoneCrimson)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Draw"), 175, 75, 255);
                }
                else if (Main.hardMode && Main.dayTime && target.ZoneJungle && !target.ZoneDungeon && !target.ZoneCorrupt && !target.ZoneCrimson)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Steps"), 175, 75, 255);
                }
                else if (target.ZoneDungeon || target.ZoneHallow || target.ZoneSnow || target.ZoneUndergroundDesert || target.ZoneDesert)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Track"), 175, 75, 255);
                }
                else if (Main.hardMode && !Main.dayTime && target.ZoneOverworldHeight)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Hunt"), 175, 75, 255);
                }

                introDone = true;
            }

            NPC.timeLeft = 100;

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            tsorcRevampGlobalNPC vanishGlobal = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Vanish trigger (server): the first tick below 25% life. Drops whatever it was doing.
            bool belowVanishLife = NPC.life < NPC.lifeMax * VanishLifeFraction;

            if (authority && _vanishState == VanishState.NotYet && belowVanishLife)
            {
                _vanishState = VanishState.Hidden;
                _vanishTimer = VanishHiddenTicks;
                EnterPhase(AttackPhase.Idle, 0);
                // A hit blink or Smoke Step queued this tick would otherwise fire as soon as it returns.
                vanishGlobal.TeleportCountdown = 0;

                // ai[0] 0 = grey smoke; ai[1] = scatter radius. The mist projectile syncs itself to every client.
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<TeleportMistLinger>(), 0, 0f, Main.myPlayer, 0f, VanishMistRadius);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Assassin.Vanished"), 175, 75, 255);
                NPC.netUpdate = true;
            }

            // Return (server): after the hidden time, as soon as the closest player is out hunting again.
            if (authority && Vanished)
            {
                if (_vanishTimer > 0)
                {
                    _vanishTimer--;
                }
                else
                {
                    NPC.TargetClosest(false);
                    Player prey = Main.player[NPC.target];
                    bool preyHunted = NPC.HasValidTarget;
                    bool preyAtBonfire = prey.HasBuff(ModContent.BuffType<Buffs.Bonfire>());
                    bool preyInTown = prey.townNPCs >= TownNPCThreshold;

                    if (preyHunted && !preyAtBonfire && !preyInTown)
                    {
                        // GenerateTeleportPosition refuses an NPC more than 125 tiles from its target, so search from
                        // the prey's own position; put the hidden body back if no landing spot turns up this tick.
                        Vector2 hiddenAt = NPC.Center;
                        NPC.Center = prey.Center;
                        Vector2? landing = tsorcRevampAIs.GenerateTeleportPosition(NPC, ReturnMaxRangeTiles,
                            requireLineofSight: false, preferHighGround: true, minRange: ReturnMinRangeTiles);

                        if (landing.HasValue)
                        {
                            NPC.Center = landing.Value;
                            _vanishState = VanishState.Returned;

                            // Arrive cloaked and already walking at the prey: Predator only stays invisible while moving.
                            int towardPrey = Math.Sign(prey.Center.X - NPC.Center.X);

                            if (towardPrey == 0)
                            {
                                towardPrey = 1;
                            }

                            NPC.direction = towardPrey;
                            NPC.velocity = new Vector2(towardPrey * TopSpeed, 0f);
                            vanishGlobal.IsInvisible = true;
                            NPC.alpha = vanishGlobal.InvisibleAlpha;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            NPC.Center = hiddenAt;
                        }
                    }
                }
            }

            // Enter/leave effects, once per peer, whichever way the state arrived (set above or by a packet).
            if (_vanishState != _appliedVanishState)
            {
                if (_vanishState == VanishState.Hidden)
                {
                    vanishGlobal.CanTeleport = false;

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                    }
                }
                else if (_vanishState == VanishState.Returned)
                {
                    vanishGlobal.CanTeleport = true;
                    NPC.dontTakeDamage = false;
                    NPC.ShowNameOnHover = true;
                }

                _appliedVanishState = _vanishState;
            }

            // Hidden: inert, untouchable and undrawn (PreDraw/PostDraw/DrawHealthBar), so the puppet AI doesn't run.
            if (Vanished)
            {
                NPC.velocity = Vector2.Zero;
                NPC.dontTakeDamage = true;
                NPC.ShowNameOnHover = false;
                NPC.damage = 0;
                DebugAttackLabel = "Vanished";
                return;
            }

            base.AI();

            // The puppet body never deals contact damage; the arrows are the only hits.
            NPC.damage = 0;

            DebugAttackLabel = null;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (InShotPose || Phase == AttackPhase.RangedRecovery)
            {
                DebugAttackLabel = "Arrow of Bard";
            }

            if (globalNPC.TeleportCountdown > 0)
            {
                DebugAttackLabel = "Smoke Step";
            }
        }

        protected override void DoMeleeAttack()
        {
        }

        // Counts completed releases for Smoke Step (a draw cancelled by a hit never gets here).
        protected override void DoRangedAttack()
        {
            base.DoRangedAttack();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _shotsSinceSmokeStep++;
            }
        }

        // Smoke Step: every SmokeStepShots-th shot, blink in grey smoke to high ground within range that still sees
        // the target. QueueTeleport syncs the countdown directly; if no spot was found the count stays armed and the
        // next recovery tries again.
        protected override void OnShotRecoveryStarted(bool authority)
        {
            if (!authority || _shotsSinceSmokeStep < SmokeStepShots || !NPC.HasValidTarget)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (globalNPC.TeleportCountdown > 0)
            {
                return;
            }

            tsorcRevampAIs.QueueTeleport(NPC, SmokeStepRangeTiles, requireLineofSight: true,
                TeleportTelegraphTime: SmokeStepTelegraphTicks, preferHighGround: true);

            bool queued = globalNPC.TeleportCountdown > 0;

            if (queued)
            {
                _shotsSinceSmokeStep = 0;
            }
        }

        // Full draw: a soft green light and a rising green mote every 4t at the nocked arrowhead.
        protected override void DoArrowheadVFX(Vector2 arrowhead)
        {
            Lighting.AddLight(arrowhead, 0.12f, 0.4f, 0.14f);

            if (Main.GameUpdateCount % 4 == 0)
            {
                Dust mote = Dust.NewDustPerfect(arrowhead, DustID.GreenTorch, new Vector2(0f, -0.4f), 100, BardGreen, 0.8f);
                mote.noGravity = true;
            }
        }

        // Its only hit reaction is the server-side smoke blink (IHitReactor): no generic hops or evasion lottery.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            bool melee = projectile.DamageType == DamageClass.Melee;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, melee);
        }

        /// <summary>Blink away from roughly one hit in fifteen — further when the hit came from range, since the
        /// assassin is trying to break a shooter's line rather than step out of a swing. Server/singleplayer only
        /// (see IHitReactor). Never during the committed shot (hyper-armored from the aim lock through the release)
        /// or while another blink is already queued.</summary>
        void IHitReactor.OnServerHit(NPC npc, bool melee)
        {
            bool lockedDraw = Phase == AttackPhase.RangedTelegraph && PhaseTimer <= AimLockTicks;
            bool committedShot = lockedDraw || Phase == AttackPhase.RangedAttack;
            bool blinkQueued = npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown > 0;

            if (committedShot || blinkQueued)
            {
                return;
            }

            if (!Main.rand.NextBool(HitBlinkChance))
            {
                return;
            }

            int teleportRange = HitBlinkRangedRangeTiles;

            if (melee)
            {
                teleportRange = HitBlinkMeleeRangeTiles;
            }

            tsorcRevampAIs.TeleportImmediately(npc, teleportRange, false);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Vanished)
            {
                return false;
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Vanished)
            {
                return;
            }

            base.PostDraw(spriteBatch, screenPos, drawColor);
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (Vanished)
            {
                return false;
            }

            return null;
        }

        // Subclass state first, then the hunter base, then PuppetNPC, read back in the same order.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_vanishState);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _vanishState = (VanishState)reader.ReadByte();
            base.ReceiveExtraAI(reader);
        }

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Assassin Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}
