using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class DarkCloud : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[npc.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[npc.type] = 1;
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 200;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.aiStyle = 3;
            npc.height = 40;
            npc.width = 20;
            music = 12;
            npc.damage = 200;
            npc.defense = 160;
            npc.lifeMax = 300000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 1500000;
            npc.knockBackResist = 0f;
            npc.boss = true;
            bossBag = ModContent.ItemType<Items.BossBags.DarkCloudBag>();
            despawnHandler = new NPCDespawnHandler("You are subsumed by your shadow...", Color.Blue, DustID.ShadowbeamStaff);
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

        public static int divineSparkDamage = 75;
        public static int darkFlowDamage = 50;
        public static int antiMatDamage = 100;
        public static int darkSlashDamage = 25; //This one gets x16'd
        public static int swordDamage = 50;
        public static int freezeBoltDamage = 60;
        public static int confinedBlastDamage = 200; //Very high because it isn't compensating for doubling/quadrupling, and is very easy to dodge
        public static int arrowRainDamage = 50;
        #endregion

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = 300000;
            npc.damage = 200;
        }

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

        float customspawn1;
        float customspawn2;
        float customspawn3;
        #endregion

        //If this is set to anything but -1, the boss will *only* use that attack ID
        readonly int testAttack = -1;
        bool firstPhase = true;
        bool changingPhases = false;
        bool setup = false;

        //The next warp point in the current attack. It gets calculated before it's used so it has time to get synced first
        Vector2 nextWarpPoint;

        //The first warp point of the *next* attack. It is only used once per attack, at the start. Whenever it's used, a new one is calculated immediately to give it time to sync.
        Vector2 preSelectedWarpPoint;

        float phaseChangeCounter = 0;
        DarkCloudMove CurrentMove;
        List<DarkCloudMove> ActiveMoveList;
        List<DarkCloudMove> DefaultList;

        public int NextAttackMode
        {
            get => (int)npc.ai[0];
            set => npc.ai[0] = value;
        }
        public float AttackModeCounter
        {
            get => npc.ai[1];
            set => npc.ai[1] = value;
        }
        public float NextConfinedBlastsAngle
        {
            get => npc.ai[2];
            set => npc.ai[2] = value;
        }
        public int AttackModeTally
        {
            get => (int)npc.ai[3];
            set => npc.ai[3] = value;
        }
        public Player Target
        {
            get => Main.player[npc.target];
        }        

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            //If we're about to despawn, and it's not first phase, then clean up by deactivating the pyramid and clearing any targeting lasers
            if (despawnHandler.TargetAndDespawn(npc.whoAmI) && !firstPhase)
            {
                ActuatePyramid();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<GenericLaser>())
                    {
                        Main.projectile[i].Kill();
                    }
                }
            }

            Lighting.AddLight(npc.Center, Color.Blue.ToVector3());
            UsefulFunctions.DustRing(npc.Center, 64, DustID.ShadowbeamStaff);

            //Force an update once a second. Terraria gets a bit lazy about it, and this consistency is required to prevent rubberbanding on certain high-intensity attacks
            if (Main.time % 20 == 0)
            {
                npc.netUpdate = true;
            }

            if (!setup)
            {                
                InitializeMoves();                
                NextAttackMode = ActiveMoveList[Main.rand.Next(ActiveMoveList.Count - 1)].ID;
                setup = true;
            }
            //If it's the first phase
            if (firstPhase)
            {
                //Check if it's either low on health or has already begun the phase change process
                if (changingPhases || ((npc.life < (9 * npc.lifeMax / 10))))
                {
                    ChangePhases();
                }
                //If not, then proceed to its 'classic' first phase
                else
                {
                    FirstPhase();
                }
            }

            //If it's not in the first phase, move according to the pattern of the current attack. If it's not a multiplayer client, then also run its attacks.
            //These are split up to keep the code readable.
            else
            {
                CurrentMove.Move();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {                   
                    CurrentMove.Attack();
                }
                AttackModeCounter++;
            }

            if(AttackModeCounter == 5)
            {
                PrecalculateFirstTeleport();
            }
        }        

        //Randomly pick a new unused attack and reset attack variables
        void ChangeAttacks()
        {           
            if (testAttack == -1)
            {                
                for(int i = 0; i < ActiveMoveList.Count; i++)
                {
                    if(ActiveMoveList[i].ID == NextAttackMode)
                    {
                        //Set the current move using the previous, stored attack mode now that it's had time to sync
                        CurrentMove = ActiveMoveList[i];

                        //Remove the chosen attack from the list so it can't be picked again until all other attacks are used up
                        ActiveMoveList.RemoveAt(i);
                    }
                }
                //If there's no moves left in the list, refill it   
                if (ActiveMoveList.Count == 0)
                {      
                    InitializeMoves();
                }                

                //Pick the next attack mode from the ones that remain, and store it in ai[0] (NextAttackMode) so it can sync
                NextAttackMode = ActiveMoveList[Main.rand.Next(ActiveMoveList.Count - 1)].ID;
            }
            else
            {
                CurrentMove = ActiveMoveList[testAttack];
                NextAttackMode = testAttack;
            }

            //Reset variables
            npc.velocity = Vector2.Zero;
            AttackModeCounter = -1;
            AttackModeTally = 0;
            nextWarpPoint = Vector2.Zero;            
            InstantNetUpdate();
        }

        //int NextWarpEntropy;
        //bool sendEntropy = true;
        public override void SendExtraAI(BinaryWriter writer)
        {
            //Send the list of remaining moves
            writer.Write(ActiveMoveList.Count);
            for (int i = 0; i < ActiveMoveList.Count; i++)
            {
                writer.Write(ActiveMoveList[i].ID);
            }

            //A seed value that clients can use whenever they'd like to pick the next attack.
            //Would allow all clients to "randomly" roll the same attack right when it happens, instead of needing to do it early.
            //writer.Write(sendEntropy);
            //if (sendEntropy)
           // {
           //     NextWarpEntropy = Main.rand.Next();
           //     writer.Write(NextWarpEntropy);
           // }

            //Send the next point to teleport to during this attack, and the first point for the next attack
            writer.WriteVector2(nextWarpPoint);
            writer.WriteVector2(preSelectedWarpPoint);
;

            if (CurrentMove == null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(CurrentMove.ID);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            //Recieve the list of remaining moves
            int moveCount = reader.ReadInt32();
            List<int> validMoves = new List<int>();
            for (int i = 0; i < moveCount; i++)
            {
                int move = reader.ReadInt32();
                validMoves.Add(move);
            }
            InitializeMoves(validMoves);

            //bool recievedEntropy = reader.ReadBoolean();
            //if (recievedEntropy)
            //{
            //    //A seed value that clients can use whenever they'd like to pick the next attack.
            //    NextWarpEntropy = reader.ReadInt32();
            //}

            //Recieve the next point to teleport to during this attack, and the first point for the next attack
            nextWarpPoint = reader.ReadVector2();
            preSelectedWarpPoint = reader.ReadVector2();


            int readMoveID = reader.ReadInt32();
            if (readMoveID != -1)
            {
                for (int i = 0; i < DefaultList.Count; i++)
                {
                    if (DefaultList[i].ID == readMoveID)
                    {
                        CurrentMove = DefaultList[i];
                    }
                }
            }            
        }

        //These describe how the boss should move, and other things that should be done on the server and every client to keep it deterministic
        #region Movements

        //A few moves use teleports that need to be calculated in advance so their first warp can be pre-synced. That's done here.
        void PrecalculateFirstTeleport()
        {
            if(NextAttackMode == DarkCloudAttackID.DivineSpark)
            {
                preSelectedWarpPoint = DivineSparkTeleport();
            }
            if (NextAttackMode == DarkCloudAttackID.ArrowRain)
            {
                preSelectedWarpPoint = ArrowRainTeleport();
            }
            if (NextAttackMode == DarkCloudAttackID.AntiMat)
            {
                preSelectedWarpPoint = Main.rand.NextVector2CircularEdge(700, 700);
            }
            if (NextAttackMode == DarkCloudAttackID.TeleportingSlashes)
            {
                preSelectedWarpPoint = Target.Center + Main.rand.NextVector2CircularEdge(slashesWarpRadius, slashesWarpRadius);
            }
            InstantNetUpdate();
        }

        void DragoonLanceMove()
        {
            npc.position.Y = Main.player[npc.target].position.Y + 400;
            if (AttackModeCounter == 0)
            {
                DarkCloudParticleEffect(-2);
                npc.position = Main.player[npc.target].position + (new Vector2(-800, 400));
                DarkCloudParticleEffect(6);
            }
            if (AttackModeCounter <= 60)
            {
                DarkCloudParticleEffect(-2, 8 * (AttackModeCounter / 60));
            }
            if (AttackModeCounter == 60)
            {
                //Burst of particles
                DarkCloudParticleEffect(18, 30);
            }
            if (AttackModeCounter >= 60 && AttackModeCounter < 180)
            {
                npc.velocity = new Vector2(17, 0);
            }
            if (AttackModeCounter == 180)
            {
                DarkCloudParticleEffect(-2);
                npc.position = Main.player[npc.target].position + (new Vector2(800, 400));
                DarkCloudParticleEffect(6);
            }
            if (AttackModeCounter >= 180 && AttackModeCounter < 300)
            {
                npc.velocity = new Vector2(-17, 0);
            }
            if (AttackModeCounter == 300)
            {
                DarkCloudParticleEffect(-2);
                npc.position = Main.player[npc.target].position + (new Vector2(-800, 400));
                DarkCloudParticleEffect(6);
            }
            if (AttackModeCounter >= 300 && AttackModeCounter < 420)
            {
                npc.velocity = new Vector2(17, 0);
            }
            if (AttackModeCounter == 420)
            {
                ChangeAttacks();
            }
        }

        float initialTargetRotation;
        bool counterClockwise = false;
        void DivineSparkMove()
        {
            if (AttackModeCounter % 90 == 0)
            {
                DarkCloudParticleEffect(-2);
                if (AttackModeCounter > 70)
                {
                    npc.Center = Target.Center + nextWarpPoint;
                }
                else
                {
                    npc.Center = Target.Center + preSelectedWarpPoint;
                }

                nextWarpPoint = DivineSparkTeleport();
                DarkCloudParticleEffect(6);
            }

            if (AttackModeCounter % 90 <= 15)
            {
                initialTargetRotation = (Target.Center - npc.Center).ToRotation();
                if (npc.Center.Y > Target.Center.Y)
                {
                    if (npc.Center.X < Target.Center.X)
                    {
                        initialTargetRotation += MathHelper.ToRadians(60);
                        counterClockwise = true;
                    }
                    else
                    {
                        initialTargetRotation -= MathHelper.ToRadians(60);
                        counterClockwise = false;
                    }
                }
                else
                {
                    if (npc.Center.X < Target.Center.X)
                    {
                        initialTargetRotation -= MathHelper.ToRadians(60);
                        counterClockwise = false;
                    }
                    else
                    {
                        initialTargetRotation += MathHelper.ToRadians(60);
                        counterClockwise = true;
                    }
                }
            }

            //Temp laser handling code, aka hell, begins here:


            //If we're not a multiplayer client, spawn the lasers at the proper times.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Spawn the targeting lasers one by one
                if ((AttackModeCounter % 90) % 10 == 0 && AttackModeCounter % 90 < 50)
                {
                    Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<GenericLaser>(), divineSparkDamage, 0.5f, Main.myPlayer, (float)GenericLaser.GenericLaserID.DarkDivineSparkTargeting, npc.whoAmI);
                }

                //Spawn the big laser
                if (AttackModeCounter % 90 == 55)
                {
                    Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<GenericLaser>(), divineSparkDamage, 0.5f, Main.myPlayer, (float)GenericLaser.GenericLaserID.DarkDivineSpark, npc.whoAmI);
                }
            }


            //This part of the code initializes and manages the lasers.
            //It HAS to run both client side and server side, hence why this code is all in Move instead of Attack.
            //1) Get all lasers with that ID. The GetLasersByID returns all lasers with that ID which are active.
            List<GenericLaser> laserList = GenericLaser.GetLasersByID(GenericLaser.GenericLaserID.DarkDivineSpark, npc.whoAmI);
            if (laserList.Count > 0)
            {
                //2) Store it in an object. There can only be one here, so as soon as one exists grab it.
                GenericLaser DarkDivineSparkBeam = laserList[0];


                //3) Check if it's been initialized on this client already, and if not then set all its values.
                if (!DarkDivineSparkBeam.initialized)
                {
                    DarkDivineSparkBeam.LaserOrigin = npc.Center;
                    DarkDivineSparkBeam.LaserTarget = npc.Center + new Vector2(1, 0).RotatedBy(initialTargetRotation);
                    DarkDivineSparkBeam.TelegraphTime = 0;
                    DarkDivineSparkBeam.LaserLength = 8000;
                    DarkDivineSparkBeam.LaserTexture = TransparentTextureHandler.TransparentTextureType.DarkDivineSpark;
                    DarkDivineSparkBeam.TileCollide = false;
                    DarkDivineSparkBeam.CastLight = true;
                    DarkDivineSparkBeam.LaserDust = 234;
                    DarkDivineSparkBeam.lightColor = Color.Indigo;
                    DarkDivineSparkBeam.MaxCharge = 0; //It fires instantly upon creation
                    DarkDivineSparkBeam.FiringDuration = 35;
                    DarkDivineSparkBeam.LaserSize = 3.5f;
                    DarkDivineSparkBeam.LaserTextureBody = new Rectangle(0, 24, 26, 30);
                    DarkDivineSparkBeam.LaserTextureHead = new Rectangle(0, 0, 26, 22);
                    DarkDivineSparkBeam.LaserTextureTail = new Rectangle(0, 56, 26, 22);
                    DarkDivineSparkBeam.LaserDust = 45;
                    DarkDivineSparkBeam.LineDust = true;
                    DarkDivineSparkBeam.frameCount = 15;
                    DarkDivineSparkBeam.LaserVolume = 0;
                    DarkDivineSparkBeam.LaserName = "Dark Divine Spark";
                    DarkDivineSparkBeam.initialized = true;
                }

                //4) Either way this laser is a moving one, so move its current target according to the formula and play its unique sound.
                if (counterClockwise)
                {
                    DarkDivineSparkBeam.LaserTarget = npc.Center + new Vector2(1, 0).RotatedBy(initialTargetRotation - MathHelper.ToRadians(4 * (int)((AttackModeCounter % 90) - 60)));
                }
                else
                {
                    DarkDivineSparkBeam.LaserTarget = npc.Center + new Vector2(1, 0).RotatedBy(initialTargetRotation + MathHelper.ToRadians(4 * (int)((AttackModeCounter % 90) - 60)));
                }
                if (Main.time % 8 == 0)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/MasterBuster"));
                }
            }

            //Do the same stuff again, but for each of the targeting lasers.
            //They don't move, so once they're initialized we never need to mess with them again.
            laserList = GenericLaser.GetLasersByID(GenericLaser.GenericLaserID.DarkDivineSparkTargeting, npc.whoAmI);
            for (int i = 0; i < laserList.Count; i++)
            {
                if(!laserList[i].initialized)
                {
                    laserList[i].LaserOrigin = npc.Center;
                    laserList[i].TelegraphTime = 99999;
                    laserList[i].LaserLength = 8000;
                    laserList[i].LaserColor = Color.Blue * 0.8f;
                    laserList[i].TileCollide = false;
                    laserList[i].CastLight = true;
                    laserList[i].LaserDust = 234;
                    laserList[i].MaxCharge = 0; //It will never fire, and is purely for telegraphing Dark Cloud's shot
                    laserList[i].FiringDuration = (int)(60 - (AttackModeCounter % 90));
                    laserList[i].LaserVolume = 0;
                    laserList[i].TargetingMode = 1;
                    laserList[i].initialized = true;
                    
                    if (counterClockwise)
                    {
                        laserList[i].LaserTarget = npc.Center + new Vector2(1, 0).RotatedBy(initialTargetRotation - MathHelper.ToRadians(30 * i));
                    }
                    else
                    {
                        laserList[i].LaserTarget = npc.Center + new Vector2(1, 0).RotatedBy(initialTargetRotation + MathHelper.ToRadians(30 * i));
                    }
                }                
            }

            if (AttackModeCounter == 450)
            {
                ChangeAttacks();
            }
        }

        Vector2 DivineSparkTeleport()
        {
            //This ensures the boss will not appear deep in a wall, making it impossible to dodge the laser
            Vector2 warp;
            bool valid;
            int triesLeft = 150;
            do
            {
                valid = false;
                float angle = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
                if (Main.rand.NextBool())
                {
                    angle += MathHelper.PiOver2;
                }
                else
                {
                    angle -= MathHelper.PiOver2;
                }
                warp = new Vector2(400, 0).RotatedBy(angle);

                if (Collision.CanHit(Target.Center + warp, 1, 1, Target.Center, 1, 1) || Collision.CanHitLine(Target.Center + warp, 1, 1, Target.Center, 1, 1))
                {
                    valid = true;
                }

                triesLeft--;
                //Retry at maximum 150 times, if no 'fair' spot exists then ignore the rule and continue
                if (triesLeft == 0)
                {
                    break;
                }
            } while (!valid);

            return warp;
        }

        List<Player> targetPlayers;
        readonly float pullSpeed = 0.3f;
        void DarkFlowMove()
        {
            //Messy and bad, could be cleaned up a lot, but it works
            //At the start of the attack, teleport to the arena center, and make a list of every player within 2000 units (the attack's range)
            if (targetPlayers == null || AttackModeCounter == 0)
            {
                TeleportToArenaCenter();
                targetPlayers = new List<Player>();
                for(int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Vector2.Distance(Main.player[i].Center, npc.Center) < 5000)
                    {
                        targetPlayers.Add(Main.player[i]);
                    }
                }
            }
            //Make sure it stays still
            npc.velocity = Vector2.Zero;
            //Draw some dust
            DarkCloudParticleEffect(-10 * (AttackModeCounter / 300), 50);

            NukeGrapples();

            //Spawn a ring of dust at the hard pull radius
            UsefulFunctions.DustRing(npc.Center, 1900, 10);

            //For the first 5 seconds of the attack, pull the player in with strength that increases over time
            //After 5 seconds, do basically the same thing but with constant strength
            if (AttackModeCounter < 300)
            {
                //Spawn dusts indicating the damage radius of the attack
                for (int j = 0; j < 20; j++)
                {

                    Vector2 dir = Main.rand.NextVector2CircularEdge(220 * (AttackModeCounter / 300), 220 * (AttackModeCounter / 300));
                    Vector2 dustPos = npc.Center + dir;

                    Vector2 dustVel = new Vector2(2, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                    Dust dustID = Dust.NewDustPerfect(dustPos, DustID.ShadowbeamStaff, dustVel, 200);
                    dustID.noGravity = true;

                }
                //For each player in the list, check if they're out of the attack range. If so, pull them back into it hard.
                //Otherwise, pull them in normally
                float distance;
                foreach (Player p in targetPlayers)
                {
                    distance = Vector2.Distance(p.Center, npc.Center);
                    if (distance < 1900)
                    {
                        p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed * (AttackModeCounter / 300));
                    }
                    else
                    {
                        p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed * 10 * (AttackModeCounter / 300));
                    }
                    //If they're within the dust ring in the center, then damage them rapidly. Calculate the damage such that it increases to counter player defense or damage reduction.
                    if (distance < 220 * (AttackModeCounter / 300))
                    {
                        float damage = 99;
                        damage /= (1 - p.endurance);
                        if (Main.expertMode)
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.75);
                        }
                        else
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.5);
                        }
                        //player.Hurt() lets you cause damage whenever and however you'd like
                        //This lets us bypass the fact that all hitboxes are square, and simply cause damage if the player is within a the dust ring radius
                        //https://en.wikipedia.org/wiki/Spaghettification
                        p.immuneTime = 0;
                        p.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(p.name + " was spaghettified."), (int)damage, 1);
                    }
                }
            }
            //Else, do all the same stuff but ignore attackTimer and use a fixed strength instead.
            //This probably could have been simplified to avoid writing it twice, but...
            else
            {
                for (int j = 0; j < 20; j++)
                {

                    Vector2 dir = Main.rand.NextVector2CircularEdge(220, 220);
                    Vector2 dustPos = npc.Center + dir;

                    Vector2 dustVel = new Vector2(2, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                    Dust dustID = Dust.NewDustPerfect(dustPos, DustID.ShadowbeamStaff, dustVel, 200);
                    dustID.noGravity = true;

                }
                DarkCloudParticleEffect(10 * (AttackModeCounter / 300), 50);
                float distance;
                foreach (Player p in targetPlayers)
                {                    
                    distance = Vector2.Distance(p.Center, npc.Center);
                    if (distance < 1900)
                    {
                        p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed);
                    }
                    else
                    {
                        p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed * 10);
                    }
                    p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed); 
                    if (Vector2.Distance(p.Center, npc.Center) < 220)
                    {
                        float damage = 99;
                        damage *= (1 - p.endurance);
                        if (Main.expertMode)
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.75f);
                        }
                        else
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.5f);
                        }

                        p.immuneTime = 0;
                        p.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(p.name + " was spaghettified."), (int)damage, 1);
                    }
                }
            }
            //At the end of the attack, change attacks and spawn a burst of dust
            if (AttackModeCounter >= 900)
            {
                for (int i = 0; i < 120; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(256, 256);
                    Vector2 velocity = new Vector2(15, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(npc.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
            }
            if (AttackModeCounter == 910)
            {
                ChangeAttacks(); 
            }
        }

        void DivineSparkThirdsMove()
        {
            //Scrapped for now, would require a lot of effort to make it interesting to dodge and to get the visuals passable. Might revisit later.
            if (AttackModeCounter == 0)
            {
                TeleportToArenaCenter();
            }
            //Split the screen up into thirds, centered on dark cloud.
            //Telegraph the order in which each third will be attacked somehow
            //Then fire the divine spark across each, one at a time in rapid sequence
            //Must be dodged by waiting near an edge, then dashing from one third to another between shots
            if (AttackModeCounter == 300)
            {
                ChangeAttacks();
            }
        }

        //Melee attack. The main, big one.
        //This attack is an enormous mess ngl lol. I tried to use a list of timings for the sub-attacks specifying what should happen when, which was a mistake.
        //Should have really used states instead (like I did for the main attacks), but I realized that too late.
        //May re-do it at some point anyway to allow the sub-attacks happen in a random order, but on the other hand the fact it's choreographed means the attacks can happen faster (since player needs less time to react).
        Vector2 targetPoint;
        Vector2 slamVelocity;
        bool hitGround = false;
        bool dashLeft = true;
        void UltimaWeaponMove()
        {
            //Inflict debuff
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.WeightOfShadow>(), 60);
                }
            }

            //Delete all grapples. This runs every tick that this attack is in use.
            for (int p = 0; p < 1000; p++)
            {
                if (Main.projectile[p].active && Main.projectile[p].aiStyle == 7)
                {
                    Main.projectile[p].Kill();
                }
            }

            //Initialize things
            if (AttackModeCounter == 0)
            {
                npc.noGravity = false;
                npc.noTileCollide = false;
                npc.Center = Target.Center + new Vector2(-500, 0);               
            }            

            //Hold AttackModeCounter at 1 and keep refreshing the debuff until the target player lands, delaying the start of the attack phase
            if (Target.velocity.Y != 0 && AttackModeCounter == 2)
            {
                AttackModeCounter = 1;               
            }

            //Once the attack properly begins, refresh the debuff, teleport in front of the player, and prepare to dash
            if (AttackModeCounter == 3)
            {
                int distance = 500;
                if (Target.direction == -1)
                {
                    distance *= -1;
                    dashLeft = false;
                }
                else
                {
                    dashLeft = true;
                }
                Vector2 warp = Target.Center;
                warp.X += distance;
                npc.Center = warp;

                DarkCloudParticleEffect(5);
            }
            
            //Prevent it from moving along the y-axis for the duration of the attack
            if(AttackModeCounter >= 3 && AttackModeCounter <= 210)
            {
                npc.velocity.Y = 0;
            }

            //Charging particle effect
            if (AttackModeCounter >= 3 && AttackModeCounter <= 60)
            {
                ChargingParticleEffect(AttackModeCounter - 3, 57);
            }
            
            //Dash at the player. Once past them, slow down and chill for a moment.
            if (AttackModeCounter >= 60 && AttackModeCounter <= 150)
            {
                if ((npc.Center.X > Target.Center.X) && dashLeft)
                {
                    npc.velocity = new Vector2(-30, 0);
                    npc.noTileCollide = true;
                    npc.noGravity = true;
                }
                else if ((npc.Center.X < Target.Center.X) && !dashLeft)
                {
                    npc.velocity = new Vector2(30, 0);
                    npc.noTileCollide = true;
                    npc.noGravity = true;
                }
                else
                {
                    if (Math.Abs(npc.velocity.X) < 1)
                    {
                        npc.noTileCollide = false;
                        npc.noGravity = false;
                    }
                }

                //Make the NPC face the direction it is moving (it defaults toward its target)
                if (npc.velocity.X > 0)
                {
                    npc.direction = 1;
                }
                else
                {
                    npc.direction = -1;
                }
            }

            //Charge up effect again
            if (AttackModeCounter > 120 && AttackModeCounter < 180)
            {
                ChargingParticleEffect((int)AttackModeCounter - 120, 60);
            }

            //Pick and store a point 505 units above the player (the extra 5 is to reduce how often it clips into the ground mid-slam, because terraria's (XNA's?) collision is hot garbage)
            if(AttackModeCounter == 180)
            {
                targetPoint = Target.Center;
                targetPoint.Y -= 505;
            }

            //Leap toward the chosen point, summoning the sword and charging up dust
            if (AttackModeCounter > 180 && AttackModeCounter < 240)
            {
                ChargingParticleEffect((int)AttackModeCounter - 180, 60);
                npc.noTileCollide = true;

                //If not close to the chosen point, accelerate toward it. Within 200 units is close enough.
                if (Vector2.DistanceSquared(npc.Center, targetPoint) > 200)
                {
                    npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, targetPoint, 30 - ((AttackModeCounter - 180) / 2));
                }
                else
                {
                    npc.velocity = Vector2.Zero;
                }
            }

            //Slam down directly toward the player
            if (AttackModeCounter >= 240 && AttackModeCounter < 300)
            {
                npc.noTileCollide = false;

                //On hitting ground after slam
                if (OnGround())
                {
                    //If it's the first frame we hit the ground, do some stuff. If not, do nothing.
                    if (!hitGround)
                    {
                        for (int i = 0; i < 60; i++)
                        {
                            Vector2 offset = Main.rand.NextVector2CircularEdge(5, 5);
                            if (Math.Abs(offset.Y) < 2.5f)
                            {
                                Vector2 velocity = new Vector2(7, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                                Dust.NewDustPerfect(npc.Center + offset, DustID.ShadowbeamStaff, velocity * 5, Scale: 5).noGravity = true;
                            }
                        }

                        npc.velocity = Vector2.Zero;
                        npc.noGravity = false;
                        hitGround = true;
                    }
                }

                //In air slamming
                else
                {
                    npc.noGravity = true;
                    ChargingParticleEffect((int)AttackModeCounter - 240, 20);

                    slamVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 20);
                    
                    //Do not change X velocity if it would cause dark cloud to change directions mid-slam
                    if ((slamVelocity.X > 0 && npc.velocity.X >= 0) || (slamVelocity.X < 0 && npc.velocity.X <= 0))
                    {
                        //Do not change x velocity if it would cause dark cloud to slow down
                        //These checks are to allow the player to dash under it
                        if (Math.Abs(slamVelocity.X) > Math.Abs(npc.velocity.X))
                        {
                            npc.velocity = slamVelocity;
                        }
                    }               

                    npc.velocity.Y = (AttackModeCounter - 240) / 1.5f;
                    if (npc.velocity.Y > 35)
                    {
                        npc.velocity.Y = 35;
                    }
                }    
                
                if(npc.velocity.X > 0)
                {
                    npc.direction = 1;
                }
                else
                {
                    npc.direction = -1;
                }

            }

            //Swing, firing off a crescent shaped wave of shadow energy from the sword
            //Also, reset some variables
            if (AttackModeCounter == 300)
            {
                slamVelocity = Vector2.Zero;
                hitGround = false;
            }

            //Set its velocity to 0 for the next few attacks
            if (AttackModeCounter >= 360 && AttackModeCounter < 600)
            {
                npc.velocity = Vector2.Zero;
            }

            //Teleport behind the player
            if (AttackModeCounter == 360)
            {
                int offset = 120;
                if (Target.direction == 1)
                {
                    offset *= -1;
                }
                Vector2 warp = Target.Center;
                warp.X += offset;
                npc.Center = warp;

                DarkCloudParticleEffect(5);
            }            

            //Teleport behind the player again
            if (AttackModeCounter == 480)
            {
                int offset = 120;
                if (Target.direction == 1)
                {
                     offset *= -1;
                }
                Vector2 warp = Target.Center;
                warp.X += offset;
                npc.Center = warp;

                DarkCloudParticleEffect(5);
            }

            
            //Teleport above player
            if (AttackModeCounter == 600)
            {
                Vector2 warp = Target.Center;
                warp.X -= 0.1f;
                warp.Y -= 500;
                npc.Center = warp;
                npc.noGravity = true;
                npc.noTileCollide = false;
                npc.velocity = Vector2.Zero;
                //Do not get hit by this.
                npc.damage = 600;
            }

            if (AttackModeCounter >= 600 && AttackModeCounter < 630)
            {
                float count = AttackModeCounter - 600;
                DarkCloudParticleEffect(-5, count * 4, 42 - count);
            }

            //Similar to first slam, but fall straight down instead.
            if (AttackModeCounter >= 630 && AttackModeCounter < 730)
            {
                npc.noTileCollide = false;

                //Mostly the same code as before, with a few tweaks
                if (OnGround())
                {
                    if (!hitGround)
                    {
                        for (int i = 1; i < 200; i++)
                        {
                            Vector2 offset = Main.rand.NextVector2CircularEdge(i * 2, i * 2);
                            while (Math.Abs(Math.Sin(offset.ToRotation())) > 0.5f)
                            {
                                offset = Main.rand.NextVector2CircularEdge(i, i);
                            }
                            
                            Vector2 velocity = new Vector2(7, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                            Dust.NewDustPerfect(npc.Center + offset, DustID.ShadowbeamStaff, velocity * 5, Scale: 5).noGravity = true;                         
                        }

                        //Fire shockwave projectiles. This sorta has to be done here, not in Attack() like the other projectiles, because it's not time-based. When the NPC hits the ground depends on how far up it is.
                        //Just another way this attack is kinda sloppy and breaks the way I tried to set this boss up lol
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float projSpeed = 15;     
                            for(int i = 0; i < 50; i++)
                            {
                                Vector2 velocity = new Vector2(projSpeed, 0).RotatedByRandom(MathHelper.ToRadians(45));
                                if (Main.rand.NextBool() == true)
                                {
                                    velocity.X *= -1;
                                }
                                if (Main.rand.NextBool() == true)
                                {
                                    velocity.Y *= -1;
                                }
                                Projectile.NewProjectileDirect(npc.Center, velocity, ModContent.ProjectileType<DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                            }
                        }


                        npc.velocity = Vector2.Zero;
                        npc.damage = 200;
                        npc.noGravity = false;
                        hitGround = true;
                    }
                }
                else
                {
                    npc.noGravity = true;
                    DarkCloudParticleEffect(5, 120, 12);
                    npc.velocity.Y = (AttackModeCounter - 540) / 2f;
                    if (npc.velocity.Y > 11)
                    {
                        npc.velocity.Y = 11;
                    }
                }
            }

            //Reset variables
            if (AttackModeCounter == 770)
            {
                npc.noGravity = true;
                npc.noTileCollide = true;
                npc.velocity = new Vector2(0, -22);
                hitGround = false;
                slamVelocity = Vector2.Zero;
            }

            //End the attack phase
            if(AttackModeCounter == 790)
            {
                ChangeAttacks();
            }
        }

        
        float confinedBlastsRadius = 500;
        float currentBlastAngle = 0;
        void ConfinedBlastsMove()
        {
            //Honestly, this attack exists at least partially just to give melee players an opening to tear the boss apart
            //I think it turned out pretty well, though!
            //Creates a safe region around the NPC, outside of which the player gets pulled in and eventually takes damage
            //Telegraph a series of blasts in different directions. Then fire them one by one with a 1 second delay

            if(AttackModeCounter == 0)
            {
                TeleportToArenaCenter();
            }

            if (targetPlayers == null)
            {
                targetPlayers = new List<Player>();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Vector2.Distance(Main.player[i].Center, npc.Center) < 5000)
                    {
                        targetPlayers.Add(Main.player[i]);
                    }
                }
            }

            //Make sure it stays still
            npc.velocity = Vector2.Zero;

            //Scales from 0 to 1 as the attack ramps up. Certain effects are tied to this.
            float intensity = (AttackModeCounter / 300);
            if (AttackModeCounter > 300)
            {
                intensity = 1;
            }

            //Draw some dust at the attack edge
            DarkCloudParticleEffect(-2 * (AttackModeCounter / 300), 50, confinedBlastsRadius + 2000 * (1 - intensity));

            //Nuke grapples
            NukeGrapples();

            
            float radius = Main.rand.Next((int)(confinedBlastsRadius + 2000 * (1 - intensity)), (int)(2000 + 2000 * (1 - intensity)));
            DarkCloudParticleEffect(-10, 100, radius);
                

            //For each player in the list, check if they're out of the attack range. If so, pull them into it hard.
            float distance;
            foreach (Player p in targetPlayers)
            {
                distance = Vector2.Distance(p.Center, npc.Center);
                if (distance > confinedBlastsRadius)
                {
                    p.velocity += UsefulFunctions.GenerateTargetingVector(p.Center, npc.Center, pullSpeed * 5 * (AttackModeCounter / 300));

                    //If more than 5 seconds have passed, damage them when outside the range
                    if (AttackModeCounter > 300)
                    {
                        float damage = 5;
                        damage *= (1 - p.endurance);
                        if (Main.expertMode)
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.75f);
                        }
                        else
                        {
                            damage += (int)Math.Ceiling(p.statDefense * 0.5f);
                        }

                        p.immuneTime = 0;
                        p.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(p.name + " was torn apart by tidal forces."), (int)damage, 1);
                    }
                }
            }

            if (AttackModeCounter > 300)
            {
                //Every second, perform the attack
                if ((AttackModeCounter - 300) % 60 == 0)
                {
                    currentBlastAngle = NextConfinedBlastsAngle;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NextConfinedBlastsAngle = Main.rand.NextFloat(0, MathHelper.Pi);
                    }
                    InstantNetUpdate();
                }

                //Spawn dust telegraphing next attack
                for (int i = 0; i < 20; i++)
                {
                    //Spawn dust within 45 degrees (Pi / 4) around the chosen angle 
                    float dustAngle = currentBlastAngle + Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
                    Vector2 dustPos = new Vector2(Main.rand.NextFloat(0, confinedBlastsRadius), 0).RotatedBy(dustAngle);
                    Dust thisDust = Dust.NewDustPerfect(npc.Center + dustPos, DustID.VenomStaff, Scale: 1.5f);
                    thisDust.noLight = true;
                    thisDust.noGravity = true;

                    thisDust = Dust.NewDustPerfect(npc.Center - dustPos, DustID.VenomStaff, Scale: 1.5f);
                    thisDust.noLight = true;
                    thisDust.noGravity = true;
                }

                if ((AttackModeCounter - 300) % 60 == 59)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        float dustAngle = currentBlastAngle + Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
                        Vector2 dustVel = new Vector2(99, 0).RotatedBy(dustAngle);
                        Dust.NewDustPerfect(npc.Center, DustID.ShadowbeamStaff, dustVel, Scale: 5).noLight = true;
                        Dust.NewDustPerfect(npc.Center, DustID.ShadowbeamStaff, -dustVel, Scale: 5).noLight = true;
                    }
                    
                    //In-between each attack, nuke all dust that was telegraphing the previous one
                    //Hacky way to do this, but it works
                    for(int i = 0; i < Main.maxDust; i++)
                    {
                        if(Main.dust[i].type == DustID.VenomStaff)
                        {
                            Main.dust[i].active = false;
                            Main.dust[i].scale = 0;
                        }
                    }
                }
            }

            //At the end of the attack, change attacks and spawn a burst of dust
            if (AttackModeCounter >= 800)
            {
                for (int i = 0; i < 120; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(256, 256);
                    Vector2 velocity = new Vector2(15, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(npc.Center, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
            }
            
            //Wait a second and a half to change attacks
            //Give the player time to move to a normal safe distance
            if (AttackModeCounter == 890)
            {
                ChangeAttacks();
            }
        }
        void FreezeBoltsMove()
        {
            if (AttackModeTally == 0)
            {
                if (AttackModeCounter == 0)
                {
                    TeleportToArenaCenter();
                }

                if (AttackModeCounter >= 0 && AttackModeCounter <= 40)
                {
                    IceChargingParticleEffect(AttackModeCounter, 40);
                }
                if (AttackModeCounter > 150)
                {
                    count++;
                    AttackModeCounter = 0;
                }
                if(count == 3)
                {
                    AttackModeCounter = 0;
                    AttackModeTally = 1;
                    return;
                }
            }

            if (AttackModeTally == 1)
            {
                if (AttackModeCounter >= 80 && AttackModeCounter <= 120)
                {
                    IceChargingParticleEffect(AttackModeCounter - 80, 40);
                }
                if (AttackModeCounter == 500)
                {
                    AttackModeCounter = 0;
                    AttackModeTally = 2;
                    return;
                }
            }

            if(AttackModeTally == 2)
            {
                if (AttackModeCounter > 500)
                {
                    //Clean up
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].type == ModContent.ProjectileType<DarkFreezeBolt>())
                        {
                            Main.projectile[i].Kill();
                        }
                    }
                    count = 0;
                    ChangeAttacks();
                }
            }
        }
        Vector2 arrowRainTargetingVector = Vector2.Zero;
        void ArrowRainMove()
        {
            //Right now, this uses a shitty vague approximation of actual ballistic projectile aiming code. It *works*, but could be so much better
            //Will probably see if I can get the actual thing working later

            //Teleport somewhere further from the player, and then fire a shotgun barrage of Arrow of Bard's at them
            if (AttackModeCounter % 80 == 0)
            {
                DarkCloudParticleEffect(-2);

                if (AttackModeCounter > 70)
                {
                    npc.Center = nextWarpPoint + Target.Center;
                }
                else
                {
                    npc.Center = preSelectedWarpPoint + Target.Center;
                }

                //Pick the next warp point immediately after warping, to give it time to sync
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    nextWarpPoint = ArrowRainTeleport();
                }
                DarkCloudParticleEffect(6);
                AttackModeTally++;

                //This is generated client-side here because it's also needed for the draw code, to make it hold the bow at the right angle
                arrowRainTargetingVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 7);
                float travelTime = Math.Abs((npc.Center.X - Target.Center.X) / arrowRainTargetingVector.X);
                //float gravityOffset = travelTime * 0.092f;
                float gravityOffset = travelTime * 0.055f;
                arrowRainTargetingVector.Y -= gravityOffset; //Up is negative
                arrowRainTargetingVector += Target.velocity / 2; //Lightly predictive
            }

            if (AttackModeCounter == 1200)
            {
                ChangeAttacks();
            }
        }
        Vector2 ArrowRainTeleport()
        {
            Vector2 warp = Vector2.Zero;
            do
            {
                warp.X += Main.rand.Next(-900, 900);
                warp.Y += Main.rand.Next(-400, 400);
            } while (Vector2.Distance(warp + Target.Center, Target.Center) < 500);
            return warp;
        }
        void AntiMatMove()
        {
            //Line up an Anti-Mat with a targeting laser, and spawn a handful of reflections around the player. After a delay, they open fire one by one.
            if(AttackModeCounter % 300 == 0)
            {
                DarkCloudParticleEffect(-2);

                //The first warp should be to the pre-selected point
                if (AttackModeCounter > 270)
                {
                    npc.Center = Target.Center + nextWarpPoint;
                }
                else
                {
                    npc.Center = Target.Center + preSelectedWarpPoint;
                }

                //They'll recieve it from the server.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    nextWarpPoint = Main.rand.NextVector2CircularEdge(700, 700);
                }
                DarkCloudParticleEffect(6);
            }

            if (AttackModeCounter % 300 == 15 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<GenericLaser>(), 0, 0.5f, Main.myPlayer, (float)GenericLaser.GenericLaserID.AntiMatTargeting, npc.whoAmI);
                }
            }

            List<GenericLaser> laserList = GenericLaser.GetLasersByID(GenericLaser.GenericLaserID.AntiMatTargeting, npc.whoAmI);
            for (int i = 0; i < laserList.Count; i++)
            {
                if (!laserList[i].initialized)
                {
                    laserList[i].LaserOrigin = npc.Center;          
                    laserList[i].TelegraphTime = 99999;
                    laserList[i].LaserLength = 4000;
                    //Forbidden secret color "half red"
                    laserList[i].LaserColor = Color.Red * 0.5f;// * 2f;
                    laserList[i].lightColor = Color.OrangeRed;
                    laserList[i].TileCollide = false;
                    laserList[i].CastLight = true;
                    laserList[i].MaxCharge = 5;
                    laserList[i].FiringDuration = 285;
                    laserList[i].LaserVolume = 0;
                    //Deals no damage, it simply exists to telegraph dark cloud's shot
                    laserList[i].TargetingMode = 2;

                    laserList[i].initialized = true;
                }

                //Get a vector of length 128 pointing from dark cloud to the player, then rotate it by 90 degrees
                Vector2 offset = UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 128).RotatedBy(MathHelper.ToRadians(90));
                //Multiply it by ((300 - AttackModeCounter) / 300), meaning as AttackModeCounter increases and approaches 0, the offset distance shrinks down
                //Then multiply it by Math.Sin, using AttackModeCounter as the parameter because it changes smoothly. Then add 120 * i, so each laser is offset by 120 degrees
                //offset *= ((300 - AttackModeCounter) / 300) * (float)Math.Sin(MathHelper.ToRadians(2 * AttackModeCounter + (120 * i)));
                offset *= ((300 - (AttackModeCounter % 300)) / 300);
                offset = offset.RotatedBy(MathHelper.ToRadians((AttackModeCounter % 300) + (120 * i)));
                laserList[i].LaserTarget = Target.Center + offset;
            }


            if (AttackModeCounter == 1200)
            {                
                ChangeAttacks();
            }            
        }

        float slashesWarpRadius = 750;
        void TeleportingSlashesMove()
        {
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.aiStyle = 0;
            npc.velocity.Y += 0.09f;
            npc.velocity.X *= 1.07f;
            if (Target.Center.X > npc.Center.X)
            {
                npc.direction = 1;
            }
            else
            {
                npc.direction = -1;
            }            

            //Skip the first one to give players time to react
            if (AttackModeCounter % 80 == 0 && AttackModeCounter != 80)
            {                
                DarkCloudParticleEffect(-2); 
                if (AttackModeCounter > 70)
                {
                    npc.Center = nextWarpPoint;
                }
                else
                {
                    npc.Center = preSelectedWarpPoint;
                }
                nextWarpPoint = Target.Center + Main.rand.NextVector2CircularEdge(slashesWarpRadius, slashesWarpRadius);
                DarkCloudParticleEffect(6);
                npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 17);
            }

            for(int i = 0; i < 50; i++)
            {
                Dust.NewDustPerfect(nextWarpPoint + Main.rand.NextVector2Circular(30, 60), DustID.ShadowbeamStaff, Main.rand.NextVector2CircularEdge(3, 3));
            }

            //Perform a rapid chain of dashes toward the player, while swinging Ultima Weapon
            //At the end, dash above the player, swing the weapon around once, then it slam it down at high speed requiring the player to dodge just as the slam starts
            if (AttackModeCounter == 640)
            {
                //Clean up
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<DarkCloudMirror>())
                    {
                        Main.npc[i].active = false;
                    }
                }
                ChangeAttacks();
            }
        }
        void BulletPortalsMove()
        {
            //Scrapped: Too similar to teleporting slashes to dodge. Might rework this later, do kinda want to give it some attack based on the Expulsor Cannon
            //Dark cloud opens a black hole in front of itself, then begins firing the quardro cannon into it (with a tighter spread than the players)
            //A second later white holes begin to open up randomly at close range around the player one by one, firing bursts of bullets toward them
        }
        #endregion

        //These describe projectiles the boss should shoot, and other things that should *not* be done for every multiplayer client
        #region Attacks
        void DragoonLanceAttack()
        {
            if (AttackModeCounter >= 60 && AttackModeCounter < 180 && ((AttackModeCounter) % 5 == 0))
            {
                Projectile.NewProjectile(npc.Center, new Vector2(-0.5f, -0.5f), ModContent.ProjectileType<DarkCloudDragoonLance>(), dragoonLanceDamage, 0.5f, Main.myPlayer, 20);
                DarkCloudParticleEffect(5, 15);
            }
            if (AttackModeCounter >= 180 && AttackModeCounter < 300 && ((AttackModeCounter - 2) % 5 == 0))
            {
                Projectile.NewProjectile(npc.Center, new Vector2(-0.5f, -0.5f), ModContent.ProjectileType<DarkCloudDragoonLance>(), dragoonLanceDamage, 0.5f, Main.myPlayer, 20);
                DarkCloudParticleEffect(5, 15);
            }
            if (AttackModeCounter >= 300 && AttackModeCounter < 420 && ((AttackModeCounter) % 5 == 0))
            {
                Projectile.NewProjectile(npc.Center, new Vector2(-0.5f, -0.5f), ModContent.ProjectileType<DarkCloudDragoonLance>(), dragoonLanceDamage, 0.5f, Main.myPlayer, 20);
                DarkCloudParticleEffect(5, 15);
            }
        }        
        
        void DivineSparkAttack()
        {
            //Since most laser code has to be run both client and server side, this is just a placeholder.
        }

        int darkFlowRadius = 2000;
        void DarkFlowAttack()
        {
            if(AttackModeCounter == 0)
            {
                //Spawn Black Hole Projectile
            }

            if (AttackModeCounter % 4 == 0)
            {
                Projectile.NewProjectile(npc.Center + Main.rand.NextVector2CircularEdge(darkFlowRadius, darkFlowRadius / 1.3f), Vector2.Zero, ModContent.ProjectileType<DarkFlow>(), darkFlowDamage, 0.5f, Main.myPlayer, npc.whoAmI, 900 - AttackModeCounter);
            }
            

        }
        
        void DivineSparkThirdsAttack()
        {

        }

        void UltimaWeaponAttack()
        {
            if(AttackModeCounter == 3)
            {
                //Spawn the sword.
                //That's it. The rest of it happens within the sword's ai.
                NPC.NewNPC((int)npc.position.X, (int)npc.position.Y, ModContent.NPCType<DarkUltimaWeapon>(), 0, npc.whoAmI);
            }
        }

        void ConfinedBlastsAttack()
        {
            if (AttackModeCounter > 300 && (AttackModeCounter - 300) % 60 == 59)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active)
                    {
                        Vector2 diff = npc.Center - p.Center;
                        float posAngle = diff.ToRotation();

                        //Deeply cursed conversions from XNA FuckoUnits to normal radians, because working with the former was giving me a headache
                        posAngle += MathHelper.Pi;
                        posAngle = MathHelper.TwoPi - posAngle;
                        float workingAngle = MathHelper.Pi - currentBlastAngle;

                        //Mirror the player angle to the top half of the circle if it's on the bottom
                        //This means we don't need to check both blast zones, since they're mirrored too
                        if (posAngle > MathHelper.Pi)
                        {
                            posAngle -= MathHelper.Pi;
                        }

                        //Take the difference.
                        //Each blast zone is actually split in two down the middle: since left and right don't make sense on a circle they can be called the "clockwise" half, and "counter clockwise" half
                        //diff < pi / 4 means they're in the counter-clockwise half of one of them. diff > 3pi / 4 means they're in the clockwise half of the other.
                        float posDiff = Math.Abs(posAngle - workingAngle);
                        if (posDiff < MathHelper.PiOver4 || posDiff > (MathHelper.Pi - MathHelper.PiOver4))
                        {                            
                            if (Vector2.DistanceSquared(npc.Center, p.Center) < confinedBlastsRadius * confinedBlastsRadius)
                            {
                                float damage = confinedBlastDamage;
                                damage *= (1 - p.endurance);
                                if (Main.expertMode)
                                {
                                    damage += (int)Math.Ceiling(p.statDefense * 0.75f);
                                }
                                else
                                {
                                    damage += (int)Math.Ceiling(p.statDefense * 0.5f);
                                }

                                p.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(p.name + " was shattered."), (int)damage, 1);
                            }
                        }
                    }
                }
            }
        }

        float attackAngle;
        float count = 0;
        void FreezeBoltsAttack()
        {
            if (AttackModeTally == 0) {
                if ((AttackModeCounter - 40) % 20 == 0 && AttackModeCounter < 80 && count < 2)
                {
                    float boltCount = 16;
                    int speed = 10;
                    for (float i = 0; i < boltCount; i++)
                    {
                        Vector2 attackVel = new Vector2(speed, 0);
                        float tally = (AttackModeCounter - 40) / 20;
                        attackVel = attackVel.RotatedBy(MathHelper.TwoPi * (i / boltCount) + MathHelper.ToRadians(15 * tally));
                        Projectile.NewProjectileDirect(npc.Center, attackVel, ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);
                    }
                }                
            }

            if (AttackModeTally == 1) {
                float boltCount = 100;
                int speed = 8;
                //Store the angle 120 degrees counter clockwise of the target's current position
                if (AttackModeCounter == 120)
                {
                    attackAngle = Target.Center.ToRotation() + MathHelper.ToRadians(120);
                }
                if ((AttackModeCounter >= 120 && AttackModeCounter < 360) && AttackModeCounter % 2 == 0)
                {
                    attackAngle += MathHelper.ToRadians((180f / boltCount) * ((AttackModeCounter - 120) / 120f));
                    Vector2 attackVel = new Vector2(speed, 0).RotatedBy(attackAngle);
                    Projectile.NewProjectileDirect(npc.Center, attackVel, ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);
                    Projectile.NewProjectileDirect(npc.Center, -attackVel, ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);
                }
            }

            if (AttackModeTally == 2)
            {
                if (AttackModeCounter >= 120 && AttackModeCounter < 390)
                {
                    int speed = 11;
                    if ((AttackModeCounter - 120) % 60 == 0)
                    {
                        count = 0;
                        attackAngle = (Target.Center - npc.Center).ToRotation() - MathHelper.ToRadians(45);
                    }
                    int step = (int)AttackModeCounter - 120;
                    //Integer division is evil, but occasionally useful evil
                    step -= (60 * (step / 60));

                    if ((step % 5 == 0) && step < 25)
                    {
                        count++; 
                        Vector2 attackVel = new Vector2(speed, 0).RotatedBy(attackAngle + MathHelper.ToRadians(15 * count));
                        Projectile.NewProjectileDirect(npc.Center, attackVel.RotatedBy(-MathHelper.PiOver4), ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);
                        Projectile.NewProjectileDirect(npc.Center, attackVel, ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);
                        Projectile.NewProjectileDirect(npc.Center, attackVel.RotatedBy(MathHelper.PiOver4), ModContent.ProjectileType<DarkFreezeBolt>(), freezeBoltDamage, 0.5f, Main.myPlayer);

                    }
                }                
            }
        }

        void ArrowRainAttack()
        {
            if (AttackModeCounter == 0 || AttackModeCounter % 80 == 20)
            {
                for (int i = 0; i < 7; i++)
                {
                    Projectile.NewProjectile(npc.Center, arrowRainTargetingVector.RotatedBy(MathHelper.ToRadians(-45 + 15 * i)), ModContent.ProjectileType<EnemyArrowOfDarkCloud>(), arrowRainDamage, 0.5f, Main.myPlayer);
                }
            }
        }

        void AntiMatAttack()
        {            
            if (AttackModeCounter % 300 == 15)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector2 pos = Main.rand.NextVector2CircularEdge(700, 700) + Target.Center;
                    NPC.NewNPC((int)pos.X, (int)pos.Y, ModContent.NPCType<DarkCloudMirror>(), 0, DarkCloudAttackID.AntiMat, 60 + Main.rand.NextFloat(150));
                }
            }            

            if (AttackModeCounter % 300 == 299)
            {
                Projectile.NewProjectile(npc.Center, UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 14).RotatedBy(MathHelper.ToRadians(10)), ModContent.ProjectileType<DarkAntiMatRound>(), antiMatDamage, 0.5f, Main.myPlayer);
                Projectile.NewProjectile(npc.Center, UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 16), ModContent.ProjectileType<DarkAntiMatRound>(), antiMatDamage, 0.5f, Main.myPlayer);
                Projectile.NewProjectile(npc.Center, UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 14).RotatedBy(MathHelper.ToRadians(-10)), ModContent.ProjectileType<DarkAntiMatRound>(), antiMatDamage, 0.5f, Main.myPlayer);
            }
        }

        void TeleportingSlashesAttack()
        {            
            if (AttackModeCounter == 0)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<DarkUltimaWeapon>(), ai0: npc.whoAmI, ai2: DarkCloudAttackID.TeleportingSlashes);

                Vector2 spawnPoint = Target.Center + Main.rand.NextVector2CircularEdge(slashesWarpRadius, slashesWarpRadius);
                NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<DarkCloudMirror>(), ai0: DarkCloudAttackID.TeleportingSlashes);
            } 
            if (AttackModeCounter % 20 == 0)
            {                
                Vector2 spawnPoint = Target.Center + Main.rand.NextVector2CircularEdge(slashesWarpRadius, slashesWarpRadius);
                NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<DarkCloudMirror>(), ai0: DarkCloudAttackID.TeleportingSlashes);
            }
        }

        void BulletPortalsAttack()
        {

        }
        #endregion

        //Change from the first classic phase to the actual fight
        void ChangePhases()
        {
            if (!changingPhases)
            {                
                InitializeMoves();
                if (testAttack == -1)
                {
                    ChangeAttacks();
                }
                else
                {
                    CurrentMove = ActiveMoveList[testAttack];
                    NextAttackMode = testAttack;
                }

                PrecalculateFirstTeleport();
                changingPhases = true;
                npc.dontTakeDamage = true;
                npc.noTileCollide = false;
                npc.noGravity = true;
                npc.aiStyle = 0;
            }

            if (phaseChangeCounter <= 180)
            {
                npc.velocity = Vector2.Zero;
                DarkCloudParticleEffect(-3, (float)30 * (float)(phaseChangeCounter / 180));
            }
            if (phaseChangeCounter == 180)
            {
                DarkCloudParticleEffect(12, 90);
                //wingAnimationState = spread open
            }
            if (phaseChangeCounter < 210)
            {
                for (int i = 0; i < 5; i++)
                {
                    float intensity = (AttackModeCounter / 210);

                    float radius = Main.rand.Next((int)(1 + 2000 * (1 - intensity)), (int)(2000 + 2000 * (1 - intensity)));
                    DarkCloudParticleEffect(-10, 100, radius);
                }
            }
            if (phaseChangeCounter == 210)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ActuatePyramid();
                }
                npc.noTileCollide = true;
                npc.velocity = new Vector2(0, -22);
                //Spawn a burst of dust throughout the whole arena and below dark cloud, potentially activate shader
            }
            if (phaseChangeCounter == 240)
            {
                npc.velocity = Vector2.Zero;
                npc.lifeMax = 1000000;
                npc.life = npc.lifeMax;
                changingPhases = false;
                npc.dontTakeDamage = false;
                firstPhase = false;              
            }
            phaseChangeCounter++;
        }

        #region Teleport Functions
        void DashToAroundPlayer()
        {
            //TODO: Implement
        }

        void TeleportToArenaCenter()
        {
            DarkCloudParticleEffect(-2);
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                npc.Center = new Vector2(5827.5f, 1698) * 16;
            }
            else
            {
                Vector2 warpPoint = Target.Center;
                warpPoint.Y -= 600;
                npc.Center = warpPoint;
            }
            DarkCloudParticleEffect(6);
            InstantNetUpdate();
        }
        #endregion

        //The dust ring particle effect the boss uses
        void DarkCloudParticleEffect(float dustSpeed, float dustAmount = 50, float radius = 64)
        {
            for(int i = 0; i < dustAmount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = new Vector2(dustSpeed, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                Dust.NewDustPerfect(npc.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
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
                Dust.NewDustPerfect(npc.Center + offset, DustID.MagicMirror, velocity, Scale: 2).noGravity = true;
            }
        }

        //Nuke all grapples
        //Yes, this will also delete them for any players not fighting this boss
        //No, I don't care. It's not a big deal, and this runs for 1000 projectiles every frame. It needs to be fast.
        void NukeGrapples()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].aiStyle == 7)
                {
                    Main.projectile[i].Kill();
                }
            }
        }

        //Tells the game to sync the NPC's data *now* instead of waiting until the end of AI() like npc.netUpdate = true;
        void InstantNetUpdate()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, this.npc.whoAmI);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = ModContent.GetTexture("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkCloud");
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height / Main.npcFrameCount[npc.type]);
            Vector2 origin = sourceRectangle.Size() / 2f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            for (float i = TRAIL_LENGTH - 1; i >= 0 ; i--)
            {
                Main.spriteBatch.Draw(texture, npc.oldPos[(int)i] - Main.screenPosition + new Vector2(12, 16), sourceRectangle, drawColor * ((TRAIL_LENGTH - i) / TRAIL_LENGTH), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            }
           
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (!firstPhase && CurrentMove != null && CurrentMove.Draw != null)
            {
                CurrentMove.Draw(spriteBatch, drawColor);
            }
        }

        #region Draw Functions
        public void DivineSparkDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            float targetPoint;
            if ((AttackModeCounter % 90) > 60 && (AttackModeCounter % 90) < 90)
            {
                if (counterClockwise)
                {
                    targetPoint = initialTargetRotation - MathHelper.ToRadians(4 * (int)((AttackModeCounter % 90) - 60));
                }
                else
                {
                    targetPoint = initialTargetRotation + MathHelper.ToRadians(4 * (int)((AttackModeCounter % 90) - 60));
                }
            }
            else
            {
                targetPoint = initialTargetRotation;
                Vector2 startPos = new Vector2(0, 160).RotatedBy(initialTargetRotation + MathHelper.ToRadians(-90));
                if (!Main.gamePaused)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 thisPos = npc.Center + startPos + Main.rand.NextVector2Circular(50, 50);
                        Vector2 thisVel = UsefulFunctions.GenerateTargetingVector(thisPos, npc.Center + Main.rand.NextVector2Circular(10, 10), 8);
                        Dust.NewDustPerfect(thisPos, DustID.FireworkFountain_Blue, thisVel).noGravity = true;
                    }
                    DarkCloudParticleEffect(-2, 1);
                }
            }
            //targetPoint = initialTargetRotation;
            Texture2D texture = ModContent.GetTexture("tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkCloudSpark");
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, drawColor, targetPoint, origin, npc.scale, SpriteEffects.None, 0f);
        }

        public void AntiMatDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            float targetPoint = UsefulFunctions.GenerateTargetingVector(npc.Center, Target.Center, 1).ToRotation();
            if (!Main.gamePaused && (AttackModeCounter % 3 == 0))
            {                
                Vector2 thisPos = npc.Center + new Vector2(0, 128).RotatedBy(targetPoint - MathHelper.PiOver2) + Main.rand.NextVector2Circular(32, 32);
                Vector2 thisVel = UsefulFunctions.GenerateTargetingVector(thisPos, npc.Center + Main.rand.NextVector2Circular(10, 10), 8);
                Dust.NewDustPerfect(thisPos, DustID.FireworkFountain_Red, thisVel, 100, default, 0.5f).noGravity = true;                
            }
            

            Texture2D texture = ModContent.GetTexture(ModContent.GetModItem(ModContent.ItemType<Items.Weapons.Ranged.AntimatRifle>()).Texture);
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);
            SpriteEffects theseEffects = (npc.Center.X < Target.Center.X) ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, drawColor, targetPoint, origin, npc.scale, theseEffects, 0f);
        }

        public void ArrowRainDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            float targetPoint = arrowRainTargetingVector.ToRotation();
            if (!Main.gamePaused && (AttackModeCounter % 80 == 20))
            {
                for (int i = 0; i < 20; i++) {
                    Vector2 thisVel = arrowRainTargetingVector + Main.rand.NextVector2Circular(10, 10);
                    Dust.NewDustPerfect(npc.Center, DustID.FireworkFountain_Green, thisVel).noGravity = true;
                }
            }


            Texture2D texture = ModContent.GetTexture(ModContent.GetModItem(ModContent.ItemType<Items.Weapons.Ranged.CernosPrime>()).Texture);
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);
            SpriteEffects theseEffects = (npc.Center.X < Target.Center.X) ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, drawColor, targetPoint, origin, npc.scale, theseEffects, 0f);
        }
        #endregion

        void InitializeMoves(List<int> validMoves = null)
        {
            DefaultList = new List<DarkCloudMove> {
                new DarkCloudMove(DragoonLanceMove, DragoonLanceAttack, DarkCloudAttackID.DragoonLance, "Dragoon Lance"),
                new DarkCloudMove(DivineSparkMove, DivineSparkAttack, DarkCloudAttackID.DivineSpark, "Divine Spark", DivineSparkDraw),
                new DarkCloudMove(DarkFlowMove, DarkFlowAttack, DarkCloudAttackID.DarkFlow, "Dark Flow"),
                new DarkCloudMove(UltimaWeaponMove, UltimaWeaponAttack, DarkCloudAttackID.UltimaWeapon, "Ultima Weapon"),
                new DarkCloudMove(FreezeBoltsMove, FreezeBoltsAttack, DarkCloudAttackID.FreezeBolts, "Freeze Bolts"),
                new DarkCloudMove(AntiMatMove, AntiMatAttack, DarkCloudAttackID.AntiMat, "Anti-Mat", AntiMatDraw),
                new DarkCloudMove(ConfinedBlastsMove, ConfinedBlastsAttack, DarkCloudAttackID.ConfinedBlasts, "Confined Blasts"),
                new DarkCloudMove(ArrowRainMove, ArrowRainAttack, DarkCloudAttackID.ArrowRain, "Arrow Rain", ArrowRainDraw),
                new DarkCloudMove(TeleportingSlashesMove, TeleportingSlashesAttack, DarkCloudAttackID.TeleportingSlashes, "Teleporting Slashes"),
                ///new DarkCloudMove(BulletPortalsMove, BulletPortalsAttack, DarkCloudAttackID.BulletPortals),
                ///new DarkCloudMove(DivineSparkThirdsMove, DivineSparkThirdsAttack, DarkCloudAttackID.DivineSparkThirds),
                };

            ActiveMoveList = new List<DarkCloudMove>();
            List<DarkCloudMove> TempList = DefaultList; 

            if (validMoves != null)
            {
                for (int i = 0; i < TempList.Count; i++)
                {
                    if (validMoves.Contains(TempList[i].ID))
                    {
                        ActiveMoveList.Add(TempList[i]);
                    }
                }
            }
            else
            {
                ActiveMoveList = TempList;
            }
        }

        //Useful code from old AI to check if it's on the ground.
        bool OnGround()
        {
            bool standing_on_solid_tile = false;
           
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            int x_left_edge = (int)npc.position.X / 16;
            int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
            {
                Tile t = Main.tile[l, y_below_feet];
                if (t.active() && !t.inActive() && Main.tileSolid[(int)t.type]) // tile exists and is solid
                {
                    standing_on_solid_tile = true;
                    break; // one is enough so stop checking
                }
            } // END traverse blocks under feet
            return standing_on_solid_tile;
        }

        //Teleport itself and the player to the center of the pyramid
        public override bool PreNPCLoot()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Vector2 pyramidCenter = new Vector2(5828, 1750) * 16;
                npc.Center = pyramidCenter; 
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Vector2.Distance(Main.player[i].Center, npc.Center) < 2000)
                    {
                        Main.player[i].Center = pyramidCenter;
                    }
                }
                DarkCloudParticleEffect(-12, 120, 64);
            }
            return true;
        }

        public override void NPCLoot()
        {

            Dust.NewDust(npc.position, npc.width, npc.height, 52, 0.3f, 0.3f, 200, default(Color), 1f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(npc.position, npc.width, npc.height, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(npc.position, npc.width, npc.height, 52, 0.2f, 0.2f, 200, default(Color), 4f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(npc.position, npc.height, npc.width, 52, 0.2f, 0.2f, 200, default(Color), 4f);

            if (Main.expertMode)
            {
                npc.DropBossBags();
            }
            else
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>());
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Humanity>(), 3);
                if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.ReflectionShift>());
                }
                else
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.DuskCrownRing>());
                }
            }

            ActuatePyramid();
            Main.NewText("You have subsumed your shadow...", Color.Blue);
        }

        #region Debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;

           
            player.AddBuff(BuffID.BrokenArmor, 120 / expertScale, false); //broken armor
            player.AddBuff(BuffID.OnFire, 180 / expertScale, false); //on fire!
            player.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 3600, false); //defense goes time on every hit
            
        }
        #endregion

        #region Pyramid
        public static int[,] Lanterns = new int[8, 2] { { 5825, 1715 }, { 5832, 1715 }, { 5825, 1732 }, { 5832, 1732 }, { 5825, 1749 }, { 5832, 1749 }, { 5822, 1767 }, { 5834, 1767 } };
        public static int[,] Bulbs = new int[6, 2] { { 5821, 1686 }, { 5823, 1684 }, { 5826, 1682 }, { 5829, 1682 }, { 5832, 1684 }, { 5834, 1686 } };
        public static void ActuatePyramid()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems) {

                //Destroy Lanterns (doing it like this prevents tiles from doing annoying things like dropping an item or spawning a boss)
                for (int i = 0; i < 8; i++)
                {
                    if (Main.tile[Lanterns[i, 0], Lanterns[i, 1]].type == TileID.HangingLanterns)
                    {
                        Main.tile[Lanterns[i, 0], Lanterns[i, 1]] = new Tile();
                        Main.tile[Lanterns[i, 0], Lanterns[i, 1] + 1] = new Tile();
                        //WorldGen.KillTile(Lanterns[i, 0], Lanterns[i, 1], noItem: true);
                        //WorldGen.KillTile(Lanterns[i, 0], Lanterns[i, 1] + 1, noItem: true);
                    }
                }
                
                //Bulbs
                for (int i = 0; i < 6; i++)
                {
                    if (Main.tile[Bulbs[i, 0], Bulbs[i, 1]].type == TileID.PlanteraBulb)
                    {
                        Main.tile[Bulbs[i, 0], Bulbs[i, 1]] = new Tile();
                        Main.tile[Bulbs[i, 0], Bulbs[i, 1] - 1] = new Tile();
                        Main.tile[Bulbs[i, 0] + 1, Bulbs[i, 1]] = new Tile();
                        Main.tile[Bulbs[i, 0] + 1, Bulbs[i, 1] - 1] = new Tile();

                        //WorldGen.PlaceTile(Bulbs[i, 0], Bulbs[i, 1], TileID.Meteorite);
                    }
                }

                //Base of the pyramid
                for (int x = 5697; x < 5937; x++) {
                    for (int y = 1696; y < 1773; y++)
                    {
                        Wiring.ActuateForced(x, y);
                    }
                }
                //109, 43
                //Middle of the pyramid
                for (int x = 5774; x < 5883; x++)
                {
                    for (int y = 1653; y < 1696; y++)
                    {
                        Wiring.ActuateForced(x, y);
                    }
                }
                
                //Tip of the pyramid
                for (int x = 5814; x < 5843; x++)
                {
                    for (int y = 1638; y < 1653; y++)
                    {                                                   
                        Wiring.ActuateForced(x, y);
                    }
                }

                //Covering the gap on the left
                int offset = 0; 
                for (int y = 1773; y < 1777; y++)                    
                {
                    for (int x = 5740; x < 5748; x++)
                    {
                        if (Main.tile[x + offset, y].type != TileID.SandstoneBrick)
                        {
                            WorldGen.PlaceTile(x + offset, y, TileID.SandstoneBrick, true, true);
                        }
                        else
                        {
                            WorldGen.KillTile(x + offset, y, noItem: true);
                        }
                    }
                    offset++;
                }

                //Place lanterns (yes, they have to be seperate from breaking because Place[X] functions respect anchor points)
                for (int i = 0; i < 8; i++)
                {
                    if (Main.tile[Lanterns[i, 0], Lanterns[i, 1]].type != TileID.HangingLanterns)
                    {
                        WorldGen.Place1x2Top(Lanterns[i, 0], Lanterns[i, 1], TileID.HangingLanterns, 24);
                    }
                }

                //And the bulbs
                for (int i = 0; i < 6; i++)
                {
                    if (Main.tile[Bulbs[i, 0], Bulbs[i, 1]].type != TileID.PlanteraBulb)
                    {
                        WorldGen.Place2x2(Bulbs[i, 0] + 1, Bulbs[i, 1], TileID.PlanteraBulb, 0);
                    }
                }

                //Making the steps on the right



            }
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

            int sound_type = 0; // Parameter for Main.PlaySound().  14 for Zombie, Skeleton, Angry Bones, Heavy Skeleton, Skeleton Archer, Bald Zombie.  26 for Mummy, Light & Dark Mummy. 0 means no sounds
            int sound_frequency = 1000;  //  chance to play sound every frame, 1000 for zombie/skel, 500 for mummies

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
            if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
                moonwalking = true;
            #endregion
            //-------------------------------------------------------------------
            #region teleportation particle effects
            if (can_teleport)  //  chaos elemental type teleporter
            {
                if (npc.ai[3] == -120f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
                {
                    npc.velocity *= 0f; // stop moving
                    npc.ai[3] = 0f; // reset boredom to 0
                    Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
                    Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f); // current location
                    float num6 = npc.oldPos[2].X + (float)npc.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
                    float num7 = npc.oldPos[2].Y + (float)npc.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
                    float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
                    num8 = 2f / num8; // to normalize to 2 unit long vector
                    num6 *= num8; // direction to where it was 3 frames ago, vector normalized
                    num7 *= num8; // direction to where it was 3 frames ago, vector normalized
                    for (int j = 0; j < 20; j++) // make 20 dusts at current position
                    {
                        int num9 = Dust.NewDust(npc.position, npc.width, npc.height, 71, num6, num7, 200, default(Color), 2f);
                        Main.dust[num9].noGravity = true; // floating
                        Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
                        expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
                    }
                    for (int k = 0; k < 20; k++) // more dust effects at old position
                    {
                        int num10 = Dust.NewDust(npc.oldPos[2], npc.width, npc.height, 71, -num6, -num7, 200, default(Color), 2f);
                        Main.dust[num10].noGravity = true;
                        Dust expr_1A6F_cp_0 = Main.dust[num10];
                        expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
                    }
                } // END just teleported
            } // END can teleport
            #endregion
            //-------------------------------------------------------------------
            #region adjust boredom level
            if (!is_archer || npc.ai[2] <= 0f)  //  loop to set ai[3] (boredom)
            {
                if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)boredom_time || moonwalking)  //  stopped or bored or moonwalking
                    npc.ai[3] += 1f; // increase boredom
                else if ((double)Math.Abs(npc.velocity.X) > bored_speed && npc.ai[3] > 0f)  //  moving fast and not bored
                    npc.ai[3] -= 1f; // decrease boredom

                if (npc.justHit || npc.ai[3] > boredom_cooldown)
                    npc.ai[3] = 0f; // boredom wears off if enough time passes, or if hit

                if (npc.ai[3] == (float)boredom_time)
                    npc.netUpdate = true; // netupdate when state changes to bored
            }
            #endregion
            //-------------------------------------------------------------------
            #region play creature sounds, target/face player, respond to boredom
            if ((!hates_light || !Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0) && npc.ai[3] < (float)boredom_time)
            {  // not fleeing light & not bored
                if (sound_type > 0 && Main.rand.Next(sound_frequency) <= 0)
                    Main.PlaySound(sound_type, (int)npc.position.X, (int)npc.position.Y, 1); // random creature sounds
                if (!canDrown || (canDrown && !npc.wet) || (quickBored && boredTimer > tBored))
                {
                    //npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
                }
            }
            else if (!is_archer || npc.ai[2] <= 0f) //  fleeing light or bored (& not aiming)
            {
                if (hates_light && Main.dayTime && (double)(npc.position.Y / 16f) < Main.worldSurface && npc.timeLeft > 10)
                    //npc.timeLeft = 10;  //  if hates light & in light, hasten despawn

                    if (npc.velocity.X == 0f)
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
                    npc.direction = 1; // flee right if direction not set? or is initial direction?
            } // END fleeing light or bored (& not aiming)
            #endregion
            //-------------------------------------------------------------------
            #region enrage
            bool enraged = false; // angry from damage; not stored from tick to tick
            if ((enrage_percentage > 0) && (npc.life < (float)npc.lifeMax * enrage_percentage))  //  speed up at low life
                enraged = true;
            if (enraged)
            { // speed up movement if enraged
                acceleration = enrage_acceleration;
                top_speed = enrage_top_speed;
            }
            #endregion
            //-------------------------------------------------------------------
            #region melee movement

            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f + comboDamage / 500);
            Main.dust[dust].noGravity = true;



            if (!is_archer || (npc.ai[2] <= 0f && !npc.confused))  //  meelee attack/movement. archers only use while not aiming
            {
                if (Math.Abs(npc.velocity.X) > top_speed)  //  running/flying faster than top speed
                {
                    if (npc.velocity.Y == 0f)  //  and not jump/fall
                        npc.velocity *= (1f - braking_power);  //  decelerate
                }
                else if ((npc.velocity.X < top_speed && npc.direction == 1) || (npc.velocity.X > -top_speed && npc.direction == -1))
                {  //  running slower than top speed (forward), can be jump/fall
                    if (can_teleport && moonwalking)
                        npc.velocity.X = npc.velocity.X * 0.99f;  //  ? small decelerate for teleporters

                    npc.velocity.X = npc.velocity.X + (float)npc.direction * acceleration;  //  accellerate fwd; can happen midair
                    if ((float)npc.direction * npc.velocity.X > top_speed)
                        npc.velocity.X = (float)npc.direction * top_speed;  //  but cap at top speed
                }  //  END running slower than top speed (forward), can be jump/fall
            } // END non archer or not aiming*/
            #endregion
            //-------------------------------------------------------------------
            #region archer projectile code (stops moving to shoot)
            if (is_archer)
            {
                if (npc.confused)
                    npc.ai[2] = 0f; // won't try to stop & aim if confused
                else // not confused
                {
                    if (npc.ai[1] > 0f)
                        npc.ai[1] -= 1f; // decrement fire & reload counter

                    if (npc.justHit) // was just hit?
                    {
                        npc.ai[1] = 30f; // shot on .5 sec cooldown
                        npc.ai[2] = 0f; // not aiming
                    }
                    if (npc.ai[2] > 0f) // if aiming: adjust aim and fire if needed
                    {
                        //npc.TargetClosest(true); // target and face closest player
                        if (npc.ai[1] == (float)(shot_rate / 2))  //  fire at halfway through; first half of delay is aim, 2nd half is cooldown
                        { // firing:
                            Vector2 npc_center = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f); // npc position
                            float npc_to_target_x = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - npc_center.X; // x vector to target
                            float num16 = Math.Abs(npc_to_target_x) * 0.1f; // 10% of x distance to target: to aim high if farther?
                            float npc_to_target_y = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - npc_center.Y - num16; // y vector to target (aiming high at distant targets)
                            npc_to_target_x += (float)Main.rand.Next(-40, 41); //  targeting error: 40 pix=2.5 blocks
                            npc_to_target_y += (float)Main.rand.Next(-40, 41); //  targeting error: 40 pix=2.5 blocks
                            float target_dist = (float)Math.Sqrt((double)(npc_to_target_x * npc_to_target_x + npc_to_target_y * npc_to_target_y)); // distance to target
                            npc.netUpdate = true; // ??
                            target_dist = projectile_velocity / target_dist; // to normalize by projectile_velocity
                            npc_to_target_x *= target_dist; // normalize by projectile_velocity
                            npc_to_target_y *= target_dist; // normalize by projectile_velocity
                            npc_center.X += npc_to_target_x;  //  initial projectile position includes one tick of initial movement
                            npc_center.Y += npc_to_target_y;  //  initial projectile position includes one tick of initial movement
                            if (Main.netMode != 1)  //  is server
                                Projectile.NewProjectile(npc_center.X, npc_center.Y, npc_to_target_x, npc_to_target_y, projectile_id, meteorDamage, 0f, Main.myPlayer);

                            if (Math.Abs(npc_to_target_y) > Math.Abs(npc_to_target_x) * 2f) // target steeply above/below NPC
                            {
                                if (npc_to_target_y > 0f)
                                    npc.ai[2] = 1f; // aim downward
                                else
                                    npc.ai[2] = 5f; // aim upward
                            }
                            else if (Math.Abs(npc_to_target_x) > Math.Abs(npc_to_target_y) * 2f) // target on level with NPC
                                npc.ai[2] = 3f;  //  aim straight ahead
                            else if (npc_to_target_y > 0f) // target is below NPC
                                npc.ai[2] = 2f;  //  aim slight downward
                            else // target is not below NPC
                                npc.ai[2] = 4f;  //  aim slight upward
                        } // END firing
                        if (npc.velocity.Y != 0f || npc.ai[1] <= 0f) // jump/fall or firing reload
                        {
                            npc.ai[2] = 0f; // not aiming
                            npc.ai[1] = 0f; // reset firing/reload counter (necessary? nonzero maybe)
                        }
                        else // no jump/fall and no firing reload
                        {
                            npc.velocity.X = npc.velocity.X * 0.9f; // decelerate to stop & shoot
                            npc.spriteDirection = npc.direction; // match animation to facing
                        }
                    } // END if aiming: adjust aim and fire if needed
                    if (npc.ai[2] <= 0f && npc.velocity.Y == 0f && npc.ai[1] <= 0f && !Main.player[npc.target].dead && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
                    { // not aiming & no jump/fall & fire/reload ctr is 0 & target is alive and LOS to target: start aiming
                        float num21 = 10f; // dummy vector length in place of initial velocity? not sure why this is needed
                        Vector2 npc_center = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                        float npc_to_target_x = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - npc_center.X;
                        float num23 = Math.Abs(npc_to_target_x) * 0.1f; // 10% of x distance to target: to aim high if farther?
                        float npc_to_target_y = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - npc_center.Y - num23; // y vector to target (aiming high at distant targets)
                        npc_to_target_x += (float)Main.rand.Next(-40, 41);
                        npc_to_target_y += (float)Main.rand.Next(-40, 41);
                        float target_dist = (float)Math.Sqrt((double)(npc_to_target_x * npc_to_target_x + npc_to_target_y * npc_to_target_y));
                        if (target_dist < 700f) // 700 pix = 43.75 blocks
                        { // target is in range
                            npc.netUpdate = true; // ??
                            npc.velocity.X = npc.velocity.X * 0.5f; // hard brake
                            target_dist = num21 / target_dist; // to normalize by num21
                            npc_to_target_x *= target_dist; // normalize by num21
                            npc_to_target_y *= target_dist; // normalize by num21
                            npc.ai[2] = 3f; // aim straight ahead
                            npc.ai[1] = (float)shot_rate; // start fire & reload counter
                            if (Math.Abs(npc_to_target_y) > Math.Abs(npc_to_target_x) * 2f) // target steeply above/below NPC
                            {
                                if (npc_to_target_y > 0f)
                                    npc.ai[2] = 1f; // aim downward
                                else
                                    npc.ai[2] = 5f; // aim upward
                            }
                            else if (Math.Abs(npc_to_target_x) > Math.Abs(npc_to_target_y) * 2f) // target on level with NPC
                                npc.ai[2] = 3f; // aim straight ahead
                            else if (npc_to_target_y > 0f)
                                npc.ai[2] = 2f; // aim slight downward
                            else
                                npc.ai[2] = 4f; // aim slight upward
                        } // END target is in range
                    } // END start aiming
                } // END not confused
            }  //  END is archer
            #endregion
            //-------------------------------------------------------------------


            #region shoot and walk
            if (!oBored && shoot_and_walk && !Main.player[npc.target].dead) // can generalize this section to moving+projectile code 
            {
                // Main.netMode != 1 &&

                #region Charge
                //if(Main.netMode != 1)
                //{
                if (breakCombo == true || (enraged == true && Main.rand.Next(700) == 1) || (enraged == false && Main.rand.Next(1700) == 1))
                {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    npc.knockBackResist = 0f;
                    breakCombo = false;
                    npc.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    npc.damage = 120;
                    npc.knockBackResist = 0;
                    chargeDamage++;
                }
                if (chargeDamage >= 96)
                {
                    chargeDamageFlag = false;
                    npc.damage = 95;
                    npc.knockBackResist = 0.2f;
                    chargeDamage = 0;
                }

                //}
                #endregion

                #region Projectiles
                //if(Main.netMode != 1)
                //{
                customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
                if (customAi1 >= 10f)
                {
                    //npc.TargetClosest(true);
                    if ((customspawn1 < 1) && Main.rand.Next(1000) == 1)
                    {
                        int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.CrystalKnight>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        npc.ai[0] = 20 - Main.rand.Next(80);
                        customspawn1 += 1f;
                        if (Main.netMode != 1)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }
                    if ((customspawn2 < 2) && Main.rand.Next(3500) == 1)
                    {
                        int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightNemesis>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        npc.ai[0] = 20 - Main.rand.Next(80);
                        customspawn2 += 1f;
                        if (Main.netMode != 1)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }



                    if ((customspawn3 < 0) && Main.rand.Next(9950) == 1)
                    {
                        int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.Assassin>(), 0);
                        Main.npc[Spawned].velocity.Y = -8;
                        Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                        npc.ai[0] = 20 - Main.rand.Next(80);
                        customspawn3 += 1f;
                        if (Main.netMode != 1)
                        {
                            NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                    }



                    if (Main.rand.Next(700) == 1)
                    {
                        float num48 = 10f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 6;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(195) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 700;
                            Main.projectile[num54].aiStyle = 23;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }


                    if (Main.rand.Next(520) == 1)
                    {
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                        npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                        npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                        npc.ai[1] = 1f;
                        npc.netUpdate = true;
                    }
                    if (Main.rand.Next(340) == 1)
                    {
                        float num48 = 18f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 100 + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, holdBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 105;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            npc.ai[1] = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(120) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.Center.Y - 10f);
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = (((Main.player[npc.target].position.Y - 10) + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemyDragoonLance>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, dragoonLanceDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 700;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(300) == 1)
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 600;
                            Main.projectile[num54].aiStyle = 23;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            npc.ai[1] = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(85) == 1)
                    {
                        float num48 = 12f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            //int damage = 80;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 450;
                            Main.projectile[num54].aiStyle = 23;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }


                    if (Main.rand.Next(350) == 1)
                    {
                        float num48 = 12f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellArmageddonBlastBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, armageddonDamage, 0f, Main.myPlayer);
                            //Main.projectile[num54].timeLeft = 0;
                            Main.projectile[num54].aiStyle = 23;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(70) == 1)
                    {
                        float num48 = 14f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity1Ball>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, gravityBallDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 40;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            npc.ai[1] = 1f;
                        }
                        npc.netUpdate = true;
                    }
                    if (Main.rand.Next(280) == 1)
                    {
                        float num48 = 11f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, poisonStrikeDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 270;
                            Main.projectile[num54].aiStyle = 23;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }
                    if (Main.rand.Next(350) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 1000 + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, crazedPurpleCrushDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 600;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }











                    if (Main.rand.Next(526) == 1)
                    {
                        float num48 = 7f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, shadowShotDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 200;
                            Main.projectile[num54].aiStyle = 23; //was 23
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }

                    if (Main.rand.Next(50) == 1)
                    {
                        float num48 = 8f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 0;//was 70
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            npc.ai[1] = 1f;
                        }
                        npc.netUpdate = true;
                    }





                    if (Main.rand.Next(65) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.EnemyArrowOfDarkCloud>(); //44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, darkArrowDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 1300;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }


                    if (Main.rand.Next(555) == 1)
                    {
                        float num48 = 13f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormWave>(); //44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, stormWaveDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 1300;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
                    }



                    if (Main.rand.Next(205) == 1)
                    {
                        float num48 = 15f;
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.EnemyArrowOfDarkCloud>(); //44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, darkArrowDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 1300;
                            Main.projectile[num54].aiStyle = 1;
                            Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
                            customAi1 = 1f;
                        }
                        npc.netUpdate = true;
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
            if (npc.velocity.Y == 0f) // no jump/fall
            {
                int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
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
            //-------------------------------------------------------------------
            #region new Tile()s, door opening/breaking
            if (standing_on_solid_tile)  //  if standing on solid tile
            {
                int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
                int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
                if (clown_sized)
                    x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f); // 16 pix in front of edge
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
                if (Main.tile[x_in_front + npc.direction, y_above_feet - 1] == null)
                    Main.tile[x_in_front + npc.direction, y_above_feet - 1] = new Tile();

                if (Main.tile[x_in_front + npc.direction, y_above_feet + 1] == null)
                    Main.tile[x_in_front + npc.direction, y_above_feet + 1] = new Tile();

                if (Main.tile[x_in_front, y_above_feet - 1].active() && Main.tile[x_in_front, y_above_feet - 1].type == 10 && can_pass_doors)
                { // tile in front is active, is door and NPC can pass doors: trying to break door
                    npc.ai[2] += 1f; // inc knock countdown
                    npc.ai[3] = 0f; // not bored if working on breaking a door
                    if (npc.ai[2] >= 60f)  //  knock once per second
                    {
                        if (!Main.bloodMoon && can_pass_doors_bloodmoon_only)
                            npc.ai[1] = 0f;  //  damage counter zeroed unless bloodmoon, but will still knock

                        npc.velocity.X = 0.5f * (float)(-(float)npc.direction); //  slight recoil from hitting it
                        npc.ai[1] += door_break_pow;  //  increase door damage counter
                        npc.ai[2] = 0f;  //  knock finished; start next knock
                        bool door_breaking = false;  //  door break flag
                        if (npc.ai[1] >= 10f)  //  at 10 damage, set door as breaking (and cap at 10)
                        {
                            door_breaking = true;
                            npc.ai[1] = 10f;
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
                                bool door_opened = WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction);  //  open the door
                                if (!door_opened)  //  door not opened successfully
                                {
                                    npc.ai[3] = (float)boredom_time;  //  bored if door is stuck
                                    npc.netUpdate = true;
                                    npc.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
                                }
                                if (Main.netMode == 2 && door_opened) // is server & door was just opened
                                    NetMessage.SendData(19, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0); // ??
                            }
                        }  //  END server and door breaking
                    } // END knock on door
                } // END trying to break door
                #endregion
                //-------------------------------------------------------------------
                #region jumping, reset door knock & damage counters
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
                        else if (npc.directionY < 0 && jump_gaps && (!Main.tile[x_in_front, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].type]) && (!Main.tile[x_in_front + npc.direction, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front + npc.direction, y_above_feet + 1].type]))
                        { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                            npc.velocity.Y = -8f; // jump with power 8
                            npc.velocity.X = npc.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                            npc.netUpdate = true;
                        }
                        else if (can_pass_doors) // standing on solid tile but not in front of a passable door, moving forward, didnt jump.  I assume recoil from hitting door is too small to move passable door out of range and trigger this
                        {
                            npc.ai[1] = 0f;  //  reset door dmg counter
                            npc.ai[2] = 0f;  //  reset knock counter
                        }
                    } // END moving forward, still: standing on solid tile but not in front of a passable door
                    if (hops && npc.velocity.Y == 0f && Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < hop_range_x && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < hop_range_y && ((npc.direction > 0 && npc.velocity.X >= hop_velocity) || (npc.direction < 0 && npc.velocity.X <= -hop_velocity)))
                    { // type that hops & no jump/fall & near target & moving forward fast enough: hop code
                        npc.velocity.X = npc.velocity.X * 2f; // burst forward
                        if (npc.velocity.X > hop_speed) // but cap at hop_speed
                            npc.velocity.X = hop_speed;
                        else if (npc.velocity.X < -hop_speed)
                            npc.velocity.X = -hop_speed;

                        npc.velocity.Y = -hop_power; // and jump of course
                        npc.netUpdate = true;
                    }
                    if (can_teleport && npc.velocity.Y < 0f) // jumping
                        npc.velocity.Y = npc.velocity.Y * 1.1f; // infinite jump? antigravity?
                }
            }
            else if (can_pass_doors)  //  not standing on a solid tile & can open/break doors
            {
                npc.ai[1] = 0f;  //  reset door damage counter
                npc.ai[2] = 0f;  //  reset knock counter
            }//*/
            #endregion
            //-------------------------------------------------------------------
            #region teleportation
            if (Main.netMode != 1 && can_teleport && npc.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
            {
                int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
                int x_blockpos = (int)npc.position.X / 16; // corner not center
                int y_blockpos = (int)npc.position.Y / 16; // corner not center
                int tp_radius = 25; // radius around target(upper left corner) in blocks to teleport into
                int tp_counter = 0;
                bool flag7 = false;
                if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
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
                        if ((m < target_y_blockpos - 9 || m > target_y_blockpos + 9 || tp_x_target < target_x_blockpos - 9 || tp_x_target > target_x_blockpos + 6) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].active())
                        { // over 6 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                            bool safe_to_stand = true;
                            bool dark_caster = false; // not a fighter type AI...
                            if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
                                safe_to_stand = false;
                            else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
                                safe_to_stand = false;

                            if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                            { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                                npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                npc.netUpdate = true;
                                npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                flag7 = true; // end the loop (after testing every lower point :/)
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
                    if (npc.velocity.X == 0f)
                    {
                        boredTimer++;
                        if (boredTimer > tBored)
                        {
                            boredResetT = 0;
                            npc.directionY = -1;
                            if (npc.velocity.Y > 0f)
                            {
                                npc.direction = 1;
                            }
                            npc.direction = -1;
                            if (npc.velocity.X > 0f)
                            {
                                npc.direction = 1;
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

            #endregion
        }
        #endregion

        #region Vanilla overrides and misc
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        //Takes double damage from melee weapons
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            damage *= 2;
            crit = true;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.melee)
            {
                damage *= 2;
                crit = true;
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

        //So I don't have to remember magic numbers
        //Public because Dark Cloud Mirror NPC's also use it
        public class DarkCloudAttackID
        {
            public const short DragoonLance = 0;
            public const short DivineSpark = 1;
            public const short DarkFlow = 2;
            public const short UltimaWeapon = 3;
            public const short FreezeBolts = 4;
            public const short AntiMat = 5;
            public const short ConfinedBlasts = 6;
            public const short ArrowRain = 7;
            public const short TeleportingSlashes = 8;
            public const short BulletPortals = 9;
            public const short Thunderstorm = 10;
            public const short DivineSparkThirds = 11;
        }
    }    
}