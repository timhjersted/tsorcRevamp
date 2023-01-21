using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class BasiliskHunter : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 3;
            Main.npcFrameCount[NPC.type] = 12;
            AnimationType = 28;
            NPC.knockBackResist = 0.03f;
            NPC.damage = 95;
            NPC.defense = 90; //was 105
            NPC.height = 54;
            NPC.width = 54;
            NPC.lifeMax = 5300;
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 8820;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BasiliskHunter>();

            NPC.buffImmune[BuffID.Poisoned] = true;
            //NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
        }

        int cursedBreathDamage = 20;
        int cursedFlamesDamage = 20;
        int darkExplosionDamage = 45;
        int disruptDamage = 43;
        int bioSpitDamage = 45;
        int bioSpitfinalDamage = 50;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            cursedBreathDamage = (int)(cursedBreathDamage * tsorcRevampWorld.SubtleSHMScale);
            darkExplosionDamage = (int)(darkExplosionDamage * tsorcRevampWorld.SubtleSHMScale);
            disruptDamage = (int)(disruptDamage * tsorcRevampWorld.SubtleSHMScale);
            bioSpitDamage = (int)(bioSpitDamage * tsorcRevampWorld.SubtleSHMScale);
            bioSpitfinalDamage = (int)(bioSpitfinalDamage * tsorcRevampWorld.SubtleSHMScale);
        }


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

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            float chance = 0;

            //Ensuring it can't spawn if two already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 1)
                    {
                        return 0;
                    }
                }
            }

            if (spawnInfo.Water) return 0f;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (((player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneUndergroundDesert) && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)) && !player.ZoneDungeon)
                {
                    chance = 0.33f;
                }
                else
                {
                    if (player.ZoneOverworldHeight && (player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHallow) && !Ocean && !Main.dayTime)
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

        //float breathTimer = 0;
        //float projTimer = 0;
        //float disruptTimer = 0;
        //bool chargeDamageFlag;
        //int chargeDamage;
        bool breath;
        int breathCD = 120;

        //float boredResetT;
        //float boredTimer;
        //float bReset;
        //float customAi1;
        //float drowningRisk;
        //float drownTimer;
        //float drownTimerMax;
        //float tBored;



        public override void AI()  //  warrior ai
        {
            tsorcRevampAIs.FighterAI(NPC, 1, .03f, 0.2f, true, 10, false, SoundID.Mummy, 1000, 0.3f, 1.1f, true);


            #region melee movement

            Player player3 = Main.player[NPC.target];

            //CHANCE TO JUMP FORWARDS 
            if (NPC.Distance(player3.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(28) && NPC.life >= 1000)
            {
                int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust2].noGravity = true;

                NPC.TargetClosest(true);

                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 2f);
                NPC.netUpdate = true;

            }
            //CHANCE TO DASH STEP FORWARDS 
            else if (NPC.Distance(player3.Center) > 350 && NPC.velocity.Y == 0f && Main.rand.NextBool(28) && NPC.life >= 1000) //was 5
            {
                int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust3].noGravity = true;



                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(6f, 5f);
                //npc.TargetClosest(true);
                //npc.velocity.X = npc.velocity.X * 5f; // burst forward

                //if ((float)npc.direction * npc.velocity.X > 5)
                //    npc.velocity.X = (float)npc.direction * 5;  //  but cap at top speed

                NPC.netUpdate = true;
            }


            //CHANCE TO JUMP BEFORE ATTACK  npc.localAI[1]  >= 103 && 
            if (NPC.localAI[1] >= 103 && Main.rand.NextBool(20) && NPC.life >= 1001)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-4f, -2f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;
            }

            if (NPC.localAI[1] >= 113 && Main.rand.NextBool(20) && NPC.life >= 1001)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 1f);
                NPC.netUpdate = true;
            }

            if (NPC.localAI[1] >= 145 && Main.rand.NextBool(3) && NPC.life <= 1000)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-11f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(1f, 0f);
                NPC.netUpdate = true;

            }


            //OFFENSIVE JUMPS  npc.localAI[1]  <= 186 
            Player player4 = Main.player[NPC.target];
            if (NPC.localAI[1] >= 100 && NPC.velocity.Y == 0f && NPC.Distance(player4.Center) > 220 && NPC.life >= 1000)
            {
                //CHANCE TO JUMP 
                if (Main.rand.NextBool(24))
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2))
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y);

                    }
                    NPC.velocity.Y = -8f; //9             
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(5f, 1f);
                    NPC.netUpdate = true;
                    //npc.TargetClosest(true);

                    //npc.localAI[1]  = 165;


                }
            }
            #endregion


            #region Charge
            //CHARGE UNTIL DESPERATE PHASE
            if (Main.netMode != 1)
            {
                Player player = Main.player[NPC.target];
                if (NPC.localAI[1] >= 95 && Main.rand.NextBool(30) && NPC.Distance(player.Center) > 250 && NPC.Distance(player.Center) < 500 && NPC.life >= 1001)
                {
                    Lighting.AddLight(NPC.Center, Color.LightYellow.ToVector3() * 3f);
                    int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
                    Main.dust[dust2].noGravity = true;

                    //chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(3f, 2f);
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                    //chargeDamageFlag = false;
                    NPC.damage = 80;
                    //chargeDamage = 0;
                    NPC.localAI[1] = 1f;
                }

            }
            #endregion




            #region Projectiles
            if (Main.netMode != 1)
            {
                

                NPC.localAI[1]++;
                NPC.localAI[2]++;

                //MAKE SOUND WHEN JUMPING/HOVERING
                if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
                }

                if (NPC.localAI[2] >= 300 && NPC.life >= 1000)
                {
                    //BREATH ATTACK
                    

                    if (NPC.localAI[2] >= 301 && NPC.localAI[2] <= 395 && NPC.Distance(player.Center) > 20 && NPC.life >= 1001)
                    {
                        
                        if (NPC.localAI[2] == 301)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.01f, Pitch = -0.5f }, NPC.Center); //magic mirror
                        }

                        NPC.velocity.X = 0f;
                        NPC.velocity.Y = 0f;


                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);

                        if (Main.rand.NextBool(2))
                        {
                            

                            Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1.0f); //purple magic outward fire
                            Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 1.0f);
                            Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 2.0f); //purple magic outward fire
                            Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 21, 0, 0, 50, Color.Yellow, 2.0f);

                            
                        }

                    }
                    if (NPC.localAI[2] == 396)
                    {
                        breath = true; 
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit30 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center); //3, 21 demon; 3,30 nimbus
                    }

                    if (breath)
                    {

                        NPC.velocity.X = 0f;
                        NPC.velocity.Y = 0f;
                        Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 3f);
                       

                        //play breath sound
                        if (Main.rand.NextBool(3))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
                        }

                        float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y /*+ (5f * npc.direction)*/, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 2), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer); //JungleWyvernFire      cursed dragons breath
                        }
                        NPC.netUpdate = true;

                        //if (Main.rand.NextBool(35))
                        //{
                            //int num65 = Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X + Main.rand.Next(-500, 500), npc.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + Main.rand.Next(-100, 100), NPC.Center.Y + Main.rand.Next(-100, 100), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), bioSpitDamage, 0f, Main.myPlayer);
                        //}
                        breathCD--;

                        if (breathCD == 120)
                        {
                            tsorcRevampAIs.Teleport(NPC, 15, true);
                        }
                        if (breathCD == 60)
                        {
                            tsorcRevampAIs.Teleport(NPC, 15, true);
                        }
                        //
                    }

                    if (breathCD <= 0)
                    {
                        breath = false;
                        breathCD = 160;
                        NPC.localAI[2] = 1f;
                        NPC.localAI[1] = 1f;
                        NPC.ai[3] = 0; //Reset bored counter. No teleporting after breath attack

                    }

                }

                //TELEGRAPH DUSTS
                if (NPC.localAI[1] >= 85)
                {
                    Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(3))
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                    }

                }

                if (NPC.localAI[1] >= 95f)
                {

                    int choice = Main.rand.Next(4);
                    //PURPLE MAGIC LOB ATTACK; 
                    if (NPC.localAI[1] >= 110f && NPC.life >= 1001 && choice == 0)
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
                                int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);
                                //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                                //DesertDjinnCurse; ProjectileID.DD2DrakinShot

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                            }
                            if (NPC.localAI[1] >= 154f)
                            { NPC.localAI[1] = 1f; }
                        }

                    }

                    NPC.TargetClosest(true);
                    Player player = Main.player[NPC.target];


                    //HYPNOTIC DISRUPTER ATTACK
                    if (Main.rand.NextBool(150) && NPC.Distance(player.Center) > 230 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                    {

                        Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6f, 1.06f, true, true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disruptDamage, 5f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center); //wobble


                        NPC.localAI[1] = 1f;
                        NPC.netUpdate = true;
                    }

                    //JUMP DASH 
                    if (NPC.localAI[1] >= 110 && NPC.velocity.Y == 0f && Main.rand.NextBool(40) && NPC.life >= 1001)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -2f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(5f, 2f);
                        NPC.netUpdate = true;
                    }


                    //MULTI-SPIT 1 ATTACK
                    if (NPC.localAI[1] >= 105f && choice == 1 && Main.rand.NextBool(8) && NPC.life >= 2001 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                    {

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        }

                        if (NPC.localAI[1] >= 114f)
                        {
                            NPC.localAI[1] = 1f;
                        }
                        //Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, [target], [speed], [projectile gravity], [should it aim high ?], [what happens if enemy is too far away to hit ?]);
                        //Then use projectileVelocity as "velocity" when you call NewProjectile, like
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center, projectileVelocity, ModContent.ProjectileType <[your projectile] > (), damage, knockBack, Main.myPlayer);

                        //Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, player, 6, true, true, false);
                        NPC.netUpdate = true;
                    }

                    //MULTI-SPIT 2 ATTACK
                    if (NPC.localAI[1] >= 113f && choice >= 2 && Main.rand.NextBool(8) && NPC.life >= 1001 && NPC.life <= 2000)
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
                        NPC.netUpdate = true;
                    }

                    //JUMP DASH 
                    if (NPC.localAI[1] >= 150 && NPC.velocity.Y == 0f && Main.rand.NextBool(20) && NPC.life <= 1000)
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
                        Main.dust[dust2].noGravity = true;

                        //npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
                        NPC.netUpdate = true;
                    }

                    //FINAL DESPERATE ATTACK
                    if (NPC.localAI[1] >= 155f && NPC.life <= 1000)
                    
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
                }
            }

           
        }

        #endregion

        #region FindFrame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player player, int target, bool crit)
        {


            player.AddBuff(36, 600, false); //broken armor
            player.AddBuff(20, 1800, false); //poisoned
            player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100

            if (Main.rand.NextBool(2))
            {
                
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
                
            }


        }
        #endregion


        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                }
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.CursedSoul>(), 1, 3, 6));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}