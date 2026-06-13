using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.NPCs.Enemies
{
    class BlackKnight : ModNPC
    {
        public int redKnightsSpearDamage = 15;
        public int redMagicDamage = 14;
        public int redKnightsGreatDamage = 18;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;


        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 50;
            NPC.defense = 21;
            NPC.lifeMax = 1000;
            NPC.value = 5000;

            if (Main.hardMode)
            {
                NPC.lifeMax = 2000;
                NPC.damage = 65;
                NPC.defense = 50;
                NPC.value = 16000; // life / 1.25
                redKnightsGreatDamage = 21;
                redKnightsSpearDamage = 23;
                redMagicDamage = 19;
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

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;

            NPC.knockBackResist = 0.25f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.14 of full knockback per ordinary hit
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();
            //UsefulFunctions.AddAttack(NPC, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 9, SoundID.Item17);

            tsorcRevampGlobalNPC blackKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            blackKnightGlobalNPC.Agility = 0.5f;
            blackKnightGlobalNPC.Aggression = 1f;
            blackKnightGlobalNPC.Patience = 2f;

            // Poise: lighter than the Red Knight — staggers a bit more readily. Tunable lever.
            blackKnightGlobalNPC.PoiseMax = 24f;
            blackKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Navigation tuning: high jumps, double jump, and ledge routing
            blackKnightGlobalNPC.MaxJumpPower = 11f;
            blackKnightGlobalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            blackKnightGlobalNPC.CanUseRopes = true;
            blackKnightGlobalNPC.MaxJumpBoost = 7f;
            blackKnightGlobalNPC.CanDoubleJump = true;
            blackKnightGlobalNPC.DoubleJumpPower = 7f;
        }
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (!spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;
            if (Main.hardMode && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneBeach && !Main.dayTime && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneUnderworldHeight && !Main.dayTime && Main.rand.NextBool(160)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;

            return 0;
        }
        #endregion

        #region Hit Logic
        // Hit logic is stored in GlobalNPC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterEvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterEvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, enragePercent: 0.5f, enrageTopSpeed: 2.9f, canTeleport: true, canDodgeroll: true);
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 2f);

            //if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150f && NPC.justHit)
            //{
            //    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 100f; // reset throw countdown when hit, was 150
            //}

            Vector2 targetPosition = Vector2.Zero;

            // Block firing and reset cooldowns if it's busy doing other things that it shouldn't be able to shoot during
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Hyper-armor window: FLASH telegraph → fire only. Windup excluded so a stagger can interrupt it.
            // Spear 155→180, poison 300→375, bomb 900→925.
            globalNPC.AttackCommitted = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                        (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                        (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f);

            // Windup (~30t before each flash): poise can still break here, but the evasive on-hit reaction is suppressed.
            globalNPC.AttackTelegraphing = (NPC.ai[1] >= 125f && NPC.ai[1] < 155f) ||
                                           (NPC.ai[1] >= 270f && NPC.ai[1] < 300f) ||
                                           (NPC.ai[1] >= 870f && NPC.ai[1] < 900f);

            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                bool inProtectedAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                          (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                          (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f) ||
                                          (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);
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
                NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch

                bool inActiveAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                       (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                       (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f) ||
                                       (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);

                // Gate all projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

                #region Sounds & Jumps
                // Play creature sounds
                if (Main.rand.NextBool(1500))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
                }
                // Chance to jump forward
                if (NPC.Distance(player.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(300) && (NPC.ai[1] <= 150f || NPC.ai[1] >= 476f))
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
                    NPC.ai[1] = 200f;
                    NPC.netUpdate = true;
                }

                // Increment the frames since we stored the player's position
                framesSinceStoredPosition++;

                #region Spear Attack
                // Spear Attack: Get targetPosition 
                if (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Use the stored player's position from 25 frames ago to calculate the targetPosition.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);
                }

                // Spear Telegraph
                if (NPC.ai[1] == 155f)
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
                if (NPC.ai[1] == 180f && NPC.Distance(player.Center) > 400 && hasPlayerLOS)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(16, 18f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    //speed.Y += Main.rand.NextFloat(-2f, 2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 200f;

                    // Chance to fire Spear again
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 90f;
                        NPC.netUpdate = true;
                    }
                }
                // Spear Attack Close
                if (NPC.ai[1] == 180f && NPC.Distance(player.Center) <= 400 && hasPlayerLOS)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(12, 14f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    //speed.Y += Main.rand.NextFloat(-1f, 1f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 200f;

                    // Chance to fire Spear again
                    if (Main.rand.NextBool(3))
                    {
                        NPC.ai[1] = 90f;
                        NPC.netUpdate = true;
                    }
                }
                #endregion

                #region Homing Attack
                // Poison Attack 1 Telegraph 
                // Part 1: Dusts
                if (NPC.ai[1] >= 265 && NPC.ai[1] <= 325)
                {
                    if (Main.rand.NextBool(2))
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, DustID.ShadowbeamStaff, Color.DarkSlateGray, 0.5f);
                        Main.dust[dust2].noGravity = true;
                    }
                }

                // Part 2: Flash 
                if (NPC.ai[1] == 300)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Orange));
                    }
                }

                // Homing Attack 1
                if (NPC.ai[1] == 325 && hasPlayerLOS)
                {
                    float projectileSpeed = 15f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 1; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 2.1f, highAngle: true, fallback: true);
                        speed2 += Main.player[NPC.target].velocity; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    // Reset the targetPosition
                    targetPosition = Vector2.Zero;

                }

                // Homing Attack 2 Telegraph
                if (NPC.ai[1] == 350)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Orange));
                    }
                }

                // Homing Attack 2
                if (NPC.ai[1] == 375 && hasPlayerLOS)
                {
                    float projectileSpeed = 18f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 2; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                        //speed2 += Main.player[NPC.target].velocity / 2; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

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
                #endregion

                #region Bomb Attack
                // Code for Bomb Telegraph & Attack: 
                if (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y); //trying + 10 again

                }

                // Bomb Telegraph
                if (NPC.ai[1] == 900f)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);
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
                // Bomb Attack Far
                if (NPC.ai[1] == 925f && NPC.Distance(player.Center) > 400 && hasPlayerLOS)
                {
                    float bombProjectileSpeed = 8f;

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    //speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
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
                if (NPC.ai[1] == 925f && NPC.Distance(player.Center) <= 400 && hasPlayerLOS)
                {
                    float bombProjectileSpeed = 5f;
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    NPC.ai[1] = 0f;

                    // Chance to throw again
                    if (Main.rand.NextBool(2))
                    {
                        NPC.ai[1] = 830f;
                        NPC.netUpdate = true;
                    }
                }
                #endregion

                #region AI 2 Attacks
                // Air attack targeting indicator — shadow dust appears above drop zone 3 frames before each wave
                if ((NPC.ai[2] == 72 || NPC.ai[2] == 97 || NPC.ai[2] == 522 || NPC.ai[2] == 547 || NPC.ai[2] == 572 || NPC.ai[2] == 597) && !inActiveAttack && NPC.Distance(player.Center) > 250 && hasPlayerLOS)
                {
                    for (int i = 0; i < 6; i++)
                        Dust.NewDust(new Vector2(player.position.X - 10 + Main.rand.Next(player.width + 20), player.position.Y - 290f), 4, 4, DustID.ShadowbeamStaff, 0f, 3f, 100, default, 1.2f);
                }

                // Death Attack from Air
                if ((NPC.ai[2] == 75 || NPC.ai[2] == 525 || NPC.ai[2] == 575) && !inActiveAttack && NPC.Distance(player.Center) > 250 && hasPlayerLOS)
                {
                    for (int pcy = 0; pcy < 4; pcy++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X, (float)player.position.Y - 300f, (float)(-100 + Main.rand.Next(100)) / 10, 5.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redMagicDamage, 3f, Main.myPlayer);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    NPC.netUpdate = true;
                }

                // Slightly Delayed Death Attack From Air
                if ((NPC.ai[2] == 100 || NPC.ai[2] == 550 || NPC.ai[2] == 600) && !inActiveAttack && NPC.Distance(player.Center) > 270 && hasPlayerLOS)
                {
                    for (int pcy = 0; pcy < 6; pcy++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 500 + Main.rand.Next(1000), (float)player.position.Y - 300f, (float)(Main.rand.Next(10)) / 10, 3.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redMagicDamage, 4f, Main.myPlayer);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    NPC.netUpdate = true;
                }

                // Breather before reset
                if (NPC.ai[2] >= 1200)
                {
                    NPC.ai[2] = 0;
                }

                // Ultrakill Telegraph: Shrinking Dust Circle
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
                {
                    NPC.knockBackResist = 0f;
                    NPC.ai[1] = -130;
                    UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((200 - NPC.ai[2]) / 20)), DustID.BoneTorch, 48, 4);
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
                // Ultrakill Attack
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 200f && NPC.ai[2] <= 235f)
                {
                    NPC.velocity.X *= 0.25f;

                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Set targetPosition with an offset of 10f * direction units from the storedPlayerPosition along the X-axis.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    // Death Skulls
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2f, fallback: true);
                    speed += Main.rand.NextVector2Circular(1, 5);//was -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center); //Play flame sound

                    // Black Breath
                    Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2, fallback: true);
                    speed2 += Main.rand.NextVector2Circular(-5, 5);//was -4, -2, then -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
                    NPC.netUpdate = true;
                }
                // After Ultrakill attack completes
                if (NPC.ai[2] == 246f)
                {
                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;
                }

                #endregion

                // Shadow Crystal Storm — Phase 3 at 1/3 health
                // Telegraph: shadow dust spirals inward for 20 frames before the burst
                if (NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 420 >= 400 && Main.rand.NextBool(2))
                {
                    Vector2 dustOffset = Main.rand.NextVector2Circular(64, 64);
                    int dustIdx = Dust.NewDust(NPC.Center + dustOffset - new Vector2(4), 8, 8, DustID.ShadowbeamStaff,
                        -dustOffset.X * 0.1f, -dustOffset.Y * 0.1f, 150, default, 1.2f);
                    Main.dust[dustIdx].noGravity = true;
                }
                // Attack: 5 homing crystals in a 60° spread
                if (NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 420 == 0 && Main.netMode != NetmodeID.MultiplayerClient && hasPlayerLOS)
                {
                    int numCrystals = 5;
                    float totalSpread = MathHelper.Pi / 3f; // 60 degrees total
                    for (int i = 0; i < numCrystals; i++)
                    {
                        float angle = (i - (numCrystals - 1) / 2f) * (totalSpread / (numCrystals - 1));
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 10f, fallback: true);
                        speed += player.velocity / 2f;
                        speed = speed.RotatedBy(angle);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 1f, Pitch = -0.3f, PitchVariance = 0.2f }, NPC.Center);
                    NPC.netUpdate = true;
                }

            }
        }


        /*
        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightGhostSpear");
            }
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTelegraphStart)
            {
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; 
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, spearTexture.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }
        */
        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600, false);
            target.AddBuff(36, 600, false); //broken armor
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
        }
        #endregion

        #region Draw Attack Sprites
        static Texture2D spearTexture;
        static Texture2D bombTexture;
        static Texture2D handTexture;
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 30), // 0 idle
            new Vector2(47, 25), // 1 jump
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
            new Vector2(45, 32), // 13
            new Vector2(48, 33), // 14
            new Vector2(48, 33), // 15
        };
        static readonly Vector2 SpearGripOrigin = new Vector2(8f, 38f);
        static readonly Vector2 BombGripOrigin = new Vector2(14f, 4f);

        Vector2 CurrentHandWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= HandPixel.Length)
            {
                frame = 0;
            }

            Vector2 fp = HandPixel[frame];
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -NPC.spriteDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
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

        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            if (handTexture == null)
            {
                return;
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(handTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackThrowingSpear");
            }

            if (bombTexture == null || bombTexture.IsDisposed)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyMoonfuryBomb");
            }

            if (handTexture == null || handTexture.IsDisposed)
            {
                handTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/BlackKnight_Hand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            // Spear
            if (NPC.ai[1] >= 120 && NPC.ai[1] <= 180f)
            {
                float spriteScale = 0.8f;
                Vector2 spearAim = NPC.ai[1] >= 155f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentSpearWorld() - Main.screenPosition;
                spriteBatch.Draw(spearTexture, handWorld, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, SpearGripOrigin, NPC.scale * spriteScale, SpriteEffects.None, 0);
                DrawHandOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (NPC.ai[1] >= 865)
            {
                Vector2 bombAim = NPC.ai[1] >= 900f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                spriteBatch.Draw(bombTexture, handWorld, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, rotation, BombGripOrigin, NPC.scale, SpriteEffects.None, 0);
                DrawHandOverlay(spriteBatch, drawColor);
            }

        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 2f;
                velY *= 2f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 2, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 1));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Weapons.Throwing.ThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Weapons.Throwing.RoyalThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Mobility.BootsOfHaste>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Spears.AncientDragonLance>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Spears.OldHalberd>(), 5));
            npcLoot.Add(new CommonDrop(ItemID.IronskinPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.ArcheryPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 5, 1, 50, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 1, 0, 1));
        }
    }
}
