using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Special
{
    public class LeonhardPhase1 : ModNPC
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        NPCDespawnHandler despawnHandler;
        public int ThisNPC => ModContent.NPCType<NPCs.Special.LeonhardPhase1>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("???");
            Main.npcFrameCount[npc.type] = 27;
            NPCID.Sets.TrailCacheLength[npc.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[npc.type] = 0;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 200;
            npc.knockBackResist = 0.3f;
            npc.aiStyle = -1;
            npc.height = 40;
            npc.width = 20;
            if (NPC.downedBoss1 || NPC.downedBoss2) { npc.damage = 18; }
            else { npc.damage = 12; } //Low contact damage, the slashes will be doing the damage
            if (NPC.downedBoss1 || NPC.downedBoss2) { npc.lifeMax = 1250; }
            else { npc.lifeMax = 750; }
            npc.defense = 8;
            npc.value = 15000;
            npc.HitSound = SoundID.NPCHit48;
            npc.DeathSound = SoundID.NPCDeath58;
            npc.dontTakeDamageFromHostiles = true;
            npc.lavaImmune = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Frostburn] = true;
            despawnHandler = new NPCDespawnHandler(null, Color.Teal, 54);

        }


        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++) //Blood splatter from being hit
            {
                int dustType = 5;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0 && !npc.dontTakeDamage) //Start aftermath
            {
                npc.life = 1;
                npc.dontTakeDamage = true;
                npc.ai[1] = 0;
                Main.PlaySound(SoundID.Shatter, npc.Center); 
                int leftOrRightVel = Main.rand.Next(-1, 1);
                if (leftOrRightVel == 0) { leftOrRightVel = 1; }

                if (npc.direction == 1)
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(npc.TopRight + new Vector2(-6, 8), 8, 8, 89, 0, 0, 50, default(Color), 0.8f)];
                        dust2.noGravity = true;
                    }

                    Projectile.NewProjectile(npc.TopRight + new Vector2(-6, 8), new Vector2(leftOrRightVel, Main.rand.Next(-3, -2)), ModContent.ProjectileType<Projectiles.ShatteredMoonlight>(), 8, 0, 0, 2);
                }
                else
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        Dust dust2 = Main.dust[Dust.NewDust(npc.position + new Vector2(6, 8), 8, 8, 89, 0, 0, 50, default(Color), 0.8f)];
                        dust2.noGravity = true;
                    }

                    Projectile.NewProjectile(npc.position + new Vector2(6, 8), new Vector2(leftOrRightVel, Main.rand.Next(-3, -2)), ModContent.ProjectileType<Projectiles.ShatteredMoonlight>(), 8, 0, 0, 2);
                }
            }
        }


        #region AI

        bool jump = false;
        bool throwing = false;

        public override void AI()
        {
            Player player = Main.player[npc.target];
            despawnHandler.TargetAndDespawn(npc.whoAmI);

            if (npc.Distance(player.Center) < 1000)
            {
                player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;
                //Main.NewText(player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff);
            }

            int lifePercentage = (npc.life * 100) / npc.lifeMax;
            float acceleration = 0.04f;
            //float top_speed = (lifePercentage * 0.02f) + .2f; //good calculation to remember for decreasing speed the lower the enemy HP%
            float top_speed = (lifePercentage * -0.02f) + 4f; //good calculation to remember for increasing speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed

            if (!npc.dontTakeDamage)
            {

                #region target/face player

                npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)

                if (npc.velocity.X == 0f && !throwing)
                {
                    if (npc.velocity.Y == 0f)
                    { // not moving
                        if (npc.ai[0] == 0f)
                            npc.ai[0] = 1f; // facing change delay
                        else
                        { // change movement and facing direction, reset delay
                            npc.direction *= -1;
                            npc.spriteDirection = npc.direction;
                            npc.ai[0] = 0f;
                        }
                    }
                }
                else // moving in x direction,
                    npc.ai[0] = 0f; // reset facing change delay

                if (npc.direction == 0) // what does it mean if direction is 0? Undefined?
                    npc.direction = 1; // flee right if direction not set? or is initial direction?

                #endregion

                #region melee movement

                if (!throwing)
                {
                    if (Math.Abs(npc.velocity.X) > top_speed && npc.velocity.Y == 0)
                    {
                        npc.velocity *= (1f - braking_power); //breaking
                    }
                    if (npc.velocity.X > 8f) //hard limit of 8f
                    {
                        npc.velocity.X = 8f;
                    }
                    if (npc.velocity.X < -8f) //both directions
                    {
                        npc.velocity.X = -8f;
                    }
                    else
                    {
                        npc.velocity.X += npc.direction * acceleration; //accelerating
                    }

                    //breaking power after turning, to turn fast or to "slip"
                    if (npc.direction == 1)
                    {
                        if (npc.velocity.X > -top_speed)
                        {
                            npc.velocity.X += 0.085f;
                        }
                        npc.netUpdate = true;
                    }
                    if (npc.direction == -1)
                    {
                        if (npc.velocity.X < top_speed)
                        {
                            npc.velocity.X += -0.085f;
                        }
                        npc.netUpdate = true;
                    }
                }

                if (Math.Abs(npc.velocity.X) > 4f) //If moving at high speed, become knockback immune
                {
                    npc.knockBackResist = 0;
                }
                if (Math.Abs(npc.velocity.Y) > 0.1f) //If moving vertically, become knockback immune
                {
                    npc.knockBackResist = 0;
                }
                else
                {
                    npc.knockBackResist = 0.3f; //If not moving at high speed, default back to taking some knockback
                }

                npc.noTileCollide = false;

                int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
                if (Main.tile[(int)npc.position.X / 16, y_below_feet].type == TileID.Platforms && Main.tile[(int)(npc.position.X + (float)npc.width) / 16, y_below_feet].type == TileID.Platforms && npc.position.Y < (player.position.Y - 4 * 16))
                {
                    npc.noTileCollide = true;
                }

                #endregion

                #region check if standing on a solid tile
                bool standing_on_solid_tile = false;
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

                #region new Tile()s, jumping
                if (standing_on_solid_tile)  //  if standing on solid tile
                {
                    int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
                    int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
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

                #region attacks


                ++npc.ai[1]; //Used for both Basic Slash and Dashes


                //Basic Slash Attack

                if (npc.ai[1] >= 30 && npc.Distance(player.Center) < 75 && !throwing) //If 30 ticks or more have passed, and player is within slash range
                {
                    Main.PlaySound(SoundID.Item71.WithVolume(.8f).WithPitchVariance(.3f), npc.position); //Play Death Sickle sound
                    Vector2 difference = Main.player[npc.target].Center - npc.Center; //Distance between player center and npc center
                    Vector2 spawnPosition = new Vector2(34, 0).RotatedBy(difference.ToRotation()); //34 is the distance we will spawn the projectile away from npc.Center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction

                    if (NPC.downedBoss1 || NPC.downedBoss2) //Does more damage post EoC/EoW/BoC, otherewise he's a joke
                    {
                        if (Math.Abs(npc.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                        {
                            Projectile.NewProjectile(npc.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 10, 0f, Main.myPlayer, 0, npc.whoAmI);
                        }

                        else //If moving at extreme speeds, use this higher damage projectile
                        {
                            Projectile.NewProjectile(npc.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 12, 0f, Main.myPlayer, 0, npc.whoAmI);
                        }
                    }

                    else
                    {
                        if (Math.Abs(npc.velocity.X) < 4.5f) //If not moving at extreme speed, use this proj
                        {
                            Projectile.NewProjectile(npc.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 8, 0f, Main.myPlayer, 0, npc.whoAmI);
                        }

                        else //If moving at extreme speeds, use this higher damage projectile
                        {
                            Projectile.NewProjectile(npc.Center + spawnPosition, velocity, ModContent.ProjectileType<Projectiles.Enemy.GreySlash>(), 10, 0f, Main.myPlayer, 0, npc.whoAmI);
                        }
                    }
                    npc.ai[1] = 0; //Reset timer
                }

                //Short Dash from medium range

                if (npc.ai[1] > 120 && Math.Abs(npc.Center.X - player.Center.X) > 20 * 16 && npc.velocity.Y == 0 && !throwing)
                {
                    jump = true;
                    npc.ai[1] = 30;
                }

                if (jump)
                {
                    ++npc.ai[2];

                    if (npc.ai[2] != 0 && npc.ai[2] < 20)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X += 0.75f;
                        }

                        else
                        {
                            npc.velocity.X += -0.75f;
                        }
                    }

                    if (npc.ai[2] > 20 && standing_on_solid_tile)
                    {
                        npc.velocity.Y = -8f;
                        jump = false;
                        npc.ai[2] = 0;
                    }
                }



                //Firebomb throw

                if (lifePercentage < 80)
                {
                    npc.ai[3] += Main.rand.Next(1, 4);

                    if (lifePercentage < 50) { npc.ai[3]++; } //Add an additional 1 to counter if low hp
                    if (lifePercentage < 30) { npc.ai[3]++; } //Add an additional 1 to counter if very low hp

                    if (npc.ai[3] > 1000 && npc.ai[2] == 0)
                    {
                        if (Main.rand.Next(15) == 0)
                        {
                            int dust = Dust.NewDust(new Vector2(npc.position.X + npc.width / 2, npc.position.Y + 14), 12, 12, 6, npc.velocity.X * 0f, npc.velocity.Y * 0f, 30, default(Color), 2f);
                            Main.dust[dust].noGravity = true;
                        }
                    }

                    if (npc.ai[3] > 1200 && !jump && npc.velocity.Y == 0 && Math.Abs(npc.Center.X - player.Center.X) < 15 * 16)
                    {
                        throwing = true;
                    }

                    if (throwing)
                    {
                        if (npc.velocity.X > 0.2)
                        {
                            npc.velocity.X -= 0.2f;
                        }

                        if (npc.velocity.X < -0.2)
                        {
                            npc.velocity.X += 0.2f;
                        }

                        if (Math.Abs(npc.velocity.X) < 0.21)
                        {
                            npc.ai[2]++;
                            npc.velocity.X = 0;

                            if (npc.ai[2] == 45)
                            {
                                Main.PlaySound(SoundID.Item1.WithVolume(.8f).WithPitchVariance(.3f), npc.position); //Play swing-throw sound

                                if (Math.Abs(npc.Center.X - player.Center.X) > 14 * 16) //If player is far
                                {
                                    if (npc.direction == 1)
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(9, Main.rand.Next(-4, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }

                                    else
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(-9, Main.rand.Next(-4, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }
                                }

                                else if (Math.Abs(npc.Center.X - player.Center.X) > 8 * 16 && Math.Abs(npc.Center.X - player.Center.X) <= 14 * 16) //If player is medium distance
                                {
                                    if (npc.direction == 1)
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(7, Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }

                                    else
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(-7, Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }
                                }

                                else //If player is close
                                {
                                    if (npc.direction == 1)
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(5, Main.rand.Next(-2, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }

                                    else
                                    {
                                        Projectile.NewProjectile(npc.Center + new Vector2(0, -14), new Vector2(-5, Main.rand.Next(-2, -1)), ModContent.ProjectileType<Projectiles.Enemy.EnemyFirebomb>(), 20, 0);
                                    }
                                }

                            }

                            if (npc.ai[2] == 60)
                            {
                                npc.ai[2] = 0;
                                npc.ai[3] = 0;
                                throwing = false;
                            }
                        }
                    }
                }

                #endregion

            }

            #region Aftermath
            //Main.NewText(tsorcRevampWorld.Slain[ModContent.NPCType<NPCs.Special.LeonhardPhase1>()]);
            if (npc.dontTakeDamage) //This meakes health bar dissapear, and no damage can be taken
            {
                npc.friendly = true; //This makes npc not deal contact damage
                npc.spriteDirection = npc.direction; //This makes sprite direction match npc direction
                npc.TargetClosest(true); //This makes npc target closest player
                throwing = false;
                jump = false;

                if (npc.velocity.X > 0) //If moving right
                {
                    npc.direction = 1; //Face right
                }

                else //If moving left
                {
                    npc.direction = -1; //Face left
                }

                npc.ai[1]++; //Add 1 to timer

                if (npc.ai[1] <= 450) //If timer is 420 or less
                {
                    npc.velocity.X = 0; //Don't move left or right
                    npc.FaceTarget(); //Face targeted player
                }

                if (npc.ai[1] == 120) //When timer is 120
                {
                    //Main.NewText("[c/CCCCCC:???:] Very good, a worthy opponent. Take these, for your trouble", 109, 145, 138); //Send message to chat
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 25);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1);
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.StaminaVessel>(), 1);

                    /*var Slain = tsorcRevampWorld.Slain;
                    int playerQuantity = 0;

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player thisPlayer = Main.player[i];
                        if (thisPlayer != null && thisPlayer.active)
                        {
                            playerQuantity++;
                        }
                    }

                    if (Slain.ContainsKey(ThisNPC))
                    {
                        if (Slain[ThisNPC] == 0)
                        {
                            player.QuickSpawnItem(ModContent.ItemType<Items.StaminaVessel>(), playerQuantity);
                            Slain[ThisNPC] = 1;
                        }
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
                    }*/

                }

                if (npc.ai[1] == 300) //When timer is 270
                {
                    //Main.NewText("[c/CCCCCC:???:] My name is Leonhard... We shall meet again", 109, 145, 138); //Send next message to chat
                    npc.frameCounter = 0;
                }

                if (npc.ai[1] > 450)
                {
                    if (Main.player[npc.target].position.X > npc.position.X && npc.velocity.X > -0.2f)
                    {
                        npc.velocity.X -= 0.01f;
                    }

                    if (Main.player[npc.target].position.X < npc.position.X && npc.velocity.X < 0.2f)
                    {
                        npc.velocity.X += 0.01f;
                    }

                    npc.alpha += 2;
                    if (npc.alpha > 250)
                    {
                        npc.active = false;
                        tsorcRevampWorld.Slain[ModContent.NPCType<NPCs.Special.LeonhardPhase1>()] = 1;
                    }
                }
            }
            #endregion

        }

        #endregion


        #region Drawing and Animation


        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) //PreDraw for trails
        {

            Vector2 drawOrigin = new Vector2(npc.position.X, npc.position.Y);
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if (npc.velocity.X > 8f || npc.velocity.X < -8f)
            {
                for (int k = 0; k < npc.oldPos.Length; k++)
                {
                    Vector2 drawPos = npc.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, npc.gfxOffY); //Where to draw trails
                    Color color = npc.GetAlpha(lightColor) * ((float)(npc.oldPos.Length - k) / (float)npc.oldPos.Length);
                    spriteBatch.Draw(Main.npcTexture[npc.type], drawPos, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), color, npc.rotation, new Vector2(npc.position.X + 26, npc.position.Y + 12), npc.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                }
            }
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (npc.ai[3] > 1000)
            {
                Texture2D firebombTexture = mod.GetTexture("NPCs/Special/Leonhard_Firebomb");
                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (npc.spriteDirection == -1)
                {
                    spriteBatch.Draw(firebombTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), drawColor, npc.rotation, new Vector2(37, 32), npc.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(firebombTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), drawColor, npc.rotation, new Vector2(37, 32), npc.scale, effects, 0);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (npc.velocity.X != 0 && !npc.dontTakeDamage) //if moving
            {
                float framecountspeed = Math.Abs(npc.velocity.X) * 2.5f;
                npc.frameCounter += framecountspeed;
                npc.spriteDirection = npc.direction;

                if (npc.frameCounter < 12)
                {
                    npc.frame.Y = 1 * frameHeight;
                }
                else if (npc.frameCounter < 24)
                {
                    npc.frame.Y = 2 * frameHeight;
                }
                else if (npc.frameCounter < 36)
                {
                    npc.frame.Y = 3 * frameHeight;
                }
                else if (npc.frameCounter < 48)
                {
                    npc.frame.Y = 4 * frameHeight;
                }
                else if (npc.frameCounter < 60)
                {
                    npc.frame.Y = 5 * frameHeight;
                }
                else if (npc.frameCounter < 72)
                {
                    npc.frame.Y = 6 * frameHeight;
                }
                else if (npc.frameCounter < 84)
                {
                    npc.frame.Y = 7 * frameHeight;
                }
                else if (npc.frameCounter < 96)
                {
                    npc.frame.Y = 8 * frameHeight;
                }
                else if (npc.frameCounter < 108)
                {
                    npc.frame.Y = 9 * frameHeight;
                }
                else if (npc.frameCounter < 120)
                {
                    npc.frame.Y = 10 * frameHeight;
                }
                else if (npc.frameCounter < 132)
                {
                    npc.frame.Y = 11 * frameHeight;
                }
                else if (npc.frameCounter < 144)
                {
                    npc.frame.Y = 12 * frameHeight;
                }
                else if (npc.frameCounter < 156)
                {
                    npc.frame.Y = 13 * frameHeight;
                }
                else if (npc.frameCounter < 168)
                {
                    npc.frame.Y = 14 * frameHeight;
                }
                else
                {
                    npc.frameCounter = 0;
                }
            }

            if (npc.velocity.Y != 0) //If falling/jumping
            {
                npc.frame.Y = 0 * frameHeight;
            }

            if (npc.velocity.X == 0 && npc.velocity.Y == 0 && !npc.dontTakeDamage) //If not moving at all (aka firebombing)
            {
                npc.frame.Y = 6 * frameHeight;
            }

            if (throwing) //throwing anim
            {
                npc.spriteDirection = npc.direction;

                if (npc.ai[2] < 45 && npc.velocity.X == 0)
                {
                    npc.frame.Y = 15 * frameHeight;
                }
                if (npc.ai[2] >= 45 && npc.ai[2] < 51)
                {
                    npc.frame.Y = 16 * frameHeight;
                }
                if (npc.ai[2] >= 51)
                {
                    npc.frame.Y = 17 * frameHeight;
                }
            }

            if (npc.velocity.X == 0 && npc.velocity.Y == 0 && npc.dontTakeDamage) //If not moving at all, once defeated
            {
                npc.frame.Y = 18 * frameHeight;
            }

            if (npc.velocity.X != 0 && npc.dontTakeDamage) //if moving once defeated
            {
                float framecountspeed = Math.Abs(npc.velocity.X) * 2.5f;
                npc.frameCounter += framecountspeed;
                npc.spriteDirection = npc.direction;

                if (npc.frameCounter < 12)
                {
                    npc.frame.Y = 19 * frameHeight;
                }
                else if (npc.frameCounter < 24)
                {
                    npc.frame.Y = 20 * frameHeight;
                }
                else if (npc.frameCounter < 36)
                {
                    npc.frame.Y = 21 * frameHeight;
                }
                else if (npc.frameCounter < 48)
                {
                    npc.frame.Y = 22 * frameHeight;
                }
                else if (npc.frameCounter < 60)
                {
                    npc.frame.Y = 23 * frameHeight;
                }
                else if (npc.frameCounter < 72)
                {
                    npc.frame.Y = 24 * frameHeight;
                }
                else if (npc.frameCounter < 84)
                {
                    npc.frame.Y = 25 * frameHeight;
                }
                else if (npc.frameCounter < 96)
                {
                    npc.frame.Y = 26 * frameHeight;
                }
            }
        }

        #endregion

    }
}