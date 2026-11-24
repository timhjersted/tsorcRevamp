using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.ElementalSet;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // The Abyss Elemental: the Elemental's body in Twilight-dyed armor, empty-handed and shrouded in shadow smoke.
    // Its touch saps (Weak), blinds (Obstructed) and calls the horde (Battle). Three moves, picked by range on the
    // server: Abyss Rake at point-blank, Shadow Pounce inside 128px, and Ambush Blink (behind the player, straight
    // into a Pounce) from 160-480px. The ordinary nav blink stays for when it is stuck. Clients pose from synced state.
    class CorruptedElemental : PuppetNPC
    {
        enum Move : byte
        {
            None,
            Pounce,
            Rake,
            Ambush,
        }

        enum Stage : byte
        {
            None,
            Tell,
            Action,
            Recovery,
        }

        // Arm facing angles (0 = forward, +PiOver2 = down, negative = up). The Elemental head's face sits low:
        // a front arm above -0.1 covers it, so every forward reach stops there.
        const float ArmRestAngle = 1.45f;
        const float ArmBackLowAngle = 2.25f;
        const float ArmForwardAngle = -0.1f;

        // Legacy FighterAI movement: 2.8 px/t, accel 0.08; below 20% life a fixed 3.6 px/t and double accel.
        const float BaseTopSpeed = 2.8f;
        const float BaseAcceleration = 0.08f;
        const float EnrageFraction = 0.2f;
        const float EnrageTopSpeed = 3.6f;

        // Shadow Pounce: the old LeapAtPlayer(6, 5, 2, 128) hop, now with a tell, a landing recovery and a cooldown.
        const float PounceRange = 128f;
        const float PounceSpeedX = 6f;
        const float PounceSpeedY = 5f;
        const int PounceTellTicks = 20;
        const int PounceArmorTick = 14;
        const int PounceArmSwingTicks = 6;
        const int PounceFailsafeTicks = 90;
        const int PounceRecoveryTicks = 24;
        const int PounceCooldownTicks = 150;
        const int PounceCooldownJitter = 30;

        // Abyss Rake: underhand claw, low-behind to forward, live on strike ticks 5-18 (13t, rollable).
        const float RakeEdgeGap = 48f;
        const float RakeVerticalRange = 40f;
        const int RakeTellTicks = 24;
        const int RakeRaiseTicks = 16;
        const int RakeArmorTick = 18;
        const int RakeLiveStart = 5;
        const int RakeLiveEnd = 18;
        const int RakeRecoveryTicks = 30;
        const int RakeCooldownTicks = 240;
        const float RakeStepSpeed = 2f;
        const int RakeStepTicks = 10;
        const float StrikeBoxReach = 40f;
        const float StrikeBoxHeight = 40f;
        // The contact hit's Expert value (50 authored x 2), scaled the same way the contact is.
        const int RakeExpertDamage = 100;
        static readonly WeightedSwing RakeCurve = new WeightedSwing(5, 20, 7f);

        // Ambush Blink: 40t departure (body fades, smoke gathers where it will appear), relocate, 18t reform,
        // then straight into a Pounce's 20t tell. 78t from the first destination smoke to the launch.
        const float AmbushMinRange = 160f;
        const float AmbushMaxRange = 480f;
        const float AmbushBehindMin = 72f;
        const float AmbushBehindMax = 112f;
        const float AmbushPlayerClearance = 64f;
        const int AmbushDepartTicks = 40;
        const int AmbushReformTicks = 18;
        const int AmbushMissRecoveryTicks = 24;
        const int AmbushCooldownTicks = 600;
        const int AmbushCooldownJitter = 120;
        const int AmbushInitialCooldown = 240;
        const int AmbushRetryTicks = 90;
        const float AmbushFadedOpacity = 0.25f;

        const int NeutralGapTicks = 30;

        // ── Synced state ────────────────────────────────────────────────────────
        Move move;
        Stage stage;
        int stageTimer;
        int stageDuration;
        int targetIndex;
        int lockedFacing = 1;
        int sequence;
        bool landed;
        Vector2 ambushDestination; // its new feet

        // ── Server-only scheduling ──────────────────────────────────────────────
        int pounceCooldown;
        int rakeCooldown;
        int ambushCooldown = AmbushInitialCooldown;
        int selectionDelay = NeutralGapTicks;

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Abyss Elemental";
        protected override int HeadArmorItemType => ModContent.ItemType<ElementalHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<ElementalBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<ElementalGreaves>();
        // Twilight Dye: a dark violet starfield anchored in world space, so the body reads as a window into the void.
        protected override int HeadArmorDyeItemType => ItemID.TwilightDye;
        protected override int BodyArmorDyeItemType => ItemID.TwilightDye;
        protected override int LegsArmorDyeItemType => ItemID.TwilightDye;
        protected override Color PuppetSkinColor => new Color(0x34, 0x53, 0x6F); // the old sheet's steel-blue skin
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => false;
        protected override float RunSpeedMult => 1f;

        bool Enraged => NPC.life < NPC.lifeMax * EnrageFraction;

        protected override float TopSpeed
        {
            get
            {
                if (Enraged)
                {
                    return EnrageTopSpeed;
                }

                return BaseTopSpeed;
            }
        }

        protected override float Acceleration
        {
            get
            {
                if (Enraged)
                {
                    return BaseAcceleration * 2f;
                }

                return BaseAcceleration;
            }
        }

        protected override float? FreeFrontArmFacingAngle => ResolveArmAngle(false);
        protected override float? FreeBackArmFacingAngle => ResolveArmAngle(true);

        // Ambush Blink: fades to 25% over the departure, holds through the relocation, reforms over 18t.
        internal override float BodyOpacity
        {
            get
            {
                if (move != Move.Ambush)
                {
                    return 1f;
                }

                if (stage == Stage.Tell)
                {
                    float departProgress = MathHelper.Clamp(stageTimer / (float)AmbushDepartTicks, 0f, 1f);
                    return MathHelper.Lerp(1f, AmbushFadedOpacity, departProgress);
                }

                float reformProgress = MathHelper.Clamp(stageTimer / (float)AmbushReformTicks, 0f, 1f);
                return MathHelper.Lerp(AmbushFadedOpacity, 1f, reformProgress);
            }
        }

        protected override void DoMeleeAttack()
        {
            // Rake and Pounce run on this class's own clock.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        #region Strike box (read by AbyssRakeStrike)
        public int StrikeSequence => sequence;
        public bool StrikeRunning => move == Move.Rake && stage == Stage.Action;

        public bool StrikeCollides(Rectangle target)
        {
            bool live = StrikeRunning && stageTimer >= RakeLiveStart && stageTimer < RakeLiveEnd;
            if (!live)
            {
                return false;
            }

            int width = (int)(NPC.width * 0.5f + StrikeBoxReach);
            int left = (int)NPC.Center.X;
            if (lockedFacing < 0)
            {
                left -= width;
            }

            int top = (int)(NPC.Center.Y - StrikeBoxHeight * 0.6f);
            Rectangle strikeBox = new Rectangle(left, top, width, (int)StrikeBoxHeight);
            return strikeBox.Intersects(target);
        }

        // Battle 60s, Obstructed 2s, Weak 60s: the legacy touch, unchanged. Called on the hit player's machine.
        public static void ApplyAbyssDebuffs(Player target)
        {
            target.AddBuff(BuffID.Battle, 60 * 60, false);
            target.AddBuff(BuffID.Obstructed, 2 * 60, false);
            target.AddBuff(BuffID.Weak, 60 * 60, false);
        }
        #endregion

        #region Defaults
        public override void SetStaticDefaults()
        {
            // The old sheet stays as the bestiary/banner art; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.1f; // overridden by the PoiseProfiles entry
            NPC.lavaImmune = true;
            NPC.timeLeft = 750;
            NPC.damage = 50;
            NPC.defense = 55;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 2000; // SHM HP comes from the EnemyBalance registry
            NPC.value = 6000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CorruptedElementalBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 45;
            // The legacy FighterAI `canTeleport: true` meant an unlimited nav blink; SF4 puppets request it directly.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }
        #endregion

        #region AI
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Harmless while slumped in a stagger and while blinking (faded body, no surprise contact on arrival).
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0)
            {
                return false;
            }

            return move != Move.Ambush;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyAbyssDebuffs(target);
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (move == Move.None)
            {
                base.RunMovementAI(1f);
            }
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (authority)
            {
                RunServerSchedule(globalNPC);
            }

            if (move != Move.None)
            {
                // Keeps the base parked in its Custom phase; this class's own stage clock ends the move.
                PhaseTimer = 2;
                NPC.target = targetIndex;
            }

            globalNPC.CanTeleport = move == Move.None;
            base.AI();
            DebugAttackLabel = move + "/" + stage;

            if (move != Move.None)
            {
                ApplyPoiseFlags(globalNPC);
                globalNPC.SuppressPreAttackJump = true;
            }

            if (!Main.dedServ)
            {
                DecorateAbyssElemental();
            }
        }

        // Each tell is interruptible until its last few ticks; strikes and the airborne pounce are armored;
        // recoveries are bare punish windows.
        void ApplyPoiseFlags(tsorcRevampGlobalNPC globalNPC)
        {
            bool telegraphing = false;
            bool committed = false;

            if (stage == Stage.Tell)
            {
                int armorTick = AmbushDepartTicks;
                if (move == Move.Pounce)
                {
                    armorTick = PounceArmorTick;
                }
                else if (move == Move.Rake)
                {
                    armorTick = RakeArmorTick;
                }

                telegraphing = stageTimer < armorTick;
                committed = !telegraphing;
            }
            else if (stage == Stage.Action)
            {
                committed = move != Move.Ambush;
                telegraphing = move == Move.Ambush;
            }

            globalNPC.AttackTelegraphing = telegraphing;
            globalNPC.AttackCommitted = committed;
        }

        // Server: cooldowns, target upkeep and neutral selection by range (Rake, then Pounce, then Ambush).
        void RunServerSchedule(tsorcRevampGlobalNPC globalNPC)
        {
            pounceCooldown = Math.Max(0, pounceCooldown - 1);
            rakeCooldown = Math.Max(0, rakeCooldown - 1);
            ambushCooldown = Math.Max(0, ambushCooldown - 1);
            selectionDelay = Math.Max(0, selectionDelay - 1);

            if (move != Move.None)
            {
                bool targetGone = targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active || Main.player[targetIndex].dead;
                if (targetGone)
                {
                    CancelMove();
                }

                return;
            }

            if (globalNPC.StaggerTimer > 0 || selectionDelay > 0)
            {
                return;
            }

            if (globalNPC.InEvasion || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }

            if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            bool grounded = NPC.velocity.Y == 0f;
            if (!grounded)
            {
                return;
            }

            float edgeGap = Math.Abs(target.Center.X - NPC.Center.X) - (NPC.width + target.width) * 0.5f;
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            if (rakeCooldown == 0 && edgeGap <= RakeEdgeGap && verticalGap <= RakeVerticalRange)
            {
                BeginMove(Move.Rake, target, RakeTellTicks);
                return;
            }

            bool inPounceBox = Math.Abs(target.Center.X - NPC.Center.X) < PounceRange && verticalGap < PounceRange;
            bool canSee = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (pounceCooldown == 0 && inPounceBox && canSee)
            {
                BeginMove(Move.Pounce, target, PounceTellTicks);
                return;
            }

            float distance = NPC.Distance(target.Center);
            bool ambushBand = distance >= AmbushMinRange && distance <= AmbushMaxRange;
            if (ambushCooldown == 0 && ambushBand)
            {
                if (TryFindAmbushSpot(target))
                {
                    BeginMove(Move.Ambush, target, AmbushDepartTicks);
                }
                else
                {
                    ambushCooldown = AmbushRetryTicks;
                }
            }
        }

        /// <summary>Server: supported, body-clear ground 72-112px behind the way the target faces, within 48px of its
        /// feet level, with line of sight to it and at least 64px from every player. Stored in ambushDestination.</summary>
        bool TryFindAmbushSpot(Player target)
        {
            int behind = -target.direction;
            if (behind == 0)
            {
                behind = 1;
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                float offset = MathHelper.Lerp(AmbushBehindMin, AmbushBehindMax, attempt / 7f);
                float spotX = target.Center.X + behind * offset;
                if (!PuppetGroundDustWave.TryFindGroundY(spotX, target.Bottom.Y, out float groundY))
                {
                    continue;
                }

                Vector2 spot = new Vector2(spotX, groundY);
                bool bodyClear = !Collision.SolidCollision(new Vector2(spotX - NPC.width * 0.5f, groundY - NPC.height - 2f), NPC.width, NPC.height);
                bool nearLevel = Math.Abs(groundY - target.Bottom.Y) <= 48f;
                bool sightOk = Collision.CanHitLine(spot - new Vector2(0f, NPC.height * 0.5f), 1, 1, target.Center, 1, 1);

                bool playersClear = true;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && player.Distance(spot) < AmbushPlayerClearance)
                    {
                        playersClear = false;
                        break;
                    }
                }

                if (bodyClear && nearLevel && sightOk && playersClear)
                {
                    ambushDestination = spot;
                    return true;
                }
            }

            return false;
        }

        void BeginMove(Move chosen, Player target, int tellTicks)
        {
            move = chosen;
            stage = Stage.Tell;
            stageTimer = 0;
            stageDuration = tellTicks;
            targetIndex = target.whoAmI;
            landed = false;

            lockedFacing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                lockedFacing = -1;
            }

            NPC.direction = lockedFacing;
            NPC.spriteDirection = lockedFacing;

            if (sequence == 0)
            {
                sequence = Main.rand.Next(1, 1 << 20);
            }

            sequence++;
            // Projectile.ai stores floats; keep the strike's owner token exactly representable.
            if (sequence >= 1 << 23)
            {
                sequence = 1;
            }

            StartCustomAttack(2);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (move == Move.None)
            {
                return;
            }

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[targetIndex];
            NPC.direction = lockedFacing;
            NPC.spriteDirection = lockedFacing;

            if (stage == Stage.Tell)
            {
                NPC.velocity.X *= 0.8f;

                if (move == Move.Ambush)
                {
                    RunAmbushDeparture(authority);
                    if (stage != Stage.Tell)
                    {
                        return;
                    }
                }
                else if (authority && stageTimer >= stageDuration)
                {
                    BeginAction();
                    return;
                }
            }
            else if (stage == Stage.Action)
            {
                RunAction(target, authority);
                if (move == Move.None || stage != Stage.Action)
                {
                    return;
                }
            }
            else if (stage == Stage.Recovery)
            {
                NPC.velocity.X *= 0.8f;
                if (authority && stageTimer >= stageDuration)
                {
                    EndMove();
                    return;
                }
            }

            stageTimer++;
        }

        void BeginAction()
        {
            stage = Stage.Action;
            stageTimer = 0;

            if (move == Move.Pounce)
            {
                pounceCooldown = PounceCooldownTicks + Main.rand.Next(PounceCooldownJitter + 1);
                stageDuration = PounceFailsafeTicks;
                NPC.velocity = new Vector2(PounceSpeedX * lockedFacing, -PounceSpeedY);
            }
            else if (move == Move.Rake)
            {
                rakeCooldown = RakeCooldownTicks;
                stageDuration = RakeCurve.TotalTicks;
                int damage = EnemyDamage.Projectile((int)(RakeExpertDamage * tsorcRevampWorld.SubtleSHMScale));
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbyssRakeStrike>(), damage, 3f, Main.myPlayer, NPC.whoAmI, sequence);
            }

            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        void RunAction(Player target, bool authority)
        {
            if (move == Move.Pounce)
            {
                // A wall bump ends the hop early; otherwise hold the hop's horizontal speed until the feet land.
                if (authority && stageTimer > 2 && NPC.collideX)
                {
                    BeginRecovery(PounceRecoveryTicks);
                    return;
                }

                bool touchedDown = stageTimer > 2 && NPC.collideY && NPC.velocity.Y == 0f;
                if (touchedDown && !landed)
                {
                    landed = true;
                    if (!Main.dedServ)
                    {
                        EmitLandingSkid();
                    }
                }

                if (!landed)
                {
                    NPC.velocity.X = PounceSpeedX * lockedFacing;
                }

                if (authority && (landed || stageTimer >= stageDuration))
                {
                    BeginRecovery(PounceRecoveryTicks);
                }

                return;
            }

            if (move == Move.Rake)
            {
                // Lean into the rake: 2 px/t for 10 ticks (~20px), then plant.
                if (stageTimer < RakeStepTicks)
                {
                    NPC.velocity.X = lockedFacing * RakeStepSpeed;
                }
                else
                {
                    NPC.velocity.X *= 0.75f;
                }

                if (authority && stageTimer >= stageDuration)
                {
                    BeginRecovery(RakeRecoveryTicks);
                }

                return;
            }

            if (move == Move.Ambush)
            {
                NPC.velocity.X = 0f;
                if (!authority || stageTimer < AmbushReformTicks)
                {
                    return;
                }

                // Reformed: chain straight into a Pounce if the target is still inside its box, else a short stand.
                bool targetValid = target.active && !target.dead;
                float horizontalGap = Math.Abs(target.Center.X - NPC.Center.X);
                float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
                bool inPounceBox = horizontalGap < PounceRange && verticalGap < PounceRange;
                if (targetValid && inPounceBox)
                {
                    BeginMove(Move.Pounce, target, PounceTellTicks);
                }
                else
                {
                    BeginRecovery(AmbushMissRecoveryTicks);
                }
            }
        }

        // Departure: the body fades, smoke gathers at the destination for the whole 40t, then it relocates.
        void RunAmbushDeparture(bool authority)
        {
            if (!Main.dedServ)
            {
                if (stageTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.55f, Pitch = -0.4f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.45f, Pitch = -0.5f }, ambushDestination);
                }

                EmitAmbushTelegraph();
            }

            if (stageTimer < AmbushDepartTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                // ~18 motes at each end; this machine has not moved the body yet, so NPC.Center is still the departure.
                Vector2 arrivalCenter = ambushDestination - new Vector2(0f, NPC.height * 0.5f);
                for (int i = 0; i < 18; i++)
                {
                    Dust leave = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 20f), DustID.Wraith, Main.rand.NextVector2Circular(2.5f, 2.5f), 40, default, 1.4f);
                    leave.noGravity = true;
                    Dust arrive = Dust.NewDustPerfect(arrivalCenter + Main.rand.NextVector2Circular(10f, 20f), DustID.Shadowflame, Main.rand.NextVector2Circular(2.5f, 2.5f), 40, default, 1.3f);
                    arrive.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.3f }, ambushDestination);
            }

            if (authority)
            {
                NPC.Bottom = ambushDestination;
                NPC.velocity = Vector2.Zero;
                stage = Stage.Action;
                stageTimer = 0;
                stageDuration = AmbushReformTicks;
                ambushCooldown = AmbushCooldownTicks + Main.rand.Next(AmbushCooldownJitter + 1);
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }
        }

        void BeginRecovery(int ticks)
        {
            stage = Stage.Recovery;
            stageTimer = 0;
            stageDuration = ticks;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        void EndMove()
        {
            move = Move.None;
            stage = Stage.None;
            stageTimer = 0;
            selectionDelay = NeutralGapTicks;
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        void CancelMove()
        {
            move = Move.None;
            stage = Stage.None;
            stageTimer = 0;
            landed = false;
            selectionDelay = 60;
            NPC.netUpdate = true;
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            // A blink broken mid-departure never relocates; it pays the ambush cooldown anyway.
            if (move == Move.Ambush && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ambushCooldown = AmbushCooldownTicks;
            }

            CancelMove();
        }
        #endregion

        #region Poses
        float? ResolveArmAngle(bool backArm)
        {
            if (move == Move.Pounce)
            {
                return ResolvePounceAngle(backArm);
            }

            if (move == Move.Rake && !backArm)
            {
                return ResolveRakeAngle();
            }

            if (move == Move.Ambush)
            {
                // Arms drawn in to the body while it comes apart and reforms.
                return 0.9f;
            }

            return null;
        }

        // Tell: both arms sweep back-low (back arm a little further). Launch: forward to the face limit over 6t,
        // held through the air. Recovery: back to rest over 16t.
        float ResolvePounceAngle(bool backArm)
        {
            float cockedAngle = ArmBackLowAngle;
            if (backArm)
            {
                cockedAngle += 0.2f;
            }

            if (stage == Stage.Tell)
            {
                float cock = MathHelper.Clamp(stageTimer / 12f, 0f, 1f);
                return MathHelper.SmoothStep(ArmRestAngle, cockedAngle, cock);
            }

            if (stage == Stage.Action)
            {
                float swing = MathHelper.Clamp(stageTimer / (float)PounceArmSwingTicks, 0f, 1f);
                return MathHelper.Lerp(cockedAngle, ArmForwardAngle, swing);
            }

            float settle = MathHelper.Clamp(stageTimer / 16f, 0f, 1f);
            return MathHelper.SmoothStep(ArmForwardAngle, ArmRestAngle, settle);
        }

        // Tell: the front claw eases to low-behind over 16t and holds. Strike: weighted curve to the face limit.
        float ResolveRakeAngle()
        {
            if (stage == Stage.Tell)
            {
                float raise = MathHelper.Clamp(stageTimer / (float)RakeRaiseTicks, 0f, 1f);
                return MathHelper.SmoothStep(ArmRestAngle, ArmBackLowAngle, raise);
            }

            if (stage == Stage.Action)
            {
                return RakeCurve.Apply(ArmBackLowAngle, ArmForwardAngle, Math.Min(stageTimer, RakeCurve.TotalTicks));
            }

            float settle = MathHelper.Clamp(stageTimer / 16f, 0f, 1f);
            return MathHelper.SmoothStep(ArmForwardAngle, ArmRestAngle, settle);
        }

        // Vanilla's full-stretch empty-hand solve, evaluated without the draw-only Player (same as LurkerPuppet).
        Vector2 HandPosition(float facingAngle)
        {
            float rotation = (facingAngle - MathHelper.PiOver2) * NPC.direction + MathHelper.PiOver2;
            Vector2 wrist = rotation.ToRotationVector2() * 10f;
            wrist += new Vector2(-4f * NPC.direction, -2f);
            wrist += new Vector2(0f, 3f * NPC.direction).RotatedBy(rotation);
            return NPC.Center + new Vector2(0f, NPC.gfxOffY) + wrist;
        }
        #endregion

        #region Visuals
        void DecorateAbyssElemental()
        {
            // The shroud: a Wraith wisp every other tick rising off the body, a Shadowflame mote every 20t.
            if (Main.GameUpdateCount % 2 == 0)
            {
                Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -0.6f);
                Dust wisp = Dust.NewDustPerfect(position, DustID.Wraith, velocity, 60, default, Main.rand.NextFloat(1.1f, 1.5f));
                wisp.noGravity = true;
            }

            if (Main.GameUpdateCount % 20 == 0)
            {
                Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust mote = Dust.NewDustPerfect(position, DustID.Shadowflame, new Vector2(0f, -0.8f), 60, default, 1f);
                mote.noGravity = true;
            }

            if (move == Move.Pounce)
            {
                DecoratePounce();
            }
            else if (move == Move.Rake)
            {
                DecorateRake();
            }
        }

        void DecoratePounce()
        {
            if (stage == Stage.Tell)
            {
                if (stageTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }

                // Smoke pooling at the feet, 2 per tick, and shadowflame drawn into both hands.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 feet = NPC.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), -2f);
                    Dust pool = Dust.NewDustPerfect(feet, DustID.Wraith, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -0.3f), 50, default, 1.3f);
                    pool.noGravity = true;
                }

                if (stageTimer % 5 == 0)
                {
                    Vector2 hand = HandPosition(ResolvePounceAngle(false));
                    Vector2 offset = Main.rand.NextVector2CircularEdge(18f, 18f);
                    Dust gather = Dust.NewDustPerfect(hand + offset, DustID.Shadowflame, -offset * 0.1f, 40, default, 1.1f);
                    gather.noGravity = true;
                }

                return;
            }

            if (stage == Stage.Action && !landed)
            {
                if (stageTimer == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.6f }, NPC.Center);
                }

                // Trail: 2 motes per tick spread along this tick's 6px step (3px gaps).
                for (int i = 0; i < 2; i++)
                {
                    Vector2 along = -NPC.velocity * (i * 0.5f);
                    Vector2 position = NPC.Center + along + Main.rand.NextVector2Circular(6f, 12f);
                    Dust trail = Dust.NewDustPerfect(position, DustID.Wraith, NPC.velocity * 0.1f, 50, default, 1.4f);
                    trail.noGravity = true;
                }
            }
        }

        void DecorateRake()
        {
            float clawAngle = ResolveRakeAngle();
            Vector2 palm = HandPosition(clawAngle);

            if (stage == Stage.Tell)
            {
                if (stageTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.5f, Pitch = -0.1f }, NPC.Center);
                }

                Vector2 offset = Main.rand.NextVector2CircularEdge(14f, 14f);
                Dust gather = Dust.NewDustPerfect(palm + offset, DustID.Wraith, -offset * 0.12f, 40, default, 1.2f);
                gather.noGravity = true;

                if (stageTimer % 2 == 0)
                {
                    Dust spark = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(3f, 3f), DustID.Shadowflame, Vector2.Zero, 40, default, 1.1f);
                    spark.noGravity = true;
                }

                return;
            }

            if (stage != Stage.Action)
            {
                return;
            }

            if (stageTimer == RakeLiveStart)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            }

            bool live = stageTimer >= RakeLiveStart && stageTimer < RakeLiveEnd;
            if (!live)
            {
                return;
            }

            // A short shadow arc trailing the claw while it is live.
            for (int i = 0; i < 2; i++)
            {
                Dust claw = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(4f, 4f), DustID.Shadowflame, new Vector2(lockedFacing * 1.5f, -0.5f), 30, default, 1.3f);
                claw.noGravity = true;
            }
        }

        void EmitAmbushTelegraph()
        {
            // Departure: smoke boiling off the fading body.
            Vector2 departure = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
            Dust leaving = Dust.NewDustPerfect(departure, DustID.Wraith, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -1f), 40, default, 1.4f);
            leaving.noGravity = true;

            // Destination: a dark column the height of its body that thickens as the departure runs out.
            float progress = stageTimer / (float)AmbushDepartTicks;
            int columnMotes = 1;
            if (progress > 0.5f)
            {
                columnMotes = 2;
            }

            for (int i = 0; i < columnMotes; i++)
            {
                Vector2 position = ambushDestination + new Vector2(Main.rand.NextFloat(-10f, 10f), -Main.rand.NextFloat(NPC.height));
                Dust column = Dust.NewDustPerfect(position, DustID.Wraith, new Vector2(0f, -0.7f), 40, default, 1.3f);
                column.noGravity = true;
            }

            if (stageTimer % 4 == 0)
            {
                Vector2 floor = ambushDestination + new Vector2(Main.rand.NextFloat(-12f, 12f), -2f);
                Dust ember = Dust.NewDustPerfect(floor, DustID.Shadowflame, new Vector2(0f, -1.2f), 40, default, 1.1f);
                ember.noGravity = true;
            }
        }

        void EmitLandingSkid()
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 feet = NPC.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -2f);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(0.5f, 1.5f));
                Dust skid = Dust.NewDustPerfect(feet, DustID.Wraith, velocity, 50, default, 1.4f);
                skid.noGravity = true;
            }

            UsefulFunctions.ScreenShake(NPC.Center, 1.5f, 8, distanceFalloff: 350f);
        }

        // Replaces the puppet's generic death burst; runs on every client (OnKill does not run on MP clients).
        // Hits: 6 wisps. Death: ~70 motes thrown 2-7 px/t (every third Shadowflame) plus a 20-mote ring at 6 px/t.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (NPC.life > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                    Dust puff = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(8f, 16f), DustID.Wraith, velocity, 50, default, 1.3f);
                    puff.noGravity = true;
                }

                return;
            }

            for (int i = 0; i < 70; i++)
            {
                int dustType = DustID.Wraith;
                if (i % 3 == 0)
                {
                    dustType = DustID.Shadowflame;
                }

                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.5f, NPC.height * 0.5f);
                Dust mote = Dust.NewDustPerfect(position, dustType, velocity, 50, default, Main.rand.NextFloat(1.2f, 1.8f));
                mote.noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                Vector2 direction = angle.ToRotationVector2();
                Dust ring = Dust.NewDustPerfect(NPC.Center + direction * 10f, DustID.Wraith, direction * 6f, 40, default, 1.6f);
                ring.noGravity = true;
            }
        }
        #endregion

        #region Netcode + loot
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)move);
            writer.Write((byte)stage);
            writer.Write((short)stageTimer);
            writer.Write((short)stageDuration);
            writer.Write((byte)targetIndex);
            writer.Write((sbyte)lockedFacing);
            writer.Write(sequence);
            writer.Write(landed);
            writer.WriteVector2(ambushDestination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            move = (Move)reader.ReadByte();
            stage = (Stage)reader.ReadByte();
            stageTimer = reader.ReadInt16();
            stageDuration = reader.ReadInt16();
            targetIndex = reader.ReadByte();
            lockedFacing = reader.ReadSByte();
            sequence = reader.ReadInt32();
            landed = reader.ReadBoolean();
            ambushDestination = reader.ReadVector2();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WhiteTitanite>(), 1, 1, 3));
        }
        #endregion
    }
}
