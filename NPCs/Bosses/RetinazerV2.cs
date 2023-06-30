using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Utilities;
using tsorcRevamp.Items.Placeable.Trophies;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class RetinazerV2 : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.OnFire,
                    BuffID.Poisoned,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.damage = 50;
            NPC.defense = 25;
            AnimationType = -1;
            NPC.lifeMax = 90000;
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

            InitializeMoves();
        }


        int DeathLaserDamage = 27;
        int PiercingGazeDamage = 35;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer;
        RetMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<RetMove> MoveList;
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
            //get => true;
            get => transformed;
        }
        public Player target
        {
            get => Main.player[NPC.target];
        }

        int finalStandTimer = 0;
        float rotationTarget;
        float rotationSpeed;
        int finalStandDelay = 0;
        int deathTimer;
        float lightCooldown;
        bool transformed = false;
        public override void AI()
        {
            MoveTimer++;
            HandleAura();
            FindFrame(0);

            //This will be changed by other attacks
            NPC.damage = 0;

            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 30 == 1)
            {
                NPC.netUpdate = true;
            }

            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            if (!HandleRealLife())
            {
                return;
            }

            if (NPC.Distance(target.Center) > 4000 && finalStandTimer == 0)
            {
                NPC.Center = Main.player[NPC.target].Center + new Vector2(-1000, 0);
                NPC.netUpdate = true;
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.RetinazerV2.ClosesIn"));
            }

            if (MoveList == null)
            {
                InitializeMoves();
            }
            if (testAttack != -1)
            {
                MoveIndex = testAttack;
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
                    fadePercentage = 0.3f;
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

        bool aimingDown;
        int laserCountdown = 0;
        float spinDirection = 0;
        void FireSupport()
        {
            if (PhaseTwo)
            {
                baseFade = 0;
                baseRadius = 0.5f;
                if (MoveTimer == 1)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.RetinazerV2.Warning"), Color.OrangeRed);
                }

                rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

                //Normal movement when not charging laser
                if (laserCountdown == 0)
                {
                    spinDirection = 0;
                    rotationSpeed = 0.2f;
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -400), 0.4f, 35);
                }
                else
                {
                    Lighting.AddLight(NPC.Center / 16, Color.Red.ToVector3() * 10);
                    float spinVelocity = 0.015f;
                    rotationSpeed = 0;

                    //Spin slower while targeting
                    if (laserCountdown > 376)
                    {
                        spinVelocity /= 3f;
                    }

                    //Check if left or right is the closest
                    Vector2 offset = new Vector2(10, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2);

                    //If less than one turn unit from being aligned, just align it (stops weird vibration)
                    if (Math.Abs(NPC.rotation - rotationTarget) < (spinVelocity / 2f) || Math.Abs(Math.Abs(NPC.rotation - rotationTarget) - MathHelper.TwoPi) < (spinVelocity / 2f))
                    {
                        NPC.rotation = rotationTarget;
                    }
                    else
                    {
                        if (Vector2.Distance(NPC.Center + offset.RotatedBy(0.1), target.Center) < Vector2.Distance(NPC.Center + offset.RotatedBy(-0.1), target.Center))
                        {
                            spinDirection += 0.07f;
                            if (spinDirection > 1)
                            {
                                spinDirection = 1;
                            }
                        }
                        else
                        {
                            spinDirection -= 0.07f;
                            if (spinDirection < -1)
                            {
                                spinDirection = -1;
                            }
                        }
                        NPC.rotation += spinVelocity * spinDirection;
                    }

                    laserCountdown--;

                    //Slow down while charging/firing
                    NPC.velocity *= 0.95f;

                    //Recoil
                    if (laserCountdown == 376)
                    {
                        NPC.velocity += new Vector2(7, 0).RotatedBy(NPC.rotation - MathHelper.PiOver2);
                    }
                }

                //Start the countdown 30 frames early to make the boss prematurely slow down
                if (MoveTimer == 300)
                {
                    laserCountdown = 616;
                    StartAura(650, 1.007f, 0.00f);
                }

                //Fire laser
                if (MoveTimer == 330)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.IncineratingGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI);
                    }
                }
            }
            else
            {
                Vector2 targetPoint;
                if (NPC.Center.X < target.Center.X)
                {
                    targetPoint = Main.player[NPC.target].Center + new Vector2(-700, 0);
                }
                else
                {
                    targetPoint = Main.player[NPC.target].Center + new Vector2(700, 0);
                }
                UsefulFunctions.SmoothHoming(NPC, targetPoint, 0.7f, 20, null, true);
                rotationSpeed = 0.03f;
                if (MoveTimer == 1)
                {
                    aimingDown = true;
                    rotationTarget = -MathHelper.PiOver4 - MathHelper.PiOver2;
                    NPC.rotation = -MathHelper.PiOver4 - MathHelper.PiOver2;
                    if(NPC.Center.X > target.Center.X)
                    {
                        rotationTarget *= -1;
                        NPC.rotation *= -1;
                    }
                }
                float laserCooldown = 200;

                if (MoveTimer % laserCooldown == 10 && MoveTimer < 750)
                {
                    StartAura(500);
                }
                if (MoveTimer % laserCooldown == 60)
                {
                    if (aimingDown)
                    {
                        rotationTarget = -MathHelper.PiOver4 - MathHelper.PiOver2;
                    }
                    else
                    {
                        rotationTarget = -MathHelper.PiOver4;
                    }
                    if(target.Center.X < NPC.Center.X)
                    {
                        rotationTarget *= -1;
                    }
                }
                if (MoveTimer % laserCooldown == 30 && MoveTimer < 750)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
                    }
                    aimingDown = !aimingDown;
                }

                if(MoveTimer % laserCooldown > 30 && MoveTimer % laserCooldown < 90 && MoveTimer < 750)
                {
                    baseFade = 0;
                    baseRadius = 0.35f;
                }
            }

            if(MoveTimer == 870)
            {
                rotationSpeed = 0.2f;
                rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            }
        }

        //Dashes at (or maybe intentionally past?) the player, aiming at them and firing a barrage of lasers as it does
        //Phase 2: Piercing laser instead of death lasers
        void Charging()
        {
            NPC.damage = 80;
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (PhaseTwo)
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 0.15f, 20, null, false);
                rotationSpeed = 0.06f;

                //Fire
                if (MoveTimer % 40 == 0 && MoveTimer < 800)
                {
                    baseFade = 0.3f;
                    baseRadius = 0.3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI + 1000, NPC.whoAmI);
                    }
                }
            }
            else
            {
                rotationSpeed = 0.2f;

                //Telegraph before each charge
                if (MoveTimer % 90 == 75 && MoveTimer < 850)
                {
                    StartAura(400, 1.06f, 0.07f);
                    fadePercentage = 0.3f;
                }

                //Charge
                if (MoveTimer % 90 == 15)
                {
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 15);
                    NPC.netUpdate = true;
                }

                //Fire
                else if (MoveTimer % 90 > 15 && MoveTimer % 90 < 35 && MoveTimer % 3 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2();
                        projVector.Normalize();
                        projVector *= 10;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(100, 0).RotatedBy(NPC.velocity.ToRotation()), projVector * 3, ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
        }

        //Hovers top right of the player and fires hitscan lingering lasers repeatedly
        //Phase 2: Comes to a stop and then fires OHKO laser continuously, requiring orbiting the boss to survive
        void Firing()
        {
            if (PhaseTwo)
            {
                Vector2 targetOffset;
                if (NPC.Center.X < target.Center.X)
                {
                    targetOffset = new Vector2(-700, -350);
                }
                else
                {
                    targetOffset = new Vector2(700, -350);
                }
                UsefulFunctions.SmoothHoming(NPC, target.Center + targetOffset, 0.9f, 25);

                rotationSpeed = 0.03f;
                if (MoveTimer == 1)
                {
                    aimingDown = true;
                    rotationTarget = -MathHelper.PiOver2;
                    NPC.rotation = -MathHelper.PiOver2;
                    if (NPC.Center.X > target.Center.X)
                    {
                        rotationTarget *= -1;
                        NPC.rotation *= -1;
                    }
                }

                if (MoveTimer % 200 == 30 && MoveTimer < 750)
                {
                    StartAura(400);
                }
                if (MoveTimer % 200 == 60)
                {
                    if (aimingDown)
                    {
                        rotationTarget = -MathHelper.PiOver2;
                    }
                    else
                    {
                        rotationTarget = 0;
                    }
                    if (target.Center.X < NPC.Center.X)
                    {
                        rotationTarget *= -1;
                    }
                }
                if (MoveTimer % 200 == 30 && MoveTimer < 750)
                {
                    baseFade = 0.3f;
                    baseRadius = 0.3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(100, 0).RotatedBy(NPC.velocity.ToRotation()), UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
                    }
                    aimingDown = !aimingDown;
                }

                if (MoveTimer % 200 < 120 && MoveTimer % 200 > 60 && MoveTimer < 850 && MoveTimer % 3 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        baseFade = 0.3f;
                        baseRadius = 0.3f;
                        if (MoveTimer % 2 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(40, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), new Vector2(12, 0).RotatedBy(NPC.rotation + -MathHelper.PiOver4 / 2f + MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(40, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), new Vector2(12, 0).RotatedBy(NPC.rotation + MathHelper.PiOver4 / 2f + MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                        }
                    }      
                }
            }
            else
            {
                Vector2 targetOffset;
                if (NPC.Center.X < target.Center.X)
                {
                    targetOffset = new Vector2(-500, -350);
                }
                else
                {
                    targetOffset = new Vector2(500, -350);
                }
                UsefulFunctions.SmoothHoming(NPC, target.Center + targetOffset, 0.7f, 20);

                rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                rotationSpeed = 0.12f;

                //Lock on on the first frame
                if (MoveTimer < 90)
                {
                    NPC.rotation = rotationTarget;
                }

                if (MoveTimer % 45 == 0)
                {
                    baseFade = 0.3f;
                    baseRadius = 0.3f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(40, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), UsefulFunctions.Aim(NPC.Center, target.Center, 12) + target.velocity / 2f, ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
        }

        float targetAngle = -MathHelper.Pi * 5f / 3f;
        void FinalStand()
        {
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (Main.GameUpdateCount % 20 == 0 && Main.netMode != NetmodeID.Server)
            {
                Vector2 dustPoint = Main.rand.NextVector2Circular(50, 50);
                Vector2 dustVel = Vector2.Normalize(dustPoint);
                float dustAmount = 30;
                for (float i = 0; i < dustAmount; i++)
                {
                    Dust.NewDustPerfect(dustPoint + NPC.Center, DustID.Torch, dustVel.RotatedBy(MathHelper.TwoPi * i / dustAmount) * Main.rand.NextFloat(0, 1), Scale: Main.rand.NextFloat(1, 3));
                }
                Gore.NewGorePerfect(NPC.GetSource_FromThis(), dustPoint + NPC.Center, Main.rand.NextVector2Circular(1, 1), GoreID.Smoke3);
            }

            //Clean up
            if (finalStandTimer == 1)
            {
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.IncineratingGaze>());
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.MaliciousGaze>());
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Triad.BlindingGaze>());
            }

            if (laserCountdown == 0)
            {
                rotationSpeed = 0.3f;
                laserCountdown = 616;
                StartAura(650, 1.004f, 0.00f);
                targetAngle += MathHelper.Pi * 5f / 3f;
            }
            else
            {
                if (laserCountdown > 516)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -700).RotatedBy(targetAngle), 2f, 45);
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
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.IncineratingGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI);
                    }
                }

                //Recoil
                if (laserCountdown == 376)
                {
                    NPC.StrikeNPC(NPC.CalculateHitInfo(999, 1, true, 0), false, false);
                    NPC.velocity += new Vector2(7, 0).RotatedBy(NPC.rotation - MathHelper.PiOver2);
                }
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

        void Transform()
        {
            NPC.HitSound = SoundID.NPCHit4;
            MoveTimer = 0;
            NPC.velocity *= 0.95f;
            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:RetShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
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
                Filters.Scene["tsorcRevamp:RetShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity * opacity).UseIntensity(0.1f);
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
                    if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
                    {
                        Filters.Scene["tsorcRevamp:RetShockwave"].Deactivate();
                    }
                }
            }

            if (transformationTimer > 240)
            {
                UsefulFunctions.SimpleGore(NPC, "Retinazer_Gore_1");
                transformed = true;
            }

            if (!transformed)
            {
                NPC.dontTakeDamage = true;
            }
        }
        float fadeInPercent;
        void HandleAura()
        {
            if (fadeInPercent < 1)
            {
                fadeInPercent += 1f / 30f;
            }
            if (ringCollapse < 0.1f)
            {
                fadePercentage += fadeSpeed;
            }
            else
            {
                ringCollapse /= collapseSpeed;
            }

            float intensityMinimum = 0.77f;
            float radiusMinimum = 0.25f;


            if (finalStandTimer > 0)
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

        bool HandleRealLife()
        {
            if (NPC.realLife < 0)
            {
                int? catID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<Cataluminance>());

                if (catID != null)
                {
                    NPC.realLife = catID.Value;
                    NPC.netUpdate = true;
                }
            }

            if (NPC.realLife >= 0 && !Main.npc[NPC.realLife].active)
            {
                OnKill();
                NPC.active = false;
            }
            else if(NPC.realLife >= 0)
            {
                NPC.life = Main.npc[NPC.realLife].life;
                NPC.lifeMax = Main.npc[NPC.realLife].lifeMax;
                NPC.target = Main.npc[NPC.realLife].target;
            }

            //Initiate death at this health gate
            if (NPC.life < NPC.lifeMax * 0.15f)
            {
                HandleDeath();
                deathTimer++;
                return false;
            }

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
            //if(true)
            if (NPC.life < NPC.lifeMax / 4f)
            {
                finalStandTimer++;
                if (finalStandDelay < 60)
                {
                    NPC.dontTakeDamage = true;
                    finalStandDelay++;
                    NPC.velocity *= 0.99f;
                }
                else
                {
                    NPC.dontTakeDamage = false;
                    FinalStand();
                }

                return false;
            }

            return true;
        }

        /*
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return base.CanBeHitByProjectile(projectile);
        }*/

        void HandleDeath()
        {
            //NPC.ShowNameOnHover = false;
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.95f;

            if (deathTimer == 30)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashPre") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:RetShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
            {
                float opacity = deathTimer / (animTime / 2);
                if (opacity > 1)
                {
                    opacity = 1;
                }

                float distancePercent = 1 - (deathTimer / animTime);
                Filters.Scene["tsorcRevamp:RetShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity).UseIntensity(0.2f);
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
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(NPC.Hitbox), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Red));
                }
                lightCooldown = lightTimer;
            }

            //"Die." - Minos Prime, 2022
            if (deathTimer > 240)
            {
                UsefulFunctions.BroadcastText(NPC.TypeName + " has been defeated!", Color.Red);
                if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
                {
                    Filters.Scene["tsorcRevamp:RetShockwave"].Deactivate();
                }
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.VFX.LightRay>());
                deathTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.Red));
                }
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashCut") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);

                OnKill();
                NPC.dontTakeDamage = false;
                NPC.realLife = -1;
                NPC.life = 0;
                for (int i = 0; i < 10; i++)
                {
                    CombatText.NewText(NPC.Hitbox, CombatText.DamagedHostile, 9999999, true);
                }
            }
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if(deathTimer > 0)
            {
                 return false;
            }

            return base.CanBeHitByProjectile(projectile);
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            if (deathTimer > 0)
            {
                return false;
            }

            return base.CanBeHitByItem(player, item);
        }

        public override bool CheckDead()
        {
            if (deathTimer > 0)
            {
                return true;
            }
            else
            {
                NPC.life = 1000;
                return false;
            }
        }

        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<RetMove> {
                new RetMove(FireSupport, RetMoveID.FireSupport, "Fire Support"),
                new RetMove(Charging, RetMoveID.Charging, "Charging"),
                new RetMove(Firing, RetMoveID.Firing, "Firing"),
                };
        }

        private class RetMoveID
        {
            public const short FireSupport = 0;
            public const short Firing = 1;
            public const short Charging = 2;
            public const short TBD = 3;
        }
        private class RetMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public RetMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
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
        public Effect effect;
        float effectTimer;
        float ringCollapse;
        float fadePercentage;
        float effectRadius = 650;
        float fadeSpeed;
        float collapseSpeed;
        float baseFade = 0.77f;
        float baseRadius = 0.25f;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);
            effectRadius = 650;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/RetAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(effectRadius / 0.7f), (int)(effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector3 hslColor = Main.rgbToHsl(Color.Red);
            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(ringCollapse);
            effect.Parameters["fadePercent"].SetValue(fadePercentage + (1 - fadeInPercent));
            effect.Parameters["scaleFactor"].SetValue(.5f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle baseRectangle = new Rectangle(0, 0, 500, 500);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;


            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(baseRadius);
            effect.Parameters["fadePercent"].SetValue(baseFade);
            effect.Parameters["scaleFactor"].SetValue(.5f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.5f);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, baseRectangle, Color.White, 0, baseOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);



            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }

            Color lightingColor = Color.Lerp(Color.White, rgbColor, 0.5f);
            lightingColor = Color.Lerp(drawColor, lightingColor, 0.5f);
            Rectangle sourceRectangle2 = NPC.frame;
            Vector2 origin2 = sourceRectangle2.Size() / 2f;
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle2, lightingColor, NPC.rotation, origin2, 1, SpriteEffects.None, 0f);

            DrawTransformation();
            DrawDeathEffect();
            return false;
        }

        public void DrawDeathEffect()
        {

            Vector3 hslColor1 = Main.rgbToHsl(Color.Red);
            Vector3 hslColor2 = Main.rgbToHsl(Color.White);
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

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 8, 8);
            float attackFadePercent = (float)Math.Pow(1 - (deathTimer / 60f), 2);
            if (deathTimer > 60)
            {
                attackFadePercent = 0;
            }
            starRectangle.Width = (int)(starRectangle.Width * deathTimer);
            starRectangle.Height = (int)(starRectangle.Height * deathTimer);

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

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

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

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        Effect TransformationEffect;
        float starRotation;
        public void DrawTransformation()
        {

            Vector3 hslColor1 = Main.rgbToHsl(Color.Red);
            Vector3 hslColor2 = Main.rgbToHsl(Color.White);
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

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 4, 4);
            float attackFadePercent = (float)Math.Pow(1 - (transformationTimer / 60f), 2);
            if(transformationTimer > 60)
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

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

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

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public void StartAura(float radius, float ringSpeed = 1.05f, float fadeOutSpeed = 0.05f)
        {
            effectRadius = radius;
            collapseSpeed = ringSpeed;
            fadeSpeed = fadeOutSpeed;
            fadePercentage = 0;
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RetinazerTrophy>(), 10));
        }

        //TODO: Copy vanilla death effects
        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Retinazer_Gore_2");
            UsefulFunctions.SimpleGore(NPC, "Retinazer_Gore_3");

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:RetShockwave"].IsActive())
            {
                Filters.Scene["tsorcRevamp:RetShockwave"].Deactivate();
            }
        }
    }
}