using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.Fiends;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Gwyn : ModNPC
    {



        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            Music = 12;
            NPC.damage = 205; //was 295
            NPC.defense = 200;
            NPC.lifeMax = 500000;
            NPC.knockBackResist = 0;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1000000;
            despawnHandler = new NPCDespawnHandler("You have fallen before the Lord of Cinder...", Color.OrangeRed, 6);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gwyn, Lord of Cinder");
        }

        //old attacks
        int deathBallDamage = 200; //200
        int phantomSeekerDamage = 180; //225
        int armageddonBallDamage = 200; //300
        int holdBallDamage = 35;
        int fireballBallDamage = 145;
        int blazeBallDamage = 55;
        int blackBreathDamage = 90;
        int purpleCrushDamage = 155;
        int iceStormDamage = 150;
        int gravityBallDamage = 150;//300

        //basilisk attacks
        int cursedBreathDamage = 106; //100
        int cursedFlamesDamage = 102; //100
        int darkExplosionDamage = 135;
        int disruptDamage = 175;//203
        int bioSpitDamage = 135;//185
        int bioSpitfinalDamage = 145;//230

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            //NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            deathBallDamage = (int)(deathBallDamage / 2);
            phantomSeekerDamage = (int)(phantomSeekerDamage / 2);
            armageddonBallDamage = (int)(armageddonBallDamage / 2);
            holdBallDamage = (int)(holdBallDamage / 2);
            fireballBallDamage = (int)(fireballBallDamage / 2);
            blazeBallDamage = (int)(blazeBallDamage / 2);
            blackBreathDamage = (int)(blackBreathDamage / 2);
            purpleCrushDamage = (int)(purpleCrushDamage / 2);
            fireBreathDamage = (int)(fireBreathDamage / 2);
            iceStormDamage = (int)(iceStormDamage / 2);
            gravityBallDamage = (int)(gravityBallDamage / 2);
            herosArrowDamage = (int)(herosArrowDamage / 2);
            throwingKnifeDamage = (int)(throwingKnifeDamage / 2);
            smokebombDamage = (int)(smokebombDamage / 2);
            tridentDamage = (int)(tridentDamage / 2);
            redMagicDamage = (int)(redMagicDamage / 2);
            cultistFireDamage = (int)(cultistFireDamage / 2);
            cultistMagicDamage = (int)(cultistMagicDamage / 2);
            cultistLightningDamage = (int)(cultistLightningDamage / 2);
            //fireBreathDamage = (int)(fireBreathDamage / 2);
            lostSoulDamage = (int)(lostSoulDamage / 2);
            greatFireballDamage = (int)(greatFireballDamage / 2);
            blackFireDamage = (int)(blackFireDamage / 2);
            greatAttackDamage = (int)(greatAttackDamage / 2);
        }

        //ultimate attack not used yet
        public int redMagicDamage = 190;//190

        //lumelia attacks
        public int throwingKnifeDamage = 140;//180
        public int smokebombDamage = 180;//295

        //death skull attack when player gets too far away
        public int herosArrowDamage = 210; //400

        //slogra attacks
        public int tridentDamage = 160; //150
        //Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
        public int burningSphereDamage = 290;//360

        //gwyn + hero of lumelia
        float customAi1;
        float customAi3;
        float customspawn1;
        float customspawn2;
        
        bool OptionSpawned = false;

        //basilisk
        bool breath;
        int breathCD = 120;
        float breathTimer = 60;
        float shotTimer;
        int hypnoticDisruptorDamage = 145;//145

        //slogra
        bool swordDead = false;
        int moveTimer = 0;
        bool dashAttack = false;
        Vector2 pickedTrajectory = Vector2.Zero;
        int baseCooldown = 240;
        int lineOfSightTimer = 0;

        //ancient demon
        int cultistFireDamage = 152;//192
        int cultistMagicDamage = 230;//259
        int cultistLightningDamage = 185;//260
        int fireBreathDamage = 131;//131
        int lostSoulDamage = 190;//223
        int greatFireballDamage = 216;//216
        int blackFireDamage = 147;//147
        int greatAttackDamage = 162;//162

        int demonBreathTimer = 0;

        //chaos
        int holdTimer = 0;

        #region debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {

            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;

            player.AddBuff(24, 600, false); //on fire
            player.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 1200, false); //lose defense on hit
            player.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1200, false); //slowed life regen
            player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false); //you lose knockback resistance
            player.AddBuff(ModContent.BuffType<Buffs.TornWings>(), 1800, false); //you lose flight
            if (Main.rand.NextBool(2))
            {
                player.AddBuff(33, 7200, false); //weak
                player.AddBuff(36, 180, false); //broken armor

            }
        }
        #endregion


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {

            //chaos defense move

            if (holdTimer > 0)
            {
                holdTimer--;
            }
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 1000)
            {
                NPC.defense = 9999;
                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("Gwyn is protected by the soul of cinder -- you're too far away!", 175, 75, 255);
                    holdTimer = 200;
                }
                else
                {
                    NPC.defense = 200;
                }
            }

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
            int num58;
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 0.2f);
            Main.dust[dust].noGravity = true;

            Player player = Main.player[NPC.target];

            //DEBUFFS
            if (NPC.Distance(player.Center) < 800)
            {
                player.AddBuff(ModContent.BuffType<Buffs.TornWings>(), 60, false);
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 60, false);
            }

            bool tooEarly = !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<EarthFiendLich>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<FireFiendMarilith>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<WaterFiendKraken>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Blight>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Chaos>()) || !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<GhostWyvernMage.WyvernMageShadow>());
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





            #region Projectiles
            if (Main.netMode != 1)
            {

                //250,000 and less
                //ANCIENT DEMON 

                if (NPC.life <= 250000)
                {

                    int demonChoice = Main.rand.Next(6);
                    NPC.localAI[3]++;

                    //play creature sounds
                    if (Main.rand.NextBool(7700))
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.5f }, NPC.Center);
                        //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
                    }



                    bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
                    //tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: false, lavaJumping: true);
                    tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.NextBool(220), false, SoundID.Item17);
                    tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistMagicDamage, 1, Main.rand.NextBool(20), false, SoundID.NPCHit34);

                    //CHANCE TO JUMP BEFORE ATTACK  && NPC.life >= 25001
                    if (NPC.localAI[3] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(50) && NPC.life >= 75001 && NPC.life <= 250000)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-9f, -6f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                        NPC.netUpdate = true;
                    }

                    if (NPC.localAI[3] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(33) && NPC.life <= 75000)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-7f, -4f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                        NPC.netUpdate = true;

                    }


                    //EARLY TELEGRAPH
                    if (NPC.localAI[3] >= 60)
                    {
                        Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(6))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y); //pink dusts


                        }
                    }
                    //LAST SECOND TELEGRAPH
                    if (NPC.localAI[3] >= 110)
                    {
                        Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(2))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                        }
                    }

                    //if(NPC.life <= 150000)
                    //{ 
                    if (demonBreathTimer == 350 && Main.rand.NextBool(3))
                    {
                        demonBreathTimer = 1;
                    }
                    // NEW BREATH ATTACK 
                    demonBreathTimer++;

                    if (demonBreathTimer > 480)
                    {
                        NPC.localAI[3] = -70;
                        if (NPC.life >= 75001)
                        { demonBreathTimer = -60; }
                        if (NPC.life <= 75000)
                        { demonBreathTimer = -160; }

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
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.Torch, 48, 4);
                        Lighting.AddLight(NPC.Center * 2, Color.Red.ToVector3() * 5);
                    }

                    if (demonBreathTimer == 0)
                    {
                        NPC.localAI[3] = -150;
                        //npc.TargetClosest(true);
                        NPC.velocity.X = 0f;

                    }
                    //}

                    //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
                    Player player3 = Main.player[NPC.target];
                    if (Main.rand.NextBool(90) && NPC.Distance(player3.Center) > 600 && NPC.Distance(player3.Center) < 1000)
                    {
                        Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
                        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center); //wobble
                        NPC.localAI[3] = 1f;

                        NPC.netUpdate = true;
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
                        //LOB ATTACK PURPLE; 
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
                                int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                                if (NPC.localAI[3] >= 186f)
                                { NPC.localAI[3] = 1f; }
                            }

                        }

                    }

                    //NPC.TargetClosest(true);
                    //Player player = Main.player[npc.target];





                    //MULTI-FIRE 1 ATTACK && NPC.life <= 400000 && NPC.life >= 350001 
                    if (NPC.localAI[3] >= 160f && demonChoice == 1) //&& Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)
                    {

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                        //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                        if (Main.rand.NextBool(3) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        }

                        if (NPC.localAI[3] >= 175f)
                        {
                            NPC.localAI[3] = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                    //MULTI-BOUNCING DESPERATE FIRE ATTACK && NPC.life >= 300001 && NPC.life <= 350000 
                    if (NPC.localAI[3] >= 160f && (demonChoice == 1 || demonChoice == 2))
                    {
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                        speed.Y += Main.rand.NextFloat(2f, -2f);
                        if (Main.rand.NextBool(2) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center); //fire

                        }

                        if (NPC.localAI[3] >= 180f) //was 190
                        {
                            NPC.localAI[3] = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                    //LIGHTNING ATTACK
                    if (NPC.localAI[3] == 160f && (demonChoice == 5 || demonChoice == 4))
                    {

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);


                        speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6


                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                            //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                            //DesertDjinnCurse; ProjectileID.DD2DrakinShot

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        }

                        NPC.localAI[3] = -50f;


                    }


                    //FINAL JUNGLE FLAMES DESPERATE ATTACK
                    if (NPC.localAI[3] >= 160f && NPC.life <= 75000 && (demonChoice == 0 || demonChoice == 3))
                    //if (Main.rand.NextBool(40))
                    {
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(2))
                        {
                            int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.OrangeRed, 1f);
                            Main.dust[dust3].noGravity = true;
                        }
                        //NPC.velocity.Y = Main.rand.NextFloat(-3f, -1f);

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


                        NPC.netUpdate = true;

                    }
                }
                //END DEMON

                


                customAi1++; ;
                customAi3++; ;




                //JUSTHIT CODE

                if (NPC.justHit && NPC.Distance(player.Center) < 150)
                {
                    customAi1 = 1f;
                    customAi3 = 1f;
                }
                if (NPC.justHit && NPC.Distance(player.Center) < 350 && Main.rand.NextBool(3))//
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f); //was 6 and 3
                    float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-6f, -4f);
                    NPC.velocity.X = v;
                    if (Main.rand.NextBool(2))
                    { customAi1 = 70f; } //was 100 but knife goes away and doesn't shoot consistently
                    else
                    { customAi1 = 240f; }


                    NPC.netUpdate = true;
                }
                if (NPC.justHit && NPC.Distance(player.Center) > 351 && Main.rand.NextBool(3))
                {
                    NPC.knockBackResist = 0f;
                    NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(9f, 3f);
                    NPC.netUpdate = true;

                }


                //SPAWN GREAT KNIGHTS
                if (customAi1 >= 10f)
                {
                    if ((customspawn1 < 1) && Main.rand.NextBool(15000))
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.GreatRedKnight>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        NPC.ai[0] = 20 - Main.rand.Next(80);
                        customspawn1 += 1f;
                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }
                    if ((customspawn2 < 2) && Main.rand.NextBool(15000))
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.RingedKnight>(), 0);
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



                    NPC.TargetClosest(true);

                    //old projectiles
                    //orange phantom seeker
                    if (customAi3 >= 230 && Main.rand.NextBool(250))
                    {
                        num58 = Projectile.NewProjectile(NPC.GetSource_FromThis(), this.NPC.position.X + 20, this.NPC.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
                        Main.projectile[num58].timeLeft = 460;
                        Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
                        Main.projectile[num58].ai[0] = this.NPC.target;


                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi3 = 160f;

                        NPC.netUpdate = true;
                    }

                    if (customAi3 >= 170 && Main.rand.NextBool(200))
                    {
                        float num48 = 4f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity4Ball>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, armageddonBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 120;
                            Main.projectile[num54].aiStyle = -1;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi3 = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                   


                    //ARROWS FROM ARCHERS NEARBY ATTACK
                    if (NPC.Distance(player.Center) < 600 && Main.rand.NextBool(350))
                    {
                        Player nT = Main.player[NPC.target];


                        if (Main.rand.NextBool(4))
                        {
                            UsefulFunctions.BroadcastText("Gwyn's fury increases!", 175, 75, 255);
                        }

                        for (int pcy = 0; pcy < 5; pcy++)
                        {
                            //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 600f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), herosArrowDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower

                            NPC.netUpdate = true;

                        }
                    }

                    //ARROWS FROM ARCHERS IN THE DISTANCE ATTACK
                    if (NPC.Distance(player.Center) > 650 && Main.rand.NextBool(100))
                    {
                        Player nT = Main.player[NPC.target];

                        if (Main.rand.NextBool(4))
                        {
                            UsefulFunctions.BroadcastText("Gwyn's fury increases!", 175, 75, 255);
                        }

                        for (int pcy = 0; pcy < 6; pcy++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 250 + Main.rand.Next(500), (float)nT.position.Y - 600f, (float)(-50 + Main.rand.Next(100)) / 10, 5.1f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), herosArrowDamage, 2f, Main.myPlayer); //was 8.9f near 10, tried Main.rand.Next(2, 5)

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
                            //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, NPC.Center);

                            NPC.netUpdate = true;

                        }
                    }



                    //JUMP BEFORE KNIFE ATTACK SOMETIMES
                    if (customAi1 == 130f && NPC.velocity.Y == 0f && NPC.life >= 50001 && NPC.life <= 250001 && Main.rand.NextBool(2))
                    //if (customAi1 >= 130f && customAi1 <= 131f && npc.velocity.Y == 0f && Main.rand.NextBool(2))
                    {

                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -5f);

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8); //0.4f, true, true																								
                        speed += Main.rand.NextVector2Circular(-4, -2);
                        if (Main.rand.NextBool(4) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            //customAi1 = 132f;
                        }

                        NPC.netUpdate = true;
                    }

                    //DESPERATE FINAL ATTACK
                    if (customAi1 >= 130f && customAi1 <= 148f && NPC.life <= 250000 && NPC.life >= 200001)
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
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

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
                    if (customAi1 >= 220 && customAi1 >= 280 && NPC.life >= 50001)
                    {
                        Lighting.AddLight(NPC.Center, Color.Green.ToVector3() * 1f);
                        if (Main.rand.NextBool(2) && NPC.Distance(player.Center) > 1)
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                        }

                        //JUMP BEFORE BOMB ATTACK SOMETIMES
                        if (customAi1 == 260f && NPC.velocity.Y == 0f && NPC.life >= 50001 && Main.rand.NextBool(2))
                        {
                            NPC.velocity.Y = Main.rand.NextFloat(-8f, -4f);
                            NPC.netUpdate = true;
                        }

                        //SMOKE BOMB ATTACK
                        if (customAi1 >= 280 && NPC.life >= 50001) //&& npc.Distance(player.Center) > 10
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





                    //100k-250k
                    //BASILISK HUNTER ATTACKS
                    

                    if (NPC.life >= 250001 && NPC.life <= 400000)
                    {

                        NPC.localAI[1]++;
                        NPC.localAI[2]++;
                        //MAKE SOUND WHEN JUMPING/HOVERING
                        if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
                        }

                        if (NPC.localAI[2] >= 300)
                        {
                            //BREATH ATTACK


                            if (NPC.localAI[2] >= 301 && NPC.localAI[2] <= 395)
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

                                if (Main.rand.NextBool(35))
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + Main.rand.Next(-100, 100), NPC.Center.Y + Main.rand.Next(-100, 100), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), bioSpitDamage, 0f, Main.myPlayer);
                                }
                                breathCD--;

                                if (breathCD == 120)
                                {
                                    tsorcRevampAIs.Teleport(NPC, 15, true);
                                }
                                if (breathCD == 60)
                                {
                                    tsorcRevampAIs.Teleport(NPC, 15, true);
                                }

                            }

                            if (breathCD <= 0)
                            {
                                breath = false;
                                breathCD = 160;
                                NPC.localAI[2] = 1f;
                                NPC.localAI[1] = 1f;
                                //NPC.ai[3] = 0; //Reset bored counter. No teleporting after breath attack

                            }

                        }

                        //TELEGRAPH DUSTS
                        if (NPC.localAI[1] >= 85 && NPC.life >= 150001 && NPC.life <= 400000)
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
                            //PURPLE MAGIC LOB ATTACK;  && NPC.life >= 150001 && NPC.life <= 300000
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
                            //Player player = Main.player[NPC.target];


                            //HYPNOTIC DISRUPTER ATTACK
                            if (Main.rand.NextBool(150) && NPC.life >= 150001 && NPC.life <= 300000 && NPC.Distance(player.Center) > 230 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                            {

                                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6f, 1.06f, true, true);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), disruptDamage, 5f, Main.myPlayer);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center); //wobble

                                //THIS WAS USED BEFORE
                                NPC.localAI[1] = 1f;
                                NPC.netUpdate = true;
                            }

                            //JUMP DASH 
                            if (NPC.localAI[1] >= 110 && NPC.velocity.Y == 0f && Main.rand.NextBool(40) && NPC.life >= 150001 && NPC.life <= 300000)
                            {
                                NPC.velocity.Y = Main.rand.NextFloat(-10f, -2f);
                                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(5f, 2f);
                                NPC.netUpdate = true;
                            }


                            //MULTI-SPIT 1 ATTACK && NPC.life >= 150001 && NPC.life <= 300000
                            if (NPC.localAI[1] >= 105f && choice == 1 && Main.rand.NextBool(8) && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
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

                            //MULTI-SPIT 2 ATTACK && NPC.life >= 150001 && NPC.life <= 300000
                            if (NPC.localAI[1] >= 113f && choice >= 2 && Main.rand.NextBool(8))
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
                            if (NPC.localAI[1] >= 150 && NPC.velocity.Y == 0f && NPC.life >= 100001 && NPC.life <= 150000 && Main.rand.NextBool(20))
                            {
                                int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
                                Main.dust[dust2].noGravity = true;

                                //npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
                                NPC.netUpdate = true;
                            }

                            //FINAL DESPERATE ATTACK
                            if (NPC.localAI[1] >= 155f && NPC.life <= 250000 && NPC.life >= 200001)
                            //if (Main.rand.NextBool(40))
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
                        } //end basilisk hunter
                    }


                    //400,000-500,000
                    //BASILISK SHIFTER && NPC.life >= 350001 &&

                    shotTimer++;
                    int shifterChoice = Main.rand.Next(4);
                    if (shotTimer >= 85 && NPC.life >= 400001)
                    {
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 1f);
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                        }

                        //&& NPC.life <= 450000
                        if (shotTimer >= 100f && NPC.life >= 400001)
                        {
                            NPC.TargetClosest(true);
                            //DISRUPTOR ATTACK
                            //Player player3 = Main.player[NPC.target];
                            if (Main.rand.NextBool(180) && NPC.Distance(player.Center) > 190 && NPC.life >= 400001)
                            {
                                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 4f, 1.06f, true, true);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 5f, Main.myPlayer);
                                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, player.Center); //wobble
                                                                                                                                           //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                                shotTimer = 1f;

                                NPC.netUpdate = true;
                            }

                            //CHANCE TO JUMP BEFORE ATTACK
                            //FOR MAIN
                            if (shotTimer == 105 && Main.rand.NextBool(3) && NPC.life >= 430001)
                            {
                                //npc.velocity.Y = -6f;
                                NPC.velocity.Y = Main.rand.NextFloat(-10f, -4f);
                            }
                            //FOR FINAL
                            if (shotTimer >= 185 && Main.rand.NextBool(2) && NPC.life >= 400001 && NPC.life <= 430000)
                            {
                                NPC.velocity.Y = Main.rand.NextFloat(-10f, 3f);
                            }


                        }

                    }


                    // NEW BREATH ATTACK     && NPC.life <= 450001
                    breathTimer++;
                    if (breathTimer > 480 && Main.rand.NextBool(2) && shotTimer <= 99f && NPC.life >= 400001)
                    {
                        breathTimer = -90;
                        shotTimer = -120f; //was -90
                    }

                    if (breathTimer > 480 && Main.rand.NextBool(2) && shotTimer <= 99f && NPC.life <= 400000)
                    {
                        breathTimer = 0;
                        shotTimer = 0f;
                    }

                    if (breathTimer < 0)
                    {
                        NPC.velocity.X = 0f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                            NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                        }
                    }
                    //&& NPC.life >= 350001 && NPC.life <= 450001
                    if (breathTimer > 360)
                    {
                        shotTimer = -60f;
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
                    }

                    if (breathTimer == 0)
                    {
                        //replaced spinning circle in place attack with gravity ball
                        shotTimer = 1f;
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity1Ball>(), darkExplosionDamage, 0f, Main.myPlayer);
                    }


                    //PURPLE MAGIC LOB ATTACK; && Main.rand.NextBool(2)
                    if (shotTimer >= 110f && NPC.life >= 400001 && shifterChoice <= 1)
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

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);

                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                            }

                            if (shotTimer >= 154f)
                            {
                                shotTimer = 1f;
                            }
                        }
                    }

                    //NORMAL SPIT ATTACK
                    if (shotTimer >= 115f && NPC.life >= 430001 && shifterChoice >= 2)
                    {
                        if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                        {
                            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9);

                            if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                  /*int num555 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), tridentDamage, 0f, Main.myPlayer);
                                Main.projectile[num555].timeLeft = 300; //40
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                                shotTimer = 1f;
                                  */
                            }
                        }
                    }

                    //FINAL DESPERATE ATTACK
                    if (shotTimer >= 175f && Main.rand.NextBool(2) && NPC.life >= 400001 && NPC.life <= 430000)
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 0.5f);
                        Main.dust[dust2].noGravity = true;

                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                        if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
                            //Terraria.Audio.SoundEngine.PlaySound(4, (int)npc.position.X, (int)npc.position.Y, 9);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.1f }, NPC.Center);
                            //customAi1 = 1f;
                        }

                        if (shotTimer >= 206f)
                        {
                            shotTimer = 1f;
                        }
                    }


                    //MAKE SOUND WHEN JUMPING/HOVERING
                    if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f && NPC.life >= 400001)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
                    }

                    //TELEGRAPH DUSTS
                    if (shotTimer >= 100 && NPC.life >= 400001)
                    {
                        Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
                            //Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemEmerald, npc.velocity.X, npc.velocity.Y);
                        }
                    }

                    //reset attack timer when hit in melee range
                    if (NPC.justHit && NPC.Distance(player.Center) < 200 && Main.rand.NextBool(4))
                    {
                        shotTimer = 10f;
                    }

                    //jump back when hit at close range; && npc.life >= 221
                    if (NPC.justHit && NPC.Distance(player.Center) < 200 && Main.rand.NextBool(2))
                    {

                        NPC.velocity.Y = Main.rand.NextFloat(-6f, -4f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-7f, -4f);
                        shotTimer = 50f;
                        NPC.netUpdate = true;
                    }

                    //jump forward when hit at range; 
                    if (NPC.justHit && NPC.Distance(player.Center) > 200 && NPC.life >= 400001 && Main.rand.NextBool(2))
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                        NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(7f, 3f);
                        NPC.netUpdate = true;

                    }



















                    //BEGIN SLOGRA ABILITIES
                    moveTimer++;


                    if (swordDead)
                    {
                        
                        NPC.defense = 100; //Speed things up a bit
                        baseCooldown = 90; 
                    }

                    //&& NPC.life >= 400001 && NPC.life <= 200001
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
                        tsorcRevampAIs.FighterAI(NPC, 1, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 2);
                        tsorcRevampAIs.LeapAtPlayer(NPC, 7, 4, 1.5f, 128);
                    }


                    //Throw tridents
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float projectileDelay = 120; //120
                        if (swordDead)
                        {
                            projectileDelay = 90; //90
                        }
                        if (moveTimer % projectileDelay == 30 && moveTimer < baseCooldown) //&& NPC.life >= 250001
                        {
                            if (swordDead) //&& NPC.life >= 250001
                            {
                                for (int i = 0; i < 9; i++)
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
                                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8, .1f, true, true);
                                if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.player[NPC.target].velocity / 1.5f, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), gravityBallDamage, 0.5f, Main.myPlayer);
                                }
                            }

                        }
                    }

                    //Spawn dust telegraphing moves
                    if (moveTimer < baseCooldown + 70)
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

                    //Check if SwordOfLordGwyn is dead. If so we don't need to keep calling AnyNPCs. !swordDead && 

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
                        NPC.netUpdate = true;
                    }
                //} //closed spawn code before

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
                            //add back if BUGGED walking not working
                            tsorcRevampAIs.FighterAI(NPC, 7, 0.2f, 0.2f, true);
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
                        {
                            if (moveTimer % 15 == 0)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 7), ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 0.5f, Main.myPlayer);
                            }

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






                if (Main.rand.NextBool(400))
                {
                    float num48 = 4f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity4Ball>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, gravityBallDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 120;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }


                /*
                //what's this do?
                if (Main.rand.NextBool(220))
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }

                if (Main.rand.NextBool(350))
                {
                    float num48 = 10f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 100 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, holdBallDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 115;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        NPC.ai[1] = 1f;





                    }
                    NPC.netUpdate = true;
                }
                

                if (customAi3 >= 450 && Main.rand.NextBool(500))
                {
                    float num48 = 5f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 100;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi3 = 1f;
                    }
                    NPC.netUpdate = true;
                }
                /*
                if (Main.rand.NextBool(150))
                {
                    float num48 = 12f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireballBallDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 90;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }
                */

                /*
                if (Main.rand.NextBool(10))
                {
                    float num48 = 6f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blazeBallDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 0;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        NPC.ai[1] = 1f;
                    }
                    NPC.netUpdate = true;
                }
                */
                /*
                if (Main.rand.NextBool(200))
                {
                    float num48 = 9f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.BlackBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blackBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 200;
                        //Main.projectile[num54].aiStyle = 1;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }

                if (Main.rand.NextBool(100))
                {
                    float num48 = 5f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 1000 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, purpleCrushDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 3600;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }

                if (Main.rand.NextBool(100))
                {
                    float num48 = 11f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 400 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 200;
                        Main.projectile[num54].aiStyle = 23; //was 23
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }

                if (Main.rand.NextBool(66)) //might remove
                {
                    float num48 = 7f;
                    Vector2 vector8 = new Vector2(NPC.position.X - 1800 + (NPC.width * 0.5f), NPC.position.Y - 600 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 2000;
                        Main.projectile[num54].aiStyle = 23; //was 23
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }

                if (Main.rand.NextBool(16)) // might remove
                {
                    float num48 = 8f;
                    Vector2 vector8 = new Vector2(NPC.position.X + 1800 + (NPC.width * 0.5f), NPC.position.Y - 600 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 2000;
                        Main.projectile[num54].aiStyle = 23; //was 23
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }





                if (Main.rand.NextBool(200))
                {
                    float num48 = 6f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 1900;
                        Main.projectile[num54].aiStyle = 23;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }

                

     }

    */



            }
            #endregion
        }




        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (swordDead)
            {
                damage = (int)(damage * 1.25f);
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (swordDead)
            {
                damage = (int)(damage * 1.20f);
            }

            if (projectile.minion)
            {
                knockback = 0;
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
            float projectileDelay = 120;
            if (swordDead && OptionSpawned == true)
            {
                projectileDelay = 90;
            }
            if (moveTimer % projectileDelay <= 30 && moveTimer < baseCooldown)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


                if (OptionSpawned == true && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.SwordOfLordGwyn>()))
                {
                    ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                    data.Apply(null);
                }



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
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);

        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.GwynBag>()));
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }




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
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.5f); //was 0.1f

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
            if (customAi1 >= 220) //&& npc.Distance(player.Center) > 10 && customAi1 <= 280
            {
                //bomb sprite doesn't always show for some reason
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
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DraxEX>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Epilogue>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.EssenceOfTerraria>());
            }

            tsorcRevampWorld.InitiateTheEnd();
        }
        #endregion
    }
}
