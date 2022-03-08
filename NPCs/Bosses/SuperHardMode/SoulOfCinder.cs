using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Gwyn;
using static tsorcRevamp.UsefulFunctions;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode {
    [AutoloadBossHead]
    class SoulOfCinder : ModNPC {
        
        public override bool Autoload(ref string name) => false;
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Gwyn, Lord of Cinder");
        }

        public override void SetDefaults() {
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
            npc.friendly = false;
            npc.damage = 1;
            npc.defense = 90;
            npc.lifeMax = 500000;
            npc.width = npc.height = 40;
            npc.npcSlots = 50;
            npc.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler("You have fallen before the Lord of Cinder...", Color.OrangeRed, 6);
        }

        private float AI_State {
            get => npc.ai[0];
            set => npc.ai[0] = value;
        }

        private float AI_Timer { //usually +1 per frame during phases
            get => npc.ai[1];
            set => npc.ai[1] = value;
        }

        private float AI_Misc { //miscellaneous tracking inside phases
            get => npc.ai[2];
            set => npc.ai[2] = value;
        }

        private float AI_State_Counter { //+1 every phase change 
            get => npc.ai[3];
            set => npc.ai[3] = value;
        }
        private float LOCALAI_Just_Spawned {
            get => npc.localAI[0];
            set => npc.localAI[0] = value;
        }

        private enum States {
            Spawning = -1,
            Idle = 0,
            Movement = 1,
            Tackle = 2,
            FarronHail = 3,
            TheArchivist = 4,
            Ritual = 5,
            AncientLight = 6,
            Placeholder1 = 7,
            Placeholder2 = 8,
            Placeholder3 = 9,
            BulletHell = 10
        }

        private enum DustShapes {
            Circle = 0,
            Plus = 1,
            X = 2
        }

        Vector2 ArenaCenter;
        List<Player> TaggedPlayers;
        public static readonly float ARENA_WIDTH = 864;
        public static readonly float ARENA_HEIGHT = 656;
        public static Vector2 ARENA_LOCATION_ADVENTURE = new Vector2(832.5f, 1226) * 16;
        bool BulletHell;

        NPCDespawnHandler despawnHandler;

        public override void AI() {
            bool expertMode = Main.expertMode;
            bool halfLife = npc.life <= npc.lifeMax / 2;
            bool DontTakeDamage = false;
            bool DisableHoming = false;

            if (despawnHandler.TargetAndDespawn(npc.whoAmI)) {
                InterruptCurrentPhase();
            }

            if (halfLife) {
                npc.defense = (int)(npc.defDefense * 0.65f);
            }

            if (LOCALAI_Just_Spawned == 0f) {
                //dont have to bother syncing npc because it's always 0 on spawn
                Init();
                LOCALAI_Just_Spawned = 1f;
            }

            if (ArenaCenter.Length() > 1) {
                for (int j = 0; j < 24; j++) {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(ARENA_WIDTH, ARENA_HEIGHT);
                    Vector2 dustPos = new Vector2(ArenaCenter.X, ArenaCenter.Y + 336) + dir;
                    Vector2 dustVel = GenerateTargetingVector(ArenaCenter, dustPos, 16);
                    Dust.NewDustPerfect(dustPos, 235, dustVel, 200).noGravity = true;
                }
            }

            if (Main.GameUpdateCount % 10 == 0) {
                for (int i = 0; i < Main.maxPlayers; i++) {
                    Player player = Main.player[i];
                    if (!player.active)
                        continue;
                    if (IsPointWithinEllipse(player.Center, ArenaCenter, ARENA_WIDTH, ARENA_HEIGHT)) {
                        TaggedPlayers.Add(player);
                    }
                }

                //Stay Inside The Dust Ring You Fool
                foreach (Player tagged in TaggedPlayers) {
                    if (!IsPointWithinEllipse(tagged.Center, ArenaCenter, ARENA_WIDTH + (8 * 16), ARENA_HEIGHT + (8 * 16))) {
                        tagged.AddBuff(ModContent.BuffType<Buffs.CowardsAffliction>(), 30);
                    }
                }
            }

            switch (AI_State) {
                case (float)(States.Spawning):
                    State_Spawning(ref DontTakeDamage, ref DisableHoming);
                    break;
                case (float)(States.Idle):
                    State_Idle(halfLife);
                    break;
                case (float)(States.Movement):
                    State_Movement();
                    break;
                case (float)(States.Tackle):
                    State_Tackle(expertMode, halfLife);
                    break;
                case (float)(States.FarronHail):
                    State_FarronHail(expertMode, halfLife);
                    break;
                case (float)States.TheArchivist:
                    State_TheArchivist(expertMode, ref DisableHoming, ref DontTakeDamage);
                    break;
                case (float)States.BulletHell:
                    State_BulletHell(expertMode, ref DontTakeDamage);
                    break;
                default:
                    State_Idle(halfLife);
                    break;
            }

            if (halfLife && !BulletHell) {
                InterruptCurrentPhase();
                AI_State = (float)States.BulletHell;
                BulletHell = true;
                Main.NewText("prepare for die");
            }



            if (AI_State == (float)States.AncientLight) {
                //UNFINISHED
                ReturnToIdle();
            }

            npc.dontTakeDamage = DontTakeDamage;
            npc.chaseable = !DisableHoming;


        }

        public override bool? CanBeHitByProjectile(Projectile projectile) {
            bool canBeHurt = false;
            if (TaggedPlayers != null) {
                foreach (Player p in TaggedPlayers) {
                    if (Main.player[projectile.owner].whoAmI == p.whoAmI) {
                        canBeHurt = true;
                        break;
                    }
                }
            }
            return canBeHurt;
        }

        public override bool? CanBeHitByItem(Player player, Item item) {
            bool canBeHurt = false;
            if (TaggedPlayers != null) {
                foreach (Player p in TaggedPlayers) {
                    if (player.whoAmI == p.whoAmI) {
                        canBeHurt = true;
                        break;
                    }
                }
            }
            return canBeHurt;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(npc.dontTakeDamage);
            writer.Write(npc.chaseable);
            writer.WriteVector2(ArenaCenter);
            writer.Write(BulletHell);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            npc.dontTakeDamage = reader.ReadBoolean();
            npc.chaseable = reader.ReadBoolean();
            ArenaCenter = reader.ReadVector2();
            BulletHell = reader.ReadBoolean();
        }

        private void State_Spawning(ref bool DontTakeDamage, ref bool DisableHoming) {
            npc.alpha -= 5;
            if (npc.alpha < 0) {
                npc.alpha = 0;
            }
            AI_Timer += 1f;

            if (AI_Timer == 45) {
                for (int j = 0; j < 96; j++) {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(5, 5);
                    Vector2 dustPos = npc.Center + dir;
                    Vector2 dustVel = new Vector2(5, 0).RotatedBy(dir.ToRotation());
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.Firework_Red, dustVel, 100);
                    dust.noGravity = true;
                    dust.scale = 1.2f;
                }
            }
            if (AI_Timer >= 75) {
                AI_State = 0f;
                AI_Timer = 0f;
                npc.netUpdate = true;
            }

            DontTakeDamage = true;
            DisableHoming = true;
        }
        private void State_Idle(bool halfLife) {
            Player player = Main.player[npc.target];
            int targetDirection = Math.Sign(player.Center.X - npc.Center.X);
            if (targetDirection != 0) {
                npc.direction = (npc.spriteDirection = targetDirection);
            }
            AI_Timer += 1f;

            int IdleTime = 15;

            if (halfLife)
                IdleTime = 8;

            if (AI_Timer == 1) {
                int maxValue = 6;
                if (npc.life < npc.lifeMax / 3) {
                    maxValue = 4;
                }
                if (npc.life < npc.lifeMax / 4) {
                    maxValue = 3;
                }

                AI_Misc = Main.rand.Next(maxValue);
                npc.netUpdate = true;
            }
            if (AI_Timer >= IdleTime) {
                /* at the end of every phase, AI_State_Counter is incremented by 1 (in ReturnToIdle())
                 * when AI_State_Counter is even (% 2), StateChange will return 0, which will go straight to Movement
                 * otherwise, pick the next attack from the predefined sequence in StateChange
                 * i.e. the sequence is always idle - movement - idle - attack
                 */

                int statePicker = StateChange(halfLife);

                if (halfLife && (AI_Misc == 0) && statePicker != (int)States.Movement) {
                    statePicker = (int)States.AncientLight;
                    AI_Misc = 0;
                }

                if (statePicker == (int)States.Idle) {
                    Vector2 toPlayer = new Vector2(player.Center.X, player.Center.Y - 192) - npc.Center;
                    float movementTimer = (float)Math.Ceiling(toPlayer.Length() / 30f);
                    if (movementTimer == 0f) { //div0 protection
                        movementTimer = 1f;
                    }
                    AI_State = (float)States.Movement;
                    AI_Timer = movementTimer * 1.35f; //movement has deceleration near the target, so time is increased to compensate
                    AI_Misc = 0;
                    npc.velocity = toPlayer / movementTimer; //longer distances = more velocity, but also longer timer. they balance out

                    npc.netUpdate = true;

                }
                else {
                    AI_Timer = 0;
                    AI_State = statePicker;
                    AI_Misc = 0;
                    npc.netUpdate = true;
                }
            }
        }
        private void State_Movement() {
            Player player = Main.player[npc.target];
            //is there air friction for NPCs even if you dont explicitly add it?
            npc.velocity.X *= 1.075f;
            Vector2 toPlayer = player.Center - npc.Center;
            if (toPlayer.Length() < 16 * 20) {
                npc.velocity *= 0.9f;
            }
            //AI_Timer is set to a positive integer at the end of any idle phase that preceeds a movement phase, so decrement instead of incrementing
            AI_Timer -= 1f;
            if (AI_Timer <= 0f) {
                ReturnToIdle();
            }
        }
        private void State_Tackle(bool expertMode, bool halfLife) {
            int TackleInterval = 20;
            int TackleSpeed = 38;
            int TackleDamage = 102;
            int TackleCount = 5;
            if (expertMode) {
                TackleDamage = (int)(TackleDamage * 0.5f);
                TackleSpeed = 42;
            }

            if (halfLife) {
                TackleInterval = 18;
            }

            Player player = Main.player[npc.target];
            Vector2 toPlayer = Vector2.Normalize(player.Center - npc.Center + player.velocity * 10f);

            //delay the first tackle
            if (AI_Timer == 0 && AI_Misc == 0) {
                for (int i = 0; i < 64; i++) {
                    //and make a ring of dust to announce we're in tackle phase
                    Vector2 dir = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 dustPos = npc.Center + dir;
                    Dust.NewDustPerfect(dustPos, DustID.RuneWizard, dir / 4f, 200).noGravity = true;

                }
                AI_Timer -= 20;
                AI_Misc = 1;
            }
            //+1 because we use the first one just above
            if (AI_Misc < TackleCount + 1) {
                AI_Timer += 1f;
                if (AI_Timer == TackleInterval) {
                    npc.velocity = Vector2.Zero;
                }

                if (AI_Timer > TackleInterval && AI_Timer < TackleInterval * 2.5) {
                    int dustRadius = 48;
                    float speed = 2;
                    for (int i = 0; i < 8; i++) {
                        Vector2 dir = toPlayer.RotatedByRandom(2.35); //135 degrees
                        Vector2 dustPos = npc.Center + dir * dustRadius;
                        Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 2) * speed;

                        Dust dust = Dust.NewDustPerfect(dustPos, 106, dustVel, 0, new Color(255, 0, 0), 1f);
                        dust.noGravity = true;

                    }
                }

                if (AI_Timer == TackleInterval * 2 && Main.netMode != NetmodeID.MultiplayerClient) {
                    Vector2 Tackle = toPlayer;
                    Tackle *= TackleSpeed;

                    //speed up the dash if the target is too far away...
                    float speedMod = ((player.Center - npc.Center).Length() / 650f);

                    //...but not to more than 130% speed
                    speedMod = MathHelper.Clamp(speedMod, 1, 1.3f);
                    Tackle *= speedMod;

                    npc.velocity = Tackle;
                    Projectile.NewProjectile(npc.position, Vector2.Zero, ModContent.ProjectileType<TackleProjectile>(), TackleDamage, 1, Main.myPlayer, TackleInterval / 2, npc.whoAmI);
                }

                if (AI_Timer > TackleInterval * 2 && npc.velocity != Vector2.Zero) {
                    npc.velocity.X *= 1.075f;
                }

                if (AI_Timer > TackleInterval * 3) {
                    for (int i = 0; i < Main.maxProjectiles; i++) {
                        if (Main.projectile[i].type == ModContent.ProjectileType<TackleProjectile>()) {
                            Main.projectile[i].Kill();
                            break;
                        }
                    }
                    npc.velocity *= 0.7f;

                }

                if (AI_Timer > TackleInterval * 3.5) {
                    AI_Timer = 0;
                    AI_Misc++;
                }

            }

            else {
                ReturnToIdle();
            }

        }
        private void State_FarronHail(bool expertMode, bool halfLife) {
            Player player = Main.player[npc.target];
            const float VELOCITY = 24;
            const int VOLLEY_COUNT = 9;
            int VolleyInterval = 35;
            float SpreadAngle = 52.5f;
            int BulletCount = 5;
            int FarronHailDamage = 88;
            if (expertMode) {
                FarronHailDamage = (int)(FarronHailDamage * 0.5f);
            }
            if (halfLife) {
                VolleyInterval = 30;
                BulletCount = 7;
                SpreadAngle = 67f;
            }

            if (AI_Misc < 90) {
                //AI_Misc but accessors cant be passed as reference
                ToArenaCenterWithDust(ref npc.ai[2], DustID.Clentaminator_Cyan, DustShapes.Plus);
            }

            else { AI_Timer++; }

            if (AI_Timer % VolleyInterval == (VolleyInterval - 5)) {

                Vector2 pos = new Vector2(npc.Center.X, npc.Center.Y - 64);
                Vector2 toPlayer = (Vector2.Normalize(player.Center - pos)) * VELOCITY;

                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = -(BulletCount / 2); i < ((BulletCount / 2) + 1); i++) {
                        //spread shot like
                        //....v
                        //..//|\\
                        //center line aimed at the player
                        //alert: one-liner gore
                        Vector2 shot = toPlayer.RotatedBy(MathHelper.ToRadians(0 - (((float)(SpreadAngle / (BulletCount - 1))) * i)));
                        Projectile.NewProjectile(pos, shot, ModContent.ProjectileType<FarronHailSpawner>(), FarronHailDamage, 1, Main.myPlayer, 0, VolleyInterval);
                    }
                }
            }

            if (AI_Timer > (VOLLEY_COUNT * VolleyInterval)) {
                ReturnToIdle();
            }

        }
        private void State_TheArchivist(bool expertMode, ref bool DisableHoming, ref bool DontTakeDamage) {

            if (AI_Misc < 90) {
                //AI_Misc but accessors cant be passed as reference
                ToArenaCenterWithDust(ref npc.ai[2], DustID.Clentaminator_Purple, DustShapes.X);
            }

            else { AI_Timer++; }
            if (AI_Timer == 1 && Main.netMode != NetmodeID.MultiplayerClient) {
                Projectile.NewProjectile(npc.Center, npc.velocity, ModContent.ProjectileType<Projectiles.Enemy.Gwyn.SwordOfLordGwyn>(), 69, 0.5f, npc.whoAmI, npc.whoAmI);
            }

            if (AI_Timer == 60) {
                for (int i = 0; i < Main.maxProjectiles; i++) {
                    if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Enemy.Gwyn.SwordOfLordGwyn>()) {
                        Main.projectile[i].Kill();
                    }
                }
                Main.NewText("hnere " + npc.whoAmI);
                ReturnToIdle();
            }
        }
        private void State_BulletHell(bool expertMode, ref bool DontTakeDamage) {
            //this phase is a long one
            int LaserDamage = 70;
            int ShotDamage = 70;
            float shotVelocity = 8f;

            if (expertMode) {
                ShotDamage = (int)(ShotDamage * 0.5f);
            }

            if (AI_Misc < 90) {
                //TeleportToArenaCenter with a different location
                AI_Misc++;
                if (AI_Misc >= 0 && AI_Misc < 15) {
                    float AlphaMod = (AI_Misc) / 15f;
                    npc.alpha = (int)(AlphaMod * 255f);

                }

                else if (AI_Misc == 15) {
                    npc.alpha = 255;
                    npc.Center = new Vector2(ArenaCenter.X, ArenaCenter.Y + (ARENA_HEIGHT / 3));
                }

                else if (AI_Misc >= 16 && AI_Misc < 30) {
                    for (int i = 0; i < 10; i++) {
                        Vector2 velocity = MakeDustShape(DustShapes.Circle, i);
                        Dust.NewDustPerfect(npc.Center, DustID.Clentaminator_Green, velocity).noGravity = true;

                    }
                }

                else if (AI_Misc >= 30 && AI_Misc < 60) {
                    float AlphaMod = (AI_Misc - 30f) / 15f;
                    npc.alpha = 255 - (int)(AlphaMod * 255f);
                }
            }

            else {
                AI_Timer++;
            }

            //1
            if (AI_Timer == 1) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1); //alternates -1 and 1
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X + (ARENA_WIDTH / 1.8f * flipflop), npc.Center.Y - (ARENA_HEIGHT / 3)),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 15;
                        spawner.lifespan = 420;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = 2 * flipflop;
                    }
                }
            }
            //2
            if (AI_Timer == 180) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X + (ARENA_WIDTH / 1.8f * flipflop), npc.Center.Y - ARENA_HEIGHT / 3f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 15;
                        spawner.lifespan = 420;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = 3 * flipflop;
                    }
                }
            }
            //3
            if (AI_Timer == 405) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X + (ARENA_WIDTH / 25f * flipflop), npc.Center.Y - ARENA_HEIGHT / 2.5f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 12;
                        spawner.lifespan = 300;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = 5 * flipflop;
                    }
                }
            }
            //4
            if (AI_Timer == 700) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X, npc.Center.Y - ARENA_HEIGHT / 3f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 6;
                        spawner.lifespan = 390;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = 5 * flipflop;
                    }
                }
            }
            //5
            if (AI_Timer == 910) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X + (ARENA_WIDTH / 1.8f * flipflop), npc.Center.Y - ARENA_HEIGHT / 2.5f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 12;
                        spawner.lifespan = 420;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = -4; //they both rotate the same direction, this is on purpose
                    }
                }
            }
            //6
            if (AI_Timer == 1240) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 4; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X + (ARENA_WIDTH / 1.8f * flipflop), npc.Center.Y - ARENA_HEIGHT / 2.2f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 21; //there are four spawners this time!
                        spawner.lifespan = 200;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = (i > 1 ? 15 : 5) * flipflop; //15 reduced to 5 on frame 1, used to check if the projectile should rotate on spawn
                    }
                }
            }
            if (AI_Timer == 1420) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    for (int i = 0; i < 2; i++) {
                        int flipflop = (((i % 2) * 2) - 1);
                        BulletHellSpawner spawner = Projectile.NewProjectileDirect(
                            new Vector2(npc.Center.X, npc.Center.Y - ARENA_HEIGHT / 2.5f),
                            Vector2.Zero,
                            ModContent.ProjectileType<BulletHellSpawner>(),
                            ShotDamage,
                            2).modProjectile as BulletHellSpawner;
                        spawner.shotInterval = 3; //it's gamer time
                        spawner.lifespan = 200;
                        spawner.shotVelocity = shotVelocity;
                        spawner.rotationSpeed = 5 * flipflop;
                    }
                }
            }
            if (AI_Timer == 1620) {
                ReturnToIdle();
            }
        }
        private void ReturnToIdle() {
            AI_State = (int)States.Idle;
            AI_Timer = 0;
            AI_Misc = 0;
            AI_State_Counter += 1;

            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
        }
        private void InterruptCurrentPhase() {
            //ReturnToIdle minus the idle phase change and state counter increment
            //for hp threshold phase changes 
            AI_Timer = 0;
            AI_Misc = 0;

            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
        }
        private int StateChange(bool halfLife) {
            if (AI_State_Counter % 2 == 0) {
                return (int)States.Idle;
            }
            else {
                int statePicker = 0;
                if (true) {
                    switch ((int)AI_State_Counter) {
                        case 1:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 3:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 5:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 7:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 9:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 11:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 13:
                            statePicker = (int)States.TheArchivist;
                            break;
                        case 15:
                            statePicker = (int)States.TheArchivist;
                            AI_State_Counter = -1f;
                            break;
                        default:
                            AI_State_Counter = -1f;
                            break;
                    }
                }
                else {
                    switch ((int)AI_State_Counter) {
                        case 1:
                            statePicker = (int)States.FarronHail;
                            break;
                        case 3:
                            statePicker = (int)States.FarronHail;
                            break;
                        case 5:
                            statePicker = (int)States.FarronHail;
                            AI_State_Counter = -1f;
                            break;
                        default:
                            AI_State_Counter = -1f;
                            break;
                    }
                }

                return statePicker;
            }
        }
        private void ToArenaCenterWithDust(ref float timer, int dustType, DustShapes shape) {
            timer++;
            if (timer >= 0 && timer < 15) {
                float AlphaMod = (timer) / 15f;
                npc.alpha = (int)(AlphaMod * 255f);

            }

            else if (timer == 15) {
                npc.alpha = 255;
                npc.Center = ArenaCenter;
            }

            else if (timer >= 16 && timer < 30) {
                for (int i = 0; i < 10; i++) {
                    Vector2 velocity = MakeDustShape(shape, i);
                    Dust.NewDustPerfect(npc.Center, dustType, velocity).noGravity = true;

                }
            }

            else if (timer >= 30 && timer < 60) {
                float AlphaMod = (timer - 30f) / 15f;
                npc.alpha = 255 - (int)(AlphaMod * 255f);
            }
        }
        private Vector2 MakeDustShape(DustShapes shape, int iteration = 0) {
            Vector2 velocity;
            switch (shape) {
                case DustShapes.Circle:
                    velocity = Main.rand.NextVector2Circular(10, 10);
                    break;
                case DustShapes.Plus:
                case DustShapes.X:
                    //makes a +
                    velocity = Main.rand.NextVector2Circular(iteration % 2 == 0 ? 0.3f : 10, iteration % 2 == 0 ? 10 : 0.3f);
                    if (shape == DustShapes.X) {
                        velocity = velocity.RotatedBy(Math.PI / 4);
                    }
                    break;
                default:
                    velocity = Vector2.Zero;
                    break;
            }
            return velocity;
        }
        private void Init() {
            Player target = Main.player[npc.target];
            if (ArenaCenter.Length() < 1 || ArenaCenter == null) {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                    ArenaCenter = ARENA_LOCATION_ADVENTURE;
                    //if the door is open
                    if (Framing.GetTileSafely(778, 1243).active() && Framing.GetTileSafely(778, 1243).inActive()) {
                        //trigger the switch to close the door and turn on the lights
                        Wiring.TripWire(782, 1241, 1, 1);
                    }
                    
                }
                else {
                    ArenaCenter = new Vector2(target.Center.X, target.Center.Y - (18 * 16));
                }
            }
            if (TaggedPlayers == null) {
                TaggedPlayers = new List<Player>();
            }
            npc.alpha = 255;
            npc.Center = ArenaCenter;
            npc.rotation = 0f;
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                AI_State = (float)States.Spawning;
                npc.netUpdate = true;
            }
        }
    }
}
