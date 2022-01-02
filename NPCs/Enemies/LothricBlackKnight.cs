using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    public class LothricBlackKnight : ModNPC //Don't look at the code, it's muy malo. Look at Lothric Spear Knight for a better example code management-wise
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/LothricKnight"; // Here we're grabbing the original texture used by Lothric Knight, to save us needing another spritesheet taking up space (albeit very little)

        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        //AI 
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;
        bool stabbing = false;
        bool enrage = false;
        bool hasEnraged = false;
        int enrageTimer;

        //Anim
        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[npc.type] = 18;
            NPCID.Sets.TrailCacheLength[npc.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[npc.type] = 0;
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 20;
            npc.knockBackResist = 0.15f;
            npc.aiStyle = -1;
            npc.damage = 65;
            npc.defense = 15;
            npc.height = 44;
            npc.width = 20;
            npc.lifeMax = 1000;
            if (Main.hardMode) { npc.lifeMax = 1500; npc.defense = 30; npc.value = 10200; }
            if (tsorcRevampWorld.SuperHardMode) { npc.lifeMax = 3000; npc.defense = 55; npc.damage = 90; npc.value = 16000; }
            npc.value = 7200;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.lavaImmune = true;
            npc.buffImmune[ModContent.BuffType<Buffs.ToxicCatDrain>()] = true;
            npc.buffImmune[ModContent.BuffType<Buffs.ViruCatDrain>()] = true;
            npc.buffImmune[ModContent.BuffType<Buffs.BiohazardDrain>()] = true;

            /*banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.DunlendingBanner>();*/
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

        public override void AI()
        {

            Player player = Main.player[npc.target];

            if (npc.Distance(player.Center) < 600)
            {
                player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 2);
            }

            var projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>();
            var projStab = ModContent.ProjectileType<Projectiles.Enemy.Spearhead>();
            int lifePercentage = (npc.life * 100) / npc.lifeMax;
            float acceleration = 0.02f;
            //float top_speed = (lifePercentage * 0.02f) + .2f; //good calculation to remember for decreasing speed the lower the enemy HP%
            float top_speed = (lifePercentage * -0.02f) + 2.5f; //good calculation to remember for increasing speed the lower the enemy HP%
            float braking_power = 0.1f; //Breaking power to slow down after moving above top_speed
                                        //Main.NewText(Math.Abs(npc.velocity.X));

            int damage = npc.damage / 4;

            if (npc.life < npc.lifeMax / 2)
            {
                top_speed *= 1.5f;
                damage = (int)(1.1f * damage);
                projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlashCrimson>();
                projStab = ModContent.ProjectileType<Projectiles.Enemy.SpearheadCrimson>();

                if (!hasEnraged)
                {
                    enrage = true;
                }
            }



            if (enrage)
            {
                enrageTimer++;

                if (enrageTimer <= 120)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        int dust = Dust.NewDust(new Vector2(npc.position.X - 10, npc.position.Y - 15), npc.width + 20, npc.height + 20, 60, 0, 0, 30, default(Color), Main.rand.NextFloat(1f, 1.5f));
                        Main.dust[dust].noGravity = true;
                    }
                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(npc.position.X - 10, npc.position.Y - 15), npc.width + 20, npc.height + 20, 60, 0, 0, 30, default(Color), Main.rand.NextFloat(.8f, 1.2f));
                    Main.dust[dust].noGravity = true;
                }

                if (enrageTimer > 180)
                {

                    hasEnraged = true;
                    enrage = false;
                }
            }


            #region target/face player, respond to boredom

            //keeping this here just in case
            /*if (!jumpSlashing && !slashing && !stabbing)
            {
                npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
            }

            if (npc.velocity.X == 0f && !jumpSlashing && !shielding && !slashing && !stabbing)
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

            if (npc.direction == 0) // what does it mean if direction is 0?
                npc.direction = 1; // flee right if direction not set? or is initial direction?*/

            if (npc.ai[0] == 0 && !jumpSlashing && !slashing && !stabbing)
            {
                npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
            }
            if (npc.velocity.X == 0 && !jumpSlashing && !shielding && !slashing && !stabbing)
            {
                npc.ai[0]++;
                if (npc.ai[0] > 120 && npc.velocity.Y == 0)
                {
                    npc.direction *= -1;
                    npc.spriteDirection = npc.direction;
                    npc.ai[0] = 50;
                }
            }

            if (Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
            {
                npc.ai[0] = 0;
            }

            #endregion

            #region melee movement

            if (npc.ai[1] >= 390 && npc.ai[1] <= 420)
            {
                top_speed = (lifePercentage * -0.015f) + 2.5f; //good calculation to remember for increasing speed the lower the enemy HP%
            }

            if (Math.Abs(npc.velocity.X) > top_speed && npc.velocity.Y == 0)
            {
                npc.velocity *= (1f - braking_power); //breaking
            }
            if (npc.velocity.X > 10.5f) //hard limit of 10.5f
            {
                npc.velocity.X = 10.5f;
            }
            if (npc.velocity.X < -10.5f) //both directions
            {
                npc.velocity.X = -10.5f;
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


            if (Math.Abs(npc.velocity.X) > 4f) //If moving at high speed, become knockback immune
            {
                npc.knockBackResist = 0;
            }
            if (Math.Abs(npc.velocity.Y) > 0.1f) //If moving vertically, become knockback immune
            {
                npc.knockBackResist = 0;
            }
            if (stabbing || jumpSlashing) //If stabbing or jumpslashing, become kb immune. I like how I made 3 ifs all separate, to do the same thing
            {
                npc.knockBackResist = 0;
            }

            else
            {
                npc.knockBackResist = 0.1f; //If not moving at high speed, default back to taking some knockback
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
            if (standing_on_solid_tile && !slashing && !shielding && !jumpSlashing && !stabbing)  //  if standing on solid tile
            {
                int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
                int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet

                if (npc.position.Y > player.position.Y + 3 * 16 && npc.position.Y < player.position.Y + 8 * 16 && Math.Abs(npc.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    slashing = true;
                    npc.ai[3] = 22;
                    npc.velocity.Y = -8f; // jump with power 8 if directly under player
                    npc.netUpdate = true;
                }

                if (npc.position.Y >= player.position.Y + 8 * 16 && Math.Abs(npc.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    slashing = true;
                    npc.ai[3] = 10;
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

            #region attacks


            //Basic Slash Attack


            //Main.NewText(npc.ai[1]);
            //Main.NewText(npc.ai[2]);
            //Main.NewText(npc.ai[3]);
            //Main.NewText(top_speed);
            //Main.NewText(Math.Abs(npc.velocity.X));

            if (npc.ai[3] < 10)
            {
                ++npc.ai[3]; //Used for Basic Slash
            }

            if (/*!shielding && */!jumpSlashing && !stabbing)
            {
                if (npc.ai[3] == 10 && npc.Distance(player.Center) < 55 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
                {
                    slashing = true;
                    shielding = false;
                }

                if (slashing)
                {
                    ++npc.ai[3];

                    if (npc.ai[3] < 26)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X -= 0.25f;
                            if (npc.velocity.X < 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }

                        else
                        {
                            npc.velocity.X += 0.25f;
                            if (npc.velocity.X > 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }

                    if (npc.ai[3] == 26) //If timer is 46
                    {
                        Main.PlaySound(SoundID.Item1.WithVolume(1f).WithPitchVariance(.3f), npc.position); //Play slash/swing sound

                        if (npc.direction == 1)
                        {
                            if (!standing_on_solid_tile)
                            {
                                Projectile.NewProjectile(npc.Center + new Vector2(20, -66), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, npc.whoAmI, 0);
                            }
                            else
                            {
                                Projectile.NewProjectile(npc.Center + new Vector2(20, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, npc.whoAmI, 0);
                            }

                        }

                        else
                        {
                            if (!standing_on_solid_tile)
                            {
                                Projectile.NewProjectile(npc.Center + new Vector2(-2, -66), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, npc.whoAmI, 0);

                            }
                            else
                            {
                                Projectile.NewProjectile(npc.Center + new Vector2(-2, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, npc.whoAmI, 0);
                            }
                        }
                    }

                    if (npc.ai[3] >= 49) //If timer is 69
                    {
                        slashing = false;
                        npc.ai[3] = 0; //Reset timer
                    }
                }
            }




            //Telegraphed Jump-slash

            if (npc.ai[1] < 420)
            {
                ++npc.ai[1]; //Used for Jump-slash
            }

            if (npc.ai[1] >= 390 && npc.ai[1] <= 400)
            {
                if (npc.direction == 1) //Large eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

            }

            if (npc.ai[1] >= 400 && npc.ai[1] < 442)
            {
                if (npc.direction == 1) //Small eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }
            }

            if (/*!shielding && */!slashing && !stabbing)
            {
                if (npc.ai[1] == 420 && npc.Distance(player.Center) < 150 && npc.Distance(player.Center) >= 55 && npc.velocity.Y == 0 && standing_on_solid_tile && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)) //If timer is at 0 and player is within range
                {
                    jumpSlashing = true;
                    shielding = false;
                }

                if (jumpSlashing)
                {
                    ++npc.ai[1];
                    if (npc.ai[1] < 436)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X -= 0.15f;
                            if (npc.velocity.X < 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }

                        else
                        {
                            npc.velocity.X += 0.15f;
                            if (npc.velocity.X > 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }

                    if (npc.ai[1] == 436) //If timer is 46
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X += 5f;
                            npc.velocity.Y -= 3f;
                        }

                        else
                        {
                            npc.velocity.X -= 5f;
                            npc.velocity.Y -= 3f;
                        }
                    }

                    if (npc.ai[1] == 442) //If timer is 50
                    {
                        Main.PlaySound(SoundID.Item1.WithVolume(1f).WithPitchVariance(.3f), npc.position); //Play slash/swing sound

                        if (npc.direction == 1)
                        {
                            Projectile.NewProjectile(npc.Center + new Vector2(24, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.4f), 5, Main.myPlayer, npc.whoAmI, 0);
                        }

                        else
                        {
                            Projectile.NewProjectile(npc.Center + new Vector2(-8, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.4f), 5, Main.myPlayer, npc.whoAmI, 0);
                        }
                    }
                    if (npc.ai[1] > 470 && npc.ai[1] < 489)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X -= 0.3f;
                            if (npc.velocity.X < 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }

                        else
                        {
                            npc.velocity.X += 0.3f;
                            if (npc.velocity.X > 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }
                    if (npc.ai[1] >= 489) //If timer is 489
                    {
                        jumpSlashing = false;
                        npc.ai[1] = 150; //Reset timer
                    }
                }
            }


            //Dash Stab
            if (/*!shielding &&*/ !slashing && !jumpSlashing)
            {
                if (npc.ai[1] == 420 && npc.Distance(player.Center) < 300 && npc.Distance(player.Center) >= 150 && npc.velocity.Y == 0 && Math.Abs(npc.Center.Y - player.Center.Y) < 6.5f * 16 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)) //If timer is at 0 and player is within range
                {
                    stabbing = true;
                    shielding = false;
                }

                if (stabbing)
                {
                    ++npc.ai[1];

                    if (npc.ai[1] < 436)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X -= 0.15f;
                            if (npc.velocity.X < 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }

                        else
                        {
                            npc.velocity.X += 0.15f;
                            if (npc.velocity.X > 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }

                    if (npc.ai[1] == 436) //If timer is 46
                    {
                        Main.PlaySound(SoundID.Item45.WithVolume(1f).WithPitchVariance(.3f), npc.position); //Play slash/swing sound

                        if (npc.direction == 1)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(44, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, npc.whoAmI, 0)];
                            npc.velocity.X += 10.5f;
                            npc.velocity.Y -= 2f;
                        }

                        else
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(npc.Center + new Vector2(-44, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, npc.whoAmI, 0)];
                            npc.velocity.X -= 10.5f;
                            npc.velocity.Y -= 2f;
                        }
                    }

                    if (npc.ai[1] > 470 && npc.ai[1] < 489)
                    {
                        if (npc.direction == 1)
                        {
                            npc.velocity.X -= 0.3f;
                            if (npc.velocity.X < 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }

                        else
                        {
                            npc.velocity.X += 0.3f;
                            if (npc.velocity.X > 0)
                            {
                                npc.velocity.X = 0;
                            }
                        }
                    }

                    if (npc.ai[1] > 489)
                    {
                        npc.ai[1] = 280;
                        stabbing = false;
                    }

                }
            }


            //Shielding

            if (shielding || npc.Distance(player.Center) < 220 || npc.ai[2] > 300)
            {
                npc.ai[2]++;

                if (!jumpSlashing && !slashing && !stabbing && npc.velocity.Y == 0)
                {
                    if (npc.ai[2] > 300 && npc.ai[2] <= 310)
                    {
                        if (npc.direction == 1) { npc.velocity.X -= 0.15f; }
                        else { npc.velocity.X += 0.15f; }
                    }

                    if (npc.ai[2] > 310)
                    {
                        npc.velocity.X = 0;
                        shielding = true;
                    }

                    if (npc.ai[2] > 500)
                    {
                        shielding = false;
                        npc.ai[2] = 0;
                    }
                }
            }
            #endregion
        }


        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            int shieldPower = npc.defense * 3;

            if (npc.life < npc.lifeMax / 2)
            {
                shieldPower = npc.defense * 4;
            }

            if (shielding)
            {
                if (npc.direction == 1)
                {
                    if (player.position.X > npc.position.X)
                    {
                        Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                        damage -= shieldPower;
                        if (npc.ai[2] > 340)
                        {
                            npc.ai[2] -= 35;
                        }
                    }
                }
                else
                {
                    if (player.position.X < npc.position.X)
                    {
                        Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                        damage -= shieldPower;
                        if (npc.ai[2] > 340)
                        {
                            npc.ai[2] -= 35;
                        }
                    }
                }
            }

            if (npc.direction == 1) //if enemy facing right
            {
                if (player.position.X < npc.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    damage = (int)(damage * 2f); //bonus damage
                    Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > npc.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    damage = (int)(damage * 2f); //bonus damage
                    Main.PlaySound(SoundID.NPCHit18.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play fleshy sound
                }
            }

            npc.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.player[npc.target];

            int shieldPower = npc.defense * 3;

            if (npc.life < npc.lifeMax / 2)
            {
                shieldPower = npc.defense * 4;
            }

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.BlizzardBlasterShot>())
            {
                if (shielding)
                {
                    if (npc.direction == 1) //if npc facing right
                    {
                        if (projectile.Center.X > npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;
                            if (npc.ai[1] < 340)
                            {
                                npc.ai[1] += 70; //Used for Jump-slash
                            }
                            if (npc.ai[2] > 340)
                            {
                                npc.ai[2] -= 35;
                            }
                        }

                        else if (hitDirection == -1 && (!projectile.melee || projectile.aiStyle == 19))
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;

                            if (npc.ai[1] < 340)
                            {
                                npc.ai[1] += 80; //Used for Jump-slash
                            }


                            if (npc.ai[2] > 340)
                            {
                                npc.ai[2] -= 35;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < npc.Center.X && projectile.melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;
                            if (npc.ai[1] < 340)
                            {
                                npc.ai[1] += 70; //Used for Jump-slash
                            }
                            if (npc.ai[2] > 350)
                            {
                                npc.ai[2] -= 35;
                            }
                        }
                        else if (hitDirection == 1 && (!projectile.melee || projectile.aiStyle == 19))
                        {
                            Main.PlaySound(SoundID.NPCHit4.WithVolume(1f).WithPitchVariance(0.3f), npc.position); //Play metal tink sound
                            damage -= shieldPower;

                            knockback = 0f;
                            if (npc.ai[1] < 340)
                            {
                                npc.ai[1] += 80; //Used for Jump-slash
                            }


                            if (npc.ai[2] > 340)
                            {
                                npc.ai[2] -= 35;
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

                if (npc.Distance(player.Center) > 220 && !shielding)
                {
                    npc.ai[2] += 120;
                }

                if (npc.ai[1] < 400)
                {
                    npc.ai[1] += 10;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode && !spawnInfo.player.ZoneJungle) return 0.002f;

            if (Main.expertMode && Main.hardMode && spawnInfo.player.ZoneDungeon) return chance = 0.002f;

            if (NPC.downedBoss3 && !spawnInfo.player.ZoneJungle) return chance = 0.00003f;

            return chance;
        }

        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulShekel>(), 5 + Main.rand.Next(1, 4));
            Item.NewItem(npc.getRect(), ItemID.Heart);
            Item.NewItem(npc.getRect(), ItemID.Heart);


            //if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenKotetsu>(), 1, false, -1);
            //if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.SpikedIronShield>(), 1, false, -1);
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.LostUndeadSoul>());
            if (Main.rand.Next(3) == 0) { Item.NewItem(npc.getRect(), ItemID.RagePotion); }
            if (Main.rand.Next(3) == 0) { Item.NewItem(npc.getRect(), ItemID.WrathPotion); }
        }

        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor) //This allows us to draw dusts or modify the color of the npc. The .NET Framework has it's own colours, there are lots and the easiest way to see them is online or by testing your code
        {

            //If you want an enemy to be black, use DimGray, as Black will make it appear as a black silhouette
            //drawColor = Color.DimGray;

            //However... When you tint an NPC a colour, the tint "glows", and therefore the enemy will glow in the dark. You can blend the colour with 'normal lighting colour' like this:
            drawColor = new Color(drawColor.ToVector3() * Color.DimGray.ToVector3());

            //You can change the blend ratio to make it add more of one colour or the other. In this case we're adding 50% more of the 'normal lighting colour', so it's not as dark.
            //drawColor = new Color((drawColor.ToVector3() * 1.5f) * Color.DimGray.ToVector3());


            /* Useful explanation someone gave me on the above code:

             @ChromaEquinox | Red Cloud Revamp just to explain what that code does
                let's say that we had a colour (R: 255, G: 128, B: 0)
                and the new color is (R: 64, G: 255, B: 255)

                the first color gets converted to a vector whose values are (X: 1, Y: 0.5, Z: 0)
                the second colour gets converted to a vector whose values are (X: 0.25, Y: 1, Z: 1)

                multiplying the two together results in (X: 0.25, Y: 0.5, Z: 0)

                converting it back to a color gives you (R: 64, G: 128, B: 0)
             
             */


            //If you want an enemy to be red, use red/crimson, and multiply the colour by a value between 0 and 1 to give transparency. 0 being totally transparent. Doing it like this makes the sprite "glow".
            //drawColor = Color.Crimson * 0.8f;

        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) //PreDraw allows you to draw things behind/under the NPC, in this case we're using it for trails
        {
            //I don't define a texture, because we'll be using the base texture (see line 909, int the first argument of the draw function we use Main.npcTexture[npc.type])
            Vector2 drawOrigin = new Vector2(npc.position.X, npc.position.Y);
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if ((npc.velocity.X > 5f || npc.velocity.X < -5f) && stabbing)
            {
                for (int k = 0; k < npc.oldPos.Length; k++)
                {
                    Vector2 drawPos = npc.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, npc.gfxOffY); //Where to draw trails
                    Color color = npc.GetAlpha(lightColor) * ((float)(npc.oldPos.Length - k) / (float)npc.oldPos.Length);
                    spriteBatch.Draw(Main.npcTexture[npc.type], drawPos, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), color, npc.rotation, new Vector2(npc.position.X + 26, npc.position.Y + 12), npc.scale, effects, 0f); //Vector2 Origin made 0 sense in this case
                }
            }
            return true; //returning true in PreDraw means "Yes, draw the base texture of the npc"
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor) //PreDraw allows you to draw things in front of the NPC, in this case I'm drawing an animated shield texture while the NPC is shielding
        {
            Texture2D CrimsonEquipment = mod.GetTexture("NPCs/Enemies/LothricKnight_CrimsonEquipment");
            Texture2D shieldTexture = mod.GetTexture("NPCs/Enemies/LothricKnight_Shield"); //In this case we do define another texture, in this case our shield
            Rectangle myrectangle = shieldTexture.Frame(1, 15, 0, shieldFrame);
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (npc.life < npc.lifeMax / 2)
            {
                if (npc.spriteDirection == 1)
                {
                    spriteBatch.Draw(CrimsonEquipment, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), Color.Crimson * 0.8f, npc.rotation, new Vector2(32, 32), npc.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(CrimsonEquipment, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 74, 56), Color.Crimson * 0.8f, npc.rotation, new Vector2(43, 32), npc.scale, effects, 0f);
                }
            }


            if (shielding && npc.velocity.X == 0 && !jumpSlashing && !slashing && !stabbing)
            {
                if (npc.life < npc.lifeMax / 2)
                {
                    if (npc.spriteDirection == 1)
                    {
                        spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, Color.Crimson * 0.8f, npc.rotation, new Vector2(43, 32), npc.scale, effects, 0f);
                    }
                    else
                    {
                        spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, Color.Crimson * 0.8f, npc.rotation, new Vector2(43, 32), npc.scale, effects, 0f);
                    }
                }
                else
                {
                    if (npc.spriteDirection == 1)
                    {
                        spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(43, 32), npc.scale, effects, 0f);
                    }
                    else
                    {
                        spriteBatch.Draw(shieldTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(43, 32), npc.scale, effects, 0f);
                    }
                }
            }


        }

        public override void FindFrame(int frameHeight)
        {
            //Main.NewText(shieldAnimTimer);
            //Main.NewText(shieldFrame);

            if (npc.velocity.X != 0) //Walking
            {
                float framecountspeed = Math.Abs(npc.velocity.X) * 2.2f;
                npc.frameCounter += framecountspeed;
                npc.spriteDirection = npc.direction;

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

            if (npc.velocity.Y != 0 && (!jumpSlashing && !shielding && !stabbing)) //If falling/jumping
            {
                npc.frame.Y = 1 * frameHeight;
            }

            if (slashing) //If slashing
            {
                npc.spriteDirection = npc.direction;

                if (npc.ai[3] < 18)
                {
                    npc.frame.Y = 11 * frameHeight;
                }
                else if (npc.ai[3] < 26)
                {
                    npc.frame.Y = 12 * frameHeight;
                }
                else if (npc.ai[3] < 29)
                {
                    npc.frame.Y = 13 * frameHeight;
                }
                else if (npc.ai[3] < 32)
                {
                    npc.frame.Y = 14 * frameHeight;
                }
                else if (npc.ai[3] < 35)
                {
                    npc.frame.Y = 15 * frameHeight;
                }
                else if (npc.ai[3] < 49)
                {
                    npc.frame.Y = 16 * frameHeight;
                }
            }

            if (jumpSlashing) //If jumpslashing
            {
                npc.spriteDirection = npc.direction;

                if (npc.ai[1] < 428)
                {
                    npc.frame.Y = 11 * frameHeight;
                }
                else if (npc.ai[1] < 436)
                {
                    npc.frame.Y = 12 * frameHeight;
                }
                else if (npc.ai[1] < 439)
                {
                    npc.frame.Y = 13 * frameHeight;
                }
                else if (npc.ai[1] < 442)
                {
                    npc.frame.Y = 14 * frameHeight;
                }
                else if (npc.ai[1] < 445)
                {
                    npc.frame.Y = 15 * frameHeight;
                }
                else if (npc.ai[1] < 489)
                {
                    npc.frame.Y = 16 * frameHeight;
                }
            }

            if (stabbing)
            {
                npc.spriteDirection = npc.direction;

                if (npc.ai[1] < 436)
                {
                    npc.frame.Y = 2 * frameHeight;
                }
                else if (npc.ai[1] < 470)
                {
                    npc.frame.Y = 17 * frameHeight;
                }
                else if (npc.ai[1] < 475)
                {
                    npc.frame.Y = 15 * frameHeight;
                }
                else if (npc.ai[1] < 489)
                {
                    npc.frame.Y = 16 * frameHeight;
                }

            }
            if (npc.velocity.X == 0 && npc.velocity.Y == 0 && shielding && !jumpSlashing && !slashing && !stabbing) //If not moving at all (shielding)
            {
                npc.spriteDirection = npc.direction;
                npc.frame.Y = 10 * frameHeight;
            }

            if (shielding) //this is the shield shine anim
            {
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 14 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 14)
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
