using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class RedKnight : ModNPC
    {
        public int redKnightsSpearDamage = 32;
        public int redMagicDamage = 22;
        public int redKnightsGreatDamage = 18;
        //Custom AI personality paramaters

        /// <summary>
        /// How likely it is to dash at the player if it is far away.
        /// Range: 0.00001 - 2.5
        /// </summary>
        public float Aggression;

        /// <summary>
        /// Controls how quickly it gets bored (and thus how long it waits before teleporting, if it has that ability).
        /// Range: 0.5 - 2
        /// </summary>
        public float Patience;

        /// <summary>
        /// How likely it is to try and run if it is low on health.
        /// Range: 0 - 0.3
        /// </summary>
        public float Cowardice;

        /// <summary>
        /// Improves the likelihood of performing low-weighted attacks.
        /// Range: 0 - 0.3
        /// </summary>
        public float Adeptness;

        /// <summary>
        /// Modifies movement speed and acceleration.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Swiftness;

        /// <summary>
        /// Modifies how often it fires projectiles.
        /// Range: 0.6 - 1.4
        /// </summary>
        public float CastingSpeed;

        /// <summary>
        /// Modifies base health, size, and contact damage.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Strength;

        /// <summary>
        /// Controls how often it tries to roll through or jumps over projectiles.
        /// Range: 0.2 - 0.6
        /// </summary>
        public float Agility;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 75;
            NPC.defense = 41;
            NPC.scale = 1.1f;
            NPC.lifeMax = 2500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 15110;
            NPC.knockBackResist = 0.06f;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);

            if (!Main.hardMode)
            {
                //npc.defense = 14;
                //npc.value = 3500;
                //npc.damage = 40;
                redKnightsGreatDamage = 10;
                redKnightsSpearDamage = 15;
                redMagicDamage = 8;
                NPC.boss = true;
            }

            //Aggression = 2.5f;
            Patience = 2;
            Cowardice = 0f;
            //Adeptness = 0.3f;
            //Swiftness = 1.3f;
            //CastingSpeed = Main.rand.NextFloat(0.6f, 1.4f);
            //Strength = Main.rand.NextFloat(0.7f, 1.4f);
            Agility = 0.6f;

            //UsefulFunctions.AddAttack(NPC, 340, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 8, SoundID.Item1 with { Volume = 0.2f, Pitch = -0.8f });
            //UsefulFunctions.AddAttack(NPC, 340, ModContent.ProjectileType<Projectiles.Enemy.EnemyForgottenPearlSpearProj>(), redKnightsSpearDamage, 8, SoundID.Item1 with { Volume = 0.2f, Pitch = -0.8f });
            //UsefulFunctions.AddAttack(NPC, 1240, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 8, weight: 0.3f);
            //UsefulFunctions.AddAttack(NPC, 1240, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), 1, 0, SoundID.Item17, needsLineOfSight: false, weight: 0.2f);
            //UsefulFunctions.AddAttack(NPC, 250, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonFieldBall>(), redMagicDamage, 8, weight: 0.3f);




        }

        NPCDespawnHandler despawnHandler;



        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (Main.hardMode && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && Main.rand.NextBool(1200)) return 1;

            if (Main.hardMode && P.ZoneMeteor && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.NextBool(250)) return 1;

            if (Main.hardMode && !Main.dayTime && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.NextBool(350)) return 1;

            if (Main.hardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(100)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.NextBool(5)) return 1; //30 was 1 percent, 10 is 2.76%

            if (tsorcRevampWorld.SuperHardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(3)) return 1;

            return 0;
        }
        #endregion

        //PROJECTILE HIT LOGIC
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

        public override void AI()
        {
            if (!Main.hardMode)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }

            tsorcRevampAIs.FighterAI(NPC, 1, 0.05f, 0.2f, canTeleport: true, 20, false, null, 1000, 0.5f, 3f, lavaJumping: true, canDodgeroll: true);
            //tsorcRevampAIs.LeapAtPlayer(NPC, 4, 3, 1, 100);
            //NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackList[2].ai1 = NPC.target; //Set the lightning ball it shoots to have an ai1 of the NPCs target, so it explodes when near it

            Lighting.AddLight(NPC.Center, Color.LightGreen.ToVector3() * 2f);

            //Jellyfish Lighting attack

            if (Main.GameUpdateCount % 6000 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player closestPlayer = UsefulFunctions.GetClosestPlayer(NPC.Center);
                if (closestPlayer != null && Collision.CanHit(NPC, closestPlayer))
                {
                    Vector2 targetVector = UsefulFunctions.Aim(NPC.Center, closestPlayer.Center, 1);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<Projectiles.Enemy.JellyfishLightning>(), 30, 1, Main.myPlayer, 0, NPC.whoAmI);
                }
            }

            //Reset spear and shot timer after teleporting
            //This prevents it from insta-hitting players
            if (NPC.ai[3] < 0)
            {
                NPC.localAI[1] = 0;
                NPC.localAI[2] = 0;
            }

            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                NPC.localAI[1]++;
                NPC.localAI[2]++;

                //play creature sounds
                if (Main.rand.NextBool(1500))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.5f }, NPC.Center);
                    //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
                }
                //CHANCE TO JUMP FORWARDS
                if (NPC.Distance(player.Center) > 160 && NPC.velocity.Y == 0f && Main.rand.NextBool(500) && NPC.localAI[1] <= 160f)
                {
                    NPC.velocity.Y = -6f; //9             
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;
                    if ((float)NPC.direction * NPC.velocity.X > 2)
                        NPC.velocity.X = (float)NPC.direction * 2;
                    NPC.netUpdate = true;
                }
                //CHANCE TO DASH STEP FORWARDS 
                if (NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(250) && NPC.localAI[1] <= 150f)
                {
                    NPC.velocity.Y = -4f;
                    NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                    if ((float)NPC.direction * NPC.velocity.X > 4)
                        NPC.velocity.X = (float)NPC.direction * 4;

                    //CHANCE TO JUMP AFTER DASH
                    if (Main.rand.NextBool(14) && NPC.localAI[1] <= 170f)
                    {
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f);
                        NPC.velocity.Y = -8f;
                        NPC.localAI[1] = 150f; //was 170;
                    }
                    NPC.netUpdate = true;
                }
                //OFFENSIVE JUMP
                if (NPC.localAI[1] == 135 && NPC.velocity.Y <= 0f && Main.rand.NextBool(5))//was 155
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f);

                    NPC.velocity.Y = -10f;
                    NPC.TargetClosest(true);
                    NPC.netUpdate = true;
                }
                //SPEAR TELEGRAPH
                if (NPC.localAI[1] == 155f)
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
                }
                //SPEAR ATTACK
                if (NPC.localAI[1] == 180f)
                {
                    NPC.TargetClosest(true);
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12, fallback: true);
                    speed += Main.rand.NextVector2Circular(-4, -2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyForgottenPearlSpearProj>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound

                    //go to poison attack
                    NPC.localAI[1] = 200f;

                    //or chance to throw again
                    if (Main.rand.NextBool(2))
                    {
                        NPC.localAI[1] = 100f;
                        NPC.netUpdate = true;
                    }
                }
                //POISON ATTACK TELEGRAPH 1
                if (NPC.localAI[1] == 300)
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
                //POISON ATTACK 1
                if (NPC.localAI[1] == 325)
                {
                    if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                    {
                        float projectileSpeed = 6f;
                        float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                        int numProjectiles = 4; // Number of projectiles to shoot

                        for (int i = 0; i < numProjectiles; i++)
                        {
                            float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                            speed2 += Main.player[NPC.target].velocity / 4;
                            speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                            if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                            NPC.localAI[1] = 326f;
                        }
                    }
                }
                //POISON ATTACK TELEGRAPH 2
                if (NPC.localAI[1] == 450)
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
                //POISON ATTACK 2
                if (NPC.localAI[1] == 475)
                {
                    NPC.TargetClosest(true);
                    if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                    {
                        float projectileSpeed = 6f;
                        float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                        int numProjectiles = 8; // Number of projectiles to shoot

                        for (int i = 0; i < numProjectiles; i++)
                        {
                            float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                            speed2 += Main.player[NPC.target].velocity / 4;
                            speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                            if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                            NPC.localAI[1] = 1f;
                        }
                    }
                }
                //FIRE ATTACK FROM THE AIR
                if ((NPC.localAI[2] == 75 || NPC.localAI[2] == 525 || NPC.localAI[2] == 575) && NPC.Distance(player.Center) > 150)
                {
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X, (float)player.position.Y - 360f, (float)(-100 + Main.rand.Next(100)) / 10, 5.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 1f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                        NPC.netUpdate = true;
                    }
                }
                //SLIGHTLY DELAYED FIRE ATTACK FROM THE AIR
                if ((NPC.localAI[2] == 100 || NPC.localAI[2] == 550 || NPC.localAI[2] == 600) && NPC.Distance(player.Center) > 150)
                {
                    for (int pcy = 0; pcy < 4; pcy++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 400 + Main.rand.Next(800), (float)player.position.Y - 300f, (float)(Main.rand.Next(10)) / 10, 1.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                        NPC.netUpdate = true;
                    }
                }
                //BOMB TELEGRAPH
                if (NPC.localAI[2] == 900f)
                {
                    if (NPC.localAI[1] <= 150)
                    {
                        NPC.localAI[1] = -50;
                    }
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

                }
                //BOMB ATTACK
                if (NPC.localAI[2] == 925f)
                {
                    //NPC.velocity.X = 0f;
                    /*
                    NPC.TargetClosest(true);
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10, fallback: true);
                    speed += Main.rand.NextVector2Circular(-4, -2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound
                    */

                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 8f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
                    }



                    NPC.localAI[2] = 950f;
                    //chance to throw again
                    //if (Main.rand.NextBool(2))
                    //{
                    //    NPC.localAI[2] = 850f;
                    //    NPC.netUpdate = true;
                    //}

                }

                //BREATHER BEFORE BIG ATTACK
                if (NPC.localAI[2] >= 1000)
                {
                    NPC.localAI[2] = 0;
                }
                //BIG ATTACK DUST CIRCLE TELEGRAPH
                if (NPC.localAI[2] >= 100f && NPC.localAI[2] <= 200f)
                {
                    NPC.localAI[1] = -100;
                    UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((200 - NPC.localAI[2]) / 20)), DustID.Torch, 48, 4);
                    Lighting.AddLight(NPC.Center * 2, Color.WhiteSmoke.ToVector3() * 5);
                    /*
                    NPC.TargetClosest(false);
                    if (NPC.direction == 1)
                    {
                        NPC.direction = 1;
                        NPC.velocity.X = 0;
                    }

                    if (NPC.direction == -1)
                    {
                        NPC.direction = -1;
                        NPC.velocity.X = 0;
                    }
                    */
                }
                //ULTRA KILL ATTACK
                if (NPC.localAI[2] >= 200f && NPC.localAI[2] <= 235f)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10, fallback: true);
                    speed += Main.rand.NextVector2Circular(-12, -16);//was -4, -2
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), redKnightsSpearDamage, 0f, Main.myPlayer); //enemygreatattack kinda interesting? - even cooler: EnemySpellGreatFireballBall
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center); //Play swing-throw sound
                    Lighting.AddLight(NPC.Center * 2, Color.WhiteSmoke.ToVector3() * 8);
                }
            }
        }
    



        //sound notes
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 42, 0.6f, 0f); //flaming wood, high pitched air going out
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 43, 0.6f, 0f); //staff magic cast, low sound
        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.6f, Pitch = 0.7f }, NPC.Center); //inferno fork, almost same as fire (works)
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 48, 0.6f, 0.7f); // mine snow, tick sound
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 60, 0.6f, 0.0f); //terra beam
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish

        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 81, 0.6f, 0f); //spawn slime mount, more like thunder flame burn
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 88, 0.6f, 0f); //meteor staff more bass and fire
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 100, 0.6f, 0f); // cursed flame wall, lasts a bit longer than flame
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 101, 0.6f, 0f); // crystal vilethorn - breaking crystal
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 103, 0.6f, 0f); //shadowflame hex (little beasty)
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 104, 0.6f, 0f); //shadowflame 
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 106, 0.6f, 0f); //flask throw tink sound
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 109, 0.6f, 0.0f); //crystal serpent fire
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 110, 0.6f, 0.0f); //crystal serpent split, paper, thud, faint high squeel
        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2

        //SPEAR ATTACK EXPERIMENT
        /*
        // Define the attack variations
        const int AttackNormal = 0;
        const int AttackTwoProjectiles = 1;
        const int AttackThreeSpears = 2;

        // Initialize the attack variation
        if (!NPC.HasValidTarget || NPC.localAI[1] == 0f)
        {
            NPC.localAI[1] = AttackNormal;
        }

        // Perform the current attack variation
        switch ((int)NPC.localAI[1])
        {
            case AttackNormal:
                // Normal spear attack
                NPC.TargetClosest(true);
                Vector2 speed1 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6, fallback: true);
                speed1 += Main.rand.NextVector2Circular(-4, -2);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed1.X, speed1.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound
                NPC.localAI[1] = AttackTwoProjectiles; // Move to the next attack variation
                break;

            case AttackTwoProjectiles:
                // Two projectiles variation
                NPC.TargetClosest(true);
                Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6, fallback: true);
                speed2 += Main.rand.NextVector2Circular(-4, -2);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Vector2 upwardsSpeed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center + new Vector2(0f, -400f), 12f, fallback: true);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, upwardsSpeed.X, upwardsSpeed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound
                NPC.localAI[1] = AttackThreeSpears; // Move to the next attack variation
                break;

            case AttackThreeSpears:
                // Three spears variation
                NPC.TargetClosest(true);
                Vector2 speed3 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6, fallback: true);
                speed3 += Main.rand.NextVector2Circular(-4, -2);
                float angle = MathHelper.PiOver4; // 45 degrees in radians
                Vector2 spearOffset = new Vector2(60f, 0f); // Distance between spears
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed3.X, speed3.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed3.X + spearOffset.X * (float)Math.Cos(angle), speed3.Y + spearOffset.Y * (float)Math.Sin(angle), ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed3.X - spearOffset.X * (float)Math.Cos(angle), speed3.Y + spearOffset.Y * (float)Math.Sin(angle), ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center); //Play swing-throw sound
                NPC.localAI[1] = AttackNormal; // Move back to the first attack variation
                break;
        }

        NPC.netUpdate = true;
        */

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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {

            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.RegenerationPotion, 30));
            npcLoot.Add(hmCondition);
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 20);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop);
            shmCondition.OnSuccess(drop2);
            npcLoot.Add(shmCondition);
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            player.AddBuff(BuffID.OnFire, 3 * 60, false);

            if (Main.rand.NextBool(5))
            {
                player.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false); // loss of flight mobility
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
                player.AddBuff(BuffID.OnFire, 60, false);

            }
        }
        #endregion

        static Texture2D spearTexture;
        static Texture2D bombTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                //spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/RedKnightsSpear");
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyForgottenPearlSpearProj");
            }
            if (NPC.localAI[1] >= 120 && NPC.localAI[1] <= 180f)
            {
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
            if (bombTexture == null)
            {          
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyFirebomb");
            }
            if (NPC.localAI[2] >= 855 && NPC.localAI[2] <= 924f) //&& NPC.localAI[1] <= 120 && NPC.localAI[1] >= 180f
            {
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }
    }
}
