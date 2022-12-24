using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    class RetinazerV2 : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
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

            NPC.value = 600000;
            NPC.aiStyle = -1;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Retinazer");
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;
            NPCID.Sets.TrailingMode[NPC.type] = 2;
        }

        int DeathLaserDamage = 20;
        int PiercingGazeDamage = 25;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer;
        RetMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<RetMove> MoveList;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveCounter
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public bool PhaseTwo
        {
            //get => true;
            get => transformationTimer >= 120;
        }
        public Player target
        {
            get => Main.player[NPC.target];
        }

        int MoveTimer = 0;
        int finalStandTimer = 0;
        float rotationTarget;
        float rotationSpeed;
        public override void AI()
        {
            if (NPC.realLife < 0)
            {
                int? catID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Cataluminance>());

                if (catID != null) {
                    NPC.realLife = catID.Value;
                }
            }

            if (NPC.realLife >= 0 && !Main.npc[NPC.realLife].active)
            {
                OnKill();
                NPC.active = false;
            }
            else
            {
                NPC.life = Main.npc[NPC.realLife].life;
                NPC.target = Main.npc[NPC.realLife].target;
            }

            MoveTimer++;
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);
            FindFrame(0);

            if (NPC.Distance(target.Center) > 4000)
            {
                NPC.Center = Main.player[NPC.target].Center + new Vector2(-1000, 0);
                UsefulFunctions.BroadcastText("Retinazer Closes In...");
            }

            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();


            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }

            if (NPC.life < NPC.lifeMax / 2 && transformationTimer < 120)
            {
                Transform();
                return;
            }

            //Switch into final stand if lower than 10% health
            if (NPC.life < NPC.lifeMax / 10f)
            {
                finalStandTimer++;
                if (finalStandTimer < 60)
                {
                    NPC.velocity *= 0.99f;
                    //Activate auras
                }
                else
                {
                    FinalStand();
                }

                return;
            }

            if (MoveTimer < 900)
            {
                CurrentMove.Move();
            }
            else if (MoveTimer < 960)
            {
                //Phase transition
                NPC.velocity *= 0.99f;
            }
            else
            {
                NextAttack();
            }
        }

        bool aimingDown;
        int laserCountdown = 0;
        float spinDirection = 0;
        void FireSupport()
        {
            if (PhaseTwo)
            {

                if (MoveTimer == 1)
                {
                    UsefulFunctions.BroadcastText("Scorching heat radiates from Retinazer's hull...", Color.OrangeRed);
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
                }

                //Fire laser
                if (MoveTimer == 330)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.IncineratingGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI);
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

                if (MoveTimer % laserCooldown < 30 && MoveTimer < 750)
                {
                    UsefulFunctions.DustRing(NPC.Center, (30 - MoveTimer % laserCooldown) * 20, DustID.GemRuby, 100, 2);
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
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
                    }
                    aimingDown = !aimingDown;

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
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            //Telegraph for the first second before the starting charge
            if (MoveTimer < 75)
            {
                UsefulFunctions.DustRing(NPC.Center, (75 - MoveTimer) * 30, DustID.GemRuby, 100, 10);
                return;
            }

            if (PhaseTwo)
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 0.15f, 20, null, false);
                rotationSpeed = 0.06f;


                //Fire
                if (MoveTimer % 40 == 0 && Main.netMode != NetmodeID.MultiplayerClient && MoveTimer < 800)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI + 1000, NPC.whoAmI);
                }
                
            }
            else
            {
                rotationSpeed = 0.2f;

                //Telegraph before each charge
                if (MoveTimer % 90 < 15)
                {
                    UsefulFunctions.DustRing(NPC.Center, (15 - MoveTimer % 90) * 20, DustID.GemRuby, 100, 2);
                }

                //Charge
                if (MoveTimer % 90 == 15)
                {
                    NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 15);
                }

                //Fire
                else if (MoveTimer % 90 > 15 && MoveTimer % 90 < 35 && MoveTimer % 3 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2();
                        projVector.Normalize();
                        projVector *= 10;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVector * 3, ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
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

                if (MoveTimer % 200 < 30 && MoveTimer < 750)
                {
                    UsefulFunctions.DustRing(NPC.Center, (30 - MoveTimer % 200) * 20, DustID.GemRuby, 100, 2);
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
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
                    }
                    aimingDown = !aimingDown;

                }

                if (MoveTimer % 200 < 120 && MoveTimer % 200 > 60 && MoveTimer < 850 && MoveTimer % 3 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //for (int i = 0; i < 5; i++)
                    {
                        //float angle = (i * MathHelper.PiOver4);
                        if (MoveTimer % 2 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(12, 0).RotatedBy(NPC.rotation + -MathHelper.PiOver4 / 2f + MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(12, 0).RotatedBy(NPC.rotation + MathHelper.PiOver4 / 2f + MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
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
                rotationSpeed = 0.012f;

                //Lock on on the first frame
                if (MoveTimer < 90)
                {
                    NPC.rotation = rotationTarget;
                }

                if (MoveTimer % 45 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (MoveTimer % 225 == 0 && MoveTimer <= 800 && MoveTimer >= 100)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), PiercingGazeDamage, 0.5f, Main.myPlayer, target.whoAmI + 1000, NPC.whoAmI);
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 12), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetDeathLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer);
                }
            }
        }

        bool spawnedLaser;
        int laserTimer;
        void FinalStand()
        {
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            //Normal movement when not charging laser
            if (!spawnedLaser)
            {
                spinDirection = 0;
                rotationSpeed = 0.2f;
                UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -400), 0.5f, 45);
                if(NPC.Distance(target.Center + new Vector2(0, -400)) < 100)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.IncineratingGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI, 1);
                    }
                    spawnedLaser = true;
                }
            }
            else
            {
                laserTimer++;
                Lighting.AddLight(NPC.Center / 16, Color.Red.ToVector3() * 10);
                float spinVelocity = 0.015f;
                rotationSpeed = 0;

                //Spin slower while targeting
                if (laserTimer < 216)
                {
                    spinVelocity /= 3f;
                }

                //Recoil
                if (laserTimer == 216)
                {
                    NPC.velocity += new Vector2(7, 0).RotatedBy(NPC.rotation - MathHelper.PiOver2);
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
            MoveCounter = 0;
        }
        float rotationVelocity;
        void Transform()
        {
            transformationTimer++;

            for(int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>())
                {
                    Main.projectile[i].Kill();
                }
            }

            if (transformationTimer <= 60)
            {
                rotationVelocity = transformationTimer / 60;
            }
            else
            {
                rotationVelocity = 1 - (transformationTimer / 60);
            }

            if(transformationTimer == 60 && !Main.dedServ)
            {
                //TODO spawn gore
            }
            MoveTimer = 0;
            NPC.rotation += rotationVelocity;
            NPC.velocity *= 0.95f;
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

            if (transformationTimer < 60)
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

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = NPC.frame;
            Vector2 origin = sourceRectangle.Size() / 2f;
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, origin, 1, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        //TODO: Copy vanilla death effects
        public override void OnKill()
        {
            if (!Main.dedServ)
            {

            }
        }
    }
}