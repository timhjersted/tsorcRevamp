using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    class AncientOolacileDemon : ModNPC
    {

        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;

            AnimationType = 28;
            NPC.height = 120;
            NPC.width = 50;
            NPC.damage = 44;
            NPC.defense = 3;
            NPC.lifeMax = 3200;
            NPC.scale = 1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 60000;
            NPC.knockBackResist = 0.0f;
            NPC.lavaImmune = true;

            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("The ancient Oolacile Demon decides to show mercy ...", Color.Gold, DustID.GoldFlame);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ancient Oolacile Demon");
        }
        NPCDespawnHandler despawnHandler;
        int meteorDamage = 21;
        int cultistFireDamage = 22;
        int cultistMagicDamage = 29;
        int cultistLightningDamage = 20;
        int fireBreathDamage = 18;
        int lostSoulDamage = 23;


        int greatFireballDamage = 36;
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
                if (Main.rand.Next(12) == 1)
                {
                    tsorcRevampAIs.Teleport(NPC, 25, true);
                }
            }
            //RISK ZONE
            if (NPC.Distance(player.Center) < 300 && NPC.localAI[1] < 70f && Main.rand.Next(5) == 1)//npc.justHit && 
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-10f, -7f);
                NPC.velocity.X = v;
                //if (Main.rand.Next(2) == 0)
                //{
                //npc.localAI[1] = 80f;
                //} 
                //else
                //{ npc.localAI[1] = 1f; }


                NPC.netUpdate = true;
            }

        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {

            //tsorcRevampAIs.RedKnightOnHit(npc, projectile.DamageType == DamageClass.Melee);
            //TELEPORT RANGED
            if (Main.rand.Next(26) == 1)
            {
                tsorcRevampAIs.Teleport(NPC, 20, true);
                NPC.localAI[1] = 70f;
            }
            //RANGED
            if (NPC.Distance(player.Center) > 201 && NPC.velocity.Y == 0f && Main.rand.Next(3) == 1)//npc.justHit &&
            {

                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(11f, 8f);
                NPC.netUpdate = true;

            }
        }


        //int breathTimer = 0;

        //int breathTimer gives weird cool arrow shape, float does the circle
        float breathTimer = 0;
        int spawnedDemons = 0;
        public override void AI()
        {

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            int choice = Main.rand.Next(6);


            //CHANCE TO JUMP BEFORE ATTACK  
            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.Next(50) == 1 && NPC.life >= 1001)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;
            }

            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.Next(33) == 1 && NPC.life <= 1000)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-7f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;

            }

            //play creature sounds
            if (Main.rand.Next(1700) == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f }, NPC.Center);
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
            }

            NPC.localAI[1]++;
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: false, lavaJumping: true);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.Next(220) == 1, false, SoundID.Item17);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistMagicDamage, 1, Main.rand.Next(20) == 1, false, SoundID.NPCHit34);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 160, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.Next(2) == 1, false, 2, 34, 0);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.Next(200) == 1, false, 2, 34, 0);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 130, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.Next(140) == 1, false, 2, 34);

            //EARLY TELEGRAPH
            if (NPC.localAI[1] >= 60)
            {
                Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(6) == 1)
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
                if (Main.rand.Next(2) == 1)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                }
            }

            if (breathTimer == 350 && Main.rand.Next(3) == 0)
            {
                breathTimer = 1;
            }
            // NEW BREATH ATTACK 
            breathTimer++;
            if (breathTimer > 480)
            {
                NPC.localAI[1] = -50;
                if (NPC.life >= 1001)
                { breathTimer = -20; }
                if (NPC.life <= 1000)
                { breathTimer = -60; }

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
                    if (Main.rand.Next(3) == 0)
                    {

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
                    }

                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].OldPos(9), 9);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);

                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
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
                //npc.TargetClosest(true);
                NPC.velocity.X = 0f;
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y / 2, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 0f, Main.myPlayer);
            }

            //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
            Player player3 = Main.player[NPC.target];
            if (Main.rand.Next(110) == 1 && NPC.Distance(player3.Center) > 700)
            {
                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center); //wobble
                NPC.localAI[1] = 1f;

                NPC.netUpdate = true;
            }
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], ProjectileID.LostSoulHostile, lostSoulDamage, 3, lineOfSight, true, 4, 9);

            //SPAWN FIRE LURKER
            if ((spawnedDemons < 2) && NPC.life >= 2000 && Main.rand.Next(3000) == 1)
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
            if (NPC.localAI[1] >= 160f && (choice == 0 || choice == 4) && NPC.life >= 1001)
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
                if (NPC.life >= 1001 && NPC.life <= 2000 && clearSpace)
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
                if (NPC.life >= 2001 && clearSpace)

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
            //Player player = Main.player[npc.target];





            //MULTI-FIRE 1 ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life >= 1001 && choice == 1) //&& Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)
            {

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                if (Main.rand.Next(3) == 1 && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
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
            if (NPC.localAI[1] >= 160f && NPC.life <= 1000 && (choice == 1 || choice == 2))
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                speed.Y += Main.rand.NextFloat(2f, -2f);
                if (Main.rand.Next(2) == 1 && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
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
            if (NPC.localAI[1] == 160f && NPC.life >= 101 && NPC.life <= 2000 && (choice == 5 || choice == 4)) //&& Main.rand.Next(8) == 1 
            {
                //&& Main.rand.Next(10) == 1 Main.rand.Next(2) == 0 &&
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);
                //speed += Main.player[npc.target].velocity / 4;

                speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6

                //speed += Main.rand.NextVector2Circular(-10, -8);
                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                    //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                    //DesertDjinnCurse; ProjectileID.DD2DrakinShot

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                }
                //if (npc.localAI[1] >= 161f)
                //{ 
                NPC.localAI[1] = -50f;
                //}

            }

            /*JUMP DASH FOR FINAL
			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.Next(20) == 1 && npc.life <= 1000)
			{
				int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
				Main.dust[dust2].noGravity = true;
				npc.velocity.Y = Main.rand.NextFloat(-9f, -6f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
				npc.netUpdate = true;
			}
			*/
            //FINAL JUNGLE FLAMES DESPERATE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= 1000 && (choice == 0 || choice == 3))
            //if (Main.rand.Next(40) == 1)
            {
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(2) == 1)
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

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileDemonBag>()));
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (!Main.dedServ)
            {
                if (NPC.life <= 0)
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
            //if not killed before (has boss bag now)
            //if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientOolacileDemon>())))
            //{
            //Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaVessel>(), 1);
            //Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Accessories.BandOfGreatCosmicPower>(), 1);
            //Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 5000);
            //Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ItemID.CloudinaBottle, 1);
            //}
            if (!Main.expertMode)
            {
                if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
                if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);

                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 10);
            }
        }
    }
}