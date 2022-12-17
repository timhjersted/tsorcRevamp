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
    class SpazmatismV2 : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPC.damage = 50;
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

            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spazmatism");
            NPCID.Sets.TrailCacheLength[NPC.type] = 50;
            NPCID.Sets.TrailingMode[NPC.type] = 2;
        }

        int EyeFireDamage = 25;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer;
        SpazMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<SpazMove> MoveList;

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
        int finalStandTimer = 0;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            if (NPC.realLife < 0)
            {
                int? catID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Cataluminance>());

                if (catID != null)
                {
                    NPC.realLife = catID.Value;
                }
            }

            if (NPC.realLife < 0 || !Main.npc[NPC.realLife].active)
            {
                OnKill();
                NPC.active = false;
            }
            else
            {
                NPC.life = Main.npc[NPC.realLife].life;
                NPC.target = Main.npc[NPC.realLife].target;
            }

            FindFrame(0);

            //Main.NewText("Spaz: " + CurrentMove.Name + " at " + MoveTimer);
            MoveTimer++;
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);

            if (NPC.Distance(target.Center) > 4000)
            {
                NPC.Center = target.Center + new Vector2(0, 1000);
                UsefulFunctions.BroadcastText("Spazmatism Closes In...");
            }

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


        //Charges, firing a shotgun spread of eye fire each time it does
        //Phase 2: Fire aura
        void Charging()
        {
            if (PhaseTwo)
            {
                //Telegraph for the first second before the starting charge
                if (MoveTimer < 70)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    UsefulFunctions.DustRing(NPC.Center, (70 - MoveTimer) * 30, DustID.CursedTorch, 100, 10);
                    return;
                }
                if (MoveTimer % 70 < 30)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    UsefulFunctions.DustRing(NPC.Center, (10 - MoveTimer % 70) * 20, DustID.CursedTorch, 100, 10);
                }
                if (MoveTimer % 70 == 30)
                {
                    NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 25);
                }
                NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity / 10f, ProjectileID.EyeFire, EyeFireDamage, 0.5f, Main.myPlayer);

            }
            else
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 0.15f, 20, target.velocity, false);

                //Telegraph for the first second before the starting charge
                if (MoveTimer < 90)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    UsefulFunctions.DustRing(NPC.Center, (90 - MoveTimer) * 30, DustID.CursedTorch, 100, 10);
                    return;
                }
                if (MoveTimer % 90 < 30)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    UsefulFunctions.DustRing(NPC.Center, (90 - MoveTimer % 90) * 20, DustID.CursedTorch, 100, 10);
                }
                if (MoveTimer % 90 == 30)
                {
                    NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 18);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 5), ProjectileID.CursedFlameHostile, EyeFireDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
        }

        //Spams cursed eye fire at the player
        //Phase 2: Flames leave a damaging trail, or maybe it fires 8 in all directions? Unsure
        void Firing()
        {

            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(600, 300), 1f, 20);
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (PhaseTwo)
            {

            }
            else
            {
                if (MoveTimer % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 offset = new Vector2(-50, 0).RotatedBy((NPC.Center - target.Center).ToRotation());
                    float angle = -MathHelper.Pi / 3;
                    for (int i = 0; i < 3; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + offset, UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 4).RotatedBy(angle), ProjectileID.CursedFlameHostile, EyeFireDamage, 0.5f, Main.myPlayer);
                        angle += MathHelper.Pi / 3;
                    }
                }
            }            
        }

        //Spaz aims down and breathes cursed fire into the earth
        //It erupts in bursts of neon green flame
        //Phase 2: Geysers of continuous flame like kraken has
        void IchorTrackers()
        {
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, 350), 0.5f, 20);
            
            if (!PhaseTwo)
            {
                if (MoveTimer % 120 == 10 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<Projectiles.Enemy.Triplets.IchorGlob>());
                }
            }
            else
            {
                if (MoveTimer % 180 == 10 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<Projectiles.Enemy.Triplets.IchorMissile>());
                }
            }
        }

        void FinalStand()
        {
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            if (MoveTimer % 70 == 10)
            {
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, target.Center, 25);
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

            if (transformationTimer <= 60)
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
            MoveTimer = 0;
            NPC.rotation += rotationVelocity;
            NPC.velocity *= 0.95f;
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<SpazMove> {
                new SpazMove(Charging, SpazMoveID.Charging, "Charging"),
                new SpazMove(Firing, SpazMoveID.Firing, "Firing"),
                new SpazMove(IchorTrackers, SpazMoveID.IchorTrackers, "Ichor Trackers"),
                };
        }

        private class SpazMoveID
        {
            public const short Charging = 0;
            public const short IchorTrackers = 1;
            public const short Firing = 2;
            public const short TBD = 3;
        }
        private class SpazMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public SpazMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
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