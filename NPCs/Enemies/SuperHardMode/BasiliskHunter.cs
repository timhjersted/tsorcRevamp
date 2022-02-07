using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class BasiliskHunter : ModNPC
    {
        public override void SetDefaults()
        {

            npc.npcSlots = 2;
            Main.npcFrameCount[npc.type] = 12;
            animationType = 28;
            npc.knockBackResist = 0.03f;
            npc.damage = 95;
            npc.defense = 90; //was 105
            npc.height = 54;
            npc.width = 54;
            npc.lifeMax = 5000;
            npc.HitSound = SoundID.NPCHit29;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.value = 4620;
            npc.lavaImmune = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.BasiliskHunter>();

            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
        }

        int cursedBreathDamage = 20;
        int cursedFlamesDamage = 20;
        int darkExplosionDamage = 35;
        int disruptDamage = 33;
        int bioSpitDamage = 25;
        int bioSpitfinalDamage = 20;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SubtleSHMScale);
            darkExplosionDamage = (int)(darkExplosionDamage * tsorcRevampWorld.SubtleSHMScale);
            disruptDamage = (int)(disruptDamage * tsorcRevampWorld.SubtleSHMScale);
            bioSpitDamage = (int)(bioSpitDamage * tsorcRevampWorld.SubtleSHMScale);
            bioSpitfinalDamage = (int)(bioSpitfinalDamage * tsorcRevampWorld.SubtleSHMScale);
        }


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

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.player;

            bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;

            float chance = 0;

            if (spawnInfo.water) return 0f;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (((player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneUndergroundDesert) && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)) && !player.ZoneDungeon)
                {
                    chance = 0.33f;
                }
                else
                {
                    if (player.ZoneOverworldHeight && (player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHoly) && !Ocean && !Main.dayTime)
                    {
                        chance = 0.11f;
                    }
                }
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        float breathTimer = 0;
        float projTimer = 0;
        float disruptTimer = 0;
        bool chargeDamageFlag;
        int chargeDamage;
        bool breath;
        int breathCD = 120;

        float boredResetT;
        float boredTimer;
        float bReset;
        float customAi1;
        float drowningRisk;
        float drownTimer;
        float drownTimerMax;
        float tBored;



        public override void AI()  //  warrior ai
        {
            tsorcRevampAIs.FighterAI(npc, 1, .03f, 0.2f, true, 10, false, 26, 1000, 0.3f, 1.1f, true);

            
            #region melee movement

            Player player3 = Main.player[npc.target];

            //CHANCE TO JUMP FORWARDS 
            if (npc.Distance(player3.Center) > 250 && npc.velocity.Y == 0f && Main.rand.Next(28) == 1 && npc.life >= 1000)
            {
                int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust2].noGravity = true;

                npc.TargetClosest(true);

                npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(3f, 2f);
                npc.netUpdate = true;

            }
            //CHANCE TO DASH STEP FORWARDS 
            else if (npc.Distance(player3.Center) > 350 && npc.velocity.Y == 0f && Main.rand.Next(28) == 1 && npc.life >= 1000) //was 5
            {
                int dust3 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust3].noGravity = true;



                npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(6f, 5f);
                //npc.TargetClosest(true);
                //npc.velocity.X = npc.velocity.X * 5f; // burst forward

                //if ((float)npc.direction * npc.velocity.X > 5)
                //    npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed

                npc.netUpdate = true;
            }


            //CHANCE TO JUMP BEFORE ATTACK  npc.localAI[1]  >= 103 && 
            if (npc.localAI[1] >= 103 && Main.rand.Next(20) == 1 && npc.life >= 1001)
            {
                npc.velocity.Y = Main.rand.NextFloat(-4f, -2f);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
                npc.netUpdate = true;
            }

            if (npc.localAI[1] >= 113 && Main.rand.Next(20) == 1 && npc.life >= 1001)
            {
                npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(3f, 1f);
                npc.netUpdate = true;
            }

            if (npc.localAI[1] >= 145 && Main.rand.Next(3) == 1 && npc.life <= 1000)
            {
                npc.velocity.Y = Main.rand.NextFloat(-11f, -4f);
                npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(1f, 0f);
                npc.netUpdate = true;

            }


            //OFFENSIVE JUMPS  npc.localAI[1]  <= 186 
            Player player4 = Main.player[npc.target];
            if (npc.localAI[1] >= 100 && npc.velocity.Y == 0f && npc.Distance(player4.Center) > 220 && npc.life >= 1000)
            {
                //CHANCE TO JUMP 
                if (Main.rand.Next(24) == 1)
                {
                    Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(2) == 1)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Fire, npc.velocity.X, npc.velocity.Y);

                    }
                    npc.velocity.Y = -8f; //9             
                    npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(5f, 1f);
                    npc.netUpdate = true;
                    //npc.TargetClosest(true);

                    //npc.localAI[1]  = 165;


                }
            }
            #endregion
            

            #region Charge
            //CHARGE UNTIL DESPERATE PHASE
            if (Main.netMode != 1)
            {
                Player player = Main.player[npc.target];
                if (npc.localAI[1] >= 95 && Main.rand.Next(30) == 1 && npc.Distance(player.Center) > 250 && npc.Distance(player.Center) < 500 && npc.life >= 1001)
                {
                    Lighting.AddLight(npc.Center, Color.LightYellow.ToVector3() * 3f);
                    int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
                    Main.dust[dust2].noGravity = true;

                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(3f, 2f);
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    npc.ai[1] = 1f;
                    npc.netUpdate = true;
                    chargeDamageFlag = false;
                    npc.damage = 80;
                    chargeDamage = 0;
                    npc.localAI[1] = 1f;
                }

            }
            #endregion




            #region Projectiles
            if (Main.netMode != 1)
            {
                //customAi1++; ;
                //customAi2++; ;

                npc.localAI[1]++;
                npc.localAI[2]++;

                //MAKE SOUND WHEN JUMPING/HOVERING
                if (Main.rand.Next(12) == 0 && npc.velocity.Y <= -1f)
                {
                    Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.4f, .1f);
                }

                if (npc.localAI[2] >= 300 && npc.life >= 1000)
                {
                    //BREATH ATTACK
                    //if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)) //&& Main.rand.Next(6) == 1
                    //  {

                    if (npc.localAI[2] >= 301 && npc.localAI[2] <= 395 && npc.Distance(player.Center) > 20 && npc.life >= 1001)
                    {
                        //npc.ai[3]++;
                        if (npc.localAI[2] == 301)
                        {
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 6, .01f, -.5f); //magic mirror
                        }
                        
                        npc.velocity.X = 0f;
                        npc.velocity.Y = 0f;

                        if (Main.rand.Next(2) == 0) //was 12
                        {

                            //Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 46, .05f, -.2f); //hydra
                            // Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, .03f, -.2f); //flamethrower
                        }

                        Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 3f);

                        if (Main.rand.Next(2) == 1)
                        {
                            //Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, npc.velocity.X, npc.velocity.Y);
                            //Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, npc.velocity.X, npc.velocity.Y);

                            //Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, npc.velocity.X, npc.velocity.Y);
                            //Dust.NewDust(npc.position, npc.width, npc.height, 26, npc.velocity.X, npc.velocity.Y);
                            //Dust.NewDust(npc.position, npc.width, npc.height, 26, npc.velocity.X, npc.velocity.Y);
                            //Dust.NewDust(npc.position, npc.width, npc.height, 26, npc.velocity.X, npc.velocity.Y);

                            Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 21, 0, 0, 50, Color.Yellow, 1.0f); //purple magic outward fire
                            Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 21, 0, 0, 50, Color.Yellow, 1.0f);
                            Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 21, 0, 0, 50, Color.Yellow, 2.0f); //purple magic outward fire
                            Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 21, 0, 0, 50, Color.Yellow, 2.0f);

                            //int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 87, 0, 0, 50, Color.Yellow, 1.0f); 
                            //Main.dust[dust].noGravity = false;
                        }

                    }
                    if (npc.localAI[2] == 396)
                    {
                        breath = true;
                        Main.PlaySound(3, (int)npc.position.X, (int)npc.position.Y, 30, 0.8f, -.3f); //3, 21 demon; 3,30 nimbus
                    }

                    if (breath) 
                    {

                        npc.velocity.X = 0f;
                        npc.velocity.Y = 0f;
                        Lighting.AddLight(npc.Center, Color.YellowGreen.ToVector3() * 3f);
                        //float num48 = 3f;
                        //float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
                        //int num54 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, (float)((Math.Cos(rotation) * 15) * -1), (float)((Math.Sin(rotation) * 15) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                        //Main.projectile[num54].timeLeft = 100;

                        //

                        //play breath sound
                        if (Main.rand.Next(3) == 0)
                        {
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.3f, .1f); //flame thrower
                        }

                        float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int num54 = Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y /*+ (5f * npc.direction)*/, npc.velocity.X * 3f + (float)Main.rand.Next(-2, 2), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer); //JungleWyvernFire      cursed dragons breath
                        }
                        npc.netUpdate = true;

                        if (Main.rand.Next(35) == 0)
                        {
                            //int num65 = Projectile.NewProjectile(npc.Center.X + Main.rand.Next(-500, 500), npc.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(npc.Center.X + Main.rand.Next(-100, 100), npc.Center.Y + Main.rand.Next(-100, 100), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), bioSpitDamage, 0f, Main.myPlayer);
                        }
                        breathCD--;

                        if (breathCD == 120)
                        {
                            tsorcRevampAIs.Teleport(npc, 15, true);
                        }
                        if (breathCD == 60)
                        {
                            tsorcRevampAIs.Teleport(npc, 15, true);
                        }
                        //
                    }

                    if (breathCD <= 0)
                    {
                        breath = false;
                        breathCD = 160;
                        npc.localAI[2] = 1f;
                        npc.localAI[1] = 1f;
                        npc.ai[3] = 0; //Reset bored counter. No teleporting after breath attack

                    }

                }

                //TELEGRAPH DUSTS
                if (npc.localAI[1] >= 85)
                {
                    Lighting.AddLight(npc.Center, Color.GreenYellow.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.Next(3) == 1)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
                    }

                }

                if (npc.localAI[1] >= 95f)
                {

                    int choice = Main.rand.Next(4);
                    //PURPLE MAGIC LOB ATTACK; 
                    if (npc.localAI[1] >= 110f && npc.life >= 1001 && choice == 0)
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
                                int lob = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);
                                //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                                //DesertDjinnCurse; ProjectileID.DD2DrakinShot

                                Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

                            }
                            if (npc.localAI[1] >= 154f)
                            { npc.localAI[1] = 1f; }
                        }

                    }

                    npc.TargetClosest(true);
                    Player player = Main.player[npc.target];


                    //HYPNOTIC DISRUPTER ATTACK
                    if (Main.rand.Next(150) == 1 && npc.Distance(player.Center) > 230 && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    {

                        Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 6f, 1.06f, true, true);
                        Projectile.NewProjectile(npc.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disruptDamage, 5f, Main.myPlayer);
                        Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.8f, -.2f); //wobble


                        npc.localAI[1] = 1f;
                        npc.netUpdate = true;
                    }

                    //JUMP DASH 
                    if (npc.localAI[1] >= 110 && npc.velocity.Y == 0f && Main.rand.Next(40) == 1 && npc.life >= 1001)
                    {
                        npc.velocity.Y = Main.rand.NextFloat(-10f, -2f);
                        npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(5f, 2f);
                        npc.netUpdate = true;
                    }


                    //MULTI-SPIT 1 ATTACK
                    if (npc.localAI[1] >= 105f && choice == 1 && Main.rand.Next(8) == 1 && npc.life >= 2001 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 10);

                        if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

                        }

                        if (npc.localAI[1] >= 114f)
                        {
                            npc.localAI[1] = 1f;
                        }
                        //Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, [target], [speed], [projectile gravity], [should it aim high ?], [what happens if enemy is too far away to hit ?]);
                        //Then use projectileVelocity as "velocity" when you call NewProjectile, like
                        //Projectile.NewProjectile(npc.Center, projectileVelocity, ModContent.ProjectileType <[your projectile] > (), damage, knockBack, Main.myPlayer);

                        //Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, player, 6, true, true, false);
                        npc.netUpdate = true;
                    }

                    //MULTI-SPIT 2 ATTACK
                    if (npc.localAI[1] >= 113f && choice >= 2 && Main.rand.Next(8) == 1 && npc.life >= 1001 && npc.life <= 2000)
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 10);

                        if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, 0.5f); //fire
                        }

                        if (npc.localAI[1] >= 145f) //was 126
                        {
                            npc.localAI[1] = 1f;
                        }
                        npc.netUpdate = true;
                    }

                    //JUMP DASH 
                    if (npc.localAI[1] >= 150 && npc.velocity.Y == 0f && Main.rand.Next(20) == 1 && npc.life <= 1000)
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
                        Main.dust[dust2].noGravity = true;

                        //npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
                        npc.netUpdate = true;
                    }

                    //FINAL DESPERATE ATTACK
                    if (npc.localAI[1] >= 155f && npc.life <= 1000)
                    //if (Main.rand.Next(40) == 1)
                    {
                        Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.Next(2) == 1)
                        {
                            int dust3 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.OrangeRed, 1f);
                            Main.dust[dust3].noGravity = true;
                        }
                        npc.velocity.Y = Main.rand.NextFloat(-7f, -3f);

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 8);
                        speed += Main.rand.NextVector2Circular(-6, -2);
                        if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitfinalDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                                                                                                                                                                                                            //Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 21, 0.2f, .1f); //3, 21 water
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.1f, 0.2f);
                        }

                        if (npc.localAI[1] >= 195f) //was 206
                        {
                            npc.localAI[1] = 1f;
                        }


                        npc.netUpdate = true;
                    }
                }
            }

            /* Alternate breath attack
            breathTimer++;
            if (breathTimer > 480)
            {
                breathTimer = -90;
            }

            if (breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer);
                    npc.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                }
            }

            if (breathTimer > 360)
            {
                UsefulFunctions.DustRing(npc.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                Lighting.AddLight(npc.Center, Color.GreenYellow.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                Projectile.NewProjectile(npc.Center.X, npc.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }*/
        }

        #endregion

        #region FindFrame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if (npc.velocity.Y == 0f)
            {
                if (npc.direction == 1)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.direction == -1)
                {
                    npc.spriteDirection = -1;
                }
                if (npc.velocity.X == 0f)
                {
                    npc.frame.Y = 0;
                    npc.frameCounter = 0.0;
                }
                else
                {
                    npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * 2f);
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 6.0)
                    {
                        npc.frame.Y = npc.frame.Y + num;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
                    {
                        npc.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                npc.frameCounter = 0.0;
                npc.frame.Y = num;
                npc.frame.Y = 0;
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player player, int target, bool crit)
        {

            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(37, 10800, false); //horrified
                player.AddBuff(20, 1200, false); //poisoned

            }
            if (Main.rand.Next(6) == 0)
            {
                player.AddBuff(36, 600, false); //broken armor
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
                player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
            }


        }
        #endregion


        public override void NPCLoot()
        {
            if (npc.life <= 0)
            {
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);

                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.CursedSoul>(), 3 + Main.rand.Next(3));
                if (Main.rand.Next(100) < 8) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
            }
        }
    }
}