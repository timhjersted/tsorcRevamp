using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class BasiliskShifter : ModNPC
    {
        //HARD MODE VARIANT 
        public override void SetDefaults()
        {
            NPC.npcSlots = 2;
            Main.npcFrameCount[NPC.type] = 12;
            AnimationType = 28;


            NPC.aiStyle = 3;
            NPC.damage = 60;
            NPC.defense = 60;
            NPC.height = 54;
            NPC.width = 54;
            NPC.lifeMax = 570; //was 870
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2000;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BasiliskShifterBanner>();

            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[24] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            NPC.defense = (int)(NPC.defense * (2 / 3));
            cursedBreathDamage = (int)(cursedBreathDamage / 2);
            darkExplosionDamage = (int)(darkExplosionDamage / 2);
            hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
            bioSpitDamage = (int)(bioSpitDamage / 2);
        }

        int choice = 2;
        float breathTimer = 60;
        //int previous = 0;
        //bool breath = false;

        float shotTimer;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        int cursedBreathDamage = 25;
        int darkExplosionDamage = 35;
        int hypnoticDisruptorDamage = 35;
        int bioSpitDamage = 35;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            // P.townNPCs > 0f // is no town NPCs nearby

            if (spawnInfo.Water) return 0f;

            //SPAWNS IN HM JUNGLE AT NIGHT ABOVE GROUND AFTER THE RAGE IS DEFEATED
            if (Main.hardMode && Jungle && !Corruption && !Main.dayTime && AboveEarth && !Ocean && P.townNPCs <= 0f && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.rand.Next(30) == 1) return 1;

            //SPAWNS IN HM METEOR UNDERGROUND AT NIGHT
            if (Main.hardMode && Meteor && !Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.Next(10) == 1) return 1;

            if (Main.hardMode && Meteor && Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.Next(20) == 1) return 1;

            //SPAWNS AGAIN IN CORRUPTION AND NOW CRIMSON
            if (Main.hardMode && Corruption && !Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.Next(20) == 1) return 1;

            if (Main.hardMode && Corruption && Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.Next(30) == 1) return 1;

            //SPAWNS IN DUNGEON AT NIGHT RARELY
            if (Main.hardMode && Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.Next(40) == 1) return 1;

            //SPAWNS IN HM HALLOW 
            if (Main.hardMode && (InBrownLayer || InGrayLayer) && Hallow && !Ocean && !spawnInfo.Water && Main.rand.Next(30) == 1) return 1;

            //SPAWNS RARELY IN HM JUNGLE UNDERGROUND
            if (Main.hardMode && Jungle && InGrayLayer && !Ocean && !spawnInfo.Water && Main.rand.Next(70) == 1) return 1;

            //BLOODMOON HIGH SPAWN IN METEOR OR JUNGLE
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && (Meteor || Jungle) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.bloodMoon && Main.rand.Next(5) == 1) return 1;

            return 0;
        }
        #endregion

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            tsorcRevampAIs.FighterAI(NPC, 1, 0.03f, canTeleport: true, randomSound: SoundID.Mummy, soundFrequency: 1000, enragePercent: 0.1f, enrageTopSpeed: 2);

            shotTimer++;

            if (shotTimer >= 85)
            {
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 1f);
                if (Main.rand.Next(3) == 1)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                }


                if (shotTimer >= 100f)
                {
                    NPC.TargetClosest(true);
                    //DISRUPTOR ATTACK
                    Player player3 = Main.player[NPC.target];
                    if (Main.rand.Next(200) == 1 && NPC.Distance(player3.Center) > 190)
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
                    if (shotTimer == 105 && Main.rand.Next(3) == 0 && NPC.life >= 221)
                    {
                        //npc.velocity.Y = -6f;
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -4f);
                    }
                    //FOR FINAL
                    if (shotTimer >= 185 && Main.rand.Next(2) == 0 && NPC.life <= 220)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, 3f);
                    }


                    //BREATH ATTACK
                    /*
					if (shotTimer >= 110f && Main.rand.Next(20) == 0 && npc.Distance(player.Center) > 260 && npc.life >= 221)
					{
						npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
						npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(6f, 3f);
						//if (Main.rand.Next(2) == 1)
						//{
						Lighting.AddLight(npc.Center, Color.BlueViolet.ToVector3() * 2f);
						//int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
						//Main.dust[dust].noGravity = true;
						//}

						if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
						{
							breath = true;
							Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.2f, 0.2f); //flamethrower
																										 //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
						}

						if (breath)
						{
							Lighting.AddLight(npc.Center, Color.BlueViolet.ToVector3() * 2f);
							float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
							Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y, (float)((Math.Cos(rotation) * 7) * -1), (float)((Math.Sin(rotation) * 15) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer); //7 was 15
							if (Main.rand.Next(30) == 0)
							{
								Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X + Main.rand.Next(-500, 500), npc.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
							}

							breathCD--;
						}
						if (breathCD <= 0)
						{
							breath = false;
							breathCD = 120;
							shotTimer = 1f;
						}
					}
					*/
                }

            }




            // NEW BREATH ATTACK 
            breathTimer++;
            if (breathTimer > 480 && Main.rand.Next(2) == 1 && shotTimer <= 99f && NPC.life >= 221)
            {
                breathTimer = -60;
                shotTimer = -60f;
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

            if (breathTimer > 360 && NPC.life >= 221)
            {
                shotTimer = -60f;
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                shotTimer = 1f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }

            int choice = Main.rand.Next(4);
            //PURPLE MAGIC LOB ATTACK; && Main.rand.Next(2) == 1
            if (shotTimer >= 110f && NPC.life >= 221 && choice <= 1)
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
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), bioSpitDamage, 0f, Main.myPlayer);
                        //DD2DrakinShot; DesertDjinnCurse; ProjectileID.DD2DrakinShot
                        //if (projectile_velocity <= 0f)
                        //{ Main.projectile[lob].tileCollide = false; }
                        //else if (projectile_velocity >= 1f)
                        //{ Main.projectile[lob].tileCollide = true; }

                        //Main.projectile[lob].hostile = true;
                        //Main.projectile[num555].timeLeft = 300; //40
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                    }

                    if (shotTimer >= 154f)
                    {
                        shotTimer = 1f;
                    }
                }
            }

            //NORMAL SPIT ATTACK
            if (shotTimer >= 115f && NPC.life >= 221 && choice >= 2)
            {
                if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9);

                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int num555 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
                        Main.projectile[num555].timeLeft = 300; //40
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        shotTimer = 1f;
                    }
                }
            }

            //FINAL DESPERATE ATTACK
            if (shotTimer >= 175f && Main.rand.Next(2) == 1 && NPC.life <= 220)
            {
                int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
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


            //Knockback conditional
            if (NPC.life >= 221)
            {
                NPC.knockBackResist = 0.04f;
            }
            else
            {
                NPC.knockBackResist = 0.0f;
            }

            //MAKE SOUND WHEN JUMPING/HOVERING
            if (Main.rand.Next(12) == 0 && NPC.velocity.Y <= -1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
            }

            //TELEGRAPH DUSTS
            if (shotTimer >= 100)
            {
                Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(3) == 1)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
                    //Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemEmerald, npc.velocity.X, npc.velocity.Y);
                }
            }

            //reset attack timer when hit in melee range
            if (NPC.justHit && NPC.Distance(player.Center) < 100)
            {
                shotTimer = 10f;
            }

            //jump back when hit at close range; && npc.life >= 221
            if (NPC.justHit && NPC.Distance(player.Center) < 150 && Main.rand.Next(2) == 1)
            {

                NPC.velocity.Y = Main.rand.NextFloat(-6f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-7f, -4f);
                shotTimer = 50f;
                NPC.netUpdate = true;
            }

            //jump forward when hit at range; && npc.life >= 221
            if (NPC.justHit && NPC.Distance(player.Center) > 150 && Main.rand.Next(2) == 1)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(7f, 3f);
                NPC.netUpdate = true;

            }

            //Shift toward the player randomly
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.Next(80) == 1 && NPC.Distance(player.Center) > 200)
                {
                    Lighting.AddLight(NPC.Center, Color.Red.ToVector3() * 3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.netUpdate = true;

                }
                if (chargeDamageFlag == true)
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

                    NPC.damage = 70;
                    chargeDamage++;
                }
                if (chargeDamage >= 70)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 60;
                    chargeDamage = 0;
                }
            }
        }

        #region Find Frame
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
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(37, 10800, false); //horrified
                player.AddBuff(20, 600, false); //poisoned

            }
            if (Main.rand.Next(8) == 0)
            {
                player.AddBuff(36, 600, false); //broken armor
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
                player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
            }
        }
        #endregion

        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
            for (int i = 0; i < 10; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
            }

            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion);
        }
    }
}