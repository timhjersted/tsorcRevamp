using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Slogra : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            NPC.width = 38;
            NPC.height = 32;
            animationType = 28;
            NPC.aiStyle = 3;
            NPC.timeLeft = 22750;
            NPC.damage = 45;
            //npc.music = 12;
            NPC.defense = 10;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = Mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 5000;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.4f;
            NPC.value = 35000;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            bossBag = ModContent.ItemType<Items.BossBags.SlograBag>();
            despawnHandler = new NPCDespawnHandler("Slogra returns to the depths...", Color.DarkGreen, DustID.Demonite);

        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Slogra, Lost Soul of the Depths");
        }

        int tridentDamage = 30;
        //Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
        int burningSphereDamage = 60;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
            tridentDamage = (int)(tridentDamage / 2);
            //For some reason, its contact damage doesn't get doubled due to expert mode either apparently?
            //burningSphereDamage = (int)(burningSphereDamage / 2);
        }


        //int customspawn1 = 0;
        float customAi1;
        //float customAi5;
        int chargeDamage = 0;
        float comboDamage = 0;
        bool breakCombo = false;
        bool chargeDamageFlag = false;
        int boredTimer = 0;
        int tBored = 1;//increasing this increases how long it take for the NP to get bored
        int boredResetT = 0;
        int bReset = 50;//increasing this will increase how long an NPC "gives up" before coming back to try again.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;

            // these are all the regular stuff you get , now lets see......
            //this doesn't really work, only one spawns 90% of the time in an enraged state
            /*for (int num36 = 0; num36 < 200; num36++)
            {
                if (Main.npc[num36].active && Main.npc[num36].type == ModContent.NPCType<Bosses.Slogra>())
                {
                    return 0;
                }
            }

            if (Jungle && !Main.dayTime && !tsorcRevampWorld.SuperHardMode && !Main.hardMode && AboveEarth && Main.rand.Next(6000) == 1)

            {
                Main.NewText("Slogra had emerged from the depths!", 175, 75, 255);
                //NPC.SpawnOnPlayer(P.whoAmI, ModContent.NPCType<NPCs.Bosses.Gaibon>());
                return 1;
            }

            if (Meteor && !Main.hardMode && !tsorcRevampWorld.SuperHardMode && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.Next(3000) == 1)

            {
                Main.NewText("Slogra has emerged from the depths!", 175, 75, 255);
                //NPC.SpawnOnPlayer(P.whoAmI, ModContent.NPCType<NPCs.Bosses.Gaibon>());
                //NPC.NewNPC((int)P.position.X + 400, (int)P.position.Y, ModContent.NPCType<NPCs.Bosses.Gaibon>());
                return 1;
            }*/

            return 0;
        }
        #endregion

        #region AI // code by GrtAndPwrflTrtl (http://www.terrariaonline.com/members/grtandpwrfltrtl.86018/)
        NPCDespawnHandler despawnHandler;
        bool gaibonDead = false;
        double fireballTimer = 0;
        float dustRadius = 20;
        float dustMin = 3;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player thisPlayer = Main.player[i];
                if(thisPlayer != null && thisPlayer.active)
                {
                    thisPlayer.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 300);
                }
            }

            //If super far away from the player, warp to them
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 5000)
            {
                NPC.Center = new Vector2(Main.player[NPC.target].Center.X, Main.player[NPC.target].Center.Y - 500);
            }

            //If gaibon is dead, we don't need to keep calling AnyNPCs.
            if (!gaibonDead)
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Gaibon>()))
                {
                    gaibonDead = true;
                }
            }
            else
            {
                if(dustRadius > dustMin)
                {
                    dustRadius -= 0.25f;
                }

                int dustPerTick = 20;
                float speed = 2;
                for (int i = 0; i < dustPerTick; i++)
                {
                    Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
                    Vector2 dustPos = NPC.Center + dir * dustRadius * 16;
                    Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 2) * speed;
                    Dust dustID = Dust.NewDustPerfect(dustPos, 173, dustVel, 200);
                    dustID.noGravity = true;
                }                
                
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    fireballTimer += (Main.rand.Next(2, 5) * 0.1f) * 1.1;
                    if (fireballTimer >= 10f)
                    {
                        if (Main.rand.Next(45) == 1)
                        {
                            Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
                            Vector2 projPos = NPC.Center + dir * dustRadius * 16;
                            int spawned = NPC.NewNPC((int)projPos.X, (int)projPos.Y, NPCID.BurningSphere, 0);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GaibonSpit2"), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2));
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                            //npc.netUpdate=true;
                        }
                    }
                }
            }

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

            int sound_type = 26; // Parameter for Terraria.Audio.SoundEngine.PlaySound().  14 for Zombie, Skeleton, Angry Bones, Heavy Skeleton, Skeleton Archer, Bald Zombie.  26 for Mummy, Light & Dark Mummy. 0 means no sounds
            int sound_frequency = 2000;  //  chance to play sound every frame, 1000 for zombie/skel, 500 for mummies

            float acceleration = .09f;  //  how fast it can speed up
            float top_speed = 2f;  //  max walking speed, also affects jump length
            float braking_power = .2f;  //  %of speed that can be shed every tick when above max walking speed
            double bored_speed = .9;  //  above this speed boredom decreases(if not already bored); usually .9

            float enrage_percentage = .5f;  //  double movement speed below this life fraction. 0 for no enrage. Mummies enrage below .5
            float enrage_acceleration = .14f;  //  faster when enraged, usually 2*acceleration
            float enrage_top_speed = 3.5f;  //  faster when enraged, usually 2*top_speed

            bool clown_sized = false; // is hitbox the same as clowns' for purposes of when to jump?
            bool jump_gaps = true; // attempt to jump gaps; everything but crabs do this

            bool hops = true; // hops when close to target like Angry Bones, Corrupt Bunny, Armored Skeleton, and Werewolf
            float hop_velocity = 2f; // forward velocity needed to initiate hopping; usually 1
            float hop_range_x = 100; // less than this is 'close to target'; usually 100
            float hop_range_y = 50; // less than this is 'close to target'; usually 50
            float hop_power = 4; // how hard/high offensive hops are; usually 4
            float hop_speed = 4; // how fast hops can accelerate vertically; usually 3 (2xSpd is 4 for Hvy Skel & Werewolf so they're noticably capped)

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
            bool tooBig = true; // force bigger creatures to jump
            bool lavaJumping = true; // Enemies jump on lava.
            //bool canDrown = false; // They will drown if in the water for too long
            bool quickBored = false; //Enemy will respond to boredom much faster(? -- test)
            bool oBored = false; //Whether they're bored under the "quickBored" conditions

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
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 8);
                    Vector2 vector = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f); // current location
                    float num6 = NPC.oldPos[2].X + (float)NPC.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
                    float num7 = NPC.oldPos[2].Y + (float)NPC.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
                    float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
                    num8 = 2f / num8; // to normalize to 2 unit long vector
                    num6 *= num8; // direction to where it was 3 frames ago, vector normalized
                    num7 *= num8; // direction to where it was 3 frames ago, vector normalized
                    for (int j = 0; j < 20; j++) // make 20 dusts at current position
                    {
                        int num9 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.UndergroundHallowedEnemies, num6, num7, 200, default(Color), 2f);
                        Main.dust[num9].noGravity = true; // floating
                        Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
                        expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
                    }
                    for (int k = 0; k < 20; k++) // more dust effects at old position
                    {
                        int num10 = Dust.NewDust(NPC.oldPos[2], NPC.width, NPC.height, DustID.UndergroundHallowedEnemies, -num6, -num7, 200, default(Color), 2f);
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
            {  // not fleeing light & not bored
                if (sound_type > 0 && Main.rand.Next(sound_frequency) <= 0)
                    Terraria.Audio.SoundEngine.PlaySound(sound_type, (int)NPC.position.X, (int)NPC.position.Y, 1); // random creature sounds
            }
            else if (!is_archer || NPC.ai[2] <= 0f) //  fleeing light or bored (& not aiming)
            {               

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
                //npc.knockBackResist=0.1f;
            }
            #endregion
            //-------------------------------------------------------------------
            #region melee movement

            //int dust = Dust.NewDust(new Vector2((float) npc.position.X, (float) npc.position.Y), npc.width, npc.height, 6, npc.velocity.X-6f, npc.velocity.Y, 150, Color.Red, 1f);
            //				Main.dust[dust].noGravity = true;
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Vile, NPC.velocity.X, NPC.velocity.Y, 200, color, 1f + comboDamage / 30);
            Main.dust[dust].noGravity = true;


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
                        //npc.TargetClosest(true); // target and face closest player
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
                            if (Main.netMode != NetmodeID.MultiplayerClient)  //  is server
                                Projectile.NewProjectile(npc_center.X, npc_center.Y, npc_to_target_x, npc_to_target_y, projectile_id, projectile_damage, 0f, Main.myPlayer);

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
            if (!oBored && shoot_and_walk && !Main.player[NPC.target].dead) // can generalize this section to moving+projectile code 
            {
                if (comboDamage > 0)
                {
                    comboDamage -= 0.4f;
                    NPC.netUpdate = true; //new
                }

                //if (customspawn1 == 0)
                //{
                //if ((customspawn1 < 1) && (comboDamage > 1) && Main.rand.Next(2)==1)
                //	{
                //		int Spawned = NPC.NewNPC((int) npc.position.X+(npc.width/2), (int) npc.position.Y+(npc.height/2), "Gaibon", 0);
                //		Main.npc[Spawned].velocity.Y = -8;
                //		Main.npc[Spawned].velocity.X = Main.rand.Next(-10,10)/10;
                //		customspawn1 += 1;

                //		if (Main.netMode != 1)
                //		{
                //			NetMessage.SendData(23, -1, -1, "", Spawned, 0f, 0f, 0f, 0);
                //		}
                //		npc.netUpdate=true; //new
                //	}

                //}

                if (breakCombo == true || (enraged == true && Main.rand.Next(450) == 1))
                {
                    chargeDamageFlag = true;
                    NPC.knockBackResist = 0f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 13) * -1; //12 was 10
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;

                    breakCombo = false;
                    NPC.netUpdate = true;

                }
                if (chargeDamageFlag == true)
                {
                    NPC.damage = 46;
                    NPC.knockBackResist = 0f;
                    chargeDamage++;
                }
                if (chargeDamage >= 50) //was 45
                {
                    chargeDamageFlag = false;
                    //npc.dontTakeDamage = false;
                    NPC.damage = 40;
                    chargeDamage = 0;

                    NPC.knockBackResist = 0.3f;
                }

                #region Projectiles
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (customAi1 >= 10f)
                {
                    if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                    {
                        if (Main.rand.Next(200) == 1)
                        {
                            float num48 = 10f;
                            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                            float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                            float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                            if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, tridentDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 600;
                                Main.projectile[num54].aiStyle = 1;
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
                                customAi1 = 1f;
                            }
                            NPC.netUpdate = true;
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
                    Main.tile[x_in_front, y_above_feet] = new Tile();

                if (Main.tile[x_in_front, y_above_feet - 1] == null)
                    Main.tile[x_in_front, y_above_feet - 1] = new Tile();

                if (Main.tile[x_in_front, y_above_feet - 2] == null)
                    Main.tile[x_in_front, y_above_feet - 2] = new Tile();

                if (Main.tile[x_in_front, y_above_feet - 3] == null)
                    Main.tile[x_in_front, y_above_feet - 3] = new Tile();

                if (Main.tile[x_in_front, y_above_feet + 1] == null)
                    Main.tile[x_in_front, y_above_feet + 1] = new Tile();
                //  create? 2 other tiles farther in front
                if (Main.tile[x_in_front + NPC.direction, y_above_feet - 1] == null)
                    Main.tile[x_in_front + NPC.direction, y_above_feet - 1] = new Tile();

                if (Main.tile[x_in_front + NPC.direction, y_above_feet + 1] == null)
                    Main.tile[x_in_front + NPC.direction, y_above_feet + 1] = new Tile();

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
                        if (door_breaking && Main.netMode != NetmodeID.MultiplayerClient)  //  server and door breaking
                        {
                            if (breaks_doors)  //  breaks doors rather than attempt to open
                            {
                                WorldGen.KillTile(x_in_front, y_above_feet - 1, false, false, false);  //  kill door
                                if (Main.netMode == NetmodeID.Server) // server
                                    NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, (float)x_in_front, (float)(y_above_feet - 1), 0f, 0); // ?? tile breaking and/or item drop probably
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
                                if (Main.netMode == NetmodeID.Server && door_opened) // is server & door was just opened
                                    NetMessage.SendData(MessageID.ChangeDoor, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)NPC.direction, 0); // ??
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
                                NPC.velocity.Y = -9f; // jump with power 8 (for 4 block steps)
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                NPC.velocity.Y = -8f; // jump with power 7 (for 3 block steps)
                                NPC.netUpdate = true;
                            }
                        } // for everything else, head height clear:
                        else if (Main.tile[x_in_front, y_above_feet - 1].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].TileType])
                        { // 2 blocks above ground level(mid body height) blocked
                            NPC.velocity.Y = -7f; // jump with power 6 (for 2 block steps)
                            NPC.netUpdate = true;
                        }
                        else if (Main.tile[x_in_front, y_above_feet].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].TileType])
                        { // 1 block above ground level(foot height) blocked
                            NPC.velocity.Y = -7f; // jump with power 5 (for 1 block steps)
                            NPC.netUpdate = true;
                        }
                        else if (NPC.directionY < 0 && jump_gaps && (!Main.tile[x_in_front, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                        { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                            NPC.velocity.Y = -9f; // jump with power 8
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
            if (Main.netMode != NetmodeID.MultiplayerClient && can_teleport && NPC.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
            {
                int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                int x_blockpos = (int)NPC.position.X / 16; // corner not center
                int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                int tp_radius = 20; // radius around target(upper left corner) in blocks to teleport into
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
                        if ((m < target_y_blockpos - 12 || m > target_y_blockpos + 12 || tp_x_target < target_x_blockpos - 12 || tp_x_target > target_x_blockpos + 12) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].HasTile)
                        { // over 12 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                            bool safe_to_stand = true;
                            bool dark_caster = false; // not a fighter type AI...
                            if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                safe_to_stand = false;
                            else if (Main.tile[tp_x_target, m - 1].LiquidType) // feet submerged in lava
                                safe_to_stand = false;

                            if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].TileType] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                            { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                                NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet
                                NPC.netUpdate = true;
                                NPC.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                flag7 = true; // end the loop (after testing every lower point :/)
                            }
                        } // END over 6 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            } // END is server & chaos ele & bored
            #endregion
            //-------------------------------------------------------------------*/
            #region New Boredom by Omnir
            if (quickBored)
            {
                if (!oBored)
                {
                    if (NPC.velocity.X == 0f)
                    {
                        boredTimer++;
                        if (boredTimer > tBored)
                        {
                            boredResetT = 0;                            
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
                            oBored = true;
                        }
                    }
                }
                if (oBored)
                {
                    boredResetT++;
                    if (boredResetT > bReset)
                    {
                        boredTimer = 0;
                        oBored = false;
                    }
                }
            }
            #endregion
        }
        #endregion

        #region damage counter then lunge
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
            {
            comboDamage += (float)damage;
            if (comboDamage > 90)
            {
                breakCombo = true;
                NPC.netUpdate = true; //new
                Color color = new Color();
                for (int num36 = 0; num36 < 50; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.t_Slime, 0, 0, 100, color, 2f);
                }
                for (int num36 = 0; num36 < 20; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Vile, 0, 0, 100, color, 2f);
                }
                //npc.ai[1] = -200;
                comboDamage = 0;
                NPC.netUpdate = true; //new
            }
            return true;
            //if (!npc.justHit)
            //{
            //comboDamage --;

            //	if (comboDamage < 0)
            //	{
            //	comboDamage = 0;
            //	}
            //}
        }
        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Slogra Gore 1"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Slogra Gore 2"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Slogra Gore 3"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Slogra Gore 2"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Slogra Gore 3"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
            if (!NPC.AnyNPCs(ModContent.NPCType<Gaibon>()))
            {
                if (Main.expertMode)
                {
                    NPC.DropBossBags();
                }
                else
                {
                    Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
                    Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);
                    Item.NewItem(NPC.getRect(), ModContent.ItemType<DarkSoul>(), 700);
                }
            }
            else
            {
                int slograID = NPC.FindFirstNPC(ModContent.NPCType<Gaibon>());
                int speed = 30;
                for (int i = 0; i < 200; i++)
                {
                    Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
                    Vector2 dustPos = NPC.Center + dir * 3 * 16;
                    float distanceFactor = Vector2.Distance(NPC.position, Main.npc[slograID].position) / speed;
                    Vector2 speedRand = Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * 10;
                    float speedX = (((Main.npc[slograID].position.X + (Main.npc[slograID].width * 0.5f)) - NPC.position.X) / distanceFactor) + speedRand.X;
                    float speedY = (((Main.npc[slograID].position.Y + (Main.npc[slograID].height * 0.5f)) - NPC.position.Y) / distanceFactor) + speedRand.Y;
                    Vector2 dustSpeed = new Vector2(speedX, speedY);
                    Dust dustObj = Dust.NewDustPerfect(dustPos, 262, dustSpeed, 200, default, 3);
                    dustObj.noGravity = true;
                }
            }
        }
    }
}