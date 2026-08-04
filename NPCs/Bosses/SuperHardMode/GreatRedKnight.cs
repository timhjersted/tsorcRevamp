using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class GreatRedKnight : ModNPC, IHumanoidMeleeHitEffects, NPCs.IDebugAttackLabel
    {
        public int poisonStrikeDamage = 35;
        public int redKnightsSpearDamage = 45;
        public int redMagicDamage = 40;
        public int redKnightsGreatDamage = 50;

        Vector2 storedPlayerPosition = Vector2.Zero;
        readonly RedKnightAttackController specialAttacks = new RedKnightAttackController();

        public int framesSinceStoredPosition = 0;

        NPCDespawnHandler despawnHandler;

        #region Defaults
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = 3; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 100;
            NPC.defense = 61;
            NPC.lifeMax = 30000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 225000; // 9000 Dark Souls via GlobalNPC.OnKill expert-mode payout
            NPC.knockBackResist = 0.2f; // poise flinch dial (boss). BasicAI restores this each tick despite the attack-state =0f lines.
            NPC.scale = 1.15f;
            NPC.boss = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GreatRedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);
            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            redKnightGlobalNPC.Agility = 0.45f;
            EvasiveProfile.RedKnight(redKnightGlobalNPC); // hop/leap/dash away, or blink away when able

            // Poise: boss-tier — many hits to stagger, and the impulse is halved for bosses. Tunable lever.
            redKnightGlobalNPC.PoiseMax = 80f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Navigation tuning: maximum jumps, double jump, and ledge routing
            redKnightGlobalNPC.MaxJumpPower = 12f;
            redKnightGlobalNPC.MaxJumpBoost = 8f;
            redKnightGlobalNPC.CanDoubleJump = true;
            redKnightGlobalNPC.DoubleJumpPower = 8f;
            // Step 6 boss lever: blink aggressively the moment it loses LOS (keeps arena pressure).
            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Fire;
            redKnightGlobalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            redKnightGlobalNPC.CanUseRopes = true;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            poisonStrikeDamage = (int)(poisonStrikeDamage * tsorcRevampWorld.SHMScale);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage * tsorcRevampWorld.SHMScale);
            redMagicDamage = (int)(redMagicDamage * tsorcRevampWorld.SHMScale);
            redKnightsGreatDamage = (int)(redKnightsGreatDamage * tsorcRevampWorld.SHMScale);
        }
        #endregion


        public Player player
        {
            get => Main.player[NPC.target];
        }

        public string DebugAttackLabel
        {
            get
            {
                if (specialAttacks.Active)
                {
                    return specialAttacks.DebugAttackName;
                }
                if (specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2
                    && NPC.ai[2] >= 100f && NPC.ai[2] <= 249f)
                {
                    return "Ultrakill Barrage";
                }
                if ((NPC.ai[2] >= 70f && NPC.ai[2] <= 105f) || (NPC.ai[2] >= 520f && NPC.ai[2] <= 605f))
                {
                    return "Abyssal Rain";
                }
                if (NPC.ai[1] >= 120f && NPC.ai[1] <= 230f)
                {
                    return "Spear Throw";
                }
                if (NPC.ai[1] >= 270f && NPC.ai[1] <= 525f)
                {
                    return "Poison Salvo";
                }
                if (specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2
                    && NPC.ai[1] >= 695f && NPC.ai[1] < 900f)
                {
                    return "Drakin Bombardment";
                }
                if (NPC.ai[1] >= 865f)
                {
                    return "Firebomb Throw";
                }
                return "Idle";
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(storedPlayerPosition.X);
            writer.Write(storedPlayerPosition.Y);
            specialAttacks.Send(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            storedPlayerPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            specialAttacks.Receive(reader);
        }

        #region On Hit
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }
        #endregion
        
        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            // Proximity Debuffs
            if (NPC.Distance(player.Center) < 700)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 1 * 60, false);
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 1 * 60, false);
            }

            specialAttacks.TickCooldowns();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            KnightAttackStats attackStats = new KnightAttackStats(redKnightsSpearDamage, redMagicDamage, redKnightsGreatDamage);
            if (specialAttacks.Active)
            {
                specialAttacks.Tick(NPC, player, attackStats);
                Lighting.AddLight(NPC.Center, new Color(190, 35, 30).ToVector3() * 0.65f);
                return;
            }

            tsorcRevampAIs.FighterAI(NPC, 2, canTeleport: true, enragePercent: 0.5f, enrageTopSpeed: 4, canDodgeroll: true);
            tsorcRevampAIs.LeapAtPlayer(NPC, 7, 5, 1.5f, 128);

            Vector2 targetPosition = Vector2.Zero;

            //Block firing and reset cooldowns if it's busy doing other things that it shouldn't be able to shoot during
            globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (specialAttacks.TryStartGreat(NPC, player, globalNPC))
            {
                specialAttacks.Tick(NPC, player, attackStats);
                return;
            }

            // Hyper-armor window: FLASH telegraph → fire only. Windup excluded so a stagger can interrupt it.
            // Spear 180→210, poison 300→375, attack4 450→485, DD2/bomb 725→955.
            globalNPC.AttackCommitted = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                        (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                        (NPC.ai[1] >= 450f && NPC.ai[1] <= 485f) ||
                                        (NPC.ai[1] >= 725f && NPC.ai[1] <= 955f);

            // Windup (~30t before each flash): poise can still break here, but the evasive on-hit reaction is suppressed.
            globalNPC.AttackTelegraphing = (NPC.ai[1] >= 150f && NPC.ai[1] < 180f) ||
                                           (NPC.ai[1] >= 270f && NPC.ai[1] < 300f) ||
                                           (NPC.ai[1] >= 420f && NPC.ai[1] < 450f) ||
                                           (NPC.ai[1] >= 695f && NPC.ai[1] < 725f);

            if (globalNPC.TeleportCountdown > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                bool inProtectedAttack = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                          (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                          (NPC.ai[1] >= 450f && NPC.ai[1] <= 485f) ||
                                          (NPC.ai[1] >= 725f && NPC.ai[1] <= 955f) ||
                                          (NPC.ai[2] >= 165f && NPC.ai[2] <= 249f);
                if (!inProtectedAttack)
                {
                    NPC.ai[1] = 60f;
                    NPC.ai[2] = -100f;
                }
            }

            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                // Freeze the attack timer while staggered so the ~1s stun holds (and a reset windup stays neutral).
                if (globalNPC.StaggerTimer <= 0)
                {
                    NPC.ai[1]++;
                    NPC.ai[2]++;
                }

                bool inActiveAttack = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                       (NPC.ai[1] >= 300f && NPC.ai[1] <= 485f) ||
                                       (NPC.ai[1] >= 725f && NPC.ai[1] <= 955f) ||
                                       (NPC.ai[2] >= 165f && NPC.ai[2] <= 249f);

                // Gate projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                if (hasPlayerLOS && (NPC.ai[1] == 210f || NPC.ai[1] == 325f || NPC.ai[1] == 375f || NPC.ai[1] == 480f || NPC.ai[1] == 750f || NPC.ai[1] == 850f || NPC.ai[1] == 955f))
                {
                    tsorcRevampAIs.RegisterFighterAttack(NPC);
                }

                #region Sounds & Jumps
                // Play creature sounds
                if (Main.rand.NextBool(1000))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
                }
                // Chance to jump forward
                if (NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(500) && (NPC.ai[1] <= 150f || NPC.ai[1] >= 476f))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-4, -8f);
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;
                    if ((float)NPC.direction * NPC.velocity.X > 2)
                        NPC.velocity.X = (float)NPC.direction * 2;
                    NPC.netUpdate = true;
                }
                // Chance to dash step forward
                if (NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(140) && (NPC.ai[1] <= 220f || NPC.ai[1] >= 276f))
                {
                    NPC.velocity.Y = -4f;
                    NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                    if ((float)NPC.direction * NPC.velocity.X > 4)
                        NPC.velocity.X = (float)NPC.direction * 4;

                    // Chance to jump after dash
                    if (Main.rand.NextBool(6) && (NPC.ai[1] <= 150f || NPC.ai[1] >= 476f))
                    {
                        NPC.velocity.Y = -8f;
                    }
                    NPC.netUpdate = true;
                }
                // Offensive jump before 3 attacks
                if ((NPC.ai[1] == 145 || NPC.ai[1] == 275 || NPC.ai[1] == 890) && NPC.velocity.Y <= 0f && Main.rand.NextBool(4))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-6, -10f);
                    NPC.netUpdate = true;
                }
                #endregion

                // Skip spear if player is at melee range — it's a ranged weapon
                if (NPC.ai[1] == 120f && NPC.Distance(player.Center) < 120f)
                {
                    NPC.ai[1] = 230f;
                    NPC.netUpdate = true;
                }

                // Increment the frames since we stored the player's position
                framesSinceStoredPosition++;

                // Spear Attack: Get targetPosition 
                if (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Use the stored player's position from 25 frames ago to calculate the targetPosition.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);
                }

                // Spear Telegraph
                if (NPC.ai[1] == 180f)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                    }

                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                        }
                    }
                }

                // Spear Attack Far
                if (NPC.ai[1] == 210f && NPC.Distance(player.Center) > 400)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(16, 19f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    speed.Y += Main.rand.NextFloat(-2f, 2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer, ai2: 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 230f;

                    // Chance to fire Spear again
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 90f;
                        NPC.netUpdate = true;
                    }
                }
                // Spear Attack Close
                if (NPC.ai[1] == 210f && NPC.Distance(player.Center) <= 400)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(11, 13f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    speed += Main.player[NPC.target].velocity;
                    speed.Y += Main.rand.NextFloat(-1f, 1f); //adds random variation from -1 to 2
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer, ai2: 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 230f;

                    // Chance to fire Spear again
                    if (Main.rand.NextBool(3))
                    {
                        NPC.ai[1] = 90f;
                        NPC.netUpdate = true;
                    }
                }

                // Poison Attack 1 Telegraph 
                if (NPC.ai[1] == 300)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.GreenYellow));
                    }
                }

                // Poison Attack 1
                if (NPC.ai[1] == 325 && hasPlayerLOS)
                {
                    float projectileSpeed = 6f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 4; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                        speed2 += Main.player[NPC.target].velocity / 2; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer, ai2: 1f);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 2f }, NPC.Center);
                        }

                    }

                    // Reset the targetPosition
                    targetPosition = Vector2.Zero;

                }

                // Poison Attack 2 Telegraph
                if (NPC.ai[1] == 350)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Green));
                    }
                }

                // Poison Attack 2
                if (NPC.ai[1] == 375 && hasPlayerLOS)
                {
                    float projectileSpeed = 7f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 4; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                        speed2 += Main.player[NPC.target].velocity / 2; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer, ai2: 1f);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 2f }, NPC.Center);
                        }
                    }

                }
                // Poison Attack 3 Telegraph
                if (NPC.ai[1] == 450)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Green));
                    }
                }

                // Poison Attack 3 — fixed 4-shot burst with slight speed variation
                if (NPC.ai[1] == 480)
                {
                    NPC.TargetClosest(true);
                    if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9f + i * 0.5f);
                                speed2 += Main.player[NPC.target].velocity;
                                speed2 += Main.rand.NextVector2Circular(0.5f, 0.5f);
                                if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer, ai2: 1f);
                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, PitchVariance = 2f }, NPC.Center);
                                }
                            }
                        }
                    }
                    NPC.netUpdate = true;
                }

                if (NPC.ai[1] == 525)
                {
                    // Shorter or longer pause before bomb attack
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 700f;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.ai[1] = 800f;
                        NPC.netUpdate = true;
                    }
                }

                // DD2DrakinShot Attack Telegraph at 1/2 health
                if ((NPC.ai[1] == 725 || NPC.ai[1] == 825) && specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.DeepPink));
                    }
                }
                // DD2DrakinShot Attack at 1/2 health
                if ((NPC.ai[1] >= 750 && NPC.ai[1] < 800 || NPC.ai[1] >= 850 && NPC.ai[1] < 900) && specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2 && hasPlayerLOS)
                {
                    bool clearSpace = true;
                    for (int i = 0; i < 15; i++)
                    {
                        if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                        {
                            clearSpace = false;
                        }
                    }

                    if (clearSpace)
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                        speed.Y += Main.rand.NextFloat(-2f, -6f);
                        if (((speed.X < 0f) && (NPC.direction < 0)) || ((speed.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        }
                    }
                }

                // Code for Bomb Telegraph & Attack: 
                if (NPC.ai[1] >= 925f && NPC.ai[1] <= 955f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);
                }

                // Bomb Telegraph
                if (NPC.ai[1] == 925f)
                {
                    Terraria.Audio.SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center); // lit fuse
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                    }
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);

                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                        }
                    }

                }
                // Bomb Attack Far
                if (NPC.ai[1] == 955f && NPC.Distance(player.Center) > 400)
                {
                    float bombProjectileSpeed = 14f;

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    //speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer, ai2: 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    // Reset attack counter
                    NPC.ai[1] = 0f;

                    // Chance to throw again
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 830f;
                        NPC.netUpdate = true;
                    }
                }
                // Bomb Attack Close
                if (NPC.ai[1] == 955f && NPC.Distance(player.Center) <= 400)
                {
                    float bombProjectileSpeed = 9f;
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer, ai2: 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    NPC.ai[1] = 0f;

                    // Chance to throw again
                    if (Main.rand.NextBool(3))
                    {
                        NPC.ai[1] = 830f;
                        NPC.netUpdate = true;
                    }
                }

                #region AI 2 Attacks
                // Air attack targeting indicator — dust appears above drop zone 3 frames before each wave
                // (telegraph still shows without LOS so the player gets fair warning when the knight IS above them)
                if ((NPC.ai[2] == 68 || NPC.ai[2] == 93 || NPC.ai[2] == 518 || NPC.ai[2] == 543 || NPC.ai[2] == 568 || NPC.ai[2] == 593) && !inActiveAttack)
                {
                    for (int i = 0; i < 6; i++)
                        Dust.NewDust(new Vector2(player.Center.X - 130f + Main.rand.Next(260), player.Top.Y - 250f), 4, 4, DustID.CursedTorch, 0f, 2f, 100, new Color(130, 205, 28), 1.05f);
                }

                // Fire Attack from Air
                if ((NPC.ai[2] == 75 || NPC.ai[2] == 525 || NPC.ai[2] == 575) && !inActiveAttack)
                {
                    SpawnAbyssalRainVolley(player, 3, 190f, 250f, 5.4f);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    NPC.netUpdate = true;
                }

                // Slightly Delayed Fire Attack From Air
                if ((NPC.ai[2] == 100 || NPC.ai[2] == 550 || NPC.ai[2] == 600) && !inActiveAttack)
                {
                    SpawnAbyssalRainVolley(player, 4, 420f, 280f, 4.8f);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    NPC.netUpdate = true;
                }


                // Breather before reset
                if (NPC.ai[2] >= 1100)
                {
                    NPC.ai[2] = 0;
                }

                // Ultrakill Telegraph: Shrinking Dust Circle
                if (specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
                {
                    NPC.knockBackResist = 0f;
                    NPC.ai[1] = -130;
                    Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f);
                    NPC.velocity.X *= 0.85f;
                }
                // Ultrakill Telegraph: Flash
                if (specialAttacks.HalfHeraldComplete && NPC.ai[2] == 165f)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.White));

                    }
                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                        }
                    }

                }
                // Ultrakill Attack — pure hand barrage (~50 InsanityShadowHostile claws). The old
                // "Exlosives" EnemyGreatAttack spawn was cut: it only existed to trigger a pink
                // RedKnightVFXBurst explosion + vanilla BombSkeletronPrime bomb-smoke dust on death,
                // neither of which reads as "hands" and both just cluttered the screen.
                if (specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 200f && NPC.ai[2] <= 249f)
                {
                    NPC.velocity.X *= 0.25f;

                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Set targetPosition with an offset of 10f * direction units from the storedPlayerPosition along the X-axis.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    // Insanity Hands
                    Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 6, fallback: true);
                    speed2 += Main.rand.NextVector2Circular(-4, 4);//was -4, -2, then -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ProjectileID.InsanityShadowHostile, redKnightsGreatDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);
                    NPC.netUpdate = true;
                }
                // After Ultrakill attack completes
                if (NPC.ai[2] == 250f)
                {
                    // Reset the targetPosition
                    targetPosition = Vector2.Zero;
                }

                #endregion

                // Jellyfish Lightning Attack at 1/3 life
                if (specialAttacks.ThirdHeraldComplete && NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 300 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player closestPlayer = UsefulFunctions.GetClosestPlayer(NPC.Center);
                    if (closestPlayer != null)
                    {
                        Vector2 targetVector = UsefulFunctions.Aim(NPC.Center, closestPlayer.Center, 1);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<Projectiles.Enemy.JellyfishLightning>(), 30, 1, Main.myPlayer, 0, NPC.whoAmI);
                    }
                }

                // Rain of Cursed Flame at 1/3 life
                if (specialAttacks.ThirdHeraldComplete && NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 60 == 0)
                {
                    Player nT = Main.player[NPC.target];

                    for (int pcy = 0; pcy < 3; pcy++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 550f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.01f }); //flamethrower
                    }
                }

            }




        }


        void SpawnAbyssalRainVolley(Player target, int count, float spread, float height, float downwardSpeed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float lane = count == 1 ? 0.5f : i / (float)(count - 1);
                float x = MathHelper.Lerp(target.Center.X - spread * 0.5f,
                    target.Center.X + spread * 0.5f, lane) + Main.rand.NextFloat(-14f, 14f);
                Vector2 spawn = FindOpenRainOrigin(new Vector2(x, target.Top.Y - height), target.Top.Y);
                Vector2 velocity = new(Main.rand.NextFloat(-0.75f, 0.75f),
                    downwardSpeed + Main.rand.NextFloat(-0.25f, 0.35f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(),
                    redMagicDamage, 1f, Main.myPlayer, ai2: 1f);
            }
        }

        static Vector2 FindOpenRainOrigin(Vector2 preferred, float targetTop)
        {
            Vector2 halfSize = new Vector2(8f);
            for (int step = 0; step <= 14; step++)
            {
                Vector2 candidate = preferred + new Vector2(0f, step * 16f);
                if (candidate.Y <= targetTop - 48f
                    && !Collision.SolidCollision(candidate - halfSize, 16, 16))
                {
                    return candidate;
                }
            }
            for (int step = 1; step <= 12; step++)
            {
                Vector2 candidate = preferred - new Vector2(0f, step * 16f);
                if (!Collision.SolidCollision(candidate - halfSize, 16, 16))
                {
                    return candidate;
                }
            }
            return new Vector2(preferred.X, targetTop - 64f);
        }


        public override void OnKill()
        {

            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 20, 50));

            IItemDropRule drop1 = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 4, 6);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 1);
            IItemDropRule drop3 = ItemDropRule.Common(ModContent.ItemType<FlameOfTheAbyss>(), 1, 2, 3);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop1);
            shmCondition.OnSuccess(drop2);
            shmCondition.OnSuccess(drop3);
            npcLoot.Add(shmCondition);
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyHitDebuffs(target);
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            ApplyHitDebuffs(target);
        }

        static void ApplyHitDebuffs(Player target)
        {
            target.AddBuff(BuffID.OnFire, 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 6 * 60, false); // knockback on hit
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 6 * 60, false); // no health regen
            target.AddBuff(ModContent.BuffType<Crippled>(), 6 * 60, false); // loss of flight mobility
            target.AddBuff(BuffID.BrokenArmor, 6 * 60, false);
        }
        #endregion

        #region PreDraw
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (NPC.alpha < 255 && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0)
            {
                if (specialAttacks.IsHerald)
                {
                    Projectiles.Enemy.RedKnightVFX.DrawHerald(NPC.Center,
                        specialAttacks.TelegraphProgress,
                        specialAttacks.Attack == KnightSpecialAttack.StormHerald);
                }
                else if (specialAttacks.HalfHeraldComplete && NPC.life <= NPC.lifeMax / 2
                    && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
                {
                    Projectiles.Enemy.RedKnightVFX.DrawUltrakillSeal(NPC.Center,
                        MathHelper.Clamp((NPC.ai[2] - 100f) / 100f, 0f, 1f));
                }
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.velocity.X > 5f || NPC.velocity.X < -5f)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height - NPC.gfxOffY - 2) - Main.screenPosition; // Where to draw trails, adjusted by 2 pixels
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(74 * 0.5f, 56), NPC.scale, effects, 0f);
                }
            }
            return true;
        }
        #endregion

        #region Draw Attack Sprites
        static Texture2D spearTexture;
        static Texture2D bombTexture;
        static Texture2D magicBallTexture;
        static Texture2D armOverlayTexture;

        // --- Hand-overlay experiment ---
        // The held weapon and the hand both anchor to the knight's gripping hand, which we track per animation frame.
        // Layering: body (normal draw) < weapon (here) < hand (here, on top), so the hand appears to grip the weapon.

        // Sheet is 70x56 per frame, raw art faces LEFT. Hand pixel = where the body's gripping hand sits in each frame.
        // frame 0 = idle, frame 1 = jump (hands up by the head), frames 2-15 = walk cycle. Tune these to your sheet.
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 42), // 0 idle
            new Vector2(33, 12), // 1 jump — hands raised near the head
            new Vector2(40, 35), // 2
            new Vector2(40, 35), // 3
            new Vector2(41, 35), // 4
            new Vector2(41, 35), // 5
            new Vector2(42, 35), // 6
            new Vector2(42, 35), // 7
            new Vector2(43, 35), // 8
            new Vector2(43, 35), // 9
            new Vector2(43, 35), // 10
            new Vector2(43, 35), // 11
            new Vector2(42, 35), // 12
            new Vector2(42, 35), // 13
            new Vector2(41, 35), // 14
            new Vector2(41, 35), // 15
        };
        static readonly Vector2[] OverlayHandPixel = new Vector2[16]
        {
            new Vector2(48, 47), // 0 idle
            new Vector2(49, 26), // 1 jump
            new Vector2(48, 33), // 2
            new Vector2(50, 31), // 3
            new Vector2(50, 31), // 4
            new Vector2(50, 31), // 5
            new Vector2(50, 33), // 6
            new Vector2(48, 33), // 7
            new Vector2(48, 33), // 8
            new Vector2(48, 33), // 9
            new Vector2(46, 31), // 10
            new Vector2(44, 31), // 11
            new Vector2(44, 31), // 12
            new Vector2(46, 33), // 13
            new Vector2(48, 33), // 14
            new Vector2(48, 33), // 15
        };
        // Global correction if the whole overlay is consistently off by a few px (tune once, applies to all frames).
        static readonly Vector2 OverlayFudge = new Vector2(0f, 0f);

        // Grip pixel ON each sprite = the point that should land on the knight's hand (rotation pivot for the weapon).
        static readonly Vector2 SpearGripOrigin = new Vector2(7f, 31f);  // BlackKnightSpear (Valkyrie's spear) is 14x62, tip up — grip the MIDDLE (was 7,70 = the butt of the old 14x84 BlackThrowingSpear)
        static readonly Vector2 BombGripOrigin = new Vector2(11f, 18f);  // EnemyFirebomb is 22x24, hand near the bottom
        static readonly Vector2 MagicBallGripOrigin = new Vector2(8f, 8f);
        const float MagicBallBodyInset = 8f;

        // World position of the body's gripping hand for the current animation frame.
        Vector2 CurrentHandWorld(int facingDirection)
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= OverlayHandPixel.Length)
            {
                frame = 0;
            }
            Vector2 fp = OverlayHandPixel[frame];
            // Map a 70x56 frame pixel to world: horizontally centered on the hitbox, bottom of frame 4px below the hitbox bottom.
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -facingDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y) + OverlayFudge;
        }

        Vector2 CurrentHandWorld()
        {
            return CurrentHandWorld(NPC.spriteDirection);
        }

        Vector2 CurrentMagicBallWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            float bodyDirection = Math.Sign(NPC.Center.X - handWorld.X);
            return handWorld + new Vector2(bodyDirection * MagicBallBodyInset, 0f);
        }

        Vector2 CurrentSpearWorld(int facingDirection)
        {
            // Preserve the authored spear grip used by the original throw telegraph.
            Vector2 handWorld = CurrentHandWorld(facingDirection);
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame == 0)
            {
                handWorld.Y -= 21f;
            }
            else if (frame >= 2)
            {
                handWorld.Y -= 5f;
            }
            return handWorld;
        }

        Vector2 CurrentSpearWorld()
        {
            return CurrentSpearWorld(NPC.spriteDirection);
        }

        void DrawHeldSpear(SpriteBatch spriteBatch, Vector2 screenPosition, float rotation,
            Color drawColor, float gripSlide = 0f)
        {
            Vector2 gripOrigin = SpearGripOrigin + new Vector2(0f, gripSlide);
            spriteBatch.Draw(spearTexture, screenPosition, null, drawColor, rotation,
                gripOrigin, NPC.scale, SpriteEffects.None, 0f);
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor, int facingDirection)
        {
            if (armOverlayTexture == null)
            {
                return;
            }

            SpriteEffects effects = facingDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(armOverlayTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            DrawArmOverlay(spriteBatch, drawColor, NPC.spriteDirection);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (NPC.alpha >= 255 || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }

            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightSpear"); // the spear Tibian Valkyrie uses (14x62), held gripped at its middle
            }

            if (bombTexture == null)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyFirebomb");
            }

            if (magicBallTexture == null)
            {
                magicBallTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemySpellAbyssPoisonStrikeBall");
            }

            if (armOverlayTexture == null)
            {
                armOverlayTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/RedKnight_LeftArm", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            if (specialAttacks.Active)
            {
                DrawSpecialAttack(spriteBatch, drawColor);
                return;
            }

            // Spear
            if (NPC.ai[1] >= 120 && NPC.ai[1] < 210f)
            {
                Vector2 handWorld = CurrentSpearWorld() - Main.screenPosition;
                Vector2 spearAim = NPC.ai[1] >= 180f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;

                // Weapon behind the hand, pivoting on the grip so it aims at the throw target.
                DrawHeldSpear(spriteBatch, handWorld, rotation, drawColor);
                DrawArmOverlay(spriteBatch, drawColor);
            }
            // Magic ball
            if (magicBallTexture != null && ((NPC.ai[1] >= 225 && NPC.ai[1] <= 325f) || (NPC.ai[1] >= 350 && NPC.ai[1] <= 375f) || (NPC.ai[1] >= 400 && NPC.ai[1] <= 480f)))
            {
                Vector2 magicBallWorld = CurrentMagicBallWorld();
                Projectiles.Enemy.RedKnightVFX.DrawToxicMotes(magicBallWorld, 3, 0.78f, 20f);
                DrawArmOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (NPC.ai[1] >= 865)
            {
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                Vector2 bombAim = NPC.ai[1] >= 925f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;

                Vector2 fusePoint = handWorld + Main.screenPosition + new Vector2(0f, -15f).RotatedBy(rotation);
                Projectiles.Enemy.RedKnightVFX.DrawBombFuse(fusePoint,
                    MathHelper.Clamp((NPC.ai[1] - 895f) / 60f, 0f, 1f), planted: false);
                spriteBatch.Draw(bombTexture, handWorld, null, drawColor, rotation, BombGripOrigin, 1f, SpriteEffects.None, 0);
                DrawArmOverlay(spriteBatch, drawColor);
            }

        }

        void DrawSpecialAttack(SpriteBatch spriteBatch, Color drawColor)
        {
            KnightHeldProp heldProp = specialAttacks.HeldProp;
            if (heldProp == KnightHeldProp.Spear)
            {
                Vector2 handWorld = CurrentSpearWorld(specialAttacks.Direction);
                if (specialAttacks.Attack == KnightSpecialAttack.CrimsonAdvance)
                {
                    handWorld.Y += 5f;
                }
                float rotation = specialAttacks.GetSpearRotation(handWorld);
                float gripSlide = specialAttacks.SpearGripSlide;
                if (specialAttacks.SpearDamageWake)
                {
                    Vector2 forward = (rotation - MathHelper.PiOver2).ToRotationVector2();
                    Projectiles.Enemy.RedKnightVFX.DrawSpearWake(
                        handWorld + forward * (gripSlide * 0.5f), forward.ToRotation(),
                        new Vector2(76f, 18f), 0.56f, empowered: true);
                }
                DrawHeldSpear(spriteBatch, handWorld - Main.screenPosition, rotation, drawColor, gripSlide);
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
                return;
            }

            if (heldProp == KnightHeldProp.Bomb)
            {
                Vector2 handWorld = CurrentHandWorld(specialAttacks.Direction);
                float rotation = new Vector2(specialAttacks.Direction, 0f).ToRotation() + MathHelper.PiOver2;
                Vector2 fusePoint = handWorld + new Vector2(0f, -15f).RotatedBy(rotation);
                Projectiles.Enemy.RedKnightVFX.DrawBombFuse(fusePoint,
                    specialAttacks.TelegraphProgress, planted: false);
                spriteBatch.Draw(bombTexture, handWorld - Main.screenPosition, null, drawColor,
                    rotation, BombGripOrigin, 1f, SpriteEffects.None, 0f);
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
                return;
            }

            if (heldProp == KnightHeldProp.Magic)
            {
                Vector2 magicBallWorld = CurrentMagicBallWorld();
                Projectiles.Enemy.RedKnightVFX.DrawToxicMotes(magicBallWorld, 3,
                    specialAttacks.TelegraphProgress, 20f);
                DrawArmOverlay(spriteBatch, drawColor, specialAttacks.Direction);
            }
        }
        #endregion



    }
}
