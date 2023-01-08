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

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Cataluminance : ModNPC
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

            despawnHandler = new NPCDespawnHandler("The Triad returns to the skies...", Color.Cyan, 180);
            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cataluminance");
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;
            NPCID.Sets.TrailingMode[NPC.type] = 2;
        }

        int StarBlastDamage = 25;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer = 0;
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
        public int MoveTimer
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

        int finalStandTimer = 0;
        float rotationTarget = 0;
        float rotationSpeed = 0.05f;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
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

            MoveTimer++;
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);

            //Smart rotation
            //The conversions are necessary to avoid some XNA rotation bullshit
            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            FindFrame(0);

            //Teleport if too far away
            if (NPC.Distance(target.Center) > 4000)
            {
                NPC.Center = target.Center + new Vector2(0, -1000);
                NPC.netUpdate = true;
                UsefulFunctions.BroadcastText("Cataluminance Closes In...");
            }

            //This exists because I wanted to make the fight far faster paced than even supersonic wings 1 allows
            //Unfinished: It will be applied by grazing Cataluimance's illuminant projectiles later
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.FasterThanSight>(), 5);
                }
            }

            if (NPC.life < NPC.lifeMax / 2 && transformationTimer < 120)
            {
                Transform();
                return;
            }

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }

            //Switch into final stand if lower than 10% health
            if (NPC.life < NPC.lifeMax / 10f)
            {
                if(finalStandTimer == 0)
                {
                    UsefulFunctions.BroadcastText("The Triad prepares to take you down with them...", Color.Cyan);
                }

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

        //Hover to the upper right of the screen and spam homing blasts that chase the player
        void StarBlasts()
        {
            rotationTarget = Vector2.Normalize(NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            rotationSpeed = 0.05f;
            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(750, -350), 0.5f, 20, target.velocity);

            if (PhaseTwo)
            {
                if (MoveTimer % 90 == 0 && MoveTimer < 850 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(MathHelper.PiOver4), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(MathHelper.PiOver4 / 2f), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(-MathHelper.PiOver4 / 2f), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(-MathHelper.PiOver4), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                }
            }
            else
            {
                if (MoveTimer % 60 == 0 && MoveTimer < 850 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 0);
                }                
            }

            //Clear all homing stars when attack is over
            if (MoveTimer == 899)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>())
                    {
                        Main.projectile[i].Kill();
                    }
                }
            }
        }

        //Chase the player rapidly and smoothly, leaving a damaging trail in its wake that obstructs movement
        void Pursuit()
        {
            rotationTarget = NPC.velocity.ToRotation() - MathHelper.PiOver2;
            rotationSpeed = 0.1f;

            if (MoveTimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.Trails.CataluminanceTrail>(), 35, 0, Main.myPlayer, 1, NPC.whoAmI);
            }
            float homingStrength = 0.17f;
            if (PhaseTwo)
            {
                homingStrength = 0.10f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (MoveTimer % 150 < 30)
                    {
                        UsefulFunctions.DustRing(NPC.Center, (30 - MoveTimer % 75) * 15, DustID.GemSapphire, 150, 2);
                    }
                    if (MoveTimer % 150 == 30 && MoveTimer > 76)
                    {
                        NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 15);
                        NPC.netUpdate = true;
                    }
                }
            }

            UsefulFunctions.SmoothHoming(NPC, target.Center, homingStrength, 12, target.velocity, false);
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

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //In phase 2 the stars leave damaging trails like EoL, but there are fewer of them
                if (PhaseTwo)
                {
                    if (MoveTimer % 15 == 0)
                    {
                        //Stars fired upward for effect
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -60), new Vector2(Main.rand.NextFloat(-20, 20), -37).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 2, 1);

                        //Stars rain down
                        Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-2000, 2000), -700);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(0, 7).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 1, 1);
                    }
                }
                else if (MoveTimer % 12 == 0)
                {
                    //Stars fired upward for effect
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -60), new Vector2(Main.rand.NextFloat(-20, 20), -37).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 2);

                    //Stars rain down
                    Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-2500, 2500), -700);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(0, 7).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 1);
                }
            }
        }

        void FinalStand()
        {
            if (MoveTimer % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(MathHelper.PiOver4), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(-MathHelper.PiOver4), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 6).RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<Projectiles.Enemy.Triad.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 0, 1);
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

        float rotationVelocity;
        void Transform()
        {
            transformationTimer++;
            
            if(transformationTimer <= 60)
            {
                rotationVelocity = transformationTimer / 600;
            }
            else
            {
                rotationVelocity = 1 - (transformationTimer / 600);
            }

            if (transformationTimer == 60 && !Main.dedServ)
            {
                //TODO spawn gore
            }
            MoveTimer = 0;
            NPC.rotation += rotationVelocity;
            NPC.velocity *= 0.95f;
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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TriadBag>()));
        }

        //TODO: Copy vanilla death effects
        public override void OnKill()
        {
            UsefulFunctions.BroadcastText("Retinazer has been defeated!", Color.MediumPurple);
            UsefulFunctions.BroadcastText("Spazmatism has been defeated!", Color.MediumPurple);
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

            int? spaz = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SpazmatismV2>());
            if(spaz != null)
            {
                Main.npc[spaz.Value].HitEffect(1, 9999999);
            }

            int? ret = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.RetinazerV2>());
            if (ret != null)
            {
                Main.npc[ret.Value].HitEffect(1, 9999999);
            }
        }
    }
}