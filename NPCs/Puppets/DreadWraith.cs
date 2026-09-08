using Microsoft.Xna.Framework;
using System;
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
    /// A post-Skeletron dungeon invader: a robed wraith that batters with a burning mace up close
    /// and hurls cursed skulls from its tome at range. Kit is entirely vanilla dungeon-tier gear.
    /// </summary>
    public class DreadWraith : PuppetNPC, IFlailAnchor
    {
        protected override string InvaderTitle => "Dread Wraith";

        protected override Color PuppetSkinColor => new Color(190, 195, 200); // pale, corpse-grey skin
        protected override Color PuppetEyeColor => new Color(150, 60, 210);   // glowing violet eyes

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Vector2? chargeWaypoint = MountAI?.NavigationWaypoint;
            // A charge can begin near the normal 65-tile search limit and then target another 15 tiles
            // beyond the player. Use SF4's full window only for that committed pass; ordinary pursuit keeps
            // the cheaper radius. This remains local to Dread Wraith's explicit waypoint opt-in.
            globalNPC.NavSearchRadius = chargeWaypoint.HasValue ? 80 : 65;
            globalNPC.RemembersLastKnownPos = true;

            bool mountedMovement = IsMounted && MountAI != null;
            EnemyMountConfig mountConfig = MountConfig;
            float movementTopSpeed = (mountedMovement ? mountConfig.ApproachTopSpeed : TopSpeed) * speedMult;
            float movementAcceleration = mountedMovement
                ? (MountAI.IsCharging ? mountConfig.ChargeAcceleration : mountConfig.ApproachAcceleration)
                : Acceleration;

            SmartFighter4AI.Run(NPC,
                topSpeed: movementTopSpeed,
                acceleration: movementAcceleration,
                doorBreakingDamage: 2,
                attackRange: MagicRange,
                movementWaypoint: chargeWaypoint);

            if (chargeWaypoint.HasValue)
            {
                MountAI.ReportNavigationStatus(SmartFighter4AI.GetWaypointStatus(NPC));
            }
        }

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ItemID.SkeletronMask;
        protected override int BodyArmorItemType => ItemID.WillsBreastplate;
        protected override int LegsArmorItemType => ItemID.WillsLeggings;

        protected override int[] AccessoryItemTypes => new int[] { ItemID.MysteriousCape };
        protected override int[] AccessoryDyeItemTypes => new int[] { ItemID.BlackDye };
        protected override int HeadArmorDyeItemType => ItemID.BlackDye; // blackens the Skeletron Mask

        // Flaming Mace is rigged as a proper ball-and-chain flail (GreatBlackKnight's "signature weapon"
        // treatment) rather than a held swing — see DreadWraithMaceBall / IFlailAnchor below. Book of
        // Skulls is the "ranged" attack, delivered through the magic path rather than the physical ranged path.
        // Two melee weapons on one slot, chosen by whichever combo is currently running. These are
        // evaluated per use (not cached), so they can flip between the mace and the fork mid-fight.
        protected override int MeleeWeaponItemType => ActiveComboIsSpear ? ItemID.TheRottedFork : ItemID.FlamingMace;
        protected override WeaponArchetype MeleeArchetype => ActiveComboIsSpear ? WeaponArchetype.Halberd : WeaponArchetype.Flail;
        // The mace's visual is its ball+chain projectile, so its held sprite stays hidden — but the spear
        // has to actually be visible in hand.
        protected override bool HideHeldMeleeSprite => !ActiveComboIsSpear && !IsInSpearPhase;
        // ── Phase 2 ranged: Phantom Phoenix ───────────────────────────────────────
        // On foot only. Dismounted the wraith loses its charge pressure, so it gains a long-range
        // option to punish anyone who simply backs off. -1 while mounted disables the whole ranged
        // branch in the attack FSM.
        protected override int RangedWeaponItemType => IsMounted ? -1 : ItemID.DD2PhoenixBow;
        protected override int MagicWeaponItemType => ItemID.BookofSkulls;

        // The chain draws to (and the ball orbits/launches from) the puppet's own rigged hand position.
        // While mounted this already includes the saddle rise, so the chain hangs from the rider, not the goat.
        public Vector2 GetFlailAnchor() => PuppetHandPosition;

        // ── Mount: the death goat ─────────────────────────────────────────────────
        // Phase one is fought from the saddle. The goat has its own health pool (NPC.life during phase one),
        // and breaking it dismounts the wraith rather than killing it — see PuppetNPC.CheckDead.
        protected override bool HasMount => true;
        protected override int MountType => MountID.WallOfFleshGoat;
        protected override int MountLifeMax => 1800;
        protected override int MountTrampleDamage => 34;
        protected override float MountedHandOffsetY => -22f; // NEEDS IN-GAME CALIBRATION against the goat's saddle
        // A swing/cast may ride more slowly during approach, but once the charge commits it must finish the
        // traversal at charge speed instead of shrinking into a normal pursuit as soon as an attack starts.
        protected override float MountedAttackSpeedScale => MountAI?.IsCharging == true ? 1f : 0.55f;

        protected override EnemyMountConfig MountConfig => new EnemyMountConfig
        {
            ApproachTopSpeed     = 4.2f,
            ApproachAcceleration = 0.09f,
            ChargeTopSpeed       = 10f,    // clearly faster than the approach so the charge reads as one
            // Deliberately slow to wind up: a heavy mount should visibly build to top speed over roughly
            // a second rather than snapping to it, which also lengthens the readable part of the charge.
            // Still reaches top speed well inside ChargeMaxTicks, so the pass never stalls short.
            ChargeAcceleration   = 0.20f,
            BrakingPower         = 0.14f,
            OvershootBraking     = 0.05f,
            TurnBraking          = 0.22f,
            ChargeMinRange       = 170f,
            ChargeTriggerRange   = 950f,
            OvershootDistance    = 240f,   // ~15 tiles past before pulling up
            HopPower             = 8.5f,
            SpecialChargeChance  = 35,   // roughly one pass in three becomes the flame charge
            ChargeWindupTicks    = 90,   // planted tell, per design
            ApproachMaxTicks     = 180,
            ChargeMaxTicks       = 150,
            OvershootTicks       = 35,
            TurnAroundTicks      = 45,     // the punish window after a dodged pass
            IdleDwellTicks       = 30,
            ChargeCooldownTicks  = 55,
        };

        // Rotted Lance/JoustDash pushes forward at this speed during its own attack step (see
        // ForwardPushMult on that SpearStep). Without this override it used the plain on-foot TopSpeed
        // (2.6) even though the combo is allowed to START from as far as ComboMaxStartRange (420px) —
        // 2.6 * ForwardPushMult over the attack's own tick budget could never close that gap, so the
        // lance finished its whole thrust animation stranded well short of the target. ChargeTopSpeed
        // matches the goat's actual charge pace, since this is meant to read as a couched charge.
        protected override float ComboForwardPushTopSpeed =>
            IsMounted && MountAI != null ? MountConfig.ChargeTopSpeed : TopSpeed;

        protected override int MeleeDamage => 26;
        protected override int RangedDamage => 24;
        protected override int MagicDamage => 22;

        // ── Phantom Phoenix tuning ────────────────────────────────────────────────
        // Only opens up beyond 15 tiles (240px), per design — inside that it stays a melee fight.
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override float RangedRange => 950f;
        protected override float MinRangedRange => 15 * 16f;
        protected override int RangedTelegraphTicks => 46;
        protected override int RangedAttackTicks => 10;
        protected override int RangedRecoveryTicks => 46;
        protected override int RangedCooldownAfterUse => 300;
        protected override int StandingRangedChance => 75;   // plants to draw the bow
        protected override Color RangedTelegraphFlashColor => new Color(255, 150, 60);

        // Volleys of 3 / 6 / 9 shots. Total shots = pauses + 1, so these are 2, 5 and 8 pauses. Every
        // third arrow becomes a phoenix (see DoRangedAttack), giving 1, 2 or 3 phoenixes per volley.
        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { 14, 14 },                                     // 3 shots  -> 1 phoenix
            new int[] { 14, 14, 14, 14, 14 },                         // 6 shots  -> 2 phoenixes
            new int[] { 14, 14, 14, 14, 14, 14, 14, 14 },             // 9 shots  -> 3 phoenixes
        };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 12, 26 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            new Color(255, 180, 90),
            Color.OrangeRed,
            Color.Red,
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 55, 30, 15 };

        // ── Combat tuning ─────────────────────────────────────────────────────────
        protected override float TopSpeed => 2.6f;
        protected override float Acceleration => 0.1f;
        protected override float MeleeRange => 84f;

        protected override int MeleeTelegraphTicks => 40;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 140, 60); // fiery orange, matches the mace

        // Proximity-driven engagement: attacks may be SELECTED from well out (ComboMaxStartRange), but the
        // wraith keeps advancing through the wind-up and only commits the swing once inside real reach.
        // That gives the "weapon held out while closing, then strike" read instead of swinging at air.
        protected override float ComboMaxStartRange => 420f;
        protected override float MeleeEngageRange => 190f;   // spear reach, not point-blank
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float ComboTelegraphAdvanceStopDistance => 120f;
        protected override int MeleeComboChance => 100;

        protected override float MagicRange => 640f;
        protected override float MinMagicRange => 140f;
        protected override int MagicTelegraphTicks => 55;
        protected override int MagicAttackTicks => 16;
        protected override int MagicRecoveryTicks => 60;
        protected override int MagicCooldownAfterUse => 260;
        // Without this the skulls almost never fire in phase one. The attack FSM checks melee combos
        // FIRST and breaks out of selection, and with MeleeComboChance 100 plus a 420px combo start range
        // the goat keeps the player inside melee the whole time — so magic never got reached. The default
        // MagicPreferenceChance of 0 means "magic only fills gaps"; this lets it actively compete.
        // Weighted higher while mounted, since dismounted already has the Phantom Phoenix at range.
        protected override int MagicPreferenceChance => IsMounted ? 45 : 25;
        protected override Color MagicTelegraphFlashColor => new Color(150, 60, 210); // matches its glowing eyes

        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 24;
        protected override int TeleportDustTypeId => DustID.PurpleTorch;
        protected override Color TeleportDustTint => new Color(120, 40, 160);

        // ── Despawn on party wipe ────────────────────────────────────────────────
        // Tracking/timing/messaging is PuppetNPC's shared PartyWipeDespawnHandler — only the flavor line
        // and the two dust bursts (kill vs. despawn) are Dread-Wraith-specific here.
        protected override string DespawnFlavorText => LangUtils.GetTextValue("NPCs.DreadWraith.DespawnHandler");
        protected override Color DespawnFlavorColor => new Color(150, 60, 210); // matches the glowing violet eyes

        protected override int DespawnDustType => DustID.Wraith;
        protected override Color DespawnDustColor => new Color(15, 15, 22); // near-black, per spec ("black wraith dusts")

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            // Populate NPC.oldPos every frame — vanilla leaves trailing off by default, and the puppet's
            // afterimage draw reads straight out of oldPos. Mode 1 (rather than 0) avoids touching
            // localAI[3], which the every-other-frame mode uses as its own counter.
            NPCID.Sets.TrailingMode[Type] = 1;
        }

        /// <summary>Mounted, this thing is roughly two tiles tall, so the readout needs extra clearance.</summary>
        internal override float DebugLabelRise => 32f;

        /// <summary>Ticks between ember patches while laying the burning wake. 7 leaves a continuous-looking
        /// line at charge speed without spawning a projectile every frame.</summary>
        private const int EmberTrailInterval = 7;
        private int _emberTrailTimer;

        public override void PostAI()
        {
            base.PostAI();

            if (MountAI == null)
            {
                return;
            }

            // ── Flame charge: the one-shot burst as it plants ─────────────────────
            if (MountAI.SpecialWindupStarted)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.35f }, NPC.Center);
                EmitFlameBurst();
            }

            // Keep the ground alight behind the goat for the whole special pass. Server-authoritative:
            // these are real damaging projectiles, not decoration.
            if (MountAI.IsCharging && MountAI.IsSpecialCharge)
            {
                _emberTrailTimer--;

                if (_emberTrailTimer <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    _emberTrailTimer = EmberTrailInterval;

                    float groundY = Projectiles.Enemy.Weapons.PuppetGroundDustWave.FindGroundY(NPC.Center.X, NPC.Bottom.Y);

                    Projectile.NewProjectile(
                        NPC.GetSource_FromThis(),
                        new Vector2(NPC.Center.X, groundY - 2f),
                        Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.Weapons.DreadWraithEmberTrail>(),
                        MagicDamage,
                        0f,
                        Main.myPlayer);
                }
            }
            else
            {
                _emberTrailTimer = 0;
            }

            EmitGlaiveGlow();

            bool charging = MountAI.IsCharging;

            if (!charging)
            {
                return;
            }

            // Afterimages for the whole assembly. Re-armed every charge tick (the field self-decrements),
            // and because the goat renders inside DrawPlayer it streaks along with the rider for free.
            AfterimageTicks = 3;

            if (Main.dedServ)
            {
                return;
            }

            // Red glow streaming off the goat's skull. The head leads the charge, so the dust sits ahead of
            // NPC.Center on the facing side and drifts backward to read as speed.
            Vector2 goatHead = NPC.Center + new Vector2(NPC.direction * GoatHeadOffsetX, GoatHeadOffsetY);

            for (int i = 0; i < 3; i++)
            {
                Dust ember = Dust.NewDustPerfect(
                    goatHead + Main.rand.NextVector2Circular(7f, 6f),
                    DustID.RedTorch,
                    new Vector2(-NPC.direction * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(-1.2f, 0.4f)),
                    100,
                    default,
                    Main.rand.NextFloat(1.1f, 1.8f));
                ember.noGravity = true;
            }

            Lighting.AddLight(goatHead, 0.6f, 0.08f, 0.05f);
        }

        /// <summary>Offset from NPC.Center to the goat's skull, for the charge embers.
        /// Derived rather than guessed: NPC.Bottom sits on the ground and the hitbox is 42 tall, so
        /// NPC.Center is only ~21px up — around the goat's LEGS. The skull rides near the top of the
        /// mount, roughly 34px above the ground, hence ~13px ABOVE centre. Negative = up.</summary>
        private const float GoatHeadOffsetY = -14f;
        private const float GoatHeadOffsetX = 30f;

        /// <summary>Single radial gout of flame as the goat plants for its flame charge. Fires once, from
        /// the wind-up pulse — the 90 planted ticks that follow are the actual telegraph.</summary>
        /// <summary>The Rotted Fork's red bloom, reproduced from vanilla projectile 153: DustID
        /// CrimtaneWeapons at alpha 140, noGravity, with fadeIn 1.25 so each mote scales UP before fading —
        /// that bloom is what reads as a glow rather than sparks — and velocity damped to a quarter so it
        /// clings to the blade instead of streaking off. Spread along the cutting edge rather than a single
        /// point, since this is a swung glaive and not a straight holdout.</summary>
        private void EmitGlaiveGlow()
        {
            if (Main.dedServ)
            {
                return;
            }

            bool glaiveInHand = ActiveComboIsSpear || IsInSpearPhase;

            if (!glaiveInHand)
            {
                return;
            }

            // Same reach formula TryMeleeHit actually swings with (ComboReachBase * 0.7 * ReachMult) —
            // this used to pass the raw ComboReachBase, landing the glow noticeably short of (or past)
            // the real blade tip.
            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(ComboReachBase * 0.7f * ActiveMeleeComboReachMult);

            // Only the forward part of the shaft is blade, so keep the glow off the haft.
            for (int i = 0; i < 2; i++)
            {
                float alongBlade = Main.rand.NextFloat(0.55f, 1f);
                Vector2 bladePoint = Vector2.Lerp(hand, tip, alongBlade);

                Dust glow = Dust.NewDustPerfect(
                    bladePoint + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.CrimtaneWeapons,
                    Vector2.Zero,
                    140,
                    default,
                    1f);
                glow.noGravity = true;
                glow.fadeIn = 1.25f;
                glow.velocity *= 0.25f;
            }

            Lighting.AddLight(Vector2.Lerp(hand, tip, 0.8f), 0.45f, 0.05f, 0.08f);
        }

        private void EmitFlameBurst()
        {
            if (Main.dedServ)
            {
                return;
            }

            const int BurstDustCount = 90;

            for (int i = 0; i < BurstDustCount; i++)
            {
                // Even angular sweep with a jittered speed, so it reads as one expanding ring rather
                // than a random cloud.
                float angle = MathHelper.TwoPi * i / BurstDustCount;
                float speed = Main.rand.NextFloat(3.5f, 7.5f);

                Dust flame = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(0f, GoatHeadOffsetY),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
                    angle.ToRotationVector2() * speed,
                    60,
                    default,
                    Main.rand.NextFloat(1.4f, 2.3f));
                flame.noGravity = true;
            }

            Lighting.AddLight(NPC.Center, 1.6f, 0.6f, 0.12f);
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 2400;
            NPC.defense = 16;
            NPC.damage = 0; // No contact damage — all damage delivered via weapon hitbox / skull projectiles
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 15000f;
            NPC.boss = true;
            NPC.npcSlots = 4f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 28f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 160;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Plague; // black/purple lingering cloud fits the "dread" theme
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.SkeletronMask));
            npcLoot.Add(ItemDropRule.Common(ItemID.WillsBreastplate));
            npcLoot.Add(ItemDropRule.Common(ItemID.WillsLeggings));
            npcLoot.Add(ItemDropRule.Common(ItemID.MysteriousCape));
            npcLoot.Add(ItemDropRule.Common(ItemID.WallOfFleshGoatMountItem)); // Goat Skull — the mount it rode
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 300, 500));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<DreadWraith>());
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

        // Dissolve into shadowflame on death rather than bleed — this is a wraith, not flesh. Base
        // PuppetNPC.HitEffect spawns these via the shared dust-burst helper; count uses the base's
        // default of 150.
        protected override int OnKillDustType => DustID.Shadowflame;

        protected override void DoMeleeAttack()
        {
            // Legacy non-combo swing path — kept as a fallback; the flail's real damage is delivered
            // through DoComboMeleeHit below, which the Flail archetype's combo table normally selects.
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Spear combos resolve as a normal reach-based melee hit — no chain, no projectile.
            if (ActiveComboIsSpear)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.2f, PitchVariance = 0.15f }, NPC.Center);
                TryMeleeHit(reach: SpearRange * 0.8f);
                return;
            }

            // One head on the chain at a time. Without this a combo can start its next extension while the
            // previous ball is still out, which reads as the mace firing twice with no telegraph between.
            // The combo cooldowns are set longer than a full spin+lash+retract cycle; this is the failsafe.
            if (HasActiveMaceBall())
            {
                return;
            }

            Vector2 origin = PuppetHandPosition;
            int damage = (int)(MeleeDamage * step.DamageMult);

            // Signature attack: the head orbits on ~5 tiles of chain as a readable telegraph, then lashes
            // straight out to wherever the player is standing at the moment of discharge, capped at 20
            // tiles. Reach is resolved once, on discharge, so sidestepping after it fires beats it.
            Projectile projectile = Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(),
                origin,
                Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.DreadWraithMaceBall>(),
                damage,
                4f,
                Main.myPlayer,
                NPC.whoAmI,
                SpinThenLash,
                MaxLashReach);

            // Must outlive spin telegraph + lash out + retract, or the head vanishes mid-lash and the
            // chain visually snaps. DreadWraithMaceBall.Lifetime already covers this; the combo step can
            // only ever extend it.
            projectile.timeLeft = Math.Max(projectile.timeLeft, step.AttackTicks + 40);

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, PitchVariance = 0.2f }, NPC.Center);

            // Phase two escalation: a dismounted slam also cracks the ground open along a marching line of
            // fire toward the player, punishing anyone who just backs off out of chain range.
            bool isSlam = step.Motion == ComboMotion.VerticalChop || step.Motion == ComboMotion.OverheadArc;

            if (!IsMounted && isSlam)
            {
                EruptGroundToward(Main.player[NPC.target]);
            }
        }

        /// <summary>True while this wraith already has a mace head out on the chain.</summary>
        private bool HasActiveMaceBall()
        {
            int ballType = ModContent.ProjectileType<Projectiles.Enemy.Weapons.DreadWraithMaceBall>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];

                if (projectile.active && projectile.type == ballType && (int)projectile.ai[0] == NPC.whoAmI)
                {
                    return true;
                }
            }

            return false;
        }

        // ── Melee combos ──────────────────────────────────────────────────────────
        // Deliberately ALL single-step. The shared Flail archetype table has 2- and 3-step combos, which
        // fired the mace repeatedly inside one combo with no telegraph between extensions. Here every
        // swing is its own attack: retract fully, spin to telegraph, then lash. Cooldowns comfortably
        // exceed the ball's full 42+9+18 tick cycle so the chain is always empty before the next wind-up.
        private static MeleeComboStep MaceStep(ComboMotion motion, int telegraphTicks, int attackTicks, float damageMult)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = 0,
                DamageMult = damageMult,
                ReachMult = 1.2f,
                ForwardPushMult = 0f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };

        // ── Spear (The Rotted Fork), mounted only ─────────────────────────────────
        // A cavalry lance is a different weapon to a flail: it wants reach and thrusts, not arcs. The
        // spear combos live in the SAME pool as the mace ones (the pool is lazy-built once, so it can't
        // be swapped at runtime) and are gated to mounted in CanSelectMeleeCombo below. Which weapon is
        // drawn / how the hit resolves keys off the active combo's name.
        private const string SpearComboPrefix = "Rotted";

        // Latched once when a combo is CHOSEN (OnMeleeComboStarted below) rather than re-derived from
        // ActiveMeleeComboName on every read. The name goes stale between combos and isn't guaranteed to
        // be assigned before SetDisplayWeapon(MeleeWeaponItemType, ...) runs at combo start, which let the
        // drawn weapon (mace) disagree with the running combo (spear) for a frame.
        private bool _activeComboUsesSpear;

        private bool ActiveComboIsSpear => _activeComboUsesSpear;

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo); // rebuilds the cached FrontHandWeapon — see its doc comment
            _activeComboUsesSpear = combo.Name != null && combo.Name.StartsWith(SpearComboPrefix);
        }

        // The spear is also the forward-pierce weapon via PuppetNPC's own spear phase — that's the
        // "classic poke", separate from the swing combos. Disabled on foot: dismounted is the mace phase.
        // Also disabled while a mace head is out: the ball lives ~70 ticks after its combo ends, so
        // without this the wraith could start a thrust while the chain was still mid-lash and both
        // weapons would be swinging at once. Returning -1 makes the FSM skip the spear entirely.
        protected override int SpearWeaponItemType => IsMounted && !HasActiveMaceBall() ? ItemID.TheRottedFork : -1;
        protected override int SpearDamage => 30;
        protected override float SpearRange => 235f;
        protected override int SpearTelegraphTicks => 40;
        protected override int SpearAttackTicks => 14;
        protected override int SpearRecoveryTicks => 32;
        protected override int SpearCooldownAfterUse => 150;
        protected override float SpearPushSpeedMult => 0.5f; // leans into the thrust without outrunning the goat
        protected override Color SpearTelegraphFlashColor => new Color(150, 220, 120);

        // ── Weapon draw ───────────────────────────────────────────────────────────
        // Custom glaive art (138x144): blade tip at UPPER-LEFT, hilt butt at LOWER-RIGHT, so the shaft
        // runs along the top-left -> bottom-right diagonal.
        protected override string SpearDrawTexturePath => "tsorcRevamp/Projectiles/Enemy/Weapons/DreadWraithGlaive";

        // Swings use the normal melee draw branch (see DrawWeaponAsSpear below), which previously had no
        // texture override hook and fell through to the small vanilla item icon. Point it at the same
        // glaive art as the thrust — but only while a spear-named combo is actually active, so a mace
        // combo (icon hidden anyway, see HideHeldMeleeSprite) never picks this up.
        protected override string MeleeDrawTexturePath =>
            ActiveComboIsSpear ? "tsorcRevamp/Projectiles/Enemy/Weapons/DreadWraithGlaive" : null;

        // CRITICAL: spear-style drawing is for the THRUST ONLY. The spear path slides the grip up and down
        // the shaft, which is right for a poke but completely wrong for a swing — it was the reason the
        // swing arcs looked stunted. Swings go through the normal melee draw below instead.
        protected override bool DrawWeaponAsSpear => IsInSpearPhase;

        // Thrust grip: hand stays near the MIDDLE of the shaft, sliding a little as the poke extends
        // and retracts. Values are normalised sprite coords along the tip->hilt diagonal.
        protected override Vector2 SpearHeadNorm => new Vector2(0.36f, 0.36f);
        protected override Vector2 SpearBaseNorm => new Vector2(0.70f, 0.70f);

        // Swing grip: back 20% of the shaft, i.e. 0.8 of the way from tip toward the hilt.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.80f, 0.80f);
        protected override float MeleeWeaponDrawScale => 0.9f;
        protected override float ComboReachBase => 104f;   // long glaive reach
        protected override float MeleeBladeWidth => 30f;

        // Sprite lies along the ANTI-diagonal (tip up-left) rather than the usual up-right, which is a
        // quarter turn from the normal convention. NEEDS IN-GAME CALIBRATION: if the glaive points the
        // wrong way, these two offsets are the knobs — adjust in PiOver4 steps.
        protected override float MeleeWeaponRotationOffset => MathHelper.PiOver4 * 3f;
        protected override float SpearDrawRotationOffset => MathHelper.PiOver4 * 3f;

        // GetWeaponWorldDirection() (PuppetNPC) — used for melee hit detection AND any cosmetic effect
        // anchored to the blade tip, e.g. EmitGlaiveGlow below — assumes 45° (the standard broadsword
        // diagonal) unless told otherwise. Same quarter-turn correction as the two offsets above,
        // 45+90=135, so the "logical" blade tip matches the anti-diagonal sprite instead of pointing
        // wherever a standard sword's tip would be. NEEDS IN-GAME CALIBRATION alongside them — this is
        // an inferred value, not a measured one.
        protected override float MeleeNaturalRestAngleDeg => 135f;

        // ── Swing quality ─────────────────────────────────────────────────────────
        // Copied from StuddedLeatherWarrior, which is the reference for graceful arcs. UseCompositeArmSwing
        // is the important one: it drives a continuously-rotated arm instead of the legacy 4-frame pose
        // table, which is what makes a swing read as a full sweep rather than a ~45 degree jerk.
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;   // wind to the opposite end, then strike
        protected override bool UseSwingEasing => true;              // accelerate through the arc, settle out
        protected override bool UseAimAdaptiveArc => true;           // arc centres on the player, not a fixed angle
        protected override bool MeleeWeaponIsSingleBladed => true;   // glaive has one cutting edge
        // Was true, applying the generic green slash arc to every swing including MACE combos, which
        // never carry a glaive. EmitGlaiveGlow above already reproduces vanilla's red Rotted Fork bloom
        // and fires on both the thrust and glaive swings (gated on ActiveComboIsSpear / IsInSpearPhase);
        // the mace has its own fire-dust trail (DreadWraithMaceBall). No generic arc needed on top.
        protected override bool HasSlashVFX => false;

        private bool IsInSpearPhase =>
            Phase == AttackPhase.SpearTelegraph
            || Phase == AttackPhase.SpearAttack
            || Phase == AttackPhase.SpearRecovery;

        protected override void DoSpearAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.15f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: SpearRange * 0.85f); // reach matches the trigger range so the poke connects
        }

        /// <summary>Spear combos are cavalry moves — mounted only. On foot the wraith falls back to the
        /// mace pool, which keeps phase two feeling like a different fight.</summary>
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            bool isSpearCombo = combo.Name != null && combo.Name.StartsWith(SpearComboPrefix);

            if (isSpearCombo && !IsMounted)
            {
                return false;
            }

            // Strict one-weapon-at-a-time. The mace ball outlives its own combo by design (spin, lash,
            // retract), so ANY new melee attack has to wait for the chain to be empty — otherwise the
            // wraith visibly attacks with the glaive and the mace simultaneously.
            if (HasActiveMaceBall())
            {
                return false;
            }

            // Likewise never start a swing on top of an in-progress thrust.
            if (IsInSpearPhase)
            {
                return false;
            }

            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        private static MeleeComboStep SpearStep(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0f,
            SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = 1.35f,       // spear outreaches the mace swing
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = ease,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };

        private static readonly MeleeCombo[] DreadWraithMaceCombos = new[]
        {
            // ── Spear: mounted-only cavalry work ──────────────────────────────────
            new MeleeCombo
            {
                Name = "Rotted Underhand",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(150, 220, 120),
                CooldownAfterUse = 70,
                RecoveryTicks = 60,
                MoveBrake = 0.03f,   // barely slows: the goat keeps carrying it forward
                // AttackTicks 75 (up from 14) to fit the Trapezoidal shape's own tick budget — a 10-tick
                // ease-in, a fast cruise, a deliberate 30-tick ease-out, and a 15-tick landed hold are
                // absolute costs regardless of the swing's total sweep. NEEDS IN-GAME CALIBRATION.
                Steps = new[] { SpearStep(ComboMotion.UnderhandArc, 24, 75, damageMult: 0.95f, ease: SwingEaseStyle.Trapezoidal) },
            },
            new MeleeCombo
            {
                Name = "Rotted Overhead",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(180, 240, 140),
                CooldownAfterUse = 70,
                RecoveryTicks = 60,
                MoveBrake = 0.03f,
                // See Rotted Underhand above re: the AttackTicks jump.
                Steps = new[] { SpearStep(ComboMotion.OverheadArc, 24, 77, damageMult: 0.95f, ease: SwingEaseStyle.Trapezoidal) },
            },
            new MeleeCombo
            {
                Name = "Rotted Doublet",
                BaseWeight = 75,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.YellowGreen,
                CooldownAfterUse = 130,
                RecoveryTicks = 30,
                MoveBrake = 0.03f,
                // The two swings back to back, as one committed string.
                Steps = new[]
                {
                    SpearStep(ComboMotion.UnderhandArc, 26, 14, pauseAfter: 8, damageMult: 0.8f),
                    SpearStep(ComboMotion.OverheadArc, 0, 16, damageMult: 1.05f),
                },
            },
            new MeleeCombo
            {
                Name = "Rotted Lance",
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 190,
                RecoveryTicks = 60,
                HeavyCommit = true,
                // Couched charge: only selectable from OUTSIDE swing range, so the wraith closes with the
                // fork levelled and thrusts on arrival rather than flailing at nothing. RangedStartOnly is
                // what makes it the gap-closer instead of competing with the in-range swings.
                RangedStartOnly = true,
                MoveBrake = 0f,
                // AttackTicks 70 (up from 20) — same Trapezoidal budget as the arc swings above,
                // just a much smaller total sweep (cocked at 90°, levels to 45°), so the plateau is
                // shorter. NEEDS IN-GAME CALIBRATION.
                Steps = new[] { SpearStep(ComboMotion.JoustDash, 36, 70, damageMult: 1.3f, forwardPushMult: 1.1f, ease: SwingEaseStyle.Trapezoidal) },
            },

            // ── Mace: available in both phases ────────────────────────────────────
            new MeleeCombo
            {
                Name = "Chain Lash",
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 140, 60),
                CooldownAfterUse = 130,
                RecoveryTicks = 60,
                MoveBrake = 0.05f,
                Steps = new[] { MaceStep(ComboMotion.OverheadArc, 24, 16, 1f) },
            },
            new MeleeCombo
            {
                Name = "Wide Lash",
                BaseWeight = 85,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 150,
                RecoveryTicks = 60,
                MoveBrake = 0.05f,
                Steps = new[] { MaceStep(ComboMotion.HorizontalSweep, 26, 18, 1.1f) },
            },
            new MeleeCombo
            {
                Name = "Reaping Lash",
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 175,
                RecoveryTicks = 60,
                HeavyCommit = true,
                MoveBrake = 0.04f,
                Steps = new[] { MaceStep(ComboMotion.VerticalChop, 30, 20, 1.25f) },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => DreadWraithMaceCombos;

        /// <summary>ai[1] mode selector on EnemyFlailProjectileBase: 2 = spin telegraph then lash.</summary>
        private const float SpinThenLash = 2f;

        /// <summary>20 tiles. Passed as ai[2] — the CAP, not the actual reach; the lash only extends as far
        /// as the player actually is, clamped into the 5-20 tile band.</summary>
        private const float MaxLashReach = 320f;

        /// <summary>Marching line of ground eruptions running from the wraith toward the player. Reuses
        /// PuppetFirefallPillar (already a warn-then-erupt ground hazard); the staggered ai[0] warning
        /// delays are what make it read as a crack travelling outward rather than five pillars at once.</summary>
        private void EruptGroundToward(Player target)
        {
            if (!target.active || target.dead)
            {
                return;
            }

            const int EruptionCount = 6;
            const float EruptionSpacing = 90f;
            const int EruptionStagger = 9;
            // +20 across the board (40 first pillar -> 85 last), per user request — more time to read
            // and react to the telegraph. Stagger between pillars is unchanged.
            const int EruptionBaseDelay = 40;

            int marchDirection = Math.Sign(target.Center.X - NPC.Center.X);

            if (marchDirection == 0)
            {
                marchDirection = NPC.direction;
            }

            int pillarType = ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetFirefallPillar>();

            for (int i = 0; i < EruptionCount; i++)
            {
                float eruptionX = NPC.Center.X + marchDirection * EruptionSpacing * (i + 1);
                float groundY = Projectiles.Enemy.Weapons.PuppetGroundDustWave.FindGroundY(eruptionX, NPC.Bottom.Y);

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    new Vector2(eruptionX, groundY - 2f),
                    Vector2.Zero,
                    pillarType,
                    MeleeDamage,
                    3f,
                    Main.myPlayer,
                    EruptionBaseDelay + i * EruptionStagger,
                    0f,
                    Projectiles.Enemy.Weapons.PuppetFirefallPillar.DreadWraithFireVisualStyle);
                    // Explicit opt-in: Owl Father retains the shared projectile's legacy visuals.
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.55f, Pitch = -0.25f }, NPC.Center);
        }

        protected override void OnMountDestroyed()
        {
            SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);

            if (Main.dedServ)
            {
                return;
            }

            // The goat bursts. 200 dusts is a deliberate one-off spike — this is the phase break, and it
            // needs to be unmistakable against the wraith's own shadowflame death effect later.
            for (int i = 0; i < 200; i++)
            {
                Dust blood = Dust.NewDustPerfect(
                    NPC.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), Main.rand.NextFloat(-30f, 4f)),
                    DustID.Blood,
                    Main.rand.NextVector2Circular(7f, 7f),
                    0,
                    default,
                    Main.rand.NextFloat(1.1f, 2.4f));
                blood.noGravity = false;
            }
        }

        /// <summary>Running count of arrows in the CURRENT volley, so every third one becomes a phoenix.
        /// Reset when a volley starts rather than tracked globally, so a 3-shot volley always ends on a
        /// phoenix instead of inheriting the previous volley's phase.</summary>
        private int _phoenixShotIndex;

        protected override void OnRangedBurstStarted(bool secondary)
        {
            base.OnRangedBurstStarted(secondary);

            if (!secondary)
            {
                _phoenixShotIndex = 0;
            }
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                return;
            }

            _phoenixShotIndex++;

            // Vanilla's Phantom Phoenix fires flaming arrows and swaps in the phoenix on every third
            // shot. Both are the REAL vanilla projectiles, so all their dust, trails and lighting come
            // along for free — they only need flipping to hostile (the same approach SpiritOfKhaios uses
            // for Sky Fracture). Rebuilding them by hand would just be a worse copy.
            bool isPhoenixShot = _phoenixShotIndex % 3 == 0;

            Vector2 muzzle = PuppetHandPosition;
            Vector2 aimPoint = target.Center + target.velocity * 10f;
            Vector2 aim = (aimPoint - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));

            float spread = MathHelper.ToRadians(Main.rand.NextFloat(-2.5f, 2.5f));
            float speed = isPhoenixShot ? 9f : 12f;   // the phoenix is slower and more readable

            int projectileType = isPhoenixShot
                ? ProjectileID.DD2PhoenixBow
                : ProjectileID.DD2PhoenixBowShot;

            int damage = isPhoenixShot ? (int)(RangedDamage * 1.5f) : RangedDamage;

            if (isPhoenixShot)
            {
                SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 0.8f }, NPC.Center);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.6f, PitchVariance = 0.15f }, NPC.Center);
            }

            int index = Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                aim.RotatedBy(spread) * speed,
                projectileType,
                damage,
                3f,
                Main.myPlayer);

            if (index < 0 || index >= Main.maxProjectiles)
            {
                return;
            }

            Projectile shot = Main.projectile[index];
            shot.friendly = false;
            shot.hostile = true;
            shot.netUpdate = true;
        }

        // ── Book of Skulls: six volley shapes ─────────────────────────────────────
        // Each cast is a short channel rather than a single shot. The counts and the gaps between them
        // both vary, so the player can't settle into one dodge rhythm. Totals run 3-6 skulls.
        // Index into both arrays together: Ticks[p][i] is when to fire, Counts[p][i] is how many.
        private static readonly int[][] SkullVolleyTicks = new int[][]
        {
            new int[] { 0, 18 },         // 2 then 1                     = 3
            new int[] { 0, 12 },         // 1 then 3                     = 4
            new int[] { 0, 14, 28 },     // 2, 1, then 3                 = 6
            new int[] { 0, 10, 20 },     // steady metronome             = 3
            new int[] { 0, 25 },         // 3, long pause, 2             = 5
            new int[] { 0, 8, 16, 24 },  // accelerating into a burst    = 6
        };

        private static readonly int[][] SkullVolleyCounts = new int[][]
        {
            new int[] { 2, 1 },
            new int[] { 1, 3 },
            new int[] { 2, 1, 3 },
            new int[] { 1, 1, 1 },
            new int[] { 3, 2 },
            new int[] { 1, 1, 1, 3 },
        };

        /// <summary>Ticks of follow-through after the last skull so the cast pose doesn't snap shut.</summary>
        private const int SkullVolleyTailTicks = 22;

        private int _skullPattern;
        private int _skullVolleyDuration;

        protected override void DoMagicAttack()
        {
            _skullPattern = Main.rand.Next(SkullVolleyTicks.Length);

            int[] ticks = SkullVolleyTicks[_skullPattern];
            _skullVolleyDuration = ticks[ticks.Length - 1] + SkullVolleyTailTicks;

            // Channel the cast — DoMagicTick fires the individual volleys as the timer runs down.
            _magicAttackTicksOverride = _skullVolleyDuration;

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, PitchVariance = 0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.Server)
            {
                NPC.netUpdate = true;
            }
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            int elapsed = _skullVolleyDuration - ticksRemaining;
            int[] ticks = SkullVolleyTicks[_skullPattern];

            for (int i = 0; i < ticks.Length; i++)
            {
                if (ticks[i] == elapsed)
                {
                    FireSkulls(SkullVolleyCounts[_skullPattern][i]);
                    return;
                }
            }
        }

        private void FireSkulls(int count)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);

            Vector2 origin = PuppetWeaponTipPosition(30f);
            Vector2 toTarget = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f));

            // Fan the volley around the aim line. A single skull goes straight; larger groups spread so
            // they arrive as a wall rather than stacking into one hitbox.
            float spreadDegrees = 9f * (count - 1);

            for (int i = 0; i < count; i++)
            {
                float offsetDegrees = 0f;

                if (count > 1)
                {
                    offsetDegrees = MathHelper.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, i / (float)(count - 1));
                }

                // Each skull gets its own launch speed, aim offset and spawn jitter. Without this every
                // skull in a volley solves the same homing path and they stack into one clump.
                float speedMultiplier = Main.rand.NextFloat(0.82f, 1.18f);
                float aimOffset = MathHelper.ToRadians(Main.rand.NextFloat(-13f, 13f));
                Vector2 spawnJitter = Main.rand.NextVector2Circular(10f, 10f);

                Vector2 velocity = toTarget.RotatedBy(MathHelper.ToRadians(offsetDegrees)) * 5.4f * speedMultiplier;

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    origin + spawnJitter,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.DreadWraithSkull>(),
                    MagicDamage,
                    3f,
                    Main.myPlayer,
                    speedMultiplier,
                    aimOffset);
            }
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_skullPattern);
            writer.Write((short)_skullVolleyDuration);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _skullPattern = reader.ReadByte();
            _skullVolleyDuration = reader.ReadInt16();
        }
    }
}
