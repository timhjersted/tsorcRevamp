using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class FirebombHollow : ModNPC
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Firebomb Hollow");
            Main.npcFrameCount[npc.type] = 14;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = -1;
            npc.knockBackResist = 0.5f;
            npc.lifeMax = 60;
            npc.damage = 16;
            npc.value = 250;
            npc.height = 40;
            npc.width = 20;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.defense = 6;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.FirebombHollowBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode) return 0.005f;

            if (Main.expertMode && Main.bloodMoon && spawnInfo.player.ZoneOverworldHeight) return chance = 0.1f;

            if (Main.expertMode && Main.bloodMoon) return chance = 0.06f;

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.player.ZoneOverworldHeight && Main.dayTime) return chance = 0.05f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && spawnInfo.player.ZoneOverworldHeight && !Main.dayTime) return chance = 0.08f;

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight) && Main.dayTime) return chance = 0.06f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight) && !Main.dayTime) return chance = 0.09f;


            if ((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) return chance = 0.04f;

            return chance;
        }

        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Throwing.Firebomb>(), Main.rand.Next(1, 3));
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), mod.ItemType("FadingSoul"));
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), mod.ItemType("CharcoalPineResin"));
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 10; i++)
            {
                int dustType = 5;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 54, 2.5f * (float)hitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(npc.position, npc.width, npc.height, 5, 1.5f * (float)hitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }


        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Universal_Timer_Slot = 1;
        private const int AI_State_Timer_1_Slot = 2;
        private const int AI_State_Timer_2_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Firebombing = 1;


        public float AI_State
        {
            get => npc.ai[AI_State_Slot];
            set => npc.ai[AI_State_Slot] = value;
        }
        public float AI_Universal_Timer
        {
            get => npc.ai[AI_Universal_Timer_Slot];
            set => npc.ai[AI_Universal_Timer_Slot] = value;
        }

        public float AI_State_Timer_1
        {
            get => npc.ai[AI_State_Timer_1_Slot];
            set => npc.ai[AI_State_Timer_1_Slot] = value;
        }

        public float AI_State_Timer_2
        {
            get => npc.ai[AI_State_Timer_2_Slot];
            set => npc.ai[AI_State_Timer_2_Slot] = value;
        }



        public override void AI()
        {
            Player player = Main.player[npc.target];


            int lifePercentage = (npc.life * 100) / npc.lifeMax;
            float acceleration = 0.02f;
            float top_speed = (lifePercentage * -0.015f) + 2f; //Increase speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed


            //Debug utilities

            //Main.NewText(Math.Abs(npc.velocity.X));
            //Main.NewText("AI_State is " + AI_State);
            //Main.NewText("AI_Universal_Timer is " + AI_Universal_Timer);
            //Main.NewText("AI_State_Timer_1 is " + AI_State_Timer_1);
            //Main.NewText("AI_Timer is " + AI_Timer);
            //Main.NewText("npc.frameCounter is " + npc.frameCounter);
            //Main.NewText("Distance is " + npc.Distance(player.Center));
            //Main.NewText("knockbackresist is " + npc.knockBackResist);


            #region AI_State Independent


            #region Check if standing on a solid tile

            bool standing_on_solid_tile = false;
            int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
            int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            if (npc.velocity.Y == 0f) // no jump/fall
            {
                int x_left_edge = (int)npc.position.X / 16;
                int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].active() && Main.tileSolid[(int)Main.tile[l, y_below_feet].type]) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall

            #endregion


            if (AI_Universal_Timer < 80)
            {
                AI_Universal_Timer++;
            }

            /*if (AI_Universal_Timer >= 60 && AI_Universal_Timer <= 70)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

            }

            if (AI_Universal_Timer >= 70 && AI_Universal_Timer <= 80)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }
            }*/


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {

                #region Target player, turn if can't reach player


                if (AI_State_Timer_1 == 0)
                {
                    npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }

                if (npc.velocity.X == 0)
                {
                    AI_State_Timer_1++;
                    if (AI_State_Timer_1 > 120 && npc.velocity.Y == 0)
                    {
                        npc.direction *= -1;
                        npc.spriteDirection = npc.direction;
                        AI_State_Timer_1 = 50;
                    }
                }

                if (Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_State_Timer_1 = 0;
                }

                #endregion

                #region Melee Movement & Drop through platforms - but also sometimes the world :(

                if (Math.Abs(npc.velocity.X) > top_speed && npc.velocity.Y == 0)
                {
                    npc.velocity *= (1f - braking_power); //breaking
                }

                else
                {
                    npc.velocity.X += npc.direction * acceleration; //accelerating
                }


                if (npc.direction == 1) //breaking power after turning, to turn fast or to "slip"
                {
                    if (npc.velocity.X > -top_speed)
                    {
                        npc.velocity.X += 0.085f;
                    }
                }

                else
                {
                    if (npc.velocity.X < top_speed)
                    {
                        npc.velocity.X += -0.085f;
                    }
                }

                //Speed limits
                if (npc.velocity.X > 4f) //hard limit of 4f
                {
                    npc.velocity.X = 4f;
                }

                if (npc.velocity.X < -4f)
                {
                    npc.velocity.X = -4f;
                }


                if (Main.tile[(int)npc.position.X / 16, y_below_feet].type == TileID.Platforms && Main.tile[(int)(npc.position.X + (float)npc.width) / 16, y_below_feet].type == TileID.Platforms && npc.position.Y < (player.position.Y - 4 * 16))
                {
                    npc.noTileCollide = true;
                }
                else { npc.noTileCollide = false; }

                #endregion

                #region New Tile()s, jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    if (Main.tile[x_in_front, y_above_feet] == null)
                    {
                        Main.tile[x_in_front, y_above_feet] = new Tile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 1] = new Tile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 2] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 2] = new Tile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 3] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 3] = new Tile();
                    }

                    if (Main.tile[x_in_front, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet + 1] = new Tile();
                    }
                    //  create? 2 other tiles farther in front
                    if (Main.tile[x_in_front + npc.direction, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front + npc.direction, y_above_feet - 1] = new Tile();
                    }

                    if (Main.tile[x_in_front + npc.direction, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front + npc.direction, y_above_feet + 1] = new Tile();
                    }

                    else // standing on solid tile but not in front of a passable door
                    {
                        if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                        {  //  moving forward
                            if (Main.tile[x_in_front, y_above_feet - 2].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].type])
                            { // 3 blocks above ground level(head height) blocked
                                if (Main.tile[x_in_front, y_above_feet - 3].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].type])
                                { // 4 blocks above ground level(over head) blocked
                                    npc.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
                                    npc.netUpdate = true;
                                }
                                else
                                {
                                    npc.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
                                    npc.netUpdate = true;
                                }
                            } // for everything else, head height clear:
                            else if (Main.tile[x_in_front, y_above_feet - 1].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].type])
                            { // 2 blocks above ground level(mid body height) blocked
                                npc.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                                npc.netUpdate = true;
                            }
                            else if (Main.tile[x_in_front, y_above_feet].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].type])
                            { // 1 block above ground level(foot height) blocked
                                npc.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                                npc.netUpdate = true;
                            }
                            else if (npc.directionY < 0 && (!Main.tile[x_in_front, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].type]) && (!Main.tile[x_in_front + npc.direction, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front + npc.direction, y_above_feet + 1].type]))
                            { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                                npc.velocity.Y = -8f; // jump with power 8
                                npc.velocity.X = npc.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                                npc.netUpdate = true;
                            }

                        } // END moving forward, still: standing on solid tile but not in front of a passable door
                    }
                }

                #endregion

                if (Math.Abs(npc.Center.X - player.Center.X) < 15 * 16 && npc.velocity.Y == 0 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    npc.velocity.X = 0;
                }

                if (AI_Universal_Timer == 80 && standing_on_solid_tile && Math.Abs(npc.Center.X - player.Center.X) < 15 * 16 && npc.velocity.Y == 0 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_State = State_Firebombing;
                    AI_State_Timer_1 = 0;
                    npc.frameCounter = 0;
                }
            }


            // FIREBOMBING
            if (AI_State == State_Firebombing)
            {
                AI_State_Timer_1++;

                if (AI_State_Timer_1 < 0)
                {
                    if (npc.velocity.X > 2f) //hard limit of 6f
                    {
                        npc.velocity.X = 2f;
                    }
                    if (npc.velocity.X < -2f) //both directions
                    {
                        npc.velocity.X = -2f;
                    }
                }

                if (AI_State_Timer_1 <= 10)
                {
                    if (npc.direction == 1 && npc.velocity.Y > 0.5f) { npc.velocity.X -= 0.15f; }
                    if (npc.direction == -1 && npc.velocity.Y < -0.5f) { npc.velocity.X += 0.15f; }
                }
                if (AI_State_Timer_1 > 10 && npc.velocity.Y == 0 && standing_on_solid_tile)
                {
                    npc.velocity.X = 0;
                }
                if (AI_State_Timer_1 < 35)
                {
                    npc.TargetClosest(true);
                }
                if (AI_State_Timer_1 == 35)
                {
                    Main.PlaySound(SoundID.Item1.WithVolume(.8f).WithPitchVariance(.3f), npc.position); //Play swing-throw sound
                    Vector2 difference = Main.player[npc.target].Center - npc.Center; //Distance between player center and npc center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                    Vector2 throwpower = (Main.player[npc.target].Center - npc.Center) / 30;

                    throwpower.Y += Main.rand.Next(-3, -1);
                    velocity += throwpower;

                    if (throwpower.Y < -8)
                    {
                        throwpower.Y = -8;
                    }

                    if (throwpower.X > 8)
                    {
                        throwpower.X = 8;
                    }
                    if (throwpower.X < -8)
                    {
                        throwpower.X = -8;
                    }
                    if (throwpower.X < 3 && throwpower.X >= 0)
                    {
                        throwpower.X = 3;
                    }
                    if (throwpower.X > -3 && throwpower.X < 0)
                    {
                        throwpower.X = -3;
                    }

                    //Main.NewText(throwpower);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!Main.hardMode)
                        {
                            Projectile.NewProjectile(npc.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(npc.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 25, 0f, Main.myPlayer);
                        }
                    }
                }

                if (AI_State_Timer_1 == 50)
                {
                    AI_State = State_Pursuing;
                    AI_Universal_Timer = 0;
                    AI_State_Timer_1 = 0;
                }
            }
        }

        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            if (AI_State == State_Firebombing && AI_State_Timer_1 < 35)
            {
                AI_State_Timer_1 = -30;
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            if (AI_State == State_Firebombing && AI_State_Timer_1 < 35)
            {
                AI_State_Timer_1 = 0;
            }
        }

        #endregion


        #region Drawing & Animation

        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (AI_Universal_Timer > 60)
            {
                Texture2D firebombTexture = mod.GetTexture("NPCs/Enemies/FirebombHollow_Firebomb");
                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (npc.spriteDirection == -1)
                {
                    spriteBatch.Draw(firebombTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 64, 54), drawColor, npc.rotation, new Vector2(32, 30), npc.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(firebombTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 64, 54), drawColor, npc.rotation, new Vector2(32, 30), npc.scale, effects, 0);
                }
            }
        }

        public override void DrawEffects(ref Color drawColor)
        {
            if (Main.rand.Next(20) == 0 && AI_State == State_Pursuing && AI_Universal_Timer >= 60)
            {
                if (npc.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(npc.Center.X + 2, npc.position.Y + 22), 12, 12, 6, npc.velocity.X * 0f, npc.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                else
                {
                    int dust = Dust.NewDust(new Vector2(npc.Center.X - 16, npc.position.Y + 22), 12, 12, 6, npc.velocity.X * 0f, npc.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Firebombing && npc.velocity.X == 0)
            {
                npc.spriteDirection = npc.direction;

                if (AI_State_Timer_1 <= 20)
                {
                    npc.frame.Y = 10 * frameHeight;
                }
                else if (AI_State_Timer_1 < 35)
                {
                    npc.frame.Y = 11 * frameHeight;
                }
                else if (AI_State_Timer_1 <= 41)
                {
                    npc.frame.Y = 12 * frameHeight;
                }
                else
                {
                    npc.frame.Y = 13 * frameHeight;
                }
            }

            else
            {
                npc.spriteDirection = npc.direction;

                if (npc.velocity.Y != 0) //If airborn
                {
                    npc.frame.Y = 1 * frameHeight;
                }
                else if (npc.velocity.X == 0)
                {
                    npc.frame.Y = 0 * frameHeight;
                }

                else
                {
                    float framecountspeed = Math.Abs(npc.velocity.X) * 2.2f;
                    npc.frameCounter += framecountspeed;


                    if (npc.frameCounter < 12)
                    {
                        npc.frame.Y = 2 * frameHeight;
                    }
                    else if (npc.frameCounter < 24)
                    {
                        npc.frame.Y = 3 * frameHeight;
                    }
                    else if (npc.frameCounter < 36)
                    {
                        npc.frame.Y = 4 * frameHeight;
                    }
                    else if (npc.frameCounter < 48)
                    {
                        npc.frame.Y = 5 * frameHeight;
                    }
                    else if (npc.frameCounter < 60)
                    {
                        npc.frame.Y = 6 * frameHeight;
                    }
                    else if (npc.frameCounter < 72)
                    {
                        npc.frame.Y = 7 * frameHeight;
                    }
                    else if (npc.frameCounter < 84)
                    {
                        npc.frame.Y = 8 * frameHeight;
                    }
                    else if (npc.frameCounter < 96)
                    {
                        npc.frame.Y = 9 * frameHeight;
                    }
                    else
                    {
                        npc.frameCounter = 0;
                    }
                }
            }
        }



        #endregion


    }
}