using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class RingedKnight : ModNPC //Do I win the award for messiest code 2022 yet?
    {
        //AI 
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;
        bool stabbing = false;
        bool enrage = false;
        bool hasEnraged = false;
        int enrageTimer;


        //Anim
        //int shieldFrame;
        //int shieldAnimTimer;
        //bool countingUP = false;


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 19;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5; //How many copies of shadow/trail
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            NPC.damage = 32;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 400;
            if (Main.hardMode) { NPC.lifeMax = 1200; NPC.defense = 45; NPC.damage = 140; }
            if (tsorcRevampWorld.SuperHardMode) { NPC.lifeMax = 5000; NPC.defense = 100; NPC.damage = 230; NPC.value = 6000; }
            NPC.value = 1500;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Frostburn] = true;


            /*Banner = npc.type;
            BannerItem = ModContent.ItemType<Banners.DunlendingBanner>();*/
        }


        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * (float)hitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * (float)hitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];

            var projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>();
            var projStab = ModContent.ProjectileType<Projectiles.Enemy.Spearhead>();
            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.01f;
            //float top_speed = (lifePercentage * 0.02f) + .2f; //good calculation to remember for decreasing speed the lower the enemy HP%
            float top_speed = /*(lifePercentage * -0.02f) +*/ 1f; //good calculation to remember for increasing speed the lower the enemy HP%
            float braking_power = 0.15f; //Breaking power to slow down after moving above top_speed
                                         //Main.NewText(Math.Abs(npc.velocity.X));


            int damage = NPC.damage / 4;

            Lighting.AddLight(NPC.Center, .28f, .16f, .04f);

            #region enrage

            if (lifePercentage <= 60)
            {
                top_speed *= 1.4f;
                damage = (int)(1.2f * damage);
                projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlashFire>();
                projStab = ModContent.ProjectileType<Projectiles.Enemy.SpearheadFire>();

                if (!hasEnraged)
                {
                    enrage = true;
                }
            }

            if (enrage)
            {
                enrageTimer++;

                if (enrageTimer <= 30)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2.5f));
                        Main.dust[dust].noGravity = true;
                    }
                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }

                if (enrageTimer > 90)
                {

                    hasEnraged = true;
                    enrage = false;
                }
            }

            if (!stabbing && !slashing && !jumpSlashing && lifePercentage <= 60 && Main.rand.NextBool(4))
            {
                if (NPC.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X + 26, NPC.position.Y - 4), 34, 34, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X - 40, NPC.position.Y - 4), 34, 34, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }
            }

            #endregion

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

            if (NPC.ai[0] == 0 && !jumpSlashing && !slashing && !stabbing)
            {
                NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
            }
            if (NPC.velocity.X == 0 && !jumpSlashing && !shielding && !slashing && !stabbing)
            {
                NPC.ai[0]++;
                if (NPC.ai[0] > 120 && NPC.velocity.Y == 0)
                {
                    NPC.direction *= -1;
                    NPC.spriteDirection = NPC.direction;
                    NPC.ai[0] = 50;
                }
            }

            if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
            {
                NPC.ai[0] = 0;
            }

            #endregion

            #region melee movement

            if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
            {
                NPC.velocity *= (1f - braking_power); //breaking
            }
            if (NPC.velocity.X > 10.5f) //hard limit of 10.5f
            {
                NPC.velocity.X = 10.5f;
            }
            if (NPC.velocity.X < -10.5f) //both directions
            {
                NPC.velocity.X = -10.5f;
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


            if (Math.Abs(NPC.velocity.X) > 4f) //If moving at high speed, become knockback immune
            {
                NPC.knockBackResist = 0;
            }
            if (Math.Abs(NPC.velocity.Y) > 0.1f) //If moving vertically, become knockback immune
            {
                NPC.knockBackResist = 0;
            }
            if (stabbing || jumpSlashing) //If stabbing or jumpslashing, become kb immune. I like how I made 3 ifs all separate, to do the same thing
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
                NPC.noTileCollide = true;
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
            if (standing_on_solid_tile && !slashing && !shielding && !jumpSlashing && !stabbing)  //  if standing on solid tile
            {
                int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
                int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet

                if (NPC.position.Y > player.position.Y + 3 * 16 && NPC.position.Y < player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    NPC.ai[3] = 22;
                    NPC.velocity.Y = -8f; // jump with power 8 if directly under player
                    NPC.netUpdate = true;
                }

                if (NPC.position.Y >= player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    NPC.ai[3] = 10;
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

            #region teleporting

            if (NPC.lavaWet)
            {
                NPC.ai[2] += 4;
            }

            //using the same timer as shielding
            if (NPC.ai[2] >= -120f && NPC.ai[2] < -100f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
            {
                NPC.velocity *= 0f; // stop moving
                NPC.ai[2] = 0f; // reset boredom to 0
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                Vector2 vector = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f); // current location
                float num6 = NPC.oldPos[2].X + (float)NPC.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
                float num7 = NPC.oldPos[2].Y + (float)NPC.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
                float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
                num8 = 2f / num8; // to normalize to 2 unit long vector
                num6 *= num8; // direction to where it was 3 frames ago, vector normalized
                num7 *= num8; // direction to where it was 3 frames ago, vector normalized
                for (int j = 0; j < 20; j++) // make 20 dusts at current position
                {
                    int num9 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, num6, num7, 200, default(Color), 2f);
                    Main.dust[num9].noGravity = true; // floating
                    Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
                    expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
                }
                for (int k = 0; k < 20; k++) // more dust effects at old position
                {
                    int num10 = Dust.NewDust(NPC.oldPos[2], NPC.width, NPC.height, 6, -num6, -num7, 200, default(Color), 2f);
                    Main.dust[num10].noGravity = true;
                    Dust expr_1A6F_cp_0 = Main.dust[num10];
                    expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
                }
            }

            if (Main.netMode != 1 && NPC.ai[2] >= 1000) // is server & bored
            {
                int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                int x_blockpos = (int)NPC.position.X / 16; // corner not center
                int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                int tp_radius = 25; // radius around target(upper left corner) in blocks to teleport into
                int tp_counter = 0;
                bool flag7 = false;
                if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 2000f)
                { // far away from target; 2000 pixels = 125 blocks
                    tp_counter = 100;
                    flag7 = true; // no teleport
                }
                while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
                {
                    if (tp_counter >= 100) // run 100 times
                        break; //return;
                    tp_counter++;

                    int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                    int tp_y_target = Main.rand.Next(target_y_blockpos - tp_radius, target_y_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                    for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                    { // (tp_x_target,m) is block under its feet I think
                        if ((m < target_y_blockpos - 20 || m > target_y_blockpos + 20 || tp_x_target < target_x_blockpos - 20 || tp_x_target > target_x_blockpos + 20) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].HasTile)
                        { // over 6 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                            bool safe_to_stand = true;
                            bool dark_caster = false; // not a fighter type AI...
                            if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                safe_to_stand = false;
                            else if (Main.tile[tp_x_target, m - 1].LiquidType == LiquidID.Lava) // feet submerged in lava
                                safe_to_stand = false;

                            if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].TileType] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                            { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                                NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet
                                NPC.netUpdate = true;
                                NPC.ai[2] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                flag7 = true; // end the loop (after testing every lower point :/)
                            }
                        } // END over 6 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            }
            #endregion

            #region attacks


            //Basic Slash Attack
            //Main.NewText(npc.ai[1]);
            //Main.NewText(npc.ai[2]);
            //Main.NewText(npc.ai[3]);
            // Main.NewText(top_speed);
            //Main.NewText(Math.Abs(npc.velocity.X));

            if (NPC.ai[3] < 10)
            {
                ++NPC.ai[3]; //Used for Basic Slash
            }

            if (/*!shielding && */!jumpSlashing && !stabbing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) <= 55 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    shielding = false;
                }

                if (slashing)
                {
                    ++NPC.ai[3];

                    if (NPC.ai[3] < 26)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.25f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.25f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }

                    if (NPC.ai[3] == 26) //If timer is 46
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f}, NPC.Center); //Play slash/swing sound

                        if (NPC.direction == 1)
                        {
                            if (!standing_on_solid_tile)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -66), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                            else
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }

                        }

                        else
                        {
                            if (!standing_on_solid_tile)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -66), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);

                            }
                            else
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                        }
                    }

                    if (NPC.ai[3] >= 49) //If timer is 69
                    {
                        slashing = false;
                        NPC.ai[3] = 0; //Reset timer
                    }
                }
            }




            //Telegraphed Jump-slash

            if (NPC.ai[1] < 420)
            {
                ++NPC.ai[1]; //Used for Jump-slash
            }

            if (NPC.ai[1] >= 390 && NPC.ai[1] <= 400)
            {
                if (NPC.direction == 1) //Large eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

            }

            if (NPC.ai[1] >= 400 && NPC.ai[1] < 442)
            {
                if (NPC.direction == 1) //Small eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }

            if (/*!shielding && */!slashing /*&& !stabbing*/)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 150 && NPC.Distance(player.Center) >= 55 && NPC.velocity.Y == 0 && standing_on_solid_tile && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0)) //If timer is at 0 and player is within range
                {
                    jumpSlashing = true;
                    shielding = false;
                }

                if (jumpSlashing)
                {
                    ++NPC.ai[1];
                    if (NPC.ai[1] < 436)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.15f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.15f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }

                    if (NPC.ai[1] == 436) //If timer is 46
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X += 5f;
                            NPC.velocity.Y -= 3f;
                        }

                        else
                        {
                            NPC.velocity.X -= 5f;
                            NPC.velocity.Y -= 3f;
                        }
                    }

                    if (NPC.ai[1] == 442) //If timer is 50
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f}, NPC.Center); //Play slash/swing sound

                        if (NPC.direction == 1)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(24, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                        }

                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), projSlash, (int)(damage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                        }
                    }
                    if (NPC.ai[1] > 470 && NPC.ai[1] < 489)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.3f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.3f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }
                    if (NPC.ai[1] >= 489) //If timer is 489
                    {
                        jumpSlashing = false;
                        NPC.ai[1] = 150; //Reset timer
                    }
                }
            }


            //Dash Stab
            //if (/*!shielding && !slashing && !jumpSlashing*/)
            //{
            if (NPC.Distance(player.Center) < 160 && NPC.Distance(player.Center) >= 55 && NPC.velocity.Y == 0 && Math.Abs(NPC.Center.Y - player.Center.Y) < 4.5f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0) && (NPC.ai[3] >= 48 || (NPC.ai[1] == 488 && jumpSlashing))) //If timer is at 0 and player is within range
            {
                NPC.TargetClosest(true);
                stabbing = true;
                shielding = false;
                jumpSlashing = false;
                NPC.ai[1] = 430;
                slashing = false;
                NPC.ai[3] = 0; //Reset timer
            }

            if (stabbing)
            {
                ++NPC.ai[1];

                if (NPC.ai[1] < 436)
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X -= 0.15f;
                        if (NPC.velocity.X < 0)
                        {
                            NPC.velocity.X = 0;
                        }
                    }

                    else
                    {
                        NPC.velocity.X += 0.15f;
                        if (NPC.velocity.X > 0)
                        {
                            NPC.velocity.X = 0;
                        }
                    }
                }

                if (NPC.ai[1] == 436) //If timer is 46
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 1.0f, PitchVariance = 0.3f }, player.Center); //Play slash/swing sound

                    if (lifePercentage <= 60)
                    {
                        if (NPC.direction == 1)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(48, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                            NPC.velocity.X += 10.5f;
                            //npc.velocity.Y -= 2f;
                        }

                        else
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-48, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                            NPC.velocity.X -= 10.5f;
                            //npc.velocity.Y -= 2f;
                        }
                    }
                    else
                    {
                        if (NPC.direction == 1)
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                            NPC.velocity.X += 10.5f;
                            //npc.velocity.Y -= 2f;
                        }

                        else
                        {
                            Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-44, -2), new Vector2(0, 0), projStab, (int)(damage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                            NPC.velocity.X -= 10.5f;
                            //npc.velocity.Y -= 2f;
                        }
                    }
                }

                if (NPC.ai[1] > 450 && NPC.ai[1] < 489)
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X -= 0.3f;
                        if (NPC.velocity.X < 0)
                        {
                            NPC.velocity.X = 0;
                        }
                    }

                    else
                    {
                        NPC.velocity.X += 0.3f;
                        if (NPC.velocity.X > 0)
                        {
                            NPC.velocity.X = 0;
                        }
                    }
                }

                if (NPC.ai[1] > 489)
                {
                    NPC.ai[1] = 280;
                    stabbing = false;
                }

            }
            //}


            //Shielding Flame/Fire Attack

            //if (shielding || npc.ai[2] > 300 || )
            //{
            NPC.ai[2]++;

            if (!jumpSlashing && !slashing && !stabbing && NPC.velocity.Y == 0 && (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0) || shielding))
            {
                if (NPC.ai[2] > 300 && NPC.ai[2] <= 310)
                {
                    if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; }
                    else { NPC.velocity.X += 0.15f; }
                }

                if (NPC.ai[2] > 310)
                {
                    NPC.velocity.X = 0;
                    shielding = true;
                }

                if (NPC.ai[2] > 380 && NPC.ai[2] < 460 && NPC.Distance(player.Center) > 55 && NPC.Distance(player.Center) < 300)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (20f * NPC.direction), NPC.Center.Y, 8f * NPC.direction, Main.rand.NextFloat(-1f, 0), ModContent.ProjectileType<Projectiles.Enemy.SmallFlameJet>(), 20, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 25;
                        //play breath sound
                        if (Main.rand.NextBool(3))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, PitchVariance = 0.1f }, NPC.Center); //flame thrower sound
                        }
                    }
                }

                if (NPC.ai[2] > 530)
                {
                    shielding = false;
                    NPC.ai[2] = 0;
                }
            }
            //}
            #endregion
        }


        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            int shieldPower = NPC.defense * 2;

            if (shielding)
            {
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                        damage -= shieldPower;
                        if (NPC.ai[2] > 355)
                        {
                            NPC.ai[2] -= 25;
                        }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                        damage -= shieldPower;
                        if (NPC.ai[2] > 355)
                        {
                            NPC.ai[2] -= 25;
                        }
                    }
                }
            }

            if (NPC.direction == 1) //if enemy facing right
            {
                if (player.position.X < NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    damage = (int)(damage * 2f); //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    damage = (int)(damage * 2f); //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }

            NPC.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.player[NPC.target];

            int shieldPower = NPC.defense * 3;

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.BlizzardBlasterShot>())
            {
                if (shielding)
                {
                    if (NPC.direction == 1) //if npc facing right
                    {
                        if (projectile.Center.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;
                            if (NPC.ai[1] < 340)
                            {
                                NPC.ai[1] += 70; //Used for Jump-slash
                            }
                            if (NPC.ai[2] > 355)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }

                        else if (hitDirection == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;

                            if (NPC.ai[1] < 340)
                            {
                                NPC.ai[1] += 80; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 355)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            damage -= shieldPower;
                            knockback = 0f;
                            if (NPC.ai[1] < 340)
                            {
                                NPC.ai[1] += 70; //Used for Jump-slash
                            }
                            if (NPC.ai[2] > 355)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                        else if (hitDirection == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            damage -= shieldPower;

                            knockback = 0f;
                            if (NPC.ai[1] < 340)
                            {
                                NPC.ai[1] += 80; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 355)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                    }
                }


                if (NPC.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (hitDirection == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }
                else //if enemy facing left
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (hitDirection == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                        damage = (int)(damage * 2f); //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }

                if (NPC.Distance(player.Center) > 220 && !shielding)
                {
                    NPC.ai[2] += 120;
                }

                if (NPC.ai[1] < 400)
                {
                    NPC.ai[1] += 10;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.townNPCs > 1f) return 0f;

            if (spawnInfo.Player.ZoneUnderworldHeight) return 0.15f;

            return chance;
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.RadiantLifegem>(), 4));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 4));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Broadswords.AncientFireSword>(), 10));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ItemID.RagePotion, 5, 1, 1, 3));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ItemID.WrathPotion, 5, 1, 1, 3));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 5, 1, 1, 3));

        }

        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor)
        {
            /*Color color = Color.DimGray;
            drawColor = color;*/
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor) //PreDraw for trails
        {
            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection
            if ((NPC.velocity.X > 5f || NPC.velocity.X < -5f) && stabbing)
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

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D texture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/RingedKnight_Glow");
            Texture2D firesword = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/RingedKnight_FireSword");
            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);
            }

            if (lifePercentage <= 60)
            {
                if (NPC.spriteDirection == 1)
                {
                    spriteBatch.Draw(firesword, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(firesword, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            //Main.NewText(shieldAnimTimer);
            //Main.NewText(shieldFrame);

            if (NPC.velocity.X != 0) //Walking
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

            if (NPC.velocity.Y != 0 && (!jumpSlashing && !shielding && !stabbing)) //If falling/jumping
            {
                NPC.frame.Y = 1 * frameHeight;
            }

            if (slashing) //If slashing
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[3] < 18)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.ai[3] < 26)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.ai[3] < 29)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.ai[3] < 32)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                else if (NPC.ai[3] < 35)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (NPC.ai[3] < 49)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (jumpSlashing) //If jumpslashing
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 428)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.ai[1] < 436)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.ai[1] < 439)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.ai[1] < 442)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                else if (NPC.ai[1] < 445)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (NPC.ai[1] < 489)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (stabbing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 436)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.ai[1] < 470)
                {
                    NPC.frame.Y = 18 * frameHeight;
                }
                else if (NPC.ai[1] < 475)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (NPC.ai[1] < 489)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }

            }
            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing && !stabbing) //If not moving at all (shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding && NPC.ai[2] > 360 && NPC.ai[2] < 460 && NPC.Distance(Main.player[NPC.target].Center) > 100 && NPC.Distance(Main.player[NPC.target].Center) < 300)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 11 * frameHeight;
            }

            /*if (shielding) //this is the shield shine anim
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
            }*/
        }

        #endregion

    }
}
