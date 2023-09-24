using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Audio;
using System.IO;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Utilities;
using tsorcRevamp.Items.Placeable.Trophies;
using tsorcRevamp.Items.Vanity;
using tsorcRevamp.Items.Placeable.Relics;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Cataluminance : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            NPC.damage = 50;
            NPC.defense = 25;
            AnimationType = -1;
            NPC.lifeMax = 120000;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.width = 80;
            NPC.height = 80;
            NPC.HitSound = SoundID.NPCHit42;

            NPC.value = 600000;
            NPC.aiStyle = -1;

            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Cataluminance.DespawnHandler"), Color.Cyan, 180);
            InitializeMoves();
        }


        int StarBlastDamage = 35;
        int FinalStandStarDamage = 30;
        int TrailDamage = 50;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        public float transformationTimer = 0;
        CataMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<CataMove> MoveList;
        public static int secondStageHeadSlot = -1;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public bool PhaseTwo
        {
            get => transformed;
        }

        public Player target
        {
            get => Main.player[NPC.target];
        }

        int finalStandLevel = 0;
        int finalStandTimer = 0;
        int finalStandDelay = 0;
        float rotationTarget = 0;
        float rotationSpeed = 0.05f;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            MoveTimer++;
            HandleAura();
            FindFrame(0);
            if (despawnHandler.TargetAndDespawn(NPC.whoAmI))
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 0, UsefulFunctions.ColorToFloat(Color.White));
                int? retID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<RetinazerV2>());
                if (retID.HasValue)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        int dustID = Dust.NewDust(Main.npc[retID.Value].position, Main.npc[retID.Value].width, Main.npc[retID.Value].height, DustID.RedTorch, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                        Main.dust[dustID].noGravity = true;
                    }
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.npc[retID.Value].Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 0, UsefulFunctions.ColorToFloat(Color.White));
                }
                int? spazID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<SpazmatismV2>());
                if (spazID.HasValue)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        int dustID = Dust.NewDust(Main.npc[spazID.Value].position, Main.npc[spazID.Value].width, Main.npc[spazID.Value].height, DustID.CursedTorch, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                        Main.dust[dustID].noGravity = true;
                    }
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.npc[spazID.Value].Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 0, UsefulFunctions.ColorToFloat(Color.White));
                }
            }

            //This will be changed by other attacks
            NPC.damage = 0;

            //Prevents time from flowing past midnight while the boss is alive
            Main.dayTime = false;
            if(Main.time > 16240.0)
            {
                Main.time = 16240.0;
            }

            //Periodically sync
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 30 == 0)
            {
                NPC.netUpdate = true;
            }

            //Smart rotation
            //The conversions are necessary to avoid some XNA rotation bullshit
            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            //This exists because I wanted to make the fight far faster paced than even supersonic wings 1 allows
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.FasterThanSight>(), 5);
                }
            }

            if (!HandleRealLife())
            {
                return;
            }

            //Teleport if too far away
            if (NPC.Distance(target.Center) > 4000 && finalStandTimer == 0)
            {
                NPC.Center = target.Center + new Vector2(0, -1000);
                NPC.netUpdate = true;
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Cataluminance.ClosesIn"));
            }

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }            

            if (MoveTimer < 900)
            {
                CurrentMove.Move();
            }
            else if (MoveTimer < 960)
            {
                //Phase transition
                if (MoveTimer == 901)
                {
                    StartAura(800, 1.08f, 0.07f);
                }
                NPC.velocity *= 0.99f;
            }
            else
            {
                NextAttack();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.rotation);
            writer.Write(rotationTarget);
            writer.Write(finalStandTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.rotation = reader.ReadSingle();
            rotationTarget = reader.ReadSingle();
            finalStandTimer = reader.ReadInt32();
        }

        bool transformed;
        bool HandleRealLife()
        {
            if (transformationTimer <= 0)
            {
                NPC.dontTakeDamage = false;
            }
            else
            {
                NPC.dontTakeDamage = true;
            }

            //Handle phase change and transformation
            if (NPC.life < NPC.lifeMax * 2f / 3f && (!transformed || transformationTimer > 0))
            {
                Transform();
                return false;
            }

            //Switch into final stand
            if (NPC.life < NPC.lifeMax / 4f)
            {
                if (finalStandTimer == 0 && finalStandLevel == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Cataluminance.Desperation"), Color.Cyan);
                    UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.CataluminanceTrail>());
                }

                if (finalStandLevel == 0 && !NPC.AnyNPCs(ModContent.NPCType<RetinazerV2>()))
                {
                    UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.IncineratingGaze>());
                    UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.MaliciousGaze>());
                    UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.BlindingGaze>());
                    finalStandDelay = 0;
                    finalStandTimer = 0;
                    finalStandLevel = 1;
                }
                if (finalStandLevel == 1 && !NPC.AnyNPCs(ModContent.NPCType<SpazmatismV2>()))
                {
                    NPC.dontTakeDamage = true;
                    StartAura(800);
                    finalStandDelay = -60;
                    finalStandTimer = 0;
                    finalStandLevel = 2;
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Cataluminance.Light"), Color.DeepPink);
                }

                finalStandTimer++;

                //Stunned for a second
                if (finalStandDelay < 60)
                {
                    NPC.dontTakeDamage = true;
                    finalStandDelay++;
                    NPC.velocity *= 0.99f;
                    MoveTimer = 0;
                }
                else
                {
                    if (finalStandLevel == 0)
                    {
                        NPC.dontTakeDamage = false;
                        FinalStand();
                    }
                    else if (finalStandLevel == 1)
                    {
                        NPC.dontTakeDamage = false;
                        FinalFinalStand();
                    }
                    else
                    {
                        FinalFinalFinalStand();
                    }
                }

                return false;
            }

            return true;
        }

        //Hover to the upper right of the screen and spam homing blasts that chase the player
        void StarBlasts()
        {
            rotationTarget = Vector2.Normalize(NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            rotationSpeed = 0.05f;

            Vector2 homingTarget = new Vector2(750, -350);
            if(NPC.Center.X < target.Center.X)
            {
                homingTarget.X *= -1;
            }
            UsefulFunctions.SmoothHoming(NPC, target.Center + homingTarget, 0.5f, 20, target.velocity);

            if (PhaseTwo)
            {
                if (MoveTimer % 110 == 0 && MoveTimer < 850)
                {
                    baseFade = 0.1f;
                    baseRadius = .4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(60, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 1);
                    }
                }
            }
            else
            {
                if (MoveTimer % 60 == 0 && MoveTimer < 850 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    baseFade = 0.2f;
                    baseRadius = .3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(60, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0);
                    }
                }
            }

            //Clear all homing stars when attack is over
            if (MoveTimer == 899)
            {
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>());
            }
        }

        //Chase the player rapidly and smoothly, leaving a damaging trail in its wake that obstructs movement
        void Pursuit()
        {
            NPC.damage = 60;
            baseFade = 0.0f;
            baseRadius = .5f;
            rotationTarget = NPC.velocity.ToRotation() - MathHelper.PiOver2;
            rotationSpeed = 0.1f;

            if (MoveTimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.Enemy.Triad.CataluminanceTrail>(), TrailDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }
            float homingStrength = 0.17f;
            if (PhaseTwo)
            {
                homingStrength = 0.10f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (MoveTimer % 150 == 0)
                    {
                        StartAura(800);
                    }
                    if (MoveTimer % 150 == 30 && MoveTimer > 76)
                    {
                        NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 15);
                        NPC.netUpdate = true;
                    }
                }
            }

            UsefulFunctions.SmoothHoming(NPC, target.Center, homingStrength, 15, target.velocity, false);
        }

        float angle = 0;
        void Starstorm()
        {
            rotationTarget = MathHelper.Pi;
            rotationSpeed = 0.1f;

            if (MoveTimer == 0)
            {
                angle = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
            }

            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -350), 0.6f, 15);


            //In phase 2 the stars leave damaging trails like EoL, but there are fewer of them
            if (PhaseTwo)
            {
                if (MoveTimer % 15 == 0)
                {
                    baseFade = 0.5f;
                    baseRadius = .3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Stars fired upward for effect
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -60), new Vector2(Main.rand.NextFloat(-20, 20), -37).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 5);

                        //Stars rain down
                        Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-2000, 2000), -700);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(0, 7).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 4);
                    }
                }
            }
            else if (MoveTimer % 12 == 0)
            {
                baseFade = 0.5f;
                baseRadius = .3f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Stars fired upward for effect
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -60), new Vector2(Main.rand.NextFloat(-20, 20), -37).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 3);

                    //Stars rain down
                    Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-2500, 2500), -700);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(0, 7).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 2);
                }
            }            
        }

        float targetAngle;
        float laserCountdown;
        void FinalStand()
        {
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (laserCountdown == 0)
            {
                rotationSpeed = 0.3f;
                if (MoveTimer < 400)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -700).RotatedBy(targetAngle + (MathHelper.Pi * 4f / 3f)), 1f, 45);
                    return;
                }
                laserCountdown = 616;
                StartAura(650, 1.004f, 0.00f);
                targetAngle += MathHelper.Pi * 5f / 3f;
                
            }
            else
            {
                if(laserCountdown > 516)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -700).RotatedBy(targetAngle + (MathHelper.Pi * 4f / 3f)), 1f, 45);
                }
                laserCountdown--;
                NPC.velocity *= 0.95f;

                //Start reducing turn speed
                if (laserCountdown > 386)
                {
                    rotationSpeed = 0.3f;
                }
                else
                {
                    rotationSpeed = 0.02f;
                }

                //Start the countdown 30 frames early to make the boss prematurely slow down
                if (laserCountdown == 586)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.BlindingGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI, 2);
                    }
                }

                //Recoil
                if (laserCountdown == 376)
                {
                    NPC.velocity += new Vector2(7, 0).RotatedBy(NPC.rotation - MathHelper.PiOver2);
                }
            }

            return;
        }

        public void FinalFinalStand()
        {
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (finalStandTimer == 61 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.Enemy.Triad.CataluminanceTrail>(), TrailDamage, 0, Main.myPlayer, 2, NPC.whoAmI);
            }

            UsefulFunctions.SmoothHoming(NPC, target.Center, 0.2f, 20, bufferZone: false);

            if (finalStandTimer == 900)
            {
                finalStandTimer = 0;
            }
        }

        bool clearedTrails = false;
        float fireRotation = MathHelper.PiOver4;
        float fireRotationRotation = MathHelper.PiOver4 / 4f;
        public void FinalFinalFinalStand()
        {
            if (Main.GameUpdateCount % 20 == 0 && Main.netMode != NetmodeID.Server)
            {
                Vector2 dustPoint = Main.rand.NextVector2Circular(50, 50);
                Vector2 dustVel = Vector2.Normalize(dustPoint);
                float dustAmount = 60;
                for (float i = 0; i < dustAmount; i++)
                {
                    int dustType = DustID.FireworkFountain_Blue;
                    if (Main.rand.NextBool())
                    {
                        dustType = DustID.FireworkFountain_Pink;
                    }
                    Dust.NewDustPerfect(dustPoint + NPC.Center, dustType, dustVel.RotatedBy(MathHelper.TwoPi * i / dustAmount) * Main.rand.NextFloat(3, 6), Scale: Main.rand.NextFloat(0, 1)).noGravity = true;
                }
                Gore.NewGorePerfect(NPC.GetSource_FromThis(), dustPoint + NPC.Center, Main.rand.NextVector2Circular(1, 1), GoreID.Smoke3);
            }
            Vector2 crystalPoint = NPC.Center + new Vector2(0, -100);
            NPC.dontTakeDamage = true;
            rotationTarget = MathHelper.Pi;
            baseRadius = 0.7f;

            if (!clearedTrails)
            {
                fireRotationRotation = 0;
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>());

                clearedTrails = true;
            }

            //Limit player radius
            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    if(Vector2.Distance(NPC.Center, Main.player[i].Center) > 1350)
                    {
                        Main.player[i].velocity = UsefulFunctions.Aim(Main.player[i].Center, NPC.Center, 6);
                    }
                }
            }

            if(finalStandTimer < 180)
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -300), 0.5f, 20);
                return;
            }
            if (finalStandTimer == 190)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.CataluminanceTrail>(), TrailDamage, 0, Main.myPlayer, 5);
                }
            }

            NPC.velocity *= 0.95f;
            if(NPC.velocity.Length() < 0.05f)
            {
                NPC.velocity = Vector2.Zero;
            }
            else
            {
                finalStandTimer--;
            }

            if (finalStandTimer > 2360f)
            {
                deathTimer++;
                HandleDeath();
                return;
            }

            if (finalStandTimer < 1300)
            {
                if (fireRotationRotation < 0.15)
                {
                    fireRotationRotation += 0.15f / 300f;
                }
                if(finalStandTimer % 120 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(60, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 10);
                    }
                }
                if (finalStandTimer % 20 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 firingVector = new Vector2(6, 0).RotatedBy(fireRotation + (MathHelper.PiOver2 * i));
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 6);
                        }
                    }
                    baseFade = 0f;
                    fireRotation += fireRotationRotation;
                }
                if (finalStandTimer > 300 && finalStandTimer % 20 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 firingVector = new Vector2(6, 0).RotatedBy(-fireRotation + (MathHelper.PiOver2 * i));
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 7);
                        }
                    }
                    baseFade = 0f;
                }
            }
            else
            {
                int period = 45 - (int)((finalStandTimer / 2600f) * 20f);
                if (finalStandTimer % period == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 firingVector = new Vector2(6, 0).RotatedBy(MathHelper.PiOver2 * Math.Sin(fireRotation));
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 8);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 9);
                        firingVector.X *= -1;
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 8);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 9);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, -firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 8);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, -firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 9);
                        firingVector.Y *= -1;
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 8);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), crystalPoint, firingVector, ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), FinalStandStarDamage, 0, Main.myPlayer, 9);
                    }
                    baseFade = 0f;
                    fireRotation += 0.5f;
                }
            }
        }

        float deathTimer = 0;
        void HandleDeath()
        {
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.95f;
            if(deathTimer == 30)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashPre") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:CatShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
            {
                float opacity = deathTimer / (animTime / 2);
                if (opacity > 1)
                {
                    opacity = 1;
                }

                float distancePercent = 1 - (deathTimer / animTime);
                Filters.Scene["tsorcRevamp:CatShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity).UseIntensity(0.2f);
            }

            float lightTimer = 20;
            if (deathTimer > 130)
            {
                lightTimer = 8;
            }
            lightCooldown--;
            if (lightCooldown <= 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(NPC.Hitbox), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.HotPink));
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(NPC.Hitbox), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Cyan));
                }
                lightCooldown = lightTimer;
            }

            //"Die." - Minos Prime, 2022
            if (deathTimer > 240)
            {
                if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
                {
                    Filters.Scene["tsorcRevamp:CatShockwave"].Deactivate();
                }
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.VFX.LightRay>());
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 2, UsefulFunctions.ColorToFloat(Color.HotPink));
                }
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashCut") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);

                NPC.dontTakeDamage = false;
                NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);

                NPC.downedMechBoss2 = true;
                NPC.downedMechBossAny = true;
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.Spazmatism)))
                {
                    tsorcRevampWorld.NewSlain.Add(new NPCDefinition(NPCID.Spazmatism), 1);
                }
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.Retinazer)))
                {
                    tsorcRevampWorld.NewSlain.Add(new NPCDefinition(NPCID.Retinazer), 1);
                }
            }
        }
        public override bool CheckDead()
        {
            if (finalStandLevel == 2 && finalStandTimer > 61)
            {
                return true;
            }
            else
            {
                NPC.life = 1000;
                return false;
            }
        }

        private void NextAttack()
        {
            MoveIndex++;
            if (MoveIndex > MoveList.Count - 1)
            {
                MoveIndex = 0;
            }

            MoveTimer = 0;
        }

        float lightCooldown;
        void Transform()
        {
            NPC.HitSound = SoundID.NPCHit4;
            MoveTimer = 0;
            NPC.velocity *= 0.95f;
            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:CatShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
            {
                float opacity = transformationTimer / (animTime / 2);
                if (opacity > 1)
                {
                    opacity = 1;
                }
                if (transformationTimer > animTime)
                {
                    opacity = animTime / transformationTimer;
                }
                float distancePercent = 1 - (transformationTimer / animTime);
                Filters.Scene["tsorcRevamp:CatShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity * opacity).UseIntensity(0.1f);
            }

            if (!transformed)
            {
                transformationTimer += 2;
            }
            else
            {
                transformationTimer -= 2;
                if (transformationTimer <= 0)
                {
                    if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
                    {
                        Filters.Scene["tsorcRevamp:CatShockwave"].Deactivate();
                    }
                }
            }

            if (transformationTimer > 240)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Cataluminance.Transformed"), Color.DeepPink);
                UsefulFunctions.SimpleGore(NPC, "Cataluminance_Gore_1");
                transformed = true;
            }
        }

        float fadeInPercent;
        void HandleAura()
        {
            if(fadeInPercent < 1)
            {
                fadeInPercent += 1f / 30f;
            }

            if (ringCollapse < 0.1f)
            {
                fadePercent += fadeSpeed;
            }
            else
            {
                ringCollapse /= collapseSpeed;
            }

            float intensityMinimum = 0.77f;
            float radiusMinimum = 0.25f;


            if(finalStandTimer > 0)
            {
                intensityMinimum = 0.45f;
                radiusMinimum = 0.4f;
            }

            if (baseFade < intensityMinimum)
            {
                baseFade += 0.02f;
            }
            if (baseFade > intensityMinimum)
            {
                baseFade = intensityMinimum;
            }
            if (baseRadius > radiusMinimum)
            {
                baseRadius -= 0.01f;
            }
            if (baseRadius < radiusMinimum)
            {
                baseRadius = radiusMinimum;
            }
        }

        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<CataMove> {
                new CataMove(StarBlasts, CataMoveID.StarBlasts, "Star Blasts"),
                new CataMove(Starstorm, CataMoveID.Pursuit, "Pursuit"),
                new CataMove(Pursuit, CataMoveID.Starstorm, "Starstorm"),
                };
        }

        private class CataMoveID
        {
            public const short StarBlasts = 0;
            public const short Starstorm = 1;
            public const short Pursuit = 2;
            public const short TBD = 3;
        }
        private class CataMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public CataMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }
        public override void BossHeadSlot(ref int index)
        {
            if (PhaseTwo)
            {
                index = secondStageHeadSlot;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int frameSize = 1;
            if (!Main.dedServ)
            {
                frameSize = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }

            NPC.frameCounter++;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + frameSize;
                NPC.frameCounter = 0.0;
            }

            if (!transformed)
            {
                if (NPC.frame.Y >= frameSize * Main.npcFrameCount[NPC.type] / 2f)
                {
                    NPC.frame.Y = 0;
                }
            }
            else
            {
                if (NPC.frame.Y >= frameSize * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = frameSize * Main.npcFrameCount[NPC.type] / 2;
                }
            }
        }
        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = NPC.Hitbox;
        }

        public static Texture2D texture;
        public static Texture2D crystalTexture;
        public Effect effect;
        float effectTimer;
        float ringCollapse;
        float fadePercent;
        float effectRadius = 650;
        float fadeSpeed = 0.05f;
        float collapseSpeed = 1.05f;
        float baseFade = 0.77f;
        float baseRadius = 0.25f;
        float colorTimer = 0;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);
            Vector3 hslColor = Main.rgbToHsl(new Color(0.1f, 0.5f, 1f));
            if (PhaseTwo)
            {
                hslColor = Main.rgbToHsl(new Color(1f, 0.3f, 0.85f));
            }
            if(finalStandLevel == 1)
            {
                hslColor = Main.rgbToHsl(Color.GreenYellow);
            }
            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);

            DrawAura(rgbColor);

            if(finalStandLevel == 2)
            {
                DrawFinalStandAttack();
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (crystalTexture == null || crystalTexture.IsDisposed)
            {
                crystalTexture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture + "_Crystal", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Rectangle sourceRectangle = NPC.frame;
            Vector2 origin = sourceRectangle.Size() / 2f;

            Color crystalColor = Color.Lerp(Color.DeepSkyBlue, new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Cos(colorTimer / 25f), 2));
            //crystalColor = UsefulFunctions.ShiftColor(crystalColor, colorTimer, 0.1f);
            colorTimer += 0.5f;

            Color lightingColor = Color.Lerp(Color.White, crystalColor, 0.5f);
            lightingColor = Color.Lerp(drawColor, lightingColor, 0.6f);
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle, lightingColor, NPC.rotation, origin, 1, SpriteEffects.None, 0f);
            
            if (PhaseTwo)
            {
                spriteBatch.Draw(crystalTexture, NPC.Center - Main.screenPosition, sourceRectangle, crystalColor, NPC.rotation, origin, 1, SpriteEffects.None, 0f);
            }

            DrawTransformationEffect();

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public void DrawAura(Color rgbColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(effectRadius / 0.7f), (int)(effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(ringCollapse);
            effect.Parameters["fadePercent"].SetValue(fadePercent + (1 - fadeInPercent));
            float timeFactor = 1;
            if (PhaseTwo)
            {
                timeFactor = 2.5f;
            }
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle baseRectangle = new Rectangle(0, 0, 500, 500);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;


            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(baseRadius);
            effect.Parameters["fadePercent"].SetValue(baseFade);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.PiOver2, baseOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        Effect FinalStandAttack;
        float starRotation;
        public void DrawFinalStandAttack()
        {
            Vector2 crystalPoint = NPC.Center + new Vector2(0, -100);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Vector3 hslColor1 = Main.rgbToHsl(new Color(0.1f, 0.5f, 1f));
            Vector3 hslColor2 = Main.rgbToHsl(new Color(1f, 0.3f, 0.85f));
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Rectangle baseRectangle = new Rectangle(0, 0, 500, 500);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;


            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            effect.Parameters["ringProgress"].SetValue(0.5f);
            effect.Parameters["fadePercent"].SetValue(0);
            effect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, crystalPoint - Main.screenPosition, baseRectangle, Color.White, MathHelper.PiOver2, baseOrigin, NPC.scale, SpriteEffects.None, 0);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                FinalStandAttack = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 600, 600);
            if (finalStandLevel == 2 && finalStandTimer > 1300)
            {
                starRectangle = new Rectangle(0, 0, finalStandTimer - 700, finalStandTimer - 700);
            }
            float attackFadePercent = 0;
            if(finalStandTimer < 60)
            {
                attackFadePercent = (float)Math.Pow(1 - (finalStandTimer / 60f), 2);
                starRectangle.Width = (int)(starRectangle.Width * (1 - attackFadePercent));
                starRectangle.Height = (int)(starRectangle.Height * (1 - attackFadePercent));
            }

            if(finalStandTimer > 2360)
            {
                attackFadePercent = (finalStandTimer - 2360f) / 120f;
                if(attackFadePercent > 1)
                {
                    attackFadePercent = 1;
                }
            }

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            FinalStandAttack.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            FinalStandAttack.Parameters["effectSize"].SetValue(starRectangle.Size());
            FinalStandAttack.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            FinalStandAttack.Parameters["ringProgress"].SetValue(0.5f);
            FinalStandAttack.Parameters["fadePercent"].SetValue(attackFadePercent);
            FinalStandAttack.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            FinalStandAttack.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, crystalPoint - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            
            //Pass relevant data to the shader via these parameters
            FinalStandAttack.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            FinalStandAttack.Parameters["effectSize"].SetValue(starRectangle.Size());
            FinalStandAttack.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            FinalStandAttack.Parameters["ringProgress"].SetValue(0.5f);
            FinalStandAttack.Parameters["fadePercent"].SetValue(attackFadePercent);
            FinalStandAttack.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            FinalStandAttack.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, crystalPoint - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        Effect TransformationEffect;
        float transStarRotation;
        public void DrawTransformationEffect()
        {

            Vector3 hslColor1 = Main.rgbToHsl(new Color(0.1f, 0.5f, 1f));
            Vector3 hslColor2 = Main.rgbToHsl(new Color(1f, 0.3f, 0.85f));
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                TransformationEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            transStarRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 4, 4);
            float attackFadePercent = (float)Math.Pow(1 - (transformationTimer / 60f), 2);
            if (transformationTimer > 60)
            {
                attackFadePercent = 0;
            }
            starRectangle.Width = (int)(starRectangle.Width * transformationTimer);
            starRectangle.Height = (int)(starRectangle.Height * transformationTimer);

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, transStarRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, -transStarRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public void StartAura(float radius, float ringSpeed = 1.05f, float fadeOutSpeed = 0.05f)
        {
            effectRadius = radius;
            collapseSpeed = ringSpeed;
            fadeSpeed = fadeOutSpeed;
            fadePercent = 0;
            ringCollapse = 1;
            fadeInPercent = 0;
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        { 
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TriadBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedCrystal>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedFlameNozzle>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedLaser>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedRemote>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrestOfSky>(), 1, 3, 3));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.HallowedBar, 1, 25, 40));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofSight, 1, 20, 40));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TheTriadMask>(), 7));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<TheTriadRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CataluminanceTrophy>(), 10));
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Cataluminance_Gore_2");
            UsefulFunctions.SimpleGore(NPC, "Cataluminance_Gore_3");
            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:CatShockwave"].IsActive())
            {
                Filters.Scene["tsorcRevamp:CatShockwave"].Deactivate();
            }
        }
    }
}