using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    [AutoloadBossHead]
    class SeathTheScalelessHead : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 44; //44 works for both
            NPC.height = 44; //was 32 tried 64
            DrawOffsetY = 49; //was 60
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 300;
            NPC.defense = 120;
            NPC.HitSound = SoundID.NPCHit6;//6 is werewolf, 7 is the worst, generic hit sound evvarrr, 13, 21 worth trying
            NPC.DeathSound = SoundID.Item119;//good dragon death sound
            NPC.lifeMax = 300000;
            Music = 12;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.behindTiles = true;
            NPC.value = 670000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            //NPC.buffImmune[BuffID.OnFire] = false;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("Seath consumes your soul...", Color.Cyan, DustID.BlueFairy);
        }


        public int breathDamage = 96;
        public int smallShardDamage = 94;
        public int iceWaterDamage = 100;
        public int iceStormDamage = 100;
        public int largeShardDamage = 142;
        public float flapWings;
        public float FrostShotTimer;
        public float FrostShotCounter;
        public float FrostShot2Timer;
        public float FrostShot2Counter;


        public static int seathPieceSeperation = -5;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            smallShardDamage = (int)(smallShardDamage / 2);
            iceStormDamage = (int)(iceStormDamage / 2);
            largeShardDamage = (int)(largeShardDamage / 2);
            iceWaterDamage = (int)(iceWaterDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Seath the Scaleless");
        }


        int breathCD = 110;
        bool breath = false;
        bool firstCrystalSpawned = false;
        bool secondCrystalSpawned = false;
        bool finalCrystalsSpawned = false;
        float customspawn1;
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<Frostbite>(), 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 18000, false);
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false);
            
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;


            if (tsorcRevampWorld.SuperHardMode && (Sky || AboveEarth) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<SeathTheScalelessHead>())) && FrozenOcean && Main.rand.NextBool(100)) return 1;

            if (Main.hardMode && P.townNPCs > 2f && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<SeathTheScalelessHead>())) && !Main.dayTime && Main.rand.NextBool(1000))
            {
                UsefulFunctions.BroadcastText("The village is under attack!", 175, 75, 255);
                UsefulFunctions.BroadcastText("Seath the Scaleless has come to destroy all...", 175, 75, 255);
                return 1;
            }
            return 0;
        }
        #endregion



        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {

            //Can phase through walls if can't reach the player, + 100 / + 200 works great! but it goes into walls too easily (+10 and +100 is better, but could be tweaked further)
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 10)))
            {
               
                    NPC.noTileCollide = false;
                    NPC.noGravity = true;
                
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 100)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                //NPC.velocity.Y = 0f;
            }


            /*
            / Can phase through walls if can't reach the player, this version works great but seath is always hovering high above the player and doesn't target him with his head


            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 200)))
            {

                NPC.noTileCollide = false;
                NPC.noGravity = true;

            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height + 200)))
            {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                NPC.velocity.Y = 0f;
                if (NPC.position.Y > Main.player[NPC.target].position.Y)
                {
                    //NPC.velocity.Y -= 6f;
                }
                if (NPC.position.Y < Main.player[NPC.target].position.Y)
                {
                    //NPC.velocity.Y += 8f;
                }
            }
            */


            flapWings++;

            //Flap Wings
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.0f }, NPC.position); //wing flap sound

            }
            if (flapWings == 90)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.1f }, NPC.position);
                flapWings = 0;
            }



            int[] bodyTypes = new int[] { ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>(), ModContent.NPCType<SeathTheScalelessBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<SeathTheScalelessHead>(), bodyTypes, ModContent.NPCType<SeathTheScalelessTail>(), 13, SeathTheScalelessHead.seathPieceSeperation, 10f, 0.17f, true, false, true, false, false); //6f was replaced by seath separation

            //speed of dragon is hiding here, 22
            //tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<HellkiteDragonHead>(), bodyTypes, ModContent.NPCType<HellkiteDragonTail>(), 12, HellkiteDragonHead.hellkitePieceSeperation, 22, 0.25f, true, false, true, false, false); //30f was 10f

            if (despawnHandler.TargetAndDespawn(NPC.whoAmI))
            {
                for(int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<PrimordialCrystal>())
                    {
                        for (int j = 0; j < 50; j++)
                        {
                            int dust;
                            Vector2 vel = Main.rand.NextVector2Circular(20, 20);
                            dust = Dust.NewDust(Main.npc[i].Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                            Main.dust[dust].noGravity = true;
                            dust = Dust.NewDust(Main.npc[i].Center, 30, 30, 226, vel.X, vel.Y, 200, default, 3f);
                            Main.dust[dust].noGravity = true;
                        }
                        Main.npc[i].active = false;
                    }
                }

                return;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialCrystal>()))
            {
                NPC.dontTakeDamage = true;
            }
            else
            {
                NPC.dontTakeDamage = false;
            }

            //Crystal spawning
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.active)
            {
                if (Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    int crystalVelocity = 16;
                    if (!firstCrystalSpawned && NPC.life <= (2 * NPC.lifeMax / 3) || !secondCrystalSpawned && NPC.life <= (NPC.lifeMax / 3))
                    {
                        int crystal = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<PrimordialCrystal>(), default, NPC.whoAmI);
                        Main.npc[crystal].velocity = Main.rand.NextVector2CircularEdge(-crystalVelocity, crystalVelocity);
                        if (NPC.life >= (NPC.lifeMax / 2))
                        {
                            firstCrystalSpawned = true;
                        }
                        else
                        {
                            secondCrystalSpawned = true;
                        }
                        UsefulFunctions.BroadcastText("Seath calls upon a Primordial Crystal...", Color.Cyan);
                    }

                    if (!finalCrystalsSpawned && NPC.life <= (NPC.lifeMax / 6))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int crystal = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<PrimordialCrystal>(), default, NPC.whoAmI);
                            Main.npc[crystal].velocity = Main.rand.NextVector2CircularEdge(-crystalVelocity, crystalVelocity);
                        }
                        finalCrystalsSpawned = true;
                        UsefulFunctions.BroadcastText("Seath calls upon his final Primordial Crystals...", Color.Cyan);
                    }
                }
            }

            Player nT = Main.player[NPC.target];
            if (Main.rand.NextBool(255))
            {
                breath = true;
                
            }
            if (breath)
            {

                if (breathCD == 110)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.8f }, NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {

                    

                    //float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (2.5f * NPC.direction), NPC.Center.Y, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);



                    Vector2 spawnOffset = NPC.velocity; //Create a vector pointing in whatever direction the NPC is moving. We can transform this into an offset we can use.
                    spawnOffset.Normalize(); //Shorten the vector to make it have a length of 1
                    spawnOffset *= 80; //Multiply it so it has a length of 16. The length determines how far offset the projectile will be, 16 units = 1 tile

                    //float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)(NPC.Center.X + spawnOffset.X), (int)(NPC.Center.Y + spawnOffset.Y), NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);




                }
                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.4f, Pitch = -0.6f }, NPC.Center); //flame thrower
                }
                
                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 110; //was 110
                if (Main.rand.NextBool(4))
                {
                    FrostShotCounter = 0;
                }
                if (Main.rand.NextBool(2))
                {
                    FrostShot2Counter = 0;
                }

            }




            FrostShotTimer++;
            FrostShot2Timer++;

            //FROST SPACED ATTACK
            //Counts up each tick. Used to space out shots
            if (FrostShotTimer >= 26 && FrostShotCounter < 9)
            {

                if (Main.netMode != NetmodeID.MultiplayerClient) //ModContent.ProjectileType<Projectiles.Enemy.FrozenTear>()
                {

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 300 + Main.rand.Next(300), (float)nT.position.Y - 630f, (float)(-50 + Main.rand.Next(100)) / 10, 4f, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), smallShardDamage, 2.5f, Main.myPlayer); //ProjectileID.FrostBlastHostile //ProjectileID.FrostShard 5 was 10.1f was 14.9f is speed - 1f was 2f
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.1f, Pitch = 0.0f }, NPC.Center); //
                }
                
               

                FrostShotTimer = 0;
                FrostShotCounter++;

            }


            //FROST GROUP ATTACK
            //Counts up each tick. Used to space out shots
            if (FrostShot2Timer >= 8 && FrostShot2Counter < 3)
            {

                if (Main.netMode != NetmodeID.MultiplayerClient) //ModContent.ProjectileType<Projectiles.Enemy.FrozenTear>()
                {

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(100), (float)nT.position.Y - 600f, (float)(-5 + Main.rand.Next(50)) / 10, 3.1f, ProjectileID.FrostBlastHostile, smallShardDamage, 6f, Main.myPlayer); //ProjectileID.FrostBlastHostile //ModContent.ProjectileType<Projectiles.Enemy.Bubble>() ProjectileID.FrostShard 5 was 10.1f was 14.9f is speed - 1f was 2f
                    Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Volume = 0.2f, Pitch = 0.0f }, NPC.Center); //ice materialize - good, NPCHit5 is a nice ice sound
                      //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center); //flame thrower

                }
                
                

                FrostShot2Timer = 0;
                FrostShot2Counter++;

            }

            

                //massive ice crystal shards falling down   
                if (Main.rand.NextBool(250) && Main.netMode != NetmodeID.MultiplayerClient)
                {

                    float num48 = 6f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 1350 + Main.rand.Next(200)); //* 0.5
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.Okiku.MassiveCrystalShardsSpell>();
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, largeShardDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 100;
                        Main.projectile[num54].aiStyle = 4;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.2f, Pitch = -0.9f }, NPC.Center); //ice materialize - good
                        Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 2f);
                        Dust.NewDust(NPC.position, NPC.width * 2, NPC.height * 2, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                        NPC.ai[3] = 0; ;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = 0f }, NPC.Center); //item 29- sheen, 28- standard ice, item 30- magical ice
                        //Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 120, 0.3f, .1f); //ice mist howl sounds crazy
                        if (Main.rand.NextBool(7))
                        {
                            FrostShotCounter = 0;
                        }
                        if (Main.rand.NextBool(6))
                        {
                            FrostShot2Counter = 0;
                        }
                    }
                
                }

            
            //ice storm horizontal attack
            if (Main.rand.NextBool(180) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float num48 = 6f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2)); //.2 was .5
                float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                {
                    float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    num51 = num48 / num51;
                    speedX *= num51;
                    speedY *= num51;
                    int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
                    Main.projectile[num54].timeLeft = 1;
                    Main.projectile[num54].aiStyle = 1;
                    //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 120, 0.3f, .1f); //ice mist howl sounds crazy
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.1f, Pitch = -0.6f }, NPC.Center); //ice materialize - good
                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = -0.9f }, NPC.Center); //flame thrower
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, color, 3.0f);
                    Main.dust[dust].noGravity = true;
                    NPC.ai[3] = 0; ;

                   

                }
               
            }


            //FROST WAVE
            /*
            if (Main.rand.NextBool(150)) //was 1560
            {
                for (int pcy = 0; pcy < 2; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(100), (float)nT.position.Y - 600f, (float)(-100 + Main.rand.Next(100)) / 10, 1.1f, ProjectileID.FrostWave, iceWaterDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.2f, Pitch = 0.0f }, NPC.Center); //ice materialize - good
                        NPC.netUpdate = true;

                        Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        
                        
                        
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }
            
            */



            //spawn
            

                if ((customspawn1 < 50) && Main.rand.NextBool(2000))
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.SuperHardMode.IceSkeleton>(), 0); 
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    
                    customspawn1 += 1f;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            



            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueFairy, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }

            
        }
        #endregion

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        //Make it take damage as if its whole body was one entity
        //Whenever any of its parts takes a hit, it sets all other living parts to be immune for 5 frames
        //Does *not* apply to true melee attacks! 100% intentional, easy to change by calling SetImmune in OnHitByItem too if necessary
        public static void SetImmune(Projectile projectile, NPC hitNPC)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC currentNPC = Main.npc[i];
                if (currentNPC.type == ModContent.NPCType<SeathTheScalelessHead>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody2>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody3>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessLegs>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessTail>())
                {
                    currentNPC.immune[projectile.owner] = 1;//was 10
                }
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            SetImmune(projectile, NPC);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.SeathBag>()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.Center, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Seath the Scaleless Head Gore").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DragonEssence>(), 35 + Main.rand.Next(5));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 7000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BequeathedSoul>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BlueTearstoneRing>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.PurgingStone>());
            }            
        }

        public static Texture2D texture;
        public static void SeathInvulnerableEffect(NPC npc, SpriteBatch spriteBatch, ref Texture2D texture, float scale = 1.5f)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(npc.ModNPC.Texture);
            }
            if (npc.dontTakeDamage)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, Color.White, npc.rotation, origin, scale, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            }

        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathInvulnerableEffect(NPC, spriteBatch, ref texture, 1.1f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathTheScalelessHead.SeathInvulnerableEffect(NPC, spriteBatch, ref texture, 1.1f);
        }
    }
}