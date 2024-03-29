using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Projectiles.Melee.Boomerangs;

namespace tsorcRevamp.NPCs.Special
{
    public class Faraam : ModNPC
    {

        NPCDespawnHandler despawnHandler;
        public int ThisNPC => ModContent.NPCType<Faraam>();

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 27;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 200;
            NPC.knockBackResist = 0.3f;
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            if (NPC.downedBoss1 || NPC.downedBoss2) { NPC.damage = 18; }
            else { NPC.damage = 12; } //Low contact damage, the slashes will be doing the damage
            if (NPC.downedBoss1 || NPC.downedBoss2) { NPC.lifeMax = 3500; }
            else { NPC.lifeMax = 2500; }
            NPC.defense = 8;
            NPC.value = 15000;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath58;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.lavaImmune = true;
            despawnHandler = new NPCDespawnHandler(null, Color.Teal, 54);
        }


        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++) //Blood splatter from being hit
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0 && !NPC.dontTakeDamage) //Start aftermath
            {
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.ai[1] = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter, NPC.Center);
                int leftOrRightVel = Main.rand.Next(-1, 1);
                if (leftOrRightVel == 0) { leftOrRightVel = 1; }

                if (NPC.direction == 1)
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(NPC.TopRight + new Vector2(-6, 8), 8, 8, 89, 0, 0, 50, default(Color), 0.8f)];
                        dust2.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.TopRight + new Vector2(-6, 8), new Vector2(leftOrRightVel, Main.rand.Next(-3, -2)), ModContent.ProjectileType<ShatteredMoonlightProjectile>(), 8, 0, 0, 2);
                    }
                }
                else
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(NPC.position + new Vector2(6, 8), 8, 8, 89, 0, 0, 50, default(Color), 0.8f)];
                        dust2.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position + new Vector2(6, 8), new Vector2(leftOrRightVel, Main.rand.Next(-3, -2)), ModContent.ProjectileType<ShatteredMoonlightProjectile>(), 8, 0, 0, 2);
                    }
                }
            }
        }


        #region AI

        bool jump = false;
        bool throwing = false;

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.04f;
            //float top_speed = (lifePercentage * 0.02f) + .2f; //good calculation to remember for decreasing speed the lower the enemy HP%
            float top_speed = (lifePercentage * -0.02f) + 4f; //good calculation to remember for increasing speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed

            if (!NPC.dontTakeDamage)
            {

                #region target/face player

                NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)

                if (NPC.velocity.X == 0f && !throwing)
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

                if (NPC.direction == 0) // what does it mean if direction is 0? Undefined?
                    NPC.direction = 1; // flee right if direction not set? or is initial direction?

                #endregion

                #region melee movement

                if (!throwing)
                {
                    if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
                    {
                        NPC.velocity *= (1f - braking_power); //breaking
                    }
                    if (NPC.velocity.X > 8f) //hard limit of 8f
                    {
                        NPC.velocity.X = 8f;
                    }
                    if (NPC.velocity.X < -8f) //both directions
                    {
                        NPC.velocity.X = -8f;
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
                    NPC.knockBackResist = 0.3f; //If not moving at high speed, default back to taking some knockback
                }

                NPC.noTileCollide = false;

                int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                if (Main.tile[(int)NPC.position.X / 16, y_below_feet].TileType == TileID.Platforms && Main.tile[(int)(NPC.position.X + (float)NPC.width) / 16, y_below_feet].TileType == TileID.Platforms && NPC.position.Y < (player.position.Y - 4 * 16))
                {
                    NPC.noTileCollide = true;
                }

                if ((Math.Abs(NPC.Center.Y - player.Center.Y) > 50 || Math.Abs(player.Center.Y - NPC.Center.Y) > 100) && Math.Abs(player.velocity.Y) < 4 && Math.Abs(player.velocity.X) < 3)
                {
                    NPC.ai[3] += 8; //this is to speed up his teleport if youre unreachable.
                }

                if (NPC.ai[3] > 3000)
                {
                    NPC.velocity.Y += 4;
                    NPC.velocity.X = 0;

                    NPC.ai[1] = 240;
                    NPC.ai[3] = 0;

                    for (int i = 0; i < 100; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 89, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 50, default(Color), 1f)];
                        dust2.noGravity = true;
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item81 with { PitchVariance = 0.3f }, NPC.Center);
                    if (Framing.GetTileSafely((int)player.position.X / 16, ((int)player.position.Y - 5 * 16) / 16).HasTile && Main.tileSolid[Framing.GetTileSafely((int)player.position.X / 16, ((int)player.position.Y - 5 * 16) / 16).TileType])
                    {
                        if (player.direction == 1) { NPC.position = player.position + new Vector2(-4 * 16, 0); }
                        else { NPC.position = player.position + new Vector2(5 * 16, 0); }
                    }
                    else
                    {
                        NPC.position = player.position + new Vector2(0, -5 * 16);
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

                        if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                        {
                            standing_on_solid_tile = true;
                            break; // one is enough so stop checking
                        }
                    } // END traverse blocks under feet
                } // END no jump/fall
                #endregion

                #region new Tile()s, jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
                    int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet
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

                #region attacks


                ++NPC.ai[1]; //Used for both Basic Slash and Dashes


                //Basic Slash Attack

                if (NPC.ai[1] >= 30 && NPC.Distance(player.Center) < 75 && !throwing) //If 30 ticks or more have passed, and player is within slash range
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.3f }, player.Center); //Play Death Sickle sound
                    Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and npc center
                    Vector2 spawnPosition = new Vector2(34, 0).RotatedBy(difference.ToRotation()); //34 is the distance we will spawn the projectile away from npc.Center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (NPC.downedBoss1 || NPC.downedBoss2) //Does more damage post EoC/EoW/BoC, otherewise he's a joke
                        {
                            if (Math.Abs(NPC.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 10, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }

                            else //If moving at extreme speeds, use this higher damage projectile
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 12, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }
                        }

                        else
                        {
                            if (Math.Abs(NPC.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 8, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }

                            else //If moving at extreme speeds, use this higher damage projectile
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 10, 0f, Main.myPlayer, 0, NPC.whoAmI);
                            }
                        }
                    }
                    NPC.ai[1] = 0; //Reset timer
                }

                //Short Dash from medium range

                if (NPC.ai[1] > 120 && Math.Abs(NPC.Center.X - player.Center.X) > 20 * 16 && NPC.velocity.Y == 0 && !throwing)
                {
                    jump = true;
                    NPC.ai[1] = 30;
                }

                if (jump)
                {
                    ++NPC.ai[2];

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

                    if (NPC.ai[2] > 20 && standing_on_solid_tile)
                    {
                        NPC.velocity.Y = -8f;
                        jump = false;
                        NPC.ai[2] = 0;
                    }
                }



                //Firebomb throw

                if (lifePercentage < 80)
                {
                    NPC.ai[3] += Main.rand.Next(1, 4);

                    if (lifePercentage < 50) { NPC.ai[3]++; } //Add an additional 1 to counter if low hp
                    if (lifePercentage < 30) { NPC.ai[3]++; } //Add an additional 1 to counter if very low hp


                    if (NPC.ai[3] > 1000 && NPC.ai[2] == 0)
                    {
                        if (Main.rand.NextBool(15))
                        {
                            int dust = Dust.NewDust(new Vector2(NPC.position.X + NPC.width / 2, NPC.position.Y + 14), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 2f);
                            Main.dust[dust].noGravity = true;
                        }
                    }

                    if (NPC.ai[3] > 1200 && !jump && standing_on_solid_tile && NPC.velocity.Y == 0 && Math.Abs(NPC.Center.X - player.Center.X) < 15 * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                    {
                        throwing = true;
                    }

                    if (throwing && standing_on_solid_tile && !jump && NPC.velocity.Y == 0)
                    {
                        if (NPC.velocity.X > 0.2)
                        {
                            NPC.velocity.X -= 0.2f;
                        }

                        if (NPC.velocity.X < -0.2)
                        {
                            NPC.velocity.X += 0.2f;
                        }


                        if (Math.Abs(NPC.velocity.X) < 0.21)
                        {
                            NPC.ai[2]++;
                            NPC.velocity.X = 0;

                            if (NPC.ai[2] == 45)
                            {
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = .8f, PitchVariance = 0.3f }, player.Center); //Play swing-throw sound

                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    if (Math.Abs(NPC.Center.X - player.Center.X) > 14 * 16) //If player is far
                                    {
                                        if (NPC.direction == 1)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(9, Main.rand.Next(-4, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }

                                        else
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(-9, Main.rand.Next(-4, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }
                                    }

                                    else if (Math.Abs(NPC.Center.X - player.Center.X) > 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) <= 14 * 16) //If player is medium distance
                                    {
                                        if (NPC.direction == 1)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(7, Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }

                                        else
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(-7, Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }
                                    }

                                    else //If player is close
                                    {
                                        if (NPC.direction == 1)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(5, Main.rand.Next(-2, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }

                                        else
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), new Vector2(-5, Main.rand.Next(-2, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                        }
                                    }
                                }
                            }

                            if (NPC.ai[2] == 60)
                            {
                                NPC.ai[2] = 0;
                                NPC.ai[3] = 0;
                                throwing = false;
                            }
                        }
                    }
                }

                #endregion

            }

            #region Aftermath
            //Main.NewText(tsorcRevampWorld.NewSlain[ModContent.NPCType<NPCs.Special.LeonhardPhase1>()]);
            if (NPC.dontTakeDamage) //This meakes health bar dissapear, and no damage can be taken
            {
                NPC.friendly = true; //This makes npc not deal contact damage
                NPC.spriteDirection = NPC.direction; //This makes sprite direction match npc direction
                NPC.TargetClosest(true); //This makes npc target closest player
                throwing = false;
                jump = false;

                if (NPC.velocity.X > 0) //If moving right
                {
                    NPC.direction = 1; //Face right
                }

                else //If moving left
                {
                    NPC.direction = -1; //Face left
                }

                NPC.ai[1]++; //Add 1 to timer

                if (NPC.ai[1] <= 450) //If timer is 420 or less
                {
                    NPC.velocity.X = 0; //Don't move left or right
                    NPC.FaceTarget(); //Face targeted player
                }

                if (NPC.ai[1] == 120 && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Faraam>()))) //When timer is 120
                {
                    //Main.NewText("[c/CCCCCC:???:] Very good, a worthy opponent. Take these, for your trouble", 109, 145, 138); //Send message to chat
                    for (int i = 0; i < Main.CurrentFrameFlags.ActivePlayersCount; i++)
                    {
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulCoin>(), 25);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1);
                        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.StaminaVessel>(), 1);
                    }
                }

                if (NPC.ai[1] == 300) //When timer is 270
                {
                    //Main.NewText("[c/CCCCCC:???:] My name is Leonhard... We shall meet again", 109, 145, 138); //Send next message to chat
                    NPC.frameCounter = 0;
                }

                if (NPC.ai[1] > 450)
                {
                    if (Main.player[NPC.target].position.X > NPC.position.X && NPC.velocity.X > -0.2f)
                    {
                        NPC.velocity.X -= 0.01f;
                    }

                    if (Main.player[NPC.target].position.X < NPC.position.X && NPC.velocity.X < 0.2f)
                    {
                        NPC.velocity.X += 0.01f;
                    }

                    NPC.alpha += 2;
                    if (NPC.alpha > 250)
                    {
                        NPC.active = false;
                        tsorcRevampWorld.NewSlain[new NPCDefinition(ModContent.NPCType<NPCs.Special.LeonhardPhase1>())] = 1;
                    }
                }
            }
            #endregion

        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ProjectileID.Boulder) //Rewarding those who are sneaky enough to use the boulder in the cave to hurt him
            {
                modifiers.FinalDamage *= 5; //340 damage +/-
            }

            if (projectile.minion)
            {
                modifiers.Knockback *= 0; //to prevent slime staff from stunlocking him
            }
        }

        #endregion


        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor)
        {
            if (NPC.ai[3] > 2800)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 89, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 50, default(Color), 1f)];
                    dust2.noGravity = true;
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor) //PreDraw for trails
        {

            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if (NPC.velocity.X > 8f || NPC.velocity.X < -8f)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, NPC.gfxOffY); //Where to draw trails
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(NPC.position.X + 26, NPC.position.Y + 12), NPC.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                }
            }
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.ai[3] > 1000 && NPC.life < NPC.lifeMax * .8f)
            {
                Texture2D firebombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Special/Leonhard_Firebomb");
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

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && !NPC.dontTakeDamage) //If not moving at all (aka firebombing)
            {
                NPC.frame.Y = 6 * frameHeight;
            }

            if (throwing) //throwing anim
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[2] < 45 && NPC.velocity.X == 0)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                if (NPC.ai[2] >= 45 && NPC.ai[2] < 51)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                if (NPC.ai[2] >= 51)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && NPC.dontTakeDamage) //If not moving at all, once defeated
            {
                NPC.frame.Y = 18 * frameHeight;
            }

            if (NPC.velocity.X != 0 && NPC.dontTakeDamage) //if moving once defeated
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.5f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 19 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 20 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 21 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 22 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 23 * frameHeight;
                }
                else if (NPC.frameCounter < 72)
                {
                    NPC.frame.Y = 24 * frameHeight;
                }
                else if (NPC.frameCounter < 84)
                {
                    NPC.frame.Y = 25 * frameHeight;
                }
                else if (NPC.frameCounter < 96)
                {
                    NPC.frame.Y = 26 * frameHeight;
                }
            }
        }

        #endregion

    }
}