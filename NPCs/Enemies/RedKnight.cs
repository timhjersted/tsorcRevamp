using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class RedKnight : ModNPC
    {
        #region Defaults
        public int redKnightsSpearDamage = 20;
        public int redMagicDamage = 15;
        public int redKnightsGreatDamage = 18;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;
        NPCDespawnHandler despawnHandler;


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.TrailCacheLength[NPC.type] = 4; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 75;
            NPC.defense = 41;
            NPC.scale = 1.1f;
            NPC.lifeMax = 2500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 20000; // life / 1.25 in HM
            NPC.knockBackResist = 0.3f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.12 of full knockback per ordinary hit
            NPC.lavaImmune = true;
            NPC.rarity =2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);
            //UsefulFunctions.AddAttack(NPC, 800, ModContent.ProjectileType<Projectiles.Enemy.EnemyForgottenPearlSpearProj>(), redKnightsSpearDamage, 13, SoundID.Item17);

            if (!Main.hardMode)
            {
                NPC.defense = 14;
                NPC.value = 12500;
                NPC.damage = 40;
                redKnightsGreatDamage = 9;
                redKnightsSpearDamage = 12;
                redMagicDamage = 7;
                NPC.boss = true;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 4000;
                NPC.defense = 75;
                NPC.damage = 120;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 38;
                redKnightsSpearDamage = 32;
                redMagicDamage = 26;
            }
            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            //redKnightGlobalNPC.Aggression = 2.5f;
            //redKnightGlobalNPC.Patience = 2;
            //redKnightGlobalNPC.Cowardice = 0f;
            //redKnightGlobalNPC.Adeptness = 0.3f;
            //redKnightGlobalNPC.Swiftness = 1.3f;
            //redKnightGlobalNPC.CastingSpeed = Main.rand.NextFloat(0.6f, 1.4f);
            //redKnightGlobalNPC.Strength = Main.rand.NextFloat(0.7f, 1.4f);

            redKnightGlobalNPC.Agility = 0.3f;

            // Poise: needs sustained pressure to stagger (poise damage = weapon knockback). Tunable lever.
            redKnightGlobalNPC.PoiseMax = 40f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Navigation tuning: smart pathfinding with above-average jumps + ledge routing
            redKnightGlobalNPC.MaxJumpPower = 12f;
            redKnightGlobalNPC.NavSearchRadius = 80;
            redKnightGlobalNPC.CanUseRopes = true;
            redKnightGlobalNPC.MaxJumpBoost = 6f;
            redKnightGlobalNPC.NavGiveUpTicks = 200;
            // CanDoubleJump remains false for RedKnight
            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;

            // Evasive on-hit: hop/leap/dash away, or blink away when able (TeleportAway uses CanTeleport above).
            EvasiveProfile.RedKnight(redKnightGlobalNPC);
        }


        #endregion

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (Main.hardMode && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && Main.rand.NextBool(1200)) return 1;

            if (Main.hardMode && P.ZoneMeteor && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.NextBool(1250)) return 1;

            if (Main.hardMode && !Main.dayTime && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.NextBool(1350)) return 1;

            if (Main.hardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(1100)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.NextBool(500)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(300)) return 1;

            return 0;
        }
        #endregion

        #region Hit Logic
        // Hit logic is stored in GlobalNPC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }
        #endregion

        #region AI
        public override void AI()
        {
            if (!Main.hardMode)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }

            tsorcRevampAIs.FighterAI(NPC, 1, 0.05f, 0.2f, canTeleport: true, 10, false, null, 1000, 0.5f, 2.5f, lavaJumping: true, canDodgeroll: true);
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 2f);

            Vector2 targetPosition = Vector2.Zero;

            //Block firing and reset cooldowns if it's busy doing other things that it shouldn't be able to shoot during
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Hyper-armor window: FLASH telegraph → fire only. Windup is intentionally excluded so a poise stagger can
            // interrupt during windup; once the flash fires the attack is uninterruptible (only the stagger breaks it).
            // Spear 180→210, poison1 300→325, poison2 375→405, bomb 925→955.
            globalNPC.AttackCommitted = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                        (NPC.ai[1] >= 300f && NPC.ai[1] <= 325f) ||
                                        (NPC.ai[1] >= 375f && NPC.ai[1] <= 405f) ||
                                        (NPC.ai[1] >= 925f && NPC.ai[1] <= 955f);

            // Windup (~30t before each flash): poise can still break here, but the evasive on-hit reaction is suppressed.
            globalNPC.AttackTelegraphing = (NPC.ai[1] >= 150f && NPC.ai[1] < 180f) ||
                                           (NPC.ai[1] >= 270f && NPC.ai[1] < 300f) ||
                                           (NPC.ai[1] >= 345f && NPC.ai[1] < 375f) ||
                                           (NPC.ai[1] >= 895f && NPC.ai[1] < 925f);
            if (globalNPC.TeleportCountdown > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                bool inProtectedAttack = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                          (NPC.ai[1] >= 300f && NPC.ai[1] <= 405f) ||
                                          (NPC.ai[1] >= 925f && NPC.ai[1] <= 955f) ||
                                          (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);
                if (!inProtectedAttack)
                {
                    NPC.ai[1] = 60f;
                    NPC.ai[2] = -100f;
                }
            }

            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                // Freeze the attack timer while staggered so the ~1s stun actually holds (and a windup that was reset to
                // 60 by the stagger stays neutral instead of advancing back into an attack).
                if (globalNPC.StaggerTimer <= 0)
                {
                    NPC.ai[1]++;
                    NPC.ai[2]++;
                }
                NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch

                bool inActiveAttack = (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f) ||
                                       (NPC.ai[1] >= 300f && NPC.ai[1] <= 405f) ||
                                       (NPC.ai[1] >= 925f && NPC.ai[1] <= 955f) ||
                                       (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);

                // Gate all projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                if (hasPlayerLOS && (NPC.ai[1] == 210f || NPC.ai[1] == 325f || NPC.ai[1] == 405f || NPC.ai[1] == 955f))
                {
                    tsorcRevampAIs.RegisterFighterAttack(NPC);
                }

                #region Sounds & Jumps
                // Play creature sounds
                if (Main.rand.NextBool(1500))
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

                // Spear Attack: Get targetPosition and set NPC direction
                if (NPC.ai[1] >= 180f && NPC.ai[1] <= 210f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    Vector2 currentStoredPos = storedPlayerPosition == Vector2.Zero ? player.Center : storedPlayerPosition;
                    int direction = (currentStoredPos.X > NPC.Center.X) ? 1 : -1;

                    // Use the stored player's position to calculate the targetPosition.
                    targetPosition = new Vector2(currentStoredPos.X + 10f * direction, currentStoredPos.Y);

                    NPC.direction = (targetPosition.X > NPC.Center.X) ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                }

                // Spear Telegraph
                if (NPC.ai[1] == 180f)
                {
                    NPC.TargetClosest(true);
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
                    int targetPlayer = NPC.target;
                    if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                    {
                        storedPlayerPosition = Main.player[targetPlayer].Center;
                    }
                }

                // Spear Attack
                if (NPC.ai[1] == 210f)
                {
                    if (hasPlayerLOS)
                    {
                        float distance = NPC.Distance(player.Center);
                        if (distance > 400f)
                        {
                            float spearProjectileSpeed = Main.rand.NextFloat(14, 16f);
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                            speed.Y += Main.rand.NextFloat(-2f, 2f);
                            speed += Main.player[NPC.target].velocity;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                            }
                        }
                        else
                        {
                            float spearProjectileSpeed = Main.rand.NextFloat(11, 13f);
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                            speed.Y += Main.rand.NextFloat(-1f, 1f);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                            }
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
                    }

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
                    float projectileSpeed = 5f;
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
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    // Reset the targetPosition
                    targetPosition = Vector2.Zero;

                }

                // Poison Attack 2 Telegraph
                if (NPC.ai[1] == 375)
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
                if (NPC.ai[1] == 405 && hasPlayerLOS)
                {
                    float projectileSpeed = 4f;
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
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    // Shorter or longer pause before bomb attack
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 800f;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.ai[1] = 700f;
                        NPC.netUpdate = true;
                    }
                }

                // Code for Bomb Telegraph & Attack: 
                if (NPC.ai[1] >= 925f && NPC.ai[1] <= 955f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    Vector2 currentStoredPos = storedPlayerPosition == Vector2.Zero ? player.Center : storedPlayerPosition;
                    int direction = (currentStoredPos.X > NPC.Center.X) ? 1 : -1;

                    targetPosition = new Vector2(currentStoredPos.X + 10f * direction, currentStoredPos.Y);

                    NPC.direction = (targetPosition.X > NPC.Center.X) ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                }

                // Bomb Telegraph
                if (NPC.ai[1] == 925f)
                {
                    NPC.TargetClosest(true);
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
                    int targetPlayer = NPC.target;
                    if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                    {
                        storedPlayerPosition = Main.player[targetPlayer].Center;
                    }
                }

                // Bomb Attack
                if (NPC.ai[1] == 955f)
                {
                    if (hasPlayerLOS)
                    {
                        float distance = NPC.Distance(player.Center);
                        if (distance > 400f)
                        {
                            float bombProjectileSpeed = 14f;
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);
                            speed += Main.player[NPC.target].velocity;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                            }
                        }
                        else
                        {
                            float bombProjectileSpeed = 9f;
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);
                            speed.Y += Main.rand.NextFloat(-1f, -2f);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                            }
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                    }

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    // Reset attack counter
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
                if ((NPC.ai[2] == 72 || NPC.ai[2] == 97 || NPC.ai[2] == 522 || NPC.ai[2] == 547 || NPC.ai[2] == 572 || NPC.ai[2] == 597) && !inActiveAttack && NPC.Distance(player.Center) > 350 && hasPlayerLOS)
                {
                    for (int i = 0; i < 6; i++)
                        Dust.NewDust(new Vector2(player.position.X - 10 + Main.rand.Next(player.width + 20), player.position.Y - 340f), 4, 4, DustID.Torch, 0f, 3f, 100, default, 1.2f);
                }

                // Fire Attack from Air
                if ((NPC.ai[2] == 75 || NPC.ai[2] == 525 || NPC.ai[2] == 575) && !inActiveAttack && NPC.Distance(player.Center) > 350 && hasPlayerLOS)
                {
                    for (int pcy = 0; pcy < 3; pcy++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X, (float)player.position.Y - 360f, (float)(-100 + Main.rand.Next(100)) / 10, 5.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 1f, Main.myPlayer);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                }

                // Slightly Delayed Fire Attack From Air
                if ((NPC.ai[2] == 100 || NPC.ai[2] == 550 || NPC.ai[2] == 600) && !inActiveAttack && NPC.Distance(player.Center) > 370 && hasPlayerLOS)
                {
                    for (int pcy = 0; pcy < 4; pcy++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 400 + Main.rand.Next(800), (float)player.position.Y - 300f, (float)(Main.rand.Next(10)) / 10, 1.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                }

                // Breather before reset
                if (NPC.ai[2] >= 1100)
                {
                    NPC.ai[2] = 0;
                }

                // Ultrakill Telegraph: Shrinking Dust Circle
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
                {
                    NPC.knockBackResist = 0f;
                    NPC.ai[1] = -130;
                    UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((200 - NPC.ai[2]) / 20)), DustID.Torch, 48, 4);
                    Lighting.AddLight(NPC.Center * 2, Color.WhiteSmoke.ToVector3() * 5);
                    NPC.velocity.X *= 0.85f;
                }
                // Ultrakill Telegraph: Flash
                if (NPC.ai[2] == 165f)
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
                // Ultrakill Attack
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 200f && NPC.ai[2] <= 235f)
                {
                    NPC.velocity.X *= 0.25f;

                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Set targetPosition with an offset of 10f * direction units from the storedPlayerPosition along the X-axis.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    // Exlosives
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 10, fallback: true);
                    speed += Main.rand.NextVector2Circular(-6, -8);//was -4, -2, then -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center); //Play flame sound

                    // Insanity Hands
                    Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 6, fallback: true);
                    speed2 += Main.rand.NextVector2Circular(-4, 4);//was -4, -2, then -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ProjectileID.InsanityShadowHostile, redKnightsGreatDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);
                }
                // After Ultrakill attack completes
                if (NPC.ai[2] == 236f)
                {
                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;
                }

                #endregion

                // Jellyfish Lightning Attack at 1/3 life
                if (NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 500 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player closestPlayer = UsefulFunctions.GetClosestPlayer(NPC.Center);
                    if (closestPlayer != null)
                    {
                        Vector2 targetVector = UsefulFunctions.Aim(NPC.Center, closestPlayer.Center, 1);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.SmallRedLightning>(), 30, 1, Main.myPlayer, 0, NPC.whoAmI);
                        }
                    }
                }

            }
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        #region Loot
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {

            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.RegenerationPotion, 30));
            npcLoot.Add(hmCondition);
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 20);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop);
            shmCondition.OnSuccess(drop2);
            npcLoot.Add(shmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<Items.StaminaVessel>()));
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false); // loss of flight mobility
        }
        #endregion

        #region PreDraw
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
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

        // Held weapons anchor to the knight's gripping hand, tracked per animation frame (70x56 sheet, raw art faces LEFT).
        // frame 0 = idle, frame 1 = jump (hands up by the head), frames 2-15 = walk cycle. Tune these to the sheet.
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 42), // 0 idle
            new Vector2(33, 12), // 1 jump — hands raised near the head
            new Vector2(40, 35), // 2
            new Vector2(40, 35), // 3
            new Vector2(40, 35), // 4
            new Vector2(40, 35), // 5
            new Vector2(41, 35), // 6
            new Vector2(41, 35), // 7
            new Vector2(42, 35), // 8
            new Vector2(42, 35), // 9
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

        // Global correction if the whole overlay is consistently off by a few px (tune once).
        static readonly Vector2 OverlayFudge = new Vector2(0f, 0f);
        static readonly Vector2 SpearGripOrigin = new Vector2(7f, 31f); // BlackKnightSpear (Valkyrie's spear) is 14x62, tip up — grip the MIDDLE (was 7,70 = the butt of the old 14x84 spear)
        static readonly Vector2 BombGripOrigin = new Vector2(11f, 18f); // EnemyFirebomb is 22x24, hand near the bottom
        static readonly Vector2 MagicBallGripOrigin = new Vector2(8f, 8f);
        const float MagicBallBodyInset = 8f;

        // World position of the body's gripping hand for placing held weapons under the arm overlay.
        Vector2 CurrentHandWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= OverlayHandPixel.Length)
            {
                frame = 0;
            }
            Vector2 fp = OverlayHandPixel[frame];
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -NPC.spriteDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y) + OverlayFudge;
        }

        Vector2 CurrentMagicBallWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            float bodyDirection = Math.Sign(NPC.Center.X - handWorld.X);
            return handWorld + new Vector2(bodyDirection * MagicBallBodyInset, 0f);
        }

        Vector2 CurrentSpearWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame == 0)
            {
                handWorld.Y -= 21f;
            }
            return handWorld;
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            if (armOverlayTexture == null)
            {
                return;
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(armOverlayTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightSpear"); // the spear Tibian Valkyrie uses (14x62)
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

            // Spear (held during telegraph, aimed at the throw target)
            if (spearTexture != null && NPC.ai[1] >= 120 && NPC.ai[1] <= 210f)
            {
                Vector2 handWorld = CurrentSpearWorld() - Main.screenPosition;
                Vector2 spearAim = NPC.ai[1] >= 180f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;

                // Weapon behind the hand, pivoting on the grip so it aims at the throw target.
                spriteBatch.Draw(spearTexture, handWorld, null, drawColor, rotation, SpearGripOrigin, NPC.scale, SpriteEffects.None, 0);
                DrawArmOverlay(spriteBatch, drawColor);
            }
            // Magic ball
            if (magicBallTexture != null && ((NPC.ai[1] >= 225 && NPC.ai[1] <= 325f) || (NPC.ai[1] >= 375 && NPC.ai[1] <= 405f)))
            {
                Vector2 magicBallWorld = CurrentMagicBallWorld();
                spriteBatch.Draw(magicBallTexture, magicBallWorld - Main.screenPosition, null, drawColor, 0f, MagicBallGripOrigin, 1f, SpriteEffects.None, 0);
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(magicBallWorld + Main.rand.NextVector2Circular(6f, 6f), DustID.YellowTorch, Main.rand.NextVector2Circular(0.35f, 0.35f), 120, default, 1.3f);
                    dust.noGravity = true;
                }
                DrawArmOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (NPC.ai[1] >= 865)
            {
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                Vector2 bombAim = NPC.ai[1] >= 925f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;

                spriteBatch.Draw(bombTexture, handWorld, null, drawColor, rotation, BombGripOrigin, 1f, SpriteEffects.None, 0);
                DrawArmOverlay(spriteBatch, drawColor);
            }

        }
        #endregion
    }

}
