using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using static tsorcRevamp.SpawnHelper;


namespace tsorcRevamp.NPCs.Enemies
{
    class HollowSpearman : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 17;
        }

        public int spearStabDamage = 10;

        public override void SetDefaults()
        {
            NPC.timeLeft = 60;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = -1;
            NPC.damage = 18;
            NPC.defense = 10;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 105;
            if (Main.hardMode)
            {
                NPC.lifeMax = 270;
                NPC.defense = 25;
                NPC.damage = 28;
                NPC.value = 1250;
                spearStabDamage = 22;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 45;
                NPC.damage = 38;
                NPC.value = 4000;
                spearStabDamage = 32;
            }
            NPC.value = 500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;

            //Banner = NPC.type;
            //BannerItem = ModContent.ItemType<Banners.LothricSpearKnightBanner>();

            NPC.buffImmune[BuffID.Confused] = true;
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            player.AddBuff(BuffID.BrokenArmor, 3 * 60, false); //broken armor         
        }
        #endregion

        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Timer_Shielding_Slot = 1;
        private const int AI_Timer_Attacking_Slot = 2;
        private const int AI_Timer_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Shielding = 1;
        private const int State_Thrusting = 2;
        private const int State_Throwing = 3;



        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }
        public float AI_Timer_Shielding
        {
            get => NPC.ai[AI_Timer_Shielding_Slot];
            set => NPC.ai[AI_Timer_Shielding_Slot] = value;
        }

        public float AI_Timer_Attacking
        {
            get => NPC.ai[AI_Timer_Attacking_Slot];
            set => NPC.ai[AI_Timer_Attacking_Slot] = value;
        }

        public float AI_Timer
        {
            get => NPC.ai[AI_Timer_Slot];
            set => NPC.ai[AI_Timer_Slot] = value;
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];


            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.02f;
            float top_speed = (lifePercentage * -0.015f) + 2f; //Increase speed the lower the enemy HP%
            float braking_power = 0.11f; //Breaking power to slow down after moving above top_speed


            //Debug utilities

            //Main.NewText(NPC.Distance(player.Center));
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
            int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
            int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet
            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
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
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

            }

            if (AI_Timer_Attacking >= 400 && AI_Timer_Attacking < 442)
            {
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {

                #region Target player, turn if can't reach player


                if (AI_Timer == 0)
                {
                    NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }

                if (NPC.velocity.X == 0)
                {
                    AI_Timer++;
                    if (AI_Timer > 120 && NPC.velocity.Y == 0)
                    {
                        NPC.direction *= -1;
                        NPC.spriteDirection = NPC.direction;
                        AI_Timer = 50;
                    }
                }

                if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_Timer = 0;
                }

                #endregion

                #region Melee Movement & Drop through platforms - but also sometimes the world :(

                if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
                {
                    NPC.velocity *= (1f - braking_power); //breaking
                }

                else
                {
                    NPC.velocity.X += NPC.direction * acceleration; //accelerating
                }


                if (NPC.direction == 1) //breaking power after turning, to turn fast or to "slip"
                {
                    if (NPC.velocity.X > -top_speed)
                    {
                        NPC.velocity.X += 0.085f;
                    }
                }

                else
                {
                    if (NPC.velocity.X < top_speed)
                    {
                        NPC.velocity.X += -0.085f;
                    }
                }

                //Speed limits
                if (NPC.velocity.X > 4f) //hard limit of 10f
                {
                    NPC.velocity.X = 4f;
                }

                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X = -4f;
                }


                if (Math.Abs(NPC.velocity.X) > 4f || Math.Abs(NPC.velocity.Y) > 0.1f) //If moving at high speed.X or vertically, become knockback immune
                {
                    NPC.knockBackResist = 0;
                }
                else
                {
                    NPC.knockBackResist = 0.1f; //aparently it doesn't default back? 
                }


                if (Main.tile[(int)NPC.position.X / 16, y_below_feet].TileType == TileID.Platforms && Main.tile[(int)(NPC.position.X + (float)NPC.width) / 16, y_below_feet].TileType == TileID.Platforms && NPC.position.Y < (player.position.Y - 4 * 16))
                {
                    NPC.noTileCollide = true;
                }
                else { NPC.noTileCollide = false; }

                #endregion

                #region New Tile()s, jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    if (NPC.position.Y > player.position.Y + 3 * 16 && NPC.position.Y < player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                    {
                        NPC.velocity.Y = -8f; // jump with power 8 if directly under player
                        NPC.netUpdate = true;
                    }

                    if (NPC.position.Y >= player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                    {
                        NPC.velocity.Y = -9.5f; // jump with power 9.5 if directly under player
                        NPC.netUpdate = true;
                    }


                    if (Main.tile[x_in_front, y_above_feet] == null)
                    {
                        Main.tile[x_in_front, y_above_feet].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 1].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 2] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 2].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet - 3] == null)
                    {
                        Main.tile[x_in_front, y_above_feet - 3].ClearTile();
                    }

                    if (Main.tile[x_in_front, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front, y_above_feet + 1].ClearTile();
                    }
                    //  create? 2 other tiles farther in front
                    if (Main.tile[x_in_front + NPC.direction, y_above_feet - 1] == null)
                    {
                        Main.tile[x_in_front + NPC.direction, y_above_feet - 1].ClearTile();
                    }

                    if (Main.tile[x_in_front + NPC.direction, y_above_feet + 1] == null)
                    {
                        Main.tile[x_in_front + NPC.direction, y_above_feet + 1].ClearTile();
                    }

                    else // standing on solid tile but not in front of a passable door
                    {
                        if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                        {  //  moving forward
                            if (Main.tile[x_in_front, y_above_feet - 2].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].TileType])
                            { // 3 blocks above ground level(head height) blocked
                                if (Main.tile[x_in_front, y_above_feet - 3].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].TileType])
                                { // 4 blocks above ground level(over head) blocked
                                    NPC.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
                                    NPC.netUpdate = true;
                                }
                                else
                                {
                                    NPC.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
                                    NPC.netUpdate = true;
                                }
                            } // for everything else, head height clear:
                            else if (Main.tile[x_in_front, y_above_feet - 1].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].TileType])
                            { // 2 blocks above ground level(mid body height) blocked
                                NPC.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                                NPC.netUpdate = true;
                            }
                            else if (Main.tile[x_in_front, y_above_feet].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].TileType])
                            { // 1 block above ground level(foot height) blocked
                                NPC.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                                NPC.netUpdate = true;
                            }
                            else if (NPC.directionY < 0 && (!Main.tile[x_in_front, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                            { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                                NPC.velocity.Y = -8f; // jump with power 8
                                NPC.velocity.X = NPC.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                                NPC.netUpdate = true;
                            }

                        } // END moving forward, still: standing on solid tile but not in front of a passable door
                    }
                }

                #endregion


                if (NPC.Distance(player.Center) < 250 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_Timer_Shielding++;
                }

                if (NPC.Distance(player.Center) < 95 && standing_on_solid_tile && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_Timer_Shielding = 300;
                    AI_State = State_Shielding;
                }

                if (AI_Timer_Shielding >= 300 && standing_on_solid_tile)
                {
                    AI_State = State_Shielding;
                }

                if (AI_Timer_Attacking >= 420 && NPC.Distance(player.Center) > 7f * 16 && NPC.Distance(player.Center) < 20f * 16 && standing_on_solid_tile && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Throwing;
                }
            }


            // SHIELDING
            if (AI_State == State_Shielding)
            {
                NPC.TargetClosest(true);
                AI_Timer_Shielding++;

                if (NPC.velocity.Y == 0)
                {
                    if (AI_Timer_Shielding > 300 && AI_Timer_Shielding <= 310 && Math.Abs(NPC.velocity.X) > 1f)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; }
                        else { NPC.velocity.X += 0.15f; }
                    }

                    if (AI_Timer_Shielding > 310)
                    {
                        NPC.velocity.X = 0;
                    }

                    if (AI_Timer_Shielding > 500)
                    {
                        AI_State = State_Pursuing;
                        AI_Timer_Shielding = 0;
                    }
                }

                if (AI_Timer_Shielding > 310 && Math.Abs(NPC.Center.X - player.Center.X) <= 6.5f * 16 && Math.Abs(NPC.Center.Y - player.Center.Y) <= 6.5f * 16 && standing_on_solid_tile && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Thrusting;
                }

                if (AI_Timer_Shielding > 310 && AI_Timer_Attacking >= 420 && Math.Abs(NPC.Center.X - player.Center.X) > 6.5f * 16 && Math.Abs(NPC.Center.Y - player.Center.Y) > 6.5f * 16 && standing_on_solid_tile && NPC.velocity.Y == 0 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    AI_State = State_Throwing;
                }
            }


            //THRUSTING (While shielding)
            if (AI_State == State_Thrusting)
            {
                AI_Timer++;
                AI_Timer_Shielding = 400;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;


                #region Projectiles & Sounds
                if (NPC.direction == 1)
                {
                    if (AI_Timer == 34)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }

                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }

                    if (AI_Timer == 77)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = 5;
                    }
                }
                else
                {
                    if (AI_Timer == 34)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }

                    if (AI_Timer == 50)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }

                    if (AI_Timer == 76)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-46, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI, 3)];
                        stab.timeLeft = 6;
                        stab.velocity.X = -5;
                    }
                }

                #endregion


                if (AI_Timer > 94)
                {
                    AI_Timer = 0;
                    AI_State = State_Shielding;
                }
            }

            //THROWING SPEAR
            if (AI_State == State_Throwing)
            {
                AI_Timer++;
                NPC.TargetClosest(true);

                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;

                if (AI_Timer_Shielding > 310) //If it was already shielding
                {
                    AI_Timer_Shielding = 400;
                }

                if (AI_Timer == 60)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                    Projectile spear = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -16), UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center + new Vector2(0, -20), 10), ModContent.ProjectileType<Projectiles.ThrowingSpear>(), spearStabDamage, 5, Main.myPlayer, NPC.whoAmI)];
                    spear.friendly = false;
                    spear.hostile = true;
                }

                if (AI_Timer > 90)
                {
                    AI_Timer = 0;
                    AI_Timer_Attacking = 0;
                    if (AI_Timer_Shielding == 400) //If previously shielding, go back to shielding
                    {
                        AI_State = State_Shielding;
                    }
                    else
                    {
                        AI_State = State_Pursuing;
                    }
                }
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Throwing)
            {
                if (NPC.ai[1] < 370)
                {
                    NPC.ai[1] += 30; //Used for Jump-slash
                }

                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 15;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 15;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
            }

            if (NPC.direction == 1) //if enemy facing right
            {
                if (player.position.X < NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }

            NPC.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[NPC.target];
            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.BlizzardBlasterShot>())
            {
                if (AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Throwing)
                {

                    if (NPC.direction == 1) //if npc facing right
                    {
                        if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 15;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }

                        else if (modifiers.HitDirection == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 15;
                            modifiers.Knockback *= 0.1f;

                            if (NPC.ai[1] < 380)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 15;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                        else if (modifiers.HitDirection == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 15;

                            modifiers.Knockback *= 0.1f;
                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                }


                if (NPC.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (modifiers.HitDirection == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }
                else //if enemy facing left
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (modifiers.HitDirection == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }

                if (NPC.Distance(player.Center) > 220 && AI_State != State_Shielding)
                {
                    NPC.ai[2] += 100;
                }

                if (NPC.ai[1] < 400)
                {
                    NPC.ai[1] += 10;
                }
            }
        }


        #endregion

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 1f) return 0f;

            if (spawnInfo.Water || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (!Main.hardMode && spawnInfo.SpawnTileType == TileID.GreenDungeonBrick && !spawnInfo.Water) return 0.07f;
            if (!spawnInfo.Water && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonSlabUnsafe || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonUnsafe) && !Main.hardMode && !tsorcRevampWorld.SuperHardMode) return 0.05f;
            if (Main.hardMode && spawnInfo.Lihzahrd) return 0.05f;
            if (Main.hardMode && p.ZoneNormalCaverns && !spawnInfo.Water) return 0.01f;
            if (Main.hardMode && p.ZoneDesert && p.ZoneOverworldHeight && !spawnInfo.Water) return 0.025f;
            if (Main.hardMode && p.ZoneUndergroundDesert && !spawnInfo.Water) return 0.035f;
            if (Main.hardMode && spawnInfo.SpawnTileType == TileID.BlueDungeonBrick && !spawnInfo.Water) return 0.09f;
            if (Main.hardMode && spawnInfo.SpawnTileType == TileID.TungstenBrick && !spawnInfo.Water) return 0.08f;

            if (tsorcRevampWorld.SuperHardMode && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUnderworldHeight)) return 0.115f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSkyHeight)) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDesert && !Ocean) return 0.15f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneUnderworldHeight) return 0.08f; //.08% is 4.28%


            if (Main.expertMode && Main.bloodMoon && spawnInfo.Player.ZoneOverworldHeight && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSkyHeight)) return chance = 0.04f;

            if (Main.expertMode && Main.bloodMoon && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.02f;


            //no longer spawns on the surface 

            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.dayTime && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow)) return chance = 0.035f;
            if (((!Main.expertMode && (NPC.downedBoss1 || NPC.downedBoss2)) || Main.expertMode) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && !Main.dayTime && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow)) return chance = 0.05f;

            // removed chance to spawn globally

            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 15));
        }

        #region Drawing & Animation


        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        /*public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection

            Texture2D texture = TextureAssets.Npc[NPC.type].Value; //Base texture, manually drawing so as to not have a ridiculously big canvas size in order to have a centered hitbox
            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), lightColor, NPC.rotation, new Vector2(37, 46), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 116, 88), lightColor, NPC.rotation, new Vector2(80, 46), NPC.scale, effects, 0f);
            }
            return false; //Don't draw base sprite
        }*/

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowWarrior_Shield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 10, 0, shieldFrame);
            if ((AI_State == State_Shielding || AI_State == State_Thrusting || AI_State == State_Throwing) && NPC.velocity.X == 0)
            {
                if (NPC.spriteDirection == 1)
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
            }
        }


        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Pursuing && NPC.velocity.X != 0) //Walking anim
            {

                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 72)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 84)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 96)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if (AI_State == State_Pursuing && (NPC.velocity.Y != 0))
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            if (AI_State == State_Shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;

                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!

                //Shieldshinies
                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
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
                NPC.spriteDirection = NPC.direction;
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!


                if (AI_Timer < 10)
                {
                    NPC.frame.Y = 11 * frameHeight;
                    NPC.frameCounter = 0;
                }

                if (AI_Timer >= 10)
                {
                    NPC.frameCounter++;

                    if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else if (NPC.frameCounter < 30)
                    {
                        NPC.frame.Y = 13 * frameHeight;
                    }
                    else if (NPC.frameCounter < 40)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else if (NPC.frameCounter < 46)
                    {
                        NPC.frame.Y = 12 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }

                //Shieldshinies
                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }

            if (AI_State == State_Throwing)
            {
                NPC.spriteDirection = NPC.direction;
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!


                if (AI_Timer < 5)
                {
                    NPC.frame.Y = 10 * frameHeight;
                    NPC.frameCounter = 0;
                }

                if (AI_Timer >= 5)
                {
                    NPC.frameCounter++;

                    if (NPC.frameCounter < 8)
                    {
                        NPC.frame.Y = 14 * frameHeight;
                    }
                    else if (NPC.frameCounter < 60)
                    {
                        NPC.frame.Y = 15 * frameHeight;
                    }
                    else if (NPC.frameCounter < 90)
                    {
                        NPC.frame.Y = 16 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }

                //Shieldshinies
                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 9 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 9)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }
        }

        #endregion

    }
}

