using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Special
{
    class DarkCloudShadow : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }


        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 200;
            NPC.defense = 80;
            NPC.lifeMax = 45000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1500000;
            NPC.rarity = 43;
            NPC.knockBackResist = 0f;
        }

        #region Damage variables
        const float TRAIL_LENGTH = 12;

        public static int meteorDamage = 17;
        public static int deathBallDamage = 75;
        public static int poisonStrikeDamage = 46;
        public static int holdBallDamage = 35;
        public static int dragoonLanceDamage = 68;
        public static int armageddonDamage = 65;
        public static int gravityBallDamage = 35;
        public static int crazedPurpleCrushDamage = 40;
        public static int shadowShotDamage = 40;
        public static int iceStormDamage = 33;
        public static int darkArrowDamage = 45;
        public static int stormWaveDamage = 95;
        #endregion

        #region First Phase Vars
        float comboDamage = 0;
        bool breakCombo = false;
        float customAi1;
        int boredTimer = 0;
        int tBored = 1;//increasing this increases how long it take for the NP to get bored
        int boredResetT = 0;
        int bReset = 50;//increasing this will increase how long an NPC "gives up" before coming back to try again.
        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        #endregion

        //If this is set to anything but -1, the boss will *only* use that attack ID
        readonly int testAttack = -1;
        bool firstPhase = true;
        DarkCloudMove CurrentMove;
        List<DarkCloudMove> ActiveMoveList;
        List<DarkCloudMove> DefaultList;

        public int NextAttackMode
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float AttackModeCounter
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public int AttackModeTally
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {

            Lighting.AddLight(NPC.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(NPC.Center, 64, DustID.ShadowbeamStaff);

            //Force an update 3 times a second. Terraria gets a bit lazy about it, and this consistency is required to prevent rubberbanding on certain high-intensity attacks
            if (Main.GameUpdateCount % 20 == 0)
            {
                NPC.netUpdate = true;
            }


            //If it's the first phase
            FirstPhase();
        }

        //The dust ring particle effect the boss uses
        void DarkCloudParticleEffect(float dustSpeed, float dustAmount = 50, float radius = 64)
        {
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = new Vector2(dustSpeed, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(NPC.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
            }
        }

        //A charging effect that focuses in on dark cloud and grows in intensity as time goes on
        void ChargingParticleEffect(float progress, float maxProgress)
        {
            float count = (progress / maxProgress) * 30;
            DarkCloudParticleEffect(-5, count * 4, 42 - count);
        }

        //Same as above, but mixes in freeze bolt particles
        void IceChargingParticleEffect(float progress, float maxProgress)
        {
            ChargingParticleEffect(progress, maxProgress);

            float count = (progress / maxProgress) * 30;
            for (int i = 0; i < count * 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(35 - count, 35 - count);
                Vector2 velocity = new Vector2(-5, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(NPC.Center + offset, DustID.MagicMirror, velocity, Scale: 2).noGravity = true;
            }
        }

        static Texture2D darkCloudTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkCloud");

        //Useful code from old AI to check if it's on the ground.
        bool OnGround()
        {
            bool standing_on_solid_tile = false;

            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            int x_left_edge = (int)NPC.position.X / 16;
            int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
            for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
            {
                Tile t = Main.tile[l, y_below_feet];
                if (t.HasTile && !t.IsActuated && Main.tileSolid[(int)t.TileType]) // tile exists and is solid
                {
                    standing_on_solid_tile = true;
                    break; // one is enough so stop checking
                }
            } // END traverse blocks under feet
            return standing_on_solid_tile;
        }


        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.DarkCloudBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 2, 4));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GuardianSoul>(), 1, 2, 4));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MoonlightGreatsword>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RadiantStrand>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SunderedMoon>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NullSpriteStaff>()));
            npcLoot.Add(notExpertCondition);
        }

        public override void OnKill()
        {

            Dust.NewDust(NPC.position, NPC.width, NPC.height, 52, 0.3f, 0.3f, 200, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 52, 0.2f, 0.2f, 200, default(Color), 4f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 52, 0.2f, 0.2f, 200, default(Color), 4f);

            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f });
            
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;

            target.AddBuff(BuffID.BrokenArmor, 2 * 60 / expertScale, false);
            target.AddBuff(BuffID.OnFire, 3 * 60 / expertScale, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 60 * 60, false); //defense goes time on every hit

        }
        #endregion

        #region Old AI
        public void FirstPhase()
        {
            #region "Classic" first phase AI // code by GrtAndPwrflTrtl (http://www.terrariaonline.com/members/grtandpwrfltrtl.86018/)


            #region set up NPC's attributes & behaviors
            // set parameters
            //  is_archer OR can_pass_doors OR shoot_and_walk, pick only 1.  They use the same ai[] vars (1&2)
            bool is_archer = false; // stops and shoots when target sighted; skel archer & gob archer are the archers
            bool can_pass_doors = false;  //  can open or break doors; c. bunny, crab, clown, skel archer, gob archer, & chaos elemental cannot
            bool shoot_and_walk = true;  //  can shoot while walking like clown; uses ai[2] so cannot be used with is_archer or can_pass_doors

            //  can_teleport==true code uses boredom_time and ai[3] (boredom), but not mutually exclusive
            bool can_teleport = true;  //  tp around like chaos ele
            int boredom_time = 20; // time until it stops targeting player if blocked etc, 60 for anything but chaos ele, 20 for chaos ele
            int boredom_cooldown = 10 * boredom_time; // boredom level where boredom wears off; usually 10*boredom_time

            bool hates_light = false;  //  flees in daylight like: Zombie, Skeleton, Undead Miner, Doctor Bones, The Groom, Werewolf, Clown, Bald Zombie, Possessed Armor
            bool can_pass_doors_bloodmoon_only = false;  //  can open or break doors, but only during bloodmoon: zombies & bald zombies. Will keep trying anyway.

            float acceleration = .05f;  //  how fast it can speed up
            float top_speed = 3f;  //  max walking speed, also affects jump length
            float braking_power = .2f;  //  %of speed that can be shed every tick when above max walking speed
            double bored_speed = .9;  //  above this speed boredom decreases(if not already bored); usually .9

            float enrage_percentage = .4f;  //  double movement speed below this life fraction. 0 for no enrage. Mummies enrage below .5
            float enrage_acceleration = .10f;  //  faster when enraged, usually 2*acceleration
            float enrage_top_speed = 5;  //  faster when enraged, usually 2*top_speed

            bool clown_sized = false; // is hitbox the same as clowns' for purposes of when to jump?
            bool jump_gaps = true; // attempt to jump gaps; everything but crabs do this

            bool hops = true; // hops when close to target like Angry Bones, Corrupt Bunny, Armored Skeleton, and Werewolf
            float hop_velocity = 1f; // forward velocity needed to initiate hopping; usually 1
            float hop_range_x = 100; // less than this is 'close to target'; usually 100
            float hop_range_y = 50; // less than this is 'close to target'; usually 50
            float hop_power = 4; // how hard/high offensive hops are; usually 4
            float hop_speed = 3; // how fast hops can accelerate vertically; usually 3 (2xSpd is 4 for Hvy Skel & Werewolf so they're noticably capped)

            // is_archer & clown bombs only
            int shot_rate = 70;  //  rate at which archers/bombers fire; 70 for skeleton archer, 180 for goblin archer, 450 for clown; atm must be an even # or won't fire at shot_rate/2
                                 //int fuse_time = 300;  //  fuse time on bombs, 300 for clown bombs
                                 //int projectile_damage = 35;  //  projectile dmg: 35 for Skeleton Archer, 11 for Goblin Archer
            int projectile_id = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellMeteor>(); // projectile id: 82(Flaming Arrow) for Skeleton Archer, 81(Wooden Arrow) for Goblin Archer, 75(Happy Bomb) for Clown
            float projectile_velocity = 11; // initial velocity? 11 for Skeleton Archers, 9 for Goblin Archers, bombs have fixed speed & direction atm

            // can_pass_doors only
            float door_break_pow = 2; // 10 dmg breaks door; 2 for goblin thief and 7 for Angry Bones; 1 for others
            bool breaks_doors = false; // meaningless unless can_pass_doors; if this is true the door breaks down instead of trying to open; Goblin Peon is only warrior to do this

            // Omnirs creature sorts
            //bool tooBig = true; // force bigger creatures to jump
            //bool lavaJumping = true; // Enemies jump on lava.
            bool canDrown = false; // They will drown if in the water for too long
            bool quickBored = true; //Enemy will respond to boredom much faster(? -- test)
            bool oBored = false; //Whether they're bored under the "quickBored" conditions

            // calculated parameters
            bool moonwalking = false;  //  not jump/fall and moving backwards to facing
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
                moonwalking = true;
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
                        expr_19EE_cp_0.velocity.X *= 2f; // faster in x direction
                    }
                    for (int k = 0; k < 20; k++) // more dust effects at old position
                    {
                        int num10 = Dust.NewDust(NPC.oldPos[2], NPC.width, NPC.height, 71, -num6, -num7, 200, default(Color), 2f);
                        Main.dust[num10].noGravity = true;
                        Dust expr_1A6F_cp_0 = Main.dust[num10];
                        expr_1A6F_cp_0.velocity.X *= 2f;
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
                if (!canDrown || (canDrown && !NPC.wet) || (quickBored && boredTimer > tBored))
                {
                    //npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }
            }
            else if (!is_archer || NPC.ai[2] <= 0f) //  fleeing light or bored (& not aiming)
            {
                if (hates_light && Main.dayTime && (double)(NPC.position.Y / 16f) < Main.worldSurface && NPC.timeLeft > 10)
                    //npc.timeLeft = 10;  //  if hates light & in light, hasten despawn

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

            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Black, 1f + comboDamage / 500);
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
                            if (Main.netMode != 1)  //  is server
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), npc_center.X, npc_center.Y, npc_to_target_x, npc_to_target_y, projectile_id, meteorDamage, 0f, Main.myPlayer);

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


            if (!oBored && shoot_and_walk && !Main.player[NPC.target].dead) // can generalize this section to moving+projectile code 
            {
                // Main.netMode != 1 &&

                //if(Main.netMode != 1)
                //{
                if (breakCombo == true || (enraged == true && Main.rand.NextBool(700)) || (enraged == false && Main.rand.NextBool(1700)))
                {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.knockBackResist = 0f;
                    breakCombo = false;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    NPC.damage = 120;
                    NPC.knockBackResist = 0;
                    chargeDamage++;
                }
                if (chargeDamage >= 96)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 95;
                    NPC.knockBackResist = 0.2f;
                    chargeDamage = 0;
                }

                //}
                #endregion

                #region Projectiles
                //if(Main.netMode != 1)
                //{
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (customAi1 >= 10f)
                {
                    if (Main.rand.NextBool(700))
                    {
                        float num48 = 10f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 6;
                                Main.projectile[num54].aiStyle = 1;
                            }                          
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.NextBool(195))
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 700;
                                Main.projectile[num54].aiStyle = 23;
                            }                          
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }


                    if (Main.rand.NextBool(520))
                    {
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                        NPC.ai[1] = 1f;
                        NPC.netUpdate = true;
                    }
                    if (Main.rand.NextBool(340))
                    {
                        float num48 = 18f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 100 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, holdBallDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 105;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.NextBool(120))
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.Center.Y - 10f);
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = (((Main.player[NPC.target].position.Y - 10) + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemyDragoonLance>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, dragoonLanceDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 700;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.NextBool(300))
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 600;
                                Main.projectile[num54].aiStyle = 23;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.NextBool(85))
                    {
                        float num48 = 12f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                //int damage = 80;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 450;
                                Main.projectile[num54].aiStyle = 23;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }


                    if (Main.rand.NextBool(350))
                    {
                        float num48 = 12f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellArmageddonBlastBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, armageddonDamage, 0f, Main.myPlayer);
                                //Main.projectile[num54].timeLeft = 0;
                                Main.projectile[num54].aiStyle = 23;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.NextBool(70))
                    {
                        float num48 = 14f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity1Ball>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, gravityBallDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 40;
                                Main.projectile[num54].aiStyle = 1;
                            }                                
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                    if (Main.rand.NextBool(280))
                    {
                        float num48 = 11f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 270;
                                Main.projectile[num54].aiStyle = 23;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                    if (Main.rand.NextBool(350))
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 1000 + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, crazedPurpleCrushDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 600;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }











                    if (Main.rand.NextBool(526))
                    {
                        float num48 = 7f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, shadowShotDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 200;
                                Main.projectile[num54].aiStyle = 23; //was 23
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }

                    if (Main.rand.NextBool(50))
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();//44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 0;//was 70
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            NPC.ai[1] = 1f;
                        }
                        NPC.netUpdate = true;
                    }





                    if (Main.rand.NextBool(65))
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.EnemyArrowOfDarkCloud>(); //44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, darkArrowDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 1300;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }


                    if (Main.rand.NextBool(555))
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormWave>(); //44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, stormWaveDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 1300;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }



                    if (Main.rand.NextBool(205))
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                num51 = num48 / num51;
                                speedX *= num51;
                                speedY *= num51;
                                int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.EnemyArrowOfDarkCloud>(); //44;//0x37; //14;
                                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, darkArrowDamage, 0f, Main.myPlayer);
                                Main.projectile[num54].timeLeft = 1300;
                                Main.projectile[num54].aiStyle = 1;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                            customAi1 = 1f;
                        }
                        NPC.netUpdate = true;
                    }
                }

                // } //end of MP thing

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
                int tp_radius = 25; // radius around target(upper left corner) in blocks to teleport into
                const float MIN_TELEPORT_DISTANCE = 192f;
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
                    Vector2 teleportPosition = new Vector2(tp_x_target * 16f - NPC.width / 2, tp_y_target * 16f - NPC.height);
                    if (Vector2.Distance(teleportPosition, Main.player[NPC.target].Center) < MIN_TELEPORT_DISTANCE)
                    {
                        continue; 
                    }
                    for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                    { // (tp_x_target,m) is block under its feet I think
                        if ((m < target_y_blockpos - 9 || m > target_y_blockpos + 9 || tp_x_target < target_x_blockpos - 9 || tp_x_target > target_x_blockpos + 6) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].HasTile)
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
                                NPC.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                flag7 = true; // end the loop (after testing every lower point :/)
                                break;
                            }
                        } // END over 6 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            } // END is server & chaos ele & bored
            #endregion
            //-------------------------------------------------------------------

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
        }
        #endregion    

        //This class exists to pair up the Move, Attack, Draw, and ID of each attack type into one nice and neat state object
        class DarkCloudMove
        {
            public Action Move;
            public Action Attack;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public DarkCloudMove(Action MoveAction, Action AttackAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                Attack = AttackAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }
    }
}