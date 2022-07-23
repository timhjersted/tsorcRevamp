using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;



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
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 105;
            NPC.defense = 61; //was 211
            NPC.lifeMax = 13000; //was 35k
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 81870;
            NPC.knockBackResist = 0.36f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GreatRedKnightOfTheAbyssBanner>();
        }

        public int poisonStrikeDamage = 40;
        public int redKnightsSpearDamage = 35;
        public int redMagicDamage = 40;


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            poisonStrikeDamage = (int)(poisonStrikeDamage * tsorcRevampWorld.SubtleSHMScale);
            redKnightsSpearDamage = (int)(redKnightsSpearDamage * tsorcRevampWorld.SubtleSHMScale);
            redMagicDamage = (int)(redMagicDamage * tsorcRevampWorld.SubtleSHMScale);
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;
            bool BeforeFourAfterSix = spawnInfo.SpawnTileX < Main.maxTilesX * 0.4f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.6f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?)

            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && Main.bloodMoon && AboveEarth && Main.rand.NextBool(200))

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                UsefulFunctions.BroadcastText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Dungeon && !Corruption && InGrayLayer && Main.rand.NextBool(400))

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                UsefulFunctions.BroadcastText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.NextBool(700))

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                UsefulFunctions.BroadcastText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && BeforeFourAfterSix && InHell && Main.rand.NextBool(200))

            {
                //Main.NewText("A portal from The Abyss has been opened!", 175, 75, 255);
                UsefulFunctions.BroadcastText("A Great Red Knight of the Abyss has come to destroy you..", 175, 75, 255);
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
            get => Main.player[NPC.target];
        }

        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }


        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 5);
            tsorcRevampAIs.LeapAtPlayer(NPC, 7, 4, 1.5f, 128);
            //tsorcRevampAIs.SimpleProjectile(npc, ref poisonTimer, 100, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonStrikeDamage, 9, Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0));

            //DEBUFFS
            if (NPC.Distance(player.Center) < 600)
            {

                player.AddBuff(ModContent.BuffType<Buffs.TornWings>(), 60, false);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60, false);

            }

            poisonTimer++; ;

            //CHANCE TO JUMP FORWARDS
            if (NPC.Distance(player.Center) > 20 && NPC.velocity.Y == 0f && Main.rand.NextBool(500))
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
                NPC.velocity.Y = -6f; //9             
                NPC.TargetClosest(true);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;  //was 2  accellerate fwd; can happen midair
                if ((float)NPC.direction * NPC.velocity.X > 2)
                    NPC.velocity.X = (float)NPC.direction * 2;  //  but cap at top speed
                NPC.netUpdate = true;
            }

            //CHANCE TO DASH STEP FORWARDS 
            else if (NPC.Distance(player.Center) > 80 && NPC.velocity.Y == 0f && Main.rand.NextBool(300))
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
                //npc.direction *= -1;

                //npc.ai[0] = 0f;
                NPC.velocity.Y = -4f;
                //npc.TargetClosest(true);
                NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                if ((float)NPC.direction * NPC.velocity.X > 4)
                    NPC.velocity.X = (float)NPC.direction * 4;  //  but cap at top speed
                                                                //poisonTimer = 160f;


                //CHANCE TO JUMP AFTER DASH
                if (Main.rand.NextBool(14))
                {
                    NPC.TargetClosest(true);

                    //npc.spriteDirection = npc.direction;
                    //npc.ai[0] = 0f;
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(3))
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y);
                    }
                    NPC.velocity.Y = -7f;


                    poisonTimer = 175f;

                }

                NPC.netUpdate = true;
            }


            //TELEGRAPH DUSTS
            if (poisonTimer >= 150 && poisonTimer <= 179)
            {
                Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int pinkDust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                    //int pinkDust = Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y); //pink dusts
                    Main.dust[pinkDust].noGravity = true;
                }
            }


            //FIRE ATTACK
            if (poisonTimer <= 100 && NPC.Distance(player.Center) < 200)
            {

                if (Main.rand.NextBool(120)) //30 was cool for great red knight
                {
                    //FIRE
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Player nT = Main.player[npc.target];

                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 10 + Main.rand.Next(10), (float)player.position.Y - 300f, (float)(-10 + Main.rand.Next(10)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 2f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }); //fire

                        //int FireAttack = Projectile... 
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        NPC.netUpdate = true;
                    }

                }
            }


            //FIRE ATTACK 2

            if (poisonTimer <= 100 && NPC.Distance(player.Center) > 200)
            {
                Player nT = Main.player[NPC.target];
                if (Main.rand.NextBool(90)) //30 was cool for great red knight
                {
                    //FIRE
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int FireAttack = Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 360f, (float)(-50 + Main.rand.Next(100)) / 10, 6.1f, ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), poisonStrikeDamage, 200f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }); //fire
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        NPC.netUpdate = true;
                    }


                    /*//HELLWING ATMOSPHERE BUT DOES NO DAMAGE YET
                    for (int pcy = 0; pcy < 2; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int FireAttack = Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 300f, (float)(-50 + Main.rand.Next(100)) / 10, 5.1f, ProjectileID.Hellwing, redMagicDamage, 12f, Main.myPlayer); //Hellwing 12 was 2, was 8.9f near 10, not sure what / 10, does   ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>()
                        Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 5);
                        //Main.projectile[FireAttack].timeLeft = 15296;
                        npc.netUpdate = true;
                    }
                    */
                }

                if (Main.rand.NextBool(600))
                {
                    //BLACK FIRE
                    for (int pcy = 0; pcy < 1; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        int BlackFire = Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 50 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 2.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.03f });//fire
                        Main.projectile[BlackFire].timeLeft = 1296;
                        NPC.netUpdate = true;
                    }
                }

                if (Main.rand.NextBool(150))
                {
                    for (int pcy = 0; pcy < 1; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL, ALSO EnemySpellAbyssStormBall
                        int StormStrike = Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 50 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 4.1f, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.4f, Pitch = 0.01f }); //flamethrower
                        //Main.projectile[StormStrike].timeLeft = 1296;
                        NPC.netUpdate = true;
                    }
                }
            }

            /*ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
              Player player = Main.player[npc.target];
              if (npc.Distance(player.Center) > 20 && Main.rand.NextBool(3))
              {
                  Player nT = Main.player[npc.target];
                  if (Main.rand.NextBool(8))
                  {
                      UsefulFunctions.BroadcastText("Death!", 175, 75, 255);
                  }

                  for (int pcy = 0; pcy < 3; pcy++)
                  {
                      //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                      Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), redMagicDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                      Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 5);
                      npc.netUpdate = true;
                  }
              }

              */

            if (Main.rand.NextBool(10) && NPC.life <= 5000)
            {
                if (Main.rand.NextBool(180))
                {
                    UsefulFunctions.BroadcastText("Death!", 175, 75, 255);
                }
                //ULTIMATE DEATH ATTACK - BLANKET OF FIRE ABOVE PLAYER THAT CURSES
                //Player player = Main.player[npc.target];
                if (NPC.Distance(player.Center) > 70 && Main.rand.NextBool(3))
                {
                    Player nT = Main.player[NPC.target];


                    for (int pcy = 0; pcy < 3; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), poisonStrikeDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.01f }); //flamethrower
                        NPC.netUpdate = true;
                    }
                }
            }






            //OFFENSIVE JUMPS
            if (poisonTimer >= 160 && poisonTimer <= 161 && NPC.Distance(player.Center) > 40)
            {
                //CHANCE TO JUMP 
                if (Main.rand.NextBool(10))
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(3))
                    {
                        //Dust.NewDust(npc.position, npc.width, npc.height, DustID.TeleportationPotion, npc.velocity.X, npc.velocity.Y);

                    }
                    NPC.velocity.Y = -9f; //9             
                    NPC.TargetClosest(true);
                    poisonTimer = 165;
                    NPC.netUpdate = true;

                }
            }
            //SPEAR ATTACK
            if (poisonTimer == 180f) //180 (without 2nd condition) and 185 created an insane attack && poisonTimer <= 181f
            {
                NPC.TargetClosest(true);
                //if (Collision.CanHitLine(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1))
                //{
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12); //0.4f, true, true																								
                speed += Main.player[NPC.target].velocity;
                //speed += Main.rand.NextVector2Circular(-4, -2);

                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.RedKnightsSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.3f }, NPC.position); //Play swing-throw sound
                                                                                                                              //go to poison attack
                    poisonTimer = 185f;

                    if (Main.rand.NextBool(3))
                    {
                        //or chance to reset
                        poisonTimer = 1f;

                    }

                }

            }

            //POISON ATTACK DUST TELEGRAPH
            if (poisonTimer >= 185 && NPC.life >= 3001) //was 180
            {
                //if(Main.rand.NextBool(60))
                //{
                Lighting.AddLight(NPC.Center, Color.Yellow.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2) && NPC.Distance(player.Center) > 10)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Teleporter, NPC.velocity.X, NPC.velocity.Y);
                }

                //POISON ATTACK
                if (poisonTimer >= 250 && Main.rand.NextBool(2)) //30 was cool for great red knight
                {
                    NPC.TargetClosest(true);
                    //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500)
                    if (Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                    {
                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12); //0.4f, true, true																								
                        speed2 += Main.player[NPC.target].velocity / 2;

                        if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), redMagicDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item125 with { Volume = 0.3f, Pitch = 0.0f }); //phantasmal bolt fire 2
                        }
                        if (poisonTimer >= 255)
                        { poisonTimer = 1f; }
                    }
                }
            }



            //DD2DrakinShot FINAL ATTACK
            if (poisonTimer >= 185f && NPC.life <= 3000)
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
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

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
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, poisonStrikeDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 34, 0.3f, .03f); //flamethrower

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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.RedTitanite>(), 1, 3, 6));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.PurgingStone>(), 1, 1, 2));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.FlameOfTheAbyss>(), 2, 1, 2));
        }


        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/RedKnightsSpear");
            }
            if (poisonTimer >= 120 && poisonTimer <= 180f)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;

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
        }



    }


}

/*
  


                
*/