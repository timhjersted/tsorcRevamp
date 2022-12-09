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
    [AutoloadBossHead]
    class Cataluminance : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
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

            NPC.value = 600000;
            NPC.aiStyle = -1;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            despawnHandler = new NPCDespawnHandler("The Triplets return to the skies...", Color.Cyan, 180);
            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cataluminance");
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

        public override void AI()
        {
            Main.NewText("Cat: " + CurrentMove.Name + " at " + MoveTimer);
            MoveTimer++;
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);
            NPC.rotation = (NPC.rotation + (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2) / 2f;
            FindFrame(0);

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

            CurrentMove.Move();

            if (MoveTimer >= 900)
            {
                NextAttack();
            }

            if (NPC.Distance(target.Center) > 4000)
            {
                NPC.Center = target.Center + new Vector2(0, -1000);
                UsefulFunctions.BroadcastText("Cataluminance Closes In...");
            }
        }

        //Hover to the upper right of the screen and spam homing blasts that chase the player
        void StarBlasts()
        {
            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(550, -350), 0.5f, 20, target.velocity);

            if(MoveTimer % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //TODO: Add magic blast projectile
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer);
            }            
        }

        //Chase the player rapidly and smoothly, leaving a damaging trail in its wake that obstructs movement
        void Pursuit()
        {
            UsefulFunctions.SmoothHoming(NPC, target.Center, 0.15f, 15, target.velocity, false);
            if(Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (MoveTimer % 300 < 50 || PhaseTwo)
                {
                    int trail = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 2);
                    Main.projectile[trail].rotation = NPC.rotation;
                }
            }
        }


        float angle = 0;
        void Starstorm()
        {
            NPC.rotation = MathHelper.Pi;

            if(MoveTimer == 0)
            {
                angle = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
            }

            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -350), 2f, 20);

            //Spawn homing stars
            if (MoveTimer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                //In phase 2 the stars leave damaging trails like EoL
                if (PhaseTwo)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, -37), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer);
                }
                else
                {

                    //Stars fired upward for effect
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(Main.rand.NextFloat(-20, 20), -37).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 2);

                    //Stars rain down
                    Vector2 spawnPos = NPC.Center + new Vector2(Main.rand.NextFloat(-700, 700), -500);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, new Vector2(0, 7).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triplets.HomingStar>(), StarBlastDamage, 0.5f, Main.myPlayer, 1);
                }
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
            
            if(transformationTimer <= 60)
            {
                rotationVelocity = transformationTimer / 60;
            }
            else
            {
                rotationVelocity = 1 - (transformationTimer / 60);
            }

            if (transformationTimer == 60 && !Main.dedServ)
            {
                //TODO spawn gore
            }
            NPC.rotation += rotationVelocity;
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
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TripletsBag>()));
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