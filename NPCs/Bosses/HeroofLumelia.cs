using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    class HeroofLumelia : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hero of Lumelia");

        }

        public int throwingKnifeDamage = 44;
        public int smokebombDamage = 22;
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 75;
            NPC.defense = 55;
            NPC.boss = true;
            //npc.timeLeft = 22000;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 20000;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.1f;
            NPC.rarity = 4;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HeroOfLumeliaBanner>();
            despawnHandler = new NPCDespawnHandler("The hero of Lumelia stands victorious...", Color.Gold, DustID.GoldFlame);
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            herosArrowDamage = (int)(herosArrowDamage / 2);
            throwingKnifeDamage = (int)(throwingKnifeDamage / 2);
        }

        int herosArrowDamage = 35; //was 40, which means 80? ouch.

        bool wolfSpawned1 = false;
        bool wolfSpawned2 = false;
        bool wolfSpawned3 = false;

        float customAi1;
        float customSpawn1;
        float customSpawn2;

        NPCDespawnHandler despawnHandler;

        int drownTimerMax = 4000;
        int drownTimer = 4000;
        int drowningRisk = 1500;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            //if (!NPC.downedBoss3) //changed from 2
            //{
            //	return 0;
            //}

            //if (Main.hardMode && !(P.ZoneCorrupt || P.ZoneCrimson || tsorcRevampWorld.SuperHardMode) && P.ZoneOverworldHeight && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()) && Main.rand.Next(500) == 1) { UsefulFunctions.BroadcastText("A hero from Lumelia has come to kill you for unleashing darkness upon the world... ", 175, 75, 255); return 1; }//why tim xD -- haha, no more preHM spawn with the new lore
            //if (Main.hardMode && !(P.ZoneCorrupt || P.ZoneCrimson || tsorcRevampWorld.SuperHardMode) && P.ZoneOverworldHeight && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()) && Main.rand.Next(350) == 1) { UsefulFunctions.BroadcastText("A hero from Lumelia has come seeking justice for their slain brother, Aaron...", 175, 75, 255); return 1; }
            //if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && P.ZoneOverworldHeight && P.ZoneSnow && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()) && Main.rand.Next(300) == 1) { Main.NewText("'You killed my brother!' A hero from Lumelia has come to kill you for slaying Aaron... ", 175, 75, 255); return 1; }
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && P.ZoneOverworldHeight && P.ZoneDesert && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()) && !(P.ZoneCorrupt || P.ZoneCrimson) && !P.ZoneBeach && Main.rand.Next(500) == 1) { UsefulFunctions.BroadcastText("'You killed my brother!' A hero from Lumelia has come to kill you for slaying his kin... ", 175, 75, 255); return 1; }

            return 0;
        }
        #endregion


        #region AI // code by GrtAndPwrflTrtl (http://www.terrariaonline.com/members/grtandpwrfltrtl.86018/)
        public override void AI()  //  warrior ai
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            #region set up NPC's attributes & behaviors
            // set parameters
            //  is_archer OR can_pass_doors OR shoot_and_walk, pick only 1.  They use the same ai[] vars (1&2)
            bool is_archer = false; // stops and shoots when target sighted; skel archer & gob archer are the archers
            bool can_pass_doors = false;  //  can open or break doors; c. bunny, crab, clown, skel archer, gob archer, & chaos elemental cannot
            bool shoot_and_walk = true;  //  can shoot while walking like clown; uses ai[2] so cannot be used with is_archer or can_pass_doors

            //  can_teleport==true code uses boredom_time and ai[3] (boredom), but not mutually exclusive
            bool can_teleport = true;  //  tp around like chaos ele
            int boredom_time = 60; // time until it stops targeting player if blocked etc, 60 for anything but chaos ele, 20 for chaos ele
            int boredom_cooldown = 10 * boredom_time; // boredom level where boredom wears off; usually 10*boredom_time

            bool hates_light = false;  //  flees in daylight like: Zombie, Skeleton, Undead Miner, Doctor Bones, The Groom, Werewolf, Clown, Bald Zombie, Possessed Armor
            bool can_pass_doors_bloodmoon_only = false;  //  can open or break doors, but only during bloodmoon: zombies & bald zombies. Will keep trying anyway.

            Terraria.Audio.SoundStyle? sound_type = null; //What sound to play. Use SoundID.SoundName to pick one, 'null' is no sound.
            int sound_frequency = 1000;  //  chance to play sound every frame, 1000 for zombie/skel, 500 for mummies

            float acceleration = .05f;  //  how fast it can speed up
            float top_speed = 3f;  //  max walking speed, also affects jump length
            float braking_power = .2f;  //  %of speed that can be shed every tick when above max walking speed
            double bored_speed = .9;  //  above this speed boredom decreases(if not already bored); usually .9

            float enrage_percentage = 0.4f;  // double movement speed below this life fraction. 0 for no enrage. Mummies enrage below .5
            float enrage_acceleration = .14f;  //  faster when enraged, usually 2*acceleration
            float enrage_top_speed = 5f;  //  faster when enraged, usually 2*top_speed

            bool clown_sized = false; // is hitbox the same as clowns' for purposes of when to jump?
            bool jump_gaps = true; // attempt to jump gaps; everything but crabs do this

            bool hops = false; // hops when close to target like Angry Bones, Corrupt Bunny, Armored Skeleton, and Werewolf
                               //bool dash = false;
            float hop_velocity = 1f; // forward velocity needed to initiate hopping; usually 1
            float hop_range_x = 100; // less than this is 'close to target'; usually 100
            float hop_range_y = 50; // less than this is 'close to target'; usually 50
            float hop_power = 4; // how hard/high offensive hops are; usually 4
            float hop_speed = 3; // how fast hops can accelerate vertically; usually 3 (2xSpd is 4 for Hvy Skel & Werewolf so they're noticably capped)



            // is_archer & clown bombs only
            int shot_rate = 70;  //  rate at which archers/bombers fire; 70 for skeleton archer, 180 for goblin archer, 450 for clown; atm must be an even # or won't fire at shot_rate/2
                                 //int fuse_time = 300;  //  fuse time on bombs, 300 for clown bombs
            int projectile_damage = 35;  //  projectile dmg: 35 for Skeleton Archer, 11 for Goblin Archer
            int projectile_id = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellMeteor>(); // projectile id: 82(Flaming Arrow) for Skeleton Archer, 81(Wooden Arrow) for Goblin Archer, 75(Happy Bomb) for Clown
            float projectile_velocity = 11; // initial velocity? 11 for Skeleton Archers, 9 for Goblin Archers, bombs have fixed speed & direction atm

            // can_pass_doors only
            float door_break_pow = 2; // 10 dmg breaks door; 2 for goblin thief and 7 for Angry Bones; 1 for others
            bool breaks_doors = false; // meaningless unless can_pass_doors; if this is true the door breaks down instead of trying to open; Goblin Peon is only warrior to do this

            // Omnirs creature sorts
            bool tooBig = false; // force bigger creatures to jump
            bool lavaJumping = true; // Enemies jump on lava.
            bool canDrown = false; // They will drown if in the water for too long

            // calculated parameters
            bool moonwalking = false;  //  not jump/fall and moving backwards to facing
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
                moonwalking = true;
            #endregion
            //-------------------------------------------------------------------
            #region Too Big and Lava Jumping
            if (tooBig)
            {
                if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X -= 2f;
                }
                else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X += 2f;
                }
            }
            if (lavaJumping)
            {
                if (NPC.lavaWet)
                {
                    NPC.velocity.Y -= 2;
                }
            }
            #endregion
            //-------------------------------------------------------------------
            #region teleportation particle effects
            if (can_teleport)  //  chaos elemental type teleporter
            {
                if (NPC.ai[3] == -120f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
                {
                    NPC.velocity *= 0f; // stop moving
                    NPC.ai[3] = 0f; // reset boredom to 0
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
                        int num9 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 71, num6, num7, 200, default(Color), 2f);
                        Main.dust[num9].noGravity = true; // floating
                        Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
                        expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
                    }
                    for (int k = 0; k < 20; k++) // more dust effects at old position
                    {
                        int num10 = Dust.NewDust(NPC.oldPos[2], NPC.width, NPC.height, 71, -num6, -num7, 200, default(Color), 2f);
                        Main.dust[num10].noGravity = true;
                        Dust expr_1A6F_cp_0 = Main.dust[num10];
                        expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
                    }
                } // END just teleported
            } // END can teleport
            #endregion
            //-------------------------------------------------------------------
            #region adjust boredom level
            if (!is_archer || NPC.ai[2] <= 0f)  //  loop to set ai[3] (boredom)
            {
                if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= (float)boredom_time || moonwalking)  //  stopped or bored or moonwalking
                    NPC.ai[3] += 1f; // increase boredom
                else if ((double)Math.Abs(NPC.velocity.X) > bored_speed && NPC.ai[3] > 0f)  //  moving fast and not bored
                    NPC.ai[3] -= 1f; // decrease boredom

                if (NPC.justHit || NPC.ai[3] > boredom_cooldown)
                    NPC.ai[3] = 0f; // boredom wears off if enough time passes, or if hit

                if (NPC.ai[3] == (float)boredom_time)
                    NPC.netUpdate = true; // netupdate when state changes to bored
            }
            #endregion
            //-------------------------------------------------------------------
            #region play creature sounds, target/face player, respond to boredom
            if ((!hates_light || !Main.dayTime || (double)NPC.position.Y > Main.worldSurface * 16.0) && NPC.ai[3] < (float)boredom_time)
            {   // not fleeing light & not bored
                if (sound_type.HasValue && Main.rand.Next(sound_frequency) <= 0)
                    Terraria.Audio.SoundEngine.PlaySound(sound_type.Value, NPC.Center); // random creature sounds
                if (!canDrown)
                {
                    NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }
                if (canDrown && !NPC.wet)
                {
                    NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }
            }
            else if (!is_archer || NPC.ai[2] <= 0f) //  fleeing light or bored (& not aiming)
            {
                if (hates_light && Main.dayTime && (double)(NPC.position.Y / 16f) < Main.worldSurface && NPC.timeLeft > 10)
                    NPC.timeLeft = 10;  //  if hates light & in light, hasten despawn

                if (NPC.velocity.X == 0f)
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
            } // END fleeing light or bored (& not aiming)
            #endregion
            //-------------------------------------------------------------------
            #region enrage
            bool enraged = false; // angry from damage; not stored from tick to tick
            if ((enrage_percentage > 0) && (NPC.life < (float)NPC.lifeMax * enrage_percentage))  //  speed up at low life
                enraged = true;
            if (enraged)
            { // speed up movement if enraged
                acceleration = enrage_acceleration;
                top_speed = enrage_top_speed;
            }
            #endregion
            //-------------------------------------------------------------------
            #region melee movement
            if (!is_archer || (NPC.ai[2] <= 0f && !NPC.confused))  //  meelee attack/movement. archers only use while not aiming
            {
                if (Math.Abs(NPC.velocity.X) > top_speed)  //  running/flying faster than top speed
                {
                    if (NPC.velocity.Y == 0f)  //  and not jump/fall
                        NPC.velocity *= (1f - braking_power);  //  decelerate
                }
                else if ((NPC.velocity.X < top_speed && NPC.direction == 1) || (NPC.velocity.X > -top_speed && NPC.direction == -1))
                {  //  running slower than top speed (forward), can be jump/fall
                    if (can_teleport && moonwalking)
                        NPC.velocity.X = NPC.velocity.X * 0.99f;  //  ? small decelerate for teleporters

                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * acceleration;  //  accellerate fwd; can happen midair
                    if ((float)NPC.direction * NPC.velocity.X > top_speed)
                        NPC.velocity.X = (float)NPC.direction * top_speed;  //  but cap at top speed
                }  //  END running slower than top speed (forward), can be jump/fall
            } // END non archer or not aiming*/

            if (NPC.ai[1] == 0)
            {
                NPC.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
            }

            //Turn and walk away if hitting a wall
            if (NPC.position.X == NPC.oldPosition.X)
            {
                NPC.ai[1]++;
                if (NPC.ai[1] > 120 && NPC.velocity.Y == 0)
                {
                    NPC.direction *= -1;
                    NPC.spriteDirection = NPC.direction;
                    NPC.ai[1] = 50;
                }
            }

            Player player = Main.player[NPC.target];

            if (NPC.ai[1] == 51 && NPC.Distance(player.Center) > 1100)
            {
                NPC.ai[1] = 0;
            }

            if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
            {
                NPC.ai[1] = 0;
            }


            #endregion
            //-------------------------------------------------------------------
            #region archer projectile code (stops moving to shoot)
            if (is_archer)
            {
                if (NPC.confused)
                    NPC.ai[2] = 0f; // won't try to stop & aim if confused
                else // not confused
                {
                    if (NPC.ai[1] > 0f)
                        NPC.ai[1] -= 1f; // decrement fire & reload counter

                    if (NPC.justHit) // was just hit?
                    {
                        NPC.ai[1] = 30f; // shot on .5 sec cooldown
                        NPC.ai[2] = 0f; // not aiming
                    }
                    if (NPC.ai[2] > 0f) // if aiming: adjust aim and fire if needed
                    {
                        NPC.TargetClosest(true); // target and face closest player
                        if (NPC.ai[1] == (float)(shot_rate / 2))  //  fire at halfway through; first half of delay is aim, 2nd half is cooldown
                        { // firing:
                            Vector2 npc_center = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f); // npc position
                            float npc_to_target_x = Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f - npc_center.X; // x vector to target
                            float num16 = Math.Abs(npc_to_target_x) * 0.1f; // 10% of x distance to target: to aim high if farther?
                            float npc_to_target_y = Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f - npc_center.Y - num16; // y vector to target (aiming high at distant targets)
                            npc_to_target_x += (float)Main.rand.Next(-40, 41); //  targeting error: 40 pix=2.5 blocks
                            npc_to_target_y += (float)Main.rand.Next(-40, 41); //  targeting error: 40 pix=2.5 blocks
                            float target_dist = (float)Math.Sqrt((double)(npc_to_target_x * npc_to_target_x + npc_to_target_y * npc_to_target_y)); // distance to target
                            NPC.netUpdate = true; // ??
                            target_dist = projectile_velocity / target_dist; // to normalize by projectile_velocity
                            npc_to_target_x *= target_dist; // normalize by projectile_velocity
                            npc_to_target_y *= target_dist; // normalize by projectile_velocity
                            npc_center.X += npc_to_target_x;  //  initial projectile position includes one tick of initial movement
                            npc_center.Y += npc_to_target_y;  //  initial projectile position includes one tick of initial movement
                            if (Main.netMode != 1)  //  is server
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), npc_center.X, npc_center.Y, npc_to_target_x, npc_to_target_y, projectile_id, projectile_damage, 0f, Main.myPlayer);

                            if (Math.Abs(npc_to_target_y) > Math.Abs(npc_to_target_x) * 2f) // target steeply above/below NPC
                            {
                                if (npc_to_target_y > 0f)
                                    NPC.ai[2] = 1f; // aim downward
                                else
                                    NPC.ai[2] = 5f; // aim upward
                            }
                            else if (Math.Abs(npc_to_target_x) > Math.Abs(npc_to_target_y) * 2f) // target on level with NPC
                                NPC.ai[2] = 3f;  //  aim straight ahead
                            else if (npc_to_target_y > 0f) // target is below NPC
                                NPC.ai[2] = 2f;  //  aim slight downward
                            else // target is not below NPC
                                NPC.ai[2] = 4f;  //  aim slight upward
                        } // END firing
                        if (NPC.velocity.Y != 0f || NPC.ai[1] <= 0f) // jump/fall or firing reload
                        {
                            NPC.ai[2] = 0f; // not aiming
                            NPC.ai[1] = 0f; // reset firing/reload counter (necessary? nonzero maybe)
                        }
                        else // no jump/fall and no firing reload
                        {
                            NPC.velocity.X = NPC.velocity.X * 0.9f; // decelerate to stop & shoot
                            NPC.spriteDirection = NPC.direction; // match animation to facing
                        }
                    } // END if aiming: adjust aim and fire if needed
                    if (NPC.ai[2] <= 0f && NPC.velocity.Y == 0f && NPC.ai[1] <= 0f && !Main.player[NPC.target].dead && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                    { // not aiming & no jump/fall & fire/reload ctr is 0 & target is alive and LOS to target: start aiming
                        float num21 = 10f; // dummy vector length in place of initial velocity? not sure why this is needed
                        Vector2 npc_center = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f);
                        float npc_to_target_x = Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f - npc_center.X;
                        float num23 = Math.Abs(npc_to_target_x) * 0.1f; // 10% of x distance to target: to aim high if farther?
                        float npc_to_target_y = Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f - npc_center.Y - num23; // y vector to target (aiming high at distant targets)
                        npc_to_target_x += (float)Main.rand.Next(-40, 41);
                        npc_to_target_y += (float)Main.rand.Next(-40, 41);
                        float target_dist = (float)Math.Sqrt((double)(npc_to_target_x * npc_to_target_x + npc_to_target_y * npc_to_target_y));
                        if (target_dist < 700f) // 700 pix = 43.75 blocks
                        { // target is in range
                            NPC.netUpdate = true; // ??
                            NPC.velocity.X = NPC.velocity.X * 0.5f; // hard brake
                            target_dist = num21 / target_dist; // to normalize by num21
                            npc_to_target_x *= target_dist; // normalize by num21
                            npc_to_target_y *= target_dist; // normalize by num21
                            NPC.ai[2] = 3f; // aim straight ahead
                            NPC.ai[1] = (float)shot_rate; // start fire & reload counter
                            if (Math.Abs(npc_to_target_y) > Math.Abs(npc_to_target_x) * 2f) // target steeply above/below NPC
                            {
                                if (npc_to_target_y > 0f)
                                    NPC.ai[2] = 1f; // aim downward
                                else
                                    NPC.ai[2] = 5f; // aim upward
                            }
                            else if (Math.Abs(npc_to_target_x) > Math.Abs(npc_to_target_y) * 2f) // target on level with NPC
                                NPC.ai[2] = 3f; // aim straight ahead
                            else if (npc_to_target_y > 0f)
                                NPC.ai[2] = 2f; // aim slight downward
                            else
                                NPC.ai[2] = 4f; // aim slight upward
                        } // END target is in range
                    } // END start aiming
                } // END not confused
            }  //  END is archer
            #endregion
            //-------------------------------------------------------------------
            #region shoot and walk
            if (shoot_and_walk && Main.netMode != 1 && !Main.player[NPC.target].dead) // can generalize this section to moving+projectile code
            {
                //ENEMY SPAWNS
                if (NPC.Distance(player.Center) > 220)
                {

                    if ((customSpawn1 == 0) && NPC.life <= 4500)
                    {
                        //spawn it and increase customSpawn1
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.EnchantedSword, 0);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        customSpawn1 += 1f;
                    }
                    if ((customSpawn1 == 1) && NPC.life <= 3500)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.EnchantedSword, 0);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        customSpawn1 += 1f;
                    }
                    if ((customSpawn1 == 2) && NPC.life <= 3000)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.ManHunter>(), 0);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        customSpawn1 += 1f;
                    }


                    //WOLF SPAWNS
                    if (!wolfSpawned1 && NPC.life <= 2500)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Wolf, 0);
                        int Spawned2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Wolf, 0);
                        int Spawned3 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.Assassin>(), 0);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.01f }, NPC.Center);
                        wolfSpawned1 = true;

                    }

                    if (!wolfSpawned2 && NPC.life <= 1500)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Wolf, 0);
                        int Spawned2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Wolf, 0);

                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.02f }, NPC.Center);

                        wolfSpawned2 = true;

                    }

                    if (!wolfSpawned3 && NPC.life <= 1000)
                    {
                        int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.Wolf, 0);
                        int Spawned2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.RedCloudHunter>(), 0);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = 0.01f }, NPC.Center);

                        wolfSpawned3 = true;

                    }
                }




                #region Projectiles

                customAi1++; ;
                NPC.TargetClosest(true);

                //JUSTHIT CODE

                if (NPC.justHit && NPC.Distance(player.Center) < 100)
                {
                    customAi1 = 1f;
                }
                if (NPC.justHit && NPC.Distance(player.Center) < 350 && Main.rand.Next(3) == 1)//
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f); //was 6 and 3
                    float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-6f, -4f);
                    NPC.velocity.X = v;
                    if (Main.rand.Next(2) == 1)
                    { customAi1 = 70f; } //was 100 but knife goes away and doesn't shoot consistently
                    else
                    { customAi1 = 240f; }


                    NPC.netUpdate = true;
                }
                if (NPC.justHit && NPC.Distance(player.Center) > 351 && Main.rand.Next(3) == 1)
                {
                    NPC.knockBackResist = 0f;
                    NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(9f, 3f);
                    NPC.netUpdate = true;

                }

                //ARROWS FROM ARCHERS NEARBY ATTACK
                if (NPC.Distance(player.Center) < 500 && Main.rand.Next(350) == 0)
                {
                    Player nT = Main.player[NPC.target];


                    if (Main.rand.Next(4) == 0)
                    {
                        UsefulFunctions.BroadcastText("Archers nearby!", 175, 75, 255);
                    }

                    for (int pcy = 0; pcy < 10; pcy++)
                    {
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 500f, (float)(-50 + Main.rand.Next(100)) / 10, 8.9f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //ORIGINAL
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(200), (float)nT.position.Y - 800f, (float)(-50 + Main.rand.Next(100)) / 10, 7.1f, ModContent.ProjectileType<Projectiles.Enemy.HerosArrow>(), herosArrowDamage, 2f, Main.myPlayer); //was 8.9f near 10, not sure what / 10, does

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, NPC.Center);

                        NPC.netUpdate = true;

                    }
                }

                //ARROWS FROM ARCHERS IN THE DISTANCE ATTACK
                if (NPC.Distance(player.Center) > 800 && Main.rand.Next(100) == 0)
                {
                    Player nT = Main.player[NPC.target];

                    if (Main.rand.Next(4) == 0)
                    {
                        UsefulFunctions.BroadcastText("Archers in the trees!", 175, 75, 255);
                    }

                    for (int pcy = 0; pcy < 12; pcy++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 100 + Main.rand.Next(400), (float)nT.position.Y - 900f, (float)(-50 + Main.rand.Next(100)) / 10, 9.1f, ModContent.ProjectileType<Projectiles.Enemy.HerosArrow>(), herosArrowDamage, 2f, Main.myPlayer); //was 8.9f near 10, tried Main.rand.Next(2, 5)

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, NPC.Center);

                        NPC.netUpdate = true;

                    }
                }




                //JUMP BEFORE KNIFE ATTACK SOMETIMES
                if (customAi1 == 130f && NPC.velocity.Y == 0f && NPC.life >= 1001 && Main.rand.Next(2) == 1)
                //if (customAi1 >= 130f && customAi1 <= 131f && npc.velocity.Y == 0f && Main.rand.Next(2) == 1)
                {

                    NPC.velocity.Y = Main.rand.NextFloat(-10f, -5f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8); //0.4f, true, true																								
                    speed += Main.rand.NextVector2Circular(-4, -2);
                    if (Main.rand.Next(4) == 1 && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        //customAi1 = 132f;
                    }

                    NPC.netUpdate = true;
                }

                //DESPERATE FINAL ATTACK
                if (customAi1 >= 130f && customAi1 <= 148f && NPC.life <= 1000)
                //if (customAi1 >= 130f && customAi1 <= 131f && npc.velocity.Y == 0f && Main.rand.Next(2) == 1)
                {
                    if (NPC.velocity.Y == 0f)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-11f, -8f);
                    }


                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8); //0.4f, true, true																								
                    speed += Main.rand.NextVector2Circular(-4, -2);
                    if (Main.rand.Next(4) == 1 && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        //customAi1 = 132f;
                    }

                    NPC.netUpdate = true;
                }

                //THROW KNIFE
                //&& Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1)
                //if (Collision.CanHitLine(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))			
                if (customAi1 == 152)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12); //0.4f, true, true
                                                                                                                         //speed += Main.rand.NextVector2Circular(-3, -1);
                    speed += Main.player[NPC.target].velocity / 2;
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);

                        //go to smoke bomb attack
                        customAi1 = 200f;
                        NPC.knockBackResist = 0.1f;

                        if (Main.rand.Next(2) == 1)
                        {
                            //or chance to reset - decided not to use for now
                            //customAi1 = 1f;
                        }
                    }
                    NPC.netUpdate = true;

                }

                //SMOKE BOMB DUST TELEGRAPH
                if (customAi1 >= 220 && customAi1 >= 280) //&& npc.Distance(player.Center) > 10
                {
                    Lighting.AddLight(NPC.Center, Color.Green.ToVector3() * 1f);
                    if (Main.rand.Next(2) == 1 && NPC.Distance(player.Center) > 1)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                    }

                    //JUMP BEFORE BOMB ATTACK SOMETIMES
                    if (customAi1 == 260f && NPC.velocity.Y == 0f && Main.rand.Next(2) == 1)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-8f, -4f);
                        NPC.netUpdate = true;
                    }

                    //SMOKE BOMB ATTACK
                    if (customAi1 >= 280) //&& npc.Distance(player.Center) > 10
                    {
                        NPC.TargetClosest(true);

                        if (Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                        {
                            Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10); //, 0.2f, false, false
                            speed2 += Main.rand.NextVector2Circular(-4, 0);
                            //speed2 += Main.player[npc.target].velocity / 2;
                            if (((speed2.X < 0f) && (NPC.velocity.X < 0f)) || ((speed2.X > 0f) && (NPC.velocity.X > 0f)))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySmokebomb>(), smokebombDamage, 0f, Main.myPlayer);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center); //Play swing-throw sound
                                customAi1 = 1f;
                            }
                        }
                    }
                }

                #endregion

            }
            #endregion
            //-------------------------------------------------------------------
            #region check if standing on a solid tile
            // warning: this section contains a return statement
            bool standing_on_solid_tile = false;
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
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
            //-------------------------------------------------------------------
            #region new Tile()s, door opening/breaking
            if (standing_on_solid_tile)  //  if standing on solid tile
            {
                int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f); // 15 pix in front of center of mass
                int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f); // 15 pix above feet
                if (clown_sized)
                    x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)((NPC.width / 2 + 16) * NPC.direction)) / 16f); // 16 pix in front of edge
                                                                                                                                         //  create? 5 tile high stack in front
                if (Main.tile[x_in_front, y_above_feet] == null)
                    Main.tile[x_in_front, y_above_feet].ClearTile();

                if (Main.tile[x_in_front, y_above_feet - 1] == null)
                    Main.tile[x_in_front, y_above_feet - 1].ClearTile();

                if (Main.tile[x_in_front, y_above_feet - 2] == null)
                    Main.tile[x_in_front, y_above_feet - 2].ClearTile();

                if (Main.tile[x_in_front, y_above_feet - 3] == null)
                    Main.tile[x_in_front, y_above_feet - 3].ClearTile();

                if (Main.tile[x_in_front, y_above_feet + 1] == null)
                    Main.tile[x_in_front, y_above_feet + 1].ClearTile();
                //  create? 2 other tiles farther in front
                if (Main.tile[x_in_front + NPC.direction, y_above_feet - 1] == null)
                    Main.tile[x_in_front + NPC.direction, y_above_feet - 1].ClearTile();

                if (Main.tile[x_in_front + NPC.direction, y_above_feet + 1] == null)
                    Main.tile[x_in_front + NPC.direction, y_above_feet + 1].ClearTile();

                if (Main.tile[x_in_front, y_above_feet - 1].HasTile && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && can_pass_doors)
                { // tile in front is active, is door and NPC can pass doors: trying to break door
                    NPC.ai[2] += 1f; // inc knock countdown
                    NPC.ai[3] = 0f; // not bored if working on breaking a door
                    if (NPC.ai[2] >= 60f)  //  knock once per second
                    {
                        if (!Main.bloodMoon && can_pass_doors_bloodmoon_only)
                            NPC.ai[1] = 0f;  //  damage counter zeroed unless bloodmoon, but will still knock

                        NPC.velocity.X = 0.5f * (float)(-(float)NPC.direction); //  slight recoil from hitting it
                        NPC.ai[1] += door_break_pow;  //  increase door damage counter
                        NPC.ai[2] = 0f;  //  knock finished; start next knock
                        bool door_breaking = false;  //  door break flag
                        if (NPC.ai[1] >= 10f)  //  at 10 damage, set door as breaking (and cap at 10)
                        {
                            door_breaking = true;
                            NPC.ai[1] = 10f;
                        }
                        WorldGen.KillTile(x_in_front, y_above_feet - 1, true, false, false);  //  kill door ? when door not breaking too? can fail=true; effect only would make more sense, to make knocking sound
                        if (door_breaking && Main.netMode != 1)  //  server and door breaking
                        {
                            if (breaks_doors)  //  breaks doors rather than attempt to open
                            {
                                WorldGen.KillTile(x_in_front, y_above_feet - 1, false, false, false);  //  kill door
                                if (Main.netMode == 2) // server
                                    NetMessage.SendData(17, -1, -1, null, 0, (float)x_in_front, (float)(y_above_feet - 1), 0f, 0); // ?? tile breaking and/or item drop probably
                            }
                            else  //  try to open without breaking
                            {
                                bool door_opened = WorldGen.OpenDoor(x_in_front, y_above_feet, NPC.direction);  //  open the door
                                if (!door_opened)  //  door not opened successfully
                                {
                                    NPC.ai[3] = (float)boredom_time;  //  bored if door is stuck
                                    NPC.netUpdate = true;
                                    NPC.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
                                }
                                if (Main.netMode == 2 && door_opened) // is server & door was just opened
                                    NetMessage.SendData(19, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)NPC.direction, 0); // ??
                            }
                        }  //  END server and door breaking
                    } // END knock on door
                } // END trying to break door
                #endregion
                //-------------------------------------------------------------------
                #region jumping, reset door knock & damage counters
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
                        else if (NPC.directionY < 0 && jump_gaps && (!Main.tile[x_in_front, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                        { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                            NPC.velocity.Y = -8f; // jump with power 8
                            NPC.velocity.X = NPC.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                            NPC.netUpdate = true;
                        }
                        else if (can_pass_doors) // standing on solid tile but not in front of a passable door, moving forward, didnt jump.  I assume recoil from hitting door is too small to move passable door out of range and trigger this
                        {
                            NPC.ai[1] = 0f;  //  reset door dmg counter
                            NPC.ai[2] = 0f;  //  reset knock counter
                        }
                    } // END moving forward, still: standing on solid tile but not in front of a passable door
                    if (hops && NPC.velocity.Y == 0f && Math.Abs(NPC.position.X + (float)(NPC.width / 2) - (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2))) < hop_range_x && Math.Abs(NPC.position.Y + (float)(NPC.height / 2) - (Main.player[NPC.target].position.Y + (float)(Main.player[NPC.target].height / 2))) < hop_range_y && ((NPC.direction > 0 && NPC.velocity.X >= hop_velocity) || (NPC.direction < 0 && NPC.velocity.X <= -hop_velocity)))
                    { // type that hops & no jump/fall & near target & moving forward fast enough: hop code
                        NPC.velocity.X = NPC.velocity.X * 2f; // burst forward
                        if (NPC.velocity.X > hop_speed) // but cap at hop_speed
                            NPC.velocity.X = hop_speed;
                        else if (NPC.velocity.X < -hop_speed)
                            NPC.velocity.X = -hop_speed;

                        NPC.velocity.Y = -hop_power; // and jump of course
                        NPC.netUpdate = true;
                    }
                    if (can_teleport && NPC.velocity.Y < 0f) // jumping
                        NPC.velocity.Y = NPC.velocity.Y * 1.1f; // infinite jump? antigravity?
                }


            }
            else if (can_pass_doors)  //  not standing on a solid tile & can open/break doors
            {
                NPC.ai[1] = 0f;  //  reset door damage counter
                NPC.ai[2] = 0f;  //  reset knock counter
            }//*/
            #endregion
            //-------------------------------------------------------------------
            #region teleportation
            if (Main.netMode != 1 && can_teleport && NPC.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
            {
                int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                int x_blockpos = (int)NPC.position.X / 16; // corner not center
                int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                int tp_radius = 20; // radius around target(upper left corner) in blocks to teleport into
                int tp_counter = 0;
                bool flag7 = false;
                if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 20000f)
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
                        if ((m < target_y_blockpos - 6 || m > target_y_blockpos + 6 || tp_x_target < target_x_blockpos - 6 || tp_x_target > target_x_blockpos + 6) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].HasTile)
                        { // over 4 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
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
                                NPC.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                flag7 = true; // end the loop (after testing every lower point :/)
                            }
                        } // END over 4 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            } // END is server & chaos ele & bored
            #endregion
            //-------------------------------------------------------------------
            #region drown // code by Omnir
            if (canDrown)
            {
                if (!NPC.wet)
                {
                    NPC.TargetClosest(true);
                    drownTimer = drownTimerMax;
                }
                if (NPC.wet)
                {
                    drownTimer--;
                }
                if (NPC.wet && drownTimer > drowningRisk)
                {
                    NPC.TargetClosest(true);
                }
                else if (NPC.wet && drownTimer <= drowningRisk)
                {
                    NPC.TargetClosest(false);
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                    }
                    NPC.directionY = -1;
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.direction = 1;
                    }
                    NPC.direction = -1;
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.direction = 1;
                    }
                }
                if (drownTimer <= 0)
                {
                    NPC.life--;
                    if (NPC.life <= 0)
                    {

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
                        NPC.NPCLoot();
                        NPC.netUpdate = true;
                    }
                }
            }
            #endregion
            //-------------------------------------------------------------------*/
        }
        #endregion

        static Texture2D spearTexture;
        static Texture2D bombTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            Player player = Main.player[NPC.target];
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (spearTexture == null) //|| spearTexture.IsDisposed
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyThrowingKnifeSmall");
            }

            if (bombTexture == null)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemySmokebomb");
            }

            //knife
            if (customAi1 >= 120 && customAi1 <= 152)
            {
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.1f);

                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); //facing left, (-22,--) was above NPC head, was 24, 48
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(4, 10), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher, 2nd value is width axis
                }
            }

            //bomb
            if (customAi1 >= 220) //&& npc.Distance(player.Center) > 10 && customAi1 <= 280
            {
                //bomb sprite doesn't always show for some reason
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); //facing left, 
                }
                else
                {
                    spriteBatch.Draw(bombTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(14, 4), NPC.scale, effects, 0); // facing right
                }
            }
        }
        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
            }
            if (!Main.expertMode)
            {
                if (Main.rand.Next(99) < 90) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.MagicBarrier>(), 1);
                if (Main.rand.Next(99) < 6) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.AncientWarhammer>(), 1, false, -1);
                if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
                if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
                if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Humanity>(), 1);
                if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ObsidianSkinPotion, 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 3);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ArcheryPotion, 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, 1 + Main.rand.Next(3));
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 3600, false); //1 minute
            player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1200, false); // loss of flight mobility for 20 seconds, down from 4 minutes moohahaha... cough cough


            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
            }
        }
        #endregion
    }
}