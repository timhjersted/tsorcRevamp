using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class LothricSpearKnight : ModNPC
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[npc.type] = 20;
            NPCID.Sets.TrailCacheLength[npc.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[npc.type] = 0;
        }

        public override void SetDefaults()
        {
            npc.timeLeft = 60;
            npc.npcSlots = 15;
            npc.knockBackResist = 0.1f;
            npc.aiStyle = -1;
            npc.damage = 40; 
            npc.defense = 50;
            npc.height = 40;
            npc.width = 20;
            npc.lifeMax = 750;
            if (Main.hardMode) { npc.lifeMax = 1400; npc.defense = 60; }
            npc.value = 3500;
            npc.noGravity = false;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.buffImmune[ModContent.BuffType<Buffs.ToxicCatDrain>()] = true;
            npc.buffImmune[ModContent.BuffType<Buffs.ViruCatDrain>()] = true;
            npc.buffImmune[ModContent.BuffType<Buffs.BiohazardDrain>()] = true;

            /*banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.DunlendingBanner>();*/
        }


        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Timer_Shielding_Slot = 1;
        private const int AI_Timer_Attacking_Slot = 2;
        private const int AI_Timer_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Shielding = 1;
        private const int State_Thrusting = 2;
        private const int State_JumpThrust = 3;
        private const int State_LungeThrust = 4;


        public float AI_State
        {
            get => npc.ai[AI_State_Slot];
            set => npc.ai[AI_State_Slot] = value;
        }
        public float AI_Timer_Shielding
        {
            get => npc.ai[AI_Timer_Shielding_Slot];
            set => npc.ai[AI_Timer_Shielding_Slot] = value;
        }

        public float AI_Timer_Attacking
        {
            get => npc.ai[AI_Timer_Attacking_Slot];
            set => npc.ai[AI_Timer_Attacking_Slot] = value;
        }

        public float AI_Timer
        {
            get => npc.ai[AI_Timer_Slot];
            set => npc.ai[AI_Timer_Slot] = value;
        }

        public override void AI()
        {
            Player player = Main.player[npc.target];

            if (npc.Distance(player.Center) < 600)
            {
                player.ZonePeaceCandle = true;
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 2);
            }

            int lifePercentage = (npc.life * 100) / npc.lifeMax;
            float acceleration = 0.02f;
            float top_speed = (lifePercentage * -0.015f) + 2f; //Increase speed the lower the enemy HP%
            float braking_power = 0.2f; //Breaking power to slow down after moving above top_speed


            //Debug utilities

            //Main.NewText(Math.Abs(npc.velocity.X));
            //Main.NewText("AI_State is " + AI_State);
            //Main.NewText("AI_Timer_Shielding is " + AI_Timer_Shielding);
            //Main.NewText("AI_Timer_Attacking is " + AI_Timer_Attacking);
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


            if (AI_Timer_Attacking < 420)
            {
                AI_Timer_Attacking++;
            }

            if (AI_Timer_Attacking >= 390 && AI_Timer_Attacking <= 400)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

            }

            if (AI_Timer_Attacking >= 400 && AI_Timer_Attacking < 442)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }
            }


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {

                #region Target player, turn if can't reach player


                if (AI_Timer == 0)
                {
                    npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }

                if (npc.velocity.X == 0)
                {
                    AI_Timer++;
                    if (AI_Timer > 120 && npc.velocity.Y == 0)
                    {
                        npc.direction *= -1;
                        npc.spriteDirection = npc.direction;
                        AI_Timer = 50;
                    }
                }

                if (Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_Timer = 0;
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
                if (npc.velocity.X > 4f) //hard limit of 10f
                {
                    npc.velocity.X = 4f;
                }

                if (npc.velocity.X < -4f)
                {
                    npc.velocity.X = -4f;
                }


                if (Math.Abs(npc.velocity.X) > 4f || Math.Abs(npc.velocity.Y) > 0.1f) //If moving at high speed.X or vertically, become knockback immune
                {
                    npc.knockBackResist = 0;
                }
                else 
                {
                    npc.knockBackResist = 0.1f; //aparently it doesn't default back? 
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
                    if (npc.position.Y > player.position.Y + 3 * 16 && npc.position.Y < player.position.Y + 8 * 16 && Math.Abs(npc.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)) 
                    {
                        npc.velocity.Y = -8f; // jump with power 8 if directly under player
                        npc.netUpdate = true;
                    }

                    if (npc.position.Y >= player.position.Y + 8 * 16 && Math.Abs(npc.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        npc.velocity.Y = -9.5f; // jump with power 9.5 if directly under player
                        npc.netUpdate = true;
                    }


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


                if (npc.Distance(player.Center) < 250 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_Timer_Shielding++;
                }

                if (npc.Distance(player.Center) < 95 && standing_on_solid_tile && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_Timer_Shielding = 300;
                    AI_State = State_Shielding;
                }

                if (AI_Timer_Shielding >= 300 && standing_on_solid_tile)
                {
                    AI_State = State_Shielding;
                }

                if (AI_Timer_Attacking == 420 && Math.Abs(npc.Center.X - player.Center.X) > 6.5f * 16 && Math.Abs(npc.Center.X - player.Center.X) < 19f * 16 && standing_on_solid_tile && npc.velocity.Y == 0 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_State = State_LungeThrust;
                    AI_Timer = 29;
                }
            }


            // SHIELDING
            if (AI_State == State_Shielding)
            {
                npc.TargetClosest(true);
                AI_Timer_Shielding++;

                if (npc.velocity.Y == 0)
                {
                    if (AI_Timer_Shielding > 300 && AI_Timer_Shielding <= 310 && Math.Abs(npc.velocity.X) > 1f)
                    {
                        if (npc.direction == 1) { npc.velocity.X -= 0.15f; }
                        else { npc.velocity.X += 0.15f; }
                    }

                    if (AI_Timer_Shielding > 310)
                    {
                        npc.velocity.X = 0;
                    }

                    if (AI_Timer_Shielding > 500)
                    {
                        AI_State = State_Pursuing;
                        AI_Timer_Shielding = 0;
                    }
                }

                if (AI_Timer_Shielding > 310 && Math.Abs(npc.Center.X - player.Center.X) < 6.5f * 16 && Math.Abs(npc.Center.Y - player.Center.Y) < 6.5f * 16 && standing_on_solid_tile && npc.velocity.Y == 0 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_State = State_Thrusting;
                }

                if (AI_Timer_Shielding > 310 && AI_Timer_Attacking == 420 && Math.Abs(npc.Center.X - player.Center.X) > 6.5f * 16 && Math.Abs(npc.Center.X - player.Center.X) < 19f * 16 && standing_on_solid_tile && npc.velocity.Y == 0 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    AI_State = State_LungeThrust;
                }
            }


            //THRUSTING (While shielding)
            if (AI_State == State_Thrusting)
            {
                AI_Timer++;
                AI_Timer_Shielding = 400;
                npc.velocity.X = 0;
                npc.velocity.Y = 0;


                #region Projectiles & Sounds
                if (npc.direction == 1)
                {
                    if (AI_Timer == 34)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 25, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }

                    if (AI_Timer == 50)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 25, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }

                    if (AI_Timer == 77)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 25, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }
                }
                else 
                {
                    if (AI_Timer == 34)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 25, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }

                    if (AI_Timer == 50)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 25, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }

                    if (AI_Timer == 76)
                    {
                        Main.PlaySound(SoundID.Item1.WithPitchVariance(.3f), npc.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 10, 5, Main.myPlayer, npc.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }
                }

                #endregion


                if (AI_Timer > 94)
                {
                    if (AI_Timer_Attacking == 420 && npc.Distance(player.Center) < 175 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                    {
                        AI_Timer = 0;
                        AI_Timer_Shielding = 0;
                        AI_State = State_JumpThrust;
                    }

                    else
                    {
                        AI_Timer = 0;
                        AI_State = State_Shielding;
                    }
                }
            }


            //JUMP-THRUST
            if (AI_State == State_JumpThrust)
            {

                #region KB & Speed Limits


                npc.knockBackResist = 0;

                if (npc.velocity.X > 6f) //hard limit of 6f
                {
                    npc.velocity.X = 6f;
                }

                if (npc.velocity.X < -6f)
                {
                    npc.velocity.X = -6f;
                }


                #endregion


                if (AI_Timer < 82)
                {
                    AI_Timer++;
                }

                if (AI_Timer == 20)
                {
                    npc.velocity.Y -= 10f;
                }

                if (AI_Timer == 52 || (AI_Timer > 23 && AI_Timer < 52 && npc.collideY))
                {
                    AI_Timer = 52;
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0;
                    npc.noGravity = true;
                }

                if (AI_Timer >= 52 && AI_Timer <= 82)
                {
                    npc.TargetClosest(true);
                }

                if (AI_Timer == 81)
                {
                    Main.PlaySound(SoundID.Item45.WithPitchVariance(.3f), npc.Center);
                }
                if (AI_Timer == 82)
                {
                    float power;

                    npc.noGravity = false;
                    npc.velocity.Y += 4f;
                    if (npc.direction == 1)
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(28, +38), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 35, 5, Main.myPlayer, npc.whoAmI, 0)];
                        stab.timeLeft = 2;

                        power = (Math.Abs(npc.Center.X - player.Center.X) / 16) * 4 / 10;
                        npc.velocity.X += power;
                    }
                    else 
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-28, +38), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 35, 5, Main.myPlayer, npc.whoAmI, 0)];
                        stab.timeLeft = 2;

                        power = (Math.Abs(npc.Center.X - player.Center.X) / 16) * 4 / 10;
                        npc.velocity.X -= power;
                    }

                }

                if (AI_Timer >= 82 && npc.collideY)
                {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0;
                    AI_Timer_Attacking = 0;
                    AI_Timer++;
                }

                if (AI_Timer == 192)
                {
                    AI_Timer = 0;
                    if (npc.Distance(player.Center) < 175)
                    {
                        AI_Timer_Shielding = 400;
                        AI_State = State_Shielding;
                    }
                    else
                    {
                        AI_State = State_Pursuing;
                    }
                }
            }


            //LUNGE-THRUST
            if (AI_State == State_LungeThrust)
            {

                #region KB & Speed Limits


                npc.knockBackResist = 0;

                if (npc.velocity.X > 8.5f) //hard limit of 6f
                {
                    npc.velocity.X = 8.5f;
                }

                if (npc.velocity.X < -8.5f)
                {
                    npc.velocity.X = -8.5f;
                }


                #endregion


                if (AI_Timer < 55)
                {
                    AI_Timer++;
                }

                if (AI_Timer < 30)
                {
                    npc.TargetClosest(true);
                }

                if (AI_Timer == 30)
                {
                    Main.PlaySound(SoundID.Item45.WithPitchVariance(.3f), npc.Center);
                    if (npc.direction == 1) 
                    { 
                        npc.velocity.X += 8.5f; 
                        //Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 30, 5, Main.myPlayer, npc.whoAmI, 0)];
                    }
                    else 
                    { 
                        npc.velocity.X -= 8.5f;
                        //Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 30, 5, Main.myPlayer, npc.whoAmI, 0)];
                    }
                }
                if (AI_Timer >= 30 && AI_Timer < 56)
                {
                    if (npc.direction == 1)
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 30, 5, Main.myPlayer, npc.whoAmI, 0)];
                        stab.timeLeft = 2;
                    }
                    else
                    {
                        Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-80, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), 30, 5, Main.myPlayer, npc.whoAmI, 0)];
                        stab.timeLeft = 2;
                    }
                }
                if (AI_Timer > 30 && npc.collideX)
                {
                    AI_Timer = 56;
                }

                if (AI_Timer == 56)
                {
                    AI_Timer_Attacking = 200;
                }

                if (AI_Timer > 45 && AI_Timer < 56 && standing_on_solid_tile)
                {
                    if (Math.Abs(npc.velocity.X) > 0.1f)
                    {
                        if (npc.direction == 1) 
                        { 
                            npc.velocity.X -= .85f;
                            if (npc.velocity.X < 0.2f)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                        else 
                        { 
                            npc.velocity.X += .85f;
                            if (npc.velocity.X > -0.2f)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }
                }

                if (AI_Timer >= 55)
                {
                    if (Math.Abs(npc.velocity.X) <= 0.1f)
                    {
                        AI_Timer++;
                    }
                }

                if (AI_Timer == 100)
                {
                    AI_State = State_Pursuing;
                    AI_Timer = 0;
                }
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (AI_State == State_Shielding || AI_State == State_Thrusting)
            {
                if (npc.direction == 1)
                {
                    if (player.position.X > npc.position.X)
                    {
                        Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                        damage -= 80;
                        if (AI_Timer_Shielding > 340)
                        {
                            AI_Timer_Shielding -= 35;
                        }
                    }
                }
                else
                {
                    if (player.position.X < npc.position.X)
                    {
                        Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                        damage -= 80;
                        if (AI_Timer_Shielding > 340)
                        {
                            AI_Timer_Shielding -= 35;
                        }
                    }
                }
            }

            if (npc.direction == 1) //if enemy facing right
            {
                if (player.position.X < npc.position.X) //if hit in the back
                {
                    damage = (int)(damage * 2f); //bonus damage
                    Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > npc.position.X) //if hit in the back
                {
                    damage = (int)(damage * 2f); //bonus damage
                    Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                }
            }

            AI_Timer_Shielding += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.player[npc.target];
            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.BlizzardBlasterShot>())
            {
                if (AI_State == State_Shielding || AI_State == State_Thrusting)
                {
                    if (npc.direction == 1) //if npc facing right
                    {
                        if (projectile.oldPosition.X > npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= 80;
                            knockback = 0f;
                            if (AI_Timer_Attacking < 340)
                            {
                                AI_Timer_Attacking += 70; //Used for Jump-slash
                            }
                            if (AI_Timer_Shielding > 340)
                            {
                                AI_Timer_Shielding -= 35;
                            }
                        }

                        else if (hitDirection == -1 && (!projectile.melee || projectile.aiStyle == 19))
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= 80;
                            knockback = 0f;

                            if (AI_Timer_Attacking < 340)
                            {
                                AI_Timer_Attacking += 80; //Used for Jump-slash
                            }
                            if (AI_Timer_Shielding > 340)
                            {
                                AI_Timer_Shielding -= 35;
                            }
                        }
                    }

                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= 80;
                            knockback = 0f;
                            if (AI_Timer_Attacking < 340)
                            {
                                AI_Timer_Attacking += 70; //Used for Jump-slash
                            }
                            if (AI_Timer_Shielding > 340)
                            {
                                AI_Timer_Shielding -= 35;
                            }
                        }
                        else if (hitDirection == 1 && (!projectile.melee || projectile.aiStyle == 19))
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= 80;

                            knockback = 0f;
                            if (AI_Timer_Attacking < 340)
                            {
                                AI_Timer_Attacking += 80; //Used for Jump-slash
                            }
                            if (AI_Timer_Shielding > 340)
                            {
                                AI_Timer_Shielding -= 35;
                            }
                        }
                    }
                }


                if (npc.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                    }
                    else if (hitDirection == 1)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                    }
                }
                else //if enemy facing left
                {
                    if (projectile.oldPosition.X > npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                    }
                    else if (hitDirection == -1)
                    {
                        CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                    }
                }

                if (npc.Distance(player.Center) > 220 && AI_State != State_Shielding && AI_State != State_Thrusting)
                {
                    AI_Timer_Shielding += 120;
                    if (AI_Timer_Shielding > 300)
                    {
                        AI_Timer_Shielding = 300;
                    }
                }
                if (AI_Timer_Attacking < 400)
                {
                    AI_Timer_Attacking += 10;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode) return 0.03f;

            if (Main.expertMode && Main.bloodMoon && spawnInfo.player.ZoneOverworldHeight && NPC.downedBoss3) return chance = 0.02f;

            if (Main.expertMode && Main.bloodMoon && NPC.downedBoss3) return chance = 0.02f;

            if (NPC.downedBoss3 && spawnInfo.player.ZoneOverworldHeight && Main.dayTime) return chance = 0.005f;
            if (NPC.downedBoss3 && spawnInfo.player.ZoneOverworldHeight && !Main.dayTime) return chance = 0.015f;

            if (NPC.downedBoss3) return chance = 0.003f;

            return chance;
        }

        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 3 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ItemID.Heart);
            Item.NewItem(npc.getRect(), ItemID.Heart);

            if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.SpikedIronShield>(), 1, false, -1);
            if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1, false, -1);
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.LostUndeadSoul>());
            if (Main.rand.Next(5) == 0) { Item.NewItem(npc.getRect(), ItemID.LifeforcePotion); }
            if (Main.rand.Next(3) == 0) { Item.NewItem(npc.getRect(), ItemID.EndurancePotion); }
        }
        #endregion


        #region Drawing & Animation


        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawOrigin = new Vector2(npc.position.X, npc.position.Y); //Shadow trails
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if ((npc.velocity.X > 3f || npc.velocity.X < -3f || npc.velocity.Y != 0) && (AI_State == State_JumpThrust || AI_State == State_LungeThrust))
            {
                for (int k = 0; k < npc.oldPos.Length; k++)
                {
                    Vector2 drawPos = npc.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, npc.gfxOffY); //Where to draw trails
                    Color color = npc.GetAlpha(lightColor) * ((float)(npc.oldPos.Length - k) / (float)npc.oldPos.Length);
                    if (npc.direction == 1)
                    {
                        spriteBatch.Draw(Main.npcTexture[npc.type], drawPos, new Rectangle(npc.frame.X, npc.frame.Y, 116, 88), color, npc.rotation, new Vector2(npc.position.X + 26, npc.position.Y + 26), npc.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                    }
                    else
                    {
                        spriteBatch.Draw(Main.npcTexture[npc.type], drawPos, new Rectangle(npc.frame.X, npc.frame.Y, 116, 88), color, npc.rotation, new Vector2(npc.position.X + 70, npc.position.Y + 26), npc.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                    }
                }
            }

            Texture2D texture = Main.npcTexture[npc.type]; //Base texture, manually drawing so as to not have a ridiculously big canvas size in order to have a centered hitbox
            if (npc.spriteDirection == 1)
            {
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 116, 88), lightColor, npc.rotation, new Vector2(37, 46), npc.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 116, 88), lightColor, npc.rotation, new Vector2(80, 46), npc.scale, effects, 0f);
            }

            return false; //Don't draw base sprite
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D shieldTexture = mod.GetTexture("NPCs/Enemies/LothricSpearKnight_Greatshield");
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 19, 0, shieldFrame);
            if ((AI_State == State_Shielding || AI_State == State_Thrusting) && npc.velocity.X == 0)
            {
                if (npc.spriteDirection == 1)
                {
                    spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(37, 46), npc.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(80, 46), npc.scale, effects, 0f);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Pursuing && npc.velocity.X != 0) //Walking anim
            {

                float framecountspeed = Math.Abs(npc.velocity.X) * 2.2f;
                npc.frameCounter += framecountspeed;
                npc.spriteDirection = npc.direction;

                if (npc.frameCounter < 12)
                {
                    npc.frame.Y = 0 * frameHeight;
                }
                else if (npc.frameCounter < 24)
                {
                    npc.frame.Y = 1 * frameHeight;
                }
                else if (npc.frameCounter < 36)
                {
                    npc.frame.Y = 2 * frameHeight;
                }
                else if (npc.frameCounter < 48)
                {
                    npc.frame.Y = 3 * frameHeight;
                }
                else if (npc.frameCounter < 60)
                {
                    npc.frame.Y = 4 * frameHeight;
                }
                else if (npc.frameCounter < 72)
                {
                    npc.frame.Y = 5 * frameHeight;
                }
                else if (npc.frameCounter < 84)
                {
                    npc.frame.Y = 6 * frameHeight;
                }
                else if (npc.frameCounter < 96)
                {
                    npc.frame.Y = 7 * frameHeight;
                }
                else
                {
                    npc.frameCounter = 0;
                }
            }

            if (AI_State == State_Pursuing && (npc.velocity.Y != 0))
            {
                npc.frame.Y = 3 * frameHeight;
            }

            if (AI_State == State_Shielding)
            {
                npc.spriteDirection = npc.direction;
                npc.frame.Y = 8 * frameHeight;

                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 18 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 18)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }

            if (AI_State == State_Thrusting)
            {
                npc.spriteDirection = npc.direction;
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!


                if (AI_Timer < 10)
                {
                    npc.frame.Y = 9 * frameHeight;
                    npc.frameCounter = 0;
                }

                if (AI_Timer >= 10)
                {
                    npc.frameCounter++;

                    if (npc.frameCounter < 24)
                    {
                        npc.frame.Y = 11 * frameHeight;
                    }
                    else if (npc.frameCounter < 30)
                    {
                        npc.frame.Y = 12 * frameHeight;
                    }
                    else if (npc.frameCounter < 40)
                    {
                        npc.frame.Y = 13 * frameHeight;
                    }
                    else if (npc.frameCounter < 46)
                    {
                        npc.frame.Y = 10 * frameHeight;
                    }
                    else
                    {
                        npc.frameCounter = 0;
                    }
                }


                //Shieldshinies
                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 18 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 18)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }

            if (AI_State == State_JumpThrust)
            {
                npc.spriteDirection = npc.direction;

                if (AI_Timer < 20)
                {
                    npc.frame.Y = 19 * frameHeight; //Jump prep
                    npc.frameCounter = 0;
                }

                else if (AI_Timer < 52)
                {
                    npc.frame.Y = 15 * frameHeight; //In air, ascending
                }

                else if (AI_Timer < 82)
                {
                    npc.frame.Y = 16 * frameHeight; //In air, spear up
                }

                else if (AI_Timer >= 82 && !npc.collideY)
                {
                    npc.frame.Y = 17 * frameHeight;
                }

                else if (AI_Timer >= 82 && AI_Timer < 162 && npc.collideY)
                {
                    npc.frame.Y = 18 * frameHeight;
                }

                else if (AI_Timer >= 162)
                {
                    npc.frame.Y = 19 * frameHeight;
                }
            }

            if (AI_State == State_LungeThrust)
            {
                npc.spriteDirection = npc.direction;

                if (AI_Timer < 30)
                {
                    npc.frame.Y = 11 * frameHeight;
                }

                if (AI_Timer >= 30 && AI_Timer < 65)
                {
                    npc.frame.Y = 14 * frameHeight;
                }

                if (AI_Timer >= 65)
                {
                    npc.frame.Y = 19 * frameHeight;
                }
            }

        }

        #endregion

    }
}
