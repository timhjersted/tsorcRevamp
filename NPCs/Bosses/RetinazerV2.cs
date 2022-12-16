using Microsoft.Xna.Framework;
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
            NPC.damage = 30;
            NPC.defense = 25;
            AnimationType = -1;
            NPC.lifeMax = (int)(32500 * (1 + (0.25f * (Main.CurrentFrameFlags.ActivePlayersCount - 1))));
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

            despawnHandler = new NPCDespawnHandler("", Color.Cyan, 180);
            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Retinazer");
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;
            NPCID.Sets.TrailingMode[NPC.type] = 2;
        }

        int DeathLaserDamage = 25;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer;
        CataMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<CataMove> MoveList;

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
            get => transformationTimer >= 120;
        }
        public Player target
        {
            get => Main.player[NPC.target];
        }

        int MoveTimer = 0;
        NPCDespawnHandler despawnHandler;

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

            //Main.NewText("Ret: " + CurrentMove.Name + " at " + MoveTimer);
            MoveTimer++;
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);
            FindFrame(0); 

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

            CurrentMove.Move();

            if (MoveTimer >= 900)
            {
                NextAttack();
            }

            if (NPC.Distance(target.Center) > 8000)
            {
                NPC.Center = Main.player[NPC.target].Center + new Vector2(-1000, 0);
                UsefulFunctions.BroadcastText("Retinazer Closes In...");
            }
        }

        //Trails off behind the player, before firing a piercing laser repeatedly
        //Phase 2: OHKO laser, too big to dodgeroll though but turns slow enough to avoid
        int currentProjectile;
        bool aimingDown;
        int laserCountdown = 0;
        float spinDirection = 0;
        void BigIron()
        {
            if (PhaseTwo)
            {
                if (MoveTimer == 1)
                {
                    UsefulFunctions.BroadcastText("Retinazer's Hull begins glowing fiercely...", Color.OrangeRed);
                }

                rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                
                //Normal movement when not charging laser
                if (laserCountdown == 0)
                {
                    spinDirection = 0;
                    rotationSpeed = 0.2f;
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -400), 0.1f, 200);
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
                if(MoveTimer == 200)
                {
                    laserCountdown = 616;
                }

                //Fire laser
                if (MoveTimer == 230)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        currentProjectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetOmegaLaser>(), 249999999, 0.5f, Main.myPlayer, NPC.whoAmI);
                    }
                }
            }
            else
            {
                rotationSpeed = 0.03f;
                if (MoveTimer == 1)
                {
                    aimingDown = true;
                    rotationTarget = -MathHelper.PiOver4 - MathHelper.PiOver2;
                    NPC.rotation = -MathHelper.PiOver4 - MathHelper.PiOver2;
                }
                float laserCooldown = 200;
                UsefulFunctions.SmoothHoming(NPC, Main.player[NPC.target].Center + new Vector2(-700, 0), 0.7f, 20, null, true);

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
                }
                if (MoveTimer % laserCooldown == 30 && MoveTimer < 750)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        currentProjectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
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

            


            if (PhaseTwo && MoveTimer < 800)
            {
                rotationSpeed = NPC.Distance(target.Center) / 5000;


                UsefulFunctions.SmoothHoming(NPC, target.Center, 0.25f, 25, null, false);

                //Fire

                if (MoveTimer % 75 == 0 && Main.netMode != NetmodeID.MultiplayerClient && MoveTimer < 800)
                {
                    currentProjectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.RetPiercingLaser>(), DeathLaserDamage, 0.5f, Main.myPlayer, target.whoAmI, NPC.whoAmI);
                }
            }
            else
            {
                rotationSpeed = 0.2f;

                //Telegraph before each charge
                if (MoveTimer % 75 < 15)
                {
                    UsefulFunctions.DustRing(NPC.Center, (15 - MoveTimer % 75) * 20, DustID.GemRuby, 100, 2);
                }

                //Charge
                if (MoveTimer % 75 == 15)
                {
                    NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 15);
                }

                //Fire
                else if (MoveTimer % 75 > 15 && MoveTimer % 75 < 35)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2();
                        projVector.Normalize();
                        projVector *= 11;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVector, ProjectileID.DeathLaser, DeathLaserDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
        }

        //Hovers top right of the player and fires hitscan lingering lasers repeatedly
        //Phase 2: Comes to a stop and then fires OHKO laser continuously, requiring orbiting the boss to survive
        void Firing()
        {
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            rotationSpeed = 1;
            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(-500, -350), 0.7f, 20);

            if (MoveTimer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //TODO: Replace this with a hitscan laser
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6), ProjectileID.DeathLaser, DeathLaserDamage, 0.5f, Main.myPlayer);
            }

        }

        void TBD()
        {
            NextAttack();
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
            NPC.rotation += rotationVelocity;
            NPC.velocity *= 0.95f;
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<CataMove> {
                new CataMove(BigIron, CataMoveID.BigIron, "Big Iron"),
                new CataMove(Charging, CataMoveID.Charging, "Charging"),
                new CataMove(Firing, CataMoveID.Firing, "Firing"),
                };
        }

        private class CataMoveID
        {
            public const short BigIron = 0;
            public const short Firing = 1;
            public const short Charging = 2;
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
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 8").Type, 1f);
            }
        }
    }
}