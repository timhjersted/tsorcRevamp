using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.BossItems;
using tsorcRevamp.Items.Lore;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Gwyn : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.scale = 1.3f;
            Music = 12;
            NPC.damage = 53; //was 295
            NPC.defense = 140;
            NPC.lifeMax = 435000;
            NPC.knockBackResist = 0.01f;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1000000;
            despawnHandler = new NPCDespawnHandler("You have fallen before the Lord of Cinder...", Color.OrangeRed, 6);
        }

        //old attacks, not all used
        int deathBallDamage = 39; //200
        int phantomSeekerDamage = 53; //225
        int armageddonBallDamage = 41; //300
        int holdBallDamage = 13;
        int fireballBallDamage = 54;
        int blazeBallDamage = 21;
        int blackBreathDamage = 34;
        int purpleCrushDamage = 58;
        int iceStormDamage = 40;
        int gravityBallDamage = 66;//300

        //basilisk attacks
        int cursedBreathDamage = 79; //100
        int cursedFlamesDamage = 75; //100
        int disruptDamage = 66;//203
        int bioSpitDamage = 66;//185
        int bioSpitfinalDamage = 72;//230

        //ultimate attack not used yet
        public int redMagicDamage = 39;

        //lumelia attacks
        public int throwingKnifeDamage = 34;//180
        public int smokebombDamage = 58;//295

        //death skull attack when player gets too far away - this is to encourage the player to stay in range of more of Gwyn's attacks so ranged builds don't make the fight too easy
        public int herosArrowDamage = 75; //400

        //slogra attacks
        public int tridentDamage = 43; //150
        //Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
        public int burningSphereDamage = 209;//360

        //gwyn 
        float customAi1;
        float customAi3;
        float customspawn2;
        bool OptionSpawned = false;

        //basilisk
        bool breath;
        int breathCD = 120;
        float breathTimer = 60;
        float shotTimer;

        //slogra
        bool swordDead = false;
        int moveTimer = 0;
        bool dashAttack = false;
        Vector2 pickedTrajectory = Vector2.Zero;
        int baseCooldown = 360; //240
        int lineOfSightTimer = 0;

        //serris x
        int plasmaOrbDamage = 102;

        //oolicile sorcerer
        public float DarkBeadShotTimer;
        public float DarkBeadShotCounter;
        int darkBeadDamage = 34;

        //ancient demon
        int cultistFireDamage = 39;//192
        int cultistMagicDamage = 60;//259
        int cultistLightningDamage = 40;//260
        int fireBreathDamage = 49;//131
        int lostSoulDamage = 36;//223
        int greatFireballDamage = 36;//216
        int blackFireDamage = 55;//147
        int greatAttackDamage = 38;//162

        int demonBreathTimer = 0;

        //chaos
        int holdTimer = 0;
        int lifeTimer = 0;
        int swordTimer = 0;

        #region debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 20 * 60, false); //lose defense on hit
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 20 * 60, false); //slowed life regen
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 20 * 60, false); //you lose knockback resistance
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false); 
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false); 
            }
        }
        #endregion


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            int num58;

            if(swordDead && swordTimer < 1)
            {
                UsefulFunctions.BroadcastText("Gwyn's sword has shattered! Gwyn's defenses have been weakened!", 150, 75, 255);
                UsefulFunctions.BroadcastText("Gwyn's rain of death has strengthened!", 150, 70, 255);
                swordTimer++;
            }
            //fury increases! - notify player when new attacks are incoming
            if (NPC.life <= NPC.lifeMax / 5 * 4 && lifeTimer < 1 || NPC.life <= NPC.lifeMax / 5 * 3 && lifeTimer < 2 || NPC.life <= NPC.lifeMax / 5 * 2 && lifeTimer < 3 || NPC.life <= NPC.lifeMax / 5 && lifeTimer < 3)
            {
                UsefulFunctions.BroadcastText("Gwyn's fury increases!", 175, 75, 255);
                num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                Main.projectile[num58].timeLeft = 560;
                Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
                Main.projectile[num58].ai[0] = this.NPC.target;
    
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                lifeTimer++;
            }


            //chaos defense move
            if (holdTimer > 0)
            {
                holdTimer--;
            }

            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 1150)
            {
                NPC.defense = 9999;
                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText("Gwyn is protected by the soul of cinder -- you're too far away!", 175, 75, 255);
                    holdTimer = 200;
                }
                else
                {
                    NPC.defense = 160;
                }
            }
            
            //spawn sword
            if (OptionSpawned == false)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int swordID = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Bosses.SuperHardMode.SwordOfLordGwyn>(), NPC.whoAmI);
                    Main.npc[swordID].velocity.Y = -10;
                    Main.npc[swordID].netUpdate = true;
                }
                OptionSpawned = true;
            }


            NPC.TargetClosest(true);
            //removed flame dust for visibility, but will maybe add it back
            //int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 0.2f);
            //Main.dust[dust].noGravity = true;

            Player player = Main.player[NPC.target];

            //PROXIMITY-BASED DEBUFFS
            if (NPC.Distance(player.Center) < 600)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);   
            }

            //near instant death when player runs too far away
            if (NPC.Distance(player.Center) > 4600)
            {
                player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 30, false);
                
            }
            //add later: 
            bool tooEarly = !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Seath.SeathTheScalelessHead>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<EarthFiendLich>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Blight>())) || !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<GhostWyvernMage.WyvernMageShadow>()));
            if (tooEarly)
            {
                
                deathBallDamage = 10000;
                phantomSeekerDamage = 10000;
                armageddonBallDamage = 10000;
                holdBallDamage = 10000;
                fireballBallDamage = 10000;
                blazeBallDamage = 10000;
                blackBreathDamage = 10000;
                purpleCrushDamage = 10000;
                fireBreathDamage = 10000;
                iceStormDamage = 10000;
                gravityBallDamage = 10000;
                NPC.damage = 10000;
                tridentDamage = 10000;
                herosArrowDamage = 10000;
                throwingKnifeDamage = 10000;
                smokebombDamage = 10000;
                cultistFireDamage = 10000;
                cultistMagicDamage = 10000;
                cultistLightningDamage = 10000;
                fireBreathDamage = 10000;
                lostSoulDamage = 10000;
                greatFireballDamage = 10000;
                blackFireDamage = 10000;
                greatAttackDamage = 10000;
                
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);


            //JUSTHIT CODE
            if (NPC.justHit && NPC.Distance(player.Center) < 150)
            {
                customAi1 = 1f;
                customAi3 = 1f;
                NPC.localAI[3] = 1f;
                NPC.localAI[2] = 1f;
                NPC.localAI[1] = 1f;

            }
            if (NPC.justHit && NPC.Distance(player.Center) > 151 && NPC.Distance(player.Center) < 349 && Main.rand.NextBool(2))
            {
                customAi1 = 1f;

                NPC.localAI[1] = 1f;

            }
            //customAi1 = 1f;
            if (NPC.justHit && NPC.Distance(player.Center) < 350)
            {

                if (Main.rand.NextBool(8))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f); //was 6 and 3
                    float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-6f, -4f);
                    NPC.velocity.X = v;
                    customAi1 = 70f;
                    NPC.localAI[3] = 1f;
                    NPC.localAI[2] = 1f;
                    NPC.localAI[1] = 1f;
                    DarkBeadShotCounter = 0;
                }
                else
                {
                    customAi1 = 240f;
                    NPC.localAI[3] = 1f;
                    NPC.localAI[2] = 1f;
                    NPC.localAI[1] = 1f;
                }

                NPC.netUpdate = true;
            }
            if (NPC.justHit && NPC.Distance(player.Center) > 451 && Main.rand.NextBool(12))
            {
                if (Main.rand.NextBool(3))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-6f, -3f);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(9f, 3f);
                }
                DarkBeadShotCounter = 0;
                NPC.localAI[2] = 1f;
                NPC.netUpdate = true;
            }

            //JUMP BEFORE KNIFE ATTACK SOMETIMES
            if (customAi1 == 130f && NPC.velocity.Y == 0f && NPC.life >= NPC.lifeMax / 10 && NPC.life <= NPC.lifeMax / 2 && Main.rand.NextBool(2))
            {

                NPC.velocity.Y = Main.rand.NextFloat(-10f, -5f);

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8); //0.4f, true, true																								
                speed += Main.rand.NextVector2Circular(-4, -2);
                if (Main.rand.NextBool(4) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center); //knife throw
                                                                                      
                }

                NPC.netUpdate = true;
            }

            //end just hit code


            /*
            //far enough away code for MP - commented out for now as I don't understand how to apply it to the multiple different projectiles, 
            //each with different distance code

            bool farEnoughAway = false

            //Loop through the whole Main.player array
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                //For each entry in the array, check if it holds an active player, and that they are not dead
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    //If that is true, check if they are far enough away. If so, set farEnoughAway to true and end the loop with break;
                    if (NPC.Distance(Main.player[i]) > 550)
                    {
                        farEnoughAway = true;
                        break;
                    }

                }
            }

            //If the loop found one player far enough away, and the random roll is true, then fire
            if (farEnoughAway && Main.rand.NextBool(350))
            {
                //Fire projectile
            }
            */

            #region Projectiles
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {

                DarkBeadShotTimer++;

                //DARKBEAD ATTACK
                if (NPC.Distance(player.Center) > 350)
                {
                    //Counts up each tick. Used to space out shots
                    if (DarkBeadShotTimer >= 12 && DarkBeadShotCounter < 2)
                    {
                        Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 7);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.ArtoriasDarkBead>(), darkBeadDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //acid flame
                        DarkBeadShotTimer = 0;
                        DarkBeadShotCounter++;
                    }

                }

                customAi1++; ;
                customAi3++; ;
                
               
                    NPC.TargetClosest(true);

                    //ORANGE PHANTOM SEEKER
                    int num59;
                    if (customAi3 >= 230 && NPC.Distance(player.Center) > 300 && Main.rand.NextBool(1500))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            num59 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                            Main.projectile[num59].timeLeft = 460;
                            Main.projectile[num59].rotation = Main.rand.Next(700) / 100f;
                            Main.projectile[num59].ai[0] = this.NPC.target;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        }

                           
                            customAi3 = 160f;

                     }
                    //ANTIMAT ROUNDS
                    if (NPC.life >= NPC.lifeMax / 10 * 3 && NPC.life <= NPC.lifeMax / 20 * 7 && customAi3 >= 270 && NPC.Distance(player.Center) > 520 && Main.rand.NextBool(12))
                    {
                        float num48 = 3f;//was 4
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2) - (160 + Main.rand.Next(80)));//added - 200
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkAntiMatRound>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, bioSpitDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 170;
                                Main.projectile[num54].aiStyle = -1;
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item96 with { Volume = 0.3f }, NPC.Center);
                            }
                            
                            
                            if (customAi3 >= 310)
                            {
                                customAi3 = 1f;
                            }
                            
                        }
                        
                    }
                    //ANTIMAT ROUNDS FINAL
                    if (NPC.life >= NPC.lifeMax / 20 * 3 && NPC.life <= NPC.lifeMax / 10 * 3 && customAi3 >= 270 && NPC.Distance(player.Center) > 550 && Main.rand.NextBool(8))
                    {
                        float num48 = 3f;//was 4
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2) - (160 + Main.rand.Next(80)));//added - 200
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkAntiMatRound>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, bioSpitDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 170;
                                Main.projectile[num54].aiStyle = -1;
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item96 with { Volume = 0.3f }, NPC.Center);
                            }
                            

                            if (customAi3 >= 320)
                            {
                                customAi3 = 1f;
                            }

                        }
                        
                    }

                    //3 DEATH SKULLS WHEN PLAYER RUNS AWAY 
                    if (NPC.Distance(player.Center) > 550 && Main.rand.NextBool(350))
                    {
                           Player nT = Main.player[NPC.target];
                          for (int pcy = 0; pcy < 3; pcy++) //2.1 was 7.1f
                          {
                                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 600f, (float)(-50 + Main.rand.Next(100)) / 10, 0.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), herosArrowDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                                    Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.8f);
                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.3f, Pitch = 0.1f}, NPC.Center); //dungeon spirit sound
                                
                          }
                    }
                    //RAIN OF DEATH
                    if (NPC.life >= NPC.lifeMax / 10 * 2 && NPC.Distance(player.Center) > 650 && Main.rand.NextBool(140))
                    {
                        Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(20))
                        {
                            UsefulFunctions.BroadcastText("Gwyn rains down death!", 175, 75, 255);
                        }

                        for (int pcy = 0; pcy < 6; pcy++) 
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 600 + Main.rand.Next(600), (float)nT.position.Y - 650f, (float)(-50 + Main.rand.Next(100)) / 10, 0.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), herosArrowDamage, 1f, Main.myPlayer); //EnemySpellSuddenDeathBall
                            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1f);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //demon spirit
                            

                        }
                    }
                    //RAIN OF DEATH FINAL
                    if (NPC.life <= NPC.lifeMax / 10 * 2 && NPC.Distance(player.Center) > 580 && NPC.Distance(player.Center) < 1199 && Main.rand.NextBool(90))
                    {
                        Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(16))
                        {
                            UsefulFunctions.BroadcastText("Gwyn summons a tidal wave of death!", 175, 75, 255);
                        }

                        for (int pcy = 0; pcy < 8; pcy++) 
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 800 + Main.rand.Next(800), (float)nT.position.Y - 650f, (float)(-50 + Main.rand.Next(100)) / 10, 0.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), herosArrowDamage, 1f, Main.myPlayer); //EnemySpellSuddenDeathBall
                            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1f);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //demon spirit
                            

                        }
                    }

                    //CURSED FLAMES RAIN WHEN FAR AWAY
                    if (NPC.life <= NPC.lifeMax / 10 * 7 && NPC.life >= 120001 && NPC.Distance(player.Center) > 1200 && Main.rand.NextBool(320))
                    {
                            Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(32))
                        {
                            UsefulFunctions.BroadcastText("Face me!", 125, 85, 255);
                        }
                        for (int pcy = 0; pcy < 12; pcy++) 
                         {
                                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 400 + Main.rand.Next(400), (float)nT.position.Y - 700f, (float)(-50 + Main.rand.Next(100)) / 10, 3f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedFlames>(), herosArrowDamage, 2f, Main.myPlayer); //EnemySpellSuddenDeathBall
                                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1f);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //acid flame
                        
                        }
                    }

                //CURSED FLAMES FAR AWAY FINAL RAIN
                if (NPC.life <= NPC.lifeMax / 100 * 12 && NPC.Distance(player.Center) > 1000 && Main.rand.NextBool(120))
                {
                    Player nT = Main.player[NPC.target];
                    if (Main.rand.NextBool(12))
                    {
                        UsefulFunctions.BroadcastText("Just let go, Red. Submit to your fate!", 125, 85, 255);
                    }
                    for (int pcy = 0; pcy < 12; pcy++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 400 + Main.rand.Next(400), (float)nT.position.Y - 600f, (float)(-50 + Main.rand.Next(100)) / 10, 3f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedFlames>(), herosArrowDamage, 2f, Main.myPlayer); //EnemySpellSuddenDeathBall
                        Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1f);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //acid flame
                    }
                }

                

                //DESPERATE FINAL ATTACK
                if (customAi1 >= 130f && customAi1 <= 148f && NPC.life <= NPC.lifeMax / 2 && NPC.life >= NPC.lifeMax / 5 * 2)
                {

                        NPC.velocity.Y = Main.rand.NextFloat(-11f, -8f);



                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8); //0.4f, true, true																								
                        speed += Main.rand.NextVector2Circular(-4, -2);
                        if (Main.rand.NextBool(4) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        }

                        NPC.netUpdate = true;
                }

                    //THROW KNIFE	
                    if (customAi1 == 152)
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12); //0.4f, true, true
                                                                                                                             //speed += Main.rand.NextVector2Circular(-3, -1);
                        speed += Main.player[NPC.target].velocity / 2;
                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center); //knife throw

                            //go to smoke bomb attack
                            customAi1 = 200f;


                            if (Main.rand.NextBool(2))
                            {
                                //does nothing yet
                            }
                        }
                        NPC.netUpdate = true;

                    }

                    //SMOKE BOMB DUST TELEGRAPH
                    if (customAi1 >= 220 && customAi1 >= 280 && NPC.life >= NPC.lifeMax / 10)
                    {
                        Lighting.AddLight(NPC.Center, Color.Green.ToVector3() * 2f);
                        if (Main.rand.NextBool(2) && NPC.Distance(player.Center) > 1)
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BoneTorch, NPC.velocity.X, NPC.velocity.Y);
                           
                        }

                        //JUMP BEFORE BOMB ATTACK SOMETIMES
                        if (customAi1 == 260f && NPC.velocity.Y == 0f && NPC.life >= NPC.lifeMax / 10 && Main.rand.NextBool(2))
                        {
                            NPC.velocity.Y = Main.rand.NextFloat(-8f, -4f);
                            NPC.netUpdate = true;
                        }

                        //SMOKE BOMB ATTACK
                        if (customAi1 >= 280 && NPC.life >= NPC.lifeMax / 10) 
                        {
                            NPC.TargetClosest(true);

                            if (Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                            {
                                Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10); //, 0.2f, false, false
                                speed2 += Main.rand.NextVector2Circular(-4, 0);
                                //speed2 += Main.player[npc.target].velocity / 2;
                                if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySmokebomb>(), smokebombDamage, 0f, Main.myPlayer);
                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center); //Play swing-throw sound
                                    customAi1 = 1f;
                                }
                            }
                        }
                    }



                    //400,000-500,000
                    //BASILISK SHIFTER PHASE

                    shotTimer++;
                    int shifterChoice = Main.rand.Next(4);
                    if (shotTimer >= 85 && NPC.life >= NPC.lifeMax / 5 * 4)
                    {
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 1f);
                        //if (Main.rand.NextBool(3))
                        //{
                            //Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                            //Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                        //}

                        
                        if (shotTimer >= 100f && NPC.life >= NPC.lifeMax / 5 * 4)
                        {
                            NPC.TargetClosest(true);
                            //PLASMA ORB
                            if (Main.rand.NextBool(100) && NPC.Distance(player.Center) > 350 && NPC.life >= NPC.lifeMax / 5 * 4)
                            {
                                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5f, 1.06f, true, true);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), plasmaOrbDamage, 5f, Main.myPlayer);
                            
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item96 with { Volume = 0.3f, Pitch = -0.5f }, player.Center); //pasheww
                                                                                                                                         
                                shotTimer = 1f;

                                NPC.netUpdate = true;
                            }

                            //CHANCE TO JUMP BEFORE ATTACK
                            //FOR MAIN
                            if (shotTimer == 105 && Main.rand.NextBool(3) && NPC.life >= 860001)
                            {
                                //npc.velocity.Y = -6f;
                                NPC.velocity.Y = Main.rand.NextFloat(-10f, -4f);
                            }
                            //FOR FINAL
                            if (shotTimer >= 185 && Main.rand.NextBool(3) && NPC.life >= NPC.lifeMax / 5 * 4 && NPC.life <= NPC.lifeMax / 100 * 86)
                            {
                                NPC.velocity.Y = Main.rand.NextFloat(-10f, 3f);
                            }


                        }

                    }


                    // NEW BREATH ATTACK    
                    breathTimer++;

                    if (breathTimer > 360)
                    {
                        shotTimer = -60f;
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
                    }

                    if (breathTimer > 480 && shotTimer <= 99f && NPC.life >= NPC.lifeMax / 5 * 4)//12 was 2
                    {
                        breathTimer = -40; 
                        shotTimer = -120f; //was -90
                    }

                    if (breathTimer > 360 && shotTimer <= 99f && NPC.life <= NPC.lifeMax / 5 * 4)
                    {
                        breathTimer = 0;
                        shotTimer = 0f;
                    }

                    if (breathTimer < 0)
                    {
                        NPC.velocity.X = 0f;
                        NPC.netUpdate = true;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                             NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                        }
                    }
                    
                    if (breathTimer == 0)
                    {  
                        shotTimer = 1f;     
                    }


                    //PURPLE MAGIC LOB ATTACK; 
                    if (shotTimer >= 110f && NPC.life >= NPC.lifeMax / 5 * 4 && shifterChoice <= 1)
                    {
                        bool clearSpace = true;
                        for (int i = 0; i < 15; i++)
                        {
                            if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                            {
                                clearSpace = false;
                            }
                        }

                        if (clearSpace && Main.rand.NextBool(3))
                        {
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                            speed.Y += Main.rand.NextFloat(-2f, -6f);

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                //int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);

                                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f }, NPC.Center);//was item20

                            }

                            if (shotTimer >= 154f)
                            {
                                shotTimer = 1f;
                            }
                        }
                    }

                    //NORMAL SPIT ATTACK
                    if (shotTimer >= 115f && NPC.life >= NPC.lifeMax / 100 * 86 && shifterChoice >= 2)
                    {
                        if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                        {
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9);

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                /*int num555 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), tridentDamage, 0f, Main.myPlayer);
                                Main.projectile[num555].timeLeft = 300; //40
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                                */
                                shotTimer = 1f;
                            }
                        }
                    }

                    //FINAL DESPERATE ATTACK
                    if (shotTimer >= 175f && Main.rand.NextBool(2) && NPC.life >= NPC.lifeMax / 5 * 4 && NPC.life <= NPC.lifeMax / 100 * 86)
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 0.5f);
                        Main.dust[dust2].noGravity = true;

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.1f }, NPC.Center);
                        }
                        if (shotTimer >= 206f)
                        {
                            shotTimer = 1f;
                        }
                    }


                    //MAKE SOUND WHEN JUMPING/HOVERING
                    if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f && NPC.life >= NPC.lifeMax / 5 * 4)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
                    }

                    //TELEGRAPH DUSTS
                    if (shotTimer >= 100 && NPC.life >= NPC.lifeMax / 5 * 4)
                    {
                        Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); 
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y); 
                        }
                    }

                    //reset attack timer when hit in melee range
                    if (NPC.justHit && NPC.Distance(player.Center) < 150 && Main.rand.NextBool(4))
                    {
                        shotTimer = 1f;
                    }
                    if (NPC.justHit && NPC.Distance(player.Center) > 151 && NPC.Distance(player.Center) < 300 && Main.rand.NextBool(4))
                    {
                        shotTimer = 1f;
                    }
                    //jump back when hit at close range
                    if (NPC.justHit && NPC.Distance(player.Center) < 250 && Main.rand.NextBool(16))
                    {

                            NPC.velocity.Y = Main.rand.NextFloat(-6f, -4f);
                            NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-7f, -4f);
                            shotTimer = 50f;
                            NPC.netUpdate = true;
                    }

                    //jump forward when hit at range; 
                    if (NPC.justHit && NPC.Distance(player.Center) > 500 && NPC.life >= NPC.lifeMax / 5 * 4 && Main.rand.NextBool(16))
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(7f, 3f);
                        NPC.netUpdate = true;

                    }
                    //END SHIFTER




                //100k-250k
                //BASILISK HUNTER PHASE
                if (NPC.life >= NPC.lifeMax / 2 && NPC.life <= NPC.lifeMax / 5 * 4)
                {

                    NPC.localAI[1]++;
                    NPC.localAI[2]++;
                    //MAKE SOUND WHEN JUMPING/HOVERING
                    if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //hovering
                    }
                    //BREATH ATTACK
                    if (NPC.localAI[2] >= 900)
                    {
                        
                        if (NPC.localAI[2] >= 901 && NPC.localAI[2] <= 995)
                        {

                            if (NPC.localAI[2] == 901)
                            {
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.01f, Pitch = -0.5f }, NPC.Center); //magic mirror
                            }

                            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);

                            if (Main.rand.NextBool(2))
                            {
                                Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1.0f); //purple magic outward fire
                                Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1.0f);
                         
                            }

                        }
                        if (NPC.localAI[2] == 996)
                        {
                            breath = true;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit30 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center); //3, 21 demon; NPCHit30 nimbus
                        }

                        if (breath)
                        {
                            moveTimer--;

                            NPC.velocity.X = 0f;
                            NPC.velocity.Y = 0f;
                            Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 3f);


                            //play breath sound
                            if (Main.rand.NextBool(3))
                            {
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //flame thrower
                            }

                            float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (15 * NPC.direction), NPC.Center.Y /*+ (5f * npc.direction)*/, NPC.velocity.X * 3f + (float)Main.rand.Next(-4, 4), NPC.velocity.Y * 3f + (float)Main.rand.Next(-4, 4), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer); //JungleWyvernFire   5 and 5 was 3f, 18 was 2 -2 ---15 direction was 5
                            }
                            

                            if (Main.rand.NextBool(35))
                            {
                                //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + Main.rand.Next(-100, 100), NPC.Center.Y + Main.rand.Next(-100, 100), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), bioSpitDamage, 0f, Main.myPlayer);
                            }
                            breathCD--;

                            if (breathCD == 120)
                            {
                                tsorcRevampAIs.Teleport(NPC, 25, true);
                            }
                            if (breathCD == 60)
                            {
                                tsorcRevampAIs.Teleport(NPC, 25, true); //was 15
                            }

                        }

                        if (breathCD <= 0)
                        {
                            tsorcRevampAIs.FighterAI(NPC, 2f, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 3);
                            breath = false;
                            breathCD = 160;
                            customAi1 = 1f;
                            customAi3 = 1f;
                            NPC.localAI[3] = 1f;
                            NPC.localAI[2] = 1f;
                            NPC.localAI[1] = 1f;
                            DarkBeadShotCounter = 0;
                        }

                    }


                    if (NPC.localAI[1] >= 95f)
                    {
                        int choice = Main.rand.Next(4);

                        //TELEGRAPH DUSTS got changed up to only show for choice 2 or final
                        if (NPC.life >= NPC.lifeMax / 10 * 3 && NPC.life <= NPC.lifeMax / 5 * 4 && (NPC.localAI[1] >= 95 && choice >= 2 || NPC.localAI[1] >= 135))
                        {
                            Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                            if (Main.rand.NextBool(3))
                            {           
                                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                            }

                        }
                        //PURPLE MAGIC LOB ATTACK;  
                        if (NPC.localAI[1] >= 110f && choice == 0)
                        {
                            bool clearSpace = true;
                            for (int i = 0; i < 15; i++)
                            {
                                if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                                {
                                    clearSpace = false;
                                }
                            }

                            //ICE SPELL 3 ATTACK
                            if (clearSpace)
                            {
                                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 14, 0.015f, true);

                                speed.Y += Main.rand.NextFloat(-2f, -6f);
                                //speed += Main.rand.NextVector2Circular(-10, -8);
                                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                                {

                                    int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), iceStormDamage, 0f, Main.myPlayer);
                                    Lighting.AddLight(NPC.Center, Color.LightBlue.ToVector3() * 0.5f);

                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.3f }, NPC.Center);//magic ice

                                }
                                if (NPC.localAI[1] >= 120f)
                                { NPC.localAI[1] = 1f; }
                            }

                        }

                        NPC.TargetClosest(true);
                       
                        //PHASED MATTER BLAST ATTACK
                        if (Main.rand.NextBool(150) && NPC.life >= NPC.lifeMax / 10 * 3 && NPC.life <= NPC.lifeMax / 5 * 3 && NPC.Distance(player.Center) > 330 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                        {
                            Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6f, 1.06f, true, true);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), disruptDamage, 5f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.2f, Pitch = 0.4f }, NPC.Center); //new sound

                            NPC.localAI[1] = 1f;
                           
                        }

                        //JUMP DASH 
                        if (NPC.localAI[1] >= 110 && NPC.Distance(player.Center) > 450 && NPC.velocity.Y == 0f && Main.rand.NextBool(30) && NPC.life >= NPC.lifeMax / 100 * 15 && NPC.life <= NPC.lifeMax / 10 * 3)
                        {
                            NPC.velocity.Y = Main.rand.NextFloat(-8f, -2f);
                            NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(5f, 2f);
                            NPC.netUpdate = true;
                        }


                        //MULTI-SPIT 1 ATTACK 
                        if (NPC.localAI[1] >= 105f && choice == 1 && Main.rand.NextBool(8) && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                        {

                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                            }

                            if (NPC.localAI[1] >= 114f)
                            {
                                NPC.localAI[1] = 1f;
                            }

                            
                        }

                        //MULTI-SPIT 2 ATTACK 
                        if (NPC.localAI[1] >= 113f && choice >= 2 && Main.rand.NextBool(4))
                        {
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer); //5f was 0f in the example that works

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center); //fire
                            }

                            if (NPC.localAI[1] >= 145f) //was 126
                            {
                                NPC.localAI[1] = 1f;
                            }
                            
                        }

                        //JUMP DASH 
                        if (NPC.localAI[1] >= 150 && NPC.velocity.Y == 0f && NPC.life >= NPC.lifeMax / 5 && NPC.life <= NPC.lifeMax / 10 * 3 && Main.rand.NextBool(20))
                        {
                            //int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
                            //Main.dust[dust2].noGravity = true;

                            NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                            NPC.netUpdate = true;
                        }

                        //FINAL DESPERATE ATTACK
                        if (NPC.localAI[1] >= 155f && NPC.life <= NPC.lifeMax / 2 && NPC.life >= NPC.lifeMax / 5 * 2)
                        {
                            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                            if (Main.rand.NextBool(2))
                            {
                                int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.OrangeRed, 1f);
                                Main.dust[dust3].noGravity = true;
                            }
                            NPC.velocity.Y = Main.rand.NextFloat(-7f, -3f);

                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8);
                            speed += Main.rand.NextVector2Circular(-6, -2);
                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitfinalDamage, 5f, Main.myPlayer); //5f was 0f in the example that works

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                            }

                            if (NPC.localAI[1] >= 195f) //was 206
                            {
                                NPC.localAI[1] = 1f;
                            }


                            NPC.netUpdate = true;
                        }
                    } //END BASILISK HUNTER PHASE
                }




                //250,000 and less
                //ANCIENT DEMON PHASE
                if (NPC.life <= NPC.lifeMax / 2)
                {
                    int demonChoice = Main.rand.Next(6);
                    NPC.localAI[1]++;
                    NPC.localAI[3]++;

                    //play creature sounds
                    if (Main.rand.NextBool(6700))
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.7f }, NPC.Center);
                        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
                    }

                    bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
                    tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.NextBool(180), false, SoundID.Item17);
                    tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistFireDamage, 1, Main.rand.NextBool(40), false, SoundID.NPCHit34);

                    //CHANCE TO JUMP BEFORE ATTACK  
                    if (NPC.localAI[3] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(30) && NPC.life >= NPC.lifeMax / 100 * 15 && NPC.life <= NPC.lifeMax / 2)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-9f, -6f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                        NPC.netUpdate = true;
                    }

                    if (NPC.localAI[3] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(33) && NPC.life <= NPC.lifeMax / 100 * 15)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-7f, -4f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                        NPC.netUpdate = true;

                    }

                    //EARLY TELEGRAPH
                    if (NPC.localAI[3] >= 60)
                    {
                        Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(8))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y);
                        }
                    }
                    //LAST SECOND TELEGRAPH
                    if (NPC.localAI[3] >= 110)
                    {
                        Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                        }
                    }

                    // NEW BREATH ATTACK 
                    if (demonBreathTimer == 350 && Main.rand.NextBool(3))
                    {
                        demonBreathTimer = 1;
                    }
                    
                    demonBreathTimer++;

                    if (demonBreathTimer > 480)
                    {
                        NPC.localAI[3] = -70;
                        if (NPC.life >= NPC.lifeMax / 100 * 15)
                        { demonBreathTimer = -60; }
                        if (NPC.life <= NPC.lifeMax / 100 * 15)
                        { demonBreathTimer = -140; }

                    }

                    if (demonBreathTimer == 470)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f }, NPC.Center); //shadowflame hex (little beasty)
                    }

                    if (demonBreathTimer < 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            //npc.velocity.Y = -1.1f;
                            NPC.velocity.Y = Main.rand.NextFloat(-4f, -1.1f);
                            NPC.velocity.X = 0f;

                            //play breath sound
                            if (Main.rand.NextBool(3))
                            {

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
                            }

                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].OldPos(9), 9);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);

                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y - 40f, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                            //NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack //this was active but unsure if it's used
                            NPC.localAI[3] = -50;
                        }
                    }

                    if (demonBreathTimer == 361)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
                    }
                    if (demonBreathTimer > 360)
                    {
                        NPC.localAI[3] = -50;
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - demonBreathTimer) / 120)), DustID.Torch, 48, 4);
                        Lighting.AddLight(NPC.Center * 2, Color.Red.ToVector3() * 5);
                    }

                    if (demonBreathTimer == 0)
                    {
                        NPC.localAI[3] = -150;
                        //npc.TargetClosest(true);
                        //NPC.velocity.X = 0f; //removing this might break it

                    }
                    //}

                    //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
                    Player player3 = Main.player[NPC.target];
                    if (Main.rand.NextBool(90) && NPC.Distance(player3.Center) > 600 && NPC.Distance(player3.Center) < 900)
                    {
                        Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
                        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = 0.5f }, NPC.Center); //wobble
                        NPC.localAI[3] = 1f;

                        
                    }
                    //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], ProjectileID.LostSoulHostile, lostSoulDamage, 3, lineOfSight, true, 4, 9);



                    //CHOICES
                    if (NPC.localAI[3] >= 160f && (demonChoice == 0 || demonChoice == 4))
                    {
                        bool clearSpace = true;
                        for (int i = 0; i < 15; i++)
                        {
                            if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                            {
                                clearSpace = false;
                            }
                        }
                        //LOB ATTACK PURPLE 
                        if (clearSpace)
                        {
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                            speed.Y += Main.rand.NextFloat(-2f, -6f);
                            //speed += Main.rand.NextVector2Circular(-10, -8);
                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                int lob2 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, fireBreathDamage, 0f, Main.myPlayer);

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -.5f }, NPC.Center);

                            }
                            if (NPC.localAI[3] >= 195f)
                            { NPC.localAI[3] = 1f; }
                        }
                        //LOB ATTACK >> BOUNCING FIRE
                        if (clearSpace)
                        {

                            

                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);
                            speed.Y += Main.rand.NextFloat(2f, -2f);
                            //speed += Main.rand.NextVector2Circular(-10, -8);
                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                /*
                                float num48 = 0.5f;
                                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                                int type = ModContent.ProjectileType<ShadowOrb>();
                                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, fireBreathDamage, 0f, Main.myPlayer);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                                //ShadowShotCount++;
                                */

                                //int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
                                //        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);
                                //        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);

                                if (NPC.localAI[3] >= 186f)
                                { NPC.localAI[3] = 1f; }
                            }

                        }

                    }

                    //MULTI-FIRE 1 ATTACK 
                    if (NPC.localAI[3] >= 160f && demonChoice == 1)
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                        //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                        if (Main.rand.NextBool(3) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);

                        }
                        if (NPC.localAI[3] >= 175f)
                        {
                            NPC.localAI[3] = 1f;
                        }
                        
                    }
                    //NEBULA HOMING
                    if (NPC.localAI[3] >= 160f && (demonChoice == 1 || demonChoice == 2))
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                        speed.Y += Main.rand.NextFloat(2f, -2f);
                        if (Main.rand.NextBool(2) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.NebulaSphere, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = 0.5f }, NPC.Center); //fire

                        }
                        
                            NPC.localAI[3] = 1f;
                        
                        
                    }
                    //LIGHTNING ATTACK
                    if (NPC.localAI[3] == 160f && (demonChoice == 5 || demonChoice == 4))
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);
                        speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6

                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                            
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
                        }
                        NPC.localAI[3] = -50f;
                    }

                    //FINAL JUNGLE FLAMES DESPERATE ATTACK
                    if (NPC.localAI[3] >= 160f && NPC.life <= NPC.lifeMax / 100 * 15 && (demonChoice == 0 || demonChoice == 3))
                    {
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); 
                        if (Main.rand.NextBool(2))
                        {
                            int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.OrangeRed, 1f);
                            Main.dust[dust3].noGravity = true;
                        }
                        

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5); //last # is speed
                        speed += Main.rand.NextVector2Circular(-3, 3);
                        speed.Y += Main.rand.NextFloat(3f, -3f); //just added
                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), fireBreathDamage, 0f, Main.myPlayer); //5f was 0f in the example that works

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                        }

                        if (NPC.localAI[3] >= 185f) //was 206
                        {
                            NPC.localAI[3] = -90f;
                        }
                        
                    }
                }
                //END DEMON PHASE


                //BEGIN SLOGRA ABILITIES
                moveTimer++;

                    if (swordDead)
                    {
                        
                        NPC.defense = 140; //Speed things up a bit
                        baseCooldown = 180; //was 90

                        if ((customspawn2 < 1) && Main.rand.NextBool(1000))
                        {
                            int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.GreatRedKnight>(), 0);
                            Main.npc[Spawned].velocity.Y = -8;
                            Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                            NPC.ai[0] = 20 - Main.rand.Next(80);
                            customspawn2 += 1f;
                            if (Main.netMode == 2)
                            {
                                NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }

                    //Perform attacks
                    if (moveTimer >= baseCooldown)
                    {
                        if (dashAttack)
                        {
                            DashAttack();
                        }
                        else
                        {
                            JumpAttack();
                        }
                    }

                    //Do basic movement
                    if (moveTimer < baseCooldown)
                    {
                        //NPC.ai[3] = 0; //Never get bored
                        tsorcRevampAIs.FighterAI(NPC, 2f, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 3);
                        //tsorcRevampAIs.LeapAtPlayer(NPC, 7, 4, 1.5f, 128);
                    }

                    //Throw tridents
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float projectileDelay = 160; //120
                        if (swordDead)
                        {
                            projectileDelay = 120; //90
                        }
                        if (moveTimer % projectileDelay == 30 && moveTimer < baseCooldown)
                        {
                            if (swordDead && Main.rand.NextBool(8)) 
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    Vector2 targetPoint = Main.player[NPC.target].Center + new Vector2(-480 + 120 * i, 0);
                                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPoint, 12, .1f, true, true);
                                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 0.5f, Main.myPlayer);
                                    }
                                }

                            }
                            else
                            {
                                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6, .1f, true, true);
                                if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.player[NPC.target].velocity / 1.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), gravityBallDamage, 0.5f, Main.myPlayer);
                                    
                            }
                            }

                        }
                    }

                    //Spawn dust telegraphing moves
                    if (moveTimer < baseCooldown + 70)//90 was 70
                    {
                        float radius = baseCooldown - moveTimer;
                        if (radius < 0)
                        {
                            radius = 0;
                        }

                        for (int j = 0; j < 10 * ((float)moveTimer / (float)baseCooldown); j++)
                        {
                            Vector2 dir = Main.rand.NextVector2CircularEdge(40 + radius, 40 + radius);
                            Vector2 dustPos = NPC.Center + dir;
                            Vector2 dustVel = dir.RotatedBy(MathHelper.Pi);

                            if (moveTimer > baseCooldown)
                            {
                                dustVel = pickedTrajectory;
                            }

                            dustVel.Normalize();
                            dustVel *= 6;
                            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 0.75f).noGravity = true;
                        }
                    }

                    //Check if SwordOfLordGwyn is dead. If so we don't need to keep calling AnyNPCs. 
                    if (OptionSpawned == true)
                    {   
                        if (!NPC.AnyNPCs(ModContent.NPCType<SwordOfLordGwyn>())) 
                        {
                            swordDead = true; 
                        }
                    }

                    //Check if it has line of sight, and if not increase the timer
                    if (Collision.CanHitLine(Main.player[NPC.target].Center, 1, 1, NPC.Center, 1, 1))
                    {
                        lineOfSightTimer = 0;
                    }
                    else
                    {
                        lineOfSightTimer++;
                    }

                    //Jump occasionally if no line of sight
                    if (moveTimer < baseCooldown && NPC.velocity.Y == 0 && lineOfSightTimer % 140 == 139)
                    {
                        NPC.velocity.Y -= 8f;
                    }

                    //If super far away from the player or no line of sight for too long, warp to them
                    if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 5000 || lineOfSightTimer > 300)
                    {
                        Vector2 warpPoint = Main.rand.NextVector2CircularEdge(500, 500);
                        for (int i = 0; i < 100; i++)
                        {
                            warpPoint = Main.rand.NextVector2CircularEdge(500, 500);
                            if (Collision.CanHitLine(Main.player[NPC.target].Center, 1, 1, Main.player[NPC.target].Center + warpPoint, 1, 1))
                            {
                                break;
                            }
                        }

                        NPC.Center = Main.player[NPC.target].Center + warpPoint;
                        moveTimer = 0;
                        customAi1 = 1f;
                        customAi3 = 1f;
                        NPC.localAI[3] = 1f;
                        NPC.localAI[2] = 1f;
                        NPC.localAI[1] = 1f;
                        NPC.netUpdate = true;
                    }              

                void DashAttack()
                {
                    if (Main.player[NPC.target].CanHit(NPC) || moveTimer >= baseCooldown + 45)
                    {
                        if (moveTimer == baseCooldown)
                        {
                            NPC.velocity.Y = -22;
                        }

                        if (moveTimer <= baseCooldown + 45)
                        {
                            pickedTrajectory = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 18);

                            //Don't fall
                            if (NPC.velocity.Y > 0)
                            {
                                NPC.velocity.Y = 0;
                            }
                            NPC.velocity *= 0.9f;
                        }

                        if (moveTimer == baseCooldown + 45)
                        {
                            for (int i = 0; i < 40; i++)
                            {
                                Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                                Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                                dustVel.Normalize();
                                dustVel *= 10;
                                Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 2f).noGravity = true;
                            }
                        }

                        if (moveTimer > baseCooldown + 45)
                        {
                            //Don't keep dashing if too close to a wall
                            if (Collision.CanHitLine(NPC.Center, 2, 2, NPC.Center + pickedTrajectory * 4, 2, 2))
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                                    Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                                    dustVel.Normalize();
                                    dustVel *= 10;
                                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1.4f).noGravity = true;
                                }
                                NPC.velocity = pickedTrajectory;
                            }
                            else
                            {
                                pickedTrajectory = Vector2.Zero;
                            }
                        }


                        if (moveTimer < baseCooldown + 160 && moveTimer >= baseCooldown + 45)
                        {
                            if (moveTimer % 10 == 0)
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    if (swordDead)
                                    {
                                        int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.BurningSphere, 0);
                                        Main.npc[spawned].damage = burningSphereDamage;
                                        Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);
                                        if (Main.netMode == NetmodeID.Server)
                                        {
                                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                                        }
                                    }
                                }
                            }
                        }

                        if (moveTimer > baseCooldown + 71 && pickedTrajectory == Vector2.Zero)
                        {
                            moveTimer = 0;
                            dashAttack = !dashAttack;
                        }
                    }
                    else
                    {
                        if (moveTimer == baseCooldown)
                        {
                            tsorcRevampAIs.FighterAI(NPC, 2, 0.2f, 0.2f, true);
                            moveTimer--; //If it is are doing the dash attack and don't have line of sight, delay the attack until it does
                        }
                    }
                }

                void JumpAttack()
                {

                    if (moveTimer <= baseCooldown + 70)
                    {
                        pickedTrajectory = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12, 0.035f, false, false);
                        pickedTrajectory.Y -= 12;
                    }
                    if (moveTimer == baseCooldown + 65)
                    {
                        NPC.velocity.Y = -5;
                    }
                    if (moveTimer > baseCooldown + 70 && moveTimer < baseCooldown + 115)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                            Vector2 dustVel = NPC.velocity.RotatedBy(MathHelper.Pi);
                            dustVel.Normalize();
                            dustVel *= 10;
                            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1.4f).noGravity = true;
                        }
                    }


                    if (moveTimer < baseCooldown + 70)
                    {
                        NPC.velocity *= 0.9f;
                    }
                    else if (moveTimer == baseCooldown + 70)
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                            Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                            dustVel.Normalize();
                            dustVel *= 10;
                            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 2f).noGravity = true;
                        }
                        NPC.velocity = pickedTrajectory;

                        if (NPC.velocity == Vector2.Zero) //Then there wasn't a valid ballistic trajectory. Just fling itself in the vague direction of the player instead.
                        {
                            NPC.velocity.X = 12 * NPC.direction;
                            NPC.velocity.Y = 12;
                        }
                    }
                    else if (moveTimer < baseCooldown + 160)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {   //jumping gravity ball attack
                            if (moveTimer % 15 == 0 && !swordDead)
                            {                               
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 2), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity4Ball>(), tridentDamage, 0.5f, Main.myPlayer); //2 was 7
                                Lighting.AddLight(NPC.Center, Color.MediumPurple.ToVector3() * 0.5f);
                            }
                            //jumping gaibon fire ball attack
                            if (moveTimer % 15 == 7)
                            {
                                if (swordDead)
                                {
                                    int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.BurningSphere, 0);
                                    Main.npc[spawned].damage = burningSphereDamage;
                                    Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);
                                    if (Main.netMode == NetmodeID.Server)
                                    {
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        moveTimer = 0;
                        dashAttack = !dashAttack;
                    }

                }


                //SERRIS X ATTACK
                float speed9 = 6f;
                Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float distanceFactor = Vector2.Distance(vector9, Main.player[NPC.target].position) / speed9;
                float speedX2 = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) / distanceFactor;
                float speedY2 = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) / distanceFactor;
                if (Main.rand.NextBool(1950) && NPC.Distance(player.Center) > 380)
                {   
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX2, speedY2, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speed9, speed9, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, -speed9, speed9, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speed9, -speed9, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, -speed9, -speed9, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.3f, }, NPC.Center); //79 fuzzy carrot PitchVariance = -0.6f
                    }
                    customAi1 = 1f;
                    
                }

            }
            #endregion
        }


        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (swordDead)
            {
                modifiers.FinalDamage *= 1.2f;
                herosArrowDamage = (int)(herosArrowDamage * 1.25f);
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (swordDead)
            {
                modifiers.FinalDamage *= 1.15f;
                herosArrowDamage = (int)(herosArrowDamage * 1.25f);
            }

            if (projectile.minion)
            {
                modifiers.Knockback *= 0;
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (swordDead)
            {
                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
                    Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 3f);
                }
               

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Rectangle sourceRectangle = NPC.frame;
                Vector2 origin = sourceRectangle.Size() / 2f;
                Vector2 offset = new Vector2(0, -8);
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition + offset, sourceRectangle, Color.White, NPC.rotation, origin, 1.3f, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            }
            float projectileDelay = 160; //was 120
            if (swordDead && OptionSpawned == true)
            {
                projectileDelay = 120; //was 90
            }
            if (moveTimer % projectileDelay <= 30 && moveTimer < baseCooldown)
            {
                //Removed this because (I think) it was causing the trident sprite to appear randomly (not before a trident attack)
                //Main.spriteBatch.End();
                //Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


                if (OptionSpawned == true && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.SwordOfLordGwyn>()))
                {
                    ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                    data.Apply(null);
                }

                //Removed this because it was causing the trident sprite to appear randomly (not before a trident attack)
                /*
                if (Projectiles.Enemy.EarthTrident.texture != null && !Projectiles.Enemy.EarthTrident.texture.IsDisposed)
                {
                    float rotation = 0;
                    if (NPC.direction == 1)
                    {
                        rotation += 0.15f;
                    }
                    else
                    {
                        rotation -= 0.15f;
                    }
                    Rectangle sourceRectangle = new Rectangle(0, 0, Projectiles.Enemy.EarthTrident.texture.Width, Projectiles.Enemy.EarthTrident.texture.Height);
                    Vector2 origin = sourceRectangle.Size() / 2f;
                    Main.EntitySpriteDraw(Projectiles.Enemy.EarthTrident.texture,
                        NPC.Center - Main.screenPosition,
                        sourceRectangle, Color.White, rotation, origin, 1, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                }
                */
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);

        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.GwynBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Epilogue>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<EssenceOfTerraria>()));
            npcLoot.Add(notExpertCondition);
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }


        //knife and bomb sprite telegraph code
        static Texture2D spearTexture;
        static Texture2D bombTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            Player player = Main.player[NPC.target];
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (spearTexture == null) //|| spearTexture.IsDisposed
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyThrowingKnifeSmall");
            }

            if (bombTexture == null)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemySmokebomb");
            }

            //knife
            if (customAi1 >= 120 && customAi1 <= 152)
            {
                Lighting.AddLight(NPC.Center, Color.LightGoldenrodYellow.ToVector3() * 0.8f); //was 0.1f

                if (customAi1 == 120)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item64 with { Volume = 0.3f,  }, NPC.Center); //Play blow dart sound PitchVariance = -0.3f
                }

                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); //facing left, (-22,--) was above NPC head, was 24, 48
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(4, 10), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher, 2nd value is width axis
                }
            }

            //bomb
            if (customAi1 >= 220) 
            {
                if (customAi1 == 220)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { Volume = 0.4f,  }, NPC.Center); //Play blow dart sound PitchVariance = -0.3f
                }

                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1f);
                
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); //facing left, 
                }
                else
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); // facing right
                }
            }
        }

        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
            }
            tsorcRevampWorld.InitiateTheEnd();
        }
        #endregion
    }
}
