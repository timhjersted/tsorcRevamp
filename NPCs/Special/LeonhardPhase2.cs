using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Special
{
    public class LeonhardPhase2 : ModNPC
    {
        //public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 22;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0.1f;
            NPC.aiStyle = -1;
            NPC.damage = 25; //Low contact damage, the slashes will be doing the damage
            NPC.defense = 10;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 4000;
            NPC.value = 10000;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath58;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Frostburn] = true;
            despawnHandler = new NPCDespawnHandler(null, Color.Teal, 54);

        }


        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++) //Blood splatter from being hit
            {
                int dustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0 && !NPC.dontTakeDamage) //Start aftermath
            {
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.ai[1] = 0;
            }
        }

        public float Boredom
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;

        }


        #region AI


        bool jump = false;
        bool throwing = false;
        bool buffing = false;
        bool weaponBuffed = false;

        int buffingTimer = 0;


        public override void AI()
        {
            Player player = Main.player[NPC.target];

            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (Main.netMode != NetmodeID.MultiplayerClient && !Main.raining)
            {
                Main.raining = true;
                Main.rainTime = 18000;

                if (Main.cloudBGActive >= 1f || Main.numClouds > 150)
                {
                    if (Main.rand.Next(3) == 0)
                    {
                        Main.maxRaining = Main.rand.Next(20, 90) * 0.01f;
                    }
                    else
                    {
                        Main.maxRaining = Main.rand.Next(40, 90) * 0.01f;
                    }
                }
                else if (Main.numClouds > 100)
                {
                    if (Main.rand.Next(3) == 0)
                    {
                        Main.maxRaining = Main.rand.Next(10, 70) * 0.01f;
                    }
                    else
                    {
                        Main.maxRaining = Main.rand.Next(20, 60) * 0.01f;
                    }
                }
                else if (Main.rand.Next(3) == 0)
                {
                    Main.maxRaining = Main.rand.Next(5, 40) * 0.01f;
                }
                else
                {
                    Main.maxRaining = Main.rand.Next(5, 30) * 0.01f;
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }

            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.04f;
            //float top_speed = (lifePercentage * 0.02f) + .2f; //good calculation to remember for decreasing speed the lower the enemy HP%
            float top_speed = (lifePercentage * -0.02f) + 4.5f; //good calculation to remember for increasing speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed

            if (!NPC.dontTakeDamage)
            {

                #region target/face player

                NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)

                if (NPC.velocity.X == 0f && !throwing && !buffing)
                {
                    if (NPC.velocity.Y == 0f)
                    { // not moving
                        if (NPC.ai[0] == 0f)
                            NPC.ai[0] = 1f; // facing change delay
                        else
                        { // change movement and facing direction, reset delay
                            NPC.direction *= -1;
                            NPC.spriteDirection = NPC.direction;
                            NPC.ai[0] = 0f;
                        }
                    }
                }
                else // moving in x direction,
                    NPC.ai[0] = 0f; // reset facing change delay

                if (NPC.direction == 0) // what does it mean if direction is 0?
                    NPC.direction = 1; // flee right if direction not set? or is initial direction?

                #endregion

                #region melee movement

                if (!throwing && !buffing)
                {
                    if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
                    {
                        NPC.velocity *= (1f - braking_power); //breaking
                    }

                    else
                    {
                        NPC.velocity.X += NPC.direction * acceleration; //accelerating
                    }

                    //breaking power after turning, to turn fast or to "slip"
                    if (NPC.direction == 1)
                    {
                        if (NPC.velocity.X > -top_speed)
                        {
                            NPC.velocity.X += 0.085f;
                        }
                        NPC.netUpdate = true;
                    }
                    if (NPC.direction == -1)
                    {
                        if (NPC.velocity.X < top_speed)
                        {
                            NPC.velocity.X += -0.085f;
                        }
                        NPC.netUpdate = true;
                    }
                }

                if (Math.Abs(NPC.velocity.X) > 4f) //If moving at high speed, become knockback immune
                {
                    NPC.knockBackResist = 0;
                }
                if (Math.Abs(NPC.velocity.Y) > 0.1f) //If moving vertically, become knockback immune
                {
                    NPC.knockBackResist = 0;
                }
                else
                {
                    NPC.knockBackResist = 0.1f; //If not moving at high speed, default back to taking some knockback
                }

                NPC.noTileCollide = false;

                int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                if (Main.tile[(int)NPC.position.X / 16, y_below_feet].TileType == TileID.Platforms && Main.tile[(int)(NPC.position.X + (float)NPC.width) / 16, y_below_feet].TileType == TileID.Platforms && NPC.position.Y < (player.position.Y - 4 * 16))
                {
                    NPC.noTileCollide = true; //My sad attempt at making AI drop through platforms
                }

                #endregion

                #region Anti-cheese & Weapon buffing

                if (((!player.wet && NPC.wet) || Boredom > 3000))
                {
                    NPC.velocity.Y += 2;
                    NPC.velocity.X = 0;
                    NPC.ai[1] = 240;
                    Boredom = 0;

                    for (int i = 0; i < 100; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 89, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 50, default(Color), 1f)];
                        dust2.noGravity = true;
                    }

                    SoundEngine.PlaySound(SoundID.Item81 with { PitchVariance = 0.3f }, NPC.position);

                    //if the tile above is solid and active
                    if (!Framing.GetTileSafely((int)player.position.X / 16, ((int)player.position.Y - 5 * 16) / 16).IsActuated && Main.tileSolid[Framing.GetTileSafely((int)player.position.X / 16, ((int)player.position.Y - 5 * 16) / 16).TileType])
                    {
                        if (player.direction == 1 && Main.tileSolid[Framing.GetTileSafely(((int)player.position.X + 4 * 16) / 16, (int)player.position.Y / 16).TileType]) { NPC.position = player.position + new Vector2(-3 * 16, 0); } //teleport behind player
                        else if (Main.tileSolid[Framing.GetTileSafely(((int)player.position.X - 3 * 16) / 16, (int)player.position.Y / 16).TileType]) { NPC.position = player.position + new Vector2(4 * 16, 0); }
                        else { NPC.position = player.position + new Vector2(0, -4 * 16); }
                    }
                    else //if air
                    {
                        if (Math.Abs(NPC.Center.Y - player.Center.Y) == 113)
                        {
                            NPC.position = player.position + new Vector2(0, -4 * 16);
                        }
                        else if (player.Center.Y - NPC.Center.Y == 159 || NPC.Center.Y - player.Center.Y == 129 || NPC.position.Y - player.position.Y == -94)
                        {
                            NPC.position = player.position + new Vector2(3 * 16, 0);
                        }

                        else
                        {
                            NPC.position = player.position + new Vector2(0, -10 * 16); //teleport above player
                        }
                    }
                }

                if (NPC.life < 3000 && (!weaponBuffed || buffing) && !throwing && Math.Abs(NPC.velocity.X) < 3 && Math.Abs(NPC.velocity.Y) == 0 && buffingTimer <= 100)
                {
                    NPC.knockBackResist = 0;
                    buffing = true;
                    buffingTimer++;

                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    Boredom = 0;

                    NPC.spriteDirection = NPC.direction;
                    if (Math.Abs(NPC.velocity.X) < 1) { NPC.velocity.X = 0; }
                    else if (buffingTimer > 10 && buffingTimer < 50) { NPC.velocity.X *= 0.96f; }

                    if (buffingTimer == 60)
                    {
                        weaponBuffed = true;
                        SoundEngine.PlaySound(SoundID.Item20, NPC.Center);


                        for (int i = 0; i < 50; i++)
                        {
                            if (NPC.direction == 1)
                            {
                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 18, NPC.position.Y - 8), 36, 36, 89, 0, Main.rand.NextFloat(-1, -3), 50, default(Color), Main.rand.NextFloat(.7f, 1.2f))];
                                dust2.noGravity = true;
                            }
                            else
                            {
                                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 32, NPC.position.Y - 8), 36, 36, 89, 0, Main.rand.NextFloat(-1, -3), 50, default(Color), Main.rand.NextFloat(.7f, 1.2f))];
                                dust2.noGravity = true;
                            }
                        }

                    }
                    if (buffingTimer == 100)
                    {
                        buffing = false;
                    }

                }
                #endregion

                #region check if standing on a solid tile
                bool standing_on_solid_tile = false;
                if (NPC.velocity.Y == 0f) // no jump/fall
                {
                    int x_left_edge = (int)NPC.position.X / 16;
                    int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                    for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                    {
                        if (Main.tile[l, y_below_feet] == null) // null tile means ??
                            return;

                        if (!Main.tile[l, y_below_feet].IsActuated && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                        {
                            standing_on_solid_tile = true;
                            break; // one is enough so stop checking
                        }
                    } // END traverse blocks under feet
                } // END no jump/fall
                #endregion

                #region jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
                    int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet

                    // standing on solid tile but not in front of a passable door                    
                    if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                    {  //  moving forward
                        if (!Main.tile[x_in_front, y_above_feet - 2].IsActuated && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].TileType])
                        { // 3 blocks above ground level(head height) blocked
                            if (!Main.tile[x_in_front, y_above_feet - 3].IsActuated && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].TileType])
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
                        else if (!Main.tile[x_in_front, y_above_feet - 1].IsActuated && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].TileType])
                        { // 2 blocks above ground level(mid body height) blocked
                            NPC.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                            NPC.netUpdate = true;
                        }
                        else if (!Main.tile[x_in_front, y_above_feet].IsActuated && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].TileType])
                        { // 1 block above ground level(foot height) blocked
                            NPC.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                            NPC.netUpdate = true;
                        }
                        else if (NPC.directionY < 0 && (Main.tile[x_in_front, y_above_feet + 1].IsActuated || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].IsActuated || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                        { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                            NPC.velocity.Y = -8f; // jump with power 8
                            NPC.velocity.X = NPC.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                            NPC.netUpdate = true;
                        }
                    } // END moving forward, still: standing on solid tile but not in front of a passable door

                }

                #endregion

                #region attacks

                //AI 0 is used for turn delay
                ++NPC.ai[1]; //Used for both Basic Slash and Dashes - lots of stuff tbh
                //AI 2 is used for the throwing portion of the firbomb throw
                //AI 3 is used as the main counter up to throwing a firebomb, and teleporting to the player if he can't firebomb them

                //Basic Slash Attack

                if (NPC.ai[1] >= 30 && NPC.Distance(player.Center) < 75 && !throwing) //If 30 ticks or more have passed, and player is within slash range
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.position); //Play Death Sickle sound
                    Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and NPC center
                    Vector2 spawnPosition = new Vector2(34, 0).RotatedBy(difference.ToRotation()); //34 is the distance we will spawn the projectile away from NPC.Center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (weaponBuffed)
                        {
                            if (Math.Abs(NPC.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 18, 0f, Main.myPlayer, 10, NPC.whoAmI);

                            }
                            else if (Math.Abs(NPC.velocity.X) >= 4.5f) //If dashing, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 22, 0f, Main.myPlayer, 10, NPC.whoAmI);
                            }
                        }
                        else
                        {
                            if (Math.Abs(NPC.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 14, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }
                            else if (Math.Abs(NPC.velocity.X) >= 4.5f) //If dashing, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 18, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }
                        }
                    }
                    NPC.ai[1] = 0; //Reset timer
                }


                #region Dashes


                //Long Dash from long range

                if (NPC.ai[1] >= 120 && Math.Abs(NPC.Center.X - player.Center.X) >= 26 * 16 && NPC.velocity.Y == 0 && NPC.ai[2] < 1 && Math.Abs(NPC.velocity.X) < 4.5f && standing_on_solid_tile) //If over 2 seconds have passed, the player is 320 pixels away from the NPC and if it isn't moving vertically
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X += 16f; //Longer dash, this dash will always happen before the shorter dash if the player is far away enough (note timer >= 120, versus timer > 120 on shorter dash)
                        NPC.velocity.Y -= 3f; //A bit of upward velocity to take NPC off the ground
                    }
                    else
                    {
                        NPC.velocity.X -= 16f;
                        NPC.velocity.Y -= 3f;
                    }
                    NPC.ai[1] = 80; //Set timer so he can slash straight after a long dash, and dash again shortly after, in order to catch up to a speedy player
                }


                //Closer Dash, decide between one or the other

                int dashChoice;

                if (NPC.ai[1] > 120 && Math.Abs(NPC.Center.X - player.Center.X) > 20 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 26 * 16 && NPC.velocity.Y == 0 && NPC.ai[2] < 1 && Math.Abs(NPC.velocity.X) < 4.5f && standing_on_solid_tile)
                {
                    dashChoice = Main.rand.Next(1, 3);
                    //Main.NewText(dashChoice);

                    if (dashChoice == 1) //speed up into jump-dash
                    {
                        jump = true;
                    }
                    if (dashChoice == 2) //shorter hop-dash
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X += 10f;
                            NPC.velocity.Y -= 3f;
                        }
                        else
                        {
                            NPC.velocity.X -= 10f;
                            NPC.velocity.Y -= 3f;
                        }
                    }

                    NPC.ai[1] = 30;
                }

                if (jump)
                {
                    if (standing_on_solid_tile && NPC.ai[2] < 20)
                    {
                        ++NPC.ai[2];
                    }

                    if (NPC.ai[2] != 0 && NPC.ai[2] < 20)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X += 0.75f;
                        }

                        else
                        {
                            NPC.velocity.X += -0.75f;
                        }
                    }

                    if (NPC.ai[2] == 20 && standing_on_solid_tile)
                    {
                        NPC.velocity.Y = -8f;
                    }
                    if (NPC.ai[2] == 20 && !standing_on_solid_tile)
                    {
                        NPC.ai[2]++;
                    }
                    if (NPC.ai[2] > 20 && standing_on_solid_tile)
                    {
                        jump = false;
                        NPC.ai[2] = 0;
                        if (weaponBuffed && NPC.ai[1] > 20 && NPC.Distance(player.Center) < 550)
                        {

                            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center); //Play Death Sickle sound
                            Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and NPC center
                            Vector2 spawnPosition = new Vector2(34, 0).RotatedBy(difference.ToRotation()); //34 is the distance we will spawn the projectile away from NPC.Center
                            Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, new Vector2(8f, 0).RotatedBy(difference.ToRotation()), ModContent.ProjectileType<Projectiles.Enemy.LeonhardCMSCrescent>(), 22, 0f, Main.myPlayer, 10, 0);
                            }

                            NPC.ai[1] = 0;
                        }
                    }
                }


                //Short dash, regardless of distance, after a longer timer. Intended to give meele players a dash to dodge after getting a few hits in

                if (NPC.ai[1] > 200 && NPC.ai[1] < 240 && NPC.ai[2] < 1)
                {
                    if (NPC.direction == 1) //Small eye dust to warn player that a close range dash is coming...
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 8, NPC.position.Y + 6), 4, 4, 89, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.5f)];
                        dust2.velocity *= 0f;
                        dust2.noGravity = true;
                        dust2.fadeIn = .3f;
                        dust2.velocity += NPC.velocity;
                    }

                    if (NPC.direction == -1)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 4, NPC.position.Y + 6), 4, 4, 89, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.5f)];
                        dust2.velocity *= 0f;
                        dust2.noGravity = true;
                        dust2.fadeIn = .3f;
                        dust2.velocity += NPC.velocity;
                    }
                }

                if (NPC.ai[1] >= 240 && NPC.ai[1] <= 280 && NPC.ai[2] < 1)
                {
                    if (NPC.direction == 1) //Large eye dust to warn player that a close range dash is imminent - "jump now or get rekt"
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 8, NPC.position.Y + 6), 4, 4, 89, NPC.velocity.X, NPC.velocity.Y, 50, default(Color), 1f)];
                        dust2.velocity *= 0f;
                        dust2.noGravity = true;
                        dust2.fadeIn = 1f;
                        dust2.velocity += NPC.velocity;
                    }

                    if (NPC.direction == -1)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 4, NPC.position.Y + 6), 4, 4, 89, NPC.velocity.X, NPC.velocity.Y, 50, default(Color), 1f)];
                        dust2.velocity *= 0f;
                        dust2.noGravity = true;
                        dust2.fadeIn = 1f;
                        dust2.velocity += NPC.velocity;
                    }
                }

                if (NPC.ai[1] > 270 && NPC.velocity.Y == 0 && NPC.ai[2] < 1) //Short dash
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X += 10f;
                        NPC.velocity.Y -= 3f;
                    }
                    else
                    {
                        NPC.velocity.X -= 10f;
                        NPC.velocity.Y -= 3f;
                    }
                }

                if (NPC.ai[1] > 280 && NPC.ai[2] < 1) //Timer resets mid dash, to allow eye dust trail during dash, note how eye dust timer ends at the same time
                {
                    NPC.ai[1] = 30; //So he can slash straight after a dash
                }

                #endregion


                if (NPC.velocity.Length() > 22)
                {
                    NPC.velocity = Vector2.Normalize(NPC.velocity) * 22;
                }

                //Firebomb throw


                Boredom += 2;

                if (lifePercentage < 50) { Boredom++; } //Add an additional 1 to counter if low hp
                if (lifePercentage < 30) { Boredom++; } //Add an additional 1 to counter if very low hp

                //No teleporting if he has line of sight and the player isn't high above or below him
                //Allow teleporting if the boss is wet and the player is not (aka it fell in a lake lol)
                if (Collision.CanHit(NPC, Main.player[NPC.target]) && Math.Abs(Main.player[NPC.target].Center.Y - NPC.Center.Y) < 50 && (!NPC.wet || Main.player[NPC.target].wet))
                {
                    if (Boredom > 1201)
                    {
                        Boredom = 1201;
                    }
                }

                if (Math.Abs(NPC.Center.Y - player.Center.Y) > 50 && Math.Abs(player.velocity.Y) < 4 && Math.Abs(player.velocity.X) < 2)
                {
                    Boredom += 12; //this is to speed up his teleport if youre unreachable.
                }

                if (Boredom > 1000 && NPC.ai[2] == 0)
                {
                    if (Main.rand.NextBool(15))
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X + NPC.width / 2, NPC.position.Y + 14), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 2f);
                        Main.dust[dust].noGravity = true;
                    }
                }

                if (Boredom > 1200 && !jump && standing_on_solid_tile && NPC.velocity.Y == 0 && Math.Abs(NPC.Center.X - player.Center.X) < 15 * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    throwing = true;
                }

                if (throwing && standing_on_solid_tile && !jump && NPC.velocity.Y == 0)
                {
                    if (Math.Abs(NPC.velocity.X) < 5f)
                    {
                        if (NPC.velocity.X > 0.2)
                        {
                            NPC.velocity.X -= 0.2f;
                        }

                        if (NPC.velocity.X < -0.2)
                        {
                            NPC.velocity.X += 0.2f;
                        }
                    }

                    if (Math.Abs(NPC.velocity.X) >= 5f)
                    {
                        if (NPC.velocity.X > 0.2)
                        {
                            NPC.velocity.X -= 0.6f;
                        }

                        if (NPC.velocity.X < -0.2)
                        {
                            NPC.velocity.X += 0.6f;
                        }
                    }

                    if (Math.Abs(NPC.velocity.X) < 0.21)
                    {
                        NPC.ai[2]++;
                        NPC.velocity.X = 0;

                        if (NPC.ai[2] == 35)
                        {
                            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.position); //Play swing-throw sound
                            Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and NPC center
                            Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                            Vector2 throwpower = (Main.player[NPC.target].Center - NPC.Center) / 30;

                            throwpower.Y += Main.rand.Next(-4, -2);

                            if (throwpower.Y > 10)
                            {
                                throwpower.Y = 10;
                            }

                            if (throwpower.X > 10)
                            {
                                throwpower.X = 10;
                            }
                            if (throwpower.X < -10)
                            {
                                throwpower.X = -10;
                            }
                            if (throwpower.X < 3 && throwpower.X >= 0)
                            {
                                throwpower.X = 3;
                            }
                            if (throwpower.X > -3 && throwpower.X < 0)
                            {
                                throwpower.X = -3;
                            }

                            velocity += throwpower;


                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 25, 0f, Main.myPlayer);
                            }
                        }

                        if (NPC.ai[2] == 50)
                        {
                            NPC.ai[2] = 0;
                            Boredom = 0;
                            throwing = false;
                        }
                    }
                }
                #endregion

            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.minion)
            {
                modifiers.Knockback.Scale(0); //to prevent slime staff from stunlocking him
            }
        }
        #endregion


        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor)
        {
            if (Boredom > 2500)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 89, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 50, default(Color), 1f)];
                    dust2.noGravity = true;
                }
            }
            if (weaponBuffed)
            {
                if (Main.rand.NextBool(5))
                {
                    if (NPC.direction == 1)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 18, NPC.position.Y - 14), 36, 36, 89, 0, 0, 50, default(Color), Main.rand.NextFloat(.7f, 1.2f))];
                        dust2.noGravity = true;
                        dust2.velocity *= 0;
                    }
                    else
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X - 32, NPC.position.Y - 14), 36, 36, 89, 0, 0, 50, default(Color), Main.rand.NextFloat(.7f, 1.2f))];
                        dust2.noGravity = true;
                        dust2.velocity *= 0;
                    }
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) //PreDraw for trails
        {

            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if (NPC.velocity.X > 8f || NPC.velocity.X < -8f)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, NPC.gfxOffY); //Where to draw trails
                    Color color = NPC.GetAlpha(drawColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw((Texture2D)Terraria.GameContent.TextureAssets.Npc[NPC.type], drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(NPC.position.X + 26, NPC.position.Y + 12), NPC.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                }
            }
            return true;
        }

        Texture2D firebombTexture;
        Texture2D glowTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Boredom > 1000)
            {
                UsefulFunctions.EnsureLoaded(ref firebombTexture, "tsorcRevamp/NPCs/Special/Leonhard_Firebomb");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), drawColor, NPC.rotation, new Vector2(37, 32), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), drawColor, NPC.rotation, new Vector2(37, 32), NPC.scale, effects, 0);
                }
            }

            if (weaponBuffed)
            {
                UsefulFunctions.EnsureLoaded(ref glowTexture, "tsorcRevamp/NPCs/Special/LeonhardPhase2_Glow");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), Color.White * .5f, NPC.rotation, new Vector2(37, 32), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), Color.White * .5f, NPC.rotation, new Vector2(37, 32), NPC.scale, effects, 0);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.X != 0 && !NPC.dontTakeDamage) //if moving
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.5f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 72)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 84)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 96)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 108)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else if (NPC.frameCounter < 120)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (NPC.frameCounter < 132)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (NPC.frameCounter < 144)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.frameCounter < 156)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.frameCounter < 168)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if (NPC.velocity.Y != 0) //If falling/jumping
            {
                NPC.frame.Y = 0 * frameHeight;
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && !NPC.dontTakeDamage) //If not moving at all (aka firebomb throwing)
            {
                NPC.frame.Y = 6 * frameHeight;
            }

            if (throwing) //throwing anim
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[2] < 35 && NPC.velocity.X == 0)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                if (NPC.ai[2] >= 35 && NPC.ai[2] <= 41)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                if (NPC.ai[2] > 41)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && NPC.dontTakeDamage) //If not moving at all, once defeated
            {
                //NPC.frame.Y = 18 * frameHeight;
            }

        }

        #endregion
    }
}
