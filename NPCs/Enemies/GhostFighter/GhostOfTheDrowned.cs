using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    class GhostOfTheDrowned : ModNPC, IStaggerable
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }

        public int spearStabDamage = 12;
        public int bubbleDamage = 10;

        public override void SetDefaults()
        {
            NPC.timeLeft = 60;
            NPC.knockBackResist = 0f; //Ghost
            NPC.aiStyle = -1;
            NPC.damage = 22;
            NPC.defense = 12;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 150;
            if (Main.hardMode)
            {
                NPC.lifeMax = 450;
                NPC.defense = 28;
                NPC.damage = 28;
                NPC.value = 1250;
                spearStabDamage = 24;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1300;
                NPC.defense = 48;
                NPC.damage = 38;
                NPC.value = 4000;
                spearStabDamage = 35;
            }
            NPC.value = 700;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;

            //Banner = NPC.type;
            //BannerItem = ModContent.ItemType<Banners.LothricSpearKnightBanner>();

            NPC.buffImmune[BuffID.Confused] = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.CanPassThroughWalls = true;
            globalNPC.HasGhostAfterimages = true;
            // Step 6 ghost levers: drift (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            // Movement: shared fighter mover + SF4 A* nav (Phase-3 migration off the bespoke jump-ladder).
            // (Ground nav; its wall-phase escape via CanPassThroughWalls still works independently.)
            globalNPC.HealthScaledSpeedBase = 2f;
            globalNPC.HealthScaledSpeedMultiplier = -0.75f; // full-health topSpeed 1.25 — was 0.75, sluggish under SF4 nav; speeds toward 2.0 when wounded (compensates SF4 overhead)
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 10f;           // preserves the old -9.5f overhead-jump reach
            globalNPC.RemembersLastKnownPos = true; // pursuer: investigate last-seen spot before drifting off
            // Poise / stagger: opt in. A stagger cancels an attack via IStaggerable.OnStagger below.
            globalNPC.PoiseMax = 15f;
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            SoundEngine.PlaySound(SoundID.Drown, target.Center);
            target.AddBuff(BuffID.Darkness, 8 * 60);
            target.AddBuff(BuffID.BrokenArmor, 8 * 60);
            target.AddBuff(BuffID.Chilled, 8 * 60);
            target.AddBuff(ModContent.BuffType<Gilled>(), 16 * 60);
        }
        #endregion

        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Timer_Shielding_Slot = 1;
        private const int AI_Timer_Attacking_Slot = 2;
        private const int AI_Timer_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Shielding = 1;
        private const int State_Thrusting = 2;
        private const int State_Shooting = 3;
        private const int State_Leaping = 4;

        // How far above/below the ghost the flat spear-thrust can actually connect. The Spearhead
        // projectile stays pinned to the ghost's own center height (see Spearhead.cs) and only moves
        // horizontally, so a target more than ~1.5 tiles off the ghost's level is unreachable no matter
        // how long it stands there thrusting at them.
        private const float ThrustVerticalReach = 28f;
        // Vertical gaps up to this are closed with a small hop instead of a doomed thrust attempt.
        // Anything taller than this breaks stance back to Pursuing so the SF4 nav can properly path up.
        private const float HopVerticalRange = 56f;
        // Cooldown (ticks) between leap attacks, tracked in NPC.localAI[1].
        private const float LeapCooldownTicks = 300f;
        // Cooldown (ticks) between hops, tracked in NPC.localAI[0], to stop hop-spam on stairs.
        private const float HopCooldownTicks = 40f;



        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }
        public float AI_Timer_Shielding
        {
            get => NPC.ai[AI_Timer_Shielding_Slot];
            set => NPC.ai[AI_Timer_Shielding_Slot] = value;
        }

        public float AI_Timer_Attacking
        {
            get => NPC.ai[AI_Timer_Attacking_Slot];
            set => NPC.ai[AI_Timer_Attacking_Slot] = value;
        }

        public float AI_Timer
        {
            get => NPC.ai[AI_Timer_Slot];
            set => NPC.ai[AI_Timer_Slot] = value;
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Staggered (poise broken): frozen. GlobalNPC.ApplyStaggerMovement drives the knockback slide in
            // PostAI; OnStagger already reset the state machine to Pursuing. Freeze here so timers don't advance.
            if (globalNPC.StaggerTimer > 0)
            {
                globalNPC.AttackTelegraphing = false;
                globalNPC.AttackCommitted = false;
                return;
            }

            // Leap/hop cooldowns tick down regardless of state.
            if (NPC.localAI[1] > 0f) NPC.localAI[1] -= 1f;
            if (NPC.localAI[0] > 0f) NPC.localAI[0] -= 1f;

            bool grounded = NPC.velocity.Y == 0; // proxy for the old standing-on-solid-tile scan (gates attacks/transitions)
            bool los = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            // Same-level melee/shield threat: roughly level + visible. Only stand and BLOCK when the player is a
            // same-level threat; on another level it pursues via SF4 (or fires bubbles at range — the Shooting
            // transition below is intentionally NOT gated on this).
            bool playerMeleeLevel = los && Math.Abs(player.Center.Y - NPC.Center.Y) <= 5 * 16;
            // Block stance: shield is up during Shielding/Thrusting/Shooting → a FRONT hit takes reduced poise
            // (GlobalNPC.ShieldGuarding) + the doubled front damage reduction in ModifyHitBy. Backstabs unaffected.
            globalNPC.ShieldGuarding = AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Shooting;

            // Restore SetDefaults knockBackResist each tick (0 = ghost is knockback-immune); poise owns the stagger
            // (a poise break still launches it via ApplyStaggerImpulse) and hyper-armor (AttackCommitted).
            if (globalNPC.BaseKnockBackResist >= 0f)
            {
                NPC.knockBackResist = globalNPC.BaseKnockBackResist;
            }

            // Poise labels (RedKnight / BlackKnight pattern). Thrust combo: WINDUP (poise can break) AI_Timer<34,
            // COMMITTED (hyper-armor) 34-94. Bubble barrage (Shooting): WINDUP (charge-up) <90, COMMITTED (firing)
            // 90-150. Shielding is a block (not labelled) so it can still be poise-broken; OnStagger cancels it.
            globalNPC.AttackTelegraphing = (AI_State == State_Thrusting && AI_Timer < 34)
                                           || (AI_State == State_Shooting && AI_Timer < 65);
            // Bubble barrage: blue flash fires at AI_Timer==65, 25 ticks before the first bubble at 90 —
            // hyper-armored from the flash through the barrage. Recovery (>150) is vulnerable.
            globalNPC.AttackCommitted = (AI_State == State_Thrusting && AI_Timer >= 34 && AI_Timer <= 94)
                                        || (AI_State == State_Shooting && AI_Timer >= 65 && AI_Timer <= 150)
                                        || AI_State == State_Leaping; // committed to the arc once airborne

            #region AI_State Independent


            if (AI_Timer_Attacking < 420)
            {
                AI_Timer_Attacking++;
            }

            if (AI_Timer_Attacking >= 390 && AI_Timer_Attacking <= 400)
            {
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

            }

            if (AI_Timer_Attacking >= 400 && AI_Timer_Attacking < 442)
            {
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {

                // Movement is now the shared fighter mover (SF4 A* nav via the SetDefaults levers). This REPLACES
                // the deleted target/turn + accel/brake + -5..-9.5 jump-ladder + overhead-jump + platform-drop
                // blocks (Phase-3 migration). Only runs in Pursuing; the Shielding/Thrusting/Shooting states stand
                // still (zero velocity) and handle their own facing. The overhead jumps were pure navigation, so
                // SF4's jump-up pathing replaces them (no attack to preserve). Wall-phase escape is independent.
                tsorcRevampAIs.FighterAI(NPC, 2f, 0.08f, 0.1f, canPounce: false, canDodgeroll: false);

                // Shield only builds/engages when the player is a same-level melee threat — otherwise SF4 pursues
                // (or it fires bubbles at range below), instead of standing and blocking across levels.
                if (playerMeleeLevel && NPC.Distance(player.Center) < 250)
                {
                    AI_Timer_Shielding++;
                }
                else if (!playerMeleeLevel && AI_Timer_Shielding < 300)
                {
                    AI_Timer_Shielding = 0;
                }

                if (playerMeleeLevel && NPC.Distance(player.Center) < 95 && grounded)
                {
                    AI_Timer_Shielding = 300;
                    AI_State = State_Shielding;
                }

                if (playerMeleeLevel && AI_Timer_Shielding >= 300 && grounded)
                {
                    AI_State = State_Shielding;
                }

                // Leap attack: a ranged gap-closer that also solves height the thrust combo can't — it can
                // launch up (or down) onto a ledge the player is standing on, up to ~20 tiles out. Its own
                // 300-tick cooldown (localAI[1]) is independent of the bubble barrage's AI_Timer_Attacking gate.
                float leapDistance = NPC.Distance(player.Center);
                if (AI_State == State_Pursuing && NPC.localAI[1] <= 0f && grounded && los
                    && leapDistance > 130f && leapDistance <= 20f * 16f)
                {
                    StartLeap(player);
                }

                if (AI_State == State_Pursuing && AI_Timer_Attacking >= 420 && NPC.Distance(player.Center) < 20f * 16 && grounded && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Shooting;
                }
            }


            // SHIELDING
            if (AI_State == State_Shielding)
            {
                NPC.TargetClosest(true);
                AI_Timer_Shielding++;

                if (NPC.velocity.Y == 0)
                {
                    if (AI_Timer_Shielding > 300 && AI_Timer_Shielding <= 310 && Math.Abs(NPC.velocity.X) > 1f)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; }
                        else { NPC.velocity.X += 0.15f; }
                    }

                    if (AI_Timer_Shielding > 310)
                    {
                        NPC.velocity.X = 0;
                    }

                    if (AI_Timer_Shielding > 500)
                    {
                        AI_State = State_Pursuing;
                        AI_Timer_Shielding = 0;
                    }
                }

                float shieldVerticalGap = player.Center.Y - NPC.Center.Y; // negative = player above
                bool shieldHorizontalClose = Math.Abs(NPC.Center.X - player.Center.X) <= 6.5f * 16;

                if (AI_Timer_Shielding > 310 && shieldHorizontalClose && Math.Abs(shieldVerticalGap) <= ThrustVerticalReach && grounded && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Thrusting;
                }
                // Standing on a step just too high/low for the flat thrust to reach: hop to close it instead of
                // uselessly poking at empty air (the "one tile higher and it keeps stabbing" bug). Too tall a
                // gap for a hop breaks stance back to Pursuing so the SF4 nav can path up/around properly.
                else if (AI_Timer_Shielding > 310 && shieldHorizontalClose && Math.Abs(shieldVerticalGap) <= HopVerticalRange && grounded && NPC.velocity.Y == 0)
                {
                    if (NPC.localAI[0] <= 0f)
                    {
                        NPC.velocity.Y = -6f;
                        NPC.velocity.X = NPC.direction * 2f;
                        NPC.localAI[0] = HopCooldownTicks;
                        NPC.netUpdate = true;
                    }
                }
                else if (AI_Timer_Shielding > 310 && shieldHorizontalClose && Math.Abs(shieldVerticalGap) > HopVerticalRange)
                {
                    AI_State = State_Pursuing;
                    AI_Timer_Shielding = 0;
                }

                if (AI_Timer_Shielding > 310 && AI_Timer_Attacking >= 420 && NPC.Distance(player.Center) < 20f * 16 && grounded && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Shooting;
                }
            }


            //THRUSTING (While shielding)
            if (AI_State == State_Thrusting)
            {
                AI_Timer++;
                AI_Timer_Shielding = 400;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;


                #region Projectiles & Sounds
                if (NPC.direction == 1)
                {
                    if (AI_Timer == 34)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = 5;
                        }
                    }

                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = 5;
                        }
                    }

                    if (AI_Timer == 77)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = 5;
                        }
                    }
                }
                else
                {
                    if (AI_Timer == 34)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }

                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }

                    if (AI_Timer == 76)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3, 2)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }
                }

                #endregion


                if (AI_Timer > 94)
                {
                    AI_Timer = 0;
                    AI_State = State_Shielding;
                }
            }

            int bubbleDelay = 8;

            //SHOOTING BUBBLES
            if (AI_State == State_Shooting)
            {
                AI_Timer++;
                NPC.TargetClosest(true);
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;

                if (AI_Timer_Shielding > 310) //If it was already shielding
                {
                    AI_Timer_Shielding = 400;
                }

                if (AI_Timer == 65 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Blue telegraph flash 25 ticks before the first bubble at 90 (hyper-armored from here).
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Blue));
                }

                int dustQuantity = (int)AI_Timer / 6;
                for (int i = 0; i < dustQuantity; i++)
                {
                    if (Main.rand.NextBool(10) && AI_Timer < 150)
                    {
                        if (NPC.direction == 1)
                        {
                            int dust = Dust.NewDust(new Vector2(NPC.Center.X + 40, NPC.Center.Y - 10), 10, 10, DustID.UltraBrightTorch, 0, 0, 100, default(Color), 0.8f);
                            Main.dust[dust].noGravity = true;
                        }
                        else
                        {
                            int dust = Dust.NewDust(new Vector2(NPC.Center.X - 54, NPC.Center.Y - 10), 10, 10, DustID.UltraBrightTorch, 0, 0, 100, default(Color), 0.8f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }

                if (AI_Timer % bubbleDelay == 0 && AI_Timer >= 90) //Every 8 frames
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item85 with { PitchVariance = .3f }, NPC.Center);
                    Vector2 shootPos;
                    if (NPC.direction == 1) shootPos = new Vector2(NPC.Center.X + 40, NPC.Center.Y - 10);
                    else shootPos = new Vector2(NPC.Center.X - 54, NPC.Center.Y - 10);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Short spray up close (the old drifting bubble is fine at melee range); a long-range
                        // bolt aimed at the player once they're far enough that the spray would just drift and
                        // die. Each bolt rolls its own drift strength/side so the volley doesn't look laser-gridded.
                        if (NPC.Distance(player.Center) > 160f)
                        {
                            Vector2 boltVelocity = (player.Center - shootPos).SafeNormalize(Vector2.UnitX * NPC.direction) * 5.5f;
                            float boltDrift = Main.rand.NextFloat(-2.5f, 2.5f);
                            Projectile bolt = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), shootPos, boltVelocity, ModContent.ProjectileType<Projectiles.Enemy.GhostBubbleBolt>(), spearStabDamage, 5, Main.myPlayer, boltDrift)];
                            bolt.friendly = false;
                            bolt.hostile = true;
                        }
                        else
                        {
                            Projectile bubble = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), shootPos, new Vector2(NPC.direction * Main.rand.NextFloat(6, 12), Main.rand.NextFloat(-2f, 2f)), ProjectileID.Bubble, spearStabDamage, 5, Main.myPlayer, NPC.whoAmI)];
                            bubble.friendly = false;
                            bubble.hostile = true;
                            bubble.tileCollide = false;
                        }
                    }
                }

                if (AI_Timer > 150)
                {
                    AI_Timer = 0;
                    AI_Timer_Attacking = 0;
                    if (AI_Timer_Shielding == 400) //If previously shielding, go back to shielding
                    {
                        AI_State = State_Shielding;
                    }
                    else
                    {
                        AI_State = State_Pursuing;
                    }
                }
            }

            // LEAPING (spear-poke leap attack — closes distance and height together)
            if (AI_State == State_Leaping)
            {
                AI_Timer++;
                NPC.TargetClosest(true);

                // Landed: either keep poking if the leap put it in real thrust range, or bail back to
                // Pursuing if the player moved / the arc undershot/overshot.
                if (AI_Timer > 3 && NPC.velocity.Y == 0f)
                {
                    NPC.velocity.X = 0;
                    bool landedInThrustRange = Math.Abs(NPC.Center.X - player.Center.X) <= 6.5f * 16
                        && Math.Abs(NPC.Center.Y - player.Center.Y) <= ThrustVerticalReach
                        && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);

                    if (landedInThrustRange)
                    {
                        AI_State = State_Thrusting;
                        AI_Timer = 0;
                        AI_Timer_Shielding = 400; // return to Shielding, not Pursuing, once the combo ends
                    }
                    else
                    {
                        AI_State = State_Pursuing;
                        AI_Timer = 0;
                        AI_Timer_Shielding = 0;
                    }
                }
                // Safety timeout in case it somehow never registers grounded (e.g. leaps into deep water).
                else if (AI_Timer > 200)
                {
                    AI_State = State_Pursuing;
                    AI_Timer = 0;
                    AI_Timer_Shielding = 0;
                }
            }
        }

        // Launches a physics-timed leap toward the player's current position: solves for the airtime needed
        // to close the vertical gap (boosting launch power if the player is well above), then picks the
        // horizontal speed that covers the gap in that time. Caps out around the 20-tile trigger range.
        private void StartLeap(Player targetPlayer)
        {
            AI_State = State_Leaping;
            AI_Timer = 0f;
            NPC.localAI[1] = LeapCooldownTicks;

            float gravity = NPC.gravity > 0f ? NPC.gravity : 0.3f;
            float dx = targetPlayer.Center.X - NPC.Center.X;
            float dy = targetPlayer.Center.Y - NPC.Center.Y; // negative = player above

            float vy0 = -MathHelper.Clamp(7f + Math.Max(0f, -dy) * 0.05f, 7f, 16f);
            float disc = vy0 * vy0 + 2f * gravity * dy;
            if (disc < 0f)
            {
                // Player is higher than this launch power can reach — boost just enough to get there.
                vy0 = -MathHelper.Clamp((float)Math.Sqrt(-2f * gravity * dy) + 0.5f, 7f, 18f);
                disc = Math.Max(0f, vy0 * vy0 + 2f * gravity * dy);
            }

            float airTime = MathHelper.Clamp((-vy0 + (float)Math.Sqrt(disc)) / gravity, 10f, 140f);
            float vx = MathHelper.Clamp(dx / airTime, -13f, 13f);
            if (Math.Abs(vx) < 2f)
            {
                vx = 2f * (dx >= 0 ? 1 : -1);
            }

            NPC.direction = vx >= 0 ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            NPC.velocity = new Vector2(vx, vy0);
            NPC.netUpdate = true; // sync the launch to MP clients

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f, PitchVariance = .2f }, NPC.Center);
        }

        // IStaggerable: a poise break cancels any in-progress attack and returns to neutral pursuit.
        public void OnStagger(NPC npc)
        {
            AI_State = State_Pursuing;
            AI_Timer = 0f;
            AI_Timer_Shielding = 0f;
            AI_Timer_Attacking = 60f; // delay the next bubble barrage windup
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Shooting)
            {
                if (NPC.ai[1] < 370)
                {
                    NPC.ai[1] += 30; //Used for Jump-slash
                }

                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 30;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 30;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
            }

            if (NPC.direction == 1) //if enemy facing right
            {
                if (player.position.X < NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }

            NPC.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[NPC.target];
            int direction = modifiers.HitDirection;

            Type hm = typeof(NPC.HitModifiers);
            PropertyInfo prop = hm.GetProperty("HitDirectionOverride");
            int? over = (int?)prop.GetValue(modifiers);

            if (over != null && over != 0)
            {
                direction = over.Value;
            }

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.Specialist.BlizzardBlasterShot>())
            {
                if (AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Shooting)
                {

                    if (NPC.direction == 1) //if npc facing right
                    {
                        if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }

                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.1f;

                            if (NPC.ai[1] < 380)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;

                            modifiers.Knockback *= 0.1f;
                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                }


                if (NPC.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (direction == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }
                else //if enemy facing left
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (direction == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }

                if (NPC.Distance(player.Center) > 220 && AI_State != State_Shielding)
                {
                    NPC.ai[2] += 100;
                }

                if (NPC.ai[1] < 400)
                {
                    NPC.ai[1] += 10;
                }
            }
        }


        #endregion

        /*public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }*/
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            // special areas
            int playerX = (int)(Main.LocalPlayer.Center.X / 16f);
            int playerY = (int)(Main.LocalPlayer.Center.Y / 16f);
            //playerX > 1737 && playerX < 1909 && playerY > 715 && playerY < 857

            //Wall IDs are the ID's given from TEdit in Catacombs of the drowned, as the name used by tedit and TML don't match.
            if (spawnInfo.Player.ZoneGraveyard && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 185 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 215
                 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 301 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 214 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 302)) chance = 2.5f;

            //Machine Temple (code in GlobalNPC.cs because I used clear all code to prevent other dungeon/hallow biome enemies spawning 
            //if (Main.hardMode && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 98 && playerY < 1430) chance = 2.5f;        

            if (spawnInfo.Water && Main.hardMode && spawnInfo.Player.ZoneNormalUnderground) chance = 0.15f;
            //Rest for Tim to decide

            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 15));
            npcLoot.Add(ItemDropRule.Common(ItemID.Trident, 10));

        }

        #region Drawing & Animation


        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection

            Texture2D texture = TextureAssets.Npc[NPC.type].Value; //Base texture, manually drawing so as to not have a ridiculously big canvas size in order to have a centered hitbox
            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 128, 74), Color.White * 0.75f, NPC.rotation, new Vector2(64, 50), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 128, 74), Color.White * 0.75f, NPC.rotation, new Vector2(64, 50), NPC.scale, effects, 0f);
            }
            return false; //Don't draw base sprite
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowWarrior_Shield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 10, 0, shieldFrame);
            if ((AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Shooting) && NPC.velocity.X == 0)
            {
                if (NPC.spriteDirection == 1)
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, Color.White * 0.2f, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, Color.White * 0.2f, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
            }
        }


        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Pursuing && NPC.velocity.X != 0) //Walking anim
            {

                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 72)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 84)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 96)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if (AI_State == State_Pursuing && (NPC.velocity.Y != 0))
            {
                NPC.frame.Y = 1 * frameHeight;
            }

            if (AI_State == State_Leaping)
            {
                // Spear held out at full extension for the whole arc — the "poke leading the jump" look.
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 14 * frameHeight;
            }

            if (AI_State == State_Shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;

                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }

            if (AI_State == State_Thrusting)
            {
                NPC.spriteDirection = NPC.direction;
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!


                if (AI_Timer < 10)
                {
                    NPC.frame.Y = 11 * frameHeight;
                    NPC.frameCounter = 0;
                }

                if (AI_Timer >= 10)
                {
                    NPC.frameCounter++;

                    if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else if (NPC.frameCounter < 30)
                    {
                        NPC.frame.Y = 13 * frameHeight;
                    }
                    else if (NPC.frameCounter < 40)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else if (NPC.frameCounter < 46)
                    {
                        NPC.frame.Y = 12 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }

            if (AI_State == State_Shooting)
            {
                NPC.spriteDirection = NPC.direction;
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!


                if (AI_Timer < 5)
                {
                    NPC.frame.Y = 10 * frameHeight;
                    NPC.frameCounter = 0;
                }

                if (AI_Timer >= 5)
                {
                    NPC.frameCounter++;

                    if (NPC.frameCounter < 150)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }
        }

        #endregion

    }
}
