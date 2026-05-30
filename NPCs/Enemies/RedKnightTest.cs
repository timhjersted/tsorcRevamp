using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.EnemySpriteRendering;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class RedKnightTest : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/RedKnight";
        private const bool CanStandAndShoot = true;

        #region Defaults
        public int redKnightsSpearDamage = 20;
        public int redMagicDamage = 15;
        public int redKnightsGreatDamage = 18;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;
        NPCDespawnHandler despawnHandler;
        EnemySpriteRenderer spriteRenderer;
        EnemySpritePose visualPose = EnemySpritePose.Natural;
        int visualPoseTimer;
        int visualPoseDuration;
        int visualHeldItemType = -1;
        bool visualWeaponVisible;
        EnemyHeldItemStyle visualHeldItemStyle;
        string visualHeldTexturePath;

        static int SpearVisualItemType => ModContent.ItemType<SpearOfMage>();
        static int BombVisualItemType => ModContent.ItemType<Firebomb>();
        static int MagicVisualItemType => ModContent.ItemType<PoisonBombRune>();
        const string SpearVisualTexturePath = "tsorcRevamp/Projectiles/Enemy/EnemyForgottenPearlSpearProj";
        const string BombVisualTexturePath = "tsorcRevamp/Projectiles/Enemy/EnemyFirebomb";
        const string MagicVisualTexturePath = "tsorcRevamp/Projectiles/Enemy/EnemySpellAbyssPoisonStrikeBall";


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.TrailCacheLength[NPC.type] = 4; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 75;
            NPC.defense = 41;
            NPC.scale = 1.4f;
            NPC.lifeMax = 2500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 20000; // life / 1.25 in HM
            NPC.knockBackResist = 0.0f;
            NPC.lavaImmune = true;
            NPC.rarity =2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);
            spriteRenderer = new EnemySpriteRenderer(
                ModContent.ItemType<ShadowCloakPlateHelm>(),
                ModContent.ItemType<ShadowCloakPlateMail>(),
                ModContent.ItemType<ShadowCloakGreaves>());
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

            // Navigation tuning: smart pathfinding with above-average jumps + ledge routing
            redKnightGlobalNPC.NavigationTier = 0;
            redKnightGlobalNPC.MaxJumpPower = 10f;
            redKnightGlobalNPC.MaxJumpBoost = 6f;
            redKnightGlobalNPC.HaltAtLedge = CanStandAndShoot;
            // CanDoubleJump remains false for RedKnight

        }


        #endregion

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0;
        }
        #endregion

        #region Hit Logic
        // Hit logic is stored in GlobalNPC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, projectile.DamageType == DamageClass.Melee);
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

            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().HaltAtLedge = CanStandAndShoot;
            tsorcRevampAIs.FighterAI(NPC, 1, 0.05f, 0.2f, canTeleport: true, 10, false, null, 1000, 0.5f, 2.5f, lavaJumping: true, canDodgeroll: true);
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 2f);

            Vector2 targetPosition = Vector2.Zero;

            //Block firing and reset cooldowns if it's busy doing other things that it shouldn't be able to shoot during
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.TeleportCountdown > 0 || globalNPC.BoredTimer < 0 || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0)
            {
                bool inProtectedAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                          (NPC.ai[1] >= 300f && NPC.ai[1] <= 405f) ||
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
                NPC.ai[1]++;
                NPC.ai[2]++;
                NPC.knockBackResist = 0f;

                bool inActiveAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                       (NPC.ai[1] >= 300f && NPC.ai[1] <= 405f) ||
                                       (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f) ||
                                       (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);

                // Gate all projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                if (hasPlayerLOS && (NPC.ai[1] == 180f || NPC.ai[1] == 325f || NPC.ai[1] == 405f || NPC.ai[1] == 925f))
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
                    NPC.ai[1] = 200f;
                    NPC.netUpdate = true;
                }

                // Increment the frames since we stored the player's position
                framesSinceStoredPosition++;

                // Spear Attack: Get targetPosition and set NPC direction (the latter part is not working)
                if (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Use the stored player's position to calculate the targetPosition.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    NPC.direction = (targetPosition.X > NPC.Center.X) ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                }

                // Spear Telegraph
                if (NPC.ai[1] == 90f)
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
                    float spearProjectileSpeed = Main.rand.NextFloat(14, 16f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    speed.Y += Main.rand.NextFloat(-2f, 2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyForgottenPearlSpearProj>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
                    StartVisualPose(EnemySpritePose.ThrowRelease, SpearVisualItemType, 12, false, EnemyHeldItemStyle.Spear, SpearVisualTexturePath);

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
                    float spearProjectileSpeed = Main.rand.NextFloat(11, 13f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    speed.Y += Main.rand.NextFloat(-1f, 1f); //adds random variation from -1 to 2
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyForgottenPearlSpearProj>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
                    StartVisualPose(EnemySpritePose.ThrowRelease, SpearVisualItemType, 12, false, EnemyHeldItemStyle.Spear, SpearVisualTexturePath);

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

                // Poison Attack 1 Telegraph 
                // Part 1: Dusts
                if (NPC.ai[1] >= 225 && NPC.ai[1] <= 300)
                {
                    if (Main.rand.NextBool(2))
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Yellow, 2f);
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
                    StartVisualPose(EnemySpritePose.MagicRelease, MagicVisualItemType, 15, false);

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
                    StartVisualPose(EnemySpritePose.MagicRelease, MagicVisualItemType, 15, false);
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
                if (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    NPC.direction = (targetPosition.X > NPC.Center.X) ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                }

                // Bomb Telegraph
                if (NPC.ai[1] == 830f)
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
                if (NPC.ai[1] == 925f && NPC.Distance(player.Center) > 400 && hasPlayerLOS)
                {
                    float bombProjectileSpeed = 14f;

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    //speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                    StartVisualPose(EnemySpritePose.ThrowRelease, BombVisualItemType, 12, false, EnemyHeldItemStyle.Bomb, BombVisualTexturePath);

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
                    float bombProjectileSpeed = 9f;
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                    StartVisualPose(EnemySpritePose.ThrowRelease, BombVisualItemType, 12, false, EnemyHeldItemStyle.Bomb, BombVisualTexturePath);

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
                    StartVisualPose(EnemySpritePose.MagicRelease, MagicVisualItemType, 15, false);
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
                    StartVisualPose(EnemySpritePose.MagicRelease, MagicVisualItemType, 15, false);
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
                    StartVisualPose(EnemySpritePose.MagicRelease, MagicVisualItemType, 15, false);

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
                    if (closestPlayer != null && Collision.CanHit(NPC, closestPlayer))
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
            if (spriteRenderer == null)
            {
                spriteRenderer = new EnemySpriteRenderer(
                    ModContent.ItemType<ShadowCloakPlateHelm>(),
                    ModContent.ItemType<ShadowCloakPlateMail>(),
                    ModContent.ItemType<ShadowCloakGreaves>());
            }

            UpdateSpritePose();
            return spriteRenderer.Draw(NPC, spriteBatch, screenPos, lightColor);
        }

        private void StartVisualPose(EnemySpritePose pose, int heldItemType, int duration, bool weaponVisible = true, EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic, string heldTexturePath = null)
        {
            visualPose = pose;
            visualPoseTimer = duration;
            visualPoseDuration = duration;
            visualHeldItemType = heldItemType;
            visualWeaponVisible = weaponVisible;
            visualHeldItemStyle = heldItemStyle;
            visualHeldTexturePath = heldTexturePath;
        }

        private void UpdateSpritePose()
        {
            EnemySpritePose pose = EnemySpritePose.Natural;
            int heldItemType = -1;
            bool weaponVisible = false;
            int poseTimer = 0;
            int poseDuration = 1;
            EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic;
            string heldTexturePath = null;

            float? targetDrawRotation = GetHeldSpearDrawRotation();

            if (visualPoseTimer > 0)
            {
                pose = visualPose;
                heldItemType = visualHeldItemType;
                weaponVisible = visualWeaponVisible;
                poseTimer = visualPoseTimer;
                poseDuration = visualPoseDuration;
                heldItemStyle = visualHeldItemStyle;
                heldTexturePath = visualHeldTexturePath;
                visualPoseTimer--;
            }
            else if (NPC.ai[1] >= 90f && NPC.ai[1] < 120f)
            {
                pose = EnemySpritePose.Carry;
                heldItemType = SpearVisualItemType;
                weaponVisible = true;
                heldItemStyle = EnemyHeldItemStyle.Spear;
                heldTexturePath = SpearVisualTexturePath;
            }
            else if (NPC.ai[1] >= 120f && NPC.ai[1] < 180f)
            {
                pose = EnemySpritePose.ThrowTelegraph;
                heldItemType = SpearVisualItemType;
                weaponVisible = true;
                poseTimer = (int)(180f - NPC.ai[1]);
                poseDuration = 60;
                heldItemStyle = EnemyHeldItemStyle.Spear;
                heldTexturePath = SpearVisualTexturePath;
            }
            else if (NPC.ai[1] >= 830f && NPC.ai[1] < 865f)
            {
                pose = EnemySpritePose.Carry;
                heldItemType = BombVisualItemType;
                weaponVisible = true;
                heldItemStyle = EnemyHeldItemStyle.Bomb;
                heldTexturePath = BombVisualTexturePath;
            }
            else if (NPC.ai[1] >= 865f && NPC.ai[1] < 925f)
            {
                pose = EnemySpritePose.ThrowTelegraph;
                heldItemType = BombVisualItemType;
                weaponVisible = true;
                poseTimer = (int)(925f - NPC.ai[1]);
                poseDuration = 60;
                heldItemStyle = EnemyHeldItemStyle.Bomb;
                heldTexturePath = BombVisualTexturePath;
            }
            else if ((NPC.ai[1] >= 225f && NPC.ai[1] < 325f) || (NPC.ai[1] >= 375f && NPC.ai[1] < 405f) ||
                     (NPC.ai[2] >= 72f && NPC.ai[2] < 100f) || (NPC.ai[2] >= 522f && NPC.ai[2] < 600f) ||
                     (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] < 200f))
            {
                pose = NPC.ai[1] >= 300f || NPC.ai[2] >= 75f ? EnemySpritePose.MagicAim : EnemySpritePose.MagicTelegraph;
                heldItemType = MagicVisualItemType;
                weaponVisible = true;
                poseTimer = 30;
                poseDuration = 60;
                heldItemStyle = EnemyHeldItemStyle.MagicBall;
                heldTexturePath = MagicVisualTexturePath;
                SpawnMagicTelegraphDust(pose == EnemySpritePose.MagicAim);
            }

            spriteRenderer.SetPose(pose, heldItemType, weaponVisible, poseTimer, poseDuration, heldItemStyle, heldTexturePath,
                heldItemStyle == EnemyHeldItemStyle.Spear ? targetDrawRotation : null);
        }

        private float? GetHeldSpearDrawRotation()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return null;
            }

            Player player = Main.player[NPC.target];
            if (!player.active || player.dead)
            {
                return null;
            }

            Vector2 target = storedPlayerPosition == Vector2.Zero ? player.Center : storedPlayerPosition;
            float speed = NPC.Distance(player.Center) > 400f ? 15f : 12f;
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, target, speed, fallback: true);
            if (velocity == Vector2.Zero)
            {
                velocity = target - NPC.Center;
            }

            if (velocity == Vector2.Zero)
            {
                return null;
            }

            return velocity.ToRotation() + MathHelper.PiOver2;
        }

        private void SpawnMagicTelegraphDust(bool aiming)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }

            Vector2 handOffset = aiming ? new Vector2(4f * NPC.direction, 2f) : new Vector2(4f * NPC.direction, -8f);
            Vector2 dustPosition = NPC.Center + handOffset - new Vector2(4f);
            int dust = Dust.NewDust(dustPosition, 8, 8, DustID.CursedTorch, Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.8f, 0.2f), 80, Color.GreenYellow, Main.rand.NextFloat(0.7f, 1.1f));
            Main.dust[dust].noGravity = true;
        }
        #endregion

    }

}


