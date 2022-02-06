using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;



namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class GreatRedKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Red Knight");
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 5;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.height = 40;
            npc.width = 20;
            npc.damage = 105;
            npc.defense = 61; //was 211
            npc.lifeMax = 13000; //was 35k
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 81870;
            npc.knockBackResist = 0.36f;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.GreatRedKnightOfTheAbyssBanner>();
        }

        public int poisonStrikeDamage = 40;
        public int redKnightsSpearDamage = 35;
        public int redMagicDamage = 40;


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            poisonStrikeDamage = (int)(poisonStrikeDamage * tsorcRevampWorld.SubtleSHMScale);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage * tsorcRevampWorld.SubtleSHMScale);
            redMagicDamage = (int)(redMagicDamage * tsorcRevampWorld.SubtleSHMScale);
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHoly;
            bool AboveEarth = spawnInfo.spawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.spawnTileY >= Main.worldSurface && spawnInfo.spawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.spawnTileY >= Main.rockLayer && spawnInfo.spawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.spawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.spawnTileX < 3600 || spawnInfo.spawnTileX > (Main.maxTilesX - 100) * 16;
            bool BeforeFourAfterSix = spawnInfo.spawnTileX < Main.maxTilesX * 0.4f || spawnInfo.spawnTileX > Main.maxTilesX * 0.6f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?)

            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && Main.bloodMoon && AboveEarth && Main.rand.Next(200) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Dungeon && !Corruption && InGrayLayer && Main.rand.Next(400) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.Next(700) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && BeforeFourAfterSix && InHell && Main.rand.Next(200) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened!", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss has come to destroy you..", 175, 75, 255);
                return 1;
            }

            //if(tsorcRevampWorld.SuperHardMode && Main.rand.Next(1800)==1) 

            //	{
            //		Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
            //		Main.NewText("The Great Red Knight of Artorias is now hunting you...", 175, 75, 255);
            //		return true;
            //	}


            return 0;
        }

        float poisonTimer = 0;

        public Player player
        {
            get => Main.player[npc.target];
        }

        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(npc, true);
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(npc, projectile.melee);
        }


        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 2, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 5);
            tsorcRevampAIs.LeapAtPlayer(npc, 7, 4, 1.5f, 128);
            //tsorcRevampAIs.SimpleProjectile(npc, ref poisonTimer, 100, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonStrikeDamage, 9, Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0));

            //DEBUFFS
            if (npc.Distance(player.Center) < 600)
            {

                player.AddBuff(ModContent.BuffType<Buffs.TornWings>(), 60, false);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60, false);

            }

            poisonTimer++; ;

            //CHANCE TO JUMP FORWARDS
            if (npc.Distance(player.Center) > 20 && npc.velocity.Y == 0f && Main.rand.Next(500) == 1)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
                npc.velocity.Y = -6f; //9             
                npc.TargetClosest(true);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * 2f;  //was 2  accellerate fwd; can happen midair
                if ((float)npc.direction * npc.velocity.X > 2)
                    npc.velocity.X = (float)npc.direction * 2;  //  but cap at top speed
                npc.netUpdate = true;
            }

            //CHANCE TO DASH STEP FORWARDS 
            else if (npc.Distance(player.Center) > 80 && npc.velocity.Y == 0f && Main.rand.Next(300) == 1)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
                //npc.direction *= -1;

                //npc.ai[0] = 0f;
                npc.velocity.Y = -4f;
                //npc.TargetClosest(true);
                npc.velocity.X = npc.velocity.X * 4f; // burst forward

                if ((float)npc.direction * npc.velocity.X > 4)
                    npc.velocity.X = (float)npc.direction * 4;  //  but cap at top speed
                                                                //poisonTimer = 160f;


                //CHANCE TO JUMP AFTER DASH
                if (Main.rand.Next(14) == 1)
                {
                    npc.TargetClosest(true);

                    //npc.spriteDirection = npc.direction;
                    //npc.ai[0] = 0f;
                    Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(3) == 1)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkFlame, npc.velocity.X, npc.velocity.Y);
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkFlame, npc.velocity.X, npc.velocity.Y);
                    }
                    npc.velocity.Y = -7f;


                    poisonTimer = 175f;

                }

                npc.netUpdate = true;
            }


            //TELEGRAPH DUSTS
            if (poisonTimer >= 150 && poisonTimer <= 179)
            {
                Lighting.AddLight(npc.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(2) == 1)
                {
                    int pinkDust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y);
                    //int pinkDust = Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y); //pink dusts
                    Main.dust[pinkDust].noGravity = true;
                }
            }


            //FIRE ATTACK
            if (poisonTimer <= 100 && npc.Distance(player.Center) < 200)
            {

                if (Main.rand.Next(120) == 0) //30 was cool for great red knight
                {
                    //FIRE
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Player nT = Main.player[npc.target];

                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        Projectile.NewProjectile((float)player.position.X - 10 + Main.rand.Next(10), (float)player.position.Y - 300f, (float)(-10 + Main.rand.Next(10)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Main.PlaySound(2, -1, -1, 20, 0.5f, -.01f); //fire
                        //int FireAttack = Projectile... 
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        npc.netUpdate = true;
                    }

                }
            }


            //FIRE ATTACK 2

            if (poisonTimer <= 100 && npc.Distance(player.Center) > 200)
            {
                Player nT = Main.player[npc.target];
                if (Main.rand.Next(90) == 0) //30 was cool for great red knight
                {
                    //FIRE
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int FireAttack = Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 360f, (float)(-50 + Main.rand.Next(100)) / 10, 6.1f, ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), poisonStrikeDamage, 200f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Main.PlaySound(2, -1, -1, 20, 0.5f, .01f); //fire
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        npc.netUpdate = true;
                    }


                    /*//HELLWING ATMOSPHERE BUT DOES NO DAMAGE YET
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int FireAttack = Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 300f, (float)(-50 + Main.rand.Next(100)) / 10, 5.1f, ProjectileID.Hellwing, redMagicDamage, 12f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>()
                        Main.PlaySound(2, -1, -1, 5);
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        npc.netUpdate = true;
                    }
                    */
                }

                if (Main.rand.Next(600) == 0)
                {
                    //BLACK FIRE
                    for (int pcy = 0; pcy < 1; pcy++)
                    {
                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int BlackFire = Projectile.NewProjectile((float)nT.position.X - 50 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 2.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Main.PlaySound(2, -1, -1, 20, 0.5f, -.03f); //fire
                        Main.projectile[BlackFire].timeLeft = 1296;
                        npc.netUpdate = true;
                    }
                }

                if (Main.rand.Next(150) == 0)
                {
                    for (int pcy = 0; pcy < 1; pcy++)
                    {
                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL, ALSO EnemySpellAbyssStormBall
                        int StormStrike = Projectile.NewProjectile((float)nT.position.X - 50 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Main.PlaySound(2, -1, -1, 34, 0.4f, .01f); //flamethrower
                        //Main.projectile[StormStrike].timeLeft = 1296;
                        npc.netUpdate = true;
                    }
                }
            }

            /*ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
              Player player = Main.player[npc.target];
              if (npc.Distance(player.Center) > 20 && Main.rand.Next(3) == 0)
              {
                  Player nT = Main.player[npc.target];
                  if (Main.rand.Next(8) == 0)
                  {
                      Main.NewText("Death!", 175, 75, 255);
                  }

                  for (int pcy = 0; pcy < 3; pcy++)
                  {
                      //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                      Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), redMagicDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                      Main.PlaySound(2, -1, -1, 5);
                      npc.netUpdate = true;
                  }
              }

              */

            if (Main.rand.Next(10) == 0 && npc.life <= 5000)
            {
                if (Main.rand.Next(180) == 0)
                {
                    Main.NewText("Death!", 175, 75, 255);
                }
                //ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
                //Player player = Main.player[npc.target];
                if (npc.Distance(player.Center) > 70 && Main.rand.Next(3) == 0)
                {
                    Player nT = Main.player[npc.target];
                    

                    for (int pcy = 0; pcy < 3; pcy++)
                    {
                        //Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        Projectile.NewProjectile((float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Main.PlaySound(2, -1, -1, 34, 0.2f, .01f); //flamethrower
                        npc.netUpdate = true;
                    }
                }
            }

            




            //OFFENSIVE JUMPS
            if (poisonTimer >= 160 && poisonTimer <= 161 && npc.Distance(player.Center) > 40)
                {
                    //CHANCE TO JUMP 
                    if (Main.rand.Next(10) == 1)
                    {
                        Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.Next(3) == 1)
                        {
                            //Dust.NewDust(npc.position, npc.width, npc.height, DustID.TeleportationPotion, npc.velocity.X, npc.velocity.Y);

                        }
                        npc.velocity.Y = -9f; //9             
                        npc.TargetClosest(true);
                        poisonTimer = 165;
                        npc.netUpdate = true;

                    }
            }
            //SPEAR ATTACK
            if (poisonTimer == 180f) //180 (without 2nd condition) and 185 created an insane attack && poisonTimer <= 181f
            {
                    npc.TargetClosest(true);
                    //if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                    //{
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 12); //0.4f, true, true																								
                         speed += Main.player[npc.target].velocity;
                        //speed += Main.rand.NextVector2Circular(-4, -2);

                    if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                        Main.PlaySound(SoundID.Item1.WithVolume(.6f).WithPitchVariance(.3f), npc.position); //Play swing-throw sound
                        //go to poison attack
                        poisonTimer = 185f;

                            if (Main.rand.Next(3) == 1)
                            {
                                //or chance to reset
                                poisonTimer = 1f;

                            }
                        
                    }

            }

                //POISON ATTACK DUST TELEGRAPH
                if (poisonTimer >= 185 && npc.life >= 2001) //was 180
                {
                    //if(Main.rand.Next(60) == 0)
                    //{
                    Lighting.AddLight(npc.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(2) == 1 && npc.Distance(player.Center) > 10)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Teleporter, npc.velocity.X, npc.velocity.Y);
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Teleporter, npc.velocity.X, npc.velocity.Y);
                    }

                    //POISON ATTACK
                    if (poisonTimer >= 250 && Main.rand.Next(2) == 1) //30 was cool for great red knight
                    {
                        npc.TargetClosest(true);
                        //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500)
                        if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                        {
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 12); //0.4f, true, true																								
                            speed2 += Main.player[npc.target].velocity / 2;

                        if (((speed2.X < 0f) && (npc.velocity.X < 0f)) || ((speed2.X > 0f) && (npc.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 125, 0.3f, 0f); //phantasmal bolt fire 2
                        }
                        if(poisonTimer >=255)
                        { poisonTimer = 1f; }
                        }
                    }
                }



            //DD2DrakinShot FINAL ATTACK
            if (poisonTimer >= 185f && npc.life <= 2000)
            {
                bool clearSpace = true;
                for (int i = 0; i < 15; i++)
                {
                    if (UsefulFunctions.IsTileReallySolid((int)npc.Center.X / 16, ((int)npc.Center.Y / 16) - i))
                    {
                        clearSpace = false;
                    }
                }

                if (clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 5);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);

                        Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

                    }

                    if (poisonTimer >= 230f)
                    {
                        poisonTimer = 1f;
                    }
                }
            }
            /*INSANE WHIP ATTACK
            if (poisonTimer >= 180f && npc.life <= 2000) //180 (without 2nd condition) and 185 created an insane attack
            {
                npc.TargetClosest(true);
                if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                {
                    Vector2 speed = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);

                    if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);
                        Main.PlaySound(2, -1, -1, 34, 0.3f, .03f); //flamethrower

                        //
                    }
                    if (poisonTimer >= 200f) //180 (without 2nd condition) and 185 created an insane attack
                    {
                        poisonTimer = 1f;
                    }
                }

            }
            */


        }



        public override void NPCLoot()
        {            
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 1"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
            
            if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>(), 1 + Main.rand.Next(1));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 3 + Main.rand.Next(3));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>(), 1 + Main.rand.Next(1));
        }


        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = mod.GetTexture("Projectiles/Enemy/RedKnightsSpear");
            }
            if (poisonTimer >= 120 && poisonTimer <= 180f)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;

                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (npc.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }



    }
  

}

/*
  


                
*/