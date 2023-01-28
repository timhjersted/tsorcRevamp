using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheRage : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 15000;
            NPC.damage = 110; 
            NPC.defense = 22;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 120000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.timeLeft = 22500;

            DrawOffsetY = +70;
            NPC.width = 140;
            NPC.height = 60;
            NPC.buffImmune[BuffID.Burning] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("The Rage is satisfied...", Color.OrangeRed, 127);
        }

        public float flapWings;
        int hitTime = 0;
        int fireTrailsDamage = 55; //45 was a bit too easy for folks based on some feedback and watching a LP

        //oolicile sorcerer
        public float FlameShotTimer;
        public float FlameShotCounter;

        public float FlameShotTimer2;
        public float FlameShotCounter2;

        public float FlameShotTimer3;
        public float FlameShotCounter3;

        //chaos
        int holdTimer = 0;
        int breathTimer = 0;

       

        public bool secondPhase
        {
            get => NPC.life <= NPC.lifeMax / 2;
        }

        //gaibon 
        public int Timer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public Player Target
        {
            get => Main.player[NPC.target];
        }        

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = NPC.damage / 2;
            NPC.defense = NPC.defense += 10;
            //NPC.lifeMax = 20000; //this set the rage health to 13000 with NPC.lifeMax = 23100; above and I have no idea why
            //fireTrailsDamage = (int)(fireTrailsDamage * 1.3 / 2);
            fireTrailsDamage = (int)(fireTrailsDamage / 2);
        }


        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.netUpdate = true;
            NPC.ai[2]++;
            NPC.ai[1]++;
            hitTime++;
            if (NPC.ai[0] > 0) NPC.ai[0] -= hitTime / 10;
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            //Fun fact: Dusts apparently have a max Scale of 16. For an incredibly good reason, i'm sure.
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 0.3f + (10.5f * (NPC.ai[0] / (NPC.lifeMax / 10))));
            Main.dust[dust].noGravity = true;


            

            flapWings++;

            //Flap Wings Sound
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.position); //wing flap sound

            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, PitchVariance = 1f }, NPC.position);
                flapWings = 0; 
            }


            
            //Demon - 1st phase
            if (NPC.life > NPC.lifeMax / 2)
            {
                FlameShotTimer2++;
            }
            // Flame attack - 2nd phase
            if (NPC.life < NPC.lifeMax / 2)
            {
                FlameShotTimer++;
                FlameShotTimer3++;
            }

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            //Proximity Debuffs
            if (NPC.Distance(player.Center) < 1550 && NPC.life < NPC.lifeMax / 2)
            {
                
                    player.AddBuff(BuffID.OnFire, 30, false);

               
                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    UsefulFunctions.BroadcastText("The Rage emits a scorching heat wave from its body! Your lungs are on fire!", 235, 199, 23);//deep yellow
                    holdTimer = 9000;
                }

            }
            //getting close to the rage triggers on fire!
            if (NPC.Distance(player.Center) < 80)
            {
                player.AddBuff(BuffID.OnFire, 180, false);
            }

            //chance to trigger fire from above / 2nd phase 
            if (Main.rand.NextBool(500) )
            {
                
                FlameShotCounter2 = 0;
               
            }


            //DEMON SICKLE - 1ST PHASE
            //counts up each tick. used to space out shots
            if (FlameShotTimer2 >= 30 && FlameShotCounter2 < 6 && NPC.Distance(player.Center) > 200)
            {
                for (int num36 = 0; num36 < 20; num36++)
                {
                    int fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y ), DustID.ShadowbeamStaff, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                    Main.dust[fireDust].noGravity = true;
                    
                    
                }

                // Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 600 + Main.rand.Next(600), (float)player.position.Y - 120f, (float)(-10 + Main.rand.Next(10)) / 2, 0.1f, ProjectileID.DemonSickle, fireTrailsDamage, 2f, Main.myPlayer); // ModContent.ProjectileType<FireTrails>()
                // Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, PitchVariance = 2 }, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 0.035f); //needs to be slow for demon sickle
                    speed += Main.player[NPC.target].velocity / 50; //10 works for demon sickle, /2 was way too sensitive to player speed

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DemonSickle, fireTrailsDamage, 0f, Main.myPlayer);
                }
                

               

                FlameShotTimer2 = 0;
                FlameShotCounter2++;

            }


            //FIRE FROM ABOVE ATTACK - 2nd Phase
            //counts up each tick. used to space out shots
            if (FlameShotTimer >= 45 && FlameShotCounter < 15)
            {
                //more fire trails
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 600 + Main.rand.Next(1200), (float)player.position.Y - 600f, (float)(-40 + Main.rand.Next(80)) / 10, 2f, ModContent.ProjectileType<FireTrails>(), fireTrailsDamage, 2f, Main.myPlayer); //  ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>()

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, PitchVariance = 2 }, NPC.Center);
                

                FlameShotTimer = 0;
                FlameShotCounter++;

                
            }
            //homing fireballs - part of 2nd phase 
            if (FlameShotTimer3 >= 25 && FlameShotCounter3 < 10)
            {
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 500 + Main.rand.Next(500), (float)player.position.Y - 400f, (float)(-40 + Main.rand.Next(80)) / 10, 2.5f, ModContent.ProjectileType<Hellwing>(), fireTrailsDamage, 2f, Main.myPlayer); // ModContent.ProjectileType<FireTrails>()
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 200 + Main.rand.Next(400), (float)player.position.Y - 480f, (float)(-40 + Main.rand.Next(80)) / 10, 3f, ProjectileID.CultistBossFireBall, fireTrailsDamage, 3f, Main.myPlayer); //ProjectileID.NebulaBlaze2 would be cool to use at the end of attraidies or gwyn fight with the text, "The spirit of your father summons cosmic light to aid you!"
                FlameShotTimer3 = 0;
                FlameShotCounter3++;
            }

            //FIRE BREATH - aggressive frequency, during all phases of movement
            //triggers at 75% health
            if (NPC.life < NPC.lifeMax / 2 * 1.5)
            {
                breathTimer++;
                
            }
            if (breathTimer > 280)
            {
                breathTimer = -29;
            }
            if (breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 12);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f); if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireTrailsDamage, 0f, Main.myPlayer);
                    }
                    

                    //play breath sound
                    if (Main.rand.NextBool(3))
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, PitchVariance = 1f }, NPC.Center); //flame thrower
                    }
                }
            }
            if (breathTimer > 180)
            {
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((280f - breathTimer) / 100f)), DustID.Torch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1);
            }



            //GAIBON FIRE BOMBS!     
            Timer++;

            //1st phase frequency
            if (Main.rand.NextBool(350) && (player.position.Y + 30 >= NPC.position.Y) && NPC.life > NPC.lifeMax / 2)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                        for (int num36 = 0; num36 < 6; num36++)
                        {
                            int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);
                            Main.dust[pink].noGravity = true;
                        }
                
               
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6.5f, .1f, true, true);
                    velocity += Target.velocity / 1.5f;
                    
                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), fireTrailsDamage, 0.5f, Main.myPlayer); 
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), fireTrailsDamage, 0.5f, Main.myPlayer); //ProjectileID.LostSoulHostile
                    }
                    
                }
            }
            //2nd phase frequency
            if (Main.rand.NextBool(130) && (player.position.Y + 30 >= NPC.position.Y) && NPC.life < NPC.lifeMax / 2)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int num36 = 0; num36 < 6; num36++)
                    {
                        int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);

                        Main.dust[pink].noGravity = true;
                    }
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6.5f, .1f, true, true);
                    velocity += Target.velocity / 1.5f;
                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), fireTrailsDamage, 0.5f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), fireTrailsDamage, 0.5f, Main.myPlayer);
                    }
                }
            }

           
            
            

            //SPAWN METEOR HELL
            if (Main.rand.NextBool(80) && NPC.Distance(player.Center) > 100 && NPC.life <= 3000 )
            {

                if (player.position.Y + 50 >= NPC.position.Y)
                {
                    int Meteor = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.MeteorHead, 0); //ModContent.NPCType<NPCs.Enemies.MutantToad>()

                    for (int num36 = 0; num36 < 20; num36++)
                    {
                        int fireDust = Dust.NewDust(new Vector2(vector8.X + 500, vector8.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X - 500, vector8.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                    }

                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit24 with { Volume = 0.5f }, NPC.Center);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Meteor, 0f, 0f, 0f, 0);
                    }
                }
                

            }

            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
                NPC.damage = 110;
                //NPC.dontTakeDamage = false;
                NPC.defense = 22;

                //if (breathTimer < 179)
                //{ breathTimer = 100; }

                if (NPC.ai[2] < 600)
                {
                    if (Main.player[NPC.target].position.X < vector8.X)
                    {

                        if (NPC.velocity.X > -8) { NPC.velocity.X -= 0.22f; }
                    }
                    if (Main.player[NPC.target].position.X > vector8.X)
                    {
                        if (NPC.velocity.X < 8) { NPC.velocity.X += 0.22f; }
                    }

                    if (Main.player[NPC.target].position.Y < vector8.Y + 300)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].position.Y > vector8.Y + 300)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }

                    if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                    {
                        float num48 = 4f;//4 was 5, speed
                        int type = ModContent.ProjectileType<FireTrails>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, PitchVariance = 1f }, NPC.Center) ; //flame thrower
                        float rotation = (float)Math.Atan2(vector8.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + 600, vector8.Y - 120, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 120, (float)((Math.Cos(rotation + 0.2) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X - 600, vector8.Y - 120, (float)((Math.Cos(rotation - 0.2) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        NPC.ai[1] = -90;
                        //Added some dust so the projectiles aren't just appearing out of thin air
                        for (int num36 = 0; num36 < 20; num36++)
                        {
                            int fireDust = Dust.NewDust(new Vector2(vector8.X + 600, vector8.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(vector8.X - 600, vector8.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                        }

                        
                    }
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 850)
                {
                    FlameShotCounter3 = 0;
                    //Then chill for a second.
                    //This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 130, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);

                    
                }
                else if (NPC.ai[2] >= 850 && NPC.ai[2] < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (vector8.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 22) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 22) * -1;//22 w 25, seems to be speed of dash
                    }
                }
                else NPC.ai[2] = 0;
                FlameShotCounter = 0;
            }
            else
            {
                //invisibility phase
                
                NPC.ai[3]++;
                NPC.alpha = 210;
                NPC.defense = 32;
                NPC.damage = 130;

                //NPC.dontTakeDamage = true;
                
                //FlameShotCounter2 = 0;

                if (Main.player[NPC.target].position.X < vector8.X)
                {
                    if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                }
                if (Main.player[NPC.target].position.X > vector8.X)
                {
                    if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                }
                if (Main.player[NPC.target].position.Y < vector8.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                    else NPC.velocity.Y -= 0.07f;
                }
                if (Main.player[NPC.target].position.Y > vector8.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                    else NPC.velocity.Y += 0.07f;
                }
                if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                {

                    float num48 = 7f;//8 WAS 9
                    float invulnDamageMult = 1.44f;
                    int type = ModContent.ProjectileType<FireTrails>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + 300, vector8.Y - 150, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 150, (float)((Math.Cos(rotation + 0.2) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X - 300, vector8.Y - 150, (float)((Math.Cos(rotation - 0.2) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    NPC.ai[1] = -90;
                    //Added some dust so the projectiles aren't just appearing out of thin air
                    for (int num36 = 0; num36 < 20; num36++)
                    {
                        int fireDust = Dust.NewDust(new Vector2(vector8.X + 300, vector8.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X - 300, vector8.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                    }
                }


                if (NPC.ai[3] == 100)
                {
                    NPC.ai[3] = 1;
                    //if (NPC.life < NPC.lifeMax / 2)
                    if (NPC.life > 500)
                    {
                        NPC.life -= 300; //amount boss takes damage when enraged
                      
                    }
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int num36 = 0; num36 < 40; num36++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, 0, 0, 0, new Color(), 3f);
                    }
                }
            }
        }
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            hitTime = 0;
            NPC.ai[0] += (float)damage;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                UsefulFunctions.BroadcastText("The Rage has taken damage too fast, its natural defenses activate...", Color.Orange);

                NPC.ai[3] = 1;
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, 0, 0, 100, default, 3f);
                }
                NPC.ai[1] = -250;
                NPC.ai[0] = 0;
            }
            return true;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheRageBag>()));
        }

        public override void OnKill()
        {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 127, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 70; num36++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 130, Main.rand.Next(-50, 50), Main.rand.Next(-40, 40), 100, Color.Orange, 3f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CrestOfFire>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.CobaltDrill, 1, false, -1);
            }
        }
    }
}
