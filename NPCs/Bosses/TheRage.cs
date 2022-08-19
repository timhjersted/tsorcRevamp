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
            NPC.lifeMax = 23100;
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

        //chaos
        int holdTimer = 0;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = NPC.damage / 2;
            NPC.defense = NPC.defense += 10;
            NPC.lifeMax = 20000;
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.1f }, NPC.position);
                flapWings = 0; 
            }


            //Flame attack starts at half health
            if (NPC.life < NPC.lifeMax / 2)
            {
                FlameShotTimer++;
            }

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            //Proximity Debuffs
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) <= 700 && NPC.life < NPC.lifeMax / 2)
            {
                //if (player.immune[BuffID.firewalk]))
                //{
                    player.AddBuff(BuffID.OnFire, 60, false);

                //}

               

                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("The Rage emits a scorching heat wave from its body! Your lungs are on fire!", 235, 199, 23);//deep yellow
                    holdTimer = 3000;
                }

            }
            //getting close to the rage triggers on fire!
            if (NPC.Distance(player.Center) < 80)
            {
                player.AddBuff(BuffID.OnFire, 180, false);
            }

            //chance to trigger fire from above
            if (Main.rand.NextBool(500) && NPC.life < NPC.lifeMax / 2)
            {
                FlameShotCounter = 0;
                NPC.netUpdate = true;
            }

            //FIRE FROM ABOVE ATTACK
            //counts up each tick. used to space out shots
            if (FlameShotTimer >= 25 && FlameShotCounter < 11)
            {
                //more fire trails
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 500 + Main.rand.Next(500), (float)player.position.Y - 400f, (float)(-40 + Main.rand.Next(80)) / 10, 5f, ModContent.ProjectileType<FireTrails>(), fireTrailsDamage, 2f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, PitchVariance = 2 }, NPC.Center);
                NPC.netUpdate = true; //new

                FlameShotTimer = 0;
                FlameShotCounter++;

                
            }
            //homing fireballs
            if (FlameShotTimer >= 25 && FlameShotCounter > 10 && FlameShotCounter < 20)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 500 + Main.rand.Next(500), (float)player.position.Y - 400f, (float)(-40 + Main.rand.Next(80)) / 10, 4.5f, ProjectileID.CultistBossFireBall, fireTrailsDamage, 2f, Main.myPlayer); //ProjectileID.NebulaBlaze2 would be cool to use at the end of attraidies or gwyn fight with the text, "The spirit of your father summons cosmic light to aid you!"
                FlameShotTimer = 0;
                FlameShotCounter++;
            }

            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
                //NPC.dontTakeDamage = false;
                NPC.defense = 22;
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
                        float num48 = 5f;//25 was 40
                        int type = ModContent.ProjectileType<FireTrails>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = 0.0f }, NPC.Center); //flame thrower
                        float rotation = (float)Math.Atan2(vector8.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + 500, vector8.Y - 100, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 100, (float)((Math.Cos(rotation + 0.2) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X - 500, vector8.Y - 100, (float)((Math.Cos(rotation - 0.2) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        NPC.ai[1] = -90;
                        //Added some dust so the projectiles aren't just appearing out of thin air
                        for (int num36 = 0; num36 < 20; num36++)
                        {
                            int fireDust = Dust.NewDust(new Vector2(vector8.X + 500, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(vector8.X - 500, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                        }
                    }
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 750)
                {
                    //Then chill for a second.
                    //This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 130, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);
                }
                else if (NPC.ai[2] >= 750 && NPC.ai[2] < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (vector8.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 25) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 25) * -1;
                    }
                }
                else NPC.ai[2] = 0;
            }
            else
            {
                //invisibility phase
                FlameShotCounter = 0;
                NPC.ai[3]++;
                NPC.alpha = 220;
                NPC.defense = 50;
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
                    float num48 = 14f;//25 was 40
                    float invulnDamageMult = 1.24f;
                    int type = ModContent.ProjectileType<FireTrails>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + 300, vector8.Y - 100, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 100, (float)((Math.Cos(rotation + 0.2) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X - 300, vector8.Y - 100, (float)((Math.Cos(rotation - 0.2) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    NPC.ai[1] = -90;
                    //Added some dust so the projectiles aren't just appearing out of thin air
                    for (int num36 = 0; num36 < 20; num36++)
                    {
                        int fireDust = Dust.NewDust(new Vector2(vector8.X + 300, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(vector8.X - 300, vector8.Y - 100), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                        Main.dust[fireDust].noGravity = true;
                    }
                }


                if (NPC.ai[3] == 100)
                {
                    NPC.ai[3] = 1;
                    NPC.life += 200;
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
