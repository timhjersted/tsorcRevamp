using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class AncientDemon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ancient Demon");
        }

        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.height = 120;
            NPC.width = 50;
            NPC.damage = 55;
            NPC.defense = 15;
            NPC.lifeMax = 9000;
            NPC.scale = 1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 76000;
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;

            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("An ancient demon decides to show you mercy ...", Color.Gold, DustID.GoldFlame);

            Main.npcFrameCount[NPC.type] = 16;


        }

        
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;

            bool oSky = (spawnInfo.SpawnTileY < (Main.maxTilesY * 0.1f));
            bool oSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.3f));
            bool oUnderground = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.4f));
            bool oCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.8f));
            if (p.ZoneDungeon || p.ZoneMeteor) return 0;
            //if (Main.hardMode && oUnderworld && Main.rand.Next(1000)==1) return true;
            //if (Main.hardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(100)==1) return true;
            //if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && oUnderworld && Main.rand.Next(30)==1) return true;
            //if (tsorcRevampWorld.SuperHardMode && oUnderworld && Main.rand.Next(35)==1) return true;

            if (spawnInfo.Player.ZoneUnderworldHeight)
            {
                if (!Main.dayTime && !Main.hardMode && !tsorcRevampWorld.SuperHardMode)
                {
                    if (Main.rand.NextBool(21000)) return 1;
                    else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.35f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && Main.rand.NextBool(10000)) return 1;
                    return 0;
                }

                if (Main.hardMode)
                {
                    bool hunterDowned = tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheHunter>());

                    if (hunterDowned && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()) && Main.rand.NextBool(100)) return 1;
                    if (hunterDowned && Main.rand.NextBool(800)) return 1;
                    if (hunterDowned && !Main.dayTime && Main.rand.NextBool(500)) return 1;
                    else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.25f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.rand.NextBool(500)) return 1;
                    return 0;
                }

                if (tsorcRevampWorld.SuperHardMode)
                {
                    if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()) && Main.rand.NextBool(10)) return 1;
                    if (Main.rand.NextBool(800)) return 1;
                    else if (!Main.dayTime && Main.rand.NextBool(400)) return 1;
                    return 0;
                }
                return 0;
            }
            return 0;
        }
        #endregion

        NPCDespawnHandler despawnHandler;
        int meteorDamage = 31;
        int cultistFireDamage = 72;
        int cultistMagicDamage = 99;
        int cultistLightningDamage = 85;
        int fireBreathDamage = 41;
        int lostSoulDamage = 63;


        int greatFireballDamage = 66;
        int blackFireDamage = 47;
        int greatAttackDamage = 62;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            meteorDamage = (int)(meteorDamage / 2);
            cultistFireDamage = (int)(cultistFireDamage / 2);
            cultistMagicDamage = (int)(cultistMagicDamage / 2);
            cultistLightningDamage = (int)(cultistLightningDamage / 2);
            fireBreathDamage = (int)(fireBreathDamage / 2);
            lostSoulDamage = (int)(lostSoulDamage / 2);
            greatFireballDamage = (int)(greatFireballDamage / 2);
            blackFireDamage = (int)(blackFireDamage / 2);
            greatAttackDamage = (int)(greatAttackDamage / 2);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
                target.AddBuff(20, 600, false); //poisoned
                target.AddBuff(30, 600, false); //bleeding
                target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 18000, false); //reduced defense on hit
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 HP after several hits
                target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 20;
  
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(33, 600, false); //weak
            }
        }


        public Player player
        {
            get => Main.player[NPC.target];
        }
        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(NPC, true);

            //JUSTHIT CODE
            //MELEE RANGE
            if (NPC.Distance(player.Center) < 100 && NPC.localAI[1] < 70f) //npc.justHit && 
            {
                NPC.localAI[1] = 50f;

                //TELEPORT MELEE
                if (Main.rand.NextBool(5))
                {
                    tsorcRevampAIs.Teleport(NPC, 25, true);
                }
            }
            //RISK ZONE
            if (NPC.Distance(player.Center) < 300 && NPC.localAI[1] < 70f && Main.rand.NextBool(5))//npc.justHit && 
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-10f, -7f);
                NPC.velocity.X = v;
                NPC.netUpdate = true;
            }

        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {

            //TELEPORT RANGED
            if (Main.rand.NextBool(12))
            {
                tsorcRevampAIs.Teleport(NPC, 20, true);
                NPC.localAI[1] = 70f;
            }
            //RANGED
            if (NPC.Distance(player.Center) > 201 && NPC.velocity.Y == 0f && Main.rand.NextBool(3))//npc.justHit &&
            {

                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(11f, 8f);
                NPC.netUpdate = true;

            }
        }


        
        //int breathTimer gives weird cool arrow shape, float does the circle
        float breathTimer = 0;
        int spawnedDemons = 0;
        public override void AI()
        {

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            int choice = Main.rand.Next(6);


            //CHANCE TO JUMP BEFORE ATTACK  
            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(40) && NPC.life >= 3001)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;
            }

            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(33) && NPC.life <= 3000)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-7f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;

            }

            //play creature sounds
            if (Main.rand.NextBool(1700))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f }, NPC.Center);
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
            }

            NPC.localAI[1]++;
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: true, lavaJumping: true);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.NextBool(220), false, SoundID.Item17);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistMagicDamage, 1, Main.rand.NextBool(20), false, SoundID.NPCHit34);
            

            //EARLY TELEGRAPH
            if (NPC.localAI[1] >= 60)
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
            if (NPC.localAI[1] >= 110)
            {
                Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                }
            }

            if (breathTimer == 350 && Main.rand.NextBool(3))
            {
                breathTimer = 1;
            }
            // NEW BREATH ATTACK 
            breathTimer++;
            if (breathTimer > 480)
            {
                NPC.localAI[1] = -50;
                if (NPC.life >= 3001)
                { breathTimer = -60; }
                if (NPC.life <= 3000)
                { breathTimer = -90; }

            }

            if (breathTimer == 470)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f }, NPC.Center); //shadowflame hex (little beasty)
            }

            if (breathTimer < 0)
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

                    
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y - 40f, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                    NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                    NPC.localAI[1] = -50;
                }
            }

            if (breathTimer == 361)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
            }
            if (breathTimer > 360)
            {
                NPC.localAI[1] = -50;
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.Torch, 48, 4);
                Lighting.AddLight(NPC.Center * 2, Color.Red.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                NPC.localAI[1] = -150;
                
                NPC.velocity.X = 0f;
               
            }

            //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
            Player player3 = Main.player[NPC.target];
            if (Main.rand.NextBool(50) && NPC.Distance(player3.Center) > 700)
            {
                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
               
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center); //wobble
                NPC.localAI[1] = 1f;

                NPC.netUpdate = true;
            }
            

            //SPAWN FIRE LURKER
            if ((spawnedDemons < 6) && NPC.life >= 2000 && Main.rand.NextBool(3000))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.FireLurker>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    spawnedDemons++;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }






            //CHOICES
            if (NPC.localAI[1] >= 160f && (choice == 0 || choice == 4) && NPC.life >= 3001)
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
                if (NPC.life >= 3001 && NPC.life <= 6000 && clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob2 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, fireBreathDamage, 0f, Main.myPlayer);

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -.5f }, NPC.Center);

                    }
                    if (NPC.localAI[1] >= 195f)
                    { NPC.localAI[1] = 1f; }
                }
                //LOB ATTACK >> BOUNCING FIRE
                if (NPC.life >= 3001 && clearSpace)

                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);
                    speed.Y += Main.rand.NextFloat(2f, -2f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        if (NPC.localAI[1] >= 186f)
                        { NPC.localAI[1] = 1f; }
                    }

                }

            }

            NPC.TargetClosest(true);
        

            //MULTI-FIRE 1 ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life >= 3001 && choice == 1) 
            {

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                if (Main.rand.NextBool(3) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                }

                if (NPC.localAI[1] >= 175f)
                {
                    NPC.localAI[1] = 1f;
                }
                NPC.netUpdate = true;
            }
            //MULTI-BOUNCING DESPERATE FIRE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= 3000 && (choice == 1 || choice == 2))
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                speed.Y += Main.rand.NextFloat(2f, -2f);
                if (Main.rand.NextBool(2) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center); //fire

                }

                if (NPC.localAI[1] >= 190f) //was 126
                {
                    NPC.localAI[1] = 1f;
                }
                NPC.netUpdate = true;
            }
            //LIGHTNING ATTACK
            if (NPC.localAI[1] == 160f && NPC.life >= 1001 && NPC.life <= 6000 && (choice == 5 || choice == 4)) //&& Main.rand.NextBool(8) 
            {
                //&& Main.rand.NextBool(10) Main.rand.NextBool(2) &&
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);
                //speed += Main.player[npc.target].velocity / 4;

                speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6

                
                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                    //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                    

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                }
               
                NPC.localAI[1] = -50f;
               

            }

            /*JUMP DASH FOR FINAL
			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.NextBool(20) && npc.life <= 1000)
			{
				int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
				Main.dust[dust2].noGravity = true;
				npc.velocity.Y = Main.rand.NextFloat(-9f, -6f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
				npc.netUpdate = true;
			}
			*/
            //FINAL JUNGLE FLAMES DESPERATE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= 3000 && (choice == 0 || choice == 3))
            //if (Main.rand.NextBool(40))
            {
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.OrangeRed, 1f);
                    Main.dust[dust3].noGravity = true;
                }
                NPC.velocity.Y = Main.rand.NextFloat(-3f, -1f);

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5); //last # is speed
                speed += Main.rand.NextVector2Circular(-3, 3);
                speed.Y += Main.rand.NextFloat(3f, -3f); //just added
                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), fireBreathDamage, 0f, Main.myPlayer); //5f was 0f in the example that works

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                }

                if (NPC.localAI[1] >= 185f) //was 206
                {
                    NPC.localAI[1] = -90f;
                }


                NPC.netUpdate = true;
            }
        }



        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 3").Type, 1f);
                }
            }
        }
        public override void OnKill()
        {

            if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientDemon>()))
            { 
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 5000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.EyeOfTheGods>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BarrierRing>(), 1);
            }

            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
            if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
            if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
          
        }
    }
}