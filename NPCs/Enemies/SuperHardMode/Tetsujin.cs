using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class Tetsujin : ModNPC
    {
        public override void SetDefaults()
        {
            //npc.npcSlots = 50;
            Main.npcFrameCount[NPC.type] = 2;
            NPC.width = 42;
            NPC.height = 42;
            NPC.aiStyle = 22;
            NPC.damage = 165;
            NPC.defense = 70; //190
            NPC.lavaImmune = true; ;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = Mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 6000; //was 35k
            NPC.scale = 1f;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.value = 18750;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.TetsujinBanner>();
        }

        int laserDamage = 20; //17
        int breathDamage = 20; //33
        int blasterDamage = 22; //35
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            laserDamage = (int)(laserDamage * tsorcRevampWorld.SubtleSHMScale);
            breathDamage = (int)(breathDamage * tsorcRevampWorld.SubtleSHMScale);
            blasterDamage = (int)(blasterDamage * tsorcRevampWorld.SubtleSHMScale);
        }


        //float customAi1;
        int breathCD = 30;
        //int previous = 0;
        bool breath = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            float chance = 0;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (player.ZoneSkyHeight)
                {
                    chance = 0.5f;
                }
                if (player.ZoneMeteor)
                {
                    chance = .75f;
                }
                if (!Main.dayTime)
                {
                    chance *= 2;
                }
                if (Main.bloodMoon)
                {
                    chance *= 2;
                }
            }

            return chance;
        }
        #endregion


        public override void AI()
        {
            if (Main.netMode != 1)
            {
                if (Main.GameUpdateCount % 240 == 0)
                {
                    Vector2 projVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 1);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.EnemyRedLaser>(), 20, 0, Main.myPlayer, NPC.target, NPC.whoAmI);
                }

                NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (NPC.ai[1] >= 10f)
                {
                    NPC.TargetClosest(true);
                    if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                    {


                        //Player nT = Main.player[npc.target];
                        if (Main.rand.Next(800) == 0)
                        {
                            breath = true;
                            Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
                        }
                        if (breath)
                        {

                            float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, (float)((Math.Cos(rotation) * 25) * -1), (float)((Math.Sin(rotation) * 25) * -1), ModContent.ProjectileType<Projectiles.Enemy.DragonsBreath>(), breathDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 40;
                            NPC.netUpdate = true;





                            breathCD--;
                            //}
                        }
                        if (breathCD <= 0)
                        {
                            breath = false;
                            breathCD = 30;
                            Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
                        }




                        //LASER
                        if (Main.rand.Next(55) == 1)//was 45
                        {
                            float num48 = 13f;
                            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                            float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                            float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                            if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.TetsujinLaser>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blasterDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 650;
                                Main.projectile[num54].aiStyle = 23;
                                Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 12);
                                //customAi1 = 1f;
                            }
                            NPC.netUpdate = true;
                        }


                    }


                }
            }
            //IF HIT, SOMETIMES GET INTERRUPTED 
            if (NPC.justHit && Main.rand.Next(2) == 1)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y; //added -60
                    NPC.ai[2] = 0f;
                }
                NPC.TargetClosest(true);
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)(((NPC.position.Y - 30) + (float)NPC.height) / 16f);
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                //npc.velocity.Y += .1f;
                //if (npc.velocity.Y > +2)
                //{
                //	npc.velocity.Y = -2;
                //}


                //int dust = Dust.NewDust(new Vector2((float) npc.position.X + (npc.width * 0.5f), (float) npc.position.Y+1), npc.width/8, npc.height/2, 15, npc.velocity.X, npc.velocity.Y+6f, 150, Color.Blue, 1f);
                //Main.dust[dust].noGravity = false;

                if (NPC.direction == -1)
                {
                    int dust = Dust.NewDust(NPC.Center + new Vector2(NPC.direction == 1 ? NPC.width * 0.5f : +15, -22), NPC.width / 8, NPC.height / 2, 15, NPC.velocity.X, NPC.velocity.Y + 6f, 150, Color.Blue, 1f);
                    Main.dust[dust].noGravity = false;
                }
                if (NPC.direction == 1)
                {
                    int dust = Dust.NewDust(NPC.Center + new Vector2(NPC.direction == -1 ? NPC.width * -0.5f : -26, -22), NPC.width / 8, NPC.height / 2, 15, NPC.velocity.X, NPC.velocity.Y + 6f, 150, Color.Blue, 1f);
                    Main.dust[dust].noGravity = false;
                }
                //-26 was -21

                NPC.velocity.Y -= 0.05f;
                if (NPC.velocity.Y < -1)
                {
                    NPC.velocity.Y = -1;
                }


            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += 0.05f;
                if (NPC.velocity.Y > 1)
                {
                    NPC.velocity.Y = 1;
                }

                //npc.velocity.Y += .2f;
                //if (npc.velocity.Y > 2)
                //{
                //	npc.velocity.Y = 2;
                //}
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2.5f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -2.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (!Main.dedServ)
            {
                int frameSize = 1;
                frameSize = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
                if (NPC.velocity.X < 0)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }

                NPC.frameCounter++;
                if (NPC.frameCounter >= 12.0)
                {
                    NPC.frame.Y = NPC.frame.Y + frameSize;
                    NPC.frameCounter = 0.0;
                }
                if (NPC.frame.Y >= Main.npcTexture[NPC.type].Height)
                {
                    NPC.frame.Y = 0;
                }
            }
        }
        #region Debuffs
        public override void OnHitPlayer(Player player, int target, bool crit)
        {

            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(24, 600, false); //on fire
            }

            if (Main.rand.Next(4) == 0)
            {

                player.AddBuff(36, 600, false); //broken armor
                                                //player.AddBuff(23, 120, false); //cursed

            }

            //if (Main.rand.Next(10) == 0 && player.statLifeMax > 20) 

            //{

            //			Main.NewText("You have been cursed!");
            //	player.statLifeMax -= 20;
            //}
        }
        #endregion
        //-------------------------------------------------------------------

        #region Glowing Eye Effect 
        //public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        //{
        //broken for now
        //			int spriteWidth=npc.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
        //			int spriteHeight = Main.npcTexture[Config.npcDefs.byName[npc.name].type].Height / Main.npcFrameCount[npc.type];

        //			int spritePosDifX = (int)(npc.frame.Width / 2);
        //			int spritePosDifY = npc.frame.Height; // was npc.frame.Height - 4;

        //			int frame = npc.frame.Y / spriteHeight;

        //			int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
        //			int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

        //			SpriteEffects flop = SpriteEffects.None;
        //			if(npc.spriteDirection == 1){
        //				flop = SpriteEffects.FlipHorizontally;
        //			}


        //				//Glowing Eye Effect
        //				for(int i=0;i>-1;i--)
        //				{
        //					//draw 3 levels of trail
        //					int alphaVal=255-(0*i); //255-(1*i);
        //					Color modifiedColour = new Color((int)(alphaVal),(int)(alphaVal),(int)(alphaVal),alphaVal);
        //					spriteBatch.Draw(Main.goreTexture[Config.goreID["Tetsujin Glow"]],
        //						new Rectangle((int)(offsetX), (int)(offsetY), spriteWidth, spriteHeight),
        //						new Rectangle(0,npc.frame.Height * frame, spriteWidth, spriteHeight),
        //						modifiedColour,
        //						0,  //Just add this here I think
        //						new Vector2(0,0),
        //						flop,
        //						0);  
        //				}	
        //}
        #endregion
        //-------------------------------------------------------------------
        #region gore
        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 1"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 2"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 3"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 3"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 2"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 3"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Tetsujin Gore 3"), 0.9f);
            if (Main.rand.Next(2) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CompactFrame>());
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DestructionElement>());
            }

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Ammo.TeslaBolt>(), 200 + Main.rand.Next(30));
        }
        #endregion
    }
}
