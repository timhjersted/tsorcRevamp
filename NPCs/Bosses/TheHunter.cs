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
    class TheHunter : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 20800;
            NPC.damage = 130;
            NPC.defense = 26;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 220000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            DrawOffsetY = +70;
            NPC.width = 140;
            NPC.height = 60;

            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("The Hunter seeks its next prey...", Color.Green, 89);
        }

        int hitTime = 0;
        int sproutDamage = 65;
        int cursedBreathDamage = 25;
        public float flapWings;

        //oolicile sorcerer
        public float FrogSpawnTimer;
        public float FrogSpawnCounter;

        //chaos
        int holdTimer = 0;

    
        bool ChildrenSpawned = false;

        float breathTimer = 60;
        float breathTimer2 = 600;


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = NPC.damage / 2;
            NPC.defense = NPC.defense += 10;
            //NPC.lifeMax = 32000; //this plus NPC.lifeMax = 32000; made the health 20,800
            //sproutDamage = (int)(sproutDamage * 1.3 / 2);
            sproutDamage = (int)(sproutDamage / 2);
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
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 89, NPC.velocity.X, NPC.velocity.Y, 200, default, 0.5f + (10.5f * (NPC.ai[0] / (NPC.lifeMax / 10))));
            Main.dust[dust].noGravity = true;


            flapWings++;
            breathTimer++;
            



            //Flap Wings
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.position); //wing flap sound
            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = -0.1f }, NPC.position);
                flapWings = 0;
            }


            //Frogs spawn above half health
            if (NPC.life >= NPC.lifeMax / 2)
            {
                FrogSpawnTimer++;
            }

            Player player = Main.player[NPC.target];
            //chaos code: announce child spawn once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            //2nd phase debuffs
            if (NPC.Distance(player.Center) < 1550 && NPC.life < NPC.lifeMax / 2)
            {
                player.AddBuff(BuffID.Starving, 120, false);


                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("The Hunter has decided to feed you to its child. It's fully camouflaged!", 235, 199, 23);//deep yellow
                    holdTimer = 9000;


                }

            }

            //spawn the child!
            if (!ChildrenSpawned && NPC.life <= NPC.lifeMax / 2)
            {
                int Child = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.TheHunterChild>(), 0);
                
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.01f }, NPC.Center);
                ChildrenSpawned = true;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Child, 0f, 0f, 0f, 0);
                }

            }
            //getting close to the hunter triggers bleeding
            if (NPC.Distance(player.Center) < 80)
            {
                player.AddBuff(BuffID.Bleeding, 180, false);
            }

            //MUTANT TOAD SPAWN
            //counts up each tick. used to space out spawns
            if (FrogSpawnTimer >= 120 && FrogSpawnCounter < 3)
            {

                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.MutantToad>(), 0);

                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie13 with { Volume = 0.5f}, NPC.Center);
                NPC.netUpdate = true; //new

                FrogSpawnTimer = 0;
                FrogSpawnCounter++;

            }
            //chance to trigger frogs spawning
            if (Main.rand.NextBool(900) && NPC.life >= NPC.lifeMax / 2 && NPC.life <= 20000)
            {
                FrogSpawnCounter = 0;
                NPC.netUpdate = true;
            }

            
            Player Player = Main.player[NPC.target];

            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
                //NPC.dontTakeDamage = false;
                NPC.defense = 26;
                if (NPC.ai[2] < 600)
                {
                    //FINAL BREATH
                    // FIRE BREATH ATTACK 
                    if (NPC.life <= NPC.lifeMax / 4)
                    {
                        breathTimer2++;
                        if (breathTimer2 > 600)
                        {
                            breathTimer2 = 355;
                        }
                        //dust animation
                        if (breathTimer2 > 360)
                        {
                            NPC.ai[2] = 300;
                            NPC.velocity.X = 0f;
                            UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer2) / 120)), DustID.MagicMirror, 48, 4);
                            Lighting.AddLight(NPC.Center, Color.PaleVioletRed.ToVector3() * 5);
                        }

                        if (breathTimer2 > 480 && breathTimer2 < 600)
                        {
                            breathTimer2 = -220;
                            NPC.ai[2] = 300;
                        }

                        //projectile
                        if (breathTimer2 < 0)
                        {

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                                breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<CursedDragonsBreath>(), sproutDamage, 0f, Main.myPlayer);
                                //play breath sound
                                if (Main.rand.NextBool(3))
                                {
                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, PitchVariance = 1f }, NPC.Center); //flame thrower
                                }
                            }
                        }



                    }
                    //END FINAL BREATH ATTACK

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
                        float num48 = 11f;//was 14

                        int type = ModContent.ProjectileType<MiracleSprouter>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.8f, PitchVariance = 2f}, vector8);
                        float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 60;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 60;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 60;
                        }
                        NPC.ai[1] = -90;
                    }
                    NPC.netUpdate = true; //new
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 850) //was 750
                {
                    
                    
                    //Then chill for a few seconds.
                    //This exists to delay switching to the 'charging' pattern for 150 frames, because otherwise the way the sprouters linger can often make the first charge impossible to dodge
                    
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 131, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1.5f);

                    // NEW BREATH ATTACK 
                  
                    if (breathTimer > 501)
                    {
                        breathTimer = 359;
                    }
                        if (breathTimer > 360)
                    {
                        NPC.velocity.X *= 0.95f;
                        NPC.velocity.Y *= 0.95f;
                        UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 100)), DustID.CursedTorch, 48, 4);
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
                    }

                    if (breathTimer > 480 && breathTimer < 500 && NPC.life >= NPC.lifeMax / 2)
                    {
                        breathTimer = -140;
                        
                    }

                    if (breathTimer > 480 && breathTimer < 500 && NPC.life < NPC.lifeMax / 2)
                    {
                        breathTimer = -190;

                    }

                    if (breathTimer < 0)
                    {
                        
                        if (Player.position.X < NPC.position.X)
                        {
                            NPC.velocity.X -= 0.1f;
                        }
                        if (Player.position.X > NPC.position.X)
                        {
                            NPC.velocity.X += 0.1f;
                        }


                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                            
                        }
                        

                    }

                    



                }
                else if (NPC.ai[2] >= 850 && NPC.ai[2] < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (vector8.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = ((float)(Math.Cos(rotation) * 25) * -1) + Main.player[NPC.target].velocity.X;
                        NPC.velocity.Y = ((float)(Math.Sin(rotation) * 25) * -1) + Main.player[NPC.target].velocity.Y;
                    }
                }
                else NPC.ai[2] = 0;
            }
            else
            {
                NPC.ai[3]++;
                NPC.alpha = 225;
                NPC.defense = 70;
                //NPC.dontTakeDamage = true;
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
                    float num48 = 11f;//22
                    float invulnDamageMult = 1.3f;
                    int type = ModContent.ProjectileType<MiracleSprouter>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 60;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 55;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 60;
                    }
                    NPC.ai[1] = -90;
                }
                if (NPC.ai[3] == 100)
                {
                    if (NPC.ai[3] == 100)
                
                    //FrogSpawnCounter = 0;
                    NPC.ai[3] = 1;
                    NPC.life += 1200;
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int num36 = 0; num36 < 40; num36++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 0, default, 1f); //was 3f
                    }
                }
            }
        }
        public override void FindFrame(int currentFrame)
        {
            Player player = Main.player[NPC.target];

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
                
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 0.5f); 
            }
            else
            {
                if (player.HasBuff(BuffID.Hunter))
                { 
                    NPC.alpha = 0; 
                }
                else 
                { 
                    NPC.alpha = 255; 
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            }
            /*
            if (NPC.ai[3] == 0)
            {
                //visible
                NPC.alpha = 0;
            }
            else
            {
                //partially invsible
                NPC.alpha = 235;//was 200
            }
            */
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
                NPC.ai[3] = 1;
                Color color = new Color();
                for (int num36 = 0; num36 < 50; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, color, 3f);
                }
                for (int num36 = 0; num36 < 20; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, color, 3f);
                }
                NPC.ai[1] = -200;
                NPC.ai[0] = 0;
            }
            return true;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheHunterBag>()));
        }

        public override void OnKill()
        {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 89, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 100; num36++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 131, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CrestOfEarth>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.WaterWalkingBoots, 1, false, -1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Drax, 1, false, -1);
            }
        }
    }
}
