using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class LothricSpearKnight : ModNPC, IStaggerable
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 20;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }
        public int lothricDamage = 25;
        public int lothricSmallDamage = 18;
        public int lothricBigDamage = 35;
        int lothricKnight;
        int lothricKnight2;

        public override void SetDefaults()
        {
            NPC.timeLeft = 60;
            NPC.npcSlots = 5;
            NPC.knockBackResist = 0.1f;
            NPC.aiStyle = -1;
            NPC.damage = 40;
            NPC.defense = 40;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 750;
            if (Main.hardMode)
            {
                NPC.lifeMax = 1200;
                NPC.defense = 60;
                NPC.value = 6000;
                lothricDamage = 30;
                lothricSmallDamage = 23;
                lothricBigDamage = 40;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 3000;
                NPC.defense = 80;
                NPC.damage = 85;
                NPC.value = 12000;
                NPC.knockBackResist = 0.0f;
                lothricDamage = 35;
                lothricSmallDamage = 28;
                lothricBigDamage = 45;
            }
            NPC.value = 3750;
            NPC.noGravity = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.LothricSpearKnightBanner>();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.HealthScaledSpeedBase = 2f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 9.5f;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.PoiseMax = 32f;
            // Evasion: a telegraphed hyper-armored RunningDash "greatshield charge" that flows into a thrust on
            // arrival. No hops/leaps (it has LungeThrust as its built-in gap-closer already).
            globalNPC.EvasiveRunningDash = true;
            // Reactive greatshield: pre-emptive + on-hit block chance. See ShieldProfile.
            ShieldProfile.LothricKnight(globalNPC);
        }

        // On-hit: roll a reactive block first (raise the greatshield to catch the combo); else evade.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (!tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), true))
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            bool melee = projectile.DamageType == DamageClass.Melee;
            if (!tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), melee))
                tsorcRevampAIs.EvasiveOnHit(NPC, melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            player.AddBuff(36, 3 * 60, false);
            player.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 15 * 60, false);
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
        private const int State_JumpThrust = 3;
        private const int State_LungeThrust = 4;

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

            if (globalNPC.StaggerTimer > 0)
            {
                globalNPC.AttackTelegraphing = false;
                globalNPC.AttackCommitted = false;
                return;
            }

            UsefulFunctions.DustRing(NPC.Center, 300, DustID.YellowTorch, 5, 2f);
            if (NPC.Distance(player.Center) < 300)
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2);
            if (Main.hardMode && NPC.Distance(player.Center) < 300)
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);

            bool grounded = NPC.velocity.Y == 0;
            bool los = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            bool playerMeleeLevel = los && Math.Abs(player.Center.Y - NPC.Center.Y) <= 4 * 16;

            #region State-independent: attack timer + eye dust

            if (AI_Timer_Attacking < 420)
                AI_Timer_Attacking++;

            if (AI_Timer_Attacking >= 390 && AI_Timer_Attacking <= 400)
            {
                Vector2 dustPos = NPC.direction == 1 ? new Vector2(NPC.position.X + 9, NPC.position.Y + 1) : new Vector2(NPC.position.X + 3, NPC.position.Y + 1);
                Dust dust2 = Main.dust[Dust.NewDust(dustPos, 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                dust2.noGravity = true;
                dust2.fadeIn = .3f;
                dust2.velocity += NPC.velocity;
            }

            if (AI_Timer_Attacking >= 400 && AI_Timer_Attacking < 442)
            {
                Vector2 dustPos = NPC.direction == 1 ? new Vector2(NPC.position.X + 9, NPC.position.Y + 1) : new Vector2(NPC.position.X + 3, NPC.position.Y + 1);
                Dust dust2 = Main.dust[Dust.NewDust(dustPos, 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                dust2.noGravity = true;
                dust2.fadeIn = .3f;
                dust2.velocity += NPC.velocity;
            }

            #endregion

            // poise labels
            globalNPC.AttackTelegraphing =
                (AI_State == State_Thrusting && AI_Timer < 34)
                || (AI_State == State_JumpThrust && AI_Timer < 82)
                || (AI_State == State_LungeThrust && AI_Timer < 30);
            globalNPC.AttackCommitted =
                (AI_State == State_Thrusting && AI_Timer >= 34 && AI_Timer <= 90)
                || (AI_State == State_JumpThrust && AI_Timer >= 82)
                || (AI_State == State_LungeThrust && AI_Timer >= 30 && AI_Timer < 56);
            globalNPC.ShieldGuarding = ((AI_State == State_Shielding || AI_State == State_Thrusting) && playerMeleeLevel)
                || globalNPC.ReactiveBlockTimer > 0;

            // PURSUING
            if (AI_State == State_Pursuing)
            {
                tsorcRevampAIs.FighterAI(NPC, 2f, 0.08f, 0.2f, canPounce: false, canDodgeroll: false);

                if (globalNPC.BaseKnockBackResist >= 0f)
                    NPC.knockBackResist = globalNPC.BaseKnockBackResist;

                // Pre-emptive block: chance to raise the greatshield when a threat (incoming shot / close player) is detected.
                tsorcRevampAIs.TryPreemptiveBlock(NPC, globalNPC);

                // Reactive block (pre-emptive / on-hit) → raise the greatshield. (Replaces the old far-range autonomous
                // shield-timer metronome, which accumulated near the player and turtled on a fixed cycle.)
                if (globalNPC.ReactiveBlockTimer > 0 && grounded)
                {
                    AI_Timer_Shielding = 311;
                    AI_State = State_Shielding;
                }

                // Close in → raise the greatshield and thrust (the melee attack approach, NOT a defensive metronome).
                if (NPC.Distance(player.Center) < 95 && grounded && los && playerMeleeLevel)
                {
                    AI_Timer_Shielding = 300;
                    AI_State = State_Shielding;
                }

                if (AI_Timer_Attacking == 420 && Math.Abs(NPC.Center.X - player.Center.X) > 6.5f * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 19f * 16 && grounded && NPC.velocity.Y == 0 && los)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == ModContent.NPCType<LothricKnight>())
                        {
                            if (NPC.Distance(Main.npc[i].Center) < 20f * 16)
                                lothricKnight2 = i;
                        }
                    }

                    if (NPC.Distance(Main.npc[lothricKnight2].Center) < 14f * 16)
                    {
                        AI_Timer = 0;
                        AI_State = State_JumpThrust;
                    }
                    if (NPC.Distance(Main.npc[lothricKnight2].Center) > 20f * 16)
                    {
                        AI_Timer = 29;
                        AI_State = State_LungeThrust;
                    }
                }
            }


            // SHIELDING
            if (AI_State == State_Shielding)
            {
                if (!playerMeleeLevel && globalNPC.ReactiveBlockTimer == 0) // a reactive block holds the guard even off-level
                {
                    AI_State = State_Pursuing;
                    AI_Timer_Shielding = 0;
                }
                else
                {
                    NPC.TargetClosest(true);
                    AI_Timer_Shielding++;

                    if (NPC.velocity.Y == 0)
                    {
                        if (AI_Timer_Shielding > 300 && AI_Timer_Shielding <= 310 && Math.Abs(NPC.velocity.X) > 1f)
                        {
                            if (NPC.direction == 1) NPC.velocity.X -= 0.15f;
                            else NPC.velocity.X += 0.15f;
                        }

                        if (AI_Timer_Shielding > 310)
                        {
                            NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                            NPC.spriteDirection = NPC.direction;
                            NPC.velocity.X = 0;
                        }

                        if (AI_Timer_Shielding > 500)
                        {
                            AI_State = State_Pursuing;
                            AI_Timer_Shielding = 0;
                        }
                    }

                    if (AI_Timer_Shielding > 310 && Math.Abs(NPC.Center.X - player.Center.X) < 6.5f * 16 && Math.Abs(NPC.Center.Y - player.Center.Y) < 6.5f * 16 && grounded && NPC.velocity.Y == 0 && los)
                        AI_State = State_Thrusting;

                    if (AI_Timer_Shielding > 310 && AI_Timer_Attacking == 420 && Math.Abs(NPC.Center.X - player.Center.X) > 6.5f * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 19f * 16 && Math.Abs(NPC.Center.Y - player.Center.Y) < 12f * 16 && grounded && NPC.velocity.Y == 0 && los)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].type == ModContent.NPCType<LothricKnight>())
                            {
                                if (NPC.Distance(Main.npc[i].Center) < 20f * 16)
                                    lothricKnight = i;
                            }
                        }

                        if (NPC.Distance(Main.npc[lothricKnight].Center) < 14f * 16)
                        {
                            AI_Timer = 0;
                            AI_Timer_Shielding = 0;
                            AI_State = State_JumpThrust;
                        }
                        else if (NPC.Distance(Main.npc[lothricKnight].Center) > 20f * 16)
                            AI_State = State_LungeThrust;
                    }
                }
            }


            // THRUSTING (while shielding)
            if (AI_State == State_Thrusting)
            {
                AI_Timer++;
                AI_Timer_Shielding = 400;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;

                if (AI_Timer == 9)
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.White));

                #region Projectiles & Sounds
                if (NPC.direction == 1)
                {
                    if (AI_Timer == 34)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                            stab.timeLeft = 6;
                            stab.velocity.X = 5;
                        }
                    }
                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                            stab.timeLeft = 6;
                            stab.velocity.X = 5;
                        }
                    }
                    if (AI_Timer == 77)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
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
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }
                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }
                    if (AI_Timer == 76)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricSmallDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                            stab.timeLeft = 6;
                            stab.velocity.X = -5;
                        }
                    }
                }
                #endregion

                if (AI_Timer > 94)
                {
                    if (AI_Timer_Attacking == 420 && NPC.Distance(player.Center) < 175 && los)
                    {
                        AI_Timer = 0;
                        AI_Timer_Shielding = 0;
                        AI_State = State_JumpThrust;
                    }
                    else
                    {
                        AI_Timer = 0;
                        AI_State = State_Shielding;
                    }
                }
            }


            // JUMP-THRUST
            if (AI_State == State_JumpThrust)
            {
                NPC.knockBackResist = 0;

                if (NPC.velocity.X > 6f) NPC.velocity.X = 6f;
                if (NPC.velocity.X < -6f) NPC.velocity.X = -6f;

                if (AI_Timer < 82)
                    AI_Timer++;

                if (AI_Timer == 20)
                    NPC.velocity.Y -= 10f;

                if (AI_Timer == 52 || (AI_Timer > 23 && AI_Timer < 52 && NPC.collideY))
                {
                    AI_Timer = 52;
                    NPC.velocity.X = 0;
                    NPC.velocity.Y = 0;
                    NPC.noGravity = true;
                }

                if (AI_Timer >= 52 && AI_Timer <= 82)
                    NPC.TargetClosest(true);

                if (AI_Timer == 57)
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Red));

                if (AI_Timer == 81)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { PitchVariance = .3f }, NPC.Center);

                if (AI_Timer == 82)
                {
                    float power;
                    NPC.noGravity = false;
                    NPC.velocity.Y += 4f;
                    if (NPC.direction == 1)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(28, +38), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricBigDamage, 5, Main.myPlayer, NPC.whoAmI, 0)];
                            stab.timeLeft = 2;
                        }
                        power = (Math.Abs(NPC.Center.X - player.Center.X) / 16) * 4 / 10;
                        NPC.velocity.X += power;
                    }
                    else
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-28, +38), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricBigDamage, 5, Main.myPlayer, NPC.whoAmI, 0)];
                            stab.timeLeft = 2;
                        }
                        power = (Math.Abs(NPC.Center.X - player.Center.X) / 16) * 4 / 10;
                        NPC.velocity.X -= power;
                    }
                }

                if (AI_Timer >= 82 && NPC.collideY)
                {
                    NPC.velocity.X = 0;
                    NPC.velocity.Y = 0;
                    AI_Timer_Attacking = 0;
                    AI_Timer++;
                }

                if (AI_Timer == 192)
                {
                    AI_Timer = 0;
                    if (NPC.Distance(player.Center) < 175)
                    {
                        AI_Timer_Shielding = 400;
                        AI_State = State_Shielding;
                    }
                    else
                        AI_State = State_Pursuing;
                }
            }


            // LUNGE-THRUST
            if (AI_State == State_LungeThrust)
            {
                NPC.knockBackResist = 0;

                if (NPC.velocity.X > 8.5f) NPC.velocity.X = 8.5f;
                if (NPC.velocity.X < -8.5f) NPC.velocity.X = -8.5f;

                if (AI_Timer < 55)
                    AI_Timer++;

                if (AI_Timer < 30)
                    NPC.TargetClosest(true);

                if (AI_Timer == 5)
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Yellow));

                if (AI_Timer == 30)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { PitchVariance = .3f }, NPC.Center);
                    if (NPC.direction == 1) NPC.velocity.X += 8.5f;
                    else NPC.velocity.X -= 8.5f;
                }

                if (AI_Timer >= 30 && AI_Timer < 56 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (NPC.direction == 1)
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricDamage, 5, Main.myPlayer, NPC.whoAmI, 0)];
                        stab.timeLeft = 2;
                    }
                    else
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), lothricDamage, 5, Main.myPlayer, NPC.whoAmI, 0)];
                        stab.timeLeft = 2;
                    }
                }

                if (AI_Timer > 30 && NPC.collideX)
                    AI_Timer = 56;

                if (AI_Timer == 56)
                    AI_Timer_Attacking = 200;

                if (AI_Timer > 45 && AI_Timer < 56 && grounded)
                {
                    if (Math.Abs(NPC.velocity.X) > 0.1f)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= .85f;
                            if (NPC.velocity.X < 0.2f) NPC.velocity.X = 0;
                        }
                        else
                        {
                            NPC.velocity.X += .85f;
                            if (NPC.velocity.X > -0.2f) NPC.velocity.X = 0;
                        }
                    }
                }

                if (AI_Timer >= 55)
                {
                    if (Math.Abs(NPC.velocity.X) <= 0.1f)
                        AI_Timer++;
                }

                if (AI_Timer == 100)
                {
                    AI_State = State_Pursuing;
                    AI_Timer = 0;
                }
            }
        }

        public void OnStagger(NPC npc)
        {
            AI_State = State_Pursuing;
            AI_Timer = 0;
            AI_Timer_Shielding = 0;
            AI_Timer_Attacking = 200;
            NPC.noGravity = false;
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (AI_State == State_Shielding || AI_State == State_Thrusting)
            {
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.FinalDamage.Flat -= 80;
                        if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.FinalDamage.Flat -= 80;
                        if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                    }
                }
            }

            if (NPC.direction == 1)
            {
                if (player.position.X < NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                }
            }
            else
            {
                if (player.position.X > NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                }
            }

            AI_Timer_Shielding += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[NPC.target];

            int direction = modifiers.HitDirection;

            Type hm = typeof(NPC.HitModifiers);
            PropertyInfo prop = hm.GetProperty("HitDirectionOverride");
            int? over = (int?)prop.GetValue(modifiers);

            if (over != null && over != 0)
                direction = over.Value;

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.Specialist.BlizzardBlasterShot>())
            {
                if (AI_State == State_Shielding || AI_State == State_Thrusting)
                {
                    if (NPC.direction == 1)
                    {
                        if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= 80;
                            modifiers.Knockback *= 0f;
                            if (AI_Timer_Attacking < 340) AI_Timer_Attacking += 70;
                            if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                        }
                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= 80;
                            modifiers.Knockback *= 0f;
                            if (AI_Timer_Attacking < 340) AI_Timer_Attacking += 80;
                            if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                        }
                    }
                    else
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= 80;
                            modifiers.Knockback *= 0f;
                            if (AI_Timer_Attacking < 340) AI_Timer_Attacking += 70;
                            if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= 80;
                            modifiers.Knockback *= 0f;
                            if (AI_Timer_Attacking < 340) AI_Timer_Attacking += 80;
                            if (AI_Timer_Shielding > 340) AI_Timer_Shielding -= 35;
                        }
                    }
                }

                if (NPC.direction == 1)
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }
                else
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType != DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }

                if (NPC.Distance(player.Center) > 220 && AI_State != State_Shielding && AI_State != State_Thrusting)
                {
                    AI_Timer_Shielding += 120;
                    if (AI_Timer_Shielding > 300) AI_Timer_Shielding = 300;
                }

                if (AI_Timer_Attacking < 400)
                    AI_Timer_Attacking += 10;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (spawnInfo.Water) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (spawnInfo.Player.ZoneDungeon) return chance = 0.02f;

            if (tsorcRevampWorld.SuperHardMode && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return 0.02f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon) return 0.05f;

            if (Main.bloodMoon && spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneSkyHeight && NPC.downedBoss3) return chance = 0.02f;

            if (Main.bloodMoon && NPC.downedBoss3 && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.02f;

            if (NPC.downedBoss3 && spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneSkyHeight && Main.dayTime && !spawnInfo.Player.ZoneJungle) return chance = 0.005f;
            if (NPC.downedBoss3 && spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneSkyHeight && !Main.dayTime && !spawnInfo.Player.ZoneJungle) return chance = 0.015f;
            if (NPC.downedBoss3 && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.003f;

            return chance;
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 12, 24));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight, 1));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Items.Potions.RadiantLifegem>(), 3, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpikedIronShield>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.LostUndeadSoul>(), 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.LifeforcePotion, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.EndurancePotion, 6));
        }
        #endregion

        #region Drawing & Animation

        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if ((NPC.velocity.X > 3f || NPC.velocity.X < -3f || NPC.velocity.Y != 0) && (AI_State == State_JumpThrust || AI_State == State_LungeThrust))
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, NPC.gfxOffY);
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    if (NPC.direction == 1)
                        spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), color, NPC.rotation, new Vector2(NPC.position.X + 26, NPC.position.Y + 26), NPC.scale, effects, 0f);
                    else
                        spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), color, NPC.rotation, new Vector2(NPC.position.X + 70, NPC.position.Y + 26), NPC.scale, effects, 0f);
                }
            }

            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            if (NPC.spriteDirection == 1)
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), lightColor, NPC.rotation, new Vector2(37, 46), NPC.scale, effects, 0f);
            else
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), lightColor, NPC.rotation, new Vector2(80, 46), NPC.scale, effects, 0f);
            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/LothricSpearKnight_Greatshield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 19, 0, shieldFrame);
            if (AI_State == State_Shielding || AI_State == State_Thrusting)
            {
                if (NPC.spriteDirection == 1)
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(37, 46), NPC.scale, effects, 0f);
                else
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(80, 46), NPC.scale, effects, 0f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Pursuing && NPC.velocity.X != 0)
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12) NPC.frame.Y = 0 * frameHeight;
                else if (NPC.frameCounter < 24) NPC.frame.Y = 1 * frameHeight;
                else if (NPC.frameCounter < 36) NPC.frame.Y = 2 * frameHeight;
                else if (NPC.frameCounter < 48) NPC.frame.Y = 3 * frameHeight;
                else if (NPC.frameCounter < 60) NPC.frame.Y = 4 * frameHeight;
                else if (NPC.frameCounter < 72) NPC.frame.Y = 5 * frameHeight;
                else if (NPC.frameCounter < 84) NPC.frame.Y = 6 * frameHeight;
                else if (NPC.frameCounter < 96) NPC.frame.Y = 7 * frameHeight;
                else NPC.frameCounter = 0;
            }

            if (AI_State == State_Pursuing && NPC.velocity.Y != 0)
                NPC.frame.Y = 3 * frameHeight;

            if (AI_State == State_Shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 8 * frameHeight;

                shieldFrame = shieldAnimTimer / 4;
                if (shieldFrame == 0) countingUP = true;
                if (shieldFrame <= 18 && countingUP) shieldAnimTimer++;
                if (shieldFrame == 18) countingUP = false;
                if (shieldFrame >= 0 && !countingUP) shieldAnimTimer--;
            }

            if (AI_State == State_Thrusting)
            {
                NPC.spriteDirection = NPC.direction;
                shieldFrame = shieldAnimTimer / 4;

                if (AI_Timer < 10)
                {
                    NPC.frame.Y = 9 * frameHeight;
                    NPC.frameCounter = 0;
                }

                if (AI_Timer >= 10)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter < 24) NPC.frame.Y = 11 * frameHeight;
                    else if (NPC.frameCounter < 30) NPC.frame.Y = 12 * frameHeight;
                    else if (NPC.frameCounter < 40) NPC.frame.Y = 13 * frameHeight;
                    else if (NPC.frameCounter < 46) NPC.frame.Y = 10 * frameHeight;
                    else NPC.frameCounter = 0;
                }

                if (shieldFrame == 0) countingUP = true;
                if (shieldFrame <= 18 && countingUP) shieldAnimTimer++;
                if (shieldFrame == 18) countingUP = false;
                if (shieldFrame >= 0 && !countingUP) shieldAnimTimer--;
            }

            if (AI_State == State_JumpThrust)
            {
                NPC.spriteDirection = NPC.direction;

                if (AI_Timer < 20)
                {
                    NPC.frame.Y = 19 * frameHeight;
                    NPC.frameCounter = 0;
                }
                else if (AI_Timer < 52)
                    NPC.frame.Y = 15 * frameHeight;
                else if (AI_Timer < 82)
                    NPC.frame.Y = 16 * frameHeight;
                else if (AI_Timer >= 82 && !NPC.collideY)
                    NPC.frame.Y = 17 * frameHeight;
                else if (AI_Timer >= 82 && AI_Timer < 162 && NPC.collideY)
                    NPC.frame.Y = 18 * frameHeight;
                else if (AI_Timer >= 162)
                    NPC.frame.Y = 19 * frameHeight;
            }

            if (AI_State == State_LungeThrust)
            {
                NPC.spriteDirection = NPC.direction;

                if (AI_Timer < 30) NPC.frame.Y = 11 * frameHeight;
                if (AI_Timer >= 30 && AI_Timer < 65) NPC.frame.Y = 14 * frameHeight;
                if (AI_Timer >= 65) NPC.frame.Y = 19 * frameHeight;
            }
        }

        #endregion
    }
}
